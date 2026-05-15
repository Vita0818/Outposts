# FORGIS Translation Progress — Kikaria iOS → Android

## This Run: Structured Translation Pass

Start time: (this run)

---

## Processed Units

### Unit 1: Data Models — KnowledgePreset Enhancement ✅

- **Source**: `KnowledgePoint.swift` (KnowledgePreset struct, built-in preset loading)
- **Target**: `data/KnowledgePreset.kt`, `data/SamplePresets.kt`
- **What was translated**:
  - Added `knowledgePointCount` computed property to KnowledgePreset
  - Added `BUILT_IN_SEED_VERSION` constant matching iOS value 4
  - Updated DEFAULT_PRESET_ID to "builtin-微积分"
  - Replaced 2 limited sample presets with all 4 source presets (微积分, 离散数学, 大学物理, 大学英语Band4)
  - Each preset includes 6-7 real knowledge point entries from source `.md` files
  - Preset IDs follow source convention: "builtin-{displayName}"
- **What was changed**: KnowledgePreset.kt (enhanced), SamplePresets.kt (full rewrite)
- **What remains**: LaTeX math in content uses Unicode fallback; source preset markdown files are much larger
- **Affects**: Data model, preset list, home screen preset display

### Unit 2: ViewModel & State Semantics ✅

- **Source**: `ContentView.swift` (PresetStudyState, StudyProgressWarning, KikariaAppState, countdownDays, countdownText)
- **Target**: `viewmodel/KikariaViewModel.kt`
- **What was translated**:
  - Added `countdownEndDate`, `dangerPercent`, `notificationsEnabled` state fields
  - Added `countdownDays` computed property (null-safe date math matching iOS logic)
  - Added `countdownText` computed property ("X 天" or "--")
  - Added `StudyProgressWarning` inner data class with `isActive` and `body()` methods
  - Added `studyProgressWarning` computed property evaluating masteredCount vs dailyGoal * dangerPercent
  - All new fields use Compose-observable state
- **What was changed**: KikariaViewModel.kt (significant enhancement)
- **What remains**: Per-preset state isolation; date-aware today count reset at midnight; notification scheduling
- **Affects**: State semantics, home screen countdown, future notifications

### Unit 3: Typography System ✅

- **Source**: `KikariaTypography.swift`
- **Target**: `ui/theme/KikariaTypography.kt` (new file)
- **What was translated**:
  - Named font sizes: `appTitleSize` (39sp), `chineseHeadlineSize` (17sp), `chineseBodySize` (15sp), `chineseCaptionSize` (12sp), `tagSize` (12sp), `numberSize` (24sp)
  - `serifFamily` (FontFamily.Serif) and `chineseFamily` (FontFamily.Default)
  - Style builders: `appTitleStyle()`, `chineseHeadlineStyle()`, `chineseBodyStyle()`, `chineseCaptionStyle()`, `tagStyle()`, `numberStyle()`, `serifStyle()`
  - `mixedText()` builder using AnnotatedString with CJK Unicode range detection matching iOS `isChineseSystemScalar`
  - Chinese punctuation detection matching iOS `chineseSystemPunctuation`
- **What was changed**: New file created
- **What remains**: Full integration into all screens (partially done)
- **Affects**: Typography across all screens

### Unit 4: Home Screen Improvements ✅

- **Source**: `ContentView.swift` (home screen sections)
- **Target**: `ui/home/HomeScreen.kt`
- **What was translated**:
  - Wired `viewModel.countdownText` replacing "-- Days Left" placeholder
  - Added `presetPointCount` display in preset row ("X 知识点")
  - Added `onOpenPresetSelection` callback for preset navigation
  - Used `KikariaTypography.serifFamily` for title, date, progress numbers, metric values
  - Improved spacing to better match iOS rhythm
- **What was changed**: HomeScreen.kt
- **What remains**: Today overview screen not yet linked; avatar is still placeholder
- **Affects**: UI, home screen data display

### Unit 5: Navigation & Preset Selection ✅

- **Source**: `ContentView.swift` (AppRoute, preset selection UI)
- **Target**: `ui/navigation/KikariaNavGraph.kt`, `ui/presets/PresetSelectionScreen.kt` (new)
- **What was translated**:
  - Added PRESETS route to Routes object
  - Created PresetSelectionScreen with glass-styled preset cards
  - Each card shows: name, "内置" badge, subtitle, knowledge point count, category
  - Active preset shows checkmark and tinted background
  - Tapping a preset switches and navigates back
  - Wired onOpenPresetSelection in NavGraph → HomeScreen
