extern alias studio;

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using ExportMapAsGameBinary = studio::RetroDevStudio.Controls.ExportMapAsGameBinary;
using StudioByteBuffer = studio::GR.Memory.ByteBuffer;
using StudioDocumentInfo = studio::RetroDevStudio.DocumentInfo;
using StudioExportMapInfo = studio::RetroDevStudio.Formats.ExportMapInfo;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// The real game-binary export form, end to end: with auto-save it writes
  /// one file per exported map, one charset file per map numbered the same
  /// way, one .def per map file and the label sidecars once. (A bad file
  /// name pattern is refused by ExportAsGameBinaryFiles before anything is
  /// written - see TestGameBinaryPerMap; the form reports it in a message
  /// box, which a test cannot click.)
  /// </summary>
  [TestClass]
  public class TestGameBinaryExportForm
  {
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



    private static MapProject.Map AddMap( MapProject Project, string Name, int MapID, int CharsetIndex, int Fill )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      for ( int y = 0; y < 2; ++y )
      {
        for ( int x = 0; x < 2; ++x )
        {
          map.Tiles[x, y] = Fill;
        }
      }
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>Two charsets, maps Alpha (cs0, ID 7), Hidden (cs0, not exported, ID 3), Bravo (cs1, ID 12); auto-save into Folder.</summary>
    private static MapProject BuildProject( string Folder, string MapPattern )
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 2, 0x20 );
      AddMap( project, "Alpha", 7, 0, 1 );
      AddMap( project, "Hidden", 3, 0, 0 ).NotExported = true;
      AddMap( project, "Bravo", 12, second, 1 );
      project.StartMapIndex = 2;

      var settings = project.Settings.GameBinary;
      settings.SaveOnExport           = true;
      settings.ExportDirectory        = Folder;
      settings.ExportFilename         = MapPattern;
      settings.CharsetExportDirectory = Folder;
      settings.CharsetFilenamePattern = "CHARSET_MAP_%d.dat";
      settings.GenerateDefFile        = true;
      settings.ExportMapLabels        = true;
      settings.ExportCharsetLabels    = false;
      return project;
    }



    /// <summary>Runs the form's export on the project; returns HandleExport's result.</summary>
    private static bool RunExport( MapProject Project, out string Log )
    {
      var studioProject = new StudioMapProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
      using ( var output = new TextBox() )
      {
        form.ApplyExportSettings( studioProject.Settings );
        bool result = form.HandleExport( new StudioExportMapInfo() { Map = studioProject }, output, new StudioDocumentInfo() );
        Log = output.Text;
        return result;
      }
    }



    private static string NewFolder()
    {
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioPerMapExport_" + Guid.NewGuid().ToString( "N" ) );
      Directory.CreateDirectory( folder );
      return folder;
    }



    [TestMethod]
    public void TestTheFormWritesOneFilePerMapAndItsCharset()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder, "MAP_%d.bin" );
          string log;
          Assert.IsTrue( RunExport( project, out log ), log );

          var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
          CollectionAssert.AreEquivalent( new[]
          {
            "MAP_0007.bin", "MAP_0012.bin",
            "CHARSET_MAP_0007.dat", "CHARSET_MAP_0012.dat",
            "MAP_0007.def", "MAP_0012.def",
            "map_names.asm"
          }, written, string.Join( ", ", written ) );

          // the bytes are exactly the model's one-map binaries / charset data
          System.Collections.Generic.List<string> errors;
          var files = project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.dat", true, true, true, out errors );
          for ( int i = 0; i < files.Count; ++i )
          {
            CollectionAssert.AreEqual( files[i].Data.Data(), File.ReadAllBytes( Path.Combine( folder, files[i].FileName ) ), files[i].FileName );
            CollectionAssert.AreEqual( files[i].CharsetData.Data(), File.ReadAllBytes( Path.Combine( folder, files[i].CharsetFileName ) ), files[i].CharsetFileName );
          }
          StringAssert.Contains( File.ReadAllText( Path.Combine( folder, "map_names.asm" ) ), ".const MAP_START_ID = 12  // Bravo" );
          StringAssert.Contains( File.ReadAllText( Path.Combine( folder, "MAP_0012.def" ) ), "map_count = 1" );
          StringAssert.Contains( log, "MAP_0012.bin - map ID 12 'Bravo'" );
          StringAssert.Contains( log, "Exported 2 map file(s)" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }
  }
}
