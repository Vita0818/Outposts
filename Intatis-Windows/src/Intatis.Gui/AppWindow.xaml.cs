using System.Text;
using System.Windows;
using System.Windows.Controls;
using Intatis.Windows.Shared;

namespace Intatis.Gui;

public partial class AppWindow : Window
{
    private IntatisConfig _config;
    private readonly IPermissionResponder _permissionResponder;
    private ConversationSession _conversation;
    private readonly SessionEventLog _codeEventLog;
    private readonly SessionEventLog _coworkEventLog;
    private CodeAgentSession _codeSession;
    private CoworkEngine _coworkEngine;
    private readonly List<ChatAttachment> _chatAttachments = [];
    private readonly List<ChatAttachment> _codeAttachments = [];
    private readonly List<ChatAttachment> _coworkAttachments = [];
    private string _runtimeModel;
    private string? _runtimeReasoning;
    private string _workspace;
    private bool _runtimeUsage;

    public AppWindow()
    {
        InitializeComponent();

        _config = ConfigStore.Load();
        _permissionResponder = new WindowPermissionResponder(this);
        _codeEventLog = new SessionEventLog("gui-code");
        _coworkEventLog = new SessionEventLog("gui-cowork");

        _workspace = ResolveWorkspace(_config.Workspace, ConfigurationFallback: true);
        _runtimeModel = _config.Model;
        _runtimeReasoning = _config.Reasoning;
        _runtimeUsage = _config.IncludeUsage;

        _conversation = new ConversationSession(_config);
        _codeSession = CreateCodeSession(_workspace);
        _coworkEngine = CreateCoworkEngine(_workspace);

        BaseUrlBox.Text = _config.BaseUrl;
        ModelBox.Text = _config.Model;
        ApiKeyBox.Text = _config.ApiKey;
        ReasoningBox.Text = _config.Reasoning ?? string.Empty;
        UsageCheckBox.IsChecked = _runtimeUsage;
        WorkspaceBox.Text = _workspace;

        AppendLine(ChatHistory, $"Loaded config: {_config}");
        AppendLine(ChatHistory, $"Chat model: {_runtimeModel}, reasoning: {_runtimeReasoning ?? "(off)"}");
        AppendLine(CodeOutput, $"Code workspace: {_workspace}");
        AppendLine(CoworkHistory, $"Cowork workspace: {_workspace}");
    }

    private void OnSaveConfigClick(object sender, RoutedEventArgs e)
    {
        var reasoning = ReasoningBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(reasoning) &&
            !CommandParser.TryNormalizeReasoning(reasoning, out var normalizedReasoning))
        {
            AppendLine(ChatHistory, "Invalid reasoning value, keeping previous value.");
            reasoning = _runtimeReasoning ?? string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(reasoning))
        {
            CommandParser.TryNormalizeReasoning(reasoning, out reasoning);
        }

        var resolvedWorkspace = CommandParser.ExpandTilde(WorkspaceBox.Text.Trim());
        var next = _config.CloneWith(
            baseUrl: BaseUrlBox.Text.Trim(),
            model: string.IsNullOrWhiteSpace(ModelBox.Text.Trim()) ? _config.Model : ModelBox.Text.Trim(),
            apiKey: ApiKeyBox.Text.Trim(),
            reasoning: reasoning,
            workspace: resolvedWorkspace,
            includeUsage: UsageCheckBox.IsChecked == true);

        ConfigStore.Save(next);

        _config = next;
        _runtimeModel = next.Model;
        _runtimeReasoning = next.Reasoning;
        _runtimeUsage = next.IncludeUsage;

        try
        {
            _workspace = ResolveWorkspace(next.Workspace, ConfigurationFallback: true);
            WorkspaceBox.Text = _workspace;
        }
        catch (Exception ex)
        {
            AppendLine(ChatHistory, ex.Message);
            _workspace = ResolveWorkspace(null, ConfigurationFallback: true);
            WorkspaceBox.Text = _workspace;
        }

        _conversation = new ConversationSession(_config);
        _codeSession = CreateCodeSession(_workspace);
        _coworkEngine = CreateCoworkEngine(_workspace);

