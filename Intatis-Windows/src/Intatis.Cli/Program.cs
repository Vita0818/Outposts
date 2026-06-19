using System.Diagnostics;
using System.Text;
using Intatis.Windows.Shared;

namespace Intatis.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var config = ConfigStore.Load();
            var command = args.Length == 0 ? config.DefaultMode.ToString().ToLowerInvariant() : args[0].ToLowerInvariant();
            var runner = new CliRunner(config);

            return command switch
            {
                "chat" or "code" or "cowork" => await RunModeLoop(runner, ParseMode(command), args.Length > 1 ? args[1] : null),
                "settings" => runner.ShowSettingsWizard(),
                "config" => runner.PrintConfig(config),
                "selftest" => await runner.RunSelftestAsync(),
                "help" or "-h" or "--help" => ShowHelp(),
                _ => PrintUnknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static IntatisMode ParseMode(string command)
    {
        return Enum.TryParse(command, true, out IntatisMode mode) ? mode : IntatisMode.Chat;
    }

    private static async Task<int> RunModeLoop(CliRunner runner, IntatisMode startMode, string? workspaceArg)
    {
        var currentMode = startMode;

        while (true)
        {
            var action = currentMode switch
            {
                IntatisMode.Chat => await runner.RunChatAsync(),
                IntatisMode.Code => await runner.RunCodeAsync(workspaceArg),
                IntatisMode.Cowork => await runner.RunCoworkAsync(workspaceArg),
                _ => ReplAction.Exit,
            };

            workspaceArg = null;

            if (action == ReplAction.Exit)
                return 0;

            currentMode = action switch
            {
                ReplAction.SwitchChat => IntatisMode.Chat,
                ReplAction.SwitchCode => IntatisMode.Code,
                ReplAction.SwitchCowork => IntatisMode.Cowork,
                ReplAction.Continue => currentMode,
                _ => currentMode,
            };
        }
    }

    private static int ShowHelp()
    {
        Console.WriteLine("""
Intatis-Windows CLI

USAGE
  intatis                Start mode from config (default INTATIS_MODE)
  intatis chat           Streaming chat mode
  intatis code [dir]     File-aware code helper (tool loop)
  intatis cowork [dir]   Cowork multi-agent mode
  intatis settings       Configure base URL, model, API key and workspace
  intatis config         Print current resolved configuration
  intatis selftest       Offline self-test without any remote calls
  intatis help           Show this help

ENV CONFIG (same as Swift CLI)
  INTATIS_BASE_URL   default https://api.openai.com/v1
  INTATIS_API_KEY    required for chat/code/cowork
  INTATIS_MODEL      default gpt-4o-mini
  INTATIS_REASONING   optional: minimal|low|medium|high
  INTATIS_MODE       chat|code|cowork
  INTATIS_WORKSPACE   default workspace path for code mode
  INTATIS_USAGE      0 / 1 (controls usage report display)

IN-SESSION SLASH COMMANDS
  /help               show command list
  /clear              clear history
  /mode               switch mode
  /model              show or set current model
  /reasoning          show or set reasoning (minimal|low|medium|high|off)
  /attach             queue image or text attachment for next chat message
  /config             print current runtime configuration
  /exit               leave mode

""");
        return 0;
    }

    private static int PrintUnknown(string command)
    {
        Console.WriteLine($"unknown command: {command}");
        return ShowHelp();
    }
}

internal enum ReplAction
{
    Continue,
    Exit,
    SwitchChat,
    SwitchCode,
    SwitchCowork,
}

internal sealed class CliRunner
{
    private readonly IntatisConfig _config;
    private readonly ConversationSession _session;
    private readonly IPermissionResponder _permissionResponder;
    private readonly SessionEventLog _codeEventLog;
    private readonly SessionEventLog _coworkEventLog;
    private CodeAgentSession _codeSession;
    private CoworkEngine _cowork;
    private readonly List<ChatAttachment> _attachments = [];
    private readonly List<ChatAttachment> _coworkAttachments = [];

    private string _runtimeModel;
    private string? _runtimeReasoning;
    private string _runtimeWorkspaceHint;

