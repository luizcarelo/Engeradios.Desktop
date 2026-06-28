// Caminho do arquivo: Engeradios.Desktop/MainWindow.xaml.cs

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Engeradios.Desktop.Services;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Helpers;
using Engeradios.Desktop.ViewModels;

namespace Engeradios.Desktop
{
    public class UIStatusCanal : INotifyPropertyChanged
    {
        public string NomeCanal { get; set; } = string.Empty;
        private int _nivelSinal; public int NivelSinal { get => _nivelSinal; set { _nivelSinal = value; OnPropertyChanged(nameof(NivelSinal)); } }
        private string _statusTexto = "AGUARDANDO"; public string StatusTexto { get => _statusTexto; set { _statusTexto = value; OnPropertyChanged(nameof(StatusTexto)); } }
        private Brush _corAtividade = Brushes.Gray; public Brush CorAtividade { get => _corAtividade; set { _corAtividade = value; OnPropertyChanged(nameof(CorAtividade)); } }
        public event PropertyChangedEventHandler? PropertyChanged; protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, AudioRecorderService> _gravadoresAtivos = new();
        private readonly ObservableCollection<UIStatusCanal> _listaUiCanais = new();

        private readonly DatabaseService _databaseService;
        private readonly TelemetryClient _telemetryClient;
        private readonly LogService _logService;
        private readonly CloudSyncService _cloudSyncService;

        private readonly string _pastaGravacoes;
        private DispatcherTimer _timerMonitoramento = null!;
        private DispatcherTimer _timerAtividadeAudio = null!;

        public MainViewModel ViewModel => (MainViewModel)this.DataContext!;

        public MainWindow(MainViewModel viewModel, DatabaseService databaseService, TelemetryClient telemetryClient, LogService logService, CloudSyncService cloudSyncService)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            _databaseService = databaseService;
            _telemetryClient = telemetryClient;
            _logService = logService;
            _cloudSyncService = cloudSyncService;

            _pastaGravacoes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Engeradios_Audios");

            if (ListaCanaisAtivos != null) ListaCanaisAtivos.ItemsSource = _listaUiCanais;

            // Configura o ItemsSource para apontar para a coleção da ViewModel
            if (GridHistorico != null)
            {
                GridHistorico.ItemsSource = ViewModel.HistoricoAudios;
                // Prepara a vista em memória para filtragem instantânea
                var view = CollectionViewSource.GetDefaultView(ViewModel.HistoricoAudios);
                if (view != null) view.Filter = FiltroEmMemoria;
            }

            AplicarTema();
            ConfigurarMonitoramentoSegundoPlano();
            IniciarGravacaoAutomatica();
            IniciarMonitorDeAtividade();
        }

        private void IniciarGravacaoAutomatica()
        {
            try
            {
                _gravadoresAtivos.Clear();
                _listaUiCanais.Clear();

                var canaisConfigurados = ConfigService.CarregarCanais();
                int numPlacasDisponiveis = NAudio.Wave.WaveInEvent.DeviceCount;

                foreach (var config in canaisConfigurados)
                {
                    var gravador = new AudioRecorderService(_pastaGravacoes) { Sensibilidade = config.VAD, NomeDoCanal = config.Nome };
                    gravador.GravacaoFinalizada += async (sender, registo) =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await _databaseService.InserirRegistoAsync(registo);
                            await ViewModel.AtualizarHistoricoAsync();
                            AtualizarContadores();
                        });
                    };

