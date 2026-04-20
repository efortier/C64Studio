using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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

      // BarTabGroup shows the tab strip and the page content area ("group").
      // BarTabOnly only draws the tabs — pages would be hidden — so we stay
      // on BarTabGroup and just flatten the group's border below.
      nav.NavigatorMode = NavigatorMode.BarTabGroup;

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



    /// <summary>
    /// Krypton's MaterialDark palette paints disabled KryptonComboBoxes with a
    /// light-gray background that reads as bright white on our dark surfaces.
    /// This override forces the disabled state to stay dark — matches what
    /// SIDSoundEditor does. Safe to call on any KryptonComboBox; idempotent.
    /// </summary>
    public static void StyleDisabledComboDark( KryptonComboBox combo )
    {
      if ( combo == null )
      {
        return;
      }
      combo.StateDisabled.ComboBox.Back.Color1    = BgInput;
      combo.StateDisabled.ComboBox.Border.Color1  = Border;
      combo.StateDisabled.ComboBox.Content.Color1 = FgMuted;
    }



    // ================================================================
    // Windows dark-mode scrollbar plumbing
    //
    // Windows 10 1903+ and Windows 11 ship a dark scrollbar theme that isn't
    // exposed through a public API. The standard unblock is:
    //   1) uxtheme.dll ordinal #135 (SetPreferredAppMode) once per process to
    //      tell the OS we opt into dark app chrome,
    //   2) uxtheme.dll!SetWindowTheme(hwnd, "DarkMode_Explorer", null) per
    //      control that has a scrollbar.
    // We wrap both in try/catch so older Windows (or Wine) fall back quietly.
    // ================================================================

    [DllImport( "uxtheme.dll", CharSet = CharSet.Unicode )]
    private static extern int SetWindowTheme( IntPtr hwnd, string subAppName, string subIdList );

    [DllImport( "uxtheme.dll", EntryPoint = "#135", CharSet = CharSet.Unicode )]
    private static extern int Uxtheme_SetPreferredAppMode( int preferredAppMode );

    private const int APP_MODE_DEFAULT = 0;
    private const int APP_MODE_ALLOW_DARK = 1;

    private static bool s_DarkModeEnabled = false;



    /// <summary>
    /// Opts the process into Windows dark-mode app chrome. Call once at app
    /// startup, before any forms are shown. No-op on pre-1903 Windows.
    /// </summary>
    public static void EnableDarkModeForApp()
    {
      if ( s_DarkModeEnabled )
      {
        return;
      }
      try
      {
        Uxtheme_SetPreferredAppMode( APP_MODE_ALLOW_DARK );
        s_DarkModeEnabled = true;
      }
      catch ( Exception )
      {
        // Older OS / Wine / missing uxtheme — silently skip.
      }
    }



    /// <summary>
    /// Applies the Win32 "DarkMode_Explorer" theme to a control, which makes
    /// its scrollbar (ListBox, multiline TextBox, TreeView, etc.) render dark
    /// on Windows 10 1903+. If the handle isn't created yet, defers the call
    /// until <see cref="Control.HandleCreated"/> fires.
    /// </summary>
    public static void ApplyDarkScrollBarsTo( Control control )
    {
      if ( control == null )
      {
        return;
      }

      void apply()
      {
        try
        {
          SetWindowTheme( control.Handle, "DarkMode_Explorer", null );
        }
        catch ( Exception )
        {
          // Don't blow up UI construction if the OS doesn't support it.
        }
      }

      if ( control.IsHandleCreated )
      {
        apply();
      }
      else
      {
        control.HandleCreated += ( s, e ) => apply();
      }
    }



    /// <summary>
    /// Darkens an owner-drawn <see cref="ComboBox"/> (the kind C64Studio uses
    /// for color swatches). The DrawItem handler already paints each item, so
    /// the only remaining bright pixels are the control's chrome — dropdown
    /// button and border. FlatStyle.Flat lets BackColor/ForeColor paint those.
    /// </summary>
    public static void StyleOwnerDrawComboDark( ComboBox combo )
    {
      if ( combo == null )
      {
        return;
      }
      combo.FlatStyle = FlatStyle.Flat;
      combo.BackColor = BgInput;
      combo.ForeColor = FgText;
    }
  }
}
