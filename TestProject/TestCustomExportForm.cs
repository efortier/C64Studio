extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
  /// The custom text exports through the real game-binary export form: the
  /// active definitions are rendered against the files just written and
  /// saved after them (CRLF, UTF-8 without BOM), inactive ones are skipped,
  /// their folders join the single missing-folder question, and a broken
  /// template, an empty file name, two definitions on one path or a
  /// definition on an exported file abort before anything is written.
  /// </summary>
  [TestClass]
  public class TestCustomExportForm
  {
    // ---------------------------------------------------------------- harness

    /// <summary>The form with every dialog of the custom-export path captured instead of shown.</summary>
    private class TestForm : ExportMapAsGameBinary
    {
      public List<string>   Aborted = null;
      public List<string>   Asked = null;
      public List<string>   WriteErrors = new List<string>();

      public TestForm( StudioCore Core ) : base( Core )
      {
      }

      protected override void ReportExportAborted( List<string> Errors )
      {
        Aborted = Errors;
      }

      protected override bool ConfirmCreateDirectories( List<string> Missing )
      {
        Asked = Missing;
        return true;
      }

      protected override void ReportCustomExportWriteError( string Name, string Path, string Message )
      {
        WriteErrors.Add( Name + ": " + Path + ": " + Message );
      }
    }



    private const string TEMPLATE_FILES =
      "{% for m in exported_maps %}\r\n" +
      "RefAddFile(\"MAP-{{ m.id:D4 }}\", \t\"../Maps/Exported/{{ m.file.base_name }}\" + MapExtension, COMPRESSED_MAPS, *)\r\n" +
      "{% endfor %}\r\n";



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



    /// <summary>
    /// Two charsets, maps Alpha (ID 7), Hidden (not exported, ID 3), Bravo
    /// (ID 12); auto-save into Folder; definitions "Files" (map folder),
    /// "Inactive" (off), "Relative" (gen\ under the map folder - the project
    /// is unsaved) and "Rooted" (RootedFolder).
    /// </summary>
    private static MapProject BuildProject( string Folder, string RootedFolder )
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
      settings.ExportFilename         = "MAP_%d.bin";
      settings.CharsetExportDirectory = Folder;
      settings.CharsetFilenamePattern = "CHARSET_MAP_%d.dat";
      settings.GenerateDefFile        = true;
      settings.ExportMapLabels        = true;
      settings.ExportCharsetLabels    = false;

      var files = project.AddCustomExport( "Files" );
      files.FileName = "map_files.asm";
      files.Content  = TEMPLATE_FILES;

      var inactive = project.AddCustomExport( "Inactive" );
      inactive.Active   = false;
      inactive.FileName = "never.txt";
      inactive.Content  = "{{ nope }}";

      var relative = project.AddCustomExport( "Relative" );
      relative.Directory = "gen";
      relative.FileName  = "names.inc";
      relative.Content   = "{% for m in exported_maps %}{{ m.label }}={{ m.id }}\r\n{% endfor %}";

      var rooted = project.AddCustomExport( "Rooted" );
      rooted.Directory = RootedFolder;
      rooted.FileName  = "info.txt";
      rooted.Content   = "{{ project.exported_map_count }} {{ export.map_directory }} {{ export.is_preview }} é";
      return project;
    }



    private static bool RunExport( MapProject Project, out string Log, out TestForm Form )
    {
      var studioProject = new StudioMapProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      Form = new TestForm( StudioTestHost.CreateCore() );
      using ( var output = new TextBox() )
      {
        Form.ApplyExportSettings( studioProject.Settings );
        bool result = Form.HandleExport( new StudioExportMapInfo() { Map = studioProject }, output, new StudioDocumentInfo() );
        Log = output.Text;
        return result;
      }
    }



    private static string NewFolder( string Suffix )
    {
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioCustomExport_" + Guid.NewGuid().ToString( "N" ) + Suffix );
      Directory.CreateDirectory( folder );
      return folder;
    }



    private static void WithFolders( Action<string, string> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder( "" );
        string rooted = NewFolder( "_rooted" );
        try
        {
          Body( folder, rooted );
        }
        finally
        {
          Directory.Delete( folder, true );
          Directory.Delete( rooted, true );
        }
      } );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestActiveDefinitionsAreWrittenAfterTheBinaries()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        string log;
        TestForm form;
        Assert.IsTrue( RunExport( project, out log, out form ), log );
        Assert.IsNull( form.Aborted );
        Assert.AreEqual( 0, form.WriteErrors.Count, string.Join( "\n", form.WriteErrors.ToArray() ) );

        string filesPath = Path.Combine( folder, "map_files.asm" );
        Assert.IsTrue( File.Exists( filesPath ), "the map-folder definition" );
        Assert.AreEqual( "RefAddFile(\"MAP-0007\", \t\"../Maps/Exported/MAP_0007\" + MapExtension, COMPRESSED_MAPS, *)\r\n"
                       + "RefAddFile(\"MAP-0012\", \t\"../Maps/Exported/MAP_0012\" + MapExtension, COMPRESSED_MAPS, *)\r\n",
                         File.ReadAllText( filesPath ) );
        var bytes = File.ReadAllBytes( filesPath );
        Assert.IsFalse( ( bytes.Length >= 3 ) && ( bytes[0] == 0xEF ) && ( bytes[1] == 0xBB ) && ( bytes[2] == 0xBF ), "no UTF-8 BOM" );

        Assert.IsFalse( File.Exists( Path.Combine( folder, "never.txt" ) ), "the inactive definition" );

        string relativePath = Path.Combine( folder, "gen", "names.inc" );
        Assert.AreEqual( "ALPHA=7\r\nBRAVO=12\r\n", File.ReadAllText( relativePath ), "relative folder under the map folder (unsaved project)" );

        string rootedPath = Path.Combine( rooted, "info.txt" );
        Assert.AreEqual( "2 " + folder + " false é", File.ReadAllText( rootedPath ), "rooted folder; UTF-8 content" );

        StringAssert.Contains( log, "Custom export 'Files' -> " + filesPath + " (" );
        StringAssert.Contains( log, "Custom export 'Relative' -> " + relativePath );
        StringAssert.Contains( log, "Custom export 'Rooted' -> " + rootedPath );
        Assert.IsTrue( log.IndexOf( "MAP_0012.bin - map ID 12" ) < log.IndexOf( "Custom export 'Files'" ), "custom exports are logged after the map files" );

        // Everything else the export writes is still there.
        var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
        CollectionAssert.AreEquivalent( new[]
        {
          "MAP_0007.bin", "MAP_0012.bin", "CHARSET_MAP_0007.dat", "CHARSET_MAP_0012.dat", "MAP_0007.def", "MAP_0012.def", "map_names.asm", "map_files.asm"
        }, written, string.Join( ", ", written ) );
      } );
    }



    [TestMethod]
    public void TestTemplatesSeeTheWrittenFiles()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports[0].Content = "{% for m in exported_maps %}{{ m.file.path }}|{{ m.file.size }}|{{ m.file.charset_path }}|{{ m.file.charset_size }}|{{ m.file.def_path }}|{{ m.file.compressed }}\r\n{% endfor %}";
        string log;
        TestForm form;
        Assert.IsTrue( RunExport( project, out log, out form ), log );

        string expected = "";
        foreach ( var id in new[] { "0007", "0012" } )
        {
          string mapPath = Path.Combine( folder, "MAP_" + id + ".bin" );
          string charsetPath = Path.Combine( folder, "CHARSET_MAP_" + id + ".dat" );
          expected += mapPath + "|" + new FileInfo( mapPath ).Length + "|" + charsetPath + "|" + new FileInfo( charsetPath ).Length + "|" + Path.Combine( folder, "MAP_" + id + ".def" ) + "|false\r\n";
        }
        Assert.AreEqual( expected, File.ReadAllText( Path.Combine( folder, "map_files.asm" ) ) );
      } );
    }



    [TestMethod]
    public void TestMissingCustomFoldersJoinTheOneQuestion()
    {
      WithFolders( ( folder, rooted ) =>
      {
        string deeper = Path.Combine( rooted, "deeper" );
        var project = BuildProject( folder, rooted );
        project.CustomExports[3].Directory = deeper;
        string log;
        TestForm form;
        Assert.IsTrue( RunExport( project, out log, out form ), log );
        Assert.IsNotNull( form.Asked, "the folder question was asked" );
        var asked = form.Asked.Select( Path.GetFullPath ).ToArray();
        CollectionAssert.AreEquivalent( new[] { Path.GetFullPath( Path.Combine( folder, "gen" ) ), Path.GetFullPath( deeper ) }, asked, string.Join( ", ", asked ) );
        Assert.IsTrue( File.Exists( Path.Combine( deeper, "info.txt" ) ) );
      } );
    }



    [TestMethod]
    public void TestABrokenTemplateAbortsBeforeAnythingIsWritten()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports[0].Content = "ok\r\n{{ exported_maps.nope }}";
        string log;
        TestForm form;
        Assert.IsFalse( RunExport( project, out log, out form ) );
        Assert.IsNotNull( form.Aborted, "the 'nothing was written' box" );
        Assert.AreEqual( 1, form.Aborted.Count, string.Join( "\n", form.Aborted.ToArray() ) );
        Assert.AreEqual( "custom export 'Files': line 2, col 18: 'exported_maps' is a list; it has no field 'nope' (only 'count') - loop over it with {% for x in exported_maps %}", form.Aborted[0] );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length, "nothing was written" );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( rooted ).Length );
      } );
    }



    [TestMethod]
    public void TestAnEmptyFileNameAborts()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports[2].FileName = "  ";
        string log;
        TestForm form;
        Assert.IsFalse( RunExport( project, out log, out form ) );
        CollectionAssert.AreEqual( new[] { "custom export 'Relative': the file name is empty" }, form.Aborted );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length );
      } );
    }



    [TestMethod]
    public void TestTwoDefinitionsOnOnePathAbort()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports[2].Directory = "";
        project.CustomExports[2].FileName = "MAP_FILES.ASM";
        string log;
        TestForm form;
        Assert.IsFalse( RunExport( project, out log, out form ) );
        CollectionAssert.AreEqual( new[] { "custom exports 'Files' and 'Relative' both write " + Path.Combine( folder, "MAP_FILES.ASM" ) }, form.Aborted );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length );
      } );
    }



    [TestMethod]
    public void TestADefinitionOnAnExportedFileAborts()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports[0].FileName = "charset_map_0012.dat";
        string log;
        TestForm form;
        Assert.IsFalse( RunExport( project, out log, out form ) );
        CollectionAssert.AreEqual( new[] { "custom export 'Files' would overwrite a file the export writes: " + Path.Combine( folder, "charset_map_0012.dat" ) }, form.Aborted );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length );
      } );
    }



    [TestMethod]
    public void TestNoDefinitionsChangesNothing()
    {
      WithFolders( ( folder, rooted ) =>
      {
        var project = BuildProject( folder, rooted );
        project.CustomExports.Clear();
        string log;
        TestForm form;
        Assert.IsTrue( RunExport( project, out log, out form ), log );
        Assert.IsFalse( log.Contains( "Custom export" ) );
        Assert.AreEqual( 7, Directory.GetFiles( folder ).Length );
        Assert.AreEqual( 0, Directory.GetFileSystemEntries( rooted ).Length );
      } );
    }
  }
}
