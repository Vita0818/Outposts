using System.Text.Json;
using Flotis.Interop;

namespace Flotis.Services;

public sealed class TranscriptionProviderStore
{
    public static TranscriptionProviderStore Shared { get; } = new();

    private static readonly string ConfigFilePath = Path.Combine(FileLocations.AppDataFolder, "flotis-provider-config.json");

    public TranscriptionProviderConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new TranscriptionProviderConfig();
            }

            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<TranscriptionProviderConfig>(json) ?? new TranscriptionProviderConfig();
        }
        catch
        {
            return new TranscriptionProviderConfig();
        }
    }

    public void SaveConfig(TranscriptionProviderConfig config)
    {
        Directory.CreateDirectory(FileLocations.AppDataFolder);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}
