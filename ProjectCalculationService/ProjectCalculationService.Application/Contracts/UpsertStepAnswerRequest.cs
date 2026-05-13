using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Contracts;

public class UpsertStepAnswerRequest
{
    public string StepCode { get; set; } = string.Empty;
    public StepAnswerType AnswerType { get; set; }
    public string? SelectedOptionCode { get; set; }
    public string? ValueJson { get; set; }
    public StepAnswerSource Source { get; set; } = StepAnswerSource.User;
    public string? UpdatedByUserId { get; set; }
}

