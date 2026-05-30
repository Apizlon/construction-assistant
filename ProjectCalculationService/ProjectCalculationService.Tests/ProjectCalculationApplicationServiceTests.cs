using Microsoft.Extensions.Logging;
using Moq;
using ProjectCalculationService.Application.Contracts;
using ProjectCalculationService.Application.Exceptions;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models;
using ProjectCalculationService.Application.Services;
using Xunit;

namespace ProjectCalculationService.Tests;

public class ProjectCalculationApplicationServiceTests
{
    private static ProjectCalculationApplicationService CreateSut(
        out Mock<IProjectRepository> projectRepository,
        out Mock<IProjectStepAnswerRepository> answerRepository,
        out Mock<ISharingRepository> sharingRepository,
        out Mock<IViewerCommentRepository> commentRepository)
    {
        projectRepository = new Mock<IProjectRepository>(MockBehavior.Strict);
        answerRepository = new Mock<IProjectStepAnswerRepository>(MockBehavior.Strict);
        sharingRepository = new Mock<ISharingRepository>(MockBehavior.Strict);
        commentRepository = new Mock<IViewerCommentRepository>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ProjectCalculationApplicationService>>();

        return new ProjectCalculationApplicationService(
            projectRepository.Object,
            answerRepository.Object,
            sharingRepository.Object,
            commentRepository.Object,
            logger.Object);
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidOwnerUserId_ThrowsBadRequest()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateProjectAsync(new CreateProjectRequest
        {
            OwnerUserId = "not-a-guid",
            Name = "Test"
        }));

        Assert.Equal("Invalid OwnerUserId", ex.Message);
    }

    [Fact]
    public async Task CreateProjectAsync_EmptyName_ThrowsBadRequest()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.CreateProjectAsync(new CreateProjectRequest
        {
            OwnerUserId = Guid.NewGuid().ToString(),
            Name = "   "
        }));

        Assert.Equal("Project name is required", ex.Message);
    }

    [Fact]
    public async Task CreateProjectAsync_ValidRequest_CreatesProjectAndReturnsResponse()
    {
        var sut = CreateSut(out var projectRepo, out _, out _, out _);

        var ownerId = Guid.NewGuid();
        Project? created = null;
        projectRepo
            .Setup(r => r.CreateAsync(It.IsAny<Project>()))
            .Callback<Project>(p => created = p)
            .Returns(Task.CompletedTask);

        var response = await sut.CreateProjectAsync(new CreateProjectRequest
        {
            OwnerUserId = ownerId.ToString(),
            Name = "  My project  "
        });

        projectRepo.Verify(r => r.CreateAsync(It.IsAny<Project>()), Times.Once);
        Assert.NotNull(created);
        Assert.Equal(ownerId, created!.OwnerUserId);
        Assert.Equal("My project", created.Name);
        Assert.Equal("Draft", created.Status);

        Assert.Equal(created.Id.ToString(), response.Id);
        Assert.Equal(ownerId.ToString(), response.OwnerUserId);
        Assert.Equal("My project", response.Name);
        Assert.Equal("Draft", response.Status);
    }

    [Fact]
    public async Task GetProjectAsync_ProjectNotFound_ThrowsNotFound()
    {
        var sut = CreateSut(out var projectRepo, out _, out _, out _);

        var projectId = Guid.NewGuid();
        projectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync((Project?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => sut.GetProjectAsync(projectId.ToString()));
        Assert.Equal("Project not found", ex.Message);
    }

    [Fact]
    public async Task UpsertProjectAnswerAsync_NoExistingAnswer_CreatesNewAnswer()
    {
        var sut = CreateSut(out _, out var answerRepo, out _, out _);

        var projectId = Guid.NewGuid();
        answerRepo.Setup(r => r.GetByProjectAndStepAsync(projectId, "builder.finish")).ReturnsAsync((ProjectStepAnswer?)null);

        ProjectStepAnswer? upserted = null;
        answerRepo
            .Setup(r => r.UpsertAsync(It.IsAny<ProjectStepAnswer>()))
            .Callback<ProjectStepAnswer>(a => upserted = a)
            .Returns(Task.CompletedTask);

        var updatedById = Guid.NewGuid();
        var response = await sut.UpsertProjectAnswerAsync(projectId.ToString(), new UpsertStepAnswerRequest
        {
            StepCode = "  builder.finish  ",
            AnswerType = StepAnswerType.Option,
            SelectedOptionCode = "premium",
            Source = StepAnswerSource.User,
            UpdatedByUserId = updatedById.ToString()
        });

        answerRepo.Verify(r => r.UpsertAsync(It.IsAny<ProjectStepAnswer>()), Times.Once);
        Assert.NotNull(upserted);
        Assert.Equal(projectId, upserted!.ProjectId);
        Assert.Equal("builder.finish", upserted.StepCode);
        Assert.Equal("premium", upserted.SelectedOptionCode);
        Assert.Equal(updatedById, upserted.UpdatedByUserId);
        Assert.NotEqual(default, upserted.UpdatedAt);

        Assert.Equal(upserted.Id.ToString(), response.Id);
        Assert.Equal(projectId.ToString(), response.ProjectId);
        Assert.Equal("builder.finish", response.StepCode);
        Assert.Equal("premium", response.SelectedOptionCode);
        Assert.Equal(updatedById.ToString(), response.UpdatedByUserId);
    }

    [Fact]
    public async Task UpsertProjectAnswerAsync_InvalidProjectId_ThrowsBadRequest()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.UpsertProjectAnswerAsync("not-a-guid", new UpsertStepAnswerRequest
        {
            StepCode = "builder.finish",
            AnswerType = StepAnswerType.Option
        }));

        Assert.Equal("Invalid projectId", ex.Message);
    }

    [Fact]
    public async Task CreateOrGetShareCodeAsync_WhenShareExists_DoesNotCreateNew()
    {
        var sut = CreateSut(out var projectRepo, out _, out var sharingRepo, out _);

        var projectId = Guid.NewGuid();
        projectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(new Project
        {
            Id = projectId,
            OwnerUserId = Guid.NewGuid(),
            Name = "P",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var existing = new SharedProject
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Code = "Q7M4-2K9P",
            IsActive = true,
            SharedAt = DateTime.UtcNow
        };

        sharingRepo.Setup(r => r.GetActiveShareByProjectIdAsync(projectId)).ReturnsAsync(existing);

        var response = await sut.CreateOrGetShareCodeAsync(projectId.ToString());

        sharingRepo.Verify(r => r.CreateShareAsync(It.IsAny<SharedProject>()), Times.Never);
        Assert.Equal(existing.Id.ToString(), response.ShareId);
        Assert.Equal(projectId.ToString(), response.ProjectId);
        Assert.Equal(existing.Code, response.Code);
        Assert.True(response.IsActive);
    }
}
