using Intatis.Core;
using Intatis.Core.Cowork;
using Intatis.Core.Permission;
using Intatis.Core.Protocol;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Intatis.Core.Tools;

namespace Intatis.Cli;

/// <summary>Console responder for Layer C: every gate-pass request asks the user.</summary>
internal sealed class ConsolePermissionResponder : IPermissionResponder
{
    public string ApprovalMode => "manual";

    public Task<PermissionDecision> RequestApprovalAsync(PermissionRequestCard request, CancellationToken ct = default)
    {
        Console.WriteLine();
        Console.WriteLine($"┌─ permission request ({request.Risk.ToWire()} risk)");
        Console.WriteLine($"│ tool: {request.Tool}");
        Console.WriteLine($"│ args: {Truncate(request.Args, 500)}");
        Console.WriteLine($"│ why:  {request.Reason}");
        Console.Write("└─ allow? [y=allow, anything else=deny] ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
        Console.WriteLine();
        return Task.FromResult(answer is "y" or "yes"
            ? PermissionDecision.Allow
            : PermissionDecision.Deny);
    }

    private static string Truncate(string value, int limit)
        => value.Length <= limit ? value : value[..limit] + "…";
}

/// <summary>Shared REPL plumbing for chat / code / cowork.</summary>
internal static class Interactive
{
    public static async Task<int> RunChatAsync()
    {
        var (config, _) = ConfigStore.Load();
        var chat = config.Chat ?? new ModelRef { ProviderId = "openai", ModelId = AppConfig.DefaultModel };
        var provider = TryProvider(chat, config, out var providerError);
        if (provider is null)
        {
            Console.WriteLine(providerError);
            return 1;
        }

        var sessionId = SessionId.New(SessionKind.Chat);
        var sessionsRoot = AppConfig.SessionsRoot();
        var log = EventLog.Open(sessionId, SessionHistoryStore.SessionFile(sessionsRoot, sessionId));
        Console.WriteLine($"intatis chat · {chat.ProviderId}/{chat.ModelId} · session {sessionId}");
        Console.WriteLine("type /help for commands; /exit to quit");

        var effort = ReasoningEffortExtensions.FromWire(Environment.GetEnvironmentVariable("INTATIS_REASONING"));
        List<ImageAttachment> pendingImages = [];
        string? currentModel = chat.ModelId;

        PrintDeltas(log, printHeader: false);

        while (true)
        {
            Console.Write("chat> ");
            var line = Console.ReadLine();
            if (line is null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith('/'))
            {
                var exit = HandleSlash(line, "chat", ref currentModel, pendingImages, log);
                if (exit) break;
                continue;
            }

            var images = pendingImages.ToList();
            pendingImages.Clear();
            var loop = new ChatLoop(log, provider, currentModel,
                systemPrompt: "You are Intatis, a concise local AI assistant.",
                reasoningEffort: effort,
                includeUsage: true);
            try
            {
                await loop.SendAsync(line, images);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }
        }

        log.Dispose();
        return 0;
    }

