using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  public class UndoMapSizeChange : UndoTask
  {
    public MapProject.Map         AffectedMap = null;
    public MapEditor              MapEditor = null;



    public int        Width = 0;
    public int        Height = 0;

    // Per-layer snapshot: a resize changes every layer's tile grid and per-char
    // colour grid (and may wipe them on a spacing change), so all layers must be
    // captured and restored. Blocked (passability) is whole-map and captured
    // once below. Grids are captured at their literal current dimensions so
    // Apply() can resize back even when the resize changed them.
    private class LayerSnap
    {
      public GR.Game.Layer<int> Tiles = new GR.Game.Layer<int>();
      public GR.Game.Layer<int> Color = new GR.Game.Layer<int>();
    }
    private List<LayerSnap>       m_LayerSnaps   = new List<LayerSnap>();
    public GR.Game.Layer<bool>    ChangedBlocked = new GR.Game.Layer<bool>();



    public UndoMapSizeChange( MapEditor Editor, MapProject.Map Map, int Width, int Height )
    {
      this.Width = Width;
      this.Height = Height;
      MapEditor = Editor;
      AffectedMap = Map;

      foreach ( var lay in Map.Layers )
      {
        var snap = new LayerSnap();
        snap.Tiles.Resize( lay.Tiles.Width, lay.Tiles.Height );
        for ( int i = 0; i < lay.Tiles.Width; ++i )
        {
          for ( int j = 0; j < lay.Tiles.Height; ++j )
          {
            snap.Tiles[i, j] = lay.Tiles[i, j];
          }
        }
        snap.Color.Resize( lay.TileColorOverrides.Width, lay.TileColorOverrides.Height );
        for ( int i = 0; i < lay.TileColorOverrides.Width; ++i )
        {
          for ( int j = 0; j < lay.TileColorOverrides.Height; ++j )
          {
            snap.Color[i, j] = lay.TileColorOverrides[i, j];
          }
        }
        m_LayerSnaps.Add( snap );
      }

      // Whole-map blocked (passability) override layer.
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
      // Restore every layer's tile grid + per-char colour grid to the
      // snapshotted size and content. Resize count never changes during a map
      // resize, so index alignment holds; guard defensively anyway.
      for ( int k = 0; ( k < m_LayerSnaps.Count ) && ( k < AffectedMap.Layers.Count ); ++k )
      {
        var snap = m_LayerSnaps[k];
        var lay  = AffectedMap.Layers[k];

        lay.Tiles.Resize( snap.Tiles.Width, snap.Tiles.Height );
        for ( int i = 0; i < snap.Tiles.Width; ++i )
        {
          for ( int j = 0; j < snap.Tiles.Height; ++j )
          {
            lay.Tiles[i, j] = snap.Tiles[i, j];
          }
        }
        lay.TileColorOverrides.Resize( snap.Color.Width, snap.Color.Height );
        for ( int i = 0; i < snap.Color.Width; ++i )
        {
          for ( int j = 0; j < snap.Color.Height; ++j )
          {
            lay.TileColorOverrides[i, j] = snap.Color[i, j];
          }
        }
      }

      // Whole-map blocked layer.
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
