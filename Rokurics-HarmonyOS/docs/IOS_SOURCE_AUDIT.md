# Rokurics iOS/macOS Source Audit

Audit date: 2026-05-25

## 1. Source Project Structure

```
/Users/vita/Vitemis/Vela/Rokurics/
├── Rokurics.xcodeproj/              # Xcode project
├── Rokurics/                        # iOS app (42 Swift files)
│   ├── RokuricsApp.swift            # @main entry, LocalNetworkSyncAppService
│   ├── ContentView.swift            # Root view, NavigationStack
│   ├── RokuricsHomeView.swift       # Home screen with recording orb + nav card
│   ├── RokuricsAdaptiveLayout.swift # Adaptive layout for iPhone/iPad
│   ├── RokuricsColors.swift         # Aqua/mint/teal color system
│   ├── RokuricsTypography.swift     # Mixed CJK/Latin typography engine
│   ├── RokuricsButtonStyle.swift    # Scale button style
│   ├── RokuricsCardStyle.swift      # Glass card views
│   ├── RokuricsGlassStyle.swift     # Glass effect styles
│   ├── RokuricsStudyDocumentViews.swift # Study doc viewing
│   ├── RecordingManager.swift       # Audio recording (AVAudioRecorder)
│   ├── RecordingMetadata.swift      # Recording metadata model
│   ├── RecordingUploadStatus.swift  # Upload status enum
│   ├── RecordingUploadClient.swift  # HTTPS upload client
│   ├── RecordingUploadCoordinator.swift # Upload orchestration
│   ├── RecordingUploadPayload.swift # Upload payload model
│   ├── RecordingSessionView.swift   # Recording session UI
│   ├── RecordingStatusView.swift    # Recording status display
│   ├── RecordingLibraryView.swift   # Recording library browser
│   ├── RecordingStudyDetailPage.swift # Study detail page
│   ├── RecordingTitleEditing.swift  # Title editing rules
│   ├── RecordingLiveActivityController.swift # Live Activities
│   ├── AudioFileStore.swift         # Local file storage (Documents/Rokurics)
│   ├── StudyFilingModels.swift      # All study hierarchy models (~1800 lines)
│   ├── StudyLibraryStore.swift      # Study library persistence
│   ├── StudyLibrarySyncCoordinator.swift # Sync coordination
│   ├── StudyLibrarySyncModels.swift # Sync models
│   ├── StudyReadingPages.swift      # Reading pages
│   ├── KeychainStore.swift          # iOS Keychain wrapper
│   ├── IPhoneAIChatView.swift       # AI chat on iPhone
│   ├── IPhoneAIModels.swift         # iPhone AI models
│   ├── IPhoneSettingsView.swift     # Settings page
│   ├── UserProfile.swift            # User profile model
│   ├── DeviceConnectionCard.swift   # Connection status card
│   ├── MacConnectionView.swift      # Mac pairing UI
│   ├── MacConnectionSettings.swift  # Connection settings
│   ├── MacUploadClient.swift        # Upload client to Mac
│   ├── SecureMacConnectionSettings.swift # Secure pairing/keychain storage
│   ├── SecureMacUploadClient.swift  # Secure HTTPS upload
│   ├── SecureUploadUtilities.swift  # HMAC/crypto utilities
│   ├── ConnectionSyncStateStores.swift # Connection/sync state stores
│   ├── TransferQueueCard.swift      # Transfer queue UI
│   ├── UploadTestPayload.swift      # Test payload
│   └── UploadableRecordingRow.swift # Recording row for upload
│
├── RokuricsMac/                     # macOS app (95+ Swift files)
│   ├── RokuricsMacApp.swift         # @main entry
│   ├── ContentView.swift            # Root view -> MacRootView
│   ├── MacRootView.swift            # NavigationSplitView, sidebar
│   ├── MacSidebarView.swift         # Sidebar with nav items
│   ├── MacDashboardView.swift       # Dashboard with receiver status
│   ├── MacDashboardCard.swift       # Dashboard card components
│   ├── MacAudioInboxView.swift      # Audio inbox view
│   ├── MacAudioInboxCard.swift      # Audio inbox cards
│   ├── MacStudyLibraryView.swift    # Study library browser
│   ├── MacAIChatView.swift          # AI chat view
│   ├── MacAIProcessingCard.swift    # AI processing status card
│   ├── MacIPhoneConnectionView.swift # iPhone connection view
│   ├── MacSettingsView.swift        # Settings page
│   ├── MacTranscriptionSettingsView.swift # Transcription settings
│   ├── MacTranscriptionProviderPicker.swift # Provider picker
│   ├── MacWhisperCppSettingsView.swift # Whisper.cpp settings
│   ├── MacNoteGenerationSettingsView.swift # Note gen settings
│   ├── MacNoteDetailView.swift      # Note detail view
│   ├── MacDocumentDetailComponents.swift # Document detail components
│   ├── MacExportCard.swift          # Export card
│   ├── MacReceiverStatusCard.swift  # Receiver status card
│   ├── MacRecordingInboxItem.swift  # Recording inbox item
│   ├── MacTheme.swift               # Mac-specific theme
│   ├── MacTypography.swift          # Mac typography
│   ├── MacGlassStyle.swift          # Glass style
│   ├── MacUserProfile.swift         # Mac user profile store
│   ├── MacAppStorageProfile.swift   # App storage paths
│   ├── MacIdentityManager.swift     # TLS/signing identity
│   ├── MacSecurityUtilities.swift   # Crypto: SHA256, HMAC, constant-time
│   ├── SelfSignedCertificateBuilder.swift # X.509v3 DER builder
│   ├── MacLocalNetworkAddressProvider.swift # LAN IPv4 detection
│   ├── SecureLocalHTTPSServer.swift # Main HTTPS server (1910 lines, 18 routes)
│   ├── LocalHTTPServer.swift        # Insecure HTTP server (disabled)
│   ├── ReceiverService.swift        # Insecure receiver (blocked)
│   ├── SecureReceiverService.swift  # Secure receiver orchestrator
│   ├── RequestVerifier.swift        # HMAC signature verification
│   ├── PairingManager.swift         # 6-digit code pairing
│   ├── PairedDeviceStore.swift      # Paired device persistence
│   ├── IncomingRecordingMetadata.swift # Received metadata model
│   ├── RecordingReceiveResult.swift # Receive result models (409 lines)
│   ├── ReceivedFileStore.swift      # Received file management
│   ├── MacRecordingFileStore.swift  # Mac recording file store
│   ├── AudioInboxStore.swift        # Audio inbox store
│   ├── AudioPreprocessor.swift      # Audio format preprocessor
│   ├── AudioPreprocessorConfiguration.swift
│   ├── AudioConversionResult.swift
│   ├── FFmpegAudioConverter.swift   # ffmpeg-based converter
│   ├── NativeAudioConverter.swift   # Native AVFoundation converter
│   ├── ExportManager.swift          # Export recordings
│   ├── SecurityScopedFileAccess.swift # Sandbox bookmark management (1029 lines)
│   ├── TranscriptionProvider.swift  # Transcription protocol
│   ├── TranscriptionProviderKind.swift
│   ├── TranscriptionProviderConfiguration.swift
│   ├── TranscriptionCoordinator.swift # Transcription orchestration
│   ├── TranscriptionQueue.swift     # Transcription queue (stub)
│   ├── TranscriptionRequest.swift
│   ├── TranscriptionResult.swift
│   ├── TranscriptionError.swift     # 70+ error cases
│   ├── TranscriptionSegment.swift
│   ├── TranscriptionSettingsStore.swift
│   ├── TranscriptionConfigurationValidator.swift
│   ├── WhisperCppTranscriptionProvider.swift # whisper.cpp impl (~2240 lines)
│   ├── WhisperCppTranscriptionConfiguration.swift
│   ├── WhisperCppRuntimeResolver.swift
│   ├── WhisperCppFilePickerConfiguration.swift
│   ├── WhisperCppFileVisibilityDiagnostics.swift
│   ├── WhisperCppSettingsDraft.swift
│   ├── MockTranscriptionProvider.swift
│   ├── NoteGenerationProvider.swift # Note generation protocol
│   ├── NoteGenerationProviderKind.swift
│   ├── NoteGenerationCoordinator.swift
│   ├── NoteGenerationRequest.swift
│   ├── NoteGenerationResult.swift
│   ├── NoteGenerationError.swift
│   ├── NoteGenerationSettingsStore.swift
│   ├── NoteGenerationTranscriptLoader.swift
│   ├── NoteStore.swift              # Note persistence
│   ├── AnthropicMessagesNoteGenerationProvider.swift
│   ├── AnthropicMessagesNoteGenerationClient.swift
│   ├── AnthropicMessagesConfiguration.swift
│   ├── OpenAICompatibleNoteGenerationProvider.swift
│   ├── OpenAICompatibleNoteGenerationClient.swift
│   ├── OpenAICompatibleNoteGenerationConfiguration.swift
│   ├── MockNoteGenerationProvider.swift
│   ├── LLMServiceConfig.swift
│   ├── ChatProvider.swift           # Chat protocol + providers
│   ├── ChatCoordinator.swift        # Chat state management
│   ├── ChatContextBuilder.swift     # (stub, moved to Shared)
│   ├── LongProcessingModels.swift   # Chunk planning, merging
│   ├── TranscriptStore.swift        # Transcript persistence
│   ├── GitBackedStudyMetadataStore.swift # Git-backed metadata
│   ├── StudyLibraryModels.swift     # Mac study models
│   ├── StudyLibraryStore.swift      # Study library store
│   ├── StudyLibrarySyncModels.swift # Sync models
│   └── RecordingTitleEditing.swift  # Title editing (Mac)
│
├── RokuricsShared/                  # Shared code (4 files)
│   ├── ChatModels.swift             # Chat message/context/conversation models
│   ├── SharedChatComponents.swift   # Shared chat UI
│   ├── SharedStudyComponents.swift  # Shared study UI
│   └── SharedRokuricsUI.swift       # Shared UI utilities
│
├── RokuricsLiveActivities/          # iOS Dynamic Island
├── RokuricsLiveActivitiesShared/    # Shared Live Activity data
├── RokuricsTests/                   # Unit tests
├── RokuricsUITests/                 # UI tests
├── RokuricsMacTests/                # Mac unit tests
├── RokuricsMacUITests/              # Mac UI tests
├── RokuricsVisualDiagnostics/       # Visual diagnostics
├── Scripts/                         # Build scripts
└── docs/
    └── LongRecordingTestPlan.md     # Long recording test plan
```

