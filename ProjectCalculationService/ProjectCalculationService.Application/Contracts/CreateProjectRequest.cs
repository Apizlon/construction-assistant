namespace ProjectCalculationService.Application.Contracts;

public class CreateProjectRequest
{
    public string OwnerUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