                    int placaID = config.PlacaIndex < numPlacasDisponiveis ? config.PlacaIndex : 0;
                    gravador.IniciarEscuta(placaID);
                    _gravadoresAtivos.Add(config.Nome, gravador);
                    _listaUiCanais.Add(new UIStatusCanal { NomeCanal = config.Nome });
                }

                if (TxtCanaisAtivos != null) TxtCanaisAtivos.Text = $"{canaisConfigurados.Count} Canais Alocados";
                if (LedGravando != null) LedGravando.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
                if (TxtGravando != null) { TxtGravando.Text = "SISTEMA ARMADO"; TxtGravando.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")); }
                if (BtnPararEmergencia != null) BtnPararEmergencia.Content = "PARAR MOTORES (EMERGÊNCIA)";
            }
            catch (Exception ex) { MessageBox.Show($"Erro: {ex.Message}"); }
        }

        private void IniciarMonitorDeAtividade()
        {
            _timerAtividadeAudio = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timerAtividadeAudio.Tick += (s, e) =>
            {
                if (_gravadoresAtivos.Count == 0) return;

                foreach (var canalUi in _listaUiCanais)
                {
                    if (_gravadoresAtivos.TryGetValue(canalUi.NomeCanal, out var gravador))
                    {
                        float sensibilidade = gravador.Sensibilidade > 0 ? gravador.Sensibilidade : 0.05f;
                        float pico = gravador.ObterPicoE_Resetar();

                        int percentualAlvo = (int)((pico / sensibilidade) * 35);
                        if (percentualAlvo > 100) percentualAlvo = 100;

                        if (percentualAlvo >= canalUi.NivelSinal)
                            canalUi.NivelSinal = percentualAlvo;
                        else
                            canalUi.NivelSinal = Math.Max(0, canalUi.NivelSinal - 12);

                        if (gravador.IsRecording)
                        {
                            canalUi.StatusTexto = "GRAVANDO";
                            canalUi.CorAtividade = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
                        }
                        else
                        {
                            canalUi.StatusTexto = "AGUARDANDO";
                            canalUi.CorAtividade = Brushes.Gray;
                        }
                    }
                }
            };
            _timerAtividadeAudio.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ViewModel.NivelAcessoUsuario == "Operador" && !MostrarPromptSenhaAdmin()) { e.Cancel = true; return; }
            foreach (var canal in _gravadoresAtivos.Values) canal.PararEscuta();
        }

        private bool MostrarPromptSenhaAdmin()
        {
            bool senhaCorreta = false;
            Window prompt = new() { Width = 350, Height = 250, Title = "Autorização de Saída", WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")), Owner = this, WindowStyle = WindowStyle.ToolWindow };
            StackPanel painel = new() { Margin = new Thickness(20) };
            TextBox txtUser = new() { Padding = new Thickness(5), FontSize = 14, Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox pwdBox = new() { Padding = new Thickness(5), FontSize = 14, Margin = new Thickness(0, 0, 0, 20) };
            Button btnOk = new() { Content = "AUTORIZAR SAÍDA", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")), Foreground = Brushes.White, Height = 35, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };

            btnOk.Click += (s, ev) => {
                if (_databaseService.ValidarLogin(txtUser.Text, pwdBox.Password) == "Administrador") { senhaCorreta = true; prompt.Close(); }
                else { MessageBox.Show("Acesso negado!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); pwdBox.Clear(); }
            };

            painel.Children.Add(new TextBlock { Text = "Acesso Restrito", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            painel.Children.Add(txtUser); painel.Children.Add(pwdBox); painel.Children.Add(btnOk);
            prompt.Content = painel; prompt.ShowDialog();

            return senhaCorreta;
        }

        private void MenuConfig_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NivelAcessoUsuario != "Administrador") { MessageBox.Show("Acesso Negado!", "Segurança", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            foreach (var canal in _gravadoresAtivos.Values) canal.PararEscuta();
            _gravadoresAtivos.Clear();

            ConfiguracoesWindow telaConfig = new(true) { Owner = this };
            telaConfig.ShowDialog();
            IniciarGravacaoAutomatica();
        }

        private void MenuTema_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsTemaEscuro = !ViewModel.IsTemaEscuro;
            AplicarTema();
        }

        private void MenuNuvem_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NivelAcessoUsuario != "Administrador") { MessageBox.Show("Acesso Negado!", "Segurança", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            SetupWindow telaSetup = new() { Owner = this }; telaSetup.ShowDialog();
        }

        private void MenuCadastro_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.NivelAcessoUsuario != "Administrador") { MessageBox.Show("Acesso Negado!", "Segurança", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            CadastroUsuariosWindow t = new(ViewModel.NomeUsuarioLogado) { Owner = this }; t.ShowDialog();
        }

        private void MenuLogs_Click(object sender, RoutedEventArgs e) { AuditoriaWindow t = new() { Owner = this }; t.ShowDialog(); }
        private void MenuAtualizar_Click(object sender, RoutedEventArgs e) { AtualizacaoWindow t = new() { Owner = this }; t.ShowDialog(); }
        private void MenuRemoto_Click(object sender, RoutedEventArgs e) { SuporteRemotoWindow t = new() { Owner = this }; t.ShowDialog(); }
        private void MenuSuporte_Click(object sender, RoutedEventArgs e) { try { Process.Start(new ProcessStartInfo { FileName = "https://www.engeradios.com.br/suporte", UseShellExecute = true }); } catch { } }

        private void BtnPararEmergencia_Click(object sender, RoutedEventArgs e)
        {
            if (_gravadoresAtivos.Count > 0)
            {
                foreach (var canal in _gravadoresAtivos) canal.Value.PararEscuta();
                _gravadoresAtivos.Clear();
                foreach (var canalUi in _listaUiCanais) { canalUi.NivelSinal = 0; canalUi.StatusTexto = "DESLIGADO"; canalUi.CorAtividade = Brushes.DarkRed; }
                if (TxtGravando != null) { TxtGravando.Text = "SISTEMA PARADO"; TxtGravando.Foreground = Brushes.Gray; LedGravando.Fill = Brushes.Gray; }
                if (BtnPararEmergencia != null) BtnPararEmergencia.Content = "REINICIAR MOTORES";
                _logService.Registar(ViewModel.NomeUsuarioLogado, "Parou os motores de gravação manualmente", "Aviso");
            }
            else
            {
                IniciarGravacaoAutomatica();
                _logService.Registar(ViewModel.NomeUsuarioLogado, "Reiniciou os motores de gravação");
            }
        }

        private void BtnProteger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                bool novoEstado = !registo.Protegido;
                _databaseService.AlternarProtecao(registo.Id, novoEstado);
                _logService.Registar(ViewModel.NomeUsuarioLogado, $"O operador {(novoEstado ? "protegeu" : "desprotegeu")} o áudio ID {registo.Id}");
                _ = ViewModel.AtualizarHistoricoAsync();
            }
        }

        private void BtnAnotar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                Window prompt = new() { Width = 400, Height = 250, Title = "Anotação", WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, WindowStyle = WindowStyle.ToolWindow, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")) };
                StackPanel painel = new() { Margin = new Thickness(20) };
                TextBox txtNota = new() { Text = registo.Anotacoes, Height = 80, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A")), Foreground = Brushes.White, Padding = new Thickness(5) };
                Button btnSalvar = new() { Content = "GUARDAR", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")), Foreground = Brushes.White, Height = 35, Margin = new Thickness(0, 15, 0, 0), FontWeight = FontWeights.Bold };
                btnSalvar.Click += (s, ev) => { _databaseService.AtualizarAnotacao(registo.Id, txtNota.Text); prompt.DialogResult = true; prompt.Close(); };
                painel.Children.Add(txtNota); painel.Children.Add(btnSalvar); prompt.Content = painel;
                if (prompt.ShowDialog() == true) _ = ViewModel.AtualizarHistoricoAsync();
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo)
            {
                if (File.Exists(registo.CaminhoFicheiro))
                {
                    _logService.Registar(ViewModel.NomeUsuarioLogado, $"Reproduziu áudio ID {registo.Id}");
                    new AudioPlayerWindow(registo.CaminhoFicheiro, registo.Canal, registo.DataHoraGravacao.ToString("dd/MM/yyyy HH:mm:ss")) { Owner = this }.Show();
                }
                else MessageBox.Show("Ficheiro ausente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is RegistoAudio registo && File.Exists(registo.CaminhoFicheiro))
            {
                _logService.Registar(ViewModel.NomeUsuarioLogado, $"Exportou áudio ID {registo.Id}");
                MessageBox.Show($"Localizado em:\n{registo.CaminhoFicheiro}", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ====================================================================
        // GESTÃO DE PESQUISA (Escalável & Em Memória)
        // ====================================================================

        private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltrosLocais();
        private void DpDataFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => AplicarFiltrosLocais();

        private void AplicarFiltrosLocais()
        {
            if (GridHistorico == null) return;

            var view = CollectionViewSource.GetDefaultView(ViewModel.HistoricoAudios);
            if (view != null)
            {
                view.Refresh(); // Força a reavaliação do filtro
            }
            AtualizarContadores();
        }

        // Lógica de filtragem limpa e que funciona sem bater na base de dados
        private bool FiltroEmMemoria(object item)
        {
            if (item is RegistoAudio r)
            {
                string filtro = TxtBusca?.Text.Trim() ?? string.Empty;
                DateTime? data = DpDataFiltro?.SelectedDate;

                bool passaTexto = string.IsNullOrEmpty(filtro) ||
                                  (r.Canal != null && r.Canal.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                                  (r.Anotacoes != null && r.Anotacoes.Contains(filtro, StringComparison.OrdinalIgnoreCase));

                bool passaData = !data.HasValue || r.DataHoraGravacao.Date == data.Value.Date;

                return passaTexto && passaData;
            }
            return false;
        }

        private void AtualizarContadores()
        {
            if (TxtAudiosHoje != null)
            {
                int audiosHoje = ViewModel.HistoricoAudios.Count(r => r.DataHoraGravacao.Date == DateTime.Today);
                TxtAudiosHoje.Text = audiosHoje.ToString();
            }
        }

        // ====================================================================
        // MONITORAMENTO GERAL E NUVEM
        // ====================================================================

        private void ConfigurarMonitoramentoSegundoPlano()
        {
            _timerMonitoramento = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
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
                double espacoUsadoGb = CalcularTamanhoPasta(_pastaGravacoes) / (1024.0 * 1024.0 * 1024.0);

                var configGeral = ConfigService.CarregarConfiguracoesGerais() ?? new Engeradios.Desktop.Models.ConfiguracoesGerais();
                double maxGb = configGeral.LimiteEspacoDiscoGB;

                if (BarraDisco != null) { BarraDisco.Maximum = maxGb; BarraDisco.Value = espacoUsadoGb; }
                if (TxtUsoDisco != null) TxtUsoDisco.Text = $"{espacoUsadoGb:F1} GB / {maxGb} GB";

                string chaveNuvem = ConfigSecurity.ObterApiKey();
                if (!string.IsNullOrEmpty(chaveNuvem) && LedNuvem != null && TxtEstadoNuvem != null)
                {
                    LedNuvem.Fill = new SolidColorBrush(Colors.LimeGreen); TxtEstadoNuvem.Text = "Conectado ao Cloud Sync";

                    GerirEspacoLocalFIFO(espacoUsadoGb, maxGb);

                    if (await _cloudSyncService.SincronizarPendentesAsync() > 0)
                    {
                        await ViewModel.AtualizarHistoricoAsync();
                        AplicarFiltrosLocais();
                    }
                    await _cloudSyncService.SincronizarLogsAsync();
                }
                else if (LedNuvem != null && TxtEstadoNuvem != null)
                {
                    LedNuvem.Fill = new SolidColorBrush(Colors.Orange);
                    TxtEstadoNuvem.Text = "Aguardando Setup de API";
                }
            }
            catch (Exception)
            {
                if (LedNuvem != null && TxtEstadoNuvem != null)
                {
                    LedNuvem.Fill = new SolidColorBrush(Colors.Red);
                    TxtEstadoNuvem.Text = "Sem ligação com a nuvem";
                }
            }
        }

        private void GerirEspacoLocalFIFO(double espacoAtualGb, double quotaGb)
        {
            if (espacoAtualGb <= quotaGb) return;
            var candidatos = _databaseService.ObterCandidatosLimpeza(50);
            foreach (var audio in candidatos)
            {
                try { if (File.Exists(audio.CaminhoFicheiro)) File.Delete(audio.CaminhoFicheiro); _databaseService.RemoverRegisto(audio.Id); }
                catch (Exception ex) { _logService.Registar(ViewModel.NomeUsuarioLogado, $"Erro FIFO: {ex.Message}", "Erro"); }
            }
            _ = ViewModel.AtualizarHistoricoAsync();
        }

        private static double CalcularTamanhoPasta(string caminho) => !Directory.Exists(caminho) ? 0 : new DirectoryInfo(caminho).EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);

        private void AplicarTema()
        {
            if (JanelaPrincipal == null) return;
            string pastaBase = AppDomain.CurrentDomain.BaseDirectory;
            if (ViewModel.IsTemaEscuro)
            {
                JanelaPrincipal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F0F"));
                if (MenuSuperior != null) { MenuSuperior.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A")); MenuSuperior.Foreground = Brushes.White; MenuTema.Header = "☀️ Tema Claro"; }
                if (CabecalhoBorder != null) CabecalhoBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));
                if (GridHistorico != null) { GridHistorico.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")); GridHistorico.Foreground = Brushes.White; GridHistorico.RowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")); GridHistorico.AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525")); }
            }
            else
            {
                JanelaPrincipal.Background = new SolidColorBrush(Colors.LightGray);
                if (MenuSuperior != null) { MenuSuperior.Background = Brushes.White; MenuSuperior.Foreground = Brushes.Black; MenuTema.Header = "🌙 Tema Escuro"; }
                if (CabecalhoBorder != null) CabecalhoBorder.Background = new SolidColorBrush(Colors.WhiteSmoke);
                if (GridHistorico != null) { GridHistorico.Background = Brushes.White; GridHistorico.Foreground = Brushes.Black; GridHistorico.RowBackground = Brushes.White; GridHistorico.AlternatingRowBackground = new SolidColorBrush(Colors.WhiteSmoke); }
            }
        }
    }
}