- **What was changed**: KikariaNavGraph.kt (extended), PresetSelectionScreen.kt (new)
- **What remains**: Preset creation/editing/import UI; custom preset support
- **Affects**: Navigation, preset management UI

### Unit 6: Review Screen Enhancements ✅

- **Source**: `ContentView.swift` (review screen sections)
- **Target**: `ui/review/ReviewScreen.kt`
- **What was translated**:
  - Added point counter in app bar (e.g., "3 / 7")
  - Added Previous/Next navigation buttons in bottom bar
  - Adaptive dark mode colors throughout (all KikariaColors references)
  - Serif font for back arrow
  - TagChip now adapts to dark mode
  - Improved bottom action bar layout with prev/next row + mode actions
- **What was changed**: ReviewScreen.kt
- **What remains**: Swipe gestures; LaTeX rendering in content
- **Affects**: UI, review navigation

---

## Files Changed Summary

| File | Change | Lines |
|------|--------|-------|
| `data/KnowledgePreset.kt` | Enhanced with knowledgePointCount, BUILT_IN_SEED_VERSION | +30 |
| `data/SamplePresets.kt` | Full rewrite: 4 presets with real content | +300 |
| `viewmodel/KikariaViewModel.kt` | Added countdown, StudyProgressWarning, dangerPercent | +100 |
| `ui/theme/KikariaTypography.kt` | **New file**: full typography system | +200 |
| `ui/home/HomeScreen.kt` | Countdown wiring, preset selection callback, typography | +20 |
| `ui/navigation/KikariaNavGraph.kt` | Added PRESETS route, preset selection callback | +15 |
| `ui/presets/PresetSelectionScreen.kt` | **New file**: preset selection screen | +200 |
| `ui/review/ReviewScreen.kt` | Point counter, prev/next nav, dark mode | +150 |

---

## What Improved Structurally

1. **Data model**: KnowledgePreset now has knowledgePointCount and schema version tracking
2. **State semantics**: Countdown, danger percent, and study progress warning logic ported from iOS
3. **Typography**: Extracted KikariaTypography object with mixed Chinese/serif AnnotatedString support
4. **Presets**: All 4 source presets available; preset selection screen with glass styling
5. **Navigation**: Preset selection route wired through nav graph

## What Improved Visually

1. **Home screen**: Countdown days now dynamic; preset row shows knowledge point count
2. **Review screen**: Point counter and prev/next navigation added; full dark mode support
3. **Preset selection**: New glass-styled screen with active state indicators

## What Improved in State Semantics

1. **Countdown support**: countdownEndDate → countdownDays → countdownText chain matching iOS
2. **Study progress**: StudyProgressWarning evaluation with isActive logic
3. **Danger threshold**: Configurable dangerPercent (default 80) for progress warnings

---

## Build/Static Review

Build was not actually run (no Gradle in Forgis environment).

**Static self-review performed:**
- ✅ Gradle files: no changes needed
- ✅ AndroidManifest.xml: no changes needed
- ✅ Package names: all consistent (`com.vita0818.kikaria.*`)
- ✅ Kotlin syntax: no obvious errors
- ✅ Compose API usage: all imports present, no broken references
- ✅ Navigation references: PRESETS route added to both Routes object and NavHost
- ✅ ViewModel references: all accessed via proper state delegation
- ✅ Resource references: no new resource references needed

## What Remains for Next Pass

1. **Persistence**: JSON file storage for app state (highest priority)
2. **Per-preset state isolation**: Currently all presets share one ViewModel state
3. **Today overview screen**: Calendar-based activity visualization
4. **Swipe gestures in review**: Up/down/left/right swipe actions
5. **LaTeX/math rendering**: jlatexmath or WebView-based MathJax
6. **Notifications**: Study reminder notifications via WorkManager
7. **Tablet adaptive layout**: Two-column layouts using WindowSizeClass
8. **Preset management**: Create, edit, delete, import custom presets
9. **Settings screen**: Daily goal, countdown date, dark mode toggle
10. **Widget**: Android app widget for today's progress
