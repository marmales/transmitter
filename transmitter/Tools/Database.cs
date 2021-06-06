using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace transmitter.Tools
{
    public static class SqliteFactory
    {
        public static IServiceCollection AddSqlite(this IServiceCollection service, string connectionKey = "Sqlite")
        {
            service.AddSingleton(di =>
            {
                var connectionString = di.GetRequiredService<IConfiguration>().GetConnectionString(connectionKey);
                var connection =
                    new SQLiteConnection(Environment.ExpandEnvironmentVariables(connectionString));
                return ActivatorUtilities.CreateInstance<Database>(di, connection);
            });
            return service;
        }
    }
    public class Database
    {
        private readonly ConcurrentDictionary<string, List<string>> _cache = new();
        private readonly IDbConnection _connection;
        private readonly ILogger<Database> _logger;
        private const string TableName = "sent_messages";

        public Database(IDbConnection connection, ILogger<Database> logger)
        {
            _connection = connection;
            _logger = logger;
            CheckDatabase(connection);
        }

        private static readonly object Lock = new();
        public bool IsNew(string messageId, string time)
        {
            lock (Lock)
            {
                if (_cache.IsEmpty)
                    InitCache();
                
                var list = _cache.GetOrAdd(messageId, _ => new List<string>());
                if (list.Contains(time))
                    return false;
                list.Add(time);
                return true;
            }
        }

        private void InitCache()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = $"SELECT id, time FROM {TableName}";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var list = _cache.GetOrAdd(reader["id"].ToString(), _ => new List<string>());
                list.Add(reader["time"].ToString());
            } 
        }
        public Action SaveMessage(string messageId, string sentTime)
        {
            _logger.LogInformation("Storing message in database.");
            var transaction = _connection.BeginTransaction();
            var command = transaction.Connection?.CreateCommand()
                                ?? throw new Exception("Cannot create database command;");
            command.CommandText = $"INSERT INTO {TableName} (id, time) Values (@id, @time)";
            var id = command.CreateParameter();
            id.ParameterName = "@id";
            id.Value = messageId;
            command.Parameters.Add(id);
            var time = command.CreateParameter();
            time.ParameterName = "@time";
            time.Value = sentTime;
            command.Parameters.Add(time);
            _logger.LogInformation("New database record prepared. Awaiting for action call to commit result.");
            return () =>
            {
                try
                {
                    command.ExecuteNonQuery();
                    transaction.Commit();
                    command.Dispose();
                    transaction.Dispose();
                    _logger.LogInformation("Database save complete.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Database save failed.");
                    throw;
                }
            };
        }
        private static void CheckDatabase(IDbConnection connection)
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{TableName}'";
            var exist = command.ExecuteScalar()
                ?? throw new Exception($"Unexpected execute scalar response from  'SELECT COUNT(*) FROM sqlite_master .... '.");
            
            if (Convert.ToBoolean(exist))
                return;
            command.CommandText = $"CREATE TABLE {TableName} (id TEXT, time TEXT)";
            command.ExecuteScalar();
        }
    }
}