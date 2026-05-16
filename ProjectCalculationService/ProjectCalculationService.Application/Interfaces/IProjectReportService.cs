using ProjectCalculationService.Application.Contracts.Reports;

namespace ProjectCalculationService.Application.Interfaces;

public interface IProjectReportService
{
    Task<ProjectEstimateBreakdownResponse> GetEstimateAsync(string projectId);
    Task<ProjectGanttResponse> GetGanttAsync(string projectId);

    Task<(byte[] Bytes, string ContentType, string FileName)> GetEstimateFileAsync(string projectId, string format);
    Task<(byte[] Bytes, string ContentType, string FileName)> GetGanttFileAsync(string projectId, string format);
}

