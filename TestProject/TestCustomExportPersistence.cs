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
  /// Custom text exports (the "Custom Exports" tab) in the .mapproject file:
  /// one MAP_CUSTOM_EXPORT sub-chunk per definition inside MAP_PROJECT_DATA,
  /// strings as UTF-8 with a byte-length prefix, line endings CRLF on load.
  /// A file without definitions has no such chunk, an older build simply
  /// skips the chunk, and a truncated chunk falls back to defaults.
  /// </summary>
  [TestClass]
  public class TestCustomExportPersistence
  {
    // ---------------------------------------------------------------- helpers

    private const string TEMPLATE_CRLF = "{% for m in exported_maps %}\r\n\tRefAddFile(\"MAP-{{ m.id:D4 }}\", \"../Maps/Exported/MAP_{{ m.id:D4 }}\" + MapExtension, COMPRESSED_MAPS, *)\r\n{% endfor %}\r\n";
    private const string TEMPLATE_MIXED = "line one\nline two\rline three\r\nline four \u2192 \u00e9\t{{ map.name }}";
    private const string TEMPLATE_MIXED_NORMALIZED = "line one\r\nline two\r\nline three\r\nline four \u2192 \u00e9\t{{ map.name }}";



    private static MapProject.Map NewMap( string Name, int MapID )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, TileSpacingX = 1, TileSpacingY = 1 };
      map.Tiles.Resize( 2, 2 );
      return map;
    }



    private static MapProject BuildProject()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "Arrival", 1 ) );
      project.MapStrings.Add( new MapProject.MapString() { Label = "WELCOME", StringID = 5 } );

      var disk = project.AddCustomExport( "Disk files" );
      disk.Directory   = "..\\src\\generated";
      disk.FileName    = "map_files.asm";
      disk.Description = "RefAddFile pairs for make_disk.asm\r\nsecond line";
      disk.Content     = TEMPLATE_CRLF;

      var mixed = project.AddCustomExport( "Mixed endings" );
      mixed.Active      = false;
      mixed.Directory   = "Z:\\DevC64\\Dreadhold\\Maps\\Exported";
      mixed.FileName    = "names.inc";
      mixed.Description = "lf\nonly";
      mixed.Content     = TEMPLATE_MIXED;
      return project;
    }



    private static MapProject Reload( MapProject Project )
    {
      return Load( Project.SaveToBuffer() );
    }



    private static MapProject Load( ByteBuffer File )
    {
      var reloaded = new MapProject();
      Assert.IsTrue( reloaded.ReadFromBuffer( File ), "the project loads" );
      return reloaded;
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



    private static ByteBuffer FirstBytes( ByteBuffer Body, int Count )
    {
      var data = Body.Data();
      var result = new ByteBuffer();
      for ( int i = 0; i < Count; ++i )
      {
        result.AppendU8( data[i] );
      }
      return result;
    }



    /// <summary>
    /// The project file with every MAP_CUSTOM_EXPORT sub-chunk replaced by
    /// Replace( body ) (null = drop the chunk, as an older build would).
    /// </summary>
    private static ByteBuffer WithCustomExportChunks( ByteBuffer File, System.Func<ByteBuffer, ByteBuffer> Replace )
    {
      var topLevel = new List<GR.IO.FileChunk>();
      foreach ( var chunk in TopLevelChunks( File ) )
      {
        if ( chunk.Type != FileChunkConstants.MAP_PROJECT_DATA )
        {
          topLevel.Add( chunk );
          continue;
        }
        var container = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_DATA );
        foreach ( var sub in SubChunks( chunk ) )
        {
          var body = BodyOf( sub );
          if ( sub.Type == FileChunkConstants.MAP_CUSTOM_EXPORT )
          {
            body = Replace( body );
            if ( body == null )
            {
              continue;
            }
          }
          container.Append( MakeChunk( sub.Type, body ).ToBuffer() );
        }
        topLevel.Add( container );
      }
      return Rebuild( topLevel );
    }



    private static int CustomExportChunkCount( ByteBuffer File )
    {
      int count = 0;
      foreach ( var chunk in TopLevelChunks( File ) )
      {
        if ( chunk.Type == FileChunkConstants.MAP_PROJECT_DATA )
        {
          count += SubChunks( chunk ).Count( s => s.Type == FileChunkConstants.MAP_CUSTOM_EXPORT );
        }
      }
      return count;
    }



    // -------------------------------------------------------------- round trip

    [TestMethod]
    public void TestEveryFieldAndTheOrderSurviveSaveLoad()
    {
      var project = BuildProject();
      var reloaded = Reload( project );

      Assert.AreEqual( 2, reloaded.CustomExports.Count );
      var disk = reloaded.CustomExports[0];
      Assert.AreEqual( 1, disk.ID );
      Assert.AreEqual( "Disk files", disk.Name );
      Assert.IsTrue( disk.Active );
      Assert.AreEqual( "..\\src\\generated", disk.Directory );
      Assert.AreEqual( "map_files.asm", disk.FileName );
      Assert.AreEqual( "RefAddFile pairs for make_disk.asm\r\nsecond line", disk.Description );
      Assert.AreEqual( TEMPLATE_CRLF, disk.Content, "tabs, CRLF and the template tags come back verbatim" );

      var mixed = reloaded.CustomExports[1];
      Assert.AreEqual( 2, mixed.ID );
      Assert.AreEqual( "Mixed endings", mixed.Name );
      Assert.IsFalse( mixed.Active );
      Assert.AreEqual( "Z:\\DevC64\\Dreadhold\\Maps\\Exported", mixed.Directory );
      Assert.AreEqual( "names.inc", mixed.FileName );
      Assert.AreEqual( "lf\r\nonly", mixed.Description, "a bare LF loads as CRLF" );
      Assert.AreEqual( TEMPLATE_MIXED_NORMALIZED, mixed.Content, "LF and lone CR load as CRLF; the arrow and the accented letter survive (UTF-8, not one byte per char)" );

      // The rest of the project is untouched by the new chunk.
      Assert.AreEqual( 1, reloaded.Maps.Count );
      Assert.AreEqual( 1, reloaded.MapStrings.Count );
      Assert.AreEqual( 5, reloaded.MapStrings[0].StringID );
    }



    [TestMethod]
    public void TestSavingTwiceGivesTheSameBytes()
    {
      // The first load normalizes the bare LF / CR of "Mixed endings" to
      // CRLF, so the comparison starts from the second save.
      var project = BuildProject();
      var first = Reload( project ).SaveToBuffer();
      var second = Load( first ).SaveToBuffer();
      CollectionAssert.AreEqual( first.Data(), second.Data(), "a save/load/save round trip is byte-stable" );
    }



    [TestMethod]
    public void TestEveryCustomExportFieldIsPersisted()
    {
      // Reflection auto-guard: a field added to CustomExport without a chunk
      // writer/reader fails here by name.
      var project = new MapProject();
      var export = project.AddCustomExport( "guard" );
      var fields = typeof( MapProject.CustomExport ).GetFields( BindingFlags.Public | BindingFlags.Instance );
      int n = 0;
      foreach ( var field in fields )
      {
        ++n;
        if ( field.FieldType == typeof( string ) )
        {
          field.SetValue( export, "val_" + n );
        }
        else if ( field.FieldType == typeof( bool ) )
        {
          field.SetValue( export, !(bool)field.GetValue( export ) );
        }
        else if ( field.FieldType == typeof( int ) )
        {
          field.SetValue( export, 100 + n );
        }
        else
        {
          Assert.Fail( "CustomExport field '" + field.Name + "' has type " + field.FieldType.Name
                     + " which this guard does not handle. Add it to the MAP_CUSTOM_EXPORT writer, the reader, and this test." );
        }
      }
      Assert.IsTrue( fields.Length >= 7, "expected the seven known fields" );

      var reloaded = Reload( project );
      Assert.AreEqual( 1, reloaded.CustomExports.Count );
      foreach ( var field in fields )
      {
        Assert.AreEqual( field.GetValue( export ), field.GetValue( reloaded.CustomExports[0] ),
                         "CustomExport." + field.Name + " did not survive save/load - add it to MAP_CUSTOM_EXPORT" );
      }
    }



    // ---------------------------------------------------------- compatibility

    [TestMethod]
    public void TestNoDefinitionsWritesNoChunk()
    {
      var project = new MapProject();
      project.Maps.Add( NewMap( "Arrival", 1 ) );
      Assert.AreEqual( 0, CustomExportChunkCount( project.SaveToBuffer() ), "no definitions = no MAP_CUSTOM_EXPORT chunk" );
      Assert.AreEqual( 0, Reload( project ).CustomExports.Count );

      Assert.AreEqual( 2, CustomExportChunkCount( BuildProject().SaveToBuffer() ), "one chunk per definition" );
    }



    [TestMethod]
    public void TestTheExportSettingsChunkStaysLast()
    {
      // TestCharsetCompression fakes an older file by cutting fields off the
      // LAST top-level chunk, which must remain MAP_PROJECT_EXPORT_SETTINGS.
      var chunks = TopLevelChunks( BuildProject().SaveToBuffer() );
      Assert.AreEqual( FileChunkConstants.MAP_PROJECT_EXPORT_SETTINGS, chunks[chunks.Count - 1].Type );
    }



    [TestMethod]
    public void TestAnOlderBuildDroppingTheChunkKeepsTheRest()
    {
      // Older builds skip unknown sub-chunks and never write them back: the
      // definitions are gone, everything else is intact.
      var file = WithCustomExportChunks( BuildProject().SaveToBuffer(), body => null );
      var reloaded = Load( file );
      Assert.AreEqual( 0, reloaded.CustomExports.Count );
      Assert.AreEqual( 1, reloaded.Maps.Count );
      Assert.AreEqual( "Arrival", reloaded.Maps[0].Name );
      Assert.AreEqual( 1, reloaded.MapStrings.Count );
    }



    [TestMethod]
    public void TestATruncatedChunkFallsBackToDefaults()
    {
      // A chunk that ends after FileName (as a file from before Description
      // and Content existed would): the missing strings read as "".
      var project = new MapProject();
      var export = project.AddCustomExport( "Short" );
      export.Directory   = "dir";
      export.FileName    = "file.txt";
      export.Description = "gone";
      export.Content     = "gone too";
      int keep = 4 + 1 + ( 4 + "Short".Length ) + ( 4 + "dir".Length ) + ( 4 + "file.txt".Length );
      var file = WithCustomExportChunks( project.SaveToBuffer(), body => FirstBytes( body, keep ) );

      var reloaded = Load( file );
      Assert.AreEqual( 1, reloaded.CustomExports.Count );
      Assert.AreEqual( 1, reloaded.CustomExports[0].ID );
      Assert.AreEqual( "Short", reloaded.CustomExports[0].Name );
      Assert.IsTrue( reloaded.CustomExports[0].Active );
      Assert.AreEqual( "dir", reloaded.CustomExports[0].Directory );
      Assert.AreEqual( "file.txt", reloaded.CustomExports[0].FileName );
      Assert.AreEqual( "", reloaded.CustomExports[0].Description );
      Assert.AreEqual( "", reloaded.CustomExports[0].Content );
    }



    [TestMethod]
    public void TestAChunkCutInsideAStringReadsThatStringAsEmpty()
    {
      var project = new MapProject();
      var export = project.AddCustomExport( "Short" );
      export.Content = "0123456789";
      // Cut in the middle of Content's bytes: the length prefix promises more
      // than the chunk holds.
      var file = WithCustomExportChunks( project.SaveToBuffer(), body => FirstBytes( body, (int)body.Length - 5 ) );

      var reloaded = Load( file );
      Assert.AreEqual( 1, reloaded.CustomExports.Count );
      Assert.AreEqual( "Short", reloaded.CustomExports[0].Name );
      Assert.AreEqual( "", reloaded.CustomExports[0].Content );
    }



    // ------------------------------------------------------------------ model

    [TestMethod]
    public void TestClearEmptiesTheDefinitions()
    {
      var project = BuildProject();
      project.Clear();
      Assert.AreEqual( 0, project.CustomExports.Count );
    }



    [TestMethod]
    public void TestAddCustomExportHandsOutTheNextID()
    {
      var project = new MapProject();
      Assert.AreEqual( 1, project.AddCustomExport( "a" ).ID );
      Assert.AreEqual( 2, project.AddCustomExport( "b" ).ID );
      Assert.AreEqual( 3, project.AddCustomExport( "c" ).ID );
      Assert.IsTrue( project.CustomExports[2].Active, "new definitions start active" );
      Assert.AreEqual( "c", project.CustomExports[2].Name );

      // Deleting the middle one leaves a gap that is never filled; deleting
      // the newest frees its number (nothing refers to IDs).
      project.CustomExports.RemoveAt( 1 );
      Assert.AreEqual( 4, project.AddCustomExport( "d" ).ID );
      project.CustomExports.RemoveAt( project.CustomExports.Count - 1 );
      Assert.AreEqual( 4, project.AddCustomExport( "e" ).ID );
      Assert.AreEqual( "", project.AddCustomExport( null ).Name );
    }



    [TestMethod]
    public void TestNormalizeLineEndings()
    {
      Assert.AreEqual( "", MapProject.NormalizeLineEndings( null ) );
      Assert.AreEqual( "", MapProject.NormalizeLineEndings( "" ) );
      Assert.AreEqual( "a\r\nb\r\nc\r\nd", MapProject.NormalizeLineEndings( "a\nb\rc\r\nd" ) );
      Assert.AreEqual( "x\r\n\r\ny", MapProject.NormalizeLineEndings( "x\n\ny" ), "an empty line stays one empty line" );
      Assert.AreEqual( "\r\n", MapProject.NormalizeLineEndings( "\r\n" ), "CRLF is never doubled" );
      Assert.AreEqual( "no breaks\there", MapProject.NormalizeLineEndings( "no breaks\there" ) );
    }
  }
}
