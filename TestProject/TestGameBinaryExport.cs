using System;
using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  [TestClass]
  public class TestGameBinaryExport
  {
    // Header offset constants (must match ExportAsGameBinary)
    const int HDR_MARKER_STRIDE     = 0x00;
    const int HDR_TILE_COUNT        = 0x01;
    const int HDR_MAP_COUNT         = 0x02;
    const int HDR_START_MAP_INDEX   = 0x03;
    const int HDR_TILES_WIDTH       = 0x04;
    const int HDR_TILES_HEIGHT      = 0x06;
    const int HDR_TILES_FLAGS       = 0x08;
    const int HDR_TILE_CHAR_OFF_LO  = 0x0A;
    const int HDR_TILE_CHAR_OFF_HI  = 0x0C;
    const int HDR_TILE_COLOR_OFF_LO = 0x0E;
    const int HDR_TILE_COLOR_OFF_HI = 0x10;
    const int HDR_MAP_WIDTH         = 0x12;
    const int HDR_MAP_HEIGHT        = 0x14;
    const int HDR_MAP_BG_COLOR      = 0x16;
    const int HDR_MAP_MC1_COLOR     = 0x18;
    const int HDR_MAP_MC2_COLOR     = 0x1A;
    const int HDR_MAP_MARKER_COUNT  = 0x1C;
    const int HDR_MAP_CHAR_GRID_LO  = 0x1E;
    const int HDR_MAP_CHAR_GRID_HI  = 0x20;
    const int HDR_MAP_COLOR_GRID_LO = 0x22;
    const int HDR_MAP_COLOR_GRID_HI = 0x24;
    const int HDR_MAP_PASSABLE_LO   = 0x26;
    const int HDR_MAP_PASSABLE_HI   = 0x28;
    const int HDR_MAP_MARKERS_LO    = 0x2A;
    const int HDR_MAP_MARKERS_HI    = 0x2C;
    const int HDR_ENTITY_STRIDE     = 0x2E;
    const int HDR_MAP_ENTITY_COUNT  = 0x2F;
    const int HDR_MAP_ENTITIES_LO   = 0x31;
    const int HDR_MAP_ENTITIES_HI   = 0x33;
    const int HEADER_SIZE           = 0x3C; // 60 bytes (v25: includes start_map_index at +$03)

    /// <summary>Read a 16-bit LE offset from the header and return it.</summary>
    int HdrOff( ByteBuffer buf, int hdrField ) => buf.UInt16At( hdrField );

    /// <summary>Read a lo/hi absolute offset pair from lookup tables for index i.</summary>
    int LookupAbsOffset( ByteBuffer buf, int hdrFieldLo, int hdrFieldHi, int index )
    {
      int loTablePos = HdrOff( buf, hdrFieldLo );
      int hiTablePos = HdrOff( buf, hdrFieldHi );
      return buf.ByteAt( loTablePos + index ) | ( buf.ByteAt( hiTablePos + index ) << 8 );
    }

    /// <summary>Helper: create a minimal MapProject with given tile and map configurations.</summary>
    private MapProject CreateTestProject( int tileCount, int mapWidth, int mapHeight,
      int tileSpacingX = 1, int tileSpacingY = 1 )
    {
      var proj = new MapProject();
      proj.BackgroundColor = 0;
      proj.MultiColor1 = 4;
      proj.MultiColor2 = 12;
      for ( int t = 0; t < tileCount; ++t )
      {
        var tile = new MapProject.Tile();
        tile.Index = t;
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0] = new MapProject.TileChar { Character = (byte)t, Color = (byte)( t % 16 ) };
        tile.Passable = true;
        tile.Name = "Tile" + t;
        proj.Tiles.Add( tile );
      }
      var map = new MapProject.Map();
      map.Name = "TestMap";
      map.TileSpacingX = tileSpacingX;
      map.TileSpacingY = tileSpacingY;
      map.Tiles.Resize( mapWidth, mapHeight );
      for ( int y = 0; y < mapHeight; ++y )
        for ( int x = 0; x < mapWidth; ++x )
          map.Tiles[x, y] = 0;
      proj.Maps.Add( map );
      return proj;
    }

    // ================================================================
    // 0. Editor-only tile layers — persistence round-trip
    // ================================================================

    [TestMethod]
    public void TestMapLayersRoundTripWithContent()
    {
      var proj = CreateTestProject( 4, 4, 3 );
      var map = proj.Maps[0];
      map.EnsureDefaultLayers();                  // Background + 2 empty
      Assert.AreEqual( 3, map.Layers.Count );

      // Content on the upper layers (tiles + a colour override + metadata).
      map.Layers[1].Name    = "Decor";
      map.Layers[1].Visible = false;
      map.Layers[1].Tiles[1, 1]              = 2;
      map.Layers[1].TileColorOverrides[1, 1] = 7;
      map.Layers[2].Tiles[2, 0]              = 3;

      var clone = MapProject.CloneMap( map );

      Assert.AreEqual( 3, clone.Layers.Count );
      Assert.AreEqual( "Decor", clone.Layers[1].Name );
      Assert.IsFalse( clone.Layers[1].Visible );
      Assert.AreEqual( 2, clone.Layers[1].Tiles[1, 1] );
      Assert.AreEqual( 7, clone.Layers[1].TileColorOverrides[1, 1] );
      Assert.AreEqual( 3, clone.Layers[2].Tiles[2, 0] );
      // Untouched upper cells stay transparent (-1).
      Assert.AreEqual( -1, clone.Layers[1].Tiles[0, 0] );
      Assert.AreEqual( -1, clone.Layers[2].Tiles[0, 0] );
      // Background unchanged.
      Assert.AreEqual( 0, clone.Layers[0].Tiles[0, 0] );
    }

    [TestMethod]
    public void TestMapLayersEmptyResynthesizesThree()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      var map = proj.Maps[0];
      // No upper-layer content -> MAP_LAYERS chunk is suppressed -> the clone
      // re-synthesizes the default 3 layers with transparent upper cells.
      var clone = MapProject.CloneMap( map );
      Assert.AreEqual( 3, clone.Layers.Count );
      Assert.AreEqual( -1, clone.Layers[1].Tiles[0, 0] );
      Assert.AreEqual( -1, clone.Layers[2].Tiles[0, 0] );
      Assert.AreEqual( 0, clone.Layers[0].Tiles[0, 0] );   // background preserved
    }

    [TestMethod]
    public void TestMapLayersVisibilityAndSelectionRoundTrip()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      var map = proj.Maps[0];
      map.EnsureDefaultLayers();

      // Non-default layer state with NO upper-layer tile content — exercises the
      // widened save gate (a hidden layer or non-zero selection alone must
      // persist, even though no upper layer has tiles).
      map.Layers[0].Visible = false;   // hide the Background
      map.Layers[2].Visible = false;   // hide an upper layer
      map.SelectedLayerIndex = 2;

      var clone = MapProject.CloneMap( map );

      Assert.AreEqual( 3, clone.Layers.Count );
      Assert.IsFalse( clone.Layers[0].Visible );           // Background visibility persisted
      Assert.IsTrue(  clone.Layers[1].Visible );
      Assert.IsFalse( clone.Layers[2].Visible );           // upper-layer visibility persisted
      Assert.AreEqual( 2, clone.SelectedLayerIndex );      // selected layer persisted
    }

    [TestMethod]
    public void TestMapLayersFlattenOnExport()
    {
      // tile 0 -> char 0, tile 1 -> char 1 (CreateTestProject sets char = index).
      var proj = CreateTestProject( 2, 4, 3 );
      var map = proj.Maps[0];
      map.EnsureDefaultLayers();
      // (0,0): only Background (tile 0). (1,0): upper Layer 1 places tile 1 over
      // Background's tile 0 -> the upper tile must win in the flattened export.
      map.Layers[1].Tiles[1, 0] = 1;

      var buf = proj.ExportAsGameBinary( false, false, false );
      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( (byte)0, buf.ByteAt( gridPos + 0 ) );   // Background tile 0
      Assert.AreEqual( (byte)1, buf.ByteAt( gridPos + 1 ) );   // upper layer tile 1 wins
    }

    // ================================================================
    // 1. Header
    // ================================================================

    [TestMethod]
    public void TestHeaderMarkerStride()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)11, buf.ByteAt( HDR_MARKER_STRIDE ) );
    }

    [TestMethod]
    public void TestHeaderTileAndMapCount()
    {
      var proj = CreateTestProject( 5, 4, 3 );
      var map2 = new MapProject.Map { Name = "Map2", TileSpacingX = 1, TileSpacingY = 1 };
      map2.Tiles.Resize( 3, 2 );
      proj.Maps.Add( map2 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)5, buf.ByteAt( HDR_TILE_COUNT ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( HDR_MAP_COUNT ) );
    }

    [TestMethod]
    public void TestHeaderSize()
    {
      var proj = CreateTestProject( 2, 2, 2 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      // Marker-section pointers (odd addresses)
      for ( int off = HDR_TILES_WIDTH; off <= HDR_MAP_MARKERS_HI; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        Assert.IsTrue( val == 0 || val >= HEADER_SIZE,
          $"Header offset at +${off:X2} = ${val:X4} should be >= HEADER_SIZE or 0" );
      }
      // Entity-section pointers (even addresses starting at $2E)
      for ( int off = HDR_MAP_ENTITY_COUNT; off <= HDR_MAP_ENTITIES_HI; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        Assert.IsTrue( val == 0 || val >= HEADER_SIZE,
          $"Entity header offset at +${off:X2} = ${val:X4} should be >= HEADER_SIZE or 0" );
      }
    }

    [TestMethod]
    public void TestAllHeaderOffsetsWithinFile()
    {
      var proj = CreateTestProject( 3, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      for ( int off = HDR_TILES_WIDTH; off <= HDR_MAP_MARKERS_HI; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        if ( val != 0 )
          Assert.IsTrue( val < buf.Length, $"Offset at +${off:X2} = ${val:X4} exceeds file size {buf.Length}" );
      }
      for ( int off = HDR_MAP_ENTITY_COUNT; off <= HDR_MAP_ENTITIES_HI; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        if ( val != 0 )
          Assert.IsTrue( val < buf.Length, $"Entity offset at +${off:X2} = ${val:X4} exceeds file size {buf.Length}" );
      }
    }

    // ================================================================
    // 2. Tile arrays — direct access via header
    // ================================================================

    [TestMethod]
    public void TestTileWidthsHeightsFlagsDirectAccess()
    {
      var proj = CreateTestProject( 3, 2, 2 );
      proj.Tiles[1].Chars.Resize( 2, 2 );
      proj.Tiles[1].Chars[0, 0] = new MapProject.TileChar { Character = 0x10, Color = 1 };
      proj.Tiles[1].Chars[1, 0] = new MapProject.TileChar { Character = 0x11, Color = 1 };
      proj.Tiles[1].Chars[0, 1] = new MapProject.TileChar { Character = 0x20, Color = 1 };
      proj.Tiles[1].Chars[1, 1] = new MapProject.TileChar { Character = 0x21, Color = 1 };
      proj.Tiles[2].Passable = false;

      var buf = proj.ExportAsGameBinary( false, false, false );

      int wOff = HdrOff( buf, HDR_TILES_WIDTH );
      int hOff = HdrOff( buf, HDR_TILES_HEIGHT );
      int fOff = HdrOff( buf, HDR_TILES_FLAGS );

      Assert.AreEqual( (byte)1, buf.ByteAt( wOff + 0 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( wOff + 1 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( wOff + 2 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( hOff + 0 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( hOff + 1 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( hOff + 2 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( fOff + 0 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( fOff + 1 ) );
      Assert.AreEqual( (byte)0, buf.ByteAt( fOff + 2 ) );
    }

    // ================================================================
    // 3. Tile char/color data via absolute offset tables
    // ================================================================

    [TestMethod]
    public void TestTileCharDataAbsoluteOffsets()
    {
      var proj = CreateTestProject( 2, 2, 2 );
      proj.Tiles[0].Chars[0, 0] = new MapProject.TileChar { Character = 0xAA, Color = 5 };
      proj.Tiles[1].Chars[0, 0] = new MapProject.TileChar { Character = 0xBB, Color = 7 };

      var buf = proj.ExportAsGameBinary( false, false, false );

      // Tile 0's char data: read absolute offset from lo/hi tables
      int tile0Abs = LookupAbsOffset( buf, HDR_TILE_CHAR_OFF_LO, HDR_TILE_CHAR_OFF_HI, 0 );
      Assert.AreEqual( (byte)0xAA, buf.ByteAt( tile0Abs ) );

      int tile1Abs = LookupAbsOffset( buf, HDR_TILE_CHAR_OFF_LO, HDR_TILE_CHAR_OFF_HI, 1 );
      Assert.AreEqual( (byte)0xBB, buf.ByteAt( tile1Abs ) );
    }

    [TestMethod]
    public void TestTileColorDataAbsoluteOffsets()
    {
      var proj = CreateTestProject( 2, 2, 2 );
      proj.Tiles[0].Chars[0, 0] = new MapProject.TileChar { Character = 0x10, Color = 5 };
      proj.Tiles[1].Chars[0, 0] = new MapProject.TileChar { Character = 0x20, Color = 9 };

      var buf = proj.ExportAsGameBinary( false, false, false );

      int tile0Abs = LookupAbsOffset( buf, HDR_TILE_COLOR_OFF_LO, HDR_TILE_COLOR_OFF_HI, 0 );
      Assert.AreEqual( (byte)5, buf.ByteAt( tile0Abs ) );

      int tile1Abs = LookupAbsOffset( buf, HDR_TILE_COLOR_OFF_LO, HDR_TILE_COLOR_OFF_HI, 1 );
      Assert.AreEqual( (byte)9, buf.ByteAt( tile1Abs ) );
    }

    // ================================================================
    // 4. Map metadata — direct access via header
    // ================================================================

    [TestMethod]
    public void TestMapMetadataDirectAccess()
    {
      var proj = CreateTestProject( 4, 5, 3 );
      proj.Maps[0].AlternativeBackgroundColor = 1;
      proj.Maps[0].AlternativeMultiColor1 = 5;

      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( (byte)5, buf.ByteAt( HdrOff( buf, HDR_MAP_WIDTH ) + 0 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( HdrOff( buf, HDR_MAP_HEIGHT ) + 0 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( HdrOff( buf, HDR_MAP_BG_COLOR ) + 0 ) );
      Assert.AreEqual( (byte)5, buf.ByteAt( HdrOff( buf, HDR_MAP_MC1_COLOR ) + 0 ) );
      Assert.AreEqual( (byte)12, buf.ByteAt( HdrOff( buf, HDR_MAP_MC2_COLOR ) + 0 ) ); // global fallback
    }

    [TestMethod]
    public void TestMultipleMapMetadata()
    {
      var proj = CreateTestProject( 2, 3, 3 );
      var map2 = new MapProject.Map { Name = "Map2", TileSpacingX = 1, TileSpacingY = 1 };
      map2.Tiles.Resize( 5, 2 );
      proj.Maps.Add( map2 );

      var buf = proj.ExportAsGameBinary( false, false, false );

      int wOff = HdrOff( buf, HDR_MAP_WIDTH );
      Assert.AreEqual( (byte)3, buf.ByteAt( wOff + 0 ), "Map 0 width" );
      Assert.AreEqual( (byte)5, buf.ByteAt( wOff + 1 ), "Map 1 width" );
    }

    // ================================================================
    // 5. Map char grid — absolute offset lookup
    // ================================================================

    [TestMethod]
    public void TestMapCharGridAbsoluteOffset()
    {
      var proj = CreateTestProject( 4, 3, 2 );
      var map = proj.Maps[0];
      map.Tiles[0, 0] = 0; map.Tiles[1, 0] = 1; map.Tiles[2, 0] = 2;
      map.Tiles[0, 1] = 3; map.Tiles[1, 1] = 0; map.Tiles[2, 1] = 1;

      var buf = proj.ExportAsGameBinary( false, false, false );

      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( (byte)0, buf.ByteAt( gridPos + 0 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( gridPos + 1 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( gridPos + 2 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( gridPos + 3 ) );
      Assert.AreEqual( (byte)0, buf.ByteAt( gridPos + 4 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( gridPos + 5 ) );
    }

    // ================================================================
    // 6. Map color grid
    // ================================================================

    [TestMethod]
    public void TestMapColorGridAbsoluteOffset()
    {
      var proj = CreateTestProject( 2, 2, 2 );
      proj.Tiles[0].Chars[0, 0] = new MapProject.TileChar { Character = 0, Color = 5 };
      proj.Tiles[1].Chars[0, 0] = new MapProject.TileChar { Character = 1, Color = 9 };
      proj.Maps[0].Tiles[0, 0] = 0; proj.Maps[0].Tiles[1, 0] = 1;
      proj.Maps[0].Tiles[0, 1] = 1; proj.Maps[0].Tiles[1, 1] = 0;

      var buf = proj.ExportAsGameBinary( false, true, false );
      int colorPos = LookupAbsOffset( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 0 );
      Assert.AreEqual( (byte)5, buf.ByteAt( colorPos + 0 ) );
      Assert.AreEqual( (byte)9, buf.ByteAt( colorPos + 1 ) );
      Assert.AreEqual( (byte)9, buf.ByteAt( colorPos + 2 ) );
      Assert.AreEqual( (byte)5, buf.ByteAt( colorPos + 3 ) );
    }

    [TestMethod]
    public void TestColorGridOmittedWhenDisabled()
    {
      var proj = CreateTestProject( 2, 2, 2 );
      var bufWith = proj.ExportAsGameBinary( false, true, false );
      var bufWithout = proj.ExportAsGameBinary( false, false, false );
      Assert.AreEqual( (uint)( 2 * 2 ), bufWith.Length - bufWithout.Length );
      // Color grid offset should be 0 when disabled
      int colorLoOff = HdrOff( bufWithout, HDR_MAP_COLOR_GRID_LO );
      Assert.AreEqual( (byte)0, bufWithout.ByteAt( colorLoOff ) );
    }

    // ================================================================
    // 7. Passable bits
    // ================================================================

    [TestMethod]
    public void TestPassableBitsAbsoluteOffset()
    {
      var proj = CreateTestProject( 4, 4, 1 );
      proj.Tiles[1].Passable = false;
      proj.Tiles[3].Passable = false;
      proj.Maps[0].Tiles[0, 0] = 0;
      proj.Maps[0].Tiles[1, 0] = 1;
      proj.Maps[0].Tiles[2, 0] = 2;
      proj.Maps[0].Tiles[3, 0] = 3;

      var buf = proj.ExportAsGameBinary( false, false, true );
      int passPos = LookupAbsOffset( buf, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI, 0 );
      Assert.AreEqual( (byte)0xA0, buf.ByteAt( passPos ) );
    }

    [TestMethod]
    public void TestPassableBitsOmittedWhenDisabled()
    {
      var proj = CreateTestProject( 2, 8, 1 );
      var bufWith = proj.ExportAsGameBinary( false, false, true );
      var bufWithout = proj.ExportAsGameBinary( false, false, false );
      Assert.IsTrue( bufWith.Length > bufWithout.Length );
    }

    // ================================================================
    // 8. Markers
    // ================================================================

    [TestMethod]
    public void TestMarkersAbsoluteOffset()
    {
      var proj = CreateTestProject( 2, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "START", TagID = 10 } );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 1, Name = "EXIT", TagID = 20 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 3, Type = 0, Value1 = 0x42, Value3 = 0x55, Value4 = 0x66, Enabled = true, Triggered = false } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 1, Value1 = 0x99, Enabled = false, Triggered = true } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      Assert.AreEqual( (byte)2, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      // Marker 0: tag=10, x=2, y=3, value1=$42, value2=0, flags=$01 (enabled),
      // group=0, linkTo=0, linkId=0, value3=$55, value4=$66 (stride 11)
      Assert.AreEqual( (byte)10, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 1 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( markersPos + 2 ) );
      Assert.AreEqual( (byte)0x42, buf.ByteAt( markersPos + 3 ) );
      Assert.AreEqual( (byte)0x00, buf.ByteAt( markersPos + 4 ) ); // value2 defaults to 0
      Assert.AreEqual( (byte)0x01, buf.ByteAt( markersPos + 5 ) ); // flags: enabled
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 6 ) );    // group id
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 7 ) );    // link to id
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 8 ) );    // link id
      Assert.AreEqual( (byte)0x55, buf.ByteAt( markersPos + 9 ) ); // value3
      Assert.AreEqual( (byte)0x66, buf.ByteAt( markersPos + 10 ) ); // value4
      // Marker 1: tag=20, x=1, y=1, value1=$99, value2=0, flags=$02 (triggered),
      // group=0, linkTo=0, linkId=0, value3=0, value4=0
      Assert.AreEqual( (byte)20, buf.ByteAt( markersPos + 11 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 12 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 13 ) );
      Assert.AreEqual( (byte)0x99, buf.ByteAt( markersPos + 14 ) );
      Assert.AreEqual( (byte)0x00, buf.ByteAt( markersPos + 15 ) );
      Assert.AreEqual( (byte)0x02, buf.ByteAt( markersPos + 16 ) ); // flags: triggered
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 17 ) );    // group id
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 18 ) );    // link to id
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 19 ) );    // link id
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 20 ) );    // value3 defaults to 0
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 21 ) );    // value4 defaults to 0
    }

    [TestMethod]
    public void TestMarkerAutoDisableGroupAfterTriggerFlag()
    {
      // AutoDisableGroupAfterTrigger is exported as bit 2 (0x04) of the FLAGS byte,
      // alongside Enabled (bit 0) and Triggered (bit 1). Adding the bit must NOT
      // change the marker stride. One marker keeps export order moot.
      var proj = CreateTestProject( 2, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TRAP", TagID = 7 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Enabled = true, AutoDisableGroupAfterTrigger = true } );

      var buf = proj.ExportAsGameBinary( true, false, false );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );

      Assert.AreEqual( (byte)7, buf.ByteAt( markersPos + 0 ) );      // tag
      // FLAGS: bit0 Enabled (0x01) | bit2 AutoDisableGroupAfterTrigger (0x04) = 0x05.
      Assert.AreEqual( (byte)0x05, buf.ByteAt( markersPos + 5 ) );
    }

    [TestMethod]
    public void TestMapStringShowNextPageMarkerTerminator()
    {
      // SHOW_NEXT_PAGE_MARKER ($FA) is a per-line TERMINATOR (alongside
      // END_OF_LINE $FD / PRESS_FIRE $FC), emitted in the terminator slot —
      // NOT a per-string tail byte. With ClearTextAreaAtEnd the stream ends
      // text, $FA, $FB, $FF.
      var ms = new MapProject.MapString();
      ms.Lines[0].Text = "HI";
      ms.Lines[0].Terminator = MapProject.MAP_STRING_SHOW_NEXT_PAGE_MARKER;
      ms.ClearTextAreaAtEnd = true;

      var stream = MapProject.BuildMapStringByteStream( ms, 1, 33, 48, 32 );
      Assert.AreEqual( (byte)0xFA, stream.ByteAt( (int)stream.Length - 3 ) );
      Assert.AreEqual( (byte)0xFB, stream.ByteAt( (int)stream.Length - 2 ) );
      Assert.AreEqual( (byte)0xFF, stream.ByteAt( (int)stream.Length - 1 ) );

      // A PRESS_FIRE line emits no $FA anywhere — the old tail-byte
      // behavior is gone.
      ms.Lines[0].Terminator = MapProject.MAP_STRING_PRESS_FIRE;
      stream = MapProject.BuildMapStringByteStream( ms, 1, 33, 48, 32 );
      for ( int i = 0; i < (int)stream.Length; ++i )
      {
        Assert.AreNotEqual( (byte)0xFA, stream.ByteAt( i ) );
      }
      Assert.AreEqual( (byte)0xFC, stream.ByteAt( (int)stream.Length - 3 ) );
      Assert.AreEqual( (byte)0xFB, stream.ByteAt( (int)stream.Length - 2 ) );
      Assert.AreEqual( (byte)0xFF, stream.ByteAt( (int)stream.Length - 1 ) );
    }

    [TestMethod]
    public void TestMarkerCoordsAreTileCoordinates()
    {
      // Previously coordinates were multiplied by TileSpacing on export; that was
      // removed because runtime code prefers raw tile coordinates.
      var proj = CreateTestProject( 2, 4, 4, tileSpacingX: 2, tileSpacingY: 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TEST", TagID = 5 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 3, Y = 2, Type = 0, Value1 = 0x7F } );

      var buf = proj.ExportAsGameBinary( true, false, false );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)5, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( markersPos + 1 ) ); // X unchanged
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 2 ) ); // Y unchanged
      Assert.AreEqual( (byte)0x7F, buf.ByteAt( markersPos + 3 ) ); // value1
      Assert.AreEqual( (byte)0x00, buf.ByteAt( markersPos + 4 ) ); // value2 default
      Assert.AreEqual( (byte)0x01, buf.ByteAt( markersPos + 5 ) ); // flags: enabled default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 6 ) );    // group id default
    }

    [TestMethod]
    public void TestMarkersOmittedWhenDisabled()
    {
      var proj = CreateTestProject( 2, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TEST", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0 } );
      var bufWith = proj.ExportAsGameBinary( true, false, false );
      var bufWithout = proj.ExportAsGameBinary( false, false, false );
      // Stride is 11 (tag, x, y, value1, value2, flags, group_id, link_to_id, link_id, value3, value4), 1 marker
      Assert.AreEqual( (uint)11, bufWith.Length - bufWithout.Length );
    }

    // ================================================================
    // 9. Edge cases
    // ================================================================

    [TestMethod]
    public void TestEmptyMapNoMarkers()
    {
      var proj = CreateTestProject( 1, 2, 2 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)0, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );
    }

    [TestMethod]
    public void TestSingleTileSingleCell()
    {
      var proj = CreateTestProject( 1, 1, 1 );
      proj.Tiles[0].Chars[0, 0] = new MapProject.TileChar { Character = 0x42, Color = 7 };

      var buf = proj.ExportAsGameBinary( false, true, true );
      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( (byte)0x42, buf.ByteAt( gridPos ) );

      int colorPos = LookupAbsOffset( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 0 );
      Assert.AreEqual( (byte)7, buf.ByteAt( colorPos ) );

      int passPos = LookupAbsOffset( buf, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI, 0 );
      Assert.AreEqual( (byte)0x80, buf.ByteAt( passPos ) );
    }

    [TestMethod]
    public void TestMapWith2x2Tile()
    {
      var proj = CreateTestProject( 2, 3, 3, tileSpacingX: 2, tileSpacingY: 2 );
      proj.Tiles[1].Chars.Resize( 2, 2 );
      proj.Tiles[1].Chars[0, 0] = new MapProject.TileChar { Character = 0xA0, Color = 1 };
      proj.Tiles[1].Chars[1, 0] = new MapProject.TileChar { Character = 0xA1, Color = 2 };
      proj.Tiles[1].Chars[0, 1] = new MapProject.TileChar { Character = 0xB0, Color = 3 };
      proj.Tiles[1].Chars[1, 1] = new MapProject.TileChar { Character = 0xB1, Color = 4 };
      proj.Maps[0].Tiles[0, 0] = 1;

      var buf = proj.ExportAsGameBinary( false, false, false );
      int width = buf.ByteAt( HdrOff( buf, HDR_MAP_WIDTH ) );
      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( (byte)0xA0, buf.ByteAt( gridPos + 0 ) );
      Assert.AreEqual( (byte)0xA1, buf.ByteAt( gridPos + 1 ) );
      Assert.AreEqual( (byte)0xB0, buf.ByteAt( gridPos + width ) );
      Assert.AreEqual( (byte)0xB1, buf.ByteAt( gridPos + width + 1 ) );
    }

    [TestMethod]
    public void TestMultipleMarkerTypes()
    {
      var proj = CreateTestProject( 1, 3, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", TagID = 100 } );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 1, Name = "B", TagID = 200 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, Value1 = 0x11 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 1, Value1 = 0x22 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, Value1 = 0x33 } );

      var buf = proj.ExportAsGameBinary( true, false, false );
      Assert.AreEqual( (byte)3, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      // Records are sorted by tag ascending, stride 11 bytes. Two markers share
      // tag 100 and one has tag 200, so the tag column must read 100, 100, 200.
      Assert.AreEqual( (byte)100, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)100, buf.ByteAt( markersPos + 11 ) );
      Assert.AreEqual( (byte)200, buf.ByteAt( markersPos + 22 ) );
      // The tag-200 record is unambiguously marker B (value1 = $22).
      Assert.AreEqual( (byte)0x22, buf.ByteAt( markersPos + 22 + 3 ) );
      // The two tag-100 records carry value1 $11 and $33 in some order.
      CollectionAssert.AreEquivalent(
        new byte[] { 0x11, 0x33 },
        new byte[] { buf.ByteAt( markersPos + 3 ), buf.ByteAt( markersPos + 11 + 3 ) } );
    }

    // ================================================================
    // 10. Round-trip
    // ================================================================

    [TestMethod]
    public void TestRoundTripMapGridMatchesSource()
    {
      var proj = CreateTestProject( 8, 6, 4 );
      var map = proj.Maps[0];
      for ( int y = 0; y < 4; ++y )
        for ( int x = 0; x < 6; ++x )
          map.Tiles[x, y] = ( x + y * 6 ) % 8;

      var buf = proj.ExportAsGameBinary( false, true, false );
      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );

      for ( int y = 0; y < 4; ++y )
        for ( int x = 0; x < 6; ++x )
        {
          int tileIdx = ( x + y * 6 ) % 8;
          byte expected = proj.Tiles[tileIdx].Chars[0, 0].Character;
          Assert.AreEqual( expected, buf.ByteAt( gridPos + x + y * 6 ), $"Mismatch at ({x},{y})" );
        }
    }

    // ================================================================
    // 11. Settings persistence
    // ================================================================

    [TestMethod]
    public void TestSettingsPersistence()
    {
      var proj = CreateTestProject( 1, 1, 1 );
      proj.Settings.GameBinary.ExportMarkers = false;
      proj.Settings.GameBinary.ExportColors = false;
      proj.Settings.GameBinary.ExportPassableBits = false;
      proj.Settings.GameBinary.PrefixLoadAddress = true;
      proj.Settings.GameBinary.PrefixLoadAddressHex = "C000";
      proj.Settings.GameBinary.SaveOnExport = true;
      proj.Settings.GameBinary.ExportDirectory = @"C:\TestDir";
      proj.Settings.GameBinary.ExportFilename = "test.bin";
      proj.Settings.GameBinary.MaxExportSizeEnabled = true;
      proj.Settings.GameBinary.MaxExportSizeText = "16384";
      proj.Settings.GameBinary.CompressMap = true;
      proj.Settings.GameBinary.Compressor = "ZX0";
      proj.Settings.GameBinary.CompressFilename = "map_intro.zx0";
      proj.Settings.GameBinary.CompressDirectory = @"Z:\DevC64\Dreadhold\Maps";
      proj.Settings.GameBinary.OverrideLoadAddress = true;
      proj.Settings.GameBinary.OverrideLoadAddressHex = "C000";

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( false, proj2.Settings.GameBinary.ExportMarkers );
      Assert.AreEqual( false, proj2.Settings.GameBinary.ExportColors );
      Assert.AreEqual( false, proj2.Settings.GameBinary.ExportPassableBits );
      Assert.AreEqual( true, proj2.Settings.GameBinary.PrefixLoadAddress );
      Assert.AreEqual( "C000", proj2.Settings.GameBinary.PrefixLoadAddressHex );
      Assert.AreEqual( true, proj2.Settings.GameBinary.SaveOnExport );
      Assert.AreEqual( @"C:\TestDir", proj2.Settings.GameBinary.ExportDirectory );
      Assert.AreEqual( "test.bin", proj2.Settings.GameBinary.ExportFilename );
      Assert.AreEqual( true, proj2.Settings.GameBinary.MaxExportSizeEnabled );
      Assert.AreEqual( "16384", proj2.Settings.GameBinary.MaxExportSizeText );
      Assert.AreEqual( true, proj2.Settings.GameBinary.CompressMap );
      Assert.AreEqual( "ZX0", proj2.Settings.GameBinary.Compressor );
      Assert.AreEqual( "map_intro.zx0", proj2.Settings.GameBinary.CompressFilename );
      Assert.AreEqual( @"Z:\DevC64\Dreadhold\Maps", proj2.Settings.GameBinary.CompressDirectory );
      Assert.AreEqual( true, proj2.Settings.GameBinary.OverrideLoadAddress );
      Assert.AreEqual( "C000", proj2.Settings.GameBinary.OverrideLoadAddressHex );
    }

    // ================================================================
    // 12. Full export — all sections, verify end-to-end
    // ================================================================

    [TestMethod]
    public void TestFullExportAllSections()
    {
      var proj = CreateTestProject( 3, 4, 3 );
      proj.Tiles[2].Passable = false;
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "START", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 2, Type = 0, Value1 = 0xAB } );
      proj.Maps[0].Tiles[2, 1] = 2;

      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( (byte)3, buf.ByteAt( HDR_TILE_COUNT ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( HDR_MAP_COUNT ) );
      Assert.AreEqual( (byte)4, buf.ByteAt( HdrOff( buf, HDR_MAP_WIDTH ) ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( HdrOff( buf, HDR_MAP_HEIGHT ) ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 0 ) );    // tag
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 1 ) );    // x
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 2 ) );    // y
      Assert.AreEqual( (byte)0xAB, buf.ByteAt( markersPos + 3 ) ); // value1
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 4 ) );    // value2 default
      Assert.AreEqual( (byte)0x01, buf.ByteAt( markersPos + 5 ) ); // flags: enabled default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 6 ) );    // group id default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 7 ) );    // link to id default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 8 ) );    // link id default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 9 ) );    // value3 default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 10 ) );   // value4 default

      // Markers should be the last thing in the file (stride = 11)
      Assert.AreEqual( (uint)( markersPos + 11 ), buf.Length );
    }

    // ================================================================
    // 13. Two-map test — verify independent offsets
    // ================================================================

    [TestMethod]
    public void TestTwoMapsIndependentOffsets()
    {
      var proj = CreateTestProject( 2, 3, 2 );
      var map2 = new MapProject.Map { Name = "Map2", TileSpacingX = 1, TileSpacingY = 1 };
      map2.Tiles.Resize( 2, 2 );
      for ( int y = 0; y < 2; ++y )
        for ( int x = 0; x < 2; ++x )
          map2.Tiles[x, y] = 1;
      proj.Maps.Add( map2 );

      var buf = proj.ExportAsGameBinary( false, true, false );

      int grid0 = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      int grid1 = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 1 );
      Assert.IsTrue( grid1 > grid0, "Map 1 grid should be after map 0" );

      int color0 = LookupAbsOffset( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 0 );
      int color1 = LookupAbsOffset( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 1 );
      Assert.IsTrue( color0 > grid0, "Map 0 color should be after map 0 grid" );
      Assert.IsTrue( color1 > grid1, "Map 1 color should be after map 1 grid" );

      // Map 1 grid should contain tile 1's char (value 1)
      Assert.AreEqual( (byte)1, buf.ByteAt( grid1 ) );
    }

    // ================================================================
    // 14. File-relative pointers — every stored pointer is a byte offset
    //     from the start of the binary, with no base address baked in.
    // ================================================================

    [TestMethod]
    public void TestFirstSectionStartsAtHeaderSize()
    {
      // The first data section (tiles_width) is written immediately after the
      // fixed 60-byte header, so its pointer must equal HEADER_SIZE exactly —
      // direct proof the pointer is a pure file offset with no base added.
      var proj = CreateTestProject( 3, 4, 3 );

      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( (ushort)HEADER_SIZE, buf.UInt16At( HDR_TILES_WIDTH ) );
    }

    [TestMethod]
    public void TestHeaderPointersAreContiguousFileOffsets()
    {
      // tiles_width / height / flags are three back-to-back byte arrays of
      // TILE_COUNT entries each. Under file-relative addressing each pointer is
      // the previous one plus the array length; a baked-in base would break this.
      var proj = CreateTestProject( 5, 4, 3 );

      var buf = proj.ExportAsGameBinary( true, true, true );

      int tileCount = buf.ByteAt( HDR_TILE_COUNT );
      int widthPtr  = buf.UInt16At( HDR_TILES_WIDTH );
      int heightPtr = buf.UInt16At( HDR_TILES_HEIGHT );
      int flagsPtr  = buf.UInt16At( HDR_TILES_FLAGS );

      Assert.AreEqual( HEADER_SIZE, widthPtr );
      Assert.AreEqual( widthPtr + tileCount, heightPtr );
      Assert.AreEqual( heightPtr + tileCount, flagsPtr );
    }

    [TestMethod]
    public void TestAllHeaderPointersAreFileOffsetsNoBase()
    {
      // Every non-zero header pointer must land inside the file (>= header size,
      // < length) when read as a raw file offset — i.e. no load base was added.
      // With an absolute base these would point past the end of the buffer.
      var proj = CreateTestProject( 3, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "S", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0 } );

      var buf = proj.ExportAsGameBinary( true, true, true );

      for ( int field = HDR_TILES_WIDTH; field <= HDR_MAP_MARKERS_HI; field += 2 )
      {
        int ptr = buf.UInt16At( field );
        if ( ptr == 0 ) continue;   // section-absent sentinel
        Assert.IsTrue( ptr >= HEADER_SIZE && ptr < buf.Length,
          $"Header pointer at +${field:X2} = ${ptr:X4} is not a valid file offset" );
      }
    }

    [TestMethod]
    public void TestLookupTableEntriesAreFileOffsets()
    {
      // The per-map char-grid lo/hi table entry must itself be a file offset:
      // dereferencing it with no base must land on the real grid data.
      var proj = CreateTestProject( 2, 3, 2 );
      proj.Maps[0].Tiles[1, 0] = 1;   // tile 1 -> char 1 (char = index)

      var buf = proj.ExportAsGameBinary( false, true, false );

      int gridPos = LookupAbsOffset( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.IsTrue( gridPos >= HEADER_SIZE && gridPos < buf.Length,
        "Map 0 char-grid pointer must be a file offset" );
      Assert.AreEqual( proj.Tiles[1].Chars[0, 0].Character, buf.ByteAt( gridPos + 1 ) );
    }

    [TestMethod]
    public void TestMapStringPointersAreFileOffsets()
    {
      // Map-string LO/HI table pointers (and the per-string stream addresses they
      // hold) are file-relative offsets too — verify they resolve in-file.
      var proj = CreateTestProject( 1, 2, 2 );
      var ms = new MapProject.MapString { Label = "MSG" };
      ms.Lines[0].Text = "HI";
      proj.MapStrings.Add( ms );

      var buf = proj.ExportAsGameBinary( false, false, false );

      Assert.AreEqual( (byte)1, buf.ByteAt( 0x35 ) );           // map_string_count
      int loTable = buf.UInt16At( 0x36 );                       // +$36 map_string_lo
      int hiTable = buf.UInt16At( 0x38 );                       // +$38 map_string_hi
      Assert.IsTrue( loTable >= HEADER_SIZE && loTable < buf.Length, "string LO table must be a file offset" );
      Assert.IsTrue( hiTable >= HEADER_SIZE && hiTable < buf.Length, "string HI table must be a file offset" );

      int streamAddr = buf.ByteAt( loTable ) | ( buf.ByteAt( hiTable ) << 8 );
      Assert.IsTrue( streamAddr >= HEADER_SIZE && streamAddr < buf.Length, "string stream must be a file offset" );
    }

    // ================================================================
    // 15. Marker Value field — serialization roundtrip + defaults
    // ================================================================

    [TestMethod]
    public void TestMarkerValueRoundtripThroughProjectFile()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Value1 = 0xCD } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, Value1 = 0 } );

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( 2, proj2.Maps[0].Markers.Count );
      Assert.AreEqual( (byte)0xCD, proj2.Maps[0].Markers[0].Value1 );
      Assert.AreEqual( (byte)0, proj2.Maps[0].Markers[1].Value1 );
    }

    [TestMethod]
    public void TestMarkerValueExportedInBinary()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "ITEM", TagID = 7 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 1, Type = 0, Value1 = 0xFE } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      // Marker stride is 9 now (tag, x, y, value1, value2, flags, group_id, link_to_id, link_id)
      Assert.AreEqual( (byte)11, buf.ByteAt( HDR_MARKER_STRIDE ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)7, buf.ByteAt( markersPos + 0 ) );    // tag
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 1 ) );    // x
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 2 ) );    // y
      Assert.AreEqual( (byte)0xFE, buf.ByteAt( markersPos + 3 ) ); // value1
      Assert.AreEqual( (byte)0x00, buf.ByteAt( markersPos + 4 ) ); // value2 default
      Assert.AreEqual( (byte)0x01, buf.ByteAt( markersPos + 5 ) ); // flags: enabled default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 6 ) );    // group id default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 7 ) );    // link to id default
      Assert.AreEqual( (byte)0, buf.ByteAt( markersPos + 8 ) );    // link id default
    }

    [TestMethod]
    public void TestMarkerValueDefaultsToZero()
    {
      // New marker, Value not explicitly set, should be 0
      var marker = new MapProject.Marker { X = 0, Y = 0, Type = 0 };
      Assert.AreEqual( (byte)0, marker.Value1 );
    }

    // ================================================================
    // 16. Marker Enabled/Triggered fields
    // ================================================================

    [TestMethod]
    public void TestMarkerEnabledTriggeredDefaults()
    {
      // New marker defaults: Enabled = true, Triggered = false
      var marker = new MapProject.Marker();
      Assert.AreEqual( true, marker.Enabled );
      Assert.AreEqual( false, marker.Triggered );
    }

    [TestMethod]
    public void TestMarkerEnabledTriggeredRoundtripThroughProjectFile()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, Enabled = true,  Triggered = false } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Enabled = false, Triggered = true } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, Enabled = false, Triggered = false } );

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( 3, proj2.Maps[0].Markers.Count );
      Assert.AreEqual( true,  proj2.Maps[0].Markers[0].Enabled );
      Assert.AreEqual( false, proj2.Maps[0].Markers[0].Triggered );
      Assert.AreEqual( false, proj2.Maps[0].Markers[1].Enabled );
      Assert.AreEqual( true,  proj2.Maps[0].Markers[1].Triggered );
      Assert.AreEqual( false, proj2.Maps[0].Markers[2].Enabled );
      Assert.AreEqual( false, proj2.Maps[0].Markers[2].Triggered );
    }

    [TestMethod]
    public void TestSpritePanelSettingsRoundtripThroughProjectFile()
    {
      // Sprites-panel settings: sprite project path + selected animation id
      // survive save/reload; old files (and fresh projects) default to "" / -1.
      var proj = CreateTestProject( 2, 4, 3 );
      proj.SpriteProjectFilename = @"sprites\sparkle.spriteproject";
      proj.SelectedSpriteAnimID  = 7;

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( @"sprites\sparkle.spriteproject", proj2.SpriteProjectFilename );
      Assert.AreEqual( 7, proj2.SelectedSpriteAnimID );

      var fresh = new MapProject();
      Assert.AreEqual( "", fresh.SpriteProjectFilename );
      Assert.AreEqual( -1, fresh.SelectedSpriteAnimID );
    }

    [TestMethod]
    public void TestMarkerEnabledTriggeredExportedInBinary()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "DOOR", TagID = 9 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Value1 = 0x55, Enabled = false, Triggered = true } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)9,    buf.ByteAt( markersPos + 0 ) ); // tag
      Assert.AreEqual( (byte)1,    buf.ByteAt( markersPos + 1 ) ); // x
      Assert.AreEqual( (byte)1,    buf.ByteAt( markersPos + 2 ) ); // y
      Assert.AreEqual( (byte)0x55, buf.ByteAt( markersPos + 3 ) ); // value1
      Assert.AreEqual( (byte)0x00, buf.ByteAt( markersPos + 4 ) ); // value2 default
      // flags byte: bit0 = Enabled (clear), bit1 = Triggered (set) => $02
      Assert.AreEqual( (byte)0x02, buf.ByteAt( markersPos + 5 ) ); // flags: triggered only
      Assert.AreEqual( (byte)0,    buf.ByteAt( markersPos + 6 ) ); // group id default
      Assert.AreEqual( (byte)0,    buf.ByteAt( markersPos + 7 ) ); // link to id default
      Assert.AreEqual( (byte)0,    buf.ByteAt( markersPos + 8 ) ); // link id default
    }

    // ================================================================
    // 17. Marker LinkToID / LinkID fields
    // ================================================================

    [TestMethod]
    public void TestMarkerLinkFieldsDefaultToZero()
    {
      // New marker, link fields not explicitly set, should both be 0.
      var marker = new MapProject.Marker { X = 0, Y = 0, Type = 0 };
      Assert.AreEqual( (byte)0, marker.LinkToID );
      Assert.AreEqual( (byte)0, marker.LinkID );
    }

    [TestMethod]
    public void TestMarkerLinkFieldsRoundtripThroughProjectFile()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, LinkToID = 5,   LinkID = 7 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, LinkToID = 200, LinkID = 0 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0 } );

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( 3, proj2.Maps[0].Markers.Count );
      Assert.AreEqual( (byte)5,   proj2.Maps[0].Markers[0].LinkToID );
      Assert.AreEqual( (byte)7,   proj2.Maps[0].Markers[0].LinkID );
      Assert.AreEqual( (byte)200, proj2.Maps[0].Markers[1].LinkToID );
      Assert.AreEqual( (byte)0,   proj2.Maps[0].Markers[1].LinkID );
      Assert.AreEqual( (byte)0,   proj2.Maps[0].Markers[2].LinkToID );
      Assert.AreEqual( (byte)0,   proj2.Maps[0].Markers[2].LinkID );
    }

    [TestMethod]
    public void TestMarkerLinkFieldsExportedInBinary()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TRIGGER", TagID = 3 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 2, Type = 0, LinkToID = 0x2A, LinkID = 0x5C } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      // Marker record is now 10 bytes; link fields stay at offsets 7/8 (value3
      // is appended after them at offset 9).
      Assert.AreEqual( (byte)11, buf.ByteAt( HDR_MARKER_STRIDE ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)0x2A, buf.ByteAt( markersPos + 7 ) ); // link to id
      Assert.AreEqual( (byte)0x5C, buf.ByteAt( markersPos + 8 ) ); // link id
    }

    [TestMethod]
    public void TestMarkerValue3And4RoundTripAndDefault()
    {
      var proj = CreateTestProject( 1, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "M", TagID = 5 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Value3 = 0x5A, Value4 = 0x6B } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0 } ); // Value3/Value4 unset -> 0

      // Chunk round-trip (project file save/load).
      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );
      Assert.AreEqual( (byte)0x5A, proj2.Maps[0].Markers[0].Value3 );
      Assert.AreEqual( (byte)0x6B, proj2.Maps[0].Markers[0].Value4 );
      Assert.AreEqual( (byte)0,    proj2.Maps[0].Markers[1].Value3 );
      Assert.AreEqual( (byte)0,    proj2.Maps[0].Markers[1].Value4 );

      // Game-binary export: value3/value4 are the last two bytes of each 11-byte record.
      var buf = proj.ExportAsGameBinary( true, false, false );
      Assert.AreEqual( (byte)11, buf.ByteAt( HDR_MARKER_STRIDE ) );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)0x5A, buf.ByteAt( markersPos + 9 ) );       // marker 0 value3
      Assert.AreEqual( (byte)0x6B, buf.ByteAt( markersPos + 10 ) );      // marker 0 value4
      Assert.AreEqual( (byte)0,    buf.ByteAt( markersPos + 11 + 9 ) );  // marker 1 value3 default 0
      Assert.AreEqual( (byte)0,    buf.ByteAt( markersPos + 11 + 10 ) ); // marker 1 value4 default 0
    }

    [TestMethod]
    public void TestMarkerSelfLinkDetection()
    {
      var proj = CreateTestProject( 1, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "T", TagID = 1 } );
      // Self-link: Link to ID == own ID (both non-zero) -> flagged.
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, LinkID = 5, LinkToID = 5 } );
      // Links to a DIFFERENT id -> not flagged.
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, LinkID = 6, LinkToID = 7 } );
      // No link / no id (both 0) -> not flagged.
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, LinkID = 0, LinkToID = 0 } );
      // Has an id but no link -> not flagged.
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 3, Y = 3, Type = 0, LinkID = 8, LinkToID = 0 } );

      var warnings = proj.GetMarkerSelfLinkWarnings();
      Assert.AreEqual( 1, warnings.Count );
      StringAssert.Contains( warnings[0], "own ID" );

      // A project with no self-links reports nothing.
      var clean = CreateTestProject( 1, 4, 4 );
      clean.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "T", TagID = 1 } );
      clean.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, LinkID = 1, LinkToID = 2 } );
      Assert.AreEqual( 0, clean.GetMarkerSelfLinkWarnings().Count );
    }
  }
}
