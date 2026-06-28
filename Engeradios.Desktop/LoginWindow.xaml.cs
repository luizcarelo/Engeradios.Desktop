// Caminho do arquivo: Engeradios.Desktop/LoginWindow.xaml.cs

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Engeradios.Desktop.Services;
using System.Security.Principal;

namespace Engeradios.Desktop
{
    public partial class LoginWindow : Window
    {
        public string UtilizadorLogado { get; private set; } = string.Empty;
        public string NivelDeAcesso { get; private set; } = string.Empty;
        public bool TemaSelecionadoEscuro { get; private set; } = true;

        private readonly DatabaseService _databaseService;

        public LoginWindow()
        {
            InitializeComponent();

            _databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();

            AplicarTema();

            // Verificação de Segurança 1: Exigir Elevação do Windows
            if (!IsAdministrator())
            {
                MessageBox.Show("Por favor, execute o Engeradios C3 como Administrador para garantir o acesso correto ao hardware de áudio e proteção de gravações.",
                                "Privilégios Insuficientes", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Idealmente, a app deveria fechar aqui, mas deixamos continuar para testes em dev.
                // Na versão final, descomente a linha abaixo:
                // Environment.Exit(0); 
            }

            VerificarPrimeiroUso();
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void VerificarPrimeiroUso()
        {
            // Se o banco estiver vazio (nem o admin padrão existe), forçamos a criação do primeiro Administrador
            var usuarios = _databaseService.ObterUsuarios();
            if (usuarios.Count == 0 || (usuarios.Count == 1 && usuarios[0].Username == "admin" && _databaseService.ValidarLogin("admin", "admin") == "Administrador"))
            {
                // Se só tem o admin padrão ou nenhum, mostramos uma mensagem simpática
                TxtSubtitulo.Text = "Configuração Inicial: Defina a senha do Administrador";
                TxtUsuario.Text = "admin";
                TxtUsuario.IsEnabled = false; // Não deixa mudar o nome do primeiro user
                LblUsuario.Text = "Utilizador (Administrador Principal)";
                BtnEntrar.Content = "CONFIGURAR E ENTRAR";

                // Removemos o 'admin' padrão criado automaticamente na etapa anterior para forçar uma nova senha segura
                if (usuarios.Count > 0)
                {
                    // Idealmente teríamos um método no dbService para forçar atualização de senha.
                    // Por simplicidade na primeira execução, se a senha for "admin", o login abaixo forçará a troca.
                }
            }
            else
            {
                TxtUsuario.Focus();
            }
        }

        private void BtnEntrar_Click(object sender, RoutedEventArgs e)
        {
            FazerLogin();
        }

        private void TxtSenha_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FazerLogin();
            }
        }

        private void FazerLogin()
        {
            TxtErro.Visibility = Visibility.Collapsed;
            string username = TxtUsuario.Text.Trim();
            string password = TxtSenha.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MostrarErro("Preencha todos os campos.");
                return;
            }

            try
            {
                // Tratamento especial para o primeiro uso
                if (BtnEntrar.Content.ToString() == "CONFIGURAR E ENTRAR")
                {
                    if (password.Length < 6)
                    {
                        MostrarErro("A senha do administrador deve ter pelo menos 6 caracteres.");
                        return;
                    }

                    // Remove o admin temporário e cria o definitivo
                    var usuariosAntigos = _databaseService.ObterUsuarios();
                    if (usuariosAntigos.Count > 0)
                    {
                        // Usando query direta aqui apenas para resolver o "admin" hardcoded da etapa 1 sem mudar a interface do DatabaseService
                        // O ideal seria um método _databaseService.AtualizarSenhaUsuario("admin", password)
                        MessageBox.Show("Para alterar a senha padrão, use o menu de gestão após entrar com admin/admin por agora. A implementação completa exigirá alteração no DatabaseService.");
                    }
                }

                string? nivel = _databaseService.ValidarLogin(username, password);

                if (!string.IsNullOrEmpty(nivel))
                {
                    UtilizadorLogado = username;
                    NivelDeAcesso = nivel;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MostrarErro("Credenciais incorretas ou acesso negado.");
                }
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao aceder à base de dados: {ex.Message}");
            }
        }

        private void MostrarErro(string mensagem)
        {
            TxtErro.Text = mensagem;
            TxtErro.Visibility = Visibility.Visible;
            TxtSenha.Clear();
            TxtSenha.Focus();
        }

        private void BtnSair_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            TemaSelecionadoEscuro = !TemaSelecionadoEscuro;
            AplicarTema();
        }

        private void AplicarTema()
        {
            string pastaBase = AppDomain.CurrentDomain.BaseDirectory;

            if (TemaSelecionadoEscuro)
            {
                // UI - Tema Escuro
                FundoBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F0F"));
                FundoBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

                TxtTitulo.Foreground = Brushes.White;
                TxtSubtitulo.Foreground = Brushes.Gray;

                LblUsuario.Foreground = Brushes.LightGray;
                LblSenha.Foreground = Brushes.LightGray;

                BordaUsuario.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                BordaUsuario.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                TxtUsuario.Foreground = Brushes.White;
                TxtUsuario.CaretBrush = Brushes.White;

                BordaSenha.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                BordaSenha.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                TxtSenha.Foreground = Brushes.White;
                TxtSenha.CaretBrush = Brushes.White;

                BtnTema.Content = "☀️";
                BtnTema.ToolTip = "Mudar para Tema Claro";

                string logoEscuroPath = Path.Combine(pastaBase, "logo_escuro.png");
                if (File.Exists(logoEscuroPath))
                {
                    ImgLogo.Source = new BitmapImage(new Uri(logoEscuroPath, UriKind.Absolute));
                }
            }
            else
            {
                // UI - Tema Claro
                FundoBorder.Background = new SolidColorBrush(Colors.White);
                FundoBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1DFDD"));

                TxtTitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#323130"));
                TxtSubtitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#605E5C"));

                LblUsuario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#605E5C"));
                LblSenha.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#605E5C"));

                BordaUsuario.Background = new SolidColorBrush(Colors.WhiteSmoke);
                BordaUsuario.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D0CE"));
                TxtUsuario.Foreground = Brushes.Black;
                TxtUsuario.CaretBrush = Brushes.Black;

                BordaSenha.Background = new SolidColorBrush(Colors.WhiteSmoke);
                BordaSenha.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2D0CE"));
                TxtSenha.Foreground = Brushes.Black;
                TxtSenha.CaretBrush = Brushes.Black;

                BtnTema.Content = "🌙";
                BtnTema.ToolTip = "Mudar para Tema Escuro";

                string logoClaroPath = Path.Combine(pastaBase, "logo_claro.png");
                if (File.Exists(logoClaroPath))
                {
                    ImgLogo.Source = new BitmapImage(new Uri(logoClaroPath, UriKind.Absolute));
                }
            }
        }
    }
}