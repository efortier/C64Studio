using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// True composite-signal simulation of the C64's video path — the same
  /// approach behind the reference renders at hitmen.c02.at/temp/palstuff.
  /// Instead of approximating individual artifacts, each scanline is
  /// ENCODED into a composite waveform (luma + modulated chroma subcarrier)
  /// and then DECODED the way a TV does. Every classic artifact emerges
  /// from the physics:
  ///
  /// <list type="bullet">
  ///   <item><b>PAL blending</b> — the decoder's delay line averages each
  ///   line's demodulated chroma with the previous line's, melting
  ///   alternating-line dithers into solid colors.</item>
  ///   <item><b>Odd/even line ripple</b> — the VIC-II's chroma phase
  ///   alternation isn't a perfect 180° (<see cref="PhaseError"/>), so
  ///   saturation/hue ripples between lines and alternating-line mixes
  ///   depend on which color is on odd vs even lines.</item>
  ///   <item><b>Dot crawl / cross-color</b> — the subcarrier can't be fully
  ///   notched out of luma, and sharp luma detail demodulates into phantom
  ///   color (composite connection only; S-Video keeps Y and C apart).</item>
  ///   <item><b>Asymmetric edges</b> — the output stage rises slowly
  ///   (≈2 px) and falls fast (≈0.5 px), so dense hires patterns render
  ///   DARKER (the famous "emulation still sucks" hidden-message test).</item>
  /// </list>
  ///
  /// The signal lives in SOURCE space (one scanline = one C64 pixel row):
  /// the map rect is downsampled to source resolution, synthesized at
  /// 8 samples per pixel, decoded, and upsampled back — so the simulation
  /// is zoom-independent and cheap enough for the editor's animation rate
  /// at typical review zooms.
  /// </summary>
  public class PALCompositeFilter : DisplayFilterBase
  {
    static PALCompositeFilter()
    {
      DisplayFilterRegistry.Register( typeof( PALCompositeFilter ) );
    }



    public const int STANDARD_PAL  = 0;
    public const int STANDARD_NTSC = 1;

    public const int CONNECTION_COMPOSITE = 0;
    public const int CONNECTION_SVIDEO    = 1;

    // Samples per source pixel in the synthesized signal.
    private const int OVERSAMPLE = 8;

    // Horizontal output resolution: subpixels per source pixel. Keeps the
    // decoded signal's edge ramps (a real CRT displays the continuous
    // waveform) instead of flattening them into whole pixels.
    private const int SUBPIXELS = 4;
    private const int SAMPLES_PER_SUB = OVERSAMPLE / SUBPIXELS;

    // Subcarrier cycles per C64 pixel: PAL 4.43361875 MHz / 7.882 MHz
    // pixel clock; NTSC 3.579545 MHz / 8.1818 MHz (values from the
    // palstuff measurements).
    private const double CYCLES_PER_PIXEL_PAL  = 4.43361875 / 7.882;
    private const double CYCLES_PER_PIXEL_NTSC = 3.579545 / 8.1818;

    // How much of the reconstructed chroma the luma notch removes on a
    // composite connection. The remainder is the visible dot crawl.
    private const double NOTCH_DEPTH = 0.85;


    /// <summary>Video standard — PAL (delay line, V-switch) or NTSC (neither; line dithers stay unresolved).</summary>
    public int Standard        { get; set; } = STANDARD_PAL;

    /// <summary>Composite (Y+C summed → crosstalk artifacts) or S-Video (separate Y/C, clean).</summary>
    public int Connection      { get; set; } = CONNECTION_COMPOSITE;

    /// <summary>Luma bandwidth × 0.1 MHz, 20..60 (2.0..6.0 MHz). Lower = softer picture.</summary>
    public int LumaBandwidth   { get; set; } = 50;

    /// <summary>Chroma bandwidth × 0.1 MHz, 5..20 (0.5..2.0 MHz). Lower = wider color bleed.</summary>
    public int ChromaBandwidth { get; set; } = 13;

    /// <summary>PAL delay-line strength, 0..100. 100 = full previous-line averaging; 0 = cheap TV without one. Inert under NTSC.</summary>
    public int LineBlend       { get; set; } = 100;

    /// <summary>VIC-II chroma phase alternation error in degrees, 0..15. Produces the odd/even line ripple and order-dependent mixes.</summary>
    public int PhaseError      { get; set; } = 4;

    /// <summary>
    /// Output-stage luma rise TIME CONSTANT × 0.1 source px, 0..30. The
    /// measured artifact (an RF-modulator circuit effect present on stock
    /// machines even over composite; machines with the modulator removed
    /// don't show it) is a low→high TRANSITION of "about 2 pixels" — a
    /// 1-pole's 10-90% transition is ≈2.2τ, so τ ≈ 0.9 px → default 9.
    /// Setting the time constant itself to 2 px more than doubles the
    /// documented effect and crushes 1-px detail to ~40% brightness.
    /// </summary>
    public int EdgeRise        { get; set; } = 9;

    /// <summary>Output-stage luma fall time constant × 0.1 source px, 0..30. Measured transition ≈0.5 px → τ ≈ 0.2 px → default 2.</summary>
    public int EdgeFall        { get; set; } = 2;

    /// <summary>Chroma gain, 50..200 percent.</summary>
    public int Saturation      { get; set; } = 100;

    /// <summary>Simulate the PAL-era γ2.8 display on a modern ≈γ2.2 monitor (0/1). Off by default — palettes like Pepto already bake display gamma in; enabling this on top double-corrects.</summary>
    public int GammaSim        { get; set; } = 0;



    // ================================================================
    // Cached working storage — capacity-grown; all indexing uses per-call
    // dimensions, so oversized arrays are harmless. The pipeline instance
    // is per-document now, but the grow-only pattern stays cheap when the
    // visible slice changes with zoom/scroll.
    // ================================================================
    private byte[]   m_SrcR, m_SrcG, m_SrcB;          // source-res input planes
    private byte[]   m_OutR, m_OutG, m_OutB;          // subpixel-res output planes
    private float[]  m_LineY, m_LineU, m_LineV;       // per-pixel line values
    private float[]  m_SubY;                          // decoded luma at subpixel res
    private float[]  m_Signal;                        // composite / luma signal (w * OVERSAMPLE)
    private float[]  m_SignalChroma;                  // S-Video separate chroma signal
    private float[]  m_DemodU, m_DemodV;              // demodulated + lowpassed chroma (signal rate)
    private float[]  m_LumaLP;                        // lowpassed luma (signal rate)
    private float[]  m_PixU, m_PixV, m_PixPrevU, m_PixPrevV;  // per-pixel chroma, current + previous line (raw)

    private byte[]   m_GammaLut;                      // γ2.8 → γ2.2 display simulation



    public override string Name
    {
      get { return "PAL Composite"; }
    }



    private void EnsureCapacity( int w, int h )
    {
      int pixels = w * h;
      if ( ( m_SrcR != null )
      &&   ( m_SrcR.Length >= pixels )
      &&   ( m_LineY.Length >= w ) )
      {
        return;
      }
      m_SrcR = new byte[pixels];
      m_SrcG = new byte[pixels];
      m_SrcB = new byte[pixels];
      // Output planes carry SUBPIXELS horizontal resolution — decimating
      // the decoded signal all the way down to source pixels box-averages
      // the edge ramps flat, which crushes 1-px detail (a bright dot's
      // ramp peaks at ~67% near its END; averaged over the whole pixel it
      // drops to ~30% — maps built from fine dithers turned to mud). The
      // reference renders keep the signal's sub-pixel structure visible;
      // so do we, and the upsampler samples the finer grid.
      m_OutR = new byte[pixels * SUBPIXELS];
      m_OutG = new byte[pixels * SUBPIXELS];
      m_OutB = new byte[pixels * SUBPIXELS];
      m_LineY = new float[w];
      m_LineU = new float[w];
      m_LineV = new float[w];
      m_SubY  = new float[w * SUBPIXELS];
      m_Signal       = new float[w * OVERSAMPLE];
      m_SignalChroma = new float[w * OVERSAMPLE];
      m_DemodU = new float[w * OVERSAMPLE];
      m_DemodV = new float[w * OVERSAMPLE];
      m_LumaLP = new float[w * OVERSAMPLE];
      m_PixU     = new float[w * SUBPIXELS];
      m_PixV     = new float[w * SUBPIXELS];
      m_PixPrevU = new float[w * SUBPIXELS];
      m_PixPrevV = new float[w * SUBPIXELS];
    }



    /// <summary>
    /// One-pole lowpass coefficient for a cutoff given in MHz, at the
    /// signal sample rate (pixelClockMHz * OVERSAMPLE).
    /// </summary>
    private static float LowpassK( double cutoffMHz, double pixelClockMHz )
    {
      double fs = pixelClockMHz * OVERSAMPLE;
      double k  = 1.0 - Math.Exp( -2.0 * Math.PI * cutoffMHz / fs );
      if ( k < 0.0005 ) k = 0.0005;
      if ( k > 1.0 )    k = 1.0f;
      return (float)k;
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int standard   = Math.Max( 0, Math.Min( 1, Standard ) );
      int connection = Math.Max( 0, Math.Min( 1, Connection ) );
      int lumaBW     = Math.Max( 20, Math.Min( 60, LumaBandwidth ) );
      int chromaBW   = Math.Max( 5, Math.Min( 20, ChromaBandwidth ) );
      int lineBlend  = Math.Max( 0, Math.Min( 100, LineBlend ) );
      int phaseErr   = Math.Max( 0, Math.Min( 15, PhaseError ) );
      int edgeRise   = Math.Max( 0, Math.Min( 30, EdgeRise ) );
      int edgeFall   = Math.Max( 0, Math.Min( 30, EdgeFall ) );
      int saturation = Math.Max( 50, Math.Min( 200, Saturation ) );
      bool gammaSim  = ( GammaSim != 0 );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = Math.Max( 0, ctx.RenderOffsetY );
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int mapBot   = Math.Min( height, ctx.RenderOffsetY + ctx.MapPixelHeight );
      int mapW     = mapRight - mapLeft;
      int mapH     = mapBot - mapTop;
      int srcW     = ctx.SourceWidth;
      int srcH     = ctx.SourceHeight;

      if ( ( mapW <= 1 )
      ||   ( mapH <= 1 )
      ||   ( srcW <= 1 )
      ||   ( srcH <= 1 ) )
      {
        CopyThrough( ctx );
        return;
      }

      EnsureCapacity( srcW, srcH );

      double cyclesPerPixel = ( standard == STANDARD_NTSC )
                              ? CYCLES_PER_PIXEL_NTSC : CYCLES_PER_PIXEL_PAL;
      double pixelClockMHz  = ( standard == STANDARD_NTSC ) ? 8.1818 : 7.882;
      bool   palSwitch      = ( standard == STANDARD_PAL );
      bool   delayLineOn    = palSwitch && ( lineBlend > 0 );
      bool   composite      = ( connection == CONNECTION_COMPOSITE );

      float kLuma   = LowpassK( lumaBW / 10.0, pixelClockMHz );
      float kChroma = LowpassK( chromaBW / 10.0, pixelClockMHz );

      // Output-stage edge shaping: per-sample coefficients from the rise /
      // fall time constants in source pixels. 0 → instantaneous.
      float kRise = ( edgeRise == 0 )
                    ? 1.0f : (float)( 1.0 - Math.Exp( -1.0 / ( edgeRise / 10.0 * OVERSAMPLE ) ) );
      float kFall = ( edgeFall == 0 )
                    ? 1.0f : (float)( 1.0 - Math.Exp( -1.0 / ( edgeFall / 10.0 * OVERSAMPLE ) ) );

      double phaseErrRad = phaseErr * Math.PI / 180.0;
      double dphi        = 2.0 * Math.PI * cyclesPerPixel / OVERSAMPLE;
      float  satEnc      = saturation / 100.0f;
      float  blendHalf   = lineBlend / 200.0f;         // 100 → 0.5 = true averaging

      // ----------------------------------------------------------------
      // Complex responses of the decoder's IIR filters AT THE SUBCARRIER.
      // A real TV's burst reference travels the same chroma path as the
      // signal, so its demodulator is automatically aligned; our reference
      // oscillator is pristine, so we must compensate explicitly or every
      // color decodes rotated/attenuated by the 2 MHz high-pass (≈0.82∠+24°
      // at the PAL subcarrier — a permanent hue error on composite), and
      // the luma notch must subtract the chroma AS IT EXISTS after the
      // luma low-pass (≈0.75∠−30°), not the pristine version — otherwise
      // the "notch" partially ADDS ripple and dot crawl runs ~5× design.
      // 1-pole LP: H(z) = k / (1 − (1−k)·z⁻¹), HP = 1 − LP, at z = e^{i·dphi}.
      // ----------------------------------------------------------------
      float kHP = LowpassK( 2.0, pixelClockMHz );
      double cosW = Math.Cos( dphi );
      double sinW = Math.Sin( dphi );

      // H_HP(f_sc) — the chroma path's pre-demod high-pass (composite only).
      double aHP   = 1.0 - kHP;
      double denRe = 1.0 - aHP * cosW;
      double denIm = aHP * sinW;
      double denSq = denRe * denRe + denIm * denIm;
      double lpRe  = kHP * denRe / denSq;
      double lpIm  = -kHP * denIm / denSq;
      double hpRe  = 1.0 - lpRe;
      double hpIm  = -lpIm;
      double hpGain  = composite ? Math.Sqrt( hpRe * hpRe + hpIm * hpIm ) : 1.0;
      double hpPhase = composite ? Math.Atan2( hpIm, hpRe ) : 0.0;
      if ( hpGain < 0.05 ) hpGain = 0.05;
      float demodScale = (float)( 2.0 / hpGain );

      // H_lumaLP(f_sc) — what the luma low-pass does to the chroma that
      // rides through it; the notch must match this to cancel.
      double aLP    = 1.0 - kLuma;
      double denRe2 = 1.0 - aLP * cosW;
      double denIm2 = aLP * sinW;
      double denSq2 = denRe2 * denRe2 + denIm2 * denIm2;
      double llRe   = kLuma * denRe2 / denSq2;
      double llIm   = -kLuma * denIm2 / denSq2;
      float lumaLpGain  = (float)Math.Sqrt( llRe * llRe + llIm * llIm );
      double lumaLpPhase = Math.Atan2( llIm, llRe );
      float lpPhaseCos = (float)Math.Cos( lumaLpPhase );
      float lpPhaseSin = (float)Math.Sin( lumaLpPhase );

      if ( ( gammaSim )
      &&   ( m_GammaLut == null ) )
      {
        m_GammaLut = new byte[256];
        for ( int i = 0; i < 256; ++i )
        {
          m_GammaLut[i] = (byte)Math.Round( Math.Pow( i / 255.0, 2.8 / 2.2 ) * 255.0 );
        }
      }

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        // Chrome: whole buffer verbatim; map rect recomposed at the end.
        long bytes = (long)srcStride * height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );

        // ---- Pass 1: downsample the map rect to source resolution ------
        for ( int sy = 0; sy < srcH; ++sy )
        {
          int ty0 = mapTop + (int)( (long)sy * mapH / srcH );
          int ty1 = mapTop + (int)( (long)( sy + 1 ) * mapH / srcH );
          if ( ty1 <= ty0 ) ty1 = ty0 + 1;
          if ( ty1 > mapBot ) ty1 = mapBot;
          int rowBase = sy * srcW;
          for ( int sx = 0; sx < srcW; ++sx )
          {
            int tx0 = mapLeft + (int)( (long)sx * mapW / srcW );
            int tx1 = mapLeft + (int)( (long)( sx + 1 ) * mapW / srcW );
            if ( tx1 <= tx0 ) tx1 = tx0 + 1;
            if ( tx1 > mapRight ) tx1 = mapRight;

            int sumB = 0, sumG = 0, sumR = 0, count = 0;
            for ( int ty = ty0; ty < ty1; ++ty )
            {
              byte* p = pSrc + ty * srcStride + tx0 * 4;
              for ( int tx = tx0; tx < tx1; ++tx )
              {
                sumB += p[0];
                sumG += p[1];
                sumR += p[2];
                ++count;
                p += 4;
              }
            }
            m_SrcB[rowBase + sx] = (byte)( sumB / count );
            m_SrcG[rowBase + sx] = (byte)( sumG / count );
            m_SrcR[rowBase + sx] = (byte)( sumR / count );
          }
        }

        // ---- Pass 2: per scanline encode → decode ----------------------
        int samples = srcW * OVERSAMPLE;
        for ( int line = 0; line < srcH; ++line )
        {
          int rowBase = line * srcW;

          // Pixel-rate YUV (CCIR 601, from the palstuff formulas).
          for ( int x = 0; x < srcW; ++x )
          {
            float r = m_SrcR[rowBase + x];
            float g = m_SrcG[rowBase + x];
            float b = m_SrcB[rowBase + x];
            m_LineY[x] = 0.299f * r + 0.587f * g + 0.114f * b;
            m_LineU[x] = -0.147f * r - 0.289f * g + 0.436f * b;
            m_LineV[x] = 0.615f * r - 0.515f * g - 0.100f * b;
          }

          // Per-line phase bookkeeping:
          //  - burst/reference phase: PAL's ~270°/line step yields the
          //    classic 4-line dot pattern (static snapshot); NTSC's
          //    227.5 cycles/line is a 180°/line step — a 2-line pattern,
          //  - PAL V-switch alternates the V modulation sign,
          //  - the VIC's imperfect alternation adds ±PhaseError to the
          //    ENCODER only — the decoder stays burst-locked, so the error
          //    survives into the output as the odd/even ripple. PAL only:
          //    the artifact models the phase FLIP the NTSC VIC doesn't do.
          double linePhase = palSwitch
                             ? ( line & 3 ) * ( Math.PI / 2.0 )
                             : ( line & 1 ) * Math.PI;
          float  vSwitch   = ( palSwitch && ( ( line & 1 ) == 1 ) ) ? -1.0f : 1.0f;
          double vicErr    = !palSwitch ? 0.0
                             : ( ( line & 1 ) == 1 ) ? -phaseErrRad : phaseErrRad;
          float  errCos    = (float)Math.Cos( vicErr );
          float  errSin    = (float)Math.Sin( vicErr );

          // Encode: upsample (sample & hold), asymmetric output stage on
          // luma, modulate chroma. Incremental rotation avoids per-sample
          // trig; double precision keeps drift far below visibility.
          double sinP = Math.Sin( linePhase );
          double cosP = Math.Cos( linePhase );
          double sinD = Math.Sin( dphi );
          double cosD = Math.Cos( dphi );

          float shaped = m_LineY[0];
          for ( int s = 0; s < samples; ++s )
          {
            int px = s / OVERSAMPLE;
            float yIn = m_LineY[px];
            // Slow rise / fast fall — the emusux0r mechanism.
            shaped += ( ( yIn > shaped ) ? kRise : kFall ) * ( yIn - shaped );

            // VIC phase error as a rotation of the (U,V) vector.
            float u = m_LineU[px] * errCos - m_LineV[px] * errSin;
            float v = m_LineU[px] * errSin + m_LineV[px] * errCos;

            float chroma = satEnc * ( u * (float)sinP + vSwitch * v * (float)cosP );
            if ( composite )
            {
              m_Signal[s]       = shaped + chroma;
              m_SignalChroma[s] = 0.0f;
            }
            else
            {
              m_Signal[s]       = shaped;
              m_SignalChroma[s] = chroma;
            }

            double nsin = sinP * cosD + cosP * sinD;
            cosP = cosP * cosD - sinP * sinD;
            sinP = nsin;
          }

          // Decode — demodulate chroma with the burst-locked reference.
          // The chroma path is band-limited BEFORE the multiply (a 1-pole
          // high-pass at ~2 MHz removes the luma baseband — otherwise flat
          // brightness aliases into strong false color) and uses a 2-pole
          // lowpass after it (a single pole leaks the up-shifted luma DC
          // at the subcarrier frequency back through). Luma detail that
          // genuinely lives NEAR the subcarrier still passes the high-pass
          // — that is the real cross-color, and it stays.
          //
          // The reference oscillator is advanced by the high-pass's phase
          // at the subcarrier and the recovered amplitude divided by its
          // gain (a real TV's burst travels the same path, aligning its
          // demodulator automatically) — without this every composite
          // color decodes rotated ≈24° and 18% desaturated.
          //
          // The IIR states are WARM-STARTED at the first pixel's settled
          // values — a real TV is locked long before active video, and
          // cold zeros paint a desaturated stripe down the map's left edge.
          sinP = Math.Sin( linePhase + hpPhase );
          cosP = Math.Cos( linePhase + hpPhase );
          float u0 = satEnc * ( m_LineU[0] * errCos - m_LineV[0] * errSin );
          float v0 = satEnc * ( m_LineU[0] * errSin + m_LineV[0] * errCos );
          float uLP1 = u0, vLP1 = v0, uLP2 = u0, vLP2 = v0;
          float hpState = composite ? m_LineY[0] : 0.0f;
          float yLP = m_Signal[0];
          for ( int s = 0; s < samples; ++s )
          {
            float chromaIn;
            if ( composite )
            {
              hpState += kHP * ( m_Signal[s] - hpState );
              chromaIn = m_Signal[s] - hpState;
            }
            else
            {
              chromaIn = m_SignalChroma[s];
            }

            float du = demodScale * chromaIn * (float)sinP;
            float dv = demodScale * chromaIn * (float)cosP * vSwitch;
            uLP1 += kChroma * ( du - uLP1 );
            vLP1 += kChroma * ( dv - vLP1 );
            uLP2 += kChroma * ( uLP1 - uLP2 );
            vLP2 += kChroma * ( vLP1 - vLP2 );
            m_DemodU[s] = uLP2;
            m_DemodV[s] = vLP2;

            yLP += kLuma * ( m_Signal[s] - yLP );
            m_LumaLP[s] = yLP;

            double nsin = sinP * cosD + cosP * sinD;
            cosP = cosP * cosD - sinP * sinD;
            sinP = nsin;
          }

          // Decimate to pixel rate. Luma: notch the reconstructed chroma
          // back out (composite) — the (1 - NOTCH_DEPTH) remainder is the
          // dot crawl; cross-color arrives in DemodU/V for free.
          //
          // The remod must match the chroma AS IT EXISTS inside m_LumaLP —
          // i.e. after the luma low-pass's gain and phase lag at the
          // subcarrier — so the demodulated (U,V) vector is rotated by
          // lumaLpPhase (the V-switch mirrors the rotation direction on
          // odd PAL lines) and scaled by lumaLpGain before remodulation.
          // Subtracting the pristine chroma instead would be ~54° out of
          // phase and ADD ripple rather than cancel it.
          sinP = Math.Sin( linePhase );
          cosP = Math.Cos( linePhase );
          int subCount = srcW * SUBPIXELS;
          for ( int sub = 0; sub < subCount; ++sub )
          {
            float ySum = 0.0f, uSum = 0.0f, vSum = 0.0f;
            for ( int o = 0; o < SAMPLES_PER_SUB; ++o )
            {
              int s = sub * SAMPLES_PER_SUB + o;
              float y = m_LumaLP[s];
              if ( composite )
              {
                float ru = lumaLpGain * ( m_DemodU[s] * lpPhaseCos - vSwitch * m_DemodV[s] * lpPhaseSin );
                float rv = lumaLpGain * ( vSwitch * m_DemodU[s] * lpPhaseSin + m_DemodV[s] * lpPhaseCos );
                float remod = ru * (float)sinP + vSwitch * rv * (float)cosP;
                y -= (float)NOTCH_DEPTH * remod;
              }
              ySum += y;
              uSum += m_DemodU[s];
              vSum += m_DemodV[s];

              double nsin = sinP * cosD + cosP * sinD;
              cosP = cosP * cosD - sinP * sinD;
              sinP = nsin;
            }
            // No renormalization by the encode gain — the Saturation knob
            // deliberately scales what the "TV" receives and shows.
            m_SubY[sub] = ySum / SAMPLES_PER_SUB;
            m_PixU[sub] = uSum / SAMPLES_PER_SUB;
            m_PixV[sub] = vSum / SAMPLES_PER_SUB;
          }

          // PAL delay line: average with the PREVIOUS line's raw
          // demodulated chroma (the transmitted line, not the displayed
          // blend). Opposite ±PhaseError rotations cancel here — leaving
          // exactly the residual ripple the strength slider dials in.
          if ( ( delayLineOn )
          &&   ( line > 0 ) )
          {
            for ( int sub = 0; sub < subCount; ++sub )
            {
              float curU = m_PixU[sub];
              float curV = m_PixV[sub];
              m_PixU[sub] = curU + blendHalf * ( m_PixPrevU[sub] - curU );
              m_PixV[sub] = curV + blendHalf * ( m_PixPrevV[sub] - curV );
              m_PixPrevU[sub] = curU;
              m_PixPrevV[sub] = curV;
            }
          }
          else
          {
            Array.Copy( m_PixU, m_PixPrevU, subCount );
            Array.Copy( m_PixV, m_PixPrevV, subCount );
          }

          // Back to RGB (CCIR 601 inverse, from the site), at subpixel res.
          int outBase = line * srcW * SUBPIXELS;
          for ( int sub = 0; sub < subCount; ++sub )
          {
            float y = m_SubY[sub];
            float u = m_PixU[sub];
            float v = m_PixV[sub];
            int r = (int)( y + 1.140f * v + 0.5f );
            int g = (int)( y - 0.396f * u - 0.581f * v + 0.5f );
            int b = (int)( y + 2.029f * u + 0.5f );
            if ( r < 0 ) r = 0; if ( r > 255 ) r = 255;
            if ( g < 0 ) g = 0; if ( g > 255 ) g = 255;
            if ( b < 0 ) b = 0; if ( b > 255 ) b = 255;
            if ( gammaSim )
            {
              r = m_GammaLut[r];
              g = m_GammaLut[g];
              b = m_GammaLut[b];
            }
            m_OutR[outBase + sub] = (byte)r;
            m_OutG[outBase + sub] = (byte)g;
            m_OutB[outBase + sub] = (byte)b;
          }
        }

        // ---- Pass 3: bilinear upsample back into the target rect --------
        // Horizontal resolution of the decoded grid is srcW * SUBPIXELS.
        int subW = srcW * SUBPIXELS;
        for ( int y = mapTop; y < mapBot; ++y )
        {
          double fy = ( ( y - mapTop ) + 0.5 ) * srcH / (double)mapH - 0.5;
          int sy0 = (int)Math.Floor( fy );
          int sy1 = sy0 + 1;
          int wy  = (int)( ( fy - sy0 ) * 256.0 );
          if ( sy0 < 0 ) { sy0 = 0; sy1 = 0; wy = 0; }
          if ( sy1 > srcH - 1 ) { sy1 = srcH - 1; if ( sy0 > sy1 ) sy0 = sy1; }
          int base0 = sy0 * subW;
          int base1 = sy1 * subW;

          byte* s = pSrc + y * srcStride + mapLeft * 4;
          byte* d = pDst + y * dstStride + mapLeft * 4;
          for ( int x = mapLeft; x < mapRight; ++x )
          {
            // Horizontal: a target pixel narrower than one subpixel is
            // MAGNIFYING — bilinear keeps the edge ramps smooth. A target
            // pixel spanning several subpixels is MINIFYING — box-average
            // the covered span or the 2-tap bilinear would alias.
            int sub0 = (int)( (long)( x - mapLeft ) * subW / mapW );
            int sub1 = (int)( (long)( x - mapLeft + 1 ) * subW / mapW );
            int b, g, r;
            if ( sub1 - sub0 >= 2 )
            {
              if ( sub1 > subW ) sub1 = subW;
              int span = sub1 - sub0;
              int sb0 = 0, sg0 = 0, sr0 = 0, sb1 = 0, sg1 = 0, sr1 = 0;
              for ( int si = sub0; si < sub1; ++si )
              {
                sb0 += m_OutB[base0 + si];
                sg0 += m_OutG[base0 + si];
                sr0 += m_OutR[base0 + si];
                sb1 += m_OutB[base1 + si];
                sg1 += m_OutG[base1 + si];
                sr1 += m_OutR[base1 + si];
              }
              b = ( ( sb0 / span ) * ( 256 - wy ) + ( sb1 / span ) * wy ) >> 8;
              g = ( ( sg0 / span ) * ( 256 - wy ) + ( sg1 / span ) * wy ) >> 8;
              r = ( ( sr0 / span ) * ( 256 - wy ) + ( sr1 / span ) * wy ) >> 8;
            }
            else
            {
              double fx = ( ( x - mapLeft ) + 0.5 ) * subW / (double)mapW - 0.5;
              int sx0 = (int)Math.Floor( fx );
              int sx1 = sx0 + 1;
              int wx  = (int)( ( fx - sx0 ) * 256.0 );
              if ( sx0 < 0 ) { sx0 = 0; sx1 = 0; wx = 0; }
              if ( sx1 > subW - 1 ) { sx1 = subW - 1; if ( sx0 > sx1 ) sx0 = sx1; }

              int w00 = ( 256 - wx ) * ( 256 - wy );
              int w10 = wx * ( 256 - wy );
              int w01 = ( 256 - wx ) * wy;
              int w11 = wx * wy;

              b = ( m_OutB[base0 + sx0] * w00 + m_OutB[base0 + sx1] * w10
                  + m_OutB[base1 + sx0] * w01 + m_OutB[base1 + sx1] * w11 ) >> 16;
              g = ( m_OutG[base0 + sx0] * w00 + m_OutG[base0 + sx1] * w10
                  + m_OutG[base1 + sx0] * w01 + m_OutG[base1 + sx1] * w11 ) >> 16;
              r = ( m_OutR[base0 + sx0] * w00 + m_OutR[base0 + sx1] * w10
                  + m_OutR[base1 + sx0] * w01 + m_OutR[base1 + sx1] * w11 ) >> 16;
            }

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



    // ================================================================
    // Persistence
    //
    // Legacy layout was (LumaBlur σ×10, ChromaBlur σ×10, LineBlend,
    // Saturation) from the approximation-based engine. The legacy slots
    // are still WRITTEN (derived from the new parameters) so older builds
    // reading a newer file behave sanely; on load they seed the new
    // bandwidth parameters via a documented mapping when the appended
    // fields are absent.
    //   LumaBandwidth   = clamp(20..60, 60 − LumaBlur)
    //   ChromaBandwidth = clamp(5..20, 21 − ChromaBlur/4)
    // New-in-v2 parameters default NEUTRAL on legacy loads (no phase
    // error, no edge asymmetry) so old projects change as little as the
    // engine swap allows.
    // ================================================================

    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( GR.MathUtil.Clamp( 0, 60 - LumaBandwidth, 30 ) );        // legacy LumaBlur slot
      buf.AppendI32( GR.MathUtil.Clamp( 5, ( 21 - ChromaBandwidth ) * 4, 80 ) ); // legacy ChromaBlur slot
      buf.AppendI32( LineBlend );
      buf.AppendI32( Saturation );
      buf.AppendI32( Standard );
      buf.AppendI32( Connection );
      buf.AppendI32( LumaBandwidth );
      buf.AppendI32( ChromaBandwidth );
      buf.AppendI32( PhaseError );
      buf.AppendI32( EdgeRise );
      buf.AppendI32( EdgeFall );
      buf.AppendI32( GammaSim );
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
      int legacyLuma   = GR.MathUtil.Clamp( 0, r.ReadInt32(), 30 );
      int legacyChroma = 30;
      if ( r.Size - r.Position >= 4 ) legacyChroma = GR.MathUtil.Clamp( 5, r.ReadInt32(), 80 );
      if ( r.Size - r.Position >= 4 ) LineBlend    = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) Saturation   = GR.MathUtil.Clamp( 50, r.ReadInt32(), 200 );

      if ( r.Size - r.Position >= 4 )
      {
        // v2 blob — read the authoritative fields.
        Standard        = GR.MathUtil.Clamp( 0, r.ReadInt32(), 1 );
        if ( r.Size - r.Position >= 4 ) Connection      = GR.MathUtil.Clamp( 0, r.ReadInt32(), 1 );
        if ( r.Size - r.Position >= 4 ) LumaBandwidth   = GR.MathUtil.Clamp( 20, r.ReadInt32(), 60 );
        if ( r.Size - r.Position >= 4 ) ChromaBandwidth = GR.MathUtil.Clamp( 5, r.ReadInt32(), 20 );
        if ( r.Size - r.Position >= 4 ) PhaseError      = GR.MathUtil.Clamp( 0, r.ReadInt32(), 15 );
        if ( r.Size - r.Position >= 4 ) EdgeRise        = GR.MathUtil.Clamp( 0, r.ReadInt32(), 30 );
        if ( r.Size - r.Position >= 4 ) EdgeFall        = GR.MathUtil.Clamp( 0, r.ReadInt32(), 30 );
        if ( r.Size - r.Position >= 4 ) GammaSim        = GR.MathUtil.Clamp( 0, r.ReadInt32(), 1 );
      }
      else
      {
        // Legacy v1 blob — map the old blur strengths onto bandwidths,
        // keep everything new NEUTRAL.
        LumaBandwidth   = GR.MathUtil.Clamp( 20, 60 - legacyLuma, 60 );
        ChromaBandwidth = GR.MathUtil.Clamp( 5, 21 - legacyChroma / 4, 20 );
        Standard   = STANDARD_PAL;
        Connection = CONNECTION_COMPOSITE;
        PhaseError = 0;
        EdgeRise   = 0;
        EdgeFall   = 0;
        GammaSim   = 0;
      }
    }



    public override IDisplayFilter Clone()
    {
      return new PALCompositeFilter
      {
        Enabled         = this.Enabled,
        Standard        = this.Standard,
        Connection      = this.Connection,
        LumaBandwidth   = this.LumaBandwidth,
        ChromaBandwidth = this.ChromaBandwidth,
        LineBlend       = this.LineBlend,
        PhaseError      = this.PhaseError,
        EdgeRise        = this.EdgeRise,
        EdgeFall        = this.EdgeFall,
        Saturation      = this.Saturation,
        GammaSim        = this.GammaSim,
      };
    }
  }
}
