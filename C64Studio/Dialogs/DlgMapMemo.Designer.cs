namespace RetroDevStudio.Dialogs
{
  partial class DlgMapMemo
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

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
      panelMemoToolbar = new System.Windows.Forms.Panel();
      comboFont = new System.Windows.Forms.ComboBox();
      editFontSize = new System.Windows.Forms.NumericUpDown();
      btnBold = new System.Windows.Forms.Button();
      btnItalic = new System.Windows.Forms.Button();
      btnUnderline = new System.Windows.Forms.Button();
      btnColor = new System.Windows.Forms.Button();
      richMemo = new System.Windows.Forms.RichTextBox();
      panelMemoToolbar.SuspendLayout();
      ( (System.ComponentModel.ISupportInitialize)editFontSize ).BeginInit();
      SuspendLayout();
      //
      // richMemo  (added FIRST so it fills the area below the docked toolbar)
      //
      richMemo.Dock = System.Windows.Forms.DockStyle.Fill;
      richMemo.HideSelection = false;
      richMemo.Location = new System.Drawing.Point( 0, 34 );
      richMemo.Name = "richMemo";
      richMemo.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
      richMemo.Size = new System.Drawing.Size( 484, 327 );
      richMemo.TabIndex = 1;
      richMemo.Text = "";
      richMemo.WordWrap = false;
      //
      // panelMemoToolbar
      //
      panelMemoToolbar.Controls.Add( comboFont );
      panelMemoToolbar.Controls.Add( editFontSize );
      panelMemoToolbar.Controls.Add( btnBold );
      panelMemoToolbar.Controls.Add( btnItalic );
      panelMemoToolbar.Controls.Add( btnUnderline );
      panelMemoToolbar.Controls.Add( btnColor );
      panelMemoToolbar.Dock = System.Windows.Forms.DockStyle.Top;
      panelMemoToolbar.Location = new System.Drawing.Point( 0, 0 );
      panelMemoToolbar.Name = "panelMemoToolbar";
      panelMemoToolbar.Size = new System.Drawing.Size( 484, 34 );
      panelMemoToolbar.TabIndex = 0;
      //
      // comboFont
      //
      comboFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      comboFont.Location = new System.Drawing.Point( 6, 6 );
      comboFont.Name = "comboFont";
      comboFont.Size = new System.Drawing.Size( 190, 23 );
      comboFont.TabIndex = 0;
      //
      // editFontSize
      //
      editFontSize.Location = new System.Drawing.Point( 202, 6 );
      editFontSize.Maximum = new decimal( new int[] { 72, 0, 0, 0 } );
      editFontSize.Minimum = new decimal( new int[] { 6, 0, 0, 0 } );
      editFontSize.Name = "editFontSize";
      editFontSize.Size = new System.Drawing.Size( 52, 23 );
      editFontSize.TabIndex = 1;
      editFontSize.Value = new decimal( new int[] { 9, 0, 0, 0 } );
      //
      // btnBold
      //
      btnBold.Font = new System.Drawing.Font( "Segoe UI", 9F, System.Drawing.FontStyle.Bold );
      btnBold.Location = new System.Drawing.Point( 262, 5 );
      btnBold.Name = "btnBold";
      btnBold.Size = new System.Drawing.Size( 26, 25 );
      btnBold.TabIndex = 2;
      btnBold.Text = "B";
      btnBold.UseVisualStyleBackColor = true;
      //
      // btnItalic
      //
      btnItalic.Font = new System.Drawing.Font( "Segoe UI", 9F, System.Drawing.FontStyle.Italic );
      btnItalic.Location = new System.Drawing.Point( 290, 5 );
      btnItalic.Name = "btnItalic";
      btnItalic.Size = new System.Drawing.Size( 26, 25 );
      btnItalic.TabIndex = 3;
      btnItalic.Text = "I";
      btnItalic.UseVisualStyleBackColor = true;
      //
      // btnUnderline
      //
      btnUnderline.Font = new System.Drawing.Font( "Segoe UI", 9F, System.Drawing.FontStyle.Underline );
      btnUnderline.Location = new System.Drawing.Point( 318, 5 );
      btnUnderline.Name = "btnUnderline";
      btnUnderline.Size = new System.Drawing.Size( 26, 25 );
      btnUnderline.TabIndex = 4;
      btnUnderline.Text = "U";
      btnUnderline.UseVisualStyleBackColor = true;
      //
      // btnColor
      //
      btnColor.Location = new System.Drawing.Point( 350, 5 );
      btnColor.Name = "btnColor";
      btnColor.Size = new System.Drawing.Size( 60, 25 );
      btnColor.TabIndex = 5;
      btnColor.Text = "Color…";
      btnColor.UseVisualStyleBackColor = true;
      //
      // DlgMapMemo
      //
      AutoScaleDimensions = new System.Drawing.SizeF( 7F, 15F );
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      ClientSize = new System.Drawing.Size( 484, 361 );
      Controls.Add( richMemo );
      Controls.Add( panelMemoToolbar );
      MinimumSize = new System.Drawing.Size( 400, 240 );
      Name = "DlgMapMemo";
      ShowInTaskbar = false;
      StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultLocation;
      Text = "Map memo";
      panelMemoToolbar.ResumeLayout( false );
      ( (System.ComponentModel.ISupportInitialize)editFontSize ).EndInit();
      ResumeLayout( false );
    }

    #endregion

    private System.Windows.Forms.Panel panelMemoToolbar;
    private System.Windows.Forms.ComboBox comboFont;
    private System.Windows.Forms.NumericUpDown editFontSize;
    private System.Windows.Forms.Button btnBold;
    private System.Windows.Forms.Button btnItalic;
    private System.Windows.Forms.Button btnUnderline;
    private System.Windows.Forms.Button btnColor;
    private System.Windows.Forms.RichTextBox richMemo;
  }
}
