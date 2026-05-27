# Rokurics HarmonyOS Migration Plan

Date: 2026-05-25

## Target Architecture

```
entry/src/main/ets/
├── entryability/
│   └── EntryAbility.ets          # App entry point
├── pages/                         # ArkUI pages
│   ├── HomePage.ets              # Home screen - Recording orb + nav
│   ├── RecordingSessionPage.ets  # Active recording session
│   ├── RecordingLibraryPage.ets  # Browse all recordings
│   ├── RecordingDetailPage.ets   # Recording detail + study filing
│   ├── AIChatPage.ets            # AI chat interface
│   └── SettingsPage.ets          # User profile settings
├── models/                        # Data models (ArkTS)
│   ├── RecordingModels.ets       # RecordingMetadata, StudyFilingPath, RecordingState
│   ├── UserProfile.ets           # User profile model
│   ├── ChatModels.ets            # ChatMessage, ChatConversation, ChatContext
│   └── ProcessingModels.ets      # TranscriptionResult, NoteGenerationResult
├── services/                      # Core services
│   ├── AudioFileStore.ets        # Local file storage service
│   ├── RecordingManager.ets      # Audio recording manager
│   └── SettingsStore.ets         # Preferences-based settings
├── providers/                     # AI provider abstractions
│   └── ProviderInterfaces.ets    # Transcription, NoteGen, Chat protocols + mocks
└── utils/                         # Utilities
    ├── RokuricsTheme.ets         # Colors, typography
    └── FormatHelpers.ets         # Time/file formatting
```

## Platform Mappings

| iOS/macOS API | HarmonyOS API |
|---|---|
| AVAudioRecorder | @ohos.multimedia.audio AudioCapturer |
| AVAudioSession | AudioManager |
| UserDefaults | @ohos.data.preferences |
| Keychain | @ohos.security.huks (HUKS) |
| FileManager | @ohos.file.fs |
| NWListener (HTTPS) | @ohos.net.socket TLSSocket |
| SecKey / CryptoKit | @ohos.security.cryptoFramework |
| SwiftUI View hierarchy | ArkUI declarative components |
| NavigationStack | @ohos.router |
| NSOpenPanel | @ohos.file.picker |

## Status Per Feature

See FEATURE_COVERAGE_MATRIX in final report.

## Known Limitations (Current Version)

1. **No real audio recording yet**: AudioCapturer API usage needs device testing
2. **No HTTPS server**: Mac receive mode not applicable to single-device HarmonyOS
3. **No whisper.cpp integration**: Requires NAPI native module
4. **Mock AI providers**: Chat and note generation use mock implementations
5. **No Live Activities**: HarmonyOS Live View requires additional setup
6. **No device pairing**: Dual-device flow not relevant for single-device use case
