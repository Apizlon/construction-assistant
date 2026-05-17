using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.DataAccess.Repositories;

public class SharingRepository : ISharingRepository
{
    private readonly ProjectCalculationDbContext _dbContext;

    public SharingRepository(ProjectCalculationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SharedProject?> GetActiveShareByProjectIdAsync(Guid projectId)
    {
        return _dbContext.SharedProjects
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.IsActive);
    }

    public Task<SharedProject?> GetActiveShareByCodeAsync(string code)
    {
        return _dbContext.SharedProjects
            .FirstOrDefaultAsync(s => s.Code == code && s.IsActive);
    }

    public async Task CreateShareAsync(SharedProject share)
    {
        _dbContext.SharedProjects.Add(share);
        await _dbContext.SaveChangesAsync();
    }

    public Task<SharedProjectViewer?> GetViewerAsync(Guid userId, Guid shareId)
    {
        return _dbContext.SharedProjectViewers
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ShareId == shareId);
    }

    public async Task UpsertViewerAsync(SharedProjectViewer viewer)
    {
        var exists = await _dbContext.SharedProjectViewers.AnyAsync(v => v.Id == viewer.Id);
        if (exists)
        {
            _dbContext.SharedProjectViewers.Update(viewer);
        }
        else
        {
            _dbContext.SharedProjectViewers.Add(viewer);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Project>> GetViewerProjectsAsync(Guid userId)
    {
        var query =
            from viewer in _dbContext.SharedProjectViewers
            join share in _dbContext.SharedProjects on viewer.ShareId equals share.Id
            join project in _dbContext.Projects on share.ProjectId equals project.Id
            where viewer.UserId == userId && viewer.IsActive && share.IsActive
            select project;

        return await query.Distinct().ToListAsync();
    }

    public async Task<bool> IsViewerOfProjectAsync(Guid userId, Guid projectId)
    {
        var query =
            from viewer in _dbContext.SharedProjectViewers
            join share in _dbContext.SharedProjects on viewer.ShareId equals share.Id
            where viewer.UserId == userId
                  && viewer.IsActive
                  && share.IsActive
                  && share.ProjectId == projectId
            select viewer.Id;

        return await query.AnyAsync();
    }
}

