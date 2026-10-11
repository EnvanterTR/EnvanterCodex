using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;

namespace EnvanterTR.Setup;

internal static class Program
{
    private const string PayloadResourceName = "EnvanterTR.SetupPayload.zip";
    private const int MaximumEntries = 5_000;
    private const long MaximumExpandedBytes = 1L * 1024 * 1024 * 1024;

    private static readonly HashSet<string> RequiredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "EnvanterTR.Web.exe",
        "setup.ps1",
        "preflight.ps1",
        "install-postgresql.ps1",
        "prepare-postgresql.ps1",
        "install.ps1",
        "migrate.ps1",
        "ensure-localhost-certificate.ps1"
    };

    private static readonly HashSet<string> ForbiddenNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env", ".env.production", "appsettings.production.json", "server.env", "backup.pass"
    };

    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        var verifyOnly = args.Any(argument => argument.Equals("--verify", StringComparison.OrdinalIgnoreCase));
        var forcedServerMode = args.Any(argument => argument.Equals("--server", StringComparison.OrdinalIgnoreCase));
        var forcedPersonalMode = args.Any(argument => argument.Equals("--personal", StringComparison.OrdinalIgnoreCase));
        if (args.Any(argument => !argument.Equals("--verify", StringComparison.OrdinalIgnoreCase)
            && !argument.Equals("--server", StringComparison.OrdinalIgnoreCase)
            && !argument.Equals("--personal", StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine("Kullanım: Setup.exe [--verify | --personal | --server]");
            return 2;
        }

        if (forcedServerMode && forcedPersonalMode)
        {
            Console.Error.WriteLine("--personal ve --server aynı anda kullanılamaz.");
            return 2;
        }

        var mode = verifyOnly ? InstallMode.Verify : ChooseInstallMode(forcedPersonalMode, forcedServerMode);
        if (mode == InstallMode.Cancelled)
        {
            Console.WriteLine("Kurulum başlatılmadı.");
            return 0;
        }

        string? extractionRoot = null;
        try
        {
            using var payload = OpenPayload();
            if (payload is null)
            {
                throw new InvalidDataException("Kurulum paketi bulunamadı. Setup.exe dosyası eksik veya değiştirilmiş olabilir.");
            }

            using var archive = new ZipArchive(payload, ZipArchiveMode.Read, leaveOpen: true);
            var plan = ValidateArchive(archive);
            VerifyRequiredFiles(plan);
            if (mode == InstallMode.Verify)
            {
                VerifyArchiveContents(plan);
                Console.WriteLine($"Kurulum paketi doğrulandı: {plan.Count(entry => !entry.IsDirectory)} dosya, {plan.Sum(entry => entry.Entry.Length):N0} bayt açılmış içerik.");
                return 0;
            }

            extractionRoot = Path.Combine(Path.GetTempPath(), $"EnvanterTR-Setup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractionRoot);
            ExtractArchive(plan, extractionRoot);

            var setupScript = Path.Combine(extractionRoot, "setup.ps1");
            var shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(setupScript) || !File.Exists(shell))
            {
                throw new FileNotFoundException("Güvenli kurulum başlatıcısı veya Windows PowerShell bulunamadı.");
            }

            if (mode == InstallMode.Prompt)
            {
                mode = PromptInstallMode();
                if (mode == InstallMode.Cancelled)
                {
                    return 0;
                }
            }

            Console.WriteLine();
            Console.WriteLine("EnvanterTR kurulum sihirbazı başlatılıyor. Windows yönetici izni isteyecek.");
            Console.WriteLine("Kurulum penceresini son adıma kadar açık tutun.");

            var arguments = new StringBuilder("-NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -File ");
            arguments.Append(QuoteWindowsArgument(setupScript));
            if (mode == InstallMode.Personal)
            {
                arguments.Append(" -PersonalSinglePc");
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = shell,
                Arguments = arguments.ToString(),
                WorkingDirectory = extractionRoot,
                UseShellExecute = true,
                Verb = "runas"
            });
            if (process is null)
            {
                throw new InvalidOperationException("Windows PowerShell başlatılamadı.");
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            Console.Error.WriteLine("Yönetici izni verilmedi; kurulum iptal edildi ve hiçbir kurulum adımı çalıştırılmadı.");
            return 3;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Kurulum başlatılamadı: {exception.Message}");
            return 1;
        }
        finally
        {
            if (extractionRoot is not null)
            {
                try
                {
                    Directory.Delete(extractionRoot, recursive: true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"Geçici kurulum dosyaları kaldırılamadı. Yönetici hesabıyla silebilirsiniz: {extractionRoot}");
                }
            }
        }
    }

    private static InstallMode ChooseInstallMode(bool forcedPersonalMode, bool forcedServerMode)
    {
        if (forcedPersonalMode)
        {
            return InstallMode.Personal;
        }

        if (forcedServerMode)
        {
            return InstallMode.Server;
        }

        return InstallMode.Prompt;
    }

    private static InstallMode PromptInstallMode()
    {
        Console.WriteLine("EnvanterTR kurulumu");
        Console.WriteLine("1. Windows 11 — bu bilgisayarda kişisel kullanım");
        Console.WriteLine("2. Windows Server — ofis ekibi için sunucu");
        Console.WriteLine("Q. İptal");
        Console.Write("Kurulum türünü seçin [1/2/Q]: ");

        return Console.ReadLine()?.Trim().ToUpperInvariant() switch
        {
            "1" => InstallMode.Personal,
            "2" => InstallMode.Server,
            _ => InstallMode.Cancelled
        };
    }

    private static Stream? OpenPayload()
    {
        return Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName);
    }

    private static List<PlannedEntry> ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumEntries)
        {
            throw new InvalidDataException("Kurulum arşivindeki dosya sayısı geçersiz.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var plan = new List<PlannedEntry>(archive.Entries.Count);
        long expandedBytes = 0;

        foreach (var entry in archive.Entries)
        {
            var isDirectory = entry.FullName.EndsWith('/');
            var relativePath = ValidateRelativePath(entry.FullName, isDirectory);
            var normalizedName = relativePath.Replace('\\', '/');
            if (!names.Add(normalizedName))
            {
                throw new InvalidDataException("Kurulum arşivinde yinelenen dosya yolu var.");
            }

            if (IsSymbolicLink(entry))
            {
                throw new InvalidDataException("Kurulum arşivinde sembolik bağlantı kabul edilmez.");
            }

            if (!isDirectory)
            {
                if (IsForbiddenFile(relativePath))
                {
                    throw new InvalidDataException("Kurulum arşivi özel yapılandırma veya sır içeren dosya barındırıyor.");
                }

                if (entry.Length < 0 || entry.Length > MaximumExpandedBytes)
                {
                    throw new InvalidDataException("Kurulum arşivinde izin verilen boyutu aşan dosya var.");
                }

                expandedBytes = checked(expandedBytes + entry.Length);
                if (expandedBytes > MaximumExpandedBytes)
                {
                    throw new InvalidDataException("Kurulum arşivinin açılmış toplam boyutu sınırı aşıyor.");
                }
            }

            plan.Add(new PlannedEntry(entry, relativePath, isDirectory));
        }

        return plan;
    }

    private static string ValidateRelativePath(string path, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1_024 || path.Contains(':'))
        {
            throw new InvalidDataException("Kurulum arşivinde geçersiz dosya yolu var.");
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith('/') || Path.IsPathRooted(normalized))
        {
            throw new InvalidDataException("Kurulum arşivinde kök dizin yolu kabul edilmez.");
        }

        if (isDirectory)
        {
            normalized = normalized.TrimEnd('/');
        }

        var segments = normalized.Split('/');
        if (segments.Length == 0 || segments.Any(segment => string.IsNullOrWhiteSpace(segment)
            || segment is "." or ".."
            || segment.EndsWith(' ') || segment.EndsWith('.')
            || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || IsReservedDeviceName(segment)))
        {
            throw new InvalidDataException("Kurulum arşivinde güvenli olmayan dosya yolu var.");
        }

        return Path.Combine(segments);
    }

    private static bool IsReservedDeviceName(string segment)
    {
        var baseName = segment.Split('.')[0];
        return baseName.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || (baseName.Length == 4
                && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && baseName[3] is >= '1' and <= '9');
    }

    private static bool IsForbiddenFile(string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        return ForbiddenNames.Contains(fileName)
            || fileName.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".dpapi", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSymbolicLink(ZipArchiveEntry entry)
    {
        var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
        return unixFileType == 0xA000;
    }

    private static void VerifyRequiredFiles(List<PlannedEntry> plan)
    {
        var fileNames = plan.Where(entry => !entry.IsDirectory)
            .Select(entry => entry.RelativePath.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = RequiredFiles.Where(required => !fileNames.Contains(required)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException($"Kurulum arşivinde gerekli dosya eksik: {string.Join(", ", missing)}");
        }
    }

    private static void VerifyArchiveContents(List<PlannedEntry> plan)
    {
        foreach (var item in plan.Where(entry => !entry.IsDirectory))
        {
            using var input = item.Entry.Open();
            DrainAndCheckLength(input, Stream.Null, item.Entry.Length);
        }
    }

    private static void ExtractArchive(List<PlannedEntry> plan, string extractionRoot)
    {
        var root = Path.GetFullPath(extractionRoot) + Path.DirectorySeparatorChar;
        foreach (var item in plan)
        {
            var destination = Path.GetFullPath(Path.Combine(extractionRoot, item.RelativePath));
            if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Kurulum arşivi geçici dizinin dışına yazmaya çalışıyor.");
            }

            if (item.IsDirectory)
            {
                Directory.CreateDirectory(destination);
                continue;
            }

            var parent = Path.GetDirectoryName(destination) ?? throw new InvalidDataException("Kurulum dosyasının üst dizini geçersiz.");
            Directory.CreateDirectory(parent);
            using var input = item.Entry.Open();
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            DrainAndCheckLength(input, output, item.Entry.Length);
        }
    }

    private static void DrainAndCheckLength(Stream input, Stream output, long expectedLength)
    {
        var buffer = new byte[64 * 1024];
        long totalRead = 0;
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalRead = checked(totalRead + read);
            if (totalRead > expectedLength || totalRead > MaximumExpandedBytes)
            {
                throw new InvalidDataException("Kurulum dosyası beyan edilen boyut sınırını aşıyor.");
            }

            output.Write(buffer, 0, read);
        }

        if (totalRead != expectedLength)
        {
            throw new InvalidDataException("Kurulum arşivindeki dosyanın içeriği eksik veya bozuk.");
        }
    }

    private static string QuoteWindowsArgument(string value)
    {
        var builder = new StringBuilder().Append('"');
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', checked(backslashes * 2 + 1)).Append(character);
                backslashes = 0;
                continue;
            }

            builder.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        builder.Append('\\', checked(backslashes * 2)).Append('"');
        return builder.ToString();
    }

    private enum InstallMode
    {
        Verify,
        Prompt,
        Personal,
        Server,
        Cancelled
    }

    private sealed record PlannedEntry(ZipArchiveEntry Entry, string RelativePath, bool IsDirectory);
}
