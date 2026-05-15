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
