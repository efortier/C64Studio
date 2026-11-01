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
using MapEditor = studio::RetroDevStudio.Documents.MapEditor;
using CharacterEditor = studio::RetroDevStudio.Controls.CharacterEditor;



namespace TestProject
{
  /// <summary>
  /// The Character Set tab's splitter under the character list is saved with
  /// the map project - one value per project, as thousandths of the list's
  /// column - and restored when it opens. View state: moving it never marks
  /// the project modified.
  /// </summary>
  [TestClass]
  public class TestCharacterListHeight
  {
    // ---------------------------------------------------------------- model

    private static MapProject RoundTrip( MapProject Project )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( Project.SaveToBuffer() ) );
      return reloaded;
    }



    private static List<GR.IO.FileChunk> TopLevelChunks( ByteBuffer File )
    {
      var reader = new GR.IO.MemoryReader( File );
      var result = new List<GR.IO.FileChunk>();
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



    private static ByteBuffer BodyOf( GR.IO.FileChunk Chunk )
    {
      var body = new ByteBuffer();
      var reader = Chunk.MemoryReader();
      reader.ReadBlock( body, (uint)( reader.Size - reader.Position ) );
      return body;
    }



    [TestMethod]
    public void TestTheHeightRoundTrips()
    {
      var project = new MapProject();
      Assert.AreEqual( -1, project.CharacterListHeightPermille, "unset by default" );
      Assert.AreEqual( -1, RoundTrip( project ).CharacterListHeightPermille );

      project.CharacterListHeightPermille = 333;
      Assert.AreEqual( 333, RoundTrip( project ).CharacterListHeightPermille );
    }



    [TestMethod]
    public void TestAFileWithoutTheHeightReadsAsUnset()
    {
      var project = new MapProject();
      project.CharacterListHeightPermille = 333;
      project.CharsetTabFollowsMap = false;

      // saved before the height existed: MAP_PROJECT_INFO ends 4 bytes earlier
      var rebuilt = new ByteBuffer();
      foreach ( var chunk in TopLevelChunks( project.SaveToBuffer() ) )
      {
        var body = BodyOf( chunk );
        if ( chunk.Type == FileChunkConstants.MAP_PROJECT_INFO )
        {
          body = new ByteBuffer( body.Data().Take( (int)body.Length - 4 ).ToArray() );
        }
        var copy = new GR.IO.FileChunk( chunk.Type );
        copy.Append( body );
        rebuilt.Append( copy.ToBuffer() );
      }
      var older = new MapProject();
      older.CharacterListHeightPermille = 500;      // a reused instance must not keep it
      Assert.IsTrue( older.ReadFromBuffer( rebuilt ) );
      Assert.AreEqual( -1, older.CharacterListHeightPermille );
      Assert.IsFalse( older.CharsetTabFollowsMap, "the field before it is intact" );
    }



    [TestMethod]
    public void TestACorruptHeightIsClamped()
    {
      var project = new MapProject();
      project.CharacterListHeightPermille = 5000;
      Assert.AreEqual( 1000, RoundTrip( project ).CharacterListHeightPermille );
      project.CharacterListHeightPermille = -7;
      Assert.AreEqual( -1, RoundTrip( project ).CharacterListHeightPermille );
    }



    // ---------------------------------------------------------------- editor

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
      var field = Owner.GetType().GetField( Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
      Assert.IsNotNull( field, Owner.GetType().Name + " field '" + Name + "' not found" );
      return field.GetValue( Owner );
    }



    /// <summary>
    /// Opens a project with the given height in a real editor (on the Map
    /// tab, as saved), creates every handle, then shows the Character Set tab.
    /// </summary>
    private static void WithEditor( int Permille, Action<MapEditor, SplitContainer> Body )
    {
      StudioTestHost.RunOnSta( () =>
      {
        var project = new MapProject();
        var tile = new MapProject.Tile() { Name = "Floor" };
        tile.Chars.Resize( 1, 1 );
        project.Charsets[0].Tiles.Add( tile );
        project.Charsets[0].ReindexTiles();
        var map = new MapProject.Map() { Name = "Alpha", CharsetIndex = 0, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 4, 3 );
        map.EnsureDefaultLayers();
        project.Maps.Add( map );
        project.CurrentMapIndex = 0;
        project.CharacterListHeightPermille = Permille;

        string path = Path.Combine( Path.GetTempPath(), "C64StudioCharListHeight_" + Guid.NewGuid().ToString( "N" ) + ".mapproject" );
        File.WriteAllBytes( path, project.SaveToBuffer().Data() );
        try
        {
          using ( var editor = new MapEditor( StudioTestHost.CreateCore() ) )
          {
            editor.DocumentInfo.DocumentFilename = path;
            Assert.IsTrue( editor.LoadDocument(), "the project loads" );
            CreateHandles( editor );
            Application.DoEvents();
            var tabs = Field( editor, "tabMapEditor" );
            tabs.GetType().GetProperty( "SelectedPage" ).SetValue( tabs, Field( editor, "tabCharset" ) );
            Application.DoEvents();

            var characters = (CharacterEditor)Field( editor, "characterEditor" );
            var split = (SplitContainer)Field( characters, "splitContainer2" );
            Assert.IsTrue( split.Height > 300, "the Character Set tab is laid out (" + split.Height + ")" );
            Body( editor, split );
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



    private static MapProject Saved( MapEditor Editor )
    {
      var saved = new MapProject();
      Assert.IsTrue( saved.ReadFromBuffer( new ByteBuffer( Editor.SaveToBuffer().Data() ) ) );
      return saved;
    }



    [TestMethod]
    public void TestTheEditorRestoresAndSavesTheHeight()
    {
      WithEditor( 250, ( editor, split ) =>
      {
        // Applied while the editor still had its designer size; the list kept
        // its share when the tab was laid out for real.
        Assert.AreEqual( split.Height / 4.0, split.SplitterDistance, 2.0, "a quarter of " + split.Height );
        Assert.IsFalse( editor.Modified, "opening restores it without modifying the project" );

        split.SplitterDistance = (int)( split.Height * 0.6 );     // the user drags the splitter
        Assert.IsFalse( editor.Modified, "moving the splitter is view state - not a modification" );
        Assert.AreEqual( 600, Saved( editor ).CharacterListHeightPermille, 2 );
      } );
    }



    [TestMethod]
    public void TestAnUnsetHeightKeepsTheDefaultSplit()
    {
      WithEditor( -1, ( editor, split ) =>
      {
        // The designer's split: 406 of 545.
        Assert.AreEqual( split.Height * 406.0 / 545.0, split.SplitterDistance, 2.0 );
        Assert.AreEqual( 745, Saved( editor ).CharacterListHeightPermille, 3, "the next save stores what is shown" );
        Assert.IsFalse( editor.Modified );
      } );
    }
  }
}
