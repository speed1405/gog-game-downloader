using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GogGameDownloader.Services.Auth;
using GogGameDownloader.Services.Library;

namespace GogGameDownloader.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IAuthService _authService;
    private readonly List<GameCardViewModel> _allGames = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<GameCardViewModel> Games { get; } = new();

    public bool IsEmpty => Games.Count == 0;

    public LibraryViewModel(IGameLibraryService gameLibraryService, IAuthService authService)
    {
        _gameLibraryService = gameLibraryService;
        _authService = authService;

        Games.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        _authService.AuthStateChanged += OnAuthStateChanged;

        if (_authService.IsAuthenticated)
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var games = await _gameLibraryService.GetOwnedGamesAsync();
            _allGames.Clear();
            _allGames.AddRange(games
                .OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .Select(g => new GameCardViewModel
                {
                    GameId = g.GameId,
                    Title = g.Title,
                    PosterUrl = g.PosterUrl,
                    StatusLabel = "Ready",
                    SizeLabel = g.SizeBytes.HasValue ? FormatSize(g.SizeBytes.Value) : string.Empty,
                    HasDownloadProgress = false,
                    DownloadProgress = 0
                }));

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void OnAuthStateChanged(object? sender, AuthUser? user)
    {
        if (user is null)
        {
            _allGames.Clear();
            Games.Clear();
            return;
        }

        _ = RefreshAsync();
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        var filtered = string.IsNullOrWhiteSpace(search)
            ? _allGames
            : _allGames.Where(g => g.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        Games.Clear();
        foreach (var game in filtered)
        {
            Games.Add(game);
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
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
