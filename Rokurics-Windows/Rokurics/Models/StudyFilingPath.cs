namespace Rokurics.Models;

/// <summary>
/// Four-level hierarchical filing path for study organization.
/// Mirrors StudyFilingPath from source.
/// </summary>
public sealed class StudyFilingPath : IEquatable<StudyFilingPath>
{
    public string? Type { get; }
    public string? Subject { get; }
    public string? Chapter { get; }
    public string? Topic { get; }

    public StudyFilingPath(string? type = null, string? subject = null, string? chapter = null, string? topic = null)
    {
        Type = Normalized(type);
        Subject = Normalized(subject);
        Chapter = Normalized(chapter);
        Topic = Normalized(topic);
    }

    public bool IsEmpty => Type is null && Subject is null && Chapter is null && Topic is null;

    public string DisplaySummary
    {
        get
        {
            var parts = new[] { Type, Subject, Chapter, Topic }.Where(p => p is not null).Cast<string>();
            return parts.Any() ? string.Join(" / ", parts) : UncategorizedTitle;
        }
    }

    public string? ValueFor(string levelKey) => StudyTag.NormalizedNamespace(levelKey) switch
    {
        "type" => Type,
        "subject" => Subject,
        "chapter" => Chapter,
        "topic" => Topic,
        _ => null
    };

    public string? ValueFor(StudyFolderLevel level) => level switch
    {
        StudyFolderLevel.Type => Type,
        StudyFolderLevel.Subject => Subject,
        StudyFolderLevel.Chapter => Chapter,
        StudyFolderLevel.Topic => Topic,
        _ => null
    };

    public string SuggestedTitle(string defaultTitle)
    {
        var parts = new[] { Subject, Chapter, Topic }.Where(p => p is not null).Cast<string>().ToList();
        return parts.Count > 0 ? string.Join(" · ", parts) : (Type ?? defaultTitle);
    }

    public static string? Normalized(string? value) =>
        value?.Trim() is { Length: > 0 } trimmed ? trimmed : null;

    public const string UncategorizedTitle = "未分类";
    public const string MissingTitle = "未填写";

    public bool Equals(StudyFilingPath? other)
    {
        if (other is null) return false;
        return Type == other.Type && Subject == other.Subject && Chapter == other.Chapter && Topic == other.Topic;
    }

    public override bool Equals(object? obj) => Equals(obj as StudyFilingPath);
    public override int GetHashCode() => HashCode.Combine(Type, Subject, Chapter, Topic);
}
