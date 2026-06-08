import SwiftUI
import UIKit
import WidgetKit

private enum WidgetTheme {
    private static func adaptive(
        light: (CGFloat, CGFloat, CGFloat),
        dark: (CGFloat, CGFloat, CGFloat)
    ) -> Color {
        Color(
            UIColor { traits in
                let color = traits.userInterfaceStyle == .dark ? dark : light
                return UIColor(red: color.0, green: color.1, blue: color.2, alpha: 1)
            }
        )
    }

    static let backgroundGradient = LinearGradient(
        colors: [
            adaptive(light: (0.93, 0.98, 1.0), dark: (0.02, 0.07, 0.11)),
            adaptive(light: (0.80, 0.94, 0.98), dark: (0.04, 0.15, 0.20))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
}

private struct WidgetSnapshot: Codable {
    var presetName: String
    var todayMasteredCount: Int
    var masteredCount: Int
    var dailyGoal: Int
    var countdownDays: Int?
    var todayReviewCount: Int
    var todayHintCount: Int
    var randomKnowledgePoints: [WidgetKnowledgePointPreview]
    var lastUpdated: Date

    init(
        presetName: String,
        todayMasteredCount: Int = 0,
        masteredCount: Int,
        dailyGoal: Int,
        countdownDays: Int?,
        todayReviewCount: Int,
        todayHintCount: Int = 0,
        randomKnowledgePoints: [WidgetKnowledgePointPreview] = [],
        lastUpdated: Date
    ) {
        self.presetName = presetName
        self.todayMasteredCount = todayMasteredCount
        self.masteredCount = masteredCount
        self.dailyGoal = dailyGoal
        self.countdownDays = countdownDays
        self.todayReviewCount = todayReviewCount
        self.todayHintCount = todayHintCount
        self.randomKnowledgePoints = randomKnowledgePoints
        self.lastUpdated = lastUpdated
    }

    enum CodingKeys: String, CodingKey {
        case presetName
        case todayMasteredCount
        case masteredCount
        case dailyGoal
        case countdownDays
        case todayReviewCount
        case todayHintCount
        case randomKnowledgePoints
        case lastUpdated
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        presetName = try container.decodeIfPresent(String.self, forKey: .presetName) ?? "Kikaria"
        todayMasteredCount = try container.decodeIfPresent(Int.self, forKey: .todayMasteredCount) ?? 0
        masteredCount = try container.decodeIfPresent(Int.self, forKey: .masteredCount) ?? 0
        dailyGoal = try container.decodeIfPresent(Int.self, forKey: .dailyGoal) ?? 20
        countdownDays = try container.decodeIfPresent(Int.self, forKey: .countdownDays)
        todayReviewCount = try container.decodeIfPresent(Int.self, forKey: .todayReviewCount) ?? 0
        todayHintCount = try container.decodeIfPresent(Int.self, forKey: .todayHintCount) ?? 0
        randomKnowledgePoints = try container.decodeIfPresent([WidgetKnowledgePointPreview].self, forKey: .randomKnowledgePoints) ?? []
        lastUpdated = try container.decodeIfPresent(Date.self, forKey: .lastUpdated) ?? Date()
    }

    static let placeholder = WidgetSnapshot(
        presetName: "高等数学知识点",
        todayMasteredCount: 0,
        masteredCount: 0,
        dailyGoal: 20,
        countdownDays: nil,
        todayReviewCount: 0,
        todayHintCount: 0,
        randomKnowledgePoints: [
            WidgetKnowledgePointPreview(title: "极限的保号性", tag: "极限"),
            WidgetKnowledgePointPreview(title: "罗尔定理", tag: "中值定理"),
            WidgetKnowledgePointPreview(title: "Bayes' Theorem", tag: "Probability"),
            WidgetKnowledgePointPreview(title: "Linear Independence", tag: "Linear Algebra"),
            WidgetKnowledgePointPreview(title: "Derivative Product Rule", tag: "Calculus")
        ],
        lastUpdated: Date()
    )

    static let preview = WidgetSnapshot(
        presetName: "高等数学知识点",
        todayMasteredCount: 3,
        masteredCount: 42,
        dailyGoal: 20,
        countdownDays: 32,
        todayReviewCount: 8,
        todayHintCount: 5,
        randomKnowledgePoints: [
            WidgetKnowledgePointPreview(title: "Derivative Product Rule", tag: "Calculus"),
            WidgetKnowledgePointPreview(title: "Limit Preservation of Sign", tag: "Limit"),
            WidgetKnowledgePointPreview(title: "Bayes' Theorem", tag: "Probability"),
            WidgetKnowledgePointPreview(title: "Linear Independence", tag: "Linear Algebra"),
            WidgetKnowledgePointPreview(title: "Rolle's Theorem", tag: "Mean Value")
        ],
        lastUpdated: Date()
    )
}

private struct WidgetKnowledgePointPreview: Codable, Equatable {
    var title: String
    var tag: String?
}

private enum WidgetDataStore {
    static let appGroupID = "group.com.vita0818.kikaria"
    static let snapshotKey = "kikaria.widgetSnapshot"

    static func loadSnapshot() -> WidgetSnapshot {
        if let appGroupDefaults = UserDefaults(suiteName: appGroupID),
           let data = appGroupDefaults.data(forKey: snapshotKey),
           let snapshot = try? JSONDecoder().decode(WidgetSnapshot.self, from: data) {
            return snapshot
        }

        if let data = UserDefaults.standard.data(forKey: snapshotKey),
           let snapshot = try? JSONDecoder().decode(WidgetSnapshot.self, from: data) {
            return snapshot
        }

        return .placeholder
    }
}

private struct KikariaWidgetEntry: TimelineEntry {
    let date: Date
    let snapshot: WidgetSnapshot
}

private struct KikariaWidgetProvider: TimelineProvider {
    func placeholder(in context: Context) -> KikariaWidgetEntry {
        KikariaWidgetEntry(date: Date(), snapshot: .placeholder)
    }

    func getSnapshot(in context: Context, completion: @escaping (KikariaWidgetEntry) -> Void) {
        completion(KikariaWidgetEntry(date: Date(), snapshot: WidgetDataStore.loadSnapshot()))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<KikariaWidgetEntry>) -> Void) {
        let entry = KikariaWidgetEntry(date: Date(), snapshot: WidgetDataStore.loadSnapshot())
        let nextRefresh = Calendar.current.date(byAdding: .minute, value: 30, to: Date()) ?? Date().addingTimeInterval(1800)
        completion(Timeline(entries: [entry], policy: .after(nextRefresh)))
    }
}

private struct KikariaWidgetView: View {
    @Environment(\.widgetFamily) private var widgetFamily
    @Environment(\.colorScheme) private var colorScheme
    let entry: KikariaWidgetEntry

