using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dashis;

public enum ProviderHTTPErrorCode
{
  BlockedEndpoint,
  Transport,
  InvalidResponse,
  HttpStatus,
  EmptyResponse,
  InvalidJson,
  ResponseTooLarge
}

public sealed class ProviderHTTPError : Exception, ILocalizedError
{
  public ProviderHTTPErrorCode Code { get; }
  public int? StatusCode { get; }

  public ProviderHTTPError(ProviderHTTPErrorCode code, string? message = null, int? statusCode = null)
    : base(message ?? ErrorText(code, statusCode))
  {
    Code = code;
    StatusCode = statusCode;
  }

  public string? ErrorDescription => Message;

  private static string ErrorText(ProviderHTTPErrorCode code, int? statusCode)
  {
    return code switch
    {
      ProviderHTTPErrorCode.BlockedEndpoint => "Endpoint policy rejected the request.",
      ProviderHTTPErrorCode.Transport => "The provider could not be reached.",
      ProviderHTTPErrorCode.InvalidResponse => "The provider returned an invalid response.",
      ProviderHTTPErrorCode.HttpStatus => statusCode == 429
        ? "The provider rate-limited this check."
        : $"The provider returned HTTP {statusCode}.",
      ProviderHTTPErrorCode.EmptyResponse => "The provider returned an empty response.",
      ProviderHTTPErrorCode.InvalidJson => "The provider returned an unsupported response.",
      ProviderHTTPErrorCode.ResponseTooLarge => "The provider response exceeded the safety limit.",
      _ => "The provider check failed."
    };
  }
}

public sealed class ProviderHTTPClient
{
  private readonly HttpClient client;
  private readonly int maximumRetries;
  private readonly int maximumResponseBytes;

  public ProviderHTTPClient(int maximumRetries = 1, int maximumResponseBytes = 8 * 1024 * 1024)
  {
    this.maximumRetries = Math.Max(0, maximumRetries);
    this.maximumResponseBytes = Math.Max(1_024, maximumResponseBytes);

    var handler = new HttpClientHandler
    {
      AllowAutoRedirect = false,
      UseCookies = false,
    };
    client = new HttpClient(handler, disposeHandler: false)
    {
      Timeout = TimeSpan.FromSeconds(30),
    };
  }

  public async Task<JsonElement> JsonAsync(HttpRequestMessage request, string operation)
  {
    var data = await DataAsync(request, operation).ConfigureAwait(false);
    try
    {
      using var document = JsonDocument.Parse(data);
      return document.RootElement.Clone();
    }
    catch
    {
      throw new ProviderHTTPError(ProviderHTTPErrorCode.InvalidJson);
    }
  }

  public async Task<byte[]> DataAsync(HttpRequestMessage request, string operation)
  {
    if (!ProviderEndpointPolicy.Allows(request))
    {
      throw new ProviderHTTPError(ProviderHTTPErrorCode.BlockedEndpoint);
    }

    var isIdempotent = string.Equals(
      request.Method.Method,
      HttpMethod.Get.Method,
      StringComparison.OrdinalIgnoreCase
    ) || string.Equals(request.Method.Method, HttpMethod.Head.Method, StringComparison.OrdinalIgnoreCase);

    var attempt = 0;

    while (true)
    {
      try
      {
        using var clonedRequest = await CloneRequestAsync(request).ConfigureAwait(false);
        clonedRequest.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        clonedRequest.Headers.Remove("X-Dashis-Request-ID");
        clonedRequest.Headers.Add("X-Dashis-Request-ID", Guid.NewGuid().ToString("N"));
        if (!clonedRequest.Headers.Contains("Cache-Control"))
        {
          clonedRequest.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };
        }

        using var response = await client.SendAsync(clonedRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        var statusCode = (int)response.StatusCode;

        if (statusCode < 200 || statusCode >= 300)
        {
          if (isIdempotent
              && attempt < maximumRetries
              && IsRetryableStatus(statusCode))
          {
            attempt += 1;
            await Task.Delay(Math.Min(1_000, 200 * attempt)).ConfigureAwait(false);
            continue;
          }

          throw new ProviderHTTPError(
            ProviderHTTPErrorCode.HttpStatus,
            statusCode: statusCode,
            message: $"HTTP {statusCode}"
          );
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength > maximumResponseBytes)
        {
          throw new ProviderHTTPError(ProviderHTTPErrorCode.ResponseTooLarge);
        }

        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var ms = new MemoryStream();
        if (contentLength > 0)
        {
          ms.Capacity = (int)Math.Min(contentLength.Value, maximumResponseBytes);
        }

        var buffer = new byte[8_192];
        while (true)
        {
          var read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
          if (read == 0) break;
          if (ms.Length + read > maximumResponseBytes)
          {
            throw new ProviderHTTPError(ProviderHTTPErrorCode.ResponseTooLarge);
          }
          ms.Write(buffer, 0, read);
        }

        if (ms.Length == 0)
        {
          throw new ProviderHTTPError(ProviderHTTPErrorCode.EmptyResponse);
        }
        return ms.ToArray();
      }
      catch (ProviderHTTPError error) when (error.Code == ProviderHTTPErrorCode.EmptyResponse)
      {
        throw error;
      }
      catch (Exception error) when (isIdempotent && attempt < maximumRetries && IsRetryableTransportError(error))
      {
        attempt += 1;
        await Task.Delay(Math.Min(1_000, 200 * attempt)).ConfigureAwait(false);
      }
      catch (Exception error) when (error is HttpRequestException or IOException)
      {
        throw new ProviderHTTPError(ProviderHTTPErrorCode.Transport);
      }
      catch (Exception error)
      {
        throw new ProviderHTTPError(ProviderHTTPErrorCode.Transport, error.Message);
      }
    }
  }

