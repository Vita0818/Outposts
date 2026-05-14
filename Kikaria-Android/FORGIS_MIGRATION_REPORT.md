# FORGIS Migration Report — Kikaria iOS → Android

## Source Inspection

### Directories Inspected

| Path | Description |
|---|---|
| `source/` (root) | Project-level README, SPEC, CODEX_CONTEXT |
| `source/Kikaria/` | Main iOS app target — SwiftUI source files |
| `source/KikariaMac/` | macOS target (not ported in this pass) |
| `source/KikariaWidget/` | WidgetKit extension (not ported) |

### Source Files Read

| File | Purpose |
|---|---|
| `KikariaApp.swift` | App entry point, notification delegate setup |
| `ContentView.swift` | ~9900-line core file: theme, glass modifiers, AppRoute, all screens, state management |
| `KnowledgePoint.swift` | `KnowledgePoint` and `KnowledgePreset` models, Markdown parser, built-in presets |
| `StudyTracking.swift` | `StudyActivityRecord`, `WidgetSnapshot`, `WidgetDataStore` |
| `KikariaTypography.swift` | Mixed Chinese/serif font system, font sizes |
| `KikariaAdaptiveLayout.swift` | Width-based adaptive layout metrics for phone/tablet |
| `KikariaLatexParser.swift` | LaTeX tokenizer for inline/block math |
| `KikariaMathFormulaView.swift` | SwiftMath-based formula rendering |
| `KikariaMathText.swift` | Mixed text + inline/block math flow layout |
| `LatexToken.swift` | Token enum for text/inlineMath/blockMath/fallback |
| `README.md` | Project overview and v0.1 goals |
| `SPEC.md` | v0.1 product specification |
| `CODEX_CONTEXT.md` | Full architecture and rules documentation |

### Major Source Concepts Identified

1. **Data Models** (`KnowledgePoint`, `KnowledgePreset`, `StudyActivityRecord`, `WidgetSnapshot`) — `KnowledgePoint` is the central entity with id, title, tags, hint, content, `reinforcementCount` (not a boolean), `isMastered`, timestamps.

2. **Markdown Import Format** — `# Title`, `tags:`, `hint:`, `content:`, `---` delimiter. Same format used for both import and export.

3. **Review Flow** — Three modes: Normal, Reinforcement, Mastered. Each has distinct bottom action bar layout. Sequence: title → hint → content → actions.

4. **Reinforcement System** — Counter-based (`reinforcementCount`), not boolean. Items can be reinforced multiple times. Ordered by count descending.

5. **Preset System** — Multiple built-in presets with independent study state. Presets bundle knowledge points as raw Markdown text.

6. **Liquid Glass Visual Design** — Custom `ViewModifier`s producing glass-morphism cards, capsules, and circles with gradient borders and adaptive light/dark colors.

7. **Kikaria Theme** — Adaptive color palette: sky blue, cyan, mint, lavender, green. Page gradient, action gradient, mastered gradient. Named tones for deep/soft/tertiary text.

8. **Typography** — Mixed Chinese/serif system: Chinese characters use system font, Latin uses serif design. App title at 39pt semibold serif.

9. **Activity Tracking** — `StudyActivityRecord` with types: viewedHint, reviewedAnswer, markedMastered, removedMastered, addedReinforcement, removedReinforcement.

10. **LaTeX/Math** — Token-based parsing (`$...$` inline, `$$...$$` block) with SwiftMath rendering. Complex flow layout for mixed text+math content.

---

## Target Files Created

### Build System

| File | Purpose |
|---|---|
| `settings.gradle.kts` | Gradle project settings, repository config |
| `build.gradle.kts` | Root build file with plugin versions |
| `app/build.gradle.kts` | App module build: Compose, Material3, Navigation, Gson |
| `gradle/wrapper/gradle-wrapper.properties` | Gradle 8.5 wrapper config |

### Android Manifest & Resources

