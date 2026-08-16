//
//  Models.cs
//  Kikaria-Windows
//
//  数据模型 + JSON 序列化,移植自 Kikaria-Apple 的 KnowledgePoint.swift / StudyTracking.swift
//  以及 ContentView.swift 中的 PresetStudyState / KikariaAppState / UserProfile。
//

using System.Text.Json.Serialization;

namespace Kikaria.Core;

/// <summary>学习活动类型,与 Apple 版 StudyActivityType 一致。</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StudyActivityType
{
    ViewedHint,
    ReviewedAnswer,
    MarkedMastered,
    RemovedMastered,
    AddedReinforcement,
    RemovedReinforcement
}

/// <summary>复习模式:普通 / 重点集锦 / 已掌握。</summary>
public enum ReviewMode
{
    Normal,
    Reinforcement,
    Mastered
}

/// <summary>单条学习活动记录(查看提示 / 查看答案 / 掌握 / 移出 / 加入重点 / 移出重点)。</summary>
public sealed class StudyActivityRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PresetId { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public StudyActivityType Type { get; set; }
    public Guid PointId { get; set; }
    public string PointTitle { get; set; } = "";
}

/// <summary>知识点当日复习次数记录。键为知识点 id(GUID 字符串)。</summary>
public sealed class DailyReviewRecord
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int Count { get; set; }
}

/// <summary>
/// 知识点。移植自 Apple 版 KnowledgePoint:
/// isReinforced 为派生属性(reinforcementCount &gt; 0),count==0 时 lastReinforcedAt 恒为 null。
/// </summary>
public sealed class KnowledgePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public string Hint { get; set; } = "";
    public string Content { get; set; } = "";
    public int ReinforcementCount { get; set; }
    public DateTime? LastReinforcedAt { get; set; }
    public bool IsMastered { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>派生属性:加入过重点集锦。序列化时忽略,行为与 Apple 版 encode 的 isReinforced 一致。</summary>
    [JsonIgnore]
    public bool IsReinforced => ReinforcementCount > 0;

    /// <summary>反序列化 / 迁移后归一化:count 夹取 ≥0、count==0 清空 lastReinforcedAt。</summary>
    public void NormalizeReinforcement()
    {
        if (ReinforcementCount < 0)
        {
            ReinforcementCount = 0;
        }

        if (ReinforcementCount == 0)
        {
            LastReinforcedAt = null;
        }
    }

    /// <summary>加入重点集锦:count+1,返回新 count。</summary>
    public int AddReinforcement(DateTime at)
    {
        ReinforcementCount = Math.Max(0, ReinforcementCount) + 1;
        LastReinforcedAt = at;
        UpdatedAt = at;
        return ReinforcementCount;
    }

    /// <summary>移出重点集锦:清零,不动掌握状态。</summary>
    public void ClearReinforcement(DateTime at)
    {
        ReinforcementCount = 0;
        LastReinforcedAt = null;
        UpdatedAt = at;
    }

    /// <summary>标记掌握:isMastered=true 且同时清空重点。</summary>
    public void MarkMastered(DateTime at)
    {
        IsMastered = true;
        ClearReinforcement(at);
    }

    /// <summary>移出已掌握:只清 isMastered。</summary>
    public void UnmarkMastered(DateTime at)
    {
        IsMastered = false;
        UpdatedAt = at;
    }
}

/// <summary>知识点预设(内置或用户上传),移植自 Apple 版 KnowledgePreset。</summary>
public sealed class KnowledgePreset
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "自定义";
    public string MarkdownText { get; set; } = "";
    public bool IsBuiltIn { get; set; }

    /// <summary>可解析出的知识点数量;解析失败为 0。</summary>
    [JsonIgnore]
    public int KnowledgePointCount
    {
        get
        {
            try
            {
                return MarkdownParser.ParseMarkdown(MarkdownText, DateTime.Now).Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}

/// <summary>用户资料。Windows 版头像为文字头像,AvatarImageData 仅保留字段兼容。</summary>
public sealed class UserProfile
{
    public string DisplayName { get; set; } = "Vita";
    public string UserHandle { get; set; } = "vita_0818";
    public byte[]? AvatarImageData { get; set; }

    public UserProfile Clone() => new()
    {
        DisplayName = DisplayName,
        UserHandle = UserHandle,
        AvatarImageData = AvatarImageData
    };
}

/// <summary>单个预设的完整学习状态,移植自 Apple 版 PresetStudyState。</summary>
public sealed class PresetStudyState
{
    public string PresetId { get; set; } = "";
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();
    public string MarkdownText { get; set; } = "";
    public HashSet<string> SelectedTags { get; set; } = new();
    public Dictionary<string, DailyReviewRecord> DailyReviewRecords { get; set; } = new();
    public List<StudyActivityRecord> ActivityRecords { get; set; } = new();
    public int DailyGoal { get; set; } = 20;
    public DateTime? CountdownStartDate { get; set; }
    public DateTime? CountdownEndDate { get; set; }
    public bool NotificationsEnabled { get; set; }
    public DateTime NotificationTime { get; set; } = StudyLogic.DefaultNotificationTime();
    public int DangerPercent { get; set; } = 80;

    /// <summary>夹取并归一化(每日目标 / 安全线 1-100)。</summary>
    public void Normalize()
    {
        DailyGoal = StudyLogic.ClampGoal(DailyGoal);
        DangerPercent = StudyLogic.ClampDanger(DangerPercent);
        if (NotificationTime == default)
        {
            NotificationTime = StudyLogic.DefaultNotificationTime();
        }

        foreach (var point in KnowledgePoints)
        {
            point.NormalizeReinforcement();
        }
    }

    /// <summary>过滤掉已不存在知识点对应的选中标签。</summary>
    public HashSet<string> ValidSelectedTags()
    {
        var available = new HashSet<string>();
        foreach (var point in KnowledgePoints)
        {
            foreach (var tag in point.Tags)
            {
                available.Add(tag);
            }
        }

        return new HashSet<string>(SelectedTags.Where(tag => available.Contains(tag)));
    }
}

/// <summary>应用全局状态,移植自 Apple 版 KikariaAppState(schemaVersion=4)。</summary>
public sealed class KikariaAppState
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<KnowledgePreset> Presets { get; set; } = new();
    public Dictionary<string, PresetStudyState> PresetStates { get; set; } = new();
    public string CurrentPresetID { get; set; } = "";
    public UserProfile UserProfile { get; set; } = new();
    public bool HasCompletedProfileSetup { get; set; }
    public bool HasCompletedOnboarding { get; set; }
}
