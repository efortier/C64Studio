extern alias studio;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;



namespace TestProject
{
  /// <summary>
  /// The Markers tab lists a marker type as its colour swatch (no text on it, a
  /// grey outline around it), its tag in decimal - right-aligned like a number,
  /// header included - then its name and symbol. Runs a REAL MapEditor.
  /// </summary>
  [TestClass]
  public class TestMarkerTypeList
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



    /// <summary>One map on one charset; Door keeps the default colour, Exit and Gate share another.</summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      var tile = new MapProject.Tile() { Name = "Floor" };
      tile.Chars.Resize( 1, 1 );
      project.Charsets[0].Tiles.Add( tile );
      project.Charsets[0].ReindexTiles();

      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 0, Name = "Door", TagID = 1, ExportSymbol = "MARKER_DOOR" } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 1, Name = "Exit", TagID = 12, Color = 3 } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 2, Name = "Gate", TagID = 200, Color = 3 } );

      var map = new MapProject.Map() { Name = "Alpha", CharsetIndex = 0, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 4, 3 );
      map.EnsureDefaultLayers();
      project.Maps.Add( map );
      project.CurrentMapIndex = 0;
      return project;
    }



    /// <summary>Opens the project in a real editor, creates every handle and shows the Markers tab.</summary>
    private static void WithMarkersTab( Action<MapEditor, ListView> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioMarkerList_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, BuildProject().SaveToBuffer().Data() );
        try
        {
          using ( var editor = new MapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();

            var tabs = Field( editor, "tabMapEditor" );
            tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabMarkers" ) );
            Application.DoEvents();
            Body( editor, (ListView)Field( editor, "listMarkerTypes" ) );
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
    public void TestMarkersTabListsSwatchTagNameSymbol()
    {
      WithMarkersTab( ( editor, list ) =>
      {
        var headers = new List<string>();
        foreach ( ColumnHeader column in list.Columns )
        {
          headers.Add( column.Text );
        }
        CollectionAssert.AreEqual( new[] { "", "Tag", "Name", "Symbol" }, headers );
        Assert.AreEqual( HorizontalAlignment.Right, list.Columns[1].TextAlign, "the tag column is right-aligned, header and cells" );

        var rows = new List<string>();
        foreach ( ListViewItem row in list.Items )
        {
          var cells = new List<string>();
          foreach ( ListViewItem.ListViewSubItem cell in row.SubItems )
          {
            cells.Add( cell.Text );
          }
          rows.Add( string.Join( " | ", cells ) );
        }
        CollectionAssert.AreEqual( new[]
        {
          " | 1 | Door | MARKER_DOOR",
          " | 12 | Exit | ",
          " | 200 | Gate | ",
        }, rows );
        Assert.IsFalse( editor.Modified, "showing the list does not modify the project" );
      } );
    }



    [TestMethod]
    public void TestMarkerSwatchIsTheTypeColourInAGreyOutline()
    {
      WithMarkersTab( ( editor, list ) =>
      {
        int outline = Color.Gray.ToArgb();
        var fills = new List<int>();
        foreach ( ListViewItem row in list.Items )
        {
          using ( var swatch = (Bitmap)list.SmallImageList.Images[row.ImageIndex] )
          {
            int fill = swatch.GetPixel( 1, 1 ).ToArgb();
            for ( int y = 0; y < swatch.Height; ++y )
            {
              for ( int x = 0; x < swatch.Width; ++x )
              {
                bool border = ( x == 0 ) || ( y == 0 ) || ( x == swatch.Width - 1 ) || ( y == swatch.Height - 1 );
                Assert.AreEqual( border ? outline : fill, swatch.GetPixel( x, y ).ToArgb(),
                                 "row " + row.Index + ", pixel " + x + "," + y + ": " + ( border ? "the grey outline" : "the plain type colour, no text" ) );
              }
            }
            fills.Add( fill );
          }
        }
        Assert.AreNotEqual( outline, fills[0], "the outline stands out from Door's default white" );
        Assert.AreNotEqual( fills[0], fills[1], "Door and Exit have different colours" );
        Assert.AreEqual( fills[1], fills[2], "Exit and Gate share a colour" );
      } );
    }
  }
}
