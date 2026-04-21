using GR.Image;
using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Darkens pixels in a repeating pattern along the Y axis to emulate the
  /// gap between CRT scanlines. The pattern is applied at <em>target</em>
  /// pixel granularity (not source-row granularity) so scanlines stay thin
  /// at every zoom level — matches VICE's behavior where the line count is
  /// driven by the display, not by the source resolution.
  ///
  /// Each row's darkening is a cosine gradient, not a binary on/off: for
  /// <see cref="Period"/> greater than 2 you get a smooth dark band with
  /// fading edges rather than a hard 1-pixel line. At Period=2 it
  /// degenerates to the classic every-other-row look.
  /// </summary>
  public class ScanlineFilter : DisplayFilterBase
  {
    static ScanlineFilter()
    {
      DisplayFilterRegistry.Register( typeof( ScanlineFilter ) );
    }



    /// <summary>How dark the scanline centers get, 0 (no dim) .. 100 (black).</summary>
    public int  Intensity { get; set; } = 25;

    /// <summary>Target-pixel period between scanline centers. 2 = tightest (every other row).</summary>
    public int  Period    { get; set; } = 2;

    /// <summary>Shifts the scanline pattern vertically by this many target pixels.</summary>
    public int  Offset    { get; set; } = 0;



    public override string Name
    {
      get { return "Scanlines"; }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int intensity = Math.Max( 0, Math.Min( 100, Intensity ) );
      int period    = Math.Max( 2, Math.Min( 16,  Period ) );
      int offset    = Math.Max( 0, Math.Min( 15,  Offset ) );

      int width  = ctx.TargetBuffer.Width;
      int height = ctx.TargetBuffer.Height;

      int mapTop    = ctx.RenderOffsetY;
      int mapBottom = ctx.RenderOffsetY + ctx.MapPixelHeight;

      // Precompute one multiplier per row of the *period* (1..period cycle),
      // not per pixel — cheap double math that only runs period times per
      // frame. Inner per-pixel loop then indexes this table.
      // shade weight uses (cos+1)/2 so:
      //   phase 0   → weight 1 → darkness = intensity  → mul = 1 - intensity/100
      //   phase 0.5 → weight 0 → darkness = 0          → mul = 1
      int[] rowMul = new int[period];
      for ( int i = 0; i < period; ++i )
      {
        double phase  = ( (double)i / period ) * 2.0 * Math.PI;
        double weight = ( Math.Cos( phase ) + 1.0 ) * 0.5;     // 0..1, peaks at i=0
        double factor = 1.0 - ( intensity / 100.0 ) * weight;
        int    m      = (int)Math.Round( factor * 256.0 );
        if ( m < 0 )   m = 0;
        if ( m > 256 ) m = 256;
        rowMul[i] = m;
      }

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        for ( int y = 0; y < height; ++y )
        {
          byte* srcRow = pSrc + y * srcStride;
          byte* dstRow = pDst + y * dstStride;

          // Outside the map region: copy through untouched so editor chrome
          // (letterboxing, etc.) isn't tinted.
          if ( ( y < mapTop )
          ||   ( y >= mapBottom ) )
          {
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );
            continue;
          }

          int mul = rowMul[( y - mapTop + offset ) % period];
          if ( mul == 256 )
          {
            // Peak brightness row — nothing to scale.
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );
            continue;
          }

          // 32bpp BGRA: byte 0=B, 1=G, 2=R, 3=A.
          byte* s = srcRow;
          byte* d = dstRow;
          for ( int x = 0; x < width; ++x )
          {
            d[0] = (byte)( ( s[0] * mul ) >> 8 );
            d[1] = (byte)( ( s[1] * mul ) >> 8 );
            d[2] = (byte)( ( s[2] * mul ) >> 8 );
            d[3] = s[3];
            s += 4;
            d += 4;
          }
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Intensity );
      buf.AppendI32( Period );
      buf.AppendI32( Offset );
      return buf;
    }



    public override void LoadParameters( ByteBuffer buf )
    {
      if ( ( buf == null )
      ||   ( buf.Length == 0 ) )
      {
        return;
      }
      var r = buf.MemoryReader();
      Intensity = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 )
      {
        Period = GR.MathUtil.Clamp( 2, r.ReadInt32(), 16 );
      }
      if ( r.Size - r.Position >= 4 )
      {
        Offset = GR.MathUtil.Clamp( 0, r.ReadInt32(), 15 );
      }
    }



    public override IDisplayFilter Clone()
    {
      return new ScanlineFilter
      {
        Enabled   = this.Enabled,
        Intensity = this.Intensity,
        Period    = this.Period,
        Offset    = this.Offset,
      };
    }
  }
}
