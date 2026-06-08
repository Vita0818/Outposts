//
//  StudyTracking.swift
//  Kikaria
//
//  Created by Codex on 2026/5/4.
//

import Foundation
import WidgetKit

enum StudyActivityType: String, Codable, CaseIterable {
    case viewedHint
    case reviewedAnswer
    case markedMastered
    case removedMastered
    case addedReinforcement
    case removedReinforcement
}

struct StudyActivityRecord: Identifiable, Codable, Equatable {
    var id: UUID
    var presetId: String
    var date: Date
    var type: StudyActivityType
    var pointId: UUID
    var pointTitle: String

    init(
        id: UUID = UUID(),
        presetId: String,
        date: Date = Date(),
        type: StudyActivityType,
        pointId: UUID,
        pointTitle: String
    ) {
        self.id = id
        self.presetId = presetId
        self.date = date
        self.type = type
        self.pointId = pointId
        self.pointTitle = pointTitle
    }
}

struct WidgetKnowledgePointPreview: Codable, Equatable {
    var title: String
    var tag: String?
}

struct WidgetSnapshot: Codable {
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
            WidgetKnowledgePointPreview(title: "极限的保号性", tag: "极限")
        ],
        lastUpdated: Date()
    )
}

enum WidgetDataStore {
    static let appGroupID = "group.com.vita0818.kikaria"
    static let snapshotKey = "kikaria.widgetSnapshot"

    static func save(_ snapshot: WidgetSnapshot) {
        guard let data = try? JSONEncoder().encode(snapshot) else {
            return
        }

        if let appGroupDefaults = UserDefaults(suiteName: appGroupID) {
            appGroupDefaults.set(data, forKey: snapshotKey)
        }

        UserDefaults.standard.set(data, forKey: snapshotKey)
        WidgetCenter.shared.reloadAllTimelines()
    }
}
