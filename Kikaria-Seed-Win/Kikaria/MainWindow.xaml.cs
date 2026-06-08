using Kikaria.Views;
using Microsoft.UI.Xaml;

namespace Kikaria
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            Title = "Kikaria";
            MainFrame.Navigate(typeof(HomePage));
        }
    }
}