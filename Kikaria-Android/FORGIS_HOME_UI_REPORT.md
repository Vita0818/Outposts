# FORGIS Home UI Report — Kikaria Android

## iOS Source Files Inspected

| File | Key Insights |
|---|---|
| `Kikaria/ContentView.swift` | Full home screen implementation: compact, pad portrait, landscape layouts. StartReviewButton with orbit animation, TodayOverviewHomeProgressButton, HomeDashboardGridCard, ProfileAvatarView, KikariaTheme colors, LiquidGlassCard modifiers |
| `Kikaria/KikariaAdaptiveLayout.swift` | Adaptive layout system: width categories, scale factors, padding logic for phones and iPads |
| `Kikaria/KikariaTypography.swift` | Typography system: `appTitle` uses serif design, `chineseBody`, `chineseCaption`, `numericText` with monospaced digits, mixed Chinese/serif text rendering |
| `Kikaria/KikariaApp.swift` | App entry: WindowGroup → ContentView |
| `SPEC.md` | Original v0.1 spec for context (app has grown significantly) |

## Android Files Inspected

| File | Purpose |
|---|---|
| `ui/home/HomeScreen.kt` | Current home screen (pre-change) |
| `ui/theme/KikariaColors.kt` | Color palette — light/dark, gradients |
| `ui/theme/KikariaTheme.kt` | Material3 theme wrapping |
| `ui/components/GlassComponents.kt` | Glass card/capsule/circle components |
| `viewmodel/KikariaViewModel.kt` | Central state: presets, review queue, progress |
| `ui/navigation/KikariaNavGraph.kt` | Navigation routes |
| `MainActivity.kt` | App entry with edge-to-edge |

## Visual Gaps Identified

1. **Background**: Solid white `GlassSurface` instead of `pageGradient` (blue-toned gradient)
2. **Header**: Material3 TopAppBar with just "Kikaria" text; missing profile avatar and proper typography
3. **Start Button**: Simplified breathing circle with no orbiting decorative bubbles, no glass material overlay, text "开始" instead of arrow icon, wrong gradient
4. **Progress Card**: Three vertical stat items (今日复习, 今日掌握, 每日目标) instead of iOS horizontal layout: date + "Days Left" + progress ratio + chevron
5. **Dashboard**: Four small 2×2 Material3 Cards instead of unified glass card with three metric columns (范围, 重点集锦, 已掌握) + preset row
6. **Glass Styling**: Cards used `Card` with `Mist` color instead of liquid glass with shadow, proper opacity, and gradient borders
7. **Typography**: System default instead of serif-like styling for "Kikaria" title
8. **Dark Mode**: Colors hardcoded to light variants in some places; GlassComponents didn't adapt to dark mode

## Android Files Changed

| File | Change Summary |
|---|---|
| `ui/home/HomeScreen.kt` | **Full rewrite.** Replaced Scaffold/TopAppBar with gradient-background Box. Added proper header row with Kikaria title + avatar placeholder. Rewrote StartReviewButton with orbit animation, 4 decorative bubbles, glass overlay, arrow icon. Rewrote progress card to iOS TodayOverviewHomeProgressButton layout (date + days left + progress + chevron). Rewrote dashboard to iOS HomeDashboardGridCard layout (unified glass card with 3 metric columns + preset row). All components now adapt to dark mode via `isSystemInDarkTheme()`. |
| `ui/components/GlassComponents.kt` | Updated GlassCard, GlassCapsule, GlassCircle, InfoCard to use `isSystemInDarkTheme()` for adaptive light/dark glass surface colors and shadow colors. |
| `MainActivity.kt` | Changed Surface background to `Color.Transparent` so that each screen's own background (e.g., home screen gradient) shows through without a solid color overlay. |

## Design Decisions Made

