using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GogGameDownloader.ViewModels;

public partial class StorageViewModel : ViewModelBase
{
    public ObservableCollection<DriveCardViewModel> Drives { get; } = new();

    public StorageViewModel()
    {
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        Drives.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            Drives.Add(new DriveCardViewModel
            {
                Name = drive.Name,
                TotalBytes = drive.TotalSize,
                FreeBytes = drive.AvailableFreeSpace
            });
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
}
