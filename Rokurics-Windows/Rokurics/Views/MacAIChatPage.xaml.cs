using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Rokurics.Models;
using Rokurics.Services;
using Rokurics.ViewModels;

namespace Rokurics.Views;

/// <summary>
/// AI Chat page matching MacAIChatView from Apple source.
/// Uses ChatViewModel for state management with real provider support.
/// </summary>
public sealed partial class MacAIChatPage : Page
{
    private readonly ChatViewModel _viewModel;

    public string Greeting => _viewModel.GreetingText;

    public MacAIChatPage()
    {
        InitializeComponent();

        _viewModel = App.Current.Services.GetService<ChatViewModel>()
            ?? new ChatViewModel();
        _viewModel.PropertyChanged += (_, _) => RefreshUI();

        Loaded += (_, _) => RefreshUI();
    }

    private void RefreshUI()
    {
        RenderMessages();
        RefreshRecentList();
        GreetingText.Visibility = _viewModel.ActiveMessages.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshRecentList()
    {
        ConversationsList.ItemsSource = null;
        ConversationsList.ItemsSource = _viewModel.Conversations;
    }

    private void RenderMessages()
    {
        MessagesList.ItemsSource = null;
        var messages = _viewModel.ActiveMessages;
        if (messages.Count == 0) return;

        var stack = new StackPanel { Spacing = 16 };
        foreach (var msg in messages)
        {
            stack.Children.Add(CreateMessageBubble(msg));
        }
        MessagesList.ItemsSource = new List<FrameworkElement> { stack };
    }

    private FrameworkElement CreateMessageBubble(ChatMessage msg)
    {
        var isUser = msg.Role == ChatMessageRole.User;
        var border = new Border
        {
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(14, 10, 14, 10),
            MaxWidth = 600,
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = msg.Content,
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
                Foreground = isUser
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : (SolidColorBrush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            }
        };

        if (isUser)
        {
            // User bubble: Mac actionGradient (aqua -> mint)
            border.Background = (LinearGradientBrush)Application.Current.Resources["RokuricsActionGradientBrush"];
        }
        else
        {
            // Assistant bubble: semi-transparent grey matching Mac
            border.Background = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128));
        }

        var container = new Grid();
        var spacer = new Border { Width = 80, HorizontalAlignment = isUser ? HorizontalAlignment.Left : HorizontalAlignment.Right };

