using System;
using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Standalone charsets (UI / menus) and per-charset modes in the map project
  /// model: the Standalone flag's persistence and load rule, the per-charset
  /// mode (kept since project-info version 7, stamped once for older files,
  /// seeded from the project mode for a new charset), the standalone files'
  /// names, errors and contents, and the CHARSET_MODE_ constants of
  /// map_charsets.asm.
  /// </summary>
  [TestClass]
  public class TestStandaloneCharsets
  {
    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex, int MapID )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, MapID = MapID, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Charset 0 "Levels" (multicolor, map Alpha ID 1), charset 1 "Caves"
    /// (hires, map Bravo ID 2), charset 2 "Menu" (ECM, standalone, "ui"),
    /// charset 3 "Title" (hires, standalone, "title.chr"); project mode multicolor.
    /// </summary>
    private static MapProject BuildProject()
    {
      var proj = new MapProject();
      proj.Mode = TextMode.COMMODORE_40_X_25_MULTICOLOR;
      proj.Charsets[0].SetMode( TextCharMode.COMMODORE_MULTICOLOR );
      proj.Charsets[0].DisplayName = "Levels";

      int caves = proj.AddCharset();
      proj.Charsets[caves].SetMode( TextCharMode.COMMODORE_HIRES );
      proj.Charsets[caves].DisplayName = "Caves";

      int menu = proj.AddCharset();
      proj.Charsets[menu].SetMode( TextCharMode.COMMODORE_ECM );
      proj.Charsets[menu].DisplayName = "Menu";
      proj.Charsets[menu].Standalone  = true;
      proj.Charsets[menu].ExportName  = "ui";
      proj.Charsets[menu].Charset.Characters[1].Tile.Data.SetU8At( 0, 0xEE );

      int title = proj.AddCharset();
      proj.Charsets[title].SetMode( TextCharMode.COMMODORE_HIRES );
      proj.Charsets[title].DisplayName = "Title";
      proj.Charsets[title].Standalone  = true;
      proj.Charsets[title].ExportName  = "title.chr";

      AddMap( proj, "Alpha", 0, 1 );
      AddMap( proj, "Bravo", caves, 2 );
      return proj;
    }



    private static MapProject RoundTrip( MapProject Project )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Project.SaveToBuffer() ) );
      return reloaded;
    }



    private static string[] Lines( string Text )
    {
      return Text.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
    }



    // ---------------------------------------------------------------- the flag

    [TestMethod]
    public void TestStandaloneFlagRoundTrips()
    {
      var reloaded = RoundTrip( BuildProject() );
      Assert.IsFalse( reloaded.Charsets[0].Standalone );
      Assert.IsFalse( reloaded.Charsets[1].Standalone );
      Assert.IsTrue( reloaded.Charsets[2].Standalone, "Menu" );
      Assert.IsTrue( reloaded.Charsets[3].Standalone, "Title" );
      Assert.AreEqual( "ui", reloaded.Charsets[2].ExportName );
      Assert.AreEqual( "title.chr", reloaded.Charsets[3].ExportName );
    }



    [TestMethod]
    public void TestLoadDropsTheFlagOfACharsetAMapUses()
    {
      var proj = BuildProject();
      proj.Charsets[1].Standalone = true;    // Bravo is bound to it - impossible through the editor
      var reloaded = RoundTrip( proj );
      Assert.IsFalse( reloaded.Charsets[1].Standalone, "a charset a map uses cannot be standalone: the flag goes, the binding stays" );
      Assert.AreEqual( 1, reloaded.Maps[1].CharsetIndex, "Bravo keeps its charset" );
      Assert.IsTrue( reloaded.Charsets[2].Standalone, "an unbound standalone charset keeps its flag" );
    }



    [TestMethod]
    public void TestDuplicateCopiesTheFlagAndTheMode()
    {
      var proj = BuildProject();
      int copy = proj.DuplicateCharset( 2 );
      Assert.IsTrue( proj.Charsets[copy].Standalone );
      Assert.AreEqual( "", proj.Charsets[copy].ExportName, "the exported filename is cleared, two sets never default to one file" );
      Assert.AreEqual( TextCharMode.COMMODORE_ECM, proj.Charsets[copy].Charset.Mode );
    }



    [TestMethod]
    public void TestANewCharsetIsAMapCharsetInTheProjectMode()
    {
      var proj = BuildProject();
      proj.Charsets[0].SetMode( TextCharMode.COMMODORE_ECM );   // charset 0 is not the seed
      int index = proj.AddCharset();
      Assert.IsFalse( proj.Charsets[index].Standalone );
      Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, proj.Charsets[index].Charset.Mode, "the project (map) mode" );
    }



    // ---------------------------------------------------------------- modes

    [TestMethod]
    public void TestPerCharsetModesSurviveASave()
    {
      var reloaded = RoundTrip( BuildProject() );
      Assert.AreEqual( TextMode.COMMODORE_40_X_25_MULTICOLOR, reloaded.Mode );
      Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, reloaded.Charsets[0].Charset.Mode );
      Assert.AreEqual( TextCharMode.COMMODORE_HIRES, reloaded.Charsets[1].Charset.Mode );
      Assert.AreEqual( TextCharMode.COMMODORE_ECM, reloaded.Charsets[2].Charset.Mode );
      Assert.AreEqual( TextCharMode.COMMODORE_HIRES, reloaded.Charsets[3].Charset.Mode );
      Assert.AreEqual( GraphicTileMode.COMMODORE_ECM, reloaded.Charsets[2].Charset.Characters[1].Tile.Mode, "the character tiles follow the charset's mode" );
    }



    [TestMethod]
    public void TestFilesBelowVersion7GetTheProjectModeStampedOnce()
    {
      var saved = BuildProject().SaveToBuffer();
      byte[] bytes = saved.Data();
      // MAP_PROJECT_INFO is the first chunk: [u16 type][u32 size][u32 version]...
      Assert.AreEqual( FileChunkConstants.MAP_PROJECT_INFO, BitConverter.ToUInt16( bytes, 0 ) );
      Assert.AreEqual( MapProject.PROJECT_INFO_VERSION, BitConverter.ToUInt32( bytes, 6 ) );
      Assert.AreEqual( 7u, MapProject.PROJECT_INFO_VERSION );

      bytes[6] = 6;   // a file saved before per-charset modes
      var old = new MapProject();
      Assert.IsTrue( old.ReadFromBuffer( new ByteBuffer( bytes ) ) );
      foreach ( var cs in old.Charsets )
      {
        Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, cs.Charset.Mode, "every charset gets the project mode, as the editor stamped it on every load before" );
        Assert.AreEqual( GraphicTileMode.COMMODORE_HIRES, cs.Charset.Characters[1].Tile.Mode, "custom color 1 in multicolor mode = a hires character tile" );
      }
      Assert.IsTrue( old.Charsets[2].Standalone, "the stamp leaves the flag alone" );

      // and once saved by this build, every charset keeps its own mode
      var resaved = RoundTrip( old );
      resaved.Charsets[2].SetMode( TextCharMode.COMMODORE_ECM );
      Assert.AreEqual( TextCharMode.COMMODORE_ECM, RoundTrip( resaved ).Charsets[2].Charset.Mode );
      Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, RoundTrip( resaved ).Charsets[0].Charset.Mode );
    }



    [TestMethod]
    public void TestSetModeRederivesTheCharacterTiles()
    {
      var cs = new MapProject().Charsets[0];
      cs.Charset.Characters[5].Tile.CustomColor = 9;
      cs.SetMode( TextCharMode.COMMODORE_ECM );
      Assert.AreEqual( TextCharMode.COMMODORE_ECM, cs.Charset.Mode );
      Assert.IsTrue( cs.Charset.Characters.All( c => c.Tile.Mode == GraphicTileMode.COMMODORE_ECM ) );

      cs.SetMode( TextCharMode.COMMODORE_MULTICOLOR );
      Assert.AreEqual( GraphicTileMode.COMMODORE_HIRES, cs.Charset.Characters[1].Tile.Mode, "custom color 1 = hires character" );
      Assert.AreEqual( GraphicTileMode.COMMODORE_MULTICOLOR_CHARACTERS, cs.Charset.Characters[5].Tile.Mode, "custom color 9 = multicolor character" );
      Assert.IsTrue( cs.Charset.Characters.All( c => c.Tile.Data.Length == 8 ) );
    }



    // ---------------------------------------------------------------- file names

    [TestMethod]
    public void TestStandaloneFileNameAddsBinWhenNoExtensionIsGiven()
    {
      var proj = BuildProject();
      Assert.AreEqual( "ui.bin", MapProject.StandaloneCharsetFileName( proj.Charsets[2] ) );
      Assert.AreEqual( "title.chr", MapProject.StandaloneCharsetFileName( proj.Charsets[3] ), "an extension is used as typed" );
      proj.Charsets[2].ExportName = "  menu  ";
      Assert.AreEqual( "menu.bin", MapProject.StandaloneCharsetFileName( proj.Charsets[2] ), "trimmed" );
      proj.Charsets[2].ExportName = "font.v2";
      Assert.AreEqual( "font.v2", MapProject.StandaloneCharsetFileName( proj.Charsets[2] ), "anything after a dot counts as the extension" );
      proj.Charsets[2].ExportName = ".hidden";
      Assert.AreEqual( ".hidden.bin", MapProject.StandaloneCharsetFileName( proj.Charsets[2] ), "a leading dot is not an extension" );
      proj.Charsets[2].ExportName = "";
      Assert.AreEqual( "", MapProject.StandaloneCharsetFileName( proj.Charsets[2] ), "no name, no file" );
      proj.Charsets[0].ExportName = "levels.bin";
      Assert.AreEqual( "", MapProject.StandaloneCharsetFileName( proj.Charsets[0] ), "a map charset has no file of its own" );
    }



    [TestMethod]
    public void TestCompressedCopyTakesTheCompressorExtension()
    {
      Assert.AreEqual( ".zx0", MapProject.CompressorFileExtension( "ZX0" ) );
      Assert.AreEqual( ".zx0", MapProject.CompressorFileExtension( "" ), "the default compressor" );
      Assert.AreEqual( ".exomizer", MapProject.CompressorFileExtension( "Exomizer" ) );
      Assert.AreEqual( "ui.zx0", MapProject.CompressedFileNameFor( "ui.bin", ".zx0" ) );
      Assert.AreEqual( "title.zx0", MapProject.CompressedFileNameFor( "title.chr", "zx0" ), "with or without the dot" );
      Assert.AreEqual( "ui.zx0", MapProject.CompressedFileNameFor( "ui", ".zx0" ), "appended when there is no extension" );
      Assert.AreEqual( "font.zx0", MapProject.CompressedFileNameFor( "font.v2", ".zx0" ) );
    }



    [TestMethod]
    public void TestStandaloneExportErrors()
    {
      var proj = BuildProject();
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors().Count );
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors( ".zx0" ).Count );
      Assert.AreEqual( 0, proj.GetCharsetFileWarnings().Count, "the assembly export sees the same list" );

      // export checked, no exported filename
      proj.Charsets[2].ExportName = "";
      var errors = proj.GetStandaloneCharsetExportErrors();
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "Menu" );
      StringAssert.Contains( errors[0], "no exported filename" );
      proj.Charsets[2].ExportEnabled = false;
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors().Count, "unchecked = nothing to write, nothing to complain about" );
      proj.Charsets[2].ExportEnabled = true;

      // characters a file name cannot hold
      proj.Charsets[2].ExportName = "ui:menu";
      errors = proj.GetStandaloneCharsetExportErrors();
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "not a valid file name" );

      // two standalone charsets on one name (case-insensitive)
      proj.Charsets[2].ExportName = "ui.bin";
      proj.Charsets[3].ExportName = "UI.BIN";
      errors = proj.GetStandaloneCharsetExportErrors();
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "Menu" );
      StringAssert.Contains( errors[0], "Title" );
      StringAssert.Contains( errors[0], "UI.BIN" );

      // two files compressing to one name
      proj.Charsets[3].ExportName = "ui.dat";
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors().Count, "different plain names" );
      errors = proj.GetStandaloneCharsetExportErrors( ".zx0" );
      Assert.AreEqual( 1, errors.Count, "but both compress to ui.zx0" );
      StringAssert.Contains( errors[0], "ui.zx0" );

      // a name already ending in the compressor's extension
      proj.Charsets[3].ExportName = "title.chr";
      proj.Charsets[2].ExportName = "ui.ZX0";
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors().Count, "fine without compression" );
      errors = proj.GetStandaloneCharsetExportErrors( ".zx0" );
      Assert.AreEqual( 1, errors.Count );
      StringAssert.Contains( errors[0], "already ends in the compressor's extension" );

      // a map charset's name never matters
      proj.Charsets[2].ExportName = "ui";
      proj.Charsets[0].ExportName = "ui.bin";
      proj.Charsets[1].ExportName = "";
      Assert.AreEqual( 0, proj.GetStandaloneCharsetExportErrors( ".zx0" ).Count );
    }



    [TestMethod]
    public void TestBuildStandaloneCharsetFiles()
    {
      var proj = BuildProject();
      proj.Charsets[0].ExportName = "levels.bin";   // a map charset: ignored
      var files = proj.BuildStandaloneCharsetFiles();
      Assert.AreEqual( 2, files.Count );
      Assert.AreEqual( 2, files[0].CharsetIndex );
      Assert.AreEqual( "ui.bin", files[0].FileName );
      Assert.AreEqual( 2048, (int)files[0].Data.Length, "256 characters x 8 bytes, no load address here" );
      Assert.AreEqual( (byte)0xEE, files[0].Data.ByteAt( 8 ), "Menu's char 1" );
      Assert.AreEqual( 3, files[1].CharsetIndex );
      Assert.AreEqual( "title.chr", files[1].FileName );
      Assert.IsTrue( proj.HasStandaloneCharsetFiles() );

      proj.Charsets[3].ExportEnabled = false;
      proj.Charsets[2].ExportName = "";
      Assert.AreEqual( 0, proj.BuildStandaloneCharsetFiles().Count, "unchecked or unnamed: no file" );
      Assert.IsFalse( proj.HasStandaloneCharsetFiles() );
    }



    [TestMethod]
    public void TestThePerMapExportStillWritesOnlyTheMapCharsets()
    {
      var proj = BuildProject();
      List<string> errors;
      var files = proj.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.dat", true, true, true, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      CollectionAssert.AreEqual( new[] { "CHARSET_MAP_0001.dat", "CHARSET_MAP_0002.dat" }, files.Select( f => f.CharsetFileName ).ToArray() );
      Assert.AreEqual( 0, files[0].CharsetData.Compare( proj.Charsets[0].Charset.CharacterData() ) );
      Assert.AreEqual( 0, files[1].CharsetData.Compare( proj.Charsets[1].Charset.CharacterData() ) );
    }



    // ---------------------------------------------------------------- sidecar

    [TestMethod]
    public void TestCharsetLabelsAsmHasTheModeConstants()
    {
      var proj = BuildProject();
      string asm = proj.GenerateCharsetLabelsAsm();
      var lines = Lines( asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_UI " ) && l.Contains( " = 2 " ) && l.Contains( "ui.bin" ) ), "standalone Menu, ECM: " + asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_TITLE " ) && l.Contains( " = 0 " ) ), "standalone Title, hires: " + asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_MAP_0001 " ) && l.Contains( " = 1 " ) && l.Contains( "Alpha" ) ), "map Alpha's charset, multicolor: " + asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_MAP_0002 " ) && l.Contains( " = 0 " ) ), "map Bravo's charset, hires: " + asm );
      Assert.IsTrue( lines.Any( l => l.StartsWith( "// Character set modes:" ) && l.Contains( "2 = Commodore ECM" ) ), "the values are explained" );

      proj.Maps[1].NotExported = true;
      proj.Charsets[3].ExportEnabled = false;
      asm = proj.GenerateCharsetLabelsAsm();
      Assert.IsFalse( asm.Contains( "CHARSET_MODE_MAP_0002" ), "a map not exported gets no constant" );
      Assert.IsFalse( asm.Contains( "CHARSET_MODE_TITLE" ), "a standalone charset not exported gets no constant" );
      Assert.IsTrue( asm.Contains( "CHARSET_MODE_UI" ) );
    }
  }
}
