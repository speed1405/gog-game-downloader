using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GogGameDownloader.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<GameCardViewModel> Games { get; } = new();

    public bool IsEmpty => Games.Count == 0;

    public LibraryViewModel()
    {
        Games.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private void Refresh()
    {
        // TODO: Phase 3 — fetch game catalog from GOG API and populate Games collection
    }
}

public partial class GameCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _gameId = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _posterUrl;

    [ObservableProperty]
    private string _statusLabel = "Queued";

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _sizeLabel = string.Empty;

    [ObservableProperty]
    private bool _hasDownloadProgress;
}
