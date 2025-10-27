namespace NewPayStation.Client.Models;

public class PS3Package
{
    public string TitleId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PkgDirectLink { get; set; } = string.Empty;
    public string Rap { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public string LastModificationDate { get; set; } = string.Empty;
    public string DownloadRapFile { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string SHA256 { get; set; } = string.Empty;

    public bool HasPkg => !string.IsNullOrWhiteSpace(PkgDirectLink) && PkgDirectLink != "MISSING";
    public bool HasRap => !string.IsNullOrWhiteSpace(Rap) && Rap != "MISSING" && Rap != "NOT REQUIRED";
    public bool RequiresRap => HasRap || Rap != "NOT REQUIRED" && Rap != "MISSING";

    public string GetFormattedSize()
    {
        if (FileSize == 0) return "Unknown";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
