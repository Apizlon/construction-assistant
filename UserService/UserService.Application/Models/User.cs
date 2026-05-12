namespace UserService.Application.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime RegistrationDate { get; set; }
    public UserRole Role { get; set; }
}
