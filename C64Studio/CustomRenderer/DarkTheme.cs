using System.Drawing;
using Krypton.Navigator;
using Krypton.Toolkit;



namespace RetroDevStudio.CustomRenderer
{
  /// <summary>
  /// Catppuccin Mocha color palette used for Krypton-themed controls in the
  /// map editor (and eventually beyond). Ported verbatim from the SID Sound
  /// Editor so the two apps share the same dark look and the same high-contrast
  /// accent ramp — the user has low-vision considerations and these colors have
  /// been tuned for that.
  ///
  /// Start small: for now this is just a palette of constants, consumed by
  /// custom Krypton state overrides in MapEditor and (planned) other editors.
  /// The KryptonManager global palette is set to MaterialDark in Program.cs —
  /// that covers combo dropdowns, checkboxes, sliders, etc. The constants here
  /// are used where we need to tweak a specific surface (e.g. tab headers) to
  /// match the overall Catppuccin feel rather than the stock MaterialDark.
  /// </summary>
  public static class DarkTheme
  {
    // Surfaces
    public static readonly Color Bg        = Color.FromArgb(  30,  30,  46 ); // #1e1e2e (base)
    public static readonly Color BgPanel   = Color.FromArgb(  49,  50,  68 ); // #313244 (surface0)
    public static readonly Color BgInput   = Color.FromArgb(  24,  24,  37 ); // #181825 (mantle)
    public static readonly Color BgHover   = Color.FromArgb(  69,  71,  90 ); // #45475a (surface1)
    public static readonly Color Border    = Color.FromArgb(  69,  71,  90 ); // #45475a
    public static readonly Color BtnBorder = Color.FromArgb( 147, 153, 178 ); // #9399b2 (overlay2)

    // Text
    public static readonly Color FgText    = Color.FromArgb( 205, 214, 244 ); // #cdd6f4
    public static readonly Color FgDim     = Color.FromArgb( 127, 132, 156 ); // #7f849c
    public static readonly Color FgMuted   = Color.FromArgb( 108, 112, 134 ); // #6c7086

    // Accents
    public static readonly Color Accent       = Color.FromArgb( 137, 180, 250 ); // #89b4fa blue
    public static readonly Color AccentGreen  = Color.FromArgb( 166, 227, 161 ); // #a6e3a1
    public static readonly Color AccentYellow = Color.FromArgb( 249, 226, 175 ); // #f9e2af
    public static readonly Color AccentPeach  = Color.FromArgb( 250, 179, 135 ); // #fab387
    public static readonly Color AccentRed    = Color.FromArgb( 243, 139, 168 ); // #f38ba8

    // Semantic shortcuts
    public static readonly Color StatusOk    = AccentGreen;
    public static readonly Color StatusInfo  = Accent;
    public static readonly Color StatusWarn  = AccentPeach;
    public static readonly Color StatusErr   = AccentRed;
    public static readonly Color StatusIdle  = FgDim;
    public static readonly Color ValueLabel  = Accent;



    /// <summary>
    /// Applies a flat, underline-style dark theme to a <see cref="KryptonNavigator"/>.
    /// Tabs render as flat text — unselected tabs are muted, the selected tab gets
    /// a 2px accent-colored bottom border. Think VS Code / Chrome tabs rather than
    /// the 3D chiseled Krypton default. Catppuccin Mocha colors are sourced from
    /// this class's constants so the whole look is coherent with the rest of the
    /// (planned) dark theme.
    /// </summary>
    public static void ApplyFlatDarkStyle( KryptonNavigator nav )
    {
      if ( nav == null )
      {
        return;
      }

      // BarTabOnly removes the "page frame" that makes the standard BarTabGroup
      // mode look like a 90s WinForms TabControl. We just want the strip of tabs
      // with the page content below, no surrounding box.
      nav.NavigatorMode = NavigatorMode.BarTabOnly;

      // LowProfile is Krypton's flattest built-in tab shape — minimal padding,
      // minimal depth. We override the actual per-state colors below, so this
      // mostly governs shape/geometry.
      nav.Bar.TabStyle = TabStyle.LowProfile;

      // Square corners.
      nav.StateCommon.Tab.Border.Rounding = 0;

      // Uniform dark body color across all states — the selection indicator is
      // the underline, not a background swap. Keeping the tab background the same
      // as the page body makes the whole area read as one continuous surface.
      var tabBg = Bg;

      // Normal (unselected, not hovered).
      nav.StateNormal.Tab.Back.Color1 = tabBg;
      nav.StateNormal.Tab.Back.Color2 = tabBg;
      nav.StateNormal.Tab.Back.ColorStyle = PaletteColorStyle.Solid;
      nav.StateNormal.Tab.Border.DrawBorders = PaletteDrawBorders.None;
      nav.StateNormal.Tab.Content.ShortText.Color1 = FgDim;
      nav.StateNormal.Tab.Content.ShortText.Color2 = FgDim;

      // Tracking (mouse over an unselected tab).
      nav.StateTracking.Tab.Back.Color1 = BgHover;
      nav.StateTracking.Tab.Back.Color2 = BgHover;
      nav.StateTracking.Tab.Back.ColorStyle = PaletteColorStyle.Solid;
      nav.StateTracking.Tab.Border.DrawBorders = PaletteDrawBorders.None;
      nav.StateTracking.Tab.Content.ShortText.Color1 = FgText;
      nav.StateTracking.Tab.Content.ShortText.Color2 = FgText;

      // Selected (currently active page). The underline is a bottom-only border
      // in the accent color.
      nav.StateSelected.Tab.Back.Color1 = tabBg;
      nav.StateSelected.Tab.Back.Color2 = tabBg;
      nav.StateSelected.Tab.Back.ColorStyle = PaletteColorStyle.Solid;
      nav.StateSelected.Tab.Border.DrawBorders = PaletteDrawBorders.Bottom;
      nav.StateSelected.Tab.Border.Color1 = Accent;
      nav.StateSelected.Tab.Border.Color2 = Accent;
      nav.StateSelected.Tab.Border.Width = 2;
      nav.StateSelected.Tab.Content.ShortText.Color1 = FgText;
      nav.StateSelected.Tab.Content.ShortText.Color2 = FgText;

      // The page content area shares the same dark body so there's no seam
      // between the bar and the page.
      nav.StateCommon.Back.Color1 = tabBg;
      nav.StateCommon.Back.Color2 = tabBg;
      nav.StateCommon.Border.DrawBorders = PaletteDrawBorders.None;
    }
  }
}
