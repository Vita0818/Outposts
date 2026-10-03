# Kikaria Android / Apple Gap Plan

Date: 2026-06-25
Mode: Spark
Scope: Source-level comparison of Kikaria-Apple and Kikaria-Android. No business source changes were made for this plan.

## Current Status

Android home and main review layout have been brought closer to the Apple structure in the previous pass, especially compact phone centering, review content centering, bottom action sizing, and empty-state centering.

This plan records remaining gaps found by read-only comparison. Visual acceptance is still open because no Android actual screenshot and no Qwen visual compare were completed.

## Implementation Update - 2026-06-25

Items 1 through 9 have now been implemented in `Kikaria-Android` source. Remaining validation work is visual only: Android actual screenshots and Qwen visual comparison are still not completed.

Serif usage has been narrowed: Kikaria brand title, Latin mixed text, and numeric identity keep the existing serif treatment; Chinese avatar initials and long-form knowledge editing fields use the Android system default font to avoid unstable CJK serif fallback.

## P0 - Data / Behavior Parity

1. Per-preset study state is not aligned with Apple.
   - Android keeps one global `knowledgePoints`, `selectedTags`, daily counts, and activity list for the active preset.
   - Apple keeps `presetStates: [String: PresetStudyState]`, including knowledge points, selected tags, daily review records, activity records, daily goal, countdown, notifications, time, and danger percent per preset.
   - Impact: switching presets can lose or mix study progress, selected scope, daily goal/countdown/notification state, reinforcement, mastered, and history behavior.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt:43`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/util/KikariaPersistence.kt:14`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:790`
     - `Kikaria-Apple/Kikaria/ContentView.swift:920`
     - `Kikaria-Apple/Kikaria/ContentView.swift:2002`

2. Review card's per-point daily review count is wrong.
   - Android displays global `todayReviewCount` in `该知识点今日复习 N 次`.
   - Apple uses `dailyReviewRecords[pointID]` and increments per point when answer content is revealed.
   - Impact: every card can show the same total count instead of the current point's count.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/review/ReviewScreen.kt:1147`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt:416`
   - Apple reference:
     - `Kikaria-Apple/Kikaria/ContentView.swift:8713`

3. Scope search is incomplete.
   - Android standalone scope search says `搜索标签或知识点`, but filters only tag text.
   - Apple searches title, tags, hint, and content, then includes relevant tags.
   - In-review Android scope panel searches title/tags only, still missing hint/content.
   - Impact: user searches by knowledge point answer or hint and cannot find the expected tag.
   - Android reference:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/scope/ScopeSelectionScreen.kt:77`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:7161`
     - `Kikaria-Apple/Kikaria/ContentView.swift:2772`

## P1 - Layout / Interaction Parity

4. Reinforcement and mastered collection pages still use a simpler layout.
   - Android places the start button near the top of the scroll content.
   - Apple phone layout keeps the list scrollable and pins the start button in a bottom material action area.
   - Android lacks collection search, filtered empty state, and two-column landscape collection layout.
   - Android reinforcement cards show at most three tags; mastered cards do not show tags.
   - Impact: important button placement and list behavior still differ from Apple, especially on long lists.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/reinforcement/ReinforcementScreen.kt:91`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/mastered/MasteredScreen.kt:90`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:9618`
     - `Kikaria-Apple/Kikaria/ContentView.swift:9788`
     - `Kikaria-Apple/Kikaria/ContentView.swift:9819`
     - `Kikaria-Apple/Kikaria/ContentView.swift:10064`

