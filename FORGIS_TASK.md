# Forgis Task

You are running inside Forgis.

This is a real first-pass migration task from the Kikaria source repository into the Outposts target repository.

The source repository is:

Vita0818/Kikaria

The target repository is:

Vita0818/Outposts

The target repository root must be treated as read-only for generated work.

All generated migration output must be written only inside the configured target_subdir:

Kikaria-Android

Do not write to the Outposts repository root.

Do not modify FORGIS_CONFIG.yml.

Do not modify this task file.

Do not modify workflow files.

Do not write outside Kikaria-Android.

Do not modify the source repository.

Do not access secrets.

Use only the file tools provided by Forgis.

You do not have direct filesystem access.

You must inspect the source repository through Forgis file tools before writing target files.

## Goal

Create the first runnable Android version of Kikaria inside:

Kikaria-Android

This should be a real Android project skeleton and first-pass port, not a dry-run report.

The goal is not to fully complete every advanced feature in one run. The goal is to produce a coherent Android project that preserves Kikaria's product direction, source structure understanding, data concepts, and core user experience enough for future iterations.

## Required target structure

Create or update files only under Kikaria-Android.

At minimum, create:

- Kikaria-Android/settings.gradle.kts
- Kikaria-Android/build.gradle.kts
- Kikaria-Android/app/build.gradle.kts
- Kikaria-Android/app/src/main/AndroidManifest.xml
- Kikaria-Android/app/src/main/java/...
- Kikaria-Android/app/src/main/res/...
- Kikaria-Android/README.md
- Kikaria-Android/FORGIS_MIGRATION_REPORT.md

Use Kotlin for Android code.

Prefer Jetpack Compose for UI unless the source inspection strongly suggests another Android-native approach.

Use Gradle Kotlin DSL.

Do not create files in the target repo root.

## Source inspection requirements

Before writing Android files, inspect the Kikaria source repository.

You should list and read enough source files to understand:

- app entry structure;
- main UI structure;
- review / memorization flow;
- preset / knowledge item data model;
- important collection or mastered-list logic;
- typography and visual design direction;
- Markdown or content parsing direction if present;
- platform-specific SwiftUI pieces that need Android equivalents.

Do not dump the whole source repository into memory.

Use directory listing and targeted file reads.

Large files must be read in pages when needed.

## Migration approach

Use a translation-first approach.

Preserve source behavior and structure where practical.

Do not redesign the product from scratch.

Do not invent unrelated features.

Do not add cloud services.

Do not add accounts, analytics, telemetry, ads, or network dependencies.

Do not add external business names.

Do not add placeholder references to unrelated products.

## Android implementation expectations

Build a first-pass Android app under Kikaria-Android that includes:

1. A runnable Android project layout.

2. A main activity.

3. A Compose-based application shell.

4. A home screen reflecting Kikaria's memorization product direction.

5. Basic data models for knowledge items, presets, important collection, mastered items, and review state, based on the source repository inspection.

6. A simple local sample preset so the app can launch and display content without network access.

7. A basic review flow:
   - show a knowledge item title or prompt;
   - allow showing hint;
   - allow showing answer;
   - allow adding to important collection;
   - allow marking as mastered;
   - move through items.

8. Basic local state management suitable for a first Android port.

9. A visual style that attempts to preserve Kikaria's clean, soft, study-focused feel.

10. Clear TODO comments only where future work is genuinely needed.

Avoid overengineering.

Avoid huge generated files.

Avoid unrelated architecture.

## Dependencies

Use standard Android / Kotlin / Compose dependencies when needed.

Keep dependencies minimal.

Do not introduce heavyweight or unrelated libraries.

Do not require external services.

Do not require secrets.

Do not require network access at runtime.

## README requirements

Create Kikaria-Android/README.md.

It should explain:

- this is the Android target workspace for Kikaria;
- generated output is intentionally contained inside Kikaria-Android;
- how to open the project in Android Studio;
- what was implemented in this first pass;
- what remains for future migration passes.

Do not mention Aider.

Do not mention hidden internal model reasoning.

Do not include secrets.

## Migration report requirements

Create Kikaria-Android/FORGIS_MIGRATION_REPORT.md.

It should include:

- source paths inspected;
- target files created or updated;
- major source concepts identified;
- how those concepts were mapped into Android/Kotlin/Compose;
- known gaps;
- recommended next migration tasks.

The report must not include secret values.

The report must not include huge copied source passages.

## Safety and boundary rules

You may read the source repository.

You may read the target repository.

You may write only inside Kikaria-Android.

You must not write to:

- Outposts repository root;
- FORGIS_CONFIG.yml;
- FORGIS_TASK.md;
- .github/workflows;
- any path outside Kikaria-Android;
- the source repository.

You must not delete files outside Kikaria-Android.

You must not access secrets.

You must not print or write environment variable values.

## Completion

When finished, return final_summary.

The final_summary should include:

- source files and directories inspected;
- Android files created or updated;
- whether all writes stayed inside Kikaria-Android;
- whether any Forgis safety rule blocked an operation;
- whether the first Android project skeleton was produced;
- known limitations;
- recommended next task for a second migration pass.
