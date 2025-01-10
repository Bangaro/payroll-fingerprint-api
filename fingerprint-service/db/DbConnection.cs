using System;
using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace fingerprint_service.db
{
    public class DbConnection
    {
        private readonly string _connectionString;
        private readonly ILogger<DbConnection> _logger;

        public DbConnection(IConfiguration configuration, ILogger<DbConnection> logger)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("MariaDBConnection");
        }
        
        public IDbConnection CreateConnection()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                
                connection.Open(); // Abre la conexión
                _logger.LogInformation("Conexión establecida con la base de datos.");
                
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al conectar con la base de datos: {ex.Message}");
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                throw;
            }
        }
    }
}