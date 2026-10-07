using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

const int HeaderLength = 44;
const int TagLength = 32;
const int SaltLength = 16;
const int IvLength = 16;
const int KeyLength = 64;
const int Iterations = 600_000;
var magic = Encoding.ASCII.GetBytes("EVTRBK01");

try
{
    if (args.Length < 2) return Usage();
    var command = args[0].ToLowerInvariant();
    switch (command)
    {
        case "encrypt" when args.Length is 3 or 4:
            await EncryptDirectoryAsync(args[1], args[2], await ReadPasswordAsync(args.Length == 4 ? args[3] : null, true));
            Console.WriteLine($"Şifreli yedek oluşturuldu: {Path.GetFullPath(args[2])}");
            return 0;
        case "verify" when args.Length is 2 or 3:
            await VerifyAsync(args[1], await ReadPasswordAsync(args.Length == 3 ? args[2] : null, false));
            Console.WriteLine("Şifreli yedek doğrulandı; HMAC ve ZIP içerikleri okunabilir.");
            return 0;
        case "decrypt" when args.Length is 3 or 4:
            await DecryptDirectoryAsync(args[1], args[2], await ReadPasswordAsync(args.Length == 4 ? args[3] : null, false));
            Console.WriteLine($"Yedek güvenli şekilde açıldı: {Path.GetFullPath(args[2])}");
            return 0;
        default:
            return Usage();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"İşlem tamamlanamadı: {ex.Message}");
    return 1;
}

int Usage()
{
    Console.Error.WriteLine("Kullanım:\n  EnvanterTR.Backup encrypt <yedek-klasörü> <çıktı.etbackup> [parola-dosyası]\n  EnvanterTR.Backup verify <yedek.etbackup> [parola-dosyası]\n  EnvanterTR.Backup decrypt <yedek.etbackup> <yeni-hedef-klasör> [parola-dosyası]");
    return 2;
}

async Task<string> ReadPasswordAsync(string? passwordFile, bool confirm)
{
    string password;
    if (passwordFile is not null)
    {
        password = (await File.ReadAllTextAsync(passwordFile)).TrimEnd('\r', '\n');
    }
    else
    {
        password = ReadConsoleSecret("Parola: ");
        if (confirm && !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(ReadConsoleSecret("Parolayı doğrulayın: "))))
            throw new InvalidOperationException("Girilen parolalar eşleşmiyor.");
    }

    if (password.Length is < 16 or > 4096) throw new InvalidOperationException("Parola en az 16, en fazla 4.096 karakter olmalıdır.");
    return password;
}

string ReadConsoleSecret(string prompt)
{
    Console.Write(prompt);
    var value = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
        if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; continue; }
        if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }
    return value.ToString();
}

byte[] DeriveKey(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);

