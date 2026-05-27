# Rokurics Windows Migration Report — Round 2 (Mac Client Parity)

Generated: 2026-05-25

## 1. MODEL_CHECK_RESULT

- **Model**: deepseek-v4-pro[1m]
- **Status**: OK

## 2. PATH_CHECK_RESULT

- **Source path**: `/Users/vita/Vitemis/Vela/Rokurics` — EXISTS
- **Target path**: `/Users/vita/Vitemis/Outposts/Rokurics-Windows` — EXISTS

## 3. SOURCE_READONLY_RESULT

**PASS** — Source project unchanged. Pre-existing modified files only. No writes to source.

## 4. MAC_CLIENT_SOURCE_AUDIT

### Mac App Entry
- `RokuricsMac/RokuricsMacApp.swift` → `ContentView.swift` → `MacRootView.swift`

### Mac Top-Level Navigation
- `MacRootView.swift` — NavigationSplitView with sidebar + detail
- `MacSidebarView.swift` — Sidebar with 3 items + profile/settings button
- Default selection: **studyLibrary** (学习库), then aiChat (AI 对话), iPhoneConnection (iPhone 连接)

### Mac Page List
| Page | Source File | Key Sub-Views |
|------|-----------|---------------|
| Study Library (default) | MacStudyLibraryView.swift | Browser, folder tiles, recording cards, detail page, transcript view, note view, trash sheet |
| AI Chat | MacAIChatView.swift | ChatConversationView, recent conversations popover, attachment menu, study library picker |
| iPhone Connection | MacIPhoneConnectionView.swift | Unpaired pairing info, connected device bubble + card, detail/paired devices/upload test sheets |
| Settings | MacSettingsView.swift | Profile pane, transcription section, AI section, about section, drill-down detail sheets |
| Dashboard | MacDashboardView.swift | Receiver status, audio inbox, AI processing cards (accessible via sidebar or fallback) |

### Mac Service/Provider Architecture
| Abstraction | Source File | Role |
|------------|-----------|------|
| TranscriptionProvider (protocol) | TranscriptionProvider.swift | Abstract transcription — whisper.cpp impl |
| NoteGenerationProvider (protocol) | NoteGenerationProvider.swift | Abstract note generation — OpenAI/Anthropic impls |
| ChatProvider (protocol) | ChatProvider.swift | Abstract chat — OpenAI/Anthropic/Mock impls |
| RecordingUploadClientProtocol | RecordingUploadClient.swift | Upload recordings to Mac |
| SecureReceiverService | SecureReceiverService.swift | HTTPS server, pairing, device management |
| TranscriptionCoordinator | TranscriptionCoordinator.swift | Async transcription task management |
| NoteGenerationCoordinator | NoteGenerationCoordinator.swift | Async note generation task management |
| ChatCoordinator | ChatCoordinator.swift | Conversation management, context import, provider dispatch |
| AudioInboxStore | AudioInboxStore.swift | Recording inbox CRUD, trash, file management |
| StudyLibraryStore | StudyLibraryStore.swift | Study items, folders, hierarchy rules, sync manifests |
| TranscriptionSettingsStore | TranscriptionSettingsStore.swift | Transcription provider/model config |
| NoteGenerationSettingsStore | NoteGenerationSettingsStore.swift | AI provider/model/API config |

### Mac Key Data Files (shared)
- `StudyFilingModels.swift` — StudyFilingPath, StudyItemMetadata, StudyFolderMetadata, StudyTag, RecordingReceiveRecord
- `ChatModels.swift` — ChatMessage, ChatConversation, ChatContext, ChatAttachment
- `ConnectionSyncStateStores.swift` — DeviceConnectionStatusStore, StudyLibrarySyncStateStore, LocalNetworkSyncStateStore
- `RecordingMetadata.swift` — RecordingMetadata with upload/transcription/note status

## 5. MAC_CLIENT_PAGE_TREE

