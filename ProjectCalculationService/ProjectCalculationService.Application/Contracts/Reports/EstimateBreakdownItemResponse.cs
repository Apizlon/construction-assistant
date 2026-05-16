namespace ProjectCalculationService.Application.Contracts.Reports;

public class EstimateBreakdownItemResponse
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal PercentDelta { get; set; }
    public int DeltaRub { get; set; }
}

