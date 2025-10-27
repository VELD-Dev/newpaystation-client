using NewPayStation.Client.Models;

namespace NewPayStation.Client.Services;

public class TsvParser
{
    public static List<PS3Package> ParseTsvFile(string filePath)
    {
        var packages = new List<PS3Package>();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"TSV file not found: {filePath}");
        }

        var lines = File.ReadAllLines(filePath);

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = line.Split('\t');
            if (columns.Length < 10) continue; // Ensure we have all columns

            var package = new PS3Package
            {
                TitleId = columns[0].Trim(),
                Region = columns[1].Trim(),
                Name = columns[2].Trim(),
                PkgDirectLink = columns[3].Trim(),
                Rap = columns[4].Trim(),
                ContentId = columns[5].Trim(),
                LastModificationDate = columns[6].Trim(),
                DownloadRapFile = columns.Length > 7 ? columns[7].Trim() : string.Empty,
                FileSize = long.TryParse(columns[8].Trim(), out long size) ? size : 0,
                SHA256 = columns.Length > 9 ? columns[9].Trim() : string.Empty
            };

            packages.Add(package);
        }

        return packages;
    }
}
