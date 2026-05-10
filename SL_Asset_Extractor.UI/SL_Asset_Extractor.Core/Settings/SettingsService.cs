using Newtonsoft.Json;
using SL_Asset_Extractor.Core.Models;

namespace SL_Asset_Extractor.Core.Settings
{
    public class SettingsService
    {
        private readonly string _settingsPath;

        public SettingsService(string settingsPath)
        {
            _settingsPath = settingsPath;
        }

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
    }
}