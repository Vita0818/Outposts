# Kikaria Android Compile Repair + Full Project Replica Night Pass

This is a long-running combined compile repair and full project replica pass for the existing Kikaria Android project under Kikaria-Android.

The current Android project has already been merged into the target repository main branch. Continue from the current target main state.

The Android project currently does not compile and is still incomplete compared with the original Kikaria source app.

This pass has two ordered goals:

1. First, repair the Android project until it compiles.
2. After compilation is repaired, continue reconstructing the rest of Kikaria's UI, screens, components, state, data model, navigation, and interaction behavior as fully as possible.

Do not skip the compilation stage.

Do not do large UI reconstruction while known compiler errors remain.

After the project compiles, do not stop early. Continue with full source-informed reconstruction across the whole app.

All writes must stay under Kikaria-Android.

Do not modify files outside Kikaria-Android.

Do not modify the source Kikaria repository.

Do not modify Forgis runtime files.

Do not modify GitHub workflow files.

Do not add secrets, API keys, private local paths, or hard-coded personal data.

Do not hard-code the visible username "Vita".

The source Kikaria app is the authority. The Android app should become a faithful Kotlin/Jetpack Compose reconstruction of Kikaria, not a generic Material 3 app.

Current known compiler error:

e: file:///Users/vita/Vitemis/Outposts/Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/ReviewHistoryScreen.kt:66:9 Unresolved reference: get

Start by inspecting Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/ReviewHistoryScreen.kt, especially the area around line 66.

Determine why get is unresolved.

Likely causes include:

1. A stray or isolated get(...)
2. A Swift-style getter translated into invalid Kotlin
3. A missing receiver such as a List, Map, State, repository, or ViewModel object
4. Incorrect collection access
5. A wrong ViewModel method or property reference
6. A generated screen referring to state that does not exist

Fix this with the smallest coherent change.

Stage 1: compile repair

Compilation success is the first priority.

Before doing full UI reconstruction, repair the current Android project so it compiles.

Inspect these files as needed:

Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/ReviewHistoryScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/TodayOverviewScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/guide/MarkdownFormatGuideScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/onboarding/OnboardingScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/preset/PresetSelectionScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/EditProfileScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/SettingsScreen.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/navigation/KikariaNavGraph.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/data/SamplePresets.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/util/KikariaPersistence.kt
Kikaria-Android/app/src/main/java/com/vita0818/kikaria/model/
Kikaria-Android/app/build.gradle
Kikaria-Android/app/build.gradle.kts
Kikaria-Android/build.gradle
Kikaria-Android/build.gradle.kts
Kikaria-Android/settings.gradle
Kikaria-Android/settings.gradle.kts
Kikaria-Android/app/src/main/AndroidManifest.xml

Follow compiler errors in order.

Fix:

1. Gradle configuration errors
2. Kotlin compiler errors
3. Missing imports
4. Unresolved references
5. Type mismatches
6. Composable parameter mismatches
7. Navigation route mismatches
8. ViewModel method/property mismatches
9. Data model field mismatches
10. Material icon dependency or unavailable icon problems
11. Resource reference errors
12. Persistence API errors

Do not patch randomly.

Do not delete large parts of the app just to hide errors.

If a visually ambitious component blocks compilation, simplify it enough to compile while preserving its intended screen role.

If persistence is incomplete, keep a clean repository/state-holder boundary and mark durable persistence as TODO. Do not fake production persistence.

If a feature cannot be fully implemented in this pass, keep the API boundary coherent and mark it deferred.

Build verification requirement:

Use available Forgis tools to run compilation if possible.

Prefer this command:

cd Kikaria-Android && ./gradlew :app:compileDebugKotlin --no-daemon --stacktrace

If that is unavailable, try:

cd Kikaria-Android && ./gradlew :app:assembleDebug --no-daemon --stacktrace

If Forgis safe command policy cannot run Gradle, inspect available build logs and fix compiler errors from those logs.

