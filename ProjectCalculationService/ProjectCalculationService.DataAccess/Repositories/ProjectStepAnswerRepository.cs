using Microsoft.EntityFrameworkCore;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.DataAccess.Repositories;

public class ProjectStepAnswerRepository : IProjectStepAnswerRepository
{
    private readonly ProjectCalculationDbContext _dbContext;

    public ProjectStepAnswerRepository(ProjectCalculationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProjectStepAnswer>> GetByProjectIdAsync(Guid projectId)
    {
        return await _dbContext.ProjectStepAnswers
            .Where(a => a.ProjectId == projectId)
            .ToListAsync();
    }

    public Task<ProjectStepAnswer?> GetByProjectAndStepAsync(Guid projectId, string stepCode)
    {
        return _dbContext.ProjectStepAnswers
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.StepCode == stepCode);
    }

    public async Task UpsertAsync(ProjectStepAnswer answer)
    {
        var exists = await _dbContext.ProjectStepAnswers.AnyAsync(a => a.Id == answer.Id);
        if (exists)
        {
            _dbContext.ProjectStepAnswers.Update(answer);
        }
        else
        {
            _dbContext.ProjectStepAnswers.Add(answer);
        }

        await _dbContext.SaveChangesAsync();
    }
}

