using fingerprint_service.Dtos;
using fingerprint_service.Models;

namespace fingerprint_service.Services.Interfaces
{
    public interface IFingerprintService
    {
        /// <summary>
        /// Prueba la conexión a la base de datos donde se almacenan las huellas digitales.
        /// </summary>
        /// <returns>Una respuesta que indica si la conexión se estableció correctamente.</returns>
        ApiResponse<bool> TestDatabaseConnection();

        /// <summary>
        /// Identifica a una persona a partir de una huella digital, comparándola con las almacenadas en la base de datos.
        /// </summary>
        /// <returns>Una respuesta que contiene la información de la persona identificada o un mensaje de error si no se encuentra coincidencia.</returns>
        /// <remarks>
        /// Este método aún no está implementado.
        /// </remarks>
        ApiResponse<DtoFingerprintResponse> IdentifyFingerprint(FingerprintCompareRequest request);

        /// <summary>
        /// Procesa una solicitud de inscripción de huella digital y la registra en la base de datos.
        /// </summary>
        /// <param name="imageRequest">
        /// Objeto que contiene los datos de la solicitud de inscripción, incluyendo:
        ///   - Imagen de la huella digital (en formato que admita el método ProcessRaw)
        ///   - ID del empleado al que pertenece la huella
        ///   - Dedo al que corresponde la huella (valor del enum Fingers)
        /// </param>
        /// <returns>
        /// ApiResponse que indica el resultado del proceso:
        ///   - Success: True si la inscripción se realizó exitosamente, False en caso de error.
        ///   - Message: Descripción del resultado (mensaje de confirmación o error).
        ///   - Data: (opcional) En caso de error con una huella existente, puede contener el dedo de la huella duplicada.
        /// </returns>
        ApiResponse<string> ProcessAndEnrollFingerprint(DtoFingerprintImageRequest imageRequest);

        /// <summary>
        /// Elimina una o varias huellas digitales asociadas a un empleado, según los parámetros proporcionados.
        /// </summary>
        /// <param name="fingerprintDelete">
        /// Objeto que contiene los datos necesarios para la eliminación de huellas digitales, incluyendo:
        ///   - EmployeeId: ID del empleado al que pertenecen las huellas a eliminar (obligatorio).
        ///   - Finger: Enumeración que indica el dedo cuya huella se desea eliminar. Si es nulo, se eliminarán todas las huellas del empleado.
        /// </param>
        /// <returns>
        /// ApiResponse que indica el resultado del proceso:
        ///   - Success: True si se eliminó al menos una huella, False si no se encontraron huellas para eliminar.
        ///   - Message: Descripción del resultado, ya sea una confirmación o un mensaje de error.
        ///   - Data: True o False dependiendo del éxito de la operación.
        /// </returns>
        /// <remarks>
        /// Este método llama al repositorio correspondiente para ejecutar la operación de eliminación,
        /// ya sea de una huella específica o de todas las huellas de un empleado.
        /// </remarks>
        ApiResponse<bool> DeleteFingerprint(DtoFingerprintDelete fingerprintDelete);

        

        /// <summary>
        /// Compara una huella digital proporcionada con las huellas almacenadas en la base de datos.
        /// </summary>
        /// <param name="fingerprintBase64">La huella digital en formato base64.</param>
        /// <returns>Una respuesta que contiene la información de la huella coincidente o un mensaje de error si no se encuentra coincidencia.</returns>
        ApiResponse<Fingerprint> CompareFingerprint(string fingerprintBase64);

        /// <summary>
        /// Verifica si hay huellas digitales duplicadas en la base de datos.
        /// </summary>
        /// <returns>Una respuesta que indica si se encontraron coincidencias.</returns>
        ApiResponse<bool> CheckFingerprintMatchesInDatabase();
    }
}