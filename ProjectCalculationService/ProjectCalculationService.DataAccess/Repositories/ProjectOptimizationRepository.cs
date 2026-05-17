using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models.Optimizations;

namespace ProjectCalculationService.DataAccess.Repositories;

public class ProjectOptimizationRepository : IProjectOptimizationRepository
{
    private readonly ProjectCalculationDbContext _dbContext;

    public ProjectOptimizationRepository(ProjectCalculationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(ProjectOptimization optimization)
    {
        _dbContext.ProjectOptimizations.Add(optimization);
        await _dbContext.SaveChangesAsync();
    }

    public Task<ProjectOptimization?> GetByIdAsync(Guid id)
    {
        return _dbContext.ProjectOptimizations.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<ProjectOptimization>> GetByProjectIdAsync(Guid projectId)
    {
        return await _dbContext.ProjectOptimizations
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteAsync(ProjectOptimization optimization)
    {
        _dbContext.ProjectOptimizations.Remove(optimization);
        await _dbContext.SaveChangesAsync();
    }
}

