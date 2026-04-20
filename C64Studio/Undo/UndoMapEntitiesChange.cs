using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshots the full entity list of a single map. Apply() replaces the map's
  /// current list with the snapshot — use this for any operation that shifts,
  /// reorders, or bulk-edits entities (e.g. ShiftMap, type deletion cascade,
  /// map resize drop).
  /// </summary>
  public class UndoMapEntitiesChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;

    public List<MapProject.Entity> Snapshot = new List<MapProject.Entity>();



    public UndoMapEntitiesChange( MapEditor Editor, MapProject.Map Map )
    {
      MapEditor = Editor;
      AffectedMap = Map;

      // Deep copy every entity so later mutations on the map don't leak into the snapshot.
      foreach ( var e in Map.Entities )
      {
        Snapshot.Add( new MapProject.Entity
        {
          X = e.X,
          Y = e.Y,
          Type = e.Type,
          Value1 = e.Value1,
          Value2 = e.Value2,
          Enabled = e.Enabled
        } );
      }
    }



    public override string Description
    {
      get { return "Map Entities Change"; }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapEntitiesChange( MapEditor, AffectedMap );
    }



    public override void Apply()
    {
      AffectedMap.Entities.Clear();
      foreach ( var e in Snapshot )
      {
        AffectedMap.Entities.Add( new MapProject.Entity
        {
          X = e.X,
          Y = e.Y,
          Type = e.Type,
          Value1 = e.Value1,
          Value2 = e.Value2,
          Enabled = e.Enabled
        } );
      }
      MapEditor.UpdateArea( 0, 0, AffectedMap.Tiles.Width, AffectedMap.Tiles.Height );
    }
  }
}
