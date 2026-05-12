using Microsoft.AspNetCore.Mvc;
using UserService.Application.Contracts;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly IUserApplicationService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserApplicationService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<UserResponse> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Register attempt for {Email}", request.Email);
        return await _userService.RegisterAsync(request);
    }
}

