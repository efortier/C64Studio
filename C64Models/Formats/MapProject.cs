using GR.Collections;
using RetroDevStudio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;



namespace RetroDevStudio.Formats
{
  public class MapProject
  {
    public class TileChar
    {
      public byte       Character = 0;
      public byte       Color = 1;
    };

    public class Tile
    {
      public GR.Game.Layer<TileChar> Chars = new GR.Game.Layer<TileChar>();
      public string       Name = "";
      public int          Index = 0;

      public Tile()
      {
        Chars.InvalidTile = new TileChar();
      }
    };

    public class Map
    {
      public GR.Game.Layer<int> Tiles = new GR.Game.Layer<int>();
      public string             Name = "";
      public int                TileSpacingX = 2;
      public int                TileSpacingY = 2;
      public GR.Memory.ByteBuffer   ExtraDataOld = new GR.Memory.ByteBuffer();
      public string             ExtraDataText = "";
      public int                AlternativeMultiColor1 = -1;
      public int                AlternativeMultiColor2 = -1;
      public int                AlternativeBackgroundColor = -1;
      public int                AlternativeBGColor4 = -1;

      /// <summary>
      /// overrides Project.Mode when set (e.g. display MC instead of hires)
      /// </summary>
      public TextCharMode       AlternativeMode = TextCharMode.UNKNOWN;
    };

    public class ExportSettings
    {
      public class AssemblySettings
      {
        public bool   PrefixWith = true;
        public string Prefix = "!byte ";
        public bool   WrapAt = true;
        public int    WrapByteCount = 8;
        public bool   ExportHex = true;
        public bool   VariableNameLabelPrefixEnabled = false;
        public string VariableNameLabelPrefix = "";
        public bool   IncludeSemicolonAfterSimpleLabels = false;
        public bool   MapSizeCommentEnabled = true;
        public string CommentChars = ";";
        public bool   EmptyTileCompressionEnabled = false;
        public int    EmptyTileIndex = 0;
      }

      public class BinarySettings
      {
        public bool   PrefixLoadAddress = false;
        public string PrefixLoadAddressHex = "";
      }

      public class TargetSettings
      {
        public string TargetFilename = "";
      }

      public int    ExportDataIndex = 0;
      public int    ExportOrientationIndex = 0;
      public int    ExportMethodIndex = 0;

      public AssemblySettings  Assembly = new AssemblySettings();
      public BinarySettings    Binary = new BinarySettings();
      public BinarySettings    CharsetBinary = new BinarySettings();
      public TargetSettings    CharsetProject = new TargetSettings();
      public TargetSettings    Charscreen = new TargetSettings();
    };


    public List<Tile>                   Tiles = new List<Tile>();

    public List<Map>                    Maps = new List<Map>();

    public string                       ExternalCharset = "";
    public int                          BackgroundColor = 0;
    public int                          MultiColor1 = 0;
    public int                          MultiColor2 = 0;
    public int                          BGColor4 = 0;

    /// <summary>
    /// This mode is used to display/build the tiles
    /// </summary>
    public TextMode                     Mode = TextMode.COMMODORE_40_X_25_HIRES;
    public CharsetProject               Charset = new Formats.CharsetProject();
    public bool                         ShowGrid = false;
    public ExportSettings               Settings = new ExportSettings();



    public MapProject()
    {
      for ( int i = 0; i < 256; ++i )
      {
        for ( int j = 0; j < 8; ++j )
        {
          Charset.Characters[i].Tile.CustomColor = 1;
          Charset.Characters[i].Tile.Data.SetU8At( j, ConstantData.UpperCaseCharsetC64.ByteAt( i * 8 + j ) );
        }
      }
    }



    public void Clear()
    {
      Tiles.Clear();
      Maps.Clear();
      ExternalCharset = "";
      Settings = new ExportSettings();
    }



    public GR.Memory.ByteBuffer SaveToBuffer()
    {
      GR.Memory.ByteBuffer projectFile = new GR.Memory.ByteBuffer();

      GR.IO.FileChunk chunkProjectInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_INFO );
      // version
      chunkProjectInfo.AppendU32( 0 );
      chunkProjectInfo.AppendString( ExternalCharset );
      chunkProjectInfo.AppendI32( ShowGrid ? 1 : 0 );
      projectFile.Append( chunkProjectInfo.ToBuffer() );

      GR.IO.FileChunk chunkCharset = new GR.IO.FileChunk( FileChunkConstants.MAP_CHARSET );
      chunkCharset.Append( Charset.SaveToBuffer() );
      projectFile.Append( chunkCharset.ToBuffer() );

      GR.IO.FileChunk chunkProjectData = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_DATA );

      GR.IO.FileChunk chunkMCData = new GR.IO.FileChunk( FileChunkConstants.MULTICOLOR_DATA );
      chunkMCData.AppendU8( (byte)Mode );
      chunkMCData.AppendU8( (byte)BackgroundColor );
      chunkMCData.AppendU8( (byte)MultiColor1 );
      chunkMCData.AppendU8( (byte)MultiColor2 );
      chunkMCData.AppendU8( (byte)BGColor4 );
      chunkProjectData.Append( chunkMCData.ToBuffer() );

