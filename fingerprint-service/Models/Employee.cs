namespace fingerprint_service.Models;

public class Employee
{
    public int Id { get; set; }
    public int IdCompany { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string NidUser { get; set; }
    public string Phone { get; set; }
    public string Job { get; set; }
    public bool IsActive { get; set; }

}