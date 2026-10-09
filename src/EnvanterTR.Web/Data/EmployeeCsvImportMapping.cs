namespace EnvanterTR.Web.Data;

internal static class EmployeeCsvImportMapping
{
    private static readonly string[][] Aliases =
    [
        ["Personel No", "Personel Numarası", "Çalışan No", "Employee Number", "Employee No", "Employee ID", "Staff ID", "Personnel Number", "Табельный номер", "Табельний номер", "Број на вработен"],
        ["Ad Soyad", "Adı Soyadı", "Ad", "Çalışan Adı", "Name", "Full Name", "Employee Name", "Display Name", "Имя и фамилия", "Ім’я та прізвище", "Име и презиме"],
        ["E-posta", "Eposta", "E-mail", "Email", "Email Address", "Электронная почта", "Електронна пошта"],
        ["Departman", "Bölüm", "Department", "Division", "Отдел", "Відділ", "Оддел"],
        ["Görev Unvanı", "Görev", "Unvan", "Job Title", "Title", "Position", "Должность", "Посада", "Работно место"],
        ["Telefon", "Phone", "Phone Number", "Telephone"],
        ["Konum", "Lokasyon", "Location", "Office", "Office Location", "Местоположение", "Місце", "Локација"],
        ["İşe Başlama Tarihi", "İşe Giriş Tarihi", "Başlangıç Tarihi", "Start Date", "Hire Date", "Date Started", "Дата начала работы", "Дата початку роботи", "Датум на започнување"],
        ["Yönetici", "Manager", "Manager Name", "Руководитель", "Керівник", "Менаџер"],
        ["Durum", "Active", "Status", "Employment Status", "Статус"],
        ["Kayıt Tarihi", "Created At", "Creation Date", "Дата создания", "Дата створення", "Датум на создавање"],
        ["Yönetici Personel No", "Yönetici No", "Manager Employee Number", "Manager Employee No", "Manager ID", "Табельный номер руководителя", "Табельний номер керівника", "Број на вработен на менаџер"]
    ];

    private static readonly string[] Headers =
        ["Personel No", "Ad Soyad", "E-posta", "Departman", "Görev Unvanı", "Telefon", "Konum", "İşe Başlama Tarihi",
         "Yönetici", "Durum", "Kayıt Tarihi", "Yönetici Personel No"];

    private static readonly string[] Keys =
        ["employeeNumber", "displayName", "email", "department", "jobTitle", "phone", "location", "startedOn",
         "managerName", "active", "createdAt", "managerEmployeeNumber"];

    private static readonly string[][] LocalizedHeaders =
    [
        ["Personel No", "Ad Soyad", "E-posta", "Departman", "Görev Unvanı", "Telefon", "Konum", "İşe Başlama Tarihi", "Yönetici", "Durum", "Kayıt Tarihi", "Yönetici Personel No"],
        ["Employee No", "Full Name", "Email", "Department", "Job Title", "Phone", "Location", "Start Date", "Manager", "Status", "Created At", "Manager Employee No"],
        ["Табельный номер", "Имя и фамилия", "Электронная почта", "Отдел", "Должность", "Телефон", "Местоположение", "Дата начала работы", "Руководитель", "Статус", "Дата создания", "Табельный номер руководителя"],
        ["Табельний номер", "Ім’я та прізвище", "Електронна пошта", "Відділ", "Посада", "Телефон", "Місце", "Дата початку роботи", "Керівник", "Статус", "Дата створення", "Табельний номер керівника"],
        ["Број на вработен", "Име и презиме", "Е-пошта", "Оддел", "Работно место", "Телефон", "Локација", "Датум на започнување", "Менаџер", "Статус", "Датум на создавање", "Број на вработен на менаџер"]
    ];

    public static string[] ExportHeaders(string language) =>
        LocalizedHeaders[language switch { "en" => 1, "ru" => 2, "uk" => 3, "mk" => 4, _ => 0 }];

    public static string ExportStatus(bool isActive, string language) => language switch
    {
        "en" => isActive ? "Active" : "Inactive",
        "ru" => isActive ? "Активен" : "Неактивен",
        "uk" => isActive ? "Активний" : "Неактивний",
        "mk" => isActive ? "Активен" : "Неактивен",
        _ => isActive ? "Etkin" : "Pasif"
    };

    public static object[] MappingTargets() => Keys.Select((key, index) => new { key, label = Headers[index] })
        .Append(new { key = "ignore", label = "Bu sütunu yoksay" }).Cast<object>().ToArray();

