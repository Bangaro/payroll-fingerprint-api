namespace fingerprint_service.Dtos;

public class DtoFingerprintDelete
{
    /// <summary>
    /// Identificador del usuario asociado a la huella.
    /// </summary>
    public int EmployeeId { get; set; }
    /// <summary>
    /// Indicador de cual dedo será eliminado.
    /// </summary>
    public string? Finger { get; set; } = null; 
}