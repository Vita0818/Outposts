using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>添加 / 编辑知识点(对齐 Apple 版 EditKnowledgePointView)。</summary>
public sealed partial class EditKnowledgePointPage : Page
{
    private string _presetId = "";
    private KnowledgePoint? _point;

    public EditKnowledgePointPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var session = AppSession.Current;
        _presetId = session.PendingPresetId ?? session.State.CurrentPresetID;
        var preset = session.State.Presets.FirstOrDefault(p => p.Id == _presetId) ?? session.CurrentPreset;
        _presetId = preset.Id;

        var state = session.StateForPreset(_presetId);
        _point = session.PendingPointId is { } pointId
            ? state.KnowledgePoints.FirstOrDefault(point => point.Id == pointId)
            : null;

        TitleText.Text = _point is null ? "添加知识点" : "编辑知识点";
        PresetNameText.Text = preset.Name;

        TitleBox.Text = _point?.Title ?? "";
        TagsBox.Text = _point is null ? "" : string.Join(", ", _point.Tags);
        HintBox.Text = _point?.Hint ?? "";
        ContentBox.Text = _point?.Content ?? "";
        ErrorCard.Visibility = Visibility.Collapsed;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var trimmedTitle = TitleBox.Text.Trim();
        var trimmedHint = HintBox.Text.Trim();
        var trimmedContent = ContentBox.Text.Trim();
        var tags = TagsBox.Text
            .Split(',', '，')
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .ToList();

        if (trimmedTitle.Length == 0 || trimmedHint.Length == 0 || trimmedContent.Length == 0)
        {
            ErrorCard.Visibility = Visibility.Visible;
            return;
        }

        var now = DateTime.Now;
        var saved = new KnowledgePoint
        {
            Id = _point?.Id ?? Guid.NewGuid(),
            Title = trimmedTitle,
            Tags = tags,
            Hint = trimmedHint,
            Content = trimmedContent,
            ReinforcementCount = _point?.ReinforcementCount ?? 0,
            LastReinforcedAt = _point?.LastReinforcedAt,
            IsMastered = _point?.IsMastered ?? false,
            CreatedAt = _point?.CreatedAt ?? now,
            UpdatedAt = now
        };

        AppSession.Current.UpsertKnowledgePoint(_presetId, saved);
        Toast.Show("已更新 " + AppSession.Current.StateForPreset(_presetId).KnowledgePoints.Count + " 个知识点");
        MainWindow.GoBack();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
