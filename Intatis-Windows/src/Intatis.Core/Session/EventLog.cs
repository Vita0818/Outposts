using System.Text;
using System.Text.Json.Nodes;
using Intatis.Core.Protocol;

namespace Intatis.Core.Session;

public sealed class EventLogException : Exception
{
    public string Code { get; }

    public EventLogException(string code, string message) : base(message) => Code = code;
}

/// <summary>
/// Append-only JSONL session store. Append is the only mutation; replay and stream are
/// projections. Sequence numbers are monotonic and gap-free from zero per session, and
/// the writer lease guarantees a single runtime per session file across processes.
/// </summary>
public sealed class EventLog : IDisposable
{
    private readonly object _gate = new();
    private FileStream? _writerLock;
    private FileStream? _dataStream;
    private StreamWriter? _dataWriter;
    private long _lastSeq = -1;
    private bool _disposed;

    public string SessionId { get; }
    public string FilePath { get; }
    public string SessionDirectory { get; }

    /// <summary>Raised after an envelope has been durably appended.</summary>
    public event Action<Envelope>? EnvelopeAppended;

    private EventLog(string sessionId, string file, string directory)
    {
        SessionId = sessionId;
        FilePath = file;
        SessionDirectory = directory;
    }

    public static EventLog Open(string sessionId, string file)
    {
        var directory = Path.GetDirectoryName(file)!;
        Directory.CreateDirectory(directory);

        // Writer lease: exclusive handle on the sidecar lock; a second runtime fails closed.
        var lockPath = file + ".writer.lock";
        FileStream writerLock;
        try
        {
            // OpenOrCreate + FileShare.None: a live second runtime conflicts on the share
            // mode, while a stale lock file left by a crashed process opens cleanly.
            writerLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            throw new EventLogException("writer_already_active",
                $"another runtime already owns the writer lease for {sessionId}");
        }

        var log = new EventLog(sessionId, file, directory) { _writerLock = writerLock };
        log.RescanTail();
        return log;
    }

    public static bool HasActiveWriter(string file)
        => File.Exists(file + ".writer.lock")
           && ExceptionsOnExclusiveOpen(file + ".writer.lock");

    private static bool ExceptionsOnExclusiveOpen(string path)
    {
        try
        {
            using var probe = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private void RescanTail()
    {
        _lastSeq = -1;
        if (!File.Exists(FilePath)) return;
        using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            Envelope envelope;
            try { envelope = Envelope.FromJsonLine(line); }
            catch (Exception) { continue; } // fail-soft: skip undecodable lines (future types)
            if (envelope.Session != SessionId)
                throw new EventLogException("session_mismatch",
                    $"event line belongs to session {envelope.Session}, expected {SessionId}");
            if (envelope.Seq <= _lastSeq)
                throw new EventLogException("non_monotonic_sequence",
                    $"sequence regression at seq {envelope.Seq} (last {_lastSeq})");
            _lastSeq = envelope.Seq;
        }
    }

    public long LastSeq => _lastSeq;

    public Envelope Append(string type, JsonNode? payload, DateTime? ts = null, bool flush = true)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_dataWriter is null)
            {
                _dataStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                _dataStream.Seek(0, SeekOrigin.End);
                _dataWriter = new StreamWriter(_dataStream, Encoding.UTF8) { AutoFlush = false };
            }
            var envelope = new Envelope
            {
                Seq = _lastSeq + 1,
                Ts = ts ?? DateTime.UtcNow,
                Session = SessionId,
                V = 1,
                Type = type,
                Payload = payload?.DeepClone(),
            };
            _dataWriter.WriteLine(envelope.ToJsonLine());
            if (flush)
            {
                _dataWriter.Flush();
                _dataStream!.Flush(true);
            }
            _lastSeq = envelope.Seq;
            EnvelopeAppended?.Invoke(envelope);
            return envelope;
        }
    }

    public List<Envelope> Replay(long fromSeq = 0)
    {
        var result = new List<Envelope>();
        if (!File.Exists(FilePath)) return result;
        using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            Envelope envelope;
            try { envelope = Envelope.FromJsonLine(line); }
            catch (Exception) { continue; }
            if (envelope.Seq >= fromSeq) result.Add(envelope);
        }
        return result;
    }

    /// <summary>Replay, then follow live appends until the channel completes.</summary>
    public async IAsyncEnumerable<Envelope> StreamAllAsync(
        long fromSeq,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var envelope in Replay(fromSeq))
            yield return envelope;

        var pending = System.Threading.Channels.Channel.CreateUnbounded<Envelope>();
        void Handler(Envelope e) => pending.Writer.TryWrite(e);
        EnvelopeAppended += Handler;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var next = await pending.Reader.ReadAsync(ct).ConfigureAwait(false);
                if (next.Seq >= fromSeq) yield return next;
            }
        }
        catch (OperationCanceledException) { /* normal stop */ }
        finally { EnvelopeAppended -= Handler; }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _dataWriter?.Flush(); } catch (IOException) { }
            _dataWriter?.Dispose();
            _dataWriter = null;
            _dataStream?.Dispose();
            _dataStream = null;
            _writerLock?.Dispose();
            _writerLock = null;
            try { File.Delete(FilePath + ".writer.lock"); } catch (IOException) { }
        }
    }
}
