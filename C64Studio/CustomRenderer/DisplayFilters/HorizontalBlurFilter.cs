using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Horizontal-only blur. Emulates a CRT beam's finite horizontal spot size,
  /// which slightly smears adjacent pixels along the scan direction. Commonly
  /// chained BEFORE <see cref="ScanlineFilter"/> so the scanline dark bands
  /// get blurred too, giving a softer "glowing" line look rather than a hard
  /// stripe.
  ///
  /// Implementation is a 3-tap symmetric kernel with user-selectable strength:
  /// 0% returns the source unchanged, 100% uses the pure box kernel
  /// (prev + 2*cur + next) / 4. Strengths in between mix between the two.
  /// One raster pass, zero allocations in the hot loop.
  /// </summary>
  public class HorizontalBlurFilter : DisplayFilterBase
  {
    static HorizontalBlurFilter()
    {
      DisplayFilterRegistry.Register( typeof( HorizontalBlurFilter ) );
    }



    /// <summary>
    /// Blur amount, 0 (no blur) .. 100 (full box). 50 gives VICE-like softness.
    /// </summary>
    public int Strength { get; set; } = 50;



    public override string Name
    {
      get { return "Horizontal Blur"; }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int strength = Math.Max( 0, Math.Min( 100, Strength ) );
      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      if ( ( strength == 0 )
      ||   ( width < 3 ) )
      {
        // No-op fast path: just copy source → target so the pipeline's
        // ping-pong invariant still holds.
        CopyThrough( ctx );
        return;
      }

      // Blend coefficients as 256-scale integers.
      //   source share s = 256 * (100 - strength) / 100
      //   blurred share b = 256 * strength / 100
      //   blurred(x) = (s[x-1] + 2*s[x] + s[x+1]) / 4
      //   out(x)     = (s[x] * s_share + blurred(x) * b_share) / 256
      // Simplify: per channel,
      //   out = (s[x] * wCenter + s[x-1] * wSide + s[x+1] * wSide) >> 8
      // where wSide = b_share / 4, wCenter = s_share + b_share / 2.
      int bShare  = ( 256 * strength ) / 100;
      int sShare  = 256 - bShare;
      int wSide   = bShare / 4;
      int wCenter = sShare + ( bShare / 2 );
      // Rounding residue: sShare + 2*(bShare/4) + bShare/2 might drift a
      // couple of units off 256 due to integer division. Push any residue
      // into wCenter so pure-gray inputs stay pure gray.
      int total = wCenter + 2 * wSide;
      wCenter += ( 256 - total );

      int mapLeft  = ctx.RenderOffsetX;
      int mapRight = ctx.RenderOffsetX + ctx.MapPixelWidth;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        for ( int y = 0; y < height; ++y )
        {
          byte* s = pSrc + y * srcStride;
          byte* d = pDst + y * dstStride;

          for ( int x = 0; x < width; ++x )
          {
            // Pixels outside the map region are copied unblurred — we don't
            // want the chrome borders to smear.
            if ( ( x < mapLeft )
            ||   ( x >= mapRight ) )
            {
              d[0] = s[0];
              d[1] = s[1];
              d[2] = s[2];
              d[3] = s[3];
              s += 4;
              d += 4;
              continue;
            }

            // Clamp at map edges so blur doesn't pull chrome pixels in.
            int xm = ( x > mapLeft  ) ? -1 : 0;
            int xp = ( x < mapRight - 1 ) ? +1 : 0;

            byte* left  = s + xm * 4;
            byte* right = s + xp * 4;

            int b = ( s[0] * wCenter + left[0] * wSide + right[0] * wSide ) >> 8;
            int g = ( s[1] * wCenter + left[1] * wSide + right[1] * wSide ) >> 8;
            int r = ( s[2] * wCenter + left[2] * wSide + right[2] * wSide ) >> 8;
            if ( b > 255 ) b = 255;
            if ( g > 255 ) g = 255;
            if ( r > 255 ) r = 255;
            d[0] = (byte)b;
            d[1] = (byte)g;
            d[2] = (byte)r;
            d[3] = s[3];

            s += 4;
            d += 4;
          }
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    private static void CopyThrough( FilterContext ctx )
    {
      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   bytes = ctx.SourceBuffer.BytesPerLine * ctx.SourceBuffer.Height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );
        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Strength );
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
      Strength = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new HorizontalBlurFilter
      {
        Enabled  = this.Enabled,
        Strength = this.Strength,
      };
    }
  }
}
