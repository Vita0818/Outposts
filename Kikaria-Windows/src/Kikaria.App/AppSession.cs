//
//  AppSession.cs
//  Kikaria-Windows
//
//  运行态单例:持有 KikariaAppState,提供页面所需的全部状态操作
//  (切换/创建/删除预设、知识点增删改、复习活动记录、学习设置),
//  持久化走 Kikaria.Core.AppStore。语义对齐 Apple 版 ContentView 的同名方法。
//

using Kikaria.Core;

namespace Kikaria.App;

/// <summary>创建预设结果。</summary>
public enum PresetCreationOutcome
{
    Success,
    MissingName,
    NoValidPoints
}

/// <summary>删除预设结果。</summary>
public enum PresetDeleteOutcome
{
    Deleted,
    BlockedLastPreset,
    NotFound
}

public sealed class AppSession
{
    public static AppSession Current { get; } = new();

    private AppSession()
    {
    }

    /// <summary>持久化状态(懒加载,首次访问时从 AppStore 读取)。</summary>
    public KikariaAppState State { get; private set; } = null!;

    private bool _loaded;

    /// <summary>确保状态已加载。</summary>
    public KikariaAppState EnsureLoaded()
    {
        if (!_loaded)
        {
            State = AppStore.Load();
            _loaded = true;
        }

        return State;
    }

    // ------------------------------------------------------------------
    // 当前预设快捷访问
    // ------------------------------------------------------------------

    public PresetStudyState CurrentState
    {
        get
        {
            EnsureLoaded();
            if (!State.PresetStates.TryGetValue(State.CurrentPresetID, out var state) || state is null)
            {
                state = AppStore.EmptyStudyState(State.CurrentPresetID, CurrentPreset.MarkdownText);
                State.PresetStates[State.CurrentPresetID] = state;
            }

            return state;
        }
    }

    public KnowledgePreset CurrentPreset
    {
        get
        {
            EnsureLoaded();
            return State.Presets.FirstOrDefault(preset => preset.Id == State.CurrentPresetID)
                ?? State.Presets.FirstOrDefault()
                ?? PresetLibrary.EmptyBuiltInPreset();
        }
    }

    public string CurrentPresetName => CurrentPreset.Name;

    public List<KnowledgePoint> Points => CurrentState.KnowledgePoints;

    /// <summary>全部标签(去重排序)。</summary>
    public List<string> AllTags() => StudyLogic.AllTags(Points);

    public int ReinforcedCount => Points.Count(point => point.ReinforcementCount > 0);

    public int MasteredCount => Points.Count(point => point.IsMastered);

    public int? CountdownDayCount => StudyLogic.CountdownDays(CurrentState.CountdownEndDate);

    public List<StudyActivityRecord> CurrentPresetActivityRecords => CurrentState.ActivityRecords;

    // ------------------------------------------------------------------
    // 页面间传参
    // ------------------------------------------------------------------

    /// <summary>ReviewPage 打开时的复习模式。</summary>
    public ReviewMode PendingReviewMode { get; set; } = ReviewMode.Normal;

    /// <summary>EditPresetPage / EditKnowledgePointPage 目标预设。</summary>
    public string? PendingPresetId { get; set; }

    /// <summary>EditKnowledgePointPage 目标知识点(null = 新建)。</summary>
    public Guid? PendingPointId { get; set; }

    /// <summary>是否从设置页重放新手引导(完成后回设置页而非首页)。</summary>
    public bool OnboardingReplay { get; set; }

    // ------------------------------------------------------------------
    // 预设操作
    // ------------------------------------------------------------------

    /// <summary>切换当前预设;目标无可用状态时失败。</summary>
    public bool SwitchPreset(string presetId)
    {
        EnsureLoaded();
        var preset = State.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset is null)
        {
            return false;
        }

        if (!State.PresetStates.TryGetValue(preset.Id, out var state) || state is null)
        {
            state = AppStore.InitialStudyState(preset);
            if (state is null)
            {
                return false;
            }

            State.PresetStates[preset.Id] = state;
        }

