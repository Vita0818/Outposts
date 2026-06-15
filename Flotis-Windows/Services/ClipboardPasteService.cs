using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace Flotis.Services;

public sealed class ClipboardPasteService
{
    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int VK_CONTROL = 0x11;
    private const int VK_V = 0x56;

    public static bool CheckPasteCapability()
    {
        try
        {
            var package = new DataPackage();
            package.SetText(string.Empty);
            Clipboard.SetContent(package);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InjectAsync(string text)
    {
        try
        {
            string? previousText = null;
            var current = Clipboard.GetContent();
            if (current != null && current.Contains(StandardDataFormats.Text))
            {
                previousText = await current.GetTextAsync();
            }

            var payload = new DataPackage();
            payload.SetText(text);
            Clipboard.SetContent(payload);
            Clipboard.Flush();

            SendPasteShortcut();
            await Task.Delay(120);

            if (!string.IsNullOrEmpty(previousText))
            {
                var restore = new DataPackage();
                restore.SetText(previousText);
                Clipboard.SetContent(restore);
                Clipboard.Flush();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SendPasteShortcut()
    {
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
}
