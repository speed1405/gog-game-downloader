using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GogGameDownloader.Services.Auth;

public class OAuthPkceAuthService : IAuthService, IDisposable
{
    private const string ClientId = "46899977096215655";
    private const string AuthorizeUrl = "https://auth.gog.com/auth";
    private const string TokenUrl = "https://auth.gog.com/token";
    private const string RedirectUriBase = "http://127.0.0.1";
    private const string TokenKeyAccess = "gog_access_token";
    private const string TokenKeyRefresh = "gog_refresh_token";
    private const string TokenKeyExpiry = "gog_token_expiry";
    private const string TokenKeyUser = "gog_user_json";

    private readonly ISecureTokenStore _tokenStore;
    private readonly HttpClient _httpClient;
    private AuthTokens? _tokens;
    private AuthUser? _currentUser;

    public bool IsAuthenticated => _currentUser != null && _tokens != null;
    public AuthUser? CurrentUser => _currentUser;
    public AuthTokens? CurrentTokens => _tokens;

    public event EventHandler<AuthUser?> AuthStateChanged = delegate { };

    public OAuthPkceAuthService(ISecureTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GogGameDownloader/1.0");
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        var accessToken = await _tokenStore.LoadTokenAsync(TokenKeyAccess);
        var refreshToken = await _tokenStore.LoadTokenAsync(TokenKeyRefresh);
        var expiryStr = await _tokenStore.LoadTokenAsync(TokenKeyExpiry);
        var userJson = await _tokenStore.LoadTokenAsync(TokenKeyUser);

        if (refreshToken == null) return false;

        if (DateTime.TryParse(expiryStr, out var expiry) && accessToken != null)
        {
            if (expiry > DateTime.UtcNow.AddMinutes(5))
            {
                _tokens = new AuthTokens(accessToken, refreshToken, expiry);
                if (userJson != null)
                    _currentUser = JsonSerializer.Deserialize<AuthUser>(userJson);
                AuthStateChanged(this, _currentUser);
                return true;
            }
        }

        _tokens = new AuthTokens(accessToken ?? string.Empty, refreshToken, DateTime.MinValue);
        return await RefreshTokenAsync(ct);
    }

    public async Task<bool> SignInAsync(CancellationToken ct = default)
    {
        var port = GetFreePort();
        var redirectUri = $"{RedirectUriBase}:{port}/callback";
        var (verifier, challenge) = GeneratePkce();
        var state = GenerateState();

        var authUrl = BuildAuthUrl(redirectUri, challenge, state);
        OpenBrowser(authUrl);

        var code = await ListenForCallbackAsync(port, state, ct);
        if (code == null) return false;

        return await ExchangeCodeAsync(code, verifier, redirectUri, ct);
    }

    public async Task SignOutAsync(bool revokeRemote = false, CancellationToken ct = default)
    {
        _tokens = null;
        _currentUser = null;

        await _tokenStore.DeleteTokenAsync(TokenKeyAccess);
        await _tokenStore.DeleteTokenAsync(TokenKeyRefresh);
        await _tokenStore.DeleteTokenAsync(TokenKeyExpiry);
        await _tokenStore.DeleteTokenAsync(TokenKeyUser);

        AuthStateChanged(this, null);
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken ct = default)
    {
        if (_tokens?.RefreshToken == null) return false;

        try
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _tokens.RefreshToken
            };

            var response = await _httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                await SignOutAsync(ct: ct);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            return await ParseAndStoreTokensAsync(json, ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        if (_tokens == null) return null;

        if (_tokens.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            if (!await RefreshTokenAsync(ct)) return null;
        }

        return _tokens?.AccessToken;
    }

    private async Task<bool> ExchangeCodeAsync(string code, string verifier, string redirectUri, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = verifier,
            ["redirect_uri"] = redirectUri
        };

        try
        {
            var response = await _httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(ct);
            return await ParseAndStoreTokensAsync(json, ct);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ParseAndStoreTokensAsync(string json, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = root.TryGetProperty("refresh_token", out var rt)
                ? rt.GetString() ?? string.Empty
                : _tokens?.RefreshToken ?? string.Empty;
            var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
            var expiry = DateTime.UtcNow.AddSeconds(expiresIn);

            _tokens = new AuthTokens(accessToken, refreshToken, expiry);

            await _tokenStore.SaveTokenAsync(TokenKeyAccess, accessToken);
            await _tokenStore.SaveTokenAsync(TokenKeyRefresh, refreshToken);
            await _tokenStore.SaveTokenAsync(TokenKeyExpiry, expiry.ToString("O"));

            if (_currentUser == null)
                await FetchUserInfoAsync(accessToken, ct);

            AuthStateChanged(this, _currentUser);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task FetchUserInfoAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://embed.gog.com/userData.json");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var username = root.TryGetProperty("username", out var un) ? un.GetString() : null;
            var avatar = root.TryGetProperty("avatar", out var av) ? av.GetString() : null;
            var email = root.TryGetProperty("email", out var em) ? em.GetString() : null;

            _currentUser = new AuthUser(username ?? "Unknown", avatar, email);

            var userJson = JsonSerializer.Serialize(_currentUser);
            await _tokenStore.SaveTokenAsync(TokenKeyUser, userJson);
        }
        catch { }
    }

    private static (string verifier, string challenge) GeneratePkce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var verifier = Base64UrlEncode(bytes);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64UrlEncode(challengeBytes);
        return (verifier, challenge);
    }

    private static string GenerateState()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildAuthUrl(string redirectUri, string challenge, string state)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = ClientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["code_challenge"] = challenge;
        query["code_challenge_method"] = "S256";
        query["state"] = state;
        query["scope"] = "openid profile email";
        return $"{AuthorizeUrl}?{query}";
    }

    private static async Task<string?> ListenForCallbackAsync(int port, string expectedState, CancellationToken ct)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/callback/");
        listener.Start();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

            var contextTask = listener.GetContextAsync();
            await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));

            if (!contextTask.IsCompletedSuccessfully) return null;

            var context = await contextTask;
            var query = context.Request.QueryString;
            var code = query["code"];
            var state = query["state"];

            var responseHtml = code != null
                ? "<html><body><h2>Authentication successful! You can close this window.</h2></body></html>"
                : "<html><body><h2>Authentication failed. Please close this window and try again.</h2></body></html>";

            var responseBytes = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = responseBytes.Length;
            await context.Response.OutputStream.WriteAsync(responseBytes, ct);
            context.Response.Close();

            if (state != expectedState) return null;
            return code;
        }
        catch
        {
            return null;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch { }
    }

    public void Dispose() => _httpClient.Dispose();
}
