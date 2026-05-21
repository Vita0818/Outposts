# Kikaria Android Deep UI and Component Functionality Alignment Pass

This is a focused UI and component functionality alignment pass for the existing Android/Kotlin/Jetpack Compose version of Kikaria.

This is not a fresh migration.

The Android project already exists under `Kikaria-Android`. The current version has a rough shape, but its typography, icons, component placement, component identity, interaction details, and some component-level behaviors still do not match the original Kikaria source app closely enough.

Your task is to inspect the original Kikaria source app and the current Android implementation, then repair the Android implementation so that it becomes much closer to the source app in both visual structure and component behavior.

Write all changes under:

`Kikaria-Android`

Do not modify files outside `Kikaria-Android`.

Do not modify the source repository.

Do not modify unrelated files in the target repository.

## Core objective

Align the Android implementation with the original Kikaria app at the component and interaction level.

Do not merely make the Android version “look acceptable”.

The goal is source-informed parity:

1. Same product structure
2. Same screen hierarchy
3. Same title placement logic
4. Same typography intent
5. Same icon semantics
6. Same component grouping
7. Same card/button identity
8. Same interaction behavior where feasible
9. Same reusable component strategy
10. Same minimal visual language

Do not redesign Kikaria into a generic Material 3 app.

Do not create a visually unrelated Android interpretation.

## Required source inspection

Before making meaningful edits, inspect the original Kikaria source files and the current Android files.

You must inspect source-side files related to:

1. Home screen
2. Study/review screen
3. Preset switching / preset management
4. Knowledge item display and editing if present
5. Important collection / key collection
6. Mastered list
7. Settings/profile page
8. Shared typography
9. Shared buttons
10. Shared cards
11. Shared icon usage
12. Navigation/page shell
13. Gesture or state logic related to review flow
14. Any source-level model/state definitions

Do not guess the source design from memory.

Do not infer UI from generic Android conventions.

Read source files before changing corresponding Android files.

## Current problems to fix

The current Android version has at least these problems:

1. Fonts are not aligned with Kikaria.
2. Chinese, English, and numeric text do not have carefully controlled typography.
3. Icons are not aligned with source semantics or source visual style.
4. Some icons are wrapped in unnecessary circles or visually separated incorrectly.
5. Component positions do not match the source layout.
6. Page titles are not consistently positioned.
7. Page margins and vertical spacing are inconsistent.
8. Some components look like default Material samples.
9. Shared component reuse is insufficient.
10. Some screen-level components are duplicated instead of being centralized.
11. Some interaction behavior may be functionally present but visually or structurally wrong.
12. The Android implementation may preserve rough feature names but not component identity.

Fix these issues directly.

## Typography alignment requirements

Create, repair, or strengthen a centralized Kikaria Android typography system.

Do not scatter font choices across screens.

Do not use default Material typography blindly.

Typography must be handled through shared tokens and reusable text components where appropriate.

The typography system should define and consistently use tokens for at least:

1. App title
2. Page title
3. Section title
4. Card title
5. Body text
6. Caption text
7. Button text
8. Large metric / display number
9. Knowledge item title
10. Review answer text
11. Settings row text

Important typography rules:

1. Preserve Kikaria's refined, calm, study-oriented typography direction.
2. If the source app uses serif-like typography, preserve that intent on Android.
3. If exact iOS fonts are unavailable, choose Android-safe approximations.
4. Chinese text, English text, and numbers must not look randomly mixed.
5. Do not hard-code the visible username “Vita”.
6. Do not hard-code personal names, avatars, local paths, secrets, or private data.
7. Avoid one-off font sizes inside individual screens.
8. If Android cannot exactly reproduce the source font, centralize the approximation and document it briefly in code comments or TODOs.

Prefer files such as:

- `ui/theme/KikariaTypography.kt`
- `ui/theme/KikariaTheme.kt`
- shared text components if needed

but adapt to the current Android project structure instead of creating parallel duplicate systems.

## Icon alignment requirements

Create, repair, or strengthen a centralized icon mapping layer.

Do not scatter arbitrary Material icons directly across screens when they represent stable Kikaria product actions.

Inspect the source app's icon semantics and map them intentionally to Android equivalents.

For each important source icon/action, define a stable Android mapping.

Important icon rules:

1. Do not wrap every icon in a circle unless the source does that.
2. Do not add a second circular background around an icon already inside a circular button.
3. Top-right icons that are visually grouped in the source should stay visually grouped.
4. Equivalent icon buttons must share size, padding, icon size, and visual treatment.
5. Use semantic mappings, not random approximate icons.
6. Keep icon choices consistent across screens.
7. If exact SF Symbols do not exist on Android, choose the closest semantic and visual equivalent.

Prefer a shared file such as:

- `ui/components/KikariaIcons.kt`
- or an existing equivalent shared component file

## Component identity requirements

Repair the Android code so repeated visual elements are represented by shared components.

Do not duplicate similar UI on each screen.

Centralize at least these component types if present:

1. Page shell / page container
2. Page title
3. Top action area
4. Circular icon button
5. Soft/glass-like button
6. Card container
7. Metric bubble/card
8. Settings row
9. Preset row/card
10. Knowledge item row/card
11. Review action button
12. Empty state
13. Section header
14. Profile/avatar display

If a component appears visually similar across screens, it should probably be the same shared component.

The goal is source-level component consistency, not screenshot-level approximation.

## Layout and placement requirements

