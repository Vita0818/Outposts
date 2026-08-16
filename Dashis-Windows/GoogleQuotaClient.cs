using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dashis;

public readonly record struct GoogleConsumerManualContext(
  string Label = "Google AI subscription",
  DateTime? ObservedAt = null,
  double? Used = null,
  double? Limit = null,
  double? Remaining = null,
  string Unit = "%"
)
{
  public DateTime EffectiveObservedAt => ObservedAt ?? DateTime.UtcNow;
}

public sealed class GoogleConsumerUsageClient : IProviderUsageClient<GoogleConsumerManualContext>
{
  public ProviderID ProviderID => ProviderID.Google;

  public Task<ProviderSnapshot> FetchSnapshotAsync(GoogleConsumerManualContext context)
  {
    var reading = new ManualReading(context);
    var warnings = new List<ProviderWarning>();

    if (!reading.HasAnyValue)
    {
      warnings.Add(
        new ProviderWarning(
          "google-consumer-manual-required",
          "Manual check required. Google does not publish a consumer subscription quota API."
        )
      );
    }
    else if (reading.IsInconsistent)
    {
      warnings.Add(
        new ProviderWarning(
          "google-consumer-manual-inconsistent",
          "The manual values do not reconcile; the entered values are shown without correction."
        )
      );
    }

    var unit = string.IsNullOrWhiteSpace(context.Unit) ? "%" : context.Unit.Trim();
    var window = reading.HasAnyValue
      ? new[] {
        new QuotaWindow(
          Id: "google-consumer-manual",
          Label: context.Label,
          Used: reading.Used,
          Limit: reading.Limit,
          Remaining: reading.Remaining,
          UsedPercentage: reading.UsedPercentage,
          RemainingPercentage: reading.RemainingPercentage,
          ResetsAt: null,
          Unit: string.IsNullOrWhiteSpace(unit) ? "%" : unit,
          IsEstimated: false
        )
      }
      : Array.Empty<QuotaWindow>();

    return Task.FromResult(new ProviderSnapshot(
      providerID: ProviderID.Google,
      scope: ProviderScope.Manual(context.Label),
      sourceKind: UsageSourceKind.ManualOnly,
      observedAt: context.EffectiveObservedAt,
      windows: window,
      balance: null,
      metrics: Array.Empty<ProviderMetric>(),
      warnings: warnings,
      partialFailures: Array.Empty<ProviderFailure>()
    ));
  }
}

public readonly record struct GeminiAPIProjectContext(
  string ProjectID,
  GoogleSessionAccessToken AccessToken,
  IReadOnlySet<string>? SelectedQuotaIDs = null,
  DateTime? ObservationDate = null
);

public sealed class GeminiAPIProjectUsageClient : IProviderUsageClient<GeminiAPIProjectContext>
{
  public ProviderID ProviderID => ProviderID.Google;

  private readonly ProviderHTTPClient httpClient;

  public GeminiAPIProjectUsageClient(ProviderHTTPClient? httpClient = null)
  {
    this.httpClient = httpClient ?? new ProviderHTTPClient();
  }

  public async Task<ProviderSnapshot> FetchSnapshotAsync(GeminiAPIProjectContext context)
  {
    var observedAt = context.ObservationDate ?? DateTime.UtcNow;
    var projectID = context.ProjectID.Trim();
    if (!GoogleQuotaValidation.ValidProjectID(projectID))
    {
      return FailureSnapshot(
        projectID,
        observedAt,
        "google.project",
        new GoogleQuotaClientException(GoogleQuotaClientError.InvalidProject).Message
      );
    }

    if (!context.AccessToken.IsUsable(observedAt))
    {
      return FailureSnapshot(
        projectID,
        observedAt,
        "google.oauth",
        new GoogleQuotaClientException(GoogleQuotaClientError.ExpiredAccessToken).Message
      );
    }

    IReadOnlyList<GoogleQuotaInfo> quotaInfos;
    try
    {
      quotaInfos = await FetchQuotaInfosAsync(projectID, context.AccessToken.Value).ConfigureAwait(false);
    }
    catch (Exception error)
    {
      return FailureSnapshot(
        projectID,
        observedAt,
        "google.quotas",
        ProviderJson.SafeMessage(error)
      );
    }

    var warnings = new List<ProviderWarning>();
    var matchingSelection = quotaInfos
      .Where(quota => context.SelectedQuotaIDs is null || context.SelectedQuotaIDs.Contains(quota.QuotaID))
      .ToList();

    var orderedSelection = matchingSelection
      .OrderByDescending(quota => (quota.IsConcurrent == true || GoogleQuotaCadence.Create(quota.RefreshInterval) != null))
      .ThenBy(quota => quota.QuotaID)
      .ToList();

    var selected = orderedSelection.Take(24).ToList();
    if (orderedSelection.Count > selected.Count)
    {
      warnings.Add(
        new ProviderWarning(
          "google-quota-selection-bounded",
          "Dashis limited this check to 24 quota definitions. Enter exact quota IDs to narrow the selection."
        )
      );
    }

    if (context.SelectedQuotaIDs is not null)
    {
      var requested = new HashSet<string>(context.SelectedQuotaIDs);
      var found = new HashSet<string>(matchingSelection.Select(quota => quota.QuotaID));
      if (!requested.IsSubsetOf(found))
      {
        warnings.Add(
          new ProviderWarning(
            "google-quota-selection-missing",
            "One or more requested quota IDs were not returned for this project."
          )
        );
      }
    }

    var failures = new List<ProviderFailure>();
    var seriesByType = new Dictionary<string, IReadOnlyList<GoogleMonitoringSeries>>();
    var validQuotaInfos = new List<GoogleQuotaInfo>();
    var monitoringStarts = new Dictionary<string, DateTime>();

    foreach (var quota in selected)
    {
      var pair = GoogleQuotaMetricPair.Create(quota.Metric);
      if (pair is null)
      {
        warnings.Add(
          new ProviderWarning(
            $"google-metric-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}",
            "A quota metric could not be mapped to an official Gemini Monitoring metric."
          )
        );
        continue;
      }

      validQuotaInfos.Add(quota);

      var intervalStart = MonitoringIntervalStart(quota, observedAt);
      if (!intervalStart.HasValue) continue;

      if (!monitoringStarts.TryGetValue(pair.Value.Limit, out var currentLimitStart)
        || intervalStart < currentLimitStart)
      {
        monitoringStarts[pair.Value.Limit] = intervalStart.Value;
      }
      if (!monitoringStarts.TryGetValue(pair.Value.Usage, out var currentUsageStart)
        || intervalStart < currentUsageStart)
      {
        monitoringStarts[pair.Value.Usage] = intervalStart.Value;
      }
    }

    var metricTasks = monitoringStarts.Select(entry => FetchMonitoringSeriesSafeAsync(
      entry.Key,
      projectID,
      context.AccessToken.Value,
      entry.Value,
      observedAt
    )).ToList();

    var monitoringResults = await Task.WhenAll(metricTasks).ConfigureAwait(false);
    foreach (var result in monitoringResults)
    {
      if (result.Item1 is null) continue;
      var metricType = result.Item1;
      if (result.Item2.IsSuccess)
      {
        seriesByType[metricType!] = result.Item2.Value!;
      }
      else
      {
        seriesByType[metricType!] = Array.Empty<GoogleMonitoringSeries>();
        var suffix = metricType!.EndsWith("/limit", StringComparison.OrdinalIgnoreCase)
          ? "limit" : "usage";
        failures.Add(
          new ProviderFailure(
            $"google.monitoring.{suffix}.{GoogleQuotaValidation.OperationSlug(metricType)}",
            ProviderJson.SafeMessage(result.Item2.Error!)
          )
        );
      }
    }

    var derivedWindows = new List<QuotaWindow>();
    foreach (var quota in validQuotaInfos)
    {
      var pair = GoogleQuotaMetricPair.Create(quota.Metric);
      if (pair is null) continue;
      var result = GoogleQuotaDeriver.Derive(
        quota,
        seriesByType.GetValueOrDefault(pair.Value.Limit, Array.Empty<GoogleMonitoringSeries>()),
        seriesByType.GetValueOrDefault(pair.Value.Usage, Array.Empty<GoogleMonitoringSeries>()),
        observedAt
      );
      derivedWindows.AddRange(result.Windows);
      warnings.AddRange(result.Warnings);
    }

    warnings.Add(
      new ProviderWarning(
        "google-monitoring-delay",
        "Cloud Monitoring quota usage can be delayed by about 150 seconds."
      )
    );
    warnings.Add(
      new ProviderWarning(
        "google-preview-dynamic-capacity",
        "Some Gemini quota metrics are preview or dynamically allocated; Dashis leaves unmatched or conflicting capacity unavailable instead of guessing."
      )
    );
    if (selected.Count == 0)
    {
      warnings.Add(
        new ProviderWarning(
          "google-no-selected-quotas",
          "No Gemini API quota definitions matched this project and selection."
        )
      );
    }

    var hasQuotaValue = derivedWindows.Any(window =>
      new[] { window.Used, window.Limit, window.Remaining, window.UsedPercentage, window.RemainingPercentage }
        .Any(value => IsFinite(value)));

    var metrics = hasQuotaValue
      ? new List<ProviderMetric>
      {
        new(
          "google-quota-definitions",
          "Quota definitions",
          selected.Count,
          "quotas"
        ),
        new(
          "google-derived-windows",
          "Derived windows",
          derivedWindows.Count(window => window.Remaining is not null),
          "windows"
        )
      }
      : Array.Empty<ProviderMetric>().ToList();

    return new ProviderSnapshot(
      providerID: ProviderID.Google,
      scope: ProviderScope.Project(projectID),
      sourceKind: UsageSourceKind.OfficialDerived,
      observedAt: observedAt,
      windows: derivedWindows.OrderBy(GoogleQuotaDeriver.WindowOrder).ToList(),
      balance: null,
      metrics: metrics,
      warnings: DeduplicateWarnings(warnings),
      partialFailures: DeduplicateFailures(failures)
    );
  }

