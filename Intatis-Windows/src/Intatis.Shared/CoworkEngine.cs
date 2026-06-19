namespace Intatis.Windows.Shared;

public sealed class CoworkEngine
{
    private const string MainAgentName = "main";

    public IReadOnlyList<string> Agents => _agents.Keys.ToList();
    public IReadOnlyList<(string From, string Text)> Transcript => _transcript.AsReadOnly();

    public sealed class CoworkAgentState
    {
        public string Name { get; }
        public string Workspace { get; set; }
        public string Model { get; set; }
        public CodeAgentSession Session { get; set; }

        public CoworkAgentState(string name, string workspace, string model, CodeAgentSession session)
        {
            Name = name;
            Workspace = workspace;
            Model = model;
            Session = session;
        }
    }

    public IReadOnlyList<CoworkAgentState> AgentStates => _agentStates.AsReadOnly();

    private readonly Dictionary<string, CoworkAgentState> _agents;
    private readonly string _baseWorkspace;
    private readonly IntatisConfig _config;
    private readonly List<(string From, string Text)> _transcript = new();
    private readonly IToolShellRunner _shell;
    private readonly IToolGitService _git;
    private readonly IPermissionResponder _responder;
    private readonly IConversationEventSink _eventSink;
    private readonly IPermissionReviewer? _permissionReviewer;
    private readonly PermissionProfile _profile;
    private readonly string _defaultModel;
    private readonly bool _includeUsage;
    private readonly bool _allowsShell;
    private readonly CoworkMessageBus _messageBus;
    private readonly int _maxIterations;
    private readonly List<CoworkAgentState> _agentStates = [];

    public string Send(string text, string? targetAgent)
        => SendAsync(text, targetAgent).GetAwaiter().GetResult();

    public async Task<string> SendAsync(
        string text,
        string? targetAgent,
        string? model = null,
        string? reasoning = null,
        IReadOnlyList<ImageAttachment>? images = null,
        bool? includeUsage = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDefaultAgent();

        var selected = ResolveAgent(targetAgent);
        if (selected is null)
            return $"no such agent: {targetAgent}";

        var resolvedModel = model ?? selected.Model;
        var response = await selected.Session.SendAsync(
            text,
            resolvedModel,
            reasoning,
            "cowork",
            images,
            includeUsage ?? _includeUsage,
            cancellationToken);

        _transcript.Add((selected.Name, text));
        _transcript.Add((selected.Name, response.Text));
        return response.Text;
    }

    public string Attach(string name, string? workspace = null, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "agent name is empty.";

        var lower = name.ToLowerInvariant();
        if (_agents.ContainsKey(lower))
            return $"{name} already exists.";

        var resolvedWorkspace = ResolveWorkspace(workspace);
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _defaultModel : model;

        var session = CreateSessionForAgent(lower, resolvedWorkspace, resolvedModel!);
        _agentStates.Add(session);
        _agents[lower] = session;
        return $"{name} attached to {resolvedWorkspace}.";
    }

    public string Detach(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "agent name is empty.";

        var lower = name.ToLowerInvariant();
        if (!_agents.TryGetValue(lower, out var removed))
            return $"no such agent: {name}";

        _agentStates.RemoveAll(agent => string.Equals(agent.Name, lower, StringComparison.OrdinalIgnoreCase));
        _agents.Remove(lower);
        EnsureDefaultAgent();
        return $"{removed.Name} detached.";
    }

    public void Clear()
    {
        _transcript.Clear();
        foreach (var agent in _agentStates)
            agent.Session.Clear();
    }

