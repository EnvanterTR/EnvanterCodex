namespace EnvanterTR.Web.Data;

internal static class ComponentCsvImportMapping
{
    private static readonly string[][] Aliases =
    [
        ["Bileşen", "Bilesen", "Component", "Component Name", "Name", "Item Name"],
        ["Kategori", "Category", "Component Category"],
        ["Model", "Model Number", "Model No"],
        ["Ürün no", "Urun no", "Ürün numarası", "Item Number", "Item No", "SKU"],
        ["Seri no", "Seri numarası", "Serial Number", "Serial No"],
        ["Tedarikçi", "Tedarikci", "Supplier"],
        ["Konum", "Location"],
        ["Toplam", "Total", "Total Quantity", "Quantity", "Qty"],
        ["Minimum", "Minimum Quantity", "Min Qty", "Min. Qty"],
        ["Birim maliyeti", "Birim maliyet", "Unit Cost", "Purchase Cost"],
        ["Para birimi", "Currency"],
        ["Alış tarihi", "Alis tarihi", "Purchase Date"],
        ["Notlar", "Notes"]
    ];

    private static readonly string[] Canonical =
        ["Bileşen", "Kategori", "Model", "Ürün no", "Seri no", "Tedarikçi", "Konum", "Toplam", "Minimum", "Birim maliyeti", "Para birimi", "Alış tarihi", "Notlar"];

    public static List<List<string>> Normalize(List<List<string>> records, out string[] unsupportedColumns)
    {
        unsupportedColumns = [];
        if (records.Count == 0) return records;
        var indexes = Enumerable.Repeat(-1, Canonical.Length).ToArray();
        var matched = new HashSet<int>();
        for (var source = 0; source < records[0].Count; source++)
        {
            var header = NormalizeHeader(records[0][source]);
            for (var target = 0; target < Aliases.Length; target++)
            {
                if (!Aliases[target].Any(alias => NormalizeHeader(alias) == header)) continue;
                if (indexes[target] >= 0) return records;
                indexes[target] = source;
                matched.Add(source);
                break;
            }
        }
        if (indexes[0] < 0 || indexes[7] < 0) return records;

        var sourceRows = records.Skip(1).ToArray();
        unsupportedColumns = records[0].Select((name, index) => (name, index))
            .Where(x => !matched.Contains(x.index) && sourceRows.Any(row => x.index < row.Count && !string.IsNullOrWhiteSpace(row[x.index])))
            .Select(x => x.name.Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

        var outputRows = new List<List<string>>(records.Count);
        outputRows.Add(Canonical.ToList());
        foreach (var row in sourceRows)
        {
            if (row.Count != records[0].Count) { outputRows.Add(row); continue; }
            var output = indexes.Select(index => index >= 0 ? row[index].Trim() : "").ToList();
            output[9] = NormalizeDecimal(output[9]);
            output[10] = string.IsNullOrWhiteSpace(output[10]) ? "TRY" : output[10];
            output[11] = NormalizeDate(output[11]);
            outputRows.Add(output);
        }
        return outputRows;
    }

    private static string NormalizeHeader(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string NormalizeDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var normalized = value.Trim().Replace(" ", "", StringComparison.Ordinal);
        var comma = normalized.LastIndexOf(',');
        var dot = normalized.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            var decimalSeparator = comma > dot ? ',' : '.';
            var groupingSeparator = decimalSeparator == ',' ? '.' : ',';
            normalized = normalized.Replace(groupingSeparator.ToString(), "", StringComparison.Ordinal)
                .Replace(decimalSeparator, '.');
        }
        else if (comma >= 0) normalized = normalized.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : value.Trim();
    }

    private static string NormalizeDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string[] formats = ["yyyy-MM-dd", "M/d/yy", "MM/dd/yy", "M/d/yyyy", "MM/dd/yyyy", "d.M.yy", "dd.MM.yy", "d.M.yyyy", "dd.MM.yyyy"];
        return DateOnly.TryParseExact(value.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : value.Trim();
    }
}
