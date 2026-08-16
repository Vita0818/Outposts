using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dashis;

public readonly record struct ProviderID(string RawValue)
{
  public static readonly ProviderID Codex = new("codex");
  public static readonly ProviderID Claude = new("claude");
  public static readonly ProviderID Google = new("google");
  public static readonly ProviderID OpenRouter = new("openrouter");
}

public enum ProviderScopeKind
{
  Personal,
  ApiKey,
  Workspace,
  Project,
  Manual
}

public sealed record ProviderScope(ProviderScopeKind Kind, string Label)
{
  public static ProviderScope Personal(string label) => new(ProviderScopeKind.Personal, label);
  public static ProviderScope Project(string label) => new(ProviderScopeKind.Project, label);
  public static ProviderScope Workspace(string label) => new(ProviderScopeKind.Workspace, label);
  public static ProviderScope Manual(string label) => new(ProviderScopeKind.Manual, label);
}

public enum UsageSourceKind
{
  OfficialDirect,
  OfficialDerived,
  OfficialLocalBridge,
  ExperimentalPrivate,
  ManualOnly
}

public static class UsageSourceKindExtensions
{
  public static string Label(this UsageSourceKind value) => value switch
  {
    UsageSourceKind.OfficialDirect => "Official",
    UsageSourceKind.OfficialDerived => "Official · Estimated",
    UsageSourceKind.OfficialLocalBridge => "Official · Local",
    UsageSourceKind.ExperimentalPrivate => "Experimental",
    UsageSourceKind.ManualOnly => "Manual check",
    _ => "Official"
  };

  public static TimeSpan DefaultStaleInterval(this UsageSourceKind value) => value switch
  {
    UsageSourceKind.OfficialLocalBridge => TimeSpan.FromMinutes(15),
    UsageSourceKind.ManualOnly => TimeSpan.FromDays(7),
    _ => TimeSpan.FromMinutes(5)
  };

  public static TimeSpan DefaultExpirationInterval(this UsageSourceKind value) => value switch
  {
    UsageSourceKind.ManualOnly => TimeSpan.FromDays(7),
    _ => TimeSpan.FromDays(1)
  };
}

public sealed record QuotaWindow(
  string Id,
  string Label,
  double? Used,
  double? Limit,
  double? Remaining,
  double? UsedPercentage,
  double? RemainingPercentage,
  DateTime? ResetsAt,
  string Unit,
  bool IsEstimated
)
{
  public bool IsExceeded =>
    Remaining is < 0 ||
    (Used is not null && Limit is not null && Used > Limit) ||
    (UsedPercentage is > 100);
}

public sealed record ProviderBalance(
  string Label,
  double? Used,
  double? Limit,
  double? Remaining,
  string Unit,
  string? ResetDescription
)
{
  public double? UsedPercentage => (Used is not null && Limit is > 0) ? Used / Limit * 100.0 : null;
  public bool IsExceeded => Remaining is < 0 || (Used is not null && Limit is not null && Used > Limit);
}

public sealed record ProviderMetric(string Key, string Label, double Value, string Unit);
public sealed record ProviderWarning(string Id, string Message);
public sealed record ProviderFailure(string Operation, string Message)
{
  public string Id => Operation;
}

public sealed record ProviderSnapshot(
  ProviderID ProviderID,
  ProviderScope Scope,
  UsageSourceKind SourceKind,
  DateTime ObservedAt,
  IReadOnlyList<QuotaWindow> Windows,
  ProviderBalance? Balance,
  IReadOnlyList<ProviderMetric> Metrics,
  IReadOnlyList<ProviderWarning> Warnings,
  IReadOnlyList<ProviderFailure> PartialFailures
)
{
  public bool HasData
  {
    get
    {
      var hasWindowValue = Windows.Any(window =>
      {
        var values = new[] { window.Used, window.Limit, window.Remaining, window.UsedPercentage, window.RemainingPercentage };
        return values.Any(IsFinite);
      });

      var hasBalanceValue = Balance is not null
        && new[] { Balance.Used, Balance.Limit, Balance.Remaining }.Any(IsFinite);

      return hasWindowValue
        || hasBalanceValue
        || Metrics.Any(metric => IsFinite(metric.Value));
    }
  }

  private static bool IsFinite(double? value)
    => value is not null && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);

  public QuotaWindow? MostUrgentWindow
  {
    get
    {
      if (Windows.Count == 0) return null;
      return Windows
        .OrderBy(window => window.RemainingPercentage ?? double.MaxValue)
        .ThenBy(window => window.ResetsAt ?? DateTime.MaxValue)
        .First();
    }
  }
}

public enum SnapshotFreshness
{
  Fresh,
  Stale,
  Expired,
  Missing
}

public static class FreshnessPolicy
{
  public static SnapshotFreshness FreshnessOf(ProviderSnapshot? snapshot, DateTime? now = null)
  {
    now ??= DateTime.UtcNow;
    if (snapshot is null || !snapshot.HasData) return SnapshotFreshness.Missing;
    var age = now.Value - snapshot.ObservedAt;
    if (age < TimeSpan.FromSeconds(-60)) return SnapshotFreshness.Missing;
    if (age <= snapshot.SourceKind.DefaultStaleInterval()) return SnapshotFreshness.Fresh;
    if (age <= snapshot.SourceKind.DefaultExpirationInterval()) return SnapshotFreshness.Stale;
    return SnapshotFreshness.Expired;
  }
}

