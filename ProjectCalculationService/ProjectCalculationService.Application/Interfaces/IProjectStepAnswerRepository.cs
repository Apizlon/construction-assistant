using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectStepAnswerRepository
{
    Task<IReadOnlyList<ProjectStepAnswer>> GetByProjectIdAsync(Guid projectId);
    Task<ProjectStepAnswer?> GetByProjectAndStepAsync(Guid projectId, string stepCode);
    Task UpsertAsync(ProjectStepAnswer answer);
}

