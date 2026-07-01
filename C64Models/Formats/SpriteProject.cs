using GR.Memory;
using RetroDevStudio;
using RetroDevStudio.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace RetroDevStudio.Formats
{
  public class SpriteProject
  {
    public enum SpriteProjectMode
    {
      [Description( "C64/Mega65 24x21" )]
      COMMODORE_24_X_21_HIRES_OR_MC,
      [Description( "Mega65 64x21" )]
      MEGA65_64_X_21_HIRES_OR_MC,
      [Description( "Mega65 16x21 16 colors" )]
      MEGA65_16_X_21_16_COLORS,
      [Description( "Commander X16 8x8 16 colors" )]
      COMMANDER_X16_8_8_16_COLORS,
      [Description( "Commander X16 16x8 16 colors" )]
      COMMANDER_X16_16_8_16_COLORS,
      [Description( "Commander X16 32x8 16 colors" )]
      COMMANDER_X16_32_8_16_COLORS,
      [Description( "Commander X16 64x8 16 colors" )]
      COMMANDER_X16_64_8_16_COLORS,
      [Description( "Commander X16 8x16 16 colors" )]
      COMMANDER_X16_8_16_16_COLORS,
      [Description( "Commander X16 16x16 16 colors" )]
      COMMANDER_X16_16_16_16_COLORS,
      [Description( "Commander X16 32x16 16 colors" )]
      COMMANDER_X16_32_16_16_COLORS,
      [Description( "Commander X16 64x16 16 colors" )]
      COMMANDER_X16_64_16_16_COLORS,
      [Description( "Commander X16 8x32 16 colors" )]
      COMMANDER_X16_8_32_16_COLORS,
      [Description( "Commander X16 16x32 16 colors" )]
      COMMANDER_X16_16_32_16_COLORS,
      [Description( "Commander X16 32x32 16 colors" )]
      COMMANDER_X16_32_32_16_COLORS,
      [Description( "Commander X16 64x32 16 colors" )]
      COMMANDER_X16_64_32_16_COLORS,
      [Description( "Commander X16 8x64 16 colors" )]
      COMMANDER_X16_8_64_16_COLORS,
      [Description( "Commander X16 16x64 16 colors" )]
      COMMANDER_X16_16_64_16_COLORS,
      [Description( "Commander X16 32x64 16 colors" )]
      COMMANDER_X16_32_64_16_COLORS,
      [Description( "Commander X16 64x64 16 colors" )]
      COMMANDER_X16_64_64_16_COLORS,
      [Description( "Commander X16 8x8 256 colors" )]
      COMMANDER_X16_8_8_256_COLORS,
      [Description( "Commander X16 16x8 256 colors" )]
      COMMANDER_X16_16_8_256_COLORS,
      [Description( "Commander X16 32x8 256 colors" )]
      COMMANDER_X16_32_8_256_COLORS,
      [Description( "Commander X16 64x8 256 colors" )]
      COMMANDER_X16_64_8_256_COLORS,
      [Description( "Commander X16 8x16 256 colors" )]
      COMMANDER_X16_8_16_256_COLORS,
      [Description( "Commander X16 16x16 256 colors" )]
      COMMANDER_X16_16_16_256_COLORS,
      [Description( "Commander X16 32x16 256 colors" )]
      COMMANDER_X16_32_16_256_COLORS,
      [Description( "Commander X16 64x16 256 colors" )]
      COMMANDER_X16_64_16_256_COLORS,
      [Description( "Commander X16 8x32 256 colors" )]
      COMMANDER_X16_8_32_256_COLORS,
      [Description( "Commander X16 16x32 256 colors" )]
      COMMANDER_X16_16_32_256_COLORS,
      [Description( "Commander X16 32x32 256 colors" )]
      COMMANDER_X16_32_32_256_COLORS,
      [Description( "Commander X16 64x32 256 colors" )]
      COMMANDER_X16_64_32_256_COLORS,
      [Description( "Commander X16 8x64 256 colors" )]
      COMMANDER_X16_8_64_256_COLORS,
      [Description( "Commander X16 16x64 256 colors" )]
      COMMANDER_X16_16_64_256_COLORS,
      [Description( "Commander X16 32x64 256 colors" )]
      COMMANDER_X16_32_64_256_COLORS,
      [Description( "Commander X16 64x64 256 colors" )]
      COMMANDER_X16_64_64_256_COLORS
    }



    public class SpriteData
    {
      public GraphicTile                Tile = null;
      public SpriteMode                 Mode = SpriteMode.COMMODORE_24_X_21_HIRES;


      public SpriteData( ColorSettings Settings )
      {
        Tile = new GraphicTile( 24, 21, GraphicTileMode.COMMODORE_HIRES, Settings );
        Tile.CustomColor = 1;
      }


      public SpriteData( SpriteData Other )
      {
        Mode          = Other.Mode;
        Tile          = new GraphicTile( Other.Tile );
      }
    }



    /// <summary>
    /// Overlay = a stack of up to 8 hardware sprites at one screen anchor.
    /// Slot 0 is the bottom of the pile, slot 7 the top. Each slot is an
    /// independently-enabled spot in the stack with its own (X,Y) pixel
    /// offset relative to the overlay origin and its own per-slot color
    /// settings; the actual bitmap+mode for the slot comes from the bank
    /// index named by the current animation frame.
    /// </summary>
    public class Overlay
    {
      public string                 Name   = "Overlay";
      public OverlaySlot[]          Slots  = new OverlaySlot[8] {
        new OverlaySlot(), new OverlaySlot(), new OverlaySlot(), new OverlaySlot(),
        new OverlaySlot(), new OverlaySlot(), new OverlaySlot(), new OverlaySlot()
      };
      public List<OverlayFrame>     Frames = new List<OverlayFrame>();
      // Single display time per frame for the WHOLE animation, in 1/50th-second
      // units (PAL jiffies), 1..255. 5 = 100 ms (the legacy default). One timing
      // per animation — there is no per-frame timing.
      public int                    FrameDelay = 5;
      // Whether the animation loops (true) or plays once then stops (false).
      // Exported to the game-binary sprite format as the per-animation Loop byte.
      public bool                   Loop = true;
      // Authored numeric animation id (0..255), used by the game-binary sprite
      // export so runtime code can look an animation up by id.
      public int                    AnimationID = 0;
      // Per-animation flag: when true, runtime should start playback at a random
      // frame instead of frame 0 (e.g. so many instances of the same animation
      // don't play in lockstep). Default false = start at frame 0.
      public bool                   StartAtRandomFrame = false;

      /// <summary>
      /// Deep copy: independent Slots and Frames so edits to the clone never
      /// touch the original. Canonical clone used by both the "Clone overlay"
      /// command and the undo snapshot.
      /// </summary>
      public Overlay Clone()
      {
        var c = new Overlay();
        c.Name        = Name;
        c.FrameDelay  = FrameDelay;
        c.Loop        = Loop;
        c.AnimationID = AnimationID;
        c.StartAtRandomFrame = StartAtRandomFrame;
        for ( int i = 0; ( i < c.Slots.Length ) && ( i < Slots.Length ); ++i )
        {
          var s   = Slots[i];
          var dst = c.Slots[i];
          dst.Enabled         = s.Enabled;
          dst.X               = s.X;
          dst.Y               = s.Y;
          dst.BackgroundColor = s.BackgroundColor;
          dst.MultiColor1     = s.MultiColor1;
          dst.MultiColor2     = s.MultiColor2;
          dst.CustomColor     = s.CustomColor;
          dst.ExpandX         = s.ExpandX;
          dst.ExpandY         = s.ExpandY;
        }
        foreach ( var f in Frames )
        {
          var fc = new OverlayFrame();
          for ( int i = 0; ( i < fc.BankIndex.Length ) && ( i < f.BankIndex.Length ); ++i )
          {
            fc.BankIndex[i] = f.BankIndex[i];
          }
          c.Frames.Add( fc );
        }
        return c;
      }
    }



    /// <summary>
    /// Structural slot definition inside an Overlay (8 fixed slots per
    /// overlay, indexed 0..7, slot 0 at the bottom of the visual stack).
    /// The slot stores per-slot screen offset and per-slot color settings.
    /// Note: BG/MC1/MC2 are global VIC-II registers on real C64 hardware
    /// (sprites can't have independent bg/mc colors). The per-slot color
    /// fields here are editor-only metadata so the user can preview the
    /// overlay under different palette assumptions when authoring.
    /// </summary>
    public class OverlaySlot
    {
      public bool       Enabled = false;
      public int        X = 0;
      public int        Y = 0;
      public int        BackgroundColor = 0;
      public int        MultiColor1 = 0;
      public int        MultiColor2 = 0;
      public int        CustomColor = 1;
      public bool       ExpandX = false;
      public bool       ExpandY = false;
    }



    /// <summary>
    /// One animation frame in an Overlay. Stores which bank sprite sits
    /// in each of the 8 slots at this point in time. Disabled slots have
    /// their bank index recorded but it is ignored at render time. The
    /// display time is a single per-animation value (see Overlay.FrameDelay),
    /// not per-frame.
    /// </summary>
    public class OverlayFrame
    {
      public int[]      BankIndex = new int[8];
    }



    /// <summary>
    /// Persisted UI settings for the "as game binary" sprite export. Two output
    /// files (anim-defs + sprite-data), each with its own directory / filename /
    /// prefix-load-address / compression / max-size; the anim-defs file also has
    /// a KickAssembler .asm const sidecar.
    /// </summary>
    public class SpriteGameBinarySettings
    {
      public string AnimDirectory      = "";
      public string AnimFilename       = "";
      public bool   AnimAsm            = true;
      public string AnimAsmFilename    = "sprite_anims.asm";
      public bool   AnimPrefix         = false;
      public string AnimPrefixHex      = "";
      public bool   AnimCompress       = false;
      public string AnimCompressFile   = "";
      public bool   AnimOverride       = false;
      public string AnimOverrideHex    = "";
      public bool   AnimMaxSize        = false;
      public string AnimMaxSizeText    = "0";

      public string SprDirectory       = "";
      public string SprFilename        = "";
      public bool   SprPrefix          = false;
      public string SprPrefixHex       = "";
      public bool   SprCompress        = false;
      public string SprCompressFile    = "";
      public bool   SprOverride        = false;
      public string SprOverrideHex     = "";
      public bool   SprMaxSize         = false;
      public string SprMaxSizeText     = "0";

      // Added later (appended to the end of the chunk): per-file compressor
      // selection. Only "ZX0" exists today; the dropdown is forward-looking.
      public string AnimCompressor     = "ZX0";
      public string SprCompressor      = "ZX0";

      // Added later (appended): dedicated directories for the .asm consts file and
      // each compressed-output file. Blank => fall back to the group directory.
      public string AnimAsmDirectory      = "";
      public string AnimCompressDirectory = "";
      public string SprCompressDirectory  = "";

      // ---- Clipboard copy/paste of the whole settings block --------------------
      // The export form's "Copy settings" / "Paste settings" buttons round-trip
      // EVERY field here through the clipboard as plain text.
      // IMPORTANT: when you add a field above, ALSO add it to ToClipboardString()
      // and FromClipboardString() below, or copy/paste will silently drop it.
      public const string ClipboardMagic = "C64Studio.SpriteGameBinarySettings.v1";

      /// <summary>Serialize every field to a clipboard-friendly text block.</summary>
      public string ToClipboardString()
      {
        var sb = new StringBuilder();
        sb.AppendLine( ClipboardMagic );
        sb.AppendLine( "AnimDirectory=" + ( AnimDirectory ?? "" ) );
        sb.AppendLine( "AnimFilename=" + ( AnimFilename ?? "" ) );
        sb.AppendLine( "AnimAsm=" + ( AnimAsm ? "1" : "0" ) );
        sb.AppendLine( "AnimAsmDirectory=" + ( AnimAsmDirectory ?? "" ) );
        sb.AppendLine( "AnimAsmFilename=" + ( AnimAsmFilename ?? "" ) );
        sb.AppendLine( "AnimPrefix=" + ( AnimPrefix ? "1" : "0" ) );
        sb.AppendLine( "AnimPrefixHex=" + ( AnimPrefixHex ?? "" ) );
        sb.AppendLine( "AnimCompress=" + ( AnimCompress ? "1" : "0" ) );
        sb.AppendLine( "AnimCompressor=" + ( AnimCompressor ?? "" ) );
        sb.AppendLine( "AnimCompressDirectory=" + ( AnimCompressDirectory ?? "" ) );
        sb.AppendLine( "AnimCompressFile=" + ( AnimCompressFile ?? "" ) );
        sb.AppendLine( "AnimOverride=" + ( AnimOverride ? "1" : "0" ) );
        sb.AppendLine( "AnimOverrideHex=" + ( AnimOverrideHex ?? "" ) );
        sb.AppendLine( "AnimMaxSize=" + ( AnimMaxSize ? "1" : "0" ) );
        sb.AppendLine( "AnimMaxSizeText=" + ( AnimMaxSizeText ?? "" ) );
        sb.AppendLine( "SprDirectory=" + ( SprDirectory ?? "" ) );
        sb.AppendLine( "SprFilename=" + ( SprFilename ?? "" ) );
        sb.AppendLine( "SprPrefix=" + ( SprPrefix ? "1" : "0" ) );
        sb.AppendLine( "SprPrefixHex=" + ( SprPrefixHex ?? "" ) );
        sb.AppendLine( "SprCompress=" + ( SprCompress ? "1" : "0" ) );
        sb.AppendLine( "SprCompressor=" + ( SprCompressor ?? "" ) );
        sb.AppendLine( "SprCompressDirectory=" + ( SprCompressDirectory ?? "" ) );
        sb.AppendLine( "SprCompressFile=" + ( SprCompressFile ?? "" ) );
        sb.AppendLine( "SprOverride=" + ( SprOverride ? "1" : "0" ) );
        sb.AppendLine( "SprOverrideHex=" + ( SprOverrideHex ?? "" ) );
        sb.AppendLine( "SprMaxSize=" + ( SprMaxSize ? "1" : "0" ) );
        sb.AppendLine( "SprMaxSizeText=" + ( SprMaxSizeText ?? "" ) );
        return sb.ToString();
      }

      /// <summary>
      /// Parse a clipboard text block produced by <see cref="ToClipboardString"/>.
      /// Returns null when the text is not our format. Unknown keys are ignored and
      /// missing keys keep their default, so old/newer clipboards still paste.
      /// </summary>
      public static SpriteGameBinarySettings FromClipboardString( string Text )
      {
        if ( string.IsNullOrEmpty( Text ) )
        {
          return null;
        }
        string[] lines = Text.Replace( "\r\n", "\n" ).Replace( "\r", "\n" ).Split( '\n' );
        int start = 0;
        while ( ( start < lines.Length ) && ( lines[start].Trim().Length == 0 ) )
        {
          ++start;
        }
        if ( ( start >= lines.Length ) || ( lines[start].Trim() != ClipboardMagic ) )
        {
          return null;
        }

        var map = new Dictionary<string, string>();
        for ( int i = start + 1; i < lines.Length; ++i )
        {
          int eq = lines[i].IndexOf( '=' );
          if ( eq <= 0 )
          {
            continue;
          }
          map[lines[i].Substring( 0, eq )] = lines[i].Substring( eq + 1 );
        }

        var s = new SpriteGameBinarySettings();
        s.AnimDirectory         = GetStr( map, "AnimDirectory", s.AnimDirectory );
        s.AnimFilename          = GetStr( map, "AnimFilename", s.AnimFilename );
        s.AnimAsm               = GetBool( map, "AnimAsm", s.AnimAsm );
        s.AnimAsmDirectory      = GetStr( map, "AnimAsmDirectory", s.AnimAsmDirectory );
        s.AnimAsmFilename       = GetStr( map, "AnimAsmFilename", s.AnimAsmFilename );
        s.AnimPrefix            = GetBool( map, "AnimPrefix", s.AnimPrefix );
        s.AnimPrefixHex         = GetStr( map, "AnimPrefixHex", s.AnimPrefixHex );
        s.AnimCompress          = GetBool( map, "AnimCompress", s.AnimCompress );
        s.AnimCompressor        = GetStr( map, "AnimCompressor", s.AnimCompressor );
        s.AnimCompressDirectory = GetStr( map, "AnimCompressDirectory", s.AnimCompressDirectory );
        s.AnimCompressFile      = GetStr( map, "AnimCompressFile", s.AnimCompressFile );
        s.AnimOverride          = GetBool( map, "AnimOverride", s.AnimOverride );
        s.AnimOverrideHex       = GetStr( map, "AnimOverrideHex", s.AnimOverrideHex );
        s.AnimMaxSize           = GetBool( map, "AnimMaxSize", s.AnimMaxSize );
        s.AnimMaxSizeText       = GetStr( map, "AnimMaxSizeText", s.AnimMaxSizeText );
        s.SprDirectory          = GetStr( map, "SprDirectory", s.SprDirectory );
        s.SprFilename           = GetStr( map, "SprFilename", s.SprFilename );
        s.SprPrefix             = GetBool( map, "SprPrefix", s.SprPrefix );
        s.SprPrefixHex          = GetStr( map, "SprPrefixHex", s.SprPrefixHex );
        s.SprCompress           = GetBool( map, "SprCompress", s.SprCompress );
        s.SprCompressor         = GetStr( map, "SprCompressor", s.SprCompressor );
        s.SprCompressDirectory  = GetStr( map, "SprCompressDirectory", s.SprCompressDirectory );
        s.SprCompressFile       = GetStr( map, "SprCompressFile", s.SprCompressFile );
        s.SprOverride           = GetBool( map, "SprOverride", s.SprOverride );
        s.SprOverrideHex        = GetStr( map, "SprOverrideHex", s.SprOverrideHex );
        s.SprMaxSize            = GetBool( map, "SprMaxSize", s.SprMaxSize );
        s.SprMaxSizeText        = GetStr( map, "SprMaxSizeText", s.SprMaxSizeText );
        return s;
      }

      private static string GetStr( Dictionary<string, string> Map, string Key, string Default )
      {
        string v;
        return Map.TryGetValue( Key, out v ) ? v : Default;
      }

      private static bool GetBool( Dictionary<string, string> Map, string Key, bool Default )
      {
        string v;
        return Map.TryGetValue( Key, out v ) ? ( v == "1" ) : Default;
      }
    }

    public SpriteGameBinarySettings GameBinary = new SpriteGameBinarySettings();

    public List<SpriteData>       Sprites  = new List<SpriteData>( 256 );
    public List<Overlay>          Overlays = new List<Overlay>();

    public ColorSettings  Colors = new ColorSettings();

    public string         Name = "";
    public string         ExportFilename = "";

    public int            ExportSpriteCount = 0;
    public int            ExportStartIndex = 0;
    // Which Export-tab method was last selected (index into comboExportMethod).
    // Persisted appended (position-guarded) in the SPRITESET_INFO chunk.
    public int            ExportMethodIndex = 0;
    public int            TotalNumberOfSprites = 256;
    public bool           ShowGrid = false;
    // Opacity (alpha 0..255) of the editor grid overlay drawn on the sprite
    // canvas. 255 = fully opaque white (the legacy look). Editor-only; persisted
    // in the SPRITESET_INFO chunk (appended, position-guarded for old files).
    public int            GridOpacity = 255;

    // Sprite-test panel (Overlay tab) settings — editor-only, persisted in the
    // SPRITESET_INFO chunk (appended, position-guarded). TestBackgroundColorIndex
    // is -1 = follow the project background colour, else a palette index 0..15.
    public int            TestBackgroundColorIndex = -1;
    public bool           TestExpandX = false;
    public bool           TestExpandY = false;
    public bool           TestLoop    = false;
    // Sprite-test playfield "C64 magnification": when enabled, the playfield
    // magnifies sprites by an integer factor = (auto-detected monitor resolution)
    // / (the C64 SCREEN size below), so a sprite appears roughly the size it would
    // on a C64 whose screen fills the monitor. TestTargetWidth/Height are the C64
    // screen's pixel dimensions you are targeting — default 320x200, set larger to
    // include borders or for PAL/NTSC differences. The monitor side is detected at
    // runtime (not stored). Editor-only; persisted appended (position-guarded) in
    // SPRITESET_INFO.
    public bool           TestUseC64Magnification = false;
    public int            TestTargetWidth  = 320;
    public int            TestTargetHeight = 200;

    public SpriteProjectMode    Mode = SpriteProjectMode.COMMODORE_24_X_21_HIRES_OR_MC;



    public SpriteProject()
    {
      Colors.Palette = ConstantData.DefaultPaletteC64();
      for ( int i = 0; i < TotalNumberOfSprites; ++i )
      {
        Sprites.Add( new SpriteData( Colors ) );
        PaletteManager.ApplyPalette( Sprites[i].Tile.Image );
      }
    }



    public GR.Memory.ByteBuffer SaveToBuffer()
    {
      GR.Memory.ByteBuffer projectFile = new GR.Memory.ByteBuffer();

      // version 3 = overlay model. Versions <=2 (legacy Layer/LayerSprite)
      // are no longer readable — the user authorized a clean break. The
      // reader rejects v<=2 with a clear message.
      projectFile.AppendU32( 3 );

      var chunkProject = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_PROJECT );

      var chunkInfo = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_INFO );
      chunkInfo.AppendI32( TotalNumberOfSprites );
      chunkInfo.AppendString( Name );
      chunkInfo.AppendString( ExportFilename );
      chunkInfo.AppendI32( ExportStartIndex );
      chunkInfo.AppendI32( ExportSpriteCount );
      // Appended later (position-guarded on read): editor grid opacity.
      chunkInfo.AppendI32( GridOpacity );
      // Appended later (position-guarded): sprite-test panel settings.
      chunkInfo.AppendI32( TestBackgroundColorIndex );
      chunkInfo.AppendI32( TestExpandX ? 1 : 0 );
      chunkInfo.AppendI32( TestExpandY ? 1 : 0 );
      chunkInfo.AppendI32( TestLoop    ? 1 : 0 );
      // Appended later (position-guarded on read): last-selected Export-tab method.
      chunkInfo.AppendI32( ExportMethodIndex );
      // Appended later: sprite-test playfield C64-magnification settings.
      chunkInfo.AppendI32( TestUseC64Magnification ? 1 : 0 );
      chunkInfo.AppendI32( TestTargetWidth );
      chunkInfo.AppendI32( TestTargetHeight );
      chunkProject.Append( chunkInfo.ToBuffer() );

      GR.IO.FileChunk chunkScreenMultiColorData = new GR.IO.FileChunk( FileChunkConstants.MULTICOLOR_DATA );
      chunkScreenMultiColorData.AppendI32( (byte)Mode );
      chunkScreenMultiColorData.AppendI32( (byte)Colors.BackgroundColor );
      chunkScreenMultiColorData.AppendI32( (byte)Colors.MultiColor1 );
      chunkScreenMultiColorData.AppendI32( (byte)Colors.MultiColor2 );
      chunkProject.Append( chunkScreenMultiColorData.ToBuffer() );

      foreach ( var pal in Colors.Palettes )
      {
        chunkProject.Append( pal.ToBuffer() );
      }

      foreach ( var sprite in Sprites )
      {
        GR.IO.FileChunk chunkSprite = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_SPRITE );
        chunkSprite.AppendI32( (int)sprite.Mode );
        chunkSprite.AppendI32( (int)sprite.Tile.Mode );
        chunkSprite.AppendI32( (int)sprite.Tile.CustomColor );
        chunkSprite.AppendI32( sprite.Tile.Width );
        chunkSprite.AppendI32( sprite.Tile.Height );
        chunkSprite.AppendI32( (int)sprite.Tile.Data.Length );
        chunkSprite.Append( sprite.Tile.Data );
        chunkSprite.AppendI32( sprite.Tile.Colors.ActivePalette );
        chunkSprite.AppendI32( sprite.Tile.Colors.PaletteOffset );

        chunkProject.Append( chunkSprite.ToBuffer() );
      }

      foreach ( var overlay in Overlays )
      {
        var chunkOverlay = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_OVERLAY );

        var chunkOverlayInfo = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_OVERLAY_INFO );
        chunkOverlayInfo.AppendString( overlay.Name ?? "" );
        chunkOverlayInfo.AppendI32( overlay.Slots.Length );
        // Single per-animation frame delay in 1/50th-second units (appended,
        // position-guarded on read). Old files lack it and derive it from the
        // legacy per-frame delays below.
        chunkOverlayInfo.AppendI32( overlay.FrameDelay );
        // Loop flag + authored animation id (appended, position-guarded on read).
        chunkOverlayInfo.AppendI32( overlay.Loop ? 1 : 0 );
        chunkOverlayInfo.AppendI32( overlay.AnimationID );
        // "Start at a random frame" flag (appended, position-guarded on read).
        chunkOverlayInfo.AppendI32( overlay.StartAtRandomFrame ? 1 : 0 );
        chunkOverlay.Append( chunkOverlayInfo.ToBuffer() );

        for ( int s = 0; s < overlay.Slots.Length; ++s )
        {
          var slot = overlay.Slots[s];
          var chunkSlot = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_OVERLAY_SLOT );
          chunkSlot.AppendI32( s );
          chunkSlot.AppendI32( slot.Enabled ? 1 : 0 );
          chunkSlot.AppendI32( slot.X );
          chunkSlot.AppendI32( slot.Y );
          chunkSlot.AppendI32( slot.ExpandX ? 1 : 0 );
          chunkSlot.AppendI32( slot.ExpandY ? 1 : 0 );
          chunkSlot.AppendI32( slot.BackgroundColor );
          chunkSlot.AppendI32( slot.MultiColor1 );
          chunkSlot.AppendI32( slot.MultiColor2 );
          chunkSlot.AppendI32( slot.CustomColor );
          chunkOverlay.Append( chunkSlot.ToBuffer() );
        }

        foreach ( var frame in overlay.Frames )
        {
          var chunkFrame = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_OVERLAY_FRAME );
          // Legacy per-frame delay slot: kept for binary stability and so older
          // builds still animate. New builds carry the real timing per-animation
          // in OVERLAY_INFO and ignore this value on read.
          chunkFrame.AppendI32( overlay.FrameDelay * 20 );
          chunkFrame.AppendI32( frame.BankIndex.Length );
          for ( int s = 0; s < frame.BankIndex.Length; ++s )
          {
            chunkFrame.AppendI32( frame.BankIndex[s] );
          }
          chunkOverlay.Append( chunkFrame.ToBuffer() );
        }

        chunkProject.Append( chunkOverlay.ToBuffer() );
      }

      // "as game binary" sprite export settings (version-prefixed, append-only).
      var chunkGameBinary = new GR.IO.FileChunk( FileChunkConstants.SPRITESET_GAME_BINARY_SETTINGS );
      chunkGameBinary.AppendU32( 1 );
      chunkGameBinary.AppendString( GameBinary.AnimDirectory ?? "" );
      chunkGameBinary.AppendString( GameBinary.AnimFilename ?? "" );
      chunkGameBinary.AppendI32( GameBinary.AnimAsm ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.AnimAsmFilename ?? "" );
      chunkGameBinary.AppendI32( GameBinary.AnimPrefix ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.AnimPrefixHex ?? "" );
      chunkGameBinary.AppendI32( GameBinary.AnimCompress ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.AnimCompressFile ?? "" );
      chunkGameBinary.AppendI32( GameBinary.AnimOverride ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.AnimOverrideHex ?? "" );
      chunkGameBinary.AppendI32( GameBinary.AnimMaxSize ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.AnimMaxSizeText ?? "0" );
      chunkGameBinary.AppendString( GameBinary.SprDirectory ?? "" );
      chunkGameBinary.AppendString( GameBinary.SprFilename ?? "" );
      chunkGameBinary.AppendI32( GameBinary.SprPrefix ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.SprPrefixHex ?? "" );
      chunkGameBinary.AppendI32( GameBinary.SprCompress ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.SprCompressFile ?? "" );
      chunkGameBinary.AppendI32( GameBinary.SprOverride ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.SprOverrideHex ?? "" );
      chunkGameBinary.AppendI32( GameBinary.SprMaxSize ? 1 : 0 );
      chunkGameBinary.AppendString( GameBinary.SprMaxSizeText ?? "0" );
      // Appended later (no version bump - position-guarded on read): compressors.
      chunkGameBinary.AppendString( GameBinary.AnimCompressor ?? "ZX0" );
      chunkGameBinary.AppendString( GameBinary.SprCompressor ?? "ZX0" );
      // Appended later: dedicated .asm / compressed-output directories.
      chunkGameBinary.AppendString( GameBinary.AnimAsmDirectory ?? "" );
      chunkGameBinary.AppendString( GameBinary.AnimCompressDirectory ?? "" );
      chunkGameBinary.AppendString( GameBinary.SprCompressDirectory ?? "" );
      chunkProject.Append( chunkGameBinary.ToBuffer() );

      projectFile.Append( chunkProject.ToBuffer() );

      return projectFile;
    }



    public bool ReadFromBuffer( GR.Memory.ByteBuffer DataIn )
    {
      if ( DataIn == null )
      {
        return false;
      }

      GR.IO.MemoryReader memIn = DataIn.MemoryReader();

      uint     Version = memIn.ReadUInt32();

      if ( Version < 3 )
      {
        // Pre-overlay project files used Layer/LayerSprite. The user
        // explicitly authorized a clean break — old projects don't load.
        // Don't clear our state — leave the in-memory project intact so
        // the editor (which is already showing a default 256-sprite bank)
        // doesn't crash on the next operation.
        Debug.Log( "SpriteProject.ReadFromBuffer: project version " + Version + " is too old. Re-create the sprite project." );
        return false;
      }

      // Only clear once we know we have a parseable v3 stream.
      Overlays.Clear();
      Sprites.Clear();
      Colors.Palettes.Clear();

      GR.IO.FileChunk   chunkMain = new GR.IO.FileChunk();

      while ( chunkMain.ReadFromStream( memIn ) )
      {
        switch ( chunkMain.Type )
        {
          case FileChunkConstants.SPRITESET_PROJECT:
            {
              var    chunkReader = chunkMain.MemoryReader();

              GR.IO.FileChunk   subChunk = new GR.IO.FileChunk();

              while ( subChunk.ReadFromStream( chunkReader ) )
              {
                var    subChunkReader = subChunk.MemoryReader();

                switch ( subChunk.Type )
                {
                  case FileChunkConstants.SPRITESET_INFO:
                    TotalNumberOfSprites  = subChunkReader.ReadInt32();
                    Name                  = subChunkReader.ReadString();
                    ExportFilename        = subChunkReader.ReadString();
                    ExportStartIndex      = subChunkReader.ReadInt32();
                    ExportSpriteCount     = subChunkReader.ReadInt32();
                    // Position-guarded: files saved before this field default to 255.
                    GridOpacity = 255;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      GridOpacity = subChunkReader.ReadInt32();
                    }
                    if ( GridOpacity < 0 )   GridOpacity = 0;
                    if ( GridOpacity > 255 ) GridOpacity = 255;
                    // Sprite-test panel settings (position-guarded; defaults for old files).
                    TestBackgroundColorIndex = -1;
                    TestExpandX = false;
                    TestExpandY = false;
                    TestLoop    = false;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestBackgroundColorIndex = subChunkReader.ReadInt32();
                    }
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestExpandX = ( subChunkReader.ReadInt32() != 0 );
                    }
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestExpandY = ( subChunkReader.ReadInt32() != 0 );
                    }
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestLoop = ( subChunkReader.ReadInt32() != 0 );
                    }
                    if ( TestBackgroundColorIndex < -1 ) TestBackgroundColorIndex = -1;
                    if ( TestBackgroundColorIndex > 15 ) TestBackgroundColorIndex = 15;
                    // Position-guarded: last-selected export method (old files default to 0).
                    ExportMethodIndex = 0;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      ExportMethodIndex = subChunkReader.ReadInt32();
                    }
                    // Position-guarded: sprite-test C64-magnification settings.
                    // TestTargetWidth/Height are the C64 SCREEN size (default the
                    // 320x200 visible area; the monitor side is detected at runtime).
                    TestUseC64Magnification = false;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestUseC64Magnification = ( subChunkReader.ReadInt32() != 0 );
                    }
                    TestTargetWidth = 320;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestTargetWidth = subChunkReader.ReadInt32();
                    }
                    TestTargetHeight = 200;
                    if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                    {
                      TestTargetHeight = subChunkReader.ReadInt32();
                    }
                    break;
                  case FileChunkConstants.MULTICOLOR_DATA:
                    Mode = (SpriteProjectMode)subChunkReader.ReadInt32();
                    Colors.BackgroundColor = subChunkReader.ReadInt32();
                    Colors.MultiColor1 = subChunkReader.ReadInt32();
                    Colors.MultiColor2 = subChunkReader.ReadInt32();
                    Colors.ActivePalette = 0;
                    break;
                  case FileChunkConstants.PALETTE:
                    Colors.Palettes.Add( Palette.Read( subChunkReader ) );
                    break;
                  case FileChunkConstants.SPRITESET_SPRITE:
                    {
                      var sprite = new SpriteData( new ColorSettings( Colors ) );

                      sprite.Mode = (SpriteMode)subChunkReader.ReadInt32();
                      sprite.Tile.Mode = (GraphicTileMode)subChunkReader.ReadInt32();
                      sprite.Tile.CustomColor = (byte)subChunkReader.ReadInt32();
                      sprite.Tile.Width = subChunkReader.ReadInt32();
                      sprite.Tile.Height = subChunkReader.ReadInt32();
                      int dataLength = subChunkReader.ReadInt32();
                      sprite.Tile.Data = new GR.Memory.ByteBuffer();
                      subChunkReader.ReadBlock( sprite.Tile.Data, (uint)dataLength );
                      if ( sprite.Tile.CustomColor == 255 )
                      {
                        sprite.Tile.CustomColor = 1;
                      }

                      sprite.Tile.Colors.ActivePalette = subChunkReader.ReadInt32();
                      sprite.Tile.Colors.PaletteOffset = subChunkReader.ReadInt32();
                      sprite.Tile.Image = new GR.Image.MemoryImage( sprite.Tile.Width, sprite.Tile.Height, GR.Drawing.PixelFormat.Format32bppRgb );

                      // bugfix - mega65 sprites have a different mode
                      if ( sprite.Tile.Mode == GraphicTileMode.MEGA65_NCM_CHARACTERS )
                      {
                        sprite.Tile.Mode = GraphicTileMode.MEGA65_NCM_SPRITES;
                      }

                      Sprites.Add( sprite );
                    }
                    break;
                  case FileChunkConstants.SPRITESET_OVERLAY:
                    {
                      var overlay = new Overlay();
                      Overlays.Add( overlay );
                      // For old files (no per-animation FrameDelay in OVERLAY_INFO),
                      // derive the timing from the first frame's legacy ms afterwards.
                      bool overlayHasDelay = false;
                      int  firstLegacyDelayMS = -1;

                      GR.IO.FileChunk   subChunkO = new GR.IO.FileChunk();

                      while ( subChunkO.ReadFromStream( subChunkReader ) )
                      {
                        var subChunkReaderO = subChunkO.MemoryReader();

                        if ( subChunkO.Type == FileChunkConstants.SPRITESET_OVERLAY_INFO )
                        {
                          overlay.Name = subChunkReaderO.ReadString();
                          // numSlots is informational; we always have 8 fixed slots
                          subChunkReaderO.ReadInt32();
                          // Per-animation frame delay (1/50th sec), position-guarded.
                          if ( subChunkReaderO.Size - subChunkReaderO.Position >= 4 )
                          {
                            overlay.FrameDelay = subChunkReaderO.ReadInt32();
                            overlayHasDelay = true;
                          }
                          // Loop flag + authored animation id, position-guarded.
                          if ( subChunkReaderO.Size - subChunkReaderO.Position >= 4 )
                          {
                            overlay.Loop = ( subChunkReaderO.ReadInt32() != 0 );
                          }
                          if ( subChunkReaderO.Size - subChunkReaderO.Position >= 4 )
                          {
                            overlay.AnimationID = subChunkReaderO.ReadInt32();
                          }
                          if ( subChunkReaderO.Size - subChunkReaderO.Position >= 4 )
                          {
                            overlay.StartAtRandomFrame = ( subChunkReaderO.ReadInt32() != 0 );
                          }
                        }
                        else if ( subChunkO.Type == FileChunkConstants.SPRITESET_OVERLAY_SLOT )
                        {
                          int slotIndex = subChunkReaderO.ReadInt32();
                          if ( slotIndex < 0 || slotIndex >= overlay.Slots.Length ) continue;
                          var slot = overlay.Slots[slotIndex];
                          slot.Enabled         = ( subChunkReaderO.ReadInt32() != 0 );
                          slot.X               = subChunkReaderO.ReadInt32();
                          slot.Y               = subChunkReaderO.ReadInt32();
                          slot.ExpandX         = ( subChunkReaderO.ReadInt32() != 0 );
                          slot.ExpandY         = ( subChunkReaderO.ReadInt32() != 0 );
                          slot.BackgroundColor = subChunkReaderO.ReadInt32();
                          slot.MultiColor1     = subChunkReaderO.ReadInt32();
                          slot.MultiColor2     = subChunkReaderO.ReadInt32();
                          slot.CustomColor     = subChunkReaderO.ReadInt32();
                        }
                        else if ( subChunkO.Type == FileChunkConstants.SPRITESET_OVERLAY_FRAME )
                        {
                          var frame = new OverlayFrame();
                          // Legacy per-frame delay (ms): ignored when the overlay
                          // carries a real per-animation delay; otherwise the first
                          // one seeds the per-animation timing for old files.
                          int legacyDelayMS = subChunkReaderO.ReadInt32();
                          if ( firstLegacyDelayMS < 0 ) firstLegacyDelayMS = legacyDelayMS;
                          int numRefs = subChunkReaderO.ReadInt32();
                          for ( int s = 0; s < numRefs; ++s )
                          {
                            int v = subChunkReaderO.ReadInt32();
                            if ( s < frame.BankIndex.Length ) frame.BankIndex[s] = v;
                          }
                          overlay.Frames.Add( frame );
                        }
                      }
                      // Old file with no per-animation delay: derive it from the
                      // first legacy per-frame ms (rounded to 1/50th sec).
                      if ( ( !overlayHasDelay )
                      &&   ( firstLegacyDelayMS > 0 ) )
                      {
                        overlay.FrameDelay = ( firstLegacyDelayMS + 10 ) / 20;
                      }
                      if ( overlay.FrameDelay < 1 )   overlay.FrameDelay = 1;
                      if ( overlay.FrameDelay > 255 ) overlay.FrameDelay = 255;
                    }
                    break;
                  case FileChunkConstants.SPRITESET_GAME_BINARY_SETTINGS:
                    {
                      uint gbVersion = subChunkReader.ReadUInt32();
                      if ( gbVersion >= 1 )
                      {
                        GameBinary.AnimDirectory    = subChunkReader.ReadString();
                        GameBinary.AnimFilename      = subChunkReader.ReadString();
                        GameBinary.AnimAsm           = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.AnimAsmFilename    = subChunkReader.ReadString();
                        GameBinary.AnimPrefix        = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.AnimPrefixHex     = subChunkReader.ReadString();
                        GameBinary.AnimCompress      = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.AnimCompressFile  = subChunkReader.ReadString();
                        GameBinary.AnimOverride      = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.AnimOverrideHex   = subChunkReader.ReadString();
                        GameBinary.AnimMaxSize       = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.AnimMaxSizeText   = subChunkReader.ReadString();
                        GameBinary.SprDirectory      = subChunkReader.ReadString();
                        GameBinary.SprFilename       = subChunkReader.ReadString();
                        GameBinary.SprPrefix         = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.SprPrefixHex      = subChunkReader.ReadString();
                        GameBinary.SprCompress       = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.SprCompressFile   = subChunkReader.ReadString();
                        GameBinary.SprOverride       = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.SprOverrideHex    = subChunkReader.ReadString();
                        GameBinary.SprMaxSize        = ( subChunkReader.ReadInt32() != 0 );
                        GameBinary.SprMaxSizeText    = subChunkReader.ReadString();
                        // Appended later: per-file compressor selection.
                        if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                        {
                          GameBinary.AnimCompressor  = subChunkReader.ReadString();
                        }
                        if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                        {
                          GameBinary.SprCompressor   = subChunkReader.ReadString();
                        }
                        // Appended later: dedicated .asm / compressed-output directories.
                        if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                        {
                          GameBinary.AnimAsmDirectory = subChunkReader.ReadString();
                        }
                        if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                        {
                          GameBinary.AnimCompressDirectory = subChunkReader.ReadString();
                        }
                        if ( subChunkReader.Size - subChunkReader.Position >= 4 )
                        {
                          GameBinary.SprCompressDirectory = subChunkReader.ReadString();
                        }
                      }
                    }
                    break;
                }
              }
            }
            break;
          default:
            Debug.Log( "SpriteProject.ReadFromBuffer unexpected chunk type " + chunkMain.Type.ToString( "X" ) );
            return false;
        }
      }

      while ( Sprites.Count > 256 )
      {
        Sprites.RemoveAt( 256 );
      }

      return true;
    }



    public ByteBuffer GetPaletteExportData( int StartIndex, int NumColors, bool Swizzled, bool SortedByColorTriplets )
    {
      // get all palette datas, first all R, then all G, then all B
      var palData = new ByteBuffer();
      for ( int i = 0; i < Colors.Palettes.Count; ++i )
      {
        var curPal = Colors.Palettes[i];

        //Debug.Log( "orig pal data: " + curPal.GetExportData( 0, curPal.NumColors, false ).ToString() );

        palData.Append( curPal.GetExportData( 0, curPal.NumColors, Swizzled, SortedByColorTriplets ) );
      }

      //Debug.Log( "Total Pal Data: " + palData.ToString() );

      // pal data has rgbrgbrgb, we need to copy all r,g,bs behind each other

      var orderedPalData = new ByteBuffer();

      for ( int i = 0; i < 3; ++i )
      {
        for ( int j = 0; j < Colors.Palettes.Count; ++j )
        {
          orderedPalData.Append( palData.SubBuffer( ( i + j * 3 ) * Colors.Palettes[0].NumColors, Colors.Palettes[0].NumColors ) );
        }
      }

      var finalPalData = new ByteBuffer();
      int totalNumColors = Colors.Palettes.Count * Colors.Palettes[0].NumColors;

      // extract R, G and B
      finalPalData.Append( orderedPalData.SubBuffer( StartIndex, NumColors ) );
      finalPalData.Append( orderedPalData.SubBuffer( totalNumColors + StartIndex, NumColors ) );
      finalPalData.Append( orderedPalData.SubBuffer( 2 * totalNumColors + StartIndex, NumColors ) );

      //Debug.Log( "GetPaletteExportData: " + finalPalData.ToString() );
      return finalPalData;
    }



    // ============================================================================
    //  Game-binary sprite export (relocatable, mirrors the map game-binary export)
    // ============================================================================
    //
    // Two outputs, per the Dreadhold sprite-animation spec:
    //   * AnimDefs   — a relocatable header + file-relative offset tables + the
    //                  animation-definition byte streams.
    //   * SpriteData — raw sprite blocks (SPRITE_BLOCK_SIZE each), no header.
    //
    // For now only the FIRST overlay is exported, using SLOT 0 (single sprite per
    // frame). Each frame = { sprite pointer (compact block index), color }.
    // Only the sprites actually referenced by the animation are written, remapped
    // to a compact 0..N-1 block index.

    public const int SPRITE_BLOCK_SIZE = 64;   // VIC-II sprite block (63 used + 1 pad)
    public const int SPRITE_ANIM_FRAME_SIZE = 2;

    // Header byte offsets (mirror the map's HDR_* constants).
    private const int SPR_HDR_OFFSET_ANIM_DEF_LO = 0x04;
    private const int SPR_HDR_OFFSET_ANIM_DEF_HI = 0x06;
    private const int SPR_HDR_OFFSET_ANIM_ID     = 0x08;
    private const int SPR_HDR_SIZE               = 0x0A;

    /// <summary>
    /// Build the two game-binary sprite buffers. Returns false only on a hard
    /// failure; an empty project yields a valid header with 0 animations/sprites.
    /// </summary>
    public bool ExportAsGameBinarySprites(
      out GR.Memory.ByteBuffer AnimDefs,
      out GR.Memory.ByteBuffer SpriteData,
      out int AnimationCount,
      out int SpriteCount )
    {
      AnimDefs   = new GR.Memory.ByteBuffer();
      SpriteData = new GR.Memory.ByteBuffer();
      AnimationCount = 0;
      SpriteCount    = 0;

      // 1) Shared sprite bank: compact every sprite referenced (slot 0) by ANY
      //    overlay's frames, in first-use order across all overlays. compactOf maps
      //    the ORIGINAL sprite index to its 0..N-1 block; orderedOriginals[k] is the
      //    original index whose bytes occupy block k. Built ONCE and shared by every
      //    animation definition, so a sprite used by two animations is stored a
      //    single time and both animations refer to the same block.
      var compactOf        = new Dictionary<int, int>();
      var orderedOriginals = new List<int>();
      foreach ( var ov in Overlays )
      {
        foreach ( var frame in ov.Frames )
        {
          int orig = frame.BankIndex[0];
          if ( ( orig < 0 ) || ( orig >= Sprites.Count ) )
          {
            continue;   // out-of-range bank reference — skip
          }
          if ( !compactOf.ContainsKey( orig ) )
          {
            compactOf[orig] = orderedOriginals.Count;
            orderedOriginals.Add( orig );
          }
        }
      }
      SpriteCount = orderedOriginals.Count;

      // 2) Sprite-data file: each referenced sprite as a SPRITE_BLOCK_SIZE block,
      //    in compact order (block k == orderedOriginals[k]'s bytes).
      foreach ( int orig in orderedOriginals )
      {
        var data = Sprites[orig].Tile.Data;
        int avail = (int)data.Length;
        for ( int b = 0; b < SPRITE_BLOCK_SIZE; ++b )
        {
          SpriteData.AppendU8( ( b < avail ) ? data.ByteAt( b ) : (byte)0 );
        }
      }

      // 3) One animation-definition stream per overlay, all referring to the shared
      //    sprite bank above.
      AnimationCount = Overlays.Count;

      var defStreams = new List<GR.Memory.ByteBuffer>( Overlays.Count );
      var animIds    = new List<byte>( Overlays.Count );
      foreach ( var overlay in Overlays )
      {
        byte animId       = (byte)overlay.AnimationID;
        byte delay        = (byte)overlay.FrameDelay;
        byte expandX      = (byte)( overlay.Slots[0].ExpandX ? 1 : 0 );
        byte expandY      = (byte)( overlay.Slots[0].ExpandY ? 1 : 0 );
        // multicolor: from this animation's first referenced (in-range) sprite.
        byte multicolor   = 0;
        foreach ( var frame in overlay.Frames )
        {
          int o = frame.BankIndex[0];
          if ( ( o >= 0 ) && ( o < Sprites.Count ) )
          {
            multicolor = (byte)( ( Sprites[o].Mode == SpriteMode.COMMODORE_24_X_21_MULTICOLOR ) ? 1 : 0 );
            break;
          }
        }
        byte frameCount   = (byte)overlay.Frames.Count;
        byte loop         = (byte)( overlay.Loop ? 1 : 0 );
        byte startRandom  = (byte)( overlay.StartAtRandomFrame ? 1 : 0 );
        // Fallback colour, only used for a frame whose bank reference is out of
        // range (its sprite pointer also falls back to block 0). Valid frames take
        // their colour from the referenced bank sprite below.
        byte fallbackColor = (byte)( overlay.Slots[0].CustomColor & 0x0F );

        var defStream = new GR.Memory.ByteBuffer();
        defStream.AppendU8( animId );       // +$00 id
        defStream.AppendU8( delay );        // +$01 delay (1/50s)
        defStream.AppendU8( expandX );      // +$02 expand x
        defStream.AppendU8( expandY );      // +$03 expand y
        defStream.AppendU8( multicolor );   // +$04 multicolor
        defStream.AppendU8( frameCount );   // +$05 frame count
        defStream.AppendU8( loop );         // +$06 loop
        defStream.AppendU8( startRandom );  // +$07 start at a random frame
        // +$08.. frames: { sprite pointer (compact block), color }. The colour is
        // the referenced bank sprite's own CustomColor (the C64 per-sprite colour
        // register), so each frame exports its sprite's true colour - matching the
        // animation preview - rather than a single overlay colour for all frames.
        foreach ( var frame in overlay.Frames )
        {
          int orig = frame.BankIndex[0];
          byte ptr = (byte)( compactOf.ContainsKey( orig ) ? compactOf[orig] : 0 );
          byte color = ( ( orig >= 0 ) && ( orig < Sprites.Count ) )
                     ? (byte)( Sprites[orig].Tile.CustomColor & 0x0F )
                     : fallbackColor;
          defStream.AppendU8( ptr );
          defStream.AppendU8( color );
        }

        defStreams.Add( defStream );
        animIds.Add( animId );
      }

      // 4) Header (10 bytes, $0A). Pointers are FILE-RELATIVE offsets (patched).
      AnimDefs.AppendU8( (byte)AnimationCount );   // +$00 animation_count
      AnimDefs.AppendU8( (byte)SpriteCount );       // +$01 sprite_count
      AnimDefs.AppendU8( (byte)SPRITE_BLOCK_SIZE ); // +$02 sprite_data_stride ($40)
      AnimDefs.AppendU8( (byte)SPRITE_ANIM_FRAME_SIZE ); // +$03 anim_frame_size ($02)
      AnimDefs.AppendU16( 0 );                      // +$04 off_anim_def_lo
      AnimDefs.AppendU16( 0 );                      // +$06 off_anim_def_hi
      AnimDefs.AppendU16( 0 );                      // +$08 off_anim_id

      if ( AnimationCount > 0 )
      {
        // Three parallel tables, one entry per animation: lo byte, hi byte, id.
        int loTablePos = (int)AnimDefs.Length;
        AnimDefs.SetU16At( SPR_HDR_OFFSET_ANIM_DEF_LO, (ushort)loTablePos );
        for ( int a = 0; a < AnimationCount; ++a )
        {
          AnimDefs.AppendU8( 0 );   // lo placeholder (patched once its def addr is known)
        }

        int hiTablePos = (int)AnimDefs.Length;
        AnimDefs.SetU16At( SPR_HDR_OFFSET_ANIM_DEF_HI, (ushort)hiTablePos );
        for ( int a = 0; a < AnimationCount; ++a )
        {
          AnimDefs.AppendU8( 0 );   // hi placeholder
        }

        int idTablePos = (int)AnimDefs.Length;
        AnimDefs.SetU16At( SPR_HDR_OFFSET_ANIM_ID, (ushort)idTablePos );
        for ( int a = 0; a < AnimationCount; ++a )
        {
          AnimDefs.AppendU8( animIds[a] );
        }

        // Append each animation's def stream and patch its lo/hi table entry to its
        // file-relative offset.
        for ( int a = 0; a < AnimationCount; ++a )
        {
          int defAddr = (int)AnimDefs.Length;
          AnimDefs.SetU8At( loTablePos + a, (byte)( defAddr & 0xFF ) );
          AnimDefs.SetU8At( hiTablePos + a, (byte)( ( defAddr >> 8 ) & 0xFF ) );
          AnimDefs.Append( defStreams[a] );
        }
      }

      return true;
    }



    /// <summary>
    /// KickAssembler constants file matching the byte layout of the anim-defs
    /// binary produced by <see cref="ExportAsGameBinarySprites"/>. Naming mirrors
    /// the map export's GenerateGameBinaryHeaderAsm (SPRITE_HEADER_* offsets,
    /// SPRITE_ANIM_* record fields). <paramref name="UserPrefix"/> is emitted
    /// verbatim at the top.
    /// </summary>
    public string GenerateSpriteGameBinaryHeaderAsm( string UserPrefix = null )
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
      sb.AppendLine( "// Auto-generated by C64Studio on sprite game-binary export." );
      sb.AppendLine( "// Constants mirror the byte layout of the accompanying anim-defs .bin." );
      sb.AppendLine( "// Do not edit by hand — regenerated on every export." );
      sb.AppendLine( "//" );
      sb.AppendLine( "// All SPRITE_HEADER_* values are byte OFFSETS into the header, not stored" );
      sb.AppendLine( "// values. The 16-bit pointers in the header (and the per-animation LO/HI" );
      sb.AppendLine( "// tables) are FILE-RELATIVE OFFSETS — the byte distance from the start of" );
      sb.AppendLine( "// the anim-defs binary, NOT absolute addresses. Add your load address at" );
      sb.AppendLine( "// runtime before dereferencing. A pointer of 0 means 'absent'." );
      sb.AppendLine( "//" );
      sb.AppendLine( "// Sprite data lives in the SEPARATE sprite-data file: SPRITE_DATA_STRIDE" );
      sb.AppendLine( "// bytes per sprite, loaded at a 64-byte-aligned address; a frame's sprite" );
      sb.AppendLine( "// pointer is the 0-based block index into it." );
      sb.AppendLine();
      sb.AppendLine( "// ====== Sprite anim-defs header (10 bytes) ======" );
      sb.AppendLine( ".const SPRITE_HEADER_ANIMATION_COUNT             = $00  // byte: number of animations" );
      sb.AppendLine( ".const SPRITE_HEADER_SPRITE_COUNT                = $01  // byte: number of sprite blocks" );
      sb.AppendLine( ".const SPRITE_HEADER_SPRITE_DATA_STRIDE          = $02  // byte: bytes per sprite block ($40)" );
      sb.AppendLine( ".const SPRITE_HEADER_ANIM_FRAME_SIZE             = $03  // byte: bytes per anim frame ($02)" );
      sb.AppendLine( "// Pointer tables (16-bit each) — file-relative offsets (add your load address):" );
      sb.AppendLine( ".const SPRITE_HEADER_OFFSET_ANIM_DEF_LO          = $04  // -> anim_def_lo[animation_count]" );
      sb.AppendLine( ".const SPRITE_HEADER_OFFSET_ANIM_DEF_HI          = $06  // -> anim_def_hi[animation_count]" );
      sb.AppendLine( ".const SPRITE_HEADER_OFFSET_ANIM_ID              = $08  // -> anim_id[animation_count]" );
      sb.AppendLine( ".const SPRITE_HEADER_SIZE                        = $0A  // total header length" );
      sb.AppendLine();
      sb.AppendLine( "// ====== Animation definition record layout ======" );
      sb.AppendLine( "// Field byte offsets within one animation definition." );
      sb.AppendLine( ".const SPRITE_ANIM_ID                            = $00" );
      sb.AppendLine( ".const SPRITE_ANIM_DELAY                         = $01  // 1/50th-second units" );
      sb.AppendLine( ".const SPRITE_ANIM_EXPAND_X                      = $02" );
      sb.AppendLine( ".const SPRITE_ANIM_EXPAND_Y                      = $03" );
      sb.AppendLine( ".const SPRITE_ANIM_MULTICOLOR                    = $04" );
      sb.AppendLine( ".const SPRITE_ANIM_FRAME_COUNT                   = $05" );
      sb.AppendLine( ".const SPRITE_ANIM_LOOP                          = $06  // 0 = play once, !=0 = loop" );
      sb.AppendLine( ".const SPRITE_ANIM_START_RANDOM                  = $07  // 0 = start at frame 0, !=0 = start at a random frame" );
      sb.AppendLine( ".const SPRITE_ANIM_FRAMES                        = $08  // frames start here; step by SPRITE_ANIM_FRAME_SIZE" );
      sb.AppendLine( ".const SPRITE_ANIM_FRAME_SIZE                    = $02  // bytes per frame" );
      sb.AppendLine( ".const SPRITE_ANIM_FRAME_SPRITE_PTR              = $00  // within a frame: sprite block index" );
      sb.AppendLine( ".const SPRITE_ANIM_FRAME_COLOR                   = $01  // within a frame: sprite colour" );
      sb.AppendLine();
      sb.AppendLine( "// ====== Sprite data (separate file) ======" );
      sb.AppendLine( ".const SPRITE_DATA_STRIDE                        = $40  // 64 bytes per sprite block" );

      // NOTE: per-animation name symbols ( .const ANIM_<name> = <index> ) are
      // intentionally NOT emitted yet — the naming/indexing scheme is still TBD.
      // MakeAsmIdentifier is kept for when that is added back.
      return sb.ToString();
    }



    private static string LayoutAddr( int Offset )
    {
      return "$" + Offset.ToString( "X4" );
    }



    private static string LayoutHexByte( int Value )
    {
      return "$" + ( Value & 0xFF ).ToString( "X2" );
    }



    private static string LayoutHexBytes( GR.Memory.ByteBuffer Buf, int Offset, int Count )
    {
      var sb = new StringBuilder();
      for ( int i = 0; i < Count; ++i )
      {
        if ( i > 0 )
        {
          sb.Append( ' ' );
        }
        sb.Append( LayoutHexByte( Buf.ByteAt( Offset + i ) ) );
      }
      return sb.ToString();
    }



    private static string LayoutResolved( int Pointer )
    {
      return ( Pointer != 0 ) ? " -> " + LayoutAddr( Pointer ) : " -> (absent)";
    }



    /// <summary>
    /// Human-readable layout/offset dump of the anim-defs binary produced by
    /// <see cref="ExportAsGameBinarySprites"/>, mirroring the map export's .def
    /// view: the header constants, then a byte-by-byte decode of the header, the
    /// file-relative offset tables, and each animation definition. All addresses
    /// are FILE-RELATIVE offsets (byte 0 = start of the anim-defs data). The
    /// separate sprite-data file is summarised by block offset.
    /// </summary>
    public string GenerateSpriteGameBinaryLayoutText( GR.Memory.ByteBuffer AnimDefs, GR.Memory.ByteBuffer SpriteData )
    {
      var sb = new StringBuilder();

      // Header constants first, so this view is a complete reference for the .bin
      // (same as the map's .def dump leading with GenerateGameBinaryHeaderAsm).
      sb.AppendLine( GenerateSpriteGameBinaryHeaderAsm() );

      if ( ( AnimDefs == null ) || ( AnimDefs.Length < SPR_HDR_SIZE ) )
      {
        sb.AppendLine( "--- ANIM-DEFS BINARY ---" );
        sb.AppendLine( "(empty)" );
        return sb.ToString();
      }

      int animationCount = AnimDefs.ByteAt( 0x00 );
      int spriteCount    = AnimDefs.ByteAt( 0x01 );
      int stride         = AnimDefs.ByteAt( 0x02 );
      int frameSize      = AnimDefs.ByteAt( 0x03 );
      int offLo          = AnimDefs.UInt16At( SPR_HDR_OFFSET_ANIM_DEF_LO );
      int offHi          = AnimDefs.UInt16At( SPR_HDR_OFFSET_ANIM_DEF_HI );
      int offId          = AnimDefs.UInt16At( SPR_HDR_OFFSET_ANIM_ID );

      sb.AppendLine( "--- ANIM-DEFS HEADER (" + SPR_HDR_SIZE + " bytes) ---" );
      sb.AppendLine( LayoutAddr( 0x00 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( 0x00 ) ).PadRight( 24 ) + "animation_count = " + animationCount );
      sb.AppendLine( LayoutAddr( 0x01 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( 0x01 ) ).PadRight( 24 ) + "sprite_count = " + spriteCount );
      sb.AppendLine( LayoutAddr( 0x02 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( 0x02 ) ).PadRight( 24 ) + "sprite_data_stride = " + stride );
      sb.AppendLine( LayoutAddr( 0x03 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( 0x03 ) ).PadRight( 24 ) + "anim_frame_size = " + frameSize );
      sb.AppendLine( LayoutAddr( SPR_HDR_OFFSET_ANIM_DEF_LO ) + ": " + LayoutHexBytes( AnimDefs, SPR_HDR_OFFSET_ANIM_DEF_LO, 2 ).PadRight( 24 ) + "offset_anim_def_lo" + LayoutResolved( offLo ) );
      sb.AppendLine( LayoutAddr( SPR_HDR_OFFSET_ANIM_DEF_HI ) + ": " + LayoutHexBytes( AnimDefs, SPR_HDR_OFFSET_ANIM_DEF_HI, 2 ).PadRight( 24 ) + "offset_anim_def_hi" + LayoutResolved( offHi ) );
      sb.AppendLine( LayoutAddr( SPR_HDR_OFFSET_ANIM_ID ) + ": " + LayoutHexBytes( AnimDefs, SPR_HDR_OFFSET_ANIM_ID, 2 ).PadRight( 24 ) + "offset_anim_id" + LayoutResolved( offId ) );
      sb.AppendLine();

      if ( ( animationCount > 0 ) && ( offLo > 0 ) && ( offHi > 0 ) && ( offId > 0 ) )
      {
        sb.AppendLine( "--- OFFSET TABLES (" + animationCount + ( animationCount == 1 ? " entry" : " entries" ) + ") ---" );
        for ( int a = 0; a < animationCount; ++a )
        {
          sb.AppendLine( LayoutAddr( offLo + a ) + ": " + LayoutHexByte( AnimDefs.ByteAt( offLo + a ) ).PadRight( 24 ) + "anim_def_lo[" + a + "]" );
        }
        for ( int a = 0; a < animationCount; ++a )
        {
          sb.AppendLine( LayoutAddr( offHi + a ) + ": " + LayoutHexByte( AnimDefs.ByteAt( offHi + a ) ).PadRight( 24 ) + "anim_def_hi[" + a + "]" );
        }
        for ( int a = 0; a < animationCount; ++a )
        {
          sb.AppendLine( LayoutAddr( offId + a ) + ": " + LayoutHexByte( AnimDefs.ByteAt( offId + a ) ).PadRight( 24 ) + "anim_id[" + a + "] = " + AnimDefs.ByteAt( offId + a ) );
        }
        sb.AppendLine();

        for ( int a = 0; a < animationCount; ++a )
        {
          int defOff = AnimDefs.ByteAt( offLo + a ) | ( AnimDefs.ByteAt( offHi + a ) << 8 );
          if ( ( defOff < SPR_HDR_SIZE ) || ( defOff + 8 > AnimDefs.Length ) )
          {
            sb.AppendLine( "--- ANIMATION " + a + " @ " + LayoutAddr( defOff ) + " (out of range) ---" );
            sb.AppendLine();
            continue;
          }
          int id         = AnimDefs.ByteAt( defOff + 0 );
          int frameCount = AnimDefs.ByteAt( defOff + 5 );
          sb.AppendLine( "--- ANIMATION " + a + " (id $" + id.ToString( "X2" ) + ") @ " + LayoutAddr( defOff ) + " ---" );
          sb.AppendLine( LayoutAddr( defOff + 0 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 0 ) ).PadRight( 24 ) + "id = " + id );
          sb.AppendLine( LayoutAddr( defOff + 1 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 1 ) ).PadRight( 24 ) + "delay = " + AnimDefs.ByteAt( defOff + 1 ) + " (1/50s)" );
          sb.AppendLine( LayoutAddr( defOff + 2 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 2 ) ).PadRight( 24 ) + "expand_x = " + AnimDefs.ByteAt( defOff + 2 ) );
          sb.AppendLine( LayoutAddr( defOff + 3 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 3 ) ).PadRight( 24 ) + "expand_y = " + AnimDefs.ByteAt( defOff + 3 ) );
          sb.AppendLine( LayoutAddr( defOff + 4 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 4 ) ).PadRight( 24 ) + "multicolor = " + AnimDefs.ByteAt( defOff + 4 ) );
          sb.AppendLine( LayoutAddr( defOff + 5 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 5 ) ).PadRight( 24 ) + "frame_count = " + frameCount );
          sb.AppendLine( LayoutAddr( defOff + 6 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 6 ) ).PadRight( 24 ) + "loop = " + AnimDefs.ByteAt( defOff + 6 ) );
          sb.AppendLine( LayoutAddr( defOff + 7 ) + ": " + LayoutHexByte( AnimDefs.ByteAt( defOff + 7 ) ).PadRight( 24 ) + "start_random = " + AnimDefs.ByteAt( defOff + 7 ) );
          int framesStart = defOff + 8;
          for ( int f = 0; f < frameCount; ++f )
          {
            int fo = framesStart + f * SPRITE_ANIM_FRAME_SIZE;
            if ( fo + SPRITE_ANIM_FRAME_SIZE > AnimDefs.Length )
            {
              break;
            }
            sb.AppendLine( "  " + LayoutAddr( fo ) + ": " + LayoutHexBytes( AnimDefs, fo, SPRITE_ANIM_FRAME_SIZE ).PadRight( 22 )
              + "frame " + f + " -> sprite_ptr " + LayoutHexByte( AnimDefs.ByteAt( fo ) ) + ", color " + LayoutHexByte( AnimDefs.ByteAt( fo + 1 ) ) );
          }
          sb.AppendLine();
        }
      }

      // Sprite-data file summary (a separate file: raw blocks, no header).
      int dataLen = ( SpriteData != null ) ? (int)SpriteData.Length : 0;
      int blocks  = dataLen / SPRITE_BLOCK_SIZE;
      sb.AppendLine( "--- SPRITE-DATA FILE (" + blocks + ( blocks == 1 ? " block x " : " blocks x " ) + SPRITE_BLOCK_SIZE + " bytes = " + dataLen + " bytes) ---" );
      for ( int b = 0; b < blocks; ++b )
      {
        sb.AppendLine( "block " + b + " @ " + LayoutAddr( b * SPRITE_BLOCK_SIZE ) + "  (" + SPRITE_BLOCK_SIZE + " bytes raw sprite bitmap)" );
      }

      return sb.ToString();
    }



    private static string MakeAsmIdentifier( string Name )
    {
      if ( string.IsNullOrEmpty( Name ) )
      {
        return "";
      }
      var sb = new StringBuilder();
      foreach ( char c in Name )
      {
        if ( ( ( c >= 'A' ) && ( c <= 'Z' ) )
        ||   ( ( c >= 'a' ) && ( c <= 'z' ) )
        ||   ( ( c >= '0' ) && ( c <= '9' ) )
        ||   ( c == '_' ) )
        {
          sb.Append( c );
        }
        else
        {
          sb.Append( '_' );
        }
      }
      string result = sb.ToString();
      if ( ( result.Length > 0 ) && ( result[0] >= '0' ) && ( result[0] <= '9' ) )
      {
        result = "_" + result;
      }
      return result;
    }



  }
}
