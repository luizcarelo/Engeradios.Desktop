// Caminho do arquivo: Engeradios.Desktop/Services/LogService.cs

using System;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    public class LogService
    {
        // Correção (IDE0044): Campo tornado somente leitura
        private readonly DatabaseService _dbService;

        public LogService()
        {
            // Correção (IDE0090): Expressão new simplificada
            _dbService = new();
        }

        public void Registar(string utilizador, string acao, string nivelCriticidade = "Info")
        {
            // Correção (CS0246): O nome correto do modelo é LogAuditoria
            var log = new LogAuditoria
            {
                DataHora = DateTime.Now,
                Operador = utilizador,
                Acao = acao,
                Severidade = nivelCriticidade
            };

            _dbService.InserirLog(log);

            Console.WriteLine($"[{log.DataHora}] {nivelCriticidade} | {utilizador}: {acao}");
        }
    }
}