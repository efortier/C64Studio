using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshots <see cref="MapProject.StartMapIndex"/>. Apply() restores the
  /// snapshot — used by the "Set start map" button so the user can undo a
  /// reassignment. Project-scoped (not per-map): only one StartMapIndex
  /// exists per project, mirroring how the field is stored and exported.
  /// </summary>
  public class UndoMapStartMapIndexChange : UndoTask
  {
    public MapProject     AffectedProject = null;
    public MapEditor      MapEditor       = null;

    public int            Snapshot        = 0;



    public UndoMapStartMapIndexChange( MapEditor Editor, MapProject Project )
    {
      MapEditor       = Editor;
      AffectedProject = Project;
      Snapshot        = Project.StartMapIndex;
    }



    public override string Description
    {
      get { return "Start Map Change"; }
    }



    public override UndoTask CreateComplementaryTask()
    {
      // Captures the state-before-Apply so redo works symmetrically.
      return new UndoMapStartMapIndexChange( MapEditor, AffectedProject );
    }



    public override void Apply()
    {
      AffectedProject.StartMapIndex = Snapshot;
      MapEditor.RefreshMapListDisplay();
    }
  }
}
