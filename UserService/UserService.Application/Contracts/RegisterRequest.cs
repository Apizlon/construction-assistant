using UserService.Application.Models;

namespace UserService.Application.Contracts;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserRole Role { get; set; }
}

