using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kikaria.Models;
using Windows.Storage;

namespace Kikaria.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static MainViewModel? _instance;
        public static MainViewModel Instance => _instance ??= new MainViewModel();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private static readonly HashSet<string> RetiredPresetIds = new()
        {
            "advanced-math",
            "college-english",
            "college-physics",
            "anatomy",
            "template",
            "builtin-university-physics",
            "builtin-college-english-band4",
            "builtin-calculus",
            "builtin-discrete-math"
        };

        [ObservableProperty]
        private ObservableCollection<KnowledgePreset> presets;

        [ObservableProperty]
        private ObservableCollection<KnowledgePoint> knowledgePoints;

        [ObservableProperty]
        private string markdownText;

        [ObservableProperty]
        private UserProfile userProfile;

        [ObservableProperty]
        private HashSet<string> selectedTags;

        [ObservableProperty]
        private Dictionary<Guid, DailyReviewRecord> dailyReviewRecords;

        [ObservableProperty]
        private List<StudyActivityRecord> activityRecords;

        [ObservableProperty]
        private Dictionary<string, PresetStudyState> presetStates;

        [ObservableProperty]
        private string currentPresetID;

        [ObservableProperty]
        private int dailyGoal;

        [ObservableProperty]
        private DateTime? countdownStartDate;

        [ObservableProperty]
        private DateTime? countdownEndDate;

        [ObservableProperty]
        private bool notificationsEnabled;

        [ObservableProperty]
        private DateTime notificationTime;

        [ObservableProperty]
        private int dangerPercent;

        [ObservableProperty]
        private bool hasCompletedProfileSetup;

        [ObservableProperty]
        private bool hasCompletedOnboarding;

        [ObservableProperty]
        private AppRoute? currentRoute;

        [ObservableProperty]
        private List<AppRoute> navigationStack;

        [ObservableProperty]
        private bool hasLoadedInitialPresetState;

        [ObservableProperty]
        private bool isApplyingPresetState;

        private CancellationTokenSource? pendingStudyStatePersistenceWorkItem;

        public MainViewModel()
        {
            Presets = new ObservableCollection<KnowledgePreset>();
            KnowledgePoints = new ObservableCollection<KnowledgePoint>();
            MarkdownText = string.Empty;
            UserProfile = new UserProfile();
            SelectedTags = new HashSet<string>();
            DailyReviewRecords = new Dictionary<Guid, DailyReviewRecord>();
            ActivityRecords = new List<StudyActivityRecord>();
            PresetStates = new Dictionary<string, PresetStudyState>();
            CurrentPresetID = KnowledgePreset.DefaultPresetID;
            DailyGoal = 20;
            CountdownStartDate = null;
            CountdownEndDate = null;
            NotificationsEnabled = false;
            NotificationTime = DateTime.Today.AddHours(21);
            DangerPercent = 80;
            HasCompletedProfileSetup = false;
            HasCompletedOnboarding = false;
            CurrentRoute = null;
            NavigationStack = new List<AppRoute>();
        }

        public List<string> AllTags
        {
            get
            {
                return KnowledgePoints
                    .SelectMany(p => p.Tags)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
        }

        public string SelectedScopeCountText
        {
            get
            {
                if (SelectedTags.Count == 0)
                    return $"{KnowledgePoints.Count} points";
                int filtered = KnowledgePoints.Count(p => p.Tags.Any(t => SelectedTags.Contains(t)));
                return $"{filtered} of {KnowledgePoints.Count} points";
            }
        }

        public int ReinforcedCount => KnowledgePoints.Count(p => p.IsReinforced);

        public int MasteredCount => KnowledgePoints.Count(p => p.IsMastered);

        public int? CountdownDayCount
        {
            get
            {
                if (!CountdownEndDate.HasValue)
                    return null;
                int days = (int)(CountdownEndDate.Value - DateTime.Today).TotalDays;
                return Math.Max(0, days);
            }
        }

        public KnowledgePreset? CurrentPreset
        {
            get => Presets.FirstOrDefault(p => p.Id == CurrentPresetID);
        }

        public List<StudyActivityRecord> CurrentPresetActivityRecords
        {
            get => ActivityRecords.Where(r => r.PresetId == CurrentPresetID).ToList();
        }

        public int TodayReviewedAnswerCount
        {
            get
            {
                var today = DateTime.Today;
                return ActivityRecords.Count(r =>
                    r.PresetId == CurrentPresetID &&
                    r.Date.Date == today &&
                    r.Type == StudyActivityType.ReviewedAnswer);
            }
        }

        public int TodayViewedHintCount
        {
            get
            {
                var today = DateTime.Today;
                return ActivityRecords.Count(r =>
                    r.PresetId == CurrentPresetID &&
                    r.Date.Date == today &&
                    r.Type == StudyActivityType.ViewedHint);
            }
        }

        public int TodayMarkedMasteredCount
        {
            get
            {
                var today = DateTime.Today;
                return ActivityRecords
                    .Where(r =>
                        r.PresetId == CurrentPresetID &&
                        r.Date.Date == today &&
                        r.Type == StudyActivityType.MarkedMastered)
                    .Select(r => r.PointId)
                    .Distinct()
                    .Count();
            }
        }

        public string HomeDateTitle
        {
            get
            {
                var now = DateTime.Now;
                var culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
                string suffix = OrdinalSuffix(now.Day);
                return now.ToString("MMM d", culture) + suffix;
            }
        }

        public string HomeDaysLeftText
        {
            get
            {
                return $"{CountdownDayCount?.ToString() ?? "--"} Days Left";
            }
        }

        public string HomeProgressText
        {
            get
            {
                return $"{TodayMarkedMasteredCount}/{DailyGoal}";
            }
        }

        public void LoadInitialPresetState()
        {
            var builtInPresets = KnowledgePreset.LoadBuiltInPresets();

            foreach (var preset in builtInPresets)
            {
                if (!Presets.Any(p => p.Id == preset.Id))
                    Presets.Add(preset);
            }

            foreach (var preset in Presets)
            {
                if (!PresetStates.ContainsKey(preset.Id))
                {
                    var points = KnowledgePoint.ParseMarkdown(preset.MarkdownText);
                    PresetStates[preset.Id] = new PresetStudyState(preset.Id, points, preset.MarkdownText);
                }
            }

            if (!PresetStates.ContainsKey(CurrentPresetID))
            {
                var fallback = Presets.FirstOrDefault();
                if (fallback != null)
                    CurrentPresetID = fallback.Id;
            }

            if (PresetStates.TryGetValue(CurrentPresetID, out var state))
            {
                RestorePresetState(state);
            }
        }

        public bool SwitchToPreset(KnowledgePreset preset)
        {
            if (preset.Id == CurrentPresetID)
                return false;

            SaveCurrentPresetState();

            CurrentPresetID = preset.Id;

            if (PresetStates.TryGetValue(preset.Id, out var state))
            {
                RestorePresetState(state);
            }
            else
            {
                var points = KnowledgePoint.ParseMarkdown(preset.MarkdownText);
                var newState = new PresetStudyState(preset.Id, points, preset.MarkdownText);
                PresetStates[preset.Id] = newState;
                RestorePresetState(newState);
            }

            OnPropertyChanged(nameof(CurrentPreset));
            OnPropertyChanged(nameof(CurrentPresetActivityRecords));
            OnPropertyChanged(nameof(AllTags));
            OnPropertyChanged(nameof(SelectedScopeCountText));
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(HomeProgressText));

            return true;
        }

        public void SaveCurrentPresetState()
        {
            var state = new PresetStudyState(
                CurrentPresetID,
                KnowledgePoints.ToList(),
                MarkdownText,
                new HashSet<string>(SelectedTags),
                new Dictionary<Guid, DailyReviewRecord>(DailyReviewRecords),
                new List<StudyActivityRecord>(ActivityRecords.Where(r => r.PresetId == CurrentPresetID)),
                DailyGoal,
                CountdownStartDate,
                CountdownEndDate,
                NotificationsEnabled,
                NotificationTime.TimeOfDay,
                DangerPercent
            );
            PresetStates[CurrentPresetID] = state;
        }

        public void RestorePresetState(PresetStudyState state)
        {
            KnowledgePoints.Clear();
            foreach (var point in state.KnowledgePoints)
            {
                KnowledgePoints.Add(point);
            }

            MarkdownText = state.MarkdownText;
            SelectedTags = new HashSet<string>(state.SelectedTags);
            DailyReviewRecords = new Dictionary<Guid, DailyReviewRecord>(state.DailyReviewRecords);

            ActivityRecords = ActivityRecords
                .Where(r => r.PresetId != CurrentPresetID)
                .Concat(state.ActivityRecords)
                .ToList();

            DailyGoal = state.DailyGoal;
            CountdownStartDate = state.CountdownStartDate;
            CountdownEndDate = state.CountdownEndDate;
            NotificationsEnabled = state.NotificationsEnabled;
            NotificationTime = DateTime.Today.Add(state.NotificationTime);
            DangerPercent = state.DangerPercent;

            OnPropertyChanged(nameof(SelectedTags));
            OnPropertyChanged(nameof(DailyReviewRecords));
            OnPropertyChanged(nameof(ActivityRecords));
            OnPropertyChanged(nameof(AllTags));
            OnPropertyChanged(nameof(SelectedScopeCountText));
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(CountdownDayCount));
            OnPropertyChanged(nameof(TodayReviewedAnswerCount));
            OnPropertyChanged(nameof(TodayViewedHintCount));
            OnPropertyChanged(nameof(TodayMarkedMasteredCount));
            OnPropertyChanged(nameof(HomeDaysLeftText));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void UpdateDailyGoal(int goal)
        {
            DailyGoal = Math.Clamp(goal, 1, 100);
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void UpdateCountdownRange(DateTime? startDate, DateTime? endDate)
        {
            CountdownStartDate = startDate;
            CountdownEndDate = endDate;
            OnPropertyChanged(nameof(CountdownDayCount));
            OnPropertyChanged(nameof(HomeDaysLeftText));
        }

        public void UpdateNotificationsEnabled(bool enabled)
        {
            NotificationsEnabled = enabled;
            if (enabled)
            {
                ScheduleNotificationsForCurrentPreset();
            }
            else
            {
                CancelNotificationsForCurrentPreset();
            }
        }

        public void UpdateNotificationTime(DateTime time)
        {
            NotificationTime = time;
            if (NotificationsEnabled)
            {
                ScheduleNotificationsForCurrentPreset();
            }
        }

        public void UpdateDangerPercent(int percent)
        {
            DangerPercent = Math.Clamp(percent, 1, 100);
        }

        public PresetCreationOutcome CreatePreset(string name, string category, string markdown)
        {
            if (string.IsNullOrWhiteSpace(name))
                return PresetCreationOutcome.InvalidInput;

            if (Presets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return PresetCreationOutcome.DuplicateName;

            List<KnowledgePoint> points;
            try
            {
                points = KnowledgePoint.ParseMarkdown(markdown);
            }
            catch
            {
                return PresetCreationOutcome.ParseError;
            }

            var preset = new KnowledgePreset
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Subtitle = $"Custom preset: {name}",
                Description = $"User-created preset for {name}",
                Category = string.IsNullOrWhiteSpace(category) ? "Custom" : category,
                MarkdownText = markdown,
                IsBuiltIn = false
            };

            Presets.Add(preset);

            var state = new PresetStudyState(preset.Id, points, markdown);
            PresetStates[preset.Id] = state;

            try
            {
                _ = SaveAppStateAsync();
            }
            catch
            {
                return PresetCreationOutcome.SaveFailed;
            }

            return PresetCreationOutcome.Success;
        }

        public void UpdatePresetMetadata(string presetId, string name, string category)
        {
            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null) return;

            preset.Name = name;
            preset.Category = category;
            preset.Subtitle = $"Custom preset: {name}";
        }

        public PresetDeleteOutcome DeletePreset(string presetId)
        {
            if (presetId == KnowledgePreset.DefaultPresetID)
                return PresetDeleteOutcome.IsDefault;

            var preset = Presets.FirstOrDefault(p => p.Id == presetId);
            if (preset == null)
                return PresetDeleteOutcome.NotFound;

            Presets.Remove(preset);
            PresetStates.Remove(presetId);

            if (CurrentPresetID == presetId)
            {
                var fallback = Presets.FirstOrDefault();
                if (fallback != null)
                {
                    SwitchToPreset(fallback);
                }
                else
                {
                    CurrentPresetID = KnowledgePreset.DefaultPresetID;
                }
            }

            try
            {
                _ = SaveAppStateAsync();
            }
            catch
            {
                return PresetDeleteOutcome.DeleteFailed;
            }

            return PresetDeleteOutcome.Success;
        }

        public void UpsertKnowledgePoint(KnowledgePoint point, string presetId)
        {
            if (presetId != CurrentPresetID)
            {
                if (PresetStates.TryGetValue(presetId, out var otherState))
                {
                    var existing = otherState.KnowledgePoints.FirstOrDefault(p => p.Id == point.Id);
                    if (existing != null)
                    {
                        int idx = otherState.KnowledgePoints.IndexOf(existing);
                        otherState.KnowledgePoints[idx] = point;
                    }
                    else
                    {
                        otherState.KnowledgePoints.Add(point);
                    }
                }
                return;
            }

            var current = KnowledgePoints.FirstOrDefault(p => p.Id == point.Id);
            if (current != null)
            {
                int idx = KnowledgePoints.IndexOf(current);
                KnowledgePoints[idx] = point;
            }
            else
            {
                KnowledgePoints.Add(point);
            }

            point.UpdatedAt = DateTime.Now;
            MarkdownText = KnowledgePoint.MarkdownTextFrom(KnowledgePoints.ToList());

            OnPropertyChanged(nameof(AllTags));
            OnPropertyChanged(nameof(SelectedScopeCountText));
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void DeleteKnowledgePoint(Guid pointId, string presetId)
        {
            if (presetId != CurrentPresetID)
            {
                if (PresetStates.TryGetValue(presetId, out var otherState))
                {
                    var point = otherState.KnowledgePoints.FirstOrDefault(p => p.Id == pointId);
                    if (point != null)
                        otherState.KnowledgePoints.Remove(point);
                }
                return;
            }

            var existing = KnowledgePoints.FirstOrDefault(p => p.Id == pointId);
            if (existing != null)
            {
                KnowledgePoints.Remove(existing);
            }

            MarkdownText = KnowledgePoint.MarkdownTextFrom(KnowledgePoints.ToList());

            OnPropertyChanged(nameof(AllTags));
            OnPropertyChanged(nameof(SelectedScopeCountText));
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void RecordStudyActivity(StudyActivityType type, KnowledgePoint point)
        {
            var record = new StudyActivityRecord
            {
                Id = Guid.NewGuid(),
                PresetId = CurrentPresetID,
                Date = DateTime.Now,
                Type = type,
                PointId = point.Id,
                PointTitle = point.Title
            };
            ActivityRecords.Add(record);

            if (type == StudyActivityType.ViewedHint || type == StudyActivityType.ReviewedAnswer)
            {
                if (!DailyReviewRecords.ContainsKey(point.Id))
                {
                    DailyReviewRecords[point.Id] = new DailyReviewRecord();
                }
                DailyReviewRecords[point.Id].Date = DateTime.Today;
                DailyReviewRecords[point.Id].Count++;
            }

            OnPropertyChanged(nameof(ActivityRecords));
            OnPropertyChanged(nameof(CurrentPresetActivityRecords));
            OnPropertyChanged(nameof(TodayReviewedAnswerCount));
            OnPropertyChanged(nameof(TodayViewedHintCount));
            OnPropertyChanged(nameof(TodayMarkedMasteredCount));
        }

        public void UpdateWidgetSnapshot()
        {
            _ = UpdateWidgetSnapshotAsync();
        }

        private async Task UpdateWidgetSnapshotAsync()
        {
            try
            {
                var presetName = CurrentPreset?.Name ?? "Unknown";
                var randomPoints = KnowledgePoints
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(3)
                    .Select(p => new WidgetKnowledgePointPreview(
                        p.Title,
                        p.Tags.FirstOrDefault()))
                    .ToList();

                int? countdownDays = CountdownDayCount;

                var snapshot = new WidgetSnapshot
                {
                    PresetName = presetName,
                    TodayMasteredCount = TodayMarkedMasteredCount,
                    MasteredCount = MasteredCount,
                    DailyGoal = DailyGoal,
                    CountdownDays = countdownDays,
                    TodayReviewCount = TodayReviewedAnswerCount,
                    TodayHintCount = TodayViewedHintCount,
                    RandomKnowledgePoints = randomPoints,
                    LastUpdated = DateTime.Now
                };

                string json = JsonSerializer.Serialize(snapshot, JsonOptions);
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    "widget_snapshot.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to update widget snapshot: {ex.Message}");
            }
        }

        public void ScheduleStudyStatePersistence()
        {
            pendingStudyStatePersistenceWorkItem?.Cancel();
            pendingStudyStatePersistenceWorkItem?.Dispose();
            pendingStudyStatePersistenceWorkItem = new CancellationTokenSource();
            var token = pendingStudyStatePersistenceWorkItem.Token;
            Task.Delay(500, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    _ = SaveAppStateAsync();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public async Task SaveAppState()
        {
            await SaveAppStateAsync();
        }

        public async Task LoadAppState()
        {
            await LoadAppStateAsync();
        }

        private async Task SaveAppStateAsync()
        {
            try
            {
                SaveCurrentPresetState();

                var appState = new KikariaAppState
                {
                    SchemaVersion = KikariaAppState.CurrentSchemaVersion,
                    Presets = Presets.ToList(),
                    PresetStates = new Dictionary<string, PresetStudyState>(PresetStates),
                    CurrentPresetID = CurrentPresetID,
                    UserProfile = UserProfile,
                    HasCompletedProfileSetup = HasCompletedProfileSetup,
                    HasCompletedOnboarding = HasCompletedOnboarding
                };

                string json = appState.Serialize();
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    "app_state.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to save app state: {ex.Message}");
            }
        }

        private async Task LoadAppStateAsync()
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("app_state.json");
                string json = await FileIO.ReadTextAsync(file);
                var appState = KikariaAppState.Deserialize(json);

                if (appState == null)
                {
                    LoadInitialPresetState();
                    return;
                }

                Presets.Clear();
                foreach (var preset in appState.Presets)
                {
                    Presets.Add(preset);
                }

                PresetStates = new Dictionary<string, PresetStudyState>(appState.PresetStates);
                CurrentPresetID = appState.CurrentPresetID;
                UserProfile = appState.UserProfile;
                HasCompletedProfileSetup = appState.HasCompletedProfileSetup;
                HasCompletedOnboarding = appState.HasCompletedOnboarding;

                if (!PresetStates.ContainsKey(CurrentPresetID) && Presets.Count > 0)
                {
                    CurrentPresetID = Presets.First().Id;
                }

                if (PresetStates.TryGetValue(CurrentPresetID, out var state))
                {
                    RestorePresetState(state);
                }

                OnPropertyChanged(nameof(CurrentPreset));
                OnPropertyChanged(nameof(CurrentPresetActivityRecords));
            }
            catch (FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("[MainViewModel] No saved app state found, loading defaults.");
                LoadInitialPresetState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to load app state: {ex.Message}");
                LoadInitialPresetState();
            }
        }

        public void NavigateTo(AppRoute route)
        {
            CurrentRoute = route;
            NavigationStack.Add(route);
            OnPropertyChanged(nameof(CurrentRoute));
        }

        public void NavigateBack()
        {
            if (NavigationStack.Count > 0)
            {
                NavigationStack.RemoveAt(NavigationStack.Count - 1);
                CurrentRoute = NavigationStack.Count > 0 ? NavigationStack.Last() : null;
                OnPropertyChanged(nameof(CurrentRoute));
            }
        }

        public void NavigateHome()
        {
            NavigationStack.Clear();
            CurrentRoute = null;
            OnPropertyChanged(nameof(CurrentRoute));
        }

        public string OrdinalSuffix(int day)
        {
            if (day >= 11 && day <= 13)
                return "th";

            return (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        }

        [RelayCommand]
        private void GoHome()
        {
            NavigateHome();
        }

        [RelayCommand]
        private void GoToSettings()
        {
            NavigateTo(AppRoute.Settings);
        }

        [RelayCommand]
        private void GoToProfile()
        {
            NavigateTo(AppRoute.EditProfile);
        }

        [RelayCommand]
        private void GoToReview()
        {
            NavigateTo(AppRoute.Review);
        }

        [RelayCommand]
        private void GoToPresetLibrary()
        {
            NavigateTo(AppRoute.PresetSelection);
        }

        [RelayCommand]
        private void GoToStatistics()
        {
            NavigateTo(AppRoute.ReviewHistory);
        }

        [RelayCommand]
        private void GoBack()
        {
            NavigateBack();
        }

        [RelayCommand]
        private async Task SaveState()
        {
            await SaveAppStateAsync();
        }

        private void ScheduleNotificationsForCurrentPreset()
        {
            try
            {
                var servicesState = BuildServicesPresetStudyState();
                var service = new Services.NotificationService();
                string presetName = CurrentPreset?.Name ?? "Unknown";
                service.RescheduleStudyProgressWarning(servicesState, presetName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to schedule notifications: {ex.Message}");
            }
        }

        private void CancelNotificationsForCurrentPreset()
        {
            try
            {
                var service = new Services.NotificationService();
                service.CancelStudyProgressWarning(CurrentPresetID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to cancel notifications: {ex.Message}");
            }
        }

        private PresetStudyState BuildServicesPresetStudyState()
        {
            if (PresetStates.TryGetValue(CurrentPresetID, out var state))
                return state;

            return new PresetStudyState
            {
                PresetId = CurrentPresetID,
                DailyGoal = DailyGoal,
                CountdownStartDate = CountdownStartDate,
                CountdownEndDate = CountdownEndDate
            };
        }

        partial void OnSelectedTagsChanged(HashSet<string> value)
        {
            OnPropertyChanged(nameof(SelectedScopeCountText));
        }

        partial void OnKnowledgePointsChanged(ObservableCollection<KnowledgePoint> value)
        {
            OnPropertyChanged(nameof(AllTags));
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(HomeProgressText));
            OnPropertyChanged(nameof(SelectedScopeCountText));
        }

        partial void OnDailyGoalChanged(int value)
        {
            OnPropertyChanged(nameof(HomeProgressText));
        }

        partial void OnCountdownEndDateChanged(DateTime? value)
        {
            OnPropertyChanged(nameof(CountdownDayCount));
            OnPropertyChanged(nameof(HomeDaysLeftText));
        }

        [ObservableProperty]
        private KnowledgePoint? currentReviewPoint;

        [ObservableProperty]
        private ReviewMode reviewMode;

        [ObservableProperty]
        private bool isHintRevealed;

        [ObservableProperty]
        private bool isAnswerRevealed;

        [ObservableProperty]
        private string toastMessage = string.Empty;

        private List<KnowledgePoint> _reviewQueue = new();
        private int _reviewIndex;

        public bool IsReviewQueueEmpty => _reviewQueue.Count == 0;
        public bool HasCurrentReviewPoint => CurrentReviewPoint != null;
        public bool IsContentRevealed => IsHintRevealed || IsAnswerRevealed;
        public bool ShowRevealActions => !IsHintRevealed && !IsAnswerRevealed;
        public bool ShowPostRevealActions => IsHintRevealed || IsAnswerRevealed;
        public bool ShowReviewEmptyState => IsReviewQueueEmpty && ReviewMode == ReviewMode.Normal;
        public bool ShowReinforcementComplete => IsReviewQueueEmpty && (ReviewMode == ReviewMode.Reinforcement || ReviewMode == ReviewMode.Mastered);
        public bool IsTagSelected(string tag) => SelectedTags.Contains(tag);
        public string CurrentPresetName => CurrentPreset?.Name ?? "未选择预设";
        public string DisplayName => UserProfile?.DisplayName ?? "用户";
        public string UserHandle => string.IsNullOrEmpty(UserProfile?.UserHandle) ? "" : $"@{UserProfile.UserHandle}";
        public string VersionString => "1.0.0";
        public string TodayDateString => DateTime.Today.ToString("M月d日 dddd");

        public string ModeActionLabel1 => ReviewMode switch
        {
            ReviewMode.Normal => "加入重点集锦",
            ReviewMode.Reinforcement => "移出重点集锦",
            ReviewMode.Mastered => "加入重点集锦",
            _ => ""
        };

        public string ModeActionLabel2 => ReviewMode switch
        {
            ReviewMode.Normal => "加入已掌握",
            ReviewMode.Reinforcement => "加入已掌握",
            ReviewMode.Mastered => "移出已掌握",
            _ => ""
        };

        public List<KnowledgePoint> ReinforcedPointsList =>
            KnowledgePoints.Where(p => p.IsReinforced).OrderByDescending(p => p.LastReinforcedAt).ToList();

        public List<KnowledgePoint> MasteredPointsList =>
            KnowledgePoints.Where(p => p.IsMastered).OrderByDescending(p => p.UpdatedAt).ToList();

        public void BuildReviewQueue()
        {
            _reviewQueue = ReviewMode switch
            {
                ReviewMode.Reinforcement => KnowledgePoints.Where(p => p.IsReinforced && !p.IsMastered).ToList(),
                ReviewMode.Mastered => KnowledgePoints.Where(p => p.IsMastered).ToList(),
                _ => FilteredKnowledgePoints().Where(p => !p.IsMastered).ToList()
            };
            _reviewIndex = 0;
            CurrentReviewPoint = _reviewQueue.Count > 0 ? _reviewQueue[0] : null;
            ResetRevealState();
            OnPropertyChanged(nameof(IsReviewQueueEmpty));
            OnPropertyChanged(nameof(ShowReviewEmptyState));
            OnPropertyChanged(nameof(ShowReinforcementComplete));
        }

        private List<KnowledgePoint> FilteredKnowledgePoints()
        {
            if (SelectedTags.Count == 0) return KnowledgePoints.ToList();
            return KnowledgePoints.Where(p => p.Tags.Any(t => SelectedTags.Contains(t))).ToList();
        }

        public void ResetRevealState()
        {
            IsHintRevealed = false;
            IsAnswerRevealed = false;
        }

        public void ShowHintAction()
        {
            IsHintRevealed = true;
            if (CurrentReviewPoint != null)
                RecordStudyActivity(StudyActivityType.ViewedHint, CurrentReviewPoint);
        }

        public void ShowAnswerAction()
        {
            IsAnswerRevealed = true;
            if (CurrentReviewPoint != null)
                RecordStudyActivity(StudyActivityType.ReviewedAnswer, CurrentReviewPoint);
        }

        public void NextReviewPoint()
        {
            if (_reviewQueue.Count == 0) return;
            _reviewIndex = (_reviewIndex + 1) % _reviewQueue.Count;
            CurrentReviewPoint = _reviewQueue[_reviewIndex];
            ResetRevealState();
        }

        public void PreviousReviewPoint()
        {
            if (_reviewQueue.Count == 0) return;
            _reviewIndex = (_reviewIndex - 1 + _reviewQueue.Count) % _reviewQueue.Count;
            CurrentReviewPoint = _reviewQueue[_reviewIndex];
            ResetRevealState();
        }

        public void AddToReinforcementAction()
        {
            if (CurrentReviewPoint == null) return;
            CurrentReviewPoint.AddReinforcement();
            RecordStudyActivity(StudyActivityType.AddedReinforcement, CurrentReviewPoint);
            ShowToastMessage("已加入重点集锦");
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(ReinforcedPointsList));
        }

        public void RemoveFromReinforcementAction()
        {
            if (CurrentReviewPoint == null) return;
            CurrentReviewPoint.ClearReinforcement();
            RecordStudyActivity(StudyActivityType.RemovedReinforcement, CurrentReviewPoint);
            ShowToastMessage("已移出重点集锦");
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(ReinforcedPointsList));
        }

        public void MarkMasteredAction()
        {
            if (CurrentReviewPoint == null) return;
            CurrentReviewPoint.IsMastered = true;
            CurrentReviewPoint.UpdatedAt = DateTime.Now;
            RecordStudyActivity(StudyActivityType.MarkedMastered, CurrentReviewPoint);
            ShowToastMessage("已加入已掌握");
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(MasteredPointsList));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void RemoveFromMasteredAction()
        {
            if (CurrentReviewPoint == null) return;
            CurrentReviewPoint.IsMastered = false;
            CurrentReviewPoint.UpdatedAt = DateTime.Now;
            RecordStudyActivity(StudyActivityType.RemovedMastered, CurrentReviewPoint);
            ShowToastMessage("已移出已掌握");
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(MasteredPointsList));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void RemoveReinforcedPointAction(KnowledgePoint point)
        {
            point.ClearReinforcement();
            RecordStudyActivity(StudyActivityType.RemovedReinforcement, point);
            ShowToastMessage("已移出重点集锦");
            OnPropertyChanged(nameof(ReinforcedCount));
            OnPropertyChanged(nameof(ReinforcedPointsList));
        }

        public void RemoveMasteredPointAction(KnowledgePoint point)
        {
            point.IsMastered = false;
            point.UpdatedAt = DateTime.Now;
            RecordStudyActivity(StudyActivityType.RemovedMastered, point);
            ShowToastMessage("已移出已掌握");
            OnPropertyChanged(nameof(MasteredCount));
            OnPropertyChanged(nameof(MasteredPointsList));
            OnPropertyChanged(nameof(HomeProgressText));
        }

        public void ToggleTagSelection(string tag)
        {
            if (SelectedTags.Contains(tag))
                SelectedTags.Remove(tag);
            else
                SelectedTags.Add(tag);
            SelectedTags = new HashSet<string>(SelectedTags);
        }

        public void ShowToastMessage(string message)
        {
            ToastMessage = message;
            Task.Delay(2500).ContinueWith(_ =>
            {
                ToastMessage = string.Empty;
            });
        }

        public void NotifyProfileChanged()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(UserHandle));
        }

        partial void OnIsHintRevealedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsContentRevealed));
            OnPropertyChanged(nameof(ShowRevealActions));
            OnPropertyChanged(nameof(ShowPostRevealActions));
        }

        partial void OnIsAnswerRevealedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsContentRevealed));
            OnPropertyChanged(nameof(ShowRevealActions));
            OnPropertyChanged(nameof(ShowPostRevealActions));
        }

        partial void OnReviewModeChanged(ReviewMode value)
        {
            OnPropertyChanged(nameof(ModeActionLabel1));
            OnPropertyChanged(nameof(ModeActionLabel2));
        }
    }
}
