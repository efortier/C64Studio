using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Undo entry for solo edits of <see cref="MapProject.Map.CharBlockedOverrides"/>
  /// (the per-character "blocked" override layer). Snapshots a CHAR-grid
  /// rectangle at construction; <see cref="Apply"/> restores it.
  ///
  /// Mirrors <see cref="UndoMapTilesChange"/> structurally but is purely
  /// for the blocked-override layer — no tile-grid snapshot, since the
  /// PASSABLE tool doesn't change tile placement. Tile-placement code
  /// continues to use <see cref="UndoMapTilesChange"/>, which already
  /// snapshots BOTH override layers alongside the tile grid; that path
  /// covers Ctrl+Z restoring both override layers when a tile is placed.
  /// This class exists so PASSABLE-mode strokes don't pay for a tile
  /// snapshot they never modify.
  ///
  /// X / Y / Width / Height are in CHAR coordinates (the layer's natural
  /// unit). The PASSABLE tool currently passes the full layer (0,0,W,H)
  /// for simplicity and one-Ctrl-Z-per-stroke; tighter bounding boxes
  /// would be a future optimization if needed.
  /// </summary>
  public class UndoMapCharBlockedChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;

    public int        X = 0;
    public int        Y = 0;
    public int        Width = 0;
    public int        Height = 0;

    public GR.Game.Layer<bool>    ChangedData = new GR.Game.Layer<bool>();
    private int                   m_SpacingX = 1;
    private int                   m_SpacingY = 1;



    public UndoMapCharBlockedChange( MapEditor Editor, MapProject.Map Map, int X, int Y, int Width, int Height )
    {
      this.X = X;
      this.Y = Y;
      this.Width = Width;
      this.Height = Height;
      MapEditor = Editor;
      AffectedMap = Map;
      m_SpacingX = Map.TileSpacingX;
      m_SpacingY = Map.TileSpacingY;

      ChangedData.Resize( Width, Height );
      // Snapshot the rectangle in char coords. Defensive about out-of-
      // range reads (the layer may briefly be 0×0 mid-load) — anything
      // outside reads as false (the no-override sentinel).
      for ( int j = 0; j < Height; ++j )
      {
        for ( int i = 0; i < Width; ++i )
        {
          int srcX = X + i;
          int srcY = Y + j;
          if ( ( srcX < Map.CharBlockedOverrides.Width )
          &&   ( srcY < Map.CharBlockedOverrides.Height ) )
          {
            ChangedData[i, j] = Map.CharBlockedOverrides[srcX, srcY];
          }
          else
          {
            ChangedData[i, j] = false;
          }
        }
      }
    }




    public override string Description
    {
      get
      {
        return "Map Blocked Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapCharBlockedChange( MapEditor, AffectedMap, X, Y, Width, Height );
    }



    public override void Apply()
    {
      // Restore each char in the snapshotted rectangle. Bounds-clamp on
      // the destination layer in case spacing changed between snapshot
      // and apply (defensive — normal flow keeps it in sync).
      for ( int j = 0; j < Height; ++j )
      {
        for ( int i = 0; i < Width; ++i )
        {
          int dstX = X + i;
          int dstY = Y + j;
          if ( ( dstX < AffectedMap.CharBlockedOverrides.Width )
          &&   ( dstY < AffectedMap.CharBlockedOverrides.Height ) )
          {
            AffectedMap.CharBlockedOverrides[dstX, dstY] = ChangedData[i, j];
          }
        }
      }
      // Convert the char-rectangle back into a tile-rectangle for
      // UpdateArea (which expects tile coords). Round up so partial
      // tile coverage at the edges still gets a repaint.
      int tileX = X / m_SpacingX;
      int tileY = Y / m_SpacingY;
      int tileW = ( Width  + m_SpacingX - 1 ) / m_SpacingX;
      int tileH = ( Height + m_SpacingY - 1 ) / m_SpacingY;
      MapEditor.UpdateArea( tileX, tileY, tileW, tileH );
    }
  }
}
