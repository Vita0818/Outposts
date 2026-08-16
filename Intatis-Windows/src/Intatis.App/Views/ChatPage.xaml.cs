using Intatis.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Intatis.App.Views;

public sealed partial class ChatPage : Page
{
    public ChatViewModel ViewModel { get; }

    public ChatPage()
    {
        InitializeComponent();
        ViewModel = new ChatViewModel(App.Current.Environment,
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        ViewModel.Messages.CollectionChanged += (_, _) => AutoScroll();
    }

    private void AutoScroll()
    {
        if (ViewModel.Messages.Count > 0)
            MessagesList.ScrollIntoView(ViewModel.Messages[^1]);
    }

    private void OnSendClick(object sender, RoutedEventArgs e) => ViewModel.Send();

    private void OnStopClick(object sender, RoutedEventArgs e) => ViewModel.Stop();

    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not Windows.System.VirtualKey.Enter) return;
        var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
        if (shift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            e.Handled = false; // Shift+Enter inserts a newline in the TextBox.
            return;
        }
        e.Handled = true;
        ViewModel.Send();
    }
}
