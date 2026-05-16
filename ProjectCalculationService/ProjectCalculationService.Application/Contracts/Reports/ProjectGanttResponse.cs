namespace ProjectCalculationService.Application.Contracts.Reports;

public class ProjectGanttResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public int TotalMonths { get; set; }
    public int TotalDays { get; set; }
    public IReadOnlyList<GanttTaskResponse> Tasks { get; set; } = Array.Empty<GanttTaskResponse>();
}

