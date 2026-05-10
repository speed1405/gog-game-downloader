using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GogGameDownloader.Services.Image;

public interface IImageCacheService
{
    Task<Bitmap?> GetPosterAsync(string gameId, string? posterUrl, CancellationToken ct = default);
    void InvalidateCache(string gameId);
    void ClearAll();
}
