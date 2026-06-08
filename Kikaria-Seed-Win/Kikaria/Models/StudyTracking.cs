using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kikaria.Models
{
    public enum StudyActivityType
    {
        ViewedHint,
        ReviewedAnswer,
        MarkedMastered,
        RemovedMastered,
        AddedReinforcement,
        RemovedReinforcement
    }

    public class StudyActivityRecord : IEquatable<StudyActivityRecord>
    {
        public Guid Id { get; set; }
        public string PresetId { get; set; } = string.Empty;
        public DateTimeOffset Date { get; set; }
        public StudyActivityType Type { get; set; }
        public Guid PointId { get; set; }
        public string PointTitle { get; set; } = string.Empty;

        public StudyActivityRecord(
            Guid id,
            string presetId,
            DateTimeOffset date,
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

        [JsonConstructor]
        public StudyActivityRecord(
            Guid id,
            string presetId,
            DateTimeOffset date,
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

        public bool Equals(StudyActivityRecord? other)
        {
            if (other is null) return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj) => Equals(obj as StudyActivityRecord);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public class WidgetKnowledgePointPreview
    {
        public string Title { get; set; } = string.Empty;
        public string? Tag { get; set; }

        public WidgetKnowledgePointPreview(string title, string? tag = null)
        {
            Title = title;
            Tag = tag;
        }

        [JsonConstructor]
        public WidgetKnowledgePointPreview(string title, string? tag)
        {
            Title = title;
            Tag = tag;
        }
    }

    public class WidgetSnapshot
    {
        public string PresetName { get; set; } = string.Empty;
        public int TodayMasteredCount { get; set; }
        public int MasteredCount { get; set; }
        public int DailyGoal { get; set; }
        public int? CountdownDays { get; set; }
        public int TodayReviewCount { get; set; }
        public int TodayHintCount { get; set; }
        public List<WidgetKnowledgePointPreview> RandomKnowledgePoints { get; set; } = new();
        public DateTimeOffset LastUpdated { get; set; }

        public WidgetSnapshot(
            string presetName,
            int todayMasteredCount,
            int masteredCount,
            int dailyGoal,
            int? countdownDays,
            int todayReviewCount,
            int todayHintCount,
            List<WidgetKnowledgePointPreview> randomKnowledgePoints,
            DateTimeOffset lastUpdated)
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
            DateTimeOffset lastUpdated)
        {
            PresetName = presetName ?? "Kikaria";
            TodayMasteredCount = todayMasteredCount;
            MasteredCount = masteredCount;
            DailyGoal = dailyGoal == default ? 20 : dailyGoal;
            CountdownDays = countdownDays;
            TodayReviewCount = todayReviewCount;
            TodayHintCount = todayHintCount;
            RandomKnowledgePoints = randomKnowledgePoints ?? new List<WidgetKnowledgePointPreview>();
            LastUpdated = lastUpdated == default ? DateTimeOffset.Now : lastUpdated;
        }

        public static WidgetSnapshot Placeholder => new(
            presetName: "高等数学知识点",
            todayMasteredCount: 0,
            masteredCount: 0,
            dailyGoal: 20,
            countdownDays: null,
            todayReviewCount: 0,
            todayHintCount: 0,
            randomKnowledgePoints: new List<WidgetKnowledgePointPreview>
            {
                new WidgetKnowledgePointPreview("极限的保号性", "极限")
            },
            lastUpdated: DateTimeOffset.Now
        );
    }
}