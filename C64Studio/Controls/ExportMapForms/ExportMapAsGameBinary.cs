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

      GR.IO.File.WriteAllBytes( targetPath, finalData );

      string log = GenerateExportLog( data, baseAddress, targetPath,
                                      exportMarkers, exportColors, exportPassable );

      // Write .def file alongside the binary
      string defPath = System.IO.Path.ChangeExtension( targetPath, ".def" );
      System.IO.File.WriteAllText( defPath, log );

      if ( EditOutput != null )
      {
        EditOutput.Font = new System.Drawing.Font( "Courier New", 8.25f );
        EditOutput.Text = log;
      }
      return true;
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
                                       bool exportMarkers, bool exportColors, bool exportPassable )
    {
      int markerStride = buf.ByteAt( 2 );
      int tileCount = buf.ByteAt( 3 );
      int mapCount = buf.ByteAt( 4 );

      var sb = new StringBuilder();
      sb.AppendLine( "Exported " + buf.Length + " bytes to " + targetPath );
      sb.AppendLine();

      // --- HEADER ---
      sb.AppendLine( "--- HEADER (47 bytes) ---" );
      sb.AppendLine( Addr( baseAddr, 0x00 ) + ": " + HexBytes( buf, 0, 2 ).PadRight( 24 ) + "magic \"DH\"" );
      sb.AppendLine( Addr( baseAddr, 0x02 ) + ": " + HexByte( buf.ByteAt( 2 ) ).PadRight( 24 ) + "marker_stride = " + markerStride );
      sb.AppendLine( Addr( baseAddr, 0x03 ) + ": " + HexByte( buf.ByteAt( 3 ) ).PadRight( 24 ) + "tile_count = " + tileCount );
      sb.AppendLine( Addr( baseAddr, 0x04 ) + ": " + HexByte( buf.ByteAt( 4 ) ).PadRight( 24 ) + "map_count = " + mapCount );

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
        int hdrOff = 0x05 + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + hdrNames[i] + resolved );
      }
      sb.AppendLine();

      // All offsets stored in the file include baseAddr when absolute mode is on.
      // To read file data we subtract baseAddr; to display addresses we use the raw value.
      int ba = baseAddr;

      // --- TILE ARRAYS ---
      sb.AppendLine( "--- TILE ARRAYS ---" );

      AppendArraySection( sb, buf, ba, 0x05, tileCount, "tiles_width", 1 );
      AppendArraySection( sb, buf, ba, 0x07, tileCount, "tiles_height", 1 );
      AppendArraySection( sb, buf, ba, 0x09, tileCount, "tiles_flags", 1 );
      AppendArraySection( sb, buf, ba, 0x0B, tileCount, "tile_char_offset_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x0D, tileCount, "tile_char_offset_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x0F, tileCount, "tile_color_offset_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x11, tileCount, "tile_color_offset_hi", 1 );

      // Tile char/color data (per tile, using offsets from offset tables)
      if ( tileCount > 0 )
      {
        int charOffLoFilePos = buf.UInt16At( 0x0B ) - ba;
        int charOffHiFilePos = buf.UInt16At( 0x0D ) - ba;
        int colorOffLoFilePos = buf.UInt16At( 0x0F ) - ba;
        int colorOffHiFilePos = buf.UInt16At( 0x11 ) - ba;
        int widthFilePos = buf.UInt16At( 0x05 ) - ba;
        int heightFilePos = buf.UInt16At( 0x07 ) - ba;

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
      AppendArraySection( sb, buf, ba, 0x13, mapCount, "map_width", 1 );
      AppendArraySection( sb, buf, ba, 0x15, mapCount, "map_height", 1 );
      AppendArraySection( sb, buf, ba, 0x17, mapCount, "map_bg_color", 1 );
      AppendArraySection( sb, buf, ba, 0x19, mapCount, "map_mc1_color", 1 );
      AppendArraySection( sb, buf, ba, 0x1B, mapCount, "map_mc2_color", 1 );
      AppendArraySection( sb, buf, ba, 0x1D, mapCount, "map_marker_count", 1 );
      sb.AppendLine();

      // --- MAP DATA LOOKUP TABLES ---
      sb.AppendLine( "--- MAP DATA LOOKUP TABLES ---" );
      AppendArraySection( sb, buf, ba, 0x1F, mapCount, "map_char_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x21, mapCount, "map_char_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x23, mapCount, "map_color_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x25, mapCount, "map_color_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x27, mapCount, "map_passable_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x29, mapCount, "map_passable_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x2B, mapCount, "map_markers_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x2D, mapCount, "map_markers_hi", 1 );
      sb.AppendLine();

      // --- PER-MAP VARIABLE DATA ---
      if ( mapCount > 0 )
      {
        int charGridLoFilePos  = buf.UInt16At( 0x1F ) - ba;
        int charGridHiFilePos  = buf.UInt16At( 0x21 ) - ba;
        int colorGridLoFilePos = buf.UInt16At( 0x23 ) - ba;
        int colorGridHiFilePos = buf.UInt16At( 0x25 ) - ba;
        int passableLoFilePos  = buf.UInt16At( 0x27 ) - ba;
        int passableHiFilePos  = buf.UInt16At( 0x29 ) - ba;
        int markersLoFilePos   = buf.UInt16At( 0x2B ) - ba;
        int markersHiFilePos   = buf.UInt16At( 0x2D ) - ba;
        int mapWidthFilePos    = buf.UInt16At( 0x13 ) - ba;
        int mapHeightFilePos   = buf.UInt16At( 0x15 ) - ba;
        int markerCountFilePos = buf.UInt16At( 0x1D ) - ba;

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
                string line = "  $" + mAddr.ToString( "X4" ) + ": tag=" + HexByte( buf.ByteAt( mFilePos ) )
                            + " x=" + buf.ByteAt( mFilePos + 1 )
                            + " y=" + buf.ByteAt( mFilePos + 2 );
                if ( markerStride >= 4 )
                  line += " value=" + HexByte( buf.ByteAt( mFilePos + 3 ) );
                sb.AppendLine( line );
              }
            }
          }
          sb.AppendLine();
        }
      }

      sb.AppendLine( "--- END (total " + buf.Length + " bytes, " + Addr( baseAddr, 0 ) + " - " + Addr( baseAddr, (int)buf.Length - 1 ) + ") ---" );
      return sb.ToString();
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
    }



  }
}
