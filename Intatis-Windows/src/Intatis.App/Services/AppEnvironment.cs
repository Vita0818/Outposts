using Intatis.Core;
using Intatis.Core.Permission;
using Intatis.Core.Providers;
using Intatis.Core.Session;
using Microsoft.UI.Dispatching;

namespace Intatis.App.Services;

/// <summary>GUI responder for Layer C: a pending permission card settles the request.</summary>
public sealed class GuiPermissionResponder : IPermissionResponder
{
    private readonly DispatcherQueue _dispatcher;
    private readonly Action<PermissionRequestCard, Action<PermissionDecision>> _present;

    public string ApprovalMode => "manual";

    public GuiPermissionResponder(DispatcherQueue dispatcher,
        Action<PermissionRequestCard, Action<PermissionDecision>> present)
    {
        _dispatcher = dispatcher;
        _present = present;
    }

    public Task<PermissionDecision> RequestApprovalAsync(PermissionRequestCard request, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<PermissionDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Settle(PermissionDecision decision) => tcs.TrySetResult(decision);

        if (!_dispatcher.TryEnqueue(() => _present(request, Settle)))
            tcs.TrySetResult(PermissionDecision.Deny); // fail closed

        // Cancellation settles as denial so a stopped turn never hangs on the card.
        ct.Register(() => tcs.TrySetResult(PermissionDecision.Deny));
        return tcs.Task;
    }
}

/// <summary>
/// Root composition: configuration, provider registry and process-owned session
/// runtimes so switching pages never tears live sessions down.
/// </summary>
public sealed class AppEnvironment
{
    private readonly object _gate = new();

    public ImportedConfig Config { get; private set; }
    public ProviderRegistry Providers { get; private set; }
    public string ConfigSourcePath { get; private set; } = "";
    public event Action? ConfigChanged;

    private AppEnvironment(ImportedConfig config, string sourcePath)
    {
        Config = config;
        ConfigSourcePath = sourcePath;
        Providers = BuildRegistry(config, sourcePath);
    }

    public static AppEnvironment Load()
    {
        var (config, sourcePath) = ConfigStore.Load();
        return new AppEnvironment(config, sourcePath);
    }

    private static ProviderRegistry BuildRegistry(ImportedConfig config, string sourcePath)
        => new(config, new ConfigSecretResolver(AppConfig.AuthFilePath(), sourcePath));

    public void ReloadConfig()
    {
        var (config, sourcePath) = ConfigStore.Load();
        lock (_gate)
        {
            Config = config;
            ConfigSourcePath = sourcePath;
            Providers = BuildRegistry(config, sourcePath);
        }
        ConfigChanged?.Invoke();
    }

    public string SaveConfig(ImportedConfig config)
    {
        var path = ConfigSourcePath.Length > 0 ? ConfigSourcePath : AppConfig.DefaultConfigPath();
        ConfigStore.Save(config, path);
        ReloadConfig();
        return path;
    }

    public string SessionFileFor(SessionId session)
        => SessionHistoryStore.SessionFile(AppConfig.SessionsRoot(), session);

    public List<SessionSummary> RecentSessions(SessionKind kind)
        => SessionHistoryStore.RecentSessions(AppConfig.SessionsRoot(), kind);

    public void DeleteSession(SessionSummary summary)
        => SessionHistoryStore.DeleteSession(AppConfig.SessionsRoot(), summary.Id);

    /// <summary>Default inference binding for new sessions, with sensible fallbacks.</summary>
    public (ModelRef Reference, string FallbackProviderId, string FallbackModelId) DefaultInference()
    {
        var reference = Config.Chat;
        if (reference is not null) return (reference, reference.ProviderId, reference.ModelId);
        var provider = Config.Providers.FirstOrDefault();
        if (provider is not null)
        {
            var model = provider.Models.FirstOrDefault(m => !m.Hidden) ?? provider.Models.FirstOrDefault();
            if (model is not null)
                return (new ModelRef { ProviderId = provider.Id, ModelId = model.Id }, provider.Id, model.Id);
        }
        return (new ModelRef { ProviderId = "openai", ModelId = AppConfig.DefaultModel },
            "openai", AppConfig.DefaultModel);
    }
}

/// <summary>Process-owned registry of live session runtimes.</summary>
public sealed class SessionRuntimeManager
{
    private readonly Dictionary<string, EventLog> _logs = new();

    public EventLog OpenLog(SessionId session)
    {
        lock (_logs)
        {
            if (_logs.TryGetValue(session.Value, out var existing)) return existing;
            var log = EventLog.Open(session.Value,
                SessionHistoryStore.SessionFile(AppConfig.SessionsRoot(), session));
            _logs[session.Value] = log;
            return log;
        }
    }

    public bool IsBusy(SessionId session)
    {
        lock (_logs)
        {
            return _logs.TryGetValue(session.Value, out var log)
                   && EventLog.HasActiveWriter(log.FilePath);
        }
    }

    public void Close(SessionId session, EventLog log)
    {
        lock (_logs)
        {
            if (_logs.Remove(session.Value)) log.Dispose();
        }
    }
}
