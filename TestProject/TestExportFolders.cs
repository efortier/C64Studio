extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using ExportMapAsAssembly = studio::RetroDevStudio.Controls.ExportMapAsAssembly;
using ExportMapAsGameBinary = studio::RetroDevStudio.Controls.ExportMapAsGameBinary;
using ExportMapFormBase = studio::RetroDevStudio.Controls.ExportMapFormBase;
using StudioByteBuffer = studio::GR.Memory.ByteBuffer;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioDocumentInfo = studio::RetroDevStudio.DocumentInfo;
using StudioExportMapInfo = studio::RetroDevStudio.Formats.ExportMapInfo;
using StudioMapExportType = studio::RetroDevStudio.Formats.MapExportType;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// Export folders that do not exist: the export lists them in one question
  /// before writing anything, creates them on Yes and writes nothing on No -
  /// for the game-binary and the assembly export.
  /// </summary>
  [TestClass]
  public class TestExportFolders
  {
    // ---------------------------------------------------------------- harness

    /// <summary>The game-binary form with its folder question answered by the test.</summary>
    private class AnsweredGameBinaryForm : ExportMapAsGameBinary
    {
      private readonly bool m_Answer;
      public List<string>   Asked = null;

      public AnsweredGameBinaryForm( StudioCore Core, bool Answer ) : base( Core )
      {
        m_Answer = Answer;
      }

      protected override bool ConfirmCreateDirectories( List<string> Missing )
      {
        Asked = Missing;
        return m_Answer;
      }
    }



    /// <summary>The assembly form with its folder question answered by the test.</summary>
    private class AnsweredAssemblyForm : ExportMapAsAssembly
    {
      private readonly bool m_Answer;
      public List<string>   Asked = null;

      public AnsweredAssemblyForm( StudioCore Core, bool Answer ) : base( Core )
      {
        m_Answer = Answer;
      }

      protected override bool ConfirmCreateDirectories( List<string> Missing )
      {
        Asked = Missing;
        return m_Answer;
      }
    }



    private static string NewRoot()
    {
      string root = Path.Combine( Path.GetTempPath(), "C64StudioExportFolders_" + Guid.NewGuid().ToString( "N" ) );
      Directory.CreateDirectory( root );
      return root;
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



    /// <summary>Two maps on charset 0 (export name cs0.bin).</summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 2, 0x10 );
      project.Charsets[0].ExportName = "cs0.bin";
      foreach ( var name in new[] { "Alpha", "Bravo" } )
      {
        var map = new MapProject.Map() { Name = name, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 2, 2 );
        project.Maps.Add( map );
      }
      return project;
    }



    private static StudioMapProject ToStudio( MapProject Project )
    {
      var studioProject = new StudioMapProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      return studioProject;
    }



    private static MapProject GameBinaryProject( string MapFolder, string CharsetFolder )
    {
      var project = BuildProject();
      var settings = project.Settings.GameBinary;
      settings.SaveOnExport           = true;
      settings.ExportDirectory        = MapFolder;
      settings.ExportFilename         = "MAP_%d.bin";
      settings.CharsetExportDirectory = CharsetFolder;
      settings.CharsetFilenamePattern = "CHARSET_MAP_%d.dat";
      settings.GenerateDefFile        = false;
      settings.ExportMapLabels        = true;
      settings.ExportCharsetLabels    = false;
      return project;
    }



    private static bool RunGameBinaryExport( AnsweredGameBinaryForm Form, MapProject Project )
    {
      var studioProject = ToStudio( Project );
      using ( var output = new TextBox() )
      {
        Form.ApplyExportSettings( studioProject.Settings );
        return Form.HandleExport( new StudioExportMapInfo() { Map = studioProject }, output, new StudioDocumentInfo() );
      }
    }



    private static MapProject AssemblyProject( string SaveFolder, string CharsetFolder )
    {
      var project = BuildProject();
      var settings = project.Settings.Assembly;
      settings.SaveOnExport           = true;
      settings.ExportDirectory        = SaveFolder;
      settings.ExportFilename         = "maps.asm";
      settings.CharsetExportDirectory = CharsetFolder;
      settings.AlwaysOverwrite        = true;
      return project;
    }



    private static bool RunAssemblyExport( AnsweredAssemblyForm Form, MapProject Project )
    {
      var studioProject = ToStudio( Project );
      using ( var output = new TextBox() )
      {
        Form.ApplyExportSettings( studioProject.Settings );
        var info = new StudioExportMapInfo() { Map = studioProject, ExportType = StudioMapExportType.TILE_AND_MAP_DATA, RowByRow = true };
        return Form.HandleExport( info, output, new StudioDocumentInfo() );
      }
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestMissingDirectoriesListsEachMissingFolderOnce()
    {
      string root = NewRoot();
      try
      {
        string missing = Path.Combine( root, "Exported", "Maps" );
        var result = ExportMapFormBase.MissingDirectories( new[]
        {
          root,                      // exists
          missing,
          missing.ToUpperInvariant(),  // Windows: same folder
          missing + "\\",
          missing.Replace( '\\', '/' ),
          null,
          "   "
        } );
        CollectionAssert.AreEqual( new[] { Path.GetFullPath( missing ) }, result );
        Assert.IsFalse( Directory.Exists( missing ), "listing never creates anything" );
      }
      finally
      {
        Directory.Delete( root, true );
      }
    }



    [TestMethod]
    public void TestGameBinaryExportCreatesTheMissingFoldersOnYes()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string root = NewRoot();
        try
        {
          string mapFolder     = Path.Combine( root, "Exported", "Maps" );
          string charsetFolder = Path.Combine( root, "Exported", "Charsets" );
          using ( var form = new AnsweredGameBinaryForm( StudioTestHost.CreateCore(), true ) )
          {
            Assert.IsTrue( RunGameBinaryExport( form, GameBinaryProject( mapFolder, charsetFolder ) ) );
            CollectionAssert.AreEqual( new[] { Path.GetFullPath( mapFolder ), Path.GetFullPath( charsetFolder ) }, form.Asked,
                                       "one question listing both folders" );
          }
          foreach ( var file in new[] { Path.Combine( mapFolder, "MAP_0000.bin" ), Path.Combine( mapFolder, "MAP_0001.bin" ),
                                        Path.Combine( charsetFolder, "CHARSET_MAP_0000.dat" ), Path.Combine( charsetFolder, "CHARSET_MAP_0001.dat" ),
                                        Path.Combine( charsetFolder, "map_names.asm" ) } )
          {
            Assert.IsTrue( File.Exists( file ), file );
          }
        }
        finally
        {
          Directory.Delete( root, true );
        }
      } );
    }



    [TestMethod]
    public void TestGameBinaryExportWritesNothingOnNo()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string root = NewRoot();
        try
        {
          string mapFolder = Path.Combine( root, "Exported" );
          using ( var form = new AnsweredGameBinaryForm( StudioTestHost.CreateCore(), false ) )
          {
            Assert.IsFalse( RunGameBinaryExport( form, GameBinaryProject( mapFolder, mapFolder ) ) );
            CollectionAssert.AreEqual( new[] { Path.GetFullPath( mapFolder ) }, form.Asked );
          }
          Assert.IsFalse( Directory.Exists( mapFolder ) );
          Assert.AreEqual( 0, Directory.GetFileSystemEntries( root ).Length, "nothing written" );
        }
        finally
        {
          Directory.Delete( root, true );
        }
      } );
    }



    [TestMethod]
    public void TestNoQuestionWhenEveryFolderExists()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string root = NewRoot();
        try
        {
          using ( var form = new AnsweredGameBinaryForm( StudioTestHost.CreateCore(), false ) )
          {
            Assert.IsTrue( RunGameBinaryExport( form, GameBinaryProject( root, root ) ) );
            Assert.IsNull( form.Asked, "nothing to ask" );
          }
          Assert.IsTrue( File.Exists( Path.Combine( root, "MAP_0000.bin" ) ) );
        }
        finally
        {
          Directory.Delete( root, true );
        }
      } );
    }



    [TestMethod]
    public void TestAssemblyExportCreatesTheMissingFoldersOnYes()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string root = NewRoot();
        try
        {
          string saveFolder    = Path.Combine( root, "Asm" );
          string charsetFolder = Path.Combine( root, "AsmCharsets" );
          using ( var form = new AnsweredAssemblyForm( StudioTestHost.CreateCore(), true ) )
          {
            Assert.IsTrue( RunAssemblyExport( form, AssemblyProject( saveFolder, charsetFolder ) ) );
            CollectionAssert.AreEqual( new[] { Path.GetFullPath( charsetFolder ), Path.GetFullPath( saveFolder ) }, form.Asked );
          }
          Assert.IsTrue( File.Exists( Path.Combine( charsetFolder, "cs0.bin" ) ) );
          Assert.IsTrue( File.Exists( Path.Combine( saveFolder, "maps.asm" ) ) );
        }
        finally
        {
          Directory.Delete( root, true );
        }
      } );
    }



    [TestMethod]
    public void TestAssemblyExportWritesNothingOnNo()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string root = NewRoot();
        try
        {
          string saveFolder = Path.Combine( root, "Asm" );
          using ( var form = new AnsweredAssemblyForm( StudioTestHost.CreateCore(), false ) )
          {
            Assert.IsFalse( RunAssemblyExport( form, AssemblyProject( saveFolder, saveFolder ) ) );
          }
          Assert.AreEqual( 0, Directory.GetFileSystemEntries( root ).Length, "nothing written" );
        }
        finally
        {
          Directory.Delete( root, true );
        }
      } );
    }
  }
}
