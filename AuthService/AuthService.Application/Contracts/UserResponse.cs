using AuthService.Application.Models;

namespace AuthService.Application.Contracts;

public class UserResponse
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
}

