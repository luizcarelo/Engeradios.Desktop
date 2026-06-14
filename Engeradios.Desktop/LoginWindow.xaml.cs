// Caminho do arquivo: Engeradios.Desktop/LoginWindow.xaml.cs

using System.Windows;
using System.Windows.Input;
using Engeradios.Desktop.Services;

namespace Engeradios.Desktop
{
    public partial class LoginWindow : Window
    {
        private DatabaseService _dbService;

        // Propriedades públicas que o App.xaml.cs vai ler se o login for um sucesso
        public string UtilizadorLogado { get; private set; } = string.Empty;
        public string NivelDeAcesso { get; private set; } = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            TxtUsername.Focus();
        }

        private void BtnEntrar_Click(object sender, RoutedEventArgs e)
        {
            FazerLogin();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            // Permite fazer login ao carregar na tecla ENTER
            if (e.Key == Key.Enter)
            {
                FazerLogin();
            }
        }

        private void FazerLogin()
        {
            string user = TxtUsername.Text.Trim();
            string pass = TxtPassword.Password;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MostrarErro("Preencha todos os campos.");
                return;
            }

            // Vai à Base de Dados SQLite verificar as credenciais
            string? nivelAcesso = _dbService.ValidarLogin(user, pass);

            if (nivelAcesso != null)
            {
                // Sucesso!
                UtilizadorLogado = user;
                NivelDeAcesso = nivelAcesso;
                this.DialogResult = true;
                this.Close(); // Fecha a janela de login e avança
            }
            else
            {
                // Falhou!
                MostrarErro("Credenciais inválidas. Tente novamente.");
                TxtPassword.Clear();
            }
        }

        private void MostrarErro(string mensagem)
        {
            MsgErro.Visibility = Visibility.Visible;
            ((System.Windows.Controls.TextBlock)MsgErro.Child).Text = mensagem;
        }

        private void BtnSair_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}