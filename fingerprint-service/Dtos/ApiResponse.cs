namespace fingerprint_service.Dtos;

public class ApiResponse<T>
{
    /// <summary>
    /// Propiedad que indica si la operación fue exitosa.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Mensaje que describe el resultado de la operación.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// Datos adicionales que se desean devolver en la respuesta.
    /// </summary>
    public T Data { get; set; }
    
    public ApiResponse(bool success, string? message, T data = default)
    {
        Success = success;
        Message = message;
        Data = data;
    }
    
}
