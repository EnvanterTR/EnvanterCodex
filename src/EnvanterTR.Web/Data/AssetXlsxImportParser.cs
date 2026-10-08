using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EnvanterTR.Web.Data;

internal static class AssetXlsxImportParser
{
    private const long MaximumExpandedBytes = 64L * 1024 * 1024;
    private const int MaximumColumns = 100;
    private const int MaximumCellCharacters = 32_000;
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace DocumentRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static List<List<string>> Parse(Stream input, int maximumRecords)
    {
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count is 0 or > 2048)
            throw new InvalidDataException("Excel paketi boş veya beklenmeyen sayıda dosya içeriyor.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        long expandedBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.StartsWith("/", StringComparison.Ordinal) || entry.FullName.Contains('\\') ||
                entry.FullName.Split('/').Any(segment => segment is "." or ".."))
                throw new InvalidDataException("Excel paketi güvenli olmayan dosya yolları içeriyor.");
            if (!entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("Excel paketi yinelenen dosya adları içeriyor.");
            expandedBytes += entry.Length;
            if (entry.Length > MaximumExpandedBytes || expandedBytes > MaximumExpandedBytes)
                throw new InvalidDataException("Excel dosyasının açılmış boyutu güvenlik sınırını aşıyor.");
        }

        var workbook = LoadXml(RequiredEntry(entries, "xl/workbook.xml"));
        var sheets = workbook.Root?.Element(Spreadsheet + "sheets")?.Elements(Spreadsheet + "sheet").ToArray() ?? [];
        if (sheets.Length != 1)
            throw new InvalidDataException("Varlık aktarımı tam olarak bir çalışma sayfası içeren Excel dosyalarını destekler.");
        var relationshipId = (string?)sheets[0].Attribute(DocumentRelationships + "id");
        if (string.IsNullOrWhiteSpace(relationshipId))
            throw new InvalidDataException("Excel çalışma sayfası bağlantısı geçersiz.");

        var relationships = LoadXml(RequiredEntry(entries, "xl/_rels/workbook.xml.rels"));
        var relationship = relationships.Root?.Elements(PackageRelationships + "Relationship")
            .SingleOrDefault(item => (string?)item.Attribute("Id") == relationshipId);
        if (relationship is null || (string?)relationship.Attribute("TargetMode") == "External")
            throw new InvalidDataException("Excel çalışma sayfası hedefi bulunamadı veya dış bağlantı kullanıyor.");
        var target = (string?)relationship.Attribute("Target") ?? "";
        if (target.Contains('\\') || Uri.TryCreate(target, UriKind.Absolute, out _))
            throw new InvalidDataException("Excel çalışma sayfası yolu geçersiz.");
        var worksheetPath = target.StartsWith("/", StringComparison.Ordinal) ? target.TrimStart('/') : $"xl/{target}";
        if (!worksheetPath.StartsWith("xl/worksheets/", StringComparison.Ordinal) ||
            worksheetPath.Split('/').Any(segment => segment is "." or ".."))
            throw new InvalidDataException("Excel çalışma sayfası beklenen klasörün dışında.");

        var sharedStrings = entries.TryGetValue("xl/sharedStrings.xml", out var stringsEntry)
            ? LoadSharedStrings(stringsEntry)
            : [];
        var worksheet = LoadXml(RequiredEntry(entries, worksheetPath));
        var sheetData = worksheet.Root?.Element(Spreadsheet + "sheetData")
            ?? throw new InvalidDataException("Excel çalışma sayfasında hücre verisi yok.");
        var rows = new List<List<string>>();
        foreach (var rowElement in sheetData.Elements(Spreadsheet + "row"))
        {
            var cells = new SortedDictionary<int, string>();
            var nextColumn = 1;
            foreach (var cell in rowElement.Elements(Spreadsheet + "c"))
            {
                var reference = (string?)cell.Attribute("r");
                var column = reference is null ? nextColumn : ParseCellReference(reference).Column;
                if (column < 1 || column > MaximumColumns)
                    throw new InvalidDataException($"Excel çalışma sayfası {MaximumColumns} sütun sınırını aşıyor.");
                nextColumn = column + 1;
                if (cell.Element(Spreadsheet + "f") is not null)
                    throw new InvalidDataException("Formül içeren hücreler kabul edilmiyor. Formülleri değer olarak yapıştırıp dosyayı yeniden yükleyin.");
                cells[column] = ReadCellValue(cell, sharedStrings);
            }

            if (cells.Count == 0 || cells.Values.All(string.IsNullOrWhiteSpace)) continue;
            var width = cells.Keys.Max();
            var row = Enumerable.Repeat("", width).ToList();
            foreach (var (column, value) in cells) row[column - 1] = value;
            rows.Add(row);
            if (rows.Count > maximumRecords)
                throw new InvalidDataException($"Excel dosyası en fazla {maximumRecords - 1:N0} veri satırı içerebilir.");
        }

