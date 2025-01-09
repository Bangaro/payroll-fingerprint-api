using fingerprint_service.Dtos;
using fingerprint_service.Models;

namespace fingerprint_service.Services.Interfaces;

// IFingerprintService.cs

public interface IFingerprintService
{
    /// <summary>
    /// Prueba la conexión al lector de huellas digitales.
    /// </summary>
    /// <returns>Una respuesta que indica si la conexión se estableció correctamente.</returns>
    ApiResponse<bool> TestDatabaseConnection();

    /// <summary>
    /// Convierte una huella digital capturada a un formato estándar (FMD) para su comparación.
    /// </summary>
    /// <param name="fingerprint">La huella digital a convertir.</param>
    /// <returns>Una respuesta que contiene la huella digital convertida o un mensaje de error.</returns>
    ApiResponse<Fingerprint> ConvertFingerprintToFMD(Fingerprint fingerprint);

    /// <summary>
    /// Compara dos huellas digitales para determinar si pertenecen a la misma persona.
    /// </summary>
    /// <param name="fingerprint1">La primera huella digital a comparar.</param>
    /// <param name="fingerprint2">La segunda huella digital a comparar.</param>
    /// <returns>Una respuesta que indica si las huellas coinciden.</returns>
    ApiResponse<bool> CompareFingerprints(Fingerprint fingerprint1, Fingerprint fingerprint2);


    /// <summary>
    /// Identifica una huella digital en una base de datos de huellas.
    /// </summary>
    /// <returns>Una respuesta que contiene la información del usuario asociado a la huella o un mensaje de error.</returns>
    ApiResponse<Fingerprint> IdentifyFingerprint();

    /// <summary>
    /// Registra una nueva huella digital en la base de datos, asociándola a un usuario.
    /// </summary>
    /// <param name="fingerprint">La huella digital a registrar.</param>
    /// <param name="userId">El ID del usuario al que pertenece la huella.</param>
    /// <returns>Una respuesta que indica si el registro fue exitoso.</returns>
    ApiResponse<Fingerprint> EnrollFingerprint(Fingerprint fingerprint, int userId);

    /// <summary>
    /// Cancela una captura de huella digital en curso.
    /// </summary>
    /// <returns>Una respuesta (puede ser vacía si la operación fue exitosa).</returns>
    ApiResponse<bool> CancelCapture();

    /// <summary>
    /// Obtiene una lista de todas las huellas digitales almacenadas.
    /// </summary>
    /// <returns>Una respuesta que contiene una lista de huellas digitales o un mensaje de error.</returns>
    ApiResponse<List<Fingerprint>> GetFingerprints();

    /// <summary>
    /// Elimina una huella digital de la base de datos.
    /// </summary>
    /// <param name="fingerprintId">El ID de la huella digital a eliminar.</param>
    /// <returns>Una respuesta que indica si la eliminación fue exitosa.</returns>
    ApiResponse<bool> DeleteFingerprint(int fingerprintId);

    /// <summary>
    /// Fuerza una reconexión al lector de huellas digitales.
    /// </summary>
    /// <returns>Una respuesta que indica si la reconexión fue exitosa.</returns>
    ApiResponse<bool> ReconnectFingerprintDevice();

    ApiResponse<string> ProcessRaw(DtoFingerprintImageRequest imageRequest);

    ApiResponse<Fingerprint> CompareFingerprint(string fingerprintBase64);

    ApiResponse<bool> CheckFingerprintMatchesInDatabase();

}