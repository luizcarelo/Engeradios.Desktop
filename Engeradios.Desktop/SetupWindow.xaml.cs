// Caminho do arquivo: Engeradios.Desktop/SetupWindow.xaml.cs

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Engeradios.Desktop.Helpers;

namespace Engeradios.Desktop
{
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            InitializeComponent();

            // Carrega a chave atual (se existir) para que o administrador a consiga ver
            TxtApiKey.Text = ConfigSecurity.ObterApiKey();

            // Modifica o texto do botão para fazer sentido se for uma edição
            if (!string.IsNullOrEmpty(TxtApiKey.Text))
            {
                BtnAtivar.Content = "ATUALIZAR CHAVE DE API";
            }
        }

        private async void BtnAtivar_Click(object sender, RoutedEventArgs e)
        {
            string novaChave = TxtApiKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(novaChave))
            {
                MessageBox.Show("Por favor, introduza uma chave válida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Interface de carregamento (feedback visual)
            BtnAtivar.Content = "A TESTAR LIGAÇÃO...";
            BtnAtivar.IsEnabled = false;
            TxtApiKey.IsEnabled = false;

            // Testa a ligação à nuvem antes de guardar!
            bool conexaoOk = await TestarLigacaoNuvemAsync(novaChave);

            // Restaura a interface
            BtnAtivar.IsEnabled = true;
            TxtApiKey.IsEnabled = true;
            BtnAtivar.Content = string.IsNullOrEmpty(ConfigSecurity.ObterApiKey()) ? "ATIVAR ACESSO" : "ATUALIZAR CHAVE DE API";

            if (conexaoOk)
            {
                // Guarda a nova chave de forma encriptada
                ConfigSecurity.SalvarApiKey(novaChave);

                MessageBox.Show("Ligação Estabelecida! O sistema reconheceu a chave e está agora emparelhado com a Nuvem.", "Autenticação Bem Sucedida", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Acesso Negado! A Chave de API foi rejeitada pelo servidor ou não tem ligação à Internet.\n\nVerifique se copiou a chave corretamente do Painel NOC.", "Falha de Comunicação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Faz um "Ping" silencioso à API de Telemetria.
        /// Se o servidor devolver 401 Unauthorized, a chave é falsa. 
        /// Se devolver qualquer outra coisa (como 400 BadRequest por faltar ação), a chave é verdadeira e passou o bloqueio!
        /// </summary>
        private async Task<bool> TestarLigacaoNuvemAsync(string apiKey)
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    // Prepara um payload de teste inofensivo
                    var payload = new { action = "ping_teste_validacao" };
                    string json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                    // Dispara contra o servidor
                    var response = await client.PostAsync("https://gravador.engeradios.com.br/api/telemetria.php", content);

                    // O nosso PHP da telemetria está programado para dar 401 se a chave for inválida.
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return false;
                    }

                    // Se não for 401 (Unauthorized), significa que a chave é válida e passou a porta da Nuvem!
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Falha grave de rede (Cabo de rede desligado, firewall, DNS)
                Console.WriteLine($"Erro no teste de ligação: {ex.Message}");
                return false;
            }
        }
    }
}