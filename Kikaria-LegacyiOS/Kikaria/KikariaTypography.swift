//
//  KikariaTypography.swift
//  Kikaria
//
//  Created by Codex on 2026/5/2.
//

import SwiftUI

enum KikariaTypography {
    private enum MixedRunStyle {
        case chinese
        case serif
    }

    private struct MixedRun {
        let text: String
        let style: MixedRunStyle
    }

    private static let chineseSystemPunctuation = Set("，。、；：？！“”‘’（）《》【】「」『』—…·￥")

    static func appTitle(size: CGFloat = 39, weight: Font.Weight = .semibold) -> Font {
        .system(size: size, weight: weight, design: .serif)
    }

    static func chineseLargeTitle(size: CGFloat = 34, weight: Font.Weight = .bold) -> Font {
        .system(size: size, weight: weight)
    }

    static func chineseTitle(size: CGFloat = 32, weight: Font.Weight = .bold) -> Font {
        .system(size: size, weight: weight)
    }

    static func chineseHeadline(size: CGFloat = 17, weight: Font.Weight = .semibold) -> Font {
        .system(size: size, weight: weight)
    }

    static func chineseBody(size: CGFloat = 15, weight: Font.Weight = .regular) -> Font {
        .system(size: size, weight: weight)
    }

    static func chineseButton(size: CGFloat = 17, weight: Font.Weight = .semibold) -> Font {
        .system(size: size, weight: weight)
    }

    static func chineseCaption(size: CGFloat = 12, weight: Font.Weight = .medium) -> Font {
        .system(size: size, weight: weight)
    }

    static func tag(size: CGFloat = 12, weight: Font.Weight = .semibold) -> Font {
        .system(size: size, weight: weight)
    }

    static func number(size: CGFloat, weight: Font.Weight = .semibold) -> Font {
        .system(size: size, weight: weight, design: .serif)
    }

    static func serifText(_ string: String, size: CGFloat, weight: Font.Weight = .regular) -> Text {
        Text(string).font(.system(size: size, weight: weight, design: .serif))
    }

    static func numericText(_ string: String, size: CGFloat, weight: Font.Weight = .semibold) -> Text {
        serifText(string, size: size, weight: weight)
    }

    static func mixedText(_ string: String, size: CGFloat, weight: Font.Weight = .regular) -> Text {
        mixedText(
            string,
            chineseFont: .system(size: size, weight: weight),
            serifFont: .system(size: size, weight: weight, design: .serif)
        )
    }

    static func mixedText(_ string: String, chineseFont: Font, serifFont: Font) -> Text {
        let runs = mixedRuns(in: string)
        guard let firstRun = runs.first else {
            return Text("")
        }

        return runs.dropFirst().reduce(styledText(for: firstRun, chineseFont: chineseFont, serifFont: serifFont)) { partial, run in
            partial + styledText(for: run, chineseFont: chineseFont, serifFont: serifFont)
        }
    }

    private static func styledText(for run: MixedRun, chineseFont: Font, serifFont: Font) -> Text {
        switch run.style {
        case .chinese:
            Text(run.text).font(chineseFont)
        case .serif:
            Text(run.text).font(serifFont)
        }
    }

    private static func mixedRuns(in string: String) -> [MixedRun] {
        var runs: [MixedRun] = []
        var currentText = ""
        var currentStyle: MixedRunStyle?

        for character in string {
            let style = mixedRunStyle(for: character)

            if let currentStyle, currentStyle != style {
                runs.append(MixedRun(text: currentText, style: currentStyle))
                currentText = String(character)
            } else {
                currentText.append(character)
            }

            currentStyle = style
        }

        if let currentStyle, !currentText.isEmpty {
            runs.append(MixedRun(text: currentText, style: currentStyle))
        }

        return runs
    }

    private static func mixedRunStyle(for character: Character) -> MixedRunStyle {
        if chineseSystemPunctuation.contains(character) {
            return .chinese
        }

        if character.unicodeScalars.contains(where: isChineseSystemScalar) {
            return .chinese
        }

        return .serif
    }

    nonisolated private static func isChineseSystemScalar(_ scalar: Unicode.Scalar) -> Bool {
        switch scalar.value {
        case 0x3400...0x4DBF,
             0x4E00...0x9FFF,
             0xF900...0xFAFF,
             0x20000...0x2A6DF,
             0x2A700...0x2B73F,
             0x2B740...0x2B81F,
             0x2B820...0x2CEAF,
             0x2CEB0...0x2EBEF,
             0x3000...0x303F,
             0xFF00...0xFFEF:
            return true
        default:
            return false
        }
    }
}
