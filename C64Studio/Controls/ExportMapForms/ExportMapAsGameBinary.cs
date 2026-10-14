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
      editPrefixLoadAddress.TextChanged += HandleSettingsChanged;
      editMaxExportSize.TextChanged += HandleSettingsChanged;
      editCompressDirectory.TextChanged += HandleSettingsChanged;
      editCompressFilename.TextChanged += HandleSettingsChanged;
      editOverrideLoadAddress.TextChanged += HandleSettingsChanged;
      editExportDirectory.TextChanged += HandleSettingsChanged;
      editExportFilename.TextChanged += HandleSettingsChanged;
      checkExportMarkers.CheckedChanged += HandleSettingsChanged;
      checkExportColors.CheckedChanged += HandleSettingsChanged;
      checkExportPassable.CheckedChanged += HandleSettingsChanged;
      editCharsetExportDirectory.TextChanged += HandleSettingsChanged;
      editCharsetPrefixLoadAddress.TextChanged += HandleSettingsChanged;
      editCharsetLabelsFilename.TextChanged += HandleSettingsChanged;
      editCharsetLabelsPrefix.TextChanged += HandleSettingsChanged;
      editMapLabelsFilename.TextChanged += HandleSettingsChanged;
      editMapLabelsPrefix.TextChanged += HandleSettingsChanged;
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
      editCharsetFilenamePattern.TextChanged += HandleSettingsChanged;
      editCharsetOverrideLoadAddress.TextChanged += HandleSettingsChanged;
      editCharsetCompressFilename.TextChanged += HandleSettingsChanged;
    }



    /// <summary>
    /// The game-binary export writes ONE FILE PER MAP: each exported map goes
    /// to its own binary named by the map file name pattern (%d = the map's
    /// exported index, its MAP_INDEX_* value), each with its own charset file
    /// named by the charset file name pattern, numbered the same way. The
    /// sidecars are written once. Nothing is written unless every file can be
    /// built.
    /// </summary>
    public override bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      // Pre-export validation: warn the user about Map String labels that
      // can't form a valid asm identifier. The strings themselves still go
      // into the binary regardless — labels are editor metadata, the
      // binary uses raw indices. The .asm sidecar's .const lines are what
      // get filtered for those entries; the raw-index reference still
      // works in the user's game code.
      if ( Info.Map != null )
      {
        var labelWarnings = Info.Map.GetMapStringLabelWarnings();
        if ( labelWarnings.Count > 0 )
        {
          var sb = new System.Text.StringBuilder();
          sb.AppendLine( "Some Map String labels can't be exported as asm identifiers:" );
          sb.AppendLine();
          foreach ( var w in labelWarnings ) sb.AppendLine( "  " + w );
          sb.AppendLine();
          sb.AppendLine( "Continue anyway?" );
          sb.AppendLine();
          sb.AppendLine( "Yes  — continue the export (unused strings are skipped as usual);" );
          sb.AppendLine( "       the listed labels will not get a .const line in the .asm" );
          sb.AppendLine( "       sidecar (reference them by their raw index in your game code)." );
          sb.AppendLine( "No   — abort the export so you can rename the labels." );

          var result = System.Windows.Forms.MessageBox.Show(
            sb.ToString(),
            "Map String label warnings",
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2 );
          if ( result != System.Windows.Forms.DialogResult.Yes )
          {
            return false;
          }
        }
      }

      // Pre-export validation: placed entities whose type has no usable tile
      // in their map's character set (none there - the record gets $FF - or a
      // number past that set's tiles). Warn, never block - the user decides.
      if ( Info.Map != null )
      {
        var entityWarnings = Info.Map.GetEntityTileExportWarnings();
        if ( entityWarnings.Count > 0 )
        {
          var sb = new System.Text.StringBuilder();
          sb.AppendLine( "Entity tile warnings:" );
          sb.AppendLine();
          foreach ( var w in entityWarnings ) sb.AppendLine( "  " + w );
          sb.AppendLine();
          sb.AppendLine( "Set the tile for that character set on the Entities tab (pick the set, pick the tile, Update)." );
          sb.AppendLine();
          sb.AppendLine( "Continue anyway?" );

          var result = System.Windows.Forms.MessageBox.Show(
            sb.ToString(),
            "Entity tile warnings",
            System.Windows.Forms.MessageBoxButtons.YesNo,
            System.Windows.Forms.MessageBoxIcon.Warning,
            System.Windows.Forms.MessageBoxDefaultButton.Button2 );
          if ( result != System.Windows.Forms.DialogResult.Yes )
          {
            return false;
          }
        }
      }

      bool exportMarkers = checkExportMarkers.Checked;
      bool exportColors = checkExportColors.Checked;
      bool exportPassable = checkExportPassable.Checked;

      // Pre-export validation: a marker whose "Link to ID" points at its own ID
      // (LinkToID == LinkID) is a self-referential trigger link — always a bug
      // (the runtime would re-queue it forever). Abort so it gets fixed rather
      // than shipped. Only relevant when markers are actually being exported.
      if ( exportMarkers && ( Info.Map != null ) )
      {
        var selfLinks = Info.Map.GetMarkerSelfLinkWarnings();
        if ( selfLinks.Count > 0 )
        {
          var sb = new System.Text.StringBuilder();
          sb.AppendLine( "Export aborted: a marker links to its own ID" );
          sb.AppendLine( "(\"Link to ID\" equals the marker's own \"ID\")." );
          sb.AppendLine();
          foreach ( var w in selfLinks ) sb.AppendLine( "  " + w );
          sb.AppendLine();
          sb.AppendLine( "Clear the self-link on the listed marker(s) and export again." );
          System.Windows.Forms.MessageBox.Show(
            sb.ToString(),
            "Marker links to its own ID",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning );
          return false;
        }
      }

      // Build every file first: a bad file name pattern, a set with too many
      // tiles or a map file over 64 KB aborts before anything is written.
      string mapPattern     = editExportFilename.Text ?? "";
      string charsetPattern = editCharsetFilenamePattern.Text ?? "";
      bool   compress         = checkCompressMap.Checked && !string.IsNullOrEmpty( editCompressFilename.Text );
      bool   compressCharsets = checkCompressCharsets.Checked && !string.IsNullOrEmpty( editCharsetCompressFilename.Text );
      var    errors           = new List<string>();
      if ( compress )
      {
        string compressError = RetroDevStudio.Formats.MapProject.ValidateFileNamePattern( editCompressFilename.Text );
        if ( compressError != null )
        {
          errors.Add( "compressed file name: " + compressError );
        }
      }
      if ( compressCharsets )
      {
        string compressError = RetroDevStudio.Formats.MapProject.ValidateFileNamePattern( editCharsetCompressFilename.Text );
        if ( compressError != null )
        {
          errors.Add( "compressed character set file name: " + compressError );
        }
      }
      // The load-address overrides are parsed ONCE, here: a bad one aborts
      // before anything is written instead of failing once per map file.
      int mapOverrideAddress     = -1;
      int charsetOverrideAddress = -1;
      if ( ( compress )
      &&   ( checkOverrideLoadAddress.Checked )
      &&   ( !GameBinaryCompression.TryParseHexAddress( editOverrideLoadAddress.Text, out mapOverrideAddress ) ) )
      {
        errors.Add( "compressed map load address: \"" + ( editOverrideLoadAddress.Text ?? "" ) + "\" is not a valid hex address ($0000-$FFFF)" );
      }
      if ( ( compressCharsets )
      &&   ( checkCharsetOverrideLoadAddress.Checked )
      &&   ( !GameBinaryCompression.TryParseHexAddress( editCharsetOverrideLoadAddress.Text, out charsetOverrideAddress ) ) )
      {
        errors.Add( "compressed character set load address: \"" + ( editCharsetOverrideLoadAddress.Text ?? "" ) + "\" is not a valid hex address ($0000-$FFFF)" );
      }
      List<string> exportErrors;
      var files = Info.Map.ExportAsGameBinaryFiles( mapPattern, charsetPattern, exportMarkers, exportColors, exportPassable, out exportErrors );
      if ( exportErrors != null )
      {
        errors.AddRange( exportErrors );
      }
      // The compressed charset files land beside the charset files, so a
      // pattern yielding the same name would overwrite the original.
      if ( ( compressCharsets )
      &&   ( files != null ) )
      {
        foreach ( var file in files )
        {
          string compressedName;
          string patternError;
          if ( ( file.CharsetData != null )
          &&   ( RetroDevStudio.Formats.MapProject.TryFormatFileNamePattern( editCharsetCompressFilename.Text, file.ExportIndex, out compressedName, out patternError ) )
          &&   ( string.Equals( compressedName, file.CharsetFileName, StringComparison.OrdinalIgnoreCase ) ) )
          {
            errors.Add( "compressed character set file name " + compressedName + " equals the character set file name " + file.CharsetFileName + " - pick a different name" );
            break;
          }
        }
      }
      if ( ( files == null )
      ||   ( errors.Count > 0 ) )
      {
        ReportExportAborted( errors );
        return false;
      }
      if ( files.Count == 0 )
      {
        System.Windows.Forms.MessageBox.Show(
          "No map is exported - every map is flagged 'Map not exported'.",
          "Game binary export",
          System.Windows.Forms.MessageBoxButtons.OK,
          System.Windows.Forms.MessageBoxIcon.Information );
        return false;
      }

      // Where the map files go: the auto-save directory, else ask for a folder.
      string mapDir = null;
      if ( ( checkSaveOnExport.Checked )
      &&   ( !string.IsNullOrEmpty( editExportDirectory.Text ) ) )
      {
        mapDir = editExportDirectory.Text;
      }
      else
      {
        using ( var dlg = new FolderBrowserDialog() )
        {
          dlg.Description = "Folder for the " + files.Count + " map file(s) (" + files[0].FileName + ( ( files.Count > 1 ) ? " ..." : "" ) + ")";
          if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )
          {
            dlg.SelectedPath = editExportDirectory.Text;
          }
          if ( dlg.ShowDialog() != DialogResult.OK )
          {
            return false;
          }
          mapDir = dlg.SelectedPath;
        }
      }
      string firstMapPath = System.IO.Path.Combine( mapDir, files[0].FileName );

      // Charset files (and the label sidecars) go to the charset export
      // directory - blank = the project's folder, like before.
      string charsetDir = ResolveCharsetExportDirectory( editCharsetExportDirectory.Text, DocInfo, firstMapPath );
      ushort charsetLoadAddress = 0;
      bool   charsetPrefix = checkCharsetPrefixLoadAddress.Checked
                          && TryParseCharsetLoadAddress( editCharsetPrefixLoadAddress.Text, out charsetLoadAddress );
      int maxExportSize = 0;
      int.TryParse( editMaxExportSize.Text, out maxExportSize );

      // Every folder this export writes to: the missing ones are offered for
      // creation in one question, before anything is written.
      var folders = new List<string>() { mapDir };
      if ( compress )
      {
        folders.Add( string.IsNullOrEmpty( editCompressDirectory.Text ) ? mapDir : editCompressDirectory.Text );
      }
      if ( ( files[0].CharsetData != null )
      ||   ( checkExportCharsetLabels.Checked )
      ||   ( checkExportMapLabels.Checked ) )
      {
        folders.Add( charsetDir );
      }
      if ( checkExportHeaderAsm.Checked )
      {
        folders.Add( SidecarDirectory( editHeaderAsmDirectory.Text, firstMapPath ) );
      }
      if ( checkExportMarkerLabels.Checked )
      {
        folders.Add( SidecarDirectory( editMarkerLabelsDirectory.Text, firstMapPath ) );
      }
      if ( checkExportEntityLabels.Checked )
      {
        folders.Add( SidecarDirectory( editEntityLabelsDirectory.Text, firstMapPath ) );
      }
      if ( checkExportMapStrings.Checked )
      {
        folders.Add( SidecarDirectory( editMapStringsDirectory.Text, firstMapPath ) );
      }
      if ( !EnsureExportDirectories( folders ) )
      {
        return false;
      }

      var summary   = new StringBuilder();
      var fileLogs  = new StringBuilder();
      var oversized = new List<string>();
      summary.AppendLine( "Exported " + files.Count + " map file(s) to " + mapDir
                        + ( ( files[0].CharsetData != null ) ? ", their charset files" + ( compressCharsets ? " (+ compressed copies)" : "" ) + " to " + charsetDir : " (no charset files - the charset file name is empty)" ) );
      foreach ( var file in files )
      {
        // The map binary (+ the optional 2-byte PRG load address).
        GR.Memory.ByteBuffer finalData = file.Data;
        if ( checkPrefixLoadAddress.Checked )
        {
          ushort address = GR.Convert.ToU16( editPrefixLoadAddress.Text, 16 );
          var addressData = new ByteBuffer();
          addressData.AppendU16( address );
          finalData = addressData + finalData;
        }
        string mapPath = System.IO.Path.Combine( mapDir, file.FileName );
        try
        {
          GR.IO.File.WriteAllBytes( mapPath, finalData );
        }
        catch ( Exception ex )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Error saving file",
              "Could not save the map file:\r\n" + mapPath + "\r\n\r\n" + ex.Message );
          }
          return false;
        }

        // Optional compressed copy - a SECOND file, named by the compressed
        // file name pattern with the same number; the original stays.
        string compressionReport = null;
        if ( compress )
        {
          string compressedName;
          string patternError;
          RetroDevStudio.Formats.MapProject.TryFormatFileNamePattern( editCompressFilename.Text, file.ExportIndex, out compressedName, out patternError );
          TryCompressExport( mapPath, compressedName, finalData.Length, mapOverrideAddress, out compressionReport );
        }

        // The map's charset file, numbered like the map file.
        string charsetReport         = null;
        string compressedCharsetName = null;
        var    charsetNames          = new List<string>();
        string charsetName           = Info.Map.CharsetDisplayNameOf( Info.Map.CharsetOf( file.Map ) );
        if ( file.CharsetData != null )
        {
          GR.Memory.ByteBuffer charsetData = file.CharsetData;
          if ( charsetPrefix )
          {
            var prefixed = new ByteBuffer();
            prefixed.AppendU16( charsetLoadAddress );
            prefixed.Append( charsetData );
            charsetData = prefixed;
          }
          string charsetPath = System.IO.Path.Combine( charsetDir ?? mapDir, file.CharsetFileName );
          try
          {
            GR.IO.File.WriteAllBytes( charsetPath, charsetData );
          }
          catch ( Exception ex )
          {
            if ( Core != null )
            {
              Core.Notification.MessageBox( "Error saving file",
                "Could not save the charset file:\r\n" + charsetPath + "\r\n\r\n" + ex.Message );
            }
            return false;
          }
          charsetReport = "Charset file: " + charsetPath + " (" + charsetData.Length + " bytes, character set '" + charsetName + "')";
          // Optional compressed copy - a SECOND file beside the charset file,
          // named by the compressed charset file name pattern with the same
          // number; the charset file itself stays.
          if ( compressCharsets )
          {
            string compressedName;
            string patternError;
            RetroDevStudio.Formats.MapProject.TryFormatFileNamePattern( editCharsetCompressFilename.Text, file.ExportIndex, out compressedName, out patternError );
            string compressedPath = System.IO.Path.Combine( charsetDir ?? mapDir, compressedName );
            string charsetCompression;
            if ( TryCompressCharsetFile( charsetPath, compressedPath, charsetData.Length, charsetOverrideAddress, out charsetCompression ) )
            {
              compressedCharsetName = compressedName;
              charsetReport += "\r\n" + charsetCompression;
            }
          }
          charsetNames.Add( file.CharsetFileName + " ('" + charsetName + "')" );
        }
        else
        {
          charsetNames.Add( "'" + charsetName + "'" );
        }

        string fileLog = GenerateExportLog( file.Data, mapPath, exportMarkers, exportColors, exportPassable,
                                            Info.Map, file.Map, charsetNames, compressionReport, charsetReport );
        // Optional .def sidecar - one per map file, beside it.
        if ( checkGenerateDefFile.Checked )
        {
          string defPath = System.IO.Path.ChangeExtension( mapPath, ".def" );
          try
          {
            System.IO.File.WriteAllText( defPath, fileLog );
          }
          catch ( Exception ex )
          {
            if ( Core != null )
            {
              Core.Notification.MessageBox( "Error saving .def file",
                "Could not save the layout sidecar file to:\r\n" + defPath + "\r\n\r\n" + ex.Message );
            }
            // continue - the binary itself was saved
          }
        }
        summary.AppendLine( "  " + file.FileName + " - map " + file.ExportIndex + " '" + file.Map.Name + "', " + finalData.Length + " bytes"
                          + ( ( file.CharsetData != null ) ? " + " + file.CharsetFileName + ( ( compressedCharsetName != null ) ? " + " + compressedCharsetName : "" ) : "" ) );
        fileLogs.AppendLine( "==================== " + file.FileName + " ====================" );
        fileLogs.AppendLine( fileLog );

        if ( ( checkMaxExportSize.Checked )
        &&   ( maxExportSize > 0 )
        &&   ( (long)finalData.Length > maxExportSize ) )
        {
          oversized.Add( file.FileName + " (" + finalData.Length + " bytes)" );
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
          firstMapPath,
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
          firstMapPath,
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
          firstMapPath,
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
          firstMapPath,
          () => map.GenerateMapStringsAsm( mapStringsPrefix ) );
      }

      // Charset-labels sidecar — export name -> compacted charset index.
      // Lands next to the charset files.
      if ( checkExportCharsetLabels.Checked )
      {
        string charsetLabelsPrefix = editCharsetLabelsPrefix.Text;
        var map = Info.Map;
        WriteAsmSidecar(
          "charset labels",
          charsetDir,
          editCharsetLabelsFilename.Text,
          "map_charsets.asm",
          firstMapPath,
          () => map.GenerateCharsetLabelsAsm( charsetLabelsPrefix ) );
      }

      // Map-labels sidecar — map name -> exported (compacted) map index.
      if ( checkExportMapLabels.Checked )
      {
        string mapLabelsPrefix = editMapLabelsPrefix.Text;
        var map = Info.Map;
        WriteAsmSidecar(
          "map labels",
          charsetDir,
          editMapLabelsFilename.Text,
          "map_names.asm",
          firstMapPath,
          () => map.GenerateMapLabelsAsm( mapLabelsPrefix ) );
      }

      if ( EditOutput != null )
      {
        EditOutput.Text = summary.ToString() + "\r\n" + fileLogs.ToString();
      }

      // Max-export-size check - runs AFTER the export is fully written, per
      // map file (the size includes the 2-byte load-address prefix when that
      // option is on). Informational only: the files are already saved.
      if ( oversized.Count > 0 )
      {
        System.Windows.Forms.MessageBox.Show(
          "These map files exceed the configured maximum of " + maxExportSize + " bytes"
          + ( checkPrefixLoadAddress.Checked ? " (sizes include the 2-byte load address)" : "" ) + ":\r\n\r\n  "
          + string.Join( "\r\n  ", oversized.ToArray() ),
          "Max export size exceeded",
          System.Windows.Forms.MessageBoxButtons.OK,
          System.Windows.Forms.MessageBoxIcon.Warning );
      }

      return true;
    }



    /// <summary>The "nothing was written" box for the build-time errors (a test answers it silently).</summary>
    protected virtual void ReportExportAborted( List<string> Errors )
    {
      var sb = new System.Text.StringBuilder();
      sb.AppendLine( "Export aborted - nothing was written:" );
      sb.AppendLine();
      foreach ( var e in Errors ) sb.AppendLine( "  " + e );
      System.Windows.Forms.MessageBox.Show(
        sb.ToString(),
        "Game binary export error",
        System.Windows.Forms.MessageBoxButtons.OK,
        System.Windows.Forms.MessageBoxIcon.Warning );
    }



    /// <summary>Where a sidecar lands: its configured folder, else the map files' folder.</summary>
    private static string SidecarDirectory( string ConfiguredDir, string BinaryPath )
    {
      if ( !string.IsNullOrEmpty( ConfiguredDir ) )
      {
        return ConfiguredDir;
      }
      try
      {
        return System.IO.Path.GetDirectoryName( BinaryPath );
      }
      catch ( Exception )
      {
        return null;
      }
    }



    private void WriteAsmSidecar( string Description, string ConfiguredDir, string ConfiguredFilename,
                                  string DefaultFilename, string BinaryPath, Func<string> GenerateContent )
    {
      string dir = SidecarDirectory( ConfiguredDir, BinaryPath );

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



    /// <summary>
    /// Optionally compress the just-exported map file with the selected
    /// compressor, writing the result as a SECOND file (the original is never
    /// touched). Runs only when "Compress map" is checked and a filename is
    /// given. The filename is used verbatim — no extension is added; a rooted
    /// path is honored, otherwise the file lands beside the main export.
    /// <paramref name="OverrideAddress"/> is the already-parsed load-address
    /// override (-1 = none). On success <paramref name="Report"/> gets a
    /// human-readable size/ratio line. Any failure (missing tool,
    /// same-as-original name, compressor error) is reported via a message box
    /// and leaves the export itself successful.
    /// </summary>
    private bool TryCompressExport( string MainFilePath, string OutputName, long UncompressedSize, int OverrideAddress, out string Report )
    {
      Report = null;
      if ( !checkCompressMap.Checked )
      {
        return false;
      }
      // The compressed file name pattern, already formatted with the map's
      // number by the caller.
      string outName = OutputName ?? "";
      if ( string.IsNullOrEmpty( outName ) )
      {
        return false;
      }

      // Output directory: the explicit Directory box, or — when blank — the same
      // folder as the main export. The filename is used exactly as typed (no
      // extension added); a rooted filename overrides the directory.
      string outDir = editCompressDirectory.Text ?? "";
      if ( string.IsNullOrEmpty( outDir ) )
      {
        outDir = System.IO.Path.GetDirectoryName( MainFilePath ) ?? "";
      }
      string outPath = System.IO.Path.IsPathRooted( outName )
                       ? outName
                       : System.IO.Path.Combine( outDir, outName );

      // Never overwrite the original map file.
      try
      {
        if ( string.Equals( System.IO.Path.GetFullPath( outPath ),
                            System.IO.Path.GetFullPath( MainFilePath ),
                            StringComparison.OrdinalIgnoreCase ) )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Compression skipped",
              "The compressed filename is the same as the exported map file.\r\n"
              + "Pick a different name so the original is not overwritten." );
          }
          return false;
        }
      }
      catch ( Exception )
      {
        // GetFullPath can throw on a malformed name — fall through and let the
        // compressor surface the bad path.
      }

      string compressor = ( comboCompressor.SelectedItem != null ) ? comboCompressor.SelectedItem.ToString() : "ZX0";
      string error;
      if ( !GameBinaryCompression.RunCompressor( compressor, MainFilePath, outPath,
                                                 OverrideAddress, UncompressedSize, out Report, out error ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Compression failed",
            error + "\r\n\r\nThe map was exported, but not compressed." );
        }
        Report = null;
        return false;
      }
      return true;
    }



    /// <summary>
    /// Compresses the just-written charset file with the charset compressor
    /// into <paramref name="OutputPath"/> - a SECOND file; the charset file
    /// itself is never touched. <paramref name="OverrideAddress"/> is the
    /// already-parsed load-address override (-1 = none). On success
    /// <paramref name="Report"/> gets the size/ratio line; a failure is
    /// reported via a message box and leaves the export itself successful,
    /// like TryCompressExport.
    /// </summary>
    private bool TryCompressCharsetFile( string CharsetPath, string OutputPath, long UncompressedSize, int OverrideAddress, out string Report )
    {
      Report = null;
      string compressor = ( comboCharsetCompressor.SelectedItem != null ) ? comboCharsetCompressor.SelectedItem.ToString() : "ZX0";
      string error;
      if ( !GameBinaryCompression.RunCompressor( compressor, CharsetPath, OutputPath,
                                                 OverrideAddress, UncompressedSize, out Report, out error ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Compression failed",
            error + "\r\n\r\nThe character set file was exported, but not compressed:\r\n" + CharsetPath );
        }
        Report = null;
        return false;
      }
      return true;
    }



    private string GenerateExportLog( ByteBuffer buf, string targetPath,
                                       bool exportMarkers, bool exportColors, bool exportPassable,
                                       RetroDevStudio.Formats.MapProject project,
                                       RetroDevStudio.Formats.MapProject.Map Map, List<string> CharsetNames,
                                       string CompressionReport = null, string CharsetReport = null )
    {
      // Pointers in the binary are file-relative offsets, so the dump shows the
      // raw file offset for every address. Kept as a local 0 so the offset/
      // file-position arithmetic below (Addr(...), "- ba") reads naturally.
      ushort baseAddr = 0;

      int markerStride = buf.ByteAt( 0 );
      int tileCount = buf.ByteAt( 1 );
      int mapCount = buf.ByteAt( 2 );
      int startMapIndex = buf.ByteAt( 0x03 );
      int entityStride = buf.ByteAt( 0x2E );
      int mapStringCount = buf.ByteAt( 0x35 );

      var sb = new StringBuilder();
      sb.AppendLine( "Exported " + buf.Length + " bytes to " + targetPath );
      if ( !string.IsNullOrEmpty( CompressionReport ) )
      {
        sb.AppendLine( CompressionReport );
      }
      if ( !string.IsNullOrEmpty( CharsetReport ) )
      {
        sb.AppendLine( CharsetReport.TrimEnd() );
      }
      sb.AppendLine();

      // Always include the header-constant definitions near the top so the .def
      // serves as a complete reference for the accompanying .bin (no need to
      // cross-reference map_header.asm).
      sb.AppendLine( RetroDevStudio.Formats.MapProject.GenerateGameBinaryHeaderAsm() );

      // --- HEADER ---
      sb.AppendLine( "--- HEADER (65 bytes) ---" );
      sb.AppendLine( Addr( baseAddr, 0x00 ) + ": " + HexByte( buf.ByteAt( 0 ) ).PadRight( 24 ) + "marker_stride = " + markerStride );
      sb.AppendLine( Addr( baseAddr, 0x01 ) + ": " + HexByte( buf.ByteAt( 1 ) ).PadRight( 24 ) + "tile_count = " + tileCount );
      sb.AppendLine( Addr( baseAddr, 0x02 ) + ": " + HexByte( buf.ByteAt( 2 ) ).PadRight( 24 ) + "map_count = " + mapCount );
      sb.AppendLine( Addr( baseAddr, 0x03 ) + ": " + HexByte( buf.ByteAt( 0x03 ) ).PadRight( 24 ) + "start_map_index = " + startMapIndex );

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
        int hdrOff = 0x04 + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + hdrNames[i] + resolved );
      }

      // Entity section (v23+) — one stride byte followed by three 2-byte
      // offset pointers, mirroring the marker section layout. Always printed
      // so the .def dump reflects the full header even when a map has no
      // entity types defined yet.
      sb.AppendLine( Addr( baseAddr, 0x2E ) + ": " + HexByte( buf.ByteAt( 0x2E ) ).PadRight( 24 ) + "entity_stride = " + entityStride );
      string[] entHdrNames = {
        "offset_map_entity_count",
        "offset_map_entities_lo",
        "offset_map_entities_hi",
      };
      for ( int i = 0; i < 3; ++i )
      {
        int hdrOff = 0x2F + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + entHdrNames[i] + resolved );
      }
      // Map-strings section (v24+) — count byte + LO/HI/ID table pointers.
      sb.AppendLine( Addr( baseAddr, 0x35 ) + ": " + HexByte( buf.ByteAt( 0x35 ) ).PadRight( 24 ) + "map_string_count = " + mapStringCount );
      string[] mapStringHdrNames = {
        "offset_map_string_lo",
        "offset_map_string_hi",
        "offset_map_string_id",
      };
      for ( int i = 0; i < 3; ++i )
      {
        int hdrOff = 0x36 + i * 2;
        ushort val = buf.UInt16At( hdrOff );
        string resolved = ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)";
        sb.AppendLine( Addr( baseAddr, hdrOff ) + ": " + HexBytes( buf, hdrOff, 2 ).PadRight( 24 ) + mapStringHdrNames[i] + resolved );
      }
      // Multiple character sets (v29): per-map charset index table, exported
      // charset count, charset directory.
      {
        ushort val = buf.UInt16At( 0x3C );
        sb.AppendLine( Addr( baseAddr, 0x3C ) + ": " + HexBytes( buf, 0x3C, 2 ).PadRight( 24 ) + "offset_map_charset_index" + ( ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)" ) );
        sb.AppendLine( Addr( baseAddr, 0x3E ) + ": " + HexByte( buf.ByteAt( 0x3E ) ).PadRight( 24 ) + "charset_count = " + buf.ByteAt( 0x3E ) );
        val = buf.UInt16At( 0x3F );
        sb.AppendLine( Addr( baseAddr, 0x3F ) + ": " + HexBytes( buf, 0x3F, 2 ).PadRight( 24 ) + "offset_charset_directory" + ( ( val != 0 ) ? " -> $" + val.ToString( "X4" ) : " -> (disabled)" ) );
      }
      sb.AppendLine();

      // Pointers stored in the file are file-relative offsets, so a stored value
      // is already the byte position to read from (baseAddr is 0).
      int ba = baseAddr;

      // --- CHARSET DIRECTORY + per-charset tile sections ---
      // The legacy header fields ($01, $04..$10) mirror directory entry 0.
      sb.AppendLine( RetroDevStudio.Formats.MapProject.DescribeGameBinaryCharsets( buf, CharsetNames ) );
      int charsetCount = buf.ByteAt( 0x3E );
      int directoryPos = buf.UInt16At( 0x3F ) - ba;
      for ( int c = 0; c < charsetCount; ++c )
      {
        int rec = directoryPos + c * 15;
        int csTileCount = buf.ByteAt( rec );
        sb.AppendLine( "--- CHARSET " + c + " TILE ARRAYS (" + csTileCount + " tiles) ---" );
        AppendArraySection( sb, buf, ba, rec + 0x01, csTileCount, "tiles_width", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x03, csTileCount, "tiles_height", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x05, csTileCount, "tiles_flags", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x07, csTileCount, "tile_char_offset_lo", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x09, csTileCount, "tile_char_offset_hi", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x0B, csTileCount, "tile_color_offset_lo", 1 );
        AppendArraySection( sb, buf, ba, rec + 0x0D, csTileCount, "tile_color_offset_hi", 1 );

        if ( csTileCount > 0 )
        {
          int charOffLoFilePos = buf.UInt16At( rec + 0x07 ) - ba;
          int charOffHiFilePos = buf.UInt16At( rec + 0x09 ) - ba;
          int colorOffLoFilePos = buf.UInt16At( rec + 0x0B ) - ba;
          int colorOffHiFilePos = buf.UInt16At( rec + 0x0D ) - ba;
          int widthFilePos = buf.UInt16At( rec + 0x01 ) - ba;
          int heightFilePos = buf.UInt16At( rec + 0x03 ) - ba;

          sb.AppendLine();
          sb.AppendLine( "--- CHARSET " + c + " TILE CHAR DATA ---" );
          for ( int t = 0; t < csTileCount; ++t )
          {
            int addr = buf.ByteAt( charOffLoFilePos + t ) | ( buf.ByteAt( charOffHiFilePos + t ) << 8 );
            int filePos = addr - ba;
            int tw = buf.ByteAt( widthFilePos + t );
            int th = buf.ByteAt( heightFilePos + t );
            int size = tw * th;
            sb.AppendLine( "$" + addr.ToString( "X4" ) + ": tile " + t + " char data (" + tw + "x" + th + " = " + size + " bytes)  " + HexBytes( buf, filePos, Math.Min( size, 16 ) ) + ( size > 16 ? " ..." : "" ) );
          }

          sb.AppendLine();
          sb.AppendLine( "--- CHARSET " + c + " TILE COLOR DATA ---" );
          for ( int t = 0; t < csTileCount; ++t )
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
      }

      // --- MAP METADATA ARRAYS ---
      sb.AppendLine( "--- MAP METADATA ---" );
      AppendArraySection( sb, buf, ba, 0x12, mapCount, "map_width", 1 );
      AppendArraySection( sb, buf, ba, 0x14, mapCount, "map_height", 1 );
      AppendArraySection( sb, buf, ba, 0x16, mapCount, "map_bg_color", 1 );
      AppendArraySection( sb, buf, ba, 0x18, mapCount, "map_mc1_color", 1 );
      AppendArraySection( sb, buf, ba, 0x1A, mapCount, "map_mc2_color", 1 );
      AppendArraySection( sb, buf, ba, 0x1C, mapCount, "map_marker_count", 1 );
      // map_entity_count's array is written right after map_marker_count (before
      // the lookup tables), so it belongs here in the metadata block — printing
      // it among the lookup tables put it out of file order in the dump.
      AppendArraySection( sb, buf, ba, 0x2F, mapCount, "map_entity_count", 1 );
      // map_charset_index[] follows map_entity_count in the file (v29).
      AppendArraySection( sb, buf, ba, 0x3C, mapCount, "map_charset_index", 1 );
      sb.AppendLine();

      // --- MAP DATA LOOKUP TABLES ---
      sb.AppendLine( "--- MAP DATA LOOKUP TABLES ---" );
      AppendArraySection( sb, buf, ba, 0x1E, mapCount, "map_char_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x20, mapCount, "map_char_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x22, mapCount, "map_color_grid_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x24, mapCount, "map_color_grid_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x26, mapCount, "map_passable_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x28, mapCount, "map_passable_hi", 1 );
      AppendArraySection( sb, buf, ba, 0x2A, mapCount, "map_markers_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x2C, mapCount, "map_markers_hi", 1 );
      // Entity lookup tables — AppendArraySection quietly skips any whose
      // header pointer is zero, so these just drop out cleanly when a project
      // has no entities. (map_entity_count is a count array, printed above in
      // the metadata block where it physically lives.)
      AppendArraySection( sb, buf, ba, 0x31, mapCount, "map_entities_lo", 1 );
      AppendArraySection( sb, buf, ba, 0x33, mapCount, "map_entities_hi", 1 );
      sb.AppendLine();

      // --- PER-MAP VARIABLE DATA ---
      if ( mapCount > 0 )
      {
        int charGridLoFilePos  = buf.UInt16At( 0x1E ) - ba;
        int charGridHiFilePos  = buf.UInt16At( 0x20 ) - ba;
        int colorGridLoFilePos = buf.UInt16At( 0x22 ) - ba;
        int colorGridHiFilePos = buf.UInt16At( 0x24 ) - ba;
        int passableLoFilePos  = buf.UInt16At( 0x26 ) - ba;
        int passableHiFilePos  = buf.UInt16At( 0x28 ) - ba;
        int markersLoFilePos   = buf.UInt16At( 0x2A ) - ba;
        int markersHiFilePos   = buf.UInt16At( 0x2C ) - ba;
        int mapWidthFilePos    = buf.UInt16At( 0x12 ) - ba;
        int mapHeightFilePos   = buf.UInt16At( 0x14 ) - ba;
        int markerCountFilePos = buf.UInt16At( 0x1C ) - ba;
        // Entity lookup arrays — zero offset means the project has no
        // entities, in which case we skip every entity-related read below
        // via the per-map address check.
        ushort entityCountAddr = buf.UInt16At( 0x2F );
        ushort entitiesLoAddr  = buf.UInt16At( 0x31 );
        ushort entitiesHiAddr  = buf.UInt16At( 0x33 );
        int entityCountFilePos = ( entityCountAddr != 0 ) ? entityCountAddr - ba : -1;
        int entitiesLoFilePos  = ( entitiesLoAddr  != 0 ) ? entitiesLoAddr  - ba : -1;
        int entitiesHiFilePos  = ( entitiesHiAddr  != 0 ) ? entitiesHiAddr  - ba : -1;

        for ( int m = 0; m < mapCount; ++m )
        {
          sb.AppendLine( "--- MAP " + m + " DATA (charset " + buf.ByteAt( buf.UInt16At( 0x3C ) - ba + m ) + ") ---" );
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
                // Current layout (stride 11): tag, x, y, value1, value2, flags, group, link_to_id, link_id, value3, value4
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
                if ( markerStride >= 8 )
                  line += " linkTo=" + buf.ByteAt( mFilePos + 7 );
                if ( markerStride >= 9 )
                  line += " linkId=" + buf.ByteAt( mFilePos + 8 );
                if ( markerStride >= 10 )
                  line += " value3=" + HexByte( buf.ByteAt( mFilePos + 9 ) );
                if ( markerStride >= 11 )
                  line += " value4=" + HexByte( buf.ByteAt( mFilePos + 10 ) );
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
                // Current layout (stride 9): index, tag, x, y, tile, value1, value2, enabled, triggered
                string line = "  $" + eAddr.ToString( "X4" ) + ": index=" + buf.ByteAt( eFilePos )
                            + " tag=" + HexByte( buf.ByteAt( eFilePos + 1 ) )
                            + " x=" + buf.ByteAt( eFilePos + 2 )
                            + " y=" + buf.ByteAt( eFilePos + 3 );
                if ( entityStride >= 5 )
                  line += " tile=" + buf.ByteAt( eFilePos + 4 );
                if ( entityStride >= 6 )
                  line += " value1=" + HexByte( buf.ByteAt( eFilePos + 5 ) );
                if ( entityStride >= 7 )
                  line += " value2=" + HexByte( buf.ByteAt( eFilePos + 6 ) );
                if ( entityStride >= 8 )
                  line += " enabled=" + buf.ByteAt( eFilePos + 7 );
                if ( entityStride >= 9 )
                  line += " triggered=" + buf.ByteAt( eFilePos + 8 );
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
        ushort mapStringLoAddr = buf.UInt16At( 0x36 );
        ushort mapStringHiAddr = buf.UInt16At( 0x38 );
        ushort mapStringIdAddr = buf.UInt16At( 0x3A );
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
          if ( mapStringIdAddr != 0 )
          {
            sb.AppendLine( "$" + mapStringIdAddr.ToString( "X4" ) + ": map_string_id[" + mapStringCount + "]"
                         + "  " + HexBytes( buf, mapStringIdAddr - ba, Math.Min( mapStringCount, 24 ) )
                         + ( mapStringCount > 24 ? " ..." : "" ) );
          }
          sb.AppendLine();

          sb.AppendLine( "--- MAP STRING DATA ---" );
          // Pull labels from the in-memory project so the log can show
          // which message lives at each binary index. Binary tables are
          // 1:1 with GetMapStringsForExport() — the SAME filtered list the
          // writer used (unused strings, i.e. StringIDs no
          // SHOW_MAP_MESSAGE marker references, are omitted) — so index N
          // in the binary is exportStrings[N], NOT MapStrings[N].
          var emittedLabels = new List<string>();
          if ( project != null )
          {
            // A per-map file carries only the strings its own markers show.
            var exportStrings = ( Map != null ) ? project.GetMapStringsForExport( Map ) : project.GetMapStringsForExport();
            foreach ( var ms in exportStrings )
            {
              emittedLabels.Add( ms.Label ?? "" );
            }
            if ( exportStrings.Count < project.MapStrings.Count )
            {
              sb.AppendLine( "( " + ( project.MapStrings.Count - exportStrings.Count )
                           + " string(s) not in this file — no message marker" + ( ( Map != null ) ? " on this map" : "" ) + " (SHOW_MAP_MESSAGE,"
                           + " or BUMPABLE_MARKER with Value1=1) references their StringID: )" );
              foreach ( var ms in project.MapStrings )
              {
                if ( !exportStrings.Contains( ms ) )
                {
                  sb.AppendLine( "(   ID " + ms.StringID + ": '"
                               + ( string.IsNullOrEmpty( ms.Label ) ? "(no label)" : ms.Label ) + "' )" );
                }
              }
              sb.AppendLine();
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
            string lbl = ( i < emittedLabels.Count ) ? emittedLabels[i] : "";
            if ( string.IsNullOrEmpty( lbl ) ) lbl = "string " + i;

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
                if ( b == 0xFA )
                {
                  flush();
                  sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FA SHOW_NEXT_PAGE_MARKER" );
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
              if ( b == 0xFA )
              {
                // Line terminator like $FD/$FC — the runtime's "more text
                // follows" marker.
                flush();
                sb.AppendLine( "  $" + ( (ushort)( streamAddr + p ) ).ToString( "X4" ) + ": $FA SHOW_NEXT_PAGE_MARKER" );
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



    private void checkPrefixLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkMaxExportSize_CheckedChanged( object sender, EventArgs e )
    {
      editMaxExportSize.Enabled = checkMaxExportSize.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkCompressMap_CheckedChanged( object sender, EventArgs e )
    {
      // Only the settings beside the checkbox are disabled - never its group
      // box - so it can always be ticked again.
      comboCompressor.Enabled = checkCompressMap.Checked;
      editCompressDirectory.Enabled = checkCompressMap.Checked;
      btnBrowseCompressDirectory.Enabled = checkCompressMap.Checked;
      editCompressFilename.Enabled = checkCompressMap.Checked;
      checkOverrideLoadAddress.Enabled = checkCompressMap.Checked;
      editOverrideLoadAddress.Enabled = checkCompressMap.Checked && checkOverrideLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkOverrideLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editOverrideLoadAddress.Enabled = checkCompressMap.Checked && checkOverrideLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void btnBrowseCompressDirectory_Click( object sender, EventArgs e )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select compressed map output directory";
        if ( !string.IsNullOrEmpty( editCompressDirectory.Text ) )
        {
          dlg.SelectedPath = editCompressDirectory.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          editCompressDirectory.Text = dlg.SelectedPath;
        }
      }
    }



    private void checkSaveOnExport_CheckedChanged( object sender, EventArgs e )
    {
      editExportDirectory.Enabled = checkSaveOnExport.Checked;
      btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;
      // The map file name pattern is always needed (without auto-save the
      // export asks for the folder only).
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



    private void checkCharsetPrefixLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editCharsetPrefixLoadAddress.Enabled = checkCharsetPrefixLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    /// <summary>
    /// A user turning compression on gets the load-address override with it -
    /// the packer writes that address into the file as its depack target -
    /// set to $0000 when the box holds no valid address yet. Done on Click,
    /// not CheckedChanged: loading a project's settings restores them as
    /// saved and must not add an override.
    /// </summary>
    private static void DefaultLoadAddressOverride( CheckBox Override, TextBox Address )
    {
      int parsed;
      if ( !GameBinaryCompression.TryParseHexAddress( Address.Text, out parsed ) )
      {
        Address.Text = "$0000";
      }
      Override.Checked = true;
    }



    private void checkCompressMap_Click( object sender, EventArgs e )
    {
      if ( checkCompressMap.Checked )
      {
        DefaultLoadAddressOverride( checkOverrideLoadAddress, editOverrideLoadAddress );
      }
    }



    private void checkCompressCharsets_Click( object sender, EventArgs e )
    {
      if ( checkCompressCharsets.Checked )
      {
        DefaultLoadAddressOverride( checkCharsetOverrideLoadAddress, editCharsetOverrideLoadAddress );
      }
    }



    private void checkCompressCharsets_CheckedChanged( object sender, EventArgs e )
    {
      // Only the settings beside the checkbox are disabled - never its group
      // box - so it can always be ticked again.
      comboCharsetCompressor.Enabled = checkCompressCharsets.Checked;
      editCharsetCompressFilename.Enabled = checkCompressCharsets.Checked;
      checkCharsetOverrideLoadAddress.Enabled = checkCompressCharsets.Checked;
      editCharsetOverrideLoadAddress.Enabled = checkCompressCharsets.Checked && checkCharsetOverrideLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkCharsetOverrideLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editCharsetOverrideLoadAddress.Enabled = checkCompressCharsets.Checked && checkCharsetOverrideLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkExportCharsetLabels_CheckedChanged( object sender, EventArgs e )
    {
      editCharsetLabelsFilename.Enabled = checkExportCharsetLabels.Checked;
      editCharsetLabelsPrefix.Enabled = checkExportCharsetLabels.Checked;
      if ( ( checkExportCharsetLabels.Checked )
      &&   ( string.IsNullOrEmpty( editCharsetLabelsFilename.Text ) ) )
      {
        editCharsetLabelsFilename.Text = "map_charsets.asm";
      }
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkExportMapLabels_CheckedChanged( object sender, EventArgs e )
    {
      editMapLabelsFilename.Enabled = checkExportMapLabels.Checked;
      editMapLabelsPrefix.Enabled = checkExportMapLabels.Checked;
      if ( ( checkExportMapLabels.Checked )
      &&   ( string.IsNullOrEmpty( editMapLabelsFilename.Text ) ) )
      {
        editMapLabelsFilename.Text = "map_names.asm";
      }
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
        checkPrefixLoadAddress.Checked = s.PrefixLoadAddress;
        editPrefixLoadAddress.Text = s.PrefixLoadAddressHex ?? "";
        editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
        checkMaxExportSize.Checked = s.MaxExportSizeEnabled;
        editMaxExportSize.Text = s.MaxExportSizeText ?? "0";
        editMaxExportSize.Enabled = checkMaxExportSize.Checked;
        checkCompressMap.Checked = s.CompressMap;
        // Select the stored compressor; fall back to the first item (ZX0) if the
        // saved name isn't one we currently offer.
        comboCompressor.SelectedIndex = comboCompressor.Items.IndexOf( s.Compressor ?? "ZX0" );
        if ( comboCompressor.SelectedIndex < 0 )
        {
          comboCompressor.SelectedIndex = ( comboCompressor.Items.Count > 0 ) ? 0 : -1;
        }
        editCompressDirectory.Text = s.CompressDirectory ?? "";
        editCompressFilename.Text = s.CompressFilename ?? "";
        checkOverrideLoadAddress.Checked = s.OverrideLoadAddress;
        editOverrideLoadAddress.Text = s.OverrideLoadAddressHex ?? "";
        comboCompressor.Enabled = checkCompressMap.Checked;
        editCompressDirectory.Enabled = checkCompressMap.Checked;
        btnBrowseCompressDirectory.Enabled = checkCompressMap.Checked;
        editCompressFilename.Enabled = checkCompressMap.Checked;
        checkOverrideLoadAddress.Enabled = checkCompressMap.Checked;
        editOverrideLoadAddress.Enabled = checkCompressMap.Checked && checkOverrideLoadAddress.Checked;
        checkSaveOnExport.Checked = s.SaveOnExport;
        editExportDirectory.Text = s.ExportDirectory ?? "";
        // ExportFilename is the map file name PATTERN (one file per map).
        editExportFilename.Text = string.IsNullOrEmpty( s.ExportFilename ) ? "MAP_%d.bin" : s.ExportFilename;
        editExportDirectory.Enabled = checkSaveOnExport.Checked;
        btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;

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

        // Charset files follow the maps: one per map file, named by the
        // charset file name pattern with the map's number.
        editCharsetExportDirectory.Text = s.CharsetExportDirectory ?? "";
        editCharsetExportDirectory.Enabled = true;
        btnBrowseCharsetExportDirectory.Enabled = true;
        editCharsetFilenamePattern.Text = s.CharsetFilenamePattern ?? "";

        checkExportCharsetLabels.Checked = s.ExportCharsetLabels;
        editCharsetLabelsFilename.Text = string.IsNullOrEmpty( s.CharsetLabelsFilename ) ? "map_charsets.asm" : s.CharsetLabelsFilename;
        editCharsetLabelsPrefix.Text = s.CharsetLabelsPrefix ?? "";
        editCharsetLabelsFilename.Enabled = checkExportCharsetLabels.Checked;
        editCharsetLabelsPrefix.Enabled = checkExportCharsetLabels.Checked;

        checkExportMapLabels.Checked = s.ExportMapLabels;
        editMapLabelsFilename.Text = string.IsNullOrEmpty( s.MapLabelsFilename ) ? "map_names.asm" : s.MapLabelsFilename;
        editMapLabelsPrefix.Text = s.MapLabelsPrefix ?? "";
        editMapLabelsFilename.Enabled = checkExportMapLabels.Checked;
        editMapLabelsPrefix.Enabled = checkExportMapLabels.Checked;

        checkCharsetPrefixLoadAddress.Checked = s.CharsetPrefixLoadAddress;
        editCharsetPrefixLoadAddress.Text = s.CharsetPrefixLoadAddressHex ?? "";
        editCharsetPrefixLoadAddress.Enabled = checkCharsetPrefixLoadAddress.Checked;

        // Charset compression: the stored compressor, else the first offered.
        checkCompressCharsets.Checked = s.CompressCharsets;
        comboCharsetCompressor.SelectedIndex = comboCharsetCompressor.Items.IndexOf( s.CharsetCompressor ?? "ZX0" );
        if ( comboCharsetCompressor.SelectedIndex < 0 )
        {
          comboCharsetCompressor.SelectedIndex = ( comboCharsetCompressor.Items.Count > 0 ) ? 0 : -1;
        }
        checkCharsetOverrideLoadAddress.Checked = s.CharsetOverrideLoadAddress;
        editCharsetOverrideLoadAddress.Text = s.CharsetOverrideLoadAddressHex ?? "";
        editCharsetCompressFilename.Text = s.CharsetCompressFilename ?? "";
        comboCharsetCompressor.Enabled = checkCompressCharsets.Checked;
        editCharsetCompressFilename.Enabled = checkCompressCharsets.Checked;
        checkCharsetOverrideLoadAddress.Enabled = checkCompressCharsets.Checked;
        editCharsetOverrideLoadAddress.Enabled = checkCompressCharsets.Checked && checkCharsetOverrideLoadAddress.Checked;
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
      s.PrefixLoadAddress = checkPrefixLoadAddress.Checked;
      s.PrefixLoadAddressHex = editPrefixLoadAddress.Text ?? "";
      s.MaxExportSizeEnabled = checkMaxExportSize.Checked;
      s.MaxExportSizeText = editMaxExportSize.Text ?? "";
      s.CompressMap = checkCompressMap.Checked;
      s.Compressor = ( comboCompressor.SelectedItem != null ) ? comboCompressor.SelectedItem.ToString() : "ZX0";
      s.CompressDirectory = editCompressDirectory.Text ?? "";
      s.CompressFilename = editCompressFilename.Text ?? "";
      s.OverrideLoadAddress = checkOverrideLoadAddress.Checked;
      s.OverrideLoadAddressHex = editOverrideLoadAddress.Text ?? "";
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
      // ExportCharset / CharsetExportFilename are dead since v29 (the per-
      // charset export name + checkbox live on the Character Set tab); they
      // keep their loaded values so the settings chunk layout stays stable.
      s.CharsetExportDirectory = editCharsetExportDirectory.Text ?? "";
      s.CharsetFilenamePattern = editCharsetFilenamePattern.Text ?? "";
      s.ExportCharsetLabels = checkExportCharsetLabels.Checked;
      s.CharsetLabelsFilename = string.IsNullOrEmpty( editCharsetLabelsFilename.Text ) ? "map_charsets.asm" : editCharsetLabelsFilename.Text;
      s.CharsetLabelsPrefix = editCharsetLabelsPrefix.Text ?? "";
      s.ExportMapLabels = checkExportMapLabels.Checked;
      s.MapLabelsFilename = string.IsNullOrEmpty( editMapLabelsFilename.Text ) ? "map_names.asm" : editMapLabelsFilename.Text;
      s.MapLabelsPrefix = editMapLabelsPrefix.Text ?? "";
      s.CharsetPrefixLoadAddress = checkCharsetPrefixLoadAddress.Checked;
      s.CharsetPrefixLoadAddressHex = editCharsetPrefixLoadAddress.Text ?? "";
      s.CompressCharsets = checkCompressCharsets.Checked;
      s.CharsetCompressor = ( comboCharsetCompressor.SelectedItem != null ) ? comboCharsetCompressor.SelectedItem.ToString() : "ZX0";
      s.CharsetOverrideLoadAddress = checkCharsetOverrideLoadAddress.Checked;
      s.CharsetOverrideLoadAddressHex = editCharsetOverrideLoadAddress.Text ?? "";
      s.CharsetCompressFilename = editCharsetCompressFilename.Text ?? "";
    }



  }
}