1. **Gradient background uses Compose `Brush.linearGradient`** — matches iOS `KikariaTheme.pageGradient` with adaptive light/dark colors
2. **Orbit animation uses `rememberInfiniteTransition` with rotate** — translates iOS `TimelineView(.animation)` orbit effect into Compose-native animation
3. **Decorative bubbles use `SoftBubble` equivalent** — radial gradient overlay + linear gradient fill + shadow matching iOS bubble style
4. **Glass cards use `shadow()` + `clip()` + `background(glassSurface.copy(alpha=...))`** — approximates iOS `.background(.ultraThinMaterial)` since Compose doesn't have native blur materials
5. **All color choices use explicit `isDark` branching** — mirrors iOS `adaptive(light:, dark:)` pattern
6. **"Kikaria" title uses `FontWeight.SemiBold` at 39sp** — matches iOS `appTitle(size: 39, weight: .semibold)`
7. **Avatar shows initial "V" letter** — placeholder for future profile image support
8. **Three dashboard columns are tappable independently** — navigates to Scope, Reinforcement, Mastered matching iOS behavior
9. **Preset row is tappable** — ready for preset selection navigation (marked TODO)
10. **Progress card is tappable** — ready for today overview navigation (marked TODO)

## Remaining Visual Gaps

1. **No native blur/glass material** — Compose on Android lacks `.ultraThinMaterial` equivalent; current approximation uses semi-transparent fills + shadows. A `RenderEffect` blur could be explored on API 31+.
2. **Avatar is a placeholder initial** — iOS uses SF Symbol `person.crop.circle.fill` with optional photo; Android needs a proper avatar composable
3. **Today overview route not wired** — progress card is tappable but navigation target not implemented
4. **Preset selection route not wired** — preset row is tappable but navigation target not implemented
5. **Countdown data not connected** — "Days Left" shows "--" placeholder; ViewModel needs `countdownEndDate` exposure
6. **Typography system not extracted** — iOS has `KikariaTypography` with mixed Chinese/serif rendering; Android currently uses plain `Text` with `FontWeight`
7. **Adaptive layout for tablets** — iOS has sophisticated pad portrait/landscape layouts; Android targets phone-only for now
8. **Bubble animation fidelity** — iOS uses `TimelineView(.animation)` with continuous orbit; Compose uses `rememberInfiniteTransition` which restarts rather than truly continuous

## Suggested Next UI Pass

1. Implement Today Overview screen and wire navigation from progress card
2. Implement Preset Selection screen and wire navigation from preset row
3. Build proper `ProfileAvatar` composable with system icon fallback
4. Extract `KikariaTypography` object with named text styles
5. Add `RenderEffect`-based blur for API 31+ glass surfaces
6. Connect countdown data to ViewModel and expose via state
7. Add tablet-adaptive layout using `WindowSizeClass`

---

## Pass 2 — Visual Fidelity Refinement (2026-05)

### Source Files Inspected

| File | Key Insights |
|---|---|
| `Kikaria/ContentView.swift` (lines 6500–7300) | StartReviewButton with `SoftBubble`/`DecorativeBubble`, `TimelineView(.animation)` orbit, center circle with `actionGradient` + `.ultraThinMaterial` + gradient stroke + radial overlay. `TodayOverviewHomeProgressButton` horizontal layout. `HomeDashboardGridCard` with `liquidGlassCard()` modifier, `HomeDashboardMetricColumn`, `HomeDashboardDivider`. |
| `Kikaria/ContentView.swift` (lines 1–700) | `LiquidGlassCardModifier`: 3-layer glass look — glassSurface fill → `.ultraThinMaterial` → gradient stroke (white→white faint→glassStrokeAccent) → soft white highlight line → shadow. `LiquidGlassCircleModifier` with similar layered treatment. |
| `Kikaria/KikariaTypography.swift` | `appTitle(size:weight:)` uses `.system(size:weight:design:.serif)` — serif design is integral to Kikaria's brand identity. Latin/English text uses serif, Chinese uses system. |
| `Kikaria/KikariaAdaptiveLayout.swift` | Scale factors, padding, iPad support (noted but not targeted this pass). |

### Android Files Inspected

| File | Status |
|---|---|
| `ui/home/HomeScreen.kt` | Pre-refinement home screen: structurally close to iOS but missing key visual treatment |
| `ui/components/GlassComponents.kt` | Glass card/capsule circle — missing gradient stroke overlay |
| `ui/theme/KikariaColors.kt` | Color palette already well-aligned with iOS |
| `viewmodel/KikariaViewModel.kt` | Data feeding — no changes needed this pass |

### Visual Gaps Identified (Pass 2)

