using System.ComponentModel.DataAnnotations;

namespace fingerprint_service.Dtos;

public class DtoFingerprintImageRequest
{
    /// <summary>
    /// Identificador del usuario asociado a la huella.
    /// </summary>
    [Required(ErrorMessage = "El ID del empleado es obligatorio.")]
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Dedo al que corresponde la huella.
    /// </summary>
    [Required(ErrorMessage = "Indicar el dedo es obligatorio.")]
    public string Finger { get; set; }

    /// <summary>
    /// Colección de las muestras de huella digital en formato Base64.
    /// </summary>
    [Required(ErrorMessage = "Se requiere al menos una muestra de la huella digital.")]
    public string[] FingerprintsData { get; set; }
}