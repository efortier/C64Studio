using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Tints each column according to a three-phase R/G/B triad, emulating a
  /// CRT's phosphor mask (or aperture grille). Cheap and gives a readable
  /// "subpixel" feel without needing 4K output resolution.
  ///
  /// The pattern repeats every 3 target columns:
  ///   column 0 → boost R, dim G and B
  ///   column 1 → boost G, dim R and B
  ///   column 2 → boost B, dim R and G
  /// Tweakable per-channel boost / shared dim so users can dial this toward
  /// either a strong aperture-grille look or something subtle.
  /// </summary>
  public class PhosphorMaskFilter : DisplayFilterBase
  {
    static PhosphorMaskFilter()
    {
      DisplayFilterRegistry.Register( typeof( PhosphorMaskFilter ) );
    }



    /// <summary>R boost on R-triad column, 0..50 percent.</summary>
    public int  RBoost { get; set; } = 15;
    /// <summary>G boost on G-triad column, 0..50 percent.</summary>
    public int  GBoost { get; set; } = 15;
    /// <summary>B boost on B-triad column, 0..50 percent.</summary>
    public int  BBoost { get; set; } = 15;
    /// <summary>Dim applied to the non-triad channels in each column, 0..50 percent.</summary>
    public int  Dim    { get; set; } = 20;



    public override string Name
    {
      get { return "Phosphor Mask"; }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int rBoost = Math.Max( 0, Math.Min( 50, RBoost ) );
      int gBoost = Math.Max( 0, Math.Min( 50, GBoost ) );
      int bBoost = Math.Max( 0, Math.Min( 50, BBoost ) );
      int dim    = Math.Max( 0, Math.Min( 50, Dim ) );

      // Precompute per-column multipliers, one triple of {B, G, R} per phase.
      // We clamp to 511 so the *>>8 math can still produce 255 max after a
      // 100% input (255 * 256 / 256 = 255). Boost above 100% is possible in
      // principle but caps at saturation below.
      int mulBoostR = ( ( 100 + rBoost ) * 256 ) / 100;
      int mulBoostG = ( ( 100 + gBoost ) * 256 ) / 100;
      int mulBoostB = ( ( 100 + bBoost ) * 256 ) / 100;
      int mulDim    = ( ( 100 - dim )    * 256 ) / 100;

      // Indexed by (column % 3): B, G, R factors stored as 256-scale integers.
      int[] colB = new int[] { mulDim,    mulDim,    mulBoostB };
      int[] colG = new int[] { mulDim,    mulBoostG, mulDim    };
      int[] colR = new int[] { mulBoostR, mulDim,    mulDim    };

      int width  = ctx.TargetBuffer.Width;
      int height = ctx.TargetBuffer.Height;

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
            int phase = x - ctx.RenderOffsetX;
            // Only tint inside the map region; chrome / letterboxing stays
            // its natural color (otherwise the tint leaks onto the editor
            // background and looks wrong).
            if ( ( phase < 0 )
            ||   ( x >= ctx.RenderOffsetX + ctx.MapPixelWidth ) )
            {
              d[0] = s[0];
              d[1] = s[1];
              d[2] = s[2];
              d[3] = s[3];
              s += 4;
              d += 4;
              continue;
            }
            int p = phase % 3;

            int b = ( s[0] * colB[p] ) >> 8;
            int g = ( s[1] * colG[p] ) >> 8;
            int r = ( s[2] * colR[p] ) >> 8;
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
      buf.AppendI32( RBoost );
      buf.AppendI32( GBoost );
      buf.AppendI32( BBoost );
      buf.AppendI32( Dim );
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
      RBoost = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) GBoost = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) BBoost = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) Dim    = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
    }



    public override IDisplayFilter Clone()
    {
      return new PhosphorMaskFilter
      {
        Enabled = this.Enabled,
        RBoost  = this.RBoost,
        GBoost  = this.GBoost,
        BBoost  = this.BBoost,
        Dim     = this.Dim,
      };
    }
  }
}
