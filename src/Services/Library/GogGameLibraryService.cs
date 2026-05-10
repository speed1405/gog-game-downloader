using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GogGameDownloader.Services.Auth;

namespace GogGameDownloader.Services.Library;

public class GogGameLibraryService : IGameLibraryService
{
    private const string LibraryUrl = "https://embed.gog.com/user/data/games";
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private readonly IAuthService _authService;

    public GogGameLibraryService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(CancellationToken ct = default)
    {
        var token = await _authService.GetValidAccessTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            return [];
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, LibraryUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await HttpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return ParseOwnedGames(content);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"Failed to load GOG library: {ex}");
            return [];
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"GOG library request canceled/timed out: {ex}");
            return [];
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse GOG library response: {ex}");
            return [];
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<OwnedGame> ParseOwnedGames(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var games = new List<OwnedGame>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (TryParseGame(item, out var game))
                {
                    games.Add(game);
                }
            }

            return games;
        }

        if (root.TryGetProperty("games", out var gamesArray) && gamesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in gamesArray.EnumerateArray())
            {
                if (TryParseGame(item, out var game))
                {
                    games.Add(game);
                }
            }
        }

        if (root.TryGetProperty("products", out var productsArray) && productsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in productsArray.EnumerateArray())
            {
                if (TryParseGame(item, out var game))
                {
                    games.Add(game);
                }
            }
        }

        return games;
    }

    private static bool TryParseGame(JsonElement item, out OwnedGame game)
    {
        game = default!;

        var id = GetString(item, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            id = GetString(item, "gameId");
        }

        var title = GetString(item, "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            title = GetString(item, "name");
        }

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var poster = GetString(item, "image") ??
                     GetString(item, "coverHorizontal") ??
                     GetString(item, "coverVertical") ??
                     GetString(item, "poster");

        var size = GetLong(item, "size");
        game = new OwnedGame(id, title, poster, size);
        return true;
    }

    private static string? GetString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            _ => null
        };
    }

    private static long? GetLong(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var value))
        {
            return value;
        }

        if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out value))
        {
            return value;
        }

        return null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "GogGameDownloader/1.0");
        return client;
    }
}
