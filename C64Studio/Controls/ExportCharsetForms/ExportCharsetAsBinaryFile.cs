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
  public partial class ExportCharsetAsBinaryFile : ExportCharsetFormBase
  {
    private CharsetProject m_BoundCharset = null;



    public ExportCharsetAsBinaryFile() :
      base( null )
    {
    }



    public ExportCharsetAsBinaryFile( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();
    }



    public override void LoadSettings( CharsetProject Charset )
    {
      m_BoundCharset = Charset;
      if ( Charset == null ) return;

      // Populate with the persistence handlers detached, so loading the
      // values never dirties the project; they are hooked (once) below.
      DetachPersistHandlers();
      checkPrefixLoadAddress.Checked  = Charset.ExportBinaryPrefixLoadAddress;
      editPrefixLoadAddress.Text      = Charset.ExportBinaryLoadAddress.ToString( "X4" );
      editPrefixLoadAddress.Enabled   = checkPrefixLoadAddress.Checked;

      checkCompress.Checked           = Charset.ExportBinaryCompress;
      // The stored compressor, else the first one offered.
      comboCompressor.SelectedIndex   = comboCompressor.Items.IndexOf( Charset.ExportBinaryCompressor ?? "ZX0" );
      if ( comboCompressor.SelectedIndex < 0 )
      {
        comboCompressor.SelectedIndex = ( comboCompressor.Items.Count > 0 ) ? 0 : -1;
      }
      editCompressFilename.Text        = Charset.ExportBinaryCompressFilename ?? "";
      checkOverrideLoadAddress.Checked = Charset.ExportBinaryOverrideLoadAddress;
      editOverrideLoadAddress.Text     = Charset.ExportBinaryOverrideLoadAddressHex ?? "";
      UpdateCompressEnabledState();

      AttachPersistHandlers();
    }



    private void AttachPersistHandlers()
    {
      checkPrefixLoadAddress.CheckedChanged   += checkPrefixLoadAddress_PersistChanged;
      editPrefixLoadAddress.TextChanged       += editPrefixLoadAddress_PersistChanged;
      checkCompress.CheckedChanged            += checkCompress_PersistChanged;
      comboCompressor.SelectedIndexChanged    += comboCompressor_PersistChanged;
      editCompressFilename.TextChanged        += editCompressFilename_PersistChanged;
      checkOverrideLoadAddress.CheckedChanged += checkOverrideLoadAddress_PersistChanged;
      editOverrideLoadAddress.TextChanged     += editOverrideLoadAddress_PersistChanged;
    }



    private void DetachPersistHandlers()
    {
      checkPrefixLoadAddress.CheckedChanged   -= checkPrefixLoadAddress_PersistChanged;
      editPrefixLoadAddress.TextChanged       -= editPrefixLoadAddress_PersistChanged;
      checkCompress.CheckedChanged            -= checkCompress_PersistChanged;
      comboCompressor.SelectedIndexChanged    -= comboCompressor_PersistChanged;
      editCompressFilename.TextChanged        -= editCompressFilename_PersistChanged;
      checkOverrideLoadAddress.CheckedChanged -= checkOverrideLoadAddress_PersistChanged;
      editOverrideLoadAddress.TextChanged     -= editOverrideLoadAddress_PersistChanged;
    }



    private void checkPrefixLoadAddress_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryPrefixLoadAddress = checkPrefixLoadAddress.Checked;
      OnSettingsChanged();
    }



    private void editPrefixLoadAddress_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryLoadAddress = GR.Convert.ToU16( editPrefixLoadAddress.Text, 16 );
      OnSettingsChanged();
    }



    private void checkCompress_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryCompress = checkCompress.Checked;
      OnSettingsChanged();
    }



    private void comboCompressor_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryCompressor = SelectedCompressor();
      OnSettingsChanged();
    }



    private void editCompressFilename_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryCompressFilename = editCompressFilename.Text ?? "";
      OnSettingsChanged();
    }



    private void checkOverrideLoadAddress_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryOverrideLoadAddress = checkOverrideLoadAddress.Checked;
      OnSettingsChanged();
    }



    private void editOverrideLoadAddress_PersistChanged( object sender, EventArgs e )
    {
      if ( m_BoundCharset == null ) return;
      m_BoundCharset.ExportBinaryOverrideLoadAddressHex = editOverrideLoadAddress.Text ?? "";
      OnSettingsChanged();
    }



    private string SelectedCompressor()
    {
      return ( comboCompressor.SelectedItem != null ) ? comboCompressor.SelectedItem.ToString() : "ZX0";
    }



    /// <summary>
    /// Writes the character set, then - with "Compress character set" on and
    /// a file name - a compressed copy beside it, a SECOND file (the
    /// exported file itself stays). A bad load-address override or a
    /// compressed name that would overwrite the export aborts before
    /// anything is written.
    /// </summary>
    public override bool HandleExport( ExportCharsetInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      bool compress        = checkCompress.Checked && !string.IsNullOrEmpty( editCompressFilename.Text );
      int  overrideAddress = -1;
      var  errors          = new List<string>();
      if ( compress )
      {
        if ( ( checkOverrideLoadAddress.Checked )
        &&   ( !GameBinaryCompression.TryParseHexAddress( editOverrideLoadAddress.Text, out overrideAddress ) ) )
        {
          errors.Add( "compressed file load address: \"" + ( editOverrideLoadAddress.Text ?? "" ) + "\" is not a valid hex address ($0000-$FFFF)" );
        }
        if ( editCompressFilename.Text.IndexOfAny( System.IO.Path.GetInvalidFileNameChars() ) >= 0 )
        {
          errors.Add( "compressed file name \"" + editCompressFilename.Text + "\" contains characters a file name cannot have" );
        }
      }
      if ( errors.Count > 0 )
      {
        ReportExportAborted( errors );
        return false;
      }

      string targetPath = Info.OutputPath;
      if ( string.IsNullOrEmpty( targetPath ) )
      {
        System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

        saveDlg.FileName = Info.Charset.ExportFilename;
        saveDlg.Title = "Export Charset to";
        saveDlg.Filter = "Charset|*.chr|All Files|*.*";
        if ( DocInfo.Project != null )
        {
          saveDlg.InitialDirectory = DocInfo.Project.Settings.BasePath;
        }
        if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
        {
          return false;
        }
        targetPath = saveDlg.FileName;
      }

      // The compressed copy lands beside the export - never ON it.
      string compressedPath = null;
      if ( compress )
      {
        try
        {
          compressedPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( targetPath ) ?? "", editCompressFilename.Text );
          if ( string.Equals( System.IO.Path.GetFullPath( compressedPath ), System.IO.Path.GetFullPath( targetPath ), StringComparison.OrdinalIgnoreCase ) )
          {
            ReportExportAborted( new List<string>() { "compressed file name " + editCompressFilename.Text + " equals the exported file name - pick a different name" } );
            return false;
          }
        }
        catch ( Exception )
        {
          // a malformed path: the compressor reports it
        }
      }

      GR.Memory.ByteBuffer charSet = new GR.Memory.ByteBuffer();

      List<int>     exportIndices = Info.ExportIndices;
      foreach ( int i in exportIndices )
      {
        charSet.Append( Info.Charset.Characters[i].Tile.Data );
      }
      if ( checkPrefixLoadAddress.Checked )
      {
        ushort address = GR.Convert.ToU16( editPrefixLoadAddress.Text, 16 );

        var addressData = new ByteBuffer();
        addressData.AppendU16( address );
        charSet = addressData + charSet;
      }
      GR.IO.File.WriteAllBytes( targetPath, charSet );
      if ( EditOutput != null )
      {
        EditOutput.Text = "Exported " + charSet.Length + " bytes to " + targetPath;
      }

      if ( compress )
      {
        string report;
        string error;
        if ( GameBinaryCompression.RunCompressor( SelectedCompressor(), targetPath, compressedPath,
                                                  overrideAddress, charSet.Length, out report, out error ) )
        {
          if ( EditOutput != null )
          {
            EditOutput.Text += "\r\n" + report;
          }
        }
        else if ( Core != null )
        {
          Core.Notification.MessageBox( "Compression failed",
            error + "\r\n\r\nThe character set was exported, but not compressed." );
        }
      }
      return true;
    }



    /// <summary>The "nothing was written" box (a test answers it silently).</summary>
    protected virtual void ReportExportAborted( List<string> Errors )
    {
      var sb = new StringBuilder();
      sb.AppendLine( "Export aborted - nothing was written:" );
      sb.AppendLine();
      foreach ( var e in Errors ) sb.AppendLine( "  " + e );
      MessageBox.Show( sb.ToString(), "Character set export error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
    }



    /// <summary>Only the settings beside "Compress character set" follow it - never its group box, so it can always be ticked again.</summary>
    private void UpdateCompressEnabledState()
    {
      comboCompressor.Enabled          = checkCompress.Checked;
      editCompressFilename.Enabled     = checkCompress.Checked;
      checkOverrideLoadAddress.Enabled = checkCompress.Checked;
      editOverrideLoadAddress.Enabled  = checkCompress.Checked && checkOverrideLoadAddress.Checked;
    }



    private void checkCompress_CheckedChanged( object sender, EventArgs e )
    {
      UpdateCompressEnabledState();
    }



    /// <summary>
    /// A user turning compression on gets the load-address override with it -
    /// the packer writes that address into the file as its depack target -
    /// set to $0000 when the box holds no valid address yet. On Click, not
    /// CheckedChanged: LoadSettings restores a project as saved and must not
    /// add an override.
    /// </summary>
    private void checkCompress_Click( object sender, EventArgs e )
    {
      if ( checkCompress.Checked )
      {
        int parsed;
        if ( !GameBinaryCompression.TryParseHexAddress( editOverrideLoadAddress.Text, out parsed ) )
        {
          editOverrideLoadAddress.Text = "$0000";
        }
        checkOverrideLoadAddress.Checked = true;
      }
    }



    private void checkOverrideLoadAddress_CheckedChanged( object sender, EventArgs e )
    {
      editOverrideLoadAddress.Enabled = checkCompress.Checked && checkOverrideLoadAddress.Checked;
    }



    private void checkPrefixLoadAddress_CheckedChanged(object sender, EventArgs e)
    {
      editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
    }



    private void editPrefixLoadAddress_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( ( ( e.KeyChar >= '0' )
      &&     ( e.KeyChar <= '9' ) )
      ||   ( ( e.KeyChar >= 'A' )
      &&     ( e.KeyChar <= 'F' ) )
      ||   ( ( e.KeyChar >= 'a' )
      &&     ( e.KeyChar <= 'f' ) )
      ||   ( char.IsControl( e.KeyChar ) ) )
      {
      }
      else
      {
        e.Handled = true;
      }
    }



  }
}
