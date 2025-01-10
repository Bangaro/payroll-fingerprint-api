using fingerprint_service.Dtos;
using fingerprint_service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
    /// Procesa e inscribe una huella digital en formato Base64.
    /// </summary>
    /// <param name="request">Un objeto DtoFingerprintImageRequest que contiene los datos de la huella digital
    ///   que se va a procesar e inscribir.</param>
    /// <returns>
    ///   Un objeto IActionResult que indica el resultado de la operación. En caso de éxito,
    ///   devuelve un código de estado 200 OK y el objeto ApiResponse que contiene la información de la respuesta.
    /// </returns>
    [HttpPost("process-fingerprint")]
    public IActionResult ProcessAndEnrollFingerprint([FromBody] DtoFingerprintImageRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(ms => ms.Value.Errors.Any())
                .ToDictionary(
                    ms => ms.Key,
                    ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return BadRequest(new
            {
                success = false,
                message = "Se encontraron errores de validación.",
                errors
            });
        }

        try
        {
            var response = _fingerprintService.ProcessAndEnrollFingerprint(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(
                success: false,
                message: "Error interno al procesar e inscribir la huella digital."
            ));
        }
    }
    
    
    /// <summary>
    /// Identifica una persona a partir de una huella digital.
    /// </summary>
    /// <param name="request">Un objeto FingerprintCompareRequest que contiene los datos de la huella digital a identificar.</param>
    /// <returns>
    /// Un objeto IActionResult que indica el resultado de la operación. 
    /// En caso de éxito, devuelve un código de estado 200 OK y el objeto ApiResponse que contiene la información de la persona identificada. 
    /// En caso de que no se encuentre ninguna coincidencia, devuelve un código de estado 404 NotFound y el objeto ApiResponse con un mensaje de error. 
    /// En caso de error interno, devuelve un código de estado 500 InternalServerError y un objeto ApiResponse con un mensaje de error genérico.
    /// </returns>
    [HttpPost("identify-fingerprint")]
    public IActionResult IdentifyFingerprint([FromBody] FingerprintCompareRequest request)
    {
        if (string.IsNullOrEmpty(request.FingerprintData))
        {
            return BadRequest(new ApiResponse<string>(
                success: false,
                message: "Los datos de la huella son obligatorios."
            ));
        }

        try
        {
            var result = _fingerprintService.IdentifyFingerprint(request.FingerprintData);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<DtoFingerprintResponse>(
                success: false,
                message: "Error interno al identificar la huella digital."
            ));
        }
    }


    /// <summary>
    /// Compara una huella digital con las huellas almacenadas en la base de datos.
    /// </summary>
    /// <param name="request">Un objeto FingerprintCompareRequest que contiene los datos de la huella digital a comparar.</param>
    /// <returns>
    /// Un objeto IActionResult que indica el resultado de la operación. 
    /// En caso de éxito, devuelve un código de estado 200 OK y el objeto ApiResponse que contiene la información de la huella coincidente (si la hay). 
    /// En caso de que no se encuentre ninguna coincidencia, devuelve un código de estado 200 OK y el objeto ApiResponse con un mensaje indicando que no se encontraron coincidencias.
    /// En caso de error, devuelve un código de estado 500 InternalServerError y un objeto ApiResponse con un mensaje de error genérico. 
    /// </returns>
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
            return NotFound(result);
        }
    }
    

    /// <summary>
    /// Elimina una huella digital o todas las huellas de un empleado.
    /// </summary>
    /// <param name="request">Un objeto DtoFingerprintDelete que contiene el ID del empleado y opcionalmente el dedo de la huella a eliminar.</param>
    /// <returns>
    /// Un objeto IActionResult que indica el resultado de la operación. 
    /// En caso de éxito, devuelve un código de estado 200 OK y el objeto ApiResponse que indica que la eliminación fue exitosa. 
    /// En caso de error de validación, devuelve un código de estado 400 BadRequest y un objeto ApiResponse con un mensaje de error. 
    /// En caso de que no se encuentre la huella o el empleado, devuelve un código de estado 404 NotFound y el objeto ApiResponse con un mensaje de error. 
    /// En caso de error interno, devuelve un código de estado 500 InternalServerError y un objeto ApiResponse con un mensaje de error genérico.
    /// </returns>
    [HttpDelete("delete-fingerprint")]
    public IActionResult DeleteFingerprint([FromBody] DtoFingerprintDelete request)
    {
        if (request.EmployeeId <= 0)
        {
            return BadRequest(new ApiResponse<bool>(
                success: false,
                message: "El ID del empleado es obligatorio."
            ));
        }

        try
        {
            var result = _fingerprintService.DeleteFingerprint(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<bool>(
                success: false,
                message: "Error interno al eliminar la huella digital."
            ));
        }
    }

}
