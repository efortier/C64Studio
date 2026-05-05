using RetroDevStudio;
using RetroDevStudio.Formats;
using RetroDevStudio.Types;
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
  public partial class ExportCharsetFormBase : UserControl
  {
    public StudioCore                   Core = null;

    /// <summary>
    /// Raised when a per-form persistent setting changes. Lets the host
    /// editor re-mark the project as modified without each form needing
    /// to know about the editor's modified-tracking mechanism.
    /// </summary>
    public event EventHandler           SettingsChanged;



    public ExportCharsetFormBase()
    {
      InitializeComponent();
    }



    public ExportCharsetFormBase( StudioCore Core )
    {
      this.Core         = Core;

      InitializeComponent();
    }



    public virtual bool HandleExport( ExportCharsetInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      return false;
    }



    /// <summary>
    /// Pull form-specific persistent settings (e.g. checkbox/textbox state
    /// like prefix load address) out of the project so the form reflects
    /// the last-saved values on creation. Default implementation is a
    /// no-op for forms that don't have any.
    /// </summary>
    public virtual void LoadSettings( CharsetProject Charset )
    {
    }



    /// <summary>
    /// Raise <see cref="SettingsChanged"/>. Forms call this from their
    /// own change handlers AFTER writing the new value into the project.
    /// </summary>
    protected void OnSettingsChanged()
    {
      var h = SettingsChanged;
      if ( h != null )
      {
        h( this, EventArgs.Empty );
      }
    }



  }
}
