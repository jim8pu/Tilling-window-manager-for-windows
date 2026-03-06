using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static SeelenWM.Native.NativeMethods;

namespace SeelenWM.UI;

public class HighlightOverlay : Window
{
    private readonly Border _border;
    private const int THICKNESS = 5;

    public HighlightOverlay()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        IsHitTestVisible = false; // Click-through

        _border = new Border
        {
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)), // Windows Blue
            BorderThickness = new Thickness(THICKNESS),
            CornerRadius = new CornerRadius(0),
            SnapsToDevicePixels = true,
        };
        Content = _border;
    }

    private double _dpiScaleX = 1.0;
    private double _dpiScaleY = 1.0;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Get DPI Scale
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
        }

        // Make it truly transparent to input (WS_EX_TRANSPARENT)
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLongW(hwnd, GWL_EXSTYLE);
        SetWindowLongW(hwnd, GWL_EXSTYLE, exStyle | (int)WS_EX_TRANSPARENT | (int)WS_EX_TOOLWINDOW);
    }

    private int _lastX,
        _lastY,
        _lastW,
        _lastH;

    public void ShowBorder(int x, int y, int w, int h)
    {
        // Prevent redundant updates (Anti-Flicker)
        if (
            x == _lastX
            && y == _lastY
            && w == _lastW
            && h == _lastH
            && Visibility == Visibility.Visible
        )
            return;

        _lastX = x;
        _lastY = y;
        _lastW = w;
        _lastH = h;

        // Adjust for thickness so it outlines the window
        // But for simplicity, let's just draw ON TOP of the window rect
        // Convert Physical Pixels (Win32) -> Logical Pixels (WPF)

        // We want to wrap AROUND the window, so we inflate by THICKNESS
        // Note: x,y,w,h are Physical. THICKNESS is Logical (WPF).
        // We should convert x/y/w/h to logical first, then inflate.

        // Fix: Set WPF properties so the layout engine knows the window size.
        // These use LOGICAL pixels.
        double logicalX = x / _dpiScaleX;
        double logicalY = y / _dpiScaleY;
        double logicalW = w / _dpiScaleX;
        double logicalH = h / _dpiScaleY;

        Left = logicalX - THICKNESS;
        Top = logicalY - THICKNESS;
        Width = logicalW + (THICKNESS * 2);
        Height = logicalH + (THICKNESS * 2);

        if (Visibility != Visibility.Visible)
            Show();

        // Fix: Use SetWindowPos to enforce Z-Order (Top of non-topmost windows)
        // This uses PHYSICAL pixels.
        // HWND_TOP (0) places it at the top of the Z-order
        int physicalThicknessX = (int)(THICKNESS * _dpiScaleX);
        int physicalThicknessY = (int)(THICKNESS * _dpiScaleY);

        SetWindowPos(
            new WindowInteropHelper(this).Handle,
            (IntPtr)0,
            x - physicalThicknessX,
            y - physicalThicknessY,
            w + (physicalThicknessX * 2),
            h + (physicalThicknessY * 2),
            SWP_NOACTIVATE | SWP_SHOWWINDOW
        );
    }

    public void HideBorder()
    {
        if (Visibility == Visibility.Visible)
            Hide();
    }
}
