# Forgis Task

You are running inside Forgis.

This is a focused build-fix pass for the existing Kikaria-Android project in the Outposts target repository.

Do not perform a new migration.

Do not redesign the app.

Do not add major new features.

Do not rewrite the whole project.

The current Android project already exists at:

Kikaria-Android

The source repository is Kikaria and must remain read-only.

The target repository is Outposts.

## Goal

Fix the existing Kikaria-Android project so that it is much more likely to sync and compile in Android Studio.

Focus on build correctness, Gradle configuration, Android resources, Kotlin syntax, Compose API compatibility, package consistency, manifest correctness, and obvious compile errors.

This pass should produce a practical build-fix PR.

## Hard safety rule

The source repository must not be modified.

## Target work area

All edits should be made inside:

Kikaria-Android

Do not modify the source repository.

Do not access secrets.

Do not print secrets.

Do not add analytics, ads, network services, accounts, cloud services, or unrelated features.

## Known current errors to fix

The user attempted to open and run the generated Android project locally and encountered these errors:

1. AndroidX / Compose dependency check failed because the project was missing gradle.properties with AndroidX enabled.

Fix expected:

- Create or update Kikaria-Android/gradle.properties
- Ensure it includes android.useAndroidX=true
- Ensure Kotlin style and reasonable JVM args are present

2. AndroidManifest.xml referenced a missing launcher icon:

- resource mipmap/ic_launcher not found

Fix expected:

- Create valid launcher icon resources or change the manifest to reference existing valid resources
- Include both normal and round launcher icons if the manifest references both

3. Compose runtime delegate error:

- Type MutableState<Boolean> has no method getValue
- This usually means Kotlin files using by remember { mutableStateOf(...) } are missing:
  import androidx.compose.runtime.getValue
  import androidx.compose.runtime.setValue

Fix expected:

- Scan all Kotlin files and add the correct Compose runtime delegate imports where needed
- Do not add duplicate imports

4. Material3 LinearProgressIndicator API mismatch:

- LinearProgressIndicator(progress = { ... }) failed
- Current dependency expects progress = Float, not lambda

Fix expected:

- Replace LinearProgressIndicator(progress = { expression }) with LinearProgressIndicator(progress = expression)
- Also check CircularProgressIndicator for the same issue
- Apply this across the whole project, not just one file

5. ViewModel property initialization errors appeared in KikariaViewModel.kt:

- Property must be initialized
- Initializer is not allowed here because this property has no backing field

The user tried a local script that incorrectly changed custom getter properties like:

val activePreset: KnowledgePreset? = null
    get() = ...

and:

var allTags: List<String> = emptyList()
    get() = ...

Fix expected:

- Repair any property with a custom get() so it does not have an initializer
- Use val instead of var where appropriate for computed properties
- Example desired shape:
  val activePreset: KnowledgePreset?
      get() = presets.find { it.id == activePresetId }

  val allTags: List<String>
      get() = knowledgePoints.flatMap { it.tags }.distinct().sorted()

6. There may be more similar Kotlin or Compose compile errors.

Fix expected:

- Inspect all Kotlin files under Kikaria-Android/app/src/main/java
- Fix obvious compile-time problems systematically
- Keep changes minimal and targeted

## Files to inspect first

Before editing, inspect:

- Kikaria-Android/settings.gradle.kts
- Kikaria-Android/build.gradle.kts
- Kikaria-Android/app/build.gradle.kts
- Kikaria-Android/gradle.properties if it exists
- Kikaria-Android/app/src/main/AndroidManifest.xml
- Kikaria-Android/app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt
- Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/review/ReviewScreen.kt
- Kikaria-Android/app/src/main/java/com/vita0818/kikaria/ui/reinforcement/ReinforcementScreen.kt
- All Kotlin files under Kikaria-Android/app/src/main/java
- Resource files under Kikaria-Android/app/src/main/res

Use the Kikaria source repository only as reference if needed.

## What to fix

Fix issues such as:

- missing gradle.properties
- AndroidX not enabled
- missing launcher icon resources
- invalid manifest resource references
- missing Compose runtime imports
- invalid Material3 progress indicator calls
- invalid custom getter property initializers
- uninitialized properties
- wrong val/var usage for computed properties
- missing imports
- invalid package references
- Kotlin syntax errors
- obvious Compose API misuse
- invalid resource references
- inconsistent package names
- missing namespace or SDK config
- missing MainActivity registration

Do not make broad product changes.

Do not redesign UI.

Do not replace the whole project.

Do not delete the current project and regenerate it from scratch.

## Desired result

The project should be in a cleaner state for Android Studio Gradle Sync and app launch.

The output does not need to be perfect or feature-complete.

The priority is to fix the current build/sync/run blockers.

## Report

Create or update:

Kikaria-Android/FORGIS_BUILD_FIX_REPORT.md

The report should include:

- files inspected
- files changed
- build errors fixed
- remaining risks
- suggested next local Android Studio steps

## Completion

When finished, return final_summary with:

- what was inspected
- what was fixed
- which files changed
- whether all writes stayed in Kikaria-Android
- whether the source repository stayed read-only
- remaining likely build risks
