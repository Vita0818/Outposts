# FORGIS Translation Plan — Kikaria iOS → Android

## Overview

This document describes the staged migration plan for translating the Kikaria iOS (Swift/SwiftUI) app to Android (Kotlin/Jetpack Compose).

The goal is not a mechanical 1:1 file translation but an architectural, semantic, and visual translation that preserves the product identity of Kikaria.

## Source Scope

The iOS source consists of:
- **App entry**: `KikariaApp.swift` — WindowGroup → ContentView
- **Core UI + State**: `ContentView.swift` — ~9900 lines containing all screens, theme, glass modifiers, app state, notifications, routing
- **Data models**: `KnowledgePoint.swift` — KnowledgePoint, KnowledgePreset, Markdown parser, built-in presets
- **Study tracking**: `StudyTracking.swift` — StudyActivityRecord, WidgetSnapshot, WidgetDataStore
- **Typography**: `KikariaTypography.swift` — Mixed Chinese/serif font system
- **Adaptive layout**: `KikariaAdaptiveLayout.swift` — Width-based metrics for phone/tablet
- **LaTeX**: `KikariaLatexParser.swift`, `KikariaMathFormulaView.swift`, `KikariaMathText.swift`, `LatexToken.swift`
- **Presets**: 4 `.md` files — 大学物理 (5022 lines), 大学英语Band4, 微积分 (7480 lines), 离散数学 (27004 lines)
- **Docs**: `SPEC.md`, `CODEX_CONTEXT.md`, `README.md`

## Target Android Scope

The existing Android project has:
- **App entry**: `MainActivity.kt` — ComponentActivity with Compose
- **Navigation**: `KikariaNavGraph.kt` — 5 routes: HOME, REVIEW, SCOPE, REINFORCEMENT, MASTERED
- **ViewModel**: `KikariaViewModel.kt` — Central state with review queue, tag selection, reinforcement/mastered toggles
- **Data models**: `KnowledgePoint.kt`, `KnowledgePreset.kt`, `SamplePresets.kt`, `StudyActivityRecord.kt`
- **Markdown parser**: `MarkdownParser.kt`
- **Screens**: `HomeScreen.kt`, `ReviewScreen.kt`, `ScopeSelectionScreen.kt`, `ReinforcementScreen.kt`, `MasteredScreen.kt`
- **Theme**: `KikariaColors.kt`, `KikariaTheme.kt`
- **Components**: `GlassComponents.kt`

## Translation Stages

### Stage 1: Project Architecture & App Entry ✅ (from prior passes)
- MainActivity.kt, KikariaNavGraph.kt, build files — already done

### Stage 2: Data Models & Preset Parsing (this pass)
- **2a**: KnowledgePoint — verify fidelity; add `reinforcementCount` migration logic
- **2b**: KnowledgePreset — extend with `knowledgePointCount`, `builtInSeedVersion`
- **2c**: SamplePresets — add all 4 source presets (currently only 2)
- **2d**: MarkdownParser — verify parse fidelity against source
- **2e**: StudyActivityRecord — verify alignment

### Stage 3: ViewModel & State Semantics (this pass)
- **3a**: Add `countdownEndDate`, `dangerPercent`, `dailyReviewRecords`
- **3b**: Add `PresetStudyState` equivalent (per-preset state isolation)
- **3c**: Add study progress warning logic
- **3d**: Improve review flow state management

### Stage 4: Typography System (this pass)
- **4a**: Extract `KikariaTypography` object with named styles
- **4b**: Implement mixed Chinese/serif text rendering via `AnnotatedString`

### Stage 5: Home Screen (this pass)
- **5a**: Add countdown days display
- **5b**: Improve bubble animation fidelity
- **5c**: Add preset picker navigation entry
- **5d**: Add reinforcement count to dashboard

### Stage 6: Review Flow (this pass)
- **6a**: Add previous-point navigation (currently next-only)
- **6b**: Add swipe gestures for hint/content reveal
- **6c**: Improve glass card styling in review
- **6d**: Add point counter display

### Stage 7: Scope Selection (this pass)
- **7a**: Improve tag cloud UI with glass styling
- **7b**: Add "select all" / "deselect all"

### Stage 8: Reinforcement & Mastered (this pass)
- **8a**: Add empty-state illustrations/guidance
- **8b**: Improve card styling with glass treatment
- **8c**: Add multi-select for batch operations

### Stage 9: Theme & Components (this pass)
- **9a**: Add Native blur via RenderEffect (API 31+)
- **9b**: Add more glass variants (bubble, pill)
- **9c**: Ensure all colors have dark variants

### Stage 10: LaTeX / Math Support (deferred)
- Document approach; may use jlatexmath or WebView-based MathJax

### Stage 11: Preset Management (partial, this pass)
- **11a**: Add preset selection dialog/sheet
- **11b**: Add basic preset info display

### Stage 12: Persistence (deferred)
- JSON file persistence for app state

### Stage 13: Notifications (deferred)
- Study reminder notifications

### Stage 14: Adaptive Layout (deferred)
- Tablet support

## Prioritization

This pass prioritizes:
1. Data model fidelity and preset expansion (Stage 2)
2. ViewModel state improvements (Stage 3)
3. Typography system (Stage 4)
4. Home screen improvements (Stage 5)
5. Review flow improvements (Stage 6)
6. Theme fidelity (Stage 9)

Later passes should focus on:
- Persistence
- Preset management UI
- LaTeX/math rendering
- Notifications
- Tablet layout
