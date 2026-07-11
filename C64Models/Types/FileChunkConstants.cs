namespace RetroDevStudio
{
  public class FileChunkConstants
  {
    public const ushort    RESTART_INFO                   = 0x0100;
    public const ushort    RESTART_DATA                   = 0x0101;
    public const ushort    RESTART_DOC_INFO               = 0x0102;

    public const ushort    SOLUTION                       = 0x0400;
    public const ushort    SOLUTION_INFO                  = 0x0401;
    public const ushort    SOLUTION_PROJECT               = 0x0402;
    public const ushort    SOLUTION_NODES                 = 0x0403;

    public const ushort    PROJECT                        = 0x1000;
    public const ushort    PROJECT_ELEMENT                = 0x1001;
    public const ushort    PROJECT_ELEMENT_DATA           = 0x1002;
    public const ushort    PROJECT_ELEMENT_DISPLAY_DATA   = 0x1003;
    public const ushort    PROJECT_ELEMENT_PER_CONFIG_SETTING  = 0x1004;
    public const ushort    PROJECT_ELEMENT_FOLDED_BLOCKS  = 0x1005;
    public const ushort    PROJECT_CONFIG                 = 0x1100;
    public const ushort    PROJECT_WATCH_ENTRY            = 0x1101;

    public const ushort    CHARSET_SCREEN_INFO            = 0x1200;
    public const ushort    SCREEN_CHAR_DATA               = 0x1300;
    public const ushort    SCREEN_COLOR_DATA              = 0x1301;
    public const ushort    GRAPHIC_SCREEN_INFO            = 0x1310;
    public const ushort    GRAPHIC_DATA                   = 0x1311;   // uint width, uint height, uint image type, uint palette entry count, byte r,g,b, uint data size, data
    public const ushort    GRAPHIC_COLOR_MAPPING          = 0x1312;   // Dictionary<int,List<byte>>

    public const ushort    MAP_PROJECT_INFO               = 0x1320;
    public const ushort    MAP_PROJECT_DATA               = 0x1321;
    public const ushort    MAP_TILE                       = 0x1322;
    public const ushort    MAP                            = 0x1324;
    public const ushort    MAP_INFO                       = 0x1325;
    public const ushort    MAP_DATA                       = 0x1326;
    public const ushort    MAP_EXTRA_DATA                 = 0x1327;
    public const ushort    MAP_CHARSET                    = 0x1328;
    public const ushort    MAP_EXTRA_DATA_TEXT            = 0x1329;   // replaces MAP_EXTRA_DATA
    public const ushort    MAP_PROJECT_EXPORT_SETTINGS    = 0x132A;
    public const ushort    MAP_MARKER_TYPES               = 0x132B;
    public const ushort    MAP_MARKERS                    = 0x132C;
    public const ushort    MAP_ENTITY_TYPES               = 0x132D;
    public const ushort    MAP_ENTITIES                   = 0x132E;
    public const ushort    MAP_TILE_COLOR_OVERRIDES       = 0x132F;

    public const ushort    SOURCE_ASM                     = 0x1330;
    public const ushort    SOURCE_BASIC                   = 0x1331;
    // One sub-chunk per revision, written inside the parent MAP chunk.
    // The body wraps a fully-formed MAP chunk for the snapshot itself, so
    // the revision payload is exactly what the load path already knows
    // how to parse — no parallel data model. See MapProject.Map.Revisions.
    public const ushort    MAP_REVISION                   = 0x1332;
    // Per-character one-way "blocked" override. Sparse: only emitted when
    // any cell is true. Layout: [i32 charW][i32 charH][byte × charW × charH]
    // (1 byte per cell, 0/1). Defers to Tile.Passable when absent or when
    // the cell value is false. See MapProject.Map.CharBlockedOverrides.
    public const ushort    MAP_CHAR_BLOCKED_OVERRIDES     = 0x1333;
    // One per-project named text script for the in-game 4-line UI text area.
    // See MapProject.MapString. Body: [string Label][byte ClearTextAreaAtEnd]
    // then 4 × [string Text][byte Terminator]. New chunk — no MAP_PROJECT_INFO
    // version bump (absent chunk = empty MapStrings list, fully back-compat).
    public const ushort    MAP_STRING                     = 0x1334;
    // Editor-only tile layers above the Background (Layers[1..N-1]). Background
    // (Layers[0]) still ships in MAP_DATA / MAP_TILE_COLOR_OVERRIDES. Body:
    // [i32 LayerCount] then per upper layer [string Name][u8 Visible]
    // [i32 W][i32 H][i32×W*H tiles] [i32 colorOverrideCount]
    // (colorOverrideCount × [i32 x][i32 y][i32 color]). Sparse: only written
    // when an upper layer has content. Absent chunk = re-synthesize the default
    // empty layers on load (fully back-compat; pre-layer files unchanged).
    public const ushort    MAP_LAYERS                     = 0x1335;
    // Per-project CRT display-filter state: [u8 enabled][pipeline blob]
    // (blob = FilterPipeline.SaveToBuffer, self-describing/versioned; the
    // model stores it opaquely — only the editor parses it). New chunk —
    // absent = project predates per-project filters; the editor seeds from
    // the global settings pipeline on load (fully back-compat).
    public const ushort    MAP_DISPLAY_FILTERS            = 0x1336;
    // Map outline sidecar file (<mapproject>.mapoutlines) — these two chunks
    // live in that standalone container, NOT inside the .mapproject itself.
    // MAP_OUTLINE_INFO: [u32 version]. MAP_OUTLINE_IMAGE (one per map):
    // [string OutlineGuid][i32 width][i32 height][u32 pngLength][png bytes].
    // Images are keyed by Map.OutlineGuid so they survive map reorder /
    // rename / delete+undo. See MapOutlineContainer.
    public const ushort    MAP_OUTLINE_INFO               = 0x1337;
    public const ushort    MAP_OUTLINE_IMAGE              = 0x1338;
    // Project-level outline TOOL settings (brush/eraser/border sizes,
    // stamp scale, text font, ink/fill colors, extend step, recents) —
    // lives in the .mapproject itself, unlike the two sidecar chunks
    // above. Absent chunk = defaults (fully back-compat).
    public const ushort    MAP_OUTLINE_TOOL_SETTINGS      = 0x1339;
    // Per-map memo (free-form notes on the Map tab's sidebar), stored as
    // an RTF string — RTF is 7-bit ASCII by design (non-ANSI chars are
    // escaped), so the byte-per-char AppendString is lossless for it.
    // Sub-chunk of MAP; only written when non-empty (back-compat free).
    public const ushort    MAP_MEMO                       = 0x133A;
    // One persistent outline text OBJECT (selectable/editable text in the
    // paint mode): [string text][string fontFamily][f32 size][u8 styleBits]
    // [u32 argb][f32 x][f32 y]. A map's objects are a SEQUENCE of these
    // chunks inside an opaque blob appended to its MAP_OUTLINE_IMAGE entry
    // (the container never decodes it). Append-tolerant per object.
    public const ushort    MAP_OUTLINE_TEXT_OBJECT        = 0x133B;

    public const ushort    CHARSET_PROJECT                = 0x1340;
    public const ushort    CHARSET_INFO                   = 0x1341;
    public const ushort    CHARSET_CHAR                   = 0x1342;
    public const ushort    CHARSET_COLOR_SETTINGS         = 0x1343;
    public const ushort    CHARSET_PLAYGROUND             = 0x1344;
    public const ushort    CHARSET_EXPORT                 = 0x1345;
    public const ushort    CHARSET_CATEGORY               = 0x1346;

    public const ushort    SPRITESET_PROJECT              = 0x13E0;
    public const ushort    SPRITESET_INFO                 = 0x13E1;
    public const ushort    SPRITESET_SPRITE               = 0x13E2;
    // 0x1400..0x1402 are abandoned (pre-overlay "Layer/LayerSprite" model).
    // Do not reuse these numeric values — old project files emit them and a
    // mistakenly-overlapping new chunk would silently misparse legacy bytes.
    // Overlay-era replacements live at 0x1410..0x1413.
    public const ushort    SPRITESET_LAYER_DEPRECATED     = 0x1400;
    public const ushort    SPRITESET_LAYER_ENTRY_DEPRECATED = 0x1401;
    public const ushort    SPRITESET_LAYER_INFO_DEPRECATED = 0x1402;
    // Overlay model: per-project list of Overlay { Slots[8], Frames[] }.
    // SPRITESET_OVERLAY is a container; INFO carries name+slot count and the
    // single per-animation frame delay (1/50th sec); SLOT carries one of 8 fixed
    // slots (enabled, X, Y, expand, colors); FRAME carries the 8 bank-index
    // references (plus a legacy per-frame delay int kept for back/forward compat).
    public const ushort    SPRITESET_OVERLAY              = 0x1410;
    public const ushort    SPRITESET_OVERLAY_INFO         = 0x1411;
    public const ushort    SPRITESET_OVERLAY_SLOT         = 0x1412;
    public const ushort    SPRITESET_OVERLAY_FRAME        = 0x1413;
    // Settings for the "as game binary" sprite export (per-file dir/filename/
    // prefix/compress/override/max + .asm sidecar). Version-prefixed, append-only.
    public const ushort    SPRITESET_GAME_BINARY_SETTINGS = 0x1414;

    public const ushort    MULTICOLOR_DATA                = 0x1500;
    public const ushort    CHARSET_DATA                   = 0x1501;   // multicolor-data und binary data
    public const ushort    PALETTE                        = 0x1502;   // int num entries, n * ARGB (uint)

    public const ushort    DISASSEMBLY_INFO               = 0x1600;
    public const ushort    DISASSEMBLY_DATA               = 0x1601;
    public const ushort    DISASSEMBLY_JUMP_ADDRESSES     = 0x1602;
    public const ushort    DISASSEMBLY_NAMED_LABELS       = 0x1603;
    public const ushort    DISASSEMBLY_DATA_ADDRESSES     = 0x1604;

    public const ushort    BOOKMARKS                      = 0x1700;

    public const ushort    SETTINGS_TOOL                  = 0x2000;
    public const ushort    SETTINGS_ACCELERATOR           = 0x2001;
    public const ushort    SETTINGS_SOUND                 = 0x2002;
    public const ushort    SETTINGS_WINDOW                = 0x2003;
    public const ushort    SETTINGS_TEXT_EDITOR           = 0x2004;
    public const ushort    SETTINGS_FONT                  = 0x2005;
    public const ushort    SETTINGS_SYNTAX_COLORING       = 0x2006;
    public const ushort    SETTINGS_UI                    = 0x2007;
    public const ushort    SETTINGS_DEFAULTS              = 0x2008;
    public const ushort    SETTINGS_FIND_REPLACE          = 0x2009;
    public const ushort    SETTINGS_IGNORED_WARNINGS      = 0x200A;
    //public const ushort    SETTINGS_LAYOUT              = 0x200B;   // do not use anymore!
    public const ushort    SETTINGS_PANEL_DISPLAY_DETAILS = 0x200C;
    public const ushort    SETTINGS_DPS_LAYOUT            = 0x200D;
    public const ushort    SETTINGS_RUN_EMULATOR          = 0x200E;
    public const ushort    SETTINGS_BASIC_KEYMAP          = 0x200F;
    public const ushort    SETTINGS_BASIC_PARSER          = 0x2010;
    public const ushort    SETTINGS_ASSEMBLER_EDITOR      = 0x2011;
    public const ushort    SETTINGS_ENVIRONMENT           = 0x2012;
    public const ushort    SETTINGS_PERSPECTIVES          = 0x2013;
    public const ushort    SETTINGS_PERSPECTIVE           = 0x2014;
    public const ushort    SETTINGS_OUTLINE               = 0x2015;
    public const ushort    SETTINGS_HEX_VIEW              = 0x2016;
    public const ushort    SETTINGS_MRU_PROJECTS          = 0x2017;
    public const ushort    SETTINGS_MRU_FILES             = 0x2018;
    public const ushort    SETTINGS_WARNINGS_AS_ERRORS    = 0x2019;
    public const ushort    SETTINGS_C64STUDIO_HACKS       = 0x201A;
    public const ushort    SETTINGS_EDITOR_BEHAVIOURS     = 0x201B;
    public const ushort    SETTINGS_LABEL_EXPLORER        = 0x201C;
    public const ushort    SETTINGS_SOURCE_CONTROL        = 0x201D;
    public const ushort    SETTINGS_HELP                  = 0x201E;
    public const ushort    SETTINGS_MEMORY_VIEW           = 0x201F;
    public const ushort    SETTINGS_BREAKPOINTS           = 0x2020;
    public const ushort    SETTINGS_BREAKPOINT            = 0x2021;
    public const ushort    SETTINGS_DIALOG_APPEARANCE     = 0x2022;
    public const ushort    SETTINGS_PALETTE               = 0x2023;
    public const ushort    SETTINGS_DIALOG_DECISIONS      = 0x2024;
    public const ushort    SETTINGS_CODE_FORMATTING       = 0x2025;
    public const ushort    SETTINGS_MAP_EDITOR            = 0x2026;
    public const ushort    SETTINGS_THEME_MODE            = 0x2027;
    public const ushort    SETTINGS_DISPLAY_FILTERS       = 0x2028;
    public const ushort    SETTINGS_DISPLAY_FILTERS_DIALOG = 0x2029;
    // Krypton GlobalPaletteMode persisted from the global toolbar combo
    // (the dev-time palette tester promoted to a real setting). Stored as
    // an int32 so the underlying enum can grow without a chunk-shape break.
    public const ushort    SETTINGS_KRYPTON_PALETTE_MODE  = 0x202A;
    // Four 16-entry tables (Linear Up, Linear Down, Hue Up, Hue Down)
    // for the brightness-shift feature on the Map editor. Each cell
    // holds the C64 color index that the source color should advance
    // to in that direction, or -1 to mean "no neighbor — leave the
    // char unchanged". Defaults are hardcoded; the chunk only emits
    // when at least one entry differs from the default to keep
    // settings files small and forward-compat with no-customization
    // installs. Layout: [i32 × 16] × 4 in the order above.
    public const ushort    SETTINGS_BRIGHTNESS_TABLES     = 0x202B;
  }

}
