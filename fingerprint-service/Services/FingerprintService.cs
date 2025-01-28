using System.Text;
using DPUruNet;
using fingerprint_service.db;
using fingerprint_service.Dtos;
using fingerprint_service.Models;
using fingerprint_service.Repository.Interfaces;
using fingerprint_service.Services.Interfaces;
using MySql.Data.MySqlClient;

namespace fingerprint_service.Services;

// FingerprintService.cs
public class FingerprintService : IFingerprintService
{
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
            return new ApiResponse<bool>(true,"Conexión a base de datos abierta", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking the database connection.");
            return new ApiResponse<bool>(
                success: false,
                message: $"An error occurred while checking the database connection: {ex.Message}",
                data: false
            );
        }
    }

    public ApiResponse<string> ProcessAndEnrollFingerprint(DtoFingerprintImageRequest imageRequest)
    {
        try
        {
            // Validar datos de entrada
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
                var fingerprintExists = CompareFingerprint(fingerprintData);
                if (fingerprintExists.Data != null)
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
                FmdQuality = 90,
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


    public ApiResponse<bool> CheckFingerprintMatchesInDatabase()
    {
        try
        {
            // Obtener todas las huellas activas de la base de datos
            var fingerprints = _fingerprintRepository.GetAllFingerprints();
            var fingerprintsList = fingerprints.ToList(); // Convertir a lista

            if (!fingerprintsList.Any())
            {
                _logger.LogInformation("No hay huellas registradas en la base de datos.");
                return new ApiResponse<bool>(false, "No hay huellas registradas en la base de datos.", false);
            }

            // Comparar cada huella con todas las demás
            for (int i = 0; i < fingerprintsList.Count; i++)
            {
                var fmd1 = new Fmd(fingerprintsList[i].Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);

                for (int j = i + 1; j < fingerprintsList.Count; j++)
                {
                    var fmd2 = new Fmd(fingerprintsList[j].Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);

                    var comparisonResult = Comparison.Compare(fmd1, 0, fmd2, 0);

                    // Verificar si las huellas coinciden según el umbral
                    if (comparisonResult.Score <= 50) // Umbral de similitud
                    {
                        _logger.LogInformation($"Coincidencia encontrada: Huella ID={fingerprintsList[i].Id} y Huella ID={fingerprintsList[j].Id}");
                        return new ApiResponse<bool>(true, "Coincidencia encontrada entre huellas en la base de datos.", true);
                    }
                }
            }

            _logger.LogInformation("No se encontraron coincidencias entre las huellas almacenadas.");
            return new ApiResponse<bool>(true, "No se encontraron coincidencias entre las huellas almacenadas.", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar coincidencias entre huellas en la base de datos.");
            return new ApiResponse<bool>(false, $"Error interno: {ex.Message}", false);
        }
    }
    
    public ApiResponse<Fingerprint> CompareFingerprint(string fingerprintBase64)
    {
        var validationResponse = ValidateAndConvertBase64ToBytes(fingerprintBase64, "Fingerprint data is missing or invalid.");
        if (!validationResponse.Success) return new ApiResponse<Fingerprint>(false, validationResponse.Message);

        var fmdResult = CreateFMD(validationResponse.Data);
        if (fmdResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
        {
            _logger.LogError("Failed to generate FMD from fingerprint data.");
            return new ApiResponse<Fingerprint>(false, "Error generating FMD from fingerprint data.");
        }

        var fmdInput = fmdResult.Data;
        var registeredFingerprints = _fingerprintRepository.GetAllFingerprints();

        foreach (var registeredFingerprint in registeredFingerprints)
        {
            var registeredFmd = new Fmd(registeredFingerprint.Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);
            var comparisonResult = Comparison.Compare(fmdInput, 0, registeredFmd, 0);

            if (comparisonResult.Score <= 50) // Umbral de similitud
            {
                _logger.LogInformation($"Matching fingerprint found for User ID={registeredFingerprint.EmployeeId}");
                return new ApiResponse<Fingerprint>(true, "Matching fingerprint found.", registeredFingerprint);
            }
        }

        return new ApiResponse<Fingerprint>(false, "No matching fingerprint found.");
    }
    


    public ApiResponse<DtoFingerprintResponse> IdentifyFingerprint(FingerprintCompareRequest request)
{
    // Validar y convertir la huella digital en formato Base64
    var validationResponse = ValidateAndConvertBase64ToBytes(request.FingerprintData, "Fingerprint data is missing or invalid.");
    if (!validationResponse.Success)
    {
        return new ApiResponse<DtoFingerprintResponse>(false, validationResponse.Message, null);
    }

    // Crear el FMD a partir de los bytes de la huella
    var fmdResult = CreateFMD(validationResponse.Data);
    if (fmdResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
    {
        _logger.LogError("Error al generar el FMD a partir de los datos de la huella digital.");
        return new ApiResponse<DtoFingerprintResponse>(false, "Error al procesar los datos de la huella digital.", null);
    }

    var fmdInput = fmdResult.Data;

    // Obtener todas las huellas registradas desde la base de datos
    var registeredFingerprints = _fingerprintRepository.GetCompanyFingerprints(request.CompanyId);

    foreach (var registeredFingerprint in registeredFingerprints)
    {
        var registeredFmd = new Fmd(registeredFingerprint.Fmd, (int)Constants.Formats.Fmd.ISO, Constants.WRAPPER_VERSION);

        // Comparar el FMD de entrada con el FMD registrado
        var comparisonResult = Comparison.Compare(fmdInput, 0, registeredFmd, 0);

        if (comparisonResult.Score <= 50) // Umbral de similitud
        {
            _logger.LogInformation($"Huella digital identificada: ID={registeredFingerprint.Id}, Employee ID={registeredFingerprint.EmployeeId}");

            Employee employee = _fingerprintRepository.GetEmployeeById(registeredFingerprint.EmployeeId);

            // Devolver el DTO con la información correspondiente
            var response = new DtoFingerprintResponse
            {
                Id = registeredFingerprint.Id,
                EmployeeId = registeredFingerprint.EmployeeId,
                Finger = registeredFingerprint.Finger,
                IdCompany = employee.IdCompany,
                Name = employee.Name,
                Email = employee.Email,
                NidUser = employee.NidUser,
                Phone = employee.Phone,
                Job = employee.Job,
                IsActive = employee.IsActive,
                CreatedDate = registeredFingerprint.CreatedDate
            };

            return new ApiResponse<DtoFingerprintResponse>(true, "Huella digital identificada exitosamente.", response);
        }
    }

    return new ApiResponse<DtoFingerprintResponse>(false, "No se encontró coincidencia para la huella digital.", null);
}

    
    public ApiResponse<bool> DeleteFingerprint(DtoFingerprintDelete fingerprintDelete)
    {
        try
        {
            // Validar parámetros
            if (fingerprintDelete.EmployeeId == null)
            {
                throw new ArgumentException("El ID del empleado no puede ser nulo.");
            }

            bool result;

            // Verificar si Finger está presente y es válido
            if (fingerprintDelete.Finger != null)
            {
                if (!Enum.IsDefined(typeof(Fingers), fingerprintDelete.Finger))
                {
                    var validFingers = string.Join(", ", Enum.GetNames(typeof(Fingers)));
                    return new ApiResponse<bool>(
                        success: false,
                        message: $"El dedo '{fingerprintDelete.Finger}' no es válido. Dedos válidos: {validFingers}"
                    );
                }

                // Convertir el Enum Finger a su representación numérica
                var dtoFingerprint = new DtoFingerprintDelete
                {
                    EmployeeId = fingerprintDelete.EmployeeId,
                    Finger = fingerprintDelete.Finger
                };

                // Llamar al repositorio para eliminar la huella específica
                result = _fingerprintRepository.DeleteUserFingerprint(dtoFingerprint);
            }
            else
            {
                // Si Finger es nulo, eliminar todas las huellas del empleado
                result = _fingerprintRepository.DeleteAllUserFingerprints(fingerprintDelete.EmployeeId);
            }

            // Retornar respuesta de éxito
            return new ApiResponse<bool>(
                success: true,
                message: result ? "Huellas eliminadas correctamente." : "No se encontraron huellas para eliminar.",
                data: result
            );
        }
        catch (Exception ex)
        {
            // Manejo de errores
            return new ApiResponse<bool>(
                success: false,
                message: $"Error al eliminar huellas: {ex.Message}"
            );
        }
    }


    
    /// <summary>
    /// Registra una nueva huella digital en la base de datos, asociándola a un usuario específico.
    /// </summary>
    /// <param name="fingerprint">La huella digital a registrar.</param>
    /// <param name="userId">El identificador del usuario al que pertenece la huella.</param>
    /// <returns>Una respuesta que indica si el registro fue exitoso.</returns>
    private ApiResponse<bool> EnrollFingerprint(Fingerprint fingerprint)
    {
        // Valida que la huella no sea nula
        if (fingerprint == null || fingerprint.Fmd == null || fingerprint.Fmd.Length == 0)
        {
            const string errorMessage = "Fingerprint data is missing.";
            _logger.LogError(errorMessage);
            return new ApiResponse<bool>(false, errorMessage);
        }

        // Convierte el FMD a formato Base64 para comparar
        var fingerprintBase64 = Convert.ToBase64String(fingerprint.Fmd);
        

        // Si no existe, registra la nueva huella
        try
        {
            fingerprint.CreatedDate = DateTime.UtcNow;
            _fingerprintRepository.AddFingerprint(fingerprint);

            _logger.LogInformation($"Fingerprint successfully enrolled for Employee ID={fingerprint.EmployeeId}.");
            return new ApiResponse<bool>(true, "Fingerprint successfully enrolled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enroll fingerprint.");
            return new ApiResponse<bool>(false, "Failed to enroll fingerprint.");
        }
    }
    
    
    /// <summary>
    /// Valida y convierte datos en formato Base64 a un arreglo de bytes.
    /// </summary>
    /// <param name="base64Data">Los datos en formato Base64.</param>
    /// <param name="errorMessage">Mensaje de error personalizado.</param>
    /// <returns>Una respuesta que contiene los datos convertidos o un mensaje de error.</returns>    
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
            _logger.LogError(ex, "Datos Base64 inválidos."); // Registra un error en el log
            return new ApiResponse<byte[]>(false, "Datos Base64 inválidos.", null);
        }
    }
    
    
    /// <summary>
    /// Procesa una imagen de una huella digital cruda para su registro.
    /// </summary>
    /// <param name="imageRequest">Una solicitud que contiene la imagen de la huella digital y la información del empleado.</param>
    /// <returns>Una respuesta que contiene el FMD de la huella registrada o un mensaje de error.</returns>
    private ApiResponse<Fingerprint> ProcessRaw(DtoFingerprintImageRequest imageRequest)
    {
        try
        {
            // Log inicial para verificar la solicitud
            _logger.LogInformation($"Iniciando el procesamiento de huellas para EmployeeId: {imageRequest.EmployeeId}, Finger: {imageRequest.Finger}");

            // Validar las entradas
            if (imageRequest.FingerprintsData == null || imageRequest.FingerprintsData.Length < 2)
            {
                _logger.LogError("No se proporcionaron suficientes huellas digitales.");
                return new ApiResponse<Fingerprint>(false, "Se requieren al menos dos muestras de la huella digital.");
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
                var validation = ValidateAndConvertBase64ToBytes(fingerprintData, "Los datos de la huella dactilar no son válidos.");
                if (!validation.Success)
                {
                    _logger.LogError("Una o más huellas digitales son inválidas.");
                    return new ApiResponse<Fingerprint>(false, "Una o más huellas digitales son inválidas.");
                }
                fingerprintBytesList.Add(validation.Data);
            }

            // Crear FMDs para cada huella digital
            var fmds = fingerprintBytesList.Select(CreateFMD).ToList();

            // Validar resultados de los FMDs
            if (fmds.Any(f => f.ResultCode != Constants.ResultCode.DP_SUCCESS))
            {
                _logger.LogError("Error al generar los FMDs para las huellas digitales. Verifica que los datos sean correctos y en el formato corrrespondiente (RAW).");
                return new ApiResponse<Fingerprint>(false, "Error al generar los FMDs para las huellas digitales.  Verifica que los datos sean correctos y en el formato corrrespondiente (RAW)");
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
                FmdQuality = 90,
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
    
    /// <summary>
    /// Crea un FMD (Fingerprint Minutiae Data) a partir de una imagen de huella digital en bruto.
    /// </summary>
    /// <param name="rawImageData">La imagen de la huella digital en formato de bytes.</param>
    /// <returns>Un resultado que contiene el FMD generado o un código de error.</returns>
    private DataResult<Fmd> CreateFMD(byte[] rawImageData)
    {
        return FeatureExtraction.CreateFmdFromRaw(
            rawImageData,
            fingerPosition: 1,        // Posición del dedo
            CbeffId: 3407615,         // ID compatible con el SDK
            width: 500,               // Ajustar según los requisitos del lector
            height: 500,              // Ajustar según los requisitos del lector
            resolution: 700,          // Ajustar según los requisitos del lector
            Constants.Formats.Fmd.ISO // Formato ISO
        );
        
    }
}
