# Forgis Task

This is a mock-safe first task for validating the Forgis configuration and file-tool workflow.

Do not perform a real platform migration.

Your goal is to inspect the authorized source repository and create a small report inside the target repository's configured target_subdir.

## Rules

- You must use only the file tools provided by Forgis.
- You may read the source repository.
- You may read the target repository.
- You may only write inside the configured target_subdir.
- Do not modify FORGIS_CONFIG.yml.
- Do not modify this task file.
- Do not write to the target repository root.
- Do not write workflow files.
- Do not access secrets.
- Do not assume any platform stack.
- Do not add scaffold code.
- Do not perform Android, iOS, Web, Gradle, npm, Cargo, Rust, Python, Swift, Kotlin, or other platform-specific migration logic unless a future task explicitly requests it.

## Required output

Create the following file inside target_subdir:

SOURCE_OVERVIEW.md

The file should contain:

1. The source repository name.
2. The source ref.
3. A short directory overview based only on files you actually read or listed.
4. A short note confirming that no real migration was performed.
5. A short note explaining what additional task instructions would be needed for a future real run.

## Completion

When finished, return a final summary explaining:

- what you inspected;
- what you wrote;
- which files were changed;
- whether any limits or safety restrictions prevented further work.
