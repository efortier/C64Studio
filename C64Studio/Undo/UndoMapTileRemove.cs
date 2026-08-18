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
    private int                     _TileIndex = -1;
    public List<Undo.UndoTask>      _InternalUndos = new List<UndoTask>();

    // Pre-deletion snapshots so undo can put everything back the way it
    // was. RemoveTile now mutates entity-type tile indices and per-cell
    // color overrides as part of the deletion; without these snapshots
    // an undo would leave both in their post-deletion state even after
    // the tile itself is restored.
    //
    // Snapshots are keyed by the Map OBJECT, not by list position: the
    // sweep covers MapEditor.AllEditableMaps() — project maps PLUS scratch
    // workspaces — so an index into Project.Maps would misalign, and map
    // adds/removes between capture and undo would shift it anyway.
    private List<int>                                _EntityTypeTileIndices = new List<int>();
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



    public UndoMapTileRemove( MapEditor Editor, MapProject Project, int TileIndex )
    {
      _MapEditor  = Editor;
      _TileIndex  = TileIndex;
      _MapProject = Project;
      _RemovedTile = _MapProject.Tiles[TileIndex];

      foreach ( var map in Project.Maps )
      {
        for ( int i = 0; i < map.Tiles.Width; ++i )
        {
          for ( int j = 0; j < map.Tiles.Height; ++j )
          {
            if ( map.Tiles[i, j] >= TileIndex )
            {
              i = map.Tiles.Width;
              _InternalUndos.Add( new Undo.UndoMapTilesChange( _MapEditor, map, 0, 0, map.Tiles.Width, map.Tiles.Height ) );
              break;
            }
          }
        }
      }

      // Snapshot every entity-type's tile binding. Index in the list
      // matches Project.EntityTypes index; on undo we walk both lists
      // in lockstep up to min(count, count) so a concurrent type-add or
      // type-remove (unlikely but possible) doesn't crash the restore.
      foreach ( var et in Project.EntityTypes )
      {
        _EntityTypeTileIndices.Add( et.TileIndex );
      }

      // Snapshot every editable map's color-override layers — the same set
      // (project maps + scratch workspaces) RemoveTile's sweep mutates,
      // and EVERY layer of each map, because the wipe is per-layer.
      // Char-grid sized (Tiles × spacing); captured deeply (per-char copy)
      // rather than holding a reference because the original layer keeps
      // mutating during the delete. Loop bounds use the layer's own
      // Width/Height so this stays correct regardless of grid dimensions.
      foreach ( var map in Editor.AllEditableMaps() )
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
      return new UndoMapTileAdd( _MapEditor, _MapProject, _TileIndex );
    }



    public override void Apply()
    {
      _MapEditor.AddTile( _TileIndex, _RemovedTile );

      // Restore entity-type → tile bindings. Done after AddTile so the
      // tile list is back to its pre-deletion length when we hand
      // indices back to the entity types — otherwise an index that was
      // valid pre-delete (e.g. == old Tiles.Count - 1) would still be
      // out of range until AddTile inserts the tile.
      int entCount = System.Math.Min( _EntityTypeTileIndices.Count, _MapProject.EntityTypes.Count );
      for ( int i = 0; i < entCount; ++i )
      {
        _MapProject.EntityTypes[i].TileIndex = _EntityTypeTileIndices[i];
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
    }
  }
}
