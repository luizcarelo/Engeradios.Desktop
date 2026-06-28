// Caminho do arquivo: Engeradios.Desktop/ViewModels/MainViewModel.cs

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Services;

namespace Engeradios.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly LogService _logService;

        // Propriedades parciais recomendadas pelo MVVM Toolkit moderno (C# 13+)
        [ObservableProperty]
        public partial string NomeUsuarioLogado { get; set; } = "A carregar...";

        [ObservableProperty]
        public partial string NivelAcessoUsuario { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsTemaEscuro { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<RegistoAudio> HistoricoAudios { get; set; } = new();

        public MainViewModel(DatabaseService dbService, LogService logService)
        {
            _dbService = dbService;
            _logService = logService;
        }

        public async void InicializarSessao(string nomeUsuario, string nivelAcesso, bool temaEscuro)
        {
            NomeUsuarioLogado = nomeUsuario;
            NivelAcessoUsuario = nivelAcesso;
            IsTemaEscuro = temaEscuro;

            await AtualizarHistoricoAsync();
        }

        [RelayCommand]
        public async Task AtualizarHistoricoAsync()
        {
            var registros = await _dbService.ObterHistoricoAsync();

            // Limpa e recarrega a lista para a UI de forma assíncrona
            HistoricoAudios.Clear();
            foreach (var reg in registros.Take(150))
            {
                HistoricoAudios.Add(reg);
            }
        }
    }
}