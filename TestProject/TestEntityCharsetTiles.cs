using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// One tile per entity type per charset (MapCharset.EntityTiles): the
  /// lookup, the renumbering that keeps an entity on its picture while its
  /// library changes, duplicating / removing charsets, the MAP_CHARSET_META
  /// persistence, the migration of files that only know the old single tile
  /// number, and the game-binary entity records.
  /// </summary>
  [TestClass]
  public class TestEntityCharsetTiles
  {
    // ---------------------------------------------------------------- helpers

    private static MapProject.EntityType Type( MapProject Project, int ID, string Name )
    {
      var type = new MapProject.EntityType() { ID = ID, Name = Name, TagID = ID + 1 };
      Project.EntityTypes.Add( type );
      return type;
    }



    private static void AddTiles( MapProject.MapCharset Charset, int Count, byte CharBase )
    {
      for ( int i = 0; i < Count; ++i )
      {
        var tile = new MapProject.Tile() { Name = "T" + i };
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
      map.Tiles.Resize( 2, 2 );
      Project.Maps.Add( map );
      return map;
    }



    private static MapProject RoundTrip( MapProject Project )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Project.SaveToBuffer() ) );
      return reloaded;
    }



    private static List<GR.IO.FileChunk> TopLevelChunks( ByteBuffer File )
    {
      var result = new List<GR.IO.FileChunk>();
      var reader = new GR.IO.MemoryReader( File );
      while ( true )
      {
        var chunk = new GR.IO.FileChunk();
        if ( !chunk.ReadFromStream( reader ) )
        {
          break;
        }
        result.Add( chunk );
      }
      return result;
    }



    /// <summary>
    /// The file as a build from before per-charset entity tiles wrote it:
    /// every MAP_CHARSET_META chunk ends after ShiftClickBlankTile.
    /// </summary>
    private static ByteBuffer WithoutEntityTileTables( ByteBuffer File )
    {
      var result = new ByteBuffer();
      foreach ( var chunk in TopLevelChunks( File ) )
      {
        var reader = chunk.MemoryReader();
        if ( chunk.Type != FileChunkConstants.MAP_CHARSET_META )
        {
          var body = new ByteBuffer();
          reader.ReadBlock( body, (uint)( reader.Size - reader.Position ) );
          var copy = new GR.IO.FileChunk( chunk.Type );
          copy.Append( body );
          result.Append( copy.ToBuffer() );
          continue;
        }
        var meta = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_META );
        meta.AppendI32( reader.ReadInt32() );     // index
        meta.AppendString( reader.ReadString() ); // display name
        meta.AppendString( reader.ReadString() ); // export name
        meta.AppendU8( reader.ReadUInt8() );      // export enabled
        meta.AppendString( reader.ReadString() ); // right-click tile
        meta.AppendString( reader.ReadString() ); // shift-click tile
        result.Append( meta.ToBuffer() );
      }
      return result;
    }



    /// <summary>Tile byte of entity record Record on exported map Map (multi-map binary).</summary>
    private static int EntityRecordTile( ByteBuffer Binary, int Map, int Record )
    {
      int loTable = Binary.UInt16At( 0x31 );
      int hiTable = Binary.UInt16At( 0x33 );
      int records = Binary.ByteAt( loTable + Map ) | ( Binary.ByteAt( hiTable + Map ) << 8 );
      return Binary.ByteAt( records + Record * 9 + 4 );
    }



    // ------------------------------------------------------------------ tests

    [TestMethod]
    public void TestEntityTileDefaultsToNone()
    {
      var project = new MapProject();
      var skeleton = Type( project, 3, "Skeleton" );
      var cs = project.Charsets[0];

      Assert.AreEqual( -1, cs.EntityTileOf( skeleton ) );
      cs.SetEntityTile( skeleton, 5 );
      Assert.AreEqual( 5, cs.EntityTileOf( skeleton ) );
      cs.SetEntityTile( skeleton, -1 );
      Assert.AreEqual( -1, cs.EntityTileOf( skeleton ) );
      Assert.AreEqual( 0, cs.EntityTiles.Count, "none means no entry" );
      Assert.AreEqual( -1, cs.EntityTileOf( null ) );
    }



    [TestMethod]
    public void TestEachCharsetKeepsItsOwnTile()
    {
      var project = new MapProject();
      project.AddCharset();
      var skeleton = Type( project, 0, "Skeleton" );
      project.Charsets[0].SetEntityTile( skeleton, 12 );
      project.Charsets[1].SetEntityTile( skeleton, 9 );
      var mapA = AddMap( project, "A", 0 );
      var mapB = AddMap( project, "B", 1 );

      Assert.AreEqual( 12, project.EntityTileOf( mapA, skeleton ) );
      Assert.AreEqual( 9, project.EntityTileOf( mapB, skeleton ) );
    }



    [TestMethod]
    public void TestInsertShiftsTilesAtOrAboveTheInsertPoint()
    {
      var project = new MapProject();
      var a = Type( project, 0, "A" );
      var b = Type( project, 1, "B" );
      var c = Type( project, 2, "C" );
      var cs = project.Charsets[0];
      cs.SetEntityTile( a, 2 );
      cs.SetEntityTile( b, 5 );
      cs.SetEntityTile( c, 7 );

      cs.ShiftEntityTilesForInsert( 5 );

      Assert.AreEqual( 2, cs.EntityTileOf( a ) );
      Assert.AreEqual( 6, cs.EntityTileOf( b ), "the tile at the insert point moves up" );
      Assert.AreEqual( 8, cs.EntityTileOf( c ) );
    }



    [TestMethod]
    public void TestRemovalClearsTheRemovedTileAndShiftsTheRest()
    {
      var project = new MapProject();
      var a = Type( project, 0, "A" );
      var b = Type( project, 1, "B" );
      var c = Type( project, 2, "C" );
      var cs = project.Charsets[0];
      cs.SetEntityTile( a, 2 );
      cs.SetEntityTile( b, 5 );
      cs.SetEntityTile( c, 7 );

      cs.ShiftEntityTilesForRemoval( 5 );

      Assert.AreEqual( 2, cs.EntityTileOf( a ) );
      Assert.AreEqual( -1, cs.EntityTileOf( b ), "its tile is gone" );
      Assert.AreEqual( 6, cs.EntityTileOf( c ) );
    }



    [TestMethod]
    public void TestMoveFollowsTheMovedTile()
    {
      var project = new MapProject();
      var moved = Type( project, 0, "Moved" );
      var between = Type( project, 1, "Between" );
      var below = Type( project, 2, "Below" );
      var above = Type( project, 3, "Above" );
      var cs = project.Charsets[0];
      cs.SetEntityTile( moved, 2 );
      cs.SetEntityTile( between, 4 );
      cs.SetEntityTile( below, 1 );
      cs.SetEntityTile( above, 8 );

      cs.MoveEntityTiles( 2, 6 );
      Assert.AreEqual( 6, cs.EntityTileOf( moved ) );
      Assert.AreEqual( 3, cs.EntityTileOf( between ), "tiles between slide down" );
      Assert.AreEqual( 1, cs.EntityTileOf( below ) );
      Assert.AreEqual( 8, cs.EntityTileOf( above ) );

      cs.MoveEntityTiles( 6, 2 );
      Assert.AreEqual( 2, cs.EntityTileOf( moved ), "moving back restores" );
      Assert.AreEqual( 4, cs.EntityTileOf( between ) );
    }



    [TestMethod]
    public void TestSwapAndPermute()
    {
      var project = new MapProject();
      var a = Type( project, 0, "A" );
      var b = Type( project, 1, "B" );
      var c = Type( project, 2, "C" );
      var far = Type( project, 3, "Far" );
      var cs = project.Charsets[0];
      cs.SetEntityTile( a, 0 );
      cs.SetEntityTile( b, 1 );
      cs.SetEntityTile( c, 3 );
      cs.SetEntityTile( far, 10 );

      cs.SwapEntityTiles( 0, 1 );
      Assert.AreEqual( 1, cs.EntityTileOf( a ) );
      Assert.AreEqual( 0, cs.EntityTileOf( b ) );

      cs.PermuteEntityTiles( new[] { 3, 2, 1, 0 } );
      Assert.AreEqual( 2, cs.EntityTileOf( a ) );
      Assert.AreEqual( 3, cs.EntityTileOf( b ) );
      Assert.AreEqual( 0, cs.EntityTileOf( c ) );
      Assert.AreEqual( 10, cs.EntityTileOf( far ), "a number past the library is left alone" );
    }



    [TestMethod]
    public void TestRenumberingOneCharsetLeavesTheOthersAlone()
    {
      var project = new MapProject();
      project.AddCharset();
      var skeleton = Type( project, 0, "Skeleton" );
      project.Charsets[0].SetEntityTile( skeleton, 4 );
      project.Charsets[1].SetEntityTile( skeleton, 4 );

      project.Charsets[1].ShiftEntityTilesForRemoval( 1 );

      Assert.AreEqual( 4, project.Charsets[0].EntityTileOf( skeleton ) );
      Assert.AreEqual( 3, project.Charsets[1].EntityTileOf( skeleton ) );
    }



    [TestMethod]
    public void TestDuplicateCharsetCopiesTheEntityTiles()
    {
      var project = new MapProject();
      var skeleton = Type( project, 0, "Skeleton" );
      project.Charsets[0].SetEntityTile( skeleton, 7 );

      int copy = project.DuplicateCharset( 0 );
      Assert.AreEqual( 7, project.Charsets[copy].EntityTileOf( skeleton ), "a fresh split keeps its entity pictures" );

      project.Charsets[copy].SetEntityTile( skeleton, 2 );
      Assert.AreEqual( 7, project.Charsets[0].EntityTileOf( skeleton ), "the copy is independent" );
    }



    [TestMethod]
    public void TestRemovedCharsetCarriesItsEntityTilesBack()
    {
      var project = new MapProject();
      int second = project.AddCharset();
      var skeleton = Type( project, 0, "Skeleton" );
      var removed = project.Charsets[second];
      removed.SetEntityTile( skeleton, 3 );

      Assert.IsTrue( project.RemoveCharset( second ) );
      project.InsertCharset( second, removed );

      Assert.AreEqual( 3, project.Charsets[second].EntityTileOf( skeleton ) );
    }



    [TestMethod]
    public void TestForgetEntityTypeClearsEveryCharset()
    {
      var project = new MapProject();
      project.AddCharset();
      var skeleton = Type( project, 0, "Skeleton" );
      var rat = Type( project, 1, "Rat" );
      project.Charsets[0].SetEntityTile( skeleton, 1 );
      project.Charsets[1].SetEntityTile( skeleton, 2 );
      project.Charsets[1].SetEntityTile( rat, 3 );

      project.ForgetEntityType( skeleton );

      Assert.AreEqual( -1, project.Charsets[0].EntityTileOf( skeleton ) );
      Assert.AreEqual( -1, project.Charsets[1].EntityTileOf( skeleton ) );
      Assert.AreEqual( 3, project.Charsets[1].EntityTileOf( rat ) );
    }



    [TestMethod]
    public void TestEntityTilesSurviveSaveAndLoad()
    {
      var project = new MapProject();
      project.AddCharset();
      var skeleton = Type( project, 5, "Skeleton" );
      var rat = Type( project, 2, "Rat" );
      project.Charsets[0].SetEntityTile( skeleton, 12 );
      project.Charsets[0].SetEntityTile( rat, 4 );
      project.Charsets[1].SetEntityTile( skeleton, 9 );

      var reloaded = RoundTrip( project );
      var types = reloaded.EntityTypes;
      Assert.AreEqual( 12, reloaded.Charsets[0].EntityTileOf( types[0] ) );
      Assert.AreEqual( 4, reloaded.Charsets[0].EntityTileOf( types[1] ) );
      Assert.AreEqual( 9, reloaded.Charsets[1].EntityTileOf( types[0] ) );
      Assert.AreEqual( -1, reloaded.Charsets[1].EntityTileOf( types[1] ), "no tile stays no tile" );

      // entries are written in type-ID order: saving the reloaded project gives the same bytes
      var first = project.SaveToBuffer();
      var second = reloaded.SaveToBuffer();
      Assert.AreEqual( first.Length, second.Length );
      Assert.IsTrue( first.Compare( second ) == 0, "a re-save is byte-identical" );
    }



    [TestMethod]
    public void TestOldFilesGiveEveryCharsetTheSingleTileNumber()
    {
      var project = new MapProject();
      project.AddCharset();
      var skeleton = Type( project, 0, "Skeleton" );
      skeleton.PreviewCharsetIndex = 1;
      project.Charsets[1].SetEntityTile( skeleton, 6 );
      project.Charsets[0].SetEntityTile( skeleton, 2 );
      var none = Type( project, 1, "Invisible" );

      // A build without per-charset tiles reads (and wrote) only the single
      // number in the entity type chunk: the tile of the preview charset.
      var old = new MapProject();
      Assert.IsTrue( old.ReadFromBuffer( WithoutEntityTileTables( project.SaveToBuffer() ) ) );

      Assert.AreEqual( 6, old.Charsets[0].EntityTileOf( old.EntityTypes[0] ), "migrated into charset 0" );
      Assert.AreEqual( 6, old.Charsets[1].EntityTileOf( old.EntityTypes[0] ), "migrated into charset 1" );
      Assert.AreEqual( -1, old.Charsets[0].EntityTileOf( old.EntityTypes[1] ), "no tile migrates as no tile" );
      Assert.AreEqual( -1, old.Charsets[1].EntityTileOf( old.EntityTypes[1] ) );
    }



    [TestMethod]
    public void TestEntriesOfMissingTypesAreDroppedOnLoad()
    {
      var project = new MapProject();
      var skeleton = Type( project, 0, "Skeleton" );
      project.Charsets[0].SetEntityTile( skeleton, 1 );
      // what an older build leaves behind after deleting type 7
      project.Charsets[0].EntityTiles[7] = 3;

      var reloaded = RoundTrip( project );
      Assert.IsFalse( reloaded.Charsets[0].EntityTiles.ContainsKey( 7 ), "a reused ID must not inherit it" );
      Assert.AreEqual( 1, reloaded.Charsets[0].EntityTileOf( reloaded.EntityTypes[0] ) );
    }



    [TestMethod]
    public void TestGameBinaryWritesTheTileOfEachMapsCharset()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 4, 0x20 );
      var skeleton = Type( project, 0, "Skeleton" );
      var ghost = Type( project, 1, "Ghost" );
      project.Charsets[0].SetEntityTile( skeleton, 1 );
      project.Charsets[second].SetEntityTile( skeleton, 2 );
      // Ghost has a tile in charset 0 only

      project.Charsets[0].SetEntityTile( ghost, 0 );
      var mapA = AddMap( project, "A", 0 );
      var mapB = AddMap( project, "B", second );
      mapA.Entities.Add( new MapProject.Entity() { Type = skeleton.ID, X = 0, Y = 0 } );
      mapB.Entities.Add( new MapProject.Entity() { Type = skeleton.ID, X = 0, Y = 0 } );
      mapB.Entities.Add( new MapProject.Entity() { Type = ghost.ID, X = 1, Y = 0 } );

      var binary = project.ExportAsGameBinary( true, true, true );
      Assert.IsNotNull( binary );
      Assert.AreEqual( 1, EntityRecordTile( binary, 0, 0 ), "map A: Skeleton's tile in charset 0" );
      Assert.AreEqual( 2, EntityRecordTile( binary, 1, 0 ), "map B: Skeleton's tile in charset 1" );
      Assert.AreEqual( 0xFF, EntityRecordTile( binary, 1, 1 ), "map B: Ghost has no tile there - $FF as always" );

      var warnings = project.GetEntityTileExportWarnings();
      Assert.AreEqual( 1, warnings.Count );
      StringAssert.Contains( warnings[0], "Ghost" );
    }



    [TestMethod]
    public void TestWarningForATileBeyondTheCharset()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 2, 0 );
      var big = Type( project, 0, "Big" );
      project.Charsets[0].SetEntityTile( big, 9 );
      var map = AddMap( project, "A", 0 );
      map.Entities.Add( new MapProject.Entity() { Type = big.ID, X = 0, Y = 0 } );

      var warnings = project.GetEntityTileExportWarnings();
      Assert.AreEqual( 1, warnings.Count );
      StringAssert.Contains( warnings[0], "Big" );
      StringAssert.Contains( warnings[0], "9" );
    }



    [TestMethod]
    public void TestNoWarningWhenEveryPlacedEntityHasATile()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 2, 0 );
      var rat = Type( project, 0, "Rat" );
      var unused = Type( project, 1, "Unused" );     // no tile anywhere, but never placed
      project.Charsets[0].SetEntityTile( rat, 1 );
      var map = AddMap( project, "A", 0 );
      map.Entities.Add( new MapProject.Entity() { Type = rat.ID, X = 0, Y = 0 } );

      Assert.AreEqual( 0, project.GetEntityTileExportWarnings().Count );
    }
  }
}
