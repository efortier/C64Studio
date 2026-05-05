using GR.Collections;
using RetroDevStudio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;



namespace RetroDevStudio.Formats
{
  public class MapProject
  {
    public class TileChar
    {
      public byte       Character = 0;
      public byte       Color = 1;
    };

    public class Marker
    {
      public int        X = 0;
      public int        Y = 0;
      public int        Type = 0;
      public string     Name = "";
      public byte       Value1 = 0;
      public byte       Value2 = 0;
      public bool       Enabled = true;
      public bool       Triggered = false;
      // Per-marker group identifier. Lets the game runtime
      // enable/disable batches of markers at once (e.g. "all triggers
      // for room 5" → GroupId == 5). Default 0 = no group / global.
      // Persisted as a single byte in both the chunk format and the
      // game-binary export record.
      public byte       GroupId = 0;
    };

    public class MarkerType
    {
      public string     Name = "";
      public string     ExportSymbol = "";
      public int        Color = 1;
      public int        ID = 0;
      public int        TagID = 0;
      // Editor-only free-form description of what this marker type
      // represents. Persisted in the map project file but never written
      // to the game binary or any sidecar.
      public string     Description = "";
    };

    public class Entity
    {
      public int        X = 0;
      public int        Y = 0;
      public int        Type = 0;
      public byte       Value1 = 0;
      public byte       Value2 = 0;
      public bool       Enabled = true;
      public bool       Triggered = false;
    };

    public class EntityType
    {
      public string     Name = "";
      public string     ExportSymbol = "";
      public int        TileIndex = 0;
      public int        ID = 0;
      public int        TagID = 0;
    };

    /// <summary>
    /// Runtime control-byte values for <see cref="MapStringLine.Terminator"/>
    /// and the asm exporter. Values match Dreadhold's map-string runtime
    /// (see Z:\DevC64\Dreadhold\src\map_strings.asm:11-17). Color bytes are
    /// $00..$0F (the 16 C64 palette indices) and bytes $20..$FA are
    /// screen-code character data — neither needs a constant here.
    /// </summary>
    public const byte MAP_STRING_END_OF_TEXT     = 0xFF;
    public const byte MAP_STRING_END_OF_LINE     = 0xFD;
    public const byte MAP_STRING_PRESS_FIRE      = 0xFC;
    public const byte MAP_STRING_CLEAR_TEXT_AREA = 0xFB;

    /// <summary>
    /// Sentinel for "this line has no control byte — skip emission". $FF
    /// is END_OF_TEXT; using it as a sentinel is unambiguous because the
    /// renderer only ever reads it as the message-stream terminator, never
    /// as a per-line color byte.
    /// </summary>
    public const byte MAP_STRING_NO_CONTROL_CODE = 0xFF;
    /// <summary>
    /// Sentinel for "this line has no terminator — skip emission". $00
    /// is COLOR_BLACK in the byte-stream alphabet; using it as a sentinel
    /// for the terminator field is unambiguous because terminators are
    /// only ever $FC (PRESS_FIRE) or $FD (END_OF_LINE) in the stream.
    /// </summary>
    public const byte MAP_STRING_NO_TERMINATOR   = 0x00;

    /// <summary>
    /// One of up to 4 lines in a <see cref="MapString"/>.
    ///
    /// <see cref="ControlCode"/> is the line's leading control byte. The
    /// runtime's line-start scan (game_message.asm READ_STRING_BYTE) reads
    /// bytes <c>&lt; $20</c> as the line color, so $00..$0F set the line's
    /// foreground colour and $10..$1F are reserved for future runtime
    /// extensions. The byte is only emitted at export time when
    /// <see cref="Text"/> is non-empty (matches the Dreadhold convention
    /// where blank middle lines emit just END_OF_LINE).
    ///
    /// <see cref="Text"/> is plain authored text — no inline tokens.
    /// Every char becomes one screen code via the per-project lowercase /
    /// uppercase / numbers offsets, plus the fixed C64 punctuation map.
    ///
    /// <see cref="Terminator"/> is the control byte that ends the line:
    /// END_OF_LINE for a normal break or PRESS_FIRE to render the static
    /// "Press Fire to continue" prompt and block until fire.
    /// </summary>
    /// <summary>Per-line text justification within the runtime text area.</summary>
    public const byte MAP_STRING_JUSTIFY_LEFT   = 0;
    public const byte MAP_STRING_JUSTIFY_CENTER = 1;
    public const byte MAP_STRING_JUSTIFY_RIGHT  = 2;

    public class MapStringLine
    {
      // Default to "None" sentinels so a freshly-added MapString emits
      // nothing for lines the user hasn't filled in. Existing project
      // files keep whatever value was saved (white control code by
      // default for pre-sentinel projects, $FD for terminator, etc).
      public byte   ControlCode   = MAP_STRING_NO_CONTROL_CODE;
      public string Text          = "";
      public byte   Terminator    = MAP_STRING_NO_TERMINATOR;
      // Editor-side hint applied at export time only: pads <see cref="Text"/>
      // with leading spaces so it lands left-aligned (no padding), centered,
      // or right-aligned within MapStringsTextAreaWidth columns. Not stored
      // as a stream byte — the runtime sees the already-padded characters.
      public byte   Justification = MAP_STRING_JUSTIFY_LEFT;
    };

    /// <summary>
    /// One named, exportable game-message script. Up to 4 lines of text
    /// rendered into the C64 game's 4-line UI text area. <see cref="Label"/>
    /// is the user-supplied asm identifier (e.g. <c>TEXT_HIT</c>) used to
    /// emit a <c>.const &lt;Label&gt; = &lt;index&gt;</c> in the sidecar; must be a
    /// valid asm identifier to be included on export. <see cref="Lines"/> is
    /// always 4 slots — empty trailing slots are dropped at export time. When
    /// <see cref="ClearTextAreaAtEnd"/> is true, a CLEAR_TEXT_AREA byte is
    /// emitted right before the mandatory END_OF_TEXT terminator.
    /// </summary>
    public class MapString
    {
      public string          Label              = "";
      public MapStringLine[] Lines              = new MapStringLine[4]
      {
        new MapStringLine(), new MapStringLine(), new MapStringLine(), new MapStringLine()
      };
      public bool            ClearTextAreaAtEnd = false;
    };

    public class Tile
    {
      public GR.Game.Layer<TileChar> Chars = new GR.Game.Layer<TileChar>();
      public string       Name = "";
      public int          Index = 0;
      public bool         Passable = true;
      public bool         NotExportedOnMap = false;
      public int          GroupId = 0;

      public Tile()
      {
        Chars.InvalidTile = new TileChar();
      }
    };

    /// <summary>
    /// Read-only snapshot of a <see cref="Map"/> taken at a point in time.
    /// Stored in <see cref="Map.Revisions"/>; the editor lets the user view
    /// any past revision (in read-only mode), revert the live map to one,
    /// or delete revisions they no longer want. Revisions persist in the
    /// project file but are deliberately excluded from any "export to game
    /// binary / asm" path — only the live map ships.
    ///
    /// The snapshot itself is just a <see cref="Map"/> instance whose
    /// <see cref="Map.Revisions"/> list is empty; nesting revisions inside
    /// snapshots would produce an unbounded chain on save.
    /// </summary>
    public class MapRevision
    {
      public string   Name = "";
      public DateTime CreatedAt = DateTime.Now;
      public Map      Snapshot = null;
    };

    public class Map
    {
      public GR.Game.Layer<int> Tiles = new GR.Game.Layer<int>();
      /// <summary>
      /// Per-CHARACTER C64 color override applied at placement time. -1
      /// means "no override" — that character renders and exports using
      /// the tile's own per-character color. 0..15 means "paint that
      /// single character in this C64 color" for both the editor preview
      /// and the exported color grid; the underlying tile definition is
      /// unchanged.
      ///
      /// Dimensions: <see cref="Tiles"/>.Width × <see cref="TileSpacingX"/>
      /// by Tiles.Height × <see cref="TileSpacingY"/> — i.e. one slot per
      /// character cell on the map. A normal tile placement stamps the
      /// placement override (when not "Default") into every char cell of
      /// the tile's footprint, so visually the whole tile gets the colour.
      /// Ctrl+left-click writes a single char cell instead.
      ///
      /// File-format migration: older project files saved this layer at
      /// tile-grid dimensions (one value per tile). The load path detects
      /// that shape and replicates each tile-cell value across its
      /// spacing block to upgrade in-place — old projects look identical
      /// after the upgrade.
      /// </summary>
      public GR.Game.Layer<int> TileColorOverrides = new GR.Game.Layer<int>();

      /// <summary>
      /// Per-character one-way "blocked" override. true at a char =>
      /// that char is impassable in the exported passable bitfield,
      /// regardless of the placed tile's <see cref="Tile.Passable"/>.
      /// false (the default) defers to the tile.
      ///
      /// One-way: this layer can ONLY make tile-passable chars
      /// impassable (place per-character walls/obstacles over a passable
      /// floor tile). It cannot flip a tile-impassable char to passable
      /// — that direction would require a tri-state model and the user
      /// explicitly asked for block-only.
      ///
      /// Dimensions: <see cref="Tiles"/>.Width × <see cref="TileSpacingX"/>
      /// by Tiles.Height × <see cref="TileSpacingY"/> — one slot per
      /// character, mirroring <see cref="TileColorOverrides"/> exactly.
      /// Default <c>false</c> after a fresh <see cref="GR.Game.Layer{T}.Resize"/>
      /// (zero-init) IS the no-override sentinel — no separate reset
      /// step is needed.
      /// </summary>
      public GR.Game.Layer<bool> CharBlockedOverrides = new GR.Game.Layer<bool>();
      public string             Name = "";
      public int                TileSpacingX = 2;
      public int                TileSpacingY = 2;
      public List<Marker>       Markers = new List<Marker>();
      public List<Entity>       Entities = new List<Entity>();
      public GR.Memory.ByteBuffer   ExtraDataOld = new GR.Memory.ByteBuffer();
      public string             ExtraDataText = "";
      public int                AlternativeMultiColor1 = -1;
      public int                AlternativeMultiColor2 = -1;
      public int                AlternativeBackgroundColor = -1;
      public int                AlternativeBGColor4 = -1;
      public int                SelectedMarkerType = 0;
      public int                SelectedEntityType = -1;
      public int                MarkerDimOpacity = 100;

      // Per-map sequential allocator for marker GroupId. The "Find next"
      // button on the marker toolbar dispenses this value, then bumps it.
      // Starts at 1 (GroupId 0 is reserved for "no group"). Stored as int
      // so the cursor can sit at 256 to mean "exhausted" — Find next
      // refuses with a warning at that point because GroupId is a byte
      // in the exported map binary.
      public int                NextMarkerGroupId = 1;

      /// <summary>
      /// overrides Project.Mode when set (e.g. display MC instead of hires)
      /// </summary>
      public TextCharMode       AlternativeMode = TextCharMode.UNKNOWN;

      /// <summary>
      /// Past snapshots of this map kept around as named, read-only points
      /// the user can return to. Persisted with the project; never exported.
      /// Always empty inside a <see cref="MapRevision.Snapshot"/> (no nesting).
      /// </summary>
      public List<MapRevision>  Revisions = new List<MapRevision>();
    };

    public class ExportSettings
    {
      public class AssemblySettings
      {
        public bool   PrefixWith = true;
        public string Prefix = "!byte ";
        public bool   WrapAt = true;
        public int    WrapByteCount = 8;
        public bool   ExportHex = true;
        public bool   VariableNameLabelPrefixEnabled = false;
        public string VariableNameLabelPrefix = "";
        public bool   IncludeSemicolonAfterSimpleLabels = false;
        public bool   MapSizeCommentEnabled = true;
        public string CommentChars = ";";
        public bool   EmptyTileCompressionEnabled = false;
        public int    EmptyTileIndex = 0;
        public bool   SaveOnExport = false;
        public string ExportDirectory = "";

        public string ExportFilename = "";
        public bool   ExportTilesetColors = true;
        public bool   ExportMapColors = true;
        public bool   ExportMapAsCharAndColors = false;
        public bool   AddFilenamespace = false;
        public string Filenamespace = "";
        public bool   WrapMapData = true;
        public bool   ExportCharset = false;
        public string CharsetExportDirectory = "";
        public string CharsetExportFilename = "";
        public bool   AlwaysOverwrite = false;
        public bool   ExportPassableBitfields = false;

        public bool   ExportPassableBitfieldsAsBinary = false;
        public bool   ExportMarkers = true;
        public string PrefixCode = "";
      }

      public class BinarySettings
      {
        public bool   PrefixLoadAddress = false;
        public string PrefixLoadAddressHex = "";
      }

      public class GameBinarySettings
      {
        public bool   ExportMarkers = true;
        public bool   ExportColors = true;
        public bool   ExportPassableBits = true;
        public bool   PrefixLoadAddress = false;
        public string PrefixLoadAddressHex = "";
        public bool   SaveOnExport = false;
        public string ExportDirectory = "";
        public string ExportFilename = "";
        public bool   UseAbsoluteAddresses = false;
        public string AbsoluteBaseAddressHex = "";
        // v18: per-method charset export + .def sidecar toggle
        public bool   ExportCharset = false;
        public string CharsetExportDirectory = "";
        public string CharsetExportFilename = "";
        public bool   CharsetPrefixLoadAddress = false;
        public string CharsetPrefixLoadAddressHex = "";
        public bool   GenerateDefFile = true;
        // v19: opt-in header-constants sidecar (map_header.asm)
        public bool   ExportHeaderAsm = false;
        // v20: optional directory for map_header.asm. Empty -> beside the .bin.
        public string HeaderAsmDirectory = "";
        // v21: per-export filename for map_header.asm, plus opt-in marker
        // labels sidecar (a file mapping ExportSymbol -> TagID).
        public string HeaderAsmFilename = "map_header.asm";
        public bool   ExportMarkerLabels = false;
        public string MarkerLabelsDirectory = "";
        public string MarkerLabelsFilename = "map_markers.asm";
        // v22: user-supplied prefix text prepended to each sidecar (includes, etc.).
        public string HeaderAsmPrefix = "";
        public string MarkerLabelsPrefix = "";
        // v23: opt-in entity-labels sidecar (ExportSymbol -> TagID for entities).
        public bool   ExportEntityLabels = false;
        public string EntityLabelsDirectory = "";
        public string EntityLabelsFilename = "map_entities.asm";
        public string EntityLabelsPrefix = "";
        // v24: opt-in map-strings sidecar (per-project named text scripts ->
        // Dreadhold-style byte stream + MAP_STRING_LO/HI tables + index consts).
        public bool   ExportMapStrings = false;
        public string MapStringsDirectory = "";
        public string MapStringsFilename = "map_strings.asm";
        public string MapStringsPrefix = "";
      }

      public class TargetSettings
      {
        public string TargetFilename = "";
      }

      public int    ExportDataIndex = 0;
      public int    ExportOrientationIndex = 0;
      public int    ExportMethodIndex = 0;

      public AssemblySettings     Assembly = new AssemblySettings();
      public BinarySettings       Binary = new BinarySettings();
      public BinarySettings       CharsetBinary = new BinarySettings();
      public GameBinarySettings   GameBinary = new GameBinarySettings();
      public TargetSettings       CharsetProject = new TargetSettings();
      public TargetSettings       Charscreen = new TargetSettings();
    };


    public List<Tile>                   Tiles = new List<Tile>();
    public List<MarkerType>             MarkerTypes = new List<MarkerType>();
    public List<EntityType>             EntityTypes = new List<EntityType>();
    public List<MapString>              MapStrings = new List<MapString>();
    public List<Map>                    Maps = new List<Map>();

    public string                       ExternalCharset = "";
    public int                          BackgroundColor = 0;
    public int                          MultiColor1 = 0;
    public int                          MultiColor2 = 0;
    public int                          BGColor4 = 0;
    public string                       RightClickAction = "";
    /// <summary>
    /// Name of the tile to drop when the user shift+left-clicks on the map.
    /// Empty string = "no shift-click behavior configured" — the editor
    /// should fall back to writing tile index 0 in that case. Stored by
    /// name (not index) for the same reason RightClickAction is: tile
    /// indices shift around when the user rearranges the tile list.
    /// </summary>
    public string                       ShiftClickBlankTile = "";
    /// <summary>
    /// C64 palette index (0..15) written into TileColorOverrides[x,y] when
    /// shift+left-clicking. -1 has been reserved as "no override" elsewhere
    /// in the codebase, so the default 0 is fine here — we always write a
    /// real color when the gesture fires.
    /// </summary>
    public int                          ShiftClickBlankColor = 0;
    /// <summary>
    /// Legacy C64-palette-index canvas background (0..15). Kept for back-
    /// compat with project files that predate <see cref="DesignerBackgroundColorARGB"/>;
    /// new code should prefer the ARGB form. Value is irrelevant when the
    /// ARGB form is set (its alpha is non-zero).
    /// </summary>
    public int                          DesignerBackgroundColor = 0;
    /// <summary>
    /// True ARGB color for the designer canvas background. The canvas isn't
    /// part of any export and never had to be limited to the C64 palette,
    /// so this field lets the user pick any color via a standard color
    /// picker. Sentinel <c>0</c> (alpha = 0) means "not set; fall back to
    /// the legacy palette-index field." Real picks always have alpha = 0xFF.
    /// </summary>
    public uint                         DesignerBackgroundColorARGB = 0;

    /// <summary>
    /// This mode is used to display/build the tiles
    /// </summary>
    public TextMode                     Mode = TextMode.COMMODORE_40_X_25_HIRES;
    public CharsetProject               Charset = new Formats.CharsetProject();
    public bool                         ShowGrid = false;
    /// <summary>
    /// State of the Auto-tiling toggle on the Map tab. When true, the
    /// editor's auto-tiling logic runs after each tile placement.
    /// Persisted per-project so the editor opens with the same toggle
    /// state the user had when last saving.
    /// </summary>
    public bool                         AutoTiling = false;

    /// <summary>
    /// Optional path to a binary font file used when rendering the Map
    /// Strings tab's preview canvas. The expected format matches what the
    /// game-binary export emits: a 2-byte little-endian load-address header
    /// followed by 8 bytes per glyph. When empty (or the file can't be
    /// loaded), the preview falls back to the project's main charset.
    /// </summary>
    public string                       MapStringsPreviewFontPath = "";

    /// <summary>
    /// Map Strings preview: starting charset index for the lowercase letter
    /// run (a..z). Used so 'a' renders at <c>MapStringsLowercaseIndex</c>,
    /// 'b' at +1, etc. Defaults to 1, matching the C64 unshifted charset
    /// where there is no separate lowercase block — users with a custom
    /// upper+lower charset (e.g. Dreadhold's UICharset) override this.
    /// </summary>
    public int                          MapStringsLowercaseIndex = 1;
    /// <summary>
    /// Map Strings preview: starting charset index for the uppercase letter
    /// run (A..Z). Defaults to 1 (C64 standard: 'A' = $01).
    /// </summary>
    public int                          MapStringsUppercaseIndex = 1;
    /// <summary>
    /// Map Strings preview: starting charset index for the digit run (0..9).
    /// Defaults to 48 ($30, C64 standard).
    /// </summary>
    public int                          MapStringsNumbersIndex = 48;
    /// <summary>
    /// Map Strings: width (in characters) of the runtime text area used for
    /// center / right justification at export time. Defaults to 40, the C64
    /// standard screen width. Padding is applied as leading spaces so the
    /// runtime can write the bytes straight to screen RAM.
    /// </summary>
    public int                          MapStringsTextAreaWidth = 40;
    /// <summary>
    /// Opacity of the grid overlay drawn on the map editor (0..100).
    /// 100 = opaque white grid lines (legacy behaviour); 0 = invisible
    /// (equivalent to ShowGrid=false). Each grid pixel is alpha-blended
    /// against the map underneath at this percentage. Stored per-project
    /// so different maps can have visually-distinct grid emphasis.
    /// </summary>
    public int                          GridOpacity = 100;
    public bool                         ShowCharacterListGrid = false;
    /// <summary>
    /// Index into <see cref="Maps"/> of the map the user had selected when
    /// the project was last saved. -1 = "no preference, just pick the first
    /// map." Restored on load so the editor reopens on the map the user
    /// was working on.
    /// </summary>
    public int                          CurrentMapIndex = -1;
    public bool                         KeepCharacterAspectRatio = false;
    public int                          CharactersPerRow = 16;
    public int                          CharacterEditorMode = 1;
    public int                          ColorSwatchSize = 16;
    public ExportSettings               Settings = new ExportSettings();



