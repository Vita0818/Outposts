namespace Flotis.Services;

public sealed class TranscriptionProviderConfig
{
    public string Name { get; set; } = "OpenAI";
    public string BaseURL { get; set; } = "https://api.openai.com";
    public string EndpointPath { get; set; } = "/v1/audio/transcriptions";
    public string Model { get; set; } = "whisper-1";
    public string? ApiKeyReference { get; set; } = "flotis.externalprovider.apikey";
    public string? Language { get; set; } = "zh";
    public double? Temperature { get; set; } = 0.0;
}
