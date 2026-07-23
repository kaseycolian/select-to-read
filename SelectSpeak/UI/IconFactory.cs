using System.Drawing.Drawing2D;
using SelectSpeak.Theming;

namespace SelectSpeak.UI;

/// <summary>Draws the tray icon from the active palette so no .ico file needs shipping.</summary>
internal static class IconFactory
{
    public static Icon CreateTrayIcon(ThemePalette p)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Dark background circle with a neon-pink ring (ring pushed near the edge so the
            // speaker has room without touching it).
            using (var bg = new SolidBrush(p.BackgroundColor))
                g.FillEllipse(bg, 0, 0, 31, 31);
            using (var ring = new Pen(p.AccentPinkColor, 2f))
                g.DrawEllipse(ring, 1.5f, 1.5f, 28, 28);

            // Neon-pink speaker, centered on its own bounds so the solid mass sits dead-center,
            // scaled up but leaving clear padding inside the ring.
            // Local bounds x[0..12] y[3..19] → center (6, 11); max reach from center ≈ 10.
            var state = g.Save();
            g.TranslateTransform(16f, 16f);
            g.ScaleTransform(1.12f, 1.12f);
            g.TranslateTransform(-6f, -11f);

            using (var pink = new SolidBrush(p.AccentPinkColor))
            {
                var speaker = new PointF[]
                {
                    new(0, 8), new(5, 8), new(12, 3), new(12, 19), new(5, 14), new(0, 14),
                };
                g.FillPolygon(pink, speaker);
            }

            g.Restore(state);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }
}
