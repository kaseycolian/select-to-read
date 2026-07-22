using SelectSpeak.Config;

namespace SelectSpeak.Theming;

/// <summary>
/// Owns the active theme and applies palettes to WinForms surfaces. Built-in themes
/// plus the user's custom themes form the selectable set; the active one is remembered
/// by name in <see cref="Settings"/>.
/// </summary>
public sealed class ThemeService
{
    private readonly Settings _settings;
    private readonly Action _save;

    public event EventHandler? ThemeChanged;

    /// <param name="save">How to persist changes; defaults to writing the settings file. Tests can pass a no-op.</param>
    public ThemeService(Settings settings, Action? save = null)
    {
        _settings = settings;
        _save = save ?? settings.Save;
    }

    public IReadOnlyList<Theme> AllThemes() =>
        BuiltInThemes.All.Concat(_settings.CustomThemes).ToList();

    public Theme Active =>
        AllThemes().FirstOrDefault(t => string.Equals(t.Name, _settings.ActiveTheme, StringComparison.OrdinalIgnoreCase))
        ?? BuiltInThemes.Dark;

    public ThemePalette Palette => Active.Palette;

    public void SetActive(string name)
    {
        if (AllThemes().Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            _settings.ActiveTheme = name;
            _save();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Add a new custom theme or overwrite an existing one by name. Rejects blank names, built-in names, and invalid palettes.</summary>
    public bool AddOrUpdateCustom(string name, ThemePalette palette, out string? error)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0)
        {
            error = "Theme name cannot be empty.";
            return false;
        }
        if (BuiltInThemes.IsBuiltInName(name))
        {
            error = $"'{name}' is a built-in theme. Choose a different name.";
            return false;
        }
        if (!palette.IsValid(out error))
            return false;

        var existing = _settings.CustomThemes
            .FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            existing.Palette = palette;
        else
            _settings.CustomThemes.Add(new Theme { Name = name, Palette = palette, IsBuiltIn = false });

        _save();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
        error = null;
        return true;
    }

    public bool RemoveCustom(string name)
    {
        var removed = _settings.CustomThemes.RemoveAll(
            t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed)
        {
            if (string.Equals(_settings.ActiveTheme, name, StringComparison.OrdinalIgnoreCase))
                _settings.ActiveTheme = BuiltInThemes.DefaultThemeName;
            _save();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
        return removed;
    }

    /// <summary>Recursively paint a control tree with the active palette.</summary>
    public void Apply(Control root) => ApplyRecursive(root, Palette);

    private static void ApplyRecursive(Control control, ThemePalette p)
    {
        switch (control)
        {
            case Button btn:
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = p.SurfaceAltColor;
                btn.ForeColor = p.TextPrimaryColor;
                btn.FlatAppearance.BorderColor = p.AccentPurpleColor;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = p.SurfaceColor;
                break;

            case TextBox or ComboBox or ListBox:
                control.BackColor = p.SurfaceAltColor;
                control.ForeColor = p.TextPrimaryColor;
                break;

            case CheckBox chk:
                // Keep the standard check glyph (Flat + transparent hides it on dark backgrounds).
                chk.FlatStyle = FlatStyle.Standard;
                chk.UseVisualStyleBackColor = false;
                chk.BackColor = p.BackgroundColor;
                chk.ForeColor = p.TextPrimaryColor;
                break;

            case Label lbl:
                lbl.BackColor = Color.Transparent;
                lbl.ForeColor = p.TextPrimaryColor;
                break;

            case GroupBox grp:
                grp.BackColor = p.SurfaceColor;
                grp.ForeColor = p.AccentBlueColor;
                break;

            case TrackBar tb:
                tb.BackColor = p.SurfaceColor;
                break;

            case Panel or Form:
                control.BackColor = p.BackgroundColor;
                control.ForeColor = p.TextPrimaryColor;
                break;

            default:
                control.ForeColor = p.TextPrimaryColor;
                break;
        }

        foreach (Control child in control.Controls)
            ApplyRecursive(child, p);
    }
}
