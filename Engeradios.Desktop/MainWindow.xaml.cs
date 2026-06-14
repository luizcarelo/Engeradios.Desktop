// Caminho do arquivo: Engeradios.Desktop/MainWindow.xaml.cs

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using Engeradios.Desktop.Services;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Helpers;

namespace Engeradios.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, AudioRecorderService> _gravadoresAtivos = [];

        private readonly DatabaseService _databaseService;
        private readonly TelemetryClient _telemetryClient;
        private readonly LogService _logService;
        private readonly CloudSyncService _cloudSyncService;

        private readonly string _pastaGravacoes;

        private DispatcherTimer _timerMonitoramento = null!;
        private DispatcherTimer _timerEspectro = null!;
        private readonly Random _random = new();

        private List<RegistoAudio> _todosOsRegistos = [];

        private readonly string _nivelAcessoUsuario;
        private readonly string _nomeUsuarioLogado;
        private bool _isTemaEscuro;

        public MainWindow(string nomeUsuario = "Administrador Local", string nivelAcesso = "Administrador", bool temaEscuro = true)
        {
            InitializeComponent();

            _nomeUsuarioLogado = nomeUsuario;
            _nivelAcessoUsuario = nivelAcesso;
            _isTemaEscuro = temaEscuro;

            // Correção IDE0031: Null propagation
            if (TxtOperadorLogado != null) TxtOperadorLogado.Text = _nomeUsuarioLogado;

            _pastaGravacoes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Engeradios_Audios");

            _databaseService = new();
            _telemetryClient = new();
            _logService = new();
            _cloudSyncService = new();

            AtualizarHistorico();
            AplicarTema();
            ConfigurarMonitoramentoSegundoPlano();

            IniciarGravacaoAutomatica();
            IniciarAnimacaoEspectro();

            _logService.Registar(_nomeUsuarioLogado, "Iniciou o Centro de Comando C3");
        }

        private void IniciarGravacaoAutomatica()
        {
            try
            {
                _gravadoresAtivos.Clear();

                var canaisConfigurados = ConfigService.CarregarCanais();
                int numPlacasDisponiveis = NAudio.Wave.WaveInEvent.DeviceCount;

                foreach (var config in canaisConfigurados)
                {
                    // Correção IDE0017: Object Initialization
                    var gravador = new AudioRecorderService(_pastaGravacoes)
                    {
                        Sensibilidade = config.VAD,
                        NomeDoCanal = config.Nome
                    };

                    gravador.GravacaoFinalizada += (sender, registo) =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _databaseService.InserirRegisto(registo);
                            AtualizarHistorico();
                        });
                    };

                    int placaID = config.PlacaIndex < numPlacasDisponiveis ? config.PlacaIndex : 0;

                    gravador.IniciarEscuta(placaID);
                    _gravadoresAtivos.Add(config.Nome, gravador);
                }

                TxtCanaisAtivos.Text = $"{canaisConfigurados.Count} Canais em Monitorização";
                LedGravando.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
                TxtGravando.Text = "GRAVAÇÃO ATIVA";
                TxtGravando.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
                BtnPararEmergencia.Content = "PARAR MOTORES (EMERGÊNCIA)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar os motores de áudio: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IniciarAnimacaoEspectro()
        {
            _timerEspectro = new() { Interval = TimeSpan.FromMilliseconds(120) };
            _timerEspectro.Tick += (s, e) =>
            {
                BarraA1.Height = _random.Next(5, 45);
                BarraA2.Height = _random.Next(10, 60);
                BarraA3.Height = _random.Next(5, 40);
                BarraA4.Height = _random.Next(20, 60);
                BarraA5.Height = _random.Next(5, 50);
                BarraA6.Height = _random.Next(15, 60);
                BarraA7.Height = _random.Next(5, 40);

                LedGravando.Opacity = _gravadoresAtivos.Count > 0 && LedGravando.Opacity == 1 ? 0.3 : 1;
            };
            _timerEspectro.Start();
        }

        private void AtualizarHistorico()
        {
            if (_databaseService != null && GridHistorico != null)
            {
                _todosOsRegistos = _databaseService.ObterHistorico();
                AplicarFiltros();
            }
        }

        private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltros();
        private void DpDataFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => AplicarFiltros();

        private void AplicarFiltros()
        {
            var query = _todosOsRegistos.AsQueryable();

            string textoFiltro = TxtBusca.Text.Trim(); // Correção CA1862
            if (!string.IsNullOrEmpty(textoFiltro))
            {
                query = query.Where(r => r.Canal.Contains(textoFiltro, StringComparison.OrdinalIgnoreCase) ||
                                         r.Anotacoes.Contains(textoFiltro, StringComparison.OrdinalIgnoreCase));
            }

            if (DpDataFiltro.SelectedDate.HasValue)
            {
                var data = DpDataFiltro.SelectedDate.Value.Date;
                query = query.Where(r => r.DataHoraGravacao.Date == data);
            }

            GridHistorico.ItemsSource = query.ToList();

            int audiosHoje = _todosOsRegistos.Count(r => r.DataHoraGravacao.Date == DateTime.Today);
            TxtAudiosHoje.Text = audiosHoje.ToString();
        }

        private void BtnAnotar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                Window prompt = new()
                {
                    Width = 400,
                    Height = 250,
                    Title = "Anotação de Áudio",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    WindowStyle = WindowStyle.ToolWindow,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"))
                };

                StackPanel painel = new() { Margin = new Thickness(20) };
                painel.Children.Add(new TextBlock { Text = $"Adicionar nota (Canal {registo.Canal}):", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

                TextBox txtNota = new() { Text = registo.Anotacoes, Height = 80, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A")), Foreground = Brushes.White, Padding = new Thickness(5) };
                painel.Children.Add(txtNota);

                Button btnSalvar = new() { Content = "GUARDAR NOTA", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")), Foreground = Brushes.White, Height = 35, Margin = new Thickness(0, 15, 0, 0), FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };
                btnSalvar.Click += (s, ev) => {
                    _databaseService.AtualizarAnotacao(registo.Id, txtNota.Text);
                    _logService.Registar(_nomeUsuarioLogado, $"Adicionou nota ao áudio ID {registo.Id}");
                    prompt.DialogResult = true;
                    prompt.Close();
                };
                painel.Children.Add(btnSalvar);
                prompt.Content = painel;

                if (prompt.ShowDialog() == true) AtualizarHistorico();
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                if (File.Exists(registo.CaminhoFicheiro))
                {
                    _logService.Registar(_nomeUsuarioLogado, $"Reproduziu áudio ID {registo.Id} no Leitor Interno");

                    AudioPlayerWindow leitor = new(registo.CaminhoFicheiro, registo.Canal, registo.DataHoraGravacao.ToString("dd/MM/yyyy HH:mm:ss"))
                    {
                        Owner = this
                    };
                    leitor.Show();
                }
                else
                {
                    MessageBox.Show("O ficheiro original não foi encontrado ou já foi sincronizado e apagado.", "Ficheiro Ausente", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo && File.Exists(registo.CaminhoFicheiro))
            {
                _logService.Registar(_nomeUsuarioLogado, $"Exportou ficheiro de áudio ID {registo.Id}");
                MessageBox.Show($"O ficheiro está localizado em:\n{registo.CaminhoFicheiro}", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnPararEmergencia_Click(object sender, RoutedEventArgs e)
        {
            if (_gravadoresAtivos.Count > 0)
            {
                foreach (var canal in _gravadoresAtivos) canal.Value.PararEscuta();
                _gravadoresAtivos.Clear();

                TxtGravando.Text = "SISTEMA PARADO";
                TxtGravando.Foreground = Brushes.Gray;
                LedGravando.Fill = Brushes.Gray;
                TxtCanaisAtivos.Text = "Nenhum canal ativo";
                BtnPararEmergencia.Content = "REINICIAR MOTORES";
                _logService.Registar(_nomeUsuarioLogado, "Parou os motores de gravação manualmente", "Aviso");
            }
            else
            {
                IniciarGravacaoAutomatica();
                _logService.Registar(_nomeUsuarioLogado, "Reiniciou os motores de gravação");
            }
        }

        private void MenuConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_nivelAcessoUsuario != "Administrador")
            {
                MessageBox.Show("Acesso Negado!", "Segurança", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (var canal in _gravadoresAtivos) canal.Value.PararEscuta();
            _gravadoresAtivos.Clear();

            ConfiguracoesWindow telaConfig = new(true) { Owner = this };
            telaConfig.ShowDialog();

            IniciarGravacaoAutomatica();
        }

        private void MenuTema_Click(object sender, RoutedEventArgs e)
        {
            _isTemaEscuro = !_isTemaEscuro;
            AplicarTema();
        }

        private void AplicarTema()
        {
            string pastaBase = AppDomain.CurrentDomain.BaseDirectory;

            if (_isTemaEscuro)
            {
                JanelaPrincipal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F0F"));
                MenuSuperior.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
                MenuSuperior.Foreground = Brushes.White;
                MenuTema.Header = "☀️ Tema Claro";

                CabecalhoBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));

                string logoEscuroPath = Path.Combine(pastaBase, "logo_escuro.png");
                if (File.Exists(logoEscuroPath)) ImgLogoPrincipal.Source = new BitmapImage(new Uri(logoEscuroPath, UriKind.Absolute));

                Card1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                Card2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                Card3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                Card4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));

                PainelEsquerdo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                PainelDireito.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));

                GridHistorico.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                GridHistorico.RowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                GridHistorico.AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525"));
                GridHistorico.Foreground = Brushes.White;
            }
            else
            {
                JanelaPrincipal.Background = new SolidColorBrush(Colors.LightGray);
                MenuSuperior.Background = new SolidColorBrush(Colors.White);
                MenuSuperior.Foreground = Brushes.Black;
                MenuTema.Header = "🌙 Tema Escuro";

                CabecalhoBorder.Background = new SolidColorBrush(Colors.WhiteSmoke);

                string logoClaroPath = Path.Combine(pastaBase, "logo_claro.png");
                if (File.Exists(logoClaroPath)) ImgLogoPrincipal.Source = new BitmapImage(new Uri(logoClaroPath, UriKind.Absolute));

                Card1.Background = Brushes.White;
                Card2.Background = Brushes.White;
                Card3.Background = Brushes.White;
                Card4.Background = Brushes.White;

                PainelEsquerdo.Background = Brushes.White;
                PainelDireito.Background = Brushes.White;

                GridHistorico.Background = Brushes.White;
                GridHistorico.RowBackground = Brushes.White;
                GridHistorico.AlternatingRowBackground = new SolidColorBrush(Colors.WhiteSmoke);
                GridHistorico.Foreground = Brushes.Black;
            }
        }

        private void ConfigurarMonitoramentoSegundoPlano()
        {
            _timerMonitoramento = new() { Interval = TimeSpan.FromSeconds(30) };
            _timerMonitoramento.Tick += TimerMonitoramento_Tick;
            _timerMonitoramento.Start();
            TimerMonitoramento_Tick(this, EventArgs.Empty);
        }

        private async void TimerMonitoramento_Tick(object? sender, EventArgs e)
        {
            try
            {
                string raiz = Path.GetPathRoot(_pastaGravacoes) ?? "C:\\";
                DriveInfo disco = new(raiz);
                double espacoLivreGb = disco.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                // NOVO: Calcula o tamanho real da pasta em Gigabytes
                double espacoUsadoNaPastaGb = CalcularTamanhoPasta(_pastaGravacoes) / (1024.0 * 1024.0 * 1024.0);
                double quotaMaximaGb = 40.0; // Aqui você define o limite do disco para este cliente

                if (BarraDisco != null && TxtUsoDisco != null)
                {
                    BarraDisco.Maximum = quotaMaximaGb;
                    BarraDisco.Value = espacoUsadoNaPastaGb;
                    TxtUsoDisco.Text = $"{espacoUsadoNaPastaGb:F1} GB / {quotaMaximaGb} GB";

                    if (espacoUsadoNaPastaGb >= quotaMaximaGb * 0.9)
                        BarraDisco.Foreground = new SolidColorBrush(Colors.Red);
                }

                string chaveDaNuvem = string.Empty;
                try { chaveDaNuvem = ConfigSecurity.ObterApiKey(); } catch { }

                if (_telemetryClient != null && LedNuvem != null && TxtEstadoNuvem != null)
                {
                    if (!string.IsNullOrEmpty(chaveDaNuvem))
                    {
                        await _telemetryClient.EnviarHeartbeatAsync(chaveDaNuvem, espacoLivreGb);
                        LedNuvem.Fill = new SolidColorBrush(Colors.LimeGreen);
                        TxtEstadoNuvem.Text = "Conectado ao Cloud Sync";

                        // NOVO: Executa a Regra FIFO Local - Protegendo Arquivos Marcados
                        GerirEspacoLocalFIFO(espacoUsadoNaPastaGb, quotaMaximaGb);

                        // Envia áudios pendentes
                        int arquivosEnviados = await _cloudSyncService.SincronizarPendentesAsync();
                        if (arquivosEnviados > 0) AtualizarHistorico();

                        // NOVO: Envia os logs de auditoria
                        await _cloudSyncService.SincronizarLogsAsync();
                    }
                    else
                    {
                        LedNuvem.Fill = new SolidColorBrush(Colors.Orange);
                        TxtEstadoNuvem.Text = "Aguardando Setup de API";
                    }
                }
            }
            catch (Exception)
            {
                if (LedNuvem != null && TxtEstadoNuvem != null)
                {
                    LedNuvem.Fill = new SolidColorBrush(Colors.Red);
                    TxtEstadoNuvem.Text = "Sem Ligação KingHost";
                }
            }
        }

        // --- NOVAS FUNÇÕES DE ESPAÇO E PROTEÇÃO ---

        private double CalcularTamanhoPasta(string caminho)
        {
            if (!Directory.Exists(caminho)) return 0;
            // Lê todos os ficheiros da pasta e soma o tamanho em bytes
            return new DirectoryInfo(caminho).EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        private void GerirEspacoLocalFIFO(double espacoAtualGb, double quotaGb)
        {
            try
            {
                // A regra é clara: Se o disco ainda tem espaço, NÃO APAGAR NADA!
                if (espacoAtualGb <= quotaGb) return;

                // Atingiu o limite. Pede à base de dados os 50 ficheiros mais antigos que NÃO estão marcados (Protegido = 0)
                var candidatos = _databaseService.ObterCandidatosLimpeza(50);

                int apagados = 0;
                foreach (var audio in candidatos)
                {
                    try
                    {
                        if (File.Exists(audio.CaminhoFicheiro))
                        {
                            File.Delete(audio.CaminhoFicheiro);
                        }
                        // Remove o registo do histórico
                        _databaseService.RemoverRegisto(audio.Id);
                        apagados++;
                    }
                    catch { /* Ficheiro trancado pelo Windows, ignora por agora */ }
                }

                if (apagados > 0)
                {
                    _logService.Registar(_nomeUsuarioLogado, $"Limpeza Automática Local: {apagados} áudios desprotegidos foram apagados porque o disco atingiu o limite de {quotaGb}GB.", "Aviso");
                    Application.Current.Dispatcher.InvokeAsync(AtualizarHistorico);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na rotina FIFO: {ex.Message}");
            }
        }

        private void BtnProteger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                bool novoEstado = !registo.Protegido;
                _databaseService.AlternarProtecao(registo.Id, novoEstado);

                string acao = novoEstado ? "protegeu" : "desprotegeu";
                _logService.Registar(_nomeUsuarioLogado, $"O operador {acao} o áudio ID {registo.Id} contra exclusão automática no PC.");

                AtualizarHistorico();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_nivelAcessoUsuario == "Operador")
            {
                bool autorizado = MostrarPromptSenhaAdmin();
                if (!autorizado)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _logService.Registar(_nomeUsuarioLogado, "Encerrou o sistema Engeradios");
            foreach (var canal in _gravadoresAtivos) canal.Value.PararEscuta();
        }

        private bool MostrarPromptSenhaAdmin()
        {
            bool senhaCorreta = false;
            Window prompt = new()
            {
                Width = 350,
                Height = 220,
                Title = "Autorização de Saída",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow
            };

            StackPanel painel = new() { Margin = new Thickness(20) };
            TextBlock txtTitulo = new() { Text = "Acesso Restrito", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            TextBlock txtLabel = new() { Text = "Para fechar o gravador, um Administrador\ndeve introduzir a sua palavra-passe:", Foreground = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 15) };
            PasswordBox pwdBox = new() { Padding = new Thickness(5), FontSize = 16, Margin = new Thickness(0, 0, 0, 20) };
            Button btnOk = new() { Content = "AUTORIZAR SAÍDA", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")), Foreground = Brushes.White, Height = 35, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };

            btnOk.Click += (s, ev) =>
            {
                if (pwdBox.Password == "admin") { senhaCorreta = true; prompt.Close(); }
                else { MessageBox.Show("Senha incorreta!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); pwdBox.Clear(); }
            };

            painel.Children.Add(txtTitulo); painel.Children.Add(txtLabel); painel.Children.Add(pwdBox); painel.Children.Add(btnOk);
            prompt.Content = painel;
            prompt.ShowDialog();

            return senhaCorreta;
        }

        private void MenuCadastro_Click(object sender, RoutedEventArgs e)
        {
            if (_nivelAcessoUsuario != "Administrador")
            {
                MessageBox.Show("Acesso Negado! Apenas Administradores podem gerir utilizadores.", "Segurança", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CadastroUsuariosWindow telaCadastro = new(_nomeUsuarioLogado) { Owner = this };
            telaCadastro.ShowDialog();
        }

        private void MenuLogs_Click(object sender, RoutedEventArgs e)
        {
            AuditoriaWindow t = new() { Owner = this };
            t.ShowDialog();
        }

        private void MenuAtualizar_Click(object sender, RoutedEventArgs e)
        {
            AtualizacaoWindow telaUpdate = new() { Owner = this };
            telaUpdate.ShowDialog();
        }

        private void MenuRemoto_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Iniciando módulo de conexão remota...", "Suporte", MessageBoxButton.OK, MessageBoxImage.Information); }

        private void MenuSuporte_Click(object sender, RoutedEventArgs e) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.engeradios.com.br/suporte", UseShellExecute = true }); } catch { } }
    }
}