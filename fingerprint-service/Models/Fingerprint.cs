namespace fingerprint_service.Models;

public class Fingerprint
{
    public int Id { get; set; }
    public int EmployeeId { get; set; } // Relación con la tabla de empleados
    public Fingers Finger { get; set; } // Ejemplo: "RIGHT_INDEX"
    public byte[] Fmd { get; set; } // Formato estándar de la huella digital
    public DateTime CreatedDate { get; set; } // Fecha de registro
    
}
