using System.Collections.ObjectModel;
using Intatis.App.Services;
using Intatis.Core.Providers;

namespace Intatis.App.ViewModels;

public sealed class ModelRowVm : ObservableBase
{
    private string _id = "";
    private string _displayName = "";

    public string Id
    {
        get => _id;
        set => Set(ref _id, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => Set(ref _displayName, value);
    }
}

public sealed class ProviderVm : ObservableBase
{
    private string _displayName = "";
    private string _baseUrl = "";
    private string _chatEndpoint = "";

    public required string Id { get; init; }
    public string KeyDescription { get; init; } = "";

    public string DisplayName
    {
        get => _displayName;
        set => Set(ref _displayName, value);
    }

    public string BaseUrl
    {
        get => _baseUrl;
        set => Set(ref _baseUrl, value);
    }

    public string ChatEndpoint
    {
        get => _chatEndpoint;
        set => Set(ref _chatEndpoint, value);
    }

    public ObservableCollection<ModelRowVm> Models { get; } = [];

    public string ModelCountLabel => Models.Count == 0 ? "no models" : $"{Models.Count} model(s)";
}

/// <summary>Settings surface: provider catalog editor writing the Intatis JSON config.</summary>
public sealed class SettingsViewModel : ObservableBase
{
    private readonly AppEnvironment _environment;
    private ProviderVm? _selectedProvider;
    private string _statusText = "";
    private string _rolesSummary = "";

    public ObservableCollection<ProviderVm> Providers { get; } = [];

    public ProviderVm? SelectedProvider
    {
        get => _selectedProvider;
        set => Set(ref _selectedProvider, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public string RolesSummary
    {
        get => _rolesSummary;
        private set => Set(ref _rolesSummary, value);
    }

    public string ConfigPath => _environment.ConfigSourcePath.Length > 0
        ? _environment.ConfigSourcePath
        : AppConfig.DefaultConfigPath();

    public SettingsViewModel(AppEnvironment environment)
    {
        _environment = environment;
        environment.ConfigChanged += () => Reload();
        Reload();
    }

    public void Reload()
    {
        Providers.Clear();
        foreach (var provider in _environment.Config.Providers)
        {
            var vm = new ProviderVm
            {
                Id = provider.Id,
                DisplayName = provider.DisplayName,
                BaseUrl = provider.BaseUrl,
                ChatEndpoint = provider.ChatEndpoint ?? "",
                KeyDescription = provider.ApiKeyRef.Describe(),
            };
            foreach (var model in provider.Models)
                vm.Models.Add(new ModelRowVm { Id = model.Id, DisplayName = model.DisplayName });
            vm.Models.CollectionChanged += (_, _) => vm.Raise(nameof(vm.ModelCountLabel));
            Providers.Add(vm);
        }
        SelectedProvider = Providers.FirstOrDefault();

        var config = _environment.Config;
        string Role(ModelRef? reference)
            => reference is null ? "-" : $"{reference.ProviderId}/{reference.ModelId}";
        RolesSummary = $"chat: {Role(config.Chat)}  ·  reviewer: {Role(config.Reviewer)}  ·  image: {Role(config.Image)}  ·  transcription: {Role(config.Transcription)}  ·  embedding: {Role(config.Embedding)}  ·  reranker: {Role(config.Reranker)}";
        StatusText = config.SourcePath.Length > 0 ? "loaded" : "no config file — defaults";
    }

    public void AddProvider(string id)
    {
        id = id.Trim();
        if (id.Length == 0 || Providers.Any(p => p.Id == id)) return;
        var vm = new ProviderVm
        {
            Id = id,
            DisplayName = id,
            BaseUrl = AppConfig.DefaultBaseUrl,
            KeyDescription = "auth file",
        };
        Providers.Add(vm);
        SelectedProvider = vm;
    }

    public void DeleteProvider(ProviderVm provider) => Providers.Remove(provider);

    public void AddModel(ProviderVm provider, string id)
    {
        id = id.Trim();
        if (id.Length == 0 || provider.Models.Any(m => m.Id == id)) return;
        provider.Models.Add(new ModelRowVm { Id = id, DisplayName = id });
    }

    public void DeleteModel(ProviderVm provider, ModelRowVm model) => provider.Models.Remove(model);

    public string Save()
    {
        var previous = _environment.Config;
        var edited = new ImportedConfig
        {
            Providers = Providers.Select(p => new ProviderEntry
            {
                Id = p.Id,
                DisplayName = p.DisplayName.Length > 0 ? p.DisplayName : p.Id,
                BaseUrl = p.BaseUrl,
                ChatEndpoint = p.ChatEndpoint.Length > 0 ? p.ChatEndpoint : null,
                ApiKeyRef = previous.Provider(p.Id)?.ApiKeyRef
                    ?? new SecretRef { Source = SecretSource.AuthFile, Value = p.Id },
                Models = p.Models.Select(m => new ModelEntry
                {
                    Id = m.Id,
                    DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? m.Id : m.DisplayName,
                }).ToList(),
                Npm = previous.Provider(p.Id)?.Npm ?? "@ai-sdk/openai-compatible",
            }).ToList(),
            Chat = previous.Chat,
            Reviewer = previous.Reviewer,
            Image = previous.Image,
            Transcription = previous.Transcription,
            Embedding = previous.Embedding,
            Reranker = previous.Reranker,
            SourcePath = previous.SourcePath,
            Warnings = [],
            ReviewerFailedClosed = previous.ReviewerFailedClosed,
        };
        var path = _environment.SaveConfig(edited);
        StatusText = $"saved → {path}";
        return path;
    }

    public async Task TestProviderAsync(ProviderVm provider)
    {
        StatusText = $"testing {provider.Id}…";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            using var request = new HttpRequestMessage(HttpMethod.Get,
                provider.BaseUrl.TrimEnd('/') + "/models");
            var entry = _environment.Config.Provider(provider.Id);
            if (entry is not null)
            {
                var key = new ConfigSecretResolver(AppConfig.AuthFilePath(), ConfigPath)
                    .ResolveSecret(entry.ApiKeyRef);
                if (key.Length > 0)
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
            }
            using var response = await http.SendAsync(request);
            StatusText = response.IsSuccessStatusCode
                ? $"{provider.Id}: reachable ({(int)response.StatusCode})"
                : $"{provider.Id}: HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            StatusText = $"{provider.Id}: {ex.Message}";
        }
    }
}
