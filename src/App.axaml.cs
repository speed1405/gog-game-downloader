using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GogGameDownloader.Data;
using GogGameDownloader.Services.Auth;
using GogGameDownloader.Services.Download;
using GogGameDownloader.Services.Library;
using GogGameDownloader.Services.Settings;
using GogGameDownloader.Services.Storage;
using GogGameDownloader.ViewModels;
using GogGameDownloader.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GogGameDownloader;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public static IServiceProvider Services => ((App)Current!).GetServices();

    private IServiceProvider GetServices() => _serviceProvider ?? throw new InvalidOperationException("Services not initialized");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        InitializeDatabase();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };

            var authVm = _serviceProvider.GetRequiredService<AuthViewModel>();
            _ = authVm.TryRestoreSessionAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GogGameDownloader",
            "app.db");

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ISecureTokenStore, SecureTokenStore>();
        services.AddSingleton<IAuthService, OAuthPkceAuthService>();
        services.AddSingleton<IGameLibraryService, GogGameLibraryService>();
        services.AddTransient<IDownloadService, DownloadService>();
        services.AddTransient<ISettingsRepository, SettingsRepository>();
        services.AddTransient<IStorageService, StorageService>();

        services.AddTransient<LibraryViewModel>();
        services.AddTransient<DownloadsViewModel>();
        services.AddTransient<StorageViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }

    private void InitializeDatabase()
    {
        using var scope = _serviceProvider!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }
}
