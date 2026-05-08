using RetroDevStudio.Controls;



namespace RetroDevStudio.Documents
{
  partial class SpriteEditor
  {
    /// <summary>
    /// Erforderliche Designervariable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Verwendete Ressourcen bereinigen.
    /// </summary>
    /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Vom Komponenten-Designer generierter Code

    /// <summary>
    /// Erforderliche Methode für die Designerunterstützung.
    /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
    /// </summary>
    private void InitializeComponent()
    {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpriteEditor));
            GR.Image.FastImage fastImage1 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage2 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage3 = new GR.Image.FastImage();
            this.tabSpriteEditor = new System.Windows.Forms.TabControl();
            this.tabEditor = new System.Windows.Forms.TabPage();
            this.editMoveTargetIndex = new System.Windows.Forms.TextBox();
            this.btnMoveSelectionToTarget = new DecentForms.Button();
            this.labelSelectionInfo = new System.Windows.Forms.Label();
            this.btnHighlightDuplicates = new DecentForms.Button();
            this.btnChangeMode = new DecentForms.MenuButton();
            this.panelColorSettings = new System.Windows.Forms.Panel();
            this.btnToolEdit = new DecentForms.RadioButton();
            this.btnToolFill = new DecentForms.RadioButton();
            this.label11 = new System.Windows.Forms.Label();
            this.tabSpriteDetails = new System.Windows.Forms.TabControl();
            this.tabPageOverlay = new System.Windows.Forms.TabPage();
            this.btnAddOverlay = new DecentForms.Button();
            this.btnRemoveOverlay = new DecentForms.Button();
            this.listOverlays = new System.Windows.Forms.ListBox();
            this.labelOverlayName = new System.Windows.Forms.Label();
            this.editOverlayName = new System.Windows.Forms.TextBox();
            this.panelOverlaySlots = new System.Windows.Forms.Panel();
            this.picOverlayPreview = new GR.Forms.FastPictureBox();
            this.btnOverlayZoomOut = new DecentForms.Button();
            this.btnOverlayZoomIn = new DecentForms.Button();
            this.labelOverlayZoom = new System.Windows.Forms.Label();
            this.panelSprites = new GR.Forms.ImageListbox();
            this.tabPageAnimation = new System.Windows.Forms.TabPage();
            this.btnAddFrame = new DecentForms.Button();
            this.btnRemoveFrame = new DecentForms.Button();
            this.btnDuplicateFrame = new DecentForms.Button();
            this.btnPlayAnim = new DecentForms.Button();
            this.btnPauseAnim = new DecentForms.Button();
            this.listAnimFrames = new System.Windows.Forms.ListBox();
            this.labelFrameDelay = new System.Windows.Forms.Label();
            this.editFrameDelay = new System.Windows.Forms.NumericUpDown();
            this.btnApplyDelayToAll = new DecentForms.Button();
            this.panelFrameSlots = new System.Windows.Forms.Panel();
            this.picAnimPreview = new GR.Forms.FastPictureBox();
            this.btnAnimZoomOut = new DecentForms.Button();
            this.btnAnimZoomIn = new DecentForms.Button();
            this.labelAnimZoom = new System.Windows.Forms.Label();
            this.btnClearSprite = new DecentForms.Button();
            this.btnDeleteSprite = new DecentForms.Button();
            this.btnBankCopy = new DecentForms.Button();
            this.btnBankPaste = new DecentForms.Button();
            this.btnInvert = new DecentForms.Button();
            this.btnMirrorY = new DecentForms.Button();
            this.btnMirrorX = new DecentForms.Button();
            this.btnShiftDown = new DecentForms.Button();
            this.btnShiftUp = new DecentForms.Button();
            this.btnShiftRight = new DecentForms.Button();
            this.btnRotateRight = new DecentForms.Button();
            this.btnRotateLeft = new DecentForms.Button();
            this.btnShiftLeft = new DecentForms.Button();
            this.btnCopyToClipboard = new DecentForms.Button();
            this.btnPasteFromClipboard = new DecentForms.Button();
            this.labelCharNo = new System.Windows.Forms.Label();
            this.checkShowGrid = new System.Windows.Forms.CheckBox();
            this.pictureEditor = new GR.Forms.FastPictureBox();
            this.tabExport = new System.Windows.Forms.TabPage();
            this.labelCharactersTo = new System.Windows.Forms.Label();
            this.editDataExport = new System.Windows.Forms.TextBox();
            this.btnExport = new DecentForms.Button();
            this.comboExportMethod = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.panelExport = new System.Windows.Forms.Panel();
            this.comboExportRange = new System.Windows.Forms.ComboBox();
            this.editSpriteCount = new System.Windows.Forms.TextBox();
            this.editSpriteFrom = new System.Windows.Forms.TextBox();
            this.labelCharactersFrom = new System.Windows.Forms.Label();
            this.tabImport = new System.Windows.Forms.TabPage();
            this.panelImport = new System.Windows.Forms.Panel();
            this.btnImport = new DecentForms.Button();
            this.comboImportMethod = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSpriteProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.contextMenuChangeMode = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.c64HiResMultiColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mega65ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mega65_24x214ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mega65_64x214ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mega65_16x2116ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commanderX16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_256ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_8x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_16x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_32x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.x16_64x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).BeginInit();
            this.tabSpriteEditor.SuspendLayout();
            this.tabEditor.SuspendLayout();
            this.tabSpriteDetails.SuspendLayout();
            this.tabPageOverlay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlayPreview)).BeginInit();
            this.tabPageAnimation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editFrameDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAnimPreview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditor)).BeginInit();
            this.tabExport.SuspendLayout();
            this.tabImport.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.contextMenuChangeMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabSpriteEditor
            // 
            this.tabSpriteEditor.Controls.Add(this.tabEditor);
            this.tabSpriteEditor.Controls.Add(this.tabExport);
            this.tabSpriteEditor.Controls.Add(this.tabImport);
            this.tabSpriteEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSpriteEditor.Location = new System.Drawing.Point(0, 24);
            this.tabSpriteEditor.Name = "tabSpriteEditor";
            this.tabSpriteEditor.SelectedIndex = 0;
            this.tabSpriteEditor.Size = new System.Drawing.Size(987, 665);
            this.tabSpriteEditor.TabIndex = 0;
            // 
            // tabEditor
            // 
            this.tabEditor.Controls.Add(this.editMoveTargetIndex);
            this.tabEditor.Controls.Add(this.btnMoveSelectionToTarget);
            this.tabEditor.Controls.Add(this.labelSelectionInfo);
            this.tabEditor.Controls.Add(this.btnHighlightDuplicates);
            this.tabEditor.Controls.Add(this.btnChangeMode);
            this.tabEditor.Controls.Add(this.panelColorSettings);
            this.tabEditor.Controls.Add(this.btnToolEdit);
            this.tabEditor.Controls.Add(this.btnToolFill);
            this.tabEditor.Controls.Add(this.label11);
            this.tabEditor.Controls.Add(this.tabSpriteDetails);
            this.tabEditor.Controls.Add(this.btnClearSprite);
            this.tabEditor.Controls.Add(this.btnDeleteSprite);
            this.tabEditor.Controls.Add(this.btnBankCopy);
            this.tabEditor.Controls.Add(this.btnBankPaste);
            this.tabEditor.Controls.Add(this.btnInvert);
            this.tabEditor.Controls.Add(this.btnMirrorY);
            this.tabEditor.Controls.Add(this.btnMirrorX);
            this.tabEditor.Controls.Add(this.btnShiftDown);
            this.tabEditor.Controls.Add(this.btnShiftUp);
            this.tabEditor.Controls.Add(this.btnShiftRight);
            this.tabEditor.Controls.Add(this.btnRotateRight);
            this.tabEditor.Controls.Add(this.btnRotateLeft);
            this.tabEditor.Controls.Add(this.btnShiftLeft);
            this.tabEditor.Controls.Add(this.btnCopyToClipboard);
            this.tabEditor.Controls.Add(this.btnPasteFromClipboard);
            this.tabEditor.Controls.Add(this.labelCharNo);
            this.tabEditor.Controls.Add(this.checkShowGrid);
            this.tabEditor.Controls.Add(this.pictureEditor);
            this.tabEditor.Location = new System.Drawing.Point(4, 22);
            this.tabEditor.Name = "tabEditor";
            this.tabEditor.Padding = new System.Windows.Forms.Padding(3);
            this.tabEditor.Size = new System.Drawing.Size(979, 639);
            this.tabEditor.TabIndex = 0;
            this.tabEditor.Text = "Sprite";
            this.tabEditor.UseVisualStyleBackColor = true;
            // 
            // editMoveTargetIndex
            // 
            this.editMoveTargetIndex.Location = new System.Drawing.Point(396, 570);
            this.editMoveTargetIndex.Name = "editMoveTargetIndex";
            this.editMoveTargetIndex.Size = new System.Drawing.Size(73, 20);
            this.editMoveTargetIndex.TabIndex = 0;
            // 
            // btnMoveSelectionToTarget
            // 
            this.btnMoveSelectionToTarget.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveSelectionToTarget.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveSelectionToTarget.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveSelectionToTarget.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveSelectionToTarget.Image = null;
            this.btnMoveSelectionToTarget.Location = new System.Drawing.Point(269, 568);
            this.btnMoveSelectionToTarget.Name = "btnMoveSelectionToTarget";
            this.btnMoveSelectionToTarget.Size = new System.Drawing.Size(121, 23);
            this.btnMoveSelectionToTarget.TabIndex = 1;
            this.btnMoveSelectionToTarget.Text = "Move to Index";
            this.btnMoveSelectionToTarget.Click += new DecentForms.EventHandler(this.btnMoveSelectionToTarget_Click);
            // 
            // labelSelectionInfo
            // 
            this.labelSelectionInfo.Location = new System.Drawing.Point(352, 471);
            this.labelSelectionInfo.Name = "labelSelectionInfo";
            this.labelSelectionInfo.Size = new System.Drawing.Size(122, 23);
            this.labelSelectionInfo.TabIndex = 62;
            this.labelSelectionInfo.Text = "label1";
            // 
            // btnHighlightDuplicates
            // 
            this.btnHighlightDuplicates.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnHighlightDuplicates.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnHighlightDuplicates.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnHighlightDuplicates.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnHighlightDuplicates.Image = null;
            this.btnHighlightDuplicates.Location = new System.Drawing.Point(269, 539);
            this.btnHighlightDuplicates.Name = "btnHighlightDuplicates";
            this.btnHighlightDuplicates.Size = new System.Drawing.Size(121, 23);
            this.btnHighlightDuplicates.TabIndex = 61;
            this.btnHighlightDuplicates.Text = "Duplicates";
            this.btnHighlightDuplicates.Click += new DecentForms.EventHandler(this.btnHighlightDuplicates_Click);
            // 
            // btnChangeMode
            // 
            this.btnChangeMode.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnChangeMode.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnChangeMode.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnChangeMode.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnChangeMode.Image = null;
            this.btnChangeMode.Location = new System.Drawing.Point(269, 510);
            this.btnChangeMode.Name = "btnChangeMode";
            this.btnChangeMode.Size = new System.Drawing.Size(205, 23);
            this.btnChangeMode.TabIndex = 60;
            this.btnChangeMode.Text = "btnChangeMode";
            this.btnChangeMode.Click += new DecentForms.EventHandler(this.btnChangeMode_Click);
            // 
            // panelColorSettings
            // 
            this.panelColorSettings.Location = new System.Drawing.Point(40, 365);
            this.panelColorSettings.Name = "panelColorSettings";
            this.panelColorSettings.Size = new System.Drawing.Size(220, 210);
            this.panelColorSettings.TabIndex = 59;
            // 
            // btnToolEdit
            // 
            this.btnToolEdit.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolEdit.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolEdit.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolEdit.Checked = true;
            this.btnToolEdit.Image = ((System.Drawing.Image)(resources.GetObject("btnToolEdit.Image")));
            this.btnToolEdit.Location = new System.Drawing.Point(8, 285);
            this.btnToolEdit.Name = "btnToolEdit";
            this.btnToolEdit.Size = new System.Drawing.Size(26, 26);
            this.btnToolEdit.TabIndex = 9;
            this.toolTip1.SetToolTip(this.btnToolEdit, "Single Character");
            this.btnToolEdit.CheckedChanged += new DecentForms.EventHandler(this.btnToolEdit_CheckedChanged);
            // 
            // btnToolFill
            // 
            this.btnToolFill.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolFill.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolFill.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolFill.Checked = false;
            this.btnToolFill.Image = ((System.Drawing.Image)(resources.GetObject("btnToolFill.Image")));
            this.btnToolFill.Location = new System.Drawing.Point(8, 316);
            this.btnToolFill.Name = "btnToolFill";
            this.btnToolFill.Size = new System.Drawing.Size(26, 26);
            this.btnToolFill.TabIndex = 10;
            this.toolTip1.SetToolTip(this.btnToolFill, "Fill");
            this.btnToolFill.CheckedChanged += new DecentForms.EventHandler(this.btnToolFill_CheckedChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(266, 494);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(37, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "Mode:";
            // 
            // tabSpriteDetails
            // 
            this.tabSpriteDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabSpriteDetails.Controls.Add(this.tabPageOverlay);
            this.tabSpriteDetails.Controls.Add(this.tabPageAnimation);
            this.tabSpriteDetails.Location = new System.Drawing.Point(480, 2);
            this.tabSpriteDetails.Name = "tabSpriteDetails";
            this.tabSpriteDetails.SelectedIndex = 0;
            this.tabSpriteDetails.Size = new System.Drawing.Size(499, 563);
            this.tabSpriteDetails.TabIndex = 18;
            // 
            // tabPageOverlay
            // 
            this.tabPageOverlay.Controls.Add(this.btnAddOverlay);
            this.tabPageOverlay.Controls.Add(this.btnRemoveOverlay);
            this.tabPageOverlay.Controls.Add(this.listOverlays);
            this.tabPageOverlay.Controls.Add(this.labelOverlayName);
            this.tabPageOverlay.Controls.Add(this.editOverlayName);
            this.tabPageOverlay.Controls.Add(this.panelOverlaySlots);
            this.tabPageOverlay.Controls.Add(this.picOverlayPreview);
            this.tabPageOverlay.Controls.Add(this.btnOverlayZoomOut);
            this.tabPageOverlay.Controls.Add(this.btnOverlayZoomIn);
            this.tabPageOverlay.Controls.Add(this.labelOverlayZoom);
            this.tabPageOverlay.Controls.Add(this.panelSprites);
            this.tabPageOverlay.Location = new System.Drawing.Point(4, 22);
            this.tabPageOverlay.Name = "tabPageOverlay";
            this.tabPageOverlay.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOverlay.Size = new System.Drawing.Size(491, 537);
            this.tabPageOverlay.TabIndex = 2;
            this.tabPageOverlay.Text = "Overlay";
            this.tabPageOverlay.UseVisualStyleBackColor = true;
            // 
            // btnAddOverlay
            // 
            this.btnAddOverlay.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddOverlay.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAddOverlay.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAddOverlay.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddOverlay.Image = null;
            this.btnAddOverlay.Location = new System.Drawing.Point(5, 5);
            this.btnAddOverlay.Name = "btnAddOverlay";
            this.btnAddOverlay.Size = new System.Drawing.Size(60, 23);
            this.btnAddOverlay.TabIndex = 0;
            this.btnAddOverlay.Text = "Add";
            this.btnAddOverlay.Click += new DecentForms.EventHandler(this.btnAddOverlay_Click);
            // 
            // btnRemoveOverlay
            // 
            this.btnRemoveOverlay.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRemoveOverlay.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRemoveOverlay.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRemoveOverlay.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRemoveOverlay.Enabled = false;
            this.btnRemoveOverlay.Image = null;
            this.btnRemoveOverlay.Location = new System.Drawing.Point(68, 5);
            this.btnRemoveOverlay.Name = "btnRemoveOverlay";
            this.btnRemoveOverlay.Size = new System.Drawing.Size(60, 23);
            this.btnRemoveOverlay.TabIndex = 1;
            this.btnRemoveOverlay.Text = "Remove";
            this.btnRemoveOverlay.Click += new DecentForms.EventHandler(this.btnRemoveOverlay_Click);
            // 
            // listOverlays
            // 
            this.listOverlays.FormattingEnabled = true;
            this.listOverlays.IntegralHeight = false;
            this.listOverlays.Location = new System.Drawing.Point(5, 33);
            this.listOverlays.Name = "listOverlays";
            this.listOverlays.Size = new System.Drawing.Size(130, 470);
            this.listOverlays.TabIndex = 2;
            this.listOverlays.SelectedIndexChanged += new System.EventHandler(this.listOverlays_SelectedIndexChanged);
            // 
            // labelOverlayName
            // 
            this.labelOverlayName.AutoSize = true;
            this.labelOverlayName.Location = new System.Drawing.Point(140, 9);
            this.labelOverlayName.Name = "labelOverlayName";
            this.labelOverlayName.Size = new System.Drawing.Size(38, 13);
            this.labelOverlayName.TabIndex = 3;
            this.labelOverlayName.Text = "Name:";
            // 
            // editOverlayName
            // 
            this.editOverlayName.Location = new System.Drawing.Point(180, 6);
            this.editOverlayName.Name = "editOverlayName";
            this.editOverlayName.Size = new System.Drawing.Size(200, 20);
            this.editOverlayName.TabIndex = 4;
            this.editOverlayName.TextChanged += new System.EventHandler(this.editOverlayName_TextChanged);
            // 
            // panelOverlaySlots
            // 
            this.panelOverlaySlots.Location = new System.Drawing.Point(140, 33);
            this.panelOverlaySlots.Name = "panelOverlaySlots";
            this.panelOverlaySlots.Size = new System.Drawing.Size(280, 180);
            this.panelOverlaySlots.TabIndex = 5;
            // 
            // picOverlayPreview
            // 
            this.picOverlayPreview.AutoResize = false;
            this.picOverlayPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOverlayPreview.DisplayPage = fastImage1;
            this.picOverlayPreview.Image = null;
            this.picOverlayPreview.Location = new System.Drawing.Point(140, 220);
            this.picOverlayPreview.Name = "picOverlayPreview";
            this.picOverlayPreview.Size = new System.Drawing.Size(280, 240);
            this.picOverlayPreview.TabIndex = 6;
            this.picOverlayPreview.TabStop = false;
            // 
            // btnOverlayZoomOut
            // 
            this.btnOverlayZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnOverlayZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnOverlayZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnOverlayZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOverlayZoomOut.Image = null;
            this.btnOverlayZoomOut.Location = new System.Drawing.Point(140, 465);
            this.btnOverlayZoomOut.Name = "btnOverlayZoomOut";
            this.btnOverlayZoomOut.Size = new System.Drawing.Size(30, 23);
            this.btnOverlayZoomOut.TabIndex = 7;
            this.btnOverlayZoomOut.Text = "-";
            this.toolTip1.SetToolTip(this.btnOverlayZoomOut, "Zoom out");
            this.btnOverlayZoomOut.Click += new DecentForms.EventHandler(this.btnOverlayZoomOut_Click);
            // 
            // btnOverlayZoomIn
            // 
            this.btnOverlayZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnOverlayZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnOverlayZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnOverlayZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOverlayZoomIn.Image = null;
            this.btnOverlayZoomIn.Location = new System.Drawing.Point(175, 465);
            this.btnOverlayZoomIn.Name = "btnOverlayZoomIn";
            this.btnOverlayZoomIn.Size = new System.Drawing.Size(30, 23);
            this.btnOverlayZoomIn.TabIndex = 8;
            this.btnOverlayZoomIn.Text = "+";
            this.toolTip1.SetToolTip(this.btnOverlayZoomIn, "Zoom in");
            this.btnOverlayZoomIn.Click += new DecentForms.EventHandler(this.btnOverlayZoomIn_Click);
            // 
            // labelOverlayZoom
            // 
            this.labelOverlayZoom.AutoSize = true;
            this.labelOverlayZoom.Location = new System.Drawing.Point(215, 470);
            this.labelOverlayZoom.Name = "labelOverlayZoom";
            this.labelOverlayZoom.Size = new System.Drawing.Size(51, 13);
            this.labelOverlayZoom.TabIndex = 9;
            this.labelOverlayZoom.Text = "Zoom: 1x";
            // 
            // panelSprites
            // 
            this.panelSprites.AllowPopup = false;
            this.panelSprites.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelSprites.AutoScroll = true;
            this.panelSprites.AutoScrollHorizontalMaximum = 100;
            this.panelSprites.AutoScrollHorizontalMinimum = 0;
            this.panelSprites.AutoScrollHPos = 0;
            this.panelSprites.AutoScrollVerticalMaximum = -23;
            this.panelSprites.AutoScrollVerticalMinimum = 0;
            this.panelSprites.AutoScrollVPos = 0;
            this.panelSprites.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSprites.EnableAutoScrollHorizontal = false;
            this.panelSprites.EnableAutoScrollVertical = true;
            this.panelSprites.GridColor = System.Drawing.SystemColors.ControlDark;
            this.panelSprites.HottrackColor = ((uint)(2151694591u));
            this.panelSprites.ItemHeight = 21;
            this.panelSprites.ItemWidth = 24;
            this.panelSprites.Location = new System.Drawing.Point(425, 33);
            this.panelSprites.Name = "panelSprites";
            this.panelSprites.PixelFormat = GR.Drawing.PixelFormat.DontCare;
            this.panelSprites.SelectedIndex = -1;
            this.panelSprites.ShowGrid = false;
            this.panelSprites.Size = new System.Drawing.Size(65, 477);
            this.panelSprites.TabIndex = 4;
            this.panelSprites.TabStop = true;
            this.panelSprites.VisibleAutoScrollHorizontal = false;
            this.panelSprites.VisibleAutoScrollVertical = false;
            this.panelSprites.SelectedIndexChanged += new System.EventHandler(this.panelSprites_SelectedIndexChanged);
            this.panelSprites.SelectionChanged += new System.EventHandler(this.panelSprites_SelectionChanged);
            this.panelSprites.ClientSizeChanged += new System.EventHandler(this.panelSprites_ClientSizeChanged);
            // 
            // tabPageAnimation
            // 
            this.tabPageAnimation.Controls.Add(this.btnAddFrame);
            this.tabPageAnimation.Controls.Add(this.btnRemoveFrame);
            this.tabPageAnimation.Controls.Add(this.btnDuplicateFrame);
            this.tabPageAnimation.Controls.Add(this.btnPlayAnim);
            this.tabPageAnimation.Controls.Add(this.btnPauseAnim);
            this.tabPageAnimation.Controls.Add(this.listAnimFrames);
            this.tabPageAnimation.Controls.Add(this.labelFrameDelay);
            this.tabPageAnimation.Controls.Add(this.editFrameDelay);
            this.tabPageAnimation.Controls.Add(this.btnApplyDelayToAll);
            this.tabPageAnimation.Controls.Add(this.panelFrameSlots);
            this.tabPageAnimation.Controls.Add(this.picAnimPreview);
            this.tabPageAnimation.Controls.Add(this.btnAnimZoomOut);
            this.tabPageAnimation.Controls.Add(this.btnAnimZoomIn);
            this.tabPageAnimation.Controls.Add(this.labelAnimZoom);
            this.tabPageAnimation.Location = new System.Drawing.Point(4, 22);
            this.tabPageAnimation.Name = "tabPageAnimation";
            this.tabPageAnimation.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAnimation.Size = new System.Drawing.Size(491, 537);
            this.tabPageAnimation.TabIndex = 3;
            this.tabPageAnimation.Text = "Animation";
            this.tabPageAnimation.UseVisualStyleBackColor = true;
            // 
            // btnAddFrame
            // 
            this.btnAddFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAddFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAddFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddFrame.Image = null;
            this.btnAddFrame.Location = new System.Drawing.Point(5, 5);
            this.btnAddFrame.Name = "btnAddFrame";
            this.btnAddFrame.Size = new System.Drawing.Size(60, 23);
            this.btnAddFrame.TabIndex = 0;
            this.btnAddFrame.Text = "Add";
            this.btnAddFrame.Click += new DecentForms.EventHandler(this.btnAddFrame_Click);
            // 
            // btnRemoveFrame
            // 
            this.btnRemoveFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRemoveFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRemoveFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRemoveFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRemoveFrame.Enabled = false;
            this.btnRemoveFrame.Image = null;
            this.btnRemoveFrame.Location = new System.Drawing.Point(68, 5);
            this.btnRemoveFrame.Name = "btnRemoveFrame";
            this.btnRemoveFrame.Size = new System.Drawing.Size(60, 23);
            this.btnRemoveFrame.TabIndex = 1;
            this.btnRemoveFrame.Text = "Remove";
            this.btnRemoveFrame.Click += new DecentForms.EventHandler(this.btnRemoveFrame_Click);
            // 
            // btnDuplicateFrame
            // 
            this.btnDuplicateFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDuplicateFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnDuplicateFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnDuplicateFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDuplicateFrame.Enabled = false;
            this.btnDuplicateFrame.Image = null;
            this.btnDuplicateFrame.Location = new System.Drawing.Point(131, 5);
            this.btnDuplicateFrame.Name = "btnDuplicateFrame";
            this.btnDuplicateFrame.Size = new System.Drawing.Size(80, 23);
            this.btnDuplicateFrame.TabIndex = 2;
            this.btnDuplicateFrame.Text = "Duplicate";
            this.btnDuplicateFrame.Click += new DecentForms.EventHandler(this.btnDuplicateFrame_Click);
            // 
            // btnPlayAnim
            // 
            this.btnPlayAnim.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPlayAnim.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnPlayAnim.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnPlayAnim.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPlayAnim.Image = null;
            this.btnPlayAnim.Location = new System.Drawing.Point(340, 5);
            this.btnPlayAnim.Name = "btnPlayAnim";
            this.btnPlayAnim.Size = new System.Drawing.Size(60, 23);
            this.btnPlayAnim.TabIndex = 3;
            this.btnPlayAnim.Text = "Play";
            this.btnPlayAnim.Click += new DecentForms.EventHandler(this.btnPlayAnim_Click);
            // 
            // btnPauseAnim
            // 
            this.btnPauseAnim.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPauseAnim.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnPauseAnim.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnPauseAnim.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPauseAnim.Enabled = false;
            this.btnPauseAnim.Image = null;
            this.btnPauseAnim.Location = new System.Drawing.Point(403, 5);
            this.btnPauseAnim.Name = "btnPauseAnim";
            this.btnPauseAnim.Size = new System.Drawing.Size(60, 23);
            this.btnPauseAnim.TabIndex = 4;
            this.btnPauseAnim.Text = "Pause";
            this.btnPauseAnim.Click += new DecentForms.EventHandler(this.btnPauseAnim_Click);
            // 
            // listAnimFrames
            // 
            this.listAnimFrames.FormattingEnabled = true;
            this.listAnimFrames.IntegralHeight = false;
            this.listAnimFrames.Location = new System.Drawing.Point(5, 33);
            this.listAnimFrames.Name = "listAnimFrames";
            this.listAnimFrames.Size = new System.Drawing.Size(130, 300);
            this.listAnimFrames.TabIndex = 5;
            this.listAnimFrames.SelectedIndexChanged += new System.EventHandler(this.listAnimFrames_SelectedIndexChanged);
            // 
            // labelFrameDelay
            // 
            this.labelFrameDelay.AutoSize = true;
            this.labelFrameDelay.Location = new System.Drawing.Point(140, 38);
            this.labelFrameDelay.Name = "labelFrameDelay";
            this.labelFrameDelay.Size = new System.Drawing.Size(59, 13);
            this.labelFrameDelay.TabIndex = 6;
            this.labelFrameDelay.Text = "Delay (ms):";
            // 
            // editFrameDelay
            // 
            this.editFrameDelay.Location = new System.Drawing.Point(210, 35);
            this.editFrameDelay.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.editFrameDelay.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.editFrameDelay.Name = "editFrameDelay";
            this.editFrameDelay.Size = new System.Drawing.Size(80, 20);
            this.editFrameDelay.TabIndex = 7;
            this.editFrameDelay.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.editFrameDelay.ValueChanged += new System.EventHandler(this.editFrameDelay_ValueChanged);
            // 
            // btnApplyDelayToAll
            // 
            this.btnApplyDelayToAll.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnApplyDelayToAll.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnApplyDelayToAll.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnApplyDelayToAll.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnApplyDelayToAll.Image = null;
            this.btnApplyDelayToAll.Location = new System.Drawing.Point(295, 33);
            this.btnApplyDelayToAll.Name = "btnApplyDelayToAll";
            this.btnApplyDelayToAll.Size = new System.Drawing.Size(100, 23);
            this.btnApplyDelayToAll.TabIndex = 10;
            this.btnApplyDelayToAll.Text = "Apply to all";
            this.toolTip1.SetToolTip(this.btnApplyDelayToAll, "Set every frame in the current overlay to the delay shown above.");
            this.btnApplyDelayToAll.Click += new DecentForms.EventHandler(this.btnApplyDelayToAll_Click);
            // 
            // panelFrameSlots
            // 
            this.panelFrameSlots.Location = new System.Drawing.Point(140, 65);
            this.panelFrameSlots.Name = "panelFrameSlots";
            this.panelFrameSlots.Size = new System.Drawing.Size(200, 180);
            this.panelFrameSlots.TabIndex = 8;
            // 
            // picAnimPreview
            // 
            this.picAnimPreview.AutoResize = false;
            this.picAnimPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picAnimPreview.DisplayPage = fastImage2;
            this.picAnimPreview.Image = null;
            this.picAnimPreview.Location = new System.Drawing.Point(140, 260);
            this.picAnimPreview.Name = "picAnimPreview";
            this.picAnimPreview.Size = new System.Drawing.Size(320, 240);
            this.picAnimPreview.TabIndex = 9;
            this.picAnimPreview.TabStop = false;
            // 
            // btnAnimZoomOut
            // 
            this.btnAnimZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAnimZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAnimZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAnimZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAnimZoomOut.Image = null;
            this.btnAnimZoomOut.Location = new System.Drawing.Point(140, 505);
            this.btnAnimZoomOut.Name = "btnAnimZoomOut";
            this.btnAnimZoomOut.Size = new System.Drawing.Size(30, 23);
            this.btnAnimZoomOut.TabIndex = 11;
            this.btnAnimZoomOut.Text = "-";
            this.toolTip1.SetToolTip(this.btnAnimZoomOut, "Zoom out");
            this.btnAnimZoomOut.Click += new DecentForms.EventHandler(this.btnAnimZoomOut_Click);
            // 
            // btnAnimZoomIn
            // 
            this.btnAnimZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAnimZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAnimZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAnimZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAnimZoomIn.Image = null;
            this.btnAnimZoomIn.Location = new System.Drawing.Point(175, 505);
            this.btnAnimZoomIn.Name = "btnAnimZoomIn";
            this.btnAnimZoomIn.Size = new System.Drawing.Size(30, 23);
            this.btnAnimZoomIn.TabIndex = 12;
            this.btnAnimZoomIn.Text = "+";
            this.toolTip1.SetToolTip(this.btnAnimZoomIn, "Zoom in");
            this.btnAnimZoomIn.Click += new DecentForms.EventHandler(this.btnAnimZoomIn_Click);
            // 
            // labelAnimZoom
            // 
            this.labelAnimZoom.AutoSize = true;
            this.labelAnimZoom.Location = new System.Drawing.Point(215, 510);
            this.labelAnimZoom.Name = "labelAnimZoom";
            this.labelAnimZoom.Size = new System.Drawing.Size(51, 13);
            this.labelAnimZoom.TabIndex = 13;
            this.labelAnimZoom.Text = "Zoom: 1x";
            // 
            // btnClearSprite
            // 
            this.btnClearSprite.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnClearSprite.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnClearSprite.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnClearSprite.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClearSprite.Enabled = false;
            this.btnClearSprite.Image = null;
            this.btnClearSprite.Location = new System.Drawing.Point(269, 445);
            this.btnClearSprite.Name = "btnClearSprite";
            this.btnClearSprite.Size = new System.Drawing.Size(57, 23);
            this.btnClearSprite.TabIndex = 14;
            this.btnClearSprite.Text = "Clear";
            this.btnClearSprite.Click += new DecentForms.EventHandler(this.btnClearSprite_Click);
            // 
            // btnDeleteSprite
            // 
            this.btnDeleteSprite.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDeleteSprite.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnDeleteSprite.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnDeleteSprite.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDeleteSprite.Enabled = false;
            this.btnDeleteSprite.Image = null;
            this.btnDeleteSprite.Location = new System.Drawing.Point(333, 445);
            this.btnDeleteSprite.Name = "btnDeleteSprite";
            this.btnDeleteSprite.Size = new System.Drawing.Size(57, 23);
            this.btnDeleteSprite.TabIndex = 15;
            this.btnDeleteSprite.Text = "Delete";
            this.btnDeleteSprite.Click += new DecentForms.EventHandler(this.btnDeleteSprite_Click);
            // 
            // btnBankCopy
            // 
            this.btnBankCopy.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnBankCopy.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnBankCopy.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnBankCopy.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnBankCopy.Enabled = false;
            this.btnBankCopy.Image = null;
            this.btnBankCopy.Location = new System.Drawing.Point(269, 595);
            this.btnBankCopy.Name = "btnBankCopy";
            this.btnBankCopy.Size = new System.Drawing.Size(57, 23);
            this.btnBankCopy.TabIndex = 63;
            this.btnBankCopy.Text = "Bank Cp";
            this.toolTip1.SetToolTip(this.btnBankCopy, "Copy the selected bank sprites to an in-process clipboard.");
            this.btnBankCopy.Click += new DecentForms.EventHandler(this.btnBankCopy_Click);
            // 
            // btnBankPaste
            // 
            this.btnBankPaste.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnBankPaste.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnBankPaste.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnBankPaste.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnBankPaste.Enabled = false;
            this.btnBankPaste.Image = null;
            this.btnBankPaste.Location = new System.Drawing.Point(333, 595);
            this.btnBankPaste.Name = "btnBankPaste";
            this.btnBankPaste.Size = new System.Drawing.Size(57, 23);
            this.btnBankPaste.TabIndex = 64;
            this.btnBankPaste.Text = "Bank Pst";
            this.toolTip1.SetToolTip(this.btnBankPaste, "Paste the in-process clipboard onto the bank starting at the current selection.");
            this.btnBankPaste.Click += new DecentForms.EventHandler(this.btnBankPaste_Click);
            // 
            // btnInvert
            // 
            this.btnInvert.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnInvert.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnInvert.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnInvert.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnInvert.Image = ((System.Drawing.Image)(resources.GetObject("btnInvert.Image")));
            this.btnInvert.Location = new System.Drawing.Point(8, 192);
            this.btnInvert.Name = "btnInvert";
            this.btnInvert.Size = new System.Drawing.Size(26, 26);
            this.btnInvert.TabIndex = 6;
            this.toolTip1.SetToolTip(this.btnInvert, "Invert selected sprites colors");
            this.btnInvert.Click += new DecentForms.EventHandler(this.btnInvert_Click);
            // 
            // btnMirrorY
            // 
            this.btnMirrorY.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMirrorY.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMirrorY.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMirrorY.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMirrorY.Image = ((System.Drawing.Image)(resources.GetObject("btnMirrorY.Image")));
            this.btnMirrorY.Location = new System.Drawing.Point(8, 161);
            this.btnMirrorY.Name = "btnMirrorY";
            this.btnMirrorY.Size = new System.Drawing.Size(26, 26);
            this.btnMirrorY.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btnMirrorY, "Mirror selected sprites vertically");
            this.btnMirrorY.Click += new DecentForms.EventHandler(this.btnMirrorY_Click);
            // 
            // btnMirrorX
            // 
            this.btnMirrorX.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMirrorX.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMirrorX.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMirrorX.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMirrorX.Image = ((System.Drawing.Image)(resources.GetObject("btnMirrorX.Image")));
            this.btnMirrorX.Location = new System.Drawing.Point(8, 130);
            this.btnMirrorX.Name = "btnMirrorX";
            this.btnMirrorX.Size = new System.Drawing.Size(26, 26);
            this.btnMirrorX.TabIndex = 4;
            this.toolTip1.SetToolTip(this.btnMirrorX, "Mirror selected sprites horizontally");
            this.btnMirrorX.Click += new DecentForms.EventHandler(this.btnMirrorX_Click);
            // 
            // btnShiftDown
            // 
            this.btnShiftDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftDown.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftDown.Image")));
            this.btnShiftDown.Location = new System.Drawing.Point(8, 99);
            this.btnShiftDown.Name = "btnShiftDown";
            this.btnShiftDown.Size = new System.Drawing.Size(26, 26);
            this.btnShiftDown.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnShiftDown, "Shift selected sprites down");
            this.btnShiftDown.Click += new DecentForms.EventHandler(this.btnShiftDown_Click);
            // 
            // btnShiftUp
            // 
            this.btnShiftUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftUp.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftUp.Image")));
            this.btnShiftUp.Location = new System.Drawing.Point(8, 68);
            this.btnShiftUp.Name = "btnShiftUp";
            this.btnShiftUp.Size = new System.Drawing.Size(26, 26);
            this.btnShiftUp.TabIndex = 2;
            this.toolTip1.SetToolTip(this.btnShiftUp, "Shift selected sprites up");
            this.btnShiftUp.Click += new DecentForms.EventHandler(this.btnShiftUp_Click);
            // 
            // btnShiftRight
            // 
            this.btnShiftRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftRight.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftRight.Image")));
            this.btnShiftRight.Location = new System.Drawing.Point(8, 37);
            this.btnShiftRight.Name = "btnShiftRight";
            this.btnShiftRight.Size = new System.Drawing.Size(26, 26);
            this.btnShiftRight.TabIndex = 1;
            this.toolTip1.SetToolTip(this.btnShiftRight, "Shift selected sprites right");
            this.btnShiftRight.Click += new DecentForms.EventHandler(this.btnShiftRight_Click);
            // 
            // btnRotateRight
            // 
            this.btnRotateRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRotateRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRotateRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRotateRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRotateRight.Image = ((System.Drawing.Image)(resources.GetObject("btnRotateRight.Image")));
            this.btnRotateRight.Location = new System.Drawing.Point(8, 254);
            this.btnRotateRight.Name = "btnRotateRight";
            this.btnRotateRight.Size = new System.Drawing.Size(26, 26);
            this.btnRotateRight.TabIndex = 8;
            this.toolTip1.SetToolTip(this.btnRotateRight, "Rotate selected sprites right");
            this.btnRotateRight.Click += new DecentForms.EventHandler(this.btnRotateRight_Click);
            // 
            // btnRotateLeft
            // 
            this.btnRotateLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRotateLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRotateLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRotateLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRotateLeft.Image = ((System.Drawing.Image)(resources.GetObject("btnRotateLeft.Image")));
            this.btnRotateLeft.Location = new System.Drawing.Point(8, 223);
            this.btnRotateLeft.Name = "btnRotateLeft";
            this.btnRotateLeft.Size = new System.Drawing.Size(26, 26);
            this.btnRotateLeft.TabIndex = 7;
            this.toolTip1.SetToolTip(this.btnRotateLeft, "Rotate selected sprites left");
            this.btnRotateLeft.Click += new DecentForms.EventHandler(this.btnRotateLeft_Click);
            // 
            // btnShiftLeft
            // 
            this.btnShiftLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftLeft.Image = ((System.Drawing.Image)(resources.GetObject("btnShiftLeft.Image")));
            this.btnShiftLeft.Location = new System.Drawing.Point(8, 6);
            this.btnShiftLeft.Name = "btnShiftLeft";
            this.btnShiftLeft.Size = new System.Drawing.Size(26, 26);
            this.btnShiftLeft.TabIndex = 0;
            this.toolTip1.SetToolTip(this.btnShiftLeft, "Shift selected sprites left");
            this.btnShiftLeft.Click += new DecentForms.EventHandler(this.btnShiftLeft_Click);
            // 
            // btnCopyToClipboard
            // 
            this.btnCopyToClipboard.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCopyToClipboard.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCopyToClipboard.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCopyToClipboard.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCopyToClipboard.Image = null;
            this.btnCopyToClipboard.Location = new System.Drawing.Point(269, 416);
            this.btnCopyToClipboard.Name = "btnCopyToClipboard";
            this.btnCopyToClipboard.Size = new System.Drawing.Size(121, 23);
            this.btnCopyToClipboard.TabIndex = 13;
            this.btnCopyToClipboard.Text = "Copy to Clipboard";
            this.btnCopyToClipboard.Click += new DecentForms.EventHandler(this.btnCopyToClipboard_Click);
            // 
            // btnPasteFromClipboard
            // 
            this.btnPasteFromClipboard.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnPasteFromClipboard.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnPasteFromClipboard.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnPasteFromClipboard.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPasteFromClipboard.Image = null;
            this.btnPasteFromClipboard.Location = new System.Drawing.Point(269, 387);
            this.btnPasteFromClipboard.Name = "btnPasteFromClipboard";
            this.btnPasteFromClipboard.Size = new System.Drawing.Size(121, 23);
            this.btnPasteFromClipboard.TabIndex = 12;
            this.btnPasteFromClipboard.Text = "Paste from Clipboard";
            this.btnPasteFromClipboard.Click += new DecentForms.EventHandler(this.btnPasteFromClipboard_Click);
            // 
            // labelCharNo
            // 
            this.labelCharNo.Location = new System.Drawing.Point(266, 471);
            this.labelCharNo.Name = "labelCharNo";
            this.labelCharNo.Size = new System.Drawing.Size(82, 23);
            this.labelCharNo.TabIndex = 16;
            this.labelCharNo.Text = "label1";
            // 
            // checkShowGrid
            // 
            this.checkShowGrid.AutoSize = true;
            this.checkShowGrid.Location = new System.Drawing.Point(269, 365);
            this.checkShowGrid.Name = "checkShowGrid";
            this.checkShowGrid.Size = new System.Drawing.Size(75, 17);
            this.checkShowGrid.TabIndex = 11;
            this.checkShowGrid.Text = "Show Grid";
            this.checkShowGrid.UseVisualStyleBackColor = true;
            this.checkShowGrid.CheckedChanged += new System.EventHandler(this.checkShowGrid_CheckedChanged);
            // 
            // pictureEditor
            // 
            this.pictureEditor.AutoResize = false;
            this.pictureEditor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureEditor.DisplayPage = fastImage3;
            this.pictureEditor.Image = null;
            this.pictureEditor.Location = new System.Drawing.Point(40, 6);
            this.pictureEditor.Name = "pictureEditor";
            this.pictureEditor.Size = new System.Drawing.Size(434, 353);
            this.pictureEditor.TabIndex = 0;
            this.pictureEditor.TabStop = false;
            this.pictureEditor.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureEditor_MouseDown);
            this.pictureEditor.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureEditor_MouseMove);
            // 
            // tabExport
            // 
            this.tabExport.Controls.Add(this.labelCharactersTo);
            this.tabExport.Controls.Add(this.editDataExport);
            this.tabExport.Controls.Add(this.btnExport);
            this.tabExport.Controls.Add(this.comboExportMethod);
            this.tabExport.Controls.Add(this.label12);
            this.tabExport.Controls.Add(this.panelExport);
            this.tabExport.Controls.Add(this.comboExportRange);
            this.tabExport.Controls.Add(this.editSpriteCount);
            this.tabExport.Controls.Add(this.editSpriteFrom);
            this.tabExport.Controls.Add(this.labelCharactersFrom);
            this.tabExport.Location = new System.Drawing.Point(4, 22);
            this.tabExport.Name = "tabExport";
            this.tabExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabExport.Size = new System.Drawing.Size(979, 639);
            this.tabExport.TabIndex = 2;
            this.tabExport.Text = "Export";
            this.tabExport.UseVisualStyleBackColor = true;
            // 
            // labelCharactersTo
            // 
            this.labelCharactersTo.AutoSize = true;
            this.labelCharactersTo.Location = new System.Drawing.Point(188, 9);
            this.labelCharactersTo.Name = "labelCharactersTo";
            this.labelCharactersTo.Size = new System.Drawing.Size(37, 13);
            this.labelCharactersTo.TabIndex = 34;
            this.labelCharactersTo.Text = "count:";
            // 
            // editDataExport
            // 
            this.editDataExport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editDataExport.Location = new System.Drawing.Point(314, 7);
            this.editDataExport.Multiline = true;
            this.editDataExport.Name = "editDataExport";
            this.editDataExport.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.editDataExport.Size = new System.Drawing.Size(657, 626);
            this.editDataExport.TabIndex = 33;
            this.editDataExport.WordWrap = false;
            // 
            // btnExport
            // 
            this.btnExport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExport.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnExport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnExport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExport.Image = null;
            this.btnExport.Location = new System.Drawing.Point(247, 33);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(61, 21);
            this.btnExport.TabIndex = 32;
            this.btnExport.Text = "Export";
            this.btnExport.Click += new DecentForms.EventHandler(this.btnExport_Click);
            // 
            // comboExportMethod
            // 
            this.comboExportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboExportMethod.FormattingEnabled = true;
            this.comboExportMethod.Location = new System.Drawing.Point(90, 34);
            this.comboExportMethod.Name = "comboExportMethod";
            this.comboExportMethod.Size = new System.Drawing.Size(151, 21);
            this.comboExportMethod.TabIndex = 30;
            this.comboExportMethod.SelectedIndexChanged += new System.EventHandler(this.comboExportMethod_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(8, 37);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(79, 13);
            this.label12.TabIndex = 31;
            this.label12.Text = "Export Method:";
            // 
            // panelExport
            // 
            this.panelExport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panelExport.Location = new System.Drawing.Point(6, 61);
            this.panelExport.Name = "panelExport";
            this.panelExport.Size = new System.Drawing.Size(302, 572);
            this.panelExport.TabIndex = 28;
            // 
            // comboExportRange
            // 
            this.comboExportRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboExportRange.FormattingEnabled = true;
            this.comboExportRange.Location = new System.Drawing.Point(8, 6);
            this.comboExportRange.Name = "comboExportRange";
            this.comboExportRange.Size = new System.Drawing.Size(88, 21);
            this.comboExportRange.TabIndex = 18;
            this.comboExportRange.SelectedIndexChanged += new System.EventHandler(this.comboExportRange_SelectedIndexChanged);
            // 
            // editSpriteCount
            // 
            this.editSpriteCount.Location = new System.Drawing.Point(231, 6);
            this.editSpriteCount.Name = "editSpriteCount";
            this.editSpriteCount.Size = new System.Drawing.Size(36, 20);
            this.editSpriteCount.TabIndex = 17;
            // 
            // editSpriteFrom
            // 
            this.editSpriteFrom.Location = new System.Drawing.Point(141, 6);
            this.editSpriteFrom.Name = "editSpriteFrom";
            this.editSpriteFrom.Size = new System.Drawing.Size(39, 20);
            this.editSpriteFrom.TabIndex = 16;
            // 
            // labelCharactersFrom
            // 
            this.labelCharactersFrom.AutoSize = true;
            this.labelCharactersFrom.Location = new System.Drawing.Point(105, 9);
            this.labelCharactersFrom.Name = "labelCharactersFrom";
            this.labelCharactersFrom.Size = new System.Drawing.Size(30, 13);
            this.labelCharactersFrom.TabIndex = 15;
            this.labelCharactersFrom.Text = "from:";
            // 
            // tabImport
            // 
            this.tabImport.Controls.Add(this.panelImport);
            this.tabImport.Controls.Add(this.btnImport);
            this.tabImport.Controls.Add(this.comboImportMethod);
            this.tabImport.Controls.Add(this.label2);
            this.tabImport.Location = new System.Drawing.Point(4, 22);
            this.tabImport.Name = "tabImport";
            this.tabImport.Padding = new System.Windows.Forms.Padding(3);
            this.tabImport.Size = new System.Drawing.Size(979, 639);
            this.tabImport.TabIndex = 3;
            this.tabImport.Text = "Import";
            this.tabImport.UseVisualStyleBackColor = true;
            // 
            // panelImport
            // 
            this.panelImport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelImport.Location = new System.Drawing.Point(6, 33);
            this.panelImport.Name = "panelImport";
            this.panelImport.Size = new System.Drawing.Size(965, 600);
            this.panelImport.TabIndex = 40;
            // 
            // btnImport
            // 
            this.btnImport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnImport.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnImport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnImport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnImport.Image = null;
            this.btnImport.Location = new System.Drawing.Point(336, 5);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(75, 21);
            this.btnImport.TabIndex = 39;
            this.btnImport.Text = "Import";
            this.btnImport.Click += new DecentForms.EventHandler(this.btnImport_Click);
            // 
            // comboImportMethod
            // 
            this.comboImportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboImportMethod.FormattingEnabled = true;
            this.comboImportMethod.Location = new System.Drawing.Point(87, 6);
            this.comboImportMethod.Name = "comboImportMethod";
            this.comboImportMethod.Size = new System.Drawing.Size(243, 21);
            this.comboImportMethod.TabIndex = 37;
            this.comboImportMethod.SelectedIndexChanged += new System.EventHandler(this.comboImportMethod_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 38;
            this.label2.Text = "Import Method:";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(987, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openCharsetProjectToolStripMenuItem,
            this.saveSpriteProjectToolStripMenuItem,
            this.closeCharsetProjectToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.fileToolStripMenuItem.Text = "&Sprites";
            // 
            // openCharsetProjectToolStripMenuItem
            // 
            this.openCharsetProjectToolStripMenuItem.Name = "openCharsetProjectToolStripMenuItem";
            this.openCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.openCharsetProjectToolStripMenuItem.Text = "&Open Sprite Project...";
            this.openCharsetProjectToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveSpriteProjectToolStripMenuItem
            // 
            this.saveSpriteProjectToolStripMenuItem.Name = "saveSpriteProjectToolStripMenuItem";
            this.saveSpriteProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.saveSpriteProjectToolStripMenuItem.Text = "&Save Project";
            this.saveSpriteProjectToolStripMenuItem.Click += new System.EventHandler(this.saveCharsetProjectToolStripMenuItem_Click);
            // 
            // closeCharsetProjectToolStripMenuItem
            // 
            this.closeCharsetProjectToolStripMenuItem.Enabled = false;
            this.closeCharsetProjectToolStripMenuItem.Name = "closeCharsetProjectToolStripMenuItem";
            this.closeCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.closeCharsetProjectToolStripMenuItem.Text = "&Close Sprite Project";
            this.closeCharsetProjectToolStripMenuItem.Click += new System.EventHandler(this.closeCharsetProjectToolStripMenuItem_Click);
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Nr.";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "X";
            this.columnHeader5.Width = 30;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Y";
            this.columnHeader6.Width = 30;
            // 
            // contextMenuChangeMode
            // 
            this.contextMenuChangeMode.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.c64HiResMultiColorToolStripMenuItem,
            this.mega65ToolStripMenuItem,
            this.commanderX16ToolStripMenuItem});
            this.contextMenuChangeMode.Name = "contextMenuChangeMode";
            this.contextMenuChangeMode.Size = new System.Drawing.Size(223, 70);
            // 
            // c64HiResMultiColorToolStripMenuItem
            // 
            this.c64HiResMultiColorToolStripMenuItem.Name = "c64HiResMultiColorToolStripMenuItem";
            this.c64HiResMultiColorToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.c64HiResMultiColorToolStripMenuItem.Text = "C64 HiRes/MultiColor 24x21";
            this.c64HiResMultiColorToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // mega65ToolStripMenuItem
            // 
            this.mega65ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mega65_24x214ColorsToolStripMenuItem,
            this.mega65_64x214ColorsToolStripMenuItem,
            this.mega65_16x2116ColorsToolStripMenuItem});
            this.mega65ToolStripMenuItem.Name = "mega65ToolStripMenuItem";
            this.mega65ToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.mega65ToolStripMenuItem.Text = "Mega65";
            // 
            // mega65_24x214ColorsToolStripMenuItem
            // 
            this.mega65_24x214ColorsToolStripMenuItem.Name = "mega65_24x214ColorsToolStripMenuItem";
            this.mega65_24x214ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.mega65_24x214ColorsToolStripMenuItem.Text = "24x21 4 Colors";
            this.mega65_24x214ColorsToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // mega65_64x214ColorsToolStripMenuItem
            // 
            this.mega65_64x214ColorsToolStripMenuItem.Name = "mega65_64x214ColorsToolStripMenuItem";
            this.mega65_64x214ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.mega65_64x214ColorsToolStripMenuItem.Text = "64x21 4 Colors";
            this.mega65_64x214ColorsToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // mega65_16x2116ColorsToolStripMenuItem
            // 
            this.mega65_16x2116ColorsToolStripMenuItem.Name = "mega65_16x2116ColorsToolStripMenuItem";
            this.mega65_16x2116ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.mega65_16x2116ColorsToolStripMenuItem.Text = "16x21 16 Colors";
            this.mega65_16x2116ColorsToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // commanderX16ToolStripMenuItem
            // 
            this.commanderX16ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.x16_16ColorsToolStripMenuItem,
            this.x16_256ColorsToolStripMenuItem});
            this.commanderX16ToolStripMenuItem.Name = "commanderX16ToolStripMenuItem";
            this.commanderX16ToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.commanderX16ToolStripMenuItem.Text = "Commander X16";
            // 
            // x16_16ColorsToolStripMenuItem
            // 
            this.x16_16ColorsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.x16_8x8ToolStripMenuItem,
            this.x16_16x8ToolStripMenuItem,
            this.x16_32x8ToolStripMenuItem,
            this.x16_64x8ToolStripMenuItem,
            this.x16_8x16ToolStripMenuItem,
            this.x16_16x16ToolStripMenuItem,
            this.x16_32x16ToolStripMenuItem,
            this.x16_64x16ToolStripMenuItem,
            this.x16_8x32ToolStripMenuItem,
            this.x16_16x32ToolStripMenuItem,
            this.x16_32x32ToolStripMenuItem,
            this.x16_64x32ToolStripMenuItem,
            this.x16_8x64ToolStripMenuItem,
            this.x16_16x64ToolStripMenuItem,
            this.x16_32x64ToolStripMenuItem,
            this.x16_64x64ToolStripMenuItem});
            this.x16_16ColorsToolStripMenuItem.Name = "x16_16ColorsToolStripMenuItem";
            this.x16_16ColorsToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.x16_16ColorsToolStripMenuItem.Text = "16 Colors";
            // 
            // x16_8x8ToolStripMenuItem
            // 
            this.x16_8x8ToolStripMenuItem.Name = "x16_8x8ToolStripMenuItem";
            this.x16_8x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x8ToolStripMenuItem.Text = "8x8";
            this.x16_8x8ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x8ToolStripMenuItem
            // 
            this.x16_16x8ToolStripMenuItem.Name = "x16_16x8ToolStripMenuItem";
            this.x16_16x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x8ToolStripMenuItem.Text = "16x8";
            this.x16_16x8ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x8ToolStripMenuItem
            // 
            this.x16_32x8ToolStripMenuItem.Name = "x16_32x8ToolStripMenuItem";
            this.x16_32x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x8ToolStripMenuItem.Text = "32x8";
            this.x16_32x8ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x8ToolStripMenuItem
            // 
            this.x16_64x8ToolStripMenuItem.Name = "x16_64x8ToolStripMenuItem";
            this.x16_64x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x8ToolStripMenuItem.Text = "64x8";
            this.x16_64x8ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x16ToolStripMenuItem
            // 
            this.x16_8x16ToolStripMenuItem.Name = "x16_8x16ToolStripMenuItem";
            this.x16_8x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x16ToolStripMenuItem.Text = "8x16";
            this.x16_8x16ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x16ToolStripMenuItem
            // 
            this.x16_16x16ToolStripMenuItem.Name = "x16_16x16ToolStripMenuItem";
            this.x16_16x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x16ToolStripMenuItem.Text = "16x16";
            this.x16_16x16ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x16ToolStripMenuItem
            // 
            this.x16_32x16ToolStripMenuItem.Name = "x16_32x16ToolStripMenuItem";
            this.x16_32x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x16ToolStripMenuItem.Text = "32x16";
            this.x16_32x16ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x16ToolStripMenuItem
            // 
            this.x16_64x16ToolStripMenuItem.Name = "x16_64x16ToolStripMenuItem";
            this.x16_64x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x16ToolStripMenuItem.Text = "64x16";
            this.x16_64x16ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x32ToolStripMenuItem
            // 
            this.x16_8x32ToolStripMenuItem.Name = "x16_8x32ToolStripMenuItem";
            this.x16_8x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x32ToolStripMenuItem.Text = "8x32";
            this.x16_8x32ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x32ToolStripMenuItem
            // 
            this.x16_16x32ToolStripMenuItem.Name = "x16_16x32ToolStripMenuItem";
            this.x16_16x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x32ToolStripMenuItem.Text = "16x32";
            this.x16_16x32ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x32ToolStripMenuItem
            // 
            this.x16_32x32ToolStripMenuItem.Name = "x16_32x32ToolStripMenuItem";
            this.x16_32x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x32ToolStripMenuItem.Text = "32x32";
            this.x16_32x32ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x32ToolStripMenuItem
            // 
            this.x16_64x32ToolStripMenuItem.Name = "x16_64x32ToolStripMenuItem";
            this.x16_64x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x32ToolStripMenuItem.Text = "64x32";
            this.x16_64x32ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x64ToolStripMenuItem
            // 
            this.x16_8x64ToolStripMenuItem.Name = "x16_8x64ToolStripMenuItem";
            this.x16_8x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x64ToolStripMenuItem.Text = "8x64";
            this.x16_8x64ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x64ToolStripMenuItem
            // 
            this.x16_16x64ToolStripMenuItem.Name = "x16_16x64ToolStripMenuItem";
            this.x16_16x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x64ToolStripMenuItem.Text = "16x64";
            this.x16_16x64ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x64ToolStripMenuItem
            // 
            this.x16_32x64ToolStripMenuItem.Name = "x16_32x64ToolStripMenuItem";
            this.x16_32x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x64ToolStripMenuItem.Text = "32x64";
            this.x16_32x64ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x64ToolStripMenuItem
            // 
            this.x16_64x64ToolStripMenuItem.Name = "x16_64x64ToolStripMenuItem";
            this.x16_64x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x64ToolStripMenuItem.Text = "64x64";
            this.x16_64x64ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_256ColorsToolStripMenuItem
            // 
            this.x16_256ColorsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.x16_8x8x256ToolStripMenuItem,
            this.x16_16x8x256ToolStripMenuItem,
            this.x16_32x8x256ToolStripMenuItem,
            this.x16_64x8x256ToolStripMenuItem,
            this.x16_8x16x256ToolStripMenuItem,
            this.x16_16x16x256ToolStripMenuItem,
            this.x16_32x16x256ToolStripMenuItem,
            this.x16_64x16x256ToolStripMenuItem,
            this.x16_8x32x256ToolStripMenuItem,
            this.x16_16x32x256ToolStripMenuItem,
            this.x16_32x32x256ToolStripMenuItem,
            this.x16_64x32x256ToolStripMenuItem,
            this.x16_8x64x256ToolStripMenuItem,
            this.x16_16x64x256ToolStripMenuItem,
            this.x16_32x64x256ToolStripMenuItem,
            this.x16_64x64x256ToolStripMenuItem});
            this.x16_256ColorsToolStripMenuItem.Name = "x16_256ColorsToolStripMenuItem";
            this.x16_256ColorsToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            this.x16_256ColorsToolStripMenuItem.Text = "256 Colors";
            // 
            // x16_8x8x256ToolStripMenuItem
            // 
            this.x16_8x8x256ToolStripMenuItem.Name = "x16_8x8x256ToolStripMenuItem";
            this.x16_8x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x8x256ToolStripMenuItem.Text = "8x8";
            this.x16_8x8x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x8x256ToolStripMenuItem
            // 
            this.x16_16x8x256ToolStripMenuItem.Name = "x16_16x8x256ToolStripMenuItem";
            this.x16_16x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x8x256ToolStripMenuItem.Text = "16x8";
            this.x16_16x8x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x8x256ToolStripMenuItem
            // 
            this.x16_32x8x256ToolStripMenuItem.Name = "x16_32x8x256ToolStripMenuItem";
            this.x16_32x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x8x256ToolStripMenuItem.Text = "32x8";
            this.x16_32x8x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x8x256ToolStripMenuItem
            // 
            this.x16_64x8x256ToolStripMenuItem.Name = "x16_64x8x256ToolStripMenuItem";
            this.x16_64x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x8x256ToolStripMenuItem.Text = "64x8";
            this.x16_64x8x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x16x256ToolStripMenuItem
            // 
            this.x16_8x16x256ToolStripMenuItem.Name = "x16_8x16x256ToolStripMenuItem";
            this.x16_8x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x16x256ToolStripMenuItem.Text = "8x16";
            this.x16_8x16x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x16x256ToolStripMenuItem
            // 
            this.x16_16x16x256ToolStripMenuItem.Name = "x16_16x16x256ToolStripMenuItem";
            this.x16_16x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x16x256ToolStripMenuItem.Text = "16x16";
            this.x16_16x16x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x16x256ToolStripMenuItem
            // 
            this.x16_32x16x256ToolStripMenuItem.Name = "x16_32x16x256ToolStripMenuItem";
            this.x16_32x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x16x256ToolStripMenuItem.Text = "32x16";
            this.x16_32x16x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x16x256ToolStripMenuItem
            // 
            this.x16_64x16x256ToolStripMenuItem.Name = "x16_64x16x256ToolStripMenuItem";
            this.x16_64x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x16x256ToolStripMenuItem.Text = "64x16";
            this.x16_64x16x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x32x256ToolStripMenuItem
            // 
            this.x16_8x32x256ToolStripMenuItem.Name = "x16_8x32x256ToolStripMenuItem";
            this.x16_8x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x32x256ToolStripMenuItem.Text = "8x32";
            this.x16_8x32x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x32x256ToolStripMenuItem
            // 
            this.x16_16x32x256ToolStripMenuItem.Name = "x16_16x32x256ToolStripMenuItem";
            this.x16_16x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x32x256ToolStripMenuItem.Text = "16x32";
            this.x16_16x32x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x32x256ToolStripMenuItem
            // 
            this.x16_32x32x256ToolStripMenuItem.Name = "x16_32x32x256ToolStripMenuItem";
            this.x16_32x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x32x256ToolStripMenuItem.Text = "32x32";
            this.x16_32x32x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x32x256ToolStripMenuItem
            // 
            this.x16_64x32x256ToolStripMenuItem.Name = "x16_64x32x256ToolStripMenuItem";
            this.x16_64x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x32x256ToolStripMenuItem.Text = "64x32";
            this.x16_64x32x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_8x64x256ToolStripMenuItem
            // 
            this.x16_8x64x256ToolStripMenuItem.Name = "x16_8x64x256ToolStripMenuItem";
            this.x16_8x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_8x64x256ToolStripMenuItem.Text = "8x64";
            this.x16_8x64x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_16x64x256ToolStripMenuItem
            // 
            this.x16_16x64x256ToolStripMenuItem.Name = "x16_16x64x256ToolStripMenuItem";
            this.x16_16x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_16x64x256ToolStripMenuItem.Text = "16x64";
            this.x16_16x64x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_32x64x256ToolStripMenuItem
            // 
            this.x16_32x64x256ToolStripMenuItem.Name = "x16_32x64x256ToolStripMenuItem";
            this.x16_32x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_32x64x256ToolStripMenuItem.Text = "32x64";
            this.x16_32x64x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // x16_64x64x256ToolStripMenuItem
            // 
            this.x16_64x64x256ToolStripMenuItem.Name = "x16_64x64x256ToolStripMenuItem";
            this.x16_64x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            this.x16_64x64x256ToolStripMenuItem.Text = "64x64";
            this.x16_64x64x256ToolStripMenuItem.Click += new System.EventHandler(this.spriteModeChangedMenuItem_Click);
            // 
            // SpriteEditor
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(987, 689);
            this.Controls.Add(this.tabSpriteEditor);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SpriteEditor";
            this.Text = "Sprite Editor";
            ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).EndInit();
            this.tabSpriteEditor.ResumeLayout(false);
            this.tabEditor.ResumeLayout(false);
            this.tabEditor.PerformLayout();
            this.tabSpriteDetails.ResumeLayout(false);
            this.tabPageOverlay.ResumeLayout(false);
            this.tabPageOverlay.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOverlayPreview)).EndInit();
            this.tabPageAnimation.ResumeLayout(false);
            this.tabPageAnimation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editFrameDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAnimPreview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditor)).EndInit();
            this.tabExport.ResumeLayout(false);
            this.tabExport.PerformLayout();
            this.tabImport.ResumeLayout(false);
            this.tabImport.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuChangeMode.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TabControl tabSpriteEditor;
    private System.Windows.Forms.TabPage tabEditor;
    private GR.Forms.FastPictureBox pictureEditor;
    private GR.Forms.ImageListbox panelSprites;
    private System.Windows.Forms.Label labelCharNo;
    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem openCharsetProjectToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem closeCharsetProjectToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem saveSpriteProjectToolStripMenuItem;
    private DecentForms.Button btnCopyToClipboard;
    private DecentForms.Button btnPasteFromClipboard;
    private DecentForms.Button btnShiftLeft;
    private DecentForms.Button btnShiftDown;
    private DecentForms.Button btnShiftUp;
    private DecentForms.Button btnShiftRight;
    private DecentForms.Button btnMirrorX;
    private DecentForms.Button btnMirrorY;
    private System.Windows.Forms.CheckBox checkShowGrid;
    private DecentForms.Button btnInvert;
    private DecentForms.Button btnRotateLeft;
    private DecentForms.Button btnRotateRight;
    private DecentForms.Button btnDeleteSprite;
    private System.Windows.Forms.TabControl tabSpriteDetails;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.ColumnHeader columnHeader6;
    private System.Windows.Forms.ColumnHeader columnHeader7;
    private System.Windows.Forms.ColumnHeader columnHeader8;
    private System.Windows.Forms.ColumnHeader columnHeader9;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.Label label11;
    private DecentForms.RadioButton btnToolFill;
    private DecentForms.RadioButton btnToolEdit;
    private System.Windows.Forms.Panel panelColorSettings;
    private DecentForms.Button btnClearSprite;
    private System.Windows.Forms.TabPage tabExport;
    private System.Windows.Forms.ComboBox comboExportRange;
    private System.Windows.Forms.TextBox editSpriteCount;
    private System.Windows.Forms.TextBox editSpriteFrom;
    private System.Windows.Forms.Label labelCharactersFrom;
    private System.Windows.Forms.Panel panelExport;
    private DecentForms.Button btnExport;
    private System.Windows.Forms.ComboBox comboExportMethod;
    private System.Windows.Forms.Label label12;
    private System.Windows.Forms.TextBox editDataExport;
    private System.Windows.Forms.Label labelCharactersTo;
    private System.Windows.Forms.TabPage tabImport;
    private DecentForms.Button btnImport;
    private System.Windows.Forms.ComboBox comboImportMethod;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Panel panelImport;
        private DecentForms.MenuButton btnChangeMode;
        private System.Windows.Forms.ContextMenuStrip contextMenuChangeMode;
        private System.Windows.Forms.ToolStripMenuItem c64HiResMultiColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mega65ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem commanderX16ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_16ColorsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_8x8ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_16x8ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_32x8ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_64x8ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_8x16ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_16x16ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_32x16ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_64x16ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_8x32ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_16x32ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_32x32ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_64x32ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_8x64ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_16x64ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_32x64ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_64x64ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem x16_256ColorsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_8x8x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_16x8x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_32x8x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_64x8x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_8x16x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_16x16x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_32x16x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_64x16x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_8x32x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_16x32x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_32x32x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_64x32x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_8x64x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_16x64x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_32x64x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem x16_64x64x256ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem mega65_24x214ColorsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem mega65_64x214ColorsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem mega65_16x2116ColorsToolStripMenuItem;
    private DecentForms.Button btnHighlightDuplicates;
    private System.Windows.Forms.Label labelSelectionInfo;
    private System.Windows.Forms.TextBox editMoveTargetIndex;
    private DecentForms.Button btnMoveSelectionToTarget;
    private DecentForms.Button btnBankCopy;
    private DecentForms.Button btnBankPaste;
    private System.Windows.Forms.TabPage tabPageOverlay;
    private DecentForms.Button btnAddOverlay;
    private DecentForms.Button btnRemoveOverlay;
    private System.Windows.Forms.ListBox listOverlays;
    private System.Windows.Forms.Label labelOverlayName;
    private System.Windows.Forms.TextBox editOverlayName;
    private System.Windows.Forms.Panel panelOverlaySlots;
    private GR.Forms.FastPictureBox picOverlayPreview;
    private DecentForms.Button btnOverlayZoomOut;
    private DecentForms.Button btnOverlayZoomIn;
    private System.Windows.Forms.Label labelOverlayZoom;
    private System.Windows.Forms.TabPage tabPageAnimation;
    private DecentForms.Button btnAddFrame;
    private DecentForms.Button btnRemoveFrame;
    private DecentForms.Button btnDuplicateFrame;
    private DecentForms.Button btnPlayAnim;
    private DecentForms.Button btnPauseAnim;
    private System.Windows.Forms.ListBox listAnimFrames;
    private System.Windows.Forms.Label labelFrameDelay;
    private System.Windows.Forms.NumericUpDown editFrameDelay;
    private DecentForms.Button btnApplyDelayToAll;
    private System.Windows.Forms.Panel panelFrameSlots;
    private GR.Forms.FastPictureBox picAnimPreview;
    private DecentForms.Button btnAnimZoomOut;
    private DecentForms.Button btnAnimZoomIn;
    private System.Windows.Forms.Label labelAnimZoom;
  }
}
