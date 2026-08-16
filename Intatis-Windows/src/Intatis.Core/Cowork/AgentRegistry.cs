using Intatis.Core.Protocol;

namespace Intatis.Core.Cowork;

/// <summary>
/// Secret-free exact inference identity for one agent. Once attached, the binding is
/// frozen for the session: changing menus or the session default never re-routes it.
/// </summary>
public sealed record AgentInferenceBinding
{
    public required string ProviderId { get; init; }
    public required string ModelId { get; init; }

    public string DisplayLabel => $"{ProviderId}/{ModelId}";
}

public sealed record Agent
{
    public required string Name { get; init; }
    public required string WorkspaceRoot { get; init; }
    public required string Model { get; init; }
    public AgentInferenceBinding? InferenceBinding { get; init; }
    public PermissionProfile Profile { get; init; } = PermissionProfile.Reviewed;
    public string Role { get; init; } = "worker";
    public int CoordinationDepth { get; init; } = 2;

    public static string MainAgentId => "main";
    public static string PermissionReviewerId => "permission-reviewer";
}

public sealed class AgentRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Agent> _agents = new(StringComparer.Ordinal);

    public void Add(Agent agent)
    {
        lock (_gate)
        {
            if (agent.Name is Agent.MainAgentId or Agent.PermissionReviewerId)
                throw new InvalidOperationException($"'{agent.Name}' is a reserved agent name");
            if (_agents.ContainsKey(agent.Name))
                throw new InvalidOperationException($"agent '{agent.Name}' already attached");
            _agents[agent.Name] = agent;
        }
    }

    public void Upsert(Agent agent)
    {
        lock (_gate) { _agents[agent.Name] = agent; }
    }

    public bool Remove(string name)
    {
        lock (_gate) { return _agents.Remove(name); }
    }

    public Agent? AgentNamed(string name)
    {
        lock (_gate) { return _agents.GetValueOrDefault(name); }
    }

    public List<Agent> All()
    {
        lock (_gate) { return _agents.Values.OrderBy(a => a.Name, StringComparer.Ordinal).ToList(); }
    }

    public List<string> Names()
    {
        lock (_gate) { return _agents.Keys.Order().ToList(); }
    }

    public string RosterLine()
    {
        lock (_gate)
        {
            return string.Join("; ", _agents.Values
                .OrderBy(a => a.Name, StringComparer.Ordinal)
                .Select(a => $"{a.Name} · {a.Model} · {a.Role}"));
        }
    }
}
