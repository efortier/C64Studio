using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using RetroDevStudio.Formats.TextTemplate;



namespace TestProject
{
  /// <summary>
  /// The user's real target: the two per-map blocks of Dreadhold's
  /// make_disk.asm (the RefAddFile pairs and the .disk entries), rendered
  /// for maps 1..4 to the exact bytes of the hand-written original - tab
  /// runs included - from a CRLF template, an LF template and a template
  /// without a trailing line break.
  /// </summary>
  [TestClass]
  public class TestMapExportDreadhold
  {
    private const string TEMPLATE_A =
      "{% for m in exported_maps %}\r\n" +
      "RefAddFile(\"MAP-{{ m.id:D4 }}\", \t\t\t\"../Maps/Exported/MAP_{{ m.id:D4 }}\" + MapExtension, \t\t\t\tCOMPRESSED_MAPS, *)\r\n" +
      "RefAddFile(\"CHARSET-MAP-{{ m.id:D4 }}\", \t\"../Maps/Exported/CHARSET_MAP_{{ m.id:D4 }}\" + MapExtension, \t\tCOMPRESSED_CHARACTER_SETS, *)\r\n" +
      "{% endfor %}\r\n";

    private const string TEMPLATE_A_VIA_FILES =
      "{% for m in exported_maps %}\r\n" +
      "RefAddFile(\"MAP-{{ m.id:D4 }}\", \t\t\t\"../Maps/Exported/{{ m.file.base_name }}\" + MapExtension, \t\t\t\tCOMPRESSED_MAPS, *)\r\n" +
      "RefAddFile(\"CHARSET-MAP-{{ m.id:D4 }}\", \t\"../Maps/Exported/{{ m.file.charset_base_name }}\" + MapExtension, \t\tCOMPRESSED_CHARACTER_SETS, *)\r\n" +
      "{% endfor %}\r\n";

    private const string TEMPLATE_B =
      "{% for m in exported_maps %}\r\n" +
      "\t[name = \"MAP-{{ m.id:D4 }}\", \t\t\ttype = \"prg<\", \tprgFiles = RefFilename(\"MAP-{{ m.id:D4 }}\") ],\r\n" +
      "{% endfor %}\r\n" +
      "{% for m in exported_maps %}\r\n" +
      "\t[name = \"CHARSET-MAP-{{ m.id:D4 }}\", \ttype = \"prg<\", \tprgFiles = RefFilename(\"CHARSET-MAP-{{ m.id:D4 }}\") ],\r\n" +
      "{% endfor %}\r\n";



    private static string ExpectedA()
    {
      var sb = new System.Text.StringBuilder();
      for ( int id = 1; id <= 4; ++id )
      {
        string n = id.ToString( "D4" );
        sb.Append( "RefAddFile(\"MAP-" + n + "\", \t\t\t\"../Maps/Exported/MAP_" + n + "\" + MapExtension, \t\t\t\tCOMPRESSED_MAPS, *)\r\n" );
        sb.Append( "RefAddFile(\"CHARSET-MAP-" + n + "\", \t\"../Maps/Exported/CHARSET_MAP_" + n + "\" + MapExtension, \t\tCOMPRESSED_CHARACTER_SETS, *)\r\n" );
      }
      return sb.ToString();
    }



    private static string ExpectedB()
    {
      var sb = new System.Text.StringBuilder();
      for ( int id = 1; id <= 4; ++id )
      {
        string n = id.ToString( "D4" );
        sb.Append( "\t[name = \"MAP-" + n + "\", \t\t\ttype = \"prg<\", \tprgFiles = RefFilename(\"MAP-" + n + "\") ],\r\n" );
      }
      for ( int id = 1; id <= 4; ++id )
      {
        string n = id.ToString( "D4" );
        sb.Append( "\t[name = \"CHARSET-MAP-" + n + "\", \ttype = \"prg<\", \tprgFiles = RefFilename(\"CHARSET-MAP-" + n + "\") ],\r\n" );
      }
      return sb.ToString();
    }



    /// <summary>Four exported maps with IDs 1..4 (Arrival, Cemetary, Caves 1, Caves 2) on one charset.</summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      var tile = new MapProject.Tile() { Name = "T" };
      tile.Chars.Resize( 1, 1 );
      project.Charsets[0].Tiles.Add( tile );
      project.Charsets[0].ReindexTiles();
      string[] names = { "Arrival", "Cemetary", "Caves 1", "Caves 2" };
      for ( int i = 0; i < names.Length; ++i )
      {
        var map = new MapProject.Map() { Name = names[i], MapID = i + 1, TileSpacingX = 1, TileSpacingY = 1 };
        map.Tiles.Resize( 2, 2 );
        project.Maps.Add( map );
      }
      return project;
    }



    private static MapExportContext Context()
    {
      var project = BuildProject();
      List<string> errors;
      var files = project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.bin", true, true, true, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      var write = new MapExportWriteResult() { MapDirectory = "Z:\\DevC64\\Dreadhold\\Maps\\Exported", MapFilePattern = "MAP_%d.bin", CharsetFilePattern = "CHARSET_MAP_%d.bin" };
      return MapExportSchema.BuildContext( project, files, write, new DateTime( 2026, 10, 18 ) );
    }



    private static string Render( string Source )
    {
      List<TemplateError> errors;
      var template = Template.Compile( Source, MapExportSchema.Schema, out errors );
      Assert.IsNotNull( template, "compile errors:\n" + string.Join( "\n", errors.Select( e => e.ToString() ).ToArray() ) );
      return template.Render( Context() );
    }



    [TestMethod]
    public void TestRefAddFileBlock()
    {
      Assert.AreEqual( ExpectedA(), Render( TEMPLATE_A ) );
    }



    [TestMethod]
    public void TestRefAddFileBlockViaFileFields()
    {
      Assert.AreEqual( ExpectedA(), Render( TEMPLATE_A_VIA_FILES ) );
    }



    [TestMethod]
    public void TestDiskBlock()
    {
      Assert.AreEqual( ExpectedB(), Render( TEMPLATE_B ) );
    }



    [TestMethod]
    public void TestLfTemplateGivesTheSameBytes()
    {
      Assert.AreEqual( ExpectedA(), Render( TEMPLATE_A.Replace( "\r\n", "\n" ) ) );
      Assert.AreEqual( ExpectedB(), Render( TEMPLATE_B.Replace( "\r\n", "\n" ) ) );
    }



    [TestMethod]
    public void TestTemplateWithoutTrailingNewlineGivesTheSameBytes()
    {
      Assert.AreEqual( ExpectedA(), Render( TEMPLATE_A.TrimEnd( '\r', '\n' ) ) );
      Assert.AreEqual( ExpectedB(), Render( TEMPLATE_B.TrimEnd( '\r', '\n' ) ) );
    }



    [TestMethod]
    public void TestBothBlocksInOneDefinition()
    {
      string both = "// generated\r\n" + TEMPLATE_A + "\r\n" + TEMPLATE_B;
      Assert.AreEqual( "// generated\r\n" + ExpectedA() + "\r\n" + ExpectedB(), Render( both ) );
    }



    [TestMethod]
    public void TestTheLastRowIdiom()
    {
      Assert.AreEqual( "MAP-0001, MAP-0002, MAP-0003, MAP-0004",
                       Render( "{% for m in exported_maps %}MAP-{{ m.id:D4 }}{% if not loop.last %}, {% endif %}{% endfor %}" ) );
    }
  }
}
