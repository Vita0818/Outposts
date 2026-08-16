using Intatis.Core.Protocol;

namespace Intatis.Core.Session;

public sealed record SessionSummary
{
    public string Id { get; init; } = "";
    public SessionKind Kind { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int EventCount { get; init; }
    public string? DisplayName { get; init; }
}

/// <summary>
/// Filesystem layout for session persistence:
/// one session = &lt;root&gt;/&lt;sessionID&gt;/events.jsonl (+ artifacts/, session.json).
/// </summary>
public static class SessionHistoryStore
{
    public const string EventsFileName = "events.jsonl";

    public static string SessionDirectory(string root, string sessionId)
    {
        if (sessionId.Contains("..") || sessionId.Contains('/') || sessionId.Contains('\\') || sessionId.Contains(Path.DirectorySeparatorChar))
            throw new ArgumentException("invalid session id", nameof(sessionId));
        return Path.Combine(root, sessionId);
    }

    public static string SessionFile(string root, string sessionId)
        => Path.Combine(SessionDirectory(root, sessionId), EventsFileName);

    public static string ArtifactsDir(string root, string sessionId)
        => Path.Combine(SessionDirectory(root, sessionId), "artifacts");

    public static List<SessionSummary> RecentSessions(string root, SessionKind? kind = null, int limit = 50)
    {
        if (!Directory.Exists(root)) return [];
        var summaries = new List<SessionSummary>();
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            SessionKind sessionKind;
            try { sessionKind = new SessionId(name).Kind; }
            catch (Exception) { continue; }
            if (kind is { } k && sessionKind != k) continue;

            var eventsFile = Path.Combine(dir, EventsFileName);
            if (!File.Exists(eventsFile)) continue;

            var projection = SessionProjectionStore.Load(eventsFile);
            var displayName = projection?.DisplayName;
            DateTime updated = File.GetLastWriteTimeUtc(eventsFile);
            int count = 0;
            try
            {
                using var reader = new StreamReader(eventsFile);
                while (reader.ReadLine() is not null) count++;
            }
            catch (IOException) { }

            summaries.Add(new SessionSummary
            {
                Id = name,
                Kind = sessionKind,
                UpdatedAt = updated,
                EventCount = count,
                DisplayName = displayName,
            });
        }
        return summaries.OrderByDescending(s => s.UpdatedAt).Take(limit).ToList();
    }

    public static void DeleteSession(string root, string sessionId)
    {
        var dir = SessionDirectory(root, sessionId);
        if (EventLog.HasActiveWriter(SessionFile(root, sessionId)))
            throw new EventLogException("writer_already_active", "cannot delete a session with a running runtime");
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
