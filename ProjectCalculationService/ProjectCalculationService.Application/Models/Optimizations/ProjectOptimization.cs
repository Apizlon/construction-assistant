namespace ProjectCalculationService.Application.Models.Optimizations;

public class ProjectOptimization
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public CommunicationType CommunicationType { get; set; }
    public string TemplateId { get; set; } = string.Empty;

    public int StartX { get; set; }
    public int StartY { get; set; }
    public int EndX { get; set; }
    public int EndY { get; set; }

    public OptimizationVariantType? SelectedVariant { get; set; }

    // JSON payload with both computed variants + metrics.
    public string ResultJson { get; set; } = "{}";
}

