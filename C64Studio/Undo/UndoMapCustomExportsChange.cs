using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshots the project's custom text exports (the whole list, field by
  /// field) for the structural steps of the Custom Exports tab: add, delete,
  /// clear, reorder and the Active toggle. Typing in the text fields is NOT
  /// on this stack - those fields keep their own undo. Apply() replaces the
  /// list with the snapshot and refreshes the tab.
  /// </summary>
  public class UndoMapCustomExportsChange : UndoTask
  {
    public MapProject                       AffectedProject = null;
    public MapEditor                        MapEditor = null;
    public List<MapProject.CustomExport>    Snapshot = new List<MapProject.CustomExport>();
    /// <summary>ID of the definition selected when the snapshot was taken; -1 = none.</summary>
    public int                              SelectedID = -1;



    public UndoMapCustomExportsChange( MapEditor Editor, MapProject Project, int SelectedID )
    {
      MapEditor       = Editor;
      AffectedProject = Project;
      this.SelectedID = SelectedID;
      foreach ( var export in Project.CustomExports )
      {
        Snapshot.Add( Clone( export ) );
      }
    }



    public override string Description
    {
      get
      {
        return "Custom Exports Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapCustomExportsChange( MapEditor, AffectedProject, MapEditor.SelectedCustomExportID );
    }



    public override void Apply()
    {
      AffectedProject.CustomExports.Clear();
      foreach ( var export in Snapshot )
      {
        AffectedProject.CustomExports.Add( Clone( export ) );
      }
      MapEditor.RefreshCustomExportList( SelectedID );
    }



    private static MapProject.CustomExport Clone( MapProject.CustomExport Source )
    {
      return new MapProject.CustomExport()
      {
        ID          = Source.ID,
        Name        = Source.Name,
        Active      = Source.Active,
        Directory   = Source.Directory,
        FileName    = Source.FileName,
        Description = Source.Description,
        Content     = Source.Content
      };
    }
  }
}
