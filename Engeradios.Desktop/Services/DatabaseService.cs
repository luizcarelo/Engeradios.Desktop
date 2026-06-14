using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    public class DatabaseService
    {
        private string _connectionString = "Data Source=engeradios_local.db;";

        public DatabaseService()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RegistroAudios (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Canal TEXT NOT NULL,
                        DataHoraGravacao TEXT NOT NULL,
                        CaminhoFicheiro TEXT NOT NULL,
                        DuracaoSegundos INTEGER NOT NULL,
                        Anotacoes TEXT,
                        SincronizadoComNuvem INTEGER NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        public void InserirRegisto(RegistoAudio registo)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO RegistroAudios (Canal, DataHoraGravacao, CaminhoFicheiro, DuracaoSegundos, Anotacoes, SincronizadoComNuvem)
                    VALUES ($canal, $data, $caminho, $duracao, $anotacoes, $sincronizado);";

                command.Parameters.AddWithValue("$canal", registo.Canal);
                command.Parameters.AddWithValue("$data", registo.DataHoraGravacao.ToString("O"));
                command.Parameters.AddWithValue("$caminho", registo.CaminhoFicheiro);
                command.Parameters.AddWithValue("$duracao", registo.DuracaoSegundos);
                command.Parameters.AddWithValue("$anotacoes", registo.Anotacoes ?? string.Empty);
                command.Parameters.AddWithValue("$sincronizado", registo.SincronizadoComNuvem ? 1 : 0);

                command.ExecuteNonQuery();
            }
        }

        public List<RegistoAudio> ObterHistorico()
        {
            var lista = new List<RegistoAudio>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM RegistroAudios ORDER BY Id DESC LIMIT 50;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // CORREÇÃO: Lê o texto em segurança.
                        string? dataString = reader.IsDBNull(2) ? null : reader.GetString(2);

                        lista.Add(new RegistoAudio
                        {
                            Id = reader.GetInt32(0),
                            Canal = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            // Se a data vier nula do SQLite, colocamos a data de hoje para não dar erro
                            DataHoraGravacao = string.IsNullOrEmpty(dataString) ? DateTime.Now : DateTime.Parse(dataString),
                            CaminhoFicheiro = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            DuracaoSegundos = reader.GetInt32(4),
                            Anotacoes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            SincronizadoComNuvem = reader.GetInt32(6) == 1
                        });
                    }
                }
            }
            return lista;
        }
    }
}