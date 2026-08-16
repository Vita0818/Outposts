using System.Text.Json.Nodes;

namespace Intatis.Core.Providers;

public interface ISecretResolver
{
    string ResolveSecret(SecretRef reference);
}

/// <summary>
/// Resolves credential references lazily at provider-construction time:
/// env vars, secret files, the owner-selected auth.json, or the provider config file.
/// A literal in-config key is accepted but flagged via warnings at import time.
/// </summary>
public sealed class ConfigSecretResolver : ISecretResolver
{
    private readonly string _authFilePath;
    private readonly string _configPath;

    public ConfigSecretResolver(string authFilePath, string configPath)
    {
        _authFilePath = authFilePath;
        _configPath = configPath;
    }

    public string ResolveSecret(SecretRef reference)
    {
        try
        {
            switch (reference.Source)
            {
                case SecretSource.Environment:
                    return Environment.GetEnvironmentVariable(reference.Value) ?? "";

                case SecretSource.File:
                    return File.Exists(reference.Value)
                        ? File.ReadAllText(reference.Value).Trim()
                        : "";

                case SecretSource.AuthFile:
                    return LookupAuthFile(reference.Value);

                case SecretSource.ProviderConfig:
                    return LookupProviderConfig(reference.Value);

                case SecretSource.Literal:
                    return reference.Value;

                default:
                    return "";
            }
        }
        catch (Exception)
        {
            return "";
        }
    }

    private string LookupAuthFile(string providerId)
    {
        if (!File.Exists(_authFilePath)) return "";
        var node = JsonNode.Parse(File.ReadAllText(_authFilePath));
        return (string?)node?[providerId] ?? "";
    }

    private string LookupProviderConfig(string providerId)
    {
        if (!File.Exists(_configPath)) return "";
        var node = JsonNode.Parse(Jsonx.StripJsonc(File.ReadAllText(_configPath))) as JsonObject;
        var provider = node?["provider"]?[providerId] as JsonObject
                       ?? node?["provider"]?[providerId.ToLowerInvariant()] as JsonObject;
        return (string?)provider?["options"]?["apiKey"] ?? (string?)provider?["apiKey"] ?? "";
    }
}

/// <summary>
/// Builds providers on demand. The API key is only resolved here, at construction
/// time, never at config-load time.
/// </summary>
public sealed class ProviderRegistry
{
    private readonly ImportedConfig _config;
    private readonly ISecretResolver _resolver;
    private readonly Dictionary<string, OpenAIWireProvider> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ProviderRegistry(ImportedConfig config, ISecretResolver resolver)
    {
        _config = config;
        _resolver = resolver;
    }

    public ImportedConfig Config => _config;

    private static readonly HttpClient SharedHttp = new()
    {
        Timeout = TimeSpan.FromMinutes(10),
    };

    public OpenAIWireProvider ChatProviderFor(string providerId)
    {
        if (_cache.TryGetValue(providerId, out var cached)) return cached;
        var entry = _config.Provider(providerId)
            ?? throw new InvalidOperationException($"unknown provider '{providerId}'");
        var apiKey = _resolver.ResolveSecret(entry.ApiKeyRef);
        var provider = new OpenAIWireProvider(SharedHttp, entry.BaseUrl, apiKey, entry.ChatEndpoint);
        _cache[providerId] = provider;
        return provider;
    }

    /// <summary>True when the provider's credential is non-empty (resolved on demand).</summary>
    public bool HasCredential(string providerId)
    {
        var entry = _config.Provider(providerId);
        return entry is not null && _resolver.ResolveSecret(entry.ApiKeyRef).Length > 0;
    }
}

