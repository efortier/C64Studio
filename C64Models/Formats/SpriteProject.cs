using GR.Memory;
using RetroDevStudio;
using RetroDevStudio.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
    /// in each of the 8 slots at this point in time, plus the delay until
    /// the next frame (in ms). Disabled slots have their bank index
    /// recorded but it is ignored at render time.
    /// </summary>
    public class OverlayFrame
    {
      public int[]      BankIndex = new int[8];
      public int        DelayMS   = 100;
    }



    public List<SpriteData>       Sprites  = new List<SpriteData>( 256 );
    public List<Overlay>          Overlays = new List<Overlay>();

    // Legacy in-memory types retained for the pre-Phase-2 UI while the
    // new Overlay model takes over the data layer. NOT persisted by
    // SaveToBuffer/ReadFromBuffer — only the Overlays list above is.
    // Remove once Phase 2 replaces the legacy layer panel UI.
    public class LayerSprite
    {
      public int        X = 0;
      public int        Y = 0;
      public int        Color = 0;
      public int        Index = 0;
      public bool       ExpandX = false;
      public bool       ExpandY = false;
    }

    public class Layer
    {
      public string             Name = "Default";
      public List<LayerSprite>  Sprites = new List<LayerSprite>();
      public int                BackgroundColor = 0;
      public int                DelayMS = 0;
    }

    public List<Layer>            SpriteLayers = new List<Layer>();

    public ColorSettings  Colors = new ColorSettings();

    public string         Name = "";
    public string         ExportFilename = "";

    public int            ExportSpriteCount = 0;
    public int            ExportStartIndex = 0;
    public int            TotalNumberOfSprites = 256;
    public bool           ShowGrid = false;

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
          chunkFrame.AppendI32( frame.DelayMS );
          chunkFrame.AppendI32( frame.BankIndex.Length );
          for ( int s = 0; s < frame.BankIndex.Length; ++s )
          {
            chunkFrame.AppendI32( frame.BankIndex[s] );
          }
          chunkOverlay.Append( chunkFrame.ToBuffer() );
        }

        chunkProject.Append( chunkOverlay.ToBuffer() );
      }
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

                      GR.IO.FileChunk   subChunkO = new GR.IO.FileChunk();

                      while ( subChunkO.ReadFromStream( subChunkReader ) )
                      {
                        var subChunkReaderO = subChunkO.MemoryReader();

                        if ( subChunkO.Type == FileChunkConstants.SPRITESET_OVERLAY_INFO )
                        {
                          overlay.Name = subChunkReaderO.ReadString();
                          // numSlots is informational; we always have 8 fixed slots
                          subChunkReaderO.ReadInt32();
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
                          frame.DelayMS = subChunkReaderO.ReadInt32();
                          int numRefs = subChunkReaderO.ReadInt32();
                          for ( int s = 0; s < numRefs; ++s )
                          {
                            int v = subChunkReaderO.ReadInt32();
                            if ( s < frame.BankIndex.Length ) frame.BankIndex[s] = v;
                          }
                          overlay.Frames.Add( frame );
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



  }
}
