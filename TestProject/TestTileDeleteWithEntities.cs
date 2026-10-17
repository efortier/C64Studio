extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using StudioCore = studio::RetroDevStudio.StudioCore;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;
using Choice = studio::RetroDevStudio.Dialogs.DlgDeleteTileEntities.Choice;



namespace TestProject
{
  /// <summary>
  /// Deleting tiles that placed entities are drawn with asks first (cancel,
  /// delete anyway, delete together with those entities - one undo step),
  /// and the Entities tab lists every type once per charset it is placed in,
  /// with the maps and the summed count. Runs a REAL MapEditor whose
  /// question is answered by the test.
  /// </summary>
  [TestClass]
  public class TestTileDeleteWithEntities
  {
    /// <summary>A MapEditor whose tile-delete question answers itself.</summary>
    private class AnsweringMapEditor : MapEditor
    {
      public Choice                                         Answer = Choice.CANCEL;
      public int                                            Asked = 0;
      public List<int>                                      AskedTiles = null;
      public List<StudioMapProject.EntityCharsetUsage>      AskedPlaced = null;

      public AnsweringMapEditor( StudioCore Core ) :
        base( Core )
      {
      }

      protected override Choice AskDeleteTilesWithEntities( StudioMapProject.MapCharset Charset, List<int> Tiles,
                                                            List<StudioMapProject.EntityCharsetUsage> Placed )
      {
        ++Asked;
        AskedTiles  = new List<int>( Tiles );
        AskedPlaced = Placed;
        return Answer;
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



    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 4, 4 );
      map.EnsureDefaultLayers();
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Charset 0 (4 tiles): Rat on tile 2, Skeleton on tile 3. Charset 1
    /// (3 tiles): Rat on tile 1. Alpha (charset 0): 2 rats + a skeleton,
    /// Bravo (charset 1): a rat, Charlie (charset 0): a rat. Ghost is placed
    /// nowhere.
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 4, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 3, 0x20 );
      project.Charsets[0].DisplayName = "Caves";
      project.Charsets[second].DisplayName = "Crypt";

      var rat      = new MapProject.EntityType() { ID = 0, Name = "Rat", TagID = 1 };
      var skeleton = new MapProject.EntityType() { ID = 1, Name = "Skeleton", TagID = 2 };
      var ghost    = new MapProject.EntityType() { ID = 2, Name = "Ghost", TagID = 3 };
      project.EntityTypes.Add( rat );
      project.EntityTypes.Add( skeleton );
      project.EntityTypes.Add( ghost );
      project.Charsets[0].SetEntityTile( rat, 2 );
      project.Charsets[0].SetEntityTile( skeleton, 3 );
      project.Charsets[second].SetEntityTile( rat, 1 );

      var alpha = AddMap( project, "Alpha", 0 );
      alpha.Entities.Add( new MapProject.Entity() { Type = rat.ID, X = 0, Y = 0 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = rat.ID, X = 1, Y = 0 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = skeleton.ID, X = 2, Y = 0 } );
      var bravo = AddMap( project, "Bravo", second );
      bravo.Entities.Add( new MapProject.Entity() { Type = rat.ID, X = 0, Y = 0 } );
      var charlie = AddMap( project, "Charlie", 0 );
      charlie.Entities.Add( new MapProject.Entity() { Type = rat.ID, X = 3, Y = 3 } );

      project.CurrentMapIndex = 0;
      return project;
    }



