using System;
using System.Collections.Generic;
using System.Drawing;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// One laid-out line of a text object: a span into the SOURCE text plus the
  /// cumulative x-advance of every character (image space, char spacing
  /// included). CumulativeX has Length+1 entries; the last one is the line's
  /// exact width.
  /// </summary>
  public class OutlineTextLine
  {
    public int      Start;
    public int      Length;
    public float[]  CumulativeX;

    public float Width
    {
      get
      {
        return CumulativeX[Length];
      }
    }
  }



  /// <summary>The complete cached layout of a text object / edit buffer.</summary>
  public class OutlineTextLayoutData
  {
    public List<OutlineTextLine> Lines = new List<OutlineTextLine>();
    /// <summary>Image-space line height of the font (ascent+descent+leading).</summary>
    public float LineHeight;

    /// <summary>Advance of one line step incl. extra line spacing.</summary>
    public float LineAdvance( float LineSpacing )
    {
      return LineHeight + LineSpacing;
    }
  }



  /// <summary>
  /// The text-object layout engine. GDI+ DrawString has NO character-spacing
  /// support and its MeasureString pads results font-dependently — so text
  /// objects are laid out HERE, per character, with typographic advances:
  /// drawing, measuring, word-wrap, the caret and the flatten bake all share
  /// the same numbers, which is what guarantees "measure == draw" (no clipped
  /// right/bottom edges) and zoom-stable line breaks (layout is computed in
  /// IMAGE space once; the view only scales positions).
  /// </summary>
  public static class OutlineTextLayout
  {
    /// <summary>Narrowest allowed wrap width (image px).</summary>
    public const float MIN_WRAP_WIDTH = 16.0f;



    /// <summary>
    /// Lays out Text with the given IMAGE-space font. WrapWidth &lt;= 0 = no
    /// wrapping (hard '\n' lines only); otherwise greedy word-wrap at spaces
    /// (an unbreakable over-long word overflows rather than clips — bounds
    /// grow to contain it).
    /// </summary>
    public static OutlineTextLayoutData Layout( Graphics MetricsGraphics, Font ImageFont,
                                                string Text, float WrapWidth, float CharSpacing )
    {
      var data = new OutlineTextLayoutData();
      data.LineHeight = ImageFont.GetHeight( MetricsGraphics );
      Text = Text ?? "";

      // Per-character advance cache (typographic + trailing spaces, so a
      // space is a real width and nothing depends on string context).
      var advances = new Dictionary<char, float>();
      Func<char, float> advanceOf = c =>
      {
        float known;
        if ( advances.TryGetValue( c, out known ) )
        {
          return known;
        }
        float measured = MeasureCharAdvance( MetricsGraphics, ImageFont, c );
        advances[c] = measured;
        return measured;
      };

      int hardStart = 0;
      while ( hardStart <= Text.Length )
      {
        int hardEnd = Text.IndexOf( '\n', hardStart );
        if ( hardEnd < 0 )
        {
          hardEnd = Text.Length;
        }
        LayoutHardLine( data, Text, hardStart, hardEnd, WrapWidth, CharSpacing, advanceOf );
        if ( hardEnd >= Text.Length )
        {
          break;
        }
        hardStart = hardEnd + 1;
      }
      if ( data.Lines.Count == 0 )
      {
        data.Lines.Add( BuildLine( Text, 0, 0, CharSpacing, advanceOf ) );
      }
      return data;
    }



    private static void LayoutHardLine( OutlineTextLayoutData Data, string Text,
                                        int Start, int End, float WrapWidth, float CharSpacing,
                                        Func<char, float> AdvanceOf )
    {
      if ( WrapWidth <= 0 )
      {
        Data.Lines.Add( BuildLine( Text, Start, End - Start, CharSpacing, AdvanceOf ) );
        return;
      }
      float wrapAt = Math.Max( MIN_WRAP_WIDTH, WrapWidth );
      int lineStart = Start;
      while ( true )
      {
        if ( lineStart >= End )
        {
          if ( ( lineStart == Start )
          ||   ( lineStart == End ) )
          {
            // Empty hard line, or the final segment ended exactly at End.
            if ( lineStart == Start )
            {
              Data.Lines.Add( BuildLine( Text, lineStart, 0, CharSpacing, AdvanceOf ) );
            }
          }
          return;
        }
        float x = 0;
        int lastSpace = -1;
        int i = lineStart;
        for ( ; i < End; ++i )
        {
          char c = Text[i];
          float advance = AdvanceOf( c ) + ( ( i > lineStart ) ? CharSpacing : 0 );
          // Greedy break: this NON-space char would overflow and there is a
          // space in the line to break at.
          if ( ( c != ' ' )
          &&   ( x + advance > wrapAt )
          &&   ( lastSpace >= 0 ) )
          {
            break;
          }
          if ( c == ' ' )
          {
            lastSpace = i;
          }
          x += advance;
        }
        if ( i >= End )
        {
          // Rest fits (or is one unbreakable overflow) — final segment.
          Data.Lines.Add( BuildLine( Text, lineStart, End - lineStart, CharSpacing, AdvanceOf ) );
          return;
        }
        // Break at the last space; the space itself is consumed by the break.
        Data.Lines.Add( BuildLine( Text, lineStart, lastSpace - lineStart, CharSpacing, AdvanceOf ) );
        lineStart = lastSpace + 1;
      }
    }



    private static OutlineTextLine BuildLine( string Text, int Start, int Length,
                                              float CharSpacing, Func<char, float> AdvanceOf )
    {
      var line = new OutlineTextLine()
      {
        Start = Start,
        Length = Length,
        CumulativeX = new float[Length + 1]
      };
      float x = 0;
      for ( int i = 0; i < Length; ++i )
      {
        line.CumulativeX[i] = x;
        x += AdvanceOf( Text[Start + i] ) + ( ( i < Length - 1 ) ? CharSpacing : 0 );
      }
      line.CumulativeX[Length] = x;
      return line;
    }



    /// <summary>
    /// Exact image-space size of a layout. With an explicit WrapWidth the
    /// WIDTH IS the wrap width (the frame follows the user's set size, text
    /// wraps inside; an unbreakable overflow still widens it so nothing
    /// clips). Height accounts for line spacing.
    /// </summary>
    public static SizeF Measure( OutlineTextLayoutData Data, float WrapWidth, float LineSpacing )
    {
      float maxLine = 0;
      foreach ( var line in Data.Lines )
      {
        maxLine = Math.Max( maxLine, line.Width );
      }
      float width = ( WrapWidth > 0 ) ? Math.Max( Math.Max( MIN_WRAP_WIDTH, WrapWidth ), maxLine ) : maxLine;
      int count = Math.Max( 1, Data.Lines.Count );
      float height = count * Data.LineHeight + ( count - 1 ) * LineSpacing;
      return new SizeF( Math.Max( 1, width ), Math.Max( Data.LineHeight, height ) );
    }



    /// <summary>
    /// Draws a layout. Positions are the layout's IMAGE-space numbers scaled
    /// by ViewZoom (pass 1.0 with an image-space font to bake), so view and
    /// bake render the exact same arrangement. The caller sets the
    /// TextRenderingHint.
    /// </summary>
    public static void Draw( Graphics TargetGraphics, OutlineTextLayoutData Data, string Text,
                             Font TargetFont, PointF Origin, float ViewZoom,
                             float LineSpacing, Color Color )
    {
      if ( Color.A == 0 )
      {
        return;
      }
      using ( var brush = new SolidBrush( Color ) )
      using ( var format = TypographicFormat() )
      {
        for ( int lineIndex = 0; lineIndex < Data.Lines.Count; ++lineIndex )
        {
          var line = Data.Lines[lineIndex];
          float y = Origin.Y + lineIndex * Data.LineAdvance( LineSpacing ) * ViewZoom;
          for ( int i = 0; i < line.Length; ++i )
          {
            char c = Text[line.Start + i];
            if ( c == ' ' )
            {
              continue;
            }
            float x = Origin.X + line.CumulativeX[i] * ViewZoom;
            TargetGraphics.DrawString( c.ToString(), TargetFont, brush, x, y, format );
          }
        }
      }
    }



    /// <summary>
    /// Caret geometry inside a layout: which visual line the buffer index
    /// sits on and its x offset (image space). An index inside a consumed
    /// soft-break space lands at the start of the following line.
    /// </summary>
    public static void CaretPosition( OutlineTextLayoutData Data, int CaretIndex,
                                      out int LineIndex, out float X )
    {
      LineIndex = 0;
      X = 0;
      for ( int i = 0; i < Data.Lines.Count; ++i )
      {
        var line = Data.Lines[i];
        if ( ( CaretIndex >= line.Start )
        &&   ( CaretIndex <= line.Start + line.Length ) )
        {
          LineIndex = i;
          X = line.CumulativeX[CaretIndex - line.Start];
          return;
        }
        if ( CaretIndex < line.Start )
        {
          // Inside a consumed break character — start of this line.
          LineIndex = i;
          X = 0;
          return;
        }
      }
      if ( Data.Lines.Count > 0 )
      {
        var last = Data.Lines[Data.Lines.Count - 1];
        LineIndex = Data.Lines.Count - 1;
        X = last.Width;
      }
    }



    /// <summary>The buffer index on the given visual line closest to image-space x.</summary>
    public static int IndexAt( OutlineTextLayoutData Data, int LineIndex, float X )
    {
      if ( ( LineIndex < 0 )
      ||   ( Data.Lines.Count == 0 ) )
      {
        return 0;
      }
      LineIndex = Math.Min( LineIndex, Data.Lines.Count - 1 );
      var line = Data.Lines[LineIndex];
      int best = 0;
      float bestDist = float.MaxValue;
      for ( int i = 0; i <= line.Length; ++i )
      {
        float dist = Math.Abs( line.CumulativeX[i] - X );
        if ( dist < bestDist )
        {
          bestDist = dist;
          best = i;
        }
      }
      return line.Start + best;
    }



    /// <summary>
    /// Typographic advance of a single character — no GenericDefault padding,
    /// trailing spaces count (a lone space has real width).
    /// </summary>
    private static float MeasureCharAdvance( Graphics MetricsGraphics, Font ImageFont, char c )
    {
      if ( c == '\n' )
      {
        return 0;
      }
      using ( var format = TypographicFormat() )
      {
        return MetricsGraphics.MeasureString( c.ToString(), ImageFont,
          new SizeF( 1000000f, 1000000f ), format ).Width;
      }
    }



    private static StringFormat TypographicFormat()
    {
      var format = (StringFormat)StringFormat.GenericTypographic.Clone();
      format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
      return format;
    }



    /// <summary>
    /// Font metrics are identical on any 96-dpi Graphics — a throwaway 1×1
    /// surface is a valid metrics source when no target Graphics is at hand
    /// (caret navigation, bounds without a renderer).
    /// </summary>
    public static OutlineTextLayoutData LayoutWithTemporaryGraphics( Font ImageFont, string Text,
                                                                     float WrapWidth, float CharSpacing )
    {
      using ( var bitmap = new Bitmap( 1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb ) )
      using ( var g = Graphics.FromImage( bitmap ) )
      {
        return Layout( g, ImageFont, Text, WrapWidth, CharSpacing );
      }
    }
  }
}
