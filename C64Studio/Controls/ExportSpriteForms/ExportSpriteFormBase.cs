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
  public partial class ExportSpriteFormBase : UserControl
  {
    public StudioCore                   Core = null;

    /// <summary>
    /// Raised when the user changes any export setting on the form, so the host
    /// editor can flush the values into the project and mark the document
    /// modified. Derived forms call RaiseSettingsChanged() from their input
    /// handlers (and suppress it while programmatically populating).
    /// </summary>
    public event EventHandler           SettingsChanged;



    public ExportSpriteFormBase()
    {
      InitializeComponent();
    }



    public ExportSpriteFormBase( StudioCore Core )
    {
      this.Core         = Core;

      InitializeComponent();
    }



    public virtual bool HandleExport( ExportSpriteInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      return false;
    }



    /// <summary>
    /// Load this export form's UI from the project's persisted settings. Default
    /// no-op; export forms that persist settings override it.
    /// </summary>
    public virtual void ApplyExportSettings( SpriteProject Project )
    {
    }



    /// <summary>
    /// Write this export form's UI back into the project's persisted settings.
    /// Default no-op.
    /// </summary>
    public virtual void UpdateExportSettings( SpriteProject Project )
    {
    }



    protected void RaiseSettingsChanged()
    {
      SettingsChanged?.Invoke( this, EventArgs.Empty );
    }



  }
}
