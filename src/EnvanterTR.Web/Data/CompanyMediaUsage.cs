namespace EnvanterTR.Web.Data;

public sealed record CompanyMediaUsageSnapshot(long Bytes, long FileCount, DateTimeOffset MeasuredAt);

public static class CompanyMediaUsage
{
    public static CompanyMediaUsageSnapshot Measure(string mediaRoot, Guid companyId, CancellationToken cancellationToken)
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetFullPath(mediaRoot), companyId.ToString("N")));
        if (!directory.Exists) return new(0, 0, DateTimeOffset.UtcNow);
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Company media directory must not redirect outside its tenant directory.");
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true, IgnoreInaccessible = false,
            AttributesToSkip = FileAttributes.ReparsePoint, ReturnSpecialDirectories = false
        };
        long bytes = 0, files = 0, entries = 0;
        foreach (var entry in directory.EnumerateFileSystemInfos("*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++entries > 1_000_000) throw new IOException("Company media scan limit reached.");
            if (entry is not FileInfo file) continue;
            bytes = checked(bytes + file.Length);
            files++;
        }
        return new(bytes, files, DateTimeOffset.UtcNow);
    }
}
