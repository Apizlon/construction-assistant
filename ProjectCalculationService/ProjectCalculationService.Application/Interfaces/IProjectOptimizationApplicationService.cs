using ProjectCalculationService.Application.Contracts.Optimizations;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectOptimizationApplicationService
{
    Task<IReadOnlyList<ProjectOptimizationListItemResponse>> GetOptimizationsAsync(string projectId, string actorUserId);
    Task<ProjectOptimizationResponse> GetOptimizationAsync(string projectId, string optimizationId, string actorUserId);
    Task<ProjectOptimizationPreviewResponse> PreviewAsync(string projectId, CreateOptimizationPreviewRequest request);
    Task<ProjectOptimizationResponse> CreateAsync(string projectId, CreateOptimizationRequest request);
    Task DeleteAsync(string projectId, string optimizationId, string actorUserId);
    Task<IReadOnlyList<OptimizationTemplateResponse>> GetTemplatesAsync(string projectId, string actorUserId);
}

