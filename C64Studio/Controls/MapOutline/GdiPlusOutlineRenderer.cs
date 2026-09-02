using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// GDI+ implementation of the outline drawing backend. Antialiasing via
  /// SmoothingMode.AntiAlias; a single Graphics is held for the renderer's
  /// lifetime (one drawing session).
  /// </summary>
  public class GdiPlusOutlineRenderer : IOutlineRenderer
  {
    private Graphics    m_Graphics;



    public GdiPlusOutlineRenderer( Bitmap Target )
    {
      m_Graphics = Graphics.FromImage( Target );
      m_Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      m_Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }



    public void RestoreRegion( Bitmap Source, Rectangle Region )
    {
      // SourceCopy is the whole point: restore must REPLACE pixels, not
      // blend the snapshot over the half-drawn stroke.
      var previousCompositing = m_Graphics.CompositingMode;
      m_Graphics.CompositingMode = CompositingMode.SourceCopy;
      m_Graphics.DrawImage( Source, Region, Region, GraphicsUnit.Pixel );
      m_Graphics.CompositingMode = previousCompositing;
    }



    public void DrawStrokePath( IReadOnlyList<PointF> Points, Color Color, float Width )
    {
      if ( ( Points == null )
      ||   ( Points.Count == 0 ) )
      {
        return;
      }
      using ( var pen = new Pen( Color, Width ) )
      {
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        pen.LineJoin = LineJoin.Round;
        if ( Points.Count == 1 )
        {
          // A click without movement: DrawLines needs two points; a
          // zero-length segment with round caps renders the brush dot.
          m_Graphics.DrawLine( pen, Points[0], new PointF( Points[0].X + 0.01f, Points[0].Y ) );
          return;
        }
        var points = new PointF[Points.Count];
        for ( int i = 0; i < Points.Count; ++i )
        {
          points[i] = Points[i];
        }
        m_Graphics.DrawLines( pen, points );
      }
    }



    public void FillRectangle( RectangleF Rect, Color Color )
    {
      if ( Color.A == 0 )
      {
        return;
      }
      using ( var brush = new SolidBrush( Color ) )
      {
        m_Graphics.FillRectangle( brush, Rect );
      }
    }



    public void DrawRectangleOutline( RectangleF Rect, Color Color, float BorderWidth )
    {
      if ( ( Color.A == 0 )
      ||   ( BorderWidth <= 0 ) )
      {
        return;
      }
      using ( var pen = new Pen( Color, BorderWidth ) )
      {
        pen.LineJoin = LineJoin.Miter;
        m_Graphics.DrawRectangle( pen, Rect.X, Rect.Y, Rect.Width, Rect.Height );
      }
    }



    public void FillEllipse( RectangleF Rect, Color Color )
    {
      if ( Color.A == 0 )
      {
        return;
      }
      using ( var brush = new SolidBrush( Color ) )
      {
        m_Graphics.FillEllipse( brush, Rect );
      }
    }



    public void DrawEllipseOutline( RectangleF Rect, Color Color, float BorderWidth )
    {
      if ( ( Color.A == 0 )
      ||   ( BorderWidth <= 0 ) )
      {
        return;
      }
      using ( var pen = new Pen( Color, BorderWidth ) )
      {
        m_Graphics.DrawEllipse( pen, Rect );
      }
    }



    /// <summary>
    /// Best GDI+ text quality for an IMAGE target: AntiAliasGridFit.
    /// (ClearType would bake subpixel color fringes into the saved PNG.)
    /// </summary>
    public void DrawText( string Text, PointF Position, string FontFamily, float FontSize,
                          bool Bold, bool Italic, Color Color )
    {
      if ( ( string.IsNullOrEmpty( Text ) )
      ||   ( Color.A == 0 ) )
      {
        return;
      }
      var previousHint = m_Graphics.TextRenderingHint;
      m_Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
      using ( var font = CreateFont( FontFamily, FontSize, Bold, Italic ) )
      using ( var brush = new SolidBrush( Color ) )
      {
        m_Graphics.DrawString( Text, font, brush, Position );
      }
      m_Graphics.TextRenderingHint = previousHint;
    }



    public SizeF MeasureText( string Text, string FontFamily, float FontSize, bool Bold, bool Italic )
    {
      if ( string.IsNullOrEmpty( Text ) )
      {
        return SizeF.Empty;
      }
      using ( var font = CreateFont( FontFamily, FontSize, Bold, Italic ) )
      using ( var format = (StringFormat)StringFormat.GenericDefault.Clone() )
      {
        // Trailing spaces count: bounds/invalidation/hit-testing must cover
        // the full typed content (MeasureString drops them by default).
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        return m_Graphics.MeasureString( Text, font, new SizeF( 1000000f, 1000000f ), format );
      }
    }



    /// <summary>
    /// Bakes an object at 1:1: a text object through the shared layout
    /// engine — the exact arrangement (wrap, char/line spacing, typographic
    /// advances) the canvas overlay shows — and an image object as a
    /// pixel-exact nearest-neighbor stamp of its decoded payload.
    /// </summary>
    public void DrawTextObject( OutlineTextObject Obj )
    {
      if ( Obj == null )
      {
        return;
      }
      if ( Obj.IsImage )
      {
        var image = Obj.GetImage();
        DrawImageNearest( image,
          new RectangleF( Obj.Position.X, Obj.Position.Y, image.Width, image.Height ) );
        return;
      }
      if ( ( string.IsNullOrEmpty( Obj.Text ) )
      ||   ( Obj.Color.A == 0 ) )
      {
        return;
      }
      var previousHint = m_Graphics.TextRenderingHint;
      m_Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
      using ( var font = CreateFont( Obj.FontFamily, Obj.FontSize, Obj.Bold, Obj.Italic ) )
      {
        OutlineTextLayout.Draw( m_Graphics, Obj.GetLayout(), Obj.Text, font,
                                Obj.Position, 1.0f, Obj.LineSpacing, Obj.Color,
                                Obj.MeasuredSize().Width, Obj.Alignment );
      }
      m_Graphics.TextRenderingHint = previousHint;
    }



    /// <summary>
    /// Draws an object in VIEW space: text via the layout's image-space
    /// positions scaled by the zoom (glyphs drawn with a zoom-scaled font —
    /// identical arrangement to the 1:1 bake, crisp at any zoom); an image
    /// object as a nearest-neighbor stamp scaled by the zoom, matching how
    /// the canvas presents its backing raster.
    /// </summary>
    public static void DrawTextObjectView( Graphics ViewGraphics, OutlineTextObject Obj,
                                           PointF ViewOrigin, float ViewZoom )
    {
      if ( Obj == null )
      {
        return;
      }
      if ( Obj.IsImage )
      {
        var image = Obj.GetImage();
        var previousInterpolation = ViewGraphics.InterpolationMode;
        var previousOffset = ViewGraphics.PixelOffsetMode;
        ViewGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        ViewGraphics.PixelOffsetMode = PixelOffsetMode.Half;
        ViewGraphics.DrawImage( image,
          new RectangleF( ViewOrigin.X, ViewOrigin.Y, image.Width * ViewZoom, image.Height * ViewZoom ) );
        ViewGraphics.InterpolationMode = previousInterpolation;
        ViewGraphics.PixelOffsetMode = previousOffset;
        return;
      }
      if ( ( string.IsNullOrEmpty( Obj.Text ) )
      ||   ( Obj.Color.A == 0 ) )
      {
        return;
      }
      float viewFontSize = Math.Max( 1.0f, Obj.FontSize * ViewZoom );
      var previousHint = ViewGraphics.TextRenderingHint;
      ViewGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
      using ( var font = CreateFont( Obj.FontFamily, viewFontSize, Obj.Bold, Obj.Italic ) )
      {
        OutlineTextLayout.Draw( ViewGraphics, Obj.GetLayout(), Obj.Text, font,
                                ViewOrigin, ViewZoom, Obj.LineSpacing, Obj.Color,
                                Obj.MeasuredSize().Width, Obj.Alignment );
      }
      ViewGraphics.TextRenderingHint = previousHint;
    }



    /// <summary>Unknown/broken family names degrade to the generic sans serif.</summary>
    public static Font CreateFont( string FontFamily, float FontSize, bool Bold, bool Italic )
    {
      var style = System.Drawing.FontStyle.Regular;
      if ( Bold )
      {
        style |= System.Drawing.FontStyle.Bold;
      }
      if ( Italic )
      {
        style |= System.Drawing.FontStyle.Italic;
      }
      float size = Math.Max( 1.0f, FontSize );
      try
      {
        return new Font( FontFamily, size, style );
      }
      catch ( Exception )
      {
        return new Font( System.Drawing.FontFamily.GenericSansSerif, size, style );
      }
    }



    public void DrawImageNearest( Bitmap Source, RectangleF Dest )
    {
      if ( Source == null )
      {
        return;
      }
      var previousInterpolation = m_Graphics.InterpolationMode;
      var previousOffset = m_Graphics.PixelOffsetMode;
      m_Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
      // Half-pixel offset keeps NN sampling aligned so the first/last
      // source rows aren't halved at integer magnifications.
      m_Graphics.PixelOffsetMode = PixelOffsetMode.Half;
      m_Graphics.DrawImage( Source, Dest );
      m_Graphics.InterpolationMode = previousInterpolation;
      m_Graphics.PixelOffsetMode = previousOffset;
    }



    public void Dispose()
    {
      if ( m_Graphics != null )
      {
        m_Graphics.Dispose();
        m_Graphics = null;
      }
    }
  }
}
