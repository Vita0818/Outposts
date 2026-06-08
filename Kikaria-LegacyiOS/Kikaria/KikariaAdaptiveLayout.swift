//
//  KikariaAdaptiveLayout.swift
//  Kikaria
//
//  Created by Codex on 2026/5/8.
//

import SwiftUI

#if os(iOS)
typealias KikariaHorizontalSizeClass = UserInterfaceSizeClass
#else
enum KikariaHorizontalSizeClass {
    case compact
    case regular
}
#endif

enum KikariaAdaptiveLayout {
    enum WidthCategory {
        case compact
        case regularPad
        case widePad
    }

    struct Metrics {
        let size: CGSize
        let horizontalSizeClass: KikariaHorizontalSizeClass?

        init(size: CGSize, horizontalSizeClass: KikariaHorizontalSizeClass? = nil) {
            self.size = size
            self.horizontalSizeClass = horizontalSizeClass
        }

        var width: CGFloat {
            max(size.width, 1)
        }

        var height: CGFloat {
            max(size.height, 1)
        }

        var widthCategory: WidthCategory {
            KikariaAdaptiveLayout.widthCategory(for: width)
        }

        var isPadWidth: Bool {
            widthCategory != .compact
        }

        var isPadPortrait: Bool {
            isPadWidth && height >= width
        }

        var isPadLandscape: Bool {
            isPadWidth && width > height
        }

        var isTwoColumnCapable: Bool {
            width >= 950 &&
                width > height &&
                widthCategory != .compact &&
                hasNonCompactHorizontalSizeClass
        }

        var homeUsesTwoColumnLayout: Bool {
            isTwoColumnCapable
        }

        var reviewUsesTwoColumnLayout: Bool {
            isTwoColumnCapable
        }

        var collectionUsesTwoColumnLayout: Bool {
            isTwoColumnCapable
        }

        var settingsUsesTwoColumnLayout: Bool {
            isTwoColumnCapable
        }

        var horizontalPadding: CGFloat {
            KikariaAdaptiveLayout.horizontalPadding(for: width)
        }

        var innerHorizontalPadding: CGFloat {
            isPadPortrait ? 32 : horizontalPadding
        }

        var portraitPadMaxWidth: CGFloat {
            portraitMainMaxWidth
        }

        var portraitHomeMaxWidth: CGFloat {
            width >= 900 ? 760 : 720
        }

        var portraitMainMaxWidth: CGFloat {
            width >= 900 ? 680 : 660
        }

        var portraitFormMaxWidth: CGFloat {
            width >= 900 ? 620 : 600
        }

        var portraitReviewMaxWidth: CGFloat {
            width >= 900 ? 720 : 700
        }

        var portraitScaleFactor: CGFloat {
            guard isPadPortrait else {
                return 1
            }

            return width >= 900 ? 1.36 : 1.30
        }

        var homeScale: CGFloat {
            if isPadPortrait {
                return width >= 900 ? 1.36 : 1.30
            }

            return isPadWidth ? 1.14 : 1
        }

        var homeHeaderScale: CGFloat {
            if isPadPortrait {
                return width >= 900 ? 1.20 : 1.16
            }

            return isPadWidth ? 1.14 : 1
        }

        var reviewScale: CGFloat {
            if isPadPortrait {
                return width >= 900 ? 1.20 : 1.18
            }

            return isPadWidth ? 1.15 : 1
        }

        var reviewButtonScale: CGFloat {
            if isPadPortrait {
                return width >= 900 ? 1.18 : 1.14
            }

            return 1
        }

        var cardScale: CGFloat {
            if isPadPortrait {
                return width >= 900 ? 1.24 : 1.18
            }

            return isPadWidth ? 1.05 : 1
        }

