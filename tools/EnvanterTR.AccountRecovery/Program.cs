using System.Security.Cryptography;
using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Length != 1 || (args[0] != "reset-password" && args[0] != "set-password" && args[0] != "unlock"))
{
    Console.Error.WriteLine("Kullanım: dotnet EnvanterTR.AccountRecovery.dll <reset-password|set-password|unlock>");
    return 2;
}
var resetPassword = args[0] == "reset-password";
var setPassword = args[0] == "set-password";
var changesPassword = resetPassword || setPassword;

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.Error.WriteLine("Kurtarma yalnızca sunucuya bağlı etkileşimli bir konsoldan çalıştırılabilir.");
    return 2;
}

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = Directory.GetCurrentDirectory()
    });
    builder.Logging.ClearProviders();
    var connectionString = builder.Configuration.GetConnectionString("EnvanterTR");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Uygulamanın veritabanı bağlantısı yapılandırılmamış.");

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddIdentityCore<EnvanterUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>();

    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.Write("Kurtarılacak hesabın e-posta adresi: ");
    var email = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
    {
        Console.Error.WriteLine("Geçerli bir e-posta adresi girilmedi.");
        return 2;
    }

    var user = await userManager.FindByEmailAsync(email);
    if (user is null || !user.EmailConfirmed)
    {
        Console.Error.WriteLine("Onaylı hesap bulunamadı; hiçbir değişiklik yapılmadı.");
        return 1;
    }

    var companyIds = await db.CompanyMemberships.AsNoTracking()
        .Where(membership => membership.UserId == user.Id)
        .Select(membership => membership.CompanyId)
        .Distinct()
        .ToListAsync();
    var isPlatformAdministrator = await db.PlatformAdministrators.AsNoTracking()
        .AnyAsync(administrator => administrator.UserId == user.Id);
    if (companyIds.Count == 0 && !isPlatformAdministrator)
    {
        Console.Error.WriteLine("Şirket veya platform erişimi bulunmayan hesap kurtarılamaz; hiçbir değişiklik yapılmadı.");
        return 1;
    }

    var lockedUntil = await userManager.GetLockoutEndDateAsync(user);
    Console.WriteLine($"Hesap bulundu · şirket üyeliği: {companyIds.Count} · platform yöneticisi: {(isPlatformAdministrator ? "evet" : "hayır")} · kilit: {(lockedUntil > DateTimeOffset.UtcNow ? "etkin" : "yok")}");
    Console.Write("Devam etmek için e-posta adresini aynen tekrar yazın: ");
    if (!string.Equals(Console.ReadLine()?.Trim(), email, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Onay eşleşmedi; hiçbir değişiklik yapılmadı.");
        return 2;
    }

    string? temporaryPassword = null;
    await using var transaction = await db.Database.BeginTransactionAsync();
    if (changesPassword)
    {
        temporaryPassword = resetPassword
            ? GenerateTemporaryPassword()
            : ReadAndConfirmPassword();
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, temporaryPassword);
            if (!validation.Succeeded)
                throw new InvalidOperationException("Üretilen parola sistem politikasını karşılamadı; yeniden deneyin.");
        }

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException("Mevcut parola güvenli biçimde kaldırılamadı.");
        }

        var addResult = await userManager.AddPasswordAsync(user, temporaryPassword);
        if (!addResult.Succeeded)
            throw new InvalidOperationException("Yeni parola Identity doğrulamasından geçmedi.");
    }

    await userManager.ResetAccessFailedCountAsync(user);
    await userManager.SetLockoutEndDateAsync(user, null);
    if (changesPassword)
        await userManager.UpdateSecurityStampAsync(user);

    foreach (var companyId in companyIds)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            CompanyId = companyId,
            ActorUserId = null,
            TargetType = "Account",
            TargetId = user.Id,
            Action = changesPassword ? "LocalConsolePasswordRecovery" : "LocalConsoleUnlock",
            Details = changesPassword
                ? "Yerel sunucu operatörü konsoldan parolayı yeniledi; başarısız giriş sayacı sıfırlandı ve diğer oturumlar geçersiz kılındı. Uygulama operatör kimliğini doğrulayamaz."
                : "Yerel sunucu operatörü konsoldan hesap kilidini açtı ve başarısız giriş sayacını sıfırladı. Parola değiştirilmedi. Uygulama operatör kimliğini doğrulayamaz."
        });
    }
    if (isPlatformAdministrator)
    {
        db.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = user.Id,
            CompanyId = companyIds.Count == 1 ? companyIds[0] : null,
            TargetType = "Account",
            TargetId = user.Id,
            Action = changesPassword ? "LocalConsolePasswordRecovery" : "LocalConsoleUnlock",
            Details = changesPassword
                ? "Yerel sunucu operatörü konsoldan parolayı yeniledi; başarısız giriş sayacı sıfırlandı ve diğer oturumlar geçersiz kılındı. Uygulama operatör kimliğini doğrulayamaz."
                : "Yerel sunucu operatörü konsoldan hesap kilidini açtı ve başarısız giriş sayacını sıfırladı. Parola değiştirilmedi. Uygulama operatör kimliğini doğrulayamaz."
        });
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    if (resetPassword)
    {
        Console.WriteLine();
        Console.WriteLine("Parola yenilendi. Bu geçici parolayı şimdi saklayın; araç tekrar gösteremez.");
        Console.WriteLine(temporaryPassword);
        Console.WriteLine("Giriş yaptıktan sonra Hesap ayarları > Parolayı değiştir bölümünden kendi parolanızı belirleyin.");
    }
    else if (setPassword)
    {
        Console.WriteLine("Parola yenilendi. Yeni parola ekrana yazdırılmadı; giriş yapabilirsiniz.");
    }
    else
    {
        Console.WriteLine("Hesap kilidi açıldı. Mevcut parola değiştirilmedi.");
    }
    return 0;
}
catch (Exception exception)
{
    // Database errors may contain connection details. Never print full exception text here.
    Console.Error.WriteLine($"Parola kurtarma tamamlanamadı ({exception.GetType().Name}); değişikliğin tamamlandığı doğrulanmadı.");
    return 1;
}

static string GenerateTemporaryPassword()
{
    const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lowercase = "abcdefghijkmnpqrstuvwxyz";
    const string digits = "23456789";
    const string symbols = "!#$%&*+-=?@_";
    const string alphabet = uppercase + lowercase + digits + symbols;
    const int length = 28;

    var characters = new char[length];
    characters[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
    characters[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
    characters[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
    characters[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
    for (var index = 4; index < characters.Length; index++)
        characters[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
    for (var index = characters.Length - 1; index > 0; index--)
    {
        var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
        (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
    }

    var password = new string(characters);
    Array.Clear(characters);
    return password;
}

static string ReadAndConfirmPassword()
{
    var password = ReadHidden("Yeni parola: ");
    var confirmation = ReadHidden("Yeni parola (tekrar): ");
    if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        throw new InvalidOperationException("Parola onayı eşleşmedi; hiçbir değişiklik yapılmadı.");
    return password;
}

static string ReadHidden(string prompt)
{
    Console.Write(prompt);
    var characters = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (characters.Count > 0)
                characters.RemoveAt(characters.Count - 1);
            continue;
        }
        if (!char.IsControl(key.KeyChar))
            characters.Add(key.KeyChar);
    }
    return new string(characters.ToArray());
}
