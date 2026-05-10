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
    private void Save() { }

    [RelayCommand]
    private void BrowseDownloadPath() { }
}
