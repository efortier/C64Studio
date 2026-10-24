using System.Collections.Generic;
using System.Linq;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// GameBinaryLayout: what the game-binary writer records as it writes.
  /// Handing a capture in never changes a byte; every recorded marker,
  /// entity and string equals the bytes in the file at the recorded offset,
  /// in the file's order; and the label helpers the sidecars iterate give
  /// exactly the sidecars' labels.
  /// </summary>
  [TestClass]
  public class TestGameBinaryLayoutCapture
  {
    const int HDR_TILE_COUNT        = 0x01;
    const int HDR_MAP_WIDTH         = 0x12;
    const int HDR_MAP_HEIGHT        = 0x14;
    const int HDR_MAP_BG_COLOR      = 0x16;
    const int HDR_MAP_MC1_COLOR     = 0x18;
    const int HDR_MAP_MC2_COLOR     = 0x1A;
    const int HDR_MAP_MARKER_COUNT  = 0x1C;
    const int HDR_MAP_CHAR_GRID_LO  = 0x1E;
    const int HDR_MAP_CHAR_GRID_HI  = 0x20;
    const int HDR_MAP_COLOR_GRID_LO = 0x22;
    const int HDR_MAP_COLOR_GRID_HI = 0x24;
    const int HDR_MAP_PASSABLE_LO   = 0x26;
    const int HDR_MAP_PASSABLE_HI   = 0x28;
    const int HDR_MAP_MARKERS_LO    = 0x2A;
    const int HDR_MAP_MARKERS_HI    = 0x2C;
    const int HDR_MAP_ENTITY_COUNT  = 0x2F;
    const int HDR_MAP_ENTITIES_LO   = 0x31;
    const int HDR_MAP_ENTITIES_HI   = 0x33;
    const int HDR_MAP_STRING_COUNT  = 0x35;
    const int HDR_MAP_STRING_LO     = 0x36;
    const int HDR_MAP_STRING_HI     = 0x38;
    const int HDR_MAP_STRING_ID     = 0x3A;
    const int HDR_MAP_CHARSET_INDEX = 0x3C;
    const int HDR_CHARSET_DIRECTORY = 0x3F;

    const int TYPE_MESSAGE  = 0;
    const int TYPE_SPAWN    = 1;
    const int TYPE_DOOR     = 2;
    const int TYPE_UNKNOWN  = 99;

    const int ENTITY_TICK    = 0;
    const int ENTITY_RAT     = 1;
    const int ENTITY_UNKNOWN = 7;



    // ---------------------------------------------------------------- helpers

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



    private static MapProject.Map AddMap( MapProject Project, string Name, int CharsetIndex, int Width, int Height, int Fill )
    {
      var map = new MapProject.Map() { Name = Name, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
      map.MapID = Project.FindFreeMapID( null );
      map.Tiles.Resize( Width, Height );
      for ( int y = 0; y < Height; ++y )
      {
        for ( int x = 0; x < Width; ++x )
        {
          map.Tiles[x, y] = Fill;
        }
      }
      Project.Maps.Add( map );
      return map;
    }



    /// <summary>
    /// Charset 0 (3 tiles) and charset 1 (2 tiles); maps Alpha (cs0, 3x2,
    /// alternative background colour, ID 1), Hidden (not exported, ID 2) and
    /// Bravo (cs1, 2x3, ID 3). Marker types message/spawn/door plus a second
    /// type sharing the door's ID (the writer takes the first). Entity types
    /// Tick (tile 1 in cs0, none in cs1) and Rat (tile 2 in cs0, tile 0 in
    /// cs1). Alpha: a 2x2 spawn, a door, a message showing string 5, a marker
    /// of an unknown type; entities Rat, Tick and one of an unknown type.
    /// Bravo: Tick (no tile there) and Rat. Strings WELCOME 5 (Alpha shows it)
    /// and SIGN 7 (unused).
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 2, 0x20 );
      project.BackgroundColor = 6;
      project.MultiColor1 = 4;
      project.MultiColor2 = 14;

      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_MESSAGE, Name = MapProject.MARKER_TYPE_SHOW_MAP_MESSAGE, TagID = 1, ExportSymbol = "MESSAGE" } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_SPAWN, Name = "Spawn", TagID = 5, ExportSymbol = "SPAWN" } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_DOOR, Name = "Door", TagID = 3, ExportSymbol = "DOOR" } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_DOOR, Name = "Door duplicate", TagID = 9 } );

      var tick = new MapProject.EntityType() { ID = ENTITY_TICK, Name = "Tick", TagID = 1, ExportSymbol = "TICK" };
      var rat  = new MapProject.EntityType() { ID = ENTITY_RAT, Name = "Rat", TagID = 2, ExportSymbol = "RAT" };
      project.EntityTypes.Add( tick );
      project.EntityTypes.Add( rat );
      project.Charsets[0].SetEntityTile( tick, 1 );
      project.Charsets[0].SetEntityTile( rat, 2 );
      project.Charsets[second].SetEntityTile( rat, 0 );

      project.MapStrings.Add( new MapProject.MapString() { Label = "WELCOME", StringID = 5 } );
      project.MapStrings[0].Lines[0].Text = "HELLO";
      project.MapStrings.Add( new MapProject.MapString() { Label = "SIGN", StringID = 7 } );

      var alpha = AddMap( project, "Alpha", 0, 3, 2, 1 );
      alpha.AlternativeBackgroundColor = 3;
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_SPAWN, X = 1, Y = 0, Width = 2, Height = 2, Value1 = 11, Value2 = 22, Value3 = 33, Value4 = 44, GroupId = 2, LinkToID = 8, LinkID = 9, Triggered = true, AutoDisableGroupAfterTrigger = true } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_DOOR, X = 0, Y = 1, Value1 = 3, Enabled = false } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_MESSAGE, X = 2, Y = 1, Value1 = 5 } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_UNKNOWN, X = 0, Y = 0, Value1 = 77 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_RAT, X = 0, Y = 0, Value1 = 1, Value2 = 2 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_TICK, X = 1, Y = 1, Enabled = false, Triggered = true } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_UNKNOWN, X = 2, Y = 0 } );

      AddMap( project, "Hidden", 0, 2, 2, 0 ).NotExported = true;

      var bravo = AddMap( project, "Bravo", second, 2, 3, 1 );
      bravo.Entities.Add( new MapProject.Entity() { Type = ENTITY_TICK, X = 0, Y = 2 } );
      bravo.Entities.Add( new MapProject.Entity() { Type = ENTITY_RAT, X = 1, Y = 2 } );
      return project;
    }



    private static List<MapProject.GameBinaryMapFile> Export( MapProject Project, bool Markers = true, bool Colors = true, bool Passable = true )
    {
      List<string> errors;
      var files = Project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.bin", Markers, Colors, Passable, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      Assert.AreEqual( 0, errors.Count );
      return files;
    }



    /// <summary>Map 0's entry of a per-map lo/hi pointer table pair.</summary>
    private static int FirstMapPointer( ByteBuffer Binary, int HdrLo, int HdrHi )
    {
      return Binary.ByteAt( Binary.UInt16At( HdrLo ) ) | ( Binary.ByteAt( Binary.UInt16At( HdrHi ) ) << 8 );
    }



    private static int FirstMapByte( ByteBuffer Binary, int HdrTable )
    {
      return Binary.ByteAt( Binary.UInt16At( HdrTable ) );
    }



    // ------------------------------------------------------------ byte parity

    [TestMethod]
    public void TestCapturingChangesNoByte()
    {
      var project = BuildProject();
      foreach ( var map in project.ExportedMaps() )
      {
        List<string> plainErrors, capturedErrors;
        var plain    = project.ExportMapAsGameBinary( map, true, true, true, out plainErrors );
        var captured = project.ExportMapAsGameBinary( map, true, true, true, out capturedErrors, new GameBinaryLayout() );
        CollectionAssert.AreEqual( plain.Data(), captured.Data(), map.Name );
      }
      foreach ( var file in Export( project ) )
      {
        List<string> errors;
        CollectionAssert.AreEqual( project.ExportMapAsGameBinary( file.Map, true, true, true, out errors ).Data(), file.Data.Data(), file.FileName );
      }
    }



    [TestMethod]
    public void TestEveryExportedFileCarriesItsLayout()
    {
      var files = Export( BuildProject() );
      Assert.AreEqual( 2, files.Count );
      foreach ( var file in files )
      {
        Assert.IsNotNull( file.Layout, file.FileName );
        Assert.AreEqual( (int)file.Data.Length, file.Layout.Length );
        Assert.AreEqual( 1, file.Layout.Maps.Count, "one map per file" );
        Assert.AreSame( file.Map, file.Layout.Maps[0].Map );
        Assert.AreEqual( 0, file.Layout.Maps[0].ExportIndex );
        Assert.AreEqual( 11, file.Layout.MarkerStride );
        Assert.AreEqual( 9, file.Layout.EntityStride );
        Assert.AreEqual( file.Data.ByteAt( HDR_TILE_COUNT ), file.Layout.TileCount );
        Assert.AreEqual( file.Data.UInt16At( HDR_CHARSET_DIRECTORY ), file.Layout.CharsetDirectoryOffset );
      }
    }



    [TestMethod]
    public void TestMapFieldsMatchTheHeaderTables()
    {
      var files = Export( BuildProject() );
      var alpha = files[0];
      var layout = alpha.Layout.Maps[0];
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_WIDTH ), layout.Width );
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_HEIGHT ), layout.Height );
      Assert.AreEqual( 3, layout.Width );
      Assert.AreEqual( 2, layout.Height );
      Assert.AreEqual( 3, layout.BackgroundColor, "Alpha's alternative background colour" );
      Assert.AreEqual( 4, layout.MultiColor1, "the project's multicolour 1" );
      Assert.AreEqual( 14, layout.MultiColor2 );
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_BG_COLOR ), layout.BackgroundColor );
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_MC1_COLOR ), layout.MultiColor1 );
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_MC2_COLOR ), layout.MultiColor2 );
      Assert.AreEqual( 0, layout.CharsetIndex, "the only charset in a per-map file" );
      Assert.AreEqual( FirstMapByte( alpha.Data, HDR_MAP_CHARSET_INDEX ), layout.CharsetIndex );
      Assert.AreEqual( FirstMapPointer( alpha.Data, HDR_MAP_CHAR_GRID_LO, HDR_MAP_CHAR_GRID_HI ), layout.CharGridOffset );
      Assert.AreEqual( FirstMapPointer( alpha.Data, HDR_MAP_COLOR_GRID_LO, HDR_MAP_COLOR_GRID_HI ), layout.ColorGridOffset );
      Assert.AreEqual( FirstMapPointer( alpha.Data, HDR_MAP_PASSABLE_LO, HDR_MAP_PASSABLE_HI ), layout.PassableOffset );
      Assert.AreEqual( FirstMapPointer( alpha.Data, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI ), layout.MarkersOffset );
      Assert.AreEqual( FirstMapPointer( alpha.Data, HDR_MAP_ENTITIES_LO, HDR_MAP_ENTITIES_HI ), layout.EntitiesOffset );

      var bravo = files[1].Layout.Maps[0];
      Assert.AreEqual( 6, bravo.BackgroundColor, "Bravo uses the project's background colour" );
      // Marker export writes a (possibly empty) marker section for every map.
      Assert.AreEqual( FirstMapPointer( files[1].Data, HDR_MAP_MARKERS_LO, HDR_MAP_MARKERS_HI ), bravo.MarkersOffset, "Bravo's empty marker section" );
      Assert.AreEqual( 0, bravo.Markers.Count, "Bravo has no markers" );
    }



    [TestMethod]
    public void TestMarkerRecordsMatchTheBytes()
    {
      var alpha = Export( BuildProject() )[0];
      var layout = alpha.Layout.Maps[0];
      var data = alpha.Data;
      int count = FirstMapByte( data, HDR_MAP_MARKER_COUNT );
      Assert.AreEqual( 7, count, "4 spawn cells + door + message + unknown" );
      Assert.AreEqual( count, layout.Markers.Count );

      for ( int i = 0; i < count; ++i )
      {
        var rec = layout.Markers[i];
        Assert.AreEqual( i, rec.Index );
        Assert.AreEqual( layout.MarkersOffset + i * 11, rec.Offset );
        Assert.AreEqual( data.ByteAt( rec.Offset + 0 ), rec.Tag, "tag " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 1 ), rec.Marker.X, "x " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 2 ), rec.Marker.Y, "y " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 3 ), rec.Marker.Value1, "value1 " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 4 ), rec.Marker.Value2, "value2 " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 5 ), rec.Flags, "flags " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 6 ), rec.Marker.GroupId, "group " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 7 ), rec.Marker.LinkToID, "link to " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 8 ), rec.Marker.LinkID, "link " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 9 ), rec.Marker.Value3, "value3 " + i );
        Assert.AreEqual( data.ByteAt( rec.Offset + 10 ), rec.Marker.Value4, "value4 " + i );
        Assert.AreEqual( rec.Marker.Enabled, ( rec.Flags & 1 ) != 0 );
        Assert.AreEqual( rec.Marker.Triggered, ( rec.Flags & 2 ) != 0 );
        Assert.AreEqual( rec.Marker.AutoDisableGroupAfterTrigger, ( rec.Flags & 4 ) != 0 );
      }

      CollectionAssert.AreEqual( new byte[] { 0, 1, 3, 5, 5, 5, 5 }, layout.Markers.Select( r => r.Tag ).ToArray(), "sorted by tag; unknown type = 0; the door takes the FIRST type with its ID" );
      var spawnCells = layout.Markers.Where( r => r.Tag == 5 ).Select( r => r.Marker.X + "," + r.Marker.Y ).ToArray();
      CollectionAssert.AreEquivalent( new[] { "1,0", "2,0", "1,1", "2,1" }, spawnCells, "the 2x2 spawn is one record per cell" );
      Assert.IsTrue( layout.Markers.Where( r => r.Tag == 5 ).All( r => ( r.Marker.Width == 1 ) && ( r.Marker.Height == 1 ) ), "expanded records are 1x1" );
      Assert.AreEqual( 77, layout.Markers[0].Marker.Value1, "the unknown-type marker keeps its values" );
    }



    [TestMethod]
    public void TestEntityRecordsMatchTheBytes()
    {
      var files = Export( BuildProject() );
      foreach ( var file in files )
      {
        var layout = file.Layout.Maps[0];
        var data = file.Data;
        int count = FirstMapByte( data, HDR_MAP_ENTITY_COUNT );
        Assert.AreEqual( count, layout.Entities.Count, file.FileName );
        for ( int i = 0; i < count; ++i )
        {
          var rec = layout.Entities[i];
          Assert.AreEqual( i, rec.Index );
          Assert.AreEqual( layout.EntitiesOffset + i * 9, rec.Offset );
          Assert.AreEqual( data.ByteAt( rec.Offset + 0 ), rec.Index, "index byte " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 1 ), rec.Tag, "tag " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 2 ), rec.Entity.X, "x " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 3 ), rec.Entity.Y, "y " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 4 ), rec.Tile, "tile " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 5 ), rec.Entity.Value1, "value1 " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 6 ), rec.Entity.Value2, "value2 " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 7 ) != 0, rec.Entity.Enabled, "enabled " + i );
          Assert.AreEqual( data.ByteAt( rec.Offset + 8 ) != 0, rec.Entity.Triggered, "triggered " + i );
        }
      }

      var alpha = files[0].Layout.Maps[0];
      CollectionAssert.AreEqual( new byte[] { 0, 1, 2 }, alpha.Entities.Select( r => r.Tag ).ToArray(), "sorted by tag; unknown type = 0" );
      CollectionAssert.AreEqual( new byte[] { 0, 1, 2 }, alpha.Entities.Select( r => r.Tile ).ToArray(), "unknown type = tile 0; Tick tile 1, Rat tile 2 in charset 0" );

      var bravo = files[1].Layout.Maps[0];
      CollectionAssert.AreEqual( new byte[] { 1, 2 }, bravo.Entities.Select( r => r.Tag ).ToArray() );
      CollectionAssert.AreEqual( new byte[] { 255, 0 }, bravo.Entities.Select( r => r.Tile ).ToArray(), "Tick has no tile in charset 1 ($FF); Rat has tile 0 there" );
    }



    [TestMethod]
    public void TestStringTableMatchesTheBytes()
    {
      var project = BuildProject();
      var files = Export( project );
      var alpha = files[0];
      var data = alpha.Data;
      Assert.AreEqual( 1, data.ByteAt( HDR_MAP_STRING_COUNT ) );
      Assert.AreEqual( 1, alpha.Layout.Strings.Count, "Alpha shows WELCOME only" );
      var rec = alpha.Layout.Strings[0];
      Assert.AreEqual( 0, rec.Index );
      Assert.AreSame( project.MapStrings[0], rec.String );
      Assert.AreEqual( 5, data.ByteAt( data.UInt16At( HDR_MAP_STRING_ID ) ) );
      int offset = data.ByteAt( data.UInt16At( HDR_MAP_STRING_LO ) ) | ( data.ByteAt( data.UInt16At( HDR_MAP_STRING_HI ) ) << 8 );
      Assert.AreEqual( offset, rec.Offset );
      var stream = MapProject.BuildMapStringByteStream( project.MapStrings[0], project.MapStringsLowercaseIndex, project.MapStringsUppercaseIndex,
                                                        project.MapStringsNumbersIndex, project.MapStringsTextAreaWidth );
      Assert.AreEqual( (int)stream.Length, rec.Size );
      Assert.AreEqual( rec.Offset + rec.Size, (int)data.Length, "the stream is the last thing in the file" );

      Assert.AreEqual( 0, files[1].Layout.Strings.Count, "Bravo shows no string" );
    }



    [TestMethod]
    public void TestSectionsNotWrittenAreMarkedAbsent()
    {
      var files = Export( BuildProject(), Markers: false, Colors: false, Passable: false );
      var alpha = files[0].Layout.Maps[0];
      Assert.AreEqual( 0, alpha.Markers.Count );
      Assert.AreEqual( -1, alpha.MarkersOffset );
      Assert.AreEqual( -1, alpha.ColorGridOffset );
      Assert.AreEqual( -1, alpha.PassableOffset );
      Assert.IsTrue( alpha.CharGridOffset > 0, "the char grid is always written" );
      Assert.AreEqual( 3, alpha.Entities.Count, "entities are written regardless" );
    }



    [TestMethod]
    public void TestTheOrderOfEqualTagsIsTheFilesOrder()
    {
      // Many markers sharing one tag: whatever order the (unstable) sort
      // produced, the capture lists the records in the order they were
      // written, so the x values read from the file equal the capture's.
      var project = BuildProject();
      var alpha = project.Maps[0];
      alpha.Markers.Clear();
      alpha.Tiles.Resize( 20, 1 );
      for ( int x = 19; x >= 0; --x )
      {
        alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_DOOR, X = x, Y = 0, Value1 = (byte)( 100 + x ) } );
      }
      var file = Export( project )[0];
      var layout = file.Layout.Maps[0];
      Assert.AreEqual( 20, layout.Markers.Count );
      for ( int i = 0; i < 20; ++i )
      {
        Assert.AreEqual( file.Data.ByteAt( layout.MarkersOffset + i * 11 + 1 ), layout.Markers[i].Marker.X, "record " + i );
        Assert.AreEqual( file.Data.ByteAt( layout.MarkersOffset + i * 11 + 3 ), layout.Markers[i].Marker.Value1, "record " + i );
      }
    }



    // ------------------------------------------------------------------ labels

    private static List<string> SidecarLabels( string Asm, string Prefix )
    {
      var result = new List<string>();
      foreach ( var line in Asm.Split( '\n' ) )
      {
        string trimmed = line.Trim();
        if ( trimmed.StartsWith( ".const " + Prefix ) )
        {
          string rest = trimmed.Substring( ( ".const " + Prefix ).Length );
          result.Add( rest.Substring( 0, rest.IndexOf( ' ' ) ) );
        }
      }
      return result;
    }



    [TestMethod]
    public void TestMapIdLabelsAreTheMapNamesSidecarsLabels()
    {
      var project = BuildProject();
      project.Maps[0].Name = "Arrival";
      project.Maps[2].Name = "arrival";
      AddMap( project, "", 0, 1, 1, 0 );
      AddMap( project, "Caves 1", 0, 1, 1, 0 );
      AddMap( project, "Skipped", 0, 1, 1, 0 ).NotExported = true;

      var labels = project.MapIdLabels();
      Assert.AreEqual( "ARRIVAL", labels[project.Maps[0]] );
      Assert.AreEqual( "ARRIVAL_2", labels[project.Maps[2]], "same label after normalizing: de-duplicated like the sidecar" );
      Assert.IsFalse( labels.ContainsKey( project.Maps[1] ), "not exported" );
      Assert.IsFalse( labels.ContainsKey( project.Maps[3] ), "no usable name" );
      Assert.AreEqual( "CAVES_1", labels[project.Maps[4]] );
      Assert.IsFalse( labels.ContainsKey( project.Maps[5] ), "not exported" );

      var expected = project.ExportedMaps().Where( m => labels.ContainsKey( m ) ).Select( m => labels[m] ).ToList();
      var sidecar = SidecarLabels( project.GenerateMapLabelsAsm(), "MAP_ID_" );
      CollectionAssert.AreEqual( expected, sidecar, "map_names.asm iterates MapIdLabels()" );
      StringAssert.Contains( project.GenerateMapLabelsAsm(), "// map ID " + project.Maps[3].MapID + ": (no map name)" );
    }



    [TestMethod]
    public void TestCharsetIndexLabelsAreTheCharsetSidecarsLabels()
    {
      var project = BuildProject();
      project.Charsets[0].ExportName = "intro.bin";
      project.Charsets[1].ExportName = "Intro.bin";
      int third = project.AddCharset();
      project.Charsets[third].ExportName = "";
      int fourth = project.AddCharset();
      project.Charsets[fourth].ExportName = "hidden.bin";
      project.Charsets[fourth].ExportEnabled = false;

      var labels = project.CharsetIndexLabels();
      Assert.AreEqual( "INTRO", labels[project.Charsets[0]] );
      Assert.AreEqual( "INTRO_2", labels[project.Charsets[1]] );
      Assert.IsFalse( labels.ContainsKey( project.Charsets[third] ), "no export name" );
      Assert.IsFalse( labels.ContainsKey( project.Charsets[fourth] ), "not export-enabled" );

      var expected = project.ExportedCharsets().Where( c => labels.ContainsKey( c ) ).Select( c => labels[c] ).ToList();
      var sidecar = SidecarLabels( project.GenerateCharsetLabelsAsm(), "CHARSET_INDEX_" );
      CollectionAssert.AreEqual( expected, sidecar, "map_charsets.asm iterates CharsetIndexLabels()" );
    }



    [TestMethod]
    public void TestNormalizeAsLabelIsPublic()
    {
      Assert.AreEqual( "Caves_1", MapProject.NormalizeAsLabel( "Caves 1" ) );
      Assert.AreEqual( "Eglise_", MapProject.NormalizeAsLabel( "Église!" ), "diacritics stripped, punctuation replaced" );
      Assert.AreEqual( "MAP_0001", MapProject.ExportNameWithoutExtension( " MAP_0001.bin " ) );
      Assert.AreEqual( "", MapProject.ExportNameWithoutExtension( "  " ) );
    }
  }
}
