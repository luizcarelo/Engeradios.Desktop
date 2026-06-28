// Caminho do arquivo: Engeradios.Desktop/Services/DatabaseService.cs

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Engeradios.Desktop.Models;

namespace Engeradios.Desktop.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=engeradios_local.db;";

        public DatabaseService()
        {
            InicializarBaseDeDados();
        }

        // Utilitário interno seguro para não depender de ficheiros externos
        private static string GerarHashSenha(string senhaPlana)
        {
            if (string.IsNullOrEmpty(senhaPlana)) return string.Empty;

            byte[] bytesSenha = Encoding.UTF8.GetBytes(senhaPlana);
            byte[] hashBytes = SHA256.HashData(bytesSenha);

            return Convert.ToBase64String(hashBytes);
        }

        private void InicializarBaseDeDados()
        {
            using var connection = new SqliteConnection(_connectionString);
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
                    SincronizadoComNuvem INTEGER NOT NULL,
                    Protegido INTEGER DEFAULT 0
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Usuarios (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    NivelAcesso TEXT NOT NULL
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AuditoriaLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DataHora TEXT NOT NULL,
                    Utilizador TEXT NOT NULL,
                    Acao TEXT NOT NULL,
                    Criticidade TEXT NOT NULL,
                    SincronizadoComNuvem INTEGER DEFAULT 0
                );";
            command.ExecuteNonQuery();

            // Fase 2:
            // Não criar mais usuário admin com senha padrão.
            // Em banco novo, a tabela Usuarios fica vazia.
            // O primeiro administrador será criado pelo LoginWindow,
            // durante a configuração inicial.
        }

        // ====================================================================
        // MÉTODOS SÍNCRONOS (Usados pelas janelas de Configuração, Login, etc.)
        // ====================================================================

        public string? ValidarLogin(string username, string password)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT NivelAcesso FROM Usuarios WHERE Username = $user AND Password = $pass;";

            command.Parameters.AddWithValue("$user", username);
            command.Parameters.AddWithValue("$pass", GerarHashSenha(password));

            return command.ExecuteScalar()?.ToString();
        }

        public bool ExisteAlgumUsuario()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Usuarios;";

            long total = Convert.ToInt64(command.ExecuteScalar() ?? 0);
            return total > 0;
        }

        public bool CriarPrimeiroAdministrador(string username, string senha)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Usuarios;";
            long totalUsuarios = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0);

            if (totalUsuarios > 0)
            {
                return false;
            }

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Usuarios (Username, Password, NivelAcesso)
                VALUES ($user, $pass, 'Administrador');";

            command.Parameters.AddWithValue("$user", username);
            command.Parameters.AddWithValue("$pass", GerarHashSenha(senha));

            return command.ExecuteNonQuery() > 0;
        }

        public List<Usuario> ObterUsuarios()
        {
            var lista = new List<Usuario>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Username, NivelAcesso FROM Usuarios ORDER BY Id;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Usuario
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    NivelAcesso = reader.GetString(2)
                });
            }

            return lista;
        }

        public bool AdicionarUsuario(string username, string password, string nivelAcesso)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Usuarios WHERE Username = $user;";
            checkCmd.Parameters.AddWithValue("$user", username);

            if (Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0) > 0)
            {
                return false;
            }

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Usuarios (Username, Password, NivelAcesso) VALUES ($user, $pass, $nivel);";
            command.Parameters.AddWithValue("$user", username);
            command.Parameters.AddWithValue("$pass", GerarHashSenha(password));
            command.Parameters.AddWithValue("$nivel", nivelAcesso);

            command.ExecuteNonQuery();

            return true;
        }

        public void RemoverUsuario(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Usuarios WHERE Id = $id AND Username != 'admin';";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public List<LogAuditoria> ObterLogs(int limite = 500)
        {
            var lista = new List<LogAuditoria>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditoriaLogs ORDER BY Id DESC LIMIT $limite;";
            command.Parameters.AddWithValue("$limite", limite);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new LogAuditoria
                {
                    Id = reader.GetInt32(0),
                    DataHora = DateTime.Parse(reader.GetString(1)),
                    Operador = reader.GetString(2),
                    Acao = reader.GetString(3),
                    Severidade = reader.GetString(4)
                });
            }

            return lista;
        }

        public List<LogAuditoria> ObterLogsPendentes()
        {
            var lista = new List<LogAuditoria>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM AuditoriaLogs WHERE SincronizadoComNuvem = 0 LIMIT 50;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new LogAuditoria
                {
                    Id = reader.GetInt32(0),
                    DataHora = DateTime.Parse(reader.GetString(1)),
                    Operador = reader.GetString(2),
                    Acao = reader.GetString(3),
                    Severidade = reader.GetString(4)
                });
            }

            return lista;
        }

        public void MarcarLogComoSincronizado(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE AuditoriaLogs SET SincronizadoComNuvem = 1 WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public void InserirLog(LogAuditoria log)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO AuditoriaLogs (DataHora, Utilizador, Acao, Criticidade) VALUES ($data, $user, $acao, $criticidade);";
            command.Parameters.AddWithValue("$data", log.DataHora.ToString("O"));
            command.Parameters.AddWithValue("$user", log.Operador);
            command.Parameters.AddWithValue("$acao", log.Acao);
            command.Parameters.AddWithValue("$criticidade", log.Severidade);

            command.ExecuteNonQuery();
        }

        public bool AtualizarSenhaUsuario(string username, string novaSenha)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Usuarios SET Password = $pass WHERE Username = $user;";
            command.Parameters.AddWithValue("$pass", GerarHashSenha(novaSenha));
            command.Parameters.AddWithValue("$user", username);

            return command.ExecuteNonQuery() > 0;
        }

        public void AtualizarAnotacao(int id, string anotacao)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE RegistroAudios SET Anotacoes = $nota WHERE Id = $id;";
            command.Parameters.AddWithValue("$nota", anotacao);
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public void AlternarProtecao(int id, bool protegido)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE RegistroAudios SET Protegido = $protegido WHERE Id = $id;";
            command.Parameters.AddWithValue("$protegido", protegido ? 1 : 0);
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public void RemoverRegisto(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM RegistroAudios WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public List<RegistoAudio> ObterCandidatosLimpeza(int limite = 50)
        {
            var lista = new List<RegistoAudio>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM RegistroAudios WHERE Protegido = 0 ORDER BY DataHoraGravacao ASC LIMIT $limite;";
            command.Parameters.AddWithValue("$limite", limite);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string? dataString = reader.IsDBNull(2) ? null : reader.GetString(2);

                lista.Add(new RegistoAudio
                {
                    Id = reader.GetInt32(0),
                    Canal = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    DataHoraGravacao = string.IsNullOrEmpty(dataString) ? DateTime.Now : DateTime.Parse(dataString),
                    CaminhoFicheiro = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    DuracaoSegundos = reader.GetInt32(4),
                    Anotacoes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    SincronizadoComNuvem = reader.GetInt32(6) == 1,
                    Protegido = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetInt32(7) == 1 : false
                });
            }

            return lista;
        }

        public List<RegistoAudio> ObterHistorico()
        {
            var lista = new List<RegistoAudio>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM RegistroAudios ORDER BY Id DESC LIMIT 200;";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string? dataString = reader.IsDBNull(2) ? null : reader.GetString(2);

                lista.Add(new RegistoAudio
                {
                    Id = reader.GetInt32(0),
                    Canal = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    DataHoraGravacao = string.IsNullOrEmpty(dataString) ? DateTime.Now : DateTime.Parse(dataString),
                    CaminhoFicheiro = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    DuracaoSegundos = reader.GetInt32(4),
                    Anotacoes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    SincronizadoComNuvem = reader.GetInt32(6) == 1,
                    Protegido = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetInt32(7) == 1 : false
                });
            }

            return lista;
        }

        // ====================================================================
        // MÉTODOS ASSÍNCRONOS (Usados pela MainWindow para não travar a UI)
        // ====================================================================

        public async Task<List<RegistoAudio>> ObterHistoricoAsync()
        {
            var lista = new List<RegistoAudio>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM RegistroAudios ORDER BY Id DESC LIMIT 200;";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string? dataString = reader.IsDBNull(2) ? null : reader.GetString(2);

                        lista.Add(new RegistoAudio
                        {
                            Id = reader.GetInt32(0),
                            Canal = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            DataHoraGravacao = string.IsNullOrEmpty(dataString) ? DateTime.Now : DateTime.Parse(dataString),
                            CaminhoFicheiro = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            DuracaoSegundos = reader.GetInt32(4),
                            Anotacoes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            SincronizadoComNuvem = reader.GetInt32(6) == 1,
                            Protegido = reader.FieldCount > 7 && !reader.IsDBNull(7) ? reader.GetInt32(7) == 1 : false
                        });
                    }
                }
            }

            return lista;
        }

        public async Task InserirRegistoAsync(RegistoAudio registo)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();

                command.CommandText = @"
                    INSERT INTO RegistroAudios (Canal, DataHoraGravacao, CaminhoFicheiro, DuracaoSegundos, Anotacoes, SincronizadoComNuvem, Protegido)
                    VALUES ($canal, $data, $caminho, $duracao, $anotacoes, $sincronizado, $protegido);";

                command.Parameters.AddWithValue("$canal", registo.Canal);
                command.Parameters.AddWithValue("$data", registo.DataHoraGravacao.ToString("O"));
                command.Parameters.AddWithValue("$caminho", registo.CaminhoFicheiro);
                command.Parameters.AddWithValue("$duracao", registo.DuracaoSegundos);
                command.Parameters.AddWithValue("$anotacoes", registo.Anotacoes ?? string.Empty);
                command.Parameters.AddWithValue("$sincronizado", registo.SincronizadoComNuvem ? 1 : 0);
                command.Parameters.AddWithValue("$protegido", registo.Protegido ? 1 : 0);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}