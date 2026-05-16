namespace ProjectCalculationService.Application.Contracts.Reports;

public class GanttTaskResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    // Relative timeline (no calendar binding) for flexible frontend rendering.
    public int StartDay { get; set; }
    public int EndDay { get; set; }

    public IReadOnlyList<string> DependsOnTaskIds { get; set; } = Array.Empty<string>();
}

