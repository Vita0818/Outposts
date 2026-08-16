//
//  Localization.cs
//  Kikaria-Windows
//
//  中英映射表与文案模板,完整照抄自 Kikaria-Apple 的 KikariaTypography.swift。
//  Windows 版 UI 默认中文(UsesEnglish 恒为 false),保留英文映射表供 Core 侧
//  与后续本地化使用。所有文案与 Apple 源逐字一致。
//

namespace Kikaria.Core;

public static class Localization
{
    /// <summary>Windows 版固定使用中文;保留该开关以对齐 Apple 版行为。</summary>
    public static bool UsesEnglish => false;

    /// <summary>查表(中文模式直接返回原文;对应 Apple 版 KikariaLocalization.string)。</summary>
    public static string Localize(string source)
    {
        if (!UsesEnglish)
        {
            return source;
        }

        return EnglishStrings.TryGetValue(source, out var translated) ? translated : source;
    }

    public static string CountdownText(int? days)
    {
        if (days is null)
        {
            return "--";
        }

        if (UsesEnglish)
        {
            return days.Value + " " + (days.Value == 1 ? "day" : "days");
        }

        return days.Value + " 天";
    }

    public static string DaysLeftText(int? days)
    {
        var value = days?.ToString() ?? "--";
        return UsesEnglish ? value + " left" : "剩余 " + value + " 天";
    }

    public static string GoalUnit(int count) => UsesEnglish ? count.ToString() : count + " 个";

    public static string KnowledgePointCount(int count) => UsesEnglish ? count + " pts" : count + " 个知识点";

    public static string RecordCount(int count) => UsesEnglish ? count + " records" : count + " 条记录";

    public static string SelectedTagsSummary(bool isEmpty, int count)
    {
        if (UsesEnglish)
        {
            return isEmpty ? "No tags selected. All points are included." : count + " tags selected.";
        }

        return isEmpty ? "未选择标签时，会默认使用全部知识点。" : "已选择 " + count + " 个标签。";
    }

    public static string TodayReviewCount(int count) =>
        UsesEnglish ? count + " reviews today" : "该知识点今日复习 " + count + " 次";

    public static string ProgressMessage(int markedMasteredCount, int reviewedAnswerCount, int dailyGoal)
    {
        if (UsesEnglish)
        {
            if (markedMasteredCount >= dailyGoal)
            {
                return "Goal done. Keep the pace.";
            }

            if (reviewedAnswerCount > 0)
            {
                return Math.Max(0, dailyGoal - markedMasteredCount) + " more to goal.";
            }

            return "Quiet day. Start with one point.";
        }

        if (markedMasteredCount >= dailyGoal)
        {
            return "今日目标已经达成，保持这份节奏就很好。";
        }

        if (reviewedAnswerCount > 0)
        {
            return "今日已经进入状态，还差 " + Math.Max(0, dailyGoal - markedMasteredCount) + " 个新增掌握达到目标。";
        }

        return "今天还很安静，可以从一个知识点开始。";
    }

    public static string NotificationBody(string presetName) =>
        UsesEnglish
            ? "Today's " + presetName + " goal is still behind."
            : "今天的「" + presetName + "」学习量尚未达标哦，抓紧学习吧！";

    public static string PresetSwitchedToast(string presetName) =>
        UsesEnglish ? "Switched to " + presetName : "已切换至「" + presetName + "」";

    public static string PresetCreatedToast(string presetName) =>
        UsesEnglish ? "Created " + presetName : "已创建「" + presetName + "」";

    public static string PresetDeletedToast(string presetName) =>
        UsesEnglish ? "Deleted " + presetName : "已删除「" + presetName + "」";

    public static string PointsUpdatedToast(int count) =>
        UsesEnglish ? "Updated " + count + " points" : "已更新 " + count + " 个知识点";

    public static string MasteredToast(string title) =>
        UsesEnglish ? title + " mastered" : title + " 已掌握";

    public static string RemovedFocusToast(string title) =>
        UsesEnglish ? title + " removed from Focus" : title + " 已移出重点集锦";

