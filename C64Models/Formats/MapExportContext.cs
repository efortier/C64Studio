using System;
using System.Collections.Generic;
using System.IO;



namespace RetroDevStudio.Formats
{
  /// <summary>
  /// What the form knows about one game-binary export run and hands to the
  /// custom text exports: the settings that shaped it and, per exported map,
  /// the files it wrote. For the Preview dialog the same object is PLANNED
  /// from the settings (IsPreview = true): nothing was written, so sizes
  /// that only a real run knows (compressed copies) stay -1.
  /// </summary>
  public class MapExportWriteResult
  {
    public bool     IsPreview = false;
    /// <summary>The .mapproject's path; "" when the project was never saved.</summary>
    public string   ProjectFilePath = "";
    public string   MapDirectory = "";
    public string   CharsetDirectory = "";
    public string   CompressDirectory = "";
    public string   MapFilePattern = "";
    public string   CharsetFilePattern = "";
    public string   CompressedFilePattern = "";
    public string   CharsetCompressedFilePattern = "";
    /// <summary>The 2-byte PRG load address written in front of the map files, -1 when none.</summary>
    public int      LoadAddress = -1;
    public int      CharsetLoadAddress = -1;
    public bool     Compress = false;
    public bool     CompressCharsets = false;
    public string   Compressor = "ZX0";
    public string   CharsetCompressor = "ZX0";
    public bool     ExportMarkers = true;
    public bool     ExportColors = true;
    public bool     ExportPassable = true;
    public bool     DefFiles = true;
    /// <summary>Per exported map (by map ID) what was actually written; empty in a preview.</summary>
    public Dictionary<int, MapExportFileResult> Files = new Dictionary<int, MapExportFileResult>();
  }



  /// <summary>The files one map's export wrote; "" / -1 = not produced.</summary>
  public class MapExportFileResult
  {
    public string   MapPath = "";
    /// <summary>Bytes written, including the 2-byte load address when enabled.</summary>
    public int      MapSize = -1;
    public string   CompressedPath = "";
    public int      CompressedSize = -1;
    public string   CharsetPath = "";
    public int      CharsetSize = -1;
    public string   CharsetCompressedPath = "";
    public int      CharsetCompressedSize = -1;
    public string   DefPath = "";
  }



  /// <summary>One map as the templates see it.</summary>
  public class MapExportMapEntry
  {
    public MapProject.Map                 Map = null;
    /// <summary>Position in the editor's map list.</summary>
    public int                            Position = 0;
    /// <summary>Position among the exported maps; -1 when not exported.</summary>
    public int                            Index = -1;
    /// <summary>The MAP_ID_ label part; "" when not exported or the name normalizes to nothing.</summary>
    public string                         Label = "";
    public bool                           IsStart = false;
    public MapExportCharsetEntry          Charset = null;
    public int                            CharsetExportIndex = -1;
    public int                            Width = 0;
    public int                            Height = 0;
    public int                            BackgroundColor = 0;
    public int                            MultiColor1 = 0;
    public int                            MultiColor2 = 0;
    public int                            BinarySize = 0;
    /// <summary>The written files; null when the map is not exported.</summary>
    public MapExportFileEntry             File = null;
    public List<MapExportMarkerEntry>     Markers = new List<MapExportMarkerEntry>();
    public List<MapExportEntityEntry>     Entities = new List<MapExportEntityEntry>();
    /// <summary>This map file's string table in table order.</summary>
    public List<MapExportStringEntry>     Strings = new List<MapExportStringEntry>();
  }



  /// <summary>The files written for one map (or planned for it in a preview).</summary>
  public class MapExportFileEntry
  {
    public string   Name = "";
    public string   BaseName = "";
    public string   Extension = "";
    public string   Path = "";
    public string   Directory = "";
    public int      Size = -1;
    public int      LoadAddress = -1;
    public bool     Compressed = false;
    public string   CompressedName = "";
    public string   CompressedBaseName = "";
    public string   CompressedPath = "";
    public int      CompressedSize = -1;
    public bool     HasCharset = false;
    public string   CharsetName = "";
    public string   CharsetBaseName = "";
    public string   CharsetPath = "";
    public int      CharsetSize = -1;
    public int      CharsetLoadAddress = -1;
    public bool     CharsetCompressed = false;
    public string   CharsetCompressedName = "";
    public string   CharsetCompressedBaseName = "";
    public string   CharsetCompressedPath = "";
    public int      CharsetCompressedSize = -1;
    public bool     HasDef = false;
    public string   DefName = "";
    public string   DefPath = "";
  }



