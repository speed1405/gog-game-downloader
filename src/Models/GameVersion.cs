using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GogGameDownloader.Models;

public class GameVersion
{
    [Key]
    public int Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? BuildId { get; set; }
    public long Size { get; set; }
    public string OS { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string InstallerUrl { get; set; } = string.Empty;
    public string? ChecksumMd5 { get; set; }

    [ForeignKey(nameof(GameId))]
    public Game? Game { get; set; }
    public ICollection<DownloadJob> DownloadJobs { get; set; } = new List<DownloadJob>();
}
