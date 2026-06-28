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
        private bool _modoPrimeiroUso = false;

        public LoginWindow()
        {
            InitializeComponent();

            _databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();

            AplicarTema();

            // Verificação de Segurança 1: Exigir Elevação do Windows
            if (!IsAdministrator())
            {
                MessageBox.Show("Por favor, execute o Engeradios Gravador como Administrador para garantir o acesso correto ao hardware de áudio e proteção de gravações.",
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
            var usuarios = _databaseService.ObterUsuarios();

            bool bancoSemUsuarios = usuarios.Count == 0;

            bool adminPadraoFase1 =
                usuarios.Count == 1 &&
                usuarios[0].Username == "admin" &&
                _databaseService.ValidarLogin("admin", "admin") == "Administrador";

            if (bancoSemUsuarios || adminPadraoFase1)
            {
                _modoPrimeiroUso = true;

                TxtSubtitulo.Text = bancoSemUsuarios
                    ? "Primeira instalação: crie a senha do Administrador"
                    : "Configuração Inicial: altere a senha do Administrador";

                TxtUsuario.Text = "admin";
                TxtUsuario.IsEnabled = false;
                LblUsuario.Text = "Utilizador (Administrador Principal)";
                LblSenha.Text = "Nova senha do administrador";
                BtnEntrar.Content = "CONFIGURAR E ENTRAR";

                LblConfirmarSenha.Visibility = Visibility.Visible;
                BordaConfirmarSenha.Visibility = Visibility.Visible;

                TxtSenha.Clear();
                TxtConfirmarSenha.Clear();
                TxtSenha.Focus();
            }
            else
            {
                _modoPrimeiroUso = false;

                LblConfirmarSenha.Visibility = Visibility.Collapsed;
                BordaConfirmarSenha.Visibility = Visibility.Collapsed;

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
                if (_modoPrimeiroUso)
                {
                    ConfigurarPrimeiroAdministrador(username, password, TxtConfirmarSenha.Password);
                    return;
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
                MostrarErro($"Erro ao acessar o banco de dados: {ex.Message}");
            }
        }

        private void ConfigurarPrimeiroAdministrador(string username, string novaSenha, string confirmacaoSenha)
        {
            if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                MostrarErro("O primeiro usuário deve ser o administrador principal 'admin'.");
                return;
            }

            string? erroSenha = ValidarNovaSenhaAdministrador(username, novaSenha, confirmacaoSenha);
            if (!string.IsNullOrEmpty(erroSenha))
            {
                MostrarErro(erroSenha);
                return;
            }

            var usuarios = _databaseService.ObterUsuarios();

            bool operacaoConcluida;

            if (usuarios.Count == 0)
            {
                operacaoConcluida = _databaseService.CriarPrimeiroAdministrador("admin", novaSenha);
            }
            else if (usuarios.Count == 1 &&
                     usuarios[0].Username == "admin" &&
                     _databaseService.ValidarLogin("admin", "admin") == "Administrador")
            {
                operacaoConcluida = _databaseService.AtualizarSenhaUsuario("admin", novaSenha);
            }
            else
            {
                MostrarErro("A configuração inicial não pode ser executada porque já existem usuários cadastrados.");
                return;
            }

            if (!operacaoConcluida)
            {
                MostrarErro("Não foi possível configurar a senha do administrador.");
                return;
            }

            string? nivel = _databaseService.ValidarLogin("admin", novaSenha);
            if (nivel != "Administrador")
            {
                MostrarErro("Não foi possível validar a nova senha do administrador. Tente novamente.");
                return;
            }

            UtilizadorLogado = "admin";
            NivelDeAcesso = nivel;

            MessageBox.Show(
                "Administrador configurado com sucesso.",
                "Primeira configuração concluída",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }

        private static string? ValidarNovaSenhaAdministrador(string username, string novaSenha, string confirmacaoSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha))
                return "Informe a nova senha do administrador.";

            if (string.IsNullOrWhiteSpace(confirmacaoSenha))
                return "Confirme a nova senha do administrador.";

            if (!string.Equals(novaSenha, confirmacaoSenha, StringComparison.Ordinal))
                return "A confirmação da senha não confere.";

            if (novaSenha.Length < 8)
                return "A senha do administrador deve ter pelo menos 8 caracteres.";

            if (string.Equals(novaSenha, "admin", StringComparison.OrdinalIgnoreCase))
                return "A nova senha não pode ser 'admin'.";

            if (string.Equals(novaSenha, username, StringComparison.OrdinalIgnoreCase))
                return "A nova senha não pode ser igual ao nome do usuário.";

            bool possuiLetra = false;
            bool possuiNumero = false;

            foreach (char c in novaSenha)
            {
                if (char.IsLetter(c)) possuiLetra = true;
                if (char.IsDigit(c)) possuiNumero = true;
            }

            if (!possuiLetra || !possuiNumero)
                return "A senha deve conter pelo menos uma letra e um número.";

            return null;
        }
        private void MostrarErro(string mensagem)
        {
            TxtErro.Text = mensagem;
            TxtErro.Visibility = Visibility.Visible;

            TxtSenha.Clear();

            if (_modoPrimeiroUso)
            {
                TxtConfirmarSenha.Clear();
            }

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