  public async Task<IReadOnlyList<GoogleQuotaInfo>> FetchQuotaInfosAsync(string projectID, string accessToken)
  {
    var pageToken = (string?)null;
    var seenTokens = new HashSet<string>();
    var result = new List<GoogleQuotaInfo>();

    for (var index = 0; index < 20; index += 1)
    {
      var builder = new UriBuilder
      {
        Scheme = "https",
        Host = "cloudquotas.googleapis.com",
        Path = $"/v1/projects/{projectID}/locations/global/services/generativelanguage.googleapis.com/quotaInfos",
      };
      var query = new List<(string, string)> { ("pageSize", "1000") };
      if (!string.IsNullOrWhiteSpace(pageToken))
      {
        query.Add(("pageToken", pageToken));
      }
      builder.Query = BuildQuery(query);

      using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
      request.Headers.Accept.ParseAdd("application/json");

      var json = await httpClient.JsonAsync(request, "google.quotas").ConfigureAwait(false);
      var page = GoogleQuotaPayloadDecoder.Decode(json);
      result.AddRange(page.QuotaInfos);

      if (string.IsNullOrWhiteSpace(page.NextPageToken))
      {
        return result;
      }
      if (page.NextPageToken.Length > 2_048 || !seenTokens.Add(page.NextPageToken))
      {
        throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidPagination);
      }
      pageToken = page.NextPageToken;
    }

    throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidPagination);
  }

  private async Task<(string?, Result<IReadOnlyList<GoogleMonitoringSeries>>)> FetchMonitoringSeriesSafeAsync(
    string metricType,
    string projectID,
    string accessToken,
    DateTime intervalStart,
    DateTime observedAt
  )
  {
    var response = await ProviderJson.CaptureProviderResultAsync(async () =>
    {
      return await FetchMonitoringSeriesAsync(
        metricType,
        projectID,
        accessToken,
        intervalStart,
        observedAt
      ).ConfigureAwait(false);
    }).ConfigureAwait(false);
    return (metricType, response);
  }

  private async Task<IReadOnlyList<GoogleMonitoringSeries>> FetchMonitoringSeriesAsync(
    string metricType,
    string projectID,
    string accessToken,
    DateTime intervalStart,
    DateTime observedAt
  )
  {
    var pageToken = (string?)null;
    var seenTokens = new HashSet<string>();
    var result = new List<GoogleMonitoringSeries>();
    for (var index = 0; index < 100; index += 1)
    {
      var builder = new UriBuilder
      {
        Scheme = "https",
        Host = "monitoring.googleapis.com",
        Path = $"/v3/projects/{projectID}/timeSeries"
      };

      var query = new List<(string, string)>
      {
        ("filter", $"metric.type = \"{metricType}\""),
        ("interval.startTime", intervalStart.ToIso8601ProviderString()),
        ("interval.endTime", observedAt.ToIso8601ProviderString()),
        ("view", "FULL"),
        ("pageSize", "1000")
      };
      if (!string.IsNullOrWhiteSpace(pageToken))
      {
        query.Add(("pageToken", pageToken));
      }
      builder.Query = BuildQuery(query);

      using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
      request.Headers.Accept.ParseAdd("application/json");
      var json = await httpClient.JsonAsync(request, metricType.EndsWith("/limit") ? "google.monitoring.limit" : "google.monitoring.usage")
        .ConfigureAwait(false);

      var page = GoogleMonitoringPayloadDecoder.Decode(json, metricType);
      result.AddRange(page.Series);

      if (string.IsNullOrWhiteSpace(page.NextPageToken))
      {
        return GoogleQuotaPageCoalescer.CoalesceMonitoringSeries(result);
      }

      if (page.NextPageToken.Length > 2_048 || !seenTokens.Add(page.NextPageToken))
      {
        throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidPagination);
      }
      pageToken = page.NextPageToken;
    }

    throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidPagination);
  }

  public static IReadOnlyList<GoogleMonitoringSeries> CoalescedMonitoringSeries(IReadOnlyList<GoogleMonitoringSeries> series) =>
    GoogleQuotaPageCoalescer.CoalesceMonitoringSeries(series);

  private static List<ProviderWarning> DeduplicateWarnings(IEnumerable<ProviderWarning> warnings)
    => warnings.GroupBy(item => item.Id).Select(group => group.First()).ToList();

  private static List<ProviderFailure> DeduplicateFailures(IEnumerable<ProviderFailure> failures)
    => failures.GroupBy(item => item.Operation).Select(group => group.First()).ToList();

  private static ProviderSnapshot FailureSnapshot(
    string projectID,
    DateTime observedAt,
    string operation,
    string message
  )
  {
    return new ProviderSnapshot(
      providerID: ProviderID.Google,
      scope: ProviderScope.Project(string.IsNullOrWhiteSpace(projectID) ? "Google Cloud project" : projectID),
      sourceKind: UsageSourceKind.OfficialDerived,
      observedAt: observedAt,
      windows: Array.Empty<QuotaWindow>(),
      balance: null,
      metrics: Array.Empty<ProviderMetric>(),
      warnings: Array.Empty<ProviderWarning>(),
      partialFailures: new[] { new ProviderFailure(operation: operation, message: message) }
    );
  }

  private static DateTime? MonitoringIntervalStart(GoogleQuotaInfo quota, DateTime observedAt)
  {
    if (quota.IsConcurrent == true)
    {
      return observedAt.AddMinutes(-10);
    }
    var cadence = GoogleQuotaCadence.Create(quota.RefreshInterval);
    if (cadence is null) return null;
    return cadence.Value switch
    {
      GoogleQuotaCadence.Minute or GoogleQuotaCadence.Concurrent => observedAt.AddMinutes(-10),
      GoogleQuotaCadence.Hour => observedAt.AddHours(-2),
      GoogleQuotaCadence.PacificDay => observedAt.AddHours(-27),
      _ => null
    };
  }

  private static string BuildQuery(IReadOnlyList<(string Name, string Value)> values)
    => string.Join("&", values.Select(item =>
      $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value)}"));

  private static bool IsFinite(double? value) => value is not null && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
}

