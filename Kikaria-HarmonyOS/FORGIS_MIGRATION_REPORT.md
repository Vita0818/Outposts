# FORGIS Migration Report — Kikaria → Kikaria-HarmonyOS

## Migration Overview

- **Source**: `Vita0818/Kikaria` (iOS SwiftUI app)
- **Target**: `Vita0818/Outposts` → `Kikaria-HarmonyOS`
- **Type**: First-pass HarmonyOS port
- **Approach**: Translation-first, preserving product direction, data models, and UX

## Source Paths Inspected

| Path | Purpose |
|------|---------|
| `source/README.md` | Project overview and design principles |
| `source/SPEC.md` | v0.1 specification with data model and screen requirements |
| `source/Kikaria/KikariaApp.swift` | iOS app entry point |
| `source/Kikaria/ContentView.swift` (partial, ~1400/9895 lines) | Main UI, theme, navigation, state management |
| `source/Kikaria/KnowledgePoint.swift` | Core data models, Markdown parser, presets |
| `source/Kikaria/StudyTracking.swift` | Study activity records, widget data |
| `source/Kikaria/KikariaTypography.swift` | Typography system with Chinese/Serif mixed text |
| `source/Kikaria/KikariaAdaptiveLayout.swift` | Responsive layout metrics |
| `source/Kikaria/LatexToken.swift` | LaTeX token types |
| `source/Kikaria/KikariaLatexParser.swift` | LaTeX text scanner |
| `source/Kikaria/KikariaMathFormulaView.swift` | Math formula rendering (SwiftMath) |
| `source/Kikaria/KikariaMathText.swift` | Rich text with inline math |
| `source/KikariaMac/KikariaMacApp.swift` | macOS app entry |
| `source/KikariaMac/KikariaMacRootView.swift` | macOS root view wrapper |

## Target Files Created

### Project Configuration (7 files)
| File | Description |
|------|-------------|
| `Kikaria-HarmonyOS/oh-package.json5` | Root package with Hvigor plugin dependency |
| `Kikaria-HarmonyOS/build-profile.json5` | Build config: API 12, HarmonyOS, entry module |
| `Kikaria-HarmonyOS/hvigorfile.ts` | Root Hvigor build entry |
| `Kikaria-HarmonyOS/AppScope/app.json5` | App bundle config (com.vita0818.kikaria) |
| `Kikaria-HarmonyOS/entry/oh-package.json5` | Entry module package |
| `Kikaria-HarmonyOS/entry/build-profile.json5` | Entry build config, Stage mode |
| `Kikaria-HarmonyOS/entry/hvigorfile.ts` | Entry Hvigor build entry |

### Module & Resources (4 files)
| File | Description |
|------|-------------|
| `Kikaria-HarmonyOS/entry/src/main/module.json5` | Stage model module: EntryAbility, pages, device types |
| `Kikaria-HarmonyOS/entry/src/main/resources/base/element/string.json` | String resources (app_name, labels) |
| `Kikaria-HarmonyOS/entry/src/main/resources/base/element/color.json` | Color resource (start_window_background) |
| `Kikaria-HarmonyOS/entry/src/main/resources/base/profile/main_pages.json` | Page route registry |

### ArkTS Source Files (8 files)
| File | Description |
|------|-------------|
| `entry/src/main/ets/entryability/EntryAbility.ets` | Stage model UIAbility entry |
| `entry/src/main/ets/pages/Index.ets` | Home screen with stats, tags, review launch |
| `entry/src/main/ets/pages/ReviewPage.ets` | Review flow: hint/answer reveal, important/mastered toggles |
| `entry/src/main/ets/pages/ReinforcementPage.ets` | Important items list with expand/collapse |
| `entry/src/main/ets/pages/MasteredPage.ets` | Mastered items list with expand/collapse |
| `entry/src/main/ets/model/KnowledgePoint.ets` | KnowledgePoint, KnowledgePreset, Markdown parser, enums |
| `entry/src/main/ets/data/SamplePresets.ets` | Built-in Advanced Mathematics preset |
| `entry/src/main/ets/data/AppState.ets` | Singleton state: points, presets, review, tags |
| `entry/src/main/ets/components/KikariaTheme.ets` | Color constants and typography scale |
| `entry/src/main/ets/components/KikariaComponents.ets` | Reusable UI: KikariaCard, KikariaButton, TagChip |

### Documentation (2 files)
| File | Description |
|------|-------------|
| `Kikaria-HarmonyOS/README.md` | Project overview, setup, implemented features, gaps |
| `Kikaria-HarmonyOS/FORGIS_MIGRATION_REPORT.md` | This report |

## Major Source Concepts Identified

1. **KnowledgePoint** — Core entity with title, tags, hint, content, reinforcement tracking (count-based, not boolean), mastered state, timestamps. Codable for JSON persistence.

2. **KnowledgePreset** — Named collection of knowledge points loaded from Markdown text. Built-in presets with sample data. Preset library with default/current selection.

3. **Markdown Format** — Sections separated by `---`, each with: `# Title`, `tags: ...`, `hint:`, `content:`. Parser handles normalized line endings, chunk splitting, tag parsing with comma/semicolon delimiters.

4. **Review Flow** — Three modes: Normal (non-mastered), Reinforcement (important only), Mastered (completed only). Shuffled random queue. Hint → Content reveal progression. Reinforcement and Mastered toggling.

5. **Study Tracking** — Activity records (viewed hint, reviewed answer, marked mastered, etc.) per preset. Daily review records. Widget snapshot for iOS widget.

