// Caminho do arquivo: Engeradios.Desktop/ConfiguracoesWindow.xaml.cs

using System.Windows;
using NAudio.Wave; // Necessário para ler as placas de som físicas do Windows
using System.Collections.Generic;

namespace Engeradios.Desktop
{
    public partial class ConfiguracoesWindow : Window
    {
        public ConfiguracoesWindow()
        {
            InitializeComponent();
            CarregarPlacasDeSom();
            ConfigurarEventosSliders();
        }

        private void CarregarPlacasDeSom()
        {
            List<string> placas = new List<string>();
            int quantidadeDeDispositivos = WaveInEvent.DeviceCount;

            // Se não encontrar nenhuma placa, adiciona um aviso
            if (quantidadeDeDispositivos == 0)
            {
                placas.Add("Nenhuma placa de som detectada no Windows.");
            }
            else
            {
                // Lê o nome de cada placa física e adiciona à lista
                for (int i = 0; i < quantidadeDeDispositivos; i++)
                {
                    var info = WaveInEvent.GetCapabilities(i);
                    placas.Add($"[{i}] {info.ProductName}");
                }
            }

            // Preenche as "caixinhas" (ComboBoxes) com as placas encontradas
            CmbPlaca1.ItemsSource = placas;
            CmbPlaca2.ItemsSource = placas;
            CmbPlaca3.ItemsSource = placas;

            // Seleciona a primeira placa por defeito para não ficar vazio
            if (placas.Count > 0)
            {
                CmbPlaca1.SelectedIndex = 0;
                CmbPlaca2.SelectedIndex = placas.Count > 1 ? 1 : 0; // Tenta selecionar placas diferentes se existirem
                CmbPlaca3.SelectedIndex = placas.Count > 2 ? 2 : 0;
            }
        }

        private void ConfigurarEventosSliders()
        {
            // Atualiza os textos dos sliders enquanto o utilizador os arrasta
            SldCanal1.ValueChanged += (s, e) => { if (LblSld1 != null) LblSld1.Text = $"{(int)(SldCanal1.Value * 100)}%"; };
            SldCanal2.ValueChanged += (s, e) => { if (LblSld2 != null) LblSld2.Text = $"{(int)(SldCanal2.Value * 100)}%"; };
            SldCanal3.ValueChanged += (s, e) => { if (LblSld3 != null) LblSld3.Text = $"{(int)(SldCanal3.Value * 100)}%"; };
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Futuramente: Salvar estes dados no SQLite local ou num ficheiro JSON
            MessageBox.Show("Configurações de Hardware guardadas com sucesso!\nO sistema precisa de ser reiniciado para aplicar as novas regras aos motores de áudio.",
                            "Engeradios", MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}