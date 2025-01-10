using fingerprint_service.Models;

namespace fingerprint_service.Dtos;

public class DtoFingerprintResponse
{
    /// <summary>
    /// Identificador de la huella.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Identificador del usuario asociado a la huella.
    /// </summary>
    public int? EmployeeId { get; set; }
    /// <summary>
    /// Dedo al que corresponde la huella.
    /// </summary>
    public Fingers Finger { get; set; }
    /// <summary>
    /// Fecha de creación de la huella.
    /// </summary>
    public DateTime CreatedDate { get; set; } 
}