namespace ProjectCalculationService.Application.Contracts.Optimizations;

public class CreateOptimizationPreviewRequest
{
    public string ActorUserId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty; // Water | Electricity
    public GridPointDto Start { get; set; } = new(0, 0);
    public GridPointDto End { get; set; } = new(0, 0);
}

