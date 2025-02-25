using System.Text;
using DPUruNet;
using fingerprint_service.Dtos;
using fingerprint_service.Models;
using fingerprint_service.Repository.Interfaces;
using fingerprint_service.Services.Interfaces;

namespace fingerprint_service.Services;

// Enum para umbrales de seguridad (Evitar falsos positivos y falsos negativos)
public enum SecurityThreshold
{
    HighSecurity = 2147,         // 1 en 1,000,000 (0.0001% FP)
    GeneralUse = 21474,          // 1 en 100,000 (0.001% FP)
    HighConvenience = 214748     // 1 en 10,000 (0.01% FP)
}

public class FingerprintService : IFingerprintService
{
    // 1. Constantes para configuración
    private const int IMAGE_WIDTH = 500;
    private const int IMAGE_HEIGHT = 500;
    private const int IMAGE_RESOLUTION = 700;
    private const int CBEFF_ID = 3407615;
    private readonly int THRESHOLD = (int)SecurityThreshold.HighConvenience;

    private readonly IFingerprintRepository _fingerprintRepository;
    private readonly ILogger<FingerprintService> _logger;
    private Reader _reader = null;


    public FingerprintService(IFingerprintRepository fingerprintRepository,
            ILogger<FingerprintService> logger)
    {
        _fingerprintRepository = fingerprintRepository;
        _logger = logger;
    }

