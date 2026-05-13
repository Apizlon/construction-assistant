namespace ProjectCalculationService.Application.Models;

public class ProjectStepAnswer
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public StepAnswerType AnswerType { get; set; }
    public string? SelectedOptionCode { get; set; }
    public string? ValueJson { get; set; }
    public StepAnswerSource Source { get; set; } = StepAnswerSource.User;
    public Guid? UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum StepAnswerType
{
    Option = 0,
    Slider = 1,
    Number = 2,
    Text = 3,
    MultiSelect = 4,
    Composite = 5
}

public enum StepAnswerSource
{
    User = 0,
    Balanced = 1,
    Recommendation = 2
}

