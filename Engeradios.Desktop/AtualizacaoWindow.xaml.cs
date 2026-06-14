// Caminho do arquivo: Engeradios.Desktop/AtualizacaoWindow.xaml.cs

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Engeradios.Desktop
{
    public partial class AtualizacaoWindow : Window
    {
        private const string VersaoLocal = "2.0.0"; // A versão compilada atual do programa
        private const string ApiUpdateUrl = "https://gravador.engeradios.com.br/api/check_update.php";
        private string _linkDownload = string.Empty;

        public AtualizacaoWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await VerificarAtualizacoesAsync();
        }

        private async Task VerificarAtualizacoesAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var response = await client.GetAsync(ApiUpdateUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();

                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            string versaoNuvem = root.GetProperty("versao_atual").GetString() ?? "0.0.0";
                            _linkDownload = root.GetProperty("url_download").GetString() ?? "";
                            string notas = root.GetProperty("notas_versao").GetString() ?? "Atualização de segurança.";

                            PainelAguardar.Visibility = Visibility.Collapsed;

                            // Compara a versão local (2.0.0) com a versão do servidor
                            if (VersaoLocal != versaoNuvem)
                            {
                                // Tem atualização
                                PainelNovaVersao.Visibility = Visibility.Visible;
                                BtnBaixar.Visibility = Visibility.Visible;
                                TxtVerLocal.Text = VersaoLocal;
                                TxtVerNuvem.Text = versaoNuvem;
                                TxtNotas.Text = notas;
                            }
                            else
                            {
                                // Já está atualizado
                                PainelAtualizado.Visibility = Visibility.Visible;
                                TxtVersaoAtual.Text = $"Versão Instalada: {VersaoLocal}";
                            }
                        }
                    }
                    else
                    {
                        MostrarErro();
                    }
                }
            }
            catch (Exception)
            {
                MostrarErro();
            }
        }

        private void MostrarErro()
        {
            PainelAguardar.Visibility = Visibility.Collapsed;
            PainelErro.Visibility = Visibility.Visible;
        }

        private void BtnBaixar_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_linkDownload))
            {
                try
                {
                    // Abre o link de download no navegador padrão do Windows do utilizador
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _linkDownload,
                        UseShellExecute = true
                    });
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Falha ao abrir o link: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}