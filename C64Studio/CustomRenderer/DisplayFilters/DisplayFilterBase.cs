using GR.Memory;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// Shared boilerplate for filter implementations: Name, Enabled, and
  /// default Save/Load that writes an empty buffer (used by filters that
  /// have no tunable parameters). Subclasses override Save/Load once they
  /// actually have something to persist.
  /// </summary>
  public abstract class DisplayFilterBase : IDisplayFilter
  {
    public abstract string Name { get; }

    public bool Enabled { get; set; } = true;

    public abstract void Apply( FilterContext ctx );



    /// <summary>
    /// Verbatim source → target copy of the whole buffer. Every skip/no-op
    /// path in a filter MUST route through this (or write all pixels some
    /// other way) — the pipeline swaps buffers after each enabled filter,
    /// so returning without writing hands stale scratch data downstream.
    /// </summary>
    protected static void CopyThrough( FilterContext ctx )
    {
      unsafe
      {
        byte* pSrc = (byte*)ctx.SourceBuffer.PinData();
        byte* pDst = (byte*)ctx.TargetBuffer.PinData();
        long  bytes = (long)ctx.SourceBuffer.BytesPerLine * ctx.SourceBuffer.Height;
        System.Buffer.MemoryCopy( pSrc, pDst, bytes, bytes );
        ctx.SourceBuffer.UnpinData();
        ctx.TargetBuffer.UnpinData();
      }
    }



    public virtual ByteBuffer SaveParameters()
    {
      return new ByteBuffer();
    }



    public virtual void LoadParameters( ByteBuffer buf )
    {
    }



    public abstract IDisplayFilter Clone();
  }
}
