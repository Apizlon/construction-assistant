namespace ProjectCalculationService.Application.Models;

public class Project
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

