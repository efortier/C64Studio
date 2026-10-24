using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RetroDevStudio.Formats;
using RetroDevStudio.Formats.TextTemplate;



namespace TestProject
{
  /// <summary>
  /// MapExportSchema: the registry the custom exports render against. Every
  /// field is described and returns its declared kind, every field renders,
  /// the values equal the exported data (records as written, labels as the
  /// sidecars emit them), the preview dry run mirrors the form's defaults,
  /// and the runner compiles / validates / resolves definitions.
  /// </summary>
  [TestClass]
  public class TestMapExportSchema
  {
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
        var tile = new MapProject.Tile() { Name = "Tile " + i };
        tile.Chars.Resize( 1, 1 );
        tile.Chars[0, 0].Character = (byte)( CharBase + i );
        tile.Chars[0, 0].Color = (byte)( 1 + i );
        Charset.Tiles.Add( tile );
      }
      Charset.ReindexTiles();
    }



    private static MapProject.Map AddMap( MapProject Project, string Name, int MapID, int CharsetIndex, int Width, int Height, int Fill )
    {
      var map = new MapProject.Map() { Name = Name, MapID = MapID, CharsetIndex = CharsetIndex, TileSpacingX = 1, TileSpacingY = 1 };
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
    /// Charsets Main (3 tiles, exported) and Caves (2 tiles, NOT exported);
    /// maps Alpha (ID 1, Main, markers + entities, alt. background), Hidden
    /// (ID 2, not exported) and Bravo (ID 3, Caves); marker types with a
    /// duplicate ID; entity types Tick (tile in Main only) and Rat; strings
    /// WELCOME 5 (shown on Alpha) and SIGN 7 (unused). Start map = Alpha.
    /// </summary>
    private static MapProject BuildProject()
    {
      var project = new MapProject();
      AddTiles( project.Charsets[0], 3, 0x10 );
      project.Charsets[0].DisplayName = "Main";
      project.Charsets[0].ExportName = "main.bin";
      int second = project.AddCharset();
      AddTiles( project.Charsets[second], 2, 0x20 );
      project.Charsets[second].DisplayName = "Caves";
      project.Charsets[second].ExportEnabled = false;
      project.BackgroundColor = 6;
      project.MultiColor1 = 4;
      project.MultiColor2 = 14;

      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_MESSAGE, Name = MapProject.MARKER_TYPE_SHOW_MAP_MESSAGE, TagID = 1, ExportSymbol = "MESSAGE" } );
      project.MarkerTypes.Add( new MapProject.MarkerType() { ID = TYPE_SPAWN, Name = "Spawn", TagID = 5, ExportSymbol = "SPAWN", Color = 2, Description = "player start" } );
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
      project.MapStrings[0].Lines[1].Text = "THERE";
      project.MapStrings[0].Lines[1].Justification = MapProject.MAP_STRING_JUSTIFY_CENTER;
      project.MapStrings.Add( new MapProject.MapString() { Label = "2bad", StringID = 7 } );

      var alpha = AddMap( project, "Alpha", 1, 0, 3, 2, 1 );
      alpha.AlternativeBackgroundColor = 3;
      alpha.ExtraDataText = "extra";
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_SPAWN, X = 1, Y = 0, Width = 2, Height = 2, Value1 = 11, Value2 = 22, Value3 = 33, Value4 = 44, GroupId = 2, LinkToID = 8, LinkID = 9, Triggered = true, AutoDisableGroupAfterTrigger = true } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_DOOR, X = 0, Y = 1, Value1 = 3, Enabled = false, Name = "to caves" } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_MESSAGE, X = 2, Y = 1, Value1 = 5 } );
      alpha.Markers.Add( new MapProject.Marker() { Type = TYPE_UNKNOWN, X = 0, Y = 0, Value1 = 77 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_RAT, X = 0, Y = 0, Value1 = 1, Value2 = 2 } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_TICK, X = 1, Y = 1, Enabled = false, Triggered = true } );
      alpha.Entities.Add( new MapProject.Entity() { Type = ENTITY_UNKNOWN, X = 2, Y = 0 } );

      AddMap( project, "Hidden", 2, 0, 2, 2, 0 ).NotExported = true;

      var bravo = AddMap( project, "Bravo", 3, second, 2, 3, 1 );
      bravo.Entities.Add( new MapProject.Entity() { Type = ENTITY_TICK, X = 0, Y = 2 } );
      bravo.Entities.Add( new MapProject.Entity() { Type = ENTITY_RAT, X = 1, Y = 2 } );
      return project;
    }



    private static List<MapProject.GameBinaryMapFile> Export( MapProject Project )
    {
      List<string> errors;
      var files = Project.ExportAsGameBinaryFiles( "MAP_%d.bin", "CHARSET_MAP_%d.bin", true, true, true, out errors );
      Assert.IsNotNull( files, string.Join( "\n", errors ) );
      return files;
    }



    private static readonly DateTime CLOCK = new DateTime( 2026, 10, 18, 12, 34, 56 );



    private static MapExportContext Context( MapProject Project, MapExportWriteResult Write = null )
    {
      var write = Write ?? new MapExportWriteResult()
      {
        ProjectFilePath = "Z:\\DevC64\\Dreadhold\\Maps\\map5.mapproject",
        MapDirectory = "Z:\\out",
        CharsetDirectory = "Z:\\out\\charsets",
        MapFilePattern = "MAP_%d.bin",
        CharsetFilePattern = "CHARSET_MAP_%d.bin"
      };
      return MapExportSchema.BuildContext( Project, Export( Project ), write, CLOCK );
    }



    private static string Render( string Source, MapExportContext Context )
    {
      List<TemplateError> errors;
      var template = Template.Compile( Source, MapExportSchema.Schema, out errors );
      Assert.IsNotNull( template, "compile errors:\n" + string.Join( "\n", errors.Select( e => e.ToString() ).ToArray() ) );
      return template.Render( Context );
    }



    private static string Render( string Source )
    {
      return Render( Source, Context( BuildProject() ) );
    }



    // ---------------------------------------------------------------- schema

    [TestMethod]
    public void TestSchemaValidates()
    {
      var problems = MapExportSchema.Schema.Validate();
      Assert.AreEqual( 0, problems.Count, string.Join( "\n", problems.ToArray() ) );
      Assert.AreSame( MapExportSchema.Schema, MapExportSchema.Schema, "built once" );
    }



    [TestMethod]
    public void TestEveryFieldHasDescription()
    {
      foreach ( var type in MapExportSchema.Schema.ObjectTypes )
      {
        Assert.IsFalse( string.IsNullOrEmpty( type.Description ), "type " + type.Name );
        foreach ( var field in type.Fields )
        {
          Assert.IsFalse( string.IsNullOrEmpty( field.Description ), type.Name + "." + field.Name );
        }
      }
      foreach ( var filter in MapExportSchema.Schema.Filters )
      {
        Assert.IsFalse( string.IsNullOrEmpty( filter.Description ), "filter " + filter.Name );
      }
    }



    [TestMethod]
    public void TestEveryGetterReturnsItsDeclaredKind()
    {
      // Breadth-first over everything reachable from the sample context: each
      // getter's value matches the field's kind.
      var context = Context( BuildProject() );
      var queue = new Queue<KeyValuePair<TemplateType, object>>();
      var seen = new HashSet<object>();
      queue.Enqueue( new KeyValuePair<TemplateType, object>( MapExportSchema.Schema.RootType, context ) );
      int checkedFields = 0;
      while ( queue.Count > 0 )
      {
        var pair = queue.Dequeue();
        if ( ( pair.Value == null )
        ||   !seen.Add( pair.Value ) )
        {
          continue;
        }
        foreach ( var field in pair.Key.Fields )
        {
          object value = field.Getter( pair.Value );
          ++checkedFields;
          string where = pair.Key.Name + "." + field.Name;
          switch ( field.Type.Kind )
          {
            case TemplateTypeKind.Int:
              Assert.IsTrue( TemplateValues.IsInteger( value ), where + " returned " + Describe( value ) );
              break;
            case TemplateTypeKind.String:
              Assert.IsTrue( value is string, where + " returned " + Describe( value ) );
              break;
            case TemplateTypeKind.Bool:
              Assert.IsTrue( value is bool, where + " returned " + Describe( value ) );
              break;
            case TemplateTypeKind.List:
              Assert.IsTrue( value is IList, where + " returned " + Describe( value ) );
              foreach ( var element in (IList)value )
              {
                Assert.IsNotNull( element, where + " holds a null element" );
                queue.Enqueue( new KeyValuePair<TemplateType, object>( field.Type.ElementType, element ) );
              }
              break;
            default:
              if ( value != null )
              {
                queue.Enqueue( new KeyValuePair<TemplateType, object>( field.Type, value ) );
              }
              break;
          }
        }
      }
      Assert.IsTrue( checkedFields > 200, "checked " + checkedFields + " field values" );
    }



    [TestMethod]
    public void TestEveryFieldRendersWithoutError()
    {
      // A generated template that prints every scalar of every type through
      // the loops that reach it; compiling and rendering it must succeed.
      var sb = new System.Text.StringBuilder();
      var counter = new int[] { 0 };
      foreach ( var field in MapExportSchema.Schema.RootType.Fields )
      {
        Emit( sb, field.Name, field.Type, new List<TemplateType>(), counter );
      }
      string output = Render( sb.ToString() );
      Assert.IsTrue( output.Length > 1000, "rendered " + output.Length + " characters" );
    }



    private static void Emit( System.Text.StringBuilder Into, string Path, TemplateType Type, List<TemplateType> Stack, int[] Counter )
    {
      switch ( Type.Kind )
      {
        case TemplateTypeKind.Int:
        case TemplateTypeKind.String:
        case TemplateTypeKind.Bool:
          Into.Append( "{{ " + Path + " }}|" );
          return;
        case TemplateTypeKind.List:
          {
            if ( Stack.Contains( Type.ElementType ) )
            {
              Into.Append( "{{ " + Path + ".count }}|" );
              return;
            }
            string variable = "v" + ( Counter[0]++ );
            Into.Append( "{% for " + variable + " in " + Path + " %}{{ loop.index }}:" );
            Stack.Add( Type.ElementType );
            foreach ( var field in Type.ElementType.Fields )
            {
              Emit( Into, variable + "." + field.Name, field.Type, Stack, Counter );
            }
            Stack.RemoveAt( Stack.Count - 1 );
            Into.Append( "{% endfor %}\r\n" );
            return;
          }
        default:
          {
            if ( Stack.Contains( Type ) )
            {
              return;
            }
            Stack.Add( Type );
            foreach ( var field in Type.Fields )
            {
              Emit( Into, Path + "." + field.Name, field.Type, Stack, Counter );
            }
            Stack.RemoveAt( Stack.Count - 1 );
            return;
          }
      }
    }



    private static string Describe( object Value )
    {
      return ( Value == null ) ? "null" : Value.GetType().Name;
    }



    // ---------------------------------------------------------------- values

    [TestMethod]
    public void TestRootNamesAndCounts()
    {
      Assert.AreEqual( "3 3 2 2 1 4 2 2 1 false",
                       Render( "{{ project.map_count }} {{ maps.count }} {{ exported_maps.count }} {{ charsets.count }} {{ exported_charsets.count }} {{ marker_types.count }} {{ entity_types.count }} {{ map_strings.count }} {{ exported_map_strings.count }} {{ export.is_preview }}" ) );
      Assert.AreEqual( "3 2 2 1 4 2 2 1",
                       Render( "{{ project.map_count }} {{ project.exported_map_count }} {{ project.charset_count }} {{ project.exported_charset_count }} {{ project.marker_type_count }} {{ project.entity_type_count }} {{ project.map_string_count }} {{ project.exported_map_string_count }}" ) );
    }



    [TestMethod]
    public void TestProjectNamesAndClock()
    {
      Assert.AreEqual( "map5|map5.mapproject|Z:\\DevC64\\Dreadhold\\Maps\\map5.mapproject|Z:\\DevC64\\Dreadhold\\Maps|2026-10-18 12:34:56",
                       Render( "{{ project.name }}|{{ project.file_name }}|{{ project.file_path }}|{{ project.directory }}|{{ project.now }}" ) );
      Assert.AreEqual( "6 4 14 0", Render( "{{ project.background_color }} {{ project.multicolor1 }} {{ project.multicolor2 }} {{ project.bgcolor4 }}" ) );
    }



    [TestMethod]
    public void TestMapIndexVersusPosition()
    {
      Assert.AreEqual( "0:0:true 1:-1:false 2:1:true ", Render( "{% for m in maps %}{{ m.position }}:{{ m.index }}:{{ m.is_exported }} {% endfor %}" ) );
      Assert.AreEqual( "Alpha Bravo ", Render( "{% for m in exported_maps %}{{ m.name }} {% endfor %}" ) );
      Assert.AreEqual( "1 3 ", Render( "{% for m in exported_maps %}{{ m.id }} {% endfor %}" ) );
    }



    [TestMethod]
    public void TestNotExportedMapHasZeroLayoutFieldsAndNoFile()
    {
      Assert.AreEqual( "0 0 0 0 0 0 false || 6",
                       Render( "{% for m in maps %}{% if not m.is_exported %}{{ m.width }} {{ m.height }} {{ m.marker_count }} {{ m.entity_count }} {{ m.string_count }} {{ m.binary_size }} {{ m.has_file }} {{ m.file_name }}|{{ m.file.name }}| {{ m.background_color }}{% endif %}{% endfor %}" ) );
    }



    [TestMethod]
    public void TestMapLabelsAndStartMap()
    {
      Assert.AreEqual( "ALPHA* BRAVO ", Render( "{% for m in exported_maps %}{{ m.label }}{% if m.is_start %}*{% endif %} {% endfor %}" ) );
      Assert.AreEqual( "1 Alpha true 0", Render( "{{ project.start_map_id }} {{ project.start_map.name }} {{ project.has_start_map }} {{ project.start_map_index }}" ) );
      Assert.AreEqual( "false", Render( "{% for m in maps %}{% if m.id == 2 %}{{ m.has_label }}{% endif %}{% endfor %}" ), "a map not exported has no label" );
    }



    [TestMethod]
    public void TestLabelFilterEqualsSidecarNormalization()
    {
      var project = BuildProject();
      project.Maps[0].Name = "\u00c9glise 1!";
      var context = Context( project );
      string viaFilter = Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.name|label }}{% endif %}{% endfor %}", context );
      string viaField  = Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.label }}{% endif %}{% endfor %}", context );
      Assert.AreEqual( "EGLISE_1_", viaFilter );
      Assert.AreEqual( viaFilter, viaField );
      StringAssert.Contains( project.GenerateMapLabelsAsm(), ".const MAP_ID_EGLISE_1_" );
    }



    [TestMethod]
    public void TestMapFields()
    {
      Assert.AreEqual( "Alpha|3|2|3|2|1|1|0|0|3|4|14|6|extra|true|0",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.name }}|{{ m.width }}|{{ m.height }}|{{ m.tile_width }}|{{ m.tile_height }}|{{ m.tile_spacing_x }}|{{ m.tile_spacing_y }}|{{ m.charset_index }}|{{ m.charset_export_index }}|{{ m.background_color }}|{{ m.multicolor1 }}|{{ m.multicolor2 }}|{{ m.charset.tile_count * 2 }}|{{ m.extra_data }}|{{ m.has_extra_data }}|{{ m.index }}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "Bravo|1|-1|Caves|6",
                       Render( "{% for m in exported_maps %}{% if loop.last %}{{ m.name }}|{{ m.charset_index }}|{{ m.charset_export_index }}|{{ m.charset.name }}|{{ m.background_color }}{% endif %}{% endfor %}" ), "Bravo's charset is not export-enabled; project background" );
    }



    [TestMethod]
    public void TestMarkerRecordsMatchTheLayout()
    {
      var project = BuildProject();
      var files = Export( project );
      var context = MapExportSchema.BuildContext( project, files, null, CLOCK );
      var expected = new System.Text.StringBuilder();
      foreach ( var record in files[0].Layout.Maps[0].Markers )
      {
        expected.Append( record.Index + ":" + record.Offset + ":" + record.Tag + ":" + record.Marker.X + "," + record.Marker.Y + ":" + record.Flags + ";" );
      }
      Assert.AreEqual( expected.ToString(),
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for r in m.markers %}{{ r.index }}:{{ r.offset }}:{{ r.tag }}:{{ r.x }},{{ r.y }}:{{ r.flags }};{% endfor %}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "7", Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.marker_count }}{% endif %}{% endfor %}", context ) );
      // Tags, types and symbols: unknown type first (tag 0, no type), then
      // message, door (the FIRST type with its ID), four spawn cells.
      Assert.AreEqual( "0::false:99 1:MESSAGE:true:0 3:DOOR:true:2 5:SPAWN:true:1 5:SPAWN:true:1 5:SPAWN:true:1 5:SPAWN:true:1 ",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for r in m.markers %}{{ r.tag }}:{{ r.symbol }}:{{ r.has_type }}:{{ r.type_id }} {% endfor %}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "to caves|3|false|false|false|0|0|0|0|0",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for r in m.markers %}{% if r.tag == 3 %}{{ r.name }}|{{ r.value1 }}|{{ r.enabled }}|{{ r.triggered }}|{{ r.auto_disable_group }}|{{ r.value2 }}|{{ r.group_id }}|{{ r.link_to_id }}|{{ r.link_id }}|{{ r.value3 }}{% endif %}{% endfor %}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "11|22|33|44|2|8|9|true|true|7",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for r in m.markers %}{% if r.tag == 5 and loop.last %}{{ r.value1 }}|{{ r.value2 }}|{{ r.value3 }}|{{ r.value4 }}|{{ r.group_id }}|{{ r.link_to_id }}|{{ r.link_id }}|{{ r.triggered }}|{{ r.auto_disable_group }}|{{ r.flags }}{% endif %}{% endfor %}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "Spawn:5:SPAWN:2:player start:1 Door:3:DOOR:1::1 ",
                       Render( "{% for t in marker_types %}{% if t.id == 1 or t.index == 2 %}{{ t.name }}:{{ t.tag }}:{{ t.symbol }}:{{ t.color }}:{{ t.description }}:{{ t.placed_count }} {% endif %}{% endfor %}", context ) );
    }



    [TestMethod]
    public void TestEntityRecordsMatchTheLayout()
    {
      Assert.AreEqual( "0:0:0:false::7 1:1:1:true:TICK:0 2:2:2:true:RAT:1 ",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for e in m.entities %}{{ e.index }}:{{ e.tag }}:{{ e.tile }}:{{ e.has_tile }}:{{ e.symbol }}:{{ e.type_id }} {% endfor %}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "0:1:255:false:TICK 1:2:0:true:RAT ",
                       Render( "{% for m in exported_maps %}{% if loop.last %}{% for e in m.entities %}{{ e.index }}:{{ e.tag }}:{{ e.tile }}:{{ e.has_tile }}:{{ e.symbol }} {% endfor %}{% endif %}{% endfor %}" ), "Tick has no tile in Caves" );
      Assert.AreEqual( "1|1|false|true|0|0|Tick",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for e in m.entities %}{% if e.tag == 1 %}{{ e.x }}|{{ e.y }}|{{ e.enabled }}|{{ e.triggered }}|{{ e.value1 }}|{{ e.value2 }}|{{ e.type.name }}{% endif %}{% endfor %}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "Tick:1:TICK:true:2 Rat:2:RAT:true:2 ", Render( "{% for t in entity_types %}{{ t.name }}:{{ t.tag }}:{{ t.symbol }}:{{ t.has_symbol }}:{{ t.placed_count }} {% endfor %}" ) );
    }



    [TestMethod]
    public void TestStringTablesMatchTheLayout()
    {
      var project = BuildProject();
      var files = Export( project );
      var context = MapExportSchema.BuildContext( project, files, null, CLOCK );
      var record = files[0].Layout.Strings[0];
      Assert.AreEqual( "0:5:WELCOME:" + record.Offset + ":" + record.Size + ":true:0|",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{% for s in m.strings %}{{ s.index }}:{{ s.id }}:{{ s.label }}:{{ s.offset }}:{{ s.size }}:{{ s.is_exported }}:{{ s.export_index }}|{% endfor %}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "0", Render( "{% for m in exported_maps %}{% if loop.last %}{{ m.string_count }}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "0:5:WELCOME:true:0:true:2:HELLO THERE 1:7:2bad:false:-1:false:0: ",
                       Render( "{% for s in map_strings %}{{ s.index }}:{{ s.id }}:{{ s.label }}:{{ s.is_exported }}:{{ s.export_index }}:{{ s.is_valid_label }}:{{ s.line_count }}:{{ s.text }} {% endfor %}", context ) );
      Assert.AreEqual( "0:5:-1:" + record.Size, Render( "{% for s in exported_map_strings %}{{ s.index }}:{{ s.id }}:{{ s.offset }}:{{ s.size }}{% endfor %}", context ) );
      Assert.AreEqual( "0:HELLO:true:left:false 1:THERE:true:center:false 2::false:left:false 3::false:left:false 4::false:left:false ",
                       Render( "{% for s in map_strings %}{% if loop.first %}{% for l in s.lines %}{{ l.index }}:{{ l.text }}:{{ l.has_text }}:{{ l.justification_name }}:{{ l.has_terminator }} {% endfor %}{% endif %}{% endfor %}", context ) );
    }



    [TestMethod]
    public void TestCharsetFields()
    {
      Assert.AreEqual( "0:0:true:Main:main.bin:main:MAIN:3:2:1:true;1:-1:false:Caves::::2:1:1:true;",
                       Render( "{% for c in charsets %}{{ c.index }}:{{ c.export_index }}:{{ c.is_exported }}:{{ c.name }}:{{ c.export_name }}:{{ c.export_base_name }}:{{ c.label }}:{{ c.tile_count }}:{{ c.map_count }}:{{ c.exported_map_count }}:{{ c.has_name }};{% endfor %}" ) );
      Assert.AreEqual( "TICK:1:true RAT:2:true ", Render( "{% for c in charsets %}{% if loop.first %}{% for t in c.entity_tiles %}{{ t.symbol }}:{{ t.tile_index }}:{{ t.has_tile }} {% endfor %}{% endif %}{% endfor %}" ), "in entity type order" );
      Assert.AreEqual( "RAT:0:Tile 0 ", Render( "{% for c in charsets %}{% if loop.last %}{% for t in c.entity_tiles %}{{ t.symbol }}:{{ t.tile_index }}:{{ t.tile.name }} {% endfor %}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "Alpha Hidden |Alpha ", Render( "{% for c in charsets %}{% if loop.first %}{% for m in c.maps %}{{ m.name }} {% endfor %}|{% for m in c.exported_maps %}{{ m.name }} {% endfor %}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "256 2048", Render( "{% for c in charsets %}{% if loop.first %}{{ c.character_count }} {{ c.data_size }}{% endif %}{% endfor %}" ) );
    }



    [TestMethod]
    public void TestTileFields()
    {
      Assert.AreEqual( "0:Tile 0:TILE_0:true:true:0:1x1:1 1:Tile 1:TILE_1:true:true:0:1x1:1 2:Tile 2:TILE_2:true:true:0:1x1:1 ",
                       Render( "{% for c in charsets %}{% if loop.first %}{% for t in c.tiles %}{{ t.index }}:{{ t.name }}:{{ t.label }}:{{ t.passable }}:{{ t.exported_on_map }}:{{ t.group_id }}:{{ t.width }}x{{ t.height }}:{{ t.char_count }} {% endfor %}{% endif %}{% endfor %}" ) );
      Assert.AreEqual( "0:0,0:$11:2 ", Render( "{% for c in charsets %}{% if loop.first %}{% for t in c.tiles %}{% if t.index == 1 %}{% for ch in t.chars %}{{ ch.index }}:{{ ch.x }},{{ ch.y }}:${{ ch.character:X2 }}:{{ ch.color }} {% endfor %}{% endif %}{% endfor %}{% endif %}{% endfor %}" ) );
    }



    // ------------------------------------------------------------- file fields

    [TestMethod]
    public void TestFileFieldsOfARealRun()
    {
      var write = new MapExportWriteResult()
      {
        MapDirectory = "Z:\\out", CharsetDirectory = "Z:\\out\\cs", CompressDirectory = "", MapFilePattern = "MAP_%d.bin", CharsetFilePattern = "CHARSET_MAP_%d.bin",
        CompressedFilePattern = "MAP_%d.zx0", CharsetCompressedFilePattern = "CHARSET_MAP_%d.zx0", Compress = true, CompressCharsets = true,
        LoadAddress = 0x4000, CharsetLoadAddress = 0x2000, DefFiles = true
      };
      write.Files[1] = new MapExportFileResult()
      {
        MapPath = "Z:\\out\\MAP_0001.bin", MapSize = 123, CompressedPath = "Z:\\out\\MAP_0001.zx0", CompressedSize = 50,
        CharsetPath = "Z:\\out\\cs\\CHARSET_MAP_0001.bin", CharsetSize = 2050, CharsetCompressedPath = "Z:\\out\\cs\\CHARSET_MAP_0001.zx0", CharsetCompressedSize = 700,
        DefPath = "Z:\\out\\MAP_0001.def"
      };
      var context = Context( BuildProject(), write );
      Assert.AreEqual( "MAP_0001.bin|MAP_0001|.bin|Z:\\out\\MAP_0001.bin|Z:\\out|123|16384|true|MAP_0001.zx0|MAP_0001|Z:\\out\\MAP_0001.zx0|50",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.file.name }}|{{ m.file.base_name }}|{{ m.file.extension }}|{{ m.file.path }}|{{ m.file.directory }}|{{ m.file.size }}|{{ m.file.load_address }}|{{ m.file.compressed }}|{{ m.file.compressed_name }}|{{ m.file.compressed_base_name }}|{{ m.file.compressed_path }}|{{ m.file.compressed_size }}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "true|CHARSET_MAP_0001.bin|CHARSET_MAP_0001|Z:\\out\\cs\\CHARSET_MAP_0001.bin|2050|8192|true|CHARSET_MAP_0001.zx0|Z:\\out\\cs\\CHARSET_MAP_0001.zx0|700|true|MAP_0001.def|Z:\\out\\MAP_0001.def",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.file.has_charset }}|{{ m.file.charset_name }}|{{ m.file.charset_base_name }}|{{ m.file.charset_path }}|{{ m.file.charset_size }}|{{ m.file.charset_load_address }}|{{ m.file.charset_compressed }}|{{ m.file.charset_compressed_name }}|{{ m.file.charset_compressed_path }}|{{ m.file.charset_compressed_size }}|{{ m.file.has_def }}|{{ m.file.def_name }}|{{ m.file.def_path }}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "MAP_0001.bin CHARSET_MAP_0001.bin MAP_0001.zx0 CHARSET_MAP_0001.zx0",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.file_name }} {{ m.charset_file_name }} {{ m.compressed_file_name }} {{ m.charset_compressed_file_name }}{% endif %}{% endfor %}", context ) );
      // Bravo has no result entry: its paths are planned from the folders, sizes from the buffers, compressed sizes unknown.
      Assert.AreEqual( "Z:\\out\\MAP_0003.bin|Z:\\out\\MAP_0003.zx0|-1|Z:\\out\\cs\\CHARSET_MAP_0003.bin|-1",
                       Render( "{% for m in exported_maps %}{% if loop.last %}{{ m.file.path }}|{{ m.file.compressed_path }}|{{ m.file.compressed_size }}|{{ m.file.charset_path }}|{{ m.file.charset_compressed_size }}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "Z:\\out|Z:\\out\\cs|Z:\\out|MAP_%d.bin|16384|8192|true|true|ZX0|true|true|true|true|false",
                       Render( "{{ export.map_directory }}|{{ export.charset_directory }}|{{ export.compress_directory }}|{{ export.map_file_pattern }}|{{ export.load_address }}|{{ export.charset_load_address }}|{{ export.compress }}|{{ export.compress_charsets }}|{{ export.compressor }}|{{ export.markers }}|{{ export.colors }}|{{ export.passable }}|{{ export.def_files }}|{{ export.is_preview }}", context ) );
    }



    [TestMethod]
    public void TestPreviewContextMirrorsTheFormAndKnowsNoCompressedSizes()
    {
      var project = BuildProject();
      var s = project.Settings.GameBinary;
      s.SaveOnExport = true;
      s.ExportDirectory = "Z:\\preview\\maps";
      s.ExportFilename = "";                      // the form substitutes MAP_%d.bin
      s.CharsetExportDirectory = "";              // blank = the project's folder
      s.CompressMap = true;
      s.CompressFilename = "MAP_%d.zx0";
      s.PrefixLoadAddress = true;
      s.PrefixLoadAddressHex = "4000";
      s.CharsetPrefixLoadAddress = true;
      s.CharsetPrefixLoadAddressHex = "2000";
      List<string> errors;
      var context = MapExportSchema.BuildPreviewContext( project, "Z:\\DevC64\\Dreadhold\\Maps\\map5.mapproject", out errors );
      Assert.IsNotNull( context, string.Join( "\n", ( errors ?? new List<string>() ).ToArray() ) );
      var files = Export( project );
      int alphaSize = (int)files[0].Data.Length + 2;
      Assert.AreEqual( "true|MAP_%d.bin|Z:\\preview\\maps|Z:\\DevC64\\Dreadhold\\Maps|Z:\\preview\\maps|16384|8192|true|false",
                       Render( "{{ export.is_preview }}|{{ export.map_file_pattern }}|{{ export.map_directory }}|{{ export.charset_directory }}|{{ export.compress_directory }}|{{ export.load_address }}|{{ export.charset_load_address }}|{{ export.compress }}|{{ export.compress_charsets }}", context ) );
      Assert.AreEqual( "MAP_0001.bin|Z:\\preview\\maps\\MAP_0001.bin|" + alphaSize + "|true|MAP_0001.zx0|Z:\\preview\\maps\\MAP_0001.zx0|-1|Z:\\DevC64\\Dreadhold\\Maps\\CHARSET_MAP_0001.bin|2050|false|-1",
                       Render( "{% for m in exported_maps %}{% if loop.first %}{{ m.file.name }}|{{ m.file.path }}|{{ m.file.size }}|{{ m.file.compressed }}|{{ m.file.compressed_name }}|{{ m.file.compressed_path }}|{{ m.file.compressed_size }}|{{ m.file.charset_path }}|{{ m.file.charset_size }}|{{ m.file.charset_compressed }}|{{ m.file.charset_compressed_size }}{% endif %}{% endfor %}", context ) );
      Assert.AreEqual( "map5", Render( "{{ project.name }}", context ) );
    }



    [TestMethod]
    public void TestPreviewOfAnUnsavedProjectFallsBackToTheMapFolder()
    {
      var project = BuildProject();
      project.Settings.GameBinary.SaveOnExport = true;
      project.Settings.GameBinary.ExportDirectory = "Z:\\preview";
      List<string> errors;
      var context = MapExportSchema.BuildPreviewContext( project, "", out errors );
      Assert.IsNotNull( context );
      Assert.AreEqual( "|Z:\\preview|Z:\\preview|-1", Render( "{{ project.name }}|{{ export.map_directory }}|{{ export.charset_directory }}|{{ export.load_address }}", context ) );
    }



    [TestMethod]
    public void TestPreviewRefusedWhenTheExportWouldBe()
    {
      var project = BuildProject();
      project.Maps[2].MapID = 1;    // duplicate of Alpha's
      List<string> errors;
      Assert.IsNull( MapExportSchema.BuildPreviewContext( project, "", out errors ) );
      Assert.IsTrue( errors.Count > 0 );
      StringAssert.Contains( errors[0], "both have map ID 1" );
    }



    // ------------------------------------------------------------------ runner

    [TestMethod]
    public void TestRunnerCompilesOnlyActiveDefinitionsAndReportsProblems()
    {
      var project = BuildProject();
      var good = project.AddCustomExport( "Good" );
      good.FileName = "good.asm";
      good.Content = "{% for m in exported_maps %}{{ m.id:D4 }}\r\n{% endfor %}";
      var inactive = project.AddCustomExport( "Inactive" );
      inactive.Active = false;
      inactive.FileName = "";
      inactive.Content = "{{ nope }}";
      var broken = project.AddCustomExport( "Broken" );
      broken.FileName = "broken.asm";
      broken.Content = "x\r\n{{ exported_maps.nope }}";
      var noName = project.AddCustomExport( "" );
      noName.FileName = "";
      noName.Content = "";
      var folderInName = project.AddCustomExport( "Folder" );
      folderInName.FileName = "sub\\file.asm";
      var badChars = project.AddCustomExport( "Chars" );
      badChars.FileName = "a|b.asm";

      List<string> errors;
      var jobs = CustomExportRunner.Prepare( project.CustomExports, out errors );
      Assert.AreEqual( 1, jobs.Count );
      Assert.AreSame( good, jobs[0].Definition );
      CollectionAssert.AreEqual( new[]
      {
        "custom export 'Broken': line 2, col 18: 'exported_maps' is a list; it has no field 'nope' (only 'count') - loop over it with {% for x in exported_maps %}",
        "custom export '(unnamed, ID 4)': the file name is empty",
        "custom export 'Folder': the file name 'sub\\file.asm' must not contain a folder - put the folder in the directory field",
        "custom export 'Chars': the file name 'a|b.asm' contains characters a file name cannot have"
      }, errors );

      Assert.AreEqual( "0001\r\n0003\r\n", CustomExportRunner.Render( jobs[0], Context( project ) ) );
    }



    [TestMethod]
    public void TestRunnerResolvesDirectories()
    {
      Assert.AreEqual( "Z:\\maps", CustomExportRunner.ResolveDirectory( "", "Z:\\maps", "Z:\\proj" ), "blank = the map folder" );
      Assert.AreEqual( "Z:\\proj\\..\\src", CustomExportRunner.ResolveDirectory( "..\\src", "Z:\\maps", "Z:\\proj" ), "relative = from the project's folder" );
      Assert.AreEqual( "Z:\\maps\\gen", CustomExportRunner.ResolveDirectory( "gen", "Z:\\maps", "" ), "relative without a project folder = from the map folder" );
      Assert.AreEqual( "Z:\\elsewhere", CustomExportRunner.ResolveDirectory( "Z:\\elsewhere", "Z:\\maps", "Z:\\proj" ), "rooted = as is" );

      var project = BuildProject();
      var a = project.AddCustomExport( "A" );
      a.FileName = "files.asm";
      a.Directory = "..\\src";
      var b = project.AddCustomExport( "B" );
      b.FileName = "FILES.asm";
      b.Directory = "Z:\\proj\\src";
      var c = project.AddCustomExport( "C" );
      c.FileName = "MAP_0001.bin";
      var d = project.AddCustomExport( "D" );
      d.FileName = "ok.inc";
      List<string> errors;
      var jobs = CustomExportRunner.Prepare( project.CustomExports, out errors );
      Assert.AreEqual( 4, jobs.Count, string.Join( "\n", errors.ToArray() ) );
      CustomExportRunner.ResolveDirectories( jobs, "Z:\\maps", "Z:\\proj\\maps", new[] { "Z:\\maps\\MAP_0001.bin" }, out errors );
      Assert.AreEqual( "Z:\\proj\\src\\files.asm", jobs[0].Path );
      Assert.AreEqual( "Z:\\proj\\src\\FILES.asm", jobs[1].Path );
      Assert.AreEqual( "Z:\\maps\\MAP_0001.bin", jobs[2].Path );
      Assert.AreEqual( "Z:\\maps\\ok.inc", jobs[3].Path );
      Assert.AreEqual( "Z:\\maps", jobs[3].Directory );
      CollectionAssert.AreEqual( new[]
      {
        "custom exports 'A' and 'B' both write Z:\\proj\\src\\FILES.asm",
        "custom export 'C' would overwrite a file the export writes: Z:\\maps\\MAP_0001.bin"
      }, errors );
    }
  }
}
