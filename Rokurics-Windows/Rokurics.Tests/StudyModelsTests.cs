using Xunit;
using Rokurics.Models;
using Rokurics.Services;
using Rokurics.ViewModels;

namespace Rokurics.Tests;

// ── StudyFilingPath Tests ────────────────────────────────────────

public class StudyFilingPathTests
{
    [Fact]
    public void Empty_Path_Returns_IsEmpty_True()
    {
        var path = new StudyFilingPath();
        Assert.True(path.IsEmpty);
    }

    [Fact]
    public void Path_With_Type_Returns_IsEmpty_False()
    {
        var path = new StudyFilingPath(type: "数学");
        Assert.False(path.IsEmpty);
    }

    [Fact]
    public void DisplaySummary_Joins_Non_Null_Components()
    {
        var path = new StudyFilingPath(type: "数学", subject: "线性代数");
        Assert.Equal("数学 / 线性代数", path.DisplaySummary);
    }

    [Fact]
    public void DisplaySummary_Empty_Path_Returns_Uncategorized()
    {
        var path = new StudyFilingPath();
        Assert.Equal(StudyFilingPath.UncategorizedTitle, path.DisplaySummary);
    }

    [Fact]
    public void SuggestedTitle_Uses_Subject_Chapter_Topic()
    {
        var path = new StudyFilingPath(type: "数学", subject: "线性代数", chapter: "矩阵");
        Assert.Equal("线性代数 · 矩阵", path.SuggestedTitle("fallback"));
    }

    [Fact]
    public void ValueFor_Returns_Correct_Level()
    {
        var path = new StudyFilingPath(type: "A", subject: "B");
        Assert.Equal("A", path.ValueFor("type"));
        Assert.Equal("B", path.ValueFor("subject"));
        Assert.Null(path.ValueFor("chapter"));
    }
}

// ── StudyItemMetadata Tests ──────────────────────────────────────

public class StudyItemMetadataTests
{
    [Fact]
    public void RecordingBundleItemId_Generates_Correctly()
    {
        var id = StudyItemMetadata.RecordingBundleItemId("rec-123");
        Assert.StartsWith("item_recording_", id);
        Assert.Contains("rec-123", id);
    }

    [Fact]
    public void DefaultForRecording_Creates_Valid_Item()
    {
        var recording = new RecordingMetadata(
            id: "test-id", title: "测试录音", fileName: "test.m4a",
            relativeAudioPath: "Recordings/test.m4a",
            relativeMetadataPath: "Metadata/test.json",
            createdAt: DateTime.UtcNow, endedAt: DateTime.UtcNow,
            duration: TimeSpan.FromMinutes(5),
            format: "m4a", codec: "AAC", sampleRate: 16000,
            channels: 1, bitrate: 64000, fileSize: 1024,
            uploadStatus: "localOnly", transcriptionStatus: "notStarted",
            noteStatus: "notStarted", tags: new List<string>());

        var item = StudyItemMetadata.DefaultForRecording(recording);
        Assert.Equal("item_recording_test-id", item.ItemId);
        Assert.Equal(StudyItemKind.RecordingBundle, item.Kind);
        Assert.Equal("测试录音", item.Title);
        Assert.Equal(TimeSpan.FromMinutes(5), item.Duration);
    }
}

// ── AudioFileStore Tests ─────────────────────────────────────────

