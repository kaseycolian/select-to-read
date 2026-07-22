using SelectSpeak.Theming;

namespace SelectSpeak.UI;

/// <summary>
/// Edits a palette role-by-role and saves it as a named custom theme. Starts from a chosen
/// base theme; a live preview reflects edits. Built-in names are rejected on save.
/// </summary>
public sealed class ThemeEditorForm : Form
{
    private readonly ThemeService _themes;
    private ThemePalette _working;

    private readonly TextBox _nameBox = new();
    private readonly ComboBox _baseBox = new();
    private readonly Panel _preview = new();
    private readonly Label _status = new();
    private readonly TableLayoutPanel _rolesTable = new();
    private readonly Dictionary<string, Button> _swatches = new();

    /// <summary>The name the theme was saved under (null if cancelled).</summary>
    public string? SavedThemeName { get; private set; }

    public ThemeEditorForm(ThemeService themes, string? baseThemeName)
    {
        _themes = themes;

        var baseTheme = themes.AllThemes()
            .FirstOrDefault(t => string.Equals(t.Name, baseThemeName, StringComparison.OrdinalIgnoreCase))
            ?? BuiltInThemes.Dark;
        _working = baseTheme.Palette.Clone();

        Text = "Theme editor";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 470);

        BuildLayout(baseTheme.Name);
        RefreshSwatches();
        _themes.Apply(this);
        StyleInputs();
    }

    private void BuildLayout(string baseName)
    {
        var nameLabel = new Label { Text = "Name", AutoSize = true, Location = new Point(16, 18) };
        _nameBox.SetBounds(120, 14, 200, 24);
        _nameBox.Text = SuggestName();

        var baseLabel = new Label { Text = "Start from", AutoSize = true, Location = new Point(16, 52) };
        _baseBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _baseBox.SetBounds(120, 48, 200, 24);
        foreach (var t in _themes.AllThemes()) _baseBox.Items.Add(t.Name);
        _baseBox.SelectedItem = baseName;
        _baseBox.SelectedIndexChanged += BaseBox_Changed;

        // Roles grid (left) — one label + swatch per role
        _rolesTable.SetBounds(16, 88, 300, 330);
        _rolesTable.ColumnCount = 2;
        _rolesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        _rolesTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        _rolesTable.AutoScroll = true;
        foreach (var role in ThemePalette.Roles)
        {
            var lbl = new Label { Text = role.DisplayName, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) };
            var swatch = new Button
            {
                Width = 90,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                Tag = role.Key,
                Margin = new Padding(3, 4, 3, 3),
            };
            swatch.Click += (_, _) => PickColor(role);
            _swatches[role.Key] = swatch;
            _rolesTable.Controls.Add(lbl);
            _rolesTable.Controls.Add(swatch);
        }

        // Preview (right)
        _preview.SetBounds(332, 88, 212, 300);
        _preview.Paint += Preview_Paint;

        _status.SetBounds(16, 424, 380, 20);
        _status.AutoSize = false;

        var save = new Button { Text = "Save theme", DialogResult = DialogResult.None };
        save.SetBounds(332, 396, 100, 30);
        save.Click += Save_Click;

        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        cancel.SetBounds(444, 396, 100, 30);

        Controls.AddRange(new Control[]
        {
            nameLabel, _nameBox, baseLabel, _baseBox, _rolesTable, _preview, _status, save, cancel,
        });
        CancelButton = cancel;
    }

    private string SuggestName()
    {
        string baseName = "My Neon";
        int n = 1;
        var existing = _themes.AllThemes().Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string candidate = baseName;
        while (existing.Contains(candidate)) candidate = $"{baseName} {++n}";
        return candidate;
    }

    private void BaseBox_Changed(object? sender, EventArgs e)
    {
        var chosen = _themes.AllThemes()
            .FirstOrDefault(t => string.Equals(t.Name, _baseBox.SelectedItem as string, StringComparison.OrdinalIgnoreCase));
        if (chosen is not null)
        {
            _working = chosen.Palette.Clone();
            RefreshSwatches();
            _preview.Invalidate();
        }
    }

    private void PickColor(PaletteRole role)
    {
        using var dlg = new ColorDialog { FullOpen = true, AllowFullOpen = true };
        if (ColorUtil.TryFromHex(role.Get(_working), out var current))
            dlg.Color = current;
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            role.Set(_working, ColorUtil.ToHex(dlg.Color));
            RefreshSwatches();
            _preview.Invalidate();
        }
    }

    private void RefreshSwatches()
    {
        foreach (var role in ThemePalette.Roles)
        {
            if (_swatches.TryGetValue(role.Key, out var btn) && ColorUtil.TryFromHex(role.Get(_working), out var c))
            {
                btn.BackColor = c;
                btn.ForeColor = c.GetBrightness() > 0.5f ? Color.Black : Color.White;
                btn.Text = role.Get(_working);
                btn.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            }
        }
    }

    private void Preview_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var p = _working;
        g.Clear(p.BackgroundColor);

        using (var surface = new SolidBrush(p.SurfaceColor))
            g.FillRectangle(surface, 12, 12, _preview.Width - 24, _preview.Height - 24);
        using (var borderPen = new Pen(p.BorderColor, 1))
            g.DrawRectangle(borderPen, 12, 12, _preview.Width - 25, _preview.Height - 25);

        using (var title = new SolidBrush(p.TextPrimaryColor))
        using (var font = new Font("Segoe UI", 11f, FontStyle.Bold))
            g.DrawString("Preview", font, title, 24, 24);

        using (var muted = new SolidBrush(p.TextMutedColor))
        using (var font = new Font("Segoe UI", 8.5f))
            g.DrawString("The quick brown fox", font, muted, 24, 50);

        var accents = p.Accents;
        for (int i = 0; i < accents.Length; i++)
            using (var b = new SolidBrush(accents[i]))
                g.FillRectangle(b, 24 + i * 34, 78, 26, 40);

        using (var btnFill = new SolidBrush(p.SurfaceAltColor))
            g.FillRectangle(btnFill, 24, 132, 120, 30);
        using (var focus = new Pen(p.FocusColor, 2))
            g.DrawRectangle(focus, 24, 132, 120, 30);
        using (var btnText = new SolidBrush(p.TextPrimaryColor))
        using (var font = new Font("Segoe UI", 9f))
            g.DrawString("Button", font, btnText, 58, 139);
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        if (_themes.AddOrUpdateCustom(_nameBox.Text, _working.Clone(), out var error))
        {
            SavedThemeName = _nameBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _status.ForeColor = Color.Red;
            _status.Text = error ?? "Could not save theme.";
        }
    }

    private void StyleInputs()
    {
        // Keep swatch buttons showing their true color (ThemeService would override them).
        RefreshSwatches();
    }
}
