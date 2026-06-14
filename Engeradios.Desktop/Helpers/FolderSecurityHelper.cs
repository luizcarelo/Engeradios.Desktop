// Caminho do arquivo: Engeradios.Desktop/Helpers/FolderSecurityHelper.cs

using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Engeradios.Desktop.Helpers
{
    /// <summary>
    /// Classe responsável por aplicar regras rigorosas de segurança nas pastas do Windows.
    /// Isso impede que operadores excluam áudios pelo Windows Explorer.
    /// </summary>
    public static class FolderSecurityHelper
    {
        /// <summary>
        /// Aplica bloqueio de Exclusão (Deletar) na pasta especificada.
        /// </summary>
        /// <param name="caminhoPasta">A pasta onde os áudios estão salvos</param>
        public static void ProtegerPastaContraExclusao(string caminhoPasta)
        {
            try
            {
                if (!Directory.Exists(caminhoPasta))
                {
                    Directory.CreateDirectory(caminhoPasta);
                }

                // Pega as regras de segurança atuais da pasta
                DirectoryInfo info = new DirectoryInfo(caminhoPasta);
                DirectorySecurity seguranca = info.GetAccessControl();

                // Identifica o grupo "Todos" ou "Usuários Autenticados" do Windows
                SecurityIdentifier todosUsuarios = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                // Cria uma regra: NEGAR as ações de DELETAR para os usuários comuns.
                // O aplicativo continuará conseguindo gravar porque ele criará arquivos, não deletará.
                // (O serviço de limpeza de disco do próprio app fará isso com permissão elevada)
                FileSystemAccessRule regraNegarExclusao = new FileSystemAccessRule(
                    todosUsuarios,
                    FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Deny);

                // Aplica a regra
                seguranca.AddAccessRule(regraNegarExclusao);
                info.SetAccessControl(seguranca);

                Console.WriteLine($"Pasta {caminhoPasta} agora está blindada contra exclusões manuais.");
            }
            catch (Exception ex)
            {
                // ATENÇÃO: Para alterar segurança de pasta, o aplicativo precisa
                // ser executado como Administrador no Windows!
                Console.WriteLine($"Erro ao proteger pasta. O app está como Administrador? Erro: {ex.Message}");
            }
        }
    }
}