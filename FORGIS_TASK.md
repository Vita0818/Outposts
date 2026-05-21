# Kikaria Android Migration Task

Migrate the current Kikaria app into an Android application built with Kotlin and Jetpack Compose.

Write all generated target code under `Kikaria-Android` only. Do not modify files outside that target subdirectory.

The target repository is provided externally by the Forgis workflow input. Do not assume or hard-code the target repository name inside generated code.

## Target platform

Use:

- Android
- Kotlin
- Jetpack Compose

Do not write platform information into `FORGIS_CONFIG.yml`. This task file is the authority for the target stack.

## Product identity

Kikaria is a local study and memorization app. It is not a generic flashcard demo and must not be redesigned into a generic Material sample app.

Preserve the existing Kikaria information architecture, naming intent, visual hierarchy, and interaction style as much as possible.

Do not hard-code Vita-specific personal data, user names, avatars, local paths, secrets, API keys, or environment assumptions.

## First-run scope

This is the first Android migration run. The goal is to create a clean Android/Kotlin/Compose foundation and migrate the primary structure and first set of core screens.

Prioritize:

1. Android project structure under `Kikaria-Android`
2. Kotlin + Jetpack Compose app foundation
3. Primary navigation structure
4. Core data models
5. Reusable UI components
6. Home screen foundation
7. Review / study session foundation
8. Preset / knowledge item model foundation
9. Settings/profile foundation where feasible
10. Clear TODOs for deferred areas

Do not pretend deferred features are complete. Clearly report migrated, deferred, and blocked areas.

## Source-reading requirement

Before implementing a target file, read the corresponding source files from the Kikaria source repository.

Do not invent the product structure from memory. Inspect source files, source UI composition, models, navigation, assets, and naming before writing Android code.

## Core Kikaria concepts to preserve

Preserve these concepts when present in the source app:

- Presets / knowledge sets
- Knowledge point name, prompt/hint, content, and tags
- Study/review flow
- Randomized or shuffled study sessions
- Daily goal / daily review progress
- Countdown-day style home information
- Important collection / key collection
- Mastered list
- Per-preset state where applicable
- Profile/settings structure where applicable
- Minimal, calm, study-oriented UI style

## UI and visual requirements

Preserve Kikaria's original visual direction as much as Compose reasonably allows.

Important:

1. Do not redesign it into a default Material demo.
2. Prefer a minimal, clean, calm interface.
3. Preserve visual hierarchy and spacing from the source app when possible.
4. Prefer reusable Compose components instead of duplicating screen-specific styling.
5. Centralize typography and color decisions.
6. Do not hard-code user names such as “Vita”.
7. If the source app uses refined serif-like typography or mixed-script font behavior, create a centralized typography layer and leave clear TODOs for exact font matching if Android font assets are not yet available.
8. Avoid adding unnecessary UI elements, labels, buttons, or explanatory text.

## Architecture requirements

Prefer a maintainable Android structure.

Use shared models, state holders, repositories, and reusable Compose components.

Reasonable first-run structure may include:

- `app/src/main/java/.../MainActivity.kt`
- Compose navigation entry
- screen package
- component package
- model package
- state or viewmodel package
- repository or sample data package
- theme package

Use placeholder/sample data only where necessary to make migrated screens coherent. Mark placeholders clearly.

## Safety and boundaries

All target changes must stay inside:

`Kikaria-Android`

Do not modify source repository files.

Do not modify unrelated target repository files outside `Kikaria-Android`.

Do not add secrets.

Do not add real API keys.

Do not add local absolute paths.

Do not configure fake build/test results.

Do not claim the Android project builds unless a real build was run successfully by Forgis.

## Build/test requirement for first run

Do not add `build_command` or `test_command` to `FORGIS_CONFIG.yml` for this first run.

If build/test cannot be run because the Android project foundation is incomplete or the safe command runner does not allow the needed command, report that honestly.

## Required final behavior

Before final summary, use `git_diff` to review target changes.

Final summary must include:

1. Files created or modified
2. Main Android foundation added
3. Screens/components migrated
4. Source areas inspected
5. Deferred areas
6. Blocked areas, if any
7. Whether build/test was run
8. Clear next suggested migration unit
