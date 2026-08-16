using Intatis.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Intatis.App.Views;

public sealed partial class CodePage : Page
{
    public CodeViewModel ViewModel { get; }

    public CodePage()
    {
        InitializeComponent();
        ViewModel = new CodeViewModel(App.Current.Environment,
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        ViewModel.Items.CollectionChanged += (_, _) => AutoScroll();
    }

    private void AutoScroll()
    {
        if (ViewModel.Items.Count > 0)
            Transcript.ScrollIntoView(ViewModel.Items[^1]);
    }

    private async void OnChooseWorkspaceClick(object sender, RoutedEventArgs e)
    {
        var path = await PickFolderAsync("Choose the Code workspace");
        if (path is { Length: > 0 }) ViewModel.SetWorkspace(path);
    }

    private void OnNewSessionClick(object sender, RoutedEventArgs e) => ViewModel.StartNewSession();

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

    internal static async Task<string?> PickFolderAsync(string prompt)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder,
            };
            picker.FileTypeFilter.Add("*");
            if (App.MainWindow is { } window)
            {
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);
            }
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
