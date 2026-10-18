extern alias studio;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// The Map ID row of the Map tab's Map details: it shows the selected map's
  /// ID, typing one renumbers the map (undoable, marks the project modified),
  /// Find next map ID picks the lowest free ID from 1 (an error when none is
  /// free), "(in use)" flags an ID another map has, Add and Cpy give the new
  /// map a free ID, an undone delete brings the map back with its own ID,
  /// the map dropdown rows start with the ID, and the row is locked on the
  /// scratch workspace and on revisions. Runs a REAL MapEditor.
  /// </summary>
  [TestClass]
  public class TestMapIDEditor
  {
    /// <summary>A MapEditor that counts the no-free-ID error instead of showing it.</summary>
    private class CountingMapEditor : MapEditor
    {
      public int                    NoFreeMapIDReported = 0;

      public CountingMapEditor( StudioCore Core ) :
        base( Core )
      {
      }

      protected override void ReportNoFreeMapID()
      {
        ++NoFreeMapIDReported;
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



    private static void Invoke( MapEditor Editor, string Method, params object[] Arguments )
    {
      var method = typeof( MapEditor ).GetMethod( Method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( method, "MapEditor method '" + Method + "' not found" );
      method.Invoke( Editor, Arguments );
      Application.DoEvents();
    }



    private static void Click( MapEditor Editor, string Handler )
    {
      Invoke( Editor, Handler, null, EventArgs.Empty );
    }



    private static void SetSelectedIndex( object Control, int Index )
    {
      Control.GetType().GetProperty( "SelectedIndex" ).SetValue( Control, Index );
      Application.DoEvents();
    }



    private static void TypeMapID( MapEditor Editor, int Value )
    {
      var spinner = Field( Editor, "editMapID" );
      spinner.GetType().GetProperty( "Value" ).SetValue( spinner, (decimal)Value );
      Application.DoEvents();
    }



    private static int ShownMapID( MapEditor Editor )
    {
      var spinner = Field( Editor, "editMapID" );
      return (int)(decimal)spinner.GetType().GetProperty( "Value" ).GetValue( spinner );
    }



    private static bool IsEnabled( MapEditor Editor, string Control )
    {
      return ( (Control)Field( Editor, Control ) ).Enabled;
    }



    /// <summary>The "(in use)" label's tooltip: names the other maps with the ID, empty when there are none.</summary>
    private static string InUseTip( MapEditor Editor )
    {
      var tip = (ToolTip)Field( Editor, "toolTip1" );
      return tip.GetToolTip( (Control)Field( Editor, "labelMapIDInUse" ) );
    }



    private static string ButtonText( MapEditor Editor, string Button )
    {
      var button = Field( Editor, Button );
      var values = button.GetType().GetProperty( "Values" ).GetValue( button );
      return (string)values.GetType().GetProperty( "Text" ).GetValue( values );
    }



    private static string Row( MapEditor Editor, int Index )
    {
      var combo = Field( Editor, "comboMaps" );
      var items = combo.GetType().GetProperty( "Items" ).GetValue( combo );
      var item = items.GetType().GetProperty( "Item" ).GetValue( items, new object[] { Index } );
      return item.ToString();
    }



    private static int[] IDs( StudioMapProject Project )
    {
      return Project.Maps.Select( m => m.MapID ).ToArray();
    }



    private static MapProject.Map AddMap( MapProject Project, string Name, int MapID, bool NotExported = false )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, NotExported = NotExported, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 3, 2 );
      map.EnsureDefaultLayers();
      Project.Maps.Add( map );
      return map;
    }



    private static MapProject OneTileProject()
    {
      var project = new MapProject();
      var tile = new MapProject.Tile() { Name = "T0" };
      tile.Chars.Resize( 1, 1 );
      project.Charsets[0].Tiles.Add( tile );
      project.Charsets[0].ReindexTiles();
      return project;
    }



    /// <summary>Alpha (ID 5), Bravo (ID 1), Charlie (ID 2) on one charset.</summary>
    private static MapProject NumberedProject()
    {
      var project = OneTileProject();
      AddMap( project, "Alpha", 5 );
      AddMap( project, "Bravo", 1 );
      AddMap( project, "Charlie", 2 );
      return project;
    }



    /// <summary>Writes Project to a temp file, opens it in a real editor (handles created, as when the tab shows) and runs Body.</summary>
    private static void WithEditor( MapProject Project, Action<MapEditor, StudioMapProject> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioMapID_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, Project.SaveToBuffer().Data() );
        try
        {
          using ( var editor = new CountingMapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();
            Body( editor, (StudioMapProject)Field( editor, "m_MapProject" ) );
          }
        }
        finally
        {
          foreach ( var file in new[] { path, Path.ChangeExtension( path, ".mapscratch" ), Path.ChangeExtension( path, ".mapoutlines" ) } )
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
      } );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestAProjectWithoutIDsOpensNumberedAndUnmodified()
    {
      var legacy = OneTileProject();
      AddMap( legacy, "Alpha", -1 );
      AddMap( legacy, "Bravo", -1, true );
      AddMap( legacy, "Charlie", -1 );

      WithEditor( legacy, ( editor, project ) =>
      {
        CollectionAssert.AreEqual( new[] { 0, 2, 1 }, IDs( project ), "exported positions first, then the lowest free from 1" );
        Assert.IsFalse( editor.Modified, "numbering an older project on load is not an edit" );

        Assert.AreEqual( "★ 0: Alpha", Row( editor, 0 ) );
        Assert.AreEqual( "2: Bravo (not exported)", Row( editor, 1 ) );
        Assert.AreEqual( "1: Charlie", Row( editor, 2 ), "the dropdown shows the ID, not the position" );

        Assert.AreEqual( 0, ShownMapID( editor ) );
        SetSelectedIndex( Field( editor, "comboMaps" ), 1 );
        Assert.AreEqual( 2, ShownMapID( editor ) );
        SetSelectedIndex( Field( editor, "comboMaps" ), 2 );
        Assert.AreEqual( 1, ShownMapID( editor ) );
        Assert.IsFalse( editor.Modified, "switching maps only shows their IDs" );
      } );
    }



    [TestMethod]
    public void TestTypingAnIDRenumbersTheMapAndUndoRestoresIt()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        Assert.AreEqual( 5, ShownMapID( editor ) );

        TypeMapID( editor, 42 );
        Assert.AreEqual( 42, project.Maps[0].MapID );
        Assert.IsTrue( editor.Modified );
        Assert.AreEqual( "★ 42: Alpha", Row( editor, 0 ) );

        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        Assert.AreEqual( 5, project.Maps[0].MapID );
        Assert.AreEqual( 5, ShownMapID( editor ) );
        Assert.AreEqual( "★ 5: Alpha", Row( editor, 0 ) );

        editor.DocumentInfo.UndoManager.Redo();
        Application.DoEvents();
        Assert.AreEqual( 42, project.Maps[0].MapID );
        Assert.AreEqual( 42, ShownMapID( editor ) );
      } );
    }



    [TestMethod]
    public void TestFindNextIDPicksTheLowestFreeIDFrom1()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        Assert.AreEqual( "Find next map ID", ButtonText( editor, "btnFindNextMapID" ) );

        Click( editor, "btnFindNextMapID_Click" );
        Assert.AreEqual( 3, project.Maps[0].MapID, "1 and 2 are taken" );
        Assert.AreEqual( 3, ShownMapID( editor ) );
        Assert.IsTrue( editor.Modified );
        Assert.AreEqual( 1, editor.DocumentInfo.UndoManager.NumUndos );

        Click( editor, "btnFindNextMapID_Click" );
        Assert.AreEqual( 3, project.Maps[0].MapID, "it already holds the lowest free ID" );
        Assert.AreEqual( 1, editor.DocumentInfo.UndoManager.NumUndos, "nothing changed, nothing to undo" );

        SetSelectedIndex( Field( editor, "comboMaps" ), 1 );
        Click( editor, "btnFindNextMapID_Click" );
        Assert.AreEqual( 1, project.Maps[1].MapID, "Bravo keeps 1: its own ID does not count as taken" );
        Assert.AreEqual( 1, editor.DocumentInfo.UndoManager.NumUndos );
      } );
    }



    [TestMethod]
    public void TestFindNextMapIDReportsAnErrorWhenNoIDIsFree()
    {
      var full = OneTileProject();
      for ( int id = 0; id <= 255; ++id )
      {
        AddMap( full, "Map" + id, id );
      }
      WithEditor( full, ( editor, project ) =>
      {
        Click( editor, "btnFindNextMapID_Click" );
        Assert.AreEqual( 1, ( (CountingMapEditor)editor ).NoFreeMapIDReported, "every ID from 1 to 255 is taken" );
        Assert.AreEqual( 0, project.Maps[0].MapID, "the map keeps its ID" );
        Assert.IsFalse( editor.Modified );
        Assert.AreEqual( 0, editor.DocumentInfo.UndoManager.NumUndos );
      } );
    }



    [TestMethod]
    public void TestAnIDAnotherMapHasIsMarkedInUse()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        Assert.AreEqual( "", InUseTip( editor ), "Alpha's 5 is its own" );

        TypeMapID( editor, 1 );
        StringAssert.Contains( InUseTip( editor ), "'Bravo'", "Bravo has 1 too" );

        SetSelectedIndex( Field( editor, "comboMaps" ), 1 );
        StringAssert.Contains( InUseTip( editor ), "'Alpha'", "Bravo's view names Alpha" );

        SetSelectedIndex( Field( editor, "comboMaps" ), 2 );
        Assert.AreEqual( "", InUseTip( editor ), "Charlie's 2 is its own" );

        SetSelectedIndex( Field( editor, "comboMaps" ), 0 );
        StringAssert.Contains( InUseTip( editor ), "'Bravo'" );
        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        Assert.AreEqual( 5, project.Maps[0].MapID );
        Assert.AreEqual( "", InUseTip( editor ), "undo clears it" );
      } );
    }



    [TestMethod]
    public void TestAddAndCpyGiveTheNewMapAFreeID()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        Click( editor, "btnMapAdd_Click" );
        Assert.AreEqual( 4, project.Maps.Count );
        Assert.AreEqual( 3, project.Maps[3].MapID );

        SetSelectedIndex( Field( editor, "comboMaps" ), 0 );
        Click( editor, "btnMapCopy_Click" );
        Assert.AreEqual( 5, project.Maps.Count );
        Assert.AreEqual( "Alpha", project.Maps[4].Name );
        Assert.AreEqual( 4, project.Maps[4].MapID, "the copy is a new map with its own number" );
        Assert.AreEqual( 5, project.Maps[0].MapID, "the original keeps its number" );
      } );
    }



    [TestMethod]
    public void TestAnUndoneDeleteBringsTheMapBackWithItsID()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        SetSelectedIndex( Field( editor, "comboMaps" ), 1 );
        Click( editor, "btnMapDelete_Click" );
        CollectionAssert.AreEqual( new[] { 5, 2 }, IDs( project ) );

        Click( editor, "btnMapAdd_Click" );
        CollectionAssert.AreEqual( new[] { 5, 2, 1 }, IDs( project ), "a deleted map's ID is free again" );

        editor.DocumentInfo.UndoManager.Undo();
        editor.DocumentInfo.UndoManager.Undo();
        Application.DoEvents();
        Assert.AreEqual( "Bravo", project.Maps[1].Name );
        CollectionAssert.AreEqual( new[] { 5, 1, 2 }, IDs( project ) );
      } );
    }



    [TestMethod]
    public void TestTheIDRowIsLockedOnTheScratchMapAndOnRevisions()
    {
      WithEditor( NumberedProject(), ( editor, project ) =>
      {
        Assert.IsTrue( IsEnabled( editor, "editMapID" ) );
        Assert.IsTrue( IsEnabled( editor, "btnFindNextMapID" ) );

        Invoke( editor, "ToggleScratchView" );
        Assert.IsFalse( IsEnabled( editor, "editMapID" ), "the workspace is not a map of the game" );
        Assert.IsFalse( IsEnabled( editor, "btnFindNextMapID" ) );
        Assert.AreEqual( 5, ShownMapID( editor ), "it shows its owner's ID" );

        Invoke( editor, "ToggleScratchView" );
        Assert.IsTrue( IsEnabled( editor, "editMapID" ) );
        Assert.IsTrue( IsEnabled( editor, "btnFindNextMapID" ) );

        Click( editor, "btnCreateRevision_Click" );
        var revisions = Field( editor, "comboRevisions" );
        SetSelectedIndex( revisions, 1 );
        Assert.IsFalse( IsEnabled( editor, "editMapID" ), "revisions are read-only" );
        Assert.IsFalse( IsEnabled( editor, "btnFindNextMapID" ) );
        Assert.AreEqual( 5, ShownMapID( editor ) );

        SetSelectedIndex( revisions, 0 );
        Assert.IsTrue( IsEnabled( editor, "editMapID" ) );
        Assert.AreEqual( 5, project.Maps[0].MapID );
      } );
    }
  }
}
