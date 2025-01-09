using System;
using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace fingerprint_service.db
{
    public class DbConnection
    {
        private readonly string _connectionString;

        public DbConnection(IConfiguration configuration)
        {
            // Obtiene la cadena de conexión del archivo appsettings.json
            _connectionString = configuration.GetConnectionString("MariaDBConnection");
        }

        public IDbConnection CreateConnection()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                
                connection.Open(); // Abre la conexión
                
                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar con la base de datos: {ex.Message}");
                throw;
            }
        }
    }
}