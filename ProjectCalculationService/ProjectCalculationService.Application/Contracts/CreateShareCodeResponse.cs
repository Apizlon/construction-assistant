namespace ProjectCalculationService.Application.Contracts;

public class CreateShareCodeResponse
{
    public string ShareId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime SharedAt { get; set; }
}

