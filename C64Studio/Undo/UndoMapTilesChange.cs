using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  public class UndoMapTilesChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;



    public int        X = 0;
    public int        Y = 0;
    public int        Width = 0;
    public int        Height = 0;

    public GR.Game.Layer<int>     ChangedData = new GR.Game.Layer<int>();
    // Per-cell color override snapshot. Captured alongside the tile
    // index so undo of a placement restores both the tile AND the color
    // it had before — without this, placing a colored tile on top of
    // an uncolored tile and undoing would leave the override behind.
    public GR.Game.Layer<int>     ChangedOverrides = new GR.Game.Layer<int>();



    public UndoMapTilesChange( MapEditor Editor, MapProject.Map Map, int X, int Y, int Width, int Height )
    {
      this.X = X;
      this.Y = Y;
      this.Width = Width;
      this.Height = Height;
      MapEditor = Editor;
      AffectedMap = Map;
      ChangedData.Resize( Width, Height );
      ChangedOverrides.Resize( Width, Height );

      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          ChangedData[i, j] = Map.Tiles[X + i, Y + j];
          // Be defensive about override layer dimensions — older project
          // files that didn't have the layer may have it sized to 0,0
          // briefly during load, and we don't want a snapshot to throw
          // here. Cells outside the layer get -1 (no override) which is
          // the correct semantic default.
          if ( ( X + i < Map.TileColorOverrides.Width )
          &&   ( Y + j < Map.TileColorOverrides.Height ) )
          {
            ChangedOverrides[i, j] = Map.TileColorOverrides[X + i, Y + j];
          }
          else
          {
            ChangedOverrides[i, j] = -1;
          }
        }
      }
    }




    public override string Description
    {
      get
      {
        return "Map Change";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapTilesChange( MapEditor, AffectedMap, X, Y, Width, Height );
    }



    public override void Apply()
    {
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          AffectedMap.Tiles[X + i, Y + j] = ChangedData[i, j];
          if ( ( X + i < AffectedMap.TileColorOverrides.Width )
          &&   ( Y + j < AffectedMap.TileColorOverrides.Height ) )
          {
            AffectedMap.TileColorOverrides[X + i, Y + j] = ChangedOverrides[i, j];
          }
        }
      }
      MapEditor.UpdateArea( X, Y, Width, Height );
    }
  }
}