public enum GoogleQuotaClientError
{
  InvalidProject,
  ExpiredAccessToken,
  InvalidRequest,
  InvalidQuotaResponse,
  InvalidMonitoringResponse,
  InvalidPagination
}

public sealed class GoogleQuotaClientException : Exception, ILocalizedError
{
  public GoogleQuotaClientError Code { get; }

  public GoogleQuotaClientException(GoogleQuotaClientError code) : base(Description(code))
  {
    Code = code;
  }

  public string? ErrorDescription => Message;

  private static string Description(GoogleQuotaClientError code) => code switch
  {
    GoogleQuotaClientError.InvalidProject => "Enter a valid Google Cloud project ID or project number.",
    GoogleQuotaClientError.ExpiredAccessToken => "The in-memory Google access token expired. Connect Google again.",
    GoogleQuotaClientError.InvalidRequest => "The Google quota request could not be created.",
    GoogleQuotaClientError.InvalidQuotaResponse => "Cloud Quotas returned an unsupported response.",
    GoogleQuotaClientError.InvalidMonitoringResponse => "Cloud Monitoring returned an unsupported response.",
    GoogleQuotaClientError.InvalidPagination => "Google returned invalid or excessive pagination.",
    _ => "The provider check failed."
  };
}

public sealed record GoogleQuotaPage(IReadOnlyList<GoogleQuotaInfo> QuotaInfos, string? NextPageToken);
public sealed record GoogleMonitoringPage(IReadOnlyList<GoogleMonitoringSeries> Series, string? NextPageToken);

public sealed record GoogleQuotaInfo(
  string QuotaID,
  string Metric,
  string DisplayName,
  string MetricUnit,
  string? RefreshInterval,
  bool? IsPrecise,
  IReadOnlyList<string> Dimensions,
  bool? IsConcurrent,
  IReadOnlyList<GoogleQuotaDimensionInfo> DimensionInfos
);

public sealed record GoogleQuotaDimensionInfo(
  IReadOnlyDictionary<string, string> Dimensions,
  double? EffectiveLimit,
  IReadOnlyList<string> ApplicableLocations
);

public static class GoogleQuotaPayloadDecoder
{
  public static GoogleQuotaPage Decode(JsonElement objectNode)
  {
    var root = ProviderJson.Dictionary(objectNode)
      ?? throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);

    var rawInfos = root.TryGetValue("quotaInfos", out var rawSeries) && root["quotaInfos"].ValueKind == JsonValueKind.Array
      ? root["quotaInfos"].EnumerateArray()
      : Array.Empty<JsonElement>();

