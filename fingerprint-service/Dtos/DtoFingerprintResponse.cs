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
    public string Finger { get; set; }

    /// <summary>
    /// Identificador de la compañía a la que pertenece el usuario.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Nombre del usuario asociado a la huella.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Correo electrónico del usuario asociado a la huella.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Número de identificación del usuario asociado a la huella.
    /// </summary>
    public string NidUser { get; set; }

    /// <summary>
    /// Número de teléfono del usuario asociado a la huella.
    /// </summary>
    public string Phone { get; set; }

    /// <summary>
    /// Puesto de trabajo del usuario asociado a la huella.
    /// </summary>
    public string Job { get; set; }

    /// <summary>
    /// Indica si la huella está activa o no.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Fecha de creación de la huella.
    /// </summary>
    public DateTime CreatedDate { get; set; } 
}