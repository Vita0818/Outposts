namespace Rokurics.Models;

/// <summary>
/// A tag with namespace and value. Mirrors StudyTag from source.
/// </summary>
public sealed class StudyTag : IEquatable<StudyTag>
{
    public string Id { get; }
    public string Namespace { get; }
    public string Value { get; }
    public string? DisplayName { get; }
    public DateTime? CreatedAt { get; }

    public StudyTag(string? id = null, string namespace_ = "custom", string value = "", string? displayName = null, DateTime? createdAt = null)
    {
        Namespace = NormalizedNamespace(namespace_);
        Value = NormalizedValue(value);
        DisplayName = displayName?.Trim() is { Length: > 0 } d ? d : null;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        Id = id?.Trim() is { Length: > 0 } i ? i : MakeId(Namespace, Value);
    }

    public string DisplayTitle => DisplayName ?? Value;

    public bool Equals(StudyTag? other)
    {
        if (other is null) return false;
        return Namespace == other.Namespace && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as StudyTag);
    public override int GetHashCode() => HashCode.Combine(Namespace, Value.ToLowerInvariant());

    public static string NormalizedNamespace(string ns) =>
        ns.Trim().ToLowerInvariant() is { Length: > 0 } n ? n : "custom";

    public static string NormalizedValue(string v) => v.Trim();

    public static string MakeId(string ns, string value) =>
        $"{NormalizedNamespace(ns)}:{NormalizedValue(value).ToLowerInvariant()}";
}
