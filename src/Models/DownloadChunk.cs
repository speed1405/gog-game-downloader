using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GogGameDownloader.Models;

public class DownloadChunk
{
    [Key]
    public int Id { get; set; }
    public int JobId { get; set; }
    public int ChunkIndex { get; set; }
    public long ByteOffset { get; set; }
    public long ByteLength { get; set; }
    public string? ChecksumMd5 { get; set; }
    public bool IsVerified { get; set; }

    [ForeignKey(nameof(JobId))]
    public DownloadJob? Job { get; set; }
}
