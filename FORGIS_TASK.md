# Kikaria Android Full Fidelity Reconstruction Pass

This is a large, exhaustive, source-informed reconstruction pass for the existing Kikaria Android project.

The Android project already exists under:

`Kikaria-Android`

However, the current Android version is still only a rough approximation. It has the general shape, but it does not yet faithfully reproduce the original Kikaria app's visual system, typography, icon system, component placement, component identity, screen structure, and interaction behavior.

Your task is to perform a deep full-app fidelity reconstruction pass.

This is not a small polish pass.

This is not a new product design.

This is not a generic Android Material rewrite.

This is a source-informed Android reconstruction of Kikaria.

## Absolute write boundary

All generated or modified target files must stay under:

`Kikaria-Android`

Do not modify any file outside `Kikaria-Android`.

Do not modify the source repository.

Do not modify unrelated target repository files.

Do not add secrets, API keys, local absolute paths, personal data, or hard-coded user identity.

Do not hard-code the visible username “Vita”.

## Core mission

Reconstruct the Android implementation so that it matches the original Kikaria source app as closely as feasible in Kotlin and Jetpack Compose.

You must align:

1. Product information architecture
2. Screen hierarchy
3. Navigation structure
4. Visual hierarchy
5. Typography system
6. Mixed text treatment
7. Icon semantics
8. Button identity
9. Card identity
10. Bubble/metric component identity
11. Page title placement
12. Top action placement
13. Horizontal margins
14. Vertical spacing
15. Review/study interaction behavior
16. Preset state behavior
17. Important collection behavior
18. Mastered list behavior
19. Settings/profile structure
20. Shared component architecture

The final Android result should feel like Kikaria, not like a Material 3 demo.

## Do not finish early

Do not stop after fixing only one or two visible issues.

Do not produce a final summary after a shallow typography-only or icon-only patch.

Continue inspecting, comparing, and patching until you have addressed all major screens and shared UI systems that exist in the current Android project and source app.

Before final summary, you must have inspected and acted on all applicable areas:

1. Theme
2. Typography
3. Icons
4. Shared components
5. Home screen
6. Review/study screen
7. Preset switching / preset management
8. Knowledge item representation
9. Important collection
10. Mastered list
11. Settings/profile
12. Navigation/page shell
13. State/model/repository layer
14. Placeholder/sample data boundaries
15. Current target Android file organization

If some area cannot be completed, explicitly mark it as deferred with a reason. Do not silently skip it.

## Required source inspection strategy

You must begin by inspecting the current Android project under `Kikaria-Android`.

Then inspect the source Kikaria repository.

Search and read source files related to:

1. App entry point
2. Navigation
3. Home page
4. Study/review page
5. Preset selection
6. Knowledge item model
7. Knowledge item editing or management
8. Important collection
9. Mastered list
10. Settings
11. Profile/avatar
12. Daily goal
13. Countdown day
14. Typography
15. Font usage
16. Buttons
17. Cards
18. Bubbles
19. Icons / SF Symbols / image assets
20. Gestures
21. Animations
22. Persistence/state

Do not guess from memory.

Do not rely on generic Android conventions.

Do not rely only on the current Android implementation.

The source app is the authority.

## Reconstruction strategy

Use this order:

1. Inspect current Android implementation.
2. Inspect source Kikaria implementation.
3. Build a concrete mismatch list.
4. Repair or rebuild shared theme/tokens.
5. Repair or rebuild typography system.
6. Repair or rebuild icon mapping system.
7. Repair or rebuild shared page shell.
8. Repair or rebuild shared buttons/cards/rows/bubbles.
9. Repair Home screen.
10. Repair Review/Study screen.
11. Repair Preset and knowledge management screens.
12. Repair Important Collection and Mastered List screens.
13. Repair Settings/Profile screens.
14. Repair state/model/repository boundaries needed for component behavior.
15. Remove or reduce generic Material demo styling.
16. Run final git status and git diff review.
17. Produce a grounded final summary.

Do not invert this order by randomly editing screens first and leaving shared systems inconsistent.

## Typography system requirements

Create or repair a centralized Android typography system for Kikaria.

Do not scatter font sizes or font families across screens.

Do not use default Material typography blindly.

The typography system must include stable tokens for:

1. App title
2. Page title
3. Large display text
4. Metric number
5. Card title
6. Knowledge title
7. Review prompt
8. Review answer
9. Body text
10. Secondary body text
11. Caption
12. Button text
13. Settings row title
14. Settings row subtitle
15. Tag text