    private static void WithEditor( Action<AnsweringMapEditor, StudioMapProject> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioTileDelete_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, BuildProject().SaveToBuffer().Data() );
        try
        {
          using ( var editor = new AnsweringMapEditor( StudioTestHost.CreateCore() ) )
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



    /// <summary>Selects the given rows of the Tiles tab list (charset of the current map) and clicks Delete.</summary>
    private static void DeleteTiles( MapEditor Editor, params int[] Tiles )
    {
      var list = (ListView)Field( Editor, "listTileInfo" );
      list.SelectedIndices.Clear();
      foreach ( int tile in Tiles )
      {
        list.Items[tile].Selected = true;
      }
      Application.DoEvents();
      typeof( MapEditor ).GetMethod( "btnTileDelete_Click", BindingFlags.Instance | BindingFlags.NonPublic ).Invoke( Editor, new object[] { null } );
      Application.DoEvents();
    }



    private static string EntitySummary( StudioMapProject.Map Map )
    {
      var parts = new List<string>();
      foreach ( var entity in Map.Entities )
      {
        parts.Add( entity.Type + "@" + entity.X + "," + entity.Y );
      }
      return string.Join( " ", parts );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestCancelKeepsTheTileAndTheEntities()
    {
      WithEditor( ( editor, project ) =>
      {
        editor.Answer = Choice.CANCEL;
        DeleteTiles( editor, 2 );

        Assert.AreEqual( 1, editor.Asked, "the rats are drawn with tile 2 - ask" );
        Assert.AreEqual( 4, project.Charsets[0].Tiles.Count, "nothing deleted" );
        Assert.AreEqual( 2, project.Charsets[0].EntityTileOf( project.EntityTypes[0] ) );
        Assert.AreEqual( 3, project.Maps[0].Entities.Count );
        Assert.AreEqual( 1, project.Maps[2].Entities.Count );
        Assert.IsFalse( editor.Modified, "a cancelled delete changes nothing" );
      } );
    }



    [TestMethod]
    public void TestTheQuestionListsThePlacedEntitiesOfThatCharsetOnly()
    {
      WithEditor( ( editor, project ) =>
      {
        editor.Answer = Choice.CANCEL;
        DeleteTiles( editor, 2 );

        CollectionAssert.AreEqual( new[] { 2 }, editor.AskedTiles );
        Assert.AreEqual( 1, editor.AskedPlaced.Count, "only the rat is drawn with tile 2" );
        Assert.AreSame( project.EntityTypes[0], editor.AskedPlaced[0].Type );
        Assert.AreEqual( 3, editor.AskedPlaced[0].Count, "2 on Alpha + 1 on Charlie; Bravo's rat is drawn from the other charset" );
        CollectionAssert.AreEqual( new[] { project.Maps[0], project.Maps[2] }, editor.AskedPlaced[0].Maps );
      } );
    }



    [TestMethod]
    public void TestDeleteAnywayKeepsTheEntitiesWithoutATile()
    {
      WithEditor( ( editor, project ) =>
      {
        editor.Answer = Choice.DELETE_TILE_ONLY;
        DeleteTiles( editor, 2 );

        Assert.AreEqual( 3, project.Charsets[0].Tiles.Count );
        Assert.AreEqual( -1, project.Charsets[0].EntityTileOf( project.EntityTypes[0] ), "the rat lost its tile in this charset" );
        Assert.AreEqual( 2, project.Charsets[0].EntityTileOf( project.EntityTypes[1] ), "the skeleton's tile moved down with its picture" );
        Assert.AreEqual( 1, project.Charsets[1].EntityTileOf( project.EntityTypes[0] ), "the other charset keeps its own tile" );
        Assert.AreEqual( 3, project.Maps[0].Entities.Count, "the entities stay" );
        Assert.AreEqual( 1, project.Maps[1].Entities.Count );
        Assert.AreEqual( 1, project.Maps[2].Entities.Count );
        Assert.IsTrue( editor.Modified );
      } );
    }



    [TestMethod]
    public void TestDeleteWithEntitiesIsOneUndoStep()
    {
      WithEditor( ( editor, project ) =>
      {
        string alphaBefore   = EntitySummary( project.Maps[0] );
        string bravoBefore   = EntitySummary( project.Maps[1] );
        string charlieBefore = EntitySummary( project.Maps[2] );

        editor.Answer = Choice.DELETE_TILE_AND_ENTITIES;
        DeleteTiles( editor, 2 );

        Assert.AreEqual( 3, project.Charsets[0].Tiles.Count );
        Assert.AreEqual( "1@2,0", EntitySummary( project.Maps[0] ), "Alpha keeps its skeleton only" );
        Assert.AreEqual( bravoBefore, EntitySummary( project.Maps[1] ), "Bravo is on the other charset - its rat stays" );
        Assert.AreEqual( "", EntitySummary( project.Maps[2] ) );

        editor.DocumentInfo.UndoManager.Undo();
        Assert.AreEqual( 4, project.Charsets[0].Tiles.Count, "one undo brings the tile back..." );
        Assert.AreEqual( 2, project.Charsets[0].EntityTileOf( project.EntityTypes[0] ), "...with the rat's tile..." );
        Assert.AreEqual( alphaBefore, EntitySummary( project.Maps[0] ), "...and the entities" );
        Assert.AreEqual( charlieBefore, EntitySummary( project.Maps[2] ) );
        Assert.AreEqual( 0, editor.DocumentInfo.UndoManager.NumUndos, "it was a single step" );

        editor.DocumentInfo.UndoManager.Redo();
        Assert.AreEqual( 3, project.Charsets[0].Tiles.Count, "redo deletes again" );
        Assert.AreEqual( "1@2,0", EntitySummary( project.Maps[0] ) );
        Assert.AreEqual( "", EntitySummary( project.Maps[2] ) );
      } );
    }



    [TestMethod]
    public void TestATileNoPlacedEntityUsesIsDeletedWithoutAsking()
    {
      WithEditor( ( editor, project ) =>
      {
        DeleteTiles( editor, 0 );

        Assert.AreEqual( 0, editor.Asked );
        Assert.AreEqual( 3, project.Charsets[0].Tiles.Count );
        Assert.AreEqual( 1, project.Charsets[0].EntityTileOf( project.EntityTypes[0] ), "the rat's tile moved down with its picture" );
        Assert.AreEqual( 3, project.Maps[0].Entities.Count );
      } );
    }



    [TestMethod]
    public void TestEntitiesTabListsEveryTypePerCharset()
    {
      WithEditor( ( editor, project ) =>
      {
        var tabs     = Field( editor, "tabMapEditor" );
        var entities = Field( editor, "tabEntities" );
        tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, entities );
        Application.DoEvents();

        var list = (ListView)Field( editor, "listEntityTypes" );
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
          "$01 | Rat |  | 0: Caves | 0: Alpha, 2: Charlie | 3",
          "$01 | Rat |  | 1: Crypt | 1: Bravo | 1",
          "$02 | Skeleton |  | 0: Caves | 0: Alpha | 1",
          "$03 | Ghost |  |  |  | 0",
        }, rows );
        Assert.IsNull( list.SmallImageList, "the tag is plain text - no badge images" );
        Assert.IsFalse( editor.Modified, "showing the list does not modify the project" );
      } );
    }



    [TestMethod]
    public void TestSelectingARowShowsTheTypesTileInThatCharset()
    {
      WithEditor( ( editor, project ) =>
      {
        var tabs = Field( editor, "tabMapEditor" );
        tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabEntities" ) );
        Application.DoEvents();

        var list    = (ListView)Field( editor, "listEntityTypes" );
        var charset = Field( editor, "comboEntityPreviewCharset" );
        var name    = (TextBox)Field( editor, "editEntityName" );

        list.Items[1].Selected = true;                  // Rat in 1: Crypt
        Application.DoEvents();
        Assert.AreEqual( "Rat", name.Text );
        Assert.AreEqual( 1, (int)charset.GetType().GetProperty( "SelectedIndex" ).GetValue( charset ), "the row's charset is shown" );

        list.Items[1].Selected = false;
        list.Items[0].Selected = true;                  // Rat in 0: Caves
        Application.DoEvents();
        Assert.AreEqual( 0, (int)charset.GetType().GetProperty( "SelectedIndex" ).GetValue( charset ) );

        // staged, not committed: a revisit refreshes the list but keeps the edit
        name.Text = "Big Rat";
        tabs.GetType().GetProperty( "SelectedIndex" ).SetValue( tabs, 0 );
        Application.DoEvents();
        tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabEntities" ) );
        Application.DoEvents();
        Assert.AreEqual( "Big Rat", name.Text, "a tab visit must not overwrite a staged edit" );
        Assert.IsTrue( list.Items[0].Selected, "the selected row is kept" );
        Assert.IsFalse( editor.Modified );
      } );
    }
  }
}
