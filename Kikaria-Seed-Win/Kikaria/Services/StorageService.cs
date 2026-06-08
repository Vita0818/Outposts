using Kikaria.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Kikaria.Services
{
    public class StorageService
    {
        private readonly string _appDataPath;
        private readonly string _appStatePath;
        private readonly string _presetsDirectory;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public StorageService()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kikaria");
            Directory.CreateDirectory(_appDataPath);
            _appStatePath = Path.Combine(_appDataPath, "app_state.json");
            _presetsDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Presets");
        }

        public KikariaAppState LoadAppState()
        {
            if (!File.Exists(_appStatePath))
            {
                var defaultPresets = LoadBuiltInPresets();
                return KikariaAppState.CreateDefault(defaultPresets);
            }

            try
            {
                var json = File.ReadAllText(_appStatePath);
                return JsonSerializer.Deserialize<KikariaAppState>(json) ?? KikariaAppState.CreateDefault(LoadBuiltInPresets());
            }
            catch
            {
                return KikariaAppState.CreateDefault(LoadBuiltInPresets());
            }
        }

        public void SaveAppState(KikariaAppState state)
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(_appStatePath, json);
        }

        public List<KnowledgePreset> LoadBuiltInPresets()
        {
            var presets = new List<KnowledgePreset>();
            if (!Directory.Exists(_presetsDirectory)) return presets;

            var markdownFiles = Directory.GetFiles(_presetsDirectory, "*.md", SearchOption.TopDirectoryOnly);
            foreach (var file in markdownFiles.OrderBy(f => Path.GetFileName(f)))
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var markdownText = File.ReadAllText(file);
                    presets.Add(new KnowledgePreset(
                        id: $"builtin-{fileName.ToLowerInvariant()}",
                        name: fileName,
                        subtitle: $"{fileName}知识点",
                        description: $"由内置 Markdown 文件「Presets/{Path.GetFileName(file)}」提供的知识点预设。",
                        category: "内置预设",
                        markdownText: markdownText,
                        isBuiltIn: true
                    ));
                }
                catch
                {
                    // Skip invalid presets
                }
            }

            return presets;
        }
    }

    public class DailyReviewRecord
    {
        public DateTimeOffset Date { get; set; }
        public int Count { get; set; }
    }

    public class PresetStudyState
    {
        public string PresetId { get; set; } = string.Empty;
        public List<KnowledgePoint> KnowledgePoints { get; set; } = new();
        public string MarkdownText { get; set; } = string.Empty;
        public HashSet<string> SelectedTags { get; set; } = new();
        public Dictionary<Guid, DailyReviewRecord> DailyReviewRecords { get; set; } = new();
        public List<StudyActivityRecord> ActivityRecords { get; set; } = new();
        public int DailyGoal { get; set; } = 20;
        public DateTimeOffset? CountdownStartDate { get; set; }
        public DateTimeOffset? CountdownEndDate { get; set; }
        public bool NotificationsEnabled { get; set; }
        public DateTimeOffset NotificationTime { get; set; } = DefaultNotificationTime();
        public int DangerPercent { get; set; } = 80;

        public PresetStudyState() { }

        public PresetStudyState(
            string presetId,
            List<KnowledgePoint> knowledgePoints,
            string markdownText,
            HashSet<string> selectedTags,
            Dictionary<Guid, DailyReviewRecord> dailyReviewRecords,
            List<StudyActivityRecord> activityRecords,
            int dailyGoal,
            DateTimeOffset? countdownStartDate = null,
            DateTimeOffset? countdownEndDate = null,
            bool notificationsEnabled = false,
            DateTimeOffset? notificationTime = null,
            int dangerPercent = 80)
        {
            PresetId = presetId;
            KnowledgePoints = knowledgePoints;
            MarkdownText = markdownText;
            SelectedTags = selectedTags;
            DailyReviewRecords = dailyReviewRecords;
            ActivityRecords = activityRecords;
            DailyGoal = dailyGoal;
            CountdownStartDate = countdownStartDate;
            CountdownEndDate = countdownEndDate;
            NotificationsEnabled = notificationsEnabled;
            NotificationTime = notificationTime ?? DefaultNotificationTime();
            DangerPercent = Math.Clamp(dangerPercent, 1, 100);
        }

        public static DateTimeOffset DefaultNotificationTime()
        {
            var now = DateTimeOffset.Now;
            return new DateTimeOffset(now.Year, now.Month, now.Day, 21, 0, 0, now.Offset);
        }
    }

    public class UserProfile
    {
        public string DisplayName { get; set; } = "Vita";
        public string UserHandle { get; set; } = "vita_0818";
        public string AvatarSystemName { get; set; } = "person.crop.circle.fill";
        public byte[]? AvatarImageData { get; set; }
    }

    public class KikariaAppState
    {
        public const int CurrentSchemaVersion = 4;
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<KnowledgePreset> Presets { get; set; } = new();
        public Dictionary<string, PresetStudyState> PresetStates { get; set; } = new();
        public string CurrentPresetId { get; set; } = string.Empty;
        public UserProfile UserProfile { get; set; } = new();
        public bool HasCompletedProfileSetup { get; set; }
        public bool HasCompletedOnboarding { get; set; }

        public KikariaAppState() { }

        public KikariaAppState(
            int schemaVersion,
            List<KnowledgePreset> presets,
            Dictionary<string, PresetStudyState> presetStates,
            string currentPresetId,
            UserProfile userProfile,
            bool hasCompletedProfileSetup,
            bool hasCompletedOnboarding)
        {
            SchemaVersion = schemaVersion;
            Presets = presets;
            PresetStates = presetStates;
            CurrentPresetId = currentPresetId;
            UserProfile = userProfile;
            HasCompletedProfileSetup = hasCompletedProfileSetup;
            HasCompletedOnboarding = hasCompletedOnboarding;
        }

        public static KikariaAppState CreateDefault(List<KnowledgePreset> builtInPresets)
        {
            if (!builtInPresets.Any())
            {
                builtInPresets.Add(new KnowledgePreset(
                    id: "builtin-empty",
                    name: "内置预设",
                    subtitle: "内置知识点",
                    description: "未找到内置 Markdown 预设。",
                    category: "内置预设",
                    markdownText: "",
                    isBuiltIn: true
                ));
            }

            var defaultPreset = builtInPresets.First();
            var defaultState = new PresetStudyState(
                presetId: defaultPreset.Id,
                knowledgePoints: KnowledgePoint.ParseMarkdown(defaultPreset.MarkdownText),
                markdownText: defaultPreset.MarkdownText,
                selectedTags: new HashSet<string>(),
                dailyReviewRecords: new Dictionary<Guid, DailyReviewRecord>(),
                activityRecords: new List<StudyActivityRecord>(),
                dailyGoal: 20
            );

            return new KikariaAppState(
                schemaVersion: CurrentSchemaVersion,
                presets: builtInPresets,
                presetStates: new Dictionary<string, PresetStudyState> { { defaultPreset.Id, defaultState } },
                currentPresetId: defaultPreset.Id,
                userProfile: new UserProfile(),
                hasCompletedProfileSetup: false,
                hasCompletedOnboarding: false
            );
        }

        public PresetStudyState CurrentPresetState => PresetStates.TryGetValue(CurrentPresetId, out var state) 
            ? state 
            : PresetStates.Values.FirstOrDefault() ?? new PresetStudyState();
    }
}