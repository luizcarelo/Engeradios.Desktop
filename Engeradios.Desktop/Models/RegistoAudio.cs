using System;

namespace Engeradios.Desktop.Models
{
    public class RegistoAudio
    {
        public int Id { get; set; }

        // O "string.Empty" garante ao .NET 10 que isto nunca será nulo
        public string Canal { get; set; } = string.Empty;

        public DateTime DataHoraGravacao { get; set; }

        public string CaminhoFicheiro { get; set; } = string.Empty;

        public int DuracaoSegundos { get; set; }

        public string Anotacoes { get; set; } = string.Empty;

        public bool SincronizadoComNuvem { get; set; }

        // NOVO: Indica se o ficheiro está marcado para não ser apagado automaticamente
        public bool Protegido { get; set; }
    }
}