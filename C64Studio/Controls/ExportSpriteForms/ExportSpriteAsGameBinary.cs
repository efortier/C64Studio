using GR.Memory;
using RetroDevStudio.Formats;
using System;
using System.Text;
using System.Windows.Forms;



namespace RetroDevStudio.Controls
{
  /// <summary>
  /// Exports a relocatable sprite game-binary, mirroring the map game-binary
  /// export. Two files: a header + offset-table ANIM-DEFS binary (with a
  /// KickAssembler .asm const sidecar) and a raw SPRITE-DATA binary of 64-byte
  /// blocks. Each file has its own Prefix Load Address / Compress (ZX0) /
  /// Max-size options. Only the first overlay (slot 0) is exported for now.
  /// </summary>
  public partial class ExportSpriteAsGameBinary : ExportSpriteFormBase
  {
    public ExportSpriteAsGameBinary() :
      base( null )
    {
      InitializeComponent();
    }



    public ExportSpriteAsGameBinary( StudioCore Core ) :
      base( Core )
    {
      InitializeComponent();
      UpdateControlStates( null, null );
      AttachSettingsHandlers();
    }



    private System.Windows.Forms.TextBox[] SettingsTextBoxes()
    {
      return new System.Windows.Forms.TextBox[] {
        editAnimDir, editAnimFile, editAnimAsmDir, editAnimAsmFile, editAnimPrefix,
        editAnimCompressDir, editAnimCompressFile, editAnimOverride, editAnimMaxSize,
        editSprDir, editSprFile, editSprPrefix,
        editSprCompressDir, editSprCompressFile, editSprOverride, editSprMaxSize };
    }



    private System.Windows.Forms.CheckBox[] SettingsCheckBoxes()
    {
      return new System.Windows.Forms.CheckBox[] {
        checkAnimAsm, checkAnimPrefix, checkAnimCompress, checkAnimOverride, checkAnimMaxSize,
        checkSprPrefix, checkSprCompress, checkSprOverride, checkSprMaxSize };
    }



    private System.Windows.Forms.ComboBox[] SettingsCombos()
    {
      return new System.Windows.Forms.ComboBox[] { comboAnimCompressor, comboSprCompressor };
    }



    private void HandleSettingsChanged( object sender, EventArgs e )
    {
      RaiseSettingsChanged();
    }



    /// <summary>
    /// Wire the per-control change events that raise SettingsChanged. Idempotent
    /// (each handler is detached before re-attaching). We DETACH these around
    /// programmatic population (see ApplyExportSettings) so loading persisted
    /// settings never raises SettingsChanged - that is the no-gating-flag way to
    /// keep a freshly-loaded project from being marked modified. The Designer's
    /// UpdateControlStates enable/disable wiring is separate and stays attached.
    /// </summary>
    private void AttachSettingsHandlers()
    {
      foreach ( var tb in SettingsTextBoxes() ) { tb.TextChanged -= HandleSettingsChanged; tb.TextChanged += HandleSettingsChanged; }
      foreach ( var cb in SettingsCheckBoxes() ) { cb.CheckedChanged -= HandleSettingsChanged; cb.CheckedChanged += HandleSettingsChanged; }
      foreach ( var co in SettingsCombos() ) { co.SelectedIndexChanged -= HandleSettingsChanged; co.SelectedIndexChanged += HandleSettingsChanged; }
    }



    private void DetachSettingsHandlers()
    {
      foreach ( var tb in SettingsTextBoxes() ) tb.TextChanged -= HandleSettingsChanged;
      foreach ( var cb in SettingsCheckBoxes() ) cb.CheckedChanged -= HandleSettingsChanged;
      foreach ( var co in SettingsCombos() ) co.SelectedIndexChanged -= HandleSettingsChanged;
    }



    private void UpdateControlStates( object sender, EventArgs e )
    {
      editAnimAsmFile.Enabled       = checkAnimAsm.Checked;
      editAnimPrefix.Enabled        = checkAnimPrefix.Checked;
      comboAnimCompressor.Enabled   = checkAnimCompress.Checked;
      editAnimCompressFile.Enabled  = checkAnimCompress.Checked;
      checkAnimOverride.Enabled     = checkAnimCompress.Checked;
      editAnimOverride.Enabled      = checkAnimCompress.Checked && checkAnimOverride.Checked;
      editAnimMaxSize.Enabled       = checkAnimMaxSize.Checked;

      editSprPrefix.Enabled         = checkSprPrefix.Checked;
      comboSprCompressor.Enabled    = checkSprCompress.Checked;
      editSprCompressFile.Enabled   = checkSprCompress.Checked;
      checkSprOverride.Enabled      = checkSprCompress.Checked;
      editSprOverride.Enabled       = checkSprCompress.Checked && checkSprOverride.Checked;
      editSprMaxSize.Enabled        = checkSprMaxSize.Checked;
    }



