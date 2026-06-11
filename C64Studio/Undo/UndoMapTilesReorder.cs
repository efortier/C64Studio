using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  // Undo for a multi-tile reorder on the Tiles tab. A reorder is an arbitrary
  // permutation of the tile list (plus the matching remap of every map cell and
  // entity-type binding), so it can't be expressed as a single from/to move.
  // We store the permutation this task applies; undo and redo are just the
  // permutation and its inverse, which makes the round-trip exact regardless of
  // how many tiles moved.
  public class UndoMapTilesReorder : UndoTask
  {
    private MapEditor    _MapEditor = null;
    private MapProject   _MapProject = null;
    private int[]        _OldToNew = null;



    public UndoMapTilesReorder( MapEditor Editor, MapProject Project, int[] OldToNew )
    {
      _MapEditor  = Editor;
      _MapProject = Project;
      _OldToNew   = OldToNew;
    }



    public override string Description
    {
      get
      {
        return "Reorder Map Tiles";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      int[] inverse = new int[_OldToNew.Length];
      for ( int i = 0; i < _OldToNew.Length; ++i )
      {
        inverse[_OldToNew[i]] = i;
      }
      return new UndoMapTilesReorder( _MapEditor, _MapProject, inverse );
    }



    public override void Apply()
    {
      _MapEditor.ReorderTiles( _OldToNew );
    }
  }
}
