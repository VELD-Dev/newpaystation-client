using NewPayStation.Client.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI.Pages;

public class SettingsPage : BasePage
{
    private readonly SettingsManager _settingsManager;
    private readonly DownloadStatusBar _downloadStatusBar;

    public SettingsPage(SettingsManager settingsManager, DownloadStatusBar downloadStatusBar)
    {
        _settingsManager = settingsManager;
        _downloadStatusBar = downloadStatusBar;
    }

    public override IRenderable Render()
    {
        var components = new List<IRenderable>();

        // Header
        components.Add(CreateHeader("Settings"));

        var settings = _settingsManager.GetSettings();

        var panel = new Panel(
            new Markup($"[yellow]Download Directory:[/]\n{settings.DownloadDirectory}"))
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

        components.Add(new Markup("[grey]C: Change Directory | O: Open Directory | ESC: Back[/]"));

        return new Rows(components);
    }

    public override async Task<bool> HandleInputAsync(ConsoleKeyInfo? key)
    {
        if (key == null) return false;

        switch (key.Value.Key)
        {
            case ConsoleKey.Escape:
                return true;

            case ConsoleKey.C:
                // Change directory - this requires breaking out of Live display temporarily
                // For now, just acknowledge
                return false;

            case ConsoleKey.O:
                var settings = _settingsManager.GetSettings();
                settings.EnsureDownloadDirectoryExists();
                OpenFolder(settings.DownloadDirectory);
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
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = path,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

    public override int RefreshIntervalMs => 500; // Slower refresh for settings page
}
