using fingerprint_service.db;
using fingerprint_service.Dtos;
using fingerprint_service.Models;
using fingerprint_service.Repository.Interfaces;
using MySql.Data.MySqlClient;

namespace fingerprint_service.Repository;

public class FingerprintRepository : IFingerprintRepository
{
    private readonly DbConnection _dbConnection;
    private readonly ILogger<FingerprintRepository> _logger;
    private readonly string fingerprintTable = "payroll_employee_fingerprints";

    public FingerprintRepository(DbConnection dbConnection, ILogger<FingerprintRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public bool TestConnection()
    {
        try
        {
            using (_dbConnection.CreateConnection())
            {
                return true; // Conexión abierta exitosamente
            }
        }
        catch (Exception)
        {
            return false; 
        }
    }

    public IEnumerable<Fingerprint> GetAllFingerprints()
    {
        var fingerprints = new List<Fingerprint>();
        _logger.LogInformation("Consultando todas las huellas dactilares.");

        using (var connection = _dbConnection.CreateConnection())
        {
            var query = $"SELECT * FROM {fingerprintTable}";
            using (var command = new MySqlCommand(query, (MySqlConnection)connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        fingerprints.Add(new Fingerprint
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            EmployeeId = Convert.ToInt32(reader["id_employee"]), 
                            Finger = (Fingers)Enum.Parse(typeof(Fingers), reader["finger"].ToString()), 
                            Fmd = (byte[])reader["fmd"], 
                            FmdQuality = Convert.ToInt32(reader["fmd_quality"]), 
                            CreatedDate = Convert.ToDateTime(reader["created_date"]),
                        });
                    }
                }
            }
        }
        return fingerprints;
    }

    public IEnumerable<Fingerprint> GetCompanyFingerprints(int companyId)
    {
        var fingerprints = new List<Fingerprint>();
        _logger.LogInformation($"Consultando huellas dactilares para la compañía con ID: {companyId}.");

        try
        {
            using (var connection = _dbConnection.CreateConnection())
            {
                connection.Open();

                var query = $@"
                SELECT f.id, f.id_employee, f.finger, f.fmd, f.fmd_quality, f.created_date
                FROM {fingerprintTable} f, payroll_employees pe
                WHERE pe.id_company = @companyId;";

                using (var command = new MySqlCommand(query, (MySqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@companyId", companyId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fingerprints.Add(new Fingerprint
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                EmployeeId = Convert.ToInt32(reader["id_employee"]), 
                                Finger = (Fingers)Enum.Parse(typeof(Fingers), reader["finger"].ToString()), 
                                Fmd = (byte[])reader["fmd"], 
                                FmdQuality = Convert.ToInt32(reader["fmd_quality"]), 
                                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                            });
                        }
                    }
                }
            }

            if (!fingerprints.Any())
            {
                _logger.LogInformation("No se encontraron huellas para el ID de compañía proporcionado.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al consultar huellas para la compañía con ID: {companyId}.");
            throw; 
        }

        return fingerprints;
    }

    public IEnumerable<Fingerprint> GetEmployeeFingerprints(int employeeId)
    {
        var fingerprints = new List<Fingerprint>();
        _logger.LogInformation($"Consultando huellas dactilares para el empleado: {employeeId}.");

        try
        {
            using (var connection = _dbConnection.CreateConnection())
            {
                connection.Open();

                var query = $@"
                SELECT id, id_employee, finger, fmd, fmd_quality, created_date
                FROM {fingerprintTable} WHERE id_employee = @employee;";

                using (var command = new MySqlCommand(query, (MySqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@employee", employeeId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fingerprints.Add(new Fingerprint
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                EmployeeId = Convert.ToInt32(reader["id_employee"]), 
                                Finger = (Fingers)Enum.Parse(typeof(Fingers), reader["finger"].ToString()), 
                                Fmd = (byte[])reader["fmd"], 
                                FmdQuality = Convert.ToInt32(reader["fmd_quality"]), 
                                CreatedDate = Convert.ToDateTime(reader["created_date"]),
                            });
                        }
                    }
                }
            }

            if (!fingerprints.Any())
            {
                _logger.LogInformation("No se encontraron huellas para el ID del empleado proporcionado.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al consultar huellas para el empleado con ID: {employeeId}.");
            throw; 
        }

        return fingerprints;
    }
    

    public bool AddFingerprint(Fingerprint fingerprint)
    {
        using (var connection = _dbConnection.CreateConnection())
        {
            using (var command = new MySqlCommand($"INSERT INTO {fingerprintTable} (id_employee, finger, fmd, fmd_quality, created_date) VALUES (@id_employee, @finger, @fmd, @fmd_quality, @created_date)", (MySqlConnection)connection))
            {
                // Asignar los valores de los parámetros
                command.Parameters.AddWithValue("@id_employee", fingerprint.EmployeeId);
                command.Parameters.AddWithValue("@finger", fingerprint.Finger);
                command.Parameters.AddWithValue("@fmd", fingerprint.Fmd);
                command.Parameters.AddWithValue("@fmd_quality", fingerprint.FmdQuality);
                command.Parameters.AddWithValue("@created_date", fingerprint.CreatedDate);

                // Ejecutar el comando y devolver si afectó alguna fila
                return command.ExecuteNonQuery() > 0;
            }
        }
    }
    
    public bool DeleteAllUserFingerprints(int employeeId)
    {
        return DeleteFingerprints(new DtoDeleteFingerprint { EmployeeId = employeeId });
    }
    public bool DeleteUserFingerprint(DtoDeleteFingerprint fingerprint)
    {
        if (fingerprint.Finger == null)
        {
            throw new ArgumentException("El campo 'Finger' no puede ser nulo para esta operación.");
        }

        return DeleteFingerprints(fingerprint);
    }
    
    /// <summary>
    /// Elimina huellas digitales de un empleado.
    /// 
    /// Este método permite eliminar una o todas las huellas digitales de un empleado, 
    /// dependiendo del valor del parámetro `Finger`. Si `Finger` es nulo, se eliminarán 
    /// todas las huellas del empleado. Si se especifica un valor para `Finger`, 
    /// solo se eliminará la huella correspondiente a ese dedo.
    /// </summary>
    /// <param name="fingerprint">Un objeto que contiene el ID del empleado y opcionalmente el dedo a eliminar.</param>
    /// <returns>True si se eliminó al menos una huella, false en caso contrario.</returns>
    private bool DeleteFingerprints(DtoDeleteFingerprint fingerprint)
    {
        // Indicador para determinar si la eliminación fue exitosa
        bool isDeleted = false;

        // Determinar el tipo de eliminación y registrar información
        _logger.LogInformation(fingerprint.Finger == null
            ? $"Eliminando todas las huellas para EmployeeId: {fingerprint.EmployeeId}."
            : $"Eliminando huella para EmployeeId: {fingerprint.EmployeeId}, Finger: {fingerprint.Finger}.");

        using (var connection = _dbConnection.CreateConnection())
        {
            connection.Open();

            // Construir la consulta SQL según si se especificó un dedo
            var query = $@"
            DELETE FROM {fingerprintTable}
            WHERE id_employee = @employeeId"
                        + (fingerprint.Finger != null ? " AND finger = @finger" : "") + ";";

            using (var command = new MySqlCommand(query, (MySqlConnection)connection))
            {
                // Asignar el parámetro obligatorio
                command.Parameters.AddWithValue("@employeeId", fingerprint.EmployeeId);

                // Asignar el parámetro opcional si se especificó el dedo
                if (fingerprint.Finger != null)
                {
                    command.Parameters.AddWithValue("@finger", fingerprint.Finger);
                }

                // Ejecutar el comando y verificar si afectó alguna fila
                int rowsAffected = command.ExecuteNonQuery();
                isDeleted = rowsAffected > 0;
            }
        }

        return isDeleted;
    }


}
