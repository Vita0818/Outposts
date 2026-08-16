using System;
using System.Buffers.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Dashis;

public sealed record GoogleSessionAccessToken(
  string Value,
  DateTime ExpiresAt,
  string? GrantedScope = null
)
{
  public bool IsUsable(DateTime? at = null, TimeSpan? leeway = null)
  {
    var now = (at ?? DateTime.UtcNow).ToUniversalTime();
    var delta = ExpiresAt.ToUniversalTime() - now;
    return !string.IsNullOrWhiteSpace(Value)
      && Value.Length <= 8_192
      && !Value.Any(char.IsWhiteSpace)
      && !Value.Any(char.IsControl)
      && delta > (leeway ?? TimeSpan.FromSeconds(30));
  }
}

public enum GoogleDesktopOAuthError
{
  InvalidClientID,
  InvalidLoopbackRedirect,
  RandomGenerationFailed,
  InvalidAuthorizationURL,
  InvalidCallback,
  StateMismatch,
  AuthorizationDenied,
  MissingAuthorizationCode,
  InvalidTokenResponse
}

public sealed class GoogleDesktopOAuthException : Exception, ILocalizedError
{
  public GoogleDesktopOAuthError Code { get; }

  public GoogleDesktopOAuthException(GoogleDesktopOAuthError code)
    : base(Description(code))
  {
    Code = code;
  }

  public string? ErrorDescription => Message;

  private static string Description(GoogleDesktopOAuthError code) => code switch
  {
    GoogleDesktopOAuthError.InvalidClientID => "Enter a Google Desktop OAuth client ID.",
    GoogleDesktopOAuthError.InvalidLoopbackRedirect => "The Google OAuth loopback callback is invalid.",
    GoogleDesktopOAuthError.RandomGenerationFailed => "A secure OAuth verifier could not be generated.",
    GoogleDesktopOAuthError.InvalidAuthorizationURL => "The Google authorization URL could not be created.",
    GoogleDesktopOAuthError.InvalidCallback => "Google returned an invalid OAuth callback.",
    GoogleDesktopOAuthError.StateMismatch => "The Google OAuth state did not match.",
    GoogleDesktopOAuthError.AuthorizationDenied => "Google authorization was cancelled or denied.",
    GoogleDesktopOAuthError.MissingAuthorizationCode => "Google did not return an authorization code.",
    GoogleDesktopOAuthError.InvalidTokenResponse => "Google returned an unsupported token response.",
    _ => "The Google OAuth flow failed."
  };
}

public readonly record struct GoogleDesktopOAuthFlow(
  Uri AuthorizationURL,
  Uri RedirectURI,
  string State,
  string CodeVerifier
);

public static class GoogleDesktopOAuth
{
  public const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