    public CoworkEngine(
        IntatisConfig config,
        string baseWorkspace,
        IToolShellRunner? shell = null,
        IToolGitService? git = null,
        IPermissionResponder? responder = null,
        PermissionProfile profile = PermissionProfile.Reviewed,
        IConversationEventSink? eventSink = null,
        CoworkMessageBus? messageBus = null,
        bool allowsShell = true,
        int maxIterations = 8,
        IPermissionReviewer? permissionReviewer = null)
    {
        _config = config;
        _baseWorkspace = baseWorkspace;
        _shell = shell ?? new ProcessShellRunner();
        _git = git ?? new ProcessGitService(_shell);
        _responder = responder ?? new AllowAllResponder();
        _eventSink = eventSink ?? new NullConversationEventSink();
        _permissionReviewer = permissionReviewer;
        _profile = profile;
        _allowsShell = allowsShell;
        _maxIterations = maxIterations;
        _messageBus = messageBus ?? new CoworkMessageBus(eventSink: _eventSink);
        _agents = new Dictionary<string, CoworkAgentState>(StringComparer.OrdinalIgnoreCase);
        _defaultModel = config.Model;
        _includeUsage = config.IncludeUsage;
        AttachInternal(MainAgentName, _baseWorkspace, _defaultModel);

    }

    public async Task<string> AskAsync(string from, string to, string question)
    {
        var normalizedFrom = string.IsNullOrWhiteSpace(from) ? "unknown" : from;
        var toAgent = ResolveAgent(to);
        if (toAgent is null)
            return $"no such agent: {to}";

        if (string.Equals(toAgent.Name, normalizedFrom, StringComparison.OrdinalIgnoreCase))
            return "self-targeted ask is blocked.";

        var forwardedQuestion = await _messageBus.DeliverAsync(
            normalizedFrom,
            toAgent.Name,
            $"[{normalizedFrom}] {question}");
        if (forwardedQuestion is null)
            return "your message was blocked by the mediator";

        var response = await toAgent.Session.SendAsync(
            forwardedQuestion,
            toAgent.Model,
            userGoal: "ask_agent",
            includeUsage: _includeUsage);

        _transcript.Add((toAgent.Name, response.Text));

        var forwardedAnswer = await _messageBus.DeliverAsync(
            toAgent.Name,
            normalizedFrom,
            response.Text);
        if (forwardedAnswer is null)
            return "the reply was blocked by the mediator";

        return forwardedAnswer;
    }

    private CoworkAgentState? ResolveAgent(string? rawTarget)
    {
        if (!string.IsNullOrWhiteSpace(rawTarget))
        {
            return _agents.TryGetValue(rawTarget, out var agent)
                ? agent
                : null;
        }

        if (_agentStates.Count == 0)
            return null;

        return _agentStates[0];
    }

    private CoworkAgentState CreateSessionForAgent(string name, string workspace, string model)
    {
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? _defaultModel : model;
        var session = new CodeAgentSession(
            _config,
            workspace,
            name,
            _profile,
            shell: _shell,
            git: _git,
            messenger: new CoworkAgentMessenger(this),
            responder: _responder,
            eventSink: _eventSink,
            permissionReviewer: _permissionReviewer,
            allowsShell: _allowsShell,
            maxIterations: _maxIterations);
        return new CoworkAgentState(name, workspace, resolvedModel, session);
    }

    private CoworkAgentState AttachInternal(string name, string workspace, string model)
    {
        var state = CreateSessionForAgent(name, workspace, model);
        _agents[name] = state;
        _agentStates.Add(state);
        return state;
    }

    private string ResolveWorkspace(string? workspace)
    {
        var requested = !string.IsNullOrWhiteSpace(workspace) ? workspace : _baseWorkspace;
        if (string.IsNullOrWhiteSpace(requested))
            requested = Environment.CurrentDirectory;

        return WorkspaceTools.ResolveWorkspace(_config.Workspace, requested);
    }

    private void EnsureDefaultAgent()
    {
        if (_agents.Count > 0)
            return;

        AttachInternal(MainAgentName, _baseWorkspace, _defaultModel);
    }

    private sealed class CoworkAgentMessenger(CoworkEngine owner) : IToolAgentMessenger
    {
        public Task<string> AskAsync(string from, string to, string question) => owner.AskAsync(from, to, question);
    }
}
