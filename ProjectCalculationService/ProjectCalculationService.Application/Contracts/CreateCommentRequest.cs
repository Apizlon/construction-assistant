namespace ProjectCalculationService.Application.Contracts;

public class CreateCommentRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
}