## 2. Functional Overview

Rokurics is a **study recording, AI transcription, and note generation system** with a dual-device (iPhone + Mac) architecture:

### iPhone App (Rokurics)
- **Audio Recording**: Record lectures/meetings in AAC/M4A format
- **Recording Library**: Browse, rename, delete, restore recordings
- **Study Filing**: Hierarchical filing system (Type > Subject > Chapter > Topic)
- **Mac Connection**: Secure HTTPS pairing with Mac for upload
- **Upload Queue**: Send recordings to Mac for processing
- **AI Chat**: Chat with study materials as context
- **Live Activities**: Dynamic Island recording status
- **User Profile**: Avatar, display name, handle

### macOS App (RokuricsMac)
- **Receiver Service**: HTTPS server receiving recordings from iPhone
- **Transcription Engine**: whisper.cpp (local), mock provider
- **Note Generation**: OpenAI-compatible + Anthropic providers
- **AI Chat**: Multi-turn chat with study context
- **Study Library**: Browse transcriptions and notes by filing hierarchy
- **Audio Inbox**: Manage incoming recordings
- **Export**: Export notes and transcripts
- **Dashboard**: System status overview

### Security Architecture
- 6-digit pairing code exchange
- Self-signed TLS certificate (ECDSA P-256)
- HMAC-SHA256 request signing per device
- Nonce-based replay protection
- Timestamp validation (±5 min window)
- SHA-256 body hash verification
- Constant-time comparisons
- Keychain-stored secrets (iOS)
- Security-scoped bookmarks (macOS sandbox)