Do not claim build success unless a real build or compile command actually ran and passed.

Stage 2: full Kikaria replica after compilation repair

After the Android project compiles, continue reconstructing the Android implementation so it more fully matches the original Kikaria source app.

This is not a new product design.

This is not a generic Android Material rewrite.

This is a source-informed Android reconstruction of Kikaria.

The source Kikaria app is the authority.

Read source files before reconstructing corresponding Android screens or components.

Do not rely on memory.

Do not infer from generic Android conventions.

Source inspection requirements:

Inspect the original Kikaria source app for:

1. App entry point
2. Navigation
3. Home page
4. Review/study page
5. Preset switching
6. Preset management
7. Knowledge item model
8. Knowledge item editing or management
9. Important collection
10. Mastered list
11. Settings
12. Profile/avatar
13. Daily goal
14. Countdown day
15. Typography
16. Font usage
17. Mixed Chinese, English, and number text handling
18. Buttons
19. Cards
20. Bubbles
21. Icons, SF Symbols, and image assets
22. Gestures
23. Animations
24. Persistence and state
25. Sample or preloaded data

Do not skip source inspection.

The final summary must list the source files inspected.

Full reconstruction priorities:

After compile repair, reconstruct and align:

1. Product information architecture
2. Screen hierarchy
3. Navigation structure
4. Visual hierarchy
5. Typography system
6. Mixed text treatment
7. Icon semantics
8. Button identity
9. Card identity
10. Bubble and metric component identity
11. Page title placement
12. Top action placement
13. Horizontal margins
14. Vertical spacing
15. Review and study interaction behavior
16. Preset state behavior
17. Important collection behavior
18. Mastered list behavior
19. Settings and profile structure
20. Shared component architecture
21. Placeholder and sample data boundaries
22. Persistence boundary

The Android result should feel like Kikaria, not like a Material 3 sample.

Typography reconstruction:

Create or repair a centralized typography system.

Do not scatter font sizes and font families across screens.

The typography system should include stable tokens for:

1. App title
2. Page title
3. Section title
4. Card title
5. Body text
6. Caption
7. Button text
8. Large metric text
9. Knowledge item title
10. Review prompt
11. Review answer
12. Settings row title
13. Settings row subtitle
14. Tag text

Preserve Kikaria's calm, refined, study-oriented visual direction.

If exact iOS fonts cannot be used on Android, choose a stable Android approximation and centralize it.

Chinese, English, and numbers should not look randomly mixed.

Avoid one-off font hacks inside screens.

Icon reconstruction:

Create or repair a centralized icon mapping layer.

Do not scatter arbitrary Material icons across screens.

Map source Kikaria icon semantics to Android equivalents intentionally.

Important actions include:

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
15. Previous and next
16. Show hint
17. Show answer

Do not wrap every icon in a visible circle.

Do not add duplicate circular backgrounds.

Equivalent icon buttons must share size, padding, icon size, and visual treatment.

Shared component reconstruction:

Repair or rebuild shared Compose components so repeated UI is not duplicated.

Centralize or repair:

1. Page shell
2. Page title
3. Top action row
4. Circular icon button
5. Soft or glass-like button
6. Primary start action
7. Card container
8. Metric bubble or card
9. Home bubble
10. Settings row
11. Preset row or card
12. Knowledge item row or card
13. Review card
14. Review action button
15. Tag chip
16. Empty state
17. Profile or avatar block
18. Section header

Screens should consume shared components rather than duplicating local styling.

Home screen reconstruction:

Inspect the original Kikaria home screen deeply.

Repair the Android home screen to better match:

1. App title
2. User/avatar location
3. Central visual system
4. Bubble or card system
5. Daily goal component
6. Countdown component
7. Preset component
8. Primary start action
9. Secondary actions
10. Icon sizes
11. Text sizes
12. Spacing
13. Motion or animation placeholders if present
14. Calm minimal visual style

