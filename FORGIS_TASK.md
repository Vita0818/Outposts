# Forgis Task

You are running inside Forgis.

This is a focused UI style and component-layout refinement pass for the existing Kikaria-Android project.

The current Android app can run, and previous passes have moved it closer to Kikaria. However, the interface still does not fully capture the original product's visual rhythm, component arrangement, typography, softness, glass feeling, and overall study-product identity.

This run should focus mainly on:

- interface style;
- component arrangement;
- visual hierarchy;
- layout rhythm;
- typography treatment;
- color and atmosphere;
- glass/card/bubble feeling;
- screen-level UI fidelity.

Do not perform a broad migration pass.

Do not rewrite the whole project.

Do not add major new features.

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

## Main goal

Improve the Android interface so it feels more like Kikaria.

This is not a generic Material redesign.

This is not pixel-perfect copying.

This is source-guided UI reconstruction.

You should study the original iOS source, understand the intended visual structure and product feeling, compare it with the current Android implementation, then refine the Android UI using idiomatic Jetpack Compose.

The priority of this run is visual quality and component arrangement, not new functionality.

## Required staged workflow

Use the staged_translation workflow seriously.

Do not return final_summary early.

Do not only write reports.

Do not randomly edit files.

The run should follow:

1. Overview and comparison.
2. UI-focused source-unit processing.
3. Stabilization and compile-oriented review.

## Stage 1: overview and comparison

First inspect the relevant source and target files.

From the source repository, focus on files that reveal visual design, layout, typography, and screen structure, especially:

- source README / SPEC / CODEX_CONTEXT if present;
- app entry and main view;
- home screen implementation;
- review screen implementation;
- theme and typography;
- adaptive layout;
- reusable visual modifiers or components;
- math/content rendering only if it affects visible layout.

From the Android target, inspect:

- HomeScreen;
- ReviewScreen;
- ScopeSelectionScreen;
- ReinforcementScreen;
- MasteredScreen;
- navigation shell;
- theme and color files;
- glass/card/component files;
- ViewModel only where it affects visible displayed values;
- existing Forgis reports and progress files.

Create or update:

- Kikaria-Android/FORGIS_TRANSLATION_PLAN.md
- Kikaria-Android/FORGIS_SOURCE_TARGET_MAP.md
- Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

In this run, the plan and map should explicitly mark this pass as UI/style/layout focused.

Do not start with large code rewrites.

## Stage 2: UI-focused per-file or per-unit refinement

Process UI-related source files or source functional units one at a time.

For each selected source file or unit, follow the staged micro-phases:

1. Feed and understand

Read the selected source unit.

Read the related Android UI/theme/component files.

Identify:

- what screen or visual system the source unit contributes to;
- what the user should see or feel;
- what component hierarchy exists;
- what state affects the visible UI;
- how the current Android version differs;
- whether the mismatch is in style, arrangement, typography, hierarchy, or interaction.

2. Translate, refine, or merge

Modify the Android implementation to better preserve the source visual intent.

Focus on:

- visual hierarchy;
- component arrangement;
- spacing;
- proportions;
- typography;
- color atmosphere;
- glass/translucent feeling;
- card or bubble hierarchy;
- screen rhythm;
- overall premium and minimal study-product feeling.

Do not mechanically translate SwiftUI syntax.

Do not add unrelated features.

Do not rewrite non-UI systems unless required to keep the UI compiling.

3. Read-only comparison

After writing, read the source unit and modified Android files again.

Generate a focused comparison report under:

Kikaria-Android/FORGIS_COMPARE_REPORTS

or append a focused comparison section to:

Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

The comparison should say:

- what visual intent was extracted;
- what Android files were changed;
- what is now closer;
- what still differs;
- what was intentionally deferred.

4. Revision

Use the comparison report to make one targeted revision.

Then update:

- FORGIS_TRANSLATION_PROGRESS.md
- FORGIS_SOURCE_TARGET_MAP.md

Mark the source unit as:

- translated
- partially translated
- needs review
- deferred
- already covered

