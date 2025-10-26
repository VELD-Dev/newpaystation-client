using System.Diagnostics;

namespace NoPayStationClient.Models
{
    public class DownloadTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        private DownloadStatus _status = DownloadStatus.Queued;
        public DownloadStatus Status
        { 
            get => _status;
            set
            {
                if(value == DownloadStatus.Paused)
                {
                }
                _status = value;
            } 
        }
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public double Speed { get; set; } // bytes per second
        public string? Error { get; set; }
        public DateTime StartTime { get; set; }
        public string? OutputPath { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        public bool IsPaused { get; set; }
        public string Url { get; set; } = string.Empty;
        public string DownloadPath { get; set; } = string.Empty;

        public double PercentComplete => TotalBytes > 0 ? (DownloadedBytes / (double)TotalBytes) * 100 : 0;

        public TimeSpan Elapsed => DateTime.Now - StartTime;

        private List<double> _dlSpeedSamples = [];
        private const int MaxSamples = 32;

        public int DlSamplesAmount => Math.Min(_dlSpeedSamples.Count, MaxSamples);

        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                if (Speed <= 0 || TotalBytes <= 0) return null;

                // Add new sample at the beginning
                _dlSpeedSamples.Insert(0, Speed);

                // Keep only the most recent samples
                if (_dlSpeedSamples.Count > MaxSamples)
                {
                    _dlSpeedSamples.RemoveRange(MaxSamples, _dlSpeedSamples.Count - MaxSamples);
                }

                var remainingBytes = TotalBytes - DownloadedBytes;
                var averageSpeed = _dlSpeedSamples.Average();

                return TimeSpan.FromSeconds(remainingBytes / averageSpeed);
            }
        }

        public string GetFormattedSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public string GetFormattedSpeed()
        {
            string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
            double len = Speed;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public enum DownloadStatus
    {
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }
}
