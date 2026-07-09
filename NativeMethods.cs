using System.Runtime.InteropServices;

namespace WaterReminder;

static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern bool IsZoomed(IntPtr hWnd);

    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;

    /// <summary>用户多久没碰键盘鼠标了。</summary>
    public static TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(unchecked(Environment.TickCount - (int)info.dwTime));
    }

    /// <summary>前台程序是否全屏独占（游戏、全屏视频）。最大化的普通窗口不算。</summary>
    public static bool IsForegroundFullscreen()
    {
        IntPtr fg = GetForegroundWindow();
        if (fg == IntPtr.Zero || fg == GetShellWindow() || fg == GetDesktopWindow()) return false;
        if (IsZoomed(fg)) return false;
        if (!GetWindowRect(fg, out RECT r)) return false;
        var screen = Screen.FromPoint(new Point((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2)).Bounds;
        return r.Left <= screen.Left && r.Top <= screen.Top
            && r.Right >= screen.Right && r.Bottom >= screen.Bottom;
    }
}