    public static string RemovedMasteredToast(string title) =>
        UsesEnglish ? title + " removed from Mastered" : title + " 已移出已掌握";

    public static string AddedFocusToast(string title, int count)
    {
        if (UsesEnglish)
        {
            return count <= 1 ? title + " added to Focus" : title + " added to Focus x" + count;
        }

        return count <= 1 ? title + " 已加入重点集锦" : title + " 已加入重点集锦 ×" + count;
    }

    public static string AddFocusButtonTitle(int count)
    {
        if (UsesEnglish)
        {
            return count > 0 ? "Add Again x" + count : "Add Focus";
        }

        return count > 0 ? "再次加入 ×" + count : "加入重点集锦";
    }

    public static string BuiltInPresetDisplayName(string name, bool isBuiltIn)
    {
        if (!isBuiltIn || !UsesEnglish)
        {
            return name;
        }

        return BuiltInPresetNames.TryGetValue(name, out var translated) ? translated : name;
    }

    public static string[] WeekdaySymbols => UsesEnglish
        ? new[] { "M", "T", "W", "T", "F", "S", "S" }
        : new[] { "一", "二", "三", "四", "五", "六", "日" };

    public static string MonthTitle(DateTime date) =>
        UsesEnglish ? date.ToString("MMM yyyy") : date.Year + "年 " + date.Month + "月";

    public static string MonthDayTitle(DateTime date) =>
        UsesEnglish ? date.ToString("MMM d") : date.Month + "月" + date.Day + "日";

    public static string HomeDateTitle(DateTime date) =>
        UsesEnglish ? date.ToString("MMM d") : date.Month + "月" + date.Day + "日";

    public static string MarkdownFormatTemplate => UsesEnglish ? """
        # Point title

        tags: Tag 1, Tag 2, Tag 3

        hint:
        Write a short recall cue here.

        content:
        Write the full answer or material here.

        ---
        """ : """
        # 知识点名称

        tags: 标签1, 标签2, 标签3

        hint:
        这里写提示，可以是一句话，也可以是几行文字。

        content:
        这里写完整答案或背诵内容，可以是一段或多段文字。

        ---
        """;

    public static string MarkdownCompleteExample => UsesEnglish ? """
        # Limit Sign Preservation

        tags: Calculus, Limits, Basics

        hint:
        If the limit is positive, nearby values are positive too.

        content:
        If lim f(x) = A and A > 0, then in some punctured neighborhood, f(x) > 0.

        ---

        # Rolle's Theorem

        tags: Calculus, Mean Value

        hint:
        Continuous on the closed interval, differentiable inside, equal endpoint values.

        content:
        If f(x) is continuous on [a,b], differentiable on (a,b), and f(a)=f(b), then some ξ∈(a,b) satisfies f'(ξ)=0.
        """ : """
        # 极限的保号性

        tags: 高等数学, 极限, 基础

        hint:
        当函数极限大于 0 时，函数值在充分靠近该点时也大于 0。

        content:
        若 lim f(x) = A，且 A > 0，则存在某个去心邻域，使得在该邻域内 f(x) > 0。

        ---

        # 罗尔定理

        tags: 高等数学, 中值定理

        hint:
        闭区间连续，开区间可导，两端函数值相等。

        content:
        若函数 f(x) 在 [a,b] 上连续，在 (a,b) 内可导，且 f(a)=f(b)，则至少存在一点 ξ∈(a,b)，使得 f'(ξ)=0。
        """;

    public static string MarkdownLatexExample => UsesEnglish ? """
        Kikaria renders formulas locally with SwiftMath.

        Recommended: keep prose outside formulas.

        The derivative of $f(x)=x^2$ is $2x$.

        As x approaches 0:

        $$
        \lim_{x\to0}\frac{\sin x}{x}=1
        $$

        Not recommended: LaTeX without $ delimiters will not render.

        \Delta\varphi=0
        """ : """
        Kikaria 使用本地 SwiftMath 渲染公式，不会联网处理。

        推荐：中文说明放在公式外

        函数 $f(x)=x^2$ 的导数是 $2x$。

        当 x 接近 0 时，有：

        $$
        \lim_{x\to0}\frac{\sin x}{x}=1
        $$

        不推荐：没有 $ 包裹的 LaTeX 不会渲染

        \Delta\varphi=0
        """;