      foreach ( Tile tile in Tiles )
      {
        GR.IO.FileChunk chunkTile = new GR.IO.FileChunk( FileChunkConstants.MAP_TILE );

        chunkTile.AppendString( tile.Name );
        chunkTile.AppendI32( tile.Chars.Width );
        chunkTile.AppendI32( tile.Chars.Height );
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            TileChar    tChar = tile.Chars[i, j];
            chunkTile.AppendU8( tChar.Character );
            chunkTile.AppendU8( tChar.Color );
          }
        }
        chunkProjectData.Append( chunkTile.ToBuffer() );
      }
      foreach ( Map map in Maps )
      {
        GR.IO.FileChunk chunkMap = new GR.IO.FileChunk( FileChunkConstants.MAP );

        GR.IO.FileChunk chunkMapInfo = new GR.IO.FileChunk( FileChunkConstants.MAP_INFO );

        chunkMapInfo.AppendString( map.Name );
        chunkMapInfo.AppendI32( map.TileSpacingX );
        chunkMapInfo.AppendI32( map.TileSpacingY );
        chunkMapInfo.AppendI32( map.AlternativeMultiColor1 + 1 );
        chunkMapInfo.AppendI32( map.AlternativeMultiColor2 + 1 );
        chunkMapInfo.AppendI32( map.AlternativeBackgroundColor + 1 );
        chunkMapInfo.AppendI32( map.AlternativeBGColor4 + 1 );
        chunkMapInfo.AppendI32( (int)map.AlternativeMode + 1 );
        chunkMap.Append( chunkMapInfo.ToBuffer() );

        GR.IO.FileChunk chunkMapData = new GR.IO.FileChunk( FileChunkConstants.MAP_DATA );
        chunkMapData.AppendI32( map.Tiles.Width );
        chunkMapData.AppendI32( map.Tiles.Height );
        for ( int j = 0; j < map.Tiles.Height; ++j )
        {
          for ( int i = 0; i < map.Tiles.Width; ++i )
          {
            chunkMapData.AppendI32( map.Tiles[i, j] );
          }
        }
        chunkMap.Append( chunkMapData.ToBuffer() );

        if ( map.ExtraDataText.Length > 0 )
        {
          GR.IO.FileChunk chunkMapExtraData = new GR.IO.FileChunk( FileChunkConstants.MAP_EXTRA_DATA_TEXT );

          chunkMapExtraData.AppendString( map.ExtraDataText );

          chunkMap.Append( chunkMapExtraData.ToBuffer() );
        }
        if ( map.ExtraDataOld.Length > 0 )
        {
          GR.IO.FileChunk chunkMapExtraData = new GR.IO.FileChunk( FileChunkConstants.MAP_EXTRA_DATA );

          chunkMapExtraData.AppendU32( map.ExtraDataOld.Length );
          chunkMapExtraData.Append( map.ExtraDataOld );

          chunkMap.Append( chunkMapExtraData.ToBuffer() );
        }
        chunkProjectData.Append( chunkMap.ToBuffer() );
      }

      projectFile.Append( chunkProjectData.ToBuffer() );

      GR.IO.FileChunk chunkExportSettings = new GR.IO.FileChunk( FileChunkConstants.MAP_PROJECT_EXPORT_SETTINGS );
      chunkExportSettings.AppendU32( 4 );
      chunkExportSettings.AppendI32(Settings.ExportDataIndex );
      chunkExportSettings.AppendI32(Settings.ExportOrientationIndex );
      chunkExportSettings.AppendI32( Settings.ExportMethodIndex );
      chunkExportSettings.AppendI32( Settings.Assembly.PrefixWith ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.Prefix ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.WrapAt ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.WrapByteCount );
      chunkExportSettings.AppendI32( Settings.Assembly.ExportHex ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.VariableNameLabelPrefixEnabled ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.VariableNameLabelPrefix ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.IncludeSemicolonAfterSimpleLabels ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.MapSizeCommentEnabled ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Assembly.CommentChars ?? "" );
      chunkExportSettings.AppendI32( Settings.Assembly.EmptyTileCompressionEnabled ? 1 : 0 );
      chunkExportSettings.AppendI32( Settings.Assembly.EmptyTileIndex );
      chunkExportSettings.AppendI32( Settings.Binary.PrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.Binary.PrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendI32( Settings.CharsetBinary.PrefixLoadAddress ? 1 : 0 );
      chunkExportSettings.AppendString( Settings.CharsetBinary.PrefixLoadAddressHex ?? "" );
      chunkExportSettings.AppendString( Settings.CharsetProject.TargetFilename ?? "" );
      chunkExportSettings.AppendString( Settings.Charscreen.TargetFilename ?? "" );
      projectFile.Append( chunkExportSettings.ToBuffer() );
      return projectFile;
    }



    public bool ReadFromBuffer( GR.Memory.ByteBuffer ProjectFile )
    {
      if ( ProjectFile == null )
      {
        return false;
      }

      GR.IO.MemoryReader    memReader = new GR.IO.MemoryReader( ProjectFile );

      GR.IO.FileChunk chunk = new GR.IO.FileChunk();

      string importedCharSet = "";
      while ( chunk.ReadFromStream( memReader ) )
      {
        GR.IO.MemoryReader chunkReader = chunk.MemoryReader();
        switch ( chunk.Type )
        {
          case FileChunkConstants.MAP_PROJECT_INFO:
            {
              uint version  = chunkReader.ReadUInt32();
              importedCharSet = chunkReader.ReadString();

              ShowGrid = ( chunkReader.ReadInt32() == 1 );
            }
            break;
          case FileChunkConstants.MAP_CHARSET:
            {
              GR.Memory.ByteBuffer    data = new GR.Memory.ByteBuffer();
              chunkReader.ReadBlock( data, (uint)( chunkReader.Size - chunkReader.Position ) );

              Charset.ReadFromBuffer( data );
            }
            break;
          case FileChunkConstants.MAP_PROJECT_DATA:
            {
              GR.IO.FileChunk chunkData = new GR.IO.FileChunk();

              while ( chunkData.ReadFromStream( chunkReader ) )
              {
                GR.IO.MemoryReader subChunkReader = chunkData.MemoryReader();
                switch ( chunkData.Type )
                {
                  case FileChunkConstants.MULTICOLOR_DATA:
                    Mode = (TextMode)subChunkReader.ReadUInt8();
                    BackgroundColor = subChunkReader.ReadUInt8();
                    MultiColor1 = subChunkReader.ReadUInt8();
                    MultiColor2 = subChunkReader.ReadUInt8();
                    BGColor4 = subChunkReader.ReadUInt8();
                    break;
                  case FileChunkConstants.MAP_TILE:
                    {
                      Tile tile = new Tile();
                      tile.Name = subChunkReader.ReadString();

                      int w = subChunkReader.ReadInt32();
                      int h = subChunkReader.ReadInt32();

                      tile.Chars.Resize( w, h );
                      for ( int j = 0; j < tile.Chars.Height; ++j )
                      {
                        for ( int i = 0; i < tile.Chars.Width; ++i )
                        {
                          tile.Chars[i, j].Character = subChunkReader.ReadUInt8();
                          tile.Chars[i, j].Color = subChunkReader.ReadUInt8();
                        }
                      }
                      Tiles.Add( tile );
                      tile.Index = Tiles.Count - 1;
                    }
                    break;
                  case FileChunkConstants.MAP:
                    {
                      GR.IO.FileChunk mapChunk = new GR.IO.FileChunk();

                      Map map = new Map();

                      while ( mapChunk.ReadFromStream( subChunkReader ) )
                      {
                        GR.IO.MemoryReader mapChunkReader = mapChunk.MemoryReader();
                        switch ( mapChunk.Type )
                        {
                          case FileChunkConstants.MAP_INFO:
                            map.Name = mapChunkReader.ReadString();
                            map.TileSpacingX = mapChunkReader.ReadInt32();
                            map.TileSpacingY = mapChunkReader.ReadInt32();
                            map.AlternativeMultiColor1 = mapChunkReader.ReadInt32() - 1;
                            map.AlternativeMultiColor2 = mapChunkReader.ReadInt32() - 1;
                            map.AlternativeBackgroundColor = mapChunkReader.ReadInt32() - 1;
                            map.AlternativeBGColor4 = mapChunkReader.ReadInt32() - 1;
                            map.AlternativeMode = (TextCharMode)( mapChunkReader.ReadInt32() - 1 );
                            break;
                          case FileChunkConstants.MAP_DATA:
                            {
                              int w = mapChunkReader.ReadInt32();
                              int h = mapChunkReader.ReadInt32();

                              map.Tiles.Resize( w, h );
                              for ( int j = 0; j < map.Tiles.Height; ++j )
                              {
                                for ( int i = 0; i < map.Tiles.Width; ++i )
                                {
                                  map.Tiles[i, j] = mapChunkReader.ReadInt32();
                                }
                              }
                            }
                            break;
                          case FileChunkConstants.MAP_EXTRA_DATA:
                            {
                              uint len = mapChunkReader.ReadUInt32();

                              mapChunkReader.ReadBlock( map.ExtraDataOld, len );

                              map.ExtraDataText = map.ExtraDataOld.ToString();
                              map.ExtraDataOld.Clear();
                            }
                            break;
                          case FileChunkConstants.MAP_EXTRA_DATA_TEXT:
                            {
                              map.ExtraDataText = mapChunkReader.ReadString();
                            }
                            break;
                        }
                      }

                      Maps.Add( map );
                    }
                    break;
                }
              }
            }
            break;
          case FileChunkConstants.MAP_PROJECT_EXPORT_SETTINGS:
            {
              uint version = chunkReader.ReadUInt32();
              if ( version == 0 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
              }
              else if ( version == 1 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefixEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefix = chunkReader.ReadString();
                Settings.Assembly.IncludeSemicolonAfterSimpleLabels = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.MapSizeCommentEnabled = true;
                Settings.Assembly.CommentChars = ";";
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
              }
              else if ( version == 2 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefixEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefix = chunkReader.ReadString();
                Settings.Assembly.IncludeSemicolonAfterSimpleLabels = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.CommentChars = chunkReader.ReadString();
                Settings.Assembly.MapSizeCommentEnabled = !string.IsNullOrEmpty( Settings.Assembly.CommentChars );
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
              }
              else if ( version == 3 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefixEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefix = chunkReader.ReadString();
                Settings.Assembly.IncludeSemicolonAfterSimpleLabels = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.MapSizeCommentEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.CommentChars = chunkReader.ReadString();
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
              }
              else if ( version == 4 )
              {
                Settings.ExportDataIndex = chunkReader.ReadInt32();
                Settings.ExportOrientationIndex = chunkReader.ReadInt32();
                Settings.ExportMethodIndex = chunkReader.ReadInt32();
                Settings.Assembly.PrefixWith = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.Prefix = chunkReader.ReadString();
                Settings.Assembly.WrapAt = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.WrapByteCount = chunkReader.ReadInt32();
                Settings.Assembly.ExportHex = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefixEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.VariableNameLabelPrefix = chunkReader.ReadString();
                Settings.Assembly.IncludeSemicolonAfterSimpleLabels = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.MapSizeCommentEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.CommentChars = chunkReader.ReadString();
                Settings.Assembly.EmptyTileCompressionEnabled = ( chunkReader.ReadInt32() != 0 );
                Settings.Assembly.EmptyTileIndex = chunkReader.ReadInt32();
                Settings.Binary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.Binary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetBinary.PrefixLoadAddress = ( chunkReader.ReadInt32() != 0 );
                Settings.CharsetBinary.PrefixLoadAddressHex = chunkReader.ReadString();
                Settings.CharsetProject.TargetFilename = chunkReader.ReadString();
                Settings.Charscreen.TargetFilename = chunkReader.ReadString();
              }
            }
            break;
        }
      }
      memReader.Close();


      Charset.Colors.MultiColor1 = MultiColor1;
      Charset.Colors.MultiColor2 = MultiColor2;
      Charset.Colors.BGColor4    = BGColor4;
      return true;
    }



    public GR.Memory.ByteBuffer ExportAsTiles()
    {
      GR.Memory.ByteBuffer    tileData = new GR.Memory.ByteBuffer();

      // find max tile size
      int     tileW = 1;
      int     tileH = 1;

      foreach ( var tile in Tiles )
      {
        if ( tile.Chars.Width > tileW )
        {
          tileW = tile.Chars.Width;
        }
        if ( tile.Chars.Height > tileH )
        {
          tileH = tile.Chars.Height;
        }
      }

      for ( int j = 0; j < tileH; ++j )
      {
        for ( int i = 0; i < tileW; ++i )
        {
          foreach ( Formats.MapProject.Tile tile in Tiles )
          {
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileData.AppendU8( (byte)tile.Chars[i,j].Character );
            }
            else
            {
              tileData.AppendU8( 0 );
            }
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
            else
            {
              tileData.AppendU8( 0 );
            }
          }
        }
      }
      return tileData;
    }



    public GR.Memory.ByteBuffer ExportMapsAsBuffer( bool RowByRow )
    {
      GR.Memory.ByteBuffer    mapData = new GR.Memory.ByteBuffer();


      foreach ( var map in Maps )
      {
        mapData.Append( ExportMapAsBuffer( map, RowByRow ) );
      }
      return mapData;
    }



    public string ExportMapsAsAssembly( string Prefix, bool RowByRow )
    {
      StringBuilder   sb = new StringBuilder();

      foreach ( var map in Maps )
      {
        GR.Memory.ByteBuffer      mapData = ExportMapAsBuffer( map, RowByRow );

        sb.Append( Prefix );
        sb.AppendLine( map.Name );

      }
      return sb.ToString();
    }



    public GR.Memory.ByteBuffer ExportMapAsBuffer( Map Map, bool RowByRow )
    {
      GR.Memory.ByteBuffer mapDataBuffer = new GR.Memory.ByteBuffer( (uint)( Map.Tiles.Width * Map.Tiles.Height ) );

      if ( RowByRow )
      {
        for ( int y = 0; y < Map.Tiles.Height; ++y )
        {
          for ( int x = 0; x < Map.Tiles.Width; ++x )
          {
            mapDataBuffer.SetU8At( x + y * Map.Tiles.Width, (byte)Map.Tiles[x, y] );
          }
        }
      }
      else
      {
        for ( int x = 0; x < Map.Tiles.Width; ++x )
        {
          for ( int y = 0; y < Map.Tiles.Height; ++y )          
          {
            mapDataBuffer.SetU8At( x + y * Map.Tiles.Width, (byte)Map.Tiles[x, y] );
          }
        }
      }
      return mapDataBuffer;
    }



    public bool ExportTilesAsElements( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      GR.Memory.ByteBuffer tileDataW = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileDataH = new GR.Memory.ByteBuffer();

      StringBuilder sbTileCharLo = new StringBuilder();
      StringBuilder sbTileCharHi = new StringBuilder();
      StringBuilder sbTileColorLo = new StringBuilder();
      StringBuilder sbTileColorHi = new StringBuilder();
      StringBuilder sbTileChars = new StringBuilder();
      StringBuilder sbTileColors = new StringBuilder();

      GR.Memory.ByteBuffer tileDataChars = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileDataHi = new GR.Memory.ByteBuffer();

      var usedLabels = new Dictionary<string, int>();

      foreach ( Formats.MapProject.Tile tile in Tiles )
      {
        tileDataW.AppendU8( (byte)tile.Chars.Width );
        tileDataH.AppendU8( (byte)tile.Chars.Height );

        string    normalizedLabel = NormalizeAsLabel( tile.Name ).ToUpper();
        if ( usedLabels.ContainsKey( normalizedLabel ) )
        {
          int   subIndex = usedLabels[normalizedLabel] + 1;

          usedLabels[normalizedLabel] = subIndex;
          normalizedLabel += "_" + subIndex;
        }
        else
        {
          usedLabels.Add( normalizedLabel, 1 );
        }

        sbTileCharLo.Append( DataByteDirective );
        sbTileCharLo.AppendLine( " <" + LabelPrefix + "TILE_CHAR_" + normalizedLabel );
        sbTileCharHi.Append( DataByteDirective );
        sbTileCharHi.AppendLine( " >" + LabelPrefix + "TILE_CHAR_" + normalizedLabel );

        sbTileColorLo.Append( DataByteDirective );
        sbTileColorLo.AppendLine( " <" + LabelPrefix + "TILE_COLOR_" + normalizedLabel );
        sbTileColorHi.Append( DataByteDirective );
        sbTileColorHi.AppendLine( " >" + LabelPrefix + "TILE_COLOR_" + normalizedLabel );

        sbTileChars.AppendLine( LabelPrefix + "TILE_CHAR_" + normalizedLabel );

        var tileCharData = new GR.Memory.ByteBuffer();
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            tileCharData.AppendU8( tile.Chars[i, j].Character );
          }
        }
        sbTileChars.AppendLine( Util.ToASMData( tileCharData, WrapData, WrapByteCount, DataByteDirective ) );


        sbTileColors.AppendLine( LabelPrefix + "TILE_COLOR_" + normalizedLabel );

        var tileColorData = new GR.Memory.ByteBuffer();
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            tileColorData.AppendU8( tile.Chars[i, j].Color );
          }
        }
        sbTileColors.AppendLine( Util.ToASMData( tileColorData, WrapData, WrapByteCount, DataByteDirective ) );

      }
      TileData = LabelPrefix + "NUM_TILES = " + Tiles.Count + System.Environment.NewLine
                + LabelPrefix + "TILE_WIDTH" + System.Environment.NewLine + Util.ToASMData( tileDataW, WrapData, WrapByteCount, DataByteDirective ) + System.Environment.NewLine
                + LabelPrefix + "TILE_HEIGHT" + System.Environment.NewLine + Util.ToASMData( tileDataH, WrapData, WrapByteCount, DataByteDirective ) + System.Environment.NewLine
                + LabelPrefix + "TILE_CHARS_LO" + System.Environment.NewLine
                + sbTileCharLo.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_CHARS_HI" + System.Environment.NewLine
                + sbTileCharHi.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_COLORS_LO" + System.Environment.NewLine
                + sbTileColorLo.ToString() + System.Environment.NewLine
                + LabelPrefix + "TILE_COLORS_HI" + System.Environment.NewLine
                + sbTileColorHi.ToString() + System.Environment.NewLine
                + sbTileChars.ToString() + System.Environment.NewLine
                + sbTileColors.ToString() + System.Environment.NewLine;
      return true;
    }



    public bool ExportTilesAsAssembly( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      int   maxTileWidth = 0;
      int   maxTileHeight = 0;
      foreach ( var tile in Tiles )
      {
        if ( tile.Chars.Width > maxTileWidth )
        {
          maxTileWidth = tile.Chars.Width;
        }
        if ( tile.Chars.Height > maxTileHeight )
        {
          maxTileHeight = tile.Chars.Height;
        }
      }

      GR.Memory.ByteBuffer[]  tileCharData = new GR.Memory.ByteBuffer[maxTileWidth * maxTileHeight];
      GR.Memory.ByteBuffer[]  tileColorData = new GR.Memory.ByteBuffer[maxTileWidth * maxTileHeight];
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          tileCharData[i + j * maxTileWidth] = new GR.Memory.ByteBuffer( (uint)Tiles.Count );
          tileColorData[i + j * maxTileWidth] = new GR.Memory.ByteBuffer( (uint)Tiles.Count );
        }
      }


      int tileIndex = 0;
      
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          tileIndex = 0;
          foreach ( var tile in Tiles )
          {
            if ( ( i < tile.Chars.Width )
            &&   ( j < tile.Chars.Height ) )
            {
              tileCharData[i + j * maxTileWidth].SetU8At( tileIndex, tile.Chars[i, j].Character );
              tileColorData[i + j * maxTileWidth].SetU8At( tileIndex, tile.Chars[i, j].Color );
            }
            ++tileIndex;
          }
        }
      }

      StringBuilder sb = new StringBuilder();

      sb.AppendLine( LabelPrefix + "NUM_TILES = " + Tiles.Count );

      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          sb.Append( LabelPrefix + "TILE_CHARS_" );
          sb.Append( i );
          sb.Append( "_" );
          sb.Append( j );
          sb.AppendLine();

          sb.AppendLine( Util.ToASMData( tileCharData[i + j * maxTileWidth], WrapData, WrapByteCount, DataByteDirective ) );
          sb.AppendLine();
        }
      }
      for ( int j = 0; j < maxTileHeight; ++j )
      {
        for ( int i = 0; i < maxTileWidth; ++i )
        {
          sb.Append( LabelPrefix + "TILE_COLORS_" );
          sb.Append( i );
          sb.Append( "_" );
          sb.Append( j );
          sb.AppendLine();

          sb.AppendLine( Util.ToASMData( tileColorData[i + j * maxTileWidth], WrapData, WrapByteCount, DataByteDirective ) );
          sb.AppendLine();
        }
      }
      TileData = sb.ToString();
      return true;
    }



    public bool ExportTileNamesAsAssembly( out string TileData, string LabelPrefix )
    {
      TileData = "";

      var sb = new StringBuilder();

      var usedLabels = new Dictionary<string, int>();

      string  prefix = NormalizeAsLabel( LabelPrefix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        sb.Append( prefix );
        sb.Append( "TILE_NAME_" );

        string    normalizedLabel = NormalizeAsLabel( Tiles[i].Name ).ToUpper();
        if ( usedLabels.ContainsKey( normalizedLabel ) )
        {
          int   subIndex = usedLabels[normalizedLabel] + 1;

          usedLabels[normalizedLabel] = subIndex;
          normalizedLabel += "_" + subIndex;
        }
        else
        {
          usedLabels.Add( normalizedLabel, 1 );
        }

        sb.Append( normalizedLabel );
        sb.Append( "=" );
        sb.Append( i );
        sb.AppendLine();
      }
      TileData = sb.ToString();
      return true;
    }



    public bool ExportTileDataAsAssembly( out string TileData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      var sbTileChars = new StringBuilder();
      var sbTileColors = new StringBuilder();

      int tileIndex = 0;
      foreach ( var tile in Tiles )
      {
        sbTileChars.Append( LabelPrefix );
        sbTileChars.Append( '_' );
        sbTileChars.Append( tileIndex );
        sbTileChars.AppendLine( "_CHARS" );
        sbTileChars.Append( DataByteDirective );
        sbTileChars.Append( ' ' );

        sbTileColors.Append( LabelPrefix );
        sbTileColors.Append( '_' );
        sbTileColors.Append( tileIndex );
        sbTileColors.AppendLine( "_COLORS" );
        sbTileColors.Append( DataByteDirective );
        sbTileColors.Append( ' ' );
        for ( int j = 0; j < tile.Chars.Height; ++j )
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            sbTileChars.Append( "$" );
            sbTileChars.Append( tile.Chars[i, j].Character.ToString( "X2" ) );
            sbTileColors.Append( "$" );
            sbTileColors.Append( tile.Chars[i, j].Color.ToString( "X2" ) );

            if ( ( i + 1 < tile.Chars.Width )
            ||   ( j + 1 < tile.Chars.Height ) )
            {
              sbTileChars.Append( ',' );
              sbTileColors.Append( ',' );
            }
          }
        }
        sbTileChars.AppendLine();
        sbTileColors.AppendLine();
        ++tileIndex;
      }

      var sb = new StringBuilder();

      TileData = sb.ToString() + sbTileChars.ToString() + sbTileColors.ToString();
      return true;
    }



    public bool ExportMapsAsAssembly( bool Vertical, out string MapData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective, string CommentChars )
    {
      bool hasExtraData = false;
      foreach ( var map in Maps )
      {
        if ( map.ExtraDataText.Length > 0 )
        {
          hasExtraData = true;
          break;
        }
      }

      StringBuilder sbMaps = new StringBuilder();

      sbMaps.Append( LabelPrefix );
      sbMaps.Append( "NUM_MAPS = " );
      sbMaps.AppendLine( Maps.Count.ToString() );

      sbMaps.Append( LabelPrefix );
      sbMaps.AppendLine( "MAP_LIST_LO" );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        sbMaps.Append( DataByteDirective );
        sbMaps.Append( ' ' );
        sbMaps.AppendLine( "<" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
      }
      sbMaps.AppendLine();
      sbMaps.Append( LabelPrefix );
      sbMaps.AppendLine( "MAP_LIST_HI" );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        sbMaps.Append( DataByteDirective );
        sbMaps.Append( ' ' );
        sbMaps.AppendLine( ">" + LabelPrefix + "MAP_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
      }
      sbMaps.AppendLine();

      if ( hasExtraData )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_LO" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_HI" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( ">" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.AppendLine();
      }


      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];

        sbMaps.AppendLine();
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_" + NormalizeAsLabel( map.Name.ToUpper() ) );

        GR.Memory.ByteBuffer mapDataBuffer = new GR.Memory.ByteBuffer( (uint)( map.Tiles.Width * map.Tiles.Height ) );

        if ( Vertical )
        {
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              mapDataBuffer.SetU8At( x * map.Tiles.Height + y, (byte)map.Tiles[x, y] );
            }
          }
        }
        else
        {
          for ( int y = 0; y < map.Tiles.Height; ++y )
          {
            for ( int x = 0; x < map.Tiles.Width; ++x )
            {
              mapDataBuffer.SetU8At( x + y * map.Tiles.Width, (byte)map.Tiles[x, y] );
            }
          }
        }
        sbMaps.Append( DataByteDirective );
        if ( !DataByteDirective.EndsWith( " " ) )
        {
          sbMaps.Append( ' ' );
        }
        sbMaps.Append( "$" );
        sbMaps.Append( ( (byte)map.Tiles.Width ).ToString( "X2" ) );
        sbMaps.Append( ", $" );
        sbMaps.Append( ( (byte)map.Tiles.Height ).ToString( "X2" ) );
        if ( !string.IsNullOrEmpty( CommentChars ) )
        {
          sbMaps.Append( ' ' );
          sbMaps.Append( CommentChars );
          if ( !CommentChars.EndsWith( " " ) )
          {
            sbMaps.Append( ' ' );
          }
          sbMaps.Append( "map width, height" );
        }
        sbMaps.AppendLine();
        sbMaps.AppendLine();
        sbMaps.Append( Util.ToASMData( mapDataBuffer, WrapData, WrapByteCount, DataByteDirective ) );
        if ( hasExtraData )
        //&&   ( map.ExtraDataText.Length > 0 ) )
        {
          sbMaps.AppendLine( ";extra data" );
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_EXTRA_DATA_" + NormalizeAsLabel( map.Name.ToUpper() ) );

          // clean extra data
          GR.Memory.ByteBuffer    extraData = new GR.Memory.ByteBuffer();
          string[]  lines = map.ExtraDataText.Split( new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
          foreach ( string line in lines )
          {
            string    tempLine = line.Trim().Replace( " ", "" );
            if ( ( !tempLine.StartsWith( ";" ) )
            &&   ( !tempLine.StartsWith( "#" ) )
            &&   ( !tempLine.StartsWith( "//" ) ) )
            {
              extraData.AppendHex( tempLine );
            }
          }

          sbMaps.Append( Util.ToASMData( extraData, WrapData, WrapByteCount, DataByteDirective ) );
          sbMaps.AppendLine();
        }
      }

      MapData = sbMaps.ToString();
      return true;
    }



    private string NormalizeAsLabel( string Label )
    {
      StringBuilder   sb = new StringBuilder();

      // remove diacritics
      string normalizedString = Label.Normalize( NormalizationForm.FormD );

      foreach ( var c in normalizedString )
      {
        if ( CharUnicodeInfo.GetUnicodeCategory( c ) == UnicodeCategory.NonSpacingMark )
        {
          continue;
        }
        if ( ( !char.IsDigit( c ) )
        &&   ( !char.IsLetter( c ) )
        &&   ( c != '_' ) )
        {
          sb.Append( '_' );
        }
        else
        {
          sb.Append( c );
        }
      }
      return sb.ToString();
    }



    public bool ExportMapExtraDataAsAssembly( out string MapData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective )
    {
      bool hasExtraData = false;
      foreach ( var map in Maps )
      {
        if ( map.ExtraDataText.Length > 0 )
        {
          hasExtraData = true;
          break;
        }
      }

      StringBuilder sbMaps = new StringBuilder();

      sbMaps.Append( LabelPrefix );
      sbMaps.Append( "NUM_MAPS = " );
      sbMaps.AppendLine( Maps.Count.ToString() );

      if ( hasExtraData )
      {
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_LO" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( "<" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.Append( LabelPrefix );
        sbMaps.AppendLine( "MAP_EXTRA_DATA_LIST_HI" );
        for ( int i = 0; i < Maps.Count; ++i )
        {
          sbMaps.Append( DataByteDirective );
          sbMaps.Append( ' ' );
          sbMaps.AppendLine( ">" + LabelPrefix + "MAP_EXTRA_DATA_" + NormalizeAsLabel( Maps[i].Name.ToUpper() ) );
        }
        sbMaps.AppendLine();
      }


      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];

        if ( hasExtraData )
        //&&   ( map.ExtraDataText.Length > 0 ) )
        {
          sbMaps.AppendLine( ";extra data" );
          sbMaps.Append( LabelPrefix );
          sbMaps.AppendLine( "MAP_EXTRA_DATA_" + NormalizeAsLabel( map.Name.ToUpper() ) );

          // clean extra data
          GR.Memory.ByteBuffer    extraData = new GR.Memory.ByteBuffer();
          string[]  lines = map.ExtraDataText.Split( new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
          foreach ( string line in lines )
          {
            string    tempLine = line.Trim().Replace( " ", "" );
            if ( ( !tempLine.StartsWith( ";" ) )
            &&   ( !tempLine.StartsWith( "#" ) )
            &&   ( !tempLine.StartsWith( "//" ) ) )
            {
              extraData.AppendHex( tempLine );
            }
          }

          sbMaps.Append( Util.ToASMData( extraData, WrapData, WrapByteCount, DataByteDirective ) );
          sbMaps.AppendLine();
        }
      }

      MapData = sbMaps.ToString();
      return true;
    }



    internal void ExportTilesAsBuffer( bool RowByRow, out GR.Memory.ByteBuffer TileData )
    {
      TileData = new GR.Memory.ByteBuffer();

      foreach ( Formats.MapProject.Tile tile in Tiles )
      {
        if ( RowByRow )
        {
          for ( int j = 0; j < tile.Chars.Height; ++j )
          {
            for ( int i = 0; i < tile.Chars.Width; ++i )
            {
              TileData.AppendU8( (byte)tile.Chars[i, j].Character );
              TileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
          }
        }
        else
        {
          for ( int i = 0; i < tile.Chars.Width; ++i )
          {
            for ( int j = 0; j < tile.Chars.Height; ++j )            
            {
              TileData.AppendU8( (byte)tile.Chars[i, j].Character );
              TileData.AppendU8( (byte)tile.Chars[i, j].Color );
            }
          }
        }
      }
    }



    public bool ExportSparseTileAndMapData( out string ExportData, string LabelPrefix, bool WrapData, int WrapByteCount, string DataByteDirective, bool EmptyTileCompression, int EmptyTileIndex )
    {
      StringBuilder sb = new StringBuilder();

      string labelSuffix = "";
      if ( Settings.Assembly.IncludeSemicolonAfterSimpleLabels )
      {
        labelSuffix = ":";
      }

      // Tiles Data
      sb.AppendLine( LabelPrefix + "TILE_COUNT=" + Tiles.Count );
      sb.AppendLine();

      GR.Memory.ByteBuffer tileWidths = new GR.Memory.ByteBuffer();
      GR.Memory.ByteBuffer tileHeights = new GR.Memory.ByteBuffer();

      foreach ( var tile in Tiles )
      {
        tileWidths.AppendU8( (byte)tile.Chars.Width );
        tileHeights.AppendU8( (byte)tile.Chars.Height );
      }

      sb.AppendLine( LabelPrefix + "TILES_WIDTH" + labelSuffix );
      sb.AppendLine( Util.ToASMData( tileWidths, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_HEIGHT" + labelSuffix );
      sb.AppendLine( Util.ToASMData( tileHeights, WrapData, WrapByteCount, DataByteDirective ) );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_CHAR_DATA" + labelSuffix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        var tile = Tiles[i];
        sb.Append( LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) + labelSuffix + " " );
        
        GR.Memory.ByteBuffer charData = new GR.Memory.ByteBuffer();
        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            charData.AppendU8( tile.Chars[x, y].Character );
          }
        }
        sb.Append( Util.ToASMData( charData, false, 0, DataByteDirective ) );
        if ( Settings.Assembly.MapSizeCommentEnabled )
        {
          sb.Append( "\t\t\t" + Settings.Assembly.CommentChars + " tile " + i + ", " + tile.Chars.Width + "x" + tile.Chars.Height );
        }
        sb.AppendLine();
      }
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_DATA" + labelSuffix );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        var tile = Tiles[i];
        sb.Append( LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) + labelSuffix + " " );

        GR.Memory.ByteBuffer colorData = new GR.Memory.ByteBuffer();
        for ( int y = 0; y < tile.Chars.Height; ++y )
        {
          for ( int x = 0; x < tile.Chars.Width; ++x )
          {
            colorData.AppendU8( tile.Chars[x, y].Color );
          }
        }
        sb.AppendLine( Util.ToASMData( colorData, false, 0, DataByteDirective ) );
      }
      sb.AppendLine();

      // Tables
      sb.AppendLine( LabelPrefix + "TILES_CHAR_TABLE_LOW" + labelSuffix );
      GR.Memory.ByteBuffer tableLow = new GR.Memory.ByteBuffer();
      StringBuilder sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_CHAR_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "TILE_CHAR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_TABLE_LOW" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "TILES_COLOR_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Tiles.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "TILE_COLOR_" + i.ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      // Map Data
      if ( Settings.Assembly.MapSizeCommentEnabled )
      {
        sb.AppendLine( Settings.Assembly.CommentChars + " map data" );
      }
      sb.AppendLine( LabelPrefix + "MAP_COUNT=" + Maps.Count );
      sb.AppendLine();

      sb.AppendLine( LabelPrefix + "MAPS_TABLE_LOW" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( "<" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();
      
      sb.AppendLine( LabelPrefix + "MAPS_TABLE_HIGH" + labelSuffix );
      sbTable = new StringBuilder();
      sbTable.Append( DataByteDirective + " " );
      for ( int i = 0; i < Maps.Count; ++i )
      {
        if ( i > 0 ) sbTable.Append( ", " );
        sbTable.Append( ">" + LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) );
      }
      sb.AppendLine( sbTable.ToString() );
      sb.AppendLine();

      for ( int i = 0; i < Maps.Count; ++i )
      {
        var map = Maps[i];
        sb.Append( LabelPrefix + "MAP_" + ( i + 1 ).ToString( "D2" ) + labelSuffix + " " );
        if ( Settings.Assembly.MapSizeCommentEnabled )
        {
          sb.Append( Settings.Assembly.CommentChars + " " + map.Name );
        }
        sb.AppendLine();
        
        // Map Size
        GR.Memory.ByteBuffer mapSize = new GR.Memory.ByteBuffer();
        mapSize.AppendU8( (byte)map.Tiles.Width );
        mapSize.AppendU8( (byte)map.Tiles.Height );
        sb.Append( Util.ToASMData( mapSize, false, 0, DataByteDirective ) );
        if ( Settings.Assembly.MapSizeCommentEnabled )
        {
          sb.Append( " " + Settings.Assembly.CommentChars + " map width, height" );
        }
        sb.AppendLine();

        // Collect tiles
        GR.Memory.ByteBuffer mapTiles = new GR.Memory.ByteBuffer();
        int tileCount = 0;
        for ( int y = 0; y < map.Tiles.Height; ++y )
        {
          for ( int x = 0; x < map.Tiles.Width; ++x )
          {
            int tileIndex = map.Tiles[x, y];
            if ( ( EmptyTileCompression )
            &&   ( tileIndex == EmptyTileIndex ) )
            {
              continue;
            }
            mapTiles.AppendU8( (byte)tileIndex );
            mapTiles.AppendU8( (byte)x );
            mapTiles.AppendU8( (byte)y );
            tileCount++;
          }
        }

        // List size
        GR.Memory.ByteBuffer listSize = new GR.Memory.ByteBuffer();
        listSize.AppendU8( (byte)( tileCount & 0xff ) );
        listSize.AppendU8( (byte)( ( tileCount >> 8 ) & 0xff ) );
        sb.Append( Util.ToASMData( listSize, false, 0, DataByteDirective ) );
        if ( Settings.Assembly.MapSizeCommentEnabled )
        {
          sb.Append( " " + Settings.Assembly.CommentChars + " tile count" );
        }
        sb.AppendLine();

        int ptr = 0;
        while ( ptr < mapTiles.Length )
        {
           byte t = mapTiles.ByteAt( ptr );
           byte tx = mapTiles.ByteAt( ptr + 1 );
           byte ty = mapTiles.ByteAt( ptr + 2 );
           
           GR.Memory.ByteBuffer entry = new GR.Memory.ByteBuffer();
           entry.AppendU8( t );
           entry.AppendU8( tx );
           entry.AppendU8( ty );
           
           string entryHex = Util.ToASMData( entry, false, 0, DataByteDirective );
           
           sb.Append( entryHex );
           if ( Settings.Assembly.MapSizeCommentEnabled )
           {
             string comment = "";
             if ( t < Tiles.Count )
             {
                var tile = Tiles[t];
                comment = Settings.Assembly.CommentChars + " tile " + t + ", " + tile.Chars.Width + "x" + tile.Chars.Height;
             }
             sb.Append( "\t\t" + comment );
           }
           sb.AppendLine();
           ptr += 3;
        }
        sb.AppendLine();
      }

      ExportData = sb.ToString();
      return true;
    }


  }
}
