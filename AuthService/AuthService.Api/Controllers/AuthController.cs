using AuthService.Application.Contracts;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthApplicationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthApplicationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<TokenResponse> Token([FromBody] TokenRequest request)
    {
        _logger.LogInformation("Token request for {Email}", request.Email);
        return await _authService.CreateTokenAsync(request);
    }
}

