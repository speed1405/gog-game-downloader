using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GogGameDownloader.Services.Library;

public record OwnedGame(string GameId, string Title, string? PosterUrl, long? SizeBytes);

public interface IGameLibraryService
{
    Task<IReadOnlyList<OwnedGame>> GetOwnedGamesAsync(CancellationToken ct = default);
}
