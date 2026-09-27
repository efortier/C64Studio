using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Multiple character sets per map project: the MapCharset model, the
  /// MAP_CHARSET_META / MAP_CHARSET_ENTRY chunks, the per-map / per-entity
  /// charset indexes, index shifting on add/remove, and the migration of
  /// pre-multi-charset project files (the LegacySingleCharset fixture was
  /// written by the single-charset build and must load unchanged).
  /// </summary>
  [TestClass]
  public class TestMapCharsets
  {
    private const string LEGACY_FIXTURE = "Compare Files/LegacySingleCharset.mapproject";
    private const string LEGACY_SCRATCH = "Compare Files/LegacySingleCharset.mapscratch";



    // ---------------------------------------------------------------- helpers

    private static MapProject.Tile MakeTile( string Name, int W, int H, byte FirstChar, byte FirstColor, bool Passable )
    {
      var tile = new MapProject.Tile();
      tile.Name = Name;
      tile.Passable = Passable;
      tile.Chars.Resize( W, H );
      int n = 0;
      for ( int y = 0; y < H; ++y )
      {
        for ( int x = 0; x < W; ++x )
        {
          tile.Chars[x, y] = new MapProject.TileChar() { Character = (byte)( FirstChar + n ), Color = (byte)( FirstColor + n ) };
          ++n;
        }
      }
      return tile;
    }



    private static MapProject.Map MakeMap( string Name, int W, int H, int Fill, int CharsetIndex )
    {
      var map = new MapProject.Map() { Name = Name, TileSpacingX = 1, TileSpacingY = 1, CharsetIndex = CharsetIndex };
      map.Tiles.Resize( W, H );
      map.TileColorOverrides.Resize( W, H );
      map.CharBlockedOverrides.Resize( W, H );
      for ( int y = 0; y < H; ++y )
      {
        for ( int x = 0; x < W; ++x )
        {
          map.Tiles[x, y] = Fill;
        }
      }
      return map;
    }



    /// <summary>Charset 0 with 2 tiles, charset 1 with 3 tiles and changed art; maps A→0, B→1.</summary>
    private static MapProject BuildTwoCharsetProject()
    {
      var proj = new MapProject();
      var cs0 = proj.Charsets[0];
      cs0.Tiles.Add( MakeTile( "Floor", 1, 1, 0x20, 1, true ) );
      cs0.Tiles.Add( MakeTile( "Wall", 2, 1, 0x40, 2, false ) );
      cs0.ReindexTiles();
      cs0.DisplayName = "Overworld";
      cs0.ExportName = "cs0.chr";
      cs0.ExportEnabled = true;
      cs0.RightClickAction = "Wall";
      cs0.ShiftClickBlankTile = "Floor";

      Assert.AreEqual( 1, proj.AddCharset() );
      var cs1 = proj.Charsets[1];
      cs1.Tiles.Add( MakeTile( "Rock", 1, 1, 0x60, 3, true ) );
      cs1.Tiles.Add( MakeTile( "Lava", 2, 2, 0x70, 4, false ) );
      var vent = MakeTile( "Vent", 1, 1, 0x7f, 5, true );
      vent.NotExportedOnMap = true;
      vent.GroupId = 9;
      cs1.Tiles.Add( vent );
      cs1.ReindexTiles();
      for ( int j = 0; j < 8; ++j )
      {
        cs1.Charset.Characters[2].Tile.Data.SetU8At( j, (byte)( 0xA0 + j ) );
      }
      cs1.Charset.Characters[2].Tile.CustomColor = 7;
      cs1.DisplayName = "Sprites";
      cs1.ExportName = "cs1.chr";
      cs1.ExportEnabled = false;
      cs1.RightClickAction = "Lava";
      cs1.ShiftClickBlankTile = "Rock";

      proj.Maps.Add( MakeMap( "A", 3, 2, 1, 0 ) );
      proj.Maps.Add( MakeMap( "B", 2, 2, 2, 1 ) );
      proj.EntityTypes.Add( new MapProject.EntityType() { ID = 0, Name = "Enemy", TileIndex = 1, TagID = 5, PreviewCharsetIndex = 1 } );
      proj.CurrentCharsetIndex = 1;
      proj.CharsetTabFollowsMap = false;
      return proj;
    }



    private static MapProject RoundTrip( MapProject proj )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      return reloaded;
    }



    private static void AssertTilesEqual( MapProject.Tile Expected, MapProject.Tile Actual, string What )
    {
      Assert.AreEqual( Expected.Name, Actual.Name, What + " name" );
      Assert.AreEqual( Expected.Chars.Width, Actual.Chars.Width, What + " width" );
      Assert.AreEqual( Expected.Chars.Height, Actual.Chars.Height, What + " height" );
      for ( int y = 0; y < Expected.Chars.Height; ++y )
      {
        for ( int x = 0; x < Expected.Chars.Width; ++x )
        {
          Assert.AreEqual( Expected.Chars[x, y].Character, Actual.Chars[x, y].Character, What + " char " + x + "," + y );
          Assert.AreEqual( Expected.Chars[x, y].Color, Actual.Chars[x, y].Color, What + " color " + x + "," + y );
        }
      }
      Assert.AreEqual( Expected.Passable, Actual.Passable, What + " passable" );
      Assert.AreEqual( Expected.NotExportedOnMap, Actual.NotExportedOnMap, What + " not exported" );
      Assert.AreEqual( Expected.GroupId, Actual.GroupId, What + " group" );
    }



    private static List<GR.IO.FileChunk> ReadChunks( GR.IO.MemoryReader Reader )
    {
      var result = new List<GR.IO.FileChunk>();
      while ( true )
      {
        var chunk = new GR.IO.FileChunk();
        if ( !chunk.ReadFromStream( Reader ) )
        {
          break;
        }
        result.Add( chunk );
      }
      return result;
    }



    private static List<GR.IO.FileChunk> TopLevelChunks( ByteBuffer File )
    {
      return ReadChunks( new GR.IO.MemoryReader( File ) );
    }



    private static List<GR.IO.FileChunk> SubChunks( GR.IO.FileChunk Container )
    {
      return ReadChunks( Container.MemoryReader() );
    }



    private static ByteBuffer BodyOf( GR.IO.FileChunk Chunk )
    {
      var body = new ByteBuffer();
      var reader = Chunk.MemoryReader();
      reader.ReadBlock( body, (uint)( reader.Size - reader.Position ) );
      return body;
    }



    private static GR.IO.FileChunk MakeChunk( ushort Type, ByteBuffer Body )
    {
      var chunk = new GR.IO.FileChunk( Type );
      chunk.Append( Body );
      return chunk;
    }



    private static ByteBuffer Rebuild( IEnumerable<GR.IO.FileChunk> Chunks )
    {
      var buf = new ByteBuffer();
      foreach ( var chunk in Chunks )
      {
        buf.Append( MakeChunk( chunk.Type, BodyOf( chunk ) ).ToBuffer() );
      }
      return buf;
    }



    private static ByteBuffer Truncated( ByteBuffer Body, int Bytes )
    {
      var data = Body.Data();
      var result = new ByteBuffer();
      for ( int i = 0; i < data.Length - Bytes; ++i )
      {
        result.AppendU8( data[i] );
      }
      return result;
    }



    /// <summary>Rebuilds a container with one sub-chunk's body truncated by Bytes.</summary>
    private static GR.IO.FileChunk TruncateSubChunk( GR.IO.FileChunk Container, ushort SubType, int Bytes )
    {
      var rebuilt = new GR.IO.FileChunk( Container.Type );
      bool found = false;
      foreach ( var sub in SubChunks( Container ) )
      {
        var body = BodyOf( sub );
        if ( ( sub.Type == SubType )
        &&   ( !found ) )
        {
          body = Truncated( body, Bytes );
          found = true;
        }
        rebuilt.Append( MakeChunk( sub.Type, body ).ToBuffer() );
      }
      Assert.IsTrue( found, "sub-chunk not found" );
      return rebuilt;
    }



    /// <summary>Sub-chunks of a nested CharsetProject image ([u32 version][CHARSET_PROJECT chunk]).</summary>
    private static List<GR.IO.FileChunk> NestedCharsetChunks( ByteBuffer Image )
    {
      var reader = new GR.IO.MemoryReader( Image );
      Assert.AreEqual( 2u, reader.ReadUInt32(), "charset image version" );
      var project = new GR.IO.FileChunk();
      Assert.IsTrue( project.ReadFromStream( reader ) );
      Assert.AreEqual( FileChunkConstants.CHARSET_PROJECT, project.Type );
      return SubChunks( project );
    }



    private static MapProject.Map DeserializeMapChunk( GR.IO.FileChunk MapChunk )
    {
      var map = new MapProject.Map();
      MapProject.ReadMapFromBody( MapChunk.MemoryReader(), map );
      return map;
    }



    private static void AssertDefaultFont( CharsetProject Charset, string What )
    {
      for ( int j = 0; j < 8; ++j )
      {
        Assert.AreEqual( ConstantData.UpperCaseCharsetC64.ByteAt( 8 + j ), Charset.Characters[1].Tile.Data.ByteAt( j ), What + " char 1 byte " + j );
      }
      Assert.AreEqual( 1, Charset.Characters[1].Tile.CustomColor, What + " custom color" );
    }



    // ---------------------------------------------------------------- defaults

    [TestMethod]
    public void TestFreshProjectHasOneDefaultCharset()
    {
      var proj = new MapProject();
      Assert.AreEqual( 1, proj.Charsets.Count );
      AssertDefaultFont( proj.Charsets[0].Charset, "fresh" );
      Assert.AreEqual( 0, proj.Charsets[0].Tiles.Count );
      Assert.IsTrue( proj.Charsets[0].ExportEnabled );
      Assert.AreEqual( "", proj.Charsets[0].DisplayName );
      Assert.AreEqual( 0, proj.CurrentCharsetIndex );
      Assert.IsTrue( proj.CharsetTabFollowsMap );
      Assert.AreEqual( 0, new MapProject.Map().CharsetIndex );
      Assert.AreEqual( 0, new MapProject.EntityType().PreviewCharsetIndex );
    }



    // ---------------------------------------------------------------- round trips

    [TestMethod]
    public void TestMultiCharsetRoundTrip()
    {
      var proj = BuildTwoCharsetProject();
      var reloaded = RoundTrip( proj );

      Assert.AreEqual( 2, reloaded.Charsets.Count );
      for ( int c = 0; c < 2; ++c )
      {
        var expected = proj.Charsets[c];
        var actual = reloaded.Charsets[c];
        Assert.AreEqual( expected.Tiles.Count, actual.Tiles.Count, "tile count charset " + c );
        for ( int t = 0; t < expected.Tiles.Count; ++t )
        {
          AssertTilesEqual( expected.Tiles[t], actual.Tiles[t], "charset " + c + " tile " + t );
          Assert.AreEqual( t, actual.Tiles[t].Index, "Tile.Index rebuilt" );
        }
        Assert.AreEqual( expected.DisplayName, actual.DisplayName, "display name " + c );
        Assert.AreEqual( expected.ExportName, actual.ExportName, "export name " + c );
        Assert.AreEqual( expected.ExportEnabled, actual.ExportEnabled, "export enabled " + c );
        Assert.AreEqual( expected.RightClickAction, actual.RightClickAction, "right click " + c );
        Assert.AreEqual( expected.ShiftClickBlankTile, actual.ShiftClickBlankTile, "blank tile " + c );
      }
      for ( int j = 0; j < 8; ++j )
      {
        Assert.AreEqual( (byte)( 0xA0 + j ), reloaded.Charsets[1].Charset.Characters[2].Tile.Data.ByteAt( j ), "charset 1 char 2 art" );
      }
      Assert.AreEqual( 7, reloaded.Charsets[1].Charset.Characters[2].Tile.CustomColor );
      AssertDefaultFont( reloaded.Charsets[0].Charset, "charset 0" );

      Assert.AreEqual( 2, reloaded.Maps.Count );
      Assert.AreEqual( 0, reloaded.Maps[0].CharsetIndex );
      Assert.AreEqual( 1, reloaded.Maps[1].CharsetIndex );
      Assert.AreEqual( 1, reloaded.EntityTypes[0].PreviewCharsetIndex );
      Assert.AreEqual( 1, reloaded.EntityTypes[0].TileIndex );
      Assert.AreEqual( 1, reloaded.CurrentCharsetIndex );
      Assert.IsFalse( reloaded.CharsetTabFollowsMap );
    }



    [TestMethod]
    public void TestMapCharsetMetaCoversEveryScalarField()
    {
      // Reflection auto-guard: a scalar added to MapCharset without a META
      // writer/reader fails here by name. Both charsets get distinct values
      // so a field only persisted for charset 0 is caught too.
      var fields = typeof( MapProject.MapCharset ).GetFields( BindingFlags.Public | BindingFlags.Instance );
      var scalars = new List<FieldInfo>();
      var proj = BuildTwoCharsetProject();
      int n = 0;
      foreach ( var cs in proj.Charsets )
      {
        foreach ( var field in fields )
        {
          ++n;
          if ( field.FieldType == typeof( string ) )
          {
            field.SetValue( cs, "val_" + n );
          }
          else if ( field.FieldType == typeof( bool ) )
          {
            field.SetValue( cs, !(bool)field.GetValue( cs ) );
          }
          else if ( field.FieldType == typeof( int ) )
          {
            field.SetValue( cs, 100 + n );
          }
          else if ( ( field.FieldType == typeof( CharsetProject ) )
          ||        ( field.FieldType == typeof( List<MapProject.Tile> ) ) )
          {
            continue;   // covered by TestMultiCharsetRoundTrip
          }
          else
          {
            Assert.Fail( "MapCharset field '" + field.Name + "' has type " + field.FieldType.Name
                       + " which this guard does not handle. Add it to the MAP_CHARSET_META writer, the reader, and this test." );
          }
          if ( !scalars.Contains( field ) )
          {
            scalars.Add( field );
          }
        }
      }
      Assert.IsTrue( scalars.Count >= 5, "expected the five known scalar fields" );

      var reloaded = RoundTrip( proj );
      for ( int c = 0; c < proj.Charsets.Count; ++c )
      {
        foreach ( var field in scalars )
        {
          Assert.AreEqual( field.GetValue( proj.Charsets[c] ), field.GetValue( reloaded.Charsets[c] ),
                           "MapCharset." + field.Name + " on charset " + c + " did not survive save/load — add it to MAP_CHARSET_META" );
        }
      }
    }



    [TestMethod]
    public void TestChunkOrderIndependence()
    {
      var proj = BuildTwoCharsetProject();
      var chunks = TopLevelChunks( proj.SaveToBuffer() );
      Assert.IsTrue( chunks.Any( c => c.Type == FileChunkConstants.MAP_CHARSET_ENTRY ), "entry chunk present" );
      Assert.AreEqual( 2, chunks.Count( c => c.Type == FileChunkConstants.MAP_CHARSET_META ), "one META per charset" );
      chunks.Reverse();

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Rebuild( chunks ) ) );
      Assert.AreEqual( 2, reloaded.Charsets.Count );
      Assert.AreEqual( "Overworld", reloaded.Charsets[0].DisplayName );
      Assert.AreEqual( "Sprites", reloaded.Charsets[1].DisplayName );
      Assert.AreEqual( 2, reloaded.Charsets[0].Tiles.Count );
      Assert.AreEqual( 3, reloaded.Charsets[1].Tiles.Count );
      Assert.AreEqual( "Vent", reloaded.Charsets[1].Tiles[2].Name );
      Assert.AreEqual( (byte)0xA3, reloaded.Charsets[1].Charset.Characters[2].Tile.Data.ByteAt( 3 ) );
      Assert.AreEqual( 1, reloaded.Maps[1].CharsetIndex );
      Assert.AreEqual( 1, reloaded.CurrentCharsetIndex );
    }



    [TestMethod]
    public void TestEntryWithInfoNotFirst()
    {
      var proj = BuildTwoCharsetProject();
      var chunks = TopLevelChunks( proj.SaveToBuffer() );
      var rebuilt = new List<GR.IO.FileChunk>();
      foreach ( var chunk in chunks )
      {
        if ( chunk.Type != FileChunkConstants.MAP_CHARSET_ENTRY )
        {
          rebuilt.Add( chunk );
          continue;
        }
        // Move ENTRY_INFO to the END of the container.
        var subs = SubChunks( chunk );
        var info = subs.First( c => c.Type == FileChunkConstants.MAP_CHARSET_ENTRY_INFO );
        subs.Remove( info );
        subs.Add( info );
        var container = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_ENTRY );
        container.Append( Rebuild( subs ) );
        rebuilt.Add( container );
      }
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Rebuild( rebuilt ) ) );
      Assert.AreEqual( 2, reloaded.Charsets.Count );
      Assert.AreEqual( 3, reloaded.Charsets[1].Tiles.Count );
      Assert.AreEqual( "Sprites", reloaded.Charsets[1].DisplayName );
      Assert.AreEqual( 7, reloaded.Charsets[1].Charset.Characters[2].Tile.CustomColor );
    }



    [TestMethod]
    public void TestCorruptEntryIndexIgnored()
    {
      var proj = new MapProject();
      proj.Charsets[0].Tiles.Add( MakeTile( "Only", 1, 1, 1, 1, true ) );
      var chunks = TopLevelChunks( proj.SaveToBuffer() );

      foreach ( int badIndex in new int[] { 0, -1, 300 } )
      {
        var entry = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_ENTRY );
        var info = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_ENTRY_INFO );
        info.AppendI32( badIndex );
        entry.Append( info.ToBuffer() );
        var data = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_ENTRY_DATA );
        data.Append( new CharsetProject().SaveToBuffer() );
        entry.Append( data.ToBuffer() );
        var meta = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET_META );
        meta.AppendI32( 300 );
        meta.AppendString( "ghost" );

        var file = Rebuild( chunks );
        file.Append( entry.ToBuffer() );
        file.Append( meta.ToBuffer() );

        var reloaded = new MapProject();
        Assert.IsTrue( reloaded.ReadFromBuffer( file ), "index " + badIndex );
        Assert.AreEqual( 1, reloaded.Charsets.Count, "corrupt index " + badIndex + " must not grow the list" );
        Assert.AreEqual( 1, reloaded.Charsets[0].Tiles.Count );
        Assert.AreEqual( "Only", reloaded.Charsets[0].Tiles[0].Name );
      }
    }



    // ---------------------------------------------------------------- legacy migration

    [TestMethod]
    public void TestLegacyFixtureMigratesIntoCharsetZero()
    {
      var proj = new MapProject();
      Assert.IsTrue( proj.ReadFromBuffer( GR.IO.File.ReadAllBytes( LEGACY_FIXTURE ) ) );

      Assert.AreEqual( 1, proj.Charsets.Count );
      var cs0 = proj.Charsets[0];
      Assert.AreEqual( 3, cs0.Tiles.Count );
      Assert.AreEqual( "Floor", cs0.Tiles[0].Name );
      Assert.AreEqual( "Wall", cs0.Tiles[1].Name );
      Assert.AreEqual( "Secret", cs0.Tiles[2].Name );
      Assert.AreEqual( 2, cs0.Tiles[1].Chars.Width );
      Assert.AreEqual( 2, cs0.Tiles[1].Chars.Height );
      Assert.AreEqual( (byte)0x13, cs0.Tiles[1].Chars[1, 1].Character );
      Assert.AreEqual( (byte)4, cs0.Tiles[1].Chars[1, 1].Color );
      Assert.IsFalse( cs0.Tiles[1].Passable );
      Assert.IsTrue( cs0.Tiles[2].NotExportedOnMap );
      Assert.AreEqual( 7, cs0.Tiles[2].GroupId );
      Assert.AreEqual( 2, cs0.Tiles[2].Index );

      Assert.AreEqual( "Wall", cs0.RightClickAction );
      Assert.AreEqual( "Floor", cs0.ShiftClickBlankTile );
      // GameBinary.ExportCharset was FALSE in the fixture (its name is skipped);
      // Assembly.ExportCharset was TRUE, so its name seeds the export name.
      Assert.AreEqual( "legacy_asm.chr", cs0.ExportName );
      Assert.IsTrue( cs0.ExportEnabled, "charset 0 is always enabled after migration" );
      Assert.AreEqual( "", cs0.DisplayName );

      Assert.AreEqual( 2, proj.Maps.Count );
      Assert.AreEqual( 0, proj.Maps[0].CharsetIndex );
      Assert.AreEqual( 0, proj.Maps[1].CharsetIndex );
      Assert.AreEqual( 1, proj.Maps[0].Revisions.Count );
      Assert.AreEqual( 0, proj.Maps[0].Revisions[0].Snapshot.CharsetIndex );
      Assert.IsTrue( proj.Maps[1].NotExported );
      Assert.AreEqual( 2, proj.EntityTypes.Count );
      Assert.AreEqual( 0, proj.EntityTypes[0].PreviewCharsetIndex );
      Assert.AreEqual( 1, proj.EntityTypes[0].TileIndex );
      Assert.AreEqual( 2, proj.EntityTypes[1].TileIndex );

      for ( int j = 0; j < 8; ++j )
      {
        Assert.AreEqual( (byte)( 0x81 + j ), cs0.Charset.Characters[1].Tile.Data.ByteAt( j ), "char 1 art byte " + j );
      }
      Assert.AreEqual( 5, cs0.Charset.Characters[1].Tile.CustomColor );
      Assert.AreEqual( 4, cs0.Charset.Colors.MultiColor1 );
      Assert.AreEqual( 12, cs0.Charset.Colors.MultiColor2 );
      Assert.AreEqual( 3, cs0.Charset.Colors.BGColor4 );

      Assert.AreEqual( 1, proj.CurrentMapIndex );
      Assert.AreEqual( 3, proj.ShiftClickBlankColor );
      Assert.AreEqual( 0, proj.CurrentCharsetIndex );
      Assert.IsTrue( proj.CharsetTabFollowsMap );
    }



    private static ByteBuffer LegacyStream( bool GameBinaryFlag, string GameBinaryName, bool AssemblyFlag, string AssemblyName )
    {
      // A "legacy" file = a current save with every MAP_CHARSET_META chunk removed.
      var proj = new MapProject();
      proj.Charsets[0].RightClickAction = "X";
      proj.Settings.GameBinary.ExportCharset = GameBinaryFlag;
      proj.Settings.GameBinary.CharsetExportFilename = GameBinaryName;
      proj.Settings.Assembly.ExportCharset = AssemblyFlag;
      proj.Settings.Assembly.CharsetExportFilename = AssemblyName;
      var chunks = TopLevelChunks( proj.SaveToBuffer() ).Where( c => c.Type != FileChunkConstants.MAP_CHARSET_META );
      return Rebuild( chunks );
    }



    [TestMethod]
    public void TestLegacyExportNameSeeding()
    {
      var gameBinary = new MapProject();
      Assert.IsTrue( gameBinary.ReadFromBuffer( LegacyStream( true, "gb.chr", false, "asm.chr" ) ) );
      Assert.AreEqual( "gb.chr", gameBinary.Charsets[0].ExportName );
      Assert.AreEqual( "X", gameBinary.Charsets[0].RightClickAction, "legacy project string migrates" );

      var assembly = new MapProject();
      Assert.IsTrue( assembly.ReadFromBuffer( LegacyStream( false, "gb.chr", true, "asm.chr" ) ) );
      Assert.AreEqual( "asm.chr", assembly.Charsets[0].ExportName );

      var none = new MapProject();
      Assert.IsTrue( none.ReadFromBuffer( LegacyStream( false, "gb.chr", false, "asm.chr" ) ) );
      Assert.AreEqual( "", none.Charsets[0].ExportName, "no flag set = no charset file, as before" );
      Assert.IsTrue( none.Charsets[0].ExportEnabled, "tiles still export" );

      var both = new MapProject();
      Assert.IsTrue( both.ReadFromBuffer( LegacyStream( true, "gb.chr", true, "asm.chr" ) ) );
      Assert.AreEqual( "gb.chr", both.Charsets[0].ExportName, "game binary name wins" );
    }



    [TestMethod]
    public void TestLegacyFixtureScratchBlobLoads()
    {
      var container = new MapScratchContainer();
      Assert.IsTrue( container.ReadFromFile( LEGACY_SCRATCH ) );
      var entry = container.GetEntry( "legacy-guid-a" );
      Assert.IsNotNull( entry );

      var memReader = new GR.IO.MemoryReader( new ByteBuffer( entry.MapData ) );
      var outer = new GR.IO.FileChunk();
      Assert.IsTrue( outer.ReadFromStream( memReader ) );
      Assert.AreEqual( FileChunkConstants.MAP, outer.Type );
      var map = DeserializeMapChunk( outer );
      Assert.AreEqual( 0, map.CharsetIndex );
      Assert.AreEqual( "Arrival (scratch)", map.Name );
      Assert.AreEqual( 2, map.Tiles[0, 0] );
      Assert.AreEqual( 4, map.Tiles.Width );
    }



    [TestMethod]
    public void TestLegacyResaveKeepsCharsetZeroBytesIdentical()
    {
      var fixture = GR.IO.File.ReadAllBytes( LEGACY_FIXTURE );
      var proj = new MapProject();
      Assert.IsTrue( proj.ReadFromBuffer( fixture ) );
      var resaved = proj.SaveToBuffer();

      var before = TopLevelChunks( fixture );
      var after = TopLevelChunks( resaved );

      // The nested charset image: [u32 version][CHARSET_PROJECT { sub-chunks }].
      // Every sub-chunk must come back byte-identical EXCEPT the color
      // settings: the loader re-syncs MC1/MC2/BGColor4 from the project
      // colors (it always did), and the fixture's charset colors were never
      // synced before it was written.
      var nestedBefore = NestedCharsetChunks( BodyOf( before.First( c => c.Type == FileChunkConstants.MAP_CHARSET ) ) );
      var nestedAfter = NestedCharsetChunks( BodyOf( after.First( c => c.Type == FileChunkConstants.MAP_CHARSET ) ) );
      Assert.AreEqual( nestedBefore.Count, nestedAfter.Count, "nested charset sub-chunk count" );
      Assert.IsTrue( nestedBefore.Count > 250, "expected the 256 character chunks plus info/export/palette" );
      for ( int i = 0; i < nestedBefore.Count; ++i )
      {
        Assert.AreEqual( nestedBefore[i].Type, nestedAfter[i].Type, "nested sub-chunk type " + i );
        if ( nestedBefore[i].Type == FileChunkConstants.CHARSET_COLOR_SETTINGS )
        {
          continue;
        }
        CollectionAssert.AreEqual( BodyOf( nestedBefore[i] ).Data(), BodyOf( nestedAfter[i] ).Data(),
                                   "nested sub-chunk " + i + " (type 0x" + nestedBefore[i].Type.ToString( "X4" ) + ") must be byte-identical" );
      }

      var tilesBefore = SubChunks( before.First( c => c.Type == FileChunkConstants.MAP_PROJECT_DATA ) )
                          .Where( c => c.Type == FileChunkConstants.MAP_TILE ).Select( c => BodyOf( c ).Data() ).ToList();
      var tilesAfter = SubChunks( after.First( c => c.Type == FileChunkConstants.MAP_PROJECT_DATA ) )
                          .Where( c => c.Type == FileChunkConstants.MAP_TILE ).Select( c => BodyOf( c ).Data() ).ToList();
      Assert.AreEqual( 3, tilesBefore.Count );
      Assert.AreEqual( tilesBefore.Count, tilesAfter.Count );
      for ( int i = 0; i < tilesBefore.Count; ++i )
      {
        CollectionAssert.AreEqual( tilesBefore[i], tilesAfter[i], "MAP_TILE " + i + " body must be byte-identical" );
      }

      Assert.AreEqual( 0, before.Count( c => c.Type == FileChunkConstants.MAP_CHARSET_META ), "fixture is legacy" );
      Assert.AreEqual( 1, after.Count( c => c.Type == FileChunkConstants.MAP_CHARSET_META ), "resave carries META(0)" );
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( resaved ) );
      Assert.AreEqual( "legacy_asm.chr", reloaded.Charsets[0].ExportName, "migrated value now persisted" );
    }



    [TestMethod]
    public void TestMetaWinsOverLegacyStrings()
    {
      var legacy = new MapProject();
      legacy.Charsets[0].RightClickAction = "Wall";
      var legacyInfo = TopLevelChunks( legacy.SaveToBuffer() ).First( c => c.Type == FileChunkConstants.MAP_PROJECT_INFO );

      var current = new MapProject();
      current.Charsets[0].RightClickAction = "X";
      var chunks = TopLevelChunks( current.SaveToBuffer() ).Where( c => c.Type != FileChunkConstants.MAP_PROJECT_INFO ).ToList();
      chunks.Insert( 0, legacyInfo );   // legacy slot says "Wall", META says "X"

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Rebuild( chunks ) ) );
      Assert.AreEqual( "X", reloaded.Charsets[0].RightClickAction );
    }



    [TestMethod]
    public void TestMapInfoWithoutCharsetByteDefaultsToZero()
    {
      var map = MakeMap( "M", 2, 2, 0, 3 );
      map.NotExported = true;
      var mapChunk = MapProject.BuildMapChunk( map, false );

      var oneShort = DeserializeMapChunk( TruncateSubChunk( mapChunk, FileChunkConstants.MAP_INFO, 1 ) );
      Assert.AreEqual( 0, oneShort.CharsetIndex, "missing byte = charset 0" );
      Assert.IsTrue( oneShort.NotExported, "the previous field is intact" );

      var twoShort = DeserializeMapChunk( TruncateSubChunk( mapChunk, FileChunkConstants.MAP_INFO, 2 ) );
      Assert.AreEqual( 0, twoShort.CharsetIndex );
      Assert.IsFalse( twoShort.NotExported );

      var intact = DeserializeMapChunk( mapChunk );
      Assert.AreEqual( 3, intact.CharsetIndex );
    }



    [TestMethod]
    public void TestCloneMapCarriesCharsetIndex()
    {
      var map = MakeMap( "M", 2, 2, 0, 2 );
      Assert.AreEqual( 2, MapProject.CloneMap( map ).CharsetIndex );

      var proj = BuildTwoCharsetProject();
      Assert.AreEqual( 2, proj.AddCharset() );
      proj.Maps[0].CharsetIndex = 2;
      proj.Maps[0].Revisions.Add( new MapProject.MapRevision() { Name = "rev", Snapshot = MapProject.CloneMap( proj.Maps[1] ) } );
      var reloaded = RoundTrip( proj );
      Assert.AreEqual( 2, reloaded.Maps[0].CharsetIndex );
      Assert.AreEqual( 1, reloaded.Maps[0].Revisions[0].Snapshot.CharsetIndex );
    }



    [TestMethod]
    public void TestEntityTypeWithoutPreviewByteDefaultsToZero()
    {
      var proj = BuildTwoCharsetProject();
      var chunks = TopLevelChunks( proj.SaveToBuffer() );
      var rebuilt = new List<GR.IO.FileChunk>();
      foreach ( var chunk in chunks )
      {
        rebuilt.Add( chunk.Type == FileChunkConstants.MAP_PROJECT_DATA
                     ? TruncateSubChunk( chunk, FileChunkConstants.MAP_ENTITY_TYPES, 1 )
                     : chunk );
      }
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Rebuild( rebuilt ) ) );
      Assert.AreEqual( 0, reloaded.EntityTypes[0].PreviewCharsetIndex, "missing byte = charset 0" );
      Assert.AreEqual( 5, reloaded.EntityTypes[0].TagID, "previous field intact" );
      Assert.AreEqual( 1, RoundTrip( proj ).EntityTypes[0].PreviewCharsetIndex, "present byte = value" );
    }



    // ---------------------------------------------------------------- lookups

    [TestMethod]
    public void TestCharsetAtClamps()
    {
      var proj = BuildTwoCharsetProject();
      Assert.AreSame( proj.Charsets[0], proj.CharsetAt( -1 ) );
      Assert.AreSame( proj.Charsets[0], proj.CharsetAt( 99 ) );
      Assert.AreSame( proj.Charsets[1], proj.CharsetAt( 1 ) );
      Assert.AreSame( proj.Charsets[0], proj.CharsetOf( null ) );
      Assert.AreSame( proj.Charsets[0], proj.CharsetOf( MakeMap( "X", 1, 1, 0, 7 ) ) );
      Assert.AreSame( proj.Charsets[1], proj.CharsetOf( proj.Maps[1] ) );
    }



    [TestMethod]
    public void TestMapsUsingCharset()
    {
      var proj = BuildTwoCharsetProject();
      proj.Maps.Add( MakeMap( "Dangling", 1, 1, 0, 9 ) );   // out of range counts as charset 0
      var users0 = proj.MapsUsingCharset( 0 );
      Assert.AreEqual( 2, users0.Count );
      Assert.AreEqual( "A", users0[0].Name );
      Assert.AreEqual( "Dangling", users0[1].Name );
      var users1 = proj.MapsUsingCharset( 1 );
      Assert.AreEqual( 1, users1.Count );
      Assert.AreEqual( "B", users1[0].Name );
      Assert.AreEqual( 0, proj.MapsUsingCharset( 5 ).Count );
    }



    [TestMethod]
    public void TestReadIntoReusedInstanceDoesNotConcatenateTiles()
    {
      var file = BuildTwoCharsetProject().SaveToBuffer();
      var proj = new MapProject();
      var slot0 = proj.Charsets[0];
      var art0 = proj.Charsets[0].Charset;
      Assert.IsTrue( proj.ReadFromBuffer( file ) );
      Assert.IsTrue( proj.ReadFromBuffer( file ) );
      Assert.AreEqual( 2, proj.Charsets.Count );
      Assert.AreEqual( 2, proj.Charsets[0].Tiles.Count, "charset 0 tiles must not concatenate" );
      Assert.AreEqual( 3, proj.Charsets[1].Tiles.Count );
      Assert.AreSame( slot0, proj.Charsets[0], "slot 0 object identity survives a reload" );
      Assert.AreSame( art0, proj.Charsets[0].Charset, "slot 0 charset object identity survives a reload" );
    }



    [TestMethod]
    public void TestClearResetsToSingleDefault()
    {
      var proj = BuildTwoCharsetProject();
      proj.AddCharset();
      var slot0 = proj.Charsets[0];
      proj.Clear();
      Assert.AreEqual( 1, proj.Charsets.Count );
      Assert.AreSame( slot0, proj.Charsets[0] );
      Assert.AreEqual( 0, proj.Charsets[0].Tiles.Count );
      Assert.AreEqual( "", proj.Charsets[0].DisplayName );
      Assert.AreEqual( "", proj.Charsets[0].ExportName );
      Assert.IsTrue( proj.Charsets[0].ExportEnabled );
      Assert.AreEqual( 0, proj.CurrentCharsetIndex );
      Assert.IsTrue( proj.CharsetTabFollowsMap );
    }



    // ---------------------------------------------------------------- add / duplicate / remove

    [TestMethod]
    public void TestAddCharsetCopiesModeAndColors()
    {
      var proj = new MapProject();
      var cs0 = proj.Charsets[0].Charset;
      cs0.Mode = TextCharMode.COMMODORE_MULTICOLOR;
      cs0.Colors.BackgroundColor = 6;
      cs0.Colors.MultiColor1 = 2;
      cs0.Colors.MultiColor2 = 3;
      cs0.Colors.BGColor4 = 4;
      cs0.Colors.Palettes.Add( new Palette( cs0.Colors.Palettes[0] ) );
      cs0.Characters[1].Tile.Data.SetU8At( 0, 0xEE );   // must NOT be copied: a fresh charset starts from the default font

      int index = proj.AddCharset();
      Assert.AreEqual( 1, index );
      var cs1 = proj.Charsets[1];
      Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, cs1.Charset.Mode );
      Assert.AreEqual( 6, cs1.Charset.Colors.BackgroundColor );
      Assert.AreEqual( 2, cs1.Charset.Colors.MultiColor1 );
      Assert.AreEqual( 3, cs1.Charset.Colors.MultiColor2 );
      Assert.AreEqual( 4, cs1.Charset.Colors.BGColor4 );
      Assert.AreEqual( cs0.Colors.Palettes.Count, cs1.Charset.Colors.Palettes.Count );
      Assert.AreNotSame( cs0.Colors.Palettes[0], cs1.Charset.Colors.Palettes[0], "palettes are deep copies" );
      Assert.AreEqual( 0, cs1.Tiles.Count );
      Assert.IsTrue( cs1.ExportEnabled );
      AssertDefaultFont( cs1.Charset, "added charset" );
    }



    [TestMethod]
    public void TestAddCharsetCapsAt256()
    {
      var proj = new MapProject();
      for ( int i = 1; i < 256; ++i )
      {
        Assert.AreEqual( i, proj.AddCharset() );
      }
      Assert.AreEqual( 256, proj.Charsets.Count );
      Assert.AreEqual( -1, proj.AddCharset() );
      Assert.AreEqual( -1, proj.DuplicateCharset( 0 ) );
      Assert.AreEqual( 256, proj.Charsets.Count );
    }



    [TestMethod]
    public void TestDuplicateCharsetIsIndependent()
    {
      var proj = BuildTwoCharsetProject();
      int index = proj.DuplicateCharset( 1 );
      Assert.AreEqual( 2, index );
      var original = proj.Charsets[1];
      var copy = proj.Charsets[2];

      Assert.AreEqual( "Sprites (copy)", copy.DisplayName );
      Assert.AreEqual( "", copy.ExportName, "export name is cleared" );
      Assert.AreEqual( original.ExportEnabled, copy.ExportEnabled );
      Assert.AreEqual( "Lava", copy.RightClickAction );
      Assert.AreEqual( 3, copy.Tiles.Count );
      for ( int t = 0; t < 3; ++t )
      {
        AssertTilesEqual( original.Tiles[t], copy.Tiles[t], "copied tile " + t );
        Assert.AreEqual( t, copy.Tiles[t].Index );
      }
      Assert.AreEqual( (byte)0xA1, copy.Charset.Characters[2].Tile.Data.ByteAt( 1 ) );

      copy.Charset.Characters[3].Tile.Data.SetU8At( 0, 0x55 );
      copy.Tiles[0].Name = "Changed";
      copy.Tiles[0].Chars[0, 0].Character = 0x11;
      Assert.AreNotEqual( (byte)0x55, original.Charset.Characters[3].Tile.Data.ByteAt( 0 ), "art is independent" );
      Assert.AreEqual( "Rock", original.Tiles[0].Name, "tile list is independent" );
      Assert.AreEqual( (byte)0x60, original.Tiles[0].Chars[0, 0].Character );

      Assert.AreEqual( -1, proj.DuplicateCharset( 9 ) );
    }



    private static MapProject BuildFourCharsetProject()
    {
      var proj = new MapProject();
      Assert.AreEqual( 1, proj.AddCharset() );
      Assert.AreEqual( 2, proj.AddCharset() );
      Assert.AreEqual( 3, proj.AddCharset() );
      proj.Maps.Add( MakeMap( "M0", 1, 1, 0, 0 ) );
      proj.Maps.Add( MakeMap( "M2", 1, 1, 0, 2 ) );
      proj.Maps.Add( MakeMap( "M3", 1, 1, 0, 3 ) );
      proj.Maps[0].Revisions.Add( new MapProject.MapRevision() { Name = "r", Snapshot = MakeMap( "snap", 1, 1, 0, 1 ) } );
      proj.EntityTypes.Add( new MapProject.EntityType() { ID = 0, Name = "E1", PreviewCharsetIndex = 1 } );
      proj.EntityTypes.Add( new MapProject.EntityType() { ID = 1, Name = "E3", PreviewCharsetIndex = 3 } );
      proj.CurrentCharsetIndex = 3;
      return proj;
    }



    [TestMethod]
    public void TestRemoveCharsetShiftsIndexes()
    {
      var proj = BuildFourCharsetProject();
      var removed = proj.Charsets[1];
      Assert.IsTrue( proj.RemoveCharset( 1 ) );
      Assert.AreEqual( 3, proj.Charsets.Count );
      Assert.IsFalse( proj.Charsets.Contains( removed ) );
      Assert.AreEqual( 0, proj.Maps[0].CharsetIndex );
      Assert.AreEqual( 1, proj.Maps[1].CharsetIndex );
      Assert.AreEqual( 2, proj.Maps[2].CharsetIndex );
      Assert.AreEqual( 0, proj.Maps[0].Revisions[0].Snapshot.CharsetIndex, "a snapshot that pointed at the removed charset falls back to 0" );
      Assert.AreEqual( 0, proj.EntityTypes[0].PreviewCharsetIndex, "a preview that pointed at it falls back to 0" );
      Assert.AreEqual( 2, proj.EntityTypes[1].PreviewCharsetIndex );
      Assert.AreEqual( 2, proj.CurrentCharsetIndex );
    }



    [TestMethod]
    public void TestRemoveCharsetAdditionalMaps()
    {
      var proj = BuildFourCharsetProject();
      var scratchHigh = MakeMap( "scratch3", 1, 1, 0, 3 );
      var scratchHit = MakeMap( "scratch1", 1, 1, 0, 1 );
      Assert.IsTrue( proj.RemoveCharset( 1, new[] { scratchHigh, scratchHit } ) );
      Assert.AreEqual( 2, scratchHigh.CharsetIndex );
      Assert.AreEqual( 0, scratchHit.CharsetIndex );

      var lone = MakeMap( "lone", 1, 1, 0, 2 );
      lone.Revisions.Add( new MapProject.MapRevision() { Name = "r", Snapshot = MakeMap( "s", 1, 1, 0, 3 ) } );
      MapProject.RemapCharsetIndexAfterRemoval( lone, 1 );
      Assert.AreEqual( 1, lone.CharsetIndex );
      Assert.AreEqual( 2, lone.Revisions[0].Snapshot.CharsetIndex );
      MapProject.RemapCharsetIndexAfterInsert( lone, 1 );
      Assert.AreEqual( 2, lone.CharsetIndex );
      Assert.AreEqual( 3, lone.Revisions[0].Snapshot.CharsetIndex );
    }



    [TestMethod]
    public void TestCanRemoveCharsetRefusals()
    {
      List<string> users;
      var single = new MapProject();
      Assert.IsFalse( single.CanRemoveCharset( 0, out users ), "the last charset can never be removed" );
      Assert.IsFalse( single.RemoveCharset( 0 ) );
      Assert.AreEqual( 1, single.Charsets.Count );

      var proj = BuildTwoCharsetProject();
      Assert.IsFalse( proj.CanRemoveCharset( 1, out users ), "map B uses charset 1" );
      Assert.AreEqual( 1, users.Count );
      Assert.AreEqual( "B", users[0] );
      Assert.IsFalse( proj.RemoveCharset( 1 ) );
      Assert.AreEqual( 2, proj.Charsets.Count, "a refused removal changes nothing" );
      Assert.AreEqual( 1, proj.Maps[1].CharsetIndex );

      // Revisions and scratch maps never block.
      proj.Maps[1].CharsetIndex = 0;
      proj.Maps[1].Revisions.Add( new MapProject.MapRevision() { Name = "r", Snapshot = MakeMap( "s", 1, 1, 0, 1 ) } );
      Assert.IsTrue( proj.CanRemoveCharset( 1, out users ) );
      Assert.AreEqual( 0, users.Count );
      Assert.IsFalse( proj.CanRemoveCharset( 5, out users ), "out of range" );
    }



    [TestMethod]
    public void TestInsertCharsetIsInverseOfRemove()
    {
      var proj = BuildFourCharsetProject();
      proj.EntityTypes[0].PreviewCharsetIndex = 2;   // nothing may point AT the removed charset for an exact inverse
      proj.Maps[0].Revisions[0].Snapshot.CharsetIndex = 3;
      var mapIndexes = proj.Maps.Select( m => m.CharsetIndex ).ToList();
      var entityIndexes = proj.EntityTypes.Select( e => e.PreviewCharsetIndex ).ToList();
      var removed = proj.Charsets[1];

      Assert.IsTrue( proj.RemoveCharset( 1 ) );
      proj.InsertCharset( 1, removed );

      Assert.AreEqual( 4, proj.Charsets.Count );
      Assert.AreSame( removed, proj.Charsets[1] );
      CollectionAssert.AreEqual( mapIndexes, proj.Maps.Select( m => m.CharsetIndex ).ToList() );
      CollectionAssert.AreEqual( entityIndexes, proj.EntityTypes.Select( e => e.PreviewCharsetIndex ).ToList() );
      Assert.AreEqual( 3, proj.Maps[0].Revisions[0].Snapshot.CharsetIndex );
      Assert.AreEqual( 3, proj.CurrentCharsetIndex );
    }



    // ---------------------------------------------------------------- export-facing helpers

    [TestMethod]
    public void TestGetExportTileIndexPerCharset()
    {
      var proj = BuildTwoCharsetProject();
      proj.Settings.Assembly.EmptyTileIndex = 0;
      var cs0 = proj.Charsets[0];
      var cs1 = proj.Charsets[1];
      Assert.AreEqual( 1, proj.GetExportTileIndex( cs0, 1 ) );
      Assert.AreEqual( 0, proj.GetExportTileIndex( cs1, 2 ), "NotExportedOnMap tile becomes the empty tile" );
      Assert.AreEqual( 1, proj.GetExportTileIndex( cs1, 1 ) );
      Assert.AreEqual( 0, proj.GetExportTileIndex( cs0, 5 ), "index beyond the tile count becomes the empty tile" );
      Assert.AreEqual( -1, proj.GetExportTileIndex( cs1, -1 ), "-1 passes through" );
      Assert.AreEqual( 0, proj.GetExportTileIndex( proj.Maps[1], 2 ), "map B resolves through charset 1" );
      Assert.AreEqual( 1, proj.GetExportTileIndex( proj.Maps[0], 1 ) );
      proj.Settings.Assembly.EmptyTileIndex = 1;
      Assert.AreEqual( 1, proj.GetExportTileIndex( cs0, 9 ) );
    }



    [TestMethod]
    public void TestExportedCharsetsCompaction()
    {
      var proj = BuildTwoCharsetProject();
      Assert.AreEqual( 2, proj.AddCharset() );
      proj.Maps.Add( MakeMap( "C", 1, 1, 0, 2 ) );
      // charset 1 is disabled in the builder
      var exported = proj.ExportedCharsets();
      Assert.AreEqual( 2, exported.Count );
      Assert.AreSame( proj.Charsets[0], exported[0] );
      Assert.AreSame( proj.Charsets[2], exported[1] );
      Assert.AreEqual( 0, proj.ExportCharsetIndex( 0 ) );
      Assert.AreEqual( -1, proj.ExportCharsetIndex( 1 ) );
      Assert.AreEqual( 1, proj.ExportCharsetIndex( 2 ) );
      Assert.AreEqual( -1, proj.ExportCharsetIndex( 99 ) );
      Assert.AreEqual( 1, proj.ExportedCharsetIndex( proj.Maps[2] ) );
      Assert.AreEqual( -1, proj.ExportedCharsetIndex( proj.Maps[1] ), "map on a disabled charset" );
    }



    [TestMethod]
    public void TestSyncCharsetColorsFromProject()
    {
      var proj = BuildTwoCharsetProject();
      proj.MultiColor1 = 5;
      proj.MultiColor2 = 6;
      proj.BGColor4 = 7;
      foreach ( var cs in proj.Charsets )
      {
        cs.Charset.Colors.BackgroundColor = 9;
        cs.Charset.Mode = TextCharMode.COMMODORE_MULTICOLOR;
      }
      proj.SyncCharsetColorsFromProject();
      foreach ( var cs in proj.Charsets )
      {
        Assert.AreEqual( 5, cs.Charset.Colors.MultiColor1 );
        Assert.AreEqual( 6, cs.Charset.Colors.MultiColor2 );
        Assert.AreEqual( 7, cs.Charset.Colors.BGColor4 );
        Assert.AreEqual( 9, cs.Charset.Colors.BackgroundColor, "background color is not touched by the sync" );
        Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, cs.Charset.Mode, "mode is not touched by the sync" );
      }
      proj.SetAllCharsetsBackgroundColor( 2 );
      Assert.AreEqual( 2, proj.BackgroundColor );
      proj.SetAllCharsetsMode( TextCharMode.COMMODORE_HIRES );
      foreach ( var cs in proj.Charsets )
      {
        Assert.AreEqual( 2, cs.Charset.Colors.BackgroundColor );
        Assert.AreEqual( TextCharMode.COMMODORE_HIRES, cs.Charset.Mode );
      }
    }



    [TestMethod]
    public void TestOldBuildResaveSimulation()
    {
      // An older build drops the chunks it does not know (ENTRY/META) and
      // the maps keep their stale index bytes: the reload must land on one
      // charset with every map clamped to it.
      var proj = BuildTwoCharsetProject();
      var chunks = TopLevelChunks( proj.SaveToBuffer() )
                     .Where( c => ( c.Type != FileChunkConstants.MAP_CHARSET_ENTRY ) && ( c.Type != FileChunkConstants.MAP_CHARSET_META ) );
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Rebuild( chunks ) ) );
      Assert.AreEqual( 1, reloaded.Charsets.Count );
      Assert.AreEqual( 2, reloaded.Charsets[0].Tiles.Count );
      Assert.AreEqual( 0, reloaded.Maps[1].CharsetIndex, "index into a missing charset clamps to 0" );
      Assert.AreEqual( 0, reloaded.EntityTypes[0].PreviewCharsetIndex );
      Assert.AreEqual( 0, reloaded.CurrentCharsetIndex );
    }
  }
}
