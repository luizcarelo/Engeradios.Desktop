// Caminho do arquivo: Engeradios.Desktop/Services/TelemetryClient.cs

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics; // NOVO: Para escrever logs no Visual Studio

namespace Engeradios.Desktop.Services
{
    public class TelemetryClient
    {
        private readonly HttpClient _httpClient;

        // VERIFIQUE ESTA URL: Tem de ser o caminho EXATO para o ficheiro no seu servidor KingHost
        // Se não usar subdomínio, mude para: "http://www.engeradios.com.br/gravador/api/telemetria.php"
        private readonly string _apiUrl = "https://gravador.engeradios.com.br/api/telemetria.php";

        public TelemetryClient()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        }

        public async Task EnviarHeartbeatAsync(string apiKey, double espacoLivreGb, string versaoApp, string statusHardware)
        {
            try
            {
                // CORREÇÃO: O payload agora envia EXATAMENTE o que o PHP da KingHost exige.
                var payload = new
                {
                    action = "heartbeat", // OBRIGATÓRIO: O PHP rejeita o pedido se não tiver esta ação

                    // Campos antigos (Para compatibilidade com o telemetria.php atual)
                    espacoLivre = Math.Round(espacoLivreGb, 2),
                    statusPlacas = statusHardware,

                    // Campos novos (Para telemetrias futuras)
                    espaco_livre_gb = Math.Round(espacoLivreGb, 2),
                    versao_app = versaoApp,
                    status_hardware = statusHardware
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                var response = await _httpClient.PostAsync(_apiUrl, content);

                // Se o servidor devolver um erro (404, 500, 401), lemos o que ele diz!
                if (!response.IsSuccessStatusCode)
                {
                    string erroDoServidor = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ERRO NUVEM] O servidor KingHost respondeu com o Código {response.StatusCode}: {erroDoServidor}");
                    return; // Sai da função sem gerar exceção fatal
                }

                Debug.WriteLine("[SUCESSO NUVEM] Telemetria enviada com sucesso!");
            }
            catch (HttpRequestException httpEx)
            {
                // Falha de rede (Sem internet, DNS errado, SSL inválido)
                Debug.WriteLine($"[FALHA DE REDE] Não foi possível contactar o servidor: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERRO INTERNO] Telemetria: {ex.Message}");
            }
        }
    }
}