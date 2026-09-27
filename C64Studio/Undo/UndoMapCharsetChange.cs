using System.Collections.Generic;
using RetroDevStudio.Formats;
using RetroDevStudio.Documents;



namespace RetroDevStudio.Undo
{
  public class UndoMapCharsetChange : UndoTask
  {
    // The charset slot the import changed (its CharsetProject is written
    // back in place, so the editor's bindings stay valid).
    public MapProject.MapCharset  Charset = null;
    public MapEditor              Editor = null;


    public List<CharData>         CharsetData = null;


    public UndoMapCharsetChange( MapProject.MapCharset Charset, MapEditor Editor )
    {
      this.Charset = Charset;
      this.Editor = Editor;

      CharsetData = new List<CharData>();
      for ( int i = 0; i < Charset.Charset.ExportNumCharacters; ++i )
      {
        var Char = new CharData();
        Char.Tile.Data        = new GR.Memory.ByteBuffer( Charset.Charset.Characters[i].Tile.Data );
        Char.Tile.CustomColor = Charset.Charset.Characters[i].Tile.CustomColor;
        Char.Category         = Charset.Charset.Characters[i].Category;
        Char.Index            = i;

        CharsetData.Add( Char );
      }
    }




    public override string Description
    {
      get
      {
        return "Map charset change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapCharsetChange( Charset, Editor );
    }



    public override void Apply()
    {
      foreach ( var singleChar in CharsetData )
      {
        singleChar.Tile.Data.CopyTo( Charset.Charset.Characters[singleChar.Index].Tile.Data );
        Charset.Charset.Characters[singleChar.Index].Tile.CustomColor = singleChar.Tile.CustomColor;
        Charset.Charset.Characters[singleChar.Index].Category = singleChar.Category;
      }
      Editor.CharsetChanged( Charset );
      Editor.SetModified();
    }
  }
}
