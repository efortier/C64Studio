using System;
using System.Windows.Forms;

namespace RetroDevStudio.Dialogs
{
  public partial class FormInputText : Form
  {
    public string InputText { get; private set; }
    private Func<string, bool> _validator;



    public FormInputText( StudioCore Core, string Title, string Prompt,
                          string DefaultValue = "",
                          Func<string, bool> Validator = null )
    {
      InitializeComponent();

      Text = Title;
      labelPrompt.Text = Prompt;
      editInput.Text = DefaultValue;
      if ( Validator != null )
      {
        _validator = Validator;
      }
      else
      {
        _validator = ( s => !string.IsNullOrEmpty( s ) && s.Trim().Length > 0 );
      }

      Core.Theming.ApplyTheme( this );

      ValidateInput();
      editInput.Select();
      editInput.SelectAll();
    }



    private void btnOK_Click( DecentForms.ControlBase Sender )
    {
      InputText = editInput.Text;
      DialogResult = DialogResult.OK;
      Close();
    }



    private void btnCancel_Click( DecentForms.ControlBase Sender )
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }



    private void editInput_TextChanged( object sender, EventArgs e )
    {
      ValidateInput();
    }



    private void ValidateInput()
    {
      if ( _validator != null )
      {
        btnOK.Enabled = _validator( editInput.Text );
      }
    }



  }
}
