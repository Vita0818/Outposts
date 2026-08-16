//
//  AppStore.cs
//  Kikaria-Windows
//
//  JSON 文件持久化(%LOCALAPPDATA%\Kikaria\appState.json)与迁移合并逻辑,
//  移植自 Kikaria-Apple 的 ContentView.swift(loadAppState / applyLoadedAppState /
//  ensurePresetStatesExist / mergedPresets / removeRetiredBuiltInPresetsIfNeeded)。
//

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kikaria.Core;

public static class AppStore
{
    /// <summary>状态文件目录:%LOCALAPPDATA%\Kikaria。</summary>
    public static string StateDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kikaria");

    /// <summary>状态文件路径:%LOCALAPPDATA%\Kikaria\appState.json。</summary>
    public static string StateFilePath => Path.Combine(StateDirectory, "appState.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>读取状态文件;不存在或损坏时返回默认状态。读取后总是执行迁移合并。</summary>
    public static KikariaAppState Load()
    {
        KikariaAppState? state = null;

        if (File.Exists(StateFilePath))
        {
            try
            {
                var json = File.ReadAllText(StateFilePath);
                state = JsonSerializer.Deserialize<KikariaAppState>(json, SerializerOptions);
            }
            catch (Exception)
            {
                // 解码失败按 Apple 版行为:丢弃并回落到默认状态。
                state = null;
            }
        }

        if (state is null)
        {
            state = CreateDefault();
        }

        EnsurePresetStates(state);
        return state;
    }

    /// <summary>默认状态:内置预设 + 第一个内置预设为当前预设。</summary>
    public static KikariaAppState CreateDefault()
    {
        var presets = PresetLibrary.LoadBuiltInPresets();
        return new KikariaAppState
        {
            SchemaVersion = KikariaAppState.CurrentSchemaVersion,
            Presets = presets,
            PresetStates = new Dictionary<string, PresetStudyState>(),
            CurrentPresetID = presets.Count > 0 ? presets[0].Id : "",
            UserProfile = new UserProfile(),
            HasCompletedProfileSetup = false,
            HasCompletedOnboarding = false
        };
    }

    /// <summary>保存状态(临时文件 + 原子替换)。</summary>
    public static void Save(KikariaAppState state)
    {
        try
        {
            Directory.CreateDirectory(StateDirectory);
            var json = JsonSerializer.Serialize(state, SerializerOptions);
            var tempPath = StateFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, StateFilePath, overwrite: true);
        }
        catch (Exception)
        {
            // 保存失败静默(与 Apple 版 fail-soft 一致)。
        }
    }

    /// <summary>
    /// 迁移合并:
    /// 1. 存量非内置预设合并到最新内置预设之后(按 id 去重);
    /// 2. 丢弃未知预设的 study state;
    /// 3. 为缺 state 的预设建立初始 state;
    /// 4. 内置预设 markdownText 与内置文件不一致时重置该预设状态;
    /// 5. currentPresetID 无效时回退第一个。
    /// </summary>
    public static void EnsurePresetStates(KikariaAppState state)
    {
        var builtIns = PresetLibrary.LoadBuiltInPresets();

        if (state.Presets.Count == 0)
        {
            state.Presets = new List<KnowledgePreset>(builtIns);
        }
        else
        {
            // mergedPresets:内置在前,存量自定义追加。
            var merged = new List<KnowledgePreset>(builtIns);
            var existingIds = new HashSet<string>(builtIns.Select(preset => preset.Id));

            foreach (var stored in state.Presets)
            {
                if (!stored.IsBuiltIn && !existingIds.Contains(stored.Id))
                {
                    merged.Add(stored);
                    existingIds.Add(stored.Id);
                }
            }

            state.Presets = merged;
        }

        // 过滤指向不存在预设的 state。
        var validIds = new HashSet<string>(state.Presets.Select(preset => preset.Id));
        var filteredStates = new Dictionary<string, PresetStudyState>();
        foreach (var (key, value) in state.PresetStates)
        {
            if (validIds.Contains(key))
            {
                filteredStates[key] = value;
            }
        }

        state.PresetStates = filteredStates;

        // 为缺 state 的预设建初始 state;内置预设内容变化时重置。
        foreach (var preset in state.Presets)
        {
            state.PresetStates.TryGetValue(preset.Id, out var existing);
            var needsReset = preset.IsBuiltIn &&
                existing is not null &&
                existing.MarkdownText != preset.MarkdownText;

            if (existing is null || needsReset)
            {
                var initial = InitialStudyState(preset);
                if (initial is not null)
                {
                    state.PresetStates[preset.Id] = initial;
                }
                else if (existing is null)
                {
                    // 解析失败也保留一个空状态,避免 UI 悬空。
                    state.PresetStates[preset.Id] = EmptyStudyState(preset.Id, preset.MarkdownText);
                }
            }
        }

        // currentPresetID 回退。
        if (state.Presets.All(preset => preset.Id != state.CurrentPresetID))
        {
            state.CurrentPresetID = state.Presets.Count > 0 ? state.Presets[0].Id : "";
        }

        foreach (var value in state.PresetStates.Values)
        {
            value.Normalize();
        }
    }

    /// <summary>从预设 Markdown 解析出初始学习状态;解析失败返回 null。</summary>
    public static PresetStudyState? InitialStudyState(KnowledgePreset preset)
    {
        var points = MarkdownParser.TryParseMarkdown(preset.MarkdownText, DateTime.Now);
        if (points is null)
        {
            return null;
        }

        return new PresetStudyState
        {
            PresetId = preset.Id,
            KnowledgePoints = points,
            MarkdownText = preset.MarkdownText,
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
    }

    /// <summary>空状态兜底(仅在预设无法解析时使用)。</summary>
    public static PresetStudyState EmptyStudyState(string presetId, string markdownText) => new()
    {
        PresetId = presetId,
        KnowledgePoints = new List<KnowledgePoint>(),
        MarkdownText = markdownText,
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
}
