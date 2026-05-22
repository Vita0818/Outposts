# Kikaria Android Complete Migration Pass

This is a complete migration pass for Kikaria Android.

The target Android project already exists under Kikaria-Android, but it is still incomplete and still has compilation errors. This pass must not be a small repair pass, a visual polish pass, or a partial migration pass. It must systematically migrate the full Kikaria source app into the Android/Kotlin/Jetpack Compose target.

All writes must stay under Kikaria-Android.

Do not modify files outside Kikaria-Android.

Do not modify the source Kikaria repository.

Do not modify Forgis runtime files.

Do not modify GitHub workflow files.

Do not add secrets, API keys, private local paths, or hard-coded personal data.

Do not hard-code the visible username "Vita".

The source Kikaria app is the authority. The Android app must become a faithful Kotlin/Jetpack Compose reconstruction of Kikaria, not a generic Android Material sample.

This pass has three required stages:

Stage 1: repair compilation.
Stage 2: inspect the entire source Kikaria app and build a complete migration inventory.
Stage 3: implement all missing Android screens, components, state, data, navigation, and UI details under Kikaria-Android.

Do not skip any stage.

Do not stop after fixing one compiler error.

Do not stop after creating a few screens.

Do not stop after a shallow UI approximation.

Do not claim completion unless all major Kikaria source areas have been inspected and either migrated or explicitly listed as deferred with a precise reason.

Current known situation:

The Android project still has compilation errors from previous runs. Treat compilation as a required acceptance condition, not an optional cleanup task.

Start by inspecting the current Android project under Kikaria-Android.

Then inspect the source Kikaria repository.

Do not rely on memory.

Do not infer from generic Android conventions.

Do not rewrite Kikaria as a new product.

Stage 1: compilation repair

Before implementing more features, repair the current Android project until it compiles, or make the best possible progress and report the first remaining compiler error exactly.

Use available Forgis build/test tools if possible.

Prefer this compile command if available:

cd Kikaria-Android && ./gradlew :app:compileDebugKotlin --no-daemon --stacktrace

If that is unavailable, try:

cd Kikaria-Android && ./gradlew :app:assembleDebug --no-daemon --stacktrace

If Forgis safe command policy cannot run Gradle, inspect available compile logs and repair known Kotlin/Gradle errors from those logs.

Do not claim build success unless a real build or compile command actually ran and passed.

Fix compiler errors in this order:

1. Gradle configuration errors
2. Android plugin / Kotlin plugin / Compose compiler configuration errors
3. Missing dependencies
4. Missing imports
5. Unresolved references
6. Type mismatches
7. Composable parameter mismatches
8. Navigation route mismatches
9. ViewModel method or property mismatches
10. Data model field mismatches
11. Material icon dependency or unavailable icon problems
12. Resource reference errors
13. Persistence API errors
14. Deprecated or unavailable Compose APIs

Do not patch randomly.

Do not delete major app areas merely to hide errors.

If a component blocks compilation, simplify it enough to compile while preserving its intended role, then continue migration.

Stage 2: full source inspection and migration inventory

After the immediate compile repair pass, inspect the original Kikaria source app broadly and systematically.

You must inspect source areas for:

1. App entry point
2. Navigation structure
3. Home screen
4. Review/study screen
5. Range selection or tag selection flow if present
6. Preset switching
7. Preset management
8. Knowledge item model
9. Knowledge item import
10. Knowledge item editing
11. Knowledge item deletion
12. Knowledge item tags
13. Hint/prompt display
14. Answer/content display
15. Important collection / key collection
16. Mastered list
17. Daily goal
18. Countdown day
19. Review progress
20. Review history
21. Profile page
22. Avatar handling
23. Settings page
24. Daily goal settings
25. Countdown settings
26. Preset settings
27. Markdown/import format guide if present
28. Onboarding or guide screens if present
29. Empty states
30. Error states
31. Toast/snackbar/temporary prompt behavior
32. Gesture behavior
33. Animations and transitions
34. Typography
35. Mixed Chinese, English, and number font handling
36. Colors
37. Cards
38. Buttons
39. Icon usage / SF Symbols / image assets
40. Bubbles / metric cards / floating components
41. Persistence and state storage
42. Sample or bundled data
43. Per-preset state
44. Important/mastered duplicate prevention
45. Session randomization or shuffle behavior

Create a concrete internal migration checklist from the inspected source. Use that checklist to drive implementation. Do not only inspect one or two source files.

The final summary must list the important source files inspected.

Stage 3: complete Android migration

After compile repair and source inspection, implement the full Android migration under Kikaria-Android.

The Android app must include all core Kikaria concepts that exist in the source app.

Required product concepts:

