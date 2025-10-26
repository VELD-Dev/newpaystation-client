using NoPayStationClient.Models;
using NoPayStationClient.Services;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

namespace NoPayStationClient.UI.Pages
{
    public class DownloadManagerPage : BasePage
    {
        private readonly DownloadManager _downloadManager;
        private readonly SettingsManager _settingsManager;
        private IPage? _childPage = null;

        public DownloadManagerPage(DownloadManager downloadManager, SettingsManager settingsManager)
        {
            _downloadManager = downloadManager;
            _settingsManager = settingsManager;
        }

        public override IRenderable Render()
        {
            // Delegate to child page if active
            if (_childPage != null)
            {
                return _childPage.Render();
            }

            var components = new List<IRenderable>
            {
                // Header
                CreateHeader("Download Manager")
            };

            var activeDownloads = _downloadManager.GetActiveDownloads();
            var completedDownloads = _downloadManager.GetRecentCompletedDownloads(10);
            var failedCancelledDownloads = _downloadManager.GetFailedAndCancelledDownloads(5);

            if (activeDownloads.Count == 0 && completedDownloads.Count == 0)
            {
                components.Add(new Markup("[grey]No downloads yet[/]\n\n[grey]Press ESC to go back...[/]"));
                return new Rows(components);
            }

            // Track total download count for numbering
            int downloadNumber = 1;

            // Show active downloads with progress bars
            if (activeDownloads.Count > 0)
            {
                var activeTable = new Table();
                activeTable.Border(TableBorder.Rounded);
                activeTable.Title = new TableTitle("[yellow]Active Downloads[/]");
                activeTable.AddColumn("[yellow]#[/]");
                activeTable.AddColumn(new TableColumn("[yellow]File[/]").Width(25));
                activeTable.AddColumn(new TableColumn("[yellow]Progress[/]").Width(45));
                activeTable.AddColumn(new TableColumn("[yellow]Speed[/]").Width(12));
                activeTable.AddColumn("[yellow]Status[/]");

                for (int i = 0; i < activeDownloads.Count; i++)
                {
                    var download = activeDownloads[i];
                    var statusColor = download.Status switch
                    {
                        DownloadStatus.Downloading => "green",
                        DownloadStatus.Paused => "yellow",
                        DownloadStatus.Queued => "blue",
                        _ => "grey"
                    };

                    var fileName = download.FileName.Length > 23
                        ? string.Concat(download.FileName.AsSpan(0, 20), "...")
                        : download.FileName;

                    string progressDisplay;
                    if (download.TotalBytes > 0)
                    {
                        var percent = download.PercentComplete;
                        var barLength = 20;
                        var filledLength = (int)(barLength * percent / 100);
                        var bar = new string('█', filledLength) + new string('░', barLength - filledLength);
                        progressDisplay = $"[green]{bar}[/] {percent:F1}%\n{download.GetFormattedSize(download.DownloadedBytes)}/{download.GetFormattedSize(download.TotalBytes)}";
                    }
                    else
                    {
                        progressDisplay = "[grey]Initializing...[/]";
                    }

                    activeTable.AddRow(
                        $"[cyan]{downloadNumber++}[/]",
                        fileName,
                        progressDisplay,
                        download.GetFormattedSpeed(),
                        $"[{statusColor}]{download.Status}[/]"
                    );
                }

                components.Add(activeTable);
            }

            // Show completed downloads
            if (completedDownloads.Count > 0)
            {
                var completedTable = new Table();
                completedTable.Border(TableBorder.Rounded);
                completedTable.Title = new TableTitle("[green]Recently Completed[/]");
                completedTable.AddColumn("[yellow]File[/]");
                completedTable.AddColumn("[yellow]Package[/]");
                completedTable.AddColumn("[yellow]Size[/]");
                completedTable.AddColumn("[yellow]Time[/]");

                foreach (var download in completedDownloads)
                {
                    var packageName = download.PackageName.Length > 35
                        ? download.PackageName.Substring(0, 32) + "..."
                        : download.PackageName;

                    completedTable.AddRow(
                        download.FileName,
                        packageName,
                        download.GetFormattedSize(download.TotalBytes),
                        download.Elapsed.ToString(@"hh\:mm\:ss")
                    );
                }

                components.Add(completedTable);
            }

            if(failedCancelledDownloads.Count > 0)
            {
                var cancelledTable = new Table();
                cancelledTable.Border(TableBorder.Rounded);
                cancelledTable.Title = new TableTitle("[red]Aborted[/]");
                cancelledTable.AddColumns([
                    "[yellow]#[/]",
                    "[yellow]File[/]",
                    "[yellow]Package[/]",
                    "[yellow]Status[/]",
                    "[yellow]Size[/]",
                    "[yellow]Time[/]"
                ]);

                foreach (var download in failedCancelledDownloads)
                {
                    var packageName = download.PackageName.Length > 35
                        ? download.PackageName.Substring(0, 32) + "..."
                        : download.PackageName;

                    var statusColor = download.Status == DownloadStatus.Failed ? "red" : "grey";

                    cancelledTable.AddRow(
                        $"[cyan]{downloadNumber++}[/]",
                        download.FileName,
                        packageName,
                        $"[{statusColor}]{download.Status}[/]",
                        download.GetFormattedSize(download.DownloadedBytes) + "/" + download.GetFormattedSize(download.TotalBytes),
                        download.Elapsed.ToString(@"hh\:mm\:ss")
                    );
                }

                components.Add(cancelledTable);
            }

            components.Add(new Markup("[grey]1-9: Manage Download | O: Open Folder | C: Clear Completed | ESC: Back[/]"));

            return new Rows(components);
        }

