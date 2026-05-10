using System.Collections.Generic;
using System.Threading.Tasks;

namespace GogGameDownloader.Services.Storage;

public record DriveInfo2(string Name, string RootPath, long TotalBytes, long FreeBytes, long GogUsedBytes);

public interface IStorageService
{
    IReadOnlyList<DriveInfo2> GetAvailableDrives();
    Task<string?> GetPrimaryDownloadPathAsync();
    Task SetPrimaryDownloadPathAsync(string path);
    Task<string?> GetBackupTargetPathAsync();
    Task SetBackupTargetPathAsync(string path);
    long GetGogUsageBytes(string rootPath);
}
