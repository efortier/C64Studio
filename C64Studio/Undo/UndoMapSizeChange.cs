using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  public class UndoMapSizeChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;



    public int        Width = 0;
    public int        Height = 0;

    public GR.Game.Layer<int>     ChangedData = new GR.Game.Layer<int>();
    // Per-CHARACTER override layers, captured at their literal current
    // dimensions. A resize (a tile size OR a tile-spacing change) resizes —
    // and on a spacing change WIPES — both override layers, so they must be
    // snapshotted and restored alongside the tile grid. Without this, undoing
    // a resize silently drops every per-character colour override and blocked
    // (passability) flag the user had set.
    public GR.Game.Layer<int>     ChangedOverrides = new GR.Game.Layer<int>();
    public GR.Game.Layer<bool>    ChangedBlocked   = new GR.Game.Layer<bool>();



    public UndoMapSizeChange( MapEditor Editor, MapProject.Map Map, int Width, int Height )
    {
      this.Width = Width;
      this.Height = Height;
      MapEditor = Editor;
      AffectedMap = Map;

      // Tile grid.
      ChangedData.Resize( Width, Height );
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          ChangedData[i, j] = Map.Tiles[i, j];
        }
      }

      // Per-char colour override layer, captured at its literal current size.
      ChangedOverrides.Resize( Map.TileColorOverrides.Width, Map.TileColorOverrides.Height );
      for ( int i = 0; i < Map.TileColorOverrides.Width; ++i )
      {
        for ( int j = 0; j < Map.TileColorOverrides.Height; ++j )
        {
          ChangedOverrides[i, j] = Map.TileColorOverrides[i, j];
        }
      }

      // Per-char blocked (passability) override layer, same approach.
      ChangedBlocked.Resize( Map.CharBlockedOverrides.Width, Map.CharBlockedOverrides.Height );
      for ( int i = 0; i < Map.CharBlockedOverrides.Width; ++i )
      {
        for ( int j = 0; j < Map.CharBlockedOverrides.Height; ++j )
        {
          ChangedBlocked[i, j] = Map.CharBlockedOverrides[i, j];
        }
      }
    }



    public override string Description
    {
      get
      {
        return "Map Size Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapSizeChange( MapEditor, AffectedMap, AffectedMap.Tiles.Width, AffectedMap.Tiles.Height );
    }



    public override void Apply()
    {
      // Restore the tile grid.
      AffectedMap.Tiles.Resize( Width, Height );
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          AffectedMap.Tiles[i, j] = ChangedData[i, j];
        }
      }

      // Restore both per-char override layers to their snapshotted size and
      // content (a resize may have changed their dimensions, so resize back
      // before refilling).
      AffectedMap.TileColorOverrides.Resize( ChangedOverrides.Width, ChangedOverrides.Height );
      for ( int i = 0; i < ChangedOverrides.Width; ++i )
      {
        for ( int j = 0; j < ChangedOverrides.Height; ++j )
        {
          AffectedMap.TileColorOverrides[i, j] = ChangedOverrides[i, j];
        }
      }
      AffectedMap.CharBlockedOverrides.Resize( ChangedBlocked.Width, ChangedBlocked.Height );
      for ( int i = 0; i < ChangedBlocked.Width; ++i )
      {
        for ( int j = 0; j < ChangedBlocked.Height; ++j )
        {
          AffectedMap.CharBlockedOverrides[i, j] = ChangedBlocked[i, j];
        }
      }

      MapEditor.UpdateArea( 0, 0, Width, Height );
      MapEditor.InvalidateCurrentMap();
    }
  }
}
