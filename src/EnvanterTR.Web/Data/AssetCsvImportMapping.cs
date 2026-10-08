namespace EnvanterTR.Web.Data;

internal static class AssetCsvImportMapping
{
    private static readonly string[][] HeaderAliases =
    [
        ["Etiket", "Asset Tag", "asset_tag", "Tag"],
        ["Ad", "Name", "Item Name", "Asset Name"],
        ["Durum", "Status", "Asset Status"],
        ["Kategori", "Category", "Category Name", "Asset Category"],
        ["Marka", "Manufacturer", "Manufacturer Name", "Brand", "Asset Manufacturer"],
        ["Model", "Model Name", "Asset Model"],
        ["Seri No", "Serial Number", "Serial No", "Serial", "Asset Serial", "Asset Serial Number"],
        ["Konum", "Location", "Asset Location"],
        ["Personel No", "Employee Number", "Employee No", "Employee #"],
        ["Çalışan", "Employee Name", "Full Name"],
        ["Alış Tarihi", "Purchase Date", "Asset Purchase Date"],
        ["Garanti Bitiş Tarihi", "Warranty Expiration", "Warranty Expiration Date", "Asset Warranty"],
        ["Alış Bedeli", "Purchase Cost", "Asset Purchase Cost"],
        ["Para Birimi", "Currency"],
        ["Üst Varlık Etiketi", "Parent Asset Tag", "Parent Tag"]
    ];

    private static readonly string[] CanonicalBaseHeaders =
        ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi"];

    public static List<List<string>> Normalize(List<List<string>> records,
        IReadOnlyCollection<AssetCustomFieldDefinition>? customFields, out string[] unsupportedColumns)
    {
        unsupportedColumns = [];
        if (records.Count == 0) return records;

        var sourceHeader = records[0];
        var sourceIndexes = new int[HeaderAliases.Length];
        Array.Fill(sourceIndexes, -1);
        var customIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var matchedIndexes = new HashSet<int>();
        for (var sourceIndex = 0; sourceIndex < sourceHeader.Count; sourceIndex++)
        {
            var normalized = NormalizeHeader(sourceHeader[sourceIndex]);
            var baseMatches = HeaderAliases.Select((aliases, index) => (aliases, index))
                .Where(item => item.aliases.Any(alias => NormalizeHeader(alias) == normalized)).ToArray();
            if (baseMatches.Length > 1)
            {
                unsupportedColumns = [sourceHeader[sourceIndex].Trim()];
                return records;
            }
            if (baseMatches.Length == 1)
            {
                var targetIndex = baseMatches[0].index;
                if (sourceIndexes[targetIndex] >= 0)
                {
                    unsupportedColumns = [sourceHeader[sourceIndex].Trim()];
                    return records;
                }
                sourceIndexes[targetIndex] = sourceIndex;
                matchedIndexes.Add(sourceIndex);
                continue;
            }
            var customMatches = (customFields ?? []).Where(field =>
                new[] { field.Key, field.Name, $"Özel alan: {field.Key}", $"Custom field: {field.Key}", $"Custom: {field.Key}" }
                    .Any(alias => NormalizeHeader(alias) == normalized)).ToArray();
            if (customMatches.Length > 1)
            {
                unsupportedColumns = [sourceHeader[sourceIndex].Trim()];
                return records;
            }
            if (customMatches.Length == 1)
            {
                var key = customMatches[0].Key;
                if (customIndexes.ContainsKey(key))
                {
                    unsupportedColumns = [sourceHeader[sourceIndex].Trim()];
                    return records;
                }
                customIndexes[key] = sourceIndex;
                matchedIndexes.Add(sourceIndex);
                continue;
            }
        }

        if (sourceIndexes[0] < 0 || sourceIndexes[1] < 0 || sourceIndexes[3] < 0)
            return records;

        var hasWarranty = sourceIndexes[11] >= 0;
        var hasParent = sourceIndexes[14] >= 0;
        var targetHeaders = new List<string>(CanonicalBaseHeaders);
        if (hasWarranty) targetHeaders.Add("Garanti Bitiş Tarihi");
        targetHeaders.Add("Alış Bedeli");
        targetHeaders.Add("Para Birimi");
        if (hasParent) targetHeaders.Add("Üst Varlık Etiketi");
        foreach (var field in (customFields ?? []).OrderBy(field => field.SortOrder).ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
            targetHeaders.Add($"Özel alan: {field.Key}");

        var sourceRows = records.Skip(1).ToArray();
        unsupportedColumns = sourceHeader.Select((header, index) => (header, index))
            .Where(item => !matchedIndexes.Contains(item.index) && sourceRows.Any(row =>
                item.index < row.Count && !string.IsNullOrWhiteSpace(row[item.index])))
            .Select(item => item.header.Trim())
            .Where(header => header.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

        var result = new List<List<string>>(records.Count) { targetHeaders };
        foreach (var row in sourceRows)
        {
            // Preserve malformed row widths so the established validator rejects them.
            if (row.Count != sourceHeader.Count)
            {
                result.Add(row);
                continue;
            }

            var mapped = new List<string>(targetHeaders.Count);
            for (var i = 0; i < CanonicalBaseHeaders.Length; i++)
                mapped.Add(Read(row, sourceIndexes[i]));
            if (sourceIndexes[2] < 0) mapped[2] = AssetStatuses.Available;
            else mapped[2] = NormalizeStatus(mapped[2]);
            mapped[10] = NormalizeDate(mapped[10]);
            if (sourceIndexes[11] >= 0) mapped.Add(NormalizeDate(Read(row, sourceIndexes[11])));
            mapped.Add(NormalizeDecimal(Read(row, sourceIndexes[12])));
            mapped.Add(string.IsNullOrWhiteSpace(Read(row, sourceIndexes[13])) ? "TRY" : Read(row, sourceIndexes[13]));
            if (hasParent) mapped.Add(Read(row, sourceIndexes[14]));
            foreach (var field in (customFields ?? []).OrderBy(field => field.SortOrder).ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
                mapped.Add(customIndexes.TryGetValue(field.Key, out var customIndex) ? Read(row, customIndex) : "");
            result.Add(mapped);
        }
        return result;
    }

    private static string Read(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index].Trim() : "";

    private static string NormalizeHeader(string value) => new(value
        .Where(char.IsLetterOrDigit)
        .Select(char.ToLowerInvariant)
        .ToArray());

    private static string NormalizeStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "ready to deploy" or "ready" or "available" or "kullanılabilir" => AssetStatuses.Available,
        "deployed" or "assigned" or "atanmış" => AssetStatuses.Deployed,
        "pending" or "beklemede" => AssetStatuses.Pending,
        "maintenance" or "out for repair" or "bakımda" => AssetStatuses.Maintenance,
        "unusable" or "broken" or "hurda" or "kullanılamaz" => AssetStatuses.Unusable,
        "archived" or "arşivlenmiş" => AssetStatuses.Archived,
        _ => value.Trim()
    };

    private static string NormalizeDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string[] formats = ["yyyy-MM-dd", "M/d/yy", "MM/dd/yy", "M/d/yyyy", "MM/dd/yyyy", "yyyy/M/d", "yyyy/MM/dd", "d.M.yy", "dd.MM.yy", "d.M.yyyy", "dd.MM.yyyy"];
        return DateOnly.TryParseExact(value.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : value.Trim();
    }

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
        else if (comma >= 0)
        {
            // A single comma is the decimal separator in Turkish exports.
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : value.Trim();
    }
}
