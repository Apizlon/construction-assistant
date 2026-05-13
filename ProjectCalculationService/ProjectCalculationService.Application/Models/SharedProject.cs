namespace ProjectCalculationService.Application.Models;

public class SharedProject
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime SharedAt { get; set; }
}