## 3. Page/Component Inventory

### iOS Pages
| Page | Source | Description |
|------|--------|-------------|
| Home | RokuricsHomeView.swift | Recording orb + nav card (3 buttons) |
| Recording Session | RecordingSessionView.swift | Active recording with controls |
| Recording Library | RecordingLibraryView.swift | Browse all recordings |
| Study Detail | RecordingStudyDetailPage.swift | Recording detail with filing |
| AI Chat | IPhoneAIChatView.swift | Chat interface |
| Mac Connection | MacConnectionView.swift | Pairing/connection status |
| Settings | IPhoneSettingsView.swift | User profile settings |

### macOS Pages
| Page | Source | Description |
|------|--------|-------------|
| Dashboard | MacDashboardView.swift | System status overview |
| Audio Inbox | MacAudioInboxView.swift | Incoming recordings |
| Study Library | MacStudyLibraryView.swift | Browse library by hierarchy |
| iPhone Connection | MacIPhoneConnectionView.swift | Connection management |
| AI Chat | MacAIChatView.swift | Chat interface |
| Note Detail | MacNoteDetailView.swift | View generated notes |
| Settings | MacSettingsView.swift | All settings |
| Transcription Settings | MacTranscriptionSettingsView.swift | Provider/config |
| Whisper.cpp Settings | MacWhisperCppSettingsView.swift | Detailed whisper config |
| Note Gen Settings | MacNoteGenerationSettingsView.swift | AI provider config |

