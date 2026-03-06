using System.Windows;
using SeelenWM.Configs;
using SeelenWM.Core;
using SeelenWM.UI;
using SeelenWM.Windows;

namespace SeelenWM;

public partial class App : System.Windows.Application
{
    private ConfigLoader? _configLoader;
    private WindowEnumerator? _windowEnumerator;
    private HighlightOverlay? _overlay;
    private EventHookManager? _hookManager;
    private WindowManager? _windowManager;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Load Configs (Default Rules)
        _configLoader = new ConfigLoader();

        // 2. Setup Overlay
        _overlay = new HighlightOverlay();
        // Don't show it yet, WindowManager will handle it

        // 3. Setup Window Manager & Event Hooks
        _hookManager = new EventHookManager();
        _windowEnumerator = new WindowEnumerator(_configLoader);
        _windowManager = new WindowManager(_windowEnumerator, _overlay, _hookManager);
        _windowManager.Start();

        // 4. System Tray (to exit)
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "SeelenWM (Running)",
        };
        _notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, args) => Shutdown());
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _windowManager?.Stop();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
