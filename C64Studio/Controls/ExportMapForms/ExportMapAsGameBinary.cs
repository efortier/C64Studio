using RetroDevStudio.Types;
using GR.Memory;
using RetroDevStudio;
using RetroDevStudio.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  public partial class ExportMapAsGameBinary : ExportMapFormBase
  {
    private bool m_ApplyingSettings = false;

    public ExportMapAsGameBinary() :
      base( null )
    {
    }



    public ExportMapAsGameBinary( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();
      editAbsoluteBaseAddress.TextChanged += HandleSettingsChanged;
      editPrefixLoadAddress.TextChanged += HandleSettingsChanged;
      editExportDirectory.TextChanged += HandleSettingsChanged;
      editExportFilename.TextChanged += HandleSettingsChanged;
      checkExportMarkers.CheckedChanged += HandleSettingsChanged;
      checkExportColors.CheckedChanged += HandleSettingsChanged;
      checkExportPassable.CheckedChanged += HandleSettingsChanged;
      editCharsetExportDirectory.TextChanged += HandleSettingsChanged;
      editCharsetExportFilename.TextChanged += HandleSettingsChanged;
      editCharsetPrefixLoadAddress.TextChanged += HandleSettingsChanged;
      editHeaderAsmDirectory.TextChanged += HandleSettingsChanged;
      editHeaderAsmFilename.TextChanged += HandleSettingsChanged;
      editHeaderAsmPrefix.TextChanged += HandleSettingsChanged;
      editMarkerLabelsDirectory.TextChanged += HandleSettingsChanged;
      editMarkerLabelsFilename.TextChanged += HandleSettingsChanged;
      editMarkerLabelsPrefix.TextChanged += HandleSettingsChanged;
      editEntityLabelsDirectory.TextChanged += HandleSettingsChanged;
      editEntityLabelsFilename.TextChanged += HandleSettingsChanged;
      editEntityLabelsPrefix.TextChanged += HandleSettingsChanged;
      editMapStringsDirectory.TextChanged += HandleSettingsChanged;
      editMapStringsFilename.TextChanged += HandleSettingsChanged;
      editMapStringsPrefix.TextChanged += HandleSettingsChanged;
    }



    public override bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      bool exportMarkers = checkExportMarkers.Checked;
      bool exportColors = checkExportColors.Checked;
      bool exportPassable = checkExportPassable.Checked;

      ushort baseAddress = 0;
      if ( checkAbsoluteBaseAddress.Checked && !string.IsNullOrEmpty( editAbsoluteBaseAddress.Text ) )
      {
        baseAddress = GR.Convert.ToU16( editAbsoluteBaseAddress.Text, 16 );
      }

      GR.Memory.ByteBuffer data = Info.Map.ExportAsGameBinary(
        exportMarkers,
        exportColors,
        exportPassable,
        baseAddress );

      if ( data == null )
      {
        return false;
      }

      GR.Memory.ByteBuffer finalData = data;
      if ( checkPrefixLoadAddress.Checked )
      {
        ushort address = GR.Convert.ToU16( editPrefixLoadAddress.Text, 16 );
        var addressData = new ByteBuffer();
        addressData.AppendU16( address );
        finalData = addressData + finalData;
      }

      string targetPath = null;

      if ( checkSaveOnExport.Checked
      &&   !string.IsNullOrEmpty( editExportDirectory.Text )
      &&   !string.IsNullOrEmpty( editExportFilename.Text ) )
      {
        targetPath = System.IO.Path.Combine( editExportDirectory.Text, editExportFilename.Text );
      }
      else
      {
        System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();
        saveDlg.Title = "Save game binary data";
        saveDlg.Filter = "Game Binary Data|*.bin|All Files|*.*";

        if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
        {
          return false;
        }
        targetPath = saveDlg.FileName;
      }

      try
      {
        GR.IO.File.WriteAllBytes( targetPath, finalData );
      }
      catch ( Exception ex )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Error saving file",
            "Could not save exported game binary to:\r\n" + targetPath + "\r\n\r\n" + ex.Message );
        }
        return false;
      }

      string log = GenerateExportLog( data, baseAddress, targetPath,
                                      exportMarkers, exportColors, exportPassable,
                                      Info.Map );

      // Optional .def sidecar
      if ( checkGenerateDefFile.Checked )
      {
        string defPath = System.IO.Path.ChangeExtension( targetPath, ".def" );
        try
        {
          System.IO.File.WriteAllText( defPath, log );
        }
        catch ( Exception ex )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Error saving .def file",
              "Could not save the layout sidecar file to:\r\n" + defPath + "\r\n\r\n" + ex.Message );
          }
          // continue — the binary itself was saved
        }
      }

      // Optional map_header.asm — opt-in. Uses the explicit directory/filename
      // settings when provided, otherwise falls back to the binary's directory
      // and "map_header.asm". Regenerated on every export so it never drifts.
      if ( checkExportHeaderAsm.Checked )
      {
        string headerPrefix = editHeaderAsmPrefix.Text;
        WriteAsmSidecar(
          "map_header.asm",
          editHeaderAsmDirectory.Text,
          editHeaderAsmFilename.Text,
          "map_header.asm",
          targetPath,
          () => RetroDevStudio.Formats.MapProject.GenerateGameBinaryHeaderAsm( headerPrefix ) );
      }

      // Optional marker-labels sidecar — maps ExportSymbol -> TagID.
      if ( checkExportMarkerLabels.Checked )
      {
        string markerPrefix = editMarkerLabelsPrefix.Text;
        var map = Info.Map;
        WriteAsmSidecar(
          "marker labels",
          editMarkerLabelsDirectory.Text,
          editMarkerLabelsFilename.Text,
          "map_markers.asm",
          targetPath,
          () => map.GenerateMarkerLabelsAsm( markerPrefix ) );
      }

      // Optional entity-labels sidecar — maps EntityType ExportSymbol -> TagID.
      if ( checkExportEntityLabels.Checked )
      {
        string entityPrefix = editEntityLabelsPrefix.Text;
        var map = Info.Map;
        WriteAsmSidecar(
          "entity labels",
          editEntityLabelsDirectory.Text,
          editEntityLabelsFilename.Text,
          "map_entities.asm",
          targetPath,
          () => map.GenerateEntityLabelsAsm( entityPrefix ) );
      }

      // Optional map-strings sidecar — Dreadhold-style byte-stream messages
      // plus MAP_STRING_LO/HI pointer tables and per-message index consts.
      if ( checkExportMapStrings.Checked )
      {
        string mapStringsPrefix = editMapStringsPrefix.Text;
        var map = Info.Map;
        WriteAsmSidecar(
          "map strings",
          editMapStringsDirectory.Text,
          editMapStringsFilename.Text,
          "map_strings.asm",
          targetPath,
          () => map.GenerateMapStringsAsm( mapStringsPrefix ) );
      }

      // Optional character set export (port of the old "As assembly" method's Character Set block)
      if ( checkExportCharset.Checked )
      {
        ExportCharsetBinary( Info, DocInfo );
      }

      if ( EditOutput != null )
      {
        EditOutput.Text = log;
      }
      return true;
    }



    private void WriteAsmSidecar( string Description, string ConfiguredDir, string ConfiguredFilename,
                                  string DefaultFilename, string BinaryPath, Func<string> GenerateContent )
    {
      string dir = ConfiguredDir;
      if ( string.IsNullOrEmpty( dir ) )
      {
        try
        {
          dir = System.IO.Path.GetDirectoryName( BinaryPath );
        }
        catch ( Exception )
        {
          dir = null;
        }
      }

      string filename = string.IsNullOrEmpty( ConfiguredFilename ) ? DefaultFilename : ConfiguredFilename;

      if ( string.IsNullOrEmpty( dir ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Sidecar not saved",
            "The " + Description + " file was not written because no output directory could be determined.\r\nSet a directory in the Game Binary export settings." );
        }
        return;
      }

      string fullPath = System.IO.Path.Combine( dir, filename );
      try
      {
        System.IO.File.WriteAllText( fullPath, GenerateContent() );
      }
      catch ( Exception ex )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Error saving " + Description,
            "Could not save the " + Description + " file to:\r\n" + fullPath + "\r\n\r\n" + ex.Message );
        }
        // continue — the binary itself was saved
      }
    }



    private void ExportCharsetBinary( ExportMapInfo Info, DocumentInfo DocInfo )
    {
      if ( ( Info == null )
      ||   ( Info.Map == null )
      ||   ( Info.Map.Charset == null ) )
      {
        return;
      }

      GR.Memory.ByteBuffer charData = Info.Map.Charset.CharacterData();
      if ( charData == null )
      {
        return;
      }

      if ( checkCharsetPrefixLoadAddress.Checked )
      {
        string addrText = editCharsetPrefixLoadAddress.Text ?? "";
        if ( ( addrText.Length == 4 )
        &&   ( GR.Convert.ToI32( addrText, 16 ) >= 0 ) )
        {
          int loadAddress = GR.Convert.ToI32( addrText, 16 );
          var prefixed = new GR.Memory.ByteBuffer();
          prefixed.AppendU16( (ushort)loadAddress );
          prefixed.Append( charData );
          charData = prefixed;
        }
      }

      string filename = editCharsetExportFilename.Text ?? "";
      if ( string.IsNullOrEmpty( filename ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Character set not exported",
            "Character set export is enabled but no filename was provided." );
        }
        return;
      }

      string fullPath = filename;
      string exportDirectory = editCharsetExportDirectory.Text ?? "";

      if ( string.IsNullOrEmpty( exportDirectory ) )
      {
        string docDir = null;
        if ( DocInfo != null )
        {
          try
          {
            docDir = System.IO.Path.GetDirectoryName( DocInfo.FullPath );
          }
          catch ( Exception )
          {
            docDir = null;
          }
        }
        if ( !string.IsNullOrEmpty( docDir ) )
        {
          fullPath = System.IO.Path.Combine( docDir, filename );
        }
      }
      else
      {
        try
        {
          fullPath = System.IO.Path.Combine( exportDirectory, filename );
        }
        catch ( Exception )
        {
          fullPath = filename;
        }
      }

      // Copy ByteBuffer into a raw byte[] for System.IO.File.WriteAllBytes
      byte[] rawData = new byte[charData.Length];
      for ( int i = 0; i < charData.Length; ++i )
      {
        rawData[i] = charData.ByteAt( i );
      }

      try
      {
        System.IO.File.WriteAllBytes( fullPath, rawData );
      }
      catch ( Exception ex )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Error saving character set",
            "Could not save the character set to:\r\n" + fullPath + "\r\n\r\n" + ex.Message );
        }
      }
    }



    private string Addr( ushort baseAddr, int offset )
    {
      return "$" + ( baseAddr + offset ).ToString( "X4" );
    }



    private string HexByte( byte b )
    {
      return "$" + b.ToString( "X2" );
    }



    private string HexBytes( ByteBuffer buf, int offset, int count )
    {
      var sb = new StringBuilder();
      for ( int i = 0; i < count; ++i )
      {
        if ( i > 0 )
          sb.Append( ' ' );
        sb.Append( HexByte( buf.ByteAt( offset + i ) ) );
      }
      return sb.ToString();
    }



    private string GenerateExportLog( ByteBuffer buf, ushort baseAddr, string targetPath,
                                       bool exportMarkers, bool exportColors, bool exportPassable,
                                       RetroDevStudio.Formats.MapProject project )
    {
      int markerStride = buf.ByteAt( 0 );
      int tileCount = buf.ByteAt( 1 );
      int mapCount = buf.ByteAt( 2 );
      int entityStride = buf.ByteAt( 0x2D );
      int mapStringCount = buf.ByteAt( 0x34 );

      var sb = new StringBuilder();
      sb.AppendLine( "Exported " + buf.Length + " bytes to " + targetPath );
      sb.AppendLine();

      // Always include the header-constant definitions near the top so the .def
      // serves as a complete reference for the accompanying .bin (no need to
      // cross-reference map_header.asm).
      sb.AppendLine( RetroDevStudio.Formats.MapProject.GenerateGameBinaryHeaderAsm() );

      // --- HEADER ---
      sb.AppendLine( "--- HEADER (57 bytes) ---" );
      sb.AppendLine( Addr( baseAddr, 0x00 ) + ": " + HexByte( buf.ByteAt( 0 ) ).PadRight( 24 ) + "marker_stride = " + markerStride );
      sb.AppendLine( Addr( baseAddr, 0x01 ) + ": " + HexByte( buf.ByteAt( 1 ) ).PadRight( 24 ) + "tile_count = " + tileCount );
      sb.AppendLine( Addr( baseAddr, 0x02 ) + ": " + HexByte( buf.ByteAt( 2 ) ).PadRight( 24 ) + "map_count = " + mapCount );

      string[] hdrNames = {
        "offset_tiles_width",
        "offset_tiles_height",
        "offset_tiles_flags",
        "offset_tile_char_offset_lo",
        "offset_tile_char_offset_hi",
        "offset_tile_color_offset_lo",
        "offset_tile_color_offset_hi",
        "offset_map_width",
        "offset_map_height",
        "offset_map_bg_color",
        "offset_map_mc1_color",
        "offset_map_mc2_color",
        "offset_map_marker_count",
        "offset_map_char_grid_lo",
        "offset_map_char_grid_hi",
        "offset_map_color_grid_lo",
        "offset_map_color_grid_hi",
        "offset_map_passable_lo",
        "offset_map_passable_hi",
        "offset_map_markers_lo",
        "offset_map_markers_hi"
      };

      for ( int i = 0; i < 21; ++i )
      {
        int hdrOff = 0x03 + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + hdrNames[i] + resolved );
      }

      // Entity section (v23+) — one stride byte followed by three 2-byte
      // offset pointers, mirroring the marker section layout. Always printed
      // so the .def dump reflects the full header even when a map has no
      // entity types defined yet.
      sb.AppendLine( Addr( baseAddr, 0x2D ) + ": " + HexByte( buf.ByteAt( 0x2D ) ).PadRight( 24 ) + "entity_stride = " + entityStride );
      string[] entHdrNames = {
        "offset_map_entity_count",
        "offset_map_entities_lo",
        "offset_map_entities_hi",
      };
      for ( int i = 0; i < 3; ++i )
      {
        int hdrOff = 0x2E + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + entHdrNames[i] + resolved );
      }
      // Map-strings section (v24+) — count byte + LO/HI table pointers.
      sb.AppendLine( Addr( baseAddr, 0x34 ) + ": " + HexByte( buf.ByteAt( 0x34 ) ).PadRight( 24 ) + "map_string_count = " + mapStringCount );
      string[] mapStringHdrNames = {
        "offset_map_string_lo",
        "offset_map_string_hi",
      };
      for ( int i = 0; i < 2; ++i )
      {
        int hdrOff = 0x35 + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + mapStringHdrNames[i] + resolved );
      }
      sb.AppendLine();

      // All offsets stored in the file include baseAddr when absolute mode is on.
      // To read file data we subtract baseAddr; to display addresses we use the raw value.
      int ba = baseAddr;

      // --- TILE ARRAYS ---
      sb.AppendLine( "--- TILE ARRAYS ---" );

      AppendArraySection( sb, buf, ba, 0x03, tileCount, "tiles_width", 1 );
      AppendArraySection( sb, buf, ba, 0x05, tileCount, "tiles_height", 1 );
      AppendArraySection( sb, buf, ba, 0x07, tileCount, "tiles_flags", 1 );
      AppendArraySection( sb, buf, ba, 0x09, tileCount, "tile_char_offset_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x0B, tileCount, "tile_char_offset_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x0D, tileCount, "tile_color_offset_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x0F, tileCount, "tile_color_offset_hi", 1 );

      // Tile char/color data (per tile, using offsets from offset tables)
      if ( tileCount > 0 )
      {
        int charOffLoFilePos = buf.UInt16At( 0x09 ) - ba;
        int charOffHiFilePos = buf.UInt16At( 0x0B ) - ba;
        int colorOffLoFilePos = buf.UInt16At( 0x0D ) - ba;
        int colorOffHiFilePos = buf.UInt16At( 0x0F ) - ba;
        int widthFilePos = buf.UInt16At( 0x03 ) - ba;
        int heightFilePos = buf.UInt16At( 0x05 ) - ba;

        sb.AppendLine();
        sb.AppendLine( "--- TILE CHAR DATA ---" );
        for ( int t = 0; t < tileCount; ++t )
        {
          int addr = buf.ByteAt( charOffLoFilePos + t ) | ( buf.ByteAt( charOffHiFilePos + t ) << 8 );
          int filePos = addr - ba;
          int tw = buf.ByteAt( widthFilePos + t );
          int th = buf.ByteAt( heightFilePos + t );
          int size = tw * th;
          sb.AppendLine( "$" + addr.ToString( "X4" ) + ": tile " + t + " char data (" + tw + "x" + th + " = " + size + " bytes)  " + HexBytes( buf, filePos, Math.Min( size, 16 ) ) + ( size > 16 ? " ..." : "" ) );
        }

        sb.AppendLine();
        sb.AppendLine( "--- TILE COLOR DATA ---" );
        for ( int t = 0; t < tileCount; ++t )
        {
          int addr = buf.ByteAt( colorOffLoFilePos + t ) | ( buf.ByteAt( colorOffHiFilePos + t ) << 8 );
          int filePos = addr - ba;
          int tw = buf.ByteAt( widthFilePos + t );
          int th = buf.ByteAt( heightFilePos + t );
          int size = tw * th;
          sb.AppendLine( "$" + addr.ToString( "X4" ) + ": tile " + t + " color data (" + tw + "x" + th + " = " + size + " bytes)  " + HexBytes( buf, filePos, Math.Min( size, 16 ) ) + ( size > 16 ? " ..." : "" ) );
        }
      }
      sb.AppendLine();

      // --- MAP METADATA ARRAYS ---
      sb.AppendLine( "--- MAP METADATA ---" );
      AppendArraySection( sb, buf, ba, 0x11, mapCount, "map_width", 1 );
      AppendArraySection( sb, buf, ba, 0x13, mapCount, "map_height", 1 );
      AppendArraySection( sb, buf, ba, 0x15, mapCount, "map_bg_color", 1 );
      AppendArraySection( sb, buf, ba, 0x17, mapCount, "map_mc1_color", 1 );
      AppendArraySection( sb, buf, ba, 0x19, mapCount, "map_mc2_color", 1 );
      AppendArraySection( sb, buf, ba, 0x1B, mapCount, "map_marker_count", 1 );
      sb.AppendLine();

      // --- MAP DATA LOOKUP TABLES ---
      sb.AppendLine( "--- MAP DATA LOOKUP TABLES ---" );
      AppendArraySection( sb, buf, ba, 0x1D, mapCount, "map_char_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x1F, mapCount, "map_char_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x21, mapCount, "map_color_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x23, mapCount, "map_color_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x25, mapCount, "map_passable_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x27, mapCount, "map_passable_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x29, mapCount, "map_markers_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x2B, mapCount, "map_markers_hi", 1 );
      // Entity lookup tables — AppendArraySection quietly skips any whose
      // header pointer is zero, so these just drop out cleanly when a
      // project has no entities.
      AppendArraySection( sb, buf, ba, 0x2E, mapCount, "map_entity_count", 1 );
      AppendArraySection( sb, buf, ba, 0x30, mapCount, "map_entities_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x32, mapCount, "map_entities_hi", 1 );
      sb.AppendLine();

      // --- PER-MAP VARIABLE DATA ---
      if ( mapCount > 0 )
      {
        int charGridLoFilePos  = buf.UInt16At( 0x1D ) - ba;
        int charGridHiFilePos  = buf.UInt16At( 0x1F ) - ba;
        int colorGridLoFilePos = buf.UInt16At( 0x21 ) - ba;
        int colorGridHiFilePos = buf.UInt16At( 0x23 ) - ba;
        int passableLoFilePos  = buf.UInt16At( 0x25 ) - ba;
        int passableHiFilePos  = buf.UInt16At( 0x27 ) - ba;
        int markersLoFilePos   = buf.UInt16At( 0x29 ) - ba;
        int markersHiFilePos   = buf.UInt16At( 0x2B ) - ba;
        int mapWidthFilePos    = buf.UInt16At( 0x11 ) - ba;
        int mapHeightFilePos   = buf.UInt16At( 0x13 ) - ba;
        int markerCountFilePos = buf.UInt16At( 0x1B ) - ba;
        // Entity lookup arrays — zero offset means the project has no
        // entities, in which case we skip every entity-related read below
        // via the per-map address check.
        ushort entityCountAddr = buf.UInt16At( 0x2E );
        ushort entitiesLoAddr  = buf.UInt16At( 0x30 );
        ushort entitiesHiAddr  = buf.UInt16At( 0x32 );
        int entityCountFilePos = ( entityCountAddr != 0 ) ? entityCountAddr - ba : -1;
        int entitiesLoFilePos  = ( entitiesLoAddr  != 0 ) ? entitiesLoAddr  - ba : -1;
        int entitiesHiFilePos  = ( entitiesHiAddr  != 0 ) ? entitiesHiAddr  - ba : -1;

        for ( int m = 0; m < mapCount; ++m )
        {
          sb.AppendLine( "--- MAP " + m + " DATA ---" );
          int mw = buf.ByteAt( mapWidthFilePos + m );
          int mh = buf.ByteAt( mapHeightFilePos + m );
          int gridSize = mw * mh;

          // Char grid
          int charGridAddr = buf.ByteAt( charGridLoFilePos + m ) | ( buf.ByteAt( charGridHiFilePos + m ) << 8 );
          sb.AppendLine( "$" + charGridAddr.ToString( "X4" ) + ": char grid (" + mw + "x" + mh + " = " + gridSize + " bytes)" );

          // Color grid
          if ( exportColors )
          {
            int colorGridAddr = buf.ByteAt( colorGridLoFilePos + m ) | ( buf.ByteAt( colorGridHiFilePos + m ) << 8 );
            if ( colorGridAddr != 0 )
              sb.AppendLine( "$" + colorGridAddr.ToString( "X4" ) + ": color grid (" + mw + "x" + mh + " = " + gridSize + " bytes)" );
          }

          // Passable bits
          if ( exportPassable )
          {
            int passableAddr = buf.ByteAt( passableLoFilePos + m ) | ( buf.ByteAt( passableHiFilePos + m ) << 8 );
            if ( passableAddr != 0 )
            {
              int passableSize = ( ( mw + 7 ) / 8 ) * mh;
              sb.AppendLine( "$" + passableAddr.ToString( "X4" ) + ": passable bits (" + passableSize + " bytes, " + ( ( mw + 7 ) / 8 ) + " bytes/row x " + mh + " rows)" );
            }
          }

          // Markers
          if ( exportMarkers )
          {
            int markersAddr = buf.ByteAt( markersLoFilePos + m ) | ( buf.ByteAt( markersHiFilePos + m ) << 8 );
            int mc = buf.ByteAt( markerCountFilePos + m );
            if ( markersAddr != 0 && mc > 0 )
            {
              sb.AppendLine( "$" + markersAddr.ToString( "X4" ) + ": markers (" + mc + " x " + markerStride + " bytes = " + ( mc * markerStride ) + " bytes)" );
              for ( int mk = 0; mk < mc; ++mk )
              {
                int mAddr = markersAddr + mk * markerStride;
                int mFilePos = mAddr - ba;
                // Current layout (stride 7): tag, x, y, value1, value2, enabled, triggered
                string line = "  $" + mAddr.ToString( "X4" ) + ": tag=" + HexByte( buf.ByteAt( mFilePos ) )
                            + " x=" + buf.ByteAt( mFilePos + 1 )
                            + " y=" + buf.ByteAt( mFilePos + 2 );
                if ( markerStride >= 4 )
                  line += " value1=" + HexByte( buf.ByteAt( mFilePos + 3 ) );
                if ( markerStride >= 5 )
                  line += " value2=" + HexByte( buf.ByteAt( mFilePos + 4 ) );
                if ( markerStride >= 6 )
                {
                  byte fb = buf.ByteAt( mFilePos + 5 );
                  // BIT_FLAGS preview: %hhhh_llll, matching the mask
                  // constants emitted in the asm sidecar.
                  line += " flags=%"
                          + System.Convert.ToString( ( fb >> 4 ) & 0xF, 2 ).PadLeft( 4, '0' )
                          + "_"
                          + System.Convert.ToString( fb & 0xF, 2 ).PadLeft( 4, '0' );
                }
                if ( markerStride >= 7 )
                  line += " group=" + buf.ByteAt( mFilePos + 6 );
                sb.AppendLine( line );
              }
            }
          }

          // Entities — mirrors the marker dump above. The count, lo, and hi
          // header pointers may all be zero when the project has no
          // entities, in which case entityCountFilePos / entitiesLoFilePos /
          // entitiesHiFilePos are -1 and we skip entirely.
          if ( ( entityCountFilePos >= 0 )
          &&   ( entitiesLoFilePos  >= 0 )
          &&   ( entitiesHiFilePos  >= 0 ) )
          {
            int entitiesAddr = buf.ByteAt( entitiesLoFilePos + m ) | ( buf.ByteAt( entitiesHiFilePos + m ) << 8 );
            int ec = buf.ByteAt( entityCountFilePos + m );
            if ( entitiesAddr != 0 && ec > 0 )
            {
              sb.AppendLine( "$" + entitiesAddr.ToString( "X4" ) + ": entities (" + ec + " x " + entityStride + " bytes = " + ( ec * entityStride ) + " bytes)" );
              for ( int ek = 0; ek < ec; ++ek )
              {
                int eAddr = entitiesAddr + ek * entityStride;
                int eFilePos = eAddr - ba;
                // Current layout (stride 8): tag, x, y, tile, value1, value2, enabled, triggered
                string line = "  $" + eAddr.ToString( "X4" ) + ": tag=" + HexByte( buf.ByteAt( eFilePos ) )
                            + " x=" + buf.ByteAt( eFilePos + 1 )
                            + " y=" + buf.ByteAt( eFilePos + 2 );
                if ( entityStride >= 4 )
                  line += " tile=" + buf.ByteAt( eFilePos + 3 );
                if ( entityStride >= 5 )
                  line += " value1=" + HexByte( buf.ByteAt( eFilePos + 4 ) );
                if ( entityStride >= 6 )
                  line += " value2=" + HexByte( buf.ByteAt( eFilePos + 5 ) );
                if ( entityStride >= 7 )
                  line += " enabled=" + buf.ByteAt( eFilePos + 6 );
                if ( entityStride >= 8 )
                  line += " triggered=" + buf.ByteAt( eFilePos + 7 );
                sb.AppendLine( line );
              }
            }
          }
          sb.AppendLine();
        }
      }

      // --- MAP STRINGS (v24+) — pointer tables + per-string byte streams. ---
      if ( mapStringCount > 0 )
      {
        ushort mapStringLoAddr = buf.UInt16At( 0x35 );
        ushort mapStringHiAddr = buf.UInt16At( 0x37 );
        if ( mapStringLoAddr != 0 && mapStringHiAddr != 0 )
        {
          int loFilePos = mapStringLoAddr - ba;
          int hiFilePos = mapStringHiAddr - ba;

          sb.AppendLine( "--- MAP STRING POINTER TABLES ---" );
          sb.AppendLine( "$" + mapStringLoAddr.ToString( "X4" ) + ": map_string_lo[" + mapStringCount + "]"
                       + "  " + HexBytes( buf, loFilePos, Math.Min( mapStringCount, 24 ) )
                       + ( mapStringCount > 24 ? " ..." : "" ) );
          sb.AppendLine( "$" + mapStringHiAddr.ToString( "X4" ) + ": map_string_hi[" + mapStringCount + "]"
                       + "  " + HexBytes( buf, hiFilePos, Math.Min( mapStringCount, 24 ) )
                       + ( mapStringCount > 24 ? " ..." : "" ) );
          sb.AppendLine();

          sb.AppendLine( "--- MAP STRING DATA ---" );
          // Prefer using the in-memory project for label info — the binary
          // doesn't carry labels. Falls back to "string N" if the project
          // reference isn't reachable here.
          var emittedLabels = new List<string>();
          if ( project != null )
          {
            List<string> skipped;
            var emittedStrings = project.GetEmittableMapStrings( out skipped );
            for ( int i = 0; i < emittedStrings.Count; ++i )
            {
              emittedLabels.Add( emittedStrings[i].Label );
            }
          }

          // Read the per-project charset offsets so we can decode screen
          // codes back to ASCII for human reading. Without these, lowercase
          // bytes in the user's UICharset would render as opaque hex.
          int lowerStart   = ( project != null ) ? project.MapStringsLowercaseIndex : 1;
          int upperStart   = ( project != null ) ? project.MapStringsUppercaseIndex : 1;
          int numbersStart = ( project != null ) ? project.MapStringsNumbersIndex   : 48;

          for ( int i = 0; i < mapStringCount; ++i )
          {
            ushort streamAddr = (ushort)( buf.ByteAt( loFilePos + i ) | ( buf.ByteAt( hiFilePos + i ) << 8 ) );
            int streamFilePos = streamAddr - ba;
            string lbl = ( i < emittedLabels.Count ) ? emittedLabels[i] : ( "string " + i );

            // Walk to find the end of this stream — first $FF (END_OF_TEXT).
            int len = 0;
            while ( streamFilePos + len < buf.Length )
            {
              byte b = buf.ByteAt( streamFilePos + len );
              ++len;
              if ( b == 0xFF ) break;
            }

            sb.AppendLine( "$" + streamAddr.ToString( "X4" ) + ": " + lbl + " (index " + i + ", " + len + " bytes)" );

            // State machine matching Dreadhold's game_message.asm renderer:
            //   AT_LINE_START — reads bytes looking for the line color
            //     ($00..$1F). Recognises $FB/$FD/$FF; anything else >= $20
            //     is silently skipped (the runtime drops it) — surfaced
            //     here as "skipped" so the user can see why a stray byte
            //     ahead of a color marker isn't rendering.
            //   IN_LINE       — every byte is a screen code written to
            //     screen RAM, EXCEPT $FF (end), $FD (end-of-line),
            //     $FC (press-fire). Bytes $01..$1F here are CHARACTERS,
            //     not colors.
            bool atLineStart = true;
            int p = 0;
            var pendingText = new StringBuilder();
            int textStart = 0;
            System.Action flush = delegate ()
            {
              if ( pendingText.Length == 0 ) return;
              sb.AppendLine( "  $" + ( (ushort)( streamAddr + textStart ) ).ToString( "X4" )
                           + ": text \"" + pendingText.ToString() + "\" (" + pendingText.Length + " bytes)" );
              pendingText.Length = 0;
            };

            while ( p < len )
            {
              byte b = buf.ByteAt( streamFilePos + p );
              if ( atLineStart )
              {
                if ( b == 0xFF )
                {
                  flush();
                  sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FF END_OF_TEXT" );
                  ++p;
                  break;
                }
                if ( b == 0xFB )
                {
                  flush();
                  sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FB CLEAR_TEXT_AREA" );
                  ++p;
                  continue;
                }
                if ( b == 0xFD )
                {
                  flush();
                  sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FD END_OF_LINE  (blank line)" );
                  ++p;
                  continue;
                }
                if ( b < 0x20 )
                {
                  flush();
                  sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": " + HexByte( b ) + " line color = " + b );
                  atLineStart = false;
                  ++p;
                  continue;
                }
                // Byte >= $20 at line start — runtime skips it without rendering.
                flush();
                sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": " + HexByte( b ) + " skipped (no line color set yet)" );
                ++p;
                continue;
              }

              // IN_LINE
              if ( b == 0xFF )
              {
                flush();
                sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FF END_OF_TEXT" );
                ++p;
                break;
              }
              if ( b == 0xFD )
              {
                flush();
                sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FD END_OF_LINE" );
                atLineStart = true;
                ++p;
                continue;
              }
              if ( b == 0xFC )
              {
                flush();
                sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FC PRESS_FIRE" );
                atLineStart = true;
                ++p;
                continue;
              }
              // Screen code — accumulate. Translate back to ASCII via the
              // user's per-project charset offsets so the run is readable.
              if ( pendingText.Length == 0 ) textStart = p;
              pendingText.Append( ScreenCodeToReadableChar( b, lowerStart, upperStart, numbersStart ) );
              ++p;
            }
            flush();
          }
          sb.AppendLine();
        }
      }

      sb.AppendLine( "--- END (total " + buf.Length + " bytes, " + Addr( baseAddr, 0 ) + " - " + Addr( baseAddr, (int)buf.Length - 1 ) + ") ---" );
      return sb.ToString();
    }



    /// <summary>
    /// Map a screen-code byte back to a readable character for the
    /// human-readable layout dump. Inverts <c>EmitMapStringTextChar</c>:
    /// the per-project lowercase/uppercase/numbers offsets locate the
    /// letter and digit blocks, and the fixed C64 punctuation positions
    /// cover the rest. Bytes that don't match any known mapping fall
    /// through to <c>?</c> so the user can see something happened
    /// without the dump getting cluttered with raw hex.
    /// </summary>
    private static string ScreenCodeToReadableChar( byte b, int LowerStart, int UpperStart, int NumbersStart )
    {
      if ( LowerStart   <= b && b < LowerStart   + 26 ) return ( (char)( 'a' + b - LowerStart ) ).ToString();
      if ( UpperStart   <= b && b < UpperStart   + 26 ) return ( (char)( 'A' + b - UpperStart ) ).ToString();
      if ( NumbersStart <= b && b < NumbersStart + 10 ) return ( (char)( '0' + b - NumbersStart ) ).ToString();
      switch ( b )
      {
        case 0x20: return " ";
        case 0x21: return "!";
        case 0x22: return "\"";
        case 0x23: return "#";
        case 0x24: return "$";
        case 0x25: return "%";
        case 0x26: return "&";
        case 0x27: return "'";
        case 0x28: return "(";
        case 0x29: return ")";
        case 0x2A: return "*";
        case 0x2B: return "+";
        case 0x2C: return ",";
        case 0x2D: return "-";
        case 0x2E: return ".";
        case 0x2F: return "/";
        case 0x3A: return ":";
        case 0x3B: return ";";
        case 0x3C: return "<";
        case 0x3D: return "=";
        case 0x3E: return ">";
        case 0x3F: return "?";
        case 0x00: return "@";
        case 0x1B: return "[";
        case 0x1D: return "]";
      }
      // Unknown screen code — show as $XX so the user can spot it.
      return "$" + b.ToString( "X2" );
    }



    private void AppendArraySection( StringBuilder sb, ByteBuffer buf, int ba, int hdrField, int count, string name, int elemSize )
    {
      int addr = buf.UInt16At( hdrField );
      if ( addr == 0 )
        return;
      int filePos = addr - ba;
      int totalSize = count * elemSize;
      sb.AppendLine( "$" + addr.ToString( "X4" ) + ": " + name + "[" + count + "]".PadRight( 8 ) + HexBytes( buf, filePos, Math.Min( totalSize, 24 ) ) + ( totalSize > 24 ? " ..." : "" ) );
    }



    private void checkAbsoluteBaseAddress_CheckedChanged( object sender, EventArgs e )
    {
      editAbsoluteBaseAddress.Enabled = checkAbsoluteBaseAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkPrefixLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkSaveOnExport_CheckedChanged( object sender, EventArgs e )
    {
      editExportDirectory.Enabled = checkSaveOnExport.Checked;
      btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;
      editExportFilename.Enabled = checkSaveOnExport.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseExportDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select export directory";
        if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
        {
          dlg.SelectedPath = editExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editExportDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void btnBrowseCharsetExportDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select character set export directory";
        if ( !string.IsNullOrEmpty( editCharsetExportDirectory.Text ) )
        {
          dlg.SelectedPath = editCharsetExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editCharsetExportDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void checkExportCharset_CheckedChanged( object sender, EventArgs e )
    {
      editCharsetExportDirectory.Enabled = checkExportCharset.Checked;
      btnBrowseCharsetExportDirectory.Enabled = checkExportCharset.Checked;
      editCharsetExportFilename.Enabled = checkExportCharset.Checked;

      // Populate sensible defaults when first enabled
      if ( ( checkExportCharset.Checked )
      &&   ( Core != null )
      &&   ( Core.MainForm != null )
      &&   ( Core.MainForm.ActiveDocument != null )
      &&   ( Core.MainForm.ActiveDocument.DocumentInfo != null ) )
      {
        string docPath = Core.MainForm.ActiveDocument.DocumentInfo.DocumentFilename;
        if ( !string.IsNullOrEmpty( docPath ) )
        {
          if ( string.IsNullOrEmpty( editCharsetExportDirectory.Text ) )
          {
            try { editCharsetExportDirectory.Text = System.IO.Path.GetDirectoryName( docPath ); }
            catch ( Exception ) { }
          }
          if ( string.IsNullOrEmpty( editCharsetExportFilename.Text ) )
          {
            try { editCharsetExportFilename.Text = System.IO.Path.GetFileNameWithoutExtension( docPath ) + ".bin"; }
            catch ( Exception ) { }
          }
        }
      }

      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkCharsetPrefixLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editCharsetPrefixLoadAddress.Enabled = checkCharsetPrefixLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkGenerateDefFile_CheckedChanged( object sender, EventArgs e )
    {
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkExportHeaderAsm_CheckedChanged( object sender, EventArgs e )
    {
      editHeaderAsmDirectory.Enabled = checkExportHeaderAsm.Checked;
      btnBrowseHeaderAsmDirectory.Enabled = checkExportHeaderAsm.Checked;
      editHeaderAsmFilename.Enabled = checkExportHeaderAsm.Checked;
      editHeaderAsmPrefix.Enabled = checkExportHeaderAsm.Checked;

      // Pre-populate the directory from the binary's auto-save directory when first
      // enabling the checkbox — that's usually what the user wants.
      if ( ( checkExportHeaderAsm.Checked )
      &&   ( string.IsNullOrEmpty( editHeaderAsmDirectory.Text ) )
      &&   ( !string.IsNullOrEmpty( editExportDirectory.Text ) ) )
      {
        editHeaderAsmDirectory.Text = editExportDirectory.Text;
      }
      if ( ( checkExportHeaderAsm.Checked )
      &&   ( string.IsNullOrEmpty( editHeaderAsmFilename.Text ) ) )
      {
        editHeaderAsmFilename.Text = "map_header.asm";
      }

      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseHeaderAsmDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select directory for map_header.asm";
        if ( !string.IsNullOrEmpty( editHeaderAsmDirectory.Text ) )
        {
          dlg.SelectedPath = editHeaderAsmDirectory.Text;
        }
        else if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
        {
          dlg.SelectedPath = editExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editHeaderAsmDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void checkExportMarkerLabels_CheckedChanged( object sender, EventArgs e )
    {
      editMarkerLabelsDirectory.Enabled = checkExportMarkerLabels.Checked;
      btnBrowseMarkerLabelsDirectory.Enabled = checkExportMarkerLabels.Checked;
      editMarkerLabelsFilename.Enabled = checkExportMarkerLabels.Checked;
      editMarkerLabelsPrefix.Enabled = checkExportMarkerLabels.Checked;

      if ( ( checkExportMarkerLabels.Checked )
      &&   ( string.IsNullOrEmpty( editMarkerLabelsDirectory.Text ) )
      &&   ( !string.IsNullOrEmpty( editExportDirectory.Text ) ) )
      {
        editMarkerLabelsDirectory.Text = editExportDirectory.Text;
      }
      if ( ( checkExportMarkerLabels.Checked )
      &&   ( string.IsNullOrEmpty( editMarkerLabelsFilename.Text ) ) )
      {
        editMarkerLabelsFilename.Text = "map_markers.asm";
      }

      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseMarkerLabelsDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select directory for marker labels sidecar";
        if ( !string.IsNullOrEmpty( editMarkerLabelsDirectory.Text ) )
        {
          dlg.SelectedPath = editMarkerLabelsDirectory.Text;
        }
        else if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
        {
          dlg.SelectedPath = editExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editMarkerLabelsDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void checkExportEntityLabels_CheckedChanged( object sender, EventArgs e )
    {
      editEntityLabelsDirectory.Enabled = checkExportEntityLabels.Checked;
      btnBrowseEntityLabelsDirectory.Enabled = checkExportEntityLabels.Checked;
      editEntityLabelsFilename.Enabled = checkExportEntityLabels.Checked;
      editEntityLabelsPrefix.Enabled = checkExportEntityLabels.Checked;

      if ( ( checkExportEntityLabels.Checked )
      &&   ( string.IsNullOrEmpty( editEntityLabelsDirectory.Text ) )
      &&   ( !string.IsNullOrEmpty( editExportDirectory.Text ) ) )
      {
        editEntityLabelsDirectory.Text = editExportDirectory.Text;
      }
      if ( ( checkExportEntityLabels.Checked )
      &&   ( string.IsNullOrEmpty( editEntityLabelsFilename.Text ) ) )
      {
        editEntityLabelsFilename.Text = "map_entities.asm";
      }

      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseEntityLabelsDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select directory for entity labels sidecar";
        if ( !string.IsNullOrEmpty( editEntityLabelsDirectory.Text ) )
        {
          dlg.SelectedPath = editEntityLabelsDirectory.Text;
        }
        else if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
        {
          dlg.SelectedPath = editExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editEntityLabelsDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void checkExportMapStrings_CheckedChanged( object sender, EventArgs e )
    {
      editMapStringsDirectory.Enabled = checkExportMapStrings.Checked;
      btnBrowseMapStringsDirectory.Enabled = checkExportMapStrings.Checked;
      editMapStringsFilename.Enabled = checkExportMapStrings.Checked;
      editMapStringsPrefix.Enabled = checkExportMapStrings.Checked;

      if ( ( checkExportMapStrings.Checked )
      &&   ( string.IsNullOrEmpty( editMapStringsDirectory.Text ) )
      &&   ( !string.IsNullOrEmpty( editExportDirectory.Text ) ) )
      {
        editMapStringsDirectory.Text = editExportDirectory.Text;
      }
      if ( ( checkExportMapStrings.Checked )
      &&   ( string.IsNullOrEmpty( editMapStringsFilename.Text ) ) )
      {
        editMapStringsFilename.Text = "map_strings.asm";
      }
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseMapStringsDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select directory for map strings sidecar";
        if ( !string.IsNullOrEmpty( editMapStringsDirectory.Text ) )
        {
          dlg.SelectedPath = editMapStringsDirectory.Text;
        }
        else if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
        {
          dlg.SelectedPath = editExportDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editMapStringsDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void HandleSettingsChanged( object sender, EventArgs e )
    {
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    public override void ApplyExportSettings( MapProject.ExportSettings Settings )
    {
      if ( Settings == null )
      {
        return;
      }
      m_ApplyingSettings = true;
      try
      {
        var s = Settings.GameBinary;
        checkExportMarkers.Checked = s.ExportMarkers;
        checkExportColors.Checked = s.ExportColors;
        checkExportPassable.Checked = s.ExportPassableBits;
        checkAbsoluteBaseAddress.Checked = s.UseAbsoluteAddresses;
        editAbsoluteBaseAddress.Text = s.AbsoluteBaseAddressHex ?? "";
        editAbsoluteBaseAddress.Enabled = checkAbsoluteBaseAddress.Checked;
        checkPrefixLoadAddress.Checked = s.PrefixLoadAddress;
        editPrefixLoadAddress.Text = s.PrefixLoadAddressHex ?? "";
        editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
        checkSaveOnExport.Checked = s.SaveOnExport;
        editExportDirectory.Text = s.ExportDirectory ?? "";
        editExportFilename.Text = s.ExportFilename ?? "";
        editExportDirectory.Enabled = checkSaveOnExport.Checked;
        btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;
        editExportFilename.Enabled = checkSaveOnExport.Checked;

        checkGenerateDefFile.Checked = s.GenerateDefFile;

        checkExportHeaderAsm.Checked = s.ExportHeaderAsm;
        editHeaderAsmDirectory.Text = s.HeaderAsmDirectory ?? "";
        editHeaderAsmFilename.Text = string.IsNullOrEmpty( s.HeaderAsmFilename ) ? "map_header.asm" : s.HeaderAsmFilename;
        editHeaderAsmPrefix.Text = s.HeaderAsmPrefix ?? "";
        editHeaderAsmDirectory.Enabled = checkExportHeaderAsm.Checked;
        btnBrowseHeaderAsmDirectory.Enabled = checkExportHeaderAsm.Checked;
        editHeaderAsmFilename.Enabled = checkExportHeaderAsm.Checked;
        editHeaderAsmPrefix.Enabled = checkExportHeaderAsm.Checked;

        checkExportMarkerLabels.Checked = s.ExportMarkerLabels;
        editMarkerLabelsDirectory.Text = s.MarkerLabelsDirectory ?? "";
        editMarkerLabelsFilename.Text = string.IsNullOrEmpty( s.MarkerLabelsFilename ) ? "map_markers.asm" : s.MarkerLabelsFilename;
        editMarkerLabelsPrefix.Text = s.MarkerLabelsPrefix ?? "";
        editMarkerLabelsDirectory.Enabled = checkExportMarkerLabels.Checked;
        btnBrowseMarkerLabelsDirectory.Enabled = checkExportMarkerLabels.Checked;
        editMarkerLabelsFilename.Enabled = checkExportMarkerLabels.Checked;
        editMarkerLabelsPrefix.Enabled = checkExportMarkerLabels.Checked;

        checkExportEntityLabels.Checked = s.ExportEntityLabels;
        editEntityLabelsDirectory.Text = s.EntityLabelsDirectory ?? "";
        editEntityLabelsFilename.Text = string.IsNullOrEmpty( s.EntityLabelsFilename ) ? "map_entities.asm" : s.EntityLabelsFilename;
        editEntityLabelsPrefix.Text = s.EntityLabelsPrefix ?? "";
        editEntityLabelsDirectory.Enabled = checkExportEntityLabels.Checked;
        btnBrowseEntityLabelsDirectory.Enabled = checkExportEntityLabels.Checked;
        editEntityLabelsFilename.Enabled = checkExportEntityLabels.Checked;
        editEntityLabelsPrefix.Enabled = checkExportEntityLabels.Checked;

        checkExportMapStrings.Checked = s.ExportMapStrings;
        editMapStringsDirectory.Text = s.MapStringsDirectory ?? "";
        editMapStringsFilename.Text = string.IsNullOrEmpty( s.MapStringsFilename ) ? "map_strings.asm" : s.MapStringsFilename;
        editMapStringsPrefix.Text = s.MapStringsPrefix ?? "";
        editMapStringsDirectory.Enabled = checkExportMapStrings.Checked;
        btnBrowseMapStringsDirectory.Enabled = checkExportMapStrings.Checked;
        editMapStringsFilename.Enabled = checkExportMapStrings.Checked;
        editMapStringsPrefix.Enabled = checkExportMapStrings.Checked;

        checkExportCharset.Checked = s.ExportCharset;
        editCharsetExportDirectory.Text = s.CharsetExportDirectory ?? "";
        editCharsetExportFilename.Text = s.CharsetExportFilename ?? "";
        editCharsetExportDirectory.Enabled = checkExportCharset.Checked;
        btnBrowseCharsetExportDirectory.Enabled = checkExportCharset.Checked;
        editCharsetExportFilename.Enabled = checkExportCharset.Checked;

        checkCharsetPrefixLoadAddress.Checked = s.CharsetPrefixLoadAddress;
        editCharsetPrefixLoadAddress.Text = s.CharsetPrefixLoadAddressHex ?? "";
        editCharsetPrefixLoadAddress.Enabled = checkCharsetPrefixLoadAddress.Checked;
      }
      finally
      {
        m_ApplyingSettings = false;
      }
    }



    public override void UpdateExportSettings( MapProject.ExportSettings Settings )
    {
      if ( Settings == null )
      {
        return;
      }
      var s = Settings.GameBinary;
      s.ExportMarkers = checkExportMarkers.Checked;
      s.ExportColors = checkExportColors.Checked;
      s.ExportPassableBits = checkExportPassable.Checked;
      s.UseAbsoluteAddresses = checkAbsoluteBaseAddress.Checked;
      s.AbsoluteBaseAddressHex = editAbsoluteBaseAddress.Text ?? "";
      s.PrefixLoadAddress = checkPrefixLoadAddress.Checked;
      s.PrefixLoadAddressHex = editPrefixLoadAddress.Text ?? "";
      s.SaveOnExport = checkSaveOnExport.Checked;
      s.ExportDirectory = editExportDirectory.Text ?? "";
      s.ExportFilename = editExportFilename.Text ?? "";
      s.GenerateDefFile = checkGenerateDefFile.Checked;
      s.ExportHeaderAsm = checkExportHeaderAsm.Checked;
      s.HeaderAsmDirectory = editHeaderAsmDirectory.Text ?? "";
      s.HeaderAsmFilename = string.IsNullOrEmpty( editHeaderAsmFilename.Text ) ? "map_header.asm" : editHeaderAsmFilename.Text;
      s.HeaderAsmPrefix = editHeaderAsmPrefix.Text ?? "";
      s.ExportMarkerLabels = checkExportMarkerLabels.Checked;
      s.MarkerLabelsDirectory = editMarkerLabelsDirectory.Text ?? "";
      s.MarkerLabelsFilename = string.IsNullOrEmpty( editMarkerLabelsFilename.Text ) ? "map_markers.asm" : editMarkerLabelsFilename.Text;
      s.MarkerLabelsPrefix = editMarkerLabelsPrefix.Text ?? "";
      s.ExportEntityLabels = checkExportEntityLabels.Checked;
      s.EntityLabelsDirectory = editEntityLabelsDirectory.Text ?? "";
      s.EntityLabelsFilename = string.IsNullOrEmpty( editEntityLabelsFilename.Text ) ? "map_entities.asm" : editEntityLabelsFilename.Text;
      s.EntityLabelsPrefix = editEntityLabelsPrefix.Text ?? "";
      s.ExportMapStrings = checkExportMapStrings.Checked;
      s.MapStringsDirectory = editMapStringsDirectory.Text ?? "";
      s.MapStringsFilename = string.IsNullOrEmpty( editMapStringsFilename.Text ) ? "map_strings.asm" : editMapStringsFilename.Text;
      s.MapStringsPrefix = editMapStringsPrefix.Text ?? "";
      s.ExportCharset = checkExportCharset.Checked;
      s.CharsetExportDirectory = editCharsetExportDirectory.Text ?? "";
      s.CharsetExportFilename = editCharsetExportFilename.Text ?? "";
      s.CharsetPrefixLoadAddress = checkCharsetPrefixLoadAddress.Checked;
      s.CharsetPrefixLoadAddressHex = editCharsetPrefixLoadAddress.Text ?? "";
    }



  }
}
