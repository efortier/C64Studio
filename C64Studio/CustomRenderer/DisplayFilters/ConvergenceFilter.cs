using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Convergence error — a CRT's three electron beams never land perfectly
  /// on top of each other, so the red and blue images sit fractionally off
  /// from the green one. Shows up as subtle colored fringes on contrast
  /// edges and is one of the most instantly-recognizable "real tube" cues.
  ///
  /// Shifts are given in TENTHS of a source pixel and scale with zoom, so
  /// a 0.4px misconvergence looks like 0.4 of a C64 pixel at every
  /// magnification. Green stays put (the eye is most sensitive to it, and
  /// service menus aligned R/B to G); red and blue get independent X/Y
  /// offsets, sampled bilinearly and clamped to the map rect.
  /// </summary>
  public class ConvergenceFilter : DisplayFilterBase
  {
    static ConvergenceFilter()
    {
      DisplayFilterRegistry.Register( typeof( ConvergenceFilter ) );
    }



    /// <summary>Red beam X offset in 1/10 source pixels, -30..30.</summary>
    public int RShiftX { get; set; } = -4;
    /// <summary>Red beam Y offset in 1/10 source pixels, -30..30.</summary>
    public int RShiftY { get; set; } = 0;
    /// <summary>Blue beam X offset in 1/10 source pixels, -30..30.</summary>
    public int BShiftX { get; set; } = 4;
    /// <summary>Blue beam Y offset in 1/10 source pixels, -30..30.</summary>
    public int BShiftY { get; set; } = 0;



    public override string Name
    {
      get { return "Convergence Error"; }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int rShiftX = Math.Max( -30, Math.Min( 30, RShiftX ) );
      int rShiftY = Math.Max( -30, Math.Min( 30, RShiftY ) );
      int bShiftX = Math.Max( -30, Math.Min( 30, BShiftX ) );
      int bShiftY = Math.Max( -30, Math.Min( 30, BShiftY ) );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = Math.Max( 0, ctx.RenderOffsetY );
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int mapBot   = Math.Min( height, ctx.RenderOffsetY + ctx.MapPixelHeight );

      if ( ( ( rShiftX == 0 ) && ( rShiftY == 0 ) && ( bShiftX == 0 ) && ( bShiftY == 0 ) )
      ||   ( mapRight - mapLeft <= 1 )
      ||   ( mapBot - mapTop <= 1 )
      ||   ( ctx.SourceWidth <= 0 )
      ||   ( ctx.SourceHeight <= 0 ) )
      {
        CopyThrough( ctx );
        return;
      }

      double zoomX = ctx.MapPixelWidth  / (double)ctx.SourceWidth;
      double zoomY = ctx.MapPixelHeight / (double)ctx.SourceHeight;

      // The displacement is constant across the frame, so the bilinear
      // footprint is too: integer offset + 256-scale fractional weight per
      // axis per channel, computed once. Sampling at (x - dx): the beam
      // painting channel C at x actually deposited the pixel value from
      // x - dx.
      SplitShift( ( rShiftX / 10.0 ) * zoomX, out int rOffX, out int rFracX );
      SplitShift( ( rShiftY / 10.0 ) * zoomY, out int rOffY, out int rFracY );
      SplitShift( ( bShiftX / 10.0 ) * zoomX, out int bOffX, out int bFracX );
      SplitShift( ( bShiftY / 10.0 ) * zoomY, out int bOffY, out int bFracY );

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        // Chrome: whole buffer verbatim; map rect rewritten below.
        long bytes = (long)srcStride * height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );

        for ( int y = mapTop; y < mapBot; ++y )
        {
          // Clamp the two source rows per channel once per row.
          int rY0 = ClampCoord( y - rOffY,     mapTop, mapBot - 1 );
          int rY1 = ClampCoord( y - rOffY - 1, mapTop, mapBot - 1 );
          int bY0 = ClampCoord( y - bOffY,     mapTop, mapBot - 1 );
          int bY1 = ClampCoord( y - bOffY - 1, mapTop, mapBot - 1 );

          byte* srcRow  = pSrc + y * srcStride;
          byte* rRow0   = pSrc + rY0 * srcStride;
          byte* rRow1   = pSrc + rY1 * srcStride;
          byte* bRow0   = pSrc + bY0 * srcStride;
          byte* bRow1   = pSrc + bY1 * srcStride;
          byte* d       = pDst + y * dstStride + mapLeft * 4;

          for ( int x = mapLeft; x < mapRight; ++x )
          {
            int rX0 = ClampCoord( x - rOffX,     mapLeft, mapRight - 1 );
            int rX1 = ClampCoord( x - rOffX - 1, mapLeft, mapRight - 1 );
            int bX0 = ClampCoord( x - bOffX,     mapLeft, mapRight - 1 );
            int bX1 = ClampCoord( x - bOffX - 1, mapLeft, mapRight - 1 );

            // Bilinear per shifted channel; byte 2 = R, byte 0 = B.
            int r = ( rRow0[rX0 * 4 + 2] * ( 256 - rFracX ) + rRow0[rX1 * 4 + 2] * rFracX ) * ( 256 - rFracY )
                  + ( rRow1[rX0 * 4 + 2] * ( 256 - rFracX ) + rRow1[rX1 * 4 + 2] * rFracX ) * rFracY;
            int b = ( bRow0[bX0 * 4] * ( 256 - bFracX ) + bRow0[bX1 * 4] * bFracX ) * ( 256 - bFracY )
                  + ( bRow1[bX0 * 4] * ( 256 - bFracX ) + bRow1[bX1 * 4] * bFracX ) * bFracY;

            byte* s = srcRow + x * 4;
            d[0] = (byte)( b >> 16 );
            d[1] = s[1];
            d[2] = (byte)( r >> 16 );
            d[3] = s[3];
            d += 4;
          }
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    /// <summary>
    /// Split a (possibly negative) sub-pixel displacement into the integer
    /// offset of the FIRST bilinear tap and the 256-scale weight of the
    /// second tap (one pixel further back).
    /// </summary>
    private static void SplitShift( double shift, out int offset, out int frac )
    {
      double floor = Math.Floor( shift );
      offset = (int)floor;
      frac   = (int)Math.Round( ( shift - floor ) * 256.0 );
      if ( frac >= 256 )
      {
        offset += 1;
        frac = 0;
      }
    }



    private static int ClampCoord( int v, int min, int max )
    {
      if ( v < min ) return min;
      if ( v > max ) return max;
      return v;
    }



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( RShiftX );
      buf.AppendI32( RShiftY );
      buf.AppendI32( BShiftX );
      buf.AppendI32( BShiftY );
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
      RShiftX = GR.MathUtil.Clamp( -30, r.ReadInt32(), 30 );
      if ( r.Size - r.Position >= 4 ) RShiftY = GR.MathUtil.Clamp( -30, r.ReadInt32(), 30 );
      if ( r.Size - r.Position >= 4 ) BShiftX = GR.MathUtil.Clamp( -30, r.ReadInt32(), 30 );
      if ( r.Size - r.Position >= 4 ) BShiftY = GR.MathUtil.Clamp( -30, r.ReadInt32(), 30 );
    }



    public override IDisplayFilter Clone()
    {
      return new ConvergenceFilter
      {
        Enabled = this.Enabled,
        RShiftX = this.RShiftX,
        RShiftY = this.RShiftY,
        BShiftX = this.BShiftX,
        BShiftY = this.BShiftY,
      };
    }
  }
}
