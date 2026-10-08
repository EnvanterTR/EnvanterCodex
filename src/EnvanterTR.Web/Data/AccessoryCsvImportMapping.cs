namespace EnvanterTR.Web.Data;

internal static class AccessoryCsvImportMapping
{
    private static readonly string[][] Aliases =
    [
        ["Aksesuar", "Accessory", "Accessory Name", "Name"],
        ["Kategori", "Category", "Accessory Category"],
        ["Model", "Model Number", "Model No"],
        ["Ürün no", "Urun no", "Ürün numarası", "Item Number", "Item No", "SKU"],
        ["Tedarikçi", "Tedarikci", "Supplier"],
        ["Konum", "Location"],
        ["Toplam", "Total", "Total Quantity", "Quantity", "Qty"],
        ["Minimum", "Minimum Quantity", "Min Qty", "Min. Qty"],
        ["Birim maliyeti", "Birim maliyet", "Unit Cost", "Purchase Cost"],
        ["Para birimi", "Currency"],
        ["Alış tarihi", "Alis tarihi", "Purchase Date"],
        ["İstek yapılabilir", "Istek yapilabilir", "Requestable", "Is Requestable"],
        ["Notlar", "Notes"]
    ];

    private static readonly string[] Canonical =
        ["Aksesuar", "Kategori", "Model", "Ürün no", "Tedarikçi", "Konum", "Toplam", "Minimum", "Birim maliyeti", "Para birimi", "Alış tarihi", "İstek yapılabilir", "Notlar"];

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

        // A usable import must at least identify the item and the initial stock amount.
        if (indexes[0] < 0 || indexes[6] < 0) return records;
        var dataRows = records.Skip(1).ToArray();
        unsupportedColumns = records[0].Select((name, index) => (name, index))
            .Where(x => !matched.Contains(x.index) && dataRows.Any(row => x.index < row.Count && !string.IsNullOrWhiteSpace(row[x.index])))
            .Select(x => x.name.Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

        var mapped = new List<List<string>>(records.Count);
        mapped.Add(Canonical.ToList());
        foreach (var row in dataRows)
        {
            if (row.Count != records[0].Count) { mapped.Add(row); continue; }
            var output = indexes.Select(index => index >= 0 ? row[index].Trim() : "").ToList();
            output[8] = NormalizeDecimal(output[8]);
            output[9] = string.IsNullOrWhiteSpace(output[9]) ? "TRY" : output[9];
            output[10] = NormalizeDate(output[10]);
            mapped.Add(output);
        }
        return mapped;
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