    var infos = new List<GoogleQuotaInfo>();
    foreach (var raw in rawInfos)
    {
      if (raw.ValueKind != JsonValueKind.Object) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      var dictionary = ProviderJson.Dictionary(raw)!;

      if (!dictionary.TryGetValue("quotaId", out var quotaIdNode)
          || quotaIdNode.ValueKind != JsonValueKind.String
          || !GoogleQuotaValidation.ValidQuotaID(quotaIdNode.GetString() ?? ""))
      {
        throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      }
      var quotaID = quotaIdNode.GetString()!;
      if (!dictionary.TryGetValue("metric", out var metricNode)
          || metricNode.ValueKind != JsonValueKind.String
          || GoogleQuotaMetricPair.Create(metricNode.GetString()!) is null)
      {
        throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      }
      var metric = metricNode.GetString()!;

      var displayName = new[] { ProviderJson.String(dictionary.TryGetValue("quotaDisplayName", out var quotaDisplayName) ? quotaDisplayName : null), ProviderJson.String(dictionary.TryGetValue("metricDisplayName", out var metricDisplayName) ? metricDisplayName : null), quotaID }
        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))!;

      var safeDisplay = GoogleQuotaValidation.SafeDisplayText(displayName, 256);
      if (safeDisplay is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);

      string metricUnit = "units";
      if (dictionary.TryGetValue("metricUnit", out var rawUnit))
      {
        var unit = ProviderJson.String(rawUnit);
        var safeUnit = GoogleQuotaValidation.SafeDisplayText(unit ?? string.Empty, 128);
        if (safeUnit is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
        metricUnit = safeUnit;
      }

      string? refreshInterval = null;
      if (dictionary.TryGetValue("refreshInterval", out var rawRefresh))
      {
        var rawValue = ProviderJson.String(rawRefresh);
        var normalized = GoogleQuotaValidation.SafeDuration(rawValue ?? string.Empty);
        if (normalized is null)
        {
          throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
        }
        refreshInterval = normalized;
      }

      bool? isPrecise = null;
      if (dictionary.TryGetValue("isPrecise", out var rawPrecise))
      {
        isPrecise = ProviderJson.Bool(rawPrecise);
        if (!isPrecise.HasValue) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      }

      var rawDimensions = dictionary.TryGetValue("dimensions", out var dimensionNames)
        ? (dimensionNames.ValueKind == JsonValueKind.Array ? dimensionNames.EnumerateArray().ToList() : new List<JsonElement>())
        : new List<JsonElement>();
      var dimensions = new List<string>();
      foreach (var dimension in rawDimensions)
      {
        if (dimension.ValueKind != JsonValueKind.String) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
        var value = dimension.GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || !Regex.IsMatch(value, @"^[A-Za-z0-9_.-]+$"))
        {
          throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
        }
        dimensions.Add(value);
      }

      bool? isConcurrent = null;
      if (dictionary.TryGetValue("isConcurrent", out var rawConcurrent))
      {
        isConcurrent = ProviderJson.Bool(rawConcurrent);
        if (!isConcurrent.HasValue) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      }

      var rawDimensionInfos = dictionary.TryGetValue("dimensionsInfos", out var rawDimensionInfoObject)
        ? (rawDimensionInfoObject.ValueKind == JsonValueKind.Array ? rawDimensionInfoObject.EnumerateArray().ToList() : new List<JsonElement>())
        : new List<JsonElement>();

      var dimensionInfos = rawDimensionInfos.Select(rawDimensionInfo => DecodeDimensionInfo(rawDimensionInfo)).ToList();
      if (dimensionInfos.Count == 0)
      {
        dimensionInfos.Add(new GoogleQuotaDimensionInfo(
          new Dictionary<string, string>(),
          null,
          Array.Empty<string>()
        ));
      }

      infos.Add(
        new GoogleQuotaInfo(
          QuotaID: quotaID,
          Metric: metric,
          DisplayName: safeDisplay,
          MetricUnit: metricUnit,
          RefreshInterval: refreshInterval,
          IsPrecise: isPrecise,
          Dimensions: dimensions,
          IsConcurrent: isConcurrent,
          DimensionInfos: dimensionInfos
        )
      );
    }

    string? nextToken = null;
    if (root.TryGetValue("nextPageToken", out var nextTokenNode))
    {
      var raw = ProviderJson.String(nextTokenNode);
      if (string.IsNullOrWhiteSpace(raw))
      {
        nextToken = null;
      }
      else
      {
        nextToken = raw;
      }
    }

    return new GoogleQuotaPage(infos, nextToken);
  }

  private static GoogleQuotaDimensionInfo DecodeDimensionInfo(JsonElement raw)
  {
    var dictionary = ProviderJson.Dictionary(raw) ?? throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);

    var rawDimensions = dictionary.TryGetValue("dimensions", out var rawDimensionNode)
      ? ProviderJson.Dictionary(rawDimensionNode) ?? new Dictionary<string, JsonElement>()
      : new Dictionary<string, JsonElement>();
    var dimensions = new Dictionary<string, string>();
    foreach (var (key, rawValue) in rawDimensions)
    {
      if (!GoogleQuotaValidation.ValidLabelKey(key)) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      var value = ProviderJson.String(rawValue);
      var safeValue = GoogleQuotaValidation.SafeDisplayText(value ?? string.Empty, 128);
      if (safeValue is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      dimensions[key] = safeValue;
    }

    double? effectiveLimit = null;
    if (dictionary.TryGetValue("details", out var detailsNode)
        && detailsNode.ValueKind == JsonValueKind.Object
        && detailsNode.TryGetProperty("value", out var rawValue))
    {
      var parsed = ProviderJson.Number(rawValue);
      if (!parsed.HasValue || parsed < 0) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      effectiveLimit = parsed.Value;
    }
    else if (dictionary.TryGetValue("details", out var detailsNodeInvalid)
             && detailsNodeInvalid.ValueKind != JsonValueKind.Undefined
             && detailsNodeInvalid.ValueKind != JsonValueKind.Null
             && detailsNodeInvalid.ValueKind != JsonValueKind.Object)
    {
      throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
    }

    var rawLocations = dictionary.TryGetValue("applicableLocations", out var rawLocationsNode)
      ? (rawLocationsNode.ValueKind == JsonValueKind.Array ? rawLocationsNode.EnumerateArray().ToList() : new List<JsonElement>())
      : new List<JsonElement>();
    var locations = new List<string>();
    foreach (var location in rawLocations)
    {
      var value = ProviderJson.String(location);
      var safe = GoogleQuotaValidation.SafeLabelValue(value ?? string.Empty);
      if (safe is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidQuotaResponse);
      locations.Add(safe);
    }

    return new GoogleQuotaDimensionInfo(
      dimensions,
      effectiveLimit,
      locations
    );
  }
}

public static class GoogleMonitoringPayloadDecoder
{
  public static GoogleMonitoringPage Decode(JsonElement objectNode, string expectedMetricType)
  {
    var root = ProviderJson.Dictionary(objectNode)
      ?? throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);

    var rawSeries = root.TryGetValue("timeSeries", out var timeSeriesNode)
      && timeSeriesNode.ValueKind == JsonValueKind.Array
      ? timeSeriesNode.EnumerateArray()
      : Enumerable.Empty<JsonElement>();

    var series = new List<GoogleMonitoringSeries>();
    foreach (var raw in rawSeries)
    {
      if (raw.ValueKind != JsonValueKind.Object) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
      var dictionary = ProviderJson.Dictionary(raw)!;
      if (!dictionary.TryGetValue("metric", out var metricNode) || metricNode.ValueKind != JsonValueKind.Object) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
      var metricDictionary = ProviderJson.Dictionary(metricNode)!;

      if (!metricDictionary.TryGetValue("type", out var metricTypeNode) || metricTypeNode.ValueKind != JsonValueKind.String) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
      var metricType = metricTypeNode.GetString()!;
      if (!string.Equals(metricType, expectedMetricType, StringComparison.Ordinal)) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);

      var metricLabels = DecodeLabels(metricDictionary.TryGetValue("labels", out var labelsNode) ? labelsNode : null);
      JsonElement? resourceLabelsNode = null;
      if (dictionary.TryGetValue("resource", out var rawResource)
          && rawResource.ValueKind == JsonValueKind.Object
          && rawResource.TryGetProperty("labels", out var labels))
      {
        resourceLabelsNode = labels;
      }
      var resourceLabels = DecodeLabels(resourceLabelsNode);
      var kindValue = dictionary.TryGetValue("metricKind", out var metricKindNode) && metricKindNode.ValueKind == JsonValueKind.String
        ? metricKindNode.GetString() ?? string.Empty
        : string.Empty;
      var kind = Enum.TryParse<GoogleMetricKind>(kindValue, out var parsedKind)
        ? parsedKind
        : GoogleMetricKind.Unknown;

      var rawPoints = dictionary.TryGetValue("points", out var rawPointsNode)
        && rawPointsNode.ValueKind == JsonValueKind.Array
        ? rawPointsNode.EnumerateArray()
        : Enumerable.Empty<JsonElement>();
      var points = rawPoints.Select(DecodePoint).ToList();