        if (rows.Count < 2) return rows;
        var uses1904DateSystem = (string?)workbook.Root?.Element(Spreadsheet + "workbookPr")?.Attribute("date1904") is "1" or "true";
        NormalizeDateCells(rows, uses1904DateSystem);
        return rows;
    }

    private static ZipArchiveEntry RequiredEntry(IReadOnlyDictionary<string, ZipArchiveEntry> entries, string name) =>
        entries.TryGetValue(name, out var entry) ? entry : throw new InvalidDataException($"Excel paketi gerekli '{name}' dosyasını içermiyor.");

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumExpandedBytes,
            MaxCharactersFromEntities = 0
        };
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static List<string> LoadSharedStrings(ZipArchiveEntry entry)
    {
        var document = LoadXml(entry);
        var result = new List<string>();
        foreach (var item in document.Root?.Elements(Spreadsheet + "si") ?? [])
        {
            var value = string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value));
            if (value.Length > MaximumCellCharacters)
                throw new InvalidDataException("Excel hücresi izin verilen metin uzunluğunu aşıyor.");
            result.Add(value);
        }
        return result;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        string value;
        if (type == "inlineStr")
            value = string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value));
        else
        {
            value = (string?)cell.Element(Spreadsheet + "v") ?? "";
            if (type == "s")
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) ||
                    index < 0 || index >= sharedStrings.Count)
                    throw new InvalidDataException("Excel hücresi geçersiz bir paylaşılan metin başvurusu içeriyor.");
                value = sharedStrings[index];
            }
            else if (type == "e")
                throw new InvalidDataException("Excel çalışma sayfası hata değeri içeren hücreler barındırıyor.");
            else if (type == "b")
                value = value == "1" ? "TRUE" : "FALSE";
        }
        if (value.Length > MaximumCellCharacters)
            throw new InvalidDataException("Excel hücresi izin verilen metin uzunluğunu aşıyor.");
        return value;
    }

    private static (int Row, int Column) ParseCellReference(string reference)
    {
        var column = 0;
        var index = 0;
        while (index < reference.Length && char.IsAsciiLetter(reference[index]))
        {
            column = checked(column * 26 + char.ToUpperInvariant(reference[index]) - 'A' + 1);
            index++;
        }
        if (index == 0 || index == reference.Length || !int.TryParse(reference.AsSpan(index), NumberStyles.None, CultureInfo.InvariantCulture, out var row) || row < 1)
            throw new InvalidDataException("Excel hücre adresi geçersiz.");
        return (row, column);
    }

    private static void NormalizeDateCells(List<List<string>> rows, bool uses1904DateSystem)
    {
        for (var column = 0; column < rows[0].Count; column++)
        {
            var header = NormalizeHeader(rows[0][column]);
            if (header is not ("alistarihi" or "purchasedate" or "assetpurchasedate" or
                "garantibitistarihi" or "warrantyexpiration" or "warrantyexpirationdate" or "assetwarranty")) continue;
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                if (column >= rows[rowIndex].Count || !double.TryParse(rows[rowIndex][column], NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) ||
                    serial < 0 || serial > 2_958_465) continue;
                try
                {
                    var oaDate = uses1904DateSystem ? serial + 1_462 : serial;
                    rows[rowIndex][column] = DateOnly.FromDateTime(DateTime.FromOADate(oaDate)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                catch (ArgumentException) { }
            }
        }
    }

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
