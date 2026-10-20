using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace EnvanterTR.Web.Data;

// Uses a private temporary file so large exports do not accumulate in memory.
// Text is always an inline string; user input can never become an Excel formula.
internal sealed class XlsxExportWriter : IDisposable
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private readonly FileStream file;
    private readonly ZipArchive archive;
    private readonly XmlWriter sheet;
    private readonly int columnCount;
    private int rowNumber;
    private bool finished;

    public XlsxExportWriter(IReadOnlyList<string> headers)
    {
        if (headers.Count is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(headers));
        columnCount = headers.Count;
        var fileOptions = new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.ReadWrite, Share = FileShare.None,
            BufferSize = 64 * 1024, Options = FileOptions.DeleteOnClose | FileOptions.SequentialScan
        };
        if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        file = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), fileOptions);
        try
        {
            archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true);
            AddPart("[Content_Types].xml", """
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
                """);
            AddPart("_rels/.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            AddPart("xl/workbook.xml", """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="EnvanterTR" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            AddPart("xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            AddPart("xl/styles.xml", """
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF245DA8"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="3"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/><xf numFmtId="4" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/></cellXfs><cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles></styleSheet>
                """);
            sheet = XmlWriter.Create(archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest).Open(),
                new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = true });
            sheet.WriteStartDocument();
            sheet.WriteStartElement("worksheet", Spreadsheet);
            sheet.WriteStartElement("sheetViews"); sheet.WriteStartElement("sheetView"); sheet.WriteAttributeString("workbookViewId", "0");
            sheet.WriteStartElement("pane"); sheet.WriteAttributeString("ySplit", "1"); sheet.WriteAttributeString("topLeftCell", "A2");
            sheet.WriteAttributeString("activePane", "bottomLeft"); sheet.WriteAttributeString("state", "frozen");
            sheet.WriteEndElement(); sheet.WriteEndElement(); sheet.WriteEndElement();
            sheet.WriteStartElement("cols"); sheet.WriteStartElement("col"); sheet.WriteAttributeString("min", "1");
            sheet.WriteAttributeString("max", columnCount.ToString(CultureInfo.InvariantCulture)); sheet.WriteAttributeString("width", "24");
            sheet.WriteAttributeString("customWidth", "1"); sheet.WriteEndElement(); sheet.WriteEndElement();
            sheet.WriteStartElement("sheetData");
            WriteRow(headers.Cast<object?>(), header: true);
        }
        catch { file.Dispose(); throw; }
    }

    public void WriteRow(IEnumerable<object?> values, bool header = false)
    {
        if (finished) throw new InvalidOperationException("Workbook is already complete.");
        if (++rowNumber > 1_048_576) throw new InvalidOperationException("Excel worksheet row limit exceeded.");
        sheet.WriteStartElement("row"); sheet.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
        var column = 0;
        foreach (var value in values)
        {
            if (++column > columnCount) throw new InvalidOperationException("Export row exceeds the header width.");
            sheet.WriteStartElement("c"); sheet.WriteAttributeString("r", ColumnName(column) + rowNumber.ToString(CultureInfo.InvariantCulture));
            var number = value switch
            {
                decimal amount => amount.ToString("0.00", CultureInfo.InvariantCulture),
                int count => count.ToString(CultureInfo.InvariantCulture),
                long count => count.ToString(CultureInfo.InvariantCulture),
                _ => null
            };
            // Excel has only 15 significant digits. Keep larger exact amounts as text.
            var numeric = !header && number is not null && number.Replace(".", "").TrimStart('-', '0').Length <= 15;
            if (header) sheet.WriteAttributeString("s", "1");
            else if (numeric && value is decimal) sheet.WriteAttributeString("s", "2");
            if (numeric) sheet.WriteElementString("v", number);
            else
            {
                sheet.WriteAttributeString("t", "inlineStr"); sheet.WriteStartElement("is"); sheet.WriteStartElement("t");
                sheet.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
                var text = number ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                if (text.Length > 32767) throw new InvalidOperationException("Excel cell text limit exceeded.");
                sheet.WriteString(CleanXml(text)); sheet.WriteEndElement(); sheet.WriteEndElement();
            }
            sheet.WriteEndElement();
        }
        sheet.WriteEndElement();
    }

    public async Task SendAsync(HttpContext context, string fileName, CancellationToken ct)
    {
        sheet.WriteEndElement();
        sheet.WriteStartElement("autoFilter"); sheet.WriteAttributeString("ref", $"A1:{ColumnName(columnCount)}{rowNumber}"); sheet.WriteEndElement();
        sheet.WriteEndElement(); sheet.WriteEndDocument(); sheet.Dispose(); archive.Dispose(); finished = true;
        file.Position = 0;
        context.Response.ContentType = ContentType;
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentLength = file.Length;
        await file.CopyToAsync(context.Response.Body, ct);
    }

    private void AddPart(string name, string xml)
    {
        using var output = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        output.Write(xml);
    }

    private static string ColumnName(int number) => number <= 26 ? ((char)('A' + number - 1)).ToString()
        : ((char)('A' + (number - 1) / 26 - 1)).ToString() + (char)('A' + (number - 1) % 26);

    private static string CleanXml(string value) => string.Concat(value.EnumerateRunes()
        .Where(rune => rune.Value is 9 or 10 or 13 or >= 0x20 and <= 0xD7FF or >= 0xE000 and <= 0xFFFD or >= 0x10000 and <= 0x10FFFF)
        .Select(rune => rune.ToString()));

    public void Dispose()
    {
        try { if (!finished) { sheet?.Dispose(); archive?.Dispose(); } }
        finally { file.Dispose(); }
    }
}