    public static bool ValidateExplicitMappings(IReadOnlyDictionary<string, string>? mappings, out string error)
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
            if (target != "ignore" && !Keys.Contains(target, StringComparer.Ordinal))
            {
                error = $"“{source}” sütununun hedef alanı geçersiz.";
                return false;
            }
            if (target != "ignore" && !targets.Add(target))
            {
                error = "Aynı çalışan alanına birden fazla sütun eşlenemez.";
                return false;
            }
        }
        return true;
    }

    public static List<List<string>> Normalize(List<List<string>> rows, IReadOnlyDictionary<string, string>? explicitMappings,
        out string[] unmappedColumns)
    {
        unmappedColumns = [];
        if (rows.Count == 0) return rows;
        var indexes = Enumerable.Repeat(-1, Headers.Length).ToArray();
        var matched = new HashSet<int>();

        for (var source = 0; source < rows[0].Count; source++)
        {
            var normalized = NormalizeHeader(rows[0][source]);
            if (explicitMappings?.Keys.Any(key => NormalizeHeader(key) == normalized) == true) continue;
            for (var target = 0; target < Aliases.Length; target++)
            {
                if (!Aliases[target].Any(alias => NormalizeHeader(alias) == normalized)) continue;
                if (indexes[target] >= 0) { unmappedColumns = [rows[0][source]]; return rows; }
                indexes[target] = source;
                matched.Add(source);
                break;
            }
        }

        foreach (var (sourceName, targetKey) in explicitMappings ?? new Dictionary<string, string>())
        {
            var candidates = rows[0].Select((header, index) => (header, index))
                .Where(item => NormalizeHeader(item.header) == NormalizeHeader(sourceName)).Select(item => item.index).ToArray();
            if (candidates.Length == 0) continue; // Profiles may contain optional columns absent from a particular export.
            if (candidates.Length != 1) { unmappedColumns = [sourceName]; return rows; }
            var sourceIndex = candidates[0];
            if (matched.Contains(sourceIndex)) { unmappedColumns = [sourceName]; return rows; }
            if (targetKey == "ignore") { matched.Add(sourceIndex); continue; }
            var targetIndex = Array.IndexOf(Keys, targetKey);
            if (targetIndex < 0 || indexes[targetIndex] >= 0) { unmappedColumns = [sourceName]; return rows; }
            indexes[targetIndex] = sourceIndex;
            matched.Add(sourceIndex);
        }

        var dataRows = rows.Skip(1).ToArray();
        if (indexes[0] < 0 || indexes[1] < 0)
        {
            unmappedColumns = rows[0].Select((name, index) => (name, index)).Where(x => !matched.Contains(x.index))
                .Select(x => x.name.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();
            if (unmappedColumns.Length == 0 && explicitMappings is { Count: > 0 }) unmappedColumns = explicitMappings.Keys.Take(20).ToArray();
            return rows;
        }
        unmappedColumns = rows[0].Select((name, index) => (name, index))
            .Where(x => !matched.Contains(x.index) && dataRows.Any(row => x.index < row.Count && !string.IsNullOrWhiteSpace(row[x.index])))
            .Select(x => x.name.Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

        var hasExportColumns = indexes.Skip(8).Any(index => index >= 0);
        var width = hasExportColumns ? Headers.Length : 8;
        var normalizedRows = new List<List<string>>(rows.Count) { Headers.Take(width).ToList() };
        foreach (var row in dataRows)
        {
            if (row.Count != rows[0].Count) { normalizedRows.Add(row); continue; }
            var normalized = Enumerable.Range(0, width).Select(index => indexes[index] >= 0 ? row[indexes[index]].Trim() : "").ToList();
            if (width > 8)
            {
                normalized[9] = NormalizeStatus(normalized[9]);
                normalized[10] = NormalizeDate(normalized[10]);
            }
            normalized[7] = NormalizeDate(normalized[7]);
            normalizedRows.Add(normalized);
        }
        return normalizedRows;
    }

    private static string NormalizeStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "" or "active" or "enabled" or "true" or "1" or "etkin" or "aktif" or "активен" or "активна" or "активно" or "активний" => "Etkin",
        "inactive" or "disabled" or "false" or "0" or "pasif" or "неактивен" or "неактивна" or "неактивно" or "неактивний" => "Pasif",
        _ => value.Trim()
    };

    private static string NormalizeDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        string[] formats = ["yyyy-MM-dd", "M/d/yy", "MM/dd/yy", "M/d/yyyy", "MM/dd/yyyy", "d.M.yy", "dd.MM.yy", "d.M.yyyy", "dd.MM.yyyy"];
        return DateOnly.TryParseExact(value.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            : value.Trim();
    }

    private static string NormalizeHeader(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
