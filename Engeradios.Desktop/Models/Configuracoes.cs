// Caminho do arquivo: Engeradios.Desktop/Models/Configuracoes.cs
using System.Collections.Generic;

namespace Engeradios.Desktop.Models
{
    public class ConfiguracoesGerais
    {
        // Limite de espaço em disco em Gigabytes (Padrão 40)
        public int LimiteEspacoDiscoGB { get; set; } = 40;
    }

    public class CanalHardwareConfig
    {
        public string Nome { get; set; } = string.Empty;
        public int PlacaIndex { get; set; } = 0;
        public float VAD { get; set; } = 0.05f;
        public float Volume { get; set; } = 1.0f;
    }

    public class AppConfigRoot
    {
        public ConfiguracoesGerais Gerais { get; set; } = new();
        public List<CanalHardwareConfig> Canais { get; set; } = new();
    }
}