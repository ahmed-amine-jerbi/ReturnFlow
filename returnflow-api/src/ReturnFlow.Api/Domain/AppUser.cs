namespace ReturnFlow.Api.Domain;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; } = Role.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