  public class MapExportMarkerEntry
  {
    public GameBinaryMarkerRecord         Record = null;
    /// <summary>The first marker type with the marker's type ID (as the writer resolves it); null when none.</summary>
    public MapExportMarkerTypeEntry       Type = null;
  }



  public class MapExportEntityEntry
  {
    public GameBinaryEntityRecord         Record = null;
    public MapExportEntityTypeEntry       Type = null;
    /// <summary>The type exists and the map's charset has a tile for it.</summary>
    public bool                           HasTile = false;
  }



  public class MapExportStringEntry
  {
    public MapProject.MapString           String = null;
    /// <summary>Position in the containing list / table.</summary>
    public int                            Index = 0;
    /// <summary>Offset of the byte stream in the map binary; -1 outside a map file.</summary>
    public int                            Offset = -1;
    public int                            Size = 0;
    public bool                           IsExported = false;
    /// <summary>Position in the project-wide export list; -1 when not exported.</summary>
    public int                            ExportIndex = -1;
    public List<MapExportStringLineEntry> Lines = new List<MapExportStringLineEntry>();
  }



  public class MapExportStringLineEntry
  {
    public MapProject.MapStringLine       Line = null;
    public int                            Index = 0;
  }



  public class MapExportCharsetEntry
  {
    public MapProject.MapCharset          Charset = null;
    public int                            Index = 0;
    /// <summary>Compacted position among the export-enabled charsets; -1 when not enabled.</summary>
    public int                            ExportIndex = -1;
    public string                         Name = "";
    public string                         Label = "";
    public int                            DataSize = 0;
    public List<MapExportTileEntry>       Tiles = new List<MapExportTileEntry>();
    public List<MapExportEntityTileEntry> EntityTiles = new List<MapExportEntityTileEntry>();
    public List<MapExportMapEntry>        Maps = new List<MapExportMapEntry>();
    public List<MapExportMapEntry>        ExportedMaps = new List<MapExportMapEntry>();
  }



  public class MapExportEntityTileEntry
  {
    public MapExportEntityTypeEntry       Type = null;
    public int                            TileIndex = 0;
    /// <summary>Null when the index is past the charset's tile library.</summary>
    public MapExportTileEntry             Tile = null;
  }



  public class MapExportTileEntry
  {
    public MapProject.Tile                Tile = null;
    public int                            Index = 0;
    public List<MapExportTileCharEntry>   Chars = new List<MapExportTileCharEntry>();
  }



  public class MapExportTileCharEntry
  {
    public int                            Index = 0;
    public int                            X = 0;
    public int                            Y = 0;
    public int                            Character = 0;
    public int                            Color = 0;
  }



  public class MapExportMarkerTypeEntry
  {
    public MapProject.MarkerType          Type = null;
    public int                            Index = 0;
    public int                            PlacedCount = 0;
  }



  public class MapExportEntityTypeEntry
  {
    public MapProject.EntityType          Type = null;
    public int                            Index = 0;
    public int                            PlacedCount = 0;
  }



  /// <summary>
  /// The root object a custom text export renders against: the project's
  /// maps, charsets, types and strings as exported, with the file results of
  /// the run. Built once per export (or preview) by Build; every template
  /// field is a plain read of these entries.
  /// </summary>
  public class MapExportContext
  {
    public MapProject                         Project = null;
    public MapExportWriteResult               Write = null;
    public DateTime                           Now = DateTime.MinValue;
    public string                             ProjectName = "";
    public string                             ProjectFileName = "";
    public string                             ProjectFilePath = "";
    public string                             ProjectDirectory = "";
    public List<MapExportMapEntry>            Maps = new List<MapExportMapEntry>();
    public List<MapExportMapEntry>            ExportedMaps = new List<MapExportMapEntry>();
    public List<MapExportCharsetEntry>        Charsets = new List<MapExportCharsetEntry>();
    public List<MapExportCharsetEntry>        ExportedCharsets = new List<MapExportCharsetEntry>();
    public List<MapExportMarkerTypeEntry>     MarkerTypes = new List<MapExportMarkerTypeEntry>();
    public List<MapExportEntityTypeEntry>     EntityTypes = new List<MapExportEntityTypeEntry>();
    public List<MapExportStringEntry>         MapStrings = new List<MapExportStringEntry>();
    public List<MapExportStringEntry>         ExportedMapStrings = new List<MapExportStringEntry>();
    public MapExportMapEntry                  StartMap = null;
    public int                                StartMapExportIndex = 0;



