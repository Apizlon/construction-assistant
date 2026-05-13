using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
[Route("projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectCalculationApplicationService _service;

    public ProjectsController(IProjectCalculationApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public Task<ProjectResponse> Create([FromBody] CreateProjectRequest request)
    {
        return _service.CreateProjectAsync(request);
    }

    [HttpGet("owned/{ownerUserId}")]
    public Task<IReadOnlyList<ProjectListItemResponse>> GetOwned([FromRoute] string ownerUserId)
    {
        return _service.GetOwnedProjectsAsync(ownerUserId);
    }

    [HttpGet("viewer/{userId}")]
    public Task<IReadOnlyList<ProjectListItemResponse>> GetViewer([FromRoute] string userId)
    {
        return _service.GetViewerProjectsAsync(userId);
    }

    [HttpGet("{projectId}")]
    public Task<ProjectResponse> Get([FromRoute] string projectId)
    {
        return _service.GetProjectAsync(projectId);
    }

    [HttpPatch("{projectId}")]
    public Task<ProjectResponse> Update([FromRoute] string projectId, [FromBody] UpdateProjectRequest request)
    {
        return _service.UpdateProjectAsync(projectId, request);
    }
}
