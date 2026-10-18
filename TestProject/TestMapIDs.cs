using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Map IDs: every map's stable number in the game - the number in its
  /// per-map export file names (four digits) and its MAP_ID_* constant.
  /// Projects saved before map IDs existed migrate on load (the exported maps
  /// keep the numbers their files already had), maps without one get the
  /// lowest free ID from 1, IDs survive save/load untouched (duplicates too -
  /// the user renumbers by hand), and deleting, moving or excluding other
  /// maps never renames a map's files.
  /// </summary>
  [TestClass]
  public class TestMapIDs
  {
    // ---------------------------------------------------------------- helpers

    private static MapProject.Map NewMap( string Name, int MapID = -1, bool NotExported = false )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, NotExported = NotExported, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      return map;
    }



    private static MapProject Reload( MapProject Project )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Project.SaveToBuffer() ) );
      return reloaded;
    }



    private static int[] IDs( MapProject Project )
    {
      return Project.Maps.Select( m => m.MapID ).ToArray();
    }



    private static string[] FileNames( MapProject Project )
    {
      List<string> errors;
      var files = Project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.bin", true, true, true, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      Assert.AreEqual( 0, errors.Count, string.Join( "\n", errors ) );
      return files.Select( f => f.FileName ).ToArray();
    }



    private static List<string> ExportErrors( MapProject Project )
    {
      List<string> errors;
      Assert.IsNull( Project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.bin", true, true, true, out errors ) );
      return errors;
    }



    // ------------------------------------------------------------- migration

    [TestMethod]
    public void TestAProjectWithoutIDsGetsItsExportedPositions()
    {
      // Saved before map IDs existed: 4 exported maps among 6 excluded ones.
      var project = new MapProject();
      var exported = new HashSet<int>() { 0, 2, 5, 9 };
      for ( int i = 0; i < 10; ++i )
      {
        project.Maps.Add( NewMap( "Map" + i, -1, !exported.Contains( i ) ) );
      }

      var loaded = Reload( project );
      // exported maps: their exported position, the number their files (and
      // door markers) already use; excluded maps: the lowest free IDs from 1
      CollectionAssert.AreEqual( new[] { 0, 4, 1, 5, 6, 2, 7, 8, 9, 3 }, IDs( loaded ) );
      CollectionAssert.AreEqual( new[] { "MAP_0000.bin", "MAP_0001.bin", "MAP_0002.bin", "MAP_0003.bin" }, FileNames( loaded ),
                                 "the files keep the numbers the old export gave them" );

      CollectionAssert.AreEqual( IDs( loaded ), IDs( Reload( project ) ), "the migration is the same every time" );
      CollectionAssert.AreEqual( IDs( loaded ), IDs( Reload( loaded ) ), "and once saved, the IDs are stored" );
    }



    [TestMethod]
    public void TestIDsSurviveSaveAndLoadUnchanged()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 0 ) );
      project.Maps.Add( NewMap( "B", 255 ) );
      project.Maps.Add( NewMap( "C", 7 ) );
      project.Maps.Add( NewMap( "D", 7 ) );   // a duplicate mid-renumbering is the user's, not fixed on load

      CollectionAssert.AreEqual( new[] { 0, 255, 7, 7 }, IDs( Reload( project ) ) );
    }



    [TestMethod]
    public void TestAMapWithoutAnIDAmongNumberedMapsGetsTheLowestFree()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 1 ) );
      project.Maps.Add( NewMap( "B" ) );
      project.Maps.Add( NewMap( "C", 2 ) );
      project.Maps.Add( NewMap( "D" ) );

      CollectionAssert.AreEqual( new[] { 1, 3, 2, 4 }, IDs( Reload( project ) ) );
    }



    [TestMethod]
    public void TestCloneMapKeepsTheID()
    {
      // revisions and scratch copies are clones - the map keeps its number
      Assert.AreEqual( 9, MapProject.CloneMap( NewMap( "A", 9 ) ).MapID );
    }



    // ------------------------------------------------------------- free IDs

    [TestMethod]
    public void TestFindFreeMapIDStartsAt1AndSkipsTheMapItself()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 0 ) );
      project.Maps.Add( NewMap( "B", 1 ) );
      project.Maps.Add( NewMap( "C", 2 ) );
      project.Maps.Add( NewMap( "D", 4 ) );

      Assert.AreEqual( 3, project.FindFreeMapID( null ), "0 is never handed out, 1 and 2 are taken" );
      Assert.AreEqual( 2, project.FindFreeMapID( project.Maps[2] ), "a map already holding the lowest free ID keeps it" );
      Assert.AreEqual( 3, project.FindFreeMapID( project.Maps[3] ) );

      for ( int id = 5; id <= 255; ++id )
      {
        project.Maps.Add( NewMap( "M" + id, id ) );
      }
      project.Maps.Add( NewMap( "Three", 3 ) );
      Assert.AreEqual( -1, project.FindFreeMapID( null ), "1..255 all taken" );
    }



    // ------------------------------------------------------------- stable file names

    [TestMethod]
    public void TestDeletingMapsKeepsTheOtherMapsNumbers()
    {
      // The question that started it: a door on map 0 loads map 5; maps 3 and
      // 4 are deleted. Map 5 must still be MAP_0005.bin with MAP_ID_ = 5.
      var legacy = new MapProject();
      for ( int i = 0; i < 6; ++i )
      {
        legacy.Maps.Add( NewMap( "Map" + i ) );
      }
      var project = Reload( legacy );
      CollectionAssert.AreEqual( new[] { 0, 1, 2, 3, 4, 5 }, IDs( project ) );
      project.Maps[0].Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, Value1 = 5 } );

      project.Maps.RemoveAt( 4 );
      project.Maps.RemoveAt( 3 );

      CollectionAssert.AreEqual( new[] { "MAP_0000.bin", "MAP_0001.bin", "MAP_0002.bin", "MAP_0005.bin" }, FileNames( project ) );
      StringAssert.Contains( project.GenerateMapLabelsAsm(), ".const MAP_ID_MAP5" );
      Assert.IsTrue( project.GenerateMapLabelsAsm().Split( '\n' ).Any( l => l.StartsWith( ".const MAP_ID_MAP5 " ) && l.Contains( " = 5 " ) ) );
      Assert.AreEqual( project.Maps[3].MapID, project.Maps[0].Markers[0].Value1, "the door's value still names map 5" );
    }



    [TestMethod]
    public void TestExcludingOrMovingAMapKeepsTheOthersNumbers()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 1 ) );
      project.Maps.Add( NewMap( "B", 2 ) );
      project.Maps.Add( NewMap( "C", 3 ) );
      CollectionAssert.AreEqual( new[] { "MAP_0001.bin", "MAP_0002.bin", "MAP_0003.bin" }, FileNames( project ) );

      project.Maps[1].NotExported = true;
      CollectionAssert.AreEqual( new[] { "MAP_0001.bin", "MAP_0003.bin" }, FileNames( project ), "C does not move up" );

      var c = project.Maps[2];
      project.Maps.RemoveAt( 2 );
      project.Maps.Insert( 0, c );
      CollectionAssert.AreEqual( new[] { "MAP_0003.bin", "MAP_0001.bin" }, FileNames( project ), "moving C first keeps its number" );
    }



    [TestMethod]
    public void TestTheStartMapIsNamedByItsID()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 8 ) );
      project.Maps.Add( NewMap( "B", 3, true ) );
      project.Maps.Add( NewMap( "C", 12 ) );

      project.StartMapIndex = 2;
      StringAssert.Contains( project.GenerateMapLabelsAsm(), ".const MAP_START_ID = 12  // C" );

      project.StartMapIndex = 1;    // excluded: the first exported map starts
      StringAssert.Contains( project.GenerateMapLabelsAsm(), ".const MAP_START_ID = 8  // A" );
    }



    // ------------------------------------------------------------- export errors

    [TestMethod]
    public void TestTwoExportedMapsWithOneIDStopTheExport()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 4 ) );
      project.Maps.Add( NewMap( "B", 4 ) );
      var errors = ExportErrors( project );
      Assert.IsTrue( errors.Any( e => e.Contains( "maps 'A' and 'B' both have map ID 4" ) ), string.Join( "\n", errors ) );

      // a not-exported map writes no files, so sharing its ID is fine
      project.Maps[1].NotExported = true;
      CollectionAssert.AreEqual( new[] { "MAP_0004.bin" }, FileNames( project ) );
    }



    [TestMethod]
    public void TestAnExportedMapWithoutAnIDStopsTheExport()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "A", 1 ) );
      project.Maps.Add( NewMap( "Nameless number" ) );
      var errors = ExportErrors( project );
      Assert.IsTrue( errors.Any( e => e.Contains( "map 'Nameless number' has no map ID" ) ), string.Join( "\n", errors ) );
    }
  }
}