Only then move to the next source unit.

## UI focus areas

Use your own judgment after reading the source and Android target.

This pass should mainly improve the following kinds of problems:

- the current Android UI may have the right rough structure but weak visual identity;
- component sizes may not match the original rhythm;
- spacing may feel too generic or too compressed;
- cards may not feel glass-like enough;
- bubbles or main action components may have the wrong visual weight;
- typography may not preserve the original serif/bookish feeling;
- numbers and Chinese labels may not feel aligned with the original;
- lower dashboard components may have imperfect hierarchy;
- review screen components may not yet match the original study flow;
- navigation and screen transitions may feel generic;
- the app may still look like a Compose demo instead of Kikaria.

Do not treat this list as a rigid checklist.

Use source inspection and your own comparison to decide the actual changes.

## Screens to consider

Prioritize screens and components that affect product identity.

Likely priority:

1. home screen;
2. review screen;
3. scope selection;
4. reinforcement / important collection;
5. mastered list;
6. shared glass/card/button components;
7. theme, typography, and color system.

Do not spend the whole run on hidden data-layer code unless it affects visible UI.

Do not touch persistence, import/export, Markdown parser, or math rendering unless a visible UI issue requires it.

## Folder-level review

When relevant UI-related files in a source folder or target UI folder have been processed, perform a folder-level review.

For the folder-level review:

- compare the group of source UI files with the group of Android UI files;
- check consistency across screens;
- check shared components and theme usage;
- check whether the Android UI now feels like one product;
- make small consistency fixes if needed;
- record the result in FORGIS_TRANSLATION_PROGRESS.md and FORGIS_SOURCE_TARGET_MAP.md.

Do not silently skip folder-level review.

## Stage 3: stabilization

After UI refinements, perform a build-oriented and consistency-oriented review.

Check:

- Kotlin syntax;
- Compose API usage;
- imports;
- resource references;
- navigation references;
- ViewModel references used by the UI;
- Gradle and manifest only if touched or needed;
- whether the app is still expected to launch.

Make small fixes if needed.

Do not start another broad UI redesign in stabilization.

Do not claim a real build succeeded unless an actual validation command ran.

If no build command was run, say that only static review was performed.

## Design principle

UI migration is not source syntax translation.

For UI, preserve the user's perception of the product:

- visual hierarchy;
- calmness;
- softness;
- premium feeling;
- light blue / translucent atmosphere where appropriate;
- minimal study-focused interface;
- bookish or serif-leaning typography where appropriate;
- clear but not heavy component arrangement.

Use Compose-native implementation.

Do not overcomplicate the screen.

Do not simply add more elements.

Prefer fewer, higher-impact refinements.

## Required output files

Create or update:

- Kikaria-Android/FORGIS_TRANSLATION_PLAN.md
- Kikaria-Android/FORGIS_SOURCE_TARGET_MAP.md
- Kikaria-Android/FORGIS_TRANSLATION_PROGRESS.md

Use this directory for comparison reports:

- Kikaria-Android/FORGIS_COMPARE_REPORTS

Also update if relevant:

- Kikaria-Android/FORGIS_HOME_UI_REPORT.md
- Kikaria-Android/FORGIS_RUN_FIX_REPORT.md
- Kikaria-Android/FORGIS_BUILD_FIX_REPORT.md

Do not create excessive unrelated report files.

## Completion rules

Do not return final_summary before this UI/style/layout-focused staged run has meaningfully completed its current scope.

If max_iterations is reached, record partial progress and next steps.

When finished, return final_summary with:

- source files or units inspected;
- Android UI files inspected;
- UI/style/layout gaps identified;
- source-target map status;
- units processed in this run;
- Android files changed;
- what improved visually;
- what improved in component arrangement;
- what improved in typography/theme/style;
- whether all writes stayed inside Kikaria-Android;
- whether the source repository stayed read-only;
- whether build/run was actually checked or only statically reviewed;
- remaining UI gaps;
- recommended next step.