    public CliRunner(IntatisConfig config)
    {
        _config = config;
        _session = new ConversationSession(config);
        _permissionResponder = new TerminalPermissionResponder();
        _codeEventLog = new SessionEventLog("cli-code");
        _coworkEventLog = new SessionEventLog("cli-cowork");

        _runtimeModel = config.Model;
        _runtimeReasoning = config.Reasoning;
        _runtimeWorkspaceHint = ResolveWorkspace(config.Workspace, null);
        _codeSession = CreateCodeSession(_runtimeWorkspaceHint);
        _cowork = CreateCoworkEngine(_runtimeWorkspaceHint);
    }

    public int PrintConfig(IntatisConfig config)
    {
        Console.WriteLine($"endpoint : {config.BaseUrl}");
        Console.WriteLine($"model    : {config.Model}");
        Console.WriteLine($"reasoning: {config.Reasoning ?? "(off)"}");
        Console.WriteLine($"mode     : {config.DefaultMode}");
        Console.WriteLine($"workspace: {config.Workspace ?? "(unset)"}");
        Console.WriteLine($"usage    : {(config.IncludeUsage ? "on" : "off")}");
        Console.WriteLine($"config   : {ConfigStore.ConfigPath}");
        Console.WriteLine($"apikey   : {(string.IsNullOrWhiteSpace(config.ApiKey) ? "(unset)" : "(set, hidden)")}");
        return 0;
    }

    public int ShowSettingsWizard()
    {
        var current = _config;
        Console.WriteLine("Intatis-Windows Settings");
        Console.WriteLine("------------------------");

        var baseUrl = Prompt("Base URL", current.BaseUrl);
        var model = Prompt("Model", current.Model);
        var reasoning = Prompt("Reasoning (minimal|low|medium|high, optional)", current.Reasoning);
        var workspace = Prompt("Default Workspace (optional)", current.Workspace);
        var modeInput = Prompt("Default Mode (chat/code/cowork)", current.DefaultMode.ToString().ToLowerInvariant());
        var usage = Prompt("Show usage in responses? (1 on, 0 off)", current.IncludeUsage ? "1" : "0");
        var key = Prompt("API Key", current.ApiKey);

        var parsedMode = Enum.TryParse(modeInput, true, out IntatisMode parsedMode) ? parsedMode : IntatisMode.Chat;
        string? parsedReasoning = null;
        if (!CommandParser.TryNormalizeReasoning(reasoning, out parsedReasoning))
        {
            Console.WriteLine("invalid reasoning level; keeping previous value.");
            parsedReasoning = current.Reasoning;
        }

        var next = current.CloneWith(
            baseUrl: baseUrl,
            apiKey: key,
            model: model,
            reasoning: parsedReasoning,
            defaultMode: parsedMode,
            workspace: string.IsNullOrWhiteSpace(workspace) ? null : workspace,
            includeUsage: usage != "0");

        ConfigStore.Save(next);
        Console.WriteLine("Saved.");
        return 0;
    }

    public async Task<ReplAction> RunChatAsync()
    {
        Console.WriteLine("Intatis chat mode. /help for commands.");
        while (true)
        {
                if (_attachments.Count > 0)
                Console.WriteLine($"[{FormatAttachmentSummary(_attachments)} queued for next message]");

            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (HandleChatSlashCommand(input.Trim(), out var action))
                return action;

            try
            {
                var (effective, images) = PrepareQueuedMessage(input.Trim());
                var (reply, latency, usage) = await _session.SendUserMessageAsync(
                    effective,
                    model: _runtimeModel,
                    reasoning: _runtimeReasoning,
                    attachments: images);

                Console.WriteLine($"\n[assistant] {reply.Content}");
                if (_config.IncludeUsage && !string.IsNullOrWhiteSpace(usage))
                    Console.WriteLine($"usage: {usage}");
                Console.WriteLine($"time: {latency.TotalMilliseconds:F0}ms\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nerror: {ex.Message}\n");
                if (_config.ApiKey == string.Empty)
                    Console.WriteLine("Hint: run `intatis settings` or set INTATIS_API_KEY.");
            }
        }
    }

