# Forgis Task

You are running inside Forgis.

This is a structured translation pass for the existing Kikaria-Android project.

The current Android app can run, but the migration is still too superficial. It looks and behaves like a partially interpreted Android version rather than a deeply translated Kikaria product.

This pass should change the working mode.

Do not do another free-form UI tweak pass.

Do not perform a full rewrite from scratch.

Do not blindly regenerate the Android project.

Do not modify the source repository.

## Hard safety rule

The source repository must remain read-only.

## Target work area

All edits must stay inside:

Kikaria-Android

Do not modify:

- the source repository
- Outposts repository root
- FORGIS_CONFIG.yml
- FORGIS_TASK.md
- .github/workflows

Do not access secrets.

## Goal

Move the Android project toward a more complete source-guided migration by using a structured file-by-file / unit-by-unit translation workflow.

The goal is not to mechanically translate every Swift file into exactly one Kotlin file.

The goal is to read the source project systematically, establish a source-to-target mapping, then migrate source files or functional units one at a time into the Android project.

This pass should improve architectural faithfulness, UI fidelity, state semantics, and product identity.

## Required working mode

You must not start by randomly editing files.

Follow this process:

1. Read the source repository structure.
2. Read the existing Kikaria-Android target structure.
3. Compare source and target at a project level.
4. Create or update a migration plan.
5. Create or update a source-target mapping.
6. Then enter a sequential translation loop.

The middle stage must be one-file / one-unit at a time:

- Pick one source file or one coherent source functional unit.
- Read that source file or unit carefully.
- Read the related existing Android target file or files.
- Decide whether the Android target already covers it, partially covers it, or misses it.
- Translate or repair the corresponding Android implementation.
- Update the migration progress record.
- Move to the next source file or functional unit.

Do not jump straight to final UI polishing.

Do not only fix the currently visible screen.

Do not only work from screenshots.

Use the source repository as the main reference.

## Required persistent progress files

Create or update these files under Kikaria-Android:

1. Kikaria-Android/FORGIS_TRANSLATION_PLAN.md

This should describe the overall staged migration plan.

2. Kikaria-Android/FORGIS_SOURCE_TARGET_MAP.md

This should map source files / source functional units to Android target files / Android functional units.

The map should distinguish:

- translated
- partially translated
- missing
- intentionally deferred
- needs review

3. Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

This should record what was processed in this run, in order.

For each processed source file or unit, record:

- source path
- target path or target unit
- what was translated
- what was changed
- what remains
- whether it affects build, state, UI, or data model

These files are important. If the run cannot finish everything, future runs should be able to continue from them.

## Translation principles

Use the following principles.

Business logic should be translated.

State logic should be mapped.

UI should be rebuilt natively from extracted intent and visual rules.

Do not mechanically translate SwiftUI line by line into Compose.

For each source UI area, first infer:

- screen purpose
- visual hierarchy
- state inputs and outputs
- navigation behavior
- gestures and interactions
- typography
- colors
- spacing
- card / glass / bubble hierarchy
- empty or edge states

Then rebuild or refine the Android implementation using idiomatic Jetpack Compose.

For state and model code:

- preserve Swift runtime semantics where possible
- avoid turning SwiftUI state into ordinary Kotlin vars
- use Compose-observable state or ViewModel-backed state where appropriate
- preserve Swift struct value semantics using immutable Kotlin data classes where practical
- preserve associated-value enum semantics using sealed classes or sealed interfaces when needed
- avoid unsafe !! unless truly justified

## What to inspect

Inspect the source repository systematically.

At minimum, inspect the source tree and then targeted files related to:

- app entry
- core ContentView / main UI implementation
- data models
- preset / Markdown parsing logic
- study tracking
- typography
- adaptive layout
- math / LaTeX rendering
- source documentation and product specs

Inspect the current Android target systematically.

At minimum, inspect:

- Gradle and manifest files
- MainActivity
- navigation
- ViewModel
- data models
- Markdown parser
- home screen
- review screen
- scope selection
- mastered / reinforcement screens
- theme and component files
- existing Forgis reports and progress files if present

Do not dump the entire source repository at once.

Use targeted reads.

Read large files in pages.

## Work prioritization

Process source files / functional units in an order that improves the Android project most efficiently.

Prefer this order unless your inspection suggests a better one:

1. project architecture and app entry
2. data models and preset parsing
3. ViewModel and state semantics
4. home screen
5. review flow
6. scope selection
7. reinforcement / important collection
8. mastered list
9. typography / theme / adaptive layout
10. Markdown and content rendering
11. math / LaTeX support
12. reports and final consistency check

Do not attempt to fully finish every advanced subsystem if time is insufficient.

Prioritize correctness and faithful structure over superficial visual tweaks.

## Target output quality

After this pass, the Android project should be more faithful to Kikaria in structure and behavior.

It should not merely look slightly better.

It should have clearer mapping from the iOS source to Android target code.

The home screen and core review flow should preserve the original product identity better.

State handling should be more deliberate.

Reports should make it clear what has and has not been migrated.

## Build and run sanity

Keep the Android project runnable.

Do not introduce fragile dependencies.

Do not make broad Gradle changes unless necessary.

At the end, perform a static build-oriented self-review:

- Gradle files
- Manifest
- package names
- resource references
- Kotlin syntax
- Compose API usage
- obvious missing imports
- obvious broken references
- navigation references
- ViewModel references

If an actual build command is unavailable, say so in the report and perform static checks instead.

Do not claim a real Gradle build succeeded unless it was actually run.

## Reports to update

Create or update:

- Kikaria-Android/FORGIS_TRANSLATION_PLAN.md
- Kikaria-Android/FORGIS_SOURCE_TARGET_MAP.md
- Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

Also update, if relevant:

- Kikaria-Android/FORGIS_MIGRATION_REPORT.md
- Kikaria-Android/FORGIS_HOME_UI_REPORT.md
- Kikaria-Android/FORGIS_BUILD_FIX_REPORT.md

Do not create excessive report files beyond these unless necessary.

## Completion

When finished, return final_summary with:

- source files or functional units inspected
- Android files inspected
- source-target mapping created or updated
- files / units translated in this run
- Android files changed
- what improved structurally
- what improved visually
- what improved in state semantics
- whether all writes stayed inside Kikaria-Android
- whether the source repository stayed read-only
- whether build/run was actually checked or only statically reviewed
- what remains for the next structured translation pass
