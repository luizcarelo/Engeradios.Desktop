using System;

namespace Engeradios.Desktop.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string NivelAcesso { get; set; } = string.Empty;
    }
}