using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Emulates the physical phosphor structure of a CRT face. Three
  /// selectable geometries:
  /// <list type="bullet">
  ///   <item><b>Aperture grille</b> — continuous vertical R/G/B stripes
  ///   (Trinitron). The classic "pro monitor" look.</item>
  ///   <item><b>Slot mask</b> — R/G/B stripes broken into staggered slots
  ///   with dark separators (the overwhelming majority of consumer TVs a
  ///   C64 was plugged into).</item>
  ///   <item><b>Dot triad</b> — shadow-mask style staggered triads.</item>
  /// </list>
  ///
  /// The mask is a property of the tube glass, not of the video signal, so
  /// it tiles in TARGET pixel space and does not scale with zoom;
  /// <see cref="MaskScale"/> sets the stripe width in target pixels.
  ///
  /// Masking removes light (each phosphor column passes only "its" channel
  /// fully); <see cref="PreserveLuma"/> renormalizes each channel's average
  /// back toward the unfiltered level so the image doesn't just get darker.
  /// </summary>
  public class PhosphorMaskFilter : DisplayFilterBase
  {
    static PhosphorMaskFilter()
    {
      DisplayFilterRegistry.Register( typeof( PhosphorMaskFilter ) );
    }



    public const int PATTERN_APERTURE_GRILLE = 0;
    public const int PATTERN_SLOT_MASK       = 1;
    public const int PATTERN_DOT_TRIAD       = 2;


    /// <summary>R boost on R-triad column, 0..50 percent.</summary>
    public int  RBoost       { get; set; } = 15;
    /// <summary>G boost on G-triad column, 0..50 percent.</summary>
    public int  GBoost       { get; set; } = 15;
    /// <summary>B boost on B-triad column, 0..50 percent.</summary>
    public int  BBoost       { get; set; } = 15;
    /// <summary>Dim applied to the non-triad channels in each column (and slot/triad gaps), 0..50 percent.</summary>
    public int  Dim          { get; set; } = 20;
    /// <summary>Mask geometry — one of the PATTERN_* constants.</summary>
    public int  Pattern      { get; set; } = PATTERN_APERTURE_GRILLE;
    /// <summary>Width of one phosphor stripe in target pixels, 1..4.</summary>
    public int  MaskScale    { get; set; } = 1;
    /// <summary>How much of the mask's average brightness loss is compensated, 0..100.</summary>
    public int  PreserveLuma { get; set; } = 50;



    // Precomputed mask tile: per-channel multiplier (256-scale, up to 511
    // after luma compensation) per tile texel. Tiles are tiny (max 24x16 at
    // scale 4) and rebuilt only when a parameter changes.
    private int[]   m_TileB;
    private int[]   m_TileG;
    private int[]   m_TileR;
    private int     m_TileW;
    private int     m_TileH;
    private long    m_TileKey = long.MinValue;



    public override string Name
    {
      get { return "Phosphor Mask"; }
    }



    private void BuildTileIfNeeded( int rBoost, int gBoost, int bBoost, int dim,
                                    int pattern, int scale, int preserve )
    {
      long key = rBoost
               | ( (long)gBoost   << 8 )
               | ( (long)bBoost   << 16 )
               | ( (long)dim      << 24 )
               | ( (long)pattern  << 32 )
               | ( (long)scale    << 40 )
               | ( (long)preserve << 48 );
      if ( key == m_TileKey )
      {
        return;
      }

      int s = scale;
      switch ( pattern )
      {
        case PATTERN_SLOT_MASK:
          m_TileW = 6 * s;
          m_TileH = 4 * s;
          break;
        case PATTERN_DOT_TRIAD:
          m_TileW = 3 * s;
          m_TileH = 4 * s;
          break;
        default:
          m_TileW = 3 * s;
          m_TileH = 1;
          break;
      }
      int count = m_TileW * m_TileH;
      m_TileB = new int[count];
      m_TileG = new int[count];
      m_TileR = new int[count];

      int mulBoostR = ( ( 100 + rBoost ) * 256 ) / 100;
      int mulBoostG = ( ( 100 + gBoost ) * 256 ) / 100;
      int mulBoostB = ( ( 100 + bBoost ) * 256 ) / 100;
      int mulDim    = ( ( 100 - dim )    * 256 ) / 100;

      for ( int ty = 0; ty < m_TileH; ++ty )
      {
        for ( int tx = 0; tx < m_TileW; ++tx )
        {
          int idx = ty * m_TileW + tx;
          int stripe;
          bool inGap = false;

          switch ( pattern )
          {
            case PATTERN_SLOT_MASK:
              {
                // Two triad column groups; the second group's slots are
                // offset by half a slot height — the classic brick layout.
                int group     = tx / ( 3 * s );
                stripe        = ( tx / s ) % 3;
                int slotPhase = ( ty + group * 2 * s ) % ( 4 * s );
                inGap         = ( slotPhase >= 3 * s );
              }
              break;
            case PATTERN_DOT_TRIAD:
              {
                // Alternate bands shift the triad by 1.5 stripes.
                int band  = ty / ( 2 * s );
                int shift = ( band & 1 ) * ( ( 3 * s ) / 2 );
                stripe    = ( ( tx + shift ) / s ) % 3;
              }
              break;
            default:
              stripe = ( tx / s ) % 3;
              break;
          }

          if ( inGap )
          {
            // Slot separator: all channels dimmed — reads as the dark
            // horizontal seams between phosphor slots.
            m_TileB[idx] = mulDim;
            m_TileG[idx] = mulDim;
            m_TileR[idx] = mulDim;
          }
          else
          {
            // Stripe 0 = R phosphor, 1 = G, 2 = B (matches the historic
            // column order of this filter).
            m_TileR[idx] = ( stripe == 0 ) ? mulBoostR : mulDim;
            m_TileG[idx] = ( stripe == 1 ) ? mulBoostG : mulDim;
            m_TileB[idx] = ( stripe == 2 ) ? mulBoostB : mulDim;
          }
        }
      }

      // Luma preservation: scale each channel so its tile-average heads
      // back toward 256 (unfiltered). preserve=0 leaves the raw mask,
      // preserve=100 makes each channel's average exactly neutral.
      if ( preserve > 0 )
      {
        NormalizeChannel( m_TileB, count, preserve );
        NormalizeChannel( m_TileG, count, preserve );
        NormalizeChannel( m_TileR, count, preserve );
      }

      m_TileKey = key;
    }



    private static void NormalizeChannel( int[] tile, int count, int preserve )
    {
      long sum = 0;
      for ( int i = 0; i < count; ++i )
      {
        sum += tile[i];
      }
      double mean = sum / (double)count;
      if ( mean <= 0.0001 )
      {
        return;
      }
      double factor = 1.0 + ( preserve / 100.0 ) * ( 256.0 / mean - 1.0 );
      for ( int i = 0; i < count; ++i )
      {
        int v = (int)Math.Round( tile[i] * factor );
        if ( v < 0 )   v = 0;
        if ( v > 511 ) v = 511;
        tile[i] = v;
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

      int rBoost   = Math.Max( 0, Math.Min( 50, RBoost ) );
      int gBoost   = Math.Max( 0, Math.Min( 50, GBoost ) );
      int bBoost   = Math.Max( 0, Math.Min( 50, BBoost ) );
      int dim      = Math.Max( 0, Math.Min( 50, Dim ) );
      int pattern  = Math.Max( 0, Math.Min( 2, Pattern ) );
      int scale    = Math.Max( 1, Math.Min( 4, MaskScale ) );
      int preserve = Math.Max( 0, Math.Min( 100, PreserveLuma ) );

      BuildTileIfNeeded( rBoost, gBoost, bBoost, dim, pattern, scale, preserve );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = ctx.RenderOffsetY;
      int mapBot   = ctx.RenderOffsetY + ctx.MapPixelHeight;
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int tileW    = m_TileW;
      int tileH    = m_TileH;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( int* tileB = m_TileB, tileG = m_TileG, tileR = m_TileR )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* srcRow = pSrc + y * srcStride;
            byte* dstRow = pDst + y * dstStride;

            // Full-row copy first so chrome on ALL four sides of the map
            // stays untinted; the map span is overwritten below.
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );

            if ( ( y < mapTop )
            ||   ( y >= mapBot ) )
            {
              continue;
            }

            int  ty       = ( y - mapTop ) % tileH;
            int* rowTileB = tileB + ty * tileW;
            int* rowTileG = tileG + ty * tileW;
            int* rowTileR = tileR + ty * tileW;

            byte* s  = srcRow + mapLeft * 4;
            byte* d  = dstRow + mapLeft * 4;
            int   tx = 0;
            for ( int x = mapLeft; x < mapRight; ++x )
            {
              int b = ( s[0] * rowTileB[tx] ) >> 8;
              int g = ( s[1] * rowTileG[tx] ) >> 8;
              int r = ( s[2] * rowTileR[tx] ) >> 8;
              if ( b > 255 ) b = 255;
              if ( g > 255 ) g = 255;
              if ( r > 255 ) r = 255;
              d[0] = (byte)b;
              d[1] = (byte)g;
              d[2] = (byte)r;
              d[3] = s[3];

              s += 4;
              d += 4;
              if ( ++tx >= tileW )
              {
                tx = 0;
              }
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
      buf.AppendI32( RBoost );
      buf.AppendI32( GBoost );
      buf.AppendI32( BBoost );
      buf.AppendI32( Dim );
      // Appended later: pattern geometry + scale + luma compensation.
      buf.AppendI32( Pattern );
      buf.AppendI32( MaskScale );
      buf.AppendI32( PreserveLuma );
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
      if ( r.Size - r.Position >= 4 ) GBoost       = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) BBoost       = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) Dim          = GR.MathUtil.Clamp( 0, r.ReadInt32(), 50 );
      if ( r.Size - r.Position >= 4 ) Pattern      = GR.MathUtil.Clamp( 0, r.ReadInt32(), 2 );
      if ( r.Size - r.Position >= 4 ) MaskScale    = GR.MathUtil.Clamp( 1, r.ReadInt32(), 4 );
      if ( r.Size - r.Position >= 4 ) PreserveLuma = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new PhosphorMaskFilter
      {
        Enabled      = this.Enabled,
        RBoost       = this.RBoost,
        GBoost       = this.GBoost,
        BBoost       = this.BBoost,
        Dim          = this.Dim,
        Pattern      = this.Pattern,
        MaskScale    = this.MaskScale,
        PreserveLuma = this.PreserveLuma,
      };
    }
  }
}
