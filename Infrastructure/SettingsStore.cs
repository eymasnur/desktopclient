using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Desktop_client_api_kod.Models;

namespace Desktop_client_api_kod.Infrastructure
{
    public sealed class SettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        private readonly string _settingsPath;

        public SettingsStore()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Klearis", "DesktopClient");
            Directory.CreateDirectory(dir);
            _settingsPath = Path.Combine(dir, "settings.json");
        }

        public async Task<Settings> LoadAsync()
        {
            if (!File.Exists(_settingsPath))
            {
                return new Settings();
            }

            await using var stream = File.OpenRead(_settingsPath);
            return await JsonSerializer.DeserializeAsync<Settings>(stream) ?? new Settings();
        }

        public async Task SaveAsync(Settings settings)
        {
            await using var stream = File.Create(_settingsPath);
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
        }
    }
}


