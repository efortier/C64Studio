using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Scratch-map persistence: the .mapscratch sidecar container and the
  /// full-fidelity round-trip of a Map through the nested-MAP-chunk blob
  /// it stores (BuildMapChunk → container → ReadMapFromBody).
  /// </summary>
  [TestClass]
  public class TestMapScratch
  {
    private byte[] MakeFakeBlob( byte Seed, int Length )
    {
      // The container treats blobs as opaque bytes — no decode happens at
      // this layer, so any payload exercises the round-trip.
      var data = new byte[Length];
      for ( int i = 0; i < Length; ++i )
      {
        data[i] = (byte)( Seed + i );
      }
      return data;
    }



    private MapProject.Map BuildRichMap()
    {
      var map = new MapProject.Map();
      map.Name = "Throne Room";
      map.TileSpacingX = 2;
      map.TileSpacingY = 3;
      map.Tiles.Resize( 4, 3 );
      map.TileColorOverrides.Resize( 8, 9 );
      map.CharBlockedOverrides.Resize( 8, 9 );
      map.Tiles[1, 2] = 7;
      map.TileColorOverrides[3, 4] = 11;
      map.CharBlockedOverrides[5, 6] = true;

      // Two upper layers — the editor's fixed 3-layer shape
      // (ReadMapFromBody pads to 3 via EnsureDefaultLayers regardless, so
      // matching it keeps the count assertion exact).
      var upper = new MapProject.MapLayer() { Name = "Detail", Visible = false };
      upper.Tiles.Resize( 4, 3 );
      upper.Tiles[0, 0] = 3;
      upper.TileColorOverrides.Resize( 8, 9 );
      upper.TileColorOverrides[1, 1] = 5;
      map.Layers.Add( upper );
      var top = new MapProject.MapLayer() { Name = "Overlay", Visible = true };
      top.Tiles.Resize( 4, 3 );
      top.Tiles[2, 1] = 4;
      top.TileColorOverrides.Resize( 8, 9 );
      map.Layers.Add( top );
      map.SelectedLayerIndex = 1;

      map.Markers.Add( new MapProject.Marker()
      {
        X = 2, Y = 1, Type = 4, Name = "door",
        Value1 = 9, Value2 = 8, Value3 = 7, Value4 = 6,
        Enabled = false, Triggered = true,
        AutoDisableGroupAfterTrigger = true,
        GroupId = 3, LinkToID = 2, LinkID = 1,
        Width = 2, Height = 2
      } );
      map.Entities.Add( new MapProject.Entity()
      {
        X = 1, Y = 1, Type = 2, Value1 = 5, Value2 = 6,
        Enabled = false, Triggered = true
      } );

      map.AlternativeMultiColor1 = 3;
      map.AlternativeMultiColor2 = 4;
      map.AlternativeBackgroundColor = 5;
      map.AlternativeBGColor4 = 6;
      map.AlternativeMode = TextCharMode.COMMODORE_MULTICOLOR;
      map.SelectedMarkerType = 2;
      map.SelectedEntityType = 1;
      map.MarkerDimOpacity = 55;
      map.NextMarkerGroupId = 9;
      map.MemoRTF = @"{\rtf1\ansi scratch notes}";
      map.ExtraDataText = "extra";
      return map;
    }



    private static MapProject.Map DeserializeScratchBlob( byte[] Blob )
    {
      // Exactly how the editor materializes a scratch: the blob is ONE
      // nested MAP chunk in BuildMapChunk format (the CloneMap mechanics).
      var memReader = new GR.IO.MemoryReader( new GR.Memory.ByteBuffer( Blob ) );
      var outerChunk = new GR.IO.FileChunk();
      var map = new MapProject.Map();
      Assert.IsTrue( outerChunk.ReadFromStream( memReader ) );
      Assert.AreEqual( FileChunkConstants.MAP, outerChunk.Type );
      MapProject.ReadMapFromBody( outerChunk.MemoryReader(), map );
      return map;
    }



    // ================================================================
    // Full fidelity: a rich Map survives BuildMapChunk → container file
    // round-trip → ReadMapFromBody with every field intact.
    // ================================================================

    [TestMethod]
    public void TestScratchBlobFullFidelityRoundTrip()
    {
      var source = BuildRichMap();
      var blob = MapProject.BuildMapChunk( source, IncludeRevisions: false ).ToBuffer().Data();

      var container = new MapScratchContainer();
      container.SetEntry( "owner-guid", blob, "Throne Room", 0 );

      var reloaded = new MapScratchContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      var entry = reloaded.GetEntry( "owner-guid" );
      Assert.IsNotNull( entry );
      Assert.AreEqual( "Throne Room", entry.OwnerMapName );
      Assert.AreEqual( 0, entry.OwnerMapIndex );

      var restored = DeserializeScratchBlob( entry.MapData );

      Assert.AreEqual( "Throne Room", restored.Name );
      Assert.AreEqual( 2, restored.TileSpacingX );
      Assert.AreEqual( 3, restored.TileSpacingY );
      Assert.AreEqual( 4, restored.Tiles.Width );
      Assert.AreEqual( 3, restored.Tiles.Height );
      Assert.AreEqual( 7, restored.Tiles[1, 2] );
      Assert.AreEqual( 11, restored.TileColorOverrides[3, 4] );
      Assert.IsTrue( restored.CharBlockedOverrides[5, 6] );

      Assert.AreEqual( 3, restored.Layers.Count );
      Assert.AreEqual( "Detail", restored.Layers[1].Name );
      Assert.IsFalse( restored.Layers[1].Visible );
      Assert.AreEqual( 3, restored.Layers[1].Tiles[0, 0] );
      Assert.AreEqual( 5, restored.Layers[1].TileColorOverrides[1, 1] );
      Assert.AreEqual( "Overlay", restored.Layers[2].Name );
      Assert.IsTrue( restored.Layers[2].Visible );
      Assert.AreEqual( 4, restored.Layers[2].Tiles[2, 1] );
      Assert.AreEqual( 1, restored.SelectedLayerIndex );

      Assert.AreEqual( 1, restored.Markers.Count );
      var marker = restored.Markers[0];
      Assert.AreEqual( 2, marker.X );
      Assert.AreEqual( 1, marker.Y );
      Assert.AreEqual( 4, marker.Type );
      Assert.AreEqual( "door", marker.Name );
      Assert.AreEqual( (byte)9, marker.Value1 );
      Assert.AreEqual( (byte)8, marker.Value2 );
      Assert.AreEqual( (byte)7, marker.Value3 );
      Assert.AreEqual( (byte)6, marker.Value4 );
      Assert.IsFalse( marker.Enabled );
      Assert.IsTrue( marker.Triggered );
      Assert.IsTrue( marker.AutoDisableGroupAfterTrigger );
      Assert.AreEqual( (byte)3, marker.GroupId );
      Assert.AreEqual( (byte)2, marker.LinkToID );
      Assert.AreEqual( (byte)1, marker.LinkID );
      Assert.AreEqual( 2, marker.Width );
      Assert.AreEqual( 2, marker.Height );

      Assert.AreEqual( 1, restored.Entities.Count );
      var entity = restored.Entities[0];
      Assert.AreEqual( 1, entity.X );
      Assert.AreEqual( 1, entity.Y );
      Assert.AreEqual( 2, entity.Type );
      Assert.AreEqual( (byte)5, entity.Value1 );
      Assert.AreEqual( (byte)6, entity.Value2 );
      Assert.IsFalse( entity.Enabled );
      Assert.IsTrue( entity.Triggered );

      Assert.AreEqual( 3, restored.AlternativeMultiColor1 );
      Assert.AreEqual( 4, restored.AlternativeMultiColor2 );
      Assert.AreEqual( 5, restored.AlternativeBackgroundColor );
      Assert.AreEqual( 6, restored.AlternativeBGColor4 );
      Assert.AreEqual( TextCharMode.COMMODORE_MULTICOLOR, restored.AlternativeMode );
      Assert.AreEqual( 2, restored.SelectedMarkerType );
      Assert.AreEqual( 1, restored.SelectedEntityType );
      Assert.AreEqual( 55, restored.MarkerDimOpacity );
      Assert.AreEqual( 9, restored.NextMarkerGroupId );
      Assert.AreEqual( @"{\rtf1\ansi scratch notes}", restored.MemoRTF );
      Assert.AreEqual( "extra", restored.ExtraDataText );
      // The scratch contract keeps the scratch's own OutlineGuid empty (it
      // must never collide with the painter sidecar identity space).
      Assert.AreEqual( "", restored.OutlineGuid );
      // BuildMapChunk( IncludeRevisions: false ) — no history nested in blobs.
      Assert.AreEqual( 0, restored.Revisions.Count );
    }



    // ================================================================
    // Lazy contract: entries the session never touched re-emit
    // byte-verbatim on the next write.
    // ================================================================

    [TestMethod]
    public void TestScratchBlobByteVerbatimPassthrough()
    {
      var blob = MakeFakeBlob( 42, 4096 );
      var container = new MapScratchContainer();
      container.SetEntry( "untouched", blob, "SomeMap", 3 );

      var pass1 = new MapScratchContainer();
      Assert.IsTrue( pass1.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      var pass2 = new MapScratchContainer();
      Assert.IsTrue( pass2.ReadFromBuffer( pass1.SaveToBuffer( null ) ) );

      CollectionAssert.AreEqual( blob, pass2.GetEntry( "untouched" ).MapData );
      Assert.AreEqual( "SomeMap", pass2.GetEntry( "untouched" ).OwnerMapName );
      Assert.AreEqual( 3, pass2.GetEntry( "untouched" ).OwnerMapIndex );
    }



    [TestMethod]
    public void TestScratchRejectsInvalidEntries()
    {
      var container = new MapScratchContainer();
      container.SetEntry( "", MakeFakeBlob( 1, 10 ) );        // no guid
      container.SetEntry( "g", null );                        // no data
      container.SetEntry( "g", new byte[0] );                 // empty data
      Assert.AreEqual( 0, container.Count );
    }



    [TestMethod]
    public void TestScratchPruneToLiveGuids()
    {
      var container = new MapScratchContainer();
      container.SetEntry( "live", MakeFakeBlob( 1, 50 ) );
      container.SetEntry( "orphan", MakeFakeBlob( 2, 50 ) );

      // Intermediate write (null): orphans retained — a map delete can be
      // undone much later and must still find its scratch.
      var keptAll = new MapScratchContainer();
      keptAll.ReadFromBuffer( container.SaveToBuffer( null ) );
      Assert.AreEqual( 2, keptAll.Count );

      // Final write (live set): orphans dropped for good.
      var pruned = new MapScratchContainer();
      pruned.ReadFromBuffer( container.SaveToBuffer( new HashSet<string>() { "live" } ) );
      Assert.AreEqual( 1, pruned.Count );
      Assert.IsNotNull( pruned.GetEntry( "live" ) );
      Assert.IsNull( pruned.GetEntry( "orphan" ) );
    }



    [TestMethod]
    public void TestScratchOrphanAdoption()
    {
      // The self-heal for "worked on a scratch, auto-saved the sidecar, but
      // never saved the project": the owner returns GUID-less and must
      // re-adopt its scratch by the stored name/index hints.
      var container = new MapScratchContainer();
      container.SetEntry( "guid-a", MakeFakeBlob( 3, 10 ), "Arrival", 0 );
      container.SetEntry( "guid-b", MakeFakeBlob( 4, 10 ), "Dungeon", 1 );

      // Hints survive the buffer round-trip.
      var reloaded = new MapScratchContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      Assert.AreEqual( "Arrival", reloaded.GetEntry( "guid-a" ).OwnerMapName );
      Assert.AreEqual( 1, reloaded.GetEntry( "guid-b" ).OwnerMapIndex );

      var inUse = new HashSet<string>();
      // Exact name+index match wins.
      Assert.AreEqual( "guid-a", reloaded.FindAdoptableEntry( "Arrival", 0, inUse ).OwnerGuid );
      // Renamed map with only the index matching is deliberately NOT adopted:
      // after a rename + reorder a bare index can bind ANOTHER map's scratch,
      // which that map would then overwrite — name evidence is required. The
      // orphan stays in the sidecar, recoverable.
      Assert.IsNull( reloaded.FindAdoptableEntry( "Dungeon Renamed", 1, inUse ) );
      // Name matches even when the index shifted (map inserted above).
      Assert.AreEqual( "guid-b", reloaded.FindAdoptableEntry( "Dungeon", 2, inUse ).OwnerGuid );
      // GUIDs already held by other maps are never stolen.
      inUse.Add( "guid-a" );
      Assert.IsNull( reloaded.FindAdoptableEntry( "Arrival", 0, inUse ) );
      // No hint match at all → nothing adopted.
      inUse.Clear();
      Assert.IsNull( reloaded.FindAdoptableEntry( "Nowhere", 9, inUse ) );

      // Ambiguity refuses to guess: two entries with the same name.
      var ambiguous = new MapScratchContainer();
      ambiguous.SetEntry( "g1", MakeFakeBlob( 3, 10 ), "Twin", 0 );
      ambiguous.SetEntry( "g2", MakeFakeBlob( 4, 10 ), "Twin", 5 );
      Assert.IsNull( ambiguous.FindAdoptableEntry( "Twin", 9, new HashSet<string>() ) );

      // Hint-less entries are never adoptable.
      var hintless = new MapScratchContainer();
      hintless.SetEntry( "g3", MakeFakeBlob( 3, 10 ) );
      Assert.IsNull( hintless.FindAdoptableEntry( "Anything", 0, new HashSet<string>() ) );
    }



    [TestMethod]
    public void TestScratchFileRoundTripAndOverwrite()
    {
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapscratch" );
      try
      {
        var container = new MapScratchContainer();
        container.SetEntry( "guidA", MakeFakeBlob( 7, 1234 ), "MapA", 0 );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsTrue( System.IO.File.Exists( path ) );

        // Atomic overwrite: second write replaces cleanly, no stale .tmp.
        container.SetEntry( "guidB", MakeFakeBlob( 8, 99 ), "MapB", 1 );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsFalse( System.IO.File.Exists( path + ".tmp" ) );

        var reloaded = new MapScratchContainer();
        Assert.IsTrue( reloaded.ReadFromFile( path ) );
        Assert.AreEqual( 2, reloaded.Count );
        CollectionAssert.AreEqual( MakeFakeBlob( 7, 1234 ), reloaded.GetEntry( "guidA" ).MapData );
      }
      finally
      {
        System.IO.File.Delete( path );
        System.IO.File.Delete( path + ".tmp" );
      }
    }



    [TestMethod]
    public void TestScratchEmptyWriteRemovesFile()
    {
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapscratch" );
      try
      {
        var container = new MapScratchContainer();
        container.SetEntry( "guidA", MakeFakeBlob( 7, 100 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsTrue( System.IO.File.Exists( path ) );

        // Emptying the last scratch (the editor's IsScratchEmpty removal)
        // leaves an empty container — writing it must delete the sidecar
        // rather than keep a header-only stub around.
        container.RemoveEntry( "guidA" );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsFalse( System.IO.File.Exists( path ) );

        // Pruning away every entry behaves the same.
        container.SetEntry( "orphan", MakeFakeBlob( 3, 10 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsTrue( System.IO.File.Exists( path ) );
        Assert.IsTrue( container.WriteToFile( path, new HashSet<string>() ) );
        Assert.IsFalse( System.IO.File.Exists( path ) );
      }
      finally
      {
        System.IO.File.Delete( path );
        System.IO.File.Delete( path + ".tmp" );
      }
    }



    [TestMethod]
    public void TestScratchCorruptFilePreservedOnWrite()
    {
      // A transient read failure must never let the next write silently
      // destroy the (possibly recoverable) scratches inside the original
      // file: the unreadable bytes get parked as "<file>.corrupt".
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapscratch" );
      try
      {
        var corruptBytes = new byte[] { 0x13, 0x37, 0xFF, 0x00, 0x01 };
        System.IO.File.WriteAllBytes( path, corruptBytes );

        var container = new MapScratchContainer();
        Assert.IsFalse( container.ReadFromFile( path ) );
        Assert.IsTrue( container.LoadFailed );

        container.SetEntry( "fresh", MakeFakeBlob( 5, 20 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );

        Assert.IsTrue( System.IO.File.Exists( path + ".corrupt" ) );
        CollectionAssert.AreEqual( corruptBytes, System.IO.File.ReadAllBytes( path + ".corrupt" ) );
        var reloaded = new MapScratchContainer();
        Assert.IsTrue( reloaded.ReadFromFile( path ) );
        Assert.IsFalse( reloaded.LoadFailed );
        Assert.AreEqual( 1, reloaded.Count );
      }
      finally
      {
        System.IO.File.Delete( path );
        System.IO.File.Delete( path + ".tmp" );
        System.IO.File.Delete( path + ".corrupt" );
      }
    }



    [TestMethod]
    public void TestScratchMissingAndCorruptFiles()
    {
      string missing = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_missing_" + Guid.NewGuid().ToString( "N" ) + ".mapscratch" );
      var container = new MapScratchContainer();
      // Missing file = "no scratch maps yet": success with an empty container.
      Assert.IsTrue( container.ReadFromFile( missing ) );
      Assert.AreEqual( 0, container.Count );

      // A corrupt/truncated file must degrade to an empty container, never
      // throw — the editor's lazy load treats it like the other sidecars.
      string corrupt = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_corrupt_" + Guid.NewGuid().ToString( "N" ) + ".mapscratch" );
      try
      {
        System.IO.File.WriteAllBytes( corrupt, new byte[] { 0x13, 0x37, 0xFF } );
        container.ReadFromFile( corrupt );
        Assert.AreEqual( 0, container.Count );
      }
      finally
      {
        System.IO.File.Delete( corrupt );
      }
    }



    [TestMethod]
    public void TestScratchTruncatedStreamIsNotValidButEmpty()
    {
      // Integrity contract: a stream that starts with a valid INFO chunk
      // but carries trailing garbage must fail as a whole (position !=
      // length), never masquerade as "valid but empty/partial".
      var container = new MapScratchContainer();
      container.SetEntry( "g", MakeFakeBlob( 1, 32 ) );
      var good = container.SaveToBuffer( null );

      // Drop the trailing 5 bytes — clips the entry chunk's payload.
      var truncated = good.SubBuffer( 0, (int)good.Length - 5 );

      var reader = new MapScratchContainer();
      Assert.IsFalse( reader.ReadFromBuffer( truncated ) );
      Assert.AreEqual( 0, reader.Count );
    }
  }
}
