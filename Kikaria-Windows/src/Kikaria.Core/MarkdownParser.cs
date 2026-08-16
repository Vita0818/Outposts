//
//  MarkdownParser.cs
//  Kikaria-Windows
//
//  结构化 Markdown 知识点解析 / 导出,逐行移植自 Kikaria-Apple 的 KnowledgePoint.swift:
//  - 整行 trim 后 == "---" 分块;
//  - 每块第一非空行必须以 # 开头作为标题;
//  - tags: 行取块内第一行匹配(小写前缀),值按英文/中文逗号分割;
//  - hint: 与 content: 必须是整行恰好等于 "hint:"/"content:"(trim 小写),且 hint 在 content 之前;
//  - hint = 两行之间 trim,content = content 行之后全部 trim;两者非空才有效。
//

namespace Kikaria.Core;

public static class MarkdownParser
{
    /// <summary>全部无效时抛出的文案(与 Apple 版一致)。</summary>
    public const string NoValidPointsMessage = "没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。";

    /// <summary>解析 Markdown 文本为知识点列表;无有效知识点时抛出 InvalidOperationException。</summary>
    public static List<KnowledgePoint> ParseMarkdown(string markdown, DateTime? date = null)
    {
        var at = date ?? DateTime.Now;
        var normalized = markdown.Replace("\r\n", "\n").Replace("\r", "\n");
        var chunks = SplitMarkdownIntoChunks(normalized);
        var points = new List<KnowledgePoint>();

        foreach (var chunk in chunks)
        {
            var point = ParseChunk(chunk, at);
            if (point is not null)
            {
                points.Add(point);
            }
        }

        if (points.Count == 0)
        {
            throw new InvalidOperationException(NoValidPointsMessage);
        }

        return points;
    }

    /// <summary>尝试解析;失败返回 null。</summary>
    public static List<KnowledgePoint>? TryParseMarkdown(string markdown, DateTime? date = null)
    {
        try
        {
            return ParseMarkdown(markdown, date);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>导出为 Kikaria Markdown 格式,以 "\n\n---\n\n" 连接。</summary>
    public static string MarkdownText(IEnumerable<KnowledgePoint> points)
    {
        var blocks = points.Select(point =>
            "# " + point.Title + "\n\n" +
            "tags: " + string.Join(", ", point.Tags) + "\n\n" +
            "hint:\n" + point.Hint + "\n\n" +
            "content:\n" + point.Content);

        return string.Join("\n\n---\n\n", blocks);
    }

    private static List<string> SplitMarkdownIntoChunks(string markdown)
    {
        var chunks = new List<string>();
        var currentLines = new List<string>();

        foreach (var line in markdown.Split('\n'))
        {
            if (line.Trim() == "---")
            {
                var chunk = string.Join("\n", currentLines).Trim();
                if (chunk.Length > 0)
                {
                    chunks.Add(chunk);
                }

                currentLines.Clear();
            }
            else
            {
                currentLines.Add(line);
            }
        }

        var finalChunk = string.Join("\n", currentLines).Trim();
        if (finalChunk.Length > 0)
        {
            chunks.Add(finalChunk);
        }

        return chunks;
    }

    private static KnowledgePoint? ParseChunk(string chunk, DateTime at)
    {
        var lines = chunk.Split('\n');
        var titleIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length > 0)
            {
                titleIndex = i;
                break;
            }
        }

        if (titleIndex < 0)
        {
            return null;
        }

        var rawTitle = lines[titleIndex].Trim();
        if (!rawTitle.StartsWith("#", StringComparison.Ordinal))
        {
            return null;
        }

        var title = rawTitle.TrimStart('#').Trim();
        if (title.Length == 0)
        {
            return null;
        }

        var tags = ParseTags(lines);
        var hintIndex = MarkerIndex("hint:", lines);
        var contentIndex = MarkerIndex("content:", lines);
        if (hintIndex is null || contentIndex is null || hintIndex.Value >= contentIndex.Value)
        {
            return null;
        }

        var hintLines = new List<string>();
        for (var i = hintIndex.Value + 1; i < contentIndex.Value; i++)
        {
            hintLines.Add(lines[i]);
        }

        var hint = string.Join("\n", hintLines).Trim();

        var contentLines = new List<string>();
        for (var i = contentIndex.Value + 1; i < lines.Length; i++)
        {
            contentLines.Add(lines[i]);
        }

        var content = string.Join("\n", contentLines).Trim();

        if (hint.Length == 0 || content.Length == 0)
        {
            return null;
        }

        return new KnowledgePoint
        {
            Id = Guid.NewGuid(),
            Title = title,
            Tags = tags,
            Hint = hint,
            Content = content,
            CreatedAt = at,
            UpdatedAt = at
        };
    }

    private static List<string> ParseTags(string[] lines)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.ToLowerInvariant().StartsWith("tags:", StringComparison.Ordinal))
            {
                var tagText = trimmed.Substring("tags:".Length);
                return tagText
                    .Split(',', '，')
                    .Select(tag => tag.Trim())
                    .Where(tag => tag.Length > 0)
                    .ToList();
            }
        }

        return new List<string>();
    }

    private static int? MarkerIndex(string marker, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().ToLowerInvariant() == marker)
            {
                return i;
            }
        }

        return null;
    }
}
