namespace fingerprint_service.Dtos;

public class FingerprintCompareRequest
{
    /// <summary>
    /// Datos de la huella en formato Base64
    /// </summary>
    public string FingerprintData { get; set; }  
}