| File | Purpose |
|---|---|
| `app/src/main/AndroidManifest.xml` | Single activity, no permissions needed |
| `app/src/main/res/values/themes.xml` | Light theme |
| `app/src/main/res/values-night/themes.xml` | Dark theme |
| `app/src/main/res/values/strings.xml` | App name string |

### Kotlin Source Files (by package)

#### `com.vita0818.kikaria`

| File | Lines | Purpose |
|---|---|---|
| `MainActivity.kt` | ~35 | Entry activity + `KikariaApp()` composable |

#### `com.vita0818.kikaria.data`

| File | Lines | Purpose |
|---|---|---|
| `KnowledgePoint.kt` | ~75 | Core model with reinforcement/mastered logic |
| `KnowledgePreset.kt` | ~20 | Preset model with `DEFAULT_PRESET_ID` |
| `StudyActivityRecord.kt` | ~25 | Activity record + `StudyActivityType` enum |
| `SamplePresets.kt` | ~130 | Two built-in presets (math + English) |

#### `com.vita0818.kikaria.util`

| File | Lines | Purpose |
|---|---|---|
| `MarkdownParser.kt` | ~130 | Full parser: split chunks, parse title/tags/hint/content |

#### `com.vita0818.kikaria.viewmodel`

| File | Lines | Purpose |
|---|---|---|
| `KikariaViewModel.kt` | ~270 | Central state: presets, review queue, tag selection, reinforcement/mastered toggles, activity recording, toast |

#### `com.vita0818.kikaria.ui.theme`

| File | Lines | Purpose |
|---|---|---|
| `KikariaColors.kt` | ~120 | Full light/dark palette + gradients |
| `KikariaTheme.kt` | ~60 | Material3 theme with light/dark color schemes |

#### `com.vita0818.kikaria.ui.components`

| File | Lines | Purpose |
|---|---|---|
| `GlassComponents.kt` | ~110 | `GlassCard`, `GlassCapsule`, `GlassCircle`, `InfoCard` |

#### `com.vita0818.kikaria.ui.navigation`

| File | Lines | Purpose |
|---|---|---|
| `KikariaNavGraph.kt` | ~95 | NavHost: HOME, REVIEW, SCOPE, REINFORCEMENT, MASTERED |

#### `com.vita0818.kikaria.ui.home`

| File | Lines | Purpose |
|---|---|---|
| `HomeScreen.kt` | ~310 | Dashboard: bubble start button, date/progress, quick-action cards |

#### `com.vita0818.kikaria.ui.review`

| File | Lines | Purpose |
|---|---|---|
| `ReviewScreen.kt` | ~310 | Review flow: title card, hint/content reveal, mode-specific action bar |

#### `com.vita0818.kikaria.ui.reinforcement`

| File | Lines | Purpose |
|---|---|---|
| `ReinforcementScreen.kt` | ~175 | Reinforcement list with expandable items |

#### `com.vita0818.kikaria.ui.mastered`

| File | Lines | Purpose |
|---|---|---|
| `MasteredScreen.kt` | ~165 | Mastered list with expandable items |

#### `com.vita0818.kikaria.ui.scope`

| File | Lines | Purpose |
|---|---|---|
| `ScopeSelectionScreen.kt` | ~135 | Tag filter chips with clear-all button |

### Documentation

| File | Purpose |
|---|---|
| `README.md` | Project overview, setup instructions, first-pass summary |
| `FORGIS_MIGRATION_REPORT.md` | This report |

---

## Concept Mapping: SwiftUI → Android/Compose

