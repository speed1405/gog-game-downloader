using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GogGameDownloader.Data;
using GogGameDownloader.Models;
using Microsoft.EntityFrameworkCore;

namespace GogGameDownloader.Services.Download;

public class DownloadService : IDownloadService
{
    private readonly AppDbContext _dbContext;

    public DownloadService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DownloadJob> EnqueueAsync(int gameVersionId, string targetPath, CancellationToken ct = default)
    {
        var job = new DownloadJob
        {
            GameVersionId = gameVersionId,
            TargetPath = targetPath,
            Status = DownloadStatus.Queued,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.DownloadJobs.Add(job);
        await _dbContext.SaveChangesAsync(ct);
        return job;
    }

    public async Task PauseAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _dbContext.DownloadJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return;
        }

        if (job.Status is DownloadStatus.Downloading or DownloadStatus.Queued or DownloadStatus.Verifying)
        {
            job.Status = DownloadStatus.Paused;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task ResumeAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _dbContext.DownloadJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return;
        }

        if (job.Status is DownloadStatus.Paused or DownloadStatus.AuthRequired)
        {
            job.Status = DownloadStatus.Downloading;
            job.StartedAt ??= DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task CancelAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _dbContext.DownloadJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null)
        {
            return;
        }

        job.Status = DownloadStatus.Error;
        job.ErrorMessage = "Canceled by user";
        job.FinishedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DownloadJob>> GetActiveJobsAsync(CancellationToken ct = default)
    {
        return await _dbContext.DownloadJobs
            .AsNoTracking()
            .Include(j => j.GameVersion)
            .ThenInclude(v => v!.Game)
            .Where(j => j.Status != DownloadStatus.Completed && j.Status != DownloadStatus.Error)
            .OrderByDescending(j => j.StartedAt)
            .ThenByDescending(j => j.Id)
            .ToListAsync(ct);
    }
}
