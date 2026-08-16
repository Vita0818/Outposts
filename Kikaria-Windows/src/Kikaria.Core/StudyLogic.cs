//
//  StudyLogic.cs
//  Kikaria-Windows
//
//  复习队列 / 每日目标 / 倒数日 / 危险线判定 / 今日计数等纯逻辑,
//  移植自 Kikaria-Apple 的 ContentView.swift(ReviewView 队列逻辑、countdownDays、
//  evaluateStudyProgressWarning、ActivitySummary、matchesSearchQuery)。
//

namespace Kikaria.Core;

/// <summary>学习进度警告(危险线判定结果)。</summary>
public sealed record StudyProgressWarning(
    int MasteredCount,
    int ExpectedMasteredCount,
    int DangerPercent,
    int? RemainingDays);

/// <summary>某日学习活动汇总。</summary>
public sealed record ActivitySummary(
    int ViewedHintCount,
    int ReviewedAnswerCount,
    int MarkedMasteredCount,
    int AddedReinforcementCount)
{
    public int TotalCount => ViewedHintCount + ReviewedAnswerCount + MarkedMasteredCount + AddedReinforcementCount;

    public static ActivitySummary Make(IEnumerable<StudyActivityRecord> records)
    {
        var viewedHint = 0;
        var reviewedAnswer = 0;
        var markedMastered = 0;
        var addedReinforcement = 0;

        foreach (var record in records)
        {
            switch (record.Type)
            {
                case StudyActivityType.ViewedHint:
                    viewedHint++;
                    break;
                case StudyActivityType.ReviewedAnswer:
                    reviewedAnswer++;
                    break;
                case StudyActivityType.MarkedMastered:
                    markedMastered++;
                    break;
                case StudyActivityType.AddedReinforcement:
                    addedReinforcement++;
                    break;
            }
        }

        return new ActivitySummary(viewedHint, reviewedAnswer, markedMastered, addedReinforcement);
    }
}

public static class StudyLogic
{
    /// <summary>匹配集:normal=全部或选中 tag 交集 / reinforcement=reinforcementCount&gt;0 / mastered=isMastered。</summary>
    public static List<Guid> MatchingPointIds(
        IReadOnlyList<KnowledgePoint> points,
        IReadOnlyCollection<string>? selectedTags,
        ReviewMode mode)
    {
        var ids = new List<Guid>();
        foreach (var point in points)
        {
            var matches = mode switch
            {
                ReviewMode.Normal => selectedTags is null || selectedTags.Count == 0
                    ? true
                    : point.Tags.Any(tag => selectedTags.Contains(tag)),
                ReviewMode.Reinforcement => point.ReinforcementCount > 0,
                ReviewMode.Mastered => point.IsMastered,
                _ => false
            };

            if (matches)
            {
                ids.Add(point.Id);
            }
        }

        return ids;
    }

    /// <summary>shuffle 队列,且首位尽量避开上一点(与 Apple 版 rebuildReviewQueue 一致)。</summary>
    public static List<Guid> BuildShuffledQueue(List<Guid> ids, Guid? avoidFirstId)
    {
        var shuffled = new List<Guid>(ids);
        var random = new Random();

        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        if (avoidFirstId is not null &&
            shuffled.Count > 1 &&
            shuffled[0].Equals(avoidFirstId.Value))
        {
            var swapIndex = shuffled.FindIndex(id => !id.Equals(avoidFirstId.Value));
            if (swapIndex > 0)
            {
                (shuffled[0], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[0]);
            }
        }

        return shuffled;
    }

    /// <summary>过滤队列中已失效的 id。</summary>
    public static List<Guid> ReconcileQueue(List<Guid> queue, HashSet<Guid> validIds)
    {
        return queue.Where(id => validIds.Contains(id)).ToList();
    }

