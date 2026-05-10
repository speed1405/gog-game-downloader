using System;
using System.Threading;
using System.Threading.Tasks;

namespace GogGameDownloader.Services.Auth;

public record AuthUser(string Username, string? AvatarUrl, string? Email);

public record AuthTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public interface IAuthService
{
    bool IsAuthenticated { get; }
    AuthUser? CurrentUser { get; }
    AuthTokens? CurrentTokens { get; }

    Task<bool> SignInAsync(CancellationToken ct = default);
    Task SignOutAsync(bool revokeRemote = false, CancellationToken ct = default);
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
    Task<bool> RefreshTokenAsync(CancellationToken ct = default);
    Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default);

    event EventHandler<AuthUser?> AuthStateChanged;
}