        var hStack = new StackPanel { Orientation = Orientation.Horizontal };
        if (isUser)
        {
            hStack.Children.Add(spacer);
            hStack.Children.Add(border);
        }
        else
        {
            hStack.Children.Add(border);
            hStack.Children.Add(spacer);
        }
        container.Children.Add(hStack);
        return container;
    }

    // ── Toolbar ──────────────────────────────────────────────────

    private void ToggleRecent_Click(object sender, RoutedEventArgs e)
    {
        RecentPanel.Visibility = RecentPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NewConversationCommand.Execute(null);
        MessageInput.Text = "";
        ErrorBanner.Visibility = Visibility.Collapsed;
        GreetingText.Visibility = Visibility.Visible;
    }

    private void OnConversationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationsList.SelectedItem is ChatConversation conv)
        {
            _viewModel.SelectConversationCommand.Execute(conv);
            RecentPanel.Visibility = Visibility.Collapsed;
            RefreshUI();
        }
    }

    // ── Send ─────────────────────────────────────────────────────

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var text = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(text) || _viewModel.IsGenerating) return;

        MessageInput.Text = "";
        LoadingBar.Visibility = Visibility.Visible;
        ErrorBanner.Visibility = Visibility.Collapsed;

        _viewModel.DraftMessage = text;
        await _viewModel.SendMessageCommand.ExecuteAsync(null);

        LoadingBar.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
        {
            ErrorBanner.Visibility = Visibility.Visible;
            if (ErrorBanner.Child is TextBlock tb)
                tb.Text = _viewModel.ErrorMessage;
        }

        RefreshUI();
    }

    // ── Attach / import ──────────────────────────────────────────

    private void Attach_Click(object sender, RoutedEventArgs e)
    {
        // Show 3-option attachment menu matching MacAIChatAttachmentMenu
        var menu = new MenuFlyout();

        var studyItem = new MenuFlyoutItem
        {
            Text = "导入学习库内容",
            Icon = new FontIcon { Glyph = "", FontSize = 13 }
        };
        studyItem.Click += async (_, _) => await ShowStudyLibraryPicker();
        menu.Items.Add(studyItem);

        var fileItem = new MenuFlyoutItem
        {
            Text = "上传文件",
            Icon = new FontIcon { Glyph = "", FontSize = 13 }
        };
        fileItem.Click += (_, _) =>
        {
            // File picker — stub on non-Windows host
        };
        menu.Items.Add(fileItem);

        var imageItem = new MenuFlyoutItem
        {
            Text = "上传图片",
            Icon = new FontIcon { Glyph = "", FontSize = 13 }
        };
        imageItem.Click += (_, _) =>
        {
            // Image picker — stub on non-Windows host
        };
        menu.Items.Add(imageItem);

        menu.ShowAt(AttachButton);
    }

    private async Task ShowStudyLibraryPicker()
    {
        var studyStore = _viewModel.StudyStore;
        studyStore.Refresh();
        var allItems = studyStore.AllStudyItems.Where(i => !i.IsTrashed).ToList();

        if (allItems.Count == 0)
        {
            await new ContentDialog
            {
                Title = "提示",
                Content = "学习库中没有可导入的内容。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            }.ShowAsync();
            return;
        }

        var panel = new StackPanel { Spacing = 14, Width = 520 };

        // Picker header
        panel.Children.Add(new TextBlock
        {
            Text = "选择要导入 AI 对话的学习内容",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        // Browse path indicator
        var browsePanel = new StackPanel { Spacing = 8 };

        // Item list with checkboxes
        var itemList = new ListView
        {
            MaxHeight = 350,
            SelectionMode = ListViewSelectionMode.Multiple
        };

        var displayItems = allItems.Select(item => new StudyPickerItem
        {
            StudyItem = item,
            DisplayText = $"{item.Title}  [{item.FilingPath.DisplaySummary}]  {item.CreatedAt:yyyy-MM-dd}"
        }).ToList();

        itemList.ItemsSource = displayItems;

        browsePanel.Children.Add(itemList);

        // Action info
        var infoBlock = new TextBlock
        {
            Text = $"共 {allItems.Count} 项，可多选。选中的学习资料将作为上下文导入当前对话。",
            FontSize = 12,
            Opacity = 0.5,
            TextWrapping = TextWrapping.Wrap
        };
        browsePanel.Children.Add(infoBlock);

        panel.Children.Add(browsePanel);

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 420,
            Content = panel
        };

        var dialog = new ContentDialog
        {
            Title = "导入学习库内容",
            Content = scrollViewer,
            PrimaryButtonText = "导入",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var selectedItems = itemList.SelectedItems
                .OfType<StudyPickerItem>()
                .Select(sp => sp.StudyItem)
                .ToList();

            if (selectedItems.Count == 0)
                return;

            ImportStudyItemsToChat(selectedItems);
        }
    }

    private async void ImportStudyItemsToChat(List<StudyItemMetadata> items)
    {
        var context = new ChatContext
        {
            Id = Guid.NewGuid().ToString(),
            Title = "学习库",
            Items = items.Select(item => new ChatContextItem(
                item.ItemId,
                item.Title,
                item.FilingPath.DisplaySummary,
                string.Empty)).ToList(),
            BrowsePathComponents = new List<string>(),
            ItemCount = items.Count
        };

        var contextText = "已导入学习库上下文：\n\n" +
            string.Join("\n", items.Select(ci =>
                $"- {ci.Title}（{ci.FilingPath.DisplaySummary}）"));

        await new ContentDialog
        {
            Title = "已导入",
            Content = contextText,
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    // ── Delete conversation ──────────────────────────────────────

    private async void DeleteConversation_Click(object sender, RoutedEventArgs e)
    {
        if (ConversationsList.SelectedItem is not ChatConversation conv) return;

        var dialog = new ContentDialog
        {
            Title = "删除对话",
            Content = $"确定要删除「{conv.Title}」吗？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _viewModel.DeleteConversationCommand.Execute(conv);
            RefreshUI();
        }
    }
}

/// <summary>
/// Display wrapper for study library picker items.
/// </summary>
internal sealed class StudyPickerItem
{
    public StudyItemMetadata StudyItem { get; set; } = null!;
    public string DisplayText { get; set; } = "";
    public override string ToString() => DisplayText;
}
