using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.DataAccess.Repositories;

public class ViewerCommentRepository : IViewerCommentRepository
{
    private readonly ProjectCalculationDbContext _dbContext;

    public ViewerCommentRepository(ProjectCalculationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(ViewerComment comment)
    {
        _dbContext.ViewerComments.Add(comment);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ViewerComment>> GetByProjectIdAsync(Guid projectId)
    {
        return await _dbContext.ViewerComments
            .Where(c => c.ProjectId == projectId)
            .ToListAsync();
    }
}

