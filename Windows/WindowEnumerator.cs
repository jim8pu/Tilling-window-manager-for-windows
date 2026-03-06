using System.Text;
using SeelenWM.Configs;
using SeelenWM.Native;
using static SeelenWM.Native.NativeMethods;

namespace SeelenWM.Windows;

public class WindowEnumerator
{
    private readonly ConfigLoader _configLoader;

    public WindowEnumerator(ConfigLoader configLoader)
    {
        _configLoader = configLoader;
    }

    public List<IntPtr> GetTileableWindows()
    {
        var windows = new List<IntPtr>();

        EnumWindows(
            (hwnd, _) =>
            {
                if (ShouldTile(hwnd))
                {
                    windows.Add(hwnd);
                }
                return true;
            },
            IntPtr.Zero
        );

        return windows;
    }

    private bool ShouldTile(IntPtr hwnd)
    {
        // ============================================================
        // PHASE 1: Basic Visibility Checks
        // ============================================================
        if (!IsWindowVisible(hwnd))
            return false;
        if (IsIconic(hwnd))
            return false;
        if (IsCloaked(hwnd))
            return false;

        var style = GetWindowLongW(hwnd, GWL_STYLE);
        var exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);

        if ((style & WS_VISIBLE) == 0)
            return false;
        if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            return false;
        if ((exStyle & WS_EX_NOACTIVATE) != 0)
            return false;
        if (GetParent(hwnd) != IntPtr.Zero && GetAncestor(hwnd, GA_ROOT) != hwnd)
            return false;

        // ============================================================
        // PHASE 2: Get Window Information
        // ============================================================
        var length = GetWindowTextLengthW(hwnd);
        var sbTitle = new StringBuilder(length + 1);
        GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
        var title = sbTitle.ToString();

        var sbClass = new StringBuilder(256);
        GetClassName(hwnd, sbClass, sbClass.Capacity);
        var className = sbClass.ToString();

        GetWindowThreadProcessId(hwnd, out int pid);
        string exeName = "";
        string fullPath = "";
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            exeName = process.ProcessName + ".exe";
            try
            {
                fullPath = process.MainModule?.FileName ?? "";
            }
            catch { }
        }
        catch { }

        // ============================================================
        // PHASE 3: Configuration Rules (Highest Priority)
        // ============================================================
        var match = _configLoader.FindMatch(title, className, exeName, fullPath);
        if (match != null)
        {
            if (match.Options.Contains(AppExtraFlag.WmUnmanage))
                return false;
            if (match.Options.Contains(AppExtraFlag.WmForce))
                return true;
        }

        // ============================================================
        // PHASE 4: Universal Popup Detection Heuristics
        // ============================================================

        // 4.1: Standard Win32 Dialog Box Class
        // Dialog boxes created with DialogBox/CreateDialog use the special class "#32770"
        if (className == "#32770")
            return false;

        // 4.2: Owner Window Check
        // Windows with an owner are typically modal/modeless dialogs spawned by another window
        IntPtr owner = GetWindow(hwnd, GW_OWNER);
        if (owner != IntPtr.Zero && owner != GetDesktopWindow())
            return false;

        // Check for Root Owner (catches modal dialogs even if direct owner is hidden)
        IntPtr rootOwner = GetAncestor(hwnd, GA_ROOTOWNER);
        if (rootOwner != IntPtr.Zero && rootOwner != hwnd && rootOwner != GetDesktopWindow())
            return false;

        // 4.3: Dialog Frame Styles
        // WS_DLGFRAME = thick border without title bar (classic dialog look)
        // WS_EX_DLGMODALFRAME = double border (modal dialog indicator)
        bool hasDlgFrame = (style & WS_DLGFRAME) != 0 && (style & WS_CAPTION) == 0;
        bool hasModalFrame = (exStyle & WS_EX_DLGMODALFRAME) != 0;
        if (hasDlgFrame || hasModalFrame)
            return false;

        // ============================================================
        // PHASE 5: UWP Special Handling
        // ============================================================
        if (className == "ApplicationFrameWindow")
        {
            // UWP apps use this wrapper. If visible and not cloaked, it's a real app.
            return true;
        }

        // ============================================================
        // PHASE 6: Standard Window Style Checks
        // ============================================================
        bool isResizable = (style & WS_THICKFRAME) != 0;
        bool isAppWindow = (exStyle & WS_EX_APPWINDOW) != 0;
        bool isTopmost = (exStyle & WS_EX_TOPMOST) != 0;
        bool canMaximize = (style & WS_MAXIMIZEBOX) != 0;
        bool canMinimize = (style & WS_MINIMIZEBOX) != 0;
        bool isPopup = (style & WS_POPUP) != 0;

        // 6.1: Topmost Check
        // Always-on-top windows that aren't resizable main windows are typically overlays
        if (isTopmost && !isResizable)
            return false;

        // 6.2: Popup Window Checks
        // WS_POPUP windows without minimize button are almost always dialogs/menus
        if (isPopup && !canMinimize)
            return false;

        // 6.3: Non-resizable Popup = Dialog/Menu
        // Popup windows that can't be resized are definitely not main app windows
        if (isPopup && !isResizable)
            return false;

        // 6.4: Size-Based Filtering (Critical for dialogs)
        GetWindowRect(hwnd, out RECT rect);
        int width = rect.Width;
        int height = rect.Height;

        // Very small windows are almost certainly popups/tooltips
        // (thresholds increased for high DPI screens where physical pixels > logical)
        if (width < 500 && height < 400)
            return false;

        // 6.5: NON-RESIZABLE SIZE CHECK (Ignores AppWindow flag!)
        // Non-resizable windows under a certain size are ALWAYS dialogs
        // This catches File Explorer dialogs, About boxes, etc.
        // Even with WS_EX_APPWINDOW, small non-resizable windows shouldn't be tiled
        if (!isResizable && (width < 800 || height < 600))
            return false;

        // 6.6: Medium windows without minimize button are likely dialogs
        if (width < 700 && height < 600 && !canMinimize)
            return false;

        // 6.7: Non-Maximizable + Non-Resizable = Fixed Utility Window
        if (!canMaximize && !isResizable)
            return false;

        // 6.8: No Minimize Button + Non-Resizable = Dialog
        if (!canMinimize && !isResizable)
            return false;

        // 6.X: Universal Tiling Check
        // A window must be maximizable to be tiled.
        // Tiling is essentially "maximizing into a region". If a window doesn't support maximization,
        // it likely wasn't designed to be resized arbitrarily or managed by a tiling WM.
        // This catches "Folder In Use" and other fixed-size dialogs that might report as resizable.
        if (!canMaximize)
            return false;

        // 6.X: Modal Frame Check
        if ((exStyle & WS_EX_DLGMODALFRAME) != 0)
            return false;

        // 6.9: Resizability as Final Filter
        // Main application windows are almost always resizable
        // Only accept non-resizable if it has WS_EX_APPWINDOW AND is large enough
        if (!isResizable && !isAppWindow)
            return false;

        return true;
    }

    private bool IsCloaked(IntPtr hwnd)
    {
        int cloaked;
        DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
        return cloaked != 0;
    }
}
