# Forgis Task

You are running inside Forgis.

This is a focused visual refinement pass for the existing Kikaria-Android project.

The current Android app already runs, and the latest home screen direction is broadly correct. However, it is still not close enough to the original Kikaria iOS experience.

Your task is to improve the Android home screen's visual fidelity and product feeling by studying the source project and making your own design/implementation decisions.

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

## Goal

Improve the Android home screen so it feels more like Kikaria, not like a generic Android/Compose imitation.

The goal is not pixel-perfect copying.

The goal is source-guided visual translation: understand the iOS design intent, compare it with the current Android result, identify the most important remaining gaps, and refine the Android implementation accordingly.

## Required reasoning process

Before editing, you must inspect both sides:

1. Inspect the relevant Kikaria iOS source and design/context files.
2. Inspect the current Kikaria-Android home screen and related styling files.
3. Compare the two implementations.
4. Identify the most important visual gaps.
5. Decide your own minimal but meaningful refinement plan.
6. Implement the refinement.
7. Keep the app runnable.

Do not rely only on the task text.

Use the source repository as the primary reference.

Use the current Android implementation as the starting point.

## What to optimize

Use your own judgment after source inspection.

Focus on the home screen's overall visual fidelity, including composition, hierarchy, rhythm, typography, color, softness, atmosphere, and product identity.

Do not make broad feature changes.

Do not change review flow, data model, Markdown parsing, persistence, or other non-home areas unless required to keep the app compiling.

## Important constraint

Do not simply add more UI elements.

Do not overcomplicate the screen.

Kikaria's visual direction should remain calm, minimal, soft, premium, and study-focused.

Prefer fewer, higher-impact changes over many scattered tweaks.

## Implementation freedom

You may choose which Android files to edit.

You may adjust theme, reusable components, and home screen implementation if needed.

You may create small helper composables if they clearly improve the implementation.

You may use Compose-native drawing and layout techniques.

Do not introduce heavy dependencies.

Do not make fragile Gradle or dependency changes unless absolutely necessary.

## Report

Create or update:

Kikaria-Android/FORGIS_HOME_UI_REPORT.md

Add a new section for this pass.

The report should include:

- what source files you inspected;
- what Android files you inspected;
- the visual gaps you identified;
- the refinement strategy you chose;
- files changed;
- remaining gaps;
- recommended next pass.

## Completion

When finished, return final_summary with:

- source files inspected;
- Android files inspected;
- main visual gaps identified;
- changes made;
- whether all writes stayed inside Kikaria-Android;
- whether the source repository stayed read-only;
- remaining risks;
- recommended next step.
