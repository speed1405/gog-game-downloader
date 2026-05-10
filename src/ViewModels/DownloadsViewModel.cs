using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GogGameDownloader.Models;
using GogGameDownloader.Services.Download;

namespace GogGameDownloader.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;

    public ObservableCollection<DownloadJobViewModel> Jobs { get; } = new();

    [ObservableProperty]
    private bool _isIdle = true;

    public DownloadsViewModel(IDownloadService downloadService)
    {
        _downloadService = downloadService;
        _ = RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task PauseAll()
    {
        var jobs = await _downloadService.GetActiveJobsAsync();
        foreach (var job in jobs)
        {
            if (job.Status is DownloadStatus.Downloading or DownloadStatus.Queued or DownloadStatus.Verifying)
            {
                await _downloadService.PauseAsync(job.Id);
            }
        }

        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task ResumeAll()
    {
        var jobs = await _downloadService.GetActiveJobsAsync();
        foreach (var job in jobs)
        {
            if (job.Status is DownloadStatus.Paused or DownloadStatus.AuthRequired)
            {
                await _downloadService.ResumeAsync(job.Id);
            }
        }

        await RefreshJobsAsync();
    }

    private async Task RefreshJobsAsync()
    {
        var jobs = await _downloadService.GetActiveJobsAsync();

        Jobs.Clear();
        foreach (var job in jobs)
        {
            var total = job.BytesTotal;
            var done = job.BytesDone;
            var progress = total > 0 ? (double)done / total * 100 : 0;
            var title = job.GameVersion?.Game?.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Download #{job.Id}";
            }

            Jobs.Add(new DownloadJobViewModel
            {
                Title = title,
                Progress = progress,
                StatusLabel = job.Status.ToString(),
                SpeedLabel = string.Empty,
                EtaLabel = string.Empty
            });
        }

        IsIdle = Jobs.Count == 0;
    }
}

public partial class DownloadJobViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private string _speedLabel = string.Empty;

    [ObservableProperty]
    private string _etaLabel = string.Empty;
}
