using RetroDevStudio.Types;
using RetroDevStudio.Formats;
using System;
using System.Windows.Forms;
using RetroDevStudio.Documents;
using static RetroDevStudio.Documents.BaseDocument;

namespace RetroDevStudio.Controls
{
  public partial class ExportMapCharsetAsCharset : ExportMapFormBase
  {
    private bool m_ApplyingSettings = false;

    public ExportMapCharsetAsCharset() :
      base( null )
    { 
    }



    public ExportMapCharsetAsCharset( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();

      comboCharsetFiles.Items.Add( new Types.ComboItem( "To new charset project" ) );
      UtilForms.FillComboWithFilesOfType( Core, comboCharsetFiles, ProjectElement.ElementType.CHARACTER_SET );
      
      comboCharsetFiles.SelectedIndex = 0;

      Core.MainForm.ApplicationEvent += new MainForm.ApplicationEventHandler( MainForm_ApplicationEvent );
      comboCharsetFiles.SelectedIndexChanged += comboCharsetFiles_SelectedIndexChanged;
    }



    private void MainForm_ApplicationEvent( ApplicationEvent Event )
    {
      UtilForms.AdaptComboWithFilesOfType( Core, Event, comboCharsetFiles, ProjectElement.ElementType.CHARACTER_SET );
    }



    public override bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      Types.ComboItem comboItem = (Types.ComboItem)comboCharsetFiles.SelectedItem;
      if ( comboItem.Tag == null )
      {
        // to new file
        BaseDocument document = null;
        if ( DocInfo.Project == null )
        {
          document = Core.MainForm.CreateNewDocument( ProjectElement.ElementType.CHARACTER_SET, null );
        }
        else
        {
          document = Core.MainForm.CreateNewElement( ProjectElement.ElementType.CHARACTER_SET, "Character Set", DocInfo.Project ).Document;
        }
        if ( document.DocumentInfo.Element != null )
        {
          string    newFilename = "New Character Set.charsetproject";
          if ( DocInfo.Project != null )
          {
            string    newFilenameTemplate = DocInfo.Project.FullPath( newFilename );

            int     curAttempt = 2;
            while ( ( DocInfo.Project != null )
            &&      ( DocInfo.Project.IsFilenameInUse( newFilename ) ) )
            {
              newFilename = GR.Path.RenameFilenameWithoutExtension( newFilenameTemplate, GR.Path.GetFileNameWithoutExtension( newFilename ) + " " + curAttempt );
              ++curAttempt;
            }
          }
          document.SetDocumentFilename( newFilename );
          document.DocumentInfo.Element.Filename = document.DocumentInfo.DocumentFilename;
        }
        ( (CharsetEditor)document ).OpenProject( Info.Map.Charset.SaveToBuffer() );
        document.SetModified();
      }
      else
      {
        DocumentInfo    docInfo = (DocumentInfo)comboItem.Tag;
        CharsetEditor document = (CharsetEditor)docInfo.BaseDoc;
        if ( document == null )
        {
          if ( docInfo.Project != null )
          {
            docInfo.Project.ShowDocument( docInfo.Element );
            document = (CharsetEditor)docInfo.BaseDoc;
          }
        }
        if ( document != null )
        {
          document.OpenProject( Info.Map.Charset.SaveToBuffer() );
          document.SetModified();
        }
      }

      return true;
    }

    private void comboCharsetFiles_SelectedIndexChanged( object sender, EventArgs e )
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
        int targetIndex = 0;
        string targetFilename = Settings.CharsetProject.TargetFilename;
        if ( !string.IsNullOrEmpty( targetFilename ) )
        {
          for ( int i = 0; i < comboCharsetFiles.Items.Count; ++i )
          {
            var comboItem = (Types.ComboItem)comboCharsetFiles.Items[i];
            var docInfo = comboItem?.Tag as DocumentInfo;
            if ( docInfo == null )
            {
              continue;
            }
            if ( string.Equals( docInfo.DocumentFilename ?? "", targetFilename, StringComparison.OrdinalIgnoreCase )
            ||   string.Equals( docInfo.FullPath ?? "", targetFilename, StringComparison.OrdinalIgnoreCase ) )
            {
              targetIndex = i;
              break;
            }
          }
        }
        if ( comboCharsetFiles.Items.Count > 0 )
        {
          comboCharsetFiles.SelectedIndex = targetIndex;
        }
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
      string targetFilename = "";
      var comboItem = comboCharsetFiles.SelectedItem as Types.ComboItem;
      var docInfo = comboItem?.Tag as DocumentInfo;
      if ( docInfo != null )
      {
        targetFilename = docInfo.DocumentFilename ?? docInfo.FullPath ?? "";
      }
      Settings.CharsetProject.TargetFilename = targetFilename;
    }



  }
}
