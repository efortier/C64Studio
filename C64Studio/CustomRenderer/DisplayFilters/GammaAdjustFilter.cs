using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Per-channel gamma / brightness / contrast correction. Standard CRT
  /// emulation fixture — real CRTs have non-linear (≈γ2.2) response, and
  /// modern LCD panels that feed them often apply a different curve, so the
  /// pixels we see in the editor don't match what would land on real
  /// hardware. Use this filter to pre-correct or post-correct.
  ///
  /// Applied as a 256-entry lookup per channel, rebuilt whenever any of the
  /// three parameters changes. The per-pixel cost is three table reads.
  /// </summary>
  public class GammaAdjustFilter : DisplayFilterBase
  {
    static GammaAdjustFilter()
    {
      DisplayFilterRegistry.Register( typeof( GammaAdjustFilter ) );
    }



    /// <summary>Gamma exponent × 100. Range 50..300 ⇒ γ 0.5..3.0. 100 = identity.</summary>
    public int Gamma      { get; set; } = 100;

    /// <summary>Additive brightness, percent of full scale. -100..+100.</summary>
    public int Brightness { get; set; } = 0;

    /// <summary>Multiplicative contrast around the midpoint, percent. -100..+100.</summary>
    public int Contrast   { get; set; } = 0;



    public override string Name
    {
      get { return "Gamma / Brightness / Contrast"; }
    }



    // Cached LUT so we don't rebuild 768 entries per paint when the user isn't
    // changing parameters. Comparison key is (gamma, brightness, contrast).
    private byte[] m_Lut = null;
    private int    m_LutKeyGamma = int.MinValue;
    private int    m_LutKeyBright = int.MinValue;
    private int    m_LutKeyContrast = int.MinValue;



    private void BuildLutIfNeeded( int gamma, int brightness, int contrast )
    {
      if ( ( m_Lut != null )
      &&   ( m_LutKeyGamma    == gamma )
      &&   ( m_LutKeyBright   == brightness )
      &&   ( m_LutKeyContrast == contrast ) )
      {
        return;
      }
      if ( m_Lut == null )
      {
        m_Lut = new byte[256];
      }

      double g    = Math.Max( 0.5, Math.Min( 3.0, gamma / 100.0 ) );
      double invG = 1.0 / g;
      double bOff = brightness / 100.0;                  // -1 .. +1
      double cMul = 1.0 + ( contrast / 100.0 );          //  0 .. +2

      for ( int i = 0; i < 256; ++i )
      {
        // Linear work in [0, 1]:
        //   contrast around midpoint, then brightness offset, clamp,
        //   then the gamma exponent. Gamma is applied last because it's
        //   the non-linearity that emulates the display response — the
        //   other two are scene-referred adjustments.
        double v = i / 255.0;
        v = ( v - 0.5 ) * cMul + 0.5;
        v = v + bOff;
        if ( v < 0.0 ) v = 0.0;
        if ( v > 1.0 ) v = 1.0;
        v = Math.Pow( v, invG );
        if ( v < 0.0 ) v = 0.0;
        if ( v > 1.0 ) v = 1.0;
        m_Lut[i] = (byte)Math.Round( v * 255.0 );
      }

      m_LutKeyGamma    = gamma;
      m_LutKeyBright   = brightness;
      m_LutKeyContrast = contrast;
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int gamma      = GR.MathUtil.Clamp( 50,   Gamma,      300 );
      int brightness = GR.MathUtil.Clamp( -100, Brightness, 100 );
      int contrast   = GR.MathUtil.Clamp( -100, Contrast,   100 );

      // Shared LUT across all three channels — gamma/brightness/contrast
      // are per-channel-equal by definition.
      BuildLutIfNeeded( gamma, brightness, contrast );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapLeft  = ctx.RenderOffsetX;
      int mapRight = ctx.RenderOffsetX + ctx.MapPixelWidth;
      int mapTop   = ctx.RenderOffsetY;
      int mapBot   = ctx.RenderOffsetY + ctx.MapPixelHeight;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( byte* pLut = m_Lut )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* s = pSrc + y * srcStride;
            byte* d = pDst + y * dstStride;

            bool rowInMap = ( y >= mapTop ) && ( y < mapBot );
            for ( int x = 0; x < width; ++x )
            {
              if ( ( !rowInMap )
              ||   ( x < mapLeft )
              ||   ( x >= mapRight ) )
              {
                d[0] = s[0];
                d[1] = s[1];
                d[2] = s[2];
                d[3] = s[3];
              }
              else
              {
                d[0] = pLut[s[0]];
                d[1] = pLut[s[1]];
                d[2] = pLut[s[2]];
                d[3] = s[3];
              }
              s += 4;
              d += 4;
            }
          }
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Gamma );
      buf.AppendI32( Brightness );
      buf.AppendI32( Contrast );
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
      Gamma      = GR.MathUtil.Clamp( 50,   r.ReadInt32(), 300 );
      if ( r.Size - r.Position >= 4 ) Brightness = GR.MathUtil.Clamp( -100, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) Contrast   = GR.MathUtil.Clamp( -100, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new GammaAdjustFilter
      {
        Enabled    = this.Enabled,
        Gamma      = this.Gamma,
        Brightness = this.Brightness,
        Contrast   = this.Contrast,
      };
    }
  }
}
