// Caminho do arquivo: Engeradios.Desktop/App.xaml.cs

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Engeradios.Desktop.Helpers;
using Engeradios.Desktop.Services;
using Engeradios.Desktop.ViewModels;

namespace Engeradios.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Configuração do Contêiner de Injeção de Dependências
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 2. Verificação de Nuvem
            string key = ConfigSecurity.ObterApiKey();
            if (string.IsNullOrEmpty(key))
            {
                SetupWindow setup = new SetupWindow();
                if (setup.ShowDialog() != true)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            // 3. Autenticação
            LoginWindow login = new LoginWindow();
            bool? loginSucesso = login.ShowDialog();

            if (loginSucesso == true)
            {
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // 4. Inicializa a MainWindow via Injeção de Dependência
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

                // Injeta o contexto de utilizador na ViewModel
                var viewModel = (MainViewModel)mainWindow.DataContext;
                viewModel.InicializarSessao(login.UtilizadorLogado, login.NivelDeAcesso, true);

                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Registo de Serviços (Singletons - partilhados por toda a app)
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<LogService>();
            services.AddSingleton<TelemetryClient>();
            services.AddSingleton<CloudSyncService>();

            // Registo de ViewModels
            services.AddTransient<MainViewModel>();

            // Registo de Windows
            services.AddTransient<MainWindow>();
        }
    }
}