using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GogGameDownloader.Services.Settings;
using GogGameDownloader.Services.Storage;

namespace GogGameDownloader.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private string _downloadPath = string.Empty;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 2;

    public SettingsViewModel(ISettingsRepository settingsRepository, IStorageService storageService)
    {
        _settingsRepository = settingsRepository;
        _storageService = storageService;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task Save()
    {
        await _settingsRepository.SetValueAsync(AppSettingKeys.StartWithWindows, StartWithWindows.ToString(CultureInfo.InvariantCulture));
        await _settingsRepository.SetValueAsync(AppSettingKeys.MinimizeToTray, MinimizeToTray.ToString(CultureInfo.InvariantCulture));
        await _settingsRepository.SetValueAsync(AppSettingKeys.MaxConcurrentDownloads, MaxConcurrentDownloads.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrWhiteSpace(DownloadPath))
        {
            await _storageService.SetPrimaryDownloadPathAsync(DownloadPath);
        }
    }

    [RelayCommand]
    private async Task BrowseDownloadPath()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Select download folder",
            AllowMultiple = false
        };

        if (!string.IsNullOrWhiteSpace(DownloadPath) && Directory.Exists(DownloadPath))
        {
            var startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(DownloadPath);
            if (startFolder is not null)
            {
                options.SuggestedStartLocation = startFolder;
            }
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count == 0)
        {
            return;
        }

        DownloadPath = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
    }

    private async Task LoadAsync()
    {
        DownloadPath = await _storageService.GetPrimaryDownloadPathAsync() ??
                       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        var settings = await _settingsRepository.GetValuesAsync([
            AppSettingKeys.StartWithWindows,
            AppSettingKeys.MinimizeToTray,
            AppSettingKeys.MaxConcurrentDownloads
        ]);

        if (settings.TryGetValue(AppSettingKeys.StartWithWindows, out var startWithWindowsRaw) &&
            bool.TryParse(startWithWindowsRaw, out var startWithWindows))
        {
            StartWithWindows = startWithWindows;
        }

        if (settings.TryGetValue(AppSettingKeys.MinimizeToTray, out var minimizeToTrayRaw) &&
            bool.TryParse(minimizeToTrayRaw, out var minimizeToTray))
        {
            MinimizeToTray = minimizeToTray;
        }

        if (settings.TryGetValue(AppSettingKeys.MaxConcurrentDownloads, out var maxConcurrentDownloadsRaw) &&
            int.TryParse(maxConcurrentDownloadsRaw, out var maxConcurrentDownloads))
        {
            MaxConcurrentDownloads = Math.Clamp(maxConcurrentDownloads, 1, 10);
        }
    }
}
