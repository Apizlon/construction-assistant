namespace ProjectCalculationService.Application.Contracts.Optimizations;

public class ProjectOptimizationPreviewResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string CommunicationType { get; set; } = string.Empty;
    public GridPointDto Start { get; set; } = new(0, 0);
    public GridPointDto End { get; set; } = new(0, 0);
    public IReadOnlyList<OptimizationVariantResponse> Variants { get; set; } = Array.Empty<OptimizationVariantResponse>();
}

public class OptimizationVariantResponse
{
    public string VariantType { get; set; } = string.Empty;
    public bool IsFound { get; set; }
    public string? Message { get; set; }

    public double LengthUnits { get; set; }
    public int Turns { get; set; }
    public double WeightedCost { get; set; }

    public IReadOnlyList<GridPointDto> Path { get; set; } = Array.Empty<GridPointDto>();

    public IReadOnlyList<string> Pros { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Cons { get; set; } = Array.Empty<string>();
}

