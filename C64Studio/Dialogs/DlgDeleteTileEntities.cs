using System;
using System.Windows.Forms;



namespace RetroDevStudio.Dialogs
{
  /// <summary>
  /// Asked before deleting tiles that placed entities are drawn with: keep
  /// the tiles (Cancel), delete them anyway (the entities stay, without a
  /// picture), or delete them together with those entities.
  /// </summary>
  public partial class DlgDeleteTileEntities : Form
  {
    public enum Choice
    {
      CANCEL,
      DELETE_TILE_ONLY,
      DELETE_TILE_AND_ENTITIES
    }



    public Choice ChosenAction
    {
      get;
      private set;
    }



    public DlgDeleteTileEntities( StudioCore Core, string Headline, string Details, bool SeveralTiles )
    {
      ChosenAction = Choice.CANCEL;

      InitializeComponent();

      labelHeadline.Text = Headline;
      editDetails.Text   = Details;
      if ( SeveralTiles )
      {
        btnDeleteTileAndEntities.Text = "Delete tiles and entities";
        btnDeleteTileOnly.Text        = "Delete tiles anyway";
      }
      // Nothing destructive happens by accident: the focus starts on Cancel.
      ActiveControl = btnCancel;

      Core.Theming.ApplyTheme( this );
    }



    private void btnDeleteTileAndEntities_Click( DecentForms.ControlBase Sender )
    {
      ChooseAndClose( Choice.DELETE_TILE_AND_ENTITIES, DialogResult.OK );
    }



    private void btnDeleteTileOnly_Click( DecentForms.ControlBase Sender )
    {
      ChooseAndClose( Choice.DELETE_TILE_ONLY, DialogResult.OK );
    }



    private void btnCancel_Click( DecentForms.ControlBase Sender )
    {
      ChooseAndClose( Choice.CANCEL, DialogResult.Cancel );
    }



    private void ChooseAndClose( Choice Action, DialogResult Result )
    {
      ChosenAction = Action;
      DialogResult = Result;
      Close();
    }



  }
}
