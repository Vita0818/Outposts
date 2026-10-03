using System.Text.Json.Nodes;

namespace Intatis.Core.Cowork;

/// <summary>Durable session-owned outbox for Cowork submitted-intent transactions (schema v1).</summary>
public sealed class SubmittedIntentStore
{
    public const string FileName = "submitted-intent-outbox.json";
    private readonly string _filePath;

    public SubmittedIntentStore(string sessionDir)
    {
        _filePath = Path.Combine(sessionDir, FileName);
    }

    public record OutboxEntry(string SubmissionId, int Attempt, string UserText, string? TaskId);

    public List<OutboxEntry> ReadAll()
    {
        if (!File.Exists(_filePath)) return new();
        try
        {
            var node = JsonNode.Parse(File.ReadAllText(_filePath)) as JsonArray;
            var list = new List<OutboxEntry>();
            if (node is not null)
            {
                foreach (var item in node)
                {
                    var obj = item as JsonObject;
                    if (obj is null) continue;
                    list.Add(new OutboxEntry(
                        (string?)obj?["submission_id"] ?? "",
                        (int?)obj?["attempt"] ?? 1,
                        (string?)obj?["user_text"] ?? "",
                        (string?)obj?["task_id"]
                    ));
                }
            }
            return list;
        }
        catch { return new(); }
    }

    public void Append(OutboxEntry entry)
    {
        var entries = ReadAll();
        // First-write-wins by submission id: skip duplicates.
        if (entries.Any(e => e.SubmissionId == entry.SubmissionId)) return;
        entries.Add(entry);
        var arr = new JsonArray();
        foreach (var e in entries)
        {
            arr.Add(new JsonObject
            {
                ["submission_id"] = e.SubmissionId,
                ["attempt"] = e.Attempt,
                ["user_text"] = e.UserText,
                ["task_id"] = e.TaskId,
            });
        }
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, arr.ToJsonString(System.Text.Json.Nodes.JsonSerializerOptions.Default));
        File.Move(tmp, _filePath, overwrite: true);
    }

    public bool Remove(string submissionId)
    {
        var entries = ReadAll();
        var removed = entries.RemoveAll(e => e.SubmissionId == submissionId);
        if (removed == 0) return false;
        var arr = new JsonArray();
        foreach (var e in entries)
        {
            arr.Add(new JsonObject
            {
                ["submission_id"] = e.SubmissionId,
                ["attempt"] = e.Attempt,
                ["user_text"] = e.UserText,
                ["task_id"] = e.TaskId,
            });
        }
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, arr.ToJsonString(System.Text.Json.Nodes.JsonSerializerOptions.Default));
        File.Move(tmp, _filePath, overwrite: true);
        return true;
    }

    public bool Contains(string submissionId) => ReadAll().Any(e => e.SubmissionId == submissionId);
}
