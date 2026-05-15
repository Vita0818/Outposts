# Forgis Task

You are running inside Forgis.

This is a focused run-fix pass for the existing Kikaria-Android project.

The latest Android UI refinement result does not run correctly. Your task is to diagnose and repair the current Android project so that it can build, install, and launch again, while preserving the current visual direction as much as possible.

Do not perform a new migration.

Do not redesign the whole app.

Do not add major new features.

Do not rewrite the project from scratch.

Do not modify the source repository.

## Hard safety rule

The source repository must remain read-only.

## Target work area

All edits should stay inside:

Kikaria-Android

Do not modify:

- the source repository
- Outposts repository root
- FORGIS_CONFIG.yml
- FORGIS_TASK.md
- .github/workflows

Do not access secrets.

## Goal

Repair the current Kikaria-Android project after the latest UI refinement pass.

The priority order is:

1. Restore build/sync/run correctness.
2. Preserve the latest improved Kikaria-like home visual direction where possible.
3. Avoid broad rewrites.
4. Avoid new feature work.
5. Leave a clear report of what was broken and what was fixed.

If a visual refinement caused compile or runtime breakage, fix it directly.

If a visual change is too fragile, simplify it enough to make the app run, but do not revert the whole home screen to the old generic skeleton.

## Required reasoning process

Before editing, inspect the current Android project carefully.

You should:

1. Inspect the Gradle configuration.
2. Inspect the Android manifest and resources.
3. Inspect the current home UI implementation.
4. Inspect theme/component files touched by the visual pass.
5. Inspect navigation and MainActivity.
6. Inspect ViewModel state used by the home screen.
7. Identify likely build, sync, install, or launch blockers.
8. Repair them systematically.

Do not only patch one isolated line.

Do not assume the first visible error is the only error.

Try to identify patterns and fix the class of problem.

## Files to inspect

Inspect relevant files under:

Kikaria-Android

including but not limited to:

- settings.gradle.kts
- build.gradle.kts
- app/build.gradle.kts
- gradle.properties
- app/src/main/AndroidManifest.xml
- app/src/main/java
- app/src/main/res
- FORGIS_HOME_UI_REPORT.md
- FORGIS_BUILD_FIX_REPORT.md
- FORGIS_LOG.md if useful

Use the source repository only as a reference for intended behavior and visual direction.

## What to fix

Fix issues that can prevent Android Studio sync, build, install, or app launch, such as:

- Kotlin syntax errors
- invalid Compose API usage
- missing imports
- invalid custom getters or state declarations
- invalid resource references
- missing resources
- invalid Manifest entries
- package or namespace mismatch
- broken navigation references
- broken ViewModel usage
- fragile visual code introduced by the previous UI pass
- Gradle configuration problems
- dependency or AndroidX configuration problems

Keep fixes targeted.

Do not add unrelated dependencies.

Do not introduce network services, analytics, accounts, ads, telemetry, cloud services, or external runtime requirements.

## Visual preservation

The latest home screen direction should be preserved as much as possible.

Do not revert to the old generic Material-style skeleton unless absolutely necessary for launch.

If a UI detail breaks build/runtime, simplify that detail while keeping the overall Kikaria-like direction.

## Report

Create or update:

Kikaria-Android/FORGIS_RUN_FIX_REPORT.md

The report should include:

- files inspected
- likely failure causes identified
- files changed
- fixes made
- whether the home UI direction was preserved
- remaining risks
- suggested next local Android Studio steps

## Completion

When finished, return final_summary with:

- what was inspected
- what was broken
- what was fixed
- which files changed
- whether all writes stayed inside Kikaria-Android
- whether the source repository stayed read-only
- whether the app is expected to build/run
- remaining risks
- recommended next step
