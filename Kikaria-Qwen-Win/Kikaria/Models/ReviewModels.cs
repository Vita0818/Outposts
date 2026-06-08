using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kikaria.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewMode
{
    Normal,
    Reinforcement,
    Mastered
}

public static class ReviewModeExtensions
{
    public static bool IsNormal(this ReviewMode mode) => mode == ReviewMode.Normal;
    public static bool IsReinforcement(this ReviewMode mode) => mode == ReviewMode.Reinforcement;
    public static bool IsMastered(this ReviewMode mode) => mode == ReviewMode.Mastered;

    public static string DisplayName(this ReviewMode mode) => mode switch
    {
        ReviewMode.Normal => "Normal",
        ReviewMode.Reinforcement => "Reinforcement",
        ReviewMode.Mastered => "Mastered",
        _ => "Unknown"
    };

    public static string Symbol(this ReviewMode mode) => mode switch
    {
        ReviewMode.Normal => "book",
        ReviewMode.Reinforcement => "arrow.clockwise",
        ReviewMode.Mastered => "checkmark.seal",
        _ => "questionmark"
    };
}

public enum AppRoute
{
    Scope,
    Review,
    TodayOverview,
    ReviewHistory,
    Reinforcement,
    ReinforcementReview,
    Mastered,
    MasteredReview,
    Settings,
    EditProfile,
    MarkdownEditor,
    PresetSelection,
    NewPreset,
    MarkdownFormatGuide,
}

public class EditPresetPageNavParam
{
    public KikariaAppState AppState { get; set; } = null!;
    public string PresetId { get; set; } = string.Empty;
    public string PresetName { get; set; } = string.Empty;
}

public class AppRouteWithPresetId
{
    public string PresetId { get; set; } = string.Empty;
}

public class AppRouteEditKnowledgePoint
{
    public string PresetId { get; set; } = string.Empty;
    public Guid? PointId { get; set; }
}

public static class AppRouteExtensions
{
    public static string Title(this AppRoute route) => route switch
    {
        AppRoute.Scope => "Scope",
        AppRoute.Review => "Review",
        AppRoute.TodayOverview => "Today Overview",
        AppRoute.ReviewHistory => "Review History",
        AppRoute.Reinforcement => "Reinforcement",
        AppRoute.ReinforcementReview => "Reinforcement Review",
        AppRoute.Mastered => "Mastered",
        AppRoute.MasteredReview => "Mastered Review",
        AppRoute.Settings => "Settings",
        AppRoute.EditProfile => "Edit Profile",
        AppRoute.MarkdownEditor => "Markdown Editor",
        AppRoute.PresetSelection => "Preset Selection",
        AppRoute.NewPreset => "New Preset",
        AppRoute.MarkdownFormatGuide => "Markdown Format Guide",
        _ => "Kikaria"
    };
}

public struct ActivitySummary
{
    public List<StudyActivityRecord> Records { get; set; }

    public ActivitySummary(List<StudyActivityRecord> records)
    {
        Records = records;
    }

    [JsonIgnore]
    public int TotalActivities => Records.Count;

    [JsonIgnore]
    public int HintViews => Records.Count(r => r.Type == StudyActivityType.ViewedHint);

    [JsonIgnore]
    public int AnswerReviews => Records.Count(r => r.Type == StudyActivityType.ReviewedAnswer);

    [JsonIgnore]
    public int MasteredCount => Records.Count(r => r.Type == StudyActivityType.MarkedMastered);

    [JsonIgnore]
    public int RemovedMasteredCount => Records.Count(r => r.Type == StudyActivityType.RemovedMastered);

    [JsonIgnore]
    public int AddedReinforcementCount => Records.Count(r => r.Type == StudyActivityType.AddedReinforcement);

    [JsonIgnore]
    public int RemovedReinforcementCount => Records.Count(r => r.Type == StudyActivityType.RemovedReinforcement);

    [JsonIgnore]
    public int TodayActivities => Records.Count(r => r.Date.Date == DateTime.Today);

    [JsonIgnore]
    public int TodayHintViews => Records.Count(r => r.Date.Date == DateTime.Today && r.Type == StudyActivityType.ViewedHint);

    [JsonIgnore]
    public int TodayAnswerReviews => Records.Count(r => r.Date.Date == DateTime.Today && r.Type == StudyActivityType.ReviewedAnswer);

    [JsonIgnore]
    public int TodayMastered => Records.Count(r => r.Date.Date == DateTime.Today && r.Type == StudyActivityType.MarkedMastered);

    public int ActivitiesOnDate(DateTime date)
    {
        return Records.Count(r => r.Date.Date == date.Date);
    }

    public int HintViewsOnDate(DateTime date)
    {
        return Records.Count(r => r.Date.Date == date.Date && r.Type == StudyActivityType.ViewedHint);
    }

    public int AnswerReviewsOnDate(DateTime date)
    {
        return Records.Count(r => r.Date.Date == date.Date && r.Type == StudyActivityType.ReviewedAnswer);
    }

    public Dictionary<DateTime, int> DailyActivityCounts()
    {
        return Records
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public Dictionary<string, int> ActivitiesByPoint()
    {
        return Records
            .GroupBy(r => r.PointTitle)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
