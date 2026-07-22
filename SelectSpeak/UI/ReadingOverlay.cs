using System.Drawing.Drawing2D;
using SelectSpeak.Theming;

namespace SelectSpeak.UI;

/// <summary>
/// A small, non-activating, always-on-top neon "now reading" indicator with an animated
/// equalizer. It never takes focus and is cheap: GDI resources are cached (not reallocated
/// per frame), the animation timer only runs while actually reading, and the window is
/// created once and shown/hidden rather than recreated.
///
/// In persistent mode (auto-speak on) it stays visible between reads — idle when silent,
/// animating while speaking — which avoids the cost of showing/hiding a topmost window on
/// every selection.
/// </summary>
public sealed class ReadingOverlay : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int[] _bars = new int[14];
    private readonly Random _rng = new();

    private ThemePalette _palette = BuiltInThemes.Dark.Palette;
    private bool _reading;
    private bool _persistent;

    // Cached GDI resources, rebuilt only on palette change.
    private SolidBrush _bg;
    private SolidBrush _textBrush;
    private SolidBrush _mutedBrush;
    private Pen _borderPen;
    private SolidBrush[] _accentBrushes;
    private readonly Font _font = new("Segoe UI", 9f, FontStyle.Bold);

    public ReadingOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(240, 74);
        DoubleBuffered = true;

        _bg = new SolidBrush(_palette.BackgroundColor);
        _textBrush = new SolidBrush(_palette.TextPrimaryColor);
        _mutedBrush = new SolidBrush(_palette.TextMutedColor);
        _borderPen = new Pen(_palette.AccentPurpleColor, 2);
        _accentBrushes = BuildAccentBrushes(_palette);

        _timer = new System.Windows.Forms.Timer { Interval = 80 };
        _timer.Tick += (_, _) => { Step(); Invalidate(); };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_TOPMOST = 0x00000008;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
            return cp;
        }
    }

    public void ApplyPalette(ThemePalette palette)
    {
        _palette = palette;
        _bg.Dispose();
        _textBrush.Dispose();
        _mutedBrush.Dispose();
        _borderPen.Dispose();
        foreach (var b in _accentBrushes) b.Dispose();

        _bg = new SolidBrush(palette.BackgroundColor);
        _textBrush = new SolidBrush(palette.TextPrimaryColor);
        _mutedBrush = new SolidBrush(palette.TextMutedColor);
        _borderPen = new Pen(palette.AccentPurpleColor, 2);
        _accentBrushes = BuildAccentBrushes(palette);

        if (Visible) Invalidate();
    }

    /// <summary>Whether the overlay stays visible (idle) between reads. Set from auto-speak mode.</summary>
    public void SetPersistent(bool persistent)
    {
        _persistent = persistent;
        if (persistent)
            EnsureVisible();
        else if (!_reading)
            HideOverlay();
    }

    /// <summary>Begin the reading animation.</summary>
    public void BeginReading()
    {
        _reading = true;
        EnsureVisible();
        Step();
        _timer.Start();
        Invalidate();
    }

    /// <summary>Stop reading. Stays visible (idle) in persistent mode; otherwise hides.</summary>
    public void EndReading()
    {
        _reading = false;
        _timer.Stop();
        if (_persistent)
            Invalidate();
        else
            HideOverlay();
    }

    private void EnsureVisible()
    {
        PositionBottomRight();
        if (!Visible) Show();
    }

    private void HideOverlay()
    {
        if (Visible) Hide();
    }

    private void PositionBottomRight()
    {
        var wa = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(wa.Right - Width - 16, wa.Bottom - Height - 16);
    }

    private void Step()
    {
        int max = Height - 34;
        for (int i = 0; i < _bars.Length; i++)
            _bars[i] = _rng.Next(4, Math.Max(6, max));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.FillRectangle(_bg, ClientRectangle);
        g.DrawRectangle(_borderPen, 1, 1, Width - 3, Height - 3);

        g.DrawString(_reading ? "♪ Reading…" : "♪ Ready", _font,
            _reading ? _textBrush : _mutedBrush, 10, 5);

        const int barW = 11, gap = 5;
        int x = 10, baseY = Height - 8;
        for (int i = 0; i < _bars.Length && x + barW <= Width - 8; i++)
        {
            // Idle (persistent, not reading) shows short dim stubs; reading shows the equalizer.
            int h = _reading ? _bars[i] : 4;
            g.FillRectangle(_accentBrushes[i % _accentBrushes.Length], x, baseY - h, barW, h);
            x += barW + gap;
        }
    }

    private static SolidBrush[] BuildAccentBrushes(ThemePalette p) =>
        p.Accents.Select(c => new SolidBrush(c)).ToArray();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _bg.Dispose();
            _textBrush.Dispose();
            _mutedBrush.Dispose();
            _borderPen.Dispose();
            _font.Dispose();
            foreach (var b in _accentBrushes) b.Dispose();
        }
        base.Dispose(disposing);
    }
}
