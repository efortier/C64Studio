using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace RetroDevStudio.Dialogs
{
  public partial class DlgDeactivatableMessage : Form
  {
    public enum UserChoice
    {
      OK,
      CANCEL,
      YES,
      NO
    }

    public enum MessageButtons
    {
      OK,
      OK_ALL,
      OK_CANCEL,
      YES_NO,
      YES_NO_ALL,
      YES_NO_CANCEL,
      YES_NO_CANCEL_ALL
    }




    public UserChoice ChosenResult
    {
      get;
      private set;
    }



    public bool StoreChoice
    {
      get;
      private set;
    }



    private MessageButtons  _buttons = MessageButtons.OK;
    private bool            _isClosing = false;



    public DlgDeactivatableMessage( MessageButtons buttons, string caption, string message, StudioCore core )
    {
      ChosenResult  = UserChoice.CANCEL;
      _buttons      = buttons;
      StoreChoice   = false;

      switch ( buttons )
      {
        case MessageButtons.OK:
        case MessageButtons.OK_ALL:
          InitializeComponent();
          btnYes.Visible              = false;
          btnNo.Visible               = true;
          btnNo.Text                  = "OK";
          btnNo.Tag                   = UserChoice.OK;
          btnCancel.Visible           = false;
          btnCancel.Tag               = UserChoice.CANCEL;
          ControlBox                  = false;
          checkRememberDecision.Text  = "Don't show this again";
          checkRememberDecision.Visible = ( buttons == MessageButtons.OK_ALL );
          break;
        case MessageButtons.OK_CANCEL:
          InitializeComponent();
          btnYes.Visible              = true;
          btnYes.Text                 = "OK";
          btnYes.Tag                  = UserChoice.OK;
          btnNo.Visible               = false;
          btnCancel.Visible           = true;
          btnCancel.Tag               = UserChoice.CANCEL;
          checkRememberDecision.Visible = false;
          CenterTwoButtons();
          break;
        case MessageButtons.YES_NO:
        case MessageButtons.YES_NO_ALL:
          InitializeComponent();
          btnYes.Visible    = true;
          btnYes.Tag        = UserChoice.YES;
          btnNo.Visible     = false;
          btnCancel.Text    = "No";
          btnCancel.Tag     = UserChoice.NO;
          ControlBox        = false;
          CenterTwoButtons();
          checkRememberDecision.Visible = ( buttons == MessageButtons.YES_NO_ALL );
          break;
        case MessageButtons.YES_NO_CANCEL:
        case MessageButtons.YES_NO_CANCEL_ALL:
          InitializeComponent();
          btnYes.Visible    = true;
          btnYes.Tag        = UserChoice.YES;
          btnNo.Visible     = true;
          btnNo.Tag         = UserChoice.NO;
          btnCancel.Visible = true;
          btnCancel.Tag     = UserChoice.CANCEL;
          checkRememberDecision.Visible = ( buttons == MessageButtons.YES_NO_CANCEL_ALL );
          break;
        default:
          throw new ArgumentOutOfRangeException( nameof( buttons ), buttons, null );
      }
      labelImageInfo.Text = message;
      Text                = caption;

      if ( ( !btnCancel.Visible )
      &&   ( buttons != MessageButtons.OK )
      &&   ( buttons != MessageButtons.OK_ALL ) )
      {
        CancelButton = null;
      }
      core.Theming.ApplyTheme( this );
    }



    private void CenterTwoButtons()
    {
      // only btnYes and btnCancel are visible
      int distance = btnNo.Left - btnYes.Right;

      int center  = ( Width - btnYes.Width - btnCancel.Width - distance ) / 2;
      btnYes.Left = center;
      btnCancel.Left  = center + btnYes.Width + distance;
    }



    private void btn_Click( DecentForms.ControlBase Sender )
    {
      ChooseAndClose( (UserChoice)Sender.Tag );
    }



    private void ChooseAndClose( UserChoice Choice )
    {
      ChosenResult = Choice;
      DialogResult = DialogResult.OK;

      if ( checkRememberDecision.Visible )
      {
        StoreChoice = checkRememberDecision.Checked;
      }
      _isClosing = true;
      Close();
    }



    /// <summary>
    /// Activate the visible button whose Tag matches <paramref name="Choice"/>.
    /// Returns false when no such button is shown.
    /// </summary>
    private bool TryChoose( UserChoice Choice )
    {
      foreach ( var btn in new DecentForms.Button[] { btnYes, btnNo, btnCancel } )
      {
        if ( ( btn != null )
        &&   ( btn.Visible )
        &&   ( btn.Tag is UserChoice )
        &&   ( (UserChoice)btn.Tag == Choice ) )
        {
          ChooseAndClose( Choice );
          return true;
        }
      }
      return false;
    }



    // Keyboard accelerators: Y = Yes/OK, N = No, Esc = Cancel. The DecentForms
    // buttons are not native IButtonControls and carry no mnemonics, so none of
    // these keys work on their own. ProcessCmdKey runs before dialog-key handling
    // and regardless of which child has focus, so it is the robust hook.
    protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
    {
      if ( keyData == Keys.Y )
      {
        if ( TryChoose( UserChoice.YES ) || TryChoose( UserChoice.OK ) )
        {
          return true;
        }
      }
      else if ( keyData == Keys.N )
      {
        if ( TryChoose( UserChoice.NO ) )
        {
          return true;
        }
      }
      else if ( keyData == Keys.Escape )
      {
        if ( TryChoose( UserChoice.CANCEL ) )
        {
          return true;
        }
      }
      return base.ProcessCmdKey( ref msg, keyData );
    }



    private void DlgDeactivatableMessage_FormClosing( object sender, FormClosingEventArgs e )
    {
      if ( _isClosing )
      {
        return;
      }
      switch ( _buttons )
      {
        case MessageButtons.OK:
        case MessageButtons.OK_ALL:
          ChosenResult = UserChoice.OK;
          DialogResult = DialogResult.OK;
          Close();
          break;
        case MessageButtons.YES_NO_CANCEL:
        case MessageButtons.YES_NO_CANCEL_ALL:
        case MessageButtons.OK_CANCEL:
          ChosenResult = UserChoice.CANCEL;
          DialogResult = DialogResult.Cancel;
          Close();
          break;
        default:
          e.Cancel = true;
          break;
      }
    }



  }
}
