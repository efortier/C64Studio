using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;



namespace TestProject
{
  /// <summary>
  /// Map outline (paint mode) persistence: the OutlineGuid identity field
  /// on MapProject.Map and the .mapoutlines sidecar container.
  /// </summary>
  [TestClass]
  public class TestMapOutline
  {
    private MapProject CreateProjectWithMaps( int mapCount )
    {
      var proj = new MapProject();
      for ( int i = 0; i < mapCount; ++i )
      {
        var map = new MapProject.Map();
        map.Name = "Map" + i;
        map.Tiles.Resize( 2, 2 );
        proj.Maps.Add( map );
      }
      return proj;
    }



    private byte[] MakeFakePNG( byte Seed, int Length )
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



    // ================================================================
    // OutlineGuid — project file round-trip + sanitize
    // ================================================================

    [TestMethod]
    public void TestOutlineGuidRoundTrip()
    {
      var proj = CreateProjectWithMaps( 2 );
      proj.Maps[0].OutlineGuid = "a1b2c3d4e5f60718293a4b5c6d7e8f90";
      // Maps[1] stays unassigned — lazily assigned on first outline use.

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );

      Assert.AreEqual( "a1b2c3d4e5f60718293a4b5c6d7e8f90", reloaded.Maps[0].OutlineGuid );
      Assert.AreEqual( "", reloaded.Maps[1].OutlineGuid );
    }



    [TestMethod]
    public void TestOutlineGuidDuplicateSanitizeOnLoad()
    {
      var proj = CreateProjectWithMaps( 3 );
      proj.Maps[0].OutlineGuid = "duplicated";
      proj.Maps[1].OutlineGuid = "duplicated";
      proj.Maps[2].OutlineGuid = "unique";

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );

