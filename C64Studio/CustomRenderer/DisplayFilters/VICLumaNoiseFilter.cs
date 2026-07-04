using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// VIC-II bus-activity luma interference — the fine VERTICAL striping
  /// visible over the whole picture on a real C64 (documented as the "AEC
  /// glitch" in the palstuff research: the activity of internal signals,
  /// most notably the AEC line, couples into the luma output). The pattern
  /// repeats with the 8-pixel character fetch cycle, giving the picture a
  /// subtle woven texture that pure signal simulation misses because it
  /// originates INSIDE the chip, not in the cable.
  ///
  /// Implementation: a deterministic per-source-column luma offset table —
  /// an 8-px base profile (the fetch cycle) plus optional finer sub-detail,
  /// with slow pseudo-random variation across character columns so the
  /// texture doesn't tile too perfectly. Columns are locked to SOURCE
  /// pixels (the artifact lives in VIC pixel time), scaled by zoom.
  /// </summary>
  public class VICLumaNoiseFilter : DisplayFilterBase
  {
    static VICLumaNoiseFilter()
    {
      DisplayFilterRegistry.Register( typeof( VICLumaNoiseFilter ) );
    }



    /// <summary>Stripe amplitude, 0 (off) .. 100 (blatant). Real hardware sits around 20-35.</summary>
    public int Strength   { get; set; } = 25;

    /// <summary>1 = 8-px fetch-cycle profile only; 2 = adds finer 4-px sub-detail.</summary>
    public int Coarseness { get; set; } = 1;



    // Per-source-column offset table, 256-scale signed luma deltas.
    // Rebuilt when parameters or the visible source width change.
    private int[]  m_ColumnOffset;
    private int    m_KeyStrength  = int.MinValue;
    private int    m_KeyCoarse    = int.MinValue;
    private int    m_KeyWidth     = int.MinValue;

    // The 8-px fetch-cycle luma profile (arbitrary units, roughly matching
    // the sawtooth-ish look of the aec_nmos reference shot).
    private static readonly int[] s_FetchProfile = { 10, -6, 3, -9, 7, -4, 9, -10 };
    private static readonly int[] s_SubProfile   = { 4, -4, -3, 3 };



    public override string Name
    {
      get { return "VIC-II Luma Noise"; }
    }



    private void BuildTableIfNeeded( int strength, int coarseness, int srcWidth )
    {
      if ( ( m_ColumnOffset != null )
      &&   ( m_KeyStrength == strength )
      &&   ( m_KeyCoarse == coarseness )
      &&   ( m_KeyWidth == srcWidth ) )
      {
        return;
      }
      m_ColumnOffset = new int[srcWidth];

      // Peak amplitude in luma units (0..255 scale): Strength 100 → ±12.
      double amp = strength * 12.0 / 100.0;

      for ( int c = 0; c < srcWidth; ++c )
      {
        double v = s_FetchProfile[c & 7] / 10.0;
        if ( coarseness >= 2 )
        {
          v += s_SubProfile[c & 3] / 10.0 * 0.6;
        }
        // Slow per-character-column variation so the texture doesn't tile
        // perfectly — deterministic integer hash, no RNG (stable frames).
        uint h = (uint)( c >> 3 ) * 2654435761u;
        h ^= h >> 13;
        double wobble = 0.75 + ( ( h & 0xFF ) / 255.0 ) * 0.5;   // 0.75..1.25
        m_ColumnOffset[c] = (int)Math.Round( v * wobble * amp );
      }

      m_KeyStrength = strength;
      m_KeyCoarse   = coarseness;
      m_KeyWidth    = srcWidth;
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int strength   = Math.Max( 0, Math.Min( 100, Strength ) );
      int coarseness = Math.Max( 1, Math.Min( 2, Coarseness ) );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = Math.Max( 0, ctx.RenderOffsetY );
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int mapBot   = Math.Min( height, ctx.RenderOffsetY + ctx.MapPixelHeight );
      int mapW     = mapRight - mapLeft;
      int srcW     = ctx.SourceWidth;

      if ( ( strength == 0 )
      ||   ( mapW <= 0 )
      ||   ( mapBot - mapTop <= 0 )
      ||   ( srcW <= 0 ) )
      {
        CopyThrough( ctx );
        return;
      }

      BuildTableIfNeeded( strength, coarseness, srcW );

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        fixed ( int* offsets = m_ColumnOffset )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* srcRow = pSrc + y * srcStride;
            byte* dstRow = pDst + y * dstStride;

            // Chrome untouched on all four sides.
            System.Buffer.MemoryCopy( srcRow, dstRow, dstStride, dstStride );

            if ( ( y < mapTop )
            ||   ( y >= mapBot ) )
            {
              continue;
            }

            byte* s = srcRow + mapLeft * 4;
            byte* d = dstRow + mapLeft * 4;
            for ( int x = mapLeft; x < mapRight; ++x )
            {
              int col = (int)( (long)( x - mapLeft ) * srcW / mapW );
              int off = offsets[col];

              int b = s[0] + off;
              int g = s[1] + off;
              int r = s[2] + off;
              if ( b < 0 ) b = 0; if ( b > 255 ) b = 255;
              if ( g < 0 ) g = 0; if ( g > 255 ) g = 255;
              if ( r < 0 ) r = 0; if ( r > 255 ) r = 255;
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



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( Strength );
      buf.AppendI32( Coarseness );
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
      if ( r.Size - r.Position >= 4 ) Coarseness = GR.MathUtil.Clamp( 1, r.ReadInt32(), 2 );
    }



    public override IDisplayFilter Clone()
    {
      return new VICLumaNoiseFilter
      {
        Enabled    = this.Enabled,
        Strength   = this.Strength,
        Coarseness = this.Coarseness,
      };
    }
  }
}
