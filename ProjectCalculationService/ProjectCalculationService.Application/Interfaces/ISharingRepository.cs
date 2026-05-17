using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Interfaces;

public interface ISharingRepository
{
    Task<SharedProject?> GetActiveShareByProjectIdAsync(Guid projectId);
    Task<SharedProject?> GetActiveShareByCodeAsync(string code);
    Task CreateShareAsync(SharedProject share);
    Task<SharedProjectViewer?> GetViewerAsync(Guid userId, Guid shareId);
    Task UpsertViewerAsync(SharedProjectViewer viewer);
    Task<IReadOnlyList<Project>> GetViewerProjectsAsync(Guid userId);
    Task<bool> IsViewerOfProjectAsync(Guid userId, Guid projectId);
}

