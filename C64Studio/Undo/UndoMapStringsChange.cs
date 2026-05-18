using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshots the full per-project <see cref="MapProject.MapStrings"/> list.
  /// Apply() replaces the project's current list with the snapshot. Use this
  /// for any add / delete / reorder / duplicate / live-field edit on a map
  /// string, mirroring the marker / entity undo pattern. The list lives on
  /// <see cref="MapProject"/> (not on a single Map), so the snapshot tracks
  /// the project, not a specific map.
  /// </summary>
  public class UndoMapStringsChange : UndoTask
  {
    public MapProject              AffectedProject = null;
    public MapEditor               MapEditor       = null;

    public List<MapProject.MapString> Snapshot = new List<MapProject.MapString>();



    public UndoMapStringsChange( MapEditor Editor, MapProject Project )
    {
      MapEditor       = Editor;
      AffectedProject = Project;

      // Deep copy every map string so later edits don't leak into the snapshot.
      foreach ( var ms in Project.MapStrings )
      {
        Snapshot.Add( CloneMapString( ms ) );
      }
    }



    public override string Description
    {
      get { return "Map Strings Change"; }
    }



    public override UndoTask CreateComplementaryTask()
    {
      // Captures the state-before-Apply so redo works symmetrically.
      return new UndoMapStringsChange( MapEditor, AffectedProject );
    }



    public override void Apply()
    {
      AffectedProject.MapStrings.Clear();
      foreach ( var ms in Snapshot )
      {
        AffectedProject.MapStrings.Add( CloneMapString( ms ) );
      }
      MapEditor.RefreshMapStrings();
    }



    private static MapProject.MapString CloneMapString( MapProject.MapString Source )
    {
      var copy = new MapProject.MapString
      {
        Label              = Source.Label,
        ClearTextAreaAtEnd = Source.ClearTextAreaAtEnd,
        StringID           = Source.StringID
      };
      for ( int i = 0; i < 4; ++i )
      {
        var src = Source.Lines[i] ?? new MapProject.MapStringLine();
        copy.Lines[i] = new MapProject.MapStringLine
        {
          Text          = src.Text,
          Terminator    = src.Terminator,
          ControlCode   = src.ControlCode,
          Justification = src.Justification
        };
      }
      return copy;
    }
  }
}
