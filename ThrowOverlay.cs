using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WindowThrow;

internal sealed class ThrowOverlay : Form
{
    private readonly IntPtr _targetHwnd;
    private readonly (Rectangle Rect, SnapPosition Pos, string Label)[] _zones;
    private readonly System.Windows.Forms.Timer _keyTimer;
    private readonly long _openedAt = Environment.TickCount64;
    private int _hovered = -1;

    // Colors
    private static readonly Color BgColor      = Color.FromArgb(18,  18,  30);
    private static readonly Color ZoneBg       = Color.FromArgb(36,  36,  56);
    private static readonly Color ZoneHover    = Color.FromArgb(72, 118, 220);
    private static readonly Color ZoneBorder   = Color.FromArgb(60,  60,  92);
    private static readonly Color SepColor     = Color.FromArgb(44,  44,  68);
    private static readonly Color TextNormal   = Color.FromArgb(210, 210, 235);
    private static readonly Color TextHover    = Color.White;
    private static readonly Color SubText      = Color.FromArgb(130, 130, 165);

    private static readonly Font FontLabel = new("Segoe UI", 9f);
    private static readonly Font FontTitle = new("Segoe UI", 11f, FontStyle.Bold);
    private static readonly Font FontHint  = new("Segoe UI", 8f);

    public ThrowOverlay(IntPtr targetHwnd)
    {
        _targetHwnd     = targetHwnd;
        FormBorderStyle = FormBorderStyle.None;
        TopMost         = true;
        ShowInTaskbar   = false;
        BackColor       = BgColor;
        Opacity         = 0.94;
        KeyPreview      = true;
        ClientSize      = new Size(540, 380);
        StartPosition   = FormStartPosition.Manual;

        _zones = BuildZones(ClientSize.Width, ClientSize.Height);

        var screen = Screen.FromHandle(targetHwnd);
        Location = new Point(
            screen.Bounds.Left + (screen.Bounds.Width  - Width)  / 2,
            screen.Bounds.Top  + (screen.Bounds.Height - Height) / 2);

        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint  |
            ControlStyles.UserPaint, true);