        var presetScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.18 : 1.12) : 1
        }

        var scopeScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.16 : 1.10) : 1
        }

        var overviewScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.18 : 1.12) : 1
        }

        var settingsScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.16 : 1.10) : 1
        }

        var settingsRowScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.14 : 1.08) : 1
        }

        var newPresetScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.16 : 1.10) : 1
        }

        var listCardScale: CGFloat {
            isPadPortrait ? (width >= 900 ? 1.16 : 1.10) : 1
        }

        var presetOuterMaxWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 760 : 720) : mainMaxWidth
        }

        var scopeOuterMaxWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 760 : 720) : mainMaxWidth
        }

        var overviewOuterMaxWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 760 : 720) : mainMaxWidth
        }

        var ipadPortraitListPageTopInset: CGFloat {
            isPadPortrait ? (width >= 900 ? 46 : 38) : 0
        }

        var ipadPortraitOverviewTopInset: CGFloat {
            isPadPortrait ? (width >= 900 ? 44 : 36) : 0
        }

        var ipadPortraitFormPageTopInset: CGFloat {
            isPadPortrait ? (width >= 900 ? 46 : 38) : 0
        }

        var ipadPortraitSettingsTopInset: CGFloat {
            isPadPortrait ? (width >= 900 ? 46 : 38) : 0
        }

        var ipadPortraitPageTitleTopInset: CGFloat {
            isPadPortrait ? (width >= 900 ? 96 : 84) : 0
        }

        var ipadPortraitPageTitleFontSize: CGFloat {
            isPadPortrait ? (width >= 900 ? 36 : 35) : 32
        }

        var ipadPortraitPageTitleSpacing: CGFloat {
            isPadPortrait ? 24 : 18
        }

        var ipadPortraitPageSubtitleSpacing: CGFloat {
            isPadPortrait ? 10 : 8
        }

        func pageTitleTopPadding(defaultValue: CGFloat) -> CGFloat {
            isPadPortrait ? ipadPortraitPageTitleTopInset : defaultValue
        }

        func pageTitleFontSize(defaultValue: CGFloat) -> CGFloat {
            isPadPortrait ? ipadPortraitPageTitleFontSize : defaultValue
        }

        func pageTitleSpacing(defaultValue: CGFloat) -> CGFloat {
            isPadPortrait ? ipadPortraitPageTitleSpacing : defaultValue
        }

        func pageTitleSubtitleSpacing(defaultValue: CGFloat) -> CGFloat {
            isPadPortrait ? ipadPortraitPageSubtitleSpacing : defaultValue
        }

        var settingsOuterMaxWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 740 : 700) : formMaxWidth
        }

        var newPresetOuterMaxWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 740 : 700) : formMaxWidth
        }

        var scopeGridMinimumWidth: CGFloat {
            isPadPortrait ? (width >= 900 ? 176 : 164) : 132
        }

        var scopeGridSpacing: CGFloat {
            isPadPortrait ? 16 : 12
        }

        func effectiveContentWidth(for outerMaxWidth: CGFloat) -> CGFloat {
            max(0, min(width, outerMaxWidth) - innerHorizontalPadding * 2)
        }

        var adaptiveBackButtonSize: CGFloat {
            return 42
        }

        var adaptiveTopBarTrailingWidth: CGFloat {
            isPadPortrait ? 64 : 42
        }

        var newPresetInputHeight: CGFloat {
            isPadPortrait ? (width >= 900 ? 62 : 58) : 0
        }

        var newPresetTextEditorHeight: CGFloat {
            isPadPortrait ? (width >= 900 ? 380 : 340) : 260
        }

        var homeLandscapeMaxWidth: CGFloat {
            1080
        }

        var homeLandscapeAvailableWidth: CGFloat {
            max(0, min(width - horizontalPadding * 2, homeLandscapeMaxWidth))
        }

        var homeLandscapeColumnSpacing: CGFloat {
            min(max(homeLandscapeAvailableWidth * 0.06, 56), 68)
        }

        var homeLandscapeRightWidth: CGFloat {
            min(max(homeLandscapeAvailableWidth * 0.39, 400), 430)
        }

        var homeLandscapeLeftWidth: CGFloat {
            min(max(homeLandscapeAvailableWidth - homeLandscapeRightWidth - homeLandscapeColumnSpacing, 410), 560)
        }

        var homeLandscapeBubbleScale: CGFloat {
            min(max(homeLandscapeLeftWidth / 500 * 1.04, 1.0), 1.12)
        }

        var homeLandscapeCardScale: CGFloat {
            min(max(homeLandscapeRightWidth / 420, 1.0), 1.05)
        }

        var reviewLandscapeMaxWidth: CGFloat {
            1160
        }

        var reviewLandscapeAvailableWidth: CGFloat {
            max(0, min(width - horizontalPadding * 2, reviewLandscapeMaxWidth))
        }

        var reviewLandscapeColumnSpacing: CGFloat {
            min(max(reviewLandscapeAvailableWidth * 0.055, 48), 64)
        }

        var reviewLandscapeRightWidth: CGFloat {
            min(max(reviewLandscapeAvailableWidth * 0.32, 340), 380)
        }

        var reviewLandscapeLeftWidth: CGFloat {
            max(0, reviewLandscapeAvailableWidth - reviewLandscapeRightWidth - reviewLandscapeColumnSpacing)
        }

        var collectionLandscapeMaxWidth: CGFloat {
            1100
        }

        var collectionLandscapeAvailableWidth: CGFloat {
            max(0, min(width - horizontalPadding * 2, collectionLandscapeMaxWidth))
        }

        var collectionLandscapeColumnSpacing: CGFloat {
            min(max(collectionLandscapeAvailableWidth * 0.055, 52), 68)
        }

        var collectionLandscapeLeftWidth: CGFloat {
            min(max(collectionLandscapeAvailableWidth * 0.31, 320), 360)
        }

        var collectionLandscapeRightWidth: CGFloat {
            max(0, collectionLandscapeAvailableWidth - collectionLandscapeLeftWidth - collectionLandscapeColumnSpacing)
        }

        var settingsLandscapeMaxWidth: CGFloat {
            1080
        }

        var settingsLandscapeAvailableWidth: CGFloat {
            max(0, min(width - horizontalPadding * 2, settingsLandscapeMaxWidth))
        }

        var settingsLandscapeColumnSpacing: CGFloat {
            min(max(settingsLandscapeAvailableWidth * 0.06, 56), 72)
        }

        var settingsLandscapeLeftWidth: CGFloat {
            min(max(settingsLandscapeAvailableWidth * 0.34, 320), 380)
        }

        var settingsLandscapeRightWidth: CGFloat {
            max(0, settingsLandscapeAvailableWidth - settingsLandscapeLeftWidth - settingsLandscapeColumnSpacing)
        }

        var homeMaxWidth: CGFloat {
            if isPadPortrait {
                return portraitHomeMaxWidth
            }

            switch widthCategory {
            case .compact:
                return .infinity
            case .regularPad:
                return 700
            case .widePad:
                return 780
            }
        }

        var mainMaxWidth: CGFloat {
            if isPadPortrait {
                return portraitMainMaxWidth
            }

            switch widthCategory {
            case .compact:
                return .infinity
            case .regularPad:
                return 680
            case .widePad:
                return 760
            }
        }

        var formMaxWidth: CGFloat {
            if isPadPortrait {
                return portraitFormMaxWidth
            }

            switch widthCategory {
            case .compact:
                return .infinity
            case .regularPad:
                return 600
            case .widePad:
                return 640
            }
        }

        var reviewMaxWidth: CGFloat {
            if isPadPortrait {
                return portraitReviewMaxWidth
            }

            switch widthCategory {
            case .compact:
                return .infinity
            case .regularPad:
                return 760
            case .widePad:
                return 820
            }
        }

        var reviewContentVerticalOffset: CGFloat {
            if isPadPortrait {
                return height < 760 ? 8 : 18
            }

            switch widthCategory {
            case .compact:
                return 0
            case .regularPad:
                return height < 620 ? 8 : 18
            case .widePad:
                return height < 620 ? 18 : 34
            }
        }

        var reviewActionBottomPadding: CGFloat {
            if isPadPortrait {
                return height < 760 ? 24 : 34
            }

            switch widthCategory {
            case .compact:
                return 16
            case .regularPad:
                return height < 620 ? 24 : 34
            case .widePad:
                return height < 620 ? 32 : 52
            }
        }

        private var hasNonCompactHorizontalSizeClass: Bool {
            horizontalSizeClass.map { $0 != .compact } ?? true
        }
    }

    static func metrics(
        for size: CGSize,
        horizontalSizeClass: KikariaHorizontalSizeClass? = nil
    ) -> Metrics {
        Metrics(size: size, horizontalSizeClass: horizontalSizeClass)
    }

    static func widthCategory(for width: CGFloat) -> WidthCategory {
        if width < 600 {
            return .compact
        }

        if width < 900 {
            return .regularPad
        }

        return .widePad
    }

    static func horizontalPadding(for width: CGFloat) -> CGFloat {
        switch widthCategory(for: width) {
        case .compact:
            return width < 360 ? 20 : 24
        case .regularPad:
            return 32
        case .widePad:
            return 40
        }
    }
}

struct KikariaAdaptivePage<Content: View>: View {
    #if os(iOS)
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    #endif
    private let content: (KikariaAdaptiveLayout.Metrics) -> Content

    init(@ViewBuilder content: @escaping (KikariaAdaptiveLayout.Metrics) -> Content) {
        self.content = content
    }

    var body: some View {
        GeometryReader { proxy in
            #if os(iOS)
            let resolvedHorizontalSizeClass = horizontalSizeClass
            #else
            let resolvedHorizontalSizeClass: KikariaHorizontalSizeClass? = .regular
            #endif

            content(KikariaAdaptiveLayout.metrics(for: proxy.size, horizontalSizeClass: resolvedHorizontalSizeClass))
                .frame(width: proxy.size.width, height: proxy.size.height)
        }
    }
}

extension View {
    func kikariaCenteredColumn(maxWidth: CGFloat) -> some View {
        frame(maxWidth: maxWidth)
            .frame(maxWidth: .infinity, alignment: .center)
    }
}
