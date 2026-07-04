using GR.Memory;
using System;
using System.Runtime.CompilerServices;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Phosphor persistence — the afterglow of a CRT. Phosphor keeps emitting
  /// after the beam moves on, decaying exponentially, so anything bright
  /// that moves (sprites!) drags a fading trail behind it.
  ///
  /// <para>
  /// The filter keeps a per-pixel history plane of the map rect. Every
  /// frame the history decays by wall-clock time (<see cref="HalfLifeMs"/>
  /// is the time for the glow to halve — frame-rate independent), then
  /// re-charges wherever the current image is brighter:
  /// <c>history = max(src, history·decay)</c>. What you SEE is
  /// <c>max(src, history·decay·Strength)</c> — <see cref="Strength"/> only
  /// scales the visible ghost, not the stored charge, so tuning it doesn't
  /// change how long trails last, only how loud they are. On a fully static
  /// image history converges to the image and the filter becomes an exact
  /// pass-through.
  /// </para>
  /// <para>
  /// This is the one temporal filter in the pipeline. The pipeline (and so
  /// this instance) is GLOBAL — shared by every open map editor — so the
  /// history is keyed by <see cref="FilterContext.StateKey"/> (one entry
  /// per host view, weakly held so closed editors get collected). Trail
  /// liveness is reported through
  /// <see cref="FilterContext.NeedsContinuousRepaint"/> so each view's
  /// repaint timer reflects only its own trail. The history is anchored to
  /// the map rect (the "screen"), exactly like a real tube — scrolling the
  /// view smears trails, which is what a camera pointed at a CRT would
  /// show. History resets when the rect size changes (zoom, resize) or
  /// after a long gap without paints (filter bypassed / tab hidden), so a
  /// stale ghost never resurfaces.
  /// </para>
  /// </summary>
  public class PhosphorPersistenceFilter : DisplayFilterBase
  {
    static PhosphorPersistenceFilter()
    {
      DisplayFilterRegistry.Register( typeof( PhosphorPersistenceFilter ) );
    }



    /// <summary>Glow half-life in milliseconds, 20..1000. P22-ish TV phosphor sits around 60-150.</summary>
    public int HalfLifeMs { get; set; } = 90;

    /// <summary>Visible trail intensity, 0 (off) .. 100 (full afterglow).</summary>
    public int Strength   { get; set; } = 70;



    private sealed class HistoryState
    {
      public byte[]  History;
      public int     Width;
      public int     Height;
      public int     LastTick;
    }

    // One history per host view. ConditionalWeakTable holds the key weakly:
    // when a map editor closes, its entry vanishes with it — no explicit
    // teardown required, no cross-document ghosting, no shared clock.
    private readonly ConditionalWeakTable<object, HistoryState>  m_States =
        new ConditionalWeakTable<object, HistoryState>();

    // Fallback key when the host doesn't provide one (standalone use) —
    // gives the exact single-view behavior of a per-instance field.
    private readonly object  m_FallbackKey = new object();



    public override string Name
    {
      get { return "Phosphor Persistence"; }
    }



    public override void Apply( FilterContext ctx )
    {
      if ( ( ctx == null )
      ||   ( ctx.SourceBuffer == null )
      ||   ( ctx.TargetBuffer == null ) )
      {
        return;
      }

      int halfLife = Math.Max( 20, Math.Min( 1000, HalfLifeMs ) );
      int strength = Math.Max( 0, Math.Min( 100, Strength ) );

      int width    = ctx.TargetBuffer.Width;
      int height   = ctx.TargetBuffer.Height;
      int mapTop   = Math.Max( 0, ctx.RenderOffsetY );
      int mapLeft  = Math.Max( 0, ctx.RenderOffsetX );
      int mapRight = Math.Min( width, ctx.RenderOffsetX + ctx.MapPixelWidth );
      int mapBot   = Math.Min( height, ctx.RenderOffsetY + ctx.MapPixelHeight );
      int mapW     = mapRight - mapLeft;
      int mapH     = mapBot - mapTop;

      object stateKey = ctx.StateKey ?? m_FallbackKey;

      if ( ( mapW <= 0 )
      ||   ( mapH <= 0 )
      ||   ( strength == 0 ) )
      {
        // Inert — drop this view's stored glow so re-enabling starts clean
        // and no invisible trail keeps a repaint loop alive.
        m_States.Remove( stateKey );
        CopyThrough( ctx );
        return;
      }

      var state = m_States.GetOrCreateValue( stateKey );

      int now = Environment.TickCount;
      int dt  = now - state.LastTick;
      state.LastTick = now;

      // Size change OR a long gap since the last pass (pipeline bypassed,
      // tab hidden, filter toggled) → the stored glow belongs to a stale
      // frame; recharge from scratch instead of ghosting old content in.
      bool freshHistory = ( state.History == null )
                       || ( state.Width != mapW )
                       || ( state.Height != mapH )
                       || ( dt > 2000 )
                       || ( dt < 0 );
      if ( freshHistory )
      {
        if ( ( state.History == null )
        ||   ( state.History.Length != mapW * mapH * 3 ) )
        {
          state.History = new byte[mapW * mapH * 3];
        }
        state.Width  = mapW;
        state.Height = mapH;
      }
      if ( dt < 1 )    dt = 1;
      if ( dt > 1000 ) dt = 1000;

      // Time-based exponential decay, capped at 255 so a burst of fast
      // repaints (dt rounding to "no decay") can never stall the trail —
      // every frame drains at least 1/256.
      int decayMul = (int)Math.Round( Math.Pow( 0.5, dt / (double)halfLife ) * 256.0 );
      if ( decayMul > 255 ) decayMul = 255;
      if ( decayMul < 0 )   decayMul = 0;

      int strengthMul = ( strength * 256 ) / 100;
      bool trailActive = false;

      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        int   srcStride = ctx.SourceBuffer.BytesPerLine;
        int   dstStride = ctx.TargetBuffer.BytesPerLine;

        // Chrome: whole buffer verbatim; map rect rewritten below.
        long bytes = (long)srcStride * height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );

        fixed ( byte* hist = state.History )
        {
          for ( int y = 0; y < mapH; ++y )
          {
            byte* s = pSrc + ( mapTop + y ) * srcStride + mapLeft * 4;
            byte* d = pDst + ( mapTop + y ) * dstStride + mapLeft * 4;
            byte* h = hist + y * mapW * 3;

            if ( freshHistory )
            {
              // First frame at this size: charge the phosphor with the
              // current image, no ghost to show yet.
              for ( int x = 0; x < mapW; ++x )
              {
                h[0] = s[0];
                h[1] = s[1];
                h[2] = s[2];
                d[0] = s[0];
                d[1] = s[1];
                d[2] = s[2];
                d[3] = s[3];
                s += 4;
                d += 4;
                h += 3;
              }
              continue;
            }

            for ( int x = 0; x < mapW; ++x )
            {
              int glowB = ( h[0] * decayMul ) >> 8;
              int glowG = ( h[1] * decayMul ) >> 8;
              int glowR = ( h[2] * decayMul ) >> 8;

              int srcB = s[0];
              int srcG = s[1];
              int srcR = s[2];

              // Recharge where the image outshines the glow.
              int newB = ( srcB > glowB ) ? srcB : glowB;
              int newG = ( srcG > glowG ) ? srcG : glowG;
              int newR = ( srcR > glowR ) ? srcR : glowR;
              h[0] = (byte)newB;
              h[1] = (byte)newG;
              h[2] = (byte)newR;
              if ( ( newB > srcB )
              ||   ( newG > srcG )
              ||   ( newR > srcR ) )
              {
                trailActive = true;
              }

              // Displayed ghost is strength-scaled; the image itself is
              // never dimmed (max, not blend).
              int visB = ( glowB * strengthMul ) >> 8;
              int visG = ( glowG * strengthMul ) >> 8;
              int visR = ( glowR * strengthMul ) >> 8;
              d[0] = (byte)( ( srcB > visB ) ? srcB : visB );
              d[1] = (byte)( ( srcG > visG ) ? srcG : visG );
              d[2] = (byte)( ( srcR > visR ) ? srcR : visR );
              d[3] = s[3];

              s += 4;
              d += 4;
              h += 3;
            }
          }
        }

        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }

      if ( trailActive )
      {
        ctx.NeedsContinuousRepaint = true;
      }
    }



    public override ByteBuffer SaveParameters()
    {
      var buf = new ByteBuffer();
      buf.AppendI32( HalfLifeMs );
      buf.AppendI32( Strength );
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
      HalfLifeMs = GR.MathUtil.Clamp( 20, r.ReadInt32(), 1000 );
      if ( r.Size - r.Position >= 4 ) Strength = GR.MathUtil.Clamp( 0, r.ReadInt32(), 100 );
    }



    public override IDisplayFilter Clone()
    {
      // History is runtime state, never cloned — a fresh instance charges
      // itself from the first frame it sees.
      return new PhosphorPersistenceFilter
      {
        Enabled    = this.Enabled,
        HalfLifeMs = this.HalfLifeMs,
        Strength   = this.Strength,
      };
    }
  }
}
