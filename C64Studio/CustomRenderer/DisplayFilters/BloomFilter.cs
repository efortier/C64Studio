using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Bloom and halation — the two ways a CRT makes bright areas glow:
  ///
  /// <list type="bullet">
  ///   <item><b>Bloom</b>: the electron beam's spot spreads with intensity
  ///   and the phosphor scatters, so bright pixels bleed a tight halo into
  ///   their neighbors.</item>
  ///   <item><b>Halation</b>: light reflects between the phosphor layer and
  ///   the front glass, producing a much wider, fainter glow around bright
  ///   regions.</item>
  /// </list>
  ///
  /// Implementation: per-channel bright-pass (everything above
  /// <see cref="Threshold"/>) into HALF resolution planes, blurred with
  /// two-pass sliding box filters (Gaussian-like, O(1) per pixel at any
  /// radius); halation reuses the bloom planes and blurs them wider. The
  /// result is upsampled bilinearly and ADDED to the image. Radii are in
  /// source pixels and scale with zoom.
  /// </summary>
  public class BloomFilter : DisplayFilterBase
  {
    static BloomFilter()
    {
      DisplayFilterRegistry.Register( typeof( BloomFilter ) );
    }



    /// <summary>Brightness level where glow starts, 0..100 percent of full scale.</summary>
    public int Threshold        { get; set; } = 55;

    /// <summary>Bloom (tight glow) contribution, 0..100.</summary>
    public int BloomStrength    { get; set; } = 40;

    /// <summary>Bloom radius σ × 10 in source pixels, 5..40.</summary>
    public int BloomRadius      { get; set; } = 15;

    /// <summary>Halation (wide faint glow) contribution, 0..100.</summary>
    public int HalationStrength { get; set; } = 20;

    /// <summary>Halation radius σ × 10 in source pixels, 20..120.</summary>
    public int HalationRadius   { get; set; } = 50;



    // Half-resolution working planes. short is plenty: bright-pass values
    // are 0..255 and box blurs only average. Capacity-based (grow-only):
    // the pipeline instance is shared by every open map editor, and two
    // views of different sizes alternating paints must not reallocate per
    // frame. All indexing uses the per-call width, so oversized arrays are
    // harmless.
    private short[]  m_BloomB;
    private short[]  m_BloomG;
    private short[]  m_BloomR;
    private short[]  m_HalB;
    private short[]  m_HalG;
    private short[]  m_HalR;
    private int[]    m_Scratch;



    public override string Name
    {
      get { return "Bloom / Halation"; }
    }



    private void EnsurePlanes( int w, int h )
    {
      int count   = w * h;
      int scratch = Math.Max( w, h );
      if ( ( m_BloomB != null )
      &&   ( m_BloomB.Length >= count )
      &&   ( m_Scratch.Length >= scratch ) )
      {
        return;
      }
      m_BloomB = new short[count];
      m_BloomG = new short[count];
      m_BloomR = new short[count];
      m_HalB   = new short[count];
      m_HalG   = new short[count];
      m_HalR   = new short[count];
      m_Scratch = new int[scratch];
    }



    private static int BoxRadiusForSigma( double sigma )
    {
      double w = Math.Sqrt( 6.0 * sigma * sigma + 1.0 );
      int    r = (int)Math.Round( ( w - 1.0 ) / 2.0 );
      return Math.Max( 0, Math.Min( 255, r ) );
    }



    private void BoxBlurRows( short[] plane, int w, int h, int radius )
    {
      if ( ( radius <= 0 )
      ||   ( w <= 1 ) )
      {
        return;
      }
      int window = 2 * radius + 1;
      for ( int pass = 0; pass < 2; ++pass )
      {
        for ( int y = 0; y < h; ++y )
        {
          int rowBase = y * w;
          for ( int x = 0; x < w; ++x )
          {
            m_Scratch[x] = plane[rowBase + x];
          }
          int sum = 0;
          for ( int i = -radius; i <= radius; ++i )
          {
            int xi = i;
            if ( xi < 0 )     xi = 0;
            if ( xi > w - 1 ) xi = w - 1;
            sum += m_Scratch[xi];
          }
          plane[rowBase] = (short)( sum / window );
          for ( int x = 1; x < w; ++x )
          {
            int xAdd = x + radius;
            int xSub = x - radius - 1;
            if ( xAdd > w - 1 ) xAdd = w - 1;
            if ( xSub < 0 )     xSub = 0;
            sum += m_Scratch[xAdd] - m_Scratch[xSub];
            plane[rowBase + x] = (short)( sum / window );
          }
        }
      }
    }



    private void BoxBlurColumns( short[] plane, int w, int h, int radius )
    {
      if ( ( radius <= 0 )
      ||   ( h <= 1 ) )
      {
        return;
      }
      int window = 2 * radius + 1;
      for ( int pass = 0; pass < 2; ++pass )
      {
        for ( int x = 0; x < w; ++x )
        {
          for ( int y = 0; y < h; ++y )
          {
            m_Scratch[y] = plane[y * w + x];
          }
          int sum = 0;
          for ( int i = -radius; i <= radius; ++i )
          {
            int yi = i;
            if ( yi < 0 )     yi = 0;
            if ( yi > h - 1 ) yi = h - 1;
            sum += m_Scratch[yi];
          }
          plane[x] = (short)( sum / window );
          for ( int y = 1; y < h; ++y )
          {
            int yAdd = y + radius;
            int ySub = y - radius - 1;
            if ( yAdd > h - 1 ) yAdd = h - 1;
            if ( ySub < 0 )     ySub = 0;
            sum += m_Scratch[yAdd] - m_Scratch[ySub];
            plane[y * w + x] = (short)( sum / window );
          }
        }
      }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int threshold   = Math.Max( 0, Math.Min( 100, Threshold ) );
      int bloomStr    = Math.Max( 0, Math.Min( 100, BloomStrength ) );
      int bloomRad    = Math.Max( 5, Math.Min( 40, BloomRadius ) );
      int halStr      = Math.Max( 0, Math.Min( 100, HalationStrength ) );
      int halRad      = Math.Max( 20, Math.Min( 120, HalationRadius ) );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = Math.Max( 0, ctx.RenderOffsetY );
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int mapBot   = Math.Min( height, ctx.RenderOffsetY + ctx.MapPixelHeight );
      int mapW     = mapRight - mapLeft;
      int mapH     = mapBot - mapTop;

      if ( ( mapW <= 3 )
      ||   ( mapH <= 3 )
      ||   ( ctx.SourceWidth <= 0 )
      ||   ( ( bloomStr == 0 ) && ( halStr == 0 ) ) )
      {
        CopyThrough( ctx );
        return;
      }

      double zoomX = ctx.MapPixelWidth / (double)ctx.SourceWidth;
      // Radii are in half-res pixels, hence the extra /2.
      double sigmaBloom = ( bloomRad / 10.0 ) * zoomX / 2.0;
      double sigmaHal   = ( halRad / 10.0 )   * zoomX / 2.0;
      int rBloom = BoxRadiusForSigma( sigmaBloom );
      // Halation continues blurring the ALREADY bloom-blurred planes, so
      // only the additional variance is applied: σ_extra² = σ_hal² − σ_bloom².
      double sigmaExtra = Math.Sqrt( Math.Max( 0.0, sigmaHal * sigmaHal - sigmaBloom * sigmaBloom ) );
      int rHal = BoxRadiusForSigma( sigmaExtra );

      int w2 = ( mapW + 1 ) / 2;
      int h2 = ( mapH + 1 ) / 2;
      EnsurePlanes( w2, h2 );

      int threshByte = ( threshold * 255 ) / 100;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        // Chrome: whole buffer verbatim, map rect recomposed below.
        long bytes = (long)srcStride * height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );

        // Downsample 2x + bright-pass into the bloom planes.
        for ( int hy = 0; hy < h2; ++hy )
        {
          int sy0 = mapTop + 2 * hy;
          int sy1 = ( 2 * hy + 1 < mapH ) ? sy0 + 1 : sy0;
          int rowBase = hy * w2;
          for ( int hx = 0; hx < w2; ++hx )
          {
            int sx0 = mapLeft + 2 * hx;
            int sx1 = ( 2 * hx + 1 < mapW ) ? sx0 + 1 : sx0;

            byte* p00 = pSrc + sy0 * srcStride + sx0 * 4;
            byte* p10 = pSrc + sy0 * srcStride + sx1 * 4;
            byte* p01 = pSrc + sy1 * srcStride + sx0 * 4;
            byte* p11 = pSrc + sy1 * srcStride + sx1 * 4;

            int b = ( p00[0] + p10[0] + p01[0] + p11[0] ) >> 2;
            int g = ( p00[1] + p10[1] + p01[1] + p11[1] ) >> 2;
            int r = ( p00[2] + p10[2] + p01[2] + p11[2] ) >> 2;

            m_BloomB[rowBase + hx] = (short)( ( b > threshByte ) ? ( b - threshByte ) : 0 );
            m_BloomG[rowBase + hx] = (short)( ( g > threshByte ) ? ( g - threshByte ) : 0 );
            m_BloomR[rowBase + hx] = (short)( ( r > threshByte ) ? ( r - threshByte ) : 0 );
          }
        }

        // Bloom blur.
        BoxBlurRows( m_BloomB, w2, h2, rBloom );
        BoxBlurRows( m_BloomG, w2, h2, rBloom );
        BoxBlurRows( m_BloomR, w2, h2, rBloom );
        BoxBlurColumns( m_BloomB, w2, h2, rBloom );
        BoxBlurColumns( m_BloomG, w2, h2, rBloom );
        BoxBlurColumns( m_BloomR, w2, h2, rBloom );

        // Halation = bloom planes blurred further.
        Array.Copy( m_BloomB, m_HalB, w2 * h2 );
        Array.Copy( m_BloomG, m_HalG, w2 * h2 );
        Array.Copy( m_BloomR, m_HalR, w2 * h2 );
        BoxBlurRows( m_HalB, w2, h2, rHal );
        BoxBlurRows( m_HalG, w2, h2, rHal );
        BoxBlurRows( m_HalR, w2, h2, rHal );
        BoxBlurColumns( m_HalB, w2, h2, rHal );
        BoxBlurColumns( m_HalG, w2, h2, rHal );
        BoxBlurColumns( m_HalR, w2, h2, rHal );

        int bloomMul = ( bloomStr * 256 ) / 100;
        int halMul   = ( halStr * 256 ) / 100;

        // Composite: src + bilinear-upsampled glow. Target pixel i maps to
        // half-res coordinate i/2 − 0.25, so the bilinear weights cycle
        // with period 2: even i → f=0.75 against cell (i/2 − 1), odd i →
        // f=0.25 against cell i/2.
        for ( int y = mapTop; y < mapBot; ++y )
        {
          int my = y - mapTop;
          int hy0 = ( ( my & 1 ) == 0 ) ? ( my / 2 ) - 1 : my / 2;
          int fy  = ( ( my & 1 ) == 0 ) ? 192 : 64;
          if ( hy0 < 0 )
          {
            hy0 = 0;
            fy  = 0;
          }
          int hy1 = ( hy0 + 1 < h2 ) ? hy0 + 1 : hy0;
          int rowBase0 = hy0 * w2;
          int rowBase1 = hy1 * w2;

          byte* s = pSrc + y * srcStride + mapLeft * 4;
          byte* d = pDst + y * dstStride + mapLeft * 4;
          for ( int x = mapLeft; x < mapRight; ++x )
          {
            int mx = x - mapLeft;
            int hx0 = ( ( mx & 1 ) == 0 ) ? ( mx / 2 ) - 1 : mx / 2;
            int fx  = ( ( mx & 1 ) == 0 ) ? 192 : 64;
            if ( hx0 < 0 )
            {
              hx0 = 0;
              fx  = 0;
            }
            int hx1 = ( hx0 + 1 < w2 ) ? hx0 + 1 : hx0;

            int w00 = ( 256 - fx ) * ( 256 - fy );
            int w10 = fx * ( 256 - fy );
            int w01 = ( 256 - fx ) * fy;
            int w11 = fx * fy;

            int glowB = ( m_BloomB[rowBase0 + hx0] * w00 + m_BloomB[rowBase0 + hx1] * w10
                        + m_BloomB[rowBase1 + hx0] * w01 + m_BloomB[rowBase1 + hx1] * w11 ) >> 16;
            int glowG = ( m_BloomG[rowBase0 + hx0] * w00 + m_BloomG[rowBase0 + hx1] * w10
                        + m_BloomG[rowBase1 + hx0] * w01 + m_BloomG[rowBase1 + hx1] * w11 ) >> 16;
            int glowR = ( m_BloomR[rowBase0 + hx0] * w00 + m_BloomR[rowBase0 + hx1] * w10
                        + m_BloomR[rowBase1 + hx0] * w01 + m_BloomR[rowBase1 + hx1] * w11 ) >> 16;

            int halB = ( m_HalB[rowBase0 + hx0] * w00 + m_HalB[rowBase0 + hx1] * w10
                       + m_HalB[rowBase1 + hx0] * w01 + m_HalB[rowBase1 + hx1] * w11 ) >> 16;
            int halG = ( m_HalG[rowBase0 + hx0] * w00 + m_HalG[rowBase0 + hx1] * w10
                       + m_HalG[rowBase1 + hx0] * w01 + m_HalG[rowBase1 + hx1] * w11 ) >> 16;
            int halR = ( m_HalR[rowBase0 + hx0] * w00 + m_HalR[rowBase0 + hx1] * w10
                       + m_HalR[rowBase1 + hx0] * w01 + m_HalR[rowBase1 + hx1] * w11 ) >> 16;

            int b = s[0] + ( ( glowB * bloomMul + halB * halMul ) >> 8 );
            int g = s[1] + ( ( glowG * bloomMul + halG * halMul ) >> 8 );
            int r = s[2] + ( ( glowR * bloomMul + halR * halMul ) >> 8 );
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



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Threshold );
      buf.AppendI32( BloomStrength );
      buf.AppendI32( BloomRadius );
      buf.AppendI32( HalationStrength );
      buf.AppendI32( HalationRadius );
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
      Threshold = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) BloomStrength    = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) BloomRadius      = GR.MathUtil.Clamp( 5, r.ReadInt32(), 40 );
      if ( r.Size - r.Position >= 4 ) HalationStrength = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) HalationRadius   = GR.MathUtil.Clamp( 20, r.ReadInt32(), 120 );
    }



    public override IDisplayFilter Clone()
    {
      return new BloomFilter
      {
        Enabled          = this.Enabled,
        Threshold        = this.Threshold,
        BloomStrength    = this.BloomStrength,
        BloomRadius      = this.BloomRadius,
        HalationStrength = this.HalationStrength,
        HalationRadius   = this.HalationRadius,
      };
    }
  }
}
