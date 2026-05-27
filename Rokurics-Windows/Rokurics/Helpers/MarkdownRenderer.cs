namespace Rokurics.Helpers;

/// <summary>
/// Lightweight markdown-to-display-text renderer for WinUI TextBlock.
/// Preserves structure (headings, lists, code blocks) while producing
/// readable plain text for non-web-rendering contexts.
/// Mirrors the markdown rendering behavior from Apple source
/// StudyReadingContentView / StudyDocumentLoader.
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>
    /// Render markdown to display text suitable for a TextBlock.
    /// Preserves section structure, list formatting, and code blocks.
    /// </summary>
    public static string Render(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return markdown ?? "";

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var result = new List<string>();
        var inCodeBlock = false;
        var prevLineWasEmpty = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var trimmed = raw.TrimEnd();

            // Code block fence detection
            if (trimmed.TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                if (!inCodeBlock) result.Add("───");
                prevLineWasEmpty = false;
                continue;
            }

            if (inCodeBlock)
            {
                result.Add("  " + trimmed);
                prevLineWasEmpty = false;
                continue;
            }

            // Blank line
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (!prevLineWasEmpty && result.Count > 0)
                    result.Add("");
                prevLineWasEmpty = true;
                continue;
            }

            prevLineWasEmpty = false;

            // Section headings
            if (trimmed.StartsWith("### "))
                result.Add("\n  " + FormatInline(trimmed[4..].Trim()));
            else if (trimmed.StartsWith("## "))
                result.Add("\n── " + FormatInline(trimmed[3..].Trim()) + " ──");
            else if (trimmed.StartsWith("# "))
                result.Add("\n══ " + FormatInline(trimmed[2..].Trim()) + " ══");
            // Block quotes
            else if (trimmed.StartsWith("> "))
                result.Add("  ▌" + FormatInline(trimmed[2..].Trim()));
            // Unordered list
            else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                result.Add("  • " + FormatInline(trimmed[2..].Trim()));
            // Ordered list
            else if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                var content = System.Text.RegularExpressions.Regex.Replace(trimmed, @"^\d+\.\s", "");
                result.Add("  " + (result.Count > 0 ? "◦" : "·") + " " + FormatInline(content.Trim()));
            }
            // Horizontal rule
            else if (trimmed.Trim() is "---" or "***" or "___")
                result.Add("───");
            // Table row (approximate)
            else if (trimmed.StartsWith("|") && trimmed.EndsWith("|"))
            {
                var cells = trimmed.Trim('|').Split('|')
                    .Select(c => FormatInline(c.Trim())).ToList();
                result.Add("  " + string.Join("  │  ", cells));
            }
            // Regular text
            else
                result.Add(FormatInline(trimmed));
        }

        return string.Join("\n", result).Trim();
    }

    /// <summary>
    /// Format inline markdown: bold (**text**), italic (*text*), inline code (`text`).
    /// </summary>
    private static string FormatInline(string text)
    {
        // Bold
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        // Italic
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1");
        // Inline code
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]+)`", "$1");
        // Links: keep text, drop URL
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
        // Strikethrough
        text = System.Text.RegularExpressions.Regex.Replace(text, @"~~(.+?)~~", "$1");
        return text;
    }

    /// <summary>
    /// Extract the first N characters as a summary.
    /// </summary>
    public static string ExtractSummary(string markdown, int maxChars = 300)
    {
        var plain = Render(markdown);
        var cleaned = System.Text.RegularExpressions.Regex
            .Replace(plain, @"[═─▌•◦·│]", "")
            .Replace("\n", " ")
            .Trim();
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        if (cleaned.Length <= maxChars) return cleaned;
        var truncated = cleaned[..maxChars];
        var lastSpace = truncated.LastIndexOf(' ');
        return (lastSpace > maxChars * 0.7 ? truncated[..lastSpace] : truncated) + "…";
    }

    /// <summary>
    /// Extract provider and model metadata from note markdown frontmatter.
    /// Mirrors metadata extraction from Apple source StudyNoteReadingPage.
    /// </summary>
    public static (string? provider, string? model, string? generatedAt) ExtractNoteMetadata(string markdown)
    {
        string? provider = null;
        string? model = null;
        string? generatedAt = null;

        var lines = markdown.Split('\n');
        foreach (var line in lines.Take(15))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("> Provider:"))
                provider = trimmed["Provider:".Length..].Trim().TrimStart('>').Trim();
            else if (trimmed.StartsWith("> Model:"))
                model = trimmed["Model:".Length..].Trim().TrimStart('>').Trim();
            else if (trimmed.StartsWith("> 由"))
                generatedAt = trimmed.TrimStart('>').Trim();
            else if (trimmed.StartsWith("> Provider:"))
                provider = trimmed["> Provider:".Length..].Trim();
        }

        return (provider, model, generatedAt);
    }
}

/// <summary>
/// Transcript JSON result model — mirrors Apple source transcription output format.
/// </summary>
public sealed class TranscriptResult
{
    public string? RecordingId { get; set; }
    public string? ProviderName { get; set; }
    public string? ModelName { get; set; }
    public string? Language { get; set; }
    public double? DurationSeconds { get; set; }
    public List<TranscriptSegment>? Segments { get; set; }
    public string? FullText { get; set; }

    public static TranscriptResult? LoadFromJson(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath)) return null;
            var json = File.ReadAllText(jsonPath);
            return System.Text.Json.JsonSerializer.Deserialize<TranscriptResult>(json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch { return null; }
    }
}

public sealed class TranscriptSegment
{
    public int Index { get; set; }
    public double Start { get; set; }
    public double End { get; set; }
    public string? Text { get; set; }
    public double? Confidence { get; set; }

    public string TimeRange => $"{FormatTime(Start)} – {FormatTime(End)}";

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"h\:mm\:ss")
            : ts.ToString(@"m\:ss");
    }
}
