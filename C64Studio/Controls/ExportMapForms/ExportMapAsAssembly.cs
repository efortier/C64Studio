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
      editPrefix.TextChanged += HandleSettingsChanged;
      editWrapByteCount.TextChanged += HandleSettingsChanged;
      checkExportHex.CheckedChanged += HandleSettingsChanged;
      editVariableNameLabelPrefix.TextChanged += HandleSettingsChanged;
      checkIncludeSemicolonAfterSimpleLabels.CheckedChanged += HandleSettingsChanged;
      editCommentCharacters.TextChanged += HandleSettingsChanged;
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
        Info.Map.ExportMapsAsAssembly( !Info.RowByRow, out mapData, "", checkExportToDataWrap.Checked, GR.Convert.ToI32( editWrapByteCount.Text ), prefix, editCommentCharacters.Text ?? "" );
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
      }

      resultText = ApplyLabelFormatting( resultText );
      EditOutput.Text = resultText;
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
        editPrefix.Enabled = checkExportToDataIncludeRes.Checked;
        editWrapByteCount.Enabled = checkExportToDataWrap.Checked;
        editVariableNameLabelPrefix.Enabled = checkVariableNameLabelPrefix.Checked;
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
      assemblySettings.CommentChars = editCommentCharacters.Text ?? "";
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
