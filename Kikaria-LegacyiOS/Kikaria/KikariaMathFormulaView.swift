//
//  KikariaMathFormulaView.swift
//  Kikaria
//
//  Created by Codex on 2026/5/10.
//

import SwiftMath
import SwiftUI
#if os(iOS)
import UIKit
#elseif os(macOS)
import AppKit
#endif

enum KikariaFormulaDisplayStyle {
    case inline
    case block
}

enum KikariaFormulaAlignment {
    case left
    case center
}

struct KikariaMathFormulaView: View {
    let latex: String
    let fallbackSource: String
    var displayStyle: KikariaFormulaDisplayStyle
    var fontSize: CGFloat
    var textColor: Color
    var alignment: KikariaFormulaAlignment
    var usesGenerousVerticalSpacing = false

    @State private var renderFailed = false

    var body: some View {
        Group {
            if renderFailed || normalizedLatex.isEmpty {
                fallbackView
            } else {
                KikariaSwiftMathLabel(
                    latex: normalizedLatex,
                    displayStyle: displayStyle,
                    fontSize: fontSize,
                    textColor: textColor,
                    alignment: alignment,
                    usesGenerousVerticalSpacing: usesGenerousVerticalSpacing,
                    renderFailed: $renderFailed
                )
                .fixedSize()
                .accessibilityLabel(fallbackSource)
            }
        }
        .task(id: normalizedLatex) {
            if renderFailed {
                renderFailed = false
            }
        }
    }

    private var normalizedLatex: String {
        var normalized = strippedBlockMathDelimiters(from: latex)
            .replacingOccurrences(of: "\\dfrac", with: "\\frac")
            .replacingOccurrences(of: "\\tfrac", with: "\\frac")
            .replacingOccurrences(of: "\\operatorname{sgn}", with: "\\mathrm{sgn}")

        guard displayStyle == .block else {
            return normalized
        }

        normalized = replacingBracedCommand(in: normalized, command: "\\operatorname") { name in
            "\\mathrm{\(name)}"
        }
        normalized = normalized
            .replacingOccurrences(of: "\\iiint", with: "\\int\\!\\!\\int\\!\\!\\int")
            .replacingOccurrences(of: "\\iint", with: "\\int\\!\\!\\int")

        return normalized
    }

    private var fallbackView: some View {
        Text(fallbackText)
            .font(.system(size: fallbackFontSize, weight: .regular, design: .serif))
            .foregroundStyle(textColor.opacity(0.82))
            .lineSpacing(displayStyle == .block ? 4 : 0)
            .fixedSize(horizontal: displayStyle == .inline, vertical: true)
            .multilineTextAlignment(displayStyle == .block ? .leading : .center)
            .frame(maxWidth: displayStyle == .block ? .infinity : nil, alignment: .leading)
    }

    private var fallbackText: String {
        switch displayStyle {
        case .inline:
            return fallbackSource
        case .block:
            return readableMathFallback(from: fallbackSource)
        }
    }

