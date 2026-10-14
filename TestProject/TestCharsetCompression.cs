extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
  /// "Compress character sets" on the game-binary export: the settings, the
  /// form (both compress checkboxes live INSIDE their group boxes, only
  /// disable the settings beside them and default the load-address override
  /// to $0000 when ticked) and the export itself, which writes a compressed
  /// copy of each map's charset file beside it, named by its own pattern.
  /// </summary>
  [TestClass]
  public class TestCharsetCompression
  {
    // ---------------------------------------------------------------- harness

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
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioCharsetCompression_" + Guid.NewGuid().ToString( "N" ) );
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



    /// <summary>Two maps on charset 0; auto-save into Folder with a $BEEF charset load-address prefix.</summary>
    private static MapProject BuildProject( string Folder )
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      for ( int c = 0; c < 64; ++c )
      {
        for ( int y = 0; y < 8; ++y )
        {
          project.Charsets[0].Charset.Characters[c].Tile.Data.SetU8At( y, (byte)( ( c * 3 + y ) & 0xFF ) );
        }
      }
      foreach ( var name in new[] { "Alpha", "Bravo" } )
      {
        var map = new MapProject.Map() { Name = name, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 2, 2 );
        project.Maps.Add( map );
      }
      var settings = project.Settings.GameBinary;
      settings.SaveOnExport               = true;
      settings.ExportDirectory            = Folder;
      settings.ExportFilename             = "MAP_%d.bin";
      settings.CharsetExportDirectory     = Folder;
      settings.CharsetFilenamePattern     = "CHARSET_MAP_%d.dat";
      settings.CharsetPrefixLoadAddress   = true;
      settings.CharsetPrefixLoadAddressHex = "BEEF";
      settings.GenerateDefFile            = false;
      settings.ExportMapLabels            = true;
      settings.ExportCharsetLabels        = false;
      return project;
    }



    private static StudioMapProject ToStudio( MapProject Project )
    {
      var studioProject = new StudioMapProject();
      Assert.IsTrue( studioProject.ReadFromBuffer( new StudioByteBuffer( Project.SaveToBuffer().Data() ) ) );
      return studioProject;
    }



    private static bool RunExport( ExportMapAsGameBinary Form, MapProject Project, out string Log )
    {
      var studioProject = ToStudio( Project );
      using ( var output = new TextBox() )
      {
        Form.ApplyExportSettings( studioProject.Settings );
        bool result = Form.HandleExport( new StudioExportMapInfo() { Map = studioProject }, output, new StudioDocumentInfo() );
        Log = output.Text;
        return result;
      }
    }



    /// <summary>The charset file the export writes: the $BEEF prefix + the character data.</summary>
    private static byte[] RawCharsetFile( MapProject Project )
    {
      var raw = new List<byte>() { 0xEF, 0xBE };
      raw.AddRange( Project.Charsets[0].Charset.CharacterData().Data() );
      return raw.ToArray();
    }



    private static T Control<T>( System.Windows.Forms.Control Root, string Name ) where T : System.Windows.Forms.Control
    {
      var found = Root.Controls.Find( Name, true );
      Assert.AreEqual( 1, found.Length, "control " + Name );
      return (T)found[0];
    }



    /// <summary>A user's click on a checkbox: WinForms toggles it, then raises Click (a plain Checked = x raises only CheckedChanged).</summary>
    private static void Click( CheckBox Box )
    {
      typeof( CheckBox ).GetMethod( "OnClick", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof( EventArgs ) }, null )
                        .Invoke( Box, new object[] { EventArgs.Empty } );
    }



    private static string CompressorPath()
    {
      return Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "Compressors", "dali.exe" );
    }



    // --------------------------------------------------------------- settings

    [TestMethod]
    public void TestCharsetCompressionSettingsAreSaved()
    {
      var project = new MapProject();
      var s = project.Settings.GameBinary;
      Assert.IsFalse( s.CompressCharsets, "default: off" );
      Assert.AreEqual( "ZX0", s.CharsetCompressor );
      Assert.AreEqual( "", s.CharsetCompressFilename );
      Assert.IsFalse( s.CharsetOverrideLoadAddress );
      Assert.AreEqual( "$0000", s.CharsetOverrideLoadAddressHex, "the override starts at $0000" );
      Assert.AreEqual( "$0000", s.OverrideLoadAddressHex, "so does the map's" );

      s.CompressCharsets            = true;
      s.CharsetCompressFilename     = "charset_map_%d.zx0";
      s.CharsetOverrideLoadAddress  = true;
      s.CharsetOverrideLoadAddressHex = "C000";
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( project.SaveToBuffer() ) );
      Assert.IsTrue( reloaded.Settings.GameBinary.CompressCharsets );
      Assert.AreEqual( "ZX0", reloaded.Settings.GameBinary.CharsetCompressor );
      Assert.AreEqual( "charset_map_%d.zx0", reloaded.Settings.GameBinary.CharsetCompressFilename );
      Assert.IsTrue( reloaded.Settings.GameBinary.CharsetOverrideLoadAddress );
      Assert.AreEqual( "C000", reloaded.Settings.GameBinary.CharsetOverrideLoadAddressHex );
    }



    /// <summary>Walks the chunk sequence (U16 type, U32 length, data) and returns the offset of the last chunk.</summary>
    private static int LastChunkStart( byte[] Data )
    {
      int pos  = 0;
      int last = 0;
      while ( pos + 6 <= Data.Length )
      {
        last = pos;
        pos += 6 + (int)BitConverter.ToUInt32( Data, pos + 2 );
      }
      Assert.AreEqual( Data.Length, pos, "the chunk walk ends exactly at the end of the file" );
      return last;
    }



    [TestMethod]
    public void TestAProjectSavedBeforeCharsetCompressionLoadsWithItOff()
    {
      var project = new MapProject();
      project.Settings.GameBinary.CompressCharsets             = true;
      project.Settings.GameBinary.CharsetOverrideLoadAddress   = true;
      project.Settings.GameBinary.CharsetOverrideLoadAddressHex = "C000";
      project.Settings.GameBinary.CharsetCompressFilename      = "charset_map_%d.zx0";
      byte[] saved = project.SaveToBuffer().Data();

      // The export settings chunk is the last one; cut its five trailing
      // fields (I32, string, I32, string, string) and patch the chunk
      // length - that is what a file saved before this feature looks like.
      int  tail       = 4 + ( 4 + "ZX0".Length ) + 4 + ( 4 + "C000".Length ) + ( 4 + "charset_map_%d.zx0".Length );
      int  chunkStart = LastChunkStart( saved );
      var  older      = new byte[saved.Length - tail];
      Array.Copy( saved, older, older.Length );
      uint length = BitConverter.ToUInt32( saved, chunkStart + 2 ) - (uint)tail;
      Array.Copy( BitConverter.GetBytes( length ), 0, older, chunkStart + 2, 4 );

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( new GR.Memory.ByteBuffer( older ) ) );
      Assert.IsFalse( reloaded.Settings.GameBinary.CompressCharsets );
      Assert.AreEqual( "ZX0", reloaded.Settings.GameBinary.CharsetCompressor );
      Assert.IsFalse( reloaded.Settings.GameBinary.CharsetOverrideLoadAddress );
      Assert.AreEqual( "$0000", reloaded.Settings.GameBinary.CharsetOverrideLoadAddressHex );
      Assert.AreEqual( "", reloaded.Settings.GameBinary.CharsetCompressFilename );
      Assert.AreEqual( "CHARSET_MAP_%d.bin", reloaded.Settings.GameBinary.CharsetFilenamePattern, "the field before the cut is intact" );
    }



    // ------------------------------------------------------------------- form

    [TestMethod]
    public void TestTheCompressCheckboxesLiveInTheirGroupsAndOnlyDisableTheSettingsBesideThem()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
        {
          form.ApplyExportSettings( new StudioMapProject().Settings );   // both compressions off

          var groupCompress          = Control<GroupBox>( form, "groupCompress" );
          var checkCompressMap       = Control<CheckBox>( form, "checkCompressMap" );
          var comboCompressor        = Control<ComboBox>( form, "comboCompressor" );
          var checkOverride          = Control<CheckBox>( form, "checkOverrideLoadAddress" );
          var editOverride           = Control<TextBox>( form, "editOverrideLoadAddress" );
          var editCompressFilename   = Control<TextBox>( form, "editCompressFilename" );
          var groupCharset           = Control<GroupBox>( form, "groupCharset" );
          var groupCharsetCompress   = Control<GroupBox>( form, "groupCharsetCompress" );
          var checkCompressCharsets  = Control<CheckBox>( form, "checkCompressCharsets" );
          var comboCharsetCompressor = Control<ComboBox>( form, "comboCharsetCompressor" );
          var editCharsetCompressFilename = Control<TextBox>( form, "editCharsetCompressFilename" );
          var checkCharsetOverride   = Control<CheckBox>( form, "checkCharsetOverrideLoadAddress" );
          var editCharsetOverride    = Control<TextBox>( form, "editCharsetOverrideLoadAddress" );

          // The checkboxes sit inside their group boxes, the charset one in
          // its own group inside the Character Sets group.
          Assert.AreSame( groupCompress, checkCompressMap.Parent );
          Assert.AreSame( groupCharsetCompress, checkCompressCharsets.Parent );
          Assert.AreSame( groupCharset, groupCharsetCompress.Parent );
          Assert.AreEqual( "Compress map", checkCompressMap.Text );
          Assert.AreEqual( "Compress character sets", checkCompressCharsets.Text );
          Assert.AreEqual( "ZX0", comboCompressor.SelectedItem );
          Assert.AreEqual( "ZX0", comboCharsetCompressor.SelectedItem );

          // Unchecked: the settings beside them are disabled, the group boxes
          // and the checkboxes themselves are not - they can be ticked again.
          Assert.IsTrue( groupCompress.Enabled );
          Assert.IsTrue( checkCompressMap.Enabled );
          Assert.IsFalse( checkCompressMap.Checked );
          Assert.IsFalse( comboCompressor.Enabled );
          Assert.IsFalse( editCompressFilename.Enabled );
          Assert.IsFalse( checkOverride.Enabled );
          Assert.IsFalse( editOverride.Enabled );
          Assert.IsTrue( groupCharsetCompress.Enabled );
          Assert.IsTrue( groupCharset.Enabled );
          Assert.IsTrue( checkCompressCharsets.Enabled );
          Assert.IsFalse( checkCompressCharsets.Checked );
          Assert.IsFalse( comboCharsetCompressor.Enabled );
          Assert.IsFalse( editCharsetCompressFilename.Enabled );
          Assert.IsFalse( checkCharsetOverride.Enabled );
          Assert.IsFalse( editCharsetOverride.Enabled );

          // Ticked: the settings come alive; the override box only with its checkbox.
          checkCompressCharsets.Checked = true;
          Assert.IsTrue( comboCharsetCompressor.Enabled );
          Assert.IsTrue( editCharsetCompressFilename.Enabled );
          Assert.IsTrue( checkCharsetOverride.Enabled );
          Assert.IsFalse( editCharsetOverride.Enabled );
          checkCharsetOverride.Checked = true;
          Assert.IsTrue( editCharsetOverride.Enabled );
          checkCompressCharsets.Checked = false;
          Assert.IsFalse( comboCharsetCompressor.Enabled );
          Assert.IsFalse( editCharsetCompressFilename.Enabled );
          Assert.IsFalse( checkCharsetOverride.Enabled );
          Assert.IsFalse( editCharsetOverride.Enabled );
          Assert.IsTrue( groupCharsetCompress.Enabled );
          Assert.IsTrue( checkCompressCharsets.Enabled );

          checkCompressMap.Checked = true;
          Assert.IsTrue( comboCompressor.Enabled );
          Assert.IsTrue( editCompressFilename.Enabled );
          Assert.IsTrue( checkOverride.Enabled );
          Assert.IsFalse( editOverride.Enabled );
          checkOverride.Checked = true;
          Assert.IsTrue( editOverride.Enabled );
          checkCompressMap.Checked = false;
          Assert.IsFalse( comboCompressor.Enabled );
          Assert.IsFalse( editOverride.Enabled );
          Assert.IsTrue( groupCompress.Enabled );
          Assert.IsTrue( checkCompressMap.Enabled );
        }
      } );
    }



    [TestMethod]
    public void TestTickingCompressionTurnsTheLoadAddressOverrideOnAtZero()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
        {
          var project = new StudioMapProject();
          project.Settings.GameBinary.OverrideLoadAddressHex        = "";
          project.Settings.GameBinary.CharsetOverrideLoadAddressHex = "zz";
          form.ApplyExportSettings( project.Settings );

          var checkCompressMap      = Control<CheckBox>( form, "checkCompressMap" );
          var checkOverride         = Control<CheckBox>( form, "checkOverrideLoadAddress" );
          var editOverride          = Control<TextBox>( form, "editOverrideLoadAddress" );
          var checkCompressCharsets = Control<CheckBox>( form, "checkCompressCharsets" );
          var checkCharsetOverride  = Control<CheckBox>( form, "checkCharsetOverrideLoadAddress" );
          var editCharsetOverride   = Control<TextBox>( form, "editCharsetOverrideLoadAddress" );

          // A click that turns compression on brings the override with it,
          // and a box without a valid address gets $0000.
          Click( checkCompressMap );
          Assert.IsTrue( checkCompressMap.Checked );
          Assert.IsTrue( checkOverride.Checked );
          Assert.AreEqual( "$0000", editOverride.Text );
          Assert.IsTrue( editOverride.Enabled );
          Click( checkCompressCharsets );
          Assert.IsTrue( checkCompressCharsets.Checked );
          Assert.IsTrue( checkCharsetOverride.Checked );
          Assert.AreEqual( "$0000", editCharsetOverride.Text );
          Assert.IsTrue( editCharsetOverride.Enabled );

          // A valid address already there is kept.
          editOverride.Text = "C000";
          Click( checkCompressMap );   // off
          Assert.IsFalse( checkCompressMap.Checked );
          Click( checkCompressMap );   // on again
          Assert.IsTrue( checkOverride.Checked );
          Assert.AreEqual( "C000", editOverride.Text );

          // Loading a project's settings restores them as saved - compression
          // on without an override stays that way.
          var saved = new StudioMapProject();
          saved.Settings.GameBinary.CompressMap              = true;
          saved.Settings.GameBinary.OverrideLoadAddress      = false;
          saved.Settings.GameBinary.OverrideLoadAddressHex   = "";
          saved.Settings.GameBinary.CompressCharsets         = true;
          saved.Settings.GameBinary.CharsetOverrideLoadAddress = false;
          saved.Settings.GameBinary.CharsetOverrideLoadAddressHex = "";
          form.ApplyExportSettings( saved.Settings );
          Assert.IsTrue( checkCompressMap.Checked );
          Assert.IsFalse( checkOverride.Checked );
          Assert.AreEqual( "", editOverride.Text );
          Assert.IsTrue( checkCompressCharsets.Checked );
          Assert.IsFalse( checkCharsetOverride.Checked );
          Assert.AreEqual( "", editCharsetOverride.Text );
        }
      } );
    }



    [TestMethod]
    public void TestTheFormKeepsTheCharsetCompressionSettings()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
        {
          var project = new StudioMapProject();
          project.Settings.GameBinary.CompressCharsets             = true;
          project.Settings.GameBinary.CharsetCompressFilename      = "charset_map_%d.zx0";
          project.Settings.GameBinary.CharsetOverrideLoadAddress   = true;
          project.Settings.GameBinary.CharsetOverrideLoadAddressHex = "C000";
          form.ApplyExportSettings( project.Settings );

          var copy = new StudioMapProject();
          form.UpdateExportSettings( copy.Settings );
          Assert.IsTrue( copy.Settings.GameBinary.CompressCharsets );
          Assert.AreEqual( "ZX0", copy.Settings.GameBinary.CharsetCompressor );
          Assert.AreEqual( "charset_map_%d.zx0", copy.Settings.GameBinary.CharsetCompressFilename );
          Assert.IsTrue( copy.Settings.GameBinary.CharsetOverrideLoadAddress );
          Assert.AreEqual( "C000", copy.Settings.GameBinary.CharsetOverrideLoadAddressHex );
        }
      } );
    }



    // ----------------------------------------------------------------- export

    [TestMethod]
    public void TestTheExportWritesACompressedCopyBesideEachCharsetFile()
    {
      StudioTestHost.RunOnSta( () =>
      {
        Assert.IsTrue( File.Exists( CompressorPath() ),
                       "compressor not found: " + CompressorPath() + " (Application.ExecutablePath = " + Application.ExecutablePath + ")" );
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          project.Settings.GameBinary.CompressCharsets        = true;
          project.Settings.GameBinary.CharsetCompressFilename = "charset_map_%d.zx0";
          string log;
          using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
          {
            Assert.IsTrue( RunExport( form, project, out log ), log );
          }

          // The usual files plus one compressed copy per charset file.
          var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
          CollectionAssert.AreEquivalent( new[] { "CHARSET_MAP_0.dat", "CHARSET_MAP_1.dat", "charset_map_0.zx0", "charset_map_1.zx0",
                                                  "MAP_0.bin", "MAP_1.bin", "map_names.asm" },
                                          written, string.Join( ", ", written ) );

          byte[] raw = RawCharsetFile( project );
          for ( int i = 0; i < 2; ++i )
          {
            CollectionAssert.AreEqual( raw, File.ReadAllBytes( Path.Combine( folder, "CHARSET_MAP_" + i + ".dat" ) ), "the charset file itself stays" );
            byte[] compressed = File.ReadAllBytes( Path.Combine( folder, "charset_map_" + i + ".zx0" ) );
            Assert.IsTrue( compressed.Length > 0 );
            Assert.IsTrue( compressed.Length < raw.Length, "charset_map_" + i + ".zx0: " + compressed.Length + " bytes is not smaller than the " + raw.Length + " raw bytes" );
            CollectionAssert.AreNotEqual( raw, compressed );
          }
          StringAssert.Contains( log, "(+ compressed copies)" );
          StringAssert.Contains( log, "Compressed (ZX0): " + raw.Length + " -> " );
          StringAssert.Contains( log, "CHARSET_MAP_1.dat + charset_map_1.zx0" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestWithCompressionOffOrWithoutANameTheCharsetFilesAreJustTheRawData()
    {
      StudioTestHost.RunOnSta( () =>
      {
        foreach ( var compressWithoutName in new[] { false, true } )
        {
          string folder = NewFolder();
          try
          {
            var project = BuildProject( folder );
            project.Settings.GameBinary.CompressCharsets        = compressWithoutName;
            project.Settings.GameBinary.CharsetCompressFilename = "";
            string log;
            using ( var form = new ExportMapAsGameBinary( StudioTestHost.CreateCore() ) )
            {
              Assert.IsTrue( RunExport( form, project, out log ), log );
            }
            var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
            CollectionAssert.AreEquivalent( new[] { "CHARSET_MAP_0.dat", "CHARSET_MAP_1.dat", "MAP_0.bin", "MAP_1.bin", "map_names.asm" },
                                            written, string.Join( ", ", written ) );
            byte[] raw = RawCharsetFile( project );
            CollectionAssert.AreEqual( raw, File.ReadAllBytes( Path.Combine( folder, "CHARSET_MAP_0.dat" ) ) );
            Assert.IsFalse( log.Contains( "Compressed (" ), log );
          }
          finally
          {
            Directory.Delete( folder, true );
          }
        }
      } );
    }



    [TestMethod]
    public void TestABadLoadAddressOverrideAbortsBeforeAnythingIsWritten()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string folder = NewFolder();
        try
        {
          var project = BuildProject( folder );
          var s = project.Settings.GameBinary;
          s.CompressMap                 = true;
          s.CompressFilename            = "MAP_%d.zx0";
          s.OverrideLoadAddress         = true;
          s.OverrideLoadAddressHex      = "$12345";      // past $FFFF
          s.CompressCharsets            = true;
          s.CharsetCompressFilename     = "charset_map_%d.zx0";
          s.CharsetOverrideLoadAddress  = true;
          s.CharsetOverrideLoadAddressHex = "nope";
          using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
          {
            string log;
            Assert.IsFalse( RunExport( form, project, out log ) );
            Assert.IsNotNull( form.Aborted, "the export reported why it stopped" );
            Assert.AreEqual( 2, form.Aborted.Count, string.Join( "\n", form.Aborted ) );
            StringAssert.Contains( form.Aborted[0], "compressed map load address: \"$12345\"" );
            StringAssert.Contains( form.Aborted[1], "compressed character set load address: \"nope\"" );
          }
          Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length, "nothing written" );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestACompressedNameThatIsBadOrEqualsTheCharsetFileAbortsBeforeAnythingIsWritten()
    {
      StudioTestHost.RunOnSta( () =>
      {
        foreach ( var pattern in new[] { "bad|name_%d.zx0", "charset_map_%d.dat" } )
        {
          string folder = NewFolder();
          try
          {
            var project = BuildProject( folder );
            project.Settings.GameBinary.CompressCharsets        = true;
            project.Settings.GameBinary.CharsetCompressFilename = pattern;
            using ( var form = new QuietGameBinaryForm( StudioTestHost.CreateCore() ) )
            {
              string log;
              Assert.IsFalse( RunExport( form, project, out log ), pattern );
              Assert.IsNotNull( form.Aborted, pattern );
              Assert.AreEqual( 1, form.Aborted.Count, string.Join( "\n", form.Aborted ) );
              StringAssert.Contains( form.Aborted[0], "compressed character set file name" );
              if ( pattern.EndsWith( ".dat" ) )
              {
                StringAssert.Contains( form.Aborted[0], "charset_map_0.dat equals the character set file name CHARSET_MAP_0.dat" );
              }
            }
            Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length, "nothing written for " + pattern );
          }
          finally
          {
            Directory.Delete( folder, true );
          }
        }
      } );
    }
  }
}
