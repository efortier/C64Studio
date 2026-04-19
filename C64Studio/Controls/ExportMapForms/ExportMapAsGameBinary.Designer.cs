
namespace RetroDevStudio.Controls
{
  partial class ExportMapAsGameBinary
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
      this.checkExportMarkers = new System.Windows.Forms.CheckBox();
      this.checkExportColors = new System.Windows.Forms.CheckBox();
      this.checkExportPassable = new System.Windows.Forms.CheckBox();
      this.checkAbsoluteBaseAddress = new System.Windows.Forms.CheckBox();
      this.editAbsoluteBaseAddress = new System.Windows.Forms.TextBox();
      this.checkPrefixLoadAddress = new System.Windows.Forms.CheckBox();
      this.editPrefixLoadAddress = new System.Windows.Forms.TextBox();
      this.checkSaveOnExport = new System.Windows.Forms.CheckBox();
      this.labelExportDirectory = new System.Windows.Forms.Label();
      this.editExportDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseExportDirectory = new System.Windows.Forms.Button();
      this.labelExportFilename = new System.Windows.Forms.Label();
      this.editExportFilename = new System.Windows.Forms.TextBox();
      this.checkGenerateDefFile = new System.Windows.Forms.CheckBox();
      this.groupCharset = new System.Windows.Forms.GroupBox();
      this.checkExportCharset = new System.Windows.Forms.CheckBox();
      this.labelCharsetExportDirectory = new System.Windows.Forms.Label();
      this.editCharsetExportDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseCharsetExportDirectory = new System.Windows.Forms.Button();
      this.labelCharsetExportFilename = new System.Windows.Forms.Label();
      this.editCharsetExportFilename = new System.Windows.Forms.TextBox();
      this.checkCharsetPrefixLoadAddress = new System.Windows.Forms.CheckBox();
      this.editCharsetPrefixLoadAddress = new System.Windows.Forms.TextBox();
      this.groupCharset.SuspendLayout();
      this.SuspendLayout();
      //
      // checkExportMarkers
      //
      this.checkExportMarkers.AutoSize = true;
      this.checkExportMarkers.Checked = true;
      this.checkExportMarkers.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkExportMarkers.Location = new System.Drawing.Point(3, 3);
      this.checkExportMarkers.Name = "checkExportMarkers";
      this.checkExportMarkers.Size = new System.Drawing.Size(103, 17);
      this.checkExportMarkers.TabIndex = 0;
      this.checkExportMarkers.Text = "Export Markers";
      this.checkExportMarkers.UseVisualStyleBackColor = true;
      //
      // checkExportColors
      //
      this.checkExportColors.AutoSize = true;
      this.checkExportColors.Checked = true;
      this.checkExportColors.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkExportColors.Location = new System.Drawing.Point(3, 26);
      this.checkExportColors.Name = "checkExportColors";
      this.checkExportColors.Size = new System.Drawing.Size(112, 17);
      this.checkExportColors.TabIndex = 1;
      this.checkExportColors.Text = "Export Color Grid";
      this.checkExportColors.UseVisualStyleBackColor = true;
      //
      // checkExportPassable
      //
      this.checkExportPassable.AutoSize = true;
      this.checkExportPassable.Checked = true;
      this.checkExportPassable.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkExportPassable.Location = new System.Drawing.Point(3, 49);
      this.checkExportPassable.Name = "checkExportPassable";
      this.checkExportPassable.Size = new System.Drawing.Size(128, 17);
      this.checkExportPassable.TabIndex = 2;
      this.checkExportPassable.Text = "Export Passable Bits";
      this.checkExportPassable.UseVisualStyleBackColor = true;
      //
      // checkAbsoluteBaseAddress
      //
      this.checkAbsoluteBaseAddress.AutoSize = true;
      this.checkAbsoluteBaseAddress.Location = new System.Drawing.Point(3, 72);
      this.checkAbsoluteBaseAddress.Name = "checkAbsoluteBaseAddress";
      this.checkAbsoluteBaseAddress.Size = new System.Drawing.Size(168, 17);
      this.checkAbsoluteBaseAddress.TabIndex = 11;
      this.checkAbsoluteBaseAddress.Text = "Absolute Base Address (hex)";
      this.checkAbsoluteBaseAddress.UseVisualStyleBackColor = true;
      this.checkAbsoluteBaseAddress.CheckedChanged += new System.EventHandler(this.checkAbsoluteBaseAddress_CheckedChanged);
      //
      // editAbsoluteBaseAddress
      //
      this.editAbsoluteBaseAddress.Enabled = false;
      this.editAbsoluteBaseAddress.Location = new System.Drawing.Point(175, 70);
      this.editAbsoluteBaseAddress.Name = "editAbsoluteBaseAddress";
      this.editAbsoluteBaseAddress.Size = new System.Drawing.Size(66, 20);
      this.editAbsoluteBaseAddress.TabIndex = 12;
      this.editAbsoluteBaseAddress.Font = new System.Drawing.Font("Courier New", 8.25F);
      //
      // checkPrefixLoadAddress
      //
      this.checkPrefixLoadAddress.AutoSize = true;
      this.checkPrefixLoadAddress.Location = new System.Drawing.Point(3, 100);
      this.checkPrefixLoadAddress.Name = "checkPrefixLoadAddress";
      this.checkPrefixLoadAddress.Size = new System.Drawing.Size(146, 17);
      this.checkPrefixLoadAddress.TabIndex = 3;
      this.checkPrefixLoadAddress.Text = "Prefix Load Address (hex)";
      this.checkPrefixLoadAddress.UseVisualStyleBackColor = true;
      this.checkPrefixLoadAddress.CheckedChanged += new System.EventHandler(this.checkPrefixLoadAddress_CheckedChanged);
      //
      // editPrefixLoadAddress
      //
      this.editPrefixLoadAddress.Enabled = false;
      this.editPrefixLoadAddress.Location = new System.Drawing.Point(155, 98);
      this.editPrefixLoadAddress.Name = "editPrefixLoadAddress";
      this.editPrefixLoadAddress.Size = new System.Drawing.Size(66, 20);
      this.editPrefixLoadAddress.TabIndex = 4;
      //
      // checkSaveOnExport
      //
      this.checkSaveOnExport.AutoSize = true;
      this.checkSaveOnExport.Location = new System.Drawing.Point(3, 131);
      this.checkSaveOnExport.Name = "checkSaveOnExport";
      this.checkSaveOnExport.Size = new System.Drawing.Size(143, 17);
      this.checkSaveOnExport.TabIndex = 5;
      this.checkSaveOnExport.Text = "Auto-save on export to:";
      this.checkSaveOnExport.UseVisualStyleBackColor = true;
      this.checkSaveOnExport.CheckedChanged += new System.EventHandler(this.checkSaveOnExport_CheckedChanged);
      //
      // labelExportDirectory
      //
      this.labelExportDirectory.AutoSize = true;
      this.labelExportDirectory.Location = new System.Drawing.Point(20, 155);
      this.labelExportDirectory.Name = "labelExportDirectory";
      this.labelExportDirectory.Size = new System.Drawing.Size(52, 13);
      this.labelExportDirectory.TabIndex = 6;
      this.labelExportDirectory.Text = "Directory:";
      //
      // editExportDirectory
      //
      this.editExportDirectory.Enabled = false;
      this.editExportDirectory.Location = new System.Drawing.Point(78, 152);
      this.editExportDirectory.Name = "editExportDirectory";
      this.editExportDirectory.Size = new System.Drawing.Size(195, 20);
      this.editExportDirectory.TabIndex = 7;
      //
      // btnBrowseExportDirectory
      //
      this.btnBrowseExportDirectory.Enabled = false;
      this.btnBrowseExportDirectory.Location = new System.Drawing.Point(279, 150);
      this.btnBrowseExportDirectory.Name = "btnBrowseExportDirectory";
      this.btnBrowseExportDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseExportDirectory.TabIndex = 8;
      this.btnBrowseExportDirectory.Text = "...";
      this.btnBrowseExportDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseExportDirectory.Click += new System.EventHandler(this.btnBrowseExportDirectory_Click);
      //
      // labelExportFilename
      //
      this.labelExportFilename.AutoSize = true;
      this.labelExportFilename.Location = new System.Drawing.Point(20, 181);
      this.labelExportFilename.Name = "labelExportFilename";
      this.labelExportFilename.Size = new System.Drawing.Size(52, 13);
      this.labelExportFilename.TabIndex = 9;
      this.labelExportFilename.Text = "Filename:";
      //
      // editExportFilename
      //
      this.editExportFilename.Enabled = false;
      this.editExportFilename.Location = new System.Drawing.Point(78, 178);
      this.editExportFilename.Name = "editExportFilename";
      this.editExportFilename.Size = new System.Drawing.Size(195, 20);
      this.editExportFilename.TabIndex = 10;
      //
      // checkGenerateDefFile
      //
      this.checkGenerateDefFile.AutoSize = true;
      this.checkGenerateDefFile.Checked = true;
      this.checkGenerateDefFile.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkGenerateDefFile.Location = new System.Drawing.Point(3, 208);
      this.checkGenerateDefFile.Name = "checkGenerateDefFile";
      this.checkGenerateDefFile.Size = new System.Drawing.Size(180, 17);
      this.checkGenerateDefFile.TabIndex = 13;
      this.checkGenerateDefFile.Text = "Generate .def layout sidecar file";
      this.checkGenerateDefFile.UseVisualStyleBackColor = true;
      this.checkGenerateDefFile.CheckedChanged += new System.EventHandler(this.checkGenerateDefFile_CheckedChanged);
      //
      // groupCharset
      //
      this.groupCharset.Controls.Add(this.checkExportCharset);
      this.groupCharset.Controls.Add(this.labelCharsetExportDirectory);
      this.groupCharset.Controls.Add(this.editCharsetExportDirectory);
      this.groupCharset.Controls.Add(this.btnBrowseCharsetExportDirectory);
      this.groupCharset.Controls.Add(this.labelCharsetExportFilename);
      this.groupCharset.Controls.Add(this.editCharsetExportFilename);
      this.groupCharset.Controls.Add(this.checkCharsetPrefixLoadAddress);
      this.groupCharset.Controls.Add(this.editCharsetPrefixLoadAddress);
      this.groupCharset.Location = new System.Drawing.Point(3, 235);
      this.groupCharset.Name = "groupCharset";
      this.groupCharset.Size = new System.Drawing.Size(316, 148);
      this.groupCharset.TabIndex = 14;
      this.groupCharset.TabStop = false;
      this.groupCharset.Text = "Character Set";
      //
      // checkExportCharset
      //
      this.checkExportCharset.AutoSize = true;
      this.checkExportCharset.Location = new System.Drawing.Point(6, 19);
      this.checkExportCharset.Name = "checkExportCharset";
      this.checkExportCharset.Size = new System.Drawing.Size(125, 17);
      this.checkExportCharset.TabIndex = 0;
      this.checkExportCharset.Text = "Export character set";
      this.checkExportCharset.UseVisualStyleBackColor = true;
      this.checkExportCharset.CheckedChanged += new System.EventHandler(this.checkExportCharset_CheckedChanged);
      //
      // labelCharsetExportDirectory
      //
      this.labelCharsetExportDirectory.AutoSize = true;
      this.labelCharsetExportDirectory.Location = new System.Drawing.Point(17, 45);
      this.labelCharsetExportDirectory.Name = "labelCharsetExportDirectory";
      this.labelCharsetExportDirectory.Size = new System.Drawing.Size(87, 13);
      this.labelCharsetExportDirectory.TabIndex = 1;
      this.labelCharsetExportDirectory.Text = "Export directory:";
      //
      // editCharsetExportDirectory
      //
      this.editCharsetExportDirectory.Enabled = false;
      this.editCharsetExportDirectory.Location = new System.Drawing.Point(110, 42);
      this.editCharsetExportDirectory.Name = "editCharsetExportDirectory";
      this.editCharsetExportDirectory.Size = new System.Drawing.Size(163, 20);
      this.editCharsetExportDirectory.TabIndex = 2;
      //
      // btnBrowseCharsetExportDirectory
      //
      this.btnBrowseCharsetExportDirectory.Enabled = false;
      this.btnBrowseCharsetExportDirectory.Location = new System.Drawing.Point(279, 40);
      this.btnBrowseCharsetExportDirectory.Name = "btnBrowseCharsetExportDirectory";
      this.btnBrowseCharsetExportDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseCharsetExportDirectory.TabIndex = 3;
      this.btnBrowseCharsetExportDirectory.Text = "...";
      this.btnBrowseCharsetExportDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseCharsetExportDirectory.Click += new System.EventHandler(this.btnBrowseCharsetExportDirectory_Click);
      //
      // labelCharsetExportFilename
      //
      this.labelCharsetExportFilename.AutoSize = true;
      this.labelCharsetExportFilename.Location = new System.Drawing.Point(17, 71);
      this.labelCharsetExportFilename.Name = "labelCharsetExportFilename";
      this.labelCharsetExportFilename.Size = new System.Drawing.Size(91, 13);
      this.labelCharsetExportFilename.TabIndex = 4;
      this.labelCharsetExportFilename.Text = "Export filename:";
      //
      // editCharsetExportFilename
      //
      this.editCharsetExportFilename.Enabled = false;
      this.editCharsetExportFilename.Location = new System.Drawing.Point(110, 68);
      this.editCharsetExportFilename.Name = "editCharsetExportFilename";
      this.editCharsetExportFilename.Size = new System.Drawing.Size(163, 20);
      this.editCharsetExportFilename.TabIndex = 5;
      //
      // checkCharsetPrefixLoadAddress
      //
      this.checkCharsetPrefixLoadAddress.AutoSize = true;
      this.checkCharsetPrefixLoadAddress.Location = new System.Drawing.Point(6, 100);
      this.checkCharsetPrefixLoadAddress.Name = "checkCharsetPrefixLoadAddress";
      this.checkCharsetPrefixLoadAddress.Size = new System.Drawing.Size(121, 17);
      this.checkCharsetPrefixLoadAddress.TabIndex = 6;
      this.checkCharsetPrefixLoadAddress.Text = "Prefix load address";
      this.checkCharsetPrefixLoadAddress.UseVisualStyleBackColor = true;
      this.checkCharsetPrefixLoadAddress.CheckedChanged += new System.EventHandler(this.checkCharsetPrefixLoadAddress_CheckedChanged);
      //
      // editCharsetPrefixLoadAddress
      //
      this.editCharsetPrefixLoadAddress.Enabled = false;
      this.editCharsetPrefixLoadAddress.Font = new System.Drawing.Font("Courier New", 8.25F);
      this.editCharsetPrefixLoadAddress.Location = new System.Drawing.Point(133, 98);
      this.editCharsetPrefixLoadAddress.Name = "editCharsetPrefixLoadAddress";
      this.editCharsetPrefixLoadAddress.Size = new System.Drawing.Size(66, 20);
      this.editCharsetPrefixLoadAddress.TabIndex = 7;
      //
      // ExportMapAsGameBinary
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.groupCharset);
      this.Controls.Add(this.checkGenerateDefFile);
      this.Controls.Add(this.editExportFilename);
      this.Controls.Add(this.labelExportFilename);
      this.Controls.Add(this.btnBrowseExportDirectory);
      this.Controls.Add(this.editExportDirectory);
      this.Controls.Add(this.labelExportDirectory);
      this.Controls.Add(this.checkSaveOnExport);
      this.Controls.Add(this.editPrefixLoadAddress);
      this.Controls.Add(this.checkPrefixLoadAddress);
      this.Controls.Add(this.editAbsoluteBaseAddress);
      this.Controls.Add(this.checkAbsoluteBaseAddress);
      this.Controls.Add(this.checkExportPassable);
      this.Controls.Add(this.checkExportColors);
      this.Controls.Add(this.checkExportMarkers);
      this.Name = "ExportMapAsGameBinary";
      this.Size = new System.Drawing.Size(322, 392);
      this.groupCharset.ResumeLayout(false);
      this.groupCharset.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.CheckBox checkExportMarkers;
    private System.Windows.Forms.CheckBox checkExportColors;
    private System.Windows.Forms.CheckBox checkExportPassable;
    private System.Windows.Forms.CheckBox checkAbsoluteBaseAddress;
    private System.Windows.Forms.TextBox editAbsoluteBaseAddress;
    private System.Windows.Forms.CheckBox checkPrefixLoadAddress;
    private System.Windows.Forms.TextBox editPrefixLoadAddress;
    private System.Windows.Forms.CheckBox checkSaveOnExport;
    private System.Windows.Forms.Label labelExportDirectory;
    private System.Windows.Forms.TextBox editExportDirectory;
    private System.Windows.Forms.Button btnBrowseExportDirectory;
    private System.Windows.Forms.Label labelExportFilename;
    private System.Windows.Forms.TextBox editExportFilename;
    private System.Windows.Forms.CheckBox checkGenerateDefFile;
    private System.Windows.Forms.GroupBox groupCharset;
    private System.Windows.Forms.CheckBox checkExportCharset;
    private System.Windows.Forms.Label labelCharsetExportDirectory;
    private System.Windows.Forms.TextBox editCharsetExportDirectory;
    private System.Windows.Forms.Button btnBrowseCharsetExportDirectory;
    private System.Windows.Forms.Label labelCharsetExportFilename;
    private System.Windows.Forms.TextBox editCharsetExportFilename;
    private System.Windows.Forms.CheckBox checkCharsetPrefixLoadAddress;
    private System.Windows.Forms.TextBox editCharsetPrefixLoadAddress;
  }
}
