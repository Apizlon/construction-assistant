namespace ProjectCalculationService.Application.Contracts;

public class JoinByCodeResponse
{
    public string ShareId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ViewerId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

