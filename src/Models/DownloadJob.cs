using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GogGameDownloader.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Verifying,
    Completed,
    Error,
    AuthRequired
}

public class DownloadJob
{
    [Key]
    public int Id { get; set; }
    public int GameVersionId { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public long BytesTotal { get; set; }
    public long BytesDone { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(GameVersionId))]
    public GameVersion? GameVersion { get; set; }
    public ICollection<DownloadChunk> Chunks { get; set; } = new List<DownloadChunk>();
}