      series.Add(new GoogleMonitoringSeries(
        MetricType: metricType,
        MetricKind: kind,
        MetricLabels: metricLabels,
        ResourceLabels: resourceLabels,
        Points: points
      ));
    }

    string? nextToken = null;
    if (root.TryGetValue("nextPageToken", out var nextTokenNode))
    {
      var value = ProviderJson.String(nextTokenNode);
      if (value is not null)
      {
        nextToken = value;
      }
    }
    return new GoogleMonitoringPage(series, nextToken);
  }

  private static IReadOnlyDictionary<string, string> DecodeLabels(JsonElement? raw)
  {
    if (raw is null) return new Dictionary<string, string>();
    if (raw.Value.ValueKind != JsonValueKind.Object) return new Dictionary<string, string>();
    var labels = new Dictionary<string, string>();
    var dictionary = ProviderJson.Dictionary(raw.Value);
    if (dictionary is null) return labels;
    foreach (var (key, value) in dictionary)
    {
      if (!GoogleQuotaValidation.ValidLabelKey(key)) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
      var safeValue = GoogleQuotaValidation.SafeLabelValue(ProviderJson.String(value) ?? string.Empty);
      if (safeValue is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
      labels[key] = safeValue;
    }
    return labels;
  }

  private static GoogleMonitoringPoint DecodePoint(JsonElement raw)
  {
    if (raw.ValueKind != JsonValueKind.Object) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    if (!raw.TryGetProperty("interval", out var intervalNode)
      || !raw.TryGetProperty("value", out var valueNode))
    {
      throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    }
    if (!intervalNode.TryGetProperty("endTime", out var endNode)) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    var endTime = ProviderJson.Date(ProviderJson.String(endNode));
    if (endTime is null) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);

    if (valueNode.ValueKind != JsonValueKind.Object) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    var keys = valueNode.EnumerateObject().Where(pair => pair.Name is "doubleValue" or "int64Value").ToList();
    if (keys.Count != 1) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    var value = ProviderJson.Number(keys[0].Value);
    if (!value.HasValue) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);

    DateTime? start = null;
    if (intervalNode.TryGetProperty("startTime", out var startNode))
    {
      start = ProviderJson.Date(ProviderJson.String(startNode));
      if (!start.HasValue) throw new GoogleQuotaClientException(GoogleQuotaClientError.InvalidMonitoringResponse);
    }

    return new GoogleMonitoringPoint(
      Start: start,
      End: endTime.Value,
      Value: value.Value
    );
  }
}

public enum GoogleMetricKind
{
  Delta,
  Gauge,
  Cumulative,
  Unknown
}

public sealed record GoogleMonitoringPoint(
  DateTime? Start,
  DateTime End,
  double Value
);

public sealed record GoogleMonitoringSeries(
  string MetricType,
  GoogleMetricKind MetricKind,
  IReadOnlyDictionary<string, string> MetricLabels,
  IReadOnlyDictionary<string, string> ResourceLabels,
  IReadOnlyList<GoogleMonitoringPoint> Points
)
{
  public string CoreSignature
  {
    get
    {
      var labels = new Dictionary<string, string>(MetricLabels);
      labels.Remove("method");
      var metricParts = labels.OrderBy(item => item.Key).Select(item => $"m:{item.Key}={item.Value}");
      var resourceParts = ResourceLabels.OrderBy(item => item.Key).Select(item => $"r:{item.Key}={item.Value}");
      return string.Join("|", metricParts.Concat(resourceParts));
    }
  }
}

public sealed record GoogleQuotaMetricPair(string Limit, string Usage)
{
  public const string ServicePrefix = "generativelanguage.googleapis.com/";

  public static GoogleQuotaMetricPair? Create(string? quotaMetric)
  {
    if (string.IsNullOrWhiteSpace(quotaMetric)) return null;
    if (!quotaMetric.StartsWith(ServicePrefix, StringComparison.Ordinal)) return null;
    var suffix = quotaMetric[ServicePrefix.Length..];
    if (suffix.StartsWith("quota/"))
    {
      suffix = suffix["quota/".Length..];
    }
    if (suffix.EndsWith("/limit"))
    {
      suffix = suffix[..^"/limit".Length];
    }
    else if (suffix.EndsWith("/usage"))
    {
      suffix = suffix[..^"/usage".Length];
    }

    if (!GoogleQuotaValidation.ValidMetricSuffix(suffix)) return null;
    return new GoogleQuotaMetricPair(
      $"{ServicePrefix}quota/{suffix}/limit",
      $"{ServicePrefix}quota/{suffix}/usage"
    );
  }
}

public sealed record GoogleQuotaDerivationResult(
  IReadOnlyList<QuotaWindow> Windows,
  IReadOnlyList<ProviderWarning> Warnings
);

