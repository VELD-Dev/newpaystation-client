using NewPayStation.Client.Models;
using NewPayStation.Client.Services;
using NewPayStation.Client.UI;
using NewPayStation.Client.UI.Pages;
using Spectre.Console;

namespace NewPayStation.Client;

public class Application
{
    private readonly List<PS3Package> _packages;
    private readonly SettingsManager _settingsManager;
    private readonly DownloadService _downloadService;
    private readonly DownloadManager _downloadManager;
    private readonly DownloadStatusBar _downloadStatusBar;

    public Application(string tsvPath)
    {
        List<PS3Package>? packages = null;

        AnsiConsole.Status()
            .Start("Loading PS3 packages...", ctx =>
            {
                packages = TsvParser.ParseTsvFile(tsvPath);
            });

        _packages = packages ?? [];
        _settingsManager = new();
        _downloadService = new();
        _downloadManager = new(_downloadService);
        _downloadStatusBar = new(_downloadManager);

        AnsiConsole.MarkupLine($"[green]✓[/] Loaded {_packages.Count:N0} packages");
        Thread.Sleep(1500);
    }

    public void Run()
    {
        while (true)
        {
            Console.Clear();
            ShowMainMenu();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\n[yellow]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "Browse Packages",
                        "Download Manager",
                        "Settings",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Browse Packages":
                    RunPage(new BrowserPage(_packages, _downloadManager, _settingsManager, _downloadStatusBar));
                    break;

                case "Download Manager":
                    RunPage(new DownloadManagerPage(_downloadManager, _settingsManager));
                    break;

                case "Settings":
                    RunPage(new SettingsPage(_settingsManager, _downloadStatusBar));
                    break;

                case "Exit":
                    return;
            }
        }
    }

    private void ShowMainMenu()
    {
        var rule = new Rule("[cyan]NewPayStation Client[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Show download status if there are active downloads
        _downloadStatusBar.Render();

        // Show package statistics
        var availableCount = _packages.Count(p => p.HasPkg && p.HasRap);
        var brokenCount = _packages.Count - availableCount;

        AnsiConsole.MarkupLine($"[blue]⁂[/] Packages: [yellow]{_packages.Count:N0}[/]");
        AnsiConsole.MarkupLine($"\t[blue]*[/] Available: [green]{availableCount:N0}[/]");
        AnsiConsole.MarkupLine($"\t[blue]*[/] Broken or unavailable: [red]{brokenCount:N0}[/]");
    }

    private void RunPage(IPage page)
    {
        Console.Clear();
        page.OnEnter();

        AutoRefreshDisplay.RunAsync(
            renderFunc: () => page.Render(),
            handleInputAsync: async (key) => await page.HandleInputAsync(key),
            refreshIntervalMs: page.RefreshIntervalMs
        ).Wait();

        page.OnExit();
    }
}
