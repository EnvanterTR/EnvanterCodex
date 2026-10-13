using EnvanterTR.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EnvanterTR.Web;

/// <summary>
/// Interactive, local-development-only recovery for a platform administrator when SMTP is unavailable.
/// The password is read without echo, never accepted as an argument, and never written to logs.
/// </summary>
public static class LocalPlatformAdminRecovery
{
    public static async Task<int> RunAsync(IServiceProvider services, bool isDevelopment, string? connectionString)
    {
        if (!isDevelopment)
        {
            Console.Error.WriteLine("Yerel hesap kurtarma yalnızca Development ortamında kullanılabilir.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("Veritabanı bağlantısı yapılandırılmamış.");
            return 2;
        }

        try
        {
            var database = new NpgsqlConnectionStringBuilder(connectionString);
            if (database.Database != "envantertr" || (database.Host ?? string.Empty).Split(',', StringSplitOptions.TrimEntries)
                    .Any(host => host is not ("database" or "localhost" or "127.0.0.1" or "::1")))
            {
                Console.Error.WriteLine("Kurtarma yalnızca yerel EnvanterTR geliştirme veritabanında çalışır.");
                return 2;
            }
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Veritabanı bağlantı ayarı geçersiz.");
            return 2;
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Kurtarma komutunu etkileşimli, gizli parola girişi destekleyen bir terminalde çalıştırın.");
            return 2;
        }

        Console.Write("Platform yöneticisi e-posta adresi: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("E-posta adresi boş olamaz.");
            return 2;
        }

        var password = ReadSecret("Yeni parola (giriş görünmez): ");
        var confirmation = ReadSecret("Yeni parolayı tekrar girin: ");
        if (password.Length < 12 || !string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Parolalar eşleşmeli ve en az 12 karakter olmalıdır. Veritabanı değiştirilmedi.");
            return 2;
        }

        Console.Write("Bu yerel platform yöneticisi parolasını yenilemek için RESET yazın: ");
        if (!string.Equals(Console.ReadLine(), "RESET", StringComparison.Ordinal))
        {
            Console.WriteLine("İşlem iptal edildi. Veritabanı değiştirilmedi.");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<EnvanterUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is null || !await db.PlatformAdministrators.AsNoTracking().AnyAsync(x => x.UserId == user.Id))
        {
            Console.Error.WriteLine("Bu e-posta için platform yöneticisi hesabı bulunamadı. Veritabanı değiştirilmedi.");
            return 2;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var resetToken = await users.GeneratePasswordResetTokenAsync(user);
        var resetResult = await users.ResetPasswordAsync(user, resetToken, password);
        if (!resetResult.Succeeded)
        {
            var reasons = string.Join(", ", resetResult.Errors.Select(error => error.Code));
            Console.Error.WriteLine($"Parola Identity kurallarını karşılamadı ({reasons}). Veritabanı değiştirilmedi.");
            await transaction.RollbackAsync();
            return 2;
        }

        await users.ResetAccessFailedCountAsync(user);
        await users.SetLockoutEndDateAsync(user, null);
        var now = DateTimeOffset.UtcNow;
        var sessions = await db.UserSessions.Where(x => x.UserId == user.Id && x.RevokedAt == null).ToListAsync();
        foreach (var session in sessions) session.RevokedAt = now;
        db.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = user.Id,
            Action = "LocalConsolePasswordRecovery",
            TargetType = "PlatformAccount",
            TargetId = user.Id,
            Details = "Parola yerel Development konsolundan yenilendi; işlemi yapan kişi uygulama oturumuyla doğrulanmadı."
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine("Parola yenilendi. Açık oturumlar kapatıldı; yeni parolayı yalnızca giriş formunda kullanın.");
        return 0;
    }

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar)) characters.Add(key.KeyChar);
        }
        Console.WriteLine();
        var secret = new string(characters.ToArray());
        characters.Clear();
        return secret;
    }
}
