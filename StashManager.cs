using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowThrow;

internal enum StashEdge { Left, Right, Top, Bottom }

internal sealed class StashManager : IDisposable
{
    private record StashedWindow(IntPtr Hwnd, NativeApi.RECT OrigRect, StashEdge Edge, int ScreenIndex);

    private readonly List<StashedWindow>       _stashed = new();
    private readonly System.Windows.Forms.Timer _timer;
    private const int TabPx     = 8; // pixels left visible at screen edge
    private const int TriggerPx = 3; // cursor distance from edge to reveal

    public StashManager()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 80 };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stash(StashEdge edge)
    {
        var hwnd = NativeApi.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || _stashed.Any(s => s.Hwnd == hwnd)) return;
        if (!NativeApi.GetWindowRect(hwnd, out var r)) return;

        var screens = Screen.AllScreens;
        var screen  = Screen.FromHandle(hwnd);
        int idx     = Array.IndexOf(screens, screen);
        var b       = screen.Bounds;

        int newX = r.Left, newY = r.Top;
        switch (edge)
        {
            case StashEdge.Left:   newX = b.Left   - r.Width  + TabPx; break;
            case StashEdge.Right:  newX = b.Right  - TabPx;            break;
            case StashEdge.Top:    newY = b.Top    - r.Height + TabPx; break;
            case StashEdge.Bottom: newY = b.Bottom - TabPx;            break;
        }

        _stashed.Add(new StashedWindow(hwnd, r, edge, idx));
        NativeApi.ShowWindow(hwnd, NativeApi.SW_SHOWNORMAL);
        NativeApi.SetWindowPos(hwnd, IntPtr.Zero, newX, newY, r.Width, r.Height,
            NativeApi.SWP_NOZORDER | NativeApi.SWP_NOACTIVATE);
    }

    public void UnstashAll()
    {
        foreach (var s in _stashed.ToList()) Reveal(s);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_stashed.Count == 0) return;
        var cur     = Cursor.Position;
        var screens = Screen.AllScreens;

        foreach (var s in _stashed.ToList())
        {
            if (s.ScreenIndex >= screens.Length) continue;
            var b = screens[s.ScreenIndex].Bounds;

            bool trigger = s.Edge switch
            {
                StashEdge.Left   => cur.X <= b.Left   + TriggerPx && cur.Y >= b.Top  && cur.Y <= b.Bottom,
                StashEdge.Right  => cur.X >= b.Right  - TriggerPx && cur.Y >= b.Top  && cur.Y <= b.Bottom,
                StashEdge.Top    => cur.Y <= b.Top    + TriggerPx && cur.X >= b.Left && cur.X <= b.Right,
                StashEdge.Bottom => cur.Y >= b.Bottom - TriggerPx && cur.X >= b.Left && cur.X <= b.Right,
                _ => false
            };

            if (trigger) Reveal(s);
        }
    }

    private void Reveal(StashedWindow s)
    {
        _stashed.Remove(s);
        var r = s.OrigRect;
        NativeApi.ShowWindow(s.Hwnd, NativeApi.SW_SHOWNORMAL);
        NativeApi.SetWindowPos(s.Hwnd, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height,
            NativeApi.SWP_NOZORDER | NativeApi.SWP_NOACTIVATE);
        NativeApi.SetForegroundWindow(s.Hwnd);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
