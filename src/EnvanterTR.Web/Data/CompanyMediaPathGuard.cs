namespace EnvanterTR.Web.Data;

internal static class CompanyMediaPathGuard
{
    public static bool IsSafePath(IConfiguration configuration, Guid companyId, string path) =>
        companyId != Guid.Empty && IsSafeScopedPath(configuration, companyId.ToString("N"), path);

    public static bool IsSafeAccountPath(IConfiguration configuration, Guid userId, string path) =>
        userId != Guid.Empty && IsSafeScopedPath(configuration, Path.Combine("accounts", userId.ToString("N")), path);

    private static bool IsSafeScopedPath(IConfiguration configuration, string namespacePath, string path)
    {
        try
        {
            var root = Path.GetFullPath(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media");
            var scope = Path.Combine(root, namespacePath);
            var fullPath = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!fullPath.StartsWith(scope + Path.DirectorySeparatorChar, comparison)) return false;

            // The configured root is trusted. No existing component below it,
            // including the final file, may redirect into another namespace.
            var current = root;
            foreach (var segment in Path.GetRelativePath(root, fullPath).Split(
                         new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (ArgumentException) { return false; }
        catch (NotSupportedException) { return false; }
    }
}
