# Kikaria Android Compile Repair Pass

This is a compile repair pass for the existing Kikaria Android project.

The previous Forgis run created and modified multiple Android/Kotlin/Jetpack Compose files under:

`Kikaria-Android`

However, the project currently does not compile.

Your only goal in this pass is to make the Android project compile cleanly.

Do not continue UI reconstruction in this pass.

Do not add new product features.

Do not add new screens unless absolutely necessary to resolve references.

Do not redesign the app.

Do not modify files outside `Kikaria-Android`.

Do not modify the source repository.

## Priority

Compilation success is the highest priority.

Visual fidelity is secondary in this pass.

If you must choose between perfect UI fidelity and successful compilation, choose successful compilation.

## Required inspection

First inspect the current Android project under `Kikaria-Android`.

Pay special attention to files recently created or modified, including but not limited to:

- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/guide/MarkdownFormatGuideScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/onboarding/OnboardingScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/ReviewHistoryScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/overview/TodayOverviewScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/preset/PresetSelectionScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/EditProfileScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/SettingsScreen.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/util/KikariaPersistence.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/navigation/KikariaNavGraph.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/data/SamplePresets.kt`
- `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/home/HomeScreen.kt`

Also inspect:

- `Kikaria-Android/settings.gradle` or `settings.gradle.kts`
- `Kikaria-Android/build.gradle` or `build.gradle.kts`
- `Kikaria-Android/app/build.gradle` or `app/build.gradle.kts`
- AndroidManifest
- theme files
- model files
- navigation route definitions
- ViewModel/state files
- repository/sample data files

## Required build-error strategy

If build output or compiler errors are available, use them as the source of truth.

Fix errors in this order:

1. Gradle configuration errors
2. Android plugin / Kotlin plugin / Compose compiler configuration errors
3. Missing dependencies
4. Missing imports
5. Unresolved references
6. Type mismatches
7. Composable parameter mismatches
8. Navigation route mismatches
9. ViewModel API mismatches
10. Data model field mismatches
11. Resource references
12. Icon dependency issues
13. Deprecated or unavailable Compose APIs

Do not patch randomly.

Follow compiler errors.

## Common likely issues to check

Check for these likely problems:

1. Material icon imports that require `material-icons-extended` but dependency is missing.
2. Calls to ViewModel methods that do not exist.
3. Screen composables expecting parameters not provided by navigation.
4. Navigation route names not defined centrally.
5. Data classes whose constructor arguments do not match sample data.
6. Use of iOS-inspired state names that do not exist in Android models.
7. Duplicate composable names.
8. Missing `@Composable` annotation.
9. Missing `remember` / `mutableStateOf` imports.
10. Wrong package declarations.
11. Files placed in package paths inconsistent with declared package names.
12. Wrong use of `Icons.Default.*` names not available in current dependency set.
13. Missing `LocalContext` import.
14. Missing coroutine/import issues.
15. Use of APIs requiring a higher minSdk or dependency not configured.

## Repair policy

Repair with minimal, coherent changes.

Prefer:

1. Adding missing imports
2. Aligning function signatures
3. Aligning route names
4. Adding missing ViewModel methods only when clearly needed
5. Simplifying broken UI code rather than adding more complexity
6. Replacing unavailable icons with available icons
7. Adding dependencies only when truly necessary
8. Centralizing route constants if routes are currently inconsistent
9. Keeping all changes inside `Kikaria-Android`

Avoid:

1. Adding unrelated screens
2. Adding fake features
3. Large visual redesign
4. Large architecture rewrite
5. Modifying source repo
6. Modifying files outside `Kikaria-Android`
7. Claiming build success without a real build

## Gradle dependency policy

If compilation fails because of missing Compose dependencies, fix Gradle minimally.

Examples of acceptable dependency repairs:

1. Add missing Compose Material 3 dependency if the code uses Material 3.
2. Add material icons extended only if the code truly uses extended Material icons.
3. Align Compose BOM usage if already present.
4. Remove or replace icons instead of adding large dependency if only one unsupported icon is used.

Do not add random libraries.

Do not add network/API libraries.

Do not add persistence libraries unless absolutely required.

## Navigation repair policy

Inspect `KikariaNavGraph.kt` and all screen route usage.

Fix:

1. Missing routes
2. Wrong route strings
3. Missing navigation callbacks
4. Screens referenced but not registered
5. Screens registered but not reachable
6. Parameter mismatch between screen composables and nav graph

Prefer simple stable route constants.

## ViewModel/state repair policy

Inspect `KikariaViewModel.kt`.

Fix:

1. Missing methods referenced by screens
2. Missing state properties referenced by screens
3. Wrong state property names
4. Type mismatches between state and UI
5. Important collection / mastered membership methods
6. Current preset / current item methods
7. Hint/answer reveal state methods

Do not implement full persistence unless necessary.

If persistence is incomplete, keep a simple in-memory implementation that compiles and mark durable persistence as TODO.

## Persistence repair policy

Inspect `KikariaPersistence.kt`.

If it causes compilation errors or introduces unsupported complexity, simplify it.

Acceptable options:

1. Make it compile with current Android APIs.
2. Keep it as a placeholder repository boundary.
3. Replace broken persistence internals with safe in-memory or simple placeholder implementation.
4. Add TODO comments for durable persistence.

Do not fake production-ready persistence.

## Final checks

Before final summary:

1. Use `git_status`.
2. Use `git_diff`.
3. If available and allowed, run the configured build command or Gradle compile command.
4. If build cannot be run by Forgis safe command runner, clearly state that.

## Final summary requirements

Your final summary must include:

1. Whether compilation was actually verified.
2. If build was run, the exact command and result.
3. If build was not run, explain why.
4. Files modified.
5. Major compile errors fixed.
6. Gradle/dependency changes, if any.
7. Navigation fixes, if any.
8. ViewModel/state fixes, if any.
9. Persistence fixes, if any.
10. Remaining known risks.
11. Confirmation that all changes stayed inside `Kikaria-Android`.
12. Confirmation that source repo was not modified.

Do not overclaim.

Do not say “build passes” unless it really passed.