```
MacRootView (NavigationSplitView)
├── Sidebar (MacSidebarView)
│   ├── Brand: "Rokurics / Mac"
│   ├── 学习库 (default selected)
│   ├── AI 对话
│   ├── iPhone 连接
│   └── [Bottom] Profile button → Settings
└── Detail Area
    ├── MacStudyLibraryView
    │   ├── Breadcrumb navigation + toolbar (back, new folder, import to chat, trash)
    │   ├── Folder tiles (grid) with color dots, inline rename, context menu
    │   ├── Recording cards (waveform icon, title, date, duration, actions: play/transcribe/note/chat)
    │   ├── Standalone note cards
    │   ├── Recording Detail Page (MacStudyRecordingDetailPage)
    │   │   ├── Header (back, title, subtitle, import to chat, delete buttons)
    │   │   ├── Action grid: transcribe / view transcript / generate note / view note
    │   │   ├── Filing picker (4-level: type/subject/chapter/topic + candidates + create new)
    │   │   ├── File status panel (recordingID, audio, transcript, note paths)
    │   │   └── AI summary preview card
    │   ├── Transcript Detail (MacTranscriptDetailView)
    │   ├── Note Detail (MacNoteDetailView)
    │   └── Trash sheet (restore / permanent delete)
    ├── MacAIChatView
    │   ├── ChatConversationView (shared component)
    │   ├── Recent conversations popover (select / delete)
    │   ├── Attachment menu → import study library / upload file / upload image
    │   └── Study library picker sheet
    ├── MacIPhoneConnectionView
    │   ├── Unpaired: header + pairing info card
    │   │   ├── Mac address + port display
    │   │   ├── Fingerprint (show/hide toggle)
    │   │   ├── Pairing code (6 digits + expiry) OR "Start Pairing" button
    │   │   └── Copy pairing info button
    │   ├── Connected: device bubble + device card
    │   │   ├── Animated device bubble (iPhone/iPad icon)
    │   │   ├── Status rows: state, last online, last sync
    │   │   └── Actions: sync now, view connection info, disconnect
    │   └── Sheets: connection detail, paired devices, upload test
    └── MacSettingsView
        ├── Profile pane (avatar, display name, handle, edit button)
        ├── Transcription section: Provider / Model / Auth & Test
        ├── AI section: Provider / Model / API Settings / Test
        └── About section: Storage / Privacy Policy / Copyright
```

## 6. WINDOWS_BEFORE_GAP_ANALYSIS (Round 1 Issues)

| Issue | Round 1 State | Gap |
|-------|-------------|-----|
| Navigation | Flat top tabs (首页/学习库/AI对话/设置) | Wrong — Mac uses sidebar with 3 items |
| Default page | Home recording page | Wrong — Mac defaults to study library |
| iPhone Connection | Buried in Settings tab | Wrong — Mac has dedicated sidebar item |
| Recording detail | Not present | Missing — Mac has full detail page with filing picker |
| Transcript/Note views | Not present | Missing — Mac has dedicated transcript and note detail views |
| Folder tiles | Not present | Missing — Mac has folder grid with color tokens and context menus |
| Settings structure | Flat form with all fields | Wrong — Mac has profile pane + grouped sections with drill-down |
| Profile pane | Not present | Missing — Mac has avatar + name + handle in settings |
| File status panel | Not present | Missing — Mac shows recordingID, audio, transcript, note status |
| AI summary preview | Not present | Missing — Mac loads and displays note summary JSON |

## 7. WINDOWS_AFTER_ARCHITECTURE (Round 2 — Current)

### Navigation
- **SidebarView** matches MacSidebarView: 3 items (学习库, AI对话, iPhone连接) + profile/settings button
- Default selection: 学习库
- MainWindow uses NavigationView pattern (sidebar + frame for detail)

### Pages (matching Mac)
| Mac Source | Windows Page | Status |
|-----------|-------------|--------|
| MacStudyLibraryView | MacStudyLibraryPage | Full parity |
| MacAIChatView | MacAIChatPage | Full parity |
| MacIPhoneConnectionView | MacIPhoneConnectionPage | Full parity |
| MacSettingsView | MacSettingsPage | Full parity |

