using Kikaria.App.Controls;
using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Kikaria.App.Pages;

/// <summary>上传新预设(对齐 Apple 版 NewPresetView):名称 / 分类 / 导入 .md|.txt / Markdown 文本。</summary>
public sealed partial class NewPresetPage : Page
{
    public NewPresetPage()
    {
        InitializeComponent();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorCard.Visibility = Visibility.Visible;
    }

    private async void OnPickFileClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".txt");

        try
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.WindowHandle);
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            var text = await FileIO.ReadTextAsync(file);
            MarkdownBox.Text = text;

            var importedName = System.IO.Path.GetFileNameWithoutExtension(file.Name).Trim();
            if (NameBox.Text.Trim().Length == 0 && importedName.Length > 0)
            {
                NameBox.Text = importedName;
            }

            ErrorCard.Visibility = Visibility.Collapsed;
        }
        catch (Exception)
        {
            ShowError("文件读取失败，请确认它是 UTF-8 文本。");
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        switch (AppSession.Current.CreatePreset(NameBox.Text, CategoryBox.Text, MarkdownBox.Text))
        {
            case PresetCreationOutcome.Success:
                Toast.Show(Localization.PresetCreatedToast(NameBox.Text.Trim()));
                MainWindow.GoBack();
                break;
            case PresetCreationOutcome.MissingName:
                ShowError("请填写预设名称。");
                break;
            case PresetCreationOutcome.NoValidPoints:
                ShowError("没有解析到有效知识点。请检查 # 标题、tags、hint: 和 content:。");
                break;
        }
    }

    private void OnOpenGuideClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Navigate("markdownGuide");
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
