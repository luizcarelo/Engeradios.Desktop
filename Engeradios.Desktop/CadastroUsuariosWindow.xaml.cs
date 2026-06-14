using System;
using System.Windows;
using System.Windows.Controls;
using Engeradios.Desktop.Services;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop
{
    public partial class CadastroUsuariosWindow : Window
    {
        private DatabaseService _dbService;
        private LogService _logService;
        private string _adminAtual;

        public CadastroUsuariosWindow(string adminAtual)
        {
            InitializeComponent();

            _dbService = new DatabaseService();
            _logService = new LogService();
            _adminAtual = adminAtual;

            AtualizarLista();
        }

        private void AtualizarLista()
        {
            var usuarios = _dbService.ObterUsuarios();
            GridUsuarios.ItemsSource = usuarios;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            string user = TxtNovoUsername.Text.Trim();
            string pass = TxtNovaPassword.Password;
            string nivel = (CmbNivelAcesso.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Operador";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Por favor, preencha o nome de utilizador e a palavra-passe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool sucesso = _dbService.AdicionarUsuario(user, pass, nivel);

            if (sucesso)
            {
                _logService.Registar(_adminAtual, $"Criou um novo {nivel} chamado '{user}'");
                MessageBox.Show("Utilizador criado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtNovoUsername.Clear();
                TxtNovaPassword.Clear();
                AtualizarLista();
            }
            else
            {
                MessageBox.Show("Já existe um utilizador com esse nome. Escolha outro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnApagar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Usuario usuario)
            {
                if (usuario.Username == "admin")
                {
                    MessageBox.Show("O administrador de sistema 'admin' não pode ser apagado.", "Proteção de Sistema", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                var resposta = MessageBox.Show($"Tem a certeza que deseja remover o acesso de '{usuario.Username}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (resposta == MessageBoxResult.Yes)
                {
                    _dbService.RemoverUsuario(usuario.Id);
                    _logService.Registar(_adminAtual, $"Removeu o utilizador '{usuario.Username}'");
                    AtualizarLista();
                }
            }
        }
    }
}