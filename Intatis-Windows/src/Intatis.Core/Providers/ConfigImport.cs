using System.Text.Json.Nodes;

namespace Intatis.Core.Providers;

public enum SecretSource
{
    Environment,
    File,
    AuthFile,
    ProviderConfig,
    Literal,
    None,
}

/// <summary>
/// A credential reference, never a credential value. Secrets resolve lazily at
/// provider-construction time and never enter the event log or projections.
/// </summary>
public sealed record SecretRef
{
    public SecretSource Source { get; init; } = SecretSource.None;
    public string Value { get; init; } = "";

    public string Describe() => Source switch
    {
        SecretSource.Environment => $"env {Value}",
        SecretSource.File => $"secret file {Value}",
        SecretSource.AuthFile => "auth file",
        SecretSource.ProviderConfig => "provider config",
        SecretSource.Literal => "configured key",
        _ => "not configured",
    };
}

public sealed record ModelEntry
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool Hidden { get; init; } // role-routed models stay out of the inference menus
}

public sealed record ProviderEntry
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    public string? ChatEndpoint { get; init; }
    public SecretRef ApiKeyRef { get; init; } = new();
    public List<ModelEntry> Models { get; init; } = [];
    public string Npm { get; init; } = "@ai-sdk/openai-compatible";
}

/// <summary>&lt;provider-id&gt;/&lt;model-id&gt; role binding.</summary>
public sealed record ModelRef
{
    public string ProviderId { get; init; } = "";
    public string ModelId { get; init; } = "";
}

public sealed record ImportedConfig
{
    public List<ProviderEntry> Providers { get; init; } = [];
    public ModelRef? Chat { get; init; }
    public ModelRef? Reviewer { get; init; }
    public ModelRef? Image { get; init; }
    public ModelRef? Transcription { get; init; }
    public ModelRef? Embedding { get; init; }
    public ModelRef? Reranker { get; init; }
    public string SourcePath { get; init; } = "";
    public List<string> Warnings { get; init; } = [];
    public bool ReviewerFailedClosed { get; init; }

    public ProviderEntry? Provider(string id)
        => Providers.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public List<ModelEntry> InferenceModels()
    {
        var result = new List<ModelEntry>();
        foreach (var provider in Providers)
        {
            foreach (var model in provider.Models)
            {
                if (model.Hidden) continue;
                result.Add(model);
            }
        }
        return result;
    }
}

/// <summary>
/// Parses the Intatis JSON/JSONC configuration: the provider map plus the top-level
/// role routes (model, permission_reviewer_model, image_model, transcription_model,
/// embedding_model, reranker_model). The permission reviewer binding is fail-closed:
/// if the field is present but unresolvable, reviewing stays unavailable instead of
/// silently falling back to the chat model.
/// </summary>
public static class ConfigImport
{
    public const int MaximumByteCount = 1_048_576;

