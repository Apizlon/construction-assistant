using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces;

public interface IUserServiceClient
{
    Task<InternalUserByEmailResponse?> GetByEmailAsync(string email);
}

