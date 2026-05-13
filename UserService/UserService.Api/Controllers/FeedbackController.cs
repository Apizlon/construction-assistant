using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Contracts;
using UserService.Application.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackApplicationService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFeedbackApplicationService feedbackService, ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost]
    public async Task<FeedbackResponse> Create([FromBody] CreateFeedbackRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        _logger.LogInformation("Feedback create attempt - {UserId} {Email}", userId, email);
        return await _feedbackService.CreateAsync(userId, email, request);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IReadOnlyList<FeedbackResponse>> GetAll()
    {
        _logger.LogInformation("Feedback list request by admin {UserId}",
            User.FindFirstValue(ClaimTypes.NameIdentifier));
        return await _feedbackService.GetAllAsync();
    }
}

