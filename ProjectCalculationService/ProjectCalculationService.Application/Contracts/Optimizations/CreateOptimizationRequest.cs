namespace ProjectCalculationService.Application.Contracts.Optimizations;

public class CreateOptimizationRequest : CreateOptimizationPreviewRequest
{
    public string SelectedVariant { get; set; } = string.Empty; // WallFriendly | Direct
}

