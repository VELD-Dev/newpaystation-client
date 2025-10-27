using NewPayStation.Client.Models;
using NewPayStation.Client.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI;

public class DownloadStatusBar
{
    private readonly DownloadManager _downloadManager;

    public DownloadStatusBar(DownloadManager downloadManager)
    {
        _downloadManager = downloadManager;
    }

    public void Render()
    {
        var renderable = RenderAsRenderable();
        if (renderable != null)
        {
            AnsiConsole.Write(renderable);
        }
    }

    public IRenderable? RenderAsRenderable()
    {
        var activeDownloads = _downloadManager.GetActiveDownloads();
        if (activeDownloads.Count == 0) return null;

        var table = new Table()
            .Border(TableBorder.Rounded);

        table.AddColumn(new TableColumn("[yellow]File[/]").Width(30));
        table.AddColumn(new TableColumn("[yellow]Progress[/]").Width(50));
        table.AddColumn(new TableColumn("[yellow]Speed[/]").Width(15));
        table.AddColumn(new TableColumn("[yellow]Status[/]").Width(12));

        foreach (var download in activeDownloads.Take(3))
        {
            var statusColor = download.Status switch
            {
                DownloadStatus.Downloading => "green",
                DownloadStatus.Paused => "yellow",
                DownloadStatus.Queued => "blue",
                _ => "grey"
            };

            var fileName = download.FileName.Length > 28 ? download.FileName.Substring(0, 25) + "..." : download.FileName;

            string progressBar = "";
            if (download.TotalBytes > 0)
            {
                var percent = download.PercentComplete;
                var barLength = 30;
                var filledLength = (int)(barLength * percent / 100);
                progressBar = $"[green]{new string('█', filledLength)}[/][grey]{new string('░', barLength - filledLength)}[/] {percent:F1}%";
                progressBar += $"\n{download.GetFormattedSize(download.DownloadedBytes)}/{download.GetFormattedSize(download.TotalBytes)}";
            }
            else
            {
                progressBar = "[grey]Initializing...[/]";
            }

            table.AddRow(
                fileName,
                progressBar,
                download.GetFormattedSpeed(),
                $"[{statusColor}]{download.Status}[/]"
            );
        }

        if (activeDownloads.Count > 3)
        {
            table.Caption = new TableTitle($"[grey]... and {activeDownloads.Count - 3} more downloads[/]");
        }

        return table;
    }
}
