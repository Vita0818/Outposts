# Forgis Task

You are running inside Forgis.

This is a real first-pass migration task from the Kikaria source repository into the Outposts target repository.

The source repository is:

Vita0818/Kikaria

The target repository is:

Vita0818/Outposts

The target repository root must be treated as read-only for generated work.

All generated migration output must be written only inside the configured target_subdir:

Kikaria-HarmonyOS

If Kikaria-HarmonyOS does not exist, create it.

Do not write to the Outposts repository root.

Do not modify FORGIS_CONFIG.yml.

Do not modify this task file.

Do not modify workflow files.

Do not write outside Kikaria-HarmonyOS.

Do not modify the source repository.

Do not access secrets.

Use only the file tools provided by Forgis.

You do not have direct filesystem access.

You must inspect the source repository through Forgis file tools before writing target files.

## Goal

Create the first runnable HarmonyOS version of Kikaria inside:

Kikaria-HarmonyOS

This should be a real HarmonyOS project skeleton and first-pass port, not a dry-run report.

The goal is not to fully complete every advanced feature in one run. The goal is to produce a coherent HarmonyOS project that preserves Kikaria's product direction, source structure understanding, data concepts, and core user experience enough for future iterations.

## Required target structure

Create or update files only under Kikaria-HarmonyOS.

At minimum, create:

- Kikaria-HarmonyOS/oh-package.json5
- Kikaria-HarmonyOS/build-profile.json5
- Kikaria-HarmonyOS/hvigorfile.ts
- Kikaria-HarmonyOS/AppScope/app.json5
- Kikaria-HarmonyOS/entry/oh-package.json5
- Kikaria-HarmonyOS/entry/build-profile.json5
- Kikaria-HarmonyOS/entry/hvigorfile.ts
- Kikaria-HarmonyOS/entry/src/main/module.json5
- Kikaria-HarmonyOS/entry/src/main/ets/entryability/EntryAbility.ets
- Kikaria-HarmonyOS/entry/src/main/ets/pages/Index.ets
- Kikaria-HarmonyOS/entry/src/main/ets/...
- Kikaria-HarmonyOS/entry/src/main/resources/...
- Kikaria-HarmonyOS/README.md
- Kikaria-HarmonyOS/FORGIS_MIGRATION_REPORT.md

Use ArkTS for HarmonyOS code.

Prefer ArkUI for UI unless the source inspection strongly suggests another HarmonyOS-native approach.

Use a HarmonyOS Stage model project structure.

Use Hvigor / JSON5 project configuration appropriate for a first-pass HarmonyOS project.

Do not create files in the target repo root.

## Source inspection requirements

Before writing HarmonyOS files, inspect the Kikaria source repository.

You should list and read enough source files to understand:

- app entry structure;
- main UI structure;
- review / memorization flow;
- preset / knowledge item data model;
- important collection or mastered-list logic;
- typography and visual design direction;
- Markdown or content parsing direction if present;
- platform-specific SwiftUI pieces that need HarmonyOS / ArkUI equivalents.

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

## HarmonyOS implementation expectations

Build a first-pass HarmonyOS app under Kikaria-HarmonyOS that includes:

1. A runnable HarmonyOS project layout.

2. A main EntryAbility.

3. An ArkUI-based application shell.

4. A home screen reflecting Kikaria's memorization product direction.

5. Basic ArkTS data models for knowledge items, presets, important collection, mastered items, and review state, based on the source repository inspection.

6. A simple local sample preset so the app can launch and display content without network access.

7. A basic review flow:
   - show a knowledge item title or prompt;
   - allow showing hint;
   - allow showing answer;
   - allow adding to important collection;
   - allow marking as mastered;
   - move through items.

8. Basic local state management suitable for a first HarmonyOS port.

9. A visual style that attempts to preserve Kikaria's clean, soft, study-focused feel.

10. Clear TODO comments only where future work is genuinely needed.

Avoid overengineering.

Avoid huge generated files.

Avoid unrelated architecture.

## Dependencies

Use standard HarmonyOS / ArkTS / ArkUI dependencies when needed.

Keep dependencies minimal.

Do not introduce heavyweight or unrelated libraries.

Do not require external services.

Do not require secrets.

Do not require network access at runtime.

## README requirements

Create Kikaria-HarmonyOS/README.md.

It should explain:

- this is the HarmonyOS target workspace for Kikaria;
- generated output is intentionally contained inside Kikaria-HarmonyOS;
- how to open the project in DevEco Studio;
- what was implemented in this first pass;
- what remains for future migration passes.

Do not mention Aider.

Do not mention hidden internal model reasoning.

Do not include secrets.

## Migration report requirements

Create Kikaria-HarmonyOS/FORGIS_MIGRATION_REPORT.md.

It should include:

- source paths inspected;
- target files created or updated;
- major source concepts identified;
- how those concepts were mapped into HarmonyOS / ArkTS / ArkUI;
- known gaps;
- recommended next migration tasks.

The report must not include secret values.

The report must not include huge copied source passages.

## Safety and boundary rules

You may read the source repository.

You may read the target repository.

You may write only inside Kikaria-HarmonyOS.

If Kikaria-HarmonyOS does not exist, create it before writing generated migration output.

You must not write to:

- Outposts repository root;
- FORGIS_CONFIG.yml;
- FORGIS_TASK.md;
- .github/workflows;
- any path outside Kikaria-HarmonyOS;
- the source repository.

You must not delete files outside Kikaria-HarmonyOS.

You must not access secrets.

You must not print or write environment variable values.

## Completion

When finished, return final_summary.

The final_summary should include:

- source files and directories inspected;
- HarmonyOS files created or updated;
- whether all writes stayed inside Kikaria-HarmonyOS;
- whether any Forgis safety rule blocked an operation;
- whether the first HarmonyOS project skeleton was produced;
- known limitations;
- recommended next task for a second migration pass.
