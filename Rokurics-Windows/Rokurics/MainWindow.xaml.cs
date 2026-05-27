using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.Views;

namespace Rokurics;

/// <summary>
/// Main app window with sidebar navigation matching MacRootView from Apple source.
/// Pages resolve their dependencies from the App DI container.
/// </summary>
public sealed partial class MainWindow : Window
{
    private string _currentSelection = "studyLibrary";
    private bool _isSettingsSelected;

    public MainWindow()
    {
        InitializeComponent();
        NavigateTo("studyLibrary");
    }

    private void OnSidebarSelectionChanged(string selection)
    {
        if (selection == "settings")
        {
            _isSettingsSelected = true;
            ContentFrame.Content = CreatePage<MacSettingsPage>();
        }
        else
        {
            _isSettingsSelected = false;
            _currentSelection = selection;
            NavigateTo(selection);
        }
    }

    private void NavigateTo(string page)
    {
        ContentFrame.Content = page switch
        {
            "studyLibrary" => CreatePage<MacStudyLibraryPage>(),
            "aiChat" => CreatePage<MacAIChatPage>(),
            "iPhoneConnection" => CreatePage<MacIPhoneConnectionPage>(),
            "providerDetailTranscription" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.Transcription, "Whisper.cpp"),
            "providerDetailNoteGeneration" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.NoteGeneration, "笔记生成"),
            "providerDetailChat" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.Chat, "AI 对话"),
            _ => CreatePage<MacStudyLibraryPage>()
        };
    }

    private MacProviderDetailPage CreateProviderDetailPage(
        ProviderDetailCard.ProviderCardKind kind, string? name = null)
    {
        var page = new MacProviderDetailPage();
        page.ConfigureFor(kind, name);
        page.NavigateBack += () =>
        {
            // Navigate back to settings when done
            ContentFrame.Content = CreatePage<MacSettingsPage>();
        };
        return page;
    }

    /// <summary>
    /// Creates a page using DI-resolved dependencies where possible.
    /// Each page's constructor handles its own DI resolution via App.Current.Services.
    /// </summary>
    private static T CreatePage<T>() where T : Page, new() => new T();
}
