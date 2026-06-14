// Caminho do arquivo: Engeradios.Desktop/Helpers/ConfigSecurity.cs

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Engeradios.Desktop.Helpers
{
    public static class ConfigSecurity
    {
        // Define primeiro a pasta, e depois o caminho completo do ficheiro
        private static readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Engeradios");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.dat");

        public static void SalvarApiKey(string apiKey)
        {
            try
            {
                // CRÍTICO: Criar a pasta caso ela não exista no PC do cliente
                if (!Directory.Exists(ConfigDir))
                {
                    Directory.CreateDirectory(ConfigDir);
                }

                byte[] data = Encoding.UTF8.GetBytes(apiKey);

                // O pragma desativa os avisos irritantes do Visual Studio sobre "código exclusivo para Windows"
#pragma warning disable CA1416
                byte[] protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416

                File.WriteAllBytes(ConfigPath, protectedData);
            }
            catch (Exception ex)
            {
                // Evita que a aplicação rebente se não tiver permissões
                Console.WriteLine($"Falha ao guardar configuração: {ex.Message}");
            }
        }

        public static string ObterApiKey()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return string.Empty;

                byte[] protectedData = File.ReadAllBytes(ConfigPath);

#pragma warning disable CA1416
                byte[] data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
#pragma warning restore CA1416

                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Falha ao ler configuração: {ex.Message}");
                return string.Empty; // Retorna vazio se o ficheiro estiver corrompido
            }
        }
    }
}