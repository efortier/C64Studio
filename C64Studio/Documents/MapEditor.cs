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
      SELECT,
      MARKER,
      ENTITY
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
    // Finer step for the mouse wheel — the +/- buttons jump in 25% blocks
    // which is great for quick fit-to-view, but the wheel feels smoother with
    // a smaller increment so the user can fine-tune the zoom level.
    private const int                   MapZoomWheelStepPercent = 5;
    private const int                   MapTileListItemHeight = 44;
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

    // Guards against spurious control-change callbacks firing while we are
    // programmatically copying a selected instance's fields INTO the
    // toolbar controls. Without it, the ValueChanged handlers would
    // immediately write those same values back into the instance — cheap
    // but it inflates the undo log and can reorder triggers.
    private bool                        m_PopulatingFromSelection = false;

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
      // KryptonTextBox's scrollbar lives on its inner TextBox, so pass that.
      RetroDevStudio.CustomRenderer.DarkTheme.ApplyDarkScrollBarsTo( comboTiles );
      RetroDevStudio.CustomRenderer.DarkTheme.ApplyDarkScrollBarsTo( editMapExtraData.TextBox );

      // Temporary palette-test dropdown. Lists every Krypton PaletteMode and
      // flips the global palette on selection. Parked in the toolbar for now;
      // can be moved later once we pick the final palette.
      AddPaletteTestCombo();

      // Owner-draw hookup for color combos. Has to happen here (not in the
      // .Designer.cs) because VS's CodeDom serializer can't handle property
      // chains like "control.InnerSubControl.Property = value" — it refuses
      // to load the form designer when InitializeComponent contains them.
      WireOwnerDrawCombo( comboMapBGColor,            comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapMultiColor1,        comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapMultiColor2,        comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboMapAlternativeBGColor4, comboAlternativeColor_DrawItem );
      WireOwnerDrawCombo( comboDesignerBackground,    comboColor_DrawItem );
      WireOwnerDrawCombo( comboMarkerColorOverride,   comboMarkerColorOverride_DrawItem );
      WireOwnerDrawCombo( comboTilePlacementColor,    comboTilePlacementColor_DrawItem );

      characterEditor.Core = Core;

      GR.Image.DPIHandler.ResizeControlsForDPI( this );

      comboTiles.ItemHeight = MapTileListItemHeight;

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

      characterEditor.UndoManager = DocumentInfo.UndoManager;
      characterEditor.Core = Core;
      characterEditor.Modified += CharacterEditor_Modified;
      characterEditor.ShowCreateTileButton = true;
      characterEditor.CreateTileFromCharacter += CharacterEditor_CreateTileFromCharacter;
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
      keepMapCharacterAspectRatioToolStripMenuItem.Click += keepMapCharacterAspectRatioToolStripMenuItem_Click;
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

      comboMapMultiColor1.Items.Add( "From charset" );
      comboMapMultiColor2.Items.Add( "From charset" );
      comboMapBGColor.Items.Add( "Project" );
      comboMapAlternativeBGColor4.Items.Add( "Project" );
      for ( int i = 0; i < 16; ++i )
      {
        comboDesignerBackground.Items.Add( i.ToString( "d2" ) );
        comboTileBackground.Items.Add( i.ToString( "d2" ) );
        comboTileMulticolor1.Items.Add( i.ToString( "d2" ) );
        comboTileMulticolor2.Items.Add( i.ToString( "d2" ) );
        comboTileBGColor4.Items.Add( i.ToString( "d2" ) );
        comboMapMultiColor1.Items.Add( i.ToString( "d2" ) );
        comboMapMultiColor2.Items.Add( i.ToString( "d2" ) );
        comboMapBGColor.Items.Add( i.ToString( "d2" ) );
        comboMapAlternativeBGColor4.Items.Add( i.ToString( "d2" ) );
        comboMarkerColor.Items.Add( i.ToString( "d2" ) );
        comboMarkerColorOverride.Items.Add( i.ToString( "d2" ) );
      }
      comboTileBackground.SelectedIndex = 0;
      comboTileMulticolor1.SelectedIndex = 0;
      comboTileMulticolor2.SelectedIndex = 0;
      comboTileBGColor4.SelectedIndex = 0;
      comboMapMultiColor1.SelectedIndex = 0;
      comboMapMultiColor2.SelectedIndex = 0;
      comboMapMultiColor2.SelectedIndex = 0;
      comboMapBGColor.SelectedIndex = 0;
      comboDesignerBackground.SelectedIndex = 0;
      comboMapAlternativeBGColor4.SelectedIndex = 0;
      comboMarkerColor.SelectedIndex = 0;
      comboMarkerColorOverride.SelectedIndex = 0;

      // "Default" + 16 C64 colors for the tile placement color override.
      // Default index 0 means no override; placing leaves the tile's
      // intrinsic char colors alone.
      RefreshTilePlacementColorCombo();
      comboTilePlacementColor.SelectedIndex = 0;

      comboExportOrientation.SelectedIndex = 0;
      comboExportData.SelectedIndex = 0;
      comboExportData.SelectedIndexChanged += ExportSettingsChanged;
      comboExportOrientation.SelectedIndexChanged += ExportSettingsChanged;
      comboRightClickBehavior.SelectedIndexChanged += comboRightClickBehavior_SelectedIndexChanged;
      comboDesignerBackground.SelectedIndexChanged += comboDesignerBackground_SelectedIndexChanged;

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
      int   baseCharWidth = MapDisplayBaseWidth / 8;
      int   baseCharHeight = MapDisplayBaseHeight / 8;
      float zoomFactor = m_MapZoomPercent / 100.0f;

      int   viewCharWidth = Math.Max( 1, (int)Math.Round( baseCharWidth / zoomFactor ) );
      int   viewCharHeight = Math.Max( 1, (int)Math.Round( baseCharHeight / zoomFactor ) );

      int   displayWidth = viewCharWidth * 8;
      int   displayHeight = viewCharHeight * 8;

      if ( ( pictureEditor.DisplayPage.Width != displayWidth )
      ||   ( pictureEditor.DisplayPage.Height != displayHeight ) )
      {
        pictureEditor.DisplayPage.Resize( displayWidth, displayHeight );
        m_Image.Create( displayWidth, displayHeight, GR.Drawing.PixelFormat.Format32bppRgb );
        PaletteManager.ApplyPalette( pictureEditor.DisplayPage );
        PaletteManager.ApplyPalette( m_Image );
      }

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

      if ( m_MapProject.ShowGrid )
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
        int x1 = offsetX;
        int y1 = offsetY;
        int x2 = Math.Min( offsetX + (int)Math.Ceiling( viewCharWidth / (float)m_CurrentMap.TileSpacingX ), offsetX + m_CurrentMap.Tiles.Width );
        int y2 = Math.Min( offsetY + (int)Math.Ceiling( viewCharHeight / (float)m_CurrentMap.TileSpacingY ), offsetY + m_CurrentMap.Tiles.Height );

        // restrict grid to actual map size
        long    mapPixelWidth = (long)( m_CurrentMap.Tiles.Width - offsetX ) * m_CurrentMap.TileSpacingX * 8;
        long    mapPixelHeight = (long)( m_CurrentMap.Tiles.Height - offsetY ) * m_CurrentMap.TileSpacingY * 8;

        int     targetMapWidth = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( renderOffsetX + (int)mapPixelWidth, sourceWidth, targetWidth ) ) );
        int     targetMapHeight = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( renderOffsetY + (int)mapPixelHeight, sourceHeight, targetHeight ) ) );

        for ( int x = x1; x <= x2; ++x )
        {
          int sourceX = renderOffsetX + ( x - offsetX ) * m_CurrentMap.TileSpacingX * 8;
          int targetX = Math.Max( 0, Math.Min( targetMaxX, ScaleCoordCeil( sourceX, sourceWidth, targetWidth ) ) );
          
          if ( targetX <= targetMapWidth )
          {
            TargetBuffer.Line( targetX, ScaleCoordCeil( renderOffsetY, sourceHeight, targetHeight ),
                               targetX, targetMapHeight,
                               0xffffffff );
          }
        }
        for ( int y = y1; y <= y2; ++y )
        {
          int sourceY = renderOffsetY + ( y - offsetY ) * m_CurrentMap.TileSpacingY * 8;
          int targetY = Math.Max( 0, Math.Min( targetMaxY, ScaleCoordCeil( sourceY, sourceHeight, targetHeight ) ) );

          if ( targetY <= targetMapHeight )
          {
            TargetBuffer.Line( ScaleCoordCeil( renderOffsetX, sourceWidth, targetWidth ), targetY,
                               targetMapWidth, targetY,
                               0xffffffff );
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

      if ( ( m_CurrentMap != null )
      &&   ( m_ToolMode == ToolMode.MARKER ) )
      {
        foreach ( var marker in m_CurrentMap.Markers )
        {
          int sourceX = renderOffsetX + ( marker.X - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8;
          int sourceY = renderOffsetY + ( marker.Y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8;
          int sourceW = m_CurrentMap.TileSpacingX * 8;
          int sourceH = m_CurrentMap.TileSpacingY * 8;
          
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
        // Shared computation: given a map-cell (mx, my) and a footprint in
        // cells (cw × ch), draw a 2-pixel-thick rectangle outline at the
        // corresponding TargetBuffer pixels.
        System.Action<int, int, int, int> drawHighlightAt = ( int mx, int my, int cw, int ch ) =>
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
          // Two nested rectangles for a 2-pixel-thick outline. FastImage's
          // Rectangle draws a 1-pixel border only, so we inset and redraw.
          TargetBuffer.Rectangle( tx,     ty,     tw,     th,     highlightColor );
          if ( ( tw > 2 ) && ( th > 2 ) )
          {
            TargetBuffer.Rectangle( tx + 1, ty + 1, tw - 2, th - 2, highlightColor );
          }
        };

        if ( ( m_SelectedMarker != null )
        &&   ( m_ToolMode == ToolMode.MARKER )
        &&   ( m_CurrentMap.Markers.Contains( m_SelectedMarker ) ) )
        {
          // Markers are point placements — always 1 cell.
          drawHighlightAt( m_SelectedMarker.X, m_SelectedMarker.Y, 1, 1 );
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
          drawHighlightAt( m_SelectedEntity.X, m_SelectedEntity.Y, cw, ch );
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
          drawHighlightAt( m_SelectedTilePos.X, m_SelectedTilePos.Y, cw, ch );
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

      for ( int j = 0; j < m_FloatingSelectionSize.Height; ++j )
      {
        for ( int i = 0; i < m_FloatingSelectionSize.Width; ++i )
        {
          var selectionChar = m_FloatingSelection[i + j * m_FloatingSelectionSize.Width];
          if ( selectionChar.first )
          {
            m_CurrentMap.Tiles[m_MousePos.X + m_CurEditorOffsetX + i, m_MousePos.Y + m_CurEditorOffsetY + j] = selectionChar.second;
            ApplyPlacementColorOverride( m_MousePos.X + m_CurEditorOffsetX + i, m_MousePos.Y + m_CurEditorOffsetY + j );

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
      RecalcTileUsageInCurrentMap();
      Redraw();
      Modified = true;
    }



    private void FillContent( int X, int Y )
    {
      List<System.Drawing.Point>      pointsToCheck = new List<System.Drawing.Point>();

      pointsToCheck.Add( new System.Drawing.Point( X, Y ) );

      int     tileToFill = m_CurrentMap.Tiles[X,Y];
      if ( tileToFill == m_CurrentEditorTile.Index )
      {
        return;
      }

      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );

      while ( pointsToCheck.Count != 0 )
      {
        System.Drawing.Point    point = pointsToCheck[pointsToCheck.Count - 1];
        pointsToCheck.RemoveAt( pointsToCheck.Count - 1 );

        if ( m_CurrentMap.Tiles[point.X, point.Y] != m_CurrentEditorTile.Index )
        {
          DrawTile( point.X, point.Y, m_CurrentEditorTile.Index, m_TilePlacementColorOverride );
          m_CurrentMap.Tiles[point.X, point.Y] = m_CurrentEditorTile.Index;
          ApplyPlacementColorOverride( point.X, point.Y );

          if ( ( point.X > 0 )
          &&   ( m_CurrentMap.Tiles[point.X - 1, point.Y] == tileToFill ) )
          {
            pointsToCheck.Add( new System.Drawing.Point( point.X - 1, point.Y ) );
          }
          if ( ( point.X + 1 < m_CurrentMap.Tiles.Width )
          &&   ( m_CurrentMap.Tiles[point.X + 1, point.Y] == tileToFill ) )
          {
            pointsToCheck.Add( new System.Drawing.Point( point.X + 1, point.Y ) );
          }
          if ( ( point.Y > 0 )
          &&   ( m_CurrentMap.Tiles[point.X, point.Y - 1] == tileToFill ) )
          {
            pointsToCheck.Add( new System.Drawing.Point( point.X, point.Y - 1 ) );
          }
          if ( ( point.Y + 1 < m_CurrentMap.Tiles.Height )
          &&   ( m_CurrentMap.Tiles[point.X, point.Y + 1] == tileToFill ) )
          {
            pointsToCheck.Add( new System.Drawing.Point( point.X, point.Y + 1 ) );
          }
        }
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
        if ( m_FloatingSelection != null )
        {
          if ( m_MouseButtonReleased )
          {
            InsertFloatingSelection();
            m_MouseButtonReleased = false;
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

              if ( ( checkAutoTiling.Checked )
              &&   ( m_CurrentEditorTile.GroupId != 0 ) )
              {
                if ( currentPos == m_LastPaintedPos )
                {
                   // same pos, assume same result
                   return;
                }
                // auto-tiling with group
                // find neighbors
                var neighbors = new List<int>();
                if ( trueX + offsetX > 0 )
                {
                  neighbors.Add( m_CurrentMap.Tiles[trueX + offsetX - 1, trueY + offsetY] );
                }
                if ( trueX + offsetX < m_CurrentMap.Tiles.Width - 1 )
                {
                  neighbors.Add( m_CurrentMap.Tiles[trueX + offsetX + 1, trueY + offsetY] );
                }
                if ( trueY + offsetY > 0 )
                {
                  neighbors.Add( m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY - 1] );
                }
                if ( trueY + offsetY < m_CurrentMap.Tiles.Height - 1 )
                {
                  neighbors.Add( m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY + 1] );
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
                m_LastPaintedPos = currentPos;
              }

              if ( ( m_CurrentMap.Tiles[trueX + offsetX, trueY + offsetY] != tileIndex )
              ||   ( m_CurrentMap.TileColorOverrides[trueX + offsetX, trueY + offsetY] != m_TilePlacementColorOverride ) )
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
                // copy to image cache
                pictureEditor.DisplayPage.DrawTo( m_Image,
                                trueX * 8 * m_CurrentMap.TileSpacingX,
                                trueY * 8 * m_CurrentMap.TileSpacingY,
                                trueX * 8 * m_CurrentMap.TileSpacingX,
                                trueY * 8 * m_CurrentMap.TileSpacingY,
                                m_MapProject.Tiles[tileIndex].Chars.Width * 8,
                                m_MapProject.Tiles[tileIndex].Chars.Height * 8 );

                pictureEditor.Invalidate( new System.Drawing.Rectangle( ( trueX * m_CurrentMap.TileSpacingX ) * 8,
                                                                        ( trueY * m_CurrentMap.TileSpacingY ) * 8,
                                                                        m_CurrentEditorTile.Chars.Width * 8,
                                                                        m_CurrentEditorTile.Chars.Height * 8 ) );
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

               // Markers can live anywhere addressable by an u8 — including
               // outside the map, for global/non-level markers.
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
                   // Unique-per-tile: replace any marker already at this position.
                   var existingMarker = m_CurrentMap.Markers.FirstOrDefault( m => m.X == placeX && m.Y == placeY );
                   if ( existingMarker != null )
                   {
                     existingMarker.Type = type.ID;
                     existingMarker.Name = type.Name + " " + ( m_CurrentMap.Markers.Count + 1 );
                     existingMarker.Value1 = (byte)editMarkerValue1.Value;
                     existingMarker.Value2 = (byte)editMarkerValue2.Value;
                     existingMarker.Enabled = checkMarkerDefaultEnabled.Checked;
                     existingMarker.Triggered = checkMarkerDefaultTriggered.Checked;
                   }
                   else
                   {
                     var marker = new MapProject.Marker();
                     marker.X = placeX;
                     marker.Y = placeY;
                     marker.Type = type.ID;
                     marker.Name = type.Name + " " + ( m_CurrentMap.Markers.Count + 1 );
                     marker.Value1 = (byte)editMarkerValue1.Value;
                     marker.Value2 = (byte)editMarkerValue2.Value;
                     marker.Enabled = checkMarkerDefaultEnabled.Checked;
                     marker.Triggered = checkMarkerDefaultTriggered.Checked;
                     m_CurrentMap.Markers.Add( marker );
                   }
                   RedrawMap();
                   pictureEditor.Invalidate();
                   Modified = true;
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
                 }
               }
             }
             break;
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
           int clickX = trueX + offsetX;
           int clickY = trueY + offsetY;
           if ( ( clickX < 0 )
           ||   ( clickY < 0 )
           ||   ( clickX > 255 )
           ||   ( clickY > 255 ) )
           {
             // outside the 0..255 marker coordinate range — do nothing
           }
           else
           {
             var markerHit = m_CurrentMap.Markers.FirstOrDefault( m => m.X == clickX && m.Y == clickY );
             if ( markerHit != null )
             {
               SelectMarker( markerHit );
             }
             else
             {
               SelectMarker( null );
             }
           }
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
              comboTiles.SelectedIndex = tileIndex;
            }
            // Remember which cell got picked so the user can press Delete
            // to clear it. The PostPaint highlight uses the same field.
            m_SelectedTilePos = new System.Drawing.Point( cellX, cellY );
            pictureEditor.Invalidate();
          }
        }
        else
        {
          // paint with selected tile
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

            pictureEditor.DisplayPage.DrawTo( m_Image,
                            trueX * 8 * m_CurrentMap.TileSpacingX,
                            trueY * 8 * m_CurrentMap.TileSpacingY,
                            trueX * 8 * m_CurrentMap.TileSpacingX,
                            trueY * 8 * m_CurrentMap.TileSpacingY,
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

      for ( int j = 0; j < m_MapProject.Tiles[TileIndex].Chars.Height; ++j )
      {
        for ( int i = 0; i < m_MapProject.Tiles[TileIndex].Chars.Width; ++i )
        {
          // Per-cell color override: when colorOverride >= 0 every char of
          // the tile renders in that single color, matching how the
          // exported color grid will look. Default colorOverride = -1
          // keeps the tile's intrinsic per-character colors.
          byte colorToUse = ( colorOverride >= 0 )
                            ? (byte)colorOverride
                            : m_MapProject.Tiles[TileIndex].Chars[i, j].Color;
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

      int     fillWidth = pictureEditor.DisplayPage.Width;
      int     fillHeight = pictureEditor.DisplayPage.Height;

      if ( m_CurrentMap != null )
      {
        fillWidth = m_CurrentMap.TileSpacingX * 8 * m_CurrentMap.Tiles.Width;
        fillHeight = m_CurrentMap.TileSpacingY * 8 * m_CurrentMap.Tiles.Height;
        if ( ( m_CurrentMap.Tiles.Width - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX < pictureEditor.DisplayPage.Width )
        {
          fillWidth = ( m_CurrentMap.Tiles.Width - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX;
        }
        if ( ( m_CurrentMap.Tiles.Height - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY < pictureEditor.DisplayPage.Height )
        {
          fillHeight = ( m_CurrentMap.Tiles.Height - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY;
        }
      }

      GetMapRenderOffsets( out int renderOffsetX, out int renderOffsetY );

      // clean background
      pictureEditor.DisplayPage.Box( 0, 0, pictureEditor.DisplayPage.Width, pictureEditor.DisplayPage.Height, m_MapProject.Charset.Colors.Palette.ColorValues[m_MapProject.DesignerBackgroundColor] );

      // draw map background
      pictureEditor.DisplayPage.Box( renderOffsetX, renderOffsetY, fillWidth, fillHeight, m_MapProject.Charset.Colors.Palette.ColorValues[bgColor] );

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

      int x1 = offsetX;
      int x2 = offsetX + ( pictureEditor.DisplayPage.Width / ( 8 * m_CurrentMap.TileSpacingX ) );
      int y1 = offsetY;
      int y2 = offsetY + ( pictureEditor.DisplayPage.Height / ( 8 * m_CurrentMap.TileSpacingY ) );

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

            // Per-cell color override pulled from the map's TileColorOverrides
            // layer. -1 = use the tile's own per-character colors (the
            // historical default). Anything 0..15 paints every char of
            // this placement in that single C64 color.
            int cellOverride = -1;
            if ( ( x < m_CurrentMap.TileColorOverrides.Width )
            &&   ( y < m_CurrentMap.TileColorOverrides.Height ) )
            {
              cellOverride = m_CurrentMap.TileColorOverrides[x, y];
            }

            for ( int j = 0; j < tile.Chars.Height; ++j )
            {
              for ( int i = 0; i < tile.Chars.Width; ++i )
              {
                alternativeSettings.CustomColor = ( cellOverride >= 0 )
                                                  ? cellOverride
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
        int dimEndX = Math.Min( pictureEditor.DisplayPage.Width, renderOffsetX + fillWidth );
        int dimEndY = Math.Min( pictureEditor.DisplayPage.Height, renderOffsetY + fillHeight );
        for ( int y = Math.Max( 0, renderOffsetY ); y < dimEndY; ++y )
        {
          for ( int x = Math.Max( 0, renderOffsetX ); x < dimEndX; ++x )
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

      int index = 0;
      comboMaps.BeginUpdate();
      foreach ( var map in m_MapProject.Maps )
      {
        comboMaps.Items.Add( new GR.Generic.Tupel<string, Formats.MapProject.Map>( index.ToString() + ": " + map.Name, map ) );
        comboMaps.Enabled = true;
        ++index;
      }
      comboMaps.EndUpdate();


      comboTileBackground.SelectedIndex   = m_MapProject.BackgroundColor;
      if ( ( m_MapProject.DesignerBackgroundColor >= 0 )
      &&   ( m_MapProject.DesignerBackgroundColor < 16 ) )
      {
        comboDesignerBackground.SelectedIndex = m_MapProject.DesignerBackgroundColor;
      }
      else
      {
        comboDesignerBackground.SelectedIndex = 0;
      }
      comboTileMulticolor1.SelectedIndex = m_MapProject.MultiColor1;
      comboTileMulticolor2.SelectedIndex = m_MapProject.MultiColor2;
      comboTileBGColor4.SelectedIndex = m_MapProject.BGColor4;
      comboMapProjectMode.SelectedIndex = (int)m_MapProject.Mode;
      checkShowGrid.Checked = m_MapProject.ShowGrid;
      keepMapCharacterAspectRatioToolStripMenuItem.Checked = m_MapProject.KeepCharacterAspectRatio;
      UpdateMapAspectRatio();
      ApplyExportSettingsToUI();

      RedrawMap();
      RedrawColorChooser();
      RedrawColorChooser();
      characterEditor.CharsetUpdated( m_MapProject.Charset );
      characterEditor.CharactersPerRow = m_MapProject.CharactersPerRow;
      characterEditor.EditorMode = m_MapProject.CharacterEditorMode;
      characterEditor.SwatchSize = m_MapProject.ColorSwatchSize;
      Modified = false;
      if ( string.IsNullOrEmpty( DocumentInfo.DocumentFilename ) )
      {
        DocumentInfo.DocumentFilename = File;
      }

      if ( ( comboMaps.Items.Count > 0 )
      &&   ( comboMaps.SelectedIndex == -1 ) )
      {
        comboMaps.SelectedIndex = 0;
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
      int listCharIndex = -1;
      if ( listTileChars.SelectedIndices.Count > 0 )
      {
        listCharIndex = listTileChars.SelectedIndices[0];
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
        comboTiles.ItemHeight = MapTileListItemHeight;
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

        if ( ( listCharIndex >= 0 )
        &&   ( listCharIndex < listTileChars.Items.Count ) )
        {
          listTileChars.SelectedIndices.Clear();
          listTileChars.SelectedIndices.Add( listCharIndex );
          listTileChars.EnsureVisible( listCharIndex );
        }
      }
      comboTiles.Invalidate();
      RefreshMarkerTypes();
      RefreshEntityTypes();
      RefreshEntityTileIndexRange();
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

    private void comboDesignerBackground_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( m_MapProject.DesignerBackgroundColor != comboDesignerBackground.SelectedIndex )
      {
        m_MapProject.DesignerBackgroundColor = comboDesignerBackground.SelectedIndex;
        RedrawMap();
        Modified = true;
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
      ShiftMap( -1, 0 );
    }

    private void btnShiftRight_Click( object sender, EventArgs e )
    {
      ShiftMap( 1, 0 );
    }

    private void btnShiftUp_Click( object sender, EventArgs e )
    {
      ShiftMap( 0, -1 );
    }

    private void btnShiftDown_Click( object sender, EventArgs e )
    {
      ShiftMap( 0, 1 );
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
    /// <summary>
    /// Drops a KryptonComboBox at the bottom of the Map Controls panel listing
    /// every Krypton PaletteMode. Selecting one flips GlobalPaletteMode live so
    /// we can compare looks without rebuilding. Temporary scaffolding — slated
    /// for removal once the palette choice is finalized.
    /// </summary>
    private void AddPaletteTestCombo()
    {
      var label = new System.Windows.Forms.Label
      {
        Text = "Palette tester (TEMP):",
        AutoSize = true,
        Location = new System.Drawing.Point( 10, groupBox1.Height - 48 ),
        Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left,
        ForeColor = RetroDevStudio.CustomRenderer.DarkTheme.StatusWarn,
        Name = "labelPaletteTest",
      };
      groupBox1.Controls.Add( label );
      label.BringToFront();

      int comboWidth = System.Math.Max( 200, groupBox1.ClientSize.Width - 20 );
      var combo = new Krypton.Toolkit.KryptonComboBox
      {
        DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
        Size = new System.Drawing.Size( comboWidth, 24 ),
        Location = new System.Drawing.Point( 10, groupBox1.Height - 30 ),
        Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right,
        Name = "comboPaletteTest",
      };
      foreach ( Krypton.Toolkit.PaletteMode mode in System.Enum.GetValues( typeof( Krypton.Toolkit.PaletteMode ) ) )
      {
        combo.Items.Add( mode );
      }
      combo.SelectedItem = Krypton.Toolkit.PaletteMode.Office2010BlackDarkMode;
      combo.SelectedIndexChanged += ( s, e ) =>
      {
        if ( combo.SelectedItem is Krypton.Toolkit.PaletteMode picked )
        {
          // GlobalPaletteMode is an instance property; any KryptonManager
          // instance writes the shared global state.
          new Krypton.Toolkit.KryptonManager().GlobalPaletteMode = picked;
        }
      };
      groupBox1.Controls.Add( combo );
      combo.BringToFront();
      RetroDevStudio.CustomRenderer.DarkTheme.StyleDisabledComboDark( combo );
    }

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

    private void ShiftMap( int DX, int DY )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }
      // Snapshot tiles and markers in one undo group so Ctrl+Z rewinds both at once.
      DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTilesChange( this, m_CurrentMap, 0, 0, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      DocumentInfo.UndoManager.AddGroupedUndoTask( new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );

      int    w = m_CurrentMap.Tiles.Width;
      int    h = m_CurrentMap.Tiles.Height;

      if ( DX > 0 )
      {
        for ( int x = w - 1; x >= DX; --x )
        {
          for ( int y = 0; y < h; ++y )
          {
            m_CurrentMap.Tiles[x, y] = m_CurrentMap.Tiles[x - DX, y];
          }
        }
        for ( int x = 0; x < DX; ++x )
        {
           for ( int y = 0; y < h; ++y )
           {
             m_CurrentMap.Tiles[x, y] = 0; 
           }
        }
      }
      else if ( DX < 0 )
      {
         int absDX = -DX;
         for ( int x = 0; x < w - absDX; ++x )
         {
           for ( int y = 0; y < h; ++y )
           {
             m_CurrentMap.Tiles[x, y] = m_CurrentMap.Tiles[x + absDX, y];
           }
         }
         for ( int x = w - absDX; x < w; ++x )
         {
            for ( int y = 0; y < h; ++y )
            {
              m_CurrentMap.Tiles[x, y] = 0;
            }
         }
      }
      
      if ( DY > 0 )
      {
        for ( int y = h - 1; y >= DY; --y )
        {
          for ( int x = 0; x < w; ++x )
          {
            m_CurrentMap.Tiles[x, y] = m_CurrentMap.Tiles[x, y - DY];
          }
        }
        for ( int y = 0; y < DY; ++y )
        {
           for ( int x = 0; x < w; ++x )
           {
             m_CurrentMap.Tiles[x, y] = 0;
           }
        }
      }
      else if ( DY < 0 )
      {
         int absDY = -DY;
         for ( int y = 0; y < h - absDY; ++y )
         {
           for ( int x = 0; x < w; ++x )
           {
             m_CurrentMap.Tiles[x, y] = m_CurrentMap.Tiles[x, y + absDY];
           }
         }
         for ( int y = h - absDY; y < h; ++y )
         {
            for ( int x = 0; x < w; ++x )
            {
              m_CurrentMap.Tiles[x, y] = 0;
            }
         }
      }

      // Shift markers that were placed inside the map area. Off-map markers
      // (global / non-level meta-markers) are left alone. A marker whose new
      // position leaves the 0..255 u8 range is dropped — same semantics as a
      // tile that shifts off the edge.
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
        if ( ( newX < 0 ) || ( newX > 255 ) || ( newY < 0 ) || ( newY > 255 ) )
        {
          // shifted off the addressable range — drop
          continue;
        }
        marker.X = newX;
        marker.Y = newY;
        shiftedMarkers.Add( marker );
      }
      m_CurrentMap.Markers = shiftedMarkers;

      // Shift entities too. Entities are strictly in-map, so anything that
      // shifts out of the map bounds is dropped.
      var shiftedEntities = new List<MapProject.Entity>();
      foreach ( var entity in m_CurrentMap.Entities )
      {
        int newX = entity.X + DX;
        int newY = entity.Y + DY;
        if ( ( newX < 0 ) || ( newY < 0 )
        ||   ( newX >= w ) || ( newY >= h ) )
        {
          continue;
        }
        entity.X = newX;
        entity.Y = newY;
        shiftedEntities.Add( entity );
      }
      m_CurrentMap.Entities = shiftedEntities;

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

          if ( ( m_CurrentTileChar != null )
          &&   ( m_CurrentTileChar.Color != m_CurrentColor )
          &&   ( listTileInfo.SelectedIndices.Count > 0 )
          &&   ( listTileChars.SelectedItems.Count > 0 ) )
          {
            DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapTileModified( this, m_MapProject, listTileInfo.SelectedIndices[0] ) );

          m_CurrentTileChar.Color = m_CurrentColor;

          listTileChars.SelectedItems[0].SubItems[2].Text = m_CurrentColor.ToString();
          RedrawTile();
          RedrawMap();
          SetModified();
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

      if ( m_CurrentMap.TileSpacingX * m_CurrentMap.Tiles.Width <= viewCharWidth )
      {
        mapHScroll.Maximum = 0;
        mapHScroll.Enabled = false;
        m_CurEditorOffsetX = 0;
      }
      else
      {
        mapHScroll.Maximum = ( m_CurrentMap.TileSpacingX * m_CurrentMap.Tiles.Width - viewCharWidth ) / m_CurrentMap.TileSpacingX + 1;
        mapHScroll.Enabled = true;
      }
      if ( m_CurEditorOffsetX > mapHScroll.Maximum )
      {
        m_CurEditorOffsetX = mapHScroll.Maximum;
      }

      mapVScroll.Minimum = 0;
      if ( m_CurrentMap.TileSpacingY * m_CurrentMap.Tiles.Height <= viewCharHeight )
      {
        mapVScroll.Maximum = 0;
        mapVScroll.Enabled = false;
        m_CurEditorOffsetY = 0;
      }
      else
      {
        mapVScroll.Maximum = ( m_CurrentMap.TileSpacingY * m_CurrentMap.Tiles.Height - viewCharHeight ) / m_CurrentMap.TileSpacingY + 1;
        mapVScroll.Enabled = true;
      }
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

      m_CurrentMap = null;

      btnMapApply.Enabled = ( comboMaps.SelectedIndex != -1 );
      btnMapDelete.Enabled = ( comboMaps.SelectedIndex != -1 );

      if ( comboMaps.SelectedIndex == -1 )
      {
        comboTiles.Items.Clear();
        btnCopy.Enabled = false;
        btnMoveMapDown.Enabled = false;
        btnMoveMapUp.Enabled = false;
        return;
      }
      m_CurrentMap = ( (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.SelectedItem ).second;
      btnCopy.Enabled = true;

      btnMoveMapDown.Enabled  = ( ( comboMaps.Items.Count >= 2 ) && ( comboMaps.SelectedIndex + 1 < comboMaps.Items.Count ) );
      btnMoveMapUp.Enabled    = ( ( comboMaps.Items.Count >= 2 ) && ( comboMaps.SelectedIndex > 0 ) );

      m_SelectedTiles = new bool[m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height];

      editMapName.Text = m_CurrentMap.Name;
      editTileSpacingW.Text = m_CurrentMap.TileSpacingX.ToString();
      editTileSpacingH.Text = m_CurrentMap.TileSpacingY.ToString();
      editMapWidth.Text = m_CurrentMap.Tiles.Width.ToString();
      editMapHeight.Text = m_CurrentMap.Tiles.Height.ToString();
      comboTiles.ItemHeight = MapTileListItemHeight;
      //editMapExtraData.Text = FormatExtraData( m_CurrentMap.ExtraData );
      editMapExtraData.Text = m_CurrentMap.ExtraDataText;
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
      RecalcTileUsageInCurrentMap();

      listTileInfo.BeginUpdate();
      foreach ( ListViewItem item in listTileInfo.Items )
      {
        Formats.MapProject.Tile tile = (Formats.MapProject.Tile)item.Tag;
        if ( ( tile.Index >= 0 )
        &&   ( tile.Index < _TileUsage.Count ) )
        {
          // SubItem 4 = "Used" column (after the new Preview column at 2
          // shifted Size and Used down to 3 and 4 respectively).
          item.SubItems[4].Text = _TileUsage[tile.Index].ToString();
        }
      }
      listTileInfo.EndUpdate();
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
      // Keep the per-cell color-override layer the same shape as Tiles
      // and initialize it to "no override" everywhere. -1 means the cell
      // renders/exports using the placed tile's intrinsic colors.
      map.TileColorOverrides.Resize( w, h );
      ResetColorOverrides( map.TileColorOverrides );
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

      int   mapIndex = MapIndex;
      comboMaps.Items.Insert( MapIndex, new GR.Generic.Tupel<string, Formats.MapProject.Map>( mapIndex.ToString() + ": " + Map.Name, Map ) );
      comboMaps.Enabled = true;

      for ( int i = 0; i < comboMaps.Items.Count; ++i )
      {
        GR.Generic.Tupel<string, Formats.MapProject.Map>    mapPair = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[i];

        mapPair.first = i.ToString() + ": " + mapPair.second.Name;

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
      bool  sizeChanged = false;
      if ( ( w != m_CurrentMap.Tiles.Width )
      ||   ( h != m_CurrentMap.Tiles.Height ) )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapSizeChange( this, m_CurrentMap, m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height ) );
        firstUndo = false;
        sizeChanged = true;
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
        }
      }

      m_CurrentMap.Tiles.Resize( w, h );
      // Keep TileColorOverrides shape in sync with Tiles. Layer.Resize
      // preserves existing cell values within the overlap; new cells get
      // the layer's default-int (0), but we explicitly set them to -1
      // (no override) to match the rest of the layer's semantics.
      ResizeColorOverridesPreservingDefaults( m_CurrentMap.TileColorOverrides, w, h );
      m_CurrentMap.Name = editMapName.Text;

      m_SelectedTiles = new bool[w, h];

      // update name in combo
      int index = 0;
      foreach ( GR.Generic.Tupel<string, Formats.MapProject.Map> mapInfo in comboMaps.Items )
      {
        if ( mapInfo.second == m_CurrentMap )
        {
          mapInfo.first = index.ToString() + ": " + m_CurrentMap.Name;
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
      if ( Core?.Theming != null )
        Core.Theming.DrawThemedBackground( e, comboTiles );
      else
        e.DrawBackground();
      if ( ( e.Index < 0 )
      ||   ( e.Index >= comboTiles.Items.Count ) )
      {
        e.DrawFocusRectangle();
        return;
      }

      var tileInfo = (GR.Generic.Tupel<string, Formats.MapProject.Tile>)comboTiles.Items[e.Index];
      Formats.MapProject.Tile tile = tileInfo.second;
      if ( tile == null )
      {
        e.DrawFocusRectangle();
        return;
      }

      int previewPadding = MapTilePreviewPadding;
      int previewSize = Math.Max( 1, e.Bounds.Height - previewPadding * 2 );
      System.Drawing.Rectangle previewRect = new System.Drawing.Rectangle( e.Bounds.Left + previewPadding,
                                                                           e.Bounds.Top + ( e.Bounds.Height - previewSize ) / 2,
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
      int textY = e.Bounds.Top + ( e.Bounds.Height - comboTiles.Font.Height ) / 2;
      System.Drawing.Brush textBrush = new System.Drawing.SolidBrush( comboTiles.ForeColor );
      e.Graphics.DrawString( label, comboTiles.Font, textBrush, textX, textY );
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
      // remove from all maps
      foreach ( var map in m_MapProject.Maps )
      {
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
            }
          }
        }
      }

      m_MapProject.Tiles.RemoveAt( TileIndex );
      for ( int i = 0; i < m_MapProject.Tiles.Count; ++i )
      {
        m_MapProject.Tiles[i].Index = i;
      }
      listTileInfo.Items.RemoveAt( TileIndex );
      for ( int i = TileIndex; i < listTileInfo.Items.Count; ++i )
      {
        listTileInfo.Items[i].Text = i.ToString();
      }
      listTileInfo_SelectedIndexChanged( null, null );
      comboTiles.Items.RemoveAt( TileIndex );
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



    private void listTileInfo_ItemDrag( object sender, ItemDragEventArgs e )
    {
      listTileInfo.DoDragDrop( e.Item, DragDropEffects.Move );
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
    }



    private void listTileInfo_DragDrop( object sender, DragEventArgs e )
    {
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



    private void editMapExtraData_TextChanged( object sender, EventArgs e )
    {
      if ( m_CurrentMap == null )
      {
        return;
      }
      if ( editMapExtraData.Text != m_CurrentMap.ExtraDataText )
      {
        DocumentInfo.UndoManager.AddUndoTask( new Undo.UndoMapValueChange( this, m_CurrentMap ) );  

        Modified = true;
        m_CurrentMap.ExtraDataText = editMapExtraData.Text;
      }
    }



    private void editMapExtraData_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( ( System.Windows.Forms.Control.ModifierKeys == Keys.Control )
      &&   ( e.KeyChar == 1 ) )
      {
        editMapExtraData.SelectAll();
        e.Handled = true;
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
      map.TileColorOverrides.Resize( cpProject.MapWidth, cpProject.MapHeight );
      ResetColorOverrides( map.TileColorOverrides );
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



    private void RemoveFloatingSelection()
    {
      if ( m_FloatingSelection != null )
      {
        m_FloatingSelection = null;
        Redraw();
      }
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
          btnToolSelect, btnToolMarker, btnToolEntity,
        };
        foreach ( var b in buttons )
        {
          if ( ( b != keeper ) && b.Checked )
          {
            b.Checked = false;
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
      // Color override layer is per-cell, so it duplicates alongside Tiles.
      // The new map should look identical to the source when the user
      // duplicates — including any per-cell color tweaks they made.
      newMap.TileColorOverrides = new GR.Game.Layer<int>();
      newMap.TileColorOverrides.Resize( m_CurrentMap.Tiles.Width, m_CurrentMap.Tiles.Height );
      for ( int i = 0; i < m_CurrentMap.Tiles.Width; ++i )
      {
        for ( int j = 0; j < m_CurrentMap.Tiles.Height; ++j )
        {
          newMap.Tiles[i,j] =  m_CurrentMap.Tiles[i,j];
          int srcOverride = ( ( i < m_CurrentMap.TileColorOverrides.Width )
                              && ( j < m_CurrentMap.TileColorOverrides.Height ) )
                            ? m_CurrentMap.TileColorOverrides[i,j] : -1;
          newMap.TileColorOverrides[i,j] = srcOverride;
        }
      }
      newMap.TileSpacingX = m_CurrentMap.TileSpacingX;
      newMap.TileSpacingY = m_CurrentMap.TileSpacingY;
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
      for ( int i = 0; i < Width; ++i )
      {
        for ( int j = 0; j < Height; ++j )
        {
          DrawTile( X + i - m_CurEditorOffsetX, Y + j - m_CurEditorOffsetY, m_CurrentMap.Tiles[X + i, Y + j] );
        }
      }
      pictureEditor.DisplayPage.DrawTo( m_Image,
                      ( X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                      ( Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                      ( X - m_CurEditorOffsetX ) * 8 * m_CurrentMap.TileSpacingX,
                      ( Y - m_CurEditorOffsetY ) * 8 * m_CurrentMap.TileSpacingY,
                      Width * 8 * m_CurrentMap.TileSpacingX, Height * 8 * m_CurrentMap.TileSpacingY );

      pictureEditor.Invalidate( new System.Drawing.Rectangle( ( X - m_CurEditorOffsetX ) * m_CurrentMap.TileSpacingX * 8,
                                                              ( Y - m_CurEditorOffsetY ) * m_CurrentMap.TileSpacingY * 8,
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

        for ( int i = 0; i < comboMaps.Items.Count; ++i )
        {
          GR.Generic.Tupel<string, Formats.MapProject.Map>    mapPair = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[i];

          mapPair.first = i.ToString() + ": " + mapPair.second.Name;

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
      Redraw();
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

          // Honor the per-cell color override for the exported image too,
          // so "Copy map to clipboard as image" produces what the editor
          // shows on screen.
          int cellOverrideCpy = -1;
          if ( ( x < m_CurrentMap.TileColorOverrides.Width )
          &&   ( y < m_CurrentMap.TileColorOverrides.Height ) )
          {
            cellOverrideCpy = m_CurrentMap.TileColorOverrides[x, y];
          }
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              alternativeSettings.CustomColor = ( cellOverrideCpy >= 0 )
                                                ? cellOverrideCpy
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


      var item1 = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[MapIndex1];
      var item2 = (GR.Generic.Tupel<string, Formats.MapProject.Map>)comboMaps.Items[MapIndex2];

      item1.first = MapIndex1.ToString() + ": " + item1.second.Name;
      item2.first = MapIndex2.ToString() + ": " + item2.second.Name;

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
      var exportInfo = new ExportMapInfo()
      {
        Map             = m_MapProject,
        RowByRow        = ( comboExportOrientation.SelectedIndex == 0 ),
        ExportType      = (MapExportType)comboExportData.SelectedIndex,
        SelectedTiles   = m_SelectedTiles,
        CurrentMap      = m_CurrentMap
      };

      editDataExport.Text = "";
      m_ExportForm.HandleExport( exportInfo, editDataExport, DocumentInfo );
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
        if ( !FocusSupport.IsFocusOnChildOfAndCouldAffectReason( tabEditor, FocusSupport.FocusControlReason.ESCAPE ) )
        {
          RemoveFloatingSelection();
          return true;
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
          if ( TryDeleteRightClickedTile() )
          {
            return true;
          }
        }
      }
      return base.ProcessCmdKey( ref msg, keyData );
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
      if ( m_CurrentMap == null ) return false;
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
      // Clearing a cell to "empty" means dropping any per-cell color
      // override along with the tile — otherwise the override would
      // silently linger and tint whatever the user paints over the
      // empty cell next.
      if ( ( x < m_CurrentMap.TileColorOverrides.Width )
      &&   ( y < m_CurrentMap.TileColorOverrides.Height ) )
      {
        m_CurrentMap.TileColorOverrides[x, y] = -1;
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
       editMarkerTagID.Value = type.TagID;
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

      var type = m_MapProject.MarkerTypes.FirstOrDefault( t => t.ID == m_SelectedMarker.Type );
      string typeName = ( type != null ) ? type.Name : "(unknown)";
      var result = System.Windows.Forms.MessageBox.Show(
        "Delete the selected marker at (" + m_SelectedMarker.X + ", " + m_SelectedMarker.Y
          + ") of type '" + typeName + "'?",
        "Delete marker",
        System.Windows.Forms.MessageBoxButtons.YesNo,
        System.Windows.Forms.MessageBoxIcon.Warning );
      if ( result != System.Windows.Forms.DialogResult.Yes ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapMarkersChange( this, m_CurrentMap ) );
      m_CurrentMap.Markers.Remove( m_SelectedMarker );
      SelectMarker( null );
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
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

      var type = m_MapProject.EntityTypes.FirstOrDefault( t => t.ID == m_SelectedEntity.Type );
      string typeName = ( type != null ) ? type.Name : "(unknown)";
      var result = System.Windows.Forms.MessageBox.Show(
        "Delete the selected entity at (" + m_SelectedEntity.X + ", " + m_SelectedEntity.Y
          + ") of type '" + typeName + "'?",
        "Delete entity",
        System.Windows.Forms.MessageBoxButtons.YesNo,
        System.Windows.Forms.MessageBoxIcon.Warning );
      if ( result != System.Windows.Forms.DialogResult.Yes ) return;

      DocumentInfo.UndoManager.AddUndoTask(
        new Undo.UndoMapEntitiesChange( this, m_CurrentMap ) );
      m_CurrentMap.Entities.Remove( m_SelectedEntity );
      SelectEntity( null );
      SetModified();
      RedrawMap();
      pictureEditor.Invalidate();
    }

    private void btnClearMarkers_Click( object sender, EventArgs e )
    {
       if ( m_CurrentMap == null ) return;
       
       m_CurrentMap.Markers.Clear();
       pictureEditor.Invalidate();
       RedrawMap();
       Modified = true;
    }

    private void btnClearMarkerType_Click( object sender, EventArgs e )
    {
       if ( m_CurrentMap == null ) return;
       if ( m_CurrentMap.SelectedMarkerType == -1 ) return;
       
       m_CurrentMap.Markers.RemoveAll( m => m.Type == m_CurrentMap.SelectedMarkerType );
       pictureEditor.Invalidate();
       RedrawMap();
       Modified = true;
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
    
    private void UpdateMarkerControlsState()
    {
       bool enabled = btnToolMarker.Checked;
       comboMarkerTypes.Enabled = enabled;
       comboMarkerColorOverride.Enabled = enabled;
       btnClearMarkers.Enabled = enabled;
       btnClearMarkerType.Enabled = enabled;
       // The dim slider is shared between marker and entity placement modes.
       dimSlider.Enabled = enabled || btnToolEntity.Checked;
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
    }



    /// <summary>
    /// Clear marker, entity, AND tile-cursor selection and refresh the
    /// delete buttons. Used on map change and tool change; separated out
    /// because it's repeated in several spots.
    /// </summary>
    private void ClearMarkerEntitySelection()
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
          checkMarkerDefaultEnabled.Checked = marker.Enabled;
          checkMarkerDefaultTriggered.Checked = marker.Triggered;

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
        // "Default" row — no swatch, just the literal label.
        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( "Default", combo.Font, brush,
                                 e.Bounds.X + 4, e.Bounds.Y + 3 );
        }
      }
      else
      {
        // Color rows: combo index 1..16 → palette index 0..15. Draw a
        // wide-ish swatch with a thin black border, leaving room on the
        // right for the numeric "00".."15" label so the user can read the
        // index alongside the color.
        int colorIndex = e.Index - 1;
        uint color = m_MapProject.Charset.Colors.Palette.ColorValues[colorIndex];

        int swatchW = e.Bounds.Height - 4;
        using ( var brush = new System.Drawing.SolidBrush( System.Drawing.Color.FromArgb( (int)color ) ) )
        {
          e.Graphics.FillRectangle( brush, e.Bounds.X + 2, e.Bounds.Y + 2, swatchW, e.Bounds.Height - 4 );
        }
        e.Graphics.DrawRectangle( System.Drawing.Pens.Black, e.Bounds.X + 2, e.Bounds.Y + 2, swatchW, e.Bounds.Height - 5 );
        using ( var brush = new System.Drawing.SolidBrush( combo.ForeColor ) )
        {
          e.Graphics.DrawString( colorIndex.ToString( "00" ), combo.Font, brush,
                                 e.Bounds.X + swatchW + 6, e.Bounds.Y + 3 );
        }
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
    }



    /// <summary>
    /// Stamp the current placement color override into a single map cell's
    /// TileColorOverrides slot. Called by every tile-placement code path
    /// right after writing the tile index, so the per-cell override always
    /// reflects whatever the toolbar combo had selected at the moment of
    /// placement (-1 = leave the tile's intrinsic colors alone, 0..15 =
    /// paint everything in this single C64 color).
    /// </summary>
    private void ApplyPlacementColorOverride( int cellX, int cellY )
    {
      if ( m_CurrentMap == null ) return;
      if ( ( cellX < 0 )
      ||   ( cellY < 0 )
      ||   ( cellX >= m_CurrentMap.TileColorOverrides.Width )
      ||   ( cellY >= m_CurrentMap.TileColorOverrides.Height ) )
      {
        return;
      }
      m_CurrentMap.TileColorOverrides[cellX, cellY] = m_TilePlacementColorOverride;
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
    }

    private void checkShowEntities_CheckedChanged( object sender, EventArgs e )
    {
      pictureEditor.Invalidate();
      RedrawMap();
    }

    private void keepMapCharacterAspectRatioToolStripMenuItem_Click( object sender, EventArgs e )
    {
      m_MapProject.KeepCharacterAspectRatio = keepMapCharacterAspectRatioToolStripMenuItem.Checked;
      Modified = true;
      UpdateMapAspectRatio();
    }

    private void UpdateMapAspectRatio()
    {
      if ( ( m_MapProject == null )
      ||   ( pictureEditor == null ) )
      {
        return;
      }
      if ( m_MapProject.KeepCharacterAspectRatio )
      {
        int     availableWidth = pictureEditor.Parent.ClientSize.Width;
        int     availableHeight = pictureEditor.Parent.ClientSize.Height;

        double    aspectRatio = 1.0;
        if ( pictureEditor.DisplayPage.Height > 0 )
        {
          aspectRatio = (double)pictureEditor.DisplayPage.Width / pictureEditor.DisplayPage.Height;
        }

        int     pixelWidth = availableWidth;
        int     pixelHeight = availableHeight;

        if ( pixelWidth > pixelHeight * aspectRatio )
        {
          pixelWidth = (int)( pixelHeight * aspectRatio );
        }
        else
        {
          pixelHeight = (int)( pixelWidth / aspectRatio );
        }

        pictureEditor.Anchor = System.Windows.Forms.AnchorStyles.None;
        pictureEditor.Size = new System.Drawing.Size( pixelWidth, pixelHeight );
        pictureEditor.Location = new System.Drawing.Point( ( availableWidth - pixelWidth ) / 2, ( availableHeight - pixelHeight ) / 2 );
      }
      else
      {
        pictureEditor.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        pictureEditor.Size = pictureEditor.Parent.ClientSize;
        pictureEditor.Location = new System.Drawing.Point( 0, 0 );
      }
    }

    private void tabEditor_Resize( object sender, EventArgs e )
    {
       UpdateMapAspectRatio();
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
    }
} // namespace RetroDevStudio.Documents