  private static bool IsRetryableStatus(int statusCode)
  {
    return statusCode == 429 || statusCode == 502 || statusCode == 503 || statusCode == 504;
  }

  private static bool IsRetryableTransportError(Exception error)
  {
    return error is HttpRequestException || error is IOException;
  }

  private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
  {
    var clone = new HttpRequestMessage(request.Method, request.RequestUri)
    {
      Version = request.Version,
    };

    foreach (var header in request.Headers)
    {
      clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    if (request.Content is not null)
    {
      var raw = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
      clone.Content = new ByteArrayContent(raw);
      foreach (var header in request.Content.Headers)
      {
        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }

    return clone;
  }
}

public static class ProviderEndpointPolicy
{
  private static readonly HashSet<string> ValidChars = new HashSet<string>();
  private static readonly TimeSpan GoogleTokenBackoff = TimeSpan.FromMilliseconds(200);

  public static bool Allows(HttpRequestMessage request)
  {
    var url = request.RequestUri;
    if (url is null) return false;
    var contentType = request.Content?.Headers.ContentType?.MediaType;
    if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    if (url.Port != 443 && !url.IsDefaultPort) return false;
    if (!Clean(url)) return false;
    if (string.IsNullOrWhiteSpace(url.Host)) return false;
    if (!string.IsNullOrEmpty(url.UserInfo)) return false;
    if (!string.IsNullOrEmpty(url.Fragment)) return false;

    var host = url.Host.ToLowerInvariant();
    var method = request.Method.Method.ToUpperInvariant();
    var bodyBytes = request.Content is null ? null : request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

    return host switch
    {
      "oauth2.googleapis.com" => AllowsGoogleToken(url, method, bodyBytes, contentType),
      "cloudquotas.googleapis.com" => AllowsGoogleQuotas(url, method, bodyBytes),
      "monitoring.googleapis.com" => AllowsGoogleMonitoring(url, method, bodyBytes),
      _ => false,
    };
  }

  public static string? SanitizeIdentifier(string value, int maximumLength = 128)
  {
    var trimmed = value.Trim();
    if (string.IsNullOrEmpty(trimmed)
      || trimmed == "."
      || trimmed == ".."
      || trimmed.Length > maximumLength
      || !Regex.IsMatch(trimmed, @"^[A-Za-z0-9_.:\-]+$"))
    {
      return null;
    }
    return trimmed;
  }

  private static bool AllowsGoogleToken(Uri url, string method, byte[]? body, string? contentType)
  {
    if (!method.Equals("POST", StringComparison.OrdinalIgnoreCase)) return false;
    if (!PathMatches(url, "/token")) return false;
    if (!string.IsNullOrEmpty(url.Query)) return false;
    if (!string.Equals(contentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return ValidGoogleTokenBody(body);
  }

  private static bool AllowsGoogleQuotas(Uri url, string method, byte[]? body)
  {
    if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase) || body is not null) return false;
    var segments = url.Segments.Select(s => s.Trim('/')).Where(s => s.Length > 0).ToArray();
    if (segments.Length != 8) return false;
    if (segments[0] != "v1"
      || segments[1] != "projects"
      || SanitizeIdentifier(segments[2]) != segments[2]
      || segments[3] != "locations"
      || segments[4] != "global"
      || segments[5] != "services"
      || segments[6] != "generativelanguage.googleapis.com"
      || segments[7] != "quotaInfos")
    {
      return false;
    }

    return ValidateQuery(
      url,
      new HashSet<string> { "pageSize", "pageToken" },
      null,
      ValidateGoogleQuery
    );
  }

  private static bool AllowsGoogleMonitoring(Uri url, string method, byte[]? body)
  {
    if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase) || body is not null) return false;
    var segments = url.Segments.Select(s => s.Trim('/')).Where(s => s.Length > 0).ToArray();
    if (segments.Length != 4) return false;
    if (segments[0] != "v3"
      || segments[1] != "projects"
      || SanitizeIdentifier(segments[2]) != segments[2]
      || segments[3] != "timeSeries")
    {
      return false;
    }

    return ValidateQuery(
      url,
      new HashSet<string> { "filter", "interval.startTime", "interval.endTime", "view", "pageSize", "pageToken" },
      new HashSet<string> { "filter", "interval.startTime", "interval.endTime", "view" },
      ValidateGoogleQuery
    );
  }

