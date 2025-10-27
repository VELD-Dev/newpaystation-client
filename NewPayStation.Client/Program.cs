using Spectre.Console;
using System.Reflection;

namespace NewPayStation.Client;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Find TSV file
            var tsvPath = FindTsvFile();

            if (tsvPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error: PS3_GAMES.tsv not found![/]");
                AnsiConsole.MarkupLine("Please place the PS3_GAMES.tsv file in:");
                AnsiConsole.MarkupLine($"  - Current directory: {Directory.GetCurrentDirectory()}");
                AnsiConsole.MarkupLine($"  - Or parent directory");
                Console.ReadKey();
                return;
            }

            var app = new Application(tsvPath);
            app.Run();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
            AnsiConsole.WriteException(ex);
            Console.ReadKey();
        }
    }

    static string? FindTsvFile()
    {
        // Check current directory
        var currentDir = Directory.GetCurrentDirectory();
        var tsvPath = Path.Combine(currentDir, "PS3_GAMES.tsv");
        if (File.Exists(tsvPath)) return tsvPath;

        return null;
    }
}
