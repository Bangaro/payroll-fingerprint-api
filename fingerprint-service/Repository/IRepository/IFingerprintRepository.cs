using fingerprint_service.Dtos;
using fingerprint_service.Models;

namespace fingerprint_service.Repository.Interfaces;

public interface IFingerprintRepository
{
    /// <summary>
    /// Prueba la conexión a la base de datos.
    /// </summary>
    /// <returns>True si la conexión es exitosa, False en caso contrario.</returns>
    bool TestConnection();

    /// <summary>
    /// Obtiene todas las huellas dactilares registradas en la base de datos.
    /// </summary>
    /// <returns>Una colección de objetos Fingerprint que representan todas las huellas dactilares.</returns>
    IEnumerable<Fingerprint> GetAllFingerprints();

    /// <summary>
    /// Obtiene las huellas dactilares de los empleados pertenecientes a una empresa específica.
    /// </summary>
    /// <param name="companyId">El ID de la empresa.</param>
    /// <returns>Una colección de objetos Fingerprint que representan las huellas dactilares de los empleados de la empresa.</returns>
    IEnumerable<Fingerprint> GetCompanyFingerprints(int companyId);

    /// <summary>
    /// Obtiene las huellas dactilares de un empleado específico.
    /// </summary>
    /// <param name="employeeId">El ID del empleado.</param>
    /// <returns>Una colección de objetos Fingerprint que representan las huellas dactilares del empleado.</returns>
    IEnumerable<Fingerprint> GetEmployeeFingerprints(int employeeId);
    
    /// <summary>
    /// Obtiene los datos más relevantes de un empleado con base en su ID.
    /// </summary>
    Employee GetEmployeeById(int employeeId);

    /// <summary>
    /// Agrega una nueva huella dactilar a la base de datos.
    /// </summary>
    /// <param name="fingerprint">El objeto Fingerprint que representa la huella a agregar.</param>
    /// <returns>True si la operación se realizó con éxito, False en caso contrario.</returns>
    bool AddFingerprint(Fingerprint fingerprint);

    /// <summary>
    /// Elimina una huella específica de un empleado.
    /// </summary>
    /// <param name="fingerprint">El objeto DtoDeleteFingerprint que representa la huella a eliminar.</param>
    /// <returns>True si la operación se realizó con éxito, False en caso contrario.</returns>
    bool DeleteUserFingerprint(DtoFingerprintDelete fingerprint);

    /// <summary>
    /// Elimina todas las huellas dactilares de un empleado específico.
    /// </summary>
    /// <param name="employeeId">El ID del empleado cuyas huellas serán eliminadas.</param>
    /// <returns>True si la operación se realizó con éxito, False en caso contrario.</returns>
    bool DeleteAllUserFingerprints(int employeeId);
}