5. Preset editing is functionally incomplete.
   - Android edit preset page is a raw Markdown editor plus parsed preview.
   - Apple edit preset page supports export Markdown, add knowledge point, search knowledge points, edit/delete individual knowledge points, delete preset, and confirmation dialogs.
   - Android navigation has no `editKnowledgePoint` route.
   - Impact: Android cannot match Apple preset management workflow or its button layout density.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/preset/EditPresetScreen.kt:42`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/navigation/KikariaNavGraph.kt:45`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:5776`
     - `Kikaria-Apple/Kikaria/ContentView.swift:5786`
     - `Kikaria-Apple/Kikaria/ContentView.swift:5878`
     - `Kikaria-Apple/Kikaria/ContentView.swift:5951`

6. Profile avatar flow is not fully connected.
   - Android edit profile can choose an image URI.
   - Android settings and home still render a generated avatar from display name instead of the selected avatar.
   - Android initial profile setup has no avatar picker.
   - Apple stores `avatarImageData` and supports avatar selection in initial setup and edit profile.
   - Impact: visible profile/avatar parity is incomplete on home and settings.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/SettingsScreen.kt:92`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/profile/ProfileSetupScreen.kt:53`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/settings/EditProfileScreen.kt:53`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:550`
     - `Kikaria-Apple/Kikaria/ContentView.swift:6150`
     - `Kikaria-Apple/Kikaria/ContentView.swift:6420`

7. Mastered action button state differs from Apple.
   - Android shows `已设定为掌握` for an already mastered point but leaves the button enabled.
   - Apple disables that button and lowers opacity.
   - Impact: button state and affordance are not Apple-parity, though behavior does not appear to re-mark successfully.
   - Android reference:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/review/ReviewScreen.kt:1277`
   - Apple reference:
     - `Kikaria-Apple/Kikaria/ContentView.swift:9328`

## P2 - Platform Features

8. Notification behavior does not match Apple.
   - Android declares `POST_NOTIFICATIONS` and silently returns if permission is missing.
   - Android does not request runtime notification permission.
   - Android notification text is fixed daily reminder text.
   - Apple requests permission and schedules progress-warning notifications only when countdown/progress/danger-percent evaluation requires it.
   - Impact: settings notification toggle can appear enabled while no notification can be scheduled; reminder logic differs from Apple.
   - Android references:
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/MainActivity.kt:23`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/util/KikariaNotificationManager.kt:37`
     - `Kikaria-Android/app/src/main/java/com/vita0818/kikaria/util/KikariaNotificationManager.kt:83`
   - Apple references:
     - `Kikaria-Apple/Kikaria/ContentView.swift:1013`
     - `Kikaria-Apple/Kikaria/ContentView.swift:1047`
     - `Kikaria-Apple/Kikaria/ContentView.swift:1149`
     - `Kikaria-Apple/Kikaria/ContentView.swift:2068`

9. Widget equivalent is missing on Android.
   - Apple writes `WidgetSnapshot` and has `KikariaWidgetProvider`.
   - Android main source/resource tree has no app widget provider or appwidget metadata.
   - Impact: platform feature parity is incomplete if Android is expected to provide a home-screen widget.
   - Apple references:
     - `Kikaria-Apple/Kikaria/StudyTracking.swift:50`
     - `Kikaria-Apple/Kikaria/ContentView.swift:2602`
     - `Kikaria-Apple/KikariaWidget/KikariaWidget.swift:153`

## Validation Gaps

1. No Android device/emulator was available during the previous verification pass.
2. No actual Android screenshots were produced.
3. No Qwen visual compare report was produced.
4. Existing command validation from the previous implementation pass:
   - `./gradlew test`: passed
   - `./gradlew assembleDebug`: passed
   - `git diff --check`: passed

## Recommended Fix Order

1. Fix per-preset state model and persistence first.
2. Fix per-point daily review counts and review-card display.
3. Fix scope search parity.
4. Rework reinforcement/mastered collection pages: search, filtered state, bottom-pinned start button, landscape grid, tag display.
5. Add Apple-style preset editor workflow and `editKnowledgePoint` route.
6. Wire avatar URI/image through home/settings/profile setup.
7. Align mastered button disabled state.
8. Decide Android notification/widget parity scope, then implement or explicitly mark as platform-deferred.
9. Run tests/build and complete screenshot/Qwen visual comparison.