public static class GoogleQuotaDeriver
{
  public static GoogleQuotaDerivationResult Derive(
    GoogleQuotaInfo quota,
    IReadOnlyList<GoogleMonitoringSeries> limitSeries,
    IReadOnlyList<GoogleMonitoringSeries> usageSeries,
    DateTime now
  )
  {
    var cadence = quota.IsConcurrent == true
      ? GoogleQuotaCadence.Concurrent
      : GoogleQuotaCadence.Create(quota.RefreshInterval);
    if (cadence is null)
    {
      var windows = quota.DimensionInfos.Select((dimension, index) =>
        UnavailableWindow(quota, dimension, index, dimension.EffectiveLimit, null)).ToList();
      return new GoogleQuotaDerivationResult(
        windows,
        new[] {
          new ProviderWarning(
            $"google-refresh-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}",
            $"{quota.DisplayName} has an unknown refresh interval; remaining is unavailable."
          )
        }
      );
    }

    var currentBounds = cadence.Value.Bounds(now);
    var warnings = new List<ProviderWarning>();
    if (quota.IsPrecise == false)
    {
      warnings.Add(
        new ProviderWarning(
          $"google-imprecise-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}",
          $"{quota.DisplayName} is marked imprecise by Cloud Quotas."
        )
      );
    }

    var exactLimitSeries = limitSeries.Where(series => BelongsToQuota(series, quota)).ToList();
    var exactUsageSeries = usageSeries.Where(series => BelongsToQuota(series, quota)).ToList();

    if ((limitSeries.Count > 0 || usageSeries.Count > 0)
      && exactLimitSeries.Count == 0
      && exactUsageSeries.Count == 0)
    {
      warnings.Add(
        new ProviderWarning(
          $"google-limit-name-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}",
          $"{quota.DisplayName} could not be matched to Monitoring limit_name; remaining is unavailable."
        )
      );
    }

    var windows = new List<QuotaWindow>();
    for (var dimensionIndex = 0; dimensionIndex < quota.DimensionInfos.Count; dimensionIndex++)
    {
      var dimension = quota.DimensionInfos.ElementAt(dimensionIndex);
      var matchingLimits = AssignedSeries(exactLimitSeries, quota, dimensionIndex).ToList();
      var matchingUsage = AssignedSeries(exactUsageSeries, quota, dimensionIndex).ToList();

      var limitGroups = matchingLimits.GroupBy(series => Signature(series, quota)).ToDictionary(group => group.Key, group => group.ToList());
      var usageGroups = matchingUsage.GroupBy(series => Signature(series, quota)).ToDictionary(group => group.Key, group => group.ToList());
      var signatures = new HashSet<string>(limitGroups.Keys.Concat(usageGroups.Keys));
      if (signatures.Count == 0) signatures.Add(string.Empty);

      foreach (var signature in signatures.OrderBy(value => value))
      {
        var limits = limitGroups.TryGetValue(signature, out var limitGroup) ? limitGroup : new List<GoogleMonitoringSeries>();
        var usageForSignature = usageGroups.TryGetValue(signature, out var usageGroup) ? usageGroup : new List<GoogleMonitoringSeries>();
        var boundsResult = DerivationBounds(cadence.Value, usageForSignature, now);
        var bounds = boundsResult.Bounds;

        var monitoredLimit = boundsResult.IsHistorical
          ? LatestLimit(limits, bounds)
          : LatestLimit(limits, currentBounds);
        double? limit = null;
        if (dimension.EffectiveLimit.HasValue && monitoredLimit.HasValue && !ApproximatelyEqual(dimension.EffectiveLimit.Value, monitoredLimit.Value))
        {
          warnings.Add(
            new ProviderWarning(
              $"google-limit-mismatch-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{dimensionIndex}-{signatures.ToList().IndexOf(signature)}",
              $"{quota.DisplayName} has conflicting Cloud Quotas and Monitoring limits; remaining is unavailable."
            )
          );
        }
        else if (boundsResult.IsHistorical)
        {
          limit = monitoredLimit;
        }
        else
        {
          limit = dimension.EffectiveLimit ?? monitoredLimit;
        }

        var usage = AggregateUsage(usageForSignature, bounds, quota.IsConcurrent == true);
        var remaining = limit.HasValue && usage.HasValue ? limit - usage : null;
        var usedPercentage = Percentage(usage, limit);
        var remainingPercentage = Percentage(remaining, limit);
        var labelSuffix = DisplayDimensions(dimension, signature, cadence.Value, boundsResult.IsHistorical ? bounds.End : (DateTime?)null);

        windows.Add(
          new QuotaWindow(
            Id: $"google-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{dimensionIndex}-{signatures.ToList().IndexOf(signature)}",
            Label: string.IsNullOrWhiteSpace(labelSuffix) ? quota.DisplayName : $"{quota.DisplayName} · {labelSuffix}",
            Used: usage,
            Limit: limit,
            Remaining: remaining,
            UsedPercentage: usedPercentage,
            RemainingPercentage: remainingPercentage,
            ResetsAt: cadence.Value.ResetDate(bounds),
            Unit: quota.MetricUnit,
            IsEstimated: true
          )
        );

        if (!usage.HasValue)
        {
          warnings.Add(
            new ProviderWarning(
              $"google-no-usage-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{dimensionIndex}-{signatures.ToList().IndexOf(signature)}",
              $"{quota.DisplayName} has no reliably matched usage series for this window."
            )
          );
        }
        if (boundsResult.IsHistorical)
        {
          warnings.Add(
            new ProviderWarning(
              $"google-historical-window-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{dimensionIndex}-{signatures.ToList().IndexOf(signature)}",
              $"{quota.DisplayName} uses the latest complete Monitoring window ending {bounds.End:O}; it is delayed historical usage, not a live current-window balance."
            )
          );
        }
        if (!limit.HasValue)
        {
          warnings.Add(
            new ProviderWarning(
              $"google-no-limit-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{dimensionIndex}-{signatures.ToList().IndexOf(signature)}",
              $"{quota.DisplayName} has no reliably matched active limit."
            )
          );
        }
      }
    }

    return new GoogleQuotaDerivationResult(windows, warnings);
  }

  public static int WindowOrder(QuotaWindow lhs, QuotaWindow rhs)
  {
    var left = lhs.RemainingPercentage ?? double.MaxValue;
    var right = rhs.RemainingPercentage ?? double.MaxValue;
    if (left == right) return lhs.ResetsAt.HasValue && rhs.ResetsAt.HasValue
      ? lhs.ResetsAt.Value.CompareTo(rhs.ResetsAt.Value)
      : 0;
    return left.CompareTo(right);
  }

  private static bool BelongsToQuota(GoogleMonitoringSeries series, GoogleQuotaInfo quota)
  {
    var limitName = series.MetricLabels.TryGetValue("limit_name", out var labelFromMetric)
      ? labelFromMetric
      : series.ResourceLabels.TryGetValue("limit_name", out var labelFromResource) ? labelFromResource : null;
    return limitName == quota.QuotaID;
  }

  private static IEnumerable<GoogleMonitoringSeries> AssignedSeries(
    IReadOnlyList<GoogleMonitoringSeries> source,
    GoogleQuotaInfo quota,
    int dimensionIndex
  )
  {
    foreach (var item in source)
    {
      for (var index = 0; index < quota.DimensionInfos.Count; index += 1)
      {
        if (Matches(item, quota.DimensionInfos[index]))
        {
          if (index == dimensionIndex)
          {
            yield return item;
          }
          break;
        }
      }
    }
  }

  private static string Signature(GoogleMonitoringSeries series, GoogleQuotaInfo quota)
  {
    var keys = new HashSet<string>(quota.Dimensions);
    foreach (var dimension in quota.DimensionInfos)
    {
      foreach (var key in dimension.Dimensions.Keys)
      {
        keys.Add(key);
      }
    }

    keys.Remove("method");
    keys.Add("limit_name");
    keys.Add("location");

    var pairs = keys
      .Select(key =>
      {
        var value = series.MetricLabels.TryGetValue(key, out var metricValue)
          ? metricValue
          : series.ResourceLabels.TryGetValue(key, out var resourceValue)
            ? resourceValue : null;
        return value is null ? null : $"{key}={value}";
      })
      .Where(value => value is not null);
    return string.Join("|", pairs);
  }

  private static bool Matches(GoogleMonitoringSeries series, GoogleQuotaDimensionInfo dimension)
  {
    foreach (var (key, expected) in dimension.Dimensions)
    {
      string? actual;
      if (key == "region" || key == "zone")
      {
        actual = series.MetricLabels.GetValueOrDefault(key, null)
          ?? series.ResourceLabels.GetValueOrDefault(key, null)
          ?? series.MetricLabels.GetValueOrDefault("location", null)
          ?? series.ResourceLabels.GetValueOrDefault("location", null);
      }
      else
      {
        actual = series.MetricLabels.GetValueOrDefault(key, null)
          ?? series.ResourceLabels.GetValueOrDefault(key, null);
      }
      if (!string.Equals(actual, expected, StringComparison.Ordinal))
      {
        return false;
      }
    }

    if (!dimension.ApplicableLocations.Any())
    {
      return true;
    }
    var location = series.MetricLabels.GetValueOrDefault("location", null)
      ?? series.ResourceLabels.GetValueOrDefault("location", null);
    return location is not null && dimension.ApplicableLocations.Contains(location);
  }

  private static double? LatestLimit(IReadOnlyList<GoogleMonitoringSeries> series, GoogleQuotaBounds bounds)
  {
    var values = series.SelectMany(item =>
      item.Points
        .Where(point => item.MetricKind == GoogleMetricKind.Gauge && point.End <= bounds.End && point.Value >= 0)
        .Select(point => (point.End, point.Value)))
      .ToList();
    if (values.Count == 0) return null;
    var latest = values.MaxBy(item => item.End);
    if (latest is null) return null;
    var contemporaneous = values.Where(item => Math.Abs((item.End - latest.Value.End).TotalSeconds) < 1).ToList();
    return contemporaneous.Select(item => item.Value).Distinct().Count() == 1 ? latest.Value.Value : null;
  }

