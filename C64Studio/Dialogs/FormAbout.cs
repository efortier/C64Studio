using GR.Image;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Dialogs
{
  public partial class FormAbout : Form
  {
    public FormAbout( StudioCore Core )
    {
      InitializeComponent();

      labelInfo.Text = labelInfo.Text.Replace( "<v>", StudioCore.StudioVersion + "." + Version.BuildNumber );

      labelBuildDate.Text = GetBuildDateText();

      pictureBox1.Image = pictureBox1.Image.GetImageStretchedDPI();
      pictureBox2.Image = pictureBox2.Image.GetImageStretchedDPI();

      Core.Theming.ApplyTheme( this );
    }



    private static string GetBuildDateText()
    {
      // Use the last-write timestamp of the currently executing assembly file.
      // This works for self-contained and framework-dependent builds alike, and
      // does not require baking a constant into the assembly at compile time.
      try
      {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        string path = asm.Location;
        if ( !string.IsNullOrEmpty( path ) && System.IO.File.Exists( path ) )
        {
          DateTime dt = System.IO.File.GetLastWriteTime( path );
          return "Built " + dt.ToString( "yyyy-MM-dd HH:mm" );
        }
      }
      catch ( Exception )
      {
        // fall through to empty string — the label just stays blank
      }
      return "";
    }



    private void btnOK_Click( DecentForms.ControlBase Sender )
    {
      Close();
    }

  }
}
