
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
      this.SuspendLayout();
      // 
      // checkExportHex
      // 
      this.checkExportHex.AutoSize = true;
      this.checkExportHex.Checked = true;
      this.checkExportHex.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkExportHex.Location = new System.Drawing.Point(3, 49);
      this.checkExportHex.Name = "checkExportHex";
      this.checkExportHex.Size = new System.Drawing.Size(141, 17);
      this.checkExportHex.TabIndex = 4;
      this.checkExportHex.Text = "Export with Hex notation";
      this.checkExportHex.UseVisualStyleBackColor = true;
      // 
      // checkExportSparseMaps
      // 
      this.checkExportSparseMaps.AutoSize = true;
      this.checkExportSparseMaps.Location = new System.Drawing.Point(3, 72);
      this.checkExportSparseMaps.Name = "checkExportSparseMaps";
      this.checkExportSparseMaps.Size = new System.Drawing.Size(141, 17);
      this.checkExportSparseMaps.TabIndex = 4;
      this.checkExportSparseMaps.Text = "Export sparse maps";
      this.checkExportSparseMaps.UseVisualStyleBackColor = true;
      this.checkExportSparseMaps.CheckedChanged += new System.EventHandler(this.checkExportSparseMaps_CheckedChanged);
      // 
      // checkWrapMapData
      // 
      this.checkWrapMapData.AutoSize = true;
      this.checkWrapMapData.Location = new System.Drawing.Point(3, 95);
      this.checkWrapMapData.Name = "checkWrapMapData";
      this.checkWrapMapData.Size = new System.Drawing.Size(141, 17);
      this.checkWrapMapData.Checked = true;
      this.checkWrapMapData.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkWrapMapData.TabIndex = 4;
      this.checkWrapMapData.Text = "Wrap map data";
      this.checkWrapMapData.UseVisualStyleBackColor = true;
      this.checkWrapMapData.CheckedChanged += new System.EventHandler(this.checkWrapMapData_CheckedChanged);
      // 
      // checkVariableNameLabelPrefix
      // 
      this.checkVariableNameLabelPrefix.AutoSize = true;
      this.checkVariableNameLabelPrefix.Location = new System.Drawing.Point(3, 118);
      this.checkVariableNameLabelPrefix.Name = "checkVariableNameLabelPrefix";
      this.checkVariableNameLabelPrefix.Size = new System.Drawing.Size(168, 17);
      this.checkVariableNameLabelPrefix.TabIndex = 5;
      this.checkVariableNameLabelPrefix.Text = "Variable name label Prefix";
      this.checkVariableNameLabelPrefix.UseVisualStyleBackColor = true;
      this.checkVariableNameLabelPrefix.CheckedChanged += new System.EventHandler(this.checkVariableNameLabelPrefix_CheckedChanged);
      // 
      // editVariableNameLabelPrefix
      // 
      this.editVariableNameLabelPrefix.Location = new System.Drawing.Point(200, 116);
      this.editVariableNameLabelPrefix.Name = "editVariableNameLabelPrefix";
      this.editVariableNameLabelPrefix.Size = new System.Drawing.Size(64, 20);
      this.editVariableNameLabelPrefix.TabIndex = 6;
      // 
      // checkIncludeSemicolonAfterSimpleLabels
      // 
      this.checkIncludeSemicolonAfterSimpleLabels.AutoSize = true;
      this.checkIncludeSemicolonAfterSimpleLabels.Location = new System.Drawing.Point(3, 141);
      this.checkIncludeSemicolonAfterSimpleLabels.Name = "checkIncludeSemicolonAfterSimpleLabels";
      this.checkIncludeSemicolonAfterSimpleLabels.Size = new System.Drawing.Size(210, 17);
      this.checkIncludeSemicolonAfterSimpleLabels.TabIndex = 7;
      this.checkIncludeSemicolonAfterSimpleLabels.Text = "Include colon after simple labels";
      this.checkIncludeSemicolonAfterSimpleLabels.UseVisualStyleBackColor = true;
      // 
      // checkCommentCharacters
      // 
      this.checkCommentCharacters.AutoSize = true;
      this.checkCommentCharacters.Checked = true;
      this.checkCommentCharacters.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkCommentCharacters.Location = new System.Drawing.Point(3, 166);
      this.checkCommentCharacters.Name = "checkCommentCharacters";
      this.checkCommentCharacters.Size = new System.Drawing.Size(122, 17);
      this.checkCommentCharacters.TabIndex = 8;
      this.checkCommentCharacters.Text = "Comment characters";
      this.checkCommentCharacters.UseVisualStyleBackColor = true;
      // 
      // editCommentCharacters
      // 
      this.editCommentCharacters.Location = new System.Drawing.Point(200, 163);
      this.editCommentCharacters.Name = "editCommentCharacters";
      this.editCommentCharacters.Size = new System.Drawing.Size(64, 20);
      this.editCommentCharacters.TabIndex = 9;
      this.editCommentCharacters.Text = ";";
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
      this.checkExportToDataWrap.Location = new System.Drawing.Point(3, 26);
      this.checkExportToDataWrap.Name = "checkExportToDataWrap";
      this.checkExportToDataWrap.Size = new System.Drawing.Size(64, 17);
      this.checkExportToDataWrap.TabIndex = 2;
      this.checkExportToDataWrap.Text = "Wrap at";
      this.checkExportToDataWrap.UseVisualStyleBackColor = true;
      this.checkExportToDataWrap.CheckedChanged += new System.EventHandler(this.checkExportToDataWrap_CheckedChanged);
      // 
      // editWrapByteCount
      // 
      this.editWrapByteCount.Location = new System.Drawing.Point(99, 24);
      this.editWrapByteCount.Name = "editWrapByteCount";
      this.editWrapByteCount.Size = new System.Drawing.Size(64, 20);
      this.editWrapByteCount.TabIndex = 3;
      this.editWrapByteCount.Text = "8";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(169, 26);
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
      // 
      // checkAddFilenamespace
      // 
      this.checkAddFilenamespace.AutoSize = true;
      this.checkAddFilenamespace.Location = new System.Drawing.Point(3, 366);
      this.checkAddFilenamespace.Name = "checkAddFilenamespace";
      this.checkAddFilenamespace.Size = new System.Drawing.Size(122, 17);
      this.checkAddFilenamespace.TabIndex = 15;
      this.checkAddFilenamespace.Text = "Add filenamespace";
      this.checkAddFilenamespace.UseVisualStyleBackColor = true;
      this.checkAddFilenamespace.CheckedChanged += new System.EventHandler(this.checkAddFilenamespace_CheckedChanged);
      // 
      // editFilenamespace
      // 
      this.editFilenamespace.Location = new System.Drawing.Point(200, 363);
      this.editFilenamespace.Name = "editFilenamespace";
      this.editFilenamespace.Size = new System.Drawing.Size(214, 20);
      this.editFilenamespace.TabIndex = 16;
      this.editFilenamespace.Enabled = false;
      this.editFilenamespace.TextChanged += new System.EventHandler(this.editFilenamespace_TextChanged);
      // 
      // ExportMapAsAssembly
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
      this.Size = new System.Drawing.Size(427, 360);
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
  }
}
