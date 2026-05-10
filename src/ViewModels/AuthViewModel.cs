using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GogGameDownloader.Services.Auth;

namespace GogGameDownloader.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private bool _isSessionExpired;

    [ObservableProperty]
    private bool _isBusy;

    public AuthViewModel(IAuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object? sender, AuthUser? user)
    {
        IsAuthenticated = user != null;
        Username = user?.Username ?? string.Empty;
        AvatarUrl = user?.AvatarUrl;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        IsBusy = true;
        try
        {
            await _authService.SignInAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        IsBusy = true;
        try
        {
            await _authService.SignOutAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshSessionAsync()
    {
        IsBusy = true;
        try
        {
            var ok = await _authService.RefreshTokenAsync();
            IsSessionExpired = !ok;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TryRestoreSessionAsync()
    {
        var ok = await _authService.TryRestoreSessionAsync();
        if (!ok) IsAuthenticated = false;
    }
}
