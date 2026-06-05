using RetroDevStudio.Types;
using RetroDevStudio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using RetroDevStudio.Formats;
using RetroDevStudio.Controls;
using System.Runtime.InteropServices;
using Be.Windows.Forms;
using RetroDevStudio.Undo;
using System.Linq;



namespace RetroDevStudio.Documents
{
  public partial class MapEditor : BaseDocument
  {
    private enum ToolMode
    {
      SINGLE_TILE,
      RECTANGLE,
      FILLED_RECTANGLE,
      FILL,
      // Flood-fill the COLOR only: clicking a cell recolours every
      // 4-connected cell of the same tile index with the toolbar's
      // selected placement color (comboTilePlacementColor). The tile
      // index is left untouched; "Default" in the dropdown is a no-op.
      COLOR_REPLACE,
      SELECT,
      MARKER,
      ENTITY,
      // Per-character "blocked" override layer. While active: no tile
      // painting; clicks toggle CharBlockedOverrides per char; the
      // PictureEditor_PostPaint overlay tints the map so the user can
      // see which chars block movement (red = tile-driven, orange =
      // override-driven, blue = redundant override).
      PASSABLE
    };



    private Formats.MapProject          m_MapProject = new RetroDevStudio.Formats.MapProject();

    private Formats.MapProject.Map      m_CurrentMap = null;

    private Formats.MapProject.Tile     m_CurrentEditedTile = null;
    private Formats.MapProject.TileChar m_CurrentTileChar = null;

    private Formats.MapProject.Tile     m_CurrentEditorTile = null;

    private byte                        m_CurrentChar = 0;
    private byte                        m_CurrentColor = 1;

    private const int                   MapDisplayBaseWidth = 320;
    private const int                   MapDisplayBaseHeight = 200;
    private const int                   MapZoomMinPercent = 50;
    private const int                   MapZoomMaxPercent = 400;
    private const int                   MapZoomStepPercent = 25;
    /// <summary>
    /// Extra characters the user can scroll past the map's right and bottom
    /// edges, regardless of zoom. Lets them park non-interactive markers in
    /// empty space outside the map without having to fit them inside the
    /// map's rendered area. At 100% zoom a small map naturally shows blank
    /// space alongside it inside the 40×25 view; at higher zoom levels the
    /// map fills the view and that natural space disappears. This overhang
    /// makes the off-map work area scroll-accessible at every zoom level.
    /// </summary>
    private const int                   MapScrollOverhangChars = 32;
    // Finer step for the mouse wheel — the +/- buttons jump in 25% blocks
    // which is great for quick fit-to-view, but the wheel feels smoother with
    // a smaller increment so the user can fine-tune the zoom level.
    private const int                   MapZoomWheelStepPercent = 5;
    private const int                   MapTileListItemHeight = 44;

    /// <summary>
    /// Effective row height of the Map tab's tile list — the fixed
    /// content height plus the user-configured inter-row separator
    /// (clamped non-negative). Settings may not be available during
    /// early construction; default to 0 separator in that case.
    /// </summary>
    private int MapTileListEffectiveItemHeight
    {
      get
      {
        int sep = 0;
        if ( Core?.Settings != null )
        {
          sep = Math.Max( 0, Core.Settings.MapTileListRowSeparatorHeight );
        }
        return MapTileListItemHeight + sep;
      }
    }
    private const int                   MapTilePreviewPadding = 2;
    // Square edge length (in pixels) of the tile thumbnail rendered into
    // each listTileInfo row on the Tiles tab. Drives both the row height
    // (via the placeholder ImageList entry) and the per-row blit size.
    private const int                   MapTileListThumbnailSize = 32;

    private GR.Image.MemoryImage        m_Image = new GR.Image.MemoryImage( MapDisplayBaseWidth, MapDisplayBaseHeight, GR.Drawing.PixelFormat.Format32bppRgb );

    // Placeholder bitmap held by listTileInfo's SmallImageList. Never
    // drawn (DrawItemImage takes over) but must remain alive while the
    // editor exists — see the constructor for the gory details.
    private System.Drawing.Bitmap       m_TileThumbPlaceholder;

    private int                         m_MapZoomPercent = 100;

    private int                         m_CurEditorOffsetX = 0;
    private int                         m_CurEditorOffsetY = 0;

    private Random                      m_Random = new Random();
    private System.Drawing.Point        m_LastPaintedPos = new System.Drawing.Point( -1, -1 );

    private ToolMode                    m_ToolMode = ToolMode.SINGLE_TILE;

    // Right-click selection: the marker or entity whose fields are currently
    // mirrored in the toolbar value controls. Assigning to either causes the
    // map to redraw with a highlight box and the respective "Delete ✕"
    // toolbar button to enable. Only one thing can be selected at a time,
    // and switching tool modes or current maps clears the selection so it
    // can't drift out of context.
    private Formats.MapProject.Marker   m_SelectedMarker = null;
    private Formats.MapProject.Entity   m_SelectedEntity = null;

    // Drag-to-move state for the currently-selected marker / entity.
    // Set when a left-click LANDS on the selected marker/entity in its
    // tool mode; cleared as soon as the mouse button releases. While
    // true, mouse-move updates the marker/entity X/Y to the cell under
    // the cursor — but only when the cell actually CHANGES (so we don't
    // burn a redraw per pixel). One undo entry is created at drag-start,
    // covering the entire drag stroke.
    // Marker mouse-drag state. m_PressedMarker is armed on a left mouse-down
    // that lands on a marker; if the cursor then leaves m_PressedMarkerCell the
    // drag begins — m_MarkerDrag becomes Move, or Resize when Shift is held.
    // A release with m_MarkerDrag still None is a plain click -> select.
    private enum MarkerDragKind { None, Move, Resize }
    private MarkerDragKind              m_MarkerDrag = MarkerDragKind.None;
    private Formats.MapProject.Marker   m_PressedMarker = null;
    private System.Drawing.Point        m_PressedMarkerCell = new System.Drawing.Point( -1, -1 );
    private bool                        m_DraggingSelectedEntity = false;

    // Bucket-toggle state for a passable-tool drag stroke. The first
    // press samples the clicked char's current value and decides what
    // the entire drag will write (the inverse). Continuing the drag re-
    // enters cells without untoggling — every cell of the stroke ends
    // up at the captured value. m_BlockedDragActive is cleared on mouse
    // release in the same place the marker/entity drag flags clear.
    private bool                        m_BlockedDragActive = false;
    private bool                        m_BlockedDragWriteValue = false;

    // Single-tile right-click selection in tile-painting modes. Pressing
    // Delete with this set replaces that map cell with tile 0. (-1, -1) =
    // no current selection. Cleared on map change and on switching to
    // MARKER / ENTITY / SELECT mode where a different selection metaphor
    // takes over.
    private System.Drawing.Point        m_SelectedTilePos = new System.Drawing.Point( -1, -1 );

    // -1 = "Default" (use the tile's intrinsic char colors when placing).
    // 0..15 = paint all of the placed tile's characters in this single C64
    // color. Driven by comboTilePlacementColor on the editor toolbar; read
    // by every tile-placement code path and written into the current map's
    // TileColorOverrides[x, y] for each cell that gets a new tile.
    private int                         m_TilePlacementColorOverride = -1;

    // Set transiently around any PROGRAMMATIC change to
    // comboTilePlacementColor.SelectedIndex (eyedropper, init,
    // repopulate-after-theme-change). The combo's
    // SelectedIndexChanged handler reads this flag to decide whether
    // to also apply the new color to a currently-selected tile —
    // user-driven combo changes should apply, programmatic ones
    // should NOT (the user didn't ask for an apply when we updated the
    // combo to mirror an eyedropped char).
    private bool                        m_SuppressTilePlacementColorAutoApply = false;

    // Set transiently around the right-click-eyedrops-tile path so
    // comboTiles_SelectedIndexChanged knows the change came from
    // sampling a map tile (not from a direct user click in the tile
    // list) and skips the "reset override color to Default" gesture.
    // Right-clicking on the map should preserve whatever override the
    // user already had set; only an explicit pick from the tile list
    // is the "I'm switching to this tile, give me its native colors"
    // intent.
    private bool                        m_SuppressTilePickerOverrideReset = false;

    // Guards against spurious control-change callbacks firing while we are
    // programmatically copying a selected instance's fields INTO the
    // toolbar controls. Without it, the ValueChanged handlers would
    // immediately write those same values back into the instance — cheap
    // but it inflates the undo log and can reorder triggers.
    private bool                        m_PopulatingFromSelection = false;

    // === Revisions ===========================================================
    // When the user picks a past revision from comboRevisions, we swap
    // m_CurrentMap to point at that revision's snapshot Map and remember
    // the live (editable) map here. Switching back to "(Current)" restores
    // m_CurrentMap from m_LiveMap. While viewing a revision the editor is
    // strictly read-only — m_IsViewingRevision gates every tile-modifying
    // and metadata-modifying entry point.
    //
    // Both the live and snapshot Maps are real Map instances; only the
    // m_IsViewingRevision flag determines whether edits are allowed. We
    // never mutate snapshot data directly: revert produces a fresh deep
    // copy onto m_LiveMap so the snapshot stays pristine for future view
    // / revert / delete operations.
    private Formats.MapProject.Map      m_LiveMap = null;
    private bool                        m_IsViewingRevision = false;
    // Set during programmatic comboRevisions repopulation so the
    // SelectedIndexChanged handler doesn't try to swap maps mid-rebuild.
    private bool                        m_PopulatingRevisionsCombo = false;

    private bool[,]                     m_SelectedTiles = new bool[20, 12];

    private bool                        m_MouseButtonReleased = false;
    private System.Drawing.Point        m_MousePos;

    private System.Drawing.Point        m_DragStartPos = new System.Drawing.Point();
    private System.Drawing.Point        m_DragEndPos = new System.Drawing.Point();
    private System.Drawing.Point        m_LastDragEndPos = new System.Drawing.Point( -1, -1 );

    private bool                        _TileDisplayMouseReleased = true;

    private List<GR.Generic.Tupel<bool,int>>          m_FloatingSelection = null;
    private System.Drawing.Size                       m_FloatingSelectionSize;
    private System.Drawing.Point                      m_FloatingSelectionPos;
    // Per-character color overrides captured at copy time, char-grid
    // sized: m_FloatingSelectionSourceSpacingX × m_FloatingSelectionSize.Width
    // wide by m_FloatingSelectionSourceSpacingY × Size.Height tall, stored
    // row-major. Null when paste came from an older clipboard payload that
    // didn't include override data — InsertFloatingSelection then falls
    // back to ApplyPlacementColorOverride for backward parity. Spacing is
    // captured at copy time because the source map's TileSpacing may differ
    // from the destination's (cross-map paste); the layout is interpreted
    // using the captured source spacing and clipped at the destination's
    // override-layer bounds.
    private List<int>                                 m_FloatingSelectionOverrides = null;
    private int                                       m_FloatingSelectionSourceSpacingX = 1;
    private int                                       m_FloatingSelectionSourceSpacingY = 1;
    // Per-character "blocked" overrides captured at copy time alongside
    // m_FloatingSelectionOverrides. Same char-grid layout (charW × charH
    // booleans, row-major). Null when the clipboard payload didn't
    // include a blocked trailer (older copies, or a payload from a
    // different source) — InsertFloatingSelection then leaves the
    // destination's blocked overrides at false (no-override default).
    private List<bool>                                m_FloatingSelectionBlocked = null;

    private ExportMapFormBase           m_ExportForm = null;
    private ImportMapFormBase           m_ImportForm = null;
    private bool                        m_ApplyingExportSettings = false;
    private bool                        m_ApplyingTileSettings = false;

    private List<int>                   _TileUsage = new List<int>();


    




    public override DocumentInfo DocumentInfo
    {
      get
      {
        return base.DocumentInfo;
      }
      set
      {
        base.DocumentInfo = value;
        characterEditor.UndoManager = DocumentInfo.UndoManager;
      }
    }



    public MapEditor( StudioCore Core )
    {
      this.Core = Core;

      DocumentInfo.Type = ProjectElement.ElementType.MAP_EDITOR;
      DocumentInfo.UndoManager.MainForm = Core.MainForm;

      m_IsSaveable = true;
      InitializeComponent();
      SuspendLayout();

      // Flat dark tab theme for tabMapEditor (map / tiles / character set /
      // export / import / markers / entities). Replaces the default 3D Krypton
      // look with a flat underline-selected style sourced from DarkTheme.
      RetroDevStudio.CustomRenderer.DarkTheme.ApplyFlatDarkStyle( tabMapEditor );

      // Krypton's MaterialDark uses a bright grey for disabled combo backgrounds,
      // which reads as white on our dark surfaces. Push every Krypton combo in
      // this form into the same dark disabled style the SID editor uses.
      foreach ( var combo in FindAllKryptonCombos( this ) )
      {
        RetroDevStudio.CustomRenderer.DarkTheme.StyleDisabledComboDark( combo );
      }

      // Dark scrollbars on the listbox / multiline textbox that show them.
      RetroDevStudio.CustomRenderer.DarkTheme.ApplyDarkScrollBarsTo( comboTiles );

      // The palette/theme dropdown that used to live here was promoted
      // to the global application toolbar (mainToolPaletteSelector) and
      // is persisted in StudioSettings.KryptonPaletteMode.

      // Owner-draw hookup for color combos. Has to happen here (not in the
      // .Designer.cs) because VS's CodeDom serializer can't handle property
      // chains like "control.InnerSubControl.Property = value" — it refuses
      // to load the form designer when InitializeComponent contains them.
      WireOwnerDrawCombo( comboMapBGColor,            comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapMultiColor1,        comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapMultiColor2,        comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapAlternativeBGColor4, comboAlternativeColor_DrawItem );
      // comboMarkerColorOverride and comboTilePlacementColor are plain
      // System.Windows.Forms.ComboBox (not KryptonComboBox), so DrawMode
      // and DrawItem are wired directly in the Designer file — same
      // pattern the 4 working Tile-tab color combos and comboMarkerColor
      // use. Krypton-wrapped combos overdraw the closed-face after our
      // owner-draw fires, which is why these two specifically can't use
      // the WireOwnerDrawCombo path.
      // Blank-color dropdown (shift-click target color) reuses the same
      // single-color drawer the other "pick a C64 palette index" combos
      // use, so the swatch + "00".."15" label rendering matches.
      WireOwnerDrawCombo( comboBlankColor,            comboColor_DrawItem );

      characterEditor.Core = Core;

      GR.Image.DPIHandler.ResizeControlsForDPI( this );

      // Sync the Linear brightness toolbar buttons to the settings
      // flag — they're always declared enabled in the Designer, so
      // we have to actively reflect the user's preference at startup.
      RefreshBrightnessButtonState();

      // Disable marker/entity controls at startup. The Designer leaves
      // them all enabled by default; UpdateMarkerControlsState reads
      // the current tool mode (SINGLE_TILE on a fresh editor) and
      // greys out everything not relevant to it. Without this initial
      // call the controls would only get disabled the first time the
      // user switched tools.
      UpdateMarkerControlsState();

      comboTiles.ItemHeight = MapTileListEffectiveItemHeight;

      // Seed the tile-list spacing controls from the (already-loaded)
      // app-level StudioSettings. Detach + reattach the ValueChanged
      // hook so this seeding doesn't fire the user-facing handler and
      // dirty anything. Defensive on Core?.Settings — Core is set by
      // the host before this constructor block runs, but settings
      // could be null in some test harnesses.
      if ( Core?.Settings != null )
      {
        if ( editTileListRowSpacing != null )
        {
          int sep = Core.Settings.MapTileListRowSeparatorHeight;
          if ( sep < 0 ) sep = 0;
          if ( sep > 32 ) sep = 32;
          editTileListRowSpacing.ValueChanged -= editTileListRowSpacing_ValueChanged;
          editTileListRowSpacing.Value = sep;
          editTileListRowSpacing.ValueChanged += editTileListRowSpacing_ValueChanged;
        }
        if ( btnTileListRowSeparatorColor != null )
        {
          btnTileListRowSeparatorColor.BackColor = System.Drawing.Color.FromArgb(
            unchecked( (int)Core.Settings.MapTileListRowSeparatorColorARGB ) );
        }
      }

      // listTileInfo (Tiles tab) gets a per-row tile thumbnail rendered
      // through the CSListView.DrawItemImage hook. The control reserves
      // image-space only when its SmallImageList has at least one entry
      // of the expected size, so we install a transparent placeholder
      // bitmap sized to MapTileListThumbnailSize × MapTileListThumbnailSize.
      // The placeholder is never actually drawn — DrawItemImage paints
      // over it with the live, palette-correct tile preview built by
      // listTileInfo_DrawItemImage (same approach comboTiles uses).
      //
      // The placeholder Bitmap MUST outlive the ImageList: ImageList.Images
      // .Add keeps the source reference, not a copy, and the ListView
      // queries that source's Size when its handle is realized. Disposing
      // the placeholder before then triggers an ArgumentException out of
      // System.Drawing.Image.get_Size. We keep it in a field so it lives
      // for the editor's lifetime.
      m_TileThumbPlaceholder = new System.Drawing.Bitmap(
        MapTileListThumbnailSize, MapTileListThumbnailSize );
      var tileImgList = new System.Windows.Forms.ImageList
      {
        ImageSize  = new System.Drawing.Size( MapTileListThumbnailSize, MapTileListThumbnailSize ),
        ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit,
      };
      tileImgList.Images.Add( m_TileThumbPlaceholder );
      listTileInfo.SmallImageList = tileImgList;
      // Column layout: # | Preview | Name | Size | Used. The preview lives
      // in column 1 — between the index and name — so the user's eye
      // tracks index → thumbnail → name left-to-right. CSListView's
      // default of column 0 would overlap the index text with the image.
      listTileInfo.ImageColumnIndex = 1;
      listTileInfo.DrawItemImage += listTileInfo_DrawItemImage;

      // Suppress the OS-level typeahead "filtering" on the two tile pickers
      // — listTileInfo (Tiles tab) and comboTiles (Map tab). Default
      // ListBox/ListView behaviour jumps the selection to the first item
      // whose name starts with the pressed letter, but tile-name prefixes
      // collide constantly (Wall1, Wall2, ...) so the jump is more
      // confusing than useful. Equally important: now that S is the
      // SELECT-tool shortcut and G the grid toggle, we don't want a
      // focused list to silently reroute those keystrokes into a name
      // search before ProcessCmdKey can pick them up. Setting Handled =
      // true on KeyPress eats the WM_CHAR before the OS typeahead sees
      // it; arrow / Enter / mouse selection still work because those
      // come through KeyDown, not KeyPress.
      listTileInfo.KeyPress += SuppressTileListTypeahead;
      comboTiles.KeyPress   += SuppressTileListTypeahead;

      characterEditor.UndoManager = DocumentInfo.UndoManager;
      characterEditor.Core = Core;
      characterEditor.Modified += CharacterEditor_Modified;
      characterEditor.ShowCreateTileButton = true;
      characterEditor.CreateTileFromCharacter += CharacterEditor_CreateTileFromCharacter;
      characterEditor.CreateMultipleTilesFromCharacters += CharacterEditor_CreateMultipleTilesFromCharacters;
      characterEditor.CharacterSelectionChanged += CharacterEditor_CharacterSelectionChanged;

      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "as assembly", typeof( ExportMapAsAssembly ) ) );
      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "to binary file", typeof( ExportMapAsBinaryFile ) ) );
      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "charset to charset project", typeof( ExportMapCharsetAsCharset ) ) );
      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "charset to binary file", typeof( ExportMapCharsetAsBinaryFile ) ) );
      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "map to char screen project", typeof( ExportMapAsCharscreen ) ) );
      comboExportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "as game binary", typeof( ExportMapAsGameBinary ) ) );
      comboExportMethod.SelectedIndex = 0;

      comboImportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "charset from character set file", typeof( ImportMapCharsetFromCharsetFile ) ) );
      comboImportMethod.Items.Add( new GR.Generic.Tupel<string, Type>( "map/charset from binary/charpad file", typeof( ImportMapFromBinaryFile ) ) );
      comboImportMethod.SelectedIndex = 0;

      foreach ( MapExportType exportType in Enum.GetValues( typeof( MapExportType ) ) )
      {
        comboExportData.Items.Add( new GR.Generic.Tupel<string, MapExportType>( GR.EnumHelper.GetDescription( exportType ), exportType ) );
      }

      pictureEditor.MouseWheel += pictureEditor_MouseWheel;
      tabEditor.Resize += tabEditor_Resize;
      pictureEditor.DisplayPage.Create( MapDisplayBaseWidth, MapDisplayBaseHeight, GR.Drawing.PixelFormat.Format32bppRgb );
      pictureEditor.PostPaint += PictureEditor_PostPaint;
      pictureTileDisplay.ClientSize = new System.Drawing.Size( 256, 256 );
      pictureTileDisplay.DisplayPage.Create( 128, 128, GR.Drawing.PixelFormat.Format32bppRgb );
      panelCharacters.PixelFormat = GR.Drawing.PixelFormat.Format32bppRgb;
      panelCharacters.SetDisplaySize( 128, 128 );
      panelCharacters.DisplayPage.Create( 128, 128, GR.Drawing.PixelFormat.Format32bppRgb );

      panelCharColors.DisplayPage.Create( 128, 8, GR.Drawing.PixelFormat.Format32bppRgb );

      m_Image.Create( MapDisplayBaseWidth, MapDisplayBaseHeight, GR.Drawing.PixelFormat.Format32bppRgb );

      Palette   pal = Core.MainForm.ActivePalette;

      PaletteManager.ApplyPalette( pictureEditor.DisplayPage );
      PaletteManager.ApplyPalette( pictureTileDisplay.DisplayPage );
      PaletteManager.ApplyPalette( panelCharacters.DisplayPage );
      PaletteManager.ApplyPalette( m_Image );
      PaletteManager.ApplyPalette( panelCharColors.DisplayPage );
      if ( Core != null )
      {
        SetMapZoomPercent( Core.Settings.MapEditorZoomPercent );
      }

      // Batch the populate loop in BeginUpdate/EndUpdate. Both ComboBox
      // and KryptonComboBox re-layout/invalidate on every Items.Add by
      // default; without batching, 11 combos × 16 strings = 176 layout
      // passes (~270 ms in profiling). The two types don't share a base
      // class with BeginUpdate, so the calls are listed inline rather
      // than driven by a typed array. The try/finally ensures EndUpdate
      // always runs even if a future addition throws.
      comboTileBackground.BeginUpdate();
      comboTileMulticolor1.BeginUpdate();
      comboTileMulticolor2.BeginUpdate();
      comboTileBGColor4.BeginUpdate();
      comboMapMultiColor1.BeginUpdate();
      comboMapMultiColor2.BeginUpdate();
      comboMapBGColor.BeginUpdate();
      comboMapAlternativeBGColor4.BeginUpdate();
      comboMarkerColor.BeginUpdate();
      comboMarkerColorOverride.BeginUpdate();
      comboBlankColor.BeginUpdate();
      comboMapStringLineControl0.BeginUpdate();
      comboMapStringLineControl1.BeginUpdate();
      comboMapStringLineControl2.BeginUpdate();
      comboMapStringLineControl3.BeginUpdate();
      comboMapStringLineControl4.BeginUpdate();
      try
      {
        comboMapMultiColor1.Items.Add( "From charset" );
        comboMapMultiColor2.Items.Add( "From charset" );
        comboMapBGColor.Items.Add( "Project" );
        comboMapAlternativeBGColor4.Items.Add( "Project" );
        // Map String line-control combos lead with "None" — selecting it
        // means the line emits no leading control byte (see
        // MAP_STRING_NO_CONTROL_CODE). Colors $00..$0F follow at indices
        // 1..16 and are rendered by comboMapStringLineControl_DrawItem.
        comboMapStringLineControl0.Items.Add( "None" );
        comboMapStringLineControl1.Items.Add( "None" );
        comboMapStringLineControl2.Items.Add( "None" );
        comboMapStringLineControl3.Items.Add( "None" );
        comboMapStringLineControl4.Items.Add( "None" );
        for ( int i = 0; i < 16; ++i )
        {
          string label = i.ToString( "d2" );
          comboTileBackground.Items.Add( label );
          comboTileMulticolor1.Items.Add( label );
          comboTileMulticolor2.Items.Add( label );
          comboTileBGColor4.Items.Add( label );
          comboMapMultiColor1.Items.Add( label );
          comboMapMultiColor2.Items.Add( label );
          comboMapBGColor.Items.Add( label );
          comboMapAlternativeBGColor4.Items.Add( label );
          comboMarkerColor.Items.Add( label );
          comboMarkerColorOverride.Items.Add( label );
          comboBlankColor.Items.Add( label );
          comboMapStringLineControl0.Items.Add( label );
          comboMapStringLineControl1.Items.Add( label );
          comboMapStringLineControl2.Items.Add( label );
          comboMapStringLineControl3.Items.Add( label );
          comboMapStringLineControl4.Items.Add( label );
        }
      }
      finally
      {
        comboTileBackground.EndUpdate();
        comboTileMulticolor1.EndUpdate();
        comboTileMulticolor2.EndUpdate();
        comboTileBGColor4.EndUpdate();
        comboMapMultiColor1.EndUpdate();
        comboMapMultiColor2.EndUpdate();
        comboMapBGColor.EndUpdate();
        comboMapAlternativeBGColor4.EndUpdate();
        comboMarkerColor.EndUpdate();
        comboMarkerColorOverride.EndUpdate();
        comboBlankColor.EndUpdate();
        comboMapStringLineControl0.EndUpdate();
        comboMapStringLineControl1.EndUpdate();
        comboMapStringLineControl2.EndUpdate();
        comboMapStringLineControl3.EndUpdate();
        comboMapStringLineControl4.EndUpdate();
      }
      comboTileBackground.SelectedIndex = 0;
      comboTileMulticolor1.SelectedIndex = 0;
      comboTileMulticolor2.SelectedIndex = 0;
      comboTileBGColor4.SelectedIndex = 0;
      comboMapMultiColor1.SelectedIndex = 0;
      comboMapMultiColor2.SelectedIndex = 0;
      comboMapMultiColor2.SelectedIndex = 0;
      comboMapBGColor.SelectedIndex = 0;
      comboMapAlternativeBGColor4.SelectedIndex = 0;
      comboMarkerColor.SelectedIndex = 0;
      comboMarkerColorOverride.SelectedIndex = 0;
      // Per-line control-code combos default to "None" (index 0). Each
      // string's actual selection is set in PopulateMapStringFieldsFromSelection.
      comboMapStringLineControl0.SelectedIndex = 0;
      comboMapStringLineControl1.SelectedIndex = 0;
      comboMapStringLineControl2.SelectedIndex = 0;
      comboMapStringLineControl3.SelectedIndex = 0;
      comboMapStringLineControl4.SelectedIndex = 0;

      InitMapStringsTab();

      // "Default" + 16 C64 colors for the tile placement color override.
      // Default index 0 means no override; placing leaves the tile's
      // intrinsic char colors alone. Suppress the auto-apply-to-
      // selected-tile path: this is editor init, no user intent to
      // overwrite anything.
      m_SuppressTilePlacementColorAutoApply = true;
      try
      {
        RefreshTilePlacementColorCombo();
        comboTilePlacementColor.SelectedIndex = 0;
      }
      finally
      {
        m_SuppressTilePlacementColorAutoApply = false;
      }

      comboExportOrientation.SelectedIndex = 0;
      comboExportData.SelectedIndex = 0;
      comboExportData.SelectedIndexChanged += ExportSettingsChanged;
      comboExportOrientation.SelectedIndexChanged += ExportSettingsChanged;
      comboRightClickBehavior.SelectedIndexChanged += comboRightClickBehavior_SelectedIndexChanged;

      foreach ( TextMode mode in Enum.GetValues( typeof( TextMode ) ) )
      {
        if ( mode != TextMode.UNKNOWN )
        {
          comboMapProjectMode.Items.Add( GR.EnumHelper.GetDescription( mode ) );
        }
      }
      comboMapProjectMode.SelectedIndex = 0;

      comboMapAlternativeMode.Items.Add( "From Project" );
      foreach ( TextCharMode mode in Enum.GetValues( typeof( TextCharMode ) ) )
      {
        if ( mode != TextCharMode.UNKNOWN )
        {
          comboMapAlternativeMode.Items.Add( GR.EnumHelper.GetDescription( mode ) );
        }
      }
      comboMapAlternativeMode.SelectedIndex = 0;

      Core.MainForm.ApplicationEvent += new MainForm.ApplicationEventHandler( MainForm_ApplicationEvent );

      for ( int i = 0; i < 256; ++i )
      {
        RebuildCharImage( i );
        panelCharacters.Items.Add( i.ToString(), m_MapProject.Charset.Characters[i].Tile.Image );
      }

      characterEditor.CharsetUpdated( m_MapProject.Charset );
      RefreshMapTileList();
      Modified = false;

      ResumeLayout();
      ResumeLayout();
    }




    private int ScreenCharWidth
    {
      get
      {
        return Lookup.ScreenWidthInCharacters( m_MapProject.Mode );
      }
    }



    private int ScreenCharHeight
    {
      get
      {
        return Lookup.ScreenHeightInCharacters( m_MapProject.Mode );
      }
    }



    private int ViewCharWidth
    {
      get
      {
        if ( ( pictureEditor == null )
        ||   ( pictureEditor.DisplayPage == null )
        ||   ( pictureEditor.DisplayPage.Width == 0 ) )
        {
          return ScreenCharWidth;
        }
        return Math.Max( 1, pictureEditor.DisplayPage.Width / 8 );
      }
    }



    private int ViewCharHeight
    {
      get
      {
        if ( ( pictureEditor == null )
        ||   ( pictureEditor.DisplayPage == null )
        ||   ( pictureEditor.DisplayPage.Height == 0 ) )
        {
          return ScreenCharHeight;
        }
        return Math.Max( 1, pictureEditor.DisplayPage.Height / 8 );
      }
    }



    private void SetMapZoomPercent( int ZoomPercent )
    {
      int clampedPercent = Math.Max( MapZoomMinPercent, Math.Min( MapZoomMaxPercent, ZoomPercent ) );
      if ( clampedPercent == m_MapZoomPercent )
      {
        return;
      }
      m_MapZoomPercent = clampedPercent;
      if ( ( Core != null )
      &&   ( Core.Settings != null ) )
      {
        Core.Settings.MapEditorZoomPercent = m_MapZoomPercent;
      }
      ApplyMapZoom();
    }



    private void ApplyMapZoom()
    {
      // The viewport buffer (DisplayPage) AND its on-screen rectangle are
      // both computed in UpdateMapAspectRatio: it derives the integer
      // magnification from the zoom level, then sizes the buffer to fill
      // the actual canvas at that magnification (so a wider/taller window
      // shows more of the map instead of letterboxing the surplus).
      UpdateMapAspectRatio();

      UpdateZoomButtons();
      AdjustScrollbars();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void UpdateZoomButtons()
    {
      if ( btnZoomIn != null )
      {
        btnZoomIn.Enabled = m_MapZoomPercent < MapZoomMaxPercent;
      }
      if ( btnZoomOut != null )
      {
        btnZoomOut.Enabled = m_MapZoomPercent > MapZoomMinPercent;
      }
      if ( labelZoom != null )
      {
        labelZoom.Text = m_MapZoomPercent.ToString() + "%";
      }
    }



    private static int ScaleCoordCeil( int SourceCoord, int SourceSize, int TargetSize )
    {
      if ( SourceSize <= 0 )
      {
        return 0;
      }
      return (int)Math.Ceiling( SourceCoord * (double)TargetSize / SourceSize );
    }



    public override bool ApplyFunction( Function Function )
    {
      if ( characterEditor.EditorFocused )
      {
        switch ( Function )
        {
          case Function.GRAPHIC_ELEMENT_MIRROR_H:
            characterEditor.MirrorX();
            return true;
          case Function.GRAPHIC_ELEMENT_MIRROR_V:
            characterEditor.MirrorY();
            return true;
          case Function.GRAPHIC_ELEMENT_SHIFT_D:
            characterEditor.ShiftDown();
            return true;
          case Function.GRAPHIC_ELEMENT_SHIFT_U:
            characterEditor.ShiftUp();
            return true;
          case Function.GRAPHIC_ELEMENT_SHIFT_L:
            characterEditor.ShiftLeft();
            return true;
          case Function.GRAPHIC_ELEMENT_SHIFT_R:
            characterEditor.ShiftRight();
            return true;
          case Function.GRAPHIC_ELEMENT_ROTATE_L:
            characterEditor.RotateLeft();
            return true;
          case Function.GRAPHIC_ELEMENT_ROTATE_R:
            characterEditor.RotateRight();
            return true;
          case Function.GRAPHIC_ELEMENT_INVERT:
            characterEditor.Invert();
            return true;
          case Function.GRAPHIC_ELEMENT_PREVIOUS:
            characterEditor.Previous();
            return true;
          case Function.GRAPHIC_ELEMENT_NEXT:
            characterEditor.Next();
            return true;
          case Function.GRAPHIC_ELEMENT_CUSTOM_COLOR:
            characterEditor.CustomColor();
            return true;
          case Function.GRAPHIC_ELEMENT_MULTI_COLOR_1:
            characterEditor.MultiColor1();
            return true;
          case Function.GRAPHIC_ELEMENT_MULTI_COLOR_2:
            characterEditor.MultiColor2();
            return true;
          case Function.GRAPHIC_ELEMENT_BACKGROUND_COLOR:
            characterEditor.BackgroundColor();
            return true;
          case Function.COPY:
            characterEditor.Copy();
            return true;
          case Function.PASTE:
            characterEditor.Paste();
            return true;
        }
      }
      else 
      {
        switch ( Function )
        {
          case Function.COPY:
            if ( m_ToolMode == ToolMode.SELECT )
            {
              if ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) )
              {
                CopyToClipboard();
                return true;
              }
            }
            break;
          case Function.PASTE:
            if ( ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) )
            ||   ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabTiles, FocusSupport.FocusControlReason.COPY_PASTE ) ) )
            {
              PasteFromClipboard();
              return true;
            }
            break;
        }
      }
      return base.ApplyFunction( Function );
    }



    public override bool CopyPossible
    {
      get
      {
        return ( ( characterEditor.EditorFocused )
        ||       ( ( m_ToolMode == ToolMode.SELECT )
        &&         ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) ) ) );
      }
    }



    public override bool PastePossible
    {
      get
      {
        return ( ( characterEditor.EditorFocused )
        ||       ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabTiles, FocusSupport.FocusControlReason.COPY_PASTE ) )
        ||       ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) ) );
      }
    }



    private void PictureEditor_PostPaint( GR.Image.FastImage TargetBuffer )
    {
      if ( ( pictureEditor.DisplayPage.Width == 0 )
      ||   ( pictureEditor.DisplayPage.Height == 0 )
      ||   ( TargetBuffer.Width == 0 )
      ||   ( TargetBuffer.Height == 0 ) )
      {
        return;
      }
      int   sourceWidth = pictureEditor.DisplayPage.Width;
      int   sourceHeight = pictureEditor.DisplayPage.Height;
      int   targetWidth = TargetBuffer.Width;
      int   targetHeight = TargetBuffer.Height;
      int   targetMaxX = targetWidth - 1;
      int   targetMaxY = targetHeight - 1;

      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      if ( ( m_MapProject.ShowGrid )
      &&   ( m_MapProject.GridOpacity > 0 ) )
      {
        if ( m_CurrentMap == null )
        {
          pictureEditor.Invalidate();
          return;
        }

        int offsetX = m_CurEditorOffsetX;
        int offsetY = m_CurEditorOffsetY;
        int viewCharWidth = ViewCharWidth;
        int viewCharHeight = ViewCharHeight;
        // Start back far enough to include the columns/rows the centering gap
        // exposes left/above the scroll position (same reasoning as the tile
        // loop in RedrawMap), clamped to 0. Otherwise the grid is missing on
        // the exposed edge of a scrolled, centered map.
        int gridCellWX = m_CurrentMap.TileSpacingX * 8;
        int gridCellWY = m_CurrentMap.TileSpacingY * 8;
        int x1 = Math.Max( 0, offsetX - ( renderOffsetX / gridCellWX ) - 1 );
        int y1 = Math.Max( 0, offsetY - ( renderOffsetY / gridCellWY ) - 1 );
        int x2 = Math.Min( offsetX + (int)Math.Ceiling( viewCharWidth / (float)m_CurrentMap.TileSpacingX ), offsetX + m_CurrentMap.Tiles.Width );
        int y2 = Math.Min( offsetY + (int)Math.Ceiling( viewCharHeight / (float)m_CurrentMap.TileSpacingY ), offsetY + m_CurrentMap.Tiles.Height );

        // restrict grid to actual map size
        long    mapPixelWidth = (long)( m_CurrentMap.Tiles.Width - offsetX ) * m_CurrentMap.TileSpacingX * 8;
        long    mapPixelHeight = (long)( m_CurrentMap.Tiles.Height - offsetY ) * m_CurrentMap.TileSpacingY * 8;

        int     targetMapWidth = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( renderOffsetX + (int)mapPixelWidth, sourceWidth, targetWidth ) ) );
        int     targetMapHeight = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( renderOffsetY + (int)mapPixelHeight, sourceHeight, targetHeight ) ) );

        // Grid alpha 0..255 derived from GridOpacity 1..100. (0 was
        // short-circuited out above.) FastImage primitives don't blend,
        // so each grid pixel does its own read-blend-write below; using
        // Line() with an opaque color would just paint solid white.
        // The left/top edge follows the leftmost/topmost visible map cell,
        // which the centering gap can push left/above renderOffset on a
        // scrolled map; clamp to the buffer so it never goes negative.
        int gridSrcLeft = Math.Max( 0, renderOffsetX + ( x1 - offsetX ) * gridCellWX );
        int gridSrcTop  = Math.Max( 0, renderOffsetY + ( y1 - offsetY ) * gridCellWY );
        int gridTopY    = ScaleCoordCeil( gridSrcTop,  sourceHeight, targetHeight );
        int gridBottomY = targetMapHeight;
        int gridLeftX   = ScaleCoordCeil( gridSrcLeft, sourceWidth, targetWidth );
        int gridRightX  = targetMapWidth;
        int gridAlpha   = ( m_MapProject.GridOpacity * 255 ) / 100;
        if ( gridAlpha > 255 ) gridAlpha = 255;

        for ( int x = x1; x <= x2; ++x )
        {
          int sourceX = renderOffsetX + ( x - offsetX ) * m_CurrentMap.TileSpacingX * 8;
          int targetX = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX, sourceWidth, targetWidth ) ) );

          if ( targetX <= targetMapWidth )
          {
            BlendGridSpanVertical( TargetBuffer, targetX, gridTopY, gridBottomY, gridAlpha );
          }
        }
        for ( int y = y1; y <= y2; ++y )
        {
          int sourceY = renderOffsetY + ( y - offsetY ) * m_CurrentMap.TileSpacingY * 8;
          int targetY = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY, sourceHeight, targetHeight ) ) );

          if ( targetY <= targetMapHeight )
          {
            BlendGridSpanHorizontal( TargetBuffer, gridLeftX, gridRightX, targetY, gridAlpha );
          }
        }
        /*
        for ( int y = y1; y <= y2; ++y )
        {
          for ( int x = x1; x <= x2; ++x )
          {
            TargetBuffer.Rectangle( ( x - offsetX ) * m_CurrentMap.TileSpacingX * 16, ( y - offsetY ) * m_CurrentMap.TileSpacingY * 16, m_CurrentMap.TileSpacingX * 16, m_CurrentMap.TileSpacingY * 16, 0xffffffff );
          }
        }*/
      }

      // Dim the whole map when in MARKER placement mode so the marker
      // overlays pop. Both modes share the slider (dimSlider) and
      // m_CurrentMap.MarkerDimOpacity, but ENTITY-mode dimming happens
      // earlier in RedrawMap — there we can dim the tile layer BEFORE the
      // entity overlays are drawn so the entity icons stay un-dimmed.
      if ( ( m_CurrentMap != null )
      &&   ( m_ToolMode == ToolMode.MARKER )
      &&   ( m_CurrentMap.MarkerDimOpacity < 100 ) )
      {
        // Manual dimming because Box doesn't blend
        int opacity = m_CurrentMap.MarkerDimOpacity;
        for ( int y = 0; y < targetHeight; ++y )
        {
          for ( int x = 0; x < targetWidth; ++x )
          {
            uint pixel = TargetBuffer.GetPixel( x, y );
            uint r = ( pixel & 0xff ) * (uint)opacity / 100;
            uint g = ( ( pixel >> 8 ) & 0xff ) * (uint)opacity / 100;
            uint b = ( ( pixel >> 16 ) & 0xff ) * (uint)opacity / 100;
            TargetBuffer.SetPixel( x, y, ( 0xff000000 | ( b << 16 ) | ( g << 8 ) | r ) );
          }
        }
      }

      // Per-character "blocked" overlay. Active only in PASSABLE tool
      // mode. For each visible character cell we determine its effective
      // export state and tint:
      //   tile.Passable=false, override=false  → RED 50%   (impassable from tile)
      //   tile.Passable=true,  override=true   → ORANGE 50% (impassable from override)
      //   tile.Passable=false, override=true   → BLUE 20%  (override is redundant)
      //   tile.Passable=true,  override=false  → no overlay
      // Per-pixel blend mirrors the MarkerDimOpacity loop above. Walks
      // visible chars (one inner loop per char's pixel block) to keep
      // the work proportional to the rendered map area.
      if ( ( m_CurrentMap != null )
      &&   ( m_ToolMode == ToolMode.PASSABLE ) )
      {
        int offsetX = m_CurEditorOffsetX;
        int offsetY = m_CurEditorOffsetY;
        int viewCharWidth = ViewCharWidth;
        int viewCharHeight = ViewCharHeight;
        int spacingX = m_CurrentMap.TileSpacingX;
        int spacingY = m_CurrentMap.TileSpacingY;
        int passableLayerW = m_CurrentMap.CharBlockedOverrides.Width;
        int passableLayerH = m_CurrentMap.CharBlockedOverrides.Height;

        // Component order in TargetBuffer is 0xAARRGGBB: r = (pixel>>16) & 0xff,
        // g = (pixel>>8) & 0xff, b = pixel & 0xff, alpha in top byte. Unlike the
        // marker-dim loop above (uniform scale, channel labels don't matter), this
        // block applies non-uniform per-channel tints, so the order must be correct.
        // Start at negative view-char coords so the chars the centering gap
        // exposes above/left of the scroll position are tinted too (matches
        // the tile loop / grid). The charMap < 0 guards below clamp the low
        // end; sourceY/sourceX below map these to pixels >= 0.
        int passableStartCharX = -( renderOffsetX / 8 ) - 1;
        int passableStartCharY = -( renderOffsetY / 8 ) - 1;
        for ( int viewCharY = passableStartCharY; viewCharY < viewCharHeight; ++viewCharY )
        {
          int charMapY = offsetY * spacingY + viewCharY;
          if ( ( charMapY < 0 ) || ( charMapY >= passableLayerH ) ) continue;
          int tileY = charMapY / spacingY;
          if ( ( tileY < 0 ) || ( tileY >= m_CurrentMap.Tiles.Height ) ) continue;

          for ( int viewCharX = passableStartCharX; viewCharX < viewCharWidth; ++viewCharX )
          {
            int charMapX = offsetX * spacingX + viewCharX;
            if ( ( charMapX < 0 ) || ( charMapX >= passableLayerW ) ) continue;
            int tileX = charMapX / spacingX;
            if ( ( tileX < 0 ) || ( tileX >= m_CurrentMap.Tiles.Width ) ) continue;

            int tileIndex = m_CurrentMap.Tiles[tileX, tileY];
            bool tilePassable = ( tileIndex >= 0 && tileIndex < m_MapProject.Tiles.Count )
                                ? m_MapProject.Tiles[tileIndex].Passable : true;
            bool blockedOverride = m_CurrentMap.CharBlockedOverrides[charMapX, charMapY];

            uint tintR, tintG, tintB;
            int  alpha;
            if ( !tilePassable && !blockedOverride )
            {
              // Impassable from tile — strong red.
              tintR = 255; tintG = 0; tintB = 0; alpha = 128;
            }
            else if ( tilePassable && blockedOverride )
            {
              // Impassable from override — strong orange to distinguish
              // from tile-driven red (so the user can see at a glance
              // which chars they personally blocked).
              tintR = 255; tintG = 140; tintB = 0; alpha = 128;
            }
            else if ( !tilePassable && blockedOverride )
            {
              // Redundant override (tile already blocks). Faint blue
              // so the user knows the override is set but isn't doing
              // any work in the bitfield.
              tintR = 0; tintG = 128; tintB = 255; alpha = 50;
            }
            else
            {
              continue;   // tile.Passable && !override → no overlay
            }

            // Source-space rect for this char (8×8 pixels), scaled to
            // TargetBuffer rect with the same ScaleCoordCeil math as
            // the grid block above.
            int sourceX  = renderOffsetX + viewCharX * 8;
            int sourceY  = renderOffsetY + viewCharY * 8;
            int sourceX2 = sourceX + 8;
            int sourceY2 = sourceY + 8;
            int targetX  = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX,  sourceWidth,  targetWidth  ) ) );
            int targetY  = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY,  sourceHeight, targetHeight ) ) );
            int targetX2 = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX2, sourceWidth,  targetWidth  ) ) );
            int targetY2 = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY2, sourceHeight, targetHeight ) ) );

            int invAlpha = 255 - alpha;
            for ( int py = targetY; py < targetY2; ++py )
            {
              for ( int px = targetX; px < targetX2; ++px )
              {
                uint pixel = TargetBuffer.GetPixel( px, py );
                uint pr = ( pixel >> 16 ) & 0xff;
                uint pg = ( pixel >> 8  ) & 0xff;
                uint pb = pixel & 0xff;
                uint nr = ( tintR * (uint)alpha + pr * (uint)invAlpha ) / 255;
                uint ng = ( tintG * (uint)alpha + pg * (uint)invAlpha ) / 255;
                uint nb = ( tintB * (uint)alpha + pb * (uint)invAlpha ) / 255;
                TargetBuffer.SetPixel( px, py, ( 0xff000000 | ( nr << 16 ) | ( ng << 8 ) | nb ) );
              }
            }
          }
        }
      }

      if ( ( m_CurrentMap != null )
      &&   ( m_ToolMode == ToolMode.MARKER ) )
      {
        foreach ( var marker in m_CurrentMap.Markers )
        {
          int sourceX = renderOffsetX + ( marker.X - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
          int sourceY = renderOffsetY + ( marker.Y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;
          // The box spans the marker's full Width x Height footprint.
          int sourceW = m_CurrentMap.TileSpacingX * 8 * Math.Max( 1, marker.Width );
          int sourceH = m_CurrentMap.TileSpacingY * 8 * Math.Max( 1, marker.Height );
          
          if ( ( sourceX >= 0 ) && ( sourceY >= 0 ) && ( sourceX < sourceWidth ) && ( sourceY < sourceHeight ) )
          {
             int reducedW = sourceW * 80 / 100;
             int reducedH = sourceH * 80 / 100;
             int shiftW = ( sourceW - reducedW ) / 2;
             int shiftH = ( sourceH - reducedH ) / 2;
             
             if ( m_ToolMode == ToolMode.MARKER )
             {
               sourceX += shiftW;
               sourceY += shiftH;
               sourceW = reducedW;
               sourceH = reducedH;
             }
             
             int targetX = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX, sourceWidth, targetWidth ) ) );
             int targetY = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY, sourceHeight, targetHeight ) ) );
             int targetW = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX + sourceW, sourceWidth, targetWidth ) ) ) - targetX;
             int targetH = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY + sourceH, sourceHeight, targetHeight ) ) ) - targetY;
             
             uint color = 0xffffffff;
             var  type = m_MapProject.MarkerTypes.FirstOrDefault( t => t.ID == marker.Type );
             if ( type != null )
             {
               color = (uint)m_MapProject.Charset.Colors.Palette.ColorValues[type.Color];
             }
             
             // Inset box
             if ( m_ToolMode == ToolMode.MARKER )
             {
               TargetBuffer.Box( targetX, targetY, targetW, targetH, color );
             }
             else
             {
               TargetBuffer.Rectangle( targetX, targetY, targetW, targetH, 0xff000000 | color );
               TargetBuffer.Rectangle( targetX + 1, targetY + 1, targetW - 2, targetH - 2, 0xff000000 );
             }
          }
        }
      }

      // Selection highlight — draw on top of everything else so the user
      // can see which marker, entity, or tile the toolbar controls are
      // editing. Bright yellow double-thickness outline drawn AT THE FULL
      // FOOTPRINT of the selected thing — for entities and tiles that's
      // the multi-cell tile size, for markers it's always 1×1.
      if ( m_CurrentMap != null )
      {
        const uint highlightColor = 0xfff9e2af;   // Catppuccin yellow
        const uint disabledEntityColor = 0xffff0000;   // red — flags a not-enabled entity
        // Shared computation: given a map-cell (mx, my) and a footprint in
        // cells (cw × ch), draw a 2-pixel-thick rectangle outline at the
        // corresponding TargetBuffer pixels.
        System.Action<int, int, int, int, uint, bool> drawHighlightAt = ( int mx, int my, int cw, int ch, uint outlineColor, bool doubleThick ) =>
        {
          if ( cw < 1 ) cw = 1;
          if ( ch < 1 ) ch = 1;
          int sourceX = renderOffsetX + ( mx - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
          int sourceY = renderOffsetY + ( my - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;
          int sourceW = cw * m_CurrentMap.TileSpacingX * 8;
          int sourceH = ch * m_CurrentMap.TileSpacingY * 8;
          if ( ( sourceX + sourceW <= 0 )
          ||   ( sourceY + sourceH <= 0 )
          ||   ( sourceX >= sourceWidth )
          ||   ( sourceY >= sourceHeight ) )
          {
            return;
          }
          int tx = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX, sourceWidth, targetWidth ) ) );
          int ty = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY, sourceHeight, targetHeight ) ) );
          int tx2 = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX + sourceW, sourceWidth, targetWidth ) - 1 ) );
          int ty2 = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY + sourceH, sourceHeight, targetHeight ) - 1 ) );
          int tw = Math.Max( 1, tx2 - tx + 1 );
          int th = Math.Max( 1, ty2 - ty + 1 );
          // One rectangle = a 1-pixel outline. doubleThick insets and
          // redraws a second for a 2-pixel border (FastImage.Rectangle
          // draws a 1-pixel border only).
          TargetBuffer.Rectangle( tx,     ty,     tw,     th,     outlineColor );
          if ( doubleThick && ( tw > 2 ) && ( th > 2 ) )
          {
            TargetBuffer.Rectangle( tx + 1, ty + 1, tw - 2, th - 2, outlineColor );
          }
        };

        // Disabled entities get a thin red outline around their whole
        // tile footprint — one rectangle for the multi-cell region, not
        // one per character — so they stand out wherever entities are
        // shown, in any tool mode.
        if ( ( checkShowEntities != null )
        &&   ( checkShowEntities.Checked ) )
        {
          foreach ( var entity in m_CurrentMap.Entities )
          {
            if ( entity.Enabled )
            {
              continue;
            }
            int cw = 1, ch = 1;
            var etype = m_MapProject.EntityTypes.FirstOrDefault( t => t.ID == entity.Type );
            if ( ( etype != null )
            &&   ( etype.TileIndex >= 0 )
            &&   ( etype.TileIndex < m_MapProject.Tiles.Count ) )
            {
              GetTileCellFootprint( m_MapProject.Tiles[etype.TileIndex], out cw, out ch );
            }
            drawHighlightAt( entity.X, entity.Y, cw, ch, disabledEntityColor, false );
          }
        }

        if ( ( m_SelectedMarker != null )
        &&   ( m_ToolMode == ToolMode.MARKER )
        &&   ( m_CurrentMap.Markers.Contains( m_SelectedMarker ) ) )
        {
          // Highlight the marker's full Width x Height footprint.
          drawHighlightAt( m_SelectedMarker.X, m_SelectedMarker.Y,
                           Math.Max( 1, m_SelectedMarker.Width ),
                           Math.Max( 1, m_SelectedMarker.Height ),
                           highlightColor, true );
        }
        if ( ( m_SelectedEntity != null )
        &&   ( m_ToolMode == ToolMode.ENTITY )
        &&   ( m_CurrentMap.Entities.Contains( m_SelectedEntity ) ) )
        {
          // Entity outline = footprint of the entity's referenced tile, so
          // a multi-character entity sprite gets boxed at full size rather
          // than just the anchor cell.
          int cw = 1, ch = 1;
          var etype = m_MapProject.EntityTypes.FirstOrDefault( t => t.ID == m_SelectedEntity.Type );
          if ( ( etype != null )
          &&   ( etype.TileIndex >= 0 )
          &&   ( etype.TileIndex < m_MapProject.Tiles.Count ) )
          {
            GetTileCellFootprint( m_MapProject.Tiles[etype.TileIndex], out cw, out ch );
          }
          drawHighlightAt( m_SelectedEntity.X, m_SelectedEntity.Y, cw, ch, highlightColor, true );
        }
        // Tile right-click selection — only relevant in tile-painting
        // modes. SELECT/MARKER/ENTITY have their own selection metaphors
        // and would just confuse the eye if a tile cursor also showed.
        if ( ( m_SelectedTilePos.X >= 0 )
        &&   ( m_SelectedTilePos.Y >= 0 )
        &&   ( m_SelectedTilePos.X < m_CurrentMap.Tiles.Width )
        &&   ( m_SelectedTilePos.Y < m_CurrentMap.Tiles.Height )
        &&   ( m_ToolMode != ToolMode.MARKER )
        &&   ( m_ToolMode != ToolMode.ENTITY )
        &&   ( m_ToolMode != ToolMode.SELECT ) )
        {
          // Use the actual tile at that map cell to size the outline so a
          // 2x2 tile shows a 2-cell-wide highlight.
          int cw = 1, ch = 1;
          int idx = m_CurrentMap.Tiles[m_SelectedTilePos.X, m_SelectedTilePos.Y];
          if ( ( idx >= 0 )
          &&   ( idx < m_MapProject.Tiles.Count ) )
          {
            GetTileCellFootprint( m_MapProject.Tiles[idx], out cw, out ch );
          }
          drawHighlightAt( m_SelectedTilePos.X, m_SelectedTilePos.Y, cw, ch, highlightColor, true );
        }
      }


      // Selection rendering only runs in SELECT mode, but the display-filter
      // pipeline below needs to run in ALL modes — so this is a guarded block,
      // not the early-return it used to be.
      if ( ( m_CurrentMap != null )
      &&   ( m_ToolMode == ToolMode.SELECT ) )
      {

      // draw selection
      uint    selectionColor = Core.Settings.FGColor( ColorableElement.SELECTION_FRAME );
      for ( int x = 0; x < m_CurrentMap.Tiles.Width; ++x )
      {
        for ( int y = 0; y < m_CurrentMap.Tiles.Height; ++y )
        {
          if ( m_SelectedTiles[x, y] )
          {
            int  sourceX1 = renderOffsetX + ( x - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
            int  sourceX2 = renderOffsetX + ( x + 1 - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
            int  sourceY1 = renderOffsetY + ( y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;
            int  sourceY2 = renderOffsetY + ( y + 1 - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;

            int  sx1 = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX1, sourceWidth, targetWidth ) ) );
            int  sx2 = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX2, sourceWidth, targetWidth ) - 1 ) );
            int  sy1 = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY1, sourceHeight, targetHeight ) ) );
            int  sy2 = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY2, sourceHeight, targetHeight ) - 1 ) );

            if ( sx2 < sx1 )
            {
              sx2 = sx1;
            }
            if ( sy2 < sy1 )
            {
              sy2 = sy1;
            }

            if ( ( y == 0 )
            ||   ( !m_SelectedTiles[x, y - 1] ) )
            {
              for ( int i = sx1; i <= sx2; ++i )
              {
                TargetBuffer.SetPixel( i, sy1, selectionColor );
              }
            }
            if ( ( y == m_SelectedTiles.GetUpperBound( 1 ) )
            ||   ( !m_SelectedTiles[x, y + 1] ) )
            {
              for ( int i = sx1; i <= sx2; ++i )
              {
                TargetBuffer.SetPixel( i, sy2, selectionColor );
              }
            }
            if ( ( x == 0 )
            ||   ( !m_SelectedTiles[x - 1, y] ) )
            {
              for ( int i = sy1; i <= sy2; ++i )
              {
                TargetBuffer.SetPixel( sx1, i, selectionColor );
              }
            }
            if ( ( x == m_SelectedTiles.GetUpperBound( 0 ) )
            ||   ( !m_SelectedTiles[x + 1, y] ) )
            {
              for ( int i = sy1; i <= sy2; ++i )
              {
                TargetBuffer.SetPixel( sx2, i, selectionColor );
              }
            }
          }
        }
      }

      // current dragged selection
      if ( ( m_ToolMode == ToolMode.SELECT )
      &&   ( m_LastDragEndPos.X != -1 ) )
      {
        System.Drawing.Point    o1, o2;

        CalcRect( m_DragStartPos, m_LastDragEndPos, out o1, out o2 );

        int sourceX = renderOffsetX + ( o1.X - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
        int sourceY = renderOffsetY + ( o1.Y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;
        int sourceW = ( o2.X - o1.X + 1 ) * m_CurrentMap.TileSpacingX * 8;
        int sourceH = ( o2.Y - o1.Y + 1 ) * m_CurrentMap.TileSpacingY * 8;

        int sourceX2 = sourceX + sourceW;
        int sourceY2 = sourceY + sourceH;

        int targetX = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX, sourceWidth, targetWidth ) ) );
        int targetY = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY, sourceHeight, targetHeight ) ) );
        int targetX2 = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX2, sourceWidth, targetWidth ) - 1 ) );
        int targetY2 = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY2, sourceHeight, targetHeight ) - 1 ) );
        int targetW = Math.Max( 1, targetX2 - targetX + 1 );
        int targetH = Math.Max( 1, targetY2 - targetY + 1 );

        TargetBuffer.Rectangle( targetX,
                                targetY,
                                targetW,
                                targetH,
                                selectionColor );
      }

      } // end of SELECT-mode selection-rendering guard

      // CRT display-filter pipeline runs LAST so it sees the fully composited
      // map (tiles + entities + grid + markers + selection). Filters operate
      // in target-pixel space, so we also convert the map's source-pixel
      // region into target pixels and hand the filter chain a context that
      // lets each filter pin its effect to the scaled map rather than the
      // whole TargetBuffer (otherwise scanlines would paint over chrome).
      var pipeline = ( Core != null ) && ( Core.Settings != null )
                     ? Core.Settings.DisplayFilters : null;
      // Session-only bypass: the "Filter enabled" checkbox on the Map tab is
      // a quick kill-switch. When unchecked, we skip the pipeline entirely
      // even if individual filters are configured and marked Enabled in the
      // settings dialog. Not persisted across app restarts.
      bool filtersAllowed = ( checkFilterEnabled != null )
                            && checkFilterEnabled.Checked;
      if ( ( filtersAllowed )
      &&   ( pipeline != null )
      &&   ( pipeline.HasAnyEnabled )
      &&   ( m_CurrentMap != null ) )
      {
        int filterOffsetX = m_CurEditorOffsetX;
        int filterOffsetY = m_CurEditorOffsetY;
        long sourceMapPixelWidth  = (long)( m_CurrentMap.Tiles.Width  - filterOffsetX ) * m_CurrentMap.TileSpacingX * 8;
        long sourceMapPixelHeight = (long)( m_CurrentMap.Tiles.Height - filterOffsetY ) * m_CurrentMap.TileSpacingY * 8;
        int  sourceMapEndX = renderOffsetX + (int)sourceMapPixelWidth;
        int  sourceMapEndY = renderOffsetY + (int)sourceMapPixelHeight;

        int  targetMapX    = Math.Max( 0, Math.Min( targetWidth,  ScaleCoordCeil( renderOffsetX, sourceWidth,  targetWidth  ) ) );
        int  targetMapY    = Math.Max( 0, Math.Min( targetHeight, ScaleCoordCeil( renderOffsetY, sourceHeight, targetHeight ) ) );
        int  targetMapEndX = Math.Max( 0, Math.Min( targetWidth,  ScaleCoordCeil( sourceMapEndX, sourceWidth,  targetWidth  ) ) );
        int  targetMapEndY = Math.Max( 0, Math.Min( targetHeight, ScaleCoordCeil( sourceMapEndY, sourceHeight, targetHeight ) ) );

        var ctx = new CustomRenderer.DisplayFilters.FilterContext
        {
          RenderOffsetX  = targetMapX,
          RenderOffsetY  = targetMapY,
          MapPixelWidth  = Math.Max( 0, targetMapEndX - targetMapX ),
          MapPixelHeight = Math.Max( 0, targetMapEndY - targetMapY ),
          // The "source" for filter purposes is the unscaled map, not the
          // DisplayPage — that's what drives scanline alignment and column
          // phase at any zoom level.
          SourceWidth  = (int)sourceMapPixelWidth,
          SourceHeight = (int)sourceMapPixelHeight,
        };
        pipeline.Apply( TargetBuffer, ctx );
      }
    }



    void pictureEditor_MouseWheel( object sender, MouseEventArgs e )
    {
      // Ctrl+wheel zooms (matches the common IDE/browser convention). Plain
      // wheel scrolls vertically, Shift+wheel scrolls horizontally. Delta is
      // 120 per detent on a standard wheel.
      if ( ( Control.ModifierKeys & Keys.Control ) != 0 )
      {
        int notches = e.Delta / 120;
        if ( notches == 0 )
        {
          return;
        }
        // SetMapZoomPercent clamps to [MapZoomMinPercent, MapZoomMaxPercent]
        // so no extra bounds check is needed here.
        SetMapZoomPercent( m_MapZoomPercent + notches * MapZoomWheelStepPercent );
        return;
      }

      int numberOfLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

      DecentForms.ScrollBar scrollbarToUse = mapVScroll;
      if ( ( Control.ModifierKeys & Keys.Shift ) != 0 )
      {
        scrollbarToUse = mapHScroll;
      }
      if ( scrollbarToUse.Enabled )
      {
        int     oldValue = scrollbarToUse.Value;
        int     newValue = oldValue - numberOfLinesToMove;
        if ( newValue < 0 )
        {
          newValue = 0;
        }
        if ( newValue > scrollbarToUse.Maximum )
        {
          newValue = scrollbarToUse.Maximum;
        }
        if ( oldValue != newValue )
        {
          scrollbarToUse.Value = newValue;
          if ( scrollbarToUse == mapVScroll )
          {
            mapVScroll_Scroll( scrollbarToUse );
          }
          else
          {
            mapHScroll_Scroll( scrollbarToUse );
          }
        }
      }
    }



    protected override void OnEnter( EventArgs e )
    {
      base.OnEnter( e );
      RefreshMapTileList();
    }



    void MainForm_ApplicationEvent( RetroDevStudio.Types.ApplicationEvent Event )
    {
    }



    protected override void OnClosed( EventArgs e )
    {
      Core.MainForm.ApplicationEvent -= MainForm_ApplicationEvent;
      base.OnClosed( e );
    }



    void RebuildCharImage( int CharIndex )
    {
      Displayer.CharacterDisplayer.DisplayChar( m_MapProject.Charset,
                                                CharIndex, m_MapProject.Charset.Characters[CharIndex].Tile.Image, 0, 0,
                                                m_MapProject.Charset.Characters[CharIndex].Tile.CustomColor );

      if ( CharIndex < panelCharacters.Items.Count )
      {
        panelCharacters.Items[CharIndex].MemoryImage = m_MapProject.Charset.Characters[CharIndex].Tile.Image;
      }
    }



    void DrawCharImage( GR.Image.FastImage TargetImage, int X, int Y, byte Char, byte Color )
    {
      int bgColor = m_MapProject.BackgroundColor;
      int mColor1 = m_MapProject.MultiColor1;
      int mColor2 = m_MapProject.MultiColor2;
      int bgColor4 = m_MapProject.BGColor4;
      if ( m_CurrentMap != null )
      {
        if ( m_CurrentMap.AlternativeBackgroundColor != -1 )
        {
          bgColor = m_CurrentMap.AlternativeBackgroundColor;
        }
        if ( m_CurrentMap.AlternativeMultiColor1 != -1 )
        {
          mColor1 = m_CurrentMap.AlternativeMultiColor1;
        }
        if ( m_CurrentMap.AlternativeMultiColor2 != -1 )
        {
          mColor2 = m_CurrentMap.AlternativeMultiColor2;
        }
        if ( m_CurrentMap.AlternativeBGColor4 != -1 )
        {
          bgColor4 = m_CurrentMap.AlternativeBGColor4;
        }
      }
      var alternativeSettings = new Types.AlternativeColorSettings()
      {
        CustomColor     = Color,
        BackgroundColor = bgColor,
        MultiColor1     = mColor1,
        MultiColor2     = mColor2,
        BGColor4        = bgColor4
      };

      Displayer.CharacterDisplayer.DisplayChar( m_MapProject.Charset, Char, TargetImage, X, Y, alternativeSettings );
    }



    private new bool Modified
    {
      get
      {
        return base.Modified;
      }
      set
      {
        if ( value )
        {
          SetModified();
        }
        else
        {
          SetUnmodified();
        }
        saveCharsetProjectToolStripMenuItem.Enabled = Modified;
      }
    }



    private void comboColor_DrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;

      Core.Theming.DrawSingleColorComboBox( combo, e, m_MapProject.Charset.Colors.Palette );
    }



    /// <summary>
    /// Owner-draw for the Map Strings tab's per-line control-code combo.
    /// Item 0 = "None" (no leading control byte emitted for the line);
    /// items 1..16 = the 16 C64 colors (palette index = item index - 1).
    /// Renders the swatch shifted right of the index label so the layout
    /// matches the other color combos in the editor.
    /// </summary>
    private void comboMapStringLineControl_DrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;
      if ( e.Index < 0 ) return;

      if ( Core?.Theming != null )
        Core.Theming.DrawThemedBackground( e, combo );
      else
        e.DrawBackground();

      if ( e.Index == 0 )
      {
        // "None" entry — text only, no swatch.
        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( "None", e.Font, brush, e.Bounds.Left + 2, e.Bounds.Top + 1 );
        }
      }
      else
      {
        // Color rows. Combo index 1..16 → palette index 0..15. Same swatch
        // layout the theme uses for other single-color combos: index label
        // on the left, swatch fills the rest of the row.
        int paletteIdx = e.Index - 1;
        var pal = m_MapProject.Charset.Colors.Palette;

        int offset = (int)e.Graphics.MeasureString( "22", e.Font ).Width + 5 + 3;
        var itemRect = new System.Drawing.Rectangle(
          e.Bounds.Left + offset, e.Bounds.Top,
          e.Bounds.Width - offset, e.Bounds.Height );
        if ( paletteIdx >= 0
        &&   paletteIdx < pal.ColorBrushes.Length
        &&   pal.ColorBrushes[paletteIdx] != null )
        {
          e.Graphics.FillRectangle( pal.ColorBrushes[paletteIdx], itemRect );
        }
        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( combo.Items[e.Index].ToString(), e.Font, brush,
                                 e.Bounds.Left + 2, e.Bounds.Top + 1 );
        }
      }

      e.DrawFocusRectangle();
    }



    private void comboMulticolor_DrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;

      Core.Theming.DrawMultiColorComboBox( combo, e, m_MapProject.Charset.Colors.Palette );
    }



    private void pictureEditor_MouseDown( object sender, MouseEventArgs e )
    {
      pictureEditor.Focus();
      HandleMouseOnEditor( e.X, e.Y, e.Button );
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void pictureEditor_MouseUp( object sender, MouseEventArgs e )
    {
      if ( e.Button != MouseButtons.Left )
      {
        return;
      }
      // A left press + release with no drag in between is a click — select
      // the marker that was pressed (m_MarkerDrag is still None). A drag has
      // already moved or resized it, so there is nothing left to do.
      if ( ( m_PressedMarker != null )
      &&   ( m_MarkerDrag == MarkerDragKind.None ) )
      {
        SelectMarker( m_PressedMarker );
      }
      m_PressedMarker = null;
      m_PressedMarkerCell = new System.Drawing.Point( -1, -1 );
      m_MarkerDrag = MarkerDragKind.None;
    }



    private void CalcRect( System.Drawing.Point In1, System.Drawing.Point In2, out System.Drawing.Point P1, out System.Drawing.Point P2 )
    {
      P1 = new System.Drawing.Point();
      P2 = new System.Drawing.Point();

      if ( In1.X <= In2.X )
      {
        P1.X = In1.X;
        P2.X = In2.X;
      }
      else
      {
        P1.X = In2.X;
        P2.X = In1.X;
      }
      if ( In1.Y <= In2.Y )
      {
        P1.Y = In1.Y;
        P2.Y = In2.Y;
      }
      else
      {
        P1.Y = In2.Y;
        P2.Y = In1.Y;
      }
    }



    private void InsertFloatingSelection()
    {
      if ( m_FloatingSelection == null )
      {
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, m_MousePos.X + m_CurEditorOffsetX, m_MousePos.Y + m_CurEditorOffsetY, m_FloatingSelectionSize.Width, m_FloatingSelectionSize.Height ) );

      // When the clipboard payload included a per-character override
      // trailer (newer copies — see CopyToClipboard / PasteFromClipboard),
      // we restore those exact char values on paste. Without it we'd lose
      // the source's per-character coloring and instead stamp every
      // character of every pasted cell with the user's CURRENT placement
      // override — which is what was happening before this fix and made
      // copy/paste look "wrong-colored". Falling back to
      // ApplyPlacementColorOverride preserves legacy behaviour for any
      // pre-trailer clipboard payload that might still be in flight.
      bool                useCapturedOverrides = ( m_FloatingSelectionOverrides != null );
      int                 srcSpacingX = m_FloatingSelectionSourceSpacingX;
      int                 srcSpacingY = m_FloatingSelectionSourceSpacingY;
      int                 dstSpacingX = m_CurrentMap.TileSpacingX;
      int                 dstSpacingY = m_CurrentMap.TileSpacingY;
      int                 captureCharW = m_FloatingSelectionSize.Width  * srcSpacingX;

      for ( int j = 0; j < m_FloatingSelectionSize.Height; ++j )
      {
        for ( int i = 0; i < m_FloatingSelectionSize.Width; ++i )
        {
          var selectionChar = m_FloatingSelection[i + j * m_FloatingSelectionSize.Width];
          if ( selectionChar.first )
          {
            m_CurrentMap.Tiles[m_MousePos.X + m_CurEditorOffsetX + i, m_MousePos.Y + m_CurEditorOffsetY + j] = selectionChar.second;

            if ( useCapturedOverrides )
            {
              // Walk this tile cell's char footprint in the SOURCE grid
              // (i*srcSpacingX..i*srcSpacingX+srcSpacingX-1, etc.) and
              // write each value into the destination's char layer at
              // the corresponding offset within the destination cell. If
              // source and destination spacings disagree we cover the
              // overlap (min of the two) — beats either dropping the
              // overrides entirely or painting them past the cell edge.
              int copyW = ( srcSpacingX < dstSpacingX ) ? srcSpacingX : dstSpacingX;
              int copyH = ( srcSpacingY < dstSpacingY ) ? srcSpacingY : dstSpacingY;
              int dstCharBaseX = ( m_MousePos.X + m_CurEditorOffsetX + i ) * dstSpacingX;
              int dstCharBaseY = ( m_MousePos.Y + m_CurEditorOffsetY + j ) * dstSpacingY;
              for ( int dy = 0; dy < copyH; ++dy )
              {
                for ( int dx = 0; dx < copyW; ++dx )
                {
                  int srcCharX = i * srcSpacingX + dx;
                  int srcCharY = j * srcSpacingY + dy;
                  int v = m_FloatingSelectionOverrides[srcCharX + srcCharY * captureCharW];
                  int dstCharX = dstCharBaseX + dx;
                  int dstCharY = dstCharBaseY + dy;
                  if ( ( dstCharX >= 0 ) && ( dstCharY >= 0 )
                  &&   ( dstCharX < m_CurrentMap.TileColorOverrides.Width )
                  &&   ( dstCharY < m_CurrentMap.TileColorOverrides.Height ) )
                  {
                    m_CurrentMap.TileColorOverrides[dstCharX, dstCharY] = v;
                  }
                  // Parallel write for the blocked overrides. If the
                  // payload didn't include a blocked trailer (older
                  // copies), default to false — placing a tile resets
                  // blocked, mirroring the color-override path's
                  // ApplyPlacementColorOverride clear behavior.
                  bool blockedValue = false;
                  if ( m_FloatingSelectionBlocked != null )
                  {
                    blockedValue = m_FloatingSelectionBlocked[srcCharX + srcCharY * captureCharW];
                  }
                  if ( ( dstCharX >= 0 ) && ( dstCharY >= 0 )
                  &&   ( dstCharX < m_CurrentMap.CharBlockedOverrides.Width )
                  &&   ( dstCharY < m_CurrentMap.CharBlockedOverrides.Height ) )
                  {
                    m_CurrentMap.CharBlockedOverrides[dstCharX, dstCharY] = blockedValue;
                  }
                }
              }
            }
            else
            {
              // Legacy (no-trailer) paste: ApplyPlacementColorOverride
              // wipes both color AND blocked overrides for the placed
              // tile's footprint (per its definition), so the legacy
              // path is correct without parallel work here.
              ApplyPlacementColorOverride( m_MousePos.X + m_CurEditorOffsetX + i, m_MousePos.Y + m_CurEditorOffsetY + j );
            }

            DrawTile( ( m_MousePos.X + i ) * 8 * m_CurrentMap.TileSpacingX,
                      ( m_MousePos.Y + j ) * 8 * m_CurrentMap.TileSpacingY,
                      selectionChar.second );

            pictureEditor.DisplayPage.DrawTo( m_Image,
                                              ( m_MousePos.X + i ) * 8 * m_CurrentMap.TileSpacingX,
                                              ( m_MousePos.Y + j ) * 8 * m_CurrentMap.TileSpacingY,
                                              ( m_MousePos.X + i ) * 8 * m_CurrentMap.TileSpacingX,
                                              ( m_MousePos.Y + j ) * 8 * m_CurrentMap.TileSpacingY,
                                              8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
            pictureEditor.Invalidate( new System.Drawing.Rectangle( ( m_MousePos.X + i ) * 8 * m_CurrentMap.TileSpacingX,
                                                                    ( m_MousePos.Y + j ) * 8 * m_CurrentMap.TileSpacingY,
                                                                    8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY ) );
          }
        }
      }
      m_FloatingSelection = null;
      m_FloatingSelectionOverrides = null;
      m_FloatingSelectionBlocked = null;
      RecalcTileUsageInCurrentMap();
      Redraw();
      Modified = true;
    }



    /// <summary>
    /// Picks the tile index to place at map cell (mapX, mapY) when auto-tiling
    /// is active, choosing among the current editor tile's group members based
    /// on the four orthogonal neighbours — the group member that appears least
    /// among the neighbours wins, ties broken randomly. Returns
    /// m_CurrentEditorTile.Index unchanged when no group candidate is found.
    /// Caller must ensure m_CurrentEditorTile.GroupId != 0 (auto-tiling only
    /// applies to grouped tiles), mirroring the manual-draw gate. Extracted
    /// from the SINGLE_TILE draw path so the FILL tool can reuse the exact same
    /// rule — "filling behaves like drawing the cells by hand".
    /// </summary>
    private int PickAutoTileIndex( int mapX, int mapY )
    {
      int tileIndex = m_CurrentEditorTile.Index;

      // find neighbors
      var neighbors = new List<int>();
      if ( mapX > 0 )
      {
        neighbors.Add( m_CurrentMap.Tiles[mapX - 1, mapY] );
      }
      if ( mapX < m_CurrentMap.Tiles.Width - 1 )
      {
        neighbors.Add( m_CurrentMap.Tiles[mapX + 1, mapY] );
      }
      if ( mapY > 0 )
      {
        neighbors.Add( m_CurrentMap.Tiles[mapX, mapY - 1] );
      }
      if ( mapY < m_CurrentMap.Tiles.Height - 1 )
      {
        neighbors.Add( m_CurrentMap.Tiles[mapX, mapY + 1] );
      }

      // filter only group members
      var groupMembers = new List<int>();
      foreach ( var tile in m_MapProject.Tiles )
      {
        if ( tile.GroupId == m_CurrentEditorTile.GroupId )
        {
          groupMembers.Add( tile.Index );
        }
      }

      var neighboringGroupMembers = new Dictionary<int,int>();
      foreach ( int neighborIndex in neighbors )
      {
        // A neighbour cell can hold an out-of-range index (empty/corrupt
        // cell, or a "covered" cell carrying a stale value) — skip those
        // rather than index past the project tile list. For a well-formed
        // map every cell is a valid index, so this is a no-op there and the
        // chosen variant is identical to before the extraction.
        if ( ( neighborIndex < 0 )
        ||   ( neighborIndex >= m_MapProject.Tiles.Count ) )
        {
          continue;
        }
        if ( m_MapProject.Tiles[neighborIndex].GroupId == m_CurrentEditorTile.GroupId )
        {
          if ( !neighboringGroupMembers.ContainsKey( neighborIndex ) )
          {
            neighboringGroupMembers.Add( neighborIndex, 0 );
          }
          neighboringGroupMembers[neighborIndex]++;
        }
      }

      var possibleCandidates = new List<int>();
      if ( neighboringGroupMembers.Count == 0 )
      {
        // no neighbors from same group, pick any
        possibleCandidates.AddRange( groupMembers );
      }
      else
      {
        // find candidates with least occurrence
        int minOccurrence = int.MaxValue;
        foreach ( var member in groupMembers )
        {
          int occurrence = 0;
          if ( neighboringGroupMembers.ContainsKey( member ) )
          {
            occurrence = neighboringGroupMembers[member];
          }
          if ( occurrence < minOccurrence )
          {
            minOccurrence = occurrence;
            possibleCandidates.Clear();
            possibleCandidates.Add( member );
          }
          else if ( occurrence == minOccurrence )
          {
            possibleCandidates.Add( member );
          }
        }
      }

      if ( possibleCandidates.Count > 0 )
      {
        tileIndex = possibleCandidates[m_Random.Next( possibleCandidates.Count )];
      }
      return tileIndex;
    }



    /// <summary>
    /// Bucket fill, tile-by-tile (multi-cell aware). Starting at the clicked
    /// cell, replaces every 4-edge-adjacent tile whose index matches the
    /// clicked tile's index. Because a tile larger than one cell (2x2, 3x1, …)
    /// is stored only at its top-left anchor — the other footprint cells are
    /// "covered" and carry stale indices — a naive cell-by-cell flood breaks
    /// 4-connectivity across such tiles and stops short. We therefore resolve
    /// which tile owns each cell (the same coverage sweep RedrawMap /
    /// ReplaceColorContent use) and step the flood whole tiles at a time.
    ///
    /// When Auto-Tiling is on, each filled cell picks its own group variant
    /// from its CURRENT neighbours, exactly as if the cells were drawn by hand
    /// one after another (filled cells only — tiles surrounding the region are
    /// left untouched). The placement colour (which the "Lock color" toggle
    /// keeps from resetting) is stamped over each placed tile's full footprint
    /// via ApplyPlacementColorOverride.
    /// </summary>
    private void FillContent( int X, int Y )
    {
      if ( m_CurrentMap == null ) return;
      if ( m_CurrentEditorTile == null ) return;

      int tw = m_CurrentMap.Tiles.Width;
      int th = m_CurrentMap.Tiles.Height;
      // The map now scrolls past its own bounds (to place off-map markers),
      // so a fill click can land outside the tile grid — guard rather than
      // index out of range.
      if ( ( X < 0 ) || ( Y < 0 ) || ( X >= tw ) || ( Y >= th ) ) return;

      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );

      // Resolve, for every cell, the ANCHOR cell of the tile that visually
      // occupies it (y outer / x inner, first-claimer-wins — matches the
      // screen). Cells with no valid owning tile stay hasOwner==false so the
      // flood can't leak into them.
      var anchorX  = new int[tw, th];
      var anchorY  = new int[tw, th];
      var hasOwner = new bool[tw, th];
      var covered  = new bool[tw, th];
      for ( int y = 0; y < th; ++y )
      {
        for ( int x = 0; x < tw; ++x )
        {
          if ( covered[x, y] ) continue;
          int idx = m_CurrentMap.Tiles[x, y];
          if ( ( idx < 0 ) || ( idx >= m_MapProject.Tiles.Count ) ) continue;
          var tile = m_MapProject.Tiles[idx];

          int cw = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Width  / (float)spacingX ) );
          int ch = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Height / (float)spacingY ) );
          for ( int cy = 0; cy < ch; ++cy )
          {
            for ( int cx = 0; cx < cw; ++cx )
            {
              int fx = x + cx;
              int fy = y + cy;
              if ( ( fx < tw ) && ( fy < th ) )
              {
                covered[fx, fy]  = true;
                hasOwner[fx, fy] = true;
                anchorX[fx, fy]  = x;
                anchorY[fx, fy]  = y;
              }
            }
          }
        }
      }

      if ( !hasOwner[X, Y] ) return;    // clicked empty / invalid cell

      int startAnchorX = anchorX[X, Y];
      int startAnchorY = anchorY[X, Y];
      int tileToFill   = m_CurrentMap.Tiles[startAnchorX, startAnchorY];

      bool autoTile = ( checkAutoTiling != null )
                   && ( checkAutoTiling.Checked )
                   && ( m_CurrentEditorTile.GroupId != 0 );

      // No-op when the region is already the brush tile — unless auto-tiling,
      // where re-stamping the region with fresh group variants is the point
      // (mirrors drawing by hand over existing same-group tiles).
      if ( ( tileToFill == m_CurrentEditorTile.Index )
      &&   ( !autoTile ) )
      {
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, tw, th ) );

      // All tiles in the region share tileToFill's index, so its footprint
      // (in cells) is constant for the whole flood.
      var fillTile = m_MapProject.Tiles[tileToFill];
      int fillCW   = Math.Max( 1, (int)Math.Ceiling( fillTile.Chars.Width  / (float)spacingX ) );
      int fillCH   = Math.Max( 1, (int)Math.Ceiling( fillTile.Chars.Height / (float)spacingY ) );

      // Phase 1 — collect the anchors to fill by stepping tile-by-tile across
      // 4-edge-adjacent tiles whose index matches tileToFill. Membership is
      // decided on the ORIGINAL map (the anchor grid above), so phase 2's
      // replacements (which may rewrite indices when auto-tiling) cannot
      // perturb the region we walk here.
      var visited   = new bool[tw, th];
      var fillOrder = new List<System.Drawing.Point>();
      var queue     = new List<System.Drawing.Point>();
      queue.Add( new System.Drawing.Point( startAnchorX, startAnchorY ) );
      visited[startAnchorX, startAnchorY] = true;

      void TryStep( int cellX, int cellY )
      {
        if ( ( cellX < 0 ) || ( cellY < 0 ) || ( cellX >= tw ) || ( cellY >= th ) ) return;
        if ( !hasOwner[cellX, cellY] ) return;
        int nax = anchorX[cellX, cellY];
        int nay = anchorY[cellX, cellY];
        if ( visited[nax, nay] ) return;
        if ( m_CurrentMap.Tiles[nax, nay] != tileToFill ) return;
        visited[nax, nay] = true;
        queue.Add( new System.Drawing.Point( nax, nay ) );
      }

      while ( queue.Count != 0 )
      {
        System.Drawing.Point a = queue[queue.Count - 1];
        queue.RemoveAt( queue.Count - 1 );
        fillOrder.Add( a );

        for ( int j = 0; j < fillCH; ++j )
        {
          TryStep( a.X - 1,      a.Y + j );    // left edge
          TryStep( a.X + fillCW, a.Y + j );    // right edge
        }
        for ( int i = 0; i < fillCW; ++i )
        {
          TryStep( a.X + i, a.Y - 1 );         // top edge
          TryStep( a.X + i, a.Y + fillCH );    // bottom edge
        }
      }

      // Phase 2 — place tiles. Auto-tiling picks each cell's variant from its
      // current neighbours (evolving as we go, like hand-drawing); otherwise
      // the brush tile is placed as-is. Either way the placement colour is
      // applied over the full footprint.
      foreach ( var a in fillOrder )
      {
        int placeIndex = autoTile ? PickAutoTileIndex( a.X, a.Y ) : m_CurrentEditorTile.Index;
        m_CurrentMap.Tiles[a.X, a.Y] = placeIndex;
        ApplyPlacementColorOverride( a.X, a.Y );
      }

      Modified = true;
      RedrawMap();
      Redraw();
    }



    /// <summary>
    /// Color-replace flood fill, per CHARACTER. Starting at the clicked
    /// character, recolours every 4-connected character that currently shows
    /// the SAME colour, repainting each with the toolbar's selected placement
    /// colour (<see cref="m_TilePlacementColorOverride"/>). "Same colour" is
    /// the colour the user sees: a character's TileColorOverride when one is
    /// set (>= 0), otherwise the owning tile's intrinsic character colour —
    /// resolved here exactly the way RedrawMap resolves it (including tiles
    /// larger than one cell). The flood therefore crosses tile boundaries and
    /// tile sizes freely (1x1, 2x2, 3x1, ...) and stops only where the colour
    /// differs or there is no tile. Only the per-character colour overrides
    /// are written; tile indices and "blocked" overrides are left untouched.
    /// "Default" in the dropdown (override = -1) is a no-op.
    /// </summary>
    private void ReplaceColorContent( int CharX, int CharY )
    {
      if ( m_CurrentMap == null ) return;
      // "Default" placement colour = nothing to apply.
      if ( m_TilePlacementColorOverride < 0 ) return;

      int charW = m_CurrentMap.TileColorOverrides.Width;
      int charH = m_CurrentMap.TileColorOverrides.Height;
      if ( ( CharX < 0 ) || ( CharY < 0 ) || ( CharX >= charW ) || ( CharY >= charH ) )
      {
        return;
      }

      int newColor = m_TilePlacementColorOverride;

      // Build the per-character EFFECTIVE colour grid the way RedrawMap draws
      // it: a char's colour is its override when set (>= 0), else the owning
      // tile's intrinsic char colour. Characters not covered by any drawn tile
      // stay NO_CHAR so the flood can't leak into empty space. The tile sweep
      // mirrors RedrawMap (y outer / x inner, first-claimer coverage) so the
      // grid matches the screen, including tiles bigger than one cell.
      const int NO_CHAR = -2;
      var effective = new int[charW, charH];
      for ( int i = 0; i < charW; ++i )
      {
        for ( int j = 0; j < charH; ++j )
        {
          effective[i, j] = NO_CHAR;
        }
      }

      int tw = m_CurrentMap.Tiles.Width;
      int th = m_CurrentMap.Tiles.Height;
      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );
      var covered = new bool[tw, th];
      for ( int y = 0; y < th; ++y )
      {
        for ( int x = 0; x < tw; ++x )
        {
          if ( covered[x, y] ) continue;
          int idx = m_CurrentMap.Tiles[x, y];
          if ( ( idx < 0 ) || ( idx >= m_MapProject.Tiles.Count ) ) continue;
          var tile = m_MapProject.Tiles[idx];

          int baseX = x * spacingX;
          int baseY = y * spacingY;
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              int cx = baseX + i;
              int cy = baseY + j;
              if ( ( cx >= 0 ) && ( cy >= 0 ) && ( cx < charW ) && ( cy < charH ) )
              {
                int ov = m_CurrentMap.TileColorOverrides[cx, cy];
                effective[cx, cy] = ( ov >= 0 ) ? ov : tile.Chars[i, j].Color;
              }
            }
          }

          int cw = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Width  / (float)spacingX ) );
          int ch = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Height / (float)spacingY ) );
          for ( int cy = 0; cy < ch; ++cy )
          {
            for ( int cx = 0; cx < cw; ++cx )
            {
              if ( ( x + cx < tw ) && ( y + cy < th ) )
              {
                covered[x + cx, y + cy] = true;
              }
            }
          }
        }
      }

      int startColor = effective[CharX, CharY];
      if ( startColor == NO_CHAR ) return;     // clicked empty space — nothing to recolour
      if ( startColor == newColor ) return;    // already that colour — no-op

      // UndoMapTilesChange snapshots TileColorOverrides for the whole map, so
      // Ctrl+Z restores the prior colours.
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, tw, th ) );

      // 4-connected flood over characters whose effective colour == startColor.
      // Matching reads the precomputed grid (stable) while we mutate overrides.
      var visited = new bool[charW, charH];
      var queue = new List<System.Drawing.Point>();
      queue.Add( new System.Drawing.Point( CharX, CharY ) );
      visited[CharX, CharY] = true;

      void TryFloodChar( int nx, int ny )
      {
        if ( ( nx < 0 ) || ( ny < 0 ) || ( nx >= charW ) || ( ny >= charH ) ) return;
        if ( visited[nx, ny] ) return;
        if ( effective[nx, ny] != startColor ) return;
        visited[nx, ny] = true;
        queue.Add( new System.Drawing.Point( nx, ny ) );
      }

      while ( queue.Count != 0 )
      {
        System.Drawing.Point c = queue[queue.Count - 1];
        queue.RemoveAt( queue.Count - 1 );

        m_CurrentMap.TileColorOverrides[c.X, c.Y] = newColor;

        TryFloodChar( c.X - 1, c.Y );
        TryFloodChar( c.X + 1, c.Y );
        TryFloodChar( c.X, c.Y - 1 );
        TryFloodChar( c.X, c.Y + 1 );
      }

      Modified = true;
      RedrawMap();
      Redraw();
    }



    private void HandleMouseOnEditor( int X, int Y, MouseButtons Buttons )
    {
      if ( m_CurrentMap == null )
      {
        labelEditInfo.Text = "";
        return;
      }
      int     viewCharWidth = ViewCharWidth;
      int     viewCharHeight = ViewCharHeight;
      if ( ( pictureEditor.ClientRectangle.Width <= 0 )
      ||   ( pictureEditor.ClientRectangle.Height <= 0 )
      ||   ( pictureEditor.DisplayPage.Width == 0 )
      ||   ( pictureEditor.DisplayPage.Height == 0 ) )
      {
        return;
      }

      float   scaleX = pictureEditor.DisplayPage.Width / (float)pictureEditor.ClientRectangle.Width;
      float   scaleY = pictureEditor.DisplayPage.Height / (float)pictureEditor.ClientRectangle.Height;
      int     sourceX = (int)Math.Floor( X * scaleX );
      int     sourceY = (int)Math.Floor( Y * scaleY );

      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      // apply centering offset
      sourceX -= renderOffsetX;
      sourceY -= renderOffsetY;

      //sourceX = Math.Max( 0, Math.Min( pictureEditor.DisplayPage.Width - 1, sourceX ) );
      //sourceY = Math.Max( 0, Math.Min( pictureEditor.DisplayPage.Height - 1, sourceY ) );

      int     charX = sourceX / 8;
      int     charY = sourceY / 8;

      m_MousePos.X = charX / m_CurrentMap.TileSpacingX;
      m_MousePos.Y = charY / m_CurrentMap.TileSpacingY;
      if ( m_FloatingSelection != null )
      {
        if ( m_MousePos != m_FloatingSelectionPos )
        {
          m_FloatingSelectionPos = m_MousePos;
          Redraw();
          pictureEditor.Invalidate();
        }
      }

      int offsetX = m_CurEditorOffsetX;
      int offsetY = m_CurEditorOffsetY;

      if ( ( charX < 0 )
      ||   ( charX >= viewCharWidth )
      ||   ( charY < 0 )
      ||   ( charY >= viewCharHeight ) )
      {
        return;
      }

      int trueX = charX / m_CurrentMap.TileSpacingX;
      int trueY = charY / m_CurrentMap.TileSpacingY;

      if ( ( trueX + offsetX < 0 )
      ||   ( trueX + offsetX >= m_CurrentMap.Tiles.Width )
      ||   ( trueY + offsetY < 0 )
      ||   ( trueY + offsetY >= m_CurrentMap.Tiles.Height ) )
      {
        // Marker tool can place off-map (for global/non-level markers), so let
        // the click through. Tile-editing tools still require an in-bounds click.
        if ( m_ToolMode != ToolMode.MARKER )
        {
          return;
        }
      }

      if ( sourceX < 0 )
      {
        // outside!
        return;
      }
      if ( sourceY < 0 )
      {
        // outside!
        return;
      }

      int mapPos = trueX + offsetX + ( trueY + offsetY ) * m_CurrentMap.Tiles.Width;
      labelEditInfo.Text = "X: " + ( trueX + offsetX ).ToString() + " Y:" + ( trueY + offsetY ).ToString() + " Abs:" + mapPos.ToString() + "/$" + mapPos.ToString( "X2" );

      if ( ( Buttons & MouseButtons.Left ) == 0 )
      {
        m_MouseButtonReleased = true;
        m_LastPaintedPos = new System.Drawing.Point( -1, -1 );
        // Mouse-up ends any in-flight marker/entity drag. Cleared
        // unconditionally — cheap, and avoids a stuck-drag if the
        // selection got nulled out from underneath us mid-drag.
        m_PressedMarker = null;
        m_MarkerDrag = MarkerDragKind.None;
        m_DraggingSelectedEntity = false;
        // End any in-flight blocked-override drag stroke. The captured
        // write value isn't reset — it's only re-read on the next press.
        m_BlockedDragActive = false;

        switch ( m_ToolMode )
        {
          case ToolMode.RECTANGLE:
          case ToolMode.FILLED_RECTANGLE:
            if ( m_LastDragEndPos.X != -1 )
            {
              m_LastDragEndPos.X = -1;
              m_LastDragEndPos.Y = -1;

              System.Drawing.Point    p1, p2;

              CalcRect( m_DragStartPos, m_DragEndPos, out p1, out p2 );

              DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, p1.X, p1.Y, p2.X - p1.X + 1, p2.Y - p1.Y + 1 ) );

              if ( m_ToolMode == ToolMode.RECTANGLE )
              {
                for ( int x = p1.X; x <= p2.X; ++x )
                {
                  DrawTile( x - m_CurEditorOffsetX, p1.Y - m_CurEditorOffsetY, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
                  DrawTile( x - m_CurEditorOffsetX, p2.Y - m_CurEditorOffsetY, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
                  m_CurrentMap.Tiles[x, p1.Y] = m_CurrentEditorTile.Index;
                  m_CurrentMap.Tiles[x, p2.Y] = m_CurrentEditorTile.Index;
                  ApplyPlacementColorOverride( x, p1.Y );
                  ApplyPlacementColorOverride( x, p2.Y );

                  pictureEditor.DisplayPage.DrawTo( m_Image,
                                  renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( p1.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( p1.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
                  pictureEditor.DisplayPage.DrawTo( m_Image,
                                  renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( p2.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( p2.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
                }
                for ( int y = p1.Y + 1; y <= p2.Y - 1; ++y )
                {
                  DrawTile( p1.X - m_CurEditorOffsetX, y - m_CurEditorOffsetY, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
                  DrawTile( p2.X - m_CurEditorOffsetX, y - m_CurEditorOffsetY, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
                  m_CurrentMap.Tiles[p1.X, y] = m_CurrentEditorTile.Index;
                  m_CurrentMap.Tiles[p2.X, y] = m_CurrentEditorTile.Index;
                  ApplyPlacementColorOverride( p1.X, y );
                  ApplyPlacementColorOverride( p2.X, y );

                  pictureEditor.DisplayPage.DrawTo( m_Image,
                                  renderOffsetX + ( p1.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  renderOffsetX + ( p1.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
                  pictureEditor.DisplayPage.DrawTo( m_Image,
                                  renderOffsetX + ( p2.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  renderOffsetX + ( p2.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                  renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                  8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
                }
              }
              else
              {
                for ( int y = p1.Y; y <= p2.Y; ++y )
                {
                  for ( int x = p1.X; x <= p2.X; ++x )
                  {
                    DrawTile( x - m_CurEditorOffsetX, y - m_CurEditorOffsetY, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
                    m_CurrentMap.Tiles[x, y] = m_CurrentEditorTile.Index;
                    ApplyPlacementColorOverride( x, y );
                    pictureEditor.DisplayPage.DrawTo( m_Image,
                                    renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                    renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                    renderOffsetX + ( x - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                    renderOffsetY + ( y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                    8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
                  }
                }
              }
              pictureEditor.Invalidate();
              RecalcTileUsageInCurrentMap();
              Modified = true;
            }
            break;
          case ToolMode.SELECT:
            if ( m_LastDragEndPos.X != -1 )
            {
              m_LastDragEndPos.X = -1;
              m_LastDragEndPos.Y = -1;

              System.Drawing.Point    p1, p2;

              CalcRect( m_DragStartPos, m_DragEndPos, out p1, out p2 );

              bool shiftPressed = ( ( ModifierKeys & Keys.Shift ) == Keys.Shift );

              if ( ( !shiftPressed )
              && ( ( ModifierKeys & Keys.Control ) == Keys.None ) )
              {
                // not ctrl-Click, remove previous selection
                for ( int x = 0; x < m_CurrentMap.Tiles.Width; ++x )
                {
                  for ( int y = 0; y < m_CurrentMap.Tiles.Height; ++y )
                  {
                    m_SelectedTiles[x, y] = false;
                  }
                }
              }

              for ( int x = p1.X; x <= p2.X; ++x )
              {
                for ( int y = p1.Y; y <= p2.Y; ++y )
                {
                  if ( shiftPressed )
                  {
                    m_SelectedTiles[x, y] = false;
                  }
                  else
                  {
                    m_SelectedTiles[x, y] = true;
                  }
                }
              }
              pictureEditor.Invalidate();
              Redraw();
            }
            break;
        }
      }

      if ( ( Buttons & MouseButtons.Left ) != 0 )
      {
        // Read-only when viewing a revision: left-click can't paint, fill,
        // place markers/entities, or drop a floating selection. Right and
        // middle buttons (eyedropper, color-picker) still flow through.
        if ( m_IsViewingRevision )
        {
          return;
        }

        // Drag-to-move continuation for an already-grabbed marker/entity.
        // The drag was started by an earlier left-click that landed on the
        // selected instance (see the MARKER/ENTITY case branches below).
        // Each subsequent move re-fires HandleMouseOnEditor — we only
        // commit a position update when the tile cell actually changes,
        // which keeps redraws to once per cell crossing instead of once
        // per mouse pixel. Out-of-bounds cells are clamped to the map
        // (entities) / 0..255 (markers) at the call sites; here we simply
        // skip the update when the new cell is outside the legal range so
        // a drag that strays off-edge doesn't move the instance to bogus
        // coordinates.
        if ( m_DraggingSelectedEntity )
        {
          int newX = trueX + offsetX;
          int newY = trueY + offsetY;
          if ( ( m_SelectedEntity != null )
          &&   ( newX >= 0 ) && ( newY >= 0 )
          &&   ( newX < m_CurrentMap.Tiles.Width )
          &&   ( newY < m_CurrentMap.Tiles.Height )
          &&   ( ( m_SelectedEntity.X != newX ) || ( m_SelectedEntity.Y != newY ) ) )
          {
            m_SelectedEntity.X = newX;
            m_SelectedEntity.Y = newY;
            SetModified();
            RedrawMap();
            pictureEditor.Invalidate();
          }
          return;
        }
        if ( m_PressedMarker != null )
        {
          int cursorX = trueX + offsetX;
          int cursorY = trueY + offsetY;

          // The drag begins the moment the cursor leaves the cell the marker
          // was pressed in. Shift held at that point makes it a resize;
          // otherwise it is a move. The kind is locked for the whole drag.
          if ( ( m_MarkerDrag == MarkerDragKind.None )
          &&   ( ( cursorX != m_PressedMarkerCell.X ) || ( cursorY != m_PressedMarkerCell.Y ) ) )
          {
            DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
            SelectMarker( m_PressedMarker );
            m_MarkerDrag = ( ( ModifierKeys & Keys.Shift ) == Keys.Shift )
                           ? MarkerDragKind.Resize
                           : MarkerDragKind.Move;
          }

          if ( m_MarkerDrag == MarkerDragKind.Move )
          {
            // A marker may be moved anywhere in the 0..255 coordinate range,
            // including past the map's width/height — that is how the user
            // positions non-interactive markers outside the map. (Resize is
            // still capped so a marker's footprint stays on the map.)
            int newX = Math.Max( 0, Math.Min( cursorX, 255 ) );
            int newY = Math.Max( 0, Math.Min( cursorY, 255 ) );
            if ( ( ( m_PressedMarker.X != newX ) || ( m_PressedMarker.Y != newY ) )
            &&   ( !MarkerFootprintOverlaps( newX, newY, m_PressedMarker.Width, m_PressedMarker.Height, m_PressedMarker ) ) )
            {
              m_PressedMarker.X = newX;
              m_PressedMarker.Y = newY;
              SetModified();
              RedrawMap();
              pictureEditor.Invalidate();
              UpdateMarkerOutOfBoundsLabel();
            }
          }
          else if ( m_MarkerDrag == MarkerDragKind.Resize )
          {
            // Stretch the footprint so its bottom-right corner reaches the
            // cursor cell; the top-left origin stays put. A cursor left of /
            // above the origin would mean a zero/negative size — ignore it.
            if ( ( cursorX >= m_PressedMarker.X )
            &&   ( cursorY >= m_PressedMarker.Y ) )
            {
              int newW = cursorX - m_PressedMarker.X + 1;
              int newH = cursorY - m_PressedMarker.Y + 1;
              // Footprint must stay on the map.
              if ( m_PressedMarker.X + newW > m_CurrentMap.Tiles.Width )
              {
                newW = m_CurrentMap.Tiles.Width - m_PressedMarker.X;
              }
              if ( m_PressedMarker.Y + newH > m_CurrentMap.Tiles.Height )
              {
                newH = m_CurrentMap.Tiles.Height - m_PressedMarker.Y;
              }
              if ( newW < 1 ) newW = 1;
              if ( newH < 1 ) newH = 1;
              if ( ( ( m_PressedMarker.Width != newW ) || ( m_PressedMarker.Height != newH ) )
              &&   ( !MarkerFootprintOverlaps( m_PressedMarker.X, m_PressedMarker.Y, newW, newH, m_PressedMarker ) ) )
              {
                m_PressedMarker.Width = newW;
                m_PressedMarker.Height = newH;
                SetModified();
                RedrawMap();
                pictureEditor.Invalidate();
              }
            }
          }
          return;
        }

        if ( m_FloatingSelection != null )
        {
          if ( m_MouseButtonReleased )
          {
            InsertFloatingSelection();
            m_MouseButtonReleased = false;
          }
          return;
        }

        // Ctrl+left-click (without Shift) writes the placement-override
        // colour into the clicked CHARACTER's slot in TileColorOverrides
        // — recolouring just that one char without changing which tile
        // sits there or any other char of that tile. No-op when the
        // combo is on "Default" (m_TilePlacementColorOverride < 0).
        // Ctrl+drag colours multiple chars under one undo entry,
        // mirroring the existing Shift-blank-click drag pattern. Falls
        // through to tool-mode handling only when Ctrl isn't held.
        bool ctrlPaintColor = ( ( Control.ModifierKeys & Keys.Control ) == Keys.Control )
                           && ( ( Control.ModifierKeys & Keys.Shift ) != Keys.Shift )
                           && ( m_TilePlacementColorOverride >= 0 )
                           && ( m_CurrentMap != null );
        if ( ctrlPaintColor )
        {
          // charX, charY are visible-char indices in the DisplayPage;
          // offsetX, offsetY are scroll offsets in TILES. Convert both
          // to absolute map char coords.
          int mapCharX = charX + offsetX * m_CurrentMap.TileSpacingX;
          int mapCharY = charY + offsetY * m_CurrentMap.TileSpacingY;
          if ( ( mapCharX >= 0 )
          &&   ( mapCharY >= 0 )
          &&   ( mapCharX < m_CurrentMap.TileColorOverrides.Width )
          &&   ( mapCharY < m_CurrentMap.TileColorOverrides.Height )
          &&   ( m_CurrentMap.TileColorOverrides[mapCharX, mapCharY] != m_TilePlacementColorOverride ) )
          {
            // First cell of a drag stroke gets one whole-map undo entry,
            // same as the SINGLE_TILE drag below — one Ctrl+Z rewinds the
            // entire stroke.
            if ( m_MouseButtonReleased )
            {
              m_MouseButtonReleased = false;
              DocumentInfo.UndoManager.AddUndoTask(
                new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0,
                                             m_CurrentMap.Tiles.Width,
                                             m_CurrentMap.Tiles.Height ) );
            }
            m_CurrentMap.TileColorOverrides[mapCharX, mapCharY] = m_TilePlacementColorOverride;
            SetModified();
            // The change is a single character; redraw the parent tile
            // (UpdateArea takes tile coords + tile counts) so the
            // surrounding chars repaint with their unchanged values too.
            int parentTileX = mapCharX / m_CurrentMap.TileSpacingX;
            int parentTileY = mapCharY / m_CurrentMap.TileSpacingY;
            UpdateArea( parentTileX, parentTileY, 1, 1 );
          }
          return;
        }

        switch ( m_ToolMode )
        {
          case ToolMode.SINGLE_TILE:
            if ( m_CurrentEditorTile != null )
            {
              int     tileIndex = m_CurrentEditorTile.Index;
              System.Drawing.Point currentPos = new System.Drawing.Point( trueX + offsetX, trueY + offsetY );

              // Shift+left-click overrides both the placed tile AND the
              // color override with the user-configured "blank" pair, and
              // skips auto-tiling — the gesture is meant to wipe a cell
              // back to a known empty state, not extend a smart pattern.
              // We swap m_TilePlacementColorOverride for the duration of
              // this single placement (rather than threading a parameter
              // through DrawTile / ApplyPlacementColorOverride / the cache
              // copy) and restore it at the end of the case.
              bool shiftBlankClick = ( ( Control.ModifierKeys & Keys.Shift ) == Keys.Shift )
                                  && ( m_MapProject != null )
                                  && ( m_MapProject.Tiles.Count > 0 );
              int  savedPlacementColor = m_TilePlacementColorOverride;
              if ( shiftBlankClick )
              {
                tileIndex = ResolveShiftClickBlankTileIndex();
                m_TilePlacementColorOverride = m_MapProject.ShiftClickBlankColor;
              }

              if ( ( !shiftBlankClick )
              &&   ( checkAutoTiling.Checked )
              &&   ( m_CurrentEditorTile.GroupId != 0 ) )
              {
                if ( currentPos == m_LastPaintedPos )
                {
                   // same pos, assume same result
                   return;
                }
                // auto-tiling with group: pick the variant from neighbours.
                // Shared with the FILL tool via PickAutoTileIndex so both
                // paths apply the identical rule.
                tileIndex = PickAutoTileIndex( trueX + offsetX, trueY + offsetY );
                m_LastPaintedPos = currentPos;
              }

              // The "skip if unchanged" fast-path needs to compare the
              // ENTIRE char footprint the tile actually OCCUPIES, not
              // just the top-left char and not just spacingX × spacingY.
              // When TileSpacing < Tile.Chars dimensions (e.g. spacing=1
              // with a 2x2 tile) the tile renders 4 chars but spacing²
              // would only check 1 — a re-place with matching top-left
              // would skip and leave the other 3 chars carrying stale
              // overrides from a prior placement. Use max(spacing, Chars)
              // so we cover both the slot and the rendered cells.
              bool footprintMatchesOverride = true;
              {
                int fpFootprintX = m_CurrentMap.TileSpacingX;
                int fpFootprintY = m_CurrentMap.TileSpacingY;
                if ( ( tileIndex >= 0 ) && ( tileIndex < m_MapProject.Tiles.Count ) )
                {
                  var fpTile = m_MapProject.Tiles[tileIndex];
                  if ( fpTile.Chars.Width  > fpFootprintX ) fpFootprintX = fpTile.Chars.Width;
                  if ( fpTile.Chars.Height > fpFootprintY ) fpFootprintY = fpTile.Chars.Height;
                }
                int fpBaseX = ( trueX + offsetX ) * m_CurrentMap.TileSpacingX;
                int fpBaseY = ( trueY + offsetY ) * m_CurrentMap.TileSpacingY;
                int fpW     = m_CurrentMap.TileColorOverrides.Width;
                int fpH     = m_CurrentMap.TileColorOverrides.Height;
                for ( int dy = 0; dy < fpFootprintY && footprintMatchesOverride; ++dy )
                {
                  for ( int dx = 0; dx < fpFootprintX; ++dx )
                  {
                    int cx = fpBaseX + dx;
                    int cy = fpBaseY + dy;
                    int cur = ( cx >= 0 && cy >= 0 && cx < fpW && cy < fpH )
                              ? m_CurrentMap.TileColorOverrides[cx, cy] : -1;
                    if ( cur != m_TilePlacementColorOverride )
                    {
                      footprintMatchesOverride = false;
                      break;
                    }
                  }
                }
              }
              if ( ( m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY] != tileIndex )
              ||   ( !footprintMatchesOverride ) )
              {
                if ( m_MouseButtonReleased )
                {
                  m_MouseButtonReleased = false;
                  DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );
                }
                m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY] = tileIndex;
                ApplyPlacementColorOverride( trueX + offsetX, trueY + offsetY );
                SetModified();
                //RecalcTileUsageInCurrentMap();

                DrawTile( trueX, trueY, tileIndex, m_TilePlacementColorOverride );
                // Copy the freshly-drawn tile from DisplayPage into m_Image
                // (the cache that Redraw() blits back over DisplayPage on
                // grid toggles, etc.). DrawTile placed the tile at
                // renderOffset+(...), so we MUST add renderOffset to the
                // src/dst coords here too — otherwise we'd sample empty
                // background pixels and the tile would silently vanish on
                // the next Redraw(). The RECTANGLE/FILLED_RECTANGLE branch
                // already gets this right; this branch (and a few others)
                // were missing it, hence "drag-paint a row of tiles, then
                // toggle the grid → only the first tile survives."
                pictureEditor.DisplayPage.DrawTo( m_Image,
                                renderOffsetX + trueX * 8 * m_CurrentMap.TileSpacingX,
                                renderOffsetY + trueY * 8 * m_CurrentMap.TileSpacingY,
                                renderOffsetX + trueX * 8 * m_CurrentMap.TileSpacingX,
                                renderOffsetY + trueY * 8 * m_CurrentMap.TileSpacingY,
                                m_MapProject.Tiles[tileIndex].Chars.Width * 8,
                                m_MapProject.Tiles[tileIndex].Chars.Height * 8 );

                // Use the PLACED tile's dimensions for invalidation, not
                // m_CurrentEditorTile's — under shift-blank-click those
                // can differ, and a too-small invalidate rect leaves
                // stale pixels around larger blank tiles.
                var placedTile = m_MapProject.Tiles[tileIndex];
                pictureEditor.Invalidate( new System.Drawing.Rectangle(
                                            renderOffsetX + ( trueX * m_CurrentMap.TileSpacingX ) * 8,
                                            renderOffsetY + ( trueY * m_CurrentMap.TileSpacingY ) * 8,
                                            placedTile.Chars.Width * 8,
                                            placedTile.Chars.Height * 8 ) );
              }
              if ( shiftBlankClick )
              {
                m_TilePlacementColorOverride = savedPlacementColor;
              }
            }
            break;
          case ToolMode.FILL:
            if ( m_MouseButtonReleased )
            {
              m_MouseButtonReleased = false;

              FillContent( trueX + m_CurEditorOffsetX, trueY + m_CurEditorOffsetY );
              RecalcTileUsageInCurrentMap();
            }
            break;
          case ToolMode.COLOR_REPLACE:
            if ( m_MouseButtonReleased )
            {
              m_MouseButtonReleased = false;

              // Pass the absolute MAP CHARACTER coords under the cursor (the
              // tool floods per character by colour). charX/charY are visible-
              // char indices; offset is the scroll in TILES — same conversion
              // the PASSABLE per-char tool uses.
              ReplaceColorContent( charX + m_CurEditorOffsetX * m_CurrentMap.TileSpacingX,
                                   charY + m_CurEditorOffsetY * m_CurrentMap.TileSpacingY );
            }
            break;
          case ToolMode.RECTANGLE:
          case ToolMode.FILLED_RECTANGLE:
            if ( m_MouseButtonReleased )
            {
              m_MouseButtonReleased = false;

              // first point
              m_DragStartPos.X = trueX + m_CurEditorOffsetX;
              m_DragStartPos.Y = trueY + m_CurEditorOffsetY;
              m_LastDragEndPos = new System.Drawing.Point( -1, -1 );
            }
            // draw other point
            m_DragEndPos.X = trueX + m_CurEditorOffsetX;
            m_DragEndPos.Y = trueY + m_CurEditorOffsetY;

            if ( m_DragEndPos != m_LastDragEndPos )
            {
              // restore background
              if ( m_LastDragEndPos.X != -1 )
              {
                System.Drawing.Point    o1, o2;

                CalcRect( m_DragStartPos, m_LastDragEndPos, out o1, out o2 );

                m_Image.DrawTo( pictureEditor.DisplayPage,
                                ( o1.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX, ( o1.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                ( o1.X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX, ( o1.Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                ( o2.X - o1.X + 1 ) * 8 * m_CurrentMap.TileSpacingX, ( o2.Y - o1.Y + 1 ) * 8 * m_CurrentMap.TileSpacingY );

                pictureEditor.Invalidate( new System.Drawing.Rectangle( o1.X * 8 * m_CurrentMap.TileSpacingX, o1.Y * 8 * m_CurrentMap.TileSpacingY, ( o2.X - o1.X + 1 ) * 8 * m_CurrentMap.TileSpacingX, ( o2.Y - o1.Y + 1 ) * 8 * m_CurrentMap.TileSpacingY ) );
              }
              m_LastDragEndPos = m_DragEndPos;

              System.Drawing.Point    p1, p2;

              CalcRect( m_DragStartPos, m_DragEndPos, out p1, out p2 );

              if ( m_ToolMode == ToolMode.RECTANGLE )
              {
                for ( int x = p1.X; x <= p2.X; ++x )
                {
                  DrawTile( x - m_CurEditorOffsetX, 
                            p1.Y - m_CurEditorOffsetY, 
                            m_CurrentEditorTile.Index );
                  DrawTile( x - m_CurEditorOffsetX,
                            p2.Y - m_CurEditorOffsetY,
                            m_CurrentEditorTile.Index );
                }
                for ( int y = p1.Y + 1; y <= p2.Y - 1; ++y )
                {
                  DrawTile( p1.X - m_CurEditorOffsetX,
                            y - m_CurEditorOffsetY,
                            m_CurrentEditorTile.Index );
                  DrawTile( p2.X - m_CurEditorOffsetX,
                            y - m_CurEditorOffsetY,
                            m_CurrentEditorTile.Index );
                }
              }
              else
              {
                for ( int x = p1.X; x <= p2.X; ++x )
                {
                  for ( int y = p1.Y; y <= p2.Y; ++y )
                  {
                    DrawTile( x - m_CurEditorOffsetX,
                              y - m_CurEditorOffsetY,
                              m_CurrentEditorTile.Index );
                  }
                }
              }
              pictureEditor.Invalidate( new System.Drawing.Rectangle( p1.X * m_CurrentMap.TileSpacingX, 
                                                                      p1.Y * m_CurrentMap.TileSpacingY,
                                                                      ( p2.X - p1.X + 1 ) * m_CurrentMap.TileSpacingX, 
                                                                      ( p2.Y - p1.Y + 1 ) * m_CurrentMap.TileSpacingY ) );
              
            }
            break;
          case ToolMode.SELECT:
            if ( m_MouseButtonReleased )
            {
              m_MouseButtonReleased = false;

              // first point
              m_DragStartPos.X = trueX + m_CurEditorOffsetX;
              m_DragStartPos.Y = trueY + m_CurEditorOffsetY;
              m_LastDragEndPos = new System.Drawing.Point( -1, -1 );
            }
            // draw other point
            m_DragEndPos.X = trueX + m_CurEditorOffsetX;
            m_DragEndPos.Y = trueY + m_CurEditorOffsetY;

            if ( m_DragEndPos != m_LastDragEndPos )
            {
              // restore background
              if ( m_LastDragEndPos.X != -1 )
              {
                System.Drawing.Point    o1, o2;

                CalcRect( m_DragStartPos, m_LastDragEndPos, out o1, out o2 );

                pictureEditor.Invalidate( new System.Drawing.Rectangle( o1.X * 8, o1.Y * 8, ( o2.X - o1.X + 1 ) * 8, ( o2.Y - o1.Y + 1 ) * 8 ) );
              }
              m_LastDragEndPos = m_DragEndPos;

              System.Drawing.Point    p1, p2;

              CalcRect( m_DragStartPos, m_DragEndPos, out p1, out p2 );

              pictureEditor.Invalidate( new System.Drawing.Rectangle( p1.X * 8, p1.Y * 8, ( p2.X - p1.X + 1 ) * 8, ( p2.Y - p1.Y + 1 ) * 8 ) );
              Redraw();

              // autoscroll at end of screen
              if ( ( trueX == 0 )
              &&   ( mapHScroll.Value > 0 ) )
              {
                mapHScroll.ScrollBy( -1 );
              }
              if ( ( trueX == ( pictureEditor.DisplayPage.Width / ( 8 * m_CurrentMap.TileSpacingX ) ) - 1 )
              &&   ( mapHScroll.Value < mapHScroll.Maximum ) )
              {
                mapHScroll.ScrollBy( 1 );
              }
              if ( ( trueY == 0 )
              &&   ( mapVScroll.Value > 0 ) )
              {
                mapVScroll.ScrollBy( -1 );
              }
              if ( ( trueY == ( pictureEditor.DisplayPage.Height / ( 8 * m_CurrentMap.TileSpacingY ) ) - 1 )
              &&   ( mapVScroll.Value < mapVScroll.Maximum ) )
              {
                mapVScroll.ScrollBy( 1 );
              }
            }
            break;

          case ToolMode.MARKER:
             if ( m_MouseButtonReleased )
             {
               m_MouseButtonReleased = false;

               int placeX = trueX + offsetX;
               int placeY = trueY + offsetY;

               // Left-press on an existing marker arms it: a plain release
               // selects it (pictureEditor_MouseUp), a drag moves it, and a
               // Shift+drag resizes it (see the marker drag block above).
               // Nothing is selected or moved on the press itself.
               var pressed = m_CurrentMap.Markers.FirstOrDefault( m => MarkerContainsPoint( m, placeX, placeY ) );
               if ( pressed != null )
               {
                 m_PressedMarker = pressed;
                 m_PressedMarkerCell = new System.Drawing.Point( placeX, placeY );
                 break;
               }

               // Empty cell. A plain click here just clears the marker
               // selection — markers are only ADDED on a Shift+click, so a
               // stray click can no longer drop markers by accident.
               if ( ( ModifierKeys & Keys.Shift ) != Keys.Shift )
               {
                 SelectMarker( null );
                 break;
               }

               // Shift + empty cell — place a new 1x1 marker. Markers can
               // live anywhere addressable by an u8, including off the map.
               if ( ( placeX < 0 )
               ||   ( placeY < 0 )
               ||   ( placeX > 255 )
               ||   ( placeY > 255 ) )
               {
                 break;
               }

               if ( m_CurrentMap.SelectedMarkerType != -1 )
               {
                 var type = m_MapProject.MarkerTypes.FirstOrDefault( t => t.ID == m_CurrentMap.SelectedMarkerType );
                 if ( type != null )
                 {
                   // Snapshot the marker list before adding so Ctrl+Z removes
                   // the just-placed marker.
                   DocumentInfo.UndoManager.AddUndoTask(
                     new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );

                   var marker = new MapProject.Marker();
                   marker.X = placeX;
                   marker.Y = placeY;
                   marker.Type = type.ID;
                   marker.Name = type.Name + " " + ( m_CurrentMap.Markers.Count + 1 );
                   marker.Value1 = (byte)editMarkerValue1.Value;
                   marker.Value2 = (byte)editMarkerValue2.Value;
                   marker.Enabled = checkMarkerDefaultEnabled.Checked;
                   marker.Triggered = checkMarkerDefaultTriggered.Checked;
                   marker.AutoDisableAfterTrigger = checkMarkerAutoDisable.Checked;
                   marker.GroupId = (byte)editMarkerGroupId.Value;
                   marker.LinkToID = (byte)editMarkerLinkToID.Value;
                   marker.LinkID = (byte)editMarkerLinkID.Value;
                   m_CurrentMap.Markers.Add( marker );
                   RedrawMap();
                   pictureEditor.Invalidate();
                   Modified = true;
                   UpdateMarkerOutOfBoundsLabel();
                 }
               }
             }
             break;

          case ToolMode.ENTITY:
             if ( m_MouseButtonReleased )
             {
               m_MouseButtonReleased = false;

               int placeX = trueX + offsetX;
               int placeY = trueY + offsetY;

               // Drag-to-move trigger: see the matching MARKER block above
               // for the rationale. Pressed on the selected entity =>
               // grab it for dragging; the continuation block above moves
               // it as the cursor crosses cells. Single undo entry covers
               // the whole drag.
               if ( ( m_SelectedEntity != null )
               &&   ( m_SelectedEntity.X == placeX )
               &&   ( m_SelectedEntity.Y == placeY ) )
               {
                 DocumentInfo.UndoManager.AddUndoTask(
                   new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
                 m_DraggingSelectedEntity = true;
                 break;
               }

               // Entities are in-map only (unlike markers which may float in 0..255).
               if ( ( placeX < 0 )
               ||   ( placeY < 0 )
               ||   ( placeX >= m_CurrentMap.Tiles.Width )
               ||   ( placeY >= m_CurrentMap.Tiles.Height ) )
               {
                 break;
               }

               if ( m_CurrentMap.SelectedEntityType != -1 )
               {
                 var type = m_MapProject.EntityTypes.FirstOrDefault( t => t.ID == m_CurrentMap.SelectedEntityType );
                 if ( type != null )
                 {
                   // Snapshot the entity list before mutating so Ctrl+Z restores
                   // the exact state (covers both "replaced" and "added" cases
                   // below without branching the undo logic).
                   DocumentInfo.UndoManager.AddUndoTask(
                     new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );

                   // Unique-per-tile: replace any entity already at this position.
                   var existing = m_CurrentMap.Entities.FirstOrDefault( en => en.X == placeX && en.Y == placeY );
                   if ( existing != null )
                   {
                     existing.Type = type.ID;
                     existing.Value1 = (byte)editEntityValue1Default.Value;
                     existing.Value2 = (byte)editEntityValue2Default.Value;
                     existing.Enabled = checkEntityDefaultEnabled.Checked;
                     existing.Triggered = checkEntityDefaultTriggered.Checked;
                   }
                   else
                   {
                     var entity = new MapProject.Entity();
                     entity.X = placeX;
                     entity.Y = placeY;
                     entity.Type = type.ID;
                     entity.Value1 = (byte)editEntityValue1Default.Value;
                     entity.Value2 = (byte)editEntityValue2Default.Value;
                     entity.Enabled = checkEntityDefaultEnabled.Checked;
                     entity.Triggered = checkEntityDefaultTriggered.Checked;
                     m_CurrentMap.Entities.Add( entity );
                   }
                   RedrawMap();
                   pictureEditor.Invalidate();
                   Modified = true;
                   UpdateEntityCountLabel();
                 }
               }
             }
             break;

          case ToolMode.PASSABLE:
             // Per-character "blocked" override editor. Bucket-toggle
             // drag: the first cell of the press decides set-or-clear,
             // and the entire stroke writes that one captured value
             // (m_BlockedDragWriteValue) — re-entering an already-
             // flipped cell during the same drag does NOT untoggle it.
             // One UndoMapCharBlockedChange entry per drag stroke.
             {
               // Revision-active maps are read-only — bail before any
               // mutation. Belt-and-suspenders: btnToolPassable should
               // be disabled in revision view, but if a stale tool
               // mode somehow runs, this guard catches it.
               if ( m_IsViewingRevision )
               {
                 break;
               }

               // charX, charY are the visible-char indices; offsetX/Y
               // are scroll offsets in TILES. Convert to absolute map
               // char coords — same conversion the Ctrl+click color-
               // paint path uses just above.
               int blkCharX = charX + offsetX * m_CurrentMap.TileSpacingX;
               int blkCharY = charY + offsetY * m_CurrentMap.TileSpacingY;
               if ( ( blkCharX < 0 )
               ||   ( blkCharY < 0 )
               ||   ( blkCharX >= m_CurrentMap.CharBlockedOverrides.Width )
               ||   ( blkCharY >= m_CurrentMap.CharBlockedOverrides.Height ) )
               {
                 break;
               }

               bool current = m_CurrentMap.CharBlockedOverrides[blkCharX, blkCharY];
               if ( m_MouseButtonReleased )
               {
                 // First press of the stroke — capture the operation
                 // and snapshot the whole layer for one-undo-per-stroke.
                 m_MouseButtonReleased    = false;
                 m_BlockedDragActive      = true;
                 m_BlockedDragWriteValue  = !current;
                 DocumentInfo.UndoManager.AddUndoTask(
                   new Undo.UndoMapCharBlockedChange(
                     this, m_CurrentMap, 0, 0,
                     m_CurrentMap.CharBlockedOverrides.Width,
                     m_CurrentMap.CharBlockedOverrides.Height ) );
               }
               if ( !m_BlockedDragActive )
               {
                 break;
               }
               if ( current == m_BlockedDragWriteValue )
               {
                 break;  // already at target value — no work
               }
               m_CurrentMap.CharBlockedOverrides[blkCharX, blkCharY] = m_BlockedDragWriteValue;
               SetModified();
               // Repaint the parent tile so the overlay refreshes for
               // this char (UpdateArea takes tile coords).
               int parentTileX = blkCharX / m_CurrentMap.TileSpacingX;
               int parentTileY = blkCharY / m_CurrentMap.TileSpacingY;
               UpdateArea( parentTileX, parentTileY, 1, 1 );
               pictureEditor.Invalidate();
             }
             break;
        }
      }

      if ( ( Buttons & MouseButtons.Middle ) != 0 )
      {
        // Middle-click eyedrops the EFFECTIVE color of the char under
        // the cursor — whatever the renderer would actually paint:
        //   1. If a per-char color override is set, use that.
        //   2. Otherwise fall back to the underlying tile's char color
        //      (tile.Chars[localCharX, localCharY].Color).
        // The dropdown is never set to "Default" by middle-click — that
        // would be a useless eyedrop (you can't paint with Default in any
        // visible way). If neither source can yield a color (sample
        // outside any tile / tile index out of range), the dropdown is
        // left untouched.
        int cellX = trueX + offsetX;
        int cellY = trueY + offsetY;
        if ( ( cellX >= 0 )
        &&   ( cellY >= 0 )
        &&   ( cellX < m_CurrentMap.Tiles.Width )
        &&   ( cellY < m_CurrentMap.Tiles.Height ) )
        {
          int sampleCharX = charX + offsetX * m_CurrentMap.TileSpacingX;
          int sampleCharY = charY + offsetY * m_CurrentMap.TileSpacingY;
          int sampledColor = -1;

          // Step 1: per-char override layer takes precedence when set.
          if ( ( sampleCharX >= 0 ) && ( sampleCharY >= 0 )
          &&   ( sampleCharX < m_CurrentMap.TileColorOverrides.Width )
          &&   ( sampleCharY < m_CurrentMap.TileColorOverrides.Height ) )
          {
            int ov = m_CurrentMap.TileColorOverrides[sampleCharX, sampleCharY];
            if ( ov >= 0 )
            {
              sampledColor = ov;
            }
          }

          // Step 2: no override — read the tile's own char color at the
          // local position within the tile. The local char coords are
          // sampleChar minus cell*spacing; bounds-check against the
          // tile's actual Chars footprint (a tile may render fewer
          // chars than spacing²).
          if ( sampledColor < 0 )
          {
            int tileIndex = m_CurrentMap.Tiles[cellX, cellY];
            if ( ( tileIndex >= 0 )
            &&   ( tileIndex < m_MapProject.Tiles.Count ) )
            {
              var tile = m_MapProject.Tiles[tileIndex];
              int localCharX = sampleCharX - cellX * m_CurrentMap.TileSpacingX;
              int localCharY = sampleCharY - cellY * m_CurrentMap.TileSpacingY;
              if ( ( localCharX >= 0 ) && ( localCharY >= 0 )
              &&   ( localCharX < tile.Chars.Width )
              &&   ( localCharY < tile.Chars.Height ) )
              {
                sampledColor = tile.Chars[localCharX, localCharY].Color;
              }
            }
          }

          // Nothing usable to eyedrop — leave the combo alone rather
          // than dropping to "Default" (the old behavior, explicitly
          // unwanted).
          if ( sampledColor < 0 )
          {
            return;
          }

          // Map to the combo's 1..16 color rows (index 0 is "Default",
          // never selected by this path).
          int targetIndex = sampledColor + 1;
          if ( ( comboTilePlacementColor != null )
          &&   ( targetIndex >= 0 )
          &&   ( targetIndex < comboTilePlacementColor.Items.Count )
          &&   ( comboTilePlacementColor.SelectedIndex != targetIndex ) )
          {
            // SelectedIndexChanged updates m_TilePlacementColorOverride,
            // so we don't need to write the field directly here. Suppress
            // the auto-apply-to-selected-tile path — the user middle-
            // clicked to SAMPLE a color, not to push it into the
            // currently-selected tile.
            m_SuppressTilePlacementColorAutoApply = true;
            try
            {
              comboTilePlacementColor.SelectedIndex = targetIndex;
            }
            finally
            {
              m_SuppressTilePlacementColorAutoApply = false;
            }
          }
        }
      }

      if ( ( Buttons & MouseButtons.Right ) != 0 )
      {
        if ( m_ToolMode == ToolMode.ENTITY )
        {
           int clickX = trueX + offsetX;
           int clickY = trueY + offsetY;
           if ( ( clickX >= 0 )
           &&   ( clickY >= 0 )
           &&   ( clickX < m_CurrentMap.Tiles.Width )
           &&   ( clickY < m_CurrentMap.Tiles.Height ) )
           {
             var entityHit = m_CurrentMap.Entities.FirstOrDefault( en => en.X == clickX && en.Y == clickY );
             if ( entityHit != null )
             {
               // SELECT — not remove. The toolbar controls now mirror this
               // entity's state, and further slider/checkbox/combo changes
               // will write straight into it until selection is cleared.
               // Deletion is a separate gesture via the "Delete ✕" button.
               SelectEntity( entityHit );
             }
             else
             {
               // Right-click on empty tile in ENTITY mode — drop any
               // existing selection so the toolbar reverts to "defaults
               // for next placement" mode.
               SelectEntity( null );
             }
           }
        }
        else if ( m_ToolMode == ToolMode.MARKER )
        {
           // Right-click does nothing in MARKER mode — marker selection is
           // left-click only. The branch is kept (empty) so a right-click
           // here doesn't fall through to the tile-eyedrop action below.
        }
        else if ( string.IsNullOrEmpty( m_MapProject.RightClickAction ) )
        {
          int cellX = trueX + offsetX;
          int cellY = trueY + offsetY;
          int tileIndex = m_CurrentMap.Tiles[cellX, cellY];
          if ( tileIndex < m_MapProject.Tiles.Count )
          {
            m_CurrentEditorTile = m_MapProject.Tiles[tileIndex];
            if ( ( tileIndex >= 0 )
            &&   ( tileIndex < comboTiles.Items.Count ) )
            {
              // Right-click on a map tile — eyedrops the tile into
              // the picker but must NOT reset the user's override
              // color choice. Suppress the reset that
              // comboTiles_SelectedIndexChanged otherwise applies
              // for direct picks from the tile list.
              m_SuppressTilePickerOverrideReset = true;
              try
              {
                comboTiles.SelectedIndex = tileIndex;
              }
              finally
              {
                m_SuppressTilePickerOverrideReset = false;
              }
            }
            // Remember which cell got picked so the user can press Delete
            // to clear it. The PostPaint highlight uses the same field.
            m_SelectedTilePos = new System.Drawing.Point( cellX, cellY );
            pictureEditor.Invalidate();
          }
        }
        else
        {
          // paint with selected tile — but only when the live map is
          // editable; right-click paint while viewing a revision would
          // mutate the snapshot, defeating the read-only contract.
          if ( m_IsViewingRevision )
          {
            return;
          }

          MapProject.Tile tileToUse = null;
          foreach ( var tile in m_MapProject.Tiles )
          {
            if ( tile.Name == m_MapProject.RightClickAction )
            {
              tileToUse = tile;
              break;
            }
          }
          if ( tileToUse != null )
          {
            DrawTile( trueX, trueY, tileToUse.Index, m_TilePlacementColorOverride );
            m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY] = tileToUse.Index;
            ApplyPlacementColorOverride( trueX + offsetX, trueY + offsetY );

            // Same renderOffset correction as the SINGLE_TILE drag-paint —
            // DrawTile placed the tile at renderOffset+(...) so the cache
            // copy must read/write from the same place.
            pictureEditor.DisplayPage.DrawTo( m_Image,
                            renderOffsetX + trueX * 8 * m_CurrentMap.TileSpacingX,
                            renderOffsetY + trueY * 8 * m_CurrentMap.TileSpacingY,
                            renderOffsetX + trueX * 8 * m_CurrentMap.TileSpacingX,
                            renderOffsetY + trueY * 8 * m_CurrentMap.TileSpacingY,
                            8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY );
            pictureEditor.Invalidate( new System.Drawing.Rectangle( ( trueX + offsetX ) * 8 * m_CurrentMap.TileSpacingX,
                                                                    ( trueY + offsetY ) * 8 * m_CurrentMap.TileSpacingY,
                                                                    8 * m_CurrentMap.TileSpacingX, 8 * m_CurrentMap.TileSpacingY ) );
            Modified = true;
          }
        }
      }
    }



    private void GetMapRenderOffsets( out int RenderOffsetX, out int RenderOffsetY )
    {
       RenderOffsetX = 0;
       RenderOffsetY = 0;
       if ( m_CurrentMap == null )
       {
         return;
       }
       long    mapPixelWidth = (long)m_CurrentMap.Tiles.Width * m_CurrentMap.TileSpacingX * 8;
       long    mapPixelHeight = (long)m_CurrentMap.Tiles.Height * m_CurrentMap.TileSpacingY * 8;

       // Center the map within the viewport on any axis where it's smaller
       // than the buffer. Scrolling is always enabled (off-map overhang), so
       // this centering gap can be active WHILE the user pans. That's fine:
       // the centering offset is a constant pixel shift applied on top of the
       // scroll offset in the render formula (renderOffset + (cell-scroll)*W),
       // and RedrawMap's cell-iteration + background fill both extend to cover
       // the columns/rows the gap exposes on the left/top, so panning a
       // centered map clips nothing. At scroll 0 the map sits centered; as you
       // scroll it pans toward the off-map overhang.
       if ( mapPixelWidth < pictureEditor.DisplayPage.Width )
       {
         RenderOffsetX = (int)( pictureEditor.DisplayPage.Width - mapPixelWidth ) / 2;
       }
       if ( mapPixelHeight < pictureEditor.DisplayPage.Height )
       {
         RenderOffsetY = (int)( pictureEditor.DisplayPage.Height - mapPixelHeight ) / 2;
       }
    }





    private void CharacterEditor_CharacterSelectionChanged( object sender, EventArgs e )
    {
      UpdateCharUsageCount();
    }



    private void UpdateCharUsageCount()
    {
      // Calculate usage of current character in map tiles
      int     usageCount = 0;
      int     charIndex = characterEditor.CurrentCharIndex;

      // cache occurrences per tile
      int[]   charOccurrencesInTile = new int[m_MapProject.Tiles.Count];

      if ( ( charIndex >= 0 )
      &&   ( charIndex < m_MapProject.Charset.Characters.Count ) )
      {
        for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
        {
          var tile = m_MapProject.Tiles[i];
          int tileCharCount = 0;
          for ( int y = 0; y < tile.Chars.Height; ++y )
          {
            for ( int x = 0; x < tile.Chars.Width; ++x )
            {
              if ( tile.Chars[x, y].Character == charIndex )
              {
                ++tileCharCount;
              }
            }
          }
          charOccurrencesInTile[i] = tileCharCount;
          usageCount += tileCharCount;
        }
      }
      characterEditor.CharacterUsageText = "Usage in tiles: " + usageCount;

      long mapUsageCount = 0;
      if ( ( charIndex >= 0 )
      &&   ( charIndex < m_MapProject.Charset.Characters.Count ) )
      {
        foreach ( var map in m_MapProject.Maps )
        {
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              int tileIndex = map.Tiles[x,y];
              if ( ( tileIndex >= 0 )
              &&   ( tileIndex < charOccurrencesInTile.Length ) )
              {
                 mapUsageCount += charOccurrencesInTile[tileIndex];
              }
            }
          }
        }
      }
      characterEditor.CharacterMapUsageText = "Usage in maps: " + mapUsageCount;
    }

    private void DrawTile( int trueX, int trueY, int TileIndex, int colorOverride = -1 )
    {
      if ( ( TileIndex < 0 )
      ||   ( TileIndex >= m_MapProject.Tiles.Count ) )
      {
        return;
      }
      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      // For per-char override lookup: where this tile sits in MAP char
      // coords. trueX is the visible TILE index; m_CurEditorOffsetX
      // converts that to the absolute map tile index; multiplying by
      // spacing gives the top-left character coord of the tile footprint.
      int mapCharBaseX = ( trueX + m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX;
      int mapCharBaseY = ( trueY + m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY;

      for ( int j = 0; j < m_MapProject.Tiles[TileIndex].Chars.Height; ++j )
      {
        for ( int i = 0; i < m_MapProject.Tiles[TileIndex].Chars.Width; ++i )
        {
          // Two paths for the colour:
          //  - colorOverride >= 0  → caller forced a single colour for
          //    this draw (preview overlays, floating selection drag).
          //    Apply uniformly to every char of the tile.
          //  - colorOverride == -1 → render from data: read the per-char
          //    override from m_CurrentMap.TileColorOverrides at the
          //    actual char coord; -1 there means "use the tile's
          //    intrinsic per-character colour."
          byte colorToUse;
          if ( colorOverride >= 0 )
          {
            colorToUse = (byte)colorOverride;
          }
          else
          {
            int charMapX = mapCharBaseX + i;
            int charMapY = mapCharBaseY + j;
            int charOverride = -1;
            if ( ( m_CurrentMap != null )
            &&   ( charMapX >= 0 ) && ( charMapY >= 0 )
            &&   ( charMapX < m_CurrentMap.TileColorOverrides.Width )
            &&   ( charMapY < m_CurrentMap.TileColorOverrides.Height ) )
            {
              charOverride = m_CurrentMap.TileColorOverrides[charMapX, charMapY];
            }
            colorToUse = ( charOverride >= 0 )
                         ? (byte)charOverride
                         : m_MapProject.Tiles[TileIndex].Chars[i, j].Color;
          }
          DrawCharImage( pictureEditor.DisplayPage,
                         renderOffsetX + ( trueX * m_CurrentMap.TileSpacingX + i ) * 8,
                         renderOffsetY + ( trueY * m_CurrentMap.TileSpacingY + j ) * 8,
                         m_MapProject.Tiles[TileIndex].Chars[i, j].Character,
                         colorToUse );
        }
      }
    }



    private void pictureEditor_MouseMove( object sender, MouseEventArgs e )
    {
      MouseButtons    buttons = e.Button;
      if ( !pictureEditor.Focused )
      {
        buttons = 0;
      }
      HandleMouseOnEditor( e.X, e.Y, buttons );
    }



    private void checkMulticolor_CheckedChanged( object sender, EventArgs e )
    {
    }



    private void RedrawMap()
    {
      uint    bgColor = (uint)m_MapProject.BackgroundColor;
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeBackgroundColor != -1 ) )
      {
        bgColor = (uint)m_CurrentMap.AlternativeBackgroundColor;
      }

      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      // clean background
      // Designer canvas color used to be a C64-palette index; it's now a
      // free-form ARGB color picked via ColorDialog (the canvas isn't part
      // of any export, so palette quantisation was never required).
      // GetDesignerBackgroundARGB resolves the legacy palette field for
      // older projects.
      pictureEditor.DisplayPage.Box( 0, 0, pictureEditor.DisplayPage.Width, pictureEditor.DisplayPage.Height, GetDesignerBackgroundARGB() );

      // draw map background (the map-colored backdrop behind the tiles).
      // Compute the map's true pixel rectangle in buffer coordinates so it
      // tracks BOTH the centering offset and the scroll offset: map cell (0,0)
      // sits at ( renderOffset - scroll * cellSize ). Clamp to the buffer.
      // This MUST match how the tile loop below positions cells, otherwise
      // the backdrop and the tiles disagree when a centered map is scrolled
      // (the bug that showed as the map being "cut" while scrolling).
      // Map's visible pixel rectangle in buffer coords. Reused below for both
      // the map backdrop and the entity-mode dim so they always agree.
      int mapVisX = 0, mapVisY = 0, mapVisW = 0, mapVisH = 0;
      if ( m_CurrentMap != null )
      {
        int bgCellWX = m_CurrentMap.TileSpacingX * 8;
        int bgCellWY = m_CurrentMap.TileSpacingY * 8;
        int mapLeftPx   = renderOffsetX - m_CurEditorOffsetX * bgCellWX;
        int mapTopPx    = renderOffsetY - m_CurEditorOffsetY * bgCellWY;
        int mapRightPx  = mapLeftPx + m_CurrentMap.Tiles.Width  * bgCellWX;
        int mapBottomPx = mapTopPx  + m_CurrentMap.Tiles.Height * bgCellWY;
        mapVisX = Math.Max( 0, mapLeftPx );
        mapVisY = Math.Max( 0, mapTopPx );
        mapVisW = Math.Min( pictureEditor.DisplayPage.Width,  mapRightPx )  - mapVisX;
        mapVisH = Math.Min( pictureEditor.DisplayPage.Height, mapBottomPx ) - mapVisY;
        if ( ( mapVisW > 0 ) && ( mapVisH > 0 ) )
        {
          pictureEditor.DisplayPage.Box( mapVisX, mapVisY, mapVisW, mapVisH, m_MapProject.Charset.Colors.Palette.ColorValues[bgColor] );
        }
      }

      if ( m_CurrentMap == null )
      {
        pictureEditor.Invalidate();
        return;
      }

      int offsetX = m_CurEditorOffsetX;
      int offsetY = m_CurEditorOffsetY;

      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );
      bool needsCoverage = false;
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        var tileToCheck = m_MapProject.Tiles[i];
        if ( ( tileToCheck.Chars.Width > spacingX )
        ||   ( tileToCheck.Chars.Height > spacingY ) )
        {
          needsCoverage = true;
          break;
        }
      }
      bool[,] coveredTiles = needsCoverage ? new bool[m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height] : null;

      // Cells render at pixel ( renderOffset + (cell - scroll) * cellSize ).
      // When the map is centered (renderOffset > 0) AND scrolled, columns/rows
      // to the LEFT/ABOVE the scroll position are still on-screen inside the
      // centering gap. Start iteration that far back (clamped to 0 so we never
      // index a negative cell) or those cells get skipped and the map looks
      // clipped as you scroll. x2/y2 stay generous; off-buffer cells are
      // clipped by the blit and the >= width/height guard below.
      int cellPixWidth  = 8 * m_CurrentMap.TileSpacingX;
      int cellPixHeight = 8 * m_CurrentMap.TileSpacingY;
      int x1 = Math.Max( 0, offsetX - ( renderOffsetX / cellPixWidth ) - 1 );
      int x2 = offsetX + ( pictureEditor.DisplayPage.Width / cellPixWidth ) + 1;
      int y1 = Math.Max( 0, offsetY - ( renderOffsetY / cellPixHeight ) - 1 );
      int y2 = offsetY + ( pictureEditor.DisplayPage.Height / cellPixHeight ) + 1;

      for ( int y = y1; y <= y2; ++y )
      {
        for ( int x = x1; x <= x2; ++x )
        {
          if ( ( x >= m_CurrentMap.Tiles.Width )
          ||   ( y >= m_CurrentMap.Tiles.Height ) )
          {
            continue;
          }
          if ( ( coveredTiles != null )
          &&   ( coveredTiles[x, y] ) )
          {
            continue;
          }
          int tileIndex = m_CurrentMap.Tiles[x, y];
          if ( tileIndex < m_MapProject.Tiles.Count )
          {
            // a real tile
            var tile = m_MapProject.Tiles[tileIndex];

            var alternativeSettings = new Types.AlternativeColorSettings()
            {
              BackgroundColor = ( m_CurrentMap.AlternativeBackgroundColor != -1 ) ? m_CurrentMap.AlternativeBackgroundColor : m_MapProject.BackgroundColor,
              MultiColor1     = ( m_CurrentMap.AlternativeMultiColor1 != -1 ) ? m_CurrentMap.AlternativeMultiColor1 : m_MapProject.MultiColor1,
              MultiColor2     = ( m_CurrentMap.AlternativeMultiColor2 != -1 ) ? m_CurrentMap.AlternativeMultiColor2 : m_MapProject.MultiColor2,
              BGColor4        = ( m_CurrentMap.AlternativeBGColor4 != -1 ) ? m_CurrentMap.AlternativeBGColor4 : m_MapProject.BGColor4,
              CharMode        = ( m_CurrentMap.AlternativeMode != TextCharMode.UNKNOWN ) ? m_CurrentMap.AlternativeMode : Lookup.TextCharModeFromTextMode( m_MapProject.Mode )
            };

            // Per-CHARACTER color override pulled from the map's
            // TileColorOverrides layer (now char-grid sized: one slot per
            // character cell on the map). -1 = use the tile's own colour
            // for that character; 0..15 paints just that character in
            // the given C64 colour.
            int tileCharBaseX = x * m_CurrentMap.TileSpacingX;
            int tileCharBaseY = y * m_CurrentMap.TileSpacingY;

            for ( int j = 0; j < tile.Chars.Height; ++j )
            {
              for ( int i = 0; i < tile.Chars.Width; ++i )
              {
                int charMapX = tileCharBaseX + i;
                int charMapY = tileCharBaseY + j;
                int charOverride = -1;
                if ( ( charMapX < m_CurrentMap.TileColorOverrides.Width )
                &&   ( charMapY < m_CurrentMap.TileColorOverrides.Height ) )
                {
                  charOverride = m_CurrentMap.TileColorOverrides[charMapX, charMapY];
                }
                alternativeSettings.CustomColor = ( charOverride >= 0 )
                                                  ? charOverride
                                                  : tile.Chars[i, j].Color;
                Displayer.CharacterDisplayer.DisplayChar( m_MapProject.Charset,
                                                          tile.Chars[i, j].Character,
                                                          pictureEditor.DisplayPage,
                                                          renderOffsetX + ( ( x - offsetX ) * m_CurrentMap.TileSpacingX + i ) * 8,
                                                          renderOffsetY + ( ( y - offsetY ) * m_CurrentMap.TileSpacingY + j ) * 8,
                                                          alternativeSettings );
              }
            }
            if ( coveredTiles != null )
            {
              int coverTilesX = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Width / (float)spacingX ) );
              int coverTilesY = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Height / (float)spacingY ) );

              for ( int cy = 0; cy < coverTilesY; ++cy )
              {
                for ( int cx = 0; cx < coverTilesX; ++cx )
                {
                  if ( ( x + cx < m_CurrentMap.Tiles.Width )
                  &&   ( y + cy < m_CurrentMap.Tiles.Height ) )
                  {
                    coveredTiles[x + cx, y + cy] = true;
                  }
                }
              }
            }
          }
        }
      }
      // Dim the map tiles BEFORE drawing entity overlays so that entities
      // render un-dimmed on top (the user wants the placement indicators to
      // pop against a muted background). MARKER-mode dimming still happens in
      // PictureEditor_PostPaint — there's nothing drawn on top of that one
      // that we need to protect. Working at source resolution here is also
      // cheaper than iterating the scaled TargetBuffer.
      if ( ( m_ToolMode == ToolMode.ENTITY )
      &&   ( m_CurrentMap.MarkerDimOpacity < 100 ) )
      {
        int opacity = m_CurrentMap.MarkerDimOpacity;
        // Dim exactly the map's visible rect (centering + scroll aware),
        // computed once above and shared with the backdrop fill.
        int dimEndX = mapVisX + mapVisW;
        int dimEndY = mapVisY + mapVisH;
        for ( int y = mapVisY; y < dimEndY; ++y )
        {
          for ( int x = mapVisX; x < dimEndX; ++x )
          {
            uint pixel = pictureEditor.DisplayPage.GetPixel( x, y );
            uint r = ( pixel & 0xff ) * (uint)opacity / 100;
            uint g = ( ( pixel >> 8 ) & 0xff ) * (uint)opacity / 100;
            uint b = ( ( pixel >> 16 ) & 0xff ) * (uint)opacity / 100;
            pictureEditor.DisplayPage.SetPixel( x, y, ( 0xff000000 | ( b << 16 ) | ( g << 8 ) | r ) );
          }
        }
      }

      // ====== Entities overlay (drawn on top of the tile layer) ======
      // Toggleable via the "Show Entities" checkbox; reuses the tile renderer
      // so multi-cell entity tiles render at full size on the entity's anchor.
      if ( ( checkShowEntities != null )
      &&   ( checkShowEntities.Checked ) )
      {
        var alternativeSettings = new Types.AlternativeColorSettings()
        {
          BackgroundColor = ( m_CurrentMap.AlternativeBackgroundColor != -1 ) ? m_CurrentMap.AlternativeBackgroundColor : m_MapProject.BackgroundColor,
          MultiColor1     = ( m_CurrentMap.AlternativeMultiColor1 != -1 ) ? m_CurrentMap.AlternativeMultiColor1 : m_MapProject.MultiColor1,
          MultiColor2     = ( m_CurrentMap.AlternativeMultiColor2 != -1 ) ? m_CurrentMap.AlternativeMultiColor2 : m_MapProject.MultiColor2,
          BGColor4        = ( m_CurrentMap.AlternativeBGColor4 != -1 ) ? m_CurrentMap.AlternativeBGColor4 : m_MapProject.BGColor4,
          CharMode        = ( m_CurrentMap.AlternativeMode != TextCharMode.UNKNOWN ) ? m_CurrentMap.AlternativeMode : Lookup.TextCharModeFromTextMode( m_MapProject.Mode )
        };

        foreach ( var entity in m_CurrentMap.Entities )
        {
          var type = m_MapProject.EntityTypes.FirstOrDefault( t => t.ID == entity.Type );
          if ( type == null ) continue;
          if ( ( type.TileIndex < 0 ) || ( type.TileIndex >= m_MapProject.Tiles.Count ) ) continue;

          int ex = entity.X - offsetX;
          int ey = entity.Y - offsetY;
          if ( ( ex < x1 - offsetX ) || ( ex > x2 - offsetX )
          ||   ( ey < y1 - offsetY ) || ( ey > y2 - offsetY ) ) continue;

          var tile = m_MapProject.Tiles[type.TileIndex];
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              alternativeSettings.CustomColor = tile.Chars[i, j].Color;
              Displayer.CharacterDisplayer.DisplayChar( m_MapProject.Charset,
                                                        tile.Chars[i, j].Character,
                                                        pictureEditor.DisplayPage,
                                                        renderOffsetX + ( ex * m_CurrentMap.TileSpacingX + i ) * 8,
                                                        renderOffsetY + ( ey * m_CurrentMap.TileSpacingY + j ) * 8,
                                                        alternativeSettings );
            }
          }
        }
      }

      pictureEditor.DisplayPage.DrawTo( m_Image, 0, 0, 0, 0, pictureEditor.DisplayPage.Width, pictureEditor.DisplayPage.Height );
      pictureEditor.Invalidate();
    }



    private void comboBackground_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject.BackgroundColor != comboTileBackground.SelectedIndex )
      {
        m_MapProject.BackgroundColor = comboTileBackground.SelectedIndex;
        m_MapProject.Charset.Colors.BackgroundColor = m_MapProject.BackgroundColor;
        for ( int i = 0; i < m_MapProject.Charset.TotalNumberOfCharacters; ++i )
        {
          RebuildCharImage( i );
        }
        Modified = true;
        RedrawMap();
        pictureEditor.Invalidate();
        panelCharacters.Invalidate();
      }
    }



    private void comboMulticolor1_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject.MultiColor1 != comboTileMulticolor1.SelectedIndex )
      {
        m_MapProject.MultiColor1 = comboTileMulticolor1.SelectedIndex;
        m_MapProject.Charset.Colors.MultiColor1 = m_MapProject.MultiColor1;
        SetModified();
        FullRebuild();
      }
    }



    private void comboMulticolor2_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject.MultiColor2 != comboTileMulticolor2.SelectedIndex )
      {
        m_MapProject.MultiColor2 = comboTileMulticolor2.SelectedIndex;
        m_MapProject.Charset.Colors.MultiColor2 = m_MapProject.MultiColor2;
        SetModified();
        FullRebuild();
      }
    }



    public void Clear()
    {
      DocumentInfo.DocumentFilename = "";

      m_MapProject.Clear();
    }



    public bool OpenProject( string File )
    {
      SuspendLayout();
      Clear();
      comboMaps.Items.Clear();
      comboMaps.Enabled = false;
      comboTiles.Items.Clear();

      GR.Memory.ByteBuffer projectFile = GR.IO.File.ReadAllBytes( File );
      if ( projectFile == null )
      {
        return false;
      }

      if ( !m_MapProject.ReadFromBuffer( projectFile ) )
      {
        return false;
      }
      RefreshMapTileList();

      // Upper-clamp StartMapIndex against the actual map count BEFORE
      // building the dropdown — the chunk reader can only clamp at >= 0
      // because it doesn't know the live map count yet. A corrupt or
      // hand-edited file pointing past the end falls back to 0 here.
      if ( ( m_MapProject.StartMapIndex < 0 )
      ||   ( m_MapProject.StartMapIndex >= m_MapProject.Maps.Count ) )
      {
        m_MapProject.StartMapIndex = 0;
      }

      int index = 0;
      comboMaps.BeginUpdate();
      foreach ( var map in m_MapProject.Maps )
      {
        comboMaps.Items.Add( new GR.Generic.Tupel<string, Formats.MapProject.Map>( FormatMapDisplayName( index, map ), map ) );
        comboMaps.Enabled = true;
        ++index;
      }
      comboMaps.EndUpdate();


      comboTileBackground.SelectedIndex   = m_MapProject.BackgroundColor;
      RefreshDesignerBackgroundSwatch();
      comboTileMulticolor1.SelectedIndex = m_MapProject.MultiColor1;
      comboTileMulticolor2.SelectedIndex = m_MapProject.MultiColor2;
      comboTileBGColor4.SelectedIndex = m_MapProject.BGColor4;
      comboMapProjectMode.SelectedIndex = (int)m_MapProject.Mode;
      checkShowGrid.Checked = m_MapProject.ShowGrid;
      // Restore Auto-tiling toggle. Detach the CheckedChanged handler
      // so the assignment doesn't ricochet into a write-back and
      // mark the just-loaded project dirty.
      if ( checkAutoTiling != null )
      {
        checkAutoTiling.CheckedChanged -= checkAutoTiling_CheckedChanged;
        checkAutoTiling.Checked = m_MapProject.AutoTiling;
        checkAutoTiling.CheckedChanged += checkAutoTiling_CheckedChanged;
      }
      // Restore Lock-color toggle. Same detach pattern as Auto-tiling.
      if ( checkLockColor != null )
      {
        checkLockColor.CheckedChanged -= checkLockColor_CheckedChanged;
        checkLockColor.Checked = m_MapProject.LockTilePlacementColor;
        checkLockColor.CheckedChanged += checkLockColor_CheckedChanged;
      }
      // Restore the Map Strings scratch text. Detach the handler around the
      // assignment so loading doesn't dirty the just-loaded project.
      if ( editMapStringScratch != null )
      {
        editMapStringScratch.TextChanged -= editMapStringScratch_TextChanged;
        editMapStringScratch.Text = m_MapProject.MapStringsScratchText ?? "";
        editMapStringScratch.TextChanged += editMapStringScratch_TextChanged;
      }
      // Load the saved grid opacity into the slider. Detach the
      // ValueChanged handler around the assignment so it doesn't write
      // back into m_MapProject and dirty the just-loaded project.
      if ( gridOpacitySlider != null )
      {
        int savedOpacity = m_MapProject.GridOpacity;
        if ( savedOpacity < 0 )                   savedOpacity = 0;
        if ( savedOpacity > gridOpacitySlider.Maximum ) savedOpacity = gridOpacitySlider.Maximum;
        gridOpacitySlider.ValueChanged -= gridOpacitySlider_ValueChanged;
        gridOpacitySlider.Value = savedOpacity;
        gridOpacitySlider.ValueChanged += gridOpacitySlider_ValueChanged;
      }
      UpdateMapAspectRatio();
      ApplyExportSettingsToUI();

      RedrawMap();
      RedrawColorChooser();
      RedrawColorChooser();

      characterEditor.CharsetUpdated( m_MapProject.Charset );
      characterEditor.CharactersPerRow = m_MapProject.CharactersPerRow;
      characterEditor.EditorMode       = m_MapProject.CharacterEditorMode;
      characterEditor.SwatchSize       = m_MapProject.ColorSwatchSize;

      // Re-point our own panelCharacters items at the loaded charset's
      // Tile.Image bitmaps. Charset.ReadFromBuffer clears Characters
      // and re-adds new CharData instances with new Tile.Image bitmaps,
      // so the references the constructor stashed in
      // panelCharacters.Items[*].MemoryImage are now orphaned. The
      // bitmaps themselves already have valid pixels at this point —
      // characterEditor.CharsetUpdated above runs RebuildAllCharImages
      // which calls DisplayChar onto the new bitmaps. We just need to
      // update the Items references and invalidate. (The previously-
      // working case relied on the unguarded comboBackground handler
      // firing FullRebuild during one of the SelectedIndex assignments
      // earlier in this method — but that only fires when the new
      // BackgroundColor differs from the constructor's default, which
      // isn't the case for projects with BackgroundColor = 0.)
      for ( int i = 0; i < m_MapProject.Charset.TotalNumberOfCharacters; ++i )
      {
        if ( i < panelCharacters.Items.Count )
        {
          panelCharacters.Items[i].MemoryImage = m_MapProject.Charset.Characters[i].Tile.Image;
        }
      }
      panelCharacters.Invalidate();

      // Restore the inner-tab selection from the project, ONCE per load.
      // Detach the SelectedIndexChanged handler around the assignment so
      // the restore doesn't dirty the project. Clamp into the live page
      // count (defensive against tab additions/removals between saves).
      int savedTab = m_MapProject.LastSelectedTabIndex;
      if ( savedTab < 0 ) savedTab = 0;
      if ( savedTab >= tabMapEditor.Pages.Count ) savedTab = 0;
      if ( tabMapEditor.SelectedIndex != savedTab )
      {
        tabMapEditor.SelectedPageChanged -= tabMapEditor_SelectedIndexChanged;
        try
        {
          tabMapEditor.SelectedIndex = savedTab;
        }
        finally
        {
          tabMapEditor.SelectedPageChanged += tabMapEditor_SelectedIndexChanged;
        }
      }

      Modified = false;
      if ( string.IsNullOrEmpty( DocumentInfo.DocumentFilename ) )
      {
        DocumentInfo.DocumentFilename = File;
      }

      if ( ( comboMaps.Items.Count > 0 )
      &&   ( comboMaps.SelectedIndex == -1 ) )
      {
        // Restore the map the user had selected when this project was
        // saved. If the persisted index is out of range (project was
        // edited externally to remove maps, etc.) fall back to the first
        // map rather than leaving nothing selected.
        int target = m_MapProject.CurrentMapIndex;
        if ( ( target < 0 ) || ( target >= comboMaps.Items.Count ) )
        {
          target = 0;
        }
        comboMaps.SelectedIndex = target;
      }
      if ( ( comboTiles.Items.Count > 0 )
      &&   ( comboTiles.SelectedIndex == -1 ) )
      {
        comboTiles.SelectedIndex = 0;
      }
      if ( ( listTileInfo.Items.Count > 0 )
      &&   ( listTileInfo.SelectedIndices.Count == 0 ) )
      {
        listTileInfo.SelectedIndices.Add( 0 );
      }

      EnableFileWatcher();
      ResumeLayout();

      return true;
    }






    private void RefreshMapTileList()
    {
      if ( comboTiles == null )
      {
        return;
      }

      if ( editSwatchSize.Text != m_MapProject.ColorSwatchSize.ToString() )
      {
        editSwatchSize.Text = m_MapProject.ColorSwatchSize.ToString();
      }

      int selectedIndex = comboTiles.SelectedIndex;
      int selectedTileIndex = -1;
      if ( m_CurrentEditorTile != null )
      {
        selectedTileIndex = m_CurrentEditorTile.Index;
      }

      int listTileIndex = -1;
      if ( listTileInfo.SelectedIndices.Count > 0 )
      {
        listTileIndex = listTileInfo.SelectedIndices[0];
      }
      // Snapshot every selected index, not just the first — listTileChars
      // is multi-select and the rebuild below blows the whole selection
      // away. Restoring just SelectedIndices[0] would silently collapse
      // a Ctrl-click multi-selection to a single row when the user takes
      // a quick detour to the Map tab.
      var listCharIndices = new System.Collections.Generic.List<int>();
      foreach ( int idx in listTileChars.SelectedIndices )
      {
        listCharIndices.Add( idx );
      }

      comboTiles.BeginUpdate();
      listTileInfo.BeginUpdate();
      try
      {
        comboTiles.Items.Clear();
        listTileInfo.Items.Clear();
        foreach ( var tile in m_MapProject.Tiles )
        {
          comboTiles.Items.Add( new GR.Generic.Tupel<string, Formats.MapProject.Tile>( tile.Name, tile ) );

          ListViewItem item = new ListViewItem();

          // Column layout: # | Preview (image only) | Name | Size | Used.
          // Index 1 is the image-only column — empty text, the thumbnail
          // is painted by listTileInfo_DrawItemImage.
          item.Text = tile.Index.ToString();
          item.SubItems.Add( "" );
          item.SubItems.Add( tile.Name );
          item.SubItems.Add( tile.Chars.Width.ToString() + "x" + tile.Chars.Height.ToString() );
          item.SubItems.Add( "?" );
          item.Tag = tile;
          // ImageIndex = 0 picks up the SmallImageList placeholder, which
          // is what triggers CSListView.DrawItemImage; the actual tile
          // preview is rendered there from item.Tag rather than from the
          // ImageList itself.
          item.ImageIndex = 0;

          listTileInfo.Items.Add( item );
        }
        comboTiles.ItemHeight = MapTileListEffectiveItemHeight;
      }
      finally
      {
        comboTiles.EndUpdate();
        listTileInfo.EndUpdate();
      }

      int selectedRightClickIndex = comboRightClickBehavior.SelectedIndex;
      comboRightClickBehavior.BeginUpdate();
      comboRightClickBehavior.SelectedIndexChanged -= comboRightClickBehavior_SelectedIndexChanged;
      comboRightClickBehavior.Items.Clear();
      comboRightClickBehavior.Items.Add( "Default" );
      foreach ( var tile in m_MapProject.Tiles )
      {
        comboRightClickBehavior.Items.Add( "Use " + tile.Index + ": " + tile.Name );
      }
      if ( string.IsNullOrEmpty( m_MapProject.RightClickAction ) )
      {
        comboRightClickBehavior.SelectedIndex = 0;
      }
      else
      {
        for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
        {
          if ( m_MapProject.Tiles[i].Name == m_MapProject.RightClickAction )
          {
            comboRightClickBehavior.SelectedIndex = i + 1;
            break;
          }
        }
        if ( comboRightClickBehavior.SelectedIndex == -1 )
        {
          comboRightClickBehavior.SelectedIndex = 0;
          m_MapProject.RightClickAction = "";
        }
      }
      comboRightClickBehavior.SelectedIndexChanged += comboRightClickBehavior_SelectedIndexChanged;
      comboRightClickBehavior.EndUpdate();

      // Mirror the same Default-first-then-tiles pattern for the
      // shift-click blank tile combo. "Default" means "use tile 0" so
      // shift-click always has SOME defined action even when the user
      // never touched the dropdown.
      RefreshBlankTileCombo();

      int restoreIndex = ( selectedTileIndex >= 0 ) ? selectedTileIndex : selectedIndex;
      if ( ( restoreIndex >= 0 )
      &&   ( restoreIndex < comboTiles.Items.Count ) )
      {
        comboTiles.SelectedIndex = restoreIndex;
      }
      else if ( comboTiles.Items.Count > 0 )
      {
        comboTiles.SelectedIndex = 0;
      }

      if ( ( listTileIndex >= 0 )
      &&   ( listTileIndex < listTileInfo.Items.Count ) )
      {
        listTileInfo.SelectedIndices.Clear();
        listTileInfo.SelectedIndices.Add( listTileIndex );
        listTileInfo.EnsureVisible( listTileIndex );

        // Restore the FULL multi-selection, filtering out any indices
        // that no longer fit inside the rebuilt list.
        if ( ( listCharIndices.Count > 0 )
        &&   ( listTileChars.Items.Count > 0 ) )
        {
          listTileChars.SelectedIndices.Clear();
          foreach ( int idx in listCharIndices )
          {
            if ( ( idx >= 0 )
            &&   ( idx < listTileChars.Items.Count ) )
            {
              listTileChars.SelectedIndices.Add( idx );
            }
          }
          if ( listTileChars.SelectedIndices.Count > 0 )
          {
            listTileChars.EnsureVisible( listTileChars.SelectedIndices[0] );
          }
        }
      }
      comboTiles.Invalidate();
      RefreshMarkerTypes();
      RefreshEntityTypes();
      RefreshEntityTileIndexRange();
      RefreshMapStrings();
      PopulateMapStringPreviewIndices();
      LoadMapStringPreviewFont();
    }

    /// <summary>
    /// Keeps the EntityTypes editor's TileIndex NumericUpDown range in sync with
    /// the current tile count so an EntityType can't reference a missing tile.
    /// Also clamps the existing value defensively if the count shrank.
    /// </summary>
    private void RefreshEntityTileIndexRange()
    {
      if ( editEntityTileIndex == null )
      {
        return;
      }
      int maxIndex = Math.Max( 0, m_MapProject.Tiles.Count - 1 );
      editEntityTileIndex.Maximum = maxIndex;
      if ( editEntityTileIndex.Value > maxIndex )
      {
        editEntityTileIndex.Value = maxIndex;
      }
    }



    private void comboRightClickBehavior_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( comboRightClickBehavior.SelectedIndex == 0 )
      {
        m_MapProject.RightClickAction = "";
      }
      else
      {
        // "Default" is 0
        int tileIndex = comboRightClickBehavior.SelectedIndex - 1;
        if ( ( tileIndex >= 0 )
        &&   ( tileIndex < m_MapProject.Tiles.Count ) )
        {
          m_MapProject.RightClickAction = m_MapProject.Tiles[tileIndex].Name;
        }
      }
      Modified = true;
    }



    /// <summary>
    /// Repopulate the shift-click blank-tile dropdown from the project's
    /// current tile list. The combo lists every tile by name; there's no
    /// "Default" entry like comboRightClickBehavior has — shift-click is
    /// an explicit gesture, so there's no "off" state to expose. Selection
    /// is stored by tile NAME (not index) so reordering the tile list
    /// doesn't silently shift the saved blank tile.
    /// </summary>
    private void RefreshBlankTileCombo()
    {
      if ( comboBlankTile == null ) return;

      comboBlankTile.SelectedIndexChanged -= comboBlankTile_SelectedIndexChanged;
      comboBlankTile.BeginUpdate();
      comboBlankTile.Items.Clear();
      foreach ( var tile in m_MapProject.Tiles )
      {
        comboBlankTile.Items.Add( tile.Index + ": " + tile.Name );
      }

      if ( comboBlankTile.Items.Count == 0 )
      {
        // No tiles in the project at all — nothing to select. Empty out
        // the saved name too so we don't carry a dead reference.
        m_MapProject.ShiftClickBlankTile = "";
      }
      else
      {
        int idx = string.IsNullOrEmpty( m_MapProject.ShiftClickBlankTile )
                  ? -1
                  : m_MapProject.Tiles.FindIndex( t => t.Name == m_MapProject.ShiftClickBlankTile );
        if ( idx < 0 ) idx = 0;
        comboBlankTile.SelectedIndex = idx;
        // Sync the saved name back so a fresh project (empty string) or
        // a stale name both write the resolved tile name into the model
        // — keeps subsequent saves clean and idempotent.
        m_MapProject.ShiftClickBlankTile = m_MapProject.Tiles[idx].Name;
      }
      comboBlankTile.SelectedIndexChanged += comboBlankTile_SelectedIndexChanged;
      comboBlankTile.EndUpdate();

      // Sync the color combo too; project just loaded or tiles changed.
      if ( comboBlankColor != null )
      {
        int colorIdx = m_MapProject.ShiftClickBlankColor;
        if ( colorIdx < 0 ) colorIdx = 0;
        if ( colorIdx >= comboBlankColor.Items.Count ) colorIdx = 0;
        if ( comboBlankColor.SelectedIndex != colorIdx )
        {
          comboBlankColor.SelectedIndex = colorIdx;
        }
      }
    }



    /// <summary>
    /// Resolve <see cref="MapProject.ShiftClickBlankTile"/> (a tile name)
    /// to a tile index. Empty / not-found falls back to 0 — that gives
    /// shift-click a sensible default even when the user never touched
    /// the dropdown OR when the named tile was deleted out from under it.
    /// </summary>
    private int ResolveShiftClickBlankTileIndex()
    {
      if ( m_MapProject == null )                              return 0;
      if ( m_MapProject.Tiles.Count == 0 )                     return 0;
      if ( string.IsNullOrEmpty( m_MapProject.ShiftClickBlankTile ) ) return 0;
      int idx = m_MapProject.Tiles.FindIndex( t => t.Name == m_MapProject.ShiftClickBlankTile );
      return ( idx >= 0 ) ? idx : 0;
    }



    private void comboBlankTile_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      int tileIndex = comboBlankTile.SelectedIndex;
      if ( tileIndex < 0 || tileIndex >= m_MapProject.Tiles.Count ) return;

      string newValue = m_MapProject.Tiles[tileIndex].Name;
      if ( m_MapProject.ShiftClickBlankTile != newValue )
      {
        m_MapProject.ShiftClickBlankTile = newValue;
        Modified = true;
      }
    }



    private void comboBlankColor_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      int picked = comboBlankColor.SelectedIndex;
      if ( picked < 0 ) picked = 0;
      if ( picked > 15 ) picked = 0;
      if ( m_MapProject.ShiftClickBlankColor != picked )
      {
        m_MapProject.ShiftClickBlankColor = picked;
        Modified = true;
      }
    }



    /// <summary>
    /// Resolve the designer canvas color in ARGB. Prefers the explicit
    /// ARGB field (alpha != 0); falls back to the legacy palette-index
    /// field for projects saved before the picker existed. Centralised
    /// here so the painter and the swatch UI agree on what colour to use.
    /// </summary>
    private uint GetDesignerBackgroundARGB()
    {
      if ( m_MapProject == null ) return 0xff000000;
      if ( ( m_MapProject.DesignerBackgroundColorARGB & 0xff000000 ) != 0 )
      {
        return m_MapProject.DesignerBackgroundColorARGB;
      }
      // Legacy fallback: clamp the palette index and look it up.
      int idx = m_MapProject.DesignerBackgroundColor;
      var palette = m_MapProject.Charset.Colors.Palette.ColorValues;
      if ( idx < 0 || idx >= palette.Length ) idx = 0;
      return palette[idx];
    }



    /// <summary>
    /// Push the resolved color onto the swatch button's BackColor (and
    /// pick a contrasting border so light and dark colors both look
    /// "interactive" against the dark groupbox).
    /// </summary>
    private void RefreshDesignerBackgroundSwatch()
    {
      if ( btnDesignerBackground == null ) return;
      uint argb = GetDesignerBackgroundARGB();
      var color = System.Drawing.Color.FromArgb( unchecked( (int)argb ) );
      btnDesignerBackground.BackColor = color;
      // Hover color is derived from BackColor by Krypton/WinForms; force
      // it to match so the swatch doesn't jitter on mouseover.
      btnDesignerBackground.FlatAppearance.MouseOverBackColor = color;
      btnDesignerBackground.FlatAppearance.MouseDownBackColor = color;
    }



    private void btnDesignerBackground_Click( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;

      using ( var dlg = new System.Windows.Forms.ColorDialog() )
      {
        // FullOpen lets the user dial in any RGB; AllowFullOpen on its own
        // only enables the Define-Custom-Colors button without expanding.
        dlg.AllowFullOpen = true;
        dlg.FullOpen      = true;
        dlg.AnyColor      = true;

        uint currentArgb = GetDesignerBackgroundARGB();
        dlg.Color = System.Drawing.Color.FromArgb( unchecked( (int)currentArgb ) );

        if ( dlg.ShowDialog( this ) != System.Windows.Forms.DialogResult.OK )
        {
          return;
        }

        // ColorDialog returns a Color with alpha 0xFF — exactly what we
        // need for the "alpha != 0 means real pick" sentinel rule.
        uint picked = unchecked( (uint)dlg.Color.ToArgb() );
        if ( m_MapProject.DesignerBackgroundColorARGB != picked )
        {
          m_MapProject.DesignerBackgroundColorARGB = picked;
          RefreshDesignerBackgroundSwatch();
          RedrawMap();
          Modified = true;
        }
      }
    }

    public override bool LoadDocument()
    {
      if ( string.IsNullOrEmpty( DocumentInfo.DocumentFilename ) )
      {
        return false;
      }
      try
      {
        OpenProject( DocumentInfo.FullPath );
      }
      catch ( System.IO.IOException ex )
      {
        Core.Notification.MessageBox( "Could not load file", "Could not load map project file " + DocumentInfo.FullPath + ".\r\n" + ex.Message );
        return false;
      }
      SetUnmodified();
      return true;
    }



    public override GR.Memory.ByteBuffer SaveToBuffer()
    {
      UpdateExportSettingsFromUI( false );
      m_MapProject.CharactersPerRow = characterEditor.CharactersPerRow;
      m_MapProject.CharacterEditorMode = characterEditor.EditorMode;
      m_MapProject.ColorSwatchSize = characterEditor.SwatchSize;
      UpdateMarkerOutOfBoundsLabel();
      return m_MapProject.SaveToBuffer();
    }



    protected override bool QueryFilename( string PreviousFilename, out string Filename )
    {
      Filename = "";

      System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

      saveDlg.Title = "Save Map Editor Project as";
      saveDlg.Filter = "Map Editor Projects|*.mapproject|All Files|*.*";
      saveDlg.FileName = GR.Path.GetFileName( PreviousFilename );
      if ( DocumentInfo.Project != null )
      {
        saveDlg.InitialDirectory = DocumentInfo.Project.Settings.BasePath;
      }
      if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return false;
      }

      Filename = saveDlg.FileName;
      return true;
    }



    protected override bool PerformSave( string FullPath )
    {
      GR.Memory.ByteBuffer projectFile = SaveToBuffer();

      return SaveDocumentData( FullPath, projectFile );
    }



    private void closeCharsetProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( DocumentInfo.DocumentFilename == "" )
      {
        return;
      }
      if ( Modified )
      {
        var endButtons = MessageBoxButtons.YesNoCancel;
        if ( Core.ShuttingDown )
        {
          endButtons = MessageBoxButtons.YesNo;
        }
        DialogResult doSave = MessageBox.Show( "There are unsaved changes in your map project. Save now?", "Save changes?", endButtons );
        if ( doSave == DialogResult.Cancel )
        {
          return;
        }
        if ( doSave == DialogResult.Yes )
        {
          Save( SaveMethod.SAVE );
        }
      }
      Clear();
      DocumentInfo.DocumentFilename = "";
      Modified = false;
      pictureEditor.Invalidate();

      closeCharsetProjectToolStripMenuItem.Enabled = false;
      saveCharsetProjectToolStripMenuItem.Enabled = false;
    }



    private void saveCharsetProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      Save( SaveMethod.SAVE );
    }



    private bool ImportCharset( string Filename )
    {
      GR.Memory.ByteBuffer charData = GR.IO.File.ReadAllBytes( Filename );
      if ( charData == null )
      {
        return false;
      }

      int charsToImport = (int)charData.Length / 8;
      if ( charsToImport > 256 )
      {
        charsToImport = 256;
      }
      for ( int i = 0; i < charsToImport; ++i )
      {
        for ( int j = 0; j < 8; ++j )
        {
          m_MapProject.Charset.Characters[i].Tile.Data.SetU8At( j, charData.ByteAt( i * 8 + j ) );
        }
        RebuildCharImage( i );
      }
      characterEditor.CharsetUpdated( m_MapProject.Charset );
      return true;
    }



    private void Redraw()
    {
      pictureEditor.DisplayPage.DrawImage( m_Image, 0, 0 );

      if ( m_CurrentMap == null )
      {
        return;
      }

      if ( m_FloatingSelection != null )
      {
        for ( int j = 0; j < m_FloatingSelectionSize.Height; ++j )
        {
          for ( int i = 0; i < m_FloatingSelectionSize.Width; ++i )
          {
            var selectionChar = m_FloatingSelection[i + j * m_FloatingSelectionSize.Width];
            if ( selectionChar.first )
            {
              DrawTile( ( m_MousePos.X + i ),
                        ( m_MousePos.Y + j ),
                        selectionChar.second );
            }
          }
        }
      }
      pictureEditor.Invalidate();
    }

    private void CharacterEditor_CreateTileFromCharacter( object sender, EventArgs e )
    {
      var dlg = new Dialogs.FormInputText( Core, "Create Tile", "Tile Name:", "Tile" );
      if ( dlg.ShowDialog() != DialogResult.OK )
      {
        return;
      }

      var tile = new MapProject.Tile();
      tile.Name = MakeTileNameUnique( dlg.InputText );

      int tileWidth = characterEditor.EditorWidth;
      int tileHeight = characterEditor.EditorHeight;

      // Resize tile layer
      tile.Chars.Resize( tileWidth, tileHeight );

      int startChar = characterEditor.CurrentCharIndex;
      int charsPerRow = characterEditor.CharactersPerRow;

      for ( int y = 0; y < tileHeight; ++y )
      {
        for ( int x = 0; x < tileWidth; ++x )
        {
          int charIndex = startChar + x + y * charsPerRow;
          if ( charIndex < m_MapProject.Charset.Characters.Count )
          {
            tile.Chars[x, y].Character = (byte)charIndex;
            tile.Chars[x, y].Color     = (byte)m_MapProject.Charset.Characters[charIndex].Tile.CustomColor;
          }
          else
          {
            tile.Chars[x, y].Character = 0;
            tile.Chars[x, y].Color     = 1;
          }
        }
      }

      m_MapProject.Tiles.Add( tile );
      tile.Index = m_MapProject.Tiles.Count - 1;
      RefreshMapTileList();

      if ( comboTiles.Items.Count > 0 )
      {
        comboTiles.SelectedIndex = comboTiles.Items.Count - 1;
      }
      Modified = true;
    }



    /// <summary>
    /// "Create Multiple Tiles" event handler — fires when the user
    /// clicks the new button on the Character Set tab. The button is
    /// only enabled in 1x1 editor mode with at least one selected
    /// character (gated by <see cref="Controls.CharacterEditor.RefreshCreateMultipleTilesEnabled"/>),
    /// so this code can assume those preconditions but still defends.
    ///
    /// Flow: prompt for a base name (default "Tile"), then for each
    /// selected character index create a 1×1 tile named "{base} N"
    /// where N starts at 1 and increments. Naming goes through
    /// <see cref="MakeTileNameUnique"/> so collisions with existing
    /// tiles slide forward to the next free number — same uniqueness
    /// behaviour as the single-tile path. The last-created tile is
    /// selected in comboTiles, mirroring Create Tile.
    /// </summary>
    private void CharacterEditor_CreateMultipleTilesFromCharacters( object sender, EventArgs e )
    {
      // Defensive preconditions — the button shouldn't be clickable
      // outside these, but a stale state shouldn't crash.
      if ( m_MapProject == null ) return;
      if ( ( characterEditor.EditorWidth != 1 )
      ||   ( characterEditor.EditorHeight != 1 ) ) return;

      var selected = characterEditor.SelectedIndices;
      if ( ( selected == null ) || ( selected.Count == 0 ) ) return;

      var dlg = new Dialogs.FormInputText( Core, "Create Multiple Tiles", "Base name:", "Tile" );
      if ( dlg.ShowDialog() != DialogResult.OK )
      {
        return;
      }
      string baseName = ( dlg.InputText ?? string.Empty ).TrimEnd();
      if ( baseName.Length == 0 )
      {
        baseName = "Tile";
      }

      // Iterate selection in panel order. Snapshot to a local list
      // because creating tiles changes project state; the panel's
      // SelectedIndices reference may or may not be live, but the
      // safe move is to capture once.
      var indices = new List<int>( selected );

      // Counter starts at 1 so the user reads "Fence 1", "Fence 2".
      // MakeTileNameUnique handles collisions with already-existing
      // names of the same prefix.
      int counter = 1;
      int lastIndex = -1;
      for ( int i = 0; i < indices.Count; ++i )
      {
        int charIndex = indices[i];
        if ( ( charIndex < 0 )
        ||   ( charIndex >= m_MapProject.Charset.Characters.Count ) )
        {
          continue;
        }

        var tile = new MapProject.Tile();
        tile.Name = MakeTileNameUnique( baseName + " " + counter );
        ++counter;

        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0].Character = (byte)charIndex;
        tile.Chars[0, 0].Color     = (byte)m_MapProject.Charset.Characters[charIndex].Tile.CustomColor;

        m_MapProject.Tiles.Add( tile );
        tile.Index = m_MapProject.Tiles.Count - 1;
        lastIndex = tile.Index;
      }

      if ( lastIndex < 0 ) return; // every selected index was out of range

      RefreshMapTileList();
      if ( ( comboTiles.Items.Count > 0 )
      &&   ( lastIndex < comboTiles.Items.Count ) )
      {
        comboTiles.SelectedIndex = lastIndex;
      }
      Modified = true;
    }



    private void pictureEditor_Paint( object sender, PaintEventArgs e )
    {
    }



    private void CopyToClipboard()
    {
      // not only rectangular pieces
      int     x1 = m_CurrentMap.Tiles.Width;
      int     x2 = 0;
      int     y1 = m_CurrentMap.Tiles.Height;
      int     y2 = 0;
      bool    selectAll = false;

      for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
      {
        for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
        {
          if ( m_SelectedTiles[i, j] )
          {
            if ( i < x1 )
            {
              x1 = i;
            }
            if ( i > x2 )
            {
              x2 = i;
            }
            if ( j < y1 )
            {
              y1 = j;
            }
            if ( j > y2 )
            {
              y2 = j;
            }
          }
        }
      }
      if ( x1 == m_CurrentMap.Tiles.Width )
      {
        // no selection, select all
        x1 = 0;
        y1 = 0;
        x2 = m_CurrentMap.Tiles.Width - 1;
        y2 = m_CurrentMap.Tiles.Height - 1;
        selectAll = true;
      }

      GR.Memory.ByteBuffer dataSelection = new GR.Memory.ByteBuffer();

      dataSelection.Reserve( ( y2 - y1 + 1 ) * ( x2 - x1 + 1 ) + 8 );
      dataSelection.AppendI32( x2 - x1 + 1 );
      dataSelection.AppendI32( y2 - y1 + 1 );

      for ( int y = 0; y < y2 - y1 + 1; ++y )
      {
        for ( int x = 0; x < x2 - x1 + 1; ++x )
        {
          if ( ( selectAll )
          ||   ( m_SelectedTiles[x1 + x, y1 + y] ) )
          {
            dataSelection.AppendU8( 1 );
            dataSelection.AppendI32( m_CurrentMap.Tiles[x1 + x, y1 + y] );
          }
          else
          {
            dataSelection.AppendU8( 0 );
          }
        }
      }

      // Append the per-character color-override block for the selection's
      // bounding rectangle. Older readers stop after the per-cell tile
      // section above (their fixed-shape loop reads exactly W*H cells),
      // so the trailer is invisible to them — this remains a forward-
      // compatible extension of the existing clipboard format. The block
      // contains spacing dimensions (so paste can interpret the char grid
      // even if pasted into a map with different spacing), the char-grid
      // dimensions, and one i32 per char value (-1 = no override). Chars
      // belonging to UNSELECTED tile cells are still serialized — paste
      // skips them, matching the per-cell isSet flag in the tile section.
      int srcSpacingX = m_CurrentMap.TileSpacingX;
      int srcSpacingY = m_CurrentMap.TileSpacingY;
      int charW = ( x2 - x1 + 1 ) * srcSpacingX;
      int charH = ( y2 - y1 + 1 ) * srcSpacingY;
      dataSelection.AppendI32( srcSpacingX );
      dataSelection.AppendI32( srcSpacingY );
      dataSelection.AppendI32( charW );
      dataSelection.AppendI32( charH );
      int charBaseX = x1 * srcSpacingX;
      int charBaseY = y1 * srcSpacingY;
      for ( int j = 0; j < charH; ++j )
      {
        for ( int i = 0; i < charW; ++i )
        {
          int srcX = charBaseX + i;
          int srcY = charBaseY + j;
          int v = -1;
          if ( ( srcX >= 0 ) && ( srcY >= 0 )
          &&   ( srcX < m_CurrentMap.TileColorOverrides.Width )
          &&   ( srcY < m_CurrentMap.TileColorOverrides.Height ) )
          {
            v = m_CurrentMap.TileColorOverrides[srcX, srcY];
          }
          dataSelection.AppendI32( v );
        }
      }

      // Per-character "blocked" override trailer. Same forward-compat
      // approach as the color trailer above: older paste code stops
      // after the color trailer (which reads exactly its declared W*H
      // ints) and ignores anything past it. Layout: [i32 charW][i32
      // charH][byte × (charW * charH)] (1 byte per cell, 0/1) — same
      // shape as the on-disk MAP_CHAR_BLOCKED_OVERRIDES chunk. Spacing
      // is shared with the color trailer above (paste reads it once).
      dataSelection.AppendI32( charW );
      dataSelection.AppendI32( charH );
      for ( int j = 0; j < charH; ++j )
      {
        for ( int i = 0; i < charW; ++i )
        {
          int srcX = charBaseX + i;
          int srcY = charBaseY + j;
          bool b = false;
          if ( ( srcX >= 0 ) && ( srcY >= 0 )
          &&   ( srcX < m_CurrentMap.CharBlockedOverrides.Width )
          &&   ( srcY < m_CurrentMap.CharBlockedOverrides.Height ) )
          {
            b = m_CurrentMap.CharBlockedOverrides[srcX, srcY];
          }
          dataSelection.AppendU8( b ? (byte)1 : (byte)0 );
        }
      }

      DataObject dataObj = new DataObject();

      dataObj.SetData( "RetroDevStudio.MapEditorSelection", false, dataSelection.MemoryStream() );

      // TODO - Grafik?
      /*
      GR.Memory.ByteBuffer      dibData = m_Charset.Characters[m_CurrentChar].Image.CreateHDIBAsBuffer();

      System.IO.MemoryStream    ms = dibData.MemoryStream();

      // WTF - SetData requires streams, NOT global data (HGLOBAL)
      dataObj.SetData( "DeviceIndependentBitmap", ms );
      */
      Clipboard.SetDataObject( dataObj, true );
    }



    private void PasteFromClipboard()
    {
      IDataObject dataObj = Clipboard.GetDataObject();
      if ( dataObj == null )
      {
        Core.Notification.MessageBox( "Clipboard empty", "The clipboard is empty" );
        return;
      }
      if ( dataObj.GetDataPresent( "RetroDevStudio.MapEditorSelection" ) )
      {
        System.IO.MemoryStream ms = (System.IO.MemoryStream)dataObj.GetData( "RetroDevStudio.MapEditorSelection" );

        GR.Memory.ByteBuffer data = new GR.Memory.ByteBuffer( (uint)ms.Length );

        ms.Read( data.Data(), 0, (int)ms.Length );

        GR.IO.MemoryReader memIn = data.MemoryReader();

        int   selectionWidth  = memIn.ReadInt32();
        int   selectionHeight = memIn.ReadInt32();

        m_FloatingSelection = new List<GR.Generic.Tupel<bool, int>>();
        m_FloatingSelectionSize = new System.Drawing.Size( selectionWidth, selectionHeight );

        for ( int y = 0; y < selectionHeight; ++y )
        {
          for ( int x = 0; x < selectionWidth; ++x )
          {
            bool  isCharSet = ( memIn.ReadUInt8() != 0 );
            if ( isCharSet )
            {
              m_FloatingSelection.Add( new GR.Generic.Tupel<bool, int>( true, memIn.ReadInt32() ) );
            }
            else
            {
              m_FloatingSelection.Add( new GR.Generic.Tupel<bool, int>( false, 0 ) );
            }
          }
        }

        // Optional trailer — per-character color overrides written by
        // newer copy code. Layout: [srcSpacingX][srcSpacingY][charW]
        // [charH][charW × charH ints]. Detected by checking whether at
        // least the four header ints remain in the stream; older payloads
        // stop here and we leave the override list null (which makes
        // InsertFloatingSelection fall back to the legacy
        // ApplyPlacementColorOverride behaviour). Defensive on charW/H
        // mismatching srcSpacing × selection dimensions: clip to the
        // expected size so a malformed payload can't blow the heap.
        m_FloatingSelectionOverrides = null;
        m_FloatingSelectionSourceSpacingX = 1;
        m_FloatingSelectionSourceSpacingY = 1;
        if ( memIn.Size - memIn.Position >= 16 )
        {
          int srcSpacingX = memIn.ReadInt32();
          int srcSpacingY = memIn.ReadInt32();
          int charW = memIn.ReadInt32();
          int charH = memIn.ReadInt32();
          if ( ( srcSpacingX > 0 ) && ( srcSpacingY > 0 )
          &&   ( charW > 0 ) && ( charH > 0 )
          &&   ( charW == selectionWidth * srcSpacingX )
          &&   ( charH == selectionHeight * srcSpacingY )
          &&   ( memIn.Size - memIn.Position >= (long)charW * charH * 4 ) )
          {
            m_FloatingSelectionSourceSpacingX = srcSpacingX;
            m_FloatingSelectionSourceSpacingY = srcSpacingY;
            m_FloatingSelectionOverrides = new List<int>( charW * charH );
            for ( int i = 0; i < charW * charH; ++i )
            {
              m_FloatingSelectionOverrides.Add( memIn.ReadInt32() );
            }
          }
        }

        // Optional second trailer — per-character blocked overrides
        // captured by even-newer copy code. Layout: [i32 charW][i32
        // charH][byte × charW × charH]. Only attempted if the previous
        // (color) trailer was present AND the payload still has at
        // least the 8-byte header. Older payloads without this trailer
        // leave m_FloatingSelectionBlocked at null, so paste defaults
        // destination blocked overrides to false (no-override).
        m_FloatingSelectionBlocked = null;
        if ( ( m_FloatingSelectionOverrides != null )
        &&   ( memIn.Size - memIn.Position >= 8 ) )
        {
          int blkW = memIn.ReadInt32();
          int blkH = memIn.ReadInt32();
          int expectedCharW = selectionWidth  * m_FloatingSelectionSourceSpacingX;
          int expectedCharH = selectionHeight * m_FloatingSelectionSourceSpacingY;
          if ( ( blkW == expectedCharW )
          &&   ( blkH == expectedCharH )
          &&   ( memIn.Size - memIn.Position >= (long)blkW * blkH ) )
          {
            m_FloatingSelectionBlocked = new List<bool>( blkW * blkH );
            for ( int i = 0; i < blkW * blkH; ++i )
            {
              m_FloatingSelectionBlocked.Add( memIn.ReadUInt8() != 0 );
            }
          }
        }

        m_FloatingSelectionPos = m_MousePos;
        Redraw();
        pictureEditor.Invalidate();
        return;
      }
      else if ( dataObj.GetDataPresent( "RetroDevStudio.CharacterScreenSelection" ) )
      {
        System.IO.MemoryStream ms = (System.IO.MemoryStream)dataObj.GetData( "RetroDevStudio.CharacterScreenSelection" );

        GR.Memory.ByteBuffer data = new GR.Memory.ByteBuffer( (uint)ms.Length );

        ms.Read( data.Data(), 0, (int)ms.Length );

        GR.IO.MemoryReader memIn = data.MemoryReader();

        int   selectionWidth  = memIn.ReadInt32();
        int   selectionHeight = memIn.ReadInt32();

        var copyData = new List<GR.Generic.Tupel<bool, uint>>();
        var copyDataSize = new System.Drawing.Size( selectionWidth, selectionHeight );

        for ( int y = 0; y < selectionHeight; ++y )
        {
          for ( int x = 0; x < selectionWidth; ++x )
          {
            bool  isCharSet = ( memIn.ReadUInt8() != 0 );
            if ( isCharSet )
            {
              copyData.Add( new GR.Generic.Tupel<bool, uint>( true, memIn.ReadUInt32() ) );
            }
            else
            {
              copyData.Add( new GR.Generic.Tupel<bool, uint>( false, 0 ) );
            }
          }
        }
        if ( tabMapEditor.SelectedPage == tabTiles )
        {
          bool modified = false;
          if ( listTileInfo.SelectedIndices.Count > 0 )
          {
            for ( int y = 0; y < selectionHeight; ++y )
            {
              for ( int x = 0; x < selectionWidth; ++x )
              {
                if ( copyData[x + y * selectionWidth].first )
                {
                  if ( ( x < m_CurrentEditedTile.Chars.Width )
                  &&   ( y < m_CurrentEditedTile.Chars.Height ) )
                  {
                    if ( !modified )
                    {
                      modified = true;
                      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
                    }

                    m_CurrentEditedTile.Chars[x, y].Character = (byte)( copyData[x + y * selectionWidth].second & 0xff );
                    m_CurrentEditedTile.Chars[x, y].Color     = (byte)( ( copyData[x + y * selectionWidth].second >> 16 ) & 0xff );
                  }
                }
              }
            }
            if ( modified )
            {
              RedrawTile();
              RedrawMap();
              SetModified();
            }
          }
        }
      }
    }

    private void btnShiftLeft_Click( object sender, EventArgs e )
    {
      ShiftMap( -1, 0, IsShiftKeyDown() );
    }

    private void btnShiftRight_Click( object sender, EventArgs e )
    {
      ShiftMap( 1, 0, IsShiftKeyDown() );
    }

    private void btnShiftUp_Click( object sender, EventArgs e )
    {
      ShiftMap( 0, -1, IsShiftKeyDown() );
    }

    private void btnShiftDown_Click( object sender, EventArgs e )
    {
      ShiftMap( 0, 1, IsShiftKeyDown() );
    }



    /// <summary>
    /// True iff Shift is currently held. Used by the Shift Map buttons
    /// to switch from "shift + vacate" to "roll / wrap" semantics —
    /// content that falls off one edge re-enters from the opposite
    /// edge instead of being dropped.
    /// </summary>
    private static bool IsShiftKeyDown()
    {
      return ( Control.ModifierKeys & Keys.Shift ) == Keys.Shift;
    }



    /// <summary>
    /// Enables owner-drawn items on a <see cref="Krypton.Toolkit.KryptonComboBox"/>
    /// by reaching through its inner WinForms <see cref="ComboBox"/>. Kept out
    /// of the designer file because the CodeDom serializer refuses to load a
    /// form whose InitializeComponent contains chained sub-control accesses
    /// like "this.kcb.ComboBox.DrawMode = ...".
    /// </summary>
    private static void WireOwnerDrawCombo( Krypton.Toolkit.KryptonComboBox kcb, DrawItemEventHandler handler )
    {
      if ( ( kcb == null ) || ( kcb.ComboBox == null ) )
      {
        return;
      }
      kcb.ComboBox.DrawMode = DrawMode.OwnerDrawFixed;
      kcb.ComboBox.DrawItem += handler;
    }



    /// <summary>
    /// Recursively walks the control tree under <paramref name="root"/> and
    /// yields every <see cref="Krypton.Toolkit.KryptonComboBox"/> found. Used
    /// during form construction to apply dark disabled-state styling.
    /// </summary>

    private static IEnumerable<Krypton.Toolkit.KryptonComboBox> FindAllKryptonCombos( Control root )
    {
      if ( root is Krypton.Toolkit.KryptonComboBox combo )
      {
        yield return combo;
      }
      foreach ( Control child in root.Controls )
      {
        foreach ( var descendant in FindAllKryptonCombos( child ) )
        {
          yield return descendant;
        }
      }
    }

    private void btnRemoveOverlappingTiles_Click( object sender, EventArgs e )
    {
      RemoveOverlappingTiles();
    }



    /// <summary>
    /// Clear every per-character passable override on the current map.
    /// One <see cref="Undo.UndoMapCharBlockedChange"/> snapshot covers
    /// the whole layer so Ctrl+Z restores it. Confirms first because
    /// a full clear is irreversible without undo, and a misclick on a
    /// large map would otherwise be expensive to redo manually.
    /// </summary>
    private void btnClearPassableMap_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;
      if ( m_IsViewingRevision ) return;

      var blocked = m_CurrentMap.CharBlockedOverrides;
      if ( ( blocked == null )
      ||   ( blocked.Width == 0 )
      ||   ( blocked.Height == 0 ) )
      {
        return;
      }

      // Skip the confirm + undo + repaint when nothing is set.
      bool anySet = false;
      for ( int y = 0; y < blocked.Height && !anySet; ++y )
      {
        for ( int x = 0; x < blocked.Width; ++x )
        {
          if ( blocked[x, y] ) { anySet = true; break; }
        }
      }
      if ( !anySet ) return;

      var confirm = MessageBox.Show( this,
        "Clear all per-character passable overrides on this map?",
        "Clear passable overrides",
        MessageBoxButtons.OKCancel,
        MessageBoxIcon.Warning,
        MessageBoxDefaultButton.Button2 );
      if ( confirm != DialogResult.OK ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapCharBlockedChange( this, m_CurrentMap, 0, 0, blocked.Width, blocked.Height ) );

      for ( int y = 0; y < blocked.Height; ++y )
      {
        for ( int x = 0; x < blocked.Width; ++x )
        {
          blocked[x, y] = false;
        }
      }

      // Repaint the whole map — the PASSABLE overlay tints every char
      // that was previously blocked, so a full redraw is the cheapest
      // way to flush the visualisation.
      RedrawMap();
      pictureEditor.Invalidate();
      SetModified();
    }



    /// <summary>
    /// Walks the map in reading order (left-to-right, top-to-bottom). For each
    /// non-empty tile, determines the tile's footprint in map cells, and clears
    /// (sets to 0) any non-empty cell inside that footprint other than the anchor.
    /// Cleanup for multi-cell tiles whose footprints overlap later placements.
    /// </summary>
    private void RemoveOverlappingTiles()
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      int w = m_CurrentMap.Tiles.Width;
      int h = m_CurrentMap.Tiles.Height;
      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );

      // Scan once first so we only record an undo (and fire Modified) when
      // there's actually something to change.
      int clearedCount = 0;
      for ( int y = 0; y < h; ++y )
      {
        for ( int x = 0; x < w; ++x )
        {
          int tileIndex = m_CurrentMap.Tiles[x, y];
          if ( ( tileIndex <= 0 )
          ||   ( tileIndex >= m_MapProject.Tiles.Count ) )
          {
            continue;
          }

          var tile = m_MapProject.Tiles[tileIndex];
          int cellsWide = Math.Max( 1, ( tile.Chars.Width  + spacingX - 1 ) / spacingX );
          int cellsTall = Math.Max( 1, ( tile.Chars.Height + spacingY - 1 ) / spacingY );

          for ( int dy = 0; dy < cellsTall; ++dy )
          {
            for ( int dx = 0; dx < cellsWide; ++dx )
            {
              if ( ( dx == 0 ) && ( dy == 0 ) )
              {
                continue;
              }
              int nx = x + dx;
              int ny = y + dy;
              if ( ( nx >= w ) || ( ny >= h ) )
              {
                continue;
              }
              if ( m_CurrentMap.Tiles[nx, ny] != 0 )
              {
                ++clearedCount;
              }
            }
          }
        }
      }

      if ( clearedCount == 0 )
      {
        Core.Notification.MessageBox( "Remove overlapping tiles",
          "No overlapping tiles were found." );
        return;
      }

      // Snapshot for undo, then perform the actual clearing. Using the same
      // scan order — the second pass sees the same tiles as the first because
      // the anchor cells are never cleared.
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange(
        this, m_CurrentMap, 0, 0, w, h ) );

      for ( int y = 0; y < h; ++y )
      {
        for ( int x = 0; x < w; ++x )
        {
          int tileIndex = m_CurrentMap.Tiles[x, y];
          if ( ( tileIndex <= 0 )
          ||   ( tileIndex >= m_MapProject.Tiles.Count ) )
          {
            continue;
          }

          var tile = m_MapProject.Tiles[tileIndex];
          int cellsWide = Math.Max( 1, ( tile.Chars.Width  + spacingX - 1 ) / spacingX );
          int cellsTall = Math.Max( 1, ( tile.Chars.Height + spacingY - 1 ) / spacingY );

          for ( int dy = 0; dy < cellsTall; ++dy )
          {
            for ( int dx = 0; dx < cellsWide; ++dx )
            {
              if ( ( dx == 0 ) && ( dy == 0 ) )
              {
                continue;
              }
              int nx = x + dx;
              int ny = y + dy;
              if ( ( nx >= w ) || ( ny >= h ) )
              {
                continue;
              }
              if ( m_CurrentMap.Tiles[nx, ny] != 0 )
              {
                m_CurrentMap.Tiles[nx, ny] = 0;
              }
            }
          }
        }
      }

      SetModified();
      UpdateArea( 0, 0, w, h );
      Core.Notification.MessageBox( "Remove overlapping tiles",
        "Cleared " + clearedCount + " overlapping tile" + ( clearedCount == 1 ? "" : "s" ) + "." );
    }

    /// <summary>
    /// Shift the entire map by (DX, DY) tile cells. Default semantics
    /// SHIFT-and-VACATE: content that falls off the edge is lost; the
    /// vacated cells are filled with sentinel values (tile 0, color
    /// override -1, blocked override false). When <paramref name="Roll"/>
    /// is true, every layer (tiles, color overrides, blocked overrides,
    /// markers, entities) WRAPS modulo the map dimensions instead — what
    /// leaves one edge re-enters the opposite edge. Roll mode is
    /// triggered by holding Shift while clicking the Shift Map buttons.
    ///
    /// Implementation uses temp buffers (one per layer) so the
    /// destination writes don't trample source values mid-copy. This
    /// removes the per-direction in-place ordering dance that the old
    /// vacate-only path had — the cost is one extra alloc per layer per
    /// shift, dwarfed by the redraw that follows anyway.
    /// </summary>
    private void ShiftMap( int DX, int DY, bool Roll = false )
    {
      if ( !IsMapEditable )
      {
        return;
      }
      // Snapshot tiles and markers in one undo group so Ctrl+Z rewinds both at once.
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );

      int    w = m_CurrentMap.Tiles.Width;
      int    h = m_CurrentMap.Tiles.Height;
      int    spacingX = m_CurrentMap.TileSpacingX;
      int    spacingY = m_CurrentMap.TileSpacingY;
      int    charW = w * spacingX;
      int    charH = h * spacingY;

      // Defensive: legacy maps may have the override layer at the old
      // tile-grid shape, OR briefly out of shape during resize. Bring it
      // to the char-grid shape before shifting; cheap to call when sizes
      // already match.
      if ( ( m_CurrentMap.TileColorOverrides.Width != charW )
      ||   ( m_CurrentMap.TileColorOverrides.Height != charH ) )
      {
        ResizeColorOverridesPreservingDefaults( m_CurrentMap.TileColorOverrides, charW, charH );
      }
      if ( ( m_CurrentMap.CharBlockedOverrides.Width != charW )
      ||   ( m_CurrentMap.CharBlockedOverrides.Height != charH ) )
      {
        m_CurrentMap.CharBlockedOverrides.Resize( charW, charH );
      }

      int charDX = DX * spacingX;
      int charDY = DY * spacingY;

      // ----- Tiles layer (tile-grid) -----
      int[,] newTiles = new int[w, h];
      for ( int x = 0; x < w; ++x )
      {
        for ( int y = 0; y < h; ++y )
        {
          int srcX, srcY;
          if ( Roll )
          {
            srcX = ( ( x - DX ) % w + w ) % w;
            srcY = ( ( y - DY ) % h + h ) % h;
            newTiles[x, y] = m_CurrentMap.Tiles[srcX, srcY];
          }
          else
          {
            srcX = x - DX;
            srcY = y - DY;
            if ( ( srcX >= 0 ) && ( srcY >= 0 ) && ( srcX < w ) && ( srcY < h ) )
            {
              newTiles[x, y] = m_CurrentMap.Tiles[srcX, srcY];
            }
            else
            {
              newTiles[x, y] = 0;   // vacated cell
            }
          }
        }
      }
      for ( int x = 0; x < w; ++x )
      {
        for ( int y = 0; y < h; ++y )
        {
          m_CurrentMap.Tiles[x, y] = newTiles[x, y];
        }
      }

      // ----- Color override layer (char-grid). -1 is the "no override"
      // sentinel, used to fill vacated cells in non-roll mode. -----
      var overrides = m_CurrentMap.TileColorOverrides;
      int[,] newOverrides = new int[charW, charH];
      for ( int x = 0; x < charW; ++x )
      {
        for ( int y = 0; y < charH; ++y )
        {
          int srcX, srcY;
          if ( Roll )
          {
            srcX = ( ( x - charDX ) % charW + charW ) % charW;
            srcY = ( ( y - charDY ) % charH + charH ) % charH;
            newOverrides[x, y] = overrides[srcX, srcY];
          }
          else
          {
            srcX = x - charDX;
            srcY = y - charDY;
            if ( ( srcX >= 0 ) && ( srcY >= 0 ) && ( srcX < charW ) && ( srcY < charH ) )
            {
              newOverrides[x, y] = overrides[srcX, srcY];
            }
            else
            {
              newOverrides[x, y] = -1;
            }
          }
        }
      }
      for ( int x = 0; x < charW; ++x )
      {
        for ( int y = 0; y < charH; ++y )
        {
          overrides[x, y] = newOverrides[x, y];
        }
      }

      // ----- Blocked-override layer (char-grid). false is the
      // "no override" sentinel; vacated cells in non-roll mode get false. -----
      var blocked = m_CurrentMap.CharBlockedOverrides;
      bool[,] newBlocked = new bool[charW, charH];
      for ( int x = 0; x < charW; ++x )
      {
        for ( int y = 0; y < charH; ++y )
        {
          int srcX, srcY;
          if ( Roll )
          {
            srcX = ( ( x - charDX ) % charW + charW ) % charW;
            srcY = ( ( y - charDY ) % charH + charH ) % charH;
            newBlocked[x, y] = blocked[srcX, srcY];
          }
          else
          {
            srcX = x - charDX;
            srcY = y - charDY;
            if ( ( srcX >= 0 ) && ( srcY >= 0 ) && ( srcX < charW ) && ( srcY < charH ) )
            {
              newBlocked[x, y] = blocked[srcX, srcY];
            }
            else
            {
              newBlocked[x, y] = false;
            }
          }
        }
      }
      for ( int x = 0; x < charW; ++x )
      {
        for ( int y = 0; y < charH; ++y )
        {
          blocked[x, y] = newBlocked[x, y];
        }
      }

      // Markers: in-map markers shift (or wrap, in roll mode); off-map
      // (global / non-level meta-markers) are always left alone — they
      // aren't part of the map's spatial content.
      var shiftedMarkers = new List<MapProject.Marker>();
      foreach ( var marker in m_CurrentMap.Markers )
      {
        bool isInsideMap = ( marker.X >= 0 )
                        && ( marker.Y >= 0 )
                        && ( marker.X < w )
                        && ( marker.Y < h );
        if ( !isInsideMap )
        {
          shiftedMarkers.Add( marker );
          continue;
        }

        int newX = marker.X + DX;
        int newY = marker.Y + DY;
        if ( Roll )
        {
          newX = ( newX % w + w ) % w;
          newY = ( newY % h + h ) % h;
        }
        else if ( ( newX < 0 ) || ( newX > 255 ) || ( newY < 0 ) || ( newY > 255 ) )
        {
          // shifted off the addressable range — drop
          continue;
        }
        marker.X = newX;
        marker.Y = newY;
        shiftedMarkers.Add( marker );
      }
      m_CurrentMap.Markers = shiftedMarkers;
      UpdateMarkerOutOfBoundsLabel();

      // Entities are strictly in-map; in non-roll mode anything past the
      // bounds is dropped, in roll mode it wraps.
      var shiftedEntities = new List<MapProject.Entity>();
      foreach ( var entity in m_CurrentMap.Entities )
      {
        int newX = entity.X + DX;
        int newY = entity.Y + DY;
        if ( Roll )
        {
          newX = ( newX % w + w ) % w;
          newY = ( newY % h + h ) % h;
        }
        else if ( ( newX < 0 ) || ( newY < 0 )
        ||        ( newX >= w ) || ( newY >= h ) )
        {
          continue;
        }
        entity.X = newX;
        entity.Y = newY;
        shiftedEntities.Add( entity );
      }
      m_CurrentMap.Entities = shiftedEntities;
      UpdateEntityCountLabel();

      SetModified();
      RedrawMap();
    }



    private void panelCharacters_SelectedIndexChanged( object sender, EventArgs e )
    {
      m_CurrentChar = (byte)panelCharacters.SelectedIndex;
      RedrawColorChooser();

      if ( ( m_CurrentTileChar != null )
      &&   ( m_CurrentTileChar.Character != m_CurrentChar ) 
      &&   ( listTileInfo.SelectedIndices.Count > 0 )
      &&   ( listTileChars.SelectedItems.Count > 0 ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );

        m_CurrentTileChar.Character = m_CurrentChar;

        listTileChars.SelectedItems[0].SubItems[1].Text = m_CurrentChar.ToString();
        RedrawTile();
        RedrawMap();
        SetModified();
      }
    }



    private void RedrawColorChooser()
    {
      if ( ( m_MapProject == null )
      ||   ( m_MapProject.ColorSwatchSize <= 0 ) )
      {
        return;
      }
      int itemsPerRow = Math.Max( 1, panelCharColors.ClientSize.Width / m_MapProject.ColorSwatchSize );
      int numRows = ( 16 + itemsPerRow - 1 ) / itemsPerRow;
      int requiredHeight = numRows * m_MapProject.ColorSwatchSize;
      
      if ( panelCharColors.Height != requiredHeight )
      {
        panelCharColors.Height = requiredHeight;
      }
      if ( ( panelCharColors.DisplayPage.Width != panelCharColors.ClientSize.Width )
      ||   ( panelCharColors.DisplayPage.Height != requiredHeight ) )
      {
        panelCharColors.DisplayPage.Create( panelCharColors.ClientSize.Width, requiredHeight, GR.Drawing.PixelFormat.Format32bppArgb );
      }
      panelCharColors.DisplayPage.Box( 0, 0, panelCharColors.DisplayPage.Width, panelCharColors.DisplayPage.Height, 0xff000000 );

      GR.Image.FastImage tempImage = new GR.Image.FastImage( 8, 8, GR.Drawing.PixelFormat.Format32bppArgb );

      for ( byte i = 0; i < 16; ++i )
      {
        DrawCharImage( tempImage, 0, 0, m_CurrentChar, i );

        for ( int y = 0; y < 8; ++y )
        {
          for ( int x = 0; x < 8; ++x )
          {
            uint pixel = tempImage.GetPixel( x, y );

            int destX = ( i % itemsPerRow ) * m_MapProject.ColorSwatchSize;
            int destY = ( i / itemsPerRow ) * m_MapProject.ColorSwatchSize;

            int destStartX = destX + ( x * m_MapProject.ColorSwatchSize ) / 8;
            int destXEnd = destX + ( ( x + 1 ) * m_MapProject.ColorSwatchSize ) / 8;
            int destStartY = destY + ( y * m_MapProject.ColorSwatchSize ) / 8;
            int destYEnd = destY + ( ( y + 1 ) * m_MapProject.ColorSwatchSize ) / 8;

            for ( int dy = destStartY; dy < destYEnd; ++dy )
            {
              for ( int dx = destStartX; dx < destXEnd; ++dx )
              {
                panelCharColors.DisplayPage.SetPixel( dx, dy, pixel );
              }
            }
          }
        }
      }
      int selX = ( m_CurrentColor % itemsPerRow ) * m_MapProject.ColorSwatchSize;
      int selY = ( m_CurrentColor / itemsPerRow ) * m_MapProject.ColorSwatchSize;

      panelCharColors.DisplayPage.Rectangle( selX, selY, m_MapProject.ColorSwatchSize, m_MapProject.ColorSwatchSize, Core.Settings.FGColor( ColorableElement.SELECTION_FRAME ) );
      panelCharColors.Invalidate();
    }



    private void panelCharColors_MouseDown( object sender, MouseEventArgs e )
    {
      HandleMouseOnColorChooser( e.X, e.Y, e.Button );
    }



    private void panelCharColors_MouseMove( object sender, MouseEventArgs e )
    {
      MouseButtons    buttons = e.Button;
      if ( !panelCharColors.Focused )
      {
        buttons = 0;
      }
      HandleMouseOnColorChooser( e.X, e.Y, buttons );
    }



    private void HandleMouseOnColorChooser( int X, int Y, MouseButtons Buttons )
    {
      if ( ( Buttons & MouseButtons.Left ) == MouseButtons.Left )
      {
        if ( m_MapProject.ColorSwatchSize <= 0 )
        {
          return;
        }
        int itemsPerRow = Math.Max( 1, panelCharColors.ClientSize.Width / m_MapProject.ColorSwatchSize );
        int col = X / m_MapProject.ColorSwatchSize;
        int row = Y / m_MapProject.ColorSwatchSize;
        int colorIndex = col + row * itemsPerRow;

        if ( ( colorIndex >= 0 )
        &&   ( colorIndex < 16 ) )
        {
          m_CurrentColor = (byte)colorIndex;
          RedrawColorChooser();

          // Multi-select aware: every selected character row in
          // listTileChars takes the new color, not just the focused one.
          // We collect the rows that actually need a change first so a
          // click on a color that's already assigned to every selected
          // row stays a no-op (no undo entry, no redraw).
          if ( ( listTileInfo.SelectedIndices.Count > 0 )
          &&   ( listTileChars.SelectedItems.Count > 0 ) )
          {
            var rowsToChange = new System.Collections.Generic.List<ListViewItem>();
            foreach ( ListViewItem item in listTileChars.SelectedItems )
            {
              var tc = item.Tag as Formats.MapProject.TileChar;
              if ( ( tc != null )
              &&   ( tc.Color != m_CurrentColor ) )
              {
                rowsToChange.Add( item );
              }
            }
            if ( rowsToChange.Count > 0 )
            {
              // One undo entry covers every per-character change because
              // UndoMapTileModified snapshots the entire tile. Pushing it
              // before the mutation matches the rest of this editor.
              DocumentInfo.UndoManager.AddUndoTask(
                new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );

              foreach ( var item in rowsToChange )
              {
                var tc = (Formats.MapProject.TileChar)item.Tag;
                tc.Color = m_CurrentColor;
                item.SubItems[2].Text = m_CurrentColor.ToString();
              }
              RedrawTile();
              RedrawMap();
              SetModified();
            }
          }
        }
      }
    }



    private void importCharsetToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ImportCharset();
    }



    public bool OpenExternalCharset( string Filename )
    {
      string extension = GR.Path.GetExtension( Filename ).ToUpper();

      if ( extension == ".CHARSETPROJECT" )
      {
        GR.Memory.ByteBuffer charSetProject = GR.IO.File.ReadAllBytes( Filename );
        if ( charSetProject == null )
        {
          return false;
        }
        if ( !m_MapProject.Charset.ReadFromBuffer( charSetProject ) )
        {
          return false;
        }
        m_MapProject.Mode = Lookup.TextModeFromTextCharMode( m_MapProject.Charset.Mode );
        comboMapProjectMode.SelectedIndex = (int)m_MapProject.Mode;

        FullRebuild();
        characterEditor.CharsetUpdated( m_MapProject.Charset );
        RedrawMap();
        Modified = true;
        return true;
      }
      // treat as .chr
      if ( !ImportCharset( Filename ) )
      {
        return false;
      }
      pictureEditor.Invalidate();
      Modified = true;
      return true;
    }



    public void ImportCharset()
    {
      string filename;

      if ( !OpenFile( "Open charset or charset project", Types.Constants.FILEFILTER_CHARSET + Types.Constants.FILEFILTER_ALL, out filename ) )
      {
        return;
      }
      OpenExternalCharset( filename );
      if ( ( DocumentInfo.Project == null )
      ||   ( string.IsNullOrEmpty( DocumentInfo.Project.Settings.BasePath ) ) )
      {
        m_MapProject.ExternalCharset = filename;
      }
      else
      {
        m_MapProject.ExternalCharset = GR.Path.RelativePathTo( filename, false, System.IO.Path.GetFullPath( DocumentInfo.Project.Settings.BasePath ), true );
      }
      Modified = true;
    }



    private void btnExportToFile_Click( object sender, EventArgs e )
    {
      System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

      saveDlg.Title = "Save data as";
      saveDlg.Filter = "Map Data|*.map|Binary Data|*.bin|All Files|*.*";
      if ( DocumentInfo.Project != null )
      {
        saveDlg.InitialDirectory = DocumentInfo.Project.Settings.BasePath;
      }
      if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return;
      }

      // prepare data
      GR.Memory.ByteBuffer tileData = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer mapData = new GR.Memory.ByteBuffer();

      GR.Memory.ByteBuffer finalData = null;

      switch ( (MapExportType)comboExportData.SelectedIndex )
      {
        case MapExportType.TILE_DATA:
          m_MapProject.ExportTilesAsBuffer( comboExportOrientation.SelectedIndex == 0, out tileData );
          finalData = tileData;
          break;
        case MapExportType.TILE_AND_MAP_DATA:
          m_MapProject.ExportTilesAsBuffer( comboExportOrientation.SelectedIndex == 0, out tileData );
          mapData = m_MapProject.ExportMapsAsBuffer( comboExportOrientation.SelectedIndex == 0 );
          finalData = tileData + mapData;
          break;
        case MapExportType.MAP_DATA:
          {
            bool    vertical = ( comboExportOrientation.SelectedIndex != 0 );

            if ( m_CurrentMap != null )
            {
              GR.Memory.ByteBuffer      selectionData = new GR.Memory.ByteBuffer();

              if ( vertical )
              {
                // select all
                for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                {
                  for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                  {
                    selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                  }
                }
              }
              else
              {
                // select all
                for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                {
                  for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                  {
                    selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                  }
                }
              }
              finalData = selectionData;
            }
          }
          break;
        case MapExportType.MAP_DATA_SELECTION:
          {
            bool    vertical = ( comboExportOrientation.SelectedIndex != 0 );

            if ( m_CurrentMap != null )
            {
              GR.Memory.ByteBuffer      selectionData = new GR.Memory.ByteBuffer();
              bool                      hasSelection = false;

              if ( vertical )
              {
                for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                {
                  for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                  {
                    if ( m_SelectedTiles[i, j] )
                    {
                      selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                      hasSelection = true;
                    }
                  }
                }
                if ( !hasSelection )
                {
                  // select all
                  for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                  {
                    for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                    {
                      selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                    }
                  }
                }
              }
              else
              {
                for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                {
                  for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                  {
                    if ( m_SelectedTiles[i, j] )
                    {
                      selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                      hasSelection = true;
                    }
                  }
                }
                if ( !hasSelection )
                {
                  // select all
                  for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
                  {
                    for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
                    {
                      selectionData.AppendU8( (byte)m_CurrentMap.Tiles[i, j] );
                    }
                  }
                }
              }
              finalData = selectionData;
            }
          }
          break;
        default:
          Core.Notification.MessageBox( "Export type not supported", "The export type " + (MapExportType)comboExportData.SelectedIndex + " is not supported for binary export." );
          return;
      }
      if ( finalData != null )
      {
        GR.IO.File.WriteAllBytes( saveDlg.FileName, finalData );
      }
    }



    private void CharacterEditor_Modified( List<int> ModifiedChars )
    {
      Modified = true;
    }

    private void AdjustScrollbars()
    {
      mapHScroll.Minimum = 0;
      mapHScroll.SmallChange = 1;
      mapHScroll.LargeChange = 1;
      mapVScroll.SmallChange = 1;
      mapVScroll.LargeChange = 1;
      if ( m_CurrentMap == null )
      {
        mapHScroll.Maximum = 0;
        mapVScroll.Maximum = 0;
        return;
      }

      int viewCharWidth = ViewCharWidth;
      int viewCharHeight = ViewCharHeight;

      // Scrollbars are ALWAYS active so the user can scroll past the map's
      // right/bottom edge into off-map space to park non-interactive markers
      // — even when the whole map already fits the viewport. The scrollable
      // extent is the larger of the map or the current viewport, plus a fixed
      // overhang, so there's always at least MapScrollOverhangChars of empty
      // space to scroll into beyond whatever is currently visible.
      //
      // Consequence: centering (RedrawMap centers a map smaller than the
      // viewport) and scrolling are BOTH active at once for a fitting map.
      // The render paths handle that — cell iteration starts far enough
      // left/up to include the columns/rows the centering gap exposes, and
      // the background fill spans the map's true on-screen rect — so the map
      // pans cleanly instead of being clipped. See RedrawMap.
      int mapCharWidth  = m_CurrentMap.TileSpacingX * m_CurrentMap.Tiles.Width;
      int mapCharHeight = m_CurrentMap.TileSpacingY * m_CurrentMap.Tiles.Height;

      int scrollableWidthChars  = Math.Max( mapCharWidth,  viewCharWidth )  + MapScrollOverhangChars;
      int scrollableHeightChars = Math.Max( mapCharHeight, viewCharHeight ) + MapScrollOverhangChars;

      mapHScroll.Maximum = ( scrollableWidthChars - viewCharWidth ) / m_CurrentMap.TileSpacingX + 1;
      mapHScroll.Enabled = true;
      if ( m_CurEditorOffsetX > mapHScroll.Maximum )
      {
        m_CurEditorOffsetX = mapHScroll.Maximum;
      }

      mapVScroll.Minimum = 0;
      mapVScroll.Maximum = ( scrollableHeightChars - viewCharHeight ) / m_CurrentMap.TileSpacingY + 1;
      mapVScroll.Enabled = true;
      if ( m_CurEditorOffsetY > mapVScroll.Maximum )
      {
        m_CurEditorOffsetY = mapVScroll.Maximum;
      }
    }



    private string FormatExtraData( GR.Memory.ByteBuffer Data )
    {
      if ( Data.Length == 0 )
      {
        return "";
      }
      StringBuilder sb = new StringBuilder();

      for ( int i = 0; i < ( Data.Length + 7 ) / 8; ++i )
      {
        int     len = 8;
        if ( i * 8 + 8 > Data.Length )
        {
          len = (int)Data.Length - i * 8;
        }
        sb.AppendLine( Data.ToString( i * 8, len ) );
      }
      return sb.ToString();
    }



    private void comboMaps_SelectedIndexChanged( object sender, EventArgs e )
    {
      // Any current marker/entity selection belongs to the OLD m_CurrentMap,
      // so clear it before we swap the map reference. Otherwise the Delete
      // button could act on a marker that's no longer in the visible map.
      ClearMarkerEntitySelection();

      // Switching maps always cancels any "viewing a revision" mode — the
      // revisions list belongs to the previous map, not the new one. The
      // revisions combo will be repopulated below from the new map.
      m_IsViewingRevision = false;
      m_LiveMap = null;
      m_CurrentMap = null;

      // Persist the user's map choice so reopening the project lands them
      // on the same map. -1 ("none selected") writes through too — falling
      // back to "no preference" rather than the last good index keeps the
      // saved value honest.
      if ( ( m_MapProject != null )
      &&   ( m_MapProject.CurrentMapIndex != comboMaps.SelectedIndex ) )
      {
        m_MapProject.CurrentMapIndex = comboMaps.SelectedIndex;
        SetModified();
      }

      btnMapApply.Enabled = ( comboMaps.SelectedIndex != -1 );
      btnMapDelete.Enabled = ( comboMaps.SelectedIndex != -1 );
      btnMapClear.Enabled = ( comboMaps.SelectedIndex != -1 );
      btnSetStartMap.Enabled = ( comboMaps.SelectedIndex != -1 );

      if ( comboMaps.SelectedIndex == -1 )
      {
        comboTiles.Items.Clear();
        btnCopy.Enabled = false;
        btnMoveMapDown.Enabled = false;
        btnMoveMapUp.Enabled = false;
        RefreshRevisionsCombo();
        return;
      }
      m_CurrentMap = ( (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.SelectedItem ).second;
      // The map the user just selected becomes our editable "live" map.
      // Any revision-viewing state was already cleared above.
      m_LiveMap = m_CurrentMap;
      btnCopy.Enabled = true;

      btnMoveMapDown.Enabled  = ( ( comboMaps.Items.Count >= 2 ) && ( comboMaps.SelectedIndex + 1 < comboMaps.Items.Count ) );
      btnMoveMapUp.Enabled    = ( ( comboMaps.Items.Count >= 2 ) && ( comboMaps.SelectedIndex > 0 ) );

      m_SelectedTiles = new bool[m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height];

      editMapName.Text = m_CurrentMap.Name;
      editTileSpacingW.Text = m_CurrentMap.TileSpacingX.ToString();
      editTileSpacingH.Text = m_CurrentMap.TileSpacingY.ToString();
      editMapWidth.Text = m_CurrentMap.Tiles.Width.ToString();
      editMapHeight.Text = m_CurrentMap.Tiles.Height.ToString();
      comboTiles.ItemHeight = MapTileListEffectiveItemHeight;
      // Extra data is now edited via Tools → Edit extra data... — no
      // longer mirrored in a constantly-visible textbox here.
      comboMapMultiColor1.SelectedIndex = m_CurrentMap.AlternativeMultiColor1 + 1;
      comboMapMultiColor2.SelectedIndex = m_CurrentMap.AlternativeMultiColor2 + 1;
      comboMapBGColor.SelectedIndex = m_CurrentMap.AlternativeBackgroundColor + 1;
  comboMapAlternativeBGColor4.SelectedIndex = m_CurrentMap.AlternativeBGColor4 + 1;
  comboMapAlternativeMode.SelectedIndex = (int)m_CurrentMap.AlternativeMode + 1;
  
  dimSlider.Value = m_CurrentMap.MarkerDimOpacity;
  if ( m_MapProject.MarkerTypes.Count > 0 )
  {
     int index = m_MapProject.MarkerTypes.FindIndex( t => t.ID == m_CurrentMap.SelectedMarkerType );
     if ( index != -1 )
     {
       comboMarkerTypes.SelectedIndex = index + 1;
     }
     else
     {
       comboMarkerTypes.SelectedIndex = 0;
     }
  }
  else
  {
     comboMarkerTypes.SelectedIndex = 0;
  }
  UpdateMarkerControlsState();

  if ( m_MapProject.EntityTypes.Count > 0 )
  {
    int idx = m_MapProject.EntityTypes.FindIndex( t => t.ID == m_CurrentMap.SelectedEntityType );
    comboEntityTypes.SelectedIndex = ( idx >= 0 ) ? idx + 1 : 0;
  }
  else
  {
    comboEntityTypes.SelectedIndex = 0;
  }

      RecalcTileUsageInCurrentMap();

      // Re-fill comboRevisions from the new map's history and re-enable
      // edit-side controls (we always start in "(Current)" view mode after
      // a map switch).
      RefreshRevisionsCombo();
      SetMapEditingControlsEnabled( true );

      AdjustScrollbars();
      RedrawMap();
    }



    private void RecalcTileUsageInCurrentMap()
    {
      _TileUsage.Clear();
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        _TileUsage.Add( 0 );
      }
      if ( m_CurrentMap != null )
      {
        for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
        {
          for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
          {
            ++_TileUsage[m_CurrentMap.Tiles[i, j]];
          }
        }
      }
      comboTiles.Invalidate();
    }



    private void btnAddTile_Click( DecentForms.ControlBase Sender )
    {
      int w = GR.Convert.ToI32( editTileWidth.Text );
      int h = GR.Convert.ToI32( editTileHeight.Text );

      if ( ( w == 0 )
      ||   ( h == 0 ) )
      {
        return;
      }
      Formats.MapProject.Tile tile = new Formats.MapProject.Tile();
      tile.Chars.Resize( w, h );
      tile.Name = MakeTileNameUnique( editTileName.Text );

      int indexToInsertAt = m_MapProject.Tiles.Count;
      if ( listTileInfo.SelectedIndices.Count > 0 )
      {
        indexToInsertAt = listTileInfo.SelectedIndices[0] + 1;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileAdd( this, m_MapProject, indexToInsertAt ) );

      AddTile( indexToInsertAt, tile );
      listTileInfo.SelectedIndices.Clear();
      listTileInfo.SelectedIndices.Add( indexToInsertAt );
    }



    private void btnGetTileCount_Click( DecentForms.ControlBase Sender )
    {
      // Two independent passes:
      //   currentMapUsage[i] = number of cells in m_CurrentMap that
      //                        reference tile index i.
      //   projectUsage[i]    = same, summed across every map in the
      //                        project.
      // The "Used" column shows "current/project" so the user can see
      // both at a glance (e.g. "1/12" = used once in this map, twelve
      // times across all maps). When there's no current map, the
      // current count collapses to 0.
      int tileCount = m_MapProject.Tiles.Count;
      var currentMapUsage = new int[tileCount];
      var projectUsage    = new int[tileCount];

      foreach ( var map in m_MapProject.Maps )
      {
        bool isCurrent = ( map == m_CurrentMap );
        for ( int x = 0; x < map.Tiles.Width; ++x )
        {
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            int idx = map.Tiles[x, y];
            if ( ( idx < 0 ) || ( idx >= tileCount ) ) continue;
            ++projectUsage[idx];
            if ( isCurrent ) ++currentMapUsage[idx];
          }
        }
      }

      // Keep the cached _TileUsage in sync with the current map so
      // other call sites that read it (e.g. the comboTiles painter)
      // see fresh numbers after a manual recount.
      _TileUsage.Clear();
      for ( int i = 0; i < tileCount; ++i )
      {
        _TileUsage.Add( currentMapUsage[i] );
      }

      // Build the "unused" font once, lazily — bold version of the
      // listview's font, used for tiles whose total project usage is
      // zero. Keeping it scoped to this method means we don't have to
      // own the font's lifetime; CSListView's painter doesn't dispose
      // the SubItem.Font, so the same font instance can persist on
      // multiple rows without leaking.
      System.Drawing.Font unusedFont = null;

      listTileInfo.BeginUpdate();
      foreach ( ListViewItem item in listTileInfo.Items )
      {
        Formats.MapProject.Tile tile = (Formats.MapProject.Tile)item.Tag;
        if ( ( tile.Index < 0 )
        ||   ( tile.Index >= tileCount ) )
        {
          continue;
        }

        // SubItem 4 = "Used" column (after the new Preview column at 2
        // shifted Size and Used down to 3 and 4 respectively). Promote
        // the existing subitem to a CSListViewSubItem if it isn't one
        // already — the OverrideForeColor / Font tweaks below need it.
        var sub = item.SubItems[4];
        if ( !( sub is RetroDevStudio.Controls.CSListViewSubItem ) )
        {
          var promoted = new RetroDevStudio.Controls.CSListViewSubItem();
          item.SubItems.RemoveAt( 4 );
          item.SubItems.Insert( 4, promoted );
          sub = promoted;
        }
        var csSub = (RetroDevStudio.Controls.CSListViewSubItem)sub;

        int curUse = currentMapUsage[tile.Index];
        int prjUse = projectUsage[tile.Index];
        csSub.Text = curUse.ToString() + "/" + prjUse.ToString();

        if ( prjUse == 0 )
        {
          // Truly unused tile — red bold so it's easy to spot in a
          // long list. ProjectUsage == 0 implies currentMapUsage is
          // also 0 (both sums share the same zero-source loop), so
          // this is the "0/0" case the user asked for.
          if ( unusedFont == null )
          {
            unusedFont = new System.Drawing.Font( listTileInfo.Font, System.Drawing.FontStyle.Bold );
          }
          csSub.Font = unusedFont;
          csSub.OverrideForeColor = System.Drawing.Color.Red;
        }
        else
        {
          // Used at least once somewhere — restore default styling
          // so a tile that became used after a previous "0/0" run
          // doesn't keep the red bold.
          csSub.Font = listTileInfo.Font;
          csSub.OverrideForeColor = null;
        }
      }
      listTileInfo.EndUpdate();
      listTileInfo.Invalidate();

      comboTiles.Invalidate();
    }



    public void AddTile( int TileIndex, Formats.MapProject.Tile Tile )
    {
      m_MapProject.Tiles.Insert( TileIndex, Tile );
      Tile.Index = TileIndex;
      comboTiles.Items.Insert( TileIndex, new GR.Generic.Tupel<string, Formats.MapProject.Tile>( Tile.Name, Tile ) );

      ListViewItem item = new ListViewItem();

      item.Text = Tile.Index.ToString();
      // Preview column — empty text; thumbnail painted by DrawItemImage.
      item.SubItems.Add( "" );
      item.SubItems.Add( Tile.Name );
      item.SubItems.Add( Tile.Chars.Width.ToString() + "x" + Tile.Chars.Height.ToString() );
      item.SubItems.Add( "0" );
      item.Tag = Tile;
      item.ImageIndex = 0;

      listTileInfo.Items.Insert( TileIndex, item );

      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        m_MapProject.Tiles[i].Index = i;
      }
      for ( int i = TileIndex; i < listTileInfo.Items.Count; ++i )
      {
        listTileInfo.Items[i].Text = i.ToString();
      }

      foreach ( var map in m_MapProject.Maps )
      {
        for ( int i = 0; i < map.Tiles.Width; ++i )
        {
          for ( int j = 0; j < map.Tiles.Height; ++j )
          {
            if ( map.Tiles[i, j] >= TileIndex )
            {
              ++map.Tiles[i, j];
            }
          }
        }
      }

      // auto-select tile
      listTileInfo.SelectedIndices.Clear();
      listTileInfo.SelectedIndices.Add( TileIndex );
      listTileInfo.EnsureVisible( TileIndex );
      RedrawMap();
      RedrawTile();
      SetModified();
    }



    private void listTileInfo_SelectedIndexChanged( object sender, EventArgs e )
    {
      m_CurrentEditedTile = null;
      m_CurrentTileChar = null;
      listTileChars.Items.Clear();

      btnTileApply.Enabled = ( listTileInfo.SelectedIndices.Count != 0 );
      btnTileDelete.Enabled = ( listTileInfo.SelectedIndices.Count != 0 );
      btnTileClone.Enabled = ( listTileInfo.SelectedIndices.Count != 0 );

      btnMoveTileUp.Enabled = ( ( listTileInfo.Items.Count > 1 ) && ( listTileInfo.SelectedIndices.Count > 0 ) && ( listTileInfo.SelectedIndices[0] > 0 ) );
      btnMoveTileDown.Enabled = ( ( listTileInfo.Items.Count > 1 ) && ( listTileInfo.SelectedIndices.Count > 0 ) && ( listTileInfo.SelectedIndices[0] + 1 < listTileInfo.Items.Count ) );

      if ( listTileInfo.SelectedIndices.Count == 0 )
      {
        btnCopyTileCharToNextIncreased.Enabled  = false;
        btnSetNextTileChar.Enabled              = false;
        panelCharColors.Enabled                 = false;

        RedrawTile();
        return;
      }

      m_CurrentEditedTile = (Formats.MapProject.Tile)listTileInfo.SelectedItems[0].Tag;

      m_ApplyingTileSettings = true;
      checkTilePassable.Checked = m_CurrentEditedTile.Passable;
      checkNotExportedOnMap.Checked = m_CurrentEditedTile.NotExportedOnMap;
      m_ApplyingTileSettings = false;

      editTileWidth.Text = m_CurrentEditedTile.Chars.Width.ToString();
      editTileHeight.Text = m_CurrentEditedTile.Chars.Height.ToString();
      editTileName.Text = m_CurrentEditedTile.Name;
      editTileGroupId.Text = m_CurrentEditedTile.GroupId.ToString();

      UpdateCurrentTileCharacterList();

      btnCopyTileCharToNextIncreased.Enabled  = ( listTileChars.SelectedIndices.Count != 0 );
      btnSetNextTileChar.Enabled              = ( listTileChars.SelectedIndices.Count != 0 );

      if ( listTileChars.Items.Count > 0 )
      {
        listTileChars.SelectedIndices.Add( 0 );
      }

      RedrawTile();
    }



    private void UpdateCurrentTileCharacterList()
    {
      if ( m_CurrentEditedTile == null )
      {
        return;
      }
      listTileChars.BeginUpdate();
      listTileChars.Items.Clear();
      for ( int j = 0; j < m_CurrentEditedTile.Chars.Height; ++j )
      {
        for ( int i = 0; i < m_CurrentEditedTile.Chars.Width; ++i )
        {
          Formats.MapProject.TileChar character = m_CurrentEditedTile.Chars[i, j];

          ListViewItem item = new ListViewItem( ( i + j * m_CurrentEditedTile.Chars.Width ).ToString() );
          item.SubItems.Add( character.Character.ToString() );
          item.SubItems.Add( character.Color.ToString() );
          item.Tag = character;

          listTileChars.Items.Add( item );
        }
      }
      listTileChars.EndUpdate();
    }



    private void RedrawTile()
    {
      pictureTileDisplay.DisplayPage.Box( 0, 0, pictureTileDisplay.DisplayPage.Width, pictureTileDisplay.DisplayPage.Height, (uint)comboTileBackground.SelectedIndex );
      if ( m_CurrentEditedTile != null )
      {
        for ( int j = 0; j < m_CurrentEditedTile.Chars.Height; ++j )
        {
          for ( int i = 0; i < m_CurrentEditedTile.Chars.Width; ++i )
          {
            Formats.MapProject.TileChar character = m_CurrentEditedTile.Chars[i, j];

            DrawCharImage( pictureTileDisplay.DisplayPage, i * 8, j * 8, character.Character, character.Color );
          }
        }
      }
      pictureTileDisplay.Invalidate();
    }



    private void listTileChars_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_CurrentEditedTile == null )
      {
        btnCopyTileCharToNextIncreased.Enabled  = false;
        btnSetNextTileChar.Enabled              = false;
        panelCharColors.Enabled                 = false;
        return;
      }
      m_CurrentTileChar = null;
      if ( listTileChars.SelectedItems.Count == 0 )
      {
        btnCopyTileCharToNextIncreased.Enabled  = false;
        btnSetNextTileChar.Enabled              = false;
        panelCharColors.Enabled                 = false;
        return;
      }
      m_CurrentTileChar = (Formats.MapProject.TileChar)listTileChars.SelectedItems[0].Tag;

      btnCopyTileCharToNextIncreased.Enabled  = ( listTileChars.SelectedIndices[0] + 1 < listTileChars.Items.Count );
      btnSetNextTileChar.Enabled              = ( listTileChars.SelectedIndices[0] + 1 < listTileChars.Items.Count );
      panelCharColors.Enabled                 = true;

      panelCharacters.SelectedIndex = m_CurrentTileChar.Character;
      m_CurrentColor = m_CurrentTileChar.Color;
      RedrawColorChooser();
      panelCharColors.Invalidate();
    }



    private void btnMapAdd_Click( object sender, EventArgs e )
    {
      int w = GR.Convert.ToI32( editMapWidth.Text );
      int h = GR.Convert.ToI32( editMapHeight.Text );
      int tw = GR.Convert.ToI32( editTileSpacingW.Text );
      int th = GR.Convert.ToI32( editTileSpacingH.Text );

      if ( ( w == 0 )
      ||   ( h == 0 )
      ||   ( tw == 0 )
      ||   ( th == 0 ) )
      {
        return;
      }

      Formats.MapProject.Map map = new Formats.MapProject.Map();

      map.TileSpacingX = tw;
      map.TileSpacingY = th;
      map.Tiles.Resize( w, h );
      // Per-character color-override layer: char-grid sized (w × spacingX,
      // h × spacingY). All -1 = "no override anywhere". The renderer and
      // exporter read this per char.
      map.TileColorOverrides.Resize( w * tw, h * th );
      ResetColorOverrides( map.TileColorOverrides );
      // Per-character "blocked" override layer — same shape. Resize alone
      // is enough: zero-init bool = false = no-override sentinel, no
      // explicit reset needed.
      map.CharBlockedOverrides.Resize( w * tw, h * th );
      map.Name = editMapName.Text;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapAdd( this, m_MapProject, m_MapProject.Maps.Count ) );

      AddMap( m_MapProject.Maps.Count, map );
    }



    /// <summary>
    /// Set every cell in <paramref name="overrides"/> to -1 (no override).
    /// Used right after a fresh Resize to give the layer the same
    /// "everything default" baseline a brand-new Map gets.
    /// </summary>
    private static void ResetColorOverrides( GR.Game.Layer<int> overrides )
    {
      for ( int y = 0; y < overrides.Height; ++y )
      {
        for ( int x = 0; x < overrides.Width; ++x )
        {
          overrides[x, y] = -1;
        }
      }
    }



    /// <summary>
    /// Resize the override layer while keeping existing cells' values and
    /// initializing any newly-exposed cells to -1. <see cref="GR.Game.Layer{int}.Resize"/>
    /// preserves overlap but the new region defaults to <c>0</c>, which
    /// would mean "override to color 0 (black)" rather than "no override".
    /// </summary>
    private static void ResizeColorOverridesPreservingDefaults(
      GR.Game.Layer<int> overrides, int newWidth, int newHeight )
    {
      int oldW = overrides.Width;
      int oldH = overrides.Height;
      overrides.Resize( newWidth, newHeight );
      // Cells that were inside the old footprint keep whatever the user
      // had set (-1 or a real color). Cells outside the old footprint are
      // freshly exposed and get the proper -1 sentinel.
      for ( int y = 0; y < newHeight; ++y )
      {
        for ( int x = 0; x < newWidth; ++x )
        {
          if ( ( x >= oldW )
          ||   ( y >= oldH ) )
          {
            overrides[x, y] = -1;
          }
        }
      }
    }



    public void AddMap( int MapIndex, Formats.MapProject.Map Map )
    {
      m_MapProject.Maps.Insert( MapIndex, Map );

      comboMaps.Items.Insert( MapIndex, new GR.Generic.Tupel<string, Formats.MapProject.Map>( FormatMapDisplayName( MapIndex, Map ), Map ) );
      comboMaps.Enabled = true;

      for ( int i = 0; i < comboMaps.Items.Count; ++i )
      {
        GR.Generic.Tupel<string, Formats.MapProject.Map>    mapPair = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[i];

        mapPair.first = FormatMapDisplayName( i, mapPair.second );

        // force name update
        comboMaps.Items[i] = comboMaps.Items[i];
      }

      SetModified();

      AdjustScrollbars();
      RedrawMap();
    }



    private void btnMapApply_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      int w = GR.Convert.ToI32( editMapWidth.Text );
      int h = GR.Convert.ToI32( editMapHeight.Text );
      int tw = GR.Convert.ToI32( editTileSpacingW.Text );
      int th = GR.Convert.ToI32( editTileSpacingH.Text );

      if ( ( w == 0 )
      ||   ( h == 0 )
      ||   ( tw == 0 )
      ||   ( th == 0 ) )
      {
        return;
      }

      if ( ( w == m_CurrentMap.Tiles.Width )
      &&   ( h == m_CurrentMap.Tiles.Height )
      &&   ( tw == m_CurrentMap.TileSpacingX )
      &&   ( th == m_CurrentMap.TileSpacingY )
      &&   ( editMapName.Text == m_CurrentMap.Name ) )
      {
        return;
      }

      // Unterschied!
      bool  firstUndo = true;
      bool  sizeChanged = ( ( w != m_CurrentMap.Tiles.Width )
                         || ( h != m_CurrentMap.Tiles.Height ) );
      // UndoMapSizeChange now snapshots the per-char override layers
      // (TileColorOverrides / CharBlockedOverrides) in addition to the tile
      // grid. Those layers are resized — and on a spacing change wiped — for
      // EITHER a size change OR a spacing change, so the size-undo task must
      // run in both cases; otherwise undoing a spacing change silently loses
      // every per-char colour and blocked override. (spacingChanged is read
      // again below, before TileSpacingX/Y are overwritten.)
      bool  spacingChanged = ( ( tw != m_CurrentMap.TileSpacingX )
                           || ( th != m_CurrentMap.TileSpacingY ) );
      if ( sizeChanged || spacingChanged )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapSizeChange( this, m_CurrentMap, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );
        firstUndo = false;
      }
      if ( ( tw != m_CurrentMap.TileSpacingX )
      ||   ( th != m_CurrentMap.TileSpacingY )
      ||   ( editMapName.Text != m_CurrentMap.Name ) )
      {
        if ( firstUndo )
        {
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );
        }
        else
        {
          DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );
        }
      }



      // spacingChanged was computed above (it also gates the size-undo task);
      // it must be read here, BEFORE we overwrite TileSpacingX/Y.
      m_CurrentMap.TileSpacingX = tw;
      m_CurrentMap.TileSpacingY = th;

      // If the map is shrinking, snapshot + drop any entities that fall outside
      // the new bounds. Entities are strictly in-map content, so entities whose
      // coords sit past the new width/height are removed as a cascade.
      if ( sizeChanged )
      {
        bool anyOutOfBounds = m_CurrentMap.Entities.Any(
          en => ( en.X >= w ) || ( en.Y >= h ) );
        if ( anyOutOfBounds )
        {
          DocumentInfo.UndoManager.AddGroupedUndoTask(
            new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
          m_CurrentMap.Entities.RemoveAll(
            en => ( en.X >= w ) || ( en.Y >= h ) );
          UpdateEntityCountLabel();
        }
      }

      m_CurrentMap.Tiles.Resize( w, h );
      // TileColorOverrides is char-grid sized (w × spacingX, h × spacingY).
      // On a width/height-only change we preserve overrides in the overlap
      // region. On a spacing change we wipe the layer — re-mapping the
      // per-char overrides across a different per-tile char block has no
      // sensible default behaviour.
      int newCharW = w * tw;
      int newCharH = h * th;
      if ( spacingChanged )
      {
        m_CurrentMap.TileColorOverrides.Resize( newCharW, newCharH );
        ResetColorOverrides( m_CurrentMap.TileColorOverrides );
        // Spacing change wipes the blocked layer for the same reason —
        // re-mapping per-char overrides across a different per-tile char
        // block has no sensible default behaviour.
        m_CurrentMap.CharBlockedOverrides.Resize( newCharW, newCharH );
        m_CurrentMap.CharBlockedOverrides.Fill( false );
      }
      else
      {
        ResizeColorOverridesPreservingDefaults( m_CurrentMap.TileColorOverrides, newCharW, newCharH );
        // Width/height-only change: Layer<bool>.Resize zero-fills new
        // cells (false = no-override, the natural default) and preserves
        // existing values in the overlap. No helper needed.
        m_CurrentMap.CharBlockedOverrides.Resize( newCharW, newCharH );
      }
      m_CurrentMap.Name = editMapName.Text;

      m_SelectedTiles = new bool[w, h];

      // update name in combo
      int index = 0;
      foreach ( GR.Generic.Tupel<string, Formats.MapProject.Map> mapInfo in comboMaps.Items )
      {
        if ( mapInfo.second == m_CurrentMap )
        {
          mapInfo.first = FormatMapDisplayName( index, m_CurrentMap );
          comboMaps.Items[index] = comboMaps.Items[index];
          break;
        }
        ++index;
      }
      RecalcTileUsageInCurrentMap();
      AdjustScrollbars();
      RedrawMap();
      SetModified();
    }



    /// <summary>
    /// Format a map's row in the Current Map dropdown: "★ N: Name" for the
    /// project's StartMapIndex, "N: Name" otherwise. Centralised so the four
    /// sites that build dropdown rows (open project, add map, rename,
    /// reindex-after-move) stay in sync — touching the star convention in
    /// one place updates them all.
    /// </summary>
    private string FormatMapDisplayName( int Index, Formats.MapProject.Map Map )
    {
      string prefix = ( ( m_MapProject != null ) && ( Index == m_MapProject.StartMapIndex ) ) ? "★ " : "";
      return prefix + Index.ToString() + ": " + Map.Name;
    }



    /// <summary>
    /// Rebuild every visible label in the Current Map dropdown by walking the
    /// items and re-running <see cref="FormatMapDisplayName"/>. Called after
    /// StartMapIndex changes so the star marker moves to the new start map,
    /// and after undo/redo of a start-map change.
    /// </summary>
    public void RefreshMapListDisplay()
    {
      for ( int i = 0; i < comboMaps.Items.Count; ++i )
      {
        var mapPair = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[i];
        mapPair.first = FormatMapDisplayName( i, mapPair.second );
        // Force the combo to repaint the changed row.
        comboMaps.Items[i] = comboMaps.Items[i];
      }
    }



    private void btnSetStartMap_Click( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      int selected = comboMaps.SelectedIndex;
      if ( ( selected < 0 ) || ( selected >= m_MapProject.Maps.Count ) ) return;
      if ( m_MapProject.StartMapIndex == selected ) return;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStartMapIndexChange( this, m_MapProject ) );
      m_MapProject.StartMapIndex = selected;
      RefreshMapListDisplay();
      SetModified();
    }



    /// <summary>
    /// Resize the current map by ±1 in width or height. Implemented by
    /// writing the desired new value into <see cref="editMapWidth"/> /
    /// <see cref="editMapHeight"/> and calling
    /// <see cref="btnMapApply_Click"/> — same code path the user takes
    /// when typing a new size and pressing Apply, so undo, entity-cascade,
    /// and per-character override-layer resize all run for free. Reads the
    /// CURRENT applied dimension from <c>m_CurrentMap</c> rather than the
    /// textbox so a stale typed value the user hasn't applied yet doesn't
    /// throw the +1/-1 off; non-applied edits in the OTHER textboxes
    /// (spacing / name) still commit through Apply, matching what would
    /// happen if the user typed a new width and clicked Apply themselves.
    /// Lower-clamped at 1 — Apply silently no-ops on 0, but exposing a
    /// "shrink to nothing" gesture would be confusing.
    /// </summary>
    private void ApplyMapSizeDelta( int dW, int dH )
    {
      if ( m_CurrentMap == null ) return;
      int newW = m_CurrentMap.Tiles.Width  + dW;
      int newH = m_CurrentMap.Tiles.Height + dH;
      if ( newW < 1 ) newW = 1;
      if ( newH < 1 ) newH = 1;
      if ( ( newW == m_CurrentMap.Tiles.Width )
      &&   ( newH == m_CurrentMap.Tiles.Height ) )
      {
        // Already at the clamp boundary — nothing to do; Apply would
        // early-out anyway (no change).
        return;
      }
      editMapWidth.Text  = newW.ToString();
      editMapHeight.Text = newH.ToString();
      btnMapApply_Click( this, EventArgs.Empty );
    }



    private void btnMapWidthInc_Click( object sender, EventArgs e )
    {
      ApplyMapSizeDelta( 1, 0 );
    }



    private void btnMapWidthDec_Click( object sender, EventArgs e )
    {
      ApplyMapSizeDelta( -1, 0 );
    }



    private void btnMapHeightInc_Click( object sender, EventArgs e )
    {
      ApplyMapSizeDelta( 0, 1 );
    }



    private void btnMapHeightDec_Click( object sender, EventArgs e )
    {
      ApplyMapSizeDelta( 0, -1 );
    }



    private void mapHScroll_Scroll( DecentForms.ControlBase Sender )
    {
      if ( m_CurEditorOffsetX != mapHScroll.Value )
      {
        m_CurEditorOffsetX = mapHScroll.Value;
        RedrawMap();
        Redraw();
      }
    }



    private void mapVScroll_Scroll( DecentForms.ControlBase Sender )
    {
      if ( m_CurEditorOffsetY != mapVScroll.Value )
      {
        m_CurEditorOffsetY = mapVScroll.Value;
        RedrawMap();
        Redraw();
      }
    }



    private void comboTiles_DrawItem( object sender, DrawItemEventArgs e )
    {
      // Reserve a strip at the bottom of each row for the inter-row
      // separator. innerBounds is the actual content rect; the strip
      // beneath is filled with the user-configured separator color
      // after the content paints. sep == 0 means "no separator", which
      // restores the original packed-rows behavior.
      int sep = 0;
      uint sepARGB = 0;
      if ( Core?.Settings != null )
      {
        sep = Math.Max( 0, Core.Settings.MapTileListRowSeparatorHeight );
        sepARGB = Core.Settings.MapTileListRowSeparatorColorARGB;
      }
      System.Drawing.Rectangle innerBounds = new System.Drawing.Rectangle(
        e.Bounds.X, e.Bounds.Y, e.Bounds.Width, Math.Max( 1, e.Bounds.Height - sep ) );

      if ( Core?.Theming != null )
        Core.Theming.DrawThemedBackground( e, comboTiles );
      else
        e.DrawBackground();
      if ( ( e.Index < 0 )
      ||   ( e.Index >= comboTiles.Items.Count ) )
      {
        // Paint the separator strip even on empty/invalid rows so
        // there's no visual gap that betrays uneven painting.
        if ( sep > 0 )
        {
          using ( var b = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( unchecked( (int)sepARGB ) ) ) )
          {
            e.Graphics.FillRectangle( b, e.Bounds.X, e.Bounds.Bottom - sep, e.Bounds.Width, sep );
          }
        }
        e.DrawFocusRectangle();
        return;
      }

      var tileInfo = (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.Items[e.Index];
      Formats.MapProject.Tile tile = tileInfo.second;
      if ( tile == null )
      {
        if ( sep > 0 )
        {
          using ( var b = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( unchecked( (int)sepARGB ) ) ) )
          {
            e.Graphics.FillRectangle( b, e.Bounds.X, e.Bounds.Bottom - sep, e.Bounds.Width, sep );
          }
        }
        e.DrawFocusRectangle();
        return;
      }

      int previewPadding = MapTilePreviewPadding;
      int previewSize = Math.Max( 1, innerBounds.Height - previewPadding * 2 );
      System.Drawing.Rectangle previewRect = new System.Drawing.Rectangle( innerBounds.Left + previewPadding,
                                                                           innerBounds.Top + ( innerBounds.Height - previewSize ) / 2,
                                                                           previewSize,
                                                                           previewSize );

      if ( ( tile.Chars.Width > 0 )
      &&   ( tile.Chars.Height > 0 ) )
      {
        GR.Image.FastImage memImage = new GR.Image.FastImage( tile.Chars.Width * 8, tile.Chars.Height * 8, GR.Drawing.PixelFormat.Format32bppRgb );
        PaletteManager.ApplyPalette( memImage );

        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            Formats.MapProject.TileChar character = tile.Chars[i, j];

            DrawCharImage( memImage, i * 8, j * 8, character.Character, character.Color );
          }
        }

        IntPtr hdc = e.Graphics.GetHdc();
        try
        {
          memImage.DrawToHDC( hdc, previewRect );
        }
        finally
        {
          e.Graphics.ReleaseHdc();
          memImage.Dispose();
        }
      }

      string label = e.Index.ToString() + ": " + tile.Name;
      int textX = previewRect.Right + 6;
      int textY = innerBounds.Top + ( innerBounds.Height - comboTiles.Font.Height ) / 2;
      System.Drawing.Brush textBrush = new System.Drawing.SolidBrush( comboTiles.ForeColor );
      e.Graphics.DrawString( label, comboTiles.Font, textBrush, textX, textY );
      // Paint the inter-row separator strip as the very last step so
      // it overlays nothing else (focus rect aside). Skipped when
      // sep == 0 to keep the no-spacing config a true no-op.
      if ( sep > 0 )
      {
        using ( var b = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( unchecked( (int)sepARGB ) ) ) )
        {
          e.Graphics.FillRectangle( b, e.Bounds.X, e.Bounds.Bottom - sep, e.Bounds.Width, sep );
        }
      }
      e.DrawFocusRectangle();
    }



    private void comboTiles_SelectedIndexChanged( object sender, EventArgs e )
    {
      m_CurrentEditorTile = null;
      if ( comboTiles.SelectedIndex == -1 )
      {
        return;
      }
      m_CurrentEditorTile = ( (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.SelectedItem ).second;

      // Reset the override dropdown to "Default" so subsequent
      // placement uses the tile's own per-char colors (the user's
      // intent when picking a fresh tile from the list — without
      // this, a leftover override color from a previous tile would
      // silently keep stamping its color over the new tile's chars).
      // Suppress the auto-apply path so this reset doesn't ricochet
      // into the right-click-selected tile on the map.
      // Also skipped when the tile change came from the right-click-
      // on-map eyedropper (m_SuppressTilePickerOverrideReset) — that
      // path should mirror the tile but leave the override color
      // alone. And skipped entirely when the user has locked the
      // placement color (the "Lock color" toolbar toggle): the whole
      // point of the lock is that picking a new tile keeps the chosen
      // color instead of snapping back to Default.
      if ( ( comboTilePlacementColor != null )
      &&   ( comboTilePlacementColor.SelectedIndex != 0 )
      &&   ( !m_SuppressTilePickerOverrideReset )
      &&   ( ( m_MapProject == null ) || ( !m_MapProject.LockTilePlacementColor ) ) )
      {
        m_SuppressTilePlacementColorAutoApply = true;
        try
        {
          comboTilePlacementColor.SelectedIndex = 0;
        }
        finally
        {
          m_SuppressTilePlacementColorAutoApply = false;
        }
      }
    }



    /// <summary>
    /// Per-row tile thumbnail painter for listTileInfo on the Tiles tab.
    /// Mirrors the comboTiles owner-draw pattern: build a FastImage at
    /// the tile's native character resolution, render every char into it
    /// using the project's palette, then blit it into the row's reserved
    /// image slot at MapTileListThumbnailSize × MapTileListThumbnailSize.
    /// The reserved slot is sized via the SmallImageList placeholder.
    /// </summary>
    private void listTileInfo_DrawItemImage( System.Drawing.Graphics g, int x, int y,
                                             ListViewItem item, ListViewItem.ListViewSubItem subItem )
    {
      if ( g == null )
      {
        return;
      }
      var tile = item?.Tag as Formats.MapProject.Tile;
      if ( ( tile == null )
      ||   ( tile.Chars.Width <= 0 )
      ||   ( tile.Chars.Height <= 0 ) )
      {
        return;
      }

      int rowH = listTileInfo.SmallImageList?.ImageSize.Height ?? MapTileListThumbnailSize;
      int previewSize = Math.Max( 1, rowH - MapTilePreviewPadding * 2 );
      var previewRect = new System.Drawing.Rectangle(
        x,
        y + ( rowH - previewSize ) / 2,
        previewSize,
        previewSize );

      using ( var memImage = new GR.Image.FastImage(
        tile.Chars.Width * 8, tile.Chars.Height * 8,
        GR.Drawing.PixelFormat.Format32bppRgb ) )
      {
        PaletteManager.ApplyPalette( memImage );
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            var ch = tile.Chars[i, j];
            DrawCharImage( memImage, i * 8, j * 8, ch.Character, ch.Color );
          }
        }
        IntPtr hdc = g.GetHdc();
        try
        {
          memImage.DrawToHDC( hdc, previewRect );
        }
        finally
        {
          g.ReleaseHdc();
        }
      }
    }

    private void tabMapEditor_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( tabMapEditor.SelectedPage == tabEditor )
      {
        RefreshMapTileList();
      }
      // Persist the user's last-visited tab so reopening the project
      // lands on the same page. Mark the project modified so the change
      // gets saved with the next Save. Guard against the early
      // SelectedIndexChanged that fires during InitializeComponent
      // (before m_MapProject is wired up via OpenProject) — at that
      // point we don't want to dirty a freshly-opened or empty project.
      if ( m_MapProject != null && tabMapEditor.SelectedIndex >= 0 )
      {
        if ( m_MapProject.LastSelectedTabIndex != tabMapEditor.SelectedIndex )
        {
          m_MapProject.LastSelectedTabIndex = tabMapEditor.SelectedIndex;
          SetModified();
        }
      }
    }



    private void comboBackground_SelectedIndexChanged_1( object sender, EventArgs e )
    {
      m_MapProject.BackgroundColor = comboTileBackground.SelectedIndex;
      FullRebuild();
    }



    private void FullRebuild()
    {
      for ( int i = 0; i < m_MapProject.Charset.TotalNumberOfCharacters; ++i )
      {
        RebuildCharImage( i );
        if ( i < panelCharacters.Items.Count )
        {
          panelCharacters.Items[i].MemoryImage = m_MapProject.Charset.Characters[i].Tile.Image;
        }
      }
      panelCharacters.Invalidate();

      SetModified();
      RedrawTile();
      RedrawColorChooser();
      RedrawMap();
    }



    private void checkTilePassable_CheckedChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentEditedTile == null )
      ||   ( m_ApplyingTileSettings ) )
      {
        return;
      }
      bool    firstUndo = true;

      foreach ( ListViewItem item in listTileInfo.SelectedItems )
      {
        Formats.MapProject.Tile tile = (Formats.MapProject.Tile)item.Tag;

        if ( tile.Passable != checkTilePassable.Checked )
        {
          if ( firstUndo )
          {
            DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, tile.Index ) );
            firstUndo = false;
          }
          else
          {
            DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, tile.Index ) );
          }
          tile.Passable = checkTilePassable.Checked;
          SetModified();
        }
      }
    }

    private void checkNotExportedOnMap_CheckedChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentEditedTile == null )
      ||   ( m_ApplyingTileSettings ) )
      {
        return;
      }
      bool    firstUndo = true;

      foreach ( ListViewItem item in listTileInfo.SelectedItems )
      {
        Formats.MapProject.Tile tile = (Formats.MapProject.Tile)item.Tag;

        if ( tile.NotExportedOnMap != checkNotExportedOnMap.Checked )
        {
          if ( firstUndo )
          {
            DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, tile.Index ) );
            firstUndo = false;
          }
          else
          {
            DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, tile.Index ) );
          }
          tile.NotExportedOnMap = checkNotExportedOnMap.Checked;
          SetModified();
        }
      }
    }


    private void btnTileApply_Click( DecentForms.ControlBase Sender )
    {
      if ( m_CurrentEditedTile == null )
      {
        return;
      }
      bool    modified = false;
      if ( m_CurrentEditedTile.Name != editTileName.Text )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
        modified = true;

        m_CurrentEditedTile.Name = editTileName.Text;

        // SubItem 2 = "Name" column (Preview now sits at index 1).
        listTileInfo.SelectedItems[0].SubItems[2].Text = m_CurrentEditedTile.Name;
        GR.Generic.Tupel<string, Formats.MapProject.Tile>      comboItem = (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.Items[listTileInfo.SelectedIndices[0]];
        comboItem.first = m_CurrentEditedTile.Name;
        SetModified();
      }

      int groupId = GR.Convert.ToI32( editTileGroupId.Text );
      if ( m_CurrentEditedTile.GroupId != groupId )
      {
        if ( !modified )
        {
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
          modified = true;
        }
        m_CurrentEditedTile.GroupId = groupId;
        SetModified();
      }

      int w = GR.Convert.ToI32( editTileWidth.Text );
      int h = GR.Convert.ToI32( editTileHeight.Text );

      if ( ( m_CurrentEditedTile.Chars.Width != w )
      ||   ( m_CurrentEditedTile.Chars.Height != h ) )
      {
        if ( !modified )
        {
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
          modified = true;
        }

        m_CurrentEditedTile.Chars.Resize( w, h );
        // SubItem 3 = "Size" column (Preview column shifted Size from 2 to 3).
        listTileInfo.SelectedItems[0].SubItems[3].Text = w.ToString() + "x" + h.ToString();
        listTileInfo_SelectedIndexChanged( null, null );
        SetModified();
      }
    }



    private void btnTileDelete_Click( DecentForms.ControlBase Sender )
    {
      if ( listTileInfo.SelectedIndices.Count == 0 )
      {
        return;
      }
      var selectedIndices = listTileInfo.SelectedIndices;
      var indicesToRemove = new List<int>();
      for ( int i = 0; i < selectedIndices.Count; ++i )
      {
        indicesToRemove.Add( selectedIndices[i] );
      }
      if ( indicesToRemove.Count > 0 )
      {
        for ( int i = 0; i < indicesToRemove.Count; ++i )
        {
          int   indexToRemove = indicesToRemove[indicesToRemove.Count - 1 - i];

          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileRemove( this, m_MapProject, indexToRemove ), i == 0 );
          RemoveTile( indexToRemove );
        }
      }
    }



    public void RemoveTile( int TileIndex )
    {
      // The tile being removed is still in the list at this point
      // (RemoveAt happens after the per-map sweep below). Capture its
      // char-footprint up-front so the override-clear loop knows exactly
      // how many chars each affected cell rendered. Footprint is
      // max(spacing, Chars dims) — when spacing < Chars (e.g. spacing=1
      // with a 2x2 tile) the tile renders 4 chars across 4 slots, so a
      // spacing²=1 clear would leave 3 stale overrides per cell.
      int removedFootprintX = 1;
      int removedFootprintY = 1;
      if ( ( TileIndex >= 0 ) && ( TileIndex < m_MapProject.Tiles.Count ) )
      {
        var removedTile = m_MapProject.Tiles[TileIndex];
        removedFootprintX = removedTile.Chars.Width;
        removedFootprintY = removedTile.Chars.Height;
      }

      // remove from all maps
      foreach ( var map in m_MapProject.Maps )
      {
        int clrFootprintX = ( removedFootprintX > map.TileSpacingX ) ? removedFootprintX : map.TileSpacingX;
        int clrFootprintY = ( removedFootprintY > map.TileSpacingY ) ? removedFootprintY : map.TileSpacingY;
        for ( int i = 0; i < map.Tiles.Width; ++i )
        {
          for ( int j = 0; j < map.Tiles.Height; ++j )
          {
            int tile = map.Tiles[i, j];
            if ( tile > TileIndex )
            {
              map.Tiles[i, j] = tile - 1;
            }
            else if ( tile == TileIndex )
            {
              map.Tiles[i, j] = 0;
              // The cell just became empty — drop the per-character
              // overrides for every char this tile actually rendered so
              // we don't carry stale tint forward into the exported
              // color grid (or onto whatever the user paints over next).
              int charBaseX = i * map.TileSpacingX;
              int charBaseY = j * map.TileSpacingY;
              for ( int dy = 0; dy < clrFootprintY; ++dy )
              {
                for ( int dx = 0; dx < clrFootprintX; ++dx )
                {
                  int cx = charBaseX + dx;
                  int cy = charBaseY + dy;
                  if ( ( cx < map.TileColorOverrides.Width )
                  &&   ( cy < map.TileColorOverrides.Height ) )
                  {
                    map.TileColorOverrides[cx, cy] = -1;
                  }
                  // Same reasoning for the blocked-override layer:
                  // the tile is gone, so any per-char override that
                  // applied to its footprint is now stale.
                  // UndoMapTileRemove snapshots both layers (see
                  // _BlockedOverrideSnapshots) so undo restores them
                  // alongside the tile.
                  if ( ( cx < map.CharBlockedOverrides.Width )
                  &&   ( cy < map.CharBlockedOverrides.Height ) )
                  {
                    map.CharBlockedOverrides[cx, cy] = false;
                  }
                }
              }
            }
          }
        }
      }

      // Entity types reference tiles by index. Mirror the same shift
      // that we just applied to map cells: indices > TileIndex slide
      // down by one; indices == TileIndex (whose tile is gone) become
      // -1, which the rendering loop's `TileIndex < 0` guard treats as
      // "no tile" and skips the entity. Better to surface a deleted
      // entity-tile binding as an invisible entity than to silently
      // re-bind it to whatever shifted into the freed slot.
      foreach ( var et in m_MapProject.EntityTypes )
      {
        if ( et.TileIndex == TileIndex )
        {
          et.TileIndex = -1;
        }
        else if ( et.TileIndex > TileIndex )
        {
          --et.TileIndex;
        }
      }

      m_MapProject.Tiles.RemoveAt( TileIndex );
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        m_MapProject.Tiles[i].Index = i;
      }

      // The painting selection (m_CurrentEditorTile) may still hold a
      // reference to the just-removed Tile object. Its .Index field
      // wasn't touched by the renumbering loop above (only surviving
      // tiles got renumbered), so reading it for placement would write
      // a stale index into m_CurrentMap.Tiles[,]. Drop the reference
      // here; the comboTiles.SelectedIndex assignment below rebinds it
      // deterministically through comboTiles_SelectedIndexChanged.
      if ( ( m_CurrentEditorTile != null )
      &&   ( !m_MapProject.Tiles.Contains( m_CurrentEditorTile ) ) )
      {
        m_CurrentEditorTile = null;
      }

      listTileInfo.Items.RemoveAt( TileIndex );
      for ( int i = TileIndex; i < listTileInfo.Items.Count; ++i )
      {
        listTileInfo.Items[i].Text = i.ToString();
      }
      listTileInfo_SelectedIndexChanged( null, null );
      comboTiles.Items.RemoveAt( TileIndex );

      // Re-establish a deterministic comboTiles selection so the painting
      // tile is bound to a real, current Tile via the SelectedIndexChanged
      // handler. Relying on WinForms ListBox's auto-shift behavior was
      // fragile — it doesn't always fire the event when the index value
      // happens to land on a different item, leaving m_CurrentEditorTile
      // pointing at a now-deleted Tile object (the original wrong-tile
      // placement bug).
      if ( comboTiles.Items.Count > 0 )
      {
        int newSelection = Math.Min( TileIndex, comboTiles.Items.Count - 1 );
        if ( comboTiles.SelectedIndex != newSelection )
        {
          comboTiles.SelectedIndex = newSelection;
        }
        else
        {
          // Same numeric index but possibly a different underlying tile —
          // re-fire the handler manually to ensure m_CurrentEditorTile
          // gets the current Tile object.
          comboTiles_SelectedIndexChanged( comboTiles, EventArgs.Empty );
        }
      }
      else
      {
        m_CurrentEditorTile = null;
      }

      m_CurrentEditedTile = null;
      listTileChars.Items.Clear();
      RedrawMap();
      SetModified();
    }



    private void btnMapDelete_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapRemove( this, m_MapProject, comboMaps.SelectedIndex ) );

      RemoveMap( comboMaps.SelectedIndex );
    }



    private void btnMapClear_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      // Destructive op — confirm first. The user already has Ctrl+Z, but
      // a misclick on a populated map deserves a one-tap-out.
      var result = System.Windows.Forms.MessageBox.Show(
        "Clear all characters, colors, markers and entities on map '"
          + ( string.IsNullOrEmpty( m_CurrentMap.Name ) ? "(unnamed)" : m_CurrentMap.Name )
          + "'? This cannot be undone in one step but Ctrl+Z will reverse each portion.",
        "Clear map?",
        System.Windows.Forms.MessageBoxButtons.YesNo,
        System.Windows.Forms.MessageBoxIcon.Warning,
        System.Windows.Forms.MessageBoxDefaultButton.Button2 );
      if ( result != System.Windows.Forms.DialogResult.Yes ) return;

      DocumentInfo.UndoManager.StartUndoGroup();

      // Snapshot tiles + per-char color/blocked overrides for the WHOLE
      // map before we wipe — UndoMapTilesChange covers all three layers
      // for the area passed in.
      int w = m_CurrentMap.Tiles.Width;
      int h = m_CurrentMap.Tiles.Height;
      DocumentInfo.UndoManager.AddGroupedUndoTask(
        new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, w, h ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );

      // Wipe tile placements (0 = the default empty tile slot).
      for ( int j = 0; j < h; ++j )
      {
        for ( int i = 0; i < w; ++i )
        {
          m_CurrentMap.Tiles[i, j] = 0;
        }
      }

      // Wipe per-character color overrides (0 = C64 black, per the
      // user's "set the color to zero" instruction).
      int charW = m_CurrentMap.TileColorOverrides.Width;
      int charH = m_CurrentMap.TileColorOverrides.Height;
      for ( int j = 0; j < charH; ++j )
      {
        for ( int i = 0; i < charW; ++i )
        {
          m_CurrentMap.TileColorOverrides[i, j] = 0;
        }
      }

      // Wipe per-character blocked overrides — false IS the no-override
      // state, so this matches a fresh map.
      int blkW = m_CurrentMap.CharBlockedOverrides.Width;
      int blkH = m_CurrentMap.CharBlockedOverrides.Height;
      for ( int j = 0; j < blkH; ++j )
      {
        for ( int i = 0; i < blkW; ++i )
        {
          m_CurrentMap.CharBlockedOverrides[i, j] = false;
        }
      }

      m_CurrentMap.Markers.Clear();
      m_CurrentMap.Entities.Clear();

      UpdateArea( 0, 0, w, h );
      RedrawMap();
      pictureEditor.Invalidate();
      UpdateMarkerOutOfBoundsLabel();
      UpdateEntityCountLabel();
      SetModified();
    }



    private void btnMoveTileUp_Click( DecentForms.ControlBase Sender )
    {
      int index1 = listTileInfo.SelectedIndices[0] - 1;
      int index2 = listTileInfo.SelectedIndices[0];

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileExchange( this, m_MapProject, index1, index2 ) );

      SwapTiles( index1, index2 );

      listTileInfo.SelectedIndices.Clear();
      listTileInfo.SelectedIndices.Add( index1 );
      listTileInfo.EnsureVisible( index1 );
    }



    public void MoveTile( int FromIndex, int ToIndex )
    {
      if ( FromIndex == ToIndex )
      {
        return;
      }
      if ( ( FromIndex < 0 )
      ||   ( FromIndex >= m_MapProject.Tiles.Count )
      ||   ( ToIndex < 0 )
      ||   ( ToIndex >= m_MapProject.Tiles.Count ) )
      {
        return;
      }

      Formats.MapProject.Tile tile = m_MapProject.Tiles[FromIndex];

      m_MapProject.Tiles.RemoveAt( FromIndex );
      m_MapProject.Tiles.Insert( ToIndex, tile );

      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        m_MapProject.Tiles[i].Index = i;
      }

      // update maps
      foreach ( var map in m_MapProject.Maps )
      {
        for ( int x = 0; x < map.Tiles.Width; ++x )
        {
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            int tileIndex = map.Tiles[x, y];

            if ( tileIndex == FromIndex )
            {
              map.Tiles[x, y] = ToIndex;
            }
            else if ( FromIndex < ToIndex )
            {
              if ( ( tileIndex > FromIndex )
              &&   ( tileIndex <= ToIndex ) )
              {
                --map.Tiles[x, y];
              }
            }
            else
            {
              // FromIndex > ToIndex
              if ( ( tileIndex >= ToIndex )
              &&   ( tileIndex < FromIndex ) )
              {
                ++map.Tiles[x, y];
              }
            }
          }
        }
      }

      // Entity types reference tiles by index — apply the same shift to
      // their TileIndex so entity overlays keep pointing at the same tile
      // after the reorder.
      foreach ( var entityType in m_MapProject.EntityTypes )
      {
        int tileIndex = entityType.TileIndex;
        if ( tileIndex == FromIndex )
        {
          entityType.TileIndex = ToIndex;
        }
        else if ( FromIndex < ToIndex )
        {
          if ( ( tileIndex > FromIndex )
          &&   ( tileIndex <= ToIndex ) )
          {
            --entityType.TileIndex;
          }
        }
        else
        {
          // FromIndex > ToIndex
          if ( ( tileIndex >= ToIndex )
          &&   ( tileIndex < FromIndex ) )
          {
            ++entityType.TileIndex;
          }
        }
      }

      // update list
      ListViewItem item = listTileInfo.Items[FromIndex];
      listTileInfo.Items.RemoveAt( FromIndex );
      listTileInfo.Items.Insert( ToIndex, item );

      // update combo
      object comboItem = comboTiles.Items[FromIndex];
      comboTiles.Items.RemoveAt( FromIndex );
      comboTiles.Items.Insert( ToIndex, comboItem );

      RedrawMap();
      SetModified();
    }



    // ----------------------------------------------------------------
    // Auto-scroll the tile list while the user is dragging an item near
    // the top/bottom edge — matches Explorer/Outlook/etc. so the user
    // can drop a tile far above/below the current viewport without
    // having to release, scroll manually, and re-grab.
    //
    // Pattern: hot-zone detection in DragOver flips the direction
    // field; a 60 ms tick timer pumps WM_VSCROLL (line up/down) at the
    // listview while the cursor sits in the zone. Released on
    // DragDrop / DragLeave so it can't keep scrolling after the drop.
    // ----------------------------------------------------------------
    private System.Windows.Forms.Timer  m_TileListAutoScrollTimer = null;
    private int                          m_TileListAutoScrollDirection = 0;
    private const int                    TileListAutoScrollHotZonePx = 24;

    [System.Runtime.InteropServices.DllImport( "user32.dll" )]
    private static extern int SendMessage( IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam );
    private const int WM_VSCROLL = 0x0115;
    private const int SB_LINEUP = 0;
    private const int SB_LINEDOWN = 1;

    private void EnsureTileListAutoScrollTimer()
    {
      if ( m_TileListAutoScrollTimer != null ) return;
      m_TileListAutoScrollTimer = new System.Windows.Forms.Timer();
      // 60 ms ≈ 16 lines/sec — fast enough that you won't think it's
      // stuck, slow enough that overshooting by one row is rare. Tuned
      // by feel; bump down to ~40 ms if it feels sluggish on long lists.
      m_TileListAutoScrollTimer.Interval = 60;
      m_TileListAutoScrollTimer.Tick += TileListAutoScrollTimer_Tick;
    }

    private void TileListAutoScrollTimer_Tick( object sender, EventArgs e )
    {
      if ( ( listTileInfo == null ) || ( !listTileInfo.IsHandleCreated ) )
      {
        return;
      }
      if ( m_TileListAutoScrollDirection < 0 )
      {
        SendMessage( listTileInfo.Handle, WM_VSCROLL, (IntPtr)SB_LINEUP, IntPtr.Zero );
      }
      else if ( m_TileListAutoScrollDirection > 0 )
      {
        SendMessage( listTileInfo.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero );
      }
    }

    private void StopTileListAutoScroll()
    {
      m_TileListAutoScrollDirection = 0;
      if ( m_TileListAutoScrollTimer != null )
      {
        m_TileListAutoScrollTimer.Stop();
      }
    }

    private void listTileInfo_ItemDrag( object sender, ItemDragEventArgs e )
    {
      EnsureTileListAutoScrollTimer();
      listTileInfo.DoDragDrop( e.Item, DragDropEffects.Move );
      StopTileListAutoScroll();
    }



    private void listTileInfo_DragLeave( object sender, EventArgs e )
    {
      // Cursor exited the listview — stop pumping scrolls so a
      // dragged-out-then-back-in motion doesn't keep scrolling while
      // the cursor is somewhere else entirely.
      StopTileListAutoScroll();
    }



    private void listTileInfo_DragEnter( object sender, DragEventArgs e )
    {
      if ( e.Data.GetDataPresent( typeof( ListViewItem ) ) )
      {
        e.Effect = DragDropEffects.Move;
      }
      else
      {
        e.Effect = DragDropEffects.None;
      }
    }



    private void listTileInfo_DragOver( object sender, DragEventArgs e )
    {
      System.Drawing.Point targetPoint = listTileInfo.PointToClient( new System.Drawing.Point( e.X, e.Y ) );
      int targetIndex = listTileInfo.InsertionMark.NearestIndex( targetPoint );

      if ( targetIndex > -1 )
      {
        System.Drawing.Rectangle itemBounds = listTileInfo.GetItemRect( targetIndex );
        if ( targetPoint.Y > itemBounds.Top + ( itemBounds.Height / 2 ) )
        {
          listTileInfo.InsertionMark.AppearsAfterItem = true;
        }
        else
        {
          listTileInfo.InsertionMark.AppearsAfterItem = false;
        }
      }
      listTileInfo.InsertionMark.Index = targetIndex;

      // Hot-zone auto-scroll: cursor near the top edge → scroll up,
      // near the bottom edge → scroll down, anywhere else → idle.
      // The timer (lazy-initialised in ItemDrag) does the actual
      // pumping at a steady rate so the user doesn't have to wiggle
      // the mouse to keep scrolling. Direction reset to 0 stops the
      // timer until the cursor re-enters a zone.
      int newDir = 0;
      if ( targetPoint.Y < TileListAutoScrollHotZonePx )
      {
        newDir = -1;
      }
      else if ( targetPoint.Y > listTileInfo.ClientSize.Height - TileListAutoScrollHotZonePx )
      {
        newDir = 1;
      }
      m_TileListAutoScrollDirection = newDir;
      if ( m_TileListAutoScrollTimer != null )
      {
        if ( newDir != 0 )
        {
          if ( !m_TileListAutoScrollTimer.Enabled )
          {
            m_TileListAutoScrollTimer.Start();
          }
        }
        else if ( m_TileListAutoScrollTimer.Enabled )
        {
          m_TileListAutoScrollTimer.Stop();
        }
      }
    }



    private void listTileInfo_DragDrop( object sender, DragEventArgs e )
    {
      // Drop committed — kill the auto-scroll pump immediately. The
      // ItemDrag finally-clause also stops it, but doing it here means
      // the listview doesn't get one extra scroll tick after the user
      // releases.
      StopTileListAutoScroll();

      System.Drawing.Point targetPoint = listTileInfo.PointToClient( new System.Drawing.Point( e.X, e.Y ) );
      int targetIndex = listTileInfo.InsertionMark.NearestIndex( targetPoint );

      if ( targetIndex > -1 )
      {
        if ( listTileInfo.InsertionMark.AppearsAfterItem )
        {
          ++targetIndex;
        }
      }

      else
      {
        bool  reallyBelow = false;
        if ( listTileInfo.Items.Count > 0 )
        {
          var lastItemRect = listTileInfo.GetItemRect( listTileInfo.Items.Count - 1 );
          if ( targetPoint.Y > lastItemRect.Bottom )
          {
            reallyBelow = true;
          }
        }
        else
        {
          reallyBelow = true;
        }

        if ( reallyBelow )
        {
          targetIndex = listTileInfo.Items.Count;
        }
        else
        {
          // We are likely ON an item but NearestIndex failed for some reason
          // OR we are dragging onto ourselves?
          var hitInfo = listTileInfo.HitTest( targetPoint );
          if ( hitInfo.Item != null )
          {
            targetIndex = hitInfo.Item.Index;
            // determine before/after
            var itemBounds = listTileInfo.GetItemRect( targetIndex );
            if ( targetPoint.Y > itemBounds.Top + ( itemBounds.Height / 2 ) )
            {
              ++targetIndex;
            }
          }
          else
          {
            return;
          }
        }
      }

      ListViewItem draggedItem = (ListViewItem)e.Data.GetData( typeof( ListViewItem ) );
      if ( draggedItem == null )
      {
        return;
      }

      int fromIndex = draggedItem.Index;
      int toIndex = targetIndex;

      if ( fromIndex == toIndex )
      {
        return;
      }
      // adjust toIndex if moving down, because removing the item shifts indices
      if ( ( toIndex > fromIndex )
      &&   ( toIndex > 0 ) )
      {
        --toIndex;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileMove( this, m_MapProject, fromIndex, toIndex ) );
      MoveTile( fromIndex, toIndex );

      listTileInfo.SelectedIndices.Clear();
      listTileInfo.SelectedIndices.Add( toIndex );
      listTileInfo.EnsureVisible( toIndex );
    }



    public void SwapTiles( int Index1, int Index2 )
    {
      Formats.MapProject.Tile tile1 = m_MapProject.Tiles[Index1];
      Formats.MapProject.Tile tile2 = m_MapProject.Tiles[Index2];

      m_MapProject.Tiles[Index1] = tile2;
      m_MapProject.Tiles[Index2] = tile1;

      m_MapProject.Tiles[Index1].Index = Index1;
      m_MapProject.Tiles[Index2].Index = Index2;

      // swap in list — SubItem indices: 0=#, 1=Preview (empty),
      // 2=Name, 3=Size, 4=Used. The preview column is image-only so we
      // leave its text alone; the row repaint picks up the new tile
      // via Tag.
      listTileInfo.Items[Index1].SubItems[2].Text = tile2.Name;
      listTileInfo.Items[Index1].SubItems[3].Text = tile2.Chars.Width.ToString() + "x" + tile2.Chars.Height.ToString();
      listTileInfo.Items[Index1].SubItems[4].Text = "0";
      listTileInfo.Items[Index1].Tag = tile2;

      listTileInfo.Items[Index2].SubItems[2].Text = tile1.Name;
      listTileInfo.Items[Index2].SubItems[3].Text = tile1.Chars.Width.ToString() + "x" + tile1.Chars.Height.ToString();
      listTileInfo.Items[Index2].SubItems[4].Text = "0";
      listTileInfo.Items[Index2].Tag = tile1;

      // swap in tile combo
      GR.Generic.Tupel<string, Formats.MapProject.Tile>    tupel1 = (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.Items[Index1];
      GR.Generic.Tupel<string, Formats.MapProject.Tile>    tupel2 = (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.Items[Index2];

      string    temp = tupel1.first;
      tupel1.first = tupel2.first;
      tupel2.first = temp;

      tupel1.second = tile2;
      tupel2.second = tile1;
         
      comboTiles.Items[Index1] = tupel1;
      comboTiles.Items[Index2] = tupel2;

      foreach ( var map in m_MapProject.Maps )
      {
        for ( int i = 0; i < map.Tiles.Width; ++i )
        {
          for ( int j = 0; j < map.Tiles.Height; ++j )
          {
            if ( map.Tiles[i, j] == Index1 )
            {
              map.Tiles[i, j] = Index2;
            }
            else if ( map.Tiles[i, j] == Index2 )
            {
              map.Tiles[i, j] = Index1;
            }
          }
        }
      }

      // Entity types reference tiles by index — keep them pointing at the
      // same visual tile after the swap so entity overlays don't silently
      // start rendering a different tile.
      foreach ( var entityType in m_MapProject.EntityTypes )
      {
        if ( entityType.TileIndex == Index1 )
        {
          entityType.TileIndex = Index2;
        }
        else if ( entityType.TileIndex == Index2 )
        {
          entityType.TileIndex = Index1;
        }
      }
      RedrawMap();
      SetModified();
    }



    private void btnMoveTileDown_Click( DecentForms.ControlBase Sender )
    {
      int index1 = listTileInfo.SelectedIndices[0];
      int index2 = listTileInfo.SelectedIndices[0] + 1;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileExchange( this, m_MapProject, index1, index2 ) );

      SwapTiles( index1, index2 );

      listTileInfo.SelectedIndices.Clear();
      listTileInfo.SelectedIndices.Add( index2 );
      listTileInfo.EnsureVisible( index2 );
    }



    private void editDataExport_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( ( System.Windows.Forms.Control.ModifierKeys == Keys.Control )
      &&   ( e.KeyChar == 1 ) )
      {
        editDataExport.SelectAll();
        e.Handled = true;
      }
    }



    /// <summary>
    /// Handler for the Tools → Edit extra data... menu item. Opens a
    /// modal multi-line text editor seeded with the current map's
    /// ExtraDataText. Saves on OK with an undo entry; Cancel discards.
    /// </summary>
    private void editExtraDataToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )    return;
      if ( m_IsViewingRevision )     return;

      using ( var dlg = new Dialogs.FormMapExtraData( Core, m_CurrentMap.ExtraDataText ) )
      {
        if ( dlg.ShowDialog( this ) != System.Windows.Forms.DialogResult.OK ) return;

        if ( dlg.ExtraData != m_CurrentMap.ExtraDataText )
        {
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );
          m_CurrentMap.ExtraDataText = dlg.ExtraData;
          Modified = true;
        }
      }
    }



    public bool OpenCharpadFile( string filename )
    {
      GR.Memory.ByteBuffer projectFile = GR.IO.File.ReadAllBytes( filename );

      Formats.CharpadProject    cpProject = new RetroDevStudio.Formats.CharpadProject();
      if ( !cpProject.LoadFromFile( projectFile ) )
      {
        return false;
      }

      m_MapProject.Charset.Colors.BackgroundColor = cpProject.BackgroundColor;
      m_MapProject.Charset.Colors.MultiColor1 = cpProject.MultiColor1;
      m_MapProject.Charset.Colors.MultiColor2 = cpProject.MultiColor2;
      m_MapProject.Charset.Colors.BGColor4 = cpProject.BackgroundColor4;

      int maxChars = cpProject.NumChars;
      if ( maxChars > 256 )
      {
        maxChars = 256;
      }

      m_MapProject.Charset.ExportNumCharacters = maxChars;
      for ( int charIndex = 0; charIndex < m_MapProject.Charset.ExportNumCharacters; ++charIndex )
      {
        m_MapProject.Charset.Characters[charIndex].Tile.Data = cpProject.Characters[charIndex].Data;
        m_MapProject.Charset.Characters[charIndex].Tile.CustomColor = cpProject.Characters[charIndex].Color;

        RebuildCharImage( charIndex );
      }

      // import tiles
      m_MapProject.Maps.Clear();
      comboMaps.Items.Clear();

      m_MapProject.Tiles.Clear();
      comboTiles.Items.Clear();
      listTileInfo.Items.Clear();

      switch ( cpProject.DisplayModeFile )
      {
        case Formats.CharpadProject.DisplayMode.HIRES:
          comboMapProjectMode.SelectedIndex = (int)TextMode.COMMODORE_40_X_25_HIRES;
          m_MapProject.Charset.Mode = TextCharMode.COMMODORE_HIRES;
          break;
        case Formats.CharpadProject.DisplayMode.MULTICOLOR:
          comboMapProjectMode.SelectedIndex = (int)TextMode.COMMODORE_40_X_25_MULTICOLOR;
          m_MapProject.Charset.Mode = TextCharMode.COMMODORE_MULTICOLOR;
          break;
        case Formats.CharpadProject.DisplayMode.ECM:
          comboMapProjectMode.SelectedIndex = (int)TextMode.COMMODORE_40_X_25_ECM;
          m_MapProject.Charset.Mode = TextCharMode.COMMODORE_ECM;
          break;
      }
      characterEditor.CharsetUpdated( m_MapProject.Charset );

      for ( int i = 0; i < cpProject.NumTiles; ++i )
      {
        Formats.MapProject.Tile tile = new Formats.MapProject.Tile();

        tile.Name = "Tile " + ( i + 1 ).ToString();
        tile.Chars.Resize( cpProject.TileWidth, cpProject.TileHeight );
        tile.Index = i;

        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            tile.Chars[x, y].Character = (byte)cpProject.Tiles[i].CharData.UInt16At( 2 * ( x + y * tile.Chars.Width ) );
            tile.Chars[x, y].Color = cpProject.Tiles[i].ColorData.ByteAt( x + y * tile.Chars.Width );
          }
        }
        m_MapProject.Tiles.Add( tile );
        comboTiles.Items.Add( new GR.Generic.Tupel<string, Formats.MapProject.Tile>( tile.Name, tile ) );

        ListViewItem item = new ListViewItem();

        item.Text = tile.Index.ToString();
        // Preview column — empty text; thumbnail painted by DrawItemImage.
        item.SubItems.Add( "" );
        item.SubItems.Add( tile.Name );
        item.SubItems.Add( tile.Chars.Width.ToString() + "x" + tile.Chars.Height.ToString() );
        item.SubItems.Add( "0" );
        item.Tag = tile;
        item.ImageIndex = 0;

        listTileInfo.Items.Add( item );
      }

      var map = new Formats.MapProject.Map();
      map.Tiles.Resize( cpProject.MapWidth, cpProject.MapHeight );
      // TileColorOverrides is char-grid — multiply by spacing.
      map.TileColorOverrides.Resize(
        cpProject.MapWidth * map.TileSpacingX,
        cpProject.MapHeight * map.TileSpacingY );
      ResetColorOverrides( map.TileColorOverrides );
      // Per-character blocked-override layer — same shape; default false.
      map.CharBlockedOverrides.Resize(
        cpProject.MapWidth * map.TileSpacingX,
        cpProject.MapHeight * map.TileSpacingY );
      for ( int j = 0; j < cpProject.MapHeight; ++j )
      {
        for ( int i = 0; i < cpProject.MapWidth; ++i )
        {
          map.Tiles[i, j] = cpProject.MapData.ByteAt( i + j * cpProject.MapWidth );
        }
      }
      map.TileSpacingX = cpProject.TileWidth;
      map.TileSpacingY = cpProject.TileHeight;
      if ( map.TileSpacingX <= 0 )
      {
        map.TileSpacingX = 1;
      }
      if ( map.TileSpacingY <= 0 )
      {
        map.TileSpacingY = 1;
      }
      map.Name = "Imported Map";
      m_MapProject.Maps.Add( map );
      comboMaps.Items.Add( new GR.Generic.Tupel<string, Formats.MapProject.Map>( map.Name, map ) );
      comboMaps.Enabled = true;

      comboTileBackground.SelectedIndex = m_MapProject.Charset.Colors.BackgroundColor;
      comboTileMulticolor1.SelectedIndex = m_MapProject.Charset.Colors.MultiColor1;
      comboTileMulticolor2.SelectedIndex = m_MapProject.Charset.Colors.MultiColor2;
      comboTileBGColor4.SelectedIndex = m_MapProject.Charset.Colors.BGColor4;

      RedrawMap();
      SetModified();
      return true;
    }



    private bool RemoveFloatingSelection()
    {
      if ( m_FloatingSelection != null )
      {
        m_FloatingSelection = null;
        // Drop the parallel override captures too so a subsequent paste
        // doesn't accidentally splice this selection's chars onto a
        // freshly-pasted tile grid.
        m_FloatingSelectionOverrides = null;
        m_FloatingSelectionBlocked = null;
        Redraw();
        return true;
      }
      return false;
    }



    // KryptonCheckButton doesn't auto-group like RadioButton does. When one
    // tool button is checked on, we uncheck the siblings so they behave as a
    // mutually-exclusive set. Guard against recursion because setting Checked
    // re-fires CheckedChanged on each sibling.
    private bool m_UncheckingToolSiblings = false;

    private void UncheckOtherToolButtons( Krypton.Toolkit.KryptonCheckButton keeper )
    {
      if ( m_UncheckingToolSiblings ) return;
      m_UncheckingToolSiblings = true;
      try
      {
        var buttons = new Krypton.Toolkit.KryptonCheckButton[]
        {
          btnToolEdit, btnToolRect, btnToolQuad, btnToolFill,
          btnToolColorReplace,
          btnToolSelect, btnToolMarker, btnToolEntity,
          btnToolPassable,
        };
        bool needFocusMove = false;
        foreach ( var b in buttons )
        {
          if ( ( b != keeper ) && b.Checked )
          {
            b.Checked = false;
            // The button's Checked just went false, but if it still
            // holds keyboard focus the focus rectangle keeps drawing
            // in a way that reads as "selected" — that's the
            // long-standing "two buttons look selected" bug, and it
            // pre-dates Krypton (focus-ring overlap with the
            // checked-frame style is a stock WinForms tool-button
            // problem). Note we need to move focus, not just flip
            // Checked. Defer the move until after the loop so we
            // don't poke focus mid-iteration.
            if ( b.Focused )
            {
              needFocusMove = true;
            }
          }
        }
        // Focus the map's picture editor — that's where the user's
        // attention is anyway, and it removes the focus ring from any
        // now-unchecked tool button. Falling back to keeper.Focus()
        // ensures focus lands somewhere visible if pictureEditor
        // can't accept focus for some reason.
        if ( needFocusMove )
        {
          if ( ( pictureEditor != null ) && pictureEditor.CanFocus )
          {
            pictureEditor.Focus();
          }
          else if ( ( keeper != null ) && keeper.CanFocus )
          {
            keeper.Focus();
          }
        }
      }
      finally
      {
        m_UncheckingToolSiblings = false;
      }
    }

    /// <summary>
    /// Tool buttons are meant to behave like a radio group — one tool is
    /// always active and clicking the active tool's button again should be a
    /// no-op. KryptonCheckButton, however, flips Checked on every click, so
    /// without this guard the second click leaves the button (and the group)
    /// with nothing selected. If Checked just went false AND we weren't the
    /// ones turning it off (via UncheckOtherToolButtons), flip it back on
    /// and the re-entrant CheckedChanged runs the full activation path.
    /// </summary>
    private bool KeepActiveIfUnchecking( Krypton.Toolkit.KryptonCheckButton btn )
    {
      if ( !btn.Checked )
      {
        if ( !m_UncheckingToolSiblings )
        {
          btn.Checked = true;
        }
        return true;
      }
      return false;
    }

    /// <summary>
    /// Called at the end of each tool activation so the map image reflects the
    /// new mode. In particular the ENTITY-mode dim is baked into DisplayPage
    /// by RedrawMap — without this, switching TO or AWAY FROM the entity tool
    /// leaves the previous frame on screen until some other event triggers a
    /// rebuild. Also refreshes marker-controls state since it depends on which
    /// tool is active (comboMarkerTypes, dimSlider, etc.).
    /// </summary>
    private void AfterToolChange()
    {
      // A marker or entity selection is only meaningful while we're in the
      // matching tool mode; switching away makes the Delete button useless
      // and the highlight stale. Drop both selections unconditionally so
      // the toolbar controls revert to "defaults for new placement".
      ClearMarkerEntitySelection();
      UpdateMarkerControlsState();
      RedrawMap();
      pictureEditor.Invalidate();
    }

    private void btnToolEdit_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolEdit ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.SINGLE_TILE;
      UncheckOtherToolButtons( btnToolEdit );
      AfterToolChange();
    }



    private void HideSelection()
    {
      if ( m_CurrentMap != null )
      {
        for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
        {
          for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
          {
            m_SelectedTiles[i, j] = false;
          }
        }
        Redraw();
      }
    }



    private void btnToolRect_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolRect ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.RECTANGLE;
      UncheckOtherToolButtons( btnToolRect );
      AfterToolChange();
    }



    private void btnToolQuad_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolQuad ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.FILLED_RECTANGLE;
      UncheckOtherToolButtons( btnToolQuad );
      AfterToolChange();
    }



    private void btnToolFill_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolFill ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.FILL;
      UncheckOtherToolButtons( btnToolFill );
      AfterToolChange();
    }



    private void btnToolColorReplace_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolColorReplace ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.COLOR_REPLACE;
      UncheckOtherToolButtons( btnToolColorReplace );
      AfterToolChange();
    }



    private void btnToolSelect_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolSelect ) ) return;
      m_ToolMode = ToolMode.SELECT;
      UncheckOtherToolButtons( btnToolSelect );
      AfterToolChange();
    }



    private void btnMapCopy_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      var newMap = new RetroDevStudio.Formats.MapProject.Map();
      newMap.ExtraDataOld = new GR.Memory.ByteBuffer( m_CurrentMap.ExtraDataOld );
      newMap.ExtraDataText = m_CurrentMap.ExtraDataText;
      newMap.Name = m_CurrentMap.Name;
      newMap.Tiles = new GR.Game.Layer<int>();
      newMap.Tiles.Resize( m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height );
      newMap.TileSpacingX = m_CurrentMap.TileSpacingX;
      newMap.TileSpacingY = m_CurrentMap.TileSpacingY;
      // Char-grid override layer: copy slot-for-slot from the source so
      // the duplicated map looks identical, per-character tweaks
      // included.
      int dupCharW = m_CurrentMap.Tiles.Width  * newMap.TileSpacingX;
      int dupCharH = m_CurrentMap.Tiles.Height * newMap.TileSpacingY;
      newMap.TileColorOverrides = new GR.Game.Layer<int>();
      newMap.TileColorOverrides.Resize( dupCharW, dupCharH );
      for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
      {
        for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
        {
          newMap.Tiles[i,j] =  m_CurrentMap.Tiles[i,j];
        }
      }
      for ( int j = 0; j < dupCharH; ++j )
      {
        for ( int i = 0; i < dupCharW; ++i )
        {
          int srcOverride = ( ( i < m_CurrentMap.TileColorOverrides.Width )
                              && ( j < m_CurrentMap.TileColorOverrides.Height ) )
                            ? m_CurrentMap.TileColorOverrides[i,j] : -1;
          newMap.TileColorOverrides[i,j] = srcOverride;
        }
      }
      // Per-character blocked-override layer: deep-copy slot-for-slot
      // alongside the color overrides so a duplicate map carries every
      // per-char passability tweak.
      newMap.CharBlockedOverrides = new GR.Game.Layer<bool>();
      newMap.CharBlockedOverrides.Resize( dupCharW, dupCharH );
      for ( int j = 0; j < dupCharH; ++j )
      {
        for ( int i = 0; i < dupCharW; ++i )
        {
          bool srcBlocked = ( ( i < m_CurrentMap.CharBlockedOverrides.Width )
                              && ( j < m_CurrentMap.CharBlockedOverrides.Height ) )
                            ? m_CurrentMap.CharBlockedOverrides[i,j] : false;
          newMap.CharBlockedOverrides[i,j] = srcBlocked;
        }
      }
      newMap.AlternativeBackgroundColor = m_CurrentMap.AlternativeBackgroundColor;
      newMap.AlternativeMultiColor1     = m_CurrentMap.AlternativeMultiColor1;
      newMap.AlternativeMultiColor2     = m_CurrentMap.AlternativeMultiColor2;


      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapAdd( this, m_MapProject, m_MapProject.Maps.Count ) );

      AddMap( m_MapProject.Maps.Count, newMap );
    }



    private void comboAlternativeColor_DrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;
      // Since these combos are now KryptonComboBox, the event sender is the
      // INNER ComboBox. Its Parent is the KryptonComboBox wrapper we compare
      // against by reference below.
      var owner = combo.Parent as Krypton.Toolkit.KryptonComboBox;

      if ( Core?.Theming != null )
        Core.Theming.DrawThemedBackground( e, combo );
      else
        e.DrawBackground();
      System.Drawing.Rectangle itemRect = new System.Drawing.Rectangle( e.Bounds.Left + 20, e.Bounds.Top, e.Bounds.Width - 20, e.Bounds.Height );

      int colorToUse = e.Index - 1;
      if ( colorToUse == -1 )
      {
        if ( owner == comboMapMultiColor1 )
        {
          colorToUse = m_MapProject.Charset.Colors.MultiColor1;
        }
        else if ( owner == comboMapMultiColor2 )
        {
          colorToUse = m_MapProject.Charset.Colors.MultiColor2;
        }
        else
        {
          colorToUse = m_MapProject.BackgroundColor;
        }
        itemRect = new System.Drawing.Rectangle( e.Bounds.Left + 80, e.Bounds.Top, e.Bounds.Width - 80, e.Bounds.Height );
      }
      e.Graphics.FillRectangle( ConstantData.Palette.ColorBrushes[colorToUse], itemRect );
      if ( ( e.State & DrawItemState.Selected ) != 0 )
      {
        e.Graphics.DrawString( combo.Items[e.Index].ToString(), combo.Font, new System.Drawing.SolidBrush( System.Drawing.Color.White ), 3.0f, e.Bounds.Top + 1.0f );
      }
      else
      {
        e.Graphics.DrawString( combo.Items[e.Index].ToString(), combo.Font, new System.Drawing.SolidBrush(e.ForeColor), 3.0f, e.Bounds.Top + 1.0f );
      }

    }



    private void comboMapMultiColor1_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeMultiColor1 + 1 != comboMapMultiColor1.SelectedIndex ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );  

        m_CurrentMap.AlternativeMultiColor1 = comboMapMultiColor1.SelectedIndex - 1;
        RedrawMap();
        Modified = true;
      }
    }



    private void comboMapMultiColor2_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeMultiColor2 + 1 != comboMapMultiColor2.SelectedIndex ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );  

        m_CurrentMap.AlternativeMultiColor2 = comboMapMultiColor2.SelectedIndex - 1;
        RedrawMap();
        Modified = true;
      }
    }



    private void comboMapBGColor_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeBackgroundColor + 1 != comboMapBGColor.SelectedIndex ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );  

        m_CurrentMap.AlternativeBackgroundColor = comboMapBGColor.SelectedIndex - 1;
        RedrawMap();
        Modified = true;
      }
    }



    public void InvalidateCurrentMap()
    {
      comboMaps_SelectedIndexChanged( null, null );
    }



    public void UpdateArea( int X, int Y, int Width, int Height )
    {
      // DrawTile already adds renderOffset internally. The cache copy and
      // Invalidate need it too — sampling the unshifted region of
      // DisplayPage when the map is centered would copy empty background
      // pixels into m_Image and the freshly-drawn tile would vanish on
      // the next Redraw().
      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          DrawTile( X + i - m_CurEditorOffsetX, Y + j - m_CurEditorOffsetY, m_CurrentMap.Tiles[X + i, Y + j] );
        }
      }
      pictureEditor.DisplayPage.DrawTo( m_Image,
                      renderOffsetX + ( X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                      renderOffsetY + ( Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                      renderOffsetX + ( X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                      renderOffsetY + ( Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                      Width * 8 * m_CurrentMap.TileSpacingX, Height * 8 * m_CurrentMap.TileSpacingY );

      pictureEditor.Invalidate( new System.Drawing.Rectangle(
                                  renderOffsetX + ( X - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8,
                                  renderOffsetY + ( Y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8,
                                  Width * m_CurrentMap.TileSpacingY * 8,
                                  Height * m_CurrentMap.TileSpacingY * 8 ) );
      RedrawMap();
      RecalcTileUsageInCurrentMap();
    }



    public void RemoveMap( int MapIndex )
    {
      if ( ( MapIndex >= 0 )
      &&   ( MapIndex < m_MapProject.Maps.Count ) )
      {
        m_MapProject.Maps.RemoveAt( MapIndex );
        comboMaps.Items.RemoveAt( MapIndex );

        // Keep StartMapIndex pointing at a real map. If the removed map WAS
        // the start map, fall back to 0 (matches the default for a fresh
        // project). If the removed map was BEFORE the start map, the start
        // map's index has shifted down by one.
        if ( m_MapProject.StartMapIndex == MapIndex )
        {
          m_MapProject.StartMapIndex = 0;
        }
        else if ( m_MapProject.StartMapIndex > MapIndex )
        {
          m_MapProject.StartMapIndex--;
        }

        for ( int i = 0; i < comboMaps.Items.Count; ++i )
        {
          GR.Generic.Tupel<string, Formats.MapProject.Map>    mapPair = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[i];

          mapPair.first = FormatMapDisplayName( i, mapPair.second );

          // force name update
          comboMaps.Items[i] = comboMaps.Items[i];
        }
        SetModified();
      }
      else
      {
        Debug.Log( "remove invalid map index" );
      }
    }



    public void TileModified( int TileIndex )
    {
      // force refresh
      listTileInfo_SelectedIndexChanged( null, null );
      listTileChars_SelectedIndexChanged( null, null );
      if ( comboTiles.SelectedIndex == TileIndex )
      {
        comboTiles.Invalidate();
      }
      // Refresh just the modified row's thumbnail in the Tiles tab list.
      // RedrawItems is cheaper than a full Invalidate and avoids visible
      // flicker when the user paints quickly in the character editor.
      if ( ( TileIndex >= 0 )
      &&   ( TileIndex < listTileInfo.Items.Count ) )
      {
        listTileInfo.RedrawItems( TileIndex, TileIndex, false );
      }
      RedrawMap();
    }



    private void comboBGColor4_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject.BGColor4 != comboTileBGColor4.SelectedIndex )
      {
        m_MapProject.BGColor4 = comboTileBGColor4.SelectedIndex;
        m_MapProject.Charset.Colors.BGColor4 = m_MapProject.BGColor4;
        SetModified();
        FullRebuild();
      }

    }



    private void comboMapBGColor4_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeBGColor4 + 1 != comboMapAlternativeBGColor4.SelectedIndex ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );

        m_CurrentMap.AlternativeBGColor4 = comboMapAlternativeBGColor4.SelectedIndex - 1;
        RedrawMap();
        Modified = true;
      }
    }



    private void comboMapAlternativeMode_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap != null )
      &&   ( (int)m_CurrentMap.AlternativeMode + 1 != comboMapAlternativeMode.SelectedIndex ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );

        m_CurrentMap.AlternativeMode = (TextCharMode)( comboMapAlternativeMode.SelectedIndex - 1 );

        switch ( m_CurrentMap.AlternativeMode )
        {
          case TextCharMode.COMMODORE_ECM:
          case TextCharMode.COMMODORE_HIRES:
          case TextCharMode.COMMODORE_MULTICOLOR:
            m_MapProject.Charset.Colors.Palettes[0] = Core.Imaging.PaletteFromMachine( MachineType.C64 );
            break;
          case TextCharMode.VIC20:
            m_MapProject.Charset.Colors.Palettes[0] = Core.Imaging.PaletteFromMachine( MachineType.VIC20 );
            break;
        }
        RedrawMap();
        Modified = true;
      }
    }



    private void btnSetNextTileChar_Click( DecentForms.ControlBase Sender )
    {
      if ( ( m_CurrentEditedTile == null )
      ||   ( m_CurrentTileChar == null )
      ||   ( listTileChars.SelectedIndices.Count == 0 ) )
      {
        return;
      }
      int     currentTileCharIndex = listTileChars.SelectedIndices[0];
      if ( currentTileCharIndex + 1 >= listTileChars.Items.Count )
      {
        return;
      }
      var nextChar = m_CurrentEditedTile.Chars[( currentTileCharIndex + 1 ) % m_CurrentEditedTile.Chars.Width, ( currentTileCharIndex + 1 ) / m_CurrentEditedTile.Chars.Width];

      if ( ( nextChar.Character != m_CurrentTileChar.Character )
      ||   ( nextChar.Color != m_CurrentTileChar.Color ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, m_CurrentEditedTile.Index ) );

        nextChar.Character = m_CurrentTileChar.Character;
        nextChar.Color = m_CurrentTileChar.Color;

        listTileChars.Items[currentTileCharIndex + 1].SubItems[1].Text = nextChar.Character.ToString();
        listTileChars.Items[currentTileCharIndex + 1].SubItems[2].Text = nextChar.Color.ToString();
        RedrawTile();
        RedrawMap();
        SetModified();
      }
      // move selection to next in any case
      listTileChars.SelectedIndices.Clear();
      listTileChars.SelectedIndices.Add( currentTileCharIndex + 1 );
    }



    private void btnCopyTileCharToNextIncreased_Click( DecentForms.ControlBase Sender )
    {
      if ( ( m_CurrentEditedTile == null )
      ||   ( m_CurrentTileChar == null )
      ||   ( listTileChars.SelectedIndices.Count == 0 ) )
      {
        return;
      }
      int     currentTileCharIndex = listTileChars.SelectedIndices[0];
      if ( currentTileCharIndex + 1 >= listTileChars.Items.Count )
      {
        return;
      }
      var nextChar = m_CurrentEditedTile.Chars[( currentTileCharIndex + 1 ) % m_CurrentEditedTile.Chars.Width, ( currentTileCharIndex + 1 ) / m_CurrentEditedTile.Chars.Width];

      if ( ( nextChar.Character != (byte)( m_CurrentTileChar.Character + 1 ) )
      ||   ( nextChar.Color != m_CurrentColor ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, m_CurrentEditedTile.Index ) );

        nextChar.Character  = (byte)( m_CurrentTileChar.Character + 1 );
        nextChar.Color      = m_CurrentTileChar.Color;

        listTileChars.Items[currentTileCharIndex + 1].SubItems[1].Text = nextChar.Character.ToString();
        listTileChars.Items[currentTileCharIndex + 1].SubItems[2].Text = nextChar.Color.ToString();
        RedrawTile();
        RedrawMap();
        SetModified();
      }
      // move selection to next in any case
      listTileChars.SelectedIndices.Clear();
      listTileChars.SelectedIndices.Add( currentTileCharIndex + 1 );
    }



    private void pictureTileDisplay_MouseDown( object sender, MouseEventArgs e )
    {
      if ( ( e.Button & System.Windows.Forms.MouseButtons.Left ) != 0 )
      {
        _TileDisplayMouseReleased = true;
        PaintTileChar( e );
      }
      if ( ( e.Button & System.Windows.Forms.MouseButtons.Right ) != 0 )
      {
        FetchTileChar( e );
      }
    }



    private void PaintTileChar( MouseEventArgs e )
    {
      if ( m_CurrentEditedTile == null )
      {
        return;
      }

      int     tx = e.X / 16;
      int     ty = e.Y / 16;

      if ( ( tx < 0 )
      ||   ( tx >= m_CurrentEditedTile.Chars.Width )
      ||   ( ty < 0 )
      ||   ( ty >= m_CurrentEditedTile.Chars.Height ) )
      {
        return;
      }

      int     currentTileCharIndex = tx + ty * m_CurrentEditedTile.Chars.Width;

      var curChar = m_CurrentEditedTile.Chars[tx, ty];

      if ( ( curChar.Character != m_CurrentChar )
      ||   ( curChar.Color != m_CurrentColor ) )
      {
        if ( _TileDisplayMouseReleased )
        {
          _TileDisplayMouseReleased = false;
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, m_CurrentEditedTile.Index ) );
        }

        curChar.Character = m_CurrentChar;
        curChar.Color = m_CurrentColor;
        Modified = true;

        listTileChars.Items[currentTileCharIndex].SubItems[1].Text = curChar.Character.ToString();
        listTileChars.Items[currentTileCharIndex].SubItems[2].Text = curChar.Color.ToString();

        RedrawTile();
        RedrawMap();
        pictureTileDisplay.Invalidate();
      }
    }



    private void FetchTileChar( MouseEventArgs e )
    {
      if ( m_CurrentEditedTile == null )
      {
        return;
      }

      int     tx = e.X / 16;
      int     ty = e.Y / 16;

      if ( ( tx < 0 )
      ||   ( tx >= m_CurrentEditedTile.Chars.Width )
      ||   ( ty < 0 )
      ||   ( ty >= m_CurrentEditedTile.Chars.Height ) )
      {
        return;
      }

      int     currentTileCharIndex = tx + ty * m_CurrentEditedTile.Chars.Width;

      if ( ( listTileChars.SelectedIndices.Count == 0 )
      ||   ( listTileChars.SelectedIndices[0] != currentTileCharIndex ) )
      {
        listTileChars.SelectedIndices.Clear();
        listTileChars.SelectedIndices.Add( currentTileCharIndex );
      }
    }



    private void pictureTileDisplay_MouseMove( object sender, MouseEventArgs e )
    {
      if ( ( e.Button & System.Windows.Forms.MouseButtons.Left ) != 0 )
      {
        PaintTileChar( e );
      }
      if ( ( e.Button & System.Windows.Forms.MouseButtons.Right ) != 0 )
      {
        FetchTileChar( e );
      }
    }



    private void checkShowGrid_CheckedChanged( object sender, EventArgs e )
    {
      m_MapProject.ShowGrid = checkShowGrid.Checked;
      SetModified();
      Redraw();
    }



    /// <summary>
    /// Mirror the auto-tiling checkbox state into the project so it
    /// persists in the save file. SetModified marks the project dirty
    /// so the change actually survives the next save (otherwise a
    /// pure toggle with no other edits could be silently dropped on
    /// app close).
    /// </summary>
    private void checkAutoTiling_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      if ( m_MapProject.AutoTiling == checkAutoTiling.Checked ) return;
      m_MapProject.AutoTiling = checkAutoTiling.Checked;
      SetModified();
    }



    /// <summary>
    /// Mirror the lock-placement-color checkbox into the project so it
    /// persists. When on, the tile-pick reset that normally snaps the
    /// placement color back to "Default" is skipped (see
    /// PopulateTileCharList), so the user's chosen color sticks across tile
    /// selections. Same persistence pattern as auto-tiling.
    /// </summary>
    private void checkLockColor_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      if ( m_MapProject.LockTilePlacementColor == checkLockColor.Checked ) return;
      m_MapProject.LockTilePlacementColor = checkLockColor.Checked;
      SetModified();
    }



    private void btnZoomIn_Click( object sender, EventArgs e )
    {
      SetMapZoomPercent( m_MapZoomPercent + MapZoomStepPercent );
    }



    private void btnZoomOut_Click( object sender, EventArgs e )
    {
      SetMapZoomPercent( m_MapZoomPercent - MapZoomStepPercent );
    }



    private void btnCloneTile_Click( DecentForms.ControlBase Sender )
    {
      if ( m_CurrentEditedTile == null )
      {
        return;
      }
      var     clonedTile = new Formats.MapProject.Tile();
      clonedTile.Name = MakeTileNameUnique( m_CurrentEditedTile.Name );

      clonedTile.Chars.Resize( m_CurrentEditedTile.Chars.Width, m_CurrentEditedTile.Chars.Height );

      for ( int i = 0; i < m_CurrentEditedTile.Chars.Width; ++i )
      {
        for ( int j = 0; j < m_CurrentEditedTile.Chars.Height; ++j )
        {
          var origChar = m_CurrentEditedTile.Chars[i,j];
          clonedTile.Chars[i, j].Character  = origChar.Character;
          clonedTile.Chars[i,j].Color       = origChar.Color;
        }
      }

      int indexToInsertAt = m_MapProject.Tiles.Count;
      if ( listTileInfo.SelectedIndices.Count > 0 )
      {
        indexToInsertAt = listTileInfo.SelectedIndices[0] + 1;
      }
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileAdd( this, m_MapProject, indexToInsertAt ) );

      AddTile( indexToInsertAt, clonedTile );
    }



    private string MakeTileNameUnique( string OrigName )
    {
      if ( !m_MapProject.Tiles.Any( t => t.Name == OrigName ) )
      {
        return OrigName;
      }
      int     copyIndex = 2;

      // is there a index at the end?
      int spacePos = OrigName.LastIndexOf( ' ' );
      if ( spacePos != -1 )
      {
        // use existing index as starting point
        if ( int.TryParse( OrigName.Substring( spacePos + 1 ), out copyIndex ) )
        {
          ++copyIndex;
          OrigName = OrigName.Substring( 0, spacePos );
        }
      }

      
      string  newName = OrigName + " " + copyIndex;

      while ( m_MapProject.Tiles.Any( t => t.Name == newName ) )
      {
        ++copyIndex;
        newName = OrigName + " " + copyIndex;
      }
      return newName;
    }



    private void characterEditor_Modified( List<int> AffectedChars )
    {
      panelCharacters.Invalidate();
      RedrawMap();
      RedrawColorChooser();
      RedrawTile();
      SetModified();
    }



    // Modeless dialog reference — kept so a second click on the toolbar
    // button re-focuses the existing dialog instead of opening a new one.
    private Dialogs.DlgDisplayFilters  m_DisplayFiltersDialog;



    private void checkFilterEnabled_CheckedChanged( object sender, EventArgs e )
    {
      // Session-only bypass toggle: does NOT persist, does NOT mutate the
      // per-filter Enabled flags. Just repaints so the PostPaint gate picks
      // up the new checkbox state.
      pictureEditor.Invalidate();
    }



    private void btnDisplayFilters_Click( object sender, EventArgs e )
    {
      // Re-focus if already open. Checking IsDisposed first because the form
      // nulls its handles on close, and WinForms raises an exception if you
      // call Focus() on a disposed form.
      if ( ( m_DisplayFiltersDialog != null )
      &&   ( !m_DisplayFiltersDialog.IsDisposed ) )
      {
        m_DisplayFiltersDialog.Activate();
        return;
      }

      var pipeline = ( Core != null ) && ( Core.Settings != null )
                     ? Core.Settings.DisplayFilters : null;
      if ( pipeline == null )
      {
        return;
      }

      m_DisplayFiltersDialog = new Dialogs.DlgDisplayFilters(
          pipeline,
          () =>
          {
            // Callback fires on any pipeline edit in the dialog. Filters
            // run in PostPaint so all we need to trigger is a repaint; no
            // RedrawMap needed since the underlying DisplayPage hasn't
            // changed.
            pictureEditor.Invalidate();
          },
          Core );
      m_DisplayFiltersDialog.FormClosed += ( s, args ) => m_DisplayFiltersDialog = null;
      m_DisplayFiltersDialog.Show( this.FindForm() );
    }



    private void btnCopyImage_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }

      // create a full image of the complete map
      var fullImage = new GR.Image.MemoryImage( m_CurrentMap.TileSpacingX * m_CurrentMap.Tiles.Width * 8,
                                                m_CurrentMap.TileSpacingY * m_CurrentMap.Tiles.Height * 8,
                                                GR.Drawing.PixelFormat.Format32bppRgb );

      uint    bgColor = (uint)m_MapProject.BackgroundColor;
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.AlternativeBackgroundColor != -1 ) )
      {
        bgColor = (uint)m_CurrentMap.AlternativeBackgroundColor;
      }
      fullImage.Box( 0, 0, fullImage.Width, fullImage.Height, m_MapProject.Charset.Colors.Palette.ColorValues[bgColor] );

      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );
      bool needsCoverage = false;
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        var tileToCheck = m_MapProject.Tiles[i];
        if ( ( tileToCheck.Chars.Width > spacingX )
        ||   ( tileToCheck.Chars.Height > spacingY ) )
        {
          needsCoverage = true;
          break;
        }
      }
      bool[,] coveredTiles = needsCoverage ? new bool[m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height] : null;

      for ( int y = 0; y < m_CurrentMap.Tiles.Height; ++y )
      {
        for ( int x = 0; x < m_CurrentMap.Tiles.Width; ++x )
        {
          if ( ( coveredTiles != null )
          &&   ( coveredTiles[x, y] ) )
          {
            continue;
          }
          var tileIndex = m_CurrentMap.Tiles[x,y];
          if ( ( tileIndex < 0 )
          ||   ( tileIndex >= m_MapProject.Tiles.Count ) )
          {
            continue;
          }
          var tile = m_MapProject.Tiles[tileIndex];

          var alternativeSettings = new Types.AlternativeColorSettings()
          {
            BackgroundColor = ( m_CurrentMap.AlternativeBackgroundColor != -1 ) ? m_CurrentMap.AlternativeBackgroundColor : m_MapProject.BackgroundColor,
            MultiColor1     = ( m_CurrentMap.AlternativeMultiColor1 != -1 ) ? m_CurrentMap.AlternativeMultiColor1 : m_MapProject.MultiColor1,
            MultiColor2     = ( m_CurrentMap.AlternativeMultiColor2 != -1 ) ? m_CurrentMap.AlternativeMultiColor2 : m_MapProject.MultiColor2,
            BGColor4        = ( m_CurrentMap.AlternativeBGColor4 != -1 ) ? m_CurrentMap.AlternativeBGColor4 : m_MapProject.BGColor4,
            CharMode        = ( m_CurrentMap.AlternativeMode != TextCharMode.UNKNOWN ) ? m_CurrentMap.AlternativeMode : Lookup.TextCharModeFromTextMode( m_MapProject.Mode )
          };

          // Honor per-CHARACTER color overrides for the exported image
          // too, so "Copy map to clipboard as image" produces what the
          // editor shows on screen. Same per-char lookup pattern as the
          // editor's RedrawMap render.
          int copyCharBaseX = x * m_CurrentMap.TileSpacingX;
          int copyCharBaseY = y * m_CurrentMap.TileSpacingY;
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              int charMapX = copyCharBaseX + i;
              int charMapY = copyCharBaseY + j;
              int charOverride = -1;
              if ( ( charMapX < m_CurrentMap.TileColorOverrides.Width )
              &&   ( charMapY < m_CurrentMap.TileColorOverrides.Height ) )
              {
                charOverride = m_CurrentMap.TileColorOverrides[charMapX, charMapY];
              }
              alternativeSettings.CustomColor = ( charOverride >= 0 )
                                                ? charOverride
                                                : tile.Chars[i, j].Color;
              Displayer.CharacterDisplayer.DisplayChar( m_MapProject.Charset,
                                                        tile.Chars[i, j].Character,
                                                        fullImage,
                                                        ( x * m_CurrentMap.TileSpacingX + i ) * 8,
                                                        ( y * m_CurrentMap.TileSpacingY + j ) * 8,
                                                        alternativeSettings );
            }
          }
          if ( coveredTiles != null )
          {
            int coverTilesX = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Width / (float)spacingX ) );
            int coverTilesY = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Height / (float)spacingY ) );
            for ( int coverY = 0; coverY < coverTilesY; ++coverY )
            {
              int targetY = y + coverY;
              if ( targetY >= m_CurrentMap.Tiles.Height )
              {
                break;
              }
              for ( int coverX = 0; coverX < coverTilesX; ++coverX )
              {
                int targetX = x + coverX;
                if ( targetX >= m_CurrentMap.Tiles.Width )
                {
                  break;
                }
                coveredTiles[targetX, targetY] = true;
              }
            }
          }
        }
      }

      Clipboard.SetImage( fullImage.GetAsBitmap() );
    }



    private void btnExportCharset_Click( object sender, EventArgs e )
    {
      System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

      saveDlg.Title = "Save Charset Project as";
      saveDlg.Filter = FilterString( Constants.FILEFILTER_CHARSET );

      if ( saveDlg.ShowDialog() != DialogResult.OK )
      {
        return;
      }
      string    extension = GR.Path.GetExtension( saveDlg.FileName );

      if ( extension.ToUpper() == ".CHARSETPROJECT" )
      {
        GR.IO.File.WriteAllBytes( saveDlg.FileName, m_MapProject.Charset.SaveToBuffer() );
      }
      else
      {
        GR.IO.File.WriteAllBytes( saveDlg.FileName, m_MapProject.Charset.SaveCharsetToBuffer() );
      }
    }



    private string FilterString( string Source )
    {
      return Source.Substring( 0, Source.Length - 1 );
    }



    private void btnMoveMapDown_Click( object sender, EventArgs e )
    {
      if ( ( comboMaps.SelectedIndex == -1 )
      ||   ( comboMaps.Items.Count < 2 )
      ||   ( comboMaps.SelectedIndex + 1 >= comboMaps.Items.Count ) )
      {
        return;
      }
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapSwap( this, m_MapProject, comboMaps.SelectedIndex, comboMaps.SelectedIndex + 1 ) );

      int   curIndex = comboMaps.SelectedIndex;
      SwapMap( comboMaps.SelectedIndex, comboMaps.SelectedIndex + 1 );
      comboMaps.SelectedIndex = curIndex + 1;
      SetModified();
    }

    

    private void btnMoveMapUp_Click( object sender, EventArgs e )
    {
      if ( ( comboMaps.SelectedIndex <= 0 )
      ||   ( comboMaps.Items.Count < 2 ) )
      {
        return;
      }
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapSwap( this, m_MapProject, comboMaps.SelectedIndex - 1, comboMaps.SelectedIndex ) );

      int   curIndex = comboMaps.SelectedIndex;
      SwapMap( comboMaps.SelectedIndex - 1, comboMaps.SelectedIndex );
      comboMaps.SelectedIndex = curIndex - 1;
      SetModified();
    }



    public void SwapMap( int MapIndex1, int MapIndex2 )
    {
      if ( ( MapIndex1 < 0 )
      ||   ( MapIndex1 >= m_MapProject.Maps.Count )
      ||   ( MapIndex2 < 0 )
      ||   ( MapIndex2 >= m_MapProject.Maps.Count ) )
      {
        return;
      }
      if ( MapIndex1 > MapIndex2 )
      {
        var map1 = m_MapProject.Maps[MapIndex1];
        m_MapProject.Maps.RemoveAt( MapIndex1 );
        m_MapProject.Maps.Insert( MapIndex2, map1 );

        var old1 = comboMaps.Items[MapIndex1];
        comboMaps.Items.RemoveAt( MapIndex1 );
        comboMaps.Items.Insert( MapIndex2, old1 );
      }
      else
      {
        var map2 = m_MapProject.Maps[MapIndex2];
        m_MapProject.Maps.RemoveAt( MapIndex2 );
        m_MapProject.Maps.Insert( MapIndex1, map2 );

        var old2 = comboMaps.Items[MapIndex2];
        comboMaps.Items.RemoveAt( MapIndex2 );
        comboMaps.Items.Insert( MapIndex1, old2 );
      }


      // Keep StartMapIndex pointing at the same MAP after the swap, not the
      // same INDEX. Three cases: it's MapIndex1 → becomes MapIndex2; it's
      // MapIndex2 → becomes MapIndex1; otherwise it's unaffected.
      if ( m_MapProject.StartMapIndex == MapIndex1 )
      {
        m_MapProject.StartMapIndex = MapIndex2;
      }
      else if ( m_MapProject.StartMapIndex == MapIndex2 )
      {
        m_MapProject.StartMapIndex = MapIndex1;
      }

      var item1 = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[MapIndex1];
      var item2 = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[MapIndex2];

      item1.first = FormatMapDisplayName( MapIndex1, item1.second );
      item2.first = FormatMapDisplayName( MapIndex2, item2.second );

      comboMaps.Items.RemoveAt( MapIndex2 );
      comboMaps.Items.Insert( MapIndex2, item2 );
      comboMaps.Items.RemoveAt( MapIndex1 );
      comboMaps.Items.Insert( MapIndex1, item1 );
    }



    private void characterEditor_CharactersShifted( int[] OldToNew, int[] NewToOld )
    {
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, i ), false );
      }
      foreach ( var tile in m_MapProject.Tiles )
      {
        for ( int i = 0; i < tile.Chars.Width; ++i )
        {
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            tile.Chars[i,j].Character = (byte)OldToNew[tile.Chars[i, j].Character];
          }
        }
      }
      for ( int i = 0; i < m_MapProject.Charset.TotalNumberOfCharacters; ++i )
      {
        RebuildCharImage( i );
        panelCharacters.Items[i].MemoryImage = m_MapProject.Charset.Characters[i].Tile.Image;
      }
      UpdateCurrentTileCharacterList();
      RedrawMap();
      RedrawTile();
    }



    private void comboMapProjectMode_SelectedIndexChanged( object sender, EventArgs e )
    {
      //TODO Undo!

      m_MapProject.Mode = (TextMode)comboMapProjectMode.SelectedIndex;

      // TODO - that should change all kind of values inside the charset! (TotalNumberOfCharacters!)
      m_MapProject.Charset.Mode         = Lookup.TextCharModeFromTextMode( m_MapProject.Mode );
      characterEditor.CharsetUpdated( m_MapProject.Charset );

      m_MapProject.Charset.Colors.Palettes[0] = Core.Imaging.PaletteFromMachine( Lookup.MachineTypeFromTextMode( m_MapProject.Mode ) );

      for ( int i = 0; i < m_MapProject.Charset.TotalNumberOfCharacters; ++i )
      {
        RebuildCharImage( i );
      }
      Modified = true;
      panelCharacters.Invalidate();
      RedrawColorChooser();
      RedrawMap();
    }



    private void btnExport_Click( DecentForms.ControlBase Sender )
    {
      ExportCurrentMap();
    }



    /// <summary>
    /// Run the map export with the current Export-tab settings and return
    /// HandleExport's success flag. Shared by the Export button and the
    /// "Map Project -> Export Map" menu item / Alt+X shortcut, so the export
    /// can be triggered from any tab — it reads the Export-tab combos, which
    /// retain their values regardless of which tab is currently shown.
    /// </summary>
    private bool ExportCurrentMap()
    {
      var exportInfo = new ExportMapInfo()
      {
        Map             = m_MapProject,
        RowByRow        = ( comboExportOrientation.SelectedIndex == 0 ),
        ExportType      = (MapExportType)comboExportData.SelectedIndex,
        SelectedTiles   = m_SelectedTiles,
        CurrentMap      = m_CurrentMap
      };

      editDataExport.Text = "";
      return m_ExportForm.HandleExport( exportInfo, editDataExport, DocumentInfo );
    }



    private void exportMapToolStripMenuItem_Click( object sender, EventArgs e )
    {
      // The Export button only lives on the Export tab; this menu item and
      // the Alt+X shortcut run the same export from whatever tab is open.
      // Beep on success so the user gets a confirmation cue when the Export
      // tab (and its output box) isn't in view.
      if ( ExportCurrentMap() )
      {
        System.Media.SystemSounds.Beep.Play();
      }
    }



    private void mapControlsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      string controls =
          "MAP EDITOR CONTROLS\r\n"
        + "\r\n"
        + "Feature     Action\r\n"
        + "\r\n"
        + "MARKERS\r\n"
        + "  Add        Shift + click an empty cell\r\n"
        + "  Select     Click the marker\r\n"
        + "  Move       Click and drag the marker (on or off the map)\r\n"
        + "  Resize     Shift + drag the marker, or the H+/H-/V+/V- buttons\r\n"
        + "  Delete     Select the marker, then press Delete\r\n"
        + "  Deselect   Click an empty cell, or press Escape\r\n"
        + "\r\n"
        + "ENTITIES\r\n"
        + "  Select     Click the entity\r\n"
        + "  Move       Click and drag the entity\r\n"
        + "  Delete     Select the entity, then press Delete\r\n"
        + "\r\n"
        + "MAP\r\n"
        + "  Export     Alt+X, or Map Project menu -> Export Map";
      System.Windows.Forms.MessageBox.Show( this, controls, "Map controls",
        System.Windows.Forms.MessageBoxButtons.OK,
        System.Windows.Forms.MessageBoxIcon.Information );
    }



    private void ApplyExportSettingsToUI()
    {
      if ( m_MapProject == null )
      {
        return;
      }
      m_ApplyingExportSettings = true;
      try
      {
        if ( comboExportData.Items.Count > 0 )
        {
          comboExportData.SelectedIndex = ClampExportIndex( m_MapProject.Settings.ExportDataIndex, comboExportData.Items.Count );
        }
        if ( comboExportOrientation.Items.Count > 0 )
        {
          comboExportOrientation.SelectedIndex = ClampExportIndex( m_MapProject.Settings.ExportOrientationIndex, comboExportOrientation.Items.Count );
        }
        if ( comboExportMethod.Items.Count > 0 )
        {
          comboExportMethod.SelectedIndex = ClampExportIndex( m_MapProject.Settings.ExportMethodIndex, comboExportMethod.Items.Count );
        }
        ApplyExportSettingsToForm();
      }
      finally
      {
        m_ApplyingExportSettings = false;
      }
    }

    private void ApplyExportSettingsToForm()
    {
      if ( m_ExportForm != null )
      {
        m_ExportForm.ApplyExportSettings( m_MapProject.Settings);
      }
    }

    private void UpdateExportSettingsFromUI( bool MarkModified )
    {
      if ( m_MapProject == null )
      {
        return;
      }
      m_MapProject.Settings.ExportDataIndex = ( comboExportData.SelectedIndex >= 0 ) ? comboExportData.SelectedIndex : 0;
      m_MapProject.Settings.ExportOrientationIndex = ( comboExportOrientation.SelectedIndex >= 0 ) ? comboExportOrientation.SelectedIndex : 0;
      m_MapProject.Settings.ExportMethodIndex = ( comboExportMethod.SelectedIndex >= 0 ) ? comboExportMethod.SelectedIndex : 0;
      if ( m_ExportForm != null )
      {
        m_ExportForm.UpdateExportSettings( m_MapProject.Settings);
      }
      if ( MarkModified )
      {
        SetModified();
      }
    }

    private void ExportSettingsChanged()
    {
      if ( m_ApplyingExportSettings )
      {
        return;
      }
      UpdateExportSettingsFromUI( true );
    }

    private void ExportSettingsChanged( object sender, EventArgs e )
    {
      ExportSettingsChanged();
    }

    private void ExportForm_SettingsChanged( object sender, EventArgs e )
    {
      ExportSettingsChanged();
    }

    private int ClampExportIndex( int Index, int Count )
    {
      if ( Count <= 0 )
      {
        return -1;
      }
      if ( ( Index < 0 )
      ||   ( Index >= Count ) )
      {
        return 0;
      }
      return Index;
    }



    private void comboExportMethod_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_ExportForm != null )
      {
        m_ExportForm.SettingsChanged -= ExportForm_SettingsChanged;
        m_ExportForm.Dispose();
        m_ExportForm = null;
      }

      editDataExport.Text = "";

      var item = (GR.Generic.Tupel<string, Type>)comboExportMethod.SelectedItem;
      if ( ( item == null )
      ||   ( item.second == null ) )
      {
        return;
      }
      m_ExportForm = (ExportMapFormBase)Activator.CreateInstance( item.second, new object[] { Core } );
      m_ExportForm.Parent = panelExport;
      m_ExportForm.CreateControl();
      m_ExportForm.SettingsChanged += ExportForm_SettingsChanged;
      ApplyExportSettingsToForm();
      UpdateExportDataDropdownsState();
      if ( !m_ApplyingExportSettings )
      {
        UpdateExportSettingsFromUI( true );
      }
    }



    // The Game Binary exporter always writes the full tileset + all maps in row-major
    // order, so it ignores Export Data / Orientation. Grey those dropdowns out so the
    // UI does not imply they have an effect.
    private void UpdateExportDataDropdownsState()
    {
      bool usesExportDataAndOrientation = !( m_ExportForm is ExportMapAsGameBinary );
      comboExportData.Enabled = usesExportDataAndOrientation;
      comboExportOrientation.Enabled = usesExportDataAndOrientation;
    }



    private void btnImport_Click( DecentForms.ControlBase Sender )
    {
      // Undo?
      var undo = new Undo.UndoMapCharsetChange( m_MapProject, this );

      if ( m_ImportForm.HandleImport( m_MapProject, this ) )
      {
        Modified = true;
        DocumentInfo.UndoManager.AddUndoTask( undo );
        RedrawMap();
        RedrawColorChooser();
        RedrawTile();
      }
    }



    private void comboImportMethod_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_ImportForm != null )
      {
        m_ImportForm.Dispose();
        m_ImportForm = null;
      }

      var item = (GR.Generic.Tupel<string, Type>)comboImportMethod.SelectedItem;
      if ( ( item == null )
      ||   ( item.second == null ) )
      {
        return;
      }
      m_ImportForm = (ImportMapFormBase)Activator.CreateInstance( item.second, new object[] { Core } );
      m_ImportForm.Parent = panelImport;
      m_ImportForm.Size = panelImport.ClientSize;
      m_ImportForm.CreateControl();
    }



    internal void CharsetChanged()
    {
      characterEditor.CharsetUpdated( m_MapProject.Charset );
      RedrawMap();
      RedrawColorChooser();
      RedrawTile();
    }



    public override void OnApplicationEvent( ApplicationEvent Event )
    {
      switch ( Event.EventType )
      {
        case ApplicationEvent.Type.DEFAULT_PALETTE_CHANGED:
          {
            bool  prevModified = Modified;

            if ( !string.IsNullOrEmpty( Event.OriginalValue ) )
            {
              Core.Imaging.ApplyPalette( (PaletteType)Enum.Parse( typeof( PaletteType ), Event.OriginalValue, true ),
                                         Lookup.PaletteTypeFromTextCharMode( m_MapProject.Charset.Mode ),
                                         m_MapProject.Charset.Colors );
            }
            else
            {
              Core.Imaging.ApplyPalette( Lookup.PaletteTypeFromTextCharMode( m_MapProject.Charset.Mode ),
                                         Lookup.PaletteTypeFromTextCharMode( m_MapProject.Charset.Mode ),
                                         m_MapProject.Charset.Colors );

            }
            characterEditor.ColorsChanged();
            RedrawMap();
            RedrawColorChooser();
            RedrawTile();

            Modified = prevModified;
          }
          break;
      }
    }



    protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
    {
      if ( keyData == Keys.Escape )
      {
        // A selected marker or entity is dropped on Escape regardless of
        // focus — it is the most common thing the user wants Escape to undo.
        if ( ( m_SelectedMarker != null ) || ( m_SelectedEntity != null ) )
        {
          ClearMarkerEntitySelection();
          return true;
        }
        if ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.ESCAPE ) )
        {
          // ESC peels back one layer of editing state at a time, in order
          // from most "live" to least:
          //   1. Floating selection (a paste in flight) — drop it.
          //   2. Right-click selection of marker / entity / tile cell —
          //      clear the highlight.
          //   3. Non-default tool mode — revert to SINGLE_TILE (the
          //      default place/pick tool). Lets the user "back out" of
          //      Rect / Fill / Select / Marker / Entity / Passable in
          //      one keypress without having to click the tile-edit
          //      button.
          // Each step reports whether it actually did something so we
          // know whether to swallow the key — falling through when
          // nothing was peelable lets parent/global ESC handlers (tab
          // close shortcuts etc.) react if they want to.
          if ( RemoveFloatingSelection() )
          {
            return true;
          }
          if ( ClearMarkerEntitySelection() )
          {
            return true;
          }
          if ( ( m_ToolMode != ToolMode.SINGLE_TILE )
          &&   ( btnToolEdit != null )
          &&   ( !btnToolEdit.Checked ) )
          {
            // Setting Checked re-enters CheckedChanged which runs the
            // full activation path (m_ToolMode = SINGLE_TILE,
            // UncheckOtherToolButtons, AfterToolChange). Same code path
            // as a user-initiated click on the tile-edit button.
            btnToolEdit.Checked = true;
            return true;
          }
        }
      }
      else if ( keyData == Keys.Delete )
      {
        // Don't steal Delete from text editors / numeric inputs. The
        // COPY_PASTE focus-reason returns true exactly when focus is on a
        // TextBox child of tabEditor — i.e., somewhere we want the OS
        // default Delete behavior. So only handle our key when that check
        // is false.
        if ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) )
        {
          // Order: marker/entity selection first (they take precedence in
          // their respective tool modes), then fall back to the right-
          // clicked tile cell. Both helpers reuse the existing toolbar
          // button click handlers — same confirmation prompt, same undo
          // entry, same redraw. Sender/EventArgs are unused by those
          // handlers so we can pass nulls / Empty.
          if ( ( m_ToolMode == ToolMode.MARKER )
          &&   ( m_SelectedMarker != null ) )
          {
            btnDeleteSelectedMarker_Click( null, EventArgs.Empty );
            return true;
          }
          if ( ( m_ToolMode == ToolMode.ENTITY )
          &&   ( m_SelectedEntity != null ) )
          {
            btnDeleteSelectedEntity_Click( null, EventArgs.Empty );
            return true;
          }
          if ( TryDeleteRightClickedTile() )
          {
            return true;
          }
        }
      }
      else if ( keyData == ( Keys.Alt | Keys.X ) )
      {
        // Alt+X exports the current map from any tab — the same action as
        // the Export-tab button and the "Map Project -> Export Map" menu
        // item. Handling it here in ProcessCmdKey scopes it to the focused
        // map document, so it never fires while another document is active.
        exportMapToolStripMenuItem_Click( null, EventArgs.Empty );
        return true;
      }
      else if ( keyData == Keys.G )
      {
        // Toggle the grid. Scoped strictly to the Map tab so other tabs'
        // arrow / typing flow stays untouched. Skipped when focus is on
        // a TextBox (so the user can type the letter g normally) — the
        // COPY_PASTE focus check returns true exactly for TextBox; combo
        // / list typeahead is intentionally overridden so the shortcut
        // works while the tile picker has focus.
        if ( ( tabMapEditor != null )
        &&   ( tabMapEditor.SelectedPage == tabEditor )
        &&   ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) ) )
        {
          ToggleGridShortcut();
          return true;
        }
      }
      else if ( keyData == Keys.S )
      {
        // Activate the rectangle floating-selection tool (SELECT mode).
        // Same scoping rules as the G shortcut: only on the Map tab and
        // never when a TextBox / numeric input has focus, so the user
        // can still type lowercase 's' in name fields. Combo / list
        // typeahead is intentionally overridden — same as G.
        if ( ( tabMapEditor != null )
        &&   ( tabMapEditor.SelectedPage == tabEditor )
        &&   ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) ) )
        {
          if ( ( btnToolSelect != null )
          &&   ( !btnToolSelect.Checked ) )
          {
            // CheckedChanged sets m_ToolMode and unchecks siblings.
            btnToolSelect.Checked = true;
          }
          return true;
        }
      }
      else if ( keyData == Keys.P )
      {
        // Activate the per-character "blocked" override tool. Same
        // scoping as G/S — Map tab only, not on a TextBox. Additionally
        // gated on m_IsViewingRevision: the revision view is read-only
        // and PASSABLE editing must not start there. Pressing P while
        // viewing a revision falls through to the normal letter-key
        // handling (no-op).
        if ( ( tabMapEditor != null )
        &&   ( tabMapEditor.SelectedPage == tabEditor )
        &&   ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) )
        &&   ( !m_IsViewingRevision ) )
        {
          if ( ( btnToolPassable != null )
          &&   ( !btnToolPassable.Checked ) )
          {
            btnToolPassable.Checked = true;
          }
          return true;
        }
      }
      else if ( ( keyData == Keys.OemCloseBrackets )
      ||        ( keyData == Keys.OemOpenBrackets )
      ||        ( keyData == ( Keys.Shift | Keys.OemCloseBrackets ) )
      ||        ( keyData == ( Keys.Shift | Keys.OemOpenBrackets ) ) )
      {
        // Brightness-shift shortcuts. Same scoping as G/S/P — Map tab
        // only, not on a TextBox, not in revision view. The bracket
        // keys aren't bound elsewhere on the Map tab so they're safe
        // to claim:
        //   ]         → linear brightness up
        //   [         → linear brightness down
        //   Shift+]   → hue-preserving brightness up
        //   Shift+[   → hue-preserving brightness down
        if ( ( tabMapEditor != null )
        &&   ( tabMapEditor.SelectedPage == tabEditor )
        &&   ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.COPY_PASTE ) )
        &&   ( !m_IsViewingRevision )
        &&   ( Core?.Settings != null ) )
        {
          if ( keyData == Keys.OemCloseBrackets )
          {
            // Linear-disabled flag silently swallows the key — no
            // shift, but we don't want it falling through to a beep
            // or some other accidental binding either.
            if ( Core.Settings.BrightnessLinearEnabled )
            {
              ApplyBrightnessShift( Core.Settings.BrightnessLinearUp );
            }
            return true;
          }
          if ( keyData == Keys.OemOpenBrackets )
          {
            if ( Core.Settings.BrightnessLinearEnabled )
            {
              ApplyBrightnessShift( Core.Settings.BrightnessLinearDown );
            }
            return true;
          }
          if ( keyData == ( Keys.Shift | Keys.OemCloseBrackets ) )
          {
            ApplyBrightnessShift( Core.Settings.BrightnessHueUp );
            return true;
          }
          if ( keyData == ( Keys.Shift | Keys.OemOpenBrackets ) )
          {
            ApplyBrightnessShift( Core.Settings.BrightnessHueDown );
            return true;
          }
        }
      }
      else if ( ( keyData == ( Keys.Alt | Keys.Left ) )
      ||        ( keyData == ( Keys.Alt | Keys.Right ) )
      ||        ( keyData == ( Keys.Alt | Keys.Up ) )
      ||        ( keyData == ( Keys.Alt | Keys.Down ) ) )
      {
        // ALT+arrow reorders the selected character(s) within the
        // character sheet — same effect as the four Move-character arrows
        // on the Character set tab. (NOT pixel-shift; that's a different
        // pair of buttons elsewhere in the editor.) Strict scope:
        //  - Character set tab must be the active page; same shortcut on
        //    other tabs would clash with arrow-key navigation there.
        //  - At least one character must be selected — otherwise
        //    SwapCharacter is a no-op and we'd needlessly swallow the key.
        if ( TryMoveCharacterByAltArrow( keyData ) )
        {
          return true;
        }
      }
      return base.ProcessCmdKey( ref msg, keyData );
    }



    /// <summary>
    /// Implementation for the ALT+arrow shortcut wired up in ProcessCmdKey.
    /// Calls into the same <see cref="Controls.CharacterEditor.SwapCharacter"/>
    /// the Move-character toolbar buttons drive, with the same hardcoded
    /// row-width of 16 (matches the grid the panelCharacters list uses).
    /// Returns true when the key was handled so the caller can swallow it;
    /// returns false when the gates fail so it falls through to whatever
    /// has focus.
    /// </summary>
    private bool TryMoveCharacterByAltArrow( Keys keyData )
    {
      if ( tabMapEditor == null )                       return false;
      if ( tabMapEditor.SelectedPage != tabCharset )    return false;
      if ( characterEditor == null )                    return false;
      // No character selected — SwapCharacter would early-return anyway,
      // but checking up front lets us NOT swallow the keypress so other
      // handlers / focus chains can react to plain Alt+arrow.
      if ( ( panelCharacters == null )
      ||   ( panelCharacters.SelectedIndex < 0 ) )
      {
        return false;
      }

      switch ( keyData )
      {
        case Keys.Alt | Keys.Left:  characterEditor.SwapCharacter(  -1 ); return true;
        case Keys.Alt | Keys.Right: characterEditor.SwapCharacter(   1 ); return true;
        case Keys.Alt | Keys.Up:    characterEditor.SwapCharacter( -16 ); return true;
        case Keys.Alt | Keys.Down:  characterEditor.SwapCharacter(  16 ); return true;
      }
      return false;
    }



    /// <summary>
    /// Replace the currently right-click-selected map cell with tile 0
    /// (treated as the empty/background tile by convention). Returns true
    /// if a delete actually happened so <see cref="ProcessCmdKey"/> can
    /// swallow the key. Cleared selection or wrong-tool-mode → false and
    /// the key falls through to whatever else might want it.
    /// </summary>
    private bool TryDeleteRightClickedTile()
    {
      if ( !IsMapEditable ) return false;
      if ( ( m_SelectedTilePos.X < 0 )
      ||   ( m_SelectedTilePos.Y < 0 )
      ||   ( m_SelectedTilePos.X >= m_CurrentMap.Tiles.Width )
      ||   ( m_SelectedTilePos.Y >= m_CurrentMap.Tiles.Height ) )
      {
        return false;
      }
      // Same tool-mode gate the highlight uses — Delete in MARKER/ENTITY/
      // SELECT mode would be ambiguous (the user might be expecting it to
      // act on the marker/entity selection or the tile rectangle), so we
      // bail and let those modes wire their own Delete if they ever need
      // one.
      if ( ( m_ToolMode == ToolMode.MARKER )
      ||   ( m_ToolMode == ToolMode.ENTITY )
      ||   ( m_ToolMode == ToolMode.SELECT ) )
      {
        return false;
      }

      int x = m_SelectedTilePos.X;
      int y = m_SelectedTilePos.Y;
      // Already empty? Nothing to undo, but consume the key so it doesn't
      // beep — the selection IS valid, just the action is a no-op.
      if ( m_CurrentMap.Tiles[x, y] == 0 )
      {
        return true;
      }

      // Snapshot a region big enough to cover the OLD tile's footprint —
      // a 2x2-char tile spans 1+ map cells, and undo needs to know about
      // every cell whose appearance changes. Using the tile-cell footprint
      // here also keeps Ctrl+Z bringing back ALL the characters, not just
      // the upper-left one.
      int oldIndex = m_CurrentMap.Tiles[x, y];
      int undoW = 1, undoH = 1;
      if ( ( oldIndex >= 0 )
      &&   ( oldIndex < m_MapProject.Tiles.Count ) )
      {
        GetTileCellFootprint( m_MapProject.Tiles[oldIndex], out undoW, out undoH );
      }
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapTilesChange( this, m_CurrentMap, x, y, undoW, undoH ) );
      m_CurrentMap.Tiles[x, y] = 0;
      // Clearing a cell to "empty" means dropping per-character overrides
      // for every char the OLD tile actually occupied — otherwise the
      // overrides would silently linger and tint whatever the user paints
      // over the empty cell next. Footprint is max(spacing, Chars dims):
      // when spacing < Chars (e.g. spacing=1 with a 2x2 tile) the tile
      // renders 4 chars, so clearing only spacing²=1 char would leave 3
      // stale overrides. Computed BEFORE Tiles[x,y]=0 above so oldIndex
      // is still valid here.
      int clrFootprintX = m_CurrentMap.TileSpacingX;
      int clrFootprintY = m_CurrentMap.TileSpacingY;
      if ( ( oldIndex >= 0 )
      &&   ( oldIndex < m_MapProject.Tiles.Count ) )
      {
        var oldTile = m_MapProject.Tiles[oldIndex];
        if ( oldTile.Chars.Width  > clrFootprintX ) clrFootprintX = oldTile.Chars.Width;
        if ( oldTile.Chars.Height > clrFootprintY ) clrFootprintY = oldTile.Chars.Height;
      }
      int clrCharBaseX = x * m_CurrentMap.TileSpacingX;
      int clrCharBaseY = y * m_CurrentMap.TileSpacingY;
      for ( int dy = 0; dy < clrFootprintY; ++dy )
      {
        for ( int dx = 0; dx < clrFootprintX; ++dx )
        {
          int cx = clrCharBaseX + dx;
          int cy = clrCharBaseY + dy;
          if ( ( cx < m_CurrentMap.TileColorOverrides.Width )
          &&   ( cy < m_CurrentMap.TileColorOverrides.Height ) )
          {
            m_CurrentMap.TileColorOverrides[cx, cy] = -1;
          }
          // Same reasoning for blocked overrides — the tile that owned
          // these chars is being cleared, so any per-char passability
          // override for its footprint is now stale. UndoMapTilesChange
          // (taken above on line 8147) snapshots both layers, so Ctrl+Z
          // restores the old tile + both override layers in one step.
          if ( ( cx < m_CurrentMap.CharBlockedOverrides.Width )
          &&   ( cy < m_CurrentMap.CharBlockedOverrides.Height ) )
          {
            m_CurrentMap.CharBlockedOverrides[cx, cy] = false;
          }
        }
      }
      // Full RedrawMap, not DrawTile — DrawTile only repaints as many
      // characters as the NEW tile occupies, so deleting a 2x2 tile and
      // replacing it with a 1x1 tile-0 leaves three stale 8x8 quadrants
      // showing the old pixels. RedrawMap rebuilds the whole DisplayPage
      // from scratch using the current Tiles[,] grid, which always
      // reflects truth.
      RedrawMap();
      pictureEditor.Invalidate();
      SetModified();
      return true;
    }



    /// <summary>
    /// How many map cells a tile occupies. A 2x2-char tile on a map with
    /// TileSpacingX=TileSpacingY=2 fits in a single 1x1 cell; a 4x2-char
    /// tile on the same map covers 2x1 cells; etc. Used by the selection
    /// highlight and by undo-region computation when deleting.
    /// </summary>
    private void GetTileCellFootprint( Formats.MapProject.Tile tile, out int cellsWide, out int cellsTall )
    {
      cellsWide = 1;
      cellsTall = 1;
      if ( ( tile == null )
      ||   ( m_CurrentMap == null ) )
      {
        return;
      }
      int spacingX = Math.Max( 1, m_CurrentMap.TileSpacingX );
      int spacingY = Math.Max( 1, m_CurrentMap.TileSpacingY );
      cellsWide = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Width  / (float)spacingX ) );
      cellsTall = Math.Max( 1, (int)Math.Ceiling( tile.Chars.Height / (float)spacingY ) );
    }





    private void characterEditor_Load( object sender, EventArgs e )
    {
    }



    // =================================================================
    // Map Strings tab — per-project named text scripts for the in-game
    // 4-line UI message area. List on the left, fields on the right,
    // live preview canvas. Authored text uses inline color tokens:
    // $X (X = 0..F) sets foreground color; $$ emits a literal '$'.
    // Exported by MapProject.GenerateMapStringsAsm; round-trips through
    // the MAP_STRING file chunk.
    // =================================================================

    /// <summary>
    /// Cached glyph data from <see cref="MapProject.MapStringsPreviewFontPath"/>.
    /// Reloaded on path change. null when no font is selected (or the load
    /// failed) — preview falls back to the project's main charset.
    /// File format: 2-byte little-endian load-address header followed by
    /// 8 bytes per glyph (raw 1-bpp char rows).
    /// </summary>
    private byte[] m_MapStringPreviewFont = null;



    private void InitMapStringsTab()
    {
      AttachMapStringEditFieldHandlers();
      RefreshMapStrings();
      UpdateMapStringButtonStates();
      LoadMapStringPreviewFont();   // also refreshes the path textbox + preview
    }



    /// <summary>
    /// (Re)load the binary font file at
    /// <see cref="MapProject.MapStringsPreviewFontPath"/> into
    /// <see cref="m_MapStringPreviewFont"/>. Skips silently if the path
    /// is empty or the file can't be read; the preview falls through to
    /// the project's main charset in that case. Always rebuilds the
    /// preview at the end so the canvas reflects the new font.
    /// </summary>
    private void LoadMapStringPreviewFont()
    {
      m_MapStringPreviewFont = null;
      if ( m_MapProject != null )
      {
        editMapStringFont.Text = m_MapProject.MapStringsPreviewFontPath ?? "";
        string path = m_MapProject.MapStringsPreviewFontPath;
        if ( !string.IsNullOrEmpty( path ) && System.IO.File.Exists( path ) )
        {
          try
          {
            byte[] raw = System.IO.File.ReadAllBytes( path );
            // 2-byte load-address header — strip it.
            if ( raw.Length > 2 )
            {
              m_MapStringPreviewFont = new byte[raw.Length - 2];
              Array.Copy( raw, 2, m_MapStringPreviewFont, 0, raw.Length - 2 );
            }
          }
          catch
          {
            // Bad file / IO error — leave the field null so the preview
            // falls back to the project charset. No exception propagates
            // because preview rendering is non-essential.
            m_MapStringPreviewFont = null;
          }
        }
      }
      else
      {
        editMapStringFont.Text = "";
      }
      RebuildMapStringPreview();
    }



    /// <summary>
    /// Mirror the project's preview-charset offsets into the 3 NumericUpDown
    /// controls. Detaches the ValueChanged handlers for the duration so
    /// populating the controls doesn't dirty the project. Called from the
    /// project-load lifecycle alongside <see cref="LoadMapStringPreviewFont"/>.
    /// </summary>
    private void PopulateMapStringPreviewIndices()
    {
      if ( m_MapProject == null ) return;
      editMapStringLowercase.ValueChanged       -= editMapStringLowercase_ValueChanged;
      editMapStringUppercase.ValueChanged       -= editMapStringUppercase_ValueChanged;
      editMapStringNumbers.ValueChanged         -= editMapStringNumbers_ValueChanged;
      editMapStringTextAreaWidth.ValueChanged   -= editMapStringTextAreaWidth_ValueChanged;
      try
      {
        editMapStringLowercase.Value     = ClampNudByte( m_MapProject.MapStringsLowercaseIndex );
        editMapStringUppercase.Value     = ClampNudByte( m_MapProject.MapStringsUppercaseIndex );
        editMapStringNumbers.Value       = ClampNudByte( m_MapProject.MapStringsNumbersIndex );
        editMapStringTextAreaWidth.Value = ClampNudRange( m_MapProject.MapStringsTextAreaWidth, 1, 255 );
      }
      finally
      {
        editMapStringLowercase.ValueChanged       += editMapStringLowercase_ValueChanged;
        editMapStringUppercase.ValueChanged       += editMapStringUppercase_ValueChanged;
        editMapStringNumbers.ValueChanged         += editMapStringNumbers_ValueChanged;
        editMapStringTextAreaWidth.ValueChanged   += editMapStringTextAreaWidth_ValueChanged;
      }
    }



    private static decimal ClampNudRange( int Value, int Min, int Max )
    {
      if ( Value < Min ) return (decimal)Min;
      if ( Value > Max ) return (decimal)Max;
      return (decimal)Value;
    }



    private static decimal ClampNudByte( int Value )
    {
      if ( Value < 0 ) return 0m;
      if ( Value > 255 ) return 255m;
      return (decimal)Value;
    }



    private void editMapStringLowercase_ValueChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      m_MapProject.MapStringsLowercaseIndex = (int)editMapStringLowercase.Value;
      SetModified();
      RebuildMapStringPreview();
    }



    private void editMapStringUppercase_ValueChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      m_MapProject.MapStringsUppercaseIndex = (int)editMapStringUppercase.Value;
      SetModified();
      RebuildMapStringPreview();
    }



    private void editMapStringNumbers_ValueChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      m_MapProject.MapStringsNumbersIndex = (int)editMapStringNumbers.Value;
      SetModified();
      RebuildMapStringPreview();
    }



    private void btnBrowseMapStringFont_Click( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;

      using ( var dlg = new System.Windows.Forms.OpenFileDialog() )
      {
        dlg.Title  = "Select preview font binary";
        dlg.Filter = "Binary font files (*.bin;*.fnt;*.cset;*.prg)|*.bin;*.fnt;*.cset;*.prg|All files (*.*)|*.*";
        if ( !string.IsNullOrEmpty( m_MapProject.MapStringsPreviewFontPath ) )
        {
          string dir = System.IO.Path.GetDirectoryName( m_MapProject.MapStringsPreviewFontPath );
          if ( !string.IsNullOrEmpty( dir ) && System.IO.Directory.Exists( dir ) )
          {
            dlg.InitialDirectory = dir;
          }
        }
        if ( dlg.ShowDialog( this ) != System.Windows.Forms.DialogResult.OK ) return;

        m_MapProject.MapStringsPreviewFontPath = dlg.FileName ?? "";
        LoadMapStringPreviewFont();
        SetModified();
      }
    }



    /// <summary>
    /// Subscribe the live-edit handlers that mutate the currently selected
    /// MapString. Paired with <see cref="DetachMapStringEditFieldHandlers"/>
    /// so the listbox-driven populate (which writes into these same
    /// controls) doesn't dirty the model. This is the explicit detach
    /// pattern the project mandates instead of a m_Populating gating flag.
    /// </summary>
    private void AttachMapStringEditFieldHandlers()
    {
      editMapStringLabel.TextChanged                    += editMapStringLabel_TextChanged;
      editMapStringLine0.TextChanged                    += editMapStringLine_TextChanged;
      editMapStringLine1.TextChanged                    += editMapStringLine_TextChanged;
      editMapStringLine2.TextChanged                    += editMapStringLine_TextChanged;
      editMapStringLine3.TextChanged                    += editMapStringLine_TextChanged;
      editMapStringLine4.TextChanged                    += editMapStringLine_TextChanged;
      comboMapStringTerminator0.SelectedIndexChanged    += comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator1.SelectedIndexChanged    += comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator2.SelectedIndexChanged    += comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator3.SelectedIndexChanged    += comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator4.SelectedIndexChanged    += comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringLineControl0.SelectedIndexChanged   += comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl1.SelectedIndexChanged   += comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl2.SelectedIndexChanged   += comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl3.SelectedIndexChanged   += comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl4.SelectedIndexChanged   += comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringJustify0.SelectedIndexChanged       += comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify1.SelectedIndexChanged       += comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify2.SelectedIndexChanged       += comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify3.SelectedIndexChanged       += comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify4.SelectedIndexChanged       += comboMapStringJustify_SelectedIndexChanged;
      checkMapStringClearAtEnd.CheckedChanged           += checkMapStringClearAtEnd_CheckedChanged;
      editMapStringID.ValueChanged                      += editMapStringID_ValueChanged;
    }



    private void DetachMapStringEditFieldHandlers()
    {
      editMapStringLabel.TextChanged                    -= editMapStringLabel_TextChanged;
      editMapStringLine0.TextChanged                    -= editMapStringLine_TextChanged;
      editMapStringLine1.TextChanged                    -= editMapStringLine_TextChanged;
      editMapStringLine2.TextChanged                    -= editMapStringLine_TextChanged;
      editMapStringLine3.TextChanged                    -= editMapStringLine_TextChanged;
      editMapStringLine4.TextChanged                    -= editMapStringLine_TextChanged;
      comboMapStringTerminator0.SelectedIndexChanged    -= comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator1.SelectedIndexChanged    -= comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator2.SelectedIndexChanged    -= comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator3.SelectedIndexChanged    -= comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringTerminator4.SelectedIndexChanged    -= comboMapStringTerminator_SelectedIndexChanged;
      comboMapStringLineControl0.SelectedIndexChanged   -= comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl1.SelectedIndexChanged   -= comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl2.SelectedIndexChanged   -= comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl3.SelectedIndexChanged   -= comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringLineControl4.SelectedIndexChanged   -= comboMapStringLineControl_SelectedIndexChanged;
      comboMapStringJustify0.SelectedIndexChanged       -= comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify1.SelectedIndexChanged       -= comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify2.SelectedIndexChanged       -= comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify3.SelectedIndexChanged       -= comboMapStringJustify_SelectedIndexChanged;
      comboMapStringJustify4.SelectedIndexChanged       -= comboMapStringJustify_SelectedIndexChanged;
      checkMapStringClearAtEnd.CheckedChanged           -= checkMapStringClearAtEnd_CheckedChanged;
      editMapStringID.ValueChanged                      -= editMapStringID_ValueChanged;
    }



    /// <summary>
    /// Format a MapString listbox entry as "NN: Label" with the index
    /// zero-padded to two digits. Used by both <see cref="RefreshMapStrings"/>
    /// (full rebuild) and the Update handler (single-entry edit) so the
    /// display style stays consistent.
    /// </summary>
    private static string FormatMapStringListEntry( int Index, Formats.MapProject.MapString Ms )
    {
      string label = string.IsNullOrEmpty( Ms.Label ) ? "(no label)" : Ms.Label;
      return Index.ToString( "D2" ) + ": " + label;
    }



    /// <summary>
    /// Refresh the listbox + per-string field pane after any structural
    /// change (add / delete / reorder / undo / project load). Preserves
    /// the previously-selected index when possible.
    /// </summary>
    public void RefreshMapStrings()
    {
      if ( listMapStrings == null ) return;
      if ( m_MapProject == null )
      {
        listMapStrings.Items.Clear();
        UpdateMapStringButtonStates();
        return;
      }

      int prevIndex = listMapStrings.SelectedIndex;

      // Detach the listbox handler — clearing/repopulating fires
      // SelectedIndexChanged with index -1 mid-rebuild and would
      // wipe the field pane to defaults.
      listMapStrings.SelectedIndexChanged -= listMapStrings_SelectedIndexChanged;
      try
      {
        listMapStrings.BeginUpdate();
        listMapStrings.Items.Clear();
        for ( int i = 0; i < m_MapProject.MapStrings.Count; ++i )
        {
          listMapStrings.Items.Add( FormatMapStringListEntry( i, m_MapProject.MapStrings[i] ) );
        }
        listMapStrings.EndUpdate();

        if ( prevIndex >= 0 && prevIndex < listMapStrings.Items.Count )
        {
          listMapStrings.SelectedIndex = prevIndex;
        }
        else if ( listMapStrings.Items.Count > 0 )
        {
          listMapStrings.SelectedIndex = 0;
        }
      }
      finally
      {
        listMapStrings.SelectedIndexChanged += listMapStrings_SelectedIndexChanged;
      }

      PopulateMapStringFieldsFromSelection();
      UpdateMapStringButtonStates();
      RebuildMapStringPreview();
    }



    private void listMapStrings_SelectedIndexChanged( object sender, EventArgs e )
    {
      PopulateMapStringFieldsFromSelection();
      UpdateMapStringButtonStates();
      RebuildMapStringPreview();
    }



    /// <summary>
    /// Mirror the selected MapString's data into the field pane.
    /// Detaches the live-edit handlers for the duration so writing into
    /// the controls doesn't dirty the model or push spurious undo entries.
    /// </summary>
    private void PopulateMapStringFieldsFromSelection()
    {
      var ms = GetSelectedMapString();
      DetachMapStringEditFieldHandlers();
      try
      {
        if ( ms == null )
        {
          editMapStringLabel.Text = "";
          editMapStringLine0.Text = "";
          editMapStringLine1.Text = "";
          editMapStringLine2.Text = "";
          editMapStringLine3.Text = "";
          editMapStringLine4.Text = "";
          // Combos default to "None" (index 0).
          comboMapStringTerminator0.SelectedIndex = 0;
          comboMapStringTerminator1.SelectedIndex = 0;
          comboMapStringTerminator2.SelectedIndex = 0;
          comboMapStringTerminator3.SelectedIndex = 0;
          comboMapStringTerminator4.SelectedIndex = 0;
          comboMapStringLineControl0.SelectedIndex = 0;
          comboMapStringLineControl1.SelectedIndex = 0;
          comboMapStringLineControl2.SelectedIndex = 0;
          comboMapStringLineControl3.SelectedIndex = 0;
          comboMapStringLineControl4.SelectedIndex = 0;
          comboMapStringJustify0.SelectedIndex = 0;
          comboMapStringJustify1.SelectedIndex = 0;
          comboMapStringJustify2.SelectedIndex = 0;
          comboMapStringJustify3.SelectedIndex = 0;
          comboMapStringJustify4.SelectedIndex = 0;
          checkMapStringClearAtEnd.Checked = false;
          editMapStringID.Value = 0;
          return;
        }

        editMapStringLabel.Text = ms.Label ?? "";
        var lineBoxes = new System.Windows.Forms.TextBox[]
        {
          editMapStringLine0, editMapStringLine1, editMapStringLine2, editMapStringLine3, editMapStringLine4
        };
        var termCombos = new System.Windows.Forms.ComboBox[]
        {
          comboMapStringTerminator0, comboMapStringTerminator1, comboMapStringTerminator2, comboMapStringTerminator3, comboMapStringTerminator4
        };
        var ctrlCombos = new System.Windows.Forms.ComboBox[]
        {
          comboMapStringLineControl0, comboMapStringLineControl1, comboMapStringLineControl2, comboMapStringLineControl3, comboMapStringLineControl4
        };
        var justifyCombos = new System.Windows.Forms.ComboBox[]
        {
          comboMapStringJustify0, comboMapStringJustify1, comboMapStringJustify2, comboMapStringJustify3, comboMapStringJustify4
        };
        for ( int i = 0; i < 5; ++i )
        {
          lineBoxes[i].Text = ms.Lines[i].Text ?? "";

          // Terminator combo: 0 = None, 1 = END_OF_LINE, 2 = PRESS_FIRE.
          if ( ms.Lines[i].Terminator == Formats.MapProject.MAP_STRING_END_OF_LINE )
            termCombos[i].SelectedIndex = 1;
          else if ( ms.Lines[i].Terminator == Formats.MapProject.MAP_STRING_PRESS_FIRE )
            termCombos[i].SelectedIndex = 2;
          else
            termCombos[i].SelectedIndex = 0;   // None / unknown

          // Control-code combo: 0 = None, 1..16 = colors $00..$0F. The
          // model stores the byte value directly, with $FF = NoControl.
          int cc = ms.Lines[i].ControlCode;
          if ( cc == Formats.MapProject.MAP_STRING_NO_CONTROL_CODE )
            ctrlCombos[i].SelectedIndex = 0;
          else if ( cc >= 0 && cc <= 15 )
            ctrlCombos[i].SelectedIndex = cc + 1;
          else
            ctrlCombos[i].SelectedIndex = 0;   // reserved range / unknown — show as None

          // Justify combo: 0 = Left, 1 = Center, 2 = Right.
          int j = ms.Lines[i].Justification;
          if ( j < 0 || j > 2 ) j = 0;
          justifyCombos[i].SelectedIndex = j;
        }
        checkMapStringClearAtEnd.Checked = ms.ClearTextAreaAtEnd;
        editMapStringID.Value = ms.StringID;
      }
      finally
      {
        AttachMapStringEditFieldHandlers();
      }
    }



    private Formats.MapProject.MapString GetSelectedMapString()
    {
      if ( m_MapProject == null ) return null;
      int idx = listMapStrings.SelectedIndex;
      if ( idx < 0 || idx >= m_MapProject.MapStrings.Count ) return null;
      return m_MapProject.MapStrings[idx];
    }



    private void UpdateMapStringButtonStates()
    {
      bool hasSelection = ( listMapStrings.SelectedIndex >= 0 )
                       && ( m_MapProject != null )
                       && ( listMapStrings.SelectedIndex < m_MapProject.MapStrings.Count );
      btnDeleteMapString.Enabled    = hasSelection;
      btnDuplicateMapString.Enabled = hasSelection;
      btnMoveMapStringUp.Enabled    = hasSelection && ( listMapStrings.SelectedIndex > 0 );
      btnMoveMapStringDown.Enabled  = hasSelection
                                   && ( listMapStrings.SelectedIndex < m_MapProject.MapStrings.Count - 1 );
    }



    // -------- Live edit handlers --------
    //
    // Per-string fields commit to the model immediately. PopulateMapString
    // FieldsFromSelection() detaches and reattaches all of these around
    // its writes so the listbox-driven populate doesn't mutate the model
    // it's reading from — that's the project's mandated detach-vs-flag
    // pattern. Each handler pushes an undo snapshot before its edit so
    // Ctrl+Z reverses one user action.

    private void editMapStringLabel_TextChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );

      ms.Label = editMapStringLabel.Text ?? "";
      SetModified();

      // Reflect the (possibly new) Label in the listbox in place. Detach
      // the listbox handler so rewriting Items[idx] doesn't re-enter
      // PopulateMapStringFieldsFromSelection mid-typing and reset the
      // textbox caret.
      int idx = listMapStrings.SelectedIndex;
      if ( idx >= 0 && idx < listMapStrings.Items.Count )
      {
        listMapStrings.SelectedIndexChanged -= listMapStrings_SelectedIndexChanged;
        try
        {
          listMapStrings.Items[idx] = FormatMapStringListEntry( idx, ms );
        }
        finally
        {
          listMapStrings.SelectedIndexChanged += listMapStrings_SelectedIndexChanged;
        }
      }
    }



    private void editMapStringLine_TextChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      int lineIdx = MapStringLineIndexOf( sender );
      if ( lineIdx < 0 ) return;

      var box = (System.Windows.Forms.TextBox)sender;

      // The line textboxes have no MaxLength, so a paste of any size lands
      // here intact. Keep only the maximum that fits the text area
      // (MapStringsTextAreaWidth) — trim the overflow so the line never
      // exceeds the display width. Detach this handler during the trim so
      // resetting .Text doesn't re-enter (which would double the undo entry).
      int maxChars = ( m_MapProject != null ) ? m_MapProject.MapStringsTextAreaWidth : 40;
      if ( maxChars < 1 ) maxChars = 1;
      string text = box.Text ?? "";
      if ( text.Length > maxChars )
      {
        int caret = box.SelectionStart;
        box.TextChanged -= editMapStringLine_TextChanged;
        box.Text = text.Substring( 0, maxChars );
        box.SelectionStart = Math.Min( caret, box.Text.Length );
        box.TextChanged += editMapStringLine_TextChanged;
        text = box.Text;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );

      ms.Lines[lineIdx].Text = text;
      SetModified();
      RebuildMapStringPreview();
    }



    /// <summary>
    /// Map Strings scratch text changed — mirror it into the project so it
    /// persists. Pure authoring aid; not part of any string or export, so no
    /// undo entry and no preview rebuild.
    /// </summary>
    private void editMapStringScratch_TextChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      if ( m_MapProject.MapStringsScratchText == editMapStringScratch.Text ) return;
      m_MapProject.MapStringsScratchText = editMapStringScratch.Text;
      SetModified();
    }



    private void comboMapStringTerminator_SelectedIndexChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      int lineIdx = MapStringTerminatorIndexOf( sender );
      if ( lineIdx < 0 ) return;
      var combo = (System.Windows.Forms.ComboBox)sender;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      switch ( combo.SelectedIndex )
      {
        case 1:  ms.Lines[lineIdx].Terminator = Formats.MapProject.MAP_STRING_END_OF_LINE; break;
        case 2:  ms.Lines[lineIdx].Terminator = Formats.MapProject.MAP_STRING_PRESS_FIRE;  break;
        default: ms.Lines[lineIdx].Terminator = Formats.MapProject.MAP_STRING_NO_TERMINATOR; break;
      }
      SetModified();
      // Terminator is a runtime control byte; no visual impact in the
      // static preview, so no rebuild call.
    }



    private void comboMapStringLineControl_SelectedIndexChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      int lineIdx = MapStringLineControlIndexOf( sender );
      if ( lineIdx < 0 ) return;
      var combo = (System.Windows.Forms.ComboBox)sender;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      // Combo: index 0 = "None", 1..16 = colors $00..$0F.
      int cc = combo.SelectedIndex;
      if ( cc <= 0 )
      {
        ms.Lines[lineIdx].ControlCode = Formats.MapProject.MAP_STRING_NO_CONTROL_CODE;
      }
      else
      {
        ms.Lines[lineIdx].ControlCode = (byte)( cc - 1 );
      }
      SetModified();
      RebuildMapStringPreview();
    }



    private void checkMapStringClearAtEnd_CheckedChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      ms.ClearTextAreaAtEnd = checkMapStringClearAtEnd.Checked;
      SetModified();
      // CLEAR_TEXT_AREA is a runtime tail byte; no visual impact in the
      // static preview.
    }



    private void editMapStringID_ValueChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      ms.StringID = (byte)editMapStringID.Value;
      SetModified();
      // StringID is exported metadata only; no visual impact in the
      // static preview.
    }



    private void comboMapStringJustify_SelectedIndexChanged( object sender, EventArgs e )
    {
      var ms = GetSelectedMapString();
      if ( ms == null ) return;
      int lineIdx = MapStringJustifyIndexOf( sender );
      if ( lineIdx < 0 ) return;
      var combo = (System.Windows.Forms.ComboBox)sender;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      switch ( combo.SelectedIndex )
      {
        case 1:  ms.Lines[lineIdx].Justification = Formats.MapProject.MAP_STRING_JUSTIFY_CENTER; break;
        case 2:  ms.Lines[lineIdx].Justification = Formats.MapProject.MAP_STRING_JUSTIFY_RIGHT;  break;
        default: ms.Lines[lineIdx].Justification = Formats.MapProject.MAP_STRING_JUSTIFY_LEFT;   break;
      }
      SetModified();
      RebuildMapStringPreview();
    }



    private void editMapStringTextAreaWidth_ValueChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      int newWidth = (int)editMapStringTextAreaWidth.Value;
      if ( m_MapProject.MapStringsTextAreaWidth == newWidth ) return;
      m_MapProject.MapStringsTextAreaWidth = newWidth;
      SetModified();
      RebuildMapStringPreview();
    }



    private int MapStringLineIndexOf( object sender )
    {
      if ( sender == editMapStringLine0 ) return 0;
      if ( sender == editMapStringLine1 ) return 1;
      if ( sender == editMapStringLine2 ) return 2;
      if ( sender == editMapStringLine3 ) return 3;
      if ( sender == editMapStringLine4 ) return 4;
      return -1;
    }



    private int MapStringTerminatorIndexOf( object sender )
    {
      if ( sender == comboMapStringTerminator0 ) return 0;
      if ( sender == comboMapStringTerminator1 ) return 1;
      if ( sender == comboMapStringTerminator2 ) return 2;
      if ( sender == comboMapStringTerminator3 ) return 3;
      if ( sender == comboMapStringTerminator4 ) return 4;
      return -1;
    }



    private int MapStringLineControlIndexOf( object sender )
    {
      if ( sender == comboMapStringLineControl0 ) return 0;
      if ( sender == comboMapStringLineControl1 ) return 1;
      if ( sender == comboMapStringLineControl2 ) return 2;
      if ( sender == comboMapStringLineControl3 ) return 3;
      if ( sender == comboMapStringLineControl4 ) return 4;
      return -1;
    }



    private int MapStringJustifyIndexOf( object sender )
    {
      if ( sender == comboMapStringJustify0 ) return 0;
      if ( sender == comboMapStringJustify1 ) return 1;
      if ( sender == comboMapStringJustify2 ) return 2;
      if ( sender == comboMapStringJustify3 ) return 3;
      if ( sender == comboMapStringJustify4 ) return 4;
      return -1;
    }



    // -------- Action buttons --------

    /// <summary>
    /// True — and shows a warning — when the project already holds the
    /// maximum of 255 map strings. The exported game binary stores the
    /// map-string count in a single byte, so a 256th cannot be
    /// represented. Add / Duplicate handlers bail out when this is true.
    /// </summary>
    private bool MapStringLimitReached()
    {
      if ( ( m_MapProject == null )
      ||   ( m_MapProject.MapStrings.Count < 255 ) )
      {
        return false;
      }
      System.Windows.Forms.MessageBox.Show(
        this,
        "A map can hold at most 255 map strings — the exported game binary stores the map-string count in a single byte, so a 256th can't be represented.\r\n\r\nDelete an existing map string before adding another.",
        "Map string limit reached",
        System.Windows.Forms.MessageBoxButtons.OK,
        System.Windows.Forms.MessageBoxIcon.Warning );
      return true;
    }



    private void btnAddMapString_Click( DecentForms.ControlBase Sender )
    {
      if ( m_MapProject == null ) return;
      if ( MapStringLimitReached() ) return;
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );

      var ms = new Formats.MapProject.MapString();
      // Auto-label MSG_<n>, bumping until unique.
      var existing = new HashSet<string>( StringComparer.Ordinal );
      foreach ( var x in m_MapProject.MapStrings )
      {
        if ( !string.IsNullOrEmpty( x.Label ) ) existing.Add( x.Label );
      }
      int n = m_MapProject.MapStrings.Count + 1;
      string candidate;
      do
      {
        candidate = "MSG_" + n;
        ++n;
      }
      while ( existing.Contains( candidate ) );
      ms.Label = candidate;

      // Assign the lowest String ID (0..255) not already used by an
      // existing map string. With 256+ strings every value is taken —
      // fall back to the default of 0.
      var usedIDs = new HashSet<byte>();
      foreach ( var x in m_MapProject.MapStrings )
      {
        usedIDs.Add( x.StringID );
      }
      for ( int id = 0; id <= 255; ++id )
      {
        if ( !usedIDs.Contains( (byte)id ) )
        {
          ms.StringID = (byte)id;
          break;
        }
      }

      m_MapProject.MapStrings.Add( ms );
      RefreshMapStrings();
      listMapStrings.SelectedIndex = m_MapProject.MapStrings.Count - 1;
      SetModified();
      editMapStringLabel.Focus();
      editMapStringLabel.SelectAll();
    }



    private void btnDeleteMapString_Click( DecentForms.ControlBase Sender )
    {
      if ( m_MapProject == null ) return;
      int idx = listMapStrings.SelectedIndex;
      if ( idx < 0 || idx >= m_MapProject.MapStrings.Count ) return;

      string label = m_MapProject.MapStrings[idx].Label;
      string shown = string.IsNullOrEmpty( label ) ? "(no label)" : label;
      var confirm = System.Windows.Forms.MessageBox.Show(
        this,
        "Delete map string '" + shown + "'?\r\n\r\nThis can be undone.",
        "Confirm delete",
        System.Windows.Forms.MessageBoxButtons.OKCancel,
        System.Windows.Forms.MessageBoxIcon.Warning,
        System.Windows.Forms.MessageBoxDefaultButton.Button2 );
      if ( confirm != System.Windows.Forms.DialogResult.OK ) return;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      m_MapProject.MapStrings.RemoveAt( idx );
      RefreshMapStrings();
      // Restore selection close to where the user was (clamped).
      if ( m_MapProject.MapStrings.Count > 0 )
      {
        listMapStrings.SelectedIndex = Math.Min( idx, m_MapProject.MapStrings.Count - 1 );
      }
      SetModified();
    }



    private void btnMoveMapStringUp_Click( DecentForms.ControlBase Sender )
    {
      if ( m_MapProject == null ) return;
      int idx = listMapStrings.SelectedIndex;
      if ( idx <= 0 || idx >= m_MapProject.MapStrings.Count ) return;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      var ms = m_MapProject.MapStrings[idx];
      m_MapProject.MapStrings.RemoveAt( idx );
      m_MapProject.MapStrings.Insert( idx - 1, ms );
      RefreshMapStrings();
      listMapStrings.SelectedIndex = idx - 1;
      SetModified();
    }



    private void btnMoveMapStringDown_Click( DecentForms.ControlBase Sender )
    {
      if ( m_MapProject == null ) return;
      int idx = listMapStrings.SelectedIndex;
      if ( idx < 0 || idx >= m_MapProject.MapStrings.Count - 1 ) return;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      var ms = m_MapProject.MapStrings[idx];
      m_MapProject.MapStrings.RemoveAt( idx );
      m_MapProject.MapStrings.Insert( idx + 1, ms );
      RefreshMapStrings();
      listMapStrings.SelectedIndex = idx + 1;
      SetModified();
    }



    private void btnDuplicateMapString_Click( DecentForms.ControlBase Sender )
    {
      if ( m_MapProject == null ) return;
      var src = GetSelectedMapString();
      if ( src == null ) return;
      if ( MapStringLimitReached() ) return;

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapStringsChange( this, m_MapProject ) );
      var copy = new Formats.MapProject.MapString
      {
        Label              = MakeUniqueMapStringLabel( ( src.Label ?? "" ) + "_COPY" ),
        ClearTextAreaAtEnd = src.ClearTextAreaAtEnd,
        StringID           = src.StringID
      };
      for ( int i = 0; i < 5; ++i )
      {
        copy.Lines[i] = new Formats.MapProject.MapStringLine
        {
          Text          = src.Lines[i].Text,
          Terminator    = src.Lines[i].Terminator,
          ControlCode   = src.Lines[i].ControlCode,
          Justification = src.Lines[i].Justification
        };
      }
      m_MapProject.MapStrings.Add( copy );
      RefreshMapStrings();
      listMapStrings.SelectedIndex = m_MapProject.MapStrings.Count - 1;
      SetModified();
    }



    private string MakeUniqueMapStringLabel( string Base )
    {
      var existing = new HashSet<string>( StringComparer.Ordinal );
      foreach ( var x in m_MapProject.MapStrings )
      {
        if ( !string.IsNullOrEmpty( x.Label ) ) existing.Add( x.Label );
      }
      if ( !existing.Contains( Base ) ) return Base;
      int n = 2;
      string candidate;
      do
      {
        candidate = Base + "_" + n;
        ++n;
      }
      while ( existing.Contains( candidate ) );
      return candidate;
    }



    // -------- Live preview --------

    /// <summary>
    /// Render the currently-selected MapString into picMapStringPreview.
    /// Best-effort static rendering: text and color tokens only — runtime
    /// PRESS_FIRE / CLEAR_TEXT_AREA semantics are not simulated. Glyphs
    /// come from the project's charset via the C64 screen-code mapping.
    /// </summary>
    private void RebuildMapStringPreview()
    {
      if ( picMapStringPreview == null ) return;

      const int CharsPerLine  = 40;
      const int LineCount     = 5;
      const int CellW         = 8;
      const int CellH         = 8;
      // Padding around the rendered text. Reads as visual breathing room
      // once the bitmap is stretched into the picture box. Glyphs stay
      // pixel-perfect at 8×8 and the padding scales linearly with them.
      const int PaddingLeft   = 8;
      const int PaddingRight  = 8;
      const int PaddingTop    = 8;
      const int PaddingBottom = 8;
      const int LineGap       = 2;

      int bmpW = PaddingLeft + CharsPerLine * CellW + PaddingRight;
      int bmpH = PaddingTop + LineCount * CellH + ( LineCount - 1 ) * LineGap + PaddingBottom;

      var bmp = new System.Drawing.Bitmap( bmpW, bmpH );
      using ( var g = System.Drawing.Graphics.FromImage( bmp ) )
      {
        g.Clear( System.Drawing.Color.Black );

        if ( m_MapProject == null || listMapStrings.SelectedIndex < 0 )
        {
          var prev = picMapStringPreview.Image;
          picMapStringPreview.Image = bmp;
          if ( prev != null ) prev.Dispose();
          return;
        }

        // The preview reads from the form controls, not the model — that
        // way "what you see" reflects pending edits before the user clicks
        // Update. Same render rules as the runtime: each line has a leading
        // ControlCode that sets the line's color; if the line has None, it
        // carries forward the previous line's color (matches what the
        // hardware does — color register isn't reset between lines).
        var palette = ConstantData.Palette;
        int lowerStart    = m_MapProject.MapStringsLowercaseIndex;
        int upperStart    = m_MapProject.MapStringsUppercaseIndex;
        int numbersStart  = m_MapProject.MapStringsNumbersIndex;
        int textAreaWidth = m_MapProject.MapStringsTextAreaWidth;
        if ( textAreaWidth < 1 ) textAreaWidth = 1;

        var lineBoxes = new System.Windows.Forms.TextBox[]
        {
          editMapStringLine0, editMapStringLine1, editMapStringLine2, editMapStringLine3, editMapStringLine4
        };
        var ctrlCombos = new System.Windows.Forms.ComboBox[]
        {
          comboMapStringLineControl0, comboMapStringLineControl1, comboMapStringLineControl2, comboMapStringLineControl3, comboMapStringLineControl4
        };
        var justifyCombos = new System.Windows.Forms.ComboBox[]
        {
          comboMapStringJustify0, comboMapStringJustify1, comboMapStringJustify2, comboMapStringJustify3, comboMapStringJustify4
        };

        int currentColor = 1;   // Default to white until the first ControlCode is set.
        for ( int li = 0; li < LineCount; ++li )
        {
          // Pull color from the form's combo. Index 0 = None, 1..16 = $00..$0F.
          int ccIdx = ctrlCombos[li].SelectedIndex;
          if ( ccIdx >= 1 && ccIdx <= 16 )
          {
            currentColor = ccIdx - 1;
          }

          string text = lineBoxes[li].Text ?? "";
          if ( text.Length == 0 ) continue;

          // Mirror the export padding so the preview matches the bytes the
          // game runtime will emit. See MapProject.BuildMapStringByteStream.
          int leadingPad = 0;
          int slack = textAreaWidth - text.Length;
          if ( slack > 0 )
          {
            int jIdx = justifyCombos[li].SelectedIndex;
            if ( jIdx == 1 )       leadingPad = slack / 2;     // Center
            else if ( jIdx == 2 )  leadingPad = slack;         // Right
          }

          int rowY = PaddingTop + li * ( CellH + LineGap );
          int col = leadingPad;
          for ( int p = 0; p < text.Length && col < CharsPerLine; ++p )
          {
            int screenCode = AsciiToScreenCode( text[p], lowerStart, upperStart, numbersStart );
            if ( screenCode < 0 ) continue;
            int colX = PaddingLeft + col * CellW;
            DrawMapStringPreviewGlyphAt( g, (byte)screenCode, colX, rowY, currentColor, palette );
            ++col;
          }
        }
      }

      var prevImg = picMapStringPreview.Image;
      picMapStringPreview.Image = bmp;
      if ( prevImg != null ) prevImg.Dispose();
    }



    private void DrawMapStringPreviewGlyphAt( System.Drawing.Graphics G, byte ScreenCode, int BaseX, int BaseY, int ColorIdx, Palette Pal )
    {
      int screenCode = ScreenCode;

      // Find the project's charset bytes. Fall back to a hardcoded glyph
      // if the project has none yet — at least the preview shows columns.
      byte[] glyph = GetMapStringPreviewGlyph( screenCode );
      if ( glyph == null ) return;

      System.Drawing.Color fg = ( ColorIdx >= 0 && ColorIdx < 16 )
                                ? Pal.Colors[ColorIdx]
                                : System.Drawing.Color.White;

      int baseX = BaseX;
      int baseY = BaseY;
      for ( int y = 0; y < 8; ++y )
      {
        byte b = glyph[y];
        for ( int x = 0; x < 8; ++x )
        {
          if ( ( b & ( 0x80 >> x ) ) != 0 )
          {
            G.FillRectangle( new System.Drawing.SolidBrush( fg ), baseX + x, baseY + y, 1, 1 );
          }
        }
      }
    }



    /// <summary>
    /// Convert an authored char to a charset index for the preview canvas.
    /// Letters and digits use the per-project user-supplied offsets
    /// (lowercase / uppercase / numbers) — so a custom charset that puts
    /// 'a' at index 65 gets the right glyph. Punctuation and the
    /// space / @ / [ / ] characters keep their fixed C64 screen-code
    /// mapping ("normal C64 characters" — per the user's spec). Returns
    /// -1 for unsupported chars (preview just skips them).
    /// </summary>
    private static int AsciiToScreenCode( char Ch, int LowerStart, int UpperStart, int NumbersStart )
    {
      if ( Ch >= 'A' && Ch <= 'Z' ) return Ch - 'A' + UpperStart;
      if ( Ch >= 'a' && Ch <= 'z' ) return Ch - 'a' + LowerStart;
      if ( Ch >= '0' && Ch <= '9' ) return Ch - '0' + NumbersStart;
      if ( Ch == ' ' )  return 0x20;
      if ( Ch == '!' )  return 0x21;
      if ( Ch == '"' )  return 0x22;
      if ( Ch == '#' )  return 0x23;
      if ( Ch == '$' )  return 0x24;
      if ( Ch == '%' )  return 0x25;
      if ( Ch == '&' )  return 0x26;
      if ( Ch == '\'' ) return 0x27;
      if ( Ch == '(' )  return 0x28;
      if ( Ch == ')' )  return 0x29;
      if ( Ch == '*' )  return 0x2A;
      if ( Ch == '+' )  return 0x2B;
      if ( Ch == ',' )  return 0x2C;
      if ( Ch == '-' )  return 0x2D;
      if ( Ch == '.' )  return 0x2E;
      if ( Ch == '/' )  return 0x2F;
      if ( Ch == ':' )  return 0x3A;
      if ( Ch == ';' )  return 0x3B;
      if ( Ch == '<' )  return 0x3C;
      if ( Ch == '=' )  return 0x3D;
      if ( Ch == '>' )  return 0x3E;
      if ( Ch == '?' )  return 0x3F;
      if ( Ch == '@' )  return 0x00;
      if ( Ch == '[' )  return 0x1B;
      if ( Ch == ']' )  return 0x1D;
      return -1;
    }



    /// <summary>
    /// Look up an 8-byte glyph for the given screen code. Prefers the
    /// user-selected preview font (if loaded) since that's what will
    /// actually render in-game; otherwise falls back to the project's
    /// main charset; otherwise null (caller skips drawing).
    /// </summary>
    private byte[] GetMapStringPreviewGlyph( int ScreenCode )
    {
      // 1. Preview font — user-selected binary file (header stripped on load).
      if ( m_MapStringPreviewFont != null )
      {
        int offset = ScreenCode * 8;
        if ( offset >= 0 && offset + 8 <= m_MapStringPreviewFont.Length )
        {
          var glyph = new byte[8];
          Array.Copy( m_MapStringPreviewFont, offset, glyph, 0, 8 );
          return glyph;
        }
        // Screen code out of range for the loaded font — fall through to
        // the project charset rather than returning null, so a glyph still
        // appears if the project's charset is broader.
      }

      // 2. Project charset fallback.
      if ( m_MapProject == null ) return null;
      if ( m_MapProject.Charset == null ) return null;
      if ( m_MapProject.Charset.Characters == null ) return null;
      if ( ScreenCode < 0 || ScreenCode >= m_MapProject.Charset.Characters.Count ) return null;
      var ch = m_MapProject.Charset.Characters[ScreenCode];
      if ( ch == null || ch.Tile == null ) return null;
      var data = ch.Tile.Data;
      if ( data == null || data.Length < 8 ) return null;
      var glyphFromCharset = new byte[8];
      for ( int i = 0; i < 8; ++i )
      {
        glyphFromCharset[i] = data.ByteAt( i );
      }
      return glyphFromCharset;
    }



    private void RefreshMarkerTypes()
    {
      if ( listMarkerTypes == null ) return;
      
      listMarkerTypes.Items.Clear();
      comboMarkerTypes.Items.Clear();
      comboMarkerTypes.Items.Add( "None" );
      foreach ( var type in m_MapProject.MarkerTypes )
      {
         listMarkerTypes.Items.Add( type.Name );
         comboMarkerTypes.Items.Add( type.Name );
      }
      if ( m_CurrentMap != null )
      {
         // Find corresponding index for selected type
         int index = m_MapProject.MarkerTypes.FindIndex( t => t.ID == m_CurrentMap.SelectedMarkerType );
         if ( index != -1 )
         {
           comboMarkerTypes.SelectedIndex = index + 1;
         }
         else
         {
           comboMarkerTypes.SelectedIndex = 0;
         }
      }
      else
      {
         comboMarkerTypes.SelectedIndex = 0;
      }
    }

    private void btnAddMarkerType_Click( DecentForms.ControlBase Sender )
    {
      string name = editMarkerName.Text;
      if ( string.IsNullOrEmpty( name ) )
      {
         name = "Marker " + ( m_MapProject.MarkerTypes.Count + 1 );
      }
      
      var newType = new MapProject.MarkerType();
      newType.Name = name;
      newType.Color = comboMarkerColor.SelectedIndex;
      newType.ExportSymbol = editMarkerExportSymbol.Text ?? "";
      newType.TagID = (int)editMarkerTagID.Value;
      newType.Description = editMarkerDescription.Text ?? "";
      newType.ID = 0;
      if ( m_MapProject.MarkerTypes.Count > 0 )
      {
        newType.ID = m_MapProject.MarkerTypes.Max( t => t.ID ) + 1;
      }
      m_MapProject.MarkerTypes.Add( newType );
      RefreshMarkerTypes();
      
      listMarkerTypes.SelectedIndex = listMarkerTypes.Items.Count - 1;
      SetModified();
    }

    private void btnUpdateMarkerType_Click( DecentForms.ControlBase Sender )
    {
       if ( listMarkerTypes.SelectedIndex == -1 ) return;
       if ( ( listMarkerTypes.SelectedIndex < 0 )
       ||   ( listMarkerTypes.SelectedIndex >= m_MapProject.MarkerTypes.Count ) )
       {
         return;
       }

       var type = m_MapProject.MarkerTypes[listMarkerTypes.SelectedIndex];
       int newTagID = (int)editMarkerTagID.Value;

       // Warn if another marker type already uses this TagID — TagID is the
       // runtime identifier for every marker instance.
       var duplicate = m_MapProject.MarkerTypes.FirstOrDefault(
         t => ( t != type ) && ( t.TagID == newTagID ) );
       if ( duplicate != null )
       {
         var result = System.Windows.Forms.MessageBox.Show(
           "Tag ID " + newTagID + " is already used by marker type '" + duplicate.Name + "'.\r\n\r\n"
           + "Runtime code distinguishes marker types by TagID, so duplicates will collide.\r\n\r\n"
           + "Save anyway?",
           "Duplicate Tag ID",
           System.Windows.Forms.MessageBoxButtons.YesNo,
           System.Windows.Forms.MessageBoxIcon.Warning );
         if ( result != System.Windows.Forms.DialogResult.Yes )
         {
           return;
         }
       }

       int savedSelection = listMarkerTypes.SelectedIndex;

       type.Name = editMarkerName.Text;
       type.Color = comboMarkerColor.SelectedIndex;
       type.ExportSymbol = editMarkerExportSymbol.Text ?? "";
       type.TagID = newTagID;
       type.Description = editMarkerDescription.Text ?? "";

       RefreshMarkerTypes();
       if ( ( savedSelection >= 0 )
       &&   ( savedSelection < listMarkerTypes.Items.Count ) )
       {
         listMarkerTypes.SelectedIndex = savedSelection;
       }
       SetModified();
    }

    private void btnDeleteMarkerType_Click( DecentForms.ControlBase Sender )
    {
       if ( listMarkerTypes.SelectedIndex == -1 ) return;
       if ( ( listMarkerTypes.SelectedIndex < 0 )
       ||   ( listMarkerTypes.SelectedIndex >= m_MapProject.MarkerTypes.Count ) )
       {
         return;
       }

       var type = m_MapProject.MarkerTypes[listMarkerTypes.SelectedIndex];

       // H3: confirm + cascade-delete any markers of this type across all maps.
       int instanceCount = 0;
       int mapsTouched = 0;
       foreach ( var m in m_MapProject.Maps )
       {
         int inThisMap = m.Markers.Count( mk => mk.Type == type.ID );
         if ( inThisMap > 0 )
         {
           instanceCount += inThisMap;
           ++mapsTouched;
         }
       }

       string message;
       if ( instanceCount == 0 )
       {
         message = "Are you sure you want to delete marker type '" + type.Name + "'?";
       }
       else
       {
         message = "Are you sure you want to delete marker type '" + type.Name + "'?\r\n\r\n"
                 + "This will also delete " + instanceCount + " marker"
                 + ( instanceCount == 1 ? "" : "s" )
                 + " of this type across " + mapsTouched + " map"
                 + ( mapsTouched == 1 ? "" : "s" ) + ".";
       }

       var confirm = System.Windows.Forms.MessageBox.Show(
         message,
         "Delete marker type",
         System.Windows.Forms.MessageBoxButtons.YesNo,
         System.Windows.Forms.MessageBoxIcon.Warning );
       if ( confirm != System.Windows.Forms.DialogResult.Yes )
       {
         return;
       }

       // Cascade-delete instances first, then the type itself.
       foreach ( var m in m_MapProject.Maps )
       {
         m.Markers.RemoveAll( mk => mk.Type == type.ID );
         if ( m.SelectedMarkerType == type.ID )
         {
           m.SelectedMarkerType = -1;
         }
       }
       m_MapProject.MarkerTypes.Remove( type );
       RefreshMarkerTypes();
       pictureEditor.Invalidate();
       RedrawMap();
       SetModified();
       UpdateMarkerOutOfBoundsLabel();
    }

    private void listMarkerTypes_SelectedIndexChanged( object sender, EventArgs e )
    {
       if ( ( listMarkerTypes.SelectedIndex < 0 )
       ||   ( listMarkerTypes.SelectedIndex >= m_MapProject.MarkerTypes.Count ) )
       {
         btnUpdateMarkerType.Enabled = false;
         btnDeleteMarkerType.Enabled = false;
         return;
       }
       btnUpdateMarkerType.Enabled = true;
       btnDeleteMarkerType.Enabled = true;

       var type = m_MapProject.MarkerTypes[listMarkerTypes.SelectedIndex];
       editMarkerName.Text = type.Name;
       comboMarkerColor.SelectedIndex = type.Color;
       editMarkerExportSymbol.Text = type.ExportSymbol ?? "";
       editMarkerDescription.Text = type.Description ?? "";
       // Clamp into the NumericUpDown's valid range — legacy projects may
       // have TagID=0 from before zero was reserved, and assigning a value
       // outside [Minimum..Maximum] throws ArgumentOutOfRangeException.
       int displayTagID = type.TagID;
       if ( displayTagID < (int)editMarkerTagID.Minimum ) displayTagID = (int)editMarkerTagID.Minimum;
       if ( displayTagID > (int)editMarkerTagID.Maximum ) displayTagID = (int)editMarkerTagID.Maximum;
       editMarkerTagID.Value = displayTagID;
    }

    private void editMarkerExportSymbol_KeyPress( object sender, KeyPressEventArgs e )
    {
       // Restrict ExportSymbol to assembler-safe characters only.
       if ( ( !char.IsLetterOrDigit( e.KeyChar ) )
       &&   ( e.KeyChar != '_' )
       &&   ( !char.IsControl( e.KeyChar ) ) )
       {
         e.Handled = true;
       }
    }


    private void btnToolMarker_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolMarker ) ) return;
      m_ToolMode = ToolMode.MARKER;
      UncheckOtherToolButtons( btnToolMarker );
      AfterToolChange();
    }

    private void comboMarkerTypes_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;

      int newSelectedMarkerType = -1;

      // M4: defend against the combo and MarkerTypes list being briefly out of sync
      // (e.g. right after a type is deleted or while Refresh repopulates).
      int markerTypeIdx = comboMarkerTypes.SelectedIndex - 1;
      if ( ( comboMarkerTypes.SelectedIndex > 0 )
      &&   ( markerTypeIdx >= 0 )
      &&   ( markerTypeIdx < m_MapProject.MarkerTypes.Count ) )
      {
         var type = m_MapProject.MarkerTypes[markerTypeIdx];
         newSelectedMarkerType = type.ID;

         if ( m_CurrentMap.SelectedMarkerType != newSelectedMarkerType )
         {
           m_CurrentMap.SelectedMarkerType = newSelectedMarkerType;
           comboMarkerColorOverride.SelectedIndex = type.Color;
           comboMarkerColorOverride.Enabled = btnToolMarker.Checked;
           SetModified();
         }
         else
         {
           // Just sync UI
           comboMarkerColorOverride.SelectedIndex = type.Color;
           comboMarkerColorOverride.Enabled = btnToolMarker.Checked;
         }

         // When a specific marker is selected, change the combo = retype
         // that marker. Guarded by m_PopulatingFromSelection so reading a
         // marker into the toolbar doesn't immediately rewrite its type.
         if ( ( !m_PopulatingFromSelection )
         &&   ( m_SelectedMarker != null )
         &&   ( m_SelectedMarker.Type != newSelectedMarkerType ) )
         {
           DocumentInfo.UndoManager.AddUndoTask(
             new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
           m_SelectedMarker.Type = newSelectedMarkerType;
           pictureEditor.Invalidate();
         }
      }
      else
      {
         if ( m_CurrentMap.SelectedMarkerType != -1 )
         {
           m_CurrentMap.SelectedMarkerType = -1;
           comboMarkerColorOverride.Enabled = false;
           SetModified();
         }
      }
    }

    private void editMarkerValue_ValueChanged( object sender, EventArgs e )
    {
      // When a marker is selected (right-clicked), treat these controls as a
      // live property editor for that marker. When nothing's selected, they
      // behave as the "next placement" defaults — the left-click handler
      // reads them at placement time, so we don't need to do anything here.
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;

      byte newV1 = (byte)editMarkerValue1.Value;
      byte newV2 = (byte)editMarkerValue2.Value;
      if ( ( m_SelectedMarker.Value1 == newV1 )
      &&   ( m_SelectedMarker.Value2 == newV2 ) )
      {
        return;
      }
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.Value1 = newV1;
      m_SelectedMarker.Value2 = newV2;
      SetModified();
      pictureEditor.Invalidate();
    }



    /// <summary>
    /// Live edit / placement-default for the marker GroupId field.
    /// Same pattern as <see cref="editMarkerValue_ValueChanged"/>:
    /// when a marker is selected, mutate it directly (with undo); when
    /// nothing's selected, leave the editor value alone — the left-
    /// click handler reads it at placement time as the new marker's
    /// default GroupId.
    /// </summary>
    private void editMarkerGroupId_ValueChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;

      byte newG = (byte)editMarkerGroupId.Value;
      if ( m_SelectedMarker.GroupId == newG ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.GroupId = newG;
      SetModified();
      pictureEditor.Invalidate();
    }



    /// <summary>
    /// Live edit / placement-default for the marker trigger-chain link
    /// fields (Link to ID + Link ID). Same pattern as
    /// <see cref="editMarkerValue_ValueChanged"/>: when a marker is
    /// selected, mutate it directly (with undo); when nothing's
    /// selected, leave the editor values alone — the left-click handler
    /// reads them at placement time as the new marker's defaults. One
    /// shared handler covers both NumericUpDowns.
    /// </summary>
    private void editMarkerLink_ValueChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;

      byte newLinkToID = (byte)editMarkerLinkToID.Value;
      byte newLinkID   = (byte)editMarkerLinkID.Value;
      if ( ( m_SelectedMarker.LinkToID == newLinkToID )
      &&   ( m_SelectedMarker.LinkID == newLinkID ) )
      {
        return;
      }
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.LinkToID = newLinkToID;
      m_SelectedMarker.LinkID   = newLinkID;
      SetModified();
      pictureEditor.Invalidate();
    }



    private void checkMarkerDefaultEnabled_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;
      if ( m_SelectedMarker.Enabled == checkMarkerDefaultEnabled.Checked ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.Enabled = checkMarkerDefaultEnabled.Checked;
      SetModified();
      pictureEditor.Invalidate();
    }



    private void checkMarkerDefaultTriggered_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;
      if ( m_SelectedMarker.Triggered == checkMarkerDefaultTriggered.Checked ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.Triggered = checkMarkerDefaultTriggered.Checked;
      SetModified();
      pictureEditor.Invalidate();
    }



    private void checkMarkerAutoDisable_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedMarker == null ) return;
      if ( m_SelectedMarker.AutoDisableAfterTrigger == checkMarkerAutoDisable.Checked ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.AutoDisableAfterTrigger = checkMarkerAutoDisable.Checked;
      SetModified();
      pictureEditor.Invalidate();
    }



    private void editEntityValue_ValueChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedEntity == null ) return;

      byte newV1 = (byte)editEntityValue1Default.Value;
      byte newV2 = (byte)editEntityValue2Default.Value;
      if ( ( m_SelectedEntity.Value1 == newV1 )
      &&   ( m_SelectedEntity.Value2 == newV2 ) )
      {
        return;
      }
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
      m_SelectedEntity.Value1 = newV1;
      m_SelectedEntity.Value2 = newV2;
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void checkEntityDefaultEnabled_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedEntity == null ) return;
      if ( m_SelectedEntity.Enabled == checkEntityDefaultEnabled.Checked ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
      m_SelectedEntity.Enabled = checkEntityDefaultEnabled.Checked;
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void checkEntityDefaultTriggered_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingFromSelection ) return;
      if ( m_SelectedEntity == null ) return;
      if ( m_SelectedEntity.Triggered == checkEntityDefaultTriggered.Checked ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
      m_SelectedEntity.Triggered = checkEntityDefaultTriggered.Checked;
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void btnDeleteSelectedMarker_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;
      if ( m_SelectedMarker == null ) return;
      if ( !m_CurrentMap.Markers.Contains( m_SelectedMarker ) )
      {
        // Stale selection (e.g. map changed under us). Just clear it.
        SelectMarker( null );
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_CurrentMap.Markers.Remove( m_SelectedMarker );
      SelectMarker( null );
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
      UpdateMarkerOutOfBoundsLabel();
    }



    private void btnReflowOOBMarkers_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;
      int w = m_CurrentMap.Tiles.Width;
      int h = m_CurrentMap.Tiles.Height;
      if ( ( w <= 0 ) || ( h <= 0 ) ) return;

      // Gather every marker that lies outside the tile grid. Order is the
      // current Markers-list order — preserved when reassigning positions.
      var oob = new List<Formats.MapProject.Marker>();
      foreach ( var marker in m_CurrentMap.Markers )
      {
        if ( ( marker.X < 0 ) || ( marker.Y < 0 )
        ||   ( marker.X >= w ) || ( marker.Y >= h ) )
        {
          oob.Add( marker );
        }
      }
      if ( oob.Count == 0 )
      {
        UpdateMarkerOutOfBoundsLabel();
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );

      // Pack left-to-right, wrap at width. If the count exceeds w*h the
      // overflow lands past the bottom edge — that becomes its own OOB
      // count on the next refresh, which is acceptable: the user pressed
      // "pull in" knowing how many markers exist.
      for ( int i = 0; i < oob.Count; ++i )
      {
        oob[i].X = i % w;
        oob[i].Y = i / w;
      }

      SelectMarker( null );
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
      UpdateMarkerOutOfBoundsLabel();
    }



    private void UpdateMarkerOutOfBoundsLabel()
    {
      if ( labelMarkersOutOfBounds == null ) return;

      int count = 0;
      if ( m_CurrentMap != null )
      {
        int w = m_CurrentMap.Tiles.Width;
        int h = m_CurrentMap.Tiles.Height;
        foreach ( var marker in m_CurrentMap.Markers )
        {
          if ( ( marker.X < 0 ) || ( marker.Y < 0 )
          ||   ( marker.X >= w ) || ( marker.Y >= h ) )
          {
            ++count;
          }
        }
      }
      labelMarkersOutOfBounds.Text = "Markers out of bounds: " + count;
    }



    /// <summary>
    /// Refresh the entity-count label on the entities toolbar. Shows the
    /// number of entities on the current map that match the currently
    /// selected entity type. When no type is selected (or no map is open),
    /// shows 0. Called from every place that can mutate either the current
    /// map's entity list (placement / delete / shift / undo / clear) or the
    /// selected type (combo change, map change, project load).
    /// </summary>
    public void UpdateEntityCountLabel()
    {
      if ( labelEntityCount == null ) return;

      int count = 0;
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.SelectedEntityType != -1 ) )
      {
        int selectedType = m_CurrentMap.SelectedEntityType;
        foreach ( var entity in m_CurrentMap.Entities )
        {
          if ( entity.Type == selectedType )
          {
            ++count;
          }
        }
      }
      labelEntityCount.Text = "Count: " + count;
    }



    private void btnFindNextMarkerGroup_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;

      // Sequential allocator: start at the persisted cursor (clamped to >=1
      // because GroupId 0 is reserved for "no group"), then skip any ids
      // currently in use on this map. Cap at 255 — the byte limit of the
      // exported map binary.
      var inUse = new System.Collections.Generic.HashSet<int>();
      foreach ( var marker in m_CurrentMap.Markers )
      {
        inUse.Add( marker.GroupId );
      }

      int candidate = m_CurrentMap.NextMarkerGroupId;
      if ( candidate < 1 ) candidate = 1;
      while ( ( candidate <= 255 ) && inUse.Contains( candidate ) )
      {
        ++candidate;
      }

      if ( candidate > 255 )
      {
        System.Windows.Forms.MessageBox.Show(
          "All marker group ids from 1 to 255 are already in use, or the per-map allocator has been advanced past 255. Marker GroupId is a byte in the exported map binary and cannot exceed 255.",
          "No free marker group id",
          System.Windows.Forms.MessageBoxButtons.OK,
          System.Windows.Forms.MessageBoxIcon.Warning );
        // Park the cursor at the overflow sentinel so re-clicking without
        // freeing a slot keeps warning rather than silently rolling back.
        m_CurrentMap.NextMarkerGroupId = 256;
        SetModified();
        return;
      }

      editMarkerGroupId.Value = candidate;
      m_CurrentMap.NextMarkerGroupId = candidate + 1;
      SetModified();
    }



    /// <summary>
    /// Shared search for the lowest unused Value1 or Value2 (starting at 1,
    /// cap 255) among markers of the currently selected marker type on the
    /// current map. <paramref name="ValueSelector"/> picks which byte to
    /// scan — Value1 or Value2 — so the two ? buttons share the same logic.
    /// Returns -1 if no marker type is selected OR every slot 1..255 is
    /// already taken; the caller decides how to handle that.
    /// Spec: if there are no markers of the type yet, returns 1; otherwise
    /// returns the first gap, or one past the highest used value.
    /// The currently-selected marker (if any) is EXCLUDED from the in-use
    /// set — otherwise clicking ? on a selected marker would treat that
    /// marker's own value as taken, ping-pong between two values, and
    /// never settle.
    /// </summary>
    private int FindNextUnusedMarkerValue( System.Func<Formats.MapProject.Marker, byte> ValueSelector )
    {
      if ( m_CurrentMap == null ) return -1;
      if ( m_CurrentMap.SelectedMarkerType == -1 ) return -1;

      int selectedType = m_CurrentMap.SelectedMarkerType;
      var inUse = new System.Collections.Generic.HashSet<int>();
      foreach ( var marker in m_CurrentMap.Markers )
      {
        if ( marker.Type != selectedType ) continue;
        // Skip self so reassigning the currently-selected marker doesn't
        // see its own current value as a conflict.
        if ( marker == m_SelectedMarker ) continue;
        inUse.Add( ValueSelector( marker ) );
      }

      // No other markers of this type on the map → first unused id is 1.
      // Otherwise walk up from 1, skipping any taken slot — the first
      // candidate the loop accepts is either a gap (1,2,4 → 3) or just
      // past the highest used (1,2,3 → 4). Cap at 255 because Value1/2
      // are bytes in the exported map binary.
      int candidate = 1;
      while ( ( candidate <= 255 ) && inUse.Contains( candidate ) )
      {
        ++candidate;
      }
      if ( candidate > 255 ) return -1;
      return candidate;
    }



    private void btnFindNextMarkerValue1_Click( object sender, EventArgs e )
    {
      int candidate = FindNextUnusedMarkerValue( m => m.Value1 );
      if ( candidate < 0 )
      {
        if ( ( m_CurrentMap != null ) && ( m_CurrentMap.SelectedMarkerType != -1 ) )
        {
          System.Windows.Forms.MessageBox.Show(
            "All Value1 ids from 1 to 255 are already in use by markers of this type on the current map.",
            "No free Value1",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning );
        }
        return;
      }
      // Assigning to the spinner fires editMarkerValue_ValueChanged, which
      // either updates the selected marker (with undo) or just sets the
      // placement default — same path the user would take typing manually.
      editMarkerValue1.Value = candidate;
    }



    private void btnFindNextMarkerValue2_Click( object sender, EventArgs e )
    {
      int candidate = FindNextUnusedMarkerValue( m => m.Value2 );
      if ( candidate < 0 )
      {
        if ( ( m_CurrentMap != null ) && ( m_CurrentMap.SelectedMarkerType != -1 ) )
        {
          System.Windows.Forms.MessageBox.Show(
            "All Value2 ids from 1 to 255 are already in use by markers of this type on the current map.",
            "No free Value2",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning );
        }
        return;
      }
      editMarkerValue2.Value = candidate;
    }



    private void btnDeleteSelectedEntity_Click( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;
      if ( m_SelectedEntity == null ) return;
      if ( !m_CurrentMap.Entities.Contains( m_SelectedEntity ) )
      {
        SelectEntity( null );
        return;
      }

      // Snapshot the entity list before removal so Ctrl+Z restores
      // the deleted entity. No confirmation prompt — user explicitly
      // asked for deletion to be one-click.
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
      m_CurrentMap.Entities.Remove( m_SelectedEntity );
      SelectEntity( null );
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
      UpdateEntityCountLabel();
    }

    private void btnClearMarkers_Click( object sender, EventArgs e )
    {

    }

    private void btnClearMarkerType_Click( object sender, EventArgs e )
    {
    }

    private void comboMarkerColorOverride_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( m_CurrentMap == null )
      ||   ( m_CurrentMap.SelectedMarkerType == -1 ) )
      {
        return;
      }
      var type = m_MapProject.MarkerTypes.FirstOrDefault( t => t.ID == m_CurrentMap.SelectedMarkerType );
      if ( type != null )
      {
        if ( type.Color != comboMarkerColorOverride.SelectedIndex )
        {
          type.Color = comboMarkerColorOverride.SelectedIndex;
          // Sync with the other list if visible/selected
          if ( listMarkerTypes.SelectedIndex != -1 )
          {
             var listType = m_MapProject.MarkerTypes[listMarkerTypes.SelectedIndex];
             if ( listType.ID == type.ID )
             {
               comboMarkerColor.SelectedIndex = type.Color;
             }
          }
          pictureEditor.Invalidate();
          SetModified();
        }
      }
    }

    private void dimSlider_Scroll( object sender, EventArgs e )
    {
      if ( m_CurrentMap != null )
      {
        m_CurrentMap.MarkerDimOpacity = dimSlider.Value;
        SetModified();
        // ENTITY-mode dim is baked into DisplayPage inside RedrawMap, so a
        // pure Invalidate wouldn't re-apply the new opacity — we need a full
        // tile re-paint. (MARKER-mode dim happens in PictureEditor_PostPaint,
        // where Invalidate alone would be enough, but unifying the path keeps
        // the handler simple and the cost is a single tile render per drag.)
        RedrawMap();
        pictureEditor.Invalidate();
      }
    }
    
    /// <summary>
    /// Sync every Map-tab control whose meaning depends on the
    /// current tool mode (MARKER, ENTITY) and current selection.
    ///
    /// MARKER mode: the marker placement / edit controls in the
    /// flowLayoutPanel4 row are enabled (type picker, value editors,
    /// color override). They serve dual purpose — without a selection
    /// they configure defaults for the next placement, with one they
    /// edit the selected marker — so they're enabled whenever MARKER
    /// mode is active. The delete button is additionally
    /// selection-gated via <see cref="UpdateDeleteSelectedButtonsEnabled"/>.
    ///
    /// ENTITY mode: same deal for entity controls in flowLayoutPanel2
    /// (type picker, default value editors, enabled / triggered
    /// checkboxes). The visibility toggle (checkShowEntities) is
    /// always usable since "show entities" is meaningful even when
    /// you're not actively editing them.
    ///
    /// Outside MARKER/ENTITY mode, all of those controls are
    /// disabled — clicking them in (say) SINGLE_TILE mode would have
    /// no observable effect anyway, so showing them as enabled was
    /// just confusing.
    /// </summary>
    private void UpdateMarkerControlsState()
    {
       bool markerMode   = ( btnToolMarker != null )   && btnToolMarker.Checked;
       bool entityMode   = ( btnToolEntity != null )   && btnToolEntity.Checked;
       bool passableMode = ( btnToolPassable != null ) && btnToolPassable.Checked;

       // PASSABLE-only commands.
       if ( btnClearPassableMap != null ) btnClearPassableMap.Enabled = passableMode;

       // ----- MARKER side (flowLayoutPanel4) -----
       if ( comboMarkerTypes        != null ) comboMarkerTypes.Enabled        = markerMode;
       if ( comboMarkerColorOverride != null ) comboMarkerColorOverride.Enabled = markerMode;
       if ( labelMarkerValue1       != null ) labelMarkerValue1.Enabled       = markerMode;
       if ( editMarkerValue1        != null ) editMarkerValue1.Enabled        = markerMode;
       if ( labelMarkerValue2       != null ) labelMarkerValue2.Enabled       = markerMode;
       if ( editMarkerValue2        != null ) editMarkerValue2.Enabled        = markerMode;
       if ( labelMarkerGroupId      != null ) labelMarkerGroupId.Enabled      = markerMode;
       if ( editMarkerGroupId       != null ) editMarkerGroupId.Enabled       = markerMode;
       if ( btnReflowOOBMarkers     != null ) btnReflowOOBMarkers.Enabled     = markerMode;
       UpdateMarkerOutOfBoundsLabel();

       // Tools menu items that act on markers — only meaningful in
       // marker mode. Disabled elsewhere so the user can't trip
       // a wholesale clear from inside, e.g., entity mode.
       if ( clearAllMarkersToolStripMenuItem != null ) clearAllMarkersToolStripMenuItem.Enabled = markerMode;
       if ( clearMarkerTypeMenuItem          != null ) clearMarkerTypeMenuItem.Enabled          = markerMode;

       // ----- ENTITY side (flowLayoutPanel2) -----
       if ( comboEntityTypes          != null ) comboEntityTypes.Enabled          = entityMode;
       if ( labelEntityValue1         != null ) labelEntityValue1.Enabled         = entityMode;
       if ( editEntityValue1Default   != null ) editEntityValue1Default.Enabled   = entityMode;
       if ( labelEntityValue2         != null ) labelEntityValue2.Enabled         = entityMode;
       if ( editEntityValue2Default   != null ) editEntityValue2Default.Enabled   = entityMode;
       if ( checkEntityDefaultEnabled != null ) checkEntityDefaultEnabled.Enabled = entityMode;
       if ( checkEntityDefaultTriggered != null ) checkEntityDefaultTriggered.Enabled = entityMode;
       // checkShowEntities intentionally NOT gated — the user may
       // want to see entity icons while painting tiles.

       // The dim slider is shared between marker and entity placement modes.
       if ( dimSlider != null ) dimSlider.Enabled = markerMode || entityMode;

       UpdateDeleteSelectedButtonsEnabled();
    }



    /// <summary>
    /// Sync the "Delete ✕" toolbar buttons' Enabled state with the current
    /// selection. Called whenever <see cref="m_SelectedMarker"/> or
    /// <see cref="m_SelectedEntity"/> changes, AND whenever the tool mode
    /// changes (since switching away from MARKER/ENTITY also clears that
    /// side's selection). Cheap enough to call eagerly.
    /// </summary>
    private void UpdateDeleteSelectedButtonsEnabled()
    {
      if ( btnDeleteSelectedMarker != null )
      {
        btnDeleteSelectedMarker.Enabled =
          ( m_ToolMode == ToolMode.MARKER )
          && ( m_SelectedMarker != null );
      }
      if ( btnDeleteSelectedEntity != null )
      {
        btnDeleteSelectedEntity.Enabled =
          ( m_ToolMode == ToolMode.ENTITY )
          && ( m_SelectedEntity != null );
      }

      // Marker resize buttons (H+/H-/V+/V-) follow the same rule as the
      // marker Delete button — a marker must be selected in MARKER mode.
      bool canResizeMarker = ( m_ToolMode == ToolMode.MARKER ) && ( m_SelectedMarker != null );
      if ( btnMarkerWidthInc  != null ) btnMarkerWidthInc.Enabled  = canResizeMarker;
      if ( btnMarkerWidthDec  != null ) btnMarkerWidthDec.Enabled  = canResizeMarker;
      if ( btnMarkerHeightInc != null ) btnMarkerHeightInc.Enabled = canResizeMarker;
      if ( btnMarkerHeightDec != null ) btnMarkerHeightDec.Enabled = canResizeMarker;
    }



    /// <summary>True when cell (PX,PY) lies inside marker M's Width x Height footprint.</summary>
    private static bool MarkerContainsPoint( Formats.MapProject.Marker M, int PX, int PY )
    {
      int w = ( M.Width < 1 ) ? 1 : M.Width;
      int h = ( M.Height < 1 ) ? 1 : M.Height;
      return ( PX >= M.X ) && ( PX < M.X + w )
          && ( PY >= M.Y ) && ( PY < M.Y + h );
    }



    /// <summary>
    /// True when the footprint rect (X,Y,W,H) overlaps any marker on the
    /// current map other than Exclude — used to keep markers non-overlapping
    /// when placing, dragging, or resizing.
    /// </summary>
    private bool MarkerFootprintOverlaps( int X, int Y, int W, int H, Formats.MapProject.Marker Exclude )
    {
      if ( m_CurrentMap == null )
      {
        return false;
      }
      foreach ( var m in m_CurrentMap.Markers )
      {
        if ( m == Exclude )
        {
          continue;
        }
        int mw = ( m.Width < 1 ) ? 1 : m.Width;
        int mh = ( m.Height < 1 ) ? 1 : m.Height;
        // Axis-aligned rectangle overlap test.
        if ( ( X < m.X + mw ) && ( X + W > m.X )
        &&   ( Y < m.Y + mh ) && ( Y + H > m.Y ) )
        {
          return true;
        }
      }
      return false;
    }



    /// <summary>
    /// Resize the selected marker by (DeltaW, DeltaH) — backs the H+/H-/V+/V-
    /// toolbar buttons. Minimum size is 1x1. Growing is rejected when the new
    /// footprint would run past the map edge or overlap another marker;
    /// shrinking can never violate either, so it is always allowed. One undo
    /// snapshot is taken so Ctrl+Z reverts the resize.
    /// </summary>
    private void ResizeSelectedMarker( int DeltaW, int DeltaH )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }
      if ( m_SelectedMarker == null )
      {
        return;
      }
      if ( !m_CurrentMap.Markers.Contains( m_SelectedMarker ) )
      {
        return;
      }

      int newW = m_SelectedMarker.Width + DeltaW;
      int newH = m_SelectedMarker.Height + DeltaH;
      if ( newW < 1 ) newW = 1;
      if ( newH < 1 ) newH = 1;
      if ( ( newW == m_SelectedMarker.Width )
      &&   ( newH == m_SelectedMarker.Height ) )
      {
        // Already at the minimum — nothing changed.
        return;
      }

      // Shrinking can never push past an edge or into another marker, so it
      // is always allowed. Growing must clear both checks.
      bool growing = ( newW > m_SelectedMarker.Width ) || ( newH > m_SelectedMarker.Height );
      if ( growing )
      {
        if ( ( m_SelectedMarker.X + newW > m_CurrentMap.Tiles.Width )
        ||   ( m_SelectedMarker.Y + newH > m_CurrentMap.Tiles.Height ) )
        {
          // Footprint would leave the map.
          return;
        }
        if ( MarkerFootprintOverlaps( m_SelectedMarker.X, m_SelectedMarker.Y, newW, newH, m_SelectedMarker ) )
        {
          // Footprint would collide with another marker.
          return;
        }
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_SelectedMarker.Width = newW;
      m_SelectedMarker.Height = newH;
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    private void btnMarkerWidthInc_Click( object sender, EventArgs e )
    {
      ResizeSelectedMarker( 1, 0 );
    }



    private void btnMarkerWidthDec_Click( object sender, EventArgs e )
    {
      ResizeSelectedMarker( -1, 0 );
    }



    private void btnMarkerHeightInc_Click( object sender, EventArgs e )
    {
      ResizeSelectedMarker( 0, 1 );
    }



    private void btnMarkerHeightDec_Click( object sender, EventArgs e )
    {
      ResizeSelectedMarker( 0, -1 );
    }



    /// <summary>
    /// Clear marker, entity, AND tile-cursor selection and refresh the
    /// delete buttons. Used on map change, tool change, and the Escape
    /// key. Returns true when something was actually cleared so callers
    /// (notably <see cref="ProcessCmdKey"/>) can decide whether to swallow
    /// the input that triggered the clear.
    /// </summary>
    private bool ClearMarkerEntitySelection()
    {
      bool hadSomething = ( m_SelectedMarker != null )
                          || ( m_SelectedEntity != null )
                          || ( m_SelectedTilePos.X >= 0 );
      m_SelectedMarker = null;
      m_SelectedEntity = null;
      m_SelectedTilePos = new System.Drawing.Point( -1, -1 );
      UpdateDeleteSelectedButtonsEnabled();
      if ( hadSomething )
      {
        pictureEditor.Invalidate();
      }
      return hadSomething;
    }



    /// <summary>
    /// Select a marker (or null to deselect). When a marker is selected the
    /// toolbar's marker-related controls are populated from it, and further
    /// changes to those controls write back into the marker instance via
    /// the ValueChanged/CheckedChanged/SelectedIndexChanged handlers.
    /// Wrapped in <see cref="m_PopulatingFromSelection"/> so the populate
    /// step doesn't immediately fire those same handlers with the values
    /// we just copied in (a no-op but it pollutes undo history).
    /// </summary>
    private void SelectMarker( Formats.MapProject.Marker marker )
    {
      m_SelectedMarker = marker;
      // Selecting a marker kicks out any entity selection — only one side
      // can be active in the UI at a time.
      m_SelectedEntity = null;

      if ( marker != null )
      {
        m_PopulatingFromSelection = true;
        try
        {
          editMarkerValue1.Value = marker.Value1;
          editMarkerValue2.Value = marker.Value2;
          editMarkerGroupId.Value = marker.GroupId;
          editMarkerLinkToID.Value = marker.LinkToID;
          editMarkerLinkID.Value = marker.LinkID;
          checkMarkerDefaultEnabled.Checked = marker.Enabled;
          checkMarkerDefaultTriggered.Checked = marker.Triggered;
          checkMarkerAutoDisable.Checked = marker.AutoDisableAfterTrigger;

          int typeIndex = m_MapProject.MarkerTypes.FindIndex( t => t.ID == marker.Type );
          if ( typeIndex != -1 )
          {
            // +1 because index 0 of the combo is the "None" placeholder.
            comboMarkerTypes.SelectedIndex = typeIndex + 1;
          }
        }
        finally
        {
          m_PopulatingFromSelection = false;
        }
      }
      UpdateDeleteSelectedButtonsEnabled();
      pictureEditor.Invalidate();
    }



    /// <summary>
    /// Select an entity (or null to deselect). Mirrors <see cref="SelectMarker"/>;
    /// entity selection clears any marker selection for the same
    /// one-thing-active-at-a-time rule.
    /// </summary>
    private void SelectEntity( Formats.MapProject.Entity entity )
    {
      m_SelectedEntity = entity;
      m_SelectedMarker = null;

      if ( entity != null )
      {
        m_PopulatingFromSelection = true;
        try
        {
          editEntityValue1Default.Value = entity.Value1;
          editEntityValue2Default.Value = entity.Value2;
          checkEntityDefaultEnabled.Checked = entity.Enabled;
          checkEntityDefaultTriggered.Checked = entity.Triggered;

          int typeIndex = m_MapProject.EntityTypes.FindIndex( t => t.ID == entity.Type );
          if ( typeIndex != -1 )
          {
            comboEntityTypes.SelectedIndex = typeIndex + 1;
          }
        }
        finally
        {
          m_PopulatingFromSelection = false;
        }
      }
      UpdateDeleteSelectedButtonsEnabled();
      pictureEditor.Invalidate();
    }
    private void comboMarkerColorOverride_DrawItem( object sender, DrawItemEventArgs e )
    {
       ComboBox combo = (ComboBox)sender;

       if ( e.Index < 0 ) return;

       if ( Core?.Theming != null )
         Core.Theming.DrawThemedBackground( e, combo );
       else
         e.DrawBackground();

       int colorIndex = e.Index;

       // Assuming items are just index 0..15 or equivalent strings
       // Or grab from item? The list was populated with "00", "01"...
       // But we just want the color.

       uint color = m_MapProject.Charset.Colors.Palette.ColorValues[colorIndex];

       using ( var brush = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( (int)color ) ) )
       {
         e.Graphics.FillRectangle( brush, e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 4, e.Bounds.Height - 4 );
       }
       e.Graphics.DrawRectangle( System.Drawing.Pens.Black, e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 5, e.Bounds.Height - 5 );

       e.DrawFocusRectangle();
    }



    /// <summary>
    /// Populate the tile-placement color combo with "Default" + 16 C64
    /// colors. Only repopulates when the count is wrong so we don't churn
    /// items every project load — the colors come from the project's
    /// palette so palette swaps still need to refresh, but adding/removing
    /// colors at runtime isn't a thing.
    /// </summary>
    private void RefreshTilePlacementColorCombo()
    {
      if ( comboTilePlacementColor == null )
      {
        return;
      }
      // Combo holds 17 items: [0] = "Default" sentinel, [1..16] map to
      // C64 color indices 0..15. Owner-draw renders a swatch for the
      // color rows and the literal text for the Default row.
      const int ExpectedCount = 17;
      if ( comboTilePlacementColor.Items.Count == ExpectedCount )
      {
        comboTilePlacementColor.Invalidate();
        return;
      }
      int prev = comboTilePlacementColor.SelectedIndex;
      // Repopulate is a UI-only refresh (e.g. after a theme change);
      // it must not push a color into a currently-selected tile.
      m_SuppressTilePlacementColorAutoApply = true;
      try
      {
        comboTilePlacementColor.BeginUpdate();
        comboTilePlacementColor.Items.Clear();
        comboTilePlacementColor.Items.Add( "Default" );
        for ( int i = 0; i < 16; ++i )
        {
          comboTilePlacementColor.Items.Add( i.ToString( "00" ) );
        }
        comboTilePlacementColor.EndUpdate();
        comboTilePlacementColor.SelectedIndex = ( prev >= 0 && prev < ExpectedCount )
                                                ? prev : 0;
      }
      finally
      {
        m_SuppressTilePlacementColorAutoApply = false;
      }
    }



    private void comboTilePlacementColor_DrawItem( object sender, DrawItemEventArgs e )
    {
      ComboBox combo = (ComboBox)sender;
      if ( e.Index < 0 ) return;

      if ( Core?.Theming != null )
        Core.Theming.DrawThemedBackground( e, combo );
      else
        e.DrawBackground();

      if ( e.Index == 0 )
      {
        // "Default" row — no swatch, just the literal label aligned with
        // where the numeric labels appear on the color rows below so the
        // dropdown reads as a clean column.
        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( "Default", combo.Font, brush,
                                 e.Bounds.X + 4, e.Bounds.Y + 3 );
        }
      }
      else
      {
        // Color rows: combo index 1..16 → palette index 0..15. Layout
        // (left-to-right): numeric "00".."15" label, then a color swatch
        // filling the rest of the row width (minus a small right margin),
        // with a thin black border.
        int colorIndex = e.Index - 1;
        uint color = m_MapProject.Charset.Colors.Palette.ColorValues[colorIndex];

        // Reserve a fixed column on the left for the index text. 26 px
        // is enough for two digits in the editor's typical font; if the
        // user picks a much larger font this stays readable because the
        // swatch only ever shrinks, never overlaps.
        const int IndexColumnWidth = 26;
        const int RightMargin      = 4;
        const int SwatchInsetTop   = 2;
        const int SwatchInsetBot   = 2;

        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( colorIndex.ToString( "00" ), combo.Font, brush,
                                 e.Bounds.X + 4, e.Bounds.Y + 3 );
        }

        int swatchX = e.Bounds.X + IndexColumnWidth;
        int swatchY = e.Bounds.Y + SwatchInsetTop;
        int swatchW = e.Bounds.Width - IndexColumnWidth - RightMargin;
        int swatchH = e.Bounds.Height - SwatchInsetTop - SwatchInsetBot;
        if ( swatchW < 1 ) swatchW = 1;
        if ( swatchH < 1 ) swatchH = 1;

        using ( var brush = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( (int)color ) ) )
        {
          e.Graphics.FillRectangle( brush, swatchX, swatchY, swatchW, swatchH );
        }
        // Inset the border by one pixel from the bottom-right edge so
        // the rectangle is fully visible (FillRectangle and DrawRectangle
        // use slightly different inclusive/exclusive conventions).
        e.Graphics.DrawRectangle( System.Drawing.Pens.Black,
                                  swatchX, swatchY,
                                  swatchW - 1, swatchH - 1 );
      }

      e.DrawFocusRectangle();
    }



    private void comboTilePlacementColor_SelectedIndexChanged( object sender, EventArgs e )
    {
      // Combo index 0 is "Default" → no override; subsequent indices map
      // 1:1 to C64 color indices via "combo - 1".
      if ( comboTilePlacementColor.SelectedIndex <= 0 )
      {
        m_TilePlacementColorOverride = -1;
      }
      else
      {
        m_TilePlacementColorOverride = comboTilePlacementColor.SelectedIndex - 1;
      }

      // User-driven combo change while a single tile is right-click
      // selected (no marker / entity selection, not in revision view) —
      // immediately stamp the new color across the selected tile's full
      // char footprint. Lets the user recolor an existing placed tile
      // without first re-painting it. Programmatic combo changes (init,
      // eyedropper, repopulate) bypass this via the suppress flag — they
      // mustn't push a color into a tile the user didn't explicitly ask
      // to recolor.
      if ( !m_SuppressTilePlacementColorAutoApply )
      {
        ApplyPlacementColorOverrideToSelectedTile();
      }
    }



    /// <summary>
    /// If a single tile is right-click selected (no marker / entity
    /// selection, map editable), stamp the current
    /// <see cref="m_TilePlacementColorOverride"/> into every char of
    /// that tile's full footprint. Footprint = max(spacing, tile.Chars
    /// dimensions), matching <see cref="ApplyPlacementColorOverride"/>.
    /// Snapshots via <see cref="Undo.UndoMapTilesChange"/> for the
    /// tile-cell footprint so Ctrl+Z restores both the prior tile data
    /// and both override layers (color + blocked) atomically. Bails on
    /// no-op (out-of-range / no tile / read-only / nothing to do).
    /// </summary>
    private void ApplyPlacementColorOverrideToSelectedTile()
    {
      if ( m_CurrentMap == null )      return;
      if ( m_IsViewingRevision )       return;
      if ( m_SelectedMarker != null )  return;
      if ( m_SelectedEntity != null )  return;
      int sx = m_SelectedTilePos.X;
      int sy = m_SelectedTilePos.Y;
      if ( ( sx < 0 ) || ( sy < 0 ) )  return;
      if ( ( sx >= m_CurrentMap.Tiles.Width )
      ||   ( sy >= m_CurrentMap.Tiles.Height ) ) return;

      int tileIndex = m_CurrentMap.Tiles[sx, sy];
      if ( ( tileIndex < 0 )
      ||   ( tileIndex >= m_MapProject.Tiles.Count ) ) return;
      var tile = m_MapProject.Tiles[tileIndex];

      // Snapshot the affected tile-cell footprint BEFORE mutating so
      // undo restores both Tiles[] (no change here, but the snapshot
      // path expects it) and the per-char override layers. Use the
      // tile's TILE-CELL footprint (in tile cells) for the undo extent
      // — same helper TryDeleteRightClickedTile uses.
      int undoW, undoH;
      GetTileCellFootprint( tile, out undoW, out undoH );
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapTilesChange( this, m_CurrentMap, sx, sy, undoW, undoH ) );

      // Char footprint = max(spacing, tile.Chars dims). Same formula as
      // ApplyPlacementColorOverride — when spacing < Chars (e.g.
      // spacing=1 on a 2x2 tile) the tile renders 4 chars, so we have
      // to recolor all of them not just spacing²=1.
      int spacingX = m_CurrentMap.TileSpacingX;
      int spacingY = m_CurrentMap.TileSpacingY;
      int footprintX = ( tile.Chars.Width  > spacingX ) ? tile.Chars.Width  : spacingX;
      int footprintY = ( tile.Chars.Height > spacingY ) ? tile.Chars.Height : spacingY;
      int charBaseX = sx * spacingX;
      int charBaseY = sy * spacingY;
      int layerW = m_CurrentMap.TileColorOverrides.Width;
      int layerH = m_CurrentMap.TileColorOverrides.Height;
      bool anyChanged = false;
      for ( int dy = 0; dy < footprintY; ++dy )
      {
        for ( int dx = 0; dx < footprintX; ++dx )
        {
          int cx = charBaseX + dx;
          int cy = charBaseY + dy;
          if ( ( cx >= 0 ) && ( cy >= 0 )
          &&   ( cx < layerW ) && ( cy < layerH ) )
          {
            if ( m_CurrentMap.TileColorOverrides[cx, cy] != m_TilePlacementColorOverride )
            {
              m_CurrentMap.TileColorOverrides[cx, cy] = m_TilePlacementColorOverride;
              anyChanged = true;
            }
          }
        }
      }

      if ( anyChanged )
      {
        UpdateArea( sx, sy, undoW, undoH );
        pictureEditor.Invalidate();
        SetModified();
      }
    }



    /// <summary>
    /// Resolve the EFFECTIVE color of a single character cell on the
    /// current map — what the renderer would actually paint:
    ///   1. If <see cref="MapProject.Map.TileColorOverrides"/> at
    ///      (charMapX, charMapY) is &gt;= 0, that's the effective color.
    ///   2. Otherwise fall back to the underlying tile's intrinsic
    ///      char color: <c>tile.Chars[localCharX, localCharY].Color</c>
    ///      where (localCharX, localCharY) is the char's position
    ///      within the tile's footprint.
    /// Returns -1 when no color can be resolved (off-map, invalid
    /// tile index, char outside the tile's <see cref="Tile.Chars"/>
    /// rect). Used by <see cref="ApplyBrightnessShift"/>; the same
    /// resolution pattern is also inlined in DrawTile, the
    /// middle-click eyedropper, and the export paths — those weren't
    /// refactored to call this helper because they're hot/critical
    /// paths and a cross-cutting refactor wasn't required for v1.
    /// </summary>
    private int GetEffectiveCharColor( int charMapX, int charMapY )
    {
      if ( m_CurrentMap == null ) return -1;
      // Override wins when set.
      if ( ( charMapX >= 0 ) && ( charMapY >= 0 )
      &&   ( charMapX < m_CurrentMap.TileColorOverrides.Width )
      &&   ( charMapY < m_CurrentMap.TileColorOverrides.Height ) )
      {
        int ov = m_CurrentMap.TileColorOverrides[charMapX, charMapY];
        if ( ov >= 0 ) return ov;
      }
      int spacingX = m_CurrentMap.TileSpacingX;
      int spacingY = m_CurrentMap.TileSpacingY;
      if ( ( spacingX <= 0 ) || ( spacingY <= 0 ) ) return -1;
      int tileX = charMapX / spacingX;
      int tileY = charMapY / spacingY;
      if ( ( tileX < 0 ) || ( tileY < 0 )
      ||   ( tileX >= m_CurrentMap.Tiles.Width )
      ||   ( tileY >= m_CurrentMap.Tiles.Height ) ) return -1;
      int tileIndex = m_CurrentMap.Tiles[tileX, tileY];
      if ( ( tileIndex < 0 ) || ( tileIndex >= m_MapProject.Tiles.Count ) ) return -1;
      var tile = m_MapProject.Tiles[tileIndex];
      int localX = charMapX - tileX * spacingX;
      int localY = charMapY - tileY * spacingY;
      if ( ( localX < 0 ) || ( localY < 0 )
      ||   ( localX >= tile.Chars.Width )
      ||   ( localY >= tile.Chars.Height ) ) return -1;
      return tile.Chars[localX, localY].Color;
    }



    /// <summary>
    /// Walk every char of every selected cell and shift its color
    /// through one of the brightness tables (Linear-Up / Linear-Down /
    /// Hue-Up / Hue-Down). Writes into TileColorOverrides as an
    /// override, except when the result equals the tile's intrinsic
    /// char color — in that case the override is cleared to -1
    /// (Default), so the layer stays sparse and Up→Down round-trips
    /// return to a fully-default state. Chars whose source color has
    /// no neighbor in the chosen direction (-1 in the table) are left
    /// unchanged.
    ///
    /// Selection sources, in priority order:
    ///   1. Rectangle selection (m_SelectedTiles) — used when any
    ///      cell is set.
    ///   2. Right-click single-tile (m_SelectedTilePos) — fallback
    ///      when source 1 is empty.
    /// No-op when neither is active.
    /// </summary>
    private void ApplyBrightnessShift( int[] map )
    {
      if ( m_CurrentMap == null )      return;
      if ( m_IsViewingRevision )       return;
      if ( m_SelectedMarker != null )  return;
      if ( m_SelectedEntity != null )  return;
      if ( map == null )               return;

      // Determine the bounding tile-rect of whichever selection is
      // active. Rectangle selection takes priority; right-click single
      // tile is the fallback when no rectangle is set.
      int minTX = int.MaxValue, minTY = int.MaxValue, maxTX = -1, maxTY = -1;

      if ( m_SelectedTiles != null )
      {
        int rectGridW = m_SelectedTiles.GetLength( 0 );
        int rectGridH = m_SelectedTiles.GetLength( 1 );
        int scanW = System.Math.Min( rectGridW, m_CurrentMap.Tiles.Width  );
        int scanH = System.Math.Min( rectGridH, m_CurrentMap.Tiles.Height );
        for ( int x = 0; x < scanW; ++x )
        {
          for ( int y = 0; y < scanH; ++y )
          {
            if ( m_SelectedTiles[x, y] )
            {
              if ( x < minTX ) minTX = x;
              if ( y < minTY ) minTY = y;
              if ( x > maxTX ) maxTX = x;
              if ( y > maxTY ) maxTY = y;
            }
          }
        }
      }

      // Right-click single-tile fallback when the rectangle was empty.
      bool useSingleTileSelection = false;
      if ( maxTX < 0 )
      {
        int sx = m_SelectedTilePos.X;
        int sy = m_SelectedTilePos.Y;
        if ( ( sx >= 0 ) && ( sy >= 0 )
        &&   ( sx < m_CurrentMap.Tiles.Width )
        &&   ( sy < m_CurrentMap.Tiles.Height ) )
        {
          minTX = maxTX = sx;
          minTY = maxTY = sy;
          useSingleTileSelection = true;
        }
      }

      if ( maxTX < 0 ) return;   // no usable selection — silent no-op

      int rectW = maxTX - minTX + 1;
      int rectH = maxTY - minTY + 1;

      // Single UndoMapTilesChange covering the bounding rect. Already
      // snapshots Tiles + both override layers (color + blocked), so
      // Ctrl+Z restores everything atomically.
      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapTilesChange( this, m_CurrentMap, minTX, minTY, rectW, rectH ) );

      int spacingX = m_CurrentMap.TileSpacingX;
      int spacingY = m_CurrentMap.TileSpacingY;
      bool any = false;

      for ( int tx = minTX; tx <= maxTX; ++tx )
      {
        for ( int ty = minTY; ty <= maxTY; ++ty )
        {
          // Cell-membership: rectangle respects the bool grid;
          // single-tile fallback is the one cell at (minTX, minTY).
          if ( useSingleTileSelection )
          {
            if ( ( tx != minTX ) || ( ty != minTY ) ) continue;
          }
          else if ( !m_SelectedTiles[tx, ty] )
          {
            continue;
          }

          int tileIndex = m_CurrentMap.Tiles[tx, ty];
          if ( ( tileIndex < 0 )
          ||   ( tileIndex >= m_MapProject.Tiles.Count ) ) continue;
          var tile = m_MapProject.Tiles[tileIndex];

          // Walk the tile's full char footprint (max(spacing, Chars
          // dims)) so when spacing < Chars dims (e.g. spacing=1 on a
          // 2x2 tile) all four chars get touched — same convention
          // ApplyPlacementColorOverride uses.
          int fpX = ( tile.Chars.Width  > spacingX ) ? tile.Chars.Width  : spacingX;
          int fpY = ( tile.Chars.Height > spacingY ) ? tile.Chars.Height : spacingY;
          for ( int dy = 0; dy < fpY; ++dy )
          {
            for ( int dx = 0; dx < fpX; ++dx )
            {
              int cx = tx * spacingX + dx;
              int cy = ty * spacingY + dy;
              if ( ( cx < 0 ) || ( cy < 0 )
              ||   ( cx >= m_CurrentMap.TileColorOverrides.Width )
              ||   ( cy >= m_CurrentMap.TileColorOverrides.Height ) ) continue;

              int srcColor = GetEffectiveCharColor( cx, cy );
              if ( ( srcColor < 0 ) || ( srcColor >= 16 ) ) continue;
              int dstColor = map[srcColor];
              if ( dstColor < 0 ) continue;        // rail — leave unchanged
              if ( dstColor == srcColor ) continue; // no-op shift

              // Tile's intrinsic char color at this local position.
              // Used for the "match-original-clear-to-default" rule
              // — keeps the override layer sparse so Up→Down round-
              // trips end at -1 (no override).
              int tileColor = -1;
              if ( ( dx < tile.Chars.Width ) && ( dy < tile.Chars.Height ) )
              {
                tileColor = tile.Chars[dx, dy].Color;
              }

              int writeValue = ( dstColor == tileColor ) ? -1 : dstColor;
              if ( m_CurrentMap.TileColorOverrides[cx, cy] != writeValue )
              {
                m_CurrentMap.TileColorOverrides[cx, cy] = writeValue;
                any = true;
              }
            }
          }
        }
      }

      if ( any )
      {
        UpdateArea( minTX, minTY, rectW, rectH );
        pictureEditor.Invalidate();
        SetModified();
      }
    }



    /// <summary>
    /// Open the modal Brightness Tables editor. Lets the user reorder
    /// the linear chain and edit hue chains via swatches. Apply on OK
    /// rebuilds the per-color Up/Down arrays from the new chains;
    /// Cancel discards. Tables persist in StudioSettings on the next
    /// settings save.
    /// </summary>
    private void brightnessTablesToolStripMenuItem_Click( object sender, EventArgs e )
    {
      using ( var dlg = new Dialogs.DlgBrightnessTables( Core ) )
      {
        dlg.ShowDialog( FindForm() );
      }
      // Dialog OK may have flipped BrightnessLinearEnabled — refresh
      // the toolbar button state (and the keyboard shortcuts pick up
      // the new flag at next press, no separate sync needed).
      RefreshBrightnessButtonState();
    }



    /// <summary>
    /// Sync the Linear brightness toolbar buttons' Enabled state to
    /// <see cref="StudioSettings.BrightnessLinearEnabled"/>. The Hue
    /// buttons stay always-enabled — Hue uses per-chain Enabled flags
    /// (skipped chains just produce no-ops at apply time, the user
    /// shouldn't have to grey out the toolbar to disable hue work
    /// because they might still want to use SOME chains).
    /// </summary>
    private void RefreshBrightnessButtonState()
    {
      bool linEnabled = ( Core?.Settings != null ) && Core.Settings.BrightnessLinearEnabled;
      if ( btnBrightnessLinearUp != null )   btnBrightnessLinearUp.Enabled   = linEnabled;
      if ( btnBrightnessLinearDown != null ) btnBrightnessLinearDown.Enabled = linEnabled;
    }



    private void btnBrightnessLinearUp_Click( object sender, EventArgs e )
    {
      ApplyBrightnessShift( ( Core?.Settings != null ) ? Core.Settings.BrightnessLinearUp : null );
    }

    private void btnBrightnessLinearDown_Click( object sender, EventArgs e )
    {
      ApplyBrightnessShift( ( Core?.Settings != null ) ? Core.Settings.BrightnessLinearDown : null );
    }

    private void btnBrightnessHueUp_Click( object sender, EventArgs e )
    {
      ApplyBrightnessShift( ( Core?.Settings != null ) ? Core.Settings.BrightnessHueUp : null );
    }

    private void btnBrightnessHueDown_Click( object sender, EventArgs e )
    {
      ApplyBrightnessShift( ( Core?.Settings != null ) ? Core.Settings.BrightnessHueDown : null );
    }



    /// <summary>
    /// Stamp the current placement color override into every CHARACTER
    /// slot covered by the tile that was just placed at <paramref name="cellX"/>,
    /// <paramref name="cellY"/>. The cell coords are TILE coords; the
    /// stamping covers the full footprint of the placed tile — which can
    /// be LARGER than spacingX × spacingY (e.g. a 2x2-char tile in a
    /// spacing=1 map covers 4 chars across 4 different slots). Without
    /// honouring the actual tile size, only the chars belonging to the
    /// stamping slot would be cleared and the other chars the tile
    /// renders into would keep stale overrides.
    ///
    /// -1 leaves every char slot at -1 (uses the tile's intrinsic
    /// per-char colours); 0..15 paints every character of the
    /// placement in that single C64 colour. Called by every
    /// tile-placement code path right after writing the tile index. For
    /// per-character painting (Ctrl+click), see HandleMouseOnEditor —
    /// that path writes a single char slot.
    /// </summary>
    private void ApplyPlacementColorOverride( int cellX, int cellY )
    {
      if ( m_CurrentMap == null ) return;
      int spacingX = m_CurrentMap.TileSpacingX;
      int spacingY = m_CurrentMap.TileSpacingY;

      // Footprint = max( spacing, tile's char dimensions ). The just-
      // placed tile lives at Tiles[cellX, cellY]; reading it here means
      // callers don't need to thread the dimensions through.
      int footprintX = spacingX;
      int footprintY = spacingY;
      if ( ( cellX >= 0 ) && ( cellY >= 0 )
      &&   ( cellX < m_CurrentMap.Tiles.Width )
      &&   ( cellY < m_CurrentMap.Tiles.Height ) )
      {
        int placedTileIndex = m_CurrentMap.Tiles[cellX, cellY];
        if ( ( placedTileIndex >= 0 )
        &&   ( placedTileIndex < m_MapProject.Tiles.Count ) )
        {
          var placedTile = m_MapProject.Tiles[placedTileIndex];
          if ( placedTile.Chars.Width  > footprintX ) footprintX = placedTile.Chars.Width;
          if ( placedTile.Chars.Height > footprintY ) footprintY = placedTile.Chars.Height;
        }
      }

      int charBaseX = cellX * spacingX;
      int charBaseY = cellY * spacingY;
      int charLayerW = m_CurrentMap.TileColorOverrides.Width;
      int charLayerH = m_CurrentMap.TileColorOverrides.Height;
      for ( int dy = 0; dy < footprintY; ++dy )
      {
        for ( int dx = 0; dx < footprintX; ++dx )
        {
          int cx = charBaseX + dx;
          int cy = charBaseY + dy;
          if ( ( cx >= 0 ) && ( cy >= 0 )
          &&   ( cx < charLayerW ) && ( cy < charLayerH ) )
          {
            m_CurrentMap.TileColorOverrides[cx, cy] = m_TilePlacementColorOverride;
          }
        }
      }

      // Reset the per-character "blocked" overrides for this tile's
      // footprint. Placing a fresh tile is a "reset this region"
      // gesture — keeping stale overrides risks silently re-blocking a
      // newly-placed wall door or floor tile. UndoMapTilesChange
      // snapshots both layers so Ctrl+Z restores the prior state.
      int blkLayerW = m_CurrentMap.CharBlockedOverrides.Width;
      int blkLayerH = m_CurrentMap.CharBlockedOverrides.Height;
      for ( int dy = 0; dy < footprintY; ++dy )
      {
        for ( int dx = 0; dx < footprintX; ++dx )
        {
          int cx = charBaseX + dx;
          int cy = charBaseY + dy;
          if ( ( cx >= 0 ) && ( cy >= 0 )
          &&   ( cx < blkLayerW ) && ( cy < blkLayerH ) )
          {
            m_CurrentMap.CharBlockedOverrides[cx, cy] = false;
          }
        }
      }
    }



    // ================================================================
    // Entity handlers (mirror marker handlers — project-level types on the
    // Entities tab + per-map placement via the Entity tool).
    // ================================================================

    private void RefreshEntityTypes()
    {
      int savedListIndex = listEntityTypes.SelectedIndex;
      int savedComboIndex = comboEntityTypes.SelectedIndex;

      listEntityTypes.Items.Clear();
      comboEntityTypes.Items.Clear();
      comboEntityTypes.Items.Add( "None" );
      foreach ( var type in m_MapProject.EntityTypes )
      {
        listEntityTypes.Items.Add( type.Name );
        comboEntityTypes.Items.Add( type.Name );
      }

      if ( ( savedListIndex >= 0 ) && ( savedListIndex < listEntityTypes.Items.Count ) )
      {
        listEntityTypes.SelectedIndex = savedListIndex;
      }

      // Restore combo: match by current map's SelectedEntityType ID if we have one.
      if ( ( m_CurrentMap != null )
      &&   ( m_CurrentMap.SelectedEntityType != -1 ) )
      {
        int idx = m_MapProject.EntityTypes.FindIndex( t => t.ID == m_CurrentMap.SelectedEntityType );
        comboEntityTypes.SelectedIndex = ( idx >= 0 ) ? idx + 1 : 0;
      }
      else if ( ( savedComboIndex >= 0 ) && ( savedComboIndex < comboEntityTypes.Items.Count ) )
      {
        comboEntityTypes.SelectedIndex = savedComboIndex;
      }
      else
      {
        comboEntityTypes.SelectedIndex = 0;
      }
    }

    private void btnAddEntityType_Click( DecentForms.ControlBase Sender )
    {
      string name = editEntityName.Text;
      if ( string.IsNullOrEmpty( name ) )
      {
        name = "Entity " + ( m_MapProject.EntityTypes.Count + 1 );
      }

      var newType = new MapProject.EntityType();
      newType.Name = name;
      newType.ExportSymbol = editEntityExportSymbol.Text ?? "";
      newType.TileIndex = (int)editEntityTileIndex.Value;
      newType.TagID = (int)editEntityTagID.Value;
      newType.ID = 0;
      if ( m_MapProject.EntityTypes.Count > 0 )
      {
        newType.ID = m_MapProject.EntityTypes.Max( t => t.ID ) + 1;
      }
      m_MapProject.EntityTypes.Add( newType );
      RefreshEntityTypes();

      listEntityTypes.SelectedIndex = listEntityTypes.Items.Count - 1;
      SetModified();
    }

    private void btnUpdateEntityType_Click( DecentForms.ControlBase Sender )
    {
      if ( ( listEntityTypes.SelectedIndex < 0 )
      ||   ( listEntityTypes.SelectedIndex >= m_MapProject.EntityTypes.Count ) )
      {
        return;
      }

      var type = m_MapProject.EntityTypes[listEntityTypes.SelectedIndex];
      int newTagID = (int)editEntityTagID.Value;

      // Warn if another entity type already uses this TagID.
      var duplicate = m_MapProject.EntityTypes.FirstOrDefault(
        t => ( t != type ) && ( t.TagID == newTagID ) );
      if ( duplicate != null )
      {
        var result = System.Windows.Forms.MessageBox.Show(
          "Tag ID " + newTagID + " is already used by entity type '" + duplicate.Name + "'.\r\n\r\n"
          + "Runtime code distinguishes entity types by TagID, so duplicates will collide.\r\n\r\n"
          + "Save anyway?",
          "Duplicate Tag ID",
          System.Windows.Forms.MessageBoxButtons.YesNo,
          System.Windows.Forms.MessageBoxIcon.Warning );
        if ( result != System.Windows.Forms.DialogResult.Yes )
        {
          return;
        }
      }

      int savedSelection = listEntityTypes.SelectedIndex;

      type.Name = editEntityName.Text;
      type.ExportSymbol = editEntityExportSymbol.Text ?? "";
      type.TileIndex = (int)editEntityTileIndex.Value;
      type.TagID = newTagID;

      RefreshEntityTypes();
      if ( ( savedSelection >= 0 )
      &&   ( savedSelection < listEntityTypes.Items.Count ) )
      {
        listEntityTypes.SelectedIndex = savedSelection;
      }
      // The Map tab renders entities using the current EntityType.TileIndex,
      // so a change here has to invalidate the cached map image — otherwise
      // switching back to the Map tab shows the entity with its old tile.
      RedrawMap();
      pictureEditor.Invalidate();
      SetModified();
    }

    private void btnDeleteEntityType_Click( DecentForms.ControlBase Sender )
    {
      if ( ( listEntityTypes.SelectedIndex < 0 )
      ||   ( listEntityTypes.SelectedIndex >= m_MapProject.EntityTypes.Count ) )
      {
        return;
      }

      var type = m_MapProject.EntityTypes[listEntityTypes.SelectedIndex];

      // Count instances across all maps.
      int instanceCount = 0;
      int mapsTouched = 0;
      foreach ( var m in m_MapProject.Maps )
      {
        int inThisMap = m.Entities.Count( en => en.Type == type.ID );
        if ( inThisMap > 0 )
        {
          instanceCount += inThisMap;
          ++mapsTouched;
        }
      }

      string message;
      if ( instanceCount == 0 )
      {
        message = "Are you sure you want to delete entity type '" + type.Name + "'?";
      }
      else
      {
        message = "Are you sure you want to delete entity type '" + type.Name + "'?\r\n\r\n"
                + "This will also delete " + instanceCount + " entit"
                + ( instanceCount == 1 ? "y" : "ies" )
                + " of this type across " + mapsTouched + " map"
                + ( mapsTouched == 1 ? "" : "s" ) + ".";
      }

      var confirm = System.Windows.Forms.MessageBox.Show(
        message,
        "Delete entity type",
        System.Windows.Forms.MessageBoxButtons.YesNo,
        System.Windows.Forms.MessageBoxIcon.Warning );
      if ( confirm != System.Windows.Forms.DialogResult.Yes )
      {
        return;
      }

      // Cascade-delete instances first.
      foreach ( var m in m_MapProject.Maps )
      {
        m.Entities.RemoveAll( en => en.Type == type.ID );
        if ( m.SelectedEntityType == type.ID )
        {
          m.SelectedEntityType = -1;
        }
      }
      m_MapProject.EntityTypes.Remove( type );
      RefreshEntityTypes();
      pictureEditor.Invalidate();
      RedrawMap();
      UpdateEntityCountLabel();
      SetModified();
    }

    private void listEntityTypes_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( ( listEntityTypes.SelectedIndex < 0 )
      ||   ( listEntityTypes.SelectedIndex >= m_MapProject.EntityTypes.Count ) )
      {
        btnUpdateEntityType.Enabled = false;
        btnDeleteEntityType.Enabled = false;
        return;
      }
      btnUpdateEntityType.Enabled = true;
      btnDeleteEntityType.Enabled = true;

      var type = m_MapProject.EntityTypes[listEntityTypes.SelectedIndex];
      editEntityName.Text = type.Name;
      editEntityExportSymbol.Text = type.ExportSymbol ?? "";
      editEntityTileIndex.Value = Math.Max( editEntityTileIndex.Minimum,
                                             Math.Min( editEntityTileIndex.Maximum, type.TileIndex ) );
      editEntityTagID.Value = type.TagID;
    }

    private void editEntityExportSymbol_KeyPress( object sender, KeyPressEventArgs e )
    {
      // Restrict to assembler-safe characters only.
      if ( ( !char.IsLetterOrDigit( e.KeyChar ) )
      &&   ( e.KeyChar != '_' )
      &&   ( !char.IsControl( e.KeyChar ) ) )
      {
        e.Handled = true;
      }
    }

    private void btnToolEntity_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolEntity ) ) return;
      m_ToolMode = ToolMode.ENTITY;
      UncheckOtherToolButtons( btnToolEntity );
      AfterToolChange();
    }



    /// <summary>
    /// Activate the per-character "blocked" override tool. Mirrors the
    /// other tool-mode buttons: drop tile selection / floating selection,
    /// flip the radio group, refresh the view. While PASSABLE is active
    /// the PictureEditor_PostPaint overlay tints the map and left-clicks
    /// edit <see cref="MapProject.Map.CharBlockedOverrides"/>.
    /// </summary>
    private void btnToolPassable_CheckedChanged( object sender, EventArgs e )
    {
      if ( KeepActiveIfUnchecking( btnToolPassable ) ) return;
      HideSelection();
      RemoveFloatingSelection();
      m_ToolMode = ToolMode.PASSABLE;
      UncheckOtherToolButtons( btnToolPassable );
      AfterToolChange();
    }

    private void comboEntityTypes_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null ) return;

      int newSelectedEntityType = -1;
      int entityTypeIdx = comboEntityTypes.SelectedIndex - 1;
      if ( ( comboEntityTypes.SelectedIndex > 0 )
      &&   ( entityTypeIdx >= 0 )
      &&   ( entityTypeIdx < m_MapProject.EntityTypes.Count ) )
      {
        newSelectedEntityType = m_MapProject.EntityTypes[entityTypeIdx].ID;
      }
      if ( m_CurrentMap.SelectedEntityType != newSelectedEntityType )
      {
        m_CurrentMap.SelectedEntityType = newSelectedEntityType;
        SetModified();
      }

      // When a specific entity is selected, change the combo = retype that
      // entity. Guarded by m_PopulatingFromSelection so right-clicking to
      // select doesn't immediately rewrite the same type back.
      if ( ( !m_PopulatingFromSelection )
      &&   ( m_SelectedEntity != null )
      &&   ( newSelectedEntityType >= 0 )
      &&   ( m_SelectedEntity.Type != newSelectedEntityType ) )
      {
        DocumentInfo.UndoManager.AddUndoTask(
          new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
        m_SelectedEntity.Type = newSelectedEntityType;
        RedrawMap();
        pictureEditor.Invalidate();
      }

      // Type changed (or selection cleared) → entity-count label needs to
      // reflect the new type's count. Also fires when the selection didn't
      // really change (same type re-picked) — UpdateEntityCountLabel is
      // cheap and idempotent, so the redundant call is harmless.
      UpdateEntityCountLabel();
    }

    private void checkShowEntities_CheckedChanged( object sender, EventArgs e )
    {
      pictureEditor.Invalidate();
      RedrawMap();
    }

    private void UpdateMapAspectRatio()
    {
      if ( ( m_MapProject == null )
      ||   ( pictureEditor == null )
      ||   ( pictureEditor.Parent == null ) )
      {
        return;
      }

      int availableWidth  = pictureEditor.Parent.ClientSize.Width;
      int availableHeight = pictureEditor.Parent.ClientSize.Height;
      if ( ( availableWidth <= 0 )
      ||   ( availableHeight <= 0 ) )
      {
        // Parent not laid out yet (e.g. tab never shown). Leave the current
        // buffer alone; a later resize/zoom re-runs this with real sizes.
        return;
      }

      // --- Integer magnification ---------------------------------------
      // Derive the on-screen scale from the zoom-"intended" base viewport
      // (a MapDisplayBase{Width,Height} screen shrunk by the zoom factor),
      // NOT from the final buffer. The buffer is grown to fill the canvas
      // below, so deriving scale from it would feed back and collapse the
      // magnification to 1x. Using the base viewport here yields exactly
      // the same scale the editor produced before this change, so tiles
      // keep their familiar on-screen size at every zoom level.
      //
      // Integer scale (not fractional) keeps every output pixel the same
      // size: GDI StretchBlt at e.g. 2.5x would make some source pixels 2
      // screen-pixels wide and some 3, showing as uneven tile widths. The
      // cost is a sub-cell letterbox remainder, centered below. Keep-
      // CharacterAspectRatio is intentionally not consulted — integer
      // scale preserves aspect by construction; the toggle stays a no-op
      // only so older project files that wrote it still load.
      float zoomFactor = m_MapZoomPercent / 100.0f;
      int   baseViewCharWidth  = Math.Max( 1, (int)Math.Round( ( MapDisplayBaseWidth  / 8 ) / zoomFactor ) );
      int   baseViewCharHeight = Math.Max( 1, (int)Math.Round( ( MapDisplayBaseHeight / 8 ) / zoomFactor ) );

      int   scale = Math.Max( 1, Math.Min( availableWidth  / ( baseViewCharWidth  * 8 ),
                                           availableHeight / ( baseViewCharHeight * 8 ) ) );

      // --- Viewport buffer: fill the actual canvas at that magnification --
      // Show as many whole character columns/rows as physically fit, so a
      // wide or tall window reveals more of the map instead of cropping it
      // to a fixed MapDisplayBase-sized window. Never go below the zoom-
      // intended size, so we never display LESS than before. The floor
      // (integer divide) guarantees viewChar*8*scale <= available, i.e. the
      // scaled buffer always fits the canvas (only a sub-cell remainder is
      // letterboxed).
      int   viewCharWidth  = Math.Max( baseViewCharWidth,  availableWidth  / ( 8 * scale ) );
      int   viewCharHeight = Math.Max( baseViewCharHeight, availableHeight / ( 8 * scale ) );

      int   displayWidth  = viewCharWidth  * 8;
      int   displayHeight = viewCharHeight * 8;

      bool  bufferChanged = false;
      if ( ( pictureEditor.DisplayPage.Width != displayWidth )
      ||   ( pictureEditor.DisplayPage.Height != displayHeight ) )
      {
        pictureEditor.DisplayPage.Resize( displayWidth, displayHeight );
        // m_Image is a same-size back buffer for incremental tile drawing;
        // it must always match DisplayPage's dimensions.
        m_Image.Create( displayWidth, displayHeight, GR.Drawing.PixelFormat.Format32bppRgb );
        PaletteManager.ApplyPalette( pictureEditor.DisplayPage );
        PaletteManager.ApplyPalette( m_Image );
        bufferChanged = true;
      }

      int finalW = displayWidth  * scale;
      int finalH = displayHeight * scale;

      pictureEditor.Anchor   = System.Windows.Forms.AnchorStyles.None;
      pictureEditor.Size     = new System.Drawing.Size( finalW, finalH );
      pictureEditor.Location = new System.Drawing.Point(
        Math.Max( 0, ( availableWidth  - finalW ) / 2 ),
        Math.Max( 0, ( availableHeight - finalH ) / 2 ) );

      // The viewport's character dimensions drive the scroll ranges, so
      // refresh them whenever the buffer actually changed size (a zoom
      // change or a window resize that revealed/hid columns or rows).
      if ( bufferChanged )
      {
        AdjustScrollbars();
      }
    }

    private void tabEditor_Resize( object sender, EventArgs e )
    {
       // Re-fit the viewport to the new canvas size. UpdateMapAspectRatio
       // re-fills the buffer (and re-runs AdjustScrollbars if the visible
       // column/row count changed), so a wider window immediately reveals
       // more of the map. Always repaint — pictureEditor's size/centering
       // may have shifted even when the buffer dimensions did not.
       UpdateMapAspectRatio();
       RedrawMap();
       pictureEditor.Invalidate();
    }

    private void editSwatchSize_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( ( !char.IsDigit( e.KeyChar ) )
      &&   ( !char.IsControl( e.KeyChar ) ) )
      {
        e.Handled = true;
      }
    }



    private void editSwatchSize_KeyDown( object sender, KeyEventArgs e )
    {
      if ( e.KeyCode == Keys.Enter )
      {
        int size = GR.Convert.ToI32( editSwatchSize.Text );
        if ( size < 4 )
        {
          size = 4;
          editSwatchSize.Text = size.ToString();
        }
        if ( size > 64 )
        {
          size = 64;
          editSwatchSize.Text = size.ToString();
        }
        if ( m_MapProject.ColorSwatchSize != size )
        {
          m_MapProject.ColorSwatchSize = size;
          SetModified();
          RedrawColorChooser();
        }
        e.Handled = true;
        e.SuppressKeyPress = true;
      }
    }
    /// <summary>
    /// Find the lowest unused Group Id ≥ 1 across every tile in the
    /// project and assign it to the currently-edited tile. "Lowest
    /// unused" walks 1, 2, 3… and returns the first integer that no
    /// tile is currently using — so holes (1, 3 → free 2) get filled
    /// before extending the range. The CURRENT tile's existing
    /// GroupId is treated as free so re-clicking the button on a
    /// tile that already has a unique id keeps the same id.
    /// </summary>
    private void btnFindFreeTileGroupId_Click( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      if ( m_CurrentEditedTile == null ) return;
      if ( ( listTileInfo.SelectedIndices == null )
      ||   ( listTileInfo.SelectedIndices.Count == 0 ) ) return;

      // Build the set of GroupIds in use, excluding the current
      // tile's own id (so it can keep its slot if already free).
      var inUse = new System.Collections.Generic.HashSet<int>();
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        var t = m_MapProject.Tiles[i];
        if ( t == m_CurrentEditedTile ) continue;
        inUse.Add( t.GroupId );
      }

      int candidate = 1;
      while ( inUse.Contains( candidate ) ) ++candidate;

      if ( m_CurrentEditedTile.GroupId == candidate )
      {
        // Already at the lowest free id — nothing to do, no undo
        // entry, no dirty flag.
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
      m_CurrentEditedTile.GroupId = candidate;
      editTileGroupId.Text = candidate.ToString();
      SetModified();
    }



    private void btnFindFreeMarkerTagID_Click( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;

      // Build the set of TagIDs in use across all marker types,
      // excluding the currently-selected one so the user can keep its
      // current id if it's already free.
      Formats.MapProject.MarkerType currentType = null;
      if ( ( listMarkerTypes.SelectedIndex >= 0 )
      &&   ( listMarkerTypes.SelectedIndex < m_MapProject.MarkerTypes.Count ) )
      {
        currentType = m_MapProject.MarkerTypes[listMarkerTypes.SelectedIndex];
      }
      var inUse = new System.Collections.Generic.HashSet<int>();
      foreach ( var mt in m_MapProject.MarkerTypes )
      {
        if ( mt == currentType ) continue;
        inUse.Add( mt.TagID );
      }

      // Start at 1 — Tag ID 0 is reserved (typically used as a "no marker"
      // sentinel by the runtime), so the search never assigns it.
      int candidate = 1;
      while ( ( candidate <= 255 ) && inUse.Contains( candidate ) )
      {
        ++candidate;
      }
      if ( candidate > 255 )
      {
        System.Windows.Forms.MessageBox.Show(
          "All Tag IDs from 1 to 255 are already in use. Tag ID 0 is reserved, and the field is exported as a single byte so it cannot exceed 255.",
          "No free Tag ID",
          System.Windows.Forms.MessageBoxButtons.OK,
          System.Windows.Forms.MessageBoxIcon.Warning );
        return;
      }

      // editMarkerTagID is the staging field for Add/Update Type buttons,
      // not a live binding to the selected type — assigning Value here is
      // the right surface to drive. The user clicks Update / Add to commit.
      editMarkerTagID.Value = candidate;
    }



    private void editTileGroupId_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( e.KeyChar == 13 )
      {
        // Enter
        int groupId = GR.Convert.ToI32( editTileGroupId.Text );
        if ( m_CurrentEditedTile.GroupId != groupId )
        {
          DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );
          m_CurrentEditedTile.GroupId = groupId;
          SetModified();
        }
        editTileGroupId.SelectAll();
        e.Handled = true;
      }
      else if ( ( !char.IsDigit( e.KeyChar ) )
      &&        ( !char.IsControl( e.KeyChar ) ) )
      {
        e.Handled = true;
      }
    }

        private void dimSlider_ValueChanged(object sender, EventArgs e)
        {

        }



    /// <summary>
    /// User dragged the grid-opacity slider. Push the value into the map
    /// project so it's saved with the file, then trigger a repaint to
    /// apply it. The grid render path reads the live value off the project,
    /// so no other plumbing is needed.
    /// </summary>
    private void gridOpacitySlider_ValueChanged( object sender, EventArgs e )
    {
      if ( m_MapProject == null ) return;
      if ( m_MapProject.GridOpacity == gridOpacitySlider.Value ) return;
      m_MapProject.GridOpacity = gridOpacitySlider.Value;
      Modified = true;
      pictureEditor.Invalidate();
    }



    /// <summary>
    /// Push the user's tile-list spacing change into StudioSettings,
    /// re-flow comboTiles' ItemHeight, and force a repaint. Settings
    /// persist on the next save (the SETTINGS_MAP_EDITOR chunk picks
    /// up the new value automatically). Guards on Core?.Settings so
    /// the early-construction NumericUpDown.ValueChanged that fires
    /// during Designer setup doesn't NRE.
    /// </summary>
    private void editTileListRowSpacing_ValueChanged( object sender, EventArgs e )
    {
      if ( Core?.Settings == null ) return;
      int v = (int)editTileListRowSpacing.Value;
      if ( v < 0 ) v = 0;
      if ( v > 32 ) v = 32;
      if ( Core.Settings.MapTileListRowSeparatorHeight == v ) return;
      Core.Settings.MapTileListRowSeparatorHeight = v;
      comboTiles.ItemHeight = MapTileListEffectiveItemHeight;
      comboTiles.Invalidate();
    }



    /// <summary>
    /// Open a standard <see cref="ColorDialog"/> seeded with the
    /// current separator color. On OK, persist the new ARGB into
    /// settings, refresh the button's swatch, and invalidate the tile
    /// list so the new color shows up immediately. Cancel = no-op.
    /// </summary>
    private void btnTileListRowSeparatorColor_Click( object sender, EventArgs e )
    {
      if ( Core?.Settings == null ) return;
      uint cur = Core.Settings.MapTileListRowSeparatorColorARGB;
      using ( var dlg = new System.Windows.Forms.ColorDialog() )
      {
        dlg.Color = System.Drawing.Color.FromArgb( unchecked( (int)cur ) );
        dlg.FullOpen = true;
        if ( dlg.ShowDialog( this ) == DialogResult.OK )
        {
          uint argb = (uint)dlg.Color.ToArgb();
          // ColorDialog only edits RGB (alpha = 255 always); preserve
          // the high byte = 0xff so the separator stays opaque.
          argb |= 0xff000000u;
          if ( argb != Core.Settings.MapTileListRowSeparatorColorARGB )
          {
            Core.Settings.MapTileListRowSeparatorColorARGB = argb;
            btnTileListRowSeparatorColor.BackColor = System.Drawing.Color.FromArgb( unchecked( (int)argb ) );
            comboTiles.Invalidate();
          }
        }
      }
    }



    /// <summary>
    /// Alpha-blend a vertical run of grid pixels onto the target buffer.
    /// FastImage.Line / SetPixel don't blend; we read existing RGB, blend
    /// with white at the given alpha (0..255), and write back.
    /// </summary>
    private static void BlendGridSpanVertical(
      GR.Image.FastImage Target, int X, int Y1, int Y2, int Alpha )
    {
      if ( Alpha <= 0 ) return;
      if ( Alpha >= 255 )
      {
        // Fully opaque: skip the blend math, draw direct white.
        Target.Line( X, Y1, X, Y2, 0xffffffff );
        return;
      }
      if ( Y1 > Y2 ) { int t = Y1; Y1 = Y2; Y2 = t; }
      for ( int y = Y1; y <= Y2; ++y )
      {
        Target.SetPixel( X, y, BlendWithWhite( Target.GetPixel( X, y ), Alpha ) );
      }
    }



    private static void BlendGridSpanHorizontal(
      GR.Image.FastImage Target, int X1, int X2, int Y, int Alpha )
    {
      if ( Alpha <= 0 ) return;
      if ( Alpha >= 255 )
      {
        Target.Line( X1, Y, X2, Y, 0xffffffff );
        return;
      }
      if ( X1 > X2 ) { int t = X1; X1 = X2; X2 = t; }
      for ( int x = X1; x <= X2; ++x )
      {
        Target.SetPixel( x, Y, BlendWithWhite( Target.GetPixel( x, Y ), Alpha ) );
      }
    }



    /// <summary>
    /// Standard "src over" blend with white. Alpha is 0..255 (255 = full
    /// white, 0 = unchanged). Returns 0xFF-RGB.
    /// </summary>
    private static uint BlendWithWhite( uint Existing, int Alpha )
    {
      int er = (int)( ( Existing >> 16 ) & 0xff );
      int eg = (int)( ( Existing >> 8 ) & 0xff );
      int eb = (int)( Existing & 0xff );
      int inv = 255 - Alpha;
      int rr = ( 255 * Alpha + er * inv ) / 255;
      int rg = ( 255 * Alpha + eg * inv ) / 255;
      int rb = ( 255 * Alpha + eb * inv ) / 255;
      return 0xff000000u | ( (uint)rr << 16 ) | ( (uint)rg << 8 ) | (uint)rb;
    }



    /// <summary>
    /// Toggle the grid overlay on the Map tab. Wired to the G keyboard
    /// shortcut (gated to the Map tab + non-text-input focus) and called
    /// directly elsewhere if needed. Mirrors the project field, the
    /// checkbox, and forces a repaint in one place.
    /// </summary>
    private void ToggleGridShortcut()
    {
      if ( m_MapProject == null ) return;
      m_MapProject.ShowGrid = !m_MapProject.ShowGrid;
      // Sync the checkbox without re-firing its handler (which would
      // toggle the field again and cancel out our change).
      checkShowGrid.CheckedChanged -= checkShowGrid_CheckedChanged;
      checkShowGrid.Checked = m_MapProject.ShowGrid;
      checkShowGrid.CheckedChanged += checkShowGrid_CheckedChanged;
      Modified = true;
      Redraw();
    }



    /// <summary>
    /// Eats character input on the tile pickers so the OS doesn't run its
    /// "jump to first item starting with this letter" typeahead. Wired to
    /// listTileInfo (Tiles tab) and comboTiles (Map tab) — see the
    /// constructor block where these are hooked up for the rationale.
    /// Setting Handled = true short-circuits the WM_CHAR pipeline before
    /// the control's default search behaviour runs.
    /// </summary>
    private void SuppressTileListTypeahead( object sender, KeyPressEventArgs e )
    {
      e.Handled = true;
    }



    // ====================================================================
    // ===========================  Revisions  ============================
    // ====================================================================
    //
    // The MapEditor lets the user keep an in-project history of named map
    // snapshots (Map.Revisions). The dropdown on the Revisions panel shows
    // "(Current)" plus one entry per saved revision; picking a revision
    // swaps the editor into a strictly read-only view of that snapshot
    // without losing the live map. Buttons let the user create, revert
    // to, or delete revisions. Revisions are persisted with the project
    // file but never exported to the game runtime — only the live map.



    /// <summary>
    /// Current map is editable iff there IS a current map and we aren't
    /// viewing a snapshot. Used as a guard at every modification entry
    /// point. Belt-and-suspenders: <see cref="SetMapEditingControlsEnabled"/>
    /// also disables the major UI controls so the user can't even reach
    /// these paths through the toolbar while viewing.
    /// </summary>
    private bool IsMapEditable
    {
      get { return ( m_CurrentMap != null ) && ( !m_IsViewingRevision ); }
    }



    /// <summary>
    /// Format a revision for the dropdown. Combines the user-visible name
    /// with a compact timestamp so two revisions with the same label can
    /// still be told apart. Invariant culture so the formatting matches
    /// across machines (the timestamp is round-trip-stable in the file).
    /// </summary>
    private string FormatRevisionLabel( Formats.MapProject.MapRevision rev, int Index )
    {
      string label = string.IsNullOrEmpty( rev.Name )
                     ? ( "Revision " + ( Index + 1 ).ToString() )
                     : rev.Name;
      return label + "  (" + rev.CreatedAt.ToString( "yyyy-MM-dd HH:mm",
                              System.Globalization.CultureInfo.InvariantCulture ) + ")";
    }



    /// <summary>
    /// Rebuild comboRevisions from m_LiveMap.Revisions. The first item is
    /// always "(Current)" so the user has a deterministic way back to the
    /// editable map. m_PopulatingRevisionsCombo gates the change handler
    /// during the rebuild — without it the SelectedIndex assignments
    /// would each fire a map-swap mid-rebuild.
    /// </summary>
    private void RefreshRevisionsCombo()
    {
      if ( comboRevisions == null ) return;

      m_PopulatingRevisionsCombo = true;
      try
      {
        comboRevisions.BeginUpdate();
        comboRevisions.Items.Clear();
        comboRevisions.Items.Add( "(Current)" );
        if ( m_LiveMap != null )
        {
          // Newest revision first. Sorting the backing list itself (not just
          // the combo) preserves the item-k -> Revisions[k-1] correspondence
          // that comboRevisions_SelectedIndexChanged, btnRevertRevision and
          // btnDeleteRevision all depend on.
          m_LiveMap.Revisions.Sort( ( a, b ) => b.CreatedAt.CompareTo( a.CreatedAt ) );
          for ( int i = 0; i < m_LiveMap.Revisions.Count; ++i )
          {
            comboRevisions.Items.Add( FormatRevisionLabel( m_LiveMap.Revisions[i], i ) );
          }
        }

        // Reflect the current view: 0 = Current, otherwise i+1 = revision i.
        int desiredIndex = 0;
        if ( m_IsViewingRevision
        &&   m_LiveMap != null
        &&   m_LiveMap.Revisions.Contains( FindRevisionContainingSnapshot( m_CurrentMap ) ) )
        {
          var rev = FindRevisionContainingSnapshot( m_CurrentMap );
          int idx = m_LiveMap.Revisions.IndexOf( rev );
          if ( idx >= 0 ) desiredIndex = idx + 1;
        }
        if ( desiredIndex >= comboRevisions.Items.Count ) desiredIndex = 0;
        comboRevisions.SelectedIndex = desiredIndex;
        comboRevisions.EndUpdate();
      }
      finally
      {
        m_PopulatingRevisionsCombo = false;
      }
      UpdateRevisionButtonsEnabled();
    }



    /// <summary>
    /// Find which revision (if any) holds <paramref name="possibleSnapshot"/>
    /// as its <see cref="Formats.MapProject.MapRevision.Snapshot"/>. Used
    /// during combo refresh to keep the dropdown consistent with the
    /// currently-displayed map.
    /// </summary>
    private Formats.MapProject.MapRevision FindRevisionContainingSnapshot(
      Formats.MapProject.Map possibleSnapshot )
    {
      if ( m_LiveMap == null || possibleSnapshot == null ) return null;
      foreach ( var rev in m_LiveMap.Revisions )
      {
        if ( rev != null && object.ReferenceEquals( rev.Snapshot, possibleSnapshot ) )
        {
          return rev;
        }
      }
      return null;
    }



    /// <summary>
    /// Toggle the major map-modifying controls. Dropdown stays usable so
    /// the user can switch back to "(Current)" or to another revision.
    /// </summary>
    private void SetMapEditingControlsEnabled( bool enabled )
    {
      // Tools panel
      if ( btnShiftLeft != null )            btnShiftLeft.Enabled            = enabled;
      if ( btnShiftRight != null )           btnShiftRight.Enabled           = enabled;
      if ( btnShiftUp != null )              btnShiftUp.Enabled              = enabled;
      if ( btnShiftDown != null )            btnShiftDown.Enabled            = enabled;
      if ( btnMapWidthInc != null )          btnMapWidthInc.Enabled          = enabled;
      if ( btnMapWidthDec != null )          btnMapWidthDec.Enabled          = enabled;
      if ( btnMapHeightInc != null )         btnMapHeightInc.Enabled         = enabled;
      if ( btnMapHeightDec != null )         btnMapHeightDec.Enabled         = enabled;
      if ( btnRemoveOverlappingTiles != null ) btnRemoveOverlappingTiles.Enabled = enabled;

      // Map metadata
      if ( editMapName != null )             editMapName.Enabled             = enabled;
      if ( editMapWidth != null )            editMapWidth.Enabled            = enabled;
      if ( editMapHeight != null )           editMapHeight.Enabled           = enabled;
      if ( editTileSpacingW != null )        editTileSpacingW.Enabled        = enabled;
      if ( editTileSpacingH != null )        editTileSpacingH.Enabled        = enabled;
      if ( comboMapMultiColor1 != null )     comboMapMultiColor1.Enabled     = enabled;
      if ( comboMapMultiColor2 != null )     comboMapMultiColor2.Enabled     = enabled;
      if ( comboMapBGColor != null )         comboMapBGColor.Enabled         = enabled;
      if ( comboMapAlternativeBGColor4 != null ) comboMapAlternativeBGColor4.Enabled = enabled;
      if ( comboMapAlternativeMode != null ) comboMapAlternativeMode.Enabled = enabled;
      // Extra data is now in a dialog opened from Tools — gate the menu
      // item itself so the dialog can't be opened while read-only.
      if ( editExtraDataToolStripMenuItem != null ) editExtraDataToolStripMenuItem.Enabled = enabled;
      if ( btnMapApply != null )             btnMapApply.Enabled             = enabled;

      // Tool selection / placement
      if ( flowLayoutPanel1 != null )        flowLayoutPanel1.Enabled        = enabled;
      // flowLayoutPanel2 holds the entity-side toolbar — entity-type combo,
      // value spinners, the enabled/triggered checkboxes, and the
      // delete-selected-entity button. All of these write back into the
      // current map's entity instances, so they must be locked while
      // viewing a revision.
      if ( flowLayoutPanel2 != null )        flowLayoutPanel2.Enabled        = enabled;
      // flowLayoutPanel4 is the marker-side toolbar (type picker, value /
      // group / link spinners, delete-selected-marker) plus the M tool
      // button. Its controls write back into the current map's markers,
      // so — by the same rule as flowLayoutPanel2 — it must be locked
      // while viewing a revision.
      if ( flowLayoutPanel4 != null )        flowLayoutPanel4.Enabled        = enabled;
      if ( comboTilePlacementColor != null ) comboTilePlacementColor.Enabled = enabled;
      if ( comboTiles != null )              comboTiles.Enabled              = enabled;
      if (clearAllMarkersToolStripMenuItem != null ) clearAllMarkersToolStripMenuItem.Enabled         = enabled;
      if (clearMarkerTypeMenuItem != null ) clearMarkerTypeMenuItem.Enabled      = enabled;
    }



    private void UpdateRevisionButtonsEnabled()
    {
      bool haveLive = ( m_LiveMap != null );
      bool revSelected = ( comboRevisions != null )
                      && ( comboRevisions.SelectedIndex > 0 );
      if ( btnCreateRevision != null ) btnCreateRevision.Enabled = haveLive && !m_IsViewingRevision;
      if ( btnRevertRevision != null ) btnRevertRevision.Enabled = revSelected;
      if ( btnDeleteRevision != null ) btnDeleteRevision.Enabled = revSelected;
    }



    /// <summary>
    /// React to comboRevisions changes: index 0 = view live, index N = view
    /// the (N-1)th revision. We never mutate the snapshot — m_CurrentMap is
    /// just retargeted, and the rest of the editor renders that read-only.
    /// </summary>
    private void comboRevisions_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_PopulatingRevisionsCombo ) return;
      if ( m_LiveMap == null )          return;

      int idx = comboRevisions.SelectedIndex;
      if ( idx <= 0 )
      {
        // Switch back to the live map.
        m_CurrentMap = m_LiveMap;
        m_IsViewingRevision = false;
      }
      else
      {
        int revIdx = idx - 1;
        if ( revIdx < 0 || revIdx >= m_LiveMap.Revisions.Count ) return;
        var snapshot = m_LiveMap.Revisions[revIdx].Snapshot;
        if ( snapshot == null ) return;

        m_CurrentMap = snapshot;
        m_IsViewingRevision = true;
      }

      // Sync everything that renders or gates on m_CurrentMap.
      SetMapEditingControlsEnabled( !m_IsViewingRevision );
      UpdateRevisionButtonsEnabled();

      // Marker/entity selection belongs to whichever map was previously
      // active — clear it so the toolbar doesn't act on a hidden instance.
      ClearMarkerEntitySelection();
      AdjustScrollbars();
      RedrawMap();
      pictureEditor.Invalidate();
    }



    /// <summary>
    /// Snapshot the live map and prepend it to the Revisions list. The
    /// snapshot is a deep copy via <see cref="Formats.MapProject.CloneMap"/>,
    /// so subsequent edits to the live map can't bleed into it. Disabled
    /// while viewing a revision (would snapshot the snapshot).
    /// </summary>
    private void btnCreateRevision_Click( object sender, EventArgs e )
    {
      if ( m_LiveMap == null )      return;
      if ( m_IsViewingRevision )    return;

      var clone = Formats.MapProject.CloneMap( m_LiveMap );
      var rev = new Formats.MapProject.MapRevision();
      rev.CreatedAt = DateTime.Now;
      rev.Name = "Revision " + ( m_LiveMap.Revisions.Count + 1 ).ToString();
      rev.Snapshot = clone;
      m_LiveMap.Revisions.Add( rev );

      RefreshRevisionsCombo();
      SetModified();
    }



    /// <summary>
    /// Replace the live map's content with a deep copy of the selected
    /// revision's snapshot, while preserving the existing revisions list
    /// (so the user keeps their history after a revert). The user gets a
    /// confirmation dialog because this discards any unsaved edits to the
    /// live map.
    /// </summary>
    private void btnRevertRevision_Click( object sender, EventArgs e )
    {
      if ( m_LiveMap == null )         return;
      if ( comboRevisions == null )    return;
      int idx = comboRevisions.SelectedIndex - 1;
      if ( idx < 0 || idx >= m_LiveMap.Revisions.Count ) return;
      var rev = m_LiveMap.Revisions[idx];
      if ( rev == null || rev.Snapshot == null ) return;

      string label = string.IsNullOrEmpty( rev.Name )
                     ? ( "Revision " + ( idx + 1 ).ToString() )
                     : rev.Name;
      var confirm = System.Windows.Forms.MessageBox.Show(
        this,
        "Revert the current map to '" + label + "'?\r\n\r\n"
        + "Any unsaved changes to the current map will be lost. The "
        + "revisions list is preserved.",
        "Confirm revert",
        System.Windows.Forms.MessageBoxButtons.OKCancel,
        System.Windows.Forms.MessageBoxIcon.Warning,
        System.Windows.Forms.MessageBoxDefaultButton.Button2 );
      if ( confirm != System.Windows.Forms.DialogResult.OK ) return;

      // Capture the full pre-revert state for undo BEFORE any field
      // mutation below. UndoMapRevert snapshots every persisted field
      // (size, both override layers, markers, entities, alt colors,
      // etc.) — the revert is too cross-cutting to compose from per-
      // aspect undos like UndoMapSizeChange + UndoMapTilesChange,
      // because those write into rect bounds that may be invalid after
      // a size-changing restore.
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapRevert( this, m_LiveMap ) );

      // In-place field copy so every other reference to m_LiveMap (e.g.
      // m_MapProject.Maps[i] and the comboMaps Tupel) stays valid.
      // Revisions list is intentionally preserved; it's metadata about
      // m_LiveMap, not part of the snapshot's content.
      var fresh = Formats.MapProject.CloneMap( rev.Snapshot );
      m_LiveMap.Tiles                       = fresh.Tiles;
      m_LiveMap.TileColorOverrides          = fresh.TileColorOverrides;
      m_LiveMap.CharBlockedOverrides        = fresh.CharBlockedOverrides;
      m_LiveMap.Name                        = fresh.Name;
      m_LiveMap.TileSpacingX                = fresh.TileSpacingX;
      m_LiveMap.TileSpacingY                = fresh.TileSpacingY;
      m_LiveMap.Markers                     = fresh.Markers;
      m_LiveMap.Entities                    = fresh.Entities;
      m_LiveMap.ExtraDataOld                = fresh.ExtraDataOld;
      m_LiveMap.ExtraDataText               = fresh.ExtraDataText;
      m_LiveMap.AlternativeMultiColor1      = fresh.AlternativeMultiColor1;
      m_LiveMap.AlternativeMultiColor2      = fresh.AlternativeMultiColor2;
      m_LiveMap.AlternativeBackgroundColor  = fresh.AlternativeBackgroundColor;
      m_LiveMap.AlternativeBGColor4         = fresh.AlternativeBGColor4;
      m_LiveMap.SelectedMarkerType          = fresh.SelectedMarkerType;
      m_LiveMap.SelectedEntityType          = fresh.SelectedEntityType;
      m_LiveMap.MarkerDimOpacity            = fresh.MarkerDimOpacity;
      m_LiveMap.AlternativeMode             = fresh.AlternativeMode;

      // Revert always lands the user back on the live (now editable) map.
      m_CurrentMap = m_LiveMap;
      m_IsViewingRevision = false;

      // Resync UI fields that mirror map metadata. This is the same
      // populate sequence comboMaps_SelectedIndexChanged uses; centralising
      // that would be nice but isn't required for v1 of this feature.
      if ( editMapName != null )     editMapName.Text  = m_LiveMap.Name;
      if ( editMapWidth != null )    editMapWidth.Text  = m_LiveMap.Tiles.Width.ToString();
      if ( editMapHeight != null )   editMapHeight.Text = m_LiveMap.Tiles.Height.ToString();
      if ( editTileSpacingW != null ) editTileSpacingW.Text = m_LiveMap.TileSpacingX.ToString();
      if ( editTileSpacingH != null ) editTileSpacingH.Text = m_LiveMap.TileSpacingY.ToString();
      // ExtraDataText is no longer mirrored to a UI textbox — it's
      // visible only via the Tools → Edit extra data... dialog.

      ClearMarkerEntitySelection();
      RecalcTileUsageInCurrentMap();
      SetMapEditingControlsEnabled( true );
      RefreshRevisionsCombo();   // resets dropdown to "(Current)"
      AdjustScrollbars();
      RedrawMap();
      pictureEditor.Invalidate();
      SetModified();
      UpdateMarkerOutOfBoundsLabel();
    }



    private void btnDeleteRevision_Click( object sender, EventArgs e )
    {
      if ( m_LiveMap == null )      return;
      if ( comboRevisions == null ) return;
      int idx = comboRevisions.SelectedIndex - 1;
      if ( idx < 0 || idx >= m_LiveMap.Revisions.Count ) return;
      var rev = m_LiveMap.Revisions[idx];

      string label = string.IsNullOrEmpty( rev.Name )
                     ? ( "Revision " + ( idx + 1 ).ToString() )
                     : rev.Name;
      var confirm = System.Windows.Forms.MessageBox.Show(
        this,
        "Delete revision '" + label + "'?\r\n\r\nThis cannot be undone.",
        "Confirm delete",
        System.Windows.Forms.MessageBoxButtons.OKCancel,
        System.Windows.Forms.MessageBoxIcon.Warning,
        System.Windows.Forms.MessageBoxDefaultButton.Button2 );
      if ( confirm != System.Windows.Forms.DialogResult.OK ) return;

      // If the user is currently viewing this revision, drop them back to
      // the live map first so the snapshot reference becomes detachable.
      bool wasViewing = m_IsViewingRevision
                     && object.ReferenceEquals( m_CurrentMap, rev.Snapshot );
      m_LiveMap.Revisions.RemoveAt( idx );
      if ( wasViewing )
      {
        m_CurrentMap = m_LiveMap;
        m_IsViewingRevision = false;
        SetMapEditingControlsEnabled( true );
        ClearMarkerEntitySelection();
        AdjustScrollbars();
        RedrawMap();
        pictureEditor.Invalidate();
      }
      RefreshRevisionsCombo();
      SetModified();
    }

        private void clearAllMarkersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_CurrentMap == null) return;

            m_CurrentMap.Markers.Clear();
            pictureEditor.Invalidate();
            RedrawMap();
            Modified = true;
            UpdateMarkerOutOfBoundsLabel();
        }

        private void clearMarkerTypeMenuItem_Click(object sender, EventArgs e)
        {
            if (m_CurrentMap == null) return;
            if (m_CurrentMap.SelectedMarkerType == -1) return;

            m_CurrentMap.Markers.RemoveAll(m => m.Type == m_CurrentMap.SelectedMarkerType);
            pictureEditor.Invalidate();
            RedrawMap();
            Modified = true;
            UpdateMarkerOutOfBoundsLabel();
        }

        private void createImageOfMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnCopyImage_Click(sender, e);
        }

    }
} // namespace RetroDevStudio.Documents

