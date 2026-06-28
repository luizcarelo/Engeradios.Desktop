// Caminho do arquivo: Engeradios.Desktop/Services/CloudSyncService.cs

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Helpers;
using System.Text.Json;
using System.Text;

namespace Engeradios.Desktop.Services
{
    public class CloudSyncService
    {
        private readonly string _connectionString = "Data Source=engeradios_local.db;";
        private readonly HttpClient _httpClient;
        private bool _isSyncing = false;

        private readonly string _apiUrl = "https://gravador.engeradios.com.br/api/upload_audio.php";
        private readonly string _apiLogsUrl = "https://gravador.engeradios.com.br/api/sync_logs.php";

        public CloudSyncService()
        {
            // Aumentado o timeout para garantir o envio mesmo em ligações 3G/4G fracas
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        }

        public async Task<int> SincronizarPendentesAsync()
        {
            if (_isSyncing) return 0;
            _isSyncing = true;

            int arquivosSincronizados = 0;

            try
            {
                string apiKey = ConfigSecurity.ObterApiKey();
                if (string.IsNullOrEmpty(apiKey)) return 0;

                var pendentes = ObterRegistosPendentes();

                foreach (var registo in pendentes)
                {
                    if (!File.Exists(registo.CaminhoFicheiro)) continue;

                    // ETAPA 4: Política de Tentativas (Retry Policy) com Atraso Exponencial
                    bool sucesso = await TentarUploadComRetryAsync(registo, apiKey, maxTentativas: 3);

                    if (sucesso)
                    {
                        MarcarComoSincronizadoLocalmente(registo.Id);
                        arquivosSincronizados++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro crítico na sincronização: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }

            return arquivosSincronizados;
        }

        // Motor de Tentativas: Garante a entrega mesmo com micro-cortes de internet
        private async Task<bool> TentarUploadComRetryAsync(RegistoAudio registo, string apiKey, int maxTentativas)
        {
            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                try
                {
                    bool resultado = await FazerUploadParaNuvemAsync(registo, apiKey);
                    if (resultado) return true; // Sucesso, sai do loop
                }
                catch (HttpRequestException)
                {
                    // Falha de rede. Se for a última tentativa, repassa o erro ou desiste calado
                    if (tentativa == maxTentativas) throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro desconhecido no upload: {ex.Message}");
                    return false;
                }

                // Espera 2s, depois 4s, depois 8s... antes de tentar novamente (Backoff)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa)));
            }
            return false;
        }

        public async Task SincronizarLogsAsync()
        {
            try
            {
                string apiKey = ConfigSecurity.ObterApiKey();
                if (string.IsNullOrEmpty(apiKey)) return;

                var dbService = new DatabaseService();
                var logsPendentes = dbService.ObterLogsPendentes();

                if (logsPendentes.Count == 0) return;

                var json = JsonSerializer.Serialize(logsPendentes);
                var conteudoHttp = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                // Retry Policy para os Logs também
                for (int tentativa = 1; tentativa <= 3; tentativa++)
                {
                    var response = await _httpClient.PostAsync(_apiLogsUrl, conteudoHttp);

                    if (response.IsSuccessStatusCode)
                    {
                        foreach (var log in logsPendentes)
                        {
                            dbService.MarcarLogComoSincronizado(log.Id);
                        }
                        break; // Sucesso, aborta novas tentativas
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2 * tentativa));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao sincronizar logs: {ex.Message}");
            }
        }

        private async Task<bool> FazerUploadParaNuvemAsync(RegistoAudio registo, string apiKey)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(registo.Canal), "canal");
            form.Add(new StringContent(registo.DataHoraGravacao.ToString("yyyy-MM-dd HH:mm:ss")), "data_hora");
            form.Add(new StringContent(registo.DuracaoSegundos.ToString()), "duracao");
            form.Add(new StringContent(registo.Anotacoes ?? string.Empty), "anotacoes");

            using var fileStream = new FileStream(registo.CaminhoFicheiro, FileMode.Open, FileAccess.Read);
            using var fileContent = new StreamContent(fileStream);

            // ETAPA 4: Suporte inteligente a MP3 e WAV dinamicamente
            string extensao = Path.GetExtension(registo.CaminhoFicheiro).ToLower();
            string mimeType = extensao == ".mp3" ? "audio/mpeg" : "audio/wav";
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

            form.Add(fileContent, "ficheiro_audio", Path.GetFileName(registo.CaminhoFicheiro));

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

            var response = await _httpClient.PostAsync(_apiUrl, form);
            return response.IsSuccessStatusCode;
        }

        private List<RegistoAudio> ObterRegistosPendentes()
        {
            var lista = new List<RegistoAudio>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();

            // Puxa em lotes de 5 para não sobrecarregar a memória
            cmd.CommandText = "SELECT Id, Canal, DataHoraGravacao, CaminhoFicheiro, DuracaoSegundos, Anotacoes FROM RegistroAudios WHERE SincronizadoComNuvem = 0 LIMIT 5;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new RegistoAudio
                {
                    Id = reader.GetInt32(0),
                    Canal = reader.GetString(1),
                    DataHoraGravacao = DateTime.Parse(reader.GetString(2)),
                    CaminhoFicheiro = reader.GetString(3),
                    DuracaoSegundos = reader.GetInt32(4),
                    Anotacoes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }
            return lista;
        }

        private void MarcarComoSincronizadoLocalmente(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE RegistroAudios SET SincronizadoComNuvem = 1 WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }
}