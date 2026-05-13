using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Project>> GetByOwnerUserIdAsync(Guid ownerUserId);
    Task CreateAsync(Project project);
    Task UpdateAsync(Project project);
}
