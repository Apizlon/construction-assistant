namespace ProjectCalculationService.Application.Models;

public class SharedProjectViewer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ShareId { get; set; }
    public bool IsActive { get; set; } = true;
}

