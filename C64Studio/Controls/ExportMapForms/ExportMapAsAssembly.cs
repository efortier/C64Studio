using RetroDevStudio;
using RetroDevStudio.Formats;
using RetroDevStudio.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  public partial class ExportMapAsAssembly : ExportMapFormBase
  {
    private bool m_ApplyingSettings = false;

    public ExportMapAsAssembly() :
      base( null )
    { 
    }

    public ExportMapAsAssembly( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();
    }

    private void checkWrapMapData_CheckedChanged( object sender, EventArgs e )
    {
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private void checkEmptyTile_CheckedChanged(object sender, EventArgs e)
    {
      editEmptyTileIndex.Enabled = checkEmptyTile.Checked;
      if (!m_ApplyingSettings)
      {
        RaiseSettingsChanged();
      }
    }

    private void checkAddFilenamespace_CheckedChanged(object sender, EventArgs e)
    {
      editFilenamespace.Enabled = checkAddFilenamespace.Checked;
      if ( ( checkAddFilenamespace.Checked )
      &&   ( string.IsNullOrEmpty( editFilenamespace.Text ) ) )
      {
        if ( Core.MainForm.ActiveDocument != null )
        {
          editFilenamespace.Text = System.IO.Path.GetFileNameWithoutExtension( Core.MainForm.ActiveDocument.DocumentInfo.DocumentFilename );
        }
      }
      if (!m_ApplyingSettings)
      {
        RaiseSettingsChanged();
      }
    }

    private void editFilenamespace_TextChanged(object sender, EventArgs e)
    {
      if (!m_ApplyingSettings)
      {
        RaiseSettingsChanged();
      }
    }

    private void checkSaveOnExport_CheckedChanged(object sender, EventArgs e)
    {
      editExportDirectory.Enabled = checkSaveOnExport.Checked;
      btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;
      editExportFilename.Enabled = checkSaveOnExport.Checked;
      if ( checkSaveOnExport.Checked )
      {
        checkAlwaysOverwrite.Enabled = true;
      }
      else
      {
        // Charset files are always written (per-charset "Export" on the
        // Character Set tab), so the overwrite choice always matters.
        checkAlwaysOverwrite.Enabled = true;
      }

      if (!m_ApplyingSettings)
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

    private void btnBrowseCharsetExportDirectory_Click(object sender, EventArgs e)
    {
      System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
      
      if ( !string.IsNullOrEmpty( editCharsetExportDirectory.Text ) )

      {
        browser.SelectedPath = editCharsetExportDirectory.Text;
      }
      if ( browser.ShowDialog() == System.Windows.Forms.DialogResult.OK )
      {
        editCharsetExportDirectory.Text = browser.SelectedPath;
      }
    }

    private void btnBrowseExportDirectory_Click(object sender, EventArgs e)
    {
      System.Windows.Forms.FolderBrowserDialog browser = new System.Windows.Forms.FolderBrowserDialog();
      
      if ( !string.IsNullOrEmpty( editExportDirectory.Text ) )

      {
        browser.SelectedPath = editExportDirectory.Text;
      }
      if ( browser.ShowDialog() == System.Windows.Forms.DialogResult.OK )
      {
        editExportDirectory.Text = browser.SelectedPath;
      }
    }



    private void checkExportToDataWrap_CheckedChanged( object sender, EventArgs e )
    {
      editWrapByteCount.Enabled = checkExportToDataWrap.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }



    private int GetExportWrapCount()
    {
      if ( checkExportToDataWrap.Checked )
      {
        int wrapByteCount = GR.Convert.ToI32( editWrapByteCount.Text );
        if ( wrapByteCount <= 0 )
        {
          wrapByteCount = 8;
        }
        return wrapByteCount;
      }
      return 80;
    }



    public override bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      UpdateExportSettings( Info.Map.Settings );
      int wrapByteCount = GetExportWrapCount();
      string prefix = editPrefix.Text;

      bool wrapData = checkExportToDataWrap.Checked;
      bool prefixRes = checkExportToDataIncludeRes.Checked;
      if ( !prefixRes )
      {
        prefix = "";
      }

      string tileData = "";
      string mapData = "";

      if ( Info.ExportType == MapExportType.TILE_DATA_AS_ELEMENTS )
      {
        Info.Map.ExportTilesAsElements( out tileData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, EditorCharsetIndex( Info ) );
      }
      if ( ( Info.ExportType == MapExportType.TILE_DATA )
      ||   ( Info.ExportType == MapExportType.TILE_AND_MAP_DATA ) )
      {
        Info.Map.ExportTilesAsAssembly( out tileData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, EditorCharsetIndex( Info ) );
      }
      if ( Info.ExportType == MapExportType.MAP_DATA_SELECTION )
      {
        bool    vertical = !Info.RowByRow;

        if ( Info.CurrentMap != null )
        {
          GR.Memory.ByteBuffer      selectionData = new GR.Memory.ByteBuffer();
          bool                      hasSelection = false;

          if ( vertical )
          {
            for ( int i = 0; i < Info.CurrentMap.Tiles.Width; ++i )
            {
              for ( int j = 0; j < Info.CurrentMap.Tiles.Height; ++j )
              {
                if ( Info.SelectedTiles[i, j] )
                {
                  selectionData.AppendU8( (byte)Info.CurrentMap.Tiles[i, j] );
                  hasSelection = true;
                }
              }
            }
            if ( !hasSelection )
            {
              // select all
              for ( int i = 0; i < Info.CurrentMap.Tiles.Width; ++i )
              {
                for ( int j = 0; j < Info.CurrentMap.Tiles.Height; ++j )
                {
                  selectionData.AppendU8( (byte)Info.CurrentMap.Tiles[i, j] );
                }
              }
            }
          }
          else
          {
            for ( int j = 0; j < Info.CurrentMap.Tiles.Height; ++j )
            {
              for ( int i = 0; i < Info.CurrentMap.Tiles.Width; ++i )
              {
                if ( Info.SelectedTiles[i, j] )
                {
                  selectionData.AppendU8( (byte)Info.CurrentMap.Tiles[i, j] );
                  hasSelection = true;
                }
              }
            }
            if ( !hasSelection )
            {
              // select all
              for ( int j = 0; j < Info.CurrentMap.Tiles.Height; ++j )
              {
                for ( int i = 0; i < Info.CurrentMap.Tiles.Width; ++i )
                {
                  selectionData.AppendU8( (byte)Info.CurrentMap.Tiles[i, j] );
                }
              }
            }
          }
          mapData = Util.ToASMData( selectionData, checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix );
        }
      }
      if ( ( Info.ExportType == MapExportType.MAP_DATA )
      ||   ( Info.ExportType == MapExportType.TILE_AND_MAP_DATA ) )
      {
        string commentChars = checkCommentCharacters.Checked ? ( editCommentCharacters.Text ?? "" ) : "";
        Info.Map.ExportMapsAsAssembly( !Info.RowByRow, out mapData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, commentChars );
      }
      if ( Info.ExportType == MapExportType.SPARSE_TILE_AND_MAP_DATA )
      {
        Info.Map.ExportSparseTileAndMapData( !Info.RowByRow, out mapData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, checkEmptyTile.Checked, GR.Convert.ToI32( editEmptyTileIndex.Text ), checkAddFilenamespace.Checked, editFilenamespace.Text, checkWrapMapData.Checked, EditorCharsetIndex( Info ) );
      }
      // Character set files — one per set with "Export character set" enabled
      // on the Character Set tab, named by each set's export name.
      {
        var fileWarnings = Info.Map.GetCharsetFileWarnings();
        if ( fileWarnings.Count > 0 )
        {
          var sb = new System.Text.StringBuilder();
          sb.AppendLine( "Character set export warnings:" );
          sb.AppendLine();
          foreach ( var w in fileWarnings )
          {
            sb.AppendLine( "  " + w );
          }
          sb.AppendLine();
          sb.AppendLine( "Continue anyway?" );
          if ( System.Windows.Forms.MessageBox.Show( sb.ToString(), "Character set export warnings",
                 System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning,
                 System.Windows.Forms.MessageBoxDefaultButton.Button2 ) != System.Windows.Forms.DialogResult.Yes )
          {
            return false;
          }
        }
        ushort charsetLoadAddress = 0;
        bool   charsetPrefix = checkCharsetPrefixLoadAddress.Checked
                            && TryParseCharsetLoadAddress( editCharsetPrefixLoadAddress.Text, out charsetLoadAddress );
        string charsetDir = ResolveCharsetExportDirectory( editCharsetExportDirectory.Text, DocInfo, DocInfo.FullPath );
        WriteCharsetExportFiles( Info.Map, charsetDir, charsetPrefix, charsetLoadAddress, !checkAlwaysOverwrite.Checked );
      }

      string resultText = "";

      switch ( Info.ExportType )
      {
        case MapExportType.TILE_DATA:
        case MapExportType.TILE_DATA_AS_ELEMENTS:
          resultText = tileData;
          break;
        case MapExportType.MAP_DATA:
        case MapExportType.MAP_DATA_SELECTION:
          resultText = mapData;
          break;
        case MapExportType.TILE_AND_MAP_DATA:
          resultText = tileData + mapData;
          break;
        case MapExportType.SPARSE_TILE_AND_MAP_DATA:
          resultText = mapData;
          break;
      }

      resultText = ApplyLabelFormatting( resultText );
      EditOutput.Text = resultText;
      
      if ( ( checkSaveOnExport.Checked ) 
      &&   ( resultText.Length > 0 ) )
      {
        if ( !string.IsNullOrEmpty( editExportFilename.Text ) )
        {
          string    fullPath = editExportFilename.Text;
          string    exportDirectory = editExportDirectory.Text;
          
          if ( string.IsNullOrEmpty( exportDirectory ) )
          {
            fullPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( DocInfo.FullPath ), editExportFilename.Text );
          }
          else
          {
            try
            {
               fullPath = System.IO.Path.Combine( exportDirectory, editExportFilename.Text );
            }
            catch ( Exception )
            {
              // invalid path combination?
              fullPath = editExportFilename.Text;
            }
          }
          if ( System.IO.File.Exists( fullPath ) )
          {
            if ( ( !checkAlwaysOverwrite.Checked )
            &&   ( System.Windows.Forms.MessageBox.Show( "The file " + fullPath + " already exists.\r\nOverwrite?", "File already exists", System.Windows.Forms.MessageBoxButtons.YesNo ) == System.Windows.Forms.DialogResult.No ) )
            {
              return true;
            }
          }
          try
          {
            System.IO.File.WriteAllText( fullPath, resultText );
          }
          catch ( Exception ex )
          {
            Core.Notification.MessageBox( "Error saving file", "Could not save exported file:\r\n" + ex.Message );
          }
        }
      }
      
      return true;
    }



    private void checkExportToDataIncludeRes_CheckedChanged( object sender, EventArgs e )
    {
      editPrefix.Enabled = checkExportToDataIncludeRes.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }

    private void checkVariableNameLabelPrefix_CheckedChanged( object sender, EventArgs e )
    {
      editVariableNameLabelPrefix.Enabled = checkVariableNameLabelPrefix.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
    }

    private void checkCommentCharacters_CheckedChanged( object sender, EventArgs e )
    {
      editCommentCharacters.Enabled = checkCommentCharacters.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
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
        var assemblySettings = Settings.Assembly;
        checkExportToDataIncludeRes.Checked = assemblySettings.PrefixWith;
        editPrefix.Text = assemblySettings.Prefix ?? "";
        checkExportToDataWrap.Checked = assemblySettings.WrapAt;
        int wrapCount = assemblySettings.WrapByteCount;
        if ( wrapCount <= 0 )
        {
          wrapCount = 8;
        }
        editWrapByteCount.Text = wrapCount.ToString();
        checkExportHex.Checked = assemblySettings.ExportHex;
        checkVariableNameLabelPrefix.Checked = assemblySettings.VariableNameLabelPrefixEnabled;
        editVariableNameLabelPrefix.Text = assemblySettings.VariableNameLabelPrefix ?? "";
        checkIncludeSemicolonAfterSimpleLabels.Checked = assemblySettings.IncludeSemicolonAfterSimpleLabels;
        string commentChars = assemblySettings.CommentChars;
        if ( string.IsNullOrEmpty( commentChars ) )
        {
          commentChars = ";";
        }
        editCommentCharacters.Text = commentChars;
        checkCommentCharacters.Checked = assemblySettings.MapSizeCommentEnabled;
        editPrefix.Enabled = checkExportToDataIncludeRes.Checked;
        editWrapByteCount.Enabled = checkExportToDataWrap.Checked;
        editVariableNameLabelPrefix.Enabled = checkVariableNameLabelPrefix.Checked;
        editCommentCharacters.Enabled = checkCommentCharacters.Checked;
        
        checkEmptyTile.Checked = assemblySettings.EmptyTileCompressionEnabled;
        editEmptyTileIndex.Text = assemblySettings.EmptyTileIndex.ToString();
        editEmptyTileIndex.Enabled = checkEmptyTile.Checked;
        
        checkSaveOnExport.Checked = assemblySettings.SaveOnExport;
        editExportDirectory.Text = assemblySettings.ExportDirectory;
        editExportFilename.Text = assemblySettings.ExportFilename;
        
        editExportDirectory.Enabled = checkSaveOnExport.Checked;
        btnBrowseExportDirectory.Enabled = checkSaveOnExport.Checked;
        editExportFilename.Enabled = checkSaveOnExport.Checked;

        checkExportTilesetColors.Checked = assemblySettings.ExportTilesetColors;
        checkExportMapColors.Checked = assemblySettings.ExportMapColors;
        checkAddFilenamespace.Checked = assemblySettings.AddFilenamespace;
        editFilenamespace.Text = assemblySettings.Filenamespace;
        editFilenamespace.Enabled = checkAddFilenamespace.Checked;
        checkWrapMapData.Checked = assemblySettings.WrapMapData;

        editCharsetExportDirectory.Text = assemblySettings.CharsetExportDirectory;

        checkCharsetPrefixLoadAddress.Checked = Settings.CharsetBinary.PrefixLoadAddress;
        editCharsetPrefixLoadAddress.Text = Settings.CharsetBinary.PrefixLoadAddressHex;
        
        editCharsetExportDirectory.Enabled = true;
        btnBrowseCharsetExportDirectory.Enabled = true;
        editCharsetPrefixLoadAddress.Enabled = checkCharsetPrefixLoadAddress.Checked;
        // CheckedChanged does not fire when the same value is re-assigned, so
        // the overwrite toggle's enabled state is set explicitly here.
        checkAlwaysOverwrite.Enabled = true;

        checkAlwaysOverwrite.Checked = assemblySettings.AlwaysOverwrite;
        checkExportMapAsCharAndColors.Checked = assemblySettings.ExportMapAsCharAndColors;
        checkExportPassableBitfields.Checked = assemblySettings.ExportPassableBitfields;
        checkExportPassableBitfieldsAsBinary.Checked = assemblySettings.ExportPassableBitfieldsAsBinary;
        checkExportMarkers.Checked = assemblySettings.ExportMarkers;
        editPrefixCode.Text = assemblySettings.PrefixCode;
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
      var assemblySettings = Settings.Assembly;
      assemblySettings.PrefixWith = checkExportToDataIncludeRes.Checked;
      assemblySettings.Prefix = editPrefix.Text ?? "";
      assemblySettings.WrapAt = checkExportToDataWrap.Checked;
      assemblySettings.WrapByteCount = GR.Convert.ToI32( editWrapByteCount.Text );
      if ( assemblySettings.WrapByteCount <= 0 )
      {
        assemblySettings.WrapByteCount = 8;
      }
      assemblySettings.ExportHex = checkExportHex.Checked;
      assemblySettings.VariableNameLabelPrefixEnabled = checkVariableNameLabelPrefix.Checked;
      assemblySettings.VariableNameLabelPrefix = editVariableNameLabelPrefix.Text ?? "";
      assemblySettings.IncludeSemicolonAfterSimpleLabels = checkIncludeSemicolonAfterSimpleLabels.Checked;
      assemblySettings.MapSizeCommentEnabled = checkCommentCharacters.Checked;
      assemblySettings.CommentChars = editCommentCharacters.Text ?? "";
      assemblySettings.EmptyTileCompressionEnabled = checkEmptyTile.Checked;
      assemblySettings.EmptyTileIndex = GR.Convert.ToI32( editEmptyTileIndex.Text );
      assemblySettings.SaveOnExport = checkSaveOnExport.Checked;
      assemblySettings.ExportTilesetColors = checkExportTilesetColors.Checked;
      assemblySettings.ExportMapColors = checkExportMapColors.Checked;
      assemblySettings.AddFilenamespace = checkAddFilenamespace.Checked;
      assemblySettings.Filenamespace = editFilenamespace.Text;
      assemblySettings.ExportDirectory = editExportDirectory.Text;
      assemblySettings.ExportFilename = editExportFilename.Text;
      assemblySettings.WrapMapData = checkWrapMapData.Checked;
      // ExportCharset / CharsetExportFilename are dead since the per-charset
      // export name + checkbox moved to the Character Set tab; they keep their
      // loaded values so the settings chunk layout stays stable.
      assemblySettings.CharsetExportDirectory = editCharsetExportDirectory.Text;
      
      Settings.CharsetBinary.PrefixLoadAddress = checkCharsetPrefixLoadAddress.Checked;
      Settings.CharsetBinary.PrefixLoadAddressHex = editCharsetPrefixLoadAddress.Text;

      assemblySettings.AlwaysOverwrite = checkAlwaysOverwrite.Checked;
      assemblySettings.ExportMapAsCharAndColors = checkExportMapAsCharAndColors.Checked;
      assemblySettings.ExportPassableBitfields = checkExportPassableBitfields.Checked;
      assemblySettings.ExportPassableBitfieldsAsBinary = checkExportPassableBitfieldsAsBinary.Checked;
      assemblySettings.ExportMarkers = checkExportMarkers.Checked;
      assemblySettings.PrefixCode = editPrefixCode.Text;
    }


    private string ApplyLabelFormatting( string Source )
    {
      if ( string.IsNullOrEmpty( Source ) )
      {
        return Source;
      }

      string rawLabelPrefix = editVariableNameLabelPrefix.Text ?? "";
      string labelPrefix = rawLabelPrefix.Trim();
      bool useLabelPrefix = checkVariableNameLabelPrefix.Checked && ( labelPrefix.Length > 0 );
      bool includeLabelSuffix = checkIncludeSemicolonAfterSimpleLabels.Checked;

      if ( !useLabelPrefix && !includeLabelSuffix )
      {
        return Source;
      }

      bool endsWithNewLine = Source.EndsWith( "\n" );
      var sb = new StringBuilder();
      using ( var reader = new StringReader( Source ) )
      {
        string line;
        while ( ( line = reader.ReadLine() ) != null )
        {
          string trimmedStart = line.TrimStart();
          string trimmedLine = trimmedStart.TrimEnd();
          string modifiedLine = line;
          bool hasLeadingWhitespace = line.Length != trimmedStart.Length;

          if ( !string.IsNullOrEmpty( trimmedLine ) )
          {
            if ( useLabelPrefix && !hasLeadingWhitespace && IsEquateLine( trimmedLine ) )
            {
              string leadingWhitespace = line.Substring( 0, line.Length - trimmedStart.Length );
              modifiedLine = leadingWhitespace + labelPrefix + " " + trimmedStart;
            }
            else if ( includeLabelSuffix && !hasLeadingWhitespace && IsSimpleLabelLine( trimmedLine ) )
            {
              string leadingWhitespace = line.Substring( 0, line.Length - trimmedStart.Length );
              modifiedLine = leadingWhitespace + trimmedLine + ":";
            }
          }

          sb.AppendLine( modifiedLine );
        }
      }

      if ( !endsWithNewLine && sb.Length >= Environment.NewLine.Length )
      {
        sb.Length -= Environment.NewLine.Length;
      }
      return sb.ToString();
    }

    private bool IsEquateLine( string TrimmedLine )
    {
      if ( string.IsNullOrEmpty( TrimmedLine ) )
      {
        return false;
      }
      char firstChar = TrimmedLine[0];
      if ( ( firstChar == ';' )
      ||   ( firstChar == '.' )
      ||   ( firstChar == '#' )
      ||   ( firstChar == '/' )
      ||   ( firstChar == '!' ) )
      {
        return false;
      }

      string lineToCheck = TrimmedLine;
      int commentPos1 = lineToCheck.IndexOf( ';' );
      int commentPos2 = lineToCheck.IndexOf( "//" );
      int commentPos = -1;
      if ( commentPos1 != -1 ) commentPos = commentPos1;
      if ( commentPos2 != -1 && ( commentPos == -1 || commentPos2 < commentPos ) ) commentPos = commentPos2;
      
      if ( commentPos != -1 )
      {
        lineToCheck = lineToCheck.Substring( 0, commentPos );
      }
      
      return lineToCheck.Contains( "=" );
    }

    private bool IsSimpleLabelLine( string TrimmedLine )
    {
      if ( string.IsNullOrEmpty( TrimmedLine ) )
      {
        return false;
      }
      char firstChar = TrimmedLine[0];
      if ( ( firstChar == ';' )
      ||   ( firstChar == '.' )
      ||   ( firstChar == '#' )
      ||   ( firstChar == '/' )
      ||   ( firstChar == '!' ) )
      {
        return false;
      }

      string lineToCheck = TrimmedLine;
      int commentPos1 = lineToCheck.IndexOf( ';' );
      int commentPos2 = lineToCheck.IndexOf( "//" );
      int commentPos = -1;
      if ( commentPos1 != -1 ) commentPos = commentPos1;
      if ( commentPos2 != -1 && ( commentPos == -1 || commentPos2 < commentPos ) ) commentPos = commentPos2;
      
      if ( commentPos != -1 )
      {
        lineToCheck = lineToCheck.Substring( 0, commentPos );
      }
      lineToCheck = lineToCheck.TrimEnd();
      
      if ( string.IsNullOrEmpty( lineToCheck ) )
      {
        return false;
      }
      if ( lineToCheck.EndsWith( ":" ) )
      {
        return false;
      }
      if ( lineToCheck.Contains( "=" ) )
      {
        return false;
      }
      for ( int i = 0; i < lineToCheck.Length; ++i )
      {
        if ( char.IsWhiteSpace( lineToCheck[i] ) )
        {
          return false;
        }
      }
      return true;
    }



  }
}
