namespace ProjectCalculationService.Application.Contracts.Optimizations;

public class OptimizationTemplateResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Apartment | House | Office | Warehouse
    public int Width { get; set; }
    public int Height { get; set; }
    public IReadOnlyList<RectDto> Walls { get; set; } = Array.Empty<RectDto>();
    public IReadOnlyList<RectDto> ForbiddenZones { get; set; } = Array.Empty<RectDto>();
}