        AppendLine(ChatHistory, "Configuration saved.");
    }

    private void OnApplyWorkspace(object sender, RoutedEventArgs e)
    {
        var requested = WorkspaceBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(requested))
        {
            MessageBox.Show(this, "Please enter a workspace path.", "Intatis Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _workspace = ResolveWorkspace(requested, ConfigurationFallback: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Intatis Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        WorkspaceBox.Text = _workspace;
        AppendLine(CodeOutput, $"Workspace changed: {_workspace}");
        AppendLine(CoworkHistory, $"Workspace changed: {_workspace}");

        _codeSession = CreateCodeSession(_workspace);
        _coworkEngine = CreateCoworkEngine(_workspace);
    }

    private async void OnChatSend(object sender, RoutedEventArgs e)
    {
        var text = ChatInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        ChatInput.Clear();
        AppendLine(ChatHistory, $"You: {text}");

        if (HandleChatSlashCommand(text))
            return;

        try
        {
            var (withText, imageAttachments) = PrepareQueuedMessage(text);
            var (message, latency, usage) = await _conversation.SendUserMessageAsync(
                withText,
                model: _runtimeModel,
                reasoning: _runtimeReasoning,
                attachments: imageAttachments);

            AppendLine(ChatHistory, $"Assistant: {message.Content}");
            if (_runtimeUsage && !string.IsNullOrWhiteSpace(usage))
                AppendLine(ChatHistory, $"Usage: {usage}");
            AppendLine(ChatHistory, $"({latency.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            AppendLine(ChatHistory, $"Error: {ex.Message}");
        }
    }

    private async void OnCodeRun(object sender, RoutedEventArgs e)
    {
        var line = CodeInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(line))
            return;

        CodeInput.Clear();
        AppendLine(CodeOutput, $"> {line}");

        if (line.StartsWith("/", StringComparison.Ordinal))
        {
            if (HandleCodeSlashCommand(line))
                return;
        }

        try
        {
            var (effectiveBody, imageAttachments) = PrepareQueuedMessage(line, _codeAttachments);
            var (reply, latency, usage) = await _codeSession.SendAsync(
                effectiveBody,
                model: _runtimeModel,
                reasoning: _runtimeReasoning,
                userGoal: "gui code mode",
                images: imageAttachments,
                includeUsage: _runtimeUsage);

            AppendLine(CodeOutput, $"Assistant: {reply}");
            if (_runtimeUsage && !string.IsNullOrWhiteSpace(usage))
                AppendLine(CodeOutput, $"Usage: {usage}");
            AppendLine(CodeOutput, $"({latency.TotalMilliseconds:F0}ms)");
        }
        catch (Exception ex)
        {
            AppendLine(CodeOutput, $"Error: {ex.Message}");
        }
    }

    private async void OnCoworkSend(object sender, RoutedEventArgs e)
    {
        var line = CoworkInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(line))
            return;

        CoworkInput.Clear();
        AppendLine(CoworkHistory, $"> {line}");

        if (line.StartsWith("/", StringComparison.Ordinal))
        {
            if (HandleCoworkSlashCommand(line))
                return;
        }

        string? targetAgent = null;
        var body = line;
        if (line.StartsWith("@", StringComparison.Ordinal))
        {
            var raw = line.AsSpan(1).ToString();
            var split = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length > 0)
            {
                targetAgent = split[0];
                body = split.Length > 1 ? split[1] : string.Empty;
            }
        }

        try
        {
            var (effectiveBody, imageAttachments) = PrepareQueuedMessage(body, _coworkAttachments);
            var reply = await _coworkEngine.SendAsync(
                effectiveBody,
                targetAgent,
                reasoning: _runtimeReasoning,
                images: imageAttachments,
                includeUsage: _runtimeUsage);
            if (!string.IsNullOrWhiteSpace(reply))
            {
                AppendLine(CoworkHistory, $"[{targetAgent ?? "next"}] {reply}");
            }
        }
        catch (Exception ex)
        {
            AppendLine(CoworkHistory, $"Error: {ex.Message}");
        }
    }

    private bool HandleChatSlashCommand(string input)
    {
        if (!input.StartsWith("/", StringComparison.Ordinal))
            return false;

        var tokens = CommandParser.ParseTokens(input[1..]);
        if (tokens.Count == 0)
            return true;

        var command = tokens[0].ToLowerInvariant();
        var arg = tokens.Skip(1).ToList();

        switch (command)
        {
            case "help":
                AppendLine(ChatHistory, "Chat commands:");
                AppendLine(ChatHistory, "/help                     show this help");
                AppendLine(ChatHistory, "/clear                    clear chat history");
                AppendLine(ChatHistory, "/mode <chat|code|cowork> switch mode");
                AppendLine(ChatHistory, "/model [name]             show or set model");
                AppendLine(ChatHistory, "/reasoning [minimal|low|medium|high|off]  show or set reasoning");
                AppendLine(ChatHistory, "/attach <path>             attach image/text for next message");
                AppendLine(ChatHistory, "/attach clear              clear attachments");
                AppendLine(ChatHistory, "/attach list               list attachments");
                AppendLine(ChatHistory, "/config                   print runtime config");
                AppendLine(ChatHistory, "/exit                     close app");
                return true;
            case "clear":
                _conversation.Clear();
                AppendLine(ChatHistory, "chat session cleared");
                return true;
            case "mode":
                if (arg.Count == 0)
                {
                    AppendLine(ChatHistory, "usage: /mode <chat|code|cowork>");
                    return true;
                }

                if (!Enum.TryParse(arg[0], true, out IntatisMode mode))
                {
                    AppendLine(ChatHistory, "usage: /mode <chat|code|cowork>");
                    return true;
                }

                SwitchModeTab(mode);
                return true;
            case "model":
                if (arg.Count == 0)
                {
                    AppendLine(ChatHistory, $"model: {_runtimeModel}");
                    return true;
                }

                _runtimeModel = arg[0];
                AppendLine(ChatHistory, $"model: {_runtimeModel}");
                return true;
            case "reasoning":
                if (arg.Count == 0)
                {
                    AppendLine(ChatHistory, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                    return true;
                }

                if (!CommandParser.TryNormalizeReasoning(arg[0], out var normalizedReasoning))
                {
                    AppendLine(ChatHistory, "usage: /reasoning [minimal|low|medium|high|off]");
                    return true;
                }

                _runtimeReasoning = normalizedReasoning;
                AppendLine(ChatHistory, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                return true;
            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    AppendLine(ChatHistory, _chatAttachments.Count == 0
                        ? "no attachments queued"
                        : DescribeAttachmentQueue(_chatAttachments));
                    return true;
                }

                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _chatAttachments.Clear();
                    AppendLine(ChatHistory, "attachments cleared");
                    return true;
                }

                var attachPath = string.Join(' ', arg);
                var attachment = AttachmentLoader.Load(attachPath);
                if (!attachment.IsSuccess || attachment.Attachment is null)
                {
                    AppendLine(ChatHistory, attachment.Failure ?? "failed to load attachment");
                    return true;
                }

                _chatAttachments.Add(attachment.Attachment);
                AppendLine(ChatHistory, $"attached {attachment.Attachment.Name}");
                return true;
            case "config":
                AppendLine(ChatHistory, $"endpoint : {_config.BaseUrl}");
                AppendLine(ChatHistory, $"model    : {_runtimeModel}");
                AppendLine(ChatHistory, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                AppendLine(ChatHistory, $"workspace: {_workspace}");
                AppendLine(ChatHistory, $"usage    : {(_runtimeUsage ? "on" : "off")}");
                AppendLine(ChatHistory, $"apikey   : {(string.IsNullOrWhiteSpace(_config.ApiKey) ? "(unset)" : "(set, hidden)")}");
                return true;
            case "exit":
            case "quit":
                Close();
                return true;
            default:
                AppendLine(ChatHistory, $"unknown command: /{command}");
                return true;
        }
    }

    private bool HandleCodeSlashCommand(string input)
    {
        var tokens = CommandParser.ParseTokens(input[1..]);
        if (tokens.Count == 0)
            return true;

        var command = tokens[0].ToLowerInvariant();
        var arg = tokens.Skip(1).ToList();

        switch (command)
        {
            case "help":
                AppendLine(CodeOutput, "Code commands:");
                AppendLine(CodeOutput, "/help");
                AppendLine(CodeOutput, "/mode <chat|code|cowork>");
                AppendLine(CodeOutput, "/model [name]");
                AppendLine(CodeOutput, "/reasoning [minimal|low|medium|high|off]");
                AppendLine(CodeOutput, "/attach <path>");
                AppendLine(CodeOutput, "/attach clear");
                AppendLine(CodeOutput, "/attach list");
                AppendLine(CodeOutput, "/config");
                AppendLine(CodeOutput, "/workspace [path]");
                AppendLine(CodeOutput, "/clear");
                AppendLine(CodeOutput, "/exit");
                AppendLine(CodeOutput, "Send natural language; agent will choose tools.");
                return true;
            case "model":
                if (arg.Count == 0)
                {
                    AppendLine(CodeOutput, $"model: {_runtimeModel}");
                    return true;
                }

                _runtimeModel = arg[0];
                AppendLine(CodeOutput, $"model: {_runtimeModel}");
                return true;
            case "reasoning":
                if (arg.Count == 0)
                {
                    AppendLine(CodeOutput, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                    return true;
                }

                if (!CommandParser.TryNormalizeReasoning(arg[0], out var reasoning))
                {
                    AppendLine(CodeOutput, "usage: /reasoning [minimal|low|medium|high|off]");
                    return true;
                }

                _runtimeReasoning = reasoning;
                AppendLine(CodeOutput, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                return true;
            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    AppendLine(CodeOutput, _codeAttachments.Count == 0
                        ? "no attachments queued. usage: /attach <path>"
                        : DescribeAttachmentQueue(_codeAttachments));
                    return true;
                }

                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _codeAttachments.Clear();
                    AppendLine(CodeOutput, "code attachments cleared");
                    return true;
                }

                var codeAttachPath = string.Join(' ', arg);
                var codeAttach = AttachmentLoader.Load(codeAttachPath);
                if (!codeAttach.IsSuccess || codeAttach.Attachment is null)
                {
                    AppendLine(CodeOutput, codeAttach.Failure ?? "failed to load attachment");
                    return true;
                }

                _codeAttachments.Add(codeAttach.Attachment);
                AppendLine(CodeOutput, $"attached {codeAttach.Attachment.Name}");
                return true;
            case "config":
                AppendLine(CodeOutput, $"endpoint : {_config.BaseUrl}");
                AppendLine(CodeOutput, $"model    : {_runtimeModel}");
                AppendLine(CodeOutput, $"reasoning: {_runtimeReasoning ?? "(off)"}");
                AppendLine(CodeOutput, $"workspace: {_workspace}");
                AppendLine(CodeOutput, $"usage    : {(_runtimeUsage ? "on" : "off")}");
                AppendLine(CodeOutput, $"apikey   : {(string.IsNullOrWhiteSpace(_config.ApiKey) ? "(unset)" : "(set, hidden)")}");
                return true;
            case "mode":
                if (arg.Count == 0 || !Enum.TryParse(arg[0], true, out IntatisMode mode))
                {
                    AppendLine(CodeOutput, "usage: /mode <chat|code|cowork>");
                    return true;
                }

                SwitchModeTab(mode);
                return true;
            case "workspace":
                if (arg.Count == 0)
                {
                    AppendLine(CodeOutput, $"workspace: {_workspace}");
                    return true;
                }

                var newWorkspace = string.Join(' ', arg);
                try
                {
                    _workspace = ResolveWorkspace(newWorkspace, ConfigurationFallback: true);
                    WorkspaceBox.Text = _workspace;
                    _codeSession = CreateCodeSession(_workspace);
                    _coworkEngine = CreateCoworkEngine(_workspace);
                    AppendLine(CodeOutput, $"workspace set to {_workspace}");
                    AppendLine(CoworkHistory, $"workspace set to {_workspace}");
                }
                catch (Exception ex)
                {
                    AppendLine(CodeOutput, ex.Message);
                }

                return true;
            case "clear":
                _codeSession.Clear();
                AppendLine(CodeOutput, "code session cleared");
                return true;
            case "exit":
            case "quit":
                Close();
                return true;
            default:
                AppendLine(CodeOutput, $"unknown command: /{command}");
                return true;
        }
    }

    private bool HandleCoworkSlashCommand(string input)
    {
        var tokens = CommandParser.ParseTokens(input[1..]);
        if (tokens.Count == 0)
            return true;

        var command = tokens[0].ToLowerInvariant();
        var arg = tokens.Skip(1).ToList();

        switch (command)
        {
            case "help":
                AppendLine(CoworkHistory, "Cowork commands:");
                AppendLine(CoworkHistory, "/help");
                AppendLine(CoworkHistory, "/agents");
                AppendLine(CoworkHistory, "/agent add <name> <path> [model]");
                AppendLine(CoworkHistory, "/agent remove <name>");
                AppendLine(CoworkHistory, "/model [name]        default model for new agents");
                AppendLine(CoworkHistory, "/attach <path>             queue attachment for next cowork message");
                AppendLine(CoworkHistory, "/attach clear             clear cowork attachment queue");
                AppendLine(CoworkHistory, "/attach list              list cowork attachments");
                AppendLine(CoworkHistory, "/mode <chat|code|cowork>");
                AppendLine(CoworkHistory, "/exit");
                AppendLine(CoworkHistory, "@name <message>  route to specific agent");
                return true;

            case "agents":
                AppendLine(CoworkHistory, "agents: " + string.Join(", ", _coworkEngine.Agents));
                return true;

            case "agent":
                if (arg.Count == 0)
                {
                    AppendLine(CoworkHistory, "usage: /agent add <name> <path> [model] | /agent remove <name>");
                    return true;
                }

                var agentAction = arg[0];
                if (agentAction.Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Count < 3)
                    {
                        AppendLine(CoworkHistory, "usage: /agent add <name> <path> [model]");
                        return true;
                    }
                    if (arg.Count > 4)
                    {
                        AppendLine(CoworkHistory, "usage: /agent add <name> <path> [model]");
                        return true;
                    }

                    var agentName = arg[1];
                    var agentWorkspace = ResolveWorkspace(arg[2], ConfigurationFallback: false);
                    var agentModel = arg.Count == 4 ? arg[3] : null;

                    var agentModelToAttach = string.IsNullOrWhiteSpace(agentModel)
                        ? _runtimeModel
                        : agentModel;
                    AppendLine(CoworkHistory, _coworkEngine.Attach(agentName, agentWorkspace, agentModelToAttach));
                    return true;
                }

                if (agentAction.Equals("remove", StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Count != 2)
                    {
                        AppendLine(CoworkHistory, "usage: /agent remove <name>");
                        return true;
                    }

                    AppendLine(CoworkHistory, _coworkEngine.Detach(arg[1]));
                    return true;
                }

                AppendLine(CoworkHistory, "usage: /agent add <name> <path> [model] | /agent remove <name>");
                return true;

            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    AppendLine(CoworkHistory, _coworkAttachments.Count == 0
                        ? "no attachments queued. usage: /attach <path>"
                        : DescribeAttachmentQueue(_coworkAttachments));
                    return true;
                }

                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _coworkAttachments.Clear();
                    AppendLine(CoworkHistory, "cowork attachments cleared");
                    return true;
                }

                var coworkAttachmentPath = string.Join(' ', arg);
                var coworkAttachment = AttachmentLoader.Load(coworkAttachmentPath);
                if (!coworkAttachment.IsSuccess || coworkAttachment.Attachment is null)
                {
                    AppendLine(CoworkHistory, coworkAttachment.Failure ?? "failed to load attachment");
                    return true;
                }

                _coworkAttachments.Add(coworkAttachment.Attachment);
                AppendLine(CoworkHistory, $"attached {coworkAttachment.Attachment.Name}");
                return true;

            case "model":
                if (arg.Count == 0)
                {
                    AppendLine(CoworkHistory, "model: " + _runtimeModel);
                    return true;
                }

                _runtimeModel = arg[0];
                AppendLine(CoworkHistory, "model: " + _runtimeModel);
                return true;

            case "mode":
                if (arg.Count == 0 || !Enum.TryParse(arg[0], true, out IntatisMode mode))
                {
                    AppendLine(CoworkHistory, "usage: /mode <chat|code|cowork>");
                    return true;
                }

                SwitchModeTab(mode);
                return true;

            case "exit":
            case "quit":
                Close();
                return true;

            default:
                AppendLine(CoworkHistory, $"unknown command: /{command}");
                return true;
        }
    }

    private (string UserText, List<ImageAttachment> ImageAttachments) PrepareQueuedMessage(string userText)
    {
        return PrepareQueuedMessage(userText, _chatAttachments);
    }

    private (string UserText, List<ImageAttachment> ImageAttachments) PrepareQueuedMessage(
        string userText,
        List<ChatAttachment> attachmentQueue)
    {
        if (attachmentQueue.Count == 0)
            return (userText, []);

        var sb = new StringBuilder(userText);
        var imageAttachments = new List<ImageAttachment>();
        foreach (var attachment in attachmentQueue)
        {
            switch (attachment)
            {
                case TextAttachment text:
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine($"[attached file: {text.Name}]");
                    sb.AppendLine(text.Content);
                    break;
                case ImageAttachment image:
                    imageAttachments.Add(image);
                    break;
            }
        }

        attachmentQueue.Clear();
        return (sb.ToString(), imageAttachments);
    }

    private static string DescribeAttachmentQueue(IReadOnlyList<ChatAttachment> attachments)
    {
        var textCount = attachments.OfType<TextAttachment>().Count();
        var imageCount = attachments.OfType<ImageAttachment>().Count();
        var names = attachments.Count == 0 ? "none" : string.Join(", ", attachments.Select(a => a.Name));
        return $"{attachments.Count} attachment(s) [text={textCount}, image={imageCount}] ({names})";
    }

    private static void AppendLine(ListBox box, string text)
    {
        box.Items.Add(text);
        box.ScrollIntoView(box.Items[box.Items.Count - 1]);
    }

    private void AppendLineWithHistory(ListBox box, string text)
    {
        AppendLine(box, text);
    }

    private void SwitchModeTab(IntatisMode mode)
    {
        ModeTabs.SelectedItem = mode switch
        {
            IntatisMode.Code => CodeTab,
            IntatisMode.Cowork => CoworkTab,
            _ => ChatTab
        };
    }

    private CodeAgentSession CreateCodeSession(string workspace)
    {
        var shell = new ProcessShellRunner();
        var git = new ProcessGitService(shell);
        var permissionReviewer = new ModelPermissionReviewer(_config, _runtimeModel);

        return new CodeAgentSession(
            _config,
            workspace,
            "gui-code",
            PermissionProfile.Reviewed,
            shell: shell,
            git: git,
            responder: _permissionResponder,
            permissionReviewer: permissionReviewer,
            eventSink: _codeEventLog,
            allowsShell: true);
    }

    private CoworkEngine CreateCoworkEngine(string workspace)
    {
        var shell = new ProcessShellRunner();
        var git = new ProcessGitService(shell);
        var permissionReviewer = new ModelPermissionReviewer(_config, _runtimeModel);

        return new CoworkEngine(
            _config,
            workspace,
            shell,
            git,
            _permissionResponder,
            PermissionProfile.Reviewed,
            eventSink: _coworkEventLog,
            permissionReviewer: permissionReviewer,
            allowsShell: true);
    }

    private static string ResolveWorkspace(string? requested, bool ConfigurationFallback)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            if (ConfigurationFallback)
                return Environment.CurrentDirectory;

            throw new InvalidOperationException("No workspace path is configured.");
        }

        return WorkspaceTools.ResolveWorkspace(null, requested);
    }
}

internal sealed class WindowPermissionResponder : IPermissionResponder
{
    private readonly Window _owner;

    public WindowPermissionResponder(Window owner)
    {
        _owner = owner;
    }

    public async Task<PermissionDecision> RequestApprovalAsync(PermissionRequest request, CancellationToken cancellationToken = default)
    {
        return await _owner.Dispatcher.InvokeAsync(() =>
        {
            var message = "Permission requested:" + Environment.NewLine
                          + $"  tool   : {request.Tool}" + Environment.NewLine
                          + $"  risk   : {request.Risk}" + Environment.NewLine
                          + $"  reason : {request.Reason}" + Environment.NewLine
                          + $"  args   : {request.Args}";

            var answer = MessageBox.Show(
                _owner,
                message,
                "Intatis Windows",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            return answer switch
            {
                MessageBoxResult.Yes => PermissionDecision.Allow,
                _ => PermissionDecision.Deny,
            };
        }, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken).Task;
    }
}