    /// <summary>
    /// Builds the context from the files ExportAsGameBinaryFiles returned
    /// (their Layout holds the reindexed data) and the run's write result.
    /// Files may be null or empty (nothing exported); Write may be null.
    /// </summary>
    public static MapExportContext Build( MapProject Project, List<MapProject.GameBinaryMapFile> Files, MapExportWriteResult Write, DateTime Now )
    {
      var context = new MapExportContext();
      context.Project = Project;
      context.Write   = Write ?? new MapExportWriteResult();
      context.Now     = Now;
      context.BuildProjectNames();
      context.BuildTypes();
      context.BuildCharsets();
      context.BuildStrings();
      context.BuildMaps( Files ?? new List<MapProject.GameBinaryMapFile>() );
      return context;
    }



    private void BuildProjectNames()
    {
      string path = Write.ProjectFilePath ?? "";
      ProjectFilePath = path;
      if ( path.Length == 0 )
      {
        return;
      }
      try
      {
        ProjectFileName  = Path.GetFileName( path );
        ProjectName      = Path.GetFileNameWithoutExtension( path );
        ProjectDirectory = Path.GetDirectoryName( path ) ?? "";
      }
      catch ( Exception )
      {
        ProjectFileName = path;
        ProjectName     = path;
      }
    }



    private void BuildTypes()
    {
      for ( int i = 0; i < Project.MarkerTypes.Count; ++i )
      {
        MarkerTypes.Add( new MapExportMarkerTypeEntry() { Type = Project.MarkerTypes[i], Index = i } );
      }
      for ( int i = 0; i < Project.EntityTypes.Count; ++i )
      {
        EntityTypes.Add( new MapExportEntityTypeEntry() { Type = Project.EntityTypes[i], Index = i } );
      }
      foreach ( var map in Project.Maps )
      {
        foreach ( var marker in map.Markers )
        {
          var type = MarkerTypeOf( marker.Type );
          if ( type != null )
          {
            ++type.PlacedCount;
          }
        }
        foreach ( var entity in map.Entities )
        {
          var type = EntityTypeOf( entity.Type );
          if ( type != null )
          {
            ++type.PlacedCount;
          }
        }
      }
    }



    /// <summary>The first marker type with this ID - exactly how the writer resolves a marker's tag.</summary>
    public MapExportMarkerTypeEntry MarkerTypeOf( int TypeID )
    {
      foreach ( var entry in MarkerTypes )
      {
        if ( entry.Type.ID == TypeID )
        {
          return entry;
        }
      }
      return null;
    }



    public MapExportEntityTypeEntry EntityTypeOf( int TypeID )
    {
      foreach ( var entry in EntityTypes )
      {
        if ( entry.Type.ID == TypeID )
        {
          return entry;
        }
      }
      return null;
    }