    private func strippedBlockMathDelimiters(from source: String) -> String {
        let trimmed = source.trimmingCharacters(in: .whitespacesAndNewlines)
        guard trimmed.hasPrefix("$$"),
              trimmed.hasSuffix("$$"),
              trimmed.count >= 4
        else {
            return trimmed
        }

        return String(trimmed.dropFirst(2).dropLast(2))
            .trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private func readableMathFallback(from source: String) -> String {
        var readable = strippedBlockMathDelimiters(from: source)
        readable = replacingFractions(in: readable)
        readable = replacingBracedCommand(in: readable, command: "\\sqrt") { value in
            "√(\(readableMathFallback(from: value)))"
        }
        readable = replacingBracedCommand(in: readable, command: "\\operatorname") { value in
            value
        }
        readable = replacingBracedCommand(in: readable, command: "\\mathrm") { value in
            value
        }
        readable = replacingBracedCommand(in: readable, command: "\\mathbf") { value in
            value
        }
        readable = replacingBracedCommand(in: readable, command: "\\text") { value in
            value
        }
        readable = replacingBracedCommand(in: readable, command: "\\bar") { value in
            "\(readableMathFallback(from: value))̄"
        }

        let replacements = [
            ("\\begin{cases}", ""),
            ("\\end{cases}", ""),
            ("\\begin{aligned}", ""),
            ("\\end{aligned}", ""),
            ("\\begin{matrix}", ""),
            ("\\end{matrix}", ""),
            ("\\\\", "\n"),
            ("&", "  "),
            ("\\qquad", "  "),
            ("\\quad", " "),
            ("\\,", " "),
            ("\\;", " "),
            ("\\:", " "),
            ("\\!", ""),
            ("\\left", ""),
            ("\\right", ""),
            ("\\Bigg", ""),
            ("\\bigg", ""),
            ("\\Big", ""),
            ("\\big", ""),
            ("\\iiint", "∫∫∫"),
            ("\\iint", "∫∫"),
            ("\\int", "∫"),
            ("\\partial", "∂"),
            ("\\nabla", "∇"),
            ("\\Delta", "Δ"),
            ("\\delta", "δ"),
            ("\\theta", "θ"),
            ("\\rho", "ρ"),
            ("\\xi", "ξ"),
            ("\\alpha", "α"),
            ("\\beta", "β"),
            ("\\gamma", "γ"),
            ("\\lambda", "λ"),
            ("\\mu", "μ"),
            ("\\pi", "π"),
            ("\\neq", "≠"),
            ("\\ne", "≠"),
            ("\\leq", "≤"),
            ("\\le", "≤"),
            ("\\geq", "≥"),
            ("\\ge", "≥"),
            ("\\to", "→"),
            ("\\infty", "∞"),
            ("\\cdots", "⋯"),
            ("\\ldots", "…"),
            ("\\times", "×"),
            ("\\pm", "±"),
            ("\\mp", "∓"),
            ("\\in", "∈")
        ]

        for (source, replacement) in replacements {
            readable = readable.replacingOccurrences(of: source, with: replacement)
        }

        readable = readable.replacingOccurrences(of: "\\", with: "")
        return normalizedFallbackWhitespace(readable)
    }

    private func replacingFractions(in text: String) -> String {
        replacingBracedPairCommand(in: text, commands: ["\\dfrac", "\\tfrac", "\\frac"]) { numerator, denominator in
            let readableNumerator = readableMathFallback(from: numerator)
            let readableDenominator = readableMathFallback(from: denominator)
            return "\(wrappedMathFallbackComponent(readableNumerator)) / \(wrappedMathFallbackComponent(readableDenominator))"
        }
    }

    private func replacingBracedPairCommand(
        in text: String,
        commands: [String],
        transform: (String, String) -> String
    ) -> String {
        var result = ""
        var index = text.startIndex

        while index < text.endIndex {
            if let command = commands.first(where: { text[index...].hasPrefix($0) }) {
                var cursor = text.index(index, offsetBy: command.count)
                skipWhitespace(in: text, cursor: &cursor)

                if let firstValue = bracedGroup(in: text, cursor: &cursor) {
                    skipWhitespace(in: text, cursor: &cursor)

                    if let secondValue = bracedGroup(in: text, cursor: &cursor) {
                        result += transform(firstValue, secondValue)
                        index = cursor
                        continue
                    }
                }
            }

            result.append(text[index])
            index = text.index(after: index)
        }

        return result
    }

    private func replacingBracedCommand(
        in text: String,
        command: String,
        transform: (String) -> String
    ) -> String {
        var result = ""
        var index = text.startIndex

        while index < text.endIndex {
            if text[index...].hasPrefix(command) {
                var cursor = text.index(index, offsetBy: command.count)
                skipWhitespace(in: text, cursor: &cursor)

                if let value = bracedGroup(in: text, cursor: &cursor) {
                    result += transform(value)
                    index = cursor
                    continue
                }
            }

            result.append(text[index])
            index = text.index(after: index)
        }

        return result
    }

    private func bracedGroup(in text: String, cursor: inout String.Index) -> String? {
        guard cursor < text.endIndex, text[cursor] == "{" else {
            return nil
        }

        cursor = text.index(after: cursor)
        let start = cursor
        var depth = 1

        while cursor < text.endIndex {
            if text[cursor] == "{" {
                depth += 1
            } else if text[cursor] == "}" {
                depth -= 1

                if depth == 0 {
                    let value = String(text[start..<cursor])
                    cursor = text.index(after: cursor)
                    return value
                }
            }

            cursor = text.index(after: cursor)
        }

        return nil
    }

    private func skipWhitespace(in text: String, cursor: inout String.Index) {
        while cursor < text.endIndex, text[cursor].isWhitespace {
            cursor = text.index(after: cursor)
        }
    }

    private func wrappedMathFallbackComponent(_ value: String) -> String {
        if value.contains(" ") || value.contains("\n") || value.contains("/") {
            return "(\(value))"
        }

        return value
    }

    private func normalizedFallbackWhitespace(_ source: String) -> String {
        var normalizedLines: [String] = []

        for rawLine in source.components(separatedBy: .newlines) {
            var line = rawLine.trimmingCharacters(in: .whitespacesAndNewlines)
            while line.contains("  ") {
                line = line.replacingOccurrences(of: "  ", with: " ")
            }

            if line.isEmpty {
                if normalizedLines.last?.isEmpty == false {
                    normalizedLines.append(line)
                }
            } else {
                normalizedLines.append(line)
            }
        }

        return normalizedLines.joined(separator: "\n")
    }

    private var fallbackFontSize: CGFloat {
        switch displayStyle {
        case .inline:
            return fontSize * 0.95
        case .block:
            return fontSize * 0.9
        }
    }
}

private struct KikariaSwiftMathLabel {
    let latex: String
    var displayStyle: KikariaFormulaDisplayStyle
    var fontSize: CGFloat
    var textColor: Color
    var alignment: KikariaFormulaAlignment
    var usesGenerousVerticalSpacing: Bool

