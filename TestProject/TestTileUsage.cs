using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Usage of a charset's tiles and characters on the maps LINKED to it (the
  /// maps bound to that charset, plus the editor's scratch workspaces): the
  /// model side of "Get tile count", "Usage in tiles / maps", "Delete unused
  /// tiles" and "Clear unused characters".
  /// </summary>
  [TestClass]
  public class TestTileUsage
  {
    // ---------------------------------------------------------------- helpers

    private static MapProject.Tile Tile( string Name, params byte[] Chars )
    {
      var tile = new MapProject.Tile();
      tile.Name = Name;
      tile.Chars.Resize( Chars.Length, 1 );
      for ( int i = 0; i < Chars.Length; ++i )
      {
        tile.Chars[i, 0].Character = Chars[i];
        tile.Chars[i, 0].Color     = 1;
      }
      return tile;
    }



    private static MapProject.Map Map( string Name, int CharsetIndex, int Width, int Height, int Fill )
    {
      var map = new MapProject.Map();
      map.Name = Name;
      map.CharsetIndex = CharsetIndex;
      map.Tiles.Resize( Width, Height );
      for ( int y = 0; y < Height; ++y )
      {
        for ( int x = 0; x < Width; ++x )
        {
          map.Tiles[x, y] = Fill;
        }
      }
      map.EnsureDefaultLayers();
      return map;
    }



    /// <summary>
    /// Charset 0 with 6 tiles (chars 10..15), charset 1 with 3 tiles (chars
    /// 20..22). Map A (charset 0) uses tiles 0 and 1 on the background and
    /// tile 2 on an upper layer; map B (charset 1) uses its tile 2.
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      var cs0 = project.Charsets[0];
      cs0.Tiles.Add( Tile( "Blank", 10 ) );
      cs0.Tiles.Add( Tile( "Floor", 11 ) );
      cs0.Tiles.Add( Tile( "Overlay", 12 ) );
      cs0.Tiles.Add( Tile( "Torch", 13 ) );
      cs0.Tiles.Add( Tile( "Skeleton", 14 ) );
      cs0.Tiles.Add( Tile( "Crate", 15, 11 ) );
      cs0.ReindexTiles();

      int second = project.AddCharset();
      var cs1 = project.Charsets[second];
      cs1.Tiles.Add( Tile( "Blank", 20 ) );
      cs1.Tiles.Add( Tile( "Wall", 21 ) );
      cs1.Tiles.Add( Tile( "Door", 22 ) );
      cs1.ReindexTiles();

      var mapA = Map( "A", 0, 4, 3, 0 );
      mapA.Tiles[1, 1] = 1;
      mapA.Tiles[2, 1] = 1;
      mapA.Layers[1].Tiles[3, 2] = 2;
      project.Maps.Add( mapA );

      var mapB = Map( "B", second, 2, 2, 2 );
      project.Maps.Add( mapB );
      return project;
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestCountTileUsageCoversEveryLayer()
    {
      var project = BuildProject();
      var usage = MapProject.CountTileUsage( project.Charsets[0], new[] { project.Maps[0] } );

      Assert.AreEqual( 6, usage.Length );
      Assert.AreEqual( 10, usage[0] );    // 12 background cells minus the two Floor cells
      Assert.AreEqual( 2, usage[1] );
      Assert.AreEqual( 1, usage[2] );     // only on an UPPER layer
      Assert.AreEqual( 0, usage[3] );
      Assert.AreEqual( 0, usage[4] );
      Assert.AreEqual( 0, usage[5] );
    }



    [TestMethod]
    public void TestCountTileUsageIgnoresTransparentAndOutOfRangeCells()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Tiles[0, 0] = 99;                 // beyond the library
      map.Layers[2].Tiles[0, 0] = -1;       // transparent
      var usage = MapProject.CountTileUsage( project.Charsets[0], new[] { map } );

      Assert.AreEqual( 9, usage[0] );
      Assert.AreEqual( 12, usage.Sum() );   // 11 valid background cells + 1 overlay cell
    }



    [TestMethod]
    public void TestMapsLinkedToOnlyReturnsMapsBoundToTheCharset()
    {
      var project = BuildProject();
      var scratchOfA = Map( "A (scratch)", 0, 2, 2, 0 );
      var scratchOfB = Map( "B (scratch)", 1, 2, 2, 0 );

      var linked0 = project.MapsLinkedTo( project.Charsets[0], new[] { scratchOfA, scratchOfB } );
      var linked1 = project.MapsLinkedTo( project.Charsets[1], new[] { scratchOfA, scratchOfB } );

      CollectionAssert.AreEqual( new[] { project.Maps[0], scratchOfA }, linked0 );
      CollectionAssert.AreEqual( new[] { project.Maps[1], scratchOfB }, linked1 );
      Assert.AreEqual( 1, project.MapsLinkedTo( project.Charsets[0], null ).Count );
    }



    [TestMethod]
    public void TestUnusedTilesIgnoreMapsOnAnotherCharset()
    {
      var project = BuildProject();

      // Map B uses index 2 - but in charset 1. It must neither protect charset 0's
      // tiles nor make charset 1's Wall (index 1, unused on B) look used.
      CollectionAssert.AreEqual( new[] { 3, 4, 5 }, project.UnusedTileIndexes( project.Charsets[0], null ) );
      CollectionAssert.AreEqual( new[] { 1 }, project.UnusedTileIndexes( project.Charsets[1], null ) );
    }



    [TestMethod]
    public void TestUnusedTilesKeepTilesOnlyUsedOnUpperLayers()
    {
      var project = BuildProject();
      Assert.IsFalse( project.UnusedTileIndexes( project.Charsets[0], null ).Contains( 2 ) );
    }



    [TestMethod]
    public void TestUnusedTilesCountScratchWorkspaces()
    {
      var project = BuildProject();
      var scratch = Map( "A (scratch)", 0, 2, 2, 3 );    // the workspace still holds Torch

      CollectionAssert.AreEqual( new[] { 4, 5 }, project.UnusedTileIndexes( project.Charsets[0], new[] { scratch } ) );
    }



    [TestMethod]
    public void TestTileZeroIsNeverUnused()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      for ( int y = 0; y < map.Tiles.Height; ++y )
      {
        for ( int x = 0; x < map.Tiles.Width; ++x )
        {
          map.Tiles[x, y] = 1;
        }
      }
      Assert.IsFalse( project.UnusedTileIndexes( project.Charsets[0], null ).Contains( 0 ) );
      StringAssert.Contains( project.ProtectedTiles( project.Charsets[0], project.Maps )[0], "tile 0" );
    }



    [TestMethod]
    public void TestEntityTilesAreProtected()
    {
      var project = BuildProject();

      // designed against charset 0 (its preview charset) - no instance anywhere
      project.EntityTypes.Add( new MapProject.EntityType() { Name = "Skeleton", ID = 1, TileIndex = 4, PreviewCharsetIndex = 0 } );
      // previews charset 1, but an instance stands on map A (charset 0): its number is read there
      project.EntityTypes.Add( new MapProject.EntityType() { Name = "Lamp", ID = 2, TileIndex = 3, PreviewCharsetIndex = 1 } );
      project.Maps[0].Entities.Add( new MapProject.Entity() { Type = 2, X = 0, Y = 0 } );
      // previews charset 1 and only stands on map B: nothing to do with charset 0
      project.EntityTypes.Add( new MapProject.EntityType() { Name = "Box", ID = 3, TileIndex = 5, PreviewCharsetIndex = 1 } );
      project.Maps[1].Entities.Add( new MapProject.Entity() { Type = 3, X = 0, Y = 0 } );

      CollectionAssert.AreEqual( new[] { 5 }, project.UnusedTileIndexes( project.Charsets[0], null ) );

      var kept = project.ProtectedTiles( project.Charsets[0], project.MapsLinkedTo( project.Charsets[0], null ) );
      StringAssert.Contains( kept[4], "Skeleton" );
      StringAssert.Contains( kept[3], "Lamp" );
      Assert.IsFalse( kept.ContainsKey( 5 ) );
    }



    [TestMethod]
    public void TestNamedAndExportTilesAreProtected()
    {
      var project = BuildProject();
      project.Charsets[0].RightClickAction    = "Torch";
      project.Charsets[0].ShiftClickBlankTile = "Skeleton";
      project.Settings.Assembly.EmptyTileIndex = 5;

      Assert.AreEqual( 0, project.UnusedTileIndexes( project.Charsets[0], null ).Count );

      var kept = project.ProtectedTiles( project.Charsets[0], project.Maps );
      StringAssert.Contains( kept[3], "right-click" );
      StringAssert.Contains( kept[4], "shift-click" );
      StringAssert.Contains( kept[5], "empty tile" );
    }



    [TestMethod]
    public void TestNothingIsUnusedWithoutALinkedMap()
    {
      var project = BuildProject();
      int orphan = project.AddCharset();
      project.Charsets[orphan].Tiles.Add( Tile( "Blank", 1 ) );
      project.Charsets[orphan].Tiles.Add( Tile( "Lonely", 2 ) );
      project.Charsets[orphan].ReindexTiles();

      // no map is bound to it: nothing to check against, so nothing may be called unused
      Assert.AreEqual( 0, project.UnusedTileIndexes( project.Charsets[orphan], null ).Count );
      Assert.IsTrue( project.UsedCharacters( project.Charsets[orphan], null ).All( used => used ) );
    }



    [TestMethod]
    public void TestUsedCharactersFollowUsedTiles()
    {
      var project = BuildProject();
      var used = project.UsedCharacters( project.Charsets[0], null );

      Assert.AreEqual( 256, used.Length );
      Assert.IsTrue( used[10] );     // Blank  (tile 0, placed)
      Assert.IsTrue( used[11] );     // Floor  (placed)
      Assert.IsTrue( used[12] );     // Overlay (upper layer only)
      Assert.IsFalse( used[13] );    // Torch: nobody places it
      Assert.IsFalse( used[14] );    // Skeleton
      Assert.IsFalse( used[15] );    // Crate's own character; its second one (11) is used by Floor
      Assert.AreEqual( 3, used.Count( u => u ) );
    }



    [TestMethod]
    public void TestUsedCharactersIncludeProtectedTiles()
    {
      var project = BuildProject();
      project.EntityTypes.Add( new MapProject.EntityType() { Name = "Skeleton", ID = 1, TileIndex = 4, PreviewCharsetIndex = 0 } );

      var used = project.UsedCharacters( project.Charsets[0], null );
      Assert.IsTrue( used[14] );     // the entity's tile is never placed in a cell, but it is drawn
      Assert.IsFalse( used[13] );
    }



    [TestMethod]
    public void TestUsedCharactersIgnoreOtherCharsets()
    {
      var project = BuildProject();
      var used1 = project.UsedCharacters( project.Charsets[1], null );

      Assert.IsTrue( used1[20] );    // tile 0
      Assert.IsFalse( used1[21] );   // Wall is not placed on map B
      Assert.IsTrue( used1[22] );    // Door
      Assert.IsFalse( used1[11] );   // charset 0's Floor character means nothing here
    }
  }
}