Preserve Kikaria's calm, refined, study-oriented style.

If the source app uses serif-like typography, preserve that direction.

If exact iOS fonts are unavailable on Android, choose a stable Android approximation and centralize that approximation in the theme layer.

Chinese, English, and numbers must not look randomly mixed.

If exact mixed-script rendering is too large to fully implement, create a clean text component boundary and TODO for exact mixed-script run splitting. Do not hard-code ad hoc per-screen font hacks.

## Icon system requirements

Create or repair a centralized Kikaria icon mapping layer.

Do not scatter arbitrary Material icons across screens.

For every important action icon, inspect the source app's icon usage and map it deliberately to Android.

Important actions include, where present:

1. Start review
2. Settings
3. Profile/avatar
4. Add
5. Edit
6. Delete
7. Back
8. Close
9. Confirm
10. Important collection
11. Mastered
12. Preset switching
13. Daily goal
14. Countdown day
15. Previous / next
16. Show hint
17. Show answer

Rules:

1. Do not wrap every icon in a circle.
2. Do not add duplicate circular backgrounds.
3. Do not visually separate icons that are grouped in the source.
4. Equivalent icon buttons must share size, padding, icon size, and visual treatment.
5. If exact SF Symbols are unavailable, choose the closest semantic and visual equivalent.
6. Keep mappings centralized.

## Shared component reconstruction

Rebuild or repair shared Compose components so repeated UI is not duplicated.

At minimum, centralize or repair these components if applicable:

1. Kikaria page shell
2. Kikaria page title
3. Top action row
4. Circular icon button
5. Soft/glass-like button
6. Primary start action
7. Card container
8. Metric bubble/card
9. Home bubble
10. Settings row
11. Preset row/card
12. Knowledge item row/card
13. Review card
14. Review action button
15. Tag chip
16. Empty state
17. Profile/avatar block
18. Section header

A screen should consume shared components rather than duplicating local styling.

If the current Android project has one-off components that should be shared, refactor them into shared components inside `Kikaria-Android`.

## Layout fidelity requirements

Repair layout according to the source app.

Pay attention to exact visual structure:

1. Page top padding
2. Page title position
3. Page title font size
4. Page title alignment
5. Page horizontal margins
6. Top-right icon position
7. Top action grouping
8. Card width
9. Card internal padding
10. Card corner radius
11. Card vertical rhythm
12. Button placement
13. Bubble placement
14. Review content vertical rhythm
15. Settings row spacing
16. Preset row spacing
17. Bottom area spacing
18. Gesture-friendly spacing
19. Long-answer layout behavior

Do not use default Android `TopAppBar` if the source app uses custom title placement.

Do not use default Material spacing if the source app has a different rhythm.

## Home screen reconstruction

Inspect the original Kikaria home screen deeply.

Reconstruct the Android Home screen so that it better matches the source in:

1. App title
2. User/avatar location
3. Central visual system
4. Bubble/card system
5. Daily goal component
6. Countdown component
7. Preset component
8. Primary start action
9. Secondary actions
10. Icon sizes
11. Text sizes
12. Spacing
13. Motion/animation placeholders if present
14. Calm minimal visual style

Do not make the Home screen a generic dashboard.

## Review / Study screen reconstruction

Inspect the original review/study flow deeply.

Repair Android so it matches source behavior and structure for:

1. Current knowledge item title
2. Prompt/hint reveal
3. Answer reveal
4. Important collection state
5. Mastered state
6. Previous/next/random navigation
7. Swipe or gesture semantics if present
8. Long answer behavior
9. Button/icon placement
10. Review card style
11. State transition visual hierarchy
12. Minimal instructional text

Do not add unnecessary labels.

Do not make it a generic flashcard screen.

## Preset and knowledge management reconstruction

Inspect the source implementation for preset and knowledge management.

Repair Android structures for:

1. Current preset display
2. Preset switching
3. Preset list row/card
4. Knowledge item row/card
5. Knowledge item tags
6. Add/edit/delete entry points
7. Per-preset state boundaries
8. Empty states
9. Shared list components
10. Screen title and action placement

Do not invent a different management model.

## Important collection and mastered list reconstruction

Inspect source behavior and UI for important collection and mastered list.

Repair Android so that:

