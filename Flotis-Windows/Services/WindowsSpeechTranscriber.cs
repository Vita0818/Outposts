using System.Diagnostics;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace Flotis.Services;

public sealed class WindowsSpeechTranscriber : SpeechTranscribing
{
    private readonly string _locale;
    private SpeechRecognizer? _recognizer;
    private string _latestResult = string.Empty;

    public Action<string>? PartialTranscriptHandler { get; set; }

    public WindowsSpeechTranscriber(string locale = "zh-CN")
    {
        _locale = locale;
    }

    public async Task Start()
    {
        _latestResult = string.Empty;
        CleanupRecognizer();

        var language = new Language(_locale);
        _recognizer = new SpeechRecognizer(language);
        _recognizer.HypothesisGenerated += OnHypothesisGenerated;
        _recognizer.ContinuousRecognitionSession.ResultGenerated += OnResultGenerated;
        _recognizer.ContinuousRecognitionSession.Completed += OnCompleted;

        _recognizer.Constraints.Clear();
        _recognizer.Constraints.Add(new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation"));

        var compileResult = await _recognizer.CompileConstraintsAsync();
        if (compileResult.Status != SpeechRecognitionResultStatus.Success)
        {
            throw new InvalidOperationException($"语音识别引擎初始化失败：{compileResult.Status}");
        }

        await _recognizer.ContinuousRecognitionSession.StartAsync();
    }

    public async Task<string> Stop()
    {
        if (_recognizer == null)
        {
            return string.Empty;
        }

        try
        {
            await _recognizer.ContinuousRecognitionSession.StopAsync();
        }
        catch
        {
            Debug.WriteLine("ContinuousRecognitionSession.StopAsync failed.");
        }

        return _latestResult;
    }

    public void Cancel()
    {
        _ = Stop();
        CleanupRecognizer();
    }

    private void OnHypothesisGenerated(object? sender, SpeechContinuousRecognitionHypothesisGeneratedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Hypothesis.Text))
        {
            PartialTranscriptHandler?.Invoke(args.Hypothesis.Text);
        }
    }

    private void OnResultGenerated(object? sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Result.Text))
        {
            _latestResult = args.Result.Text;
            PartialTranscriptHandler?.Invoke(args.Result.Text);
        }
    }

    private void OnCompleted(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
    {
        // No-op: app reads latest result on Stop.
    }

    private void CleanupRecognizer()
    {
        if (_recognizer == null) return;

        _recognizer.HypothesisGenerated -= OnHypothesisGenerated;
        _recognizer.ContinuousRecognitionSession.ResultGenerated -= OnResultGenerated;
        _recognizer.ContinuousRecognitionSession.Completed -= OnCompleted;
        _recognizer.Dispose();
        _recognizer = null;
    }
}
