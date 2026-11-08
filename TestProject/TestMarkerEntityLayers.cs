extern alias studio;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using GR.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio;
using RetroDevStudio.Formats;
using StudioCore = studio::RetroDevStudio.StudioCore;
using StudioMapProject = studio::RetroDevStudio.Formats.MapProject;
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;



namespace TestProject
{
  /// <summary>
  /// Markers and entities live on the editor's tile layers, for DISPLAY and
  /// SELECTION only: placing puts them on the selected layer, hiding a layer
  /// hides them and takes them out of every hit test, flattening brings them
  /// down to the Background, and the layer rides along in the project file, the
  /// clone path and the undo snapshots - while the game binary exports every
  /// one of them regardless of any of that.
  ///
  /// The editor-level tests run a REAL MapEditor (handles created, never
  /// Show()n) and reach its private members by reflection, so what is under
  /// test is the code the app actually runs. C64Models is a SHARED project, so
  /// the model types exist twice: the fixture is built with the plain
  /// MapProject and written to a file, and everything read back out of the
  /// editor is a StudioMapProject - the two never mix.
  /// </summary>
  [TestClass]
  public class TestMarkerEntityLayers
  {
    // ---------------------------------------------------------------- model harness

    private static MapProject BuildProject( int MapWidth = 4, int MapHeight = 3 )
    {
      var project = new MapProject();
      project.BackgroundColor = 0;
      for ( int t = 0; t < 3; ++t )
      {
        var tile = new MapProject.Tile() { Index = t, Name = "Tile" + t, Passable = true };
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0] = new MapProject.TileChar { Character = (byte)t, Color = (byte)( 1 + t ) };
        project.Charsets[0].Tiles.Add( tile );
      }
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 0, Name = "Door", TagID = 1 } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = 1, Name = "Exit", TagID = 2, Color = 3 } );
      var rat = new MapProject.EntityType() { ID = 0, Name = "Rat", TagID = 1 };
      project.EntityTypes.Add( rat );
      project.Charsets[0].SetEntityTile( rat, 1 );

      var map = new MapProject.Map() { Name = "Alpha", TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( MapWidth, MapHeight );
      map.CharBlockedOverrides.Resize( MapWidth, MapHeight );
      map.EnsureDefaultLayers();
      project.Maps.Add( map );
      project.CurrentMapIndex = 0;
      return project;
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



    private static ByteBuffer BodyOf( GR.IO.FileChunk Chunk )
    {
      var body = new ByteBuffer();
      var reader = Chunk.MemoryReader();
      reader.ReadBlock( body, (uint)( reader.Size - reader.Position ) );
      return body;
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



    /// <summary>
    /// Round-trips a map through the chunk serializer with every sub-chunk of
    /// one type shortened by Bytes - the stand-in for a file written by a build
    /// that did not have the appended field yet.
    /// </summary>
    private static MapProject.Map ReloadWithSubChunkTruncated( MapProject.Map Source, ushort SubType, int Bytes )
    {
      var mapChunk = MapProject.BuildMapChunk( Source, false );
      var rebuilt = new GR.IO.FileChunk( mapChunk.Type );
      int truncatedCount = 0;
      foreach ( var sub in ReadChunks( mapChunk.MemoryReader() ) )
      {
        var body = BodyOf( sub );
        if ( sub.Type == SubType )
        {
          body = Truncated( body, Bytes );
          ++truncatedCount;
        }
        var copy = new GR.IO.FileChunk( sub.Type );
        copy.Append( body );
        rebuilt.Append( copy.ToBuffer() );
      }
      Assert.IsTrue( truncatedCount > 0, "no sub-chunk of the requested type was found" );

      var reader = new GR.IO.MemoryReader( rebuilt.ToBuffer() );
      var outer = new GR.IO.FileChunk();
      Assert.IsTrue( outer.ReadFromStream( reader ), "the rebuilt map chunk re-reads" );
      var clone = new MapProject.Map();
      MapProject.ReadMapFromBody( outer.MemoryReader(), clone );
      return clone;
    }



    // ---------------------------------------------------------------- editor harness

    private static void CreateHandles( Control Control )
    {
      var handle = Control.Handle;
      foreach ( Control child in Control.Controls )
      {
        CreateHandles( child );
      }
    }



    private static FieldInfo FieldOf( object Owner, string Name )
    {
      var field = Owner.GetType().GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, "MapEditor field '" + Name + "' not found" );
      return field;
    }



    /// <summary>Calls a private method that has exactly one signature.</summary>
    private static object Invoke( object Owner, string Name, params object[] Args )
    {
      var method = Owner.GetType().GetMethod( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( method, "MapEditor method '" + Name + "' not found" );
      return method.Invoke( Owner, Args );
    }



    /// <summary>Calls one specific overload - needed where a null argument cannot pick it.</summary>
    private static object InvokeTyped( object Owner, string Name, Type[] Signature, params object[] Args )
    {
      var method = Owner.GetType().GetMethod( Name,
                                              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                                              null, Signature, null );
      Assert.IsNotNull( method, "MapEditor method '" + Name + "' with that signature not found" );
      return method.Invoke( Owner, Args );
    }



    private static bool IsOnVisibleLayer( MapEditor Editor, StudioMapProject.Marker Marker )
    {
      return (bool)InvokeTyped( Editor, "IsOnVisibleLayer", new[] { typeof( StudioMapProject.Marker ) }, Marker );
    }



    private static bool IsOnVisibleLayer( MapEditor Editor, StudioMapProject.Entity Entity )
    {
      return (bool)InvokeTyped( Editor, "IsOnVisibleLayer", new[] { typeof( StudioMapProject.Entity ) }, Entity );
    }



    private static bool FootprintOverlaps( MapEditor Editor, int X, int Y, int W, int H,
                                           int LayerIndex, StudioMapProject.Marker Exclude )
    {
      return (bool)InvokeTyped( Editor, "MarkerFootprintOverlaps",
                                new[] { typeof( int ), typeof( int ), typeof( int ), typeof( int ),
                                        typeof( int ), typeof( StudioMapProject.Marker ) },
                                X, Y, W, H, LayerIndex, Exclude );
    }



    /// <summary>Sets the editor's private ToolMode field by enum member name.</summary>
    private static void SetToolMode( MapEditor Editor, string Mode )
    {
      var field = FieldOf( Editor, "m_ToolMode" );
      field.SetValue( Editor, Enum.Parse( field.FieldType, Mode ) );
    }



    private static ContextMenuStrip MoveToLayerMenu( MapEditor Editor )
    {
      return (ContextMenuStrip)FieldOf( Editor, "contextMenuMoveToLayer" ).GetValue( Editor );
    }



    /// <summary>The layer rows of the popup, in order (past the header + separator).</summary>
    private static List<ToolStripMenuItem> LayerRows( MapEditor Editor )
    {
      var menu = MoveToLayerMenu( Editor );
      var rows = new List<ToolStripMenuItem>();
      bool pastSeparator = false;
      foreach ( ToolStripItem item in menu.Items )
      {
        if ( item is ToolStripSeparator )
        {
          pastSeparator = true;
          continue;
        }
        if ( pastSeparator )
        {
          rows.Add( (ToolStripMenuItem)item );
        }
      }
      return rows;
    }



    /// <summary>
    /// A client pixel of the map canvas that lands on map cell (CellX, CellY),
    /// found through the editor's own client-to-cell conversion - no guessing
    /// at render offsets or scaling.
    /// </summary>
    private static bool TryFindClientPixelForCell( MapEditor Editor, Control Picture, int CellX, int CellY,
                                                   out int PX, out int PY )
    {
      PX = 0;
      PY = 0;
      var method = Editor.GetType().GetMethod( "TryClientToMapCell",
                                               BindingFlags.Instance | BindingFlags.NonPublic );
      Assert.IsNotNull( method, "TryClientToMapCell not found" );
      for ( int y = 0; y < Picture.ClientRectangle.Height; ++y )
      {
        for ( int x = 0; x < Picture.ClientRectangle.Width; ++x )
        {
          var args = new object[] { x, y, 0, 0 };
          if ( ( (bool)method.Invoke( Editor, args ) )
          &&   ( (int)args[2] == CellX )
          &&   ( (int)args[3] == CellY ) )
          {
            PX = x;
            PY = y;
            return true;
          }
        }
      }
      return false;
    }



    /// <summary>A mouse move followed by a left press at the same spot, as the control raises them.</summary>
    private static void ClickOnMap( MapEditor Editor, int PX, int PY )
    {
      Invoke( Editor, "HandleMouseOnEditor", PX, PY, MouseButtons.None );
      Invoke( Editor, "HandleMouseOnEditor", PX, PY, MouseButtons.Left );
    }



    private static StudioMapProject.Map CurrentMap( MapEditor Editor )
    {
      return (StudioMapProject.Map)FieldOf( Editor, "m_CurrentMap" ).GetValue( Editor );
    }



    /// <summary>Opens a project in a real editor (handles created, never shown) and runs Body.</summary>
    private static void WithEditor( MapProject Project, Action<MapEditor, StudioMapProject.Map> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        string path = Path.Combine( Path.GetTempPath(), "C64StudioLayers_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, Project.SaveToBuffer().Data() );
        try
        {
          using ( var editor = new MapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();

            var map = CurrentMap( editor );
            Assert.IsNotNull( map, "the editor has a current map" );
            Body( editor, map );
          }
        }
        finally
        {
          foreach ( var file in new[] { path,
                                        Path.ChangeExtension( path, ".mapscratch" ),
                                        Path.ChangeExtension( path, ".mapoutlines" ) } )
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



    // ================================================================
    // 1. The layer count is one constant
    // ================================================================

    [TestMethod]
    public void TestEnsureDefaultLayersUsesTheConstant()
    {
      var map = new MapProject.Map() { TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 3, 2 );
      map.EnsureDefaultLayers();

      Assert.AreEqual( 5, MapProject.Map.DEFAULT_LAYER_COUNT, "Background + 4 upper layers" );
      Assert.AreEqual( MapProject.Map.DEFAULT_LAYER_COUNT, map.Layers.Count );
      Assert.AreEqual( "Background", map.Layers[0].Name );
      for ( int li = 1; li < map.Layers.Count; ++li )
      {
        Assert.AreEqual( "Layer " + li, map.Layers[li].Name, "upper layer " + li + " name" );
        Assert.IsTrue( map.Layers[li].Visible, "upper layer " + li + " starts visible" );
        Assert.AreEqual( 3, map.Layers[li].Tiles.Width );
        Assert.AreEqual( 2, map.Layers[li].Tiles.Height );
        Assert.AreEqual( -1, map.Layers[li].Tiles[0, 0], "upper layer " + li + " starts transparent" );
      }
    }



    [TestMethod]
    public void TestAnOlderThreeLayerFileGrowsToTheNewCount()
    {
      // A project written when a map had Background + 2 layers: the two upper
      // layers keep their content and the missing ones are appended empty.
      var project = BuildProject();
      var map = project.Maps[0];
      while ( map.Layers.Count > 3 )
      {
        map.Layers.RemoveAt( map.Layers.Count - 1 );
      }
      map.Layers[1].Name = "Decor";
      map.Layers[1].Tiles[1, 1] = 2;
      map.Layers[2].Visible = false;

      var clone = MapProject.CloneMap( map );

      Assert.AreEqual( MapProject.Map.DEFAULT_LAYER_COUNT, clone.Layers.Count );
      Assert.AreEqual( "Decor", clone.Layers[1].Name );
      Assert.AreEqual( 2, clone.Layers[1].Tiles[1, 1] );
      Assert.IsFalse( clone.Layers[2].Visible );
      Assert.AreEqual( -1, clone.Layers[3].Tiles[0, 0] );
      Assert.AreEqual( -1, clone.Layers[4].Tiles[0, 0] );
    }



    // ================================================================
    // 2. Persistence
    // ================================================================

    [TestMethod]
    public void TestMarkerAndEntityLayersSurviveACloneAndASave()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
      map.Markers.Add( new MapProject.Marker() { Type = 1, X = 1, Y = 0, LayerIndex = 3 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1, LayerIndex = 0 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 2, LayerIndex = 4 } );

      // The clone path (duplicate map / revision snapshot / scratch workspace).
      var clone = MapProject.CloneMap( map );
      Assert.AreEqual( 0, clone.Markers[0].LayerIndex );
      Assert.AreEqual( 3, clone.Markers[1].LayerIndex );
      Assert.AreEqual( 0, clone.Entities[0].LayerIndex );
      Assert.AreEqual( 4, clone.Entities[1].LayerIndex );

      // The whole project file.
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( project.SaveToBuffer() ), "the project re-reads" );
      Assert.AreEqual( 3, reloaded.Maps[0].Markers[1].LayerIndex );
      Assert.AreEqual( 4, reloaded.Maps[0].Entities[1].LayerIndex );
    }



    [TestMethod]
    public void TestAFileWithoutTheLayerFieldLandsOnTheBackground()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, Value1 = 9, LayerIndex = 2 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1, Value1 = 7, LayerIndex = 3 } );

      // Chop the appended I32 off each record - what an older build wrote.
      var noMarkerLayer = ReloadWithSubChunkTruncated( map, FileChunkConstants.MAP_MARKERS, 4 );
      Assert.AreEqual( 1, noMarkerLayer.Markers.Count );
      Assert.AreEqual( 0, noMarkerLayer.Markers[0].LayerIndex, "a marker with no stored layer is on the Background" );
      Assert.AreEqual( (byte)9, noMarkerLayer.Markers[0].Value1, "the fields before it still read" );

      var noEntityLayer = ReloadWithSubChunkTruncated( map, FileChunkConstants.MAP_ENTITIES, 4 );
      Assert.AreEqual( 1, noEntityLayer.Entities.Count );
      Assert.AreEqual( 0, noEntityLayer.Entities[0].LayerIndex, "an entity with no stored layer is on the Background" );
      Assert.AreEqual( (byte)7, noEntityLayer.Entities[0].Value1 );
    }



    [TestMethod]
    public void TestALayerPastTheLastOneIsClampedOnLoad()
    {
      // A file from a build with more layers (or a damaged one) must not leave
      // markers on a layer that does not exist - they would be invisible and
      // unreachable forever. They land on the topmost layer instead.
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 99 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 0, LayerIndex = -5 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 0, LayerIndex = 42 } );

      var clone = MapProject.CloneMap( map );

      int top = clone.Layers.Count - 1;
      Assert.AreEqual( top, clone.Markers[0].LayerIndex );
      Assert.AreEqual( 0, clone.Markers[1].LayerIndex );
      Assert.AreEqual( top, clone.Entities[0].LayerIndex );
    }



    [TestMethod]
    public void TestUndoSnapshotsDeepCopyTheLayer()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 2 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 3 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var markerUndo = new studio::RetroDevStudio.Undo.UndoMapMarkersChange( editor, loaded );
        var entityUndo = new studio::RetroDevStudio.Undo.UndoMapEntitiesChange( editor, loaded );
        Assert.AreEqual( 2, markerUndo.Snapshot[0].LayerIndex, "the marker snapshot carries the layer" );
        Assert.AreEqual( 3, entityUndo.Snapshot[0].LayerIndex, "the entity snapshot carries the layer" );

        loaded.Markers[0].LayerIndex = 0;
        loaded.Entities[0].LayerIndex = 0;
        Assert.AreEqual( 2, markerUndo.Snapshot[0].LayerIndex, "the snapshot is not aliased to the live marker" );
        Assert.AreEqual( 3, entityUndo.Snapshot[0].LayerIndex, "nor to the live entity" );

        // Apply() rebuilds fresh instances - the layer must come back with them.
        markerUndo.Apply();
        entityUndo.Apply();
        Assert.AreEqual( 2, CurrentMap( editor ).Markers[0].LayerIndex, "Apply restores the marker layer" );
        Assert.AreEqual( 3, CurrentMap( editor ).Entities[0].LayerIndex, "Apply restores the entity layer" );
      } );
    }



    // ================================================================
    // 3. The export ignores layers entirely
    // ================================================================

    [TestMethod]
    public void TestEveryMarkerAndEntityExportsEvenFromAHiddenLayer()
    {
      const int HDR_MAP_MARKER_COUNT = 0x1C;
      const int HDR_MAP_ENTITY_COUNT = 0x2F;

      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
      map.Markers.Add( new MapProject.Marker() { Type = 1, X = 1, Y = 0, LayerIndex = 2 } );
      map.Markers.Add( new MapProject.Marker() { Type = 1, X = 2, Y = 0, LayerIndex = 4 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 1, LayerIndex = 0 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 3 } );
      // Every layer hidden, the Background included.
      foreach ( var layer in map.Layers )
      {
        layer.Visible = false;
      }

      var buf = project.ExportAsGameBinary( true, true, true );
      int markerCountTable = buf.UInt16At( HDR_MAP_MARKER_COUNT );
      int entityCountTable = buf.UInt16At( HDR_MAP_ENTITY_COUNT );
      Assert.AreEqual( (byte)3, buf.ByteAt( markerCountTable ), "all 3 markers export" );
      Assert.AreEqual( (byte)2, buf.ByteAt( entityCountTable ), "both entities export" );
    }



    // ================================================================
    // 4. Display and selection in the real editor
    // ================================================================

    [TestMethod]
    public void TestAHiddenLayerHidesItsMarkersAndEntities()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 0, LayerIndex = 2 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 1, LayerIndex = 2 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var onBackground = loaded.Markers.First( m => m.LayerIndex == 0 );
        var onLayer2     = loaded.Markers.First( m => m.LayerIndex == 2 );
        var entity       = loaded.Entities[0];

        Assert.IsTrue( IsOnVisibleLayer( editor, onLayer2 ), "visible while its layer is ticked" );
        Assert.IsTrue( IsOnVisibleLayer( editor, entity ) );

        loaded.Layers[2].Visible = false;

        Assert.IsFalse( IsOnVisibleLayer( editor, onLayer2 ), "hidden with its layer" );
        Assert.IsFalse( IsOnVisibleLayer( editor, entity ) );
        Assert.IsTrue( IsOnVisibleLayer( editor, onBackground ), "the Background is untouched" );
      } );
    }



    [TestMethod]
    public void TestAMarkerOnAHiddenLayerCannotBeHit()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 2, Y = 1, LayerIndex = 3 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1, LayerIndex = 3 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Assert.IsNotNull( Invoke( editor, "MarkerHitAt", 2, 1 ), "hit while the layer is visible" );
        Assert.IsNotNull( Invoke( editor, "EntityHitAt", 2, 1 ) );

        loaded.Layers[3].Visible = false;

        Assert.IsNull( Invoke( editor, "MarkerHitAt", 2, 1 ), "not hit once the layer is hidden" );
        Assert.IsNull( Invoke( editor, "EntityHitAt", 2, 1 ) );
      } );
    }



    [TestMethod]
    public void TestAStackResolvesToTheActiveLayerThenTheTopmost()
    {
      // Several markers and entities on one cell, one per layer - exactly what
      // the layers are for. A click acts on the one the user is working on;
      // with the Background selected the topmost visible one wins instead.
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 1, Value1 = 1 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 2, Value1 = 2 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 4, Value1 = 4 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 1, Value1 = 1 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 3, Value1 = 3 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var activeLayer = FieldOf( editor, "m_ActiveLayerIndex" );

        activeLayer.SetValue( editor, 2 );
        var marker = (StudioMapProject.Marker)Invoke( editor, "MarkerHitAt", 1, 1 );
        Assert.AreEqual( (byte)2, marker.Value1, "the marker on the selected layer is the one hit" );

        activeLayer.SetValue( editor, 3 );
        var entity = (StudioMapProject.Entity)Invoke( editor, "EntityHitAt", 1, 1 );
        Assert.AreEqual( (byte)3, entity.Value1, "same for entities" );

        activeLayer.SetValue( editor, 0 );
        marker = (StudioMapProject.Marker)Invoke( editor, "MarkerHitAt", 1, 1 );
        Assert.AreEqual( (byte)4, marker.Value1, "topmost when the selected layer holds nothing" );

        loaded.Layers[4].Visible = false;
        marker = (StudioMapProject.Marker)Invoke( editor, "MarkerHitAt", 1, 1 );
        Assert.AreEqual( (byte)2, marker.Value1, "a hidden topmost one is skipped" );
      } );
    }



    [TestMethod]
    public void TestMarkersOnDifferentLayersMayShareACell()
    {
      // Same-layer overlap is still refused (unchanged behaviour); the same
      // footprint on another layer is now fine - that is the whole feature.
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, Width = 2, Height = 2, LayerIndex = 1 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var existing = loaded.Markers[0];
        Assert.IsTrue( FootprintOverlaps( editor, 2, 2, 1, 1, 1, null ),
                       "the same cell on the SAME layer still collides" );
        Assert.IsFalse( FootprintOverlaps( editor, 2, 2, 1, 1, 2, null ),
                        "the same cell on another layer does not" );
        Assert.IsFalse( FootprintOverlaps( editor, 2, 2, 1, 1, 1, existing ),
                        "the excluded marker never collides with itself" );
      } );
    }



    [TestMethod]
    public void TestUntickingALayerDropsTheSelectionThatLivesOnIt()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 2 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 2 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var list = (ListView)FieldOf( editor, "listLayers" ).GetValue( editor );
        Assert.AreEqual( MapProject.Map.DEFAULT_LAYER_COUNT, list.Items.Count, "one row per layer" );
        var selectedMarkers = (System.Collections.IList)FieldOf( editor, "m_SelectedMarkers" ).GetValue( editor );
        var selectedEntity  = FieldOf( editor, "m_SelectedEntity" );

        // A selected MARKER on layer 2.
        Invoke( editor, "SelectMarker", loaded.Markers[0] );
        Assert.AreEqual( 1, selectedMarkers.Count, "the marker is selected" );
        list.Items[2].Checked = false;              // the real ItemChecked path
        Application.DoEvents();
        Assert.IsFalse( loaded.Layers[2].Visible, "the tick box writes through to the model" );
        Assert.AreEqual( 0, selectedMarkers.Count, "a marker on a hidden layer cannot stay selected" );

        // A selected ENTITY on layer 2 (selecting an entity clears the marker
        // selection - one thing is active at a time).
        list.Items[2].Checked = true;
        Application.DoEvents();
        Invoke( editor, "SelectEntity", loaded.Entities[0] );
        Assert.IsNotNull( selectedEntity.GetValue( editor ), "the entity is selected" );
        list.Items[2].Checked = false;
        Application.DoEvents();
        Assert.IsNull( selectedEntity.GetValue( editor ), "same for an entity" );
      } );
    }



    [TestMethod]
    public void TestAPlacedEntityLandsOnTheSelectedLayerAndStacks()
    {
      // The headline rule, driven through the real mouse path: the cell
      // already holds an entity on the Background, and placing with layer 3
      // selected must ADD one there instead of replacing it.
      var project = BuildProject();
      var map = project.Maps[0];
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 0, Value1 = 77 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var picture = (Control)FieldOf( editor, "pictureEditor" ).GetValue( editor );
        Assert.IsTrue( picture.ClientRectangle.Width > 0, "the map canvas has a size to click in" );

        SetToolMode( editor, "ENTITY" );
        loaded.SelectedEntityType = 0;
        FieldOf( editor, "m_ActiveLayerIndex" ).SetValue( editor, 3 );

        Assert.IsTrue( TryFindClientPixelForCell( editor, picture, 1, 1, out int px, out int py ),
                       "found a canvas pixel over cell (1,1)" );
        ClickOnMap( editor, px, py );

        var atCell = loaded.Entities.Where( en => ( en.X == 1 ) && ( en.Y == 1 ) ).ToList();
        Assert.AreEqual( 2, atCell.Count, "one entity per layer on the same cell - nothing was replaced" );
        Assert.IsNotNull( atCell.FirstOrDefault( en => ( en.LayerIndex == 0 ) && ( en.Value1 == 77 ) ),
                          "the entity on the Background is untouched" );
        Assert.IsNotNull( atCell.FirstOrDefault( en => en.LayerIndex == 3 ),
                          "the new one landed on the selected layer" );

        // Placing again with the same layer selected replaces, as before.
        ClickOnMap( editor, px, py );
        Assert.AreEqual( 2, loaded.Entities.Count( en => ( en.X == 1 ) && ( en.Y == 1 ) ),
                         "a second placement on the SAME layer replaces instead of piling up" );
      } );
    }



    [TestMethod]
    public void TestPastedMarkersLandOnTheSelectedLayer()
    {
      // A marker paste is a placement too: the armed block drops onto the
      // layer selected in the layer list, whatever layer it was copied from.
      var project = BuildProject();

      WithEditor( project, ( editor, loaded ) =>
      {
        FieldOf( editor, "m_ActiveLayerIndex" ).SetValue( editor, 4 );

        var floating = new List<StudioMapProject.Marker>
        {
          new StudioMapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 1 },
          new StudioMapProject.Marker() { Type = 0, X = 1, Y = 0, LayerIndex = 2 }
        };
        FieldOf( editor, "m_FloatingMarkers" ).SetValue( editor, floating );

        Invoke( editor, "InsertFloatingMarkers", 2, 1 );

        Assert.AreEqual( 2, loaded.Markers.Count, "both markers were placed" );
        Assert.IsTrue( loaded.Markers.All( m => m.LayerIndex == 4 ),
                       "the copied layers are ignored - the paste lands on the selected layer" );
        CollectionAssert.AreEquivalent( new[] { 2, 3 }, loaded.Markers.Select( m => m.X ).ToArray(),
                                        "the block keeps its shape at the anchor" );
      } );
    }



    // ================================================================
    // 5. Moving between layers (what the right-click popup does)
    // ================================================================

    [TestMethod]
    public void TestMoveMarkerToLayerMovesItAndIsUndoable()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Invoke( editor, "MoveMarkerToLayer", loaded.Markers[0], 3 );
        Assert.AreEqual( 3, loaded.Markers[0].LayerIndex, "the marker moved" );
        Assert.IsTrue( editor.Modified, "moving a marker is an edit" );

        editor.DocumentInfo.UndoManager.Undo();
        var after = CurrentMap( editor );
        Assert.AreEqual( 1, after.Markers.Count );
        Assert.AreEqual( 0, after.Markers[0].LayerIndex, "one undo puts it back" );
      } );
    }



    [TestMethod]
    public void TestMoveEntityToLayerMovesItAndIsUndoable()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1, LayerIndex = 1 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Invoke( editor, "MoveEntityToLayer", loaded.Entities[0], 4 );
        Assert.AreEqual( 4, loaded.Entities[0].LayerIndex );
        Assert.IsTrue( editor.Modified );

        editor.DocumentInfo.UndoManager.Undo();
        var after = CurrentMap( editor );
        Assert.AreEqual( 1, after.Entities.Count );
        Assert.AreEqual( 1, after.Entities[0].LayerIndex, "one undo puts it back" );
      } );
    }



    [TestMethod]
    public void TestMoveToLayerRefusesNonsenseTargets()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 1 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 0, LayerIndex = 1 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        foreach ( int target in new[] { -1, 1, MapProject.Map.DEFAULT_LAYER_COUNT, 99 } )
        {
          Invoke( editor, "MoveMarkerToLayer", loaded.Markers[0], target );
          Invoke( editor, "MoveEntityToLayer", loaded.Entities[0], target );
        }
        Assert.AreEqual( 1, loaded.Markers[0].LayerIndex, "out-of-range and no-op targets change nothing" );
        Assert.AreEqual( 1, loaded.Entities[0].LayerIndex );
        Assert.IsFalse( editor.Modified, "a refused move must not dirty the document" );
      } );
    }



    [TestMethod]
    public void TestMovingOntoAHiddenLayerDropsTheSelection()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 0, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        loaded.Layers[2].Visible = false;

        var selectedMarkers = (System.Collections.IList)FieldOf( editor, "m_SelectedMarkers" ).GetValue( editor );
        Invoke( editor, "SelectMarker", loaded.Markers[0] );
        Assert.AreEqual( 1, selectedMarkers.Count );
        Invoke( editor, "MoveMarkerToLayer", loaded.Markers[0], 2 );
        Assert.AreEqual( 0, selectedMarkers.Count, "a marker moved out of sight is deselected" );

        var selectedEntity = FieldOf( editor, "m_SelectedEntity" );
        Invoke( editor, "SelectEntity", loaded.Entities[0] );
        Assert.IsNotNull( selectedEntity.GetValue( editor ) );
        Invoke( editor, "MoveEntityToLayer", loaded.Entities[0], 2 );
        Assert.IsNull( selectedEntity.GetValue( editor ), "same for an entity" );
      } );
    }



    // ================================================================
    // 6. Flatten
    // ================================================================

    [TestMethod]
    public void TestAMarkerAloneCountsAsContentAboveTheBackground()
    {
      // The flatten gate: without this, a map whose only layered content is a
      // marker would answer "there is nothing to flatten". Tested directly
      // because that no-op branch puts up a modal message box.
      var project = BuildProject();

      WithEditor( project, ( editor, loaded ) =>
      {
        Assert.IsFalse( (bool)Invoke( editor, "HasContentAboveBackground" ),
                        "an untouched map has nothing above the Background" );

        loaded.Markers.Add( new StudioMapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 2 } );
        Assert.IsTrue( (bool)Invoke( editor, "HasContentAboveBackground" ), "a marker on layer 2 counts" );

        loaded.Markers.Clear();
        loaded.Entities.Add( new StudioMapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 3 } );
        Assert.IsTrue( (bool)Invoke( editor, "HasContentAboveBackground" ), "an entity on layer 3 counts" );

        loaded.Entities.Clear();
        loaded.Markers.Add( new StudioMapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
        loaded.Entities.Add( new StudioMapProject.Entity() { Type = 0, X = 1, Y = 0, LayerIndex = 0 } );
        Assert.IsFalse( (bool)Invoke( editor, "HasContentAboveBackground" ),
                        "everything already on the Background is still a no-op" );
      } );
    }



    [TestMethod]
    public void TestFlattenBringsMarkersAndEntitiesToTheBackground()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Layers[1].Tiles[0, 0] = 1;      // something for the tile flatten to do
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 0, Y = 0, LayerIndex = 0 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 0, LayerIndex = 2 } );
      map.Markers.Add( new MapProject.Marker() { Type = 1, X = 2, Y = 0, LayerIndex = 4 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 1, LayerIndex = 3 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Invoke( editor, "btnFlattenLayers_Click", null, EventArgs.Empty );

        Assert.IsTrue( loaded.Markers.All( m => m.LayerIndex == 0 ), "every marker is on the Background" );
        Assert.IsTrue( loaded.Entities.All( en => en.LayerIndex == 0 ), "every entity is on the Background" );
        Assert.AreEqual( 1, loaded.Tiles[0, 0], "the upper tile came down too" );

        // One Ctrl+Z (one undo group) restores the tiles AND the layers.
        editor.DocumentInfo.UndoManager.Undo();
        var after = CurrentMap( editor );
        Assert.AreEqual( 3, after.Markers.Count );
        CollectionAssert.AreEquivalent( new[] { 0, 2, 4 },
                                        after.Markers.Select( m => m.LayerIndex ).ToArray(),
                                        "one undo restores the marker layers" );
        Assert.AreEqual( 3, after.Entities[0].LayerIndex, "and the entity layer" );
        Assert.AreEqual( 1, after.Layers[1].Tiles[0, 0], "and the upper tile" );
      } );
    }



    // ================================================================
    // 7. The right-click popup
    // ================================================================

    [TestMethod]
    public void TestTheMenuEnablesEveryLayerExceptTheCurrentOne()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 1 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Invoke( editor, "BuildMoveToLayerMenu", 1, "marker", (Action<int>)( Target => { } ) );

        var menu = MoveToLayerMenu( editor );
        Assert.IsFalse( menu.Items[0].Enabled, "the caption row is a label, not a command" );
        Assert.AreEqual( "Move marker to layer", menu.Items[0].Text );
        Assert.IsInstanceOfType( menu.Items[1], typeof( ToolStripSeparator ) );

        var rows = LayerRows( editor );
        Assert.AreEqual( MapProject.Map.DEFAULT_LAYER_COUNT, rows.Count, "one row per layer" );
        for ( int li = 0; li < rows.Count; ++li )
        {
          if ( li == 1 )
          {
            Assert.IsFalse( rows[li].Enabled, "the layer it is already on is disabled" );
            Assert.IsTrue( rows[li].Checked, "and ticked, so the menu shows where it is" );
          }
          else
          {
            Assert.IsTrue( rows[li].Enabled, "layer " + li + " is a valid destination" );
            Assert.IsFalse( rows[li].Checked );
          }
        }
        Assert.AreEqual( "Background", rows[0].Text );
        Assert.AreEqual( "Layer 4", rows[4].Text );
      } );
    }



    [TestMethod]
    public void TestTheMenuMarksAHiddenLayer()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        loaded.Layers[3].Visible = false;
        Invoke( editor, "BuildMoveToLayerMenu", 0, "marker", (Action<int>)( Target => { } ) );

        var rows = LayerRows( editor );
        StringAssert.Contains( rows[3].Text, "hidden", "a hidden destination says so" );
        Assert.IsTrue( rows[3].Enabled, "it is still a legal destination" );
        Assert.IsFalse( rows[2].Text.Contains( "hidden" ) );
      } );
    }



    [TestMethod]
    public void TestClickingAMenuRowMovesTheMarker()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var marker = loaded.Markers[0];
        Invoke( editor, "BuildMoveToLayerMenu", marker.LayerIndex, "marker",
                (Action<int>)( Target => Invoke( editor, "MoveMarkerToLayer", marker, Target ) ) );

        LayerRows( editor )[3].PerformClick();
        Assert.AreEqual( 3, marker.LayerIndex, "clicking the row moves the marker there" );
      } );
    }



    [TestMethod]
    public void TestARightClickOpensTheMenuOnlyOnAMarkerInMarkerMode()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 2, Y = 1, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        SetToolMode( editor, "MARKER" );
        Assert.IsNotNull( Invoke( editor, "MoveToLayerMarkerAt", 2, 1 ), "on the marker: the menu opens" );
        Assert.IsNull( Invoke( editor, "MoveToLayerMarkerAt", 0, 0 ), "on empty ground: nothing" );

        loaded.Layers[0].Visible = false;
        Assert.IsNull( Invoke( editor, "MoveToLayerMarkerAt", 2, 1 ), "not for a marker on a hidden layer" );

        loaded.Layers[0].Visible = true;
        SetToolMode( editor, "ENTITY" );
        Assert.IsNull( Invoke( editor, "MoveToLayerMarkerAt", 2, 1 ), "and not outside MARKER mode" );
      } );
    }



    [TestMethod]
    public void TestTheFirstRightClickOnAnEntitySelectsAndTheSecondOpensTheMenu()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 1, LayerIndex = 0 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 2, Y = 1, LayerIndex = 0 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        SetToolMode( editor, "ENTITY" );
        var first  = loaded.Entities.First( en => en.X == 1 );
        var second = loaded.Entities.First( en => en.X == 2 );

        Assert.IsNull( Invoke( editor, "MoveToLayerEntityAt", 1, 1 ),
                       "nothing selected yet: the right-click selects instead of opening the menu" );

        Invoke( editor, "SelectEntity", first );
        Assert.AreSame( first, Invoke( editor, "MoveToLayerEntityAt", 1, 1 ),
                        "a second right-click on the SAME entity opens the menu" );
        Assert.IsNull( Invoke( editor, "MoveToLayerEntityAt", 2, 1 ),
                       "a right-click on a DIFFERENT entity selects that one instead" );

        Invoke( editor, "SelectEntity", second );
        Assert.AreSame( second, Invoke( editor, "MoveToLayerEntityAt", 2, 1 ) );
        Assert.IsNull( Invoke( editor, "MoveToLayerEntityAt", 1, 1 ),
                       "the previously selected entity is no longer the target" );

        SetToolMode( editor, "MARKER" );
        Assert.IsNull( Invoke( editor, "MoveToLayerEntityAt", 2, 1 ), "and nothing outside ENTITY mode" );
      } );
    }



    // ================================================================
    // 8. "Show only when layer selected"
    // ================================================================

    private static Krypton.Toolkit.KryptonCheckBox OnlyActiveLayerBox( MapEditor Editor )
    {
      return (Krypton.Toolkit.KryptonCheckBox)FieldOf( Editor, "checkShowEntitiesOnlyOnActiveLayer" ).GetValue( Editor );
    }



    /// <summary>Picks the active layer the way the user does - a click on the layer list row.</summary>
    private static void SelectLayerRow( MapEditor Editor, int LayerIndex )
    {
      var list = (ListView)FieldOf( Editor, "listLayers" ).GetValue( Editor );
      list.Items[LayerIndex].Selected = true;
      list.Items[LayerIndex].Focused  = true;
      Application.DoEvents();
      Assert.AreEqual( LayerIndex, (int)FieldOf( Editor, "m_ActiveLayerIndex" ).GetValue( Editor ), "the row click took" );
    }



    [TestMethod]
    public void TestShowOnlyWhenLayerSelectedShowsJustTheActiveLayersEntities()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 0, LayerIndex = 1, Value1 = 1 } );
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 1, Y = 0, LayerIndex = 3, Value1 = 3 } );
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 2, Y = 0, LayerIndex = 3 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var on1 = loaded.Entities.First( en => en.LayerIndex == 1 );
        var on3 = loaded.Entities.First( en => en.LayerIndex == 3 );
        var markerOn3 = loaded.Markers[0];
        SelectLayerRow( editor, 1 );

        Assert.IsFalse( OnlyActiveLayerBox( editor ).Checked, "off by default - everything shows" );
        Assert.IsTrue( IsOnVisibleLayer( editor, on3 ) );

        OnlyActiveLayerBox( editor ).Checked = true;        // the real CheckedChanged path
        Application.DoEvents();
        Assert.IsTrue( IsOnVisibleLayer( editor, on1 ), "the active layer's entity shows" );
        Assert.IsFalse( IsOnVisibleLayer( editor, on3 ), "an entity on another layer is hidden" );
        Assert.IsNull( Invoke( editor, "EntityHitAt", 1, 0 ), "and cannot be clicked" );
        Assert.IsTrue( IsOnVisibleLayer( editor, markerOn3 ), "markers are not affected - it is an entity option" );

        // Selecting the other layer swaps which entities show.
        SelectLayerRow( editor, 3 );
        Assert.IsTrue( IsOnVisibleLayer( editor, on3 ) );
        Assert.IsFalse( IsOnVisibleLayer( editor, on1 ) );
        Assert.IsNotNull( Invoke( editor, "EntityHitAt", 1, 0 ) );

        // A hidden layer stays hidden even when it is the active one.
        loaded.Layers[3].Visible = false;
        Assert.IsFalse( IsOnVisibleLayer( editor, on3 ), "the layer tick box still wins" );
        loaded.Layers[3].Visible = true;

        // Off again: everything shows, whatever the active layer.
        OnlyActiveLayerBox( editor ).Checked = false;
        Application.DoEvents();
        Assert.IsTrue( IsOnVisibleLayer( editor, on1 ) );
        Assert.IsTrue( IsOnVisibleLayer( editor, on3 ) );

        Assert.IsFalse( editor.Modified, "a view toggle and a layer switch never dirty the project" );
      } );
    }



    [TestMethod]
    public void TestShowOnlyWhenLayerSelectedDropsASelectionThatLeavesTheView()
    {
      var project = BuildProject();
      var map = project.Maps[0];
      map.Entities.Add( new MapProject.Entity() { Type = 0, X = 0, Y = 0, LayerIndex = 1 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        var selectedEntity = FieldOf( editor, "m_SelectedEntity" );
        var entity = loaded.Entities[0];

        // Ticking the box while an entity on another layer is selected.
        SelectLayerRow( editor, 2 );
        Invoke( editor, "SelectEntity", entity );
        Assert.IsNotNull( selectedEntity.GetValue( editor ) );
        OnlyActiveLayerBox( editor ).Checked = true;
        Application.DoEvents();
        Assert.IsNull( selectedEntity.GetValue( editor ), "ticking the box deselects an entity that just went out of view" );

        // Switching away from the selected entity's layer.
        SelectLayerRow( editor, 1 );
        Invoke( editor, "SelectEntity", entity );
        Assert.IsNotNull( selectedEntity.GetValue( editor ), "selectable again on its own layer" );
        SelectLayerRow( editor, 4 );
        Assert.IsNull( selectedEntity.GetValue( editor ), "leaving its layer deselects it" );

        Assert.IsFalse( editor.Modified );
      } );
    }



    [TestMethod]
    public void TestFlattenRunsWhenOnlyMarkersAreOnUpperLayers()
    {
      // No upper TILE content at all - the only layered thing is a marker.
      var project = BuildProject();
      var map = project.Maps[0];
      map.Markers.Add( new MapProject.Marker() { Type = 0, X = 1, Y = 1, LayerIndex = 2 } );

      WithEditor( project, ( editor, loaded ) =>
      {
        Assert.IsTrue( (bool)Invoke( editor, "HasContentAboveBackground" ),
                       "guard: without this the click below would open a modal message box" );
        Invoke( editor, "btnFlattenLayers_Click", null, EventArgs.Empty );
        Assert.AreEqual( 0, loaded.Markers[0].LayerIndex );
        Assert.IsTrue( editor.Modified );
      } );
    }
  }
}