    public ApiResponse<bool> TestDatabaseConnection()
    {
        try
        {
            var result = _fingerprintRepository.TestConnection();
            return new ApiResponse<bool>(true, "Conexión a base de datos abierta", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar la conexión a la base de datos.");
            return new ApiResponse<bool>(false, $"Error al verificar la conexión: {ex.Message}", false);
        }
    }

    public ApiResponse<string> ProcessAndEnrollFingerprint(DtoFingerprintImageRequest imageRequest)
    {
        try
        {
            // Validar datos de entrada
            if (imageRequest.EmployeeId == null || imageRequest.EmployeeId <= 0)
            {
                return new ApiResponse<string>(false, "Se requiere el ID del empleado, y que este sea válido.");
            }
            if (imageRequest.CompanyId == null || imageRequest.CompanyId <= 0)
            {
                return new ApiResponse<string>(false, "Se requiere el ID de la compañía, y que este sea válido.");
            }
            if (imageRequest.FingerprintsData == null || imageRequest.FingerprintsData.Length < 2)
            {
                return new ApiResponse<string>(false, "Se requieren al menos dos muestras de la huella digital.");
            }
            if (!Enum.IsDefined(typeof(Fingers), imageRequest.Finger))
            {
                var validFingers = string.Join(", ", Enum.GetNames(typeof(Fingers)));
                return new ApiResponse<string>(false, $"El dedo '{imageRequest.Finger}' no es válido. Dedos válidos: {validFingers}");
            }

            // Verificar si las huellas ya existen
            foreach (var fingerprintData in imageRequest.FingerprintsData)
            {
                var fingerprintExists = CompareFingerprint(fingerprintData, imageRequest.CompanyId);
                if (fingerprintExists.Success && fingerprintExists.Data != null)
                {
                    _logger.LogInformation("La huella digital coincide con un registro existente.");
                    return new ApiResponse<string>(false, "La huella coincide con un registro existente.", fingerprintExists.Data.Finger + ", Empleado: " + fingerprintExists.Data.EmployeeId);
                }
            }

            // Procesar huellas digitales y obtener el FMD
            var processResult = ProcessRaw(imageRequest);
            if (!processResult.Success)
            {
                return new ApiResponse<string>(false, processResult.Message);
            }

            // Crear objeto Fingerprint para la inscripción
            var fingerprint = new Fingerprint
            {
                EmployeeId = imageRequest.EmployeeId,
                Finger = Enum.Parse<Fingers>(imageRequest.Finger),
                Fmd = processResult.Data.Fmd,
                CreatedDate = DateTime.UtcNow
            };

            // Inscribir huella en la base de datos
            var enrollResult = EnrollFingerprint(fingerprint);
            if (!enrollResult.Success)
            {
                return new ApiResponse<string>(false, enrollResult.Message);
            }

            return new ApiResponse<string>(true, "Huella digital procesada e inscrita exitosamente.",
                imageRequest.Finger + ", Employee ID: " + imageRequest.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al procesar e inscribir la huella digital.");
            return new ApiResponse<string>(false, "Error interno al procesar e inscribir la huella digital.");
        }
    }

    public ApiResponse<Fingerprint> CompareFingerprint(string fingerprintBase64, int companyId)
    {
        var validationResponse = ValidateAndConvertBase64ToBytes(fingerprintBase64, "Datos de huella inválidos.");
        if (!validationResponse.Success) return new ApiResponse<Fingerprint>(false, validationResponse.Message);

        var fmdResult = CreateFMD(validationResponse.Data);
        if (fmdResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
        {
            _logger.LogError("Error al generar FMD a partir de los datos de la huella.");
            return new ApiResponse<Fingerprint>(false, "Error al generar FMD a partir de los datos de la huella.");
        }

        var fmdInput = fmdResult.Data;
        var registeredFingerprints = _fingerprintRepository.GetCompanyFingerprints(companyId);

        foreach (var registeredFingerprint in registeredFingerprints)
        {
            var registeredFmd = new Fmd(registeredFingerprint.Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);
            var comparisonResult = Comparison.Compare(fmdInput, 0, registeredFmd, 0);

            if (comparisonResult.Score <= THRESHOLD) // Umbral de similitud
            {
                _logger.LogInformation($"Huella encontrada: User ID={registeredFingerprint.EmployeeId}");
                return new ApiResponse<Fingerprint>(true, "Huella encontrada.", registeredFingerprint);
            }
        }

        return new ApiResponse<Fingerprint>(false, "No se encontró coincidencia para la huella.");
    }

    public ApiResponse<DtoFingerprintResponse> IdentifyFingerprint(FingerprintCompareRequest request)
    {
        // Validar datos de entrada
        if (request.CompanyId == null || request.CompanyId <= 0)
        {
            return new ApiResponse<DtoFingerprintResponse>(false, "Se requiere el ID de la compañía, y que este sea válido.");
        }
        if (request.FingerprintData == null)
        {
            return new ApiResponse<DtoFingerprintResponse>(false, "Se requiere la muestra de la huella digital.");
        }
        
        // Validar y convertir la huella digital en formato Base64
        var validationResponse = ValidateAndConvertBase64ToBytes(request.FingerprintData, "Datos de huella inválidos.");
        if (!validationResponse.Success)
        {
            return new ApiResponse<DtoFingerprintResponse>(false, validationResponse.Message, null);
        }

        // Crear el FMD a partir de los bytes de la huella
        var fmdResult = CreateFMD(validationResponse.Data);
        if (fmdResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
        {
            _logger.LogError("Error al generar FMD a partir de los datos de la huella.");
            return new ApiResponse<DtoFingerprintResponse>(false, "Error al procesar los datos de la huella.", null);
        }

        var fmdInput = fmdResult.Data;

        // Obtener todas las huellas registradas desde la base de datos
        var registeredFingerprints = _fingerprintRepository.GetCompanyFingerprints(request.CompanyId);

        foreach (var registeredFingerprint in registeredFingerprints)
        {
            var registeredFmd = new Fmd(registeredFingerprint.Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);

            // Comparar el FMD de entrada con el FMD registrado
            var comparisonResult = Comparison.Compare(fmdInput, 0, registeredFmd, 0);

            if (comparisonResult.Score <= THRESHOLD) // Umbral de similitud
            {
                _logger.LogInformation($"Huella identificada: ID={registeredFingerprint.Id}, Employee ID={registeredFingerprint.EmployeeId}");

                Employee employee = _fingerprintRepository.GetEmployeeById(registeredFingerprint.EmployeeId);

                // Devolver el DTO con la información correspondiente
                var response = new DtoFingerprintResponse
                {
                    Id = registeredFingerprint.Id,
                    EmployeeId = registeredFingerprint.EmployeeId,
                    Finger = registeredFingerprint.Finger.ToString(),
                    CompanyId = employee.IdCompany,
                    Name = employee.Name,
                    Email = employee.Email,
                    NidUser = employee.NidUser,
                    Phone = employee.Phone,
                    Job = employee.Job,
                    IsActive = employee.IsActive,
                    CreatedDate = registeredFingerprint.CreatedDate
                };

                return new ApiResponse<DtoFingerprintResponse>(true, "Huella identificada exitosamente.", response);
            }
        }

        return new ApiResponse<DtoFingerprintResponse>(false, "No se encontró coincidencia para la huella.", null);
    }

    public ApiResponse<bool> DeleteFingerprint(DtoFingerprintDelete fingerprintDelete)
    {
        try
        {
            // Validar parámetros
            if (string.IsNullOrEmpty(fingerprintDelete.EmployeeId.ToString()))
            {
                throw new ArgumentException("El ID del empleado no puede ser nulo.");
            }

            bool result;

            // Verificar si Finger está presente y es válido
            if (!string.IsNullOrEmpty(fingerprintDelete.Finger))
            {
                if (!Enum.IsDefined(typeof(Fingers), fingerprintDelete.Finger))
                {
                    var validFingers = string.Join(", ", Enum.GetNames(typeof(Fingers)));
                    return new ApiResponse<bool>(false, $"El dedo '{fingerprintDelete.Finger}' no es válido. Dedos válidos: {validFingers}");
                }

                // Llamar al repositorio para eliminar la huella específica
                result = _fingerprintRepository.DeleteUserFingerprint(fingerprintDelete);
            }
            else
            {
                // Si Finger es nulo, eliminar todas las huellas del empleado
                result = _fingerprintRepository.DeleteAllUserFingerprints(fingerprintDelete.EmployeeId);
            }

            // Retornar respuesta de éxito
            return new ApiResponse<bool>(true, result ? "Huella eliminada correctamente." : "No se encontraron huellas para eliminar.", result);
        }
        catch (Exception ex)
        {
            // Manejo de errores
            return new ApiResponse<bool>(false, $"Error al eliminar huellas: {ex.Message}");
        }
    }

    private ApiResponse<bool> EnrollFingerprint(Fingerprint fingerprint)
    {
        // Valida que la huella no sea nula
        if (fingerprint == null || fingerprint.Fmd == null || fingerprint.Fmd.Length == 0)
        {
            const string errorMessage = "Datos de huella inválidos.";
            _logger.LogError(errorMessage);
            return new ApiResponse<bool>(false, errorMessage);
        }

        try
        {
            fingerprint.CreatedDate = DateTime.UtcNow;
            _fingerprintRepository.AddFingerprint(fingerprint);

            _logger.LogInformation($"Huella inscrita exitosamente para Employee ID={fingerprint.EmployeeId}.");
            return new ApiResponse<bool>(true, "Huella inscrita exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inscribir la huella.");
            return new ApiResponse<bool>(false, ex.Message);
        }
    }

    private ApiResponse<byte[]> ValidateAndConvertBase64ToBytes(string base64Data, string errorMessage)
    {
        if (string.IsNullOrEmpty(base64Data))
        {
            _logger.LogError(errorMessage);
            return new ApiResponse<byte[]>(false, errorMessage, null);
        }

        try
        {
            return new ApiResponse<byte[]>(true, "Datos convertidos correctamente.", Convert.FromBase64String(base64Data));
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Datos Base64 inválidos.");
            return new ApiResponse<byte[]>(false, "Datos Base64 inválidos.", null);
        }
    }

    private ApiResponse<Fingerprint> ProcessRaw(DtoFingerprintImageRequest imageRequest)
    {
        try
        {
            // Log inicial para verificar la solicitud
            _logger.LogInformation($"Procesando huellas para EmployeeId: {imageRequest.EmployeeId}, Dedo: {imageRequest.Finger}");

            // Validar las entradas
            if (imageRequest.FingerprintsData == null || imageRequest.FingerprintsData.Length < 2)
            {
                _logger.LogError("No se proporcionaron suficientes muestras de huellas.");
                return new ApiResponse<Fingerprint>(false, "Se requieren al menos dos muestras de huellas.");
            }

            // Validar el dedo
            if (!Enum.TryParse<Fingers>(imageRequest.Finger, ignoreCase: true, out var finger))
            {
                var validFingers = string.Join(", ", Enum.GetNames(typeof(Fingers)));
                _logger.LogError($"El dedo '{imageRequest.Finger}' no es válido.");
                return new ApiResponse<Fingerprint>(false, $"El dedo '{imageRequest.Finger}' no es válido. Dedos válidos: {validFingers}");
            }

            // Convertir y validar todas las muestras de la huella digital
            var fingerprintBytesList = new List<byte[]>();
            foreach (var fingerprintData in imageRequest.FingerprintsData)
            {
                var validation = ValidateAndConvertBase64ToBytes(fingerprintData, "Datos de huella inválidos.");
                if (!validation.Success)
                {
                    _logger.LogError("Una o más muestras de huellas son inválidas.");
                    return new ApiResponse<Fingerprint>(false, "Una o más muestras de huellas son inválidas.");
                }
                fingerprintBytesList.Add(validation.Data);
            }

            // Crear FMDs para cada huella digital
            var fmds = fingerprintBytesList.Select(CreateFMD).ToList();

            // Validar resultados de los FMDs
            if (fmds.Any(f => f.ResultCode != Constants.ResultCode.DP_SUCCESS))
            {
                _logger.LogError("Error al generar los FMDs para las huellas. Verifica que los datos sean correctos y en formato RAW.");
                return new ApiResponse<Fingerprint>(false, "Error al generar los FMDs para las huellas. Verifica que los datos sean correctos y en formato RAW.");
            }

            // Crear el FMD de inscripción
            var enrollmentResult = Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ISO, fmds.Select(f => f.Data).ToArray());
            if (enrollmentResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                _logger.LogError("Error al crear el FMD de inscripción.");
                return new ApiResponse<Fingerprint>(false, "Error al crear el FMD de inscripción.");
            }

            // Crear el objeto Fingerprint con los datos procesados
            var fingerprint = new Fingerprint
            {
                EmployeeId = imageRequest.EmployeeId,
                Finger = finger,
                Fmd = enrollmentResult.Data.Bytes,
                CreatedDate = DateTime.UtcNow,
            };

            _logger.LogInformation("Procesamiento de huella completado. Listo para inscribir.");
            return new ApiResponse<Fingerprint>(true, "Procesamiento completado.", fingerprint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al procesar la huella.");
            return new ApiResponse<Fingerprint>(false, "Error interno al procesar la huella.");
        }
    }

    private DataResult<Fmd> CreateFMD(byte[] rawImageData)
    {
        return FeatureExtraction.CreateFmdFromRaw(
            rawImageData,
            fingerPosition: 1,        // Posición del dedo
            CbeffId: CBEFF_ID,        // ID compatible con el SDK
            width: IMAGE_WIDTH,       // Ancho de la imagen
            height: IMAGE_HEIGHT,     // Alto de la imagen
            resolution: IMAGE_RESOLUTION, // Resolución de la imagen
            Constants.Formats.Fmd.ISO // Formato ISO
        );
    }
}
