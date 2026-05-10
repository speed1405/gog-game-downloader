using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GogGameDownloader.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    public ObservableCollection<DownloadJobViewModel> Jobs { get; } = new();

    [ObservableProperty]
    private bool _isIdle = true;

    [RelayCommand]
    private void PauseAll()
    {
        // TODO: Phase 4 — pause all active download jobs via IDownloadService
    }

    [RelayCommand]
    private void ResumeAll()
    {
        // TODO: Phase 4 — resume all paused download jobs via IDownloadService
    }
}

public partial class DownloadJobViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private string _speedLabel = string.Empty;

    [ObservableProperty]
    private string _etaLabel = string.Empty;
}