    var body: some View {
        ZStack {
            if shouldDrawInlineWidgetGradient {
                widgetGradient
            }

            Circle()
                .fill(widgetHighlight.opacity(colorScheme == .dark ? 0.16 : 0.26))
                .frame(width: 118, height: 118)
                .blur(radius: 1.5)
                .offset(x: -54, y: -46)

            Circle()
                .fill(widgetAccent.opacity(colorScheme == .dark ? 0.22 : 0.24))
                .frame(width: 96, height: 96)
                .blur(radius: 1.0)
                .offset(x: 58, y: 48)

            switch widgetFamily {
            case .systemLarge:
                largeContent
            case .systemMedium:
                mediumContent
            case .systemSmall:
                smallContent
            default:
                smallContent
            }
        }
        .widgetBackground()
    }

    private var shouldDrawInlineWidgetGradient: Bool {
        !(widgetFamily == .systemLarge && colorScheme == .dark)
    }

    private var widgetGradient: LinearGradient {
        LinearGradient(
            colors: colorScheme == .dark
                ? [
                    Color(red: 0.02, green: 0.07, blue: 0.11),
                    Color(red: 0.04, green: 0.15, blue: 0.20)
                ]
                : [
                    Color(red: 0.93, green: 0.98, blue: 1.0),
                    Color(red: 0.80, green: 0.94, blue: 0.98)
                ],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }

    private var widgetPrimaryText: Color {
        colorScheme == .dark
            ? Color(red: 0.90, green: 0.96, blue: 1.0)
            : Color(red: 0.13, green: 0.25, blue: 0.33)
    }

    private var widgetSecondaryText: Color {
        colorScheme == .dark
            ? Color(red: 0.66, green: 0.77, blue: 0.86)
            : Color(red: 0.42, green: 0.54, blue: 0.62)
    }

    private var widgetAccent: Color {
        colorScheme == .dark
            ? Color(red: 0.32, green: 0.80, blue: 0.82)
            : Color(red: 0.57, green: 0.88, blue: 0.91)
    }

    private var widgetHighlight: Color {
        colorScheme == .dark
            ? Color(red: 0.25, green: 0.53, blue: 0.70)
            : .white
    }

    private var widgetMasteredText: Color {
        colorScheme == .dark
            ? Color(red: 0.58, green: 0.94, blue: 0.74)
            : Color(red: 0.12, green: 0.47, blue: 0.30)
    }

    private var widgetCardFill: Color {
        colorScheme == .dark
            ? Color(red: 0.06, green: 0.13, blue: 0.18).opacity(0.58)
            : Color.white.opacity(0.48)
    }

    private var largeWidgetCardFill: Color {
        colorScheme == .dark ? Color.white.opacity(0.10) : widgetCardFill
    }

    private var widgetDateText: String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "MMM"
        let month = formatter.string(from: entry.date)
        let day = Calendar.current.component(.day, from: entry.date)
        return "\(month) \(day)\(ordinalSuffix(for: day))"
    }