  private static bool ValidateQuery(
    Uri url,
    HashSet<string> allowedNames,
    HashSet<string>? requiredNames,
    Func<string, string, bool> validate
  )
  {
    var queryItems = ParseQueryItems(url.Query);
    var names = queryItems.Select(item => item.Key).ToArray();
    if (names.Length != names.Distinct().Count()) return false;
    var nameSet = names.ToHashSet();
    if (!nameSet.IsSubsetOf(allowedNames)) return false;
    if (requiredNames is not null && !requiredNames.IsSubsetOf(nameSet)) return false;
    return queryItems.All(item => validate(item.Key, item.Value));
  }

  private static bool ValidateGoogleQuery(string name, string value)
  {
    if (string.IsNullOrWhiteSpace(value)) return false;

    return name switch
    {
      "pageSize" => int.TryParse(value, out var pageSize) && pageSize is >= 1 and <= 10000,
      "pageToken" => value.Length <= 2048,
      "filter" => value.Length <= 1024
        && Regex.IsMatch(
          value,
          "^metric\\.type = \"generativelanguage\\.googleapis\\.com/quota/[A-Za-z0-9_./-]+/(limit|usage)\"$"
        ),
      "interval.startTime" or "interval.endTime" => ProviderJson.Date(value).HasValue,
      "view" => value == "FULL",
      _ => true,
    };
  }

  private static bool ValidGoogleTokenBody(byte[]? rawBody)
  {
    if (rawBody is null || rawBody.Length > 16_384) return false;
    var body = Encoding.UTF8.GetString(rawBody);
    var parameters = ParseQueryItems("?" + body);
    if (parameters.Count != 5 || parameters.Any(item => string.IsNullOrEmpty(item.Key))) return false;

    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var (key, value) in parameters)
    {
      if (!map.TryAdd(key, value))
      {
        return false;
      }
    }

    var required = new[] { "client_id", "code", "code_verifier", "grant_type", "redirect_uri" };
    if (!required.All(key => map.TryGetValue(key, out _))) return false;
    if (map["grant_type"] != "authorization_code") return false;
    if (!IsClientId(map["client_id"])) return false;
    if (string.IsNullOrEmpty(map["code"])
      || map["code"].Length > 4_096
      || map["code"].Any(char.IsControl))
      return false;
    if (!Regex.IsMatch(map["code_verifier"], @"^[A-Za-z0-9._~-]{43,128}$")) return false;
    if (!Uri.TryCreate(map["redirect_uri"], UriKind.Absolute, out var redirect)) return false;
    if (!redirect.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)) return false;
    if (!string.Equals(redirect.Host, "127.0.0.1", StringComparison.Ordinal)) return false;
    if (redirect.Port <= 0) return false;
    if (!redirect.Path.StartsWith("/dashis/google/oauth/")) return false;
    if (!string.IsNullOrEmpty(redirect.Query) || !string.IsNullOrEmpty(redirect.Fragment)) return false;
    if (!string.IsNullOrEmpty(redirect.UserInfo)) return false;
    return true;
  }

  private static bool Clean(Uri uri)
  {
    var rawPath = uri.AbsolutePath;
    if (rawPath.EndsWith("/", StringComparison.Ordinal)) return false;
    if (rawPath.Contains("//")) return false;
    if (rawPath.Contains('%')) return false;
    var segments = rawPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Any(segment => segment == "." || segment == "..")) return false;
    return true;
  }

  private static bool PathMatches(Uri url, string path)
    => string.Equals(url.AbsolutePath, path, StringComparison.Ordinal);

  private static bool IsClientId(string value)
  {
    return value.Length <= 512 && Regex.IsMatch(value, @"^[A-Za-z0-9._-]+\.apps\.googleusercontent\.com$");
  }

  private static IReadOnlyList<(string Key, string Value)> ParseQueryItems(string query)
  {
    if (string.IsNullOrWhiteSpace(query)) return Array.Empty<(string, string)>();
    var normalized = query;
    if (normalized.StartsWith("?")) normalized = normalized[1..];
    if (string.IsNullOrWhiteSpace(normalized)) return Array.Empty<(string, string)>();

    var items = new List<(string, string)>();
    foreach (var pair in normalized.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
      var parts = pair.Split('=', 2, StringSplitOptions.None);
      if (parts.Length != 2) return Array.Empty<(string, string)>();

      var key = DecodeFormComponent(parts[0]);
      var value = DecodeFormComponent(parts[1]);
      if (key is null || value is null) return Array.Empty<(string, string)>();
      items.Add((key, value));
    }
    return items;
  }

  private static string? DecodeFormComponent(string raw)
  {
    try
    {
      return Uri.UnescapeDataString(raw.Replace("+", " "));
    }
    catch
    {
      return null;
    }
  }
}
