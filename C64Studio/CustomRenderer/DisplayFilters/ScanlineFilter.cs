using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// CRT scanline emulation aligned to the SOURCE raster: one scanline per
  /// C64 pixel row, exactly like the beam draws it. Each target row inside
  /// a source row gets a Gaussian beam profile — bright at the line center,
  /// falling off toward the gap between lines.
  ///
  /// <para>
  /// The beam width is modulated by pixel luminance ("beam bloom"): bright
  /// pixels drive the electron beam harder, widening its spot until the gap
  /// between lines nearly closes; dark pixels keep a thin line with a wide
  /// gap. This luminance coupling is what separates a CRT look from a
  /// striped overlay — every serious CRT shader (crt-geom, Lottes,
  /// Guest-Advanced) does it.
  /// </para>
  /// <para>
  /// Removing light darkens the picture, so <see cref="PreserveBrightness"/>
  /// boosts the beam peak per luminance band to keep the average close to
  /// the unfiltered image (capped so saturated colors don't blow out).
  /// </para>
  /// <para>
  /// Below 2 target pixels per source row there is no room to draw a gap —
  /// a fixed pattern would just delete image rows — so the effect fades out
  /// linearly between 1x and 2x zoom and becomes a plain copy at 1x.
  /// </para>
  /// </summary>
  public class ScanlineFilter : DisplayFilterBase
  {
    static ScanlineFilter()
    {
      DisplayFilterRegistry.Register( typeof( ScanlineFilter ) );
    }



    private const int SUBROWS    = 64;
    private const int LUMA_STEPS = 32;


    /// <summary>How dark the gap between scanlines gets, 0 (no effect) .. 100 (black gap).</summary>
    public int  Intensity          { get; set; } = 50;

    /// <summary>Beam width for BLACK pixels, percent of the line pitch. Small = thin line, wide gap.</summary>
    public int  BeamWidthMin       { get; set; } = 45;

    /// <summary>Beam width for WHITE pixels, percent of the line pitch. Above ~100 the gap closes on bright areas.</summary>
    public int  BeamWidthMax       { get; set; } = 115;

    /// <summary>How much of the lost average brightness gets compensated by boosting the beam peak, 0..100.</summary>
    public int  PreserveBrightness { get; set; } = 70;



    // Beam profile LUT: [subrow within the scanline 0..63][luminance band
    // 0..31] → multiplier in 256-scale (may exceed 256 when brightness
    // preservation boosts the peak; capped at 512). Rebuilt only when a
    // parameter or the zoom-dependent effective intensity changes — the
    // README's "precompute on parameter change, not per frame" rule.
    private ushort[]  m_Lut;
    private int       m_LutKeyIntensity = int.MinValue;
    private int       m_LutKeyWidthMin  = int.MinValue;
    private int       m_LutKeyWidthMax  = int.MinValue;
    private int       m_LutKeyPreserve  = int.MinValue;



    public override string Name
    {
      get { return "Scanlines"; }
    }



    private void BuildLutIfNeeded( int effIntensity, int widthMin, int widthMax, int preserve )
    {
      if ( ( m_Lut != null )
      &&   ( m_LutKeyIntensity == effIntensity )
      &&   ( m_LutKeyWidthMin  == widthMin )
      &&   ( m_LutKeyWidthMax  == widthMax )
      &&   ( m_LutKeyPreserve  == preserve ) )
      {
        return;
      }
      if ( m_Lut == null )
      {
        m_Lut = new ushort[SUBROWS * LUMA_STEPS];
      }

      double intensity = effIntensity / 100.0;
      double preserveF = preserve / 100.0;

      for ( int lumaIdx = 0; lumaIdx < LUMA_STEPS; ++lumaIdx )
      {
        double luma01 = lumaIdx / (double)( LUMA_STEPS - 1 );
        // Beam width as a fraction of the line pitch, luminance-lerped.
        // FWHM → Gaussian sigma: sigma = (width/2) / sqrt(2·ln2).
        double width  = ( widthMin + ( widthMax - widthMin ) * luma01 ) / 100.0;
        double sigma  = Math.Max( 0.02, width * 0.4247 );
        double twoSigmaSq = 2.0 * sigma * sigma;

        // First pass: raw profile per subrow, accumulate the mean so the
        // brightness-preservation boost can be derived per luminance band.
        double[] mul01 = new double[SUBROWS];
        double   mean  = 0.0;
        for ( int subrow = 0; subrow < SUBROWS; ++subrow )
        {
          double frac = ( subrow + 0.5 ) / SUBROWS;
          double d    = frac - 0.5;
          // Own line's beam plus the two neighboring lines' beams — wide
          // beams overlap into the gap, which is exactly how bright areas
          // close their scanline gaps on a real tube.
          double s = Math.Exp( -( d * d ) / twoSigmaSq )
                   + Math.Exp( -( ( d - 1.0 ) * ( d - 1.0 ) ) / twoSigmaSq )
                   + Math.Exp( -( ( d + 1.0 ) * ( d + 1.0 ) ) / twoSigmaSq );
          if ( s > 1.0 )
          {
            s = 1.0;
          }
          mul01[subrow] = ( 1.0 - intensity ) + intensity * s;
          mean += mul01[subrow];
        }
        mean /= SUBROWS;

        double boost = 1.0;
        if ( mean > 0.0001 )
        {
          boost = 1.0 + preserveF * ( 1.0 / mean - 1.0 );
        }
        if ( boost > 2.0 )
        {
          boost = 2.0;
        }

        for ( int subrow = 0; subrow < SUBROWS; ++subrow )
        {
          int entry = (int)Math.Round( mul01[subrow] * boost * 256.0 );
          if ( entry < 0 )   entry = 0;
          if ( entry > 512 ) entry = 512;
          m_Lut[subrow * LUMA_STEPS + lumaIdx] = (ushort)entry;
        }
      }

      m_LutKeyIntensity = effIntensity;
      m_LutKeyWidthMin  = widthMin;
      m_LutKeyWidthMax  = widthMax;
      m_LutKeyPreserve  = preserve;
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
      int widthMin  = Math.Max( 10, Math.Min( 160, BeamWidthMin ) );
      int widthMax  = Math.Max( 10, Math.Min( 160, BeamWidthMax ) );
      if ( widthMax < widthMin )
      {
        int tmp = widthMin;
        widthMin = widthMax;
        widthMax = tmp;
      }
      int preserve = Math.Max( 0, Math.Min( 100, PreserveBrightness ) );

      // Scanlines are per SOURCE row; rowsPerLine is how many target rows
      // one source row spans at the current zoom. Below 2 there is no room
      // for a gap, so fade the effect out toward 1x instead of decimating
      // the image with a pattern finer than its pixels.
      if ( ( ctx.MapPixelHeight <= 0 )
      ||   ( ctx.SourceHeight <= 0 ) )
      {
        CopyThrough( ctx );
        return;
      }
      double rowsPerLine = ctx.MapPixelHeight / (double)ctx.SourceHeight;
      double fade        = Math.Max( 0.0, Math.Min( 1.0, rowsPerLine - 1.0 ) );
      int    effIntensity = (int)Math.Round( intensity * fade );
      if ( effIntensity == 0 )
      {
        CopyThrough( ctx );
        return;
      }

      BuildLutIfNeeded( effIntensity, widthMin, widthMax, preserve );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = ctx.RenderOffsetY;
      int mapBot   = ctx.RenderOffsetY + ctx.MapPixelHeight;
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( ushort* pLut = m_Lut )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* srcRow = pSrc + y * srcStride;
            byte* dstRow = pDst + y * dstStride;

            // Copy the full row first — chrome above/below AND left/right
            // of the map stays untouched; the map span is then overwritten
            // from the source below.
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );

            if ( ( y < mapTop )
            ||   ( y >= mapBot ) )
            {
              continue;
            }

            // Position of this target row inside its source scanline. The
            // visible map top is always char-aligned (integer source rows),
            // so anchoring to the map rect anchors to content rows too —
            // scrolling never slides the scanline pattern across pixels.
            double srcPos = ( y - mapTop + 0.5 ) * ctx.SourceHeight / (double)ctx.MapPixelHeight;
            double frac   = srcPos - Math.Floor( srcPos );
            int    subrow = (int)( frac * SUBROWS );
            if ( subrow > SUBROWS - 1 )
            {
              subrow = SUBROWS - 1;
            }
            ushort* lutSlice = pLut + subrow * LUMA_STEPS;

            byte* s = srcRow + mapLeft * 4;
            byte* d = dstRow + mapLeft * 4;
            for ( int x = mapLeft; x < mapRight; ++x )
            {
              // Fast integer luma: (2R + 5G + B) / 8, then into 32 bands.
              int luma = ( 2 * s[2] + 5 * s[1] + s[0] ) >> 3;
              int mul  = lutSlice[luma >> 3];

              int b = ( s[0] * mul ) >> 8;
              int g = ( s[1] * mul ) >> 8;
              int r = ( s[2] * mul ) >> 8;
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
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    // ================================================================
    // Persistence
    //
    // Legacy layout was (Intensity, Period, Offset) — Period/Offset made
    // sense for the old fixed-target-pixel pattern and are meaningless for
    // the source-aligned beam. We still WRITE placeholder values in those
    // slots so an older build reading a newer settings file gets sane
    // values, and we SKIP them on load. New fields append after.
    // ================================================================

    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Intensity );
      buf.AppendI32( 2 );     // legacy Period slot
      buf.AppendI32( 0 );     // legacy Offset slot
      buf.AppendI32( BeamWidthMin );
      buf.AppendI32( BeamWidthMax );
      buf.AppendI32( PreserveBrightness );
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
        r.ReadInt32();        // legacy Period — ignored
      }
      if ( r.Size - r.Position >= 4 )
      {
        r.ReadInt32();        // legacy Offset — ignored
      }
      if ( r.Size - r.Position >= 4 )
      {
        BeamWidthMin = GR.MathUtil.Clamp( 10, r.ReadInt32(), 160 );
      }
      if ( r.Size - r.Position >= 4 )
      {
        BeamWidthMax = GR.MathUtil.Clamp( 10, r.ReadInt32(), 160 );
      }
      if ( r.Size - r.Position >= 4 )
      {
        PreserveBrightness = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      }
    }



    public override IDisplayFilter Clone()
    {
      return new ScanlineFilter
      {
        Enabled            = this.Enabled,
        Intensity          = this.Intensity,
        BeamWidthMin       = this.BeamWidthMin,
        BeamWidthMax       = this.BeamWidthMax,
        PreserveBrightness = this.PreserveBrightness,
      };
    }
  }
}
