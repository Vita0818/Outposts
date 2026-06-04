using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Rokurics.Helpers;
using Rokurics.Models;

namespace Rokurics.Views;

/// <summary>
/// Visual folder color picker with 12-color swatch grid.
/// Mirrors the folder color token system from Apple source StudyFolderColorToken.
/// Colors match MacTheme.swift + RokuricsColors palette.
/// </summary>
public sealed partial class FolderColorPicker : UserControl
{
    private readonly List<ColorSwatchItem> _swatches;
    private StudyFolderColorToken _selectedToken = StudyFolderColorToken.Default;
    private StudyFolderColorToken _originalToken = StudyFolderColorToken.Default;

    public event Action<StudyFolderColorToken>? ColorSelected;
    public event Action? ResetToDefault;

    public StudyFolderColorToken SelectedToken => _selectedToken;

    public FolderColorPicker()
    {
        InitializeComponent();
        _swatches = BuildSwatches();
        SwatchGrid.ItemsSource = _swatches;
    }

    /// <summary>
    /// Initialize with the current folder color token.
    /// </summary>
    public void Initialize(StudyFolderColorToken currentToken)
    {
        _originalToken = currentToken;
        _selectedToken = currentToken;
        UpdatePreview();
        UpdateSwatchSelection();
    }

    private static List<ColorSwatchItem> BuildSwatches()
    {
        var colors = new (StudyFolderColorToken token, Color color, string chinese, string english)[]
        {
            (StudyFolderColorToken.Default, RokuricsColors.FolderDefault, "蓝色", "Blue"),
            (StudyFolderColorToken.Red, RokuricsColors.FolderRed, "红色", "Red"),
            (StudyFolderColorToken.Orange, RokuricsColors.FolderOrange, "橙色", "Orange"),
            (StudyFolderColorToken.Yellow, RokuricsColors.FolderYellow, "黄色", "Yellow"),
            (StudyFolderColorToken.Green, RokuricsColors.FolderGreen, "绿色", "Green"),
            (StudyFolderColorToken.Mint, RokuricsColors.FolderMint, "薄荷", "Mint"),
            (StudyFolderColorToken.Teal, RokuricsColors.FolderTeal, "青蓝", "Teal"),
            (StudyFolderColorToken.Cyan, RokuricsColors.FolderCyan, "青色", "Cyan"),
            (StudyFolderColorToken.Blue, RokuricsColors.FolderBlue, "深蓝", "Deep Blue"),
            (StudyFolderColorToken.Indigo, RokuricsColors.FolderIndigo, "靛蓝", "Indigo"),
            (StudyFolderColorToken.Purple, RokuricsColors.FolderPurple, "紫色", "Purple"),
            (StudyFolderColorToken.Gray, RokuricsColors.FolderGray, "灰色", "Gray"),
        };

        return colors.Select(c => new ColorSwatchItem
        {
            Token = c.token,
            Brush = new SolidColorBrush(c.color),
            ChineseName = c.chinese,
            EnglishName = c.english,
            IsSelected = false
        }).ToList();
    }

    private void Swatch_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not StudyFolderColorToken token) return;

        _selectedToken = token;
        UpdatePreview();
        UpdateSwatchSelection();
    }

    private void UpdatePreview()
    {
        var (chinese, english, color) = ColorInfoFor(_selectedToken);
        PreviewSwatch.Background = new SolidColorBrush(color);
        ColorNameBlock.Text = chinese;
        ColorTokenBlock.Text = english;
    }

    private void UpdateSwatchSelection()
    {
        foreach (var swatch in _swatches)
            swatch.IsSelected = swatch.Token == _selectedToken;
        // Force ItemsControl refresh
        SwatchGrid.ItemsSource = null;
        SwatchGrid.ItemsSource = _swatches;
    }

    private void ResetToDefault_Click(object sender, RoutedEventArgs e)
    {
        _selectedToken = StudyFolderColorToken.Default;
        UpdatePreview();
        UpdateSwatchSelection();
        ResetToDefault?.Invoke();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ColorSelected?.Invoke(_selectedToken);
    }

    private static (string chinese, string english, Color color) ColorInfoFor(
        StudyFolderColorToken token) => token switch
    {
        StudyFolderColorToken.Red => ("红色", "Red", RokuricsColors.FolderRed),
        StudyFolderColorToken.Orange => ("橙色", "Orange", RokuricsColors.FolderOrange),
        StudyFolderColorToken.Yellow => ("黄色", "Yellow", RokuricsColors.FolderYellow),
        StudyFolderColorToken.Green => ("绿色", "Green", RokuricsColors.FolderGreen),
        StudyFolderColorToken.Mint => ("薄荷", "Mint", RokuricsColors.FolderMint),
        StudyFolderColorToken.Teal => ("青蓝", "Teal", RokuricsColors.FolderTeal),
        StudyFolderColorToken.Cyan => ("青色", "Cyan", RokuricsColors.FolderCyan),
        StudyFolderColorToken.Blue => ("深蓝", "Deep Blue", RokuricsColors.FolderBlue),
        StudyFolderColorToken.Indigo => ("靛蓝", "Indigo", RokuricsColors.FolderIndigo),
        StudyFolderColorToken.Purple => ("紫色", "Purple", RokuricsColors.FolderPurple),
        StudyFolderColorToken.Gray => ("灰色", "Gray", RokuricsColors.FolderGray),
        _ => ("蓝色", "Blue (Default)", RokuricsColors.FolderDefault)
    };
}

internal sealed class ColorSwatchItem
{
    public StudyFolderColorToken Token { get; set; }
    public SolidColorBrush Brush { get; set; } = null!;
    public string ChineseName { get; set; } = "";
    public string EnglishName { get; set; } = "";
    public bool IsSelected { get; set; }
}
