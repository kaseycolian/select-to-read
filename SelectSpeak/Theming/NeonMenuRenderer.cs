namespace SelectSpeak.Theming;

/// <summary>Renders the tray <see cref="ContextMenuStrip"/> in the active palette's neon-on-dark style.</summary>
public sealed class NeonMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly ThemePalette _p;

    public NeonMenuRenderer(ThemePalette palette) : base(new NeonColorTable(palette))
    {
        _p = palette;
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? _p.BackgroundColor : _p.TextPrimaryColor;
        base.OnRenderItemText(e);
    }

    private sealed class NeonColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _p;
        public NeonColorTable(ThemePalette p) { _p = p; UseSystemColors = false; }

        public override Color ToolStripDropDownBackground => _p.SurfaceColor;
        public override Color ImageMarginGradientBegin => _p.SurfaceColor;
        public override Color ImageMarginGradientMiddle => _p.SurfaceColor;
        public override Color ImageMarginGradientEnd => _p.SurfaceColor;
        public override Color MenuBorder => _p.AccentPurpleColor;
        public override Color MenuItemBorder => _p.FocusColor;
        public override Color MenuItemSelected => _p.FocusColor;
        public override Color MenuItemSelectedGradientBegin => _p.FocusColor;
        public override Color MenuItemSelectedGradientEnd => _p.FocusColor;
        public override Color MenuItemPressedGradientBegin => _p.AccentPurpleColor;
        public override Color MenuItemPressedGradientEnd => _p.AccentPurpleColor;
        public override Color SeparatorDark => _p.BorderColor;
        public override Color SeparatorLight => _p.BorderColor;
        public override Color CheckBackground => _p.AccentGreenColor;
        public override Color CheckSelectedBackground => _p.AccentGreenColor;
        public override Color CheckPressedBackground => _p.AccentGreenColor;
    }
}
