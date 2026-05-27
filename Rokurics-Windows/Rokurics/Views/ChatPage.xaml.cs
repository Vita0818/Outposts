using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.ViewModels;

namespace Rokurics.Views;

public sealed partial class ChatPage : Page
{
    internal ChatViewModel ViewModel { get; }

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.SendMessageCommand.ExecuteAsync(null);
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NewConversationCommand.Execute(null);
    }

    private void ToggleConversationList_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleConversationListCommand.Execute(null);
    }
}
