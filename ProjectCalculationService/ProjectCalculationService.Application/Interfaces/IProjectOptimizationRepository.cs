using ProjectCalculationService.Application.Models.Optimizations;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectOptimizationRepository
{
    Task CreateAsync(ProjectOptimization optimization);
    Task<ProjectOptimization?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ProjectOptimization>> GetByProjectIdAsync(Guid projectId);
    Task DeleteAsync(ProjectOptimization optimization);
}

