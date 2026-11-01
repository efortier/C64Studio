using RetroDevStudio.Documents;
using RetroDevStudio.Formats;
using System.Collections.Generic;



namespace RetroDevStudio.Undo
{
  public class UndoMapTileRemove : UndoTask
  {
    private MapEditor               _MapEditor = null;
    private MapProject.Tile         _RemovedTile = null;
    private MapProject              _MapProject = null;
    // The tile library the removal happened in (see UndoMapTileAdd).
    private MapProject.MapCharset   _Charset = null;
    private int                     _TileIndex = -1;

    // Pre-deletion snapshots so undo can put everything back the way it
    // was. RemoveTile now mutates entity-type tile indices and per-cell
    // color overrides as part of the deletion; without these snapshots
    // an undo would leave both in their post-deletion state even after
    // the tile itself is restored.
    //
    // Snapshots are keyed by the Map OBJECT, not by list position: the
    // sweep covers MapEditor.MapsBoundTo( charset ) — the project maps PLUS
    // scratch workspaces bound to the charset — so an index into
    // Project.Maps would misalign, and map adds/removes between capture and
    // undo would shift it anyway.
    // The charset's entity tiles (type ID -> tile), copied: RemoveTile
    // clears the one on the deleted tile and shifts the ones above it.
    private Dictionary<int, int>                     _EntityTiles = new Dictionary<int, int>();
    // Colour overrides are PER-LAYER (RemoveTile wipes them on every layer
    // a removed tile occupied), so each map's snapshot is one grid per
    // layer, index-aligned to map.Layers — the same shape
    // UndoMapSizeChange.LayerSnap uses. Restoring by layer INDEX (not by
    // captured layer reference) stays correct even when a revert replaced
    // the layer objects wholesale in between.
    private Dictionary<MapProject.Map, List<GR.Game.Layer<int>>> _ColorOverrideSnapshots = new Dictionary<MapProject.Map, List<GR.Game.Layer<int>>>();
    // Mirrors _ColorOverrideSnapshots for the per-character "blocked"
    // override layer — whole-map (a real Map field, not per-layer).
    // RemoveTile clears blocked overrides on every map cell that used the
    // deleted tile (so its footprint isn't left pointing at a stale
    // tile-index after the shift), and Apply must restore them.
    private Dictionary<MapProject.Map, GR.Game.Layer<bool>> _BlockedOverrideSnapshots = new Dictionary<MapProject.Map, GR.Game.Layer<bool>>();
    // The cells that hold the tile, per map and layer: RemoveTile erases them
    // (tile 0 on the background, transparent above) and AddTile's index shift
    // cannot bring them back, so Apply writes the tile into them again.
    private Dictionary<MapProject.Map, List<PlacedCell>> _PlacedCells = new Dictionary<MapProject.Map, List<PlacedCell>>();



    private class PlacedCell
    {
      public int  Layer = 0;
      public int  X = 0;
      public int  Y = 0;
    }



    public UndoMapTileRemove( MapEditor Editor, MapProject Project, MapProject.MapCharset Charset, int TileIndex )
    {
      _MapEditor  = Editor;
      _TileIndex  = TileIndex;
      _MapProject = Project;
      _Charset    = Charset;
      _RemovedTile = Charset.Tiles[TileIndex];

      // Snapshot the entity types' tiles in THIS charset (the only ones a
      // removal touches - every charset keeps its own).
      _EntityTiles = new Dictionary<int, int>( Charset.EntityTiles );

      // Snapshot the color-override layers of every map bound to the charset
      // — the same set (project maps + scratch workspaces) RemoveTile's
      // sweep mutates, and EVERY layer of each map, because the wipe is
      // per-layer.
      // Char-grid sized (Tiles × spacing); captured deeply (per-char copy)
      // rather than holding a reference because the original layer keeps
      // mutating during the delete. Loop bounds use the layer's own
      // Width/Height so this stays correct regardless of grid dimensions.
      foreach ( var map in Editor.MapsBoundTo( Charset ) )
      {
        var layerSnaps = new List<GR.Game.Layer<int>>();
        foreach ( var lay in map.Layers )
        {
          var snap = new GR.Game.Layer<int>();
          snap.Resize( lay.TileColorOverrides.Width, lay.TileColorOverrides.Height );
          for ( int j = 0; j < lay.TileColorOverrides.Height; ++j )
          {
            for ( int i = 0; i < lay.TileColorOverrides.Width; ++i )
            {
              snap[i, j] = lay.TileColorOverrides[i, j];
            }
          }
          layerSnaps.Add( snap );
        }
        _ColorOverrideSnapshots[map] = layerSnaps;

        // Blocked-override layer too — whole-map, same lifecycle.
        // RemoveTile wipes these for the deleted tile's footprint; Apply
        // restores them in lock step with the color layers.
        var blkSnap = new GR.Game.Layer<bool>();
        blkSnap.Resize( map.CharBlockedOverrides.Width, map.CharBlockedOverrides.Height );
        for ( int j = 0; j < map.CharBlockedOverrides.Height; ++j )
        {
          for ( int i = 0; i < map.CharBlockedOverrides.Width; ++i )
          {
            blkSnap[i, j] = map.CharBlockedOverrides[i, j];
          }
        }
        _BlockedOverrideSnapshots[map] = blkSnap;

        var cells = new List<PlacedCell>();
        for ( int k = 0; k < map.Layers.Count; ++k )
        {
          var tiles = map.Layers[k].Tiles;
          for ( int j = 0; j < tiles.Height; ++j )
          {
            for ( int i = 0; i < tiles.Width; ++i )
            {
              if ( tiles[i, j] == TileIndex )
              {
                cells.Add( new PlacedCell() { Layer = k, X = i, Y = j } );
              }
            }
          }
        }
        _PlacedCells[map] = cells;
      }
    }




