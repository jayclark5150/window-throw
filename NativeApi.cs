using System.Runtime.InteropServices;

namespace WindowThrow;

internal static class NativeApi
{
    public const int  WM_HOTKEY               = 0x0312;
    public const uint SWP_NOACTIVATE          = 0x0010;
    public const uint SWP_NOZORDER            = 0x0004;
    public const int  SW_SHOWNORMAL           = 1;
    public const int  SW_MAXIMIZE             = 3;
    public const int  SW_RESTORE              = 9;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const uint MOD_ALT                 = 0x0001;
    public const uint MOD_CONTROL             = 0x0002;
    public const uint MOD_SHIFT               = 0x0004;
    public const uint MOD_NOREPEAT            = 0x4000;

    // For GetAsyncKeyState polling
    public const int VK_CONTROL = 0x11;
    public const int VK_MENU    = 0x12; // Alt

    // Virtual key codes for RegisterHotKey
    public const uint VK_BACK   = 0x08;
    public const uint VK_RETURN = 0x0D;
    public const uint VK_SPACE  = 0x20;
    public const uint VK_LEFT   = 0x25;
    public const uint VK_UP     = 0x26;
    public const uint VK_RIGHT  = 0x27;
    public const uint VK_DOWN   = 0x28;
    public const uint VK_C      = 0x43;
    public const uint VK_D      = 0x44;
    public const uint VK_E      = 0x45;
    public const uint VK_F      = 0x46;
    public const uint VK_G      = 0x47;
    public const uint VK_I      = 0x49;
    public const uint VK_J      = 0x4A;
    public const uint VK_K      = 0x4B;
    public const uint VK_T      = 0x54;
    public const uint VK_U      = 0x55;
    public const uint VK_W      = 0x57;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width  => Right  - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int  cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool   SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool   GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool   SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] public static extern bool   ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    [DllImport("user32.dll")] public static extern bool   GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    [DllImport("user32.dll")] public static extern bool   RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool   UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern bool   IsZoomed(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern short  GetAsyncKeyState(int vKey);
}
