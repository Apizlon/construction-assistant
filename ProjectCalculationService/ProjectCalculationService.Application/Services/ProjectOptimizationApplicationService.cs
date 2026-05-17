using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectCalculationService.Application.Contracts.Optimizations;
using ProjectCalculationService.Application.Exceptions;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Models.Optimizations;
using ProjectCalculationService.Application.Services.Optimizations;

namespace ProjectCalculationService.Application.Services;

public class ProjectOptimizationApplicationService : IProjectOptimizationApplicationService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISharingRepository _sharingRepository;
    private readonly IProjectStepAnswerRepository _answerRepository;
    private readonly IProjectOptimizationRepository _optimizationRepository;
    private readonly ILogger<ProjectOptimizationApplicationService> _logger;

    public ProjectOptimizationApplicationService(
        IProjectRepository projectRepository,
        ISharingRepository sharingRepository,
        IProjectStepAnswerRepository answerRepository,
        IProjectOptimizationRepository optimizationRepository,
        ILogger<ProjectOptimizationApplicationService> logger)
    {
        _projectRepository = projectRepository;
        _sharingRepository = sharingRepository;
        _answerRepository = answerRepository;
        _optimizationRepository = optimizationRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OptimizationTemplateResponse>> GetTemplatesAsync(string projectId, string actorUserId)
    {
        var (project, actorId, _) = await EnsureCanReadAsync(projectId, actorUserId);
        var category = await GetCategoryForProjectAsync(project.Id);
        var templates = category == null
            ? OptimizationTemplates.All
            : OptimizationTemplates.All.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        _logger.LogInformation("Templates requested - Project {ProjectId} Actor {ActorUserId}", project.Id, actorId);
        return templates;
    }

    public async Task<ProjectOptimizationPreviewResponse> PreviewAsync(string projectId, CreateOptimizationPreviewRequest request)
    {
        var (_, actorId, _) = await EnsureCanReadAsync(projectId, request.ActorUserId);
        var template = GetTemplateOrThrow(request.TemplateId);
        var commType = ParseCommunicationType(request.CommunicationType);

        ValidatePoint(template, request.Start, "Start");
        ValidatePoint(template, request.End, "End");

        var wall = GridPathfinding.FindWallFriendly(template, request.Start, request.End);
        var direct = GridPathfinding.FindDirect(template, request.Start, request.End);

        _logger.LogInformation(
            "Optimization preview - Project {ProjectId} Actor {ActorUserId} Template {TemplateId} Type {CommType} FoundWall {FoundWall} FoundDirect {FoundDirect}",
            projectId, actorId, template.Id, commType, wall.IsFound, direct.IsFound);

        return new ProjectOptimizationPreviewResponse
        {
            ProjectId = projectId,
            TemplateId = template.Id,
            CommunicationType = commType.ToString(),
            Start = request.Start,
            End = request.End,
            Variants =
            [
                MapVariant("WallFriendly", wall),
                MapVariant("Direct", direct)
            ]
        };
    }

    public async Task<ProjectOptimizationResponse> CreateAsync(string projectId, CreateOptimizationRequest request)
    {
        var (project, actorId, isOwner) = await EnsureCanReadAsync(projectId, request.ActorUserId);
        if (!isOwner)
        {
            throw new ForbiddenException("Наблюдатель не может создавать оптимизации.");
        }

        var template = GetTemplateOrThrow(request.TemplateId);
        var commType = ParseCommunicationType(request.CommunicationType);
        var selectedVariant = ParseVariant(request.SelectedVariant);

        ValidatePoint(template, request.Start, "Start");
        ValidatePoint(template, request.End, "End");

        var preview = await PreviewAsync(projectId, request);
        var chosen = preview.Variants.FirstOrDefault(v => string.Equals(v.VariantType, selectedVariant.ToString(), StringComparison.OrdinalIgnoreCase));
        if (chosen == null)
        {
            throw new BadRequestException("Invalid SelectedVariant");
        }

        if (!chosen.IsFound)
        {
            throw new BadRequestException("SelectedVariant путь не найден — сохранить нельзя.");
        }

        var optimization = new ProjectOptimization
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow,
            CommunicationType = commType,
            TemplateId = template.Id,
            StartX = request.Start.X,
            StartY = request.Start.Y,
            EndX = request.End.X,
            EndY = request.End.Y,
            SelectedVariant = selectedVariant,
            ResultJson = GridPathfinding.ToResultJson(preview)
        };

        await _optimizationRepository.CreateAsync(optimization);

        return MapOptimization(optimization, preview);
    }

    public async Task<IReadOnlyList<ProjectOptimizationListItemResponse>> GetOptimizationsAsync(string projectId, string actorUserId)
    {
        var (project, _, _) = await EnsureCanReadAsync(projectId, actorUserId);
        var list = await _optimizationRepository.GetByProjectIdAsync(project.Id);

        return list.Select(MapOptimizationListItem).ToList();
    }

    public async Task<ProjectOptimizationResponse> GetOptimizationAsync(string projectId, string optimizationId, string actorUserId)
    {
        var (project, _, _) = await EnsureCanReadAsync(projectId, actorUserId);
        if (!Guid.TryParse(optimizationId, out var id))
        {
            throw new BadRequestException("Invalid optimizationId");
        }

        var opt = await _optimizationRepository.GetByIdAsync(id);
        if (opt == null || opt.ProjectId != project.Id)
        {
            throw new NotFoundException("Optimization not found");
        }

        var result = DeserializeResult(opt.ResultJson);
        return MapOptimization(opt, result);
    }

    public async Task DeleteAsync(string projectId, string optimizationId, string actorUserId)
    {
        var (project, actorId, isOwner) = await EnsureCanReadAsync(projectId, actorUserId);
        if (!isOwner)
        {
            throw new ForbiddenException("Наблюдатель не может удалять оптимизации.");
        }

        if (!Guid.TryParse(optimizationId, out var id))
        {
            throw new BadRequestException("Invalid optimizationId");
        }

        var opt = await _optimizationRepository.GetByIdAsync(id);
        if (opt == null || opt.ProjectId != project.Id)
        {
            throw new NotFoundException("Optimization not found");
        }

        await _optimizationRepository.DeleteAsync(opt);
        _logger.LogInformation("Optimization deleted - {OptimizationId} by {ActorUserId}", opt.Id, actorId);
    }

    private async Task<(Application.Models.Project project, Guid actorUserId, bool isOwner)> EnsureCanReadAsync(string projectId, string actorUserId)
    {
        if (!Guid.TryParse(projectId, out var pid))
        {
            throw new BadRequestException("Invalid projectId");
        }

        if (!Guid.TryParse(actorUserId, out var actorId))
        {
            throw new BadRequestException("Invalid actorUserId");
        }

        var project = await _projectRepository.GetByIdAsync(pid);
        if (project == null)
        {
            throw new NotFoundException("Project not found");
        }

        if (project.OwnerUserId == actorId)
        {
            return (project, actorId, true);
        }

        var isViewer = await _sharingRepository.IsViewerOfProjectAsync(actorId, pid);
        if (!isViewer)
        {
            throw new ForbiddenException("Нет доступа к проекту.");
        }

        return (project, actorId, false);
    }

    private static OptimizationTemplateResponse GetTemplateOrThrow(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new BadRequestException("TemplateId is required");
        }

        var template = OptimizationTemplates.Get(templateId.Trim());
        if (template == null)
        {
            throw new NotFoundException("Template not found");
        }

        return template;
    }

    private async Task<string?> GetCategoryForProjectAsync(Guid projectId)
    {
        var answers = await _answerRepository.GetByProjectIdAsync(projectId);
        var raw = answers.FirstOrDefault(a => a.StepCode == "passport.building_type")?.SelectedOptionCode;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        return raw.Trim().ToLowerInvariant() switch
        {
            "apartment" => "Apartment",
            "house" => "House",
            "office" => "Office",
            "warehouse" => "Warehouse",
            _ => null
        };
    }

    private static CommunicationType ParseCommunicationType(string raw)
    {
        if (!Enum.TryParse<CommunicationType>(raw, ignoreCase: true, out var ct))
        {
            throw new BadRequestException("Invalid CommunicationType");
        }

        return ct;
    }

    private static OptimizationVariantType ParseVariant(string raw)
    {
        if (!Enum.TryParse<OptimizationVariantType>(raw, ignoreCase: true, out var v))
        {
            throw new BadRequestException("Invalid SelectedVariant");
        }

        return v;
    }

    private static void ValidatePoint(OptimizationTemplateResponse template, GridPointDto p, string name)
    {
        if (p.X < 0 || p.Y < 0 || p.X >= template.Width || p.Y >= template.Height)
        {
            throw new BadRequestException($"{name} point out of bounds");
        }
    }

    private static OptimizationVariantResponse MapVariant(string type, GridPathfinding.VariantResult r)
    {
        var (pros, cons) = type.Equals("WallFriendly", StringComparison.OrdinalIgnoreCase)
            ? (new[]
                {
                    "Прокладка вдоль стен и перегородок: проще соблюдать нормы и избегать конструктивных рисков.",
                    "Проще доступ/ремонт: логика трассы предсказуема, меньше скрытых участков.",
                    "Меньше риск случайных пересечений с запретными зонами при строительстве."
                },
                new[]
                {
                    "Длина трассы обычно больше.",
                    "Больше поворотов: больше соединений/штробления по углам."
                })
            : (new[]
                {
                    "Минимальная длина по геометрии: дешевле по материалам.",
                    "Монтаж обычно выполняется быстрее за счёт меньшей общей длины трассы."
                },
                new[]
                {
                    "Чаще проходит через центр помещений: сложнее ремонт/доступ, выше риск повредить при отделке.",
                    "Часто больше поворотов и пересечений зон: сложнее монтаж и дальнейшее обслуживание.",
                    "Не всегда удобно по нормам/технологии — требуется инженерная проверка."
                });

        return new OptimizationVariantResponse
        {
            VariantType = type,
            IsFound = r.IsFound,
            Message = r.Message,
            Path = r.Path,
            LengthUnits = r.LengthUnits,
            Turns = r.Turns,
            WeightedCost = r.WeightedCost,
            Pros = pros,
            Cons = cons
        };
    }

    private static ProjectOptimizationListItemResponse MapOptimizationListItem(ProjectOptimization o)
    {
        var selectedLength = TryGetSelectedLength(o.ResultJson, o.SelectedVariant?.ToString());
        return new ProjectOptimizationListItemResponse
        {
            Id = o.Id.ToString(),
            ProjectId = o.ProjectId.ToString(),
            CreatedByUserId = o.CreatedByUserId.ToString(),
            CreatedAt = o.CreatedAt,
            TemplateId = o.TemplateId,
            CommunicationType = o.CommunicationType.ToString(),
            Start = new GridPointDto(o.StartX, o.StartY),
            End = new GridPointDto(o.EndX, o.EndY),
            SelectedVariant = o.SelectedVariant?.ToString(),
            SelectedLengthUnits = selectedLength
        };
    }

    private static ProjectOptimizationResponse MapOptimization(ProjectOptimization o, ProjectOptimizationPreviewResponse result)
    {
        var listItem = MapOptimizationListItem(o);
        return new ProjectOptimizationResponse
        {
            Id = listItem.Id,
            ProjectId = listItem.ProjectId,
            CreatedByUserId = listItem.CreatedByUserId,
            CreatedAt = listItem.CreatedAt,
            TemplateId = listItem.TemplateId,
            CommunicationType = listItem.CommunicationType,
            Start = listItem.Start,
            End = listItem.End,
            SelectedVariant = listItem.SelectedVariant,
            SelectedLengthUnits = listItem.SelectedLengthUnits,
            Result = result
        };
    }

    private static ProjectOptimizationPreviewResponse DeserializeResult(string json)
    {
        try
        {
            var v = JsonSerializer.Deserialize<ProjectOptimizationPreviewResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return v ?? new ProjectOptimizationPreviewResponse();
        }
        catch
        {
            return new ProjectOptimizationPreviewResponse();
        }
    }

    private static double? TryGetSelectedLength(string json, string? selectedVariant)
    {
        if (string.IsNullOrWhiteSpace(selectedVariant)) return null;
        try
        {
            var result = DeserializeResult(json);
            return result.Variants.FirstOrDefault(v => v.VariantType.Equals(selectedVariant, StringComparison.OrdinalIgnoreCase))?.LengthUnits;
        }
        catch
        {
            return null;
        }
    }
}
