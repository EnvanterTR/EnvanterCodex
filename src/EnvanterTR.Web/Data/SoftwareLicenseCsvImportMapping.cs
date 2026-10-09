namespace EnvanterTR.Web.Data;

internal static class SoftwareLicenseCsvImportMapping
{
    private static readonly string[][] Aliases =
    [
        ["Lisans", "License", "License Name", "Software", "Software Name", "Product Name", "Name"],
        ["Yayıncı", "Yayinci", "Publisher", "Manufacturer"],
        ["Ürün sürümü", "Urun surumu", "Product Version", "Version"],
        ["Tür", "Tur", "Type", "License Type"],
        ["Tedarikçi", "Tedarikci", "Supplier"],
        ["Koltuk", "Koltuk sayısı", "Seat Count", "Seats"],
        ["Birim maliyeti", "Birim maliyet", "Unit Cost", "Purchase Cost"],
        ["Para birimi", "Currency"],
        ["Alış tarihi", "Alis tarihi", "Purchase Date"],
        ["Bitiş tarihi", "Bitis tarihi", "Expiration Date", "Expiry Date", "License Expiration Date"],
        ["Otomatik yenileme", "Otomatik yenile", "Auto Renew", "Auto-Renew"],
        ["Notlar", "Notes"]
    ];

    private static readonly string[] Canonical =
        ["Lisans", "Yayıncı", "Ürün sürümü", "Tür", "Tedarikçi", "Koltuk", "Birim maliyeti", "Para birimi", "Alış tarihi", "Bitiş tarihi", "Otomatik yenileme", "Notlar"];

    private static readonly string[] TargetKeys = ["name", "publisher", "productVersion", "licenseType", "supplier", "seats", "unitCost", "currency", "purchaseDate", "expirationDate", "autoRenew", "notes"];

    public static object[] MappingTargets() => TargetKeys.Select((key, index) => new { key, label = Canonical[index] })
        .Append(new { key = "ignore", label = "Bu sütunu yoksay" }).Cast<object>().ToArray();

    public static bool ValidateExplicitMappings(IReadOnlyDictionary<string, string>? mappings, out string error)
    {
        error = "";
        if (mappings is null || mappings.Count is < 1 or > 20) { error = "Bir eşleme profili 1–20 sütun eşlemesi içermelidir."; return false; }
        var sources = new HashSet<string>(StringComparer.Ordinal); var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (source, target) in mappings)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Trim().Length > 160 || !sources.Add(NormalizeHeader(source)))
            { error = "Kaynak sütun adları boş, çok uzun veya yinelenen olamaz."; return false; }
            if (target != "ignore" && !TargetKeys.Contains(target, StringComparer.Ordinal)) { error = $"“{source}” sütununun hedef alanı geçersiz."; return false; }
            if (target != "ignore" && !targets.Add(target)) { error = "Aynı lisans alanına birden fazla sütun eşlenemez."; return false; }
        }
        return true;
    }

    public static List<List<string>> Normalize(List<List<string>> records, out string[] unsupportedColumns,
        IReadOnlyDictionary<string, string>? explicitMappings = null)
    {
        unsupportedColumns = [];
        if (records.Count == 0) return records;
        var indexes = Enumerable.Repeat(-1, Canonical.Length).ToArray();
        var matched = new HashSet<int>();
        for (var source = 0; source < records[0].Count; source++)
        {
            if (explicitMappings?.Keys.Any(key => NormalizeHeader(key) == NormalizeHeader(records[0][source])) == true) continue;
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
        var sourceRows = records.Skip(1).ToArray();
        foreach (var (sourceName, targetKey) in explicitMappings ?? new Dictionary<string, string>())
        {
            var candidates = records[0].Select((header, index) => (header, index)).Where(item => NormalizeHeader(item.header) == NormalizeHeader(sourceName)).Select(item => item.index).ToArray();
            if (candidates.Length == 0) continue;
            if (candidates.Length != 1) { unsupportedColumns = [sourceName]; return records; }
            var sourceIndex = candidates[0];
            if (matched.Contains(sourceIndex)) { unsupportedColumns = [sourceName]; return records; }
            if (targetKey == "ignore") { matched.Add(sourceIndex); continue; }
            var targetIndex = Array.IndexOf(TargetKeys, targetKey);
            if (targetIndex < 0 || indexes[targetIndex] >= 0) { unsupportedColumns = [sourceName]; return records; }
            indexes[targetIndex] = sourceIndex; matched.Add(sourceIndex);
        }
        if (indexes[0] < 0 || indexes[3] < 0 || indexes[5] < 0)
        {
            unsupportedColumns = records[0].Select((name, index) => (name, index)).Where(x => !matched.Contains(x.index))
                .Select(x => x.name.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
            if (unsupportedColumns.Length == 0 && explicitMappings is { Count: > 0 }) unsupportedColumns = explicitMappings.Keys.Take(20).ToArray();
            return records;
        }
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
            output[3] = NormalizeType(output[3]);
            output[6] = NormalizeDecimal(output[6]);
            output[7] = string.IsNullOrWhiteSpace(output[7]) ? "TRY" : output[7];
            output[8] = NormalizeDate(output[8]);
            output[9] = NormalizeDate(output[9]);
            if (indexes[10] < 0) output[10] = "false";
            else output[10] = NormalizeBoolean(output[10]);
            outputRows.Add(output);
        }
        return outputRows;
    }

    private static string NormalizeHeader(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string NormalizeType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "subscription" or "subscription license" or "abonelik" => "Subscription",
        "perpetual" or "perpetual license" or "süresiz" or "suresiz" => "Perpetual",
        "device" or "device license" or "cihaz" => "Device",
        "site" or "site license" or "kurum" => "Site",
        "trial" or "deneme" => "Trial",
        "other" or "diğer" or "diger" => "Other",
        _ => value.Trim()
    };

    private static string NormalizeBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "true" or "yes" or "y" or "evet" => "true",
        "false" or "no" or "n" or "hayır" or "hayir" or "" => "false",
        _ => value.Trim()
    };

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
