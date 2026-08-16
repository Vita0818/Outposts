using System.Text;
using System.Text.Json.Nodes;

namespace Intatis.Core.Session;

public sealed record SessionProjectionDocument
{
    public int SchemaVersion { get; init; } = 2;
    public string SessionId { get; init; } = "";
    public string Kind { get; init; } = "chat";
    public string? DisplayName { get; init; }
    public long ProjectedThroughSeq { get; init; }
    public int? SettingsRevision { get; init; }
}

/// <summary>
/// session.json is a rebuildable, secret-free derived cache. Deleting it is always
/// safe; events.jsonl is the only canonical authority.
/// </summary>
public static class SessionProjectionStore
{
    public const string FileName = "session.json";

    public static string FileFor(string eventsFile)
        => Path.Combine(Path.GetDirectoryName(eventsFile)!, FileName);

    public static SessionProjectionDocument? Load(string eventsFile)
    {
        var path = FileFor(eventsFile);
        if (!File.Exists(path)) return null;
        try
        {
            var node = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
            if (node is null) return null;
            return new SessionProjectionDocument
            {
                SchemaVersion = (int?)node["schema_version"] ?? 2,
                SessionId = (string?)node["session_id"] ?? "",
                Kind = (string?)node["kind"] ?? "chat",
                DisplayName = (string?)node["display_name"],
                ProjectedThroughSeq = (int?)node["projected_through_seq"] ?? 0,
                SettingsRevision = (int?)node["settings_revision"],
            };
        }
        catch (Exception)
        {
            return null; // derived cache: unreadable means rebuild
        }
    }

    public static SessionProjectionDocument Rebuild(EventLog log)
    {
        string? displayName = null;
        int? revision = null;
        string kind = "chat";
        long through = -1;
        foreach (var envelope in log.Replay())
        {
            through = envelope.Seq;
            if (envelope.Type == Protocol.EventType.SessionSettingsUpdated)
            {
                var o = envelope.Payload as JsonObject;
                displayName = (string?)o?["display_name"] ?? displayName;
                revision = (int?)o?["revision"] ?? revision;
                kind = (string?)o?["kind"] ?? kind;
            }
        }
        return new SessionProjectionDocument
        {
            SessionId = log.SessionId,
            Kind = kind,
            DisplayName = displayName,
            ProjectedThroughSeq = through,
            SettingsRevision = revision,
        };
    }

    public static void Save(string eventsFile, SessionProjectionDocument document)
    {
        var path = FileFor(eventsFile);
        var json = new JsonObject
        {
            ["schema_version"] = document.SchemaVersion,
            ["session_id"] = document.SessionId,
            ["kind"] = document.Kind,
            ["display_name"] = document.DisplayName,
            ["projected_through_seq"] = document.ProjectedThroughSeq,
            ["settings_revision"] = document.SettingsRevision,
        };
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json.ToJsonString(Jsonx.Pretty), new UTF8Encoding(false));
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>Set or rename the display name EventLog-first via a settings event append.</summary>
    public static void UpdateDisplayName(EventLog log, SessionKind kind, string displayName, string changeKind = "updated")
    {
        if (string.IsNullOrWhiteSpace(displayName)) return;
        displayName = displayName.Trim();
        if (displayName.Length > 120) displayName = displayName[..120];

        var current = Rebuild(log);
        var revision = (current.SettingsRevision ?? 0) + 1;
        log.Append(Protocol.EventType.SessionSettingsUpdated, new Protocol.SessionSettingsUpdatedPayload
        {
            Revision = revision,
            PreviousRevision = current.SettingsRevision,
            ChangeKind = changeKind,
            Kind = kind.ToWire(),
            DisplayName = displayName,
        }.ToJson());
        Save(FileFor(log.FilePath), Rebuild(log));
    }
}