    /// <summary>Select <paramref name="Value"/> in a compressor dropdown,
    /// falling back to the first entry when it is missing/unknown.</summary>
    private static void SelectCompressor( System.Windows.Forms.ComboBox Combo, string Value )
    {
      int idx = Combo.FindStringExact( Value ?? "" );
      Combo.SelectedIndex = ( idx >= 0 ) ? idx : ( Combo.Items.Count > 0 ? 0 : -1 );
    }



    /// <summary>The compressor currently chosen in a dropdown.</summary>
    private static string SelectedCompressor( System.Windows.Forms.ComboBox Combo )
    {
      if ( Combo.SelectedItem != null )
      {
        return Combo.SelectedItem.ToString();
      }
      return string.IsNullOrEmpty( Combo.Text ) ? "ZX0" : Combo.Text;
    }



    private void btnAnimDirBrowse_Click( object sender, EventArgs e )
    {
      BrowseInto( editAnimDir );
    }



    private void btnSprDirBrowse_Click( object sender, EventArgs e )
    {
      BrowseInto( editSprDir );
    }



    private void btnAnimAsmDirBrowse_Click( object sender, EventArgs e )
    {
      BrowseInto( editAnimAsmDir );
    }



    private void btnAnimCompressDirBrowse_Click( object sender, EventArgs e )
    {
      BrowseInto( editAnimCompressDir );
    }



    private void btnSprCompressDirBrowse_Click( object sender, EventArgs e )
    {
      BrowseInto( editSprCompressDir );
    }



