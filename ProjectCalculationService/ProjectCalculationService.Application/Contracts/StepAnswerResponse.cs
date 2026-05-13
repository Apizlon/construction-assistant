using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Contracts;

public class StepAnswerResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string StepCode { get; set; } = string.Empty;
    public StepAnswerType AnswerType { get; set; }
    public string? SelectedOptionCode { get; set; }
    public string? ValueJson { get; set; }
    public StepAnswerSource Source { get; set; }
    public string? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
}

