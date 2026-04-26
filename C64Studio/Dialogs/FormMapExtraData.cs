using System;
using System.Windows.Forms;

namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Multi-line editor for the map's free-form "extra data" string. Lives
  /// on the right-side panel as a constantly-visible textbox before; now
  /// reached via the Tools → "Edit extra data..." menu item to free up
  /// real estate for more useful per-map controls.
  /// </summary>
  public partial class FormMapExtraData : Form
  {
    public string ExtraData { get; private set; }



    public FormMapExtraData( StudioCore Core, string Initial )
    {
      InitializeComponent();

      Text = "Map extra data";
      ExtraData = Initial ?? "";
      editExtraData.Text = ExtraData;

      Core.Theming.ApplyTheme( this );

      editExtraData.Select();
      editExtraData.SelectionStart = editExtraData.Text.Length;
    }



    private void btnOK_Click( DecentForms.ControlBase Sender )
    {
      ExtraData = editExtraData.Text;
      DialogResult = DialogResult.OK;
      Close();
    }



    private void btnCancel_Click( DecentForms.ControlBase Sender )
    {
      DialogResult = DialogResult.Cancel;
      Close();
    }
  }
}
