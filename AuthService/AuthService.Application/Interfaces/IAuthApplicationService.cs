using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces;

public interface IAuthApplicationService
{
    Task<TokenResponse> CreateTokenAsync(TokenRequest request);
}

