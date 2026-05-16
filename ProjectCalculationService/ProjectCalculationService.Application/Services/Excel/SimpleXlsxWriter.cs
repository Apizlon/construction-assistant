using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace ProjectCalculationService.Application.Services.Excel;

public static class SimpleXlsxWriter
{
    private const string SpreadsheetMlNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeDocRelsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] CreateWorkbook(IReadOnlyList<Sheet> sheets, Chart? chart = null)
    {
        if (sheets.Count == 0) throw new ArgumentException("At least one sheet is required", nameof(sheets));

        var chartHostSheetIndex = -1;
        if (chart != null)
        {
            chartHostSheetIndex = FindSheetIndex(sheets, chart.HostSheetName);
            if (chartHostSheetIndex < 0)
            {
                throw new ArgumentException($"Host sheet '{chart.HostSheetName}' not found", nameof(chart));
            }
        }

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "[Content_Types].xml", BuildContentTypesXml(sheets.Count, hasChart: chart != null));
            WriteEntry(zip, "_rels/.rels", BuildRootRelsXml());
            WriteEntry(zip, "xl/workbook.xml", BuildWorkbookXml(sheets));
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml(sheets.Count));
            WriteEntry(zip, "xl/styles.xml", BuildStylesXml());

            for (var i = 0; i < sheets.Count; i++)
            {
                var sheetIndex = i + 1;
                var isChartHost = chartHostSheetIndex == i;
                WriteEntry(zip, $"xl/worksheets/sheet{sheetIndex}.xml", BuildWorksheetXml(sheets[i], sheetIndex, isChartHost));
                if (isChartHost)
                {
                    WriteEntry(zip, $"xl/worksheets/_rels/sheet{sheetIndex}.xml.rels", BuildSheetRelsXml());
                }
            }

            if (chart != null)
            {
                WriteEntry(zip, "xl/drawings/drawing1.xml", BuildDrawingXml());
                WriteEntry(zip, "xl/drawings/_rels/drawing1.xml.rels", BuildDrawingRelsXml());
                WriteEntry(zip, "xl/charts/chart1.xml", BuildChartXml(chart));
            }
        }

        return ms.ToArray();
    }

    public sealed record Sheet(string Name, IReadOnlyList<IReadOnlyList<object?>> Rows);
    public sealed record Formula(string Expr);
    public sealed record Chart(string HostSheetName, string Title, string CategoriesRange, IReadOnlyList<ChartSeries> Series);
    public sealed record ChartSeries(string Name, string ValuesRange, bool Hidden);

    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string BuildContentTypesXml(int sheetCount, bool hasChart)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
        sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
        sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
        sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
        sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }
        if (hasChart)
        {
            sb.Append("<Override PartName=\"/xl/drawings/drawing1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawing+xml\"/>");
            sb.Append("<Override PartName=\"/xl/charts/chart1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.drawingml.chart+xml\"/>");
        }
        sb.Append("</Types>");
        return sb.ToString();
    }

    private static string BuildRootRelsXml()
    {
        return
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Relationships xmlns=\"{RelationshipsNs}\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";
    }

    private static string BuildWorkbookXml(IReadOnlyList<Sheet> sheets)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<workbook xmlns=\"{SpreadsheetMlNs}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        sb.Append("<sheets>");
        for (var i = 0; i < sheets.Count; i++)
        {
            var sheetId = i + 1;
            sb.Append($"<sheet name=\"{EscapeXmlAttr(TrimSheetName(sheets[i].Name))}\" sheetId=\"{sheetId}\" r:id=\"rId{sheetId}\"/>");
        }
        sb.Append("</sheets>");
        sb.Append("</workbook>");
        return sb.ToString();
    }

    private static string BuildWorkbookRelsXml(int sheetCount)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append($"<Relationships xmlns=\"{RelationshipsNs}\">");
        for (var i = 1; i <= sheetCount; i++)
        {
            sb.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
        }
        sb.Append($"<Relationship Id=\"rId{sheetCount + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
        sb.Append("</Relationships>");
        return sb.ToString();
    }

    private static string BuildStylesXml()
    {
        // Minimal style part (default font/fill/border).
        return
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<styleSheet xmlns=\"{SpreadsheetMlNs}\">" +
            "<fonts count=\"1\"><font><sz val=\"11\"/><color theme=\"1\"/><name val=\"Calibri\"/><family val=\"2\"/></font></fonts>" +
            "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
            "</styleSheet>";
    }

    private static string BuildWorksheetXml(Sheet sheet, int sheetIndex, bool isChartHost)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        if (isChartHost)
        {
            sb.Append($"<worksheet xmlns=\"{SpreadsheetMlNs}\" xmlns:r=\"{OfficeDocRelsNs}\">");
        }
        else
        {
            sb.Append($"<worksheet xmlns=\"{SpreadsheetMlNs}\">");
        }
        sb.Append(BuildAutoColsXml(sheet));
        sb.Append("<sheetData>");

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var rowIndex = r + 1;
            var row = sheet.Rows[r];
            sb.Append($"<row r=\"{rowIndex}\">");
            for (var c = 0; c < row.Count; c++)
            {
                var colIndex = c + 1;
                var cellRef = $"{ColName(colIndex)}{rowIndex}";
                sb.Append(BuildCellXml(cellRef, row[c]));
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData>");
        if (isChartHost)
        {
            sb.Append("<drawing r:id=\"rId1\"/>");
        }
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string BuildAutoColsXml(Sheet sheet)
    {
        var maxCols = sheet.Rows.Count == 0 ? 0 : sheet.Rows.Max(r => r.Count);
        if (maxCols == 0) return string.Empty;

        var maxLen = new int[maxCols];
        foreach (var row in sheet.Rows)
        {
            for (var c = 0; c < row.Count; c++)
            {
                var len = EstimateDisplayLength(row[c]);
                if (len > maxLen[c]) maxLen[c] = len;
            }
        }

        var sb = new StringBuilder();
        sb.Append("<cols>");
        for (var i = 0; i < maxCols; i++)
        {
            var width = ComputeExcelWidth(maxLen[i], i + 1);
            sb.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{width.ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        }
        sb.Append("</cols>");
        return sb.ToString();
    }

    private static int EstimateDisplayLength(object? value)
    {
        if (value == null) return 0;
        if (value is Formula) return 10;
        if (value is DateTime) return 20;
        var s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        s = s.Replace("\r", "").Replace("\n", " ");
        return s.Length;
    }

    private static double ComputeExcelWidth(int maxLen, int colIndex1Based)
    {
        // Rough heuristic: Excel width is approx. number of '0' characters.
        // Keep some padding and clamp for readability.
        var baseWidth = Math.Clamp(maxLen + 2, 8, 60);

        // For the first text columns, allow more space.
        if (colIndex1Based <= 2) baseWidth = Math.Clamp(maxLen + 3, 10, 80);

        return baseWidth;
    }

    private static string BuildCellXml(string cellRef, object? value)
    {
        if (value == null) return $"<c r=\"{cellRef}\"/>";

        return value switch
        {
            Formula f => $"<c r=\"{cellRef}\"><f>{EscapeXmlText(NormalizeFormula(f.Expr))}</f></c>",
            int i => $"<c r=\"{cellRef}\" t=\"n\"><v>{i.ToString(CultureInfo.InvariantCulture)}</v></c>",
            long l => $"<c r=\"{cellRef}\" t=\"n\"><v>{l.ToString(CultureInfo.InvariantCulture)}</v></c>",
            decimal d => $"<c r=\"{cellRef}\" t=\"n\"><v>{d.ToString(CultureInfo.InvariantCulture)}</v></c>",
            double dbl => $"<c r=\"{cellRef}\" t=\"n\"><v>{dbl.ToString(CultureInfo.InvariantCulture)}</v></c>",
            float f => $"<c r=\"{cellRef}\" t=\"n\"><v>{f.ToString(CultureInfo.InvariantCulture)}</v></c>",
            bool b => $"<c r=\"{cellRef}\" t=\"b\"><v>{(b ? "1" : "0")}</v></c>",
            DateTime dt => $"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>{EscapeXmlText(dt.ToString("u", CultureInfo.InvariantCulture))}</t></is></c>",
            _ => $"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>{EscapeXmlText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}</t></is></c>"
        };
    }

    private static string ColName(int index1Based)
    {
        var n = index1Based;
        var sb = new StringBuilder();
        while (n > 0)
        {
            n--;
            sb.Insert(0, (char)('A' + (n % 26)));
            n /= 26;
        }
        return sb.ToString();
    }

    private static string TrimSheetName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return "Sheet1";
        // Excel sheet name length max = 31.
        if (trimmed.Length > 31) trimmed = trimmed[..31];
        return trimmed;
    }

    private static string EscapeXmlText(string s)
    {
        // Basic XML escaping for text nodes.
        return SecurityElement.Escape(s) ?? string.Empty;
    }

    private static string EscapeXmlAttr(string s)
    {
        return EscapeXmlText(s);
    }

    private static string NormalizeFormula(string expr)
    {
        var trimmed = (expr ?? string.Empty).Trim();
        if (trimmed.StartsWith('=')) return trimmed[1..];
        return trimmed;
    }

    private static int FindSheetIndex(IReadOnlyList<Sheet> sheets, string name)
    {
        for (var i = 0; i < sheets.Count; i++)
        {
            if (string.Equals(sheets[i].Name, name, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private static string BuildSheetRelsXml()
    {
        // Link worksheet -> drawing
        return
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Relationships xmlns=\"{RelationshipsNs}\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing\" Target=\"../drawings/drawing1.xml\"/>" +
            "</Relationships>";
    }

    private static string BuildDrawingXml()
    {
        // Place chart on a drawing canvas via a oneCellAnchor.
        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<xdr:oneCellAnchor>" +
            "<xdr:from><xdr:col>0</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>1</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>" +
            "<xdr:ext cx=\"9900000\" cy=\"5200000\"/>" +
            "<xdr:graphicFrame macro=\"\">" +
            "<xdr:nvGraphicFramePr><xdr:cNvPr id=\"2\" name=\"Диаграмма 1\"/><xdr:cNvGraphicFramePr><a:graphicFrameLocks noGrp=\"1\"/></xdr:cNvGraphicFramePr></xdr:nvGraphicFramePr>" +
            "<xdr:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"9900000\" cy=\"5200000\"/></xdr:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\">" +
            "<c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"rId1\"/>" +
            "</a:graphicData></a:graphic>" +
            "</xdr:graphicFrame>" +
            "<xdr:clientData/>" +
            "</xdr:oneCellAnchor>" +
            "</xdr:wsDr>";
    }

    private static string BuildDrawingRelsXml()
    {
        // Link drawing -> chart
        return
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            $"<Relationships xmlns=\"{RelationshipsNs}\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart\" Target=\"../charts/chart1.xml\"/>" +
            "</Relationships>";
    }

    private static string BuildChartXml(Chart chart)
    {
        // Minimal stacked bar chart: first series is "offset" (hidden), second is "duration".
        // Categories and values are references like: План!$B$2:$B$6, План!$C$2:$C$6, План!$D$2:$D$6
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<c:chartSpace xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        sb.Append("<c:chart>");
        sb.Append("<c:title><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>");
        sb.Append(EscapeXmlText(chart.Title));
        sb.Append("</a:t></a:r></a:p></c:rich></c:tx></c:title>");
        sb.Append("<c:plotArea><c:layout/>");
        sb.Append("<c:barChart>");
        sb.Append("<c:barDir val=\"bar\"/>");
        sb.Append("<c:grouping val=\"stacked\"/>");

        for (var i = 0; i < chart.Series.Count; i++)
        {
            var ser = chart.Series[i];
            var idx = i;
            sb.Append("<c:ser>");
            sb.Append($"<c:idx val=\"{idx}\"/><c:order val=\"{idx}\"/>");
            sb.Append("<c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>");
            sb.Append(EscapeXmlText(ser.Name));
            sb.Append("</a:t></a:r></a:p></c:rich></c:tx>");

            sb.Append("<c:cat><c:strRef><c:f>");
            sb.Append(EscapeXmlText(chart.CategoriesRange));
            sb.Append("</c:f></c:strRef></c:cat>");

            sb.Append("<c:val><c:numRef><c:f>");
            sb.Append(EscapeXmlText(ser.ValuesRange));
            sb.Append("</c:f></c:numRef></c:val>");

            if (ser.Hidden)
            {
                // Make series invisible (no fill, no line)
                sb.Append("<c:spPr><a:noFill/><a:ln><a:noFill/></a:ln></c:spPr>");
            }

            sb.Append("</c:ser>");
        }

        sb.Append("<c:axId val=\"123456\"/><c:axId val=\"654321\"/>");
        sb.Append("</c:barChart>");

        // Category axis (tasks)
        sb.Append("<c:catAx>");
        sb.Append("<c:axId val=\"654321\"/>");
        sb.Append("<c:scaling><c:orientation val=\"minMax\"/></c:scaling>");
        sb.Append("<c:axPos val=\"l\"/>");
        sb.Append("<c:tickLblPos val=\"nextTo\"/>");
        sb.Append("<c:crossAx val=\"123456\"/>");
        sb.Append("<c:crosses val=\"autoZero\"/>");
        sb.Append("</c:catAx>");

        // Value axis (days)
        sb.Append("<c:valAx>");
        sb.Append("<c:axId val=\"123456\"/>");
        sb.Append("<c:scaling><c:orientation val=\"minMax\"/></c:scaling>");
        sb.Append("<c:axPos val=\"b\"/>");
        sb.Append("<c:majorGridlines/>");
        sb.Append("<c:numFmt formatCode=\"0\" sourceLinked=\"1\"/>");
        sb.Append("<c:tickLblPos val=\"nextTo\"/>");
        sb.Append("<c:crossAx val=\"654321\"/>");
        sb.Append("<c:crosses val=\"autoZero\"/>");
        sb.Append("</c:valAx>");

        sb.Append("</c:plotArea>");
        sb.Append("<c:legend><c:legendPos val=\"r\"/><c:overlay val=\"0\"/></c:legend>");
        sb.Append("<c:plotVisOnly val=\"1\"/>");
        sb.Append("<c:dispBlanksAs val=\"gap\"/>");
        sb.Append("</c:chart>");
        sb.Append("</c:chartSpace>");
        return sb.ToString();
    }
}
