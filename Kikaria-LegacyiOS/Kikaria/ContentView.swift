//
//  ContentView.swift
//  Kikaria
//
//  Created by Vita on 2026/5/1.
//

import SwiftUI
#if os(iOS)
import PhotosUI
import UIKit
#elseif os(macOS)
import AppKit
#endif
import UserNotifications
import UniformTypeIdentifiers

#if os(macOS)
private enum KikariaNavigationBarTitleDisplayMode {
    case inline
}

private enum KikariaTextInputAutocapitalization {
    case never
}

private extension View {
    func navigationBarTitleDisplayMode(_ mode: KikariaNavigationBarTitleDisplayMode) -> some View {
        self
    }

    func textInputAutocapitalization(_ mode: KikariaTextInputAutocapitalization) -> some View {
        self
    }
}
#endif

private extension View {
    @ViewBuilder
    func kikariaNavigationBarHidden(_ hidden: Bool) -> some View {
        #if os(iOS)
        navigationBarHidden(hidden)
        #else
        self
        #endif
    }

    @ViewBuilder
    func kikariaHiddenNavigationChrome() -> some View {
        #if os(iOS)
        if #available(iOS 16, *) {
            toolbar(.hidden, for: .navigationBar)
        } else {
            navigationBarHidden(true)
        }
        #else
        self
        #endif
    }

    @ViewBuilder
    func kikariaHomeNavigationChrome() -> some View {
        #if os(iOS)
        self
            .navigationTitle("")
            .navigationBarTitleDisplayMode(.inline)
            .navigationBarBackButtonHidden(true)
            .kikariaHiddenNavigationChrome()
        #else
        self
        #endif
    }

    @ViewBuilder
    func kikariaWheelPickerStyle() -> some View {
        #if os(iOS)
        pickerStyle(.wheel)
        #else
        pickerStyle(.menu)
        #endif
    }

    @ViewBuilder
    func kikariaMacPlainTextFieldStyle(_ enabled: Bool) -> some View {
        #if os(macOS)
        if enabled {
            textFieldStyle(.plain)
        } else {
            self
        }
        #else
        self
        #endif
    }

    @ViewBuilder
    func kikariaMacClearTextEditorBackground() -> some View {
        #if os(macOS)
        background(KikariaMacTextEditorBackgroundClearer())
        #else
        self
        #endif
    }

    @ViewBuilder
    func kikariaScrollIndicators(hidden: Bool) -> some View {
        if #available(iOS 16, *) {
            if hidden {
                self.scrollIndicators(.hidden)
            } else {
                self.scrollIndicators(.visible)
            }
        } else {
            self
        }
    }

    @ViewBuilder
    func kikariaHideScrollContentBackground() -> some View {
        if #available(iOS 16, *) {
            self.scrollContentBackground(.hidden)
        } else {
            self
        }
    }

    @ViewBuilder
    func kikariaEnableTextSelection() -> some View {
        if #available(iOS 16, *) {
            self.textSelection(.enabled)
        } else {
            self
        }
    }
}

#if os(macOS)
private struct KikariaMacTextEditorBackgroundClearer: NSViewRepresentable {
    func makeNSView(context: Context) -> NSView {
        let view = NSView()
        DispatchQueue.main.async {
            clearTextEditorBackground(around: view)
        }
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {
        DispatchQueue.main.async {
            clearTextEditorBackground(around: nsView)
        }
    }

    private func clearTextEditorBackground(around view: NSView) {
        var current: NSView? = view
        while let node = current {
            clearTextEditorBackground(in: node)
            current = node.superview
        }
    }

    private func clearTextEditorBackground(in view: NSView) {
        if let scrollView = view as? NSScrollView {
            scrollView.drawsBackground = false
            scrollView.backgroundColor = .clear
        }

        if let textView = view as? NSTextView {
            textView.drawsBackground = false
            textView.backgroundColor = .clear
            textView.insertionPointColor = NSColor(KikariaTheme.deepText)
        }

        for subview in view.subviews {
            clearTextEditorBackground(in: subview)
        }
    }
}

private extension View {
    @ViewBuilder
    func kikariaMacFirstLaunchOverlay<OverlayContent: View>(
        isPresented: Bool,
        @ViewBuilder content: @escaping () -> OverlayContent
    ) -> some View {
        GeometryReader { proxy in
            ZStack {
                self
                    .frame(width: proxy.size.width, height: proxy.size.height)

                if isPresented {
                    content()
                        .frame(width: proxy.size.width, height: proxy.size.height)
                        .zIndex(901)
                }
            }
        }
    }
}
#endif

private enum KikariaTheme {
    private struct RGBA {
        let red: CGFloat
        let green: CGFloat
        let blue: CGFloat
        let alpha: CGFloat
    }

    private static func rgb(_ red: Double, _ green: Double, _ blue: Double, _ alpha: Double = 1) -> RGBA {
        RGBA(
            red: CGFloat(red),
            green: CGFloat(green),
            blue: CGFloat(blue),
            alpha: CGFloat(alpha)
        )
    }

    private static func adaptive(light: RGBA, dark: RGBA) -> Color {
        #if os(iOS)
        Color(
            UIColor { traits in
                let color = traits.userInterfaceStyle == .dark ? dark : light
                return UIColor(
                    red: color.red,
                    green: color.green,
                    blue: color.blue,
                    alpha: color.alpha
                )
            }
        )
        #elseif os(macOS)
        Color(
            NSColor(name: nil) { appearance in
                let color = appearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua ? dark : light
                return NSColor(
                    calibratedRed: color.red,
                    green: color.green,
                    blue: color.blue,
                    alpha: color.alpha
                )
            }
        )
        #endif
    }

    static let sky = adaptive(light: rgb(0.39, 0.73, 0.96), dark: rgb(0.30, 0.72, 0.96))
    static let cyan = adaptive(light: rgb(0.57, 0.88, 0.91), dark: rgb(0.32, 0.80, 0.82))
    static let mist = adaptive(light: rgb(0.91, 0.97, 0.99), dark: rgb(0.08, 0.16, 0.22))
    static let blueGray = adaptive(light: rgb(0.62, 0.72, 0.80), dark: rgb(0.48, 0.61, 0.72))
    static let masteredGreen = adaptive(light: rgb(0.36, 0.76, 0.54), dark: rgb(0.32, 0.82, 0.60))
    static let masteredDeepGreen = adaptive(light: rgb(0.12, 0.47, 0.30), dark: rgb(0.58, 0.94, 0.74))
    static let masteredCompletedGreen = adaptive(light: rgb(0.79, 0.93, 0.84), dark: rgb(0.18, 0.38, 0.30))
    static let nextAmber = adaptive(light: rgb(0.54, 0.49, 0.75), dark: rgb(0.55, 0.46, 0.82))
    static let removeCoral = adaptive(light: rgb(0.86, 0.32, 0.30), dark: rgb(0.98, 0.42, 0.42))
    static let deepText = adaptive(light: rgb(0.13, 0.25, 0.33), dark: rgb(0.90, 0.96, 1.0))
    static let softText = adaptive(light: rgb(0.42, 0.54, 0.62), dark: rgb(0.66, 0.77, 0.86))
    static let tertiaryText = adaptive(light: rgb(0.58, 0.68, 0.76), dark: rgb(0.43, 0.55, 0.66))
    static let glassSurface = adaptive(light: rgb(1, 1, 1), dark: rgb(0.06, 0.13, 0.18))
    static let glassStrokeAccent = adaptive(light: rgb(0.57, 0.88, 0.91), dark: rgb(0.42, 0.84, 0.93))
    static let shadow = adaptive(light: rgb(0.39, 0.73, 0.96), dark: rgb(0.00, 0.02, 0.05))
    static let bubbleMint = adaptive(light: rgb(0.73, 0.95, 0.90), dark: rgb(0.20, 0.58, 0.54))
    static let bubbleLavender = adaptive(light: rgb(0.75, 0.78, 1.0), dark: rgb(0.32, 0.30, 0.58))
    static let bubbleGreen = adaptive(light: rgb(0.78, 0.95, 0.74), dark: rgb(0.20, 0.50, 0.34))
    static let bubbleWhite = adaptive(light: rgb(1, 1, 1), dark: rgb(0.15, 0.23, 0.33))

    static let pageGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.93, 0.98, 1.0), dark: rgb(0.02, 0.07, 0.11)),
            adaptive(light: rgb(0.86, 0.96, 0.98), dark: rgb(0.04, 0.15, 0.20)),
            adaptive(light: rgb(0.96, 0.98, 1.0), dark: rgb(0.01, 0.04, 0.08))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )

    static let actionGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.35, 0.72, 0.97), dark: rgb(0.08, 0.44, 0.70)),
            adaptive(light: rgb(0.50, 0.87, 0.89), dark: rgb(0.06, 0.62, 0.66))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )

    static let masteredGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.39, 0.78, 0.55), dark: rgb(0.11, 0.54, 0.36)),
            adaptive(light: rgb(0.68, 0.91, 0.76), dark: rgb(0.20, 0.75, 0.54))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )

    static let masteredActionGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.25, 0.66, 0.42), dark: rgb(0.09, 0.48, 0.33)),
            adaptive(light: rgb(0.54, 0.82, 0.63), dark: rgb(0.18, 0.70, 0.49))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )

    static let nextGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.78, 0.72, 0.94), dark: rgb(0.35, 0.29, 0.58)),
            adaptive(light: rgb(0.58, 0.53, 0.80), dark: rgb(0.50, 0.40, 0.76))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )

    static let removeGradient = LinearGradient(
        colors: [
            adaptive(light: rgb(0.90, 0.38, 0.35), dark: rgb(0.58, 0.14, 0.16)),
            adaptive(light: rgb(0.98, 0.58, 0.50), dark: rgb(0.86, 0.28, 0.28))
        ],
        startPoint: .topLeading,
        endPoint: .bottomTrailing
    )
}

private struct LiquidGlassCardModifier: ViewModifier {
    @Environment(\.colorScheme) private var colorScheme
    let cornerRadius: CGFloat
    let material: Material
    let fillOpacity: Double
    let strokeOpacity: Double
    let shadowOpacity: Double
    let shadowRadius: CGFloat
    let shadowY: CGFloat

    func body(content: Content) -> some View {
        let shape = RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
        let adjustedFillOpacity = colorScheme == .dark ? min(fillOpacity * 0.82, 0.38) : fillOpacity
        let adjustedStrokeOpacity = colorScheme == .dark ? min(strokeOpacity * 0.86, 0.34) : strokeOpacity
        let accentOpacity = colorScheme == .dark ? 0.22 : 0.13
        let baseShadowOpacity = colorScheme == .dark ? max(shadowOpacity * 0.58, 0.08) : shadowOpacity

        content
            .background {
                shape
                    .fill(KikariaTheme.glassSurface.opacity(adjustedFillOpacity))
            }
            .background(material, in: shape)
            .overlay {
                shape
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(adjustedStrokeOpacity),
                                Color.white.opacity(adjustedStrokeOpacity * 0.24),
                                KikariaTheme.glassStrokeAccent.opacity(accentOpacity)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 1
                    )
            }
            .overlay {
                shape
                    .stroke(Color.white.opacity(colorScheme == .dark ? 0.10 : 0.18), lineWidth: 0.5)
                    .blur(radius: 0.4)
                    .offset(y: 0.5)
                    .mask(shape)
            }
            .shadow(color: KikariaTheme.shadow.opacity(baseShadowOpacity), radius: shadowRadius, x: 0, y: shadowY)
            .shadow(color: Color.black.opacity(colorScheme == .dark ? 0.18 : 0.025), radius: shadowRadius * 0.55, x: 0, y: shadowY * 0.55)
    }
}

private struct LiquidGlassCapsuleModifier: ViewModifier {
    @Environment(\.colorScheme) private var colorScheme
    let material: Material
    let fillOpacity: Double
    let strokeOpacity: Double
    let shadowOpacity: Double
    let shadowRadius: CGFloat
    let shadowY: CGFloat

    func body(content: Content) -> some View {
        let adjustedFillOpacity = colorScheme == .dark ? min(fillOpacity * 0.82, 0.38) : fillOpacity
        let adjustedStrokeOpacity = colorScheme == .dark ? min(strokeOpacity * 0.86, 0.34) : strokeOpacity
        let accentOpacity = colorScheme == .dark ? 0.24 : 0.16

        content
            .background {
                Capsule()
                    .fill(KikariaTheme.glassSurface.opacity(adjustedFillOpacity))
            }
            .background(material, in: Capsule())
            .overlay {
                Capsule()
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(adjustedStrokeOpacity),
                                Color.white.opacity(adjustedStrokeOpacity * 0.24),
                                KikariaTheme.cyan.opacity(accentOpacity)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 1
                    )
            }
            .shadow(color: KikariaTheme.shadow.opacity(colorScheme == .dark ? max(shadowOpacity * 0.58, 0.08) : shadowOpacity), radius: shadowRadius, y: shadowY)
    }
}

private struct LiquidGlassCircleModifier: ViewModifier {
    @Environment(\.colorScheme) private var colorScheme
    let material: Material
    let fillOpacity: Double
    let strokeOpacity: Double
    let shadowOpacity: Double
    let shadowRadius: CGFloat
    let shadowY: CGFloat

    func body(content: Content) -> some View {
        let adjustedFillOpacity = colorScheme == .dark ? min(fillOpacity * 0.82, 0.38) : fillOpacity
        let adjustedStrokeOpacity = colorScheme == .dark ? min(strokeOpacity * 0.86, 0.34) : strokeOpacity
        let accentOpacity = colorScheme == .dark ? 0.24 : 0.12

        content
            .background {
                Circle()
                    .fill(KikariaTheme.glassSurface.opacity(adjustedFillOpacity))
            }
            .background(material, in: Circle())
            .overlay {
                Circle()
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(adjustedStrokeOpacity),
                                Color.white.opacity(adjustedStrokeOpacity * 0.22),
                                KikariaTheme.sky.opacity(accentOpacity)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 1
                    )
            }
            .shadow(color: KikariaTheme.shadow.opacity(colorScheme == .dark ? max(shadowOpacity * 0.58, 0.08) : shadowOpacity), radius: shadowRadius, y: shadowY)
    }
}

private extension View {
    func liquidGlassCard(
        cornerRadius: CGFloat = 28,
        material: Material = .ultraThinMaterial,
        fillOpacity: Double = 0.48,
        strokeOpacity: Double = 0.42,
        shadowOpacity: Double = 0.12,
        shadowRadius: CGFloat = 18,
        shadowY: CGFloat = 10
    ) -> some View {
        modifier(
            LiquidGlassCardModifier(
                cornerRadius: cornerRadius,
                material: material,
                fillOpacity: fillOpacity,
                strokeOpacity: strokeOpacity,
                shadowOpacity: shadowOpacity,
                shadowRadius: shadowRadius,
                shadowY: shadowY
            )
        )
    }

    func liquidGlassCapsule(
        material: Material = .ultraThinMaterial,
        fillOpacity: Double = 0.48,
        strokeOpacity: Double = 0.42,
        shadowOpacity: Double = 0.10,
        shadowRadius: CGFloat = 14,
        shadowY: CGFloat = 7
    ) -> some View {
        modifier(
            LiquidGlassCapsuleModifier(
                material: material,
                fillOpacity: fillOpacity,
                strokeOpacity: strokeOpacity,
                shadowOpacity: shadowOpacity,
                shadowRadius: shadowRadius,
                shadowY: shadowY
            )
        )
    }

    func liquidGlassCircle(
        material: Material = .ultraThinMaterial,
        fillOpacity: Double = 0.44,
        strokeOpacity: Double = 0.42,
        shadowOpacity: Double = 0.14,
        shadowRadius: CGFloat = 14,
        shadowY: CGFloat = 7
    ) -> some View {
        modifier(
            LiquidGlassCircleModifier(
                material: material,
                fillOpacity: fillOpacity,
                strokeOpacity: strokeOpacity,
                shadowOpacity: shadowOpacity,
                shadowRadius: shadowRadius,
                shadowY: shadowY
            )
        )
    }

    @ViewBuilder
    func highPriorityGestureIf<GestureType: Gesture>(_ isActive: Bool, _ gesture: GestureType) -> some View {
        if isActive {
            highPriorityGesture(gesture)
        } else {
            self
        }
    }

    @ViewBuilder
    func simultaneousGestureIf<GestureType: Gesture>(_ isActive: Bool, _ gesture: GestureType) -> some View {
        if isActive {
            simultaneousGesture(gesture)
        } else {
            self
        }
    }
}

private struct LegacyPresentedRoute: Identifiable, Equatable {
    let id = UUID()
    let route: AppRoute
}

private enum AppRoute: Hashable {
    case scope
    case review
    case todayOverview
    case reviewHistory
    case reinforcement
    case reinforcementReview
    case mastered
    case masteredReview
    case settings
    case editProfile
    case markdownEditor
    case presetSelection
    case newPreset
    case markdownFormatGuide
    case editPreset(String)
    case editKnowledgePoint(String, UUID?)
}

enum ReviewMode {
    case normal
    case reinforcement
    case mastered

    var isNormal: Bool {
        if case .normal = self {
            return true
        }

        return false
    }

    var isReinforcement: Bool {
        if case .reinforcement = self {
            return true
        }

        return false
    }

    var isMastered: Bool {
        if case .mastered = self {
            return true
        }

        return false
    }
}

private struct UserProfile: Codable, Equatable {
    var displayName = "Vita"
    var userHandle = "vita_0818"
    var avatarSystemName = "person.crop.circle.fill"
    var avatarImageData: Data?
}

#if os(macOS)
private enum MacSidebarDestination: CaseIterable, Identifiable {
    case dashboard
    case todayOverview
    case reinforcement
    case mastered
    case presetSelection

    var id: Self {
        self
    }

    static var allCases: [MacSidebarDestination] {
        [.dashboard, .todayOverview, .reinforcement, .mastered, .presetSelection]
    }

    var title: String {
        switch self {
        case .dashboard:
            "仪表盘"
        case .todayOverview:
            "今日概览"
        case .reinforcement:
            "重点集锦"
        case .mastered:
            "已掌握"
        case .presetSelection:
            "预设管理"
        }
    }

    var systemImage: String {
        switch self {
        case .dashboard:
            "rectangle.3.group"
        case .todayOverview:
            "calendar"
        case .reinforcement:
            "sparkles"
        case .mastered:
            "checkmark.seal"
        case .presetSelection:
            "slider.horizontal.3"
        }
    }

    init?(route: AppRoute) {
        switch route {
        case .scope, .review:
            self = .dashboard
        case .todayOverview, .reviewHistory:
            self = .todayOverview
        case .reinforcement, .reinforcementReview:
            self = .reinforcement
        case .mastered, .masteredReview:
            self = .mastered
        case .presetSelection, .newPreset, .editPreset, .editKnowledgePoint, .markdownEditor:
            self = .presetSelection
        default:
            return nil
        }
    }
}

private struct MacSidebarView: View {
    @Binding var selection: MacSidebarDestination?
    @Binding var isSettingsSelected: Bool
    let profile: UserProfile
    @Environment(\.colorScheme) private var colorScheme

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            VStack(alignment: .leading, spacing: 4) {
                Text("Kikaria")
                    .font(KikariaTypography.appTitle(size: 30))
                    .foregroundStyle(KikariaTheme.deepText)
                    .lineLimit(1)

                Text("Mac")
                    .font(KikariaTypography.chineseCaption(size: 12, weight: .semibold))
                    .foregroundStyle(KikariaTheme.softText)
                    .lineLimit(1)
            }
            .padding(.horizontal, 20)
            .padding(.top, 24)
            .padding(.bottom, 18)

            VStack(spacing: 6) {
                ForEach(MacSidebarDestination.allCases) { destination in
                    Button {
                        selection = destination
                        isSettingsSelected = false
                    } label: {
                        MacSidebarItemButton(
                            destination: destination,
                            isSelected: !isSettingsSelected && selection == destination
                        )
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 4)

            Spacer(minLength: 12)

            Button {
                selection = nil
                isSettingsSelected = true
            } label: {
                HStack(spacing: 10) {
                    ProfileAvatarView(
                        systemName: profile.avatarSystemName,
                        imageData: profile.avatarImageData,
                        size: 30
                    )

                    VStack(alignment: .leading, spacing: 2) {
                        KikariaTypography.mixedText(profile.displayName, size: 13, weight: .semibold)
                            .foregroundStyle(KikariaTheme.deepText)
                            .lineLimit(1)

                        Text("@\(profile.userHandle)")
                            .font(KikariaTypography.chineseCaption(size: 10, weight: .medium))
                            .foregroundStyle(KikariaTheme.softText)
                            .lineLimit(1)
                    }

                    Spacer(minLength: 8)
                }
                .padding(.horizontal, 12)
                .padding(.vertical, 10)
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
            .accessibilityLabel("打开设置")
            .padding(.horizontal, 14)
            .padding(.bottom, 14)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
        .background {
            Rectangle()
                .fill(KikariaTheme.glassSurface.opacity(colorScheme == .dark ? 0.22 : 0.30))
                .background(.thinMaterial)
        }
    }
}

private struct MacSidebarItemButton: View {
    let destination: MacSidebarDestination
    let isSelected: Bool
    @Environment(\.colorScheme) private var colorScheme
    @State private var isHovering = false

    var body: some View {
        HStack(spacing: 8) {
            Image(systemName: destination.systemImage)
                .font(.system(size: 13, weight: .medium))
                .foregroundStyle(isSelected ? KikariaTheme.sky : KikariaTheme.softText)
                .frame(width: 20, alignment: .center)

            Text(destination.title)
                .font(KikariaTypography.chineseBody(size: 13, weight: isSelected ? .semibold : .medium))
                .foregroundStyle(isSelected ? KikariaTheme.deepText : KikariaTheme.softText)

            Spacer(minLength: 0)
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 10)
        .contentShape(RoundedRectangle(cornerRadius: 15, style: .continuous))
        .background {
            RoundedRectangle(cornerRadius: 15, style: .continuous)
                .fill(backgroundFill)
                .background(isSelected ? .ultraThinMaterial : .regularMaterial, in: RoundedRectangle(cornerRadius: 15, style: .continuous))
                .opacity(isSelected || isHovering ? 1 : 0)
        }
        .overlay {
            RoundedRectangle(cornerRadius: 15, style: .continuous)
                .stroke(borderColor, lineWidth: 1)
                .opacity(isSelected ? 1 : (isHovering ? 0.45 : 0))
        }
        .shadow(color: KikariaTheme.sky.opacity(isSelected && colorScheme == .light ? 0.12 : 0), radius: 10, x: 0, y: 5)
        .onHover { isHovering = $0 }
        .accessibilityLabel(destination.title)
    }

    private var backgroundFill: AnyShapeStyle {
        if isSelected {
            return AnyShapeStyle(
                LinearGradient(
                    colors: [
                        KikariaTheme.sky.opacity(colorScheme == .dark ? 0.30 : 0.42),
                        KikariaTheme.cyan.opacity(colorScheme == .dark ? 0.20 : 0.26)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
        }

        return AnyShapeStyle(KikariaTheme.glassSurface.opacity(colorScheme == .dark ? 0.18 : 0.32))
    }

    private var borderColor: Color {
        isSelected
            ? KikariaTheme.glassStrokeAccent.opacity(colorScheme == .dark ? 0.38 : 0.52)
            : KikariaTheme.glassStrokeAccent.opacity(0.35)
    }
}
#endif

struct DailyReviewRecord: Codable, Equatable {
    var date: Date
    var count: Int
}

private func studyProgressNotificationBody(for presetName: String) -> String {
    "今天的「\(presetName)」学习量尚未达标哦，抓紧学习吧！"
}

private let retiredBuiltInPresetIDs: Set<String> = [
    "advanced-math",
    "college-english",
    "college-physics",
    "anatomy",
    "template",
    "builtin-university-physics",
    "builtin-college-english-band4",
    "builtin-calculus",
    "builtin-discrete-math"
]

private func isRetiredBuiltInPreset(_ preset: KnowledgePreset) -> Bool {
    preset.isBuiltIn && retiredBuiltInPresetIDs.contains(preset.id)
}

private struct PresetStudyState: Codable {
    let presetId: String
    var knowledgePoints: [KnowledgePoint]
    var markdownText: String
    var selectedTags: Set<String>
    var dailyReviewRecords: [KnowledgePoint.ID: DailyReviewRecord]
    var activityRecords: [StudyActivityRecord]
    var dailyGoal: Int
    var countdownStartDate: Date?
    var countdownEndDate: Date?
    var notificationsEnabled: Bool
    var notificationTime: Date
    var dangerPercent: Int

    init(
        presetId: String,
        knowledgePoints: [KnowledgePoint],
        markdownText: String,
        selectedTags: Set<String>,
        dailyReviewRecords: [KnowledgePoint.ID: DailyReviewRecord],
        activityRecords: [StudyActivityRecord] = [],
        dailyGoal: Int,
        countdownStartDate: Date? = nil,
        countdownEndDate: Date? = nil,
        notificationsEnabled: Bool = false,
        notificationTime: Date = PresetStudyState.defaultNotificationTime(),
        dangerPercent: Int = 80
    ) {
        self.presetId = presetId
        self.knowledgePoints = knowledgePoints
        self.markdownText = markdownText
        self.selectedTags = selectedTags
        self.dailyReviewRecords = dailyReviewRecords
        self.activityRecords = activityRecords
        self.dailyGoal = dailyGoal
        self.countdownStartDate = countdownStartDate
        self.countdownEndDate = countdownEndDate
        self.notificationsEnabled = notificationsEnabled
        self.notificationTime = notificationTime
        self.dangerPercent = min(max(dangerPercent, 1), 100)
    }

    private enum CodingKeys: String, CodingKey {
        case presetId
        case knowledgePoints
        case markdownText
        case selectedTags
        case dailyReviewRecords
        case activityRecords
        case dailyGoal
        case countdownDate
        case countdownStartDate
        case countdownEndDate
        case notificationsEnabled
        case notificationTime
        case dangerPercent
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        presetId = try container.decode(String.self, forKey: .presetId)
        knowledgePoints = try container.decode([KnowledgePoint].self, forKey: .knowledgePoints)
        markdownText = try container.decode(String.self, forKey: .markdownText)
        selectedTags = try container.decodeIfPresent(Set<String>.self, forKey: .selectedTags) ?? []
        dailyReviewRecords = try container.decodeIfPresent([KnowledgePoint.ID: DailyReviewRecord].self, forKey: .dailyReviewRecords) ?? [:]
        activityRecords = try container.decodeIfPresent([StudyActivityRecord].self, forKey: .activityRecords) ?? []
        dailyGoal = try container.decodeIfPresent(Int.self, forKey: .dailyGoal) ?? 20

        let legacyCountdownDate = try container.decodeIfPresent(Date.self, forKey: .countdownDate)
        countdownStartDate = try container.decodeIfPresent(Date.self, forKey: .countdownStartDate)
        countdownEndDate = try container.decodeIfPresent(Date.self, forKey: .countdownEndDate) ?? legacyCountdownDate

        notificationsEnabled = try container.decodeIfPresent(Bool.self, forKey: .notificationsEnabled) ?? false
        notificationTime = try container.decodeIfPresent(Date.self, forKey: .notificationTime) ?? PresetStudyState.defaultNotificationTime()
        dangerPercent = min(max(try container.decodeIfPresent(Int.self, forKey: .dangerPercent) ?? 80, 1), 100)
    }

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(presetId, forKey: .presetId)
        try container.encode(knowledgePoints, forKey: .knowledgePoints)
        try container.encode(markdownText, forKey: .markdownText)
        try container.encode(selectedTags, forKey: .selectedTags)
        try container.encode(dailyReviewRecords, forKey: .dailyReviewRecords)
        try container.encode(activityRecords, forKey: .activityRecords)
        try container.encode(dailyGoal, forKey: .dailyGoal)
        try container.encodeIfPresent(countdownStartDate, forKey: .countdownStartDate)
        try container.encodeIfPresent(countdownEndDate, forKey: .countdownEndDate)
        try container.encode(notificationsEnabled, forKey: .notificationsEnabled)
        try container.encode(notificationTime, forKey: .notificationTime)
        try container.encode(dangerPercent, forKey: .dangerPercent)
    }

    static func defaultNotificationTime() -> Date {
        var components = Calendar.current.dateComponents([.year, .month, .day], from: Date())
        components.hour = 21
        components.minute = 0
        components.second = 0
        return Calendar.current.date(from: components) ?? Date()
    }
}

private struct PresetLibrarySnapshot: Codable {
    var presets: [KnowledgePreset]
    var presetStates: [String: PresetStudyState]
    var currentPresetID: String
}

private struct KikariaAppState: Codable {
    static let storageKey = "kikaria.appStateJSON"
    static let currentSchemaVersion = KnowledgePreset.builtInSeedVersion

    var schemaVersion: Int
    var presets: [KnowledgePreset]
    var presetStates: [String: PresetStudyState]
    var currentPresetID: String
    var userProfile: UserProfile
    var hasCompletedProfileSetup: Bool
    var hasCompletedOnboarding: Bool

    init(
        schemaVersion: Int = KikariaAppState.currentSchemaVersion,
        presets: [KnowledgePreset],
        presetStates: [String: PresetStudyState],
        currentPresetID: String,
        userProfile: UserProfile,
        hasCompletedProfileSetup: Bool,
        hasCompletedOnboarding: Bool
    ) {
        self.schemaVersion = schemaVersion
        self.presets = presets
        self.presetStates = presetStates
        self.currentPresetID = currentPresetID
        self.userProfile = userProfile
        self.hasCompletedProfileSetup = hasCompletedProfileSetup
        self.hasCompletedOnboarding = hasCompletedOnboarding
    }

    private enum CodingKeys: String, CodingKey {
        case schemaVersion
        case presets
        case presetStates
        case currentPresetID
        case userProfile
        case hasCompletedProfileSetup
        case hasCompletedOnboarding
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        schemaVersion = try container.decodeIfPresent(Int.self, forKey: .schemaVersion) ?? 0
        presets = try container.decodeIfPresent([KnowledgePreset].self, forKey: .presets) ?? KnowledgePreset.all
        presetStates = try container.decodeIfPresent([String: PresetStudyState].self, forKey: .presetStates) ?? [:]
        currentPresetID = try container.decodeIfPresent(String.self, forKey: .currentPresetID) ?? KnowledgePreset.defaultPresetID
        let decodedProfile = try container.decodeIfPresent(UserProfile.self, forKey: .userProfile) ?? UserProfile()
        userProfile = decodedProfile
        hasCompletedProfileSetup = try container.decodeIfPresent(Bool.self, forKey: .hasCompletedProfileSetup) ?? (decodedProfile != UserProfile())
        hasCompletedOnboarding = try container.decodeIfPresent(Bool.self, forKey: .hasCompletedOnboarding) ?? false
    }
}

private enum PresetDeleteOutcome {
    case deleted(String)
    case blockedLastPreset
    case notFound
}

private enum PresetCreationOutcome {
    case success(KnowledgePreset)
    case failure(String)
}

private func countdownDays(until targetDate: Date?) -> Int? {
    guard let targetDate else {
        return nil
    }

    let calendar = Calendar.current
    let today = calendar.startOfDay(for: Date())
    let target = calendar.startOfDay(for: targetDate)
    let dayCount = calendar.dateComponents([.day], from: today, to: target).day ?? 0
    return max(0, dayCount)
}

private func countdownText(for targetDate: Date?) -> String {
    guard let days = countdownDays(until: targetDate) else {
        return "--"
    }

    return "\(days) 天"
}

private struct StudyProgressWarning {
    let masteredCount: Int
    let expectedMasteredCount: Int
    let dangerPercent: Int
    let remainingDays: Int?

    func body(for presetName: String) -> String {
        studyProgressNotificationBody(for: presetName)
    }
}

final class KikariaNotificationDelegate: NSObject, UNUserNotificationCenterDelegate {
    static let shared = KikariaNotificationDelegate()

    private override init() {
        super.init()
    }

    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        completionHandler([.banner, .sound, .list])
    }
}

private enum KikariaNotificationManager {
    static func identifier(for presetID: String) -> String {
        "kikaria.studyProgressWarning.\(presetID)"
    }

    static func requestAuthorization(completion: @escaping (Bool) -> Void) {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound, .badge]) { granted, _ in
            DispatchQueue.main.async {
                completion(granted)
            }
        }
    }

    static func cancelStudyProgressWarning(for presetID: String) {
        let identifier = identifier(for: presetID)
        UNUserNotificationCenter.current().removePendingNotificationRequests(withIdentifiers: [identifier])
    }

    static func cancelAllKikariaStudyNotifications() {
        UNUserNotificationCenter.current().getPendingNotificationRequests { requests in
            let identifiers = requests
                .map(\.identifier)
                .filter { $0.hasPrefix("kikaria.studyProgressWarning.") }
            UNUserNotificationCenter.current().removePendingNotificationRequests(withIdentifiers: identifiers)
        }
    }

    static func rescheduleAllStudyProgressWarnings(
        for states: [String: PresetStudyState],
        presetNames: [String: String]
    ) {
        for state in states.values {
            rescheduleStudyProgressWarning(
                for: state,
                presetName: presetNames[state.presetId] ?? "当前预设"
            )
        }
    }

    static func rescheduleStudyProgressWarning(for state: PresetStudyState, presetName: String) {
        let center = UNUserNotificationCenter.current()
        let identifier = identifier(for: state.presetId)
        center.removePendingNotificationRequests(withIdentifiers: [identifier])

        guard state.notificationsEnabled else {
            return
        }

        center.getNotificationSettings { settings in
            let isAuthorized = settings.authorizationStatus == .authorized ||
                settings.authorizationStatus == .provisional
            #if os(iOS)
            let canSchedule = isAuthorized || settings.authorizationStatus == .ephemeral
            #else
            let canSchedule = isAuthorized
            #endif

            guard canSchedule else {
                return
            }

            guard let warning = evaluateStudyProgressWarning(for: state) else {
                return
            }

            let content = UNMutableNotificationContent()
            content.title = "Kikaria"
            content.body = warning.body(for: presetName)
            content.sound = .default

            let triggerDate = nextTriggerDate(for: state.notificationTime)
            let components = Calendar.current.dateComponents([.year, .month, .day, .hour, .minute], from: triggerDate)
            let trigger = UNCalendarNotificationTrigger(dateMatching: components, repeats: false)
            let request = UNNotificationRequest(identifier: identifier, content: content, trigger: trigger)
            center.add(request)
        }
    }

    static func scheduleDebugTestNotification(presetName: String, completion: @escaping (String) -> Void) {
        #if DEBUG
        let center = UNUserNotificationCenter.current()
        center.getNotificationSettings { settings in
            let isAuthorized = settings.authorizationStatus == .authorized ||
                settings.authorizationStatus == .provisional
            #if os(iOS)
            let canSchedule = isAuthorized || settings.authorizationStatus == .ephemeral
            #else
            let canSchedule = isAuthorized
            #endif

            if canSchedule {
                scheduleAuthorizedDebugTestNotification(presetName: presetName, completion: completion)
            } else if settings.authorizationStatus == .notDetermined {
                requestAuthorization { granted in
                    if granted {
                        scheduleAuthorizedDebugTestNotification(presetName: presetName, completion: completion)
                    } else {
                        completion("请在系统设置中允许通知")
                    }
                }
            } else if settings.authorizationStatus == .denied {
                DispatchQueue.main.async {
                    completion("请在系统设置中允许通知")
                }
            } else {
                DispatchQueue.main.async {
                    completion("通知权限不可用")
                }
            }
        }
        #endif
    }

    private static func scheduleAuthorizedDebugTestNotification(presetName: String, completion: @escaping (String) -> Void) {
        #if DEBUG
        let center = UNUserNotificationCenter.current()
        let identifier = "kikaria.test.notification"
        center.removePendingNotificationRequests(withIdentifiers: [identifier])

        let content = UNMutableNotificationContent()
        content.title = "Kikaria"
        content.body = studyProgressNotificationBody(for: presetName)
        content.sound = .default
        let trigger = UNTimeIntervalNotificationTrigger(timeInterval: 5, repeats: false)
        let request = UNNotificationRequest(
            identifier: identifier,
            content: content,
            trigger: trigger
        )
        center.add(request) { error in
            DispatchQueue.main.async {
                if error == nil {
                    completion("提醒将在 5 秒后发送")
                } else {
                    completion("提醒发送失败")
                }
            }
        }
        #endif
    }

    static func evaluateStudyProgressWarning(for state: PresetStudyState, now: Date = Date()) -> StudyProgressWarning? {
        let totalCount = state.knowledgePoints.count
        let masteredCount = state.knowledgePoints.filter(\.isMastered).count
        let dangerPercent = min(max(state.dangerPercent, 1), 100)

        guard totalCount > 0,
              let startDate = state.countdownStartDate,
              let endDate = state.countdownEndDate
        else {
            return nil
        }

        let calendar = Calendar.current
        let today = calendar.startOfDay(for: now)
        let start = calendar.startOfDay(for: startDate)
        let end = calendar.startOfDay(for: endDate)

        guard start <= end else {
            return nil
        }

        if today < start {
            return nil
        }

        let expectedProgress: Double
        if today >= end {
            expectedProgress = 1
        } else {
            let totalDays = max(1, (calendar.dateComponents([.day], from: start, to: end).day ?? 0) + 1)
            let elapsedDays = max(1, (calendar.dateComponents([.day], from: start, to: today).day ?? 0) + 1)
            expectedProgress = Double(elapsedDays) / Double(totalDays)
        }

        let expectedMasteredCount = Int(ceil(Double(totalCount) * expectedProgress))
        guard expectedMasteredCount > 0 else {
            return nil
        }

        let actualProgressRatio = Double(masteredCount) / Double(expectedMasteredCount)
        guard actualProgressRatio < Double(dangerPercent) / 100 else {
            return nil
        }

        return StudyProgressWarning(
            masteredCount: masteredCount,
            expectedMasteredCount: expectedMasteredCount,
            dangerPercent: dangerPercent,
            remainingDays: countdownDays(until: endDate)
        )
    }

    private static func nextTriggerDate(for notificationTime: Date, now: Date = Date()) -> Date {
        let calendar = Calendar.current
        let timeComponents = calendar.dateComponents([.hour, .minute], from: notificationTime)
        let hour = timeComponents.hour ?? 21
        let minute = timeComponents.minute ?? 0
        let today = calendar.startOfDay(for: now)
        let todayTrigger = calendar.date(bySettingHour: hour, minute: minute, second: 0, of: today) ?? now

        if todayTrigger > now {
            return todayTrigger
        }

        let tomorrow = calendar.date(byAdding: .day, value: 1, to: today) ?? now.addingTimeInterval(24 * 60 * 60)
        return calendar.date(bySettingHour: hour, minute: minute, second: 0, of: tomorrow) ?? tomorrow
    }
}

struct ContentView: View {
    @Environment(\.scenePhase) private var scenePhase
    @State private var presets = KnowledgePreset.all
    @State private var knowledgePoints = KnowledgePoint.samples
    @State private var markdownText = KnowledgePreset.defaultPreset.markdownText
    @State private var userProfile = UserProfile()
    @State private var selectedTags = Set<String>()
    @State private var navigationPath: [AppRoute] = []
    #if os(iOS)
    @State private var legacyNavigationStack: [LegacyPresentedRoute] = []
    #endif
    @State private var dailyReviewRecords: [KnowledgePoint.ID: DailyReviewRecord] = [:]
    @State private var activityRecords: [StudyActivityRecord] = []
    @State private var presetStates: [String: PresetStudyState] = [:]
    @State private var currentPresetID = KnowledgePreset.defaultPresetID
    @State private var dailyGoal = 20
    @State private var countdownStartDate: Date?
    @State private var countdownEndDate: Date?
    @State private var notificationsEnabled = false
    @State private var notificationTime = PresetStudyState.defaultNotificationTime()
    @State private var dangerPercent = 80
    @State private var hasLoadedInitialPresetState = false
    @State private var isApplyingPresetState = false
    @State private var pendingStudyStatePersistenceWorkItem: DispatchWorkItem?
    @State private var pendingStudyStatePersistenceRefreshesWidget = false
    @State private var hasCompletedProfileSetup = false
    @State private var isShowingProfileSetup = false
    @State private var hasCompletedOnboarding = false
    @State private var isShowingOnboarding = false

    private var allTags: [String] {
        Array(Set(knowledgePoints.flatMap(\.tags))).sorted()
    }

    private var selectedScopeCountText: String {
        selectedTags.isEmpty ? "\(allTags.count)" : "\(selectedTags.count)"
    }

    private var reinforcedCount: Int {
        knowledgePoints.filter { $0.reinforcementCount > 0 }.count
    }

    private var masteredCount: Int {
        knowledgePoints.filter(\.isMastered).count
    }

    private var countdownDayCount: Int? {
        countdownDays(until: countdownEndDate)
    }

    private var currentPreset: KnowledgePreset {
        presets.first { $0.id == currentPresetID } ?? KnowledgePreset.defaultPreset
    }

    private var currentPresetActivityRecords: [StudyActivityRecord] {
        activityRecords.filter { $0.presetId == currentPresetID }
    }

    private var todayReviewedAnswerCount: Int {
        records(on: Date(), type: .reviewedAnswer).count
    }

    private var todayViewedHintCount: Int {
        records(on: Date(), type: .viewedHint).count
    }

    private var todayMarkedMasteredCount: Int {
        Set(records(on: Date(), type: .markedMastered).map(\.pointId)).count
    }

    private var homeDateTitle: String {
        let date = Date()
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "MMM"

        let day = Calendar.current.component(.day, from: date)
        return "\(formatter.string(from: date)) \(day)\(ordinalSuffix(for: day))"
    }

    private var homeDaysLeftText: String {
        "\(countdownDayCount.map(String.init) ?? "--") Days Left"
    }

    private var homeProgressText: String {
        "\(todayMarkedMasteredCount)/\(dailyGoal)"
    }

    private func padPortraitHomeContent(metrics: KikariaAdaptiveLayout.Metrics) -> some View {
        let isLargePortrait = metrics.width >= 900
        let bubbleScale = min(metrics.homeScale, 1.32)
        let topPadding: CGFloat = isLargePortrait ? 58 : 48
        let bubbleSafeSpacing: CGFloat = isLargePortrait ? 36 : 30
        let contentWidth = min(metrics.width, metrics.homeMaxWidth)
        let cardEdgeInset = max(
            metrics.horizontalPadding,
            (metrics.width - contentWidth) / 2 + metrics.horizontalPadding
        )

        return VStack(spacing: 0) {
            HStack(alignment: .center) {
                Text("Kikaria")
                    .font(KikariaTypography.appTitle(size: isLargePortrait ? 58 : 54))
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer(minLength: 24)

                #if os(iOS)
                routeLink(to: .settings) {
                    ProfileAvatarView(
                        systemName: userProfile.avatarSystemName,
                        imageData: userProfile.avatarImageData,
                        size: isLargePortrait ? 66 : 62
                    )
                }
                .buttonStyle(.plain)
                .accessibilityLabel("打开设置")
                #endif
            }

            VStack(spacing: 0) {
                Spacer(minLength: bubbleSafeSpacing)

                routeLink(to: .review) {
                    StartReviewButton(
                        dailyGoal: dailyGoal,
                        masteredCount: masteredCount,
                        countdownDays: countdownDayCount,
                        visualScale: bubbleScale
                    )
                }
                .buttonStyle(.plain)
                .accessibilityLabel("开始背诵")

                Spacer(minLength: bubbleSafeSpacing)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)

            VStack(spacing: 18) {
                routeLink(to: .todayOverview) {
                    PadPortraitHomeProgressCard(
                        dateText: homeDateTitle,
                        daysLeftText: homeDaysLeftText,
                        progressText: homeProgressText
                    )
                }
                .buttonStyle(.plain)

                PadPortraitHomeDashboardCard(
                    scopeCountText: selectedScopeCountText,
                    reinforcedCount: reinforcedCount,
                    masteredCount: masteredCount,
                    presetName: currentPreset.name
                )
            }
        }
        .padding(.top, topPadding)
        .padding(.horizontal, metrics.horizontalPadding)
        .padding(.bottom, cardEdgeInset)
        .frame(maxWidth: metrics.homeMaxWidth)
        .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .top)
    }

    private func homeLandscapeContent(metrics: KikariaAdaptiveLayout.Metrics) -> some View {
        let leftWidth = metrics.homeLandscapeLeftWidth
        let rightWidth = metrics.homeLandscapeRightWidth
        let cardScale = metrics.homeLandscapeCardScale
        let columnHeight = min(max(metrics.height - 112, 460), 640)

        return ZStack(alignment: .topTrailing) {
            HStack(alignment: .center, spacing: metrics.homeLandscapeColumnSpacing) {
                VStack(spacing: 0) {
                    Text("Kikaria")
                        .font(KikariaTypography.appTitle(size: 39 * metrics.homeHeaderScale))
                        .foregroundStyle(KikariaTheme.deepText)
                        .frame(maxWidth: .infinity, alignment: .leading)

                    Spacer(minLength: 34)

                    routeLink(to: .review) {
                        StartReviewButton(
                            dailyGoal: dailyGoal,
                            masteredCount: masteredCount,
                            countdownDays: countdownDayCount,
                            visualScale: metrics.homeLandscapeBubbleScale
                        )
                    }
                    .buttonStyle(.plain)
                    .accessibilityLabel("开始背诵")

                    Spacer(minLength: 34)
                }
                .frame(width: leftWidth)
                .frame(minHeight: columnHeight, alignment: .center)

                VStack(spacing: 14 * cardScale) {
                    routeLink(to: .todayOverview) {
                        TodayOverviewHomeProgressButton(
                            dateText: homeDateTitle,
                            daysLeftText: homeDaysLeftText,
                            progressText: homeProgressText,
                            isExpanded: true,
                            cardScale: cardScale
                        )
                    }
                    .buttonStyle(.plain)

                    HomeDashboardGridCard(
                        scopeCountText: selectedScopeCountText,
                        reinforcedCount: reinforcedCount,
                        masteredCount: masteredCount,
                        presetName: currentPreset.name,
                        isExpanded: true,
                        cardScale: cardScale
                    )
                }
                .frame(width: rightWidth)
            }
            .frame(maxWidth: metrics.homeLandscapeMaxWidth)

            #if os(iOS)
            routeLink(to: .settings) {
                ProfileAvatarView(
                    systemName: userProfile.avatarSystemName,
                    imageData: userProfile.avatarImageData,
                    size: 48 * metrics.homeLandscapeCardScale
                )
            }
            .buttonStyle(.plain)
            .accessibilityLabel("打开设置")
            .padding(.top, 26)
            #endif
        }
        .padding(.horizontal, metrics.horizontalPadding)
        .padding(.vertical, 36)
        .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)
    }

    var body: some View {
        platformNavigationRoot
            .onAppear {
                loadInitialPresetStateIfNeeded()
                if !hasCompletedProfileSetup {
                    isShowingProfileSetup = true
                } else if !hasCompletedOnboarding {
                    isShowingOnboarding = true
                }
            }
            .onChange(of: knowledgePoints) { _ in
                persistCurrentStudyStateIfReady(refreshWidget: true)
            }
            .onChange(of: selectedTags) { _ in
                persistCurrentStudyStateIfReady()
            }
            .onChange(of: dailyReviewRecords) { _ in
                persistCurrentStudyStateIfReady(refreshWidget: true)
            }
            .onChange(of: activityRecords) { _ in
                persistCurrentStudyStateIfReady(refreshWidget: true)
            }
            .onChange(of: userProfile) { _ in
                saveAppStateIfReady()
            }
            .onChange(of: hasCompletedProfileSetup) { _ in
                saveAppStateIfReady()
            }
            .onChange(of: hasCompletedOnboarding) { _ in
                saveAppStateIfReady()
            }
            .onChange(of: markdownText) { _ in
                persistCurrentStudyStateIfReady()
            }
            .onChange(of: scenePhase) { phase in
                if phase == .active {
                    rescheduleAllPresetNotifications()
                } else if phase == .inactive || phase == .background {
                    saveAppStateIfReady()
                }
            }
            #if os(iOS)
            .fullScreenCover(isPresented: $isShowingOnboarding) {
                OnboardingView {
                    hasCompletedOnboarding = true
                    isShowingOnboarding = false
                    saveAppStateIfReady()
                }
                .interactiveDismissDisabled(!hasCompletedOnboarding)
            }
            .fullScreenCover(isPresented: $isShowingProfileSetup) {
                InitialProfileSetupView(profile: $userProfile) {
                    hasCompletedProfileSetup = true
                    isShowingProfileSetup = false
                    saveAppStateIfReady()

                    if !hasCompletedOnboarding {
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.35) {
                            isShowingOnboarding = true
                        }
                    }
                }
                .interactiveDismissDisabled(true)
            }
            #elseif os(macOS)
            .kikariaMacFirstLaunchOverlay(isPresented: isShowingOnboarding) {
                OnboardingView {
                    hasCompletedOnboarding = true
                    isShowingOnboarding = false
                    saveAppStateIfReady()
                }
            }
            .kikariaMacFirstLaunchOverlay(isPresented: isShowingProfileSetup) {
                InitialProfileSetupView(profile: $userProfile) {
                    hasCompletedProfileSetup = true
                    isShowingProfileSetup = false
                    saveAppStateIfReady()

                    if !hasCompletedOnboarding {
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.20) {
                            isShowingOnboarding = true
                        }
                    }
                }
            }
            #endif
    }

    private func pushRoute(_ route: AppRoute) {
        #if os(iOS)
        if #available(iOS 16, *) {
            navigationPath.append(route)
        } else {
            legacyNavigationStack.append(LegacyPresentedRoute(route: route))
        }
        #else
        navigationPath.append(route)
        #endif
    }

    private func popRoute() {
        #if os(iOS)
        if #available(iOS 16, *) {
            guard !navigationPath.isEmpty else {
                return
            }

            navigationPath.removeLast()
            return
        }

        guard !legacyNavigationStack.isEmpty else {
            return
        }

        legacyNavigationStack.removeLast()
        #else
        guard !navigationPath.isEmpty else {
            return
        }

        navigationPath.removeLast()
        #endif
    }

    private func resetRouteStack() {
        #if os(iOS)
        if #available(iOS 16, *) {
            navigationPath.removeAll()
        } else {
            legacyNavigationStack.removeAll()
        }
        #else
        navigationPath.removeAll()
        #endif
    }

    @ViewBuilder
    private func routeLink<Label: View>(
        to route: AppRoute,
        @ViewBuilder _ label: () -> Label
    ) -> some View {
        #if os(iOS)
        if #available(iOS 16, *) {
            NavigationLink(value: route, label: label)
        } else {
            Button(action: { pushRoute(route) }) {
                label()
            }
        }
        #else
        NavigationLink(value: route, label: label)
        #endif
    }

    @ViewBuilder
    private func routeDestination(for route: AppRoute) -> some View {
        switch route {
        case .scope:
            ScopeSelectionView(
                selectedTags: $selectedTags,
                knowledgePoints: knowledgePoints,
                allTags: allTags
            )
        case .review:
            ReviewView(
                knowledgePoints: $knowledgePoints,
                selectedTags: $selectedTags,
                dailyReviewRecords: $dailyReviewRecords,
                mode: .normal,
                onRecordActivity: recordStudyActivity
            )
        case .todayOverview:
            TodayOverviewView(
                presetName: currentPreset.name,
                activityRecords: currentPresetActivityRecords,
                knowledgePoints: knowledgePoints,
                dailyGoal: dailyGoal,
                countdownEndDate: countdownEndDate,
                onOpenHistory: {
                    pushRoute(.reviewHistory)
                }
            )
        case .reviewHistory:
            ReviewHistoryView(
                activityRecords: currentPresetActivityRecords
            )
        case .reinforcement:
            ReinforcementView(
                knowledgePoints: $knowledgePoints,
                onRecordActivity: recordStudyActivity,
                onStartReview: {
                    pushRoute(.reinforcementReview)
                }
            )
        case .reinforcementReview:
            ReviewView(
                knowledgePoints: $knowledgePoints,
                selectedTags: .constant([]),
                dailyReviewRecords: $dailyReviewRecords,
                mode: .reinforcement,
                onRecordActivity: recordStudyActivity,
                onReturnHome: {
                    resetRouteStack()
                }
            )
        case .mastered:
            MasteredView(
                knowledgePoints: $knowledgePoints,
                onRecordActivity: recordStudyActivity,
                onStartReview: {
                    pushRoute(.masteredReview)
                }
            )
        case .masteredReview:
            ReviewView(
                knowledgePoints: $knowledgePoints,
                selectedTags: .constant([]),
                dailyReviewRecords: $dailyReviewRecords,
                mode: .mastered,
                onRecordActivity: recordStudyActivity,
                onReturnHome: {
                    resetRouteStack()
                }
            )
        case .settings:
            SettingsView(
                profile: userProfile,
                dailyGoal: dailyGoalBinding,
                countdownStartDate: countdownStartDateBinding,
                countdownEndDate: countdownEndDateBinding,
                notificationsEnabled: notificationsEnabled,
                notificationTime: notificationTimeBinding,
                dangerPercent: dangerPercentBinding,
                currentPresetName: currentPreset.name,
                onClose: {
                    resetRouteStack()
                },
                onEditProfile: {
                    pushRoute(.editProfile)
                },
                onOpenOnboarding: {
                    isShowingOnboarding = true
                },
                onOpenMarkdownGuide: {
                    pushRoute(.markdownFormatGuide)
                },
                onSetNotificationsEnabled: updateNotificationsEnabled,
                onSendTestNotification: sendDebugTestNotification
            )
        case .editProfile:
            EditProfileView(profile: $userProfile)
        case .markdownEditor:
            MarkdownEditorView(
                markdownText: $markdownText,
                knowledgePoints: $knowledgePoints,
                selectedTags: $selectedTags,
                dailyReviewRecords: $dailyReviewRecords
            )
        case .presetSelection:
            PresetSelectionView(
                presets: presets,
                currentPresetID: $currentPresetID,
                switchPreset: switchToPreset,
                deletePreset: deletePresetFromSelection,
                onUploadNewPreset: {
                    pushRoute(.newPreset)
                },
                onEditPreset: { preset in
                    pushRoute(.editPreset(preset.id))
                }
            )
        case .newPreset:
            NewPresetView(createPreset: createPreset)
        case .markdownFormatGuide:
            MarkdownFormatGuideView()
        case .editPreset(let presetID):
            if let preset = presets.first(where: { $0.id == presetID }),
               let state = studyState(for: preset) {
                EditPresetView(
                    preset: preset,
                    knowledgePoints: state.knowledgePoints,
                    onSavePreset: updatePresetMetadata,
                    onAddPoint: {
                        pushRoute(.editKnowledgePoint(presetID, nil))
                    },
                    onEditPoint: { pointID in
                        pushRoute(.editKnowledgePoint(presetID, pointID))
                    },
                    onDeletePoint: deleteKnowledgePoint,
                    onDeletePreset: deletePreset
                )
            } else {
                SoftEmptyState(
                    title: "预设不存在",
                    subtitle: "请返回后重新选择预设。",
                    systemImage: "questionmark.folder"
                )
                .padding(24)
            }
        case .editKnowledgePoint(let presetID, let pointID):
            if let editorContext = knowledgePointEditorContext(presetID: presetID, pointID: pointID) {
                EditKnowledgePointView(
                    presetName: editorContext.presetName,
                    point: editorContext.point,
                    onSave: { point in
                        upsertKnowledgePoint(point, inPresetID: presetID)
                    }
                )
            } else {
                SoftEmptyState(
                    title: "知识点不存在",
                    subtitle: "请返回后重新选择知识点。",
                    systemImage: "doc.text.magnifyingglass"
                )
                .padding(24)
            }
        }
    }

    @ViewBuilder
    private var platformNavigationRoot: some View {
        #if os(macOS)
        NavigationSplitView {
            MacSidebarView(
                selection: macSidebarSelectionBinding,
                isSettingsSelected: macSettingsSelectionBinding,
                profile: userProfile
            )
            .navigationSplitViewColumnWidth(min: 210, ideal: 236, max: 280)
        } detail: {
            contentNavigationStack
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
        .navigationTitle("")
        .toolbar(removing: .title)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background {
            KikariaTheme.pageGradient
                .ignoresSafeArea()
        }
        .frame(minWidth: 1240, minHeight: 690)
        #else
        contentNavigationStack
        #endif
    }

    private var contentNavigationStack: some View {
        #if os(iOS)
        if #available(iOS 16, *) {
            contentNavigationStackModern
        } else {
            contentNavigationStackLegacy
        }
        #else
        contentNavigationStackModern
        #endif
    }

    @available(iOS 16, *)
    private var contentNavigationStackModern: some View {
        NavigationStack(path: $navigationPath) {
            contentHomePage
                .navigationDestination(for: AppRoute.self) { route in
                    routeDestination(for: route)
                }
        }
    }

    private var contentNavigationStackLegacy: some View {
        contentHomePage
            .fullScreenCover(item: legacyPresentedRouteBinding) { route in
                legacyRouteContainer(for: route.route)
            }
    }

    private var contentHomePage: some View {
        KikariaAdaptivePage { metrics in
            let isExpanded = metrics.isPadWidth
            let homeScale = metrics.homeScale
            let headerScale = metrics.homeHeaderScale
            let homeCardScale = metrics.isPadPortrait ? metrics.cardScale : 1

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                ScrollView(.vertical, showsIndicators: false) {
                    if metrics.homeUsesTwoColumnLayout {
                        homeLandscapeContent(metrics: metrics)
                    } else if metrics.isPadPortrait {
                        padPortraitHomeContent(metrics: metrics)
                    } else {
                        VStack(spacing: 0) {
                            HStack(alignment: .center) {
                                Text("Kikaria")
                                    .font(KikariaTypography.appTitle(size: 39 * headerScale))
                                    .foregroundStyle(KikariaTheme.deepText)

                                Spacer(minLength: 16)

                                #if os(iOS)
                                routeLink(to: .settings) {
                                    ProfileAvatarView(
                                        systemName: userProfile.avatarSystemName,
                                        imageData: userProfile.avatarImageData,
                                        size: 44 * headerScale
                                    )
                                }
                                .buttonStyle(.plain)
                                .accessibilityLabel("打开设置")
                                #endif
                            }
                            .padding(.top, 14)

                            Spacer(minLength: 32)

                            routeLink(to: .review) {
                                StartReviewButton(
                                    dailyGoal: dailyGoal,
                                    masteredCount: masteredCount,
                                    countdownDays: countdownDayCount,
                                    visualScale: homeScale
                                )
                            }
                            .buttonStyle(.plain)
                            .accessibilityLabel("开始背诵")

                            Spacer(minLength: 30)

                            VStack(spacing: 12) {
                                routeLink(to: .todayOverview) {
                                    TodayOverviewHomeProgressButton(
                                        dateText: homeDateTitle,
                                        daysLeftText: "\(countdownDayCount.map(String.init) ?? "--") Days Left",
                                        progressText: "\(todayMarkedMasteredCount)/\(dailyGoal)",
                                        isExpanded: isExpanded,
                                        cardScale: homeCardScale
                                    )
                                }
                                .buttonStyle(.plain)

                                HomeDashboardGridCard(
                                    scopeCountText: selectedScopeCountText,
                                    reinforcedCount: reinforcedCount,
                                    masteredCount: masteredCount,
                                    presetName: currentPreset.name,
                                    isExpanded: isExpanded,
                                    cardScale: homeCardScale
                                )
                            }
                            .padding(.bottom, 12)
                        }
                        .padding(.horizontal, metrics.horizontalPadding)
                        .frame(maxWidth: metrics.homeMaxWidth)
                        .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)
                    }
                }
            }
        }
        .kikariaHomeNavigationChrome()
    }

    private var contentHomePage: some View {
        KikariaAdaptivePage { metrics in
            let isExpanded = metrics.isPadWidth
            let homeScale = metrics.homeScale
            let headerScale = metrics.homeHeaderScale
            let homeCardScale = metrics.isPadPortrait ? metrics.cardScale : 1

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                ScrollView(.vertical, showsIndicators: false) {
                    if metrics.homeUsesTwoColumnLayout {
                        homeLandscapeContent(metrics: metrics)
                    } else if metrics.isPadPortrait {
                        padPortraitHomeContent(metrics: metrics)
                    } else {
                        VStack(spacing: 0) {
                            HStack(alignment: .center) {
                                Text("Kikaria")
                                    .font(KikariaTypography.appTitle(size: 39 * headerScale))
                                    .foregroundStyle(KikariaTheme.deepText)

                                Spacer(minLength: 16)

                                #if os(iOS)
                                routeLink(to: .settings) {
                                    ProfileAvatarView(
                                        systemName: userProfile.avatarSystemName,
                                        imageData: userProfile.avatarImageData,
                                        size: 44 * headerScale
                                    )
                                }
                                .buttonStyle(.plain)
                                .accessibilityLabel("打开设置")
                                #endif
                            }
                            .padding(.top, 14)

                            Spacer(minLength: 32)

                            routeLink(to: .review) {
                                StartReviewButton(
                                    dailyGoal: dailyGoal,
                                    masteredCount: masteredCount,
                                    countdownDays: countdownDayCount,
                                    visualScale: homeScale
                                )
                            }
                            .buttonStyle(.plain)
                            .accessibilityLabel("开始背诵")

                            Spacer(minLength: 30)

                            VStack(spacing: 12) {
                                routeLink(to: .todayOverview) {
                                    TodayOverviewHomeProgressButton(
                                        dateText: homeDateTitle,
                                        daysLeftText: "\(countdownDayCount.map(String.init) ?? "--") Days Left",
                                        progressText: "\(todayMarkedMasteredCount)/\(dailyGoal)",
                                        isExpanded: isExpanded,
                                        cardScale: homeCardScale
                                    )
                                }
                                .buttonStyle(.plain)

                                HomeDashboardGridCard(
                                    scopeCountText: selectedScopeCountText,
                                    reinforcedCount: reinforcedCount,
                                    masteredCount: masteredCount,
                                    presetName: currentPreset.name,
                                    isExpanded: isExpanded,
                                    cardScale: homeCardScale
                                )
                            }
                            .padding(.bottom, 12)
                        }
                        .padding(.horizontal, metrics.horizontalPadding)
                        .frame(maxWidth: metrics.homeMaxWidth)
                        .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)
                    }
                }
            }
        }
        .kikariaHomeNavigationChrome()
    }

    @ViewBuilder
    private func legacyRouteContainer(for route: AppRoute) -> some View {
        ZStack(alignment: .topLeading) {
            routeDestination(for: route)
                .id(route)

            if legacyNavigationStack.count > 1 {
                Button(action: popRoute) {
                    Image(systemName: "chevron.left")
                        .font(.system(size: 17, weight: .semibold))
                        .foregroundStyle(KikariaTheme.deepText)
                        .frame(width: 44, height: 44)
                        .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.10, shadowRadius: 12, shadowY: 6)
                }
                .buttonStyle(.plain)
                .padding(.top, 18)
                .padding(.leading, 18)
            }
        }
        .background {
            KikariaTheme.pageGradient
                .ignoresSafeArea()
        }
        .ignoresSafeArea()
    }

    private var legacyPresentedRouteBinding: Binding<LegacyPresentedRoute?> {
        Binding(
            get: {
                legacyNavigationStack.last
            },
            set: { route in
                if route == nil {
                    legacyNavigationStack.removeAll()
                }
            }
        )
    }

    #if os(macOS)
    private var macSidebarSelectionBinding: Binding<MacSidebarDestination?> {
        Binding(
            get: { macSidebarSelection },
            set: { destination in
                if let destination {
                    openMacSidebarDestination(destination)
                }
            }
        )
    }

    private var macSettingsSelectionBinding: Binding<Bool> {
        Binding(
            get: { macSettingsIsSelected },
            set: { isSelected in
                if isSelected {
                    openMacSettings()
                }
            }
        )
    }

    private var macSidebarSelection: MacSidebarDestination? {
        guard let route = navigationPath.last else {
            return .dashboard
        }

        return MacSidebarDestination(route: route)
    }

    private var macSettingsIsSelected: Bool {
        guard let route = navigationPath.last else {
            return false
        }

        switch route {
        case .settings, .editProfile, .markdownFormatGuide:
            return true
        default:
            return false
        }
    }

    private func openMacSidebarDestination(_ destination: MacSidebarDestination) {
        withAnimation(.spring(response: 0.30, dampingFraction: 0.88)) {
            switch destination {
            case .dashboard:
                navigationPath.removeAll()
            case .todayOverview:
                navigationPath = [.todayOverview]
            case .reinforcement:
                navigationPath = [.reinforcement]
            case .mastered:
                navigationPath = [.mastered]
            case .presetSelection:
                navigationPath = [.presetSelection]
            }
        }
    }

    private func openMacSettings() {
        withAnimation(.spring(response: 0.30, dampingFraction: 0.88)) {
            navigationPath = [.settings]
        }
    }
    #endif

    private var dailyGoalBinding: Binding<Int> {
        Binding(
            get: { dailyGoal },
            set: { newValue in
                updateDailyGoal(newValue)
            }
        )
    }

    private var countdownStartDateBinding: Binding<Date?> {
        Binding(
            get: { countdownStartDate },
            set: { newValue in
                updateCountdownRange(startDate: newValue, endDate: countdownEndDate)
            }
        )
    }

    private var countdownEndDateBinding: Binding<Date?> {
        Binding(
            get: { countdownEndDate },
            set: { newValue in
                updateCountdownRange(startDate: countdownStartDate, endDate: newValue)
            }
        )
    }

    private var notificationTimeBinding: Binding<Date> {
        Binding(
            get: { notificationTime },
            set: { newValue in
                updateNotificationTime(newValue)
            }
        )
    }

    private var dangerPercentBinding: Binding<Int> {
        Binding(
            get: { dangerPercent },
            set: { newValue in
                updateDangerPercent(newValue)
            }
        )
    }

    private func loadInitialPresetStateIfNeeded() {
        guard !hasLoadedInitialPresetState else {
            return
        }

        hasLoadedInitialPresetState = true
        loadAppState()

        guard let state = studyState(for: currentPreset) else {
            return
        }

        presetStates[currentPresetID] = state
        restorePresetState(state)
        rescheduleAllPresetNotifications()
    }

    private func switchToPreset(_ preset: KnowledgePreset) -> Bool {
        guard let targetState = studyState(for: preset) else {
            return false
        }

        saveCurrentPresetState()

        withAnimation(.spring(response: 0.36, dampingFraction: 0.9)) {
            if presetStates[preset.id] == nil {
                presetStates[preset.id] = targetState
            }

            restorePresetState(targetState)
        }

        persistLibrary()
        rescheduleAllPresetNotifications()
        return true
    }

    private func saveCurrentPresetState() {
        let state = currentPresetStateSnapshot()
        presetStates[currentPresetID] = state
        persistLibrary()
    }

    private func studyState(for preset: KnowledgePreset) -> PresetStudyState? {
        if let state = presetStates[preset.id] {
            return state
        }

        return initialStudyState(for: preset)
    }

    private func initialStudyState(for preset: KnowledgePreset) -> PresetStudyState? {
        guard let parsedPoints = try? KnowledgePoint.parseMarkdown(preset.markdownText) else {
            return nil
        }

        return PresetStudyState(
            presetId: preset.id,
            knowledgePoints: parsedPoints,
            markdownText: preset.markdownText,
            selectedTags: [],
            dailyReviewRecords: [:],
            activityRecords: [],
            dailyGoal: dailyGoal(forPresetID: preset.id),
            countdownStartDate: nil,
            countdownEndDate: nil
        )
    }

    private func currentPresetStateSnapshot() -> PresetStudyState {
        PresetStudyState(
            presetId: currentPresetID,
            knowledgePoints: knowledgePoints,
            markdownText: markdownText,
            selectedTags: selectedTags,
            dailyReviewRecords: dailyReviewRecords,
            activityRecords: activityRecords,
            dailyGoal: clampedDailyGoal(dailyGoal),
            countdownStartDate: countdownStartDate,
            countdownEndDate: countdownEndDate,
            notificationsEnabled: notificationsEnabled,
            notificationTime: notificationTime,
            dangerPercent: clampedDangerPercent(dangerPercent)
        )
    }

    private func restorePresetState(_ state: PresetStudyState) {
        isApplyingPresetState = true
        currentPresetID = state.presetId
        knowledgePoints = state.knowledgePoints
        markdownText = state.markdownText
        selectedTags = validSelectedTags(from: state.selectedTags, in: state.knowledgePoints)
        dailyReviewRecords = state.dailyReviewRecords
        activityRecords = state.activityRecords.filter { record in
            state.knowledgePoints.contains { $0.id == record.pointId }
        }
        dailyGoal = clampedDailyGoal(state.dailyGoal)
        countdownStartDate = state.countdownStartDate
        countdownEndDate = state.countdownEndDate
        notificationsEnabled = state.notificationsEnabled
        notificationTime = normalizedNotificationTime(state.notificationTime)
        dangerPercent = clampedDangerPercent(state.dangerPercent)

        DispatchQueue.main.async {
            isApplyingPresetState = false
            presetStates[state.presetId] = currentPresetStateSnapshot()
            persistLibrary()
            rescheduleAllPresetNotifications()
            updateWidgetSnapshot()
        }
    }

    private func validSelectedTags(from tags: Set<String>, in points: [KnowledgePoint]) -> Set<String> {
        let availableTags = Set(points.flatMap(\.tags))
        return Set(tags.filter { availableTags.contains($0) })
    }

    private func updateDailyGoal(_ newValue: Int) {
        let goal = clampedDailyGoal(newValue)
        dailyGoal = goal
        presetStates[currentPresetID] = currentPresetStateSnapshot()
        persistLibrary()
        rescheduleAllPresetNotifications()
        updateWidgetSnapshot()
    }

    private func updateCountdownRange(startDate: Date?, endDate: Date?) {
        countdownStartDate = startDate
        countdownEndDate = endDate
        presetStates[currentPresetID] = currentPresetStateSnapshot()
        persistLibrary()
        rescheduleAllPresetNotifications()
        updateWidgetSnapshot()
    }

    private func updateNotificationsEnabled(_ newValue: Bool, completion: @escaping (Bool, String?) -> Void) {
        if newValue {
            KikariaNotificationManager.requestAuthorization { granted in
                notificationsEnabled = granted
                presetStates[currentPresetID] = currentPresetStateSnapshot()
                persistLibrary()
                rescheduleAllPresetNotifications()
                completion(granted, granted ? nil : "请在系统设置中允许通知")
            }
        } else {
            notificationsEnabled = false
            presetStates[currentPresetID] = currentPresetStateSnapshot()
            persistLibrary()
            KikariaNotificationManager.cancelStudyProgressWarning(for: currentPresetID)
            completion(false, nil)
        }
    }

    private func updateNotificationTime(_ newValue: Date) {
        notificationTime = normalizedNotificationTime(newValue)
        presetStates[currentPresetID] = currentPresetStateSnapshot()
        persistLibrary()
        rescheduleAllPresetNotifications()
        updateWidgetSnapshot()
    }

    private func updateDangerPercent(_ newValue: Int) {
        dangerPercent = clampedDangerPercent(newValue)
        presetStates[currentPresetID] = currentPresetStateSnapshot()
        persistLibrary()
        rescheduleAllPresetNotifications()
    }

    private func sendDebugTestNotification(completion: @escaping (String) -> Void) {
        KikariaNotificationManager.scheduleDebugTestNotification(
            presetName: currentPreset.name,
            completion: completion
        )
    }

    private func dailyGoal(forPresetID presetID: String) -> Int {
        if let goal = presetStates[presetID]?.dailyGoal {
            return clampedDailyGoal(goal)
        }

        if presetID == KnowledgePreset.defaultPresetID {
            return clampedDailyGoal(legacyDailyGoalValue())
        }

        return 20
    }

    private func legacyDailyGoalValue() -> Int {
        if let value = UserDefaults.standard.object(forKey: "dailyLearningGoal") as? Int {
            return value
        }

        return 20
    }

    private func persistCurrentStudyStateIfReady(refreshWidget: Bool = false) {
        guard hasLoadedInitialPresetState, !isApplyingPresetState else {
            return
        }

        presetStates[currentPresetID] = currentPresetStateSnapshot()
        scheduleStudyStatePersistence(refreshWidget: refreshWidget)
    }

    private func scheduleStudyStatePersistence(refreshWidget: Bool) {
        pendingStudyStatePersistenceWorkItem?.cancel()
        pendingStudyStatePersistenceRefreshesWidget = pendingStudyStatePersistenceRefreshesWidget || refreshWidget

        var workItem: DispatchWorkItem?
        workItem = DispatchWorkItem {
            guard workItem?.isCancelled == false else {
                return
            }

            let shouldRefreshWidget = pendingStudyStatePersistenceRefreshesWidget
            pendingStudyStatePersistenceWorkItem = nil
            pendingStudyStatePersistenceRefreshesWidget = false
            saveAppState()

            if shouldRefreshWidget {
                updateWidgetSnapshot()
            }
        }

        pendingStudyStatePersistenceWorkItem = workItem
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.7, execute: workItem!)
    }

    private func cancelPendingStudyStatePersistence() -> (hadPendingWork: Bool, shouldRefreshWidget: Bool) {
        let hadPendingWork = pendingStudyStatePersistenceWorkItem != nil
        let shouldRefreshWidget = hadPendingWork && pendingStudyStatePersistenceRefreshesWidget
        pendingStudyStatePersistenceWorkItem?.cancel()
        pendingStudyStatePersistenceWorkItem = nil
        pendingStudyStatePersistenceRefreshesWidget = false
        return (hadPendingWork, shouldRefreshWidget)
    }

    @discardableResult
    private func flushPendingStudyStatePersistenceIfNeeded() -> Bool {
        let pendingPersistence = cancelPendingStudyStatePersistence()
        guard pendingPersistence.hadPendingWork else {
            return false
        }

        saveAppState()

        if pendingPersistence.shouldRefreshWidget {
            updateWidgetSnapshot()
        }

        return true
    }

    private func loadAppState() {
        let defaults = UserDefaults.standard

        if let data = defaults.data(forKey: KikariaAppState.storageKey) {
            do {
                let appState = try JSONDecoder().decode(KikariaAppState.self, from: data)
                applyLoadedAppState(appState)
                #if DEBUG
                print("Kikaria app state loaded")
                #endif
                return
            } catch {
                #if DEBUG
                print("Kikaria app state decode failed: \(error)")
                #endif
            }
        }

        if let legacyEncodedLibrary = defaults.string(forKey: "presetLibraryJSON"),
           let data = legacyEncodedLibrary.data(using: .utf8),
           let snapshot = try? JSONDecoder().decode(PresetLibrarySnapshot.self, from: data),
           !snapshot.presets.isEmpty {
            presets = mergedPresets(with: snapshot.presets)
            presetStates = snapshot.presetStates
            if presets.contains(where: { $0.id == snapshot.currentPresetID }) {
                currentPresetID = snapshot.currentPresetID
            } else {
                currentPresetID = presets.first?.id ?? KnowledgePreset.defaultPresetID
            }
            removeRetiredBuiltInPresetsIfNeeded()
        } else {
            presets = KnowledgePreset.all
            presetStates = [:]
            currentPresetID = KnowledgePreset.defaultPresetID
        }

        if let completed = defaults.object(forKey: "hasCompletedOnboarding") as? Bool {
            hasCompletedOnboarding = completed
        }

        userProfile = UserProfile()
        hasCompletedProfileSetup = false
        ensurePresetStatesExist()
        #if DEBUG
        print("Kikaria app state loaded")
        #endif
    }

    private func applyLoadedAppState(_ appState: KikariaAppState) {
        presets = appState.presets.isEmpty ? KnowledgePreset.all : mergedPresets(with: appState.presets)

        presetStates = appState.presetStates
        userProfile = appState.userProfile
        hasCompletedProfileSetup = appState.hasCompletedProfileSetup
        hasCompletedOnboarding = appState.hasCompletedOnboarding

        if presets.contains(where: { $0.id == appState.currentPresetID }) {
            currentPresetID = appState.currentPresetID
        } else {
            currentPresetID = presets.first?.id ?? KnowledgePreset.defaultPresetID
        }

        removeRetiredBuiltInPresetsIfNeeded()
        ensurePresetStatesExist()
    }

    private func ensurePresetStatesExist() {
        let validPresetIDs = Set(presets.map(\.id))
        presetStates = presetStates.filter { validPresetIDs.contains($0.key) }

        for preset in presets where presetStates[preset.id] == nil {
            if let state = initialStudyState(for: preset) {
                presetStates[preset.id] = state
            }
        }

        for preset in presets where preset.isBuiltIn {
            guard presetStates[preset.id]?.markdownText != preset.markdownText,
                  let state = initialStudyState(for: preset) else {
                continue
            }

            presetStates[preset.id] = state
        }
    }

    private func mergedPresets(with storedPresets: [KnowledgePreset]) -> [KnowledgePreset] {
        var merged = KnowledgePreset.all
        var existingIDs = Set(merged.map(\.id))

        for storedPreset in storedPresets where !storedPreset.isBuiltIn {
            guard !existingIDs.contains(storedPreset.id) else {
                continue
            }

            merged.append(storedPreset)
            existingIDs.insert(storedPreset.id)
        }

        return merged
    }

    private func removeRetiredBuiltInPresetsIfNeeded() {
        let removedIDs = Set(presets.filter(isRetiredBuiltInPreset).map(\.id))
            .union(Set(presetStates.keys).intersection(retiredBuiltInPresetIDs))

        if !removedIDs.isEmpty {
            presets.removeAll(where: isRetiredBuiltInPreset)
        }

        for presetID in removedIDs {
            presetStates[presetID] = nil
            KikariaNotificationManager.cancelStudyProgressWarning(for: presetID)
        }

        if presets.isEmpty {
            presets = KnowledgePreset.all
        }

        if !presets.contains(where: { $0.id == currentPresetID }) {
            currentPresetID = presets.first?.id ?? KnowledgePreset.defaultPresetID
        }
    }

    private func persistLibrary() {
        let pendingPersistence = cancelPendingStudyStatePersistence()
        saveAppState()

        if pendingPersistence.shouldRefreshWidget {
            updateWidgetSnapshot()
        }
    }

    private func saveAppStateIfReady() {
        guard hasLoadedInitialPresetState, !isApplyingPresetState else {
            return
        }

        if !flushPendingStudyStatePersistenceIfNeeded() {
            saveAppState()
        }
    }

    private func saveAppState() {
        var states = presetStates

        if hasLoadedInitialPresetState {
            states[currentPresetID] = currentPresetStateSnapshot()
        }

        let appState = KikariaAppState(
            presets: presets,
            presetStates: states,
            currentPresetID: currentPresetID,
            userProfile: userProfile,
            hasCompletedProfileSetup: hasCompletedProfileSetup,
            hasCompletedOnboarding: hasCompletedOnboarding
        )

        do {
            let data = try JSONEncoder().encode(appState)
            UserDefaults.standard.set(data, forKey: KikariaAppState.storageKey)
            #if DEBUG
            print("Kikaria app state saved")
            #endif
        } catch {
            #if DEBUG
            print("Kikaria app state save failed: \(error)")
            #endif
        }
    }

    private func createPreset(name: String, category: String, markdownText: String) -> PresetCreationOutcome {
        let trimmedName = name.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedName.isEmpty else {
            return .failure("请填写预设名称。")
        }

        let trimmedMarkdown = markdownText.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedCategory = category.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let parsedPoints = try? KnowledgePoint.parseMarkdown(trimmedMarkdown) else {
            return .failure("没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。")
        }

        saveCurrentPresetState()

        let preset = KnowledgePreset(
            id: "user-\(UUID().uuidString)",
            name: trimmedName,
            subtitle: "自定义知识点",
            description: "",
            category: trimmedCategory.isEmpty ? "自定义" : trimmedCategory,
            markdownText: trimmedMarkdown,
            isBuiltIn: false
        )

        let state = PresetStudyState(
            presetId: preset.id,
            knowledgePoints: parsedPoints,
            markdownText: trimmedMarkdown,
            selectedTags: [],
            dailyReviewRecords: [:],
            activityRecords: [],
            dailyGoal: 20,
            countdownStartDate: nil,
            countdownEndDate: nil,
            notificationsEnabled: false,
            notificationTime: PresetStudyState.defaultNotificationTime(),
            dangerPercent: 80
        )

        presets.append(preset)
        presetStates[preset.id] = state
        restorePresetState(state)
        persistLibrary()

        return .success(preset)
    }

    private func updatePresetMetadata(presetID: String, name: String, category: String) {
        guard let index = presets.firstIndex(where: { $0.id == presetID }) else {
            return
        }

        let trimmedName = name.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedCategory = category.trimmingCharacters(in: .whitespacesAndNewlines)

        presets[index].name = trimmedName.isEmpty ? presets[index].name : trimmedName
        presets[index].category = trimmedCategory.isEmpty ? "自定义" : trimmedCategory
        persistLibrary()
        rescheduleAllPresetNotifications()
        if presetID == currentPresetID {
            updateWidgetSnapshot()
        }
    }

    private func deletePreset(_ presetID: String) {
        _ = deletePresetFromSelection(presetID)
    }

    private func deletePresetFromSelection(_ presetID: String) -> PresetDeleteOutcome {
        guard let preset = presets.first(where: { $0.id == presetID }) else {
            return .notFound
        }

        guard presets.count > 1 else {
            return .blockedLastPreset
        }

        let deletedName = preset.name
        let wasCurrentPreset = presetID == currentPresetID
        presets.removeAll { $0.id == presetID }
        presetStates[presetID] = nil
        KikariaNotificationManager.cancelStudyProgressWarning(for: presetID)

        if wasCurrentPreset, let nextPreset = presets.first {
            let nextState = studyState(for: nextPreset) ?? initialStudyState(for: nextPreset)

            if let nextState {
                withAnimation(.spring(response: 0.36, dampingFraction: 0.9)) {
                    restorePresetState(nextState)
                }
            } else {
                currentPresetID = nextPreset.id
                persistLibrary()
                rescheduleAllPresetNotifications()
                updateWidgetSnapshot()
            }
        } else {
            persistLibrary()
            rescheduleAllPresetNotifications()
            updateWidgetSnapshot()
        }

        return .deleted(deletedName)
    }

    private func knowledgePointEditorContext(presetID: String, pointID: UUID?) -> (presetName: String, point: KnowledgePoint?)? {
        guard let preset = presets.first(where: { $0.id == presetID }),
              let state = studyState(for: preset)
        else {
            return nil
        }

        let point = pointID.flatMap { id in
            state.knowledgePoints.first { $0.id == id }
        }

        if pointID != nil, point == nil {
            return nil
        }

        return (preset.name, point)
    }

    private func upsertKnowledgePoint(_ point: KnowledgePoint, inPresetID presetID: String) {
        guard var state = stateForEditing(presetID: presetID) else {
            return
        }

        if let index = state.knowledgePoints.firstIndex(where: { $0.id == point.id }) {
            state.knowledgePoints[index] = point
        } else {
            state.knowledgePoints.append(point)
        }

        syncEditedState(state)
    }

    private func deleteKnowledgePoint(_ pointID: UUID, fromPresetID presetID: String) {
        guard var state = stateForEditing(presetID: presetID) else {
            return
        }

        state.knowledgePoints.removeAll { $0.id == pointID }
        state.dailyReviewRecords[pointID] = nil
        state.activityRecords.removeAll { $0.pointId == pointID }
        state.selectedTags = validSelectedTags(from: state.selectedTags, in: state.knowledgePoints)
        syncEditedState(state)
    }

    private func stateForEditing(presetID: String) -> PresetStudyState? {
        if presetID == currentPresetID {
            return currentPresetStateSnapshot()
        }

        guard let preset = presets.first(where: { $0.id == presetID }) else {
            return nil
        }

        return studyState(for: preset)
    }

    private func syncEditedState(_ state: PresetStudyState) {
        var editedState = state
        let generatedMarkdown = KnowledgePoint.markdownText(from: editedState.knowledgePoints)
        editedState.markdownText = generatedMarkdown
        presetStates[editedState.presetId] = editedState

        if let presetIndex = presets.firstIndex(where: { $0.id == editedState.presetId }) {
            presets[presetIndex].markdownText = generatedMarkdown
        }

        if editedState.presetId == currentPresetID {
            restorePresetState(editedState)
        } else {
            persistLibrary()
            rescheduleAllPresetNotifications()
        }
        updateWidgetSnapshot()
    }

    private func clampedDailyGoal(_ goal: Int) -> Int {
        min(max(goal, 1), 100)
    }

    private func clampedDangerPercent(_ percent: Int) -> Int {
        min(max(percent, 1), 100)
    }

    private func normalizedNotificationTime(_ date: Date) -> Date {
        let components = Calendar.current.dateComponents([.hour, .minute], from: date)
        var normalized = Calendar.current.dateComponents([.year, .month, .day], from: Date())
        normalized.hour = components.hour ?? 21
        normalized.minute = components.minute ?? 0
        normalized.second = 0
        return Calendar.current.date(from: normalized) ?? PresetStudyState.defaultNotificationTime()
    }

    private func rescheduleAllPresetNotifications() {
        guard hasLoadedInitialPresetState else {
            return
        }

        var states = presetStates
        states[currentPresetID] = currentPresetStateSnapshot()
        let presetNames = Dictionary(uniqueKeysWithValues: presets.map { ($0.id, $0.name) })
        KikariaNotificationManager.rescheduleAllStudyProgressWarnings(for: states, presetNames: presetNames)
    }

    private func records(on date: Date, type: StudyActivityType? = nil) -> [StudyActivityRecord] {
        currentPresetActivityRecords.filter { record in
            Calendar.current.isDate(record.date, inSameDayAs: date) &&
                (type == nil || record.type == type)
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

    private func recordStudyActivity(_ type: StudyActivityType, point: KnowledgePoint) {
        activityRecords.append(
            StudyActivityRecord(
                presetId: currentPresetID,
                type: type,
                pointId: point.id,
                pointTitle: point.title
            )
        )
    }

    private func updateWidgetSnapshot() {
        WidgetDataStore.save(
            WidgetSnapshot(
                presetName: currentPreset.name,
                todayMasteredCount: todayMarkedMasteredCount,
                masteredCount: masteredCount,
                dailyGoal: dailyGoal,
                countdownDays: countdownDayCount,
                todayReviewCount: todayReviewedAnswerCount,
                todayHintCount: todayViewedHintCount,
                randomKnowledgePoints: widgetKnowledgePointPreviews(),
                lastUpdated: Date()
            )
        )
    }

    private func widgetKnowledgePointPreviews() -> [WidgetKnowledgePointPreview] {
        let preferredPoints = knowledgePoints.filter { !$0.isMastered }
        let sourcePoints = preferredPoints.isEmpty ? knowledgePoints : preferredPoints

        return sourcePoints
            .shuffled()
            .prefix(5)
            .map { point in
                WidgetKnowledgePointPreview(
                    title: point.title,
                    tag: point.tags.first
                )
            }
    }

}

private struct OnboardingPage: Identifiable {
    let id = UUID()
    let title: String
    let subtitle: String
    let systemImage: String
}

private struct OnboardingView: View {
    let onComplete: () -> Void
    @State private var selectedPage = 0

    private let pages = [
        OnboardingPage(
            title: "选择一套预设",
            subtitle: "从数学、物理、计算机科学与英语预设开始，也可以上传自己的 Markdown 知识点。",
            systemImage: "books.vertical.fill"
        ),
        OnboardingPage(
            title: "先回忆，再查看",
            subtitle: "背诵时先看知识点名称，必要时查看提示，再查看答案。",
            systemImage: "lightbulb.max.fill"
        ),
        OnboardingPage(
            title: "整理你的学习状态",
            subtitle: "把不熟的内容加入重点集锦，把已经掌握的内容标记为已掌握。",
            systemImage: "checkmark.seal.fill"
        )
    ]

    var body: some View {
        KikariaAdaptivePage { metrics in
            let isExpanded = metrics.isPadWidth
            let cardMaxWidth: CGFloat = metrics.isPadPortrait ? 620 : 600

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 28) {
                    HStack {
                        Text("Kikaria")
                            .font(KikariaTypography.appTitle(size: 36))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()
                    }
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.top, 24)
                    .frame(maxWidth: metrics.homeMaxWidth)
                    .frame(maxWidth: .infinity)

                    TabView(selection: $selectedPage) {
                        ForEach(Array(pages.enumerated()), id: \.element.id) { index, page in
                            OnboardingPageCard(isExpanded: isExpanded, page: page)
                                .frame(maxWidth: isExpanded ? cardMaxWidth : metrics.formMaxWidth)
                                .frame(maxWidth: .infinity)
                                .tag(index)
                                .padding(.horizontal, metrics.horizontalPadding)
                        }
                    }
                    #if os(iOS)
                    .tabViewStyle(.page(indexDisplayMode: .always))
                    .indexViewStyle(.page(backgroundDisplayMode: .interactive))
                    #endif

                    Button {
                        if selectedPage < pages.count - 1 {
                            withAnimation(.spring(response: 0.36, dampingFraction: 0.88)) {
                                selectedPage += 1
                            }
                        } else {
                            onComplete()
                        }
                    } label: {
                        Text(selectedPage == pages.count - 1 ? "开始使用" : "下一步")
                            .font(KikariaTypography.chineseButton(size: isExpanded ? 19 : 18))
                            .foregroundStyle(.white)
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, isExpanded ? 19 : 17)
                            .background(KikariaTheme.actionGradient, in: Capsule())
                            .shadow(color: KikariaTheme.sky.opacity(0.22), radius: 18, y: 10)
                    }
                    .buttonStyle(.plain)
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.bottom, 28)
                    .frame(maxWidth: isExpanded ? cardMaxWidth : metrics.formMaxWidth)
                    .frame(maxWidth: .infinity)
                }
            }
        }
    }
}

private struct OnboardingPageCard: View {
    var isExpanded = false
    let page: OnboardingPage

    var body: some View {
        VStack(spacing: isExpanded ? 30 : 26) {
            ZStack {
                Circle()
                    .fill(KikariaTheme.actionGradient)
                    .frame(width: isExpanded ? 156 : 132, height: isExpanded ? 156 : 132)
                    .background(.ultraThinMaterial, in: Circle())
                    .shadow(color: KikariaTheme.sky.opacity(0.20), radius: isExpanded ? 28 : 24, y: isExpanded ? 16 : 14)

                Circle()
                    .fill(.white.opacity(0.24))
                    .frame(width: isExpanded ? 102 : 86, height: isExpanded ? 102 : 86)
                    .offset(x: isExpanded ? 32 : 28, y: isExpanded ? -30 : -26)

                Image(systemName: page.systemImage)
                    .font(.system(size: isExpanded ? 62 : 54, weight: .semibold))
                    .foregroundStyle(.white.opacity(0.96))
            }

            VStack(spacing: isExpanded ? 14 : 12) {
                Text(page.title)
                    .font(KikariaTypography.chineseTitle(size: isExpanded ? 32 : 29, weight: .bold))
                    .foregroundStyle(KikariaTheme.deepText)
                    .multilineTextAlignment(.center)

                Text(page.subtitle)
                    .font(KikariaTypography.chineseBody(size: isExpanded ? 17 : 16, weight: .medium))
                    .foregroundStyle(KikariaTheme.softText)
                    .multilineTextAlignment(.center)
                    .lineSpacing(isExpanded ? 7 : 6)
            }
        }
        .padding(.horizontal, isExpanded ? 34 : 24)
        .padding(.vertical, isExpanded ? 54 : 44)
        .frame(maxWidth: .infinity, maxHeight: isExpanded ? 520 : 430)
        .liquidGlassCard(cornerRadius: isExpanded ? 38 : 34, fillOpacity: 0.50, strokeOpacity: 0.48, shadowOpacity: 0.13, shadowRadius: isExpanded ? 28 : 24, shadowY: isExpanded ? 16 : 14)
    }
}

private extension KnowledgePoint {
    func matchesSearchQuery(_ query: String) -> Bool {
        let trimmedQuery = query.trimmingCharacters(in: .whitespacesAndNewlines)
        return matchesPreparedSearchQuery(trimmedQuery)
    }

    func matchesPreparedSearchQuery(_ trimmedQuery: String) -> Bool {
        guard !trimmedQuery.isEmpty else {
            return true
        }

        let searchableFields = [
            title,
            tags.joined(separator: " "),
            hint,
            content
        ]

        return searchableFields.contains { field in
            field.range(
                of: trimmedQuery,
                options: [.caseInsensitive, .diacriticInsensitive]
            ) != nil
        }
    }
}

private struct KikariaSearchBar: View {
    @Binding var text: String
    var placeholder = "搜索知识点"
    var scale: CGFloat = 1

    var body: some View {
        let resolvedScale = max(scale, 1)

        HStack(spacing: 10 * resolvedScale) {
            Image(systemName: "magnifyingglass")
                .font(.system(size: 15 * resolvedScale, weight: .semibold))
                .foregroundStyle(KikariaTheme.blueGray)

            TextField(placeholder, text: $text)
                .font(KikariaTypography.chineseBody(size: 15 * resolvedScale, weight: .medium))
                .foregroundStyle(KikariaTheme.deepText)
                .textInputAutocapitalization(.never)
                .autocorrectionDisabled()
                .kikariaMacPlainTextFieldStyle(true)

            if !text.isEmpty {
                Button {
                    text = ""
                } label: {
                    Image(systemName: "xmark.circle.fill")
                        .font(.system(size: 15 * resolvedScale, weight: .semibold))
                        .foregroundStyle(KikariaTheme.blueGray.opacity(0.75))
                }
                .buttonStyle(.plain)
            }
        }
        .padding(.horizontal, 16 * resolvedScale)
        .frame(maxWidth: .infinity, minHeight: 50 * resolvedScale)
        .liquidGlassCard(cornerRadius: 22 * resolvedScale, fillOpacity: 0.44, strokeOpacity: 0.40, shadowOpacity: 0.08, shadowRadius: 12 * resolvedScale, shadowY: 7 * resolvedScale)
    }
}

private struct KikariaAdaptiveBackButton: View {
    let metrics: KikariaAdaptiveLayout.Metrics
    let action: () -> Void

    var body: some View {
        let size: CGFloat = 42

        Button(action: action) {
            Image(systemName: "chevron.left")
                .font(.headline.weight(.semibold))
                .foregroundStyle(KikariaTheme.deepText)
                .frame(width: size, height: size)
                .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.08, shadowRadius: 10, shadowY: 5)
        }
        .buttonStyle(.plain)
        .frame(width: size, height: size)
        .contentShape(Circle())
        .accessibilityLabel("返回")
    }
}

private struct KikariaAdaptiveNavigationChrome: ViewModifier {
    @Environment(\.dismiss) private var dismiss
    let metrics: KikariaAdaptiveLayout.Metrics
    let outerMaxWidth: CGFloat

    func body(content: Content) -> some View {
        content
            .navigationTitle("")
            .navigationBarTitleDisplayMode(.inline)
            .navigationBarBackButtonHidden(metrics.isPadPortrait)
            .overlay(alignment: .topLeading) {
                if metrics.isPadPortrait {
                    KikariaAdaptiveBackButton(metrics: metrics) {
                        dismiss()
                    }
                    .padding(.leading, metrics.horizontalPadding)
                    .padding(.top, 12)
                }
            }
    }
}

extension View {
    func kikariaAdaptiveNavigationChrome(
        metrics: KikariaAdaptiveLayout.Metrics,
        outerMaxWidth: CGFloat
    ) -> some View {
        modifier(KikariaAdaptiveNavigationChrome(metrics: metrics, outerMaxWidth: outerMaxWidth))
    }
}

private struct ShareFile: Identifiable {
    let id = UUID()
    let url: URL
}

#if os(iOS)
private struct ActivityView: UIViewControllerRepresentable {
    let activityItems: [Any]

    func makeUIViewController(context: Context) -> UIActivityViewController {
        UIActivityViewController(activityItems: activityItems, applicationActivities: nil)
    }

    func updateUIViewController(_ uiViewController: UIActivityViewController, context: Context) {}
}
#elseif os(macOS)
private struct ActivityView: View {
    @Environment(\.dismiss) private var dismiss
    let activityItems: [Any]

    var body: some View {
        VStack(spacing: 16) {
            Text("导出文件已准备好")
                .font(KikariaTypography.chineseHeadline(size: 18))
                .foregroundStyle(KikariaTheme.deepText)

            if let url = activityItems.compactMap({ $0 as? URL }).first {
                KikariaTypography.mixedText(url.lastPathComponent, size: 14)
                    .foregroundStyle(KikariaTheme.softText)
                    .lineLimit(2)
            }

            Button("完成") {
                dismiss()
            }
            .font(KikariaTypography.chineseButton(size: 14))
            .buttonStyle(.borderedProminent)
        }
        .padding(28)
        .frame(minWidth: 320)
    }
}
#endif

private func sanitizedFilename(_ name: String) -> String {
    let invalidCharacters = CharacterSet(charactersIn: "/\\?%*|\"<>:")
        .union(.newlines)
    let components = name.components(separatedBy: invalidCharacters)
    let sanitized = components.joined(separator: "-")
        .trimmingCharacters(in: .whitespacesAndNewlines)
    return sanitized.isEmpty ? "预设" : sanitized
}

private struct ActivitySummary {
    let viewedHintCount: Int
    let reviewedAnswerCount: Int
    let markedMasteredCount: Int
    let addedReinforcementCount: Int
    let removedMasteredCount: Int
    let removedReinforcementCount: Int

    var totalCount: Int {
        viewedHintCount + reviewedAnswerCount + markedMasteredCount + addedReinforcementCount + removedMasteredCount + removedReinforcementCount
    }

    static func make(from records: [StudyActivityRecord]) -> ActivitySummary {
        ActivitySummary(
            viewedHintCount: records.filter { $0.type == .viewedHint }.count,
            reviewedAnswerCount: records.filter { $0.type == .reviewedAnswer }.count,
            markedMasteredCount: Set(records.filter { $0.type == .markedMastered }.map(\.pointId)).count,
            addedReinforcementCount: records.filter { $0.type == .addedReinforcement }.count,
            removedMasteredCount: records.filter { $0.type == .removedMastered }.count,
            removedReinforcementCount: records.filter { $0.type == .removedReinforcement }.count
        )
    }
}

private struct TodayOverviewView: View {
    let presetName: String
    let activityRecords: [StudyActivityRecord]
    let knowledgePoints: [KnowledgePoint]
    let dailyGoal: Int
    let countdownEndDate: Date?
    let onOpenHistory: () -> Void

    private var todayRecords: [StudyActivityRecord] {
        activityRecords.filter { Calendar.current.isDate($0.date, inSameDayAs: Date()) }
    }

    private var todaySummary: ActivitySummary {
        ActivitySummary.make(from: todayRecords)
    }

    private var masteredTotal: Int {
        knowledgePoints.filter(\.isMastered).count
    }

    private var remainingToGoal: Int {
        max(0, dailyGoal - todaySummary.markedMasteredCount)
    }

    private var progressMessage: String {
        if todaySummary.markedMasteredCount >= dailyGoal {
            return "今日目标已经达成，保持这份节奏就很好。"
        }

        if todaySummary.reviewedAnswerCount > 0 {
            return "今日已经进入状态，还差 \(remainingToGoal) 个新增掌握达到目标。"
        }

        return "今天还很安静，可以从一个知识点开始。"
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.overviewScale
            let columnMaxWidth = metrics.overviewOuterMaxWidth
            let pagePadding = metrics.innerHorizontalPadding
            let heroMinHeight: CGFloat? = metrics.isPadPortrait ? 176 * scale : nil
            let metricMinHeight: CGFloat? = metrics.isPadPortrait ? 122 * scale : nil

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                ScrollView {
                    VStack(alignment: .leading, spacing: 18 * scale) {
                        VStack(alignment: .leading, spacing: 8 * scale) {
                            Text("今日概览")
                                .font(KikariaTypography.chineseTitle(size: 32 * scale))
                                .foregroundStyle(KikariaTheme.deepText)

                            KikariaTypography.mixedText(presetName, size: 15 * scale, weight: .medium)
                                .foregroundStyle(KikariaTheme.softText)
                        }
                        .padding(.top, 18 * scale + metrics.ipadPortraitOverviewTopInset)

                        VStack(alignment: .leading, spacing: 12 * scale) {
                            Text("今日新增已掌握")
                                .font(KikariaTypography.chineseHeadline(size: 15 * scale))
                                .foregroundStyle(KikariaTheme.softText)

                            HStack(alignment: .firstTextBaseline, spacing: 8 * scale) {
                                KikariaTypography.numericText("\(todaySummary.markedMasteredCount)", size: 58 * scale, weight: .bold)
                                    .monospacedDigit()
                                    .foregroundStyle(KikariaTheme.masteredDeepGreen)

                                KikariaTypography.numericText("/ \(dailyGoal)", size: 24 * scale, weight: .semibold)
                                    .foregroundStyle(KikariaTheme.softText)
                            }

                            KikariaTypography.mixedText(progressMessage, size: 15 * scale, weight: .medium)
                                .foregroundStyle(KikariaTheme.deepText.opacity(0.82))
                        }
                        .padding(22 * scale)
                        .frame(maxWidth: .infinity, minHeight: heroMinHeight, alignment: .leading)
                        .liquidGlassCard(cornerRadius: 30 * scale, material: .thinMaterial, fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.11, shadowRadius: 20 * scale, shadowY: 11 * scale)

                        LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 12 * scale) {
                            OverviewMetricCard(title: "查看答案", value: "\(todaySummary.reviewedAnswerCount)", scale: scale, minHeight: metricMinHeight)
                            OverviewMetricCard(title: "总已掌握", value: "\(masteredTotal)", scale: scale, minHeight: metricMinHeight)
                            OverviewMetricCard(title: "查看提示", value: "\(todaySummary.viewedHintCount)", scale: scale, minHeight: metricMinHeight)
                            OverviewMetricCard(title: "倒数", value: countdownText(for: countdownEndDate), scale: scale, minHeight: metricMinHeight)
                        }

                        Button(action: onOpenHistory) {
                            HStack(spacing: 12 * scale) {
                                Text("复习历史")
                                    .font(KikariaTypography.chineseHeadline(size: 18 * scale))
                                    .foregroundStyle(KikariaTheme.deepText)

                                Spacer()

                                Image(systemName: "calendar")
                                    .font(.system(size: 18 * scale, weight: .semibold))
                                    .foregroundStyle(KikariaTheme.sky)

                                Image(systemName: "chevron.right")
                                    .font(.system(size: 14 * scale, weight: .semibold))
                                    .foregroundStyle(KikariaTheme.blueGray)
                            }
                            .padding(.horizontal, 20 * scale)
                            .padding(.vertical, 19 * scale)
                            .liquidGlassCard(cornerRadius: 26 * scale, material: .thinMaterial, fillOpacity: 0.38, strokeOpacity: 0.38, shadowOpacity: 0.10, shadowRadius: 16 * scale, shadowY: 9 * scale)
                        }
                        .buttonStyle(.plain)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.bottom, 32)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)
                }
            }
            .kikariaAdaptiveNavigationChrome(metrics: metrics, outerMaxWidth: columnMaxWidth)
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
    }
}

private struct OverviewMetricCard: View {
    let title: String
    let value: String
    var scale: CGFloat = 1
    var minHeight: CGFloat? = nil

    var body: some View {
        let resolvedScale = max(scale, 1)

        VStack(alignment: .leading, spacing: 10 * resolvedScale) {
            Text(title)
                .font(KikariaTypography.chineseCaption(size: 13 * resolvedScale, weight: .semibold))
                .foregroundStyle(KikariaTheme.softText)

            KikariaTypography.mixedText(value, size: 46 * resolvedScale, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.deepText)
                .lineLimit(1)
                .minimumScaleFactor(0.58)
        }
        .padding(18 * resolvedScale)
        .frame(maxWidth: .infinity, minHeight: minHeight, alignment: .leading)
        .liquidGlassCard(cornerRadius: 24 * resolvedScale, material: .thinMaterial, fillOpacity: 0.34, strokeOpacity: 0.34, shadowOpacity: 0.08, shadowRadius: 14 * resolvedScale, shadowY: 8 * resolvedScale)
    }
}

private struct ReviewHistoryView: View {
    let activityRecords: [StudyActivityRecord]
    @State private var visibleMonth = Date()
    @State private var selectedDate = Date()

    private let columns = Array(repeating: GridItem(.flexible(), spacing: 8), count: 7)
    private let weekdaySymbols = ["一", "二", "三", "四", "五", "六", "日"]

    var body: some View {
        KikariaAdaptivePage { metrics in
            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                ScrollView {
                    VStack(alignment: .leading, spacing: 18) {
                        Text("复习历史")
                            .font(KikariaTypography.chineseTitle())
                            .foregroundStyle(KikariaTheme.deepText)
                            .padding(.top, 18)

                        VStack(spacing: 18) {
                            HStack {
                                Button {
                                    changeMonth(by: -1)
                                } label: {
                                    Image(systemName: "chevron.left")
                                        .font(.headline.weight(.semibold))
                                        .foregroundStyle(KikariaTheme.sky)
                                        .frame(width: 40, height: 40)
                                        .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                                }
                                .buttonStyle(.plain)

                                Spacer()

                                KikariaTypography.mixedText(monthTitle, size: 20, weight: .semibold)
                                    .foregroundStyle(KikariaTheme.deepText)

                                Spacer()

                                Button {
                                    changeMonth(by: 1)
                                } label: {
                                    Image(systemName: "chevron.right")
                                        .font(.headline.weight(.semibold))
                                        .foregroundStyle(KikariaTheme.sky)
                                        .frame(width: 40, height: 40)
                                        .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                                }
                                .buttonStyle(.plain)
                            }

                            LazyVGrid(columns: columns, spacing: 8) {
                                ForEach(weekdaySymbols, id: \.self) { symbol in
                                    Text(symbol)
                                        .font(KikariaTypography.chineseCaption(size: 12, weight: .semibold))
                                        .foregroundStyle(KikariaTheme.softText)
                                        .frame(maxWidth: .infinity)
                                }

                                ForEach(Array(monthCells.enumerated()), id: \.offset) { _, date in
                                    HistoryCalendarDayCell(
                                        date: date,
                                        count: date.map(recordCount(on:)) ?? 0,
                                        isToday: date.map { Calendar.current.isDateInToday($0) } ?? false,
                                        isSelected: date.map { Calendar.current.isDate($0, inSameDayAs: selectedDate) } ?? false
                                    ) {
                                        if let date {
                                            selectedDate = date
                                        }
                                    }
                                }
                            }
                        }
                        .padding(18)
                        .liquidGlassCard(cornerRadius: 30, fillOpacity: 0.44, strokeOpacity: 0.42, shadowOpacity: 0.12, shadowRadius: 20, shadowY: 12)

                        HistoryDaySummaryCard(date: selectedDate, summary: ActivitySummary.make(from: records(on: selectedDate)))
                    }
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.bottom, 32)
                    .frame(maxWidth: metrics.mainMaxWidth)
                    .frame(maxWidth: .infinity)
                }
            }
            .kikariaAdaptiveNavigationChrome(metrics: metrics, outerMaxWidth: metrics.mainMaxWidth)
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
    }

    private var monthTitle: String {
        let components = Calendar.current.dateComponents([.year, .month], from: visibleMonth)
        return "\(components.year ?? 0)年 \(components.month ?? 1)月"
    }

    private var monthCells: [Date?] {
        let calendar = Calendar.current
        let components = calendar.dateComponents([.year, .month], from: visibleMonth)
        guard let monthStart = calendar.date(from: components),
              let range = calendar.range(of: .day, in: .month, for: monthStart)
        else {
            return []
        }

        let firstWeekday = calendar.component(.weekday, from: monthStart)
        let leadingBlankCount = (firstWeekday + 5) % 7
        var cells = Array<Date?>(repeating: nil, count: leadingBlankCount)

        for day in range {
            cells.append(calendar.date(byAdding: .day, value: day - 1, to: monthStart))
        }

        while cells.count % 7 != 0 {
            cells.append(nil)
        }

        return cells
    }

    private func changeMonth(by offset: Int) {
        visibleMonth = Calendar.current.date(byAdding: .month, value: offset, to: visibleMonth) ?? visibleMonth
    }

    private func records(on date: Date) -> [StudyActivityRecord] {
        activityRecords.filter { Calendar.current.isDate($0.date, inSameDayAs: date) }
    }

    private func recordCount(on date: Date) -> Int {
        records(on: date).count
    }
}

private struct HistoryCalendarDayCell: View {
    let date: Date?
    let count: Int
    let isToday: Bool
    let isSelected: Bool
    let action: () -> Void

    private var fillColor: Color {
        switch count {
        case 0:
            return .white.opacity(0.42)
        case 1...2:
            return KikariaTheme.cyan.opacity(0.42)
        case 3...5:
            return KikariaTheme.sky.opacity(0.54)
        default:
            return KikariaTheme.masteredGreen.opacity(0.62)
        }
    }

    var body: some View {
        Button(action: action) {
            ZStack {
                RoundedRectangle(cornerRadius: 12, style: .continuous)
                    .fill(date == nil ? Color.clear : fillColor)
                    .overlay {
                        RoundedRectangle(cornerRadius: 12, style: .continuous)
                            .stroke(
                                isSelected ? KikariaTheme.deepText.opacity(0.45) : (isToday ? KikariaTheme.sky.opacity(0.65) : .clear),
                                lineWidth: isSelected ? 2 : 1.4
                            )
                    }

                if let date {
                    KikariaTypography.numericText("\(Calendar.current.component(.day, from: date))", size: 12, weight: .semibold)
                        .foregroundStyle(KikariaTheme.deepText.opacity(count == 0 ? 0.58 : 0.86))
                }
            }
            .frame(height: 38)
        }
        .buttonStyle(.plain)
        .disabled(date == nil)
    }
}

private struct HistoryDaySummaryCard: View {
    let date: Date
    let summary: ActivitySummary

    private var title: String {
        let components = Calendar.current.dateComponents([.month, .day], from: date)
        return "\(components.month ?? 1)月\(components.day ?? 1)日"
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack {
                KikariaTypography.mixedText(title, size: 19, weight: .semibold)
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer()

                KikariaTypography.mixedText("\(summary.totalCount) 条记录", size: 12, weight: .semibold)
                    .foregroundStyle(KikariaTheme.softText)
                    .padding(.horizontal, 11)
                    .padding(.vertical, 6)
                    .liquidGlassCapsule(fillOpacity: 0.36, strokeOpacity: 0.34, shadowOpacity: 0.04, shadowRadius: 6, shadowY: 3)
            }

            if summary.totalCount == 0 {
                Text("这一天还没有学习记录。")
                    .font(KikariaTypography.chineseBody(size: 15, weight: .medium))
                    .foregroundStyle(KikariaTheme.softText)
                    .frame(maxWidth: .infinity, alignment: .leading)
            } else {
                VStack(spacing: 9) {
                    HistorySummaryRow(title: "查看提示", count: summary.viewedHintCount)
                    HistorySummaryRow(title: "查看答案", count: summary.reviewedAnswerCount)
                    HistorySummaryRow(title: "新增掌握", count: summary.markedMasteredCount)
                    HistorySummaryRow(title: "加入重点", count: summary.addedReinforcementCount)
                }
            }
        }
        .padding(20)
        .liquidGlassCard(cornerRadius: 28, fillOpacity: 0.44, strokeOpacity: 0.40, shadowOpacity: 0.10, shadowRadius: 18, shadowY: 10)
    }
}

private struct HistorySummaryRow: View {
    let title: String
    let count: Int

    var body: some View {
        HStack {
            Text(title)
                .font(KikariaTypography.chineseBody(size: 15, weight: .medium))
                .foregroundStyle(KikariaTheme.deepText)

            Spacer()

            KikariaTypography.numericText("\(count)", size: 17, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.sky)
        }
    }
}

private struct ProfileAvatarView: View {
    let systemName: String
    let imageData: Data?
    let size: CGFloat

    var body: some View {
        Group {
            #if os(iOS)
            if let imageData, let platformImage = UIImage(data: imageData) {
                Image(uiImage: platformImage)
                    .resizable()
                    .scaledToFill()
                    .frame(width: size, height: size)
                    .clipShape(Circle())
                    .overlay {
                        Circle()
                            .stroke(.white.opacity(0.42), lineWidth: 1)
                    }
            } else {
                Image(systemName: systemName)
                    .font(.system(size: size))
                    .foregroundStyle(KikariaTheme.sky, .white.opacity(0.85))
                    .frame(width: size, height: size)
            }
            #elseif os(macOS)
            if let imageData, let platformImage = NSImage(data: imageData) {
                Image(nsImage: platformImage)
                    .resizable()
                    .scaledToFill()
                    .frame(width: size, height: size)
                    .clipShape(Circle())
                    .overlay {
                        Circle()
                            .stroke(.white.opacity(0.42), lineWidth: 1)
                    }
            } else {
                Image(systemName: systemName)
                    .font(.system(size: size))
                    .foregroundStyle(KikariaTheme.sky, .white.opacity(0.85))
                    .frame(width: size, height: size)
            }
            #endif
        }
        .padding(size <= 48 ? 3 : 5)
        .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.50, shadowOpacity: 0.16, shadowRadius: 12, shadowY: 6)
    }
}

#if os(iOS)
@available(iOS 16, *)
private func loadCompressedAvatarData(from selectedPhotoItem: PhotosPickerItem) async -> Data? {
    guard let data = try? await selectedPhotoItem.loadTransferable(type: Data.self) else {
        return nil
    }

    return compressedAvatarData(from: data)
}

private func compressedAvatarData(from data: Data) -> Data? {
    guard let image = UIImage(data: data) else {
        return nil
    }

    let maxDimension: CGFloat = 512
    let largestSide = max(image.size.width, image.size.height)
    let scale = largestSide > 0 ? min(1, maxDimension / largestSide) : 1

    let outputImage: UIImage
    if scale < 1 {
        let targetSize = CGSize(
            width: image.size.width * scale,
            height: image.size.height * scale
        )
        outputImage = UIGraphicsImageRenderer(size: targetSize).image { _ in
            image.draw(in: CGRect(origin: .zero, size: targetSize))
        }
    } else {
        outputImage = image
    }

    return outputImage.jpegData(compressionQuality: 0.82) ?? outputImage.pngData()
}

private struct LegacyImagePicker: UIViewControllerRepresentable {
    let onPickImageData: (Data?) -> Void

    func makeUIViewController(context: Context) -> UIImagePickerController {
        let picker = UIImagePickerController()
        picker.sourceType = .photoLibrary
        picker.mediaTypes = ["public.image"]
        picker.allowsEditing = false
        picker.delegate = context.coordinator
        return picker
    }

    func updateUIViewController(_ uiViewController: UIImagePickerController, context: Context) {}

    func makeCoordinator() -> Coordinator {
        Coordinator(onPickImageData: onPickImageData)
    }

    final class Coordinator: NSObject, UINavigationControllerDelegate, UIImagePickerControllerDelegate {
        let onPickImageData: (Data?) -> Void

        init(onPickImageData: @escaping (Data?) -> Void) {
            self.onPickImageData = onPickImageData
            super.init()
        }

        func imagePickerControllerDidCancel(_ picker: UIImagePickerController) {
            picker.dismiss(animated: true)
            onPickImageData(nil)
        }

        func imagePickerController(
            _ picker: UIImagePickerController,
            didFinishPickingMediaWithInfo info: [UIImagePickerController.InfoKey: Any]
        ) {
            picker.dismiss(animated: true)

            guard let image = info[.originalImage] as? UIImage,
                  let sourceData = image.pngData() ?? image.jpegData(compressionQuality: 0.9),
                  let data = compressedAvatarData(from: sourceData) else {
                onPickImageData(nil)
                return
            }

            onPickImageData(data)
        }
    }
}

private struct PhotoPickerCompat<Label: View>: View {
    let onPickImageData: (Data?) -> Void
    @ViewBuilder let label: () -> Label
    @available(iOS 16, *)
    @State private var selectedPhotoItem: PhotosPickerItem?
    @State private var isShowingLegacyPicker = false

    var body: some View {
        Group {
            if #available(iOS 16, *) {
                PhotosPicker(selection: $selectedPhotoItem, matching: .images) {
                    label()
                }
                .buttonStyle(.plain)
                .onChange(of: selectedPhotoItem) { item in
                    guard let selectedPhotoItem = item else {
                        return
                    }

                    Task {
                        guard let compressedData = await loadCompressedAvatarData(from: selectedPhotoItem) else {
                            await MainActor.run {
                                onPickImageData(nil)
                            }
                            return
                        }

                        await MainActor.run {
                            onPickImageData(compressedData)
                        }
                    }
                }
            } else {
                Button(action: {
                    isShowingLegacyPicker = true
                }) {
                    label()
                }
                .buttonStyle(.plain)
                .sheet(isPresented: $isShowingLegacyPicker) {
                    LegacyImagePicker { data in
                        isShowingLegacyPicker = false
                        onPickImageData(data)
                    }
                }
            }
        }
    }
}
#elseif os(macOS)
private func compressedAvatarData(from data: Data) -> Data? {
    data
}
#endif

private struct SettingsView: View {
    let profile: UserProfile
    @Binding var dailyGoal: Int
    @Binding var countdownStartDate: Date?
    @Binding var countdownEndDate: Date?
    let notificationsEnabled: Bool
    @Binding var notificationTime: Date
    @Binding var dangerPercent: Int
    let currentPresetName: String
    let onClose: () -> Void
    let onEditProfile: () -> Void
    let onOpenOnboarding: () -> Void
    let onOpenMarkdownGuide: () -> Void
    let onSetNotificationsEnabled: (Bool, @escaping (Bool, String?) -> Void) -> Void
    let onSendTestNotification: (@escaping (String) -> Void) -> Void
    @State private var isShowingDailyGoalPicker = false
    @State private var isShowingCountdownPicker = false
    @State private var isShowingDangerPicker = false
    @State private var isShowingNotificationTimePicker = false
    @State private var countdownDraftStartDate = Date()
    @State private var countdownDraftEndDate = Date()
    @State private var countdownErrorMessage: String?
    @State private var toastMessage: String?
    @State private var toastToken = UUID()
    @State private var isShowingPrivacyPolicy = false

    private var versionText: String {
        let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0"
        let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "1"
        return "\(version) (\(build))"
    }

    private var notificationTimeText: String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: notificationTime)
    }

    private func settingsCloseButton(metrics: KikariaAdaptiveLayout.Metrics, scale: CGFloat) -> some View {
        Button(action: onClose) {
            Image(systemName: "xmark")
                .font(.system(size: metrics.isPadPortrait ? 17 * scale : 17, weight: .semibold))
                .foregroundStyle(KikariaTheme.deepText)
                .frame(
                    width: metrics.isPadPortrait ? 46 * scale : (metrics.isPadWidth ? 46 : 42),
                    height: metrics.isPadPortrait ? 46 * scale : (metrics.isPadWidth ? 46 : 42)
                )
                .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.10, shadowRadius: 12 * scale, shadowY: 6 * scale)
        }
        .buttonStyle(.plain)
    }

    private func settingsProfileSummary(metrics: KikariaAdaptiveLayout.Metrics, scale: CGFloat) -> some View {
        VStack(spacing: 12 * scale) {
            ProfileAvatarView(
                systemName: profile.avatarSystemName,
                imageData: profile.avatarImageData,
                size: metrics.isPadPortrait ? 112 : (metrics.isPadWidth ? 98 : 86)
            )

            VStack(spacing: 4 * scale) {
                KikariaTypography.mixedText(profile.displayName, size: metrics.isPadPortrait ? 31 * scale : (metrics.isPadWidth ? 30 : 28), weight: .semibold)
                    .foregroundStyle(KikariaTheme.deepText)

                KikariaTypography.mixedText("@\(profile.userHandle)", size: metrics.isPadPortrait ? 16 * scale : (metrics.isPadWidth ? 16 : 15), weight: .medium)
                    .foregroundStyle(KikariaTheme.softText)
            }

            Button(action: onEditProfile) {
                Text("编辑个人资料")
                    .font(KikariaTypography.chineseButton(size: metrics.isPadPortrait ? 17 * scale : (metrics.isPadWidth ? 17 : 16)))
                    .foregroundStyle(KikariaTheme.deepText)
                    .padding(.horizontal, metrics.isPadPortrait ? 28 * scale : (metrics.isPadWidth ? 28 : 24))
                    .padding(.vertical, metrics.isPadPortrait ? 14 * scale : (metrics.isPadWidth ? 14 : 13))
                    .liquidGlassCapsule(fillOpacity: 0.36, strokeOpacity: 0.40, shadowOpacity: 0.08, shadowRadius: 13 * scale, shadowY: 7 * scale)
            }
            .buttonStyle(.plain)
            .padding(.top, 4 * scale)
        }
        .frame(maxWidth: .infinity)
    }

    private func currentPresetOnlySection(scale: CGFloat, rowScale: CGFloat) -> some View {
        SettingsSectionCard(title: "当前预设", scale: scale) {
            SettingsListRow(
                title: "当前预设",
                valueText: currentPresetName,
                showsChevron: false,
                scale: rowScale
            )
        }
    }

    private func learningSettingsSection(scale: CGFloat, rowScale: CGFloat) -> some View {
        SettingsSectionCard(title: "学习", scale: scale) {
            SettingsListRow(
                title: "每日学习目标",
                valueText: "\(dailyGoal)",
                scale: rowScale
            ) {
                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                    isShowingCountdownPicker = false
                    isShowingDangerPicker = false
                    isShowingNotificationTimePicker = false
                    isShowingDailyGoalPicker.toggle()
                }
            }

            SettingsSectionDivider(scale: scale)

            SettingsListRow(
                title: "倒数日",
                valueText: countdownEndDate.map { "\(countdownDays(until: $0) ?? 0)天" } ?? "未设置",
                scale: rowScale
            ) {
                prepareCountdownDraft()
                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                    isShowingDailyGoalPicker = false
                    isShowingDangerPicker = false
                    isShowingNotificationTimePicker = false
                    isShowingCountdownPicker.toggle()
                }
            }

            SettingsSectionDivider(scale: scale)

            SettingsListRow(
                title: "进度安全线",
                valueText: "\(dangerPercent)%",
                scale: rowScale
            ) {
                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                    isShowingDailyGoalPicker = false
                    isShowingCountdownPicker = false
                    isShowingNotificationTimePicker = false
                    isShowingDangerPicker.toggle()
                }
            }
        }
    }

    private func notificationSettingsSection(scale: CGFloat, rowScale: CGFloat) -> some View {
        SettingsSectionCard(title: "通知", scale: scale) {
            SettingsToggleRow(
                title: "学习进度通知",
                isOn: notificationsEnabled,
                scale: rowScale
            ) { newValue in
                if !newValue {
                    withAnimation(.easeOut(duration: 0.18)) {
                        isShowingNotificationTimePicker = false
                    }
                }

                onSetNotificationsEnabled(newValue) { _, message in
                    if let message {
                        showToast(message)
                    }
                }
            }

            if notificationsEnabled {
                SettingsSectionDivider(scale: scale)

                SettingsListRow(
                    title: "通知时间",
                    valueText: notificationTimeText,
                    scale: rowScale
                ) {
                    withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                        isShowingDailyGoalPicker = false
                        isShowingCountdownPicker = false
                        isShowingDangerPicker = false
                        isShowingNotificationTimePicker.toggle()
                    }
                }

                if countdownStartDate == nil || countdownEndDate == nil {
                    Text("需设置倒数日")
                        .font(KikariaTypography.chineseCaption(size: 12 * scale, weight: .medium))
                        .foregroundStyle(KikariaTheme.softText)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .padding(.horizontal, 18 * scale)
                        .padding(.bottom, 10 * scale)
                }

                #if DEBUG
                SettingsSectionDivider(scale: scale)

                Button {
                    onSendTestNotification { message in
                        showToast(message)
                    }
                } label: {
                    SettingsRowContent(title: "预览提醒", valueText: "", scale: rowScale)
                }
                .buttonStyle(.plain)
                #endif
            }
        }
    }

    private func helpSettingsSection(scale: CGFloat, rowScale: CGFloat) -> some View {
        SettingsSectionCard(title: "帮助", scale: scale) {
            SettingsListRow(
                title: "新手引导",
                valueText: "",
                scale: rowScale
            ) {
                onOpenOnboarding()
            }

            SettingsSectionDivider(scale: scale)

            SettingsListRow(
                title: "Markdown 格式",
                valueText: "",
                scale: rowScale
            ) {
                onOpenMarkdownGuide()
            }
        }
    }

    private func aboutSettingsSection(scale: CGFloat, rowScale: CGFloat) -> some View {
        SettingsSectionCard(title: "关于", scale: scale) {
            SettingsListRow(
                title: "隐私政策",
                valueText: "",
                scale: rowScale
            ) {
                isShowingPrivacyPolicy = true
            }

            SettingsSectionDivider(scale: scale)

            SettingsListRow(
                title: "版权声明",
                valueText: "© 2026 Vita",
                showsChevron: false,
                scale: rowScale
            )

            SettingsSectionDivider(scale: scale)

            SettingsListRow(
                title: "版本",
                valueText: versionText,
                showsChevron: false,
                scale: rowScale
            )

            SettingsSectionDivider(scale: scale)

            SettingsInfoTextRow(text: "浙ICP备2026034004号", scale: rowScale)
        }
    }

    private func settingsLandscapeContent(
        metrics: KikariaAdaptiveLayout.Metrics,
        scale: CGFloat,
        rowScale: CGFloat
    ) -> some View {
        ZStack(alignment: .topTrailing) {
            VStack(alignment: .leading, spacing: 26 * scale) {
                Text("设置")
                    .font(KikariaTypography.chineseTitle(size: metrics.isPadWidth ? 32 : 30))
                    .foregroundStyle(KikariaTheme.deepText)
                    .padding(.top, 18 * scale)
                    .padding(.trailing, 72)

                HStack(alignment: .center, spacing: metrics.settingsLandscapeColumnSpacing) {
                    settingsProfileSummary(metrics: metrics, scale: scale)
                        .frame(width: metrics.settingsLandscapeLeftWidth)
                        .frame(maxHeight: .infinity, alignment: .center)

                    ScrollView {
                        VStack(spacing: 20 * scale) {
                            currentPresetOnlySection(scale: scale, rowScale: rowScale)
                            learningSettingsSection(scale: scale, rowScale: rowScale)
                            notificationSettingsSection(scale: scale, rowScale: rowScale)
                            helpSettingsSection(scale: scale, rowScale: rowScale)
                            aboutSettingsSection(scale: scale, rowScale: rowScale)
                        }
                        .padding(.bottom, 34 * scale)
                        .frame(width: metrics.settingsLandscapeRightWidth)
                    }
                    .kikariaScrollIndicators(hidden: true)
                }
                .frame(maxHeight: .infinity, alignment: .center)
            }
            .frame(maxWidth: metrics.settingsLandscapeMaxWidth)
            .padding(.horizontal, metrics.horizontalPadding)
            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)

            settingsCloseButton(metrics: metrics, scale: scale)
                .padding(.top, 18 * scale)
                .padding(.trailing, metrics.horizontalPadding)
        }
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.settingsScale
            let rowScale = metrics.settingsRowScale
            let columnMaxWidth = metrics.settingsOuterMaxWidth
            let pagePadding = metrics.innerHorizontalPadding

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                if metrics.settingsUsesTwoColumnLayout {
                    settingsLandscapeContent(metrics: metrics, scale: scale, rowScale: rowScale)
                } else {
                    VStack(spacing: 0) {
                    HStack {
                        Text("设置")
                            .font(KikariaTypography.chineseTitle(size: metrics.isPadPortrait ? 34 * scale : (metrics.isPadWidth ? 32 : 30)))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()

                        Button(action: onClose) {
                            Image(systemName: "xmark")
                                .font(.system(size: metrics.isPadPortrait ? 17 * scale : 17, weight: .semibold))
                                .foregroundStyle(KikariaTheme.deepText)
                                .frame(width: metrics.isPadPortrait ? 46 * scale : (metrics.isPadWidth ? 46 : 42), height: metrics.isPadPortrait ? 46 * scale : (metrics.isPadWidth ? 46 : 42))
                                .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.10, shadowRadius: 12 * scale, shadowY: 6 * scale)
                        }
                        .buttonStyle(.plain)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.top, 18 * scale + metrics.ipadPortraitSettingsTopInset)
                    .padding(.bottom, 18 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)

                    ScrollView {
                        VStack(spacing: 22 * scale) {
                        VStack(spacing: 12 * scale) {
                            ProfileAvatarView(
                                systemName: profile.avatarSystemName,
                                imageData: profile.avatarImageData,
                                size: metrics.isPadPortrait ? 112 : (metrics.isPadWidth ? 98 : 86)
                            )

                            VStack(spacing: 4 * scale) {
                                KikariaTypography.mixedText(profile.displayName, size: metrics.isPadPortrait ? 31 * scale : (metrics.isPadWidth ? 30 : 28), weight: .semibold)
                                    .foregroundStyle(KikariaTheme.deepText)

                                KikariaTypography.mixedText("@\(profile.userHandle)", size: metrics.isPadPortrait ? 16 * scale : (metrics.isPadWidth ? 16 : 15), weight: .medium)
                                    .foregroundStyle(KikariaTheme.softText)
                            }

                            Button(action: onEditProfile) {
                                Text("编辑个人资料")
                                    .font(KikariaTypography.chineseButton(size: metrics.isPadPortrait ? 17 * scale : (metrics.isPadWidth ? 17 : 16)))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .padding(.horizontal, metrics.isPadPortrait ? 28 * scale : (metrics.isPadWidth ? 28 : 24))
                                    .padding(.vertical, metrics.isPadPortrait ? 14 * scale : (metrics.isPadWidth ? 14 : 13))
                                    .liquidGlassCapsule(fillOpacity: 0.36, strokeOpacity: 0.40, shadowOpacity: 0.08, shadowRadius: 13 * scale, shadowY: 7 * scale)
                            }
                            .buttonStyle(.plain)
                            .padding(.top, 4 * scale)
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.top, 8 * scale)
                        .padding(.bottom, 6 * scale)

                        SettingsSectionCard(title: "当前预设", scale: scale) {
                            SettingsListRow(
                                title: "当前预设",
                                valueText: currentPresetName,
                                showsChevron: false,
                                scale: rowScale
                            )

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "每日学习目标",
                                valueText: "\(dailyGoal)",
                                scale: rowScale
                            ) {
                                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                                    isShowingCountdownPicker = false
                                    isShowingDangerPicker = false
                                    isShowingNotificationTimePicker = false
                                    isShowingDailyGoalPicker.toggle()
                                }
                            }

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "倒数日",
                                valueText: countdownEndDate.map { "\(countdownDays(until: $0) ?? 0)天" } ?? "未设置",
                                scale: rowScale
                            ) {
                                prepareCountdownDraft()
                                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                                    isShowingDailyGoalPicker = false
                                    isShowingDangerPicker = false
                                    isShowingNotificationTimePicker = false
                                    isShowingCountdownPicker.toggle()
                                }
                            }

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "进度安全线",
                                valueText: "\(dangerPercent)%",
                                scale: rowScale
                            ) {
                                withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                                    isShowingDailyGoalPicker = false
                                    isShowingCountdownPicker = false
                                    isShowingNotificationTimePicker = false
                                    isShowingDangerPicker.toggle()
                                }
                            }
                        }

                        SettingsSectionCard(title: "通知", scale: scale) {
                            SettingsToggleRow(
                                title: "学习进度通知",
                                isOn: notificationsEnabled,
                                scale: rowScale
                            ) { newValue in
                                if !newValue {
                                    withAnimation(.easeOut(duration: 0.18)) {
                                        isShowingNotificationTimePicker = false
                                    }
                                }

                                onSetNotificationsEnabled(newValue) { _, message in
                                    if let message {
                                        showToast(message)
                                    }
                                }
                            }

                            if notificationsEnabled {
                                SettingsSectionDivider(scale: scale)

                                SettingsListRow(
                                    title: "通知时间",
                                    valueText: notificationTimeText,
                                    scale: rowScale
                                ) {
                                    withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                                        isShowingDailyGoalPicker = false
                                        isShowingCountdownPicker = false
                                        isShowingDangerPicker = false
                                        isShowingNotificationTimePicker.toggle()
                                    }
                                }

                                if countdownStartDate == nil || countdownEndDate == nil {
                                    Text("需设置倒数日")
                                        .font(KikariaTypography.chineseCaption(size: 12 * scale, weight: .medium))
                                        .foregroundStyle(KikariaTheme.softText)
                                        .frame(maxWidth: .infinity, alignment: .leading)
                                        .padding(.horizontal, 18 * scale)
                                        .padding(.bottom, 10 * scale)
                                }

                                #if DEBUG
                                SettingsSectionDivider(scale: scale)

                                Button {
                                    onSendTestNotification { message in
                                        showToast(message)
                                    }
                                } label: {
                                    SettingsRowContent(title: "预览提醒", valueText: "", scale: rowScale)
                                }
                                .buttonStyle(.plain)
                                #endif
                            }
                        }

                        SettingsSectionCard(title: "帮助", scale: scale) {
                            SettingsListRow(
                                title: "新手引导",
                                valueText: "",
                                scale: rowScale
                            ) {
                                onOpenOnboarding()
                            }

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "Markdown 格式",
                                valueText: "",
                                scale: rowScale
                            ) {
                                onOpenMarkdownGuide()
                            }
                        }

                        SettingsSectionCard(title: "关于", scale: scale) {
                            SettingsListRow(
                                title: "隐私政策",
                                valueText: "",
                                scale: rowScale
                            ) {
                                isShowingPrivacyPolicy = true
                            }

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "版权声明",
                                valueText: "© 2026 Vita",
                                showsChevron: false,
                                scale: rowScale
                            )

                            SettingsSectionDivider(scale: scale)

                            SettingsListRow(
                                title: "版本",
                                valueText: versionText,
                                showsChevron: false,
                                scale: rowScale
                            )

                            SettingsSectionDivider(scale: scale)

                            SettingsInfoTextRow(text: "浙ICP备2026034004号", scale: rowScale)
                        }
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.bottom, 34 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)
                    }
                }
            }

            if isShowingDailyGoalPicker {
                Color.black.opacity(0.001)
                    .ignoresSafeArea()
                    .onTapGesture {
                        withAnimation(.easeOut(duration: 0.18)) {
                            isShowingDailyGoalPicker = false
                        }
                    }
                    .transition(.opacity)

                VStack {
                    if metrics.isPadWidth {
                        Spacer()
                    } else {
                        Spacer()
                            .frame(height: 352)
                    }

                    DailyGoalPickerBubble(dailyGoal: $dailyGoal) {
                        withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                            isShowingDailyGoalPicker = false
                        }
                    }
                    .padding(.horizontal, 34)
                    .transition(.scale(scale: 0.94, anchor: .topTrailing).combined(with: .opacity))

                    Spacer()
                }
            }

            if isShowingCountdownPicker {
                Color.black.opacity(0.001)
                    .ignoresSafeArea()
                    .onTapGesture {
                        withAnimation(.easeOut(duration: 0.18)) {
                            isShowingCountdownPicker = false
                        }
                    }
                    .transition(.opacity)

                VStack {
                    if metrics.isPadWidth {
                        Spacer()
                    } else {
                        Spacer()
                            .frame(height: 332)
                    }

                    CountdownDateRangePickerBubble(
                        startDate: $countdownDraftStartDate,
                        endDate: $countdownDraftEndDate,
                        isConfigured: countdownEndDate != nil,
                        errorMessage: countdownErrorMessage
                    ) {
                        countdownStartDate = nil
                        countdownEndDate = nil
                        countdownErrorMessage = nil
                        withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                            isShowingCountdownPicker = false
                        }
                    } onDone: {
                        guard Calendar.current.startOfDay(for: countdownDraftEndDate) >= Calendar.current.startOfDay(for: countdownDraftStartDate) else {
                            withAnimation(.easeInOut(duration: 0.2)) {
                                countdownErrorMessage = "结束日期不能早于开始日期。"
                            }
                            showToast("结束日期不能早于开始日期")
                            return
                        }

                        countdownStartDate = countdownDraftStartDate
                        countdownEndDate = countdownDraftEndDate
                        countdownErrorMessage = nil

                        withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                            isShowingCountdownPicker = false
                        }
                    }
                    .padding(.horizontal, 34)
                    .transition(.scale(scale: 0.94, anchor: .topTrailing).combined(with: .opacity))

                    Spacer()
                }
            }

            if isShowingDangerPicker {
                Color.black.opacity(0.001)
                    .ignoresSafeArea()
                    .onTapGesture {
                        withAnimation(.easeOut(duration: 0.18)) {
                            isShowingDangerPicker = false
                        }
                    }
                    .transition(.opacity)

                VStack {
                    if metrics.isPadWidth {
                        Spacer()
                    } else {
                        Spacer()
                            .frame(height: 492)
                    }

                    DangerPercentPickerBubble(dangerPercent: $dangerPercent) {
                        withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                            isShowingDangerPicker = false
                        }
                    }
                    .padding(.horizontal, 34)
                    .transition(.scale(scale: 0.94, anchor: .topTrailing).combined(with: .opacity))

                    Spacer()
                }
            }

            if isShowingNotificationTimePicker && notificationsEnabled {
                Color.black.opacity(0.001)
                    .ignoresSafeArea()
                    .onTapGesture {
                        withAnimation(.easeOut(duration: 0.18)) {
                            isShowingNotificationTimePicker = false
                        }
                    }
                    .transition(.opacity)

                VStack {
                    if metrics.isPadWidth {
                        Spacer()
                    } else {
                        Spacer()
                            .frame(height: 456)
                    }

                    NotificationTimePickerBubble(notificationTime: $notificationTime) {
                        withAnimation(.spring(response: 0.32, dampingFraction: 0.88)) {
                            isShowingNotificationTimePicker = false
                        }
                    }
                    .padding(.horizontal, 34)
                    .transition(.scale(scale: 0.94, anchor: .topTrailing).combined(with: .opacity))

                    Spacer()
                }
            }

            if let toastMessage {
                KikariaToastLayer(message: toastMessage)
                    .transition(.move(edge: .top).combined(with: .opacity))
            }
        }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
        .alert("隐私政策", isPresented: $isShowingPrivacyPolicy) {
            Button("知道了", role: .cancel) {}
        } message: {
            Text("Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。学习进度通知使用 iOS 本地通知，不会上传到服务器。")
        }
    }

    private func prepareCountdownDraft() {
        let today = Date()
        countdownDraftStartDate = countdownStartDate ?? today
        countdownDraftEndDate = countdownEndDate ?? countdownStartDate ?? today
        countdownErrorMessage = nil
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }

}

private struct SettingsSectionCard<Content: View>: View {
    let title: String
    var scale: CGFloat = 1
    let content: Content

    init(title: String, scale: CGFloat = 1, @ViewBuilder content: () -> Content) {
        self.title = title
        self.scale = scale
        self.content = content()
    }

    var body: some View {
        let resolvedScale = max(scale, 1)

        VStack(alignment: .leading, spacing: 8 * resolvedScale) {
            KikariaTypography.mixedText(title, size: 13 * resolvedScale, weight: .semibold)
                .foregroundStyle(KikariaTheme.softText)
                .padding(.horizontal, 4 * resolvedScale)

            VStack(spacing: 0) {
                content
            }
            .liquidGlassCard(cornerRadius: 28 * resolvedScale, material: .thinMaterial, fillOpacity: 0.32, strokeOpacity: 0.34, shadowOpacity: 0.08, shadowRadius: 15 * resolvedScale, shadowY: 8 * resolvedScale)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

private struct SettingsSectionDivider: View {
    var scale: CGFloat = 1

    var body: some View {
        let resolvedScale = max(scale, 1)

        Rectangle()
            .fill(KikariaTheme.blueGray.opacity(0.10))
            .frame(height: 1)
            .padding(.leading, 18 * resolvedScale)
    }
}

private struct SettingsRowContent: View {
    let title: String
    let valueText: String
    var showsChevron = true
    var scale: CGFloat = 1

    var body: some View {
        let resolvedScale = max(scale, 1)

        HStack(spacing: 14 * resolvedScale) {
            KikariaTypography.mixedText(title, size: 16 * resolvedScale, weight: .semibold)
                .foregroundStyle(KikariaTheme.deepText)
                .lineLimit(1)
                .minimumScaleFactor(0.84)

            Spacer(minLength: 12 * resolvedScale)

            if !valueText.isEmpty {
                KikariaTypography.mixedText(valueText, size: 16 * resolvedScale, weight: .semibold)
                    .foregroundStyle(showsChevron ? KikariaTheme.sky : KikariaTheme.softText)
                    .monospacedDigit()
                    .lineLimit(1)
                    .minimumScaleFactor(0.72)
            }

            if showsChevron {
                Image(systemName: "chevron.right")
                    .font(.system(size: 13 * resolvedScale, weight: .semibold))
                    .foregroundStyle(KikariaTheme.blueGray)
            }
        }
        .padding(.horizontal, 18 * resolvedScale)
        .frame(maxWidth: .infinity, minHeight: 58 * resolvedScale)
        .contentShape(Rectangle())
    }
}

private struct SettingsListRow: View {
    let title: String
    let valueText: String
    var showsChevron = true
    var scale: CGFloat = 1
    var action: (() -> Void)? = nil

    var body: some View {
        Group {
            if let action {
                Button(action: action) {
                    SettingsRowContent(title: title, valueText: valueText, showsChevron: showsChevron, scale: scale)
                }
                .buttonStyle(.plain)
            } else {
                SettingsRowContent(title: title, valueText: valueText, showsChevron: false, scale: scale)
            }
        }
    }
}

private struct SettingsInfoTextRow: View {
    let text: String
    var scale: CGFloat = 1

    var body: some View {
        let resolvedScale = max(scale, 1)

        KikariaTypography.mixedText(text, size: 16 * resolvedScale, weight: .semibold)
            .foregroundStyle(KikariaTheme.softText)
            .lineLimit(1)
            .minimumScaleFactor(0.72)
            .padding(.horizontal, 18 * resolvedScale)
            .frame(maxWidth: .infinity, minHeight: 58 * resolvedScale, alignment: .trailing)
    }
}

private struct SettingsToggleRow: View {
    let title: String
    let isOn: Bool
    var scale: CGFloat = 1
    let onChange: (Bool) -> Void

    var body: some View {
        let resolvedScale = max(scale, 1)

        HStack(spacing: 14 * resolvedScale) {
            KikariaTypography.mixedText(title, size: 17 * resolvedScale, weight: .semibold)
                .foregroundStyle(KikariaTheme.deepText)

            Spacer()

            Toggle(
                title,
                isOn: Binding(
                    get: { isOn },
                    set: { onChange($0) }
                )
            )
            .labelsHidden()
            .tint(KikariaTheme.sky)
        }
        .padding(.horizontal, 18 * resolvedScale)
        .frame(maxWidth: .infinity, minHeight: 58 * resolvedScale)
    }
}

private struct SettingsOptionRow: View {
    let title: String
    let subtitle: String
    let systemImage: String
    var valueText: String? = nil
    var showsChevron = true
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 14) {
                Image(systemName: systemImage)
                    .font(.title3.weight(.semibold))
                    .foregroundStyle(KikariaTheme.sky)
                    .frame(width: 38, height: 38)
                    .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.34, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)

                VStack(alignment: .leading, spacing: 4) {
                    KikariaTypography.mixedText(title, size: 17, weight: .semibold)
                        .foregroundStyle(KikariaTheme.deepText)

                    KikariaTypography.mixedText(subtitle, size: 13, weight: .medium)
                        .foregroundStyle(KikariaTheme.softText)
                }

                Spacer()

                if let valueText {
                    KikariaTypography.mixedText(valueText, size: 17, weight: .semibold)
                        .monospacedDigit()
                        .foregroundStyle(KikariaTheme.sky)
                        .lineLimit(1)
                        .minimumScaleFactor(0.76)
                }

                if showsChevron {
                    Image(systemName: "chevron.right")
                        .font(.subheadline.weight(.semibold))
                        .foregroundStyle(KikariaTheme.blueGray)
                }
            }
            .padding(18)
            .frame(maxWidth: .infinity)
            .liquidGlassCard(cornerRadius: 26, fillOpacity: 0.44, strokeOpacity: 0.42, shadowOpacity: 0.12, shadowRadius: 18, shadowY: 10)
        }
        .buttonStyle(.plain)
    }
}

private enum KikariaWheelStyle {
    static let fontSize: CGFloat = 16
    static let fontWeight: Font.Weight = .medium
    static let pickerHeight: CGFloat = 102
    static let columnSpacing: CGFloat = 5
    static let minimumScaleFactor: CGFloat = 0.78

    static var valueFont: Font {
        .system(size: fontSize, weight: fontWeight, design: .serif)
    }
}

private struct KikariaWheelValueText: View {
    let text: String
    let width: CGFloat
    var usesMonospacedDigits = true

    var body: some View {
        let valueText = KikariaTypography.mixedText(
            text,
            chineseFont: .system(size: KikariaWheelStyle.fontSize, weight: KikariaWheelStyle.fontWeight),
            serifFont: KikariaWheelStyle.valueFont
        )

        Group {
            if usesMonospacedDigits {
                valueText
                    .monospacedDigit()
            } else {
                valueText
            }
        }
        .lineLimit(1)
        .minimumScaleFactor(KikariaWheelStyle.minimumScaleFactor)
        .frame(width: width, alignment: .center)
    }
}

private struct DailyGoalPickerBubble: View {
    @Binding var dailyGoal: Int
    let onDone: () -> Void

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                Text("每日学习目标")
                    .font(KikariaTypography.chineseHeadline())
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer()

                KikariaTypography.numericText("\(dailyGoal)", size: 17)
                    .monospacedDigit()
                    .foregroundStyle(KikariaTheme.sky)
            }

            Picker("每日学习目标", selection: $dailyGoal) {
                ForEach(1...100, id: \.self) { goal in
                    KikariaWheelValueText(text: "\(goal) 个", width: 88)
                        .tag(goal)
                }
            }
            .kikariaWheelPickerStyle()
            .frame(maxWidth: .infinity)
            .frame(height: KikariaWheelStyle.pickerHeight)
            .clipped()

            Button(action: onDone) {
                Text("完成")
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                    .background(KikariaTheme.actionGradient, in: Capsule())
            }
            .buttonStyle(.plain)
        }
        .padding(18)
        .frame(maxWidth: 318)
        .liquidGlassCard(cornerRadius: 28, material: .regularMaterial, fillOpacity: 0.50, strokeOpacity: 0.52, shadowOpacity: 0.18, shadowRadius: 24, shadowY: 14)
    }
}

private struct NotificationTimePickerBubble: View {
    @Binding var notificationTime: Date
    let onDone: () -> Void

    private var timeText: String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: notificationTime)
    }

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                Text("通知时间")
                    .font(KikariaTypography.chineseHeadline())
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer()

                KikariaTypography.numericText(timeText, size: 17)
                    .monospacedDigit()
                    .foregroundStyle(KikariaTheme.sky)
            }

            NotificationTimeWheelPicker(time: $notificationTime)
                .frame(maxWidth: .infinity)
                .frame(height: KikariaWheelStyle.pickerHeight)
                .clipped()

            Button(action: onDone) {
                Text("完成")
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                    .background(KikariaTheme.actionGradient, in: Capsule())
            }
            .buttonStyle(.plain)
        }
        .padding(18)
        .frame(maxWidth: 318)
        .liquidGlassCard(cornerRadius: 28, material: .regularMaterial, fillOpacity: 0.50, strokeOpacity: 0.52, shadowOpacity: 0.18, shadowRadius: 24, shadowY: 14)
    }
}

private struct NotificationTimeWheelPicker: View {
    @Binding var time: Date

    private let calendar = Calendar.current

    private var selectedHour: Int {
        calendar.component(.hour, from: time)
    }

    private var selectedMinute: Int {
        calendar.component(.minute, from: time)
    }

    private var hourBinding: Binding<Int> {
        Binding(
            get: { selectedHour },
            set: { updateTime(hour: $0) }
        )
    }

    private var minuteBinding: Binding<Int> {
        Binding(
            get: { selectedMinute },
            set: { updateTime(minute: $0) }
        )
    }

    var body: some View {
        HStack(alignment: .center, spacing: KikariaWheelStyle.columnSpacing) {
            Picker("Hour", selection: hourBinding) {
                ForEach(0...23, id: \.self) { hour in
                    KikariaWheelValueText(text: String(format: "%02d", hour), width: 54)
                        .tag(hour)
                }
            }
            .kikariaWheelPickerStyle()
            .labelsHidden()
            .frame(width: 68)
            .clipped()

            Text(":")
                .font(KikariaWheelStyle.valueFont)
                .foregroundStyle(KikariaTheme.softText)
                .frame(width: 16)

            Picker("Minute", selection: minuteBinding) {
                ForEach(0...59, id: \.self) { minute in
                    KikariaWheelValueText(text: String(format: "%02d", minute), width: 54)
                        .tag(minute)
                }
            }
            .kikariaWheelPickerStyle()
            .labelsHidden()
            .frame(width: 68)
            .clipped()
        }
        .frame(maxWidth: .infinity, alignment: .center)
        .frame(height: KikariaWheelStyle.pickerHeight)
    }

    private func updateTime(hour: Int? = nil, minute: Int? = nil) {
        var components = calendar.dateComponents([.year, .month, .day, .second, .nanosecond], from: time)
        components.hour = hour ?? selectedHour
        components.minute = minute ?? selectedMinute
        time = calendar.date(from: components) ?? time
    }
}

private struct CountdownDateRangePickerBubble: View {
    @Binding var startDate: Date
    @Binding var endDate: Date
    let isConfigured: Bool
    let errorMessage: String?
    let onClear: () -> Void
    let onDone: () -> Void

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                Text("倒数日")
                    .font(KikariaTypography.chineseHeadline())
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer()

                KikariaTypography.mixedText(isConfigured ? countdownText(for: endDate) : "未设置", size: 17, weight: .semibold)
                    .monospacedDigit()
                    .foregroundStyle(KikariaTheme.sky)
            }

            VStack(spacing: 12) {
                datePickerSection(title: "开始日期", selection: $startDate)
                datePickerSection(title: "结束日期", selection: $endDate)
            }

            if let errorMessage {
                KikariaTypography.mixedText(errorMessage, size: 12, weight: .semibold)
                    .foregroundStyle(KikariaTheme.removeCoral)
                    .frame(maxWidth: .infinity, alignment: .leading)
            }

            HStack(spacing: 10) {
                Button(action: onClear) {
                    Text("清除")
                        .font(KikariaTypography.chineseButton())
                        .foregroundStyle(KikariaTheme.deepText)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .liquidGlassCapsule(fillOpacity: 0.44, strokeOpacity: 0.42, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                }
                .buttonStyle(.plain)

                Button(action: onDone) {
                    Text("完成")
                        .font(KikariaTypography.chineseButton())
                        .foregroundStyle(.white)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .background(KikariaTheme.actionGradient, in: Capsule())
                }
                .buttonStyle(.plain)
            }
        }
        .padding(18)
        .frame(maxWidth: 326)
        .liquidGlassCard(cornerRadius: 28, material: .regularMaterial, fillOpacity: 0.50, strokeOpacity: 0.52, shadowOpacity: 0.18, shadowRadius: 24, shadowY: 14)
    }

    private func datePickerSection(title: String, selection: Binding<Date>) -> some View {
        VStack(spacing: 14) {
            KikariaTypography.mixedText(title, size: 13, weight: .semibold)
                .foregroundStyle(KikariaTheme.softText)
                .frame(maxWidth: .infinity, alignment: .center)

            CountdownDateWheelPicker(date: selection)
                .frame(maxWidth: .infinity)
                .frame(height: KikariaWheelStyle.pickerHeight)
                .clipped()
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

private struct CountdownDateWheelPicker: View {
    @Binding var date: Date

    private let calendar = Calendar.current
    private static let monthSymbols: [String] = {
        let formatter = DateFormatter()
        return formatter.monthSymbols
    }()

    private var selectedYear: Int {
        calendar.component(.year, from: date)
    }

    private var selectedMonth: Int {
        calendar.component(.month, from: date)
    }

    private var selectedDay: Int {
        calendar.component(.day, from: date)
    }

    private var daysInSelectedMonth: Int {
        days(inMonth: selectedMonth, year: selectedYear)
    }

    private var yearValues: [Int] {
        let currentYear = calendar.component(.year, from: Date())
        let lowerBound = min(currentYear - 10, selectedYear - 2)
        let upperBound = max(currentYear + 50, selectedYear + 2)
        return Array(lowerBound...upperBound)
    }

    private var monthBinding: Binding<Int> {
        Binding(
            get: { selectedMonth },
            set: { updateDate(month: $0) }
        )
    }

    private var dayBinding: Binding<Int> {
        Binding(
            get: { selectedDay },
            set: { updateDate(day: $0) }
        )
    }

    private var yearBinding: Binding<Int> {
        Binding(
            get: { selectedYear },
            set: { updateDate(year: $0) }
        )
    }

    var body: some View {
        HStack(alignment: .center, spacing: KikariaWheelStyle.columnSpacing) {
            Picker("Month", selection: monthBinding) {
                ForEach(1...12, id: \.self) { month in
                    KikariaWheelValueText(
                        text: monthName(for: month),
                        width: 108,
                        usesMonospacedDigits: false
                    )
                        .tag(month)
                }
            }
            .kikariaWheelPickerStyle()
            .labelsHidden()
            .frame(width: 114)
            .clipped()

            Picker("Day", selection: dayBinding) {
                ForEach(1...daysInSelectedMonth, id: \.self) { day in
                    KikariaWheelValueText(text: "\(day)", width: 48)
                        .tag(day)
                }
            }
            .kikariaWheelPickerStyle()
            .labelsHidden()
            .frame(width: 54)
            .clipped()

            Picker("Year", selection: yearBinding) {
                ForEach(yearValues, id: \.self) { year in
                    KikariaWheelValueText(text: "\(year)", width: 74)
                        .tag(year)
                }
            }
            .kikariaWheelPickerStyle()
            .labelsHidden()
            .frame(width: 80)
            .clipped()
        }
        .frame(maxWidth: .infinity, alignment: .center)
        .frame(height: KikariaWheelStyle.pickerHeight)
    }

    private func monthName(for month: Int) -> String {
        let index = month - 1
        guard Self.monthSymbols.indices.contains(index) else {
            return "\(month)"
        }

        return Self.monthSymbols[index]
    }

    private func updateDate(year: Int? = nil, month: Int? = nil, day: Int? = nil) {
        let newYear = year ?? selectedYear
        let newMonth = month ?? selectedMonth
        let maxDay = days(inMonth: newMonth, year: newYear)
        let newDay = min(day ?? selectedDay, maxDay)
        var components = calendar.dateComponents([.hour, .minute, .second, .nanosecond], from: date)
        components.year = newYear
        components.month = newMonth
        components.day = newDay
        date = calendar.date(from: components) ?? date
    }

    private func days(inMonth month: Int, year: Int) -> Int {
        let components = DateComponents(year: year, month: month)
        guard let date = calendar.date(from: components),
              let range = calendar.range(of: .day, in: .month, for: date)
        else {
            return 31
        }

        return range.count
    }
}

private struct DangerPercentPickerBubble: View {
    @Binding var dangerPercent: Int
    let onDone: () -> Void

    var body: some View {
        VStack(spacing: 12) {
            HStack {
                Text("进度安全线")
                    .font(KikariaTypography.chineseHeadline())
                    .foregroundStyle(KikariaTheme.deepText)

                Spacer()

                KikariaTypography.numericText("\(dangerPercent)%", size: 17)
                    .monospacedDigit()
                    .foregroundStyle(KikariaTheme.sky)
            }

            Picker("进度安全线", selection: $dangerPercent) {
                ForEach(1...100, id: \.self) { percent in
                    KikariaWheelValueText(text: "\(percent)%", width: 82)
                        .tag(percent)
                }
            }
            .kikariaWheelPickerStyle()
            .frame(maxWidth: .infinity)
            .frame(height: KikariaWheelStyle.pickerHeight)
            .clipped()

            Button(action: onDone) {
                Text("完成")
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 12)
                    .background(KikariaTheme.actionGradient, in: Capsule())
            }
            .buttonStyle(.plain)
        }
        .padding(18)
        .frame(maxWidth: 318)
        .liquidGlassCard(cornerRadius: 28, material: .regularMaterial, fillOpacity: 0.50, strokeOpacity: 0.52, shadowOpacity: 0.18, shadowRadius: 24, shadowY: 14)
    }
}

private struct PresetSelectionView: View {
    let presets: [KnowledgePreset]
    @Binding var currentPresetID: String
    let switchPreset: (KnowledgePreset) -> Bool
    let deletePreset: (String) -> PresetDeleteOutcome
    let onUploadNewPreset: () -> Void
    let onEditPreset: (KnowledgePreset) -> Void
    @State private var pendingPreset: KnowledgePreset?
    @State private var pendingDeletePreset: KnowledgePreset?
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    private func landscapeContent(
        metrics: KikariaAdaptiveLayout.Metrics,
        scale: CGFloat,
        titleFontSize: CGFloat
    ) -> some View {
        let gridSpacing = min(max(metrics.collectionLandscapeAvailableWidth * 0.026, 24), 32)
        let gridColumns = [
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top),
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top)
        ]
        let uploadButtonWidth = min(max(metrics.collectionLandscapeAvailableWidth * 0.22, 220), 260)

        return ScrollView {
            VStack(alignment: .leading, spacing: metrics.pageTitleSpacing(defaultValue: 18 * scale)) {
                HStack(alignment: .center, spacing: 20 * scale) {
                    Text("切换预设")
                        .font(KikariaTypography.chineseTitle(size: titleFontSize))
                        .foregroundStyle(KikariaTheme.deepText)

                    Spacer()

                    Button(action: onUploadNewPreset) {
                        HStack(spacing: 12 * scale) {
                            Text("上传新预设")
                                .font(KikariaTypography.chineseButton(size: 17 * scale))
                            Spacer()
                            Image(systemName: "plus")
                                .font(.system(size: 15 * scale, weight: .semibold))
                        }
                        .foregroundStyle(.white)
                        .padding(.horizontal, 20 * scale)
                        .frame(maxWidth: .infinity, minHeight: 58 * scale)
                        .background(KikariaTheme.actionGradient, in: RoundedRectangle(cornerRadius: 22 * scale, style: .continuous))
                        .overlay {
                            RoundedRectangle(cornerRadius: 22 * scale, style: .continuous)
                                .stroke(
                                    LinearGradient(
                                        colors: [
                                            Color.white.opacity(0.36),
                                            Color.white.opacity(0.10)
                                        ],
                                        startPoint: .topLeading,
                                        endPoint: .bottomTrailing
                                    ),
                                    lineWidth: 1
                                )
                        }
                        .shadow(color: KikariaTheme.sky.opacity(0.18), radius: 16 * scale, y: 8 * scale)
                    }
                    .buttonStyle(.plain)
                    .frame(width: uploadButtonWidth)
                }
                    .padding(.top, metrics.pageTitleTopPadding(defaultValue: 18 * scale))

                LazyVGrid(columns: gridColumns, alignment: .center, spacing: 20) {
                    ForEach(presets) { preset in
                        PresetCard(
                            preset: preset,
                            isCurrent: preset.id == currentPresetID,
                            cardScale: metrics.listCardScale,
                            onSelect: {
                                if preset.id != currentPresetID {
                                    pendingPreset = preset
                                }
                            },
                            onEdit: {
                                onEditPreset(preset)
                            },
                            onDelete: {
                                pendingDeletePreset = preset
                            }
                        )
                        .buttonStyle(.plain)
                        .frame(maxWidth: .infinity, alignment: .topLeading)
                    }
                }
                .padding(.top, 4)
            }
            .padding(.horizontal, metrics.horizontalPadding)
            .padding(.bottom, 34)
            .frame(maxWidth: metrics.collectionLandscapeMaxWidth)
            .frame(maxWidth: .infinity)
        }
        .kikariaScrollIndicators(hidden: true)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.presetScale
            let columnMaxWidth = metrics.presetOuterMaxWidth
            let outerMaxWidth = metrics.collectionUsesTwoColumnLayout ? metrics.collectionLandscapeMaxWidth : columnMaxWidth
            let pagePadding = metrics.innerHorizontalPadding
            let titleFontSize = metrics.pageTitleFontSize(defaultValue: 32 * scale)
            let titleTopPadding = metrics.pageTitleTopPadding(defaultValue: 18 * scale)
            let titleSpacing = metrics.pageTitleSpacing(defaultValue: 18 * scale)

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                if metrics.collectionUsesTwoColumnLayout {
                    landscapeContent(metrics: metrics, scale: scale, titleFontSize: titleFontSize)
                } else {
                    ScrollView {
                        VStack(alignment: .leading, spacing: titleSpacing) {
                            Text("切换预设")
                                .font(KikariaTypography.chineseTitle(size: titleFontSize))
                                .foregroundStyle(KikariaTheme.deepText)
                                .padding(.top, titleTopPadding)
                                .padding(.bottom, metrics.isPadPortrait ? 0 : 2 * scale)

                            Button(action: onUploadNewPreset) {
                                HStack(spacing: 12 * scale) {
                                    Text("上传新预设")
                                        .font(KikariaTypography.chineseButton(size: 17 * scale))
                                    Spacer()
                                    Image(systemName: "plus")
                                        .font(.system(size: 15 * scale, weight: .semibold))
                                }
                                .foregroundStyle(.white)
                                .padding(.horizontal, 20 * scale)
                                .frame(maxWidth: .infinity, minHeight: 58 * scale)
                                .background(KikariaTheme.actionGradient, in: RoundedRectangle(cornerRadius: 22 * scale, style: .continuous))
                                .overlay {
                                    RoundedRectangle(cornerRadius: 22 * scale, style: .continuous)
                                        .stroke(
                                            LinearGradient(
                                                colors: [
                                                    Color.white.opacity(0.36),
                                                    Color.white.opacity(0.10)
                                                ],
                                                startPoint: .topLeading,
                                                endPoint: .bottomTrailing
                                            ),
                                            lineWidth: 1
                                        )
                                }
                                .shadow(color: KikariaTheme.sky.opacity(0.18), radius: 16 * scale, y: 8 * scale)
                            }
                            .buttonStyle(.plain)
                            .padding(.bottom, 2 * scale)

                            ForEach(presets) { preset in
                                PresetCard(
                                    preset: preset,
                                    isCurrent: preset.id == currentPresetID,
                                    cardScale: metrics.listCardScale,
                                    onSelect: {
                                        if preset.id != currentPresetID {
                                            pendingPreset = preset
                                        }
                                    },
                                    onEdit: {
                                        onEditPreset(preset)
                                    },
                                    onDelete: {
                                        pendingDeletePreset = preset
                                    }
                                )
                                .buttonStyle(.plain)
                            }
                        }
                        .padding(.horizontal, pagePadding)
                        .padding(.bottom, 34)
                        .frame(maxWidth: columnMaxWidth)
                        .frame(maxWidth: .infinity)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                    .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
            .kikariaAdaptiveNavigationChrome(metrics: metrics, outerMaxWidth: outerMaxWidth)
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
        .alert("切换预设？", isPresented: isConfirmingPreset) {
            Button("取消", role: .cancel) {
                pendingPreset = nil
            }

            Button("确认切换", role: .destructive) {
                confirmPresetSwitch()
            }
        } message: {
            Text("将切换到另一套知识点。当前预设的学习进度会被保留。")
        }
        .alert("删除预设？", isPresented: isConfirmingPresetDelete) {
            Button("取消", role: .cancel) {
                pendingDeletePreset = nil
            }

            Button("删除", role: .destructive) {
                confirmPresetDelete()
            }
        } message: {
            Text("删除后将移除该预设的所有知识点、重点集锦、已掌握状态和学习记录。")
        }
    }

    private var isConfirmingPreset: Binding<Bool> {
        Binding(
            get: { pendingPreset != nil },
            set: { isPresented in
                if !isPresented {
                    pendingPreset = nil
                }
            }
        )
    }

    private var isConfirmingPresetDelete: Binding<Bool> {
        Binding(
            get: { pendingDeletePreset != nil },
            set: { isPresented in
                if !isPresented {
                    pendingDeletePreset = nil
                }
            }
        )
    }

    private func confirmPresetSwitch() {
        guard let preset = pendingPreset else {
            return
        }

        pendingPreset = nil

        if switchPreset(preset) {
            showToast("已切换至「\(preset.name)」")
        } else {
            showToast("预设解析失败，请稍后再试")
        }
    }

    private func confirmPresetDelete() {
        guard let preset = pendingDeletePreset else {
            return
        }

        pendingDeletePreset = nil

        switch deletePreset(preset.id) {
        case .deleted(let name):
            showToast("已删除「\(name)」")
        case .blockedLastPreset:
            showToast("至少需要保留一个预设")
        case .notFound:
            showToast("预设不存在")
        }
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct PresetCard: View {
    let preset: KnowledgePreset
    let isCurrent: Bool
    var cardScale: CGFloat = 1
    let onSelect: () -> Void
    let onEdit: () -> Void
    let onDelete: () -> Void

    var body: some View {
        let scale = max(cardScale, 1)

        VStack(alignment: .leading, spacing: 11 * scale) {
            HStack(alignment: .top, spacing: 12 * scale) {
                VStack(alignment: .leading, spacing: 8 * scale) {
                    HStack(spacing: 8 * scale) {
                        KikariaTypography.mixedText(preset.name, size: 20 * scale, weight: .semibold)
                            .foregroundStyle(KikariaTheme.deepText)
                            .lineLimit(1)
                            .minimumScaleFactor(0.82)

                        if isCurrent {
                            Text("当前")
                                .font(KikariaTypography.tag(size: 11 * scale, weight: .bold))
                                .foregroundStyle(KikariaTheme.sky)
                                .padding(.horizontal, 8 * scale)
                                .padding(.vertical, 4 * scale)
                                .liquidGlassCapsule(fillOpacity: 0.34, strokeOpacity: 0.38, shadowOpacity: 0.04, shadowRadius: 6 * scale, shadowY: 3 * scale)
                        }
                    }

                    HStack(spacing: 9 * scale) {
                        KikariaTypography.mixedText("\(preset.knowledgePointCount) 个知识点", size: 12 * scale, weight: .semibold)
                            .foregroundStyle(KikariaTheme.softText)
                    }
                }

                Spacer()

                HStack(spacing: 8 * scale) {
                    Button(action: onEdit) {
                        Image(systemName: "pencil")
                            .font(.system(size: 15 * scale, weight: .semibold))
                            .foregroundStyle(KikariaTheme.deepText)
                            .frame(width: 34 * scale, height: 34 * scale)
                            .liquidGlassCircle(fillOpacity: 0.34, strokeOpacity: 0.36, shadowOpacity: 0.07, shadowRadius: 8 * scale, shadowY: 4 * scale)
                    }
                    .buttonStyle(.plain)

                    Button(action: onDelete) {
                        Image(systemName: "trash")
                            .font(.system(size: 15 * scale, weight: .semibold))
                            .foregroundStyle(KikariaTheme.removeCoral)
                            .frame(width: 34 * scale, height: 34 * scale)
                            .liquidGlassCircle(fillOpacity: 0.34, strokeOpacity: 0.36, shadowOpacity: 0.07, shadowRadius: 8 * scale, shadowY: 4 * scale)
                    }
                    .buttonStyle(.plain)
                }
            }
        }
        .padding(18 * scale)
        .frame(maxWidth: .infinity)
        .contentShape(RoundedRectangle(cornerRadius: 24 * scale, style: .continuous))
        .onTapGesture(perform: onSelect)
        .liquidGlassCard(cornerRadius: 24 * scale, material: .thinMaterial, fillOpacity: isCurrent ? 0.42 : 0.34, strokeOpacity: isCurrent ? 0.52 : 0.34, shadowOpacity: isCurrent ? 0.12 : 0.08, shadowRadius: 17 * scale, shadowY: 9 * scale)
    }
}

private struct NewPresetView: View {
    @Environment(\.dismiss) private var dismiss
    let createPreset: (String, String, String) -> PresetCreationOutcome
    @State private var name = ""
    @State private var category = ""
    @State private var markdownText = ""
    @State private var errorMessage: String?
    @State private var isImportingFile = false
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    private var allowedContentTypes: [UTType] {
        var types: [UTType] = [.plainText, .text]

        if let markdownType = UTType(filenameExtension: "md") {
            types.insert(markdownType, at: 0)
        }

        return types
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.newPresetScale
            let columnMaxWidth = metrics.newPresetOuterMaxWidth
            let pagePadding = metrics.innerHorizontalPadding
            let inputHeight: CGFloat? = metrics.isPadPortrait ? metrics.newPresetInputHeight : nil
            let textEditorHeight = metrics.newPresetTextEditorHeight

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 0) {
                    HStack {
                        KikariaAdaptiveBackButton(metrics: metrics) {
                            dismiss()
                        }

                        Spacer()

                        Text("上传新预设")
                            .font(KikariaTypography.chineseHeadline(size: 17 * scale))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()

                        Button("保存") {
                            savePreset()
                        }
                        .font(KikariaTypography.chineseButton(size: 17 * scale))
                        .foregroundStyle(KikariaTheme.sky)
                        .frame(width: metrics.adaptiveTopBarTrailingWidth, alignment: .trailing)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.top, 18 * scale)
                    .padding(.bottom, 16 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)

                    ScrollView {
                        VStack(spacing: 16 * scale) {
                            ProfileTextField(title: "预设名称", text: $name, scale: scale, minHeight: inputHeight)
                            ProfileTextField(title: "分类", text: $category, scale: scale, minHeight: inputHeight)

                            Button {
                                isImportingFile = true
                            } label: {
                                Label {
                                    KikariaTypography.mixedText("选择 .md / .txt 文件", size: 17 * scale, weight: .semibold)
                                } icon: {
                                    Image(systemName: "doc.badge.plus")
                                }
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .frame(maxWidth: .infinity)
                                    .padding(.vertical, 15 * scale)
                                    .liquidGlassCard(cornerRadius: 22 * scale, fillOpacity: 0.42, strokeOpacity: 0.38, shadowOpacity: 0.08, shadowRadius: 12 * scale, shadowY: 7 * scale)
                            }
                            .buttonStyle(.plain)

                            VStack(alignment: .leading, spacing: 8 * scale) {
                                HStack(alignment: .center) {
                                    KikariaTypography.mixedText("Markdown 文本", size: 14 * scale, weight: .semibold)
                                        .foregroundStyle(KikariaTheme.softText)

                                    Spacer()

                                    routeLink(to: .markdownFormatGuide) {
                                        KikariaTypography.mixedText("如何编写 Markdown 预设？", size: 12 * scale, weight: .semibold)
                                            .foregroundStyle(KikariaTheme.sky)
                                            .padding(.horizontal, 12 * scale)
                                            .padding(.vertical, 7 * scale)
                                            .liquidGlassCapsule(fillOpacity: 0.38, strokeOpacity: 0.36, shadowOpacity: 0.04, shadowRadius: 6 * scale, shadowY: 3 * scale)
                                    }
                                    .buttonStyle(.plain)
                                }

                                TextEditor(text: $markdownText)
                                    .font(.system(size: 17 * scale, weight: .regular, design: .serif))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .kikariaHideScrollContentBackground()
                                    .kikariaMacClearTextEditorBackground()
                                    .padding(14 * scale)
                                    .frame(minHeight: textEditorHeight)
                                    .liquidGlassCard(cornerRadius: 24 * scale, material: .thinMaterial, fillOpacity: 0.56, strokeOpacity: 0.34, shadowOpacity: 0.10, shadowRadius: 14 * scale, shadowY: 8 * scale)
                            }

                            if let errorMessage {
                                KikariaTypography.mixedText(errorMessage, size: 14 * scale, weight: .semibold)
                                    .foregroundStyle(KikariaTheme.removeCoral)
                                    .frame(maxWidth: .infinity, alignment: .leading)
                                    .padding(14 * scale)
                                    .liquidGlassCard(cornerRadius: 18 * scale, fillOpacity: 0.50, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8 * scale, shadowY: 4 * scale)
                            }
                        }
                        .padding(.horizontal, pagePadding)
                        .padding(.top, metrics.ipadPortraitFormPageTopInset)
                        .padding(.bottom, 32 * scale)
                        .frame(maxWidth: columnMaxWidth)
                        .frame(maxWidth: .infinity)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
        .fileImporter(isPresented: $isImportingFile, allowedContentTypes: allowedContentTypes, allowsMultipleSelection: false) { result in
            importMarkdownFile(result)
        }
    }

    private func importMarkdownFile(_ result: Result<[URL], Error>) {
        switch result {
        case .success(let urls):
            guard let url = urls.first else {
                return
            }

            let canAccess = url.startAccessingSecurityScopedResource()
            defer {
                if canAccess {
                    url.stopAccessingSecurityScopedResource()
                }
            }

            do {
                markdownText = try String(contentsOf: url, encoding: .utf8)
                let importedName = url.deletingPathExtension().lastPathComponent
                    .trimmingCharacters(in: .whitespacesAndNewlines)
                if name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, !importedName.isEmpty {
                    name = importedName
                }
                errorMessage = nil
            } catch {
                errorMessage = "文件读取失败，请确认它是 UTF-8 文本。"
            }
        case .failure:
            errorMessage = "文件选择失败，请重试。"
        }
    }

    private func savePreset() {
        switch createPreset(name, category, markdownText) {
        case .success(let preset):
            showToast("已创建「\(preset.name)」")
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.55) {
                dismiss()
            }
        case .failure(let message):
            withAnimation(.easeInOut(duration: 0.2)) {
                errorMessage = message
            }
        }
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct MarkdownFormatGuideView: View {
    @Environment(\.dismiss) private var dismiss
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    private static let formatTemplate = """
    # 知识点名称

    tags: 标签1, 标签2, 标签3

    hint:
    这里写提示，可以是一句话，也可以是几行文字。

    content:
    这里写完整答案或背诵内容，可以是一段或多段文字。

    ---
    """

    private static let completeExample = """
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
    """

    private static let latexExample = """
    Kikaria 使用本地 SwiftMath 渲染公式，不会联网处理。

    推荐：中文说明放在公式外

    函数 $f(x)=x^2$ 的导数是 $2x$。

    当 x 接近 0 时，有：

    $$
    \\lim_{x\\to0}\\frac{\\sin x}{x}=1
    $$

    不推荐：没有 $ 包裹的 LaTeX 不会渲染

    \\Delta\\varphi=0
    """

    private static let aiPrompt = """
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
    14. 公式环境中不要混入中文，中文解释要写在公式外；必要时可少量使用 \\text{...}。

    下面是需要整理的资料：

    【在这里粘贴课本、讲义、笔记或 OCR 文本】
    """

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.newPresetScale
            let columnMaxWidth = metrics.formMaxWidth
            let pagePadding = metrics.innerHorizontalPadding

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 0) {
                    HStack {
                        KikariaAdaptiveBackButton(metrics: metrics) {
                            dismiss()
                        }

                        Spacer()

                        Text("Markdown 格式说明")
                            .font(KikariaTypography.chineseHeadline(size: 17 * scale))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()

                        Color.clear
                            .frame(width: metrics.adaptiveTopBarTrailingWidth, height: metrics.adaptiveBackButtonSize)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.top, 18 * scale)
                    .padding(.bottom, 12 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)

                    ScrollView {
                        VStack(alignment: .leading, spacing: 16) {
                            MarkdownGuideCard {
                                Text("Kikaria 使用结构化 Markdown 来导入知识点。每个知识点由标题、标签、提示和答案组成。多个知识点之间使用 --- 分隔。")
                                    .font(KikariaTypography.chineseBody(size: 15))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .lineSpacing(5)
                            }

                            MarkdownGuideCard(title: "格式规则") {
                                MarkdownCodeBlock(text: Self.formatTemplate)

                                Text("多个知识点之间用一行 --- 分隔。")
                                    .font(KikariaTypography.chineseBody(size: 14, weight: .medium))
                                    .foregroundStyle(KikariaTheme.softText)
                            }

                            MarkdownGuideCard(title: "规则说明") {
                                VStack(alignment: .leading, spacing: 9) {
                                    MarkdownRuleText("标题必须以 # 开头。")
                                    MarkdownRuleText("tags: 后面写标签，多个标签用英文逗号或中文逗号分隔。")
                                    MarkdownRuleText("hint: 后面写提示。")
                                    MarkdownRuleText("content: 后面写完整内容。")
                                    MarkdownRuleText("每个知识点之间用单独一行 --- 分隔。")
                                    MarkdownRuleText("建议每个知识点不要太长，适合一次背诵。")
                                    MarkdownRuleText("标签可以用于后续选择背诵范围。")
                                }
                            }

                            MarkdownGuideCard(title: "LaTeX 公式") {
                                VStack(alignment: .leading, spacing: 10) {
                                    MarkdownRuleText("Kikaria 使用本地 SwiftMath 渲染公式，不会联网处理。")
                                    MarkdownRuleText("行内公式必须写成：$f(x)=x^2$。")
                                    MarkdownRuleText("块级公式必须写成：用 $$...$$ 单独成块。")
                                    MarkdownRuleText("只有 $...$ 和 $$...$$ 中的内容会渲染为公式。")
                                    MarkdownRuleText("没有包裹的 LaTeX 命令不会自动识别，会按普通文本显示。")
                                    MarkdownRuleText("公式环境中不要混入中文，中文说明应放在公式外。")
                                    MarkdownRuleText("App 会尽量渲染 \\text{...}，但不建议在复杂公式里滥用中文。")
                                    MarkdownRuleText("矩阵、cases、align 等复杂结构会尽量交给 SwiftMath 渲染；失败时显示原始源码。")
                                    MarkdownRuleText("导入、编辑和导出都会保留原始 LaTeX 源码。")

                                    MarkdownCodeBlock(text: Self.latexExample)
                                }
                            }

                            MarkdownGuideCard(title: "完整示例") {
                                MarkdownCodeBlock(text: Self.completeExample)
                            }

                            MarkdownGuideCard {
                                VStack(alignment: .leading, spacing: 12) {
                                    HStack(alignment: .center) {
                                        Text("给 AI 助手的 Prompt")
                                            .font(KikariaTypography.chineseHeadline(size: 18))
                                            .foregroundStyle(KikariaTheme.deepText)

                                        Spacer()

                                        Button {
                                            copyPrompt()
                                        } label: {
                                            Label("复制 Prompt", systemImage: "doc.on.doc")
                                                .font(KikariaTypography.chineseCaption(size: 12, weight: .semibold))
                                                .foregroundStyle(.white)
                                                .padding(.horizontal, 12)
                                                .padding(.vertical, 8)
                                                .background(KikariaTheme.actionGradient, in: Capsule())
                                        }
                                        .buttonStyle(.plain)
                                    }

                                    Text("你可以把下面这段 prompt 复制给 AI 助手，并附上你的课本、讲义、笔记或照片识别出的文本，让 AI 帮你整理成 Kikaria 支持的 Markdown 格式。")
                                        .font(KikariaTypography.chineseBody(size: 14))
                                        .foregroundStyle(KikariaTheme.softText)
                                        .lineSpacing(4)

                                    MarkdownCodeBlock(text: Self.aiPrompt)
                                }
                            }
                        }
                        .padding(.horizontal, metrics.horizontalPadding)
                        .padding(.bottom, 34)
                        .frame(maxWidth: metrics.formMaxWidth)
                        .frame(maxWidth: .infinity)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
    }

    private func copyPrompt() {
        #if os(iOS)
        UIPasteboard.general.string = Self.aiPrompt
        #elseif os(macOS)
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(Self.aiPrompt, forType: .string)
        #endif
        showToast("Prompt 已复制")
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct MarkdownGuideCard<Content: View>: View {
    var title: String?
    @ViewBuilder var content: Content

    init(title: String? = nil, @ViewBuilder content: () -> Content) {
        self.title = title
        self.content = content()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            if let title {
                Text(title)
                    .font(KikariaTypography.chineseHeadline(size: 18))
                    .foregroundStyle(KikariaTheme.deepText)
            }

            content
        }
        .padding(18)
        .frame(maxWidth: .infinity, alignment: .leading)
        .liquidGlassCard(cornerRadius: 24, fillOpacity: 0.44, strokeOpacity: 0.40, shadowOpacity: 0.10, shadowRadius: 16, shadowY: 8)
    }
}

private struct MarkdownCodeBlock: View {
    let text: String

    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            Text(text)
                .font(.system(size: 13, weight: .regular, design: .monospaced))
                .foregroundStyle(KikariaTheme.deepText)
                .lineSpacing(4)
                .kikariaEnableTextSelection()
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(14)
        }
        .liquidGlassCard(cornerRadius: 18, material: .thinMaterial, fillOpacity: 0.54, strokeOpacity: 0.28, shadowOpacity: 0.04, shadowRadius: 8, shadowY: 4)
    }
}

private struct MarkdownRuleText: View {
    let text: String

    init(_ text: String) {
        self.text = text
    }

    var body: some View {
        HStack(alignment: .top, spacing: 8) {
            Circle()
                .fill(KikariaTheme.sky.opacity(0.72))
                .frame(width: 5, height: 5)
                .padding(.top, 8)

            Text(text)
                .font(KikariaTypography.chineseBody(size: 14))
                .foregroundStyle(KikariaTheme.deepText)
                .lineSpacing(4)
        }
    }
}

private struct EditPresetView: View {
    @Environment(\.dismiss) private var dismiss
    let preset: KnowledgePreset
    let knowledgePoints: [KnowledgePoint]
    let onSavePreset: (String, String, String) -> Void
    let onAddPoint: () -> Void
    let onEditPoint: (UUID) -> Void
    let onDeletePoint: (UUID, String) -> Void
    let onDeletePreset: (String) -> Void
    @State private var name: String
    @State private var category: String
    @State private var searchText = ""
    @State private var pendingDeletePoint: KnowledgePoint?
    @State private var isConfirmingPresetDelete = false
    @State private var shareFile: ShareFile?
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    init(
        preset: KnowledgePreset,
        knowledgePoints: [KnowledgePoint],
        onSavePreset: @escaping (String, String, String) -> Void,
        onAddPoint: @escaping () -> Void,
        onEditPoint: @escaping (UUID) -> Void,
        onDeletePoint: @escaping (UUID, String) -> Void,
        onDeletePreset: @escaping (String) -> Void
    ) {
        self.preset = preset
        self.knowledgePoints = knowledgePoints
        self.onSavePreset = onSavePreset
        self.onAddPoint = onAddPoint
        self.onEditPoint = onEditPoint
        self.onDeletePoint = onDeletePoint
        self.onDeletePreset = onDeletePreset
        _name = State(initialValue: preset.name)
        _category = State(initialValue: preset.category)
    }

    private var filteredKnowledgePoints: [KnowledgePoint] {
        knowledgePoints.filter { $0.matchesSearchQuery(searchText) }
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.newPresetScale
            let columnMaxWidth = metrics.formMaxWidth
            let pagePadding = metrics.innerHorizontalPadding

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 0) {
                    HStack {
                        KikariaAdaptiveBackButton(metrics: metrics) {
                            dismiss()
                        }

                        Spacer()

                        Text("编辑预设")
                            .font(KikariaTypography.chineseHeadline(size: 17 * scale))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()

                        Button("保存") {
                            onSavePreset(preset.id, name, category)
                            dismiss()
                        }
                        .font(KikariaTypography.chineseButton(size: 17 * scale))
                        .foregroundStyle(KikariaTheme.sky)
                        .frame(width: metrics.adaptiveTopBarTrailingWidth, alignment: .trailing)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.top, 18 * scale)
                    .padding(.bottom, 16 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)

                    ScrollView {
                        VStack(spacing: 16) {
                        ProfileTextField(title: "预设名称", text: $name)
                        ProfileTextField(title: "分类", text: $category)

                        Button(action: exportMarkdown) {
                            Label("导出 Markdown", systemImage: "square.and.arrow.up")
                                .font(KikariaTypography.chineseButton())
                                .foregroundStyle(KikariaTheme.deepText)
                                .frame(maxWidth: .infinity)
                                .padding(.vertical, 15)
                                .liquidGlassCard(cornerRadius: 22, fillOpacity: 0.42, strokeOpacity: 0.38, shadowOpacity: 0.08, shadowRadius: 12, shadowY: 7)
                        }
                        .buttonStyle(.plain)

                        Button(action: onAddPoint) {
                            Label("添加知识点", systemImage: "plus.circle.fill")
                                .font(KikariaTypography.chineseButton())
                                .foregroundStyle(.white)
                                .frame(maxWidth: .infinity)
                                .padding(.vertical, 15)
                                .background(KikariaTheme.actionGradient, in: RoundedRectangle(cornerRadius: 22, style: .continuous))
                        }
                        .buttonStyle(.plain)

                        KikariaSearchBar(text: $searchText)

                        VStack(spacing: 12) {
                            if filteredKnowledgePoints.isEmpty {
                                SoftEmptyState(
                                    title: "没有找到相关知识点",
                                    subtitle: "换个关键词试试看。",
                                    systemImage: "magnifyingglass"
                                )
                                .padding(.vertical, 18)
                            } else {
                                ForEach(filteredKnowledgePoints) { point in
                                    HStack(spacing: 12) {
                                        VStack(alignment: .leading, spacing: 6) {
                                            KikariaTypography.mixedText(point.title, size: 16, weight: .semibold)
                                                .foregroundStyle(KikariaTheme.deepText)

                                            KikariaTypography.mixedText(point.tags.joined(separator: ", "), size: 12, weight: .semibold)
                                                .foregroundStyle(KikariaTheme.softText)
                                                .lineLimit(2)
                                        }

                                        Spacer()

                                        Button {
                                            onEditPoint(point.id)
                                        } label: {
                                            Image(systemName: "pencil")
                                                .font(.headline.weight(.semibold))
                                                .foregroundStyle(KikariaTheme.sky)
                                                .frame(width: 34, height: 34)
                                                .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.34, shadowOpacity: 0.05, shadowRadius: 7, shadowY: 3)
                                        }
                                        .buttonStyle(.plain)

                                        Button {
                                            pendingDeletePoint = point
                                        } label: {
                                            Image(systemName: "trash")
                                                .font(.headline.weight(.semibold))
                                                .foregroundStyle(KikariaTheme.removeCoral)
                                                .frame(width: 34, height: 34)
                                                .liquidGlassCircle(fillOpacity: 0.36, strokeOpacity: 0.34, shadowOpacity: 0.05, shadowRadius: 7, shadowY: 3)
                                        }
                                        .buttonStyle(.plain)
                                    }
                                    .padding(16)
                                    .liquidGlassCard(cornerRadius: 22, fillOpacity: 0.42, strokeOpacity: 0.36, shadowOpacity: 0.08, shadowRadius: 12, shadowY: 7)
                                }
                            }
                        }

                        if !preset.isBuiltIn {
                            Button(role: .destructive) {
                                isConfirmingPresetDelete = true
                            } label: {
                                Text("删除此预设")
                                    .font(KikariaTypography.chineseButton())
                                    .foregroundStyle(KikariaTheme.removeCoral)
                                    .frame(maxWidth: .infinity)
                                    .padding(.vertical, 15)
                                    .liquidGlassCard(cornerRadius: 22, fillOpacity: 0.42, strokeOpacity: 0.36, shadowOpacity: 0.08, shadowRadius: 12, shadowY: 7)
                            }
                            .buttonStyle(.plain)
                            .padding(.top, 6)
                        }
                        }
                        .padding(.horizontal, metrics.horizontalPadding)
                        .padding(.bottom, 34)
                        .frame(maxWidth: metrics.formMaxWidth)
                        .frame(maxWidth: .infinity)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
        .sheet(item: $shareFile) { file in
            ActivityView(activityItems: [file.url])
        }
        .alert("删除知识点？", isPresented: isConfirmingPointDelete) {
            Button("取消", role: .cancel) {
                pendingDeletePoint = nil
            }

            Button("删除", role: .destructive) {
                if let pendingDeletePoint {
                    onDeletePoint(pendingDeletePoint.id, preset.id)
                }

                pendingDeletePoint = nil
            }
        } message: {
            Text("删除后，该知识点的重点集锦、已掌握和今日复习次数也会一并移除。")
        }
        .alert("删除预设？", isPresented: $isConfirmingPresetDelete) {
            Button("取消", role: .cancel) {}
            Button("删除", role: .destructive) {
                onDeletePreset(preset.id)
                dismiss()
            }
        } message: {
            Text("此操作会删除该自定义预设和它的学习状态。")
        }
    }

    private var isConfirmingPointDelete: Binding<Bool> {
        Binding(
            get: { pendingDeletePoint != nil },
            set: { isPresented in
                if !isPresented {
                    pendingDeletePoint = nil
                }
            }
        )
    }

    private func exportMarkdown() {
        let markdown = KnowledgePoint.markdownText(from: knowledgePoints)
        let filename = "Kikaria-\(sanitizedFilename(preset.name)).md"
        let url = FileManager.default.temporaryDirectory.appendingPathComponent(filename)

        do {
            try markdown.write(to: url, atomically: true, encoding: .utf8)
            shareFile = ShareFile(url: url)
        } catch {
            showToast("导出失败")
        }
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct EditKnowledgePointView: View {
    @Environment(\.dismiss) private var dismiss
    let presetName: String
    let point: KnowledgePoint?
    let onSave: (KnowledgePoint) -> Void
    @State private var title: String
    @State private var tagsText: String
    @State private var hint: String
    @State private var content: String
    @State private var errorMessage: String?

    init(presetName: String, point: KnowledgePoint?, onSave: @escaping (KnowledgePoint) -> Void) {
        self.presetName = presetName
        self.point = point
        self.onSave = onSave
        _title = State(initialValue: point?.title ?? "")
        _tagsText = State(initialValue: point?.tags.joined(separator: ", ") ?? "")
        _hint = State(initialValue: point?.hint ?? "")
        _content = State(initialValue: point?.content ?? "")
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.newPresetScale
            let columnMaxWidth = metrics.formMaxWidth
            let pagePadding = metrics.innerHorizontalPadding

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 0) {
                    HStack {
                        KikariaAdaptiveBackButton(metrics: metrics) {
                            dismiss()
                        }

                        Spacer()

                        Text(point == nil ? "添加知识点" : "编辑知识点")
                            .font(KikariaTypography.chineseHeadline(size: 17 * scale))
                            .foregroundStyle(KikariaTheme.deepText)

                        Spacer()

                        Button("保存") {
                            savePoint()
                        }
                        .font(KikariaTypography.chineseButton(size: 17 * scale))
                        .foregroundStyle(KikariaTheme.sky)
                        .frame(width: metrics.adaptiveTopBarTrailingWidth, alignment: .trailing)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.top, 18 * scale)
                    .padding(.bottom, 16 * scale)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)

                    ScrollView {
                        VStack(spacing: 16) {
                            KikariaTypography.mixedText(presetName, size: 26, weight: .semibold)
                                .foregroundStyle(KikariaTheme.deepText)
                                .frame(maxWidth: .infinity, alignment: .leading)

                            ProfileTextField(title: "标题", text: $title)
                            ProfileTextField(title: "标签，用逗号分隔", text: $tagsText)

                            EditableLongTextField(title: "提示", text: $hint, minHeight: 150)
                            EditableLongTextField(title: "答案", text: $content, minHeight: 220)

                            if let errorMessage {
                                KikariaTypography.mixedText(errorMessage, size: 14, weight: .semibold)
                                    .foregroundStyle(KikariaTheme.removeCoral)
                                    .frame(maxWidth: .infinity, alignment: .leading)
                                    .padding(14)
                                    .liquidGlassCard(cornerRadius: 18, fillOpacity: 0.50, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                            }
                        }
                        .padding(.horizontal, metrics.horizontalPadding)
                        .padding(.bottom, 34)
                        .frame(maxWidth: metrics.formMaxWidth)
                        .frame(maxWidth: .infinity)
                    }
                }
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
    }

    private func savePoint() {
        let trimmedTitle = title.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedHint = hint.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedContent = content.trimmingCharacters(in: .whitespacesAndNewlines)
        let tags = tagsText
            .split(whereSeparator: { $0 == "," || $0 == "，" })
            .map { $0.trimmingCharacters(in: .whitespacesAndNewlines) }
            .filter { !$0.isEmpty }

        guard !trimmedTitle.isEmpty, !trimmedHint.isEmpty, !trimmedContent.isEmpty else {
            errorMessage = "标题、提示和答案都不能为空。"
            return
        }

        let now = Date()
        let savedPoint = KnowledgePoint(
            id: point?.id ?? UUID(),
            title: trimmedTitle,
            tags: tags,
            hint: trimmedHint,
            content: trimmedContent,
            isReinforced: point?.isReinforced ?? false,
            isMastered: point?.isMastered ?? false,
            createdAt: point?.createdAt ?? now,
            updatedAt: now,
            reinforcementCount: point?.reinforcementCount,
            lastReinforcedAt: point?.lastReinforcedAt
        )

        onSave(savedPoint)
        dismiss()
    }
}

private struct EditableLongTextField: View {
    let title: String
    @Binding var text: String
    let minHeight: CGFloat

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(title)
                .font(KikariaTypography.chineseHeadline(size: 14))
                .foregroundStyle(KikariaTheme.softText)

            TextEditor(text: $text)
                .font(.system(.body, design: .serif))
                .foregroundStyle(KikariaTheme.deepText)
                .kikariaHideScrollContentBackground()
                .kikariaMacClearTextEditorBackground()
                .padding(14)
                .frame(minHeight: minHeight)
                .liquidGlassCard(cornerRadius: 22, material: .thinMaterial, fillOpacity: 0.56, strokeOpacity: 0.32, shadowOpacity: 0.08, shadowRadius: 12, shadowY: 7)
        }
    }
}

private struct InitialProfileSetupView: View {
    @Binding var profile: UserProfile
    let onComplete: () -> Void
    @State private var displayName = ""
    @State private var userHandle = ""
    @State private var avatarImageData: Data?
    #if os(macOS)
    @State private var isImportingAvatar = false
    #endif
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    private var trimmedDisplayName: String {
        displayName.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private var canSave: Bool {
        !trimmedDisplayName.isEmpty
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let isExpanded = metrics.isPadWidth
            let setupCardMaxWidth: CGFloat = metrics.isPadPortrait ? 500 : (isExpanded ? 480 : 370)
            let avatarSize: CGFloat = metrics.isPadPortrait ? 116 : (isExpanded ? 108 : 88)

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                ScrollView(.vertical, showsIndicators: false) {
                    VStack(spacing: isExpanded ? 28 : 22) {
                        VStack(spacing: isExpanded ? 12 : 10) {
                            Text("欢迎使用 Kikaria")
                                .font(KikariaTypography.chineseTitle(size: isExpanded ? 34 : 30, weight: .bold))
                                .foregroundStyle(KikariaTheme.deepText)

                            Text("先设置你的个人资料")
                                .font(KikariaTypography.chineseBody(size: isExpanded ? 18 : 16, weight: .medium))
                                .foregroundStyle(KikariaTheme.softText)
                        }

                        ZStack(alignment: .bottomTrailing) {
                            ProfileAvatarView(
                                systemName: profile.avatarSystemName,
                                imageData: avatarImageData ?? profile.avatarImageData,
                                size: avatarSize
                            )

                            #if os(iOS)
                            PhotoPickerCompat { imageData in
                                if let imageData {
                                    avatarImageData = imageData
                                } else {
                                    showToast("头像加载失败")
                                }
                            } label: {
                                Image(systemName: "plus")
                                    .font(.system(size: isExpanded ? 17 : 15, weight: .bold))
                                    .foregroundStyle(.white)
                                    .frame(width: isExpanded ? 34 : 30, height: isExpanded ? 34 : 30)
                                    .background(KikariaTheme.actionGradient, in: Circle())
                                    .overlay {
                                        Circle()
                                            .stroke(.white.opacity(0.72), lineWidth: 1)
                                    }
                                    .shadow(color: KikariaTheme.sky.opacity(0.26), radius: 10, y: 5)
                            }
                            .offset(x: 3, y: 3)
                            .accessibilityLabel("选择头像")
                            #elseif os(macOS)
                            Button {
                                isImportingAvatar = true
                            } label: {
                                Image(systemName: "plus")
                                    .font(.system(size: isExpanded ? 17 : 15, weight: .bold))
                                    .foregroundStyle(.white)
                                    .frame(width: isExpanded ? 34 : 30, height: isExpanded ? 34 : 30)
                                    .background(KikariaTheme.actionGradient, in: Circle())
                                    .overlay {
                                        Circle()
                                            .stroke(.white.opacity(0.72), lineWidth: 1)
                                    }
                                    .shadow(color: KikariaTheme.sky.opacity(0.26), radius: 10, y: 5)
                            }
                            .buttonStyle(.plain)
                            .contentShape(Circle())
                            .offset(x: 3, y: 3)
                            .accessibilityLabel("选择头像")
                            #endif
                        }
                        .padding(.top, 4)

                        VStack(spacing: isExpanded ? 16 : 14) {
                            ProfileTextField(title: "昵称", text: $displayName, usesMacPlainFieldStyle: true)
                            ProfileTextField(title: "用户名", text: $userHandle, usesMacPlainFieldStyle: true)
                        }

                        Button(action: saveProfile) {
                            Text("开始使用")
                                .font(KikariaTypography.chineseButton(size: isExpanded ? 18 : 17))
                                .foregroundStyle(.white)
                                .frame(maxWidth: .infinity)
                                .padding(.vertical, isExpanded ? 18 : 16)
                                .background(KikariaTheme.actionGradient, in: Capsule())
                                .shadow(color: KikariaTheme.sky.opacity(canSave ? 0.22 : 0.04), radius: 16, y: 8)
                        }
                        .buttonStyle(.plain)
                        .disabled(!canSave)
                        .opacity(canSave ? 1 : 0.48)
                        .padding(.top, 4)
                    }
                    .padding(isExpanded ? 32 : 24)
                    .frame(maxWidth: setupCardMaxWidth)
                    .liquidGlassCard(cornerRadius: isExpanded ? 38 : 34, material: .regularMaterial, fillOpacity: 0.46, strokeOpacity: 0.46, shadowOpacity: 0.16, shadowRadius: isExpanded ? 28 : 24, shadowY: isExpanded ? 16 : 14)
                    .padding(.horizontal, metrics.horizontalPadding)
                    .frame(maxWidth: metrics.formMaxWidth)
                    .frame(maxWidth: .infinity)
                    .frame(minHeight: metrics.height, alignment: .center)
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                }
            }
        }
        .onAppear {
            if displayName.isEmpty {
                displayName = profile.displayName == "Vita" ? "" : profile.displayName
            }

            if userHandle.isEmpty {
                userHandle = profile.userHandle == "vita_0818" ? "" : profile.userHandle
            }

            if avatarImageData == nil {
                avatarImageData = profile.avatarImageData
            }
        }
        #if os(macOS)
        .fileImporter(isPresented: $isImportingAvatar, allowedContentTypes: [.image], allowsMultipleSelection: false) { result in
            importAvatarFile(result)
        }
        #endif
    }

    private func saveProfile() {
        guard canSave else {
            return
        }

        let trimmedHandle = userHandle
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .trimmingCharacters(in: CharacterSet(charactersIn: "@"))
        profile.displayName = trimmedDisplayName
        profile.userHandle = trimmedHandle.isEmpty ? generatedHandle(from: trimmedDisplayName) : trimmedHandle
        profile.avatarImageData = avatarImageData
        onComplete()
    }

    #if os(macOS)
    private func importAvatarFile(_ result: Result<[URL], Error>) {
        guard case .success(let urls) = result,
              let url = urls.first else {
            return
        }

        let didStartAccessing = url.startAccessingSecurityScopedResource()
        defer {
            if didStartAccessing {
                url.stopAccessingSecurityScopedResource()
            }
        }

        guard let data = try? Data(contentsOf: url),
              let compressedData = compressedAvatarData(from: data) else {
            showToast("头像加载失败")
            return
        }

        avatarImageData = compressedData
    }
    #endif

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.easeInOut(duration: 0.18)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeInOut(duration: 0.18)) {
                toastMessage = nil
            }
        }
    }

    private func generatedHandle(from name: String) -> String {
        let normalized = name
            .lowercased()
            .unicodeScalars
            .map { scalar in
                CharacterSet.alphanumerics.contains(scalar) ? String(scalar) : "_"
            }
            .joined()
            .trimmingCharacters(in: CharacterSet(charactersIn: "_"))

        return normalized.isEmpty ? "kikaria_user" : normalized
    }
}

    private struct EditProfileView: View {
        @Environment(\.dismiss) private var dismiss
        @Binding var profile: UserProfile
        @State private var displayName: String
        @State private var userHandle: String

    init(profile: Binding<UserProfile>) {
        _profile = profile
        _displayName = State(initialValue: profile.wrappedValue.displayName)
        _userHandle = State(initialValue: profile.wrappedValue.userHandle)
    }

    var body: some View {
        ZStack {
            KikariaTheme.pageGradient
                .ignoresSafeArea()

            VStack(spacing: 0) {
                HStack {
                    Button {
                        dismiss()
                    } label: {
                        Image(systemName: "chevron.left")
                            .font(.headline.weight(.semibold))
                            .foregroundStyle(KikariaTheme.deepText)
                            .frame(width: 42, height: 42)
                            .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.08, shadowRadius: 10, shadowY: 5)
                    }
                    .buttonStyle(.plain)

                    Spacer()

                    Text("编辑个人资料")
                        .font(KikariaTypography.chineseHeadline())
                        .foregroundStyle(KikariaTheme.deepText)

                    Spacer()

                    Button("保存") {
                        saveProfile()
                    }
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(KikariaTheme.sky)
                    .frame(width: 42, alignment: .trailing)
                }
                .padding(.horizontal, 24)
                .padding(.top, 18)
                .padding(.bottom, 18)

                ScrollView {
                    VStack(spacing: 24) {
                        VStack(spacing: 12) {
                            ProfileAvatarView(
                                systemName: profile.avatarSystemName,
                                imageData: profile.avatarImageData,
                                size: 92
                            )

                            #if os(iOS)
                            PhotoPickerCompat { imageData in
                                if let imageData {
                                    profile.avatarImageData = imageData
                                }
                            } label: {
                                Label("更换头像", systemImage: "photo")
                                    .font(KikariaTypography.chineseButton(size: 14))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .padding(.horizontal, 18)
                                    .padding(.vertical, 11)
                                    .liquidGlassCapsule(fillOpacity: 0.38, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                            }
                            .buttonStyle(.plain)
                            #elseif os(macOS)
                            Button {
                                // TODO: macOS 头像导入后续接入 NSOpenPanel；这里保留 iPad 版入口与布局。
                            } label: {
                                Label("更换头像", systemImage: "photo")
                                    .font(KikariaTypography.chineseButton(size: 14))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .padding(.horizontal, 18)
                                    .padding(.vertical, 11)
                                    .liquidGlassCapsule(fillOpacity: 0.38, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                            }
                            .buttonStyle(.plain)
                            #endif
                        }
                        .padding(.top, 12)

                        VStack(spacing: 14) {
                            ProfileTextField(
                                title: "显示名称",
                                text: $displayName
                            )

                            ProfileTextField(
                                title: "用户 ID",
                                text: $userHandle
                            )
                        }
                    }
                    .padding(.horizontal, 24)
                    .padding(.bottom, 34)
                }
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
    }

    private func saveProfile() {
        let trimmedName = displayName.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedHandle = userHandle
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .trimmingCharacters(in: CharacterSet(charactersIn: "@"))

        profile.displayName = trimmedName.isEmpty ? "Vita" : trimmedName
        profile.userHandle = trimmedHandle.isEmpty ? "vita_0818" : trimmedHandle
        dismiss()
    }

}

private struct ProfileTextField: View {
    let title: String
    @Binding var text: String
    var scale: CGFloat = 1
    var minHeight: CGFloat? = nil
    var usesMacPlainFieldStyle = false

    var body: some View {
        let resolvedScale = max(scale, 1)

        VStack(alignment: .leading, spacing: 8 * resolvedScale) {
            KikariaTypography.mixedText(title, size: 14 * resolvedScale, weight: .semibold)
                .foregroundStyle(KikariaTheme.softText)

            TextField(title, text: $text)
                .font(KikariaTypography.chineseBody(size: 16 * resolvedScale))
                .foregroundStyle(KikariaTheme.deepText)
                .textInputAutocapitalization(.never)
                .autocorrectionDisabled()
                .kikariaMacPlainTextFieldStyle(true)
                .padding(.horizontal, 16 * resolvedScale)
                .padding(.vertical, 15 * resolvedScale)
                .frame(minHeight: minHeight)
                .liquidGlassCard(cornerRadius: 20 * resolvedScale, fillOpacity: 0.50, strokeOpacity: 0.34, shadowOpacity: 0.08, shadowRadius: 12 * resolvedScale, shadowY: 7 * resolvedScale)
        }
    }
}

private struct MarkdownEditorView: View {
    @Environment(\.dismiss) private var dismiss
    @Binding var markdownText: String
    @Binding var knowledgePoints: [KnowledgePoint]
    @Binding var selectedTags: Set<String>
    @Binding var dailyReviewRecords: [KnowledgePoint.ID: DailyReviewRecord]
    @State private var draftText: String
    @State private var errorMessage: String?
    @State private var toastMessage: String?
    @State private var toastToken = UUID()

    init(
        markdownText: Binding<String>,
        knowledgePoints: Binding<[KnowledgePoint]>,
        selectedTags: Binding<Set<String>>,
        dailyReviewRecords: Binding<[KnowledgePoint.ID: DailyReviewRecord]>
    ) {
        _markdownText = markdownText
        _knowledgePoints = knowledgePoints
        _selectedTags = selectedTags
        _dailyReviewRecords = dailyReviewRecords
        _draftText = State(initialValue: markdownText.wrappedValue)
    }

    var body: some View {
        ZStack {
            KikariaTheme.pageGradient
                .ignoresSafeArea()

            VStack(spacing: 0) {
                HStack {
                    Button {
                        dismiss()
                    } label: {
                        Image(systemName: "chevron.left")
                            .font(.headline.weight(.semibold))
                            .foregroundStyle(KikariaTheme.deepText)
                            .frame(width: 42, height: 42)
                            .liquidGlassCircle(fillOpacity: 0.40, strokeOpacity: 0.42, shadowOpacity: 0.08, shadowRadius: 10, shadowY: 5)
                    }
                    .buttonStyle(.plain)

                    Spacer()

                    Text("知识点上传")
                        .font(KikariaTypography.chineseHeadline())
                        .foregroundStyle(KikariaTheme.deepText)

                    Spacer()

                    Button("应用") {
                        applyMarkdown()
                    }
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(KikariaTheme.sky)
                    .frame(width: 42, alignment: .trailing)
                }
                .padding(.horizontal, 24)
                .padding(.top, 18)
                .padding(.bottom, 16)

                VStack(alignment: .leading, spacing: 12) {
                    TextEditor(text: $draftText)
                        .font(.system(.body, design: .serif))
                        .foregroundStyle(KikariaTheme.deepText)
                        .kikariaHideScrollContentBackground()
                        .kikariaMacClearTextEditorBackground()
                        .padding(16)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                        .liquidGlassCard(cornerRadius: 26, material: .thinMaterial, fillOpacity: 0.56, strokeOpacity: 0.34, shadowOpacity: 0.12, shadowRadius: 18, shadowY: 10)

                    if let errorMessage {
                        Text(errorMessage)
                            .font(KikariaTypography.chineseBody(size: 14, weight: .semibold))
                            .foregroundStyle(KikariaTheme.removeCoral)
                            .padding(14)
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .liquidGlassCard(cornerRadius: 18, fillOpacity: 0.50, strokeOpacity: 0.36, shadowOpacity: 0.06, shadowRadius: 8, shadowY: 4)
                            .transition(.opacity.combined(with: .move(edge: .bottom)))
                    }
                }
                .padding(.horizontal, 24)
                .padding(.bottom, 24)
            }

            if let toastMessage {
                KikariaToastLayer(message: toastMessage)
                    .transition(.move(edge: .top).combined(with: .opacity))
            }
        }
        .navigationBarBackButtonHidden(true)
        .kikariaHiddenNavigationChrome()
    }

    private func applyMarkdown() {
        do {
            let parsedPoints = try KnowledgePoint.parseMarkdown(draftText)

            withAnimation(.spring(response: 0.36, dampingFraction: 0.9)) {
                markdownText = draftText
                knowledgePoints = parsedPoints
                selectedTags.removeAll()
                dailyReviewRecords.removeAll()
                errorMessage = nil
            }

            showToast("已更新 \(parsedPoints.count) 个知识点")
        } catch {
            withAnimation(.easeInOut(duration: 0.2)) {
                errorMessage = "没有解析到有效知识点。请检查 # 标题、hint: 和 content:。"
            }
        }
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct StartReviewButton: View {
    let dailyGoal: Int
    let masteredCount: Int
    let countdownDays: Int?
    var visualScale: CGFloat = 1
    @State private var isBreathing = false
    @State private var hasStartedBreathingAnimation = false
    private let orbitDuration: TimeInterval = 150

    var body: some View {
        let scale = max(visualScale, 0.1)

        TimelineView(.animation) { timeline in
            let orbitDegrees = orbitAngle(for: timeline.date)

            ZStack {
                ZStack {
                    DecorativeBubble(
                        size: 92 * scale,
                        colors: [KikariaTheme.cyan, KikariaTheme.bubbleMint],
                        opacity: 0.48
                    )
                    .rotationEffect(.degrees(-orbitDegrees))
                    .scaleEffect(isBreathing ? 1.035 : 0.985)
                    .offset(x: -96 * scale, y: -68 * scale)

                    DecorativeBubble(
                        size: 80 * scale,
                        colors: [KikariaTheme.bubbleLavender, KikariaTheme.mist],
                        opacity: 0.42
                    )
                    .rotationEffect(.degrees(-orbitDegrees))
                    .scaleEffect(isBreathing ? 0.985 : 1.04)
                    .offset(x: 102 * scale, y: -56 * scale)

                    DecorativeBubble(
                        size: 78 * scale,
                        colors: [KikariaTheme.bubbleGreen, KikariaTheme.cyan],
                        opacity: 0.38
                    )
                    .rotationEffect(.degrees(-orbitDegrees))
                    .scaleEffect(isBreathing ? 1.035 : 0.985)
                    .offset(x: 92 * scale, y: 80 * scale)

                    DecorativeBubble(
                        size: 74 * scale,
                        colors: [KikariaTheme.sky, KikariaTheme.bubbleWhite],
                        opacity: 0.36
                    )
                    .rotationEffect(.degrees(-orbitDegrees))
                    .scaleEffect(isBreathing ? 0.99 : 1.045)
                    .offset(x: -106 * scale, y: 78 * scale)
                }
                .rotationEffect(.degrees(orbitDegrees))

                Circle()
                    .fill(KikariaTheme.actionGradient)
                    .frame(width: 190 * scale, height: 190 * scale)
                    .background(.ultraThinMaterial, in: Circle())
                    .shadow(color: KikariaTheme.sky.opacity(0.28), radius: 28 * scale, x: 0, y: 18 * scale)
                    .scaleEffect(isBreathing ? 1.018 : 0.992)
                    .overlay {
                        Circle()
                            .fill(
                                RadialGradient(
                                    colors: [
                                        Color.white.opacity(0.30),
                                        Color.white.opacity(0.10),
                                        Color.white.opacity(0.02)
                                    ],
                                    center: .topLeading,
                                    startRadius: 12 * scale,
                                    endRadius: 150 * scale
                                )
                            )
                            .padding(scale)
                    }
                    .overlay {
                        Circle()
                            .stroke(
                                LinearGradient(
                                    colors: [
                                        Color.white.opacity(0.48),
                                        Color.white.opacity(0.12),
                                        KikariaTheme.cyan.opacity(0.22)
                                    ],
                                    startPoint: .topLeading,
                                    endPoint: .bottomTrailing
                                ),
                                lineWidth: 1.1 * scale
                            )
                    }

                Image(systemName: "arrow.right")
                    .font(.system(size: 70 * scale, weight: .regular))
                    .foregroundStyle(.white.opacity(0.96))
                    .shadow(color: KikariaTheme.deepText.opacity(0.10), radius: 8 * scale, y: 4 * scale)
            }
        }
        .frame(width: 272 * scale, height: 260 * scale)
        .scaleEffect(isBreathing ? 1.012 : 0.996)
        .offset(y: (isBreathing ? -5 : 2) * scale)
        .onAppear {
            guard !hasStartedBreathingAnimation else {
                return
            }

            hasStartedBreathingAnimation = true
            withAnimation(.easeInOut(duration: 5.4).repeatForever(autoreverses: true)) {
                isBreathing = true
            }
        }
    }

    private func orbitAngle(for date: Date) -> Double {
        let progress = date.timeIntervalSinceReferenceDate
            .truncatingRemainder(dividingBy: orbitDuration) / orbitDuration
        return progress * 360
    }
}

private struct SoftBubble: View {
    let size: CGFloat
    let colors: [Color]
    let opacity: Double

    var body: some View {
        Circle()
            .fill(
                LinearGradient(
                    colors: colors.map { $0.opacity(opacity) },
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
            .frame(width: size, height: size)
            .background(.ultraThinMaterial, in: Circle())
            .overlay {
                Circle()
                    .fill(
                        RadialGradient(
                            colors: [
                                Color.white.opacity(0.24),
                                Color.white.opacity(0.05),
                                Color.clear
                            ],
                            center: .topLeading,
                            startRadius: 4,
                            endRadius: size * 0.72
                        )
                    )
            }
            .overlay {
                Circle()
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(0.42),
                                Color.white.opacity(0.08),
                                KikariaTheme.cyan.opacity(0.16)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 1
                    )
            }
            .shadow(color: KikariaTheme.sky.opacity(0.10), radius: 14, y: 8)
    }
}

private struct DecorativeBubble: View {
    let size: CGFloat
    let colors: [Color]
    let opacity: Double

    var body: some View {
        SoftBubble(size: size, colors: colors, opacity: opacity)
            .accessibilityHidden(true)
    }
}

private struct HomeEntryCard: View {
    let title: String
    let countText: String

    var body: some View {
        HStack(spacing: 14) {
            Text(title)
                .font(KikariaTypography.chineseHeadline(size: 20))
                .foregroundStyle(KikariaTheme.deepText)

            Spacer()

            KikariaTypography.mixedText(countText, size: 20, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.sky)

            Image(systemName: "chevron.right")
                .font(.subheadline.weight(.semibold))
                .foregroundStyle(KikariaTheme.blueGray)
        }
        .padding(.horizontal, 22)
        .padding(.vertical, 22)
        .frame(maxWidth: .infinity)
        .background {
            RoundedRectangle(cornerRadius: 28, style: .continuous)
                .fill(KikariaTheme.glassSurface.opacity(0.46))
        }
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 28, style: .continuous))
        .shadow(color: KikariaTheme.sky.opacity(0.12), radius: 18, y: 10)
    }
}

private struct TodayOverviewHomeProgressButton: View {
    let dateText: String
    let daysLeftText: String
    let progressText: String
    var isExpanded = false
    var cardScale: CGFloat = 1

    var body: some View {
        let scale = max(cardScale, 1)

        HStack(alignment: .center, spacing: (isExpanded ? 18 : 14) * scale) {
            VStack(alignment: .leading, spacing: (isExpanded ? 6 : 5) * scale) {
                KikariaTypography.mixedText(dateText, size: (isExpanded ? 27 : 23) * scale, weight: .semibold)
                    .foregroundStyle(KikariaTheme.deepText)
                    .lineLimit(1)
                    .minimumScaleFactor(0.78)

                KikariaTypography.mixedText(daysLeftText, size: (isExpanded ? 14 : 13) * scale, weight: .semibold)
                    .foregroundStyle(KikariaTheme.softText)
                    .lineLimit(1)
            }

            Spacer(minLength: 12 * scale)

            KikariaTypography.mixedText(progressText, size: (isExpanded ? 30 : 25) * scale, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.masteredDeepGreen)
                .lineLimit(1)

            Image(systemName: "chevron.right")
                .font(.system(size: (isExpanded ? 15 : 12) * scale, weight: .semibold))
                .foregroundStyle(KikariaTheme.blueGray.opacity(0.52))
        }
        .padding(.horizontal, (isExpanded ? 24 : 20) * scale)
        .padding(.vertical, (isExpanded ? 24 : 20) * scale)
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: (isExpanded ? 28 : 25) * scale, fillOpacity: 0.42, strokeOpacity: 0.46, shadowOpacity: 0.11, shadowRadius: (isExpanded ? 19 : 17) * scale, shadowY: (isExpanded ? 10 : 9) * scale)
    }
}

private struct HomeDashboardGridCard: View {
    let scopeCountText: String
    let reinforcedCount: Int
    let masteredCount: Int
    let presetName: String
    var isExpanded = false
    var cardScale: CGFloat = 1

    var body: some View {
        let scale = max(cardScale, 1)

        VStack(spacing: 0) {
            HStack(spacing: 0) {
                routeLink(to: .scope) {
                    HomeDashboardMetricColumn(title: "范围", valueText: scopeCountText, tint: KikariaTheme.sky, isExpanded: isExpanded, cardScale: scale)
                }
                .buttonStyle(.plain)

                HomeDashboardDivider(isExpanded: isExpanded, cardScale: scale)

                routeLink(to: .reinforcement) {
                    HomeDashboardMetricColumn(title: "重点集锦", valueText: "\(reinforcedCount)", tint: KikariaTheme.cyan, isExpanded: isExpanded, cardScale: scale)
                }
                .buttonStyle(.plain)

                HomeDashboardDivider(isExpanded: isExpanded, cardScale: scale)

                routeLink(to: .mastered) {
                    HomeDashboardMetricColumn(title: "已掌握", valueText: "\(masteredCount)", tint: KikariaTheme.masteredGreen, isExpanded: isExpanded, cardScale: scale)
                }
                .buttonStyle(.plain)
            }

            Rectangle()
                .fill(KikariaTheme.blueGray.opacity(0.12))
                .frame(height: 1)
                .padding(.horizontal, 18 * scale)

            routeLink(to: .presetSelection) {
                HStack(spacing: 8 * scale) {
                    KikariaTypography.mixedText(presetName, size: (isExpanded ? 18 : 16) * scale, weight: .semibold)
                        .foregroundStyle(KikariaTheme.deepText)
                        .lineLimit(1)
                        .minimumScaleFactor(0.74)

                    Text("当前预设")
                        .font(KikariaTypography.chineseCaption(size: (isExpanded ? 13 : 12) * scale, weight: .semibold))
                        .foregroundStyle(KikariaTheme.softText)

                    Spacer()

                    Image(systemName: "chevron.right")
                        .font(.system(size: 12 * scale, weight: .semibold))
                        .foregroundStyle(KikariaTheme.blueGray.opacity(0.58))
                }
                .padding(.horizontal, (isExpanded ? 24 : 20) * scale)
                .frame(maxWidth: .infinity, minHeight: (isExpanded ? 64 : 56) * scale)
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
        }
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: (isExpanded ? 30 : 28) * scale, fillOpacity: 0.40, strokeOpacity: 0.44, shadowOpacity: 0.12, shadowRadius: (isExpanded ? 20 : 18) * scale, shadowY: (isExpanded ? 11 : 10) * scale)
    }
}

private struct HomeDashboardMetricColumn: View {
    let title: String
    let valueText: String
    let tint: Color
    var isExpanded = false
    var cardScale: CGFloat = 1

    var body: some View {
        let scale = max(cardScale, 1)

        VStack(spacing: (isExpanded ? 10 : 8) * scale) {
            Text(title)
                .font(KikariaTypography.chineseCaption(size: (isExpanded ? 14 : 13) * scale, weight: .semibold))
                .foregroundStyle(KikariaTheme.softText)
                .lineLimit(1)
                .minimumScaleFactor(0.75)

            KikariaTypography.mixedText(valueText, size: (isExpanded ? 29 : 24) * scale, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(tint)
                .lineLimit(1)
                .minimumScaleFactor(0.72)
        }
        .frame(maxWidth: .infinity, minHeight: (isExpanded ? 98 : 82) * scale)
        .contentShape(Rectangle())
    }
}

private struct HomeDashboardDivider: View {
    var isExpanded = false
    var cardScale: CGFloat = 1

    var body: some View {
        let scale = max(cardScale, 1)

        Rectangle()
            .fill(KikariaTheme.blueGray.opacity(0.16))
            .frame(width: 1, height: (isExpanded ? 50 : 42) * scale)
    }
}

private struct PadPortraitHomeProgressCard: View {
    let dateText: String
    let daysLeftText: String
    let progressText: String

    var body: some View {
        HStack(alignment: .center, spacing: 28) {
            VStack(alignment: .leading, spacing: 9) {
                KikariaTypography.mixedText(dateText, size: 42, weight: .semibold)
                    .foregroundStyle(KikariaTheme.deepText)
                    .lineLimit(1)
                    .minimumScaleFactor(0.82)

                KikariaTypography.mixedText(daysLeftText, size: 17, weight: .semibold)
                    .foregroundStyle(KikariaTheme.softText)
                    .lineLimit(1)
            }

            Spacer(minLength: 24)

            KikariaTypography.mixedText(progressText, size: 54, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.masteredDeepGreen)
                .lineLimit(1)
                .minimumScaleFactor(0.76)

            Image(systemName: "chevron.right")
                .font(.system(size: 17, weight: .semibold))
                .foregroundStyle(KikariaTheme.blueGray.opacity(0.54))
        }
        .padding(.horizontal, 36)
        .padding(.vertical, 30)
        .frame(maxWidth: .infinity, minHeight: 136)
        .contentShape(RoundedRectangle(cornerRadius: 40, style: .continuous))
        .liquidGlassCard(cornerRadius: 40, fillOpacity: 0.42, strokeOpacity: 0.46, shadowOpacity: 0.12, shadowRadius: 24, shadowY: 14)
    }
}

private struct PadPortraitHomeDashboardCard: View {
    let scopeCountText: String
    let reinforcedCount: Int
    let masteredCount: Int
    let presetName: String

    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 0) {
                routeLink(to: .scope) {
                    PadPortraitHomeMetricColumn(
                        title: "范围",
                        valueText: scopeCountText,
                        tint: KikariaTheme.sky
                    )
                }
                .buttonStyle(.plain)

                PadPortraitHomeDashboardDivider()

                routeLink(to: .reinforcement) {
                    PadPortraitHomeMetricColumn(
                        title: "重点集锦",
                        valueText: "\(reinforcedCount)",
                        tint: KikariaTheme.cyan
                    )
                }
                .buttonStyle(.plain)

                PadPortraitHomeDashboardDivider()

                routeLink(to: .mastered) {
                    PadPortraitHomeMetricColumn(
                        title: "已掌握",
                        valueText: "\(masteredCount)",
                        tint: KikariaTheme.masteredGreen
                    )
                }
                .buttonStyle(.plain)
            }
            .frame(minHeight: 154)

            Rectangle()
                .fill(KikariaTheme.blueGray.opacity(0.12))
                .frame(height: 1)
                .padding(.horizontal, 30)

            routeLink(to: .presetSelection) {
                HStack(spacing: 11) {
                    KikariaTypography.mixedText(presetName, size: 22, weight: .semibold)
                        .foregroundStyle(KikariaTheme.deepText)
                        .lineLimit(1)
                        .minimumScaleFactor(0.72)

                    Text("当前预设")
                        .font(KikariaTypography.chineseCaption(size: 15, weight: .semibold))
                        .foregroundStyle(KikariaTheme.softText)
                        .lineLimit(1)

                    Spacer(minLength: 18)

                    Image(systemName: "chevron.right")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundStyle(KikariaTheme.blueGray.opacity(0.58))
                }
                .padding(.horizontal, 34)
                .frame(maxWidth: .infinity, minHeight: 82)
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
        }
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: 40, fillOpacity: 0.40, strokeOpacity: 0.44, shadowOpacity: 0.12, shadowRadius: 24, shadowY: 14)
    }
}

private struct PadPortraitHomeMetricColumn: View {
    let title: String
    let valueText: String
    let tint: Color

    var body: some View {
        VStack(spacing: 12) {
            Text(title)
                .font(KikariaTypography.chineseCaption(size: 17, weight: .semibold))
                .foregroundStyle(KikariaTheme.softText)
                .lineLimit(1)
                .minimumScaleFactor(0.74)

            KikariaTypography.mixedText(valueText, size: 46, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(tint)
                .lineLimit(1)
                .minimumScaleFactor(0.70)
        }
        .frame(maxWidth: .infinity, minHeight: 154)
        .contentShape(Rectangle())
    }
}

private struct PadPortraitHomeDashboardDivider: View {
    var body: some View {
        Rectangle()
            .fill(KikariaTheme.blueGray.opacity(0.16))
            .frame(width: 1, height: 82)
    }
}

struct ScopeSelectionView: View {
    @Environment(\.dismiss) private var dismiss
    @Binding var selectedTags: Set<String>
    let knowledgePoints: [KnowledgePoint]
    let allTags: [String]
    var onDone: (() -> Void)? = nil
    @State private var searchText = ""

    private var filteredTags: [String] {
        let query = searchText.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !query.isEmpty else {
            return allTags
        }

        let relevantTags = Set(
            knowledgePoints
                .filter { $0.matchesSearchQuery(query) }
                .flatMap(\.tags)
        )

        return allTags.filter { tag in
            tag.range(of: query, options: [.caseInsensitive, .diacriticInsensitive]) != nil ||
                relevantTags.contains(tag)
        }
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let scale = metrics.scopeScale
            let columnMaxWidth = metrics.scopeOuterMaxWidth
            let pagePadding = metrics.innerHorizontalPadding
            let gridSpacing = metrics.scopeGridSpacing
            let doneButtonBottomPadding = metrics.isPadPortrait ? 18 * scale + 18 : 18 * scale
            let effectiveContentWidth = metrics.effectiveContentWidth(for: columnMaxWidth)
            let titleFontSize = metrics.pageTitleFontSize(defaultValue: 32 * scale)
            let titleTopPadding = metrics.pageTitleTopPadding(defaultValue: 24 * scale + metrics.ipadPortraitListPageTopInset)
            let titleSpacing = metrics.pageTitleSpacing(defaultValue: 24 * scale)
            let subtitleSpacing = metrics.pageTitleSubtitleSpacing(defaultValue: 8 * scale)
            let tagMinimumWidth = metrics.isPadPortrait
                ? min(max(effectiveContentWidth / 4, metrics.scopeGridMinimumWidth), 190)
                : metrics.scopeGridMinimumWidth
            let columns = [
                GridItem(.adaptive(minimum: tagMinimumWidth), spacing: gridSpacing)
            ]

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                VStack(spacing: 0) {
                    ScrollView {
                        VStack(alignment: .leading, spacing: titleSpacing) {
                            VStack(alignment: .leading, spacing: subtitleSpacing) {
                                Text("选择范围")
                                    .font(KikariaTypography.chineseTitle(size: titleFontSize))
                                    .foregroundStyle(KikariaTheme.deepText)

                                KikariaTypography.mixedText(selectedTags.isEmpty ? "未选择标签时，会默认使用全部知识点。" : "已选择 \(selectedTags.count) 个标签。", size: 15 * scale)
                                    .foregroundStyle(KikariaTheme.softText)
                            }
                            .padding(.top, metrics.isPadPortrait ? 0 : 16 * scale)

                            KikariaSearchBar(text: $searchText, placeholder: "搜索标签或知识点", scale: scale)

                            if filteredTags.isEmpty {
                                SoftEmptyState(
                                    title: "没有找到相关标签",
                                    subtitle: "换个关键词试试看。",
                                    systemImage: "magnifyingglass"
                                )
                                .padding(.top, 18 * scale)
                            } else {
                                LazyVGrid(columns: columns, spacing: gridSpacing) {
                                    ForEach(filteredTags, id: \.self) { tag in
                                        Button {
                                            toggleTag(tag)
                                        } label: {
                                            ScopeTagChip(
                                                title: tag,
                                                isSelected: selectedTags.contains(tag),
                                                scale: scale
                                            )
                                        }
                                        .buttonStyle(.plain)
                                    }
                                }
                            }
                        }
                        .padding(.horizontal, pagePadding)
                        .padding(.top, titleTopPadding)
                        .padding(.bottom, 96)
                        .frame(maxWidth: columnMaxWidth)
                        .frame(maxWidth: .infinity)
                    }

                    Button {
                        if let onDone {
                            onDone()
                        } else {
                            dismiss()
                        }
                    } label: {
                        Text("完成")
                            .font(KikariaTypography.chineseButton(size: 17 * scale))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 18 * scale)
                            .foregroundStyle(.white)
                            .background(KikariaTheme.actionGradient, in: Capsule())
                            .shadow(color: KikariaTheme.sky.opacity(0.22), radius: 18 * scale, y: 9 * scale)
                    }
                    .padding(.horizontal, pagePadding)
                    .padding(.bottom, doneButtonBottomPadding)
                    .frame(maxWidth: columnMaxWidth)
                    .frame(maxWidth: .infinity)
                }
            }
            .kikariaAdaptiveNavigationChrome(metrics: metrics, outerMaxWidth: columnMaxWidth)
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func toggleTag(_ tag: String) {
        if selectedTags.contains(tag) {
            selectedTags.remove(tag)
        } else {
            selectedTags.insert(tag)
        }
    }
}

private struct ScopeTagChip: View {
    let title: String
    let isSelected: Bool
    var scale: CGFloat = 1

    var body: some View {
        let resolvedScale = max(scale, 1)
        let shape = RoundedRectangle(cornerRadius: 20 * resolvedScale, style: .continuous)

        KikariaTypography.mixedText(title, size: 13 * resolvedScale, weight: .semibold)
            .foregroundStyle(isSelected ? .white : KikariaTheme.deepText)
            .lineLimit(2)
            .minimumScaleFactor(0.82)
            .frame(maxWidth: .infinity, minHeight: 54 * resolvedScale)
            .padding(.horizontal, 14 * resolvedScale)
            .background {
                if isSelected {
                    shape
                        .fill(KikariaTheme.actionGradient)
                } else {
                    shape
                        .fill(KikariaTheme.glassSurface.opacity(0.34))
                }
            }
            .background(.ultraThinMaterial, in: shape)
            .overlay {
                shape
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(isSelected ? 0.36 : 0.30),
                                KikariaTheme.cyan.opacity(isSelected ? 0.62 : 0.22)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: 1.2 * resolvedScale
                    )
            }
            .shadow(color: KikariaTheme.sky.opacity(isSelected ? 0.18 : 0.06), radius: 12 * resolvedScale, y: 7 * resolvedScale)
    }
}

private enum ReviewGestureOwner: Equatable {
    case undecided
    case formulaHorizontal
    case cardVerticalScroll
    case fullScreenNavigation
    case ignored
}

private enum ReviewDragAxis: Equatable {
    case horizontal
    case vertical
}

private enum ReviewScrollCoordinateSpace {
    static let readingContent = "review-reading-content-scroll"
}

private struct ReviewScrollContentMetrics: Equatable {
    var contentHeight: CGFloat = 0
}

private struct ReviewScrollContentMetricsPreferenceKey: PreferenceKey {
    static var defaultValue = ReviewScrollContentMetrics()

    static func reduce(value: inout ReviewScrollContentMetrics, nextValue: () -> ReviewScrollContentMetrics) {
        value = nextValue()
    }
}

private struct ReviewScrollMetricsReader: View {
    var body: some View {
        GeometryReader { proxy in
            Color.clear.preference(
                key: ReviewScrollContentMetricsPreferenceKey.self,
                value: ReviewScrollContentMetrics(
                    contentHeight: proxy.size.height
                )
            )
        }
    }
}

private struct ReviewCardFramePreferenceKey: PreferenceKey {
    static var defaultValue: [CGRect] = []

    static func reduce(value: inout [CGRect], nextValue: () -> [CGRect]) {
        value.append(contentsOf: nextValue())
    }
}

private struct ReviewContainerFramePreferenceKey: PreferenceKey {
    static var defaultValue: CGRect = .zero

    static func reduce(value: inout CGRect, nextValue: () -> CGRect) {
        value = nextValue()
    }
}

private struct ReviewCardFrameReader: View {
    var body: some View {
        GeometryReader { proxy in
            Color.clear.preference(
                key: ReviewCardFramePreferenceKey.self,
                value: [proxy.frame(in: .global)]
            )
        }
    }
}

private struct ReviewContainerFrameReader: View {
    var body: some View {
        GeometryReader { proxy in
            Color.clear.preference(
                key: ReviewContainerFramePreferenceKey.self,
                value: proxy.frame(in: .global)
            )
        }
    }
}

private struct ReviewScrollState {
    var viewportHeight: CGFloat = 0
    var contentHeight: CGFloat = 0

    var maxScrollOffset: CGFloat {
        max(0, contentHeight - viewportHeight)
    }

    var isScrollable: Bool {
        maxScrollOffset > 2
    }

    var hasMetrics: Bool {
        viewportHeight > 0 && contentHeight > 0
    }

    func needsUpdate(contentMetrics: ReviewScrollContentMetrics, viewportHeight: CGFloat) -> Bool {
        abs(self.viewportHeight - viewportHeight) > 0.5 ||
            abs(contentHeight - contentMetrics.contentHeight) > 0.5
    }

    mutating func update(contentMetrics: ReviewScrollContentMetrics, viewportHeight: CGFloat) {
        self.viewportHeight = viewportHeight
        self.contentHeight = contentMetrics.contentHeight
    }
}

#if os(macOS)
private struct KikariaMacReviewKeyboardHandler: NSViewRepresentable {
    var isEnabled: Bool
    let onSpace: () -> Void
    let onReturn: () -> Void
    let onMastered: () -> Void
    let onReinforcement: () -> Void

    func makeNSView(context: Context) -> KeyCatcherView {
        let view = KeyCatcherView()
        view.configure(
            isEnabled: isEnabled,
            onSpace: onSpace,
            onReturn: onReturn,
            onMastered: onMastered,
            onReinforcement: onReinforcement
        )
        return view
    }

    func updateNSView(_ nsView: KeyCatcherView, context: Context) {
        nsView.configure(
            isEnabled: isEnabled,
            onSpace: onSpace,
            onReturn: onReturn,
            onMastered: onMastered,
            onReinforcement: onReinforcement
        )
    }

    final class KeyCatcherView: NSView {
        private var localMonitor: Any?
        private var isEnabled = false
        private var onSpace: () -> Void = {}
        private var onReturn: () -> Void = {}
        private var onMastered: () -> Void = {}
        private var onReinforcement: () -> Void = {}

        deinit {
            removeLocalMonitor()
        }

        override func viewDidMoveToWindow() {
            super.viewDidMoveToWindow()
            updateLocalMonitor()
        }

        func configure(
            isEnabled: Bool,
            onSpace: @escaping () -> Void,
            onReturn: @escaping () -> Void,
            onMastered: @escaping () -> Void,
            onReinforcement: @escaping () -> Void
        ) {
            self.isEnabled = isEnabled
            self.onSpace = onSpace
            self.onReturn = onReturn
            self.onMastered = onMastered
            self.onReinforcement = onReinforcement
            updateLocalMonitor()
        }

        private func updateLocalMonitor() {
            guard window != nil else {
                removeLocalMonitor()
                return
            }

            guard localMonitor == nil else {
                return
            }

            localMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
                self?.handle(event) ?? event
            }
        }

        private func removeLocalMonitor() {
            if let localMonitor {
                NSEvent.removeMonitor(localMonitor)
                self.localMonitor = nil
            }
        }

        private func handle(_ event: NSEvent) -> NSEvent? {
            guard isEnabled,
                  let window,
                  event.window === window,
                  !Self.isTextInputFocused
            else {
                return event
            }

            let ignoredModifiers: NSEvent.ModifierFlags = [.command, .control, .option]
            guard event.modifierFlags.intersection(ignoredModifiers).isEmpty else {
                return event
            }

            guard let characters = event.charactersIgnoringModifiers?.lowercased(),
                  !characters.isEmpty
            else {
                return event
            }

            switch characters {
            case " ":
                onSpace()
                return nil
            case "\r", "\n":
                onReturn()
                return nil
            case "k", "m":
                onMastered()
                return nil
            case "l", ";", "'":
                onReinforcement()
                return nil
            default:
                return event
            }
        }

        private static var isTextInputFocused: Bool {
            guard let firstResponder = NSApp.keyWindow?.firstResponder else {
                return false
            }

            return firstResponder is NSTextView || firstResponder is NSTextField
        }
    }
}
#endif

struct ReviewView: View {
    @Environment(\.dismiss) private var dismiss
    @Binding var knowledgePoints: [KnowledgePoint]
    @Binding var selectedTags: Set<String>
    @Binding var dailyReviewRecords: [KnowledgePoint.ID: DailyReviewRecord]
    let mode: ReviewMode
    let onRecordActivity: (StudyActivityType, KnowledgePoint) -> Void
    var onReturnHome: (() -> Void)?

    @State private var currentPointID: KnowledgePoint.ID?
    @State private var isShowingHint = false
    @State private var isShowingContent = false
    @State private var reviewQueue: [KnowledgePoint.ID] = []
    @State private var reviewQueueIndex = 0
    @State private var lastQueuePointID: KnowledgePoint.ID?
    @State private var gestureFeedback = false
    @State private var isShowingScopePanel = false
    @State private var toastMessage: String?
    @State private var toastToken = UUID()
    @State private var reviewScrollState = ReviewScrollState()
    @State private var reviewGestureOwner = ReviewGestureOwner.undecided
    @State private var reviewCardFrames: [CGRect] = []
    @State private var reviewFormulaFrames: [CGRect] = []
    @State private var reviewContainerFrame = CGRect.zero
    @State private var isReviewPointTransitioning = false
    @State private var reviewPointTransitionToken = UUID()
    @State private var isReviewTextVisible = true
    @State private var matchingPointIDsCache: [KnowledgePoint.ID] = []
    @State private var hasPreparedReviewQueue = false
    @State private var pendingAnswerRevealToken: UUID?

    private var allTags: [String] {
        Array(Set(knowledgePoints.flatMap(\.tags))).sorted()
    }

    private var matchingPointIDs: [KnowledgePoint.ID] {
        matchingPointIDsCache
    }

    private var currentPoint: KnowledgePoint? {
        guard let currentPointID else {
            return nil
        }

        return knowledgePoints.first { $0.id == currentPointID }
    }

    private func makeMatchingPointIDs() -> [KnowledgePoint.ID] {
        switch mode {
        case .normal:
            if selectedTags.isEmpty {
                return knowledgePoints.map(\.id)
            }

            return knowledgePoints
                .filter { point in
                    point.tags.contains { selectedTags.contains($0) }
                }
                .map(\.id)
        case .reinforcement:
            return knowledgePoints
                .filter { $0.reinforcementCount > 0 }
                .map(\.id)
        case .mastered:
            return knowledgePoints
                .filter(\.isMastered)
                .map(\.id)
        }
    }

    private func refreshMatchingPointIDs() {
        matchingPointIDsCache = makeMatchingPointIDs()
        hasPreparedReviewQueue = true
    }

    private var revealAnimation: Animation {
        .spring(response: 0.42, dampingFraction: 0.88)
    }

    private var reviewUnusedComponentFadeOutDuration: TimeInterval {
        0.18
    }

    private var reviewTextFadeInDuration: TimeInterval {
        0.20
    }

    private var reviewPointSwapDelay: TimeInterval {
        reviewUnusedComponentFadeOutDuration + 0.03
    }

    private var reviewUnusedComponentFadeOutAnimation: Animation {
        .easeOut(duration: reviewUnusedComponentFadeOutDuration)
    }

    private var reviewTextFadeInAnimation: Animation {
        .easeIn(duration: reviewTextFadeInDuration)
    }

    private var reviewEphemeralStateTransition: AnyTransition {
        .asymmetric(
            insertion: .opacity.combined(with: .scale(scale: 0.98)),
            removal: .opacity
        )
    }

    private var reviewLongAnswerRevealAnimation: Animation {
        .easeInOut(duration: 0.24)
    }

    private var reviewDeferredAnswerRevealDelay: TimeInterval {
        0.05
    }

    private var pointTransitionDelay: TimeInterval {
        0.36
    }

    private func usesLightweightAnswerReveal(for point: KnowledgePoint) -> Bool {
        point.content.count >= 180 ||
            point.content.contains("$") ||
            point.content.contains("\n")
    }

    private func answerRevealAnimation(for point: KnowledgePoint) -> Animation? {
        guard !isReviewPointTransitioning else {
            return nil
        }

        return usesLightweightAnswerReveal(for: point) ? reviewLongAnswerRevealAnimation : revealAnimation
    }

    private func answerTransition(for point: KnowledgePoint) -> AnyTransition {
        usesLightweightAnswerReveal(for: point) ? .opacity : reviewEphemeralStateTransition
    }

    private func prewarmReviewMathText(for point: KnowledgePoint) {
        let hint = point.hint
        let content = point.content

        DispatchQueue.global(qos: .utility).async {
            KikariaMathText.prewarm(hint)
            KikariaMathText.prewarm(content)
        }
    }

    private func revealButtons(
        isExpanded: Bool,
        buttonScale: CGFloat,
        isInteractive: Bool
    ) -> some View {
        let minButtonHeight: CGFloat? = isExpanded ? 76 * buttonScale : nil

        return VStack(spacing: (isExpanded ? 16 : 14) * buttonScale) {
            ReviewActionButton(
                title: "查看提示",
                systemImage: "lightbulb",
                isPrimary: false,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                minHeight: minButtonHeight,
                shortcutHints: [.space]
            ) {
                withAnimation(revealAnimation) {
                    revealHint()
                }
            }
            .opacity(isShowingHint ? 0 : 1)
            .allowsHitTesting(isInteractive && !isShowingHint)

            ReviewActionButton(
                title: "查看答案",
                systemImage: "doc.text",
                isPrimary: true,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                minHeight: minButtonHeight,
                shortcutHints: [.space, .returnKey]
            ) {
                withAnimation(revealAnimation) {
                    revealContent()
                }
            }
        }
        .animation(isReviewPointTransitioning ? nil : .easeInOut(duration: 0.18), value: isShowingHint)
    }

    @ViewBuilder
    private func answeredActionGrid(
        for currentPoint: KnowledgePoint,
        isExpanded: Bool,
        buttonScale: CGFloat,
        usesWideAnswerStack: Bool
    ) -> some View {
        if mode.isReinforcement {
            ReinforcementReviewAnsweredActionGrid(
                point: currentPoint,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                usesWideAnswerStack: usesWideAnswerStack,
                removeFromReinforcement: {
                    removeCurrentPointFromReinforcementAndAdvance()
                },
                markAsMastered: {
                    markCurrentPointAsMasteredAndAdvance()
                },
                next: {
                    advanceToNextPoint()
                }
            )
        } else if mode.isMastered {
            MasteredReviewAnsweredActionGrid(
                point: currentPoint,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                usesWideAnswerStack: usesWideAnswerStack,
                addToReinforcement: {
                    addCurrentPointToReinforcementAndAdvance()
                },
                removeFromMastered: {
                    removeCurrentPointFromMasteredAndAdvance()
                },
                next: {
                    advanceToNextPoint()
                }
            )
        } else {
            NormalReviewAnsweredActionGrid(
                point: currentPoint,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                usesWideAnswerStack: usesWideAnswerStack,
                addToReinforcement: {
                    addCurrentPointToReinforcementAndAdvance()
                },
                markAsMastered: {
                    markCurrentPointAsMasteredAndAdvance()
                },
                next: {
                    advanceToNextPoint()
                }
            )
        }
    }

    private func actionRegionMinimumHeight(
        isExpanded: Bool,
        buttonScale: CGFloat,
        isShowingContent: Bool,
        usesWideAnswerStack: Bool
    ) -> CGFloat {
        if isShowingContent && usesWideAnswerStack {
            let scale = max(buttonScale, 1)
            let buttonHeight = (isExpanded ? 76 : 66) * scale
            let spacing = (isExpanded ? 14 : 12) * scale
            return buttonHeight * 3 + spacing * 2
        }

        return (isExpanded ? 178 : 156) * buttonScale
    }

    private func usesWideAnswerActionStack(metrics: KikariaAdaptiveLayout.Metrics) -> Bool {
        #if os(macOS)
        true
        #else
        metrics.reviewUsesTwoColumnLayout
        #endif
    }

    private func titleGroup(for currentPoint: KnowledgePoint, metrics: KikariaAdaptiveLayout.Metrics) -> some View {
        let isExpanded = metrics.isPadWidth
        let titleSize = 40 * metrics.reviewScale

        return VStack(spacing: isExpanded ? 20 : 18) {
            reviewTitleText(currentPoint.title, titleSize: titleSize)
                .foregroundStyle(KikariaTheme.deepText)
                .multilineTextAlignment(.center)
                .minimumScaleFactor(0.72)
                .padding(.horizontal, isExpanded ? 26 : 22)

            LightTagRow(tags: currentPoint.tags, isExpanded: isExpanded)
                .id("tags-\(currentPoint.id)")
                .transition(reviewEphemeralStateTransition)

            TodayReviewCountPill(count: todayReviewCount(for: currentPoint.id), isExpanded: isExpanded)
                .id("today-review-\(currentPoint.id)")
                .transition(reviewEphemeralStateTransition)
        }
        .frame(maxWidth: .infinity)
    }

    private func reviewTitleText(_ title: String, titleSize: CGFloat) -> Text {
        #if os(macOS)
        return KikariaTypography.mixedText(
            title,
            chineseFont: .system(size: titleSize, weight: .semibold),
            serifFont: .system(size: titleSize, weight: .semibold, design: .serif)
        )
        #else
        return Text(title)
            .font(.system(size: titleSize, weight: .semibold, design: .serif))
        #endif
    }

    private func centralContentStack(for currentPoint: KnowledgePoint, metrics: KikariaAdaptiveLayout.Metrics) -> some View {
        let isExpanded = metrics.isPadWidth
        let stackSpacing: CGFloat = metrics.isPadPortrait ? 20 : (isExpanded ? 18 : 14)
        let textVisibilityAnimation = isReviewTextVisible ? reviewTextFadeInAnimation : reviewUnusedComponentFadeOutAnimation

        return VStack(spacing: stackSpacing) {
            titleGroup(for: currentPoint, metrics: metrics)

            if isShowingHint {
                FloatingInfoCard(
                    title: "提示",
                    text: currentPoint.hint,
                    isExpanded: isExpanded
                )
                .transition(reviewEphemeralStateTransition)
            }

            if isShowingContent {
                FloatingInfoCard(
                    title: "答案",
                    text: currentPoint.content,
                    isExpanded: isExpanded
                )
                .transition(answerTransition(for: currentPoint))
            }
        }
        .frame(maxWidth: .infinity)
        .opacity(isReviewTextVisible ? 1 : 0)
        .animation(isReviewPointTransitioning ? nil : revealAnimation, value: isShowingHint)
        .animation(answerRevealAnimation(for: currentPoint), value: isShowingContent)
        .animation(textVisibilityAnimation, value: isReviewTextVisible)
    }

    private func contentRegion(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics
    ) -> some View {
        GeometryReader { proxy in
            ScrollView {
                centralContentStack(for: currentPoint, metrics: metrics)
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.vertical, metrics.isPadPortrait ? 34 : (metrics.isPadWidth ? 30 : 24))
                    .frame(maxWidth: metrics.reviewMaxWidth)
                    .frame(maxWidth: .infinity)
                    .background {
                        ReviewScrollMetricsReader()
                    }
                    .frame(
                        minHeight: proxy.size.height + metrics.reviewContentVerticalOffset * 2,
                        alignment: .center
                    )
            }
            .kikariaScrollIndicators(hidden: true)
            .coordinateSpace(name: ReviewScrollCoordinateSpace.readingContent)
            .onPreferenceChange(ReviewScrollContentMetricsPreferenceKey.self) { contentMetrics in
                updateReviewScrollState(contentMetrics, viewportHeight: proxy.size.height)
            }
        }
    }

    private func reviewLandscapeReadingColumn(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics
    ) -> some View {
        GeometryReader { proxy in
            let safeTop = proxy.safeAreaInsets.top
            let safeBottom = proxy.safeAreaInsets.bottom
            let safeContentHeight = max(0, proxy.size.height - safeTop - safeBottom)

            ScrollView {
                centralContentStack(for: currentPoint, metrics: metrics)
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.vertical, 24)
                    .frame(maxWidth: metrics.reviewMaxWidth)
                    .frame(maxWidth: .infinity)
                    .background {
                        ReviewScrollMetricsReader()
                    }
                    .frame(minHeight: safeContentHeight, alignment: .center)
                    .padding(.top, safeTop)
                    .padding(.bottom, safeBottom)
            }
            .kikariaScrollIndicators(hidden: true)
            .coordinateSpace(name: ReviewScrollCoordinateSpace.readingContent)
            .onPreferenceChange(ReviewScrollContentMetricsPreferenceKey.self) { contentMetrics in
                updateReviewScrollState(contentMetrics, viewportHeight: proxy.size.height)
            }
            .simultaneousGesture(reviewDragGesture())
        }
    }

    private func reviewActionContent(
        for currentPoint: KnowledgePoint,
        isExpanded: Bool,
        buttonScale: CGFloat,
        usesWideAnswerStack: Bool,
        isInteractive: Bool
    ) -> some View {
        ZStack {
            revealButtons(
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                isInteractive: isInteractive
            )
                .opacity(isShowingContent ? 0 : 1)
                .allowsHitTesting(isInteractive && !isShowingContent)

            answeredActionGrid(
                for: currentPoint,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                usesWideAnswerStack: usesWideAnswerStack
            )
                .opacity(isShowingContent ? 1 : 0)
                .allowsHitTesting(isInteractive && isShowingContent)
        }
        .animation(isReviewPointTransitioning ? nil : .easeInOut(duration: 0.18), value: isShowingContent)
        .allowsHitTesting(isInteractive && !isReviewPointTransitioning)
    }

    private func actionRegion(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics,
        isInteractive: Bool
    ) -> some View {
        let isExpanded = metrics.isPadWidth
        let buttonScale = metrics.reviewButtonScale
        let usesWideAnswerStack = usesWideAnswerActionStack(metrics: metrics)

        return reviewActionContent(
            for: currentPoint,
            isExpanded: isExpanded,
            buttonScale: buttonScale,
            usesWideAnswerStack: usesWideAnswerStack,
            isInteractive: isInteractive
        )
        .frame(maxWidth: .infinity)
        .frame(
            height: actionRegionMinimumHeight(
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                isShowingContent: isShowingContent,
                usesWideAnswerStack: usesWideAnswerStack
            ),
            alignment: .bottom
        )
    }

    private func reviewLandscapeActionPanel(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics,
        isInteractive: Bool
    ) -> some View {
        let isExpanded = metrics.isPadWidth
        let buttonScale = metrics.reviewButtonScale
        let usesWideAnswerStack = usesWideAnswerActionStack(metrics: metrics)

        return VStack(spacing: 0) {
            Spacer(minLength: 0)

            reviewActionContent(
                for: currentPoint,
                isExpanded: isExpanded,
                buttonScale: buttonScale,
                usesWideAnswerStack: usesWideAnswerStack,
                isInteractive: isInteractive
            )
                .frame(maxWidth: .infinity)
                .frame(
                    height: actionRegionMinimumHeight(
                        isExpanded: isExpanded,
                        buttonScale: buttonScale,
                        isShowingContent: isShowingContent,
                        usesWideAnswerStack: usesWideAnswerStack
                    ),
                    alignment: .center
                )

            Spacer(minLength: 0)
        }
        .frame(width: metrics.reviewLandscapeRightWidth)
        .frame(maxHeight: .infinity)
        .contentShape(Rectangle())
    }

    private func reviewLandscapeContent(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics,
        isInteractive: Bool
    ) -> some View {
        HStack(alignment: .center, spacing: metrics.reviewLandscapeColumnSpacing) {
            reviewLandscapeReadingColumn(
                for: currentPoint,
                metrics: metrics
            )
                .frame(width: metrics.reviewLandscapeLeftWidth)
                .frame(maxHeight: .infinity)

            reviewLandscapeActionPanel(
                for: currentPoint,
                metrics: metrics,
                isInteractive: isInteractive
            )
        }
        .frame(maxWidth: metrics.reviewLandscapeMaxWidth)
        .padding(.horizontal, metrics.horizontalPadding)
        .padding(.vertical, 28)
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .center)
    }

    @ViewBuilder
    private func reviewContent(
        for currentPoint: KnowledgePoint,
        metrics: KikariaAdaptiveLayout.Metrics
    ) -> some View {
        if metrics.reviewUsesTwoColumnLayout {
            reviewLandscapeContent(
                for: currentPoint,
                metrics: metrics,
                isInteractive: !isReviewPointTransitioning
            )
        } else {
            VStack(spacing: 0) {
                contentRegion(
                    for: currentPoint,
                    metrics: metrics
                )

                actionRegion(
                    for: currentPoint,
                    metrics: metrics,
                    isInteractive: !isReviewPointTransitioning
                )
                    .padding(.horizontal, metrics.horizontalPadding)
                    .padding(.top, 12)
                    .padding(.bottom, metrics.reviewActionBottomPadding)
                    .frame(maxWidth: metrics.reviewMaxWidth)
                    .frame(maxWidth: .infinity)
            }
        }
    }

    private func reviewBackButton(metrics: KikariaAdaptiveLayout.Metrics) -> some View {
        return VStack {
            HStack {
                KikariaAdaptiveBackButton(metrics: metrics) {
                    dismiss()
                }

                Spacer()
            }
            .padding(.leading, metrics.horizontalPadding)
            .padding(.top, 12)

            Spacer()
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                if hasPreparedReviewQueue &&
                    matchingPointIDsCache.isEmpty &&
                    currentPoint == nil {
                    if mode.isReinforcement || mode.isMastered {
                        ReinforcementCompletionView {
                            if let onReturnHome {
                                onReturnHome()
                            } else {
                                dismiss()
                            }
                        }
                        .padding(metrics.horizontalPadding)
                        .frame(maxWidth: metrics.reviewMaxWidth)
                    } else {
                        SoftEmptyState(
                            title: "暂无知识点",
                            subtitle: "请返回后调整选择范围。",
                            systemImage: "tag.slash"
                        )
                        .padding(metrics.horizontalPadding)
                        .frame(maxWidth: metrics.reviewMaxWidth)
                    }
                } else if let currentPoint {
                    reviewContent(for: currentPoint, metrics: metrics)
                } else {
                    ProgressView()
                }

                if isShowingScopePanel {
                    ScopeSelectionView(
                        selectedTags: $selectedTags,
                        knowledgePoints: knowledgePoints,
                        allTags: allTags,
                        onDone: {
                            withAnimation(.spring(response: 0.42, dampingFraction: 0.88)) {
                                isShowingScopePanel = false
                            }
                        }
                    )
                    .transition(.move(edge: .leading).combined(with: .opacity))
                    .zIndex(4)
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                        .zIndex(5)
                }

                if metrics.isPadPortrait {
                    reviewBackButton(metrics: metrics)
                        .zIndex(6)
                }
            }
            .background(ReviewContainerFrameReader())
            .onPreferenceChange(ReviewContainerFramePreferenceKey.self) { frame in
                reviewContainerFrame = frame
            }
            .onPreferenceChange(ReviewCardFramePreferenceKey.self) { frames in
                reviewCardFrames = frames
            }
            .onPreferenceChange(KikariaMathBlockFramePreferenceKey.self) { frames in
                reviewFormulaFrames = frames
            }
            .simultaneousGestureIf(!metrics.reviewUsesTwoColumnLayout, reviewDragGesture())
            .navigationBarBackButtonHidden(metrics.isPadPortrait)
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
        .onAppear {
            refreshMatchingPointIDs()

            if currentPointID == nil {
                rebuildReviewQueue(avoiding: lastQueuePointID)
            }

            if let currentPoint {
                prewarmReviewMathText(for: currentPoint)
            }

        }
        .onChange(of: selectedTags) { _ in
            if mode.isNormal {
                refreshMatchingPointIDs()
                rebuildReviewQueue(avoiding: currentPointID)
            }
        }
        .onChange(of: knowledgePoints) { _ in
            refreshMatchingPointIDs()
            reconcileReviewQueue()
        }
        #if os(macOS)
        .background {
            KikariaMacReviewKeyboardHandler(
                isEnabled: currentPoint != nil && !isShowingScopePanel,
                onSpace: handleMacSpaceShortcut,
                onReturn: handleMacReturnShortcut,
                onMastered: handleMacMasteredShortcut,
                onReinforcement: handleMacReinforcementShortcut
            )
            .frame(width: 0, height: 0)
        }
        #endif
    }

    private func reviewDragGesture() -> some Gesture {
        DragGesture(minimumDistance: 24, coordinateSpace: .global)
            .onChanged { value in
                decideReviewGestureOwnerIfNeeded(for: value)
            }
            .onEnded { value in
                decideReviewGestureOwnerIfNeeded(for: value)
                let owner = reviewGestureOwner
                reviewGestureOwner = .undecided

                guard owner == .fullScreenNavigation else {
                    return
                }

                handleFullScreenDragGesture(
                    translation: value.translation,
                    startLocation: value.startLocation
                )
            }
    }

    private func decideReviewGestureOwnerIfNeeded(for value: DragGesture.Value) {
        guard reviewGestureOwner == .undecided,
              let owner = reviewGestureOwner(for: value)
        else {
            return
        }

        reviewGestureOwner = owner
    }

    private func updateReviewScrollState(_ contentMetrics: ReviewScrollContentMetrics, viewportHeight: CGFloat) {
        guard reviewScrollState.needsUpdate(contentMetrics: contentMetrics, viewportHeight: viewportHeight) else {
            return
        }

        reviewScrollState.update(contentMetrics: contentMetrics, viewportHeight: viewportHeight)
    }

    private func reviewGestureOwner(for value: DragGesture.Value) -> ReviewGestureOwner? {
        guard currentPoint != nil else {
            return .ignored
        }

        guard !isShowingScopePanel else {
            return .ignored
        }

        guard let axis = dominantDragAxis(for: value.translation) else {
            return nil
        }

        if startsInFormulaArea(value.startLocation) {
            return axis == .horizontal ? .formulaHorizontal : .cardVerticalScroll
        }

        if startsInCardArea(value.startLocation) {
            switch axis {
            case .horizontal:
                return .fullScreenNavigation
            case .vertical:
                guard reviewScrollState.hasMetrics else {
                    return .cardVerticalScroll
                }

                return reviewScrollState.isScrollable ? .cardVerticalScroll : .fullScreenNavigation
            }
        }

        return .fullScreenNavigation
    }

    private func dominantDragAxis(for translation: CGSize) -> ReviewDragAxis? {
        let horizontal = abs(translation.width)
        let vertical = abs(translation.height)
        let minimumDistance: CGFloat = 14
        let dominance: CGFloat = 1.15

        guard max(horizontal, vertical) >= minimumDistance else {
            return nil
        }

        if horizontal > vertical * dominance {
            return .horizontal
        }

        if vertical > horizontal * dominance {
            return .vertical
        }

        return nil
    }

    private func startsInFormulaArea(_ location: CGPoint) -> Bool {
        reviewFormulaFrames.contains { frame in
            frame.insetBy(dx: -12, dy: -12).contains(location)
        }
    }

    private func startsInCardArea(_ location: CGPoint) -> Bool {
        reviewCardFrames.contains { frame in
            frame.insetBy(dx: -10, dy: -10).contains(location)
        }
    }

    private func localReviewLocation(from globalLocation: CGPoint) -> CGPoint {
        CGPoint(
            x: globalLocation.x - reviewContainerFrame.minX,
            y: globalLocation.y - reviewContainerFrame.minY
        )
    }

    private func handleFullScreenDragGesture(translation: CGSize, startLocation: CGPoint) {
        guard !isShowingScopePanel else {
            return
        }

        guard !isReviewPointTransitioning else {
            return
        }

        let localStartLocation = localReviewLocation(from: startLocation)

        let dx = translation.width
        let dy = translation.height
        let horizontal = abs(dx)
        let vertical = abs(dy)
        let horizontalThreshold: CGFloat = 80
        let revealAnswerSwipeThreshold: CGFloat = 90
        let nextAfterAnswerSwipeThreshold: CGFloat = 160
        let verticalThreshold: CGFloat = isShowingContent ? nextAfterAnswerSwipeThreshold : revealAnswerSwipeThreshold
        let dominance: CGFloat = 1.4

        if horizontal > horizontalThreshold && horizontal > vertical * dominance {
            if dx > 0 {
                guard mode.isNormal, localStartLocation.x > 34 else {
                    return
                }

                triggerGestureFeedback()
                withAnimation(.spring(response: 0.42, dampingFraction: 0.88)) {
                    isShowingScopePanel = true
                }
            } else {
                triggerGestureFeedback()
                handleSwipeLeft()
            }
        } else if vertical > verticalThreshold && vertical > horizontal * dominance {
            if dy < 0 {
                triggerGestureFeedback()
                if isShowingContent {
                    transitionToNextPoint()
                } else {
                    withAnimation(revealAnimation) {
                        revealContent()
                    }
                }
            } else {
                if isShowingContent, startsInCardArea(startLocation) {
                    return
                }

                triggerGestureFeedback()
                transitionToPreviousPoint()
            }
        }
    }

    private func handleSwipeLeft() {
        switch mode {
        case .normal:
            handleNormalSwipeLeft()
        case .reinforcement:
            handleReinforcementSwipeLeft()
        case .mastered:
            handleMasteredSwipeLeft()
        }
    }

    private func handleNormalSwipeLeft() {
        // Normal-mode left swipe only adds/re-adds to reinforcement; it must never mark a point as mastered.
        let wasMastered = currentPoint?.isMastered
        withAnimation(revealAnimation) {
            revealContent()
        }
        addCurrentPointToReinforcement(shouldShowToast: true)
        assert(currentPoint?.isMastered == wasMastered)
    }

    private func handleReinforcementSwipeLeft() {
        // Reinforcement-mode left swipe only removes from reinforcement; mastered status is untouched.
        removeCurrentPointFromReinforcement(shouldShowToast: true)
        transitionToNextPoint()
    }

    private func handleMasteredSwipeLeft() {
        // Mastered-mode left swipe only removes from mastered; reinforcement status is untouched.
        removeCurrentPointFromMastered(shouldShowToast: true)
        transitionToNextPoint()
    }

    private func triggerGestureFeedback() {
        withAnimation(.easeInOut(duration: 0.12)) {
            gestureFeedback = true
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 0.14) {
            withAnimation(.easeInOut(duration: 0.16)) {
                gestureFeedback = false
            }
        }
    }

    private func chooseRandomPoint() {
        moveToNextInQueue()
    }

    private func advanceToNextPoint() {
        transitionToNextPoint()
    }

    private func transitionToNextPoint() {
        transitionReviewPoint {
            chooseRandomPoint()
        }
    }

    private func transitionToPreviousPoint() {
        transitionReviewPoint {
            goBackOrChooseRandom()
        }
    }

    private func transitionReviewPoint(_ updateCurrentPoint: @escaping () -> Void) {
        guard !isReviewPointTransitioning else {
            return
        }

        guard currentPoint != nil else {
            updateCurrentPoint()
            return
        }

        let token = UUID()

        reviewPointTransitionToken = token
        isReviewPointTransitioning = true

        withAnimation(reviewUnusedComponentFadeOutAnimation) {
            isReviewTextVisible = false
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + reviewPointSwapDelay) {
            guard reviewPointTransitionToken == token else {
                return
            }

            var transaction = Transaction()
            transaction.disablesAnimations = true

            withTransaction(transaction) {
                updateCurrentPoint()
                reviewScrollState = ReviewScrollState()
                reviewGestureOwner = .undecided
                reviewCardFrames = []
                reviewFormulaFrames = []
            }

            withAnimation(reviewTextFadeInAnimation) {
                isReviewTextVisible = true
            }
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + pointTransitionDelay) {
            guard reviewPointTransitionToken == token else {
                return
            }

            isReviewPointTransitioning = false
        }
    }

    private func cancelReviewPointTransition() {
        reviewPointTransitionToken = UUID()
        isReviewPointTransitioning = false
        isReviewTextVisible = true
    }

    private func rebuildReviewQueue(avoiding avoidedFirstID: KnowledgePoint.ID? = nil) {
        cancelReviewPointTransition()
        refreshMatchingPointIDs()
        var shuffledIDs = matchingPointIDs.shuffled()

        guard !shuffledIDs.isEmpty else {
            reviewQueue = []
            reviewQueueIndex = 0
            currentPointID = nil
            resetRevealState()
            return
        }

        if let avoidedFirstID,
           shuffledIDs.count > 1,
           shuffledIDs.first == avoidedFirstID,
           let swapIndex = shuffledIDs.firstIndex(where: { $0 != avoidedFirstID }) {
            shuffledIDs.swapAt(0, swapIndex)
        }

        reviewQueue = shuffledIDs
        setCurrentPointFromQueue(at: 0)
    }

    private func moveToNextInQueue() {
        reconcileReviewQueue()

        guard !reviewQueue.isEmpty else {
            rebuildReviewQueue(avoiding: lastQueuePointID)
            return
        }

        let nextIndex: Int
        if let currentPointID,
           let currentIndex = reviewQueue.firstIndex(of: currentPointID) {
            nextIndex = currentIndex + 1
        } else {
            nextIndex = reviewQueueIndex
        }

        if nextIndex < reviewQueue.count {
            setCurrentPointFromQueue(at: nextIndex)
        } else {
            rebuildReviewQueue(avoiding: currentPointID ?? lastQueuePointID)
        }
    }

    private func goBackOrChooseRandom() {
        moveToPreviousInQueue()
    }

    private func moveToPreviousInQueue() {
        reconcileReviewQueue()

        guard !reviewQueue.isEmpty else {
            rebuildReviewQueue(avoiding: lastQueuePointID)
            return
        }

        if let currentPointID,
           let currentIndex = reviewQueue.firstIndex(of: currentPointID) {
            reviewQueueIndex = currentIndex
        }

        if reviewQueue.count == 1 {
            setCurrentPointFromQueue(at: 0)
            return
        }

        let previousIndex = reviewQueueIndex > 0 ? reviewQueueIndex - 1 : reviewQueue.count - 1
        setCurrentPointFromQueue(at: previousIndex)
    }

    private func reconcileReviewQueue() {
        let validIDs = Set(matchingPointIDs)
        reviewQueue = reviewQueue.filter { validIDs.contains($0) }

        if let currentPointID,
           let currentIndex = reviewQueue.firstIndex(of: currentPointID) {
            reviewQueueIndex = currentIndex
        } else if reviewQueueIndex >= reviewQueue.count {
            reviewQueueIndex = max(0, reviewQueue.count - 1)
        }
    }

    private func setCurrentPointFromQueue(at index: Int) {
        guard reviewQueue.indices.contains(index) else {
            rebuildReviewQueue(avoiding: lastQueuePointID)
            return
        }

        reviewQueueIndex = index
        currentPointID = reviewQueue[index]
        lastQueuePointID = currentPointID
        resetRevealState()

        if let currentPoint {
            prewarmReviewMathText(for: currentPoint)
        }
    }

    private func resetRevealState() {
        isShowingHint = false
        isShowingContent = false
        pendingAnswerRevealToken = nil
        reviewScrollState = ReviewScrollState()
        reviewGestureOwner = .undecided
        reviewCardFrames = []
        reviewFormulaFrames = []
    }

    private func revealHint() {
        if !isShowingHint,
           let currentPointID,
           let point = knowledgePoints.first(where: { $0.id == currentPointID }) {
            onRecordActivity(.viewedHint, point)
        }

        isShowingHint = true
    }

    private func revealContent() {
        guard !isShowingContent, pendingAnswerRevealToken == nil else {
            return
        }

        guard let currentPointID, let point = currentPoint else {
            return
        }

        if usesLightweightAnswerReveal(for: point) {
            let revealToken = UUID()
            let content = point.content
            let revealDelay = reviewDeferredAnswerRevealDelay
            pendingAnswerRevealToken = revealToken

            DispatchQueue.global(qos: .utility).async {
                KikariaMathText.prewarm(content)
            }

            DispatchQueue.main.asyncAfter(deadline: .now() + revealDelay) {
                guard pendingAnswerRevealToken == revealToken else {
                    return
                }

                guard self.currentPointID == currentPointID else {
                    pendingAnswerRevealToken = nil
                    return
                }

                pendingAnswerRevealToken = nil
                showAnswerContent(for: point)
                finishAnswerReveal(pointID: currentPointID, point: point)
            }
            return
        }

        showAnswerContent(for: point)
        finishAnswerReveal(pointID: currentPointID, point: point)
    }

    private func showAnswerContent(for point: KnowledgePoint) {
        if usesLightweightAnswerReveal(for: point) {
            var transaction = Transaction(animation: reviewLongAnswerRevealAnimation)
            transaction.disablesAnimations = false
            withTransaction(transaction) {
                isShowingContent = true
            }
        } else {
            isShowingContent = true
        }
    }

    private func finishAnswerReveal(pointID: KnowledgePoint.ID, point: KnowledgePoint) {
        DispatchQueue.main.async {
            guard currentPointID == pointID, isShowingContent else {
                return
            }

            incrementTodayReviewCount(for: pointID)
            onRecordActivity(.reviewedAnswer, point)
        }
    }

    private func todayReviewCount(for pointID: KnowledgePoint.ID) -> Int {
        guard let record = dailyReviewRecords[pointID],
              Calendar.current.isDate(record.date, inSameDayAs: Date())
        else {
            return 0
        }

        return record.count
    }

    private func incrementTodayReviewCount(for pointID: KnowledgePoint.ID) {
        let now = Date()

        if let record = dailyReviewRecords[pointID],
           Calendar.current.isDate(record.date, inSameDayAs: now) {
            dailyReviewRecords[pointID] = DailyReviewRecord(
                date: now,
                count: record.count + 1
            )
        } else {
            dailyReviewRecords[pointID] = DailyReviewRecord(date: now, count: 1)
        }
    }

    private func addCurrentPointToReinforcement(shouldShowToast: Bool = false) {
        guard let currentPointID,
              let index = knowledgePoints.firstIndex(where: { $0.id == currentPointID })
        else {
            return
        }

        let title = knowledgePoints[index].title
        let wasMastered = knowledgePoints[index].isMastered
        let newCount = knowledgePoints[index].addReinforcement()
        assert(knowledgePoints[index].isMastered == wasMastered)
        onRecordActivity(.addedReinforcement, knowledgePoints[index])

        if shouldShowToast {
            showToast(reinforcementAddedToastTitle(for: title, count: newCount))
        }
    }

    private func addCurrentPointToReinforcementAndAdvance() {
        guard !isReviewPointTransitioning else {
            return
        }

        addCurrentPointToReinforcement(shouldShowToast: true)
        advanceToNextPoint()
    }

    private func markCurrentPointAsMasteredAndAdvance() {
        guard !isReviewPointTransitioning else {
            return
        }

        markCurrentPointAsMastered()
        advanceToNextPoint()
    }

    private func removeCurrentPointFromReinforcementAndAdvance() {
        guard !isReviewPointTransitioning else {
            return
        }

        removeCurrentPointFromReinforcement(shouldShowToast: true)
        advanceToNextPoint()
    }

    private func removeCurrentPointFromMasteredAndAdvance() {
        guard !isReviewPointTransitioning else {
            return
        }

        removeCurrentPointFromMastered(shouldShowToast: true)
        advanceToNextPoint()
    }

    private func markCurrentPointAsMastered() {
        // Mastered status is only set from the explicit "已掌握" action, not from normal-mode swipes.
        guard let currentPointID,
              let index = knowledgePoints.firstIndex(where: { $0.id == currentPointID })
        else {
            return
        }

        let title = knowledgePoints[index].title
        knowledgePoints[index].isMastered = true
        knowledgePoints[index].clearReinforcement()
        knowledgePoints[index].updatedAt = Date()
        onRecordActivity(.markedMastered, knowledgePoints[index])
        showToast("\(title) 已掌握")
    }

    @discardableResult
    private func removeCurrentPointFromReinforcement(shouldShowToast: Bool = false) -> Bool {
        guard let currentPointID,
              let index = knowledgePoints.firstIndex(where: { $0.id == currentPointID })
        else {
            return false
        }

        guard knowledgePoints[index].reinforcementCount > 0 else {
            return false
        }

        let title = knowledgePoints[index].title
        let wasMastered = knowledgePoints[index].isMastered
        knowledgePoints[index].clearReinforcement()
        assert(knowledgePoints[index].isMastered == wasMastered)
        onRecordActivity(.removedReinforcement, knowledgePoints[index])

        if shouldShowToast {
            showToast("\(title) 已移出重点集锦")
        }

        return true
    }

    @discardableResult
    private func removeCurrentPointFromMastered(shouldShowToast: Bool = false) -> Bool {
        guard let currentPointID,
              let index = knowledgePoints.firstIndex(where: { $0.id == currentPointID })
        else {
            return false
        }

        guard knowledgePoints[index].isMastered else {
            return false
        }

        let title = knowledgePoints[index].title
        let wasReinforced = knowledgePoints[index].isReinforced
        knowledgePoints[index].isMastered = false
        knowledgePoints[index].updatedAt = Date()
        assert(knowledgePoints[index].isReinforced == wasReinforced)
        onRecordActivity(.removedMastered, knowledgePoints[index])

        if shouldShowToast {
            showToast("\(title) 已移出已掌握")
        }

        return true
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }

    private func reinforcementAddedToastTitle(for title: String, count: Int) -> String {
        count <= 1 ? "\(title) 已加入重点集锦" : "\(title) 已加入重点集锦 ×\(count)"
    }

    #if os(macOS)
    private func handleMacSpaceShortcut() {
        guard currentPoint != nil,
              !isShowingScopePanel
        else {
            return
        }

        if isShowingContent {
            advanceToNextPoint()
        } else if isShowingHint {
            withAnimation(revealAnimation) {
                revealContent()
            }
        } else {
            withAnimation(revealAnimation) {
                revealHint()
            }
        }
    }

    private func handleMacReturnShortcut() {
        guard currentPoint != nil,
              !isShowingScopePanel
        else {
            return
        }

        if isShowingContent {
            advanceToNextPoint()
        } else {
            withAnimation(revealAnimation) {
                revealContent()
            }
        }
    }

    private func handleMacMasteredShortcut() {
        guard isShowingContent,
              !isShowingScopePanel
        else {
            return
        }

        if mode.isMastered {
            removeCurrentPointFromMasteredAndAdvance()
        } else if currentPoint?.isMastered == false {
            markCurrentPointAsMasteredAndAdvance()
        }
    }

    private func handleMacReinforcementShortcut() {
        guard isShowingContent,
              !isShowingScopePanel
        else {
            return
        }

        if mode.isReinforcement {
            removeCurrentPointFromReinforcementAndAdvance()
        } else {
            addCurrentPointToReinforcementAndAdvance()
        }
    }
    #endif
}

private enum ReviewActionTone {
    case blue
    case green
    case amber
    case red
}

private enum KikariaMacShortcutKey: String, Identifiable {
    case space
    case returnKey
    case k
    case m
    case l
    case semicolon
    case quote

    var id: String {
        rawValue
    }

    var title: String {
        switch self {
        case .space:
            return ""
        case .returnKey:
            return "↩"
        case .k:
            return "K"
        case .m:
            return "M"
        case .l:
            return "L"
        case .semicolon:
            return ";"
        case .quote:
            return "'"
        }
    }
}

#if os(macOS)
private struct KikariaMacShortcutHintGroup: View {
    let keys: [KikariaMacShortcutKey]
    var scale: CGFloat = 1
    var usesLightForeground = false

    var body: some View {
        if !keys.isEmpty {
            HStack(spacing: 4 * max(scale, 1)) {
                ForEach(keys) { key in
                    KikariaMacShortcutBadge(
                        key: key,
                        scale: scale,
                        usesLightForeground: usesLightForeground
                    )
                }
            }
            .fixedSize()
        }
    }
}

private struct KikariaMacShortcutBadge: View {
    let key: KikariaMacShortcutKey
    var scale: CGFloat = 1
    var usesLightForeground = false

    private var resolvedScale: CGFloat {
        max(scale, 1)
    }

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 6 * resolvedScale, style: .continuous)
                .fill(Color.white.opacity(usesLightForeground ? 0.16 : 0.30))
                .overlay {
                    RoundedRectangle(cornerRadius: 6 * resolvedScale, style: .continuous)
                        .stroke(Color.white.opacity(usesLightForeground ? 0.30 : 0.46), lineWidth: 0.8 * resolvedScale)
                }

            if key == .space {
                KikariaMacSpacebarSymbol(usesLightForeground: usesLightForeground)
                    .frame(width: 13 * resolvedScale, height: 9 * resolvedScale)
            } else {
                KikariaTypography.serifText(key.title, size: 10 * resolvedScale, weight: .semibold)
                    .foregroundStyle(
                        usesLightForeground
                            ? Color.white.opacity(0.86)
                            : KikariaTheme.deepText.opacity(0.68)
                    )
            }
        }
        .frame(width: 23 * resolvedScale, height: 21 * resolvedScale)
    }
}

private struct KikariaMacSpacebarSymbol: View {
    var usesLightForeground = false

    var body: some View {
        GeometryReader { proxy in
            Path { path in
                let width = proxy.size.width
                let height = proxy.size.height
                let leftX = width * 0.14
                let rightX = width * 0.86
                let bottomY = height * 0.76
                let topY = height * 0.34

                path.move(to: CGPoint(x: leftX, y: topY))
                path.addLine(to: CGPoint(x: leftX, y: bottomY))
                path.addLine(to: CGPoint(x: rightX, y: bottomY))
                path.addLine(to: CGPoint(x: rightX, y: topY))
            }
            .stroke(
                usesLightForeground ? Color.white.opacity(0.86) : KikariaTheme.deepText.opacity(0.68),
                style: StrokeStyle(lineWidth: 1.35, lineCap: .round, lineJoin: .round)
            )
        }
    }
}
#endif

private struct ReviewAnsweredActionGrid<TopButton: View, BottomButton: View>: View {
    let next: () -> Void
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var usesWideAnswerStack = false
    private let topButton: () -> TopButton
    private let bottomButton: () -> BottomButton

    init(
        next: @escaping () -> Void,
        isExpanded: Bool = false,
        buttonScale: CGFloat = 1,
        usesWideAnswerStack: Bool = false,
        @ViewBuilder topButton: @escaping () -> TopButton,
        @ViewBuilder bottomButton: @escaping () -> BottomButton
    ) {
        self.next = next
        self.isExpanded = isExpanded
        self.buttonScale = buttonScale
        self.usesWideAnswerStack = usesWideAnswerStack
        self.topButton = topButton
        self.bottomButton = bottomButton
    }

    @ViewBuilder
    var body: some View {
        if usesWideAnswerStack {
            wideStack
        } else {
            compactGrid
        }
    }

    private var wideStack: some View {
        let scale = max(buttonScale, 1)
        let spacing: CGFloat = (isExpanded ? 14 : 12) * scale
        let buttonHeight = (isExpanded ? 76 : 66) * scale

        return VStack(spacing: spacing) {
            topButton()

            bottomButton()

            ReviewActionButton(
                title: "下一个",
                systemImage: "shuffle",
                isPrimary: false,
                tone: .amber,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: [.space, .returnKey]
            ) {
                next()
            }
        }
        .frame(maxWidth: .infinity)
    }

    private var compactGrid: some View {
        return GeometryReader { proxy in
            let scale = max(buttonScale, 1)
            let spacing: CGFloat = (isExpanded ? 14 : 12) * scale
            let availableWidth = max(0, proxy.size.width - spacing)
            let leftWidth = availableWidth * 0.65
            let rightWidth = availableWidth - leftWidth
            let gridHeight = (isExpanded ? 166 : 144) * scale

            HStack(spacing: spacing) {
                VStack(spacing: spacing) {
                    topButton()
                    bottomButton()
                }
                .frame(width: leftWidth)

                ReviewActionButton(
                    title: "下一个",
                    systemImage: "shuffle",
                    isPrimary: false,
                    tone: .amber,
                    isVerticalContent: true,
                    isExpanded: isExpanded,
                    buttonScale: scale,
                    minHeight: gridHeight,
                    shortcutHints: [.space, .returnKey]
                ) {
                    next()
                }
                .frame(width: rightWidth)
            }
        }
        .frame(height: (isExpanded ? 166 : 144) * max(buttonScale, 1))
    }
}

private struct NormalReviewAnsweredActionGrid: View {
    let point: KnowledgePoint
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var usesWideAnswerStack = false
    let addToReinforcement: () -> Void
    let markAsMastered: () -> Void
    let next: () -> Void

    var body: some View {
        let scale = max(buttonScale, 1)
        let buttonHeight = (isExpanded ? 76 : 66) * scale

        ReviewAnsweredActionGrid(
            next: next,
            isExpanded: isExpanded,
            buttonScale: scale,
            usesWideAnswerStack: usesWideAnswerStack
        ) {
            ReviewActionButton(
                title: point.reinforcementCount > 0 ? "再次加入 ×\(point.reinforcementCount)" : "加入重点集锦",
                systemImage: "plus.circle.fill",
                isPrimary: true,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: [.l, .semicolon, .quote]
            ) {
                addToReinforcement()
            }
        } bottomButton: {
            MasteredReviewButton(
                isMastered: point.isMastered,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: point.isMastered ? [] : [.k, .m]
            ) {
                markAsMastered()
            }
        }
    }
}

private struct ReinforcementReviewAnsweredActionGrid: View {
    let point: KnowledgePoint
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var usesWideAnswerStack = false
    let removeFromReinforcement: () -> Void
    let markAsMastered: () -> Void
    let next: () -> Void

    var body: some View {
        let scale = max(buttonScale, 1)
        let buttonHeight = (isExpanded ? 76 : 66) * scale

        ReviewAnsweredActionGrid(
            next: next,
            isExpanded: isExpanded,
            buttonScale: scale,
            usesWideAnswerStack: usesWideAnswerStack
        ) {
            ReviewActionButton(
                title: "移出重点集锦",
                systemImage: "minus.circle.fill",
                isPrimary: true,
                tone: .red,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: [.l, .semicolon, .quote]
            ) {
                removeFromReinforcement()
            }
        } bottomButton: {
            MasteredReviewButton(
                isMastered: point.isMastered,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: point.isMastered ? [] : [.k, .m]
            ) {
                markAsMastered()
            }
        }
    }
}

private struct MasteredReviewAnsweredActionGrid: View {
    let point: KnowledgePoint
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var usesWideAnswerStack = false
    let addToReinforcement: () -> Void
    let removeFromMastered: () -> Void
    let next: () -> Void

    var body: some View {
        let scale = max(buttonScale, 1)
        let buttonHeight = (isExpanded ? 76 : 66) * scale

        ReviewAnsweredActionGrid(
            next: next,
            isExpanded: isExpanded,
            buttonScale: scale,
            usesWideAnswerStack: usesWideAnswerStack
        ) {
            ReviewActionButton(
                title: point.reinforcementCount > 0 ? "再次加入 ×\(point.reinforcementCount)" : "加入重点集锦",
                systemImage: "plus.circle.fill",
                isPrimary: true,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: [.l, .semicolon, .quote]
            ) {
                addToReinforcement()
            }
        } bottomButton: {
            ReviewActionButton(
                title: "移出已掌握",
                systemImage: "minus.circle.fill",
                isPrimary: true,
                tone: .red,
                isExpanded: isExpanded,
                buttonScale: scale,
                minHeight: buttonHeight,
                shortcutHints: [.k, .m]
            ) {
                removeFromMastered()
            }
        }
    }
}

private struct ReviewActionButton: View {
    @Environment(\.colorScheme) private var colorScheme
    let title: String
    let systemImage: String
    let isPrimary: Bool
    var tone: ReviewActionTone = .blue
    var isEnabled = true
    var isVerticalContent = false
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var minHeight: CGFloat? = nil
    var shortcutHints: [KikariaMacShortcutKey] = []
    let action: () -> Void

    private var primaryFill: AnyShapeStyle {
        switch tone {
        case .blue:
            return AnyShapeStyle(KikariaTheme.actionGradient)
        case .green:
            return AnyShapeStyle(KikariaTheme.masteredGradient)
        case .amber:
            return AnyShapeStyle(KikariaTheme.nextGradient)
        case .red:
            return AnyShapeStyle(KikariaTheme.removeGradient)
        }
    }

    private var secondaryFill: AnyShapeStyle {
        switch tone {
        case .blue, .green:
            return AnyShapeStyle(KikariaTheme.glassSurface.opacity(colorScheme == .dark ? 0.36 : 0.46))
        case .amber:
            return AnyShapeStyle(
                LinearGradient(
                    colors: [
                        KikariaTheme.nextAmber.opacity(colorScheme == .dark ? 0.72 : 0.68),
                        Color(red: 0.58, green: 0.53, blue: 0.80).opacity(colorScheme == .dark ? 0.68 : 0.56)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
        case .red:
            return AnyShapeStyle(
                LinearGradient(
                    colors: [
                        KikariaTheme.removeCoral.opacity(colorScheme == .dark ? 0.70 : 0.58),
                        Color(red: 0.98, green: 0.58, blue: 0.50).opacity(colorScheme == .dark ? 0.56 : 0.46)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
        }
    }

    private var foregroundColor: Color {
        if isPrimary {
            return .white
        }

        switch tone {
        case .amber:
            return .white.opacity(0.94)
        default:
            return colorScheme == .dark ? KikariaTheme.deepText.opacity(0.92) : KikariaTheme.deepText
        }
    }

    private var textShadowColor: Color {
        switch tone {
        case .amber:
            return Color(red: 0.23, green: 0.20, blue: 0.36).opacity(0.22)
        default:
            return .clear
        }
    }

    private var strokeAccentOpacity: Double {
        switch tone {
        case .amber:
            return 0.16
        default:
            return 0.18
        }
    }

    private var buttonShadowOpacity: Double {
        switch tone {
        case .amber:
            return isPrimary ? 0.12 : 0.055
        default:
            return isPrimary ? 0.22 : 0.10
        }
    }

    private var shadowColor: Color {
        switch tone {
        case .blue:
            return KikariaTheme.sky
        case .green:
            return KikariaTheme.masteredGreen
        case .amber:
            return KikariaTheme.nextAmber
        case .red:
            return KikariaTheme.removeCoral
        }
    }

    var body: some View {
        let scale = max(buttonScale, 1)
        let cornerRadius = (isExpanded ? 28 : 26) * scale

        Button(action: action) {
            content
                .foregroundStyle(foregroundColor)
                .shadow(color: textShadowColor, radius: tone == .amber ? 4 : 0, y: tone == .amber ? 1 : 0)
                .frame(maxWidth: .infinity)
                .padding(.vertical, (isExpanded ? 22 : 19) * scale)
                .frame(minHeight: minHeight)
                .background {
                    RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
                        .fill(isPrimary ? primaryFill : secondaryFill)
                }
                .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: cornerRadius, style: .continuous))
                .overlay {
                    RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
                        .stroke(
                            LinearGradient(
                                colors: [
                                    Color.white.opacity(isPrimary ? 0.44 : 0.48),
                                    Color.white.opacity(0.12),
                                    shadowColor.opacity(strokeAccentOpacity)
                                ],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            ),
                            lineWidth: scale
                        )
                }
                .shadow(color: shadowColor.opacity(buttonShadowOpacity), radius: 16 * scale, y: 9 * scale)
        }
        .buttonStyle(.plain)
        .disabled(!isEnabled)
        .opacity(isEnabled ? 1 : 0.82)
    }

    @ViewBuilder
    private var content: some View {
        let scale = max(buttonScale, 1)

        if isVerticalContent {
            VStack(spacing: (isExpanded ? 10 : 8) * scale) {
                Image(systemName: systemImage)
                    .font(.system(size: (isExpanded ? 28 : 20) * scale, weight: .semibold))

                #if os(macOS)
                HStack(spacing: 8 * scale) {
                    Text(title)
                        .font(KikariaTypography.chineseButton(size: (isExpanded ? 18 : 17) * scale))

                    KikariaMacShortcutHintGroup(
                        keys: shortcutHints,
                        scale: scale,
                        usesLightForeground: isPrimary || tone == .amber
                    )
                }
                #else
                Text(title)
                    .font(KikariaTypography.chineseButton(size: (isExpanded ? 18 : 17) * scale))
                #endif
            }
            .frame(maxWidth: .infinity)
        } else {
            #if os(macOS)
            HStack(spacing: 8 * scale) {
                Label(title, systemImage: systemImage)
                    .font(KikariaTypography.chineseButton(size: (isExpanded ? 18 : 17) * scale))

                KikariaMacShortcutHintGroup(
                    keys: shortcutHints,
                    scale: scale,
                    usesLightForeground: isPrimary || tone == .amber
                )
            }
            #else
            Label(title, systemImage: systemImage)
                .font(KikariaTypography.chineseButton(size: (isExpanded ? 18 : 17) * scale))
            #endif
        }
    }
}

private struct MasteredReviewButton: View {
    @Environment(\.colorScheme) private var colorScheme
    let isMastered: Bool
    var isExpanded = false
    var buttonScale: CGFloat = 1
    var minHeight: CGFloat? = nil
    var shortcutHints: [KikariaMacShortcutKey] = []
    let action: () -> Void

    var body: some View {
        let scale = max(buttonScale, 1)
        let cornerRadius = (isExpanded ? 28 : 26) * scale

        Button(action: action) {
            HStack(spacing: (isExpanded ? 10 : 8) * scale) {
                Image(systemName: isMastered ? "checkmark.seal.fill" : "plus.circle.fill")
                    .font(.system(size: (isExpanded ? 18 : 15) * scale, weight: .semibold))
                    .foregroundStyle(isMastered ? KikariaTheme.masteredGreen.opacity(0.9) : .white)

                Text(isMastered ? "已设定为掌握" : "加入已掌握")
                    .font(KikariaTypography.chineseButton(size: (isExpanded ? 18 : 17) * scale))

                #if os(macOS)
                KikariaMacShortcutHintGroup(
                    keys: shortcutHints,
                    scale: scale,
                    usesLightForeground: !isMastered
                )
                #endif
            }
            .foregroundStyle(isMastered ? KikariaTheme.softText : .white)
            .frame(maxWidth: .infinity)
            .padding(.vertical, (isExpanded ? 22 : 19) * scale)
            .frame(minHeight: minHeight)
            .background {
                RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
                    .fill(
                        isMastered
                            ? AnyShapeStyle(KikariaTheme.glassSurface.opacity(colorScheme == .dark ? 0.34 : 0.42))
                            : AnyShapeStyle(KikariaTheme.masteredActionGradient)
                    )
            }
            .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: cornerRadius, style: .continuous))
            .overlay {
                RoundedRectangle(cornerRadius: cornerRadius, style: .continuous)
                    .stroke(
                        LinearGradient(
                            colors: [
                                Color.white.opacity(0.46),
                                Color.white.opacity(0.12),
                                KikariaTheme.masteredGreen.opacity(0.20)
                            ],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        ),
                        lineWidth: scale
                    )
            }
            .shadow(
                color: isMastered
                    ? KikariaTheme.blueGray.opacity(0.10)
                    : KikariaTheme.masteredGreen.opacity(0.20),
                radius: 16 * scale,
                y: 9 * scale
            )
        }
        .buttonStyle(.plain)
        .disabled(isMastered)
        .opacity(isMastered ? 0.88 : 1)
    }
}

private struct ReinforcementCompletionView: View {
    let returnHome: () -> Void

    var body: some View {
        VStack(spacing: 28) {
            Image(systemName: "checkmark.circle.fill")
                .font(.system(size: 86, weight: .semibold))
                .foregroundStyle(KikariaTheme.masteredGreen, .white.opacity(0.96))
                .shadow(color: Color.green.opacity(0.16), radius: 16, y: 8)

            Button(action: returnHome) {
                Text("返回首页")
                    .font(KikariaTypography.chineseButton())
                    .foregroundStyle(.white)
                    .padding(.horizontal, 42)
                    .padding(.vertical, 16)
                    .background(KikariaTheme.actionGradient, in: Capsule())
                    .shadow(color: KikariaTheme.sky.opacity(0.20), radius: 16, y: 8)
            }
            .buttonStyle(.plain)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }
}

private enum KnowledgeCollectionKind {
    case reinforcement
    case mastered
}

private struct KnowledgeCollectionSnapshot: Equatable {
    var knowledgePointCount: Int
    var items: [KnowledgePoint]
    var filteredItems: [KnowledgePoint]
    var searchTextIsEmpty: Bool

    static let empty = KnowledgeCollectionSnapshot(
        knowledgePointCount: 0,
        items: [],
        filteredItems: [],
        searchTextIsEmpty: true
    )

    static func make(
        kind: KnowledgeCollectionKind,
        knowledgePoints: [KnowledgePoint],
        searchText: String
    ) -> KnowledgeCollectionSnapshot {
        let collectionItems: [KnowledgePoint]

        switch kind {
        case .reinforcement:
            collectionItems = knowledgePoints
                .filter { $0.reinforcementCount > 0 }
                .sorted { lhs, rhs in
                    if lhs.reinforcementCount != rhs.reinforcementCount {
                        return lhs.reinforcementCount > rhs.reinforcementCount
                    }

                    switch (lhs.lastReinforcedAt, rhs.lastReinforcedAt) {
                    case let (lhsDate?, rhsDate?):
                        return lhsDate > rhsDate
                    case (_?, nil):
                        return true
                    case (nil, _?):
                        return false
                    case (nil, nil):
                        return lhs.title.localizedCaseInsensitiveCompare(rhs.title) == .orderedAscending
                    }
                }
        case .mastered:
            collectionItems = knowledgePoints.filter(\.isMastered)
        }

        let trimmedSearchText = searchText.trimmingCharacters(in: .whitespacesAndNewlines)
        let filteredItems = trimmedSearchText.isEmpty
            ? collectionItems
            : collectionItems.filter { $0.matchesPreparedSearchQuery(trimmedSearchText) }

        return KnowledgeCollectionSnapshot(
            knowledgePointCount: knowledgePoints.count,
            items: collectionItems,
            filteredItems: filteredItems,
            searchTextIsEmpty: trimmedSearchText.isEmpty
        )
    }
}

struct ReinforcementView: View {
    @Binding var knowledgePoints: [KnowledgePoint]
    let onRecordActivity: (StudyActivityType, KnowledgePoint) -> Void
    let onStartReview: () -> Void
    @State private var searchText = ""
    @State private var toastMessage: String?
    @State private var toastToken = UUID()
    @State private var collectionSnapshot: KnowledgeCollectionSnapshot

    init(
        knowledgePoints: Binding<[KnowledgePoint]>,
        onRecordActivity: @escaping (StudyActivityType, KnowledgePoint) -> Void,
        onStartReview: @escaping () -> Void
    ) {
        self._knowledgePoints = knowledgePoints
        self.onRecordActivity = onRecordActivity
        self.onStartReview = onStartReview
        self._collectionSnapshot = State(
            initialValue: KnowledgeCollectionSnapshot.make(
                kind: .reinforcement,
                knowledgePoints: knowledgePoints.wrappedValue,
                searchText: ""
            )
        )
    }

    private func refreshCollectionSnapshot() {
        let snapshot = KnowledgeCollectionSnapshot.make(
            kind: .reinforcement,
            knowledgePoints: knowledgePoints,
            searchText: searchText
        )
        collectionSnapshot = snapshot
    }

    private func landscapeContent(
        metrics: KikariaAdaptiveLayout.Metrics,
        titleFontSize: CGFloat,
        reinforcedPoints: [KnowledgePoint],
        filteredReinforcedPoints: [KnowledgePoint]
    ) -> some View {
        let gridSpacing = min(max(metrics.collectionLandscapeAvailableWidth * 0.026, 24), 32)
        let gridColumns = [
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top),
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top)
        ]
        let startButtonWidth = min(max(metrics.collectionLandscapeAvailableWidth * 0.24, 240), 260)

        return ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                Text("重点集锦")
                    .font(KikariaTypography.chineseTitle(size: titleFontSize))
                    .foregroundStyle(KikariaTheme.deepText)

                HStack(alignment: .center, spacing: 18) {
                    KikariaSearchBar(text: $searchText)

                    if !reinforcedPoints.isEmpty {
                        Button(action: onStartReview) {
                            ReinforcementStartButton(count: reinforcedPoints.count)
                        }
                        .buttonStyle(.plain)
                        .frame(width: startButtonWidth)
                    }
                }

                if reinforcedPoints.isEmpty {
                    SoftEmptyState(
                        title: "还没有重点",
                        subtitle: "在背诵时查看答案后，可以把知识点加入这里。",
                        systemImage: "sparkles"
                    )
                    .frame(maxWidth: .infinity, minHeight: 260)
                    .padding(.top, 12)
                } else if filteredReinforcedPoints.isEmpty {
                    SoftEmptyState(
                        title: "没有找到相关知识点",
                        subtitle: "换个关键词试试看。",
                        systemImage: "magnifyingglass"
                    )
                    .frame(maxWidth: .infinity, minHeight: 220)
                    .padding(.top, 12)
                } else {
                    LazyVGrid(columns: gridColumns, alignment: .center, spacing: 20) {
                        ForEach(filteredReinforcedPoints) { point in
                            ReinforcementCard(point: point) {
                                removeFromReinforcement(point)
                            }
                            .frame(maxWidth: .infinity, alignment: .topLeading)
                        }
                    }
                    .padding(.top, 4)
                }
            }
            .padding(.top, metrics.pageTitleTopPadding(defaultValue: 18))
            .padding(.horizontal, metrics.horizontalPadding)
            .padding(.bottom, 34)
            .frame(maxWidth: metrics.collectionLandscapeMaxWidth)
            .frame(maxWidth: .infinity)
        }
        .kikariaScrollIndicators(hidden: true)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let titleFontSize = metrics.pageTitleFontSize(defaultValue: 32)
            let titleTopPadding = metrics.pageTitleTopPadding(defaultValue: 18)
            let titleSpacing = metrics.pageTitleSpacing(defaultValue: 18)
            let currentSnapshot = collectionSnapshot
            let currentReinforcedPoints = currentSnapshot.items
            let currentFilteredReinforcedPoints = currentSnapshot.filteredItems

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                if metrics.collectionUsesTwoColumnLayout {
                    landscapeContent(
                        metrics: metrics,
                        titleFontSize: titleFontSize,
                        reinforcedPoints: currentReinforcedPoints,
                        filteredReinforcedPoints: currentFilteredReinforcedPoints
                    )
                } else if currentReinforcedPoints.isEmpty {
                    SoftEmptyState(
                        title: "还没有重点",
                        subtitle: "在背诵时查看答案后，可以把知识点加入这里。",
                        systemImage: "sparkles"
                    )
                    .padding(metrics.horizontalPadding)
                    .frame(maxWidth: metrics.mainMaxWidth)
                } else {
                    VStack(spacing: 0) {
                        ScrollView {
                            LazyVStack(alignment: .leading, spacing: titleSpacing) {
                                Text("重点集锦")
                                    .font(KikariaTypography.chineseTitle(size: titleFontSize))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .padding(.top, titleTopPadding)

                                KikariaSearchBar(text: $searchText)

                                if currentFilteredReinforcedPoints.isEmpty {
                                    SoftEmptyState(
                                        title: "没有找到相关知识点",
                                        subtitle: "换个关键词试试看。",
                                        systemImage: "magnifyingglass"
                                    )
                                    .padding(.top, 12)
                                } else {
                                    ForEach(currentFilteredReinforcedPoints) { point in
                                        ReinforcementCard(point: point) {
                                            removeFromReinforcement(point)
                                        }
                                    }
                                }
                            }
                            .padding(.horizontal, metrics.horizontalPadding)
                            .padding(.bottom, 150)
                            .frame(maxWidth: metrics.mainMaxWidth)
                            .frame(maxWidth: .infinity)
                        }

                        VStack(spacing: 0) {
                            Button(action: onStartReview) {
                                ReinforcementStartButton(count: currentReinforcedPoints.count)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(.top, 18)
                        .padding(.horizontal, metrics.horizontalPadding)
                        .padding(.bottom, 20)
                        .frame(maxWidth: metrics.mainMaxWidth)
                        .frame(maxWidth: .infinity)
                        .background(.ultraThinMaterial)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
            .kikariaAdaptiveNavigationChrome(
                metrics: metrics,
                outerMaxWidth: metrics.collectionUsesTwoColumnLayout ? metrics.collectionLandscapeMaxWidth : metrics.mainMaxWidth
            )
        }
        .onAppear {
            refreshCollectionSnapshot()
        }
        .onChange(of: knowledgePoints) { _ in
            refreshCollectionSnapshot()
        }
        .onChange(of: searchText) { _ in
            refreshCollectionSnapshot()
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func removeFromReinforcement(_ point: KnowledgePoint) {
        guard let index = knowledgePoints.firstIndex(where: { $0.id == point.id }) else {
            return
        }

        withAnimation(.spring(response: 0.36, dampingFraction: 0.9)) {
            knowledgePoints[index].clearReinforcement()
        }

        onRecordActivity(.removedReinforcement, point)
        showToast("\(point.title) 已移出重点集锦")
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

struct MasteredView: View {
    @Binding var knowledgePoints: [KnowledgePoint]
    let onRecordActivity: (StudyActivityType, KnowledgePoint) -> Void
    let onStartReview: () -> Void
    @State private var searchText = ""
    @State private var toastMessage: String?
    @State private var toastToken = UUID()
    @State private var collectionSnapshot: KnowledgeCollectionSnapshot

    init(
        knowledgePoints: Binding<[KnowledgePoint]>,
        onRecordActivity: @escaping (StudyActivityType, KnowledgePoint) -> Void,
        onStartReview: @escaping () -> Void
    ) {
        self._knowledgePoints = knowledgePoints
        self.onRecordActivity = onRecordActivity
        self.onStartReview = onStartReview
        self._collectionSnapshot = State(
            initialValue: KnowledgeCollectionSnapshot.make(
                kind: .mastered,
                knowledgePoints: knowledgePoints.wrappedValue,
                searchText: ""
            )
        )
    }

    private func refreshCollectionSnapshot() {
        let snapshot = KnowledgeCollectionSnapshot.make(
            kind: .mastered,
            knowledgePoints: knowledgePoints,
            searchText: searchText
        )
        collectionSnapshot = snapshot
    }

    private func landscapeContent(
        metrics: KikariaAdaptiveLayout.Metrics,
        titleFontSize: CGFloat,
        masteredPoints: [KnowledgePoint],
        filteredMasteredPoints: [KnowledgePoint]
    ) -> some View {
        let gridSpacing = min(max(metrics.collectionLandscapeAvailableWidth * 0.026, 24), 32)
        let gridColumns = [
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top),
            GridItem(.flexible(), spacing: gridSpacing, alignment: .top)
        ]
        let startButtonWidth = min(max(metrics.collectionLandscapeAvailableWidth * 0.24, 240), 260)

        return ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                Text("已掌握")
                    .font(KikariaTypography.chineseTitle(size: titleFontSize))
                    .foregroundStyle(KikariaTheme.deepText)

                HStack(alignment: .center, spacing: 18) {
                    KikariaSearchBar(text: $searchText)

                    if !masteredPoints.isEmpty {
                        Button(action: onStartReview) {
                            MasteredStartButton(count: masteredPoints.count)
                        }
                        .buttonStyle(.plain)
                        .frame(width: startButtonWidth)
                    }
                }

                if masteredPoints.isEmpty {
                    SoftEmptyState(
                        title: "还没有已掌握",
                        subtitle: "在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。",
                        systemImage: "checkmark.seal"
                    )
                    .frame(maxWidth: .infinity, minHeight: 260)
                    .padding(.top, 12)
                } else if filteredMasteredPoints.isEmpty {
                    SoftEmptyState(
                        title: "没有找到相关知识点",
                        subtitle: "换个关键词试试看。",
                        systemImage: "magnifyingglass"
                    )
                    .frame(maxWidth: .infinity, minHeight: 220)
                    .padding(.top, 12)
                } else {
                    LazyVGrid(columns: gridColumns, alignment: .center, spacing: 20) {
                        ForEach(filteredMasteredPoints) { point in
                            ReinforcementCard(
                                point: point,
                                removeTitle: "移出已掌握",
                                removeSystemImage: "minus.circle.fill",
                                showsReinforcementCountBadge: false
                            ) {
                                removeFromMastered(point)
                            }
                            .frame(maxWidth: .infinity, alignment: .topLeading)
                        }
                    }
                    .padding(.top, 4)
                }
            }
            .padding(.top, metrics.pageTitleTopPadding(defaultValue: 18))
            .padding(.horizontal, metrics.horizontalPadding)
            .padding(.bottom, 34)
            .frame(maxWidth: metrics.collectionLandscapeMaxWidth)
            .frame(maxWidth: .infinity)
        }
        .kikariaScrollIndicators(hidden: true)
        .frame(maxWidth: .infinity, maxHeight: .infinity)
    }

    var body: some View {
        KikariaAdaptivePage { metrics in
            let titleFontSize = metrics.pageTitleFontSize(defaultValue: 32)
            let titleTopPadding = metrics.pageTitleTopPadding(defaultValue: 18)
            let titleSpacing = metrics.pageTitleSpacing(defaultValue: 18)
            let currentSnapshot = collectionSnapshot
            let currentMasteredPoints = currentSnapshot.items
            let currentFilteredMasteredPoints = currentSnapshot.filteredItems

            ZStack {
                KikariaTheme.pageGradient
                    .ignoresSafeArea()

                if metrics.collectionUsesTwoColumnLayout {
                    landscapeContent(
                        metrics: metrics,
                        titleFontSize: titleFontSize,
                        masteredPoints: currentMasteredPoints,
                        filteredMasteredPoints: currentFilteredMasteredPoints
                    )
                } else if currentMasteredPoints.isEmpty {
                    SoftEmptyState(
                        title: "还没有已掌握",
                        subtitle: "在背诵时查看答案后，可以把真正熟悉的知识点标记到这里。",
                        systemImage: "checkmark.seal"
                    )
                    .padding(metrics.horizontalPadding)
                    .frame(maxWidth: metrics.mainMaxWidth)
                } else {
                    VStack(spacing: 0) {
                        ScrollView {
                            LazyVStack(alignment: .leading, spacing: titleSpacing) {
                                Text("已掌握")
                                    .font(KikariaTypography.chineseTitle(size: titleFontSize))
                                    .foregroundStyle(KikariaTheme.deepText)
                                    .padding(.top, titleTopPadding)

                                KikariaSearchBar(text: $searchText)

                                if currentFilteredMasteredPoints.isEmpty {
                                    SoftEmptyState(
                                        title: "没有找到相关知识点",
                                        subtitle: "换个关键词试试看。",
                                        systemImage: "magnifyingglass"
                                    )
                                    .padding(.top, 12)
                                } else {
                                    ForEach(currentFilteredMasteredPoints) { point in
                                        ReinforcementCard(
                                            point: point,
                                            removeTitle: "移出已掌握",
                                            removeSystemImage: "minus.circle.fill",
                                            showsReinforcementCountBadge: false
                                        ) {
                                            removeFromMastered(point)
                                        }
                                    }
                                }
                            }
                            .padding(.horizontal, metrics.horizontalPadding)
                            .padding(.bottom, 150)
                            .frame(maxWidth: metrics.mainMaxWidth)
                            .frame(maxWidth: .infinity)
                        }

                        VStack(spacing: 0) {
                            Button(action: onStartReview) {
                                MasteredStartButton(count: currentMasteredPoints.count)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(.top, 18)
                        .padding(.horizontal, metrics.horizontalPadding)
                        .padding(.bottom, 20)
                        .frame(maxWidth: metrics.mainMaxWidth)
                        .frame(maxWidth: .infinity)
                        .background(.ultraThinMaterial)
                    }
                }

                if let toastMessage {
                    KikariaToastLayer(message: toastMessage)
                        .transition(.move(edge: .top).combined(with: .opacity))
                }
            }
            .kikariaAdaptiveNavigationChrome(
                metrics: metrics,
                outerMaxWidth: metrics.collectionUsesTwoColumnLayout ? metrics.collectionLandscapeMaxWidth : metrics.mainMaxWidth
            )
        }
        .onAppear {
            refreshCollectionSnapshot()
        }
        .onChange(of: knowledgePoints) { _ in
            refreshCollectionSnapshot()
        }
        .onChange(of: searchText) { _ in
            refreshCollectionSnapshot()
        }
        .navigationTitle("")
        .navigationBarTitleDisplayMode(.inline)
    }

    private func removeFromMastered(_ point: KnowledgePoint) {
        guard let index = knowledgePoints.firstIndex(where: { $0.id == point.id }) else {
            return
        }

        withAnimation(.spring(response: 0.36, dampingFraction: 0.9)) {
            knowledgePoints[index].isMastered = false
            knowledgePoints[index].updatedAt = Date()
        }

        onRecordActivity(.removedMastered, point)
        showToast("\(point.title) 已移出已掌握")
    }

    private func showToast(_ message: String) {
        let token = UUID()
        toastToken = token

        withAnimation(.spring(response: 0.34, dampingFraction: 0.88)) {
            toastMessage = message
        }

        DispatchQueue.main.asyncAfter(deadline: .now() + 2.0) {
            guard toastToken == token else {
                return
            }

            withAnimation(.easeOut(duration: 0.22)) {
                toastMessage = nil
            }
        }
    }
}

private struct ReinforcementStartButton: View {
    let count: Int

    var body: some View {
        HStack(spacing: 14) {
            Text("开始重点背诵")
                .font(KikariaTypography.chineseHeadline(size: 20))
                .foregroundStyle(KikariaTheme.deepText)

            Spacer()

            KikariaTypography.mixedText("\(count)", size: 20, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.sky)

            Image(systemName: "chevron.right")
                .font(.subheadline.weight(.semibold))
                .foregroundStyle(KikariaTheme.blueGray)
        }
        .padding(.horizontal, 22)
        .padding(.vertical, 22)
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: 28, fillOpacity: 0.46, strokeOpacity: 0.46, shadowOpacity: 0.16, shadowRadius: 20, shadowY: 10)
    }
}

private struct MasteredStartButton: View {
    let count: Int

    var body: some View {
        HStack(spacing: 14) {
            Text("开始复习")
                .font(KikariaTypography.chineseHeadline(size: 20))
                .foregroundStyle(KikariaTheme.deepText)

            Spacer()

            KikariaTypography.mixedText("\(count)", size: 20, weight: .bold)
                .monospacedDigit()
                .foregroundStyle(KikariaTheme.masteredGreen)

            Image(systemName: "chevron.right")
                .font(.subheadline.weight(.semibold))
                .foregroundStyle(KikariaTheme.blueGray)
        }
        .padding(.horizontal, 22)
        .padding(.vertical, 22)
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: 28, fillOpacity: 0.46, strokeOpacity: 0.46, shadowOpacity: 0.16, shadowRadius: 20, shadowY: 10)
    }
}

private enum ReinforcementCardInteractionMode {
    case list
    case review

    var enablesCardSwipeGesture: Bool {
        switch self {
        case .list:
            return false
        case .review:
            return true
        }
    }
}

private struct ReinforcementCard: View {
    let point: KnowledgePoint
    var removeTitle = "移出重点集锦"
    var removeSystemImage = "minus.circle.fill"
    var showsReinforcementCountBadge = true
    var interactionMode: ReinforcementCardInteractionMode = .list
    let removeAction: () -> Void
    @GestureState private var dragTranslation: CGSize = .zero

    private var previewOffset: CGFloat {
        let horizontal = abs(dragTranslation.width)
        let vertical = abs(dragTranslation.height)

        guard horizontal > vertical * 1.35 else {
            return 0
        }

        return min(max(dragTranslation.width * 0.18, -24), 24)
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack(alignment: .top, spacing: 12) {
                KikariaTypography.mixedText(point.title, size: 22, weight: .semibold)
                    .foregroundStyle(KikariaTheme.deepText)
                    .frame(maxWidth: .infinity, alignment: .leading)

                if showsReinforcementCountBadge, point.reinforcementCount > 0 {
                    KikariaTypography.mixedText("×\(point.reinforcementCount)", size: 14, weight: .bold)
                        .monospacedDigit()
                        .foregroundStyle(KikariaTheme.sky)
                        .padding(.horizontal, 11)
                        .padding(.vertical, 7)
                        .liquidGlassCard(cornerRadius: 16, material: .ultraThinMaterial, fillOpacity: 0.52, strokeOpacity: 0.42, shadowOpacity: 0.08, shadowRadius: 10, shadowY: 5)
                }
            }

            LightTagRow(tags: point.tags)

            KnowledgeListInfoPreview(title: "提示", text: point.hint)

            KnowledgeListInfoPreview(title: "答案", text: point.content)

            Button(action: removeAction) {
                Label(removeTitle, systemImage: removeSystemImage)
                    .font(KikariaTypography.chineseButton(size: 14))
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 14)
                    .background {
                        Capsule(style: .continuous)
                            .fill(KikariaTheme.removeGradient)
                    }
                    .background(.ultraThinMaterial, in: Capsule(style: .continuous))
                    .overlay {
                        Capsule(style: .continuous)
                            .stroke(
                                LinearGradient(
                                    colors: [
                                        Color.white.opacity(0.48),
                                        Color.white.opacity(0.12),
                                        KikariaTheme.removeCoral.opacity(0.22)
                                    ],
                                    startPoint: .topLeading,
                                    endPoint: .bottomTrailing
                                ),
                                lineWidth: 1
                            )
                    }
                    .shadow(color: KikariaTheme.removeCoral.opacity(0.18), radius: 14, y: 8)
            }
            .buttonStyle(.plain)
        }
        .padding(18)
        .liquidGlassCard(cornerRadius: 30, material: .thinMaterial, fillOpacity: 0.42, strokeOpacity: 0.40, shadowOpacity: 0.12, shadowRadius: 20, shadowY: 12)
        .offset(x: previewOffset)
        .simultaneousGestureIf(interactionMode.enablesCardSwipeGesture, cardSwipeGesture)
    }

    private var cardSwipeGesture: some Gesture {
        DragGesture(minimumDistance: 30, coordinateSpace: .local)
            .updating($dragTranslation) { value, state, _ in
                state = value.translation
            }
            .onEnded { value in
                handleCardSwipe(translation: value.translation)
            }
    }

    private func handleCardSwipe(translation: CGSize) {
        let horizontal = abs(translation.width)
        let vertical = abs(translation.height)
        let threshold: CGFloat = 86
        let dominance: CGFloat = 1.45

        guard horizontal > threshold, horizontal > vertical * dominance else {
            return
        }

        removeAction()
    }
}

private struct KikariaToast: View {
    let message: String

    var body: some View {
        KikariaTypography.mixedText(message, size: 14, weight: .semibold)
            .foregroundStyle(KikariaTheme.deepText)
            .multilineTextAlignment(.center)
            .lineLimit(2)
            .padding(.horizontal, 18)
            .padding(.vertical, 13)
            .liquidGlassCard(cornerRadius: 22, material: .regularMaterial, fillOpacity: 0.52, strokeOpacity: 0.52, shadowOpacity: 0.18, shadowRadius: 18, shadowY: 10)
    }
}

private struct KikariaToastLayer: View {
    let message: String

    var body: some View {
        VStack {
            KikariaToast(message: message)
                .padding(.horizontal, 24)
                .padding(.top, 76)

            Spacer()
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
        .allowsHitTesting(false)
    }
}

private struct KnowledgeListInfoPreview: View {
    let title: String
    let text: String
    private let maxPreviewCharacters = 120

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(title)
                .font(KikariaTypography.chineseHeadline(size: 13, weight: .bold))
                .foregroundStyle(KikariaTheme.sky)

            Text(previewText)
                .font(KikariaTypography.chineseBody(size: 15, weight: .medium))
                .foregroundStyle(KikariaTheme.deepText.opacity(0.82))
                .lineSpacing(3)
                .lineLimit(2)
                .frame(maxWidth: .infinity, alignment: .leading)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }

    private var previewText: String {
        let collapsedText = text
            .replacingOccurrences(of: "\n", with: " ")
            .replacingOccurrences(of: "\t", with: " ")
            .split(separator: " ", omittingEmptySubsequences: true)
            .joined(separator: " ")

        guard !collapsedText.isEmpty else {
            return "暂无内容"
        }

        guard collapsedText.count > maxPreviewCharacters else {
            return collapsedText
        }

        let preview = collapsedText
            .prefix(maxPreviewCharacters)
            .trimmingCharacters(in: .whitespacesAndNewlines)
        return "\(preview)..."
    }
}

private struct FloatingInfoCard: View {
    let title: String
    let text: String
    var isExpanded = false

    var body: some View {
        VStack(alignment: .leading, spacing: bodySpacing) {
            Text(title)
                .font(KikariaTypography.chineseHeadline(size: isExpanded ? 15 : 14, weight: .bold))
                .foregroundStyle(KikariaTheme.sky)

            KikariaMathText(
                text,
                fontSize: isExpanded ? 18 : 17,
                textColor: KikariaTheme.deepText,
                accentColor: KikariaTheme.sky,
                lineSpacing: mathLineSpacing,
                usesSystemChineseFont: usesMacReviewMathRendering,
                usesGenerousFormulaSpacing: usesMacReviewMathRendering
            )
        }
        .padding(isExpanded ? 22 : 18)
        .frame(maxWidth: isExpanded ? 820 : 700)
        .background(ReviewCardFrameReader())
        .liquidGlassCard(cornerRadius: isExpanded ? 28 : 26, material: .thinMaterial, fillOpacity: 0.56, strokeOpacity: 0.42, shadowOpacity: 0.14, shadowRadius: isExpanded ? 20 : 18, shadowY: isExpanded ? 11 : 10)
    }

    private var bodySpacing: CGFloat {
        #if os(macOS)
        return isExpanded ? 16 : 14
        #else
        return isExpanded ? 12 : 10
        #endif
    }

    private var mathLineSpacing: CGFloat {
        #if os(macOS)
        return isExpanded ? 8 : 7
        #else
        return isExpanded ? 4 : 3
        #endif
    }

    private var usesMacReviewMathRendering: Bool {
        #if os(macOS)
        return true
        #else
        return false
        #endif
    }
}

private struct LightTagRow: View {
    let tags: [String]
    var isExpanded = false

    var body: some View {
        if #available(iOS 16, *) {
            CenteredTagFlow(spacing: isExpanded ? 10 : 8, rowSpacing: isExpanded ? 9 : 8) {
                ForEach(tags, id: \.self) { tag in
                    LightTagPill(
                        tag,
                        isExpanded: isExpanded
                    )
                }
            }
        } else {
            LegacyCenteredTagFlow(
                spacing: isExpanded ? 10 : 8,
                rowSpacing: isExpanded ? 9 : 8,
                isExpanded: isExpanded,
                tags: tags
            )
        }
        .frame(maxWidth: .infinity)
    }
}

private struct LightTagPill: View {
    let tag: String
    let isExpanded: Bool

    init(_ tag: String, isExpanded: Bool) {
        self.tag = tag
        self.isExpanded = isExpanded
    }

    var body: some View {
        KikariaTypography.mixedText(tag, size: isExpanded ? 13 : 12, weight: .semibold)
            .foregroundStyle(KikariaTheme.softText)
            .padding(.horizontal, isExpanded ? 13 : 11)
            .padding(.vertical, isExpanded ? 7 : 6)
            .liquidGlassCapsule(fillOpacity: 0.38, strokeOpacity: 0.34, shadowOpacity: 0.04, shadowRadius: 6, shadowY: 3)
    }
}

private struct LegacyCenteredTagFlow: View {
    let spacing: CGFloat
    let rowSpacing: CGFloat
    let isExpanded: Bool
    let tags: [String]

    var body: some View {
        let columns = [GridItem(.adaptive(minimum: 1), spacing: spacing)]

        LazyVGrid(columns: columns, alignment: .center, spacing: rowSpacing) {
            ForEach(tags, id: \.self) { tag in
                LightTagPill(tag, isExpanded: isExpanded)
            }
        }
    }
}

private struct TodayReviewCountPill: View {
    let count: Int
    var isExpanded = false

    var body: some View {
        KikariaTypography.mixedText("该知识点今日复习 \(count) 次", size: isExpanded ? 13 : 12, weight: .semibold)
            .foregroundStyle(KikariaTheme.deepText.opacity(0.78))
            .monospacedDigit()
            .padding(.horizontal, isExpanded ? 20 : 18)
            .padding(.vertical, isExpanded ? 9 : 8)
            .liquidGlassCapsule(fillOpacity: 0.42, strokeOpacity: 0.38, shadowOpacity: 0.10, shadowRadius: isExpanded ? 14 : 12, shadowY: isExpanded ? 7 : 6)
            .accessibilityLabel("该知识点今日复习 \(count) 次")
    }
}

@available(iOS 16, *)
private struct CenteredTagFlow: Layout {
    var spacing: CGFloat
    var rowSpacing: CGFloat

    func sizeThatFits(
        proposal: ProposedViewSize,
        subviews: Subviews,
        cache: inout ()
    ) -> CGSize {
        let maxWidth = proposal.width ?? .greatestFiniteMagnitude
        let rows = makeRows(maxWidth: maxWidth, subviews: subviews)
        let width = proposal.width ?? rows.map(\.width).max() ?? 0
        let height = rows.reduce(0) { $0 + $1.height } + rowSpacing * CGFloat(max(rows.count - 1, 0))

        return CGSize(width: width, height: height)
    }

    func placeSubviews(
        in bounds: CGRect,
        proposal: ProposedViewSize,
        subviews: Subviews,
        cache: inout ()
    ) {
        let rows = makeRows(maxWidth: bounds.width, subviews: subviews)
        var y = bounds.minY

        for row in rows {
            var x = bounds.minX + max((bounds.width - row.width) / 2, 0)

            for item in row.items {
                subviews[item.index].place(
                    at: CGPoint(x: x, y: y + (row.height - item.size.height) / 2),
                    anchor: .topLeading,
                    proposal: ProposedViewSize(width: item.size.width, height: item.size.height)
                )
                x += item.size.width + spacing
            }

            y += row.height + rowSpacing
        }
    }

    private func makeRows(maxWidth: CGFloat, subviews: Subviews) -> [TagFlowRow] {
        let availableWidth = maxWidth.isFinite ? maxWidth : .greatestFiniteMagnitude
        var rows: [TagFlowRow] = []
        var current = TagFlowRow()

        for index in subviews.indices {
            let size = subviews[index].sizeThatFits(.unspecified)
            let nextWidth = current.items.isEmpty ? size.width : current.width + spacing + size.width

            if nextWidth > availableWidth && !current.items.isEmpty {
                rows.append(current)
                current = TagFlowRow()
            }

            if !current.items.isEmpty {
                current.width += spacing
            }

            current.items.append(TagFlowItem(index: index, size: size))
            current.width += size.width
            current.height = max(current.height, size.height)
        }

        if !current.items.isEmpty {
            rows.append(current)
        }

        return rows
    }
}

private struct TagFlowRow {
    var items: [TagFlowItem] = []
    var width: CGFloat = 0
    var height: CGFloat = 0
}

private struct TagFlowItem {
    let index: Int
    let size: CGSize
}

private struct SoftEmptyState: View {
    let title: String
    let subtitle: String
    let systemImage: String

    var body: some View {
        VStack(spacing: 14) {
            Image(systemName: systemImage)
                .font(.system(size: 42))
                .foregroundStyle(KikariaTheme.sky)

            KikariaTypography.mixedText(title, size: 20, weight: .bold)
                .foregroundStyle(KikariaTheme.deepText)

            KikariaTypography.mixedText(subtitle, size: 15)
                .foregroundStyle(KikariaTheme.softText)
                .multilineTextAlignment(.center)
        }
        .padding(26)
        .frame(maxWidth: .infinity)
        .liquidGlassCard(cornerRadius: 30, material: .thinMaterial, fillOpacity: 0.54, strokeOpacity: 0.42, shadowOpacity: 0.12, shadowRadius: 18, shadowY: 10)
    }
}

#Preview {
    ContentView()
}
