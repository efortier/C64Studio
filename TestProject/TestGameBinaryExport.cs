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
    const int HDR_MAGIC             = 0x00;
    const int HDR_MARKER_STRIDE     = 0x02;
    const int HDR_TILE_COUNT        = 0x03;
    const int HDR_MAP_COUNT         = 0x04;
    const int HDR_TILES_WIDTH       = 0x05;
    const int HDR_TILES_HEIGHT      = 0x07;
    const int HDR_TILES_FLAGS       = 0x09;
    const int HDR_TILE_CHAR_OFF_LO  = 0x0B;
    const int HDR_TILE_CHAR_OFF_HI  = 0x0D;
    const int HDR_TILE_COLOR_OFF_LO = 0x0F;
    const int HDR_TILE_COLOR_OFF_HI = 0x11;
    const int HDR_MAP_WIDTH         = 0x13;
    const int HDR_MAP_HEIGHT        = 0x15;
    const int HDR_MAP_BG_COLOR      = 0x17;
    const int HDR_MAP_MC1_COLOR     = 0x19;
    const int HDR_MAP_MC2_COLOR     = 0x1B;
    const int HDR_MAP_MARKER_COUNT  = 0x1D;
    const int HDR_MAP_CHAR_GRID_LO  = 0x1F;
    const int HDR_MAP_CHAR_GRID_HI  = 0x21;
    const int HDR_MAP_COLOR_GRID_LO = 0x23;
    const int HDR_MAP_COLOR_GRID_HI = 0x25;
    const int HDR_MAP_PASSABLE_LO   = 0x27;
    const int HDR_MAP_PASSABLE_HI   = 0x29;
    const int HDR_MAP_MARKERS_LO    = 0x2B;
    const int HDR_MAP_MARKERS_HI    = 0x2D;
    const int HEADER_SIZE           = 0x2F; // 47 bytes

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
    // 1. Header
    // ================================================================

    [TestMethod]
    public void TestHeaderMagicBytes()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)0x44, buf.ByteAt( 0 ) );
      Assert.AreEqual( (byte)0x48, buf.ByteAt( 1 ) );
    }

    [TestMethod]
    public void TestHeaderMarkerStride()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)4, buf.ByteAt( HDR_MARKER_STRIDE ) );
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
      // All header offsets should point >= HEADER_SIZE
      for ( int off = HDR_TILES_WIDTH; off < HEADER_SIZE; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        Assert.IsTrue( val == 0 || val >= HEADER_SIZE,
          $"Header offset at +${off:X2} = ${val:X4} should be >= HEADER_SIZE or 0" );
      }
    }

    [TestMethod]
    public void TestAllHeaderOffsetsWithinFile()
    {
      var proj = CreateTestProject( 3, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      for ( int off = HDR_TILES_WIDTH; off < HEADER_SIZE; off += 2 )
      {
        ushort val = buf.UInt16At( off );
        if ( val != 0 )
          Assert.IsTrue( val < buf.Length, $"Offset at +${off:X2} = ${val:X4} exceeds file size {buf.Length}" );
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
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "START", ExportSymbol = "START", TagID = 10 } );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 1, Name = "EXIT", ExportSymbol = "EXIT", TagID = 20 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 3, Type = 0, Value = 0x42 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 1, Value = 0x99 } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      Assert.AreEqual( (byte)2, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      // Marker 0: tag=10, x=2, y=3, value=$42
      Assert.AreEqual( (byte)10, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 1 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( markersPos + 2 ) );
      Assert.AreEqual( (byte)0x42, buf.ByteAt( markersPos + 3 ) );
      // Marker 1: tag=20, x=1, y=1, value=$99
      Assert.AreEqual( (byte)20, buf.ByteAt( markersPos + 4 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 5 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 6 ) );
      Assert.AreEqual( (byte)0x99, buf.ByteAt( markersPos + 7 ) );
    }

    [TestMethod]
    public void TestMarkerCoordsConvertedByTileSpacing()
    {
      var proj = CreateTestProject( 2, 4, 4, tileSpacingX: 2, tileSpacingY: 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TEST", ExportSymbol = "TEST", TagID = 5 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 3, Y = 2, Type = 0, Value = 0x7F } );

      var buf = proj.ExportAsGameBinary( true, false, false );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)5, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)6, buf.ByteAt( markersPos + 1 ) );  // 3*2
      Assert.AreEqual( (byte)6, buf.ByteAt( markersPos + 2 ) );  // 2*3
      Assert.AreEqual( (byte)0x7F, buf.ByteAt( markersPos + 3 ) );
    }

    [TestMethod]
    public void TestMarkersOmittedWhenDisabled()
    {
      var proj = CreateTestProject( 2, 4, 4 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "TEST", ExportSymbol = "TEST", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0 } );
      var bufWith = proj.ExportAsGameBinary( true, false, false );
      var bufWithout = proj.ExportAsGameBinary( false, false, false );
      // Stride is 4 (tag, x, y, value), 1 marker
      Assert.AreEqual( (uint)4, bufWith.Length - bufWithout.Length );
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
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", ExportSymbol = "A", TagID = 100 } );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 1, Name = "B", ExportSymbol = "B", TagID = 200 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 0, Y = 0, Type = 0, Value = 0x11 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 1, Value = 0x22 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, Value = 0x33 } );

      var buf = proj.ExportAsGameBinary( true, false, false );
      Assert.AreEqual( (byte)3, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );
      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      // Stride is now 4 bytes (tag, x, y, value)
      Assert.AreEqual( (byte)100, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)0x11, buf.ByteAt( markersPos + 3 ) );
      Assert.AreEqual( (byte)200, buf.ByteAt( markersPos + 4 ) );
      Assert.AreEqual( (byte)0x22, buf.ByteAt( markersPos + 7 ) );
      Assert.AreEqual( (byte)100, buf.ByteAt( markersPos + 8 ) );
      Assert.AreEqual( (byte)0x33, buf.ByteAt( markersPos + 11 ) );
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
      proj.Settings.GameBinary.UseAbsoluteAddresses = true;
      proj.Settings.GameBinary.AbsoluteBaseAddressHex = "A000";

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
      Assert.AreEqual( true, proj2.Settings.GameBinary.UseAbsoluteAddresses );
      Assert.AreEqual( "A000", proj2.Settings.GameBinary.AbsoluteBaseAddressHex );
    }

    // ================================================================
    // 12. Full export — all sections, verify end-to-end
    // ================================================================

    [TestMethod]
    public void TestFullExportAllSections()
    {
      var proj = CreateTestProject( 3, 4, 3 );
      proj.Tiles[2].Passable = false;
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "START", ExportSymbol = "START", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 2, Type = 0, Value = 0xAB } );
      proj.Maps[0].Tiles[2, 1] = 2;

      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( (byte)0x44, buf.ByteAt( 0 ) );
      Assert.AreEqual( (byte)0x48, buf.ByteAt( 1 ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( HDR_TILE_COUNT ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( HDR_MAP_COUNT ) );
      Assert.AreEqual( (byte)4, buf.ByteAt( HdrOff( buf, HDR_MAP_WIDTH ) ) );
      Assert.AreEqual( (byte)3, buf.ByteAt( HdrOff( buf, HDR_MAP_HEIGHT ) ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( HdrOff( buf, HDR_MAP_MARKER_COUNT ) ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 0 ) );
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 1 ) );
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 2 ) );
      Assert.AreEqual( (byte)0xAB, buf.ByteAt( markersPos + 3 ) );

      // Markers should be the last thing in the file (stride=4)
      Assert.AreEqual( (uint)( markersPos + 4 ), buf.Length );
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
    // 14. Absolute base address — all offsets shifted by base
    // ================================================================

    [TestMethod]
    public void TestAbsoluteBaseAddressShiftsHeaderOffsets()
    {
      var proj = CreateTestProject( 2, 3, 2 );
      ushort baseAddr = 0xA000;

      var bufRel = proj.ExportAsGameBinary( true, true, true );
      var bufAbs = proj.ExportAsGameBinary( true, true, true, baseAddr );

      // Same total size
      Assert.AreEqual( bufRel.Length, bufAbs.Length );

      // Every header offset should be shifted by baseAddr
      for ( int i = 0; i < 21; ++i )
      {
        int hdrOff = HDR_TILES_WIDTH + i * 2;
        ushort relVal = bufRel.UInt16At( hdrOff );
        ushort absVal = bufAbs.UInt16At( hdrOff );
        if ( relVal == 0 )
          Assert.AreEqual( (ushort)0, absVal, "Disabled offset at +" + hdrOff.ToString( "X2" ) + " should stay 0" );
        else
          Assert.AreEqual( relVal + baseAddr, absVal, "Header offset at +" + hdrOff.ToString( "X2" ) + " should be shifted by base" );
      }
    }

    [TestMethod]
    public void TestAbsoluteBaseAddressShiftsLookupTables()
    {
      var proj = CreateTestProject( 2, 3, 2 );
      ushort baseAddr = 0x8000;

      var bufRel = proj.ExportAsGameBinary( true, true, true );
      var bufAbs = proj.ExportAsGameBinary( true, true, true, baseAddr );

      // Tile char offset lookup: read the absolute address from the lookup table
      int loTableRel = bufRel.UInt16At( HDR_TILE_CHAR_OFF_LO );
      int hiTableRel = bufRel.UInt16At( HDR_TILE_CHAR_OFF_HI );
      int loTableAbs = bufAbs.UInt16At( HDR_TILE_CHAR_OFF_LO ) - baseAddr;
      int hiTableAbs = bufAbs.UInt16At( HDR_TILE_CHAR_OFF_HI ) - baseAddr;

      for ( int t = 0; t < 2; ++t )
      {
        int relAddr = bufRel.ByteAt( loTableRel + t ) | ( bufRel.ByteAt( hiTableRel + t ) << 8 );
        int absAddr = bufAbs.ByteAt( loTableAbs + t ) | ( bufAbs.ByteAt( hiTableAbs + t ) << 8 );
        Assert.AreEqual( relAddr + baseAddr, absAddr, "Tile " + t + " char offset should be shifted by base" );
      }

      // Map char grid lookup
      int gridLoRel = bufRel.UInt16At( HDR_MAP_CHAR_GRID_LO );
      int gridHiRel = bufRel.UInt16At( HDR_MAP_CHAR_GRID_HI );
      int gridLoAbs = bufAbs.UInt16At( HDR_MAP_CHAR_GRID_LO ) - baseAddr;
      int gridHiAbs = bufAbs.UInt16At( HDR_MAP_CHAR_GRID_HI ) - baseAddr;

      int relGridAddr = bufRel.ByteAt( gridLoRel ) | ( bufRel.ByteAt( gridHiRel ) << 8 );
      int absGridAddr = bufAbs.ByteAt( gridLoAbs ) | ( bufAbs.ByteAt( gridHiAbs ) << 8 );
      Assert.AreEqual( relGridAddr + baseAddr, absGridAddr, "Map 0 char grid offset should be shifted by base" );

      // Actual data bytes should be identical
      int relDataPos = relGridAddr;
      int absDataPos = absGridAddr - baseAddr;
      Assert.AreEqual( bufRel.ByteAt( relDataPos ), bufAbs.ByteAt( absDataPos ), "Grid data should be identical" );
    }

    // ================================================================
    // 15. Marker Value field — serialization roundtrip + defaults
    // ================================================================

    [TestMethod]
    public void TestMarkerValueRoundtripThroughProjectFile()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "A", ExportSymbol = "A", TagID = 1 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 1, Y = 1, Type = 0, Value = 0xCD } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 2, Type = 0, Value = 0 } );

      var savedBuffer = proj.SaveToBuffer();
      var proj2 = new MapProject();
      proj2.ReadFromBuffer( savedBuffer );

      Assert.AreEqual( 2, proj2.Maps[0].Markers.Count );
      Assert.AreEqual( (byte)0xCD, proj2.Maps[0].Markers[0].Value );
      Assert.AreEqual( (byte)0, proj2.Maps[0].Markers[1].Value );
    }

    [TestMethod]
    public void TestMarkerValueExportedInBinary()
    {
      var proj = CreateTestProject( 2, 4, 3 );
      proj.MarkerTypes.Add( new MapProject.MarkerType { ID = 0, Name = "ITEM", ExportSymbol = "ITEM", TagID = 7 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker { X = 2, Y = 1, Type = 0, Value = 0xFE } );

      var buf = proj.ExportAsGameBinary( true, false, false );

      // Marker stride should be 4 now
      Assert.AreEqual( (byte)4, buf.ByteAt( HDR_MARKER_STRIDE ) );

      int markersPos = LookupAbsOffset( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.AreEqual( (byte)7, buf.ByteAt( markersPos + 0 ) );    // tag
      Assert.AreEqual( (byte)2, buf.ByteAt( markersPos + 1 ) );    // x
      Assert.AreEqual( (byte)1, buf.ByteAt( markersPos + 2 ) );    // y
      Assert.AreEqual( (byte)0xFE, buf.ByteAt( markersPos + 3 ) ); // value
    }

    [TestMethod]
    public void TestMarkerValueDefaultsToZero()
    {
      // New marker, Value not explicitly set, should be 0
      var marker = new MapProject.Marker { X = 0, Y = 0, Type = 0 };
      Assert.AreEqual( (byte)0, marker.Value );
    }
  }
}
