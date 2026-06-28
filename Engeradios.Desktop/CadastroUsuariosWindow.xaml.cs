using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Services;

namespace Engeradios.Desktop
{
    public partial class CadastroUsuariosWindow : Window
    {
        private readonly DatabaseService _dbService;
        private readonly LogService _logService;
        private readonly string _adminAtual;

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
            GridUsuarios.ItemsSource = null;
            GridUsuarios.ItemsSource = _dbService.ObterUsuarios();
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtNovoUsername.Text.Trim();
            string senha = TxtNovaPassword.Password;
            string confirmacaoSenha = TxtConfirmarPassword.Password;
            string nivel = (CmbNivelAcesso.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Operador";

            string? erro = ValidarNovoUsuario(username, senha, confirmacaoSenha, nivel);

            if (!string.IsNullOrEmpty(erro))
            {
                MessageBox.Show(erro, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool sucesso = _dbService.AdicionarUsuario(username, senha, nivel);

            if (!sucesso)
            {
                MessageBox.Show(
                    "Não foi possível criar o usuário. Verifique se já existe um usuário com esse nome.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _logService.Registar(_adminAtual, $"Criou um novo {nivel} chamado '{username}'");

            MessageBox.Show(
                "Usuário criado com sucesso!",
                "Sucesso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            LimparFormulario();
            AtualizarLista();
        }

        private string? ValidarNovoUsuario(string username, string senha, string confirmacaoSenha, string nivel)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Informe o nome do usuário.";

            if (username.Length < 3)
                return "O nome do usuário deve ter pelo menos 3 caracteres.";

            if (username.Length > 30)
                return "O nome do usuário deve ter no máximo 30 caracteres.";

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
                return "O nome do usuário deve conter apenas letras, números, ponto, hífen ou sublinhado.";

            if (_dbService.UsuarioExiste(username))
                return "Já existe um usuário com esse nome. Escolha outro.";

            if (string.IsNullOrWhiteSpace(senha))
                return "Informe a senha do usuário.";

            if (string.IsNullOrWhiteSpace(confirmacaoSenha))
                return "Confirme a senha do usuário.";

            if (!string.Equals(senha, confirmacaoSenha, StringComparison.Ordinal))
                return "A confirmação da senha não confere.";

            if (senha.Length < 8)
                return "A senha deve ter pelo menos 8 caracteres.";

            if (string.Equals(senha, username, StringComparison.OrdinalIgnoreCase))
                return "A senha não pode ser igual ao nome do usuário.";

            bool possuiLetra = false;
            bool possuiNumero = false;

            foreach (char c in senha)
            {
                if (char.IsLetter(c)) possuiLetra = true;
                if (char.IsDigit(c)) possuiNumero = true;
            }

            if (!possuiLetra || !possuiNumero)
                return "A senha deve conter pelo menos uma letra e um número.";

            if (nivel != "Operador" && nivel != "Administrador")
                return "Selecione um nível de acesso válido.";

            return null;
        }

        private void BtnApagar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.CommandParameter is not Usuario usuario)
                return;

            if (string.Equals(usuario.Username, _adminAtual, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Você não pode remover o próprio usuário enquanto está logado.",
                    "Proteção de Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);

                return;
            }

            if (string.Equals(usuario.Username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "O administrador principal 'admin' não pode ser apagado.",
                    "Proteção de Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);

                return;
            }

            if (string.Equals(usuario.NivelAcesso, "Administrador", StringComparison.OrdinalIgnoreCase) &&
                _dbService.ContarAdministradores() <= 1)
            {
                MessageBox.Show(
                    "Não é possível remover o último administrador do sistema.",
                    "Proteção de Sistema",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);

                return;
            }

            var resposta = MessageBox.Show(
                $"Tem certeza que deseja remover o acesso de '{usuario.Username}'?",
                "Confirmar remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resposta != MessageBoxResult.Yes)
                return;

            bool removido = _dbService.RemoverUsuario(usuario.Id);

            if (!removido)
            {
                MessageBox.Show(
                    "Não foi possível remover o usuário. Verifique se ele não é o último administrador.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _logService.Registar(_adminAtual, $"Removeu o usuário '{usuario.Username}'");

            AtualizarLista();
        }

        private void LimparFormulario()
        {
            TxtNovoUsername.Clear();
            TxtNovaPassword.Clear();
            TxtConfirmarPassword.Clear();
            CmbNivelAcesso.SelectedIndex = 0;
            TxtNovoUsername.Focus();
        }
    }
}