6. **Visual Design** — "Liquid glass" card design with adaptive light/dark colors. Sky/cyan/mist color palette. Serif font for English/math, system font for Chinese. Gradients for action buttons.

7. **Typography** — Mixed Chinese/Serif text rendering by detecting Unicode ranges. App title in serif, body text with adaptive font per character script.

8. **Adaptive Layout** — Sophisticated responsive metrics for compact/regular/wide widths. Two-column layouts for iPad/Mac landscape. Scale factors for portrait iPad.

9. **LaTeX Math** — Token-based parser for `$inline$` and `$$block$$` math. Rendered via SwiftMath (MTMathUILabel). Fallback to plain text on render failure.

10. **Notifications & Widget** — Study progress warnings with countdown dates. iOS widget showing daily mastered count, goal, random knowledge points.

## How Concepts Were Mapped to HarmonyOS / ArkTS / ArkUI

| Source Concept | HarmonyOS Mapping |
|---------------|-------------------|
| `@main App` with `WindowGroup` | Stage model `UIAbility` with `windowStage.loadContent('pages/Index')` |
| `@State` properties in ContentView | `AppState` singleton class with methods mutating state; pages use `@State` for local copies refreshed via `aboutToAppear` / `onPageShow` |
| `NavigationStack` with `NavigationLink` | `Navigation` component with `router.pushUrl()` / `router.back()` |
| `List` / `ForEach` with cards | `List` + `ListItem` + `ForEach` with `KikariaCard` @Component |
| LiquidGlassCard modifier | `KikariaCard` @Component with `borderRadius`, `backgroundColor`, `shadow` |
| `LinearGradient` buttons | Flat color `KikariaButton` with `borderRadius: 16` (gradients deferred to future pass) |
| `KikariaTheme` adaptive colors | `KikariaColors` class with light/dark hex constants; current pass uses light variants |
| `KikariaTypography` font system | `KikariaTypography` class with size constants; `fontFamily('serif')` for titles |
| `KnowledgePoint` struct (Codable) | `KnowledgePoint` class with manual properties (no JSON persistence yet) |
| `parseMarkdown()` static method | `parseMarkdown()` function with identical chunk-splitting and marker-finding logic |
| `ReviewMode` enum | `ReviewMode` string enum with NORMAL/REINFORCEMENT/MASTERED |
| `StudyActivityType` / `StudyActivityRecord` | Same-named classes in ArkTS |
| Tag selection with `Set<String>` | `Set<string>` in ArkTS with `toggleTag()` mutation |
| Shuffle for random review | `Array.sort(() => Math.random() - 0.5)` |
| SF Symbols icons | Text characters (★, ✓, ▲, ▼, ←, →) as placeholder icons |
| `UNUserNotificationCenter` | Deferred — no notification implementation in first pass |

## Known Gaps

1. **No persistent storage** — All state (presets, knowledge points, activities) lives in memory and resets on app restart. Needs `@ohos.data.preferences` or relational store integration.

2. **No Markdown import** — The built-in preset is the only data source. File picker and import flow needed for loading external `.md` files.

3. **No LaTeX rendering** — Math formulas (e.g., `lim f(x) = A`, `f'(c) = 0`) render as plain text. The source uses `SwiftMath`; HarmonyOS would need a custom Canvas-based or WebView-based renderer.

4. **No dark mode** — Only light theme colors are currently applied. HarmonyOS supports `darkColorMode` resource qualifiers.

5. **No adaptive tablet layout** — Single-column layout only. The source has sophisticated two-column layouts for wide screens.

6. **No notifications or widgets** — Study reminders and home screen widgets are not implemented.

7. **No onboarding / profile** — Profile setup and onboarding flows are absent.

8. **Simplified reinforcement** — The source tracks `reinforcementCount` (adding/clearing multiple times). Currently simplified to boolean toggle.

9. **No countdown / exam tracking** — The source has countdown dates, daily goals with progress warnings.

10. **No data export** — Cannot export knowledge points back to Markdown or other formats.

11. **Icon assets** — No app icon or media resources; using text-based placeholders.

## Recommended Next Migration Tasks

1. **Add persistent storage** — Implement `@ohos.data.preferences` for saving `AppState` (presets, points, activities) as serialized JSON. This is the highest-priority gap.

2. **Add Markdown import** — Implement file picker using `@ohos.file.picker` and integrate the existing `parseMarkdown()` function.

3. **Add dark mode support** — Use HarmonyOS system resource qualifiers (`base/element` vs `dark/element`) or programmatic color switching based on `ColorMode`.

4. **Implement LaTeX math rendering** — Evaluate options: custom `Canvas` renderer for simple math, or a lightweight math typesetting library if available.

5. **Add tablet responsive layout** — Use `BreakpointSystem` or manual width detection to switch between single-column and two-column layouts.

6. **Add notification reminders** — Implement study progress notifications using `@ohos.notificationManager`.

7. **Restore reinforcement count tracking** — Track multiple add/clear cycles instead of boolean toggle.

## Safety Verification

- ✅ All writes stayed inside `Kikaria-HarmonyOS`
- ✅ No writes to Outposts repository root
- ✅ No modification of `FORGIS_CONFIG.yml`
- ✅ No modification of the task file
- ✅ No modification of workflow files
- ✅ No access to secrets
- ✅ No writes to source repository
- ✅ First HarmonyOS project skeleton was produced
