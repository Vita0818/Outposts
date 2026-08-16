using Intatis.Core.Protocol;

namespace Intatis.Core.Cowork;

public enum WorkTaskStatus
{
    Pending,
    Ready,
    InProgress,
    Blocked,
    Completed,
    Failed,
    Cancelled,
}

public enum WorkTaskPriority
{
    Low,
    Normal,
    High,
    Critical,
}

public static class WorkTaskStatusExtensions
{
    public static string ToWire(this WorkTaskStatus status) => status switch
    {
        WorkTaskStatus.InProgress => "in_progress",
        _ => status.ToString().ToLowerInvariant(),
    };

    public static bool IsTerminal(this WorkTaskStatus status)
        => status is WorkTaskStatus.Completed or WorkTaskStatus.Failed or WorkTaskStatus.Cancelled;
}

public sealed record WorkTask
{
    public required WorkTaskId Id { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public List<string> AcceptanceCriteria { get; init; } = [];
    public WorkTaskStatus Status { get; init; } = WorkTaskStatus.Pending;
    public WorkTaskPriority Priority { get; init; } = WorkTaskPriority.Normal;
    public List<WorkTaskId> DependsOn { get; init; } = [];
    public string? Result { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public int Revision { get; init; } = 0;
}

public enum WorkTaskGraphViolation
{
    DuplicateTaskId,
    MissingDependency,
    SelfDependency,
    CycleDetected,
    InvalidStatusTransition,
    TerminalDependency,
    MissingCompletionResult,
}

/// <summary>
/// User-visible plan graph. The graph never treats an execution-layer invocation
/// result as WorkTask completion — completion requires a non-empty result.
/// </summary>
public sealed class WorkTaskGraph
{
    private readonly object _gate = new();
    private readonly Dictionary<WorkTaskId, WorkTask> _tasks = new();

    public WorkTask? this[WorkTaskId id]
    {
        get { lock (_gate) { return _tasks.GetValueOrDefault(id); } }
    }

    public List<WorkTask> All()
    {
        lock (_gate) { return _tasks.Values.OrderBy(t => t.CreatedAt).ToList(); }
    }

    public WorkTask Add(WorkTask task)
    {
        lock (_gate)
        {
            if (_tasks.ContainsKey(task.Id))
                throw new InvalidOperationException($"{WorkTaskGraphViolation.DuplicateTaskId}: {task.Id}");
            foreach (var dependency in task.DependsOn)
            {
                if (dependency == task.Id)
                    throw new InvalidOperationException($"{WorkTaskGraphViolation.SelfDependency}: {task.Id}");
                if (!_tasks.ContainsKey(dependency))
                    throw new InvalidOperationException($"{WorkTaskGraphViolation.MissingDependency}: {dependency}");
                if (_tasks[dependency].Status.IsTerminal())
                    throw new InvalidOperationException($"{WorkTaskGraphViolation.TerminalDependency}: {dependency}");
            }
            ValidateNoCycle();

            var effective = task.Status == WorkTaskStatus.Pending && IsReady(task)
                ? task with { Status = WorkTaskStatus.Ready, Revision = 1 }
                : task with { Revision = 1 };
            _tasks[effective.Id] = effective;
            return effective;
        }
    }

    public WorkTask Transition(WorkTaskId id, WorkTaskStatus to, string? result = null)
    {
        lock (_gate)
        {
            var task = _tasks.GetValueOrDefault(id)
                ?? throw new InvalidOperationException($"{WorkTaskGraphViolation.MissingDependency}: {id}");
            if (!IsValidTransition(task.Status, to))
                throw new InvalidOperationException(
                    $"{WorkTaskGraphViolation.InvalidStatusTransition}: {task.Status} -> {to}");
            if (to == WorkTaskStatus.Completed && string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException($"{WorkTaskGraphViolation.MissingCompletionResult}: {id}");

            var updated = task with
            {
                Status = to,
                Result = result ?? task.Result,
                UpdatedAt = DateTime.UtcNow,
                Revision = task.Revision + 1,
            };
            _tasks[id] = updated;

            // Recompute readiness of pending dependents.
            foreach (var dependent in _tasks.Values.Where(t => t.Status == WorkTaskStatus.Pending))
            {
                if (IsReady(dependent))
                    _tasks[dependent.Id] = dependent with { Status = WorkTaskStatus.Ready };
            }
            return updated;
        }
    }

    private bool IsReady(WorkTask task)
        => task.DependsOn.Count == 0
           || task.DependsOn.All(d => _tasks.GetValueOrDefault(d)?.Status == WorkTaskStatus.Completed);

    private static bool IsValidTransition(WorkTaskStatus from, WorkTaskStatus to) => from switch
    {
        WorkTaskStatus.Pending => to is WorkTaskStatus.Ready or WorkTaskStatus.Blocked or WorkTaskStatus.Cancelled,
        WorkTaskStatus.Ready => to is WorkTaskStatus.InProgress or WorkTaskStatus.Blocked or WorkTaskStatus.Cancelled,
        WorkTaskStatus.InProgress => to is WorkTaskStatus.Completed or WorkTaskStatus.Blocked
            or WorkTaskStatus.Failed or WorkTaskStatus.Cancelled,
        WorkTaskStatus.Blocked => to is WorkTaskStatus.Ready or WorkTaskStatus.Cancelled,
        WorkTaskStatus.Failed => to is WorkTaskStatus.Ready, // retry
        WorkTaskStatus.Cancelled => to is WorkTaskStatus.Ready, // retry
        WorkTaskStatus.Completed => false,
        _ => false,
    };

    private void ValidateNoCycle()
    {
        var visited = new HashSet<WorkTaskId>();
        var stack = new HashSet<WorkTaskId>();
        void Visit(WorkTaskId id)
        {
            if (stack.Contains(id))
                throw new InvalidOperationException($"{WorkTaskGraphViolation.CycleDetected}: {id}");
            if (!visited.Add(id)) return;
            stack.Add(id);
            foreach (var dependency in _tasks[id].DependsOn) Visit(dependency);
            stack.Remove(id);
        }
        foreach (var task in _tasks.Values) Visit(task.Id);
    }
}