      // First holder keeps the GUID (and thus the sidecar image); later
      // holders reset to "" = assigned lazily on first outline use.
      Assert.AreEqual( "duplicated", reloaded.Maps[0].OutlineGuid );
      Assert.AreEqual( "", reloaded.Maps[1].OutlineGuid );
      Assert.AreEqual( "unique", reloaded.Maps[2].OutlineGuid );
    }



    [TestMethod]
    public void TestCloneMapCopiesOutlineGuid()
    {
      // Documents WHY btnMapCopy_Click must reset the duplicate's GUID:
      // CloneMap round-trips the serializer, so the GUID comes along. If
      // this ever stops holding, the reset becomes dead code — this test
      // flags the contract change either way.
      var proj = CreateProjectWithMaps( 1 );
      proj.Maps[0].OutlineGuid = "cafebabe00112233445566778899aabb";

      var clone = MapProject.CloneMap( proj.Maps[0] );

      Assert.AreEqual( proj.Maps[0].OutlineGuid, clone.OutlineGuid );
    }



    [TestMethod]
    public void TestOutlineToolSettingsRoundTrip()
    {
      var proj = CreateProjectWithMaps( 1 );
      proj.OutlineToolSettings.BrushSize = 32;
      proj.OutlineToolSettings.EraserSize = 64;
      proj.OutlineToolSettings.ShapeBorderSize = 7;
      proj.OutlineToolSettings.StampScale = 3;
      proj.OutlineToolSettings.TextFontFamily = "Consolas";
      proj.OutlineToolSettings.TextFontSize = 22;
      proj.OutlineToolSettings.TextFontBold = true;
      proj.OutlineToolSettings.TextFontItalic = true;
      proj.OutlineToolSettings.InkColorARGB = 0x80FF4020;
      proj.OutlineToolSettings.FillColorARGB = 0xFF102030;
      proj.OutlineToolSettings.ExtendStep = 64;
      proj.OutlineToolSettings.RecentColorsARGB.Add( 0xFF112233 );
      proj.OutlineToolSettings.RecentColorsARGB.Add( 0x40FFEEDD );

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );

      var tools = reloaded.OutlineToolSettings;
      Assert.AreEqual( 32, tools.BrushSize );
      Assert.AreEqual( 64, tools.EraserSize );
      Assert.AreEqual( 7, tools.ShapeBorderSize );
      Assert.AreEqual( 3, tools.StampScale );
      Assert.AreEqual( "Consolas", tools.TextFontFamily );
      Assert.AreEqual( 22, tools.TextFontSize );
      Assert.IsTrue( tools.TextFontBold );
      Assert.IsTrue( tools.TextFontItalic );
      Assert.AreEqual( 0x80FF4020u, tools.InkColorARGB );
      Assert.AreEqual( 0xFF102030u, tools.FillColorARGB );
      Assert.AreEqual( 64, tools.ExtendStep );
      CollectionAssert.AreEqual( new List<uint>() { 0xFF112233, 0x40FFEEDD }, tools.RecentColorsARGB );

      // Out-of-range values from a corrupt/hand-edited file must clamp,
      // not crash or poison the UI controls.
      proj.OutlineToolSettings.BrushSize = 9999;
      proj.OutlineToolSettings.StampScale = 0;
      var clamped = new MapProject();
      Assert.IsTrue( clamped.ReadFromBuffer( proj.SaveToBuffer() ) );
      Assert.AreEqual( 128, clamped.OutlineToolSettings.BrushSize );
      Assert.AreEqual( 1, clamped.OutlineToolSettings.StampScale );
    }



    // ================================================================
    // MapOutlineContainer — buffer round-trip, pruning, file I/O
    // ================================================================

    [TestMethod]
    public void TestContainerBufferRoundTrip()
    {
      var container = new MapOutlineContainer();
      var pngA = MakeFakePNG( 10, 300 );
      var pngB = MakeFakePNG( 99, 5000 );
      container.SetImage( "guidA", 640, 400, pngA );
      container.SetImage( "guidB", 1380, 737, pngB );

      var reloaded = new MapOutlineContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );

      Assert.AreEqual( 2, reloaded.Count );
      var entryA = reloaded.GetImage( "guidA" );
      Assert.IsNotNull( entryA );
      Assert.AreEqual( 640, entryA.Width );
      Assert.AreEqual( 400, entryA.Height );
      CollectionAssert.AreEqual( pngA, entryA.PNGData );
      var entryB = reloaded.GetImage( "guidB" );
      Assert.IsNotNull( entryB );
      Assert.AreEqual( 1380, entryB.Width );
      Assert.AreEqual( 737, entryB.Height );
      CollectionAssert.AreEqual( pngB, entryB.PNGData );
    }



    [TestMethod]
    public void TestContainerPruneToLiveGuids()
    {
      var container = new MapOutlineContainer();
      container.SetImage( "live", 100, 100, MakeFakePNG( 1, 50 ) );
      container.SetImage( "orphan", 100, 100, MakeFakePNG( 2, 50 ) );

      // Intermediate write (null): orphans retained — a map delete can be
      // undone much later and must still find its picture.
      var keptAll = new MapOutlineContainer();
      keptAll.ReadFromBuffer( container.SaveToBuffer( null ) );
      Assert.AreEqual( 2, keptAll.Count );

      // Final write (live set): orphans dropped for good.
      var pruned = new MapOutlineContainer();
      pruned.ReadFromBuffer( container.SaveToBuffer( new HashSet<string>() { "live" } ) );
      Assert.AreEqual( 1, pruned.Count );
      Assert.IsNotNull( pruned.GetImage( "live" ) );
      Assert.IsNull( pruned.GetImage( "orphan" ) );
    }



    [TestMethod]
    public void TestContainerRejectsInvalidEntries()
    {
      var container = new MapOutlineContainer();
      container.SetImage( "", 100, 100, MakeFakePNG( 1, 10 ) );          // no guid
      container.SetImage( "g", 100, 100, null );                        // no data
      container.SetImage( "g", 100, 100, new byte[0] );                 // empty data
      container.SetImage( "g", 0, 100, MakeFakePNG( 1, 10 ) );          // zero width
      container.SetImage( "g", 100, -1, MakeFakePNG( 1, 10 ) );         // negative height
      Assert.AreEqual( 0, container.Count );
    }



    [TestMethod]
    public void TestContainerFileRoundTripAndOverwrite()
    {
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapoutlines" );
      try
      {
        var container = new MapOutlineContainer();
        container.SetImage( "guidA", 320, 200, MakeFakePNG( 7, 1234 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsTrue( System.IO.File.Exists( path ) );

        // Atomic overwrite: second write replaces cleanly, no stale .tmp.
        container.SetImage( "guidB", 64, 64, MakeFakePNG( 8, 99 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsFalse( System.IO.File.Exists( path + ".tmp" ) );

        var reloaded = new MapOutlineContainer();
        Assert.IsTrue( reloaded.ReadFromFile( path ) );
        Assert.AreEqual( 2, reloaded.Count );
        CollectionAssert.AreEqual( MakeFakePNG( 7, 1234 ), reloaded.GetImage( "guidA" ).PNGData );
      }
      finally
      {
        System.IO.File.Delete( path );
        System.IO.File.Delete( path + ".tmp" );
      }
    }



    [TestMethod]
    public void TestContainerEmptyWriteRemovesFile()
    {
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapoutlines" );
      try
      {
        var container = new MapOutlineContainer();
        container.SetImage( "guidA", 320, 200, MakeFakePNG( 7, 100 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsTrue( System.IO.File.Exists( path ) );

        // Removing the last image (the "Delete picture" option) leaves an
        // empty container — writing it must delete the sidecar rather
        // than keep a header-only stub around.
        container.RemoveImage( "guidA" );
        Assert.IsTrue( container.WriteToFile( path, null ) );
        Assert.IsFalse( System.IO.File.Exists( path ) );

        // Pruning away every image behaves the same.
        container.SetImage( "orphan", 10, 10, MakeFakePNG( 3, 10 ) );
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
    public void TestMapMemoRoundTrip()
    {
      var proj = CreateProjectWithMaps( 2 );
      proj.Maps[0].MemoRTF = @"{\rtf1\ansi Bold \b stuff\b0 .}";
      // Maps[1] stays empty — must survive as empty, no chunk written.

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      Assert.AreEqual( proj.Maps[0].MemoRTF, reloaded.Maps[0].MemoRTF );
      Assert.AreEqual( "", reloaded.Maps[1].MemoRTF );

      // CloneMap (revisions/duplicate/undo path) carries the memo.
      var clone = MapProject.CloneMap( reloaded.Maps[0] );
      Assert.AreEqual( proj.Maps[0].MemoRTF, clone.MemoRTF );
    }



    [TestMethod]
    public void TestContainerOrphanAdoption()
    {
      // The self-heal for "drew, auto-saved, but never saved the project":
      // the map returns GUID-less and must re-adopt its image by the
      // stored name/index hints.
      var container = new MapOutlineContainer();
      container.SetImage( "guid-a", 10, 10, MakeFakePNG( 3, 10 ), "Arrival", 0 );
      container.SetImage( "guid-b", 10, 10, MakeFakePNG( 4, 10 ), "Dungeon", 1 );

      // Hints survive the file round-trip.
      var reloaded = new MapOutlineContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      Assert.AreEqual( "Arrival", reloaded.GetImage( "guid-a" ).MapName );
      Assert.AreEqual( 1, reloaded.GetImage( "guid-b" ).MapIndex );

      var inUse = new HashSet<string>();
      // Exact name+index match wins.
      Assert.AreEqual( "guid-a", reloaded.FindAdoptableImage( "Arrival", 0, inUse ).Guid );
      // Renamed map with only the index matching is deliberately NOT adopted:
      // after a rename + reorder a bare index can bind ANOTHER map's drawing,
      // which that map would then overwrite — name evidence is required. The
      // orphan stays in the sidecar, recoverable.
      Assert.IsNull( reloaded.FindAdoptableImage( "Dungeon Renamed", 1, inUse ) );
      // Name matches even when the index shifted (map inserted above).
      Assert.AreEqual( "guid-b", reloaded.FindAdoptableImage( "Dungeon", 2, inUse ).Guid );
      // GUIDs already held by other maps are never stolen.
      inUse.Add( "guid-a" );
      Assert.IsNull( reloaded.FindAdoptableImage( "Arrival", 0, inUse ) );
      // No hint match at all → nothing adopted.
      inUse.Clear();
      Assert.IsNull( reloaded.FindAdoptableImage( "Nowhere", 9, inUse ) );

      // Ambiguity refuses to guess: two entries with the same name.
      var ambiguous = new MapOutlineContainer();
      ambiguous.SetImage( "g1", 10, 10, MakeFakePNG( 3, 10 ), "Twin", 0 );
      ambiguous.SetImage( "g2", 10, 10, MakeFakePNG( 4, 10 ), "Twin", 5 );
      Assert.IsNull( ambiguous.FindAdoptableImage( "Twin", 9, new HashSet<string>() ) );

      // Hint-less legacy entries are never adoptable.
      var legacy = new MapOutlineContainer();
      legacy.SetImage( "g3", 10, 10, MakeFakePNG( 3, 10 ) );
      Assert.IsNull( legacy.FindAdoptableImage( "Anything", 0, new HashSet<string>() ) );
    }



    [TestMethod]
    public void TestContainerCorruptFilePreservedOnWrite()
    {
      // A transient read failure must never let the next write silently
      // destroy the (possibly recoverable) images inside the original
      // file: the unreadable bytes get parked as "<file>.corrupt".
      string path = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_" + Guid.NewGuid().ToString( "N" ) + ".mapoutlines" );
      try
      {
        var corruptBytes = new byte[] { 0x13, 0x37, 0xFF, 0x00, 0x01 };
        System.IO.File.WriteAllBytes( path, corruptBytes );

        var container = new MapOutlineContainer();
        Assert.IsFalse( container.ReadFromFile( path ) );
        Assert.IsTrue( container.LoadFailed );

        container.SetImage( "fresh", 10, 10, MakeFakePNG( 5, 20 ) );
        Assert.IsTrue( container.WriteToFile( path, null ) );

        Assert.IsTrue( System.IO.File.Exists( path + ".corrupt" ) );
        CollectionAssert.AreEqual( corruptBytes, System.IO.File.ReadAllBytes( path + ".corrupt" ) );
        var reloaded = new MapOutlineContainer();
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
    public void TestContainerMissingAndCorruptFiles()
    {
      string missing = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_missing_" + Guid.NewGuid().ToString( "N" ) + ".mapoutlines" );
      var container = new MapOutlineContainer();
      // Missing file = "no outlines yet": success with an empty container.
      Assert.IsTrue( container.ReadFromFile( missing ) );
      Assert.AreEqual( 0, container.Count );

      // A corrupt/truncated file must degrade to an empty container, never
      // throw — the editor's lazy load treats it like the other sidecars.
      string corrupt = System.IO.Path.Combine( System.IO.Path.GetTempPath(),
        "c64studio_test_corrupt_" + Guid.NewGuid().ToString( "N" ) + ".mapoutlines" );
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



    // ================================================================
    // Pen (Wacom) tool settings — every field must round-trip in the
    // MAP_OUTLINE_TOOL_SETTINGS chunk (the user's "save/reload ALL tablet
    // settings" requirement). Floats are stored scaled, hence the tolerances.
    // ================================================================

    [TestMethod]
    public void TestOutlinePenSettingsRoundTrip()
    {
      var proj = new MapProject();
      var s = proj.OutlineToolSettings;
      // Every pen field set to a distinctive, in-range non-default.
      s.PenPressureEnabled = false;
      s.PenPressureTarget  = 1;
      s.PenPressureGamma   = 2.5f;
      s.PenMinWidth        = 3.5f;
      s.PenMinAlpha        = 0.4f;
      s.PenFlipEraser      = false;
      s.PenCursorIndex     = 3;
      s.PenButtonBindings  = new List<int>() { 5, 7, 2 };
      // Also nudge a pre-existing field + the recents list to prove the
      // appended pen block reads correctly AFTER the variable-length list.
      s.BrushSize = 11;
      s.RecentColorsARGB = new List<uint>() { 0xFF102030, 0xFF405060 };

      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      var r = reloaded.OutlineToolSettings;

      Assert.AreEqual( false, r.PenPressureEnabled, "PenPressureEnabled" );
      Assert.AreEqual( 1, r.PenPressureTarget, "PenPressureTarget" );
      Assert.AreEqual( 2.5f, r.PenPressureGamma, 0.011f, "PenPressureGamma" );
      Assert.AreEqual( 3.5f, r.PenMinWidth, 0.051f, "PenMinWidth" );
      Assert.AreEqual( 0.4f, r.PenMinAlpha, 0.011f, "PenMinAlpha" );
      Assert.AreEqual( false, r.PenFlipEraser, "PenFlipEraser" );
      Assert.AreEqual( 3, r.PenCursorIndex, "PenCursorIndex" );
      CollectionAssert.AreEqual( new List<int>() { 5, 7, 2 }, r.PenButtonBindings, "PenButtonBindings" );
      // The recents list + the earlier field must still be intact around it.
      Assert.AreEqual( 11, r.BrushSize, "BrushSize (before the pen block)" );
      CollectionAssert.AreEqual( new List<uint>() { 0xFF102030, 0xFF405060 }, r.RecentColorsARGB, "recents" );
    }



    // ================================================================
    // Text-objects blob on sidecar entries — the container treats it as
    // opaque bytes; it must survive SetImage (which recreates entries),
    // buffer round-trips, pruning, and be null for pre-feature files.
    // ================================================================

    [TestMethod]
    public void TestOutlineTextObjectsBlobSurvivesRoundTrip()
    {
      var blob = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
      var container = new MapOutlineContainer();
      // SetImage recreates the entry wholesale — the blob must travel through
      // the signature (the landmine this guards against).
      container.SetImage( "guid-t", 10, 10, MakeFakePNG( 5, 16 ), "MapT", 0, blob );
      Assert.IsNotNull( container.GetImage( "guid-t" ).TextObjectsData );

      var reloaded = new MapOutlineContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      CollectionAssert.AreEqual( blob, reloaded.GetImage( "guid-t" ).TextObjectsData );

      // Pruning to live GUIDs keeps the blob with its entry.
      var live = new HashSet<string>() { "guid-t" };
      var pruned = new MapOutlineContainer();
      Assert.IsTrue( pruned.ReadFromBuffer( reloaded.SaveToBuffer( live ) ) );
      CollectionAssert.AreEqual( blob, pruned.GetImage( "guid-t" ).TextObjectsData );
    }



    [TestMethod]
    public void TestOutlineTextObjectsAbsentInOldFiles()
    {
      // An entry stored WITHOUT a blob (pre-feature or plain drawing) loads
      // with null — and a mixed container keeps each entry's own state.
      var container = new MapOutlineContainer();
      container.SetImage( "guid-plain", 10, 10, MakeFakePNG( 1, 12 ), "Plain", 0 );
      container.SetImage( "guid-texty", 10, 10, MakeFakePNG( 2, 12 ), "Texty", 1, new byte[] { 9, 9 } );

      var reloaded = new MapOutlineContainer();
      Assert.IsTrue( reloaded.ReadFromBuffer( container.SaveToBuffer( null ) ) );
      Assert.IsNull( reloaded.GetImage( "guid-plain" ).TextObjectsData );
      Assert.IsNotNull( reloaded.GetImage( "guid-texty" ).TextObjectsData );
    }



    [TestMethod]
    public void TestOutlinePenSettingsDefaultsWhenAbsent()
    {
      // A round-trip that never touched the pen fields must load them at their
      // defaults — never throw, never zero them (forward/backward-compat).
      var proj = new MapProject();
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( proj.SaveToBuffer() ) );
      var r = reloaded.OutlineToolSettings;
      Assert.IsTrue( r.PenPressureEnabled );
      Assert.AreEqual( 2, r.PenPressureTarget );
      Assert.IsTrue( r.PenFlipEraser );
      Assert.IsNotNull( r.PenButtonBindings );
    }
  }
}
