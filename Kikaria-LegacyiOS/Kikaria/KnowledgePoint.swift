//
//  KnowledgePoint.swift
//  Kikaria
//
//  Created by Codex on 2026/5/1.
//

import Foundation

struct KnowledgePoint: Identifiable, Equatable, Codable {
    let id: UUID
    var title: String
    var tags: [String]
    var hint: String
    var content: String
    var isReinforced: Bool
    var reinforcementCount: Int
    var lastReinforcedAt: Date?
    var isMastered: Bool
    var createdAt: Date
    var updatedAt: Date

    init(
        id: UUID,
        title: String,
        tags: [String],
        hint: String,
        content: String,
        isReinforced: Bool = false,
        isMastered: Bool = false,
        createdAt: Date,
        updatedAt: Date,
        reinforcementCount: Int? = nil,
        lastReinforcedAt: Date? = nil
    ) {
        self.id = id
        self.title = title
        self.tags = tags
        self.hint = hint
        self.content = content
        let migratedReinforcementCount = max(0, reinforcementCount ?? (isReinforced ? 1 : 0))
        self.reinforcementCount = migratedReinforcementCount
        self.isReinforced = migratedReinforcementCount > 0
        self.lastReinforcedAt = migratedReinforcementCount > 0 ? lastReinforcedAt : nil
        self.isMastered = isMastered
        self.createdAt = createdAt
        self.updatedAt = updatedAt
    }

    private enum CodingKeys: String, CodingKey {
        case id
        case title
        case tags
        case hint
        case content
        case isReinforced
        case reinforcementCount
        case lastReinforcedAt
        case isMastered
        case createdAt
        case updatedAt
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(UUID.self, forKey: .id)
        title = try container.decode(String.self, forKey: .title)
        tags = try container.decode([String].self, forKey: .tags)
        hint = try container.decode(String.self, forKey: .hint)
        content = try container.decode(String.self, forKey: .content)
        let legacyIsReinforced = try container.decodeIfPresent(Bool.self, forKey: .isReinforced) ?? false
        let decodedReinforcementCount = try container.decodeIfPresent(Int.self, forKey: .reinforcementCount)
        reinforcementCount = max(0, decodedReinforcementCount ?? (legacyIsReinforced ? 1 : 0))
        isReinforced = reinforcementCount > 0
        lastReinforcedAt = try container.decodeIfPresent(Date.self, forKey: .lastReinforcedAt)
        if reinforcementCount == 0 {
            lastReinforcedAt = nil
        }
        isMastered = try container.decodeIfPresent(Bool.self, forKey: .isMastered) ?? false
        createdAt = try container.decodeIfPresent(Date.self, forKey: .createdAt) ?? Date()
        updatedAt = try container.decodeIfPresent(Date.self, forKey: .updatedAt) ?? createdAt
    }

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(id, forKey: .id)
        try container.encode(title, forKey: .title)
        try container.encode(tags, forKey: .tags)
        try container.encode(hint, forKey: .hint)
        try container.encode(content, forKey: .content)
        try container.encode(reinforcementCount > 0, forKey: .isReinforced)
        try container.encode(reinforcementCount, forKey: .reinforcementCount)
        try container.encodeIfPresent(lastReinforcedAt, forKey: .lastReinforcedAt)
        try container.encode(isMastered, forKey: .isMastered)
        try container.encode(createdAt, forKey: .createdAt)
        try container.encode(updatedAt, forKey: .updatedAt)
    }

    mutating func addReinforcement(at date: Date = Date()) -> Int {
        reinforcementCount = max(0, reinforcementCount) + 1
        isReinforced = true
        lastReinforcedAt = date
        updatedAt = date
        return reinforcementCount
    }

    mutating func clearReinforcement(at date: Date = Date()) {
        reinforcementCount = 0
        isReinforced = false
        lastReinforcedAt = nil
        updatedAt = date
    }
}

enum KnowledgePointMarkdownError: LocalizedError {
    case noValidKnowledgePoints

    var errorDescription: String? {
        switch self {
        case .noValidKnowledgePoints:
            return "No valid knowledge points were found."
        }
    }
}

