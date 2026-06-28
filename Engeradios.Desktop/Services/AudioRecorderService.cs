// Caminho do arquivo: Engeradios.Desktop/Services/AudioRecorderService.cs

using Engeradios.Desktop.Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO;

namespace Engeradios.Desktop.Services
{
    public class AudioRecorderService
    {
        private WaveInEvent? _waveIn; // Alterado para anulável para tratar avisos
        private WaveFileWriter? _writer; // Alterado para anulável para tratar avisos
        private string _arquivoAtual = string.Empty;
        private readonly string _pastaDestino; // Marcado como readonly
        private bool _estaGravando = false;

        private DateTime _ultimoMomentoComSom;
        private DateTime _dataHoraInicioGravacao;

        private float _picoAtualParaUI = 0f;

        public float Sensibilidade { get; set; } = 0.05f;
        public string NomeDoCanal { get; set; } = "Canal";
        public int SegundosDeEspera { get; set; } = 3;

        // Exposição do estado real de gravação para a Interface Gráfica
        public bool IsRecording => _estaGravando;

        // Leitura do pico máximo desde a última vez que a UI verificou
        public float ObterPicoE_Resetar()
        {
            float pico = _picoAtualParaUI;
            _picoAtualParaUI = 0f; // Prepara para acumular o próximo pico
            return pico;
        }

        public event EventHandler<RegistoAudio>? GravacaoFinalizada;

        public AudioRecorderService(string pastaDestino)
        {
            _pastaDestino = pastaDestino;
            if (!Directory.Exists(_pastaDestino))
            {
                Directory.CreateDirectory(_pastaDestino);
            }

            // Inicializa a API do Media Foundation (Necessária para conversão MP3 nativa do Windows)
            try
            {
                MediaFoundationApi.Startup();
            }
            catch
            {
                /* Pode já estar inicializada, ignora o erro silenciosamente */
            }
        }

        public void IniciarEscuta(int deviceNumber = 0)
        {
            try
            {
                // Inicialização do objeto simplificada
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(8000, 1)
                };

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
                try
                {
                    _waveIn.StopRecording();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Aviso ao parar placa de som: {ex.Message}");
                }
                finally
                {
                    _waveIn.DataAvailable -= AoReceberDadosDeAudio;
                    _waveIn.Dispose();
                    _waveIn = null; // Removido o '!' para evitar forçar tipo
                }
            }
        }

        private void AoReceberDadosDeAudio(object? sender, WaveInEventArgs e)
        {
            float max = 0;

            // Prevenção de desreferência possivelmente nula
            if (e.Buffer == null) return;

            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            // Acumula o pico mais alto para a barra de som da UI
            if (max > _picoAtualParaUI)
            {
                _picoAtualParaUI = max;
            }

            // Validação do VAD (Voice Activity Detection)
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
                    _writer?.Write(e.Buffer, 0, e.BytesRecorded); // Null-conditional já previne erros
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
                if (_waveIn == null) return; // Evita desreferência nula

                _dataHoraInicioGravacao = DateTime.Now;

                // Substring simplificado com Range Operator [..4]
                string uid = Guid.NewGuid().ToString("N")[..4].ToUpper();

                // Começamos por gravar num ficheiro WAV temporário para máxima performance (sem bloquear a thread)
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
                    _writer.Flush();
                    _writer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao fechar ficheiro de áudio: {ex.Message}");
                }
                finally
                {
                    _writer = null;
                }

                string wavTemp = _arquivoAtual;
                string mp3Final = Path.ChangeExtension(wavTemp, ".mp3");

                try
                {
                    // ETAPA 4: Converte o ficheiro WAV pesado num MP3 leve logo após o rádio fechar o PTT
                    using (var reader = new WaveFileReader(wavTemp))
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, mp3Final);
                    }

                    // Se a conversão for bem-sucedida, elimina o WAV para poupar espaço
                    if (File.Exists(wavTemp))
                    {
                        File.Delete(wavTemp);
                    }

                    // Atualiza o caminho para apontar para o ficheiro MP3 gerado
                    _arquivoAtual = mp3Final;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Aviso: Falha na compressão para MP3, a manter o ficheiro original WAV. Erro: {ex.Message}");
                    // O caminho continua a ser o ficheiro WAV original como fallback
                }

                int duracaoSegundos = (int)(DateTime.Now - _dataHoraInicioGravacao).TotalSeconds;

                var registo = new RegistoAudio
                {
                    Canal = NomeDoCanal,
                    DataHoraGravacao = _dataHoraInicioGravacao,
                    CaminhoFicheiro = _arquivoAtual, // Agora aponta para o MP3 (ou WAV em caso de fallback)
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