  private static double? AggregateUsage(IReadOnlyList<GoogleMonitoringSeries> series, GoogleQuotaBounds bounds, bool concurrent)
  {
    if (series.Count == 0) return null;
    var values = new List<double>();
    foreach (var item in series)
    {
      if (concurrent && item.MetricKind != GoogleMetricKind.Gauge)
      {
        return null;
      }
      switch (item.MetricKind)
      {
        case GoogleMetricKind.Delta:
          var completePoints = item.Points
            .Where(point =>
              point.Start.HasValue
              && point.Start.Value >= bounds.Start
              && point.End <= bounds.End
              && point.End > point.Start.Value
              && point.Value >= 0)
            .ToList();
          if (completePoints.Count == 0) return null;
          values.Add(completePoints.Sum(point => point.Value));
          break;
        case GoogleMetricKind.Gauge:
          var gaugePoint = item.Points
            .Where(point => point.End >= bounds.Start && point.End <= bounds.End && point.Value >= 0)
            .MaxBy(point => point.End);
          if (gaugePoint is null) return null;
          values.Add(gaugePoint.Value.Value);
          break;
        case GoogleMetricKind.Cumulative:
        case GoogleMetricKind.Unknown:
        default:
          return null;
      }
    }
    return values.Sum();
  }

  private static bool ApproximatelyEqual(double lhs, double rhs)
  {
    return Math.Abs(lhs - rhs) <= Math.Max(0.000001, Math.Max(Math.Abs(lhs), Math.Abs(rhs)) * 0.000001);
  }

  private static double? Percentage(double? value, double? of)
  {
    if (!value.HasValue || !of.HasValue || of <= 0) return null;
    return value.Value / of.Value * 100.0;
  }

  private static QuotaWindow UnavailableWindow(
    GoogleQuotaInfo quota,
    GoogleQuotaDimensionInfo dimension,
    int index,
    double? limit,
    DateTime? resetsAt
  )
  {
    return new QuotaWindow(
      $"google-{GoogleQuotaValidation.OperationSlug(quota.QuotaID)}-{index}",
      quota.DisplayName,
      null,
      limit,
      null,
      null,
      null,
      resetsAt,
      quota.MetricUnit,
      true
    );
  }

  private static string DisplayDimensions(
    GoogleQuotaDimensionInfo dimension,
    string signature,
    GoogleQuotaCadence cadence,
    DateTime? historicalAsOf
  )
  {
    var values = dimension.Dimensions
      .OrderBy(item => item.Key)
      .Select(item => $"{item.Key}: {item.Value}")
      .ToList();
    if (!values.Any() && !string.IsNullOrWhiteSpace(signature))
    {
      var modelValue = signature.Split('|', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault(item => item.StartsWith("model=", StringComparison.Ordinal));
      if (modelValue is not null)
      {
        values.Add($"model: {modelValue.Substring("model=".Length)}");
      }
    }
    values.Add(cadence.Label);
    if (historicalAsOf.HasValue)
    {
      values.Add($"as of {historicalAsOf.Value:O}");
    }
    return string.Join(" · ", values);
  }

  private static (GoogleQuotaBounds Bounds, bool IsHistorical) DerivationBounds(
    GoogleQuotaCadence cadence,
    IReadOnlyList<GoogleMonitoringSeries> usageSeries,
    DateTime now
  )
  {
    var defaultBounds = cadence.Bounds(now);
    var (windowDurationSeconds, maximumAgeSeconds) = cadence switch
    {
      GoogleQuotaCadence.Minute => (60, 10 * 60),
      GoogleQuotaCadence.Hour => (3600, 2 * 60 * 60),
      GoogleQuotaCadence.Concurrent or GoogleQuotaCadence.PacificDay => (0, 0),
      _ => (0, 0)
    };

    if (windowDurationSeconds == 0) return (defaultBounds, false);
    if (usageSeries.Count == 0 || usageSeries.Any(item => item.MetricKind != GoogleMetricKind.Delta))
    {
      return (defaultBounds, false);
    }

    var latestEnds = new List<DateTime>();
    foreach (var series in usageSeries)
    {
      var latest = series.Points
        .Where(point => point.Start.HasValue
          && point.Start.Value < point.End
          && point.End <= now
          && point.Value >= 0)
        .Select(point => point.End)
        .DefaultIfEmpty()
        .Max();
      if (latest == default)
      {
        return (defaultBounds, false);
      }
      latestEnds.Add(latest);
    }

    if (latestEnds.Count != usageSeries.Count) return (defaultBounds, false);
    var asOf = latestEnds.Min();
    if ((now - asOf).TotalSeconds < 0 || (now - asOf).TotalSeconds > maximumAgeSeconds)
    {
      return (defaultBounds, false);
    }
    return (
      new GoogleQuotaBounds(
        Start: asOf.AddSeconds(-windowDurationSeconds),
        End: asOf,
        Reset: null
      ),
      true
    );
  }
}

public sealed record GoogleQuotaBounds(DateTime Start, DateTime End, DateTime? Reset);

public enum GoogleQuotaCadence
{
  Minute,
  Hour,
  PacificDay,
  Concurrent
}

public static class GoogleQuotaCadenceExtensions
{
  public static GoogleQuotaCadence? Create(string? refreshInterval)
  {
    if (string.IsNullOrWhiteSpace(refreshInterval))
    {
      return null;
    }
    var seconds = GoogleQuotaValidation.DurationSeconds(refreshInterval);
    return seconds switch
    {
      60 => GoogleQuotaCadence.Minute,
      3_600 => GoogleQuotaCadence.Hour,
      86_400 => GoogleQuotaCadence.PacificDay,
      _ => null
    };
  }

  public static GoogleQuotaBounds Bounds(this GoogleQuotaCadence cadence, DateTime date)
  {
    return cadence switch
    {
      GoogleQuotaCadence.Minute => new GoogleQuotaBounds(date.AddMinutes(-1), date, null),
      GoogleQuotaCadence.Hour => new GoogleQuotaBounds(date.AddHours(-1), date, null),
      GoogleQuotaCadence.PacificDay => BoundsForPacificDay(date),
      GoogleQuotaCadence.Concurrent => new GoogleQuotaBounds(date.AddMinutes(-5), date, null),
      _ => new GoogleQuotaBounds(date, date, null)
    };
  }

  public static string Label(this GoogleQuotaCadence cadence) => cadence switch
  {
    GoogleQuotaCadence.Minute => "per minute",
    GoogleQuotaCadence.Hour => "per hour",
    GoogleQuotaCadence.PacificDay => "per Pacific day",
    GoogleQuotaCadence.Concurrent => "concurrent",
    _ => "per minute"
  };

  public static DateTime? ResetDate(this GoogleQuotaCadence cadence, GoogleQuotaBounds bounds)
  {
    return cadence switch
    {
      GoogleQuotaCadence.PacificDay => bounds.Reset,
      _ => null
    };
  }

