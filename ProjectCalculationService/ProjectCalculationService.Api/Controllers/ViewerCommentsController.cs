using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
[Route("projects/{projectId}/comments")]
public class ViewerCommentsController : ControllerBase
{
    private readonly IProjectCalculationApplicationService _service;

    public ViewerCommentsController(IProjectCalculationApplicationService service)
    {
        _service = service;
    }

    [HttpPost]
    public Task<ViewerCommentResponse> Add([FromRoute] string projectId, [FromBody] CreateCommentRequest request)
    {
        return _service.AddCommentAsync(projectId, request);
    }

    [HttpGet]
    public Task<IReadOnlyList<ViewerCommentResponse>> Get([FromRoute] string projectId)
    {
        return _service.GetCommentsAsync(projectId);
    }
}

