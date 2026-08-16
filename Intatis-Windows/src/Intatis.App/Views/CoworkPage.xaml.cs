using Intatis.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Intatis.App.Views;

public sealed partial class CoworkPage : Page
{
    public CoworkViewModel ViewModel { get; }

    public CoworkPage()
    {
        InitializeComponent();
        ViewModel = new CoworkViewModel(App.Current.Environment,
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        ViewModel.Thread.CollectionChanged += (_, _) => AutoScroll();
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CoworkViewModel.SelectedAgent)) AutoScroll();
        };
        Loaded += (_, _) => MaybeStartSession();
    }

    private bool _sessionOffered;

    private async void MaybeStartSession()
    {
        if (_sessionOffered || ViewModel.Workspace.Length > 0) return;
        _sessionOffered = true;
        var path = await PickFolderPromptAsync();
        if (path is { Length: > 0 })
            ViewModel.StartNewSession(path);
    }

    private void AutoScroll()
    {
        if (ViewModel.Thread.Count > 0)
            AgentThread.ScrollIntoView(ViewModel.Thread[^1]);
    }

    private async void OnNewSessionClick(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderPromptAsync();
        if (path is { Length: > 0 })
            ViewModel.StartNewSession(path);
    }

    private static async Task<string?> PickFolderPromptAsync()
        => await PickFolderAsync("Choose the Cowork workspace");

    private async void OnAddAgentClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Workspace.Length == 0)
        {
            var path = await PickFolderPromptAsync();
            if (path is { Length: > 0 }) ViewModel.StartNewSession(path);
            if (ViewModel.Workspace.Length == 0) return;
        }

        var nameBox = new TextBox
        {
            Header = "Agent name",
            PlaceholderText = "writer",
        };
        var workspaceBox = new TextBox
        {
            Header = "Workspace (empty = session workspace)",
            PlaceholderText = ViewModel.Workspace,
        };
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(nameBox);
        panel.Children.Add(workspaceBox);

        var dialog = new ContentDialog
        {
            Title = "Add agent",
            Content = panel,
            PrimaryButtonText = "Attach",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() is not ContentDialogResult.Primary) return;

        var name = nameBox.Text.Trim();
        if (name.Length == 0) return;
        var workspace = workspaceBox.Text.Trim().Length > 0 ? workspaceBox.Text.Trim() : ViewModel.Workspace;
        if (!ViewModel.AddAgent(name, workspace))
        {
            ViewModel.ErrorText = $"cannot attach @{name} (duplicate or reserved name)";
        }
    }

    private void OnAgentSelected(object sender, SelectionChangedEventArgs e)
    {
        // Selection already flows to the ViewModel via TwoWay binding.
    }

    private void OnSendClick(object sender, RoutedEventArgs e) => ViewModel.Send();

    private void OnStopClick(object sender, RoutedEventArgs e) => ViewModel.Stop();

    private void OnApproveClick(object sender, RoutedEventArgs e) => ViewModel.SettlePermission(allow: true);

    private void OnDeclineClick(object sender, RoutedEventArgs e) => ViewModel.SettlePermission(allow: false);

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not Windows.System.VirtualKey.Enter) return;
        var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
        if (shift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = false;
            return;
        }
        e.Handled = true;
        ViewModel.Send();
    }
}
