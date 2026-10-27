namespace EnvanterTR.Web.Data;

internal static class CompanyMediaPathGuard
{
    public static bool IsSafePath(IConfiguration configuration, Guid companyId, string path)
    {
        if (companyId == Guid.Empty) return false;
        try
        {
            var root = Path.GetFullPath(configuration["Storage:CompanyMediaPath"] ?? "/var/lib/envantertr/company-media");
            var company = Path.Combine(root, companyId.ToString("N"));
            var fullPath = Path.GetFullPath(path);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!fullPath.StartsWith(company + Path.DirectorySeparatorChar, comparison)) return false;

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