Do not make the Home screen a generic dashboard.

Review and study reconstruction:

Inspect the original review and study flow deeply.

Repair Android so it matches source behavior and structure for:

1. Current knowledge item title
2. Prompt or hint reveal
3. Answer reveal
4. Important collection state
5. Mastered state
6. Previous, next, or random navigation
7. Swipe or gesture semantics if present
8. Long answer behavior
9. Button and icon placement
10. Review card style
11. State transition visual hierarchy
12. Minimal instructional text

Do not make it a generic flashcard screen.

Preset and knowledge management reconstruction:

Inspect the source implementation for preset and knowledge management.

Repair Android structures for:

1. Current preset display
2. Preset switching
3. Preset list row or card
4. Knowledge item row or card
5. Knowledge item tags
6. Add, edit, and delete entry points
7. Per-preset state boundaries
8. Empty states
9. Shared list components
10. Screen title and action placement

Do not invent a different management model.

Important collection and mastered list reconstruction:

Inspect source behavior and UI for important collection and mastered list.

Repair Android so that:

1. Important collection and mastered list are separate concepts if source separates them.
2. Their visual treatment follows source.
3. Their row or card components are shared where appropriate.
4. Add/remove state is reflected consistently.
5. Empty states match Kikaria style.
6. Gesture behavior is approximated or clearly deferred.
7. Button state does not allow duplicate-add behavior if the source prevents it.

Settings and profile reconstruction:

Inspect source settings and profile pages.

Repair Android so settings and profile match source in:

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

Use neutral placeholder text or state/sample repository data only if needed.

State and repository reconstruction:

Improve component functionality, not just visuals.

Inspect source state behavior and repair Android state boundaries for:

1. Current preset
2. Daily goal
3. Countdown day
4. Study item list
5. Current review item
6. Hint visible and answer visible
7. Important collection membership
8. Mastered membership
9. Per-preset important/mastered state if source has it
10. Profile display data
11. Settings changes
12. Empty state conditions
13. Review history
14. Today's overview and daily progress

If durable persistence is too large for this pass, create a clean repository/state-holder boundary and clearly mark persistence as deferred.

Do not fake persistence as complete.

Assets:

Inspect source assets if present.

If equivalent assets are needed in Android:

1. Copy or recreate only what is necessary under Kikaria-Android.
2. Do not use private absolute paths.
3. Do not create fake unrelated assets.
4. Do not reference missing resources.
5. Prefer simple vector drawables or Compose shapes if they reproduce the source better than random Material icons.

Completion requirement:

Use the available iteration budget.

Do not stop after fixing only the first compiler error.

Do not stop after changing only one or two files.

After compile repair, continue to full reconstruction.

Before final summary, ensure you have considered:

1. Home screen
2. Review/study screen
3. Settings/profile
4. Preset/knowledge management
5. Important/mastered lists
6. Shared typography
7. Shared icons
8. Shared components
9. State/repository behavior
10. Target file organization
11. Compilation status

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

Required final checks:

Before final response, use available tools for:

1. git_status
2. git_diff

If compilation was run, report the exact command and result.

If compilation could not be run, report why and provide the first remaining compiler error if known.

Final summary requirements:

The final summary must include:

1. Whether compilation was actually verified
2. Exact compile/build command and result, if run
3. Source Kikaria files inspected
4. Android files inspected
5. Android files modified
6. Compiler errors fixed
7. Typography reconstruction completed
8. Icon reconstruction completed
9. Shared component reconstruction completed
10. Home screen reconstruction completed
11. Review/study reconstruction completed
12. Preset/knowledge management reconstruction completed
13. Important/mastered list reconstruction completed
14. Settings/profile reconstruction completed
15. State/repository behavior changes
16. Remaining mismatches
17. Deferred work
18. Confirmation that all writes stayed inside Kikaria-Android
19. Confirmation that source repo was not modified
20. Suggested next pass

Be honest. Do not overclaim.
