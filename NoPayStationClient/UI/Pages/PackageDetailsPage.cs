using NewPayStation.Client.Models;
using NewPayStation.Client.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI.Pages;

public class PackageDetailsPage : BasePage
{
    private readonly PS3Package _package;
    private readonly DownloadManager _downloadManager;
    private readonly SettingsManager _settingsManager;
    private readonly DownloadStatusBar _downloadStatusBar;

    public PackageDetailsPage(
        PS3Package package,
        DownloadManager downloadManager,
        SettingsManager settingsManager,
        DownloadStatusBar downloadStatusBar)
    {
        _package = package;
        _downloadManager = downloadManager;
        _settingsManager = settingsManager;
        _downloadStatusBar = downloadStatusBar;
    }

    public override IRenderable Render()
    {
        var components = new List<IRenderable>();

        // Header
        components.Add(CreateHeader("Package Details"));

        // Package details panel
        var panel = new Panel(
            new Rows(
                new Markup($"[yellow]Title ID:[/] {_package.TitleId}"),
                new Markup($"[yellow]Region:[/] {_package.Region}"),
                new Markup($"[yellow]Name:[/] {_package.Name}"),
                new Markup($"[yellow]Content ID:[/] {_package.ContentId}"),
                new Markup($"[yellow]File Size:[/] {_package.GetFormattedSize()}"),
                new Markup($"[yellow]PKG Available:[/] {(_package.HasPkg ? "[green]Yes[/]" : "[red]No[/]")}"),
                new Markup($"[yellow]RAP Required:[/] {(_package.RequiresRap ? _package.HasRap ? "[green]Yes (Available)[/]" : "[red]Yes (Missing)[/]" : "[grey]No[/]")}"),
                new Markup($"[yellow]Last Modified:[/] {_package.LastModificationDate}"),
                new Markup($"[yellow]SHA256:[/] {(string.IsNullOrWhiteSpace(_package.SHA256) ? "[grey]N/A[/]" : _package.SHA256)}")
            ))
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(2, 1)
        };

        components.Add(panel);

        // Download status bar
        var downloadStatus = _downloadStatusBar.RenderAsRenderable();
        if (downloadStatus != null)
        {
            components.Add(downloadStatus);
        }

        // Instructions
        var instructions = _package.HasPkg && _package.HasRap
            ? "[grey]ENTER: Download PKG + RAP | 1: Download PKG only | 2: Download RAP only | ESC: Back[/]"
            : !_package.HasPkg && _package.HasRap
            ? "[grey]ENTER or 2: Download RAP only | ESC: Back[/]"
            : _package.HasPkg && !_package.HasRap
            ? "[grey]ENTER or 1: Download PKG only | ESC: Back[/]"
            : "[grey]ESC: Back[/]";

        components.Add(new Markup(instructions));

        return new Rows(components);
    }

    public override async Task<bool> HandleInputAsync(ConsoleKeyInfo? key)
    {
        if (key == null) return false;

        switch (key.Value.Key)
        {
            case ConsoleKey.Escape:
                return true;

            case ConsoleKey.D1:
                if (_package.HasPkg)
                {
                    await StartPkgDownload();
                }
                return false;

            case ConsoleKey.D2:
                if (_package.HasRap)
                {
                    await StartRapDownload();
                }
                return false;

            case ConsoleKey.Enter:
                if (_package.HasPkg)
                {
                    await StartDownload();
                }
                return false;

            default:
                return false;
        }
    }

    private async Task StartDownload()
    {
        var settings = _settingsManager.GetSettings();
        settings.EnsureDownloadDirectoryExists();

        await _downloadManager.StartPackageDownload(_package, settings.DownloadDirectory, _package.Name);

        // Give user feedback
        await Task.Delay(500);
    }

    private async Task StartPkgDownload()
    {
        var settings = _settingsManager.GetSettings();
        settings.EnsureDownloadDirectoryExists();
        await _downloadManager.StartPkgDownload(_package, settings.DownloadDirectory, _package.Name);
        // Give user feedback
        await Task.Delay(500);
    }

    private async Task StartRapDownload()
    {
        var settings = _settingsManager.GetSettings();
        settings.EnsureDownloadDirectoryExists();
        await _downloadManager.StartRapDownload(_package, settings.DownloadDirectory, _package.Name);
        // Give user feedback
        await Task.Delay(500);
    }

    public override int RefreshIntervalMs => 100; // Faster refresh to show download started feedback
}