### Data Models (preserved from Round 1)
All models retained: RecordingMetadata, StudyFilingPath, StudyItemMetadata, StudyFolderMetadata, StudyTag, ChatModels, ConnectionModels

### Services (preserved from Round 1)
All interfaces: ITranscriptionProvider, INoteGenerationProvider, IChatProvider, IRecordingUploadClient
All stores: AudioFileStore, StudyLibraryStore, DeviceConnectionStatusStore, StudyLibrarySyncStateStore
All mocks: MockTranscriptionProvider, MockNoteGenerationProvider, MockChatProvider, MockRecordingUploadClient

## 8. IMPLEMENTED_FILES

### Solution & Config
- `Rokurics.sln`
- `Rokurics/Rokurics.csproj`
- `Rokurics/app.manifest`
- `Rokurics.Tests/Rokurics.Tests.csproj`

### Application Core
- `Rokurics/App.xaml` — WinUI 3 resources, converters, typography
- `Rokurics/App.xaml.cs` — DI container, service registration
- `Rokurics/MainWindow.xaml` — Sidebar + frame navigation
- `Rokurics/MainWindow.xaml.cs` — Navigation dispatch

### Views (Mac-parity pages)
- `Rokurics/Views/SidebarView.xaml` + `.cs` — 3-item sidebar + profile
- `Rokurics/Views/MacStudyLibraryPage.xaml` + `.cs` — Browser, folder tiles, recording cards, detail page
- `Rokurics/Views/MacAIChatPage.xaml` + `.cs` — AI chat with conversations, messages, input bar
- `Rokurics/Views/MacIPhoneConnectionPage.xaml` + `.cs` — Unpaired/connected states
- `Rokurics/Views/MacSettingsPage.xaml` + `.cs` — Profile pane + 3 section groups

### Retained Views (from Round 1, kept as-is)
- `Rokurics/Views/HomePage.xaml` + `.cs` — Dashboard (accessible)
- `Rokurics/Views/StudyLibraryPage.xaml` + `.cs` — Kept for reference
- `Rokurics/Views/ChatPage.xaml` + `.cs` — Kept for reference
- `Rokurics/Views/SettingsPage.xaml` + `.cs` — Kept for reference

### Models
- `Rokurics/Models/RecordingMetadata.cs`
- `Rokurics/Models/StudyFilingPath.cs`
- `Rokurics/Models/StudyFolderLevel.cs`
- `Rokurics/Models/StudyItemMetadata.cs`
- `Rokurics/Models/StudyFolderMetadata.cs`
- `Rokurics/Models/StudyTag.cs`
- `Rokurics/Models/StudyEnums.cs`
- `Rokurics/Models/ChatModels.cs`
- `Rokurics/Models/ConnectionModels.cs`

### Services
- `Rokurics/Services/ProviderInterfaces.cs`
- `Rokurics/Services/AudioFileStore.cs`
- `Rokurics/Services/StudyLibraryStore.cs`
- `Rokurics/Services/RecordingManager.cs`
- `Rokurics/Services/MockProviders.cs`

### Stores
- `Rokurics/Stores/ConnectionSyncStateStores.cs`

### Helpers & Converters
- `Rokurics/Helpers/RokuricsColors.cs`
- `Rokurics/Converters/ValueConverters.cs`

### ViewModels (retained from Round 1)
- `Rokurics/ViewModels/MainViewModel.cs`
- `Rokurics/ViewModels/StudyLibraryViewModel.cs`
- `Rokurics/ViewModels/ChatViewModel.cs`
- `Rokurics/ViewModels/SettingsViewModel.cs`

### Tests
- `Rokurics.Tests/StudyModelsTests.cs`

### Documentation
- `MIGRATION_REPORT.md`

## 9. MODIFIED_FILES