1. Important collection and mastered list are separate concepts if source separates them.
2. Their visual treatment follows source.
3. Their row/card components are shared where appropriate.
4. Add/remove state is reflected consistently.
5. Empty states match Kikaria style.
6. Gesture behavior is approximated or clearly deferred.
7. Button state does not allow duplicate-add behavior if the source prevents it.

## Settings/profile reconstruction

Inspect source settings/profile pages.

Repair Android so that settings/profile matches source in:

1. Page title position
2. Profile/avatar structure
3. User display name handling
4. Daily goal setting entry
5. Countdown setting entry
6. Preset management entry
7. Section spacing
8. Settings row style
9. Icon treatment
10. Typography hierarchy

Do not hard-code the visible display name.

Use a placeholder from state/sample repository only if needed.

## State and component behavior requirements

This pass should improve component functionality, not just visuals.

Inspect source state behavior and repair Android state boundaries where feasible.

Important state areas:

1. Current preset
2. Daily goal
3. Countdown day
4. Study item list
5. Current review item
6. Hint visible / answer visible
7. Important collection membership
8. Mastered membership
9. Per-preset important/mastered state if source has it
10. Profile display data
11. Settings changes
12. Empty state conditions

If persistence is too large for this pass, create a clean repository/state-holder boundary and clearly mark persistence as deferred.

Do not fake persistence as complete.

Do not claim incomplete behavior is complete.

## Assets and visual resources

Inspect source assets if present.

If equivalent assets are needed in Android:

1. Copy or recreate only what is necessary under `Kikaria-Android`.
2. Do not use private absolute paths.
3. Do not create fake unrelated assets.
4. Do not reference missing resources.
5. Prefer simple vector drawables or Compose shapes if they reproduce the source better than random Material icons.

## Android project quality

Keep the Android project coherent.

Preferred structure:

1. `ui/theme`
2. `ui/components`
3. `ui/screens`
4. `model`
5. `state`
6. `repository`
7. `navigation`

Adapt to the existing project structure, but reduce chaos.

Avoid:

1. Massive single-file app
2. Per-screen duplicate styles
3. Random hard-coded colors
4. Random hard-coded font sizes
5. Random hard-coded padding
6. Random icon choices
7. Unrelated architecture churn
8. Fake test/build success

## Aggressive completion requirement

Use the available iteration budget.

Do not stop after a small patch.

After each major area, continue to the next area unless all major areas have been addressed.

A good run should include meaningful work across theme, components, and multiple screens.

A bad run is one that only changes one theme file and then summarizes.

Before final summary, verify that you have considered:

1. Home screen
2. Review screen
3. Settings/profile
4. Preset/knowledge management
5. Important/mastered lists
6. Shared typography
7. Shared icons
8. Shared components
9. State/repository behavior
10. Target file organization

## Build/test policy

Do not add `build_command` or `test_command` to `FORGIS_CONFIG.yml`.

If existing safe tools can inspect project structure, use them.

If a build cannot be run safely, say so.

Do not claim build success unless a real build actually ran and passed.

## Forbidden actions

Do not:

1. Modify files outside `Kikaria-Android`.
2. Modify source repo files.
3. Modify unrelated target repo files.
4. Add secrets or API keys.
5. Read private local files.
6. Hard-code local paths.
7. Hard-code “Vita” as visible username.
8. Rewrite Kikaria into a generic Material 3 sample.
9. Create fake screenshots.
10. Create fake tests.
11. Claim unrun tests passed.
12. Restore Aider.
13. Use arbitrary shell.
14. Change Forgis runtime code.
15. Change GitHub workflow code.

## Required final checks

Before final response, run or use available tools for:

1. `git_status`
2. `git_diff`

Review the diff and ensure all changes are under `Kikaria-Android`.

## Final summary requirements

The final summary must be specific and grounded.

Include:

1. Source files inspected
2. Target Android files inspected
3. Target Android files modified
4. Typography reconstruction completed
5. Icon reconstruction completed
6. Shared component reconstruction completed
7. Home screen reconstruction completed
8. Review/study reconstruction completed
9. Preset/knowledge management reconstruction completed
10. Important/mastered list reconstruction completed
11. Settings/profile reconstruction completed
12. State/repository behavior changes
13. Remaining mismatches
14. Deferred work
15. Build/test status
16. Confirmation that all writes stayed inside `Kikaria-Android`
17. Confirmation that source repo was not modified
18. Suggested next pass

Be honest. Do not overclaim.
