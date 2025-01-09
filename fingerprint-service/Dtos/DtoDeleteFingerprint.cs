namespace fingerprint_service.Dtos;

public class DtoDeleteFingerprint
{
    public int EmployeeId { get; set; } // Relación con la tabla de usuarios
    public string? Finger { get; set; } = null; // Referencia el dedo del usuario

}