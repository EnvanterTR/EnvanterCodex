using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace EnvanterTR.Web.Data;

public sealed class AssetCustomFieldProtection(IDataProtectionProvider provider)
{
    private const string CiphertextPrefix = "enc:v1:";
    private const int MaximumJsonLength = 16_000;

    public (string? Json, string? Error) Protect(string? json, string? existingProtectedJson,
        Guid companyId, IReadOnlyCollection<AssetCustomFieldDefinition> fields)
    {
        Dictionary<string, string> values;
        Dictionary<string, string> previous;
        try
        {
            values = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "{}") ?? new(StringComparer.Ordinal);
            previous = JsonSerializer.Deserialize<Dictionary<string, string>>(existingProtectedJson ?? "{}") ?? new(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return (null, "Özel alan verisi korunamadığı için kayıt yapılmadı.");
        }

        foreach (var field in fields.Where(x => x.FieldType == "sensitive"))
        {
            if (!values.TryGetValue(field.Key, out var value) || string.IsNullOrEmpty(value)) continue;
            if (previous.TryGetValue(field.Key, out var oldCiphertext) && oldCiphertext.StartsWith(CiphertextPrefix, StringComparison.Ordinal))
            {
                var oldValue = UnprotectValue(oldCiphertext, companyId, field);
                if (string.Equals(value, oldValue, StringComparison.Ordinal))
                {
                    values[field.Key] = oldCiphertext;
                    continue;
                }
            }
            values[field.Key] = CiphertextPrefix + CreateProtector(companyId, field).Protect(value);
        }

        var serialized = JsonSerializer.Serialize(values);
        return serialized.Length > MaximumJsonLength
            ? (null, "Şifreleme sonrası varlık özel alanlarının toplam boyutu 16.000 karakteri aşıyor.")
            : (serialized, null);
    }

    public Dictionary<string, string> Read(string? json, IReadOnlyCollection<AssetCustomFieldDefinition> fields,
        Guid companyId, bool revealSensitive)
    {
        Dictionary<string, string> values;
        try { values = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? "{}") ?? new(StringComparer.Ordinal); }
        catch (JsonException) { throw new CryptographicException("Varlık özel alan verisi okunamadı."); }

        foreach (var field in fields.Where(x => x.FieldType == "sensitive"))
        {
            if (!values.TryGetValue(field.Key, out var value)) continue;
            if (!revealSensitive) { values.Remove(field.Key); continue; }
            values[field.Key] = UnprotectValue(value, companyId, field);
        }
        return values;
    }

    private string UnprotectValue(string value, Guid companyId, AssetCustomFieldDefinition field)
    {
        if (!value.StartsWith(CiphertextPrefix, StringComparison.Ordinal))
            throw new CryptographicException("Hassas özel alan beklenen şifreli biçimde değil; açık metin değeri kullanıma sunulmadı.");
        try { return CreateProtector(companyId, field).Unprotect(value[CiphertextPrefix.Length..]); }
        catch (CryptographicException) { throw new CryptographicException("Hassas özel alan çözülemedi. Data Protection anahtarlarını ve yedek kapsamını doğrulayın."); }
    }

    private IDataProtector CreateProtector(Guid companyId, AssetCustomFieldDefinition field) =>
        provider.CreateProtector("EnvanterTR.AssetCustomField.v1", companyId.ToString("N"), field.Id.ToString("N"));
}
