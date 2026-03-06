using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SeelenWM.Native;
using static SeelenWM.Native.NativeMethods;

namespace SeelenWM.Core;

public class EventHookManager : IDisposable
{
    private IntPtr _shellHookWindow;
    private HwndSource? _hwndSource;
    private int _shellHookMessage;

    private IntPtr _hookCreate;
    private IntPtr _hookDestroy;
    private IntPtr _hookShowHide;
    private IntPtr _hookMinimize;
    private IntPtr _hookMoveSize;
    private IntPtr _hookForeground;

    // Keep reference to delegate so it is not garbage collected
    private readonly WinEventDelegate _winEventDelegate;

    public event Action? WindowStateChanged;

    public EventHookManager()
    {
        _winEventDelegate = WinEventCallback;
    }

    public void Start()
    {
        // 1. Setup WinEvent Hooks
        _hookCreate = SetWinEventHook(
            EVENT_OBJECT_CREATE,
            EVENT_OBJECT_CREATE,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );
        _hookDestroy = SetWinEventHook(
            EVENT_OBJECT_DESTROY,
            EVENT_OBJECT_DESTROY,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );
        _hookShowHide = SetWinEventHook(
            EVENT_OBJECT_SHOW,
            EVENT_OBJECT_HIDE,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );
        _hookMinimize = SetWinEventHook(
            EVENT_SYSTEM_MINIMIZESTART,
            EVENT_SYSTEM_MINIMIZEEND,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );
        _hookMoveSize = SetWinEventHook(
            EVENT_SYSTEM_MOVESIZESTART,
            EVENT_SYSTEM_MOVESIZEEND,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );
        _hookForeground = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _winEventDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS
        );

        // 2. Setup Shell Hooks
        _shellHookMessage = RegisterWindowMessage("SHELLHOOK");

        var parameters = new HwndSourceParameters("SeelenWM_ShellHookWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = (int)(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT),
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _shellHookWindow = _hwndSource.Handle;

        RegisterShellHookWindow(_shellHookWindow);
    }

    private void WinEventCallback(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime
    )
    {
        if (idObject != OBJID_WINDOW)
            return;

        if (hwnd == IntPtr.Zero)
            return;

        if (
            eventType == EVENT_OBJECT_CREATE
            || eventType == EVENT_OBJECT_DESTROY
            || eventType == EVENT_OBJECT_SHOW
            || eventType == EVENT_OBJECT_HIDE
            || eventType == EVENT_SYSTEM_MINIMIZESTART
            || eventType == EVENT_SYSTEM_MINIMIZEEND
            || eventType == EVENT_SYSTEM_MOVESIZEEND
            || eventType == EVENT_SYSTEM_FOREGROUND
        )
        {
            TriggerEvent();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _shellHookMessage)
        {
            int shellEvent = wParam.ToInt32() & 0x7FFF; // Handle lower byte if needed, though typically wParam is exact
            if (
                shellEvent == HSHELL_WINDOWCREATED
                || shellEvent == HSHELL_WINDOWDESTROYED
                || shellEvent == HSHELL_WINDOWREPLACED
                || shellEvent == HSHELL_WINDOWACTIVATED
            )
            {
                TriggerEvent();
            }
        }
        return IntPtr.Zero;
    }

    private System.Threading.Timer? _debounceTimer;

    private void TriggerEvent()
    {
        _debounceTimer?.Dispose();

        // Trigger action after 50ms of quiet time
        _debounceTimer = new System.Threading.Timer(
            _ =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WindowStateChanged?.Invoke();
                });
            },
            null,
            50,
            System.Threading.Timeout.Infinite
        );
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();

        if (_hookCreate != IntPtr.Zero)
            UnhookWinEvent(_hookCreate);
        if (_hookDestroy != IntPtr.Zero)
            UnhookWinEvent(_hookDestroy);
        if (_hookShowHide != IntPtr.Zero)
            UnhookWinEvent(_hookShowHide);
        if (_hookMinimize != IntPtr.Zero)
            UnhookWinEvent(_hookMinimize);
        if (_hookMoveSize != IntPtr.Zero)
            UnhookWinEvent(_hookMoveSize);
        if (_hookForeground != IntPtr.Zero)
            UnhookWinEvent(_hookForeground);

        if (_shellHookWindow != IntPtr.Zero)
        {
            DeregisterShellHookWindow(_shellHookWindow);
            _hwndSource?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