    private void BuildCharsets()
    {
      var labels = Project.CharsetIndexLabels();
      for ( int i = 0; i < Project.Charsets.Count; ++i )
      {
        var charset = Project.Charsets[i];
        var entry = new MapExportCharsetEntry();
        entry.Charset     = charset;
        entry.Index       = i;
        entry.ExportIndex = Project.ExportCharsetIndex( i );
        entry.Name        = Project.CharsetDisplayNameOf( charset );
        string label;
        entry.Label       = labels.TryGetValue( charset, out label ) ? label : "";
        var data = charset.Charset.CharacterData();
        entry.DataSize    = ( data != null ) ? (int)data.Length : 0;
        for ( int t = 0; t < charset.Tiles.Count; ++t )
        {
          var tile = charset.Tiles[t];
          var tileEntry = new MapExportTileEntry() { Tile = tile, Index = t };
          int c = 0;
          for ( int y = 0; y < tile.Chars.Height; ++y )
          {
            for ( int x = 0; x < tile.Chars.Width; ++x )
            {
              tileEntry.Chars.Add( new MapExportTileCharEntry() { Index = c++, X = x, Y = y, Character = tile.Chars[x, y].Character, Color = tile.Chars[x, y].Color } );
            }
          }
          entry.Tiles.Add( tileEntry );
        }
        foreach ( var type in EntityTypes )
        {
          int tileIndex = charset.EntityTileOf( type.Type );
          if ( tileIndex < 0 )
          {
            continue;
          }
          entry.EntityTiles.Add( new MapExportEntityTileEntry()
          {
            Type      = type,
            TileIndex = tileIndex,
            Tile      = ( tileIndex < entry.Tiles.Count ) ? entry.Tiles[tileIndex] : null
          } );
        }
        Charsets.Add( entry );
        if ( charset.ExportEnabled )
        {
          ExportedCharsets.Add( entry );
        }
      }
    }



    private void BuildStrings()
    {
      var exported = Project.GetMapStringsForExport();
      var exportIndex = new Dictionary<MapProject.MapString, int>();
      for ( int i = 0; i < exported.Count; ++i )
      {
        exportIndex[exported[i]] = i;
      }
      for ( int i = 0; i < Project.MapStrings.Count; ++i )
      {
        MapStrings.Add( MakeStringEntry( Project.MapStrings[i], i, -1, exportIndex ) );
      }
      for ( int i = 0; i < exported.Count; ++i )
      {
        ExportedMapStrings.Add( MakeStringEntry( exported[i], i, -1, exportIndex ) );
      }
    }



    private MapExportStringEntry MakeStringEntry( MapProject.MapString String, int Index, int Offset, Dictionary<MapProject.MapString, int> ExportIndex )
    {
      var entry = new MapExportStringEntry() { String = String, Index = Index, Offset = Offset };
      int index;
      if ( ExportIndex.TryGetValue( String, out index ) )
      {
        entry.IsExported  = true;
        entry.ExportIndex = index;
      }
      var stream = MapProject.BuildMapStringByteStream( String, Project.MapStringsLowercaseIndex, Project.MapStringsUppercaseIndex,
                                                         Project.MapStringsNumbersIndex, Project.MapStringsTextAreaWidth );
      entry.Size = ( stream != null ) ? (int)stream.Length : 0;
      for ( int i = 0; i < String.Lines.Length; ++i )
      {
        entry.Lines.Add( new MapExportStringLineEntry() { Line = String.Lines[i] ?? new MapProject.MapStringLine(), Index = i } );
      }
      return entry;
    }



