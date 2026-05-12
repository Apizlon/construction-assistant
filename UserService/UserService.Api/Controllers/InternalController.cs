using Microsoft.AspNetCore.Mvc;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("internal/user")]
public class InternalController : ControllerBase
{
    private readonly IUserApplicationService _userService;
    private readonly ILogger<InternalController> _logger;

    public InternalController(IUserApplicationService userService, ILogger<InternalController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        _logger.LogInformation("Internal get by email {Email}", email);
        var user = await _userService.GetByEmailInternalAsync(email);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet("by-id/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Internal get by id {Id}", id);
        var user = await _userService.GetByIdInternalAsync(id);
        return user == null ? NotFound() : Ok(user);
    }
}