1. Presets / knowledge sets
2. Knowledge item name
3. Prompt or hint
4. Answer or content
5. Tags
6. Study/review flow
7. Range or tag-based selection if present in source
8. Randomized or shuffled session behavior if present in source
9. Daily goal
10. Countdown day
11. Current preset display
12. Important collection / key collection
13. Mastered list
14. Per-preset state where source supports it
15. Profile/settings
16. Knowledge management
17. Import or format guide if present
18. Review history or daily overview if present
19. Empty states
20. Durable persistence boundary or clear TODO if persistence is incomplete

Do not fake completed functionality.

If full durable persistence is too large for this pass, create a clean repository/state-holder boundary and mark durable persistence as TODO. The UI and ViewModel should still be coherent and compile.

Architecture requirements:

Keep the Android project coherent and maintainable.

Use or create these layers where appropriate:

1. ui/theme
2. ui/components
3. ui/navigation
4. ui/screens
5. model
6. state or viewmodel
7. repository or persistence
8. data/sample or bundled presets

Avoid:

1. Massive single-file UI
2. Duplicated per-screen components
3. Random hard-coded paddings
4. Random hard-coded font sizes
5. Random icon choices
6. Fake data wired directly into every screen
7. Unrelated architecture churn
8. Generic Material sample structure
9. Hard-coded user-private data

Typography requirements:

Create or repair a centralized Kikaria typography system.

Do not scatter font sizes and font families across screens.

Define stable typography tokens for:

1. App title
2. Page title
3. Section title
4. Card title
5. Body text
6. Secondary body text
7. Caption
8. Button text
9. Large metric text
10. Bubble text
11. Knowledge item title
12. Review prompt
13. Review answer
14. Settings row title
15. Settings row subtitle
16. Tag text

Preserve Kikaria's calm, refined, study-oriented visual direction.

If the source app uses serif-like typography, preserve that direction on Android as closely as feasible.

If exact iOS fonts cannot be used on Android, choose a stable Android approximation and centralize it.

Chinese, English, and numbers must not look randomly mixed.

If exact mixed-script rendering is too large to implement fully, create a clean text component boundary and add a TODO for precise mixed-script run splitting. Do not scatter font hacks across screens.

Icon requirements:

Create or repair a centralized Kikaria icon mapping layer.

Do not scatter arbitrary Material icons across screens.

Map source Kikaria icon semantics to Android equivalents intentionally.

Important icon actions include:

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
15. Previous
16. Next
17. Random
18. Show hint
19. Show answer
20. Import
21. Guide/help

Do not wrap every icon in a visible circle.

Do not add duplicate circular backgrounds.

Equivalent icon buttons must share size, padding, icon size, and visual treatment.

If an exact SF Symbol is unavailable on Android, choose the closest semantic and visual equivalent.

Shared component requirements:

Repair or rebuild shared Compose components so repeated UI is not duplicated.

Centralize or repair:

1. Page shell
2. Page title
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
19. Collection/mastered item row
20. Import/guide row

Screens should consume shared components rather than duplicating local styling.

Home screen requirements:

Inspect the original Kikaria home screen deeply.

Reconstruct the Android home screen to match source intent for:

1. App title placement
2. Profile/avatar location
3. Central visual system
4. Bubble/card system
5. Daily goal component
6. Countdown component
7. Current preset component
8. Primary start action
9. Secondary actions
10. Icon sizes
11. Text sizes
12. Spacing
13. Animation or motion placeholders if present
14. Calm minimal visual style

Do not make the Home screen a generic dashboard.

Review/study requirements:

Inspect the original review/study flow deeply.

Reconstruct Android review/study behavior and UI for:

1. Current knowledge item title
2. Prompt/hint reveal
3. Answer reveal
4. Important collection state
5. Mastered state
6. Previous item behavior
7. Next or random item behavior
8. Swipe or gesture semantics if present
9. Long answer handling
10. Button/icon placement
11. Review card style
12. State transition visual hierarchy
13. Minimal instructional text
14. Duplicate prevention for important/mastered actions
15. Session ordering or shuffle behavior if present

Do not make it a generic flashcard screen.

Preset and knowledge management requirements:

Inspect the source implementation for preset and knowledge management.

Reconstruct Android structures for:

1. Current preset display
2. Preset switching
3. Preset list row/card
4. Preset creation if present
5. Preset editing if present
6. Knowledge item list
7. Knowledge item details
8. Knowledge item add/edit/delete if present
9. Knowledge item tags
10. Tag or range selection
11. Per-preset state boundaries
12. Empty states
13. Shared list components
14. Screen title and action placement

Do not invent a different management model.

Important collection and mastered list requirements:

