extern alias studio;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;
using StudioCore = studio::RetroDevStudio.StudioCore;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;
using StudioTextCharMode = studio::RetroDevStudio.TextCharMode;
using StudioTextMode = studio::RetroDevStudio.TextMode;



namespace TestProject
{
  /// <summary>
  /// Per-charset modes and standalone charsets in a REAL MapEditor: opening a
  /// project keeps every charset's own mode (no re-stamp, no dirty flag), the
  /// Character Set tab's mode combo is live and changes only the edited
  /// charset, the Map tab's charset dropdown leaves standalone charsets out,
  /// "Standalone charset" is refused while a map uses the set and otherwise
  /// enables the exported filename / export checkbox (undoable), and charset
  /// 0 can never be standalone.
  /// </summary>
  [TestClass]
  public class TestStandaloneCharsetEditor
  {
    /// <summary>A MapEditor whose "cannot make standalone" box records instead of showing.</summary>
    private class RecordingMapEditor : MapEditor
    {
      public int           Refused = 0;
      public List<string>  RefusedUsers = null;

      public RecordingMapEditor( StudioCore Core ) :
        base( Core )
      {
      }

      protected override void ReportStandaloneRefused( string CharsetName, List<string> Users )
      {
        ++Refused;
        RefusedUsers = new List<string>( Users );
      }
    }



    // ---------------------------------------------------------------- harness

    private static void CreateHandles( Control Control )
    {
      var handle = Control.Handle;
      foreach ( Control child in Control.Controls )
      {
        CreateHandles( child );
      }
    }



    private static object Field( object Owner, string Name )
    {
      var field = typeof( MapEditor ).GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, "MapEditor field '" + Name + "' not found" );
      return field.GetValue( Owner );
    }



