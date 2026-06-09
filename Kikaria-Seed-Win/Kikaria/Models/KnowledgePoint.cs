using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Kikaria.Models
{
    public class KnowledgePoint : IEquatable<KnowledgePoint>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string Hint { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        
        private bool _isReinforced;
        public bool IsReinforced
        {
            get => ReinforcementCount > 0;
            set => _isReinforced = value;
        }
        
        public int ReinforcementCount { get; set; }
        public DateTimeOffset? LastReinforcedAt { get; set; }
        public bool IsMastered { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public KnowledgePoint(
            Guid id,
            string title,
            List<string> tags,
            string hint,
            string content,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt,
            bool isReinforced = false,
            bool isMastered = false,
            int? reinforcementCount = null,
            DateTimeOffset? lastReinforcedAt = null)
        {
            Id = id;
            Title = title;
            Tags = tags;
            Hint = hint;
            Content = content;
            _isReinforced = isReinforced;
            
            var migratedReinforcementCount = Math.Max(0, reinforcementCount ?? (isReinforced ? 1 : 0));
            ReinforcementCount = migratedReinforcementCount;
            LastReinforcedAt = migratedReinforcementCount > 0 ? lastReinforcedAt : null;
            IsMastered = isMastered;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        [JsonConstructor]
        public KnowledgePoint(
            Guid id,
            string title,
            List<string> tags,
            string hint,
            string content,
            bool isReinforced,
            int reinforcementCount,
            DateTimeOffset? lastReinforcedAt,
            bool isMastered,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt)
        {
            Id = id;
            Title = title;
            Tags = tags;
            Hint = hint;
            Content = content;
            _isReinforced = isReinforced;
            
            ReinforcementCount = Math.Max(0, reinforcementCount);
            IsReinforced = ReinforcementCount > 0;
            LastReinforcedAt = lastReinforcedAt;
            if (ReinforcementCount == 0)
            {
                LastReinforcedAt = null;
            }
            IsMastered = isMastered;
            CreatedAt = createdAt == default ? DateTimeOffset.Now : createdAt;
            UpdatedAt = updatedAt == default ? CreatedAt : updatedAt;
        }

        public int AddReinforcement(DateTimeOffset? date = null)
        {
            date ??= DateTimeOffset.Now;
            ReinforcementCount = Math.Max(0, ReinforcementCount) + 1;
            _isReinforced = true;
            LastReinforcedAt = date;
            UpdatedAt = date.Value;
            return ReinforcementCount;
        }

        public void ClearReinforcement(DateTimeOffset? date = null)
        {
            date ??= DateTimeOffset.Now;
            ReinforcementCount = 0;
            _isReinforced = false;
            LastReinforcedAt = null;
            UpdatedAt = date.Value;
        }

        public bool Equals(KnowledgePoint? other)
        {
            if (other is null) return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj) => Equals(obj as KnowledgePoint);
        public override int GetHashCode() => Id.GetHashCode();

        public static List<KnowledgePoint> ParseMarkdown(string markdown, DateTimeOffset? date = null)
        {
            date ??= DateTimeOffset.Now;
            var normalizedText = markdown
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
            var chunks = SplitMarkdownIntoChunks(normalizedText);
            var points = chunks.Select(c => ParseChunk(c, date.Value)).Where(p => p != null).Cast<KnowledgePoint>().ToList();
            
            if (!points.Any())
            {
                throw new InvalidOperationException("No valid knowledge points were found.");
            }
            
            return points;
        }

        public static string ToMarkdown(List<KnowledgePoint> points)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                sb.AppendLine($"# {point.Title}");
                sb.AppendLine();
                sb.AppendLine($"tags: {string.Join(", ", point.Tags)}");
                sb.AppendLine();
                sb.AppendLine("hint:");
                sb.AppendLine(point.Hint);
                sb.AppendLine();
                sb.AppendLine("content:");
                sb.AppendLine(point.Content);
                
                if (i != points.Count - 1)
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
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
                    if (!string.IsNullOrEmpty(chunk))
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
            if (!string.IsNullOrEmpty(finalChunk))
            {
                chunks.Add(finalChunk);
            }
            
            return chunks;
        }

        private static KnowledgePoint? ParseChunk(string chunk, DateTimeOffset date)
        {
            var lines = chunk.Split('\n').ToList();
            var titleIndex = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            if (titleIndex == -1) return null;
            
            var rawTitle = lines[titleIndex].Trim();
            if (!rawTitle.StartsWith("#")) return null;
            
            var title = rawTitle.TrimStart('#').Trim();
            if (string.IsNullOrEmpty(title)) return null;
            
            var tags = ParseTags(lines);
            var hintIndex = MarkerIndex("hint:", lines);
            var contentIndex = MarkerIndex("content:", lines);
            
            if (hintIndex == -1 || contentIndex == -1 || hintIndex >= contentIndex)
            {
                return null;
            }
            
            var hint = string.Join("\n", lines.GetRange(hintIndex + 1, contentIndex - hintIndex - 1)).Trim();
            var content = string.Join("\n", lines.GetRange(contentIndex + 1, lines.Count - contentIndex - 1)).Trim();
            
            if (string.IsNullOrEmpty(hint) || string.IsNullOrEmpty(content))
            {
                return null;
            }
            
            return new KnowledgePoint(
                id: Guid.NewGuid(),
                title: title,
                tags: tags,
                hint: hint,
                content: content,
                createdAt: date,
                updatedAt: date
            );
        }

        private static List<string> ParseTags(List<string> lines)
        {
            var tagLine = lines.FirstOrDefault(l => l.Trim().ToLower().StartsWith("tags:"));
            if (tagLine == null) return new List<string>();
            
            var tagText = tagLine.Trim().Substring("tags:".Length);
            return tagText.Split(',', '，')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();
        }

        private static int MarkerIndex(string marker, List<string> lines)
        {
            return lines.FindIndex(l => l.Trim().ToLower() == marker.ToLower());
        }
    }

    public class KnowledgePreset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string MarkdownText { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; }

        public KnowledgePreset(
            string id,
            string name,
            string subtitle,
            string description,
            string category,
            string markdownText,
            bool isBuiltIn)
        {
            Id = id;
            Name = name;
            Subtitle = subtitle ?? string.Empty;
            Description = description ?? subtitle ?? string.Empty;
            Category = category ?? "自定义";
            MarkdownText = markdownText;
            IsBuiltIn = isBuiltIn;
        }

        public int KnowledgePointCount
        {
            get
            {
                try
                {
                    return KnowledgePoint.ParseMarkdown(MarkdownText).Count;
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
