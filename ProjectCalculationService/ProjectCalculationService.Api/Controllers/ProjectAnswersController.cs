using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
[Route("projects/{projectId}/answers")]
public class ProjectAnswersController : ControllerBase
{
    private readonly IProjectCalculationApplicationService _service;

    public ProjectAnswersController(IProjectCalculationApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IReadOnlyList<StepAnswerResponse>> GetAll([FromRoute] string projectId)
    {
        return _service.GetProjectAnswersAsync(projectId);
    }

    [HttpPut]
    public Task<StepAnswerResponse> Upsert([FromRoute] string projectId, [FromBody] UpsertStepAnswerRequest request)
    {
        return _service.UpsertProjectAnswerAsync(projectId, request);
    }
}

