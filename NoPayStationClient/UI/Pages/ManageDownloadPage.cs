using NewPayStation.Client.Models;
using NewPayStation.Client.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI.Pages;

public class ManageDownloadPage : BasePage
{
    private readonly string _downloadId;
    private readonly DownloadManager _downloadManager;

    public ManageDownloadPage(DownloadTask download, DownloadManager downloadManager)
    {
        _downloadId = download.Id;
        _downloadManager = downloadManager;
    }

    public override IRenderable Render()
    {
        // Get fresh download data
        var _download = _downloadManager.GetDownload(_downloadId);

        // If download no longer exists, show error
        if (_download == null)
        {
            return new Rows(
                CreateHeader("Manage Download"),
                new Markup("[red]Download not found[/]"),
                new Markup("[grey]ESC: Back[/]")
            );
        }

        var components = new List<IRenderable>
        {
            // Header
            CreateHeader("Manage Download")
        };

        // Build detail rows
        var detailRows = new List<IRenderable>
        {
            new Markup($"[yellow]File:[/] {_download.FileName}"),
            new Markup($"[yellow]Package:[/] {_download.PackageName}"),
            new Markup($"[yellow]Status:[/] {GetStatusMarkup(_download.Status)}"),
            new Markup($"[yellow]Progress:[/] {_download.PercentComplete:F1}%"),
            new Markup($"[yellow]Downloaded:[/] {_download.GetFormattedSize(_download.DownloadedBytes)} / {_download.GetFormattedSize(_download.TotalBytes)}"),
            new Markup($"[yellow]Speed:[/] {_download.GetFormattedSpeed()}"),
            new Markup($"[yellow]Elapsed:[/] {_download.Elapsed:hh\\:mm\\:ss}"),
            new Markup($"[yellow]ETA:[/] {GetEtaString(_download)} ({_download.DlSamplesAmount} samples)")
        };

        // Add error/cancellation details if applicable
        if (_download.Status == DownloadStatus.Failed && !string.IsNullOrEmpty(_download.Error))
        {
            detailRows.Add(new Text("")); // Empty line
            detailRows.Add(new Markup($"[red]Error:[/] {_download.Error}"));
        }
        else if (_download.Status == DownloadStatus.Cancelled)
        {
            detailRows.Add(new Text("")); // Empty line
            detailRows.Add(new Markup($"[grey]Reason:[/] Download was cancelled by user"));
        }

        // Download details panel
        var panel = new Panel(new Rows(detailRows))
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };

        components.Add(panel);

        // Progress bar
        if (_download.TotalBytes > 0)
        {
            var percent = _download.PercentComplete;
            var barLength = 50;
            var filledLength = (int)(barLength * percent / 100);
            var bar = new string('█', filledLength) + new string('░', barLength - filledLength);

            components.Add(new Panel(
                new Markup($"[green]{bar}[/] {percent:F1}%"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            });
        }

        // Action instructions
        var actions = new List<string>();

        if (_download.Status == DownloadStatus.Downloading)
        {
            actions.Add("[yellow]P[/]: Pause");
        }
        else if (_download.Status == DownloadStatus.Paused)
        {
            actions.Add("[green]R[/]: Resume");
        }

        if (_download.Status != DownloadStatus.Completed &&
            _download.Status != DownloadStatus.Failed &&
            _download.Status != DownloadStatus.Cancelled)
        {
            actions.Add("[red]C[/]: Cancel");
        }

        if (_download.Status == DownloadStatus.Failed || _download.Status == DownloadStatus.Cancelled)
        {
            actions.Add("[blue]D[/]: Delete from history");
        }

        actions.Add("[grey]ESC[/]: Back");

        components.Add(new Markup(string.Join(" | ", actions)));

        return new Rows(components);
    }

    public override async Task<bool> HandleInputAsync(ConsoleKeyInfo? key)
    {
        if (key == null) return false;

        // Get fresh download data
        var download = _downloadManager.GetDownload(_downloadId);
        if (download == null) return true; // Exit if download no longer exists

        switch (key.Value.Key)
        {
            case ConsoleKey.Escape:
                return true; // Exit manage page

            case ConsoleKey.P:
                if (download.Status == DownloadStatus.Downloading)
                {
                    _downloadManager.PauseDownload(_downloadId);
                    await Task.Delay(500); // Give user visual feedback
                }
                return false;

            case ConsoleKey.R:
                if (download.Status == DownloadStatus.Paused)
                {
                    await _downloadManager.ResumeDownload(_downloadId);
                    await Task.Delay(500); // Give user visual feedback
                }
                return false;

            case ConsoleKey.C:
                if (download.Status != DownloadStatus.Completed &&
                    download.Status != DownloadStatus.Failed &&
                    download.Status != DownloadStatus.Cancelled)
                {
                    // Show confirmation without breaking Live display
                    // For now, just cancel directly
                    _downloadManager.CancelDownload(_downloadId);
                    await Task.Delay(500); // Give user visual feedback
                    return true; // Exit after canceling
                }
                return false;

            case ConsoleKey.D:
                if (download.Status == DownloadStatus.Failed || download.Status == DownloadStatus.Cancelled)
                {
                    _downloadManager.DeleteDownload(_downloadId);
                    await Task.Delay(300); // Give user visual feedback
                    return true; // Exit after deleting
                }
                return false;

            default:
                return false;
        }
    }

    private string GetStatusMarkup(DownloadStatus status)
    {
        return status switch
        {
            DownloadStatus.Downloading => "[green]Downloading[/]",
            DownloadStatus.Paused => "[yellow]Paused[/]",
            DownloadStatus.Queued => "[blue]Queued[/]",
            DownloadStatus.Completed => "[green]Completed[/]",
            DownloadStatus.Failed => "[red]Failed[/]",
            DownloadStatus.Cancelled => "[grey]Cancelled[/]",
            _ => "[grey]Unknown[/]"
        };
    }

    private string GetEtaString(DownloadTask download)
    {
        if (download.Status != DownloadStatus.Downloading)
            return "N/A";

        var eta = download.EstimatedTimeRemaining;
        if (eta == null || eta.Value.TotalSeconds < 1)
            return "Calculating...";

        if (eta.Value.TotalHours >= 1)
            return $"{eta.Value:hh\\:mm\\:ss}";
        else if (eta.Value.TotalMinutes >= 1)
            return $"{eta.Value:mm\\:ss}";
        else
            return $"{eta.Value.TotalSeconds:F0}s";
    }

    public override int RefreshIntervalMs => 100; // Fast refresh for live download stats
}
