using System.ComponentModel.DataAnnotations;

namespace fingerprint_service.Dtos;

public class FingerprintCompareRequest
{
    /// <summary>
    /// Datos de la huella en formato Base64
    /// </summary>
    [Required(ErrorMessage = "Se requiere una muestra de la huella digital.")]
    public string FingerprintData { get; set; }  
    
        
    /// <summary>
    /// Identificador la empresa desde la cual se envía la solicitud.
    /// </summary>
    [Required(ErrorMessage = "El ID de la  compañía es obligatorio.")]
    public int CompanyId { get; set; }
}
