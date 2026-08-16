using System.Text;

namespace Intatis.Core.Providers;

/// <summary>
/// Incremental SSE parser: joins multi-line data fields, ignores event/id/retry and
/// comments, dispatches on blank line — mirroring the Apple SSEParser contract.
/// </summary>
public sealed class SseParser
{
    private readonly StringBuilder _pending = new();
    private bool _pendingHasData;

    public List<string> Consume(ReadOnlySpan<char> chunk)
    {
        var events = new List<string>();
        foreach (var rawLine in SplitLines(chunk))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                Dispatch(events);
                continue;
            }
            if (line[0] == ':') continue; // comment
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                var data = line[5..];
                if (data.Length > 0 && data[0] == ' ') data = data[1..];
                if (_pendingHasData) _pending.Append('\n');
                _pending.Append(data);
                _pendingHasData = true;
            }
            // event:/id:/retry: intentionally ignored
        }
        return events;
    }

    public List<string> Flush()
    {
        var events = new List<string>();
        Dispatch(events);
        return events;
    }

    private void Dispatch(List<string> events)
    {
        if (_pendingHasData)
        {
            events.Add(_pending.ToString());
            _pending.Clear();
            _pendingHasData = false;
        }
    }

    private static IEnumerable<ReadOnlyMemory<char>> SplitLines(ReadOnlySpan<char> chunk)
    {
        var memory = chunk.ToArray();
        int start = 0;
        for (var i = 0; i < memory.Length; i++)
        {
            if (memory[i] == '\n')
            {
                yield return memory.AsMemory(start, i - start);
                start = i + 1;
            }
        }
        if (start < memory.Length)
            yield return memory.AsMemory(start, memory.Length - start);
    }
}
