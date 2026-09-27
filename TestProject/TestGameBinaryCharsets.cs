using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Game-binary layout with multiple character sets: the 65-byte header,
  /// the per-charset tile sections and directory, the compacted per-map
  /// charset index table, per-map grid expansion through each map's own
  /// charset, and the new hard-error checks. The single-charset legacy
  /// semantics of $00-$3B are asserted separately in TestGameBinaryExport.
  /// </summary>
  [TestClass]
  public class TestGameBinaryCharsets
  {
    const int HDR_TILE_COUNT        = 0x01;
    const int HDR_MAP_COUNT         = 0x02;
    const int HDR_TILES_WIDTH       = 0x04;
    const int HDR_TILES_HEIGHT      = 0x06;
    const int HDR_TILES_FLAGS       = 0x08;
    const int HDR_TILE_CHAR_OFF_LO  = 0x0A;
    const int HDR_TILE_CHAR_OFF_HI  = 0x0C;
    const int HDR_TILE_COLOR_OFF_LO = 0x0E;
    const int HDR_TILE_COLOR_OFF_HI = 0x10;
    const int HDR_MAP_WIDTH         = 0x12;
    const int HDR_MAP_CHAR_GRID_LO  = 0x1E;
    const int HDR_MAP_CHAR_GRID_HI  = 0x20;
    const int HDR_MAP_COLOR_GRID_LO = 0x22;
    const int HDR_MAP_COLOR_GRID_HI = 0x24;
    const int HDR_MAP_PASSABLE_LO   = 0x26;
    const int HDR_MAP_PASSABLE_HI   = 0x28;
    const int HDR_MAP_MARKERS_LO    = 0x2A;
    const int HDR_MAP_MARKERS_HI    = 0x2C;
    const int HDR_MAP_ENTITY_COUNT  = 0x2F;
    const int HDR_MAP_ENTITIES_LO   = 0x31;
    const int HDR_MAP_ENTITIES_HI   = 0x33;
    const int HDR_MAP_CHARSET_INDEX = 0x3C;
    const int HDR_CHARSET_COUNT     = 0x3E;
    const int HDR_CHARSET_DIRECTORY = 0x3F;
    const int HEADER_SIZE           = 0x41;
    const int CHARSET_RECORD_SIZE   = 15;
    const int CSREC_TILE_COUNT         = 0x00;
    const int CSREC_TILES_WIDTH        = 0x01;
    const int CSREC_TILES_HEIGHT       = 0x03;
    const int CSREC_TILES_FLAGS        = 0x05;
    const int CSREC_TILE_CHAR_OFF_LO   = 0x07;
    const int CSREC_TILE_CHAR_OFF_HI   = 0x09;
    const int CSREC_TILE_COLOR_OFF_LO  = 0x0B;
    const int CSREC_TILE_COLOR_OFF_HI  = 0x0D;
    const int ENTITY_RECORD_TILE       = 0x04;



    // ---------------------------------------------------------------- helpers

    private static void FillTiles( MapProject.MapCharset Charset, int TileCount, byte CharBase )
    {
      // tile N => char CharBase+N, color (CharBase+N) % 16, 1x1, passable
      for ( int t = 0; t < TileCount; ++t )
      {
        var tile = new MapProject.Tile();
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0] = new MapProject.TileChar() { Character = (byte)( CharBase + t ), Color = (byte)( ( CharBase + t ) % 16 ) };
        tile.Passable = true;
        tile.Name = "Tile" + t;
        Charset.Tiles.Add( tile );
      }
      Charset.ReindexTiles();
    }



    /// <summary>Charset 0 with TileCount 1x1 tiles (tile N => char N) and one map filled with tile 0.</summary>
    private static MapProject CreateProject( int TileCount, int MapWidth, int MapHeight )
    {
      var proj = new MapProject();
      proj.BackgroundColor = 0;
      proj.MultiColor1 = 4;
      proj.MultiColor2 = 12;
      FillTiles( proj.Charsets[0], TileCount, 0 );
      proj.Charsets[0].ExportName = "charset0.bin";
      AddMap( proj, "TestMap", MapWidth, MapHeight, 0, 0 );
      return proj;
    }



    private static int AddCharset( MapProject proj, string ExportName, int TileCount, byte CharBase, bool Enabled )
    {
      int index = proj.AddCharset();
      Assert.IsTrue( index > 0 );
      var cs = proj.Charsets[index];
      FillTiles( cs, TileCount, CharBase );
      cs.ExportName = ExportName;
      cs.ExportEnabled = Enabled;
      cs.DisplayName = "CS" + index;
      return index;
    }



    private static MapProject.Map AddMap( MapProject proj, string Name, int Width, int Height, int FillTile, int CharsetIndex )
    {
      var map = new MapProject.Map();
      map.Name = Name;
      map.TileSpacingX = 1;
      map.TileSpacingY = 1;
      map.CharsetIndex = CharsetIndex;
      map.Tiles.Resize( Width, Height );
      for ( int y = 0; y < Height; ++y )
      {
        for ( int x = 0; x < Width; ++x )
        {
          map.Tiles[x, y] = FillTile;
        }
      }
      proj.Maps.Add( map );
      return map;
    }



    /// <summary>Byte read as int, so expected int literals compare by value.</summary>
    private static int B( ByteBuffer buf, int Pos )
    {
      return buf.ByteAt( Pos );
    }



    private static int DirRecordPos( ByteBuffer buf, int Charset )
    {
      return buf.UInt16At( HDR_CHARSET_DIRECTORY ) + Charset * CHARSET_RECORD_SIZE;
    }



    private static int DirPtr( ByteBuffer buf, int Charset, int Field )
    {
      return buf.UInt16At( DirRecordPos( buf, Charset ) + Field );
    }



    private static int LookupAbs( ByteBuffer buf, int LoTable, int HiTable, int Index )
    {
      return B( buf, LoTable + Index ) | ( B( buf, HiTable + Index ) << 8 );
    }



    private static int MapTableAbs( ByteBuffer buf, int HdrLo, int HdrHi, int Map )
    {
      return LookupAbs( buf, buf.UInt16At( HdrLo ), buf.UInt16At( HdrHi ), Map );
    }



    // ---------------------------------------------------------------- header

    [TestMethod]
    public void TestHeaderSizeIs65AndNewFieldsPresent()
    {
      var proj = CreateProject( 3, 4, 3 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.IsNotNull( buf );
      Assert.AreEqual( HEADER_SIZE, buf.UInt16At( HDR_TILES_WIDTH ), "the first section starts right after the 65-byte header" );
      int indexPtr = buf.UInt16At( HDR_MAP_CHARSET_INDEX );
      int dirPtr = buf.UInt16At( HDR_CHARSET_DIRECTORY );
      Assert.IsTrue( ( indexPtr >= HEADER_SIZE ) && ( indexPtr < buf.Length ), "map_charset_index pointer inside the file" );
      Assert.IsTrue( ( dirPtr >= HEADER_SIZE ) && ( dirPtr < buf.Length ), "directory pointer inside the file" );
      Assert.AreEqual( 1, B( buf, HDR_CHARSET_COUNT ) );
    }



    [TestMethod]
    public void TestLegacyTileFieldsMirrorDirectoryEntry0()
    {
      var proj = CreateProject( 4, 2, 2 );
      AddCharset( proj, "b.bin", 2, 0x40, true );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( B( buf, DirRecordPos( buf, 0 ) + CSREC_TILE_COUNT ), B( buf, HDR_TILE_COUNT ) );
      Assert.AreEqual( 4, B( buf, HDR_TILE_COUNT ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILES_WIDTH ), buf.UInt16At( HDR_TILES_WIDTH ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILES_HEIGHT ), buf.UInt16At( HDR_TILES_HEIGHT ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILES_FLAGS ), buf.UInt16At( HDR_TILES_FLAGS ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILE_CHAR_OFF_LO ), buf.UInt16At( HDR_TILE_CHAR_OFF_LO ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILE_CHAR_OFF_HI ), buf.UInt16At( HDR_TILE_CHAR_OFF_HI ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILE_COLOR_OFF_LO ), buf.UInt16At( HDR_TILE_COLOR_OFF_LO ) );
      Assert.AreEqual( DirPtr( buf, 0, CSREC_TILE_COLOR_OFF_HI ), buf.UInt16At( HDR_TILE_COLOR_OFF_HI ) );
    }



    [TestMethod]
    public void TestSingleCharsetKeepsLegacySemantics()
    {
      var proj = CreateProject( 3, 4, 3 );
      proj.Maps[0].Tiles[1, 1] = 2;
      proj.Charsets[0].Tiles[1].Passable = false;
      var buf = proj.ExportAsGameBinary( true, true, true );

      int widths = buf.UInt16At( HDR_TILES_WIDTH );
      int flags = buf.UInt16At( HDR_TILES_FLAGS );
      for ( int t = 0; t < 3; ++t )
      {
        Assert.AreEqual( 1, B( buf, widths + t ) );
        int charAddr = LookupAbs( buf, buf.UInt16At( HDR_TILE_CHAR_OFF_LO ), buf.UInt16At( HDR_TILE_CHAR_OFF_HI ), t );
        Assert.AreEqual( t, B( buf, charAddr ), "tile " + t + " char through the legacy tables" );
      }
      Assert.AreEqual( 0, B( buf, flags + 1 ) );
      Assert.AreEqual( 1, B( buf, flags + 2 ) );

      int grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0, B( buf, grid ) );
      Assert.AreEqual( 2, B( buf, grid + 1 + 1 * 4 ), "cell (1,1) expands to tile 2's char" );
    }



    // ---------------------------------------------------------------- directory

    [TestMethod]
    public void TestCharsetDirectoryLayoutWithThreeCharsets()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 5, 0x40, true );
      AddCharset( proj, "c.bin", 3, 0x80, true );
      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( 3, B( buf, HDR_CHARSET_COUNT ) );
      int[] counts = { 2, 5, 3 };
      byte[] bases = { 0, 0x40, 0x80 };
      int previousWidths = 0;
      for ( int c = 0; c < 3; ++c )
      {
        Assert.AreEqual( counts[c], B( buf, DirRecordPos( buf, c ) + CSREC_TILE_COUNT ), "tile count of charset " + c );
        int widths = DirPtr( buf, c, CSREC_TILES_WIDTH );
        int heights = DirPtr( buf, c, CSREC_TILES_HEIGHT );
        int flags = DirPtr( buf, c, CSREC_TILES_FLAGS );
        Assert.AreEqual( widths + counts[c], heights, "heights follow widths in charset " + c );
        Assert.AreEqual( heights + counts[c], flags, "flags follow heights in charset " + c );
        Assert.IsTrue( widths > previousWidths, "sections are written in compacted order" );
        previousWidths = widths;
        for ( int t = 0; t < counts[c]; ++t )
        {
          int charAddr = LookupAbs( buf, DirPtr( buf, c, CSREC_TILE_CHAR_OFF_LO ), DirPtr( buf, c, CSREC_TILE_CHAR_OFF_HI ), t );
          Assert.AreEqual( ( bases[c] + t ), B( buf, charAddr ), "charset " + c + " tile " + t + " char" );
          int colorAddr = LookupAbs( buf, DirPtr( buf, c, CSREC_TILE_COLOR_OFF_LO ), DirPtr( buf, c, CSREC_TILE_COLOR_OFF_HI ), t );
          Assert.AreEqual( ( ( bases[c] + t ) % 16 ), B( buf, colorAddr ), "charset " + c + " tile " + t + " color" );
        }
      }
    }



    [TestMethod]
    public void TestDirectoryPrecedesMapMetadata()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 1, 0x40, true );
      AddCharset( proj, "c.bin", 1, 0x80, true );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( buf.UInt16At( HDR_CHARSET_DIRECTORY ) + 3 * CHARSET_RECORD_SIZE, buf.UInt16At( HDR_MAP_WIDTH ) );
    }



    [TestMethod]
    public void TestCharsetCompactionSkipsDisabledMiddleCharset()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 5, 0x40, false );   // disabled
      AddCharset( proj, "c.bin", 3, 0x80, true );
      AddMap( proj, "OnLast", 2, 2, 0, 2 );
      var buf = proj.ExportAsGameBinary( true, true, true );

      Assert.AreEqual( 2, B( buf, HDR_CHARSET_COUNT ) );
      Assert.AreEqual( 3, B( buf, DirRecordPos( buf, 1 ) + CSREC_TILE_COUNT ), "record 1 is the LAST charset (the middle one compacted out)" );
      int index = buf.UInt16At( HDR_MAP_CHARSET_INDEX );
      Assert.AreEqual( 0, B( buf, index + 0 ) );
      Assert.AreEqual( 1, B( buf, index + 1 ), "map on charset 2 gets compacted index 1" );
    }



    [TestMethod]
    public void TestMapOnDisabledCharsetGetsIndexZeroAndWarning()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 3, 0x40, false );
      AddMap( proj, "Orphan", 2, 2, 1, 1 );
      var buf = proj.ExportAsGameBinary( true, true, true );

      int index = buf.UInt16At( HDR_MAP_CHARSET_INDEX );
      Assert.AreEqual( 0, B( buf, index + 1 ) );
      var warnings = proj.GetCharsetExportWarnings();
      Assert.AreEqual( 1, warnings.Count );
      StringAssert.Contains( warnings[0], "Orphan" );
      StringAssert.Contains( warnings[0], "not enabled" );
      int grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 1 );
      Assert.AreEqual( 0x41, B( buf, grid ), "the grid still expands through the map's own (disabled) charset" );
    }



    [TestMethod]
    public void TestMapCharsetIndexArrayFollowsEntityCount()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 2, 0x40, true );
      AddMap( proj, "Second", 3, 1, 0, 1 );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( buf.UInt16At( HDR_MAP_ENTITY_COUNT ) + 2, buf.UInt16At( HDR_MAP_CHARSET_INDEX ) );
      int index = buf.UInt16At( HDR_MAP_CHARSET_INDEX );
      Assert.AreEqual( 0, B( buf, index ) );
      Assert.AreEqual( 1, B( buf, index + 1 ) );
    }



    // ---------------------------------------------------------------- per-map expansion

    [TestMethod]
    public void TestGridExpansionUsesEachMapsOwnCharset()
    {
      var proj = CreateProject( 2, 2, 2 );               // charset 0: tile 1 = char 1, color 1
      int cs1 = AddCharset( proj, "b.bin", 2, 0x45, true );   // charset 1: tile 1 = char 0x46, color 6
      proj.Charsets[cs1].Tiles[1].Passable = false;
      proj.Maps[0].Tiles[0, 0] = 1;
      proj.Maps[0].Tiles[1, 0] = 1;
      proj.Maps[0].Tiles[0, 1] = 1;
      proj.Maps[0].Tiles[1, 1] = 1;
      AddMap( proj, "B", 2, 2, 1, cs1 );
      var buf = proj.ExportAsGameBinary( true, true, true );

      int gridA = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      int gridB = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 1 );
      for ( int i = 0; i < 4; ++i )
      {
        Assert.AreEqual( 0x01, B( buf, gridA + i ), "map A char " + i );
        Assert.AreEqual( 0x46, B( buf, gridB + i ), "map B char " + i );
      }
      int colorA = MapTableAbs( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 0 );
      int colorB = MapTableAbs( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 1 );
      Assert.AreEqual( 1, B( buf, colorA ) );
      Assert.AreEqual( 6, B( buf, colorB ) );
      int passA = MapTableAbs( buf, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI, 0 );
      int passB = MapTableAbs( buf, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI, 1 );
      Assert.AreEqual( 0xC0, B( buf, passA ), "row 0 of A: both chars passable" );
      Assert.AreEqual( 0x00, B( buf, passB ), "row 0 of B: both chars blocked by charset 1's tile 1" );
    }



    [TestMethod]
    public void TestOutOfRangeTileIndexBecomesEmptyTile()
    {
      var proj = CreateProject( 2, 2, 2 );
      proj.Settings.Assembly.EmptyTileIndex = 0;
      proj.Settings.Assembly.EmptyTileCompressionEnabled = false;
      for ( int y = 0; y < 2; ++y )
      {
        for ( int x = 0; x < 2; ++x )
        {
          proj.Maps[0].Tiles[x, y] = 7;   // beyond the 2-tile charset
        }
      }
      var buf = proj.ExportAsGameBinary( true, true, true );
      int grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0, B( buf, grid ), "out-of-range cell draws the empty tile (tile 0 = char 0)" );

      proj.Charsets[0].Tiles[0].Chars[0, 0].Character = 0x2A;
      buf = proj.ExportAsGameBinary( true, true, true );
      grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0x2A, B( buf, grid ), "the empty tile's own char is what lands in the grid" );

      proj.Settings.Assembly.EmptyTileCompressionEnabled = true;
      buf = proj.ExportAsGameBinary( true, true, true );
      grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0, B( buf, grid ), "with compression the empty tile is skipped (cell stays 0)" );

      proj.Settings.Assembly.EmptyTileIndex = 9;   // the empty tile itself out of range
      proj.Settings.Assembly.EmptyTileCompressionEnabled = false;
      buf = proj.ExportAsGameBinary( true, true, true );
      Assert.IsNotNull( buf );
      grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0, B( buf, grid ), "an out-of-range empty tile draws nothing, no exception" );

      // -1 (no tile) passes through: nothing drawn, char stays 0, passable.
      proj.Settings.Assembly.EmptyTileIndex = 0;
      proj.Charsets[0].Tiles[0].Chars[0, 0].Character = 0x2A;
      for ( int y = 0; y < 2; ++y )
      {
        for ( int x = 0; x < 2; ++x )
        {
          proj.Maps[0].Tiles[x, y] = -1;
        }
      }
      buf = proj.ExportAsGameBinary( true, true, true );
      grid = MapTableAbs( buf, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI, 0 );
      Assert.AreEqual( 0, B( buf, grid ) );
      int pass = MapTableAbs( buf, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI, 0 );
      Assert.AreEqual( 0xC0, B( buf, pass ) );
    }



    [TestMethod]
    public void TestEntityTileIndexExportedRaw()
    {
      var proj = CreateProject( 3, 2, 2 );
      proj.EntityTypes.Add( new MapProject.EntityType() { ID = 0, Name = "Big", TileIndex = 200, TagID = 7, PreviewCharsetIndex = 0 } );
      proj.Maps[0].Entities.Add( new MapProject.Entity() { X = 1, Y = 1, Type = 0 } );
      var buf = proj.ExportAsGameBinary( true, true, true );
      int entities = MapTableAbs( buf, HDR_MAP_ENTITIES_LO, HDR_MAP_ENTITIES_HI, 0 );
      Assert.AreEqual( 200, B( buf, entities + ENTITY_RECORD_TILE ), "the entity tile number is written raw" );
    }



    [TestMethod]
    public void TestZeroExportedCharsets()
    {
      var proj = CreateProject( 2, 2, 2 );
      proj.Charsets[0].ExportEnabled = false;
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.IsNotNull( buf );
      Assert.AreEqual( 0, B( buf, HDR_CHARSET_COUNT ) );
      Assert.AreEqual( 0, B( buf, HDR_TILE_COUNT ) );
      for ( int off = HDR_TILES_WIDTH; off <= HDR_TILE_COLOR_OFF_HI; off += 2 )
      {
        Assert.AreEqual( 0, buf.UInt16At( off ), "legacy tile pointer at $" + off.ToString( "X2" ) + " = section absent" );
      }
      Assert.IsTrue( buf.UInt16At( HDR_CHARSET_DIRECTORY ) >= HEADER_SIZE );
      Assert.AreEqual( 0, B( buf, buf.UInt16At( HDR_MAP_CHARSET_INDEX ) ) );
      var warnings = proj.GetCharsetExportWarnings();
      Assert.IsTrue( warnings.Any( w => w.Contains( "no character set is enabled" ) ) );
    }



    [TestMethod]
    public void TestMarkersStillPresentWithMultipleCharsets()
    {
      var proj = CreateProject( 2, 2, 2 );
      AddCharset( proj, "b.bin", 2, 0x40, true );
      proj.MarkerTypes.Add( new MapProject.MarkerType() { ID = 0, Name = "Door", TagID = 3 } );
      proj.Maps[0].Markers.Add( new MapProject.Marker() { X = 1, Y = 0, Type = 0, Value1 = 9 } );
      var buf = proj.ExportAsGameBinary( true, true, true );
      int markers = MapTableAbs( buf, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI, 0 );
      Assert.IsTrue( markers + 11 <= buf.Length );
      Assert.AreEqual( 3, B( buf, markers + 0 ), "tag" );
      Assert.AreEqual( 1, B( buf, markers + 1 ), "x" );
      Assert.AreEqual( 9, B( buf, markers + 3 ), "value1" );
    }



    // ---------------------------------------------------------------- hard errors

    [TestMethod]
    public void TestTooManyTilesIsHardError()
    {
      var proj = CreateProject( 256, 1, 1 );
      var errors = proj.GetGameBinaryExportErrors();
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "255" );
      List<string> reported;
      Assert.IsNull( proj.ExportAsGameBinary( true, true, true, out reported ) );
      Assert.AreEqual( 1, reported.Count );
      Assert.IsNull( proj.ExportAsGameBinary( true, true, true ), "the 3-argument overload aborts too" );

      proj.Charsets[0].ExportEnabled = false;   // a disabled charset's tile count does not matter
      Assert.AreEqual( 0, proj.GetGameBinaryExportErrors().Count );
    }



    [TestMethod]
    public void TestTooManyMapsIsHardError()
    {
      var proj = CreateProject( 1, 1, 1 );
      for ( int i = 1; i < 256; ++i )
      {
        AddMap( proj, "M" + i, 1, 1, 0, 0 );
      }
      var errors = proj.GetGameBinaryExportErrors();
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "maps" );
      proj.Maps[0].NotExported = true;
      Assert.AreEqual( 0, proj.GetGameBinaryExportErrors().Count, "only EXPORTED maps count" );
    }



    [TestMethod]
    public void TestBinaryOver64KIsHardError()
    {
      var big = CreateProject( 1, 200, 200 );
      List<string> errors;
      Assert.IsNull( big.ExportAsGameBinary( true, true, true, out errors ) );
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "65535" );

      var fits = CreateProject( 1, 150, 150 );
      var buf = fits.ExportAsGameBinary( true, true, true, out errors );
      Assert.IsNotNull( buf );
      Assert.AreEqual( 0, errors.Count );
      foreach ( int off in new int[] { HDR_MAP_CHAR_GRID_LO, HDR_MAP_COLOR_GRID_LO, HDR_MAP_PASSABLE_LO, HDR_MAP_CHARSET_INDEX, HDR_CHARSET_DIRECTORY } )
      {
        Assert.IsTrue( buf.UInt16At( off ) < buf.Length, "pointer at $" + off.ToString( "X2" ) + " inside the file" );
      }
      Assert.IsTrue( MapTableAbs( buf, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI, 0 ) < buf.Length );
    }



    // ---------------------------------------------------------------- generated asm + settings

    [TestMethod]
    public void TestGenerateGameBinaryHeaderAsmContainsCharsetConsts()
    {
      string asm = MapProject.GenerateGameBinaryHeaderAsm();
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_HEADER_OFFSET_MAP_CHARSET_INDEX\s+=\s+\$3C" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_HEADER_CHARSET_COUNT\s+=\s+\$3E" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_HEADER_OFFSET_CHARSET_DIRECTORY\s+=\s+\$3F" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_HEADER_SIZE\s+=\s+\$41" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_CHARSET_SIZE\s+=\s+\$0F" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_CHARSET_TILECOUNT\s+=\s+\$00" ) );
      Assert.IsTrue( Regex.IsMatch( asm, @"MAP_CHARSET_OFFSET_TILE_COLOR_OFFSET_HI\s+=\s+\$0D" ) );
      StringAssert.Contains( asm, "(65 bytes)" );
      Assert.IsFalse( Regex.IsMatch( asm, @"MAP_HEADER_SIZE\s+=\s+\$3C" ) );
    }



    [TestMethod]
    public void TestSettingsV29RoundTrip()
    {
      var fresh = new MapProject();
      Assert.IsTrue( fresh.Settings.GameBinary.ExportCharsetLabels );
      Assert.IsTrue( fresh.Settings.GameBinary.ExportMapLabels );
      Assert.AreEqual( "map_charsets.asm", fresh.Settings.GameBinary.CharsetLabelsFilename );
      Assert.AreEqual( "map_names.asm", fresh.Settings.GameBinary.MapLabelsFilename );

      var proj = CreateProject( 1, 1, 1 );
      proj.Settings.GameBinary.ExportCharsetLabels = false;
      proj.Settings.GameBinary.CharsetLabelsFilename = "cs.asm";
      proj.Settings.GameBinary.CharsetLabelsPrefix = "#import \"a.asm\"";
      proj.Settings.GameBinary.ExportMapLabels = false;
      proj.Settings.GameBinary.MapLabelsFilename = "mn.asm";
      proj.Settings.GameBinary.MapLabelsPrefix = ".namespace x";
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      Assert.IsFalse( reloaded.Settings.GameBinary.ExportCharsetLabels );
      Assert.AreEqual( "cs.asm", reloaded.Settings.GameBinary.CharsetLabelsFilename );
      Assert.AreEqual( "#import \"a.asm\"", reloaded.Settings.GameBinary.CharsetLabelsPrefix );
      Assert.IsFalse( reloaded.Settings.GameBinary.ExportMapLabels );
      Assert.AreEqual( "mn.asm", reloaded.Settings.GameBinary.MapLabelsFilename );
      Assert.AreEqual( ".namespace x", reloaded.Settings.GameBinary.MapLabelsPrefix );

      proj.Settings.GameBinary.CharsetLabelsFilename = "";
      proj.Settings.GameBinary.MapLabelsFilename = "";
      reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      Assert.AreEqual( "map_charsets.asm", reloaded.Settings.GameBinary.CharsetLabelsFilename, "blank file names fall back to the default" );
      Assert.AreEqual( "map_names.asm", reloaded.Settings.GameBinary.MapLabelsFilename );
    }



    [TestMethod]
    public void TestGameBinarySettingsRoundTripCoversEveryField()
    {
      // Reflection auto-guard: a GameBinarySettings field added without
      // persistence fails here by name.
      var proj = CreateProject( 1, 1, 1 );
      var settings = proj.Settings.GameBinary;
      var fields = typeof( MapProject.ExportSettings.GameBinarySettings ).GetFields( BindingFlags.Public | BindingFlags.Instance );
      int n = 0;
      foreach ( var field in fields )
      {
        ++n;
        if ( field.FieldType == typeof( string ) )
        {
          field.SetValue( settings, "val_" + n );
        }
        else if ( field.FieldType == typeof( bool ) )
        {
          field.SetValue( settings, !(bool)field.GetValue( settings ) );
        }
        else
        {
          Assert.Fail( "GameBinarySettings field '" + field.Name + "' has type " + field.FieldType.Name
                     + " which this guard does not handle. Add it to the writer, the reader, and this test." );
        }
      }
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      foreach ( var field in fields )
      {
        Assert.AreEqual( field.GetValue( settings ), field.GetValue( reloaded.Settings.GameBinary ),
                         "GameBinarySettings." + field.Name + " did not survive save/load" );
      }
    }
  }
}
