extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using ExportCharsetAsBinaryFile = studio::RetroDevStudio.Controls.ExportCharsetAsBinaryFile;
using StudioCharsetProject = studio::RetroDevStudio.Formats.CharsetProject;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioDocumentInfo = studio::RetroDevStudio.DocumentInfo;
using StudioExportCharsetInfo = studio::RetroDevStudio.Formats.ExportCharsetInfo;



namespace TestProject
{
  /// <summary>
  /// "Compress character set" on the charset project's "to binary file"
  /// export: the settings, the form (the checkbox inside its Compressor group
  /// only disables the settings beside it and defaults the load-address
  /// override to $0000 when ticked) and the export, which writes a
  /// compressed copy beside the exported file.
  /// </summary>
  [TestClass]
  public class TestCharsetProjectCompression
  {
    // ---------------------------------------------------------------- harness

    /// <summary>The form with its "nothing was written" box captured instead of shown.</summary>
    private class QuietForm : ExportCharsetAsBinaryFile
    {
      public List<string> Aborted = null;

      public QuietForm( StudioCore Core ) : base( Core )
      {
      }

      protected override void ReportExportAborted( List<string> Errors )
      {
        Aborted = Errors;
      }
    }



    private static string NewFolder()
    {
      string folder = Path.Combine( Path.GetTempPath(), "C64StudioCharsetProjectCompression_" + Guid.NewGuid().ToString( "N" ) );
      Directory.CreateDirectory( folder );
      return folder;
    }



    /// <summary>A charset with recognisable data in its first 64 characters.</summary>
    private static StudioCharsetProject BuildCharset()
    {
      var charset = new StudioCharsetProject();
      for ( int c = 0; c < 64; ++c )
      {
        for ( int y = 0; y < 8; ++y )
        {
          charset.Characters[c].Tile.Data.SetU8At( y, (byte)( ( c * 5 + y ) & 0xFF ) );
        }
      }
      charset.ExportBinaryPrefixLoadAddress = true;
      charset.ExportBinaryLoadAddress       = 0x3000;
      return charset;
    }



    private static List<int> AllCharacters()
    {
      return Enumerable.Range( 0, 256 ).ToList();
    }



    /// <summary>The exported file: the $3000 prefix + every character's 8 bytes.</summary>
    private static byte[] RawExport( StudioCharsetProject Charset )
    {
      var raw = new List<byte>() { 0x00, 0x30 };
      foreach ( int i in AllCharacters() )
      {
        raw.AddRange( Charset.Characters[i].Tile.Data.Data() );
      }
      return raw.ToArray();
    }



    private static bool RunExport( ExportCharsetAsBinaryFile Form, StudioCharsetProject Charset, string OutputPath, out string Log )
    {
      using ( var output = new TextBox() )
      {
        Form.LoadSettings( Charset );
        var info = new StudioExportCharsetInfo() { Charset = Charset, ExportIndices = AllCharacters(), OutputPath = OutputPath };
        bool result = Form.HandleExport( info, output, new StudioDocumentInfo() );
        Log = output.Text;
        return result;
      }
    }



    private static T Control<T>( System.Windows.Forms.Control Root, string Name ) where T : System.Windows.Forms.Control
    {
      var found = Root.Controls.Find( Name, true );
      Assert.AreEqual( 1, found.Length, "control " + Name );
      return (T)found[0];
    }



    /// <summary>A user's click on a checkbox: WinForms toggles it, then raises Click.</summary>
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
    public void TestCharsetProjectCompressionSettingsAreSaved()
    {
      var charset = new CharsetProject();
      Assert.IsFalse( charset.ExportBinaryCompress, "default: off" );
      Assert.AreEqual( "ZX0", charset.ExportBinaryCompressor );
      Assert.AreEqual( "", charset.ExportBinaryCompressFilename );
      Assert.IsFalse( charset.ExportBinaryOverrideLoadAddress );
      Assert.AreEqual( "$0000", charset.ExportBinaryOverrideLoadAddressHex, "the override starts at $0000" );

      charset.ExportBinaryCompress                = true;
      charset.ExportBinaryCompressFilename        = "charset.zx0";
      charset.ExportBinaryOverrideLoadAddress     = true;
      charset.ExportBinaryOverrideLoadAddressHex  = "C000";
      charset.ExportBinaryPrefixLoadAddress       = true;
      charset.ExportBinaryLoadAddress             = 0x3000;
      var reloaded = new CharsetProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( charset.SaveToBuffer() ) );
      Assert.IsTrue( reloaded.ExportBinaryCompress );
      Assert.AreEqual( "ZX0", reloaded.ExportBinaryCompressor );
      Assert.AreEqual( "charset.zx0", reloaded.ExportBinaryCompressFilename );
      Assert.IsTrue( reloaded.ExportBinaryOverrideLoadAddress );
      Assert.AreEqual( "C000", reloaded.ExportBinaryOverrideLoadAddressHex );
      Assert.IsTrue( reloaded.ExportBinaryPrefixLoadAddress, "the fields before them are intact" );
      Assert.AreEqual( 0x3000, reloaded.ExportBinaryLoadAddress );
    }



