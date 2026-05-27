# Rokurics Windows/.NET Validation Checklist

Generated: 2026-05-26
Host: macOS (no .NET SDK) — all items require Windows x64 with .NET 9 SDK.

## Prerequisites
- [ ] Windows 10/11 (x64) with .NET 9 SDK installed
- [ ] Visual Studio 2022+ or `dotnet` CLI in PATH
- [ ] Git for Windows (clone repo + submodules if applicable)

## Build Validation
- [ ] `dotnet restore Rokurics.sln` — restores NuGet packages
- [ ] `dotnet build Rokurics.sln` — compiles without errors
- [ ] `dotnet test Rokurics.Tests` — existing tests pass
- [ ] Verify no `NotImplementedException` in runtime paths during normal navigation

## Core UI Validation (WinUI 3 / Windows App SDK)

| Component | File | Validation |
|-----------|------|------------|
| App launch | App.xaml.cs | Window opens, title "Rokurics" visible |
| Sidebar navigation | SidebarView.xaml | 3 items render with icons, profile button at bottom |
| Navigation dispatch | MainWindow.xaml.cs | Clicking sidebar items navigates to correct page |
| Acrylic sidebar | SidebarView.xaml | AcrylicBackgroundFillColorDefaultBrush renders correctly |
| Glass card styles | App.xaml | RokuricsGlassCardStyle renders with acrylic |
| Card styles | App.xaml | RokuricsCardStyle renders with border/corner radius |

## Page Validation

### Study Library (MacStudyLibraryPage)
- [ ] Page loads with folder tile grid and recording cards
- [ ] Empty state renders when no items exist
- [ ] Breadcrumb navigation works (back button, path segments)
- [ ] Folder tiles show color dots for non-default colors
- [ ] Recording cards show title, date, duration, action buttons
- [ ] Detail panel shows on card click (title, subtitle, filing picker, file status)
- [ ] Filing picker autocomplete resolves candidates from study library data
- [ ] "Create new value" flow for filing categories
- [ ] Transcript detail view loads markdown content
- [ ] Note detail view loads note and summary
- [ ] Trash dialog opens and shows trashed recordings
- [ ] Sync status bar shows last sync time
- [ ] Folder context menu (rename, color, delete)

### AI Chat (MacAIChatPage)
- [ ] Page loads with greeting text
- [ ] Message input accepts text, send button triggers response
- [ ] Messages render as bubbles (user right, assistant left)
- [ ] Recent conversations sidebar toggles
- [ ] New conversation button clears state
- [ ] Attach button shows 3-option flyout menu
- [ ] Study library picker imports context into chat
- [ ] Loading bar shows during generation
- [ ] Error banner shows on provider failure
- [ ] Conversation persistence (JSON in conversations/ directory)

### iPhone Connection (MacIPhoneConnectionPage)
- [ ] Unpaired panel shows address, port, fingerprint
- [ ] Start pairing generates 6-digit code
- [ ] Breathing animation on device bubble
- [ ] Copy pairing info copies to clipboard
- [ ] Connected panel shows device info card
- [ ] Sync now triggers (placeholder)
- [ ] Connection detail sheet opens
- [ ] Paired devices sheet shows device list
- [ ] Disconnect resets to unpaired state

### Settings (MacSettingsPage)
- [ ] Profile pane shows display name, handle, avatar initials
- [ ] Edit profile dialog works
- [ ] Transcription section: Provider picker dialog
- [ ] Transcription section: Model picker dialog
- [ ] Transcription section: Auth & Test dialog (whisper paths, status, validation)
- [ ] AI section: Provider picker with ProviderDetailCard
- [ ] AI section: Model picker with metadata (owned_by, created) from fetched candidates
- [ ] AI section: API settings dialog (base URL, key, version, presets)
- [ ] AI section: Test dialog (connection, model, generation)
- [ ] About section: Storage folder opens
- [ ] About section: Privacy policy displays
- [ ] About section: Copyright displays
- [ ] Settings persist to settings.json

### Provider Detail (MacProviderDetailPage)
- [ ] Page loads with back button to settings
- [ ] ProviderDetailCard renders for Transcription (Whisper.cpp models)
- [ ] ProviderDetailCard renders for NoteGeneration (OpenAI/Anthropic)
- [ ] ProviderDetailCard renders for Chat
- [ ] Model list populates from server or shows fallback
- [ ] Refresh models button works
- [ ] Model metadata shown (owned_by, created)
- [ ] Preset selector populates base URL (OpenAI-compatible)
- [ ] Test connection sends HTTP request
- [ ] Validate config checks required fields
- [ ] Save settings persists to settings.json
- [ ] Endpoint/security info panel reflects current state

