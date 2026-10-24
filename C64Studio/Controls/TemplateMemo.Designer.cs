namespace RetroDevStudio.Controls
{
  partial class TemplateMemo
  {
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
      editFast = new FastColoredTextBoxNS.FastColoredTextBox();
      editPlain = new System.Windows.Forms.TextBox();
      ((System.ComponentModel.ISupportInitialize)editFast).BeginInit();
      SuspendLayout();
      //
      // editFast
      //
      editFast.AllowTabs = true;
      editFast.AutoCompleteBrackets = false;
      editFast.AutoIndent = false;
      editFast.AutoIndentChars = false;
      editFast.AutoIndentExistingLines = false;
      editFast.AutoScrollMinSize = new System.Drawing.Size( 27, 14 );
      editFast.BackBrush = null;
      editFast.CharHeight = 14;
      editFast.CharWidth = 8;
      editFast.ConvertTabsToSpaces = false;
      editFast.Cursor = System.Windows.Forms.Cursors.IBeam;
      editFast.Dock = System.Windows.Forms.DockStyle.Fill;
      editFast.Font = new System.Drawing.Font( "Consolas", 10F );
      editFast.IsReplaceMode = false;
      editFast.Language = FastColoredTextBoxNS.Language.Custom;
      editFast.Location = new System.Drawing.Point( 0, 0 );
      editFast.Name = "editFast";
      editFast.Paddings = new System.Windows.Forms.Padding( 0 );
      editFast.ShowLineNumbers = true;
      editFast.Size = new System.Drawing.Size( 600, 300 );
      editFast.TabIndex = 0;
      editFast.TabLength = 4;
      editFast.WordWrap = false;
      editFast.Zoom = 100;
      //
      // editPlain
      //
      editPlain.AcceptsReturn = true;
      editPlain.AcceptsTab = true;
      editPlain.Dock = System.Windows.Forms.DockStyle.Fill;
      editPlain.Font = new System.Drawing.Font( "Consolas", 10F );
      editPlain.Location = new System.Drawing.Point( 0, 0 );
      editPlain.MaxLength = 1000000;
      editPlain.Multiline = true;
      editPlain.Name = "editPlain";
      editPlain.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      editPlain.Size = new System.Drawing.Size( 600, 300 );
      editPlain.TabIndex = 1;
      editPlain.Visible = false;
      editPlain.WordWrap = false;
      //
      // TemplateMemo
      //
      AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      Controls.Add( editPlain );
      Controls.Add( editFast );
      Name = "TemplateMemo";
      Size = new System.Drawing.Size( 600, 300 );
      ((System.ComponentModel.ISupportInitialize)editFast).EndInit();
      ResumeLayout( false );
      PerformLayout();
    }

    #endregion

    private FastColoredTextBoxNS.FastColoredTextBox editFast;
    private System.Windows.Forms.TextBox editPlain;
  }
}
