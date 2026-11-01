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
using CharacterEditor = studio::RetroDevStudio.Controls.CharacterEditor;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;
using Choice = studio::RetroDevStudio.Dialogs.DlgDeleteTileEntities.Choice;



namespace TestProject
{
  /// <summary>
  /// "Clear character and tile" on the Character Set tab: enabled while the
  /// current character is part of a tile; clears the characters of that tile
  /// and deletes it as ONE undo step (map cells included); asks first only
  /// when more than one tile goes or a tile that stays loses a non-blank
  /// character; entities drawn with the tile get the Tiles tab's question.
  /// Without tiles (a charset project) it shows, disabled. The tab's usage
  /// labels count the selected map only and follow tile and map changes
  /// made while the tab is open. Runs a REAL MapEditor whose questions are
  /// answered by the test.
  /// </summary>
  [TestClass]
  public class TestClearCharacterAndTile
  {
    /// <summary>A MapEditor whose questions answer themselves.</summary>
    private class AnsweringMapEditor : MapEditor
    {
      public bool                               Confirm = false;
      public int                                Confirmed = 0;
      public List<int>                          ConfirmedTiles = null;
      public List<int>                          ConfirmedCharacters = null;
      public SortedDictionary<int, List<int>>   ConfirmedLosing = null;
      public Choice                             EntityAnswer = Choice.CANCEL;
      public int                                AskedEntities = 0;

      public AnsweringMapEditor( StudioCore Core ) :
        base( Core )
      {
      }

      protected override bool ConfirmClearCharactersAndTiles( StudioMapProject.MapCharset Charset, int Character, List<int> Tiles,
                                                              List<int> Characters, SortedDictionary<int, List<int>> Losing )
      {
        ++Confirmed;
        ConfirmedTiles      = new List<int>( Tiles );
        ConfirmedCharacters = new List<int>( Characters );
        ConfirmedLosing     = Losing;
        return Confirm;
      }

      protected override Choice AskDeleteTilesWithEntities( StudioMapProject.MapCharset Charset, List<int> Tiles,
                                                            List<StudioMapProject.EntityCharsetUsage> Placed )
      {
        ++AskedEntities;
        return EntityAnswer;
      }
    }



    // Tiles of charset 0 (see BuildProject).
    private const int EMPTY       = 0;    // blank character 32
    private const int SKELETON    = 1;    // 2x2: 100 101 / 116 117
    private const int WALL        = 2;    // 120
    private const int WALL_TORCH  = 3;    // 120 121 - shares 120 with Wall
    private const int GRAVE       = 4;    // 130 32  - shares the blank 32 with Empty
    private const int BLANK       = 32;



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



    private static object EditorField( CharacterEditor Editor, string Name )
    {
      var field = typeof( CharacterEditor ).GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, "CharacterEditor field '" + Name + "' not found" );
      return field.GetValue( Editor );
    }



    /// <summary>Every character but the blank one shows a picture of its own.</summary>
    private static byte Picture( int Character )
    {
      return (byte)( 0x80 | ( Character & 0x7f ) );
    }



    private static MapProject.Tile MakeTile( string Name, int Width, params int[] Characters )
    {
      var tile = new MapProject.Tile() { Name = Name };
      tile.Chars.Resize( Width, Characters.Length / Width );
      for ( int i = 0; i < Characters.Length; ++i )
      {
        tile.Chars[i % Width, i / Width].Character = (byte)Characters[i];
        tile.Chars[i % Width, i / Width].Color = 1;
      }
      return tile;
    }