    public static async Task<int> RunCodeAsync(string workspace)
    {
        workspace = Path.GetFullPath(workspace);
        if (!Directory.Exists(workspace))
        {
            Console.WriteLine($"workspace not found: {workspace}");
            return 1;
        }

        var (config, _) = ConfigStore.Load();
        var binding = config.Chat ?? new ModelRef { ProviderId = "openai", ModelId = AppConfig.DefaultModel };
        var provider = TryProvider(binding, config, out var providerError);
        if (provider is null)
        {
            Console.WriteLine(providerError);
            return 1;
        }

        var sessionId = SessionId.New(SessionKind.Code);
        var log = EventLog.Open(sessionId, SessionHistoryStore.SessionFile(AppConfig.SessionsRoot(), sessionId));
        Console.WriteLine($"intatis code · {binding.ProviderId}/{binding.ModelId}");
        Console.WriteLine($"workspace: {workspace}");
        Console.WriteLine("type /help for commands; /exit to quit");

        PrintDeltas(log, printHeader: true);

        var registry = ToolRegistry.Standard();
        var permissions = BuildPermissions(config, new ConsolePermissionResponder());
        var currentModel = binding.ModelId;
        var pendingImages = new List<ImageAttachment>();

        while (true)
        {
            Console.Write("code> ");
            var line = Console.ReadLine();
            if (line is null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith('/'))
            {
                var exit = HandleSlash(line, "code", ref currentModel, pendingImages, log);
                if (exit) break;
                continue;
            }

            var loop = new AgentLoop(log, provider, registry, permissions, workspace,
                "Coder", currentModel, AgentPrompts.CodePrompt(workspace));
            try
            {
                await loop.SendAsync(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }
        }

        log.Dispose();
        return 0;
    }

    public static async Task<int> RunCoworkAsync(string workspace)
    {
        workspace = Path.GetFullPath(workspace);
        if (!Directory.Exists(workspace))
        {
            Console.WriteLine($"workspace not found: {workspace}");
            return 1;
        }

        var (config, _) = ConfigStore.Load();
        var binding = config.Chat ?? new ModelRef { ProviderId = "openai", ModelId = AppConfig.DefaultModel };
        var providerError = "";
        if (config.Provider(binding.ProviderId) is null)
        {
            providerError = $"no provider '{binding.ProviderId}' configured; run 'intatis config'";
            Console.WriteLine(providerError);
            return 1;
        }

        var resolver = new ConfigSecretResolver(AppConfig.AuthFilePath(), config.SourcePath);
        var registry = new ProviderRegistry(config, resolver);
        var responder = new ConsolePermissionResponder();
        var reviewerBinding = config.Reviewer is { } reviewer
            ? new AgentInferenceBinding { ProviderId = reviewer.ProviderId, ModelId = reviewer.ModelId }
            : null;

        var sessionId = SessionId.New(SessionKind.Cowork);
        var log = EventLog.Open(sessionId, SessionHistoryStore.SessionFile(AppConfig.SessionsRoot(), sessionId));
        using var orchestrator = Orchestrator.BootstrapFreshSession(
            log, registry, responder, workspace,
            new AgentInferenceBinding { ProviderId = binding.ProviderId, ModelId = binding.ModelId },
            reviewerBinding);

        Console.WriteLine($"intatis cowork · main={binding.ProviderId}/{binding.ModelId}");
        Console.WriteLine($"reviewer: {orchestrator.ReviewerModel ?? (orchestrator.ReviewerFailClosed ? "FAIL CLOSED" : "manual")}");
        Console.WriteLine($"workspace: {workspace}");
        Console.WriteLine("type /help for commands; /exit to quit");

        PrintDeltas(log, printHeader: true);

        while (true)
        {
            Console.Write("cowork> ");
            var line = Console.ReadLine();
            if (line is null) break;
            line = line.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith('/'))
            {
                var exit = await HandleCoworkSlashAsync(line, config, orchestrator, workspace);
                if (exit) break;
                continue;
            }

            try
            {
                var answer = await orchestrator.SendAsync(line);
                if (line.StartsWith('@') is false)
                    Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }
        }
        return 0;
    }

    private static PermissionEngine BuildPermissions(ImportedConfig config, IPermissionResponder responder)
    {
        ModelPermissionReviewer? reviewer = null;
        if (config.Reviewer is { } reference)
        {
            try
            {
                var resolver = new ConfigSecretResolver(AppConfig.AuthFilePath(), config.SourcePath);
                var registry = new ProviderRegistry(config, resolver);
                reviewer = new ModelPermissionReviewer(
                    registry.ChatProviderFor(reference.ProviderId), reference.ModelId);
            }
            catch (Exception)
            {
                reviewer = null; // fail closed: no reviewer, manual approval only
            }
        }
        return new PermissionEngine(reviewer, responder);
    }

    private static OpenAIWireProvider? TryProvider(ModelRef reference, ImportedConfig config, out string error)
    {
        var entry = config.Provider(reference.ProviderId);
        if (entry is null)
        {
            error = $"no provider '{reference.ProviderId}' configured; run 'intatis config'";
            return null;
        }
        var resolver = new ConfigSecretResolver(AppConfig.AuthFilePath(), config.SourcePath);
        var apiKey = resolver.ResolveSecret(entry.ApiKeyRef);
        if (apiKey.Length == 0)
        {
            error = $"api key for '{reference.ProviderId}' is empty ({entry.ApiKeyRef.Describe()})";
            return null;
        }
        error = "";
        return new OpenAIWireProvider(new HttpClient(), entry.BaseUrl, apiKey, entry.ChatEndpoint);
    }

    /// <summary>Lives-streams deltas/tool traffic from the log to the console.</summary>
    private static void PrintDeltas(EventLog log, bool printHeader)
    {
        log.EnvelopeAppended += envelope =>
        {
            switch (envelope.Type)
            {
                case EventType.MessageDelta:
                    var delta = (string?)envelope.Payload?["text_delta"];
                    if (delta is not null) Console.Write(delta);
                    break;
                case EventType.MessageCompleted:
                    if (printHeader && (string?)envelope.Payload?["role"] == "assistant")
                        Console.WriteLine($"  [{(string?)envelope.Payload?["agent"] ?? "assistant"}]");
                    else
                        Console.WriteLine();
                    break;
                case EventType.ToolCall:
                    Console.WriteLine($"\n  → {(string?)envelope.Payload?["name"]}({TruncArgs((string?)envelope.Payload?["args"])})");
                    break;
                case EventType.ToolResult:
                    Console.WriteLine($"  ← {TruncArgs((string?)envelope.Payload?["observation"])}");
                    break;
                case EventType.Error:
                    Console.WriteLine($"\n  error: {(string?)envelope.Payload?["message"]}");
                    break;
                case EventType.AgentAttached:
                    Console.WriteLine($"  agent attached: {(string?)envelope.Payload?["agent"]} ({(string?)envelope.Payload?["model"]})");
                    break;
            }
        };
    }

