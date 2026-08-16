using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Kikaria.App.Pages;

/// <summary>Markdown 格式说明(对齐 Apple 版 MarkdownFormatGuideView,文案照抄)。</summary>
public sealed partial class MarkdownGuidePage : Page
{
    public MarkdownGuidePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        FormatTemplateText.Text = Localization.MarkdownFormatTemplate;
        LatexExampleText.Text = Localization.MarkdownLatexExample;
        CompleteExampleText.Text = Localization.MarkdownCompleteExample;
        AiPromptText.Text = Localization.MarkdownAIPrompt;
    }

    private void OnCopyPromptClick(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(Localization.MarkdownAIPrompt);
        Clipboard.SetContent(package);
        Toast.Show("Prompt 已复制");
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