| iOS/SwiftUI Concept | Android/Kotlin/Compose Equivalent |
|---|---|
| `@main struct KikariaApp: App` | `MainActivity` + `KikariaApp()` composable |
| `@ObservableObject` / `@StateObject` | `ViewModel` + `mutableStateOf` / `mutableStateListOf` |
| `NavigationStack(path:)` | `NavHost` + `composable()` routes |
| `AppRoute` enum | `Routes` object + string-based routes |
| `KikariaTheme` (adaptive colors) | `KikariaColors` object + `MaterialTheme` color scheme |
| `liquidGlassCard()` ViewModifier | `GlassCard` composable |
| `FloatingInfoCard` | `InfoCard` composable |
| `UserDefaults` + Codable JSON | In-memory state (→ Gson/JSON file or Room in future) |
| `WidgetKit` / `WidgetSnapshot` | Not ported (Android widgets use RemoteViews) |
| `SwiftMath` (`MTMathUILabel`) | Not ported (no direct Android equivalent yet) |
| `KikariaTypography.mixedText` | Not ported (would need `AnnotatedString` with span styling) |
| `KikariaAdaptiveLayout.Metrics` | Not ported (Compose `BoxWithConstraints` or `WindowSizeClass`) |
| `UNUserNotificationCenter` | Not ported (`NotificationManager` in future) |

---

## Known Gaps

1. **No persistent storage** — State is in-memory only. Adding JSON file persistence or Room database is the highest-priority next task.

2. **No LaTeX/math rendering** — The iOS app uses SwiftMath for inline and block math formulas. Android options: jlatexmath-android, MathJax WebView, or a custom canvas renderer.

3. **No swipe gestures in review** — The iOS app has up/down/left/right swipe gestures for reveal, next, previous, and mode-specific actions. Only button-based interaction is implemented.

4. **No tablet-adaptive layout** — The iOS app has a sophisticated `KikariaAdaptiveLayout` system for iPad portrait/landscape two-column layouts. Currently single-column only.

5. **No preset management UI** — Cannot create, edit, delete, or import presets. Only two built-in presets are available.

6. **No today overview or review history** — Missing the calendar-based activity visualization.

7. **No study notifications** — Missing local notifications for study reminders with progress tracking.

8. **No widget** — Android app widgets use a different architecture (RemoteViews) and were not ported.

9. **No user profile / onboarding** — Missing first-launch onboarding flow and profile setup.

10. **No countdown / daily goal settings** — Settings screen and daily goal adjustment not implemented.

11. **Typography simplification** — No mixed Chinese/serif font system. Using default Compose fonts.

---

## Safety Compliance

- ✅ All writes stayed inside `Kikaria-Android/`
- ✅ No writes to Outposts repository root
- ✅ No writes to `FORGIS_CONFIG.yml`
- ✅ No writes to workflow files
- ✅ No writes to source repository
- ✅ No secrets accessed, printed, or written
- ✅ No Forgis safety rule blocked an operation
- ✅ First Android project skeleton was produced

---

## Recommended Next Migration Tasks

### Priority 1 — Runnability & Persistence

1. **Add local persistence** — Implement JSON file read/write for `KikariaViewModel` state using Gson + internal storage. This makes study progress survive app restarts.
2. **Add proguard-rules.pro** — Create a basic ProGuard rules file for the release build type.

### Priority 2 — Core Feature Completion

3. **Preset management** — Add a preset selection dialog/screen, "New Preset" flow with Markdown import (file picker or paste), edit, and delete.
4. **Settings screen** — Daily goal picker, countdown date picker, dark mode toggle.
5. **Swipe gestures in review** — Implement up/down/left/right swipe with appropriate actions for each review mode.

### Priority 3 — Visual Polish

6. **Improve glass-morphism fidelity** — Add gradient borders, inner highlights, and more faithful shadow rendering to GlassCard composables.
7. **Typography refinement** — Add serif font for Latin text and system font for CJK, mimicking the `KikariaTypography.mixedText` system.
8. **Animations** — Add more sophisticated bubble animations, page transitions, and micro-interactions.

### Priority 4 — Advanced Features

9. **LaTeX/math rendering** — Integrate a math rendering solution for knowledge points containing formulas.
10. **Today overview & history** — Calendar-based activity visualization.
11. **Local notifications** — Study reminder notifications using `AlarmManager` or `WorkManager`.
12. **Tablet adaptive layout** — Two-column layouts for larger screens using `WindowSizeClass`.
13. **Android widget** — Basic app widget showing today's progress.

---

*Report generated by Forgis first-pass migration. Source: Vita0818/Kikaria → Target: Vita0818/Outposts/Kikaria-Android.*
