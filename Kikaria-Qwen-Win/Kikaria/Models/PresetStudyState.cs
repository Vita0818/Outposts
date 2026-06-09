using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Kikaria.Models;

public partial class PresetStudyState : ObservableObject
{
    [ObservableProperty]
    [JsonInclude]
    private string presetId;

    [ObservableProperty]
    [JsonInclude]
    private List<KnowledgePoint> knowledgePoints;

    [ObservableProperty]
    [JsonInclude]
    private string markdownText;

    [ObservableProperty]
    [JsonInclude]
    private HashSet<string> selectedTags;

    [ObservableProperty]
    [JsonInclude]
    private Dictionary<Guid, DailyReviewRecord> dailyReviewRecords;

    [ObservableProperty]
    [JsonInclude]
    private List<StudyActivityRecord> activityRecords;

    [ObservableProperty]
    [JsonInclude]
    private int dailyGoal;

    [ObservableProperty]
    [JsonInclude]
    private DateTime? countdownStartDate;

    [ObservableProperty]
    [JsonInclude]
    private DateTime? countdownEndDate;

    [ObservableProperty]
    [JsonInclude]
    private bool notificationsEnabled;

    [ObservableProperty]
    [JsonInclude]
    private TimeSpan notificationTime;

    [ObservableProperty]
    [JsonInclude]
    private int dangerPercent;

    [JsonPropertyName("countdownDate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LegacyCountdownDate
    {
        get => CountdownEndDate;
        set
        {
            if (value.HasValue && CountdownEndDate == null)
                CountdownEndDate = value;
        }
    }

    [JsonConstructor]
    public PresetStudyState(
        string presetId,
        List<KnowledgePoint> knowledgePoints,
        string markdownText,
        HashSet<string> selectedTags,
        Dictionary<Guid, DailyReviewRecord> dailyReviewRecords,
        List<StudyActivityRecord> activityRecords,
        int dailyGoal,
        DateTime? countdownStartDate,
        DateTime? countdownEndDate,
        bool notificationsEnabled,
        TimeSpan notificationTime,
        int dangerPercent)
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
        NotificationTime = notificationTime;
        DangerPercent = Math.Clamp(dangerPercent, 1, 100);
    }

    public PresetStudyState()
    {
        PresetId = string.Empty;
        KnowledgePoints = new List<KnowledgePoint>();
        MarkdownText = string.Empty;
        SelectedTags = new HashSet<string>();
        DailyReviewRecords = new Dictionary<Guid, DailyReviewRecord>();
        ActivityRecords = new List<StudyActivityRecord>();
        DailyGoal = 20;
        CountdownStartDate = null;
        CountdownEndDate = null;
        NotificationsEnabled = false;
        NotificationTime = new TimeSpan(21, 0, 0);
        DangerPercent = 80;
    }

    public PresetStudyState(string presetId, List<KnowledgePoint> points, string markdownText)
    {
        PresetId = presetId;
        KnowledgePoints = points;
        MarkdownText = markdownText;
        SelectedTags = new HashSet<string>();
        DailyReviewRecords = new Dictionary<Guid, DailyReviewRecord>();
        ActivityRecords = new List<StudyActivityRecord>();
        DailyGoal = 20;
        CountdownStartDate = null;
        CountdownEndDate = null;
        NotificationsEnabled = false;
        NotificationTime = new TimeSpan(21, 0, 0);
        DangerPercent = 80;
    }

    [JsonIgnore]
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

    [JsonIgnore]
    public List<KnowledgePoint> FilteredPoints
    {
        get
        {
            if (SelectedTags.Count == 0)
                return KnowledgePoints;

            return KnowledgePoints
                .Where(p => p.Tags.Any(t => SelectedTags.Contains(t)))
                .ToList();
        }
    }

    [JsonIgnore]
    public int MasteredCount => KnowledgePoints.Count(p => p.IsMastered);

    [JsonIgnore]
    public int ReinforcedCount => KnowledgePoints.Count(p => p.IsReinforced);

    [JsonIgnore]
    public int TotalCount => KnowledgePoints.Count;

    [JsonIgnore]
    public double MasteryProgress => TotalCount > 0 ? (double)MasteredCount / TotalCount : 0.0;

    public void RecordDailyReview(Guid pointId, DateTime date)
    {
        var dateKey = date.Date;
        if (!DailyReviewRecords.ContainsKey(pointId))
        {
            DailyReviewRecords[pointId] = new DailyReviewRecord();
        }
        DailyReviewRecords[pointId].Date = dateKey;
        DailyReviewRecords[pointId].Count++;
    }

    public void AddActivityRecord(StudyActivityType type, Guid pointId, string pointTitle)
    {
        var record = new StudyActivityRecord
        {
            Id = Guid.NewGuid(),
            PresetId = PresetId,
            Date = DateTime.Now,
            Type = type,
            PointId = pointId,
            PointTitle = pointTitle
        };
        ActivityRecords.Add(record);
    }

    public int GetTodayReviewCount()
    {
        var today = DateTime.Today;
        return ActivityRecords
            .Count(r => r.Date.Date == today &&
                       (r.Type == StudyActivityType.ReviewedAnswer ||
                        r.Type == StudyActivityType.ViewedHint));
    }

    public int GetTodayHintCount()
    {
        var today = DateTime.Today;
        return ActivityRecords
            .Count(r => r.Date.Date == today && r.Type == StudyActivityType.ViewedHint);
    }

    public int GetTodayMasteredCount()
    {
        var today = DateTime.Today;
        return ActivityRecords
            .Count(r => r.Date.Date == today && r.Type == StudyActivityType.MarkedMastered);
    }
}
