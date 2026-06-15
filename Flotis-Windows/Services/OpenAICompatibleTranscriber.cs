using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Flotis.Models;

namespace Flotis.Services;

public sealed class OpenAICompatibleTranscriber : SpeechTranscribing
{
    private readonly TranscriptionProviderConfig _config;
    private readonly string? _apiKey;
    private readonly AudioRecorder _audioRecorder = new();
    private static readonly HttpClient HttpClient = new();

    public OpenAICompatibleTranscriber(TranscriptionProviderConfig config, string? apiKey)
    {
        _config = config;
        _apiKey = apiKey;
    }

    public Action<string>? PartialTranscriptHandler
    {
        get; set;
    }

    public async Task Start()
    {
        if (_apiKey is null || _apiKey.Length == 0)
        {
            throw new InvalidOperationException("请先配置外部转写提供商的 API Key。");
        }

        PartialTranscriptHandler?.Invoke("开始录音...");
        await _audioRecorder.StartRecordingAsync();
    }

    public async Task<string> Stop()
    {
        var file = await _audioRecorder.StopRecordingAsync();
        if (string.IsNullOrWhiteSpace(file))
        {
            return string.Empty;
        }

        PartialTranscriptHandler?.Invoke("上传中...");
        try
        {
            return await UploadAsync(file);
        }
        finally
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }

    public void Cancel()
    {
        _audioRecorder.CancelRecording();
    }

    private async Task<string> UploadAsync(string filePath)
    {
        var baseUrl = (_config.BaseURL ?? string.Empty).TrimEnd('/');
        var endpoint = _config.EndpointPath ?? "/v1/audio/transcriptions";
        var endpointUrl = $"{baseUrl}/{endpoint.TrimStart('/')}";

        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("外部转写配置中的 Base URL 或 Endpoint 无效。");
        }

        using var form = new MultipartFormDataContent($"----FlotisBoundary-{Guid.NewGuid()}");
        form.Add(new StringContent(_config.Model ?? "whisper-1", Encoding.UTF8), "model");
        if (!string.IsNullOrWhiteSpace(_config.Language))
        {
            form.Add(new StringContent(_config.Language, Encoding.UTF8), "language");
        }
        if (_config.Temperature.HasValue)
        {
            form.Add(new StringContent(_config.Temperature.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), Encoding.UTF8), "temperature");
        }
        form.Add(new StringContent("json", Encoding.UTF8), "response_format");

        byte[] fileBytes;
        try
        {
            fileBytes = await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"录音文件读取失败：{ex.Message}");
        }

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");
        form.Add(fileContent, "file", Path.GetFileName(filePath));

        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = form;

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"上传音频失败：{ex.Message}");
        }

        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var statusText = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException($"转写请求失败：{statusText}.");
            }
            throw new InvalidOperationException($"转写请求失败：{statusText}，{responseText}");
        }

        using var jsonDoc = JsonDocument.Parse(responseText);
        return ParseTranscript(jsonDoc.RootElement) ?? throw new InvalidOperationException("转写响应中没有找到 text 字段。");
    }

    private static string? ParseTranscript(JsonElement root)
    {
        if (root.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
        {
            return text.GetString()?.Trim();
        }
        if (root.TryGetProperty("transcript", out var transcript) && transcript.ValueKind == JsonValueKind.String)
        {
            return transcript.GetString()?.Trim();
        }
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("text", out var nestedText) && nestedText.ValueKind == JsonValueKind.String)
        {
            return nestedText.GetString()?.Trim();
        }
        return null;
    }
}
