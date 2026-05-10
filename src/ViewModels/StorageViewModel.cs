using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GogGameDownloader.Services.Storage;

namespace GogGameDownloader.ViewModels;

public partial class StorageViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;

    public ObservableCollection<DriveCardViewModel> Drives { get; } = new();

    public StorageViewModel(IStorageService storageService)
    {
        _storageService = storageService;
        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var primaryPath = await _storageService.GetPrimaryDownloadPathAsync();
        var backupPath = await _storageService.GetBackupTargetPathAsync();

        var drives = await Task.Run(() => _storageService.GetAvailableDrives());

        Drives.Clear();
        foreach (var drive in drives)
        {
            Drives.Add(new DriveCardViewModel
            {
                Name = drive.Name,
                TotalBytes = drive.TotalBytes,
                FreeBytes = drive.FreeBytes,
                GogUsedBytes = drive.GogUsedBytes,
                IsPrimary = !string.IsNullOrWhiteSpace(primaryPath) &&
                            primaryPath.StartsWith(drive.RootPath, System.StringComparison.OrdinalIgnoreCase),
                IsBackupTarget = !string.IsNullOrWhiteSpace(backupPath) &&
                                 backupPath.StartsWith(drive.RootPath, System.StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize storage page: {ex}");
        }
    }
}

public partial class DriveCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private long _freeBytes;

    [ObservableProperty]
    private long _gogUsedBytes;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private bool _isBackupTarget;

    private const long BytesPerGigabyte = 1_073_741_824L;

    public double UsedPercent => TotalBytes > 0 ? (double)(TotalBytes - FreeBytes) / TotalBytes * 100 : 0;
    public double FreePercent => TotalBytes > 0 ? (double)FreeBytes / TotalBytes * 100 : 0;
    public string FreeLabel => $"{FreeBytes / (double)BytesPerGigabyte:F1} GB free";
    public string GogUsedLabel => $"{GogUsedBytes / (double)BytesPerGigabyte:F1} GB used by GOG data";
    public bool IsCriticalSpace => FreePercent < 5;
    public bool IsWarningSpace => !IsCriticalSpace && FreePercent < 15;
    public string SpaceStateLabel => IsCriticalSpace ? "Critical: less than 5% free" : IsWarningSpace ? "Warning: less than 15% free" : "Healthy free space";
}
