// Caminho do arquivo: Engeradios.Desktop/Helpers/SecurityHelper.cs

using System;
using System.Security.Cryptography;
using System.Text;

namespace Engeradios.Desktop.Helpers
{
    public static class SecurityHelper
    {
        public static string GerarHashSenha(string senhaPlana)
        {
            if (string.IsNullOrEmpty(senhaPlana)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytesSenha = Encoding.UTF8.GetBytes(senhaPlana);
                byte[] hashBytes = sha256.ComputeHash(bytesSenha);
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}