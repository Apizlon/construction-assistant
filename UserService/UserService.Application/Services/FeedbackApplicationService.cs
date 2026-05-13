using Microsoft.Extensions.Logging;
using UserService.Application.Contracts;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Application.Models;

namespace UserService.Application.Services;

public class FeedbackApplicationService : IFeedbackApplicationService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<FeedbackApplicationService> _logger;

    public FeedbackApplicationService(IFeedbackRepository feedbackRepository, ILogger<FeedbackApplicationService> logger)
    {
        _feedbackRepository = feedbackRepository;
        _logger = logger;
    }

    public async Task<FeedbackResponse> CreateAsync(string userId, string email, CreateFeedbackRequest request)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new BadRequestException("Invalid userId");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Invalid email");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new BadRequestException("Message is required");
        }

        var feedback = new FeedbackMessage
        {
            Id = Guid.NewGuid(),
            UserId = parsedUserId,
            Email = email.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _feedbackRepository.CreateAsync(feedback);
        _logger.LogInformation("Feedback created - {FeedbackId} User {UserId}", feedback.Id, feedback.UserId);

        return Map(feedback);
    }

    public async Task<IReadOnlyList<FeedbackResponse>> GetAllAsync()
    {
        var all = await _feedbackRepository.GetAllAsync();
        return all
            .OrderByDescending(f => f.CreatedAt)
            .Select(Map)
            .ToList();
    }

    private static FeedbackResponse Map(FeedbackMessage feedback) => new()
    {
        Id = feedback.Id.ToString(),
        UserId = feedback.UserId.ToString(),
        Email = feedback.Email,
        Message = feedback.Message,
        CreatedAt = feedback.CreatedAt
    };
}

