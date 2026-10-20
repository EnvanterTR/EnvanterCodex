using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnvanterTR.Web.Data;

public sealed record CompanyAssetStatusDefinition(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("color")] string Color);

public sealed record CompanyAssetStatusConfigurationDocument(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("labels")] Dictionary<string, string> Labels,
    [property: JsonPropertyName("customStatuses")] List<CompanyAssetStatusDefinition> CustomStatuses,
    [property: JsonPropertyName("transitions")] Dictionary<string, string[]> Transitions);

public static class AssetStatusConfiguration
{
    public const int MaximumCustomStatuses = 8;
    public const int MaximumStoredLength = 3000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly (string Status, string Label)[] BuiltInStatuses =
    [
        (AssetStatuses.Available, "Kullanılabilir"),
        (AssetStatuses.Deployed, "Atanmış"),
        (AssetStatuses.Pending, "Beklemede"),
        (AssetStatuses.Maintenance, "Bakımda"),
        (AssetStatuses.Unusable, "Kullanılamaz"),
        (AssetStatuses.Archived, "Arşiv")
    ];

    private static readonly string[] ManuallyManagedBuiltIns =
    [AssetStatuses.Available, AssetStatuses.Pending, AssetStatuses.Maintenance, AssetStatuses.Unusable];

    public static IReadOnlyList<(string Status, string Label)> BuiltIns => BuiltInStatuses;
    public static IReadOnlyList<string> ManualBuiltIns => ManuallyManagedBuiltIns;
    public static string DefaultColor(string status) => status switch
    {
        AssetStatuses.Available => "#20A779",
        AssetStatuses.Deployed => "#3478F6",
        AssetStatuses.Pending => "#D99A22",
        AssetStatuses.Maintenance => "#D97706",
        AssetStatuses.Unusable => "#D64E5A",
        AssetStatuses.Archived => "#68758A",
        _ => "#607D9B"
    };

    public static CompanyAssetStatusConfigurationDocument Parse(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("version", out _))
                {
                    var parsed = JsonSerializer.Deserialize<CompanyAssetStatusConfigurationDocument>(json, JsonOptions);
                    if (parsed is not null && parsed.Version == 1)
                        return Normalize(parsed);
                }

                // Before custom statuses were introduced this column stored only { status: label }.
                var legacyLabels = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                if (legacyLabels is not null)
                    return Normalize(new CompanyAssetStatusConfigurationDocument(1, legacyLabels, [], DefaultTransitions([])));
            }
            catch (JsonException) { }
        }

        return Normalize(new CompanyAssetStatusConfigurationDocument(1, new(StringComparer.Ordinal), [], DefaultTransitions([])));
    }

    public static CompanyAssetStatusConfigurationDocument Normalize(CompanyAssetStatusConfigurationDocument value)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var builtIn in BuiltInStatuses)
            if (value.Labels is not null && value.Labels.TryGetValue(builtIn.Status, out var label) && !string.IsNullOrWhiteSpace(label))
                labels[builtIn.Status] = label.Trim();

        var custom = (value.CustomStatuses ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Status) && !string.IsNullOrWhiteSpace(x.Label))
            .Select(x => new CompanyAssetStatusDefinition(x.Status.Trim(), x.Label.Trim(), NormalizeColor(x.Color)))
            .Take(MaximumCustomStatuses)
            .ToList();
        var validCodes = new HashSet<string>(BuiltInStatuses.Select(x => x.Status), StringComparer.Ordinal);
        validCodes.UnionWith(custom.Select(x => x.Status));
        var transitions = (value.Transitions ?? new Dictionary<string, string[]>(StringComparer.Ordinal))
            .Where(pair => validCodes.Contains(pair.Key))
            .ToDictionary(pair => pair.Key,
                pair => pair.Value.Where(validCodes.Contains).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        return new CompanyAssetStatusConfigurationDocument(1, labels, custom, transitions);
    }

    public static Dictionary<string, string> GetLabels(CompanyAssetStatusConfigurationDocument configuration) =>
        new(configuration.Labels, StringComparer.Ordinal);

    public static Dictionary<string, string[]> DefaultTransitions(IReadOnlyCollection<CompanyAssetStatusDefinition> customStatuses)
    {
        var manualStatuses = ManuallyManagedBuiltIns
            .Concat(customStatuses.Select(x => x.Status))
            .ToArray();
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var from in manualStatuses)
        {
            if (from == AssetStatuses.Archived)
            {
                result[from] = [];
                continue;
            }

            result[from] = manualStatuses.Where(to => to != from).Distinct(StringComparer.Ordinal).ToArray();
        }
        return result;
    }

    public static bool IsManualStatus(string? status, CompanyAssetStatusConfigurationDocument configuration) =>
        (status is not null && (ManuallyManagedBuiltIns.Contains(status, StringComparer.Ordinal) || status == AssetStatuses.Archived)) ||
        configuration.CustomStatuses.Any(x => string.Equals(x.Status, status, StringComparison.Ordinal));

    public static bool CanTransition(string? from, string? to, CompanyAssetStatusConfigurationDocument configuration)
    {
        // Archiving remains an explicit protected lifecycle action, not a tenant-editable matrix edge.
        if (to == AssetStatuses.Archived)
            return from != AssetStatuses.Archived && IsManualStatus(from, configuration);
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to)
            return from == to && from != AssetStatuses.Archived && !string.IsNullOrWhiteSpace(from);
        if (from == AssetStatuses.Archived) return false;
        if (!IsManualStatus(from, configuration) || !IsManualStatus(to, configuration)) return false;

        return configuration.Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to, StringComparer.Ordinal);
    }

    public static bool CanReachAvailable(CompanyAssetStatusConfigurationDocument configuration, string start)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(start);
        while (pending.TryPop(out var current))
        {
            if (current == AssetStatuses.Available) return true;
            if (!visited.Add(current) || !configuration.Transitions.TryGetValue(current, out var next)) continue;
            foreach (var status in next) pending.Push(status);
        }
        return false;
    }

    public static string Serialize(CompanyAssetStatusConfigurationDocument configuration) =>
        JsonSerializer.Serialize(Normalize(configuration), JsonOptions);

    private static string NormalizeColor(string? color) =>
        color is not null && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$")
            ? color.ToUpperInvariant()
            : "#607D9B";
}