    private void BuildMaps( List<MapProject.GameBinaryMapFile> Files )
    {
      var labels = Project.MapIdLabels();
      var byMap = new Dictionary<MapProject.Map, MapExportMapEntry>();
      for ( int i = 0; i < Project.Maps.Count; ++i )
      {
        var map = Project.Maps[i];
        var entry = new MapExportMapEntry() { Map = map, Position = i };
        string label;
        entry.Label = labels.TryGetValue( map, out label ) ? label : "";
        int charsetIndex = Math.Max( 0, Math.Min( Charsets.Count - 1, map.CharsetIndex ) );
        entry.Charset = ( Charsets.Count > 0 ) ? Charsets[charsetIndex] : null;
        entry.CharsetExportIndex = Project.ExportedCharsetIndex( map );
        // The effective colours; an exported map takes the bytes its file holds below.
        entry.BackgroundColor = ( map.AlternativeBackgroundColor >= 0 ) ? map.AlternativeBackgroundColor : Project.BackgroundColor;
        entry.MultiColor1     = ( map.AlternativeMultiColor1 >= 0 ) ? map.AlternativeMultiColor1 : Project.MultiColor1;
        entry.MultiColor2     = ( map.AlternativeMultiColor2 >= 0 ) ? map.AlternativeMultiColor2 : Project.MultiColor2;
        Maps.Add( entry );
        byMap[map] = entry;
        if ( entry.Charset != null )
        {
          entry.Charset.Maps.Add( entry );
          if ( !map.NotExported )
          {
            entry.Charset.ExportedMaps.Add( entry );
          }
        }
      }
      var exportedMaps = Project.ExportedMaps();
      for ( int i = 0; i < exportedMaps.Count; ++i )
      {
        MapExportMapEntry entry;
        if ( byMap.TryGetValue( exportedMaps[i], out entry ) )
        {
          entry.Index = i;
          ExportedMaps.Add( entry );
        }
      }
      var start = Project.StartMapForExport();
      if ( ( start != null )
      &&   byMap.ContainsKey( start ) )
      {
        StartMap = byMap[start];
        StartMap.IsStart = true;
      }
      StartMapExportIndex = Project.StartMapExportIndex();

      var exportIndex = new Dictionary<MapProject.MapString, int>();
      for ( int i = 0; i < ExportedMapStrings.Count; ++i )
      {
        exportIndex[ExportedMapStrings[i].String] = i;
      }
      foreach ( var file in Files )
      {
        MapExportMapEntry entry;
        if ( ( file.Map == null )
        ||   ( !byMap.TryGetValue( file.Map, out entry ) ) )
        {
          continue;
        }
        entry.BinarySize = ( file.Data != null ) ? (int)file.Data.Length : 0;
        entry.File = BuildFileEntry( file );
        var layout = ( ( file.Layout != null ) && ( file.Layout.Maps.Count > 0 ) ) ? file.Layout.Maps[0] : null;
        if ( layout == null )
        {
          continue;
        }
        entry.Width           = layout.Width;
        entry.Height          = layout.Height;
        entry.BackgroundColor = layout.BackgroundColor;
        entry.MultiColor1     = layout.MultiColor1;
        entry.MultiColor2     = layout.MultiColor2;
        foreach ( var record in layout.Markers )
        {
          entry.Markers.Add( new MapExportMarkerEntry() { Record = record, Type = MarkerTypeOf( record.Marker.Type ) } );
        }
        foreach ( var record in layout.Entities )
        {
          var type = EntityTypeOf( record.Entity.Type );
          entry.Entities.Add( new MapExportEntityEntry()
          {
            Record  = record,
            Type    = type,
            HasTile = ( type != null ) && ( entry.Charset != null ) && ( entry.Charset.Charset.EntityTileOf( type.Type ) >= 0 )
          } );
        }
        foreach ( var record in file.Layout.Strings )
        {
          entry.Strings.Add( MakeStringEntry( record.String, record.Index, record.Offset, exportIndex ) );
        }
      }
    }



