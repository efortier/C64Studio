using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;



namespace RetroDevStudio
{
  public class FocusSupport
  {
    [DllImport( "user32.dll" )]
    static extern IntPtr GetFocus();

    public static Control GetFocusedControl()
    {
      IntPtr  wndHandle = GetFocus();
      return Control.FromChildHandle( wndHandle );
    }



    public enum FocusControlReason
    {
      COPY_PASTE,
      ESCAPE
    }



    /// <summary>
    /// Whether the control that currently owns the KEYBOARD would consume a
    /// key pressed for the given reason: text editors (TextBoxBase — which
    /// includes the inner edit hosted by Krypton text boxes and by numeric
    /// spinners) own letters, Delete and the clipboard chords (COPY_PASTE);
    /// combo boxes own Escape to close their dropdown (ESCAPE). Document
    /// shortcuts run only when this returns FALSE.
    ///
    /// Deliberately NOT scoped to any container: keys are delivered to the
    /// focused control no matter which panel it sits on, so only the
    /// control's TYPE matters. The predecessor of this helper
    /// (IsFocusOnChildOfAndCouldAffectReason) answered "blocked" for any
    /// focus OUTSIDE the tab page passed to it — which silently killed
    /// every bare-letter shortcut (G/S/T/P, …) while focus sat on the map
    /// dropdown, the tab header or any other control outside the page.
    /// Callers that used that containment as an implicit "is my page
    /// active" test now check SelectedPage / SelectedTab explicitly.
    /// </summary>
    public static bool FocusedControlUsesKeysFor( FocusControlReason Reason )
    {
      var focusedControl = GetFocusedControl();
      switch ( Reason )
      {
        case FocusControlReason.ESCAPE:
          return focusedControl is ComboBox;
        case FocusControlReason.COPY_PASTE:
        default:
          return focusedControl is TextBoxBase;
      }
    }

  }

}
