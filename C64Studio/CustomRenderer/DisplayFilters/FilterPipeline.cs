using GR.Image;
using GR.Memory;
using System;
using System.Collections.Generic;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Runs a list of <see cref="IDisplayFilter"/> passes over a target image.
  /// Owns one scratch buffer so adding a filter is one raster and one swap,
  /// not an allocation per pass.
  ///
  /// Typical use from an editor:
  /// <code>
  /// if ( !m_Pipeline.HasAnyEnabled )
  /// {
  ///   return;                 // fast path, no overhead
  /// }
  /// var ctx = new FilterContext { ... };
  /// m_Pipeline.Apply( TargetBuffer, ctx );
  /// </code>
  /// </summary>
  public class FilterPipeline
  {
    /// <summary>
    /// Filters in execution order. Consumers mutate this directly (add,
    /// remove, reorder); the pipeline has no hidden state tied to the list
    /// except the scratch buffer.
    /// </summary>
    public List<IDisplayFilter> Filters { get; } = new List<IDisplayFilter>();

    /// <summary>
    /// Reused scratch buffer for the ping-pong dance; resized on demand to
    /// match the incoming target. Intentionally not exposed.
    /// </summary>
    private FastImage           m_Scratch;



    public bool HasAnyEnabled
    {
      get
      {
        foreach ( var f in Filters )
        {
          if ( f.Enabled )
          {
            return true;
          }
        }
        return false;
      }
    }



    /// <summary>
    /// Run every enabled filter in order. On return, <paramref name="target"/>
    /// contains the final filtered image. When no filters are enabled this
    /// method is a no-op and does not touch the scratch buffer.
    ///
    /// The ping-pong invariant: we pick the number of enabled passes up
    /// front; if that count is even, the first filter writes to Scratch and
    /// we swap each pass so the final write lands back in target. If odd,
    /// we pre-copy target → scratch and start with target as the write side.
    /// Either way the last filter's TargetBuffer == the caller's target.
    /// </summary>
    public void Apply( FastImage target, FilterContext baseCtx )
    {
      if ( ( target == null )
      ||   ( baseCtx == null ) )
      {
        return;
      }

      // Count enabled filters first — we need to know parity to decide
      // where to start so we land in the caller's buffer on the last pass.
      int enabledCount = 0;
      foreach ( var f in Filters )
      {
        if ( f.Enabled )
        {
          ++enabledCount;
        }
      }
      if ( enabledCount == 0 )
      {
        return;
      }

      EnsureScratchMatches( target );

      // Output flag: temporal filters set it during their pass; the caller
      // reads it after Apply to know whether to keep repainting this view.
      baseCtx.NeedsContinuousRepaint = false;

      FastImage reader;
      FastImage writer;
      if ( ( enabledCount & 1 ) == 1 )
      {
        // Odd number of passes — one copy up front, then end on target.
        CopyImage( target, m_Scratch );
        reader = m_Scratch;
        writer = target;
      }
      else
      {
        // Even — start by reading target (clean input) and writing to scratch.
        // After all swaps, the final write lands on target.
        reader = target;
        writer = m_Scratch;
      }

      baseCtx.SourceBuffer = reader;
      baseCtx.TargetBuffer = writer;

      foreach ( var filter in Filters )
      {
        if ( !filter.Enabled )
        {
          continue;
        }
        baseCtx.SourceBuffer = reader;
        baseCtx.TargetBuffer = writer;

        filter.Apply( baseCtx );

        // Swap for next pass.
        var tmp = reader;
        reader = writer;
        writer = tmp;
      }
    }



    private void EnsureScratchMatches( FastImage target )
    {
      if ( m_Scratch == null )
      {
        m_Scratch = new FastImage( target.Width, target.Height, target.PixelFormat );
        return;
      }
      // Pixel format can't be changed by Resize, so for that (rare) case we
      // recreate. Size-only mismatch uses the cheaper in-place resize.
      if ( m_Scratch.PixelFormat != target.PixelFormat )
      {
        m_Scratch.Dispose();
        m_Scratch = new FastImage( target.Width, target.Height, target.PixelFormat );
      }
      else if ( ( m_Scratch.Width != target.Width )
      ||        ( m_Scratch.Height != target.Height ) )
      {
        m_Scratch.Resize( target.Width, target.Height );
      }
    }



    /// <summary>
    /// Bulk pixel copy via raw pointers — faster than per-pixel GetPixel/SetPixel
    /// and doesn't go through GDI. Assumes both images have the same size and
    /// stride (we just checked that in <see cref="EnsureScratchMatches"/>).
    /// </summary>
    private static void CopyImage( FastImage src, FastImage dst )
    {
      unsafe
      {
        byte* pSrc = (byte*)src.PinData();
        byte* pDst = (byte*)dst.PinData();
        int   bytes = src.BytesPerLine * src.Height;
        // System.Buffer.MemoryCopy works on net4.8 and net8; avoids the
        // GDI CopyMemory dependency and has no alignment gotchas.
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );
        src.UnpinData();
        dst.UnpinData();
      }
    }



    // ================================================================
    // Persistence
    //
    // Layout:
    //   u16  version (currently 1)
    //   i32  filterCount
    //   per filter:
    //     string    TypeName (class name, e.g. "ScanlineFilter")
    //     u8        Enabled flag
    //     i32       ParamBlobLength
    //     bytes[]   ParamBlob  (opaque, parsed by the filter itself)
    //
    // Unknown TypeName on load → skip that filter (we read and discard the
    // paramBlob by length). That keeps new settings files readable by older
    // builds and vice versa.
    // ================================================================

    private const ushort SAVE_VERSION = 1;



    public ByteBuffer SaveToBuffer()
    {
      var buf = new ByteBuffer();
      buf.AppendU16( SAVE_VERSION );
      buf.AppendI32( Filters.Count );
      foreach ( var filter in Filters )
      {
        buf.AppendString( filter.GetType().Name );
        buf.AppendU8( (byte)( filter.Enabled ? 1 : 0 ) );
        var paramBlob = filter.SaveParameters() ?? new ByteBuffer();
        buf.AppendI32( (int)paramBlob.Length );
        buf.Append( paramBlob );
      }
      return buf;
    }



    public void LoadFromBuffer( ByteBuffer buf )
    {
      Filters.Clear();
      if ( ( buf == null )
      ||   ( buf.Length == 0 ) )
      {
        return;
      }
      var reader = buf.MemoryReader();
      // Version byte lets us evolve the outer envelope; individual filters
      // handle their own backward compat via their param-blob length. A
      // version HIGHER than ours means the envelope layout itself may have
      // changed — parsing it as v1 would misread, so bail to an empty
      // pipeline instead (same net effect as "settings from the future").
      ushort version = reader.ReadUInt16();
      if ( ( version == 0 )
      ||   ( version > SAVE_VERSION ) )
      {
        return;
      }
      int filterCount = reader.ReadInt32();
      for ( int i = 0; i < filterCount; ++i )
      {
        string typeName  = reader.ReadString();
        bool   enabled   = ( reader.ReadUInt8() != 0 );
        int    blobLen   = reader.ReadInt32();
        var    paramBlob = new ByteBuffer();
        if ( blobLen > 0 )
        {
          reader.ReadBlock( paramBlob, (uint)blobLen );
        }
        var filter = DisplayFilterRegistry.Create( typeName );
        if ( filter == null )
        {
          // Unknown filter — probably from a newer build. Skip gracefully.
          continue;
        }
        filter.Enabled = enabled;
        try
        {
          filter.LoadParameters( paramBlob );
        }
        catch ( Exception )
        {
          // Malformed param data — keep the filter with defaults rather
          // than drop it entirely.
        }
        Filters.Add( filter );
      }
    }



    /// <summary>
    /// Deep-copy snapshot — used by the settings dialog to keep a pristine
    /// "original" around so Revert can restore it if the user cancels.
    /// </summary>
    public FilterPipeline Clone()
    {
      var copy = new FilterPipeline();
      foreach ( var f in Filters )
      {
        copy.Filters.Add( f.Clone() );
      }
      return copy;
    }
  }
}
