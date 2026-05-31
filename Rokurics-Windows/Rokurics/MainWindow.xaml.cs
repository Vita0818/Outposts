using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.Services;
using Rokurics.Views;
using Windows.Graphics;

namespace Rokurics;

public sealed partial class MainWindow : Window
{
    private string _currentSelection = "studyLibrary";
    private readonly Dictionary<string, Page> _pageCache = new();

    public MainWindow()
    {
        InitializeComponent();

        var appWindow = GetAppWindowForCurrentWindow();
        if (appWindow is not null)
        {
            appWindow.Resize(new SizeInt32(1040, 690));
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }

        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        NavigateTo("studyLibrary");
    }

    private AppWindow? GetAppWindowForCurrentWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            // Settings navigates without menu highlight; deselect all menu items
            if (tag == "settings")
            {
                sender.SelectedItem = null;
                if (!_pageCache.TryGetValue("settings", out var settingsPage))
                    _pageCache["settings"] = settingsPage = new MacSettingsPage();
                ContentFrame.Content = settingsPage;
                _currentSelection = string.Empty;
                return;
            }

            if (tag == _currentSelection)
                return;

            _currentSelection = tag;
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string page)
    {
        if (_pageCache.TryGetValue(page, out var cached))
        {
            ContentFrame.Content = cached;
            return;
        }

        Page newPage = page switch
        {
            "studyLibrary" => new MacStudyLibraryPage(),
            "aiChat" => new MacAIChatPage(),
            "iPhoneConnection" => new MacIPhoneConnectionPage(),
            "providerDetailTranscription" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.Transcription, "Whisper.cpp"),
            "providerDetailNoteGeneration" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.NoteGeneration, "笔记生成"),
            "providerDetailChat" => CreateProviderDetailPage(
                ProviderDetailCard.ProviderCardKind.Chat, "AI 对话"),
            _ => new MacStudyLibraryPage()
        };

        // Don't cache provider detail pages (they have per-instance configuration)
        if (!page.StartsWith("providerDetail"))
            _pageCache[page] = newPage;

        ContentFrame.Content = newPage;
    }

    private MacProviderDetailPage CreateProviderDetailPage(
        ProviderDetailCard.ProviderCardKind kind, string? name = null)
    {
        var page = new MacProviderDetailPage();
        page.ConfigureFor(kind, name);
        page.NavigateBack += () =>
        {
            if (!_pageCache.TryGetValue("settings", out var settingsPage))
                _pageCache["settings"] = settingsPage = new MacSettingsPage();
            ContentFrame.Content = settingsPage;
        };
        return page;
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Query))
            return;

        // Navigate to study library with search context
        if (_currentSelection != "studyLibrary")
        {
            _currentSelection = "studyLibrary";
            StudyLibraryNavItem.IsSelected = true;
            NavigateTo("studyLibrary");
        }
        // TODO: propagate search query to MacStudyLibraryPage for filtering
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.Trim();
            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                sender.ItemsSource = null;
                return;
            }

            var studyStore = (Application.Current as App)?.Services.GetService<StudyLibraryStore>();
            if (studyStore is null)
            {
                sender.ItemsSource = null;
                return;
            }

            var suggestions = studyStore.AllStudyItems
                .Where(i => !i.IsTrashed && i.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(i => i.Title)
                .ToList();

            sender.ItemsSource = suggestions.Count > 0 ? suggestions : null;
        }
    }
}
