namespace Intatis.Core;

/// <summary>Application-level paths and defaults shared by the CLI and the GUI.</summary>
public static class AppConfig
{
    public const string DefaultBaseUrl = "https://api.openai.com/v1";
    public const string DefaultModel = "gpt-4o-mini";
    public const string ConfigFileName = "intatis.json";
    public const string ConfigFileNameC = "intatis.jsonc";

    /// <summary>
    /// Session data root. Windows: %AppData%\Intatis\Intatis-Windows; other hosts fall
    /// back to ~/.intatis so the CLI stays testable cross-platform. INTATIS_HOME wins.
    /// </summary>
    public static string ApplicationDataRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("INTATIS_HOME");
        if (!string.IsNullOrWhiteSpace(overrideRoot)) return overrideRoot;

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Intatis", "Intatis-Windows");
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".intatis");
    }

    public static string SessionsRoot() => Path.Combine(ApplicationDataRoot(), "sessions");

    /// <summary>Candidate config files in priority order: INTATIS_CONFIG, app data, ~/.config.</summary>
    public static List<string> ConfigCandidates()
    {
        var candidates = new List<string>();
        var env = Environment.GetEnvironmentVariable("INTATIS_CONFIG");
        if (!string.IsNullOrWhiteSpace(env)) candidates.Add(env);

        var root = ApplicationDataRoot();
        candidates.Add(Path.Combine(root, ConfigFileNameC));
        candidates.Add(Path.Combine(root, ConfigFileName));

        var configHome = Environment.GetEnvironmentVariable("INTATIS_CONFIG_HOME");
        var userConfig = string.IsNullOrWhiteSpace(configHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "intatis")
            : configHome;
        candidates.Add(Path.Combine(userConfig, ConfigFileNameC));
        candidates.Add(Path.Combine(userConfig, ConfigFileName));
        return candidates;
    }

    public static string DefaultConfigPath()
        => ConfigCandidates().FirstOrDefault(File.Exists)
           ?? ConfigCandidates()[1];

    public static string AuthFilePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Intatis", "auth.json");
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "intatis", "auth.json");
    }
}
