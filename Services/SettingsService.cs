using System;
using System.IO;
using System.Text.Json;
using TicketeraApp.Models;

namespace TicketeraApp.Services
{
    /// <summary>
    /// Guarda y carga toda la configuración de la aplicación en un archivo JSON
    /// dentro del directorio especial de datos del usuario (AppData/Roaming/TicketeraApp).
    /// </summary>
    public class SettingsService
    {
        private static readonly string SettingsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TicketeraApp");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsFolder, "settings.json");

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    if (loaded != null) return loaded;
                }
            }
            catch
            {
            }

            return AppSettings.CreateDefaults();
        }

        public void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
            }
        }
    }
}
