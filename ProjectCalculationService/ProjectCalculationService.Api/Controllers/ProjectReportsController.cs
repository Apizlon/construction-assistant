using Microsoft.AspNetCore.Mvc;
using ProjectCalculationService.Application.Contracts.Reports;
using ProjectCalculationService.Application.Interfaces;

namespace ProjectCalculationService.Api.Controllers;

[ApiController]
[Route("projects/{projectId}/report")]
public class ProjectReportsController : ControllerBase
{
    private readonly IProjectReportService _reportService;

    public ProjectReportsController(IProjectReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("estimate")]
    public Task<ProjectEstimateBreakdownResponse> GetEstimate([FromRoute] string projectId)
    {
        return _reportService.GetEstimateAsync(projectId);
    }

    [HttpGet("estimate/download")]
    public async Task<IActionResult> DownloadEstimate([FromRoute] string projectId, [FromQuery] string format = "xlsx")
    {
        var (bytes, contentType, fileName) = await _reportService.GetEstimateFileAsync(projectId, format);
        return File(bytes, contentType, fileName);
    }

    [HttpGet("gantt")]
    public Task<ProjectGanttResponse> GetGantt([FromRoute] string projectId)
    {
        return _reportService.GetGanttAsync(projectId);
    }

    [HttpGet("gantt/download")]
    public async Task<IActionResult> DownloadGantt([FromRoute] string projectId, [FromQuery] string format = "xlsx")
    {
        var (bytes, contentType, fileName) = await _reportService.GetGanttFileAsync(projectId, format);
        return File(bytes, contentType, fileName);
    }
}
