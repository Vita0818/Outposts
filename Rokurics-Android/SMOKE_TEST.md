# Rokurics Android — Smoke Test Guide

## Build & Install

```bash
# Assemble debug APK
./gradlew assembleDebug

# Install on connected device/emulator
adb install app/build/outputs/apk/debug/app-debug.apk

# Or build + install in one step
./gradlew installDebug
```

Requirements: Android 8.0+ (API 26), JDK 21, Android SDK 34.

## Emulator Quick Start

```bash
# List available AVDs
emulator -list-avds

# Launch an AVD (API 26+)
emulator -avd <avd_name> &

# Wait for boot, then install
adb wait-for-device && ./gradlew installDebug
```

## Smoke Scenarios

### 1. Home Dashboard
- [ ] App launches to home screen with Rokurics header
- [ ] Recording orb shows "+" in idle state
- [ ] Stats card reflects existing recordings
- [ ] Navigation card shows 3 buttons: Study Library, AI Chat, Mac Connection

### 2. Recording Flow
- [ ] Tap orb → navigate to recording screen
- [ ] Grant microphone permission if prompted
- [ ] Tap Start → timer runs, waveform animates
- [ ] Tap Pause → timer freezes
- [ ] Tap Resume → timer continues
- [ ] Tap Stop → filing overlay appears
- [ ] Fill filing fields → Save → returns to home
- [ ] Low-power mode: wait 5s during recording → minimal clock display, tap to exit

### 3. Study Library
- [ ] Navigate to Study Library
- [ ] Breadcrumb navigation through type→subject→chapter→topic
- [ ] Folder tiles with color badges, tap to navigate in
- [ ] Recording rows with play button, status chips, upload progress
- [ ] Tap recording → detail page with metadata, filing editor, actions
- [ ] Play button in detail → AudioPlayerBar with seek slider and time
- [ ] Long-press folder → color picker with 12 colors
- [ ] Trash tab: move to trash, restore, permanent delete
- [ ] Empty state shown when no content at current level

### 4. Inline Playback (Mini-Player)
- [ ] Play a recording from recording row
- [ ] Mini-player bar appears at bottom with: play/pause, title, seek slider, time display (MM:SS / MM:SS), close button
- [ ] Seek slider responds to drag → position updates
- [ ] Time display updates during playback
- [ ] Close button stops and dismisses mini-player
- [ ] Auto-stops when playback completes

### 5. Mac Connection
- [ ] Navigate to Mac Connection
- [ ] Enter host/port/fingerprint/pairing code
- [ ] Pair button attempts connection
- [ ] Sync status displayed when paired

### 6. Upload Queue Recovery
- [ ] Upload a recording (requires paired Mac)
- [ ] Force-kill app during upload
- [ ] Re-open app → recording shows "传输失败" (failed) status
- [ ] Re-upload works from detail page

### 7. AI Chat
- [ ] Navigate to AI Chat
- [ ] Greeting card shown
- [ ] Input bar with text field and send button
- [ ] Settings dialog for provider configuration
- [ ] Context picker for study library items

### 8. Settings
- [ ] Navigate to Settings via profile icon on home
- [ ] Display name / username fields
- [ ] AI provider selection (OpenAI / Anthropic)

## Run Unit Tests

```bash
./gradlew test
```

## Adaptive Layout Checkpoints

- [ ] Phone portrait: compact layout, bottom NavigationBar, 2-column folder grid
- [ ] Tablet (600dp+): NavigationRail on left, 3-column folder grid, side pane detail
- [ ] Small phone (<360dp): reduced orb scale, tighter padding
- [ ] Landscape/short screens: compressed vertical spacing
- [ ] Recording screen: timer font scales up on wider screens

## Visual Smoke Testing

### Manual Screenshot Comparison

1. Launch app on emulator/device and on each key screen
2. Take screenshots: `adb shell screencap -p /sdcard/screen.png && adb pull /sdcard/screen.png`
3. Compare against prior baselines stored in `screenshots/baseline/`

### Key Screens to Capture

| Screen | States |
|--------|--------|
| Home Dashboard | Idle, Recording active, With upload pending |
| Recording Session | Idle, Recording, Paused, Filing overlay, Low-power mode |
| Study Library | Root level, Folder drill-down, With recordings, Trash tab |
| Recording Detail | Basic, With filing editor, Audio player visible |
| AI Chat | Greeting, Active conversation, Context picker |
| Mac Connection | Unpaired, Paired, Syncing |
| Settings | Default, AI provider configured |

### Multi-pane Tablet Checkpoints

- [ ] Library browser + detail side-by-side on wide (>=600dp) screens
- [ ] NavigationRail visible on left for medium/expanded windows
- [ ] Bottom NavigationBar hidden on wide screens
- [ ] Folder grid shows 3 columns on wide, 2 on narrow

### Persistent Mini-Player

- [ ] Play recording in Library → navigate to Home → playback continues
- [ ] Mini-player bar visible at screen bottom across Home/Library/Chat/Connection
- [ ] Play/pause toggle, seek slider, time display all functional
- [ ] Close button stops playback and dismisses bar
- [ ] Mini-player hidden during full-screen recording

### Automation Plan (Future)

- [ ] Set up Paparazzi for Compose screenshot testing
- [ ] Define `@Preview` composables for each screen state
- [ ] Compare Paparazzi output against golden images in CI
- [ ] Use `androidx.compose.ui.test` for UI interaction tests