    private var previewPoints: [WidgetKnowledgePointPreview] {
        Array(entry.snapshot.randomKnowledgePoints.prefix(2))
    }

    private var largePreviewPoints: [WidgetKnowledgePointPreview] {
        Array(entry.snapshot.randomKnowledgePoints.prefix(4))
    }

    private var countdownText: String {
        entry.snapshot.countdownDays.map(String.init) ?? "--"
    }

    private var smallContent: some View {
        VStack(alignment: .leading, spacing: 7) {
            Text("Kikaria")
                .font(.system(size: 22, weight: .semibold, design: .serif))
                .foregroundStyle(widgetPrimaryText)

            Text(entry.snapshot.presetName)
                .font(.system(size: 12, weight: .semibold))
                .foregroundStyle(widgetSecondaryText)
                .lineLimit(1)
                .truncationMode(.tail)
                .minimumScaleFactor(0.72)

            Spacer(minLength: 0)

            VStack(alignment: .leading, spacing: 2) {
                Text(widgetDateText)
                    .font(.system(size: 15, weight: .semibold, design: .serif))
                    .foregroundStyle(widgetPrimaryText)
                    .lineLimit(1)

                Text("\(entry.snapshot.todayMasteredCount) / \(entry.snapshot.dailyGoal)")
                    .font(.system(size: 34, weight: .bold, design: .serif))
                    .monospacedDigit()
                    .foregroundStyle(widgetMasteredText)
                    .lineLimit(1)
                    .minimumScaleFactor(0.72)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
        .padding(15)
    }

    private var mediumContent: some View {
        GeometryReader { proxy in
            let leadingPadding: CGFloat = 12
            let trailingPadding: CGFloat = 2
            let columnSpacing: CGFloat = 4
            let contentWidth = max(0, proxy.size.width - leadingPadding - trailingPadding)
            let leftWidth = min(CGFloat(116), contentWidth * 0.38)
            let rightWidth = max(0, contentWidth - leftWidth - columnSpacing)

            HStack(alignment: .center, spacing: columnSpacing) {
                VStack(alignment: .leading, spacing: 7) {
                    Text("Kikaria")
                        .font(.system(size: 26, weight: .semibold, design: .serif))
                        .foregroundStyle(widgetPrimaryText)

                    Text(entry.snapshot.presetName)
                        .font(.system(size: 14, weight: .semibold))
                        .foregroundStyle(widgetSecondaryText)
                        .lineLimit(1)
                        .truncationMode(.tail)
                        .minimumScaleFactor(0.72)

                    Spacer(minLength: 4)

                    VStack(alignment: .leading, spacing: 2) {
                        Text(widgetDateText)
                            .font(.system(size: 15, weight: .semibold, design: .serif))
                            .foregroundStyle(widgetPrimaryText)
                            .lineLimit(1)

                        Text("\(entry.snapshot.todayMasteredCount) / \(entry.snapshot.dailyGoal)")
                            .font(.system(size: 34, weight: .bold, design: .serif))
                            .monospacedDigit()
                            .foregroundStyle(widgetMasteredText)
                            .lineLimit(1)
                            .minimumScaleFactor(0.78)
                    }
                    .offset(y: -6)
                }
                .frame(width: leftWidth, alignment: .leading)
                .frame(maxHeight: .infinity, alignment: .leading)

                VStack(spacing: 10) {
                    if previewPoints.isEmpty {
                        WidgetKnowledgePreviewCard(
                            title: "暂无知识点",
                            fill: widgetCardFill
                        )
                    } else {
                        ForEach(previewPoints.indices, id: \.self) { index in
                            WidgetKnowledgePreviewCard(
                                title: previewPoints[index].title,
                                fill: widgetCardFill
                            )
                        }
                    }
                }
                .frame(width: rightWidth, alignment: .trailing)
                .frame(maxHeight: .infinity, alignment: .center)
                .offset(y: -8)
            }
            .padding(.leading, leadingPadding)
            .padding(.trailing, trailingPadding)
            .padding(.vertical, 18)
            .offset(y: -8)
        }
    }

    private var largeContent: some View {
        GeometryReader { proxy in
            let metrics = largeWidgetListMetrics(
                size: proxy.size,
                pointCount: largePreviewPoints.isEmpty ? 1 : largePreviewPoints.count
            )
            let visiblePoints = Array(largePreviewPoints.prefix(metrics.capsuleCount))

            VStack(alignment: .leading, spacing: metrics.listTopSpacing) {
                largeTopInfoBar
                    .frame(width: metrics.contentWidth, height: metrics.topSectionHeight, alignment: .top)

                VStack(spacing: metrics.capsuleSpacing) {
                    if visiblePoints.isEmpty {
                        LargeRandomPointCapsule(
                            title: "暂无知识点",
                            fill: largeWidgetCardFill,
                            width: metrics.contentWidth,
                            height: metrics.capsuleHeight
                        )
                    } else {
                        ForEach(visiblePoints.indices, id: \.self) { index in
                            LargeRandomPointCapsule(
                                title: visiblePoints[index].title,
                                fill: largeWidgetCardFill,
                                width: metrics.contentWidth,
                                height: metrics.capsuleHeight
                            )
                        }
                    }
                }
                .frame(width: metrics.contentWidth, height: metrics.listHeight, alignment: .top)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
            .padding(.horizontal, metrics.horizontalPadding)
            .padding(.vertical, metrics.verticalPadding)
        }
    }

    private func largeWidgetListMetrics(size: CGSize, pointCount: Int) -> LargeWidgetListMetrics {
        let width = max(size.width, 1)
        let height = max(size.height, 1)

        let horizontalPadding = clamped(width * 0.038, lower: 12, upper: 15)
        let verticalPadding = clamped(height * 0.040, lower: 12, upper: 18)
        let contentWidth = max(0, width - horizontalPadding * 2)
        let contentHeight = max(0, height - verticalPadding * 2)
        let topSectionHeight = clamped(height * 0.215, lower: 64, upper: 82)
        let listTopSpacing = clamped(height * 0.022, lower: 7, upper: 11)
        let listHeight = max(0, contentHeight - topSectionHeight - listTopSpacing)

        let minimumCapsuleHeight: CGFloat = 38
        let maximumCapsuleHeight: CGFloat = 48
        let minimumSpacing: CGFloat = 6
        let maximumSpacing: CGFloat = 10

        var capsuleCount = max(1, min(4, pointCount))
        while capsuleCount > 1 {
            let requiredMinimumHeight = CGFloat(capsuleCount) * minimumCapsuleHeight
                + CGFloat(capsuleCount - 1) * minimumSpacing
            if requiredMinimumHeight <= listHeight {
                break
            }
            capsuleCount -= 1
        }

        var capsuleSpacing = clamped(listHeight * 0.045, lower: minimumSpacing, upper: maximumSpacing)
        let requiredHeightWithAdaptiveSpacing = CGFloat(capsuleCount) * minimumCapsuleHeight
            + CGFloat(max(capsuleCount - 1, 0)) * capsuleSpacing
        if requiredHeightWithAdaptiveSpacing > listHeight {
            capsuleSpacing = minimumSpacing
        }

        let rawCapsuleHeight = (
            listHeight - CGFloat(max(capsuleCount - 1, 0)) * capsuleSpacing
        ) / CGFloat(capsuleCount)
        let capsuleHeight = min(clamped(rawCapsuleHeight, lower: minimumCapsuleHeight, upper: maximumCapsuleHeight), listHeight)

        return LargeWidgetListMetrics(
            horizontalPadding: horizontalPadding,
            verticalPadding: verticalPadding,
            contentWidth: contentWidth,
            topSectionHeight: topSectionHeight,
            listTopSpacing: listTopSpacing,
            listHeight: listHeight,
            capsuleCount: capsuleCount,
            capsuleSpacing: capsuleSpacing,
            capsuleHeight: capsuleHeight
        )
    }

    private func clamped(_ value: CGFloat, lower: CGFloat, upper: CGFloat) -> CGFloat {
        min(max(value, lower), upper)
    }

    private var largeTopInfoBar: some View {
        HStack(alignment: .top, spacing: 12) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Kikaria")
                    .font(.system(size: 30, weight: .semibold, design: .serif))
                    .foregroundStyle(widgetPrimaryText)

                Text(entry.snapshot.presetName)
                    .font(.system(size: 14, weight: .semibold))
                    .foregroundStyle(widgetSecondaryText)
                    .lineLimit(1)
                    .truncationMode(.tail)
                    .minimumScaleFactor(0.72)
            }

            Spacer(minLength: 6)

            Text("\(entry.snapshot.todayMasteredCount) / \(entry.snapshot.dailyGoal)")
                .font(.system(size: 30, weight: .bold, design: .serif))
                .monospacedDigit()
                .foregroundStyle(widgetMasteredText)
                .lineLimit(1)
                .minimumScaleFactor(0.76)
                .padding(.top, 2)

            Spacer(minLength: 6)

            VStack(alignment: .center, spacing: 0) {
                Text(countdownText)
                    .font(.system(size: 30, weight: .bold, design: .serif))
                    .monospacedDigit()
                    .foregroundStyle(widgetPrimaryText)
                    .lineLimit(1)
                    .minimumScaleFactor(0.76)

                Text("Left")
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundStyle(widgetSecondaryText)
            }
        }
    }

    private func ordinalSuffix(for day: Int) -> String {
        let lastTwoDigits = day % 100
        if lastTwoDigits == 11 || lastTwoDigits == 12 || lastTwoDigits == 13 {
            return "th"
        }

        switch day % 10 {
        case 1:
            return "st"
        case 2:
            return "nd"
        case 3:
            return "rd"
        default:
            return "th"
        }
    }
}

private struct LargeWidgetListMetrics {
    let horizontalPadding: CGFloat
    let verticalPadding: CGFloat
    let contentWidth: CGFloat
    let topSectionHeight: CGFloat
    let listTopSpacing: CGFloat
    let listHeight: CGFloat
    let capsuleCount: Int
    let capsuleSpacing: CGFloat
    let capsuleHeight: CGFloat
}

private struct WidgetKnowledgePreviewCard: View {
    @Environment(\.colorScheme) private var colorScheme
    let title: String
    let fill: Color

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text(title)
                .font(.system(size: 14, weight: .semibold, design: .serif))
                .foregroundStyle(primaryText)
                .lineLimit(2)
                .truncationMode(.tail)
                .minimumScaleFactor(0.82)
        }
        .padding(.horizontal, 10)
        .padding(.vertical, 10)
        .frame(maxWidth: .infinity, minHeight: 56, alignment: .leading)
        .widgetGlassCard(cornerRadius: 20, fill: fill)
    }

