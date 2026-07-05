using System.Drawing;
using System.Runtime.CompilerServices;
using Krypton.Toolkit;



namespace RetroDevStudio.CustomRenderer
{
  /// <summary>
  /// Makes monochrome (black-ink) tool button glyphs readable on dark
  /// themes: dark, low-saturation pixels are re-inked with the theme's
  /// control-text color, while colored pixels (e.g. the fill bucket's
  /// paint splash) keep their hue. Whether re-inking is needed is derived
  /// from the ACTUAL themed button-face brightness — not the theme mode
  /// enum — so Custom themes behave correctly too.
  ///
  /// The pristine designer image is cached per button (weakly, so closed
  /// editors release theirs); switching back to a light theme restores the
  /// original pixels exactly, and repeated applications always transform
  /// from the original — never from an already re-inked image.
  /// </summary>
  public static class ToolIconTheming
  {
    // Pristine designer images per button.
    private static readonly ConditionalWeakTable<KryptonButton, Image>  s_OriginalImages =
        new ConditionalWeakTable<KryptonButton, Image>();

    // The last generated re-inked bitmap per button — disposed when a new
    // one replaces it so theme flips don't accumulate bitmaps.
    private static readonly ConditionalWeakTable<KryptonButton, Bitmap>  s_GeneratedImages =
        new ConditionalWeakTable<KryptonButton, Bitmap>();



    /// <summary>
    /// Applies theme-appropriate glyph images to the given buttons. Call
    /// once after InitializeComponent and again whenever the theme changes
    /// (RefreshDisplayOptions).
    /// </summary>
    public static void ApplyToolIcons( StudioCore Core, params KryptonButton[] Buttons )
    {
      if ( ( Core == null )
      ||   ( Core.Settings == null ) )
      {
        return;
      }
      bool darkTheme = Luma( Core.Settings.BGColor( Types.ColorableElement.BACKGROUND_BUTTON ) ) < 128;
      uint ink       = Core.Settings.FGColor( Types.ColorableElement.CONTROL_TEXT );

      foreach ( var button in Buttons )
      {
        if ( button == null )
        {
          continue;
        }
        if ( !s_OriginalImages.TryGetValue( button, out Image original ) )
        {
          original = button.Values.Image;
          if ( original == null )
          {
            continue;         // text-only button — nothing to re-ink
          }
          s_OriginalImages.Add( button, original );
        }

        s_GeneratedImages.TryGetValue( button, out Bitmap previousGenerated );
        if ( darkTheme )
        {
          var reinked = ReInkDarkPixels( original, ink );
          button.Values.Image = reinked;
          s_GeneratedImages.Remove( button );
          s_GeneratedImages.Add( button, reinked );
        }
        else
        {
          button.Values.Image = original;
          s_GeneratedImages.Remove( button );
        }
        previousGenerated?.Dispose();
      }
    }



    private static int Luma( uint ARGB )
    {
      return ( 77 * (int)( ( ARGB >> 16 ) & 0xff )
             + 150 * (int)( ( ARGB >> 8 ) & 0xff )
             + 29 * (int)( ARGB & 0xff ) ) >> 8;
    }



    /// <summary>
    /// Copy of the source image with every dark, low-saturation pixel's
    /// RGB replaced by the ink color. Alpha is preserved, so anti-aliased
    /// glyph edges keep their softness; saturated pixels (colored icon
    /// parts) pass through untouched. Icons are tiny (16x16-ish) and this
    /// runs only on theme application, so per-pixel Get/SetPixel is fine.
    /// </summary>
    private static Bitmap ReInkDarkPixels( Image Source, uint InkARGB )
    {
      var bmp = new Bitmap( Source );
      var ink = Color.FromArgb( (int)( 0xff000000 | ( InkARGB & 0xffffff ) ) );

      for ( int y = 0; y < bmp.Height; ++y )
      {
        for ( int x = 0; x < bmp.Width; ++x )
        {
          Color p = bmp.GetPixel( x, y );
          if ( p.A == 0 )
          {
            continue;
          }
          int max = System.Math.Max( p.R, System.Math.Max( p.G, p.B ) );
          int min = System.Math.Min( p.R, System.Math.Min( p.G, p.B ) );
          int luma = ( 77 * p.R + 150 * p.G + 29 * p.B ) >> 8;
          // Low saturation + dark = the glyph's black ink (or its grey
          // anti-aliasing) — re-ink it; anything colorful stays.
          if ( ( max - min < 48 )
          &&   ( luma < 140 ) )
          {
            bmp.SetPixel( x, y, Color.FromArgb( p.A, ink.R, ink.G, ink.B ) );
          }
        }
      }
      return bmp;
    }
  }
}
