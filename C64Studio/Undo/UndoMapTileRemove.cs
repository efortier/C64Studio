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
    private List<int>                                _EntityTypeTileIndices = new List<int>();
    private List<GR.Game.Layer<int>>                 _ColorOverrideSnapshots = new List<GR.Game.Layer<int>>();
    // Mirrors _ColorOverrideSnapshots for the per-character "blocked"
    // override layer. RemoveTile clears blocked overrides on every map
    // cell that used the deleted tile (so its footprint isn't left
    // pointing at a stale tile-index after the shift), and Apply must
    // be able to restore them.
    private List<GR.Game.Layer<bool>>                _BlockedOverrideSnapshots = new List<GR.Game.Layer<bool>>();



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

      // Snapshot every map's color-override layer. Char-grid sized
      // (Tiles × spacing); captured deeply (per-char copy) rather than
      // holding a reference because the original layer keeps mutating
      // during the delete. Loop bounds use the layer's own Width/Height
      // so this stays correct regardless of grid dimensions.
      foreach ( var map in Project.Maps )
      {
        var snap = new GR.Game.Layer<int>();
        snap.Resize( map.TileColorOverrides.Width, map.TileColorOverrides.Height );
        for ( int j = 0; j < map.TileColorOverrides.Height; ++j )
        {
          for ( int i = 0; i < map.TileColorOverrides.Width; ++i )
          {
            snap[i, j] = map.TileColorOverrides[i, j];
          }
        }
        _ColorOverrideSnapshots.Add( snap );
      }

      // Snapshot every map's blocked-override layer too — same shape,
      // same lifecycle as the color overrides. RemoveTile wipes these
      // for the deleted tile's footprint; Apply restores them in lock
      // step with the color layer.
      foreach ( var map in Project.Maps )
      {
        var blkSnap = new GR.Game.Layer<bool>();
        blkSnap.Resize( map.CharBlockedOverrides.Width, map.CharBlockedOverrides.Height );
        for ( int j = 0; j < map.CharBlockedOverrides.Height; ++j )
        {
          for ( int i = 0; i < map.CharBlockedOverrides.Width; ++i )
          {
            blkSnap[i, j] = map.CharBlockedOverrides[i, j];
          }
        }
        _BlockedOverrideSnapshots.Add( blkSnap );
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

      // Restore per-map color overrides. We deep-copy back into the
      // existing layer rather than swapping references so anyone holding
      // a reference to map.TileColorOverrides keeps seeing the right
      // data.
      int mapCount = System.Math.Min( _ColorOverrideSnapshots.Count, _MapProject.Maps.Count );
      for ( int m = 0; m < mapCount; ++m )
      {
        var map = _MapProject.Maps[m];
        var snap = _ColorOverrideSnapshots[m];
        if ( ( map.TileColorOverrides.Width != snap.Width )
        ||   ( map.TileColorOverrides.Height != snap.Height ) )
        {
          map.TileColorOverrides.Resize( snap.Width, snap.Height );
        }
        for ( int j = 0; j < snap.Height; ++j )
        {
          for ( int i = 0; i < snap.Width; ++i )
          {
            map.TileColorOverrides[i, j] = snap[i, j];
          }
        }
      }

      // Restore per-map blocked overrides — same deep-copy approach.
      int blkMapCount = System.Math.Min( _BlockedOverrideSnapshots.Count, _MapProject.Maps.Count );
      for ( int m = 0; m < blkMapCount; ++m )
      {
        var map = _MapProject.Maps[m];
        var blkSnap = _BlockedOverrideSnapshots[m];
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
