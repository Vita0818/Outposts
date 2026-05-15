# Forgis Task

You are running inside Forgis.

This is a focused visual-fidelity pass for the existing Kikaria-Android project.

The Android project already runs, but the current home screen looks like a generic Android skeleton instead of Kikaria.

Your task is to make the Android home screen meaningfully closer to the original Kikaria iOS home screen.

Do not perform a new migration.

Do not rewrite the whole project.

Do not add unrelated features.

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

## High-level goal

Improve the Android home screen so that it better preserves Kikaria's original identity, layout logic, visual rhythm, and premium study-product feeling.

This is not a generic Material Design redesign.

This is a source-guided visual translation from Kikaria iOS to Kikaria Android.

## Required reasoning process

Before editing, inspect the relevant iOS source and current Android implementation.

You should first understand:

1. What the Kikaria iOS home screen is trying to communicate.
2. What its visual hierarchy is.
3. What design primitives it relies on.
4. What typography and spacing logic it uses.
5. What the current Android version gets wrong.
6. Which changes would produce the largest fidelity improvement with the least risk.

Do not blindly copy SwiftUI code.

Translate the design intent into idiomatic Jetpack Compose.

## Source inspection

Use the Kikaria source repository as the design reference.

Inspect relevant source files such as:

- iOS app entry and main view files
- home screen implementation
- typography system
- adaptive layout helpers
- theme/color definitions
- product specification or design notes if present

Use targeted reads.

Do not dump the whole source repository.

## Android inspection

Inspect the current Android implementation, especially:

- home screen
- theme/colors
- reusable glass or card components
- navigation shell
- ViewModel state used by the home screen

Understand the current implementation before changing it.

## What to improve

Make the Android home screen substantially closer to the iOS Kikaria home screen.

You decide the exact implementation details after inspecting the source.

Focus on visual fidelity and product identity, including but not limited to:

- overall composition
- background treatment
- title and profile/avatar area
- primary action area
- dashboard/progress information
- preset/status information
- typography
- color palette
- glass/translucent feeling
- spacing and proportions
- minimalism

Do not overfit to one screenshot.

Use the source project itself as the stronger reference.

## Constraints

Keep the app runnable.

Keep changes focused on the home screen and directly related theme/component files.

Do not break review flow, navigation, data models, or existing sample presets.

Do not add network services, analytics, accounts, ads, cloud dependencies, or unrelated libraries.

Do not introduce heavy dependencies.

Prefer Compose-native implementation.

Avoid fragile APIs that may break the current Gradle/Compose setup.

## Expected output

Modify the Android project so that the home screen is visibly more Kikaria-like.

Create or update:

Kikaria-Android/FORGIS_HOME_UI_REPORT.md

The report should include:

- iOS source files inspected
- Android files inspected
- visual gaps identified
- Android files changed
- design decisions made
- remaining visual gaps
- suggested next UI pass

## Completion

When finished, return final_summary with:

- what source files were inspected
- what Android files were inspected
- what visual gaps were identified
- what changes were made
- whether all writes stayed inside Kikaria-Android
- whether the source repository stayed read-only
- remaining risks
- recommended next step
