using Microsoft.UI.Xaml;

namespace Flotis;

public static class Program
{
    [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [global::System.STAThread]
    public static void Main(string[] args)
    {
        XamlCheckProcessRequirements();

        Application.Start((_) =>
        {
            new App();
        });
    }
}
