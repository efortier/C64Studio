using GR.Memory;



namespace RetroDevStudio.CustomRenderer.DisplayFilters
{
  /// <summary>
  /// A post-processing filter that runs on the already-composited map image
  /// (after tiles, entities, grid and marker overlays have been drawn). Each
  /// filter reads from <see cref="FilterContext.SourceBuffer"/> and writes to
  /// <see cref="FilterContext.TargetBuffer"/>; the pipeline ping-pongs those
  /// two buffers between passes so adding a filter costs one raster, not two.
  ///
  /// Implementations should live in <c>C64Models/CustomRenderer/DisplayFilters/</c>
  /// and register themselves with <see cref="DisplayFilterRegistry"/> via a
  /// static constructor so the settings UI and the settings-loader can
  /// discover them by type name.
  /// </summary>
  public interface IDisplayFilter
  {
    /// <summary>
    /// Human-readable name for the settings UI (e.g. "Scanlines").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this filter participates in the pipeline. Disabled filters
    /// are skipped entirely — the pipeline does not allocate or copy for
    /// them, so toggling this is cheap at runtime.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Apply the filter. The implementation MUST read from
    /// <see cref="FilterContext.SourceBuffer"/> and write to
    /// <see cref="FilterContext.TargetBuffer"/>, never in-place, so the
    /// pipeline's ping-pong invariant holds.
    /// </summary>
    void Apply( FilterContext ctx );

    /// <summary>
    /// Serialize the filter's tunable parameters (not Name, not Enabled —
    /// those are handled by the pipeline wrapper). Backward compat: append
    /// new fields at the end and make the loader tolerate the shorter old
    /// layout via a length check.
    /// </summary>
    ByteBuffer SaveParameters();

    /// <summary>
    /// Deserialize what <see cref="SaveParameters"/> wrote. Called once
    /// right after the filter is constructed from the registry during
    /// settings load. Implementations should clamp values into valid
    /// ranges rather than throwing on out-of-range data.
    /// </summary>
    void LoadParameters( ByteBuffer buf );

    /// <summary>
    /// Deep copy — used when the settings dialog needs a snapshot for its
    /// Revert button. The clone must not share mutable state (parameter
    /// fields, cached tables) with the original.
    /// </summary>
    IDisplayFilter Clone();
  }
}
