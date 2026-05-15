# FORGIS Run Fix Report

## Date

2026-05-15

## Task

Diagnose and repair the current Kikaria-Android project after the latest UI refinement pass (Pass 2 — Visual Fidelity Refinement). Priority: restore build/sync/run correctness while preserving the Kikaria-like home visual direction.

## Files Inspected

### Gradle / Build Configuration
- `settings.gradle.kts` — root project name, repositories, includes
- `build.gradle.kts` (root) — AGP 8.2.2, Kotlin 1.9.22
- `app/build.gradle.kts` — SDK 34, Compose BOM 2024.02.00, dependencies
- `gradle.properties` — AndroidX, JVM args
- `gradle/wrapper/gradle-wrapper.properties` — Gradle 8.5
- `app/proguard-rules.pro` — minimal rules

### Android Resources
- `app/src/main/AndroidManifest.xml` — MainActivity, launcher intent, theme
- `app/src/main/res/values/themes.xml` — light theme
- `app/src/main/res/values-night/themes.xml` — dark theme
- `app/src/main/res/values/strings.xml` — app name
- `app/src/main/res/values/colors.xml` — launcher background color
- `app/src/main/res/mipmap-anydpi-v26/ic_launcher.xml` — adaptive icon
- `app/src/main/res/mipmap-anydpi-v26/ic_launcher_round.xml` — round adaptive icon
- `app/src/main/res/drawable/ic_launcher_foreground.xml` — vector foreground

### Kotlin Source Files
- `app/src/main/java/com/vita0818/kikaria/MainActivity.kt`
- `app/src/main/java/com/vita0818/kikaria/data/KnowledgePoint.kt`
- `app/src/main/java/com/vita0818/kikaria/data/KnowledgePreset.kt`
- `app/src/main/java/com/vita0818/kikaria/data/SamplePresets.kt`
- `app/src/main/java/com/vita0818/kikaria/data/StudyActivityRecord.kt`
- `app/src/main/java/com/vita0818/kikaria/util/MarkdownParser.kt`
- `app/src/main/java/com/vita0818/kikaria/viewmodel/KikariaViewModel.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/components/GlassComponents.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/home/HomeScreen.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/mastered/MasteredScreen.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/navigation/KikariaNavGraph.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/reinforcement/ReinforcementScreen.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/review/ReviewScreen.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/scope/ScopeSelectionScreen.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/theme/KikariaColors.kt`
- `app/src/main/java/com/vita0818/kikaria/ui/theme/KikariaTheme.kt`

### Documentation
- `FORGIS_HOME_UI_REPORT.md` — previous UI refinement documentation
- `FORGIS_BUILD_FIX_REPORT.md` — previous build fix documentation
- `FORGIS_LOG.md` — run history

## Likely Failure Causes Identified

### 1. `CornerSize.toPx(size, density)` type mismatch — COMPILE ERROR (HIGH CONFIDENCE)

**Location**: Both `HomeScreen.kt` (`liquidGlassStroke`) and `GlassComponents.kt` (`glassCardStroke`)

**Root cause**: Inside `Modifier.drawBehind { ... }`, the lambda receiver is `DrawScope`. `DrawScope` has a property `density: Float` (inherited from `Density`). The code called `shape.topStart.toPx(size, density)` where `density` resolves to the `Float` property. However, `CornerSize.toPx(shapeSize: Size, density: Density)` expects the second parameter to be of type `Density`, not `Float`.

In `DrawScope`, `this` is the `DrawScope` which implements `Density`. The correct call is `shape.topStart.toPx(size, this)`.

This would produce a Kotlin compile error: `Type mismatch: inferred type is Float but Density was expected`.

**Impact**: Both `HomeScreen.kt` and `GlassComponents.kt` fail to compile, blocking the entire build.

### 2. `rememberDateTitle()` recomputes on every recomposition — INEFFICIENCY (LOW)

**Location**: `HomeScreen.kt`

**Root cause**: The `rememberDateTitle()` function was marked `@Composable` but did not use `remember` to cache the result. It would create new `Calendar` and `SimpleDateFormat` objects on every recomposition. While not a compile error or crash, it's wasteful.

### 3. No `remember` import (related to fix #2)

**Location**: `HomeScreen.kt`

**Root cause**: The file did not import `androidx.compose.runtime.remember`, which would be needed once `remember` is used inside `rememberDateTitle()`.

## Files Changed

