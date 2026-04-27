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
    // Per-CHARACTER color override snapshot. Sized to (Width × spacingX,
    // Height × spacingY) — one slot per char cell that the snapshotted
    // tile range covers. TileColorOverrides went per-character so undo
    // has to capture the same granularity, otherwise undoing a Ctrl-
    // click recolour would only restore one in spacingX×spacingY chars.
    public GR.Game.Layer<int>     ChangedOverrides = new GR.Game.Layer<int>();
    // Captured at construction time so Apply() can recompute char-grid
    // bounds even if the map's spacing changes between create and undo.
    private int                   m_SpacingX = 1;
    private int                   m_SpacingY = 1;



    public UndoMapTilesChange( MapEditor Editor, MapProject.Map Map, int X, int Y, int Width, int Height )
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
      ChangedOverrides.Resize( Width * m_SpacingX, Height * m_SpacingY );

      // Tile snapshot — one value per tile cell.
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          ChangedData[i, j] = Map.Tiles[X + i, Y + j];
        }
      }

      // Per-char override snapshot — covers the char-rectangle
      // corresponding to the snapshotted tile range. Defensive about
      // out-of-range reads (older projects briefly have the layer at
      // 0×0 during load) — anything outside the layer reads as -1.
      int charBaseX = X * m_SpacingX;
      int charBaseY = Y * m_SpacingY;
      int charW = Width * m_SpacingX;
      int charH = Height * m_SpacingY;
      for ( int j = 0; j < charH; ++j )
      {
        for ( int i = 0; i < charW; ++i )
        {
          int srcX = charBaseX + i;
          int srcY = charBaseY + j;
          if ( ( srcX < Map.TileColorOverrides.Width )
          &&   ( srcY < Map.TileColorOverrides.Height ) )
          {
            ChangedOverrides[i, j] = Map.TileColorOverrides[srcX, srcY];
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
      // Restore tiles (tile-grid).
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          AffectedMap.Tiles[X + i, Y + j] = ChangedData[i, j];
        }
      }
      // Restore per-char overrides (char-grid). Bounds check guards
      // against the layer being out-of-shape (e.g. spacing changed
      // between snapshot and apply — defensive, normal flow keeps it
      // in sync).
      int charBaseX = X * m_SpacingX;
      int charBaseY = Y * m_SpacingY;
      int charW = Width * m_SpacingX;
      int charH = Height * m_SpacingY;
      for ( int j = 0; j < charH; ++j )
      {
        for ( int i = 0; i < charW; ++i )
        {
          int dstX = charBaseX + i;
          int dstY = charBaseY + j;
          if ( ( dstX < AffectedMap.TileColorOverrides.Width )
          &&   ( dstY < AffectedMap.TileColorOverrides.Height ) )
          {
            AffectedMap.TileColorOverrides[dstX, dstY] = ChangedOverrides[i, j];
          }
        }
      }
      MapEditor.UpdateArea( X, Y, Width, Height );
    }
  }
}