extension KnowledgePoint {
    static let samples: [KnowledgePoint] = {
        (try? parseMarkdown(KnowledgePreset.defaultPreset.markdownText, date: Date(timeIntervalSince1970: 1_777_654_400))) ?? []
    }()

    static func parseMarkdown(_ markdown: String, date: Date = Date()) throws -> [KnowledgePoint] {
        let normalizedText = markdown
            .replacingOccurrences(of: "\r\n", with: "\n")
            .replacingOccurrences(of: "\r", with: "\n")
        let chunks = splitMarkdownIntoChunks(normalizedText)
        let points = chunks.compactMap { parseChunk($0, date: date) }

        guard !points.isEmpty else {
            throw KnowledgePointMarkdownError.noValidKnowledgePoints
        }

        return points
    }

    static func markdownText(from points: [KnowledgePoint]) -> String {
        points.map { point in
            """
            # \(point.title)

            tags: \(point.tags.joined(separator: ", "))

            hint:
            \(point.hint)

            content:
            \(point.content)
            """
        }
        .joined(separator: "\n\n---\n\n")
    }

    private static func splitMarkdownIntoChunks(_ markdown: String) -> [String] {
        var chunks: [String] = []
        var currentLines: [String] = []

        for line in markdown.components(separatedBy: "\n") {
            if line.trimmingCharacters(in: .whitespacesAndNewlines) == "---" {
                let chunk = currentLines.joined(separator: "\n").trimmingCharacters(in: .whitespacesAndNewlines)
                if !chunk.isEmpty {
                    chunks.append(chunk)
                }
                currentLines.removeAll()
            } else {
                currentLines.append(line)
            }
        }

        let finalChunk = currentLines.joined(separator: "\n").trimmingCharacters(in: .whitespacesAndNewlines)
        if !finalChunk.isEmpty {
            chunks.append(finalChunk)
        }

        return chunks
    }

    private static func parseChunk(_ chunk: String, date: Date) -> KnowledgePoint? {
        let lines = chunk.components(separatedBy: "\n")
        guard let titleIndex = lines.firstIndex(where: { !$0.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty }) else {
            return nil
        }

        let rawTitle = lines[titleIndex].trimmingCharacters(in: .whitespacesAndNewlines)
        guard rawTitle.hasPrefix("#") else {
            return nil
        }

        let title = rawTitle
            .drop(while: { $0 == "#" })
            .trimmingCharacters(in: .whitespacesAndNewlines)
        guard !title.isEmpty else {
            return nil
        }

        let tags = parseTags(from: lines)
        guard let hintIndex = markerIndex("hint:", in: lines),
              let contentIndex = markerIndex("content:", in: lines),
              hintIndex < contentIndex
        else {
            return nil
        }

        let hint = lines[(hintIndex + 1)..<contentIndex]
            .joined(separator: "\n")
            .trimmingCharacters(in: .whitespacesAndNewlines)
        let content = lines[(contentIndex + 1)..<lines.count]
            .joined(separator: "\n")
            .trimmingCharacters(in: .whitespacesAndNewlines)

        guard !hint.isEmpty, !content.isEmpty else {
            return nil
        }

        return KnowledgePoint(
            id: UUID(),
            title: title,
            tags: tags,
            hint: hint,
            content: content,
            isReinforced: false,
            isMastered: false,
            createdAt: date,
            updatedAt: date
        )
    }

    private static func parseTags(from lines: [String]) -> [String] {
        guard let tagLine = lines.first(where: {
            $0.trimmingCharacters(in: .whitespacesAndNewlines).lowercased().hasPrefix("tags:")
        }) else {
            return []
        }

        let tagText = tagLine
            .trimmingCharacters(in: .whitespacesAndNewlines)
            .dropFirst("tags:".count)

        return tagText
            .split(whereSeparator: { $0 == "," || $0 == "，" })
            .map { $0.trimmingCharacters(in: .whitespacesAndNewlines) }
            .filter { !$0.isEmpty }
    }

    private static func markerIndex(_ marker: String, in lines: [String]) -> Int? {
        lines.firstIndex {
            $0.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() == marker
        }
    }
}