    public override string Description
    {
      get
      {
        return "Remove Map Tile";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapTileAdd( _MapEditor, _MapProject, _Charset, _TileIndex );
    }



    public override void Apply()
    {
      _MapEditor.AddTile( _Charset, _TileIndex, _RemovedTile );

      // The tile goes back into its cells only now: AddTile's insert shift
      // bumps every cell >= the index, the erased background cells (tile 0)
      // too when the tile was tile 0.
      foreach ( var pair in _PlacedCells )
      {
        var map = pair.Key;
        foreach ( var cell in pair.Value )
        {
          if ( cell.Layer < map.Layers.Count )
          {
            var tiles = map.Layers[cell.Layer].Tiles;
            if ( ( cell.X < tiles.Width )
            &&   ( cell.Y < tiles.Height ) )
            {
              tiles[cell.X, cell.Y] = _TileIndex;
            }
          }
        }
      }

      // Restore the charset's entity tiles exactly - including the one that
      // sat on the deleted tile. Done after AddTile, whose insert shift has
      // already moved the others back; this puts the cleared one back too.
      _Charset.EntityTiles.Clear();
      foreach ( var pair in _EntityTiles )
      {
        _Charset.EntityTiles[pair.Key] = pair.Value;
      }

      // Restore per-map, per-LAYER color overrides. We deep-copy back into
      // the existing layers rather than swapping references so anyone
      // holding a reference keeps seeing the right data. Each snapshot
      // restores into the exact Map object it was captured from — a map
      // (or scratch) that has since been deleted still gets its data back
      // through the held reference, ready for the delete's own undo. The
      // count guard mirrors UndoMapSizeChange: layer counts are fixed, but
      // guard defensively anyway.
      foreach ( var pair in _ColorOverrideSnapshots )
      {
        var map = pair.Key;
        var layerSnaps = pair.Value;
        for ( int k = 0; ( k < layerSnaps.Count ) && ( k < map.Layers.Count ); ++k )
        {
          var snap = layerSnaps[k];
          var lay  = map.Layers[k];
          if ( ( lay.TileColorOverrides.Width != snap.Width )
          ||   ( lay.TileColorOverrides.Height != snap.Height ) )
          {
            lay.TileColorOverrides.Resize( snap.Width, snap.Height );
          }
          for ( int j = 0; j < snap.Height; ++j )
          {
            for ( int i = 0; i < snap.Width; ++i )
            {
              lay.TileColorOverrides[i, j] = snap[i, j];
            }
          }
        }
      }

      // Restore per-map blocked overrides — same deep-copy approach.
      foreach ( var pair in _BlockedOverrideSnapshots )
      {
        var map = pair.Key;
        var blkSnap = pair.Value;
        if ( ( map.CharBlockedOverrides.Width != blkSnap.Width )
        ||   ( map.CharBlockedOverrides.Height != blkSnap.Height ) )
        {
          map.CharBlockedOverrides.Resize( blkSnap.Width, blkSnap.Height );
        }
        for ( int j = 0; j < blkSnap.Height; ++j )
        {
          for ( int i = 0; i < blkSnap.Width; ++i )
          {
            map.CharBlockedOverrides[i, j] = blkSnap[i, j];
          }
        }
      }

      // AddTile redrew the map before its cells and colours were back.
      _MapEditor.TileModified( _Charset, _TileIndex );
    }
  }
}
