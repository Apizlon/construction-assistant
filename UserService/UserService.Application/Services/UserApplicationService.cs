using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using UserService.Application.Contracts;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Application.Models;

namespace UserService.Application.Services;

public class UserApplicationService : IUserApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserApplicationService> _logger;

    public UserApplicationService(IUserRepository userRepository, ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Role == UserRole.Admin)
        {
            throw new BadRequestException(RegistrationError.ADMIN_REGISTRATION_ATTEMPT.ToString());
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await _userRepository.EmailExistsAsync(normalizedEmail))
        {
            throw new BadRequestException(RegistrationError.EMAIL_ALREADY_USED.ToString());
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(request.Password),
            RegistrationDate = DateTime.UtcNow,
            Role = request.Role
        };

        await _userRepository.CreateAsync(user);
        _logger.LogInformation("User registered - {UserId} {Email}", user.Id, user.Email);

        return new UserResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<InternalUserByEmailResponse?> GetByEmailInternalAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return null;
        }

        return new InternalUserByEmailResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role
        };
    }

    public async Task<InternalUserResponse?> GetByIdInternalAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        return new InternalUserResponse
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            RegistrationDate = user.RegistrationDate,
            Role = user.Role
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