    /// <summary>
    /// Charset 0: Empty, Skeleton, Wall, Wall torch, Grave (see the
    /// constants); charset 1: no tiles, no map. Alpha (charset 0, selected)
    /// places Skeleton on the background and on layer 1, Grave, Wall torch
    /// and Wall; Gamma (charset 0) places Skeleton twice. WithEntity adds
    /// Bones, drawn with Skeleton, on Alpha.
    /// </summary>
    private static MapProject BuildProject( bool WithEntity )
    {
      var project = new MapProject();
      var charset = project.Charsets[0];
      for ( int c = 0; c < charset.Charset.Characters.Count; ++c )
      {
        var data = charset.Charset.Characters[c].Tile.Data;
        for ( int j = 0; j < (int)data.Length; ++j )
        {
          data.SetU8At( j, ( c == BLANK ) ? (byte)0 : Picture( c ) );
        }
      }
      charset.Tiles.Add( MakeTile( "Empty", 1, BLANK ) );
      charset.Tiles.Add( MakeTile( "Skeleton", 2, 100, 101, 116, 117 ) );
      charset.Tiles.Add( MakeTile( "Wall", 1, 120 ) );
      charset.Tiles.Add( MakeTile( "Wall torch", 2, 120, 121 ) );
      charset.Tiles.Add( MakeTile( "Grave", 2, 130, BLANK ) );
      charset.ReindexTiles();

      var alpha = new MapProject.Map() { Name = "Alpha", CharsetIndex = 0, TileSpacingX = 1, TileSpacingY = 1 };
      alpha.Tiles.Resize( 4, 3 );
      alpha.EnsureDefaultLayers();
      alpha.Tiles[1, 1] = SKELETON;
      alpha.Tiles[2, 1] = GRAVE;
      alpha.Tiles[0, 2] = WALL_TORCH;
      alpha.Tiles[3, 2] = WALL;
      alpha.Layers[1].Tiles[3, 0] = SKELETON;
      project.Maps.Add( alpha );

      var gamma = new MapProject.Map() { Name = "Gamma", CharsetIndex = 0, TileSpacingX = 1, TileSpacingY = 1 };
      gamma.Tiles.Resize( 2, 1 );
      gamma.EnsureDefaultLayers();
      gamma.Tiles[0, 0] = SKELETON;
      gamma.Tiles[1, 0] = SKELETON;
      project.Maps.Add( gamma );
      project.AddCharset();

      if ( WithEntity )
      {
        var bones = new MapProject.EntityType() { ID = 0, Name = "Bones", TagID = 1 };
        project.EntityTypes.Add( bones );
        charset.SetEntityTile( bones, SKELETON );
        alpha.Entities.Add( new MapProject.Entity() { Type = bones.ID, X = 1, Y = 1 } );
      }
      project.CurrentMapIndex = 0;
      return project;
    }