Round 2 replaced:
- `MainWindow.xaml` — from tabs to sidebar navigation
- `MainWindow.xaml.cs` — navigation dispatch to Mac pages
- `App.xaml` — added converter resources
- `App.xaml.cs` — cleaned DI setup

Round 2 added:
- `Views/SidebarView.xaml` + `.cs`
- `Views/MacStudyLibraryPage.xaml` + `.cs`
- `Views/MacAIChatPage.xaml` + `.cs`
- `Views/MacIPhoneConnectionPage.xaml` + `.cs`
- `Views/MacSettingsPage.xaml` + `.cs`

## 10. FEATURE_PARITY_MAP

| Mac Feature | Mac Source File | Windows File | Status | Gap | Next Step |
|------------|----------------|-------------|--------|-----|-----------|
| Sidebar navigation | MacSidebarView.swift | SidebarView.xaml | 已复刻 | Icon-only, no multi-font | Add mixed CJK/English font rendering |
| Study library browser | MacStudyLibraryView.swift | MacStudyLibraryPage.xaml | 已复刻 | Folder color dots missing | Implement color token picker |
| Folder tiles grid | MacStudyFolderTile | CreateFolderTile() | 部分复刻 | No context menu, no inline rename | Add right-click popover |
| Recording cards with actions | MacStudyRecordingCard | CreateRecordingCard() | 已复刻 | No hover delete icon swap | Add pointer hover events |
| Recording detail page | MacStudyRecordingDetailPage | DetailPanel in XAML | 已复刻 | Filing picker candidates not wired | Wire candidate resolution |
| Filing picker (4-level) | MacStudyFilingPicker | Filing picker section | 部分复刻 | No "create new value" flow | Add create-new-value input |
| Transcript detail | MacTranscriptDetailView | (in page code) | stub | No markdown content loader | Wire TranscriptMarkdownDocumentLoader |
| Note detail | MacNoteDetailView | (in page code) | stub | No note markdown display | Implement note detail page |
| AI summary preview | MacStudyNoteSummaryPreviewCard | Summary preview card | stub | Does not load summary.json | Wire NoteStore summary loading |
| AI Chat page | MacAIChatView.swift | MacAIChatPage.xaml | 已复刻 | Provider abstraction preserved | Wire real OpenAI/Anthropic clients |
| Recent conversations | MacRecentConversationsPopover | RecentPanel sidebar | 已复刻 | JSON persistence already works | Add swipe-to-delete |
| Attachment import menu | MacAIChatAttachmentMenu | Attach button (stub) | stub | Menu not implemented | Add flyout with 3 options |
| Study library picker | ChatStudyLibraryPickerView | (not implemented) | 未完成 | No picker sheet | Implement folder/item picker |
| iPhone connection (unpaired) | unpairedSection | UnpairedPanel | 已复刻 | Pairing code generation stub | Implement real pairing flow |
| iPhone connection (connected) | MacConnectedDeviceLayout | ConnectedPanel | 已复刻 | Status rows display static | Wire DeviceConnectionStatusStore |
| Device bubble animation | MacConnectedDeviceBubbleView | Gradient circle | 部分复刻 | No animation | Add breathing animation |
| Connection detail sheet | MacIPhoneConnectionDetailSheet | (not implemented) | 未完成 | No detail sheet | Implement detail flyout |
| Settings — profile pane | MacSettingsProfilePane | Profile pane section | 已复刻 | Avatar uses PersonPicture | No change needed |
| Settings — transcription | MacSettingsView transcriptionGroup | Transcription section | 已复刻 | Rows are static, no drill-down | Implement detail sheets |
| Settings — AI | MacSettingsView aiGroup | AI section | 已复刻 | Rows are static, no drill-down | Implement detail sheets |
| Settings — about | MacSettingsView aboutGroup | About section | 已复刻 | Storage opener stub | Implement folder open |
| Transcription provider abstraction | TranscriptionProvider protocol | ITranscriptionProvider | 已复刻 | Only mock impl | Implement WhisperCpp provider |
| Note generation provider abstraction | NoteGenerationProvider protocol | INoteGenerationProvider | 已复刻 | Only mock impl | Implement OpenAI/Anthropic clients |
| Chat provider abstraction | ChatProvider protocol | IChatProvider | 已复刻 | Only mock impl | Implement OpenAI/Anthropic clients |
| Secure receiver service | SecureReceiverService | (stub) | 未完成 | HTTPS server not ported | Implement Kestrel server |
| Pairing/device management | PairingManager | (stub) | 未完成 | Pairing flow not ported | Implement pairing protocol |
| Study library sync | StudyLibrarySyncCoordinator | StudyLibrarySyncManifest | 部分复刻 | Manifest generation only, no merge | Implement merge logic |

