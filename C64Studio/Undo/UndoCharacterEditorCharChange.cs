using RetroDevStudio.Controls;
using RetroDevStudio.Formats;
using System.Collections.Generic;

namespace RetroDevStudio.Undo
{
  public class UndoCharacterEditorCharChange : UndoTask
  {
    public CharacterEditor        Editor = null;
    public CharsetProject         Project = null;
    public int                    CharIndex = 0;
    public int                    CharCount = 0;
    public List<CharData>         Chars = new List<CharData>();



    public UndoCharacterEditorCharChange( CharacterEditor Editor, CharsetProject Project, int CharIndex, int Count )
    {
      this.Editor = Editor;
      this.Project = Project;
      this.CharIndex = CharIndex;
      this.CharCount = Count;

      for ( int i = 0; i < Count; ++i )
      {
        var charData = new CharData();
        charData.Tile.Data        = new GR.Memory.ByteBuffer( Project.Characters[CharIndex + i].Tile.Data );
        charData.Tile.CustomColor = Project.Characters[CharIndex + i].Tile.CustomColor;
        // Mode must be snapshotted too: ImportChar() flips a character's
        // Mode (e.g. HIRES <-> MULTICOLOR) based on the imported pixels. Without
        // capturing it, undo restores the pixel bytes but leaves the wrong mode,
        // so the bitmap is decoded incorrectly = scrambled graphics on undo.
        charData.Tile.Mode        = Project.Characters[CharIndex + i].Tile.Mode;
        // Width/Height travel with Mode: a project mode change (e.g. 8x8 <-> 8x16)
        // updates Mode, Width, Height and the Data size together. Restoring Mode
        // alone would leave Width/Height mismatched, so GetPixel/SetPixel would
        // compute byte offsets past the end of the restored (old-size) buffer.
        charData.Tile.Width       = Project.Characters[CharIndex + i].Tile.Width;
        charData.Tile.Height      = Project.Characters[CharIndex + i].Tile.Height;
        charData.Category         = Project.Characters[CharIndex + i].Category;
        charData.Index            = CharIndex + i;

        Chars.Add( charData );
      }
    }



    public override string Description
    {
      get
      {
        return "CharacterEditor Char Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoCharacterEditorCharChange( Editor, Project, CharIndex, CharCount );
    }



    public override void Apply()
    {
      for ( int i = 0; i < CharCount; ++i )
      {
        var charData = Chars[i];

        Project.Characters[CharIndex + i].Tile.Data = new GR.Memory.ByteBuffer( charData.Tile.Data );
        Project.Characters[CharIndex + i].Tile.CustomColor = charData.Tile.CustomColor;
        Project.Characters[CharIndex + i].Tile.Mode = charData.Tile.Mode;
        Project.Characters[CharIndex + i].Tile.Width = charData.Tile.Width;
        Project.Characters[CharIndex + i].Tile.Height = charData.Tile.Height;
        Project.Characters[CharIndex + i].Category = charData.Category;
        Project.Characters[CharIndex + i].Index = charData.Index;
      }
      Editor.CharacterChanged( CharIndex, CharCount );
    }
  }
}