    public static string MarkdownAIPrompt => UsesEnglish ? """
        Convert the study material I provide into structured Markdown knowledge points for Kikaria.

        Follow this format exactly:

        # Point title

        tags: Tag 1, Tag 2, Tag 3

        hint:
        Give a concise recall cue. Do not reveal the full answer.

        content:
        Write the complete, accurate material for memorization.

        ---

        Requirements:
        1. Separate points with a line containing only ---.
        2. Each point must include title, tags, hint, and content.
        3. Separate tags with commas.
        4. Keep hints short.
        5. Keep content complete and memorizable.
        6. Do not add extra explanation.
        7. Do not use tables.
        8. Do not merge several points into one.
        9. Split long material into smaller points.
        10. Output Markdown only.
        11. Math formulas may use LaTeX. Kikaria renders them locally with SwiftMath.
        12. Only $...$ and $$...$$ render as formulas.
        13. Use $...$ inline and $$...$$ for block formulas.
        14. Keep prose outside formula environments.

        Material to organize:

        [Paste textbook, notes, lecture text, or OCR text here]
        """ : """
        请你把我提供的学习资料整理成 Kikaria 背诵 App 支持的结构化 Markdown 知识点。

        格式必须严格遵守：

        # 知识点名称

        tags: 标签1, 标签2, 标签3

        hint:
        用简洁语言给出背诵提示，不要直接泄露完整答案。

        content:
        写出完整、准确、适合背诵的知识点内容。

        ---

        要求：
        1. 每个知识点之间必须用单独一行 --- 分隔。
        2. 每个知识点都必须包含标题、tags、hint、content 四部分。
        3. tags 后的标签用逗号分隔。
        4. hint 要简短，适合作为回忆提示。
        5. content 要完整、准确、适合直接背诵。
        6. 不要生成多余解释。
        7. 不要使用表格。
        8. 不要把多个知识点混在一起。
        9. 如果原资料太长，请拆分成多个小知识点。
        10. 输出结果只保留 Markdown 内容，不要添加寒暄或说明。
        11. 数学公式可以使用 LaTeX，Kikaria 会用本地 SwiftMath 渲染，不会联网处理。
        12. 只有 $...$ 和 $$...$$ 中的内容会渲染为公式；没有包裹的 LaTeX 命令会按普通文本保留。
        13. 行内公式用 $...$，块级公式用 $$...$$。
        14. 公式环境中不要混入中文，中文解释要写在公式外；必要时可少量使用 \text{...}。

        下面是需要整理的资料：

        【在这里粘贴课本、讲义、笔记或 OCR 文本】
        """;

    private static readonly Dictionary<string, string> BuiltInPresetNames = new()
    {
        ["大学物理"] = "Physics",
        ["大学英语Band4"] = "CET-4 English",
        ["微积分"] = "Calculus",
        ["离散数学"] = "Discrete Math",
        ["离散数学_BACKUP"] = "Discrete Math Backup",
        ["内置预设"] = "Built-in"
    };

