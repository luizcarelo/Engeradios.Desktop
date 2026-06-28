// Caminho do arquivo: Engeradios.Desktop/Services/ConfigService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    // Modelo de Dados local que descreve como os canais são guardados pela camada de configuração
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
        private static readonly string AppConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Engeradios", "app_config.json");

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

        // Carrega as configurações gerais (limite de disco, etc.) do ficheiro de configuração da aplicação.
        // Se o ficheiro não existir ou estiver corrompido, devolve os valores por defeito definidos em Models.ConfiguracoesGerais.
        public static ConfiguracoesGerais CarregarConfiguracoesGerais()
        {
            if (!File.Exists(AppConfigPath)) return new ConfiguracoesGerais();

            try
            {
                string json = File.ReadAllText(AppConfigPath);
                var root = JsonSerializer.Deserialize<AppConfigRoot>(json);
                return root?.Gerais ?? new ConfiguracoesGerais();
            }
            catch
            {
                return new ConfiguracoesGerais();
            }
        }

        // Guarda as configurações gerais numa estrutura AppConfigRoot. Mantém a lista de canais atual ao gravar.
        public static void SalvarConfiguracoesGerais(ConfiguracoesGerais gerais)
        {
            string dir = Path.GetDirectoryName(AppConfigPath) ?? string.Empty;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var root = new AppConfigRoot { Gerais = gerais, Canais = CarregarCanais().ConvertAll(c => new Models.CanalHardwareConfig { Nome = c.Nome, PlacaIndex = c.PlacaIndex, VAD = c.VAD, Volume = c.Volume }) };

            string json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppConfigPath, json);
        }
    }
}