/// <summary>Loads the Intatis configuration from the candidate paths, with env overrides.</summary>
public static class ConfigStore
{
    public static (ImportedConfig Config, string SourcePath) Load()
    {
        var candidates = AppConfig.ConfigCandidates();
        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate)) continue;
            var text = File.ReadAllText(candidate);
            var environment = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Where(e => e.Key is string && e.Value is string)
                .ToDictionary(e => (string)e.Key, e => (string)e.Value);
            var config = ConfigImport.Parse(text, candidate, environment);
            return (ApplyEnvOverrides(config), candidate);
        }

        // No config file: honor the flat env overrides alone (CLI-friendly).
        var empty = new ImportedConfig();
        return (ApplyEnvOverrides(empty), "");
    }

    public static void Save(ImportedConfig config, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Serialize(config));
    }

    /// <summary>Writes the canonical modern shape (provider map + role fields).</summary>
    public static string Serialize(ImportedConfig config)
    {
        var providerMap = new JsonObject();
        foreach (var provider in config.Providers)
        {
            var options = new JsonObject
            {
                ["baseURL"] = provider.BaseUrl,
                ["apiKey"] = provider.ApiKeyRef.Source switch
                {
                    SecretSource.Environment => $"{{env:{provider.ApiKeyRef.Value}}}",
                    SecretSource.File => $"{{file:{provider.ApiKeyRef.Value}}}",
                    SecretSource.Literal => provider.ApiKeyRef.Value,
                    _ => provider.ApiKeyRef.Value,
                },
            };
            if (provider.ChatEndpoint is { Length: > 0 } chatEndpoint)
                options["chatEndpoint"] = chatEndpoint;

            var models = new JsonObject();
            foreach (var model in provider.Models)
                models[model.Id] = model.DisplayName.Length > 0 ? model.DisplayName : model.Id;

            providerMap[provider.Id] = new JsonObject
            {
                ["npm"] = provider.Npm,
                ["displayName"] = provider.DisplayName,
                ["options"] = options,
                ["models"] = models,
            };
        }

        var root = new JsonObject { ["provider"] = providerMap };
        if (config.Chat is { } chat) root["model"] = $"{chat.ProviderId}/{chat.ModelId}";
        if (config.Reviewer is { } reviewer) root["permission_reviewer_model"] = $"{reviewer.ProviderId}/{reviewer.ModelId}";
        if (config.Image is { } image) root["image_model"] = $"{image.ProviderId}/{image.ModelId}";
        if (config.Transcription is { } transcription) root["transcription_model"] = $"{transcription.ProviderId}/{transcription.ModelId}";
        if (config.Embedding is { } embedding) root["embedding_model"] = $"{embedding.ProviderId}/{embedding.ModelId}";
        if (config.Reranker is { } reranker) root["reranker_model"] = $"{reranker.ProviderId}/{reranker.ModelId}";

        return root.ToJsonString(Jsonx.Pretty);
    }

    private static ImportedConfig ApplyEnvOverrides(ImportedConfig config)
    {
        var baseUrl = Environment.GetEnvironmentVariable("INTATIS_BASE_URL");
        var apiKey = Environment.GetEnvironmentVariable("INTATIS_API_KEY");
        var model = Environment.GetEnvironmentVariable("INTATIS_MODEL");
        if (baseUrl is null && apiKey is null && model is null) return config;

        var providers = config.Providers;
        if (baseUrl is { Length: > 0 } || apiKey is { Length: > 0 })
        {
            var envProvider = new ProviderEntry
            {
                Id = "env",
                DisplayName = "Environment",
                BaseUrl = baseUrl ?? AppConfig.DefaultBaseUrl,
                ApiKeyRef = apiKey is { Length: > 0 }
                    ? new SecretRef { Source = SecretSource.Environment, Value = "INTATIS_API_KEY" }
                    : new SecretRef { Source = SecretSource.AuthFile, Value = "env" },
                Models = [],
            };
            providers = new List<ProviderEntry>(config.Providers) { envProvider };
        }

        ModelRef? chat = config.Chat;
        if (model is { Length: > 0 })
        {
            var slash = model.IndexOf('/');
            chat = slash > 0
                ? new ModelRef { ProviderId = model[..slash], ModelId = model[(slash + 1)..] }
                : new ModelRef { ProviderId = "env", ModelId = model };
        }

        return config with { Providers = providers, Chat = chat };
    }
}