    private static object FieldOf( object Owner, string Name )
    {
      var field = Owner.GetType().GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, Owner.GetType().Name + " field '" + Name + "' not found" );
      return field.GetValue( Owner );
    }



    private static object Prop( object Control, string Name )
    {
      var property = Control.GetType().GetProperty( Name );
      Assert.IsNotNull( property, Control.GetType().Name + " has no " + Name );
      return property.GetValue( Control );
    }



    private static void SetProp( object Control, string Name, object Value )
    {
      var property = Control.GetType().GetProperty( Name );
      Assert.IsNotNull( property, Control.GetType().Name + " has no " + Name );
      property.SetValue( Control, Value );
      Application.DoEvents();
    }



    private static int ItemCount( object Combo )
    {
      var items = Prop( Combo, "Items" );
      return (int)items.GetType().GetProperty( "Count" ).GetValue( items );
    }



    /// <summary>The index of the first dropdown item whose text starts with Prefix, -1 when none.</summary>
    private static int ItemIndexStartingWith( object Combo, string Prefix )
    {
      int index = 0;
      foreach ( var item in (IEnumerable)Prop( Combo, "Items" ) )
      {
        if ( item.ToString().StartsWith( Prefix ) )
        {
          return index;
        }
        ++index;
      }
      return -1;
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



    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex, int MapID )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, MapID = MapID, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 3, 2 );
      map.EnsureDefaultLayers();
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Project mode multicolor. Charset 0 "Levels" (multicolor, map Alpha),
    /// charset 1 "Caves" (hires, map Bravo), charset 2 "Menu" (ECM,
    /// standalone, "ui"), charset 3 "Spare" (hires, unused map charset). The
    /// Character Set tab does not follow the map.
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      project.Mode = TextMode.COMMODORE_40_X_25_MULTICOLOR;
      project.Charsets[0].SetMode( TextCharMode.COMMODORE_MULTICOLOR );
      project.Charsets[0].DisplayName = "Levels";
      AddTiles( project.Charsets[0], 3, 0x10 );

      int caves = project.AddCharset();
      project.Charsets[caves].SetMode( TextCharMode.COMMODORE_HIRES );
      project.Charsets[caves].DisplayName = "Caves";
      AddTiles( project.Charsets[caves], 2, 0x20 );

      int menu = project.AddCharset();
      project.Charsets[menu].SetMode( TextCharMode.COMMODORE_ECM );
      project.Charsets[menu].DisplayName = "Menu";
      project.Charsets[menu].Standalone  = true;
      project.Charsets[menu].ExportName  = "ui";

      int spare = project.AddCharset();
      project.Charsets[spare].SetMode( TextCharMode.COMMODORE_HIRES );
      project.Charsets[spare].DisplayName = "Spare";

      AddMap( project, "Alpha", 0, 1 );
      AddMap( project, "Bravo", caves, 2 );
      project.CurrentMapIndex       = 0;
      project.CurrentCharsetIndex   = 0;
      project.CharsetTabFollowsMap  = false;
      return project;
    }



    private static string WriteProject( MapProject Project )
    {
      string path = Path.Combine( Path.GetTempPath(), "C64StudioStandaloneEditor_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
      File.WriteAllBytes( path, Project.SaveToBuffer().Data() );
      return path;
    }



    private static void DeleteProjectFiles( string Path )
    {
      foreach ( var file in new[] { Path, System.IO.Path.ChangeExtension( Path, ".mapscratch" ), System.IO.Path.ChangeExtension( Path, ".mapoutlines" ) } )
      {
        try
        {
          if ( File.Exists( file ) )
          {
            File.Delete( file );
          }
        }
        catch ( IOException )
        {
        }
      }
    }



    /// <summary>Opens the file the way the app does, then creates every handle (= the tab being shown).</summary>
    private static RecordingMapEditor OpenEditor( string Path )
    {
      var editor = new RecordingMapEditor( StudioTestHost.CreateCore() );
      editor.DocumentInfo.DocumentFilename = Path;
      Assert.IsTrue( editor.LoadDocument(), "the project loads" );
      CreateHandles( editor );
      Application.DoEvents();
      return editor;
    }



    private static StudioMapProject ModelOf( MapEditor Editor )
    {
      return (StudioMapProject)Field( Editor, "m_MapProject" );
    }



    private static void RunWithEditor( Action<RecordingMapEditor> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = WriteProject( BuildProject() );
        try
        {
          using ( var editor = OpenEditor( path ) )
          {
            Body( editor );
          }
        }
        finally
        {
          DeleteProjectFiles( path );
        }
      } );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestOpeningKeepsEveryCharsetsModeAndStaysClean()
    {
      RunWithEditor( editor =>
      {
        var model = ModelOf( editor );
        Assert.AreEqual( StudioTextMode.COMMODORE_40_X_25_MULTICOLOR, model.Mode );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_MULTICOLOR, model.Charsets[0].Charset.Mode );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_HIRES, model.Charsets[1].Charset.Mode, "no longer stamped with the project mode on load" );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_ECM, model.Charsets[2].Charset.Mode );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_HIRES, model.Charsets[3].Charset.Mode );
        Assert.IsTrue( model.Charsets[2].Standalone );
        Assert.IsFalse( editor.Modified, "opening the project must not mark it modified" );
      } );
    }



    [TestMethod]
    public void TestTheMapTabDropdownLeavesTheStandaloneCharsetOut()
    {
      RunWithEditor( editor =>
      {
        var comboMapCharset = Field( editor, "comboMapCharset" );
        Assert.AreEqual( 4, ItemCount( Field( editor, "comboCharsets" ) ), "the Character Set tab lists every charset" );
        Assert.AreEqual( 4, ItemCount( Field( editor, "comboEntityPreviewCharset" ) ), "so does the Entities tab" );
        Assert.AreEqual( 3, ItemCount( comboMapCharset ), "the Map tab leaves the standalone charset out" );
        Assert.AreEqual( -1, ItemIndexStartingWith( comboMapCharset, "2:" ), "Menu is not offered" );
        Assert.AreEqual( "0: Levels", Prop( comboMapCharset, "SelectedItem" ).ToString(), "Alpha's charset is selected" );

        // binding the current map (Alpha) to Spare through the dropdown
        int spare = ItemIndexStartingWith( comboMapCharset, "3:" );
        Assert.IsTrue( spare >= 0 );
        SetProp( comboMapCharset, "SelectedIndex", spare );
        Assert.AreEqual( 3, ModelOf( editor ).Maps[0].CharsetIndex, "the item's charset index, not its position" );
        Assert.IsTrue( editor.Modified );
      } );
    }



    [TestMethod]
    public void TestStandaloneIsRefusedWhileAMapUsesTheCharset()
    {
      RunWithEditor( editor =>
      {
        SetProp( Field( editor, "comboCharsets" ), "SelectedIndex", 1 );   // Caves, used by Bravo
        var check = Field( editor, "checkCharsetStandalone" );
        Assert.IsTrue( (bool)Prop( check, "Enabled" ) );
        Assert.IsFalse( (bool)Prop( check, "Checked" ) );

        SetProp( check, "Checked", true );
        Assert.AreEqual( 1, editor.Refused, "refused with the maps that use it" );
        Assert.IsTrue( editor.RefusedUsers.Exists( u => u.Contains( "Bravo" ) ), string.Join( ", ", editor.RefusedUsers ) );
        Assert.IsFalse( ModelOf( editor ).Charsets[1].Standalone );
        Assert.IsFalse( (bool)Prop( check, "Checked" ), "the checkbox is put back" );
        Assert.IsFalse( editor.Modified, "a refusal changes nothing" );
      } );
    }



    [TestMethod]
    public void TestTickingStandaloneOnAnUnusedCharsetEnablesTheExportControlsAndUndoes()
    {
      RunWithEditor( editor =>
      {
        SetProp( Field( editor, "comboCharsets" ), "SelectedIndex", 3 );   // Spare, unused
        var check    = Field( editor, "checkCharsetStandalone" );
        var filename = Field( editor, "editCharsetExportName" );
        var export   = Field( editor, "checkCharsetExport" );
        var dropdown = Field( editor, "comboMapCharset" );
        Assert.IsFalse( (bool)Prop( filename, "Enabled" ), "a map charset has no exported filename" );
        Assert.IsFalse( (bool)Prop( export, "Enabled" ) );
        Assert.AreEqual( 3, ItemCount( dropdown ) );

        SetProp( check, "Checked", true );
        Assert.AreEqual( 0, editor.Refused );
        Assert.IsTrue( ModelOf( editor ).Charsets[3].Standalone );
        Assert.IsTrue( (bool)Prop( filename, "Enabled" ), "the exported filename belongs to a standalone charset" );
        Assert.IsTrue( (bool)Prop( export, "Enabled" ) );
        Assert.AreEqual( 2, ItemCount( dropdown ), "the Map tab no longer offers it" );
        Assert.IsTrue( editor.Modified );

        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        Assert.IsFalse( ModelOf( editor ).Charsets[3].Standalone, "undone" );
        Assert.IsFalse( (bool)Prop( check, "Checked" ) );
        Assert.IsFalse( (bool)Prop( filename, "Enabled" ) );
        Assert.AreEqual( 3, ItemCount( dropdown ) );
      } );
    }



    [TestMethod]
    public void TestChangingTheModeOnTheTabChangesOnlyThatCharset()
    {
      RunWithEditor( editor =>
      {
        SetProp( Field( editor, "comboCharsets" ), "SelectedIndex", 1 );   // Caves, hires
        var characterEditor = Field( editor, "characterEditor" );
        var modeCombo = FieldOf( characterEditor, "comboCharsetMode" );
        Assert.IsTrue( (bool)Prop( modeCombo, "Enabled" ), "the mode combo is live in the map editor" );
        Assert.AreEqual( (int)StudioTextCharMode.COMMODORE_HIRES, (int)Prop( modeCombo, "SelectedIndex" ) );

        SetProp( modeCombo, "SelectedIndex", (int)StudioTextCharMode.COMMODORE_MULTICOLOR );
        var model = ModelOf( editor );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_MULTICOLOR, model.Charsets[1].Charset.Mode, "the edited charset" );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_MULTICOLOR, model.Charsets[0].Charset.Mode, "untouched" );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_ECM, model.Charsets[2].Charset.Mode, "untouched" );
        Assert.AreEqual( StudioTextCharMode.COMMODORE_HIRES, model.Charsets[3].Charset.Mode, "untouched" );
        Assert.AreEqual( StudioTextMode.COMMODORE_40_X_25_MULTICOLOR, model.Mode, "the project mode is not the charset's" );
        Assert.IsTrue( editor.Modified );

        // and the saved file carries it
        var saved = new MapProject();
        Assert.IsTrue( saved.ReadFromBuffer( new GR.Memory.ByteBuffer( editor.SaveToBuffer().Data() ) ) );
        Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, saved.Charsets[1].Charset.Mode );
        Assert.AreEqual( TextCharMode.COMMODORE_ECM, saved.Charsets[2].Charset.Mode );
      } );
    }



    [TestMethod]
    public void TestCharsetZeroCanNeverBeStandalone()
    {
      RunWithEditor( editor =>
      {
        SetProp( Field( editor, "comboCharsets" ), "SelectedIndex", 0 );
        Assert.IsFalse( (bool)Prop( Field( editor, "checkCharsetStandalone" ), "Enabled" ), "charset 0 is every map's fallback" );
        Assert.IsFalse( (bool)Prop( Field( editor, "editCharsetExportName" ), "Enabled" ) );
        Assert.IsFalse( (bool)Prop( Field( editor, "checkCharsetExport" ), "Enabled" ) );

        SetProp( Field( editor, "comboCharsets" ), "SelectedIndex", 2 );   // Menu, standalone
        Assert.IsTrue( (bool)Prop( Field( editor, "checkCharsetStandalone" ), "Checked" ) );
        Assert.IsTrue( (bool)Prop( Field( editor, "editCharsetExportName" ), "Enabled" ) );
        Assert.AreEqual( "ui", Prop( Field( editor, "editCharsetExportName" ), "Text" ) );
        Assert.IsFalse( editor.Modified, "looking around never dirties" );
      } );
    }
  }
}