  public static Uri MakeLoopbackRedirectURI(int port)
  {
    if (port <= 0) throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidLoopbackRedirect);
    var nonce = RandomUrlSafeString(24);
    return new Uri($"http://127.0.0.1:{port}/dashis/google/oauth/{nonce}");
  }

  public static GoogleDesktopOAuthFlow MakeAuthorizationFlow(string clientID, Uri redirectURI)
  {
    var sanitizedClientID = clientID.Trim();
    if (!IsValidClientID(sanitizedClientID))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidClientID);
    }
    if (!IsValidLoopbackRedirect(redirectURI))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidLoopbackRedirect);
    }

    var codeVerifier = RandomUrlSafeString(32);
    var state = RandomUrlSafeString(32);
    var challenge = ComputeSha256Base64Url(codeVerifier);
    var auth = new UriBuilder("https", "accounts.google.com", -1, "/o/oauth2/v2/auth")
    {
      Query = BuildQuery(
        ("client_id", sanitizedClientID),
        ("redirect_uri", redirectURI.AbsoluteUri),
        ("response_type", "code"),
        ("scope", CloudPlatformScope),
        ("code_challenge", challenge),
        ("code_challenge_method", "S256"),
        ("state", state)
      )
    }.Uri;

    return new GoogleDesktopOAuthFlow(auth, redirectURI, state, codeVerifier);
  }

  public static string AuthorizationCode(Uri callbackURL, GoogleDesktopOAuthFlow flow)
  {
    if (!SameLoopbackTarget(callbackURL, flow.RedirectURI))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }

    if (callbackURL.UserEscapedUri.Contains('#')) throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    if (!string.IsNullOrWhiteSpace(callbackURL.UserInfo))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }
    if (callbackURL.Query is null || !string.IsNullOrWhiteSpace(callbackURL.Fragment))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }
    if (!TryQueryParameters(callbackURL.Query, out var parameters))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }
    if (!parameters.TryGetValue("state", out var state) || state != flow.State)
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.StateMismatch);
    }
    if (parameters.ContainsKey("error"))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.AuthorizationDenied);
    }
    if (!parameters.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.MissingAuthorizationCode);
    }
    return code;
  }

  public static HttpRequestMessage TokenExchangeRequest(
    string authorizationCode,
    string clientID,
    GoogleDesktopOAuthFlow flow
  )
  {
    var sanitizedClientID = clientID.Trim();
    if (!IsValidClientID(sanitizedClientID))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidClientID);
    }
    if (string.IsNullOrWhiteSpace(authorizationCode) || authorizationCode.Length > 4_096)
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }
    if (!IsValidLoopbackRedirect(flow.RedirectURI))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidLoopbackRedirect);
    }
    if (flow.CodeVerifier.Length is < 43 or > 128)
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidCallback);
    }

    var body = BuildQuery(
      ("client_id", sanitizedClientID),
      ("code", authorizationCode),
      ("code_verifier", flow.CodeVerifier),
      ("grant_type", "authorization_code"),
      ("redirect_uri", flow.RedirectURI.AbsoluteUri)
    );

    var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
    {
      Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
    };
    request.Headers.Accept.ParseAdd("application/json");
    return request;
  }

  public static GoogleSessionAccessToken SessionAccessTokenFromData(byte[] data, DateTime? now = null)
  {
    using var document = JsonDocument.Parse(data);
    var root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("access_token", out var tokenNode))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }

    var token = tokenNode.GetString();
    if (string.IsNullOrWhiteSpace(token) || !IsValidAccessToken(token) || token.Length > 8_192)
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }

    if (!root.TryGetProperty("expires_in", out var expiresNode))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }
    var expiresIn = (int?)expiresNode.Deserialize<long>() ?? 0;
    if (expiresIn <= 0 || expiresIn > 7 * 24 * 60 * 60)
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }

    if (!root.TryGetProperty("token_type", out var tokenTypeNode))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }
    var tokenType = tokenTypeNode.GetString();
    if (!"Bearer".Equals(tokenType, StringComparison.OrdinalIgnoreCase))
    {
      throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
    }

    string? scope = null;
    if (root.TryGetProperty("scope", out var scopeNode))
    {
      scope = scopeNode.GetString();
      if (scope is null) throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
      var granted = scope.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
      if (!(granted.Count == 1 && granted.Contains(CloudPlatformScope)))
      {
        throw new GoogleDesktopOAuthException(GoogleDesktopOAuthError.InvalidTokenResponse);
      }
    }

    return new GoogleSessionAccessToken(
      token,
      (now ?? DateTime.UtcNow).AddSeconds(expiresIn),
      scope
    );
  }

  private static bool IsValidClientID(string value)
    => value.Length <= 512 && Regex.IsMatch(value, @"^[A-Za-z0-9._-]+\.apps\.googleusercontent\.com$");

  private static bool IsValidLoopbackRedirect(Uri url)
  {
    if (!url.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)) return false;
    if (!url.Host.Equals("127.0.0.1", StringComparison.Ordinal)) return false;
    if (url.Port <= 0) return false;
    if (!string.IsNullOrWhiteSpace(url.UserInfo)) return false;
    if (!string.IsNullOrWhiteSpace(url.Query) || !string.IsNullOrWhiteSpace(url.Fragment)) return false;
    if (url.Path == null) return false;
    if (!url.AbsolutePath.StartsWith("/dashis/google/oauth/", StringComparison.Ordinal)) return false;
    var parts = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 4;
  }

  private static bool SameLoopbackTarget(Uri lhs, Uri rhs)
    => lhs.Scheme.Equals(rhs.Scheme, StringComparison.OrdinalIgnoreCase)
      && lhs.Host.Equals(rhs.Host, StringComparison.OrdinalIgnoreCase)
      && lhs.Port == rhs.Port
      && lhs.AbsolutePath.Equals(rhs.AbsolutePath, StringComparison.Ordinal);

  private static bool IsValidAccessToken(string value)
    => !string.IsNullOrWhiteSpace(value)
      && !value.Any(char.IsWhiteSpace)
      && !value.Any(char.IsControl)
      && value.Length <= 8_192;

  private static string ComputeSha256Base64Url(string value)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Base64UrlEncode(bytes);
  }

  private static string Base64UrlEncode(byte[] bytes)
    => Convert.ToBase64String(bytes)
      .Replace('+', '-')
      .Replace('/', '_')
      .TrimEnd('=');

  private static string RandomUrlSafeString(int byteCount)
  {
    var bytes = new byte[byteCount];
    RandomNumberGenerator.Fill(bytes);
    return Base64UrlEncode(bytes);
  }

  private static string BuildQuery(params (string Name, string Value)[] parts)
  {
    return string.Join("&", parts.Select(part =>
      $"{Uri.EscapeDataString(part.Name)}={Uri.EscapeDataString(part.Value)}"));
  }

  private static bool TryQueryParameters(string query, out Dictionary<string, string> parameters)
  {
    parameters = new Dictionary<string, string>(StringComparer.Ordinal);
    var source = query;
    if (source.StartsWith("?")) source = source[1..];
    foreach (var part in source.Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
      var fragments = part.Split('=', 2, StringSplitOptions.None);
      if (fragments.Length != 2) return false;

      var key = DecodeFormComponent(fragments[0]);
      var value = DecodeFormComponent(fragments[1]);
      if (key is null || value is null) return false;
      if (!parameters.TryAdd(key, value))
      {
        return false;
      }
    }
    return true;
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
