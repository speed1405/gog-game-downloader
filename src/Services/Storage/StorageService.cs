using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GogGameDownloader.Services.Settings;

namespace GogGameDownloader.Services.Storage;

public class StorageService : IStorageService
{
    private readonly ISettingsRepository _settingsRepository;

    public StorageService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public IReadOnlyList<DriveInfo2> GetAvailableDrives()
    {
        var drives = new List<DriveInfo2>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            drives.Add(new DriveInfo2(
                drive.Name,
                drive.RootDirectory.FullName,
                drive.TotalSize,
                drive.AvailableFreeSpace,
                GetGogUsageBytes(drive.RootDirectory.FullName)));
        }

        return drives;
    }

    public async Task<string?> GetPrimaryDownloadPathAsync()
    {
        return await _settingsRepository.GetValueAsync(AppSettingKeys.PrimaryDownloadPath);
    }

    public async Task SetPrimaryDownloadPathAsync(string path)
    {
        await _settingsRepository.SetValueAsync(AppSettingKeys.PrimaryDownloadPath, path);
    }

    public async Task<string?> GetBackupTargetPathAsync()
    {
        return await _settingsRepository.GetValueAsync(AppSettingKeys.BackupDownloadPath);
    }

    public async Task SetBackupTargetPathAsync(string path)
    {
        await _settingsRepository.SetValueAsync(AppSettingKeys.BackupDownloadPath, path);
    }

    public long GetGogUsageBytes(string rootPath)
    {
        var total = 0L;
        foreach (var candidate in GetCandidatePaths(rootPath))
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            total += GetDirectorySizeSafe(candidate);
        }

        return total;
    }

    private static IEnumerable<string> GetCandidatePaths(string rootPath)
    {
        yield return Path.Combine(rootPath, "GOG Games");
        yield return Path.Combine(rootPath, "GOG");
        yield return Path.Combine(rootPath, "gog");
        yield return Path.Combine(rootPath, "GogGameDownloader");
    }

    private static long GetDirectorySizeSafe(string path)
    {
        var total = 0L;
        var pending = new Stack<string>();
        pending.Push(path);

        try
        {
            while (pending.Count > 0)
            {
                var current = pending.Pop();

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(current);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    try
                    {
                        total += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // Ignore inaccessible files.
                    }
                }

                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (var directory in directories)
                {
                    pending.Push(directory);
                }
            }
        }
        catch
        {
            return 0L;
        }

        return total;
    }
}
