using AuthService.Application.Models;

namespace AuthService.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(string userId, string email, UserRole role);
    string GenerateRefreshToken();
}