    /// <summary>Opens the project in a real editor, creates every handle and shows the Character Set tab.</summary>
    private static void WithEditor( bool WithEntity, Action<AnsweringMapEditor, StudioMapProject, CharacterEditor> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioClearCharTile_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, BuildProject( WithEntity ).SaveToBuffer().Data() );
        try
        {
          using ( var editor = new AnsweringMapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();
            var tabs = Field( editor, "tabMapEditor" );
            tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabCharset" ) );
            Application.DoEvents();
            Body( editor, (StudioMapProject)Field( editor, "m_MapProject" ), (CharacterEditor)Field( editor, "characterEditor" ) );
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



    private static void SelectCharacter( CharacterEditor Editor, int Character )
    {
      var panel = EditorField( Editor, "panelCharacters" );
      panel.GetType().GetProperty( "SelectedIndex" ).SetValue( panel, Character );
      Application.DoEvents();
      Assert.AreEqual( Character, Editor.CurrentCharIndex );
    }



    private static Control ClearButton( CharacterEditor Editor )
    {
      return (Control)EditorField( Editor, "btnClearCharacterAndTile" );
    }



    private static void ClickClear( CharacterEditor Editor )
    {
      typeof( CharacterEditor ).GetMethod( "btnClearCharacterAndTile_Click", BindingFlags.Instance | BindingFlags.NonPublic ).Invoke( Editor, new object[] { null } );
      Application.DoEvents();
    }



    private static string TileNames( StudioMapProject Project )
    {
      var names = new List<string>();
      foreach ( var tile in Project.Charsets[0].Tiles )
      {
        names.Add( tile.Name );
      }
      return string.Join( ", ", names );
    }



    /// <summary>"P" for a character still showing its picture, "-" for a blank one, "?" otherwise.</summary>
    private static string Pictures( StudioMapProject Project, params int[] Characters )
    {
      var result = "";
      foreach ( int c in Characters )
      {
        var data = Project.Charsets[0].Charset.Characters[c].Tile.Data;
        bool picture = true;
        bool blank = true;
        for ( int j = 0; j < (int)data.Length; ++j )
        {
          picture &= ( data.ByteAt( j ) == Picture( c ) );
          blank   &= ( data.ByteAt( j ) == 0 );
        }
        result += picture ? "P" : ( blank ? "-" : "?" );
      }
      return result;
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestTheButtonSitsUnderClearInsideTheNamedGroup()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        var group = (GroupBox)EditorField( characters, "groupCharacters" );
        Assert.AreEqual( "Characters", group.Text );

        var clear  = (Control)EditorField( characters, "btnClearChars" );
        var button = ClearButton( characters );
        Assert.AreSame( group, button.Parent );
        Assert.AreEqual( "Clear character and tile", button.Text );
        Assert.AreEqual( clear.Left, button.Left, "lined up with Clear..." );
        Assert.IsTrue( button.Top > clear.Bottom, "...right under it" );

        // Every control of the group lies inside it (Create Multiple Tiles
        // used to stick out on the right) and none overlaps another.
        var children = new List<Control>();
        foreach ( Control child in group.Controls )
        {
          children.Add( child );
        }
        foreach ( var child in children )
        {
          Assert.IsTrue( ( child.Left >= 0 ) && ( child.Right <= group.Width - 6 ) && ( child.Bottom <= group.Height - 6 ),
                         child.Name + " " + child.Bounds + " lies inside the group " + group.Size );
          foreach ( var other in children )
          {
            Assert.IsTrue( ( child == other ) || ( !child.Bounds.IntersectsWith( other.Bounds ) ), child.Name + " overlaps " + other.Name );
          }
        }
      } );
    }



    [TestMethod]
    public void TestWithoutTilesTheButtonShowsDisabled()
    {
      StudioTestHost.RunOnSta( () =>
      {
        // A charset project's character editor: no host, no tiles.
        using ( var characters = new CharacterEditor( StudioTestHost.CreateCore() ) )
        {
          CreateHandles( characters );
          Assert.IsFalse( ( (Control)EditorField( characters, "btnCreateTile" ) ).Visible, "the tile buttons stay hidden..." );
          Assert.IsTrue( ClearButton( characters ).Visible, "...but Clear character and tile shows: no empty row under Clear" );
          Assert.IsFalse( ClearButton( characters ).Enabled );
        }
      } );
    }



    [TestMethod]
    public void TestUsageInMapsCountsTheSelectedMapOnly()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        SelectCharacter( characters, 100 );
        Assert.AreEqual( "Usage in tiles: 1", characters.CharacterUsageText );
        Assert.AreEqual( "Usage in selected map: 2", characters.CharacterMapUsageText, "Alpha's background and layer 1 - not Gamma's two" );

