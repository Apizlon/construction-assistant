namespace ProjectCalculationService.Application.Contracts;

public class JoinByCodeRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

