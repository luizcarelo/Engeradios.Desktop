// Caminho do arquivo: Engeradios.Desktop/AuditoriaWindow.xaml.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Engeradios.Desktop.Models;
using Engeradios.Desktop.Services;

namespace Engeradios.Desktop
{
    public partial class AuditoriaWindow : Window
    {
        private readonly DatabaseService _dbService;
        private List<LogAuditoria> _todosOsLogs = []; // Correção IDE0028

        public AuditoriaWindow()
        {
            InitializeComponent();
            _dbService = new(); // Correção IDE0090
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarLogs();
        }

        private void CarregarLogs()
        {
            _todosOsLogs = _dbService.ObterLogs();
            GridLogs.ItemsSource = _todosOsLogs;
        }

        private void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = TxtFiltro.Text.Trim(); // Correção CA1862: Removido o ToLower()

            if (string.IsNullOrEmpty(filtro))
            {
                GridLogs.ItemsSource = _todosOsLogs;
            }
            else
            {
                // Correção CA1862: Utilização de OrdinalIgnoreCase para não alocar memória extra
                var logsFiltrados = _todosOsLogs.Where(l =>
                    l.Operador.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    l.Acao.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    l.Severidade.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                GridLogs.ItemsSource = logsFiltrados;
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string caminhoPasta = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string nomeArquivo = $"Relatorio_Auditoria_C3_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                string caminhoCompleto = Path.Combine(caminhoPasta, nomeArquivo);

                using (StreamWriter sw = new(caminhoCompleto)) // Correção IDE0090
                {
                    sw.WriteLine("DATA_HORA;CRITICIDADE;UTILIZADOR;ACAO");
                    foreach (var log in _todosOsLogs)
                    {
                        string acaoLimpa = log.Acao.Replace(";", ",");
                        sw.WriteLine($"{log.DataHora:dd/MM/yyyy HH:mm:ss};{log.Severidade};{log.Operador};{acaoLimpa}");
                    }
                }

                MessageBox.Show($"Relatório de Auditoria exportado para o seu Ambiente de Trabalho (Desktop).\n\nFicheiro: {nomeArquivo}", "Exportação Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro ao exportar o ficheiro: {ex.Message}", "Erro na Exportação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}