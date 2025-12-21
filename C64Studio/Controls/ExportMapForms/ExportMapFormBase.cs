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
  public partial class ExportMapFormBase : UserControl
  {
    public StudioCore                   Core = null;
    public event EventHandler           SettingsChanged;



    public ExportMapFormBase()
    {
      InitializeComponent();
    }



    public ExportMapFormBase( StudioCore Core )
    {
      this.Core         = Core;

      InitializeComponent();
    }



    public virtual bool HandleExport( ExportMapInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      return false;
    }

    public virtual void ApplyExportSettings( MapProject.ExportSettings Settings )
    {
    }

    public virtual void UpdateExportSettings( MapProject.ExportSettings Settings )
    {
    }

    protected void RaiseSettingsChanged()
    {
      SettingsChanged?.Invoke( this, EventArgs.Empty );
    }



  }
}
