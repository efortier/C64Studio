using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  public class UndoMapTileAdd : UndoTask
  {
    public MapEditor              _MapEditor = null;
    public MapProject             _MapProject = null;
    // The tile library the add happened in — undo must target it even after
    // the editor switched to a map bound to another charset.
    public MapProject.MapCharset  _Charset = null;
    public int                    _TileIndex = -1;



    public UndoMapTileAdd( MapEditor Editor, MapProject Project, MapProject.MapCharset Charset, int TileIndex )
    {
      _MapEditor  = Editor;
      _MapProject = Project;
      _Charset    = Charset;
      _TileIndex  = TileIndex;
    }




    public override string Description
    {
      get
      {
        return "Add Map Tile";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapTileRemove( _MapEditor, _MapProject, _Charset, _TileIndex );
    }



    public override void Apply()
    {
      _MapEditor.RemoveTile( _Charset, _TileIndex );
    }
  }
}
