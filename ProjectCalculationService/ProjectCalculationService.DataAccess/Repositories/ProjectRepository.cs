using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.DataAccess.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ProjectCalculationDbContext _dbContext;

    public ProjectRepository(ProjectCalculationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Project?> GetByIdAsync(Guid id)
    {
        return _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Project>> GetByOwnerUserIdAsync(Guid ownerUserId)
    {
        return await _dbContext.Projects
            .Where(p => p.OwnerUserId == ownerUserId)
            .ToListAsync();
    }

    public async Task CreateAsync(Project project)
    {
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        _dbContext.Projects.Update(project);
        await _dbContext.SaveChangesAsync();
    }
}