    public MapProject()
    {
      for ( int i = 0; i < 256; ++i )
      {
        for ( int j = 0; j < 8; ++j )
        {
          Charset.Characters[i].Tile.CustomColor = 1;
          Charset.Characters[i].Tile.Data.SetU8At( j, ConstantData.UpperCaseCharsetC64.ByteAt( i * 8 + j ) );
        }
      }
    }



    public void Clear()
    {
      Tiles.Clear();
      Maps.Clear();
      MapStrings.Clear();
      ExternalCharset = "";
      RightClickAction = "";
      CharactersPerRow = 16;
      CharacterEditorMode = 1;
      Settings = new ExportSettings();
    }



    public GR.Memory.ByteBuffer SaveToBuffer()
    {
      GR.Memory.ByteBuffer projectFile = new GR.Memory.ByteBuffer();

      GR.IO.FileChunk chunkProjectInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_INFO );
      // version
      chunkProjectInfo.AppendU32( 6 );
      chunkProjectInfo.AppendString( ExternalCharset );
      chunkProjectInfo.AppendI32( ShowGrid ? 1 : 0 );
      chunkProjectInfo.AppendString( RightClickAction );
      chunkProjectInfo.AppendI32( KeepCharacterAspectRatio ? 1 : 0 );
      chunkProjectInfo.AppendI32( CharactersPerRow );
      chunkProjectInfo.AppendI32( CharacterEditorMode );
      chunkProjectInfo.AppendI32( ColorSwatchSize );
      chunkProjectInfo.AppendI32( ShowCharacterListGrid ? 1 : 0 );
      // Appended without a version bump — readers use a position check to
      // tell old files (no value) from new (value present). See the load
      // side at the bottom of MAP_PROJECT_INFO's case.
      chunkProjectInfo.AppendI32( CurrentMapIndex );
      // Shift-click "blank" tile + color, also appended.
      chunkProjectInfo.AppendString( ShiftClickBlankTile ?? "" );
      chunkProjectInfo.AppendI32( ShiftClickBlankColor );
      // Grid opacity (0..100). Append-only.
      chunkProjectInfo.AppendI32( GridOpacity );
      // Auto-tiling toggle state (per-project). Append-only — old
      // files without this byte fall through to the default false on
      // load.
      chunkProjectInfo.AppendU8( AutoTiling ? (byte)1 : (byte)0 );
      // Map Strings preview font path. Append-only; old files fall through
      // to the default empty string (preview renders from project charset).
      chunkProjectInfo.AppendString( MapStringsPreviewFontPath ?? "" );
      // Map Strings preview charset offsets (lowercase / uppercase / digits).
      // Append-only; old files fall through to the field defaults (1, 1, 48).
      chunkProjectInfo.AppendI32( MapStringsLowercaseIndex );
      chunkProjectInfo.AppendI32( MapStringsUppercaseIndex );
      chunkProjectInfo.AppendI32( MapStringsNumbersIndex );
      // Map Strings text-area width (default 40). Used at export time to
      // compute leading-space padding for centered / right-justified lines.
      chunkProjectInfo.AppendI32( MapStringsTextAreaWidth );
      projectFile.Append( chunkProjectInfo.ToBuffer() );

      GR.IO.FileChunk chunkCharset = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET );
      chunkCharset.Append( Charset.SaveToBuffer() );
      projectFile.Append( chunkCharset.ToBuffer() );

