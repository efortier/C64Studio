using GR.Memory;
using System;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Shifts the image's color temperature and tint to emulate different CRT
  /// whitepoints. Different CRT models ran warmer (red-biased) or cooler
  /// (blue-biased) than 6500K, and PAL vs NTSC tubes drifted differently on
  /// the magenta-green axis. The Guest Advanced shader exposes essentially
  /// the same two knobs; this is our equivalent.
  ///
  /// Implementation is per-channel multiplicative gain via a 256-entry LUT
  /// per channel, rebuilt only when either knob changes.
  ///
  /// <para>
  /// Temperature is a red-vs-blue trade: positive values bias toward red
  /// (warm), negative toward blue (cool). Tint is a magenta-vs-green trade:
  /// positive toward magenta, negative toward green.
  /// </para>
  /// </summary>
  public class ColorTemperatureFilter : DisplayFilterBase
  {
    static ColorTemperatureFilter()
    {
      DisplayFilterRegistry.Register( typeof( ColorTemperatureFilter ) );
    }



    /// <summary>-100 (cool, blue-biased) .. +100 (warm, red-biased). 0 = neutral.</summary>
    public int Temperature { get; set; } = 0;

    /// <summary>-100 (green) .. +100 (magenta). 0 = neutral.</summary>
    public int Tint        { get; set; } = 0;



    public override string Name
    {
      get { return "Color Temperature"; }
    }



    private byte[] m_LutR = null;
    private byte[] m_LutG = null;
    private byte[] m_LutB = null;
    private int    m_LutKeyTemp = int.MinValue;
    private int    m_LutKeyTint = int.MinValue;



    private void BuildLutsIfNeeded( int temperature, int tint )
    {
      if ( ( m_LutR != null )
      &&   ( m_LutKeyTemp == temperature )
      &&   ( m_LutKeyTint == tint ) )
      {
        return;
      }
      if ( m_LutR == null ) m_LutR = new byte[256];
      if ( m_LutG == null ) m_LutG = new byte[256];
      if ( m_LutB == null ) m_LutB = new byte[256];

      // Scale so ±100 on either slider produces a substantial but not
      // clipping-prone shift. 0.3 magnitude on a channel gain is plenty:
      // at +100 Temperature, R *= 1.3, B *= 0.7 — a clearly warm look
      // without clipping bright reds to pure 255 everywhere.
      double t = temperature / 100.0;                // -1..+1
      double n = tint        / 100.0;                // -1..+1

      double rMul = 1.0 + 0.30 * t;
      double bMul = 1.0 - 0.30 * t;
      double gMul = 1.0 - 0.20 * n;                  // magenta pushes G down
      // Magenta also slightly lifts R and B; green lifts G.
      rMul *= 1.0 + 0.10 * n;
      bMul *= 1.0 + 0.10 * n;

      if ( rMul < 0.0 ) rMul = 0.0;
      if ( gMul < 0.0 ) gMul = 0.0;
      if ( bMul < 0.0 ) bMul = 0.0;

      for ( int i = 0; i < 256; ++i )
      {
        int r = (int)Math.Round( i * rMul );
        int g = (int)Math.Round( i * gMul );
        int b = (int)Math.Round( i * bMul );
        if ( r > 255 ) r = 255;
        if ( g > 255 ) g = 255;
        if ( b > 255 ) b = 255;
        if ( r < 0 )   r = 0;
        if ( g < 0 )   g = 0;
        if ( b < 0 )   b = 0;
        m_LutR[i] = (byte)r;
        m_LutG[i] = (byte)g;
        m_LutB[i] = (byte)b;
      }

      m_LutKeyTemp = temperature;
      m_LutKeyTint = tint;
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int temperature = GR.MathUtil.Clamp( -100, Temperature, 100 );
      int tint        = GR.MathUtil.Clamp( -100, Tint,        100 );

      BuildLutsIfNeeded( temperature, tint );

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

        fixed ( byte* pLutR = m_LutR, pLutG = m_LutG, pLutB = m_LutB )
        {
          for ( int y = 0; y < height; ++y )
          {
            byte* s = pSrc + y * srcStride;
            byte* d = pDst + y * dstStride;
            bool  rowInMap = ( y >= mapTop ) && ( y < mapBot );

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
                // 32bpp BGRA: byte 0=B, 1=G, 2=R, 3=A.
                d[0] = pLutB[s[0]];
                d[1] = pLutG[s[1]];
                d[2] = pLutR[s[2]];
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
      buf.AppendI32( Temperature );
      buf.AppendI32( Tint );
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
      Temperature = GR.MathUtil.Clamp( -100, r.ReadInt32(), 100 );
      if ( r.Size - r.Position >= 4 ) Tint = GR.MathUtil.Clamp( -100, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      return new ColorTemperatureFilter
      {
        Enabled     = this.Enabled,
        Temperature = this.Temperature,
        Tint        = this.Tint,
      };
    }
  }
}
