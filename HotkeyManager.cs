using System;
using System.Collections.Generic;

namespace WindowThrow;

internal sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, SnapPosition> _map = new();
    private int _nextId = 1;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        uint ca  = NativeApi.MOD_CONTROL | NativeApi.MOD_ALT | NativeApi.MOD_NOREPEAT;
        uint cas = NativeApi.MOD_CONTROL | NativeApi.MOD_ALT | NativeApi.MOD_SHIFT | NativeApi.MOD_NOREPEAT;

        Register(ca,  NativeApi.VK_LEFT,   SnapPosition.LeftHalf);
        Register(ca,  NativeApi.VK_RIGHT,  SnapPosition.RightHalf);
        Register(ca,  NativeApi.VK_UP,     SnapPosition.TopHalf);
        Register(ca,  NativeApi.VK_DOWN,   SnapPosition.BottomHalf);
        Register(ca,  NativeApi.VK_U,      SnapPosition.TopLeft);
        Register(ca,  NativeApi.VK_I,      SnapPosition.TopRight);
        Register(ca,  NativeApi.VK_J,      SnapPosition.BottomLeft);
        Register(ca,  NativeApi.VK_K,      SnapPosition.BottomRight);
        Register(ca,  NativeApi.VK_D,      SnapPosition.LeftThird);
        Register(ca,  NativeApi.VK_F,      SnapPosition.CenterThird);
        Register(ca,  NativeApi.VK_G,      SnapPosition.RightThird);
        Register(ca,  NativeApi.VK_E,      SnapPosition.LeftTwoThirds);
        Register(ca,  NativeApi.VK_T,      SnapPosition.RightTwoThirds);
        Register(ca,  NativeApi.VK_RETURN, SnapPosition.Maximize);
        Register(ca,  NativeApi.VK_C,      SnapPosition.Center);
        Register(ca,  NativeApi.VK_BACK,   SnapPosition.Restore);
        Register(cas, NativeApi.VK_LEFT,   SnapPosition.PrevDisplay);
        Register(cas, NativeApi.VK_RIGHT,  SnapPosition.NextDisplay);
        Register(ca,  NativeApi.VK_W,      SnapPosition.Throw);
    }

    private void Register(uint mods, uint vk, SnapPosition pos)
    {
        int id = _nextId++;
        if (NativeApi.RegisterHotKey(_hwnd, id, mods, vk))
            _map[id] = pos;
    }

    public bool TryGetPosition(int id, out SnapPosition pos) =>
        _map.TryGetValue(id, out pos);

    public void Dispose()
    {
        foreach (var id in _map.Keys)
            NativeApi.UnregisterHotKey(_hwnd, id);
    }
}
