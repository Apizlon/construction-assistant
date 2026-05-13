using UserService.Application.Contracts;

namespace UserService.Application.Interfaces;

public interface IFeedbackApplicationService
{
    Task<FeedbackResponse> CreateAsync(string userId, string email, CreateFeedbackRequest request);
    Task<IReadOnlyList<FeedbackResponse>> GetAllAsync();
}

