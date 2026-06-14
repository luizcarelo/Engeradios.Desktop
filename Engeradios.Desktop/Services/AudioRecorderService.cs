// Caminho do arquivo: Engeradios.Desktop/Services/AudioRecorderService.cs

using System;
using System.IO;
using NAudio.Wave;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    public class AudioRecorderService
    {
        private WaveInEvent _waveIn = null!;
        private WaveFileWriter _writer = null!;
        private string _arquivoAtual = string.Empty;
        private string _pastaDestino;
        private bool _estaGravando = false;

        private DateTime _ultimoMomentoComSom;
        private DateTime _dataHoraInicioGravacao;

        public float Sensibilidade { get; set; } = 0.05f;
        public string NomeDoCanal { get; set; } = "Canal";
        public int SegundosDeEspera { get; set; } = 3;

        public event EventHandler<RegistoAudio>? GravacaoFinalizada;

        public AudioRecorderService(string pastaDestino)
        {
            _pastaDestino = pastaDestino;
            if (!Directory.Exists(_pastaDestino))
            {
                Directory.CreateDirectory(_pastaDestino);
            }
        }

        public void IniciarEscuta(int deviceNumber = 0)
        {
            try
            {
                _waveIn = new WaveInEvent();
                _waveIn.DeviceNumber = deviceNumber;
                _waveIn.WaveFormat = new WaveFormat(8000, 1);
                _waveIn.DataAvailable += AoReceberDadosDeAudio;
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar dispositivo {deviceNumber}: {ex.Message}");
            }
        }

        public void PararEscuta()
        {
            if (_estaGravando)
            {
                PararGravacaoAtual();
            }

            if (_waveIn != null)
            {
                try { _waveIn.StopRecording(); } catch { }
                _waveIn.Dispose();
                _waveIn = null!;
            }
        }

        private void AoReceberDadosDeAudio(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            if (max > Sensibilidade)
            {
                _ultimoMomentoComSom = DateTime.Now;

                if (!_estaGravando)
                {
                    IniciarNovoArquivoDeAudio();
                }
            }

            if (_estaGravando)
            {
                try
                {
                    _writer?.Write(e.Buffer, 0, e.BytesRecorded);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever áudio: {ex.Message}");
                }

                if ((DateTime.Now - _ultimoMomentoComSom).TotalSeconds > SegundosDeEspera)
                {
                    PararGravacaoAtual();
                }
            }
        }

        private void IniciarNovoArquivoDeAudio()
        {
            try
            {
                _dataHoraInicioGravacao = DateTime.Now;

                // CORREÇÃO: Adicionados os milissegundos (_fff) e um identificador aleatório de 4 letras para eliminar colisões.
                string uid = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                string nomeArquivo = $"Gravação_{NomeDoCanal}_{_dataHoraInicioGravacao:yyyy-MM-dd_HH-mm-ss_fff}_{uid}.wav";

                _arquivoAtual = Path.Combine(_pastaDestino, nomeArquivo);
                _writer = new WaveFileWriter(_arquivoAtual, _waveIn.WaveFormat);
                _estaGravando = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro crítico ao criar ficheiro de áudio: {ex.Message}");
            }
        }

        private void PararGravacaoAtual()
        {
            if (_writer != null)
            {
                try
                {
                    _writer.Dispose();
                }
                catch { }
                finally
                {
                    _writer = null!;
                }

                int duracaoSegundos = (int)(DateTime.Now - _dataHoraInicioGravacao).TotalSeconds;

                var registo = new RegistoAudio
                {
                    Canal = NomeDoCanal,
                    DataHoraGravacao = _dataHoraInicioGravacao,
                    CaminhoFicheiro = _arquivoAtual,
                    DuracaoSegundos = duracaoSegundos,
                    Anotacoes = string.Empty,
                    SincronizadoComNuvem = false
                };

                GravacaoFinalizada?.Invoke(this, registo);
            }
            _estaGravando = false;
        }
    }
}