## 4. Data Model Inventory

### Core Models
- **RecordingMetadata**: id, title, fileName, relativeAudioPath/relativeMetadataPath, createdAt/endedAt, duration, format/codec/sampleRate/channels/bitrate, fileSize, uploadStatus, transcriptionStatus, noteStatus, tags, studyFiling, isDeleted, deletedAt, upload progress fields
- **StudyFilingPath**: type, subject, chapter, topic (4-level hierarchy)
- **StudyItemMetadata**: itemID, kind (recordingBundle/standaloneNote), title, filing path, tags, folderIDs, recordingID, transcript/note relative paths, status fields
- **StudyFolderMetadata**: folderID, name, level, path, parent/child IDs
- **StudyTag**: id, namespace, value, displayName
- **RecordingReceiveRecord**: Full receive record with all metadata
- **UserProfile**: displayName, handle, avatar

### Chat Models (Shared)
- **ChatMessage**: id, role (system/user/assistant), content, createdAt
- **ChatConversation**: id, title, messages, context, attachments
- **ChatContext**: id, title, browsePathComponents, items, metadata
- **ChatContextItem**: id, title, filingPath, content, sourcePath
- **ChatAttachment**: id, conversationID, fileName, fileType, kind

### Processing Models
- **TranscriptionRequest/Result**: Input/output for transcription
- **NoteGenerationRequest/Result**: Input/output for note generation
- **AudioChunkDescriptor/Plan**: Chunk planning for long audio
- **TranscriptTextChunk**: Text chunking for long transcripts
- **ProcessingMode**: singlePass, chunked, sectioned

## 5. Service/Protocol Inventory

### Provider Protocols
- **TranscriptionProvider**: id, displayName, validateConfiguration(), transcribe(request:)
- **NoteGenerationProvider**: id, displayName, validateConfiguration(), generateNote(request:)
- **ChatProvider**: id, displayName, send(request:), generateConversationTitle(request:)

### Stores
- **AudioFileStore**: iOS local file management (Documents/Rokurics)
- **StudyLibraryStore**: iOS study library persistence
- **KeychainStore**: iOS Keychain wrapper
- **MacRecordingFileStore**: Mac recording file management
- **ReceivedFileStore**: Received file management
- **AudioInboxStore**: Audio inbox management
- **NoteStore**: Note file persistence (notes/ dir)
- **TranscriptStore**: Transcript file persistence
- **PairedDeviceStore**: Paired device persistence
- **TranscriptionSettingsStore**: UserDefaults-backed settings singleton
- **NoteGenerationSettingsStore**: UserDefaults-backed settings singleton

### Coordinators
- **RecordingUploadCoordinator**: Upload orchestration (iOS)
- **TranscriptionCoordinator**: Transcription workflow (Mac)
- **NoteGenerationCoordinator**: Note generation workflow (Mac)
- **ChatCoordinator**: Chat conversation state (Mac/Shared)

### Network/Security
- **SecureLocalHTTPSServer**: 18-route HTTPS server
- **RequestVerifier**: HMAC signature verification
- **PairingManager**: 6-digit code exchange
- **MacIdentityManager**: TLS/signing key management

## 6. File Formats & Local Storage

