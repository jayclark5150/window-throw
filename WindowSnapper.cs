using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowThrow;

internal static class WindowSnapper
{
    // Pressing the same hotkey repeatedly steps through these chains
    private static readonly Dictionary<SnapPosition, SnapPosition[]> _chains = new()
    {
        [SnapPosition.LeftHalf]  = [SnapPosition.LeftHalf,  SnapPosition.LeftThird,  SnapPosition.LeftTwoThirds],
        [SnapPosition.RightHalf] = [SnapPosition.RightHalf, SnapPosition.RightThird, SnapPosition.RightTwoThirds],
    };

    private record CycleState(SnapPosition Key, int Index, long Ticks);
    private static readonly Dictionary<IntPtr, CycleState>     _cycle      = new();
    private static readonly Dictionary<IntPtr, NativeApi.RECT> _savedRects = new();

    // Called from hotkey — resolves cycling, uses foreground window
    public static void Snap(SnapPosition pos)
    {
        var hwnd = NativeApi.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;
        SnapWindow(hwnd, ResolveCycle(hwnd, pos));
    }

    // Called from ThrowOverlay — explicit hwnd, no cycling
    public static void SnapDirect(SnapPosition pos, IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        SnapWindow(hwnd, pos);
    }

    private static SnapPosition ResolveCycle(IntPtr hwnd, SnapPosition intent)
    {
        if (!_chains.TryGetValue(intent, out var chain)) return intent;

        long now = Environment.TickCount64;
        if (_cycle.TryGetValue(hwnd, out var s) && s.Key == intent && now - s.Ticks < 2000)
        {
            int next = (s.Index + 1) % chain.Length;
            _cycle[hwnd] = new CycleState(intent, next, now);
            return chain[next];
        }

        _cycle[hwnd] = new CycleState(intent, 0, now);
        return chain[0];
    }

    private static void SnapWindow(IntPtr hwnd, SnapPosition pos)
    {
        switch (pos)
        {
            case SnapPosition.Restore:
                Restore(hwnd);
                return;
            case SnapPosition.NextDisplay:
                MoveToDisplay(hwnd, forward: true);
                return;
            case SnapPosition.PrevDisplay:
                MoveToDisplay(hwnd, forward: false);
                return;
            case SnapPosition.Maximize:
                SaveRect(hwnd);
                NativeApi.ShowWindow(hwnd, NativeApi.SW_MAXIMIZE);
                return;
        }

        if (NativeApi.IsZoomed(hwnd))
            NativeApi.ShowWindow(hwnd, NativeApi.SW_RESTORE);

        var mi = GetMonitorInfo(hwnd);
        var wa = mi.rcWork;
        int W = wa.Width, H = wa.Height, L = wa.Left, T = wa.Top;

        var (x, y, w, h) = pos switch
        {
            SnapPosition.LeftHalf       => (L,           T,           W / 2,       H),
            SnapPosition.RightHalf      => (L + W / 2,   T,           W - W / 2,   H),
            SnapPosition.TopHalf        => (L,           T,           W,           H / 2),
            SnapPosition.BottomHalf     => (L,           T + H / 2,   W,           H - H / 2),
            SnapPosition.TopLeft        => (L,           T,           W / 2,       H / 2),
            SnapPosition.TopRight       => (L + W / 2,   T,           W - W / 2,   H / 2),
            SnapPosition.BottomLeft     => (L,           T + H / 2,   W / 2,       H - H / 2),
            SnapPosition.BottomRight    => (L + W / 2,   T + H / 2,   W - W / 2,   H - H / 2),
            SnapPosition.LeftThird      => (L,           T,           W / 3,       H),
            SnapPosition.CenterThird    => (L + W / 3,   T,           W / 3,       H),
            SnapPosition.RightThird     => (L + 2*W/3,   T,           W - 2*W/3,   H),
            SnapPosition.LeftTwoThirds  => (L,           T,           2*W/3,       H),
            SnapPosition.RightTwoThirds => (L + W / 3,   T,           W - W / 3,   H),
            SnapPosition.Center         => (L + W / 4,   T + H / 4,   W / 2,       H / 2),
            _ => throw new ArgumentOutOfRangeException(nameof(pos))
        };

        SaveRect(hwnd);
        NativeApi.SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h,
            NativeApi.SWP_NOZORDER | NativeApi.SWP_NOACTIVATE);
    }

    private static void SaveRect(IntPtr hwnd)
    {
        if (NativeApi.GetWindowRect(hwnd, out var r))
            _savedRects[hwnd] = r;
    }

    private static void Restore(IntPtr hwnd)
    {
        if (NativeApi.IsZoomed(hwnd))
        {
            NativeApi.ShowWindow(hwnd, NativeApi.SW_RESTORE);
            return;
        }
        if (_savedRects.TryGetValue(hwnd, out var r))
        {
            NativeApi.ShowWindow(hwnd, NativeApi.SW_SHOWNORMAL);
            NativeApi.SetWindowPos(hwnd, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height,
                NativeApi.SWP_NOZORDER | NativeApi.SWP_NOACTIVATE);
        }
    }

    private static void MoveToDisplay(IntPtr hwnd, bool forward)
    {
        var screens = Screen.AllScreens;
        if (screens.Length <= 1) return;

        var src = Screen.FromHandle(hwnd);
        int idx    = Array.IndexOf(screens, src);
        int newIdx = forward
            ? (idx + 1) % screens.Length
            : (idx - 1 + screens.Length) % screens.Length;
        var dst = screens[newIdx];

        NativeApi.GetWindowRect(hwnd, out var wr);
        var sw = src.WorkingArea;
        var dw = dst.WorkingArea;

        float rx = (float)(wr.Left  - sw.Left)  / sw.Width;
        float ry = (float)(wr.Top   - sw.Top)   / sw.Height;
        float rw = (float)wr.Width              / sw.Width;
        float rh = (float)wr.Height             / sw.Height;

        int nx = dw.Left + (int)(rx * dw.Width);
        int ny = dw.Top  + (int)(ry * dw.Height);
        int nw = (int)(rw * dw.Width);
        int nh = (int)(rh * dw.Height);

        bool wasMax = NativeApi.IsZoomed(hwnd);
        if (wasMax) NativeApi.ShowWindow(hwnd, NativeApi.SW_RESTORE);

        NativeApi.SetWindowPos(hwnd, IntPtr.Zero, nx, ny, nw, nh,
            NativeApi.SWP_NOZORDER | NativeApi.SWP_NOACTIVATE);

        if (wasMax) NativeApi.ShowWindow(hwnd, NativeApi.SW_MAXIMIZE);
    }

    private static NativeApi.MONITORINFO GetMonitorInfo(IntPtr hwnd)
    {
        var mon  = NativeApi.MonitorFromWindow(hwnd, NativeApi.MONITOR_DEFAULTTONEAREST);
        var info = new NativeApi.MONITORINFO
        {
            cbSize = Marshal.SizeOf<NativeApi.MONITORINFO>()
        };
        NativeApi.GetMonitorInfo(mon, ref info);
        return info;
    }
}
