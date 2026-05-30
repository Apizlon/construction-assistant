using System.Globalization;
using System.Text.Json;
using ProjectCalculationService.Application.Contracts.Reports;
using ProjectCalculationService.Application.Exceptions;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Services.Excel;
using ProjectCalculationService.Application.Services.Reports;

namespace ProjectCalculationService.Application.Services;

public class ProjectReportService : IProjectReportService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectStepAnswerRepository _answerRepository;

    public ProjectReportService(IProjectRepository projectRepository, IProjectStepAnswerRepository answerRepository)
    {
        _projectRepository = projectRepository;
        _answerRepository = answerRepository;
    }

    public async Task<ProjectEstimateBreakdownResponse> GetEstimateAsync(string projectId)
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

        var answers = await _answerRepository.GetByProjectIdAsync(id);
        var byCode = answers.ToDictionary(a => a.StepCode, a => a, StringComparer.Ordinal);

        var buildingType = ParseBuildingType(byCode.TryGetValue("passport.building_type", out var bt) ? bt.SelectedOptionCode : null);
        var areaM2 = ParseNumberValue(byCode.TryGetValue("passport.area_m2", out var area) ? area.ValueJson : null);

        var selected = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["builder.finish"] = byCode.TryGetValue("builder.finish", out var v1) ? v1.SelectedOptionCode : null,
            ["builder.floor_base"] = byCode.TryGetValue("builder.floor_base", out var v2) ? v2.SelectedOptionCode : null,
            ["builder.floor_covering_main"] = byCode.TryGetValue("builder.floor_covering_main", out var v3) ? v3.SelectedOptionCode : null,
            ["builder.windows"] = byCode.TryGetValue("builder.windows", out var v4) ? v4.SelectedOptionCode : null,
            ["builder.electricity"] = byCode.TryGetValue("builder.electricity", out var v5) ? v5.SelectedOptionCode : null,
            ["builder.furniture"] = byCode.TryGetValue("builder.furniture", out var v6) ? v6.SelectedOptionCode : null,
            ["builder.state_now"] = byCode.TryGetValue("builder.state_now", out var v7) ? v7.SelectedOptionCode : null
        };

        var heatingValues = ParseMultiValues(byCode.TryGetValue("builder.heating", out var heating) ? heating.ValueJson : null);
        var tilesAreas = ParseMultiValues(byCode.TryGetValue("builder.tiles_areas", out var tiles) ? tiles.ValueJson : null);

        var areaValue = areaM2 is > 0 ? areaM2.Value : 0;
        var basePerM2 = buildingType switch
        {
            "apartment" => 35000,
            "warehouse" => 30000,
            "office" => 55000,
            "house" => 60000,
            _ => 60000
        };

        var items = new List<EstimateBreakdownItemResponse>();
        var factor = 1.0m;

        AddIf(selected["builder.windows"] == "panoramic", "windows.panoramic", "Панорамные окна", 0.08m);
        AddIf(selected["builder.windows"] == "warm", "windows.warm", "Тёплые окна", 0.05m);
        AddIf(selected["builder.electricity"] == "smart", "electricity.smart", "Умный дом / расширенная электрика", 0.10m);
        AddIf(selected["builder.electricity"] == "pass_through_switches", "electricity.pass_through_switches", "Проходные выключатели", 0.04m);
        AddIf(selected["builder.finish"] == "premium", "finish.premium", "Премиальная отделка", 0.20m);
        AddIf(selected["builder.finish"] == "rough", "finish.rough", "Черновая отделка (упрощение)", -0.08m);
        AddIf(selected["builder.furniture"] == "partial", "furniture.partial", "Частичная меблировка", 0.10m);
        AddIf(selected["builder.furniture"] == "turnkey", "furniture.turnkey", "Меблировка под ключ", 0.25m);

        AddIf(buildingType == "apartment" && selected["builder.floor_covering_main"] == "parquet", "floor_covering_main.parquet", "Паркет", 0.08m);
        AddIf(buildingType == "apartment" && selected["builder.floor_covering_main"] == "lvt", "floor_covering_main.lvt", "LVT", 0.06m);
        AddIf(buildingType == "apartment" && selected["builder.floor_base"] == "wet_screed", "floor_base.wet_screed", "Мокрая стяжка", 0.03m);

        AddIf(heatingValues.Contains("floor"), "heating.floor", "Тёплый пол", 0.08m);
        AddIf(heatingValues.Contains("heat_pump"), "heating.heat_pump", "Тепловой насос", 0.12m);
        AddIf(heatingValues.Contains("gas"), "heating.gas", "Газовое отопление", 0.04m);
        AddIf(heatingValues.Contains("electric"), "heating.electric", "Электро-отопление", 0.03m);

        if (buildingType == "apartment")
        {
            var tilesFactor = Math.Min(0.08m, tilesAreas.Count * 0.03m);
            AddIf(tilesFactor != 0, "tiles_areas", "Плитка: зоны", tilesFactor);
        }

        var baseCost = (int)Math.Round(areaValue * basePerM2, MidpointRounding.AwayFromZero);
        foreach (var item in items)
        {
            item.DeltaRub = (int)Math.Round(baseCost * item.PercentDelta, MidpointRounding.AwayFromZero);
        }

        var totalRub = (int)Math.Round(areaValue * basePerM2 * factor, MidpointRounding.AwayFromZero);

        var baseMonths = Math.Max(3, (int)Math.Round(areaValue / 25m, MidpointRounding.AwayFromZero));
        var months = baseMonths;
        if (selected["builder.finish"] == "premium") months += 2;
        if (selected["builder.electricity"] == "smart") months += 1;
        if (selected["builder.furniture"] == "turnkey") months += 1;
        if (heatingValues.Contains("floor")) months += 1;
        if (buildingType == "warehouse") months = Math.Max(1, (int)Math.Round(areaValue / 200m, MidpointRounding.AwayFromZero) + 1);
        if (buildingType == "apartment") months = Math.Max(3, (int)Math.Round(areaValue / 25m, MidpointRounding.AwayFromZero) + 2);

        return new ProjectEstimateBreakdownResponse
        {
            ProjectId = project.Id.ToString(),
            GeneratedAt = DateTime.UtcNow,
            BuildingType = buildingType,
            AreaM2 = areaM2,
            BasePerM2Rub = basePerM2,
            BaseCostRub = baseCost,
            Factor = factor,
            Items = items,
            TotalRub = totalRub,
            Months = months
        };

        void AddIf(bool condition, string code, string title, decimal percentDelta)
        {
            if (!condition) return;
            factor += percentDelta;
            items.Add(new EstimateBreakdownItemResponse
            {
                Code = code,
                Title = title,
                PercentDelta = percentDelta,
                DeltaRub = 0
            });
        }
    }

    public async Task<ProjectGanttResponse> GetGanttAsync(string projectId)
    {
        var estimate = await GetEstimateAsync(projectId);

        var totalMonths = Math.Max(1, estimate.Months);
        var totalDays = totalMonths * 30; // simple approximation for planning (relative timeline)

        // Heuristics based on selected "state now" and furniture option.
        // If current state is "finished" or "whitebox", demolition is likely.
        var answers = await _answerRepository.GetByProjectIdAsync(Guid.Parse(projectId));
        var byCode = answers.ToDictionary(a => a.StepCode, a => a, StringComparer.Ordinal);
        var stateNow = byCode.TryGetValue("builder.state_now", out var st) ? st.SelectedOptionCode : null;
        var furniture = byCode.TryGetValue("builder.furniture", out var f) ? f.SelectedOptionCode : null;

        var includeDemolition = stateNow is "finished" or "whitebox";
        var includeFurniture = furniture is "partial" or "turnkey";

        var tasks = new List<GanttTaskResponse>();

        // Phase weights sum to 1.0; later we normalize and convert to days.
        var phases = new List<(string Id, string Title, decimal Weight, string[] DependsOn)>
        {
            ("design", "Проектирование и подготовка", 0.12m, Array.Empty<string>()),
        };

        if (includeDemolition)
        {
            phases.Add(("demolition", "Демонтаж", 0.10m, new[] { "design" }));
        }

        phases.Add(("engineering", "Инженерия (электрика/отопление)", 0.18m, new[] { includeDemolition ? "demolition" : "design" }));
        phases.Add(("rough", "Черновые работы", 0.22m, new[] { "engineering" }));
        phases.Add(("finishing", "Чистовая отделка", 0.28m, new[] { "rough" }));
        if (includeFurniture)
        {
            phases.Add(("furniture", "Меблировка и оснащение", 0.10m, new[] { "finishing" }));
        }

        var sumWeight = phases.Sum(p => p.Weight);
        var dayCursor = 0;
        foreach (var phase in phases)
        {
            var days = (int)Math.Max(1, Math.Round((double)(phase.Weight / sumWeight * totalDays), MidpointRounding.AwayFromZero));
            var start = dayCursor;
            var end = Math.Min(totalDays, dayCursor + days);
            tasks.Add(new GanttTaskResponse
            {
                Id = phase.Id,
                Title = phase.Title,
                StartDay = start,
                EndDay = end,
                DependsOnTaskIds = phase.DependsOn
            });
            dayCursor = end;
        }

        // Ensure last task ends at totalDays (cosmetic for UI/export).
        if (tasks.Count > 0 && tasks[^1].EndDay != totalDays)
        {
            tasks[^1] = new GanttTaskResponse
            {
                Id = tasks[^1].Id,
                Title = tasks[^1].Title,
                StartDay = tasks[^1].StartDay,
                EndDay = totalDays,
                DependsOnTaskIds = tasks[^1].DependsOnTaskIds
            };
        }

        return new ProjectGanttResponse
        {
            ProjectId = estimate.ProjectId,
            GeneratedAt = DateTime.UtcNow,
            TotalMonths = totalMonths,
            TotalDays = totalDays,
            Tasks = tasks
        };
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetEstimateFileAsync(string projectId, string format)
    {
        var estimate = await GetEstimateAsync(projectId);
        var safeFormat = (format ?? string.Empty).Trim().ToLowerInvariant();

        return safeFormat switch
        {
            "xlsx" => (BuildEstimateXlsx(estimate), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Смета_проект_{estimate.ProjectId}.xlsx"),
            _ => throw new BadRequestException("Unsupported format. Use format=xlsx")
        };
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetGanttFileAsync(string projectId, string format)
    {
        var gantt = await GetGanttAsync(projectId);
        var safeFormat = (format ?? string.Empty).Trim().ToLowerInvariant();

        return safeFormat switch
        {
            "xlsx" => (BuildGanttXlsx(gantt), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ДиаграммаГанта_проект_{gantt.ProjectId}.xlsx"),
            "html" => (GanttHtmlExporter.BuildHtml(gantt), "text/html; charset=utf-8", $"ДиаграммаГанта_проект_{gantt.ProjectId}.html"),
            _ => throw new BadRequestException("Unsupported format. Use format=xlsx|html")
        };
    }

    private static byte[] BuildEstimateXlsx(ProjectEstimateBreakdownResponse estimate)
    {
        var area = estimate.AreaM2 is > 0 ? estimate.AreaM2.Value : 0m;
        var totalTarget = Math.Max(0, estimate.TotalRub);

        var metaRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Смета по проекту (оценка)" },
            new object?[] { "Проект", estimate.ProjectId },
            new object?[] { "Сформировано (UTC)", estimate.GeneratedAt.ToString("u", CultureInfo.InvariantCulture) },
            new object?[] { "Тип объекта", ToRuBuildingType(estimate.BuildingType) },
            new object?[] { "Площадь, м²", area },
            new object?[] { "Базовая ставка, ₽/м²", estimate.BasePerM2Rub },
            new object?[] { "Коэффициент", estimate.Factor },
            new object?[] { "Оценка итого, ₽", totalTarget },
            new object?[] { "Оценка срок, мес.", estimate.Months }
        };

        var smetaRows = new List<IReadOnlyList<object?>>();
        // A..H: Раздел | Позиция | Ед.изм. | Кол-во | Цена | Сумма (строка) | Итого (агрегации) | Примечание
        // Totals are kept in a separate column to avoid double-counting when summing item rows.
        smetaRows.Add(new object?[] { "Раздел", "Позиция", "Ед.изм.", "Кол-во", "Цена, ₽", "Сумма, ₽", "Итого, ₽", "Примечание" });

        // Build a detailed structure by distributing the total across sections and items.
        var sections = BuildEstimateSections(area, estimate);
        var baseTotal = sections.Sum(s => s.Items.Sum(i => i.Qty * i.UnitPrice));
        if (baseTotal <= 0) baseTotal = 1;
        var scale = totalTarget / baseTotal;
        if (scale <= 0) scale = 1;
        // Guard against accidental over/under scaling due to rounding/empty rows:
        // keep smeta total within ±1% of target by applying a final correction factor.
        // (Will be applied later after we compute rows.)
        var correction = 1m;

        var itemRanges = new List<(int StartRow1Based, int EndRow1Based)>();

        // Pre-calc (with rounding) to ensure Excel totals match backend total.
        var roundedBase = new List<(EstimateSection Section, List<(EstimateLine Line, decimal RoundedUnitPrice)> Lines)>();
        foreach (var section in sections)
        {
            var list = new List<(EstimateLine Line, decimal RoundedUnitPrice)>();
            foreach (var item in section.Items)
            {
                var unitPrice = Math.Round(item.UnitPrice * scale, 2, MidpointRounding.AwayFromZero);
                list.Add((item, unitPrice));
            }
            roundedBase.Add((section, list));
        }

        var approxTotal = roundedBase.Sum(s => s.Lines.Sum(x => x.Line.Qty * x.RoundedUnitPrice));
        if (approxTotal > 0)
        {
            correction = totalTarget / approxTotal;
        }

        foreach (var sectionPack in roundedBase)
        {
            var section = sectionPack.Section;
            var lines = sectionPack.Lines;

            // Section header
            smetaRows.Add(new object?[] { section.Title, null, null, null, null, null, null, null });

            var itemRowsStart = smetaRows.Count + 1; // next row (1-based)

            foreach (var pair in lines)
            {
                var item = pair.Line;
                var qty = item.Qty;
                var unitPrice = Math.Round(pair.RoundedUnitPrice * correction, 2, MidpointRounding.AwayFromZero);
                var rowIndex1Based = smetaRows.Count + 1;
                smetaRows.Add(new object?[]
                {
                    null,
                    item.Title,
                    item.Unit,
                    qty,
                    unitPrice,
                    new SimpleXlsxWriter.Formula($"D{rowIndex1Based}*E{rowIndex1Based}"),
                    null,
                    item.Note
                });
            }

            var itemRowsEnd = smetaRows.Count;
            if (itemRowsEnd >= itemRowsStart)
            {
                itemRanges.Add((itemRowsStart, itemRowsEnd));
                smetaRows.Add(new object?[]
                {
                    null,
                    "Итого по разделу",
                    null,
                    null,
                    null,
                    null,
                    new SimpleXlsxWriter.Formula($"SUM(F{itemRowsStart}:F{itemRowsEnd})"),
                    null
                });
            }
            else
            {
                smetaRows.Add(new object?[] { null, "Итого по разделу", null, null, null, null, 0, null });
            }
        }

        // Grand total (sum only item rows to avoid double-counting section subtotals)
        if (itemRanges.Count > 0)
        {
            var sumParts = string.Join(",", itemRanges.Select(r => $"F{r.StartRow1Based}:F{r.EndRow1Based}"));
            smetaRows.Add(Array.Empty<object?>());
            smetaRows.Add(new object?[]
            {
                null,
                "ИТОГО",
                null,
                null,
                null,
                null,
                new SimpleXlsxWriter.Formula($"SUM({sumParts})"),
                null
            });
            smetaRows.Add(new object?[] { null, "Контроль: итог должен совпадать с расчётом (±1%)", null, null, null, null, totalTarget, null });
        }

        return SimpleXlsxWriter.CreateWorkbook(new[]
        {
            new SimpleXlsxWriter.Sheet("Параметры", metaRows),
            new SimpleXlsxWriter.Sheet("Смета", smetaRows)
        });
    }

    private static byte[] BuildGanttXlsx(ProjectGanttResponse gantt)
    {
        return GanttOpenXmlExporter.BuildGanttXlsx(gantt);
#if false
        var metaRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "План работ (для диаграммы Ганта)" },
            new object?[] { "Проект", gantt.ProjectId },
            new object?[] { "Сформировано (UTC)", gantt.GeneratedAt.ToString("u", CultureInfo.InvariantCulture) },
            new object?[] { "Оценка срок, мес.", gantt.TotalMonths },
            new object?[] { "Оценка срок, дней", gantt.TotalDays }
        };

        // Structured table for building a chart in Excel
        // Columns: WBS, Работа, Начало(день), Длительность(дней), Окончание(день), Предшественники
        var planRows = new List<IReadOnlyList<object?>>();
        planRows.Add(new object?[] { "WBS", "Работа", "Начало, день", "Длительность, дней", "Окончание, день", "Предшественники (ID через ;)" });

        var wbs = 1;
        foreach (var t in gantt.Tasks)
        {
            var duration = Math.Max(0, t.EndDay - t.StartDay);
            planRows.Add(new object?[]
            {
                $"{wbs}.0",
                t.Title,
                t.StartDay,
                duration,
                new SimpleXlsxWriter.Formula($"C{planRows.Count + 1}+D{planRows.Count + 1}"),
                string.Join(";", t.DependsOnTaskIds)
            });
            wbs++;
        }

        // A simple "diagram" sheet using formulas and REPT blocks.
        // Create a dedicated sheet for a real Excel chart object.
        var diagramRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Диаграмма Ганта" },
            new object?[] { "Примечание: если диаграмма не обновилась автоматически, откройте файл в Excel и нажмите Обновить." }
        };

        var n = gantt.Tasks.Count;
        SimpleXlsxWriter.Chart? chart = null;
        if (n > 0)
        {
            var categories = $"'{EscapeSheetNameForFormula("План")}'!$B$2:$B${n + 1}";
            var offsets = $"'{EscapeSheetNameForFormula("План")}'!$C$2:$C${n + 1}";
            var durations = $"'{EscapeSheetNameForFormula("План")}'!$D$2:$D${n + 1}";

            chart = new SimpleXlsxWriter.Chart(
                HostSheetName: "Диаграмма",
                Title: "Диаграмма Ганта",
                CategoriesRange: categories,
                Series: new[]
                {
                    new SimpleXlsxWriter.ChartSeries("Смещение (начало)", offsets, Hidden: true),
                    new SimpleXlsxWriter.ChartSeries("Длительность (дней)", durations, Hidden: false)
                });
        }

        return SimpleXlsxWriter.CreateWorkbook(new[]
        {
            new SimpleXlsxWriter.Sheet("Параметры", metaRows),
            new SimpleXlsxWriter.Sheet("План", planRows),
            new SimpleXlsxWriter.Sheet("Диаграмма", diagramRows)
        }, chart);
#endif
    }

    private static string EscapeSheetNameForFormula(string name)
    {
        // In formula contexts Excel uses single quotes around sheet name; inside, a single quote is escaped by doubling.
        return (name ?? string.Empty).Replace("'", "''");
    }

    private sealed record EstimateSection(string Title, IReadOnlyList<EstimateLine> Items);
    private sealed record EstimateLine(string Title, string Unit, decimal Qty, decimal UnitPrice, string? Note);

    private static IReadOnlyList<EstimateSection> BuildEstimateSections(decimal areaM2, ProjectEstimateBreakdownResponse estimate)
    {
        // Note: current DB does not store a real BOQ; we produce a detailed, human-friendly estimate model
        // by using area and the builder selections, then distribute the final total to lines.
        var a = areaM2 <= 0 ? 0m : areaM2;

        // Use the same idea of options that affect the factor, but output Russian notes.
        var sections = new List<EstimateSection>
        {
            new("Проектирование и подготовка", new List<EstimateLine>
            {
                new("Обмеры и исходные данные", "проект", 1, 15000, null),
                new("Планировочное решение", "проект", 1, 25000, null),
                new("Рабочая документация (планы/узлы)", "проект", 1, 35000, null),
                new("Организация работ и график", "проект", 1, 15000, null)
            }),
            new("Демонтаж и подготовка основания", new List<EstimateLine>
            {
                new("Демонтажные работы (при необходимости)", "м²", RoundQty(a * 0.6m), 1800, null),
                new("Вывоз и утилизация", "м²", RoundQty(a * 0.6m), 650, null),
                new("Подготовка оснований", "м²", RoundQty(a), 1200, null)
            }),
            new("Инженерные системы", new List<EstimateLine>
            {
                new("Электромонтажные работы", "м²", RoundQty(a), 3200, null),
                new("Отопление / тёплый пол (по сценарию)", "м²", RoundQty(a * 0.5m), 2800, null),
                new("Слаботочные системы (интернет/тв)", "м²", RoundQty(a), 900, null)
            }),
            new("Черновые работы", new List<EstimateLine>
            {
                new("Выравнивание стен/потолков", "м²", RoundQty(a * 2.6m), 650, null),
                new("Стяжка / подготовка пола", "м²", RoundQty(a), 1900, null),
                new("Грунтовка, гидроизоляция зон", "м²", RoundQty(a * 0.4m), 850, null)
            }),
            new("Чистовая отделка", new List<EstimateLine>
            {
                new("Шпаклёвка и покраска", "м²", RoundQty(a * 2.6m), 950, null),
                new("Напольные покрытия", "м²", RoundQty(a), 2400, null),
                new("Плиточные работы (зоны)", "м²", RoundQty(a * 0.25m), 4200, null),
                new("Двери и доборы", "шт", Math.Max(1, (int)Math.Round((double)(a / 20m), MidpointRounding.AwayFromZero)), 22000, null)
            }),
            new("Окна и фасадные элементы", new List<EstimateLine>
            {
                new("Окна (комплект/настройки)", "компл.", 1, 45000, null),
                new("Откосы и подоконники", "м.п.", RoundQty(a * 0.12m), 2400, null)
            }),
            new("Меблировка и оснащение", new List<EstimateLine>
            {
                new("Сборка/монтаж мебели (если выбрано)", "м²", RoundQty(a * 0.4m), 2500, null),
                new("Оснащение (сантехника/свет)", "м²", RoundQty(a), 2800, null)
            })
        };

        // Filter sections that don't make sense for zero area.
        if (a <= 0)
        {
            return new[]
            {
                new EstimateSection("Проектирование и подготовка", sections[0].Items)
            };
        }

        return sections;

        static decimal RoundQty(decimal qty) => Math.Round(Math.Max(0, qty), 2, MidpointRounding.AwayFromZero);
    }

    private static string ToRuBuildingType(string? buildingType) => buildingType switch
    {
        "apartment" => "Квартира",
        "house" => "Дом",
        "office" => "Офис",
        "warehouse" => "Склад",
        _ => "Не указан"
    };

    private static string? ParseBuildingType(string? raw)
    {
        raw = raw?.Trim();
        return raw is "house" or "apartment" or "office" or "warehouse" ? raw : null;
    }

    private static decimal? ParseNumberValue(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            if (!doc.RootElement.TryGetProperty("value", out var valueEl)) return null;
            return valueEl.ValueKind switch
            {
                JsonValueKind.Number when valueEl.TryGetDecimal(out var d) => d,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<string> ParseMultiValues(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) return [];
        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            if (!doc.RootElement.TryGetProperty("values", out var valuesEl) || valuesEl.ValueKind != JsonValueKind.Array) return [];
            var list = new List<string>();
            foreach (var el in valuesEl.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
                }
            }

            return list;
        }
        catch
        {
            return [];
        }
    }

    // CSV helpers removed: export is XLSX.
}
