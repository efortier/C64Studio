using RetroDevStudio.Documents;
using RetroDevStudio.Formats;



namespace RetroDevStudio.Undo
{
  /// <summary>
  /// Full-state undo entry for "revert to revision". Captures the
  /// entire <see cref="MapProject.Map"/> via <see cref="MapProject.CloneMap"/>,
  /// which round-trips every field through the chunk save/load path —
  /// so it covers tiles, both override layers, markers, entities,
  /// name, spacing, alt colors, mode, extra data, etc. without having
  /// to enumerate them by hand. <see cref="Apply"/> copies those
  /// fields back over the live map in place (preserving every other
  /// reference to <c>m_LiveMap</c>) and triggers a full editor
  /// resync via <see cref="MapEditor.InvalidateCurrentMap"/> so the
  /// width / height / name textboxes, scrollbars, and redraw all
  /// catch up to the restored size.
  ///
  /// Why this exists separate from the per-aspect undo classes
  /// (UndoMapTilesChange / UndoMapSizeChange / UndoMapMarkersChange /
  /// etc.): the revert mutates EVERYTHING at once, including size.
  /// Composing per-aspect undos would require each one to be size-
  /// invariant, which they're not — UndoMapTilesChange writes into a
  /// fixed (X, Y, Width, Height) tile rect, and after a size-changing
  /// revert that rect can be out of bounds. A full snapshot avoids
  /// the ordering / bounds / cross-layer-consistency hazards.
  /// </summary>
  public class UndoMapRevert : UndoTask
  {
    private MapEditor          m_MapEditor = null;
    private MapProject.Map     m_AffectedMap = null;
    private MapProject.Map     m_Snapshot = null;



    public UndoMapRevert( MapEditor Editor, MapProject.Map Map )
    {
      m_MapEditor   = Editor;
      m_AffectedMap = Map;
      // Deep clone via the chunk save/load path. Includes all persisted
      // fields automatically — adding a new persisted field to Map
      // doesn't require an update here.
      m_Snapshot = MapProject.CloneMap( Map );
    }




    public override string Description
    {
      get
      {
        return "Revert Map";
      }
    }



    public override UndoTask CreateComplementaryTask()
    {
      return new UndoMapRevert( m_MapEditor, m_AffectedMap );
    }



    public override void Apply()
    {
      if ( ( m_AffectedMap == null )
      ||   ( m_Snapshot == null ) )
      {
        return;
      }

      // In-place field copy so every other reference to the live map
      // (m_MapProject.Maps[i], comboMaps Tupel, m_CurrentMap, etc.)
      // remains valid. This is the same pattern btnRevertRevision_Click
      // uses, kept in lockstep so undo restores ALL fields the original
      // revert touched.
      // Assign the whole layer list so ALL layers restore (Map.Tiles /
      // Map.TileColorOverrides are proxies to Layers[0]; assigning them alone
      // would leave the upper layers stale).
      m_AffectedMap.Layers                      = m_Snapshot.Layers;
      m_AffectedMap.CharBlockedOverrides        = m_Snapshot.CharBlockedOverrides;
      m_AffectedMap.Name                        = m_Snapshot.Name;
      m_AffectedMap.TileSpacingX                = m_Snapshot.TileSpacingX;
      m_AffectedMap.TileSpacingY                = m_Snapshot.TileSpacingY;
      m_AffectedMap.Markers                     = m_Snapshot.Markers;
      m_AffectedMap.Entities                    = m_Snapshot.Entities;
      m_AffectedMap.ExtraDataOld                = m_Snapshot.ExtraDataOld;
      m_AffectedMap.ExtraDataText               = m_Snapshot.ExtraDataText;
      m_AffectedMap.AlternativeMultiColor1      = m_Snapshot.AlternativeMultiColor1;
      m_AffectedMap.AlternativeMultiColor2      = m_Snapshot.AlternativeMultiColor2;
      m_AffectedMap.AlternativeBackgroundColor  = m_Snapshot.AlternativeBackgroundColor;
      m_AffectedMap.AlternativeBGColor4         = m_Snapshot.AlternativeBGColor4;
      m_AffectedMap.SelectedMarkerType          = m_Snapshot.SelectedMarkerType;
      m_AffectedMap.SelectedEntityType          = m_Snapshot.SelectedEntityType;
      m_AffectedMap.MarkerDimOpacity            = m_Snapshot.MarkerDimOpacity;
      m_AffectedMap.AlternativeMode             = m_Snapshot.AlternativeMode;
      m_AffectedMap.MemoRTF                     = m_Snapshot.MemoRTF;

      // Full editor resync. This covers width/height textboxes,
      // scrollbars, char-list refresh, redraw — all the UI bits that
      // would otherwise be stale after a size-changing restore.
      if ( m_MapEditor != null )
      {
        m_MapEditor.InvalidateCurrentMap();
      }
    }
  }
}
