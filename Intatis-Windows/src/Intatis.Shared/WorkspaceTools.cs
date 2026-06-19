namespace Intatis.Windows.Shared;

public static class WorkspaceTools
{
    public static string ResolveWorkspace(string? configured, string? requested)
    {
        var source = !string.IsNullOrWhiteSpace(requested) ? requested : configured;
        if (string.IsNullOrWhiteSpace(source))
            throw new InvalidOperationException("No workspace path is configured.");

        var expanded = CommandParser.ExpandTilde(source);
        var full = Path.GetFullPath(expanded);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"Workspace not found: {full}");
        return full;
    }

    public static IReadOnlyList<string> List(string workspace, string relativePath = "")
    {
        var dir = ResolvePath(workspace, relativePath);
        EnsureInsideWorkspace(workspace, dir);
        return Directory.EnumerateFileSystemEntries(dir)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public static string ReadText(string workspace, string relativePath)
    {
        var path = ResolvePath(workspace, relativePath);
        EnsureInsideWorkspace(workspace, path);
        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);
        return File.ReadAllText(path);
    }

    public static IReadOnlyList<SearchHit> Search(string workspace, string needle, string? relativePath = null)
    {
        var root = ResolvePath(workspace, relativePath ?? ".");
        EnsureInsideWorkspace(workspace, root);

        if (!Directory.Exists(root) && !File.Exists(root))
            throw new FileNotFoundException("Path not found.", root);

        var matches = new List<SearchHit>();
        if (File.Exists(root))
        {
            SearchInFile(root, needle, matches, workspace);
            return matches;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            SearchInFile(file, needle, matches, workspace);
        }

        return matches;
    }

    public static void WriteText(string workspace, string relativePath, string content, bool overwrite = true)
    {
        var path = ResolvePath(workspace, relativePath);
        EnsureInsideWorkspace(workspace, path);
        var parent = Path.GetDirectoryName(path) ?? workspace;
        Directory.CreateDirectory(parent);
        if (!overwrite && File.Exists(path))
            throw new IOException("File already exists.");
        File.WriteAllText(path, content);
    }

    private static void SearchInFile(string path, string needle, List<SearchHit> matches, string workspace)
    {
        var lines = File.ReadLines(path);
        var relative = Path.GetRelativePath(workspace, path);
        var idx = 1;
        foreach (var line in lines)
        {
            if (line.Contains(needle, StringComparison.OrdinalIgnoreCase))
                matches.Add(new SearchHit(relative, idx, line));
            idx++;
        }
    }

    private static string ResolvePath(string workspace, string relativePath)
    {
        var fullWorkspace = Path.GetFullPath(workspace);
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
            return fullWorkspace;
        if (Path.IsPathRooted(relativePath))
            return Path.GetFullPath(relativePath);
        return Path.GetFullPath(Path.Combine(fullWorkspace, relativePath));
    }

    private static void EnsureInsideWorkspace(string workspace, string path)
    {
        var fullWorkspace = Path.GetFullPath(workspace)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullWorkspaceWithSlash = fullWorkspace + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!(string.Equals(fullPath, fullWorkspace, StringComparison.OrdinalIgnoreCase) ||
              fullPath.StartsWith(fullWorkspaceWithSlash, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("Access denied: path escapes workspace.");
        }
    }
}
