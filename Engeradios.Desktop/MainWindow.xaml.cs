// Caminho do arquivo: Engeradios.Desktop/MainWindow.xaml.cs

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Controls;
using Engeradios.Desktop.Services;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop
{
    public partial class MainWindow : Window
    {
        // Dicionário para capturar múltiplos canais de hardware rodando concorrentemente
        private Dictionary<string, AudioRecorderService> _gravadoresAtivos = new Dictionary<string, AudioRecorderService>();

        private DatabaseService _databaseService = null!;
        private TelemetryClient _telemetryClient = null!;
        private string _pastaGravacoes = string.Empty;

        private DispatcherTimer _timerMonitoramento = null!;
        private DispatcherTimer _timerEspectro = null!;
        private Random _random = new Random();

        private string _nivelAcessoUsuario;
        private string _nomeUsuarioLogado;
        private bool _isTemaEscuro;

        public MainWindow(string nomeUsuario = "Administrador Local", string nivelAcesso = "Administrador", bool temaEscuro = true)
        {
            InitializeComponent();

            _nomeUsuarioLogado = nomeUsuario;
            _nivelAcessoUsuario = nivelAcesso;
            _isTemaEscuro = temaEscuro;

            if (TxtOperadorLogado != null)
                TxtOperadorLogado.Text = $"Operador: {_nomeUsuarioLogado} ({_nivelAcessoUsuario})";

            // Pasta oculta e segura partilhada no Windows para logs de gravação
            _pastaGravacoes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Engeradios_Audios");

            _databaseService = new DatabaseService();
            _telemetryClient = new TelemetryClient();

            AtualizarHistorico();
            AplicarTema();
            ConfigurarMonitoramentoSegundoPlano();

            // DISPARO AUTOMÁTICO REQUISITADO: Gravação e Espectro iniciam na inicialização
            IniciarGravacaoAutomatica();
            IniciarAnimacaoEspectro();
        }

        /// <summary>
        /// Instancia e liga a captura de todas as entradas de áudio de forma simultânea.
        /// </summary>
        private void IniciarGravacaoAutomatica()
        {
            try
            {
                _gravadoresAtivos.Clear();
                string[] canais = { "Segurança", "Manutenção", "Brigada" };
                int numPlacasDisponiveis = NAudio.Wave.WaveInEvent.DeviceCount;

                // CORRIGIDO: Nome da variável alinhado!
                float sensibilidadeGlobal = 0.05f;

                for (int i = 0; i < canais.Length; i++)
                {
                    var gravador = new AudioRecorderService(_pastaGravacoes);
                    gravador.Sensibilidade = sensibilidadeGlobal; // <-- Agora usa a variável correta
                    gravador.NomeDoCanal = canais[i];

                    // Vincula canais a placas físicas disponíveis diferentes. Se houver apenas 1, mapeia no canal 0.
                    int placaID = i < numPlacasDisponiveis ? i : 0;
                    gravador.IniciarEscuta(placaID);

                    _gravadoresAtivos.Add(canais[i], gravador);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro de hardware ao iniciar os motores de áudio: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Move as barras do espectro de áudio criando a simbologia visual de gravação ativa.
        /// </summary>
        private void IniciarAnimacaoEspectro()
        {
            _timerEspectro = new DispatcherTimer();
            _timerEspectro.Interval = TimeSpan.FromMilliseconds(120);
            _timerEspectro.Tick += (s, e) =>
            {
                BarraA1.Height = _random.Next(5, 45);
                BarraA2.Height = _random.Next(10, 60);
                BarraA3.Height = _random.Next(5, 40);
                BarraA4.Height = _random.Next(20, 60);
                BarraA5.Height = _random.Next(5, 50);
                BarraA6.Height = _random.Next(15, 60);
                BarraA7.Height = _random.Next(5, 40);
                BarraA8.Height = _random.Next(10, 55);

                LedGravando.Opacity = LedGravando.Opacity == 1 ? 0.2 : 1;
            };
            _timerEspectro.Start();
        }

        /// <summary>
        /// Protege o menu de configurações técnicas contra acessos não autorizados de operadores.
        /// </summary>
        private void MenuConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_nivelAcessoUsuario != "Administrador")
            {
                MessageBox.Show("Acesso Negado! Apenas Administradores podem mapear placas, alterar sensibilidade de canais e configurar volumes.",
                                "Segurança Engeradios", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Abre a nova janela de configurações se o utilizador for Administrador
            ConfiguracoesWindow telaConfig = new ConfiguracoesWindow();
            telaConfig.Owner = this; // Bloqueia a janela principal enquanto esta estiver aberta
            telaConfig.ShowDialog();
        }

        // --- SISTEMA INTERNO DE ALTERNÂNCIA DE TEMAS DE PRODUÇÃO ---
        private void MenuTema_Click(object sender, RoutedEventArgs e)
        {
            _isTemaEscuro = !_isTemaEscuro;
            AplicarTema();
        }

        private void AplicarTema()
        {
            // Abordagem à prova de falhas: procura a imagem na pasta exata onde o .exe está a ser executado.
            string pastaBase = AppDomain.CurrentDomain.BaseDirectory;

            if (_isTemaEscuro)
            {
                JanelaPrincipal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                MenuSuperior.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                MenuSuperior.Foreground = Brushes.White;
                MenuTema.Header = "☀️ Tema Claro";

                CabecalhoBorder.Background = Brushes.Black;

                // Carrega logo escuro pelo caminho absoluto
                string logoEscuroPath = Path.Combine(pastaBase, "logo_escuro.png");
                if (File.Exists(logoEscuroPath))
                {
                    ImgLogoPrincipal.Source = new BitmapImage(new Uri(logoEscuroPath, UriKind.Absolute));
                }

                AreaControlos.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                LblCanais.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                LblCanaisLista.Foreground = Brushes.White;

                GrupoHistorico.Foreground = Brushes.White;
                GrupoHistorico.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));

                GridHistorico.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                GridHistorico.RowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                GridHistorico.AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));

                RodapeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
                LblUsoDisco.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                TxtUsoDisco.Foreground = Brushes.White;
                TxtOperadorLogado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                TxtEstadoGravacao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            }
            else
            {
                JanelaPrincipal.Background = new SolidColorBrush(Colors.WhiteSmoke);
                MenuSuperior.Background = new SolidColorBrush(Colors.LightGray);
                MenuSuperior.Foreground = Brushes.Black;
                MenuTema.Header = "🌙 Tema Escuro";

                CabecalhoBorder.Background = Brushes.White;

                // Carrega logo claro pelo caminho absoluto
                string logoClaroPath = Path.Combine(pastaBase, "logo_claro.png");
                if (File.Exists(logoClaroPath))
                {
                    ImgLogoPrincipal.Source = new BitmapImage(new Uri(logoClaroPath, UriKind.Absolute));
                }

                AreaControlos.Background = Brushes.White;
                LblCanais.Foreground = Brushes.DarkGray;
                LblCanaisLista.Foreground = Brushes.Black;

                GrupoHistorico.Foreground = Brushes.Black;
                GrupoHistorico.BorderBrush = Brushes.LightGray;

                GridHistorico.Background = Brushes.White;
                GridHistorico.RowBackground = Brushes.White;
                GridHistorico.AlternatingRowBackground = new SolidColorBrush(Colors.WhiteSmoke);

                RodapeBorder.Background = new SolidColorBrush(Colors.LightGray);
                LblUsoDisco.Foreground = Brushes.DarkGray;
                TxtUsoDisco.Foreground = Brushes.Black;
                TxtOperadorLogado.Foreground = Brushes.DarkGray;
                TxtEstadoGravacao.Foreground = Brushes.DarkGray;
            }
        }

        // --- MONITORAMENTO DE REDE E DISCO ---
        private void ConfigurarMonitoramentoSegundoPlano()
        {
            _timerMonitoramento = new DispatcherTimer();
            _timerMonitoramento.Interval = TimeSpan.FromSeconds(30);
            _timerMonitoramento.Tick += TimerMonitoramento_Tick;
            _timerMonitoramento.Start();
            TimerMonitoramento_Tick(this, EventArgs.Empty);
        }

        private async void TimerMonitoramento_Tick(object? sender, EventArgs e)
        {
            try
            {
                string raiz = Path.GetPathRoot(_pastaGravacoes) ?? "C:\\";
                DriveInfo disco = new DriveInfo(raiz);
                double espacoLivreGb = disco.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);

                double espacoUsadoNaPastaGb = 1.5;
                double quotaMaximaGb = 40.0;

                if (BarraDisco != null && TxtUsoDisco != null)
                {
                    BarraDisco.Maximum = quotaMaximaGb;
                    BarraDisco.Value = espacoUsadoNaPastaGb;
                    TxtUsoDisco.Text = $"{espacoUsadoNaPastaGb:F1} GB / {quotaMaximaGb} GB";

                    if (espacoUsadoNaPastaGb >= quotaMaximaGb * 0.9)
                        BarraDisco.Foreground = new SolidColorBrush(Colors.Red);
                }

                if (_telemetryClient != null && LedNuvem != null && TxtEstadoNuvem != null)
                {
                    await _telemetryClient.EnviarHeartbeatAsync("CHAVE_CLIENTE_EXEMPLO", espacoLivreGb);
                    LedNuvem.Fill = new SolidColorBrush(Colors.LimeGreen);
                    TxtEstadoNuvem.Text = "Nuvem: Online";
                }
            }
            catch (Exception)
            {
                if (LedNuvem != null && TxtEstadoNuvem != null)
                {
                    LedNuvem.Fill = new SolidColorBrush(Colors.Red);
                    TxtEstadoNuvem.Text = "Nuvem: Offline";
                }
            }
        }

        private void AtualizarHistorico()
        {
            if (_databaseService != null && GridHistorico != null)
            {
                var lista = _databaseService.ObterHistorico();
                GridHistorico.ItemsSource = lista;
            }
        }

        // --- GESTÃO DE FECHAMENTO COM VALIDAÇÃO DE SENHA ---
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_nivelAcessoUsuario == "Operador")
            {
                bool autorizado = MostrarPromptSenhaAdmin();
                if (!autorizado)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                // Encerramento limpo efetuado por Administrador técnico
                foreach (var canal in _gravadoresAtivos)
                {
                    canal.Value.PararEscuta();
                }
            }
        }

        private bool MostrarPromptSenhaAdmin()
        {
            bool senhaCorreta = false;
            Window prompt = new Window()
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

            StackPanel painel = new StackPanel() { Margin = new Thickness(20) };
            TextBlock txtTitulo = new TextBlock() { Text = "Acesso Restrito", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) };
            TextBlock txtLabel = new TextBlock() { Text = "Para fechar o gravador, um Administrador\ndeve introduzir a sua palavra-passe:", Foreground = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 15) };
            PasswordBox pwdBox = new PasswordBox() { Padding = new Thickness(5), FontSize = 16, Margin = new Thickness(0, 0, 0, 20) };
            Button btnOk = new Button() { Content = "AUTORIZAR SAÍDA", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000")), Foreground = Brushes.White, Height = 35, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };

            btnOk.Click += (s, ev) =>
            {
                if (pwdBox.Password == "admin")
                {
                    senhaCorreta = true;
                    foreach (var canal in _gravadoresAtivos) canal.Value.PararEscuta();
                    prompt.Close();
                }
                else
                {
                    MessageBox.Show("Senha de administrador incorreta!", "Erro de Segurança", MessageBoxButton.OK, MessageBoxImage.Error);
                    pwdBox.Clear();
                }
            };

            painel.Children.Add(txtTitulo); painel.Children.Add(txtLabel); painel.Children.Add(pwdBox); painel.Children.Add(btnOk);
            prompt.Content = painel;
            prompt.ShowDialog();

            return senhaCorreta;
        }

        // --- OUTRAS ABAS DO MENU DE SUPORTE ---
        private void MenuAtualizar_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Buscando atualizações na nuvem KingHost...", "Atualizador", MessageBoxButton.OK, MessageBoxImage.Information); }
        private void MenuRemoto_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Iniciando módulo de conexão remota para a equipa técnica da Engeradios...", "Suporte Remoto", MessageBoxButton.OK, MessageBoxImage.Information); }
        private void MenuSuporte_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.engeradios.com.br/suporte", UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show($"Não foi possível abrir o navegador: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}