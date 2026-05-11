using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace GogGameDownloader;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            ReportFatalError(ex);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ReportFatalError(ex);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        WriteCrashLog(e.Exception);
    }

    private static void ReportFatalError(Exception ex)
    {
        var logPath = WriteCrashLog(ex);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ShowWindowsMessageBox(
                $"GOG Game Downloader could not start.\n\n{ex.Message}\n\nA crash log has been saved to:\n{logPath}",
                "Startup Error");
    }

    private static string WriteCrashLog(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GogGameDownloader");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            File.AppendAllText(path,
                $"[{DateTime.UtcNow:O}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n\n{ex}\n\n");
            return path;
        }
        catch
        {
            return "(unable to write crash log)";
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ShowWindowsMessageBox(string text, string caption)
    {
        NativeMethods.MessageBox(IntPtr.Zero, text, caption, 0x10 /* MB_ICONERROR */);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [SupportedOSPlatform("windows")]
        internal static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
