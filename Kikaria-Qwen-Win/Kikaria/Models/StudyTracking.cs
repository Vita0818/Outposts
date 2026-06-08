using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kikaria.Models;

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

public class StudyActivityRecord
{
    [JsonInclude]
    public Guid Id { get; set; }

    [JsonInclude]
    public string PresetId { get; set; }

    [JsonInclude]
    public DateTime Date { get; set; }

    [JsonInclude]
    public StudyActivityType Type { get; set; }

    [JsonInclude]
    public Guid PointId { get; set; }

    [JsonInclude]
    public string PointTitle { get; set; }

    [JsonConstructor]
    public StudyActivityRecord(
        Guid id,
        string presetId,
        DateTime date,
        StudyActivityType type,
        Guid pointId,
        string pointTitle)
    {
        Id = id;
        PresetId = presetId;
        Date = date;
        Type = type;
        PointId = pointId;
        PointTitle = pointTitle;
    }

    public StudyActivityRecord()
    {
        Id = Guid.NewGuid();
        PresetId = string.Empty;
        Date = DateTime.Now;
        Type = StudyActivityType.ViewedHint;
        PointId = Guid.Empty;
        PointTitle = string.Empty;
    }
}

public class WidgetKnowledgePointPreview
{
    [JsonInclude]
    public string Title { get; set; }

    [JsonInclude]
    public string? Tag { get; set; }

    [JsonConstructor]
    public WidgetKnowledgePointPreview(string title, string? tag)
    {
        Title = title;
        Tag = tag;
    }

    public WidgetKnowledgePointPreview()
    {
        Title = string.Empty;
        Tag = null;
    }
}

public class WidgetSnapshot
{
    [JsonInclude]
    public string PresetName { get; set; }

    [JsonInclude]
    public int TodayMasteredCount { get; set; }

    [JsonInclude]
    public int MasteredCount { get; set; }

    [JsonInclude]
    public int DailyGoal { get; set; }

    [JsonInclude]
    public int? CountdownDays { get; set; }

    [JsonInclude]
    public int TodayReviewCount { get; set; }

    [JsonInclude]
    public int TodayHintCount { get; set; }

    [JsonInclude]
    public List<WidgetKnowledgePointPreview> RandomKnowledgePoints { get; set; }

    [JsonInclude]
    public DateTime LastUpdated { get; set; }

    [JsonConstructor]
    public WidgetSnapshot(
        string presetName,
        int todayMasteredCount,
        int masteredCount,
        int dailyGoal,
        int? countdownDays,
        int todayReviewCount,
        int todayHintCount,
        List<WidgetKnowledgePointPreview> randomKnowledgePoints,
        DateTime lastUpdated)
    {
        PresetName = presetName;
        TodayMasteredCount = todayMasteredCount;
        MasteredCount = masteredCount;
        DailyGoal = dailyGoal;
        CountdownDays = countdownDays;
        TodayReviewCount = todayReviewCount;
        TodayHintCount = todayHintCount;
        RandomKnowledgePoints = randomKnowledgePoints;
        LastUpdated = lastUpdated;
    }

    public WidgetSnapshot()
    {
        PresetName = string.Empty;
        TodayMasteredCount = 0;
        MasteredCount = 0;
        DailyGoal = 5;
        CountdownDays = null;
        TodayReviewCount = 0;
        TodayHintCount = 0;
        RandomKnowledgePoints = new List<WidgetKnowledgePointPreview>();
        LastUpdated = DateTime.Now;
    }

    public static WidgetSnapshot Placeholder { get; } = new()
    {
        PresetName = "Placeholder",
        TodayMasteredCount = 0,
        MasteredCount = 0,
        DailyGoal = 20,
        CountdownDays = null,
        TodayReviewCount = 0,
        TodayHintCount = 0,
        RandomKnowledgePoints = new List<WidgetKnowledgePointPreview>(),
        LastUpdated = DateTime.Now
    };

    public static WidgetSnapshot CreateFrom(PresetStudyState state, string presetName)
    {
        var randomPoints = state.KnowledgePoints
            .OrderBy(_ => Guid.NewGuid())
            .Take(3)
            .Select(p => new WidgetKnowledgePointPreview(
                p.Title,
                p.Tags.FirstOrDefault()))
            .ToList();

        int? countdownDays = null;
        if (state.CountdownEndDate.HasValue)
        {
            countdownDays = Math.Max(0, (int)(state.CountdownEndDate.Value - DateTime.Today).TotalDays);
        }

        return new WidgetSnapshot
        {
            PresetName = presetName,
            TodayMasteredCount = state.GetTodayMasteredCount(),
            MasteredCount = state.MasteredCount,
            DailyGoal = state.DailyGoal,
            CountdownDays = countdownDays,
            TodayReviewCount = state.GetTodayReviewCount(),
            TodayHintCount = state.GetTodayHintCount(),
            RandomKnowledgePoints = randomPoints,
            LastUpdated = DateTime.Now
        };
    }
}

public class DailyReviewRecord
{
    [JsonInclude]
    public DateTime Date { get; set; }

    [JsonInclude]
    public int Count { get; set; }

    [JsonConstructor]
    public DailyReviewRecord(DateTime date, int count)
    {
        Date = date;
        Count = count;
    }

    public DailyReviewRecord()
    {
        Date = DateTime.Today;
        Count = 0;
    }
}
