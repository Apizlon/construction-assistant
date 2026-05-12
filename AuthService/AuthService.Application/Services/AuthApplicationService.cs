using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Contracts;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Application.Services;

public class AuthApplicationService : IAuthApplicationService
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthApplicationService> _logger;

    public AuthApplicationService(
        IUserServiceClient userServiceClient,
        IJwtService jwtService,
        ILogger<AuthApplicationService> logger)
    {
        _userServiceClient = userServiceClient;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<TokenResponse> CreateTokenAsync(TokenRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _userServiceClient.GetByEmailAsync(normalizedEmail);
        if (user == null)
        {
            _logger.LogWarning("Token request failed: user not found - {Email}", normalizedEmail);
            throw new BadRequestException("Invalid credentials");
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Token request failed: invalid password - {Email}", normalizedEmail);
            throw new BadRequestException("Invalid credentials");
        }

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

