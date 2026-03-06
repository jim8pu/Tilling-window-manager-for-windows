using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SeelenWM.Native;

public static partial class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime
    );

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWinEvent(IntPtr hWinEventHook);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterShellHookWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeregisterShellHookWindow(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int RegisterWindowMessage(string lpString);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsZoomed(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetParent(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetShellWindow();

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetDesktopWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [LibraryImport("user32.dll")]
    public static partial int GetWindowTextLengthW(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    public static partial int GetWindowLongW(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll")]
    public static partial int SetWindowLongW(IntPtr hWnd, int nIndex, int dwNewLong);

    [LibraryImport("user32.dll")]
    public static partial int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [LibraryImport("user32.dll")]
    public static partial int GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [LibraryImport("kernel32.dll")]
    public static partial int GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(
        int idAttach,
        int idAttachTo,
        [MarshalAs(UnmanagedType.Bool)] bool fAttach
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BringWindowToTop(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out int pvAttribute,
        int cbAttribute
    );

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmGetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        out RECT pvAttribute,
        int cbAttribute
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    [LibraryImport("user32.dll")]
    public static partial IntPtr BeginDeferWindowPos(int nNumWindows);

    [LibraryImport("user32.dll")]
    public static partial IntPtr DeferWindowPos(
        IntPtr hWinPosInfo,
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EndDeferWindowPos(IntPtr hWinPosInfo);

    [LibraryImport("user32.dll")]
    public static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left,
            Top,
            Right,
            Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const uint WS_VISIBLE = 0x10000000;
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_CHILD = 0x40000000;
    public const uint WS_CAPTION = 0x00C00000;
    public const uint WS_THICKFRAME = 0x00040000;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_APPWINDOW = 0x00040000;
    public const uint WS_EX_TRANSPARENT = 0x00000020;
    public const uint WS_EX_NOACTIVATE = 0x08000000;
    public const uint WS_EX_TOPMOST = 0x00000008;
    public const uint WS_EX_DLGMODALFRAME = 0x00000001;
    public const uint WS_MAXIMIZEBOX = 0x00010000;
    public const uint WS_MINIMIZEBOX = 0x00020000;
    public const uint WS_DLGFRAME = 0x00400000;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOOWNERZORDER = 0x0200;
    public const uint SWP_SHOWWINDOW = 0x0040;

    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    public const int DWMWA_CLOAKED = 14;

    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int SW_RESTORE = 9;

    public const uint GA_ROOT = 2;
    public const uint GA_ROOTOWNER = 3;
    public const uint GW_OWNER = 4;

    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
    public const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
    public const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
    public const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
    public const uint EVENT_OBJECT_CREATE = 0x8000;
    public const uint EVENT_OBJECT_DESTROY = 0x8001;
    public const uint EVENT_OBJECT_SHOW = 0x8002;
    public const uint EVENT_OBJECT_HIDE = 0x8003;
    public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    public const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    public const int HSHELL_WINDOWCREATED = 1;
    public const int HSHELL_WINDOWDESTROYED = 2;
    public const int HSHELL_ACTIVATESHELLWINDOW = 3;
    public const int HSHELL_WINDOWACTIVATED = 4;
    public const int HSHELL_GETMINRECT = 5;
    public const int HSHELL_REDRAW = 6;
    public const int HSHELL_TASKMAN = 7;
    public const int HSHELL_LANGUAGE = 8;
    public const int HSHELL_SYSMENU = 9;
    public const int HSHELL_ENDTASK = 10;
    public const int HSHELL_ACCESSIBILITYSTATE = 11;
    public const int HSHELL_APPCOMMAND = 12;
    public const int HSHELL_WINDOWREPLACED = 13;

    public const int OBJID_WINDOW = 0;
}