        state.SelectedTags = state.ValidSelectedTags();
        State.CurrentPresetID = preset.Id;
        Save();
        return true;
    }

    /// <summary>创建自定义预设并切换为当前。</summary>
    public PresetCreationOutcome CreatePreset(string name, string category, string markdownText)
    {
        EnsureLoaded();
        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            return PresetCreationOutcome.MissingName;
        }

        var trimmedMarkdown = markdownText.Trim();
        var trimmedCategory = category.Trim();
        var parsedPoints = MarkdownParser.TryParseMarkdown(trimmedMarkdown, DateTime.Now);
        if (parsedPoints is null)
        {
            return PresetCreationOutcome.NoValidPoints;
        }

        var preset = new KnowledgePreset
        {
            Id = "user-" + Guid.NewGuid().ToString("N"),
            Name = trimmedName,
            Subtitle = "自定义知识点",
            Description = "",
            Category = trimmedCategory.Length == 0 ? "自定义" : trimmedCategory,
            MarkdownText = trimmedMarkdown,
            IsBuiltIn = false
        };

        State.Presets.Add(preset);
        State.PresetStates[preset.Id] = new PresetStudyState
        {
            PresetId = preset.Id,
            KnowledgePoints = parsedPoints,
            MarkdownText = trimmedMarkdown,
            SelectedTags = new HashSet<string>(),
            DailyReviewRecords = new Dictionary<string, DailyReviewRecord>(),
            ActivityRecords = new List<StudyActivityRecord>(),
            DailyGoal = 20,
            CountdownStartDate = null,
            CountdownEndDate = null,
            NotificationsEnabled = false,
            NotificationTime = StudyLogic.DefaultNotificationTime(),
            DangerPercent = 80
        };
        State.CurrentPresetID = preset.Id;
        Save();
        return PresetCreationOutcome.Success;
    }

    /// <summary>删除预设(至少保留一个;删的是当前预设时切到剩余第一个)。</summary>
    public PresetDeleteOutcome DeletePreset(string presetId)
    {
        EnsureLoaded();
        var preset = State.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset is null)
        {
            return PresetDeleteOutcome.NotFound;
        }

        if (State.Presets.Count <= 1)
        {
            return PresetDeleteOutcome.BlockedLastPreset;
        }

        var deletedName = preset.Name;
        State.Presets.RemoveAll(p => p.Id == presetId);
        State.PresetStates.Remove(presetId);

        if (State.CurrentPresetID == presetId)
        {
            State.CurrentPresetID = State.Presets[0].Id;
        }

        Save();
        _lastDeletedPresetName = deletedName;
        return PresetDeleteOutcome.Deleted;
    }

    private string? _lastDeletedPresetName;

    /// <summary>最近一次删除的预设名(用于 Toast 文案)。</summary>
    public string? LastDeletedPresetName => _lastDeletedPresetName;

    /// <summary>编辑预设元数据(名称 / 分类)。</summary>
    public void UpdatePresetMetadata(string presetId, string name, string category)
    {
        EnsureLoaded();
        var preset = State.Presets.FirstOrDefault(p => p.Id == presetId);
        if (preset is null)
        {
            return;
        }

        var trimmedName = name.Trim();
        var trimmedCategory = category.Trim();
        if (trimmedName.Length > 0)
        {
            preset.Name = trimmedName;
        }

        preset.Category = trimmedCategory.Length == 0 ? "自定义" : trimmedCategory;
        Save();
    }

    /// <summary>读取某预设的学习状态(编辑页也可编辑非当前预设)。</summary>
    public PresetStudyState StateForPreset(string presetId)
    {
        EnsureLoaded();
        if (State.PresetStates.TryGetValue(presetId, out var state) && state is not null)
        {
            return state;
        }

        var preset = State.Presets.FirstOrDefault(p => p.Id == presetId);
        var created = AppStore.InitialStudyState(preset ?? PresetLibrary.EmptyBuiltInPreset())
            ?? AppStore.EmptyStudyState(presetId, preset?.MarkdownText ?? "");
        State.PresetStates[presetId] = created;
        return created;
    }

    // ------------------------------------------------------------------
    // 知识点操作
    // ------------------------------------------------------------------

    /// <summary>新增或更新知识点;返回变更后的知识点数(用于"已更新 N 个知识点")。</summary>
    public void UpsertKnowledgePoint(string presetId, KnowledgePoint point)
    {
        EnsureLoaded();
        var state = StateForPreset(presetId);
        var index = state.KnowledgePoints.FindIndex(p => p.Id == point.Id);
        if (index >= 0)
        {
            state.KnowledgePoints[index] = point;
        }
        else
        {
            state.KnowledgePoints.Add(point);
        }

        state.MarkdownText = MarkdownParser.MarkdownText(state.KnowledgePoints);
        Save();
    }

    /// <summary>删除知识点(连带当日复习次数与活动记录,并校正选中标签)。</summary>
    public void DeleteKnowledgePoint(string presetId, Guid pointId)
    {
        EnsureLoaded();
        var state = StateForPreset(presetId);
        state.KnowledgePoints.RemoveAll(p => p.Id == pointId);
        state.DailyReviewRecords.Remove(pointId.ToString());
        state.ActivityRecords.RemoveAll(record => record.PointId == pointId);
        state.SelectedTags = state.ValidSelectedTags();
        state.MarkdownText = MarkdownParser.MarkdownText(state.KnowledgePoints);
        Save();
    }

    // ------------------------------------------------------------------
    // 复习活动
    // ------------------------------------------------------------------

    /// <summary>记录一条学习活动。</summary>
    public void RecordActivity(StudyActivityType type, KnowledgePoint point)
    {
        EnsureLoaded();
        CurrentState.ActivityRecords.Add(new StudyActivityRecord
        {
            Id = Guid.NewGuid(),
            PresetId = State.CurrentPresetID,
            Date = DateTime.Now,
            Type = type,
            PointId = point.Id,
            PointTitle = point.Title
        });
        Save();
    }

    /// <summary>今日某知识点复习次数。</summary>
    public int TodayReviewCountFor(Guid pointId)
    {
        EnsureLoaded();
        var key = pointId.ToString();
        if (CurrentState.DailyReviewRecords.TryGetValue(key, out var record) &&
            StudyLogic.IsSameDay(record.Date, DateTime.Now))
        {
            return record.Count;
        }

        return 0;
    }

    /// <summary>查看答案后今日计数 +1。</summary>
    public void IncrementTodayReviewCount(Guid pointId)
    {
        EnsureLoaded();
        var now = DateTime.Now;
        var key = pointId.ToString();
        if (CurrentState.DailyReviewRecords.TryGetValue(key, out var record) &&
            StudyLogic.IsSameDay(record.Date, now))
        {
            record.Date = now;
            record.Count += 1;
        }
        else
        {
            CurrentState.DailyReviewRecords[key] = new DailyReviewRecord { Date = now, Count = 1 };
        }

        Save();
    }

    // ------------------------------------------------------------------
    // 学习设置(作用于当前预设)
    // ------------------------------------------------------------------

    public void UpdateDailyGoal(int value)
    {
        EnsureLoaded();
        CurrentState.DailyGoal = StudyLogic.ClampGoal(value);
        Save();
    }

    public void UpdateCountdown(DateTime? start, DateTime? end)
    {
        EnsureLoaded();
        CurrentState.CountdownStartDate = start;
        CurrentState.CountdownEndDate = end;
        Save();
    }

    public void UpdateDangerPercent(int value)
    {
        EnsureLoaded();
        CurrentState.DangerPercent = StudyLogic.ClampDanger(value);
        Save();
    }

    /// <summary>通知开关:Windows 版未接系统通知,仅保存状态。</summary>
    public void UpdateNotificationsEnabled(bool value)
    {
        EnsureLoaded();
        CurrentState.NotificationsEnabled = value;
        Save();
    }

    public void UpdateNotificationTime(DateTime value)
    {
        EnsureLoaded();
        CurrentState.NotificationTime = value;
        Save();
    }

    public void UpdateSelectedTags(HashSet<string> tags)
    {
        EnsureLoaded();
        CurrentState.SelectedTags = tags;
        Save();
    }

    // ------------------------------------------------------------------
    // 引导与资料
    // ------------------------------------------------------------------

    public void CompleteOnboarding()
    {
        EnsureLoaded();
        State.HasCompletedOnboarding = true;
        Save();
    }

    public void CompleteProfileSetup(string displayName, string userHandle)
    {
        EnsureLoaded();
        State.HasCompletedProfileSetup = true;
        SaveProfile(displayName, userHandle);
    }

    public void SaveProfile(string displayName, string userHandle)
    {
        EnsureLoaded();
        var trimmedName = displayName.Trim();
        var trimmedHandle = userHandle.Trim().TrimStart('@');
        State.UserProfile.DisplayName = trimmedName.Length == 0 ? "Vita" : trimmedName;
        State.UserProfile.UserHandle = trimmedHandle.Length == 0 ? "vita_0818" : trimmedHandle;
        Save();
    }

    /// <summary>文字头像首字母。</summary>
    public string AvatarInitial()
    {
        EnsureLoaded();
        var name = State.UserProfile.DisplayName;
        return name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "V";
    }

    // ------------------------------------------------------------------
    // 持久化
    // ------------------------------------------------------------------

    public void Save()
    {
        if (!_loaded || State is null)
        {
            return;
        }

        AppStore.Save(State);
    }
}
