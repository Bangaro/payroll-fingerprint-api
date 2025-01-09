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

    
    private ApiResponse<byte[]> ValidateAndConvertBase64ToBytes(string base64Data, string errorMessage)
    {
        if (string.IsNullOrEmpty(base64Data))
        {
            _logger.LogError(errorMessage);
            return new ApiResponse<byte[]>(false, errorMessage, null);
        }

        try
        {
            return new ApiResponse<byte[]>(true, "Data converted successfully.", Convert.FromBase64String(base64Data));
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 data.");
            return new ApiResponse<byte[]>(false, "Invalid Base64 data.", null);
        }
    }
    
    
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


    public ApiResponse<string> ProcessRaw(DtoFingerprintImageRequest imageRequest)
{
    try
    {
        // Log inicial para verificar la solicitud
        _logger.LogInformation($"Iniciando el procesamiento de huellas para EmployeeId: {imageRequest.EmployeeId}, Finger: {imageRequest.Finger}");

        // Validar las entradas
        if (imageRequest.FingerprintsData == null || imageRequest.FingerprintsData.Length < 2)
        {
            _logger.LogError("No se proporcionaron suficientes huellas digitales.");
            return new ApiResponse<string>(false, "Se requieren al menos dos muestras de la huella digital.");
        }

        // Validar el dedo
        if (!Enum.IsDefined(typeof(Fingers), imageRequest.Finger))
        {
            var validFingers = string.Join(", ", Enum.GetNames(typeof(Fingers)));
            _logger.LogError($"El dedo '{imageRequest.Finger}' no es válido.");
            return new ApiResponse<string>(false, $"El dedo '{imageRequest.Finger}' no es válido. Dedos válidos: {validFingers}");
        }

        // Convertir y validar todas las muestras de la huella digital
        var fingerprintBytesList = new List<byte[]>();
        foreach (var fingerprintData in imageRequest.FingerprintsData)
        {
            var validation = ValidateAndConvertBase64ToBytes(fingerprintData, "Los datos de la huella dactilar no son válidos.");
            if (!validation.Success)
            {
                _logger.LogError("Una o más huellas digitales son inválidas.");
                return new ApiResponse<string>(false, "Una o más huellas digitales son inválidas.");
            }
            fingerprintBytesList.Add(validation.Data);
        }

        // Verificar si las huellas ya existen
        foreach (var fingerprintData in imageRequest.FingerprintsData)
        {
            var fingerprintExists = CompareFingerprint(fingerprintData);
            if (fingerprintExists.Data != null)
            {
                _logger.LogInformation("La huella digital coincide con un registro existente.");
                return new ApiResponse<string>(false, "La huella coincide con un registro existente.", fingerprintExists.Data.Finger.ToString());
            }
        }

        // Crear FMDs para cada huella digital
        var fmds = new List<Fmd>();
        foreach (var fingerprintBytes in fingerprintBytesList)
        {
            var fmdResult = CreateFMD(fingerprintBytes);
            if (fmdResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
            {
                _logger.LogError("Error al generar el FMD para una de las huellas digitales.");
                return new ApiResponse<string>(false, "Error al generar el FMD para una de las huellas digitales.");
            }
            fmds.Add(fmdResult.Data);
        }

        // Crear el FMD de inscripción
        var enrollmentResult = Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ISO, fmds.ToArray());
        if (enrollmentResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
        {
            _logger.LogError("Error al crear el FMD de inscripción.");
            return new ApiResponse<string>(false, "Error al crear el FMD de inscripción.");
        }

        // Guardar la huella digital
        var fingerprint = new Fingerprint
        {
            EmployeeId = imageRequest.EmployeeId,
            Finger = Enum.Parse<Fingers>(imageRequest.Finger),
            Fmd = enrollmentResult.Data.Bytes,
            FmdQuality = 90,
            CreatedDate = DateTime.UtcNow,
        };

        var added = _fingerprintRepository.AddFingerprint(fingerprint);
        if (!added)
        {
            _logger.LogError("Error al guardar la huella digital en la base de datos.");
            return new ApiResponse<string>(false, "Error al guardar la huella digital en la base de datos.");
        }

        _logger.LogInformation("FMD de inscripción creado y guardado exitosamente.");
        return new ApiResponse<string>(true, "FMD de inscripción creado exitosamente.", Convert.ToBase64String(enrollmentResult.Data.Bytes));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error interno al procesar la huella.");
        return new ApiResponse<string>(false, "Error interno al procesar la huella.");
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



    public ApiResponse<Fingerprint> ConvertFingerprintToFMD(Fingerprint fingerprint)
    {
        throw new NotImplementedException();
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
    
    
    public ApiResponse<bool> CompareFingerprints(Fingerprint fingerprint1, Fingerprint fingerprint2)
    {
        try
        {
            // Validar que ambas huellas tengan datos de FMD
            if (fingerprint1?.Fmd == null || fingerprint2?.Fmd == null)
            {
                _logger.LogError("Una o ambas huellas no tienen datos de FMD.");
                return new ApiResponse<bool>(
                    success: false,
                    message: "Una o ambas huellas no tienen datos de FMD.",
                    data: false
                );
            }

            // Convertir los datos de FMD a objetos Fmd
            var fmd1 = Fmd.DeserializeXml(Convert.ToBase64String(fingerprint1.Fmd));
            var fmd2 = Fmd.DeserializeXml(Convert.ToBase64String(fingerprint2.Fmd));

            // Comparar las huellas dactilares usando el SDK
            var comparisonResult = Comparison.Compare(fmd1, 0, fmd2, 0);

            // Definir un umbral de similitud
            const int similarityThreshold = 50;

            // Determinar si las huellas coinciden
            bool areMatching = comparisonResult.Score <= similarityThreshold;

            _logger.LogInformation($"Resultado de la comparación: Score={comparisonResult.Score}, Threshold={similarityThreshold}");

            return new ApiResponse<bool>(
                success: true,
                message: areMatching ? "Las huellas coinciden." : "Las huellas no coinciden.",
                data: areMatching
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error al comparar las huellas dactilares.");
            return new ApiResponse<bool>(
                success: false,
                message: $"Error al comparar las huellas: {ex.Message}",
                data: false
            );
        }
    }


    public ApiResponse<Fingerprint> IdentifyFingerprint()
    {
        throw new NotImplementedException();
    }

    public ApiResponse<Fingerprint> EnrollFingerprint(Fingerprint fingerprint, int userId)
    {
        throw new NotImplementedException();
    }

    public ApiResponse<bool> CancelCapture()
    {
        throw new NotImplementedException();
    }

    public ApiResponse<List<Fingerprint>> GetFingerprints()
    {
        throw new NotImplementedException();
    }

    public ApiResponse<bool> DeleteFingerprint(int fingerprintId)
    {
        throw new NotImplementedException();
    }

    public ApiResponse<bool> ReconnectFingerprintDevice()
    {
        throw new NotImplementedException();
    }
}