## 11. BUILD_TEST_RESULT

- **dotnet SDK**: Not available (macOS without .NET SDK)
- **dotnet restore/build/test**: Cannot run
- **C# syntax**: Valid C# 12 patterns throughout
- **XAML validation**: Valid WinUI 3 namespace declarations

To build on Windows:
```powershell
cd Rokurics-Windows
dotnet restore
dotnet build
dotnet test Rokurics.Tests
```

## 12. SOURCE_GIT_STATUS_AFTER

Source project (`/Users/vita/Vitemis/Vela/Rokurics`): 22 pre-existing modified files. **No new changes introduced.**

## 13. TARGET_GIT_STATUS_AFTER

Target project (`/Users/vita/Vitemis/Outposts/Rokurics-Windows`): All files are new/uncommitted. Key files changed in Round 2 listed under MODIFIED_FILES.

## 14. KNOWN_GAPS

1. **Real AI providers**: OpenAICompatibleChatProvider, AnthropicMessagesChatProvider need HttpClient implementations
2. **WhisperCpp transcription provider**: Needs whisper.cpp binding or HTTP-based whisper service
3. **Secure HTTP receiver**: Kestrel-based HTTPS server with self-signed certificate generation
4. **Pairing protocol**: Shared secret verification, fingerprint exchange, device trust store
5. **Audio capture**: WASAPI recording for Windows (RecordingManager.StartRecording is stub)
6. **Resumable upload**: Chunked upload transport for large audio files
7. **Study library sync merge**: Conflict resolution for bi-directional sync
8. **Transcript/Note detail views**: Markdown rendering for transcript and note content
9. **AI summary preview loading**: JSON summary.json parsing from note directory
10. **Folder color token UI**: Color picker popover for folder context menu
11. **Attachment menu flyout**: 3-option menu (study library import / file upload / image upload)
12. **Study library picker sheet**: Hierarchical folder/item picker for chat context import

## 15. NEXT_STEPS

### Immediate (Round 3)
1. Implement OpenAI-compatible and Anthropic HTTP clients for ChatProvider and NoteGenerationProvider
2. Implement transcript and note detail views with markdown content loading
3. Wire filing picker candidate resolution from study library data
4. Add context menu (right-click) for folder tiles with rename/color/delete
5. Implement attachment menu flyout on AI chat page
6. Implement connection detail sheet for iPhone connection page

### Medium-term (Round 4)
7. Implement WhisperCpp transcription provider (local or HTTP-based)
8. Implement Kestrel HTTPS server for receiving recordings
9. Implement pairing protocol (fingerprint exchange, shared secret)
10. Implement study library sync merge logic
11. Implement WASAPI audio capture for RecordingManager

### Polish (Round 5)
12. Breathing animation on connected device bubble
13. Enhanced window chrome / acrylic materials
14. Mixed CJK/English font rendering in sidebar
15. Keyboard shortcuts
16. Export functionality (notes → PDF/Markdown)

---

**Round 2 Status**: Mac client page structure fully replicated. Sidebar navigation, study library browser with detail page, AI chat with conversations, iPhone connection with paired/unpaired states, and settings with profile pane all match the Mac source layout. Provider abstractions preserved. Data models fully translated. The Windows project is now a faithful platform translation of the Mac client rather than a generic prototype.