        public override async Task<bool> HandleInputAsync(ConsoleKeyInfo? key)
        {
            if (key == null) return false;

            // Handle child page input
            if (_childPage != null)
            {
                var shouldExitChild = await _childPage.HandleInputAsync(key);
                if (shouldExitChild)
                {
                    _childPage.OnExit();
                    _childPage = null;
                }
                return false;
            }

            var activeDownloads = _downloadManager.GetActiveDownloads();
            var completedDownloads = _downloadManager.GetRecentCompletedDownloads(10);
            var failedCancelledDownloads = _downloadManager.GetFailedAndCancelledDownloads(5);

            // Combine all manageable downloads
            var allDownloads = new List<DownloadTask>();
            allDownloads.AddRange(activeDownloads);
            allDownloads.AddRange(failedCancelledDownloads);

            switch (key.Value.Key)
            {
                case ConsoleKey.Escape:
                    return true;

                case ConsoleKey.D1:
                case ConsoleKey.D2:
                case ConsoleKey.D3:
                case ConsoleKey.D4:
                case ConsoleKey.D5:
                case ConsoleKey.D6:
                case ConsoleKey.D7:
                case ConsoleKey.D8:
                case ConsoleKey.D9:
                    // Get the number pressed (D1 = 1, D2 = 2, etc.)
                    int selectedNumber = key.Value.Key - ConsoleKey.D0;
                    if (selectedNumber > 0 && selectedNumber <= allDownloads.Count)
                    {
                        var downloadToManage = allDownloads[selectedNumber - 1];
                        _childPage = new ManageDownloadPage(downloadToManage, _downloadManager);
                        _childPage.OnEnter();
                    }
                    return false;

                case ConsoleKey.O:
                    if (completedDownloads.Count > 0)
                    {
                        var settings = _settingsManager.GetSettings();
                        OpenFolder(settings.DownloadDirectory);
                    }
                    return false;

                case ConsoleKey.C:
                    if (completedDownloads.Count > 0)
                    {
                        _downloadManager.ClearCompleted();
                    }
                    return false;

                default:
                    return false;
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = path,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = path,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = path,
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                // Silently fail
            }
        }

        public override int RefreshIntervalMs => 250; // Faster refresh for download progress
    }
}