Fix component positions by reading the source layout.

Pay special attention to:

1. Page title top offset
2. Page title left offset
3. Page horizontal margins
4. Top-right action icon placement
5. Top bar vertical alignment
6. Home screen central composition
7. Start/review primary action placement
8. Card height and internal padding
9. Review screen title/hint/answer vertical rhythm
10. Settings page title and row spacing
11. Preset page/list spacing
12. Collection/mastered list spacing
13. Bottom controls or floating controls
14. Safe-area-like padding behavior

Do not simply use default Material `TopAppBar` if the source layout is custom.

If the source app has custom title placement, reproduce it with custom Compose layout.

## Home screen alignment

Inspect the original Kikaria home screen carefully.

Repair the Android home screen to better match the source in:

1. App title placement
2. Profile/avatar area
3. Central bubble or central visual system
4. Primary start action
5. Daily goal display
6. Countdown-day display
7. Preset display
8. Bubble/card size and spacing
9. Icon and text treatment
10. Overall visual rhythm

Do not turn the home screen into a generic dashboard.

## Review / study screen alignment

Inspect the original review/study source implementation carefully.

Repair the Android review/study screen to better match the source in:

1. Knowledge point title placement
2. Hint reveal behavior
3. Answer reveal behavior
4. Answer typography and spacing
5. Important collection state
6. Mastered state
7. Button positions
8. Gesture-related behavior if implemented
9. State transitions
10. Long answer handling

Do not add unnecessary instructional labels if the source does not have them.

Do not make the review screen look like a generic flashcard demo.

## Preset and knowledge management alignment

Inspect source preset/knowledge management screens if present.

Repair Android implementation to better match:

1. Preset switching layout
2. Preset row/card style
3. Knowledge item row/card style
4. Tags display
5. Add/edit/delete entry points
6. Empty states
7. Title and margin system
8. Shared row components

Do not invent new management flows unless the source has them.

## Important collection and mastered list alignment

Inspect the source implementation for important collection and mastered list behavior if present.

Repair Android implementation so that:

1. Important collection is visually and behaviorally distinct from mastered list if the source distinguishes them.
2. Add/remove state is reflected consistently.
3. Row/card components are shared where appropriate.
4. The same typography and icon tokens are used.
5. Empty states follow the app style.

## Settings/profile alignment

Inspect the original Kikaria settings/profile source implementation.

Repair Android settings/profile UI to better match:

1. Title position
2. Avatar/profile layout
3. Row spacing
4. Section spacing
5. Icon treatment
6. Typography hierarchy
7. Daily goal / countdown / preset settings entry points if present
8. Minimal visual style

Do not hard-code “Vita” as the visible user.

Use neutral placeholder text or source-appropriate default data if necessary.

## Interaction functionality alignment

This pass is not only visual.

Also align component-level behavior where feasible.

Inspect source state and interaction logic, then repair Android behavior for:

1. Daily goal state
2. Current preset state
3. Review answer/hint reveal state
4. Important collection add/remove state
5. Mastered add/remove state
6. Preset switching
7. Previous/next/random study item behavior if present
8. Settings row interactions
9. Profile/avatar placeholder behavior if present
10. State persistence placeholders or TODOs

If full persistence is too large for this pass, create a clean shared state/repository boundary and leave explicit TODOs.

Do not fake completed persistence.

Do not claim unavailable features are complete.

## Architecture constraints

Keep the Android project maintainable.

Prefer:

1. Shared theme layer
2. Shared component layer
3. Shared model layer
4. Shared state holder or repository layer
5. Screen files that consume shared components
6. Minimal screen-specific styling

Avoid:

1. Massive single-file UI
2. Per-screen duplicated card/button/title implementations
3. Random one-off hard-coded padding
4. Random one-off icon choices
5. Random one-off font sizes
6. Unrelated refactors
7. Replacing the existing project with a new app

## What not to do

Do not:

1. Modify files outside `Kikaria-Android`.
2. Modify the source repo.
3. Access secrets, API keys, local private paths, or personal files.
4. Add real user-private data.
5. Add build/test commands to `FORGIS_CONFIG.yml`.
6. Claim build success unless a real build actually succeeds.
7. Create fake screenshots.
8. Create fake test results.
9. Replace Kikaria with a generic Material 3 app.
10. Start a new migration from scratch.
11. Rewrite the entire Android project unless absolutely necessary and still only inside `Kikaria-Android`.
12. Add unrelated features.
13. Add arbitrary shell usage.
14. Hard-code “Vita” as a visible user.

## Required workflow

Follow this workflow:

1. Inspect current Android files under `Kikaria-Android`.
2. Inspect relevant original Kikaria source files.
3. Identify concrete mismatches in typography, icons, layout, components, and behavior.
4. Patch shared theme/tokens first.
5. Patch shared components second.
6. Patch screens third.
7. Patch state/model/repository behavior only where needed for component functionality.
8. Use `git_status` and `git_diff` before final summary.
9. Final summary must be specific and file-grounded.

## Final summary requirements

The final summary must include:

1. Source Kikaria files inspected
2. Android target files modified
3. Typography alignment changes
4. Icon alignment changes
5. Component placement changes
6. Shared components created or repaired
7. Component functionality changes
8. Screens improved
9. Remaining mismatches
10. Deferred work
11. Whether build/test was run
12. Confirmation that all changes stayed inside `Kikaria-Android`
13. Confirmation that the source repo was not modified

Be honest about incomplete areas.
