using System.Threading.Tasks;

namespace GogGameDownloader.Services.Notification;

public interface INotificationService
{
    Task ShowAsync(string title, string message, NotificationKind kind = NotificationKind.Info);
}

public enum NotificationKind
{
    Info,
    Success,
    Warning,
    Error
}