  private static GoogleQuotaBounds BoundsForPacificDay(DateTime date)
  {
    var pacificZone = GetPacificZone();
    var local = TimeZoneInfo.ConvertTime(date, pacificZone);
    var start = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Unspecified);
    var end = start.AddDays(1);
    var reset = end;
    return new GoogleQuotaBounds(
      TimeZoneInfo.ConvertTimeToUtc(start, pacificZone),
      date,
      TimeZoneInfo.ConvertTimeToUtc(reset, pacificZone)
    );
  }

  private static TimeZoneInfo GetPacificZone()
  {
    try
    {
      return TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
    }
    catch
    {
      return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    }
  }
}

public static class GoogleQuotaValidation
{
  public static bool ValidProjectID(string value)
  {
    return Regex.IsMatch(value, @"^[a-z][a-z0-9-]{4,28}[a-z0-9]$") || Regex.IsMatch(value, @"^[0-9]{6,32}$");
  }

  public static bool ValidQuotaID(string value)
  {
    return !string.IsNullOrWhiteSpace(value)
      && value.Length <= 256
      && Regex.IsMatch(value, @"^[A-Za-z0-9_./:\-]+$");
  }

  public static bool ValidMetricSuffix(string value)
  {
    return !string.IsNullOrWhiteSpace(value)
      && value.Length <= 512
      && !value.StartsWith("/", StringComparison.Ordinal)
      && !value.EndsWith("/", StringComparison.Ordinal)
      && !value.Contains("..")
      && Regex.IsMatch(value, @"^[A-Za-z0-9_./-]+$");
  }

  public static bool ValidLabelKey(string value)
  {
    return !string.IsNullOrWhiteSpace(value)
      && value.Length <= 128
      && Regex.IsMatch(value, @"^[A-Za-z0-9_.-]+$");
  }

  public static string? SafeLabelValue(string value)
  {
    return SafeDisplayText(value, 512);
  }

  public static string? SafeDisplayText(string value, int maximumLength)
  {
    var trimmed = value.Trim();
    if (string.IsNullOrEmpty(trimmed)
      || trimmed.Length > maximumLength
      || trimmed.Any(char.IsControl))
    {
      return null;
    }
    return trimmed;
  }

  public static string? SafeDuration(string value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var normalized = value.Trim().ToLowerInvariant();
    var seconds = DurationSeconds(normalized);
    return seconds is null ? null : normalized;
  }

  public static int? DurationSeconds(string value)
  {
    var normalized = value.Trim().ToLowerInvariant();
    return normalized switch
    {
      "minute" or "1 minute" => 60,
      "hour" or "1 hour" => 3600,
      "day" or "1 day" => 86400,
      _ => Regex.IsMatch(normalized, @"^[0-9]{1,9}s$")
        ? int.TryParse(normalized.TrimEnd('s'), out var seconds) ? seconds : null
        : null,
    };
  }

  public static string? SafeDateText(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);

  public static string OperationSlug(string value)
  {
    var normalized = string.Concat(value
      .ToLowerInvariant()
      .Select(character => Regex.IsMatch(character.ToString(), @"[A-Za-z0-9]") ? character : '-'));
    var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
    var slug = string.Join("-", parts);
    return string.IsNullOrEmpty(slug) ? "quota" : slug[..Math.Min(slug.Length, 80)];
  }
}

public static class GoogleQuotaPageCoalescer
{
  public static IReadOnlyList<GoogleMonitoringSeries> CoalesceMonitoringSeries(IReadOnlyList<GoogleMonitoringSeries> series)
  {
    var grouped = series.GroupBy(item => new GoogleMonitoringSeriesKey(
      item.MetricType,
      item.MetricKind,
      item.MetricLabels,
      item.ResourceLabels
    ));

    var result = new List<GoogleMonitoringSeries>();
    foreach (var group in grouped)
    {
      var points = group
        .SelectMany(item => item.Points)
        .Distinct()
        .OrderBy(point => point.End)
        .ThenBy(point => point.Start ?? point.End)
        .ToList();
      result.Add(new GoogleMonitoringSeries(
        group.Key.MetricType,
        group.Key.MetricKind,
        group.Key.MetricLabels,
        group.Key.ResourceLabels,
        points
      ));
    }
    return result;
  }
}

public sealed record GoogleMonitoringSeriesKey(
  string MetricType,
  GoogleMetricKind MetricKind,
  IReadOnlyDictionary<string, string> MetricLabels,
  IReadOnlyDictionary<string, string> ResourceLabels
);

public readonly record struct ManualReading(double? Used, double? Limit, double? Remaining)
{
  public ManualReading(GoogleConsumerManualContext context) : this(context.Used, context.Limit, context.Remaining)
  {
    var normalizedUsed = IsFinite(context.Used) ? context.Used : null;
    var normalizedLimit = IsFinite(context.Limit) ? context.Limit : null;
    var normalizedRemaining = IsFinite(context.Remaining) ? context.Remaining : null;

    double? nextLimit = normalizedLimit;
    double? nextUsed = normalizedUsed;
    double? nextRemaining = normalizedRemaining;

    if (nextLimit is null && nextUsed.HasValue && nextRemaining.HasValue)
    {
      var candidate = nextUsed.Value + nextRemaining.Value;
      nextLimit = IsFinite(candidate) ? candidate : null;
    }
    if (nextUsed is null && nextLimit.HasValue && nextRemaining.HasValue)
    {
      var candidate = nextLimit.Value - nextRemaining.Value;
      nextUsed = IsFinite(candidate) ? candidate : null;
    }
    if (nextRemaining is null && nextLimit.HasValue && nextUsed.HasValue)
    {
      var candidate = nextLimit.Value - nextUsed.Value;
      nextRemaining = IsFinite(candidate) ? candidate : null;
    }

    Used = nextUsed;
    Limit = nextLimit;
    Remaining = nextRemaining;
  }

  public bool HasAnyValue => Used.HasValue || Limit.HasValue || Remaining.HasValue;

  public double? UsedPercentage => Used is not null && Limit is > 0
    ? IsFinite(Used.Value / Limit.Value * 100.0) ? Used.Value / Limit.Value * 100.0 : null
    : null;

  public double? RemainingPercentage => Remaining is not null && Limit is > 0
    ? IsFinite(Remaining.Value / Limit.Value * 100.0) ? Remaining.Value / Limit.Value * 100.0 : null
    : null;

  public bool IsInconsistent
  {
    get
    {
      if (Used is null || Limit is null || Remaining is null) return false;
      var reconstructedLimit = Used + Remaining;
      if (!IsFinite(reconstructedLimit)) return true;
      var tolerance = Math.Max(0.000001, Math.Abs(Limit.Value) * 0.000001);
      return Math.Abs(reconstructedLimit - Limit.Value) > tolerance;
    }
  }

  private static bool IsFinite(double? value) => value is not null && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
}

public static class GoogleQuotaClientExtensions
{
  public static string ToProviderStandardString(this DateTime dateTime)
    => dateTime.ToIso8601ProviderString();
  public static string ToProviderStandardString(this DateTime? dateTime)
    => dateTime?.ToIso8601ProviderString() ?? DateTime.UtcNow.ToIso8601ProviderString();
}
