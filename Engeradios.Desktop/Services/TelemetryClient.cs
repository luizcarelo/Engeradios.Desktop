// Caminho do arquivo: Engeradios.Desktop/Services/TelemetryClient.cs

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Engeradios.Desktop.Services
{
    /// <summary>
    /// Classe responsável por enviar dados de saúde para o PHP na KingHost
    /// </summary>
    public class TelemetryClient
    {
        private readonly HttpClient _httpClient;

        // URL do nosso servidor PHP (Conforme nosso planejamento)
        private const string ApiUrl = "https://gravador.engeradios.com.br/api/telemetria.php";

        public TelemetryClient()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Envia um "Sinal de Vida" para a KingHost.
        /// </summary>
        /// <param name="apiKey">A chave secreta gerada para este cliente</param>
        /// <param name="espacoLivreGb">Quanto de disco ainda tem livre localmente</param>
        public async Task EnviarHeartbeatAsync(string apiKey, double espacoLivreGb)
        {
            try
            {
                // Monta o pacote de dados que será enviado para o servidor (Formato JSON)
                var dadosPayload = new
                {
                    action = "heartbeat",
                    horarioLocal = DateTime.Now,
                    espacoLivre = espacoLivreGb,
                    statusPlacas = "OK" // No futuro buscaremos as placas dinamicamente
                };

                // Transforma o objeto C# em um texto JSON
                var json = JsonSerializer.Serialize(dadosPayload);
                var conteudoHttp = new StringContent(json, Encoding.UTF8, "application/json");

                // Configura o cabeçalho de segurança exigido pela nossa API PHP
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                // Dispara o pacote pela internet
                var response = await _httpClient.PostAsync(ApiUrl, conteudoHttp);

                if (response.IsSuccessStatusCode)
                {
                    // Deu tudo certo, o servidor recebeu
                    string respostaServidor = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Sucesso Telemetria: {respostaServidor}");
                }
                else
                {
                    Console.WriteLine($"Erro no Servidor: Código {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Se a internet do cliente cair, cai aqui para não "travar" ou "fechar" o programa
                Console.WriteLine($"Erro de Conexão. Sem internet? Detalhes: {ex.Message}");
            }
        }
    }
}