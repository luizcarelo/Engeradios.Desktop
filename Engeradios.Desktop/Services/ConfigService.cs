// Caminho do arquivo: Engeradios.Desktop/Services/ConfigService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Engeradios.Desktop.Services
{
    // Modelo de Dados movido para aqui para ser partilhado por toda a aplicação
    public class CanalHardwareConfig
    {
        public string Nome { get; set; } = "Novo Canal";
        public int PlacaIndex { get; set; } = 0;
        public float VAD { get; set; } = 0.05f;
        public float Volume { get; set; } = 1.0f;
    }

    public static class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Engeradios", "hardware_config.json");

        public static void SalvarCanais(List<CanalHardwareConfig> canais)
        {
            string dir = Path.GetDirectoryName(ConfigPath) ?? string.Empty;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(canais, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static List<CanalHardwareConfig> CarregarCanais()
        {
            if (!File.Exists(ConfigPath))
            {
                // Definições de Fábrica caso o ficheiro não exista
                return new List<CanalHardwareConfig>
                {
                    new CanalHardwareConfig { Nome = "Segurança", PlacaIndex = 0, VAD = 0.05f, Volume = 1.0f },
                    new CanalHardwareConfig { Nome = "Manutenção", PlacaIndex = 1, VAD = 0.05f, Volume = 1.0f },
                    new CanalHardwareConfig { Nome = "Brigada", PlacaIndex = 2, VAD = 0.05f, Volume = 1.0f }
                };
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<List<CanalHardwareConfig>>(json) ?? new List<CanalHardwareConfig>();
            }
            catch
            {
                return new List<CanalHardwareConfig>();
            }
        }
    }
}