using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// 1D horizontal Gaussian blur. Emulates the CRT beam's finite horizontal
  /// spot size and the limited horizontal bandwidth of composite video, both
  /// of which smear adjacent pixels along the scan direction. Commonly
  /// chained BEFORE <see cref="ScanlineFilter"/> so the scanline dark bands
  /// also get softened.
  ///
  /// Three knobs:
  /// <list type="bullet">
  ///   <item><see cref="Strength"/> — wet/dry mix (0 passthrough, 100 pure blurred).</item>
  ///   <item><see cref="Taps"/>    — kernel width in pixels (odd; 3 = narrow, 15 = wide).</item>
  ///   <item><see cref="Sigma"/>   — Gaussian σ × 10. Low = peaked, high = flatter.</item>
  /// </list>
  /// Defaults land on a real, visible blur — the old 3-tap (1,2,1)/4 kernel
  /// was σ≈0.7, barely perceptible on pixel art. For VICE-like softness try
  /// Taps=11 / Sigma=25.
  /// </summary>
  public class HorizontalBlurFilter : DisplayFilterBase
  {
    static HorizontalBlurFilter()
    {
      DisplayFilterRegistry.Register( typeof( HorizontalBlurFilter ) );
    }



    /// <summary>Wet/dry mix, 0 (no blur) .. 100 (fully blurred).</summary>
    public int Strength { get; set; } = 50;

    /// <summary>Kernel width in pixels. Clamped to the nearest odd value in [3, 15].</summary>
    public int Taps { get; set; } = 7;

    /// <summary>
    /// Gaussian σ × 10. Range 5..30 ⇒ σ 0.5..3.0. A good starting Gaussian
    /// for a given tap count is roughly σ ≈ (Taps - 1) / 6, i.e. 7 taps ⇒ σ≈1.0.
    /// </summary>
    public int Sigma { get; set; } = 12;



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
      int tapCount = Math.Max( 3, Math.Min( 15, Taps ) );
      if ( ( tapCount & 1 ) == 0 ) --tapCount;    // force odd
      int   radius = tapCount / 2;
      double sigma = Math.Max( 0.5, Math.Min( 3.0, Sigma / 10.0 ) );

      int width  = ctx.TargetBuffer.Width;
      int height = ctx.TargetBuffer.Height;

      if ( ( strength == 0 )
      ||   ( width <= radius + 1 ) )
      {
        // No-op fast path: copy source → target so the pipeline's ping-pong
        // invariant still holds.
        CopyThrough( ctx );
        return;
      }

      // Build the combined kernel: Gaussian weights times "blurred share",
      // plus the "source share" added to the center tap. One sum-reduce per
      // pixel, no separate blend step. All arithmetic in 256-scale integer.
      //
      //   w_gauss[i] ∝ exp(-(i-radius)² / (2σ²))      i=0..tapCount-1
      //   w_gauss normalized so Σ = 256
      //   bShare    = 256 * strength / 100
      //   combined[i] = (w_gauss[i] * bShare) >> 8
      //   combined[radius] += 256 - bShare                  // source share
      //
      // Any rounding residue (combined total ≠ 256) is swept into the center
      // tap so a solid-gray input stays solid gray.
      int[] combined = new int[tapCount];
      {
        double[] g = new double[tapCount];
        double   gSum = 0.0;
        double   twoSigmaSq = 2.0 * sigma * sigma;
        for ( int i = 0; i < tapCount; ++i )
        {
          int d = i - radius;
          g[i] = Math.Exp( -( d * d ) / twoSigmaSq );
          gSum += g[i];
        }
        int totalW = 0;
        for ( int i = 0; i < tapCount; ++i )
        {
          combined[i] = (int)Math.Round( g[i] / gSum * 256.0 );
          totalW     += combined[i];
        }
        combined[radius] += ( 256 - totalW );   // residue → center

        int bShare = ( 256 * strength ) / 100;
        int sShare = 256 - bShare;
        for ( int i = 0; i < tapCount; ++i )
        {
          combined[i] = ( combined[i] * bShare ) >> 8;
        }
        combined[radius] += sShare;

        // Second residue pass after the strength blend.
        int comboTotal = 0;
        for ( int i = 0; i < tapCount; ++i )
        {
          comboTotal += combined[i];
        }
        combined[radius] += ( 256 - comboTotal );
      }

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
          byte* srcRow = pSrc + y * srcStride;
          byte* dstRow = pDst + y * dstStride;

          for ( int x = 0; x < width; ++x )
          {
            // Outside the map: copy untouched so chrome borders don't smear.
            if ( ( x < mapLeft )
            ||   ( x >= mapRight ) )
            {
              byte* s = srcRow + x * 4;
              byte* d = dstRow + x * 4;
              d[0] = s[0];
              d[1] = s[1];
              d[2] = s[2];
              d[3] = s[3];
              continue;
            }

            int b = 0, g = 0, r = 0;
            for ( int i = 0; i < tapCount; ++i )
            {
              int xi = x + i - radius;
              // Clamp to map edges so the blur doesn't pull chrome pixels
              // into the map or vice versa. Equivalent to "extend edge".
              if ( xi < mapLeft )       xi = mapLeft;
              else if ( xi >= mapRight ) xi = mapRight - 1;

              byte* sp = srcRow + xi * 4;
              int   w  = combined[i];
              b += w * sp[0];
              g += w * sp[1];
              r += w * sp[2];
            }
            b >>= 8;
            g >>= 8;
            r >>= 8;
            if ( b > 255 ) b = 255;
            if ( g > 255 ) g = 255;
            if ( r > 255 ) r = 255;

            byte* dp = dstRow + x * 4;
            dp[0] = (byte)b;
            dp[1] = (byte)g;
            dp[2] = (byte)r;
            dp[3] = ( srcRow + x * 4 )[3];
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
      buf.AppendI32( Taps );
      buf.AppendI32( Sigma );
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
      // Taps and Sigma were added after Strength — old files lacking them
      // fall through to the field defaults, keeping visual parity.
      if ( r.Size - r.Position >= 4 )
      {
        Taps = GR.MathUtil.Clamp( 3, r.ReadInt32(), 15 );
      }
      if ( r.Size - r.Position >= 4 )
      {
        Sigma = GR.MathUtil.Clamp( 5, r.ReadInt32(), 30 );
      }
    }



    public override IDisplayFilter Clone()
    {
      return new HorizontalBlurFilter
      {
        Enabled  = this.Enabled,
        Strength = this.Strength,
        Taps     = this.Taps,
        Sigma    = this.Sigma,
      };
    }
  }
}
