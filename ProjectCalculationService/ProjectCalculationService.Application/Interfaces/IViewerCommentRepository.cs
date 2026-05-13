using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Interfaces;

public interface IViewerCommentRepository
{
    Task CreateAsync(ViewerComment comment);
    Task<IReadOnlyList<ViewerComment>> GetByProjectIdAsync(Guid projectId);
}