    private static string TruncArgs(string? value)
    {
        value ??= "";
        var single = value.Replace("\n", "␤");
        return single.Length <= 160 ? single : single[..160] + "…";
    }

    private static bool HandleSlash(
        string line,
        string mode,
        ref string currentModel,
        List<ImageAttachment> pendingImages,
        EventLog log)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "/help":
                Console.WriteLine("""
/help              this help
/model [id]        show or set the session model
/mode <name>       chat | code | cowork (restart the CLI with that mode)
/attach <path>     attach an image to the next chat message
/config            print the resolved configuration path
/exit              quit
""");
                break;

            case "/model":
                if (argument.Length > 0) currentModel = argument;
                Console.WriteLine($"model: {currentModel}");
                break;

            case "/mode":
                if (argument is "chat" or "code" or "cowork")
                    Console.WriteLine($"restart with: intatis {argument}");
                else
                    Console.WriteLine("usage: /mode chat|code|cowork");
                break;

            case "/attach":
                if (argument.Length == 0)
                {
                    Console.WriteLine("usage: /attach <image-path>");
                    break;
                }
                if (!File.Exists(argument))
                {
                    Console.WriteLine($"not found: {argument}");
                    break;
                }
                var mime = Path.GetExtension(argument).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => "image/jpeg",
                };
                var bytes = File.ReadAllBytes(argument);
                pendingImages.Add(new ImageAttachment($"data:{mime};base64,{Convert.ToBase64String(bytes)}"));
                Console.WriteLine($"attached image ({bytes.Length / 1024} KiB)");
                break;

            case "/config":
                Console.WriteLine(AppConfig.DefaultConfigPath());
                break;

            case "/exit" or "/quit":
                return true;

            default:
                Console.WriteLine($"unknown command {command}; /help for help");
                break;
        }
        return false;
    }

    private static async Task<bool> HandleCoworkSlashAsync(
        string line,
        ImportedConfig config,
        Orchestrator orchestrator,
        string workspace)
    {
        var parts = line.Split(' ', StringSplitOptions.TrimEntries);
        switch (parts[0].ToLowerInvariant())
        {
            case "/help":
                Console.WriteLine("""
/help                       this help
/agents                    list the roster
/agent add <name> [dir] [provider/model]
/agent rm <name>           detach an agent
/tasks                     list scheduler records
/exit                      quit
@agent <message>           address one agent directly
""");
                break;

            case "/agents":
                foreach (var agent in orchestrator.Registry.All())
                    Console.WriteLine($"  @{agent.Name} · {agent.Model} · {agent.Role} · {agent.WorkspaceRoot}");
                break;

            case "/agent":
                if (parts.Length < 3)
                {
                    Console.WriteLine("usage: /agent add <name> [dir] [provider/model] | /agent rm <name>");
                    break;
                }
                if (parts[1] == "add")
                {
                    var name = parts[2];
                    var dir = parts.Length > 3 && !parts[3].Contains('/') ? workspace : (parts.Length > 3 ? parts[3] : workspace);
                    var modelArg = parts.Length > 4 ? parts[4]
                        : config.Chat is { } chat ? $"{chat.ProviderId}/{chat.ModelId}"
                        : $"openai/{AppConfig.DefaultModel}";
                    var slash = modelArg.IndexOf('/');
                    var binding = new AgentInferenceBinding
                    {
                        ProviderId = slash > 0 ? modelArg[..slash] : "openai",
                        ModelId = slash > 0 ? modelArg[(slash + 1)..] : modelArg,
                    };
                    if (!Directory.Exists(dir)) dir = workspace;
                    orchestrator.Attach(name, Path.GetFullPath(dir), binding);
                    Console.WriteLine($"attached @{name} ({binding.DisplayLabel}) in {dir}");
                }
                else if (parts[1] == "rm")
                {
                    Console.WriteLine(orchestrator.Detach(parts[2]) ? $"detached @{parts[2]}" : $"cannot detach @{parts[2]}");
                }
                break;

            case "/tasks":
                foreach (var record in orchestrator.Scheduler.Records())
                    Console.WriteLine($"  {record.TaskId} @{record.Assignee} · {record.Status}{(record.Error is { Length: > 0 } ? $" · {record.Error}" : "")}");
                break;

            case "/exit" or "/quit":
                return true;

            default:
                Console.WriteLine($"unknown command {parts[0]}; /help for help");
                break;
        }
        await Task.CompletedTask;
        return false;
    }
}
