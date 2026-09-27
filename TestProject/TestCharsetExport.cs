using System;
using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// The per-charset files the map export writes, their pre-export warnings,
  /// the two label sidecars, the .def directory description, and the other
  /// export paths' per-charset tile resolution.
  /// </summary>
  [TestClass]
  public class TestCharsetExport
  {
    private static void FillTiles( MapProject.MapCharset Charset, int TileCount, byte CharBase )
    {
      for ( int t = 0; t < TileCount; ++t )
      {
        var tile = new MapProject.Tile();
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0] = new MapProject.TileChar() { Character = (byte)( CharBase + t ), Color = (byte)( ( CharBase + t ) % 16 ) };
        tile.Passable = true;
        tile.Name = "T" + CharBase.ToString( "X2" ) + "_" + t;
        Charset.Tiles.Add( tile );
      }
      Charset.ReindexTiles();
    }



    /// <summary>Three charsets with distinctive art on char 1; the middle one disabled.</summary>
    private static MapProject BuildThreeCharsetProject()
    {
      var proj = new MapProject();
      proj.Charsets[0].ExportName = "charset0.bin";
      proj.Charsets[0].DisplayName = "Base";
      FillTiles( proj.Charsets[0], 2, 0 );
      proj.Charsets[0].Charset.Characters[1].Tile.Data.SetU8At( 0, 0x11 );

      int a = proj.AddCharset();
      proj.Charsets[a].ExportName = "middle.bin";
      proj.Charsets[a].DisplayName = "Middle";
      proj.Charsets[a].ExportEnabled = false;
      FillTiles( proj.Charsets[a], 5, 0x40 );
      proj.Charsets[a].Charset.Characters[1].Tile.Data.SetU8At( 0, 0x22 );

      int b = proj.AddCharset();
      proj.Charsets[b].ExportName = "last.bin";
      proj.Charsets[b].DisplayName = "Last";
      FillTiles( proj.Charsets[b], 3, 0x80 );
      proj.Charsets[b].Charset.Characters[1].Tile.Data.SetU8At( 0, 0x33 );

      var map = new MapProject.Map() { Name = "M", TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 1, 1 );
      proj.Maps.Add( map );
      return proj;
    }



    // ---------------------------------------------------------------- charset files

    [TestMethod]
    public void TestBuildCharsetExportFilesNamesDataAndOrder()
    {
      var proj = BuildThreeCharsetProject();
      var files = proj.BuildCharsetExportFiles( false, 0 );
      Assert.AreEqual( 2, files.Count );
      Assert.AreEqual( 0, files[0].ExportIndex );
      Assert.AreEqual( 1, files[1].ExportIndex );
      Assert.AreEqual( 0, files[0].CharsetIndex );
      Assert.AreEqual( 2, files[1].CharsetIndex );
      Assert.AreEqual( "charset0.bin", files[0].FileName );
      Assert.AreEqual( "last.bin", files[1].FileName );
      Assert.AreEqual( 2048, (int)files[0].Data.Length, "256 characters x 8 bytes" );
      Assert.AreEqual( (byte)0x11, files[0].Data.ByteAt( 8 ), "char 1 byte 0 of charset 0" );
      Assert.AreEqual( (byte)0x33, files[1].Data.ByteAt( 8 ), "char 1 byte 0 of charset 2" );
    }



    [TestMethod]
    public void TestBuildCharsetExportFilesLoadAddressPrefix()
    {
      var proj = BuildThreeCharsetProject();
      var files = proj.BuildCharsetExportFiles( true, 0x3000 );
      Assert.AreEqual( 2050, (int)files[0].Data.Length );
      Assert.AreEqual( (byte)0x00, files[0].Data.ByteAt( 0 ) );
      Assert.AreEqual( (byte)0x30, files[0].Data.ByteAt( 1 ) );
      Assert.AreEqual( (byte)0x11, files[0].Data.ByteAt( 10 ) );
    }



    [TestMethod]
    public void TestBuildCharsetExportFilesSkipsEmptyName()
    {
      var proj = BuildThreeCharsetProject();
      proj.Charsets[0].ExportName = "   ";
      var files = proj.BuildCharsetExportFiles( false, 0 );
      Assert.AreEqual( 1, files.Count, "an enabled charset without a name writes no file" );
      Assert.AreEqual( 2, files[0].CharsetIndex );
      Assert.AreEqual( 1, files[0].ExportIndex, "but it still occupies its compacted slot" );
      var buf = proj.ExportAsGameBinary( true, true, true );
      Assert.AreEqual( (byte)2, buf.ByteAt( 0x3E ), "and it is still counted in the binary" );
    }



    [TestMethod]
    public void TestBuildCharsetExportFilesHonoursExportNumCharacters()
    {
      var proj = BuildThreeCharsetProject();
      proj.Charsets[0].Charset.ExportNumCharacters = 128;
      var files = proj.BuildCharsetExportFiles( false, 0 );
      Assert.AreEqual( 1024, (int)files[0].Data.Length );
    }



    // ---------------------------------------------------------------- warnings

    [TestMethod]
    public void TestDuplicateExportNameWarning()
    {
      var proj = BuildThreeCharsetProject();
      proj.Charsets[2].ExportName = "Charset0.BIN";
      var warnings = proj.GetCharsetFileWarnings();
      Assert.AreEqual( 1, warnings.Count, "case-insensitive duplicate" );
      StringAssert.Contains( warnings[0], "Base" );
      StringAssert.Contains( warnings[0], "Last" );

      proj.Charsets[2].ExportEnabled = false;
      Assert.AreEqual( 0, proj.GetCharsetFileWarnings().Count, "a disabled duplicate does not count" );

      proj.Charsets[2].ExportEnabled = true;
      proj.Charsets[2].ExportName = "";
      Assert.AreEqual( 0, proj.GetCharsetFileWarnings().Count, "an empty name is not a warning" );
    }



    [TestMethod]
    public void TestNoWarningsForCleanProject()
    {
      var proj = BuildThreeCharsetProject();
      Assert.AreEqual( 0, proj.GetCharsetFileWarnings().Count );
      Assert.AreEqual( 0, proj.GetCharsetExportWarnings().Count );
      Assert.AreEqual( 0, proj.GetGameBinaryExportErrors().Count );
    }



    // ---------------------------------------------------------------- label sidecars

    [TestMethod]
    public void TestCharsetLabelsAsm()
    {
      var proj = new MapProject();
      proj.Charsets[0].ExportName = "level 1 tiles.bin";
      proj.Charsets[0].DisplayName = "Level one";
      proj.Charsets[proj.AddCharset()].ExportName = "Cave.chr";
      proj.Charsets[proj.AddCharset()].ExportName = "level 1 tiles.chr";
      int hidden = proj.AddCharset();
      proj.Charsets[hidden].ExportName = "hidden.bin";
      proj.Charsets[hidden].DisplayName = "Hidden";
      proj.Charsets[hidden].ExportEnabled = false;
      int unnamed = proj.AddCharset();
      proj.Charsets[unnamed].ExportName = "";
      proj.Charsets[unnamed].DisplayName = "Nameless";

      string asm = proj.GenerateCharsetLabelsAsm( "#import \"defs.asm\"" );
      var lines = asm.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
      Assert.AreEqual( "#import \"defs.asm\"", lines[0], "the user prefix is the first line" );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_INDEX_LEVEL_1_TILES " ) && l.Contains( " = 0 " ) ), asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_INDEX_CAVE " ) && l.Contains( " = 1 " ) ), asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_INDEX_LEVEL_1_TILES_2 " ) && l.Contains( " = 2 " ) ), "duplicate label gets _2: " + asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_INDEX_LEVEL_1_TILES " ) && l.Contains( "Level one" ) ), "display name in the comment" );
      Assert.IsFalse( asm.Contains( ".const CHARSET_INDEX_HIDDEN" ), "a disabled charset gets no constant" );
      Assert.IsTrue( lines.Any( l => l.StartsWith( "// " ) && l.Contains( "Hidden" ) ), "but is listed as a hint" );
      Assert.IsTrue( lines.Any( l => l.StartsWith( "// index 3" ) && l.Contains( "no export name" ) ), "an enabled charset without a name is a comment" );
      Assert.AreEqual( 3, lines.Count( l => l.StartsWith( ".const " ) ) );
    }



    [TestMethod]
    public void TestCharsetLabelsUseCompactedIndex()
    {
      var proj = BuildThreeCharsetProject();
      string asm = proj.GenerateCharsetLabelsAsm();
      Assert.IsTrue( asm.Contains( "CHARSET_INDEX_CHARSET0" ) );
      var last = asm.Split( '\n' ).First( l => l.Contains( "CHARSET_INDEX_LAST" ) );
      StringAssert.Contains( last, " = 1 " );
      Assert.IsFalse( asm.Contains( ".const CHARSET_INDEX_MIDDLE" ) );
    }



    [TestMethod]
    public void TestMapLabelsAsm()
    {
      var proj = new MapProject();
      foreach ( var name in new[] { "Start", "cave-2", "Start", "Secret" } )
      {
        var map = new MapProject.Map() { Name = name, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 1, 1 );
        proj.Maps.Add( map );
      }
      proj.Maps[3].NotExported = true;
      string asm = proj.GenerateMapLabelsAsm();
      var lines = asm.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const MAP_INDEX_START " ) && l.Contains( " = 0 " ) ), asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const MAP_INDEX_CAVE_2 " ) && l.Contains( " = 1 " ) ), asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const MAP_INDEX_START_2 " ) && l.Contains( " = 2 " ) ), asm );
      Assert.IsFalse( asm.Contains( ".const MAP_INDEX_SECRET" ) );
      Assert.IsTrue( lines.Any( l => l.StartsWith( "// " ) && l.Contains( "Secret" ) ) );
      Assert.AreEqual( 3, lines.Count( l => l.StartsWith( ".const " ) ) );
    }



    [TestMethod]
    public void TestMapLabelsSkipUnnamedMap()
    {
      var proj = new MapProject();
      foreach ( var name in new[] { "", "Named" } )
      {
        var map = new MapProject.Map() { Name = name, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 1, 1 );
        proj.Maps.Add( map );
      }
      string asm = proj.GenerateMapLabelsAsm();
      var lines = asm.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
      Assert.IsTrue( lines.Any( l => l.StartsWith( "// index 0" ) && l.Contains( "no map name" ) ) );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const MAP_INDEX_NAMED " ) && l.Contains( " = 1 " ) ), "the unnamed map still occupies index 0" );
    }



    [TestMethod]
    public void TestLabelsWithNoExportedItems()
    {
      var proj = new MapProject();
      proj.Charsets[0].ExportEnabled = false;
      StringAssert.Contains( proj.GenerateCharsetLabelsAsm(), "(no character set is enabled for export)" );
      StringAssert.Contains( proj.GenerateMapLabelsAsm(), "(no map is exported)" );
    }



    // ---------------------------------------------------------------- .def description

    [TestMethod]
    public void TestDescribeGameBinaryCharsetsMentionsCharsets()
    {
      var proj = BuildThreeCharsetProject();
      proj.Charsets[2].ExportName = "";
      var buf = proj.ExportAsGameBinary( true, true, true );
      string text = MapProject.DescribeGameBinaryCharsets( buf, proj );
      StringAssert.Contains( text, "CHARSET DIRECTORY" );
      StringAssert.Contains( text, "charset 0 'charset0.bin' tile_count=2" );
      StringAssert.Contains( text, "charset 1 '(no export name)' tile_count=3" );
      StringAssert.Contains( text, "mirrored by the legacy header fields" );
    }



    // ---------------------------------------------------------------- other export paths

    [TestMethod]
    public void TestTileOnlyExportsUseRequestedCharset()
    {
      var proj = BuildThreeCharsetProject();
      string data;
      Assert.IsTrue( proj.ExportTilesAsAssembly( out data, "", false, 8, "!byte", 1 ) );
      StringAssert.Contains( data, "NUM_TILES = 5" );
      Assert.IsTrue( proj.ExportTilesAsAssembly( out data, "", false, 8, "!byte" ) );
      StringAssert.Contains( data, "NUM_TILES = 2" );

      string names;
      Assert.IsTrue( proj.ExportTileNamesAsAssembly( out names, "", 2 ) );
      StringAssert.Contains( names, "T80_0" );
      Assert.IsFalse( names.Contains( "T00_0" ) );
      Assert.IsTrue( proj.ExportTileNamesAsAssembly( out names, "" ) );
      StringAssert.Contains( names, "T00_0" );

      var tiles = proj.ExportAsTiles( 2 );
      Assert.AreEqual( (byte)0x80, tiles.ByteAt( 0 ), "charset 2's first tile char" );
    }



    [TestMethod]
    public void TestExportMapAsBufferResolvesPerMapCharset()
    {
      var proj = BuildThreeCharsetProject();
      proj.Settings.Assembly.EmptyTileIndex = 0;
      proj.Charsets[2].Tiles[1].NotExportedOnMap = true;

      var onBase = proj.Maps[0];
      onBase.Tiles.Resize( 3, 1 );
      onBase.Tiles[0, 0] = 1;
      onBase.Tiles[1, 0] = 9;    // beyond charset 0's two tiles
      onBase.Tiles[2, 0] = 0;
      var buf = proj.ExportMapAsBuffer( onBase, true );
      Assert.AreEqual( (byte)1, buf.ByteAt( 0 ) );
      Assert.AreEqual( (byte)0, buf.ByteAt( 1 ), "out-of-range cell becomes the empty tile" );

      var onLast = new MapProject.Map() { Name = "L", TileSpacingX = 1, TileSpacingY = 1, CharsetIndex = 2 };
      onLast.Tiles.Resize( 2, 1 );
      onLast.Tiles[0, 0] = 1;    // NotExportedOnMap in charset 2 only
      onLast.Tiles[1, 0] = 2;
      proj.Maps.Add( onLast );
      buf = proj.ExportMapAsBuffer( onLast, true );
      Assert.AreEqual( (byte)0, buf.ByteAt( 0 ), "NotExportedOnMap resolved in the map's own charset" );
      Assert.AreEqual( (byte)2, buf.ByteAt( 1 ) );
    }
  }
}