Inspect source behavior and UI for important collection and mastered list.

Reconstruct Android so that:

1. Important collection and mastered list are separate concepts if source separates them.
2. Their visual treatment follows source.
3. Their row/card components are shared where appropriate.
4. Add/remove state is reflected consistently.
5. Empty states match Kikaria style.
6. Gesture behavior is approximated or clearly deferred.
7. Button state prevents duplicate-add behavior if the source prevents it.
8. Per-preset important/mastered state is preserved if source supports it.

Settings/profile requirements:

Inspect source settings and profile pages.

Reconstruct Android settings/profile for:

1. Page title position
2. Profile/avatar structure
3. User display name handling
4. Avatar placeholder behavior
5. Daily goal setting entry
6. Countdown setting entry
7. Preset management entry
8. Import/format guide entry if present
9. Section spacing
10. Settings row style
11. Icon treatment
12. Typography hierarchy

Do not hard-code the visible display name.

Use neutral placeholder text or state/sample repository data only if needed.

State and repository requirements:

Improve component functionality, not just visuals.

Inspect source state behavior and repair Android state boundaries for:

1. Current preset
2. Daily goal
3. Countdown day
4. Study item list
5. Current review item
6. Hint visible state
7. Answer visible state
8. Important collection membership
9. Mastered membership
10. Per-preset important/mastered state if source has it
11. Profile display data
12. Settings changes
13. Empty state conditions
14. Review history
15. Today's overview / daily progress
16. Knowledge item add/edit/delete where feasible
17. Preset switching and selection
18. Import/guide placeholder

If durable persistence is too large for this pass, create a clean repository/state-holder boundary and clearly mark persistence as deferred.

Do not fake persistence as complete.

Assets requirements:

Inspect source assets if present.

If equivalent assets are needed in Android:

1. Copy or recreate only what is necessary under Kikaria-Android.
2. Do not use private absolute paths.
3. Do not create fake unrelated assets.
4. Do not reference missing resources.
5. Prefer simple vector drawables or Compose shapes if they reproduce the source better than random Material icons.

Compilation and validation requirement:

This pass must actively try to leave the project in a compilable state.

Before final summary, use available tools for:

1. git_status
2. git_diff
3. compile/build if available

Prefer compile command:

cd Kikaria-Android && ./gradlew :app:compileDebugKotlin --no-daemon --stacktrace

If unavailable, try:

cd Kikaria-Android && ./gradlew :app:assembleDebug --no-daemon --stacktrace

If compilation still fails, report the exact first remaining compiler error and what was attempted.

Do not claim build success unless it actually passed.

Completion requirement:

Use the available iteration budget.

Do not stop after fixing only the first compiler error.

Do not stop after changing only one or two files.

Do not stop after making only visual changes.

Do not stop until all major Kikaria areas have been inspected and either migrated or explicitly deferred.

Before final summary, ensure you have considered:

1. Compilation status
2. Home screen
3. Review/study screen
4. Settings/profile
5. Preset/knowledge management
6. Important/mastered lists
7. Daily goal
8. Countdown day
9. Review history / today overview
10. Shared typography
11. Shared icons
12. Shared components
13. State/repository behavior
14. Target file organization
15. Source inspected coverage

Forbidden actions:

Do not:

1. Modify files outside Kikaria-Android.
2. Modify source repo files.
3. Modify Forgis runtime files.
4. Modify GitHub workflow files.
5. Add secrets or API keys.
6. Read private local files.
7. Hard-code local paths.
8. Hard-code "Vita" as visible username.
9. Rewrite Kikaria into a generic Material 3 sample.
10. Create fake screenshots.
11. Create fake tests.
12. Claim unrun tests passed.
13. Restore Aider.
14. Use arbitrary shell.
15. Change source/target write boundaries.
16. Modify unrelated target repository files.

Final summary requirements:

The final summary must include:

1. Whether compilation was actually verified
2. Exact compile/build command and result, if run
3. First remaining compiler error if compilation still fails
4. Source Kikaria files inspected
5. Android files inspected
6. Android files modified
7. Compiler errors fixed
8. Features migrated
9. Screens migrated or repaired
10. Typography reconstruction completed
11. Icon reconstruction completed
12. Shared component reconstruction completed
13. Home screen reconstruction completed
14. Review/study reconstruction completed
15. Preset/knowledge management reconstruction completed
16. Important/mastered list reconstruction completed
17. Settings/profile reconstruction completed
18. State/repository behavior changes
19. Remaining mismatches
20. Deferred work with reasons
21. Confirmation that all writes stayed inside Kikaria-Android
22. Confirmation that source repo was not modified
23. Suggested next pass

Be honest. Do not overclaim.
