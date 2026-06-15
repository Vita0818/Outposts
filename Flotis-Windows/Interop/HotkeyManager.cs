using System.Runtime.InteropServices;

namespace Flotis.Interop;

public sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _windowHandle;
    private IntPtr _prevWndProc;
    private WndProcDelegate? _wndProcDelegate;
    private IntPtr _wndProcDelegatePtr;
    private bool _isDisposed;

    private const int COMMAND_HOTKEY_START_ID = 3000;
    private const int TOGGLE_PANEL_HOTKEY_ID = 4000;
    private const int TOGGLE_VOICE_HOTKEY_ID = 4001;

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const int VK_R = 0x52;
    private const int VK_0 = 0x30;
    private readonly Dictionary<int, int> _commands = new()
    {
        { 1, 0x31 },
        { 2, 0x32 },
        { 3, 0x33 },
        { 4, 0x34 },
        { 5, 0x35 },
        { 6, 0x36 },
        { 7, 0x37 },
        { 8, 0x38 },
    };

    public Action? OnTogglePanel;
    public Action? OnToggleVoice;
    public Action<int>? OnCommandShortcut;

    public HotkeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public void Start()
    {
        if (_isDisposed) return;

        _wndProcDelegate = WndProc;
        _wndProcDelegatePtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        _prevWndProc = NativeInterop.SetWindowLongPtr(_windowHandle, -4, _wndProcDelegatePtr);

        RegisterHotkey(TOGGLE_PANEL_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_0);
        RegisterHotkey(TOGGLE_VOICE_HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_R);
        foreach (var entry in _commands)
        {
            RegisterHotkey(COMMAND_HOTKEY_START_ID + entry.Key - 1, MOD_CONTROL | MOD_SHIFT, (uint)entry.Value);
        }
    }

    private void RegisterHotkey(int id, uint modifiers, uint virtualKey)
    {
        NativeInterop.RegisterHotKey(_windowHandle, id, modifiers, virtualKey);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();

            if (id == TOGGLE_PANEL_HOTKEY_ID)
            {
                OnTogglePanel?.Invoke();
            }
            else if (id == TOGGLE_VOICE_HOTKEY_ID)
            {
                OnToggleVoice?.Invoke();
            }
            else if (id >= COMMAND_HOTKEY_START_ID && id < COMMAND_HOTKEY_START_ID + 10)
            {
                int index = id - COMMAND_HOTKEY_START_ID + 1;
                OnCommandShortcut?.Invoke(index);
            }
        }

        return NativeInterop.CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var command in _commands.Keys)
        {
            NativeInterop.UnregisterHotKey(_windowHandle, COMMAND_HOTKEY_START_ID + command - 1);
        }
        NativeInterop.UnregisterHotKey(_windowHandle, TOGGLE_PANEL_HOTKEY_ID);
        NativeInterop.UnregisterHotKey(_windowHandle, TOGGLE_VOICE_HOTKEY_ID);

        if (_prevWndProc != IntPtr.Zero)
        {
            NativeInterop.SetWindowLongPtr(_windowHandle, -4, _prevWndProc);
            _prevWndProc = IntPtr.Zero;
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static class NativeInterop
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}
