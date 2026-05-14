# Kikaria HarmonyOS

This is the HarmonyOS target workspace for **Kikaria** — a local-first memorization assistant originally built for iOS (SwiftUI).

Generated output is intentionally contained inside `Kikaria-HarmonyOS` within the Outposts repository.

## About Kikaria

Kikaria helps users import structured Markdown study materials, randomly review knowledge points by tag, reveal hints or full content when needed, and collect weak points into a reinforcement (important) list.

This HarmonyOS version is a **first-pass port** of the iOS app, preserving the core product direction, data concepts, and user experience.

## Platform

- HarmonyOS (API 12 / 5.0.0)
- ArkTS + ArkUI
- Stage model
- Local-only (no network, no accounts, no cloud)
- Hvigor build system

## How to Open in DevEco Studio

1. Install DevEco Studio (5.0+ recommended).
2. Open the `Kikaria-HarmonyOS` directory as a project.
3. Wait for dependency resolution (Hvigor).
4. Connect a HarmonyOS device or emulator.
5. Run the `entry` module.

## What Was Implemented (First Pass)

1. **Runnable HarmonyOS project layout** — including `oh-package.json5`, `build-profile.json5`, `hvigorfile.ts`, `AppScope/app.json5`, `entry` module with Stage model configuration.

2. **EntryAbility** — Standard Stage model `UIAbility` that loads the Index page with the Kikaria page background color.

3. **Home Screen (Index)** — ArkUI-based home screen with:
   - Kikaria branding with serif typography
   - Mastered count / total count bubble
   - Stats dashboard cards (tags, important, mastered)
   - Tag filter chips
   - Quick launch buttons for review, reinforcement review, and mastered browsing

4. **Review Flow (ReviewPage)** — Core memorization review:
   - Shows knowledge point title
   - "Show Hint" button to reveal hint
   - "Show Answer" button to reveal full content
   - "Add to Important" / "Remove from Important" toggle
   - "Mark as Mastered" / "Mark as Not Mastered" toggle
   - "Next" navigation through shuffled queue
   - Empty state handling

5. **Reinforcement Page (ReinforcementPage)** — Shows all items marked as important:
   - Expandable cards with hint and content
   - "Remove from Important" action
   - "Review All" quick action

6. **Mastered Page (MasteredPage)** — Shows all mastered items:
   - Expandable cards with green checkmark
   - "Mark as Not Mastered" action
   - "Browse All" quick action

7. **Data Models (ArkTS)** — Core data structures translated from Swift:
   - `KnowledgePoint` — with reinforcement tracking, mastered state, timestamps
   - `KnowledgePreset` — preset collections with Markdown text
   - `ReviewMode` — normal, reinforcement, mastered modes
   - `StudyActivityType` and `StudyActivityRecord`
   - Markdown parser (`parseMarkdown`) preserving the source's chunk-based parsing logic

8. **Sample Preset** — Built-in "Advanced Mathematics" preset with 6 sample knowledge points (limits, derivatives, matrices, probability), matching the source's `defaultMarkdownText`.

9. **Visual Style** — Soft, study-focused feel with:
   - Light page backgrounds (mist/blue-white)
   - Rounded card components with subtle shadows
   - Serif font family for headings and key numbers
   - Color palette derived from the source's KikariaTheme (sky, cyan, mastered green, amber, coral)

10. **Local State Management** — Singleton `AppState` class managing all knowledge points, presets, review queues, tag selection, and activity records in memory.

## What Remains for Future Migration Passes

- **Persistent storage** — Current state is in-memory only. Need to implement `@ohos.data.preferences` or relational store for saving presets, knowledge points, and study records.
- **Markdown import** — File picker and import flow for external `.md` files (the source supports document picker).
- **Multiple preset management** — Creating, editing, deleting presets; the source has full preset library management.
- **Dashboard / Today Overview** — Daily review counts, progress tracking, countdown to exam dates.
- **LaTeX math rendering** — The source uses `SwiftMath` for inline and block LaTeX formulas. A HarmonyOS equivalent or custom canvas-based math renderer is needed.
- **Widget** — The source has an iOS widget showing daily progress. HarmonyOS has FormAbility/ServiceWidget for this.
- **Notifications** — Study progress reminders. HarmonyOS has NotificationManager.
- **Dark mode** — The source has full light/dark adaptive colors. Need to implement using HarmonyOS `darkColorMode`.
- **iPad / tablet layout** — The source has sophisticated adaptive layout (two-column for wide screens). A responsive layout for HarmonyOS tablets would be valuable.
- **Profile / onboarding** — User profile display name, avatar, onboarding flow.
- **Data export** — Exporting knowledge points back to Markdown.
- **Reinforcement count tracking** — The source tracks repeated reinforcement (multiple "add to important" cycles). Currently simplified to boolean toggle.
