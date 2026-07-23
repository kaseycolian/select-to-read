# Theme Service integration

**theme-service v0.1.1 — applied 2026-07-23**
Source: `C:\Sources\mine\theme-service` (`themes/theme.css`, `themes/themes.index.json`).

This is a WinForms (non-web) app, so the CSS is **not** vendored. Instead the brand themes are
ported into [`BuiltInThemes.cs`](BuiltInThemes.cs) as `ThemePalette` objects, and
[`ThemeService.cs`](ThemeService.cs) applies the active palette to the live WinForms controls,
the tray menu ([`NeonMenuRenderer`](NeonMenuRenderer.cs)), the reading overlay, and the tray icon.
The theme selector is the dropdown in `UI/SettingsForm.cs` (lists `ThemeService.AllThemes()`).

## Themes (10 = 5 families × dark/light)
Rink Classic (default), Midnight Arcade, Hot Neon, Synthwave Sunset, Acid Arcade — each `Dark`/`Light`.
Default = **Rink Classic Dark**. (Desktop app has no `prefers-color-scheme`; users pick a theme,
which persists in `%AppData%\SelectSpeak\settings.json` by name.)

## Token → ThemePalette role mapping
| theme.css token   | ThemePalette role |
|-------------------|-------------------|
| `--bg`            | `Background`      |
| `--bg-panel`      | `Surface`         |
| `--bg-elevated`   | `SurfaceAlt`      |
| `--text`          | `TextPrimary`     |
| `--text-muted`    | `TextMuted`       |
| `--border`        | `Border`          |
| `--focus-ring`    | `Focus`           |
| `--accent-pink`   | `AccentPink`      |
| `--accent-green`  | `AccentGreen`     |
| `--accent-purple` | `AccentPurple`    |
| `--accent-blue`   | `AccentBlue`      |
| `--border-strong` | `Disabled`        |

`--on-*` (text-on-accent) tokens aren't used directly; the app rarely puts text on a filled accent
(the one case — selected tray-menu item — uses `Background` as the on-focus text color).

## Updating
When the theme-service publishes a new VERSION, re-read `themes/theme.css` and re-port the values
into `BuiltInThemes.All` using the mapping above (values are AA-validated at the source — don't
invent colors). Update the version line here. New families just add rows to `BuiltInThemes.All`.
