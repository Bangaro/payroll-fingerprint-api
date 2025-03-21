﻿using fingerprint_service.db;
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
    // Mapeo de nombres de dedos en inglés a español
    private static readonly Dictionary<string, string> FingerNames = new Dictionary<string, string>
    {
        { "RIGHT_THUMB", "Pulgar derecho" },
        { "RIGHT_INDEX", "Índice derecho" },
        { "RIGHT_MIDDLE", "Medio derecho" },
        { "RIGHT_RING", "Anular derecho" },
        { "RIGHT_PINKY", "Meñique derecho" },
        { "LEFT_THUMB", "Pulgar izquierdo" },
        { "LEFT_INDEX", "Índice izquierdo" },
        { "LEFT_MIDDLE", "Medio izquierdo" },
        { "LEFT_RING", "Anular izquierdo" },
        { "LEFT_PINKY", "Meñique izquierdo" }
    };


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

                var query = $@"
                SELECT f.id, f.id_employee, f.finger, f.fmd, f.created_date
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
                var query = $@"
                SELECT id, id_employee, finger, fmd, created_date
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
    
    public Employee GetEmployeeById(int employeeId)
    {
        Employee employee = null;
        _logger.LogInformation($"Consultando datos del empleado con ID: {employeeId}.");

        try
        {
            using (var connection = _dbConnection.CreateConnection())
            {

                var query = @"
                SELECT 
                    id, id_company, name, email, nid_user, phone, job, is_active
                FROM payroll_employees
                WHERE id = @employeeId;";

                using (var command = new MySqlCommand(query, (MySqlConnection)connection))
                {
                    command.Parameters.AddWithValue("@employeeId", employeeId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            employee = new Employee
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                IdCompany = Convert.ToInt32(reader["id_company"]),
                                Name = reader["name"].ToString(),
                                Email = reader["email"].ToString(),
                                NidUser = reader["nid_user"].ToString(),
                                Phone = reader["phone"].ToString(),
                                Job = reader["job"].ToString(),
                                IsActive = Convert.ToBoolean(reader["is_active"])
                            };
                        }
                    }
                }
            }

            if (employee == null)
            {
                _logger.LogInformation($"No se encontró un empleado con ID: {employeeId}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al consultar datos del empleado con ID: {employeeId}.");
            throw;
        }

        return employee;
    }

    

public bool AddFingerprint(Fingerprint fingerprint)
{
    try
    {
        using (var connection = _dbConnection.CreateConnection())
        {
            // Paso 1: Verificar si el empleado existe en la tabla payroll_employee
            string checkEmployeeExists = "SELECT COUNT(*) FROM payroll_employees WHERE id = @id_employee";
            using (var checkCommand = new MySqlCommand(checkEmployeeExists, (MySqlConnection)connection))
            {
                checkCommand.Parameters.AddWithValue("@id_employee", fingerprint.EmployeeId);
                int employeeCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (employeeCount == 0)
                {
                    // El empleado no existe, lanzar una excepción personalizada
                    string errorMessage = $"El empleado con ID {fingerprint.EmployeeId} no existe en la tabla payroll_employees.";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }

            // Paso 2: Insertar la huella si el empleado existe
            string sql = $@"INSERT INTO {fingerprintTable} 
                      (id_employee, finger, fmd, created_date) 
                      VALUES (@id_employee, @finger, @fmd, @created_date)";
            using (var command = new MySqlCommand(sql, (MySqlConnection)connection))
            {
                command.Parameters.AddWithValue("@id_employee", fingerprint.EmployeeId);
                command.Parameters.AddWithValue("@finger", fingerprint.Finger);
                command.Parameters.AddWithValue("@fmd", fingerprint.Fmd);
                command.Parameters.AddWithValue("@created_date", fingerprint.CreatedDate);

                int rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation($"Dedo agregado: {fingerprint.Finger} para el empleado {fingerprint.EmployeeId}");
                return rowsAffected > 0;
            }
        }
    }
    catch (MySqlException ex) when (ex.Number == 1062) // Error de duplicado
    {
        string fingerName = FingerNames.ContainsKey(fingerprint.Finger.ToString()) ? FingerNames[fingerprint.Finger.ToString()] : fingerprint.Finger.ToString();
        string errorMessage = $"Dedo duplicado para este empleado, procura elegir un dedo distinto al {fingerName}";
        _logger.LogError(ex, errorMessage);

        // Lanza la excepción personalizada
        throw new Exception(errorMessage);
    }

    catch (Exception ex)
    {
        _logger.LogError(ex, "Ha ocurrido un error al guardar la huella");
        throw;
    }
}
    
    public bool DeleteAllUserFingerprints(int employeeId)
    {
        return DeleteFingerprints(new DtoFingerprintDelete { EmployeeId = employeeId });
    }
    public bool DeleteUserFingerprint(DtoFingerprintDelete fingerprint)
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
    /// dependiendo del valor del parámetro Finger. Si Finger es nulo, se eliminarán 
    /// todas las huellas del empleado. Si se especifica un valor para Finger, 
    /// solo se eliminará la huella correspondiente a ese dedo.
    /// </summary>
    /// <param name="fingerprint">Un objeto que contiene el ID del empleado y opcionalmente el dedo a eliminar.</param>
    /// <returns>True si se eliminó al menos una huella, false en caso contrario.</returns>
private bool DeleteFingerprints(DtoFingerprintDelete fingerprint)
{
    // Indicador para determinar si la eliminación fue exitosa
    bool isDeleted = false;

    // Determinar el tipo de eliminación y registrar información
    _logger.LogInformation(fingerprint.Finger == null
        ? $"Eliminando todas las huellas para EmployeeId: {fingerprint.EmployeeId}."
        : $"Eliminando huella para EmployeeId: {fingerprint.EmployeeId}, Finger: {fingerprint.Finger}.");

    using (var connection = _dbConnection.CreateConnection())
    {
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
                try
                {
                    // Parsear el string al enume Fingers
                    Fingers parsedFinger = (Fingers)Enum.Parse(typeof(Fingers), fingerprint.Finger, true);

                    // Convertir el valor del enum a int
                    int fingerValue = (int)parsedFinger;
                    
                    command.Parameters.AddWithValue("@finger", fingerValue);

                    // Registrar el valor parseado para depuración
                    _logger.LogInformation($"Dedo parseado: {parsedFinger}, Valor numérico: {fingerValue}");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError($"Error al parsear el dedo '{fingerprint.Finger}'. Detalles: {ex.Message}");
                    return false; 
                }
            }

            // Ejecutar el comando y verificar si afectó alguna fila
            int rowsAffected = command.ExecuteNonQuery();
            isDeleted = rowsAffected > 0;
        }
    }

    return isDeleted;
}


}
