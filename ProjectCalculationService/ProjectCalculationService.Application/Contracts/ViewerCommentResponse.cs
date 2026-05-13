namespace ProjectCalculationService.Application.Contracts;

public class ViewerCommentResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public DateTime CommentDate { get; set; }
}

