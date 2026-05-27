using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rokurics.Models;
using Rokurics.Services;

namespace Rokurics.ViewModels;

/// <summary>
/// ViewModel for the AI chat page.
/// </summary>
public partial class ChatViewModel : ObservableObject
{
    private readonly IChatProvider _chatProvider;
    private readonly StudyLibraryStore _studyStore;

    [ObservableProperty] private List<ChatConversation> _conversations = new();
    [ObservableProperty] private ChatConversation? _activeConversation;
    [ObservableProperty] private string _draftMessage = "";
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string _greetingText = "你好！";
    [ObservableProperty] private bool _showConversationList;

    public ChatViewModel(IChatProvider? chatProvider = null, StudyLibraryStore? studyStore = null)
    {
        _chatProvider = chatProvider ?? new MockChatProvider();
        _studyStore = studyStore ?? new StudyLibraryStore();
        _greetingText = ChatGreeting.Current();
        LoadConversations();
    }

    public IChatProvider Provider => _chatProvider;
    public StudyLibraryStore StudyStore => _studyStore;

    public bool HasActiveConversation => ActiveConversation is not null;
    public List<ChatMessage> ActiveMessages => ActiveConversation?.Messages ?? new List<ChatMessage>();

    [RelayCommand]
    private void NewConversation()
    {
        ActiveConversation = new ChatConversation();
        ShowConversationList = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void SelectConversation(ChatConversation conversation)
    {
        ActiveConversation = conversation;
        ShowConversationList = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        var text = DraftMessage.Trim();
        if (string.IsNullOrEmpty(text) || IsGenerating || ActiveConversation is null) return;

        DraftMessage = "";
        ErrorMessage = null;

        var userMsg = new ChatMessage(Guid.NewGuid().ToString(), ChatMessageRole.User, text);
        ActiveConversation = ActiveConversation.WithMessage(userMsg);
        OnPropertyChanged(nameof(ActiveMessages));

        IsGenerating = true;
        try
        {
            var request = new ChatRequest(ActiveConversation.Messages);

            if (_chatProvider.SupportsStreaming)
            {
                // Streaming: accumulate tokens in-place on a placeholder assistant message
                var assistantMsg = new ChatMessage(Guid.NewGuid().ToString(), ChatMessageRole.Assistant, "");
                ActiveConversation = ActiveConversation.WithMessage(assistantMsg);
                OnPropertyChanged(nameof(ActiveMessages));

                var fullContent = "";
                await foreach (var token in _chatProvider.StreamAsync(request))
                {
                    fullContent += token;
                    // Update the last (assistant) message content in-place
                    var messages = ActiveConversation.Messages;
                    var lastIdx = messages.Count - 1;
                    messages[lastIdx] = new ChatMessage(assistantMsg.Id, ChatMessageRole.Assistant, fullContent);
                    OnPropertyChanged(nameof(ActiveMessages));
                }
            }
            else
            {
                var result = await _chatProvider.SendAsync(request);
                ActiveConversation = ActiveConversation.WithMessage(result.Message);
                OnPropertyChanged(nameof(ActiveMessages));
            }

            if (ActiveConversation.TitleSource == ChatConversationTitleSource.Fallback
                && ActiveConversation.Messages.Count(m => m.Role == ChatMessageRole.User) >= 1)
            {
                try
                {
                    var titleRequest = new ChatTitleRequest(
                        new List<string> { userMsg.Content },
                        ActiveConversation.Messages.LastOrDefault(m => m.Role == ChatMessageRole.Assistant)?.Content ?? "");
                    var title = await _chatProvider.GenerateConversationTitleAsync(titleRequest);
                    ActiveConversation.Title = title;
                    ActiveConversation.TitleSource = ChatConversationTitleSource.AiGenerated;
                }
                catch { }
            }

            SaveConversations();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void DeleteConversation(ChatConversation conversation)
    {
        Conversations.Remove(conversation);
        if (ActiveConversation?.Id == conversation.Id)
            ActiveConversation = null;
        SaveConversations();
    }

    [RelayCommand]
    private void ToggleConversationList()
    {
        ShowConversationList = !ShowConversationList;
    }

    private void LoadConversations()
    {
        // Conversations are persisted as JSON in the study directory
        var convDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "conversations");
        if (!Directory.Exists(convDir)) return;

        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        foreach (var file in Directory.GetFiles(convDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var conv = System.Text.Json.JsonSerializer.Deserialize<ChatConversation>(json, options);
                if (conv is not null) Conversations.Add(conv);
            }
            catch { }
        }
        Conversations = Conversations.OrderByDescending(c => c.UpdatedAt).ToList();
    }

    private void SaveConversations()
    {
        var convDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Rokurics", "conversations");
        Directory.CreateDirectory(convDir);

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        foreach (var conv in Conversations)
        {
            var path = Path.Combine(convDir, $"{conv.Id}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(conv, options);
            File.WriteAllText(path, json);
        }
    }
}

public static class ChatGreeting
{
    public static string Current()
    {
        var hour = DateTime.Now.Hour;
        var period = hour switch
        {
            >= 5 and < 12 => "早上好",
            >= 12 and < 18 => "下午好",
            _ => "晚上好"
        };
        var name = Environment.UserName;
        return $"{name}，{period}！";
    }
}
