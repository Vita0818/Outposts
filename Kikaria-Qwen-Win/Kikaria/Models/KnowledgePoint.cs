using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kikaria.Models;

public partial class KnowledgePoint : ObservableObject
{
    [ObservableProperty]
    [JsonInclude]
    private Guid id;

    [ObservableProperty]
    [JsonInclude]
    private string title;

    [ObservableProperty]
    [JsonInclude]
    private List<string> tags;

    [ObservableProperty]
    [JsonInclude]
    private string hint;

    [ObservableProperty]
    [JsonInclude]
    private string content;

    [ObservableProperty]
    [JsonInclude]
    private bool isReinforced;

    [ObservableProperty]
    [JsonInclude]
    private int reinforcementCount;

    [ObservableProperty]
    [JsonInclude]
    private DateTime? lastReinforcedAt;

    [ObservableProperty]
    [JsonInclude]
    private bool isMastered;

    [ObservableProperty]
    [JsonInclude]
    private DateTime createdAt;

    [ObservableProperty]
    [JsonInclude]
    private DateTime updatedAt;

    [JsonConstructor]
    public KnowledgePoint(
        Guid id,
        string title,
        List<string> tags,
        string hint,
        string content,
        bool isReinforced,
        int reinforcementCount,
        DateTime? lastReinforcedAt,
        bool isMastered,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Title = title;
        Tags = tags;
        Hint = hint;
        Content = content;
        IsReinforced = isReinforced;
        ReinforcementCount = reinforcementCount;
        LastReinforcedAt = lastReinforcedAt;
        IsMastered = isMastered;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public KnowledgePoint()
    {
        Id = Guid.NewGuid();
        Title = string.Empty;
        Tags = new List<string>();
        Hint = string.Empty;
        Content = string.Empty;
        IsReinforced = false;
        ReinforcementCount = 0;
        LastReinforcedAt = null;
        IsMastered = false;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public KnowledgePoint(string title, List<string> tags, string hint, string content)
    {
        Id = Guid.NewGuid();
        Title = title;
        Tags = tags;
        Hint = hint;
        Content = content;
        IsReinforced = false;
        ReinforcementCount = 0;
        LastReinforcedAt = null;
        IsMastered = false;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public int AddReinforcement(DateTime? date = null)
    {
        IsReinforced = true;
        ReinforcementCount++;
        var d = date ?? DateTime.Now;
        LastReinforcedAt = d;
        UpdatedAt = d;
        return ReinforcementCount;
    }

    public void ClearReinforcement(DateTime? date = null)
    {
        IsReinforced = false;
        ReinforcementCount = 0;
        LastReinforcedAt = null;
        UpdatedAt = date ?? DateTime.Now;
    }

    public static List<KnowledgePoint> ParseMarkdown(string markdown, DateTime? date = null)
    {
        var points = new List<KnowledgePoint>();
        var sections = Regex.Split(markdown, @"(?m)^---\s*$");
        var timestamp = date ?? DateTime.Now;

        foreach (var section in sections)
        {
            var trimmed = section.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            string title = string.Empty;
            var tags = new List<string>();
            string hint = string.Empty;
            string content = string.Empty;
            int hintIndex = -1;
            int contentIndex = -1;

            var lines = trimmed.Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i].TrimEnd('\r');
                var lineLower = line.ToLowerInvariant();

                if (line.Length > 0 && line[0] == '#')
                {
                    title = line.TrimStart('#').Trim();
                    i++;
                }
                else if (lineLower.StartsWith("tags:"))
                {
                    var tagsLine = line.Substring(5).Trim();
                    if (!string.IsNullOrWhiteSpace(tagsLine))
                    {
                        tags = tagsLine.Split(new[] { ',', '\uFF0C' })
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToList();
                    }
                    i++;
                }
                else if (lineLower == "hint:")
                {
                    hintIndex = i;
                    i++;
                    var hintLines = new List<string>();
                    while (i < lines.Length)
                    {
                        var hLine = lines[i].TrimEnd('\r');
                        var hLineLower = hLine.ToLowerInvariant();
                        if (hLineLower == "content:" || (hLine.Length > 0 && hLine[0] == '#') || hLine.Trim() == "---")
                            break;
                        hintLines.Add(hLine);
                        i++;
                    }
                    hint = string.Join("\n", hintLines).Trim();
                }
                else if (lineLower == "content:")
                {
                    contentIndex = i;
                    i++;
                    var contentLines = new List<string>();
                    while (i < lines.Length)
                    {
                        var cLine = lines[i].TrimEnd('\r');
                        if ((cLine.Length > 0 && cLine[0] == '#') || cLine.Trim() == "---")
                            break;
                        contentLines.Add(cLine);
                        i++;
                    }
                    content = string.Join("\n", contentLines).Trim();
                }
                else
                {
                    i++;
                }
            }

            if (!string.IsNullOrWhiteSpace(title)
                && (hintIndex < 0 || contentIndex < 0 || hintIndex < contentIndex)
                && !string.IsNullOrEmpty(hint)
                && !string.IsNullOrEmpty(content))
            {
                var point = new KnowledgePoint(title, tags, hint, content)
                {
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp
                };
                points.Add(point);
            }
        }

        if (points.Count == 0)
            throw new KnowledgePointMarkdownError();

        return points;
    }

    public static string MarkdownTextFrom(List<KnowledgePoint> points)
    {
        var sections = new List<string>();

        foreach (var point in points)
        {
            var lines = new List<string>();
            lines.Add($"# {point.Title}");
            lines.Add(string.Empty);

            lines.Add(point.Tags.Count > 0 ? $"tags: {string.Join(", ", point.Tags)}" : "tags:");
            lines.Add(string.Empty);

            lines.Add("hint:");
            lines.Add(point.Hint ?? string.Empty);
            lines.Add(string.Empty);

            lines.Add("content:");
            lines.Add(point.Content ?? string.Empty);
            lines.Add(string.Empty);

            sections.Add(string.Join("\n", lines));
        }

        return string.Join("\n---\n\n", sections);
    }
}

public class KnowledgePointMarkdownError : InvalidOperationException
{
    public KnowledgePointMarkdownError()
        : base("No valid knowledge points found in markdown.") { }
}
