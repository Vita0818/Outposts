using System.Text.Json;

namespace Intatis.Windows.Shared;

public static class ConfigStore
{
    private const string AppFolder = "Intatis";
    private const string AppName = "Intatis-Windows";
    private const string ConfigFileName = "config.json";

    public static string ConfigFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolder, AppName);

    public static string ConfigPath =>
        Path.Combine(ConfigFolder, ConfigFileName);

    public static IntatisConfig Load()
    {
        var fileValues = LoadFromFile();

        string value(string envKey, string fileKey, string? fallback)
        {
            var env = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrWhiteSpace(env))
                return env;
            if (fileValues.TryGetValue(fileKey, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
            return fallback ?? string.Empty;
        }

        var baseUrl = value("INTATIS_BASE_URL", nameof(BaseUrl), "https://api.openai.com/v1");
        var apiKey = value("INTATIS_API_KEY", nameof(ApiKey), string.Empty);
        var model = value("INTATIS_MODEL", nameof(Model), "gpt-4o-mini");
        var reasoning = value("INTATIS_REASONING", nameof(Reasoning), null);
        var modeValue = value("INTATIS_MODE", nameof(DefaultMode), IntatisMode.Chat.ToString());
        var workspace = value("INTATIS_WORKSPACE", nameof(Workspace), string.Empty);
        var usageValue = value("INTATIS_USAGE", nameof(IncludeUsage), "1").ToLowerInvariant();
        var includeUsage = usageValue is not ("0" or "false" or "off");

        var mode = Enum.TryParse(modeValue, true, out IntatisMode parsedMode)
            ? parsedMode
            : IntatisMode.Chat;

        return new IntatisConfig(
            baseUrl,
            apiKey,
            model,
            string.IsNullOrWhiteSpace(reasoning) ? null : reasoning,
            mode,
            string.IsNullOrWhiteSpace(workspace) ? null : workspace,
            includeUsage);
    }

    public static void Save(IntatisConfig config)
    {
        var payload = new Dictionary<string, string>
        {
            [nameof(BaseUrl)] = config.BaseUrl,
            [nameof(ApiKey)] = config.ApiKey,
            [nameof(Model)] = config.Model,
            [nameof(DefaultMode)] = config.DefaultMode.ToString().ToLowerInvariant()
        };

        if (!string.IsNullOrWhiteSpace(config.Reasoning))
            payload[nameof(Reasoning)] = config.Reasoning!;
        if (!string.IsNullOrWhiteSpace(config.Workspace))
            payload[nameof(Workspace)] = config.Workspace!;
        payload[nameof(IncludeUsage)] = config.IncludeUsage ? "1" : "0";

        Directory.CreateDirectory(ConfigFolder);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
        try
        {
            File.SetAttributes(ConfigPath, FileAttributes.Hidden | File.GetAttributes(ConfigPath));
        }
        catch
        {
            // Best effort only. On some systems attributes may be restricted.
        }
    }

    private static Dictionary<string, string> LoadFromFile()
    {
        if (!File.Exists(ConfigPath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var raw = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw, new JsonSerializerOptions())
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