async Task EncryptDirectoryAsync(string sourceDirectory, string outputPath, string password)
{
    var source = Path.GetFullPath(sourceDirectory);
    if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Yedek klasörü bulunamadı.");
    var output = Path.GetFullPath(outputPath);
    if (IsInside(output, source)) throw new InvalidOperationException("Şifreli çıktı kaynak yedek klasörünün içine yazılamaz.");
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    if (File.Exists(output)) throw new IOException("Çıktı dosyası zaten var; var olan yedek üzerine yazılmadı.");

    var work = Directory.CreateTempSubdirectory("envantertr-backup-").FullName;
    var archivePath = Path.Combine(work, "backup.zip");
    var encryptedTemp = output + $".{Guid.NewGuid():N}.tmp";
    try
    {
        await CreateArchiveAsync(source, archivePath);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var iv = RandomNumberGenerator.GetBytes(IvLength);
        var header = new byte[HeaderLength];
        magic.CopyTo(header, 0);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), Iterations);
        salt.CopyTo(header, 12);
        iv.CopyTo(header, 12 + SaltLength);
        var key = DeriveKey(password, salt);
        try
        {
            await using (var encrypted = new FileStream(encryptedTemp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await encrypted.WriteAsync(header);
                using var aes = Aes.Create();
                aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = key.AsSpan(0, 32).ToArray(); aes.IV = iv;
                await using var cipher = new CryptoStream(encrypted, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
                await using var input = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await input.CopyToAsync(cipher);
                await cipher.FlushFinalBlockAsync();
                await encrypted.FlushAsync();
            }
            await AppendHmacAsync(encryptedTemp, key.AsMemory(32, 32));
            File.Move(encryptedTemp, output);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
    finally
    {
        TryDeleteFile(encryptedTemp);
        TryDeleteDirectory(work);
    }
}

async Task VerifyAsync(string encryptedPath, string password)
{
    var work = Directory.CreateTempSubdirectory("envantertr-backup-verify-").FullName;
    try
    {
        var zipPath = Path.Combine(work, "backup.zip");
        await DecryptToZipAsync(encryptedPath, zipPath, password);
        await VerifyArchiveAsync(zipPath);
    }
    finally { TryDeleteDirectory(work); }
}

async Task DecryptDirectoryAsync(string encryptedPath, string destination, string password)
{
    var target = Path.GetFullPath(destination);
    if (Directory.Exists(target) || File.Exists(target)) throw new IOException("Hedef zaten var; var olan dosya veya klasör üzerine yazılmadı.");
    var parent = Path.GetDirectoryName(target)!;
    Directory.CreateDirectory(parent);
    var work = Path.Combine(parent, $".envantertr-restore-{Guid.NewGuid():N}");
    Directory.CreateDirectory(work);
    if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(work, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    try
    {
        var zipPath = Path.Combine(work, "backup.zip");
        await DecryptToZipAsync(encryptedPath, zipPath, password);
        var staging = Path.Combine(work, "content");
        Directory.CreateDirectory(staging);
        await ExtractArchiveAsync(zipPath, staging);
        Directory.Move(staging, target);
    }
    finally { TryDeleteDirectory(work); }
}

async Task DecryptToZipAsync(string encryptedPath, string zipPath, string password)
{
    var input = Path.GetFullPath(encryptedPath);
    var fileInfo = new FileInfo(input);
    if (!fileInfo.Exists || fileInfo.Length <= HeaderLength + TagLength) throw new InvalidDataException("Şifreli yedek dosyası eksik veya biçimi geçersiz.");
    await using var file = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
    var header = new byte[HeaderLength];
    await file.ReadExactlyAsync(header);
    if (!header.AsSpan(0, magic.Length).SequenceEqual(magic)) throw new InvalidDataException("Dosya EnvanterTR şifreli yedek biçiminde değil.");
    var iterations = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(8, 4));
    if (iterations != Iterations) throw new InvalidDataException("Bu yedek sürümünün parola türetme ayarı desteklenmiyor.");
    var salt = header.AsSpan(12, SaltLength).ToArray();
    var iv = header.AsSpan(12 + SaltLength, IvLength).ToArray();
    var key = DeriveKey(password, salt);
    try
    {
        await VerifyHmacAsync(file, header, key.AsMemory(32, 32));
        file.Position = HeaderLength;
        var cipherLength = file.Length - HeaderLength - TagLength;
        await using var boundedCipher = new BoundedReadStream(file, cipherLength);
        using var aes = Aes.Create();
        aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = key.AsSpan(0, 32).ToArray(); aes.IV = iv;
        await using var clear = new CryptoStream(boundedCipher, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
        await using var output = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await clear.CopyToAsync(output);
    }
    finally { CryptographicOperations.ZeroMemory(key); }
}

async Task AppendHmacAsync(string path, ReadOnlyMemory<byte> hmacKey)
{
    await using var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
    using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, hmacKey.ToArray());
    var buffer = new byte[128 * 1024];
    int read;
    while ((read = await file.ReadAsync(buffer)) > 0) hmac.AppendData(buffer.AsSpan(0, read));
    var tag = hmac.GetHashAndReset();
    file.Position = file.Length;
    await file.WriteAsync(tag);
    await file.FlushAsync();
}

async Task VerifyHmacAsync(FileStream file, byte[] header, ReadOnlyMemory<byte> hmacKey)
{
    var cipherLength = file.Length - HeaderLength - TagLength;
    using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, hmacKey.ToArray());
    hmac.AppendData(header);
    file.Position = HeaderLength;
    var buffer = new byte[128 * 1024];
    var remaining = cipherLength;
    while (remaining > 0)
    {
        var read = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)));
        if (read == 0) throw new EndOfStreamException("Şifreli yedek beklenmedik biçimde kesilmiş.");
        hmac.AppendData(buffer.AsSpan(0, read));
        remaining -= read;
    }
    var actual = hmac.GetHashAndReset();
    var expected = new byte[TagLength];
    await file.ReadExactlyAsync(expected);
    if (!CryptographicOperations.FixedTimeEquals(actual, expected)) throw new CryptographicException("Parola yanlış veya şifreli yedek değiştirilmiş.");
}

async Task CreateArchiveAsync(string source, string zipPath)
{
    if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) throw new IOException("Yedek klasörü sembolik bağlantı olamaz.");
    await using var output = new FileStream(zipPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
    using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8);
    foreach (var path in EnumerateRegularFiles(source).OrderBy(x => x, StringComparer.Ordinal))
    {
        var entryName = Path.GetRelativePath(source, path).Replace('\\', '/');
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var entryStream = entry.Open();
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await input.CopyToAsync(entryStream);
    }
}

IEnumerable<string> EnumerateRegularFiles(string root)
{
    var pending = new Stack<string>();
    pending.Push(root);
    while (pending.TryPop(out var directory))
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Yedek klasöründe sembolik bağlantı bulundu; güvenli arşivleme için işlem durduruldu.");
            if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
            else yield return entry;
        }
    }
}

async Task VerifyArchiveAsync(string zipPath)
{
    await using var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
    using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
    if (archive.Entries.Count == 0) throw new InvalidDataException("Yedek arşivi boş.");
    foreach (var entry in archive.Entries)
    {
        await using var input = entry.Open();
        await input.CopyToAsync(Stream.Null);
    }
}

async Task ExtractArchiveAsync(string zipPath, string destination)
{
    await using var file = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
    using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
    var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
    foreach (var entry in archive.Entries)
    {
        var target = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("Arşivde hedef klasör dışına çıkan bir dosya adı bulundu.");
        if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
        if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000) throw new InvalidDataException("Arşivde sembolik bağlantı bulundu.");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        await using var input = entry.Open();
        await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await input.CopyToAsync(output);
    }
}

bool IsInside(string path, string directory)
{
    var root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
    return Path.GetFullPath(path).StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}

void TryDeleteFile(string path) { try { File.Delete(path); } catch { } }
void TryDeleteDirectory(string path) { try { Directory.Delete(path, recursive: true); } catch { } }

sealed class BoundedReadStream : Stream
{
    private readonly Stream inner;
    private readonly long length;
    private long remaining;
    public BoundedReadStream(Stream inner, long length) { this.inner = inner; this.length = length; remaining = length; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => length - remaining; set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) { var read = inner.Read(buffer, offset, (int)Math.Min(count, remaining)); remaining -= read; return read; }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, remaining)], cancellationToken); remaining -= read; return read; }
    public override int Read(Span<byte> buffer) { var read = inner.Read(buffer[..(int)Math.Min(buffer.Length, remaining)]); remaining -= read; return read; }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
