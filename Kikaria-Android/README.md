# Kikaria for Android

This is the Android target workspace for **Kikaria** — a local-first memorization assistant originally built for iOS/SwiftUI, now being ported to Android with Kotlin and Jetpack Compose.

All generated output is intentionally contained inside this `Kikaria-Android` directory.

## About Kikaria

Kikaria helps users study structured Markdown knowledge points. The core flow:

1. Browse a preset of knowledge points (math, English vocabulary, custom).
2. Select tags to scope review, or review all.
3. Review mode: see a title → reveal hint → reveal full content.
4. Mark items as "reinforced" (important collection) or "mastered".
5. Review your reinforcement list and mastered list separately.

Kikaria is local-only, does not require an account, network access, or cloud services.

## Opening in Android Studio

1. Open Android Studio.
2. Select **File → Open**.
3. Navigate to `Kikaria-Android/` and open it.
4. Android Studio will sync Gradle automatically.
5. Select a device/emulator and click **Run**.

### Requirements

- Android Studio Hedgehog (2023.1) or newer
- Android SDK 34
- JDK 17
- Kotlin 1.9.22

## First-Pass Implementation

### What's Included

- **Runnable Android project layout** with Gradle Kotlin DSL, Compose, and Material 3.
- **Main Activity** with Jetpack Compose shell.
- **Home screen** with Kikaria branding, bubble start button, date/progress card, and quick-action cards for scope, reinforcement, mastered, and preset.
- **Data models**: `KnowledgePoint`, `KnowledgePreset`, `StudyActivityRecord` — translated from the Swift source models.
- **Markdown parser**: Parses the same `# Title / tags: / hint: / content: / ---` format used by the iOS app.
- **Sample presets**: Built-in "高等数学知识点" (advanced math) and "大学英语 Band 4" (college English vocabulary) presets so the app launches with content.
- **Review flow**:
  - Show knowledge point title and tags.
  - "查看提示" (Show Hint) button → reveals hint.
  - "查看答案" (Show Content) button → reveals full content.
  - Bottom action bar with reinforcement toggle, mastered toggle, and next button.
  - Three review modes: Normal, Reinforcement, Mastered — each with mode-appropriate button layout.
- **Reinforcement screen** (重点集锦): List of items marked for reinforcement, ordered by reinforcement count.
- **Mastered screen** (已掌握): List of mastered items.
- **Scope selection** (范围选择): Tag filter chips to narrow review scope.
- **Kikaria theme**: Adaptive light/dark colors, matching the original's soft sky-blue, mint, and lavender palette with glass-like card surfaces.
- **Liquid glass card components**: Glass-style card/capsule/circle composables preserving the visual identity.

### What's Not Yet Implemented

- **Preset management** (create, edit, delete, import custom `.md` files).
- **Markdown editor** for editing knowledge point content.
- **Today overview** and review history with calendar view.
- **LaTeX/math formula rendering** — the original uses iOS SwiftMath; Android would need a different approach (e.g., jlatexmath or a WebView-based renderer).
- **Widget** — no Android equivalent of the iOS WidgetKit widget.
- **Local notifications** for study reminders.
- **User profile** with avatar.
- **Onboarding flow**.
- **Persistent storage** — currently uses in-memory state only. Future passes should add JSON file persistence or Room database.
- **Swipe gestures** in review (up/down/left/right).
- **Countdown date** and daily goal settings UI.
- **Adaptive layout** for tablets.
- **Dark mode toggle** in settings (system dark mode detection is wired up but no manual toggle).

## Project Structure

```
Kikaria-Android/
├── build.gradle.kts                  # Root build file
├── settings.gradle.kts               # Project settings
├── gradle/wrapper/                   # Gradle wrapper
├── README.md
├── FORGIS_MIGRATION_REPORT.md
└── app/
    ├── build.gradle.kts              # App build file
    └── src/main/
        ├── AndroidManifest.xml
        ├── res/                      # Resources (themes, strings)
        └── java/com/vita0818/kikaria/
            ├── MainActivity.kt       # Entry point
            ├── data/                 # Data models
            │   ├── KnowledgePoint.kt
            │   ├── KnowledgePreset.kt
            │   ├── StudyActivityRecord.kt
            │   └── SamplePresets.kt
            ├── util/
            │   └── MarkdownParser.kt # Knowledge point parser
            ├── viewmodel/
            │   └── KikariaViewModel.kt # Central state
            └── ui/
                ├── theme/            # Kikaria colors & theme
                ├── components/       # Glass card composables
                ├── navigation/       # Nav graph & routes
                ├── home/             # Home screen
                ├── review/           # Review flow
                ├── scope/            # Tag scope selection
                ├── reinforcement/    # Reinforcement list
                └── mastered/         # Mastered list
```

## Dependencies

- Jetpack Compose (BOM 2024.01.00)
- Material 3
- Navigation Compose
- Lifecycle ViewModel Compose
- Gson (for future JSON persistence)

No network, analytics, ads, cloud services, or third-party business SDKs.

## Next Steps

See `FORGIS_MIGRATION_REPORT.md` for a detailed migration report and recommended next tasks.
