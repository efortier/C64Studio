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
  /// <para>
  /// <see cref="Sigma"/> is specified in SOURCE pixels (×10) and the kernel
  /// is scaled by the current zoom, so the blur means the same thing at
  /// every magnification — 0.8 source pixels of smear looks like 0.8 source
  /// pixels whether the map is at 100% or 400%. (The old target-pixel
  /// kernel was invisible at high zoom and mushy at low zoom.) The kernel
  /// tap count derives from sigma automatically.
  /// </para>
  /// </summary>
  public class HorizontalBlurFilter : DisplayFilterBase
  {
    static HorizontalBlurFilter()
    {
      DisplayFilterRegistry.Register( typeof( HorizontalBlurFilter ) );
    }



    /// <summary>Wet/dry mix, 0 (no blur) .. 100 (fully blurred).</summary>
    public int Strength { get; set; } = 50;

    /// <summary>Gaussian σ × 10 in SOURCE pixels. Range 2..40 ⇒ σ 0.2..4.0 source px.</summary>
    public int Sigma { get; set; } = 8;



    // Kernel cache — rebuilt when strength, sigma or zoom changes.
    private int[]  m_Kernel;
    private int    m_KernelRadius;
    private int    m_KernelKeyStrength = int.MinValue;
    private int    m_KernelKeySigma    = int.MinValue;
    private int    m_KernelKeyZoomQ    = int.MinValue;



    public override string Name
    {
      get { return "Horizontal Blur"; }
    }



    private void BuildKernelIfNeeded( int strength, int sigmaX10, double zoomX )
    {
      int zoomQ = (int)( zoomX * 1024.0 );
      if ( ( m_Kernel != null )
      &&   ( m_KernelKeyStrength == strength )
      &&   ( m_KernelKeySigma    == sigmaX10 )
      &&   ( m_KernelKeyZoomQ    == zoomQ ) )
      {
        return;
      }

      double sigmaT = ( sigmaX10 / 10.0 ) * zoomX;      // σ in target pixels
      int    radius = (int)Math.Ceiling( 2.5 * sigmaT );
      if ( radius < 1 )  radius = 1;
      if ( radius > 31 ) radius = 31;
      int tapCount = 2 * radius + 1;

      // Combined kernel: Gaussian × wet share, dry share added to the
      // center tap, rounding residue swept into the center so flat input
      // stays flat. All 256-scale integer.
      var combined = new int[tapCount];
      {
        double[] g = new double[tapCount];
        double   gSum = 0.0;
        double   twoSigmaSq = 2.0 * Math.Max( 0.05, sigmaT ) * Math.Max( 0.05, sigmaT );
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
        combined[radius] += ( 256 - totalW );

        int bShare = ( 256 * strength ) / 100;
        int sShare = 256 - bShare;
        for ( int i = 0; i < tapCount; ++i )
        {
          combined[i] = ( combined[i] * bShare ) >> 8;
        }
        combined[radius] += sShare;

        int comboTotal = 0;
        for ( int i = 0; i < tapCount; ++i )
        {
          comboTotal += combined[i];
        }
        combined[radius] += ( 256 - comboTotal );
      }

      m_Kernel            = combined;
      m_KernelRadius      = radius;
      m_KernelKeyStrength = strength;
      m_KernelKeySigma    = sigmaX10;
      m_KernelKeyZoomQ    = zoomQ;
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
      int sigmaX10 = Math.Max( 2, Math.Min( 40, Sigma ) );

      if ( ( strength == 0 )
      ||   ( ctx.MapPixelWidth <= 0 )
      ||   ( ctx.SourceWidth <= 0 ) )
      {
        CopyThrough( ctx );
        return;
      }

      double zoomX  = ctx.MapPixelWidth / (double)ctx.SourceWidth;
      double sigmaT = ( sigmaX10 / 10.0 ) * zoomX;
      if ( sigmaT < 0.08 )
      {
        // Sub-visible at this zoom — nothing worth convolving.
        CopyThrough( ctx );
        return;
      }

      BuildKernelIfNeeded( strength, sigmaX10, zoomX );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = ctx.RenderOffsetY;
      int mapBot   = ctx.RenderOffsetY + ctx.MapPixelHeight;
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int radius   = m_KernelRadius;
      int tapCount = 2 * radius + 1;

      if ( mapRight - mapLeft <= 1 )
      {
        CopyThrough( ctx );
        return;
      }

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( int* kernel = m_Kernel )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* srcRow = pSrc + y * srcStride;
            byte* dstRow = pDst + y * dstStride;

            // Full-row copy first — chrome above/below the map must not be
            // smeared (the old code blurred those rows too); rows inside
            // the map get their map span re-computed below.
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );

            if ( ( y < mapTop )
            ||   ( y >= mapBot ) )
            {
              continue;
            }

            for ( int x = mapLeft; x < mapRight; ++x )
            {
              int b = 0, g = 0, r = 0;
              for ( int i = 0; i < tapCount; ++i )
              {
                int xi = x + i - radius;
                // Clamp to map edges so the blur doesn't pull chrome pixels
                // into the map or vice versa. Equivalent to "extend edge".
                if ( xi < mapLeft )        xi = mapLeft;
                else if ( xi >= mapRight ) xi = mapRight - 1;

                byte* sp = srcRow + xi * 4;
                int   w  = kernel[i];
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
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    // ================================================================
    // Persistence
    //
    // Legacy layout: (Strength, Taps, SigmaTargetPx). Taps is obsolete
    // (derived from sigma now) and Sigma changed meaning to source pixels,
    // so the new value is APPENDED as a fourth field. The legacy slots are
    // still written with in-range values so an older build reading a newer
    // settings file behaves sanely; on load the legacy sigma seeds the new
    // field when the fourth int is absent (old file).
    // ================================================================

    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Strength );
      buf.AppendI32( 7 );                                        // legacy Taps slot
      buf.AppendI32( GR.MathUtil.Clamp( 5, Sigma, 30 ) );        // legacy Sigma slot
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
      if ( r.Size - r.Position >= 4 )
      {
        r.ReadInt32();      // legacy Taps — ignored, derived from sigma now
      }
      if ( r.Size - r.Position >= 4 )
      {
        // Legacy target-px sigma: best available seed for old files; the
        // appended source-px value overrides it right after when present.
        Sigma = GR.MathUtil.Clamp( 2, r.ReadInt32(), 40 );
      }
      if ( r.Size - r.Position >= 4 )
      {
        Sigma = GR.MathUtil.Clamp( 2, r.ReadInt32(), 40 );
      }
    }



    public override IDisplayFilter Clone()
    {
      return new HorizontalBlurFilter
      {
        Enabled  = this.Enabled,
        Strength = this.Strength,
        Sigma    = this.Sigma,
      };
    }
  }
}
