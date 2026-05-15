# FORGIS Source-Target Map — Kikaria iOS → Android

## Status Legend

- ✅ **translated** — Feature is fully migrated and functional
- 🟡 **partially translated** — Core functionality exists but details missing
- ❌ **missing** — Not yet implemented
- 📅 **intentionally deferred** — Planned for future pass
- 🔍 **needs review** — Translation exists but should be verified

---

## Source → Target File Mapping

### Core Application

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `KikariaApp.swift` | `MainActivity.kt` | ✅ | App entry mapped; notification delegate not ported |
| `ContentView.swift` (AppRoute) | `KikariaNavGraph.kt` + `Routes` | ✅ | Route enum → NavHost routes |
| `ContentView.swift` (KikariaTheme colors) | `KikariaColors.kt` | ✅ | All adaptive colors mapped |
| `ContentView.swift` (LiquidGlassCardModifier) | `GlassComponents.kt` (GlassCard) | 🟡 | Visual treatment mapped; no native blur |
| `ContentView.swift` (KikariaAppState) | `KikariaViewModel.kt` | 🟡 | Core state mapped; persistence not implemented |
| `ContentView.swift` (PresetStudyState) | `KikariaViewModel.kt` | 🔍 | Per-preset state isolation missing |
| `ContentView.swift` (UserProfile) | Not mapped | 📅 | Deferred |

### Data Models

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `KnowledgePoint.swift` (KnowledgePoint) | `data/KnowledgePoint.kt` | ✅ | All fields mapped; `addReinforcement`/`clearReinforcement` semantics preserved |
| `KnowledgePoint.swift` (KnowledgePreset) | `data/KnowledgePreset.kt` | 🟡 | Basic fields mapped; `knowledgePointCount` missing; `builtInSeedVersion` missing |
| `KnowledgePoint.swift` (Markdown parsing) | `util/MarkdownParser.kt` | ✅ | Full parse logic translated |
| `KnowledgePoint.swift` (Built-in presets) | `data/SamplePresets.kt` | 🟡 | Only 2 of 4 presets populated |
| `StudyTracking.swift` (StudyActivityRecord) | `data/StudyActivityRecord.kt` | ✅ | All types and fields mapped |
| `StudyTracking.swift` (WidgetSnapshot) | Not mapped | 📅 | Android widgets use different architecture |
| `LatexToken.swift` | Not mapped | 📅 | Deferred until LaTeX support |

### Typography

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `KikariaTypography.swift` | Not yet created | ❌ | Mixed Chinese/serif system needed |

### LaTeX / Math

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `KikariaLatexParser.swift` | Not mapped | 📅 | Deferred |
| `KikariaMathFormulaView.swift` | Not mapped | 📅 | Deferred; needs jlatexmath or WebView MathJax |
| `KikariaMathText.swift` | Not mapped | 📅 | Deferred |

### Adaptive Layout

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `KikariaAdaptiveLayout.swift` | Not mapped | 📅 | Tablet support deferred |

### Presets

| Source | Target | Status | Notes |
|--------|--------|--------|-------|
| `Presets/大学物理.md` | `data/SamplePresets.kt` | ❌ | Not yet imported |
| `Presets/大学英语Band4.md` | `data/SamplePresets.kt` | 🟡 | Partial; only 5 entries |
| `Presets/微积分.md` | `data/SamplePresets.kt` | ❌ | Not yet imported |
| `Presets/离散数学.md` | `data/SamplePresets.kt` | ❌ | Not yet imported |

---

## Functional Unit Mapping

### Home Screen

| Source Feature (ContentView.swift) | Target | Status | Notes |
|--------|--------|--------|-------|
| App title "Kikaria" (serif) | `HomeScreen.kt` | 🟡 | Serif font added; typography not extracted |
| StartReviewButton with orbit animation | `HomeScreen.kt` KikariaStartButton | ✅ | Bubbles + orbit mapped |
| TodayOverviewHomeProgressButton | `HomeScreen.kt` TodayProgressCard | 🟡 | Layout mapped; countdown days placeholder |
| HomeDashboardGridCard | `HomeScreen.kt` DashboardCard | ✅ | 3-column layout mapped |
| Preset selector row | `HomeScreen.kt` DashboardCard preset row | 🔍 | Row exists; navigation TODO |
| Profile avatar | `HomeScreen.kt` avatar placeholder | 📅 | Just "V" initial |

### Review Flow

| Source Feature | Target | Status | Notes |
|--------|--------|--------|-------|
| Title display | `ReviewScreen.kt` | ✅ | |
| Hint reveal | `ReviewScreen.kt` | ✅ | |
| Content reveal | `ReviewScreen.kt` | ✅ | |
| Mode-specific action bar | `ReviewScreen.kt` BottomActionBar | ✅ | |
| Previous point navigation | `ReviewScreen.kt` | ❌ | Next-only currently |
| Swipe gestures | Not mapped | ❌ | |
| Glass card styling | `ReviewScreen.kt` | 🟡 | Uses GlassCard but not full liquid glass treatment |

### Scope Selection

| Source Feature | Target | Status | Notes |
|--------|--------|--------|-------|
| Tag filter chips | `ScopeSelectionScreen.kt` | ✅ | |
| Clear all button | `ScopeSelectionScreen.kt` | ✅ | |
| Preview count | `ScopeSelectionScreen.kt` | ✅ | |

### Reinforcement

| Source Feature | Target | Status | Notes |
|--------|--------|--------|-------|
| Sorted list (by count) | `ReinforcementScreen.kt` | ✅ | |
| Expandable items | `ReinforcementScreen.kt` | ✅ | |
| Remove from list | `ReinforcementScreen.kt` | ✅ | |
| Start review from list | `ReinforcementScreen.kt` | ✅ | |

### Mastered

| Source Feature | Target | Status | Notes |
|--------|--------|--------|-------|
| Sorted list | `MasteredScreen.kt` | ✅ | |
| Expandable items | `MasteredScreen.kt` | ✅ | |
| Remove from list | `MasteredScreen.kt` | ✅ | |
| Start review from list | `MasteredScreen.kt` | ✅ | |

---

## Summary

| Category | ✅ | 🟡 | ❌ | 📅 | 🔍 |
|----------|-----|-----|-----|-----|-----|
| Core App | 2 | 2 | 0 | 1 | 1 |
| Data Models | 2 | 2 | 0 | 1 | 0 |
| Typography | 0 | 0 | 1 | 0 | 0 |
| LaTeX/Math | 0 | 0 | 0 | 3 | 0 |
| Adaptive Layout | 0 | 0 | 0 | 1 | 0 |
| Home Screen | 2 | 2 | 0 | 1 | 1 |
| Review Flow | 4 | 1 | 2 | 0 | 0 |
| Scope Selection | 3 | 0 | 0 | 0 | 0 |
| Reinforcement | 4 | 0 | 0 | 0 | 0 |
| Mastered | 4 | 0 | 0 | 0 | 0 |
| Presets | 0 | 1 | 2 | 0 | 0 |
