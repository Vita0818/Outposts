//
//  KikariaLatexParser.swift
//  Kikaria
//
//  Created by Codex on 2026/5/10.
//

import Foundation

enum KikariaLatexParser {
    static func tokenize(_ text: String) -> [KikariaLatexToken] {
        KikariaLatexTextScanner(text: text).scan()
    }
}

private final class KikariaLatexTextScanner {
    private let characters: [Character]
    private var index = 0
    private var textBuffer = ""
    private var tokens: [KikariaLatexToken] = []

    init(text: String) {
        characters = Array(text)
    }

    func scan() -> [KikariaLatexToken] {
        while index < characters.count {
            if startsWith("```", at: index) {
                appendCodeSpan(fence: "```")
            } else if characters[index] == "`" {
                appendCodeSpan(fence: "`")
            } else if isEscapedDollar(at: index) {
                textBuffer.append("$")
                index += 2
            } else if characters[index] == "$" {
                scanMathToken()
            } else {
                textBuffer.append(characters[index])
                index += 1
            }
        }

        flushText()
        return tokens
    }

    private func scanMathToken() {
        if startsWith("$$", at: index) {
            scanBlockMath()
        } else {
            scanInlineMath()
        }
    }

    private func scanBlockMath() {
        let start = index
        guard let closeIndex = closingDoubleDollarIndex(startingAt: index + 2) else {
            textBuffer.append(contentsOf: characters[start...])
            index = characters.count
            return
        }

        let body = String(characters[(start + 2)..<closeIndex])
        let source = String(characters[start..<(closeIndex + 2)])

        flushText()
        tokens.append(.blockMath(source: source, body: body))
        index = closeIndex + 2
    }

    private func scanInlineMath() {
        let start = index
        guard let closeIndex = closingSingleDollarIndex(startingAt: index + 1) else {
            textBuffer.append(contentsOf: characters[start...])
            index = characters.count
            return
        }

        let body = String(characters[(start + 1)..<closeIndex])
        let source = String(characters[start...closeIndex])

        flushText()
        tokens.append(.inlineMath(source: source, body: body))
        index = closeIndex + 1
    }

    private func appendCodeSpan(fence: String) {
        let start = index
        index += fence.count

        while index < characters.count {
            if startsWith(fence, at: index) {
                index += fence.count
                textBuffer.append(String(characters[start..<index]))
                return
            }

            index += 1
        }

        textBuffer.append(String(characters[start..<characters.count]))
    }

    private func closingDoubleDollarIndex(startingAt startIndex: Int) -> Int? {
        var searchIndex = startIndex
        while searchIndex < characters.count - 1 {
            if startsWith("$$", at: searchIndex), !isEscaped(at: searchIndex) {
                return searchIndex
            }

            searchIndex += 1
        }

        return nil
    }

    private func closingSingleDollarIndex(startingAt startIndex: Int) -> Int? {
        var searchIndex = startIndex
        while searchIndex < characters.count {
            if characters[searchIndex] == "\n" {
                return nil
            }

            if characters[searchIndex] == "$",
               !isEscaped(at: searchIndex),
               !startsWith("$$", at: searchIndex) {
                return searchIndex
            }

            searchIndex += 1
        }

        return nil
    }

    private func flushText() {
        guard !textBuffer.isEmpty else {
            return
        }

        tokens.append(.text(textBuffer))
        textBuffer.removeAll(keepingCapacity: true)
    }

    private func isEscapedDollar(at characterIndex: Int) -> Bool {
        characterIndex + 1 < characters.count &&
            characters[characterIndex] == "\\" &&
            characters[characterIndex + 1] == "$"
    }

    private func isEscaped(at characterIndex: Int) -> Bool {
        var slashCount = 0
        var searchIndex = characterIndex - 1
        while searchIndex >= 0, characters[searchIndex] == "\\" {
            slashCount += 1
            searchIndex -= 1
        }

        return slashCount % 2 == 1
    }

    private func startsWith(_ marker: String, at startIndex: Int) -> Bool {
        let markerCharacters = Array(marker)
        guard startIndex >= 0,
              startIndex + markerCharacters.count <= characters.count
        else {
            return false
        }

        for offset in markerCharacters.indices where characters[startIndex + offset] != markerCharacters[offset] {
            return false
        }

        return true
    }
}
