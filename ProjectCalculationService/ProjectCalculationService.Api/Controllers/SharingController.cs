using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
public class SharingController : ControllerBase
{
    private readonly IProjectCalculationApplicationService _service;

    public SharingController(IProjectCalculationApplicationService service)
    {
        _service = service;
    }

    [HttpPost("projects/{projectId}/share-code")]
    public Task<CreateShareCodeResponse> CreateShareCode([FromRoute] string projectId)
    {
        return _service.CreateOrGetShareCodeAsync(projectId);
    }

    [HttpPost("projects/join-by-code")]
    public Task<JoinByCodeResponse> JoinByCode([FromBody] JoinByCodeRequest request)
    {
        return _service.JoinByCodeAsync(request);
    }
}

