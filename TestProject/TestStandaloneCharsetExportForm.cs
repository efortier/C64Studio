extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;
using ExportMapAsGameBinary = studio::RetroDevStudio.Controls.ExportMapAsGameBinary;
using StudioByteBuffer = studio::GR.Memory.ByteBuffer;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioDocumentInfo = studio::RetroDevStudio.DocumentInfo;
using StudioExportMapInfo = studio::RetroDevStudio.Formats.ExportMapInfo;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// The real game-binary export form with standalone charsets: a standalone
  /// charset with "Export character set" checked is written under its exported
  /// filename beside the numbered charset files (".bin" appended, the same load
  /// address prefix, a compressed copy named with the compressor's extension),
  /// a map charset's export name is ignored, and the pre-export validation
  /// aborts on an unnamed export-checked standalone charset, a name a numbered
  /// file takes, or a name ending in the compressor's extension.
  /// </summary>
  [TestClass]
  public class TestStandaloneCharsetExportForm
  {
    /// <summary>The form with its "nothing was written" box captured instead of shown.</summary>
    private class QuietGameBinaryForm : ExportMapAsGameBinary
    {
      public List<string> Aborted = null;

      public QuietGameBinaryForm( StudioCore Core ) : base( Core )
      {
      }

      protected override void ReportExportAborted( List<string> Errors )
      {
        Aborted = Errors;
      }
    }



    private static string NewFolder()
    {
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioStandaloneCharsets_" + Guid.NewGuid().ToString( "N" ) );
      Directory.CreateDirectory( folder );
      return folder;
    }



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



    private static MapProject.Map AddMap( MapProject Project, string Name, int MapID, int CharsetIndex )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      for ( int y = 0; y < 2; ++y )
      {
        for ( int x = 0; x < 2; ++x )
        {
          map.Tiles[x, y] = 1;
        }
      }
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Charset 0 (maps Alpha ID 7 and Bravo ID 12, export name ignored),
    /// standalone "Menu" (ECM, exported filename "ui") and standalone
    /// "Scratch" (export unchecked); auto-save into Folder with a $BEEF
    /// charset load-address prefix and the charset labels sidecar.
    /// </summary>
    private static MapProject BuildProject( string Folder )
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      project.Charsets[0].ExportName = "levels.bin";

      int menu = project.AddCharset();
      project.Charsets[menu].SetMode( TextCharMode.COMMODORE_ECM );
      project.Charsets[menu].DisplayName = "Menu";
      project.Charsets[menu].Standalone  = true;
      project.Charsets[menu].ExportName  = "ui";
      for ( int c = 0; c < 64; ++c )
      {
        for ( int y = 0; y < 8; ++y )
        {
          project.Charsets[menu].Charset.Characters[c].Tile.Data.SetU8At( y, (byte)( ( c * 5 + y ) & 0xFF ) );
        }
      }

      int scratch = project.AddCharset();
      project.Charsets[scratch].DisplayName   = "Scratch";
      project.Charsets[scratch].Standalone    = true;
      project.Charsets[scratch].ExportEnabled = false;
      project.Charsets[scratch].ExportName    = "scratch.bin";

      AddMap( project, "Alpha", 7, 0 );
      AddMap( project, "Bravo", 12, 0 );

      var settings = project.Settings.GameBinary;
      settings.SaveOnExport                = true;
      settings.ExportDirectory             = Folder;
      settings.ExportFilename              = "MAP_%d.bin";
      settings.CharsetExportDirectory      = Folder;
      settings.CharsetFilenamePattern      = "CHARSET_MAP_%d.dat";
      settings.CharsetPrefixLoadAddress    = true;
      settings.CharsetPrefixLoadAddressHex = "BEEF";
      settings.GenerateDefFile             = false;
      settings.ExportMapLabels             = false;
      settings.ExportCharsetLabels         = true;
      return project;
    }



    private static bool RunExport( QuietGameBinaryForm Form, MapProject Project, out string Log )
    {
      var studioProject = new StudioMapProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      using ( var output = new TextBox() )
      {
        Form.ApplyExportSettings( studioProject.Settings );
        bool result = Form.HandleExport( new StudioExportMapInfo() { Map = studioProject }, output, new StudioDocumentInfo() );
        Log = output.Text;
        return result;
      }
    }



    /// <summary>The $BEEF prefix + the character data, what a charset file holds.</summary>
    private static byte[] Prefixed( MapProject.MapCharset Charset )
    {
      var raw = new List<byte>() { 0xEF, 0xBE };
      raw.AddRange( Charset.Charset.CharacterData().Data() );
      return raw.ToArray();
    }



    private static string CompressorPath()
    {
      return Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "Compressors", "dali.exe" );
    }



    [TestMethod]
    public void TestAStandaloneCharsetIsWrittenUnderItsExportedFilename()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          string log;
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            Assert.IsTrue( RunExport( form, project, out log ), log );
            Assert.IsNull( form.Aborted );
          }
          var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
          CollectionAssert.AreEquivalent( new[]
          {
            "MAP_0007.bin", "MAP_0012.bin",
            "CHARSET_MAP_0007.dat", "CHARSET_MAP_0012.dat",
            "ui.bin",
            "map_charsets.asm"
          }, written, string.Join( ", ", written ) );
          CollectionAssert.AreEqual( Prefixed( project.Charsets[1] ), File.ReadAllBytes( Path.Combine( folder, "ui.bin" ) ), "the standalone file: load address + Menu's characters" );
          StringAssert.Contains( log, "ui.bin - standalone character set 'Menu'" );
          StringAssert.Contains( log, "1 standalone charset file(s)" );

          string asm = File.ReadAllText( Path.Combine( folder, "map_charsets.asm" ) );
          var lines = asm.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
          Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_UI " ) && l.Contains( " = 2 " ) ), "the standalone set's mode (ECM): " + asm );
          Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_MAP_0007 " ) && l.Contains( " = 0 " ) ), "each exported map's charset mode by map ID: " + asm );
          Assert.IsTrue( lines.Any( l => l.StartsWith( ".const CHARSET_MODE_MAP_0012 " ) ), asm );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestAnExportCheckedStandaloneCharsetWithoutAFilenameAborts()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          project.Charsets[1].ExportName = "";
          string log;
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            Assert.IsFalse( RunExport( form, project, out log ) );
            Assert.IsNotNull( form.Aborted, "the pre-export validation aborts" );
            Assert.IsTrue( form.Aborted.Any( e => e.Contains( "Menu" ) && e.Contains( "no exported filename" ) ), string.Join( "\n", form.Aborted ) );
          }
          Assert.AreEqual( 0, Directory.GetFiles( folder ).Length, "nothing was written" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestAStandaloneFilenameANumberedFileTakesAborts()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          project.Charsets[1].ExportName = "CHARSET_MAP_0007.dat";
          string log;
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            Assert.IsFalse( RunExport( form, project, out log ) );
            Assert.IsNotNull( form.Aborted );
            Assert.IsTrue( form.Aborted.Any( e => e.Contains( "would both be written to 'CHARSET_MAP_0007.dat'" ) ), string.Join( "\n", form.Aborted ) );
          }
          Assert.AreEqual( 0, Directory.GetFiles( folder ).Length, "nothing was written" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestTheCompressedCopyTakesTheCompressorExtension()
    {
      StudioTestHost.RunOnSta( () =>
      {
        Assert.IsTrue( File.Exists( CompressorPath() ), "dali.exe beside the test binaries: " + CompressorPath() );
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          project.Settings.GameBinary.CompressCharsets        = true;
          project.Settings.GameBinary.CharsetCompressFilename = "CHARSET_MAP_%d.zx0";
          string log;
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            Assert.IsTrue( RunExport( form, project, out log ), log );
          }
          var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToList();
          CollectionAssert.Contains( written, "ui.bin" );
          CollectionAssert.Contains( written, "ui.zx0", string.Join( ", ", written ) );
          CollectionAssert.Contains( written, "CHARSET_MAP_0007.zx0" );
          Assert.IsTrue( new FileInfo( Path.Combine( folder, "ui.zx0" ) ).Length > 0 );
          StringAssert.Contains( log, "ui.bin - standalone character set 'Menu'" );
          StringAssert.Contains( log, "+ ui.zx0" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestAFilenameEndingInTheCompressorExtensionAborts()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          project.Settings.GameBinary.CompressCharsets        = true;
          project.Settings.GameBinary.CharsetCompressFilename = "CHARSET_MAP_%d.zx0";
          project.Charsets[1].ExportName = "ui.zx0";
          string log;
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            Assert.IsFalse( RunExport( form, project, out log ) );
            Assert.IsNotNull( form.Aborted );
            Assert.IsTrue( form.Aborted.Any( e => e.Contains( "already ends in the compressor's extension" ) ), string.Join( "\n", form.Aborted ) );
          }
          Assert.AreEqual( 0, Directory.GetFiles( folder ).Length, "nothing was written" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }
  }
}
