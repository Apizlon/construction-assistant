using UserService.Application.Models;

namespace UserService.Application.Interfaces;

public interface IFeedbackRepository
{
    Task CreateAsync(FeedbackMessage feedback);
    Task<IReadOnlyList<FeedbackMessage>> GetAllAsync();
}

