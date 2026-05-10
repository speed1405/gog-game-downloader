using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GogGameDownloader.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _downloadPath = string.Empty;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 2;

    [RelayCommand]
    private void Save()
    {
        // TODO: Phase 7 — persist settings to AppSettings table via ISettingsRepository
    }

    [RelayCommand]
    private void BrowseDownloadPath()
    {
        // TODO: Phase 5 — open folder picker dialog and update DownloadPath
    }
}