public class AudioFileStoreTests
{
    [Fact]
    public void EnsureStorageDirectories_Creates_Dirs()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var store = new AudioFileStore(tempDir);
            store.EnsureStorageDirectories();
            Assert.True(Directory.Exists(Path.Combine(tempDir, "Recordings")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "Metadata")));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveAndLoadMetadata_Roundtrips()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var store = new AudioFileStore(tempDir);
            store.EnsureStorageDirectories();

            var metadata = new RecordingMetadata(
                id: "test-rec", title: "测试", fileName: "test.m4a",
                relativeAudioPath: "Recordings/test.m4a",
                relativeMetadataPath: "Metadata/test-rec.json",
                createdAt: DateTime.UtcNow, endedAt: DateTime.UtcNow,
                duration: TimeSpan.FromMinutes(3),
                format: "m4a", codec: "AAC", sampleRate: 16000,
                channels: 1, bitrate: 64000, fileSize: 2048,
                uploadStatus: "localOnly", transcriptionStatus: "notStarted",
                noteStatus: "notStarted", tags: new List<string>());

            store.SaveMetadata(metadata);
            var loaded = store.LoadMetadata("test-rec");

            Assert.NotNull(loaded);
            Assert.Equal("测试", loaded!.Title);
            Assert.Equal(TimeSpan.FromMinutes(3), loaded.Duration);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAllMetadata_Excludes_Deleted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var store = new AudioFileStore(tempDir);
            store.EnsureStorageDirectories();

            var active = new RecordingMetadata(
                id: "active", title: "Active", fileName: "a.m4a",
                relativeAudioPath: "Recordings/a.m4a", relativeMetadataPath: "Metadata/active.json",
                createdAt: DateTime.UtcNow, endedAt: DateTime.UtcNow,
                duration: TimeSpan.Zero, format: "m4a", codec: "AAC",
                sampleRate: 16000, channels: 1, bitrate: 64000, fileSize: 0,
                uploadStatus: "localOnly", transcriptionStatus: "notStarted",
                noteStatus: "notStarted", tags: new List<string>());

            var deleted = new RecordingMetadata(
                id: "deleted", title: "Deleted", fileName: "d.m4a",
                relativeAudioPath: "Recordings/d.m4a", relativeMetadataPath: "Metadata/deleted.json",
                createdAt: DateTime.UtcNow, endedAt: DateTime.UtcNow,
                duration: TimeSpan.Zero, format: "m4a", codec: "AAC",
                sampleRate: 16000, channels: 1, bitrate: 64000, fileSize: 0,
                uploadStatus: "localOnly", transcriptionStatus: "notStarted",
                noteStatus: "notStarted", tags: new List<string>(),
                isDeleted: true, deletedAt: DateTime.UtcNow);

            store.SaveMetadata(active);
            store.SaveMetadata(deleted);

            var all = store.LoadAllMetadata();
            Assert.Single(all);
            Assert.Equal("active", all[0].Id);

            var allWithDeleted = store.LoadAllMetadata(includeDeleted: true);
            Assert.Equal(2, allWithDeleted.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

// ── StudyLibraryBrowser Tests ────────────────────────────────────

public class StudyLibraryBrowserTests
{
    [Fact]
    public void Browse_At_Root_Returns_Folders()
    {
        var items = new List<StudyItemMetadata>
        {
            new() { ItemId = "1", Title = "录音1", Filing = new StudyFilingPath(type: "数学") },
            new() { ItemId = "2", Title = "录音2", Filing = new StudyFilingPath(type: "物理") },
        };

        var content = StudyLibraryBrowser.Browse(items, new List<StudyFolderMetadata>(), new StudyBrowserPath());

        Assert.Equal(2, content.Folders.Count);
        Assert.Empty(content.Items);
    }

    [Fact]
    public void Browse_At_Leaf_Returns_Items()
    {
        var items = new List<StudyItemMetadata>
        {
            new() { ItemId = "1", Title = "录音1", Filing = new StudyFilingPath(type: "数学", subject: "线代") },
        };

        var path = new StudyBrowserPath(new List<string> { "数学", "线代" });
        var content = StudyLibraryBrowser.Browse(items, new List<StudyFolderMetadata>(), path);

        Assert.Empty(content.Folders);
        Assert.NotEmpty(content.Items);
    }
}

// ── RecordingManager Tests ───────────────────────────────────────

public class RecordingManagerTests
{
    [Fact]
    public void StartRecording_Transitions_To_ConfiguringSession()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var audioStore = new AudioFileStore(tempDir);
            var studyStore = new StudyLibraryStore(audioStore);
            var manager = new RecordingManager(audioStore, studyStore);

            manager.StartRecording();

            Assert.True(manager.State == RokuricsRecordingState.Recording
                || manager.State == RokuricsRecordingState.ConfiguringSession);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FinalizeRecording_Creates_Metadata()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var audioStore = new AudioFileStore(tempDir);
            var studyStore = new StudyLibraryStore(audioStore);
            var manager = new RecordingManager(audioStore, studyStore);

            manager.StartRecording();
            manager.StopRecording();
            manager.FinalizeRecording("测试录音");

            Assert.Equal(RokuricsRecordingState.Saved, manager.State);
            Assert.NotEmpty(manager.Recordings);
            Assert.Equal("测试录音", manager.LatestRecordingMetadata?.Title);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ToggleRecording_From_Idle_Starts_Recording()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"rokurics-test-{Guid.NewGuid()}");
        try
        {
            var audioStore = new AudioFileStore(tempDir);
            var studyStore = new StudyLibraryStore(audioStore);
            var manager = new RecordingManager(audioStore, studyStore);

            manager.ToggleRecording();

            Assert.True(manager.State is RokuricsRecordingState.Recording
                or RokuricsRecordingState.ConfiguringSession);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}

// ── Provider Tests ───────────────────────────────────────────────

public class MockProviderTests
{
    [Fact]
    public async Task MockTranscriptionProvider_Returns_Transcript()
    {
        var provider = new MockTranscriptionProvider();
        var request = new TranscriptionRequest("test-1", "/fake/path.m4a");
        var result = await provider.TranscribeAsync(request);

        Assert.Equal("test-1", result.RecordingId);
        Assert.Contains("Mock Transcript", result.Text);
        Assert.Equal("mockTranscriptionProvider", result.ProviderId);
    }

    [Fact]
    public async Task MockNoteGenerationProvider_Returns_Note()
    {
        var provider = new MockNoteGenerationProvider();
        var request = new NoteGenerationRequest("test-1", "transcript text", noteTitle: "测试笔记");
        var result = await provider.GenerateNoteAsync(request);

        Assert.Equal("test-1", result.RecordingId);
        Assert.Contains("摘要", result.NoteMarkdown);
        Assert.Contains("重点", result.NoteMarkdown);
    }

    [Fact]
    public async Task MockChatProvider_Returns_Reply()
    {
        var provider = new MockChatProvider();
        var messages = new List<Models.ChatMessage>
        {
            new(Guid.NewGuid().ToString(), Models.ChatMessageRole.User, "你好")
        };
        var request = new ChatRequest(messages);
        var result = await provider.SendAsync(request);

        Assert.NotNull(result.Message);
        Assert.Equal(Models.ChatMessageRole.Assistant, result.Message.Role);
        Assert.Contains("你好", result.Message.Content);
    }
}

// ── OpenAICompatibleProvider Tests ───────────────────────────────

public class OpenAICompatibleNoteGenerationProviderTests
{
    [Fact]
    public void TranscriptInput_Prefers_Markdown()
    {
        var request = new NoteGenerationRequest("test", "plain text",
            transcriptMarkdown: "# markdown text");
        var result = OpenAICompatibleNoteGenerationProvider.TranscriptInput(request);
        Assert.Equal("# markdown text", result);
    }

    [Fact]
    public void TranscriptInput_FallsBack_To_PlainText()
    {
        var request = new NoteGenerationRequest("test", "plain text");
        var result = OpenAICompatibleNoteGenerationProvider.TranscriptInput(request);
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void TruncateTranscript_Under_Limit_Not_Truncated()
    {
        var (text, wasTruncated) = OpenAICompatibleNoteGenerationProvider
            .TruncateTranscript("short text", 12000);
        Assert.Equal("short text", text);
        Assert.False(wasTruncated);
    }

    [Fact]
    public void TruncateTranscript_Over_Limit_Truncated()
    {
        var longText = new string('a', 100);
        var (text, wasTruncated) = OpenAICompatibleNoteGenerationProvider
            .TruncateTranscript(longText, 50);
        Assert.Equal(50, text.Length);
        Assert.True(wasTruncated);
    }

    [Fact]
    public void CleanMarkdown_Strips_CodeFences()
    {
        var raw = "```markdown\n# Hello\nWorld\n```";
        var cleaned = OpenAICompatibleNoteGenerationProvider.CleanMarkdown(raw);
        Assert.Contains("# Hello", cleaned);
        Assert.DoesNotContain("```", cleaned);
    }

    [Fact]
    public void BuildUserPrompt_Contains_Required_Sections()
    {
        var request = new NoteGenerationRequest("test", "transcript", noteTitle: "Test");
        var prompt = OpenAICompatibleNoteGenerationProvider.BuildUserPrompt(request, "transcript", false);
        Assert.Contains("Test", prompt);
        Assert.Contains("录音笔记", prompt);
        Assert.Contains("摘要", prompt);
        Assert.Contains("大纲", prompt);
        Assert.Contains("重点", prompt);
        Assert.Contains("待复习问题", prompt);
        Assert.Contains("Kikaria", prompt);
    }
}

// ── ChatViewModel Tests ──────────────────────────────────────────

public class ChatViewModelTests
{
    [Fact]
    public void NewConversation_Creates_Empty_Conversation()
    {
        var vm = new ChatViewModel(new MockChatProvider());
        vm.NewConversationCommand.Execute(null);

        Assert.NotNull(vm.ActiveConversation);
        Assert.Empty(vm.ActiveMessages);
    }

    [Fact]
    public async Task SendMessage_Adds_User_And_Assistant_Messages()
    {
        var vm = new ChatViewModel(new MockChatProvider());
        vm.NewConversationCommand.Execute(null);
        vm.DraftMessage = "测试消息";

        await vm.SendMessageCommand.ExecuteAsync(null);

        Assert.NotEmpty(vm.ActiveMessages);
        Assert.Equal(Models.ChatMessageRole.User, vm.ActiveMessages[0].Role);
        Assert.Equal(Models.ChatMessageRole.Assistant, vm.ActiveMessages[1].Role);
    }

    [Fact]
    public void DeleteConversation_Removes_From_List()
    {
        var vm = new ChatViewModel(new MockChatProvider());
        vm.NewConversationCommand.Execute(null);
        var conv = vm.ActiveConversation!;
        vm.NewConversationCommand.Execute(null);

        vm.DeleteConversationCommand.Execute(conv);

        Assert.DoesNotContain(conv, vm.Conversations);
    }
}

// ── SettingsViewModel Tests ──────────────────────────────────────

public class SettingsViewModelTests
{
    [Fact]
    public void Default_Providers_Are_Mock()
    {
        var vm = new SettingsViewModel();
        Assert.Equal("Mock", vm.SelectedTranscriptionProvider);
        Assert.Equal("Mock", vm.SelectedNoteProvider);
        Assert.Equal("Mock", vm.SelectedChatProvider);
    }

    [Fact]
    public void Save_Command_Completes_Without_Error()
    {
        var vm = new SettingsViewModel();
        vm.SaveCommand.Execute(null);
        // Should not throw
    }

    [Fact]
    public void Provider_Options_Contain_Expected_Values()
    {
        var vm = new SettingsViewModel();
        Assert.Contains("Mock", vm.TranscriptionProviderOptions);
        Assert.Contains("Whisper.cpp", vm.TranscriptionProviderOptions);
        Assert.Contains("OpenAI-compatible", vm.NoteProviderOptions);
        Assert.Contains("Claude / Anthropic", vm.NoteProviderOptions);
    }
}

// ── ProviderFactory Tests ────────────────────────────────────────

public class ProviderFactoryTests
{
    [Fact]
    public void CreateChatProvider_Mock_Returns_MockChatProvider()
    {
        var settings = new AppSettings { ChatProvider = "Mock" };
        var provider = ProviderFactory.CreateChatProvider(settings);
        Assert.IsType<MockChatProvider>(provider);
    }

    [Fact]
    public void CreateChatProvider_OpenAI_Returns_OpenAICompatible()
    {
        var settings = new AppSettings
        {
            ChatProvider = "OpenAI-compatible",
            OpenAIBaseUrl = "http://localhost:1234/v1",
            OpenAIModelName = "test-model"
        };
        var provider = ProviderFactory.CreateChatProvider(settings);
        Assert.IsType<OpenAICompatibleChatProvider>(provider);
    }

    [Fact]
    public void CreateChatProvider_Anthropic_Returns_AnthropicMessages()
    {
        var settings = new AppSettings
        {
            ChatProvider = "Claude / Anthropic",
            AnthropicBaseUrl = "https://api.anthropic.com",
            AnthropicModelName = "claude-sonnet-4-6",
            AnthropicApiKey = "test-key"
        };
        var provider = ProviderFactory.CreateChatProvider(settings);
        Assert.IsType<AnthropicMessagesChatProvider>(provider);
    }

    [Fact]
    public void CreateNoteProvider_Mock_Returns_MockNoteGenerationProvider()
    {
        var settings = new AppSettings { NoteProvider = "Mock" };
        var provider = ProviderFactory.CreateNoteProvider(settings);
        Assert.IsType<MockNoteGenerationProvider>(provider);
    }
}
