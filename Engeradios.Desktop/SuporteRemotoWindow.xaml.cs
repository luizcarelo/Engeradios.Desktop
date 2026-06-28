// Caminho do arquivo: Engeradios.Desktop/SuporteRemotoWindow.xaml.cs

using System;
using System.Windows;

namespace Engeradios.Desktop
{
    public partial class SuporteRemotoWindow : Window
    {
        private readonly Random _random = new();

        public SuporteRemotoWindow()
        {
            InitializeComponent();
            GerarCredenciais();
        }

        private void GerarCredenciais()
        {
            // Gera um ID de parceiro no formato: 123 456 789
            int part1 = _random.Next(100, 999);
            int part2 = _random.Next(100, 999);
            int part3 = _random.Next(100, 999);
            TxtIdAcesso.Text = $"{part1} {part2} {part3}";

            // Gera uma palavra-passe temporária de 4 dígitos
            TxtSenhaAcesso.Text = _random.Next(1000, 9999).ToString();
        }

        private void BtnGerarNovaSenha_Click(object sender, RoutedEventArgs e)
        {
            // Renova apenas a password
            TxtSenhaAcesso.Text = _random.Next(1000, 9999).ToString();
        }

        private void BtnCopiar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dados = $"Suporte Técnico Engeradios C3\nID de Acesso: {TxtIdAcesso.Text}\nPalavra-Passe: {TxtSenhaAcesso.Text}";
                Clipboard.SetText(dados);
                MessageBox.Show("Credenciais copiadas para a área de transferência!", "Copiar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível copiar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}