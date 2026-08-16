using System.Security.Cryptography;

namespace Intatis.Core;

public enum SessionKind
{
    Chat,
    Code,
    Cowork,
}

public static class SessionKindExtensions
{
    public static string ToWire(this SessionKind kind) => kind switch
    {
        SessionKind.Chat => "chat",
        SessionKind.Code => "code",
        SessionKind.Cowork => "cowork",
        _ => "chat",
    };

    public static SessionKind FromWire(string value) => value switch
    {
        "code" => SessionKind.Code,
        "cowork" => SessionKind.Cowork,
        _ => SessionKind.Chat,
    };

    /// <summary>The session ID prefix also encodes the kind (sess_ / code_ / cowork_).</summary>
    public static string IdPrefix(this SessionKind kind) => kind switch
    {
        SessionKind.Chat => "sess_",
        SessionKind.Code => "code_",
        SessionKind.Cowork => "cowork_",
        _ => "sess_",
    };

    public static bool UsesWorkspace(this SessionKind kind) => kind != SessionKind.Chat;
}

/// <summary>
/// Typed identifiers mirror the Apple project's TypedID scheme: random
/// lowercase alphanumerics of length 8 behind a short prefix such as
/// "sess_" or "msg_". They serialize as bare JSON strings.
/// </summary>
public readonly record struct TypedId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator string(TypedId id) => id.Value;
}

public static class IdGen
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string Random(string prefix, int length = 8)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return prefix + new string(chars);
    }
}

public readonly record struct SessionId(string Value)
{
    public static SessionId New(SessionKind kind) => new(IdGen.Random(kind.IdPrefix()));
    public SessionKind Kind => Value.StartsWith("code_") ? SessionKind.Code
        : Value.StartsWith("cowork_") ? SessionKind.Cowork
        : SessionKind.Chat;
    public static implicit operator string(SessionId id) => id.Value;
}

public readonly record struct MessageId(string Value)
{
    public static MessageId New() => new(IdGen.Random("msg_"));
    public static implicit operator string(MessageId id) => id.Value;
}

public readonly record struct SubmissionId(string Value)
{
    public static SubmissionId New() => new(IdGen.Random("sub_"));
    public static implicit operator string(SubmissionId id) => id.Value;
}

public readonly record struct TurnId(string Value)
{
    public static TurnId New() => new(IdGen.Random("turn_"));
    public static implicit operator string(TurnId id) => id.Value;
}

public readonly record struct TaskId(string Value)
{
    public static TaskId New() => new(IdGen.Random("task_"));
    public static implicit operator string(TaskId id) => id.Value;
}

public readonly record struct WorkTaskId(string Value)
{
    public static WorkTaskId New() => new(IdGen.Random("wt_"));
    public static implicit operator string(WorkTaskId id) => id.Value;
}

public readonly record struct GoalId(string Value)
{
    public static GoalId New() => new(IdGen.Random("goal_"));
    public static implicit operator string(GoalId id) => id.Value;
}

public readonly record struct ArtifactId(string Value)
{
    public static ArtifactId New() => new(IdGen.Random("art_"));
    public static implicit operator string(ArtifactId id) => id.Value;
}

public readonly record struct RequestId(string Value)
{
    public static RequestId New() => new(IdGen.Random("req_"));
    public static implicit operator string(RequestId id) => id.Value;
}
