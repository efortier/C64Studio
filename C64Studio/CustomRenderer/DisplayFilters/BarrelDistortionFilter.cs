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
  /// <c>r_out = r_src · (1 + k·r_src²)</c>. That's positive-k with
  /// multiplicative factor, which by convention in CRT / game shaders
  /// pushes content outward at the corners — the same direction used by
  /// Prideout's barrel shader and OpenXcom's CRT-simple. (Note: the optics
  /// literature calls this "pincushion" under its sign convention; the
  /// visual-FX community calls it "barrel" because the resulting image
  /// looks bulged like a barrel. We use the visual-FX convention.)
  /// </para>
  /// <para>
  /// Rendering inverts this: for each TARGET pixel (u, v) we need the
  /// SOURCE (u_src, v_src) that forward-maps to (u, v). Since the forward
  /// is a cubic in r_src with no clean closed form, we use two Newton
  /// iterations starting from the first-order approximation
  /// u_src ≈ u / (1 + k·r²_out). Two iterations get within 0.1 pixel even
  /// at maximum curvature — more than adequate for a live preview.
  /// </para>
  /// <para>
  /// Higher <see cref="Curvature"/> means more bow; a <see cref="Vignette"/>
  /// knob fades the corners to black so the unused rim doesn't draw
  /// attention. Sampling is bilinear — without it, integer-floored reads
  /// alias into blocky moiré as the radial factor crosses pixel boundaries.
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



    /// <summary>0 (flat) .. 100 (heavy bow). Internally mapped to k ≤ 0.6.</summary>
    public int Curvature { get; set; } = 25;

    /// <summary>0 (no vignette) .. 100 (corners fully black).</summary>
    public int Vignette  { get; set; } = 20;



    public override string Name
    {
      get { return "Barrel Distortion"; }
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
      double k = ( curvature / 100.0 ) * 0.6;                // 0 .. 0.6
      double vStrength = vignette / 100.0;                   // 0 .. 1

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
        return;
      }

      double centerX = mapLeft + mapW * 0.5;
      double centerY = mapTop  + mapH * 0.5;
      double halfW   = mapW * 0.5;
      double halfH   = mapH * 0.5;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        for ( int y = 0; y < height; ++y )
        {
          byte* dstRow = pDst + y * dstStride;

          bool rowInMap = ( y >= mapTop ) && ( y < mapBot );
          for ( int x = 0; x < width; ++x )
          {
            byte* d = dstRow + x * 4;

            // Outside map region: passthrough.
            if ( ( !rowInMap )
            ||   ( x < mapLeft )
            ||   ( x >= mapRight ) )
            {
              byte* s = pSrc + y * srcStride + x * 4;
              d[0] = s[0];
              d[1] = s[1];
              d[2] = s[2];
              d[3] = s[3];
              continue;
            }

            // Normalize to [-1, +1] in the map's coord frame.
            double u = ( x - centerX ) / halfW;
            double v = ( y - centerY ) / halfH;
            double r2 = u * u + v * v;

            // Inverse of forward mapping  r_out = r_src · (1 + k·r_src²).
            // Start with the first-order approximation then refine with two
            // Newton iterations. We express the inverse as a scalar shrink
            // factor sh = r_src / r_out so both u and v scale by the same
            // number and the direction vector is preserved. Without the
            // refinement, at max curvature the actual source position
            // drifts several pixels from where a true forward warp would
            // pull it, which shows up as diagonal creep in the corners.
            double sh = 1.0 / ( 1.0 + k * r2 );
            for ( int iter = 0; iter < 2; ++iter )
            {
              // We want  sh · (1 + k · sh² · r2) = 1.
              // Newton step on  f(sh)  = sh + k·sh³·r2 - 1
              //                 f'(sh) = 1 + 3·k·sh²·r2
              double fsh  = sh + k * sh * sh * sh * r2 - 1.0;
              double dfsh = 1.0 + 3.0 * k * sh * sh * r2;
              sh -= fsh / dfsh;
            }
            double uSrc = u * sh;
            double vSrc = v * sh;

            // Back to target-pixel coordinates in the map's frame.
            double srcX = centerX + uSrc * halfW;
            double srcY = centerY + vSrc * halfH;

            // Outside the map rect after the warp? Render black so corners
            // don't reveal whatever was underneath before the filter ran.
            if ( ( srcX < mapLeft )
            ||   ( srcX >= mapRight - 1 )
            ||   ( srcY < mapTop )
            ||   ( srcY >= mapBot - 1 ) )
            {
              d[0] = 0;
              d[1] = 0;
              d[2] = 0;
              d[3] = 0xff;
              continue;
            }

            // Bilinear sample.
            int x0 = (int)Math.Floor( srcX );
            int y0 = (int)Math.Floor( srcY );
            int x1 = x0 + 1;
            int y1 = y0 + 1;
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

            // Vignette: darken toward the corners based on original radius.
            // Using post-warp r² would also work but the user tends to think
            // in terms of the rectangular frame, so match that intuition.
            if ( vStrength > 0.0 )
            {
              // r² goes from 0 at center to 2 at the corner (1² + 1²). Map to
              // 0..1 over the nominal [0..1] disc, then fade past that.
              double dim = 1.0 - vStrength * Math.Min( 1.0, Math.Max( 0.0, r2 - 0.4 ) / 1.6 );
              b *= dim;
              g *= dim;
              r *= dim;
            }

            if ( b > 255 ) b = 255; if ( b < 0 ) b = 0;
            if ( g > 255 ) g = 255; if ( g < 0 ) g = 0;
            if ( r > 255 ) r = 255; if ( r < 0 ) r = 0;

            d[0] = (byte)b;
            d[1] = (byte)g;
            d[2] = (byte)r;
            d[3] = 0xff;
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
      if ( r.Size - r.Position >= 4 ) Vignette = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new BarrelDistortionFilter
      {
        Enabled   = this.Enabled,
        Curvature = this.Curvature,
        Vignette  = this.Vignette,
      };
    }
  }
}