1. **Serif typography missing**: iOS `appTitle` uses `.design: .serif`; Android used default sans-serif for "Kikaria" title and all Latin text. This is the strongest brand-identity gap.
2. **Glass card gradient stroke missing**: iOS `LiquidGlassCardModifier` has a sophisticated gradient border (white → white(0.10) → cyan accent) plus a soft white highlight. Android cards had only flat `glassSurface` fill + shadow. This made cards feel flat and non-premium.
3. **Start button center circle no stroke**: iOS center circle has a gradient border stroke matching `LiquidGlassCircleModifier`. Android circle lacked this.
4. **Decorative bubbles no stroke**: iOS `SoftBubble` has gradient stroke + `.ultraThinMaterial` background. Android bubbles had only radial highlight + linear fill.
5. **Shadow depth slightly off**: iOS uses `shadowOpacity: 0.12, shadowRadius: 18, shadowY: 10` for cards; Android was close but shadow opacity was inconsistent.
6. **Number typography**: iOS value numbers use serif design (via `KikariaTypography.numericText`). Android used system sans-serif.
7. **Spacing rhythm**: Minor vertical spacing gaps slightly wider than iOS's tighter rhythm.

### Refinement Strategy

**Philosophy**: Fewer, higher-impact changes. Focus on the "atmosphere" rather than adding elements.

**Changes made**:

1. **Serif font for Latin text** — Added `fontFamily = FontFamily.Serif` to "Kikaria" title, date text, progress numbers, and dashboard metric values. This brings the iOS serif identity to Android.

2. **Glass card gradient stroke overlay** — Created `Modifier.liquidGlassStroke()` using `drawBehind` with a `Brush.linearGradient` (white → white faint → glassStrokeAccent) in a `Stroke` style. Applied to `TodayProgressCard`, `DashboardCard`, and `GlassCard` in GlassComponents. This is the single biggest visual upgrade — cards now have the premium glass border that defines Kikaria's look.

3. **Start button circle stroke** — Created `Modifier.liquidGlassCircleStroke()` matching iOS `LiquidGlassCircleModifier` (white → white faint → sky accent). Applied to center action circle.

4. **Decorative bubble strokes** — Applied `liquidGlassCircleStroke` to each orbiting bubble, matching iOS `SoftBubble` treatment.

5. **Shadow tuning** — Tightened shadow values on cards to `shadowOpacity: 0.12, shadowRadius: 18.dp` matching iOS more closely. Corner radius on progress card aligned to 28.dp.

6. **Tighter vertical spacing** — Reduced spacers between header/start-button/from 28→24.dp to match iOS's more compact rhythm.

### Files Changed

| File | Change Summary |
|---|---|
| `ui/home/HomeScreen.kt` | Added `FontFamily.Serif` to title, date, progress, and metric values. Added `liquidGlassStroke()` and `liquidGlassCircleStroke()` helper modifiers using `drawBehind` with gradient strokes. Applied strokes to TodayProgressCard, DashboardCard, start button center circle, and all four decorative bubbles. Tuned shadow opacities and corner radii. Tightened vertical spacing. |
| `ui/components/GlassComponents.kt` | Added `glassCardStroke()` private modifier using `drawBehind`. Applied gradient stroke to `GlassCard` composable so all GlassCard usages get the liquid glass border. |

### Remaining Visual Gaps (After Pass 2)

1. **No native blur material** — Compose lacks `RenderEffect`-based blur (API 31+). Current semi-transparent fill approximates `.ultraThinMaterial` but doesn't blur background content.
2. **Avatar still placeholder** — Shows "V" initial; needs proper icon or photo composable.
3. **Countdown "Days Left" still placeholder** — Shows "-- Days Left"; needs ViewModel to expose countdown data.
4. **Orbit animation not truly continuous** — `rememberInfiniteTransition` restarts every 150s rather than being driven by wall-clock time like iOS `TimelineView(.animation)`.
5. **Typography system not extracted** — No `KikariaTypography` object yet; serif choices are inline.
6. **Navigation targets not wired** — Today overview and preset selection routes are TODO placeholders.
7. **Tablet adaptive layout** — iOS has comprehensive iPad portrait/landscape support; Android targets phone only.
8. **Review/Mastered/Reinforcement screens** — Not yet visually refined to match iOS glass aesthetic.

### Recommended Next Pass

1. Extract `KikariaTypography` object with named styles for consistency
2. Add `RenderEffect` blur for glass cards on API 31+ devices
3. Wire countdown data from ViewModel to home screen
4. Refine Review screen to use glass card styling consistently
5. Build proper `ProfileAvatar` composable
