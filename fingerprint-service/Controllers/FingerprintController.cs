using fingerprint_service.db;
using fingerprint_service.Dtos;
using Microsoft.AspNetCore.Mvc;
using fingerprint_service.Services;
using fingerprint_service.Services.Interfaces;
using MySql.Data.MySqlClient;

namespace fingerprint_service.Controllers;

[ApiController]
[Route("[controller]")]
public class FingerprintController : ControllerBase
{
    private readonly IFingerprintService _fingerprintService;

    public FingerprintController(IFingerprintService fingerprintService)
    {
        _fingerprintService = fingerprintService;
    }
    
    [HttpGet("test-connection")]
    public IActionResult TestConnection()
    {
        try
        {
            var response = _fingerprintService.TestDatabaseConnection();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al ejecutar la consulta: {ex.Message}");
        }
    }

    /// <summary>
    /// Procesa una imagen de huella digital en formato Base64.
    /// </summary>
    /// <param name="request">Objeto que contiene los datos de la imagen.</param>
    /// <returns>Una respuesta indicando el éxito o fracaso de la operación.</returns>
    [HttpPost("process-fingerprint")]
    public IActionResult ProcessFingerprint([FromBody] DtoFingerprintImageRequest request)
    {
        
        if (!ModelState.IsValid)
        {
            // Organizar los errores en un diccionario clave-valor
            var errors = ModelState
                .Where(ms => ms.Value.Errors.Any())
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            // Devolver los errores como una respuesta estructurada
            return BadRequest(new
            {
                success = false,
                message = "Se encontraron errores de validación.",
                errors
            });
        }
        
        if (request.FingerprintsData == null || request.FingerprintsData.Length == 0)
        {
            return BadRequest(new ApiResponse<string>(
                success: false,
                message: "No se proporcionaron huellas digitales en la solicitud."
            ));
        }

        // Validar que cada huella en la colección no esté vacía o sea inválida
        foreach (var fingerprintData in request.FingerprintsData)
        {
            if (string.IsNullOrWhiteSpace(fingerprintData))
            {
                return BadRequest(new ApiResponse<string>(
                    success: false,
                    message: "Una o más huellas digitales en la solicitud están vacías o son inválidas."
                ));
            }
        }

        try
        {
            var response = _fingerprintService.ProcessRaw(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (FormatException)
        {
            return BadRequest(new ApiResponse<string>(
                success: false,
                message: "La imagen proporcionada no está en un formato Base64 válido."
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(
                success: false,
                message: "Error interno al procesar la huella.",
                data: null
            ));
        }
    }
    
    [HttpPost("compare-fingerprint")]
    public IActionResult CompareFingerprint([FromBody] FingerprintCompareRequest request)
    {
        if (string.IsNullOrEmpty(request.FingerprintData))
        {
            return BadRequest(new ApiResponse<string>(
                success: false,
                message: "Los datos de la huella son obligatorios."
            ));
        }

        var result = _fingerprintService.CompareFingerprint(request.FingerprintData);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(404, result);
        }
    }
    
    [HttpGet("check-fingerprint-matches", Name = "CheckFingerprintMatches")]
    public IActionResult CheckFingerprintMatches()
    {
        var result = _fingerprintService.CheckFingerprintMatchesInDatabase();

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }


}