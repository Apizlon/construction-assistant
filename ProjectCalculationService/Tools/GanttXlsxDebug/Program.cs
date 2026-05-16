// See https://aka.ms/new-console-template for more information
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ProjectCalculationService.Application.Contracts.Reports;
using ProjectCalculationService.Application.Services.Excel;

var gantt = new ProjectGanttResponse
{
    ProjectId = Guid.NewGuid().ToString(),
    GeneratedAt = DateTime.UtcNow,
    TotalMonths = 6,
    TotalDays = 180,
    Tasks = new[]
    {
        new GanttTaskResponse { Id = "design", Title = "Проектирование и подготовка", StartDay = 0, EndDay = 25, DependsOnTaskIds = Array.Empty<string>() },
        new GanttTaskResponse { Id = "engineering", Title = "Инженерия (электрика/отопление)", StartDay = 25, EndDay = 60, DependsOnTaskIds = new[] { "design" } },
        new GanttTaskResponse { Id = "rough", Title = "Черновые работы", StartDay = 60, EndDay = 95, DependsOnTaskIds = new[] { "engineering" } },
        new GanttTaskResponse { Id = "finishing", Title = "Чистовая отделка", StartDay = 95, EndDay = 170, DependsOnTaskIds = new[] { "rough" } }
    }
};

var bytes = GanttOpenXmlExporter.BuildGanttXlsx(gantt);
var outPath = Path.Combine(Environment.CurrentDirectory, "gantt_debug.xlsx");
await File.WriteAllBytesAsync(outPath, bytes);
Console.WriteLine($"Wrote: {outPath} ({bytes.Length} bytes)");

using var ms = new MemoryStream(bytes);
using var doc = SpreadsheetDocument.Open(ms, false);
var validator = new OpenXmlValidator();
var errors = validator.Validate(doc).ToList();
Console.WriteLine($"Validation errors: {errors.Count}");
foreach (var e in errors.Take(50))
{
    Console.WriteLine($"{e.Description} | {e.Path?.XPath}");
}

var wb = doc.WorkbookPart!;
var ws3 = wb.WorksheetParts.Last();
var drawings = ws3.DrawingsPart;
if (drawings != null)
{
    Console.WriteLine($"Drawing URI: {drawings.Uri}");
    var chartParts = drawings.ChartParts.ToList();
    Console.WriteLine($"ChartParts: {chartParts.Count}");
    foreach (var cp in chartParts)
    {
        Console.WriteLine($"Chart URI: {cp.Uri}");
    }
}