    // ------------------------------------------------------------------- form

    [TestMethod]
    public void TestLoadSettingsFillsTheFormWithoutDirtyingAndLaterChangesPersist()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var form = new ExportCharsetAsBinaryFile( StudioTestHost.CreateCore() ) )
        {
          int settingsChanged = 0;
          form.SettingsChanged += ( s, e ) => ++settingsChanged;

          var charset = BuildCharset();
          charset.ExportBinaryCompress               = true;
          charset.ExportBinaryCompressFilename       = "charset.zx0";
          charset.ExportBinaryOverrideLoadAddress    = true;
          charset.ExportBinaryOverrideLoadAddressHex = "C000";
          form.LoadSettings( charset );
          form.LoadSettings( charset );   // a second load must not double the handlers
          Assert.AreEqual( 0, settingsChanged, "loading never dirties the project" );

          var groupCompress    = Control<GroupBox>( form, "groupCompress" );
          var checkCompress    = Control<CheckBox>( form, "checkCompress" );
          var comboCompressor  = Control<ComboBox>( form, "comboCompressor" );
          var editFilename     = Control<TextBox>( form, "editCompressFilename" );
          var checkOverride    = Control<CheckBox>( form, "checkOverrideLoadAddress" );
          var editOverride     = Control<TextBox>( form, "editOverrideLoadAddress" );
          Assert.AreSame( groupCompress, checkCompress.Parent );
          Assert.AreEqual( "Compress character set", checkCompress.Text );
          Assert.IsTrue( checkCompress.Checked );
          Assert.AreEqual( "ZX0", comboCompressor.SelectedItem );
          Assert.AreEqual( "charset.zx0", editFilename.Text );
          Assert.IsTrue( checkOverride.Checked );
          Assert.AreEqual( "C000", editOverride.Text );
          Assert.IsTrue( comboCompressor.Enabled );
          Assert.IsTrue( editFilename.Enabled );
          Assert.IsTrue( checkOverride.Enabled );
          Assert.IsTrue( editOverride.Enabled );

          // User changes land in the project, once each.
          editFilename.Text = "font.zx0";
          Assert.AreEqual( "font.zx0", charset.ExportBinaryCompressFilename );
          Assert.AreEqual( 1, settingsChanged );
          editOverride.Text = "$4000";
          Assert.AreEqual( "$4000", charset.ExportBinaryOverrideLoadAddressHex );
          Assert.AreEqual( 2, settingsChanged );
          checkOverride.Checked = false;
          Assert.IsFalse( charset.ExportBinaryOverrideLoadAddress );
          Assert.IsFalse( editOverride.Enabled );
          Assert.AreEqual( 3, settingsChanged );

          // Unticking compression disables only the settings beside it.
          checkCompress.Checked = false;
          Assert.IsFalse( charset.ExportBinaryCompress );
          Assert.IsFalse( comboCompressor.Enabled );
          Assert.IsFalse( editFilename.Enabled );
          Assert.IsFalse( checkOverride.Enabled );
          Assert.IsFalse( editOverride.Enabled );
          Assert.IsTrue( groupCompress.Enabled );
          Assert.IsTrue( checkCompress.Enabled );
        }
      } );
    }



    [TestMethod]
    public void TestTickingCompressionTurnsTheLoadAddressOverrideOnAtZero()
    {
      StudioTestHost.RunOnSta( () =>
      {
        using ( var form = new ExportCharsetAsBinaryFile( StudioTestHost.CreateCore() ) )
        {
          var charset = BuildCharset();
          charset.ExportBinaryOverrideLoadAddressHex = "";
          form.LoadSettings( charset );
          var checkCompress = Control<CheckBox>( form, "checkCompress" );
          var checkOverride = Control<CheckBox>( form, "checkOverrideLoadAddress" );
          var editOverride  = Control<TextBox>( form, "editOverrideLoadAddress" );
          Assert.IsFalse( checkOverride.Checked );

          Click( checkCompress );
          Assert.IsTrue( checkCompress.Checked );
          Assert.IsTrue( checkOverride.Checked );
          Assert.AreEqual( "$0000", editOverride.Text );
          Assert.IsTrue( editOverride.Enabled );
          Assert.IsTrue( charset.ExportBinaryCompress, "persisted" );
          Assert.IsTrue( charset.ExportBinaryOverrideLoadAddress );
          Assert.AreEqual( "$0000", charset.ExportBinaryOverrideLoadAddressHex );

          // A valid address already there is kept.
          editOverride.Text = "C000";
          Click( checkCompress );   // off
          Click( checkCompress );   // on again
          Assert.AreEqual( "C000", editOverride.Text );

          // Loading a project with compression on but no override keeps it that way.
          var saved = BuildCharset();
          saved.ExportBinaryCompress               = true;
          saved.ExportBinaryOverrideLoadAddress    = false;
          saved.ExportBinaryOverrideLoadAddressHex = "";
          form.LoadSettings( saved );
          Assert.IsTrue( checkCompress.Checked );
          Assert.IsFalse( checkOverride.Checked );
          Assert.AreEqual( "", editOverride.Text );
        }
      } );
    }



    // ----------------------------------------------------------------- export

    [TestMethod]
    public void TestTheExportWritesACompressedCopyBesideTheCharsetFile()
    {
      StudioTestHost.RunOnSta( () =>
      {
        Assert.IsTrue( File.Exists( CompressorPath() ),
                       "compressor not found: " + CompressorPath() + " (Application.ExecutablePath = " + Application.ExecutablePath + ")" );
        string folder = NewFolder();
        try
        {
          var charset = BuildCharset();
          charset.ExportBinaryCompress         = true;
          charset.ExportBinaryCompressFilename = "charset.zx0";
          string log;
          using ( var form = new ExportCharsetAsBinaryFile( StudioTestHost.CreateCore() ) )
          {
            Assert.IsTrue( RunExport( form, charset, Path.Combine( folder, "charset.bin" ), out log ), log );
          }
          var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
          CollectionAssert.AreEquivalent( new[] { "charset.bin", "charset.zx0" }, written, string.Join( ", ", written ) );

          byte[] raw = RawExport( charset );
          CollectionAssert.AreEqual( raw, File.ReadAllBytes( Path.Combine( folder, "charset.bin" ) ), "the exported file itself stays" );
          byte[] compressed = File.ReadAllBytes( Path.Combine( folder, "charset.zx0" ) );
          Assert.IsTrue( compressed.Length > 0 );
          Assert.IsTrue( compressed.Length < raw.Length, compressed.Length + " bytes is not smaller than the " + raw.Length + " raw bytes" );
          CollectionAssert.AreNotEqual( raw, compressed );
          StringAssert.Contains( log, "Exported " + raw.Length + " bytes to " );
          StringAssert.Contains( log, "Compressed (ZX0): " + raw.Length + " -> " );
        }
        finally
        {
          Directory.Delete( folder, true );
        }
      } );
    }



    [TestMethod]
    public void TestWithCompressionOffOrWithoutANameOnlyTheCharsetFileIsWritten()
    {
      StudioTestHost.RunOnSta( () =>
      {
        foreach ( var compressWithoutName in new[] { false, true } )
        {
          string folder = NewFolder();
          try
          {
            var charset = BuildCharset();
            charset.ExportBinaryCompress         = compressWithoutName;
            charset.ExportBinaryCompressFilename = "";
            string log;
            using ( var form = new ExportCharsetAsBinaryFile( StudioTestHost.CreateCore() ) )
            {
              Assert.IsTrue( RunExport( form, charset, Path.Combine( folder, "charset.bin" ), out log ), log );
            }
            var written = Directory.GetFiles( folder ).Select( Path.GetFileName ).ToArray();
            CollectionAssert.AreEquivalent( new[] { "charset.bin" }, written, string.Join( ", ", written ) );
            CollectionAssert.AreEqual( RawExport( charset ), File.ReadAllBytes( Path.Combine( folder, "charset.bin" ) ) );
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
    public void TestABadOverrideOrACompressedNameEqualToTheExportAbortsBeforeAnythingIsWritten()
    {
      StudioTestHost.RunOnSta( () =>
      {
        foreach ( var badCase in new[] { "override", "same name", "bad name" } )
        {
          string folder = NewFolder();
          try
          {
            var charset = BuildCharset();
            charset.ExportBinaryCompress         = true;
            charset.ExportBinaryCompressFilename = "charset.zx0";
            string expected;
            switch ( badCase )
            {
              case "override":
                charset.ExportBinaryOverrideLoadAddress    = true;
                charset.ExportBinaryOverrideLoadAddressHex = "nope";
                expected = "compressed file load address: \"nope\"";
                break;
              case "same name":
                charset.ExportBinaryCompressFilename = "CHARSET.BIN";
                expected = "equals the exported file name";
                break;
              default:
                charset.ExportBinaryCompressFilename = "char|set.zx0";
                expected = "contains characters a file name cannot have";
                break;
            }
            using ( var form = new QuietForm( StudioTestHost.CreateCore() ) )
            {
              string log;
              Assert.IsFalse( RunExport( form, charset, Path.Combine( folder, "charset.bin" ), out log ), badCase );
              Assert.IsNotNull( form.Aborted, badCase );
              Assert.AreEqual( 1, form.Aborted.Count, string.Join( "\n", form.Aborted ) );
              StringAssert.Contains( form.Aborted[0], expected );
            }
            Assert.AreEqual( 0, Directory.GetFileSystemEntries( folder ).Length, "nothing written for " + badCase );
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
