using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Kikaria.App.Pages;

/// <summary>
/// 3 页新手引导(FlipView 等价的简化实现:单卡片翻页 + 指示点)。
/// 文案照抄 Apple 版 OnboardingView。
/// </summary>
public sealed partial class OnboardingPage : Page
{
    private sealed record OnboardingSlide(string Title, string Subtitle, string Glyph);

    private readonly OnboardingSlide[] _slides =
    {
        new("选择一套预设", "从数学、物理、计算机科学与英语预设开始，也可以上传自己的 Markdown 知识点。", "\uE8F1"),
        new("先回忆，再查看", "背诵时先看知识点名称，必要时查看提示，再查看答案。", "\uE7A3"),
        new("整理你的学习状态", "把不熟的内容加入重点集锦，把已经掌握的内容标记为已掌握。", "\uE73E")
    };

    private int _index;
    private readonly List<Ellipse> _dots = new();

    public OnboardingPage()
    {
        InitializeComponent();
        BuildDots();
        ShowSlide(0);
    }

    private void BuildDots()
    {
        DotsPanel.Children.Clear();
        _dots.Clear();
        for (var i = 0; i < _slides.Length; i++)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)Application.Current.Resources["BlueGrayBrush"]
            };
            _dots.Add(dot);
            DotsPanel.Children.Add(dot);
        }
    }

    private void ShowSlide(int index)
    {
        _index = Math.Clamp(index, 0, _slides.Length - 1);
        var slide = _slides[_index];

        SlideIcon.Glyph = slide.Glyph;
        SlideTitle.Text = slide.Title;
        SlideSubtitle.Text = slide.Subtitle;
        NextButton.Content = _index == _slides.Length - 1 ? "开始使用" : "下一步";

        var active = Theme.ThemedBrush(this, "SkyBrush");
        var idle = Theme.ThemedBrush(this, "BlueGrayBrush");
        for (var i = 0; i < _dots.Count; i++)
        {
            _dots[i].Fill = i == _index ? active : idle;
            _dots[i].Width = i == _index ? 18 : 8;
        }
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_index < _slides.Length - 1)
        {
            ShowSlide(_index + 1);
            return;
        }

        AppSession.Current.CompleteOnboarding();
        if (!AppSession.Current.State.HasCompletedProfileSetup)
        {
            MainWindow.Navigate("profileSetup", clearHistory: true);
        }
        else
        {
            MainWindow.Navigate("home", clearHistory: true);
        }
    }
}
