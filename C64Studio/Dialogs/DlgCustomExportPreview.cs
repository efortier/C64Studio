using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Shows what one custom export renders to - a dry run of the game-binary
  /// export, nothing written - or its compile errors, with Save (that one
  /// file, after naming the full path), Copy and Close. Double-clicking an
  /// error line asks the editor to go to that line of the template.
  /// </summary>
  public partial class DlgCustomExportPreview : Form
  {
    private string                  m_OutputPath = "";
    private string                  m_RenderedText = "";
    private bool                    m_ShowsErrors = false;
    private static readonly Regex   s_LineNumber = new Regex( @"line (\d+), col \d+:" );



    /// <summary>Raised with the 1-based template line of a double-clicked error.</summary>
    public event Action<int>        ErrorLineActivated;



    /// <summary>Errors (when any) replace the rendered text; Save is then off.</summary>
    public DlgCustomExportPreview( StudioCore Core, string DefinitionName, string Info, string OutputPath, string RenderedText, List<string> Errors )
    {
      InitializeComponent();

      Text = "Custom export preview - " + DefinitionName;
      m_OutputPath = OutputPath ?? "";
      labelInfo.Text = Info ?? "";
      if ( ( Errors != null )
      &&   ( Errors.Count > 0 ) )
      {
        m_ShowsErrors = true;
        editOutput.Text = string.Join( "\r\n", Errors.ToArray() );
        btnSave.Enabled = false;
        btnCopy.Enabled = false;
      }
      else
      {
        m_RenderedText = RenderedText ?? "";
        editOutput.Text = m_RenderedText;
        btnSave.Enabled = m_OutputPath.Length > 0;
      }
      editOutput.Select( 0, 0 );
      ActiveControl = btnClose;

      Core.Theming.ApplyTheme( this );
    }



    private void btnSave_Click( DecentForms.ControlBase Sender )
    {
      if ( MessageBox.Show( this, "Write this file?\r\n\r\n" + m_OutputPath, "Save custom export",
                            MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1 ) != DialogResult.OK )
      {
        return;
      }
      try
      {
        string folder = System.IO.Path.GetDirectoryName( m_OutputPath );
        if ( ( !string.IsNullOrEmpty( folder ) )
        &&   ( !System.IO.Directory.Exists( folder ) ) )
        {
          if ( MessageBox.Show( this, "The folder does not exist:\r\n\r\n" + folder + "\r\n\r\nCreate it?", "Save custom export",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1 ) != DialogResult.Yes )
          {
            return;
          }
          System.IO.Directory.CreateDirectory( folder );
        }
        System.IO.File.WriteAllText( m_OutputPath, m_RenderedText, new System.Text.UTF8Encoding( false ) );
        labelInfo.Text = "Written: " + m_OutputPath + " (" + System.Text.Encoding.UTF8.GetByteCount( m_RenderedText ) + " bytes)";
      }
      catch ( Exception ex )
      {
        MessageBox.Show( this, "Could not write the file:\r\n" + m_OutputPath + "\r\n\r\n" + ex.Message, "Save custom export",
                         MessageBoxButtons.OK, MessageBoxIcon.Error );
      }
    }



    private void btnCopy_Click( DecentForms.ControlBase Sender )
    {
      try
      {
        if ( editOutput.Text.Length > 0 )
        {
          Clipboard.SetText( editOutput.Text );
        }
      }
      catch ( Exception )
      {
        // the clipboard is busy - nothing to do about it
      }
    }



    private void btnClose_Click( DecentForms.ControlBase Sender )
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }



    private void editOutput_DoubleClick( object sender, EventArgs e )
    {
      if ( ( !m_ShowsErrors )
      ||   ( ErrorLineActivated == null ) )
      {
        return;
      }
      int lineIndex = editOutput.GetLineFromCharIndex( editOutput.SelectionStart );
      if ( ( lineIndex < 0 )
      ||   ( lineIndex >= editOutput.Lines.Length ) )
      {
        return;
      }
      var match = s_LineNumber.Match( editOutput.Lines[lineIndex] );
      int line;
      if ( ( match.Success )
      &&   int.TryParse( match.Groups[1].Value, out line ) )
      {
        ErrorLineActivated( line );
      }
    }
  }
}
