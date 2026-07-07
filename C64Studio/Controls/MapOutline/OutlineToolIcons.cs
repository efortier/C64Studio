using System;
using System.Drawing;
using System.Drawing.Drawing2D;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Runtime-drawn 16×16 glyphs for the outline toolbar. Drawn in code
  /// (not .resx) so they render crisply in a single light ink that reads
  /// on the dark theme — the same reason ToolIconTheming re-inks the map
  /// toolbar's resource icons. Each call returns a fresh bitmap; the
  /// caller (button) keeps it alive.
  /// </summary>
  public static class OutlineToolIcons
  {
    private static readonly Color INK = Color.Gainsboro;



    private static Bitmap NewIcon( out Graphics g )
    {
      var icon = new Bitmap( 16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
      g = Graphics.FromImage( icon );
      g.SmoothingMode = SmoothingMode.AntiAlias;
      return icon;
    }



    public static Bitmap Brush()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 2 ) )
      using ( var fill = new SolidBrush( INK ) )
      {
        // Handle from top-right down to the tip at bottom-left.
        g.DrawLine( pen, 13, 2, 7, 8 );
        // Bristle wedge.
        g.FillPolygon( fill, new PointF[] { new PointF( 7, 8 ), new PointF( 9, 10 ), new PointF( 4, 13 ), new PointF( 2, 14 ), new PointF( 3, 11 ) } );
      }
      return icon;
    }



    public static Bitmap Eraser()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.6f ) )
      using ( var fill = new SolidBrush( Color.FromArgb( 110, INK ) ) )
      {
        // Slanted eraser block with a darker "rubber" half.
        var block = new PointF[] { new PointF( 5, 3 ), new PointF( 13, 7 ), new PointF( 10, 13 ), new PointF( 2, 9 ) };
        g.FillPolygon( fill, new PointF[] { new PointF( 3.5f, 6 ), new PointF( 11.5f, 10 ), new PointF( 10, 13 ), new PointF( 2, 9 ) } );
        g.DrawPolygon( pen, block );
      }
      return icon;
    }



    public static Bitmap RectErase()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.4f ) )
      using ( var fill = new SolidBrush( Color.FromArgb( 110, INK ) ) )
      {
        pen.DashStyle = DashStyle.Dash;
        g.DrawRectangle( pen, 2, 3, 11, 9 );
        g.FillRectangle( fill, 3, 4, 10, 8 );
      }
      return icon;
    }



    public static Bitmap RectangleOutline()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.8f ) )
      {
        g.DrawRectangle( pen, 2, 4, 12, 8 );
      }
      return icon;
    }



    public static Bitmap EllipseOutline()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.8f ) )
      {
        g.DrawEllipse( pen, 2, 3, 12, 10 );
      }
      return icon;
    }



    public static Bitmap Text()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var font = new Font( FontFamily.GenericSansSerif, 10, FontStyle.Bold, GraphicsUnit.Pixel ) )
      using ( var fill = new SolidBrush( INK ) )
      {
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var size = g.MeasureString( "A", font );
        g.DrawString( "A", font, fill, ( 16 - size.Width ) / 2, ( 15 - size.Height ) / 2 );
        g.DrawLine( Pens.Gainsboro, 3, 13, 13, 13 );
      }
      return icon;
    }



    public static Bitmap TileStamp()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var fill = new SolidBrush( INK ) )
      using ( var faint = new SolidBrush( Color.FromArgb( 110, INK ) ) )
      {
        g.FillRectangle( fill, 2, 2, 5, 5 );
        g.FillRectangle( faint, 9, 2, 5, 5 );
        g.FillRectangle( faint, 2, 9, 5, 5 );
        g.FillRectangle( fill, 9, 9, 5, 5 );
      }
      return icon;
    }



    public static Bitmap CopyImage()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.5f ) )
      {
        // Two offset pages.
        g.DrawRectangle( pen, 5, 2, 8, 9 );
        g.DrawRectangle( pen, 2, 5, 8, 9 );
      }
      return icon;
    }



    public static Bitmap PasteImage()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.5f ) )
      using ( var fill = new SolidBrush( Color.FromArgb( 110, INK ) ) )
      {
        // Clipboard with clip tab + inserted page.
        g.DrawRectangle( pen, 2, 3, 10, 11 );
        g.FillRectangle( fill, 5, 1, 4, 3 );
        g.DrawRectangle( pen, 7, 7, 7, 7 );
      }
      return icon;
    }



    public static Bitmap DeletePicture()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.5f ) )
      {
        // Trash can: lid, handle, body, two slats.
        g.DrawLine( pen, 2, 4, 14, 4 );
        g.DrawLine( pen, 6, 2, 10, 2 );
        g.DrawLines( pen, new PointF[] { new PointF( 4, 5 ), new PointF( 5, 14 ), new PointF( 11, 14 ), new PointF( 12, 5 ) } );
        g.DrawLine( pen, 7, 7, 7, 12 );
        g.DrawLine( pen, 9, 7, 9, 12 );
      }
      return icon;
    }



    public static Bitmap ZoomReset()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var font = new Font( FontFamily.GenericSansSerif, 8, FontStyle.Bold, GraphicsUnit.Pixel ) )
      using ( var fill = new SolidBrush( INK ) )
      {
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var size = g.MeasureString( "1:1", font );
        g.DrawString( "1:1", font, fill, ( 16 - size.Width ) / 2, ( 16 - size.Height ) / 2 );
      }
      return icon;
    }



    public static Bitmap MarqueeSelect()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.4f ) )
      {
        pen.DashStyle = DashStyle.Dash;
        g.DrawRectangle( pen, 2, 3, 12, 10 );
      }
      return icon;
    }



    public static Bitmap CropToSelection()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.6f ) )
      using ( var faint = new Pen( Color.FromArgb( 110, INK ), 1.2f ) )
      {
        // Classic crop glyph: two overlapping L brackets + cut diagonal.
        g.DrawLines( pen, new PointF[] { new PointF( 4, 1 ), new PointF( 4, 11 ), new PointF( 15, 11 ) } );
        g.DrawLines( pen, new PointF[] { new PointF( 1, 4 ), new PointF( 11, 4 ), new PointF( 11, 15 ) } );
        faint.DashStyle = DashStyle.Dot;
        g.DrawLine( faint, 5, 5, 10, 10 );
      }
      return icon;
    }



    public static Bitmap CenterView()
    {
      var icon = NewIcon( out Graphics g );
      using ( g )
      using ( var pen = new Pen( INK, 1.5f ) )
      {
        // Crosshair target: circle + four ticks + center dot.
        g.DrawEllipse( pen, 4, 4, 8, 8 );
        g.DrawLine( pen, 8, 1, 8, 4 );
        g.DrawLine( pen, 8, 12, 8, 15 );
        g.DrawLine( pen, 1, 8, 4, 8 );
        g.DrawLine( pen, 12, 8, 15, 8 );
        g.FillEllipse( Brushes.Gainsboro, 7, 7, 2, 2 );
      }
      return icon;
    }
  }
}
