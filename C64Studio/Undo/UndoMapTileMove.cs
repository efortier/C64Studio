using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  public class UndoMapTileMove : UndoTask
  {
    public MapEditor              _MapEditor = null;
    public MapProject             _MapProject = null;
    public int                    _TileIndexFrom = -1;
    public int                    _TileIndexTo = -1;



    public UndoMapTileMove( MapEditor Editor, MapProject Project, int TileIndexFrom, int TileIndexTo )
    {
      _MapEditor    = Editor;
      _MapProject   = Project;
      _TileIndexFrom = TileIndexFrom;
      _TileIndexTo   = TileIndexTo;
    }




    public override string Description
    {
      get
      {
        return "Move Map Tile";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapTileMove( _MapEditor, _MapProject, _TileIndexTo, _TileIndexFrom );
    }



    public override void Apply()
    {
      _MapEditor.MoveTile( _TileIndexFrom, _TileIndexTo );
    }
  }
}
