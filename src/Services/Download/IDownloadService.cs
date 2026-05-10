using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GogGameDownloader.Models;

namespace GogGameDownloader.Services.Download;

public interface IDownloadService
{
    Task<DownloadJob> EnqueueAsync(int gameVersionId, string targetPath, CancellationToken ct = default);
    Task PauseAsync(int jobId, CancellationToken ct = default);
    Task ResumeAsync(int jobId, CancellationToken ct = default);
    Task CancelAsync(int jobId, CancellationToken ct = default);
    Task<IReadOnlyList<DownloadJob>> GetActiveJobsAsync(CancellationToken ct = default);
}