    // 与 KikariaTypography.swift 的 englishStrings 完全一致。
    private static readonly Dictionary<string, string> EnglishStrings = new()
    {
        ["仪表盘"] = "Dashboard",
        ["今日概览"] = "Today",
        ["重点集锦"] = "Focus",
        ["已掌握"] = "Mastered",
        ["预设管理"] = "Presets",
        ["打开设置"] = "Open Settings",
        ["开始背诵"] = "Start Review",
        ["当前预设"] = "Preset",
        ["预设不存在"] = "Preset not found",
        ["请返回后重新选择预设。"] = "Go back and pick a preset.",
        ["知识点不存在"] = "Point not found",
        ["请返回后重新选择知识点。"] = "Go back and pick a point.",
        ["请在系统设置中允许通知"] = "Enable notifications in Settings",
        ["通知权限不可用"] = "Notifications unavailable",
        ["提醒将在 5 秒后发送"] = "Reminder sends in 5s",
        ["提醒发送失败"] = "Reminder failed",
        ["请填写预设名称。"] = "Enter a preset name.",
        ["没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。"] = "No valid points. Check # title, tags, hint:, and content:.",
        ["自定义知识点"] = "Custom points",
        ["自定义"] = "Custom",
        ["选择一套预设"] = "Pick a Preset",
        ["从数学、物理、计算机科学与英语预设开始，也可以上传自己的 Markdown 知识点。"] = "Start with built-in subjects or import Markdown.",
        ["先回忆，再查看"] = "Recall First",
        ["背诵时先看知识点名称，必要时查看提示，再查看答案。"] = "Review the title, reveal a hint, then the answer.",
        ["整理你的学习状态"] = "Track Progress",
        ["把不熟的内容加入重点集锦，把已经掌握的内容标记为已掌握。"] = "Add weak points to Focus and mark learned points Mastered.",
        ["开始使用"] = "Start",
        ["下一步"] = "Next",
        ["搜索知识点"] = "Search points",
        ["搜索标签或知识点"] = "Search tags or points",
        ["返回"] = "Back",
        ["导出文件已准备好"] = "Export ready",
        ["完成"] = "Done",
        ["预设"] = "Preset",
        ["今日新增已掌握"] = "New Mastered",
        ["查看答案"] = "Answers",
        ["总已掌握"] = "Total Mastered",
        ["查看提示"] = "Hints",
        ["倒数"] = "Countdown",
        ["复习历史"] = "History",
        ["新增掌握"] = "New Mastered",
        ["加入重点"] = "Added Focus",
        ["这一天还没有学习记录。"] = "No study records for this day.",
        ["编辑个人资料"] = "Edit Profile",
        ["学习"] = "Study",
        ["每日学习目标"] = "Daily Goal",
        ["倒数日"] = "Countdown",
        ["进度安全线"] = "Safety Line",
        ["通知"] = "Notifications",
        ["学习进度通知"] = "Progress Alerts",
        ["通知时间"] = "Alert Time",
        ["需设置倒数日"] = "Set countdown first",
        ["预览提醒"] = "Preview Alert",
        ["帮助"] = "Help",
        ["新手引导"] = "Onboarding",
        ["Markdown 格式"] = "Markdown Format",
        ["关于"] = "About",
        ["隐私政策"] = "Privacy",
        ["版权声明"] = "Copyright",
        ["版本"] = "Version",
        ["设置"] = "Settings",
        ["未设置"] = "Not set",
        ["结束日期不能早于开始日期。"] = "End date cannot be before start.",
        ["结束日期不能早于开始日期"] = "End date cannot be before start",
        ["知道了"] = "OK",
        ["Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。学习进度通知使用 iOS 本地通知，不会上传到服务器。"] = "Kikaria stores study data, presets, avatar, and progress on this device. Progress alerts use local notifications and are not uploaded.",
        ["开始日期"] = "Start",
        ["结束日期"] = "End",
        ["清除"] = "Clear",
        ["切换预设"] = "Switch Preset",
        ["上传新预设"] = "New Preset",
        ["切换预设？"] = "Switch preset?",
        ["取消"] = "Cancel",
        ["确认切换"] = "Switch",
        ["将切换到另一套知识点。当前预设的学习进度会被保留。"] = "Switch to another point set. Current progress is kept.",
        ["删除预设？"] = "Delete preset?",
        ["删除"] = "Delete",
        ["删除后将移除该预设的所有知识点、重点集锦、已掌握状态和学习记录。"] = "This removes its points, Focus, Mastered state, and history.",
        ["预设解析失败，请稍后再试"] = "Preset failed to parse.",
        ["至少需要保留一个预设"] = "Keep at least one preset",
        ["当前"] = "Current",
        ["保存"] = "Save",
        ["预设名称"] = "Preset Name",
        ["分类"] = "Category",
        ["选择 .md / .txt 文件"] = "Choose .md / .txt",
        ["Markdown 文本"] = "Markdown",
        ["如何编写 Markdown 预设？"] = "Markdown help",
        ["文件读取失败，请确认它是 UTF-8 文本。"] = "Could not read file. Use UTF-8 text.",
        ["文件选择失败，请重试。"] = "File selection failed.",
        ["Markdown 格式说明"] = "Markdown Guide",
        ["Kikaria 使用结构化 Markdown 来导入知识点。每个知识点由标题、标签、提示和答案组成。多个知识点之间使用 --- 分隔。"] = "Kikaria imports structured Markdown. Each point has a title, tags, hint, and answer. Separate points with ---.",
        ["格式规则"] = "Format",
        ["多个知识点之间用一行 --- 分隔。"] = "Separate points with a line containing ---.",
        ["规则说明"] = "Rules",
        ["标题必须以 # 开头。"] = "Titles start with #.",
        ["tags: 后面写标签，多个标签用英文逗号或中文逗号分隔。"] = "Put tags after tags:, separated by commas.",
        ["hint: 后面写提示。"] = "Put the cue after hint:.",
        ["content: 后面写完整内容。"] = "Put the full answer after content:.",
        ["每个知识点之间用单独一行 --- 分隔。"] = "Use a standalone --- between points.",
        ["建议每个知识点不要太长，适合一次背诵。"] = "Keep each point short enough to review once.",
        ["标签可以用于后续选择背诵范围。"] = "Tags help choose review scope.",
        ["LaTeX 公式"] = "LaTeX",
        ["Kikaria 使用本地 SwiftMath 渲染公式，不会联网处理。"] = "Kikaria renders formulas locally with SwiftMath.",
        ["行内公式必须写成：$f(x)=x^2$。"] = "Inline formula: $f(x)=x^2$.",
        ["块级公式必须写成：用 $$...$$ 单独成块。"] = "Block formula: put $$...$$ on its own block.",
        ["只有 $...$ 和 $$...$$ 中的内容会渲染为公式。"] = "Only $...$ and $$...$$ render as formulas.",
        ["没有包裹的 LaTeX 命令不会自动识别，会按普通文本显示。"] = "Undelimited LaTeX stays plain text.",
        ["公式环境中不要混入中文，中文说明应放在公式外。"] = "Keep prose outside formula environments.",
        ["App 会尽量渲染 \\text{...}，但不建议在复杂公式里滥用中文。"] = "Kikaria tries to render \\text{...}; avoid heavy prose inside formulas.",
        ["矩阵、cases、align 等复杂结构会尽量交给 SwiftMath 渲染；失败时显示原始源码。"] = "Complex matrix/cases/align blocks use SwiftMath; failures show source.",
        ["导入、编辑和导出都会保留原始 LaTeX 源码。"] = "Import, edit, and export preserve LaTeX source.",
        ["完整示例"] = "Example",
        ["给 AI 助手的 Prompt"] = "AI Prompt",
        ["复制 Prompt"] = "Copy Prompt",
        ["你可以把下面这段 prompt 复制给 AI 助手，并附上你的课本、讲义、笔记或照片识别出的文本，让 AI 帮你整理成 Kikaria 支持的 Markdown 格式。"] = "Copy this prompt to an AI assistant with your textbook, notes, or OCR text to produce Kikaria Markdown.",
        ["Prompt 已复制"] = "Prompt copied",
        ["编辑预设"] = "Edit Preset",
        ["导出 Markdown"] = "Export Markdown",
        ["没有找到相关知识点"] = "No matching points",
        ["没有找到相关标签"] = "No matching tags",
        ["换个关键词试试看。"] = "Try another keyword.",
        ["删除此预设"] = "Delete Preset",
        ["删除知识点？"] = "Delete point?",
        ["删除后，该知识点的重点集锦、已掌握和今日复习次数也会一并移除。"] = "This also removes its Focus, Mastered state, and today's review count.",
        ["此操作会删除该自定义预设和它的学习状态。"] = "This deletes the custom preset and its study state.",
        ["导出失败"] = "Export failed",
        ["添加知识点"] = "Add Point",
        ["编辑知识点"] = "Edit Point",
        ["标题"] = "Title",
        ["标签，用逗号分隔"] = "Tags, comma-separated",
        ["提示"] = "Hint",
        ["答案"] = "Answer",
        ["标题、提示和答案都不能为空。"] = "Title, hint, and answer are required.",
        ["欢迎使用 Kikaria"] = "Welcome to Kikaria",
        ["先设置你的个人资料"] = "Set up your profile",
        ["选择头像"] = "Choose Avatar",
        ["昵称"] = "Name",
        ["用户名"] = "Username",
        ["头像加载失败"] = "Avatar failed to load",
        ["更换头像"] = "Change Avatar",
        ["显示名称"] = "Display Name",
        ["用户 ID"] = "User ID",
        ["知识点上传"] = "Upload Points",
        ["应用"] = "Apply",
        ["范围"] = "Scope",
        ["选择范围"] = "Choose Scope",
        ["暂无知识点"] = "No points",
        ["请返回后调整选择范围。"] = "Go back and adjust scope.",
        ["下一个"] = "Next",
        ["再次加入"] = "Add Again",
        ["加入重点集锦"] = "Add Focus",
        ["移出重点集锦"] = "Remove Focus",
        ["移出已掌握"] = "Remove Mastered",
        ["加入已掌握"] = "Mark Mastered",
        ["已设定为掌握"] = "Already Mastered",
        ["返回首页"] = "Home",
        ["还没有重点"] = "No Focus Yet",
        ["在背诵时查看答案后，可以把知识点加入这里。"] = "After revealing answers, add weak points here.",
        ["还没有已掌握"] = "No Mastered Yet",
        ["在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。"] = "After revealing answers, mark familiar points here.",
        ["开始重点背诵"] = "Review Focus",
        ["开始复习"] = "Start Review",
        ["暂无内容"] = "No content",
        ["浙ICP备2026034004号"] = "ICP 2026034004"
    };

