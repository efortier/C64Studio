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
  public partial class ExportMapCharsetAsBinaryFile : ExportMapFormBase
  {
    private bool m_ApplyingSettings = false;

    public ExportMapCharsetAsBinaryFile() :
      base( null )
    { 
    }



    public ExportMapCharsetAsBinaryFile( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();
      editPrefixLoadAddress.TextChanged += HandleSettingsChanged;
    }



    public override bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

      saveDlg.FileName = GR.Path.RenameExtension( DocInfo.DocumentFilename, ".chr" );
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
      GR.Memory.ByteBuffer charSet = new GR.Memory.ByteBuffer();

      for ( int i = 0; i < Info.Map.Charset.Characters.Count; ++i )
      {
        charSet.Append( Info.Map.Charset.Characters[i].Tile.Data );
      }
      if ( checkPrefixLoadAddress.Checked )
      {
        ushort address = GR.Convert.ToU16( editPrefixLoadAddress.Text, 16 );

        var addressData = new ByteBuffer();
        addressData.AppendU16( address );
        charSet = addressData + charSet;
      }
      GR.IO.File.WriteAllBytes( saveDlg.FileName, charSet );
      return true;
    }



    private void checkPrefixLoadAddress_CheckedChanged(object sender, EventArgs e)
    {
      editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
      if ( !m_ApplyingSettings )
      {
        RaiseSettingsChanged();
      }
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
        var binarySettings = Settings.CharsetBinary;
        checkPrefixLoadAddress.Checked = binarySettings.PrefixLoadAddress;
        editPrefixLoadAddress.Text = binarySettings.PrefixLoadAddressHex ?? "";
        editPrefixLoadAddress.Enabled = checkPrefixLoadAddress.Checked;
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
      var binarySettings = Settings.CharsetBinary;
      binarySettings.PrefixLoadAddress = checkPrefixLoadAddress.Checked;
      binarySettings.PrefixLoadAddressHex = editPrefixLoadAddress.Text ?? "";
    }



  }
}
