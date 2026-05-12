using UserService.Application.Models;

namespace UserService.Application.Contracts;

public class InternalUserResponse
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime RegistrationDate { get; set; }
    public UserRole Role { get; set; }
}