    // ---------------------------------------------------------------------------
    // 混排辅助(对齐 KikariaTypography.mixedText 的中文/衬线分轨逻辑)。
    // Windows 版正文统一无衬线,数学 fallback 用衬线斜体;此表供需要时切分。
    // ---------------------------------------------------------------------------

    private static readonly HashSet<char> ChineseSystemPunctuation = new(
        "，。、；：？！“”‘’（）《》【】「」『』—…·￥");

    public enum MixedRunStyle
    {
        Chinese,
        Serif
    }

    public static bool IsChineseScalar(int code)
    {
        return code is (>= 0x3400 and <= 0x4DBF)
            or (>= 0x4E00 and <= 0x9FFF)
            or (>= 0xF900 and <= 0xFAFF)
            or (>= 0x20000 and <= 0x2A6DF)
            or (>= 0x2A700 and <= 0x2B73F)
            or (>= 0x2B740 and <= 0x2B81F)
            or (>= 0x2B820 and <= 0x2CEAF)
            or (>= 0x2CEB0 and <= 0x2EBEF)
            or (>= 0x3000 and <= 0x303F)
            or (>= 0xFF00 and <= 0xFFEF);
    }

    public static MixedRunStyle MixedRunStyleForChar(char c)
    {
        if (ChineseSystemPunctuation.Contains(c))
        {
            return MixedRunStyle.Chinese;
        }

        return IsChineseScalar(c) ? MixedRunStyle.Chinese : MixedRunStyle.Serif;
    }

    public static List<(string Text, MixedRunStyle Style)> MixedRuns(string value)
    {
        var runs = new List<(string Text, MixedRunStyle Style)>();
        var current = new StringBuilder();
        MixedRunStyle? currentStyle = null;

        foreach (var c in value)
        {
            var style = MixedRunStyleForChar(c);
            if (currentStyle is not null && currentStyle.Value != style)
            {
                runs.Add((current.ToString(), currentStyle.Value));
                current.Clear();
            }

            current.Append(c);
            currentStyle = style;
        }

        if (currentStyle is not null && current.Length > 0)
        {
            runs.Add((current.ToString(), currentStyle.Value));
        }

        return runs;
    }
}
