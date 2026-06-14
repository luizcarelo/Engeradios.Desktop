using System;

namespace Engeradios.Desktop.Models
{
    public class LogAuditoria
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Operador { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Severidade { get; set; } = "Info"; // Info, Aviso, Critico
    }
}