    private void btnCopySettings_Click( object sender, EventArgs e )
    {
      var s = new SpriteProject.SpriteGameBinarySettings();
      GatherSettingsFromUI( s );
      try
      {
        Clipboard.SetText( s.ToClipboardString() );
      }
      catch ( Exception ex )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Clipboard error",
            "Could not copy the export settings to the clipboard.\r\n\r\n" + ex.Message );
        }
      }
    }



    private void btnPasteSettings_Click( object sender, EventArgs e )
    {
      string text = "";
      try
      {
        if ( Clipboard.ContainsText() )
        {
          text = Clipboard.GetText();
        }
      }
      catch ( Exception )
      {
        // Clipboard can be locked by another app — treat as no usable text.
      }

      var s = SpriteProject.SpriteGameBinarySettings.FromClipboardString( text );
      if ( s == null )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Paste settings",
            "The clipboard does not contain sprite export settings.\r\n\r\n"
            + "Use \"Copy settings\" first (here or in another sprite project)." );
        }
        return;
      }

      ApplySettingsToUI( s );
      // Pasting is a real edit: flush + mark the document modified (the populate
      // above ran with change handlers detached, so raise it once here).
      RaiseSettingsChanged();
    }



    private void BrowseInto( TextBox Target )
    {
      using ( var dlg = new FolderBrowserDialog() )
      {
        dlg.Description = "Select export directory";
        if ( !string.IsNullOrEmpty( Target.Text ) )
        {
          dlg.SelectedPath = Target.Text;
        }
        if ( dlg.ShowDialog() == DialogResult.OK )
        {
          Target.Text = dlg.SelectedPath;
        }
      }
    }



    public override void ApplyExportSettings( SpriteProject Project )
    {
      if ( Project == null )
      {
        return;
      }
      ApplySettingsToUI( Project.GameBinary );
    }



    /// <summary>Push a settings block into the UI (used by load and by Paste).</summary>
    private void ApplySettingsToUI( SpriteProject.SpriteGameBinarySettings s )
    {
      if ( s == null )
      {
        return;
      }
      // Populate with the change-raising handlers detached, so applying the
      // settings does not raise SettingsChanged (load must not mark the document
      // modified; Paste raises it once explicitly afterwards).
      DetachSettingsHandlers();
      try
      {
        editAnimDir.Text          = s.AnimDirectory ?? "";
        editAnimFile.Text         = s.AnimFilename ?? "";
        checkAnimAsm.Checked      = s.AnimAsm;
        editAnimAsmDir.Text       = s.AnimAsmDirectory ?? "";
        editAnimAsmFile.Text      = string.IsNullOrEmpty( s.AnimAsmFilename ) ? "sprite_anims.asm" : s.AnimAsmFilename;
        checkAnimPrefix.Checked   = s.AnimPrefix;
        editAnimPrefix.Text       = s.AnimPrefixHex ?? "";
        checkAnimCompress.Checked = s.AnimCompress;
        SelectCompressor( comboAnimCompressor, s.AnimCompressor );
        editAnimCompressDir.Text  = s.AnimCompressDirectory ?? "";
        editAnimCompressFile.Text = s.AnimCompressFile ?? "";
        checkAnimOverride.Checked = s.AnimOverride;
        editAnimOverride.Text     = s.AnimOverrideHex ?? "";
        checkAnimMaxSize.Checked  = s.AnimMaxSize;
        editAnimMaxSize.Text      = s.AnimMaxSizeText ?? "0";
        editSprDir.Text           = s.SprDirectory ?? "";
        editSprFile.Text          = s.SprFilename ?? "";
        checkSprPrefix.Checked    = s.SprPrefix;
        editSprPrefix.Text        = s.SprPrefixHex ?? "";
        checkSprCompress.Checked  = s.SprCompress;
        SelectCompressor( comboSprCompressor, s.SprCompressor );
        editSprCompressDir.Text   = s.SprCompressDirectory ?? "";
        editSprCompressFile.Text  = s.SprCompressFile ?? "";
        checkSprOverride.Checked  = s.SprOverride;
        editSprOverride.Text      = s.SprOverrideHex ?? "";
        checkSprMaxSize.Checked   = s.SprMaxSize;
        editSprMaxSize.Text       = s.SprMaxSizeText ?? "0";
        UpdateControlStates( null, null );
      }
      finally
      {
        AttachSettingsHandlers();
      }
    }



    public override void UpdateExportSettings( SpriteProject Project )
    {
      if ( Project == null )
      {
        return;
      }
      GatherSettingsFromUI( Project.GameBinary );
    }



    /// <summary>Read the UI into a settings block (used by save and by Copy).</summary>
    private void GatherSettingsFromUI( SpriteProject.SpriteGameBinarySettings s )
    {
      if ( s == null )
      {
        return;
      }
      s.AnimDirectory    = editAnimDir.Text ?? "";
      s.AnimFilename     = editAnimFile.Text ?? "";
      s.AnimAsm          = checkAnimAsm.Checked;
      s.AnimAsmDirectory = editAnimAsmDir.Text ?? "";
      s.AnimAsmFilename  = editAnimAsmFile.Text ?? "";
      s.AnimPrefix       = checkAnimPrefix.Checked;
      s.AnimPrefixHex    = editAnimPrefix.Text ?? "";
      s.AnimCompress        = checkAnimCompress.Checked;
      s.AnimCompressor      = SelectedCompressor( comboAnimCompressor );
      s.AnimCompressDirectory = editAnimCompressDir.Text ?? "";
      s.AnimCompressFile    = editAnimCompressFile.Text ?? "";
      s.AnimOverride     = checkAnimOverride.Checked;
      s.AnimOverrideHex  = editAnimOverride.Text ?? "";
      s.AnimMaxSize      = checkAnimMaxSize.Checked;
      s.AnimMaxSizeText  = editAnimMaxSize.Text ?? "0";
      s.SprDirectory     = editSprDir.Text ?? "";
      s.SprFilename      = editSprFile.Text ?? "";
      s.SprPrefix        = checkSprPrefix.Checked;
      s.SprPrefixHex     = editSprPrefix.Text ?? "";
      s.SprCompress         = checkSprCompress.Checked;
      s.SprCompressor       = SelectedCompressor( comboSprCompressor );
      s.SprCompressDirectory = editSprCompressDir.Text ?? "";
      s.SprCompressFile     = editSprCompressFile.Text ?? "";
      s.SprOverride      = checkSprOverride.Checked;
      s.SprOverrideHex   = editSprOverride.Text ?? "";
      s.SprMaxSize       = checkSprMaxSize.Checked;
      s.SprMaxSizeText   = editSprMaxSize.Text ?? "0";
    }



    public override bool HandleExport( ExportSpriteInfo Info, TextBox EditOutput, DocumentInfo DocInfo )
    {
      if ( Info == null || Info.Project == null )
      {
        return false;
      }
      // Flush the current UI into the project so an export also persists the
      // settings (they then ride the next project save).
      UpdateExportSettings( Info.Project );
      if ( string.IsNullOrEmpty( editAnimFile.Text )
      ||   string.IsNullOrEmpty( editSprFile.Text ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Filenames required",
            "Both the animation-definitions filename and the sprite-data filename must be set." );
        }
        return false;
      }
      // A relative filename with a blank directory would resolve against the
      // IDE's working directory. Require an explicit directory (or a rooted
      // filename) so the user always controls where the files land. The .asm /
      // compressed sub-files fall back to these directories, so guarding the two
      // main files covers every written file.
      if ( !System.IO.Path.IsPathRooted( editAnimFile.Text ) && string.IsNullOrEmpty( editAnimDir.Text ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Directory required",
            "Set a Directory for the animation-definitions file (or give the filename a full path)." );
        }
        return false;
      }
      if ( !System.IO.Path.IsPathRooted( editSprFile.Text ) && string.IsNullOrEmpty( editSprDir.Text ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Directory required",
            "Set a Directory for the sprite-data file (or give the filename a full path)." );
        }
        return false;
      }

      GR.Memory.ByteBuffer animDefs, spriteData;
      int animationCount, spriteCount;
      Info.Project.ExportAsGameBinarySprites( out animDefs, out spriteData, out animationCount, out spriteCount );

      var log = new StringBuilder();
      log.AppendLine( "Sprite game-binary export — "
        + animationCount + " animation(s), " + spriteCount + " sprite block(s)." );
      log.AppendLine();

      // 1) Animation definitions file (+ optional .asm sidecar).
      string animPath = WriteOneFile( "Anim defs", animDefs,
        editAnimDir.Text, editAnimFile.Text,
        checkAnimPrefix.Checked, editAnimPrefix.Text,
        checkAnimCompress.Checked, SelectedCompressor( comboAnimCompressor ), editAnimCompressDir.Text, editAnimCompressFile.Text,
        checkAnimOverride.Checked, editAnimOverride.Text,
        checkAnimMaxSize.Checked, editAnimMaxSize.Text,
        log );
      if ( animPath == null )
      {
        return false;
      }
      if ( checkAnimAsm.Checked && !string.IsNullOrEmpty( editAnimAsmFile.Text ) )
      {
        // Prefer the dedicated .asm directory; else the group directory; else
        // the directory the anim-defs binary was actually written to.
        string asmDir;
        if ( !string.IsNullOrEmpty( editAnimAsmDir.Text ) )
        {
          asmDir = editAnimAsmDir.Text;
        }
        else if ( !string.IsNullOrEmpty( editAnimDir.Text ) )
        {
          asmDir = editAnimDir.Text;
        }
        else
        {
          asmDir = System.IO.Path.GetDirectoryName( animPath ) ?? "";
        }
        string asmPath = System.IO.Path.IsPathRooted( editAnimAsmFile.Text )
                         ? editAnimAsmFile.Text
                         : System.IO.Path.Combine( asmDir, editAnimAsmFile.Text );
        try
        {
          System.IO.File.WriteAllText( asmPath, Info.Project.GenerateSpriteGameBinaryHeaderAsm( "#importonce" ) );
          log.AppendLine( "Wrote consts: " + asmPath );
        }
        catch ( Exception ex )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Error saving .asm", "Could not write " + asmPath + "\r\n\r\n" + ex.Message );
          }
        }
      }
      log.AppendLine();

      // 2) Sprite data file.
      string sprPath = WriteOneFile( "Sprite data", spriteData,
        editSprDir.Text, editSprFile.Text,
        checkSprPrefix.Checked, editSprPrefix.Text,
        checkSprCompress.Checked, SelectedCompressor( comboSprCompressor ), editSprCompressDir.Text, editSprCompressFile.Text,
        checkSprOverride.Checked, editSprOverride.Text,
        checkSprMaxSize.Checked, editSprMaxSize.Text,
        log );
      if ( sprPath == null )
      {
        return false;
      }

      // Layout / offset view of the def file (mirrors the map export's .def
      // dump): header constants, decoded header, offset tables and each
      // animation definition, after the size/compression info above.
      log.AppendLine();
      log.Append( Info.Project.GenerateSpriteGameBinaryLayoutText( animDefs, spriteData ) );

      if ( EditOutput != null )
      {
        EditOutput.Text = log.ToString();
      }
      return true;
    }



    /// <summary>
    /// Write one export file: apply the optional 2-byte load-address prefix,
    /// save, optionally compress (ZX0/dali, beside the file with the typed name),
    /// and warn if it exceeds the optional max size. Appends a report to
    /// <paramref name="Log"/>. Returns the written file path, or null on a hard
    /// failure (the caller aborts the whole export).
    /// </summary>
    private string WriteOneFile( string Label, GR.Memory.ByteBuffer Data,
      string Dir, string File,
      bool PrefixOn, string PrefixHex,
      bool CompressOn, string Compressor, string CompressDir, string CompressFile,
      bool OverrideOn, string OverrideHex,
      bool MaxOn, string MaxText,
      StringBuilder Log )
    {
      string path = System.IO.Path.IsPathRooted( File )
                    ? File
                    : System.IO.Path.Combine( Dir ?? "", File );

      GR.Memory.ByteBuffer finalData = Data;
      if ( PrefixOn )
      {
        ushort address = GR.Convert.ToU16( PrefixHex, 16 );
        var addressData = new ByteBuffer();
        addressData.AppendU16( address );
        finalData = addressData + Data;
      }

      try
      {
        GR.IO.File.WriteAllBytes( path, finalData );
      }
      catch ( Exception ex )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Error saving file",
            "Could not save " + Label + " to:\r\n" + path + "\r\n\r\n" + ex.Message );
        }
        return null;
      }
      Log.AppendLine( Label + ": " + finalData.Length + " bytes -> " + path );

      // Optional compression (second file beside the main one).
      if ( CompressOn && !string.IsNullOrEmpty( CompressFile ) )
      {
        // Prefer the dedicated compress directory; else the directory the
        // uncompressed file was written to.
        string outDir  = !string.IsNullOrEmpty( CompressDir )
                         ? CompressDir
                         : ( System.IO.Path.GetDirectoryName( path ) ?? "" );
        string outPath = System.IO.Path.IsPathRooted( CompressFile )
                         ? CompressFile
                         : System.IO.Path.Combine( outDir, CompressFile );

        bool sameName = false;
        try
        {
          sameName = string.Equals( System.IO.Path.GetFullPath( outPath ),
                                     System.IO.Path.GetFullPath( path ),
                                     StringComparison.OrdinalIgnoreCase );
        }
        catch ( Exception ) { }

        int overrideAddress = -1;
        bool overrideBad = false;
        if ( OverrideOn )
        {
          if ( !GameBinaryCompression.TryParseHexAddress( OverrideHex, out overrideAddress ) )
          {
            overrideBad = true;
          }
        }

        if ( sameName )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Compression skipped",
              "The compressed filename equals the " + Label + " filename — pick a different name." );
          }
        }
        else if ( overrideBad )
        {
          if ( Core != null )
          {
            Core.Notification.MessageBox( "Invalid load address",
              "\"" + ( OverrideHex ?? "" ) + "\" is not a valid hex address ($0000-$FFFF). "
              + Label + " was not compressed." );
          }
        }
        else
        {
          string report, error;
          if ( GameBinaryCompression.RunCompressor( Compressor, path, outPath, overrideAddress, finalData.Length, out report, out error ) )
          {
            Log.AppendLine( "  " + report );
          }
          else
          {
            if ( Core != null )
            {
              Core.Notification.MessageBox( "Compression failed", error + "\r\n\r\n" + Label + " was not compressed." );
            }
          }
        }
      }

      // Optional max-size warning (after writing).
      int maxSize = 0;
      int.TryParse( MaxText, out maxSize );
      if ( MaxOn && ( maxSize > 0 ) && ( (long)finalData.Length > maxSize ) )
      {
        if ( Core != null )
        {
          Core.Notification.MessageBox( "Max export size exceeded",
            Label + " is " + finalData.Length + " bytes, which exceeds the configured maximum of " + maxSize + " bytes." );
        }
      }

      return path;
    }
  }
}
