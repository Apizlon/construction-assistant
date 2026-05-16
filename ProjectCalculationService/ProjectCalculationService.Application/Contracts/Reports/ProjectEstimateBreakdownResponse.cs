namespace ProjectCalculationService.Application.Contracts.Reports;

public class ProjectEstimateBreakdownResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }

    public string? BuildingType { get; set; }
    public decimal? AreaM2 { get; set; }

    public int BasePerM2Rub { get; set; }
    public int BaseCostRub { get; set; }

    public decimal Factor { get; set; }
    public IReadOnlyList<EstimateBreakdownItemResponse> Items { get; set; } = Array.Empty<EstimateBreakdownItemResponse>();

    public int TotalRub { get; set; }
    public int Months { get; set; }
}

