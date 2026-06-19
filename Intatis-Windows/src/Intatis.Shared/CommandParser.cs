using System.Text;
using System.IO;

namespace Intatis.Windows.Shared;

public static class CommandParser
{
    private static readonly string[] AllowedReasoning = ["minimal", "low", "medium", "high"];

    public static List<string> ParseTokens(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        var tokens = new List<string>();
        var token = new StringBuilder();
        var inQuotes = false;
        var escaping = false;

        foreach (var ch in input)
        {
            if (escaping)
            {
                token.Append(ch);
                escaping = false;
                continue;
            }

            if (ch == '\\')
            {
                escaping = true;
                continue;
            }

            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }

                continue;
            }

            token.Append(ch);
        }

        if (token.Length > 0)
            tokens.Add(token.ToString());

        return tokens;
    }

    public static string ExpandTilde(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
            return path;

        if (path == "~")
            return home;

        if (path.StartsWith($"~{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            path.StartsWith("~/", StringComparison.Ordinal))
        {
            return Path.Combine(home, path[2..]);
        }

        return path;
    }

    public static bool TryNormalizeReasoning(string? value, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            normalized = null;
            return true;
        }

        foreach (var level in AllowedReasoning)
        {
            if (level.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                normalized = level;
                return true;
            }
        }

        normalized = null;
        return false;
    }
}