    /// <summary>
    /// 危险线判定(与 Apple 版 evaluateStudyProgressWarning 一致):
    /// totalCount&gt;0、起止都设置、start&lt;=end、today&gt;=start 才继续;
    /// expectedProgress = elapsedDays/totalDays(两端含当天,today&gt;=end 则 1);
    /// mastered/expectedMastered(=ceil(total*progress)) &lt; dangerPercent/100 时警告。
    /// </summary>
    public static StudyProgressWarning? EvaluateStudyProgressWarning(PresetStudyState state, DateTime? now = null)
    {
        var totalCount = state.KnowledgePoints.Count;
        var masteredCount = state.KnowledgePoints.Count(point => point.IsMastered);
        var dangerPercent = ClampDanger(state.DangerPercent);

        if (totalCount == 0 ||
            state.CountdownStartDate is null ||
            state.CountdownEndDate is null)
        {
            return null;
        }

        var today = (now ?? DateTime.Now).Date;
        var start = state.CountdownStartDate.Value.Date;
        var end = state.CountdownEndDate.Value.Date;

        if (start > end || today < start)
        {
            return null;
        }

        double expectedProgress;
        if (today >= end)
        {
            expectedProgress = 1.0;
        }
        else
        {
            var totalDays = Math.Max(1, (int)(end - start).TotalDays + 1);
            var elapsedDays = Math.Max(1, (int)(today - start).TotalDays + 1);
            expectedProgress = (double)elapsedDays / totalDays;
        }

        var expectedMasteredCount = (int)Math.Ceiling(totalCount * expectedProgress);
        if (expectedMasteredCount <= 0)
        {
            return null;
        }

        var actualProgressRatio = (double)masteredCount / expectedMasteredCount;
        if (actualProgressRatio >= (double)dangerPercent / 100)
        {
            return null;
        }

        return new StudyProgressWarning(masteredCount, expectedMasteredCount, dangerPercent, CountdownDays(end));
    }

    /// <summary>倒数天数:startOfDay(end) - startOfDay(today),下限 0;未设置为 null。</summary>
    public static int? CountdownDays(DateTime? targetDate)
    {
        if (targetDate is null)
        {
            return null;
        }

        var today = DateTime.Today;
        var target = targetDate.Value.Date;
        var dayCount = (int)(target - today).TotalDays;
        return Math.Max(0, dayCount);
    }

    /// <summary>取某日的活动记录。</summary>
    public static List<StudyActivityRecord> RecordsOnDate(IEnumerable<StudyActivityRecord> records, DateTime date)
    {
        return records.Where(record => IsSameDay(record.Date, date)).ToList();
    }

    /// <summary>今日新增掌握 = 今日 markedMastered 记录去重点数。</summary>
    public static int TodayMarkedMasteredCount(IEnumerable<StudyActivityRecord> records, DateTime? today = null)
    {
        var date = today ?? DateTime.Now;
        return RecordsOnDate(records, date)
            .Where(record => record.Type == StudyActivityType.MarkedMastered)
            .Select(record => record.PointId)
            .Distinct()
            .Count();
    }

    public static int TodayReviewedAnswerCount(IEnumerable<StudyActivityRecord> records, DateTime? today = null)
    {
        var date = today ?? DateTime.Now;
        return RecordsOnDate(records, date).Count(record => record.Type == StudyActivityType.ReviewedAnswer);
    }

    public static int TodayViewedHintCount(IEnumerable<StudyActivityRecord> records, DateTime? today = null)
    {
        var date = today ?? DateTime.Now;
        return RecordsOnDate(records, date).Count(record => record.Type == StudyActivityType.ViewedHint);
    }

    public static bool IsSameDay(DateTime a, DateTime b) => a.Date == b.Date;

    /// <summary>默认通知时间:今天 21:00。</summary>
    public static DateTime DefaultNotificationTime()
    {
        var now = DateTime.Now;
        return new DateTime(now.Year, now.Month, now.Day, 21, 0, 0);
    }

    public static int ClampGoal(int value) => Math.Clamp(value, 1, 100);
    public static int ClampDanger(int value) => Math.Clamp(value, 1, 100);

    /// <summary>知识点搜索:标题 / 标签 / 提示 / 答案包含查询串(忽略大小写)。</summary>
    public static bool MatchesSearchQuery(KnowledgePoint point, string query)
    {
        var trimmed = query.Trim();
        if (trimmed.Length == 0)
        {
            return true;
        }

        var fields = new[]
        {
            point.Title,
            string.Join(" ", point.Tags),
            point.Hint,
            point.Content
        };

        return fields.Any(field => field.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>全部标签(去重排序)。</summary>
    public static List<string> AllTags(IEnumerable<KnowledgePoint> points)
    {
        var tags = new HashSet<string>();
        foreach (var point in points)
        {
            foreach (var tag in point.Tags)
            {
                tags.Add(tag);
            }
        }

        return tags.OrderBy(tag => tag, StringComparer.CurrentCulture).ToList();
    }
}
