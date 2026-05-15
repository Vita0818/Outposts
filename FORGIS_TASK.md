# Forgis Task

You are running inside Forgis.

This is a staged source-guided translation pass for the existing Kikaria-Android project.

The source repository is Kikaria.

The target repository is Outposts.

The Android target project is located at:

Kikaria-Android

The current Android version already exists and can be inspected. However, it is still not faithful enough to the original Kikaria source in structure, behavior, state semantics, and visual identity.

This run should use the staged_translation execution mode.

Do not perform a free-form UI tweak pass.

Do not randomly edit files.

Do not rewrite the whole project from scratch.

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

Do not print secrets.

Do not add analytics, ads, accounts, telemetry, cloud services, or unrelated network services.

## High-level goal

Move Kikaria-Android from a rough runnable Android approximation toward a more faithful translation of Kikaria.

The goal is not to mechanically translate Swift code line by line.

The goal is:

- business logic should be translated;
- state logic should be mapped;
- UI should be rebuilt natively from extracted intent and visual rules.

For UI, preserve user-visible product identity rather than source-code shape.

For state and model logic, preserve runtime semantics rather than superficial names.

## Required staged workflow

Use the staged_translation workflow seriously.

Do not return final_summary early.

Do not stop after only polishing one screen.

Do not skip the progress artifacts.

This run should follow three stages:

1. Overall reading and understanding.
2. One-file or one-functional-unit translation.
3. Stabilization and build-oriented review.

## Stage 1: overall reading and understanding

First inspect the source repository structure and the existing Android target structure.

Read enough source files and target files to understand:

- app entry and routing;
- core data model;
- preset and Markdown parsing;
- review flow;
- important collection / reinforcement flow;
- mastered list flow;
- home screen identity;
- theme, typography, glass, and visual design rules;
- current Android architecture;
- current Android gaps.

Create or update these files under Kikaria-Android:

- FORGIS_TRANSLATION_PLAN.md
- FORGIS_SOURCE_TARGET_MAP.md
- FORGIS_TRANSLATION_PROGRESS.md

The plan should describe the staged migration strategy.

The source-target map should map source files or source functional units to Android target files or target functional units.

The progress file should record what this run actually processed.

Do not do large code rewrites during the overview stage.

## Stage 2: one-file or one-unit translation

Process the source repository systematically.

The middle stage must be one source file or one coherent functional unit at a time.

For each selected source file or unit, follow this four-step loop:

1. Feed and understand

Read the selected source file or source unit.

Read the related Android target file or files.

Identify what the source unit does, what state it owns, what UI or behavior it drives, and whether the Android version already covers it.

2. Translate or merge

Translate the source unit into the Android target in an idiomatic way.

For business logic, preserve semantics.

For state, use Compose-observable or ViewModel-backed state where appropriate.

For UI, rebuild from intent using idiomatic Compose rather than mechanically copying SwiftUI structure.

Do not jump to unrelated source files during this step.

3. Read-only comparison

After writing, read the source unit again and read the generated or modified target files.

Compare source intent and Android result.

Write a focused comparison report under:

Kikaria-Android/FORGIS_COMPARE_REPORTS

or append a focused comparison entry to:

Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

This comparison should cover what is faithful, what is partial, and what remains.

4. Revision

Use the comparison report to make a small targeted revision.

Then update:

- FORGIS_TRANSLATION_PROGRESS.md
- FORGIS_SOURCE_TARGET_MAP.md

Mark the unit as one of:

- translated
- partially translated
- needs review
- deferred
- missing target support

Only then move to the next source file or source unit.

## Folder-level review

Whenever all directly relevant files in a source folder have been processed, perform a folder-level review.

For that folder:

- review the processed source files as a group;
- review the corresponding Android target files as a group;
- check cross-file consistency;
- check shared state, navigation, model, UI, and component relationships;
- make small consistency fixes if needed;
- update FORGIS_TRANSLATION_PROGRESS.md and FORGIS_SOURCE_TARGET_MAP.md.

If the folder is too large, review it in batches and clearly record what was included or omitted.

Do not silently skip folder-level review.

## Suggested processing priorities

Use your own judgment after inspecting the source and target, but prioritize work that improves faithfulness and runnability.

A reasonable order is:

1. app entry and project structure;
2. data models;
3. preset and Markdown parsing;
4. ViewModel and state semantics;
5. home screen;
6. review flow;
7. scope selection;
8. reinforcement / important collection;
9. mastered list;
10. theme, typography, adaptive layout, and glass components;
11. content rendering and math-related areas;
12. final consistency and build-oriented review.

Do not spend the entire run on visual details.

Do not ignore state semantics.

Do not ignore build stability.

## Translation principles

SwiftUI to Compose is not syntax translation.

For model and business logic:

- preserve value semantics where practical;
- prefer immutable Kotlin data classes for Swift value-like models;
- use copy-style updates instead of shared mutable data when appropriate;
- preserve nullability and early-exit control flow;
- avoid unsafe forced null handling unless truly justified.

For state:

- do not translate SwiftUI state into ordinary Kotlin variables that do not drive recomposition;
- use Compose state, snapshot state, StateFlow, ViewModel state, or another observable state pattern where appropriate;
- keep state ownership clear.

For UI:

- infer screen purpose, visual hierarchy, typography, spacing, colors, interaction, and product feeling before writing Compose;
- rebuild natively in Compose;
- avoid generic Material-looking UI when the source design has a stronger identity;
- keep Kikaria calm, minimal, soft, glass-like, premium, and study-focused.

For reports:

- be honest about partial coverage;
- do not claim real build success unless a real build or validation command actually ran;
- record what remains for future runs.

## Stage 3: stabilization and build-oriented review

After processing the selected files or units, perform a global stabilization pass.

Inspect:

- Gradle files;
- manifest;
- resources;
- package names;
- Kotlin syntax;
- Compose API usage;
- imports;
- navigation;
- ViewModel references;
- model references;
- resource references;
- obvious runtime launch blockers;
- progress files and reports.

Make small fixes needed to keep the project runnable.

Do not start a new broad migration in this stage.

If an actual build command is unavailable, perform static build-oriented review and say that no real build was run.

If validation commands are not configured, do not claim validation passed.

## Required output files

Create or update:

- Kikaria-Android/FORGIS_TRANSLATION_PLAN.md
- Kikaria-Android/FORGIS_SOURCE_TARGET_MAP.md
- Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

Use this directory for per-file or per-unit comparison reports:

- Kikaria-Android/FORGIS_COMPARE_REPORTS

You may update existing reports when relevant:

- Kikaria-Android/FORGIS_MIGRATION_REPORT.md
- Kikaria-Android/FORGIS_HOME_UI_REPORT.md
- Kikaria-Android/FORGIS_BUILD_FIX_REPORT.md
- Kikaria-Android/FORGIS_RUN_FIX_REPORT.md

Do not create excessive unrelated report files.

## Completion rules

Do not return final_summary before the staged workflow has meaningfully completed its current run scope.

If max_iterations is reached, record partial progress and next steps instead of pretending the task is complete.

When finished, return final_summary with:

- source files or source units inspected;
- Android files inspected;
- source-target map status;
- units processed in this run;
- Android files changed;
- structural improvements;
- state-semantic improvements;
- UI and product-identity improvements;
- build or run stability improvements;
- whether all writes stayed inside Kikaria-Android;
- whether the source repository stayed read-only;
- whether real build validation was run or only static review was performed;
- what remains for the next staged translation pass.
