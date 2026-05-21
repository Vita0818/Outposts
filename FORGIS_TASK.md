# Kikaria Android UI Fidelity Repair Pass

This is not a new migration run.

The first Android/Kotlin/Jetpack Compose version already exists under `Kikaria-Android`. It has the rough product structure, but its typography, icons, component placement, spacing, and visual component hierarchy do not yet match the original Kikaria source app closely enough.

Your task is to perform a focused UI fidelity repair pass.

Write all changes under `Kikaria-Android` only. Do not modify files outside that target subdirectory.

## Core rule

Do not redesign the app.

Do not create a new architecture.

Do not replace the current Android app with a new generic Material sample.

Do not add unrelated features.

Do not invent new screens.

Do not rename product concepts.

Only inspect the current Android implementation, inspect the original Kikaria source implementation, then patch the existing Android code so that the visible UI better matches the source app.

## Required inspection before editing

Before editing any target UI file, read the relevant source Kikaria files.

You must inspect source files for:

1. Typography and font handling
2. Title positions
3. Page horizontal margins
4. Top bar structure
5. Icon choices
6. Button sizes
7. Floating/circular button styling
8. Home screen component layout
9. Review/study screen component layout
10. Settings/profile page layout
11. Preset/knowledge management page layout if present
12. Shared UI components and visual tokens

Do not rely on memory or generic Android conventions.

## Highest-priority problems to fix

The current Android version has these issues:

1. Fonts are wrong.
2. Icons are wrong or visually inconsistent.
3. Component positions are wrong.
4. Page titles do not match the original source layout.
5. Spacing and margins do not match the original source layout.
6. Some components look like default Material components instead of Kikaria components.
7. Shared UI style is not centralized enough.

Fix these issues first.

## Typography requirements

Create or repair a centralized typography layer for Kikaria Android.

The typography system must not scatter font choices across screens.

Inspect the original Kikaria source typography and reproduce its intent as closely as Android allows.

Important:

1. Do not use default Material typography blindly.
2. Do not use random font sizes per screen.
3. Do not hard-code user names.
4. Do not hard-code Vita-specific text.
5. If exact iOS fonts cannot be used on Android, choose a stable Android approximation and document the limitation clearly in comments or TODOs.
6. Chinese, English, and numbers should not be visually chaotic.
7. If the source app uses serif-like typography, preserve that visual direction.
8. Create named typography tokens for title, page title, card title, body, caption, button, metric, and large display text where needed.

Prefer a structure such as:

- `ui/theme/KikariaTypography.kt`
- `ui/theme/KikariaTheme.kt`

but adapt to the existing Android project structure if it already has a theme layer.

## Icon requirements

Create or repair a centralized icon layer.

Do not scatter arbitrary Material icons directly across screens if those icons are part of the Kikaria visual identity.

Inspect the original Kikaria source for system image names, icon meanings, button roles, and placement.

For each important icon, map it deliberately to an Android/Compose equivalent.

If an exact SF Symbol or source icon is unavailable on Android, choose the closest semantic and visual equivalent, and keep the mapping centralized.

Prefer a structure such as:

- `ui/components/KikariaIcons.kt`
- or a similar existing shared component file

Important:

1. Do not wrap every icon in its own visible circle unless the source UI does that.
2. Do not add duplicate circular backgrounds around icons that are already inside a circular or glass button.
3. Keep icon size consistent across equivalent buttons.
4. Top-right icons that are visually grouped in the source should remain visually grouped in Android.
5. Circular buttons should use a shared component, not one-off implementations.

## Component placement requirements

Fix component positions by reading the source layout, not by guessing.

Pay special attention to:

1. Page title top offset
2. Page title left/right alignment
3. Top-right action icon positions
4. Home screen central visual layout
5. Card vertical spacing
6. Card horizontal margins
7. Review screen answer/hint layout
8. Bottom or floating controls
9. Settings page title position and row spacing
10. Preset/list page title and row/card layout

Do not simply use default Material `TopAppBar` if the source app has a custom layout.

If the original app uses custom title placement, reproduce it with custom Compose layout.

## Shared component requirements

Do not duplicate similar-looking components per screen.

Repair the Android implementation so that repeated visual elements use shared components.

At minimum, centralize these if they exist:

1. Page shell / page container
2. Page title
3. Circular icon button
4. Glass-like or soft button
5. Card container
6. Settings row
7. Preset/knowledge row
8. Empty or placeholder state
9. Home metric bubble/card
10. Review action button

The goal is source-level consistency, not approximate visual similarity.

## Home screen requirements

Inspect the original Kikaria home screen.

Repair Android home screen layout to better match:

1. Product title placement
2. Profile/avatar position
3. Central visual element or bubble system
4. Main start action
5. Daily goal / countdown / preset metric components if present
6. Spacing and visual hierarchy

Do not turn the home screen into a generic Android dashboard.

## Study/review screen requirements

Inspect the original Kikaria review/study screen.

Repair Android review screen layout to better match:

1. Knowledge point title placement
2. Hint/content reveal hierarchy
3. Action button placement
4. Important collection / mastered state presentation
5. Gesture-related UI hints only if present in source
6. Spacing for long answers

Do not add unnecessary instructional labels.

## Settings/profile requirements

Inspect the original Kikaria settings/profile source.

Repair Android settings/profile page layout to better match:

1. Title size and position
2. Avatar/profile area
3. Row spacing
4. Section spacing
5. Typography hierarchy
6. Icon treatment

If there is an existing Kikaria iOS layout style in the source, follow that rather than Android Material defaults.

## What not to do

Do not:

1. Rewrite the whole Android project.
2. Delete the existing Android foundation unless it is clearly wrong and replacement stays inside `Kikaria-Android`.
3. Add build/test commands to `FORGIS_CONFIG.yml`.
4. Claim build success unless an actual build was run successfully.
5. Move changes outside `Kikaria-Android`.
6. Add secrets, API keys, local paths, or user-private data.
7. Replace Kikaria with a generic Material 3 sample app.
8. Introduce unrelated features.
9. Add fake screenshots or fake test results.
10. Hard-code “Vita” as a visible username.

## Required implementation approach

1. Inspect the current target Android files under `Kikaria-Android`.
2. Inspect the original source Kikaria UI files.
3. Identify the main UI differences.
4. Patch shared theme/components first.
5. Then patch screens to consume shared components.
6. Keep changes focused and minimal.
7. Use `git_diff` before final summary.

## Final summary requirements

The final summary must clearly report:

1. Which source Kikaria files were inspected.
2. Which Android files were modified.
3. What typography changes were made.
4. What icon changes were made.
5. What layout/position changes were made.
6. Which shared components were introduced or repaired.
7. Which screens are now closer to source.
8. Which visual differences remain deferred.
9. Whether build/test was run.
10. Confirmation that all changes stayed inside `Kikaria-Android`.
