extern alias studio;

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using StudioCore = studio::RetroDevStudio.StudioCore;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;



namespace TestProject
{
  /// <summary>
  /// Looking around must never mark a map project modified: opening it (and
  /// creating its window handles, as showing the tab does), switching maps
  /// with the map dropdown and flipping through the tabs all leave the
  /// document clean. Runs a REAL MapEditor so every event handler that a
  /// programmatic populate triggers is exercised.
  /// </summary>
  [TestClass]
  public class TestMapEditorStaysClean
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
      var field = Owner.GetType().GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, "MapEditor field '" + Name + "' not found" );
      return field.GetValue( Owner );
    }



    private static void SetSelectedIndex( object Control, int Index )
    {
      var property = Control.GetType().GetProperty( "SelectedIndex" );
      Assert.IsNotNull( property, Control.GetType().Name + " has no SelectedIndex" );
      property.SetValue( Control, Index );
      Application.DoEvents();
    }



    private static int ItemCount( object Control )
    {
      var items = Control.GetType().GetProperty( "Items" ).GetValue( Control );
      return (int)items.GetType().GetProperty( "Count" ).GetValue( items );
    }



    private static void AddTiles( MapProject.MapCharset Charset, int Count, byte CharBase )
    {
      for ( int i = 0; i < Count; ++i )
      {
        var tile = new MapProject.Tile() { Name = "T" + CharBase + "_" + i };
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0].Character = (byte)( CharBase + i );
        tile.Chars[0, 0].Color = (byte)( 1 + i );
        Charset.Tiles.Add( tile );
      }
      Charset.ReindexTiles();
    }



    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex, int Width, int Height )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( Width, Height );
      map.EnsureDefaultLayers();
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Three maps on two charsets, different sizes and alternative colours,
    /// markers and entities - and one map whose remembered marker/entity
    /// placement types no longer exist (a stale ID).
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 2, 0x20 );

      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 0, Name = "Door", TagID = 1 } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 1, Name = "Exit", TagID = 2, Color = 3 } );
      var skeleton = new MapProject.EntityType() { ID = 0, Name = "Skeleton", TagID = 1 };
      project.EntityTypes.Add( skeleton );
      project.Charsets[0].SetEntityTile( skeleton, 1 );
      project.MapStrings.Add( new MapProject.MapString() { Label = "HELLO", StringID = 1 } );

      var alpha = AddMap( project, "Alpha", 0, 4, 3 );
      alpha.SelectedMarkerType = 1;
      alpha.SelectedEntityType = 0;
      alpha.Markers.Add( new MapProject.Marker() { Type = 1, X = 1, Y = 1 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1 } );
      alpha.Tiles[1, 1] = 2;

      var bravo = AddMap( project, "Bravo", second, 6, 5 );
      bravo.AlternativeBackgroundColor = 6;
      bravo.AlternativeMultiColor1 = 2;
      bravo.NotExported = true;
      bravo.SelectedMarkerType = 0;
      bravo.Tiles[0, 0] = 1;

      var charlie = AddMap( project, "Charlie", 0, 3, 3 );
      charlie.SelectedMarkerType = 42;    // a marker type that no longer exists
      charlie.SelectedEntityType = 17;    // likewise for entities

      project.CurrentMapIndex = 0;
      return project;
    }



    private static string WriteProject( MapProject Project )
    {
      string path = Path.Combine( Path.GetTempPath(), "C64StudioStaysClean_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
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
    private static MapEditor OpenEditor( string Path )
    {
      var editor = new MapEditor( StudioTestHost.CreateCore() );
      editor.DocumentInfo.DocumentFilename = Path;
      Assert.IsTrue( editor.LoadDocument(), "the project loads" );
      CreateHandles( editor );
      Application.DoEvents();
      return editor;
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestOpeningTheProjectLeavesItUnmodified()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = WriteProject( BuildProject() );
        try
        {
          using ( var editor = OpenEditor( path ) )
          {
            Assert.IsFalse( editor.Modified, "opening the project (and showing it) must not mark it modified" );
          }
        }
        finally
        {
          DeleteProjectFiles( path );
        }
      } );
    }



    [TestMethod]
    public void TestSwitchingMapsLeavesTheProjectUnmodified()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = WriteProject( BuildProject() );
        try
        {
          using ( var editor = OpenEditor( path ) )
          {
            var comboMaps = Field( editor, "comboMaps" );
            Assert.AreEqual( 3, ItemCount( comboMaps ) );

            // every pair of maps, both directions - other charset, other size,
            // stale placement types, back again
            foreach ( int index in new[] { 1, 2, 0, 2, 1, 0, 1 } )
            {
              SetSelectedIndex( comboMaps, index );
              Assert.IsFalse( editor.Modified, "switching to map " + index + " must not mark the project modified" );
            }

            // the map choice still rides along with the next real save
            var saved = new MapProject();
            Assert.IsTrue( saved.ReadFromBuffer( new GR.Memory.ByteBuffer( editor.SaveToBuffer().Data() ) ) );
            Assert.AreEqual( 1, saved.CurrentMapIndex, "the current map is remembered by the next save" );
          }
        }
        finally
        {
          DeleteProjectFiles( path );
        }
      } );
    }



    [TestMethod]
    public void TestFlippingThroughTheTabsLeavesTheProjectUnmodified()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = WriteProject( BuildProject() );
        try
        {
          using ( var editor = OpenEditor( path ) )
          {
            var tabs = Field( editor, "tabMapEditor" );
            var pages = tabs.GetType().GetProperty( "Pages" ).GetValue( tabs );
            int pageCount = (int)pages.GetType().GetProperty( "Count" ).GetValue( pages );
            Assert.IsTrue( pageCount > 1 );
            for ( int page = 0; page < pageCount; ++page )
            {
              SetSelectedIndex( tabs, page );
              Assert.IsFalse( editor.Modified, "showing tab page " + page + " must not mark the project modified" );
            }
            // and switching maps while on the last tab, then going back to the map tab
            SetSelectedIndex( Field( editor, "comboMaps" ), 1 );
            Assert.IsFalse( editor.Modified, "switching maps from another tab" );
            SetSelectedIndex( tabs, 0 );
            Assert.IsFalse( editor.Modified, "back to the first tab" );
          }
        }
        finally
        {
          DeleteProjectFiles( path );
        }
      } );
    }
  }
}
