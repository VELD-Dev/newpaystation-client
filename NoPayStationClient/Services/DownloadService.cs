using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

namespace NewPayStation.Client.Services;

public class DownloadService
{
    private readonly HttpClient _httpClient;

    public DownloadService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromHours(2)
        };
    }

    public async Task<string> DownloadFileWithProgressAsync(
        string url,
        string outputPath,
        string fileName,
        Action<long, long, double> progressCallback,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(outputPath, fileName);
        var tempPath = fullPath + ".download";

        long existingLength = 0;
        if (File.Exists(tempPath))
        {
            existingLength = new FileInfo(tempPath).Length;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingLength > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = existingLength + (response.Content.Headers.ContentLength ?? 0);

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(tempPath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = existingLength;
            var stopwatch = Stopwatch.StartNew();
            var lastUpdate = DateTime.Now;
            long lastBytes = totalRead;

            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                // Calculate speed every 500ms
                var now = DateTime.Now;
                var elapsed = (now - lastUpdate).TotalSeconds;
                if (elapsed >= 0.5)
                {
                    var speed = (totalRead - lastBytes) / elapsed;
                    progressCallback(totalRead, totalBytes, speed);
                    lastUpdate = now;
                    lastBytes = totalRead;
                }
            }

            contentStream.Close();
            fileStream.Close();
            stopwatch.Stop();

            // Final update
            progressCallback(totalRead, totalBytes, 0);

            // Move temp file to final location
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            File.Move(tempPath, fullPath);

            return fullPath;
        }
        catch (OperationCanceledException)
        {
            // Keep the partial file for resume
            throw;
        }
        catch (Exception)
        {
            // Clean up on other errors
            if (File.Exists(tempPath) && existingLength == 0)
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }

    public async Task<string> DownloadRapFromHexAsync(string rapHex, string outputPath, string contentId)
    {
        var fileName = $"{contentId}.rap";
        var fullPath = Path.Combine(outputPath, fileName);

        try
        {
            // Convert hex string to bytes
            var bytes = Convert.FromHexString(rapHex);
            await File.WriteAllBytesAsync(fullPath, bytes);
            return fullPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating RAP file: {ex.Message}", ex);
        }
    }

    public void CleanupPartialFile(string outputPath, string fileName)
    {
        var tempPath = Path.Combine(outputPath, fileName + ".partial");
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }
}

// Custom column for displaying downloaded size
public class DownloadedColumn : ProgressColumn
{
    protected override bool NoWrap => true;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var downloaded = FormatBytes((long)task.Value);
        var total = FormatBytes((long)task.MaxValue);
        return new Text($"{downloaded}/{total}");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

// Custom column for displaying transfer speed
public class TransferSpeedColumn : ProgressColumn
{
    private readonly Dictionary<int, SpeedTracker> _trackers = new();

    protected override bool NoWrap => true;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        if (!_trackers.ContainsKey(task.Id))
        {
            _trackers[task.Id] = new SpeedTracker();
        }

        var tracker = _trackers[task.Id];
        var speed = tracker.CalculateSpeed((long)task.Value, deltaTime);

        return new Text($"{FormatSpeed(speed)}");
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
        double len = bytesPerSecond;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private class SpeedTracker
    {
        private long _lastBytes;
        private TimeSpan _elapsed;

        public double CalculateSpeed(long currentBytes, TimeSpan deltaTime)
        {
            _elapsed += deltaTime;

            if (_elapsed.TotalSeconds < 0.5)
                return 0;

            var bytesDelta = currentBytes - _lastBytes;
            var speed = bytesDelta / _elapsed.TotalSeconds;

            _lastBytes = currentBytes;
            _elapsed = TimeSpan.Zero;

            return speed;
        }
    }
}
