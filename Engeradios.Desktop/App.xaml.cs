// Caminho do arquivo: Engeradios.Desktop/App.xaml.cs

using System.Windows;
using Engeradios.Desktop.Helpers;

namespace Engeradios.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // CORREÇÃO CRÍTICA: Diz ao WPF para não fechar a app sozinho quando uma janela fechar.
            // Nós é que vamos controlar o fecho manualmente nesta fase de arranque.
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Verifica se a API KEY (Servidor) está configurada
            string key = ConfigSecurity.ObterApiKey();
            if (string.IsNullOrEmpty(key))
            {
                SetupWindow setup = new SetupWindow();
                if (setup.ShowDialog() != true)
                {
                    // Se cancelou o setup, forçamos o fecho
                    Application.Current.Shutdown();
                    return;
                }
            }

            // 2. Abre a Janela de Login
            LoginWindow login = new LoginWindow();
            bool? loginSucesso = login.ShowDialog();

            // 3. Se o login for bem sucedido, abre a MainWindow
            if (loginSucesso == true)
            {
                // Agora que o login passou, voltamos a entregar o controlo ao WPF.
                // Dizemos: "A partir de agora, a aplicação só fecha quando a MainWindow fechar".
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Injeta o utilizador real e o seu nível de acesso no Dashboard!
                MainWindow mainWindow = new MainWindow(login.UtilizadorLogado, login.NivelDeAcesso, true);
                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                // Se fechou a janela de login sem entrar, encerra a aplicação
                Application.Current.Shutdown();
            }
        }
    }
}