## Service Validation (requires real API keys or mock)

| Service | Interface | Windows Implementation | Test |
|---------|-----------|----------------------|------|
| OpenAI-compatible Note Gen | INoteGenerationProvider | OpenAICompatibleNoteGenerationProvider | Send note gen request, verify markdown output |
| Anthropic Messages Note Gen | INoteGenerationProvider | AnthropicMessagesNoteGenerationProvider | Send note gen request, verify markdown output |
| OpenAI-compatible Chat | IChatProvider | OpenAICompatibleChatProvider | Send chat messages, verify responses |
| Anthropic Messages Chat | IChatProvider | AnthropicMessagesChatProvider | Send chat messages, verify responses |
| Whisper.cpp Transcription | ITranscriptionProvider | WhisperCppProvider | Transcribe audio file, verify text output |
| Mock providers | — | Mock*Providers | All mock providers return valid stub responses |

## Infrastructure Validation (Windows-specific)

| Component | Interface/Class | Dependencies | Validation |
|-----------|---------------|--------------|------------|
| Kestrel HTTPS server | IKestrelReceiverService | Microsoft.AspNetCore.App | Start server on port 8787, verify HTTPS endpoint, check self-signed cert |
| Self-signed certificate | SelfSignedCertificateHelper | System.Security.Cryptography | Generate .pfx, verify fingerprint SHA-256 |
| Pairing protocol | IPairingService | Kestrel server running | Generate 6-digit code, verify, complete pairing |
| WASAPI audio capture | IWindowsAudioCapture | NAudio NuGet package | Enumerate devices, start/stop recording, verify .wav output |
| RecordingManager (audio) | RecordingManager | WindowsAudioCapture | StartRecording, StopRecording, elapsed timer, save to AudioFileStore |
| Study library sync merge | StudyLibrarySyncMerger | StudyLibraryStore | Pull remote manifest, merge, resolve conflicts |
| Recording upload | IRecordingUploadClient | Kestrel server | Upload metadata + audio, verify progress events |

## NuGet Package Restoration
Packages expected in .csproj:
- [ ] `Microsoft.WindowsAppSDK` (WinUI 3)
- [ ] `CommunityToolkit.Mvvm` (MVVM source generators)
- [ ] `CommunityToolkit.WinUI.UI` (optional UI helpers)
- [ ] `NAudio` (WASAPI audio capture)
- [ ] `Microsoft.AspNetCore.App` (Kestrel HTTPS server)

## Runtime Edge Cases

### Round 2 Additions — Markdown Rendering
- [ ] Transcript markdown renders with section headings (══, ──), lists (•), block quotes (▌)
- [ ] Note markdown renders with provider/model metadata extracted from frontmatter
- [ ] Transcript result JSON (segments with timing) loads and displays segment count/duration
- [ ] MarkdownRenderer.ExtractSummary produces clean 300-char summaries
- [ ] MarkdownRenderer.Render handles code blocks (fenced), tables, ordered lists
- [ ] Transcript view shows "转写内容尚未生成" when no transcript file exists
- [ ] Note view shows "AI 总结尚未生成" when no note file exists
- [ ] Summary preview card loads from summary.json (short_summary, key_points)

### Round 2 Additions — Sync Merge Logic
- [ ] SyncChecksum.Compute produces SHA-256 hex matching Apple source format
- [ ] SyncChecksum.Verify validates checksum comparison (case-insensitive)
- [ ] SyncSanitizer.FilterCustomProperties strips apiKey, secret, hmac, pairing, prompt, debug keys
- [ ] SyncInventoryBuilder.Build creates inventory from StudyLibraryStore + AudioFileStore
- [ ] SyncInventory.InventoryHash is deterministic (sorted keys, consistent serialization)
- [ ] SyncDiffPlanner.Plan computes correct 4-way diff (recordings, folders, studyItems, artifacts)
- [ ] Conflict detection: BothChangedAfterSync detects true conflicts when both sides modified after last sync
- [ ] Conflict detection: tombstone wins when one side deleted (last-write-wins)
- [ ] Artifact auto-download: transcript/note artifacts auto-download, audio does not
- [ ] SyncDiffPlan.TotalActions counts all action categories correctly
- [ ] StudyLibrarySyncMerger.SyncAsync runs full cycle: diff → plan → apply

