namespace Intatis.Core.Permission;

/// <summary>
/// Workspace path confinement: canonicalizes paths, rejects sensitive targets, and
/// verifies candidates stay inside the reviewed workspace root.
/// </summary>
public static class PathConfinement
{
    private static readonly string[] SensitiveFileNamePatterns =
    [
        ".env", ".ssh", ".aws", ".gnupg", "id_rsa", "id_ed25519",
        "auth.json", "credentials.json",
    ];

    private static readonly string[] SensitiveExtensions =
    [
        ".pem", ".key", ".p12", ".pfx", ".keystore",
    ];

    public static string Canonicalize(string path)
    {
        var expanded = path.StartsWith('~')
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart('/', '\\'))
            : path;
        return Path.GetFullPath(expanded);
    }

    public static bool IsSensitivePath(string path)
    {
        var name = Path.GetFileName(path);
        var lower = name.ToLowerInvariant();
        var dir = (Path.GetDirectoryName(path) ?? "").ToLowerInvariant().Replace('\\', '/');

        foreach (var pattern in SensitiveFileNamePatterns)
        {
            if (lower.Equals(pattern, StringComparison.Ordinal)) return true;
            if (pattern.StartsWith('.') && lower.StartsWith(pattern + ".", StringComparison.Ordinal)) return true;
        }
        foreach (var extension in SensitiveExtensions)
        {
            if (lower.EndsWith(extension, StringComparison.Ordinal)) return true;
        }
        if (lower.Contains("secret", StringComparison.Ordinal) && lower.EndsWith(".json")) return true;
        if (dir.Contains("/.ssh/") || dir.Contains("/.aws/") || dir.Contains("/.gnupg/")) return true;
        if (lower.Equals(".git") || (dir.Contains("/.git/") && lower is "config" or "credentials")) return true;
        if (dir.Contains("/.config/intatis/") && lower.StartsWith("intatis.json")) return true;
        return false;
    }

    public static bool IsWithin(string root, string candidate)
    {
        var canonicalRoot = Canonicalize(root).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var canonicalCandidate = Canonicalize(candidate);
        return canonicalCandidate.StartsWith(canonicalRoot, OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal);
    }

    /// <summary>Resolves a candidate inside the workspace, rejecting escapes and sensitive targets.</summary>
    public static string ResolveWithin(string root, string candidate)
    {
        var resolved = Path.IsPathRooted(candidate)
            ? Canonicalize(candidate)
            : Canonicalize(Path.Join(Canonicalize(root), candidate));
        if (!IsWithin(root, resolved))
            throw new InvalidOperationException($"path escapes the workspace: {candidate}");
        if (IsSensitivePath(resolved))
            throw new InvalidOperationException($"path touches a sensitive file: {candidate}");
        return resolved;
    }

    public static string? TryResolveWithin(string root, string candidate)
    {
        try { return ResolveWithin(root, candidate); }
        catch (InvalidOperationException) { return null; }
    }
}

/// <summary>Heuristic secret detection used by the Mediator and shell gate.</summary>
public static class SecretScanner
{
    private static readonly string[] MarkerPrefixes =
    [
        "sk-", "sk-proj-", "gsk_", "ghp_", "gho_", "github_pat_",
        "xoxb-", "xoxp-", "AKIA", "AIza",
    ];

    public static bool ContainsSecret(string text)
    {
        foreach (var prefix in MarkerPrefixes)
        {
            var index = text.IndexOf(prefix, StringComparison.Ordinal);
            if (index >= 0)
            {
                var remainder = text[(index + prefix.Length)..];
                var run = 0;
                foreach (var c in remainder)
                {
                    if (char.IsLetterOrDigit(c) || c is '_' or '-') run++;
                    else break;
                    if (run >= 12) return true;
                }
            }
        }
        return text.Contains("BEGIN RSA PRIVATE KEY", StringComparison.Ordinal)
            || text.Contains("BEGIN OPENSSH PRIVATE KEY", StringComparison.Ordinal)
            || text.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal)
            || text.Contains("BEGIN CERTIFICATE", StringComparison.Ordinal);
    }
}
