using Intatis.Core;
using Intatis.Core.Permission;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Intatis.Cli;

var command = args.FirstOrDefault() ?? "help";
var rest = args.Skip(1).ToList();

switch (command)
{
    case "help" or "--help" or "-h":
        PrintHelp();
        return 0;

    case "config":
        PrintConfig();
        return 0;

    case "settings":
        PrintSettings();
        return 0;

    case "chat":
        return await Interactive.RunChatAsync();

    case "code":
    {
        var workspace = rest.FirstOrDefault() ?? Environment.CurrentDirectory;
        return await Interactive.RunCodeAsync(workspace);
    }

    case "cowork":
    {
        var workspace = rest.FirstOrDefault() ?? Environment.CurrentDirectory;
        return await Interactive.RunCoworkAsync(workspace);
    }

    case "selftest":
        return SelfTest.Run();

    default:
        Console.WriteLine($"unknown command: {command}");
        PrintHelp();
        return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
intatis — local AI workspace (Windows port)

usage:
  intatis help                 show this help
  intatis config               print the resolved provider configuration (secrets masked)
  intatis settings             print application paths
  intatis chat                 streaming chat REPL (no tools)
  intatis code [dir]           code agent REPL in a workspace (tools + permissions)
  intatis cowork [dir]         cowork REPL: multi-agent roster, FIFO scheduler
  intatis selftest             offline test suite (no network)

slash commands (all modes):
  /help /model [id] /mode chat|code|cowork /exit
chat-only:
  /attach <path>               attach a text file or image to the next message
cowork-only:
  @agent <message>             address one agent
  /agents                      list the roster
  /agent add <name> [dir] [provider/model]
  /agent rm <name>
""");
}

static void PrintConfig()
{
    var (config, source) = ConfigStore.Load();
    Console.WriteLine($"source: {(source.Length > 0 ? source : "(defaults; no config file found)")}");
    foreach (var warning in config.Warnings)
        Console.WriteLine($"warning: {warning}");

    foreach (var provider in config.Providers)
    {
        var models = provider.Models.Count == 0 ? "-" : string.Join(", ", provider.Models.Select(m => m.Id));
        Console.WriteLine($"provider {provider.Id} ({provider.DisplayName})");
        Console.WriteLine($"  base url: {provider.BaseUrl}");
        Console.WriteLine($"  api key:  {provider.ApiKeyRef.Describe()}");
        Console.WriteLine($"  models:   {models}");
    }
    void Role(string label, ModelRef? reference)
        => Console.WriteLine($"{label}: {(reference is null ? "-" : $"{reference.ProviderId}/{reference.ModelId}")}");
    Role("model", config.Chat);
    Role("permission_reviewer_model", config.Reviewer);
    Role("image_model", config.Image);
    Role("transcription_model", config.Transcription);
    Role("embedding_model", config.Embedding);
    Role("reranker_model", config.Reranker);
    if (config.ReviewerFailedClosed)
        Console.WriteLine("permission reviewer: FAIL CLOSED (field present but unresolvable)");
}

static void PrintSettings()
{
    Console.WriteLine($"app data:     {AppConfig.ApplicationDataRoot()}");
    Console.WriteLine($"sessions:     {AppConfig.SessionsRoot()}");
    Console.WriteLine($"auth file:    {AppConfig.AuthFilePath()}");
    var candidates = AppConfig.ConfigCandidates();
    Console.WriteLine("config paths:");
    foreach (var candidate in candidates)
        Console.WriteLine($"  {(File.Exists(candidate) ? "*" : " ")} {candidate}");
}