      GR.IO.FileChunk chunkProjectData = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_DATA );

      GR.IO.FileChunk chunkMCData = new GR.IO.FileChunk( FileChunkConstants.MULTICOLOR_DATA );
      chunkMCData.AppendU8( (byte)Mode );
      chunkMCData.AppendU8( (byte)BackgroundColor );
      chunkMCData.AppendU8( (byte)MultiColor1 );
      chunkMCData.AppendU8( (byte)MultiColor2 );
      chunkMCData.AppendU8( (byte)BGColor4 );
      chunkProjectData.Append( chunkMCData.ToBuffer() );

      foreach ( var markerType in MarkerTypes )
      {
        GR.IO.FileChunk chunkMarkerType = new GR.IO.FileChunk( FileChunkConstants.MAP_MARKER_TYPES );
        chunkMarkerType.AppendI32( markerType.ID );
        chunkMarkerType.AppendString( markerType.Name );
        chunkMarkerType.AppendI32( markerType.Color );
        chunkMarkerType.AppendString( markerType.ExportSymbol ?? "" );
        chunkMarkerType.AppendU8( (byte)markerType.TagID );
        // Appended for Description — editor-only free-form text. Forward-
        // compat: older readers stop after TagID and leave the default "".
        chunkMarkerType.AppendString( markerType.Description ?? "" );
        chunkProjectData.Append( chunkMarkerType.ToBuffer() );
      }

      foreach ( var entityType in EntityTypes )
      {
        GR.IO.FileChunk chunkEntityType = new GR.IO.FileChunk( FileChunkConstants.MAP_ENTITY_TYPES );
        chunkEntityType.AppendI32( entityType.ID );
        chunkEntityType.AppendString( entityType.Name );
        chunkEntityType.AppendString( entityType.ExportSymbol ?? "" );
        chunkEntityType.AppendI32( entityType.TileIndex );
        chunkEntityType.AppendU8( (byte)entityType.TagID );
        chunkProjectData.Append( chunkEntityType.ToBuffer() );
      }

      foreach ( var ms in MapStrings )
      {
        GR.IO.FileChunk chunkMapString = new GR.IO.FileChunk( FileChunkConstants.MAP_STRING );
        chunkMapString.AppendString( ms.Label ?? "" );
        chunkMapString.AppendU8( ms.ClearTextAreaAtEnd ? (byte)1 : (byte)0 );
        for ( int i = 0; i < 4; ++i )
        {
          var line = ms.Lines[i] ?? new MapStringLine();
          chunkMapString.AppendString( line.Text ?? "" );
          chunkMapString.AppendU8( line.Terminator );
          // Per-line control code (line color byte). Appended; old project
          // files that lack this byte fall through to the default of $01
          // (white) on load.
          chunkMapString.AppendU8( line.ControlCode );
          // Per-line justification (Left=0 / Center=1 / Right=2). Appended.
          chunkMapString.AppendU8( line.Justification );
        }
        chunkProjectData.Append( chunkMapString.ToBuffer() );
      }

      foreach ( Tile tile in Tiles )
      {
        GR.IO.FileChunk chunkTile = new GR.IO.FileChunk( FileChunkConstants.MAP_TILE );

        chunkTile.AppendString( tile.Name );
        chunkTile.AppendI32( tile.Chars.Width );
        chunkTile.AppendI32( tile.Chars.Height );
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            TileChar    tChar = tile.Chars[i, j];
            chunkTile.AppendU8( tChar.Character );
            chunkTile.AppendU8( tChar.Color );
          }
        }
        chunkTile.AppendU8( tile.Passable ? (byte)1 : (byte)0 );
        chunkTile.AppendU8( tile.NotExportedOnMap ? (byte)1 : (byte)0 );
        chunkTile.AppendI32( tile.GroupId );
        chunkProjectData.Append( chunkTile.ToBuffer() );
      }
      foreach ( Map map in Maps )
      {
        // Top-level project save includes per-map revision history. Snapshot
        // chunks recurse with IncludeRevisions=false so revisions don't
        // nest inside revisions.
        chunkProjectData.Append( BuildMapChunk( map, IncludeRevisions: true ).ToBuffer() );
      }

      projectFile.Append( chunkProjectData.ToBuffer() );

      GR.IO.FileChunk chunkExportSettings = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_EXPORT_SETTINGS );
      chunkExportSettings.AppendU32( 24 );
      chunkExportSettings.AppendI32(Settings.ExportDataIndex );
      chunkExportSettings.AppendI32(Settings.ExportOrientationIndex );
      chunkExportSettings.AppendI32( Settings.ExportMethodIndex );
      chunkExportSettings.AppendI32( Settings.Assembly.PrefixWith ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.Prefix ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.WrapAt ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.WrapByteCount );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportHex ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.VariableNameLabelPrefixEnabled ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.VariableNameLabelPrefix ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.IncludeSemicolonAfterSimpleLabels ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.MapSizeCommentEnabled ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.CommentChars ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.EmptyTileCompressionEnabled ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.EmptyTileIndex );
      chunkExportSettings.AppendI32( Settings.Assembly.SaveOnExport ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.ExportDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.Assembly.ExportFilename ?? "" );
      chunkExportSettings.AppendI32( Settings.Binary.PrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Binary.PrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendI32( Settings.CharsetBinary.PrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.CharsetBinary.PrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendString( Settings.CharsetProject.TargetFilename ?? "" );
      chunkExportSettings.AppendString( Settings.Charscreen.TargetFilename ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportTilesetColors ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportMapColors ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.AddFilenamespace ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.Filenamespace ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.WrapMapData ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportCharset ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.CharsetExportDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.Assembly.CharsetExportFilename ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.AlwaysOverwrite ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportMapAsCharAndColors ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportPassableBitfields ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportPassableBitfieldsAsBinary ? 1 : 0 );
      chunkExportSettings.AppendI32( DesignerBackgroundColor );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportMarkers ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.PrefixCode ?? "" ); // Added new field
      // version 16: game binary settings
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportMarkers ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportColors ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportPassableBits ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.GameBinary.PrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.PrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendI32( Settings.GameBinary.SaveOnExport ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.ExportDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.ExportFilename ?? "" );
      // version 17: absolute base address
      chunkExportSettings.AppendI32( Settings.GameBinary.UseAbsoluteAddresses ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.AbsoluteBaseAddressHex ?? "" );
      // version 18: game binary per-method charset export + .def sidecar toggle
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportCharset ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.CharsetExportDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.CharsetExportFilename ?? "" );
      chunkExportSettings.AppendI32( Settings.GameBinary.CharsetPrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.CharsetPrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendI32( Settings.GameBinary.GenerateDefFile ? 1 : 0 );
      // version 19: header-constants sidecar toggle (map_header.asm)
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportHeaderAsm ? 1 : 0 );
      // version 20: optional directory override for map_header.asm
      chunkExportSettings.AppendString( Settings.GameBinary.HeaderAsmDirectory ?? "" );
      // version 21: header-asm filename + marker-labels sidecar
      chunkExportSettings.AppendString( Settings.GameBinary.HeaderAsmFilename ?? "map_header.asm" );
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportMarkerLabels ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.MarkerLabelsDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.MarkerLabelsFilename ?? "map_markers.asm" );
      // version 22: per-sidecar user-supplied prefix text
      chunkExportSettings.AppendString( Settings.GameBinary.HeaderAsmPrefix ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.MarkerLabelsPrefix ?? "" );
      // version 23: entity-labels sidecar
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportEntityLabels ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.EntityLabelsDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.EntityLabelsFilename ?? "map_entities.asm" );
      chunkExportSettings.AppendString( Settings.GameBinary.EntityLabelsPrefix ?? "" );
      // version 24: map-strings sidecar
      chunkExportSettings.AppendI32( Settings.GameBinary.ExportMapStrings ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.GameBinary.MapStringsDirectory ?? "" );
      chunkExportSettings.AppendString( Settings.GameBinary.MapStringsFilename ?? "map_strings.asm" );
      chunkExportSettings.AppendString( Settings.GameBinary.MapStringsPrefix ?? "" );
      // True ARGB designer canvas color. Appended at the end of the chunk
      // (no version bump per the project's append-only convention). Older
      // readers stop here and use the legacy palette-index field above.
      // We still write the legacy field a few lines up so old apps keep
      // loading the project with a sensible (palette-quantised) color.
      chunkExportSettings.AppendU32( DesignerBackgroundColorARGB );
      projectFile.Append( chunkExportSettings.ToBuffer() );
      return projectFile;
    }



    public bool ReadFromBuffer( GR.Memory.ByteBuffer ProjectFile )
    {
      if ( ProjectFile == null )
      {
        return false;
      }

      GR.IO.MemoryReader    memReader = new GR.IO.MemoryReader( ProjectFile );

      GR.IO.FileChunk chunk = new GR.IO.FileChunk();

      string importedCharSet = "";
      while ( chunk.ReadFromStream( memReader ) )
      {
        GR.IO.MemoryReader chunkReader = chunk.MemoryReader();
        switch ( chunk.Type )
        {
          case FileChunkConstants.MAP_PROJECT_INFO:
            {
              uint version  = chunkReader.ReadUInt32();
              importedCharSet = chunkReader.ReadString();

              ShowGrid = ( chunkReader.ReadInt32() == 1 );
              if ( version >= 1 )
              {
                RightClickAction = chunkReader.ReadString();
              }
              if ( version >= 2 )
              {
                KeepCharacterAspectRatio = ( chunkReader.ReadInt32() == 1 );
              }
              if ( version >= 3 )
              {
                CharactersPerRow = chunkReader.ReadInt32();
              }
              if ( version >= 4 )
              {
                CharacterEditorMode = chunkReader.ReadInt32();
              }
              else
              {
                CharacterEditorMode = 1;
              }
              if ( version >= 5 )
              {
                ColorSwatchSize = chunkReader.ReadInt32();
                if ( ColorSwatchSize < 1 )
                {
                  ColorSwatchSize = 16;
                }
              }
              if ( version >= 6 )
              {
                ShowCharacterListGrid = ( chunkReader.ReadInt32() == 1 );
              }
              // Optional appended field — old files just leave it at -1
              // (the constructor default), which means "pick the first map."
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                CurrentMapIndex = chunkReader.ReadInt32();
              }
              // Shift-click blank-tile name + blank-color index. Both are
              // optional appendages; if either is missing, defaults stick.
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                ShiftClickBlankTile = chunkReader.ReadString();
              }
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                ShiftClickBlankColor = chunkReader.ReadInt32();
                if ( ShiftClickBlankColor < 0 || ShiftClickBlankColor > 15 )
                {
                  ShiftClickBlankColor = 0;
                }
              }
              // Optional appended Grid opacity. Old files leave it at the
              // default of 100 (fully opaque, matches the legacy behaviour
              // before this slider existed).
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                GridOpacity = chunkReader.ReadInt32();
                if ( GridOpacity < 0 )   GridOpacity = 0;
                if ( GridOpacity > 100 ) GridOpacity = 100;
              }
              // Optional appended AutoTiling toggle. Old files default
              // to false (toggle off) — same as the field's initializer.
              if ( chunkReader.Size - chunkReader.Position >= 1 )
              {
                AutoTiling = ( chunkReader.ReadUInt8() != 0 );
              }
              // Optional Map Strings preview font path. ReadString on a
              // truncated stream is safer than guessing a length, so guard
              // with a position check first. Empty string is the default.
              if ( chunkReader.Size - chunkReader.Position >= 1 )
              {
                MapStringsPreviewFontPath = chunkReader.ReadString();
              }
              // Optional Map Strings preview charset offsets. Read all 3
              // together — if any are missing, we keep all field defaults
              // rather than partially overwriting.
              if ( chunkReader.Size - chunkReader.Position >= 12 )
              {
                MapStringsLowercaseIndex = chunkReader.ReadInt32();
                MapStringsUppercaseIndex = chunkReader.ReadInt32();
                MapStringsNumbersIndex   = chunkReader.ReadInt32();
                if ( MapStringsLowercaseIndex < 0 ) MapStringsLowercaseIndex = 0;
                if ( MapStringsLowercaseIndex > 255 ) MapStringsLowercaseIndex = 255;
                if ( MapStringsUppercaseIndex < 0 ) MapStringsUppercaseIndex = 0;
                if ( MapStringsUppercaseIndex > 255 ) MapStringsUppercaseIndex = 255;
                if ( MapStringsNumbersIndex < 0 ) MapStringsNumbersIndex = 0;
                if ( MapStringsNumbersIndex > 255 ) MapStringsNumbersIndex = 255;
              }
              // Optional Map Strings text-area width (append-only; default 40).
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                MapStringsTextAreaWidth = chunkReader.ReadInt32();
                if ( MapStringsTextAreaWidth < 1 )    MapStringsTextAreaWidth = 1;
                if ( MapStringsTextAreaWidth > 255 )  MapStringsTextAreaWidth = 255;
              }
            }
            break;
          case FileChunkConstants.MAP_CHARSET:
            {
              GR.Memory.ByteBuffer    data = new GR.Memory.ByteBuffer();
              chunkReader.ReadBlock( data, (uint)( chunkReader.Size - chunkReader.Position ) );

              Charset.ReadFromBuffer( data );
            }
            break;
          case FileChunkConstants.MAP_PROJECT_DATA:
            {
              GR.IO.FileChunk chunkData = new GR.IO.FileChunk();

              while ( chunkData.ReadFromStream( chunkReader ) )
              {
                GR.IO.MemoryReader subChunkReader = chunkData.MemoryReader();
                switch ( chunkData.Type )
                {
                  case FileChunkConstants.MULTICOLOR_DATA:
                    Mode = (TextMode)subChunkReader.ReadUInt8();
                    BackgroundColor = subChunkReader.ReadUInt8();
                    MultiColor1 = subChunkReader.ReadUInt8();
                    MultiColor2 = subChunkReader.ReadUInt8();
                    BGColor4 = subChunkReader.ReadUInt8();
                    break;
                  case FileChunkConstants.MAP_MARKER_TYPES:
                    {
                      MarkerType  mType = new MarkerType();
                      mType.ID = subChunkReader.ReadInt32();
                      mType.Name = subChunkReader.ReadString();
                      mType.Color = subChunkReader.ReadInt32();
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        mType.ExportSymbol = subChunkReader.ReadString();
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        mType.TagID = subChunkReader.ReadUInt8();
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        mType.Description = subChunkReader.ReadString();
                      }
                      MarkerTypes.Add( mType );
                    }
                    break;
                  case FileChunkConstants.MAP_ENTITY_TYPES:
                    {
                      EntityType  eType = new EntityType();
                      eType.ID = subChunkReader.ReadInt32();
                      eType.Name = subChunkReader.ReadString();
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        eType.ExportSymbol = subChunkReader.ReadString();
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        eType.TileIndex = subChunkReader.ReadInt32();
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        eType.TagID = subChunkReader.ReadUInt8();
                      }
                      EntityTypes.Add( eType );
                    }
                    break;
                  case FileChunkConstants.MAP_STRING:
                    {
                      MapString ms = new MapString();
                      ms.Label = subChunkReader.ReadString();
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        ms.ClearTextAreaAtEnd = ( subChunkReader.ReadUInt8() != 0 );
                      }
                      for ( int li = 0; li < 4; ++li )
                      {
                        if ( subChunkReader.Position < subChunkReader.Size )
                        {
                          ms.Lines[li].Text = subChunkReader.ReadString();
                        }
                        if ( subChunkReader.Position < subChunkReader.Size )
                        {
                          ms.Lines[li].Terminator = subChunkReader.ReadUInt8();
                        }
                        // Validate against the three legal values: None,
                        // END_OF_LINE, PRESS_FIRE. Anything else is a
                        // corrupt/legacy byte — fall back to None so the
                        // line emits no terminator.
                        if ( ( ms.Lines[li].Terminator != MAP_STRING_NO_TERMINATOR )
                        &&   ( ms.Lines[li].Terminator != MAP_STRING_END_OF_LINE )
                        &&   ( ms.Lines[li].Terminator != MAP_STRING_PRESS_FIRE ) )
                        {
                          ms.Lines[li].Terminator = MAP_STRING_NO_TERMINATOR;
                        }
                        if ( subChunkReader.Position < subChunkReader.Size )
                        {
                          ms.Lines[li].ControlCode = subChunkReader.ReadUInt8();
                        }
                        if ( subChunkReader.Position < subChunkReader.Size )
                        {
                          ms.Lines[li].Justification = subChunkReader.ReadUInt8();
                          if ( ms.Lines[li].Justification > MAP_STRING_JUSTIFY_RIGHT )
                          {
                            ms.Lines[li].Justification = MAP_STRING_JUSTIFY_LEFT;
                          }
                        }
                      }
                      MapStrings.Add( ms );
                    }
                    break;
                  case FileChunkConstants.MAP_TILE:
                    {
                      Tile tile = new Tile();
                      tile.Name = subChunkReader.ReadString();
 
                      int w = subChunkReader.ReadInt32();
                      int h = subChunkReader.ReadInt32();
 
                      tile.Chars.Resize( w, h );
                      for ( int j = 0; j < tile.Chars.Height; ++j )
                      {
                        for ( int i = 0; i < tile.Chars.Width; ++i )
                        {
                          tile.Chars[i, j].Character = subChunkReader.ReadUInt8();
                          tile.Chars[i, j].Color = subChunkReader.ReadUInt8();
                        }
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        tile.Passable = ( subChunkReader.ReadUInt8() != 0 );
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        tile.NotExportedOnMap = ( subChunkReader.ReadUInt8() != 0 );
                      }
                      if ( subChunkReader.Position < subChunkReader.Size )
                      {
                        tile.GroupId = subChunkReader.ReadInt32();
                      }
                      Tiles.Add( tile );
                      tile.Index = Tiles.Count - 1;
                    }
                    break;
                  case FileChunkConstants.MAP:
                    {
                      Map map = new Map();
                      ReadMapFromBody( subChunkReader, map );
                      Maps.Add( map );
                    }
                    break;
                }
              }
            }
            break;
          case FileChunkConstants.MAP_PROJECT_EXPORT_SETTINGS:
            {
              uint version = chunkReader.ReadUInt32();
              if ( version >= 13 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefixEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefix = chunkReader.ReadString();
                Settings.Assembly.IncludeSemicolonAfterSimpleLabels = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.MapSizeCommentEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.CommentChars = chunkReader.ReadString();
                Settings.Assembly.EmptyTileCompressionEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.EmptyTileIndex = chunkReader.ReadInt32();
                Settings.Assembly.SaveOnExport = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportDirectory = chunkReader.ReadString();
                Settings.Assembly.ExportFilename = chunkReader.ReadString();
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
                Settings.Assembly.ExportTilesetColors = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportMapColors = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.AddFilenamespace = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Filenamespace = chunkReader.ReadString();
                Settings.Assembly.WrapMapData = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportCharset = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.CharsetExportDirectory = chunkReader.ReadString();
                Settings.Assembly.CharsetExportFilename = chunkReader.ReadString();
                Settings.Assembly.AlwaysOverwrite = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportMapAsCharAndColors = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportPassableBitfields = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.ExportPassableBitfieldsAsBinary = ( chunkReader.ReadInt32() != 0 );
                if ( chunkReader.Position < chunkReader.Size )
                {
                  DesignerBackgroundColor = chunkReader.ReadInt32();
                }
                if ( version >= 14 )
                {
                  Settings.Assembly.ExportMarkers = ( chunkReader.ReadInt32() != 0 );
                }
                if ( version >= 15 )
                {
                  Settings.Assembly.PrefixCode = chunkReader.ReadString();
                }
                if ( version >= 16 )
                {
                  Settings.GameBinary.ExportMarkers = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.ExportColors = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.ExportPassableBits = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                  Settings.GameBinary.SaveOnExport = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.ExportDirectory = chunkReader.ReadString();
                  Settings.GameBinary.ExportFilename = chunkReader.ReadString();
                }
                if ( version >= 17 )
                {
                  Settings.GameBinary.UseAbsoluteAddresses = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.AbsoluteBaseAddressHex = chunkReader.ReadString();
                }
                if ( version >= 18 )
                {
                  Settings.GameBinary.ExportCharset = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.CharsetExportDirectory = chunkReader.ReadString();
                  Settings.GameBinary.CharsetExportFilename = chunkReader.ReadString();
                  Settings.GameBinary.CharsetPrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.CharsetPrefixLoadAddressHex = chunkReader.ReadString();
                  Settings.GameBinary.GenerateDefFile = ( chunkReader.ReadInt32() != 0 );
                }
                if ( version >= 19 )
                {
                  Settings.GameBinary.ExportHeaderAsm = ( chunkReader.ReadInt32() != 0 );
                }
                if ( version >= 20 )
                {
                  Settings.GameBinary.HeaderAsmDirectory = chunkReader.ReadString();
                }
                if ( version >= 21 )
                {
                  Settings.GameBinary.HeaderAsmFilename = chunkReader.ReadString();
                  Settings.GameBinary.ExportMarkerLabels = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.MarkerLabelsDirectory = chunkReader.ReadString();
                  Settings.GameBinary.MarkerLabelsFilename = chunkReader.ReadString();
                  // Defaults if a newer field is somehow empty
                  if ( string.IsNullOrEmpty( Settings.GameBinary.HeaderAsmFilename ) )
                  {
                    Settings.GameBinary.HeaderAsmFilename = "map_header.asm";
                  }
                  if ( string.IsNullOrEmpty( Settings.GameBinary.MarkerLabelsFilename ) )
                  {
                    Settings.GameBinary.MarkerLabelsFilename = "map_markers.asm";
                  }
                }
                if ( version >= 22 )
                {
                  Settings.GameBinary.HeaderAsmPrefix = chunkReader.ReadString();
                  Settings.GameBinary.MarkerLabelsPrefix = chunkReader.ReadString();
                }
                if ( version >= 23 )
                {
                  Settings.GameBinary.ExportEntityLabels = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.EntityLabelsDirectory = chunkReader.ReadString();
                  Settings.GameBinary.EntityLabelsFilename = chunkReader.ReadString();
                  Settings.GameBinary.EntityLabelsPrefix = chunkReader.ReadString();
                  if ( string.IsNullOrEmpty( Settings.GameBinary.EntityLabelsFilename ) )
                  {
                    Settings.GameBinary.EntityLabelsFilename = "map_entities.asm";
                  }
                }
                if ( version >= 24 )
                {
                  Settings.GameBinary.ExportMapStrings = ( chunkReader.ReadInt32() != 0 );
                  Settings.GameBinary.MapStringsDirectory = chunkReader.ReadString();
                  Settings.GameBinary.MapStringsFilename = chunkReader.ReadString();
                  Settings.GameBinary.MapStringsPrefix = chunkReader.ReadString();
                  if ( string.IsNullOrEmpty( Settings.GameBinary.MapStringsFilename ) )
                  {
                    Settings.GameBinary.MapStringsFilename = "map_strings.asm";
                  }
                }
              }
              // Optional appended ARGB canvas color — older project files
              // stop here and the editor will fall back to the legacy
              // palette-index field. Sentinel 0 (alpha = 0) also means
              // "use legacy"; real picks always carry alpha 0xFF.
              if ( chunkReader.Size - chunkReader.Position >= 4 )
              {
                DesignerBackgroundColorARGB = chunkReader.ReadUInt32();
              }
            }
            break;
        }
      }
      memReader.Close();


      Charset.Colors.MultiColor1 = MultiColor1;
      Charset.Colors.MultiColor2 = MultiColor2;
      Charset.Colors.BGColor4    = BGColor4;
      return true;
    }



    public GR.Memory.ByteBuffer ExportAsTiles()
    {
      GR.Memory.ByteBuffer    tileData = new GR.Memory.ByteBuffer();

      // find max tile size
      int     tileW = 1;
      int     tileH = 1;

      foreach ( var tile in Tiles )
      {
        if ( tile.Chars.Width > tileW )
        {
          tileW = tile.Chars.Width;
        }
        if ( tile.Chars.Height > tileH )
        {
          tileH = tile.Chars.Height;
        }
      }

      for ( int j = 0; j < tileH; ++j )
      {
        for ( int i = 0; i < tileW; ++i )
        {
          foreach ( Formats.MapProject.Tile tile in Tiles )
          {
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileData.AppendU8( (byte)tile.Chars[i,j].Character );
            }
            else
            {
              tileData.AppendU8( 0 );
            }
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
            else
            {
              tileData.AppendU8( 0 );
            }
          }
        }
      }
      return tileData;
    }



    public GR.Memory.ByteBuffer ExportMapsAsBuffer( bool RowByRow )
    {
      GR.Memory.ByteBuffer    mapData = new GR.Memory.ByteBuffer();


      foreach ( var map in Maps )
      {
        mapData.Append( ExportMapAsBuffer( map, RowByRow ) );
      }
      return mapData;
    }



    public string ExportMapsAsAssembly( string Prefix, bool RowByRow )
    {
      StringBuilder   sb = new StringBuilder();

      foreach ( var map in Maps )
      {
        GR.Memory.ByteBuffer      mapData = ExportMapAsBuffer( map, RowByRow );

        sb.Append( Prefix );
        sb.AppendLine( map.Name );

      }
      return sb.ToString();
    }



    public GR.Memory.ByteBuffer ExportMapAsBuffer( Map Map, bool RowByRow )
    {
      GR.Memory.ByteBuffer mapDataBuffer = new GR.Memory.ByteBuffer( (uint)( Map.Tiles.Width * Map.Tiles.Height ) );

      if ( RowByRow )
      {
        for ( int y = 0; y < Map.Tiles.Height; ++y )
        {
          for ( int x = 0; x < Map.Tiles.Width; ++x )
          {
            mapDataBuffer.SetU8At( x + y * Map.Tiles.Width, (byte)GetExportTileIndex( Map.Tiles[x, y] ) );
          }
        }
      }
      else
      {
        for ( int x = 0; x < Map.Tiles.Width; ++x )
        {
          for ( int y = 0; y < Map.Tiles.Height; ++y )          
          {
            mapDataBuffer.SetU8At( x + y * Map.Tiles.Width, (byte)GetExportTileIndex( Map.Tiles[x, y] ) );
          }
        }
      }
      return mapDataBuffer;
    }



    public bool ExportTilesAsElements( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      GR.Memory.ByteBuffer tileDataW = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileDataH = new GR.Memory.ByteBuffer();

      StringBuilder sbTileCharLo = new StringBuilder();
      StringBuilder sbTileCharHi = new StringBuilder();
      StringBuilder sbTileColorLo = new StringBuilder();
      StringBuilder sbTileColorHi = new StringBuilder();
      StringBuilder sbTileChars = new StringBuilder();
      StringBuilder sbTileColors = new StringBuilder();

      GR.Memory.ByteBuffer tileDataChars = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileDataHi = new GR.Memory.ByteBuffer();

      var usedLabels = new Dictionary<string, int>();

      foreach ( Formats.MapProject.Tile tile in Tiles )
      {
        tileDataW.AppendU8( (byte)tile.Chars.Width );
        tileDataH.AppendU8( (byte)tile.Chars.Height );

        string    normalizedLabel = NormalizeAsLabel( tile.Name ).ToUpper();
        if ( usedLabels.ContainsKey( normalizedLabel ) )
        {
          int   subIndex = usedLabels[normalizedLabel] + 1;

          usedLabels[normalizedLabel] = subIndex;
          normalizedLabel += "_" + subIndex;
        }
        else
        {
          usedLabels.Add( normalizedLabel, 1 );
        }

        sbTileCharLo.Append( DataByteDirective );
        sbTileCharLo.AppendLine( " <" + LabelPrefix + "TILE_CHAR_" + normalizedLabel );
        sbTileCharHi.Append( DataByteDirective );
        sbTileCharHi.AppendLine( " >" + LabelPrefix + "TILE_CHAR_" + normalizedLabel );

        sbTileColorLo.Append( DataByteDirective );
        sbTileColorLo.AppendLine( " <" + LabelPrefix + "TILE_COLOR_" + normalizedLabel );
        sbTileColorHi.Append( DataByteDirective );
        sbTileColorHi.AppendLine( " >" + LabelPrefix + "TILE_COLOR_" + normalizedLabel );

        sbTileChars.AppendLine( LabelPrefix + "TILE_CHAR_" + normalizedLabel );

        var tileCharData = new GR.Memory.ByteBuffer();
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            tileCharData.AppendU8( tile.Chars[i, j].Character );
          }
        }
        sbTileChars.AppendLine( Util.ToASMData( tileCharData, WrapData, WrapByteCount, DataByteDirective ) );


        sbTileColors.AppendLine( LabelPrefix + "TILE_COLOR_" + normalizedLabel );

        var tileColorData = new GR.Memory.ByteBuffer();
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            tileColorData.AppendU8( tile.Chars[i, j].Color );
          }
        }
        sbTileColors.AppendLine( Util.ToASMData( tileColorData, WrapData, WrapByteCount, DataByteDirective ) );

      }
      TileData = LabelPrefix + "NUM_TILES = " + Tiles.Count + System.Environment.NewLine
                + LabelPrefix + "TILE_WIDTH" + System.Environment.NewLine + Util.ToASMData( tileDataW, WrapData, WrapByteCount, DataByteDirective ) + System.Environment.NewLine
                + LabelPrefix + "TILE_HEIGHT" + System.Environment.NewLine + Util.ToASMData( tileDataH, WrapData, WrapByteCount, DataByteDirective ) + System.Environment.NewLine
                + LabelPrefix + "TILE_CHARS_LO" + System.Environment.NewLine
                + sbTileCharLo.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_CHARS_HI" + System.Environment.NewLine
                + sbTileCharHi.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_COLORS_LO" + System.Environment.NewLine
                + sbTileColorLo.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_COLORS_HI" + System.Environment.NewLine
                + sbTileColorHi.ToString() + System.Environment.NewLine
                + sbTileChars.ToString() + System.Environment.NewLine
                + sbTileColors.ToString() + System.Environment.NewLine;
      return true;
    }



    public bool ExportTilesAsAssembly( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      int   maxTileWidth = 0;
      int   maxTileHeight = 0;
      foreach ( var tile in Tiles )
      {
        if ( tile.Chars.Width > maxTileWidth )
        {
          maxTileWidth = tile.Chars.Width;
        }
        if ( tile.Chars.Height > maxTileHeight )
        {
          maxTileHeight = tile.Chars.Height;
        }
      }

      GR.Memory.ByteBuffer[]  tileCharData = new GR.Memory.ByteBuffer[maxTileWidth * maxTileHeight];
      GR.Memory.ByteBuffer[]  tileColorData = new GR.Memory.ByteBuffer[maxTileWidth * maxTileHeight];
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          tileCharData[i + j * maxTileWidth] = new GR.Memory.ByteBuffer( (uint)Tiles.Count );
          tileColorData[i + j * maxTileWidth] = new GR.Memory.ByteBuffer( (uint)Tiles.Count );
        }
      }


      int tileIndex = 0;
      
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          tileIndex = 0;
          foreach ( var tile in Tiles )
          {
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileCharData[i + j * maxTileWidth].SetU8At( tileIndex, tile.Chars[i, j].Character );
              tileColorData[i + j * maxTileWidth].SetU8At( tileIndex, tile.Chars[i, j].Color );
            }
            ++tileIndex;
          }
        }
      }

      StringBuilder sb = new StringBuilder();

      sb.AppendLine( LabelPrefix + "NUM_TILES = " + Tiles.Count );
      sb.AppendLine();
      if ( ( Settings.Assembly.MapSizeCommentEnabled ) && ( !string.IsNullOrEmpty( Settings.Assembly.CommentChars ) ) )
      {
        for ( int i = 0; i < Tiles.Count; ++i )
        {
          sb.AppendLine( Settings.Assembly.CommentChars + " " + i.ToString( "D2" ) + ": " + Tiles[i].Name );
        }
      }

      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          sb.Append( LabelPrefix + "TILE_CHARS_" );
          sb.Append( i );
          sb.Append( "_" );
          sb.Append( j );
          sb.AppendLine();

          sb.AppendLine( Util.ToASMData( tileCharData[i + j * maxTileWidth], WrapData, WrapByteCount, DataByteDirective ) );
          sb.AppendLine();
        }
      }
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          sb.Append( LabelPrefix + "TILE_COLORS_" );
          sb.Append( i );
          sb.Append( "_" );
          sb.Append( j );
          sb.AppendLine();

          sb.AppendLine( Util.ToASMData( tileColorData[i + j * maxTileWidth], WrapData, WrapByteCount, DataByteDirective ) );
          sb.AppendLine();
        }
      }
      TileData = sb.ToString();
      return true;
    }



    public bool ExportTileNamesAsAssembly( out string TileData, string LabelPrefix )
    {
      TileData = "";

      var sb = new StringBuilder();

      var usedLabels = new Dictionary<string, int>();

      string  prefix = NormalizeAsLabel( LabelPrefix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        sb.Append( prefix );
        sb.Append( "TILE_NAME_" );

        string    normalizedLabel = NormalizeAsLabel( Tiles[i].Name ).ToUpper();
        if ( usedLabels.ContainsKey( normalizedLabel ) )
        {
          int   subIndex = usedLabels[normalizedLabel] + 1;

          usedLabels[normalizedLabel] = subIndex;
          normalizedLabel += "_" + subIndex;
        }
        else
        {
          usedLabels.Add( normalizedLabel, 1 );
        }

        sb.Append( normalizedLabel );
        sb.Append( "=" );
        sb.Append( i );
        sb.AppendLine();
      }
      TileData = sb.ToString();
      return true;
    }



    public bool ExportTileDataAsAssembly( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      var sbTileChars = new StringBuilder();
      var sbTileColors = new StringBuilder();

      int tileIndex = 0;
      foreach ( var tile in Tiles )
      {
        sbTileChars.Append( LabelPrefix );
        sbTileChars.Append( '_' );
        sbTileChars.Append( tileIndex );
        sbTileChars.AppendLine( "_CHARS" );
        sbTileChars.Append( DataByteDirective );
        sbTileChars.Append( ' ' );

        sbTileColors.Append( LabelPrefix );
        sbTileColors.Append( '_' );
        sbTileColors.Append( tileIndex );
        sbTileColors.AppendLine( "_COLORS" );
        sbTileColors.Append( DataByteDirective );
        sbTileColors.Append( ' ' );
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            sbTileChars.Append( "$" );
            sbTileChars.Append( tile.Chars[i, j].Character.ToString( "X2" ) );
            sbTileColors.Append( "$" );
            sbTileColors.Append( tile.Chars[i, j].Color.ToString( "X2" ) );

            if ( ( i + 1 < tile.Chars.Width )
            ||   ( j + 1 < tile.Chars.Height ) )
            {
              sbTileChars.Append( ',' );
              sbTileColors.Append( ',' );
            }
          }
        }
        sbTileChars.AppendLine();
        sbTileColors.AppendLine();
        ++tileIndex;
      }

      var sb = new StringBuilder();

      TileData = sb.ToString() + sbTileChars.ToString() + sbTileColors.ToString();
      return true;
    }



    public bool ExportMapsAsAssembly( bool Vertical, out string MapData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective, string CommentChars )
    {
      bool hasExtraData = false;
      foreach ( var map in Maps )
      {
        if ( map.ExtraDataText.Length > 0 )
        {
          hasExtraData = true;
          break;
        }
      }

      StringBuilder sbMaps = new StringBuilder();

      sbMaps.Append( LabelPrefix );
      sbMaps.Append( "NUM_MAPS = " );
      sbMaps.AppendLine( Maps.Count.ToString() );

      sbMaps.Append( LabelPrefix );
      sbMaps.AppendLine( "MAP_LIST_LO" );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        sbMaps.Append( DataByteDirective );
        sbMaps.Append( ' ' );
        sbMaps.AppendLine( "<" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
      }
      sbMaps.AppendLine();
      sbMaps.Append( LabelPrefix );
      sbMaps.AppendLine( "MAP_LIST_HI" );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        sbMaps.Append( DataByteDirective );
        sbMaps.Append( ' ' );
        sbMaps.AppendLine( ">" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
      }
      sbMaps.AppendLine();

      if ( ( Settings.Assembly.ExportMapAsCharAndColors )
      &&   ( Settings.Assembly.ExportMapColors ) )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAPS_COLOR_TABLE_LOW" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) + "_COLOR" );
        }
        sbMaps.AppendLine();
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAPS_COLOR_TABLE_HIGH" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
        }
        sbMaps.AppendLine();
      }

      if ( Settings.Assembly.ExportPassableBitfields )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAPS_PASSABLE_BITS_TABLE_LOW" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) + "_PASSABLE_BITS" );
        }
        sbMaps.AppendLine();
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAPS_PASSABLE_BITS_TABLE_HIGH" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( ">" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) + "_PASSABLE_BITS" );
        }
        sbMaps.AppendLine();
      }

      if ( hasExtraData )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_LO" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_HI" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( ">" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.AppendLine();
      }

      // Marker Tables
      AppendMarkerGlobalTables( sbMaps, LabelPrefix, DataByteDirective, Settings.Assembly.ExportHex );

      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];

        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_" + NormalizeAsLabel( map.Name.ToUpper() ) );

        GR.Memory.ByteBuffer mapDataBuffer = new GR.Memory.ByteBuffer();
        GR.Memory.ByteBuffer mapColorBuffer = new GR.Memory.ByteBuffer();
        int     exportWidth = map.Tiles.Width;
        int     exportHeight = map.Tiles.Height;

        if ( Settings.Assembly.ExportMapAsCharAndColors )
        {
          exportWidth = 0;
          exportHeight = 0;
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              int tileIndex = GetExportTileIndex( map.Tiles[x, y] );
              if ( ( tileIndex >= 0 )
              &&   ( tileIndex < Tiles.Count ) )
              {
                var tile = Tiles[tileIndex];
                if ( x * map.TileSpacingX + tile.Chars.Width > exportWidth )
                {
                  exportWidth = x * map.TileSpacingX + tile.Chars.Width;
                }
                if ( y * map.TileSpacingY + tile.Chars.Height > exportHeight )
                {
                  exportHeight = y * map.TileSpacingY + tile.Chars.Height;
                }
              }
            }
          }
          if ( exportWidth == 0 )
          {
             exportWidth = map.Tiles.Width * map.TileSpacingX;
          }
          if ( exportHeight == 0 )
          {
             exportHeight = map.Tiles.Height * map.TileSpacingY;
          }
          mapDataBuffer.Resize( (uint)( exportWidth * exportHeight ) );
          if ( Settings.Assembly.ExportMapColors )
          {
            mapColorBuffer.Resize( (uint)( exportWidth * exportHeight ) );
          }
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              int tileIndex = GetExportTileIndex( map.Tiles[x, y] );
              if ( ( tileIndex >= 0 )
              &&   ( tileIndex < Tiles.Count )
              &&   ( tileIndex != Settings.Assembly.EmptyTileIndex ) )
              {
                var tile = Tiles[tileIndex];
                for ( int ty = 0; ty < tile.Chars.Height; ++ty )
                {
                  for ( int tx = 0; tx < tile.Chars.Width; ++tx )
                  {
                    int finalX = x * map.TileSpacingX + tx;
                    int finalY = y * map.TileSpacingY + ty;

                    if ( ( finalX < exportWidth )
                    &&   ( finalY < exportHeight ) )
                    {
                      int offset = 0;
                      if ( Vertical )
                      {
                        offset = finalX * exportHeight + finalY;
                      }
                      else
                      {
                        offset = finalX + finalY * exportWidth;
                      }

                      mapDataBuffer.SetU8At( offset, tile.Chars[tx, ty].Character );
                      if ( Settings.Assembly.ExportMapColors )
                      {
                        mapColorBuffer.SetU8At( offset, tile.Chars[tx, ty].Color );
                      }
                    }
                  }
                }
              }
            }
          }
        }
        else
        {
          mapDataBuffer.Resize( (uint)( exportWidth * exportHeight ) );
          if ( Vertical )
          {
            for ( int y = 0; y < map.Tiles.Height; ++y )
            {
              for ( int x = 0; x < map.Tiles.Width; ++x )
              {
                mapDataBuffer.SetU8At( x * map.Tiles.Height + y, (byte)GetExportTileIndex( map.Tiles[x, y] ) );
              }
            }
          }
          else
          {
            for ( int y = 0; y < map.Tiles.Height; ++y )
            {
              for ( int x = 0; x < map.Tiles.Width; ++x )
              {
                mapDataBuffer.SetU8At( x + y * map.Tiles.Width, (byte)GetExportTileIndex( map.Tiles[x, y] ) );
              }
            }
          }
        }
        sbMaps.Append( DataByteDirective );
        if ( !DataByteDirective.EndsWith( " " ) )
        {
          sbMaps.Append( ' ' );
        }
        sbMaps.Append( "$" );
        sbMaps.Append( ( (byte)exportWidth ).ToString( "X2" ) );
        sbMaps.Append( ", $" );
        sbMaps.Append( ( (byte)exportHeight ).ToString( "X2" ) );
        if ( !string.IsNullOrEmpty( CommentChars ) )
        {
          sbMaps.Append( ' ' );
          sbMaps.Append( CommentChars );
          if ( !CommentChars.EndsWith( " " ) )
          {
            sbMaps.Append( ' ' );
          }
          sbMaps.Append( "map width, height" );
        }
        sbMaps.AppendLine();
        sbMaps.AppendLine();
        sbMaps.Append( Util.ToASMData( mapDataBuffer, WrapData, WrapByteCount, DataByteDirective ) );
        if ( ( Settings.Assembly.ExportMapAsCharAndColors )
        &&   ( Settings.Assembly.ExportMapColors ) )
        {
          sbMaps.AppendLine();
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_" + NormalizeAsLabel( map.Name.ToUpper() ) + "_COLOR" );
          sbMaps.Append( Util.ToASMData( mapColorBuffer, WrapData, WrapByteCount, DataByteDirective ) );
        }
        if ( Settings.Assembly.ExportPassableBitfields )
        {
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_" + NormalizeAsLabel( map.Name.ToUpper() ) + "_PASSABLE_BITS" );

           bool[] passable = new bool[exportWidth * exportHeight];
           for ( int idx = 0; idx < passable.Length; ++idx )
           {
             passable[idx] = true;
           }
           for ( int ty = 0; ty < map.Tiles.Height; ++ty )
           {
             for ( int tx = 0; tx < map.Tiles.Width; ++tx )
             {
               int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
               if ( ( tileIndex >= 0 )
               &&   ( tileIndex < Tiles.Count )
               &&   ( !Tiles[tileIndex].Passable ) )
               {
                 var tile = Tiles[tileIndex];
                 for ( int cy = 0; cy < tile.Chars.Height; ++cy )
                 {
                   for ( int cx = 0; cx < tile.Chars.Width; ++cx )
                   {
                     int px = tx * map.TileSpacingX + cx;
                     int py = ty * map.TileSpacingY + cy;
                     if ( ( px < exportWidth )
                     &&   ( py < exportHeight ) )
                     {
                       passable[py * exportWidth + px] = false;
                     }
                   }
                 }
               }
             }
           }

           // Per-character one-way "blocked" override: anywhere the
           // user marked a char as blocked, force it impassable in the
           // bitfield. (false in CharBlockedOverrides means "no
           // override" — the per-tile loop above is already authoritative
           // for those.) This is the only way to flip false → in the
           // exported bits; it cannot turn an impassable char passable.
           if ( ( map.CharBlockedOverrides.Width  > 0 )
           &&   ( map.CharBlockedOverrides.Height > 0 ) )
           {
             int ovW = System.Math.Min( map.CharBlockedOverrides.Width,  exportWidth );
             int ovH = System.Math.Min( map.CharBlockedOverrides.Height, exportHeight );
             for ( int cy = 0; cy < ovH; ++cy )
             {
               for ( int cx = 0; cx < ovW; ++cx )
               {
                 if ( map.CharBlockedOverrides[cx, cy] )
                 {
                   passable[cy * exportWidth + cx] = false;
                 }
               }
             }
           }

           GR.Memory.ByteBuffer bitfieldData = new GR.Memory.ByteBuffer();
           for ( int y = 0; y < exportHeight; ++y )
           {
             int     currentX = 0;
             while ( currentX < exportWidth )
             {
               byte   bits = 0;
               for ( int j = 0; j < 8; ++j )
               {
                 if ( ( currentX + j < exportWidth )
                 &&   ( passable[y * exportWidth + currentX + j] ) )
                 {
                   bits |= (byte)( 1 << ( 7 - j ) );
                 }
               }
               bitfieldData.AppendU8( bits );
               currentX += 8;
             }
           }
 
           if ( Settings.Assembly.ExportPassableBitfieldsAsBinary )
           {
             int bytesPerLine = WrapData ? WrapByteCount : int.MaxValue;
             int bytesWritten = 0;
             while ( bytesWritten < bitfieldData.Length )
             {
                int bytesToOutput = Math.Min( bytesPerLine, (int)bitfieldData.Length - bytesWritten );
                
                sbMaps.Append( DataByteDirective );
                for ( int k = 0; k < bytesToOutput; ++k )
                {
                  if ( k > 0 ) sbMaps.Append( "," );
                  sbMaps.Append( " %" + Convert.ToString( bitfieldData.ByteAt( bytesWritten + k ), 2 ).PadLeft( 8, '0' ) );
                }
                sbMaps.AppendLine();
                bytesWritten += bytesToOutput;
             }
           }
           else
           {
             sbMaps.Append( Util.ToASMData( bitfieldData, WrapData, WrapByteCount, DataByteDirective ) );
           }
        }

        if ( hasExtraData )
        //&&   ( map.ExtraDataText.Length > 0 ) )
        {
          sbMaps.AppendLine( ";extra data" );
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_EXTRA_DATA_" + NormalizeAsLabel( map.Name.ToUpper() ) );

          // clean extra data
          GR.Memory.ByteBuffer    extraData = new GR.Memory.ByteBuffer();
          string[]  lines = map.ExtraDataText.Split( new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
          foreach ( string line in lines )
          {
            string    tempLine = line.Trim().Replace( " ", "" );
            if ( ( !tempLine.StartsWith( ";" ) )
            &&   ( !tempLine.StartsWith( "#" ) )
            &&   ( !tempLine.StartsWith( "//" ) ) )
            {
              extraData.AppendHex( tempLine );
            }
          }

          sbMaps.Append( Util.ToASMData( extraData, WrapData, WrapByteCount, DataByteDirective ) );
          sbMaps.AppendLine();
        }
      }

      MapData = sbMaps.ToString();
      return true;
    }



    private string NormalizeAsLabel( string Label )
    {
      StringBuilder   sb = new StringBuilder();

      // remove diacritics
      string normalizedString = Label.Normalize( NormalizationForm.FormD );

      foreach ( var c in normalizedString )
      {
        if ( CharUnicodeInfo.GetUnicodeCategory( c ) == UnicodeCategory.NonSpacingMark )
        {
          continue;
        }
        if ( ( !char.IsDigit( c ) )
        &&   ( !char.IsLetter( c ) )
        &&   ( c != '_' ) )
        {
          sb.Append( '_' );
        }
        else
        {
          sb.Append( c );
        }
      }
      return sb.ToString();
    }



    public bool ExportMapExtraDataAsAssembly( out string MapData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      bool hasExtraData = false;
      foreach ( var map in Maps )
      {
        if ( map.ExtraDataText.Length > 0 )
        {
          hasExtraData = true;
          break;
        }
      }

      StringBuilder sbMaps = new StringBuilder();

      sbMaps.Append( LabelPrefix );
      sbMaps.Append( "NUM_MAPS = " );
      sbMaps.AppendLine( Maps.Count.ToString() );

      if ( hasExtraData )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_LO" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_HI" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( ">" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.AppendLine();
      }


      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];

        if ( hasExtraData )
        //&&   ( map.ExtraDataText.Length > 0 ) )
        {
          sbMaps.AppendLine( ";extra data" );
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_EXTRA_DATA_" + NormalizeAsLabel( map.Name.ToUpper() ) );

          // clean extra data
          GR.Memory.ByteBuffer    extraData = new GR.Memory.ByteBuffer();
          string[]  lines = map.ExtraDataText.Split( new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
          foreach ( string line in lines )
          {
            string    tempLine = line.Trim().Replace( " ", "" );
            if ( ( !tempLine.StartsWith( ";" ) )
            &&   ( !tempLine.StartsWith( "#" ) )
            &&   ( !tempLine.StartsWith( "//" ) ) )
            {
              extraData.AppendHex( tempLine );
            }
          }



          sbMaps.Append( Util.ToASMData( extraData, WrapData, WrapByteCount, DataByteDirective ) );
          sbMaps.AppendLine();
        }

        // Markers
        AppendMarkerMapData( sbMaps, map, i, LabelPrefix, DataByteDirective, Settings.Assembly.ExportHex );
      }

      MapData = sbMaps.ToString();
      return true;
    }



    internal void ExportTilesAsBuffer( bool RowByRow, out GR.Memory.ByteBuffer TileData )
    {
      TileData = new GR.Memory.ByteBuffer();

      foreach ( Formats.MapProject.Tile tile in Tiles )
      {
        if ( RowByRow )
        {
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              TileData.AppendU8( (byte)tile.Chars[i, j].Character );
              TileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
          }
        }
        else
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            for ( int j = 0; j < tile.Chars.Height; ++j )            
            {
              TileData.AppendU8( (byte)tile.Chars[i, j].Character );
              TileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
          }
        }
      }
    }



    public GR.Memory.ByteBuffer ExportAsGameBinary( bool ExportMarkers, bool ExportColors, bool ExportPassable, ushort BaseAddress = 0 )
    {
      var buf = new GR.Memory.ByteBuffer();
      int addrBase = BaseAddress;

      // ========== HEADER (57 bytes, 0x39) ==========
      buf.AppendU8( 7 );    // +$00 marker_stride (bytes per marker record: tag, x, y, value1, value2, flags, group_id) — flags is a bitfield: bit0 = Enabled, bit1 = Triggered
      buf.AppendU8( (byte)Tiles.Count );  // +$01
      buf.AppendU8( (byte)Maps.Count );   // +$02
      // 21 x 2-byte offset placeholders (+$03 .. +$2C)
      for ( int i = 0; i < 21; ++i )
        buf.AppendU16( 0 );
      buf.AppendU8( 8 );    // +$2D entity_stride (bytes per entity record: tag, x, y, tile, value1, value2, enabled, triggered)
      // 3 x 2-byte entity offset placeholders (+$2E .. +$33)
      for ( int i = 0; i < 3; ++i )
        buf.AppendU16( 0 );
      // Map-strings section (v24+): a single byte count followed by two
      // 2-byte pointers to the MAP_STRING_LO and MAP_STRING_HI tables.
      // Always emitted — even when the project has no strings — so the
      // header layout is fixed at 57 bytes regardless of project content.
      buf.AppendU8( 0 );        // +$34 map_string_count (patched below)
      buf.AppendU16( 0 );       // +$35 offset_map_string_lo (patched below)
      buf.AppendU16( 0 );       // +$37 offset_map_string_hi (patched below)

      // Header offset positions (byte offset within header for each pointer)
      const int HDR_TILES_WIDTH       = 0x03;
      const int HDR_TILES_HEIGHT      = 0x05;
      const int HDR_TILES_FLAGS       = 0x07;
      const int HDR_TILE_CHAR_OFF_LO  = 0x09;
      const int HDR_TILE_CHAR_OFF_HI  = 0x0B;
      const int HDR_TILE_COLOR_OFF_LO = 0x0D;
      const int HDR_TILE_COLOR_OFF_HI = 0x0F;
      const int HDR_MAP_WIDTH         = 0x11;
      const int HDR_MAP_HEIGHT        = 0x13;
      const int HDR_MAP_BG_COLOR      = 0x15;
      const int HDR_MAP_MC1_COLOR     = 0x17;
      const int HDR_MAP_MC2_COLOR     = 0x19;
      const int HDR_MAP_MARKER_COUNT  = 0x1B;
      const int HDR_MAP_CHAR_GRID_LO  = 0x1D;
      const int HDR_MAP_CHAR_GRID_HI  = 0x1F;
      const int HDR_MAP_COLOR_GRID_LO = 0x21;
      const int HDR_MAP_COLOR_GRID_HI = 0x23;
      const int HDR_MAP_PASSABLE_LO   = 0x25;
      const int HDR_MAP_PASSABLE_HI   = 0x27;
      const int HDR_MAP_MARKERS_LO    = 0x29;
      const int HDR_MAP_MARKERS_HI    = 0x2B;
      const int HDR_MAP_ENTITY_COUNT  = 0x2E;
      const int HDR_MAP_ENTITIES_LO   = 0x30;
      const int HDR_MAP_ENTITIES_HI   = 0x32;
      const int HDR_MAP_STRING_COUNT  = 0x34;
      const int HDR_MAP_STRING_LO     = 0x35;
      const int HDR_MAP_STRING_HI     = 0x37;

      // ========== TILE ARRAYS ==========

      // tiles_width[]
      buf.SetU16At( HDR_TILES_WIDTH, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( (byte)Tiles[t].Chars.Width );

      // tiles_height[]
      buf.SetU16At( HDR_TILES_HEIGHT, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( (byte)Tiles[t].Chars.Height );

      // tiles_flags[]
      buf.SetU16At( HDR_TILES_FLAGS, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( (byte)( Tiles[t].Passable ? 1 : 0 ) );

      // Build tile char and color blobs
      var tileCharBlobs = new List<GR.Memory.ByteBuffer>();
      var tileColorBlobs = new List<GR.Memory.ByteBuffer>();
      for ( int t = 0; t < Tiles.Count; ++t )
      {
        var tile = Tiles[t];
        var charBlob = new GR.Memory.ByteBuffer();
        var colorBlob = new GR.Memory.ByteBuffer();
        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            charBlob.AppendU8( tile.Chars[x, y].Character );
            colorBlob.AppendU8( tile.Chars[x, y].Color );
          }
        }
        tileCharBlobs.Add( charBlob );
        tileColorBlobs.Add( colorBlob );
      }

      // tile_char_offset_lo[] — placeholders, will patch with absolute offsets
      int tileCharOffLoPos = (int)buf.Length;
      buf.SetU16At( HDR_TILE_CHAR_OFF_LO, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( 0 );

      // tile_char_offset_hi[]
      int tileCharOffHiPos = (int)buf.Length;
      buf.SetU16At( HDR_TILE_CHAR_OFF_HI, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( 0 );

      // tile_color_offset_lo[]
      int tileColorOffLoPos = (int)buf.Length;
      buf.SetU16At( HDR_TILE_COLOR_OFF_LO, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( 0 );

      // tile_color_offset_hi[]
      int tileColorOffHiPos = (int)buf.Length;
      buf.SetU16At( HDR_TILE_COLOR_OFF_HI, (ushort)( buf.Length + addrBase ) );
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.AppendU8( 0 );

      // Tile char data (concatenated) — patch offset tables
      int tileCharDataStart = (int)buf.Length;
      int runningOffset = 0;
      for ( int t = 0; t < Tiles.Count; ++t )
      {
        int absAddr = tileCharDataStart + runningOffset + addrBase;
        buf.SetU8At( tileCharOffLoPos + t, (byte)( absAddr & 0xFF ) );
        buf.SetU8At( tileCharOffHiPos + t, (byte)( ( absAddr >> 8 ) & 0xFF ) );
        runningOffset += (int)tileCharBlobs[t].Length;
      }
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.Append( tileCharBlobs[t] );

      // Tile color data (concatenated) — patch offset tables
      int tileColorDataStart = (int)buf.Length;
      runningOffset = 0;
      for ( int t = 0; t < Tiles.Count; ++t )
      {
        int absAddr = tileColorDataStart + runningOffset + addrBase;
        buf.SetU8At( tileColorOffLoPos + t, (byte)( absAddr & 0xFF ) );
        buf.SetU8At( tileColorOffHiPos + t, (byte)( ( absAddr >> 8 ) & 0xFF ) );
        runningOffset += (int)tileColorBlobs[t].Length;
      }
      for ( int t = 0; t < Tiles.Count; ++t )
        buf.Append( tileColorBlobs[t] );

      // ========== MAP METADATA ARRAYS ==========

      // Pre-compute char-level dimensions and marker counts for all maps
      int[] exportWidths = new int[Maps.Count];
      int[] exportHeights = new int[Maps.Count];
      int[] markerCounts = new int[Maps.Count];
      int[] entityCounts = new int[Maps.Count];
      for ( int m = 0; m < Maps.Count; ++m )
      {
        var map = Maps[m];
        int ew = map.Tiles.Width * map.TileSpacingX;
        int eh = map.Tiles.Height * map.TileSpacingY;
        for ( int ty = 0; ty < map.Tiles.Height; ++ty )
        {
          for ( int tx = 0; tx < map.Tiles.Width; ++tx )
          {
            int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
            if ( ( tileIndex >= 0 ) && ( tileIndex < Tiles.Count ) )
            {
              var tile = Tiles[tileIndex];
              int w = tx * map.TileSpacingX + tile.Chars.Width;
              int h = ty * map.TileSpacingY + tile.Chars.Height;
              if ( w > ew ) ew = w;
              if ( h > eh ) eh = h;
            }
          }
        }
        exportWidths[m] = ew;
        exportHeights[m] = eh;
        markerCounts[m] = ExportMarkers ? map.Markers.Count : 0;
        entityCounts[m] = map.Entities.Count;
      }

      // map_width[]
      buf.SetU16At( HDR_MAP_WIDTH, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)exportWidths[m] );

      // map_height[]
      buf.SetU16At( HDR_MAP_HEIGHT, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)exportHeights[m] );

      // map_bg_color[]
      buf.SetU16At( HDR_MAP_BG_COLOR, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)( Maps[m].AlternativeBackgroundColor >= 0 ? Maps[m].AlternativeBackgroundColor : BackgroundColor ) );

      // map_mc1_color[]
      buf.SetU16At( HDR_MAP_MC1_COLOR, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)( Maps[m].AlternativeMultiColor1 >= 0 ? Maps[m].AlternativeMultiColor1 : MultiColor1 ) );

      // map_mc2_color[]
      buf.SetU16At( HDR_MAP_MC2_COLOR, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)( Maps[m].AlternativeMultiColor2 >= 0 ? Maps[m].AlternativeMultiColor2 : MultiColor2 ) );

      // map_marker_count[]
      buf.SetU16At( HDR_MAP_MARKER_COUNT, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)markerCounts[m] );

      // map_entity_count[]
      buf.SetU16At( HDR_MAP_ENTITY_COUNT, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m )
        buf.AppendU8( (byte)entityCounts[m] );

      // ========== MAP DATA LOOKUP TABLES (placeholders) ==========

      int mapCharGridLoPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_CHAR_GRID_LO, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );
      int mapCharGridHiPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_CHAR_GRID_HI, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );

      int mapColorGridLoPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_COLOR_GRID_LO, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );
      int mapColorGridHiPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_COLOR_GRID_HI, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );

      int mapPassableLoPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_PASSABLE_LO, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );
      int mapPassableHiPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_PASSABLE_HI, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );

      int mapMarkersLoPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_MARKERS_LO, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );
      int mapMarkersHiPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_MARKERS_HI, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );

      int mapEntitiesLoPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_ENTITIES_LO, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );
      int mapEntitiesHiPos = (int)buf.Length;
      buf.SetU16At( HDR_MAP_ENTITIES_HI, (ushort)( buf.Length + addrBase ) );
      for ( int m = 0; m < Maps.Count; ++m ) buf.AppendU8( 0 );

      // ========== PER-MAP VARIABLE DATA ==========

      for ( int m = 0; m < Maps.Count; ++m )
      {
        var map = Maps[m];
        int ew = exportWidths[m];
        int eh = exportHeights[m];

        // Build char grid and color grid
        var charGrid = new byte[ew * eh];
        var colorGrid = new byte[ew * eh];
        for ( int ty = 0; ty < map.Tiles.Height; ++ty )
        {
          for ( int tx = 0; tx < map.Tiles.Width; ++tx )
          {
            int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
            if ( ( tileIndex >= 0 )
            &&   ( tileIndex < Tiles.Count )
            &&   ( tileIndex != Settings.Assembly.EmptyTileIndex || !Settings.Assembly.EmptyTileCompressionEnabled ) )
            {
              var tile = Tiles[tileIndex];
              // Per-CHARACTER color override: each char of the tile
              // placement has its own slot in TileColorOverrides indexed
              // by character coords. -1 means "use the tile's intrinsic
              // colour for this character"; 0..15 means "paint this
              // single character in that C64 colour".
              for ( int cy = 0; cy < tile.Chars.Height; ++cy )
              {
                for ( int cx = 0; cx < tile.Chars.Width; ++cx )
                {
                  int finalX = tx * map.TileSpacingX + cx;
                  int finalY = ty * map.TileSpacingY + cy;
                  if ( ( finalX < ew ) && ( finalY < eh ) )
                  {
                    int charOverride = -1;
                    if ( ( finalX < map.TileColorOverrides.Width )
                    &&   ( finalY < map.TileColorOverrides.Height ) )
                    {
                      charOverride = map.TileColorOverrides[finalX, finalY];
                    }
                    int off = finalX + finalY * ew;
                    charGrid[off] = tile.Chars[cx, cy].Character;
                    colorGrid[off] = ( charOverride >= 0 )
                                     ? (byte)charOverride
                                     : tile.Chars[cx, cy].Color;
                  }
                }
              }
            }
          }
        }

        // Write char grid, patch lookup table
        int charGridAddr = (int)buf.Length + addrBase;
        buf.SetU8At( mapCharGridLoPos + m, (byte)( charGridAddr & 0xFF ) );
        buf.SetU8At( mapCharGridHiPos + m, (byte)( ( charGridAddr >> 8 ) & 0xFF ) );
        for ( int i = 0; i < charGrid.Length; ++i )
          buf.AppendU8( charGrid[i] );

        // Write color grid
        if ( ExportColors )
        {
          int colorGridAddr = (int)buf.Length + addrBase;
          buf.SetU8At( mapColorGridLoPos + m, (byte)( colorGridAddr & 0xFF ) );
          buf.SetU8At( mapColorGridHiPos + m, (byte)( ( colorGridAddr >> 8 ) & 0xFF ) );
          for ( int i = 0; i < colorGrid.Length; ++i )
            buf.AppendU8( colorGrid[i] );
        }

        // Write passable bits
        if ( ExportPassable )
        {
          int passableAddr = (int)buf.Length + addrBase;
          buf.SetU8At( mapPassableLoPos + m, (byte)( passableAddr & 0xFF ) );
          buf.SetU8At( mapPassableHiPos + m, (byte)( ( passableAddr >> 8 ) & 0xFF ) );

          bool[] passable = new bool[ew * eh];
          for ( int idx = 0; idx < passable.Length; ++idx )
            passable[idx] = true;
          for ( int ty = 0; ty < map.Tiles.Height; ++ty )
          {
            for ( int tx = 0; tx < map.Tiles.Width; ++tx )
            {
              int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
              if ( ( tileIndex >= 0 ) && ( tileIndex < Tiles.Count ) && ( !Tiles[tileIndex].Passable ) )
              {
                var tile = Tiles[tileIndex];
                for ( int cy = 0; cy < tile.Chars.Height; ++cy )
                {
                  for ( int cx = 0; cx < tile.Chars.Width; ++cx )
                  {
                    int px = tx * map.TileSpacingX + cx;
                    int py = ty * map.TileSpacingY + cy;
                    if ( ( px < ew ) && ( py < eh ) )
                      passable[py * ew + px] = false;
                  }
                }
              }
            }
          }
          // Per-character one-way "blocked" override pass — see the
          // matching block in ExportAsAssembly for full rationale. true
          // forces impassable; false defers to the tile-driven decision
          // above. Cannot turn an impassable char passable.
          if ( ( map.CharBlockedOverrides.Width  > 0 )
          &&   ( map.CharBlockedOverrides.Height > 0 ) )
          {
            int ovW = System.Math.Min( map.CharBlockedOverrides.Width,  ew );
            int ovH = System.Math.Min( map.CharBlockedOverrides.Height, eh );
            for ( int cy = 0; cy < ovH; ++cy )
            {
              for ( int cx = 0; cx < ovW; ++cx )
              {
                if ( map.CharBlockedOverrides[cx, cy] )
                {
                  passable[cy * ew + cx] = false;
                }
              }
            }
          }
          for ( int y = 0; y < eh; ++y )
          {
            int currentX = 0;
            while ( currentX < ew )
            {
              byte bits = 0;
              for ( int bitIndex = 0; bitIndex < 8; ++bitIndex )
              {
                if ( ( currentX + bitIndex < ew ) && ( passable[y * ew + currentX + bitIndex] ) )
                  bits |= (byte)( 1 << ( 7 - bitIndex ) );
              }
              buf.AppendU8( bits );
              currentX += 8;
            }
          }
        }

        // Write markers — sorted by TagID (ascending), so lower-numbered types
        // come first and higher-numbered types come last. This lets the engine
        // rely on grouping for early-out scans or bucket-by-tag dispatch.
        if ( ExportMarkers )
        {
          int markersAddr = (int)buf.Length + addrBase;
          buf.SetU8At( mapMarkersLoPos + m, (byte)( markersAddr & 0xFF ) );
          buf.SetU8At( mapMarkersHiPos + m, (byte)( ( markersAddr >> 8 ) & 0xFF ) );

          // Precompute (marker, tagId) pairs once, then sort.
          var markerPairs = new List<KeyValuePair<Marker, byte>>( map.Markers.Count );
          foreach ( var marker in map.Markers )
          {
            byte tagId = 0;
            foreach ( var mt in MarkerTypes )
            {
              if ( mt.ID == marker.Type )
              {
                tagId = (byte)mt.TagID;
                break;
              }
            }
            markerPairs.Add( new KeyValuePair<Marker, byte>( marker, tagId ) );
          }
          markerPairs.Sort( ( a, b ) => a.Value.CompareTo( b.Value ) );

          foreach ( var pair in markerPairs )
          {
            var marker = pair.Key;
            byte tagId = pair.Value;
            buf.AppendU8( tagId );
            buf.AppendU8( (byte)marker.X );
            buf.AppendU8( (byte)marker.Y );
            buf.AppendU8( marker.Value1 );
            buf.AppendU8( marker.Value2 );
            // Packed flags byte: bit 0 = Enabled, bit 1 = Triggered.
            // Mask constants are emitted into the asm sidecar as
            // MAP_MARKER_FLAGS_MASK_ENABLED / MAP_MARKER_FLAGS_MASK_TRIGGERED.
            byte flags = 0;
            if ( marker.Enabled )   flags |= 0x01;
            if ( marker.Triggered ) flags |= 0x02;
            buf.AppendU8( flags );
            buf.AppendU8( marker.GroupId );
          }
        }

        // Write entities — sorted by TagID (ascending), matching marker sort.
        if ( map.Entities.Count > 0 )
        {
          int entitiesAddr = (int)buf.Length + addrBase;
          buf.SetU8At( mapEntitiesLoPos + m, (byte)( entitiesAddr & 0xFF ) );
          buf.SetU8At( mapEntitiesHiPos + m, (byte)( ( entitiesAddr >> 8 ) & 0xFF ) );

          // Precompute (entity, tagId, tileIndex) once so we can sort + emit
          // without re-doing the EntityType lookup per field.
          var entityTriples = new List<KeyValuePair<Entity, ushort>>( map.Entities.Count );
          foreach ( var entity in map.Entities )
          {
            byte tagId = 0;
            byte tileIdx = 0;
            foreach ( var et in EntityTypes )
            {
              if ( et.ID == entity.Type )
              {
                tagId = (byte)et.TagID;
                tileIdx = (byte)et.TileIndex;
                break;
              }
            }
            // Pack (tagId, tileIdx) as ushort so one .Sort call does what we need.
            entityTriples.Add( new KeyValuePair<Entity, ushort>( entity, (ushort)( ( tagId << 8 ) | tileIdx ) ) );
          }
          entityTriples.Sort( ( a, b ) => ( a.Value >> 8 ).CompareTo( b.Value >> 8 ) );

          foreach ( var pair in entityTriples )
          {
            var entity = pair.Key;
            byte tagId   = (byte)( pair.Value >> 8 );
            byte tileIdx = (byte)( pair.Value & 0xff );
            buf.AppendU8( tagId );
            buf.AppendU8( (byte)entity.X );
            buf.AppendU8( (byte)entity.Y );
            buf.AppendU8( tileIdx );
            buf.AppendU8( entity.Value1 );
            buf.AppendU8( entity.Value2 );
            buf.AppendU8( (byte)( entity.Enabled ? 1 : 0 ) );
            buf.AppendU8( (byte)( entity.Triggered ? 1 : 0 ) );
          }
        }
      }

      // ========== MAP STRINGS (v24+) ==========
      //
      // Layout, written sequentially after all per-map data:
      //   1. MAP_STRING_LO table — N bytes, low byte of each string's address
      //   2. MAP_STRING_HI table — N bytes, high byte of each string's address
      //   3. The N concatenated byte streams (one per emitted string)
      //
      // The header pointers at +$35 / +$37 hold the absolute addresses of
      // the LO and HI tables; +$34 holds N. When the project has no
      // emittable strings the count is 0 and the LO/HI pointers stay
      // zero (the per-map empty-data convention).
      List<string> mapStringSkipReasons;
      var emittableStrings = GetEmittableMapStrings( out mapStringSkipReasons );
      if ( emittableStrings.Count > 0 )
      {
        // Pre-build every byte stream so we know each one's exact length
        // before we start patching pointers.
        var streams = new List<GR.Memory.ByteBuffer>( emittableStrings.Count );
        foreach ( var ms in emittableStrings )
        {
          streams.Add( BuildMapStringByteStream(
            ms,
            MapStringsLowercaseIndex,
            MapStringsUppercaseIndex,
            MapStringsNumbersIndex,
            MapStringsTextAreaWidth ) );
        }

        // LO / HI tables get placeholder bytes; we patch them once we know
        // each stream's final address.
        int loTablePos = (int)buf.Length;
        buf.SetU16At( HDR_MAP_STRING_LO, (ushort)( loTablePos + addrBase ) );
        for ( int i = 0; i < emittableStrings.Count; ++i ) buf.AppendU8( 0 );

        int hiTablePos = (int)buf.Length;
        buf.SetU16At( HDR_MAP_STRING_HI, (ushort)( hiTablePos + addrBase ) );
        for ( int i = 0; i < emittableStrings.Count; ++i ) buf.AppendU8( 0 );

        // Now write the byte streams and patch the LO/HI pointer tables.
        for ( int i = 0; i < emittableStrings.Count; ++i )
        {
          int streamAddr = (int)buf.Length + addrBase;
          buf.SetU8At( loTablePos + i, (byte)( streamAddr & 0xFF ) );
          buf.SetU8At( hiTablePos + i, (byte)( ( streamAddr >> 8 ) & 0xFF ) );
          buf.Append( streams[i] );
        }

        // Stash the count last — clamp at 255 because the field is one byte.
        // (The asm sidecar's index constants will exceed that limit too if
        // there are more than 256 strings; that's an editor-side concern.)
        int count = emittableStrings.Count;
        if ( count > 255 ) count = 255;
        buf.SetU8At( HDR_MAP_STRING_COUNT, (byte)count );
      }

      return buf;
    }



    /// <summary>
    /// Generates a KickAssembler-style constants file that matches the exact byte
    /// layout used by <see cref="ExportAsGameBinary"/>. Intended to be saved next
    /// to the exported .bin so runtime code can include symbolic offsets instead
    /// of magic numbers. <paramref name="UserPrefix"/> is inserted verbatim at
    /// the very top of the file (e.g. for includes or namespace declarations).
    /// </summary>
    public static string GenerateGameBinaryHeaderAsm( string UserPrefix = null )
    {
      var sb = new StringBuilder();
      if ( !string.IsNullOrEmpty( UserPrefix ) )
      {
        sb.AppendLine( UserPrefix );
        if ( !UserPrefix.EndsWith( "\n" ) )
        {
          sb.AppendLine();
        }
      }
      sb.AppendLine( "// Auto-generated by C64Studio on game-binary export." );
      sb.AppendLine( "// Constants mirror the byte layout of the accompanying .bin file." );
      sb.AppendLine( "// Do not edit by hand — regenerated on every export." );
      sb.AppendLine( "//" );
      sb.AppendLine( "// All MAP_HEADER_* values are byte OFFSETS into the header, not stored" );
      sb.AppendLine( "// values. To read the stride at runtime: LDA MAP_HEADER + MAP_HEADER_MARKER_STRIDE" );
      sb.AppendLine( "// To step between marker records at compile time, use MAP_MARKER_SIZE." );
      sb.AppendLine();
      sb.AppendLine( "// ====== Game binary header (57 bytes) ======" );
      sb.AppendLine( "// Direct byte values at the start of the header:" );
      sb.AppendLine( ".const MAP_HEADER_MARKER_STRIDE                  = $00  // byte: marker record size" );
      sb.AppendLine( ".const MAP_HEADER_TILECOUNT                      = $01  // byte: number of tiles" );
      sb.AppendLine( ".const MAP_HEADER_MAPCOUNT                       = $02  // byte: number of maps" );
      sb.AppendLine();
      sb.AppendLine( "// Pointer tables (16-bit each) — absolute addresses into the data section:" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILES_WIDTH             = $03" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILES_HEIGHT            = $05" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILES_FLAGS             = $07" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILE_CHAR_OFFSET_LO     = $09" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILE_CHAR_OFFSET_HI     = $0B" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILE_COLOR_OFFSET_LO    = $0D" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_TILE_COLOR_OFFSET_HI    = $0F" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_WIDTH               = $11" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_HEIGHT              = $13" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_BG_COLOR            = $15" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_MC1_COLOR           = $17" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_MC2_COLOR           = $19" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_MARKER_COUNT        = $1B" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_CHAR_GRID_LO        = $1D" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_CHAR_GRID_HI        = $1F" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_COLOR_GRID_LO       = $21" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_COLOR_GRID_HI       = $23" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_PASSABLE_LO         = $25" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_PASSABLE_HI         = $27" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_MARKERS_LO          = $29" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_MARKERS_HI          = $2B" );
      sb.AppendLine();
      sb.AppendLine( "// Entity section (v23):" );
      sb.AppendLine( ".const MAP_HEADER_ENTITY_STRIDE                  = $2D  // byte: entity record size" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_ENTITY_COUNT        = $2E" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_ENTITIES_LO         = $30" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_ENTITIES_HI         = $32" );
      sb.AppendLine();
      sb.AppendLine( "// Map strings section (v24): per-project named text scripts. The" );
      sb.AppendLine( "// MAP_STRING_LO / MAP_STRING_HI tables and the byte streams they" );
      sb.AppendLine( "// point to live IN this binary at the offsets below. Pass an index" );
      sb.AppendLine( "// (see map_strings.asm for the named index constants) to the message" );
      sb.AppendLine( "// renderer; it reads the address from MAP_STRING_LO/HI[index] and" );
      sb.AppendLine( "// walks the byte stream until END_OF_TEXT ($FF)." );
      sb.AppendLine( ".const MAP_HEADER_MAP_STRING_COUNT               = $34  // byte: number of strings" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_STRING_LO           = $35" );
      sb.AppendLine( ".const MAP_HEADER_OFFSET_MAP_STRING_HI           = $37" );
      sb.AppendLine( ".const MAP_HEADER_SIZE                           = $39  // total header length" );
      sb.AppendLine();
      sb.AppendLine( "// ====== Marker record layout (7 bytes per marker) ======" );
      sb.AppendLine( "// Byte offsets within a single marker record." );
      sb.AppendLine( "// Use MAP_MARKER_SIZE to advance between records." );
      sb.AppendLine( ".const MAP_MARKER_TAG                            = $00" );
      sb.AppendLine( ".const MAP_MARKER_X                              = $01" );
      sb.AppendLine( ".const MAP_MARKER_Y                              = $02" );
      sb.AppendLine( ".const MAP_MARKER_VALUE1                         = $03" );
      sb.AppendLine( ".const MAP_MARKER_VALUE2                         = $04" );
      sb.AppendLine( ".const MAP_MARKER_FLAGS                          = $05  // bit flags — see masks below" );
      sb.AppendLine( ".const MAP_MARKER_GROUP_ID                       = $06" );
      sb.AppendLine( ".const MAP_MARKER_SIZE                           = $07  // bytes per marker" );
      sb.AppendLine();
      sb.AppendLine( "// MAP_MARKER_FLAGS bit masks. Test with AND, set with ORA." );
      sb.AppendLine( ".const MAP_MARKER_FLAGS_MASK_ENABLED             = %0000_0001" );
      sb.AppendLine( ".const MAP_MARKER_FLAGS_MASK_TRIGGERED           = %0000_0010" );
      sb.AppendLine();
      sb.AppendLine( "// ====== Entity record layout (8 bytes per entity) ======" );
      sb.AppendLine( "// Byte offsets within a single entity record." );
      sb.AppendLine( "// Use MAP_ENTITY_SIZE to advance between records." );
      sb.AppendLine( ".const MAP_ENTITY_TAG                            = $00" );
      sb.AppendLine( ".const MAP_ENTITY_X                              = $01" );
      sb.AppendLine( ".const MAP_ENTITY_Y                              = $02" );
      sb.AppendLine( ".const MAP_ENTITY_TILE                           = $03" );
      sb.AppendLine( ".const MAP_ENTITY_VALUE1                         = $04" );
      sb.AppendLine( ".const MAP_ENTITY_VALUE2                         = $05" );
      sb.AppendLine( ".const MAP_ENTITY_ENABLED                        = $06" );
      sb.AppendLine( ".const MAP_ENTITY_TRIGGERED                      = $07" );
      sb.AppendLine( ".const MAP_ENTITY_SIZE                           = $08  // bytes per entity" );
      return sb.ToString();
    }



    /// <summary>
    /// Generates a KickAssembler constants file mapping marker-type export
    /// symbols to their tag IDs. <paramref name="UserPrefix"/> is inserted
    /// verbatim at the top of the file (e.g. for includes).
    /// </summary>
    public string GenerateMarkerLabelsAsm( string UserPrefix = null )
    {
      var sb = new StringBuilder();
      if ( !string.IsNullOrEmpty( UserPrefix ) )
      {
        sb.AppendLine( UserPrefix );
        if ( !UserPrefix.EndsWith( "\n" ) )
        {
          sb.AppendLine();
        }
      }
      sb.AppendLine( "// Auto-generated by C64Studio on game-binary export." );
      sb.AppendLine( "// Maps marker-type ExportSymbols to their TagIDs." );
      sb.AppendLine( "// Do not edit by hand — regenerated on every export." );
      sb.AppendLine();

      // Order by TagID for readability and stable diffs across exports.
      var ordered = new List<MarkerType>( MarkerTypes );
      ordered.Sort( ( a, b ) => a.TagID.CompareTo( b.TagID ) );

      int emitted = 0;
      foreach ( var mt in ordered )
      {
        if ( string.IsNullOrEmpty( mt.ExportSymbol ) )
        {
          continue;
        }
        sb.AppendLine( ".const MARKER_" + mt.ExportSymbol.PadRight( 32 )
                     + " = $" + ( mt.TagID & 0xff ).ToString( "X2" )
                     + "  // " + mt.Name );
        ++emitted;
      }
      if ( emitted == 0 )
      {
        sb.AppendLine( "// (no marker types defined an ExportSymbol)" );
      }

      // Also emit the unassigned ones as commented hints so users see what's missing.
      bool headerWritten = false;
      foreach ( var mt in ordered )
      {
        if ( !string.IsNullOrEmpty( mt.ExportSymbol ) )
        {
          continue;
        }
        if ( !headerWritten )
        {
          sb.AppendLine();
          sb.AppendLine( "// Marker types without an ExportSymbol (set one in the Markers tab):" );
          headerWritten = true;
        }
        sb.AppendLine( "// " + mt.Name + " -> tag $" + ( mt.TagID & 0xff ).ToString( "X2" ) );
      }

      return sb.ToString();
    }



    /// <summary>
    /// Generates a KickAssembler constants file mapping entity-type export
    /// symbols to their tag IDs. <paramref name="UserPrefix"/> is inserted
    /// verbatim at the top of the file (e.g. for includes).
    /// </summary>
    public string GenerateEntityLabelsAsm( string UserPrefix = null )
    {
      var sb = new StringBuilder();
      if ( !string.IsNullOrEmpty( UserPrefix ) )
      {
        sb.AppendLine( UserPrefix );
        if ( !UserPrefix.EndsWith( "\n" ) )
        {
          sb.AppendLine();
        }
      }
      sb.AppendLine( "// Auto-generated by C64Studio on game-binary export." );
      sb.AppendLine( "// Maps entity-type ExportSymbols to their TagIDs." );
      sb.AppendLine( "// Do not edit by hand — regenerated on every export." );
      sb.AppendLine();

      var ordered = new List<EntityType>( EntityTypes );
      ordered.Sort( ( a, b ) => a.TagID.CompareTo( b.TagID ) );

      int emitted = 0;
      foreach ( var et in ordered )
      {
        if ( string.IsNullOrEmpty( et.ExportSymbol ) )
        {
          continue;
        }
        sb.AppendLine( ".const ENTITY_" + et.ExportSymbol.PadRight( 32 )
                     + " = $" + ( et.TagID & 0xff ).ToString( "X2" )
                     + "  // " + et.Name );
        ++emitted;
      }
      if ( emitted == 0 )
      {
        sb.AppendLine( "// (no entity types defined an ExportSymbol)" );
      }

      bool headerWritten = false;
      foreach ( var et in ordered )
      {
        if ( !string.IsNullOrEmpty( et.ExportSymbol ) )
        {
          continue;
        }
        if ( !headerWritten )
        {
          sb.AppendLine();
          sb.AppendLine( "// Entity types without an ExportSymbol (set one in the Entities tab):" );
          headerWritten = true;
        }
        sb.AppendLine( "// " + et.Name + " -> tag $" + ( et.TagID & 0xff ).ToString( "X2" ) );
      }

      return sb.ToString();
    }



    /// <summary>
    /// Generates a KickAssembler-compatible sidecar containing every named
    /// <see cref="MapString"/> as a Dreadhold-style byte stream, plus the
    /// MAP_STRING_LO / MAP_STRING_HI pointer tables and one
    /// <c>.const &lt;Label&gt; = &lt;index&gt;</c> per emitted message.
    /// <paramref name="UserPrefix"/> is inserted verbatim at the top of the
    /// file (e.g. for <c>#import</c>s of the COLOR_*/END_OF_LINE/PRESS_FIRE/
    /// CLEAR_TEXT_AREA/END_OF_TEXT constants the consuming project provides).
    ///
    /// Messages whose Label is empty or not a valid asm identifier are
    /// skipped with a comment in the output (and excluded from both the
    /// constants block and the pointer tables).
    /// </summary>
    public string GenerateMapStringsAsm( string UserPrefix = null )
    {
      var sb = new StringBuilder();
      if ( !string.IsNullOrEmpty( UserPrefix ) )
      {
        sb.AppendLine( UserPrefix );
        if ( !UserPrefix.EndsWith( "\n" ) )
        {
          sb.AppendLine();
        }
      }
      sb.AppendLine( "// Auto-generated by C64Studio on game-binary export." );
      sb.AppendLine( "// Map string index constants — runtime pass these in A to look up the" );
      sb.AppendLine( "// matching message via MAP_STRING_LO / MAP_STRING_HI in the .bin." );
      sb.AppendLine( "// The actual byte streams + pointer tables live IN the game binary," );
      sb.AppendLine( "// at the offsets named by MAP_HEADER_OFFSET_MAP_STRING_LO / _HI." );
      sb.AppendLine( "// Do not edit by hand — regenerated on every export." );
      sb.AppendLine();

      List<string> skipped;
      var emitted = GetEmittableMapStrings( out skipped );
      foreach ( var s in skipped )
      {
        sb.AppendLine( "// SKIPPED: " + s );
      }
      if ( skipped.Count > 0 )
      {
        sb.AppendLine();
      }

      if ( emitted.Count == 0 )
      {
        sb.AppendLine( "// (no map strings defined)" );
        return sb.ToString();
      }

      sb.AppendLine( "// String index constants — pass these in A to the message renderer." );
      for ( int i = 0; i < emitted.Count; ++i )
      {
        sb.AppendLine( ".const " + emitted[i].Label.PadRight( 32 )
                     + " = " + i );
      }

      return sb.ToString();
    }



    /// <summary>
    /// Filter <see cref="MapStrings"/> down to the subset that's safe to
    /// emit as numbered indices in both the binary and the asm sidecar:
    /// non-empty Label, valid asm identifier, no duplicate labels (first
    /// occurrence wins). The <paramref name="SkipReasons"/> list contains
    /// one human-readable reason per dropped entry, in source order, so
    /// the sidecar can show users exactly why a string didn't make it.
    /// Both the binary writer and the asm sidecar share this filter so
    /// the index constants always match the binary's pointer table.
    /// </summary>
    public List<MapString> GetEmittableMapStrings( out List<string> SkipReasons )
    {
      SkipReasons = new List<string>();
      var labelRegex = new System.Text.RegularExpressions.Regex( "^[A-Za-z_][A-Za-z0-9_]*$" );
      var seenLabels = new HashSet<string>( StringComparer.Ordinal );
      var emitted    = new List<MapString>();
      foreach ( var ms in MapStrings )
      {
        string label = ms.Label ?? "";
        if ( string.IsNullOrEmpty( label ) )
        {
          SkipReasons.Add( "message with empty label (set a label in the Map Strings tab)." );
          continue;
        }
        if ( !labelRegex.IsMatch( label ) )
        {
          SkipReasons.Add( "invalid label '" + label + "' — must match [A-Za-z_][A-Za-z0-9_]*." );
          continue;
        }
        if ( !seenLabels.Add( label ) )
        {
          SkipReasons.Add( "duplicate label '" + label + "'." );
          continue;
        }
        emitted.Add( ms );
      }
      return emitted;
    }



    /// <summary>
    /// Build the runtime byte stream for one <see cref="MapString"/>. The
    /// format is the one Dreadhold's renderer consumes:
    ///   per non-empty line: [ControlCode] [screen codes...] [Terminator]
    ///   per blank middle line: [Terminator]
    ///   optional [CLEAR_TEXT_AREA]
    ///   [END_OF_TEXT] (mandatory)
    ///
    /// ControlCode is the line's leading byte (game_message.asm's "line
    /// color"). $00..$0F set the foreground color; $10..$1F are reserved
    /// for future runtime extensions but already round-trip through the
    /// project file.
    ///
    /// Text is plain — no inline tokens. Each char becomes one screen
    /// code via the per-project lowercase / uppercase / numbers offsets,
    /// plus the fixed C64 punctuation map. Chars that don't have a known
    /// mapping are skipped (matches the editor preview).
    /// </summary>
    public static GR.Memory.ByteBuffer BuildMapStringByteStream(
      MapString Msg, int LowerStart, int UpperStart, int NumbersStart, int TextAreaWidth )
    {
      var buf = new GR.Memory.ByteBuffer();
      if ( TextAreaWidth < 1 ) TextAreaWidth = 1;

      // Each of the 4 line slots has 3 independently-optional pieces:
      //   ControlCode (leading byte) — skipped if MAP_STRING_NO_CONTROL_CODE.
      //   Text       (screen codes)   — skipped if empty (no null byte).
      //     Padded with leading spaces here for Center / Right justification
      //     so the runtime can write the bytes straight to screen RAM.
      //   Terminator (trailing byte)  — skipped if MAP_STRING_NO_TERMINATOR.
      // A line where all three are skipped emits no bytes at all, letting
      // the user "skip a line" by setting everything to None.
      for ( int li = 0; li < 4; ++li )
      {
        var line = Msg.Lines[li];
        if ( line.ControlCode != MAP_STRING_NO_CONTROL_CODE )
        {
          buf.AppendU8( line.ControlCode );
        }
        string text = line.Text ?? "";
        if ( text.Length > 0 )
        {
          int pad = 0;
          int slack = TextAreaWidth - text.Length;
          if ( slack > 0 )
          {
            if ( line.Justification == MAP_STRING_JUSTIFY_CENTER )      pad = slack / 2;
            else if ( line.Justification == MAP_STRING_JUSTIFY_RIGHT )  pad = slack;
          }
          // Spaces at $20 — fixed C64 punctuation table, independent of charset offsets.
          for ( int p = 0; p < pad; ++p )
          {
            buf.AppendU8( 0x20 );
          }
          for ( int p = 0; p < text.Length; ++p )
          {
            EmitMapStringTextChar( buf, text[p], LowerStart, UpperStart, NumbersStart );
          }
        }
        if ( line.Terminator != MAP_STRING_NO_TERMINATOR )
        {
          buf.AppendU8( line.Terminator );
        }
      }

      if ( Msg.ClearTextAreaAtEnd )
      {
        buf.AppendU8( MAP_STRING_CLEAR_TEXT_AREA );
      }
      buf.AppendU8( MAP_STRING_END_OF_TEXT );
      return buf;
    }



    /// <summary>
    /// Convert a single authored char to its on-screen byte value and
    /// append it to <paramref name="Buf"/>. Skips chars that don't have a
    /// known C64 mapping (matches the preview's "unsupported chars are
    /// invisible" behavior — the user sees in the preview what the
    /// runtime will draw, byte-for-byte).
    /// </summary>
    private static void EmitMapStringTextChar( GR.Memory.ByteBuffer Buf, char Ch,
                                               int LowerStart, int UpperStart, int NumbersStart )
    {
      int sc;
      if ( Ch >= 'A' && Ch <= 'Z' )      sc = Ch - 'A' + UpperStart;
      else if ( Ch >= 'a' && Ch <= 'z' ) sc = Ch - 'a' + LowerStart;
      else if ( Ch >= '0' && Ch <= '9' ) sc = Ch - '0' + NumbersStart;
      else if ( Ch == ' ' )  sc = 0x20;
      else if ( Ch == '!' )  sc = 0x21;
      else if ( Ch == '"' )  sc = 0x22;
      else if ( Ch == '#' )  sc = 0x23;
      else if ( Ch == '$' )  sc = 0x24;
      else if ( Ch == '%' )  sc = 0x25;
      else if ( Ch == '&' )  sc = 0x26;
      else if ( Ch == '\'' ) sc = 0x27;
      else if ( Ch == '(' )  sc = 0x28;
      else if ( Ch == ')' )  sc = 0x29;
      else if ( Ch == '*' )  sc = 0x2A;
      else if ( Ch == '+' )  sc = 0x2B;
      else if ( Ch == ',' )  sc = 0x2C;
      else if ( Ch == '-' )  sc = 0x2D;
      else if ( Ch == '.' )  sc = 0x2E;
      else if ( Ch == '/' )  sc = 0x2F;
      else if ( Ch == ':' )  sc = 0x3A;
      else if ( Ch == ';' )  sc = 0x3B;
      else if ( Ch == '<' )  sc = 0x3C;
      else if ( Ch == '=' )  sc = 0x3D;
      else if ( Ch == '>' )  sc = 0x3E;
      else if ( Ch == '?' )  sc = 0x3F;
      else if ( Ch == '@' )  sc = 0x00;
      else if ( Ch == '[' )  sc = 0x1B;
      else if ( Ch == ']' )  sc = 0x1D;
      else return;

      // Clamp into a byte. Out-of-range offsets the user might have set
      // wrap rather than overflow (255 is the highest valid screen code).
      if ( sc < 0 ) sc = 0;
      if ( sc > 255 ) sc = 255;
      Buf.AppendU8( (byte)sc );
    }



    [System.Obsolete( "Superseded by ExportAsGameBinary; will be removed." )]
    public bool ExportSparseTileAndMapData( bool Vertical, out string ExportData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective, bool EmptyTileCompression, int EmptyTileIndex, bool AddFilenamespace, string Filenamespace, bool WrapMapData )
    {
      StringBuilder sb = new StringBuilder();

      if ( Settings.Assembly.AddFilenamespace )
      {
        sb.Append( ".filenamespace " );
        sb.AppendLine( Settings.Assembly.Filenamespace );
        if ( !string.IsNullOrEmpty( Settings.Assembly.PrefixCode ) )
        {
          sb.AppendLine( Settings.Assembly.PrefixCode );
        }
        sb.AppendLine();
      }
      else if ( !string.IsNullOrEmpty( Settings.Assembly.PrefixCode ) )
      {
        sb.AppendLine( Settings.Assembly.PrefixCode );
        sb.AppendLine();
      }

      string labelSuffix = "";
      if ( Settings.Assembly.IncludeSemicolonAfterSimpleLabels )
      {
        labelSuffix = ":";
      }

      // Tiles Data
      string[] colorNames = new string[] {
        "black", "white", "red", "cyan", "purple", "green", "blue", "yellow",
        "orange", "brown", "light red", "dark grey", "grey", "light green", "light blue", "light grey"
      };

      string[]  colorLabels = { "TILESET_BG_COLOR", "TILESET_MC1_COLOR", "TILESET_MC2_COLOR" };
      string[]  commentLabels = { "background color", "multicolor 1", "multicolor 2" };
      int[]     colors = { BackgroundColor, MultiColor1, MultiColor2 };

      if ( Settings.Assembly.ExportTilesetColors )
      {
        for ( int i = 0; i < 3; ++i )
        {
          int     colorIndex = colors[i] & 0x0f;

          string    line = colorLabels[i] + labelSuffix + " " + DataByteDirective + " $" + colorIndex.ToString( "X2" );

          if ( ( Settings.Assembly.MapSizeCommentEnabled ) && ( !string.IsNullOrEmpty( Settings.Assembly.CommentChars ) ) )
          {
            line += " " + Settings.Assembly.CommentChars + " " + commentLabels[i] + " = ";

            if ( colorIndex < 16 )
            {
              line += colorNames[colorIndex];
            }
            else
            {
              line += "unknown";
            }
          }
          sb.AppendLine( line );
        }
        sb.AppendLine();
      }

      sb.AppendLine( "TILE_COUNT" + labelSuffix + " " + DataByteDirective + " $" + Tiles.Count.ToString( "X2" ) );
      sb.AppendLine( "MAP_COUNT" + labelSuffix + " " + DataByteDirective + " $" + Maps.Count.ToString( "X2" ) );
      sb.AppendLine();

      // Map Data
      if ( ( Settings.Assembly.MapSizeCommentEnabled ) && ( !string.IsNullOrEmpty( Settings.Assembly.CommentChars ) ) )
      {
        sb.AppendLine( Settings.Assembly.CommentChars + " map data" );
      }

      // Global Map Tables
      // MAPS_WIDTH
      sb.AppendLine( LabelPrefix + "MAPS_WIDTH" + labelSuffix );
      GR.Memory.ByteBuffer mapWidths = new GR.Memory.ByteBuffer();
      for ( int i = 0; i < Maps.Count; ++i )
      {
        mapWidths.AppendU8( (byte)Maps[i].Tiles.Width );
      }
      sb.AppendLine( Util.ToASMData( mapWidths, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      // MAPS_HEIGHT
      sb.AppendLine( LabelPrefix + "MAPS_HEIGHT" + labelSuffix );
      GR.Memory.ByteBuffer mapHeights = new GR.Memory.ByteBuffer();
      for ( int i = 0; i < Maps.Count; ++i )
      {
        mapHeights.AppendU8( (byte)Maps[i].Tiles.Height );
      }
      sb.AppendLine( Util.ToASMData( mapHeights, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      if ( Settings.Assembly.ExportMapColors )
      {
        // MAPS_BG_COLOR
        sb.AppendLine( LabelPrefix + "MAPS_BG_COLOR" + labelSuffix );
        GR.Memory.ByteBuffer mapBGColors = new GR.Memory.ByteBuffer();
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int effectiveBGColor = Maps[i].AlternativeBackgroundColor;
          if ( effectiveBGColor == -1 ) effectiveBGColor = BackgroundColor;
          mapBGColors.AppendU8( (byte)( effectiveBGColor & 0x0f ) );
        }
        sb.AppendLine( Util.ToASMData( mapBGColors, WrapData, WrapByteCount, DataByteDirective ) );
        sb.AppendLine();

        // MAPS_MC1_COLOR
        sb.AppendLine( LabelPrefix + "MAPS_MC1_COLOR" + labelSuffix );
        GR.Memory.ByteBuffer mapMC1Colors = new GR.Memory.ByteBuffer();
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int effectiveMC1 = Maps[i].AlternativeMultiColor1;
          if ( effectiveMC1 == -1 ) effectiveMC1 = Charset.Colors.MultiColor1;
          mapMC1Colors.AppendU8( (byte)( effectiveMC1 & 0x0f ) );
        }
        sb.AppendLine( Util.ToASMData( mapMC1Colors, WrapData, WrapByteCount, DataByteDirective ) );
        sb.AppendLine();

        // MAPS_MC2_COLOR
        sb.AppendLine( LabelPrefix + "MAPS_MC2_COLOR" + labelSuffix );
        GR.Memory.ByteBuffer mapMC2Colors = new GR.Memory.ByteBuffer();
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int effectiveMC2 = Maps[i].AlternativeMultiColor2;
          if ( effectiveMC2 == -1 ) effectiveMC2 = Charset.Colors.MultiColor2;
          mapMC2Colors.AppendU8( (byte)( effectiveMC2 & 0x0f ) );
        }
        sb.AppendLine( Util.ToASMData( mapMC2Colors, WrapData, WrapByteCount, DataByteDirective ) );
        sb.AppendLine();
      }

      if ( ( Settings.Assembly.MapSizeCommentEnabled ) && ( !string.IsNullOrEmpty( Settings.Assembly.CommentChars ) ) )
      {
        for ( int i = 0; i < Tiles.Count; ++i )
        {
          sb.AppendLine( Settings.Assembly.CommentChars + " " + i.ToString( "D2" ) + ": " + Tiles[i].Name );
        }
        sb.AppendLine();
      }

      GR.Memory.ByteBuffer tileWidths = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileHeights = new GR.Memory.ByteBuffer();

      foreach ( var tile in Tiles )
      {
        tileWidths.AppendU8( (byte)tile.Chars.Width );
        tileHeights.AppendU8( (byte)tile.Chars.Height );
      }

      sb.AppendLine( LabelPrefix + "TILES_WIDTH" + labelSuffix );
      sb.AppendLine( Util.ToASMData( tileWidths, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_HEIGHT" + labelSuffix );
      sb.AppendLine( Util.ToASMData( tileHeights, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_FLAGS" + labelSuffix );
      GR.Memory.ByteBuffer tileFlags = new GR.Memory.ByteBuffer();
      foreach ( var tile in Tiles )
      {
        tileFlags.AppendU8( (byte)( tile.Passable ? 1 : 0 ) );
      }
      sb.AppendLine( Util.ToASMData( tileFlags, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_CHAR_DATA" + labelSuffix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        var tile = Tiles[i];
        sb.Append( LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) + labelSuffix + " " );
        
        GR.Memory.ByteBuffer charData = new GR.Memory.ByteBuffer();
        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            charData.AppendU8( tile.Chars[x, y].Character );
          }
        }
        sb.Append( Util.ToASMData( charData, false, 0, DataByteDirective ) );
        if ( ( Settings.Assembly.MapSizeCommentEnabled ) && ( !string.IsNullOrEmpty( Settings.Assembly.CommentChars ) ) )
        {
          sb.Append( "\t\t\t" + Settings.Assembly.CommentChars + " tile " + i + ", " + tile.Chars.Width + "x" + tile.Chars.Height );
        }
        sb.AppendLine();
      }
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_CHAR_TABLE_LOW" + labelSuffix );
      GR.Memory.ByteBuffer tableLow = new GR.Memory.ByteBuffer();
      StringBuilder sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_CHAR_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_DATA" + labelSuffix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        var tile = Tiles[i];
        sb.Append( LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) + labelSuffix + " " );

        GR.Memory.ByteBuffer colorData = new GR.Memory.ByteBuffer();
        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            colorData.AppendU8( tile.Chars[x, y].Color );
          }
        }
        sb.AppendLine( Util.ToASMData( colorData, false, 0, DataByteDirective ) );
      }
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_TABLE_LOW" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();


      if ( ( Settings.Assembly.ExportMapAsCharAndColors )
      &&   ( Settings.Assembly.ExportMapColors ) )
      {
        sb.AppendLine( LabelPrefix + "MAPS_COLOR_TABLE_LOW" + labelSuffix );
        sbTable = new StringBuilder();
        sbTable.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          if ( i > 0 ) sbTable.Append( ", " );
          sbTable.Append( "<" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_COLOR" );
        }
        sb.AppendLine( sbTable.ToString() );
        sb.AppendLine();

        sb.AppendLine( LabelPrefix + "MAPS_COLOR_TABLE_HIGH" + labelSuffix );
        sbTable = new StringBuilder();
        sbTable.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          if ( i > 0 ) sbTable.Append( ", " );
          sbTable.Append( ">" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_COLOR" );
        }
        sb.AppendLine( sbTable.ToString() );
        sb.AppendLine();
      }

      sb.AppendLine( LabelPrefix + "MAPS_TABLE_LOW" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();
      
      sb.AppendLine( LabelPrefix + "MAPS_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      if ( Settings.Assembly.ExportPassableBitfields )
      {
        sb.AppendLine( LabelPrefix + "MAPS_PASSABLE_BITS_TABLE_LOW" + labelSuffix );
        sbTable = new StringBuilder();
        sbTable.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          if ( i > 0 ) sbTable.Append( ", " );
          sbTable.Append( "<" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_PASSABLE_BITS" );
        }
        sb.AppendLine( sbTable.ToString() );
        sb.AppendLine();

        sb.AppendLine( LabelPrefix + "MAPS_PASSABLE_BITS_TABLE_HIGH" + labelSuffix );
        sbTable = new StringBuilder();
        sbTable.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          if ( i > 0 ) sbTable.Append( ", " );
          sbTable.Append( ">" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_PASSABLE_BITS" );
        }
        sb.AppendLine( sbTable.ToString() );
        sb.AppendLine();
      }

      // Marker Tables
      AppendMarkerGlobalTables( sb, LabelPrefix, DataByteDirective, Settings.Assembly.ExportHex );

      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];
        sb.Append( LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + labelSuffix + " " );
        if ( (Settings.Assembly.MapSizeCommentEnabled) && (!string.IsNullOrEmpty(Settings.Assembly.CommentChars)) )
        {
          sb.Append( Settings.Assembly.CommentChars + " " + map.Name );
        }
        sb.AppendLine();


        // Collect tiles
        GR.Memory.ByteBuffer mapTiles = new GR.Memory.ByteBuffer();

        if ( Settings.Assembly.ExportMapAsCharAndColors )
        {
          GR.Memory.ByteBuffer mapColorBuffer = new GR.Memory.ByteBuffer();
          int     exportWidth = 0;
          int     exportHeight = 0;
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              int tileIndex = GetExportTileIndex( map.Tiles[x, y] );
              if ( ( tileIndex >= 0 )
              &&   ( tileIndex < Tiles.Count ) )
              {
                var tile = Tiles[tileIndex];
                if ( x * map.TileSpacingX + tile.Chars.Width > exportWidth )
                {
                   exportWidth = x * map.TileSpacingX + tile.Chars.Width;
                }
                if ( y * map.TileSpacingY + tile.Chars.Height > exportHeight )
                {
                   exportHeight = y * map.TileSpacingY + tile.Chars.Height;
                }
              }
            }
          }
          if ( exportWidth == 0 )
          {
             exportWidth = map.Tiles.Width * map.TileSpacingX;
          }
          if ( exportHeight == 0 )
          {
             exportHeight = map.Tiles.Height * map.TileSpacingY;
          }

          mapTiles.Resize( (uint)( exportWidth * exportHeight ) );
          if ( Settings.Assembly.ExportMapColors )
          {
            mapColorBuffer.Resize( (uint)( exportWidth * exportHeight ) );
          }
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              int tileIndex = GetExportTileIndex( map.Tiles[x, y] );
              if ( ( tileIndex >= 0 )
              &&   ( tileIndex < Tiles.Count )
              &&   ( tileIndex != Settings.Assembly.EmptyTileIndex ) )
              {
                var tile = Tiles[tileIndex];
                for ( int ty = 0; ty < tile.Chars.Height; ++ty )
                {
                  for ( int tx = 0; tx < tile.Chars.Width; ++tx )
                  {
                    int finalX = x * map.TileSpacingX + tx;
                    int finalY = y * map.TileSpacingY + ty;

                    if ( ( finalX < exportWidth )
                    &&   ( finalY < exportHeight ) )
                    {
                      int offset = finalX + finalY * exportWidth;

                      mapTiles.SetU8At( offset, tile.Chars[tx, ty].Character );
                      if ( Settings.Assembly.ExportMapColors )
                      {
                        mapColorBuffer.SetU8At( offset, tile.Chars[tx, ty].Color );
                      }
                    }
                  }
                }
              }
            }
          }
          sb.AppendLine( Util.ToASMData( mapTiles, WrapData, WrapByteCount, DataByteDirective ) );

          if ( Settings.Assembly.ExportMapColors )
          {
            sb.Append( LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_COLOR" + labelSuffix + " " );
            if ( (Settings.Assembly.MapSizeCommentEnabled) && (!string.IsNullOrEmpty(Settings.Assembly.CommentChars)) )
            {
              sb.Append( Settings.Assembly.CommentChars + " " + map.Name );
            }
            sb.AppendLine();
            sb.AppendLine( Util.ToASMData( mapColorBuffer, WrapData, WrapByteCount, DataByteDirective ) );
          }
        }
        else
        {
          // dense
          if ( WrapMapData )
          {
            // like regular export
            for ( int y = 0; y < map.Tiles.Height; ++y )
            {
              for ( int x = 0; x < map.Tiles.Width; ++x )
              {
                mapTiles.AppendU8( (byte)GetExportTileIndex( map.Tiles[x, y] ) );
              }
            }
            sb.AppendLine( Util.ToASMData( mapTiles, WrapData, WrapByteCount, DataByteDirective ) );
          }
          else
          {
            // dense
            var mapData = ExportMapAsBuffer( map, !Vertical );

            if ( WrapMapData )
            {
              // like regular export
              sb.AppendLine( Util.ToASMData( mapData, WrapData, WrapByteCount, DataByteDirective ) );
            }
            else
            {
              // one line per row
              // RowByRow = !Vertical
              // if Vertical = false (Horizontal), we want Width bytes per line
              // if Vertical = true (Vertical), we want Height bytes per line
              // ExportMapAsBuffer returns data ordered by the requested direction
              // So we just chop it into linear chunks of the minor dimension stride
              int stride = Vertical ? map.Tiles.Height : map.Tiles.Width;
              int numLines = (int)mapData.Length / stride;
              GR.Memory.ByteBuffer  lineData = new GR.Memory.ByteBuffer();
              for ( int k = 0; k < numLines; ++k )
              {
                lineData.Clear();
                // manually copy bytes to avoid allocating new buffer if possible, but AppendU8 is fast
                for ( int x = 0; x < stride; ++x )
                {
                  lineData.AppendU8( mapData.ByteAt( k * stride + x ) );
                }
                sb.AppendLine( Util.ToASMData( lineData, false, 0, DataByteDirective ) );
              }
            }
          }
        }

        if ( Settings.Assembly.ExportPassableBitfields )
        {
          sb.Append( LabelPrefix );
          sb.AppendLine( "MAP_" + ( i + 1 ).ToString( "D2" ) + "_PASSABLE_BITS" + labelSuffix );

           int passableWidth = map.Tiles.Width * map.TileSpacingX;
           int passableHeight = map.Tiles.Height * map.TileSpacingY;
           for ( int ty = 0; ty < map.Tiles.Height; ++ty )
           {
             for ( int tx = 0; tx < map.Tiles.Width; ++tx )
             {
               int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
               if ( ( tileIndex >= 0 )
               &&   ( tileIndex < Tiles.Count ) )
               {
                 var tile = Tiles[tileIndex];
                 int w = tx * map.TileSpacingX + tile.Chars.Width;
                 int h = ty * map.TileSpacingY + tile.Chars.Height;
                 if ( w > passableWidth )
                 {
                   passableWidth = w;
                 }
                 if ( h > passableHeight )
                 {
                   passableHeight = h;
                 }
               }
             }
           }

           bool[] passable = new bool[passableWidth * passableHeight];
           for ( int idx = 0; idx < passable.Length; ++idx )
           {
             passable[idx] = true;
           }
           for ( int ty = 0; ty < map.Tiles.Height; ++ty )
           {
             for ( int tx = 0; tx < map.Tiles.Width; ++tx )
             {
               int tileIndex = GetExportTileIndex( map.Tiles[tx, ty] );
               if ( ( tileIndex >= 0 )
               &&   ( tileIndex < Tiles.Count )
               &&   ( !Tiles[tileIndex].Passable ) )
               {
                 var tile = Tiles[tileIndex];
                 for ( int cy = 0; cy < tile.Chars.Height; ++cy )
                 {
                   for ( int cx = 0; cx < tile.Chars.Width; ++cx )
                   {
                     int px = tx * map.TileSpacingX + cx;
                     int py = ty * map.TileSpacingY + cy;
                     if ( ( px < passableWidth )
                     &&   ( py < passableHeight ) )
                     {
                       passable[py * passableWidth + px] = false;
                     }
                   }
                 }
               }
             }
           }

           // Per-character one-way "blocked" override pass — same as
           // ExportAsAssembly / ExportAsGameBinary. Applied here for
           // parity until this deprecated export path is removed.
           if ( ( map.CharBlockedOverrides.Width  > 0 )
           &&   ( map.CharBlockedOverrides.Height > 0 ) )
           {
             int ovW = System.Math.Min( map.CharBlockedOverrides.Width,  passableWidth );
             int ovH = System.Math.Min( map.CharBlockedOverrides.Height, passableHeight );
             for ( int cy = 0; cy < ovH; ++cy )
             {
               for ( int cx = 0; cx < ovW; ++cx )
               {
                 if ( map.CharBlockedOverrides[cx, cy] )
                 {
                   passable[cy * passableWidth + cx] = false;
                 }
               }
             }
           }

           GR.Memory.ByteBuffer bitfieldData = new GR.Memory.ByteBuffer();
           for ( int y = 0; y < passableHeight; ++y )
           {
             int     currentX = 0;
             while ( currentX < passableWidth )
             {
               byte   bits = 0;
               for ( int bitIndex = 0; bitIndex < 8; ++bitIndex )
               {
                 if ( ( currentX + bitIndex < passableWidth )
                 &&   ( passable[y * passableWidth + currentX + bitIndex] ) )
                 {
                   bits |= (byte)( 1 << ( 7 - bitIndex ) );
                 }
               }
               bitfieldData.AppendU8( bits );
               currentX += 8;
             }
           }
 
           if ( Settings.Assembly.ExportPassableBitfieldsAsBinary )
           {
             int bytesPerLine = WrapData ? WrapByteCount : int.MaxValue;
             int bytesWritten = 0;
             while ( bytesWritten < bitfieldData.Length )
             {
                int bytesToOutput = Math.Min( bytesPerLine, (int)bitfieldData.Length - bytesWritten );
                
                sb.Append( DataByteDirective );
                for ( int k = 0; k < bytesToOutput; ++k )
                {
                  if ( k > 0 ) sb.Append( "," );
                  sb.Append( " %" + Convert.ToString( bitfieldData.ByteAt( bytesWritten + k ), 2 ).PadLeft( 8, '0' ) );
                }
                sb.AppendLine();
                bytesWritten += bytesToOutput;
             }
           }
           else
           {
             sb.AppendLine( Util.ToASMData( bitfieldData, WrapData, WrapByteCount, DataByteDirective ) );
           }
          sb.AppendLine();
        }

        // Markers
        AppendMarkerMapData( sb, map, i, LabelPrefix, DataByteDirective, Settings.Assembly.ExportHex );
      }

      ExportData = sb.ToString();
      return true;
    }


    private int GetExportTileIndex( int TileIndex )
    {
      if ( ( TileIndex < 0 ) 
      ||   ( TileIndex >= Tiles.Count ) )
      {
        return TileIndex;
      }
      if ( Tiles[TileIndex].NotExportedOnMap )
      {
        return Settings.Assembly.EmptyTileIndex;
      }
      return TileIndex;
    }

    private void AppendMarkerGlobalTables( StringBuilder sb, string LabelPrefix, string DataByteDirective, bool HexFormat )
    {
      if ( !Settings.Assembly.ExportMarkers ) return;

      foreach ( var markerType in MarkerTypes )
      {
        bool isUsed = false;
        foreach ( var map in Maps )
        {
          foreach ( var marker in map.Markers )
          {
            if ( marker.Type == markerType.ID )
            {
              isUsed = true;
              break;
            }
          }
          if ( isUsed ) break;
        }
        if ( !isUsed ) continue;

        sb.Append( LabelPrefix );
        sb.AppendLine( "MAPS_MARKERS_COUNT_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
        
        // Count Table
        StringBuilder sbData = new StringBuilder();
        sbData.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int count = 0;
          foreach ( var marker in Maps[i].Markers )
          {
            if ( marker.Type == markerType.ID )
            {
              ++count;
            }
          }
          if ( i > 0 ) sbData.Append( "," );
          if ( HexFormat )
          {
            sbData.Append( "$" + count.ToString( "X2" ) );
          }
          else
          {
            sbData.Append( count.ToString() );
          }
        }
        sb.AppendLine( sbData.ToString() );

        // Table Low
        sb.AppendLine();
        sb.Append( LabelPrefix );
        sb.AppendLine( "MAPS_MARKERS_TABLE_LOW_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
        sbData = new StringBuilder();
        sbData.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int count = 0;
          foreach ( var marker in Maps[i].Markers )
          {
            if ( marker.Type == markerType.ID )
            {
              ++count;
            }
          }

          if ( i > 0 ) sbData.Append( "," );
          if ( count == 0 )
          {
             if ( HexFormat ) sbData.Append( "$00" );
             else sbData.Append( "0" );
          }
          else
          {
            sbData.Append( "<" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_MARKERS_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
          }
        }
        sb.AppendLine( sbData.ToString() );

        // Table High
        sb.AppendLine();
        sb.Append( LabelPrefix );
        sb.AppendLine( "MAPS_MARKERS_TABLE_HIGH_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
        sbData = new StringBuilder();
        sbData.Append( DataByteDirective + " " );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          int count = 0;
          foreach ( var marker in Maps[i].Markers )
          {
            if ( marker.Type == markerType.ID )
            {
              ++count;
            }
          }

          if ( i > 0 ) sbData.Append( "," );
          if ( count == 0 )
          {
             if ( HexFormat ) sbData.Append( "$00" );
             else sbData.Append( "0" );
          }
          else
          {
            sbData.Append( ">" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + "_MARKERS_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
          }
        }
        sb.AppendLine( sbData.ToString() );
        sb.AppendLine();
      }
    }

    private void AppendMarkerMapData( StringBuilder sb, Map map, int mapIndex, string LabelPrefix, string DataByteDirective, bool HexFormat )
    {
        if ( !Settings.Assembly.ExportMarkers ) return;

        foreach ( var markerType in MarkerTypes )
        {
          bool isUsedGlobally = false;
          foreach ( var m in Maps )
          {
            foreach ( var marker in m.Markers )
            {
              if ( marker.Type == markerType.ID )
              {
                isUsedGlobally = true;
                break;
              }
            }
            if ( isUsedGlobally ) break;
          }
          if ( !isUsedGlobally ) continue;

          int     countInMap = 0;
          foreach ( var marker in map.Markers )
          {
             if ( marker.Type == markerType.ID )
             {
               ++countInMap;
             }
          }

          if ( countInMap > 0 )
          {
             sb.Append( LabelPrefix );
             sb.AppendLine( "MAP_" + ( mapIndex + 1 ).ToString( "D2" ) + "_MARKERS_" + ( "TAG" + markerType.TagID.ToString( "D3" ) ) );
             
             foreach ( var marker in map.Markers )
             {
               if ( marker.Type == markerType.ID )
               {
                 sb.Append( DataByteDirective + " " );
             if ( HexFormat )
             {
               sb.Append( "$" + marker.X.ToString( "X2" ) );
               sb.Append( ",$" + marker.Y.ToString( "X2" ) );
               sb.Append( ",$" + markerType.TagID.ToString( "X2" ) );
             }
             else
             {
               sb.Append( marker.X.ToString() );
               sb.Append( "," + marker.Y.ToString() );
               sb.Append( "," + markerType.TagID.ToString() );
             }
             sb.AppendLine();
               }
             }
             sb.AppendLine();
          }
        }
    }



    /// <summary>
    /// Build the parent MAP chunk for one <see cref="Map"/>. Used both by the
    /// project-level save loop and by <see cref="CloneMap"/> (which serializes
    /// then deserializes a map to deep-copy it). Lifting this out of the save
    /// loop means new fields only need to be added in one place.
    ///
    /// <paramref name="IncludeRevisions"/> is false when building the chunk
    /// for a snapshot (i.e. inside a MAP_REVISION wrapper); a snapshot's
    /// Revisions list is always empty in memory, but the explicit parameter
    /// makes that contract impossible to break by accident.
    /// </summary>
    public static GR.IO.FileChunk BuildMapChunk( Map map, bool IncludeRevisions )
    {
      GR.IO.FileChunk chunkMap = new GR.IO.FileChunk( FileChunkConstants.MAP );

      GR.IO.FileChunk chunkMapInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_INFO );

      chunkMapInfo.AppendString( map.Name );
      chunkMapInfo.AppendI32( map.TileSpacingX );
      chunkMapInfo.AppendI32( map.TileSpacingY );
      chunkMapInfo.AppendI32( map.AlternativeMultiColor1 + 1 );
      chunkMapInfo.AppendI32( map.AlternativeMultiColor2 + 1 );
      chunkMapInfo.AppendI32( map.AlternativeBackgroundColor + 1 );
      chunkMapInfo.AppendI32( map.AlternativeBGColor4 + 1 );
      chunkMapInfo.AppendI32( (int)map.AlternativeMode + 1 );
      chunkMapInfo.AppendI32( map.SelectedMarkerType );
      chunkMapInfo.AppendI32( map.MarkerDimOpacity );
      // Appended for NextMarkerGroupId — sequential allocator for the
      // marker toolbar's Find-next button. Forward-compat: older readers
      // simply stop after MarkerDimOpacity and leave the default of 1.
      chunkMapInfo.AppendI32( map.NextMarkerGroupId );
      chunkMap.Append( chunkMapInfo.ToBuffer() );

      GR.IO.FileChunk chunkMapData = new GR.IO.FileChunk( FileChunkConstants.MAP_DATA );
      chunkMapData.AppendI32( map.Tiles.Width );
      chunkMapData.AppendI32( map.Tiles.Height );
      for ( int j = 0; j < map.Tiles.Height; ++j )
      {
        for ( int i = 0; i < map.Tiles.Width; ++i )
        {
          chunkMapData.AppendI32( map.Tiles[i, j] );
        }
      }
      chunkMap.Append( chunkMapData.ToBuffer() );

      // Per-character color override layer. Char-grid sized (Tiles.Width
      // × spacingX, Tiles.Height × spacingY). Saved as a sparse chunk:
      // skip entirely if every char cell is -1, since most maps won't use
      // this feature and there's no point bloating the file. The reader
      // treats an absent chunk as "all default" and pre-resizes the layer
      // in MAP_DATA. The dimensions written here also disambiguate this
      // (char-grid) format from the legacy (tile-grid) format on load.
      int  saveCharW = map.Tiles.Width  * map.TileSpacingX;
      int  saveCharH = map.Tiles.Height * map.TileSpacingY;
      bool anyOverride = false;
      if ( ( map.TileColorOverrides.Width == saveCharW )
      &&   ( map.TileColorOverrides.Height == saveCharH ) )
      {
        for ( int j = 0; j < saveCharH && !anyOverride; ++j )
        {
          for ( int i = 0; i < saveCharW; ++i )
          {
            if ( map.TileColorOverrides[i, j] != -1 )
            {
              anyOverride = true;
              break;
            }
          }
        }
      }
      if ( anyOverride )
      {
        GR.IO.FileChunk chunkOverrides = new GR.IO.FileChunk( FileChunkConstants.MAP_TILE_COLOR_OVERRIDES );
        chunkOverrides.AppendI32( saveCharW );
        chunkOverrides.AppendI32( saveCharH );
        for ( int j = 0; j < saveCharH; ++j )
        {
          for ( int i = 0; i < saveCharW; ++i )
          {
            chunkOverrides.AppendI32( map.TileColorOverrides[i, j] );
          }
        }
        chunkMap.Append( chunkOverrides.ToBuffer() );
      }

      // Per-character one-way "blocked" override layer (mirrors the
      // color-override chunk above, but stored as 1 byte per cell since
      // the value is binary). Sparse: only emitted when at least one
      // cell is true. Skipping the chunk for fully-default maps keeps
      // pre-feature saves byte-identical (no chunk = layer defaults to
      // all false on load, which is the no-override sentinel).
      bool anyBlocked = false;
      if ( ( map.CharBlockedOverrides.Width == saveCharW )
      &&   ( map.CharBlockedOverrides.Height == saveCharH ) )
      {
        for ( int j = 0; j < saveCharH && !anyBlocked; ++j )
        {
          for ( int i = 0; i < saveCharW; ++i )
          {
            if ( map.CharBlockedOverrides[i, j] )
            {
              anyBlocked = true;
              break;
            }
          }
        }
      }
      if ( anyBlocked )
      {
        GR.IO.FileChunk chunkBlocked = new GR.IO.FileChunk( FileChunkConstants.MAP_CHAR_BLOCKED_OVERRIDES );
        chunkBlocked.AppendI32( saveCharW );
        chunkBlocked.AppendI32( saveCharH );
        for ( int j = 0; j < saveCharH; ++j )
        {
          for ( int i = 0; i < saveCharW; ++i )
          {
            chunkBlocked.AppendU8( map.CharBlockedOverrides[i, j] ? (byte)1 : (byte)0 );
          }
        }
        chunkMap.Append( chunkBlocked.ToBuffer() );
      }

      if ( map.ExtraDataText.Length > 0 )
      {
        GR.IO.FileChunk chunkMapExtraData = new GR.IO.FileChunk( FileChunkConstants.MAP_EXTRA_DATA_TEXT );

        chunkMapExtraData.AppendString( map.ExtraDataText );

        chunkMap.Append( chunkMapExtraData.ToBuffer() );
      }

      foreach ( var marker in map.Markers )
      {
        GR.IO.FileChunk chunkMarker = new GR.IO.FileChunk( FileChunkConstants.MAP_MARKERS );
        chunkMarker.AppendI32( marker.X );
        chunkMarker.AppendI32( marker.Y );
        chunkMarker.AppendI32( marker.Type );
        chunkMarker.AppendString( marker.Name );
        chunkMarker.AppendU8( marker.Value1 );
        chunkMarker.AppendU8( (byte)( marker.Enabled ? 1 : 0 ) );
        chunkMarker.AppendU8( (byte)( marker.Triggered ? 1 : 0 ) );
        // Appended for Value2 — old files that lack this byte get the default 0.
        chunkMarker.AppendU8( marker.Value2 );
        // Appended for GroupId — same forward-compat pattern, default 0.
        chunkMarker.AppendU8( marker.GroupId );
        chunkMap.Append( chunkMarker.ToBuffer() );
      }

      foreach ( var entity in map.Entities )
      {
        GR.IO.FileChunk chunkEntity = new GR.IO.FileChunk( FileChunkConstants.MAP_ENTITIES );
        chunkEntity.AppendI32( entity.X );
        chunkEntity.AppendI32( entity.Y );
        chunkEntity.AppendI32( entity.Type );
        chunkEntity.AppendU8( entity.Value1 );
        chunkEntity.AppendU8( entity.Value2 );
        chunkEntity.AppendU8( (byte)( entity.Enabled ? 1 : 0 ) );
        // Triggered was added later — old files without this byte get the
        // default false on load (see entity reader for the position check).
        chunkEntity.AppendU8( (byte)( entity.Triggered ? 1 : 0 ) );
        chunkMap.Append( chunkEntity.ToBuffer() );
      }

      if ( map.ExtraDataOld.Length > 0 )
      {
        GR.IO.FileChunk chunkMapExtraData = new GR.IO.FileChunk( FileChunkConstants.MAP_EXTRA_DATA );

        chunkMapExtraData.AppendU32( map.ExtraDataOld.Length );
        chunkMapExtraData.Append( map.ExtraDataOld );

        chunkMap.Append( chunkMapExtraData.ToBuffer() );
      }

      // Revisions live alongside the rest of the map's content. Each
      // MAP_REVISION sub-chunk carries a label, a creation timestamp, and
      // a fully-serialized inner MAP chunk for the snapshot itself. We
      // recurse with IncludeRevisions=false so a snapshot never carries
      // its own revisions (which would be unbounded on save).
      if ( IncludeRevisions )
      {
        foreach ( var revision in map.Revisions )
        {
          if ( revision == null || revision.Snapshot == null ) continue;

          GR.IO.FileChunk chunkRev = new GR.IO.FileChunk( FileChunkConstants.MAP_REVISION );
          chunkRev.AppendString( revision.Name ?? "" );
          // Ticks serialized as a string (no AppendI64 available) — culture-
          // invariant so a project file written on one machine round-trips
          // on another with a different locale.
          chunkRev.AppendString( revision.CreatedAt.Ticks.ToString(
                                   System.Globalization.CultureInfo.InvariantCulture ) );
          chunkRev.Append( BuildMapChunk( revision.Snapshot, false ).ToBuffer() );
          chunkMap.Append( chunkRev.ToBuffer() );
        }
      }

      return chunkMap;
    }



    /// <summary>
    /// Inverse of <see cref="BuildMapChunk"/>: walks the body of a parent
    /// MAP chunk and populates <paramref name="map"/> from its sub-chunks.
    /// </summary>
    public static void ReadMapFromBody( GR.IO.MemoryReader bodyReader, Map map )
    {
      GR.IO.FileChunk mapChunk = new GR.IO.FileChunk();

      while ( mapChunk.ReadFromStream( bodyReader ) )
      {
        GR.IO.MemoryReader mapChunkReader = mapChunk.MemoryReader();
        switch ( mapChunk.Type )
        {
          case FileChunkConstants.MAP_INFO:
            map.Name = mapChunkReader.ReadString();
            map.TileSpacingX = mapChunkReader.ReadInt32();
            map.TileSpacingY = mapChunkReader.ReadInt32();
            map.AlternativeMultiColor1 = mapChunkReader.ReadInt32() - 1;
            map.AlternativeMultiColor2 = mapChunkReader.ReadInt32() - 1;
            map.AlternativeBackgroundColor = mapChunkReader.ReadInt32() - 1;
            map.AlternativeBGColor4 = mapChunkReader.ReadInt32() - 1;
            map.AlternativeMode = (TextCharMode)( mapChunkReader.ReadInt32() - 1 );
            if ( mapChunkReader.Size - mapChunkReader.Position >= 4 )
            {
              map.SelectedMarkerType = mapChunkReader.ReadInt32();
            }
            if ( mapChunkReader.Size - mapChunkReader.Position >= 4 )
            {
              map.MarkerDimOpacity = mapChunkReader.ReadInt32();
            }
            if ( mapChunkReader.Size - mapChunkReader.Position >= 4 )
            {
              map.NextMarkerGroupId = mapChunkReader.ReadInt32();
              if ( map.NextMarkerGroupId < 1 ) map.NextMarkerGroupId = 1;
            }
            break;
          case FileChunkConstants.MAP_DATA:
            {
              int w = mapChunkReader.ReadInt32();
              int h = mapChunkReader.ReadInt32();

              map.Tiles.Resize( w, h );
              // Char-grid override layer: one slot per character cell on
              // the map (Tiles × spacing). Default to -1 (no override) for
              // every char — the optional MAP_TILE_COLOR_OVERRIDES chunk
              // below may overwrite these. By the time this case runs,
              // MAP_INFO has already set TileSpacingX/Y (it precedes
              // MAP_DATA in the saved chunk order).
              int charW = w * map.TileSpacingX;
              int charH = h * map.TileSpacingY;
              map.TileColorOverrides.Resize( charW, charH );
              for ( int yy = 0; yy < charH; ++yy )
              {
                for ( int xx = 0; xx < charW; ++xx )
                {
                  map.TileColorOverrides[xx, yy] = -1;
                }
              }
              // Per-character "blocked" override layer — same shape as
              // TileColorOverrides above. Resize alone is enough: the
              // default false IS the no-override sentinel, no explicit
              // reset needed. The optional MAP_CHAR_BLOCKED_OVERRIDES
              // chunk below may overwrite individual cells; absent chunk
              // means "no overrides anywhere", which matches every
              // pre-feature project.
              map.CharBlockedOverrides.Resize( charW, charH );

              // Optimization: read entire block at once
              GR.Memory.ByteBuffer  inputBuffer = new GR.Memory.ByteBuffer();
              mapChunkReader.ReadBlock( inputBuffer, (uint)( w * h * 4 ) );

              for ( int j = 0; j < h; ++j )
              {
                for ( int i = 0; i < w; ++i )
                {
                  int offset = ( i + j * w ) * 4;
                  map.Tiles[i, j] = (int)( inputBuffer.ByteAt( offset )
                                       | ( inputBuffer.ByteAt( offset + 1 ) << 8 )
                                       | ( inputBuffer.ByteAt( offset + 2 ) << 16 )
                                       | ( inputBuffer.ByteAt( offset + 3 ) << 24 ) );
                }
              }
            }
            break;
          case FileChunkConstants.MAP_TILE_COLOR_OVERRIDES:
            {
              int chunkW = mapChunkReader.ReadInt32();
              int chunkH = mapChunkReader.ReadInt32();

              int tileW = map.Tiles.Width;
              int tileH = map.Tiles.Height;
              int charW = tileW * map.TileSpacingX;
              int charH = tileH * map.TileSpacingY;

              if ( ( map.TileColorOverrides.Width != charW )
              ||   ( map.TileColorOverrides.Height != charH ) )
              {
                map.TileColorOverrides.Resize( charW, charH );
              }

              if ( ( chunkW == charW )
              &&   ( chunkH == charH ) )
              {
                // New (char-grid) format — read each char's override directly.
                for ( int j = 0; j < charH; ++j )
                {
                  for ( int i = 0; i < charW; ++i )
                  {
                    map.TileColorOverrides[i, j] = mapChunkReader.ReadInt32();
                  }
                }
              }
              else if ( ( chunkW == tileW )
              &&        ( chunkH == tileH ) )
              {
                // Legacy (tile-grid) format from before per-char overrides.
                // Read each tile's value and replicate it across the
                // spacingX × spacingY char block belonging to that tile —
                // visually identical to the old behaviour (the whole tile
                // had one colour) but now stored in the new layout.
                for ( int ty = 0; ty < tileH; ++ty )
                {
                  for ( int tx = 0; tx < tileW; ++tx )
                  {
                    int v = mapChunkReader.ReadInt32();
                    for ( int dy = 0; dy < map.TileSpacingY; ++dy )
                    {
                      for ( int dx = 0; dx < map.TileSpacingX; ++dx )
                      {
                        int cx = tx * map.TileSpacingX + dx;
                        int cy = ty * map.TileSpacingY + dy;
                        if ( ( cx < charW )
                        &&   ( cy < charH ) )
                        {
                          map.TileColorOverrides[cx, cy] = v;
                        }
                      }
                    }
                  }
                }
              }
              else
              {
                // Unknown shape — read defensively, copy what fits.
                // Shouldn't trigger for files written by our save code.
                for ( int j = 0; j < chunkH; ++j )
                {
                  for ( int i = 0; i < chunkW; ++i )
                  {
                    int v = mapChunkReader.ReadInt32();
                    if ( ( i < charW )
                    &&   ( j < charH ) )
                    {
                      map.TileColorOverrides[i, j] = v;
                    }
                  }
                }
              }
            }
            break;
          case FileChunkConstants.MAP_CHAR_BLOCKED_OVERRIDES:
            {
              // Per-character "blocked" override layer. Stored as 1 byte
              // per cell (0 / 1) in char-grid shape. MAP_DATA already
              // resized the layer to the current char-grid; mismatched
              // dimensions here are defensive (clamp on read).
              int chunkW = mapChunkReader.ReadInt32();
              int chunkH = mapChunkReader.ReadInt32();
              int charW  = map.Tiles.Width  * map.TileSpacingX;
              int charH  = map.Tiles.Height * map.TileSpacingY;

              if ( ( map.CharBlockedOverrides.Width != charW )
              ||   ( map.CharBlockedOverrides.Height != charH ) )
              {
                map.CharBlockedOverrides.Resize( charW, charH );
              }

              for ( int j = 0; j < chunkH; ++j )
              {
                for ( int i = 0; i < chunkW; ++i )
                {
                  byte v = mapChunkReader.ReadUInt8();
                  if ( ( i < charW )
                  &&   ( j < charH ) )
                  {
                    map.CharBlockedOverrides[i, j] = ( v != 0 );
                  }
                }
              }
            }
            break;
          case FileChunkConstants.MAP_EXTRA_DATA:
            {
              uint len = mapChunkReader.ReadUInt32();

              mapChunkReader.ReadBlock( map.ExtraDataOld, len );

              map.ExtraDataText = map.ExtraDataOld.ToString();
              map.ExtraDataOld.Clear();
            }
            break;
          case FileChunkConstants.MAP_EXTRA_DATA_TEXT:
            {
              map.ExtraDataText = mapChunkReader.ReadString();
            }
            break;
          case FileChunkConstants.MAP_MARKERS:
            {
              Marker  marker = new Marker();
              marker.X = mapChunkReader.ReadInt32();
              marker.Y = mapChunkReader.ReadInt32();
              marker.Type = mapChunkReader.ReadInt32();
              marker.Name = mapChunkReader.ReadString();
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                marker.Value1 = mapChunkReader.ReadUInt8();
              }
              else
              {
                marker.Value1 = 0;
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                marker.Enabled = ( mapChunkReader.ReadUInt8() != 0 );
              }
              else
              {
                marker.Enabled = true;
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                marker.Triggered = ( mapChunkReader.ReadUInt8() != 0 );
              }
              else
              {
                marker.Triggered = false;
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                marker.Value2 = mapChunkReader.ReadUInt8();
              }
              else
              {
                marker.Value2 = 0;
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                marker.GroupId = mapChunkReader.ReadUInt8();
              }
              else
              {
                marker.GroupId = 0;
              }
              map.Markers.Add( marker );
            }
            break;
          case FileChunkConstants.MAP_ENTITIES:
            {
              Entity  entity = new Entity();
              entity.X = mapChunkReader.ReadInt32();
              entity.Y = mapChunkReader.ReadInt32();
              entity.Type = mapChunkReader.ReadInt32();
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                entity.Value1 = mapChunkReader.ReadUInt8();
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                entity.Value2 = mapChunkReader.ReadUInt8();
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                entity.Enabled = ( mapChunkReader.ReadUInt8() != 0 );
              }
              else
              {
                entity.Enabled = true;
              }
              if ( mapChunkReader.Size - mapChunkReader.Position >= 1 )
              {
                entity.Triggered = ( mapChunkReader.ReadUInt8() != 0 );
              }
              else
              {
                entity.Triggered = false;
              }
              map.Entities.Add( entity );
            }
            break;
          case FileChunkConstants.MAP_REVISION:
            {
              // The revision wrapper carries label + timestamp, then a
              // fully-formed inner MAP chunk for the snapshot itself.
              var revision = new MapRevision();
              revision.Name = mapChunkReader.ReadString();
              string ticksText = mapChunkReader.ReadString();
              long ticks = 0;
              long.TryParse( ticksText,
                             System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture,
                             out ticks );
              if ( ticks <= 0 ) ticks = DateTime.Now.Ticks;
              revision.CreatedAt = new DateTime( ticks );

              // Find the inner MAP chunk and walk its body.
              GR.IO.FileChunk innerMapChunk = new GR.IO.FileChunk();
              if ( innerMapChunk.ReadFromStream( mapChunkReader )
              &&   innerMapChunk.Type == FileChunkConstants.MAP )
              {
                var snapshot = new Map();
                ReadMapFromBody( innerMapChunk.MemoryReader(), snapshot );
                revision.Snapshot = snapshot;
                map.Revisions.Add( revision );
              }
            }
            break;
        }
      }
    }



    /// <summary>
    /// Deep-copy a <see cref="Map"/> by round-tripping it through the chunk
    /// serializer. Robust by construction: any future field added to the
    /// serializer is automatically copied. The clone has an empty
    /// <see cref="Map.Revisions"/> list (we never want history nested inside
    /// another snapshot).
    /// </summary>
    public static Map CloneMap( Map source )
    {
      if ( source == null ) return null;

      var bytes = BuildMapChunk( source, IncludeRevisions: false ).ToBuffer();

      var memReader = new GR.IO.MemoryReader( bytes );
      var outerChunk = new GR.IO.FileChunk();
      var clone = new Map();
      if ( outerChunk.ReadFromStream( memReader )
      &&   outerChunk.Type == FileChunkConstants.MAP )
      {
        ReadMapFromBody( outerChunk.MemoryReader(), clone );
      }
      return clone;
    }

  }
}
