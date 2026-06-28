namespace RetroDevStudio.Controls
{
  partial class ExportSpriteAsGameBinary
  {
    /// <summary> Required designer variable. </summary>
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
      this.grpAnimDefs = new System.Windows.Forms.GroupBox();
      this.lblAnimDir = new System.Windows.Forms.Label();
      this.editAnimDir = new System.Windows.Forms.TextBox();
      this.btnAnimDirBrowse = new System.Windows.Forms.Button();
      this.lblAnimFile = new System.Windows.Forms.Label();
      this.editAnimFile = new System.Windows.Forms.TextBox();
      this.checkAnimAsm = new System.Windows.Forms.CheckBox();
      this.lblAnimAsmDir = new System.Windows.Forms.Label();
      this.editAnimAsmDir = new System.Windows.Forms.TextBox();
      this.btnAnimAsmDirBrowse = new System.Windows.Forms.Button();
      this.lblAnimAsmFile = new System.Windows.Forms.Label();
      this.editAnimAsmFile = new System.Windows.Forms.TextBox();
      this.checkAnimPrefix = new System.Windows.Forms.CheckBox();
      this.editAnimPrefix = new System.Windows.Forms.TextBox();
      this.checkAnimCompress = new System.Windows.Forms.CheckBox();
      this.comboAnimCompressor = new System.Windows.Forms.ComboBox();
      this.lblAnimCompressDir = new System.Windows.Forms.Label();
      this.editAnimCompressDir = new System.Windows.Forms.TextBox();
      this.btnAnimCompressDirBrowse = new System.Windows.Forms.Button();
      this.lblAnimCompressOut = new System.Windows.Forms.Label();
      this.editAnimCompressFile = new System.Windows.Forms.TextBox();
      this.checkAnimOverride = new ThemedCheckBox();
      this.editAnimOverride = new System.Windows.Forms.TextBox();
      this.checkAnimMaxSize = new System.Windows.Forms.CheckBox();
      this.editAnimMaxSize = new System.Windows.Forms.TextBox();
      this.grpSpriteData = new System.Windows.Forms.GroupBox();
      this.lblSprDir = new System.Windows.Forms.Label();
      this.editSprDir = new System.Windows.Forms.TextBox();
      this.btnSprDirBrowse = new System.Windows.Forms.Button();
      this.lblSprFile = new System.Windows.Forms.Label();
      this.editSprFile = new System.Windows.Forms.TextBox();
      this.checkSprPrefix = new System.Windows.Forms.CheckBox();
      this.editSprPrefix = new System.Windows.Forms.TextBox();
      this.checkSprCompress = new System.Windows.Forms.CheckBox();
      this.comboSprCompressor = new System.Windows.Forms.ComboBox();
      this.lblSprCompressDir = new System.Windows.Forms.Label();
      this.editSprCompressDir = new System.Windows.Forms.TextBox();
      this.btnSprCompressDirBrowse = new System.Windows.Forms.Button();
      this.lblSprCompressOut = new System.Windows.Forms.Label();
      this.editSprCompressFile = new System.Windows.Forms.TextBox();
      this.checkSprOverride = new ThemedCheckBox();
      this.editSprOverride = new System.Windows.Forms.TextBox();
      this.checkSprMaxSize = new System.Windows.Forms.CheckBox();
      this.editSprMaxSize = new System.Windows.Forms.TextBox();
      this.btnCopySettings = new System.Windows.Forms.Button();
      this.btnPasteSettings = new System.Windows.Forms.Button();
      this.grpAnimDefs.SuspendLayout();
      this.grpSpriteData.SuspendLayout();
      this.SuspendLayout();
      //
      // grpAnimDefs
      //
      this.grpAnimDefs.Controls.Add( this.lblAnimDir );
      this.grpAnimDefs.Controls.Add( this.editAnimDir );
      this.grpAnimDefs.Controls.Add( this.btnAnimDirBrowse );
      this.grpAnimDefs.Controls.Add( this.lblAnimFile );
      this.grpAnimDefs.Controls.Add( this.editAnimFile );
      this.grpAnimDefs.Controls.Add( this.checkAnimAsm );
      this.grpAnimDefs.Controls.Add( this.lblAnimAsmDir );
      this.grpAnimDefs.Controls.Add( this.editAnimAsmDir );
      this.grpAnimDefs.Controls.Add( this.btnAnimAsmDirBrowse );
      this.grpAnimDefs.Controls.Add( this.lblAnimAsmFile );
      this.grpAnimDefs.Controls.Add( this.editAnimAsmFile );
      this.grpAnimDefs.Controls.Add( this.checkAnimPrefix );
      this.grpAnimDefs.Controls.Add( this.editAnimPrefix );
      this.grpAnimDefs.Controls.Add( this.checkAnimCompress );
      this.grpAnimDefs.Controls.Add( this.comboAnimCompressor );
      this.grpAnimDefs.Controls.Add( this.lblAnimCompressDir );
      this.grpAnimDefs.Controls.Add( this.editAnimCompressDir );
      this.grpAnimDefs.Controls.Add( this.btnAnimCompressDirBrowse );
      this.grpAnimDefs.Controls.Add( this.lblAnimCompressOut );
      this.grpAnimDefs.Controls.Add( this.editAnimCompressFile );
      this.grpAnimDefs.Controls.Add( this.checkAnimOverride );
      this.grpAnimDefs.Controls.Add( this.editAnimOverride );
      this.grpAnimDefs.Controls.Add( this.checkAnimMaxSize );
      this.grpAnimDefs.Controls.Add( this.editAnimMaxSize );
      this.grpAnimDefs.Location = new System.Drawing.Point( 4, 34 );
      this.grpAnimDefs.Name = "grpAnimDefs";
      this.grpAnimDefs.Size = new System.Drawing.Size( 284, 348 );
      this.grpAnimDefs.TabIndex = 2;
      this.grpAnimDefs.TabStop = false;
      this.grpAnimDefs.Text = "Animation definitions file";
      //
      // lblAnimDir
      //
      this.lblAnimDir.AutoSize = true;
      this.lblAnimDir.Location = new System.Drawing.Point( 8, 20 );
      this.lblAnimDir.Name = "lblAnimDir";
      this.lblAnimDir.Size = new System.Drawing.Size( 52, 13 );
      this.lblAnimDir.Text = "Directory:";
      //
      // editAnimDir
      //
      this.editAnimDir.Location = new System.Drawing.Point( 8, 38 );
      this.editAnimDir.Name = "editAnimDir";
      this.editAnimDir.Size = new System.Drawing.Size( 240, 20 );
      this.editAnimDir.TabIndex = 0;
      //
      // btnAnimDirBrowse
      //
      this.btnAnimDirBrowse.Location = new System.Drawing.Point( 250, 37 );
      this.btnAnimDirBrowse.Name = "btnAnimDirBrowse";
      this.btnAnimDirBrowse.Size = new System.Drawing.Size( 26, 22 );
      this.btnAnimDirBrowse.TabIndex = 1;
      this.btnAnimDirBrowse.Text = "...";
      this.btnAnimDirBrowse.UseVisualStyleBackColor = true;
      this.btnAnimDirBrowse.Click += new System.EventHandler( this.btnAnimDirBrowse_Click );
      //
      // lblAnimFile
      //
      this.lblAnimFile.AutoSize = true;
      this.lblAnimFile.Location = new System.Drawing.Point( 8, 64 );
      this.lblAnimFile.Name = "lblAnimFile";
      this.lblAnimFile.Size = new System.Drawing.Size( 52, 13 );
      this.lblAnimFile.Text = "Filename:";
      //
      // editAnimFile
      //
      this.editAnimFile.Location = new System.Drawing.Point( 8, 80 );
      this.editAnimFile.Name = "editAnimFile";
      this.editAnimFile.Size = new System.Drawing.Size( 268, 20 );
      this.editAnimFile.TabIndex = 2;
      //
      // checkAnimAsm
      //
      this.checkAnimAsm.AutoSize = true;
      this.checkAnimAsm.Checked = true;
      this.checkAnimAsm.CheckState = System.Windows.Forms.CheckState.Checked;
      this.checkAnimAsm.Location = new System.Drawing.Point( 8, 108 );
      this.checkAnimAsm.Name = "checkAnimAsm";
      this.checkAnimAsm.Size = new System.Drawing.Size( 150, 17 );
      this.checkAnimAsm.TabIndex = 3;
      this.checkAnimAsm.Text = "Generate .asm consts file:";
      this.checkAnimAsm.UseVisualStyleBackColor = true;
      this.checkAnimAsm.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // lblAnimAsmDir
      //
      this.lblAnimAsmDir.AutoSize = true;
      this.lblAnimAsmDir.Location = new System.Drawing.Point( 24, 134 );
      this.lblAnimAsmDir.Name = "lblAnimAsmDir";
      this.lblAnimAsmDir.Size = new System.Drawing.Size( 28, 13 );
      this.lblAnimAsmDir.Text = "Dir:";
      //
      // editAnimAsmDir
      //
      this.editAnimAsmDir.Location = new System.Drawing.Point( 72, 131 );
      this.editAnimAsmDir.Name = "editAnimAsmDir";
      this.editAnimAsmDir.Size = new System.Drawing.Size( 176, 20 );
      this.editAnimAsmDir.TabIndex = 4;
      //
      // btnAnimAsmDirBrowse
      //
      this.btnAnimAsmDirBrowse.Location = new System.Drawing.Point( 250, 130 );
      this.btnAnimAsmDirBrowse.Name = "btnAnimAsmDirBrowse";
      this.btnAnimAsmDirBrowse.Size = new System.Drawing.Size( 26, 22 );
      this.btnAnimAsmDirBrowse.TabIndex = 5;
      this.btnAnimAsmDirBrowse.Text = "...";
      this.btnAnimAsmDirBrowse.UseVisualStyleBackColor = true;
      this.btnAnimAsmDirBrowse.Click += new System.EventHandler( this.btnAnimAsmDirBrowse_Click );
      //
      // lblAnimAsmFile
      //
      this.lblAnimAsmFile.AutoSize = true;
      this.lblAnimAsmFile.Location = new System.Drawing.Point( 24, 160 );
      this.lblAnimAsmFile.Name = "lblAnimAsmFile";
      this.lblAnimAsmFile.Size = new System.Drawing.Size( 30, 13 );
      this.lblAnimAsmFile.Text = "File:";
      //
      // editAnimAsmFile
      //
      this.editAnimAsmFile.Location = new System.Drawing.Point( 72, 157 );
      this.editAnimAsmFile.Name = "editAnimAsmFile";
      this.editAnimAsmFile.Size = new System.Drawing.Size( 204, 20 );
      this.editAnimAsmFile.TabIndex = 6;
      this.editAnimAsmFile.Text = "sprite_anims.asm";
      //
      // checkAnimPrefix
      //
      this.checkAnimPrefix.AutoSize = true;
      this.checkAnimPrefix.Location = new System.Drawing.Point( 8, 187 );
      this.checkAnimPrefix.Name = "checkAnimPrefix";
      this.checkAnimPrefix.Size = new System.Drawing.Size( 150, 17 );
      this.checkAnimPrefix.TabIndex = 7;
      this.checkAnimPrefix.Text = "Prefix load address (hex):";
      this.checkAnimPrefix.UseVisualStyleBackColor = true;
      this.checkAnimPrefix.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editAnimPrefix
      //
      this.editAnimPrefix.Enabled = false;
      this.editAnimPrefix.Location = new System.Drawing.Point( 196, 185 );
      this.editAnimPrefix.Name = "editAnimPrefix";
      this.editAnimPrefix.Size = new System.Drawing.Size( 60, 20 );
      this.editAnimPrefix.TabIndex = 8;
      //
      // checkAnimCompress
      //
      this.checkAnimCompress.AutoSize = true;
      this.checkAnimCompress.Location = new System.Drawing.Point( 8, 213 );
      this.checkAnimCompress.Name = "checkAnimCompress";
      this.checkAnimCompress.Size = new System.Drawing.Size( 95, 17 );
      this.checkAnimCompress.TabIndex = 9;
      this.checkAnimCompress.Text = "Compress with:";
      this.checkAnimCompress.UseVisualStyleBackColor = true;
      this.checkAnimCompress.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // comboAnimCompressor
      //
      this.comboAnimCompressor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboAnimCompressor.Enabled = false;
      this.comboAnimCompressor.FormattingEnabled = true;
      this.comboAnimCompressor.Items.AddRange( new object[] {
      "ZX0"} );
      this.comboAnimCompressor.Location = new System.Drawing.Point( 120, 211 );
      this.comboAnimCompressor.Name = "comboAnimCompressor";
      this.comboAnimCompressor.Size = new System.Drawing.Size( 156, 21 );
      this.comboAnimCompressor.TabIndex = 10;
      //
      // lblAnimCompressDir
      //
      this.lblAnimCompressDir.AutoSize = true;
      this.lblAnimCompressDir.Location = new System.Drawing.Point( 24, 239 );
      this.lblAnimCompressDir.Name = "lblAnimCompressDir";
      this.lblAnimCompressDir.Size = new System.Drawing.Size( 28, 13 );
      this.lblAnimCompressDir.Text = "Dir:";
      //
      // editAnimCompressDir
      //
      this.editAnimCompressDir.Enabled = false;
      this.editAnimCompressDir.Location = new System.Drawing.Point( 72, 236 );
      this.editAnimCompressDir.Name = "editAnimCompressDir";
      this.editAnimCompressDir.Size = new System.Drawing.Size( 176, 20 );
      this.editAnimCompressDir.TabIndex = 11;
      //
      // btnAnimCompressDirBrowse
      //
      this.btnAnimCompressDirBrowse.Enabled = false;
      this.btnAnimCompressDirBrowse.Location = new System.Drawing.Point( 250, 235 );
      this.btnAnimCompressDirBrowse.Name = "btnAnimCompressDirBrowse";
      this.btnAnimCompressDirBrowse.Size = new System.Drawing.Size( 26, 22 );
      this.btnAnimCompressDirBrowse.TabIndex = 12;
      this.btnAnimCompressDirBrowse.Text = "...";
      this.btnAnimCompressDirBrowse.UseVisualStyleBackColor = true;
      this.btnAnimCompressDirBrowse.Click += new System.EventHandler( this.btnAnimCompressDirBrowse_Click );
      //
      // lblAnimCompressOut
      //
      this.lblAnimCompressOut.AutoSize = true;
      this.lblAnimCompressOut.Location = new System.Drawing.Point( 24, 265 );
      this.lblAnimCompressOut.Name = "lblAnimCompressOut";
      this.lblAnimCompressOut.Size = new System.Drawing.Size( 45, 13 );
      this.lblAnimCompressOut.Text = "Output:";
      //
      // editAnimCompressFile
      //
      this.editAnimCompressFile.Enabled = false;
      this.editAnimCompressFile.Location = new System.Drawing.Point( 72, 262 );
      this.editAnimCompressFile.Name = "editAnimCompressFile";
      this.editAnimCompressFile.Size = new System.Drawing.Size( 204, 20 );
      this.editAnimCompressFile.TabIndex = 13;
      //
      // checkAnimOverride
      //
      this.checkAnimOverride.AutoSize = false;
      this.checkAnimOverride.Enabled = false;
      this.checkAnimOverride.Location = new System.Drawing.Point( 24, 289 );
      this.checkAnimOverride.Name = "checkAnimOverride";
      this.checkAnimOverride.Size = new System.Drawing.Size( 180, 18 );
      this.checkAnimOverride.TabIndex = 14;
      this.checkAnimOverride.Text = "Override load address (hex):";
      this.checkAnimOverride.UseVisualStyleBackColor = true;
      this.checkAnimOverride.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editAnimOverride
      //
      this.editAnimOverride.Enabled = false;
      this.editAnimOverride.Location = new System.Drawing.Point( 210, 287 );
      this.editAnimOverride.Name = "editAnimOverride";
      this.editAnimOverride.Size = new System.Drawing.Size( 60, 20 );
      this.editAnimOverride.TabIndex = 15;
      //
      // checkAnimMaxSize
      //
      this.checkAnimMaxSize.AutoSize = true;
      this.checkAnimMaxSize.Location = new System.Drawing.Point( 8, 315 );
      this.checkAnimMaxSize.Name = "checkAnimMaxSize";
      this.checkAnimMaxSize.Size = new System.Drawing.Size( 100, 17 );
      this.checkAnimMaxSize.TabIndex = 16;
      this.checkAnimMaxSize.Text = "Max size (bytes):";
      this.checkAnimMaxSize.UseVisualStyleBackColor = true;
      this.checkAnimMaxSize.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editAnimMaxSize
      //
      this.editAnimMaxSize.Enabled = false;
      this.editAnimMaxSize.Location = new System.Drawing.Point( 130, 313 );
      this.editAnimMaxSize.Name = "editAnimMaxSize";
      this.editAnimMaxSize.Size = new System.Drawing.Size( 60, 20 );
      this.editAnimMaxSize.TabIndex = 17;
      this.editAnimMaxSize.Text = "0";
      //
      // grpSpriteData
      //
      this.grpSpriteData.Controls.Add( this.lblSprDir );
      this.grpSpriteData.Controls.Add( this.editSprDir );
      this.grpSpriteData.Controls.Add( this.btnSprDirBrowse );
      this.grpSpriteData.Controls.Add( this.lblSprFile );
      this.grpSpriteData.Controls.Add( this.editSprFile );
      this.grpSpriteData.Controls.Add( this.checkSprPrefix );
      this.grpSpriteData.Controls.Add( this.editSprPrefix );
      this.grpSpriteData.Controls.Add( this.checkSprCompress );
      this.grpSpriteData.Controls.Add( this.comboSprCompressor );
      this.grpSpriteData.Controls.Add( this.lblSprCompressDir );
      this.grpSpriteData.Controls.Add( this.editSprCompressDir );
      this.grpSpriteData.Controls.Add( this.btnSprCompressDirBrowse );
      this.grpSpriteData.Controls.Add( this.lblSprCompressOut );
      this.grpSpriteData.Controls.Add( this.editSprCompressFile );
      this.grpSpriteData.Controls.Add( this.checkSprOverride );
      this.grpSpriteData.Controls.Add( this.editSprOverride );
      this.grpSpriteData.Controls.Add( this.checkSprMaxSize );
      this.grpSpriteData.Controls.Add( this.editSprMaxSize );
      this.grpSpriteData.Location = new System.Drawing.Point( 4, 386 );
      this.grpSpriteData.Name = "grpSpriteData";
      this.grpSpriteData.Size = new System.Drawing.Size( 284, 274 );
      this.grpSpriteData.TabIndex = 3;
      this.grpSpriteData.TabStop = false;
      this.grpSpriteData.Text = "Sprite data file (raw 64-byte blocks)";
      //
      // lblSprDir
      //
      this.lblSprDir.AutoSize = true;
      this.lblSprDir.Location = new System.Drawing.Point( 8, 20 );
      this.lblSprDir.Name = "lblSprDir";
      this.lblSprDir.Size = new System.Drawing.Size( 52, 13 );
      this.lblSprDir.Text = "Directory:";
      //
      // editSprDir
      //
      this.editSprDir.Location = new System.Drawing.Point( 8, 38 );
      this.editSprDir.Name = "editSprDir";
      this.editSprDir.Size = new System.Drawing.Size( 240, 20 );
      this.editSprDir.TabIndex = 0;
      //
      // btnSprDirBrowse
      //
      this.btnSprDirBrowse.Location = new System.Drawing.Point( 250, 37 );
      this.btnSprDirBrowse.Name = "btnSprDirBrowse";
      this.btnSprDirBrowse.Size = new System.Drawing.Size( 26, 22 );
      this.btnSprDirBrowse.TabIndex = 1;
      this.btnSprDirBrowse.Text = "...";
      this.btnSprDirBrowse.UseVisualStyleBackColor = true;
      this.btnSprDirBrowse.Click += new System.EventHandler( this.btnSprDirBrowse_Click );
      //
      // lblSprFile
      //
      this.lblSprFile.AutoSize = true;
      this.lblSprFile.Location = new System.Drawing.Point( 8, 64 );
      this.lblSprFile.Name = "lblSprFile";
      this.lblSprFile.Size = new System.Drawing.Size( 52, 13 );
      this.lblSprFile.Text = "Filename:";
      //
      // editSprFile
      //
      this.editSprFile.Location = new System.Drawing.Point( 8, 80 );
      this.editSprFile.Name = "editSprFile";
      this.editSprFile.Size = new System.Drawing.Size( 268, 20 );
      this.editSprFile.TabIndex = 2;
      //
      // checkSprPrefix
      //
      this.checkSprPrefix.AutoSize = true;
      this.checkSprPrefix.Location = new System.Drawing.Point( 8, 110 );
      this.checkSprPrefix.Name = "checkSprPrefix";
      this.checkSprPrefix.Size = new System.Drawing.Size( 150, 17 );
      this.checkSprPrefix.TabIndex = 3;
      this.checkSprPrefix.Text = "Prefix load address (hex):";
      this.checkSprPrefix.UseVisualStyleBackColor = true;
      this.checkSprPrefix.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editSprPrefix
      //
      this.editSprPrefix.Enabled = false;
      this.editSprPrefix.Location = new System.Drawing.Point( 196, 108 );
      this.editSprPrefix.Name = "editSprPrefix";
      this.editSprPrefix.Size = new System.Drawing.Size( 60, 20 );
      this.editSprPrefix.TabIndex = 4;
      //
      // checkSprCompress
      //
      this.checkSprCompress.AutoSize = true;
      this.checkSprCompress.Location = new System.Drawing.Point( 8, 138 );
      this.checkSprCompress.Name = "checkSprCompress";
      this.checkSprCompress.Size = new System.Drawing.Size( 95, 17 );
      this.checkSprCompress.TabIndex = 5;
      this.checkSprCompress.Text = "Compress with:";
      this.checkSprCompress.UseVisualStyleBackColor = true;
      this.checkSprCompress.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // comboSprCompressor
      //
      this.comboSprCompressor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboSprCompressor.Enabled = false;
      this.comboSprCompressor.FormattingEnabled = true;
      this.comboSprCompressor.Items.AddRange( new object[] {
      "ZX0"} );
      this.comboSprCompressor.Location = new System.Drawing.Point( 120, 136 );
      this.comboSprCompressor.Name = "comboSprCompressor";
      this.comboSprCompressor.Size = new System.Drawing.Size( 156, 21 );
      this.comboSprCompressor.TabIndex = 6;
      //
      // lblSprCompressDir
      //
      this.lblSprCompressDir.AutoSize = true;
      this.lblSprCompressDir.Location = new System.Drawing.Point( 24, 164 );
      this.lblSprCompressDir.Name = "lblSprCompressDir";
      this.lblSprCompressDir.Size = new System.Drawing.Size( 28, 13 );
      this.lblSprCompressDir.Text = "Dir:";
      //
      // editSprCompressDir
      //
      this.editSprCompressDir.Enabled = false;
      this.editSprCompressDir.Location = new System.Drawing.Point( 72, 161 );
      this.editSprCompressDir.Name = "editSprCompressDir";
      this.editSprCompressDir.Size = new System.Drawing.Size( 176, 20 );
      this.editSprCompressDir.TabIndex = 7;
      //
      // btnSprCompressDirBrowse
      //
      this.btnSprCompressDirBrowse.Enabled = false;
      this.btnSprCompressDirBrowse.Location = new System.Drawing.Point( 250, 160 );
      this.btnSprCompressDirBrowse.Name = "btnSprCompressDirBrowse";
      this.btnSprCompressDirBrowse.Size = new System.Drawing.Size( 26, 22 );
      this.btnSprCompressDirBrowse.TabIndex = 8;
      this.btnSprCompressDirBrowse.Text = "...";
      this.btnSprCompressDirBrowse.UseVisualStyleBackColor = true;
      this.btnSprCompressDirBrowse.Click += new System.EventHandler( this.btnSprCompressDirBrowse_Click );
      //
      // lblSprCompressOut
      //
      this.lblSprCompressOut.AutoSize = true;
      this.lblSprCompressOut.Location = new System.Drawing.Point( 24, 190 );
      this.lblSprCompressOut.Name = "lblSprCompressOut";
      this.lblSprCompressOut.Size = new System.Drawing.Size( 45, 13 );
      this.lblSprCompressOut.Text = "Output:";
      //
      // editSprCompressFile
      //
      this.editSprCompressFile.Enabled = false;
      this.editSprCompressFile.Location = new System.Drawing.Point( 72, 187 );
      this.editSprCompressFile.Name = "editSprCompressFile";
      this.editSprCompressFile.Size = new System.Drawing.Size( 204, 20 );
      this.editSprCompressFile.TabIndex = 9;
      //
      // checkSprOverride
      //
      this.checkSprOverride.AutoSize = false;
      this.checkSprOverride.Enabled = false;
      this.checkSprOverride.Location = new System.Drawing.Point( 24, 214 );
      this.checkSprOverride.Name = "checkSprOverride";
      this.checkSprOverride.Size = new System.Drawing.Size( 180, 18 );
      this.checkSprOverride.TabIndex = 10;
      this.checkSprOverride.Text = "Override load address (hex):";
      this.checkSprOverride.UseVisualStyleBackColor = true;
      this.checkSprOverride.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editSprOverride
      //
      this.editSprOverride.Enabled = false;
      this.editSprOverride.Location = new System.Drawing.Point( 210, 212 );
      this.editSprOverride.Name = "editSprOverride";
      this.editSprOverride.Size = new System.Drawing.Size( 60, 20 );
      this.editSprOverride.TabIndex = 11;
      //
      // checkSprMaxSize
      //
      this.checkSprMaxSize.AutoSize = true;
      this.checkSprMaxSize.Location = new System.Drawing.Point( 8, 240 );
      this.checkSprMaxSize.Name = "checkSprMaxSize";
      this.checkSprMaxSize.Size = new System.Drawing.Size( 100, 17 );
      this.checkSprMaxSize.TabIndex = 12;
      this.checkSprMaxSize.Text = "Max size (bytes):";
      this.checkSprMaxSize.UseVisualStyleBackColor = true;
      this.checkSprMaxSize.CheckedChanged += new System.EventHandler( this.UpdateControlStates );
      //
      // editSprMaxSize
      //
      this.editSprMaxSize.Enabled = false;
      this.editSprMaxSize.Location = new System.Drawing.Point( 130, 238 );
      this.editSprMaxSize.Name = "editSprMaxSize";
      this.editSprMaxSize.Size = new System.Drawing.Size( 60, 20 );
      this.editSprMaxSize.TabIndex = 13;
      this.editSprMaxSize.Text = "0";
      //
      // btnCopySettings
      //
      this.btnCopySettings.Location = new System.Drawing.Point( 8, 6 );
      this.btnCopySettings.Name = "btnCopySettings";
      this.btnCopySettings.Size = new System.Drawing.Size( 120, 24 );
      this.btnCopySettings.TabIndex = 0;
      this.btnCopySettings.Text = "Copy settings";
      this.btnCopySettings.UseVisualStyleBackColor = true;
      this.btnCopySettings.Click += new System.EventHandler( this.btnCopySettings_Click );
      //
      // btnPasteSettings
      //
      this.btnPasteSettings.Location = new System.Drawing.Point( 132, 6 );
      this.btnPasteSettings.Name = "btnPasteSettings";
      this.btnPasteSettings.Size = new System.Drawing.Size( 120, 24 );
      this.btnPasteSettings.TabIndex = 1;
      this.btnPasteSettings.Text = "Paste settings";
      this.btnPasteSettings.UseVisualStyleBackColor = true;
      this.btnPasteSettings.Click += new System.EventHandler( this.btnPasteSettings_Click );
      //
      // ExportSpriteAsGameBinary
      //
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.AutoScroll = true;
      this.Controls.Add( this.btnCopySettings );
      this.Controls.Add( this.btnPasteSettings );
      this.Controls.Add( this.grpAnimDefs );
      this.Controls.Add( this.grpSpriteData );
      this.Name = "ExportSpriteAsGameBinary";
      this.Size = new System.Drawing.Size( 296, 668 );
      this.grpAnimDefs.ResumeLayout( false );
      this.grpAnimDefs.PerformLayout();
      this.grpSpriteData.ResumeLayout( false );
      this.grpSpriteData.PerformLayout();
      this.ResumeLayout( false );
    }

    #endregion

    private System.Windows.Forms.Button btnCopySettings;
    private System.Windows.Forms.Button btnPasteSettings;
    private System.Windows.Forms.GroupBox grpAnimDefs;
    private System.Windows.Forms.Label lblAnimDir;
    private System.Windows.Forms.TextBox editAnimDir;
    private System.Windows.Forms.Button btnAnimDirBrowse;
    private System.Windows.Forms.Label lblAnimFile;
    private System.Windows.Forms.TextBox editAnimFile;
    private System.Windows.Forms.CheckBox checkAnimAsm;
    private System.Windows.Forms.Label lblAnimAsmDir;
    private System.Windows.Forms.TextBox editAnimAsmDir;
    private System.Windows.Forms.Button btnAnimAsmDirBrowse;
    private System.Windows.Forms.Label lblAnimAsmFile;
    private System.Windows.Forms.TextBox editAnimAsmFile;
    private System.Windows.Forms.CheckBox checkAnimPrefix;
    private System.Windows.Forms.TextBox editAnimPrefix;
    private System.Windows.Forms.CheckBox checkAnimCompress;
    private System.Windows.Forms.ComboBox comboAnimCompressor;
    private System.Windows.Forms.Label lblAnimCompressDir;
    private System.Windows.Forms.TextBox editAnimCompressDir;
    private System.Windows.Forms.Button btnAnimCompressDirBrowse;
    private System.Windows.Forms.Label lblAnimCompressOut;
    private System.Windows.Forms.TextBox editAnimCompressFile;
    private ThemedCheckBox checkAnimOverride;
    private System.Windows.Forms.TextBox editAnimOverride;
    private System.Windows.Forms.CheckBox checkAnimMaxSize;
    private System.Windows.Forms.TextBox editAnimMaxSize;
    private System.Windows.Forms.GroupBox grpSpriteData;
    private System.Windows.Forms.Label lblSprDir;
    private System.Windows.Forms.TextBox editSprDir;
    private System.Windows.Forms.Button btnSprDirBrowse;
    private System.Windows.Forms.Label lblSprFile;
    private System.Windows.Forms.TextBox editSprFile;
    private System.Windows.Forms.CheckBox checkSprPrefix;
    private System.Windows.Forms.TextBox editSprPrefix;
    private System.Windows.Forms.CheckBox checkSprCompress;
    private System.Windows.Forms.ComboBox comboSprCompressor;
    private System.Windows.Forms.Label lblSprCompressDir;
    private System.Windows.Forms.TextBox editSprCompressDir;
    private System.Windows.Forms.Button btnSprCompressDirBrowse;
    private System.Windows.Forms.Label lblSprCompressOut;
    private System.Windows.Forms.TextBox editSprCompressFile;
    private ThemedCheckBox checkSprOverride;
    private System.Windows.Forms.TextBox editSprOverride;
    private System.Windows.Forms.CheckBox checkSprMaxSize;
    private System.Windows.Forms.TextBox editSprMaxSize;
  }
}
