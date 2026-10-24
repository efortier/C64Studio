using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RetroDevStudio.Formats.TextTemplate;



namespace RetroDevStudio.Formats
{
  /// <summary>
  /// The template schema of the custom text exports: every name a template
  /// may use, typed and described (the Fields dialog lists exactly this),
  /// and the builders of the context a template renders against - after a
  /// real game-binary export (BuildContext) or as a dry run from the
  /// project's settings (BuildPreviewContext).
  /// </summary>
  public static class MapExportSchema
  {
    private static readonly object    s_Lock = new object();
    private static TemplateSchema     s_Schema = null;
    private static readonly Regex     s_LabelPattern = new Regex( "^[A-Za-z_][A-Za-z0-9_]*$" );



    /// <summary>Built once; the same instance serves every compile, preview and the Fields dialog.</summary>
    public static TemplateSchema Schema
    {
      get
      {
        lock ( s_Lock )
        {
          if ( s_Schema == null )
          {
            s_Schema = Build();
          }
          return s_Schema;
        }
      }
    }



    private static TemplateSchema Build()
    {
      var s = new TemplateSchema();
      var project    = s.DefineObjectType( "project", "the map project: its lists, counts, colours and start map" );
      var export     = s.DefineObjectType( "export", "the settings and folders of this export run" );
      var map        = s.DefineObjectType( "map", "one map of the project" );
      var mapFile    = s.DefineObjectType( "map_file", "the files written for one exported map" );
      var marker     = s.DefineObjectType( "marker", "one marker record of a map file, as written: one cell, sorted by tag" );
      var entity     = s.DefineObjectType( "entity", "one entity record of a map file, as written: sorted by tag" );
      var mapString  = s.DefineObjectType( "map_string", "one map string" );
      var stringLine = s.DefineObjectType( "map_string_line", "one of the five lines of a map string" );
      var charset    = s.DefineObjectType( "charset", "one character set with its tile library" );
      var entityTile = s.DefineObjectType( "entity_tile", "an entity type's tile in one character set" );
      var tile       = s.DefineObjectType( "tile", "one tile of a tile library" );
      var tileChar   = s.DefineObjectType( "tile_char", "one character cell of a tile" );
      var markerType = s.DefineObjectType( "marker_type", "a marker type" );
      var entityType = s.DefineObjectType( "entity_type", "an entity type" );

      var maps        = s.ListOf( map );
      var charsets    = s.ListOf( charset );
      var markerTypes = s.ListOf( markerType );
      var entityTypes = s.ListOf( entityType );
      var mapStrings  = s.ListOf( mapString );

      // ---- root names
      s.RootType.AddField<MapExportContext>( "project", project, "the map project", c => c );
      s.RootType.AddField<MapExportContext>( "maps", maps, "every map in editor order, including maps flagged 'Map not exported'", c => c.Maps );
      s.RootType.AddField<MapExportContext>( "exported_maps", maps, "the exported maps in export order", c => c.ExportedMaps );
      s.RootType.AddField<MapExportContext>( "charsets", charsets, "every character set in editor order", c => c.Charsets );
      s.RootType.AddField<MapExportContext>( "exported_charsets", charsets, "the export-enabled character sets in compacted order", c => c.ExportedCharsets );
      s.RootType.AddField<MapExportContext>( "marker_types", markerTypes, "the marker types in list order", c => c.MarkerTypes );
      s.RootType.AddField<MapExportContext>( "entity_types", entityTypes, "the entity types in list order", c => c.EntityTypes );
      s.RootType.AddField<MapExportContext>( "map_strings", mapStrings, "every map string in list order", c => c.MapStrings );
      s.RootType.AddField<MapExportContext>( "exported_map_strings", mapStrings, "the map strings the project-wide export list holds (used by a message marker; all of them when no message marker type exists)", c => c.ExportedMapStrings );
      s.RootType.AddField<MapExportContext>( "export", export, "the settings and folders of this export run", c => c.Write );

      // ---- project
      project.AddField<MapExportContext>( "name", TemplateType.String, "the project file name without extension; empty when never saved", c => c.ProjectName );
      project.AddField<MapExportContext>( "file_name", TemplateType.String, "the project file name with extension", c => c.ProjectFileName );
      project.AddField<MapExportContext>( "file_path", TemplateType.String, "the project file's full path", c => c.ProjectFilePath );
      project.AddField<MapExportContext>( "directory", TemplateType.String, "the project file's folder", c => c.ProjectDirectory );
      project.AddField<MapExportContext>( "maps", maps, "every map in editor order", c => c.Maps );
      project.AddField<MapExportContext>( "exported_maps", maps, "the exported maps in export order", c => c.ExportedMaps );
      project.AddField<MapExportContext>( "charsets", charsets, "every character set", c => c.Charsets );
      project.AddField<MapExportContext>( "exported_charsets", charsets, "the export-enabled character sets", c => c.ExportedCharsets );
      project.AddField<MapExportContext>( "marker_types", markerTypes, "the marker types", c => c.MarkerTypes );
      project.AddField<MapExportContext>( "entity_types", entityTypes, "the entity types", c => c.EntityTypes );
      project.AddField<MapExportContext>( "map_strings", mapStrings, "every map string", c => c.MapStrings );
      project.AddField<MapExportContext>( "exported_map_strings", mapStrings, "the project-wide exported map strings", c => c.ExportedMapStrings );
      project.AddField<MapExportContext>( "map_count", TemplateType.Int, "number of maps", c => c.Maps.Count );
      project.AddField<MapExportContext>( "exported_map_count", TemplateType.Int, "number of exported maps", c => c.ExportedMaps.Count );
      project.AddField<MapExportContext>( "charset_count", TemplateType.Int, "number of character sets", c => c.Charsets.Count );
      project.AddField<MapExportContext>( "exported_charset_count", TemplateType.Int, "number of export-enabled character sets", c => c.ExportedCharsets.Count );
      project.AddField<MapExportContext>( "marker_type_count", TemplateType.Int, "number of marker types", c => c.MarkerTypes.Count );
      project.AddField<MapExportContext>( "entity_type_count", TemplateType.Int, "number of entity types", c => c.EntityTypes.Count );
      project.AddField<MapExportContext>( "map_string_count", TemplateType.Int, "number of map strings", c => c.MapStrings.Count );
      project.AddField<MapExportContext>( "exported_map_string_count", TemplateType.Int, "number of project-wide exported map strings", c => c.ExportedMapStrings.Count );
      project.AddField<MapExportContext>( "start_map", map, "the map the game starts on (MAP_START_ID); nothing when no map is exported", c => c.StartMap );
      project.AddField<MapExportContext>( "has_start_map", TemplateType.Bool, "whether a start map exists", c => c.StartMap != null );
      project.AddField<MapExportContext>( "start_map_id", TemplateType.Int, "the start map's ID (MAP_START_ID); -1 when none", c => ( c.StartMap != null ) ? c.StartMap.Map.MapID : -1 );
      project.AddField<MapExportContext>( "start_map_index", TemplateType.Int, "the start map's position among the exported maps", c => c.StartMapExportIndex );
      project.AddField<MapExportContext>( "background_color", TemplateType.Int, "the project's background colour", c => c.Project.BackgroundColor );
      project.AddField<MapExportContext>( "multicolor1", TemplateType.Int, "the project's multicolour 1", c => c.Project.MultiColor1 );
      project.AddField<MapExportContext>( "multicolor2", TemplateType.Int, "the project's multicolour 2", c => c.Project.MultiColor2 );
      project.AddField<MapExportContext>( "bgcolor4", TemplateType.Int, "the project's background colour 4", c => c.Project.BGColor4 );
      project.AddField<MapExportContext>( "text_area_width", TemplateType.Int, "the map strings' text area width", c => c.Project.MapStringsTextAreaWidth );
      project.AddField<MapExportContext>( "now", TemplateType.String, "the export time as yyyy-MM-dd HH:mm:ss (the only value that changes between identical exports - use it on purpose)", c => c.Now.ToString( "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture ) );

      // ---- export
      export.AddField<MapExportWriteResult>( "map_directory", TemplateType.String, "the folder the map files went to", w => w.MapDirectory ?? "" );
      export.AddField<MapExportWriteResult>( "charset_directory", TemplateType.String, "the folder the charset files and label sidecars went to", w => w.CharsetDirectory ?? "" );
      export.AddField<MapExportWriteResult>( "compress_directory", TemplateType.String, "the folder the compressed map files went to", w => string.IsNullOrEmpty( w.CompressDirectory ) ? ( w.MapDirectory ?? "" ) : w.CompressDirectory );
      export.AddField<MapExportWriteResult>( "map_file_pattern", TemplateType.String, "the map file name pattern (%d = the map ID)", w => w.MapFilePattern ?? "" );
      export.AddField<MapExportWriteResult>( "charset_file_pattern", TemplateType.String, "the charset file name pattern", w => w.CharsetFilePattern ?? "" );
      export.AddField<MapExportWriteResult>( "compressed_file_pattern", TemplateType.String, "the compressed map file name pattern", w => w.CompressedFilePattern ?? "" );
      export.AddField<MapExportWriteResult>( "charset_compressed_file_pattern", TemplateType.String, "the compressed charset file name pattern", w => w.CharsetCompressedFilePattern ?? "" );
      export.AddField<MapExportWriteResult>( "load_address", TemplateType.Int, "the 2-byte load address in front of the map files; -1 when none", w => w.LoadAddress );
      export.AddField<MapExportWriteResult>( "charset_load_address", TemplateType.Int, "the load address in front of the charset files; -1 when none", w => w.CharsetLoadAddress );
      export.AddField<MapExportWriteResult>( "compress", TemplateType.Bool, "whether compressed map copies are written", w => w.Compress );
      export.AddField<MapExportWriteResult>( "compress_charsets", TemplateType.Bool, "whether compressed charset copies are written", w => w.CompressCharsets );
      export.AddField<MapExportWriteResult>( "compressor", TemplateType.String, "the map compressor", w => w.Compressor ?? "" );
      export.AddField<MapExportWriteResult>( "charset_compressor", TemplateType.String, "the charset compressor", w => w.CharsetCompressor ?? "" );
      export.AddField<MapExportWriteResult>( "markers", TemplateType.Bool, "whether marker records are exported", w => w.ExportMarkers );
      export.AddField<MapExportWriteResult>( "colors", TemplateType.Bool, "whether colour grids are exported", w => w.ExportColors );
      export.AddField<MapExportWriteResult>( "passable", TemplateType.Bool, "whether passable bits are exported", w => w.ExportPassable );
      export.AddField<MapExportWriteResult>( "def_files", TemplateType.Bool, "whether .def sidecars are written", w => w.DefFiles );
      export.AddField<MapExportWriteResult>( "is_preview", TemplateType.Bool, "true in the Preview dialog (nothing was written)", w => w.IsPreview );

      // ---- map
      map.AddField<MapExportMapEntry>( "id", TemplateType.Int, "the map ID: the number in its file names and its MAP_ID_ constant", m => m.Map.MapID );
      map.AddField<MapExportMapEntry>( "index", TemplateType.Int, "position among the exported maps; -1 when not exported", m => m.Index );
      map.AddField<MapExportMapEntry>( "position", TemplateType.Int, "position in the editor's map list", m => m.Position );
      map.AddField<MapExportMapEntry>( "name", TemplateType.String, "the map's name", m => m.Map.Name ?? "" );
      map.AddField<MapExportMapEntry>( "label", TemplateType.String, "the label part of its MAP_ID_ constant in map_names.asm; empty when not exported or the name holds no usable character", m => m.Label );
      map.AddField<MapExportMapEntry>( "has_label", TemplateType.Bool, "whether the map has a MAP_ID_ label", m => m.Label.Length > 0 );
      map.AddField<MapExportMapEntry>( "is_exported", TemplateType.Bool, "false when flagged 'Map not exported'", m => !m.Map.NotExported );
      map.AddField<MapExportMapEntry>( "is_start", TemplateType.Bool, "whether this is the map the game starts on", m => m.IsStart );
      map.AddField<MapExportMapEntry>( "charset", charset, "the map's character set", m => m.Charset );
      map.AddField<MapExportMapEntry>( "charset_index", TemplateType.Int, "the character set's position in the project", m => m.Map.CharsetIndex );
      map.AddField<MapExportMapEntry>( "charset_export_index", TemplateType.Int, "the character set's compacted export position; -1 when it is not export-enabled (a per-map file always stores 0)", m => m.CharsetExportIndex );
      map.AddField<MapExportMapEntry>( "width", TemplateType.Int, "exported width in characters; 0 when not exported", m => m.Width );
      map.AddField<MapExportMapEntry>( "height", TemplateType.Int, "exported height in characters; 0 when not exported", m => m.Height );
      map.AddField<MapExportMapEntry>( "tile_width", TemplateType.Int, "width in tiles", m => m.Map.Tiles.Width );
      map.AddField<MapExportMapEntry>( "tile_height", TemplateType.Int, "height in tiles", m => m.Map.Tiles.Height );
      map.AddField<MapExportMapEntry>( "tile_spacing_x", TemplateType.Int, "characters per tile column", m => m.Map.TileSpacingX );
      map.AddField<MapExportMapEntry>( "tile_spacing_y", TemplateType.Int, "characters per tile row", m => m.Map.TileSpacingY );
      map.AddField<MapExportMapEntry>( "background_color", TemplateType.Int, "the exported background colour (the map's own or the project's)", m => m.BackgroundColor );
      map.AddField<MapExportMapEntry>( "multicolor1", TemplateType.Int, "the exported multicolour 1", m => m.MultiColor1 );
      map.AddField<MapExportMapEntry>( "multicolor2", TemplateType.Int, "the exported multicolour 2", m => m.MultiColor2 );
      map.AddField<MapExportMapEntry>( "markers", s.ListOf( marker ), "the marker records as written: one per cell, sorted by tag; empty without marker export", m => m.Markers );
      map.AddField<MapExportMapEntry>( "marker_count", TemplateType.Int, "number of marker records", m => m.Markers.Count );
      map.AddField<MapExportMapEntry>( "entities", s.ListOf( entity ), "the entity records as written, sorted by tag", m => m.Entities );
      map.AddField<MapExportMapEntry>( "entity_count", TemplateType.Int, "number of entity records", m => m.Entities.Count );
      map.AddField<MapExportMapEntry>( "strings", mapStrings, "this map file's string table in table order", m => m.Strings );
      map.AddField<MapExportMapEntry>( "string_count", TemplateType.Int, "number of strings in this map file", m => m.Strings.Count );
      map.AddField<MapExportMapEntry>( "binary_size", TemplateType.Int, "the map binary's length without the load address; 0 when not exported", m => m.BinarySize );
      map.AddField<MapExportMapEntry>( "file", mapFile, "the files written for this map; nothing when not exported", m => m.File );
      map.AddField<MapExportMapEntry>( "has_file", TemplateType.Bool, "whether files were written for this map", m => m.File != null );
      map.AddField<MapExportMapEntry>( "file_name", TemplateType.String, "the map file's name (MAP_0005.bin); empty when not exported", m => ( m.File != null ) ? m.File.Name : "" );
      map.AddField<MapExportMapEntry>( "charset_file_name", TemplateType.String, "the charset file's name; empty without one", m => ( m.File != null ) ? m.File.CharsetName : "" );
      map.AddField<MapExportMapEntry>( "compressed_file_name", TemplateType.String, "the compressed map file's name; empty without compression", m => ( m.File != null ) ? m.File.CompressedName : "" );
      map.AddField<MapExportMapEntry>( "charset_compressed_file_name", TemplateType.String, "the compressed charset file's name; empty without compression", m => ( m.File != null ) ? m.File.CharsetCompressedName : "" );
      map.AddField<MapExportMapEntry>( "extra_data", TemplateType.String, "the map's extra data text", m => m.Map.ExtraDataText ?? "" );
      map.AddField<MapExportMapEntry>( "has_extra_data", TemplateType.Bool, "whether the map has extra data text", m => !string.IsNullOrEmpty( m.Map.ExtraDataText ) );

      // ---- map_file
      mapFile.AddField<MapExportFileEntry>( "name", TemplateType.String, "the map file's name", f => f.Name );
      mapFile.AddField<MapExportFileEntry>( "base_name", TemplateType.String, "the map file's name without extension", f => f.BaseName );
      mapFile.AddField<MapExportFileEntry>( "extension", TemplateType.String, "the map file's extension, with the dot", f => f.Extension );
      mapFile.AddField<MapExportFileEntry>( "path", TemplateType.String, "the map file's full path", f => f.Path );
      mapFile.AddField<MapExportFileEntry>( "directory", TemplateType.String, "the map file's folder", f => f.Directory );
      mapFile.AddField<MapExportFileEntry>( "size", TemplateType.Int, "bytes written, including the 2-byte load address when enabled", f => f.Size );
      mapFile.AddField<MapExportFileEntry>( "load_address", TemplateType.Int, "the load address in front of the file; -1 when none", f => f.LoadAddress );
      mapFile.AddField<MapExportFileEntry>( "compressed", TemplateType.Bool, "whether a compressed copy was written", f => f.Compressed );
      mapFile.AddField<MapExportFileEntry>( "compressed_name", TemplateType.String, "the compressed copy's name; empty without compression", f => f.CompressedName );
      mapFile.AddField<MapExportFileEntry>( "compressed_base_name", TemplateType.String, "the compressed copy's name without extension", f => f.CompressedBaseName );
      mapFile.AddField<MapExportFileEntry>( "compressed_path", TemplateType.String, "the compressed copy's full path", f => f.CompressedPath );
      mapFile.AddField<MapExportFileEntry>( "compressed_size", TemplateType.Int, "the compressed copy's size; -1 when not produced or in a preview", f => f.CompressedSize );
      mapFile.AddField<MapExportFileEntry>( "has_charset", TemplateType.Bool, "whether a charset file was written for the map", f => f.HasCharset );
      mapFile.AddField<MapExportFileEntry>( "charset_name", TemplateType.String, "the charset file's name", f => f.CharsetName );
      mapFile.AddField<MapExportFileEntry>( "charset_base_name", TemplateType.String, "the charset file's name without extension", f => f.CharsetBaseName );
      mapFile.AddField<MapExportFileEntry>( "charset_path", TemplateType.String, "the charset file's full path", f => f.CharsetPath );
      mapFile.AddField<MapExportFileEntry>( "charset_size", TemplateType.Int, "the charset file's size including its load address; -1 without a charset file", f => f.CharsetSize );
      mapFile.AddField<MapExportFileEntry>( "charset_load_address", TemplateType.Int, "the load address in front of the charset file; -1 when none", f => f.CharsetLoadAddress );
      mapFile.AddField<MapExportFileEntry>( "charset_compressed", TemplateType.Bool, "whether a compressed charset copy was written", f => f.CharsetCompressed );
      mapFile.AddField<MapExportFileEntry>( "charset_compressed_name", TemplateType.String, "the compressed charset copy's name", f => f.CharsetCompressedName );
      mapFile.AddField<MapExportFileEntry>( "charset_compressed_base_name", TemplateType.String, "the compressed charset copy's name without extension", f => f.CharsetCompressedBaseName );
      mapFile.AddField<MapExportFileEntry>( "charset_compressed_path", TemplateType.String, "the compressed charset copy's full path", f => f.CharsetCompressedPath );
      mapFile.AddField<MapExportFileEntry>( "charset_compressed_size", TemplateType.Int, "the compressed charset copy's size; -1 when not produced or in a preview", f => f.CharsetCompressedSize );
      mapFile.AddField<MapExportFileEntry>( "has_def", TemplateType.Bool, "whether a .def sidecar was written beside the map file", f => f.HasDef );
      mapFile.AddField<MapExportFileEntry>( "def_name", TemplateType.String, "the .def sidecar's name", f => f.DefName );
      mapFile.AddField<MapExportFileEntry>( "def_path", TemplateType.String, "the .def sidecar's full path", f => f.DefPath );

      // ---- marker (record)
      marker.AddField<MapExportMarkerEntry>( "index", TemplateType.Int, "position in the map's marker table", r => r.Record.Index );
      marker.AddField<MapExportMarkerEntry>( "offset", TemplateType.Int, "byte offset of the record in the map binary", r => r.Record.Offset );
      marker.AddField<MapExportMarkerEntry>( "tag", TemplateType.Int, "byte 0: the type's tag; 0 when the type is unknown", r => (int)r.Record.Tag );
      marker.AddField<MapExportMarkerEntry>( "type", markerType, "the marker's type; nothing when its type ID matches no marker type", r => r.Type );
      marker.AddField<MapExportMarkerEntry>( "has_type", TemplateType.Bool, "whether the marker's type exists", r => r.Type != null );
      marker.AddField<MapExportMarkerEntry>( "type_id", TemplateType.Int, "the marker's type ID as authored", r => r.Record.Marker.Type );
      marker.AddField<MapExportMarkerEntry>( "symbol", TemplateType.String, "the type's export symbol; empty without", r => ( r.Type != null ) ? ( r.Type.Type.ExportSymbol ?? "" ) : "" );
      marker.AddField<MapExportMarkerEntry>( "name", TemplateType.String, "the marker's name", r => r.Record.Marker.Name ?? "" );
      marker.AddField<MapExportMarkerEntry>( "x", TemplateType.Int, "byte 1: the cell's x", r => r.Record.Marker.X );
      marker.AddField<MapExportMarkerEntry>( "y", TemplateType.Int, "byte 2: the cell's y", r => r.Record.Marker.Y );
      marker.AddField<MapExportMarkerEntry>( "value1", TemplateType.Int, "byte 3", r => (int)r.Record.Marker.Value1 );
      marker.AddField<MapExportMarkerEntry>( "value2", TemplateType.Int, "byte 4", r => (int)r.Record.Marker.Value2 );
      marker.AddField<MapExportMarkerEntry>( "enabled", TemplateType.Bool, "flags bit 0", r => r.Record.Marker.Enabled );
      marker.AddField<MapExportMarkerEntry>( "triggered", TemplateType.Bool, "flags bit 1", r => r.Record.Marker.Triggered );
      marker.AddField<MapExportMarkerEntry>( "auto_disable_group", TemplateType.Bool, "flags bit 2", r => r.Record.Marker.AutoDisableGroupAfterTrigger );
      marker.AddField<MapExportMarkerEntry>( "flags", TemplateType.Int, "byte 5: the flags byte as written", r => (int)r.Record.Flags );
      marker.AddField<MapExportMarkerEntry>( "group_id", TemplateType.Int, "byte 6", r => (int)r.Record.Marker.GroupId );
      marker.AddField<MapExportMarkerEntry>( "link_to_id", TemplateType.Int, "byte 7", r => (int)r.Record.Marker.LinkToID );
      marker.AddField<MapExportMarkerEntry>( "link_id", TemplateType.Int, "byte 8", r => (int)r.Record.Marker.LinkID );
      marker.AddField<MapExportMarkerEntry>( "value3", TemplateType.Int, "byte 9", r => (int)r.Record.Marker.Value3 );
      marker.AddField<MapExportMarkerEntry>( "value4", TemplateType.Int, "byte 10", r => (int)r.Record.Marker.Value4 );

      // ---- entity (record)
      entity.AddField<MapExportEntityEntry>( "index", TemplateType.Int, "byte 0: position in the map's entity table", r => r.Record.Index );
      entity.AddField<MapExportEntityEntry>( "offset", TemplateType.Int, "byte offset of the record in the map binary", r => r.Record.Offset );
      entity.AddField<MapExportEntityEntry>( "tag", TemplateType.Int, "byte 1: the type's tag; 0 when the type is unknown", r => (int)r.Record.Tag );
      entity.AddField<MapExportEntityEntry>( "type", entityType, "the entity's type; nothing when its type ID matches no entity type", r => r.Type );
      entity.AddField<MapExportEntityEntry>( "has_type", TemplateType.Bool, "whether the entity's type exists", r => r.Type != null );
      entity.AddField<MapExportEntityEntry>( "type_id", TemplateType.Int, "the entity's type ID as authored", r => r.Record.Entity.Type );
      entity.AddField<MapExportEntityEntry>( "symbol", TemplateType.String, "the type's export symbol; empty without", r => ( r.Type != null ) ? ( r.Type.Type.ExportSymbol ?? "" ) : "" );
      entity.AddField<MapExportEntityEntry>( "x", TemplateType.Int, "byte 2", r => r.Record.Entity.X );
      entity.AddField<MapExportEntityEntry>( "y", TemplateType.Int, "byte 3", r => r.Record.Entity.Y );
      entity.AddField<MapExportEntityEntry>( "tile", TemplateType.Int, "byte 4: the type's tile in the map's character set as written; 255 when the type has no tile there, 0 when the type is unknown", r => (int)r.Record.Tile );
      entity.AddField<MapExportEntityEntry>( "has_tile", TemplateType.Bool, "whether the type exists and has a tile in the map's character set", r => r.HasTile );
      entity.AddField<MapExportEntityEntry>( "value1", TemplateType.Int, "byte 5", r => (int)r.Record.Entity.Value1 );
      entity.AddField<MapExportEntityEntry>( "value2", TemplateType.Int, "byte 6", r => (int)r.Record.Entity.Value2 );
      entity.AddField<MapExportEntityEntry>( "enabled", TemplateType.Bool, "byte 7", r => r.Record.Entity.Enabled );
      entity.AddField<MapExportEntityEntry>( "triggered", TemplateType.Bool, "byte 8", r => r.Record.Entity.Triggered );

      // ---- map_string
      mapString.AddField<MapExportStringEntry>( "index", TemplateType.Int, "position in the containing list or table", e => e.Index );
      mapString.AddField<MapExportStringEntry>( "offset", TemplateType.Int, "byte offset of the string's stream in the map binary; -1 outside a map file", e => e.Offset );
      mapString.AddField<MapExportStringEntry>( "size", TemplateType.Int, "length of the string's byte stream", e => e.Size );
      mapString.AddField<MapExportStringEntry>( "id", TemplateType.Int, "the string ID (the MAP_STRING_ID table byte)", e => (int)e.String.StringID );
      mapString.AddField<MapExportStringEntry>( "label", TemplateType.String, "the label as typed", e => e.String.Label ?? "" );
      mapString.AddField<MapExportStringEntry>( "has_label", TemplateType.Bool, "whether the string has a label", e => !string.IsNullOrEmpty( e.String.Label ) );
      mapString.AddField<MapExportStringEntry>( "is_valid_label", TemplateType.Bool, "whether the label is a valid assembler identifier", e => ( e.String.Label != null ) && s_LabelPattern.IsMatch( e.String.Label ) );
      mapString.AddField<MapExportStringEntry>( "lines", s.ListOf( stringLine ), "the five lines", e => e.Lines );
      mapString.AddField<MapExportStringEntry>( "line_count", TemplateType.Int, "number of lines with text", e => LineCount( e ) );
      mapString.AddField<MapExportStringEntry>( "text", TemplateType.String, "the lines' texts joined by one space", e => JoinedText( e ) );
      mapString.AddField<MapExportStringEntry>( "clear_at_end", TemplateType.Bool, "whether the text area is cleared at the end", e => e.String.ClearTextAreaAtEnd );
      mapString.AddField<MapExportStringEntry>( "is_exported", TemplateType.Bool, "whether the string is in the project-wide export list", e => e.IsExported );
      mapString.AddField<MapExportStringEntry>( "export_index", TemplateType.Int, "position in the project-wide export list; -1 when not exported", e => e.ExportIndex );

      // ---- map_string_line
      stringLine.AddField<MapExportStringLineEntry>( "index", TemplateType.Int, "the line's number, 0 to 4", l => l.Index );
      stringLine.AddField<MapExportStringLineEntry>( "text", TemplateType.String, "the line's text", l => l.Line.Text ?? "" );
      stringLine.AddField<MapExportStringLineEntry>( "has_text", TemplateType.Bool, "whether the line has text", l => !string.IsNullOrEmpty( l.Line.Text ) );
      stringLine.AddField<MapExportStringLineEntry>( "control_code", TemplateType.Int, "the line's control code byte; 255 = none", l => (int)l.Line.ControlCode );
      stringLine.AddField<MapExportStringLineEntry>( "has_control_code", TemplateType.Bool, "whether the line has a control code", l => l.Line.ControlCode != MapProject.MAP_STRING_NO_CONTROL_CODE );
      stringLine.AddField<MapExportStringLineEntry>( "terminator", TemplateType.Int, "the line's terminator byte; 0 = none", l => (int)l.Line.Terminator );
      stringLine.AddField<MapExportStringLineEntry>( "has_terminator", TemplateType.Bool, "whether the line has a terminator", l => l.Line.Terminator != MapProject.MAP_STRING_NO_TERMINATOR );
      stringLine.AddField<MapExportStringLineEntry>( "justification", TemplateType.Int, "0 = left, 1 = center, 2 = right", l => (int)l.Line.Justification );
      stringLine.AddField<MapExportStringLineEntry>( "justification_name", TemplateType.String, "left, center or right", l => JustificationName( l.Line.Justification ) );

      // ---- charset
      charset.AddField<MapExportCharsetEntry>( "index", TemplateType.Int, "position in the project's character sets (what a map's charset_index holds)", c => c.Index );
      charset.AddField<MapExportCharsetEntry>( "export_index", TemplateType.Int, "compacted position among the export-enabled sets; -1 when not enabled", c => c.ExportIndex );
      charset.AddField<MapExportCharsetEntry>( "is_exported", TemplateType.Bool, "whether the set is export-enabled", c => c.Charset.ExportEnabled );
      charset.AddField<MapExportCharsetEntry>( "name", TemplateType.String, "the display name ('Charset N' when none was given)", c => c.Name );
      charset.AddField<MapExportCharsetEntry>( "has_name", TemplateType.Bool, "whether a display name was given", c => !string.IsNullOrEmpty( c.Charset.DisplayName ) );
      charset.AddField<MapExportCharsetEntry>( "export_name", TemplateType.String, "the export name as typed", c => c.Charset.ExportName ?? "" );
      charset.AddField<MapExportCharsetEntry>( "export_base_name", TemplateType.String, "the export name without extension", c => MapProject.ExportNameWithoutExtension( c.Charset.ExportName ) );
      charset.AddField<MapExportCharsetEntry>( "label", TemplateType.String, "the label part of its CHARSET_INDEX_ constant in map_charsets.asm; empty when not exported or without export name", c => c.Label );
      charset.AddField<MapExportCharsetEntry>( "has_label", TemplateType.Bool, "whether the set has a CHARSET_INDEX_ label", c => c.Label.Length > 0 );
      charset.AddField<MapExportCharsetEntry>( "tiles", s.ListOf( tile ), "the tile library in list order", c => c.Tiles );
      charset.AddField<MapExportCharsetEntry>( "tile_count", TemplateType.Int, "number of tiles", c => c.Tiles.Count );
      charset.AddField<MapExportCharsetEntry>( "entity_tiles", s.ListOf( entityTile ), "one entry per entity type that has a tile in this set, in entity type order", c => c.EntityTiles );
      charset.AddField<MapExportCharsetEntry>( "entity_tile_count", TemplateType.Int, "number of entity types with a tile in this set", c => c.EntityTiles.Count );
      charset.AddField<MapExportCharsetEntry>( "character_count", TemplateType.Int, "number of characters exported", c => c.Charset.Charset.ExportNumCharacters );
      charset.AddField<MapExportCharsetEntry>( "data_size", TemplateType.Int, "bytes of character data (what the charset file holds without its load address)", c => c.DataSize );
      charset.AddField<MapExportCharsetEntry>( "maps", maps, "the maps using this set, in editor order", c => c.Maps );
      charset.AddField<MapExportCharsetEntry>( "map_count", TemplateType.Int, "number of maps using this set", c => c.Maps.Count );
      charset.AddField<MapExportCharsetEntry>( "exported_maps", maps, "the exported maps using this set", c => c.ExportedMaps );
      charset.AddField<MapExportCharsetEntry>( "exported_map_count", TemplateType.Int, "number of exported maps using this set", c => c.ExportedMaps.Count );

      // ---- entity_tile
      entityTile.AddField<MapExportEntityTileEntry>( "type", entityType, "the entity type", t => t.Type );
      entityTile.AddField<MapExportEntityTileEntry>( "type_id", TemplateType.Int, "the entity type's ID", t => t.Type.Type.ID );
      entityTile.AddField<MapExportEntityTileEntry>( "tag", TemplateType.Int, "the entity type's tag", t => t.Type.Type.TagID & 0xFF );
      entityTile.AddField<MapExportEntityTileEntry>( "symbol", TemplateType.String, "the entity type's export symbol", t => t.Type.Type.ExportSymbol ?? "" );
      entityTile.AddField<MapExportEntityTileEntry>( "has_symbol", TemplateType.Bool, "whether the entity type has an export symbol", t => !string.IsNullOrEmpty( t.Type.Type.ExportSymbol ) );
      entityTile.AddField<MapExportEntityTileEntry>( "tile_index", TemplateType.Int, "the tile's position in the set's library", t => t.TileIndex );
      entityTile.AddField<MapExportEntityTileEntry>( "tile", tile, "the tile; nothing when the position is past the library", t => t.Tile );
      entityTile.AddField<MapExportEntityTileEntry>( "has_tile", TemplateType.Bool, "whether the tile position exists in the library", t => t.Tile != null );

      // ---- tile
      tile.AddField<MapExportTileEntry>( "index", TemplateType.Int, "position in the tile library", t => t.Index );
      tile.AddField<MapExportTileEntry>( "name", TemplateType.String, "the tile's name", t => t.Tile.Name ?? "" );
      tile.AddField<MapExportTileEntry>( "label", TemplateType.String, "the name as an upper-case identifier", t => MapProject.NormalizeAsLabel( t.Tile.Name ?? "" ).ToUpper() );
      tile.AddField<MapExportTileEntry>( "passable", TemplateType.Bool, "whether the tile is passable", t => t.Tile.Passable );
      tile.AddField<MapExportTileEntry>( "exported_on_map", TemplateType.Bool, "false when flagged 'not exported on map'", t => !t.Tile.NotExportedOnMap );
      tile.AddField<MapExportTileEntry>( "group_id", TemplateType.Int, "the tile's group", t => t.Tile.GroupId );
      tile.AddField<MapExportTileEntry>( "width", TemplateType.Int, "width in characters", t => t.Tile.Chars.Width );
      tile.AddField<MapExportTileEntry>( "height", TemplateType.Int, "height in characters", t => t.Tile.Chars.Height );
      tile.AddField<MapExportTileEntry>( "chars", s.ListOf( tileChar ), "the tile's characters, row by row", t => t.Chars );
      tile.AddField<MapExportTileEntry>( "char_count", TemplateType.Int, "number of characters", t => t.Chars.Count );

      // ---- tile_char
      tileChar.AddField<MapExportTileCharEntry>( "index", TemplateType.Int, "position, row by row", c => c.Index );
      tileChar.AddField<MapExportTileCharEntry>( "x", TemplateType.Int, "column in the tile", c => c.X );
      tileChar.AddField<MapExportTileCharEntry>( "y", TemplateType.Int, "row in the tile", c => c.Y );
      tileChar.AddField<MapExportTileCharEntry>( "character", TemplateType.Int, "the character code", c => c.Character );
      tileChar.AddField<MapExportTileCharEntry>( "color", TemplateType.Int, "the character's colour", c => c.Color );

      // ---- marker_type
      markerType.AddField<MapExportMarkerTypeEntry>( "index", TemplateType.Int, "position in the marker type list", t => t.Index );
      markerType.AddField<MapExportMarkerTypeEntry>( "id", TemplateType.Int, "the type's ID (what placed markers refer to)", t => t.Type.ID );
      markerType.AddField<MapExportMarkerTypeEntry>( "name", TemplateType.String, "the type's name", t => t.Type.Name ?? "" );
      markerType.AddField<MapExportMarkerTypeEntry>( "tag", TemplateType.Int, "the tag byte written in marker records", t => t.Type.TagID & 0xFF );
      markerType.AddField<MapExportMarkerTypeEntry>( "symbol", TemplateType.String, "the export symbol (MARKER_ constant)", t => t.Type.ExportSymbol ?? "" );
      markerType.AddField<MapExportMarkerTypeEntry>( "has_symbol", TemplateType.Bool, "whether the type has an export symbol", t => !string.IsNullOrEmpty( t.Type.ExportSymbol ) );
      markerType.AddField<MapExportMarkerTypeEntry>( "color", TemplateType.Int, "the editor colour", t => t.Type.Color );
      markerType.AddField<MapExportMarkerTypeEntry>( "description", TemplateType.String, "the editor description", t => t.Type.Description ?? "" );
      markerType.AddField<MapExportMarkerTypeEntry>( "placed_count", TemplateType.Int, "markers of this type on all maps, before expansion", t => t.PlacedCount );

      // ---- entity_type
      entityType.AddField<MapExportEntityTypeEntry>( "index", TemplateType.Int, "position in the entity type list", t => t.Index );
      entityType.AddField<MapExportEntityTypeEntry>( "id", TemplateType.Int, "the type's ID (what placed entities refer to)", t => t.Type.ID );
      entityType.AddField<MapExportEntityTypeEntry>( "name", TemplateType.String, "the type's name", t => t.Type.Name ?? "" );
      entityType.AddField<MapExportEntityTypeEntry>( "tag", TemplateType.Int, "the tag byte written in entity records", t => t.Type.TagID & 0xFF );
      entityType.AddField<MapExportEntityTypeEntry>( "symbol", TemplateType.String, "the export symbol (ENTITY_ constant)", t => t.Type.ExportSymbol ?? "" );
      entityType.AddField<MapExportEntityTypeEntry>( "has_symbol", TemplateType.Bool, "whether the type has an export symbol", t => !string.IsNullOrEmpty( t.Type.ExportSymbol ) );
      entityType.AddField<MapExportEntityTypeEntry>( "placed_count", TemplateType.Int, "entities of this type on all maps", t => t.PlacedCount );

      // ---- filters
      s.DefineFilter( "upper", "upper case", text => text.ToUpperInvariant() );
      s.DefineFilter( "lower", "lower case", text => text.ToLowerInvariant() );
      s.DefineFilter( "label", "as an exported label: diacritics stripped, other characters replaced by _, upper case (what map.label and charset.label use)", text => MapProject.NormalizeAsLabel( text ).ToUpper() );

      var problems = s.Validate();
      if ( problems.Count > 0 )
      {
        throw new InvalidOperationException( "MapExportSchema: " + string.Join( "; ", problems.ToArray() ) );
      }
      return s;
    }



    private static int LineCount( MapExportStringEntry Entry )
    {
      int count = 0;
      foreach ( var line in Entry.Lines )
      {
        if ( !string.IsNullOrEmpty( line.Line.Text ) )
        {
          ++count;
        }
      }
      return count;
    }



    private static string JoinedText( MapExportStringEntry Entry )
    {
      var sb = new StringBuilder();
      foreach ( var line in Entry.Lines )
      {
        if ( string.IsNullOrEmpty( line.Line.Text ) )
        {
          continue;
        }
        if ( sb.Length > 0 )
        {
          sb.Append( ' ' );
        }
        sb.Append( line.Line.Text );
      }
      return sb.ToString();
    }



    private static string JustificationName( byte Justification )
    {
      switch ( Justification )
      {
        case MapProject.MAP_STRING_JUSTIFY_CENTER:
          return "center";
        case MapProject.MAP_STRING_JUSTIFY_RIGHT:
          return "right";
        default:
          return "left";
      }
    }



    // ------------------------------------------------------------ builders

    /// <summary>The context of a real export: the files written and the run's settings.</summary>
    public static MapExportContext BuildContext( MapProject Project, List<MapProject.GameBinaryMapFile> Files, MapExportWriteResult Write, DateTime Now )
    {
      return MapExportContext.Build( Project, Files, Write, Now );
    }



    /// <summary>
    /// A dry run for the Preview dialog: the per-map export is built in
    /// memory from the project's game-binary settings (the form's defaults
    /// applied: an empty map file pattern is MAP_%d.bin, a blank charset
    /// folder is the project's folder) and nothing is written. The planned
    /// write result says IsPreview; compressed sizes are unknown (-1). Null
    /// when the export itself would be refused (Errors says why).
    /// </summary>
    public static MapExportContext BuildPreviewContext( MapProject Project, string DocumentPath, out List<string> Errors )
    {
      var write = PlannedWriteResult( Project, DocumentPath );
      var files = Project.ExportAsGameBinaryFiles( write.MapFilePattern, write.CharsetFilePattern, write.ExportMarkers, write.ExportColors, write.ExportPassable, out Errors );
      if ( files == null )
      {
        return null;
      }
      return MapExportContext.Build( Project, files, write, DateTime.Now );
    }



    /// <summary>The write result a real export would produce, planned from the settings (nothing written).</summary>
    public static MapExportWriteResult PlannedWriteResult( MapProject Project, string DocumentPath )
    {
      var s = Project.Settings.GameBinary;
      var write = new MapExportWriteResult();
      write.IsPreview       = true;
      write.ProjectFilePath = DocumentPath ?? "";
      string documentFolder = "";
      try
      {
        documentFolder = string.IsNullOrEmpty( DocumentPath ) ? "" : ( Path.GetDirectoryName( DocumentPath ) ?? "" );
      }
      catch ( Exception )
      {
      }
      write.MapDirectory     = ( s.SaveOnExport && !string.IsNullOrEmpty( s.ExportDirectory ) ) ? s.ExportDirectory : documentFolder;
      write.CharsetDirectory = !string.IsNullOrEmpty( s.CharsetExportDirectory ) ? s.CharsetExportDirectory
                             : ( ( documentFolder.Length > 0 ) ? documentFolder : write.MapDirectory );
      write.CompressDirectory = string.IsNullOrEmpty( s.CompressDirectory ) ? write.MapDirectory : s.CompressDirectory;
      write.MapFilePattern     = string.IsNullOrEmpty( s.ExportFilename ) ? "MAP_%d.bin" : s.ExportFilename;
      write.CharsetFilePattern = s.CharsetFilenamePattern ?? "";
      write.CompressedFilePattern        = s.CompressFilename ?? "";
      write.CharsetCompressedFilePattern = s.CharsetCompressFilename ?? "";
      write.LoadAddress        = s.PrefixLoadAddress ? GR.Convert.ToU16( s.PrefixLoadAddressHex ?? "", 16 ) : -1;
      write.CharsetLoadAddress = s.CharsetPrefixLoadAddress ? ParseCharsetLoadAddress( s.CharsetPrefixLoadAddressHex ) : -1;
      write.Compress         = s.CompressMap && !string.IsNullOrEmpty( s.CompressFilename );
      write.CompressCharsets = s.CompressCharsets && !string.IsNullOrEmpty( s.CharsetCompressFilename );
      write.Compressor        = s.Compressor ?? "ZX0";
      write.CharsetCompressor = s.CharsetCompressor ?? "ZX0";
      write.ExportMarkers  = s.ExportMarkers;
      write.ExportColors   = s.ExportColors;
      write.ExportPassable = s.ExportPassableBits;
      write.DefFiles       = s.GenerateDefFile;
      return write;
    }



    /// <summary>Exactly four hex digits, like the export form's parser; -1 otherwise.</summary>
    public static int ParseCharsetLoadAddress( string Text )
    {
      string address = ( Text ?? "" ).Trim();
      if ( ( address.Length != 4 )
      ||   ( GR.Convert.ToI32( address, 16 ) < 0 ) )
      {
        return -1;
      }
      return GR.Convert.ToI32( address, 16 );
    }
  }
}