    @Binding var renderFailed: Bool
}

#if os(iOS)
extension KikariaSwiftMathLabel: UIViewRepresentable {
    func makeUIView(context: Context) -> MTMathUILabel {
        let label = MTMathUILabel()
        label.backgroundColor = .clear
        label.setContentHuggingPriority(.required, for: .horizontal)
        label.setContentHuggingPriority(.required, for: .vertical)
        label.setContentCompressionResistancePriority(.required, for: .horizontal)
        label.setContentCompressionResistancePriority(.required, for: .vertical)
        return label
    }

    func updateUIView(_ label: MTMathUILabel, context: Context) {
        configure(label)

        let hasError = label.error != nil
        if renderFailed != hasError {
            DispatchQueue.main.async {
                renderFailed = hasError
            }
        }
    }

    func sizeThatFits(
        _ proposal: CGSize,
        uiView label: MTMathUILabel,
        context: Context
    ) -> CGSize {
        configure(label)
        return measuredSize(for: label)
    }
    
    @available(iOS 16, *)
    func sizeThatFits(
        _ proposal: ProposedViewSize,
        uiView label: MTMathUILabel,
        context: Context
    ) -> CGSize? {
        configure(label)
        return measuredSize(for: label)
    }
}
#elseif os(macOS)
extension KikariaSwiftMathLabel: NSViewRepresentable {
    func makeNSView(context: Context) -> MTMathUILabel {
        let label = MTMathUILabel()
        label.setContentHuggingPriority(.required, for: .horizontal)
        label.setContentHuggingPriority(.required, for: .vertical)
        label.setContentCompressionResistancePriority(.required, for: .horizontal)
        label.setContentCompressionResistancePriority(.required, for: .vertical)
        return label
    }

    func updateNSView(_ label: MTMathUILabel, context: Context) {
        configure(label)

        let hasError = label.error != nil
        if renderFailed != hasError {
            DispatchQueue.main.async {
                renderFailed = hasError
            }
        }
    }

    func sizeThatFits(
        _ proposal: CGSize,
        nsView label: MTMathUILabel,
        context: Context
    ) -> CGSize {
        configure(label)
        return measuredSize(for: label)
    }
    
    func sizeThatFits(
        _ proposal: ProposedViewSize,
        nsView label: MTMathUILabel,
        context: Context
    ) -> CGSize? {
        configure(label)
        return measuredSize(for: label)
    }
}
#endif

private extension KikariaSwiftMathLabel {

    func configure(_ label: MTMathUILabel) {
        label.displayErrorInline = false
        label.labelMode = labelMode
        label.textAlignment = textAlignment
        label.textColor = MTColor(textColor)
        label.contentInsets = contentInsets

        if let mathFont = MTFontManager().font(withName: MathFont.latinModernFont.rawValue, size: fontSize) {
            label.font = mathFont
        }
        label.fontSize = fontSize
        label.latex = latex
        label.invalidateIntrinsicContentSize()
    }

    func measuredSize(for label: MTMathUILabel) -> CGSize {
        #if os(macOS)
        // SwiftMath's AppKit label reports its math bounds through fittingSize.
        // intrinsicContentSize falls back to a collapsed NSView metric here.
        let size = label.fittingSize
        #else
        let size = label.intrinsicContentSize
        #endif

        let expansion = measurementSafetyExpansion
        return CGSize(
            width: ceil(max(finiteDimension(size.width) + expansion.width, 1)),
            height: ceil(max(finiteDimension(size.height) + expansion.height, 1))
        )
    }

    private func finiteDimension(_ value: CGFloat) -> CGFloat {
        value.isFinite && value > 0 ? value : 0
    }

    private var labelMode: MTMathUILabelMode {
        switch displayStyle {
        case .inline:
            return .text
        case .block:
            return .display
        }
    }

    private var textAlignment: MTTextAlignment {
        switch alignment {
        case .left:
            return .left
        case .center:
            return .center
        }
    }

    private var contentInsets: MTEdgeInsets {
        #if os(macOS)
        if usesGenerousVerticalSpacing {
            switch displayStyle {
            case .inline:
                return MTEdgeInsets(top: 4, left: 0, bottom: 4, right: 1)
            case .block:
                return MTEdgeInsets(top: 10, left: 4, bottom: 10, right: 8)
            }
        }
        #endif

        switch displayStyle {
        case .inline:
            return MTEdgeInsets(top: 2, left: 0, bottom: 2, right: 1)
        case .block:
            return MTEdgeInsets(top: 4, left: 4, bottom: 4, right: 8)
        }
    }

    private var measurementSafetyExpansion: CGSize {
        #if os(macOS)
        if usesGenerousVerticalSpacing {
            switch displayStyle {
            case .inline:
                return CGSize(width: 1, height: 8)
            case .block:
                return CGSize(width: 8, height: 16)
            }
        }
        #endif

        switch displayStyle {
        case .inline:
            return CGSize(width: 0, height: 2)
        case .block:
            return CGSize(width: 4, height: 2)
        }
    }
}
