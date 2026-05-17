using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts.Optimizations;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
[Route("projects/{projectId}/optimizations")]
public class ProjectOptimizationsController : ControllerBase
{
    private readonly IProjectOptimizationApplicationService _service;

    public ProjectOptimizationsController(IProjectOptimizationApplicationService service)
    {
        _service = service;
    }

    [HttpGet("templates")]
    public Task<IReadOnlyList<OptimizationTemplateResponse>> GetTemplates([FromRoute] string projectId, [FromQuery] string actorUserId)
    {
        return _service.GetTemplatesAsync(projectId, actorUserId);
    }

    [HttpGet]
    public Task<IReadOnlyList<ProjectOptimizationListItemResponse>> List([FromRoute] string projectId, [FromQuery] string actorUserId)
    {
        return _service.GetOptimizationsAsync(projectId, actorUserId);
    }

    [HttpGet("{optimizationId}")]
    public Task<ProjectOptimizationResponse> Get([FromRoute] string projectId, [FromRoute] string optimizationId, [FromQuery] string actorUserId)
    {
        return _service.GetOptimizationAsync(projectId, optimizationId, actorUserId);
    }

    [HttpPost("preview")]
    public Task<ProjectOptimizationPreviewResponse> Preview([FromRoute] string projectId, [FromBody] CreateOptimizationPreviewRequest request)
    {
        return _service.PreviewAsync(projectId, request);
    }

    [HttpPost]
    public Task<ProjectOptimizationResponse> Create([FromRoute] string projectId, [FromBody] CreateOptimizationRequest request)
    {
        return _service.CreateAsync(projectId, request);
    }

    [HttpDelete("{optimizationId}")]
    public Task Delete([FromRoute] string projectId, [FromRoute] string optimizationId, [FromQuery] string actorUserId)
    {
        return _service.DeleteAsync(projectId, optimizationId, actorUserId);
    }
}

