using GR.Image;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// State passed through a <see cref="FilterPipeline"/> run. The pipeline
  /// mutates <see cref="SourceBuffer"/> / <see cref="TargetBuffer"/> between
  /// passes; every other field describes the scene and is immutable for the
  /// duration of a single Apply call.
  ///
  /// Net3.5 has no record types, so this is a plain class with public fields
  /// (matching existing GR.* conventions in this codebase).
  /// </summary>
  public class FilterContext
  {
    /// <summary>
    /// What the filter reads this pass. The pipeline sets this to the
    /// previous pass's output (or the original TargetBuffer on pass 0).
    /// Filters must not write to this buffer.
    /// </summary>
    public FastImage  SourceBuffer;

    /// <summary>
    /// Where the filter writes this pass. The pipeline guarantees this
    /// is distinct from SourceBuffer and is the same size. On the final
    /// pass the pipeline ensures this is the caller's TargetBuffer so
    /// the filtered result lands where expected.
    /// </summary>
    public FastImage  TargetBuffer;

    /// <summary>
    /// Top-left of the map region inside the TargetBuffer, in target
    /// pixels. Useful for filters that want to restrict their effect
    /// to the map area and leave the editor chrome alone.
    /// </summary>
    public int        RenderOffsetX;
    public int        RenderOffsetY;

    /// <summary>
    /// Size of the map region inside the TargetBuffer, in target pixels.
    /// Combined with RenderOffset this is the rectangle to confine to.
    /// </summary>
    public int        MapPixelWidth;
    public int        MapPixelHeight;

    /// <summary>
    /// Size of the unscaled source render (DisplayPage size, e.g. 320x200
    /// for a single C64 screen). Lets a filter derive how many *source*
    /// pixels fit in one *target* row so scanlines align to the scaled
    /// map rather than to raw TargetBuffer rows.
    /// </summary>
    public int        SourceWidth;
    public int        SourceHeight;

    /// <summary>
    /// Identity of the host view (the map editor document) this pass
    /// renders for. The pipeline and its filter instances are GLOBAL —
    /// shared by every open editor — so any per-view temporal state
    /// (phosphor history) must be keyed by this instead of living directly
    /// on the filter instance. Null is allowed; filters fall back to a
    /// private key (single-view behavior).
    /// </summary>
    public object     StateKey;

    /// <summary>
    /// OUTPUT: set true by temporal filters (phosphor persistence) during
    /// Apply when their result for THIS view is still evolving and the
    /// host should keep repainting. The pipeline clears it at the start of
    /// each run; the host reads it after Apply. Lives on the context, not
    /// the filter, because the same filter instance serves several views
    /// whose trails settle independently.
    /// </summary>
    public bool       NeedsContinuousRepaint;
  }
}
