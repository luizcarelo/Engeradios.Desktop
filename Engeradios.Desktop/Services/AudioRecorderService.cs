using System;
using System.IO;
using NAudio.Wave;

namespace Engeradios.Desktop.Services
{
    public class AudioRecorderService
    {
        // O '= null!' diz ao .NET que estas variáveis vão ser instanciadas depois
        private WaveInEvent _waveIn = null!;
        private WaveFileWriter _writer = null!;
        private string _arquivoAtual = string.Empty;
        private string _pastaDestino;
        private bool _estaGravando = false;
        private DateTime _ultimoMomentoComSom;

        public float Sensibilidade { get; set; } = 0.05f;
        public string NomeDoCanal { get; set; } = "Canal";
        public int SegundosDeEspera { get; set; } = 3;

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
            _waveIn = new WaveInEvent();
            _waveIn.DeviceNumber = deviceNumber;
            _waveIn.WaveFormat = new WaveFormat(8000, 1);

            // Ligamos o evento ao método abaixo
            _waveIn.DataAvailable += AoReceberDadosDeAudio;
            _waveIn.StartRecording();
        }

        public void PararEscuta()
        {
            if (_waveIn != null!)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null!;
            }
            if (_writer != null!)
            {
                _writer.Dispose();
                _writer = null!;
            }
        }

        // CORREÇÃO: O object? diz que o sender pode vir nulo por baixo dos panos (padrão C# 10)
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
                _writer.Write(e.Buffer, 0, e.BytesRecorded);

                if ((DateTime.Now - _ultimoMomentoComSom).TotalSeconds > SegundosDeEspera)
                {
                    PararGravacaoAtual();
                }
            }
        }

        private void IniciarNovoArquivoDeAudio()
        {
            string nomeArquivo = $"Gravação_{NomeDoCanal}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.wav";
            _arquivoAtual = Path.Combine(_pastaDestino, nomeArquivo);
            _writer = new WaveFileWriter(_arquivoAtual, _waveIn.WaveFormat);
            _estaGravando = true;
        }

        private void PararGravacaoAtual()
        {
            if (_writer != null!)
            {
                _writer.Dispose();
                _writer = null!;
            }
            _estaGravando = false;
        }
    }
}