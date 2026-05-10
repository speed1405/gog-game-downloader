using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GogGameDownloader.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public LibraryViewModel Library { get; }
    public DownloadsViewModel Downloads { get; }
    public StorageViewModel Storage { get; }
    public SettingsViewModel Settings { get; }
    public AuthViewModel Auth { get; }

    public MainWindowViewModel(
        LibraryViewModel library,
        DownloadsViewModel downloads,
        StorageViewModel storage,
        SettingsViewModel settings,
        AuthViewModel auth)
    {
        Library = library;
        Downloads = downloads;
        Storage = storage;
        Settings = settings;
        Auth = auth;
        _currentPage = library;
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page switch
        {
            "Library" => Library,
            "Downloads" => Downloads,
            "Storage" => Storage,
            "Settings" => Settings,
            _ => Library
        };
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;
}
