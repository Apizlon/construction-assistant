namespace ProjectCalculationService.Application.Models;

public class ViewerComment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public DateTime CommentDate { get; set; }
}

