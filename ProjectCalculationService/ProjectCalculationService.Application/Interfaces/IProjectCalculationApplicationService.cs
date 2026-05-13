using ProjectCalculationService.Application.Contracts;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectCalculationApplicationService
{
    Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request);
    Task<IReadOnlyList<ProjectListItemResponse>> GetOwnedProjectsAsync(string ownerUserId);
    Task<IReadOnlyList<ProjectListItemResponse>> GetViewerProjectsAsync(string userId);
    Task<ProjectResponse> GetProjectAsync(string projectId);
    Task<ProjectResponse> UpdateProjectAsync(string projectId, UpdateProjectRequest request);

    Task<IReadOnlyList<StepAnswerResponse>> GetProjectAnswersAsync(string projectId);
    Task<StepAnswerResponse> UpsertProjectAnswerAsync(string projectId, UpsertStepAnswerRequest request);

    Task<CreateShareCodeResponse> CreateOrGetShareCodeAsync(string projectId);
    Task<JoinByCodeResponse> JoinByCodeAsync(JoinByCodeRequest request);

    Task<ViewerCommentResponse> AddCommentAsync(string projectId, CreateCommentRequest request);
    Task<IReadOnlyList<ViewerCommentResponse>> GetCommentsAsync(string projectId);
}
