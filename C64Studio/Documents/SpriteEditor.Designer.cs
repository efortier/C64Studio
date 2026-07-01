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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpriteEditor));
            GR.Image.FastImage fastImage4 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage3 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage2 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage1 = new GR.Image.FastImage();
            tabSpriteEditor = new System.Windows.Forms.TabControl();
            tabEditor = new System.Windows.Forms.TabPage();
            editMoveTargetIndex = new System.Windows.Forms.TextBox();
            btnMoveSelectionToTarget = new DecentForms.Button();
            labelSelectionInfo = new System.Windows.Forms.Label();
            btnHighlightDuplicates = new DecentForms.Button();
            btnChangeMode = new DecentForms.MenuButton();
            panelColorSettings = new System.Windows.Forms.Panel();
            btnToolEdit = new DecentForms.RadioButton();
            btnToolFill = new DecentForms.RadioButton();
            label11 = new System.Windows.Forms.Label();
            btnClearSprite = new DecentForms.Button();
            btnDeleteSprite = new DecentForms.Button();
            btnBankCopy = new DecentForms.Button();
            btnBankPaste = new DecentForms.Button();
            btnInvert = new DecentForms.Button();
            btnMirrorY = new DecentForms.Button();
            btnMirrorX = new DecentForms.Button();
            btnShiftDown = new DecentForms.Button();
            btnShiftUp = new DecentForms.Button();
            btnShiftRight = new DecentForms.Button();
            btnRotateRight = new DecentForms.Button();
            btnRotateLeft = new DecentForms.Button();
            btnShiftLeft = new DecentForms.Button();
            btnCopyToClipboard = new DecentForms.Button();
            btnPasteFromClipboard = new DecentForms.Button();
            labelCharNo = new System.Windows.Forms.Label();
            checkShowGrid = new System.Windows.Forms.CheckBox();
            trackGridOpacity = new System.Windows.Forms.TrackBar();
            labelGridOpacity = new System.Windows.Forms.Label();
            pictureEditor = new GR.Forms.FastPictureBox();
            tabExport = new System.Windows.Forms.TabPage();
            labelCharactersTo = new System.Windows.Forms.Label();
            editDataExport = new System.Windows.Forms.TextBox();
            btnExport = new DecentForms.Button();
            comboExportMethod = new System.Windows.Forms.ComboBox();
            label12 = new System.Windows.Forms.Label();
            panelExport = new System.Windows.Forms.Panel();
            comboExportRange = new System.Windows.Forms.ComboBox();
            editSpriteCount = new System.Windows.Forms.TextBox();
            editSpriteFrom = new System.Windows.Forms.TextBox();
            labelCharactersFrom = new System.Windows.Forms.Label();
            tabImport = new System.Windows.Forms.TabPage();
            panelImport = new System.Windows.Forms.Panel();
            btnImport = new DecentForms.Button();
            comboImportMethod = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveSpriteProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            columnHeader4 = new System.Windows.Forms.ColumnHeader();
            columnHeader5 = new System.Windows.Forms.ColumnHeader();
            columnHeader6 = new System.Windows.Forms.ColumnHeader();
            columnHeader7 = new System.Windows.Forms.ColumnHeader();
            columnHeader8 = new System.Windows.Forms.ColumnHeader();
            columnHeader9 = new System.Windows.Forms.ColumnHeader();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            contextMenuChangeMode = new System.Windows.Forms.ContextMenuStrip(components);
            c64HiResMultiColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mega65ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mega65_24x214ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mega65_64x214ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mega65_16x2116ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            commanderX16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x8ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x16ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x32ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x64ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_256ColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x8x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x16x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x32x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_8x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_16x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_32x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            x16_64x64x256ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tabPageOverlay = new System.Windows.Forms.TabPage();
            panelSprites = new GR.Forms.ImageListbox();
            labelOverlayZoom = new System.Windows.Forms.Label();
            btnOverlayZoomIn = new DecentForms.Button();
            btnOverlayZoomOut = new DecentForms.Button();
            picOverlayPreview = new GR.Forms.FastPictureBox();
            panelOverlaySlots = new System.Windows.Forms.Panel();
            editOverlayName = new System.Windows.Forms.TextBox();
            labelOverlayName = new System.Windows.Forms.Label();
            listOverlays = new System.Windows.Forms.ListBox();
            btnRemoveOverlay = new DecentForms.Button();
            btnAddOverlay = new DecentForms.Button();
            btnCloneOverlay = new DecentForms.Button();
            panel1 = new System.Windows.Forms.Panel();
            editAnimationID = new System.Windows.Forms.NumericUpDown();
            labelAnimationID = new System.Windows.Forms.Label();
            checkLoop = new System.Windows.Forms.CheckBox();
            checkStartRandomFrame = new System.Windows.Forms.CheckBox();
            editFrameDelay = new System.Windows.Forms.NumericUpDown();
            labelFrameDelay = new System.Windows.Forms.Label();
            listAnimFrames = new System.Windows.Forms.ListBox();
            panelFrameSlots = new System.Windows.Forms.Panel();
            btnPauseAnim = new DecentForms.Button();
            picAnimPreview = new GR.Forms.FastPictureBox();
            btnPlayAnim = new DecentForms.Button();
            btnAnimZoomOut = new DecentForms.Button();
            btnDuplicateFrame = new DecentForms.Button();
            btnAnimZoomIn = new DecentForms.Button();
            btnRemoveFrame = new DecentForms.Button();
            labelAnimZoom = new System.Windows.Forms.Label();
            btnAddFrame = new DecentForms.Button();
            btnTestStop = new DecentForms.Button();
            labelTestMagResult = new System.Windows.Forms.Label();
            editTestTargetH = new System.Windows.Forms.TextBox();
            labelTestTargetX = new System.Windows.Forms.Label();
            editTestTargetW = new System.Windows.Forms.TextBox();
            labelTestTarget = new System.Windows.Forms.Label();
            checkTestC64Mag = new System.Windows.Forms.CheckBox();
            checkTestLoop = new System.Windows.Forms.CheckBox();
            checkTestExpandY = new System.Windows.Forms.CheckBox();
            checkTestExpandX = new System.Windows.Forms.CheckBox();
            comboTestBackColor = new System.Windows.Forms.ComboBox();
            labelTestBack = new System.Windows.Forms.Label();
            panelSpriteTest = new System.Windows.Forms.Panel();
            picSpriteTest = new GR.Forms.FastPictureBox();
            tabSpriteDetails = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)m_FileWatcher).BeginInit();
            tabSpriteEditor.SuspendLayout();
            tabEditor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackGridOpacity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureEditor).BeginInit();
            tabExport.SuspendLayout();
            tabImport.SuspendLayout();
            menuStrip1.SuspendLayout();
            contextMenuChangeMode.SuspendLayout();
            tabPageOverlay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picOverlayPreview).BeginInit();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)editAnimationID).BeginInit();
            ((System.ComponentModel.ISupportInitialize)editFrameDelay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picAnimPreview).BeginInit();
            panelSpriteTest.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picSpriteTest).BeginInit();
            tabSpriteDetails.SuspendLayout();
            SuspendLayout();
            // 
            // tabSpriteEditor
            // 
            tabSpriteEditor.Controls.Add(tabEditor);
            tabSpriteEditor.Controls.Add(tabExport);
            tabSpriteEditor.Controls.Add(tabImport);
            tabSpriteEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            tabSpriteEditor.Location = new System.Drawing.Point(0, 24);
            tabSpriteEditor.Name = "tabSpriteEditor";
            tabSpriteEditor.SelectedIndex = 0;
            tabSpriteEditor.Size = new System.Drawing.Size(2054, 762);
            tabSpriteEditor.TabIndex = 0;
            // 
            // tabEditor
            // 
            tabEditor.Controls.Add(editMoveTargetIndex);
            tabEditor.Controls.Add(btnMoveSelectionToTarget);
            tabEditor.Controls.Add(labelSelectionInfo);
            tabEditor.Controls.Add(btnHighlightDuplicates);
            tabEditor.Controls.Add(btnChangeMode);
            tabEditor.Controls.Add(panelColorSettings);
            tabEditor.Controls.Add(btnToolEdit);
            tabEditor.Controls.Add(btnToolFill);
            tabEditor.Controls.Add(label11);
            tabEditor.Controls.Add(tabSpriteDetails);
            tabEditor.Controls.Add(btnClearSprite);
            tabEditor.Controls.Add(btnDeleteSprite);
            tabEditor.Controls.Add(btnBankCopy);
            tabEditor.Controls.Add(btnBankPaste);
            tabEditor.Controls.Add(btnInvert);
            tabEditor.Controls.Add(btnMirrorY);
            tabEditor.Controls.Add(btnMirrorX);
            tabEditor.Controls.Add(btnShiftDown);
            tabEditor.Controls.Add(btnShiftUp);
            tabEditor.Controls.Add(btnShiftRight);
            tabEditor.Controls.Add(btnRotateRight);
            tabEditor.Controls.Add(btnRotateLeft);
            tabEditor.Controls.Add(btnShiftLeft);
            tabEditor.Controls.Add(btnCopyToClipboard);
            tabEditor.Controls.Add(btnPasteFromClipboard);
            tabEditor.Controls.Add(labelCharNo);
            tabEditor.Controls.Add(checkShowGrid);
            tabEditor.Controls.Add(trackGridOpacity);
            tabEditor.Controls.Add(labelGridOpacity);
            tabEditor.Controls.Add(pictureEditor);
            tabEditor.Location = new System.Drawing.Point(4, 22);
            tabEditor.Name = "tabEditor";
            tabEditor.Padding = new System.Windows.Forms.Padding(3);
            tabEditor.Size = new System.Drawing.Size(2046, 736);
            tabEditor.TabIndex = 0;
            tabEditor.Text = "Sprite";
            tabEditor.UseVisualStyleBackColor = true;
            // 
            // editMoveTargetIndex
            // 
            editMoveTargetIndex.Location = new System.Drawing.Point(396, 570);
            editMoveTargetIndex.Name = "editMoveTargetIndex";
            editMoveTargetIndex.Size = new System.Drawing.Size(73, 20);
            editMoveTargetIndex.TabIndex = 0;
            // 
            // btnMoveSelectionToTarget
            // 
            btnMoveSelectionToTarget.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnMoveSelectionToTarget.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnMoveSelectionToTarget.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnMoveSelectionToTarget.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnMoveSelectionToTarget.Image = null;
            btnMoveSelectionToTarget.Location = new System.Drawing.Point(269, 568);
            btnMoveSelectionToTarget.Name = "btnMoveSelectionToTarget";
            btnMoveSelectionToTarget.Size = new System.Drawing.Size(121, 23);
            btnMoveSelectionToTarget.TabIndex = 1;
            btnMoveSelectionToTarget.Text = "Move to Index";
            btnMoveSelectionToTarget.Click += btnMoveSelectionToTarget_Click;
            // 
            // labelSelectionInfo
            // 
            labelSelectionInfo.Location = new System.Drawing.Point(352, 471);
            labelSelectionInfo.Name = "labelSelectionInfo";
            labelSelectionInfo.Size = new System.Drawing.Size(122, 23);
            labelSelectionInfo.TabIndex = 62;
            labelSelectionInfo.Text = "label1";
            // 
            // btnHighlightDuplicates
            // 
            btnHighlightDuplicates.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnHighlightDuplicates.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnHighlightDuplicates.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnHighlightDuplicates.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnHighlightDuplicates.Image = null;
            btnHighlightDuplicates.Location = new System.Drawing.Point(269, 539);
            btnHighlightDuplicates.Name = "btnHighlightDuplicates";
            btnHighlightDuplicates.Size = new System.Drawing.Size(121, 23);
            btnHighlightDuplicates.TabIndex = 61;
            btnHighlightDuplicates.Text = "Duplicates";
            btnHighlightDuplicates.Click += btnHighlightDuplicates_Click;
            // 
            // btnChangeMode
            // 
            btnChangeMode.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnChangeMode.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnChangeMode.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnChangeMode.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnChangeMode.Image = null;
            btnChangeMode.Location = new System.Drawing.Point(269, 510);
            btnChangeMode.Name = "btnChangeMode";
            btnChangeMode.Size = new System.Drawing.Size(205, 23);
            btnChangeMode.TabIndex = 60;
            btnChangeMode.Text = "btnChangeMode";
            btnChangeMode.Click += btnChangeMode_Click;
            // 
            // panelColorSettings
            // 
            panelColorSettings.Location = new System.Drawing.Point(40, 365);
            panelColorSettings.Name = "panelColorSettings";
            panelColorSettings.Size = new System.Drawing.Size(220, 210);
            panelColorSettings.TabIndex = 59;
            // 
            // btnToolEdit
            // 
            btnToolEdit.Appearance = System.Windows.Forms.Appearance.Button;
            btnToolEdit.BorderStyle = DecentForms.BorderStyle.NONE;
            btnToolEdit.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            btnToolEdit.Checked = true;
            btnToolEdit.Image = (System.Drawing.Image)resources.GetObject("btnToolEdit.Image");
            btnToolEdit.Location = new System.Drawing.Point(8, 285);
            btnToolEdit.Name = "btnToolEdit";
            btnToolEdit.Size = new System.Drawing.Size(26, 26);
            btnToolEdit.TabIndex = 9;
            toolTip1.SetToolTip(btnToolEdit, "Single Character");
            btnToolEdit.CheckedChanged += btnToolEdit_CheckedChanged;
            // 
            // btnToolFill
            // 
            btnToolFill.Appearance = System.Windows.Forms.Appearance.Button;
            btnToolFill.BorderStyle = DecentForms.BorderStyle.NONE;
            btnToolFill.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            btnToolFill.Checked = false;
            btnToolFill.Image = (System.Drawing.Image)resources.GetObject("btnToolFill.Image");
            btnToolFill.Location = new System.Drawing.Point(8, 316);
            btnToolFill.Name = "btnToolFill";
            btnToolFill.Size = new System.Drawing.Size(26, 26);
            btnToolFill.TabIndex = 10;
            toolTip1.SetToolTip(btnToolFill, "Fill");
            btnToolFill.CheckedChanged += btnToolFill_CheckedChanged;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(266, 494);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(37, 13);
            label11.TabIndex = 16;
            label11.Text = "Mode:";
            // 
            // btnClearSprite
            // 
            btnClearSprite.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnClearSprite.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnClearSprite.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnClearSprite.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnClearSprite.Enabled = false;
            btnClearSprite.Image = null;
            btnClearSprite.Location = new System.Drawing.Point(269, 445);
            btnClearSprite.Name = "btnClearSprite";
            btnClearSprite.Size = new System.Drawing.Size(57, 23);
            btnClearSprite.TabIndex = 14;
            btnClearSprite.Text = "Clear";
            btnClearSprite.Click += btnClearSprite_Click;
            // 
            // btnDeleteSprite
            // 
            btnDeleteSprite.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnDeleteSprite.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnDeleteSprite.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnDeleteSprite.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnDeleteSprite.Enabled = false;
            btnDeleteSprite.Image = null;
            btnDeleteSprite.Location = new System.Drawing.Point(333, 445);
            btnDeleteSprite.Name = "btnDeleteSprite";
            btnDeleteSprite.Size = new System.Drawing.Size(57, 23);
            btnDeleteSprite.TabIndex = 15;
            btnDeleteSprite.Text = "Delete";
            btnDeleteSprite.Click += btnDeleteSprite_Click;
            // 
            // btnBankCopy
            // 
            btnBankCopy.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnBankCopy.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnBankCopy.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnBankCopy.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnBankCopy.Enabled = false;
            btnBankCopy.Image = null;
            btnBankCopy.Location = new System.Drawing.Point(269, 595);
            btnBankCopy.Name = "btnBankCopy";
            btnBankCopy.Size = new System.Drawing.Size(57, 23);
            btnBankCopy.TabIndex = 63;
            btnBankCopy.Text = "Bank Cp";
            toolTip1.SetToolTip(btnBankCopy, "Copy the selected bank sprites to an in-process clipboard.");
            btnBankCopy.Click += btnBankCopy_Click;
            // 
            // btnBankPaste
            // 
            btnBankPaste.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnBankPaste.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnBankPaste.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnBankPaste.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnBankPaste.Enabled = false;
            btnBankPaste.Image = null;
            btnBankPaste.Location = new System.Drawing.Point(333, 595);
            btnBankPaste.Name = "btnBankPaste";
            btnBankPaste.Size = new System.Drawing.Size(57, 23);
            btnBankPaste.TabIndex = 64;
            btnBankPaste.Text = "Bank Pst";
            toolTip1.SetToolTip(btnBankPaste, "Paste the in-process clipboard onto the bank starting at the current selection.");
            btnBankPaste.Click += btnBankPaste_Click;
            // 
            // btnInvert
            // 
            btnInvert.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnInvert.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnInvert.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnInvert.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnInvert.Image = (System.Drawing.Image)resources.GetObject("btnInvert.Image");
            btnInvert.Location = new System.Drawing.Point(8, 192);
            btnInvert.Name = "btnInvert";
            btnInvert.Size = new System.Drawing.Size(26, 26);
            btnInvert.TabIndex = 6;
            toolTip1.SetToolTip(btnInvert, "Invert selected sprites colors");
            btnInvert.Click += btnInvert_Click;
            // 
            // btnMirrorY
            // 
            btnMirrorY.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnMirrorY.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnMirrorY.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnMirrorY.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnMirrorY.Image = (System.Drawing.Image)resources.GetObject("btnMirrorY.Image");
            btnMirrorY.Location = new System.Drawing.Point(8, 161);
            btnMirrorY.Name = "btnMirrorY";
            btnMirrorY.Size = new System.Drawing.Size(26, 26);
            btnMirrorY.TabIndex = 5;
            toolTip1.SetToolTip(btnMirrorY, "Mirror selected sprites vertically");
            btnMirrorY.Click += btnMirrorY_Click;
            // 
            // btnMirrorX
            // 
            btnMirrorX.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnMirrorX.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnMirrorX.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnMirrorX.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnMirrorX.Image = (System.Drawing.Image)resources.GetObject("btnMirrorX.Image");
            btnMirrorX.Location = new System.Drawing.Point(8, 130);
            btnMirrorX.Name = "btnMirrorX";
            btnMirrorX.Size = new System.Drawing.Size(26, 26);
            btnMirrorX.TabIndex = 4;
            toolTip1.SetToolTip(btnMirrorX, "Mirror selected sprites horizontally");
            btnMirrorX.Click += btnMirrorX_Click;
            // 
            // btnShiftDown
            // 
            btnShiftDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnShiftDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnShiftDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnShiftDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnShiftDown.Image = (System.Drawing.Image)resources.GetObject("btnShiftDown.Image");
            btnShiftDown.Location = new System.Drawing.Point(8, 99);
            btnShiftDown.Name = "btnShiftDown";
            btnShiftDown.Size = new System.Drawing.Size(26, 26);
            btnShiftDown.TabIndex = 3;
            toolTip1.SetToolTip(btnShiftDown, "Shift selected sprites down");
            btnShiftDown.Click += btnShiftDown_Click;
            // 
            // btnShiftUp
            // 
            btnShiftUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnShiftUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnShiftUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnShiftUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnShiftUp.Image = (System.Drawing.Image)resources.GetObject("btnShiftUp.Image");
            btnShiftUp.Location = new System.Drawing.Point(8, 68);
            btnShiftUp.Name = "btnShiftUp";
            btnShiftUp.Size = new System.Drawing.Size(26, 26);
            btnShiftUp.TabIndex = 2;
            toolTip1.SetToolTip(btnShiftUp, "Shift selected sprites up");
            btnShiftUp.Click += btnShiftUp_Click;
            // 
            // btnShiftRight
            // 
            btnShiftRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnShiftRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnShiftRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnShiftRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnShiftRight.Image = (System.Drawing.Image)resources.GetObject("btnShiftRight.Image");
            btnShiftRight.Location = new System.Drawing.Point(8, 37);
            btnShiftRight.Name = "btnShiftRight";
            btnShiftRight.Size = new System.Drawing.Size(26, 26);
            btnShiftRight.TabIndex = 1;
            toolTip1.SetToolTip(btnShiftRight, "Shift selected sprites right");
            btnShiftRight.Click += btnShiftRight_Click;
            // 
            // btnRotateRight
            // 
            btnRotateRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnRotateRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnRotateRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnRotateRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnRotateRight.Image = (System.Drawing.Image)resources.GetObject("btnRotateRight.Image");
            btnRotateRight.Location = new System.Drawing.Point(8, 254);
            btnRotateRight.Name = "btnRotateRight";
            btnRotateRight.Size = new System.Drawing.Size(26, 26);
            btnRotateRight.TabIndex = 8;
            toolTip1.SetToolTip(btnRotateRight, "Rotate selected sprites right");
            btnRotateRight.Click += btnRotateRight_Click;
            // 
            // btnRotateLeft
            // 
            btnRotateLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnRotateLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnRotateLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnRotateLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnRotateLeft.Image = (System.Drawing.Image)resources.GetObject("btnRotateLeft.Image");
            btnRotateLeft.Location = new System.Drawing.Point(8, 223);
            btnRotateLeft.Name = "btnRotateLeft";
            btnRotateLeft.Size = new System.Drawing.Size(26, 26);
            btnRotateLeft.TabIndex = 7;
            toolTip1.SetToolTip(btnRotateLeft, "Rotate selected sprites left");
            btnRotateLeft.Click += btnRotateLeft_Click;
            // 
            // btnShiftLeft
            // 
            btnShiftLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnShiftLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnShiftLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnShiftLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnShiftLeft.Image = (System.Drawing.Image)resources.GetObject("btnShiftLeft.Image");
            btnShiftLeft.Location = new System.Drawing.Point(8, 6);
            btnShiftLeft.Name = "btnShiftLeft";
            btnShiftLeft.Size = new System.Drawing.Size(26, 26);
            btnShiftLeft.TabIndex = 0;
            toolTip1.SetToolTip(btnShiftLeft, "Shift selected sprites left");
            btnShiftLeft.Click += btnShiftLeft_Click;
            // 
            // btnCopyToClipboard
            // 
            btnCopyToClipboard.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnCopyToClipboard.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnCopyToClipboard.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnCopyToClipboard.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnCopyToClipboard.Image = null;
            btnCopyToClipboard.Location = new System.Drawing.Point(269, 416);
            btnCopyToClipboard.Name = "btnCopyToClipboard";
            btnCopyToClipboard.Size = new System.Drawing.Size(121, 23);
            btnCopyToClipboard.TabIndex = 13;
            btnCopyToClipboard.Text = "Copy to Clipboard";
            btnCopyToClipboard.Click += btnCopyToClipboard_Click;
            // 
            // btnPasteFromClipboard
            // 
            btnPasteFromClipboard.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnPasteFromClipboard.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnPasteFromClipboard.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnPasteFromClipboard.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnPasteFromClipboard.Image = null;
            btnPasteFromClipboard.Location = new System.Drawing.Point(269, 387);
            btnPasteFromClipboard.Name = "btnPasteFromClipboard";
            btnPasteFromClipboard.Size = new System.Drawing.Size(121, 23);
            btnPasteFromClipboard.TabIndex = 12;
            btnPasteFromClipboard.Text = "Paste from Clipboard";
            btnPasteFromClipboard.Click += btnPasteFromClipboard_Click;
            // 
            // labelCharNo
            // 
            labelCharNo.Location = new System.Drawing.Point(266, 471);
            labelCharNo.Name = "labelCharNo";
            labelCharNo.Size = new System.Drawing.Size(82, 23);
            labelCharNo.TabIndex = 16;
            labelCharNo.Text = "label1";
            // 
            // checkShowGrid
            // 
            checkShowGrid.AutoSize = true;
            checkShowGrid.Location = new System.Drawing.Point(269, 365);
            checkShowGrid.Name = "checkShowGrid";
            checkShowGrid.Size = new System.Drawing.Size(75, 17);
            checkShowGrid.TabIndex = 11;
            checkShowGrid.Text = "Show Grid";
            checkShowGrid.UseVisualStyleBackColor = true;
            checkShowGrid.CheckedChanged += checkShowGrid_CheckedChanged;
            // 
            // trackGridOpacity
            // 
            trackGridOpacity.AutoSize = false;
            trackGridOpacity.Location = new System.Drawing.Point(266, 630);
            trackGridOpacity.Maximum = 255;
            trackGridOpacity.Name = "trackGridOpacity";
            trackGridOpacity.Size = new System.Drawing.Size(152, 28);
            trackGridOpacity.TabIndex = 18;
            trackGridOpacity.TickStyle = System.Windows.Forms.TickStyle.None;
            toolTip1.SetToolTip(trackGridOpacity, "Opacity of the editing grid overlay (0 = invisible, 255 = solid white).");
            trackGridOpacity.Value = 255;
            trackGridOpacity.Scroll += trackGridOpacity_Scroll;
            // 
            // labelGridOpacity
            // 
            labelGridOpacity.AutoSize = true;
            labelGridOpacity.Location = new System.Drawing.Point(266, 514);
            labelGridOpacity.Name = "labelGridOpacity";
            labelGridOpacity.Size = new System.Drawing.Size(66, 13);
            labelGridOpacity.TabIndex = 17;
            labelGridOpacity.Text = "Grid opacity:";
            // 
            // pictureEditor
            // 
            pictureEditor.AutoResize = false;
            pictureEditor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pictureEditor.DisplayPage = fastImage4;
            pictureEditor.Image = null;
            pictureEditor.Location = new System.Drawing.Point(40, 6);
            pictureEditor.Name = "pictureEditor";
            pictureEditor.Size = new System.Drawing.Size(434, 353);
            pictureEditor.TabIndex = 0;
            pictureEditor.TabStop = false;
            pictureEditor.MouseDown += pictureEditor_MouseDown;
            pictureEditor.MouseMove += pictureEditor_MouseMove;
            // 
            // tabExport
            // 
            tabExport.Controls.Add(labelCharactersTo);
            tabExport.Controls.Add(editDataExport);
            tabExport.Controls.Add(btnExport);
            tabExport.Controls.Add(comboExportMethod);
            tabExport.Controls.Add(label12);
            tabExport.Controls.Add(panelExport);
            tabExport.Controls.Add(comboExportRange);
            tabExport.Controls.Add(editSpriteCount);
            tabExport.Controls.Add(editSpriteFrom);
            tabExport.Controls.Add(labelCharactersFrom);
            tabExport.Location = new System.Drawing.Point(4, 22);
            tabExport.Name = "tabExport";
            tabExport.Padding = new System.Windows.Forms.Padding(3);
            tabExport.Size = new System.Drawing.Size(2046, 735);
            tabExport.TabIndex = 2;
            tabExport.Text = "Export";
            tabExport.UseVisualStyleBackColor = true;
            // 
            // labelCharactersTo
            // 
            labelCharactersTo.AutoSize = true;
            labelCharactersTo.Location = new System.Drawing.Point(188, 9);
            labelCharactersTo.Name = "labelCharactersTo";
            labelCharactersTo.Size = new System.Drawing.Size(37, 13);
            labelCharactersTo.TabIndex = 34;
            labelCharactersTo.Text = "count:";
            // 
            // editDataExport
            // 
            editDataExport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            editDataExport.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            editDataExport.Location = new System.Drawing.Point(541, 7);
            editDataExport.Multiline = true;
            editDataExport.Name = "editDataExport";
            editDataExport.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            editDataExport.Size = new System.Drawing.Size(1497, 722);
            editDataExport.TabIndex = 33;
            editDataExport.WordWrap = false;
            // 
            // btnExport
            // 
            btnExport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnExport.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnExport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnExport.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnExport.Image = null;
            btnExport.Location = new System.Drawing.Point(247, 33);
            btnExport.Name = "btnExport";
            btnExport.Size = new System.Drawing.Size(61, 21);
            btnExport.TabIndex = 32;
            btnExport.Text = "Export";
            btnExport.Click += btnExport_Click;
            // 
            // comboExportMethod
            // 
            comboExportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboExportMethod.FormattingEnabled = true;
            comboExportMethod.Location = new System.Drawing.Point(90, 34);
            comboExportMethod.Name = "comboExportMethod";
            comboExportMethod.Size = new System.Drawing.Size(151, 21);
            comboExportMethod.TabIndex = 30;
            comboExportMethod.SelectedIndexChanged += comboExportMethod_SelectedIndexChanged;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(8, 37);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(79, 13);
            label12.TabIndex = 31;
            label12.Text = "Export Method:";
            // 
            // panelExport
            // 
            panelExport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            panelExport.Location = new System.Drawing.Point(6, 61);
            panelExport.Name = "panelExport";
            panelExport.Size = new System.Drawing.Size(529, 668);
            panelExport.TabIndex = 28;
            // 
            // comboExportRange
            // 
            comboExportRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboExportRange.FormattingEnabled = true;
            comboExportRange.Location = new System.Drawing.Point(8, 6);
            comboExportRange.Name = "comboExportRange";
            comboExportRange.Size = new System.Drawing.Size(88, 21);
            comboExportRange.TabIndex = 18;
            comboExportRange.SelectedIndexChanged += comboExportRange_SelectedIndexChanged;
            // 
            // editSpriteCount
            // 
            editSpriteCount.Location = new System.Drawing.Point(231, 6);
            editSpriteCount.Name = "editSpriteCount";
            editSpriteCount.Size = new System.Drawing.Size(36, 20);
            editSpriteCount.TabIndex = 17;
            // 
            // editSpriteFrom
            // 
            editSpriteFrom.Location = new System.Drawing.Point(141, 6);
            editSpriteFrom.Name = "editSpriteFrom";
            editSpriteFrom.Size = new System.Drawing.Size(39, 20);
            editSpriteFrom.TabIndex = 16;
            // 
            // labelCharactersFrom
            // 
            labelCharactersFrom.AutoSize = true;
            labelCharactersFrom.Location = new System.Drawing.Point(105, 9);
            labelCharactersFrom.Name = "labelCharactersFrom";
            labelCharactersFrom.Size = new System.Drawing.Size(30, 13);
            labelCharactersFrom.TabIndex = 15;
            labelCharactersFrom.Text = "from:";
            // 
            // tabImport
            // 
            tabImport.Controls.Add(panelImport);
            tabImport.Controls.Add(btnImport);
            tabImport.Controls.Add(comboImportMethod);
            tabImport.Controls.Add(label2);
            tabImport.Location = new System.Drawing.Point(4, 22);
            tabImport.Name = "tabImport";
            tabImport.Padding = new System.Windows.Forms.Padding(3);
            tabImport.Size = new System.Drawing.Size(2046, 735);
            tabImport.TabIndex = 3;
            tabImport.Text = "Import";
            tabImport.UseVisualStyleBackColor = true;
            // 
            // panelImport
            // 
            panelImport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            panelImport.Location = new System.Drawing.Point(6, 33);
            panelImport.Name = "panelImport";
            panelImport.Size = new System.Drawing.Size(2032, 696);
            panelImport.TabIndex = 40;
            // 
            // btnImport
            // 
            btnImport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnImport.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnImport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnImport.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnImport.Image = null;
            btnImport.Location = new System.Drawing.Point(336, 5);
            btnImport.Name = "btnImport";
            btnImport.Size = new System.Drawing.Size(75, 21);
            btnImport.TabIndex = 39;
            btnImport.Text = "Import";
            btnImport.Click += btnImport_Click;
            // 
            // comboImportMethod
            // 
            comboImportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboImportMethod.FormattingEnabled = true;
            comboImportMethod.Location = new System.Drawing.Point(87, 6);
            comboImportMethod.Name = "comboImportMethod";
            comboImportMethod.Size = new System.Drawing.Size(243, 21);
            comboImportMethod.TabIndex = 37;
            comboImportMethod.SelectedIndexChanged += comboImportMethod_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 9);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(78, 13);
            label2.TabIndex = 38;
            label2.Text = "Import Method:";
            // 
            // menuStrip1
            // 
            menuStrip1.Font = new System.Drawing.Font("Segoe UI", 9F);
            menuStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(2054, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openCharsetProjectToolStripMenuItem, saveSpriteProjectToolStripMenuItem, closeCharsetProjectToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            fileToolStripMenuItem.Text = "&Sprites";
            // 
            // openCharsetProjectToolStripMenuItem
            // 
            openCharsetProjectToolStripMenuItem.Name = "openCharsetProjectToolStripMenuItem";
            openCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            openCharsetProjectToolStripMenuItem.Text = "&Open Sprite Project...";
            openCharsetProjectToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveSpriteProjectToolStripMenuItem
            // 
            saveSpriteProjectToolStripMenuItem.Name = "saveSpriteProjectToolStripMenuItem";
            saveSpriteProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            saveSpriteProjectToolStripMenuItem.Text = "&Save Project";
            saveSpriteProjectToolStripMenuItem.Click += saveCharsetProjectToolStripMenuItem_Click;
            // 
            // closeCharsetProjectToolStripMenuItem
            // 
            closeCharsetProjectToolStripMenuItem.Enabled = false;
            closeCharsetProjectToolStripMenuItem.Name = "closeCharsetProjectToolStripMenuItem";
            closeCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            closeCharsetProjectToolStripMenuItem.Text = "&Close Sprite Project";
            closeCharsetProjectToolStripMenuItem.Click += closeCharsetProjectToolStripMenuItem_Click;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Nr.";
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "X";
            columnHeader5.Width = 30;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "Y";
            columnHeader6.Width = 30;
            // 
            // contextMenuChangeMode
            // 
            contextMenuChangeMode.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { c64HiResMultiColorToolStripMenuItem, mega65ToolStripMenuItem, commanderX16ToolStripMenuItem });
            contextMenuChangeMode.Name = "contextMenuChangeMode";
            contextMenuChangeMode.Size = new System.Drawing.Size(223, 70);
            // 
            // c64HiResMultiColorToolStripMenuItem
            // 
            c64HiResMultiColorToolStripMenuItem.Name = "c64HiResMultiColorToolStripMenuItem";
            c64HiResMultiColorToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            c64HiResMultiColorToolStripMenuItem.Text = "C64 HiRes/MultiColor 24x21";
            c64HiResMultiColorToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // mega65ToolStripMenuItem
            // 
            mega65ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { mega65_24x214ColorsToolStripMenuItem, mega65_64x214ColorsToolStripMenuItem, mega65_16x2116ColorsToolStripMenuItem });
            mega65ToolStripMenuItem.Name = "mega65ToolStripMenuItem";
            mega65ToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            mega65ToolStripMenuItem.Text = "Mega65";
            // 
            // mega65_24x214ColorsToolStripMenuItem
            // 
            mega65_24x214ColorsToolStripMenuItem.Name = "mega65_24x214ColorsToolStripMenuItem";
            mega65_24x214ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            mega65_24x214ColorsToolStripMenuItem.Text = "24x21 4 Colors";
            mega65_24x214ColorsToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // mega65_64x214ColorsToolStripMenuItem
            // 
            mega65_64x214ColorsToolStripMenuItem.Name = "mega65_64x214ColorsToolStripMenuItem";
            mega65_64x214ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            mega65_64x214ColorsToolStripMenuItem.Text = "64x21 4 Colors";
            mega65_64x214ColorsToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // mega65_16x2116ColorsToolStripMenuItem
            // 
            mega65_16x2116ColorsToolStripMenuItem.Name = "mega65_16x2116ColorsToolStripMenuItem";
            mega65_16x2116ColorsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            mega65_16x2116ColorsToolStripMenuItem.Text = "16x21 16 Colors";
            mega65_16x2116ColorsToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // commanderX16ToolStripMenuItem
            // 
            commanderX16ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { x16_16ColorsToolStripMenuItem, x16_256ColorsToolStripMenuItem });
            commanderX16ToolStripMenuItem.Name = "commanderX16ToolStripMenuItem";
            commanderX16ToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            commanderX16ToolStripMenuItem.Text = "Commander X16";
            // 
            // x16_16ColorsToolStripMenuItem
            // 
            x16_16ColorsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { x16_8x8ToolStripMenuItem, x16_16x8ToolStripMenuItem, x16_32x8ToolStripMenuItem, x16_64x8ToolStripMenuItem, x16_8x16ToolStripMenuItem, x16_16x16ToolStripMenuItem, x16_32x16ToolStripMenuItem, x16_64x16ToolStripMenuItem, x16_8x32ToolStripMenuItem, x16_16x32ToolStripMenuItem, x16_32x32ToolStripMenuItem, x16_64x32ToolStripMenuItem, x16_8x64ToolStripMenuItem, x16_16x64ToolStripMenuItem, x16_32x64ToolStripMenuItem, x16_64x64ToolStripMenuItem });
            x16_16ColorsToolStripMenuItem.Name = "x16_16ColorsToolStripMenuItem";
            x16_16ColorsToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            x16_16ColorsToolStripMenuItem.Text = "16 Colors";
            // 
            // x16_8x8ToolStripMenuItem
            // 
            x16_8x8ToolStripMenuItem.Name = "x16_8x8ToolStripMenuItem";
            x16_8x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x8ToolStripMenuItem.Text = "8x8";
            x16_8x8ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x8ToolStripMenuItem
            // 
            x16_16x8ToolStripMenuItem.Name = "x16_16x8ToolStripMenuItem";
            x16_16x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x8ToolStripMenuItem.Text = "16x8";
            x16_16x8ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x8ToolStripMenuItem
            // 
            x16_32x8ToolStripMenuItem.Name = "x16_32x8ToolStripMenuItem";
            x16_32x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x8ToolStripMenuItem.Text = "32x8";
            x16_32x8ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x8ToolStripMenuItem
            // 
            x16_64x8ToolStripMenuItem.Name = "x16_64x8ToolStripMenuItem";
            x16_64x8ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x8ToolStripMenuItem.Text = "64x8";
            x16_64x8ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x16ToolStripMenuItem
            // 
            x16_8x16ToolStripMenuItem.Name = "x16_8x16ToolStripMenuItem";
            x16_8x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x16ToolStripMenuItem.Text = "8x16";
            x16_8x16ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x16ToolStripMenuItem
            // 
            x16_16x16ToolStripMenuItem.Name = "x16_16x16ToolStripMenuItem";
            x16_16x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x16ToolStripMenuItem.Text = "16x16";
            x16_16x16ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x16ToolStripMenuItem
            // 
            x16_32x16ToolStripMenuItem.Name = "x16_32x16ToolStripMenuItem";
            x16_32x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x16ToolStripMenuItem.Text = "32x16";
            x16_32x16ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x16ToolStripMenuItem
            // 
            x16_64x16ToolStripMenuItem.Name = "x16_64x16ToolStripMenuItem";
            x16_64x16ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x16ToolStripMenuItem.Text = "64x16";
            x16_64x16ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x32ToolStripMenuItem
            // 
            x16_8x32ToolStripMenuItem.Name = "x16_8x32ToolStripMenuItem";
            x16_8x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x32ToolStripMenuItem.Text = "8x32";
            x16_8x32ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x32ToolStripMenuItem
            // 
            x16_16x32ToolStripMenuItem.Name = "x16_16x32ToolStripMenuItem";
            x16_16x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x32ToolStripMenuItem.Text = "16x32";
            x16_16x32ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x32ToolStripMenuItem
            // 
            x16_32x32ToolStripMenuItem.Name = "x16_32x32ToolStripMenuItem";
            x16_32x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x32ToolStripMenuItem.Text = "32x32";
            x16_32x32ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x32ToolStripMenuItem
            // 
            x16_64x32ToolStripMenuItem.Name = "x16_64x32ToolStripMenuItem";
            x16_64x32ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x32ToolStripMenuItem.Text = "64x32";
            x16_64x32ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x64ToolStripMenuItem
            // 
            x16_8x64ToolStripMenuItem.Name = "x16_8x64ToolStripMenuItem";
            x16_8x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x64ToolStripMenuItem.Text = "8x64";
            x16_8x64ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x64ToolStripMenuItem
            // 
            x16_16x64ToolStripMenuItem.Name = "x16_16x64ToolStripMenuItem";
            x16_16x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x64ToolStripMenuItem.Text = "16x64";
            x16_16x64ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x64ToolStripMenuItem
            // 
            x16_32x64ToolStripMenuItem.Name = "x16_32x64ToolStripMenuItem";
            x16_32x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x64ToolStripMenuItem.Text = "32x64";
            x16_32x64ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x64ToolStripMenuItem
            // 
            x16_64x64ToolStripMenuItem.Name = "x16_64x64ToolStripMenuItem";
            x16_64x64ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x64ToolStripMenuItem.Text = "64x64";
            x16_64x64ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_256ColorsToolStripMenuItem
            // 
            x16_256ColorsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { x16_8x8x256ToolStripMenuItem, x16_16x8x256ToolStripMenuItem, x16_32x8x256ToolStripMenuItem, x16_64x8x256ToolStripMenuItem, x16_8x16x256ToolStripMenuItem, x16_16x16x256ToolStripMenuItem, x16_32x16x256ToolStripMenuItem, x16_64x16x256ToolStripMenuItem, x16_8x32x256ToolStripMenuItem, x16_16x32x256ToolStripMenuItem, x16_32x32x256ToolStripMenuItem, x16_64x32x256ToolStripMenuItem, x16_8x64x256ToolStripMenuItem, x16_16x64x256ToolStripMenuItem, x16_32x64x256ToolStripMenuItem, x16_64x64x256ToolStripMenuItem });
            x16_256ColorsToolStripMenuItem.Name = "x16_256ColorsToolStripMenuItem";
            x16_256ColorsToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
            x16_256ColorsToolStripMenuItem.Text = "256 Colors";
            // 
            // x16_8x8x256ToolStripMenuItem
            // 
            x16_8x8x256ToolStripMenuItem.Name = "x16_8x8x256ToolStripMenuItem";
            x16_8x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x8x256ToolStripMenuItem.Text = "8x8";
            x16_8x8x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x8x256ToolStripMenuItem
            // 
            x16_16x8x256ToolStripMenuItem.Name = "x16_16x8x256ToolStripMenuItem";
            x16_16x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x8x256ToolStripMenuItem.Text = "16x8";
            x16_16x8x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x8x256ToolStripMenuItem
            // 
            x16_32x8x256ToolStripMenuItem.Name = "x16_32x8x256ToolStripMenuItem";
            x16_32x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x8x256ToolStripMenuItem.Text = "32x8";
            x16_32x8x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x8x256ToolStripMenuItem
            // 
            x16_64x8x256ToolStripMenuItem.Name = "x16_64x8x256ToolStripMenuItem";
            x16_64x8x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x8x256ToolStripMenuItem.Text = "64x8";
            x16_64x8x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x16x256ToolStripMenuItem
            // 
            x16_8x16x256ToolStripMenuItem.Name = "x16_8x16x256ToolStripMenuItem";
            x16_8x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x16x256ToolStripMenuItem.Text = "8x16";
            x16_8x16x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x16x256ToolStripMenuItem
            // 
            x16_16x16x256ToolStripMenuItem.Name = "x16_16x16x256ToolStripMenuItem";
            x16_16x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x16x256ToolStripMenuItem.Text = "16x16";
            x16_16x16x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x16x256ToolStripMenuItem
            // 
            x16_32x16x256ToolStripMenuItem.Name = "x16_32x16x256ToolStripMenuItem";
            x16_32x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x16x256ToolStripMenuItem.Text = "32x16";
            x16_32x16x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x16x256ToolStripMenuItem
            // 
            x16_64x16x256ToolStripMenuItem.Name = "x16_64x16x256ToolStripMenuItem";
            x16_64x16x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x16x256ToolStripMenuItem.Text = "64x16";
            x16_64x16x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x32x256ToolStripMenuItem
            // 
            x16_8x32x256ToolStripMenuItem.Name = "x16_8x32x256ToolStripMenuItem";
            x16_8x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x32x256ToolStripMenuItem.Text = "8x32";
            x16_8x32x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x32x256ToolStripMenuItem
            // 
            x16_16x32x256ToolStripMenuItem.Name = "x16_16x32x256ToolStripMenuItem";
            x16_16x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x32x256ToolStripMenuItem.Text = "16x32";
            x16_16x32x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x32x256ToolStripMenuItem
            // 
            x16_32x32x256ToolStripMenuItem.Name = "x16_32x32x256ToolStripMenuItem";
            x16_32x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x32x256ToolStripMenuItem.Text = "32x32";
            x16_32x32x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x32x256ToolStripMenuItem
            // 
            x16_64x32x256ToolStripMenuItem.Name = "x16_64x32x256ToolStripMenuItem";
            x16_64x32x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x32x256ToolStripMenuItem.Text = "64x32";
            x16_64x32x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_8x64x256ToolStripMenuItem
            // 
            x16_8x64x256ToolStripMenuItem.Name = "x16_8x64x256ToolStripMenuItem";
            x16_8x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_8x64x256ToolStripMenuItem.Text = "8x64";
            x16_8x64x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_16x64x256ToolStripMenuItem
            // 
            x16_16x64x256ToolStripMenuItem.Name = "x16_16x64x256ToolStripMenuItem";
            x16_16x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_16x64x256ToolStripMenuItem.Text = "16x64";
            x16_16x64x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_32x64x256ToolStripMenuItem
            // 
            x16_32x64x256ToolStripMenuItem.Name = "x16_32x64x256ToolStripMenuItem";
            x16_32x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_32x64x256ToolStripMenuItem.Text = "32x64";
            x16_32x64x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // x16_64x64x256ToolStripMenuItem
            // 
            x16_64x64x256ToolStripMenuItem.Name = "x16_64x64x256ToolStripMenuItem";
            x16_64x64x256ToolStripMenuItem.Size = new System.Drawing.Size(104, 22);
            x16_64x64x256ToolStripMenuItem.Text = "64x64";
            x16_64x64x256ToolStripMenuItem.Click += spriteModeChangedMenuItem_Click;
            // 
            // tabPageOverlay
            // 
            tabPageOverlay.Controls.Add(panel1);
            tabPageOverlay.Controls.Add(btnAddOverlay);
            tabPageOverlay.Controls.Add(btnRemoveOverlay);
            tabPageOverlay.Controls.Add(btnCloneOverlay);
            tabPageOverlay.Controls.Add(listOverlays);
            tabPageOverlay.Controls.Add(labelOverlayName);
            tabPageOverlay.Controls.Add(editOverlayName);
            tabPageOverlay.Controls.Add(panelSprites);
            tabPageOverlay.Controls.Add(panelOverlaySlots);
            tabPageOverlay.Controls.Add(picOverlayPreview);
            tabPageOverlay.Controls.Add(btnOverlayZoomOut);
            tabPageOverlay.Controls.Add(btnOverlayZoomIn);
            tabPageOverlay.Controls.Add(labelOverlayZoom);
            tabPageOverlay.Location = new System.Drawing.Point(4, 22);
            tabPageOverlay.Name = "tabPageOverlay";
            tabPageOverlay.Padding = new System.Windows.Forms.Padding(3);
            tabPageOverlay.Size = new System.Drawing.Size(1558, 634);
            tabPageOverlay.TabIndex = 2;
            tabPageOverlay.Text = "Animation";
            tabPageOverlay.UseVisualStyleBackColor = true;
            // 
            // panelSprites
            // 
            panelSprites.AllowPopup = false;
            panelSprites.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            panelSprites.AutoScroll = true;
            panelSprites.AutoScrollHorizontalMaximum = 100;
            panelSprites.AutoScrollHorizontalMinimum = 0;
            panelSprites.AutoScrollHPos = 0;
            panelSprites.AutoScrollVerticalMaximum = -23;
            panelSprites.AutoScrollVerticalMinimum = 0;
            panelSprites.AutoScrollVPos = 0;
            panelSprites.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelSprites.EnableAutoScrollHorizontal = false;
            panelSprites.EnableAutoScrollVertical = true;
            panelSprites.GridColor = System.Drawing.SystemColors.ControlDark;
            panelSprites.HottrackColor = 2151694591U;
            panelSprites.ItemHeight = 21;
            panelSprites.ItemWidth = 24;
            panelSprites.Location = new System.Drawing.Point(316, 277);
            panelSprites.Name = "panelSprites";
            panelSprites.PixelFormat = GR.Drawing.PixelFormat.DontCare;
            panelSprites.SelectedIndex = -1;
            panelSprites.ShowGrid = false;
            panelSprites.Size = new System.Drawing.Size(302, 187);
            panelSprites.TabIndex = 4;
            panelSprites.TabStop = true;
            panelSprites.VisibleAutoScrollHorizontal = false;
            panelSprites.VisibleAutoScrollVertical = false;
            panelSprites.SelectedIndexChanged += panelSprites_SelectedIndexChanged;
            panelSprites.SelectionChanged += panelSprites_SelectionChanged;
            panelSprites.ClientSizeChanged += panelSprites_ClientSizeChanged;
            // 
            // labelOverlayZoom
            // 
            labelOverlayZoom.AutoSize = true;
            labelOverlayZoom.Location = new System.Drawing.Point(217, 475);
            labelOverlayZoom.Name = "labelOverlayZoom";
            labelOverlayZoom.Size = new System.Drawing.Size(51, 13);
            labelOverlayZoom.TabIndex = 9;
            labelOverlayZoom.Text = "Zoom: 1x";
            // 
            // btnOverlayZoomIn
            // 
            btnOverlayZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnOverlayZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnOverlayZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnOverlayZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOverlayZoomIn.Image = null;
            btnOverlayZoomIn.Location = new System.Drawing.Point(177, 470);
            btnOverlayZoomIn.Name = "btnOverlayZoomIn";
            btnOverlayZoomIn.Size = new System.Drawing.Size(30, 24);
            btnOverlayZoomIn.TabIndex = 8;
            btnOverlayZoomIn.Text = "+";
            toolTip1.SetToolTip(btnOverlayZoomIn, "Zoom in");
            btnOverlayZoomIn.Click += btnOverlayZoomIn_Click;
            // 
            // btnOverlayZoomOut
            // 
            btnOverlayZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnOverlayZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnOverlayZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnOverlayZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOverlayZoomOut.Image = null;
            btnOverlayZoomOut.Location = new System.Drawing.Point(142, 470);
            btnOverlayZoomOut.Name = "btnOverlayZoomOut";
            btnOverlayZoomOut.Size = new System.Drawing.Size(30, 24);
            btnOverlayZoomOut.TabIndex = 7;
            btnOverlayZoomOut.Text = "-";
            toolTip1.SetToolTip(btnOverlayZoomOut, "Zoom out");
            btnOverlayZoomOut.Click += btnOverlayZoomOut_Click;
            // 
            // picOverlayPreview
            // 
            picOverlayPreview.AutoResize = false;
            picOverlayPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picOverlayPreview.DisplayPage = fastImage3;
            picOverlayPreview.Image = null;
            picOverlayPreview.Location = new System.Drawing.Point(140, 277);
            picOverlayPreview.Name = "picOverlayPreview";
            picOverlayPreview.Size = new System.Drawing.Size(170, 187);
            picOverlayPreview.TabIndex = 6;
            picOverlayPreview.TabStop = false;
            // 
            // panelOverlaySlots
            // 
            panelOverlaySlots.Location = new System.Drawing.Point(140, 56);
            panelOverlaySlots.Name = "panelOverlaySlots";
            panelOverlaySlots.Size = new System.Drawing.Size(328, 215);
            panelOverlaySlots.TabIndex = 5;
            // 
            // editOverlayName
            // 
            editOverlayName.Location = new System.Drawing.Point(182, 30);
            editOverlayName.Name = "editOverlayName";
            editOverlayName.Size = new System.Drawing.Size(200, 20);
            editOverlayName.TabIndex = 4;
            editOverlayName.TextChanged += editOverlayName_TextChanged;
            // 
            // labelOverlayName
            // 
            labelOverlayName.AutoSize = true;
            labelOverlayName.Location = new System.Drawing.Point(142, 33);
            labelOverlayName.Name = "labelOverlayName";
            labelOverlayName.Size = new System.Drawing.Size(38, 13);
            labelOverlayName.TabIndex = 3;
            labelOverlayName.Text = "Name:";
            // 
            // listOverlays
            // 
            listOverlays.FormattingEnabled = true;
            listOverlays.IntegralHeight = false;
            listOverlays.ItemHeight = 13;
            listOverlays.Location = new System.Drawing.Point(5, 33);
            listOverlays.Name = "listOverlays";
            listOverlays.Size = new System.Drawing.Size(130, 470);
            listOverlays.TabIndex = 2;
            listOverlays.SelectedIndexChanged += listOverlays_SelectedIndexChanged;
            // 
            // btnRemoveOverlay
            // 
            btnRemoveOverlay.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnRemoveOverlay.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnRemoveOverlay.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnRemoveOverlay.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnRemoveOverlay.Enabled = false;
            btnRemoveOverlay.Image = null;
            btnRemoveOverlay.Location = new System.Drawing.Point(68, 5);
            btnRemoveOverlay.Name = "btnRemoveOverlay";
            btnRemoveOverlay.Size = new System.Drawing.Size(60, 23);
            btnRemoveOverlay.TabIndex = 1;
            btnRemoveOverlay.Text = "Remove";
            btnRemoveOverlay.Click += btnRemoveOverlay_Click;
            // 
            // btnAddOverlay
            // 
            btnAddOverlay.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnAddOverlay.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnAddOverlay.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnAddOverlay.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnAddOverlay.Image = null;
            btnAddOverlay.Location = new System.Drawing.Point(5, 5);
            btnAddOverlay.Name = "btnAddOverlay";
            btnAddOverlay.Size = new System.Drawing.Size(60, 23);
            btnAddOverlay.TabIndex = 0;
            btnAddOverlay.Text = "Add";
            btnAddOverlay.Click += btnAddOverlay_Click;
            //
            // btnCloneOverlay
            //
            btnCloneOverlay.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnCloneOverlay.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnCloneOverlay.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnCloneOverlay.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnCloneOverlay.Enabled = false;
            btnCloneOverlay.Image = null;
            btnCloneOverlay.Location = new System.Drawing.Point(131, 5);
            btnCloneOverlay.Name = "btnCloneOverlay";
            btnCloneOverlay.Size = new System.Drawing.Size(60, 23);
            btnCloneOverlay.TabIndex = 2;
            btnCloneOverlay.Text = "Clone";
            btnCloneOverlay.Click += btnCloneOverlay_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(panelSpriteTest);
            panel1.Controls.Add(labelTestBack);
            panel1.Controls.Add(comboTestBackColor);
            panel1.Controls.Add(checkTestExpandX);
            panel1.Controls.Add(checkTestExpandY);
            panel1.Controls.Add(checkTestLoop);
            panel1.Controls.Add(checkTestC64Mag);
            panel1.Controls.Add(labelTestTarget);
            panel1.Controls.Add(editTestTargetW);
            panel1.Controls.Add(labelTestTargetX);
            panel1.Controls.Add(editTestTargetH);
            panel1.Controls.Add(labelTestMagResult);
            panel1.Controls.Add(btnTestStop);
            panel1.Controls.Add(btnAddFrame);
            panel1.Controls.Add(labelAnimZoom);
            panel1.Controls.Add(btnRemoveFrame);
            panel1.Controls.Add(btnAnimZoomIn);
            panel1.Controls.Add(btnDuplicateFrame);
            panel1.Controls.Add(btnAnimZoomOut);
            panel1.Controls.Add(btnPlayAnim);
            panel1.Controls.Add(picAnimPreview);
            panel1.Controls.Add(btnPauseAnim);
            panel1.Controls.Add(panelFrameSlots);
            panel1.Controls.Add(listAnimFrames);
            panel1.Controls.Add(labelFrameDelay);
            panel1.Controls.Add(editFrameDelay);
            panel1.Controls.Add(checkLoop);
            panel1.Controls.Add(checkStartRandomFrame);
            panel1.Controls.Add(labelAnimationID);
            panel1.Controls.Add(editAnimationID);
            panel1.Location = new System.Drawing.Point(624, 5);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(784, 568);
            panel1.TabIndex = 14;
            panel1.Paint += panel1_Paint;
            // 
            // editAnimationID
            // 
            editAnimationID.Location = new System.Drawing.Point(455, 37);
            editAnimationID.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            editAnimationID.Name = "editAnimationID";
            editAnimationID.Size = new System.Drawing.Size(60, 20);
            editAnimationID.TabIndex = 10;
            editAnimationID.ValueChanged += editAnimationID_ValueChanged;
            // 
            // labelAnimationID
            // 
            labelAnimationID.AutoSize = true;
            labelAnimationID.Location = new System.Drawing.Point(400, 40);
            labelAnimationID.Name = "labelAnimationID";
            labelAnimationID.Size = new System.Drawing.Size(47, 13);
            labelAnimationID.TabIndex = 9;
            labelAnimationID.Text = "Anim ID:";
            // 
            // checkLoop
            // 
            checkLoop.AutoSize = true;
            checkLoop.Location = new System.Drawing.Point(330, 39);
            checkLoop.Name = "checkLoop";
            checkLoop.Size = new System.Drawing.Size(50, 17);
            checkLoop.TabIndex = 8;
            checkLoop.Text = "Loop";
            checkLoop.UseVisualStyleBackColor = true;
            checkLoop.CheckedChanged += checkLoop_CheckedChanged;
            //
            // checkStartRandomFrame
            //
            checkStartRandomFrame.AutoSize = true;
            checkStartRandomFrame.Location = new System.Drawing.Point(149, 62);
            checkStartRandomFrame.Name = "checkStartRandomFrame";
            checkStartRandomFrame.Size = new System.Drawing.Size(127, 17);
            checkStartRandomFrame.TabIndex = 9;
            checkStartRandomFrame.Text = "Start at random frame";
            checkStartRandomFrame.UseVisualStyleBackColor = true;
            checkStartRandomFrame.CheckedChanged += checkStartRandomFrame_CheckedChanged;
            //
            // editFrameDelay
            // 
            editFrameDelay.Location = new System.Drawing.Point(235, 37);
            editFrameDelay.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            editFrameDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            editFrameDelay.Name = "editFrameDelay";
            editFrameDelay.Size = new System.Drawing.Size(80, 20);
            editFrameDelay.TabIndex = 7;
            editFrameDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });
            editFrameDelay.ValueChanged += editFrameDelay_ValueChanged;
            // 
            // labelFrameDelay
            // 
            labelFrameDelay.AutoSize = true;
            labelFrameDelay.Location = new System.Drawing.Point(149, 40);
            labelFrameDelay.Name = "labelFrameDelay";
            labelFrameDelay.Size = new System.Drawing.Size(74, 13);
            labelFrameDelay.TabIndex = 6;
            labelFrameDelay.Text = "Delay (1/50s):";
            // 
            // listAnimFrames
            // 
            listAnimFrames.FormattingEnabled = true;
            listAnimFrames.IntegralHeight = false;
            listAnimFrames.ItemHeight = 13;
            listAnimFrames.Location = new System.Drawing.Point(14, 35);
            listAnimFrames.Name = "listAnimFrames";
            listAnimFrames.Size = new System.Drawing.Size(130, 468);
            listAnimFrames.TabIndex = 5;
            listAnimFrames.SelectedIndexChanged += listAnimFrames_SelectedIndexChanged;
            // 
            // panelFrameSlots
            // 
            panelFrameSlots.Location = new System.Drawing.Point(154, 119);
            panelFrameSlots.Name = "panelFrameSlots";
            panelFrameSlots.Size = new System.Drawing.Size(200, 180);
            panelFrameSlots.TabIndex = 8;
            // 
            // btnPauseAnim
            // 
            btnPauseAnim.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnPauseAnim.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnPauseAnim.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnPauseAnim.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnPauseAnim.Enabled = false;
            btnPauseAnim.Image = null;
            btnPauseAnim.Location = new System.Drawing.Point(412, 7);
            btnPauseAnim.Name = "btnPauseAnim";
            btnPauseAnim.Size = new System.Drawing.Size(60, 23);
            btnPauseAnim.TabIndex = 4;
            btnPauseAnim.Text = "Pause";
            btnPauseAnim.Click += btnPauseAnim_Click;
            // 
            // picAnimPreview
            // 
            picAnimPreview.AutoResize = false;
            picAnimPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picAnimPreview.DisplayPage = fastImage2;
            picAnimPreview.Image = null;
            picAnimPreview.Location = new System.Drawing.Point(360, 67);
            picAnimPreview.Name = "picAnimPreview";
            picAnimPreview.Size = new System.Drawing.Size(208, 183);
            picAnimPreview.TabIndex = 9;
            picAnimPreview.TabStop = false;
            // 
            // btnPlayAnim
            // 
            btnPlayAnim.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnPlayAnim.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnPlayAnim.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnPlayAnim.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnPlayAnim.Image = null;
            btnPlayAnim.Location = new System.Drawing.Point(349, 7);
            btnPlayAnim.Name = "btnPlayAnim";
            btnPlayAnim.Size = new System.Drawing.Size(60, 23);
            btnPlayAnim.TabIndex = 3;
            btnPlayAnim.Text = "Play";
            btnPlayAnim.Click += btnPlayAnim_Click;
            // 
            // btnAnimZoomOut
            // 
            btnAnimZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnAnimZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnAnimZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnAnimZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnAnimZoomOut.Image = null;
            btnAnimZoomOut.Location = new System.Drawing.Point(360, 258);
            btnAnimZoomOut.Name = "btnAnimZoomOut";
            btnAnimZoomOut.Size = new System.Drawing.Size(30, 23);
            btnAnimZoomOut.TabIndex = 11;
            btnAnimZoomOut.Text = "-";
            toolTip1.SetToolTip(btnAnimZoomOut, "Zoom out");
            btnAnimZoomOut.Click += btnAnimZoomOut_Click;
            // 
            // btnDuplicateFrame
            // 
            btnDuplicateFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnDuplicateFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnDuplicateFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnDuplicateFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnDuplicateFrame.Enabled = false;
            btnDuplicateFrame.Image = null;
            btnDuplicateFrame.Location = new System.Drawing.Point(140, 7);
            btnDuplicateFrame.Name = "btnDuplicateFrame";
            btnDuplicateFrame.Size = new System.Drawing.Size(80, 23);
            btnDuplicateFrame.TabIndex = 2;
            btnDuplicateFrame.Text = "Duplicate";
            btnDuplicateFrame.Click += btnDuplicateFrame_Click;
            // 
            // btnAnimZoomIn
            // 
            btnAnimZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnAnimZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnAnimZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnAnimZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnAnimZoomIn.Image = null;
            btnAnimZoomIn.Location = new System.Drawing.Point(395, 258);
            btnAnimZoomIn.Name = "btnAnimZoomIn";
            btnAnimZoomIn.Size = new System.Drawing.Size(30, 23);
            btnAnimZoomIn.TabIndex = 12;
            btnAnimZoomIn.Text = "+";
            toolTip1.SetToolTip(btnAnimZoomIn, "Zoom in");
            btnAnimZoomIn.Click += btnAnimZoomIn_Click;
            // 
            // btnRemoveFrame
            // 
            btnRemoveFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnRemoveFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnRemoveFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnRemoveFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnRemoveFrame.Enabled = false;
            btnRemoveFrame.Image = null;
            btnRemoveFrame.Location = new System.Drawing.Point(77, 7);
            btnRemoveFrame.Name = "btnRemoveFrame";
            btnRemoveFrame.Size = new System.Drawing.Size(60, 23);
            btnRemoveFrame.TabIndex = 1;
            btnRemoveFrame.Text = "Remove";
            btnRemoveFrame.Click += btnRemoveFrame_Click;
            // 
            // labelAnimZoom
            // 
            labelAnimZoom.AutoSize = true;
            labelAnimZoom.Location = new System.Drawing.Point(435, 263);
            labelAnimZoom.Name = "labelAnimZoom";
            labelAnimZoom.Size = new System.Drawing.Size(51, 13);
            labelAnimZoom.TabIndex = 13;
            labelAnimZoom.Text = "Zoom: 1x";
            // 
            // btnAddFrame
            // 
            btnAddFrame.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnAddFrame.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnAddFrame.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnAddFrame.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnAddFrame.Image = null;
            btnAddFrame.Location = new System.Drawing.Point(14, 7);
            btnAddFrame.Name = "btnAddFrame";
            btnAddFrame.Size = new System.Drawing.Size(60, 23);
            btnAddFrame.TabIndex = 0;
            btnAddFrame.Text = "Add";
            btnAddFrame.Click += btnAddFrame_Click;
            // 
            // btnTestStop
            // 
            btnTestStop.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            btnTestStop.BorderStyle = DecentForms.BorderStyle.FLAT;
            btnTestStop.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            btnTestStop.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnTestStop.Image = null;
            btnTestStop.Location = new System.Drawing.Point(574, 476);
            btnTestStop.Name = "btnTestStop";
            btnTestStop.Size = new System.Drawing.Size(80, 24);
            btnTestStop.TabIndex = 20;
            btnTestStop.Text = "Stop";
            btnTestStop.Click += btnTestStop_Click;
            // 
            // labelTestMagResult
            // 
            labelTestMagResult.AutoSize = true;
            labelTestMagResult.Location = new System.Drawing.Point(574, 551);
            labelTestMagResult.Name = "labelTestMagResult";
            labelTestMagResult.Size = new System.Drawing.Size(103, 13);
            labelTestMagResult.TabIndex = 25;
            labelTestMagResult.Text = "= 1.0x wide, 1.0x tall";
            // 
            // editTestTargetH
            // 
            editTestTargetH.Location = new System.Drawing.Point(708, 527);
            editTestTargetH.Name = "editTestTargetH";
            editTestTargetH.Size = new System.Drawing.Size(44, 20);
            editTestTargetH.TabIndex = 24;
            toolTip1.SetToolTip(editTestTargetH, "C64 screen height in pixels you are targeting (e.g. 200; raise to include borders / for PAL).");
            editTestTargetH.TextChanged += editTestTargetH_TextChanged;
            // 
            // labelTestTargetX
            // 
            labelTestTargetX.AutoSize = true;
            labelTestTargetX.Location = new System.Drawing.Point(692, 530);
            labelTestTargetX.Name = "labelTestTargetX";
            labelTestTargetX.Size = new System.Drawing.Size(12, 13);
            labelTestTargetX.TabIndex = 24;
            labelTestTargetX.Text = "x";
            // 
            // editTestTargetW
            // 
            editTestTargetW.Location = new System.Drawing.Point(644, 527);
            editTestTargetW.Name = "editTestTargetW";
            editTestTargetW.Size = new System.Drawing.Size(44, 20);
            editTestTargetW.TabIndex = 23;
            toolTip1.SetToolTip(editTestTargetW, "C64 screen width in pixels you are targeting (e.g. 320; raise to include borders / for PAL).");
            editTestTargetW.TextChanged += editTestTargetW_TextChanged;
            // 
            // labelTestTarget
            // 
            labelTestTarget.AutoSize = true;
            labelTestTarget.Location = new System.Drawing.Point(574, 530);
            labelTestTarget.Name = "labelTestTarget";
            labelTestTarget.Size = new System.Drawing.Size(64, 13);
            labelTestTarget.TabIndex = 22;
            labelTestTarget.Text = "C64 screen:";
            // 
            // checkTestC64Mag
            // 
            checkTestC64Mag.AutoSize = true;
            checkTestC64Mag.Location = new System.Drawing.Point(574, 505);
            checkTestC64Mag.Name = "checkTestC64Mag";
            checkTestC64Mag.Size = new System.Drawing.Size(132, 17);
            checkTestC64Mag.TabIndex = 21;
            checkTestC64Mag.Text = "Use C64 magnification";
            toolTip1.SetToolTip(checkTestC64Mag, "Magnify the playfield so sprites appear at the size they would on your monitor when the C64 screen (below) fills it.");
            checkTestC64Mag.UseVisualStyleBackColor = true;
            checkTestC64Mag.CheckedChanged += checkTestC64Mag_CheckedChanged;
            // 
            // checkTestLoop
            // 
            checkTestLoop.AutoSize = true;
            checkTestLoop.Location = new System.Drawing.Point(574, 450);
            checkTestLoop.Name = "checkTestLoop";
            checkTestLoop.Size = new System.Drawing.Size(50, 17);
            checkTestLoop.TabIndex = 19;
            checkTestLoop.Text = "Loop";
            checkTestLoop.UseVisualStyleBackColor = true;
            checkTestLoop.CheckedChanged += checkTestLoop_CheckedChanged;
            // 
            // checkTestExpandY
            // 
            checkTestExpandY.AutoSize = true;
            checkTestExpandY.Location = new System.Drawing.Point(574, 427);
            checkTestExpandY.Name = "checkTestExpandY";
            checkTestExpandY.Size = new System.Drawing.Size(72, 17);
            checkTestExpandY.TabIndex = 18;
            checkTestExpandY.Text = "Expand Y";
            checkTestExpandY.UseVisualStyleBackColor = true;
            checkTestExpandY.CheckedChanged += checkTestExpandY_CheckedChanged;
            // 
            // checkTestExpandX
            // 
            checkTestExpandX.AutoSize = true;
            checkTestExpandX.Location = new System.Drawing.Point(574, 404);
            checkTestExpandX.Name = "checkTestExpandX";
            checkTestExpandX.Size = new System.Drawing.Size(72, 17);
            checkTestExpandX.TabIndex = 17;
            checkTestExpandX.Text = "Expand X";
            checkTestExpandX.UseVisualStyleBackColor = true;
            checkTestExpandX.CheckedChanged += checkTestExpandX_CheckedChanged;
            // 
            // comboTestBackColor
            // 
            comboTestBackColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            comboTestBackColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboTestBackColor.FormattingEnabled = true;
            comboTestBackColor.Location = new System.Drawing.Point(574, 374);
            comboTestBackColor.Name = "comboTestBackColor";
            comboTestBackColor.Size = new System.Drawing.Size(160, 21);
            comboTestBackColor.TabIndex = 16;
            comboTestBackColor.DrawItem += comboTestBackColor_DrawItem;
            comboTestBackColor.SelectedIndexChanged += comboTestBackColor_SelectedIndexChanged;
            // 
            // labelTestBack
            // 
            labelTestBack.AutoSize = true;
            labelTestBack.Location = new System.Drawing.Point(574, 358);
            labelTestBack.Name = "labelTestBack";
            labelTestBack.Size = new System.Drawing.Size(68, 13);
            labelTestBack.TabIndex = 15;
            labelTestBack.Text = "Background:";
            // 
            // panelSpriteTest
            // 
            panelSpriteTest.Controls.Add(picSpriteTest);
            panelSpriteTest.Location = new System.Drawing.Point(150, 358);
            panelSpriteTest.Name = "panelSpriteTest";
            panelSpriteTest.Size = new System.Drawing.Size(418, 145);
            panelSpriteTest.TabIndex = 14;
            // 
            // picSpriteTest
            // 
            picSpriteTest.AutoResize = false;
            picSpriteTest.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picSpriteTest.DisplayPage = fastImage1;
            picSpriteTest.Dock = System.Windows.Forms.DockStyle.Fill;
            picSpriteTest.Image = null;
            picSpriteTest.Location = new System.Drawing.Point(0, 0);
            picSpriteTest.Name = "picSpriteTest";
            picSpriteTest.Size = new System.Drawing.Size(418, 145);
            picSpriteTest.TabIndex = 0;
            picSpriteTest.TabStop = false;
            picSpriteTest.MouseDown += picSpriteTest_MouseDown;
            // 
            // tabSpriteDetails
            // 
            tabSpriteDetails.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tabSpriteDetails.Controls.Add(tabPageOverlay);
            tabSpriteDetails.Location = new System.Drawing.Point(480, 2);
            tabSpriteDetails.Name = "tabSpriteDetails";
            tabSpriteDetails.SelectedIndex = 0;
            tabSpriteDetails.Size = new System.Drawing.Size(1566, 660);
            tabSpriteDetails.TabIndex = 18;
            // 
            // SpriteEditor
            // 
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            ClientSize = new System.Drawing.Size(2054, 786);
            Controls.Add(tabSpriteEditor);
            Controls.Add(menuStrip1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "SpriteEditor";
            Text = "Sprite Editor";
            ((System.ComponentModel.ISupportInitialize)m_FileWatcher).EndInit();
            tabSpriteEditor.ResumeLayout(false);
            tabEditor.ResumeLayout(false);
            tabEditor.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackGridOpacity).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureEditor).EndInit();
            tabExport.ResumeLayout(false);
            tabExport.PerformLayout();
            tabImport.ResumeLayout(false);
            tabImport.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            contextMenuChangeMode.ResumeLayout(false);
            tabPageOverlay.ResumeLayout(false);
            tabPageOverlay.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picOverlayPreview).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)editAnimationID).EndInit();
            ((System.ComponentModel.ISupportInitialize)editFrameDelay).EndInit();
            ((System.ComponentModel.ISupportInitialize)picAnimPreview).EndInit();
            panelSpriteTest.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picSpriteTest).EndInit();
            tabSpriteDetails.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabSpriteEditor;
    private System.Windows.Forms.TabPage tabEditor;
    private GR.Forms.FastPictureBox pictureEditor;
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
    private System.Windows.Forms.TrackBar trackGridOpacity;
    private System.Windows.Forms.Label labelGridOpacity;
    private DecentForms.Button btnInvert;
    private DecentForms.Button btnRotateLeft;
    private DecentForms.Button btnRotateRight;
    private DecentForms.Button btnDeleteSprite;
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
        private System.Windows.Forms.TabControl tabSpriteDetails;
        private System.Windows.Forms.TabPage tabPageOverlay;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelSpriteTest;
        private GR.Forms.FastPictureBox picSpriteTest;
        private System.Windows.Forms.Label labelTestBack;
        private System.Windows.Forms.ComboBox comboTestBackColor;
        private System.Windows.Forms.CheckBox checkTestExpandX;
        private System.Windows.Forms.CheckBox checkTestExpandY;
        private System.Windows.Forms.CheckBox checkTestLoop;
        private System.Windows.Forms.CheckBox checkTestC64Mag;
        private System.Windows.Forms.Label labelTestTarget;
        private System.Windows.Forms.TextBox editTestTargetW;
        private System.Windows.Forms.Label labelTestTargetX;
        private System.Windows.Forms.TextBox editTestTargetH;
        private System.Windows.Forms.Label labelTestMagResult;
        private DecentForms.Button btnTestStop;
        private DecentForms.Button btnAddFrame;
        private System.Windows.Forms.Label labelAnimZoom;
        private DecentForms.Button btnRemoveFrame;
        private DecentForms.Button btnAnimZoomIn;
        private DecentForms.Button btnDuplicateFrame;
        private DecentForms.Button btnAnimZoomOut;
        private DecentForms.Button btnPlayAnim;
        private GR.Forms.FastPictureBox picAnimPreview;
        private DecentForms.Button btnPauseAnim;
        private System.Windows.Forms.Panel panelFrameSlots;
        private System.Windows.Forms.ListBox listAnimFrames;
        private System.Windows.Forms.Label labelFrameDelay;
        private System.Windows.Forms.NumericUpDown editFrameDelay;
        private System.Windows.Forms.CheckBox checkLoop;
        private System.Windows.Forms.CheckBox checkStartRandomFrame;
        private System.Windows.Forms.Label labelAnimationID;
        private System.Windows.Forms.NumericUpDown editAnimationID;
        private DecentForms.Button btnAddOverlay;
        private DecentForms.Button btnRemoveOverlay;
        private DecentForms.Button btnCloneOverlay;
        private System.Windows.Forms.ListBox listOverlays;
        private System.Windows.Forms.Label labelOverlayName;
        private System.Windows.Forms.TextBox editOverlayName;
        private GR.Forms.ImageListbox panelSprites;
        private System.Windows.Forms.Panel panelOverlaySlots;
        private GR.Forms.FastPictureBox picOverlayPreview;
        private DecentForms.Button btnOverlayZoomOut;
        private DecentForms.Button btnOverlayZoomIn;
        private System.Windows.Forms.Label labelOverlayZoom;
    }
}
