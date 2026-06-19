
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
      this.checkPrefixLoadAddress = new System.Windows.Forms.CheckBox();
      this.editPrefixLoadAddress = new System.Windows.Forms.TextBox();
      this.checkMaxExportSize = new System.Windows.Forms.CheckBox();
      this.editMaxExportSize = new System.Windows.Forms.TextBox();
      this.labelMaxExportSize = new System.Windows.Forms.Label();
      this.checkSaveOnExport = new System.Windows.Forms.CheckBox();
      this.labelExportDirectory = new System.Windows.Forms.Label();
      this.editExportDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseExportDirectory = new System.Windows.Forms.Button();
      this.labelExportFilename = new System.Windows.Forms.Label();
      this.editExportFilename = new System.Windows.Forms.TextBox();
      this.checkGenerateDefFile = new System.Windows.Forms.CheckBox();
      this.checkExportHeaderAsm = new System.Windows.Forms.CheckBox();
      this.labelHeaderAsmDirectory = new System.Windows.Forms.Label();
      this.editHeaderAsmDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseHeaderAsmDirectory = new System.Windows.Forms.Button();
      this.labelHeaderAsmFilename = new System.Windows.Forms.Label();
      this.editHeaderAsmFilename = new System.Windows.Forms.TextBox();
      this.labelHeaderAsmPrefix = new System.Windows.Forms.Label();
      this.editHeaderAsmPrefix = new System.Windows.Forms.TextBox();
      this.checkExportMarkerLabels = new System.Windows.Forms.CheckBox();
      this.labelMarkerLabelsDirectory = new System.Windows.Forms.Label();
      this.editMarkerLabelsDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseMarkerLabelsDirectory = new System.Windows.Forms.Button();
      this.labelMarkerLabelsFilename = new System.Windows.Forms.Label();
      this.editMarkerLabelsFilename = new System.Windows.Forms.TextBox();
      this.labelMarkerLabelsPrefix = new System.Windows.Forms.Label();
      this.editMarkerLabelsPrefix = new System.Windows.Forms.TextBox();
      this.checkExportEntityLabels = new System.Windows.Forms.CheckBox();
      this.labelEntityLabelsDirectory = new System.Windows.Forms.Label();
      this.editEntityLabelsDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseEntityLabelsDirectory = new System.Windows.Forms.Button();
      this.labelEntityLabelsFilename = new System.Windows.Forms.Label();
      this.editEntityLabelsFilename = new System.Windows.Forms.TextBox();
      this.labelEntityLabelsPrefix = new System.Windows.Forms.Label();
      this.editEntityLabelsPrefix = new System.Windows.Forms.TextBox();
      this.checkExportMapStrings = new System.Windows.Forms.CheckBox();
      this.labelMapStringsDirectory = new System.Windows.Forms.Label();
      this.editMapStringsDirectory = new System.Windows.Forms.TextBox();
      this.btnBrowseMapStringsDirectory = new System.Windows.Forms.Button();
      this.labelMapStringsFilename = new System.Windows.Forms.Label();
      this.editMapStringsFilename = new System.Windows.Forms.TextBox();
      this.labelMapStringsPrefix = new System.Windows.Forms.Label();
      this.editMapStringsPrefix = new System.Windows.Forms.TextBox();
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
      // checkPrefixLoadAddress
      //
      this.checkPrefixLoadAddress.AutoSize = true;
      this.checkPrefixLoadAddress.Location = new System.Drawing.Point(3, 72);
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
      this.editPrefixLoadAddress.Location = new System.Drawing.Point(155, 70);
      this.editPrefixLoadAddress.Name = "editPrefixLoadAddress";
      this.editPrefixLoadAddress.Size = new System.Drawing.Size(66, 20);
      this.editPrefixLoadAddress.TabIndex = 4;
      //
      // checkMaxExportSize
      //
      this.checkMaxExportSize.AutoSize = true;
      this.checkMaxExportSize.Location = new System.Drawing.Point(3, 100);
      this.checkMaxExportSize.Name = "checkMaxExportSize";
      this.checkMaxExportSize.Size = new System.Drawing.Size(104, 17);
      this.checkMaxExportSize.TabIndex = 11;
      this.checkMaxExportSize.Text = "Max export size";
      this.checkMaxExportSize.UseVisualStyleBackColor = true;
      this.checkMaxExportSize.CheckedChanged += new System.EventHandler(this.checkMaxExportSize_CheckedChanged);
      //
      // editMaxExportSize
      //
      this.editMaxExportSize.Enabled = false;
      this.editMaxExportSize.Location = new System.Drawing.Point(155, 98);
      this.editMaxExportSize.Name = "editMaxExportSize";
      this.editMaxExportSize.Size = new System.Drawing.Size(66, 20);
      this.editMaxExportSize.TabIndex = 12;
      this.editMaxExportSize.Text = "0";
      //
      // labelMaxExportSize
      //
      this.labelMaxExportSize.AutoSize = true;
      this.labelMaxExportSize.Location = new System.Drawing.Point(227, 101);
      this.labelMaxExportSize.Name = "labelMaxExportSize";
      this.labelMaxExportSize.Size = new System.Drawing.Size(78, 13);
      this.labelMaxExportSize.TabIndex = 11;
      this.labelMaxExportSize.Text = "bytes (0 = off)";
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
      // checkExportHeaderAsm
      //
      this.checkExportHeaderAsm.AutoSize = true;
      this.checkExportHeaderAsm.Checked = false;
      this.checkExportHeaderAsm.CheckState = System.Windows.Forms.CheckState.Unchecked;
      this.checkExportHeaderAsm.Location = new System.Drawing.Point(3, 228);
      this.checkExportHeaderAsm.Name = "checkExportHeaderAsm";
      this.checkExportHeaderAsm.Size = new System.Drawing.Size(200, 17);
      this.checkExportHeaderAsm.TabIndex = 15;
      this.checkExportHeaderAsm.Text = "Generate map_header.asm sidecar file";
      this.checkExportHeaderAsm.UseVisualStyleBackColor = true;
      this.checkExportHeaderAsm.CheckedChanged += new System.EventHandler(this.checkExportHeaderAsm_CheckedChanged);
      //
      // labelHeaderAsmDirectory
      //
      this.labelHeaderAsmDirectory.AutoSize = true;
      this.labelHeaderAsmDirectory.Location = new System.Drawing.Point(20, 252);
      this.labelHeaderAsmDirectory.Name = "labelHeaderAsmDirectory";
      this.labelHeaderAsmDirectory.Size = new System.Drawing.Size(52, 13);
      this.labelHeaderAsmDirectory.TabIndex = 16;
      this.labelHeaderAsmDirectory.Text = "Directory:";
      //
      // editHeaderAsmDirectory
      //
      this.editHeaderAsmDirectory.Enabled = false;
      this.editHeaderAsmDirectory.Location = new System.Drawing.Point(78, 249);
      this.editHeaderAsmDirectory.Name = "editHeaderAsmDirectory";
      this.editHeaderAsmDirectory.Size = new System.Drawing.Size(195, 20);
      this.editHeaderAsmDirectory.TabIndex = 17;
      //
      // btnBrowseHeaderAsmDirectory
      //
      this.btnBrowseHeaderAsmDirectory.Enabled = false;
      this.btnBrowseHeaderAsmDirectory.Location = new System.Drawing.Point(279, 247);
      this.btnBrowseHeaderAsmDirectory.Name = "btnBrowseHeaderAsmDirectory";
      this.btnBrowseHeaderAsmDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseHeaderAsmDirectory.TabIndex = 18;
      this.btnBrowseHeaderAsmDirectory.Text = "...";
      this.btnBrowseHeaderAsmDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseHeaderAsmDirectory.Click += new System.EventHandler(this.btnBrowseHeaderAsmDirectory_Click);
      //
      // labelHeaderAsmFilename
      //
      this.labelHeaderAsmFilename.AutoSize = true;
      this.labelHeaderAsmFilename.Location = new System.Drawing.Point(20, 278);
      this.labelHeaderAsmFilename.Name = "labelHeaderAsmFilename";
      this.labelHeaderAsmFilename.Size = new System.Drawing.Size(52, 13);
      this.labelHeaderAsmFilename.TabIndex = 19;
      this.labelHeaderAsmFilename.Text = "Filename:";
      //
      // editHeaderAsmFilename
      //
      this.editHeaderAsmFilename.Enabled = false;
      this.editHeaderAsmFilename.Location = new System.Drawing.Point(78, 275);
      this.editHeaderAsmFilename.Name = "editHeaderAsmFilename";
      this.editHeaderAsmFilename.Size = new System.Drawing.Size(195, 20);
      this.editHeaderAsmFilename.TabIndex = 20;
      //
      // labelHeaderAsmPrefix
      //
      this.labelHeaderAsmPrefix.AutoSize = true;
      this.labelHeaderAsmPrefix.Location = new System.Drawing.Point(325, 231);
      this.labelHeaderAsmPrefix.Name = "labelHeaderAsmPrefix";
      this.labelHeaderAsmPrefix.Size = new System.Drawing.Size(62, 13);
      this.labelHeaderAsmPrefix.TabIndex = 27;
      this.labelHeaderAsmPrefix.Text = "Prefix text:";
      //
      // editHeaderAsmPrefix
      //
      this.editHeaderAsmPrefix.Enabled = false;
      this.editHeaderAsmPrefix.Location = new System.Drawing.Point(325, 249);
      this.editHeaderAsmPrefix.Multiline = true;
      this.editHeaderAsmPrefix.Name = "editHeaderAsmPrefix";
      this.editHeaderAsmPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.editHeaderAsmPrefix.Size = new System.Drawing.Size(280, 46);
      this.editHeaderAsmPrefix.TabIndex = 28;
      this.editHeaderAsmPrefix.Font = new System.Drawing.Font("Courier New", 8.25F);
      //
      // checkExportMarkerLabels
      //
      this.checkExportMarkerLabels.AutoSize = true;
      this.checkExportMarkerLabels.Checked = false;
      this.checkExportMarkerLabels.CheckState = System.Windows.Forms.CheckState.Unchecked;
      this.checkExportMarkerLabels.Location = new System.Drawing.Point(3, 305);
      this.checkExportMarkerLabels.Name = "checkExportMarkerLabels";
      this.checkExportMarkerLabels.Size = new System.Drawing.Size(220, 17);
      this.checkExportMarkerLabels.TabIndex = 21;
      this.checkExportMarkerLabels.Text = "Generate marker labels sidecar file";
      this.checkExportMarkerLabels.UseVisualStyleBackColor = true;
      this.checkExportMarkerLabels.CheckedChanged += new System.EventHandler(this.checkExportMarkerLabels_CheckedChanged);
      //
      // labelMarkerLabelsDirectory
      //
      this.labelMarkerLabelsDirectory.AutoSize = true;
      this.labelMarkerLabelsDirectory.Location = new System.Drawing.Point(20, 329);
      this.labelMarkerLabelsDirectory.Name = "labelMarkerLabelsDirectory";
      this.labelMarkerLabelsDirectory.Size = new System.Drawing.Size(52, 13);
      this.labelMarkerLabelsDirectory.TabIndex = 22;
      this.labelMarkerLabelsDirectory.Text = "Directory:";
      //
      // editMarkerLabelsDirectory
      //
      this.editMarkerLabelsDirectory.Enabled = false;
      this.editMarkerLabelsDirectory.Location = new System.Drawing.Point(78, 326);
      this.editMarkerLabelsDirectory.Name = "editMarkerLabelsDirectory";
      this.editMarkerLabelsDirectory.Size = new System.Drawing.Size(195, 20);
      this.editMarkerLabelsDirectory.TabIndex = 23;
      //
      // btnBrowseMarkerLabelsDirectory
      //
      this.btnBrowseMarkerLabelsDirectory.Enabled = false;
      this.btnBrowseMarkerLabelsDirectory.Location = new System.Drawing.Point(279, 324);
      this.btnBrowseMarkerLabelsDirectory.Name = "btnBrowseMarkerLabelsDirectory";
      this.btnBrowseMarkerLabelsDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseMarkerLabelsDirectory.TabIndex = 24;
      this.btnBrowseMarkerLabelsDirectory.Text = "...";
      this.btnBrowseMarkerLabelsDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseMarkerLabelsDirectory.Click += new System.EventHandler(this.btnBrowseMarkerLabelsDirectory_Click);
      //
      // labelMarkerLabelsFilename
      //
      this.labelMarkerLabelsFilename.AutoSize = true;
      this.labelMarkerLabelsFilename.Location = new System.Drawing.Point(20, 355);
      this.labelMarkerLabelsFilename.Name = "labelMarkerLabelsFilename";
      this.labelMarkerLabelsFilename.Size = new System.Drawing.Size(52, 13);
      this.labelMarkerLabelsFilename.TabIndex = 25;
      this.labelMarkerLabelsFilename.Text = "Filename:";
      //
      // editMarkerLabelsFilename
      //
      this.editMarkerLabelsFilename.Enabled = false;
      this.editMarkerLabelsFilename.Location = new System.Drawing.Point(78, 352);
      this.editMarkerLabelsFilename.Name = "editMarkerLabelsFilename";
      this.editMarkerLabelsFilename.Size = new System.Drawing.Size(195, 20);
      this.editMarkerLabelsFilename.TabIndex = 26;
      //
      // labelMarkerLabelsPrefix
      //
      this.labelMarkerLabelsPrefix.AutoSize = true;
      this.labelMarkerLabelsPrefix.Location = new System.Drawing.Point(325, 308);
      this.labelMarkerLabelsPrefix.Name = "labelMarkerLabelsPrefix";
      this.labelMarkerLabelsPrefix.Size = new System.Drawing.Size(62, 13);
      this.labelMarkerLabelsPrefix.TabIndex = 29;
      this.labelMarkerLabelsPrefix.Text = "Prefix text:";
      //
      // editMarkerLabelsPrefix
      //
      this.editMarkerLabelsPrefix.Enabled = false;
      this.editMarkerLabelsPrefix.Location = new System.Drawing.Point(325, 326);
      this.editMarkerLabelsPrefix.Multiline = true;
      this.editMarkerLabelsPrefix.Name = "editMarkerLabelsPrefix";
      this.editMarkerLabelsPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.editMarkerLabelsPrefix.Size = new System.Drawing.Size(280, 46);
      this.editMarkerLabelsPrefix.TabIndex = 30;
      this.editMarkerLabelsPrefix.Font = new System.Drawing.Font("Courier New", 8.25F);
      //
      // checkExportEntityLabels
      //
      this.checkExportEntityLabels.AutoSize = true;
      this.checkExportEntityLabels.Checked = false;
      this.checkExportEntityLabels.CheckState = System.Windows.Forms.CheckState.Unchecked;
      this.checkExportEntityLabels.Location = new System.Drawing.Point(3, 380);
      this.checkExportEntityLabels.Name = "checkExportEntityLabels";
      this.checkExportEntityLabels.Size = new System.Drawing.Size(220, 17);
      this.checkExportEntityLabels.TabIndex = 31;
      this.checkExportEntityLabels.Text = "Generate entity labels sidecar file";
      this.checkExportEntityLabels.UseVisualStyleBackColor = true;
      this.checkExportEntityLabels.CheckedChanged += new System.EventHandler(this.checkExportEntityLabels_CheckedChanged);
      //
      // labelEntityLabelsDirectory
      //
      this.labelEntityLabelsDirectory.AutoSize = true;
      this.labelEntityLabelsDirectory.Location = new System.Drawing.Point(20, 404);
      this.labelEntityLabelsDirectory.Name = "labelEntityLabelsDirectory";
      this.labelEntityLabelsDirectory.Size = new System.Drawing.Size(52, 13);
      this.labelEntityLabelsDirectory.TabIndex = 32;
      this.labelEntityLabelsDirectory.Text = "Directory:";
      //
      // editEntityLabelsDirectory
      //
      this.editEntityLabelsDirectory.Enabled = false;
      this.editEntityLabelsDirectory.Location = new System.Drawing.Point(78, 401);
      this.editEntityLabelsDirectory.Name = "editEntityLabelsDirectory";
      this.editEntityLabelsDirectory.Size = new System.Drawing.Size(195, 20);
      this.editEntityLabelsDirectory.TabIndex = 33;
      //
      // btnBrowseEntityLabelsDirectory
      //
      this.btnBrowseEntityLabelsDirectory.Enabled = false;
      this.btnBrowseEntityLabelsDirectory.Location = new System.Drawing.Point(279, 399);
      this.btnBrowseEntityLabelsDirectory.Name = "btnBrowseEntityLabelsDirectory";
      this.btnBrowseEntityLabelsDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseEntityLabelsDirectory.TabIndex = 34;
      this.btnBrowseEntityLabelsDirectory.Text = "...";
      this.btnBrowseEntityLabelsDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseEntityLabelsDirectory.Click += new System.EventHandler(this.btnBrowseEntityLabelsDirectory_Click);
      //
      // labelEntityLabelsFilename
      //
      this.labelEntityLabelsFilename.AutoSize = true;
      this.labelEntityLabelsFilename.Location = new System.Drawing.Point(20, 430);
      this.labelEntityLabelsFilename.Name = "labelEntityLabelsFilename";
      this.labelEntityLabelsFilename.Size = new System.Drawing.Size(52, 13);
      this.labelEntityLabelsFilename.TabIndex = 35;
      this.labelEntityLabelsFilename.Text = "Filename:";
      //
      // editEntityLabelsFilename
      //
      this.editEntityLabelsFilename.Enabled = false;
      this.editEntityLabelsFilename.Location = new System.Drawing.Point(78, 427);
      this.editEntityLabelsFilename.Name = "editEntityLabelsFilename";
      this.editEntityLabelsFilename.Size = new System.Drawing.Size(195, 20);
      this.editEntityLabelsFilename.TabIndex = 36;
      //
      // labelEntityLabelsPrefix
      //
      this.labelEntityLabelsPrefix.AutoSize = true;
      this.labelEntityLabelsPrefix.Location = new System.Drawing.Point(325, 383);
      this.labelEntityLabelsPrefix.Name = "labelEntityLabelsPrefix";
      this.labelEntityLabelsPrefix.Size = new System.Drawing.Size(62, 13);
      this.labelEntityLabelsPrefix.TabIndex = 37;
      this.labelEntityLabelsPrefix.Text = "Prefix text:";
      //
      // editEntityLabelsPrefix
      //
      this.editEntityLabelsPrefix.Enabled = false;
      this.editEntityLabelsPrefix.Location = new System.Drawing.Point(325, 401);
      this.editEntityLabelsPrefix.Multiline = true;
      this.editEntityLabelsPrefix.Name = "editEntityLabelsPrefix";
      this.editEntityLabelsPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.editEntityLabelsPrefix.Size = new System.Drawing.Size(280, 46);
      this.editEntityLabelsPrefix.TabIndex = 38;
      this.editEntityLabelsPrefix.Font = new System.Drawing.Font("Courier New", 8.25F);
      //
      // checkExportMapStrings
      //
      this.checkExportMapStrings.AutoSize = true;
      this.checkExportMapStrings.Checked = false;
      this.checkExportMapStrings.CheckState = System.Windows.Forms.CheckState.Unchecked;
      this.checkExportMapStrings.Location = new System.Drawing.Point(3, 475);
      this.checkExportMapStrings.Name = "checkExportMapStrings";
      this.checkExportMapStrings.Size = new System.Drawing.Size(220, 17);
      this.checkExportMapStrings.TabIndex = 39;
      this.checkExportMapStrings.Text = "Generate map strings sidecar file";
      this.checkExportMapStrings.UseVisualStyleBackColor = true;
      this.checkExportMapStrings.CheckedChanged += new System.EventHandler(this.checkExportMapStrings_CheckedChanged);
      //
      // labelMapStringsDirectory
      //
      this.labelMapStringsDirectory.AutoSize = true;
      this.labelMapStringsDirectory.Location = new System.Drawing.Point(20, 499);
      this.labelMapStringsDirectory.Name = "labelMapStringsDirectory";
      this.labelMapStringsDirectory.Size = new System.Drawing.Size(52, 13);
      this.labelMapStringsDirectory.TabIndex = 40;
      this.labelMapStringsDirectory.Text = "Directory:";
      //
      // editMapStringsDirectory
      //
      this.editMapStringsDirectory.Enabled = false;
      this.editMapStringsDirectory.Location = new System.Drawing.Point(78, 496);
      this.editMapStringsDirectory.Name = "editMapStringsDirectory";
      this.editMapStringsDirectory.Size = new System.Drawing.Size(195, 20);
      this.editMapStringsDirectory.TabIndex = 41;
      //
      // btnBrowseMapStringsDirectory
      //
      this.btnBrowseMapStringsDirectory.Enabled = false;
      this.btnBrowseMapStringsDirectory.Location = new System.Drawing.Point(279, 494);
      this.btnBrowseMapStringsDirectory.Name = "btnBrowseMapStringsDirectory";
      this.btnBrowseMapStringsDirectory.Size = new System.Drawing.Size(30, 23);
      this.btnBrowseMapStringsDirectory.TabIndex = 42;
      this.btnBrowseMapStringsDirectory.Text = "...";
      this.btnBrowseMapStringsDirectory.UseVisualStyleBackColor = true;
      this.btnBrowseMapStringsDirectory.Click += new System.EventHandler(this.btnBrowseMapStringsDirectory_Click);
      //
      // labelMapStringsFilename
      //
      this.labelMapStringsFilename.AutoSize = true;
      this.labelMapStringsFilename.Location = new System.Drawing.Point(20, 525);
      this.labelMapStringsFilename.Name = "labelMapStringsFilename";
      this.labelMapStringsFilename.Size = new System.Drawing.Size(52, 13);
      this.labelMapStringsFilename.TabIndex = 43;
      this.labelMapStringsFilename.Text = "Filename:";
      //
      // editMapStringsFilename
      //
      this.editMapStringsFilename.Enabled = false;
      this.editMapStringsFilename.Location = new System.Drawing.Point(78, 522);
      this.editMapStringsFilename.Name = "editMapStringsFilename";
      this.editMapStringsFilename.Size = new System.Drawing.Size(195, 20);
      this.editMapStringsFilename.TabIndex = 44;
      //
      // labelMapStringsPrefix
      //
      this.labelMapStringsPrefix.AutoSize = true;
      this.labelMapStringsPrefix.Location = new System.Drawing.Point(325, 478);
      this.labelMapStringsPrefix.Name = "labelMapStringsPrefix";
      this.labelMapStringsPrefix.Size = new System.Drawing.Size(62, 13);
      this.labelMapStringsPrefix.TabIndex = 45;
      this.labelMapStringsPrefix.Text = "Prefix text:";
      //
      // editMapStringsPrefix
      //
      this.editMapStringsPrefix.Enabled = false;
      this.editMapStringsPrefix.Location = new System.Drawing.Point(325, 496);
      this.editMapStringsPrefix.Multiline = true;
      this.editMapStringsPrefix.Name = "editMapStringsPrefix";
      this.editMapStringsPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.editMapStringsPrefix.Size = new System.Drawing.Size(280, 46);
      this.editMapStringsPrefix.TabIndex = 46;
      this.editMapStringsPrefix.Font = new System.Drawing.Font("Courier New", 8.25F);
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
      this.groupCharset.Location = new System.Drawing.Point(3, 552);
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
      this.Controls.Add(this.editMarkerLabelsPrefix);
      this.Controls.Add(this.labelMarkerLabelsPrefix);
      this.Controls.Add(this.editMarkerLabelsFilename);
      this.Controls.Add(this.labelMarkerLabelsFilename);
      this.Controls.Add(this.btnBrowseMarkerLabelsDirectory);
      this.Controls.Add(this.editMarkerLabelsDirectory);
      this.Controls.Add(this.labelMarkerLabelsDirectory);
      this.Controls.Add(this.checkExportMarkerLabels);
      this.Controls.Add(this.editEntityLabelsPrefix);
      this.Controls.Add(this.labelEntityLabelsPrefix);
      this.Controls.Add(this.editEntityLabelsFilename);
      this.Controls.Add(this.labelEntityLabelsFilename);
      this.Controls.Add(this.btnBrowseEntityLabelsDirectory);
      this.Controls.Add(this.editEntityLabelsDirectory);
      this.Controls.Add(this.labelEntityLabelsDirectory);
      this.Controls.Add(this.checkExportEntityLabels);
      this.Controls.Add(this.editMapStringsPrefix);
      this.Controls.Add(this.labelMapStringsPrefix);
      this.Controls.Add(this.editMapStringsFilename);
      this.Controls.Add(this.labelMapStringsFilename);
      this.Controls.Add(this.btnBrowseMapStringsDirectory);
      this.Controls.Add(this.editMapStringsDirectory);
      this.Controls.Add(this.labelMapStringsDirectory);
      this.Controls.Add(this.checkExportMapStrings);
      this.Controls.Add(this.editHeaderAsmPrefix);
      this.Controls.Add(this.labelHeaderAsmPrefix);
      this.Controls.Add(this.editHeaderAsmFilename);
      this.Controls.Add(this.labelHeaderAsmFilename);
      this.Controls.Add(this.btnBrowseHeaderAsmDirectory);
      this.Controls.Add(this.editHeaderAsmDirectory);
      this.Controls.Add(this.labelHeaderAsmDirectory);
      this.Controls.Add(this.checkExportHeaderAsm);
      this.Controls.Add(this.checkGenerateDefFile);
      this.Controls.Add(this.editExportFilename);
      this.Controls.Add(this.labelExportFilename);
      this.Controls.Add(this.btnBrowseExportDirectory);
      this.Controls.Add(this.editExportDirectory);
      this.Controls.Add(this.labelExportDirectory);
      this.Controls.Add(this.checkSaveOnExport);
      this.Controls.Add(this.editPrefixLoadAddress);
      this.Controls.Add(this.checkPrefixLoadAddress);
      this.Controls.Add(this.labelMaxExportSize);
      this.Controls.Add(this.editMaxExportSize);
      this.Controls.Add(this.checkMaxExportSize);
      this.Controls.Add(this.checkExportPassable);
      this.Controls.Add(this.checkExportColors);
      this.Controls.Add(this.checkExportMarkers);
      this.Name = "ExportMapAsGameBinary";
      this.Size = new System.Drawing.Size(620, 710);
      this.groupCharset.ResumeLayout(false);
      this.groupCharset.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.CheckBox checkExportMarkers;
    private System.Windows.Forms.CheckBox checkExportColors;
    private System.Windows.Forms.CheckBox checkExportPassable;
    private System.Windows.Forms.CheckBox checkPrefixLoadAddress;
    private System.Windows.Forms.TextBox editPrefixLoadAddress;
    private System.Windows.Forms.CheckBox checkMaxExportSize;
    private System.Windows.Forms.TextBox editMaxExportSize;
    private System.Windows.Forms.Label labelMaxExportSize;
    private System.Windows.Forms.CheckBox checkSaveOnExport;
    private System.Windows.Forms.Label labelExportDirectory;
    private System.Windows.Forms.TextBox editExportDirectory;
    private System.Windows.Forms.Button btnBrowseExportDirectory;
    private System.Windows.Forms.Label labelExportFilename;
    private System.Windows.Forms.TextBox editExportFilename;
    private System.Windows.Forms.CheckBox checkGenerateDefFile;
    private System.Windows.Forms.CheckBox checkExportHeaderAsm;
    private System.Windows.Forms.Label labelHeaderAsmDirectory;
    private System.Windows.Forms.TextBox editHeaderAsmDirectory;
    private System.Windows.Forms.Button btnBrowseHeaderAsmDirectory;
    private System.Windows.Forms.Label labelHeaderAsmFilename;
    private System.Windows.Forms.TextBox editHeaderAsmFilename;
    private System.Windows.Forms.Label labelHeaderAsmPrefix;
    private System.Windows.Forms.TextBox editHeaderAsmPrefix;
    private System.Windows.Forms.CheckBox checkExportMarkerLabels;
    private System.Windows.Forms.Label labelMarkerLabelsDirectory;
    private System.Windows.Forms.TextBox editMarkerLabelsDirectory;
    private System.Windows.Forms.Button btnBrowseMarkerLabelsDirectory;
    private System.Windows.Forms.Label labelMarkerLabelsFilename;
    private System.Windows.Forms.TextBox editMarkerLabelsFilename;
    private System.Windows.Forms.Label labelMarkerLabelsPrefix;
    private System.Windows.Forms.TextBox editMarkerLabelsPrefix;
    private System.Windows.Forms.CheckBox checkExportEntityLabels;
    private System.Windows.Forms.Label labelEntityLabelsDirectory;
    private System.Windows.Forms.TextBox editEntityLabelsDirectory;
    private System.Windows.Forms.Button btnBrowseEntityLabelsDirectory;
    private System.Windows.Forms.Label labelEntityLabelsFilename;
    private System.Windows.Forms.TextBox editEntityLabelsFilename;
    private System.Windows.Forms.Label labelEntityLabelsPrefix;
    private System.Windows.Forms.TextBox editEntityLabelsPrefix;
    private System.Windows.Forms.CheckBox checkExportMapStrings;
    private System.Windows.Forms.Label labelMapStringsDirectory;
    private System.Windows.Forms.TextBox editMapStringsDirectory;
    private System.Windows.Forms.Button btnBrowseMapStringsDirectory;
    private System.Windows.Forms.Label labelMapStringsFilename;
    private System.Windows.Forms.TextBox editMapStringsFilename;
    private System.Windows.Forms.Label labelMapStringsPrefix;
    private System.Windows.Forms.TextBox editMapStringsPrefix;
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
