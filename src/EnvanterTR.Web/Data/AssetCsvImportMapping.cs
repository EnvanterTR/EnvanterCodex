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
        ["Üst Varlık Etiketi", "Parent Asset Tag", "Parent Tag"],
        ["BYOD", "Kişisel Cihaz", "Personal Device", "Bring Your Own Device"]
    ];

    private static readonly string[] CanonicalBaseHeaders =
        ["Etiket", "Ad", "Durum", "Kategori", "Marka", "Model", "Seri No", "Konum", "Personel No", "Çalışan", "Alış Tarihi"];

    private static readonly string[] TargetKeys =
        ["assetTag", "name", "status", "category", "brand", "model", "serialNumber", "location", "employeeNumber", "employeeName", "purchaseDate", "warrantyExpiration", "purchaseCost", "currency", "parentAssetTag", "isByod"];

    public static object[] MappingTargets(IReadOnlyCollection<AssetCustomFieldDefinition>? customFields) =>
        TargetKeys.Select((key, index) => new { key, label = index < CanonicalBaseHeaders.Length ? CanonicalBaseHeaders[index] : index switch
        {
            11 => "Garanti Bitiş Tarihi", 12 => "Alış Bedeli", 13 => "Para Birimi", 14 => "Üst Varlık Etiketi", _ => "BYOD"
        } })
        .Concat((customFields ?? []).OrderBy(field => field.SortOrder).ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
            .Select(field => new { key = $"custom:{field.Key}", label = $"Özel alan: {field.Name}" }))
        .Append(new { key = "ignore", label = "Bu sütunu yoksay" })
        .Cast<object>().ToArray();

    public static bool ValidateExplicitMappings(IReadOnlyDictionary<string, string>? mappings,
        IReadOnlyCollection<AssetCustomFieldDefinition>? customFields, out string error)
    {
        error = "";
        if (mappings is null || mappings.Count is < 1 or > 20)
        {
            error = "Bir eşleme profili 1–20 sütun eşlemesi içermelidir.";
            return false;
        }
        var sources = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (source, target) in mappings)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Trim().Length > 160 || !sources.Add(NormalizeHeader(source)))
            {
                error = "Kaynak sütun adları boş, çok uzun veya yinelenen olamaz.";
                return false;
            }
            var supported = target == "ignore" || TargetKeys.Contains(target, StringComparer.Ordinal) ||
                target.StartsWith("custom:", StringComparison.Ordinal) && (customFields ?? []).Any(field => field.IsActive && field.Key == target[7..]);
            if (!supported)
            {
                error = $"“{source}” sütununun hedef alanı bu şirkette etkin değil veya geçersiz.";
                return false;
            }
            if (target != "ignore" && !targets.Add(target))
            {
                error = "Aynı varlık alanına birden fazla sütun eşlenemez.";
                return false;
            }
        }
        return true;
    }

    public static List<List<string>> Normalize(List<List<string>> records,
        IReadOnlyCollection<AssetCustomFieldDefinition>? customFields, out string[] unsupportedColumns,
        IReadOnlyDictionary<string, string>? explicitMappings = null)
    {
        unsupportedColumns = [];
        if (records.Count == 0) return records;

        var sourceHeader = records[0];
        var sourceIndexes = new int[HeaderAliases.Length];
        Array.Fill(sourceIndexes, -1);
        var customIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var matchedIndexes = new HashSet<int>();
        var mappingErrors = new List<string>();
        for (var sourceIndex = 0; sourceIndex < sourceHeader.Count; sourceIndex++)
        {
            var normalized = NormalizeHeader(sourceHeader[sourceIndex]);
            if (explicitMappings?.Keys.Any(key => NormalizeHeader(key) == normalized) == true) continue;
            var baseMatches = HeaderAliases.Select((aliases, index) => (aliases, index))
                .Where(item => item.aliases.Any(alias => NormalizeHeader(alias) == normalized)).ToArray();
            if (baseMatches.Length > 1)
            {
                continue;
            }
            if (baseMatches.Length == 1)
            {
                var targetIndex = baseMatches[0].index;
                if (sourceIndexes[targetIndex] >= 0)
                {
                    continue;
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
                continue;
            }
            if (customMatches.Length == 1)
            {
                var key = customMatches[0].Key;
                if (customIndexes.ContainsKey(key))
                {
                    continue;
                }
                customIndexes[key] = sourceIndex;
                matchedIndexes.Add(sourceIndex);
                continue;
            }
        }

        foreach (var (sourceName, targetKey) in explicitMappings ?? new Dictionary<string, string>())
        {
            var candidates = sourceHeader.Select((header, index) => (header, index))
                .Where(item => NormalizeHeader(item.header) == NormalizeHeader(sourceName)).Select(item => item.index).ToArray();
            // A saved profile can contain columns absent from a later file; keep applying the matching entries.
            if (candidates.Length == 0) continue;
            if (candidates.Length != 1)
            {
                mappingErrors.Add(sourceName);
                continue;
            }
            var sourceIndex = candidates[0];
            if (matchedIndexes.Contains(sourceIndex))
            {
                mappingErrors.Add(sourceName);
                continue;
            }
            if (targetKey == "ignore")
            {
                matchedIndexes.Add(sourceIndex);
                continue;
            }
            var targetIndex = Array.IndexOf(TargetKeys, targetKey);
            if (targetIndex >= 0)
            {
                if (sourceIndexesForTarget(targetIndex) >= 0)
                {
                    mappingErrors.Add(sourceName);
                    continue;
                }
                sourceIndexes[targetIndex] = sourceIndex;
                matchedIndexes.Add(sourceIndex);
                continue;
            }
            if (targetKey.StartsWith("custom:", StringComparison.Ordinal) &&
                (customFields ?? []).Any(field => field.Key == targetKey[7..]) && !customIndexes.ContainsKey(targetKey[7..]))
            {
                customIndexes[targetKey[7..]] = sourceIndex;
                matchedIndexes.Add(sourceIndex);
                continue;
            }
            mappingErrors.Add(sourceName);
        }
        if (mappingErrors.Count > 0)
        {
            unsupportedColumns = mappingErrors.Take(20).ToArray();
            return records;
        }

        int sourceIndexesForTarget(int index) => sourceIndexes[index];

        if (sourceIndexes[0] < 0 || sourceIndexes[1] < 0 || sourceIndexes[3] < 0)
        {
            unsupportedColumns = sourceHeader.Select((header, index) => (header, index))
                .Where(item => !matchedIndexes.Contains(item.index)).Select(item => item.header.Trim())
                .Where(header => header.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
            return records;
        }

        var hasWarranty = sourceIndexes[11] >= 0;
        var hasParent = sourceIndexes[14] >= 0;
        var hasByod = sourceIndexes[15] >= 0;
        var targetHeaders = new List<string>(CanonicalBaseHeaders);
        if (hasWarranty) targetHeaders.Add("Garanti Bitiş Tarihi");
        targetHeaders.Add("Alış Bedeli");
        targetHeaders.Add("Para Birimi");
        if (hasParent) targetHeaders.Add("Üst Varlık Etiketi");
        targetHeaders.Add("BYOD");
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
            mapped.Add(hasByod ? NormalizeBoolean(Read(row, sourceIndexes[15])) : "false");
            foreach (var field in (customFields ?? []).OrderBy(field => field.SortOrder).ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase))
                mapped.Add(customIndexes.TryGetValue(field.Key, out var customIndex) ? Read(row, customIndex) : "");
            result.Add(mapped);
        }
        return result;
    }

    private static string Read(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index].Trim() : "";

    private static string NormalizeBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" or "evet" or "kişisel" or "personal" => "true",
        "0" or "false" or "no" or "hayır" or "hayir" or "şirket" or "company" or "" => "false",
        _ => value.Trim()
    };

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
