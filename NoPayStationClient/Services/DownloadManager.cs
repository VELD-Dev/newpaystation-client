using NewPayStation.Client.Models;
using System.Collections.Concurrent;

namespace NewPayStation.Client.Services;

public class DownloadManager
{
    private readonly ConcurrentDictionary<string, DownloadTask> _downloads = new();
    private readonly DownloadService _downloadService;
    private static readonly Lock _lock = new();

    public DownloadManager(DownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    public List<DownloadTask> GetActiveDownloads()
    {
        lock (_lock)
        {
            return _downloads.Values
                .Where(d => d.Status == DownloadStatus.Queued ||
                           d.Status == DownloadStatus.Downloading ||
                           d.Status == DownloadStatus.Paused)
                .OrderBy(d => d.StartTime)
                .ToList();
        }
    }

    public List<DownloadTask> GetRecentCompletedDownloads(int count = 3)
    {
        lock (_lock)
        {
            return _downloads.Values
                .Where(d => d.Status == DownloadStatus.Completed)
                .OrderByDescending(d => d.StartTime)
                .Take(count)
                .ToList();
        }
    }

    public List<DownloadTask> GetFailedAndCancelledDownloads(int count = 3)
    {
        lock(_lock)
        {
            return _downloads.Values
                .Where(d => d.Status == DownloadStatus.Failed || d.Status == DownloadStatus.Cancelled)
                .OrderBy(d => d.StartTime)
                .Take(count)
                .ToList();
        }
    }

    public DownloadTask? GetDownload(string id)
    {
        _downloads.TryGetValue(id, out var task);
        return task;
    }

    public void ClearCompleted()
    {
        lock (_lock)
        {
            var completedIds = _downloads.Values
                .Where(d => d.Status == DownloadStatus.Completed || d.Status == DownloadStatus.Failed)
                .Select(d => d.Id)
                .ToList();

            foreach (var id in completedIds)
            {
                _downloads.TryRemove(id, out _);
            }
        }
    }

    public void DeleteDownload(string id)
    {
        _downloads.TryRemove(id, out _);
    }

    public void PauseDownload(string id)
    {
        if (_downloads.TryGetValue(id, out var task))
        {
            if (task.Status == DownloadStatus.Downloading)
            {
                task.CancellationTokenSource?.Cancel();
                task.Status = DownloadStatus.Paused;
                task.IsPaused = true;
            }
        }
    }

    public void CancelDownload(string id)
    {
        if (_downloads.TryGetValue(id, out var task))
        {
            task.CancellationTokenSource?.Cancel();
            task.Status = DownloadStatus.Cancelled;

            // Clean up partial file
            if (!string.IsNullOrEmpty(task.DownloadPath))
            {
                _downloadService.CleanupPartialFile(task.DownloadPath, task.FileName);
            }
        }
    }

    public Task ResumeDownload(string id)
    {
        if (_downloads.TryGetValue(id, out var task))
        {
            if (task.Status == DownloadStatus.Paused)
            {
                task.IsPaused = false;
                task.CancellationTokenSource = new CancellationTokenSource();

                _ = DownloadFileAsync(task.Url, task.DownloadPath, task);
            }
        }
        return Task.CompletedTask;
    }

    public Task<string> StartPackageDownload(PS3Package package, string outputPath, string packageName)
    {
        var downloadId = Guid.NewGuid().ToString();

        // Download RAP if available
        if (package.HasRap)
        {
            var rapTask = new DownloadTask
            {
                Id = $"{downloadId}_rap",
                FileName = $"{package.ContentId}.rap",
                PackageName = packageName,
                Status = DownloadStatus.Queued,
                TotalBytes = package.Rap.Length / 2,
                StartTime = DateTime.Now,
                DownloadPath = outputPath
            };

            _downloads[rapTask.Id] = rapTask;
            _ = DownloadRapAsync(package.Rap, outputPath, package.TitleId, rapTask);
        }

        // Download PKG if available
        if (package.HasPkg)
        {
            var pkgTask = new DownloadTask
            {
                Id = $"{downloadId}_pkg",
                FileName = $"{package.TitleId}.pkg",
                PackageName = packageName,
                Status = DownloadStatus.Queued,
                TotalBytes = package.FileSize,
                StartTime = DateTime.Now,
                Url = package.PkgDirectLink,
                DownloadPath = outputPath,
                CancellationTokenSource = new CancellationTokenSource()
            };

            _downloads[pkgTask.Id] = pkgTask;
            _ = DownloadFileAsync(package.PkgDirectLink, outputPath, pkgTask);
        }

        return Task.FromResult(downloadId);
    }

    public Task<string> StartRapDownload(PS3Package package, string outputPath, string packageName)
    {
        var downloadId = Guid.NewGuid().ToString();
        if (package.HasRap)
        {
            var rapTask = new DownloadTask
            {
                Id = downloadId,
                FileName = $"{package.ContentId}.rap",
                PackageName = packageName,
                Status = DownloadStatus.Queued,
                TotalBytes = package.Rap.Length / 2,
                StartTime = DateTime.Now,
                DownloadPath = outputPath
            };
            _downloads[rapTask.Id] = rapTask;
            _ = DownloadRapAsync(package.Rap, outputPath, package.ContentId, rapTask);
        }
        return Task.FromResult(downloadId);
    }

    public Task<string> StartPkgDownload(PS3Package package, string outputPath, string packageName)
    {
        var downloadId = Guid.NewGuid().ToString();
        if(package.HasPkg)
        {
            var pkgTask = new DownloadTask
            {
                Id = downloadId,
                FileName = $"{package.TitleId}.pkg",
                PackageName = packageName,
                Status = DownloadStatus.Queued,
                TotalBytes = package.FileSize,
                StartTime = DateTime.Now,
                Url = package.PkgDirectLink,
                DownloadPath = outputPath,
                CancellationTokenSource = new CancellationTokenSource()
            };
            _downloads[pkgTask.Id] = pkgTask;
            _ = DownloadFileAsync(package.PkgDirectLink, outputPath, pkgTask);
        }

        return Task.FromResult(downloadId);
    }

    private async Task<DownloadTask> DownloadRapAsync(string rapHex, string outputPath, string contentId, DownloadTask task)
    {
        try
        {
            task.Status = DownloadStatus.Downloading;
            var path = await _downloadService.DownloadRapFromHexAsync(rapHex, outputPath, contentId);
            task.Status = DownloadStatus.Completed;
            task.DownloadedBytes = task.TotalBytes;
            task.OutputPath = path;
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.Error = ex.Message;
        }

        return task;
    }

    private async Task<DownloadTask> DownloadFileAsync(string url, string outputPath, DownloadTask task)
    {
        try
        {
            task.Status = DownloadStatus.Downloading;

            var path = await _downloadService.DownloadFileWithProgressAsync(
                url,
                outputPath,
                task.FileName,
                (downloaded, total, speed) =>
                {
                    task.DownloadedBytes = downloaded;
                    task.TotalBytes = total;
                    task.Speed = speed;
                },
                task.CancellationTokenSource?.Token ?? CancellationToken.None);

            task.Status = DownloadStatus.Completed;
            task.OutputPath = path;
        }
        catch (OperationCanceledException)
        {
            if (!task.IsPaused && task.Status != DownloadStatus.Cancelled)
            {
                task.Status = DownloadStatus.Cancelled;
            }
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.Error = ex.Message;
        }

        return task;
    }
}
