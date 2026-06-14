// Caminho do arquivo: Engeradios.Desktop/ConfiguracoesWindow.xaml.cs

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using NAudio.Wave;
using Engeradios.Desktop.Services;

namespace Engeradios.Desktop
{
    public class CanalConfigModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _nome = "Novo Canal";
        public string Nome { get => _nome; set { _nome = value; OnPropertyChanged(nameof(Nome)); } }

        private int _placaIndex = 0;
        public int PlacaIndex { get => _placaIndex; set { _placaIndex = value; OnPropertyChanged(nameof(PlacaIndex)); } }

        private float _vad = 0.05f;
        public float VAD { get => _vad; set { _vad = value; OnPropertyChanged(nameof(VAD)); OnPropertyChanged(nameof(VADPercent)); OnPropertyChanged(nameof(CorBarraAudio)); } }
        public string VADPercent => $"{(int)(VAD * 100)}%";

        private float _volume = 1.0f;
        public float Volume { get => _volume; set { _volume = value; OnPropertyChanged(nameof(Volume)); OnPropertyChanged(nameof(VolumePercent)); } }
        public string VolumePercent => $"{(int)(Volume * 100)}%";

        private int _nivelAudio = 0;
        public int NivelAudio { get => _nivelAudio; set { _nivelAudio = value; OnPropertyChanged(nameof(NivelAudio)); OnPropertyChanged(nameof(CorBarraAudio)); } }

        public SolidColorBrush CorBarraAudio => NivelAudio > (VAD * 100) ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.DimGray);

        public WaveInEvent? TestadorAudio { get; set; }
    }

    public class DispositivoAudio
    {
        public int Index { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public partial class ConfiguracoesWindow : Window
    {
        public ObservableCollection<CanalConfigModel> Canais { get; set; }
        public List<DispositivoAudio> DispositivosDisponiveis { get; set; }

        public ConfiguracoesWindow(bool isAuthenticated = false)
        {
            if (!isAuthenticated)
            {
                MessageBox.Show("Acesso negado. Por favor, efetue o login primeiro.", "Autenticação Necessária", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            InitializeComponent();

            Canais = new ObservableCollection<CanalConfigModel>();
            DispositivosDisponiveis = new List<DispositivoAudio>();

            this.DataContext = this;

            CarregarPlacasDoWindows();
            CarregarCanaisExistentes();
        }

        private void CarregarPlacasDoWindows()
        {
            try
            {
                int qtd = WaveInEvent.DeviceCount;
                for (int i = 0; i < qtd; i++)
                {
                    var info = WaveInEvent.GetCapabilities(i);
                    DispositivosDisponiveis.Add(new DispositivoAudio { Index = i, Nome = info.ProductName });
                }

                if (qtd == 0)
                    DispositivosDisponiveis.Add(new DispositivoAudio { Index = -1, Nome = "Nenhuma placa de som detetada" });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao detetar hardware de áudio: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CarregarCanaisExistentes()
        {
            var configuracoesSalvas = ConfigService.CarregarCanais();

            foreach (var config in configuracoesSalvas)
            {
                AdicionarCanal(config.Nome, config.PlacaIndex, config.VAD, config.Volume);
            }

            ListaCanaisUI.ItemsSource = Canais;
        }

        private void AdicionarCanal(string nome, int placaPadrao, float vad = 0.05f, float volume = 1.0f)
        {
            var novoCanal = new CanalConfigModel
            {
                Nome = nome,
                PlacaIndex = placaPadrao,
                VAD = vad,
                Volume = volume
            };

            Canais.Add(novoCanal);
            IniciarPreviewDeAudio(novoCanal);
        }

        private void IniciarPreviewDeAudio(CanalConfigModel canal)
        {
            try
            {
                PararTestadorEspecifico(canal);

                if (canal.PlacaIndex < 0) return;

                canal.TestadorAudio = new WaveInEvent();
                canal.TestadorAudio.DeviceNumber = canal.PlacaIndex;
                canal.TestadorAudio.WaveFormat = new WaveFormat(8000, 1);

                canal.TestadorAudio.DataAvailable += (s, e) =>
                {
                    float max = 0;
                    for (int index = 0; index < e.BytesRecorded; index += 2)
                    {
                        short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                        var sample32 = sample / 32768f;
                        if (sample32 < 0) sample32 = -sample32;
                        if (sample32 > max) max = sample32;
                    }

                    float somComGanho = max * canal.Volume;
                    if (somComGanho > 1f) somComGanho = 1f;

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        canal.NivelAudio = (int)(somComGanho * 100);
                    });
                };

                canal.TestadorAudio.StartRecording();
            }
            catch (Exception)
            {
                canal.NivelAudio = 0;
            }
        }

        private void PararTestadorEspecifico(CanalConfigModel canal)
        {
            if (canal.TestadorAudio != null)
            {
                try
                {
                    canal.TestadorAudio.StopRecording();
                    canal.TestadorAudio.Dispose();
                }
                catch { }
                finally
                {
                    canal.TestadorAudio = null;
                }
            }
        }

        private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
        {
            AdicionarCanal("Novo Canal", 0);
        }

        private void BtnRemoverCanal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is CanalConfigModel canal)
            {
                PararTestadorEspecifico(canal);
                Canais.Remove(canal);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is CanalConfigModel canal)
            {
                IniciarPreviewDeAudio(canal);
            }
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            var listaParaSalvar = new List<CanalHardwareConfig>();
            foreach (var c in Canais)
            {
                listaParaSalvar.Add(new CanalHardwareConfig
                {
                    Nome = c.Nome,
                    PlacaIndex = c.PlacaIndex,
                    VAD = c.VAD,
                    Volume = c.Volume
                });
            }

            ConfigService.SalvarCanais(listaParaSalvar);

            MessageBox.Show($"As definições de {Canais.Count} canais foram guardadas com sucesso!\nO sistema principal será agora reiniciado.",
                            "Configurações Salvas", MessageBoxButton.OK, MessageBoxImage.Information);

            PararTodosOsTestadores();
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            PararTodosOsTestadores();
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            PararTodosOsTestadores();
        }

        private void PararTodosOsTestadores()
        {
            foreach (var canal in Canais)
            {
                PararTestadorEspecifico(canal);
            }
        }
    }
}