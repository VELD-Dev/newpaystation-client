namespace NoPayStationClient.Models
{
    public class AppSettings
    {
        public string DownloadDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "PS3");
        public int MaxConcurrentDownloads { get; set; } = 3;
        public int DownloadSpeedSamplesCount { get; set; } = 32;

        public void EnsureDownloadDirectoryExists()
        {
            if (!Directory.Exists(DownloadDirectory))
            {
                Directory.CreateDirectory(DownloadDirectory);
            }
        }
    }
}