        _keyTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _keyTimer.Tick += OnKeyTick;
        _keyTimer.Start();
    }

    // Release Ctrl+Alt to snap (with 200ms grace after open so the hotkey keydown doesn't immediately fire)
    private void OnKeyTick(object? sender, EventArgs e)
    {
        if (Environment.TickCount64 - _openedAt < 200) return;
        bool ctrl = (NativeApi.GetAsyncKeyState(NativeApi.VK_CONTROL) & 0x8000) != 0;
        bool alt  = (NativeApi.GetAsyncKeyState(NativeApi.VK_MENU)    & 0x8000) != 0;
        if (!ctrl || !alt) ApplyAndClose();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int prev = _hovered;
        _hovered = -1;
        for (int i = 0; i < _zones.Length; i++)
        {
            if (_zones[i].Rect.Contains(e.Location)) { _hovered = i; break; }
        }
        if (_hovered != prev) Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left && _hovered >= 0) ApplyAndClose();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) Close();
    }

    private void ApplyAndClose()
    {
        if (_hovered >= 0)
            WindowSnapper.SnapDirect(_zones[_hovered].Pos, _targetHwnd);
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode       = SmoothingMode.AntiAlias;
        g.TextRenderingHint   = TextRenderingHint.ClearTypeGridFit;
        g.Clear(BgColor);

        // Title row
        using (var tb = new SolidBrush(TextNormal))
            g.DrawString("Window Throw", FontTitle, tb, new PointF(14, 10));
        using (var sb = new SolidBrush(SubText))
            g.DrawString("release Ctrl+Alt to snap  ·  ESC to cancel", FontHint, sb,
                new PointF(ClientSize.Width - 260f, 14));

        // Separator
        using (var sp = new Pen(SepColor, 1))
            g.DrawLine(sp, 14, 36, ClientSize.Width - 14, 36);

        // Zones
        for (int i = 0; i < _zones.Length; i++)
        {
            var (rect, _, label) = _zones[i];
            bool hot = i == _hovered;

            using (var bg = new SolidBrush(hot ? ZoneHover : ZoneBg))
                FillRounded(g, bg, rect, 7);
            using (var bp = new Pen(hot ? ZoneHover : ZoneBorder, 1))
                DrawRounded(g, bp, rect, 7);

            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            using (var lb = new SolidBrush(hot ? TextHover : TextNormal))
                g.DrawString(label, FontLabel, lb, rect, sf);
        }
    }

    // Layout: 3-column grid, 5 rows
    private static (Rectangle, SnapPosition, string)[] BuildZones(int W, int H)
    {
        const int pad     = 14;
        const int gap     = 6;
        const int titleH  = 42;
        const int rowMain = 62;
        const int rowSub  = 48;

        int colW = (W - 2 * pad - 2 * gap) / 3;
        int c0 = pad,  c1 = pad + colW + gap,  c2 = pad + 2 * (colW + gap);

        int y0 = titleH + 4;
        int y1 = y0 + rowMain + gap;
        int y2 = y1 + rowMain + gap;
        int y3 = y2 + rowMain + 12; // extra gap before thirds
        int y4 = y3 + rowSub  + gap;

        return new[]
        {
            (new Rectangle(c0, y0, colW, rowMain), SnapPosition.TopLeft,        "Top Left"),
            (new Rectangle(c1, y0, colW, rowMain), SnapPosition.TopHalf,        "Top Half"),
            (new Rectangle(c2, y0, colW, rowMain), SnapPosition.TopRight,       "Top Right"),

            (new Rectangle(c0, y1, colW, rowMain), SnapPosition.LeftHalf,       "Left Half"),
            (new Rectangle(c1, y1, colW, rowMain), SnapPosition.Maximize,       "Maximize"),
            (new Rectangle(c2, y1, colW, rowMain), SnapPosition.RightHalf,      "Right Half"),

            (new Rectangle(c0, y2, colW, rowMain), SnapPosition.BottomLeft,     "Bottom Left"),
            (new Rectangle(c1, y2, colW, rowMain), SnapPosition.BottomHalf,     "Bottom Half"),
            (new Rectangle(c2, y2, colW, rowMain), SnapPosition.BottomRight,    "Bottom Right"),

            (new Rectangle(c0, y3, colW, rowSub),  SnapPosition.LeftThird,      "Left ⅓"),
            (new Rectangle(c1, y3, colW, rowSub),  SnapPosition.CenterThird,    "Center ⅓"),
            (new Rectangle(c2, y3, colW, rowSub),  SnapPosition.RightThird,     "Right ⅓"),

            (new Rectangle(c0, y4, colW, rowSub),  SnapPosition.LeftTwoThirds,  "Left ⅔"),
            (new Rectangle(c1, y4, colW, rowSub),  SnapPosition.Center,         "Center"),
            (new Rectangle(c2, y4, colW, rowSub),  SnapPosition.RightTwoThirds, "Right ⅔"),
        };
    }

    private static void FillRounded(Graphics g, Brush b, Rectangle r, int rad)
    {
        using var path = RoundedPath(r, rad);
        g.FillPath(b, path);
    }

    private static void DrawRounded(Graphics g, Pen p, Rectangle r, int rad)
    {
        using var path = RoundedPath(r, rad);
        g.DrawPath(p, path);
    }

    private static GraphicsPath RoundedPath(Rectangle r, int rad)
    {
        int d = rad * 2;
        var path = new GraphicsPath();
        path.AddArc(r.Left,          r.Top,           d, d, 180, 90);
        path.AddArc(r.Right - d,     r.Top,           d, d, 270, 90);
        path.AddArc(r.Right - d,     r.Bottom - d,    d, d,   0, 90);
        path.AddArc(r.Left,          r.Bottom - d,    d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _keyTimer.Stop(); _keyTimer.Dispose(); }
        base.Dispose(disposing);
    }
}