    public async Task<ReplAction> RunCodeAsync(string? workspaceArg)
    {
        if (!string.IsNullOrWhiteSpace(workspaceArg))
            _runtimeWorkspaceHint = ResolveWorkspace(_config.Workspace, workspaceArg);

        ConfigureSessions(_runtimeWorkspaceHint);

        Console.WriteLine($"Code mode: workspace = {_runtimeWorkspaceHint}");
        Console.WriteLine("Describe what to do. The agent decides and runs tools automatically.");

        while (true)
        {
            Console.Write("code> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            var trimmed = input.Trim();
            if (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                var slashAction = HandleCodeSlashCommand(trimmed);
                if (slashAction == ReplAction.Exit)
                    return ReplAction.Exit;
                if (slashAction == ReplAction.SwitchChat)
                    return ReplAction.SwitchChat;
                if (slashAction == ReplAction.SwitchCowork)
                    return ReplAction.SwitchCowork;

                continue;
            }

            try
            {
                var (effectiveCodeText, codeImages) = PrepareQueuedMessage(trimmed, _attachments);
                var (reply, latency, usage) = await _codeSession.SendAsync(
                    effectiveCodeText,
                    model: _runtimeModel,
                    reasoning: _runtimeReasoning,
                    userGoal: "code mode",
                    images: codeImages,
                    includeUsage: _config.IncludeUsage);

                Console.WriteLine($"\n[assistant] {reply}\n");
                if (_config.IncludeUsage && !string.IsNullOrWhiteSpace(usage))
                    Console.WriteLine($"usage: {usage}");
                Console.WriteLine($"time: {latency.TotalMilliseconds:F0}ms\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nerror: {ex.Message}\n");
            }
        }
    }

    public async Task<ReplAction> RunCoworkAsync(string? workspaceArg)
    {
        if (!string.IsNullOrWhiteSpace(workspaceArg))
            _runtimeWorkspaceHint = ResolveWorkspace(_config.Workspace, workspaceArg);

        ConfigureSessions(_runtimeWorkspaceHint);

        Console.WriteLine($"Cowork mode: default workspace = {_runtimeWorkspaceHint}");
        Console.WriteLine("Examples: /agents, /agent add reviewer .");
        Console.WriteLine("Use /attach to queue image/text attachment for next cowork message.");
        Console.WriteLine("Use @agent message for explicit routing, or just send a message.");
        Console.WriteLine("Type /help for available commands and /exit to quit.");

        while (true)
        {
            if (_coworkAttachments.Count > 0)
                Console.WriteLine($"[{FormatAttachmentSummary(_coworkAttachments)} queued for next cowork message]");

            Console.Write("cowork> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            var trimmed = input.Trim();
            if (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                var action = HandleCoworkSlashCommand(trimmed);
                if (action == ReplAction.Exit)
                    return ReplAction.Exit;
                if (action != ReplAction.Continue)
                    return action;

                continue;
            }

            string? target = null;
            var body = trimmed;
            if (trimmed.StartsWith("@", StringComparison.Ordinal))
            {
                var raw = trimmed.AsSpan().Slice(1).ToString();
                var split = raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length > 0)
                {
                    target = split[0];
                    body = split.Length > 1 ? split[1] : string.Empty;
                }
            }

            try
            {
                var (effectiveBody, images) = PrepareQueuedMessage(body, _coworkAttachments);
                var reply = await _cowork.SendAsync(
                    effectiveBody,
                    target,
                    reasoning: _runtimeReasoning,
                    images: images,
                    includeUsage: _config.IncludeUsage);
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    Console.WriteLine($"\n[{target ?? "next"}] {reply}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nerror: {ex.Message}\n");
            }
        }
    }

    public async Task<int> RunSelftestAsync()
    {
        var sw = Stopwatch.StartNew();
        var root = Path.Combine(Path.GetTempPath(), "intatis-windows-selftest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            WorkspaceTools.WriteText(root, "readme.txt", "hello intatis");
            var text = WorkspaceTools.ReadText(root, "readme.txt");
            var hits = WorkspaceTools.Search(root, "intatis");
            var passed = text == "hello intatis" && hits.Count == 1;
            Console.WriteLine(passed ? "SELFTEST: OK" : "SELFTEST: FAIL");
            if (!passed)
                return 1;

            Console.WriteLine($"workspace temp: {root}");
            Console.WriteLine($"elapsed: {sw.ElapsedMilliseconds}ms");
            return 0;
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
            }
        }
    }

