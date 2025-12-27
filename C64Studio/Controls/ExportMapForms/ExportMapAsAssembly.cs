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

    private void checkExportSparseMaps_CheckedChanged( object sender, EventArgs e )
    {
      checkWrapMapData.Enabled = !checkExportSparseMaps.Checked;
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
        checkAlwaysOverwrite.Enabled = checkExportCharset.Checked;
      }

      if (!m_ApplyingSettings)
      {
        RaiseSettingsChanged();
      }
    }

    private void checkExportCharset_CheckedChanged(object sender, EventArgs e)
    {
      editCharsetExportDirectory.Enabled = checkExportCharset.Checked;
      btnBrowseCharsetExportDirectory.Enabled = checkExportCharset.Checked;
      editCharsetExportFilename.Enabled = checkExportCharset.Checked;
      if ( checkExportCharset.Checked )
      {
        checkAlwaysOverwrite.Enabled = true;
      }
      else
      {
        checkAlwaysOverwrite.Enabled = checkSaveOnExport.Checked;
      }

      if ( ( checkExportCharset.Checked )
      &&   ( string.IsNullOrEmpty( editCharsetExportDirectory.Text ) ) )
      {
        if ( Core.MainForm.ActiveDocument != null )
        {
          editCharsetExportDirectory.Text = System.IO.Path.GetDirectoryName( Core.MainForm.ActiveDocument.DocumentInfo.DocumentFilename );
        }
      }
      if ( ( checkExportCharset.Checked )
      &&   ( string.IsNullOrEmpty( editCharsetExportFilename.Text ) ) )
      {
        if ( Core.MainForm.ActiveDocument != null )
        {
          editCharsetExportFilename.Text = System.IO.Path.GetFileNameWithoutExtension( Core.MainForm.ActiveDocument.DocumentInfo.DocumentFilename ) + ".bin";
        }
      }

      if (!m_ApplyingSettings)
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
        Info.Map.ExportTilesAsElements( out tileData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix );
      }
      if ( ( Info.ExportType == MapExportType.TILE_DATA )
      ||   ( Info.ExportType == MapExportType.TILE_AND_MAP_DATA ) )
      {
        Info.Map.ExportTilesAsAssembly( out tileData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix );
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
        Info.Map.ExportSparseTileAndMapData( out mapData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, checkEmptyTile.Checked, GR.Convert.ToI32( editEmptyTileIndex.Text ), checkAddFilenamespace.Checked, editFilenamespace.Text, checkExportSparseMaps.Checked, checkWrapMapData.Checked );
      }
      if ( checkExportCharset.Checked )
      {
        GR.Memory.ByteBuffer charData = Info.Map.Charset.CharacterData();
        
        if ( !string.IsNullOrEmpty( editCharsetExportFilename.Text ) )
        {
          byte[] rawData = new byte[charData.Length];
          for ( int i = 0; i < charData.Length; ++i )
          {
            rawData[i] = charData.ByteAt( i );
          }

          string    fullPath = editCharsetExportFilename.Text;
          string    exportDirectory = editCharsetExportDirectory.Text;
          
          if ( string.IsNullOrEmpty( exportDirectory ) )
          {
            fullPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( DocInfo.FullPath ), editCharsetExportFilename.Text );
          }
          else
          {
            try
            {
               fullPath = System.IO.Path.Combine( exportDirectory, editCharsetExportFilename.Text );
            }
            catch ( Exception )
            {
              // invalid path combination?
              fullPath = editCharsetExportFilename.Text;
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
            System.IO.File.WriteAllBytes( fullPath, rawData );
          }
          catch ( Exception ex )
          {
            Core.Notification.MessageBox( "Error saving file", "Could not save exported char set file:\r\n" + ex.Message );
          }
        }
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
        checkExportSparseMaps.Checked = assemblySettings.ExportSparseMaps;
        checkWrapMapData.Checked = assemblySettings.WrapMapData;
        checkWrapMapData.Enabled = !checkExportSparseMaps.Checked;

        checkExportCharset.Checked = assemblySettings.ExportCharset;
        editCharsetExportDirectory.Text = assemblySettings.CharsetExportDirectory;
        editCharsetExportFilename.Text = assemblySettings.CharsetExportFilename;
        
        editCharsetExportDirectory.Enabled = checkExportCharset.Checked;
        btnBrowseCharsetExportDirectory.Enabled = checkExportCharset.Checked;
        editCharsetExportFilename.Enabled = checkExportCharset.Checked;

        checkAlwaysOverwrite.Checked = assemblySettings.AlwaysOverwrite;
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
      assemblySettings.ExportSparseMaps = checkExportSparseMaps.Checked;
      assemblySettings.WrapMapData = checkWrapMapData.Checked;
      assemblySettings.ExportCharset = checkExportCharset.Checked;
      assemblySettings.CharsetExportDirectory = editCharsetExportDirectory.Text;
      assemblySettings.CharsetExportFilename = editCharsetExportFilename.Text;
      assemblySettings.AlwaysOverwrite = checkAlwaysOverwrite.Checked;
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
      return TrimmedLine.Contains( "=" );
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
      if ( TrimmedLine.EndsWith( ":" ) )
      {
        return false;
      }
      if ( TrimmedLine.Contains( "=" ) )
      {
        return false;
      }
      for ( int i = 0; i < TrimmedLine.Length; ++i )
      {
        if ( char.IsWhiteSpace( TrimmedLine[i] ) )
        {
          return false;
        }
      }
      return true;
    }



  }
}