        typeof( MapEditor ).GetMethod( "SelectCharsetOnTab", BindingFlags.Instance | BindingFlags.NonPublic ).Invoke( editor, new object[] { 1 } );
        Application.DoEvents();
        Assert.AreEqual( "Usage in tiles: 0", characters.CharacterUsageText );
        Assert.AreEqual( "Selected map uses another charset", characters.CharacterMapUsageText );
        Assert.IsFalse( editor.Modified );
      } );
    }



    [TestMethod]
    public void TestAProjectSavedOnTheCharacterSetTabOpensWithItsUsage()
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioClearCharTile_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        try
        {
          using ( var editor = new AnsweringMapEditor( StudioTestHost.CreateCore() ) )
          {
            var tabs  = Field( editor, "tabMapEditor" );
            var pages = (System.Collections.IList)tabs.GetType().GetProperty( "Pages" ).GetValue( tabs );
            var project = BuildProject( false );
            project.LastSelectedTabIndex = pages.IndexOf( Field( editor, "tabCharset" ) );
            // Character 0 - the current one after opening - in a tile placed twice on Alpha.
            project.Charsets[0].Tiles.Add( MakeTile( "Zero", 1, 0 ) );
            project.Charsets[0].ReindexTiles();
            project.Maps[0].Tiles[0, 0] = 5;
            project.Maps[0].Tiles[1, 0] = 5;
            File.WriteAllBytes( path, project.SaveToBuffer().Data() );

            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();
            Assert.AreSame( Field( editor, "tabCharset" ), tabs.GetType().GetProperty( "SelectedPage" ).GetValue( tabs ), "it opens on the Character Set tab" );

            var characters = (CharacterEditor)Field( editor, "characterEditor" );
            Assert.AreEqual( 0, characters.CurrentCharIndex );
            Assert.AreEqual( "Usage in tiles: 1", characters.CharacterUsageText );
            Assert.AreEqual( "Usage in selected map: 2", characters.CharacterMapUsageText, "counted on the map the project opened on" );
            Assert.IsTrue( ClearButton( characters ).Enabled );
            Assert.IsFalse( editor.Modified );
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



    [TestMethod]
    public void TestUsageFollowsChangesWhileTheTabIsOpen()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        var alpha = project.Maps[0];
        var tabs  = Field( editor, "tabMapEditor" );
        SelectCharacter( characters, 100 );

        // the tile goes and comes back
        ClickClear( characters );
        Assert.AreEqual( "Usage in tiles: 0", characters.CharacterUsageText );
        Assert.AreEqual( "Usage in selected map: 0", characters.CharacterMapUsageText );
        editor.Undo();
        Assert.AreEqual( "Usage in tiles: 1", characters.CharacterUsageText );
        Assert.AreEqual( "Usage in selected map: 2", characters.CharacterMapUsageText );

        // a cell painted on the Map tab, then undone from the Character Set tab
        tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabEditor" ) );
        Application.DoEvents();
        editor.DocumentInfo.UndoManager.AddUndoTask( new studio::RetroDevStudio.Undo.UndoMapTilesChange( editor, alpha, 0, 0, 1, 1 ) );
        alpha.Tiles[0, 0] = SKELETON;
        tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabCharset" ) );
        Application.DoEvents();
        Assert.AreEqual( "Usage in selected map: 3", characters.CharacterMapUsageText, "opening the tab counts the new cell" );
        editor.Undo();
        Assert.AreEqual( "Usage in selected map: 2", characters.CharacterMapUsageText, "an undo on this tab recounts" );
        editor.Redo();
        Assert.AreEqual( "Usage in selected map: 3", characters.CharacterMapUsageText, "so does a redo" );
      } );
    }



    [TestMethod]
    public void TestEnabledWhileTheCharacterIsPartOfATile()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        SelectCharacter( characters, 116 );
        Assert.IsTrue( ClearButton( characters ).Enabled, "116 is part of Skeleton" );
        SelectCharacter( characters, 200 );
        Assert.IsFalse( ClearButton( characters ).Enabled, "no tile draws 200" );
        SelectCharacter( characters, BLANK );
        Assert.IsTrue( ClearButton( characters ).Enabled, "the blank character is part of tiles too" );

        // Follows tiles going and coming back while the tab is showing.
        SelectCharacter( characters, 100 );
        editor.Confirm = true;
        ClickClear( characters );
        Assert.IsFalse( ClearButton( characters ).Enabled, "Skeleton is gone - 100 is part of no tile" );
        editor.DocumentInfo.UndoManager.Undo();
        Assert.IsTrue( ClearButton( characters ).Enabled, "undo brought Skeleton back" );
      } );
    }



    [TestMethod]
    public void TestOneTileIsClearedAndDeletedInOneUndoStep()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        var alpha = project.Maps[0];
        int undos = editor.DocumentInfo.UndoManager.NumUndos;

        SelectCharacter( characters, 101 );
        ClickClear( characters );

        Assert.AreEqual( 0, editor.Confirmed, "Skeleton alone draws its characters - no question" );
        Assert.AreEqual( 0, editor.AskedEntities );
        Assert.AreEqual( "Empty, Wall, Wall torch, Grave", TileNames( project ) );
        Assert.AreEqual( "----", Pictures( project, 100, 101, 116, 117 ), "every character of the tile is cleared" );
        Assert.AreEqual( "PPP", Pictures( project, 99, 102, 120 ), "the others are untouched" );
        Assert.AreEqual( EMPTY, alpha.Tiles[1, 1], "the map cell is erased" );
        Assert.AreEqual( -1, alpha.Layers[1].Tiles[3, 0] );
        Assert.AreEqual( GRAVE - 1, alpha.Tiles[2, 1], "the tiles above moved down" );
        Assert.IsTrue( editor.Modified );

        editor.DocumentInfo.UndoManager.Undo();
        Assert.AreEqual( undos, editor.DocumentInfo.UndoManager.NumUndos, "it was a single step" );
        Assert.AreEqual( "Empty, Skeleton, Wall, Wall torch, Grave", TileNames( project ), "one undo brings the tile back..." );
        Assert.AreEqual( "PPPP", Pictures( project, 100, 101, 116, 117 ), "...its characters..." );
        Assert.AreEqual( SKELETON, alpha.Tiles[1, 1], "...and its places on the map" );
        Assert.AreEqual( SKELETON, alpha.Layers[1].Tiles[3, 0] );
        Assert.AreEqual( GRAVE, alpha.Tiles[2, 1] );

        editor.DocumentInfo.UndoManager.Redo();
        Assert.AreEqual( "Empty, Wall, Wall torch, Grave", TileNames( project ), "redo does it all again" );
        Assert.AreEqual( "----", Pictures( project, 100, 101, 116, 117 ) );
        Assert.AreEqual( EMPTY, alpha.Tiles[1, 1] );
      } );
    }



    [TestMethod]
    public void TestATileThatStaysAndLosesACharacterIsAskedAbout()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        int undos = editor.DocumentInfo.UndoManager.NumUndos;
        SelectCharacter( characters, 121 );

        editor.Confirm = false;
        ClickClear( characters );
        Assert.AreEqual( 1, editor.Confirmed );
        CollectionAssert.AreEqual( new[] { WALL_TORCH }, editor.ConfirmedTiles );
        CollectionAssert.AreEqual( new[] { 120, 121 }, editor.ConfirmedCharacters );
        CollectionAssert.AreEqual( new[] { WALL }, new List<int>( editor.ConfirmedLosing.Keys ), "Wall stays but draws 120" );
        CollectionAssert.AreEqual( new[] { 120 }, editor.ConfirmedLosing[WALL] );
        Assert.AreEqual( "Empty, Skeleton, Wall, Wall torch, Grave", TileNames( project ), "No changes nothing" );
        Assert.AreEqual( "PP", Pictures( project, 120, 121 ) );
        Assert.AreEqual( undos, editor.DocumentInfo.UndoManager.NumUndos );
        Assert.IsFalse( editor.Modified );

        editor.Confirm = true;
        ClickClear( characters );
        Assert.AreEqual( "Empty, Skeleton, Wall, Grave", TileNames( project ), "Yes deletes Wall torch only" );
        Assert.AreEqual( "--", Pictures( project, 120, 121 ), "both its characters are cleared, Wall's too" );
      } );
    }



    [TestMethod]
    public void TestACharacterOfSeveralTilesAsksAndDeletesThemAll()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        var alpha = project.Maps[0];
        SelectCharacter( characters, 120 );
        editor.Confirm = true;
        ClickClear( characters );

        Assert.AreEqual( 1, editor.Confirmed );
        CollectionAssert.AreEqual( new[] { WALL, WALL_TORCH }, editor.ConfirmedTiles );
        Assert.AreEqual( 0, editor.ConfirmedLosing.Count, "no other tile draws 120 or 121" );
        Assert.AreEqual( "Empty, Skeleton, Grave", TileNames( project ) );
        Assert.AreEqual( "--", Pictures( project, 120, 121 ) );
        Assert.AreEqual( EMPTY, alpha.Tiles[0, 2] );
        Assert.AreEqual( EMPTY, alpha.Tiles[3, 2] );
        Assert.AreEqual( 2, alpha.Tiles[2, 1], "Grave moved down by two" );

        editor.DocumentInfo.UndoManager.Undo();
        Assert.AreEqual( "Empty, Skeleton, Wall, Wall torch, Grave", TileNames( project ) );
        Assert.AreEqual( "PP", Pictures( project, 120, 121 ) );
        Assert.AreEqual( WALL_TORCH, alpha.Tiles[0, 2] );
        Assert.AreEqual( WALL, alpha.Tiles[3, 2] );
        Assert.AreEqual( GRAVE, alpha.Tiles[2, 1] );
      } );
    }



    [TestMethod]
    public void TestASharedBlankCharacterIsNotAskedAbout()
    {
      WithEditor( false, ( editor, project, characters ) =>
      {
        SelectCharacter( characters, 130 );
        ClickClear( characters );

        Assert.AreEqual( 0, editor.Confirmed, "Grave shares only the blank character with Empty - Empty loses nothing" );
        Assert.AreEqual( "Empty, Skeleton, Wall, Wall torch", TileNames( project ) );
        Assert.AreEqual( "--", Pictures( project, 130, BLANK ) );
      } );
    }



    [TestMethod]
    public void TestEntitiesDrawnWithTheTileGetTheTilesTabQuestion()
    {
      WithEditor( true, ( editor, project, characters ) =>
      {
        var alpha = project.Maps[0];
        int undos = editor.DocumentInfo.UndoManager.NumUndos;
        SelectCharacter( characters, 100 );

        editor.EntityAnswer = Choice.CANCEL;
        ClickClear( characters );
        Assert.AreEqual( 1, editor.AskedEntities );
        Assert.AreEqual( "Empty, Skeleton, Wall, Wall torch, Grave", TileNames( project ), "cancel changes nothing" );
        Assert.AreEqual( "PPPP", Pictures( project, 100, 101, 116, 117 ) );
        Assert.AreEqual( undos, editor.DocumentInfo.UndoManager.NumUndos );

        editor.EntityAnswer = Choice.DELETE_TILE_AND_ENTITIES;
        ClickClear( characters );
        Assert.AreEqual( 0, alpha.Entities.Count, "the bones went with the tile" );
        Assert.AreEqual( "Empty, Wall, Wall torch, Grave", TileNames( project ) );

        editor.DocumentInfo.UndoManager.Undo();
        Assert.AreEqual( undos, editor.DocumentInfo.UndoManager.NumUndos, "one step" );
        Assert.AreEqual( 1, alpha.Entities.Count );
        Assert.AreEqual( SKELETON, project.Charsets[0].EntityTileOf( project.EntityTypes[0] ) );
        Assert.AreEqual( "PPPP", Pictures( project, 100, 101, 116, 117 ) );
        Assert.AreEqual( SKELETON, alpha.Tiles[1, 1] );
      } );
    }
  }
}