public interface IProviderUsageClient<TContext>
{
  ProviderID ProviderID { get; }
  Task<ProviderSnapshot> FetchSnapshotAsync(TContext context);
}

public readonly struct Result<T>
{
  public bool IsSuccess { get; }
  public T? Value { get; }
  public Exception? Error { get; }

  private Result(T? value)
  {
    IsSuccess = true;
    Value = value;
    Error = null;
  }

  private Result(Exception error)
  {
    IsSuccess = false;
    Value = default;
    Error = error;
  }

  public static Result<T> Success(T value) => new(value);
  public static Result<T> Failure(Exception error) => new(error);
}

public interface ILocalizedError
{
  string? ErrorDescription { get; }
}

public static class ProviderJson
{
  public static IReadOnlyDictionary<string, JsonElement>? Dictionary(object? value)
  {
    return value switch
    {
      JsonElement element when element.ValueKind == JsonValueKind.Object
        => element.EnumerateObject().ToDictionary(item => item.Name, item => item.Value),
      IDictionary<string, JsonElement> dictionary => dictionary,
      _ => null
    };
  }

  public static IReadOnlyDictionary<string, JsonElement>? OptionalDictionary(object? value) => Dictionary(value);

  public static IReadOnlyList<JsonElement> Array(object? value)
  {
    return value is JsonElement element && element.ValueKind == JsonValueKind.Array
      ? element.EnumerateArray().ToList()
      : Array.Empty<JsonElement>();
  }

  public static string? String(object? value)
  {
    if (value is null) return null;

    return value switch
    {
      string s => s,
      JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),
      JsonElement { ValueKind: JsonValueKind.Number } e => e.GetRawText(),
      bool b => b ? "true" : "false",
      _ => null
    };
  }

  public static double? Number(object? value)
  {
    if (value is null) return null;

    return value switch
    {
      int i => i,
      long l => l,
      double d => IsFinite(d) ? d : null,
      float f => IsFinite(f) ? f : null,
      decimal m => IsFinite((double)m) ? (double)m : null,
      JsonElement e when e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var parsed) => IsFinite(parsed) ? parsed : null,
      JsonElement e when e.ValueKind == JsonValueKind.String => ParseDouble(e.GetString()),
      _ => null
    };
  }

  public static int? Int(object? value)
  {
    var number = Number(value);
    if (number is null) return null;
    if (!double.IsFinite(number.Value)) return null;
    if (number.Value != Math.Truncate(number.Value)) return null;
    try
    {
      return checked((int)number.Value);
    }
    catch
    {
      return null;
    }
  }

  public static bool? Bool(object? value)
  {
    if (value is null) return null;
    if (value is bool b) return b;
    if (value is JsonElement { ValueKind: JsonValueKind.True }) return true;
    if (value is JsonElement { ValueKind: JsonValueKind.False }) return false;
    if (value is string s)
    {
      if (s.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
      if (s.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
    }
    if (value is JsonElement stringElement && stringElement.ValueKind == JsonValueKind.String)
    {
      var stringValue = stringElement.GetString();
      if (stringValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true) return true;
      if (stringValue?.Equals("false", StringComparison.OrdinalIgnoreCase) == true) return false;
    }
    return null;
  }

  public static DateTime? Date(object? value)
  {
    var raw = String(value);
    if (string.IsNullOrWhiteSpace(raw)) return null;
    return TryParseDate(raw, out var parsed) ? parsed : null;
  }

  public static string SafeMessage(Exception error)
  {
    if (error is ILocalizedError localized && !string.IsNullOrWhiteSpace(localized.ErrorDescription))
    {
      return localized.ErrorDescription!;
    }

    return string.IsNullOrWhiteSpace(error.Message)
      ? "The provider check failed."
      : error.Message;
  }

  public static double ClampForDisplay(double value) => Math.Clamp(value, 0, 100);

  public static Task<Result<T>> CaptureProviderResultAsync<T>(Func<Task<T>> operation)
  {
    try
    {
      return operation().ContinueWith(task => task.IsFaulted
        ? Result<T>.Failure(task.Exception!.InnerException ?? task.Exception!)
        : Result<T>.Success(task.Result));
    }
    catch (Exception error)
    {
      return Task.FromResult(Result<T>.Failure(error));
    }
  }

  private static bool TryParseDate(string value, out DateTime parsed)
  {
    if (DateTime.TryParseExact(
          value,
          "yyyy'-'MM'-'dd'T'HH':'mm':'ssK",
          CultureInfo.InvariantCulture,
          DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
          out parsed
        ))
    {
      return true;
    }

    if (DateTime.TryParse(
          value,
          CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
          out parsed
        ))
    {
      return true;
    }

    if (DateTimeOffset.TryParse(
          value,
          CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
          out var parsedOffset)
      )
    {
      parsed = parsedOffset.UtcDateTime;
      return true;
    }

    return false;
  }

  private static double? ParseDouble(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return double.TryParse(
      value,
      NumberStyles.Float | NumberStyles.AllowThousands,
      CultureInfo.InvariantCulture,
      out var parsed
    ) && IsFinite(parsed)
      ? parsed
      : null;
  }

  private static bool IsFinite(double value) => !(double.IsNaN(value) || double.IsInfinity(value));
}

public static class ProviderDateTimeExtensions
{
  public static string ToIso8601ProviderString(this DateTime value)
    => value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
}

public sealed class JsonParseException : Exception, ILocalizedError
{
  public JsonParseException(string message) : base(message) { }
  public string? ErrorDescription => Message;
}
