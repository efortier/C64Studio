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
