
namespace RetroDevStudio.Controls
{
  partial class ExportMapAsAssembly
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
            this.checkExportHex = new System.Windows.Forms.CheckBox();
            this.checkExportSparseMaps = new System.Windows.Forms.CheckBox();
            this.checkWrapMapData = new System.Windows.Forms.CheckBox();
            this.checkVariableNameLabelPrefix = new System.Windows.Forms.CheckBox();
            this.editVariableNameLabelPrefix = new System.Windows.Forms.TextBox();
            this.checkIncludeSemicolonAfterSimpleLabels = new System.Windows.Forms.CheckBox();
            this.checkCommentCharacters = new System.Windows.Forms.CheckBox();
            this.editCommentCharacters = new System.Windows.Forms.TextBox();
            this.checkExportToDataIncludeRes = new System.Windows.Forms.CheckBox();
            this.checkExportToDataWrap = new System.Windows.Forms.CheckBox();
            this.editWrapByteCount = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.editPrefix = new System.Windows.Forms.TextBox();
            this.checkAddFilenamespace = new System.Windows.Forms.CheckBox();
            this.editFilenamespace = new System.Windows.Forms.TextBox();
            this.checkEmptyTile = new System.Windows.Forms.CheckBox();
            this.editEmptyTileIndex = new System.Windows.Forms.TextBox();
            this.checkExportTilesetColors = new System.Windows.Forms.CheckBox();
            this.checkExportMapColors = new System.Windows.Forms.CheckBox();
            this.groupAutoSave = new System.Windows.Forms.GroupBox();
            this.editExportFilename = new System.Windows.Forms.TextBox();
            this.labelExportFilename = new System.Windows.Forms.Label();
            this.btnBrowseExportDirectory = new System.Windows.Forms.Button();
            this.editExportDirectory = new System.Windows.Forms.TextBox();
            this.labelExportDirectory = new System.Windows.Forms.Label();
            this.checkSaveOnExport = new System.Windows.Forms.CheckBox();
            this.groupCharset = new System.Windows.Forms.GroupBox();
            this.editCharsetExportFilename = new System.Windows.Forms.TextBox();
            this.labelCharsetExportFilename = new System.Windows.Forms.Label();
            this.btnBrowseCharsetExportDirectory = new System.Windows.Forms.Button();
            this.editCharsetExportDirectory = new System.Windows.Forms.TextBox();
            this.labelCharsetExportDirectory = new System.Windows.Forms.Label();
            this.checkExportCharset = new System.Windows.Forms.CheckBox();
            this.checkAlwaysOverwrite = new System.Windows.Forms.CheckBox();
            this.groupAutoSave.SuspendLayout();
            this.groupCharset.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkExportHex
            // 
            this.checkExportHex.AutoSize = true;
            this.checkExportHex.Checked = true;
            this.checkExportHex.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkExportHex.Location = new System.Drawing.Point(3, 52);
            this.checkExportHex.Name = "checkExportHex";
            this.checkExportHex.Size = new System.Drawing.Size(141, 17);
            this.checkExportHex.TabIndex = 4;
            this.checkExportHex.Text = "Export with Hex notation";
            this.checkExportHex.UseVisualStyleBackColor = true;
            this.checkExportHex.CheckedChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkExportSparseMaps
            // 
            this.checkExportSparseMaps.AutoSize = true;
            this.checkExportSparseMaps.Location = new System.Drawing.Point(3, 76);
            this.checkExportSparseMaps.Name = "checkExportSparseMaps";
            this.checkExportSparseMaps.Size = new System.Drawing.Size(118, 17);
            this.checkExportSparseMaps.TabIndex = 6;
            this.checkExportSparseMaps.Text = "Export sparse maps";
            this.checkExportSparseMaps.UseVisualStyleBackColor = true;
            this.checkExportSparseMaps.CheckedChanged += new System.EventHandler(this.checkExportSparseMaps_CheckedChanged);
            // 
            // checkWrapMapData
            // 
            this.checkWrapMapData.AutoSize = true;
            this.checkWrapMapData.Checked = true;
            this.checkWrapMapData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkWrapMapData.Location = new System.Drawing.Point(3, 100);
            this.checkWrapMapData.Name = "checkWrapMapData";
            this.checkWrapMapData.Size = new System.Drawing.Size(99, 17);
            this.checkWrapMapData.TabIndex = 8;
            this.checkWrapMapData.Text = "Wrap map data";
            this.checkWrapMapData.UseVisualStyleBackColor = true;
            this.checkWrapMapData.CheckedChanged += new System.EventHandler(this.checkWrapMapData_CheckedChanged);
            // 
            // checkVariableNameLabelPrefix
            // 
            this.checkVariableNameLabelPrefix.AutoSize = true;
            this.checkVariableNameLabelPrefix.Location = new System.Drawing.Point(3, 124);
            this.checkVariableNameLabelPrefix.Name = "checkVariableNameLabelPrefix";
            this.checkVariableNameLabelPrefix.Size = new System.Drawing.Size(147, 17);
            this.checkVariableNameLabelPrefix.TabIndex = 10;
            this.checkVariableNameLabelPrefix.Text = "Variable name label Prefix";
            this.checkVariableNameLabelPrefix.UseVisualStyleBackColor = true;
            this.checkVariableNameLabelPrefix.CheckedChanged += new System.EventHandler(this.checkVariableNameLabelPrefix_CheckedChanged);
            // 
            // editVariableNameLabelPrefix
            // 
            this.editVariableNameLabelPrefix.Location = new System.Drawing.Point(200, 122);
            this.editVariableNameLabelPrefix.Name = "editVariableNameLabelPrefix";
            this.editVariableNameLabelPrefix.Size = new System.Drawing.Size(64, 20);
            this.editVariableNameLabelPrefix.TabIndex = 11;
            this.editVariableNameLabelPrefix.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkIncludeSemicolonAfterSimpleLabels
            // 
            this.checkIncludeSemicolonAfterSimpleLabels.AutoSize = true;
            this.checkIncludeSemicolonAfterSimpleLabels.Location = new System.Drawing.Point(3, 148);
            this.checkIncludeSemicolonAfterSimpleLabels.Name = "checkIncludeSemicolonAfterSimpleLabels";
            this.checkIncludeSemicolonAfterSimpleLabels.Size = new System.Drawing.Size(176, 17);
            this.checkIncludeSemicolonAfterSimpleLabels.TabIndex = 12;
            this.checkIncludeSemicolonAfterSimpleLabels.Text = "Include colon after simple labels";
            this.checkIncludeSemicolonAfterSimpleLabels.UseVisualStyleBackColor = true;
            this.checkIncludeSemicolonAfterSimpleLabels.CheckedChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkCommentCharacters
            // 
            this.checkCommentCharacters.AutoSize = true;
            this.checkCommentCharacters.Checked = true;
            this.checkCommentCharacters.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkCommentCharacters.Location = new System.Drawing.Point(420, 3);
            this.checkCommentCharacters.Name = "checkCommentCharacters";
            this.checkCommentCharacters.Size = new System.Drawing.Size(123, 17);
            this.checkCommentCharacters.TabIndex = 16;
            this.checkCommentCharacters.Text = "Comment characters";
            this.checkCommentCharacters.UseVisualStyleBackColor = true;
            this.checkCommentCharacters.CheckedChanged += new System.EventHandler(this.checkCommentCharacters_CheckedChanged);
            // 
            // editCommentCharacters
            // 
            this.editCommentCharacters.Location = new System.Drawing.Point(617, 1);
            this.editCommentCharacters.Name = "editCommentCharacters";
            this.editCommentCharacters.Size = new System.Drawing.Size(64, 20);
            this.editCommentCharacters.TabIndex = 17;
            this.editCommentCharacters.Text = ";";
            this.editCommentCharacters.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkExportToDataIncludeRes
            // 
            this.checkExportToDataIncludeRes.AutoSize = true;
            this.checkExportToDataIncludeRes.Checked = true;
            this.checkExportToDataIncludeRes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkExportToDataIncludeRes.Location = new System.Drawing.Point(3, 3);
            this.checkExportToDataIncludeRes.Name = "checkExportToDataIncludeRes";
            this.checkExportToDataIncludeRes.Size = new System.Drawing.Size(74, 17);
            this.checkExportToDataIncludeRes.TabIndex = 0;
            this.checkExportToDataIncludeRes.Text = "Prefix with";
            this.checkExportToDataIncludeRes.UseVisualStyleBackColor = true;
            this.checkExportToDataIncludeRes.CheckedChanged += new System.EventHandler(this.checkExportToDataIncludeRes_CheckedChanged);
            // 
            // checkExportToDataWrap
            // 
            this.checkExportToDataWrap.AutoSize = true;
            this.checkExportToDataWrap.Checked = true;
            this.checkExportToDataWrap.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkExportToDataWrap.Location = new System.Drawing.Point(3, 27);
            this.checkExportToDataWrap.Name = "checkExportToDataWrap";
            this.checkExportToDataWrap.Size = new System.Drawing.Size(64, 17);
            this.checkExportToDataWrap.TabIndex = 2;
            this.checkExportToDataWrap.Text = "Wrap at";
            this.checkExportToDataWrap.UseVisualStyleBackColor = true;
            this.checkExportToDataWrap.CheckedChanged += new System.EventHandler(this.checkExportToDataWrap_CheckedChanged);
            // 
            // editWrapByteCount
            // 
            this.editWrapByteCount.Location = new System.Drawing.Point(99, 25);
            this.editWrapByteCount.Name = "editWrapByteCount";
            this.editWrapByteCount.Size = new System.Drawing.Size(64, 20);
            this.editWrapByteCount.TabIndex = 3;
            this.editWrapByteCount.Text = "8";
            this.editWrapByteCount.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(169, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "bytes";
            // 
            // editPrefix
            // 
            this.editPrefix.Location = new System.Drawing.Point(99, 1);
            this.editPrefix.Name = "editPrefix";
            this.editPrefix.Size = new System.Drawing.Size(64, 20);
            this.editPrefix.TabIndex = 1;
            this.editPrefix.Text = "!byte ";
            this.editPrefix.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkAddFilenamespace
            // 
            this.checkAddFilenamespace.AutoSize = true;
            this.checkAddFilenamespace.Location = new System.Drawing.Point(3, 173);
            this.checkAddFilenamespace.Name = "checkAddFilenamespace";
            this.checkAddFilenamespace.Size = new System.Drawing.Size(116, 17);
            this.checkAddFilenamespace.TabIndex = 14;
            this.checkAddFilenamespace.Text = "Add filenamespace";
            this.checkAddFilenamespace.UseVisualStyleBackColor = true;
            this.checkAddFilenamespace.CheckedChanged += new System.EventHandler(this.checkAddFilenamespace_CheckedChanged);
            // 
            // editFilenamespace
            // 
            this.editFilenamespace.Enabled = false;
            this.editFilenamespace.Location = new System.Drawing.Point(200, 170);
            this.editFilenamespace.Name = "editFilenamespace";
            this.editFilenamespace.Size = new System.Drawing.Size(200, 20);
            this.editFilenamespace.TabIndex = 15;
            this.editFilenamespace.TextChanged += new System.EventHandler(this.editFilenamespace_TextChanged);
            // 
            // checkEmptyTile
            // 
            this.checkEmptyTile.AutoSize = true;
            this.checkEmptyTile.Location = new System.Drawing.Point(420, 27);
            this.checkEmptyTile.Name = "checkEmptyTile";
            this.checkEmptyTile.Size = new System.Drawing.Size(109, 17);
            this.checkEmptyTile.TabIndex = 18;
            this.checkEmptyTile.Text = "Empty tile number";
            this.checkEmptyTile.UseVisualStyleBackColor = true;
            this.checkEmptyTile.CheckedChanged += new System.EventHandler(this.checkEmptyTile_CheckedChanged);
            // 
            // editEmptyTileIndex
            // 
            this.editEmptyTileIndex.Location = new System.Drawing.Point(617, 25);
            this.editEmptyTileIndex.Name = "editEmptyTileIndex";
            this.editEmptyTileIndex.Size = new System.Drawing.Size(64, 20);
            this.editEmptyTileIndex.TabIndex = 19;
            this.editEmptyTileIndex.Text = "0";
            this.editEmptyTileIndex.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkExportTilesetColors
            // 
            this.checkExportTilesetColors.AutoSize = true;
            this.checkExportTilesetColors.Location = new System.Drawing.Point(420, 52);
            this.checkExportTilesetColors.Name = "checkExportTilesetColors";
            this.checkExportTilesetColors.Size = new System.Drawing.Size(122, 17);
            this.checkExportTilesetColors.TabIndex = 20;
            this.checkExportTilesetColors.Text = "Include tileset colors";
            this.checkExportTilesetColors.UseVisualStyleBackColor = true;
            this.checkExportTilesetColors.CheckedChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // checkExportMapColors
            // 
            this.checkExportMapColors.AutoSize = true;
            this.checkExportMapColors.Location = new System.Drawing.Point(420, 76);
            this.checkExportMapColors.Name = "checkExportMapColors";
            this.checkExportMapColors.Size = new System.Drawing.Size(115, 17);
            this.checkExportMapColors.TabIndex = 21;
            this.checkExportMapColors.Text = "Include map colors";
            this.checkExportMapColors.UseVisualStyleBackColor = true;
            this.checkExportMapColors.CheckedChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // groupAutoSave
            // 
            this.groupAutoSave.Controls.Add(this.editExportFilename);
            this.groupAutoSave.Controls.Add(this.labelExportFilename);
            this.groupAutoSave.Controls.Add(this.btnBrowseExportDirectory);
            this.groupAutoSave.Controls.Add(this.editExportDirectory);
            this.groupAutoSave.Controls.Add(this.labelExportDirectory);
            this.groupAutoSave.Controls.Add(this.checkSaveOnExport);
            this.groupAutoSave.Location = new System.Drawing.Point(3, 210);
            this.groupAutoSave.Name = "groupAutoSave";
            this.groupAutoSave.Size = new System.Drawing.Size(397, 100);
            this.groupAutoSave.TabIndex = 23;
            this.groupAutoSave.TabStop = false;
            this.groupAutoSave.Text = "Auto save";
            // 
            // editExportFilename
            // 
            this.editExportFilename.Enabled = false;
            this.editExportFilename.Location = new System.Drawing.Point(97, 65);
            this.editExportFilename.Name = "editExportFilename";
            this.editExportFilename.Size = new System.Drawing.Size(214, 20);
            this.editExportFilename.TabIndex = 5;
            this.editExportFilename.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // labelExportFilename
            // 
            this.labelExportFilename.AutoSize = true;
            this.labelExportFilename.Location = new System.Drawing.Point(6, 68);
            this.labelExportFilename.Name = "labelExportFilename";
            this.labelExportFilename.Size = new System.Drawing.Size(82, 13);
            this.labelExportFilename.TabIndex = 4;
            this.labelExportFilename.Text = "Export filename:";
            // 
            // btnBrowseExportDirectory
            // 
            this.btnBrowseExportDirectory.Enabled = false;
            this.btnBrowseExportDirectory.Location = new System.Drawing.Point(317, 37);
            this.btnBrowseExportDirectory.Name = "btnBrowseExportDirectory";
            this.btnBrowseExportDirectory.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseExportDirectory.TabIndex = 3;
            this.btnBrowseExportDirectory.Text = "Browse...";
            this.btnBrowseExportDirectory.UseVisualStyleBackColor = true;
            this.btnBrowseExportDirectory.Click += new System.EventHandler(this.btnBrowseExportDirectory_Click);
            // 
            // editExportDirectory
            // 
            this.editExportDirectory.Enabled = false;
            this.editExportDirectory.Location = new System.Drawing.Point(97, 39);
            this.editExportDirectory.Name = "editExportDirectory";
            this.editExportDirectory.Size = new System.Drawing.Size(214, 20);
            this.editExportDirectory.TabIndex = 2;
            this.editExportDirectory.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // labelExportDirectory
            // 
            this.labelExportDirectory.AutoSize = true;
            this.labelExportDirectory.Location = new System.Drawing.Point(6, 42);
            this.labelExportDirectory.Name = "labelExportDirectory";
            this.labelExportDirectory.Size = new System.Drawing.Size(83, 13);
            this.labelExportDirectory.TabIndex = 1;
            this.labelExportDirectory.Text = "Export directory:";
            // 
            // checkSaveOnExport
            // 
            this.checkSaveOnExport.AutoSize = true;
            this.checkSaveOnExport.Location = new System.Drawing.Point(6, 19);
            this.checkSaveOnExport.Name = "checkSaveOnExport";
            this.checkSaveOnExport.Size = new System.Drawing.Size(98, 17);
            this.checkSaveOnExport.TabIndex = 0;
            this.checkSaveOnExport.Text = "Save on export";
            this.checkSaveOnExport.UseVisualStyleBackColor = true;
            this.checkSaveOnExport.CheckedChanged += new System.EventHandler(this.checkSaveOnExport_CheckedChanged);
            // 
            // groupCharset
            // 
            this.groupCharset.Controls.Add(this.editCharsetExportFilename);
            this.groupCharset.Controls.Add(this.labelCharsetExportFilename);
            this.groupCharset.Controls.Add(this.btnBrowseCharsetExportDirectory);
            this.groupCharset.Controls.Add(this.editCharsetExportDirectory);
            this.groupCharset.Controls.Add(this.labelCharsetExportDirectory);
            this.groupCharset.Controls.Add(this.checkExportCharset);
            this.groupCharset.Location = new System.Drawing.Point(3, 316);
            this.groupCharset.Name = "groupCharset";
            this.groupCharset.Size = new System.Drawing.Size(397, 100);
            this.groupCharset.TabIndex = 24;
            this.groupCharset.TabStop = false;
            this.groupCharset.Text = "Character set";
            // 
            // editCharsetExportFilename
            // 
            this.editCharsetExportFilename.Enabled = false;
            this.editCharsetExportFilename.Location = new System.Drawing.Point(97, 65);
            this.editCharsetExportFilename.Name = "editCharsetExportFilename";
            this.editCharsetExportFilename.Size = new System.Drawing.Size(214, 20);
            this.editCharsetExportFilename.TabIndex = 5;
            this.editCharsetExportFilename.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // labelCharsetExportFilename
            // 
            this.labelCharsetExportFilename.AutoSize = true;
            this.labelCharsetExportFilename.Location = new System.Drawing.Point(6, 68);
            this.labelCharsetExportFilename.Name = "labelCharsetExportFilename";
            this.labelCharsetExportFilename.Size = new System.Drawing.Size(82, 13);
            this.labelCharsetExportFilename.TabIndex = 4;
            this.labelCharsetExportFilename.Text = "Export filename:";
            // 
            // btnBrowseCharsetExportDirectory
            // 
            this.btnBrowseCharsetExportDirectory.Enabled = false;
            this.btnBrowseCharsetExportDirectory.Location = new System.Drawing.Point(317, 37);
            this.btnBrowseCharsetExportDirectory.Name = "btnBrowseCharsetExportDirectory";
            this.btnBrowseCharsetExportDirectory.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseCharsetExportDirectory.TabIndex = 3;
            this.btnBrowseCharsetExportDirectory.Text = "Browse...";
            this.btnBrowseCharsetExportDirectory.UseVisualStyleBackColor = true;
            this.btnBrowseCharsetExportDirectory.Click += new System.EventHandler(this.btnBrowseCharsetExportDirectory_Click);
            // 
            // editCharsetExportDirectory
            // 
            this.editCharsetExportDirectory.Enabled = false;
            this.editCharsetExportDirectory.Location = new System.Drawing.Point(97, 39);
            this.editCharsetExportDirectory.Name = "editCharsetExportDirectory";
            this.editCharsetExportDirectory.Size = new System.Drawing.Size(214, 20);
            this.editCharsetExportDirectory.TabIndex = 2;
            this.editCharsetExportDirectory.TextChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // labelCharsetExportDirectory
            // 
            this.labelCharsetExportDirectory.AutoSize = true;
            this.labelCharsetExportDirectory.Location = new System.Drawing.Point(6, 42);
            this.labelCharsetExportDirectory.Name = "labelCharsetExportDirectory";
            this.labelCharsetExportDirectory.Size = new System.Drawing.Size(83, 13);
            this.labelCharsetExportDirectory.TabIndex = 1;
            this.labelCharsetExportDirectory.Text = "Export directory:";
            // 
            // checkExportCharset
            // 
            this.checkExportCharset.AutoSize = true;
            this.checkExportCharset.Location = new System.Drawing.Point(6, 19);
            this.checkExportCharset.Name = "checkExportCharset";
            this.checkExportCharset.Size = new System.Drawing.Size(121, 17);
            this.checkExportCharset.TabIndex = 0;
            this.checkExportCharset.Text = "Export character set";
            this.checkExportCharset.UseVisualStyleBackColor = true;
            this.checkExportCharset.CheckedChanged += new System.EventHandler(this.checkExportCharset_CheckedChanged);
            // 
            // checkAlwaysOverwrite
            // 
            this.checkAlwaysOverwrite.AutoSize = true;
            this.checkAlwaysOverwrite.Location = new System.Drawing.Point(420, 100);
            this.checkAlwaysOverwrite.Name = "checkAlwaysOverwrite";
            this.checkAlwaysOverwrite.Size = new System.Drawing.Size(152, 17);
            this.checkAlwaysOverwrite.TabIndex = 22;
            this.checkAlwaysOverwrite.Text = "Always overwrite on export";
            this.checkAlwaysOverwrite.UseVisualStyleBackColor = true;
            this.checkAlwaysOverwrite.CheckedChanged += new System.EventHandler(this.HandleSettingsChanged);
            // 
            // ExportMapAsAssembly
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkAlwaysOverwrite);
            this.Controls.Add(this.groupCharset);
            this.Controls.Add(this.groupAutoSave);
            this.Controls.Add(this.checkExportMapColors);
            this.Controls.Add(this.checkExportTilesetColors);
            this.Controls.Add(this.editEmptyTileIndex);
            this.Controls.Add(this.checkEmptyTile);
            this.Controls.Add(this.editFilenamespace);
            this.Controls.Add(this.checkAddFilenamespace);
            this.Controls.Add(this.editCommentCharacters);
            this.Controls.Add(this.checkCommentCharacters);
            this.Controls.Add(this.checkIncludeSemicolonAfterSimpleLabels);
            this.Controls.Add(this.editVariableNameLabelPrefix);
            this.Controls.Add(this.checkVariableNameLabelPrefix);
            this.Controls.Add(this.checkExportHex);
            this.Controls.Add(this.checkExportToDataIncludeRes);
            this.Controls.Add(this.checkExportToDataWrap);
            this.Controls.Add(this.editWrapByteCount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.editPrefix);
            this.Controls.Add(this.checkExportSparseMaps);
            this.Controls.Add(this.checkWrapMapData);
            this.Name = "ExportMapAsAssembly";
            this.Size = new System.Drawing.Size(840, 568);
            this.groupAutoSave.ResumeLayout(false);
            this.groupAutoSave.PerformLayout();
            this.groupCharset.ResumeLayout(false);
            this.groupCharset.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.CheckBox checkExportHex;
    private System.Windows.Forms.CheckBox checkVariableNameLabelPrefix;
    private System.Windows.Forms.TextBox editVariableNameLabelPrefix;
    private System.Windows.Forms.CheckBox checkIncludeSemicolonAfterSimpleLabels;
    private System.Windows.Forms.CheckBox checkCommentCharacters;
    private System.Windows.Forms.TextBox editCommentCharacters;
    private System.Windows.Forms.CheckBox checkExportToDataIncludeRes;
    private System.Windows.Forms.CheckBox checkExportToDataWrap;
    private System.Windows.Forms.TextBox editWrapByteCount;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox editPrefix;
    private System.Windows.Forms.CheckBox checkAddFilenamespace;
    private System.Windows.Forms.TextBox editFilenamespace;
    private System.Windows.Forms.CheckBox checkExportSparseMaps;
    private System.Windows.Forms.CheckBox checkWrapMapData;
    private System.Windows.Forms.CheckBox checkEmptyTile;
    private System.Windows.Forms.TextBox editEmptyTileIndex;
    private System.Windows.Forms.CheckBox checkExportTilesetColors;
    private System.Windows.Forms.CheckBox checkExportMapColors;
    private System.Windows.Forms.GroupBox groupAutoSave;
    private System.Windows.Forms.TextBox editExportFilename;
    private System.Windows.Forms.Label labelExportFilename;
    private System.Windows.Forms.Button btnBrowseExportDirectory;
    private System.Windows.Forms.TextBox editExportDirectory;
    private System.Windows.Forms.Label labelExportDirectory;
    private System.Windows.Forms.CheckBox checkSaveOnExport;
    private System.Windows.Forms.GroupBox groupCharset;
    private System.Windows.Forms.TextBox editCharsetExportFilename;
    private System.Windows.Forms.Label labelCharsetExportFilename;
    private System.Windows.Forms.Button btnBrowseCharsetExportDirectory;
    private System.Windows.Forms.TextBox editCharsetExportDirectory;
    private System.Windows.Forms.Label labelCharsetExportDirectory;
    private System.Windows.Forms.CheckBox checkExportCharset;
    private System.Windows.Forms.CheckBox checkAlwaysOverwrite;
  }
}
