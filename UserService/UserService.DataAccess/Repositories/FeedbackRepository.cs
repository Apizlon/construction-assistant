using Microsoft.EntityFrameworkCore;
using UserService.Application.Interfaces;
using UserService.Application.Models;

namespace UserService.DataAccess.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly UserDbContext _dbContext;

    public FeedbackRepository(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(FeedbackMessage feedback)
    {
        _dbContext.FeedbackMessages.Add(feedback);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<FeedbackMessage>> GetAllAsync()
    {
        return await _dbContext.FeedbackMessages
            .AsNoTracking()
            .ToListAsync();
    }
}

