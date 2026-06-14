// Caminho do arquivo: Engeradios.Desktop/AudioPlayerWindow.xaml.cs

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Engeradios.Desktop
{
    public partial class AudioPlayerWindow : Window
    {
        private MediaPlayer _player = new MediaPlayer();
        private DispatcherTimer _timer;
        private bool _isDragging = false;
        private bool _isPlaying = true;

        public AudioPlayerWindow(string caminhoFicheiro, string canal, string dataStr)
        {
            InitializeComponent();

            TxtCanal.Text = $"Canal: {canal}";
            TxtData.Text = dataStr;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;

            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += Player_MediaEnded;

            _player.Open(new Uri(caminhoFicheiro, UriKind.Absolute));
            _player.Play();
            _timer.Start();
        }

        private void Player_MediaOpened(object? sender, EventArgs e)
        {
            if (_player.NaturalDuration.HasTimeSpan)
            {
                SldProgresso.Maximum = _player.NaturalDuration.TimeSpan.TotalSeconds;
                TxtTempoTotal.Text = _player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isDragging && _player.NaturalDuration.HasTimeSpan)
            {
                SldProgresso.Value = _player.Position.TotalSeconds;
                TxtTempoAtual.Text = _player.Position.ToString(@"mm\:ss");
            }
        }

        private void SldProgresso_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDragging = true;
        }

        private void SldProgresso_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDragging = false;
            _player.Position = TimeSpan.FromSeconds(SldProgresso.Value);
        }

        private void SldProgresso_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Permite clicar na barra para avançar (sem arrastar)
            if (!_isDragging)
                _player.Position = TimeSpan.FromSeconds(SldProgresso.Value);
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                _player.Pause();
                BtnPlayPause.Content = "▶ REPRODUZIR";
                BtnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
            }
            else
            {
                _player.Play();
                BtnPlayPause.Content = "⏸ PAUSAR";
                BtnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC0000"));
            }
            _isPlaying = !_isPlaying;
        }

        private void Player_MediaEnded(object? sender, EventArgs e)
        {
            _player.Stop();
            _isPlaying = false;
            BtnPlayPause.Content = "▶ REINICIAR";
            BtnPlayPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
            SldProgresso.Value = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // MUITO IMPORTANTE: Fecha o ficheiro para não dar erro de "File in use" no Windows
            _timer.Stop();
            _player.Stop();
            _player.Close();
        }
    }
}