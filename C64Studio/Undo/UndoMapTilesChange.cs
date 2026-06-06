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
    // Per-CHARACTER "blocked" override snapshot. Same shape as
    // ChangedOverrides. ApplyPlacementColorOverride clears blocked-
    // overrides for the placed tile's footprint, so Ctrl+Z must restore
    // them alongside the tile + color. Default false IS the no-override
    // sentinel — Resize zero-fills, no separate reset needed.
    public GR.Game.Layer<bool>    ChangedBlocked   = new GR.Game.Layer<bool>();
    // Captured at construction time so Apply() can recompute char-grid
    // bounds even if the map's spacing changes between create and undo.
    private int                   m_SpacingX = 1;
    private int                   m_SpacingY = 1;
    // Which editor tile layer this snapshot belongs to (0 = Background). Tiles
    // and colour overrides are per-layer; blocked overrides are whole-map and
    // always read/written on AffectedMap directly.
    public int                    LayerIndex = 0;

    private MapProject.MapLayer ResolveLayer()
    {
      if ( ( LayerIndex >= 0 ) && ( LayerIndex < AffectedMap.Layers.Count ) )
      {
        return AffectedMap.Layers[LayerIndex];
      }
      return AffectedMap.Layers[0];
    }



    public UndoMapTilesChange( MapEditor Editor, MapProject.Map Map, int X, int Y, int Width, int Height, int LayerIndex = 0 )
    {
      this.X = X;
      this.Y = Y;
      this.Width = Width;
      this.Height = Height;
      MapEditor = Editor;
      AffectedMap = Map;
      this.LayerIndex = LayerIndex;
      m_SpacingX = Map.TileSpacingX;
      m_SpacingY = Map.TileSpacingY;
      var layer = ResolveLayer();

      ChangedData.Resize( Width, Height );
      ChangedOverrides.Resize( Width * m_SpacingX, Height * m_SpacingY );
      ChangedBlocked.Resize( Width * m_SpacingX, Height * m_SpacingY );

      // Tile snapshot — one value per tile cell.
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          ChangedData[i, j] = layer.Tiles[X + i, Y + j];
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
          if ( ( srcX < layer.TileColorOverrides.Width )
          &&   ( srcY < layer.TileColorOverrides.Height ) )
          {
            ChangedOverrides[i, j] = layer.TileColorOverrides[srcX, srcY];
          }
          else
          {
            ChangedOverrides[i, j] = -1;
          }
          // Per-char blocked-override snapshot. Same defensive bounds
          // check; out-of-range reads as false (no override).
          if ( ( srcX < Map.CharBlockedOverrides.Width )
          &&   ( srcY < Map.CharBlockedOverrides.Height ) )
          {
            ChangedBlocked[i, j] = Map.CharBlockedOverrides[srcX, srcY];
          }
          else
          {
            ChangedBlocked[i, j] = false;
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
      return new UndoMapTilesChange( MapEditor, AffectedMap, X, Y, Width, Height, LayerIndex );
    }



    public override void Apply()
    {
      var layer = ResolveLayer();
      // Restore tiles (tile-grid) on the snapshot's layer.
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          layer.Tiles[X + i, Y + j] = ChangedData[i, j];
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
          if ( ( dstX < layer.TileColorOverrides.Width )
          &&   ( dstY < layer.TileColorOverrides.Height ) )
          {
            layer.TileColorOverrides[dstX, dstY] = ChangedOverrides[i, j];
          }
          // Restore per-char blocked-overrides. Same bounds guard.
          if ( ( dstX < AffectedMap.CharBlockedOverrides.Width )
          &&   ( dstY < AffectedMap.CharBlockedOverrides.Height ) )
          {
            AffectedMap.CharBlockedOverrides[dstX, dstY] = ChangedBlocked[i, j];
          }
        }
      }
      MapEditor.UpdateArea( X, Y, Width, Height );
    }
  }
}
