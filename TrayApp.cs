using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowThrow;

internal sealed class TrayApp : Form
{
    private readonly NotifyIcon  _tray;
    private readonly StashManager _stash = new();
    private HotkeyManager?       _hotkeys;
    private ThrowOverlay?        _overlay;

    public TrayApp()
    {
        ShowInTaskbar   = false;
        WindowState     = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.None;
        Width = Height  = 1;
        Opacity         = 0;

        _tray = new NotifyIcon
        {
            Icon             = CreateIcon(),
            Text             = "Window Throw",
            Visible          = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
        _hotkeys = new HotkeyManager(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeApi.WM_HOTKEY && _hotkeys is not null)
        {
            if (_hotkeys.TryGetPosition(m.WParam.ToInt32(), out var pos))
                HandleSnap(pos);
        }
        base.WndProc(ref m);
    }

    private void HandleSnap(SnapPosition pos)
    {
        switch (pos)
        {
            case SnapPosition.Throw:
                ShowThrowOverlay();
                break;
            case SnapPosition.StashLeft:
                _stash.Stash(StashEdge.Left);
                break;
            case SnapPosition.StashRight:
                _stash.Stash(StashEdge.Right);
                break;
            case SnapPosition.StashUp:
                _stash.Stash(StashEdge.Top);
                break;
            case SnapPosition.StashDown:
                _stash.Stash(StashEdge.Bottom);
                break;
            default:
                WindowSnapper.Snap(pos);
                break;
        }
    }

    private void ShowThrowOverlay()
    {
        if (_overlay is { IsDisposed: false }) return;
        var hwnd = NativeApi.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || hwnd == Handle) return;
        _overlay = new ThrowOverlay(hwnd);
        _overlay.Show();
        _overlay.Activate();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(new ToolStripMenuItem("Window Throw") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        var snap = new ToolStripMenuItem("Snap");
        snap.DropDownItems.AddRange(new ToolStripItem[]
        {
            Item("Window Throw",     "Ctrl+Alt+W"),
            new ToolStripSeparator(),
            Item("Left Half",        "Ctrl+Alt+←  · cycles ⅓ → ⅔"),
            Item("Right Half",       "Ctrl+Alt+→  · cycles ⅓ → ⅔"),
            Item("Top Half",         "Ctrl+Alt+↑"),
            Item("Bottom Half",      "Ctrl+Alt+↓"),
            new ToolStripSeparator(),
            Item("Top Left",         "Ctrl+Alt+U"),
            Item("Top Right",        "Ctrl+Alt+I"),
            Item("Bottom Left",      "Ctrl+Alt+J"),
            Item("Bottom Right",     "Ctrl+Alt+K"),
            new ToolStripSeparator(),
            Item("Left Third",       "Ctrl+Alt+D"),
            Item("Center Third",     "Ctrl+Alt+F"),
            Item("Right Third",      "Ctrl+Alt+G"),
            Item("Left 2/3",         "Ctrl+Alt+E"),
            Item("Right 2/3",        "Ctrl+Alt+T"),
            new ToolStripSeparator(),
            Item("Maximize",         "Ctrl+Alt+Enter"),
            Item("Center",           "Ctrl+Alt+C"),
            Item("Restore",          "Ctrl+Alt+Backspace"),
            new ToolStripSeparator(),
            Item("Prev Display",     "Ctrl+Alt+Shift+←"),
            Item("Next Display",     "Ctrl+Alt+Shift+→"),
        });
        menu.Items.Add(snap);

        var stash = new ToolStripMenuItem("Stash");
        stash.DropDownItems.Add("Stash Left",   null, (_, _) => _stash.Stash(StashEdge.Left));
        stash.DropDownItems.Add("Stash Right",  null, (_, _) => _stash.Stash(StashEdge.Right));
        stash.DropDownItems.Add("Stash Up",     null, (_, _) => _stash.Stash(StashEdge.Top));
        stash.DropDownItems.Add("Stash Down",   null, (_, _) => _stash.Stash(StashEdge.Bottom));
        stash.DropDownItems.Add(new ToolStripSeparator());
        stash.DropDownItems.Add("Unstash All",  null, (_, _) => _stash.UnstashAll());
        menu.Items.Add(stash);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Application.Exit());
        return menu;
    }

    private static ToolStripMenuItem Item(string label, string shortcut) =>
        new(label) { ShortcutKeyDisplayString = shortcut, ShowShortcutKeys = true, Enabled = false };

    private static Icon CreateIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        var blue = Color.FromArgb(99, 148, 237);
        var dark = Color.FromArgb(55, 100, 190);

        using var fill = new SolidBrush(blue);
        using var pen  = new Pen(dark, 1f);

        g.FillRectangle(fill, 1, 3, 6, 10);
        g.DrawRectangle(pen,  1, 3, 5, 9);
        g.FillRectangle(fill, 9, 3, 6, 10);
        g.DrawRectangle(pen,  9, 3, 5, 9);

        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeys?.Dispose();
            _stash.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}
