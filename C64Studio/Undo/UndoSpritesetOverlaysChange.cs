using System.Collections.Generic;
using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Snapshot-based undo for the entire Overlays list (overlays + slots
  /// + frames). Single class covers add/remove/duplicate, rename, slot
  /// enabled/X/Y/expand/colors, and frame add/remove/duplicate/delay/
  /// bank-index changes. Per-keystroke granularity matches the existing
  /// marker-editor pattern — coalescing can be layered on later if the
  /// undo stream gets noisy.
  /// </summary>
  public class UndoSpritesetOverlaysChange : UndoTask
  {
    private SpriteEditor                Editor;
    private SpriteProject               Project;
    private List<SpriteProject.Overlay> Snapshot;



    public UndoSpritesetOverlaysChange( SpriteEditor Editor, SpriteProject Project )
    {
      this.Editor   = Editor;
      this.Project  = Project;
      this.Snapshot = CloneOverlays( Project.Overlays );
    }



    public override string Description
    {
      get { return "Spriteset Overlays Change"; }
    }



    public override UndoTask CreateComplementaryTask()
    {
      // Take a fresh snapshot of the *current* state so a redo lands
      // back where we were before Apply rewrote Overlays.
      return new UndoSpritesetOverlaysChange( Editor, Project );
    }



    public override void Apply()
    {
      Project.Overlays.Clear();
      foreach ( var ov in Snapshot )
      {
        Project.Overlays.Add( CloneOverlay( ov ) );
      }
      Editor.RefreshOverlaysList();
      Editor.SetModified();
    }



    private static List<SpriteProject.Overlay> CloneOverlays( List<SpriteProject.Overlay> Source )
    {
      var copy = new List<SpriteProject.Overlay>( Source.Count );
      foreach ( var ov in Source )
      {
        copy.Add( CloneOverlay( ov ) );
      }
      return copy;
    }



    private static SpriteProject.Overlay CloneOverlay( SpriteProject.Overlay Source )
    {
      return Source.Clone();
    }
  }
}