### Round 2 Additions — Provider Validation
- [ ] ProviderValidator.ValidateBaseUrl accepts http:// and https:// URLs
- [ ] ProviderValidator.ValidateBaseUrl rejects empty/invalid URLs
- [ ] ProviderValidator.ValidateBaseUrl recognizes localhost as valid
- [ ] ProviderValidator.ValidateApiKey detects Anthropic prefix (sk-ant-)
- [ ] ProviderValidator.ValidateApiKey detects OpenAI prefix (sk-)
- [ ] ProviderValidator.ValidateModelName rejects newlines/null chars (injection prevention)
- [ ] ProviderValidator.ValidateConfiguration returns all issues for a full config
- [ ] ProviderValidator.ValidateChatRequest checks messages, maxTokens, temperature ranges
- [ ] ProviderValidator.ValidateAnthropicMessageRequest checks system/user content and ranges
- [ ] ProviderValidator.CheckEndpointReachableAsync handles timeout, HTTP errors, network errors
- [ ] ProviderValidator.DiagnosticSummary masks API keys in output
- [ ] ValidationResult.ToString produces correct symbols (✓ ⚠ ✗)

### Round 2 Additions — Folder Color Picker
- [ ] FolderColorPicker renders 12-color swatch grid (4x3)
- [ ] Clicking a swatch selects it and updates preview
- [ ] Color name and token update in preview panel
- [ ] "恢复默认" resets to Default (blue)
- [ ] "确认" fires ColorSelected event with chosen token
- [ ] Picker integrates into folder context menu via ContentDialog
- [ ] Color change persists via StudyLibraryStore.SetFolderColor
- [ ] Tooltip shows Chinese + English color name on hover

## Runtime Edge Cases
- [ ] App launches without settings.json (creates defaults)
- [ ] App launches with corrupted settings.json (handles deserialization failure)
- [ ] Network unavailable — model list fetch fails gracefully with fallback
- [ ] Empty study library — empty state renders instead of error
- [ ] Missing audio file referenced in metadata — shows "缺失" status
- [ ] Missing transcript/note file — shows appropriate placeholder text
- [ ] Kestrel port already in use — error handled gracefully
- [ ] WASAPI no microphone — device list empty, recording disabled

## Regression Check (Round 1 features preserved)
- [ ] RecordingManager state machine (Idle → Recording → Paused → Stopped → Saved)
- [ ] StudyLibraryStore CRUD operations
- [ ] AudioFileStore file resolution (relative → absolute paths)
- [ ] Study filing hierarchy (4-level: type/subject/chapter/topic)
- [ ] StudyLibraryBrowser tree navigation
- [ ] ConnectionSyncStateStores persistence (device status, sync state, local network state)
- [ ] All Converters (InvertBool, NullToCollapsed, ChatRoleToAlignment, ChatRoleToBackground)
- [ ] RokuricsColors palette correctness

### Round 2 New Files Regression
- [ ] Helpers/MarkdownRenderer.cs — no namespace conflicts, Render/ExtractSummary/ExtractNoteMetadata work correctly
- [ ] Helpers/TranscriptResult — JSON deserialization handles missing fields
- [ ] Services/ProviderValidator.cs — all static methods return correct ValidationResult
- [ ] Services/StudyLibrarySyncMerger.cs — new classes (SyncChecksum, SyncSanitizer, SyncInventory, SyncDiffPlanner, SyncInventoryBuilder) compile and are accessible
- [ ] Views/FolderColorPicker.xaml — renders correctly, swatch grid interactive
- [ ] Views/ProviderDetailCard.xaml — renders in ContentDialog and standalone
- [ ] Views/MacProviderDetailPage.xaml — back navigation works, endpoint info updates

---

**Status as of 2026-05-26 (Round 2)**: All code written statically on macOS. 0 items validated on Windows.
**Round 2 additions**: MarkdownRenderer, sync merge checksums/diff planner/inventory builder, provider validation, folder color picker, transcript JSON loading.
**To start validation**: Clone this repo on a Windows machine with .NET 9 SDK, run `dotnet restore && dotnet build`, then work through the checklist.
