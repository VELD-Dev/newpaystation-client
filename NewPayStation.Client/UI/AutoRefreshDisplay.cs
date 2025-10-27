using Spectre.Console;
using Spectre.Console.Rendering;

namespace NewPayStation.Client.UI;

/// <summary>
/// Helper class for creating smooth auto-refreshing displays using Spectre.Console's Live
/// </summary>
public static class AutoRefreshDisplay
{
    /// <summary>
    /// Runs a smooth live display that auto-refreshes at the specified interval
    /// </summary>
    public static async Task RunAsync(
        Func<IRenderable> renderFunc,
        Func<ConsoleKeyInfo?, Task<bool>> handleInputAsync,
        int refreshIntervalMs = 500)
    {
        var cts = new CancellationTokenSource();
        var shouldExit = false;
        var renderLock = new object();

        await AnsiConsole.Live(renderFunc())
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                // Background refresh task
                var refreshTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested && !shouldExit)
                    {
                        try
                        {
                            await Task.Delay(refreshIntervalMs, cts.Token);

                            if (!cts.Token.IsCancellationRequested && !shouldExit)
                            {
                                lock (renderLock)
                                {
                                    ctx.UpdateTarget(renderFunc());
                                }
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }, cts.Token);

                // Input handling
                try
                {
                    while (!shouldExit)
                    {
                        ConsoleKeyInfo? key = null;

                        // Non-blocking key check
                        if (Console.KeyAvailable)
                        {
                            key = Console.ReadKey(intercept: true);
                        }

                        var exitRequested = await handleInputAsync(key);
                        if (exitRequested)
                        {
                            shouldExit = true;
                            break;
                        }

                        if (!Console.KeyAvailable)
                        {
                            await Task.Delay(50);
                        }
                    }
                }
                finally
                {
                    cts.Cancel();
                    try
                    {
                        await refreshTask;
                    }
                    catch { }
                }
            });
    }
}
