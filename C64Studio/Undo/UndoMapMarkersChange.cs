using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshots the full marker list of a single map. Apply() replaces the map's
  /// current list with the snapshot — use this for any operation that shifts,
  /// reorders, or bulk-edits markers (e.g. ShiftMap).
  /// </summary>
  public class UndoMapMarkersChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;

    public List<MapProject.Marker> Snapshot = new List<MapProject.Marker>();



    public UndoMapMarkersChange( MapEditor Editor, MapProject.Map Map )
    {
      MapEditor = Editor;
      AffectedMap = Map;

      // Deep copy every marker so later mutations on the map don't leak into the snapshot.
      foreach ( var m in Map.Markers )
      {
        Snapshot.Add( new MapProject.Marker
        {
          X = m.X,
          Y = m.Y,
          Type = m.Type,
          Name = m.Name,
          Value1 = m.Value1,
          Value2 = m.Value2,
          Enabled = m.Enabled,
          Triggered = m.Triggered,
          GroupId = m.GroupId,
          LinkToID = m.LinkToID,
          LinkID = m.LinkID,
          Width = m.Width,
          Height = m.Height
        } );
      }
    }



    public override string Description
    {
      get { return "Map Markers Change"; }
    }



    public override UndoTask CreateComplementaryTask()
    {
      // Captures the state-before-Apply so redo works symmetrically.
      return new UndoMapMarkersChange( MapEditor, AffectedMap );
    }



    public override void Apply()
    {
      AffectedMap.Markers.Clear();
      foreach ( var m in Snapshot )
      {
        AffectedMap.Markers.Add( new MapProject.Marker
        {
          X = m.X,
          Y = m.Y,
          Type = m.Type,
          Name = m.Name,
          Value1 = m.Value1,
          Value2 = m.Value2,
          Enabled = m.Enabled,
          Triggered = m.Triggered,
          GroupId = m.GroupId,
          LinkToID = m.LinkToID,
          LinkID = m.LinkID,
          Width = m.Width,
          Height = m.Height
        } );
      }
      MapEditor.UpdateArea( 0, 0, AffectedMap.Tiles.Width, AffectedMap.Tiles.Height );
    }
  }
}
