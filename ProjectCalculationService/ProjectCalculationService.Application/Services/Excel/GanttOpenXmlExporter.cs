using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using ProjectCalculationService.Application.Contracts.Reports;

namespace ProjectCalculationService.Application.Services.Excel;

public static class GanttOpenXmlExporter
{
    private const string RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    // Excel Gantt = stacked bar:
    // - Series0 (offset/start) is transparent
    // - Series1 (duration) is visible
    public static byte[] BuildGanttXlsx(ProjectGanttResponse gantt)
    {
        var rows = ExpandWbs(gantt.Tasks);
        var totalDays = Math.Max(0, gantt.TotalDays);

        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, autoSave: true))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = BuildStylesheet();
            stylesPart.Stylesheet.Save();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var parametersPart = workbookPart.AddNewPart<WorksheetPart>();
            parametersPart.Worksheet = new Worksheet(new SheetData());
            parametersPart.Worksheet.Save();

            var planPart = workbookPart.AddNewPart<WorksheetPart>();
            planPart.Worksheet = new Worksheet(new SheetData());
            planPart.Worksheet.Save();

            var diagramPart = workbookPart.AddNewPart<WorksheetPart>();
            diagramPart.Worksheet = new Worksheet(new SheetData());
            diagramPart.Worksheet.Save();

            sheets.Append(
                new Sheet { Id = workbookPart.GetIdOfPart(parametersPart), SheetId = 1u, Name = "Параметры" },
                new Sheet { Id = workbookPart.GetIdOfPart(planPart), SheetId = 2u, Name = "План" },
                new Sheet { Id = workbookPart.GetIdOfPart(diagramPart), SheetId = 3u, Name = "Диаграмма" }
            );

            WriteParameters(parametersPart, gantt);
            WritePlan(planPart, rows);
            WriteDiagramHeader(diagramPart);

            if (rows.Count > 0)
            {
                AddChart(diagramPart, dataRowCount: rows.Count, totalDays: totalDays);
            }

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    private static void WriteParameters(WorksheetPart part, ProjectGanttResponse gantt)
    {
        var sheetData = part.Worksheet.GetFirstChild<SheetData>()!;
        sheetData.RemoveAllChildren();

        AppendRow(sheetData, CellFactory.Text("План работ (для диаграммы Ганта)"));
        AppendRow(sheetData, CellFactory.Text("Проект"), CellFactory.Text(gantt.ProjectId));
        AppendRow(sheetData, CellFactory.Text("Сформировано (UTC)"), CellFactory.Text(gantt.GeneratedAt.ToString("u", CultureInfo.InvariantCulture)));
        AppendRow(sheetData, CellFactory.Text("Оценка срок, мес."), CellFactory.Number(gantt.TotalMonths));
        AppendRow(sheetData, CellFactory.Text("Оценка срок, дней"), CellFactory.Number(gantt.TotalDays));

        SetBestFitColumnWidths(part, sheetData, maxColWidths: new[] { 40d, 60d });
    }

    private static void WritePlan(WorksheetPart part, IReadOnlyList<WbsRow> rows)
    {
        var sheetData = part.Worksheet.GetFirstChild<SheetData>()!;
        sheetData.RemoveAllChildren();

        AppendRow(
            sheetData,
            CellFactory.Text("WBS"),
            CellFactory.Text("Этап"),
            CellFactory.Text("Начало, день"),
            CellFactory.Text("Длительность, дней"),
            CellFactory.Text("Окончание, день"),
            CellFactory.Text("Предшественники (WBS через ;)")
        );

        foreach (var r in rows)
        {
            AppendRow(
                sheetData,
                CellFactory.Text(r.Wbs),
                CellFactory.Text($"{r.Wbs} {r.Title}"),
                CellFactory.Number(r.StartDay),
                CellFactory.Number(r.DurationDays),
                CellFactory.Number(r.StartDay + r.DurationDays),
                CellFactory.Text(r.DependsOnWbs)
            );
        }

        SetBestFitColumnWidths(part, sheetData, maxColWidths: new[] { 10d, 70d, 14d, 20d, 18d, 30d });
    }

    private static void WriteDiagramHeader(WorksheetPart part)
    {
        var sheetData = part.Worksheet.GetFirstChild<SheetData>()!;
        sheetData.RemoveAllChildren();

        AppendRow(sheetData, CellFactory.Text("Диаграмма Ганта"));
        AppendRow(sheetData, CellFactory.Text("Ось X — дни. Длина отрезка = длительность, позиция = старт."));

        SetBestFitColumnWidths(part, sheetData, maxColWidths: new[] { 90d });
    }

    private static void AddChart(WorksheetPart diagramSheetPart, int dataRowCount, int totalDays)
    {
        var drawingsPart = diagramSheetPart.AddNewPart<DrawingsPart>();
        var chartPart = drawingsPart.AddNewPart<ChartPart>();

        chartPart.ChartSpace = BuildGanttChartSpace(dataRowCount, totalDays);
        chartPart.ChartSpace.Save();

        var chartRelId = drawingsPart.GetIdOfPart(chartPart);
        drawingsPart.WorksheetDrawing = BuildWorksheetDrawing(chartRelId);
        drawingsPart.WorksheetDrawing.Save();

        var ws = diagramSheetPart.Worksheet;
        ws.AddNamespaceDeclaration("r", RelationshipsNs);
        ws.RemoveAllChildren<Drawing>();
        ws.Append(new Drawing { Id = diagramSheetPart.GetIdOfPart(drawingsPart) });
        ws.Save();
    }

    private static C.ChartSpace BuildGanttChartSpace(int dataRowCount, int totalDays)
    {
        var lastRow = dataRowCount + 1; // header is row 1
        var categories = $"План!$B$2:$B${lastRow}";
        var offsets = $"План!$C$2:$C${lastRow}";
        var durations = $"План!$D$2:$D${lastRow}";

        var chartSpace = new C.ChartSpace();
        chartSpace.Append(new C.EditingLanguage { Val = "ru-RU" });

        var chart = new C.Chart();
        chart.Append(new C.Title(
            new C.ChartText(
                new C.RichText(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text("Диаграмма Ганта")))
                )
            )
        ));

        var plotArea = new C.PlotArea(new C.Layout());

        var barChart = new C.BarChart(
            new C.BarDirection { Val = C.BarDirectionValues.Bar },
            new C.BarGrouping { Val = C.BarGroupingValues.Stacked },
            new C.VaryColors { Val = false }
        );

        // Transparent "offset" series
        barChart.Append(BuildSeries(0u, "Смещение (начало)", categories, offsets, transparent: true));
        // Visible duration series
        barChart.Append(BuildSeries(1u, "Длительность (дней)", categories, durations, transparent: false));

        var valAxisId = 123456u;
        var catAxisId = 654321u;
        barChart.Append(new C.AxisId { Val = valAxisId });
        barChart.Append(new C.AxisId { Val = catAxisId });

        plotArea.Append(barChart);

        // Category axis: reverse so first row is top.
        var catAx = new C.CategoryAxis(
            new C.AxisId { Val = catAxisId },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MaxMin }),
            new C.AxisPosition { Val = C.AxisPositionValues.Left },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = valAxisId },
            new C.Crosses { Val = C.CrossesValues.AutoZero }
        );

        var scaling = new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax });
        // Element order inside <c:scaling> is important for Excel compatibility.
        if (totalDays > 0)
        {
            scaling.Append(new C.MaxAxisValue { Val = totalDays });
        }
        scaling.Append(new C.MinAxisValue { Val = 0d });

        var major = PickMajorUnit(totalDays);
        var minor = Math.Max(1d, major / 2d);

        // Value axis: ticks + gridlines so timeline is readable.
        var valAx = new C.ValueAxis(
            new C.AxisId { Val = valAxisId },
            scaling,
            new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
            new C.MajorGridlines(),
            new C.NumberingFormat { FormatCode = "0", SourceLinked = true },
            new C.MajorTickMark { Val = C.TickMarkValues.Outside },
            new C.MinorTickMark { Val = C.TickMarkValues.None },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = catAxisId },
            new C.Crosses { Val = C.CrossesValues.AutoZero },
            new C.MajorUnit { Val = major },
            new C.MinorUnit { Val = minor }
        );

        plotArea.Append(catAx);
        plotArea.Append(valAx);

        chart.Append(plotArea);
        chart.Append(new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Right }, new C.Overlay { Val = false }));
        chart.Append(new C.PlotVisibleOnly { Val = true });

        chartSpace.Append(chart);
        return chartSpace;
    }

    private static C.BarChartSeries BuildSeries(uint index, string name, string categoriesRange, string valuesRange, bool transparent)
    {
        var ser = new C.BarChartSeries(
            new C.Index { Val = index },
            new C.Order { Val = index },
            new C.SeriesText(new C.NumericValue(name))
        );

        if (transparent)
        {
            ser.Append(new C.ChartShapeProperties(new A.NoFill(), new A.Outline(new A.NoFill())));
        }

        ser.Append(new C.CategoryAxisData(new C.StringReference(new C.Formula { Text = categoriesRange })));
        ser.Append(new C.Values(new C.NumberReference(new C.Formula { Text = valuesRange })));
        return ser;
    }

    private static double PickMajorUnit(int totalDays)
    {
        if (totalDays <= 0) return 10;
        if (totalDays <= 30) return 5;
        if (totalDays <= 90) return 10;
        if (totalDays <= 180) return 15;
        if (totalDays <= 365) return 30;
        return 60;
    }

    private static Xdr.WorksheetDrawing BuildWorksheetDrawing(string chartRelId)
    {
        // One anchored chart.
        return new Xdr.WorksheetDrawing(
            new Xdr.OneCellAnchor(
                new Xdr.FromMarker(
                    new Xdr.ColumnId("0"),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId("2"),
                    new Xdr.RowOffset("0")
                ),
                new Xdr.Extent { Cx = 9900000L, Cy = 5200000L },
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        new Xdr.NonVisualDrawingProperties { Id = 2u, Name = "Диаграмма 1" },
                        new Xdr.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoGrouping = true })
                    ),
                    new Xdr.Transform(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = 9900000L, Cy = 5200000L }
                    ),
                    new A.Graphic(
                        new A.GraphicData(new C.ChartReference { Id = chartRelId })
                        {
                            Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
                        }
                    )
                ),
                new Xdr.ClientData()
            )
        );
    }

    private static Stylesheet BuildStylesheet()
    {
        return new Stylesheet(
            new Fonts(new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" })) { Count = 1 },
            new Fills(new Fill(new PatternFill { PatternType = PatternValues.None })) { Count = 1 },
            new Borders(new Border(new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder())) { Count = 1 },
            new CellStyleFormats(new CellFormat()) { Count = 1 },
            new CellFormats(new CellFormat()) { Count = 1 }
        );
    }

    private static void SetBestFitColumnWidths(WorksheetPart part, SheetData sheetData, IReadOnlyList<double>? maxColWidths)
    {
        var rows = sheetData.Elements<Row>().ToList();
        if (rows.Count == 0) return;

        var maxCols = rows.Max(r => r.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().Count());
        if (maxCols == 0) return;

        var maxLen = new int[maxCols];
        foreach (var row in rows)
        {
            var cells = row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
            for (var i = 0; i < cells.Count; i++)
            {
                var len = EstimateCellTextLength(cells[i]);
                if (len > maxLen[i]) maxLen[i] = len;
            }
        }

        var widths = new double[maxCols];
        for (var i = 0; i < maxCols; i++)
        {
            var w = (double)Math.Clamp(maxLen[i] + 2, 8, 60);
            if (maxColWidths != null && i < maxColWidths.Count) w = Math.Min(w, maxColWidths[i]);
            widths[i] = w;
        }

        var ws = part.Worksheet;
        ws.Elements<Columns>().FirstOrDefault()?.Remove();
        var cols = new Columns();
        for (var i = 0; i < widths.Length; i++)
        {
            cols.Append(new Column { Min = (uint)(i + 1), Max = (uint)(i + 1), Width = widths[i], CustomWidth = true });
        }

        ws.InsertAt(cols, 0);
        ws.Save();
    }

    private static int EstimateCellTextLength(DocumentFormat.OpenXml.Spreadsheet.Cell cell)
    {
        if (cell.DataType?.Value == CellValues.InlineString && cell.InlineString?.Text != null)
        {
            return (cell.InlineString.Text.Text ?? string.Empty).Length;
        }
        if (cell.DataType?.Value == CellValues.Number) return 10;
        return 0;
    }

    private static void AppendRow(SheetData sheetData, params DocumentFormat.OpenXml.Spreadsheet.Cell[] cells)
    {
        var row = new Row();
        foreach (var c in cells) row.Append(c);
        sheetData.Append(row);
    }

    private sealed record WbsRow(string Wbs, string Title, int StartDay, int DurationDays, string DependsOnWbs);

    private static IReadOnlyList<WbsRow> ExpandWbs(IReadOnlyList<GanttTaskResponse> tasks)
    {
        var idToWbs = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < tasks.Count; i++)
        {
            idToWbs[tasks[i].Id] = (i + 1).ToString(CultureInfo.InvariantCulture);
        }

        var rows = new List<WbsRow>();
        for (var i = 0; i < tasks.Count; i++)
        {
            var t = tasks[i];
            var duration = Math.Max(0, t.EndDay - t.StartDay);
            var wbs = (i + 1).ToString(CultureInfo.InvariantCulture);

            var depends = string.Join(
                ";",
                t.DependsOnTaskIds.Select(id => idToWbs.TryGetValue(id, out var dep) ? dep : null).Where(x => !string.IsNullOrWhiteSpace(x))!
            );

            rows.Add(new WbsRow(wbs, t.Title, t.StartDay, duration, depends));

            var details = BuildDetailsForPhase(t.Title, t.StartDay, duration);
            for (var j = 0; j < details.Count; j++)
            {
                var d = details[j];
                rows.Add(new WbsRow($"{wbs}.{j + 1}", d.Title, d.StartDay, d.DurationDays, wbs));
            }
        }

        return rows;
    }

    private sealed record Detail(string Title, int StartDay, int DurationDays);

    private static IReadOnlyList<Detail> BuildDetailsForPhase(string phaseTitle, int startDay, int durationDays)
    {
        if (durationDays <= 0) return Array.Empty<Detail>();

        var titles = phaseTitle switch
        {
            var s when s.Contains("Проектирование", StringComparison.OrdinalIgnoreCase) => new[] { "ТЗ и замеры", "Планировки и ведомости", "Согласования" },
            var s when s.Contains("Демонтаж", StringComparison.OrdinalIgnoreCase) => new[] { "Подготовка", "Демонтаж", "Вывоз и уборка" },
            var s when s.Contains("Инженер", StringComparison.OrdinalIgnoreCase) => new[] { "Электрика", "Сантехника/отопление", "Слаботочка" },
            var s when s.Contains("Чернов", StringComparison.OrdinalIgnoreCase) => new[] { "Стены/потолок", "Пол/стяжка", "Подготовка под чистовую" },
            var s when s.Contains("Чистов", StringComparison.OrdinalIgnoreCase) => new[] { "Покраска/обои", "Плитка/полы", "Двери/плинтусы" },
            var s when s.Contains("Мебл", StringComparison.OrdinalIgnoreCase) => new[] { "Поставка", "Сборка/монтаж", "Финальная приемка" },
            _ => new[] { "Подзадача 1", "Подзадача 2", "Подзадача 3" }
        };

        var d1 = Math.Max(1, (int)Math.Round(durationDays * 0.3, MidpointRounding.AwayFromZero));
        var d2 = Math.Max(1, (int)Math.Round(durationDays * 0.4, MidpointRounding.AwayFromZero));
        var d3 = Math.Max(1, durationDays - d1 - d2);

        var s1 = startDay;
        var s2 = startDay + d1;
        var s3 = startDay + d1 + d2;
        var end = startDay + durationDays;

        return new[]
        {
            new Detail(titles[0], s1, Math.Max(0, Math.Min(end - s1, d1))),
            new Detail(titles[1], s2, Math.Max(0, Math.Min(end - s2, d2))),
            new Detail(titles[2], s3, Math.Max(0, Math.Min(end - s3, d3)))
        };
    }

    private static class CellFactory
    {
        public static DocumentFormat.OpenXml.Spreadsheet.Cell Text(string text)
        {
            return new DocumentFormat.OpenXml.Spreadsheet.Cell
            {
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(text) { Space = SpaceProcessingModeValues.Preserve })
            };
        }

        public static DocumentFormat.OpenXml.Spreadsheet.Cell Number(int number)
        {
            return new DocumentFormat.OpenXml.Spreadsheet.Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture))
            };
        }
    }
}
