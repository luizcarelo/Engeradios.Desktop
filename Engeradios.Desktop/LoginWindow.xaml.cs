using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Engeradios.Desktop
{
    public partial class LoginWindow : Window
    {
        private bool _isTemaEscuro = true;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            _isTemaEscuro = !_isTemaEscuro;

            if (_isTemaEscuro)
            {
                JanelaPrincipal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                LblUsuario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                LblSenha.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));

                TxtUsuario.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                TxtUsuario.Foreground = Brushes.White;
                TxtSenha.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                TxtSenha.Foreground = Brushes.White;

                BtnTema.Content = "☀️ Tema Claro";
                BtnTema.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                BtnSair.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"));

                // Corrigido para caminho relativo
                ImgLogo.Source = new BitmapImage(new Uri("logo_escuro.png", UriKind.Relative));
            }
            else
            {
                JanelaPrincipal.Background = new SolidColorBrush(Colors.WhiteSmoke);
                LblUsuario.Foreground = new SolidColorBrush(Colors.Black);
                LblSenha.Foreground = new SolidColorBrush(Colors.Black);

                TxtUsuario.Background = new SolidColorBrush(Colors.White);
                TxtUsuario.Foreground = Brushes.Black;
                TxtSenha.Background = new SolidColorBrush(Colors.White);
                TxtSenha.Foreground = Brushes.Black;

                BtnTema.Content = "🌙 Tema Escuro";
                BtnTema.Foreground = new SolidColorBrush(Colors.DimGray);
                BtnSair.Foreground = new SolidColorBrush(Colors.DimGray);

                // Corrigido para caminho relativo
                ImgLogo.Source = new BitmapImage(new Uri("logo_claro.png", UriKind.Relative));
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string utilizador = TxtUsuario.Text.ToLower().Trim();
            string senha = TxtSenha.Password;

            if (utilizador == "admin" && senha == "admin")
            {
                AbrirSistema("Luiz Carlos (Admin)", "Administrador");
            }
            else if (utilizador == "operador" && senha == "123")
            {
                AbrirSistema("João Silva", "Operador");
            }
            else
            {
                TxtErro.Visibility = Visibility.Visible;
            }
        }

        private void AbrirSistema(string nome, string nivel)
        {
            MainWindow janelaPrincipal = new MainWindow(nome, nivel, _isTemaEscuro);
            janelaPrincipal.Show();
            this.Close();
        }

        private void BtnSair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}