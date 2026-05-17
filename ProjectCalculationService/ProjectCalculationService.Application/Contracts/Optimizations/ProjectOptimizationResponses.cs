namespace ProjectCalculationService.Application.Contracts.Optimizations;

public class ProjectOptimizationListItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty;
    public GridPointDto Start { get; set; } = new(0, 0);
    public GridPointDto End { get; set; } = new(0, 0);
    public string? SelectedVariant { get; set; }
    public double? SelectedLengthUnits { get; set; }
}

public class ProjectOptimizationResponse : ProjectOptimizationListItemResponse
{
    public ProjectOptimizationPreviewResponse Result { get; set; } = new();
}