### iOS Storage Structure (Documents/Rokurics/)
```
Rokurics/
├── Recordings/           # *.m4a audio files
│   └── rokurics_YYYY-MM-DD_HH-mm-ss(_fallback).m4a
├── Metadata/             # *.json per recording
│   └── {recording-id}.json
└── Study Library/        # Study library data
```

### macOS Storage Structure
```
Rokurics/
├── Recordings/           # Received audio files
├── Metadata/             # Recording metadata
├── Receives/             # receive.json per recording
├── Transcripts/          # transcript.json + transcript.md
├── Notes/                # note.md + summary.json
│   └── {date}/{sanitized-id}/
├── Chats/                # conversations/ + attachments/
├── Sync/                 # sync state files
├── Security/             # TLS cert + identity
│   ├── mac-identity.json
│   └── tls-certificate.der
└── paired-devices.json
```

### Key File Formats
- **Metadata JSON**: ISO 8601 dates, Codable RecordingMetadata
- **receive.json**: RecordingReceiveRecord with full lifecycle state
- **transcript.json**: TranscriptionResult with segments
- **transcript.md**: Markdown transcript
- **note.md**: Generated study note in markdown
- **summary.json**: NoteSummaryPreview with shortSummary + keyPoints
- **paired-devices.json**: Array of PairedDevice objects
- **device-connection-status.json**: Connection state tracking

## 7. Apple-only APIs Requiring HarmonyOS Alternatives

| Apple API | Purpose | HarmonyOS Alternative |
|-----------|---------|----------------------|
| AVAudioRecorder | Audio recording | @ohos.multimedia.audio (AudioCapturer) |
| AVAudioSession | Audio session config | AudioManager |
| NWListener / NWConnection | TCP/TLS server | @ohos.net.socket (TLS Socket) |
| SecKey / CryptoKit | ECDSA, HMAC, SHA256 | @ohos.security.cryptoFramework |
| SecIdentity / SecCertificate | TLS certificate | cryptoFramework cert/key |
| UserDefaults | Settings persistence | @ohos.data.preferences |
| Keychain | Sensitive data storage | @ohos.security.huks (HUKS) |
| FileManager | File system operations | @ohos.file.fs |
| NSOpenPanel | File picker | @ohos.file.picker |
| Live Activities | Dynamic Island | Live View / Notification |
| NSPredicate / SortDescriptor | Data sorting | Array sort/filter in ArkTS |
| SwiftUI View Modifiers | UI styling | ArkUI attribute modifiers |
| URLSession | HTTP client | @ohos.net.http |

## 8. Migration Priority

### P0 (Must have - core functionality)
1. Recording (audio capture)
2. Recording metadata + local storage
3. Recording library (CRUD)
4. Study filing hierarchy
5. User profile
6. Home screen with navigation
7. Color/typography system
8. Recording session UI

### P1 (Should have - processing)
9. Transcription provider abstraction
10. Note generation provider abstraction
11. Study library browser
12. Settings pages
13. Local file management

### P2 (Nice to have - connectivity)
14. HTTPS server for Mac receive
15. iPhone-Mac pairing
16. Secure upload
17. AI Chat
18. Export

### P3 (Platform-specific - may differ)
19. Live Activities → Live View
20. Sandbox bookmarks → file permission model
21. whisper.cpp integration → native module
22. Git-backed metadata → versioned storage

## 9. Points That Cannot Be Fully Migrated

1. **whisper.cpp integration**: Requires native C++ module or NAPI bridge for HarmonyOS. Current implementation depends on macOS process execution with complex sandbox/entitlement management. Provider abstraction layer allows plugging in alternative engines.

2. **iPhone-Mac secure pairing**: The dual-device architecture (iPhone records → Mac processes) assumes two separate devices. On single-device HarmonyOS, this collapses to a local-only pipeline. The provider/planner architecture is preserved.

3. **Security-scoped bookmarks**: macOS sandbox bookmark mechanism has no direct HarmonyOS equivalent. Replaced by HarmonyOS file permission model with user consent.

4. **Live Activities (Dynamic Island)**: iOS-specific. HarmonyOS has Live View as alternative.

5. **Git-backed study metadata**: Depends on local git repository. Can be replaced with versioned JSON storage or omitted initially.
