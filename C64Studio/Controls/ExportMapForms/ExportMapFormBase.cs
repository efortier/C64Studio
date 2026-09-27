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



    // ---- Character set export helpers shared by the forms -----------------

    /// <summary>Exactly 4 hex digits → address; the per-form parsing that used to be duplicated.</summary>
    protected static bool TryParseCharsetLoadAddress( string Text, out ushort Address )
    {
      Address = 0;
      string addrText = ( Text ?? "" ).Trim();
      if ( ( addrText.Length != 4 )
      ||   ( GR.Convert.ToI32( addrText, 16 ) < 0 ) )
      {
        return false;
      }
      Address = (ushort)GR.Convert.ToI32( addrText, 16 );
      return true;
    }



    /// <summary>
    /// The charset TILE-ONLY exports run from the editor use: the current
    /// map's charset (what the Tiles tab shows), else the Character Set tab's
    /// selection when no map is current.
    /// </summary>
    protected static int EditorCharsetIndex( ExportMapInfo Info )
    {
      if ( ( Info != null )
      &&   ( Info.CurrentMap != null ) )
      {
        return Info.CurrentMap.CharsetIndex;
      }
      return ( ( Info != null ) && ( Info.Map != null ) ) ? Info.Map.CurrentCharsetIndex : 0;
    }



    /// <summary>
    /// The configured directory, else the document's, else FallbackPath's.
    /// Resolved ONCE per export and handed to both the charset files and the
    /// label sidecars so they can never land in different folders.
    /// </summary>
    protected static string ResolveCharsetExportDirectory( string Configured, DocumentInfo DocInfo, string FallbackPath )
    {
      if ( !string.IsNullOrEmpty( Configured ) )
      {
        return Configured;
      }
      try
      {
        if ( ( DocInfo != null )
        &&   ( !string.IsNullOrEmpty( DocInfo.FullPath ) ) )
        {
          string docDir = System.IO.Path.GetDirectoryName( DocInfo.FullPath );
          if ( !string.IsNullOrEmpty( docDir ) )
          {
            return docDir;
          }
        }
      }
      catch ( Exception )
      {
      }
      try
      {
        if ( !string.IsNullOrEmpty( FallbackPath ) )
        {
          return System.IO.Path.GetDirectoryName( FallbackPath );
        }
      }
      catch ( Exception )
      {
      }
      return null;
    }



    /// <summary>
    /// Writes every BuildCharsetExportFiles() entry into Directory — one file
    /// per character set with "Export character set" enabled and a non-empty
    /// export name. Returns a report for the export log (one line per file,
    /// and a line for every enabled set that has no export name). A "No" on
    /// the overwrite prompt skips THAT file and continues with the next.
    /// </summary>
    protected string WriteCharsetExportFiles( MapProject Project, string Directory, bool PrefixLoadAddress, ushort LoadAddress, bool AskBeforeOverwrite )
    {
      var report = new StringBuilder();
      var exported = Project.ExportedCharsets();
      if ( exported.Count == 0 )
      {
        report.AppendLine( "Character sets: none enabled for export." );
        return report.ToString();
      }
      foreach ( var cs in exported )
      {
        if ( string.IsNullOrWhiteSpace( cs.ExportName ) )
        {
          report.AppendLine( "Character set '" + Project.CharsetDisplayNameOf( cs ) + "': no export name, no file written." );
        }
      }
      foreach ( var file in Project.BuildCharsetExportFiles( PrefixLoadAddress, LoadAddress ) )
      {
        string fullPath = file.FileName;
        if ( !string.IsNullOrEmpty( Directory ) )
        {
          try
          {
            fullPath = System.IO.Path.Combine( Directory, file.FileName );
          }
          catch ( Exception )
          {
            fullPath = file.FileName;
          }
        }
        string label = "Character set " + file.ExportIndex + " (" + Project.CharsetDisplayNameAt( file.CharsetIndex ) + ")";
        if ( ( AskBeforeOverwrite )
        &&   ( System.IO.File.Exists( fullPath ) ) )
        {
          if ( MessageBox.Show( "The file " + fullPath + " already exists.\r\nOverwrite?", "File already exists", MessageBoxButtons.YesNo ) == DialogResult.No )
          {
            report.AppendLine( label + " -> " + fullPath + " skipped (not overwritten)." );
            continue;
          }
        }
        try
        {
          GR.IO.File.WriteAllBytes( fullPath, file.Data );
          report.AppendLine( label + " -> " + fullPath + " (" + file.Data.Length + " bytes)" );
        }
        catch ( Exception ex )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Error saving character set",
              "Could not save the character set to:\r\n" + fullPath + "\r\n\r\n" + ex.Message );
          }
          report.AppendLine( label + " -> " + fullPath + " FAILED: " + ex.Message );
        }
      }
      return report.ToString();
    }



  }
}