| File | Change Summary |
|---|---|
| `app/src/main/java/com/vita0818/kikaria/ui/home/HomeScreen.kt` | **Fix 1**: Changed `shape.topStart.toPx(size, density)` → `shape.topStart.toPx(size, this)` and `shape.topEnd.toPx(size, density)` → `shape.topEnd.toPx(size, this)` in `liquidGlassStroke`. **Fix 2**: Added `import androidx.compose.runtime.remember`. **Fix 3**: Wrapped `rememberDateTitle()` body in `remember { ... }`. **Fix 4**: Wrapped `rememberDaysLeftText()` body in `remember { ... }`. |
| `app/src/main/java/com/vita0818/kikaria/ui/components/GlassComponents.kt` | **Fix 1**: Changed `shape.topStart.toPx(size, density)` → `shape.topStart.toPx(size, this)` and `shape.topEnd.toPx(size, density)` → `shape.topEnd.toPx(size, this)` in `glassCardStroke`. |

## Fixes Made

### Fix 1: `CornerSize.toPx` density parameter

In both files, inside `drawBehind` lambdas, the `toPx` calls were passing `density` (a `Float` property of `DrawScope`) where a `Density` object was expected. Changed to pass `this` (the `DrawScope` receiver, which implements `Density`).

This is the critical fix — without it, neither file would compile.

The `lineWidth * density` computations were left unchanged because `density: Float` is correct for Float arithmetic.

### Fix 2: `rememberDateTitle` caching

Wrapped the date computation in `remember { ... }` so it is computed once per composition and not recreated on every recomposition. Added the missing `import androidx.compose.runtime.remember`.

### Fix 3: `rememberDaysLeftText` caching

Applied the same `remember` wrapping for consistency.

## Home UI Direction Preservation

✅ **Fully preserved.** The fixes are surgical:

- Only the `toPx` calls inside `drawBehind` were changed (semantically equivalent but type-correct)
- The `remember` wrapping is purely an optimization
- No visual changes were made
- The Kikaria-like home screen design (gradient background, serif title, orbit-animated start button with decorative bubbles, glass card progress/dashboard with gradient strokes, dark mode adaptivity) remains intact
- All colors, typography, spacing, and animation parameters are unchanged

## Remaining Risks

1. **No native blur material**: The glass effect uses semi-transparent fills + shadows rather than `RenderEffect` blur (API 31+). Visual fidelity is close but not identical to iOS `.ultraThinMaterial`.

2. **`CornerRadius(Float, Float)` creates elliptical corners**: The two-parameter `CornerRadius` constructor creates `CornerRadius(x = ..., y = ...)`, which is an elliptical corner radius. For `RoundedCornerShape` with uniform corners, both values are identical, so it behaves as a circular corner. This is correct for `drawRoundRect` which does not support per-corner radii.

3. **`ExperimentalMaterial3Api` and `ExperimentalLayoutApi`**: Used in several screens. These APIs may change in future Compose BOM versions. With BOM 2024.02.00, they are stable enough but should be monitored.

4. **Compose compiler extension lockstep**: Kotlin 1.9.22 requires compiler extension 1.5.8. If Kotlin is bumped, the extension must be bumped in lockstep.

5. **No build verification in Forgis environment**: The changes are syntactically and semantically correct based on code analysis, but have not been verified with an actual Gradle build. A local Android Studio sync + build is the definitive test.

6. **Date caching with `remember`**: The date is now cached once per composition. If the user leaves the app open past midnight, the displayed date won't update until the Activity is recreated. This matches the original `@Composable` behavior (the date was already "frozen" at first render since there were no state triggers to recompute it). A production fix would use `LaunchedEffect` with a timer or observe `LocalDate`, but this is out of scope for this run-fix pass.

## Suggested Next Local Android Studio Steps

1. **Open the project** in Android Studio: `Kikaria-Android/` (the directory containing `settings.gradle.kts`)
2. **Sync Gradle**: File → Sync Project with Gradle Files
3. **Fix SDK path** if prompted: ensure `sdk.dir` in `local.properties` points to your Android SDK
4. **Build**: Build → Make Project
5. **Fix any remaining errors**: If the build fails, check Build Output for any additional compilation errors. Address them individually.
6. **Run**: Run → Run 'app' on an API 26+ emulator or device
7. **Verify home screen**: Confirm gradient background, serif "Kikaria" title, orbit-animated start button with decorative bubbles, glass progress card, and glass dashboard card all render correctly in both light and dark modes
