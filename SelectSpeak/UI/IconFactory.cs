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

            // Dark background circle with a neon-pink ring.
            using (var bg = new SolidBrush(p.BackgroundColor))
                g.FillEllipse(bg, 1, 1, 30, 30);
            using (var ring = new Pen(p.AccentPinkColor, 2f))
                g.DrawEllipse(ring, 2, 2, 27, 27);

            // Neon-pink speaker, centered on its own bounds so the solid mass sits dead-center
            // and scaled up to nearly fill the circle while staying clear of the ring.
            // Local bounds x[0..12] y[3..19] → center (6, 11).
            var state = g.Save();
            g.TranslateTransform(16f, 16f);
            g.ScaleTransform(1.35f, 1.35f);
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
