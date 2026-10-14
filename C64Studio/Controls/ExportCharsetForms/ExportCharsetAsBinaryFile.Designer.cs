
namespace RetroDevStudio.Controls
{
  partial class ExportCharsetAsBinaryFile
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.components = new System.ComponentModel.Container();
      this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
      this.editPrefixLoadAddress = new System.Windows.Forms.TextBox();
      this.checkPrefixLoadAddress = new System.Windows.Forms.CheckBox();
      this.groupCompress = new System.Windows.Forms.GroupBox();
      this.checkCompress = new System.Windows.Forms.CheckBox();
      this.comboCompressor = new System.Windows.Forms.ComboBox();
      this.labelCompressFilename = new System.Windows.Forms.Label();
      this.editCompressFilename = new System.Windows.Forms.TextBox();
      this.checkOverrideLoadAddress = new ThemedCheckBox();
      this.editOverrideLoadAddress = new System.Windows.Forms.TextBox();
      this.groupCompress.SuspendLayout();
      this.SuspendLayout();
      // 
      // editPrefixLoadAddress
      // 
      this.editPrefixLoadAddress.Enabled = false;
      this.editPrefixLoadAddress.Location = new System.Drawing.Point(155, 1);
      this.editPrefixLoadAddress.MaxLength = 4;
      this.editPrefixLoadAddress.Name = "editPrefixLoadAddress";
      this.editPrefixLoadAddress.Size = new System.Drawing.Size(66, 20);
      this.editPrefixLoadAddress.TabIndex = 1;
      this.editPrefixLoadAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editPrefixLoadAddress_KeyPress);
      // 
      // checkPrefixLoadAddress
      // 
      this.checkPrefixLoadAddress.AutoSize = true;
      this.checkPrefixLoadAddress.Location = new System.Drawing.Point(3, 3);
      this.checkPrefixLoadAddress.Name = "checkPrefixLoadAddress";
      this.checkPrefixLoadAddress.Size = new System.Drawing.Size(146, 17);
      this.checkPrefixLoadAddress.TabIndex = 0;
      this.checkPrefixLoadAddress.Text = "Prefix Load Address (hex)";
      this.checkPrefixLoadAddress.UseVisualStyleBackColor = true;
      this.checkPrefixLoadAddress.CheckedChanged += new System.EventHandler(this.checkPrefixLoadAddress_CheckedChanged);
      // 
      // groupCompress
      // 
      this.groupCompress.Controls.Add(this.checkCompress);
      this.groupCompress.Controls.Add(this.comboCompressor);
      this.groupCompress.Controls.Add(this.labelCompressFilename);
      this.groupCompress.Controls.Add(this.editCompressFilename);
      this.groupCompress.Controls.Add(this.checkOverrideLoadAddress);
      this.groupCompress.Controls.Add(this.editOverrideLoadAddress);
      this.groupCompress.Location = new System.Drawing.Point(3, 27);
      this.groupCompress.Name = "groupCompress";
      this.groupCompress.Size = new System.Drawing.Size(303, 100);
      this.groupCompress.TabIndex = 2;
      this.groupCompress.TabStop = false;
      this.groupCompress.Text = "Compressor";
      // 
      // checkCompress
      // 
      this.checkCompress.AutoSize = true;
      this.checkCompress.Location = new System.Drawing.Point(6, 20);
      this.checkCompress.Name = "checkCompress";
      this.checkCompress.Size = new System.Drawing.Size(139, 17);
      this.checkCompress.TabIndex = 0;
      this.checkCompress.Text = "Compress character set";
      this.checkCompress.UseVisualStyleBackColor = true;
      this.toolTip1.SetToolTip(this.checkCompress, "Also writes the exported character set compressed, as a SECOND file beside it, named below (the exported file itself stays).");
      this.checkCompress.CheckedChanged += new System.EventHandler(this.checkCompress_CheckedChanged);
      this.checkCompress.Click += new System.EventHandler(this.checkCompress_Click);
      // 
      // comboCompressor
      // 
      this.comboCompressor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboCompressor.Enabled = false;
      this.comboCompressor.FormattingEnabled = true;
      this.comboCompressor.Items.AddRange(new object[] {
      "ZX0"});
      this.comboCompressor.Location = new System.Drawing.Point(181, 18);
      this.comboCompressor.Name = "comboCompressor";
      this.comboCompressor.Size = new System.Drawing.Size(110, 21);
      this.comboCompressor.TabIndex = 1;
      // 
      // labelCompressFilename
      // 
      this.labelCompressFilename.AutoSize = true;
      this.labelCompressFilename.Location = new System.Drawing.Point(6, 48);
      this.labelCompressFilename.Name = "labelCompressFilename";
      this.labelCompressFilename.Size = new System.Drawing.Size(52, 13);
      this.labelCompressFilename.TabIndex = 2;
      this.labelCompressFilename.Text = "Filename:";
      // 
      // editCompressFilename
      // 
      this.editCompressFilename.Enabled = false;
      this.editCompressFilename.Location = new System.Drawing.Point(66, 45);
      this.editCompressFilename.Name = "editCompressFilename";
      this.editCompressFilename.Size = new System.Drawing.Size(225, 20);
      this.editCompressFilename.TabIndex = 3;
      this.toolTip1.SetToolTip(this.editCompressFilename, "File name of the compressed character set, written beside the exported file (e.g. charset.zx0). Empty = no compression.");
      // 
      // checkOverrideLoadAddress
      // 
      this.checkOverrideLoadAddress.AutoSize = false;
      this.checkOverrideLoadAddress.Enabled = false;
      this.checkOverrideLoadAddress.Location = new System.Drawing.Point(6, 74);
      this.checkOverrideLoadAddress.Name = "checkOverrideLoadAddress";
      this.checkOverrideLoadAddress.Size = new System.Drawing.Size(180, 18);
      this.checkOverrideLoadAddress.TabIndex = 4;
      this.checkOverrideLoadAddress.Text = "Override file load address";
      this.checkOverrideLoadAddress.UseVisualStyleBackColor = true;
      this.checkOverrideLoadAddress.CheckedChanged += new System.EventHandler(this.checkOverrideLoadAddress_CheckedChanged);
      // 
      // editOverrideLoadAddress
      // 
      this.editOverrideLoadAddress.Enabled = false;
      this.editOverrideLoadAddress.Font = new System.Drawing.Font("Courier New", 8.25F);
      this.editOverrideLoadAddress.Location = new System.Drawing.Point(225, 72);
      this.editOverrideLoadAddress.Name = "editOverrideLoadAddress";
      this.editOverrideLoadAddress.Size = new System.Drawing.Size(66, 20);
      this.editOverrideLoadAddress.TabIndex = 5;
      // 
      // ExportCharsetAsBinaryFile
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.groupCompress);
      this.Controls.Add(this.editPrefixLoadAddress);
      this.Controls.Add(this.checkPrefixLoadAddress);
      this.Name = "ExportCharsetAsBinaryFile";
      this.Size = new System.Drawing.Size(317, 317);
      this.groupCompress.ResumeLayout(false);
      this.groupCompress.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox editPrefixLoadAddress;
    private System.Windows.Forms.CheckBox checkPrefixLoadAddress;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.GroupBox groupCompress;
    private System.Windows.Forms.CheckBox checkCompress;
    private System.Windows.Forms.ComboBox comboCompressor;
    private System.Windows.Forms.Label labelCompressFilename;
    private System.Windows.Forms.TextBox editCompressFilename;
    private ThemedCheckBox checkOverrideLoadAddress;
    private System.Windows.Forms.TextBox editOverrideLoadAddress;
  }
}
