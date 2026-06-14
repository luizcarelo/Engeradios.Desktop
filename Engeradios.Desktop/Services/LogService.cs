using System;
using Microsoft.Data.Sqlite;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    public class LogService
    {
        private string _connectionString = "Data Source=engeradios_local.db;";

        public LogService()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Logs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DataHora TEXT NOT NULL,
                        Operador TEXT NOT NULL,
                        Acao TEXT NOT NULL,
                        Severidade TEXT NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }
        }

        public void Registar(string operador, string acao, string severidade = "Info")
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Logs (DataHora, Operador, Acao, Severidade) VALUES ($data, $op, $acao, $sev);";
                cmd.Parameters.AddWithValue("$data", DateTime.Now.ToString("O"));
                cmd.Parameters.AddWithValue("$op", operador);
                cmd.Parameters.AddWithValue("$acao", acao);
                cmd.Parameters.AddWithValue("$sev", severidade);
                cmd.ExecuteNonQuery();
            }
        }
    }
}