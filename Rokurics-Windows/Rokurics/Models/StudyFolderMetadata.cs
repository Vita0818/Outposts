namespace Rokurics.Models;

/// <summary>
/// Study folder metadata. Mirrors StudyFolderMetadata from source.
/// </summary>
public sealed class StudyFolderMetadata
{
    public string FolderId { get; set; }
    public string Name { get; set; }
    public StudyFolderLevel Level { get; set; }
    public StudyFilingPath Path { get; set; }
    public string? ParentFolderId { get; set; }
    public List<string> ChildFolderIds { get; set; }
    public List<string> ItemIds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StudyFolderColorToken? ColorToken { get; set; }
    public bool IsTrashed { get; set; }
    public DateTime? TrashedAt { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; }
    public string? ModifiedByDeviceId { get; set; }
    public string? SyncConflictStatus { get; set; }

    public StudyFolderMetadata()
    {
        FolderId = "";
        Name = "";
        Path = new StudyFilingPath();
        ChildFolderIds = new List<string>();
        ItemIds = new List<string>();
        CustomProperties = new Dictionary<string, string>();
    }

    public StudyFolderMetadata(
        string? folderId, string name, StudyFolderLevel level, StudyFilingPath path,
        string? parentFolderId = null, List<string>? childFolderIds = null, List<string>? itemIds = null,
        DateTime? createdAt = null, DateTime? updatedAt = null, StudyFolderColorToken? colorToken = null,
        bool isTrashed = false, DateTime? trashedAt = null, Dictionary<string, string>? customProperties = null,
        string? modifiedByDeviceId = null, string? syncConflictStatus = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? StudyHierarchyRule.MissingValue : name.Trim();
        Level = level;
        Path = path;
        FolderId = string.IsNullOrWhiteSpace(folderId) ? FolderIdFor(level, path) : folderId.Trim();
        ParentFolderId = parentFolderId?.Trim() is { Length: > 0 } p ? p : null;
        ChildFolderIds = childFolderIds ?? new List<string>();
        ItemIds = itemIds ?? new List<string>();
        CreatedAt = createdAt ?? DateTime.UtcNow;
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
        ColorToken = colorToken == StudyFolderColorToken.Default ? null : colorToken;
        IsTrashed = isTrashed;
        TrashedAt = trashedAt;
        CustomProperties = customProperties ?? new Dictionary<string, string>();
        ModifiedByDeviceId = modifiedByDeviceId?.Trim() is { Length: > 0 } m ? m : null;
        SyncConflictStatus = syncConflictStatus?.Trim() is { Length: > 0 } s ? s : null;
    }

    public List<string> PathComponents => PathComponentsFor(Path, Level);

    public static string FolderIdFor(StudyFolderLevel level, StudyFilingPath path)
    {
        var components = PathComponentsFor(path, level);
        var raw = string.Join("_", new[] { level.ToString().ToLowerInvariant() }.Concat(components));
        return $"folder_{StudyPathSanitizer.SanitizedPathComponent(raw)}";
    }

    public static List<string> PathComponentsFor(StudyFilingPath path, StudyFolderLevel level)
    {
        var values = new (StudyFolderLevel, string?)[]
        {
            (StudyFolderLevel.Type, path.Type),
            (StudyFolderLevel.Subject, path.Subject),
            (StudyFolderLevel.Chapter, path.Chapter),
            (StudyFolderLevel.Topic, path.Topic)
        };

        var result = new List<string>();
        foreach (var (lvl, val) in values)
        {
            var v = StudyFilingPath.Normalized(val);
            if (v is null) break;
            result.Add(v);
            if (lvl == level) break;
        }
        return result;
    }

    public static StudyFolderLevel? DeepestLevelIn(StudyFilingPath path)
    {
        if (path.Topic is not null) return StudyFolderLevel.Topic;
        if (path.Chapter is not null) return StudyFolderLevel.Chapter;
        if (path.Subject is not null) return StudyFolderLevel.Subject;
        if (path.Type is not null) return StudyFolderLevel.Type;
        return null;
    }

    public static StudyFolderLevel? LevelForDepth(int depth) => depth switch
    {
        0 => StudyFolderLevel.Type,
        1 => StudyFolderLevel.Subject,
        2 => StudyFolderLevel.Chapter,
        3 => StudyFolderLevel.Topic,
        _ => null
    };

    public static StudyFilingPath FilingPathFor(List<string> components)
    {
        return new StudyFilingPath(
            type: components.Count > 0 ? components[0] : null,
            subject: components.Count > 1 ? components[1] : null,
            chapter: components.Count > 2 ? components[2] : null,
            topic: components.Count > 3 ? components[3] : null
        );
    }

    public StudyFolderMetadata SyncSanitized(string modifiedByDeviceId)
    {
        return new StudyFolderMetadata
        {
            FolderId = FolderId, Name = Name, Level = Level, Path = Path,
            ParentFolderId = ParentFolderId, ChildFolderIds = ChildFolderIds, ItemIds = ItemIds,
            CreatedAt = CreatedAt, UpdatedAt = UpdatedAt, ColorToken = ColorToken,
            IsTrashed = IsTrashed, TrashedAt = TrashedAt, CustomProperties = CustomProperties,
            ModifiedByDeviceId = modifiedByDeviceId, SyncConflictStatus = SyncConflictStatus
        };
    }
}
