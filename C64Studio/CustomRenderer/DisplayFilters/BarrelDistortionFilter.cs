using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Radial (barrel) distortion to emulate the curvature of a CRT's glass
  /// envelope. Straight lines near the edges of the image bow outward the
  /// way they do on a real curved tube.
  ///
  /// <para>
  /// The forward mapping (source → output) is the standard visual-FX form
  /// <c>r_out = r_src · (1 + k·r_src²)</c>. Rendering needs the inverse,
  /// which has no closed form — it's solved once per curvature setting
  /// into a lookup table over r² (Newton iterations at build time), so the
  /// per-pixel cost is two multiplies and a table lerp instead of per-pixel
  /// Newton solves. Matters now that the map repaints at animation rate.
  /// </para>
  /// <para>
  /// <see cref="Vignette"/> fades the corners; <see cref="CornerRadius"/>
  /// rounds the visible corners off the way a real tube's faceplate does.
  /// Sampling is bilinear, with the outermost row/column clamped (the old
  /// code rejected them, which painted the map's right and bottom edges
  /// black even at zero curvature).
  /// </para>
  /// <para>
  /// Order in the pipeline: this filter is typically LAST, so it distorts
  /// the fully composited image (scanlines, phosphor, etc. all curve with
  /// the glass, just like a real CRT).
  /// </para>
  /// </summary>
  public class BarrelDistortionFilter : DisplayFilterBase
  {
    static BarrelDistortionFilter()
    {
      DisplayFilterRegistry.Register( typeof( BarrelDistortionFilter ) );
    }



    private const int LUT_SIZE = 2048;


    /// <summary>0 (flat) .. 100 (heavy bow). Internally mapped to k ≤ 0.6.</summary>
    public int Curvature    { get; set; } = 25;

    /// <summary>0 (no vignette) .. 100 (corners fully black).</summary>
    public int Vignette     { get; set; } = 20;

    /// <summary>0 (square corners) .. 100 (strongly rounded faceplate corners).</summary>
    public int CornerRadius { get; set; } = 0;



    // r²-indexed inverse-warp and vignette tables. The shrink factor
    // sh = r_src / r_out depends only on k and r², so one 1D table covers
    // every pixel; same for the vignette falloff. Rebuilt on param change.
    private double[]  m_ShTable;
    private int[]     m_DimTable;
    private int       m_LutKeyCurvature = int.MinValue;
    private int       m_LutKeyVignette  = int.MinValue;



    public override string Name
    {
      get { return "Barrel Distortion"; }
    }



    private void BuildLutsIfNeeded( int curvature, int vignette )
    {
      if ( ( m_ShTable != null )
      &&   ( m_LutKeyCurvature == curvature )
      &&   ( m_LutKeyVignette  == vignette ) )
      {
        return;
      }
      if ( m_ShTable == null )
      {
        m_ShTable  = new double[LUT_SIZE + 1];
        m_DimTable = new int[LUT_SIZE + 1];
      }

      double k         = ( curvature / 100.0 ) * 0.6;
      double vStrength = vignette / 100.0;

      for ( int i = 0; i <= LUT_SIZE; ++i )
      {
        double r2 = ( i / (double)LUT_SIZE ) * 2.0;    // r² ∈ [0, 2]

        // Invert  r_out = r_src · (1 + k·r_src²)  for the shrink factor
        // sh = r_src / r_out: first-order start, three Newton refinements
        // (build-time only, so the extra iteration is free).
        double sh = 1.0 / ( 1.0 + k * r2 );
        for ( int iter = 0; iter < 3; ++iter )
        {
          double fsh  = sh + k * sh * sh * sh * r2 - 1.0;
          double dfsh = 1.0 + 3.0 * k * sh * sh * r2;
          sh -= fsh / dfsh;
        }
        m_ShTable[i] = sh;

        // Vignette: r² is 0 at center, 2 at the corner. Fade starts at
        // r² = 0.4 and reaches full strength at the corner.
        double dim = 1.0 - vStrength * Math.Min( 1.0, Math.Max( 0.0, r2 - 0.4 ) / 1.6 );
        m_DimTable[i] = (int)Math.Round( dim * 256.0 );
      }

      m_LutKeyCurvature = curvature;
      m_LutKeyVignette  = vignette;
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int curvature = GR.MathUtil.Clamp( 0, Curvature, 100 );
      int vignette  = GR.MathUtil.Clamp( 0, Vignette,  100 );
      int cornerRad = GR.MathUtil.Clamp( 0, CornerRadius, 100 );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapLeft  = ctx.RenderOffsetX;
      int mapRight = ctx.RenderOffsetX + ctx.MapPixelWidth;
      int mapTop   = ctx.RenderOffsetY;
      int mapBot   = ctx.RenderOffsetY + ctx.MapPixelHeight;
      int mapW     = mapRight - mapLeft;
      int mapH     = mapBot   - mapTop;

      if ( ( mapW <= 0 )
      ||   ( mapH <= 0 ) )
      {
        // The pipeline swaps buffers after every enabled filter — bailing
        // without writing would hand stale scratch data downstream.
        CopyThrough( ctx );
        return;
      }

      BuildLutsIfNeeded( curvature, vignette );

      double centerX = mapLeft + mapW * 0.5;
      double centerY = mapTop  + mapH * 0.5;
      double halfW   = mapW * 0.5;
      double halfH   = mapH * 0.5;

      // Corner rounding in normalized coords: a pixel is outside the
      // faceplate when it lies beyond the quarter-circle of radius cr
      // centered (1-cr, 1-cr) into the corner.
      double cr = ( cornerRad / 100.0 ) * 0.5;
      double crSq = cr * cr;
      bool   roundCorners = ( cr > 0.0001 );

      double lutScale = LUT_SIZE / 2.0;    // r² ∈ [0,2] → table index

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( double* shTable = m_ShTable )
        fixed ( int* dimTable = m_DimTable )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* dstRow = pDst + y * dstStride;

            bool rowInMap = ( y >= mapTop ) && ( y < mapBot );
            if ( !rowInMap )
            {
              byte* srcRow = pSrc + y * srcStride;
              System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );
              continue;
            }

            double v  = ( y - centerY ) / halfH;
            double v2 = v * v;
            double av = Math.Abs( v );

            for ( int x = 0; x < width; ++x )
            {
              byte* d = dstRow + x * 4;

              // Outside map region: passthrough.
              if ( ( x < mapLeft )
              ||   ( x >= mapRight ) )
              {
                byte* s = pSrc + y * srcStride + x * 4;
                d[0] = s[0];
                d[1] = s[1];
                d[2] = s[2];
                d[3] = s[3];
                continue;
              }

              double u  = ( x - centerX ) / halfW;
              double r2 = u * u + v2;

              // Faceplate corner rounding — cheaper than the warp, test first.
              if ( roundCorners )
              {
                double dxx = Math.Abs( u ) - ( 1.0 - cr );
                double dyy = av - ( 1.0 - cr );
                if ( ( dxx > 0.0 )
                &&   ( dyy > 0.0 )
                &&   ( dxx * dxx + dyy * dyy > crSq ) )
                {
                  d[0] = 0;
                  d[1] = 0;
                  d[2] = 0;
                  d[3] = 0xff;
                  continue;
                }
              }

              // Inverse warp via the r² table (linear interpolation).
              double fi  = r2 * lutScale;
              int    i0  = (int)fi;
              if ( i0 >= LUT_SIZE )
              {
                i0 = LUT_SIZE - 1;
              }
              double ft  = fi - i0;
              double sh  = shTable[i0] + ( shTable[i0 + 1] - shTable[i0] ) * ft;

              double srcX = centerX + u * sh * halfW;
              double srcY = centerY + v * sh * halfH;

              // Strictly outside the map after the warp → black tube rim.
              // Note ">" against the last valid coordinate, not ">=" — the
              // old test blacked out the map's right/bottom edge pixels
              // even at zero curvature.
              if ( ( srcX < mapLeft )
              ||   ( srcX > mapRight - 1 )
              ||   ( srcY < mapTop )
              ||   ( srcY > mapBot - 1 ) )
              {
                d[0] = 0;
                d[1] = 0;
                d[2] = 0;
                d[3] = 0xff;
                continue;
              }

              // Bilinear sample with edge-clamped neighbors.
              int x0 = (int)srcX;
              int y0 = (int)srcY;
              int x1 = ( x0 + 1 < mapRight ) ? x0 + 1 : mapRight - 1;
              int y1 = ( y0 + 1 < mapBot )   ? y0 + 1 : mapBot - 1;
              double fx = srcX - x0;
              double fy = srcY - y0;

              byte* s00 = pSrc + y0 * srcStride + x0 * 4;
              byte* s10 = pSrc + y0 * srcStride + x1 * 4;
              byte* s01 = pSrc + y1 * srcStride + x0 * 4;
              byte* s11 = pSrc + y1 * srcStride + x1 * 4;

              double w00 = ( 1 - fx ) * ( 1 - fy );
              double w10 =       fx   * ( 1 - fy );
              double w01 = ( 1 - fx ) *       fy;
              double w11 =       fx   *       fy;

              double b = s00[0] * w00 + s10[0] * w10 + s01[0] * w01 + s11[0] * w11;
              double g = s00[1] * w00 + s10[1] * w10 + s01[1] * w01 + s11[1] * w11;
              double r = s00[2] * w00 + s10[2] * w10 + s01[2] * w01 + s11[2] * w11;

              int dim = dimTable[i0];
              if ( dim < 256 )
              {
                b = ( b * dim ) / 256.0;
                g = ( g * dim ) / 256.0;
                r = ( r * dim ) / 256.0;
              }

              if ( b > 255 ) b = 255; if ( b < 0 ) b = 0;
              if ( g > 255 ) g = 255; if ( g < 0 ) g = 0;
              if ( r > 255 ) r = 255; if ( r < 0 ) r = 0;

              d[0] = (byte)b;
              d[1] = (byte)g;
              d[2] = (byte)r;
              d[3] = s00[3];
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
      buf.AppendI32( Curvature );
      buf.AppendI32( Vignette );
      // Appended later: faceplate corner rounding.
      buf.AppendI32( CornerRadius );
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
      Curvature = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) Vignette     = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) CornerRadius = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new BarrelDistortionFilter
      {
        Enabled      = this.Enabled,
        Curvature    = this.Curvature,
        Vignette     = this.Vignette,
        CornerRadius = this.CornerRadius,
      };
    }
  }
}