    private MapExportFileEntry BuildFileEntry( MapProject.GameBinaryMapFile File )
    {
      MapExportFileResult result;
      if ( !Write.Files.TryGetValue( File.MapID, out result ) )
      {
        result = null;
      }
      var entry = new MapExportFileEntry();
      entry.Name      = File.FileName ?? "";
      entry.BaseName  = BaseNameOf( entry.Name );
      entry.Extension = ExtensionOf( entry.Name );
      entry.Path      = Pick( ( result != null ) ? result.MapPath : null, Combine( Write.MapDirectory, entry.Name ) );
      entry.Directory = DirectoryOf( entry.Path );
      entry.LoadAddress = Write.LoadAddress;
      int prefix = ( Write.LoadAddress >= 0 ) ? 2 : 0;
      entry.Size      = PickSize( ( result != null ) ? result.MapSize : -1, ( File.Data != null ) ? (int)File.Data.Length + prefix : -1 );
      entry.Compressed = Write.Compress;
      if ( Write.Compress )
      {
        entry.CompressedName     = Formatted( Write.CompressedFilePattern, File.MapID );
        entry.CompressedBaseName = BaseNameOf( entry.CompressedName );
        string compressDir = string.IsNullOrEmpty( Write.CompressDirectory ) ? Write.MapDirectory : Write.CompressDirectory;
        entry.CompressedPath     = Pick( ( result != null ) ? result.CompressedPath : null, Combine( compressDir, entry.CompressedName ) );
        entry.CompressedSize     = ( result != null ) ? result.CompressedSize : -1;
        if ( ( result != null )
        &&   ( !Write.IsPreview )
        &&   ( string.IsNullOrEmpty( result.CompressedPath ) ) )
        {
          entry.Compressed = false;
        }
      }
      entry.HasCharset = ( File.CharsetData != null );
      if ( entry.HasCharset )
      {
        entry.CharsetName        = File.CharsetFileName ?? "";
        entry.CharsetBaseName    = BaseNameOf( entry.CharsetName );
        entry.CharsetPath        = Pick( ( result != null ) ? result.CharsetPath : null, Combine( Write.CharsetDirectory, entry.CharsetName ) );
        entry.CharsetLoadAddress = Write.CharsetLoadAddress;
        int charsetPrefix = ( Write.CharsetLoadAddress >= 0 ) ? 2 : 0;
        entry.CharsetSize        = PickSize( ( result != null ) ? result.CharsetSize : -1, (int)File.CharsetData.Length + charsetPrefix );
        entry.CharsetCompressed  = Write.CompressCharsets;
        if ( Write.CompressCharsets )
        {
          entry.CharsetCompressedName     = Formatted( Write.CharsetCompressedFilePattern, File.MapID );
          entry.CharsetCompressedBaseName = BaseNameOf( entry.CharsetCompressedName );
          entry.CharsetCompressedPath     = Pick( ( result != null ) ? result.CharsetCompressedPath : null, Combine( Write.CharsetDirectory, entry.CharsetCompressedName ) );
          entry.CharsetCompressedSize     = ( result != null ) ? result.CharsetCompressedSize : -1;
          if ( ( result != null )
          &&   ( !Write.IsPreview )
          &&   ( string.IsNullOrEmpty( result.CharsetCompressedPath ) ) )
          {
            entry.CharsetCompressed = false;
          }
        }
      }
      entry.HasDef = Write.DefFiles;
      if ( Write.DefFiles )
      {
        entry.DefName = ChangeExtension( entry.Name, ".def" );
        entry.DefPath = Pick( ( result != null ) ? result.DefPath : null, ChangeExtension( entry.Path, ".def" ) );
      }
      return entry;
    }



    private static string Pick( string Actual, string Planned )
    {
      return string.IsNullOrEmpty( Actual ) ? ( Planned ?? "" ) : Actual;
    }



    private static int PickSize( int Actual, int Planned )
    {
      return ( Actual >= 0 ) ? Actual : Planned;
    }



    private static string Formatted( string Pattern, int Number )
    {
      string name;
      string error;
      if ( ( !string.IsNullOrEmpty( Pattern ) )
      &&   MapProject.TryFormatFileNamePattern( Pattern, Number, out name, out error ) )
      {
        return name;
      }
      return "";
    }



    private static string Combine( string Directory, string Name )
    {
      if ( string.IsNullOrEmpty( Name ) )
      {
        return "";
      }
      try
      {
        return string.IsNullOrEmpty( Directory ) ? Name : Path.Combine( Directory, Name );
      }
      catch ( Exception )
      {
        return Name;
      }
    }



    private static string BaseNameOf( string Name )
    {
      try
      {
        return string.IsNullOrEmpty( Name ) ? "" : Path.GetFileNameWithoutExtension( Name );
      }
      catch ( Exception )
      {
        return Name;
      }
    }



    private static string ExtensionOf( string Name )
    {
      try
      {
        return string.IsNullOrEmpty( Name ) ? "" : Path.GetExtension( Name );
      }
      catch ( Exception )
      {
        return "";
      }
    }



    private static string DirectoryOf( string FullPath )
    {
      try
      {
        return string.IsNullOrEmpty( FullPath ) ? "" : ( Path.GetDirectoryName( FullPath ) ?? "" );
      }
      catch ( Exception )
      {
        return "";
      }
    }



    private static string ChangeExtension( string FileName, string Extension )
    {
      try
      {
        return string.IsNullOrEmpty( FileName ) ? "" : Path.ChangeExtension( FileName, Extension );
      }
      catch ( Exception )
      {
        return "";
      }
    }
  }
}
