using NoPayStationClient.Models;
using System.Text.Json;

namespace NoPayStationClient.Services
{
    public class SettingsManager
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public SettingsManager()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoPayStationClient");
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            _settings = LoadSettings();
        }

        public AppSettings GetSettings() => _settings;

        private AppSettings LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    return new AppSettings();
                }
            }

            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            _settings = settings;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
    }
}