    private static readonly Dictionary<string, string> DefaultBaseUrls = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = "https://api.openai.com/v1",
        ["openrouter"] = "https://openrouter.ai/api/v1",
        ["deepseek"] = "https://api.deepseek.com/v1",
        ["ollama"] = "http://localhost:11434/v1",
        ["lmstudio"] = "http://localhost:1234/v1",
        ["groq"] = "https://api.groq.com/openai/v1",
        ["xai"] = "https://api.x.ai/v1",
        ["together"] = "https://api.together.xyz/v1",
        ["fireworks"] = "https://api.fireworks.ai/inference/v1",
        ["cerebras"] = "https://api.cerebras.ai/v1",
        ["moonshot"] = "https://api.moonshot.cn/v1",
    };

    public static ImportedConfig Parse(string source, string sourcePath, IReadOnlyDictionary<string, string>? environment = null)
    {
        if (source.Length > MaximumByteCount)
            throw new InvalidOperationException("configuration exceeds 1 MiB");
        var root = Jsonx.ParseObject(Jsonx.StripJsonc(source));
        environment ??= new Dictionary<string, string>();

        var warnings = new List<string>();

        var enabled = ReadStringList(root, "enabled_providers", "enabledProviders");
        var disabled = ReadStringList(root, "disabled_providers", "disabledProviders")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var providers = new List<ProviderEntry>();
        if (root["provider"] is JsonObject providerMap)
        {
            foreach (var (providerId, providerNode) in providerMap)
            {
                if (providerNode is null) continue;
                if (disabled.Contains(providerId)) continue;
                if (enabled.Count > 0 &&
                    !enabled.Contains(providerId, StringComparer.OrdinalIgnoreCase)) continue;

                var entry = ParseProvider(providerId, providerNode, environment, warnings);
                if (entry is null)
                {
                    warnings.Add($"provider '{providerId}' skipped: no resolvable base URL");
                    continue;
                }
                providers.Add(entry);
            }
        }
        else if (root["providers"] is JsonArray legacyArray)
        {
            foreach (var node in legacyArray)
            {
                if (node is not JsonObject providerNode) continue;
                var id = (string?)providerNode["id"] ?? "";
                if (id.Length == 0) continue;
                var entry = ParseProvider(id, providerNode, environment, warnings);
                if (entry is null)
                {
                    warnings.Add($"provider '{id}' skipped: no resolvable base URL");
                    continue;
                }
                providers.Add(entry);
            }
        }

        var chat = ParseModelRef(root, "model", environment)
                   ?? ParseModelRef(root, "small_model", environment)
                   ?? ParseModelRef(root, "smallModel", environment);

        ModelRef? reviewer;
        var reviewerFailedClosed = false;
        if (root["permission_reviewer_model"] is { } reviewerNode)
        {
            reviewer = ParseModelRef(root, "permission_reviewer_model", environment);
            if (reviewer is null || Resolve(reviewer, providers) is null)
            {
                reviewer = null;
                reviewerFailedClosed = true;
                warnings.Add("permission_reviewer_model present but unresolvable; automatic review is disabled (fail closed)");
            }
        }
        else
        {
            // Compatibility: a missing field inherits the same document's top-level model.
            reviewer = chat is not null && Resolve(chat, providers) is not null ? chat : null;
        }

        var image = OptionalRole(root, "image_model", environment, providers, warnings, hiddenRole: true);
        var transcription = OptionalRole(root, "transcription_model", environment, providers, warnings, hiddenRole: true);
        var embedding = OptionalRole(root, "embedding_model", environment, providers, warnings, hiddenRole: true);
        var reranker = OptionalRole(root, "reranker_model", environment, providers, warnings, hiddenRole: true);

        MarkRoleRoutedModelsHidden(providers, chat, reviewer,
            new[] { image, transcription, embedding, reranker });

        if (chat is null && providers.Count > 0)
            warnings.Add("no top-level 'model' field; the active model must be picked in the UI");

        return new ImportedConfig
        {
            Providers = providers,
            Chat = chat,
            Reviewer = reviewer,
            Image = image,
            Transcription = transcription,
            Embedding = embedding,
            Reranker = reranker,
            SourcePath = sourcePath,
            Warnings = warnings,
            ReviewerFailedClosed = reviewerFailedClosed,
        };
    }

    /// <summary>
    /// Models bound only to extension roles (image/transcription/embedding/reranker)
    /// stay out of the Chat/Code/Cowork inference menus; models referenced by the
    /// chat or reviewer routes remain selectable.
    /// </summary>
    private static void MarkRoleRoutedModelsHidden(
        List<ProviderEntry> providers,
        ModelRef? chat,
        ModelRef? reviewer,
        ModelRef?[] extensionRoles)
    {
        var extensionKeys = extensionRoles
            .Where(r => r is not null)
            .Select(r => (r!.ProviderId.ToLowerInvariant(), r.ModelId))
            .ToHashSet();

        var menuKeys = new HashSet<(string, string)>(StringComparer.Ordinal);
        void Keep(ModelRef? r)
        {
            if (r is null) return;
            menuKeys.Add((r.ProviderId.ToLowerInvariant(), r.ModelId));
        }
        Keep(chat);
        Keep(reviewer);

        foreach (var provider in providers)
        {
            for (var i = 0; i < provider.Models.Count; i++)
            {
                var model = provider.Models[i];
                var key = (provider.Id.ToLowerInvariant(), model.Id);
                if (extensionKeys.Contains(key) && !menuKeys.Contains(key))
                    provider.Models[i] = model with { Hidden = true };
            }
        }
    }

    private static ProviderEntry? ParseProvider(
        string providerId,
        JsonNode node,
        IReadOnlyDictionary<string, string> environment,
        List<string> warnings)
    {
        var o = node as JsonObject;
        var options = o?["options"] as JsonObject;

        var baseUrl = (string?)options?["baseURL"] ?? (string?)options?["baseUrl"]
            ?? (string?)o?["baseURL"] ?? (string?)o?["baseUrl"]
            ?? DefaultBaseUrls.GetValueOrDefault(providerId) ?? "";
        if (baseUrl.Length == 0) return null;

        var chatEndpoint = (string?)options?["chatEndpoint"] ?? (string?)o?["chatEndpoint"];
        var displayName = (string?)o?["displayName"] ?? (string?)o?["name"] ?? providerId;
        var npm = (string?)o?["npm"] ?? "@ai-sdk/openai-compatible";

        var apiKeyRef = ParseCredential(providerId, options, o, environment);

        var models = new List<ModelEntry>();
        if (o?["models"] is JsonObject modelMap)
        {
            foreach (var (modelId, modelNode) in modelMap)
            {
                if (modelNode is null) continue;
                if (modelNode is JsonValue value && value.TryGetValue<string>(out var modelName))
                {
                    models.Add(new ModelEntry { Id = modelId, DisplayName = modelName });
                    continue;
                }
                if (modelNode is JsonObject modelObj)
                {
                    models.Add(new ModelEntry
                    {
                        Id = (string?)modelObj["id"] ?? modelId,
                        DisplayName = (string?)modelObj["displayName"]
                                      ?? (string?)modelObj["name"] ?? modelId,
                    });
                }
            }
        }
        else if (o?["models"] is JsonArray modelArray)
        {
            foreach (var modelNode in modelArray)
            {
                var id = (string?)modelNode?["id"] ?? (string?)modelNode;
                if (id is null) continue;
                models.Add(new ModelEntry
                {
                    Id = id,
                    DisplayName = (string?)modelNode?["displayName"] ?? (string?)modelNode?["name"] ?? id,
                });
            }
        }

        return new ProviderEntry
        {
            Id = providerId,
            DisplayName = displayName,
            BaseUrl = baseUrl,
            ChatEndpoint = chatEndpoint,
            ApiKeyRef = apiKeyRef,
            Models = models,
            Npm = npm,
        };
    }

    private static SecretRef ParseCredential(
        string providerId,
        JsonObject? options,
        JsonObject? providerObj,
        IReadOnlyDictionary<string, string> environment)
    {
        if (providerObj?["apiKeySource"] is JsonObject apiKeySource)
        {
            var type = ((string?)apiKeySource["type"] ?? "").ToLowerInvariant();
            var value = (string?)apiKeySource["value"] ?? "";
            return type switch
            {
                "env" => new SecretRef { Source = SecretSource.Environment, Value = value },
                "file" => new SecretRef { Source = SecretSource.File, Value = value },
                "authfile" => new SecretRef { Source = SecretSource.AuthFile, Value = providerId },
                "providerconfig" => new SecretRef { Source = SecretSource.ProviderConfig, Value = providerId },
                _ => new SecretRef { Source = SecretSource.AuthFile, Value = providerId },
            };
        }

        var apiKey = (string?)options?["apiKey"] ?? (string?)providerObj?["apiKey"];
        if (apiKey is { Length: > 0 })
        {
            var variable = ParseConfigVariable(apiKey);
            if (variable is { Kind: "env" })
                return new SecretRef { Source = SecretSource.Environment, Value = variable.Value };
            if (variable is { Kind: "file" })
                return new SecretRef { Source = SecretSource.File, Value = variable.Value };
            return new SecretRef { Source = SecretSource.Literal, Value = apiKey };
        }

        var apiKeyEnv = (string?)providerObj?["apiKeyEnv"]
                        ?? (providerObj?["env"] as JsonArray)?.OfType<JsonValue>()
                            .Select(v => (string?)v).FirstOrDefault(v => v is not null);
        if (apiKeyEnv is { Length: > 0 })
            return new SecretRef { Source = SecretSource.Environment, Value = apiKeyEnv };

        var apiKeyFile = (string?)providerObj?["apiKeyFile"];
        if (apiKeyFile is { Length: > 0 })
            return new SecretRef { Source = SecretSource.File, Value = apiKeyFile };

        return new SecretRef { Source = SecretSource.AuthFile, Value = providerId };
    }

    private static ModelRef? ParseModelRef(
        JsonObject root,
        string field,
        IReadOnlyDictionary<string, string> environment)
    {
        var raw = root[field] as string;
        if (raw is null or { Length: 0 }) return null;

        var variable = ParseConfigVariable(raw);
        if (variable is { Kind: "env" } && environment.TryGetValue(variable.Value, out var resolved))
            raw = resolved;
        else if (variable is { Kind: "env" })
            return null;

        var slash = raw.IndexOf('/');
        if (slash <= 0 || slash >= raw.Length - 1) return null;
        return new ModelRef { ProviderId = raw[..slash], ModelId = raw[(slash + 1)..] };
    }

    private static ModelRef? OptionalRole(
        JsonObject root,
        string field,
        IReadOnlyDictionary<string, string> environment,
        List<ProviderEntry> providers,
        List<string> warnings,
        bool hiddenRole)
    {
        var reference = ParseModelRef(root, field, environment);
        if (reference is null)
        {
            if (root[field] is string present)
                warnings.Add($"{field} present but not in '<provider>/<model>' form; route disabled");
            return null;
        }
        var provider = providers.FirstOrDefault(p =>
            string.Equals(p.Id, reference.ProviderId, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            warnings.Add($"{field} references unknown provider '{reference.ProviderId}'; route disabled");
            return null;
        }
        return reference;
    }

    internal static ProviderEntry? Resolve(ModelRef reference, List<ProviderEntry> providers)
        => providers.FirstOrDefault(p =>
            string.Equals(p.Id, reference.ProviderId, StringComparison.OrdinalIgnoreCase)
            && p.Models.Any(m => string.Equals(m.Id, reference.ModelId, StringComparison.Ordinal)));

    /// <summary>Whole trimmed value must be {kind:value}; kinds are case-insensitive.</summary>
    internal static (string Kind, string Value)? ParseConfigVariable(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed[0] != '{' || trimmed[^1] != '}') return null;
        var inner = trimmed[1..^1];
        var colon = inner.IndexOf(':');
        if (colon <= 0 || colon >= inner.Length - 1) return null;
        var kind = inner[..colon].Trim().ToLowerInvariant();
        var body = inner[(colon + 1)..].Trim();
        if (kind is not ("env" or "file")) return null;
        return (kind, body);
    }

    private static List<string> ReadStringList(JsonObject root, params string[] fields)
    {
        foreach (var field in fields)
        {
            if (root[field] is JsonArray array)
            {
                return array.OfType<JsonValue>()
                    .Select(v => (string?)v)
                    .Where(v => v is not null)
                    .Select(v => v!.Trim().ToLowerInvariant())
                    .ToList();
            }
        }
        return [];
    }
}
