using Microsoft.Extensions.Logging;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Exceptions;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;

namespace ProjectCalculationService.Application.Services;

public class ProjectCalculationApplicationService : IProjectCalculationApplicationService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectStepAnswerRepository _answerRepository;
    private readonly ISharingRepository _sharingRepository;
    private readonly IViewerCommentRepository _commentRepository;
    private readonly ILogger<ProjectCalculationApplicationService> _logger;

    public ProjectCalculationApplicationService(
        IProjectRepository projectRepository,
        IProjectStepAnswerRepository answerRepository,
        ISharingRepository sharingRepository,
        IViewerCommentRepository commentRepository,
        ILogger<ProjectCalculationApplicationService> logger)
    {
        _projectRepository = projectRepository;
        _answerRepository = answerRepository;
        _sharingRepository = sharingRepository;
        _commentRepository = commentRepository;
        _logger = logger;
    }

    public async Task<ProjectResponse> CreateProjectAsync(CreateProjectRequest request)
    {
        if (!Guid.TryParse(request.OwnerUserId, out var ownerUserId))
        {
            throw new BadRequestException("Invalid OwnerUserId");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException("Project name is required");
        }

        var now = DateTime.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = request.Name.Trim(),
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _projectRepository.CreateAsync(project);
        _logger.LogInformation("Project created - {ProjectId} Owner {OwnerUserId}", project.Id, project.OwnerUserId);

        return MapProject(project);
    }

    public async Task<IReadOnlyList<ProjectListItemResponse>> GetOwnedProjectsAsync(string ownerUserId)
    {
        if (!Guid.TryParse(ownerUserId, out var ownerId))
        {
            throw new BadRequestException("Invalid ownerUserId");
        }

        var projects = await _projectRepository.GetByOwnerUserIdAsync(ownerId);
        return projects
            .OrderByDescending(p => p.UpdatedAt)
            .Select(MapProjectListItem)
            .ToList();
    }

    public async Task<IReadOnlyList<ProjectListItemResponse>> GetViewerProjectsAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var viewerUserId))
        {
            throw new BadRequestException("Invalid userId");
        }

        var projects = await _sharingRepository.GetViewerProjectsAsync(viewerUserId);
        return projects
            .OrderByDescending(p => p.UpdatedAt)
            .Select(MapProjectListItem)
            .ToList();
    }

    public async Task<ProjectResponse> GetProjectAsync(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found");
        }

        return MapProject(project);
    }

    public async Task<IReadOnlyList<StepAnswerResponse>> GetProjectAnswersAsync(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        var answers = await _answerRepository.GetByProjectIdAsync(id);
        return answers
            .OrderBy(a => a.StepCode)
            .Select(MapAnswer)
            .ToList();
    }

    public async Task<StepAnswerResponse> UpsertProjectAnswerAsync(string projectId, UpsertStepAnswerRequest request)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        if (string.IsNullOrWhiteSpace(request.StepCode))
        {
            throw new BadRequestException("StepCode is required");
        }

        Guid? updatedBy = null;
        if (!string.IsNullOrWhiteSpace(request.UpdatedByUserId))
        {
            if (!Guid.TryParse(request.UpdatedByUserId, out var parsed))
            {
                throw new BadRequestException("Invalid UpdatedByUserId");
            }
            updatedBy = parsed;
        }

        var existing = await _answerRepository.GetByProjectAndStepAsync(id, request.StepCode.Trim());

        var answer = existing ?? new ProjectStepAnswer
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
            StepCode = request.StepCode.Trim()
        };

        answer.AnswerType = request.AnswerType;
        answer.SelectedOptionCode = request.SelectedOptionCode?.Trim();
        answer.ValueJson = request.ValueJson;
        answer.Source = request.Source;
        answer.UpdatedByUserId = updatedBy;
        answer.UpdatedAt = DateTime.UtcNow;

        await _answerRepository.UpsertAsync(answer);
        return MapAnswer(answer);
    }

    public async Task<CreateShareCodeResponse> CreateOrGetShareCodeAsync(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found");
        }

        var existingShare = await _sharingRepository.GetActiveShareByProjectIdAsync(id);
        if (existingShare != null)
        {
            return MapShare(existingShare);
        }

        var share = new SharedProject
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
            Code = GenerateShareCode(),
            IsActive = true,
            SharedAt = DateTime.UtcNow
        };

        await _sharingRepository.CreateShareAsync(share);
        return MapShare(share);
    }

    public async Task<JoinByCodeResponse> JoinByCodeAsync(JoinByCodeRequest request)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new BadRequestException("Invalid userId");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BadRequestException("Code is required");
        }

        var share = await _sharingRepository.GetActiveShareByCodeAsync(request.Code.Trim());
        if (share == null)
        {
            throw new NotFoundException("Share code not found or inactive");
        }

        var existingViewer = await _sharingRepository.GetViewerAsync(userId, share.Id);
        var viewer = existingViewer ?? new SharedProjectViewer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShareId = share.Id
        };
        viewer.IsActive = true;

        await _sharingRepository.UpsertViewerAsync(viewer);

        return new JoinByCodeResponse
        {
            ShareId = share.Id.ToString(),
            ProjectId = share.ProjectId.ToString(),
            ViewerId = viewer.Id.ToString(),
            IsActive = viewer.IsActive
        };
    }

    public async Task<ViewerCommentResponse> AddCommentAsync(string projectId, CreateCommentRequest request)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new BadRequestException("Invalid userId");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new BadRequestException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.CommentText))
        {
            throw new BadRequestException("CommentText is required");
        }

        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found");
        }

        var comment = new ViewerComment
        {
            Id = Guid.NewGuid(),
            ProjectId = id,
            UserId = userId,
            Email = request.Email.Trim(),
            CommentText = request.CommentText.Trim(),
            CommentDate = DateTime.UtcNow
        };

        await _commentRepository.CreateAsync(comment);

        return MapComment(comment);
    }

    public async Task<IReadOnlyList<ViewerCommentResponse>> GetCommentsAsync(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new BadRequestException("Invalid projectId");
        }

        var comments = await _commentRepository.GetByProjectIdAsync(id);
        return comments
            .OrderByDescending(c => c.CommentDate)
            .Select(MapComment)
            .ToList();
    }

    private static ProjectResponse MapProject(Project project) => new()
    {
        Id = project.Id.ToString(),
        OwnerUserId = project.OwnerUserId.ToString(),
        Name = project.Name,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt
    };

    private static ProjectListItemResponse MapProjectListItem(Project project) => new()
    {
        Id = project.Id.ToString(),
        OwnerUserId = project.OwnerUserId.ToString(),
        Name = project.Name,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt
    };

    private static StepAnswerResponse MapAnswer(ProjectStepAnswer answer) => new()
    {
        Id = answer.Id.ToString(),
        ProjectId = answer.ProjectId.ToString(),
        StepCode = answer.StepCode,
        AnswerType = answer.AnswerType,
        SelectedOptionCode = answer.SelectedOptionCode,
        ValueJson = answer.ValueJson,
        Source = answer.Source,
        UpdatedByUserId = answer.UpdatedByUserId?.ToString(),
        UpdatedAt = answer.UpdatedAt
    };

    private static ViewerCommentResponse MapComment(ViewerComment comment) => new()
    {
        Id = comment.Id.ToString(),
        ProjectId = comment.ProjectId.ToString(),
        UserId = comment.UserId.ToString(),
        Email = comment.Email,
        CommentText = comment.CommentText,
        CommentDate = comment.CommentDate
    };

    private static CreateShareCodeResponse MapShare(SharedProject share) => new()
    {
        ShareId = share.Id.ToString(),
        ProjectId = share.ProjectId.ToString(),
        Code = share.Code,
        IsActive = share.IsActive,
        SharedAt = share.SharedAt
    };

    private static string GenerateShareCode()
    {
        // Short human-friendly code; keep only uppercase letters and digits.
        // Example: "Q7M4-2K9P"
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[random.Next(alphabet.Length)];
        }

        return $"{new string(chars[..4])}-{new string(chars[4..])}";
    }
}