struct KnowledgePreset: Identifiable, Equatable, Codable {
    var id: String
    var name: String
    var subtitle: String
    var description: String
    var category: String
    var markdownText: String
    var isBuiltIn: Bool

    init(
        id: String,
        name: String,
        subtitle: String,
        description: String,
        category: String,
        markdownText: String,
        isBuiltIn: Bool
    ) {
        self.id = id
        self.name = name
        self.subtitle = subtitle
        self.description = description
        self.category = category
        self.markdownText = markdownText
        self.isBuiltIn = isBuiltIn
    }

    private enum CodingKeys: String, CodingKey {
        case id
        case name
        case subtitle
        case description
        case category
        case markdownText
        case isBuiltIn
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(String.self, forKey: .id)
        name = try container.decode(String.self, forKey: .name)
        subtitle = try container.decodeIfPresent(String.self, forKey: .subtitle) ?? ""
        description = try container.decodeIfPresent(String.self, forKey: .description) ?? subtitle
        category = try container.decodeIfPresent(String.self, forKey: .category) ?? "自定义"
        markdownText = try container.decode(String.self, forKey: .markdownText)
        isBuiltIn = try container.decodeIfPresent(Bool.self, forKey: .isBuiltIn) ?? false
    }

    var knowledgePointCount: Int {
        (try? KnowledgePoint.parseMarkdown(markdownText).count) ?? 0
    }

    private struct BuiltInMarkdownResource {
        let url: URL

        var id: String {
            "builtin-\(displayName)"
        }

        var fileName: String {
            url.lastPathComponent
        }

        var displayName: String {
            url.deletingPathExtension().lastPathComponent
        }
    }

    static let builtInSeedVersion = 4

    private static let presetsResourceDirectory = "Presets"
    private static let markdownFileExtension = "md"
    private static let builtInCategory = "内置预设"
    private static let emptyBuiltInPreset = KnowledgePreset(
        id: "builtin-empty",
        name: "内置预设",
        subtitle: "内置知识点",
        description: "未找到内置 Markdown 预设。",
        category: builtInCategory,
        markdownText: "",
        isBuiltIn: true
    )

    static let all: [KnowledgePreset] = loadBuiltInPresets()
    static let currentBuiltInPresetIDs = Set(all.map(\.id))
    static let defaultPresetID = all.first?.id ?? emptyBuiltInPreset.id

    static var defaultPreset: KnowledgePreset {
        all.first ?? emptyBuiltInPreset
    }

    private static func loadBuiltInPresets() -> [KnowledgePreset] {
        let resources = bundledMarkdownResources()
        if resources.isEmpty {
            #if DEBUG
            print("No bundled preset Markdown files found in \(presetsResourceDirectory)")
            #endif
        }

        return resources.map { makeBuiltInPreset(from: $0) }
    }

    private static func bundledMarkdownResources() -> [BuiltInMarkdownResource] {
        let urls = Bundle.main.urls(
            forResourcesWithExtension: markdownFileExtension,
            subdirectory: presetsResourceDirectory
        ) ?? []

        return urls
            .sorted { lhs, rhs in
                if lhs.lastPathComponent == rhs.lastPathComponent {
                    return lhs.path < rhs.path
                }

                return lhs.lastPathComponent < rhs.lastPathComponent
            }
            .map { BuiltInMarkdownResource(url: $0) }
    }

    private static func bundledMarkdownText(for resource: BuiltInMarkdownResource) -> String {
        guard let markdown = try? String(contentsOf: resource.url, encoding: .utf8) else {
            #if DEBUG
            print("Missing bundled preset Markdown: \(resource.fileName)")
            #endif
            return ""
        }

        return markdown.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private static func makeBuiltInPreset(from resource: BuiltInMarkdownResource) -> KnowledgePreset {
        KnowledgePreset(
            id: resource.id,
            name: resource.displayName,
            subtitle: "\(resource.displayName)知识点",
            description: "由内置 Markdown 文件「\(presetsResourceDirectory)/\(resource.fileName)」提供的知识点预设。",
            category: builtInCategory,
            markdownText: bundledMarkdownText(for: resource),
            isBuiltIn: true
        )
    }
}
