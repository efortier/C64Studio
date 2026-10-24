using System.Collections.Generic;



namespace RetroDevStudio.Formats
{
  /// <summary>
  /// What BuildGameBinary wrote into one game binary, recorded as it writes:
  /// the per-map tables, every marker/entity record in the order and with the
  /// values the file holds, and the string table. Filled only when a caller
  /// hands one in (the custom text exports read the exported data from it),
  /// never recomputed - the records ARE the sorted lists the writer walked,
  /// so a template can never disagree with the bytes. Offsets are
  /// file-relative, without the optional 2-byte load-address prefix.
  /// </summary>
  public class GameBinaryLayout
  {
    /// <summary>Length of the binary in bytes (no load-address prefix).</summary>
    public int                            Length = 0;
    public int                            MarkerStride = 11;
    public int                            EntityStride = 9;
    /// <summary>Tile count of compacted charset 0 (header byte $01).</summary>
    public int                            TileCount = 0;
    /// <summary>Offset of the charset directory (header word $3F).</summary>
    public int                            CharsetDirectoryOffset = 0;
    /// <summary>One entry per map in the file, in file order.</summary>
    public List<GameBinaryMapLayout>      Maps = new List<GameBinaryMapLayout>();
    /// <summary>The file's string table in table order (per-map files hold only their own map's strings).</summary>
    public List<GameBinaryStringRecord>   Strings = new List<GameBinaryStringRecord>();
  }



  /// <summary>One map's part of a game binary (see GameBinaryLayout).</summary>
  public class GameBinaryMapLayout
  {
    public MapProject.Map                 Map = null;
    /// <summary>Position in the file's map tables (0 in a per-map file).</summary>
    public int                            ExportIndex = 0;
    /// <summary>Char-level size as exported (tiles x spacing, grown by overhanging tiles).</summary>
    public int                            Width = 0;
    public int                            Height = 0;
    /// <summary>The colour bytes written to map_bg_color[] / map_mc1_color[] / map_mc2_color[].</summary>
    public int                            BackgroundColor = 0;
    public int                            MultiColor1 = 0;
    public int                            MultiColor2 = 0;
    /// <summary>The byte written to map_charset_index[] (the compacted charset index, 0 in a per-map file).</summary>
    public int                            CharsetIndex = 0;
    /// <summary>Offsets of the map's sections; -1 when the section was not written.</summary>
    public int                            CharGridOffset = -1;
    public int                            ColorGridOffset = -1;
    public int                            PassableOffset = -1;
    public int                            MarkersOffset = -1;
    public int                            EntitiesOffset = -1;
    /// <summary>Marker records as written: expanded to 1x1 cells and sorted by tag.</summary>
    public List<GameBinaryMarkerRecord>   Markers = new List<GameBinaryMarkerRecord>();
    /// <summary>Entity records as written: sorted by tag, Index = byte 0 of the record.</summary>
    public List<GameBinaryEntityRecord>   Entities = new List<GameBinaryEntityRecord>();
  }



  /// <summary>One 11-byte marker record of a game binary.</summary>
  public class GameBinaryMarkerRecord
  {
    /// <summary>Position in the map's marker table (0-based).</summary>
    public int                            Index = 0;
    public int                            Offset = 0;
    /// <summary>Byte 0: the type's TagID; 0 when the type is unknown.</summary>
    public byte                           Tag = 0;
    /// <summary>Byte 5: bit 0 Enabled, bit 1 Triggered, bit 2 AutoDisableGroupAfterTrigger.</summary>
    public byte                           Flags = 0;
    /// <summary>The expanded 1x1 marker the record was written from (x, y, values, links as written).</summary>
    public MapProject.Marker              Marker = null;
  }



  /// <summary>One 9-byte entity record of a game binary.</summary>
  public class GameBinaryEntityRecord
  {
    /// <summary>Byte 0: post-sort position in the map's entity table.</summary>
    public int                            Index = 0;
    public int                            Offset = 0;
    /// <summary>Byte 1: the type's TagID; 0 when the type is unknown.</summary>
    public byte                           Tag = 0;
    /// <summary>Byte 4: the type's tile in the map's charset as written ($FF = the type has no tile there, 0 = unknown type).</summary>
    public byte                           Tile = 0;
    public MapProject.Entity              Entity = null;
  }



  /// <summary>One entry of a game binary's string table.</summary>
  public class GameBinaryStringRecord
  {
    /// <summary>Position in the table (the MAP_STRING_LO/HI/ID index).</summary>
    public int                            Index = 0;
    /// <summary>Offset of the byte stream.</summary>
    public int                            Offset = 0;
    /// <summary>Length of the byte stream.</summary>
    public int                            Size = 0;
    public MapProject.MapString           String = null;
  }
}
