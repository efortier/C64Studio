extern alias studio;

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;



namespace TestProject
{
  /// <summary>
  /// The Map tab's map dropdown: refreshing its rows never re-runs the map
  /// switch (re-setting the selected row raises SelectedIndexChanged even
  /// when its text is unchanged), so edits keep the cell selection and the
  /// scratch / revision view - Map not exported, the map's charset, the map
  /// ID, the start map, adding / renaming a charset, and their undos. Runs a
  /// REAL MapEditor with handles created - without a handle the dropdown
  /// never raises that event.
  /// </summary>
  [TestClass]
  public class TestMapDropdownRefresh
  {
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



    private static void SetProperty( object Control, string Property, object Value )
    {
      Control.GetType().GetProperty( Property ).SetValue( Control, Value );
      Application.DoEvents();
    }



    private static bool Viewing( MapEditor Editor, string Flag )
    {
      return (bool)Field( Editor, Flag );
    }



    private static void SelectCell( MapEditor Editor, int X, int Y )
    {
      ( (bool[,])Field( Editor, "m_SelectedTiles" ) )[X, Y] = true;
    }



    private static bool CellSelected( MapEditor Editor, int X, int Y )
    {
      return ( (bool[,])Field( Editor, "m_SelectedTiles" ) )[X, Y];
    }



    private static void RenameEditedCharset( MapEditor Editor, string Name )
    {
      ( (Control)Field( Editor, "editCharsetDisplayName" ) ).Text = Name;
      Invoke( Editor, "CommitCharsetDisplayName" );
    }



    private static MapProject.Map AddMap( MapProject Project, string Name, int MapID )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 3, 2 );
      map.EnsureDefaultLayers();
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>Two map charsets with one tile each; Alpha (ID 1) and Bravo (ID 2) on charset 0.</summary>
    private static MapProject TwoCharsetProject()
    {
      var project = new MapProject();
      project.AddCharset();
      foreach ( var charset in project.Charsets )
      {
        var tile = new MapProject.Tile() { Name = "T0" };
        tile.Chars.Resize( 1, 1 );
        charset.Tiles.Add( tile );
        charset.ReindexTiles();
      }
      AddMap( project, "Alpha", 1 );
      AddMap( project, "Bravo", 2 );
      return project;
    }



    /// <summary>Writes Project to a temp file, opens it in a real editor (handles created, as when the tab shows) and runs Body.</summary>
    private static void WithEditor( MapProject Project, Action<MapEditor, StudioMapProject> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioMapDropdown_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, Project.SaveToBuffer().Data() );
        try
        {
          using ( var editor = new MapEditor( StudioTestHost.CreateCore() ) )
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
    public void TestTogglingMapNotExportedKeepsTheSelection()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        SelectCell( editor, 1, 1 );
        SetProperty( Field( editor, "checkMapNotExported" ), "Checked", true );
        Assert.IsTrue( project.Maps[0].NotExported );
        Assert.IsTrue( editor.Modified );
        Assert.IsTrue( CellSelected( editor, 1, 1 ), "the cell selection survives" );
      } );
    }



    [TestMethod]
    public void TestChangingTheMapsCharsetKeepsTheSelection()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        SelectCell( editor, 1, 1 );
        SetProperty( Field( editor, "comboMapCharset" ), "SelectedIndex", 1 );
        Assert.AreEqual( 1, project.Maps[0].CharsetIndex );
        Assert.AreSame( project.Charsets[1], Field( editor, "m_DisplayedCharset" ), "the map-side lists show the map's new charset" );
        Assert.IsTrue( CellSelected( editor, 1, 1 ), "the cell selection survives" );
      } );
    }



    [TestMethod]
    public void TestCharsetEditsKeepTheScratchView()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        Invoke( editor, "ToggleScratchView" );
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ) );

        RenameEditedCharset( editor, "Renamed" );
        Assert.AreEqual( "Renamed", project.Charsets[project.CurrentCharsetIndex].DisplayName );
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ), "renaming a charset" );

        Invoke( editor, "btnCharsetAdd_Click", new object[] { null } );
        Assert.AreEqual( 3, project.Charsets.Count );
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ), "adding a charset" );

        editor.Undo();
        Application.DoEvents();
        Assert.AreEqual( 2, project.Charsets.Count );
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ), "undoing the add" );
      } );
    }



    [TestMethod]
    public void TestCharsetEditsKeepTheRevisionView()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        Invoke( editor, "btnCreateRevision_Click", null, EventArgs.Empty );
        SetProperty( Field( editor, "comboRevisions" ), "SelectedIndex", 1 );
        Assert.IsTrue( Viewing( editor, "m_IsViewingRevision" ) );

        RenameEditedCharset( editor, "Renamed" );
        Assert.IsTrue( Viewing( editor, "m_IsViewingRevision" ), "renaming a charset" );

        Invoke( editor, "btnCharsetAdd_Click", new object[] { null } );
        Assert.IsTrue( Viewing( editor, "m_IsViewingRevision" ), "adding a charset" );
      } );
    }



    [TestMethod]
    public void TestTypingAMapIDKeepsTheSelection()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        SelectCell( editor, 1, 1 );
        SetProperty( Field( editor, "editMapID" ), "Value", (decimal)7 );
        Assert.AreEqual( 7, project.Maps[0].MapID );
        Assert.IsTrue( CellSelected( editor, 1, 1 ), "the cell selection survives" );
      } );
    }



    [TestMethod]
    public void TestSettingTheStartMapKeepsTheSelection()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        SetProperty( Field( editor, "comboMaps" ), "SelectedIndex", 1 );
        SelectCell( editor, 1, 1 );

        Invoke( editor, "btnSetStartMap_Click", null, EventArgs.Empty );
        Assert.AreEqual( 1, project.StartMapIndex );
        Assert.IsTrue( CellSelected( editor, 1, 1 ), "setting the start map" );

        editor.Undo();
        Application.DoEvents();
        Assert.AreEqual( 0, project.StartMapIndex );
        Assert.IsTrue( CellSelected( editor, 1, 1 ), "undoing it" );
      } );
    }



    /// <summary>UndoMapValueChange resyncs through InvalidateCurrentMap's scratch branch - the row refresh after it must not undo that.</summary>
    [TestMethod]
    public void TestUndoingAScratchEditKeepsTheScratchView()
    {
      WithEditor( TwoCharsetProject(), ( editor, project ) =>
      {
        Invoke( editor, "ToggleScratchView" );
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ) );
        SetProperty( Field( editor, "comboMapBGColor" ), "SelectedIndex", 3 );
        editor.Undo();
        Application.DoEvents();
        Assert.IsTrue( Viewing( editor, "m_IsViewingScratch" ), "undoing a scratch edit" );
      } );
    }
  }
}
