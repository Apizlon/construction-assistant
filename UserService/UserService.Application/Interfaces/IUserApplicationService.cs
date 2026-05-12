using UserService.Application.Contracts;

namespace UserService.Application.Interfaces;

public interface IUserApplicationService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);

    // Internal API (for other services)
    Task<InternalUserByEmailResponse?> GetByEmailInternalAsync(string email);
    Task<InternalUserResponse?> GetByIdInternalAsync(Guid id);
}