    private bool HandleChatSlashCommand(string input, out ReplAction action)
    {
        action = ReplAction.Continue;

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
                PrintChatHelp();
                return true;
            case "clear":
                _session.Clear();
                Console.WriteLine("session cleared.");
                return true;
            case "mode":
                if (arg.Count == 0)
                {
                    Console.WriteLine("usage: /mode <chat|code|cowork>");
                    return true;
                }

                if (!Enum.TryParse(arg[0], true, out IntatisMode targetMode))
                {
                    Console.WriteLine("usage: /mode <chat|code|cowork>");
                    return true;
                }

                action = targetMode switch
                {
                    IntatisMode.Chat => ReplAction.SwitchChat,
                    IntatisMode.Code => ReplAction.SwitchCode,
                    IntatisMode.Cowork => ReplAction.SwitchCowork,
                    _ => ReplAction.Continue,
                };

                Console.WriteLine($"switching to {targetMode} mode");
                return true;
            case "model":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"model: {_runtimeModel}");
                    return true;
                }

                _runtimeModel = arg[0];
                Console.WriteLine($"model -> {_runtimeModel}");
                return true;
            case "reasoning":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"reasoning: {_runtimeReasoning ?? "(off)"}");
                    return true;
                }

                if (!CommandParser.TryNormalizeReasoning(arg[0], out var nextReasoning))
                {
                    Console.WriteLine("usage: /reasoning minimal|low|medium|high|off");
                    return true;
                }

                _runtimeReasoning = nextReasoning;
                Console.WriteLine($"reasoning -> {_runtimeReasoning ?? "off"}");
                return true;
            case "config":
                PrintRuntimeConfig();
                return true;
            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    if (_attachments.Count == 0)
                    {
                        Console.WriteLine("no attachments queued.");
                    }
                    else
                    {
                        Console.WriteLine(DescribeAttachmentQueue(_attachments));
                    }

                    return true;
                }

                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _attachments.Clear();
                    Console.WriteLine("attachments cleared.");
                    return true;
                }

                var path = string.Join(' ', arg);
                var result = AttachmentLoader.Load(path);
                if (!result.IsSuccess || result.Attachment is null)
                {
                    Console.WriteLine(result.Failure ?? "failed to attach file.");
                    return true;
                }

                _attachments.Add(result.Attachment);
                switch (result.Attachment)
                {
                    case TextAttachment txt:
                        Console.WriteLine($"attached text: {txt.Name}");
                        break;
                    case ImageAttachment image:
                        Console.WriteLine($"attached image: {image.Name}");
                        break;
                    default:
                        Console.WriteLine($"attached: {result.Attachment.Name}");
                        break;
                }
                return true;
            case "exit":
            case "quit":
                action = ReplAction.Exit;
                return true;
            default:
                Console.WriteLine($"unknown command: /{command}");
                return true;
        }
    }

    private ReplAction HandleCodeSlashCommand(string input)
    {
        var tokens = CommandParser.ParseTokens(input[1..]);
        if (tokens.Count == 0)
            return ReplAction.Continue;

        var command = tokens[0].ToLowerInvariant();
        var arg = tokens.Skip(1).ToList();

        switch (command)
        {
            case "help":
                PrintCodeHelp();
                return ReplAction.Continue;
            case "mode":
                if (arg.Count == 0 || !Enum.TryParse(arg[0], true, out IntatisMode target))
                {
                    Console.WriteLine("usage: /mode <chat|code|cowork>");
                    return ReplAction.Continue;
                }

                return target switch
                {
                    IntatisMode.Chat => Return(ReplAction.SwitchChat),
                    IntatisMode.Code => ReplAction.Continue,
                    IntatisMode.Cowork => Return(ReplAction.SwitchCowork),
                    _ => ReplAction.Continue,
                };

            case "model":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"model: {_runtimeModel}");
                    return ReplAction.Continue;
                }

                _runtimeModel = arg[0];
                Console.WriteLine($"model: {_runtimeModel}");
                return ReplAction.Continue;

            case "reasoning":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"reasoning: {_runtimeReasoning ?? "(off)"}");
                    return ReplAction.Continue;
                }

                if (!CommandParser.TryNormalizeReasoning(arg[0], out var nextReasoning))
                {
                    Console.WriteLine("usage: /reasoning minimal|low|medium|high|off");
                    return ReplAction.Continue;
                }

                _runtimeReasoning = nextReasoning;
                Console.WriteLine($"reasoning: {_runtimeReasoning ?? "off"}");
                return ReplAction.Continue;

            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    if (_attachments.Count == 0)
                        Console.WriteLine("no attachments queued. usage: /attach <path>");
                    else
                        Console.WriteLine(DescribeAttachmentQueue(_attachments));
                    return ReplAction.Continue;
                }
                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _attachments.Clear();
                    Console.WriteLine("attachments cleared.");
                    return ReplAction.Continue;
                }

                var attachPath = string.Join(' ', arg);
                var attachResult = AttachmentLoader.Load(attachPath);
                if (!attachResult.IsSuccess || attachResult.Attachment is null)
                {
                    Console.WriteLine(attachResult.Failure ?? "failed to attach file.");
                    return ReplAction.Continue;
                }

                _attachments.Add(attachResult.Attachment);
                Console.WriteLine($"attached {attachResult.Attachment.Name}");
                return ReplAction.Continue;

            case "config":
                PrintRuntimeConfig();
                return ReplAction.Continue;

            case "workspace":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"workspace: {_runtimeWorkspaceHint}");
                    return ReplAction.Continue;
                }

                _runtimeWorkspaceHint = ResolveWorkspace(_config.Workspace, string.Join(' ', arg));
                ConfigureSessions(_runtimeWorkspaceHint);
                Console.WriteLine($"workspace set to {_runtimeWorkspaceHint}");
                return ReplAction.Continue;

            case "clear":
                _codeSession.Clear();
                Console.WriteLine("code session cleared.");
                return ReplAction.Continue;

            case "exit":
            case "quit":
                return ReplAction.Exit;

            default:
                Console.WriteLine($"unknown command: /{command}");
                return ReplAction.Continue;
        }

        static ReplAction Return(ReplAction action)
        {
            Console.WriteLine(action == ReplAction.SwitchChat ? "switching to chat mode" : "already in requested mode");
            return action;
        }
    }

    private ReplAction HandleCoworkSlashCommand(string input)
    {
        var tokens = CommandParser.ParseTokens(input[1..]);
        if (tokens.Count == 0)
            return ReplAction.Continue;

        var command = tokens[0].ToLowerInvariant();
        var arg = tokens.Skip(1).ToList();

            switch (command)
            {
                case "help":
                    PrintCoworkHelp();
                return ReplAction.Continue;

            case "agents":
                Console.WriteLine("agents: " + string.Join(", ", _cowork.Agents));
                return ReplAction.Continue;

            case "agent":
                if (arg.Count == 0)
                {
                    Console.WriteLine("usage: /agent add <name> <path> [model] | /agent remove <name>");
                    return ReplAction.Continue;
                }

                var action = arg[0];
                if (action.Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Count < 3)
                    {
                        Console.WriteLine("usage: /agent add <name> <path> [model]");
                        return ReplAction.Continue;
                    }
                    if (arg.Count > 4)
                    {
                        Console.WriteLine("usage: /agent add <name> <path> [model]");
                        return ReplAction.Continue;
                    }

                    var name = arg[1];
                    var workspace = ResolveWorkspace(_config.Workspace, arg[2]);
                    var model = arg.Count == 4 ? arg[3] : null;

                    var modelToAttach = model ?? _runtimeModel;
                    Console.WriteLine(_cowork.Attach(name, workspace, modelToAttach));
                    return ReplAction.Continue;
                }

                if (action.Equals("remove", StringComparison.OrdinalIgnoreCase))
                {
                    if (arg.Count != 2)
                    {
                        Console.WriteLine("usage: /agent remove <name>");
                        return ReplAction.Continue;
                    }

                    Console.WriteLine(_cowork.Detach(arg[1]));
                    return ReplAction.Continue;
                }

                Console.WriteLine("usage: /agent add <name> <path> [model] | /agent remove <name>");
                return ReplAction.Continue;

            case "attach":
                if (arg.Count == 0 || (arg.Count == 1 && arg[0].Equals("list", StringComparison.OrdinalIgnoreCase)))
                {
                    if (_coworkAttachments.Count == 0)
                        Console.WriteLine("no attachments queued. usage: /attach <path>");
                    else
                        Console.WriteLine(DescribeAttachmentQueue(_coworkAttachments));
                    return ReplAction.Continue;
                }

                if (arg.Count == 1 && arg[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    _coworkAttachments.Clear();
                    Console.WriteLine("cowork attachments cleared.");
                    return ReplAction.Continue;
                }

                var attachmentPath = string.Join(' ', arg);
                var attachment = AttachmentLoader.Load(attachmentPath);
                if (!attachment.IsSuccess || attachment.Attachment is null)
                {
                    Console.WriteLine(attachment.Failure ?? "failed to attach file.");
                    return ReplAction.Continue;
                }

                _coworkAttachments.Add(attachment.Attachment);
                Console.WriteLine($"attached {attachment.Attachment.Name}");
                return ReplAction.Continue;

            case "model":
                if (arg.Count == 0)
                {
                    Console.WriteLine($"default model for new agents: {_runtimeModel}");
                    return ReplAction.Continue;
                }

                _runtimeModel = arg[0];
                Console.WriteLine($"default model for new agents -> {_runtimeModel}");
                return ReplAction.Continue;

            case "mode":
                if (arg.Count == 0 || !Enum.TryParse(arg[0], true, out IntatisMode target))
                {
                    Console.WriteLine("usage: /mode <chat|code|cowork>");
                    return ReplAction.Continue;
                }

                return target switch
                {
                    IntatisMode.Chat => Return("chat"),
                    IntatisMode.Code => Return("code"),
                    IntatisMode.Cowork => ReplAction.Continue,
                    _ => ReplAction.Continue,
                };

            case "exit":
            case "quit":
                return ReplAction.Exit;

            default:
                Console.WriteLine($"unknown command: /{command}");
                return ReplAction.Continue;
        }

        static ReplAction Return(string modeName)
        {
            Console.WriteLine($"switching to {modeName} mode");
            return modeName == "chat" ? ReplAction.SwitchChat : ReplAction.SwitchCode;
        }
    }

    private static ReplAction MessageWithAction(string message, ReplAction action)
    {
        Console.WriteLine(message);
        return action;
    }

    private static string DescribeAttachmentQueue(IReadOnlyList<ChatAttachment> attachments)
    {
        var textCount = attachments.OfType<TextAttachment>().Count();
        var imageCount = attachments.OfType<ImageAttachment>().Count();
        var names = attachments.Count == 0 ? "none" : string.Join(", ", attachments.Select(a => a.Name));
        return $"{attachments.Count} attachment(s) [text={textCount}, image={imageCount}] ({names})";
    }

    private static string FormatAttachmentSummary(IReadOnlyList<ChatAttachment> attachments)
    {
        var textCount = attachments.OfType<TextAttachment>().Count();
        var imageCount = attachments.OfType<ImageAttachment>().Count();
        return $"{attachments.Count} attachment(s) [text={textCount}, image={imageCount}]";
    }

    private (string UserText, List<ImageAttachment> ImageAttachments) PrepareQueuedMessage(string userText)
    {
        return PrepareQueuedMessage(userText, _attachments);
    }

    private (string UserText, List<ImageAttachment> ImageAttachments) PrepareQueuedMessage(string userText, List<ChatAttachment> attachmentQueue)
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

    private void PrintRuntimeConfig()
    {
        Console.WriteLine($"endpoint : {_config.BaseUrl}");
        Console.WriteLine($"model    : {_runtimeModel}");
        Console.WriteLine($"reasoning: {_runtimeReasoning ?? "(off)"}");
        Console.WriteLine($"mode     : {_config.DefaultMode}");
        Console.WriteLine($"workspace: {_config.Workspace ?? "(unset)"}");
        Console.WriteLine($"usage    : {(_config.IncludeUsage ? "on" : "off")}");
        Console.WriteLine($"config   : {ConfigStore.ConfigPath}");
        Console.WriteLine($"apikey   : {(string.IsNullOrWhiteSpace(_config.ApiKey) ? "(unset)" : "(set, hidden)")}");
    }

    private void ConfigureSessions(string workspace)
    {
        _runtimeWorkspaceHint = workspace;
        _codeSession = CreateCodeSession(workspace);
        _cowork = CreateCoworkEngine(workspace);
    }

    private string ResolveWorkspace(string? configured, string? requested)
    {
        var candidate = !string.IsNullOrWhiteSpace(requested) ? requested : configured;
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = Environment.CurrentDirectory;

        return WorkspaceTools.ResolveWorkspace(null, candidate);
    }

    private CodeAgentSession CreateCodeSession(string workspace)
    {
        var shell = new ProcessShellRunner();
        var git = new ProcessGitService(shell);
        var permissionReviewer = new ModelPermissionReviewer(_config, _runtimeModel);

        return new CodeAgentSession(
            _config,
            workspace,
            "cli-code",
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

    private static void PrintChatHelp()
    {
        Console.WriteLine("/help       - show this help");
        Console.WriteLine("/clear      - clear chat history");
        Console.WriteLine("/mode       - /mode <chat|code|cowork>");
        Console.WriteLine("/model      - /model [name]");
        Console.WriteLine("/reasoning  - /reasoning minimal|low|medium|high|off");
        Console.WriteLine("/attach     - /attach <path> (text or image) | /attach clear | /attach list");
        Console.WriteLine("/config     - print resolved config");
        Console.WriteLine("/exit       - leave chat");
    }

    private static void PrintCodeHelp()
    {
        Console.WriteLine("code mode commands:");
        Console.WriteLine("/help                         this help");
        Console.WriteLine("/mode <chat|code|cowork>      switch mode");
        Console.WriteLine("/model [name]                 show or set code model");
        Console.WriteLine("/reasoning [minimal|low|medium|high|off]  show or set reasoning");
        Console.WriteLine("/attach <path>                queue attachment for next code message");
        Console.WriteLine("/attach clear                 clear attachment queue");
        Console.WriteLine("/attach list                  list queued attachments");
        Console.WriteLine("/config                       print runtime config");
        Console.WriteLine("/clear                        clear code session history");
        Console.WriteLine("/exit                         leave mode");
        Console.WriteLine("describe tasks naturally; the agent will call tools");
    }

    private static void PrintCoworkHelp()
    {
        Console.WriteLine("cowork commands:");
        Console.WriteLine("/help                         this help");
        Console.WriteLine("/agents                       list agents");
        Console.WriteLine("/agent add <name> <path> [model]  attach agent");
        Console.WriteLine("/agent remove <name>           remove agent");
        Console.WriteLine("/model [name]                  default model for new agents");
        Console.WriteLine("/attach <path>                 queue attachment for next cowork message");
        Console.WriteLine("/attach clear                  clear queued attachments");
        Console.WriteLine("/attach list                   list queued attachments");
        Console.WriteLine("@name <message>               route to agent");
        Console.WriteLine("<message>                     auto-route to default agent");
        Console.WriteLine("/mode <chat|code|cowork>      switch mode");
        Console.WriteLine("/exit                         leave mode");
    }

    private static string Prompt(string label, string? current)
    {
        Console.Write($"{label} [{current ?? string.Empty}]: ");
        var next = Console.ReadLine();
        return string.IsNullOrWhiteSpace(next) ? (current ?? string.Empty) : next!.Trim();
    }
}

internal sealed class TerminalPermissionResponder : IPermissionResponder
{
    public async Task<PermissionDecision> RequestApprovalAsync(PermissionRequest request, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                return PermissionDecision.Deny;

            Console.WriteLine("Permission requested:");
            Console.WriteLine($"  tool    : {request.Tool}");
            Console.WriteLine($"  agent   : {request.Agent ?? "(shared)"}");
            Console.WriteLine($"  risk    : {request.Risk}");
            Console.WriteLine($"  reason  : {request.Reason}");
            Console.WriteLine($"  args    : {request.Args}");
            Console.Write("Approve [y=allow, n=deny, q=quit]? ");

            var input = await Task.Run(() => Console.ReadLine(), cancellationToken);
            var normalized = input?.Trim().ToLowerInvariant();
            if (normalized is "y" or "yes" or "allow")
                return PermissionDecision.Allow;
            if (normalized is "n" or "no" or "deny")
                return PermissionDecision.Deny;
            if (normalized is "q" or "quit" or "exit")
                return PermissionDecision.Deny;

            Console.WriteLine("input y / n / q");
        }
    }
}
