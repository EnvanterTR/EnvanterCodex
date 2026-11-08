using System.Text.Json;
using EnvanterTR.Web.Data;
using Xunit;

namespace EnvanterTR.SecurityAcceptance;

public sealed class AssetCustomFieldTypesTests
{
    [Fact]
    public void Text_select_checkbox_and_date_values_are_normalized_with_type_validation()
    {
        var companyId = Guid.NewGuid();
        var fields = new[]
        {
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "reference", Name = "Reference", FieldType = "text", IsActive = true },
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "class", Name = "Class", FieldType = "select", OptionsJson = JsonSerializer.Serialize(new[] { "Laptop", "Desktop" }), IsActive = true },
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "encrypted", Name = "Encrypted", FieldType = "checkbox", IsActive = true },
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "purchase_date", Name = "Purchase date", FieldType = "date", IsActive = true }
        };

        var normalized = AssetCustomFieldEndpoints.NormalizeValues(fields,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["reference"] = "  QA-42  ", ["class"] = "Laptop", ["encrypted"] = "false", ["purchase_date"] = "2026-10-15"
            });

        Assert.Null(normalized.Error);
        using var payload = JsonDocument.Parse(normalized.Json!);
        Assert.Equal("QA-42", payload.RootElement.GetProperty("reference").GetString());
        Assert.Equal("Laptop", payload.RootElement.GetProperty("class").GetString());
        Assert.Equal("false", payload.RootElement.GetProperty("encrypted").GetString());
        Assert.Equal("2026-10-15", payload.RootElement.GetProperty("purchase_date").GetString());
    }

    [Fact]
    public void Invalid_select_checkbox_and_date_values_are_rejected()
    {
        var companyId = Guid.NewGuid();
        var fields = new[]
        {
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "class", Name = "Class", FieldType = "select", OptionsJson = JsonSerializer.Serialize(new[] { "Laptop", "Desktop" }), IsActive = true },
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "encrypted", Name = "Encrypted", FieldType = "checkbox", IsActive = true },
            new AssetCustomFieldDefinition { CompanyId = companyId, Key = "purchase_date", Name = "Purchase date", FieldType = "date", IsActive = true }
        };

        foreach (var invalidValue in new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["class"] = "Server", ["encrypted"] = "sometimes", ["purchase_date"] = "15/10/2026"
        })
        {
            var result = AssetCustomFieldEndpoints.NormalizeValues(fields,
                new Dictionary<string, string>(StringComparer.Ordinal) { [invalidValue.Key] = invalidValue.Value });
            Assert.Null(result.Json);
            Assert.Contains(invalidValue.Key switch
            {
                "class" => "Class",
                "encrypted" => "Encrypted",
                _ => "Purchase date"
            }, result.Error, StringComparison.Ordinal);
        }
    }
}
