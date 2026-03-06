using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using SeelenWM.Configs;
using SeelenWM.Native;
using SeelenWM.UI;
using SeelenWM.Windows;
using static SeelenWM.Native.NativeMethods;

namespace SeelenWM.Core;

public class WindowManager
{
    private readonly WindowEnumerator _enumerator;
    private readonly HighlightOverlay _overlay;
    private readonly EventHookManager _hookManager;
    private readonly List<IntPtr> _stableWindows = new(); // Stable list for layout
    private const int GAP = 8;

    public WindowManager(
        WindowEnumerator enumerator,
        HighlightOverlay overlay,
        EventHookManager hookManager
    )
    {
        _enumerator = enumerator;
        _overlay = overlay;
        _hookManager = hookManager;

        _hookManager.WindowStateChanged += OnWindowStateChanged;
    }

    public void Start()
    {
        _hookManager.Start();
        Retile(); // Initial layout pass
    }

    public void Stop()
    {
        _hookManager.Dispose();
        _overlay.HideBorder();
    }

    private void OnWindowStateChanged()
    {
        Retile();
    }

    private void Retile()
    {
        // 1. Get current valid windows
        var currentWindows = _enumerator.GetTileableWindows();

        // 2. Sync Stable List
        // Remove closed
        _stableWindows.RemoveAll(h => !currentWindows.Contains(h));
        // Add new (append to end)
        var newlyAdded = new List<IntPtr>();
        foreach (var hwnd in currentWindows)
        {
            if (!_stableWindows.Contains(hwnd))
            {
                _stableWindows.Add(hwnd);
                newlyAdded.Add(hwnd);
            }
        }

        foreach (var hwnd in newlyAdded)
        {
            ForceSetForeground(hwnd);
        }

        // Check foreground window for overlay
        var foreground = GetForegroundWindow();
        bool overlayUpdated = false;

        // 3. Tile
        if (_stableWindows.Count == 0)
        {
            _overlay.HideBorder();
            return;
        }

        // Get Monitor Info (Physical Pixels)
        var hMonitor = MonitorFromWindow(_stableWindows[0], MONITOR_DEFAULTTONEAREST);
        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMonitor, ref monitorInfo);

        var workArea = monitorInfo.rcWork; // Physical Pixels

        bool anyChange = false;
        var moves = new List<(IntPtr hwnd, int x, int y, int w, int h)>();

        if (_stableWindows.Count == 1)
        {
            // Monocle Mode
            var hwnd = _stableWindows[0];
            if (IsZoomed(hwnd))
                ShowWindow(hwnd, SW_RESTORE);

            int x = workArea.Left + GAP;
            int y = workArea.Top + GAP;
            int w = workArea.Width - GAP * 2;
            int h = workArea.Height - GAP * 2;

            // Sync Overlay if this is the foreground window
            if (hwnd == foreground)
            {
                _overlay.ShowBorder(x, y, w, h);
                overlayUpdated = true;
            }

            AdjustForShadow(hwnd, ref x, ref y, ref w, ref h);

            if (NeedsUpdate(hwnd, x, y, w, h))
                anyChange = true;
            moves.Add((hwnd, x, y, w, h));
        }
        else
        {
            // Master + Stack
            int width = workArea.Width / 2;
            int height = workArea.Height;

            // Master (First Window)
            var master = _stableWindows[0];
            if (IsZoomed(master))
                ShowWindow(master, SW_RESTORE);

            int mx = workArea.Left + GAP;
            int my = workArea.Top + GAP;
            int mw = width - GAP * 2;
            int mh = height - GAP * 2;

            if (master == foreground)
            {
                _overlay.ShowBorder(mx, my, mw, mh);
                overlayUpdated = true;
            }

            AdjustForShadow(master, ref mx, ref my, ref mw, ref mh);

            if (NeedsUpdate(master, mx, my, mw, mh))
                anyChange = true;
            moves.Add((master, mx, my, mw, mh));

            // Stack (Rest)
            int stackCount = _stableWindows.Count - 1;
            int stackHeight = height / stackCount;

            for (int i = 1; i < _stableWindows.Count; i++)
            {
                var hwnd = _stableWindows[i];
                if (IsZoomed(hwnd))
                    ShowWindow(hwnd, SW_RESTORE);

                int sx = workArea.Left + width + GAP;
                int sy = (int)workArea.Top + (i - 1) * stackHeight + GAP;
                int sw = width - GAP * 2;
                int sh = stackHeight - GAP * 2;

                if (hwnd == foreground)
                {
                    _overlay.ShowBorder(sx, sy, sw, sh);
                    overlayUpdated = true;
                }

                AdjustForShadow(hwnd, ref sx, ref sy, ref sw, ref sh);

                if (NeedsUpdate(hwnd, sx, sy, sw, sh))
                    anyChange = true;
                moves.Add((hwnd, sx, sy, sw, sh));
            }
        }

        if (anyChange)
        {
            var hdwp = BeginDeferWindowPos(moves.Count);
            if (hdwp != IntPtr.Zero)
            {
                foreach (var m in moves)
                {
                    if (NeedsUpdate(m.hwnd, m.x, m.y, m.w, m.h))
                    {
                        hdwp = DeferWindowPos(
                            hdwp,
                            m.hwnd,
                            IntPtr.Zero,
                            m.x,
                            m.y,
                            m.w,
                            m.h,
                            SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER
                        );
                        if (hdwp == IntPtr.Zero)
                            break;
                    }
                }
                if (hdwp != IntPtr.Zero)
                    EndDeferWindowPos(hdwp);
            }
        }

        if (!overlayUpdated)
        {
            _overlay.HideBorder();
        }
    }

    private void AdjustForShadow(IntPtr hwnd, ref int x, ref int y, ref int w, ref int h)
    {
        GetWindowRect(hwnd, out var rect);
        if (
            DwmGetWindowAttribute(
                hwnd,
                DWMWA_EXTENDED_FRAME_BOUNDS,
                out RECT frame,
                Marshal.SizeOf<RECT>()
            ) == 0
        )
        {
            int leftMargin = frame.Left - rect.Left;
            int topMargin = frame.Top - rect.Top;
            int rightMargin = rect.Right - frame.Right;
            int bottomMargin = rect.Bottom - frame.Bottom;

            x -= leftMargin;
            y -= topMargin;
            w += leftMargin + rightMargin;
            h += topMargin + bottomMargin;
        }
    }

    private bool NeedsUpdate(IntPtr hwnd, int x, int y, int w, int h)
    {
        GetWindowRect(hwnd, out var rect);
        // Allow small tolerance (e.g. 1px) due to potential rounding
        return Math.Abs(rect.Left - x) > 1
            || Math.Abs(rect.Top - y) > 1
            || Math.Abs(rect.Width - w) > 1
            || Math.Abs(rect.Height - h) > 1;
    }

    private void ForceSetForeground(IntPtr hwnd)
    {
        if (IsIconic(hwnd))
        {
            ShowWindow(hwnd, SW_RESTORE);
        }

        IntPtr foreground = GetForegroundWindow();
        if (foreground == hwnd)
            return;

        int foregroundThread = GetWindowThreadProcessId(foreground, IntPtr.Zero);
        int appThread = GetCurrentThreadId();

        bool attached = false;
        if (foregroundThread != appThread && foregroundThread != 0)
        {
            attached = AttachThreadInput(foregroundThread, appThread, true);
        }

        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);

        if (attached)
        {
            AttachThreadInput(foregroundThread, appThread, false);
        }
    }
}
