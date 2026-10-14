using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// The per-map game-binary export: one file per exported map named by a
  /// %d pattern, each holding exactly one map in the usual 65-byte layout
  /// (map count 1, start byte 0, the map's own charset as the only tile
  /// section, only its own strings with their authored IDs), one charset
  /// file per map numbered the same way, the start map as MAP_START_INDEX,
  /// and the file-name pattern rules.
  /// </summary>
  [TestClass]
  public class TestGameBinaryPerMap
  {
    const int HDR_TILE_COUNT        = 0x01;
    const int HDR_MAP_COUNT         = 0x02;
    const int HDR_START_MAP         = 0x03;
    const int HDR_MAP_WIDTH         = 0x12;
    const int HDR_MAP_CHAR_GRID_LO  = 0x1E;
    const int HDR_MAP_CHAR_GRID_HI  = 0x20;
    const int HDR_MAP_ENTITIES_LO   = 0x31;
    const int HDR_MAP_ENTITIES_HI   = 0x33;
    const int HDR_MAP_STRING_COUNT  = 0x35;
    const int HDR_MAP_STRING_ID     = 0x3A;
    const int HDR_MAP_CHARSET_INDEX = 0x3C;
    const int HDR_CHARSET_COUNT     = 0x3E;



    // ---------------------------------------------------------------- helpers

    private static void AddTiles( MapProject.MapCharset Charset, int Count, byte CharBase )
    {
      for ( int i = 0; i < Count; ++i )
      {
        var tile = new MapProject.Tile() { Name = "T" + CharBase + "_" + i };
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0].Character = (byte)( CharBase + i );
        tile.Chars[0, 0].Color = 1;
        Charset.Tiles.Add( tile );
      }
      Charset.ReindexTiles();
    }



    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex, int Width, int Height, int Fill )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( Width, Height );
      for ( int y = 0; y < Height; ++y )
      {
        for ( int x = 0; x < Width; ++x )
        {
          map.Tiles[x, y] = Fill;
        }
      }
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Charset 0 (3 tiles, chars $10..) and charset 1 (2 tiles, chars $20..);
    /// maps Alpha (cs0, tile 1), Hidden (cs0, not exported), Bravo (cs1,
    /// tile 1), Charlie (cs0, tile 2).
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 2, 0x20 );
      project.Charsets[0].Charset.Characters[0x11].Tile.Data.SetU8At( 0, 0xAA );
      project.Charsets[second].Charset.Characters[0x21].Tile.Data.SetU8At( 0, 0xBB );

      AddMap( project, "Alpha", 0, 3, 2, 1 );
      AddMap( project, "Hidden", 0, 2, 2, 0 ).NotExported = true;
      AddMap( project, "Bravo", second, 2, 3, 1 );
      AddMap( project, "Charlie", 0, 1, 1, 2 );
      return project;
    }



    private static List<MapProject.GameBinaryMapFile> Export( MapProject Project, string MapPattern = "MAP_%d.bin", string CharsetPattern = "CHARSET_MAP_%d.bin" )
    {
      List<string> errors;
      var files = Project.ExportAsGameBinaryFiles( MapPattern, CharsetPattern, true, true, true, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      Assert.AreEqual( 0, errors.Count );
      return files;
    }



    /// <summary>Absolute position of map 0's entry in a per-map lo/hi pointer table pair.</summary>
    private static int FirstMapTable( ByteBuffer Binary, int HdrLo, int HdrHi )
    {
      int lo = Binary.UInt16At( HdrLo );
      int hi = Binary.UInt16At( HdrHi );
      return Binary.ByteAt( lo ) | ( Binary.ByteAt( hi ) << 8 );
    }



    private static List<int> StringIDs( ByteBuffer Binary )
    {
      var ids = new List<int>();
      int count = Binary.ByteAt( HDR_MAP_STRING_COUNT );
      int table = Binary.UInt16At( HDR_MAP_STRING_ID );
      for ( int i = 0; i < count; ++i )
      {
        ids.Add( Binary.ByteAt( table + i ) );
      }
      return ids;
    }



    // ------------------------------------------------------------- the files

    [TestMethod]
    public void TestOneFilePerExportedMapNumberedLikeTheMapIndices()
    {
      var files = Export( BuildProject() );

      Assert.AreEqual( 3, files.Count, "the map flagged 'not exported' gets no file" );
      CollectionAssert.AreEqual( new[] { "MAP_0.bin", "MAP_1.bin", "MAP_2.bin" }, files.Select( f => f.FileName ).ToArray() );
      CollectionAssert.AreEqual( new[] { "Alpha", "Bravo", "Charlie" }, files.Select( f => f.Map.Name ).ToArray() );
      CollectionAssert.AreEqual( new[] { 0, 1, 2 }, files.Select( f => f.ExportIndex ).ToArray(), "the same numbers as MAP_INDEX_*" );
    }



    [TestMethod]
    public void TestEachFileHoldsExactlyOneMap()
    {
      var files = Export( BuildProject() );
      foreach ( var file in files )
      {
        var bin = file.Data;
        Assert.AreEqual( 1, bin.ByteAt( HDR_MAP_COUNT ), file.FileName + ": map count" );
        Assert.AreEqual( 0, bin.ByteAt( HDR_START_MAP ), file.FileName + ": the only map is the start of the file" );
        Assert.AreEqual( 1, bin.ByteAt( HDR_CHARSET_COUNT ), file.FileName + ": its own charset only" );
        Assert.AreEqual( 0, bin.ByteAt( bin.UInt16At( HDR_MAP_CHARSET_INDEX ) ), file.FileName + ": map 0 uses directory entry 0" );
      }

      // Bravo (charset 1, 2 tiles, tile 1 = char $21, 2x3)
      var bravo = files[1].Data;
      Assert.AreEqual( 2, bravo.ByteAt( HDR_TILE_COUNT ) );
      Assert.AreEqual( 2, bravo.ByteAt( bravo.UInt16At( HDR_MAP_WIDTH ) ) );
      int grid = FirstMapTable( bravo, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI );
      Assert.AreEqual( 0x21, bravo.ByteAt( grid ), "cells come from the map's own charset" );

      // Charlie (charset 0, tile 2 = char $12)
      var charlie = files[2].Data;
      Assert.AreEqual( 3, charlie.ByteAt( HDR_TILE_COUNT ) );
      Assert.AreEqual( 0x12, charlie.ByteAt( FirstMapTable( charlie, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI ) ) );
    }



    [TestMethod]
    public void TestCharsetFilesFollowTheMapNumbers()
    {
      var project = BuildProject();
      var files = Export( project, "MAP_%d.bin", "CHARSET_MAP_%d.dat" );

      CollectionAssert.AreEqual( new[] { "CHARSET_MAP_0.dat", "CHARSET_MAP_1.dat", "CHARSET_MAP_2.dat" }, files.Select( f => f.CharsetFileName ).ToArray() );
      Assert.AreEqual( 0xAA, files[0].CharsetData.ByteAt( 0x11 * 8 ), "Alpha's file holds charset 0" );
      Assert.AreEqual( 0xBB, files[1].CharsetData.ByteAt( 0x21 * 8 ), "Bravo's file holds charset 1" );
      // Alpha and Charlie share charset 0: written once for each of them
      Assert.AreEqual( 0, files[0].CharsetData.Compare( files[2].CharsetData ) );
      Assert.AreEqual( project.Charsets[0].Charset.CharacterData().Length, files[0].CharsetData.Length );
    }



    [TestMethod]
    public void TestNoCharsetFilesWithoutAPattern()
    {
      var files = Export( BuildProject(), "MAP_%d.bin", "" );
      Assert.IsTrue( files.All( f => ( f.CharsetFileName == "" ) && ( f.CharsetData == null ) ) );
    }



    // ------------------------------------------------------------- strings

    private static MapProject StringProject()
    {
      var project = BuildProject();
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 0, Name = MapProject.MARKER_TYPE_SHOW_MAP_MESSAGE, TagID = 1 } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 1, Name = MapProject.MARKER_TYPE_BUMPABLE_MARKER, TagID = 2 } );
      project.MapStrings.Add( new MapProject.MapString() { Label = "WELCOME", StringID = 5 } );
      project.MapStrings.Add( new MapProject.MapString() { Label = "SIGN", StringID = 7 } );
      project.MapStrings.Add( new MapProject.MapString() { Label = "UNUSED", StringID = 9 } );
      // Alpha shows string 5, Bravo bumps string 7 (variant 1), Charlie shows nothing
      project.Maps[0].Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, Value1 = 5 } );
      project.Maps[2].Markers.Add( new MapProject.Marker() { Type = 1, X = 0, Y = 0, Value1 = MapProject.BUMPABLE_VARIANT_SHOW_MESSAGE, Value2 = 7 } );
      return project;
    }



    [TestMethod]
    public void TestEachFileCarriesOnlyItsOwnStringsWithTheirIDs()
    {
      var files = Export( StringProject() );

      CollectionAssert.AreEqual( new[] { 5 }, StringIDs( files[0].Data ), "Alpha: the string its marker shows, ID as authored" );
      CollectionAssert.AreEqual( new[] { 7 }, StringIDs( files[1].Data ), "Bravo: the bumpable's string" );
      Assert.AreEqual( 0, StringIDs( files[2].Data ).Count, "Charlie shows no string" );
    }



    [TestMethod]
    public void TestEveryFileGetsEveryStringWhenNoMessageMarkerTypeExists()
    {
      var project = StringProject();
      project.MarkerTypes.Clear();
      var files = Export( project );
      foreach ( var file in files )
      {
        CollectionAssert.AreEqual( new[] { 5, 7, 9 }, StringIDs( file.Data ), file.FileName );
      }
    }



    // ------------------------------------------------------------- start map

    [TestMethod]
    public void TestTheStartMapIsAConstantInTheMapLabels()
    {
      var project = BuildProject();
      project.StartMapIndex = 2;    // Bravo - exported index 1, Hidden is skipped
      var files = Export( project );

      Assert.IsTrue( files.All( f => f.Data.ByteAt( HDR_START_MAP ) == 0 ) );
      Assert.AreEqual( 1, project.StartMapExportIndex() );
      StringAssert.Contains( project.GenerateMapLabelsAsm(), ".const MAP_START_INDEX = 1  // Bravo" );
    }



    // ------------------------------------------------------------- entities

    [TestMethod]
    public void TestEntityRecordsUseTheTileOfEachMapsCharset()
    {
      var project = BuildProject();
      var skeleton = new MapProject.EntityType() { ID = 0, Name = "Skeleton", TagID = 3 };
      project.EntityTypes.Add( skeleton );
      project.Charsets[0].SetEntityTile( skeleton, 2 );
      project.Charsets[1].SetEntityTile( skeleton, 1 );
      project.Maps[0].Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1 } );
      project.Maps[2].Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 0 } );

      var files = Export( project );
      int alpha = FirstMapTable( files[0].Data, HDR_MAP_ENTITIES_LO, HDR_MAP_ENTITIES_HI );
      int bravo = FirstMapTable( files[1].Data, HDR_MAP_ENTITIES_LO, HDR_MAP_ENTITIES_HI );
      Assert.AreEqual( 3, files[0].Data.ByteAt( alpha + 1 ), "tag" );
      Assert.AreEqual( 2, files[0].Data.ByteAt( alpha + 4 ), "Alpha: Skeleton's tile in charset 0" );
      Assert.AreEqual( 1, files[1].Data.ByteAt( bravo + 4 ), "Bravo: Skeleton's tile in charset 1" );
    }



    // ------------------------------------------------------------- file name patterns

    private static string Format( string Pattern, int Number )
    {
      string name;
      string error;
      Assert.IsTrue( MapProject.TryFormatFileNamePattern( Pattern, Number, out name, out error ), error );
      return name;
    }



    private static string FormatError( string Pattern )
    {
      string name;
      string error;
      Assert.IsFalse( MapProject.TryFormatFileNamePattern( Pattern, 3, out name, out error ), "'" + Pattern + "' must be refused" );
      Assert.AreEqual( "", name );
      Assert.IsFalse( string.IsNullOrEmpty( error ) );
      return error;
    }



    [TestMethod]
    public void TestFileNamePatterns()
    {
      Assert.AreEqual( "MAP_7.bin", Format( "MAP_%d.bin", 7 ) );
      Assert.AreEqual( "GAMEMAP_12.DAT", Format( "GAMEMAP_%d.DAT", 12 ) );
      Assert.AreEqual( "MAP_07.bin", Format( "MAP_%02d.bin", 7 ) );
      Assert.AreEqual( "MAP_123.bin", Format( "MAP_%02d.bin", 123 ), "padding never truncates" );
      Assert.AreEqual( "MAP_  7.bin", Format( "MAP_%3d.bin", 7 ) );
      Assert.AreEqual( "100%_7.dat", Format( "100%%_%d.dat", 7 ) );
      Assert.AreEqual( "L7_7", Format( "L%d_%d", 7 ), "every placeholder gets the number" );
    }



    [TestMethod]
    public void TestBadFileNamePatternsAreRefused()
    {
      StringAssert.Contains( FormatError( "map_intro.bin" ), "no %d placeholder" );
      StringAssert.Contains( FormatError( "MAP_%x.bin" ), "%x" );
      StringAssert.Contains( FormatError( "MAP_%" ), "unknown placeholder" );
      StringAssert.Contains( FormatError( "MAP_%123d.bin" ), "unknown placeholder" );
      StringAssert.Contains( FormatError( "MAP_%d?.bin" ), "not a valid file name" );
      StringAssert.Contains( FormatError( "" ), "no file name" );
      StringAssert.Contains( FormatError( "   " ), "no file name" );
      Assert.IsNull( MapProject.ValidateFileNamePattern( "MAP_%d.bin" ) );
    }



    [TestMethod]
    public void TestAPatternWithoutPlaceholderStopsTheExport()
    {
      List<string> errors;
      Assert.IsNull( BuildProject().ExportAsGameBinaryFiles( "map_intro.bin", "CHARSET_%d.bin", true, true, true, out errors ) );
      Assert.IsTrue( errors.Any( e => e.StartsWith( "map file name:" ) ), string.Join( "\n", errors ) );

      Assert.IsNull( BuildProject().ExportAsGameBinaryFiles( "MAP_%d.bin", "charset.bin", true, true, true, out errors ) );
      Assert.IsTrue( errors.Any( e => e.StartsWith( "charset file name:" ) ), string.Join( "\n", errors ) );
    }



    [TestMethod]
    public void TestMapAndCharsetFilesMayNotShareNames()
    {
      List<string> errors;
      // Windows file names ignore case
      Assert.IsNull( BuildProject().ExportAsGameBinaryFiles( "DATA_%d.bin", "data_%d.BIN", true, true, true, out errors ) );
      Assert.IsTrue( errors.Any( e => e.Contains( "would both be written to" ) ), string.Join( "\n", errors ) );
    }



    // ------------------------------------------------------------- hard errors

    [TestMethod]
    public void TestACharsetWithTooManyTilesStopsTheExport()
    {
      var project = BuildProject();
      AddTiles( project.Charsets[1], 254, 0 );     // 256 tiles
      List<string> errors;
      Assert.IsNull( project.ExportAsGameBinaryFiles( "MAP_%d.bin", "", true, true, true, out errors ) );
      Assert.IsTrue( errors.Any( e => e.Contains( "Bravo" ) && e.Contains( "256 tiles" ) ), string.Join( "\n", errors ) );
    }



    [TestMethod]
    public void TestAMapFileOver64KStopsTheExport()
    {
      var project = BuildProject();
      AddMap( project, "Huge", 0, 256, 256, 1 );   // char grid + colour grid = 128 KB
      List<string> errors;
      Assert.IsNull( project.ExportAsGameBinaryFiles( "MAP_%d.bin", "", true, true, true, out errors ) );
      Assert.IsTrue( errors.Any( e => e.Contains( "Huge" ) && e.Contains( "65535" ) ), string.Join( "\n", errors ) );
    }



    // ------------------------------------------------------------- settings

    [TestMethod]
    public void TestCharsetFileNamePatternIsSaved()
    {
      var project = BuildProject();
      Assert.AreEqual( "CHARSET_MAP_%d.bin", project.Settings.GameBinary.CharsetFilenamePattern, "default" );
      project.Settings.GameBinary.CharsetFilenamePattern = "CHARSET_MAP_%d.dat";

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( project.SaveToBuffer() ) );
      Assert.AreEqual( "CHARSET_MAP_%d.dat", reloaded.Settings.GameBinary.CharsetFilenamePattern );
    }
  }
}
