# Forgis Task

You are running inside Forgis.

This is a dry-run-safe validation task for the Kikaria to Android target workspace.

Do not perform a real migration in this run.

## Repositories

The source repository is Kikaria.

The target repository is Outposts.

The target repository root is read-only for generated work. You must not write to the target repository root.

All generated files, reports, experiments, or future migration output must be placed only inside the configured target_subdir:

Kikaria-Android

## Current task

Inspect the authorized source repository and the target workspace structure using only Forgis file tools.

Create a small source overview report inside:

Kikaria-Android/SOURCE_OVERVIEW.md

## Strict rules

- Use only the file tools provided by Forgis.
- You may read the source repository.
- You may read the target repository.
- You may only write inside Kikaria-Android.
- Do not write to the Outposts repository root.
- Do not modify FORGIS_CONFIG.yml.
- Do not modify this task file.
- Do not modify workflow files.
- Do not access secrets.
- Do not assume direct filesystem access.
- Do not perform a real Android migration yet.
- Do not create Android project scaffolding yet.
- Do not create Gradle files yet.
- Do not rewrite source code yet.
- Do not make platform-specific migration decisions beyond observing the source structure.
- Do not write outside Kikaria-Android.

## Required output file

Create:

Kikaria-Android/SOURCE_OVERVIEW.md

The report should contain:

1. A short statement that the source repository is Kikaria.
2. A short statement that the target repository is Outposts.
3. A short statement that all future output must stay inside Kikaria-Android.
4. A concise directory overview of the source repository based only on files and directories actually listed or read.
5. A concise directory overview of the existing Kikaria-Android target_subdir, if it exists.
6. A note confirming that no real migration was performed in this dry-run validation task.
7. A note describing what additional task instructions would be needed before a future real Android migration run.

## Completion

When finished, return a final_summary explaining:

- what source paths you inspected;
- what target paths you inspected;
- what file you wrote;
- whether any operation was blocked by Forgis safety rules;
- whether the task stayed within Kikaria-Android.