    private var primaryText: Color {
        colorScheme == .dark
            ? Color(red: 0.90, green: 0.96, blue: 1.0)
            : Color(red: 0.13, green: 0.25, blue: 0.33)
    }

}

private struct LargeRandomPointCapsule: View {
    @Environment(\.colorScheme) private var colorScheme
    let title: String
    let fill: Color
    let width: CGFloat
    let height: CGFloat

    var body: some View {
        Text(title)
            .font(.system(size: 16, weight: .semibold, design: .serif))
            .foregroundStyle(primaryText)
            .lineLimit(1)
            .truncationMode(.tail)
            .minimumScaleFactor(0.78)
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .frame(width: width, height: height, alignment: .leading)
            .widgetGlassCard(cornerRadius: height / 2, fill: fill)
    }

    private var primaryText: Color {
        colorScheme == .dark
            ? Color(red: 0.90, green: 0.96, blue: 1.0)
            : Color(red: 0.13, green: 0.25, blue: 0.33)
    }
}

private extension View {
    func widgetGlassCard(cornerRadius: CGFloat, fill: Color) -> some View {
        background(fill, in: RoundedRectangle(cornerRadius: cornerRadius, style: .continuous))
            .overlay {
                RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
                    .stroke(
                        LinearGradient(
                            colors: [
                                .white.opacity(0.42),
                                .white.opacity(0.12),
                                Color(red: 0.54, green: 0.88, blue: 0.92).opacity(0.20)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 0.8
                    )
            }
            .shadow(color: Color.black.opacity(0.08), radius: 8, y: 4)
    }

    @ViewBuilder
    func widgetBackground() -> some View {
        if #available(iOSApplicationExtension 17.0, *) {
            containerBackground(for: .widget) {
                WidgetTheme.backgroundGradient
            }
        } else {
            background(WidgetTheme.backgroundGradient)
        }
    }
}

struct KikariaProgressWidget: Widget {
    let kind = "KikariaProgressWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: KikariaWidgetProvider()) { entry in
            KikariaWidgetView(entry: entry)
        }
        .configurationDisplayName("Kikaria 学习概览")
        .description("查看当前预设的今日完成和知识点预览。")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge])
    }
}

struct KikariaWidget_Previews: PreviewProvider {
    static var previews: some View {
        Group {
            KikariaWidgetView(entry: KikariaWidgetEntry(date: Date(), snapshot: .preview))
                .previewContext(WidgetPreviewContext(family: .systemSmall))

            KikariaWidgetView(entry: KikariaWidgetEntry(date: Date(), snapshot: .preview))
                .previewContext(WidgetPreviewContext(family: .systemMedium))

            KikariaWidgetView(entry: KikariaWidgetEntry(date: Date(), snapshot: .preview))
                .previewContext(WidgetPreviewContext(family: .systemLarge))
        }
    }
}

@main
struct KikariaWidgetBundle: WidgetBundle {
    var body: some Widget {
        KikariaProgressWidget()
    }
}
