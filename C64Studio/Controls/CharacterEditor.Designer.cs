namespace RetroDevStudio.Controls
{
  partial class CharacterEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CharacterEditor));
            GR.Image.FastImage fastImage1 = new GR.Image.FastImage();
            this.comboCategories = new System.Windows.Forms.ComboBox();
            this.checkShowPlaygroundGrid = new System.Windows.Forms.CheckBox();
            this.trackGridOpacity = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.labelCharNo = new System.Windows.Forms.Label();
            this.checkShowGrid = new System.Windows.Forms.CheckBox();
            this.checkPasteMultiColor = new System.Windows.Forms.CheckBox();
            this.editMoveTargetIndex = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnShiftLeft = new DecentForms.Button();
            this.btnShiftRight = new DecentForms.Button();
            this.btnShiftUp = new DecentForms.Button();
            this.btnShiftDown = new DecentForms.Button();
            this.btnMirrorX = new DecentForms.Button();
            this.btnMirrorY = new DecentForms.Button();
            this.btnInvert = new DecentForms.Button();
            this.btnRotateLeft = new DecentForms.Button();
            this.button3 = new DecentForms.Button();
            this.ButtonCanvas1x1 = new DecentForms.RadioButton();
            this.btnZoomIn = new DecentForms.Button();
            this.btnZoomOut = new DecentForms.Button();
            this.btnCopy = new DecentForms.Button();
            this.btnPasteFromClipboard = new DecentForms.Button();
            this.btnPaste = new DecentForms.Button();
            this.btnHighlightDuplicates = new DecentForms.Button();
            this.btnCharMoveDown = new DecentForms.Button();
            this.btnCharMoveRight = new DecentForms.Button();
            this.btnCharMoveUp = new DecentForms.Button();
            this.btnCharMoveLeft = new DecentForms.Button();
            this.tabCharacterEditor = new System.Windows.Forms.TabControl();
            this.tabEditor = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.panelCharacters = new GR.Forms.ImageListbox();
            this.picturePlayground = new GR.Forms.FastPictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnClearPlayground = new DecentForms.Button();
            this.btnCropPlayground = new DecentForms.Button();
            this.labelSwatchSize = new System.Windows.Forms.Label();
            this.editSwatchSize = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.ButtonCanvas2x2 = new DecentForms.RadioButton();
            this.ButtonCanvas2x3 = new DecentForms.RadioButton();
            this.ButtonCanvas4x4 = new DecentForms.RadioButton();
            this.labelZoom = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboCharactersPerRow = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnMoveSelectionToTarget = new DecentForms.Button();
            this.btnCreateTile = new DecentForms.Button();
            this.btnRestoreDefault = new DecentForms.Button();
            this.btnRemoveDuplicates = new DecentForms.Button();
            this.btnClearChars = new DecentForms.Button();
            this.panelColorChooser = new System.Windows.Forms.Panel();
            this.groupRightClick = new System.Windows.Forms.GroupBox();
            this.radioRightClickDefault = new DecentForms.RadioButton();
            this.radioRightClickBackground = new DecentForms.RadioButton();
            this.radioRightClickMulticolor1 = new DecentForms.RadioButton();
            this.radioRightClickMulticolor2 = new DecentForms.RadioButton();
            this.radioRightClickCharColor = new DecentForms.RadioButton();
            this.groupMoveChar = new System.Windows.Forms.GroupBox();
            this.panelColorSettings = new System.Windows.Forms.Panel();
            this.canvasEditor = new RetroDevStudio.Controls.CustomDrawControl();
            this.comboCharsetMode = new System.Windows.Forms.ComboBox();
            this.labelCharsetMode = new System.Windows.Forms.Label();
            this.flowPlaygroundScale = new System.Windows.Forms.FlowLayoutPanel();
            this.radioPlaygroundScale1x = new DecentForms.RadioButton();
            this.radioPlaygroundScale2x = new DecentForms.RadioButton();
            this.radioPlaygroundScale4x = new DecentForms.RadioButton();
            this.radioPlaygroundScale8x = new DecentForms.RadioButton();
            this.labelCharUsageCount = new System.Windows.Forms.Label();
            this.labelCharMapUsageCount = new System.Windows.Forms.Label();
            this.tabCategories = new System.Windows.Forms.TabPage();
            this.btnMoveCategoryDown = new DecentForms.Button();
            this.btnMoveCategoryUp = new DecentForms.Button();
            this.groupAllCategories = new System.Windows.Forms.GroupBox();
            this.groupCategorySpecific = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.editCollapseIndex = new System.Windows.Forms.TextBox();
            this.btnCollapseCategory = new DecentForms.Button();
            this.btnReseatCategory = new DecentForms.Button();
            this.btnDelete = new DecentForms.Button();
            this.btnAddCategory = new DecentForms.Button();
            this.listCategories = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.editCategoryName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.btnSortCategories = new DecentForms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackGridOpacity)).BeginInit();
            this.tabCharacterEditor.SuspendLayout();
            this.tabEditor.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picturePlayground)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupRightClick.SuspendLayout();
            this.groupMoveChar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.canvasEditor)).BeginInit();
            this.flowPlaygroundScale.SuspendLayout();
            this.tabCategories.SuspendLayout();
            this.groupCategorySpecific.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboCategories
            // 
            this.comboCategories.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCategories.FormattingEnabled = true;
            this.comboCategories.Location = new System.Drawing.Point(457, 245);
            this.comboCategories.Name = "comboCategories";
            this.comboCategories.Size = new System.Drawing.Size(157, 21);
            this.comboCategories.TabIndex = 11;
            this.comboCategories.SelectedIndexChanged += new System.EventHandler(this.comboCategories_SelectedIndexChanged);
            // 
            // checkShowPlaygroundGrid
            // 
            this.checkShowPlaygroundGrid.AutoSize = true;
            this.checkShowPlaygroundGrid.Location = new System.Drawing.Point(9, 50);
            this.checkShowPlaygroundGrid.Name = "checkShowPlaygroundGrid";
            this.checkShowPlaygroundGrid.Size = new System.Drawing.Size(75, 17);
            this.checkShowPlaygroundGrid.TabIndex = 2;
            this.checkShowPlaygroundGrid.Text = "Show Grid";
            this.checkShowPlaygroundGrid.UseVisualStyleBackColor = true;
            this.checkShowPlaygroundGrid.CheckedChanged += new System.EventHandler(this.checkShowPlaygroundGrid_CheckedChanged);
            // 
            // trackGridOpacity
            // 
            this.trackGridOpacity.AutoSize = false;
            this.trackGridOpacity.Location = new System.Drawing.Point(81, 50);
            this.trackGridOpacity.Maximum = 255;
            this.trackGridOpacity.Name = "trackGridOpacity";
            this.trackGridOpacity.Size = new System.Drawing.Size(100, 24);
            this.trackGridOpacity.TabIndex = 3;
            this.trackGridOpacity.TickFrequency = 16;
            this.trackGridOpacity.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackGridOpacity.Value = 128;
            this.trackGridOpacity.Scroll += new System.EventHandler(this.trackGridOpacity_Scroll);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(383, 248);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 23);
            this.label4.TabIndex = 35;
            this.label4.Text = "Category:";
            // 
            // labelCharNo
            // 
            this.labelCharNo.Location = new System.Drawing.Point(383, 192);
            this.labelCharNo.Name = "labelCharNo";
            this.labelCharNo.Size = new System.Drawing.Size(231, 23);
            this.labelCharNo.TabIndex = 34;
            this.labelCharNo.Text = "label1";
            this.labelCharNo.Paint += new System.Windows.Forms.PaintEventHandler(this.labelCharNo_Paint);
            // 
            // checkShowGrid
            // 
            this.checkShowGrid.AutoSize = true;
            this.checkShowGrid.Location = new System.Drawing.Point(386, 327);
            this.checkShowGrid.Name = "checkShowGrid";
            this.checkShowGrid.Size = new System.Drawing.Size(75, 17);
            this.checkShowGrid.TabIndex = 13;
            this.checkShowGrid.Text = "Show Grid";
            this.checkShowGrid.UseVisualStyleBackColor = true;
            this.checkShowGrid.CheckedChanged += new System.EventHandler(this.checkShowGrid_CheckedChanged);
            // 
            // checkPasteMultiColor
            // 
            this.checkPasteMultiColor.AutoSize = true;
            this.checkPasteMultiColor.Location = new System.Drawing.Point(386, 304);
            this.checkPasteMultiColor.Name = "checkPasteMultiColor";
            this.checkPasteMultiColor.Size = new System.Drawing.Size(145, 17);
            this.checkPasteMultiColor.TabIndex = 12;
            this.checkPasteMultiColor.Text = "Force Multicolor on paste";
            this.checkPasteMultiColor.UseVisualStyleBackColor = true;
            // 
            // editMoveTargetIndex
            // 
            this.editMoveTargetIndex.Location = new System.Drawing.Point(107, 83);
            this.editMoveTargetIndex.Name = "editMoveTargetIndex";
            this.editMoveTargetIndex.Size = new System.Drawing.Size(109, 20);
            this.editMoveTargetIndex.TabIndex = 21;
            this.editMoveTargetIndex.TextChanged += new System.EventHandler(this.editMoveTargetIndex_TextChanged);
            // 
            // btnShiftLeft
            // 
            this.btnShiftLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftLeft.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftLeft.Image")));
            this.btnShiftLeft.Location = new System.Drawing.Point(3, 3);
            this.btnShiftLeft.Name = "btnShiftLeft";
            this.btnShiftLeft.Size = new System.Drawing.Size(26, 26);
            this.btnShiftLeft.TabIndex = 0;
            this.toolTip1.SetToolTip(this.btnShiftLeft, "Shift Left");
            this.btnShiftLeft.Click += new DecentForms.EventHandler(this.btnShiftLeft_Click);
            // 
            // btnShiftRight
            // 
            this.btnShiftRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftRight.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftRight.Image")));
            this.btnShiftRight.Location = new System.Drawing.Point(35, 3);
            this.btnShiftRight.Name = "btnShiftRight";
            this.btnShiftRight.Size = new System.Drawing.Size(26, 26);
            this.btnShiftRight.TabIndex = 1;
            this.toolTip1.SetToolTip(this.btnShiftRight, "Shift Right");
            this.btnShiftRight.Click += new DecentForms.EventHandler(this.btnShiftRight_Click);
            // 
            // btnShiftUp
            // 
            this.btnShiftUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftUp.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftUp.Image")));
            this.btnShiftUp.Location = new System.Drawing.Point(67, 3);
            this.btnShiftUp.Name = "btnShiftUp";
            this.btnShiftUp.Size = new System.Drawing.Size(26, 26);
            this.btnShiftUp.TabIndex = 2;
            this.toolTip1.SetToolTip(this.btnShiftUp, "Shift Up");
            this.btnShiftUp.Click += new DecentForms.EventHandler(this.btnShiftUp_Click);
            // 
            // btnShiftDown
            // 
            this.btnShiftDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftDown.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftDown.Image")));
            this.btnShiftDown.Location = new System.Drawing.Point(99, 3);
            this.btnShiftDown.Name = "btnShiftDown";
            this.btnShiftDown.Size = new System.Drawing.Size(26, 26);
            this.btnShiftDown.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnShiftDown, "Shift Down");
            this.btnShiftDown.Click += new DecentForms.EventHandler(this.btnShiftDown_Click);
            // 
            // btnMirrorX
            // 
            this.btnMirrorX.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMirrorX.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMirrorX.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMirrorX.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMirrorX.Image = ((System.Drawing.Image)(resources.GetObject("btnMirrorX.Image")));
            this.btnMirrorX.Location = new System.Drawing.Point(131, 3);
            this.btnMirrorX.Name = "btnMirrorX";
            this.btnMirrorX.Size = new System.Drawing.Size(26, 26);
            this.btnMirrorX.TabIndex = 4;
            this.toolTip1.SetToolTip(this.btnMirrorX, "Mirror X");
            this.btnMirrorX.Click += new DecentForms.EventHandler(this.btnMirrorX_Click);
            // 
            // btnMirrorY
            // 
            this.btnMirrorY.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMirrorY.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMirrorY.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMirrorY.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMirrorY.Image = ((System.Drawing.Image)(resources.GetObject("btnMirrorY.Image")));
            this.btnMirrorY.Location = new System.Drawing.Point(163, 3);
            this.btnMirrorY.Name = "btnMirrorY";
            this.btnMirrorY.Size = new System.Drawing.Size(26, 26);
            this.btnMirrorY.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btnMirrorY, "Mirror Y");
            this.btnMirrorY.Click += new DecentForms.EventHandler(this.btnMirrorY_Click);
            // 
            // btnInvert
            // 
            this.btnInvert.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnInvert.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnInvert.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnInvert.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnInvert.Image = ((System.Drawing.Image)(resources.GetObject("btnInvert.Image")));
            this.btnInvert.Location = new System.Drawing.Point(195, 3);
            this.btnInvert.Name = "btnInvert";
            this.btnInvert.Size = new System.Drawing.Size(26, 26);
            this.btnInvert.TabIndex = 6;
            this.toolTip1.SetToolTip(this.btnInvert, "Invert");
            this.btnInvert.Click += new DecentForms.EventHandler(this.btnInvert_Click);
            // 
            // btnRotateLeft
            // 
            this.btnRotateLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRotateLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRotateLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRotateLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRotateLeft.Image = ((System.Drawing.Image)(resources.GetObject("btnRotateLeft.Image")));
            this.btnRotateLeft.Location = new System.Drawing.Point(227, 3);
            this.btnRotateLeft.Name = "btnRotateLeft";
            this.btnRotateLeft.Size = new System.Drawing.Size(26, 26);
            this.btnRotateLeft.TabIndex = 7;
            this.toolTip1.SetToolTip(this.btnRotateLeft, "Rotate Left");
            this.btnRotateLeft.Click += new DecentForms.EventHandler(this.btnRotateLeft_Click);
            // 
            // button3
            // 
            this.button3.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.button3.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.button3.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.button3.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button3.Image = ((System.Drawing.Image)(resources.GetObject("button3.Image")));
            this.button3.Location = new System.Drawing.Point(259, 3);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(26, 26);
            this.button3.TabIndex = 8;
            this.toolTip1.SetToolTip(this.button3, "Rotate Right");
            this.button3.Click += new DecentForms.EventHandler(this.btnRotateRight_Click);
            // 
            // ButtonCanvas1x1
            // 
            this.ButtonCanvas1x1.Appearance = System.Windows.Forms.Appearance.Button;
            this.ButtonCanvas1x1.BorderStyle = DecentForms.BorderStyle.NONE;
            this.ButtonCanvas1x1.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.ButtonCanvas1x1.Checked = false;
            this.ButtonCanvas1x1.Image = null;
            this.ButtonCanvas1x1.Location = new System.Drawing.Point(3, 3);
            this.ButtonCanvas1x1.Name = "ButtonCanvas1x1";
            this.ButtonCanvas1x1.Size = new System.Drawing.Size(33, 24);
            this.ButtonCanvas1x1.TabIndex = 62;
            this.ButtonCanvas1x1.Text = "1x1";
            this.toolTip1.SetToolTip(this.ButtonCanvas1x1, "Place/Pick Single Tile");
            this.ButtonCanvas1x1.CheckedChanged += new DecentForms.EventHandler(this.btnToolEdit_CheckedChanged);
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnZoomIn.Image = null;
            this.btnZoomIn.Location = new System.Drawing.Point(159, 3);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(24, 24);
            this.btnZoomIn.TabIndex = 59;
            this.btnZoomIn.Text = "+";
            this.toolTip1.SetToolTip(this.btnZoomIn, "Zoom in");
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnZoomOut.Image = null;
            this.btnZoomOut.Location = new System.Drawing.Point(189, 3);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(24, 24);
            this.btnZoomOut.TabIndex = 58;
            this.btnZoomOut.Text = "-";
            this.toolTip1.SetToolTip(this.btnZoomOut, "Zoom out");
            // 
            // btnCopy
            // 
            this.btnCopy.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCopy.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCopy.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCopy.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCopy.Image = ((System.Drawing.Image)(resources.GetObject("btnCopy.Image")));
            this.btnCopy.Location = new System.Drawing.Point(10, 23);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(26, 23);
            this.btnCopy.TabIndex = 15;
            this.toolTip1.SetToolTip(this.btnCopy, "Copy Characters to Clipboard");
            this.btnCopy.Click += new DecentForms.EventHandler(this.btnCopy_Click);
            // 
            // btnPasteFromClipboard
            // 
            this.btnPasteFromClipboard.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPasteFromClipboard.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnPasteFromClipboard.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnPasteFromClipboard.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPasteFromClipboard.Image = null;
            this.btnPasteFromClipboard.Location = new System.Drawing.Point(107, 23);
            this.btnPasteFromClipboard.Name = "btnPasteFromClipboard";
            this.btnPasteFromClipboard.Size = new System.Drawing.Size(109, 23);
            this.btnPasteFromClipboard.TabIndex = 17;
            this.btnPasteFromClipboard.Text = "Paste Image";
            this.toolTip1.SetToolTip(this.btnPasteFromClipboard, "Paste Image");
            this.btnPasteFromClipboard.Click += new DecentForms.EventHandler(this.btnPasteFromClipboard_Click);
            // 
            // btnPaste
            // 
            this.btnPaste.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPaste.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnPaste.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnPaste.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPaste.Image = ((System.Drawing.Image)(resources.GetObject("btnPaste.Image")));
            this.btnPaste.Location = new System.Drawing.Point(42, 23);
            this.btnPaste.Name = "btnPaste";
            this.btnPaste.Size = new System.Drawing.Size(26, 23);
            this.btnPaste.TabIndex = 16;
            this.toolTip1.SetToolTip(this.btnPaste, "Paste Characters");
            this.btnPaste.Click += new DecentForms.EventHandler(this.btnPaste_Click);
            // 
            // btnHighlightDuplicates
            // 
            this.btnHighlightDuplicates.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnHighlightDuplicates.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnHighlightDuplicates.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnHighlightDuplicates.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnHighlightDuplicates.Image = null;
            this.btnHighlightDuplicates.Location = new System.Drawing.Point(10, 110);
            this.btnHighlightDuplicates.Name = "btnHighlightDuplicates";
            this.btnHighlightDuplicates.Size = new System.Drawing.Size(90, 23);
            this.btnHighlightDuplicates.TabIndex = 22;
            this.btnHighlightDuplicates.Text = "Duplicates";
            this.toolTip1.SetToolTip(this.btnHighlightDuplicates, "Highlight duplicates");
            this.btnHighlightDuplicates.Click += new DecentForms.EventHandler(this.btnHighlightDuplicates_Click);
            // 
            // btnCharMoveDown
            // 
            this.btnCharMoveDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCharMoveDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCharMoveDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCharMoveDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCharMoveDown.Enabled = false;
            this.btnCharMoveDown.Image = ((System.Drawing.Image)(resources.GetObject("btnCharMoveDown.Image")));
            this.btnCharMoveDown.Location = new System.Drawing.Point(102, 19);
            this.btnCharMoveDown.Name = "btnCharMoveDown";
            this.btnCharMoveDown.Size = new System.Drawing.Size(26, 26);
            this.btnCharMoveDown.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnCharMoveDown, "Move Character Down");
            this.btnCharMoveDown.Click += new DecentForms.EventHandler(this.btnCharMoveDown_Click);
            // 
            // btnCharMoveRight
            // 
            this.btnCharMoveRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCharMoveRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCharMoveRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCharMoveRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCharMoveRight.Enabled = false;
            this.btnCharMoveRight.Image = ((System.Drawing.Image)(resources.GetObject("btnCharMoveRight.Image")));
            this.btnCharMoveRight.Location = new System.Drawing.Point(38, 19);
            this.btnCharMoveRight.Name = "btnCharMoveRight";
            this.btnCharMoveRight.Size = new System.Drawing.Size(26, 26);
            this.btnCharMoveRight.TabIndex = 2;
            this.toolTip1.SetToolTip(this.btnCharMoveRight, "Move Character Right");
            this.btnCharMoveRight.Click += new DecentForms.EventHandler(this.btnCharMoveRight_Click);
            // 
            // btnCharMoveUp
            // 
            this.btnCharMoveUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCharMoveUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCharMoveUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCharMoveUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCharMoveUp.Enabled = false;
            this.btnCharMoveUp.Image = ((System.Drawing.Image)(resources.GetObject("btnCharMoveUp.Image")));
            this.btnCharMoveUp.Location = new System.Drawing.Point(70, 19);
            this.btnCharMoveUp.Name = "btnCharMoveUp";
            this.btnCharMoveUp.Size = new System.Drawing.Size(26, 26);
            this.btnCharMoveUp.TabIndex = 1;
            this.toolTip1.SetToolTip(this.btnCharMoveUp, "Move Character Up");
            this.btnCharMoveUp.Click += new DecentForms.EventHandler(this.btnCharMoveUp_Click);
            // 
            // btnCharMoveLeft
            // 
            this.btnCharMoveLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCharMoveLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCharMoveLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCharMoveLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCharMoveLeft.Enabled = false;
            this.btnCharMoveLeft.Image = ((System.Drawing.Image)(resources.GetObject("btnCharMoveLeft.Image")));
            this.btnCharMoveLeft.Location = new System.Drawing.Point(6, 19);
            this.btnCharMoveLeft.Name = "btnCharMoveLeft";
            this.btnCharMoveLeft.Size = new System.Drawing.Size(26, 26);
            this.btnCharMoveLeft.TabIndex = 0;
            this.toolTip1.SetToolTip(this.btnCharMoveLeft, "Move Character Left");
            this.btnCharMoveLeft.Click += new DecentForms.EventHandler(this.btnCharMoveLeft_Click);
            // 
            // tabCharacterEditor
            // 
            this.tabCharacterEditor.Controls.Add(this.tabEditor);
            this.tabCharacterEditor.Controls.Add(this.tabCategories);
            this.tabCharacterEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabCharacterEditor.Location = new System.Drawing.Point(0, 0);
            this.tabCharacterEditor.Name = "tabCharacterEditor";
            this.tabCharacterEditor.SelectedIndex = 0;
            this.tabCharacterEditor.Size = new System.Drawing.Size(1438, 742);
            this.tabCharacterEditor.TabIndex = 0;
            // 
            // tabEditor
            // 
            this.tabEditor.Controls.Add(this.splitContainer1);
            this.tabEditor.Controls.Add(this.groupBox2);
            this.tabEditor.Controls.Add(this.flowLayoutPanel2);
            this.tabEditor.Controls.Add(this.flowLayoutPanel1);
            this.tabEditor.Controls.Add(this.label1);
            this.tabEditor.Controls.Add(this.comboCharactersPerRow);
            this.tabEditor.Controls.Add(this.groupBox1);
            this.tabEditor.Controls.Add(this.panelColorChooser);
            this.tabEditor.Controls.Add(this.groupRightClick);
            this.tabEditor.Controls.Add(this.groupMoveChar);
            this.tabEditor.Controls.Add(this.panelColorSettings);
            this.tabEditor.Controls.Add(this.canvasEditor);
            this.tabEditor.Controls.Add(this.comboCharsetMode);
            this.tabEditor.Controls.Add(this.labelCharsetMode);
            this.tabEditor.Controls.Add(this.flowPlaygroundScale);
            this.tabEditor.Controls.Add(this.checkPasteMultiColor);
            this.tabEditor.Controls.Add(this.checkShowGrid);
            this.tabEditor.Controls.Add(this.labelCharNo);
            this.tabEditor.Controls.Add(this.label4);
            this.tabEditor.Controls.Add(this.labelCharUsageCount);
            this.tabEditor.Controls.Add(this.labelCharMapUsageCount);
            this.tabEditor.Controls.Add(this.comboCategories);
            this.tabEditor.Location = new System.Drawing.Point(4, 22);
            this.tabEditor.Name = "tabEditor";
            this.tabEditor.Padding = new System.Windows.Forms.Padding(3);
            this.tabEditor.Size = new System.Drawing.Size(1430, 716);
            this.tabEditor.TabIndex = 0;
            this.tabEditor.Text = "Editor";
            this.tabEditor.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(620, 6);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.picturePlayground);
            this.splitContainer1.Size = new System.Drawing.Size(804, 545);
            this.splitContainer1.SplitterDistance = 317;
            this.splitContainer1.TabIndex = 68;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.panelCharacters);
            this.splitContainer2.Size = new System.Drawing.Size(317, 545);
            this.splitContainer2.SplitterDistance = 406;
            this.splitContainer2.TabIndex = 69;
            // 
            // panelCharacters
            // 
            this.panelCharacters.AllowPopup = false;
            this.panelCharacters.AutoScroll = true;
            this.panelCharacters.AutoScrollHorizontalMaximum = 100;
            this.panelCharacters.AutoScrollHorizontalMinimum = 0;
            this.panelCharacters.AutoScrollHPos = 0;
            this.panelCharacters.AutoScrollVerticalMaximum = -23;
            this.panelCharacters.AutoScrollVerticalMinimum = 0;
            this.panelCharacters.AutoScrollVPos = 0;
            this.panelCharacters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelCharacters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCharacters.EnableAutoScrollHorizontal = true;
            this.panelCharacters.EnableAutoScrollVertical = true;
            this.panelCharacters.HottrackColor = ((uint)(2151694591u));
            this.panelCharacters.ItemHeight = 8;
            this.panelCharacters.ItemWidth = 8;
            this.panelCharacters.Location = new System.Drawing.Point(0, 0);
            this.panelCharacters.Name = "panelCharacters";
            this.panelCharacters.PixelFormat = GR.Drawing.PixelFormat.DontCare;
            this.panelCharacters.SelectedIndex = -1;
            this.panelCharacters.Size = new System.Drawing.Size(317, 406);
            this.panelCharacters.TabIndex = 14;
            this.panelCharacters.TabStop = true;
            this.panelCharacters.VisibleAutoScrollHorizontal = false;
            this.panelCharacters.VisibleAutoScrollVertical = false;
            this.panelCharacters.SelectionChanged += new System.EventHandler(this.panelCharacters_SelectionChanged);
            // 
            // picturePlayground
            // 
            this.picturePlayground.AutoResize = false;
            this.picturePlayground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.picturePlayground.DisplayPage = fastImage1;
            this.picturePlayground.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picturePlayground.Image = null;
            this.picturePlayground.Location = new System.Drawing.Point(0, 0);
            this.picturePlayground.Name = "picturePlayground";
            this.picturePlayground.Size = new System.Drawing.Size(483, 545);
            this.picturePlayground.TabIndex = 51;
            this.picturePlayground.TabStop = false;
            this.picturePlayground.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picturePlayground_MouseDown);
            this.picturePlayground.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picturePlayground_MouseMove);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.btnClearPlayground);
            this.groupBox2.Controls.Add(this.btnCropPlayground);
            this.groupBox2.Controls.Add(this.labelSwatchSize);
            this.groupBox2.Controls.Add(this.editSwatchSize);
            this.groupBox2.Controls.Add(this.trackGridOpacity);
            this.groupBox2.Controls.Add(this.checkShowPlaygroundGrid);
            this.groupBox2.Location = new System.Drawing.Point(1208, 599);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(216, 112);
            this.groupBox2.TabIndex = 67;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "groupBox2";
            // 
            // btnClearPlayground
            // 
            this.btnClearPlayground.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnClearPlayground.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnClearPlayground.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnClearPlayground.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClearPlayground.Image = null;
            this.btnClearPlayground.Location = new System.Drawing.Point(102, 80);
            this.btnClearPlayground.Name = "btnClearPlayground";
            this.btnClearPlayground.Size = new System.Drawing.Size(100, 24);
            this.btnClearPlayground.TabIndex = 1;
            this.btnClearPlayground.Text = "Clear Playground";
            this.btnClearPlayground.Click += new DecentForms.EventHandler(this.btnClearPlayground_Click);
            // 
            // btnCropPlayground
            // 
            this.btnCropPlayground.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCropPlayground.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCropPlayground.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCropPlayground.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCropPlayground.Image = null;
            this.btnCropPlayground.Location = new System.Drawing.Point(6, 80);
            this.btnCropPlayground.Name = "btnCropPlayground";
            this.btnCropPlayground.Size = new System.Drawing.Size(90, 24);
            this.btnCropPlayground.TabIndex = 0;
            this.btnCropPlayground.Text = "Crop to view";
            this.btnCropPlayground.Click += new DecentForms.EventHandler(this.btnCropPlayground_Click);
            // 
            // labelSwatchSize
            // 
            this.labelSwatchSize.AutoSize = true;
            this.labelSwatchSize.Location = new System.Drawing.Point(6, 20);
            this.labelSwatchSize.Name = "labelSwatchSize";
            this.labelSwatchSize.Size = new System.Drawing.Size(69, 13);
            this.labelSwatchSize.TabIndex = 10;
            this.labelSwatchSize.Text = "Swatch Size:";
            // 
            // editSwatchSize
            // 
            this.editSwatchSize.Location = new System.Drawing.Point(81, 17);
            this.editSwatchSize.Name = "editSwatchSize";
            this.editSwatchSize.Size = new System.Drawing.Size(100, 20);
            this.editSwatchSize.TabIndex = 11;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btnShiftLeft);
            this.flowLayoutPanel2.Controls.Add(this.btnShiftRight);
            this.flowLayoutPanel2.Controls.Add(this.btnShiftUp);
            this.flowLayoutPanel2.Controls.Add(this.btnShiftDown);
            this.flowLayoutPanel2.Controls.Add(this.btnMirrorX);
            this.flowLayoutPanel2.Controls.Add(this.btnMirrorY);
            this.flowLayoutPanel2.Controls.Add(this.btnInvert);
            this.flowLayoutPanel2.Controls.Add(this.btnRotateLeft);
            this.flowLayoutPanel2.Controls.Add(this.button3);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(6, 374);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(316, 34);
            this.flowLayoutPanel2.TabIndex = 64;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.ButtonCanvas1x1);
            this.flowLayoutPanel1.Controls.Add(this.ButtonCanvas2x2);
            this.flowLayoutPanel1.Controls.Add(this.ButtonCanvas2x3);
            this.flowLayoutPanel1.Controls.Add(this.ButtonCanvas4x4);
            this.flowLayoutPanel1.Controls.Add(this.btnZoomIn);
            this.flowLayoutPanel1.Controls.Add(this.btnZoomOut);
            this.flowLayoutPanel1.Controls.Add(this.labelZoom);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(6, 414);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(263, 33);
            this.flowLayoutPanel1.TabIndex = 63;
            // 
            // ButtonCanvas2x2
            // 
            this.ButtonCanvas2x2.Appearance = System.Windows.Forms.Appearance.Button;
            this.ButtonCanvas2x2.BorderStyle = DecentForms.BorderStyle.NONE;
            this.ButtonCanvas2x2.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.ButtonCanvas2x2.Checked = false;
            this.ButtonCanvas2x2.Image = null;
            this.ButtonCanvas2x2.Location = new System.Drawing.Point(42, 3);
            this.ButtonCanvas2x2.Name = "ButtonCanvas2x2";
            this.ButtonCanvas2x2.Size = new System.Drawing.Size(33, 24);
            this.ButtonCanvas2x2.TabIndex = 62;
            this.ButtonCanvas2x2.Text = "2x2";
            this.ButtonCanvas2x2.CheckedChanged += new DecentForms.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // ButtonCanvas2x3
            // 
            this.ButtonCanvas2x3.Appearance = System.Windows.Forms.Appearance.Button;
            this.ButtonCanvas2x3.BorderStyle = DecentForms.BorderStyle.NONE;
            this.ButtonCanvas2x3.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.ButtonCanvas2x3.Checked = false;
            this.ButtonCanvas2x3.Image = null;
            this.ButtonCanvas2x3.Location = new System.Drawing.Point(81, 3);
            this.ButtonCanvas2x3.Name = "ButtonCanvas2x3";
            this.ButtonCanvas2x3.Size = new System.Drawing.Size(33, 24);
            this.ButtonCanvas2x3.TabIndex = 62;
            this.ButtonCanvas2x3.Text = "2x3";
            this.ButtonCanvas2x3.CheckedChanged += new DecentForms.EventHandler(this.ButtonCanvas2x3_CheckedChanged);
            // 
            // ButtonCanvas4x4
            // 
            this.ButtonCanvas4x4.Appearance = System.Windows.Forms.Appearance.Button;
            this.ButtonCanvas4x4.BorderStyle = DecentForms.BorderStyle.NONE;
            this.ButtonCanvas4x4.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.ButtonCanvas4x4.Checked = false;
            this.ButtonCanvas4x4.Image = null;
            this.ButtonCanvas4x4.Location = new System.Drawing.Point(120, 3);
            this.ButtonCanvas4x4.Name = "ButtonCanvas4x4";
            this.ButtonCanvas4x4.Size = new System.Drawing.Size(33, 24);
            this.ButtonCanvas4x4.TabIndex = 62;
            this.ButtonCanvas4x4.Text = "4x4";
            this.ButtonCanvas4x4.CheckedChanged += new DecentForms.EventHandler(this.ButtonCanvas4x4_CheckedChanged);
            // 
            // labelZoom
            // 
            this.labelZoom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.labelZoom.AutoSize = true;
            this.labelZoom.Location = new System.Drawing.Point(219, 0);
            this.labelZoom.Name = "labelZoom";
            this.labelZoom.Size = new System.Drawing.Size(33, 30);
            this.labelZoom.TabIndex = 57;
            this.labelZoom.Text = "100%";
            this.labelZoom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(383, 275);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 61;
            this.label1.Text = "Characters per row:";
            // 
            // comboCharactersPerRow
            // 
            this.comboCharactersPerRow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCharactersPerRow.FormattingEnabled = true;
            this.comboCharactersPerRow.Items.AddRange(new object[] {
            "2",
            "4",
            "8",
            "16",
            "32",
            "64",
            "128"});
            this.comboCharactersPerRow.Location = new System.Drawing.Point(488, 272);
            this.comboCharactersPerRow.Name = "comboCharactersPerRow";
            this.comboCharactersPerRow.Size = new System.Drawing.Size(126, 21);
            this.comboCharactersPerRow.TabIndex = 60;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnCopy);
            this.groupBox1.Controls.Add(this.btnMoveSelectionToTarget);
            this.groupBox1.Controls.Add(this.btnPasteFromClipboard);
            this.groupBox1.Controls.Add(this.btnPaste);
            this.groupBox1.Controls.Add(this.btnCreateTile);
            this.groupBox1.Controls.Add(this.editMoveTargetIndex);
            this.groupBox1.Controls.Add(this.btnHighlightDuplicates);
            this.groupBox1.Controls.Add(this.btnRestoreDefault);
            this.groupBox1.Controls.Add(this.btnRemoveDuplicates);
            this.groupBox1.Controls.Add(this.btnClearChars);
            this.groupBox1.Location = new System.Drawing.Point(382, 350);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(232, 176);
            this.groupBox1.TabIndex = 55;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // btnMoveSelectionToTarget
            // 
            this.btnMoveSelectionToTarget.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveSelectionToTarget.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveSelectionToTarget.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveSelectionToTarget.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveSelectionToTarget.Image = null;
            this.btnMoveSelectionToTarget.Location = new System.Drawing.Point(10, 81);
            this.btnMoveSelectionToTarget.Name = "btnMoveSelectionToTarget";
            this.btnMoveSelectionToTarget.Size = new System.Drawing.Size(90, 23);
            this.btnMoveSelectionToTarget.TabIndex = 20;
            this.btnMoveSelectionToTarget.Text = "Move to Index";
            this.btnMoveSelectionToTarget.Click += new DecentForms.EventHandler(this.btnMoveSelectionToTarget_Click);
            // 
            // btnCreateTile
            // 
            this.btnCreateTile.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCreateTile.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCreateTile.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCreateTile.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCreateTile.Image = null;
            this.btnCreateTile.Location = new System.Drawing.Point(10, 139);
            this.btnCreateTile.Name = "btnCreateTile";
            this.btnCreateTile.Size = new System.Drawing.Size(90, 23);
            this.btnCreateTile.TabIndex = 24;
            this.btnCreateTile.Text = "Create Tile";
            this.btnCreateTile.Visible = false;
            this.btnCreateTile.Click += new DecentForms.EventHandler(this.btnCreateTile_Click);
            // 
            // btnRestoreDefault
            // 
            this.btnRestoreDefault.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRestoreDefault.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRestoreDefault.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRestoreDefault.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRestoreDefault.Image = null;
            this.btnRestoreDefault.Location = new System.Drawing.Point(107, 52);
            this.btnRestoreDefault.Name = "btnRestoreDefault";
            this.btnRestoreDefault.Size = new System.Drawing.Size(109, 23);
            this.btnRestoreDefault.TabIndex = 19;
            this.btnRestoreDefault.Text = "Restore Default";
            this.btnRestoreDefault.Click += new DecentForms.EventHandler(this.btnRestoreDefault_Click);
            // 
            // btnRemoveDuplicates
            // 
            this.btnRemoveDuplicates.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRemoveDuplicates.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRemoveDuplicates.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRemoveDuplicates.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRemoveDuplicates.Image = null;
            this.btnRemoveDuplicates.Location = new System.Drawing.Point(107, 110);
            this.btnRemoveDuplicates.Name = "btnRemoveDuplicates";
            this.btnRemoveDuplicates.Size = new System.Drawing.Size(109, 23);
            this.btnRemoveDuplicates.TabIndex = 23;
            this.btnRemoveDuplicates.Text = "Remove Duplicates";
            this.btnRemoveDuplicates.Click += new DecentForms.EventHandler(this.btnRemoveDuplicates_Click);
            // 
            // btnClearChars
            // 
            this.btnClearChars.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnClearChars.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnClearChars.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnClearChars.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClearChars.Image = null;
            this.btnClearChars.Location = new System.Drawing.Point(10, 52);
            this.btnClearChars.Name = "btnClearChars";
            this.btnClearChars.Size = new System.Drawing.Size(90, 23);
            this.btnClearChars.TabIndex = 18;
            this.btnClearChars.Text = "Clear";
            this.btnClearChars.Click += new DecentForms.EventHandler(this.btnClear_Click);
            // 
            // panelColorChooser
            // 
            this.panelColorChooser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.panelColorChooser.Location = new System.Drawing.Point(950, 557);
            this.panelColorChooser.Name = "panelColorChooser";
            this.panelColorChooser.Size = new System.Drawing.Size(252, 154);
            this.panelColorChooser.TabIndex = 24;
            // 
            // groupRightClick
            // 
            this.groupRightClick.Controls.Add(this.radioRightClickDefault);
            this.groupRightClick.Controls.Add(this.radioRightClickBackground);
            this.groupRightClick.Controls.Add(this.radioRightClickMulticolor1);
            this.groupRightClick.Controls.Add(this.radioRightClickMulticolor2);
            this.groupRightClick.Controls.Add(this.radioRightClickCharColor);
            this.groupRightClick.Location = new System.Drawing.Point(9, 453);
            this.groupRightClick.Name = "groupRightClick";
            this.groupRightClick.Size = new System.Drawing.Size(125, 186);
            this.groupRightClick.TabIndex = 66;
            this.groupRightClick.TabStop = false;
            this.groupRightClick.Text = "Right-click drawing";
            // 
            // radioRightClickDefault
            // 
            this.radioRightClickDefault.Appearance = System.Windows.Forms.Appearance.Normal;
            this.radioRightClickDefault.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioRightClickDefault.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.radioRightClickDefault.Checked = true;
            this.radioRightClickDefault.Image = null;
            this.radioRightClickDefault.Location = new System.Drawing.Point(6, 19);
            this.radioRightClickDefault.Name = "radioRightClickDefault";
            this.radioRightClickDefault.Size = new System.Drawing.Size(116, 24);
            this.radioRightClickDefault.TabIndex = 0;
            this.radioRightClickDefault.Text = "Default behavior";
            this.radioRightClickDefault.CheckedChanged += new DecentForms.EventHandler(this.radioRightClick_CheckedChanged);
            // 
            // radioRightClickBackground
            // 
            this.radioRightClickBackground.Appearance = System.Windows.Forms.Appearance.Normal;
            this.radioRightClickBackground.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioRightClickBackground.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.radioRightClickBackground.Checked = false;
            this.radioRightClickBackground.Image = null;
            this.radioRightClickBackground.Location = new System.Drawing.Point(6, 49);
            this.radioRightClickBackground.Name = "radioRightClickBackground";
            this.radioRightClickBackground.Size = new System.Drawing.Size(116, 24);
            this.radioRightClickBackground.TabIndex = 1;
            this.radioRightClickBackground.Text = "Background";
            this.radioRightClickBackground.CheckedChanged += new DecentForms.EventHandler(this.radioRightClick_CheckedChanged);
            // 
            // radioRightClickMulticolor1
            // 
            this.radioRightClickMulticolor1.Appearance = System.Windows.Forms.Appearance.Normal;
            this.radioRightClickMulticolor1.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioRightClickMulticolor1.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.radioRightClickMulticolor1.Checked = false;
            this.radioRightClickMulticolor1.Image = null;
            this.radioRightClickMulticolor1.Location = new System.Drawing.Point(6, 79);
            this.radioRightClickMulticolor1.Name = "radioRightClickMulticolor1";
            this.radioRightClickMulticolor1.Size = new System.Drawing.Size(116, 24);
            this.radioRightClickMulticolor1.TabIndex = 2;
            this.radioRightClickMulticolor1.Text = "Multicolor 1";
            this.radioRightClickMulticolor1.CheckedChanged += new DecentForms.EventHandler(this.radioRightClick_CheckedChanged);
            // 
            // radioRightClickMulticolor2
            // 
            this.radioRightClickMulticolor2.Appearance = System.Windows.Forms.Appearance.Normal;
            this.radioRightClickMulticolor2.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioRightClickMulticolor2.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.radioRightClickMulticolor2.Checked = false;
            this.radioRightClickMulticolor2.Image = null;
            this.radioRightClickMulticolor2.Location = new System.Drawing.Point(6, 109);
            this.radioRightClickMulticolor2.Name = "radioRightClickMulticolor2";
            this.radioRightClickMulticolor2.Size = new System.Drawing.Size(116, 24);
            this.radioRightClickMulticolor2.TabIndex = 3;
            this.radioRightClickMulticolor2.Text = "Multicolor 2";
            this.radioRightClickMulticolor2.CheckedChanged += new DecentForms.EventHandler(this.radioRightClick_CheckedChanged);
            // 
            // radioRightClickCharColor
            // 
            this.radioRightClickCharColor.Appearance = System.Windows.Forms.Appearance.Normal;
            this.radioRightClickCharColor.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioRightClickCharColor.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.radioRightClickCharColor.Checked = false;
            this.radioRightClickCharColor.Image = null;
            this.radioRightClickCharColor.Location = new System.Drawing.Point(6, 139);
            this.radioRightClickCharColor.Name = "radioRightClickCharColor";
            this.radioRightClickCharColor.Size = new System.Drawing.Size(116, 24);
            this.radioRightClickCharColor.TabIndex = 4;
            this.radioRightClickCharColor.Text = "Char Color";
            this.radioRightClickCharColor.CheckedChanged += new DecentForms.EventHandler(this.radioRightClick_CheckedChanged);
            // 
            // groupMoveChar
            // 
            this.groupMoveChar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupMoveChar.Controls.Add(this.btnCharMoveDown);
            this.groupMoveChar.Controls.Add(this.btnCharMoveRight);
            this.groupMoveChar.Controls.Add(this.btnCharMoveUp);
            this.groupMoveChar.Controls.Add(this.btnCharMoveLeft);
            this.groupMoveChar.Location = new System.Drawing.Point(620, 560);
            this.groupMoveChar.Name = "groupMoveChar";
            this.groupMoveChar.Size = new System.Drawing.Size(137, 54);
            this.groupMoveChar.TabIndex = 25;
            this.groupMoveChar.TabStop = false;
            this.groupMoveChar.Text = "Move character";
            // 
            // panelColorSettings
            // 
            this.panelColorSettings.Location = new System.Drawing.Point(383, 3);
            this.panelColorSettings.Name = "panelColorSettings";
            this.panelColorSettings.Size = new System.Drawing.Size(231, 186);
            this.panelColorSettings.TabIndex = 9;
            // 
            // canvasEditor
            // 
            this.canvasEditor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.canvasEditor.Location = new System.Drawing.Point(6, 3);
            this.canvasEditor.Name = "canvasEditor";
            this.canvasEditor.Size = new System.Drawing.Size(370, 365);
            this.canvasEditor.TabIndex = 54;
            this.canvasEditor.TabStop = false;
            this.canvasEditor.Paint += new System.Windows.Forms.PaintEventHandler(this.canvasEditor_Paint);
            this.canvasEditor.MouseDown += new System.Windows.Forms.MouseEventHandler(this.canvasEditor_MouseDown);
            this.canvasEditor.MouseMove += new System.Windows.Forms.MouseEventHandler(this.canvasEditor_MouseMove);
            // 
            // comboCharsetMode
            // 
            this.comboCharsetMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboCharsetMode.FormattingEnabled = true;
            this.comboCharsetMode.Location = new System.Drawing.Point(457, 218);
            this.comboCharsetMode.Name = "comboCharsetMode";
            this.comboCharsetMode.Size = new System.Drawing.Size(157, 21);
            this.comboCharsetMode.TabIndex = 10;
            this.comboCharsetMode.SelectedIndexChanged += new System.EventHandler(this.comboCharsetMode_SelectedIndexChanged);
            // 
            // labelCharsetMode
            // 
            this.labelCharsetMode.AutoSize = true;
            this.labelCharsetMode.Location = new System.Drawing.Point(383, 221);
            this.labelCharsetMode.Name = "labelCharsetMode";
            this.labelCharsetMode.Size = new System.Drawing.Size(37, 13);
            this.labelCharsetMode.TabIndex = 17;
            this.labelCharsetMode.Text = "Mode:";
            // 
            // flowPlaygroundScale
            // 
            this.flowPlaygroundScale.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.flowPlaygroundScale.Controls.Add(this.radioPlaygroundScale1x);
            this.flowPlaygroundScale.Controls.Add(this.radioPlaygroundScale2x);
            this.flowPlaygroundScale.Controls.Add(this.radioPlaygroundScale4x);
            this.flowPlaygroundScale.Controls.Add(this.radioPlaygroundScale8x);
            this.flowPlaygroundScale.Location = new System.Drawing.Point(1214, 557);
            this.flowPlaygroundScale.Name = "flowPlaygroundScale";
            this.flowPlaygroundScale.Size = new System.Drawing.Size(196, 32);
            this.flowPlaygroundScale.TabIndex = 65;
            // 
            // radioPlaygroundScale1x
            // 
            this.radioPlaygroundScale1x.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioPlaygroundScale1x.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioPlaygroundScale1x.CheckAlign = DecentForms.ContentAlignment.MiddleCenter;
            this.radioPlaygroundScale1x.Checked = false;
            this.radioPlaygroundScale1x.Image = null;
            this.radioPlaygroundScale1x.Location = new System.Drawing.Point(3, 3);
            this.radioPlaygroundScale1x.Name = "radioPlaygroundScale1x";
            this.radioPlaygroundScale1x.Size = new System.Drawing.Size(30, 24);
            this.radioPlaygroundScale1x.TabIndex = 0;
            this.radioPlaygroundScale1x.Text = "1x";
            this.radioPlaygroundScale1x.CheckedChanged += new DecentForms.EventHandler(this.radioPlaygroundScale_CheckedChanged);
            // 
            // radioPlaygroundScale2x
            // 
            this.radioPlaygroundScale2x.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioPlaygroundScale2x.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioPlaygroundScale2x.CheckAlign = DecentForms.ContentAlignment.MiddleCenter;
            this.radioPlaygroundScale2x.Checked = true;
            this.radioPlaygroundScale2x.Image = null;
            this.radioPlaygroundScale2x.Location = new System.Drawing.Point(39, 3);
            this.radioPlaygroundScale2x.Name = "radioPlaygroundScale2x";
            this.radioPlaygroundScale2x.Size = new System.Drawing.Size(30, 24);
            this.radioPlaygroundScale2x.TabIndex = 1;
            this.radioPlaygroundScale2x.Text = "2x";
            this.radioPlaygroundScale2x.CheckedChanged += new DecentForms.EventHandler(this.radioPlaygroundScale_CheckedChanged);
            // 
            // radioPlaygroundScale4x
            // 
            this.radioPlaygroundScale4x.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioPlaygroundScale4x.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioPlaygroundScale4x.CheckAlign = DecentForms.ContentAlignment.MiddleCenter;
            this.radioPlaygroundScale4x.Checked = false;
            this.radioPlaygroundScale4x.Image = null;
            this.radioPlaygroundScale4x.Location = new System.Drawing.Point(75, 3);
            this.radioPlaygroundScale4x.Name = "radioPlaygroundScale4x";
            this.radioPlaygroundScale4x.Size = new System.Drawing.Size(30, 24);
            this.radioPlaygroundScale4x.TabIndex = 2;
            this.radioPlaygroundScale4x.Text = "4x";
            this.radioPlaygroundScale4x.CheckedChanged += new DecentForms.EventHandler(this.radioPlaygroundScale_CheckedChanged);
            // 
            // radioPlaygroundScale8x
            // 
            this.radioPlaygroundScale8x.Appearance = System.Windows.Forms.Appearance.Button;
            this.radioPlaygroundScale8x.BorderStyle = DecentForms.BorderStyle.NONE;
            this.radioPlaygroundScale8x.CheckAlign = DecentForms.ContentAlignment.MiddleCenter;
            this.radioPlaygroundScale8x.Checked = false;
            this.radioPlaygroundScale8x.Image = null;
            this.radioPlaygroundScale8x.Location = new System.Drawing.Point(111, 3);
            this.radioPlaygroundScale8x.Name = "radioPlaygroundScale8x";
            this.radioPlaygroundScale8x.Size = new System.Drawing.Size(30, 24);
            this.radioPlaygroundScale8x.TabIndex = 3;
            this.radioPlaygroundScale8x.Text = "8x";
            this.radioPlaygroundScale8x.CheckedChanged += new DecentForms.EventHandler(this.radioPlaygroundScale_CheckedChanged);
            // 
            // labelCharUsageCount
            // 
            this.labelCharUsageCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCharUsageCount.AutoSize = true;
            this.labelCharUsageCount.Location = new System.Drawing.Point(763, 560);
            this.labelCharUsageCount.Name = "labelCharUsageCount";
            this.labelCharUsageCount.Size = new System.Drawing.Size(46, 13);
            this.labelCharUsageCount.TabIndex = 36;
            this.labelCharUsageCount.Text = "[tile use]";
            // 
            // labelCharMapUsageCount
            // 
            this.labelCharMapUsageCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCharMapUsageCount.AutoSize = true;
            this.labelCharMapUsageCount.Location = new System.Drawing.Point(763, 579);
            this.labelCharMapUsageCount.Name = "labelCharMapUsageCount";
            this.labelCharMapUsageCount.Size = new System.Drawing.Size(53, 13);
            this.labelCharMapUsageCount.TabIndex = 37;
            this.labelCharMapUsageCount.Text = "[map use]";
            // 
            // tabCategories
            // 
            this.tabCategories.Controls.Add(this.btnMoveCategoryDown);
            this.tabCategories.Controls.Add(this.btnMoveCategoryUp);
            this.tabCategories.Controls.Add(this.groupAllCategories);
            this.tabCategories.Controls.Add(this.groupCategorySpecific);
            this.tabCategories.Controls.Add(this.btnDelete);
            this.tabCategories.Controls.Add(this.btnAddCategory);
            this.tabCategories.Controls.Add(this.listCategories);
            this.tabCategories.Controls.Add(this.editCategoryName);
            this.tabCategories.Controls.Add(this.label3);
            this.tabCategories.Location = new System.Drawing.Point(4, 22);
            this.tabCategories.Name = "tabCategories";
            this.tabCategories.Padding = new System.Windows.Forms.Padding(3);
            this.tabCategories.Size = new System.Drawing.Size(1430, 631);
            this.tabCategories.TabIndex = 1;
            this.tabCategories.Text = "Categories";
            this.tabCategories.UseVisualStyleBackColor = true;
            // 
            // btnMoveCategoryDown
            // 
            this.btnMoveCategoryDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveCategoryDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveCategoryDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveCategoryDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveCategoryDown.Enabled = false;
            this.btnMoveCategoryDown.Image = null;
            this.btnMoveCategoryDown.Location = new System.Drawing.Point(90, 201);
            this.btnMoveCategoryDown.Name = "btnMoveCategoryDown";
            this.btnMoveCategoryDown.Size = new System.Drawing.Size(75, 23);
            this.btnMoveCategoryDown.TabIndex = 12;
            this.btnMoveCategoryDown.Text = "Move Down";
            this.btnMoveCategoryDown.Click += new DecentForms.EventHandler(this.btnMoveCategoryDown_Click);
            // 
            // btnMoveCategoryUp
            // 
            this.btnMoveCategoryUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveCategoryUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveCategoryUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveCategoryUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveCategoryUp.Enabled = false;
            this.btnMoveCategoryUp.Image = null;
            this.btnMoveCategoryUp.Location = new System.Drawing.Point(9, 201);
            this.btnMoveCategoryUp.Name = "btnMoveCategoryUp";
            this.btnMoveCategoryUp.Size = new System.Drawing.Size(75, 23);
            this.btnMoveCategoryUp.TabIndex = 12;
            this.btnMoveCategoryUp.Text = "Move Up";
            this.btnMoveCategoryUp.Click += new DecentForms.EventHandler(this.btnMoveCategoryUp_Click);
            // 
            // groupAllCategories
            // 
            this.groupAllCategories.Location = new System.Drawing.Point(261, 119);
            this.groupAllCategories.Name = "groupAllCategories";
            this.groupAllCategories.Size = new System.Drawing.Size(255, 76);
            this.groupAllCategories.TabIndex = 10;
            this.groupAllCategories.TabStop = false;
            this.groupAllCategories.Text = "All Categories";
            // 
            // groupCategorySpecific
            // 
            this.groupCategorySpecific.Controls.Add(this.label5);
            this.groupCategorySpecific.Controls.Add(this.editCollapseIndex);
            this.groupCategorySpecific.Controls.Add(this.btnCollapseCategory);
            this.groupCategorySpecific.Controls.Add(this.btnReseatCategory);
            this.groupCategorySpecific.Location = new System.Drawing.Point(261, 37);
            this.groupCategorySpecific.Name = "groupCategorySpecific";
            this.groupCategorySpecific.Size = new System.Drawing.Size(255, 76);
            this.groupCategorySpecific.TabIndex = 11;
            this.groupCategorySpecific.TabStop = false;
            this.groupCategorySpecific.Text = "Selected Category";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(117, 52);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "at index:";
            // 
            // editCollapseIndex
            // 
            this.editCollapseIndex.Location = new System.Drawing.Point(180, 49);
            this.editCollapseIndex.Name = "editCollapseIndex";
            this.editCollapseIndex.Size = new System.Drawing.Size(69, 20);
            this.editCollapseIndex.TabIndex = 5;
            // 
            // btnCollapseCategory
            // 
            this.btnCollapseCategory.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCollapseCategory.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCollapseCategory.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCollapseCategory.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCollapseCategory.Enabled = false;
            this.btnCollapseCategory.Image = null;
            this.btnCollapseCategory.Location = new System.Drawing.Point(6, 19);
            this.btnCollapseCategory.Name = "btnCollapseCategory";
            this.btnCollapseCategory.Size = new System.Drawing.Size(140, 23);
            this.btnCollapseCategory.TabIndex = 3;
            this.btnCollapseCategory.Text = "Collapse Unique Chars";
            this.btnCollapseCategory.Click += new DecentForms.EventHandler(this.btnCollapseCategory_Click);
            // 
            // btnReseatCategory
            // 
            this.btnReseatCategory.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnReseatCategory.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnReseatCategory.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnReseatCategory.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnReseatCategory.Enabled = false;
            this.btnReseatCategory.Image = null;
            this.btnReseatCategory.Location = new System.Drawing.Point(6, 47);
            this.btnReseatCategory.Name = "btnReseatCategory";
            this.btnReseatCategory.Size = new System.Drawing.Size(105, 23);
            this.btnReseatCategory.TabIndex = 3;
            this.btnReseatCategory.Text = "Reseat Category";
            this.btnReseatCategory.Click += new DecentForms.EventHandler(this.btnReseatCategory_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDelete.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnDelete.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnDelete.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDelete.Enabled = false;
            this.btnDelete.Image = null;
            this.btnDelete.Location = new System.Drawing.Point(342, 8);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(96, 23);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "Delete Category";
            this.btnDelete.Click += new DecentForms.EventHandler(this.btnDelete_Click);
            // 
            // btnAddCategory
            // 
            this.btnAddCategory.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddCategory.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAddCategory.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAddCategory.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddCategory.Enabled = false;
            this.btnAddCategory.Image = null;
            this.btnAddCategory.Location = new System.Drawing.Point(261, 8);
            this.btnAddCategory.Name = "btnAddCategory";
            this.btnAddCategory.Size = new System.Drawing.Size(75, 23);
            this.btnAddCategory.TabIndex = 9;
            this.btnAddCategory.Text = "Add";
            this.btnAddCategory.Click += new DecentForms.EventHandler(this.btnAddCategory_Click);
            // 
            // listCategories
            // 
            this.listCategories.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listCategories.FullRowSelect = true;
            this.listCategories.HideSelection = false;
            this.listCategories.Location = new System.Drawing.Point(9, 36);
            this.listCategories.Name = "listCategories";
            this.listCategories.ShowGroups = false;
            this.listCategories.Size = new System.Drawing.Size(246, 159);
            this.listCategories.TabIndex = 7;
            this.listCategories.UseCompatibleStateImageBehavior = false;
            this.listCategories.View = System.Windows.Forms.View.Details;
            this.listCategories.SelectedIndexChanged += new System.EventHandler(this.listCategories_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "No. Chars";
            this.columnHeader2.Width = 67;
            // 
            // editCategoryName
            // 
            this.editCategoryName.Location = new System.Drawing.Point(81, 10);
            this.editCategoryName.Name = "editCategoryName";
            this.editCategoryName.Size = new System.Drawing.Size(174, 20);
            this.editCategoryName.TabIndex = 6;
            this.editCategoryName.TextChanged += new System.EventHandler(this.editCategoryName_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Category:";
            // 
            // btnSortCategories
            // 
            this.btnSortCategories.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSortCategories.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnSortCategories.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnSortCategories.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSortCategories.Image = null;
            this.btnSortCategories.Location = new System.Drawing.Point(6, 19);
            this.btnSortCategories.Name = "btnSortCategories";
            this.btnSortCategories.Size = new System.Drawing.Size(105, 23);
            this.btnSortCategories.TabIndex = 3;
            this.btnSortCategories.Text = "Sort by Categories";
            this.btnSortCategories.Click += new DecentForms.EventHandler(this.btnSortCategories_Click);
            // 
            // CharacterEditor
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.tabCharacterEditor);
            this.Name = "CharacterEditor";
            this.Size = new System.Drawing.Size(1438, 742);
            ((System.ComponentModel.ISupportInitialize)(this.trackGridOpacity)).EndInit();
            this.tabCharacterEditor.ResumeLayout(false);
            this.tabEditor.ResumeLayout(false);
            this.tabEditor.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picturePlayground)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupRightClick.ResumeLayout(false);
            this.groupMoveChar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.canvasEditor)).EndInit();
            this.flowPlaygroundScale.ResumeLayout(false);
            this.tabCategories.ResumeLayout(false);
            this.tabCategories.PerformLayout();
            this.groupCategorySpecific.ResumeLayout(false);
            this.groupCategorySpecific.PerformLayout();
            this.ResumeLayout(false);

    }

        #endregion
        private GR.Forms.FastPictureBox picturePlayground;
        private DecentForms.Button btnClearChars;
        private DecentForms.Button btnPaste;
        private DecentForms.Button btnCopy;
        private DecentForms.Button btnInvert;
        private DecentForms.Button btnMirrorY;
        private DecentForms.Button btnMirrorX;
        private DecentForms.Button btnShiftDown;
        private DecentForms.Button btnShiftUp;
        private DecentForms.Button btnShiftRight;
        private DecentForms.Button button3;
        private DecentForms.Button btnRotateLeft;
        private DecentForms.Button btnShiftLeft;
        private System.Windows.Forms.ComboBox comboCategories;
        private DecentForms.Button btnPasteFromClipboard;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelCharNo;
        private GR.Forms.ImageListbox panelCharacters;
        private System.Windows.Forms.CheckBox checkShowGrid;
        private System.Windows.Forms.CheckBox checkShowPlaygroundGrid;
        private System.Windows.Forms.TrackBar trackGridOpacity;
        private System.Windows.Forms.CheckBox checkPasteMultiColor;
        private DecentForms.Button btnMoveSelectionToTarget;
        private DecentForms.Button btnCreateTile;
        private System.Windows.Forms.TextBox editMoveTargetIndex;
    private CustomDrawControl canvasEditor;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.TabControl tabCharacterEditor;
    private System.Windows.Forms.TabPage tabEditor;
    private System.Windows.Forms.TabPage tabCategories;
    private System.Windows.Forms.GroupBox groupAllCategories;
    private DecentForms.Button btnSortCategories;
    private System.Windows.Forms.GroupBox groupCategorySpecific;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.TextBox editCollapseIndex;
    private DecentForms.Button btnCollapseCategory;
    private DecentForms.Button btnReseatCategory;
    private DecentForms.Button btnDelete;
    private DecentForms.Button btnAddCategory;
    private System.Windows.Forms.ListView listCategories;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.TextBox editCategoryName;
    private System.Windows.Forms.Label label3;
    private DecentForms.Button btnMoveCategoryDown;
    private DecentForms.Button btnMoveCategoryUp;
    private System.Windows.Forms.ComboBox comboCharsetMode;
    private System.Windows.Forms.Label labelCharsetMode;
    private System.Windows.Forms.Panel panelColorSettings;
    private DecentForms.Button btnHighlightDuplicates;
    private System.Windows.Forms.Panel panelColorChooser;
    private DecentForms.Button btnRemoveDuplicates;
    private System.Windows.Forms.GroupBox groupMoveChar;
    private DecentForms.Button btnCharMoveDown;
    private DecentForms.Button btnCharMoveRight;
    private DecentForms.Button btnCharMoveUp;
    private DecentForms.Button btnCharMoveLeft;
    private DecentForms.Button btnRestoreDefault;

        private System.Windows.Forms.GroupBox groupBox1;
        private DecentForms.Button btnZoomOut;
        private DecentForms.Button btnZoomIn;
        private System.Windows.Forms.Label labelCharUsageCount;
        private System.Windows.Forms.Label labelCharMapUsageCount;
        private System.Windows.Forms.Label labelZoom;
        private System.Windows.Forms.ComboBox comboCharactersPerRow;
        private System.Windows.Forms.Label label1;
        private DecentForms.RadioButton ButtonCanvas4x4;
        private DecentForms.RadioButton ButtonCanvas2x3;
        private DecentForms.RadioButton ButtonCanvas2x2;
        private DecentForms.RadioButton ButtonCanvas1x1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private DecentForms.RadioButton radioPlaygroundScale1x;
        private DecentForms.RadioButton radioPlaygroundScale2x;
        private DecentForms.RadioButton radioPlaygroundScale4x;
        private DecentForms.RadioButton radioPlaygroundScale8x;
        private System.Windows.Forms.FlowLayoutPanel flowPlaygroundScale;
        private DecentForms.Button btnCropPlayground;
        private DecentForms.Button btnClearPlayground;
        private System.Windows.Forms.GroupBox groupRightClick;
        private DecentForms.RadioButton radioRightClickDefault;
        private DecentForms.RadioButton radioRightClickBackground;
        private DecentForms.RadioButton radioRightClickMulticolor1;
        private DecentForms.RadioButton radioRightClickMulticolor2;
        private DecentForms.RadioButton radioRightClickCharColor;
        private System.Windows.Forms.Label labelSwatchSize;
        private System.Windows.Forms.TextBox editSwatchSize;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
    }
}
