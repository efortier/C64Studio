namespace RetroDevStudio.Documents
{
  partial class MapEditor
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
            GR.Image.FastImage fastImage5 = new GR.Image.FastImage();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapEditor));
            GR.Image.FastImage fastImage6 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage7 = new GR.Image.FastImage();
            GR.Image.FastImage fastImage8 = new GR.Image.FastImage();
            this.panelMapContainer = new System.Windows.Forms.Panel();
            this.pictureEditor = new GR.Forms.FastPictureBox();
            this.tabMarkers = new Krypton.Navigator.KryptonPage();
            this.tabEntities = new Krypton.Navigator.KryptonPage();
            this.btnDeleteMarkerType = new DecentForms.Button();
            this.btnUpdateMarkerType = new DecentForms.Button();
            this.btnAddMarkerType = new DecentForms.Button();
            this.btnDeleteEntityType = new DecentForms.Button();
            this.btnUpdateEntityType = new DecentForms.Button();
            this.btnAddEntityType = new DecentForms.Button();
            this.editMarkerExportSymbol = new System.Windows.Forms.TextBox();
            this.labelMarkerExportSymbol = new System.Windows.Forms.Label();
            this.editEntityExportSymbol = new System.Windows.Forms.TextBox();
            this.labelEntityExportSymbol = new System.Windows.Forms.Label();
            this.editMarkerTagID = new System.Windows.Forms.NumericUpDown();
            this.editEntityTagID = new System.Windows.Forms.NumericUpDown();
            this.editEntityTileIndex = new System.Windows.Forms.NumericUpDown();
            this.labelEntityTileIndex = new System.Windows.Forms.Label();
            this.editEntityName = new System.Windows.Forms.TextBox();
            this.labelEntityName = new System.Windows.Forms.Label();
            this.labelEntityTagID = new System.Windows.Forms.Label();
            this.listEntityTypes = new System.Windows.Forms.ListBox();
            this.labelMarkerTagID = new System.Windows.Forms.Label();
            this.comboMarkerColor = new System.Windows.Forms.ComboBox();
            this.labelMarkerColor = new System.Windows.Forms.Label();
            this.editMarkerName = new System.Windows.Forms.TextBox();
            this.labelMarkerName = new System.Windows.Forms.Label();
            this.listMarkerTypes = new System.Windows.Forms.ListBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importCharsetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeCharsetProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.keepMapCharacterAspectRatioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabEditor = new Krypton.Navigator.KryptonPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.dimSlider = new System.Windows.Forms.TrackBar();
            this.groupSize = new System.Windows.Forms.GroupBox();
            this.checkShowGrid = new System.Windows.Forms.CheckBox();
            this.comboMapAlternativeMode = new System.Windows.Forms.ComboBox();
            this.comboMapAlternativeBGColor4 = new System.Windows.Forms.ComboBox();
            this.comboMapMultiColor2 = new System.Windows.Forms.ComboBox();
            this.comboMapBGColor = new System.Windows.Forms.ComboBox();
            this.comboMapMultiColor1 = new System.Windows.Forms.ComboBox();
            this.btnCopy = new DecentForms.Button();
            this.btnMoveMapDown = new DecentForms.Button();
            this.btnMoveMapUp = new DecentForms.Button();
            this.btnMapAdd = new DecentForms.Button();
            this.btnMapDelete = new DecentForms.Button();
            this.btnMapApply = new DecentForms.Button();
            this.label14 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.editMapName = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.editTileSpacingH = new System.Windows.Forms.TextBox();
            this.editMapHeight = new System.Windows.Forms.TextBox();
            this.editTileSpacingW = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.editMapWidth = new System.Windows.Forms.TextBox();
            this.btnClearMarkerType = new DecentForms.Button();
            this.comboMaps = new System.Windows.Forms.ComboBox();
            this.btnClearMarkers = new DecentForms.Button();
            this.comboMapProjectMode = new System.Windows.Forms.ComboBox();
            this.groupMapExtraData = new System.Windows.Forms.GroupBox();
            this.editMapExtraData = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.comboDesignerBackground = new System.Windows.Forms.ComboBox();
            this.labelRightClickBehavior = new System.Windows.Forms.Label();
            this.labelDesignerBackground = new System.Windows.Forms.Label();
            this.comboRightClickBehavior = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnToolEdit = new DecentForms.RadioButton();
            this.btnToolRect = new DecentForms.RadioButton();
            this.btnToolQuad = new DecentForms.RadioButton();
            this.btnToolFill = new DecentForms.RadioButton();
            this.btnToolSelect = new DecentForms.RadioButton();
            this.btnToolMarker = new DecentForms.RadioButton();
            this.btnToolEntity = new DecentForms.RadioButton();
            this.comboEntityTypes = new System.Windows.Forms.ComboBox();
            this.labelEntityValue1 = new System.Windows.Forms.Label();
            this.editEntityValue1Default = new System.Windows.Forms.NumericUpDown();
            this.labelEntityValue2 = new System.Windows.Forms.Label();
            this.editEntityValue2Default = new System.Windows.Forms.NumericUpDown();
            this.checkEntityDefaultEnabled = new System.Windows.Forms.CheckBox();
            this.checkShowEntities = new System.Windows.Forms.CheckBox();
            this.comboMarkerTypes = new System.Windows.Forms.ComboBox();
            this.labelMarkerValue1 = new System.Windows.Forms.Label();
            this.editMarkerValue1 = new System.Windows.Forms.NumericUpDown();
            this.labelMarkerValue2 = new System.Windows.Forms.Label();
            this.editMarkerValue2 = new System.Windows.Forms.NumericUpDown();
            this.checkMarkerDefaultEnabled = new System.Windows.Forms.CheckBox();
            this.checkMarkerDefaultTriggered = new System.Windows.Forms.CheckBox();
            this.comboMarkerColorOverride = new System.Windows.Forms.ComboBox();
            this.btnZoomOut = new DecentForms.Button();
            this.btnZoomIn = new DecentForms.Button();
            this.labelZoom = new System.Windows.Forms.Label();
            this.btnCopyMapImage = new DecentForms.Button();
            this.btnShiftLeft = new DecentForms.Button();
            this.btnShiftUp = new DecentForms.Button();
            this.btnShiftDown = new DecentForms.Button();
            this.btnShiftRight = new DecentForms.Button();
            this.btnRemoveOverlappingTiles = new DecentForms.Button();
            this.checkAutoTiling = new System.Windows.Forms.CheckBox();
            this.labelEditInfo = new System.Windows.Forms.Label();
            this.comboTiles = new System.Windows.Forms.ListBox();
            this.mapHScroll = new DecentForms.HScrollBar();
            this.mapVScroll = new DecentForms.VScrollBar();
            this.tabMapEditor = new Krypton.Navigator.KryptonNavigator();
            this.tabTiles = new Krypton.Navigator.KryptonPage();
            this.labelSwatchSize = new System.Windows.Forms.Label();
            this.editSwatchSize = new System.Windows.Forms.TextBox();
            this.btnTileApply = new DecentForms.Button();
            this.btnGetTileCount = new DecentForms.Button();
            this.btnCopyTileCharToNextIncreased = new DecentForms.Button();
            this.btnSetNextTileChar = new DecentForms.Button();
            this.btnMoveTileDown = new DecentForms.Button();
            this.btnMoveTileUp = new DecentForms.Button();
            this.btnTileDelete = new DecentForms.Button();
            this.btnTileClone = new DecentForms.Button();
            this.btnTileAdd = new DecentForms.Button();
            this.listTileChars = new RetroDevStudio.Controls.CSListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listTileInfo = new RetroDevStudio.Controls.CSListView();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.editTileName = new System.Windows.Forms.TextBox();
            this.editTileGroupId = new System.Windows.Forms.TextBox();
            this.labelTileGroupId = new System.Windows.Forms.Label();
            this.editTileHeight = new System.Windows.Forms.TextBox();
            this.editTileWidth = new System.Windows.Forms.TextBox();
            this.checkNotExportedOnMap = new System.Windows.Forms.CheckBox();
            this.checkTilePassable = new System.Windows.Forms.CheckBox();
            this.label17 = new System.Windows.Forms.Label();
            this.labelTilesBGColor4 = new System.Windows.Forms.Label();
            this.labelTilesMulticolor2 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.labelTilesMulticolor1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.panelCharacters = new GR.Forms.ImageListbox();
            this.comboTileBGColor4 = new System.Windows.Forms.ComboBox();
            this.comboTileMulticolor2 = new System.Windows.Forms.ComboBox();
            this.comboTileMulticolor1 = new System.Windows.Forms.ComboBox();
            this.comboTileBackground = new System.Windows.Forms.ComboBox();
            this.panelCharColors = new GR.Forms.FastPictureBox();
            this.pictureTileDisplay = new GR.Forms.FastPictureBox();
            this.tabCharset = new Krypton.Navigator.KryptonPage();
            this.characterEditor = new RetroDevStudio.Controls.CharacterEditor();
            this.tabExport = new Krypton.Navigator.KryptonPage();
            this.label5 = new System.Windows.Forms.Label();
            this.comboExportOrientation = new System.Windows.Forms.ComboBox();
            this.panelExport = new System.Windows.Forms.Panel();
            this.editDataExport = new System.Windows.Forms.TextBox();
            this.btnExport = new DecentForms.Button();
            this.comboExportMethod = new System.Windows.Forms.ComboBox();
            this.label24 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboExportData = new System.Windows.Forms.ComboBox();
            this.tabImport = new Krypton.Navigator.KryptonPage();
            this.panelImport = new System.Windows.Forms.Panel();
            this.btnImport = new DecentForms.Button();
            this.comboImportMethod = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.imageListbox1 = new GR.Forms.ImageListbox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.fastPictureBox1 = new GR.Forms.FastPictureBox();
            this.fastPictureBox2 = new GR.Forms.FastPictureBox();
            this.label8 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).BeginInit();
            this.panelMapContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditor)).BeginInit();
            this.tabMarkers.SuspendLayout();
            this.tabEntities.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerTagID)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityTagID)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityTileIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityValue1Default)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityValue2Default)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerValue1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerValue2)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.tabEditor.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dimSlider)).BeginInit();
            this.groupSize.SuspendLayout();
            this.groupMapExtraData.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tabMapEditor.SuspendLayout();
            this.tabTiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelCharColors)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureTileDisplay)).BeginInit();
            this.tabCharset.SuspendLayout();
            this.tabExport.SuspendLayout();
            this.tabImport.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fastPictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fastPictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMapContainer
            // 
            this.panelMapContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMapContainer.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panelMapContainer.Controls.Add(this.pictureEditor);
            this.panelMapContainer.Location = new System.Drawing.Point(177, 42);
            this.panelMapContainer.Name = "panelMapContainer";
            this.panelMapContainer.Size = new System.Drawing.Size(814, 551);
            this.panelMapContainer.TabIndex = 0;
            // 
            // pictureEditor
            // 
            this.pictureEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureEditor.AutoResize = false;
            this.pictureEditor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureEditor.DisplayPage = fastImage5;
            this.pictureEditor.Image = null;
            this.pictureEditor.Location = new System.Drawing.Point(0, 0);
            this.pictureEditor.Name = "pictureEditor";
            this.pictureEditor.Size = new System.Drawing.Size(1251, 855);
            this.pictureEditor.TabIndex = 0;
            this.pictureEditor.TabStop = false;
            this.pictureEditor.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureEditor_Paint);
            this.pictureEditor.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureEditor_MouseDown);
            this.pictureEditor.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureEditor_MouseMove);
            // 
            // tabMarkers
            // 
            this.tabMarkers.Controls.Add(this.btnDeleteMarkerType);
            this.tabMarkers.Controls.Add(this.btnUpdateMarkerType);
            this.tabMarkers.Controls.Add(this.btnAddMarkerType);
            this.tabMarkers.Controls.Add(this.editMarkerExportSymbol);
            this.tabMarkers.Controls.Add(this.labelMarkerExportSymbol);
            this.tabMarkers.Controls.Add(this.editMarkerTagID);
            this.tabMarkers.Controls.Add(this.labelMarkerTagID);
            this.tabMarkers.Controls.Add(this.checkMarkerDefaultEnabled);
            this.tabMarkers.Controls.Add(this.checkMarkerDefaultTriggered);
            this.tabMarkers.Controls.Add(this.comboMarkerColor);
            this.tabMarkers.Controls.Add(this.labelMarkerColor);
            this.tabMarkers.Controls.Add(this.editMarkerName);
            this.tabMarkers.Controls.Add(this.labelMarkerName);
            this.tabMarkers.Controls.Add(this.listMarkerTypes);
            this.tabMarkers.Location = new System.Drawing.Point(4, 22);
            this.tabMarkers.Name = "tabMarkers";
            this.tabMarkers.Padding = new System.Windows.Forms.Padding(3);
            this.tabMarkers.Size = new System.Drawing.Size(192, 74);
            this.tabMarkers.TabIndex = 4;
            this.tabMarkers.Text = "Markers";
            //
            // tabEntities
            //
            this.tabEntities.Controls.Add(this.btnDeleteEntityType);
            this.tabEntities.Controls.Add(this.btnUpdateEntityType);
            this.tabEntities.Controls.Add(this.btnAddEntityType);
            this.tabEntities.Controls.Add(this.editEntityExportSymbol);
            this.tabEntities.Controls.Add(this.labelEntityExportSymbol);
            this.tabEntities.Controls.Add(this.editEntityTagID);
            this.tabEntities.Controls.Add(this.labelEntityTagID);
            this.tabEntities.Controls.Add(this.editEntityTileIndex);
            this.tabEntities.Controls.Add(this.labelEntityTileIndex);
            this.tabEntities.Controls.Add(this.editEntityName);
            this.tabEntities.Controls.Add(this.labelEntityName);
            this.tabEntities.Controls.Add(this.listEntityTypes);
            this.tabEntities.Location = new System.Drawing.Point(4, 22);
            this.tabEntities.Name = "tabEntities";
            this.tabEntities.Padding = new System.Windows.Forms.Padding(3);
            this.tabEntities.Size = new System.Drawing.Size(192, 74);
            this.tabEntities.TabIndex = 5;
            this.tabEntities.Text = "Entities";
            //
            // btnDeleteEntityType
            //
            this.btnDeleteEntityType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDeleteEntityType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnDeleteEntityType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnDeleteEntityType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDeleteEntityType.Enabled = false;
            this.btnDeleteEntityType.Image = null;
            this.btnDeleteEntityType.Location = new System.Drawing.Point(346, 126);
            this.btnDeleteEntityType.Name = "btnDeleteEntityType";
            this.btnDeleteEntityType.Size = new System.Drawing.Size(80, 23);
            this.btnDeleteEntityType.TabIndex = 9;
            this.btnDeleteEntityType.Text = "Delete Type";
            this.btnDeleteEntityType.Click += new DecentForms.EventHandler(this.btnDeleteEntityType_Click);
            //
            // btnUpdateEntityType
            //
            this.btnUpdateEntityType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnUpdateEntityType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnUpdateEntityType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnUpdateEntityType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnUpdateEntityType.Enabled = false;
            this.btnUpdateEntityType.Image = null;
            this.btnUpdateEntityType.Location = new System.Drawing.Point(260, 126);
            this.btnUpdateEntityType.Name = "btnUpdateEntityType";
            this.btnUpdateEntityType.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateEntityType.TabIndex = 8;
            this.btnUpdateEntityType.Text = "Update";
            this.btnUpdateEntityType.Click += new DecentForms.EventHandler(this.btnUpdateEntityType_Click);
            //
            // btnAddEntityType
            //
            this.btnAddEntityType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddEntityType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAddEntityType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAddEntityType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddEntityType.Image = null;
            this.btnAddEntityType.Location = new System.Drawing.Point(174, 126);
            this.btnAddEntityType.Name = "btnAddEntityType";
            this.btnAddEntityType.Size = new System.Drawing.Size(75, 23);
            this.btnAddEntityType.TabIndex = 7;
            this.btnAddEntityType.Text = "Add Type";
            this.btnAddEntityType.Click += new DecentForms.EventHandler(this.btnAddEntityType_Click);
            //
            // editEntityExportSymbol
            //
            this.editEntityExportSymbol.Location = new System.Drawing.Point(260, 68);
            this.editEntityExportSymbol.Name = "editEntityExportSymbol";
            this.editEntityExportSymbol.Size = new System.Drawing.Size(120, 20);
            this.editEntityExportSymbol.TabIndex = 6;
            this.editEntityExportSymbol.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editEntityExportSymbol_KeyPress);
            //
            // labelEntityExportSymbol
            //
            this.labelEntityExportSymbol.AutoSize = true;
            this.labelEntityExportSymbol.Location = new System.Drawing.Point(174, 70);
            this.labelEntityExportSymbol.Name = "labelEntityExportSymbol";
            this.labelEntityExportSymbol.Size = new System.Drawing.Size(77, 13);
            this.labelEntityExportSymbol.TabIndex = 5;
            this.labelEntityExportSymbol.Text = "Export Symbol:";
            //
            // editEntityTagID
            //
            this.editEntityTagID.Location = new System.Drawing.Point(260, 96);
            this.editEntityTagID.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.editEntityTagID.Name = "editEntityTagID";
            this.editEntityTagID.Size = new System.Drawing.Size(120, 20);
            this.editEntityTagID.TabIndex = 10;
            //
            // labelEntityTagID
            //
            this.labelEntityTagID.AutoSize = true;
            this.labelEntityTagID.Location = new System.Drawing.Point(174, 98);
            this.labelEntityTagID.Name = "labelEntityTagID";
            this.labelEntityTagID.Size = new System.Drawing.Size(43, 13);
            this.labelEntityTagID.TabIndex = 11;
            this.labelEntityTagID.Text = "Tag ID:";
            //
            // editEntityTileIndex
            //
            this.editEntityTileIndex.Location = new System.Drawing.Point(260, 38);
            this.editEntityTileIndex.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.editEntityTileIndex.Name = "editEntityTileIndex";
            this.editEntityTileIndex.Size = new System.Drawing.Size(120, 20);
            this.editEntityTileIndex.TabIndex = 4;
            //
            // labelEntityTileIndex
            //
            this.labelEntityTileIndex.AutoSize = true;
            this.labelEntityTileIndex.Location = new System.Drawing.Point(174, 40);
            this.labelEntityTileIndex.Name = "labelEntityTileIndex";
            this.labelEntityTileIndex.Size = new System.Drawing.Size(60, 13);
            this.labelEntityTileIndex.TabIndex = 3;
            this.labelEntityTileIndex.Text = "Tile Index:";
            //
            // editEntityName
            //
            this.editEntityName.Location = new System.Drawing.Point(220, 8);
            this.editEntityName.Name = "editEntityName";
            this.editEntityName.Size = new System.Drawing.Size(160, 20);
            this.editEntityName.TabIndex = 2;
            //
            // labelEntityName
            //
            this.labelEntityName.AutoSize = true;
            this.labelEntityName.Location = new System.Drawing.Point(174, 10);
            this.labelEntityName.Name = "labelEntityName";
            this.labelEntityName.Size = new System.Drawing.Size(38, 13);
            this.labelEntityName.TabIndex = 1;
            this.labelEntityName.Text = "Name:";
            //
            // listEntityTypes
            //
            this.listEntityTypes.FormattingEnabled = true;
            this.listEntityTypes.Location = new System.Drawing.Point(8, 8);
            this.listEntityTypes.Name = "listEntityTypes";
            this.listEntityTypes.Size = new System.Drawing.Size(160, 394);
            this.listEntityTypes.TabIndex = 0;
            this.listEntityTypes.SelectedIndexChanged += new System.EventHandler(this.listEntityTypes_SelectedIndexChanged);
            //
            // btnDeleteMarkerType
            // 
            this.btnDeleteMarkerType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnDeleteMarkerType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnDeleteMarkerType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnDeleteMarkerType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDeleteMarkerType.Enabled = false;
            this.btnDeleteMarkerType.Image = null;
            this.btnDeleteMarkerType.Location = new System.Drawing.Point(346, 126);
            this.btnDeleteMarkerType.Name = "btnDeleteMarkerType";
            this.btnDeleteMarkerType.Size = new System.Drawing.Size(80, 23);
            this.btnDeleteMarkerType.TabIndex = 9;
            this.btnDeleteMarkerType.Text = "Delete Type";
            this.btnDeleteMarkerType.Click += new DecentForms.EventHandler(this.btnDeleteMarkerType_Click);
            // 
            // btnUpdateMarkerType
            // 
            this.btnUpdateMarkerType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnUpdateMarkerType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnUpdateMarkerType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnUpdateMarkerType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnUpdateMarkerType.Enabled = false;
            this.btnUpdateMarkerType.Image = null;
            this.btnUpdateMarkerType.Location = new System.Drawing.Point(260, 126);
            this.btnUpdateMarkerType.Name = "btnUpdateMarkerType";
            this.btnUpdateMarkerType.Size = new System.Drawing.Size(75, 23);
            this.btnUpdateMarkerType.TabIndex = 8;
            this.btnUpdateMarkerType.Text = "Update";
            this.btnUpdateMarkerType.Click += new DecentForms.EventHandler(this.btnUpdateMarkerType_Click);
            // 
            // btnAddMarkerType
            // 
            this.btnAddMarkerType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnAddMarkerType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnAddMarkerType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnAddMarkerType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAddMarkerType.Image = null;
            this.btnAddMarkerType.Location = new System.Drawing.Point(174, 126);
            this.btnAddMarkerType.Name = "btnAddMarkerType";
            this.btnAddMarkerType.Size = new System.Drawing.Size(75, 23);
            this.btnAddMarkerType.TabIndex = 7;
            this.btnAddMarkerType.Text = "Add Type";
            this.btnAddMarkerType.Click += new DecentForms.EventHandler(this.btnAddMarkerType_Click);
            //
            // editMarkerExportSymbol
            //
            this.editMarkerExportSymbol.Location = new System.Drawing.Point(260, 68);
            this.editMarkerExportSymbol.Name = "editMarkerExportSymbol";
            this.editMarkerExportSymbol.Size = new System.Drawing.Size(120, 20);
            this.editMarkerExportSymbol.TabIndex = 6;
            this.editMarkerExportSymbol.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editMarkerExportSymbol_KeyPress);
            //
            // labelMarkerExportSymbol
            //
            this.labelMarkerExportSymbol.AutoSize = true;
            this.labelMarkerExportSymbol.Location = new System.Drawing.Point(174, 70);
            this.labelMarkerExportSymbol.Name = "labelMarkerExportSymbol";
            this.labelMarkerExportSymbol.Size = new System.Drawing.Size(77, 13);
            this.labelMarkerExportSymbol.TabIndex = 5;
            this.labelMarkerExportSymbol.Text = "Export Symbol:";
            //
            // editMarkerTagID
            // 
            this.editMarkerTagID.Location = new System.Drawing.Point(260, 96);
            this.editMarkerTagID.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.editMarkerTagID.Name = "editMarkerTagID";
            this.editMarkerTagID.Size = new System.Drawing.Size(120, 20);
            this.editMarkerTagID.TabIndex = 10;
            //
            // labelMarkerTagID
            //
            this.labelMarkerTagID.AutoSize = true;
            this.labelMarkerTagID.Location = new System.Drawing.Point(174, 98);
            this.labelMarkerTagID.Name = "labelMarkerTagID";
            this.labelMarkerTagID.Size = new System.Drawing.Size(43, 13);
            this.labelMarkerTagID.TabIndex = 11;
            this.labelMarkerTagID.Text = "Tag ID:";
            //
            // checkMarkerDefaultEnabled
            //
            this.checkMarkerDefaultEnabled.AutoSize = true;
            this.checkMarkerDefaultEnabled.Checked = true;
            this.checkMarkerDefaultEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkMarkerDefaultEnabled.Location = new System.Drawing.Point(174, 158);
            this.checkMarkerDefaultEnabled.Name = "checkMarkerDefaultEnabled";
            this.checkMarkerDefaultEnabled.Size = new System.Drawing.Size(127, 17);
            this.checkMarkerDefaultEnabled.TabIndex = 12;
            this.checkMarkerDefaultEnabled.Text = "Enabled (default)";
            this.checkMarkerDefaultEnabled.UseVisualStyleBackColor = true;
            //
            // checkMarkerDefaultTriggered
            //
            this.checkMarkerDefaultTriggered.AutoSize = true;
            this.checkMarkerDefaultTriggered.Checked = false;
            this.checkMarkerDefaultTriggered.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.checkMarkerDefaultTriggered.Location = new System.Drawing.Point(174, 182);
            this.checkMarkerDefaultTriggered.Name = "checkMarkerDefaultTriggered";
            this.checkMarkerDefaultTriggered.Size = new System.Drawing.Size(133, 17);
            this.checkMarkerDefaultTriggered.TabIndex = 13;
            this.checkMarkerDefaultTriggered.Text = "Triggered (default)";
            this.checkMarkerDefaultTriggered.UseVisualStyleBackColor = true;
            //
            // comboMarkerColor
            // 
            this.comboMarkerColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMarkerColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMarkerColor.FormattingEnabled = true;
            this.comboMarkerColor.Location = new System.Drawing.Point(220, 38);
            this.comboMarkerColor.Name = "comboMarkerColor";
            this.comboMarkerColor.Size = new System.Drawing.Size(160, 21);
            this.comboMarkerColor.TabIndex = 4;
            this.comboMarkerColor.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            // 
            // labelMarkerColor
            // 
            this.labelMarkerColor.AutoSize = true;
            this.labelMarkerColor.Location = new System.Drawing.Point(174, 40);
            this.labelMarkerColor.Name = "labelMarkerColor";
            this.labelMarkerColor.Size = new System.Drawing.Size(34, 13);
            this.labelMarkerColor.TabIndex = 3;
            this.labelMarkerColor.Text = "Color:";
            // 
            // editMarkerName
            // 
            this.editMarkerName.Location = new System.Drawing.Point(220, 8);
            this.editMarkerName.Name = "editMarkerName";
            this.editMarkerName.Size = new System.Drawing.Size(160, 20);
            this.editMarkerName.TabIndex = 2;
            // 
            // labelMarkerName
            // 
            this.labelMarkerName.AutoSize = true;
            this.labelMarkerName.Location = new System.Drawing.Point(174, 10);
            this.labelMarkerName.Name = "labelMarkerName";
            this.labelMarkerName.Size = new System.Drawing.Size(38, 13);
            this.labelMarkerName.TabIndex = 1;
            this.labelMarkerName.Text = "Name:";
            // 
            // listMarkerTypes
            // 
            this.listMarkerTypes.FormattingEnabled = true;
            this.listMarkerTypes.Location = new System.Drawing.Point(8, 8);
            this.listMarkerTypes.Name = "listMarkerTypes";
            this.listMarkerTypes.Size = new System.Drawing.Size(160, 394);
            this.listMarkerTypes.TabIndex = 0;
            this.listMarkerTypes.SelectedIndexChanged += new System.EventHandler(this.listMarkerTypes_SelectedIndexChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1393, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importCharsetToolStripMenuItem,
            this.saveCharsetProjectToolStripMenuItem,
            this.closeCharsetProjectToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(83, 20);
            this.fileToolStripMenuItem.Text = "&Map Project";
            // 
            // importCharsetToolStripMenuItem
            // 
            this.importCharsetToolStripMenuItem.Name = "importCharsetToolStripMenuItem";
            this.importCharsetToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.importCharsetToolStripMenuItem.Text = "Import &Charset...";
            this.importCharsetToolStripMenuItem.Click += new System.EventHandler(this.importCharsetToolStripMenuItem_Click);
            // 
            // saveCharsetProjectToolStripMenuItem
            // 
            this.saveCharsetProjectToolStripMenuItem.Enabled = false;
            this.saveCharsetProjectToolStripMenuItem.Name = "saveCharsetProjectToolStripMenuItem";
            this.saveCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.saveCharsetProjectToolStripMenuItem.Text = "&Save Project";
            this.saveCharsetProjectToolStripMenuItem.Click += new System.EventHandler(this.saveCharsetProjectToolStripMenuItem_Click);
            // 
            // closeCharsetProjectToolStripMenuItem
            // 
            this.closeCharsetProjectToolStripMenuItem.Enabled = false;
            this.closeCharsetProjectToolStripMenuItem.Name = "closeCharsetProjectToolStripMenuItem";
            this.closeCharsetProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.closeCharsetProjectToolStripMenuItem.Text = "&Close Charset Project";
            this.closeCharsetProjectToolStripMenuItem.Click += new System.EventHandler(this.closeCharsetProjectToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.keepMapCharacterAspectRatioToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // keepMapCharacterAspectRatioToolStripMenuItem
            // 
            this.keepMapCharacterAspectRatioToolStripMenuItem.CheckOnClick = true;
            this.keepMapCharacterAspectRatioToolStripMenuItem.Name = "keepMapCharacterAspectRatioToolStripMenuItem";
            this.keepMapCharacterAspectRatioToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.keepMapCharacterAspectRatioToolStripMenuItem.Text = "Keep map character aspect ratio";
            // 
            // tabEditor
            // 
            this.tabEditor.Controls.Add(this.groupBox1);
            this.tabEditor.Controls.Add(this.flowLayoutPanel1);
            this.tabEditor.Controls.Add(this.comboTiles);
            this.tabEditor.Controls.Add(this.mapHScroll);
            this.tabEditor.Controls.Add(this.mapVScroll);
            this.tabEditor.Controls.Add(this.panelMapContainer);
            this.tabEditor.Location = new System.Drawing.Point(4, 22);
            this.tabEditor.Name = "tabEditor";
            this.tabEditor.Padding = new System.Windows.Forms.Padding(3);
            this.tabEditor.Size = new System.Drawing.Size(1385, 628);
            this.tabEditor.TabIndex = 0;
            this.tabEditor.Text = "Map";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.label19);
            this.groupBox1.Controls.Add(this.label25);
            this.groupBox1.Controls.Add(this.dimSlider);
            this.groupBox1.Controls.Add(this.groupSize);
            this.groupBox1.Controls.Add(this.btnClearMarkerType);
            this.groupBox1.Controls.Add(this.comboMaps);
            this.groupBox1.Controls.Add(this.btnClearMarkers);
            this.groupBox1.Controls.Add(this.comboMapProjectMode);
            this.groupBox1.Controls.Add(this.groupMapExtraData);
            this.groupBox1.Controls.Add(this.comboDesignerBackground);
            this.groupBox1.Controls.Add(this.labelRightClickBehavior);
            this.groupBox1.Controls.Add(this.labelDesignerBackground);
            this.groupBox1.Controls.Add(this.comboRightClickBehavior);
            this.groupBox1.Location = new System.Drawing.Point(1016, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(361, 614);
            this.groupBox1.TabIndex = 37;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Map Controls";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(15, 25);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(68, 13);
            this.label19.TabIndex = 25;
            this.label19.Text = "Current Map:";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(15, 52);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(37, 13);
            this.label25.TabIndex = 25;
            this.label25.Text = "Mode:";
            // 
            // dimSlider
            // 
            this.dimSlider.Location = new System.Drawing.Point(30, 542);
            this.dimSlider.Maximum = 100;
            this.dimSlider.Name = "dimSlider";
            this.dimSlider.Size = new System.Drawing.Size(150, 45);
            this.dimSlider.TabIndex = 0;
            this.dimSlider.TickFrequency = 10;
            this.dimSlider.Value = 100;
            this.dimSlider.Scroll += new System.EventHandler(this.dimSlider_Scroll);
            // 
            // groupSize
            // 
            this.groupSize.Controls.Add(this.checkShowGrid);
            this.groupSize.Controls.Add(this.comboMapAlternativeMode);
            this.groupSize.Controls.Add(this.comboMapAlternativeBGColor4);
            this.groupSize.Controls.Add(this.comboMapMultiColor2);
            this.groupSize.Controls.Add(this.comboMapBGColor);
            this.groupSize.Controls.Add(this.comboMapMultiColor1);
            this.groupSize.Controls.Add(this.btnCopy);
            this.groupSize.Controls.Add(this.btnMoveMapDown);
            this.groupSize.Controls.Add(this.btnMoveMapUp);
            this.groupSize.Controls.Add(this.btnMapAdd);
            this.groupSize.Controls.Add(this.btnMapDelete);
            this.groupSize.Controls.Add(this.btnMapApply);
            this.groupSize.Controls.Add(this.label14);
            this.groupSize.Controls.Add(this.label1);
            this.groupSize.Controls.Add(this.label22);
            this.groupSize.Controls.Add(this.label23);
            this.groupSize.Controls.Add(this.editMapName);
            this.groupSize.Controls.Add(this.label13);
            this.groupSize.Controls.Add(this.label21);
            this.groupSize.Controls.Add(this.label18);
            this.groupSize.Controls.Add(this.editTileSpacingH);
            this.groupSize.Controls.Add(this.editMapHeight);
            this.groupSize.Controls.Add(this.editTileSpacingW);
            this.groupSize.Controls.Add(this.label3);
            this.groupSize.Controls.Add(this.editMapWidth);
            this.groupSize.Location = new System.Drawing.Point(18, 74);
            this.groupSize.Name = "groupSize";
            this.groupSize.Size = new System.Drawing.Size(337, 183);
            this.groupSize.TabIndex = 28;
            this.groupSize.TabStop = false;
            this.groupSize.Text = "Map Details";
            // 
            // checkShowGrid
            // 
            this.checkShowGrid.AutoSize = true;
            this.checkShowGrid.Location = new System.Drawing.Point(191, 99);
            this.checkShowGrid.Name = "checkShowGrid";
            this.checkShowGrid.Size = new System.Drawing.Size(45, 17);
            this.checkShowGrid.TabIndex = 12;
            this.checkShowGrid.Text = "Grid";
            this.checkShowGrid.UseVisualStyleBackColor = true;
            this.checkShowGrid.CheckedChanged += new System.EventHandler(this.checkShowGrid_CheckedChanged);
            // 
            // comboMapAlternativeMode
            // 
            this.comboMapAlternativeMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapAlternativeMode.FormattingEnabled = true;
            this.comboMapAlternativeMode.Location = new System.Drawing.Point(63, 97);
            this.comboMapAlternativeMode.Name = "comboMapAlternativeMode";
            this.comboMapAlternativeMode.Size = new System.Drawing.Size(121, 21);
            this.comboMapAlternativeMode.TabIndex = 11;
            this.comboMapAlternativeMode.SelectedIndexChanged += new System.EventHandler(this.comboMapAlternativeMode_SelectedIndexChanged);
            // 
            // comboMapAlternativeBGColor4
            // 
            this.comboMapAlternativeBGColor4.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMapAlternativeBGColor4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapAlternativeBGColor4.FormattingEnabled = true;
            this.comboMapAlternativeBGColor4.Location = new System.Drawing.Point(182, 151);
            this.comboMapAlternativeBGColor4.Name = "comboMapAlternativeBGColor4";
            this.comboMapAlternativeBGColor4.Size = new System.Drawing.Size(82, 21);
            this.comboMapAlternativeBGColor4.TabIndex = 16;
            this.comboMapAlternativeBGColor4.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboAlternativeColor_DrawItem);
            this.comboMapAlternativeBGColor4.SelectedIndexChanged += new System.EventHandler(this.comboMapBGColor4_SelectedIndexChanged);
            // 
            // comboMapMultiColor2
            // 
            this.comboMapMultiColor2.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMapMultiColor2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapMultiColor2.FormattingEnabled = true;
            this.comboMapMultiColor2.Location = new System.Drawing.Point(182, 124);
            this.comboMapMultiColor2.Name = "comboMapMultiColor2";
            this.comboMapMultiColor2.Size = new System.Drawing.Size(82, 21);
            this.comboMapMultiColor2.TabIndex = 14;
            this.comboMapMultiColor2.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboAlternativeColor_DrawItem);
            this.comboMapMultiColor2.SelectedIndexChanged += new System.EventHandler(this.comboMapMultiColor2_SelectedIndexChanged);
            // 
            // comboMapBGColor
            // 
            this.comboMapBGColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMapBGColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapBGColor.FormattingEnabled = true;
            this.comboMapBGColor.Location = new System.Drawing.Point(63, 151);
            this.comboMapBGColor.Name = "comboMapBGColor";
            this.comboMapBGColor.Size = new System.Drawing.Size(82, 21);
            this.comboMapBGColor.TabIndex = 15;
            this.comboMapBGColor.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboAlternativeColor_DrawItem);
            this.comboMapBGColor.SelectedIndexChanged += new System.EventHandler(this.comboMapBGColor_SelectedIndexChanged);
            // 
            // comboMapMultiColor1
            // 
            this.comboMapMultiColor1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMapMultiColor1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapMultiColor1.FormattingEnabled = true;
            this.comboMapMultiColor1.Location = new System.Drawing.Point(63, 124);
            this.comboMapMultiColor1.Name = "comboMapMultiColor1";
            this.comboMapMultiColor1.Size = new System.Drawing.Size(82, 21);
            this.comboMapMultiColor1.TabIndex = 13;
            this.comboMapMultiColor1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboAlternativeColor_DrawItem);
            this.comboMapMultiColor1.SelectedIndexChanged += new System.EventHandler(this.comboMapMultiColor1_SelectedIndexChanged);
            // 
            // btnCopy
            // 
            this.btnCopy.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCopy.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCopy.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCopy.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCopy.Image = null;
            this.btnCopy.Location = new System.Drawing.Point(231, 17);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(35, 23);
            this.btnCopy.TabIndex = 5;
            this.btnCopy.Text = "Cpy";
            this.btnCopy.Click += new DecentForms.EventHandler(this.btnMapCopy_Click);
            // 
            // btnMoveMapDown
            // 
            this.btnMoveMapDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveMapDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveMapDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveMapDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveMapDown.Enabled = false;
            this.btnMoveMapDown.Image = null;
            this.btnMoveMapDown.Location = new System.Drawing.Point(231, 43);
            this.btnMoveMapDown.Name = "btnMoveMapDown";
            this.btnMoveMapDown.Size = new System.Drawing.Size(35, 23);
            this.btnMoveMapDown.TabIndex = 7;
            this.btnMoveMapDown.Text = "▼";
            this.toolTip1.SetToolTip(this.btnMoveMapDown, "Move Map Down");
            this.btnMoveMapDown.Click += new DecentForms.EventHandler(this.btnMoveMapDown_Click);
            // 
            // btnMoveMapUp
            // 
            this.btnMoveMapUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveMapUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveMapUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveMapUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveMapUp.Enabled = false;
            this.btnMoveMapUp.Image = null;
            this.btnMoveMapUp.Location = new System.Drawing.Point(192, 43);
            this.btnMoveMapUp.Name = "btnMoveMapUp";
            this.btnMoveMapUp.Size = new System.Drawing.Size(35, 23);
            this.btnMoveMapUp.TabIndex = 6;
            this.btnMoveMapUp.Text = "▲";
            this.toolTip1.SetToolTip(this.btnMoveMapUp, "Move Map Up");
            this.btnMoveMapUp.Click += new DecentForms.EventHandler(this.btnMoveMapUp_Click);
            // 
            // btnMapAdd
            // 
            this.btnMapAdd.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMapAdd.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMapAdd.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMapAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMapAdd.Image = null;
            this.btnMapAdd.Location = new System.Drawing.Point(191, 17);
            this.btnMapAdd.Name = "btnMapAdd";
            this.btnMapAdd.Size = new System.Drawing.Size(35, 23);
            this.btnMapAdd.TabIndex = 4;
            this.btnMapAdd.Text = "Add";
            this.btnMapAdd.Click += new DecentForms.EventHandler(this.btnMapAdd_Click);
            // 
            // btnMapDelete
            // 
            this.btnMapDelete.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMapDelete.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMapDelete.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMapDelete.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMapDelete.Enabled = false;
            this.btnMapDelete.Image = null;
            this.btnMapDelete.Location = new System.Drawing.Point(251, 69);
            this.btnMapDelete.Name = "btnMapDelete";
            this.btnMapDelete.Size = new System.Drawing.Size(56, 23);
            this.btnMapDelete.TabIndex = 10;
            this.btnMapDelete.Text = "Delete";
            this.btnMapDelete.Click += new DecentForms.EventHandler(this.btnMapDelete_Click);
            // 
            // btnMapApply
            // 
            this.btnMapApply.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMapApply.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMapApply.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMapApply.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMapApply.Enabled = false;
            this.btnMapApply.Image = null;
            this.btnMapApply.Location = new System.Drawing.Point(192, 69);
            this.btnMapApply.Name = "btnMapApply";
            this.btnMapApply.Size = new System.Drawing.Size(53, 23);
            this.btnMapApply.TabIndex = 9;
            this.btnMapApply.Text = "Apply";
            this.btnMapApply.Click += new DecentForms.EventHandler(this.btnMapApply_Click);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(148, 154);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 13);
            this.label14.TabIndex = 25;
            this.label14.Text = "BG4:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(30, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "Size:";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(148, 127);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(32, 13);
            this.label22.TabIndex = 25;
            this.label22.Text = "MC2:";
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(9, 154);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(25, 13);
            this.label23.TabIndex = 25;
            this.label23.Text = "BG:";
            // 
            // editMapName
            // 
            this.editMapName.Location = new System.Drawing.Point(63, 71);
            this.editMapName.Name = "editMapName";
            this.editMapName.Size = new System.Drawing.Size(122, 20);
            this.editMapName.TabIndex = 8;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(9, 100);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(37, 13);
            this.label13.TabIndex = 25;
            this.label13.Text = "Mode:";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(9, 127);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(32, 13);
            this.label21.TabIndex = 25;
            this.label21.Text = "MC1:";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(9, 74);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(38, 13);
            this.label18.TabIndex = 25;
            this.label18.Text = "Name:";
            // 
            // editTileSpacingH
            // 
            this.editTileSpacingH.Location = new System.Drawing.Point(128, 45);
            this.editTileSpacingH.Name = "editTileSpacingH";
            this.editTileSpacingH.Size = new System.Drawing.Size(56, 20);
            this.editTileSpacingH.TabIndex = 3;
            this.editTileSpacingH.Text = "2";
            // 
            // editMapHeight
            // 
            this.editMapHeight.Location = new System.Drawing.Point(128, 19);
            this.editMapHeight.Name = "editMapHeight";
            this.editMapHeight.Size = new System.Drawing.Size(56, 20);
            this.editMapHeight.TabIndex = 1;
            this.editMapHeight.Text = "12";
            // 
            // editTileSpacingW
            // 
            this.editTileSpacingW.Location = new System.Drawing.Point(63, 45);
            this.editTileSpacingW.Name = "editTileSpacingW";
            this.editTileSpacingW.Size = new System.Drawing.Size(60, 20);
            this.editTileSpacingW.TabIndex = 2;
            this.editTileSpacingW.Text = "2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Tilesize:";
            // 
            // editMapWidth
            // 
            this.editMapWidth.Location = new System.Drawing.Point(63, 19);
            this.editMapWidth.Name = "editMapWidth";
            this.editMapWidth.Size = new System.Drawing.Size(60, 20);
            this.editMapWidth.TabIndex = 0;
            this.editMapWidth.Text = "20";
            // 
            // btnClearMarkerType
            // 
            this.btnClearMarkerType.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnClearMarkerType.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnClearMarkerType.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnClearMarkerType.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClearMarkerType.Image = null;
            this.btnClearMarkerType.Location = new System.Drawing.Point(146, 513);
            this.btnClearMarkerType.Name = "btnClearMarkerType";
            this.btnClearMarkerType.Size = new System.Drawing.Size(111, 23);
            this.btnClearMarkerType.TabIndex = 1;
            this.btnClearMarkerType.Text = "Clear Marker Type";
            this.btnClearMarkerType.Click += new DecentForms.EventHandler(this.btnClearMarkerType_Click);
            // 
            // comboMaps
            // 
            this.comboMaps.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMaps.Enabled = false;
            this.comboMaps.FormattingEnabled = true;
            this.comboMaps.Location = new System.Drawing.Point(89, 22);
            this.comboMaps.Name = "comboMaps";
            this.comboMaps.Size = new System.Drawing.Size(246, 21);
            this.comboMaps.TabIndex = 0;
            this.comboMaps.SelectedIndexChanged += new System.EventHandler(this.comboMaps_SelectedIndexChanged);
            // 
            // btnClearMarkers
            // 
            this.btnClearMarkers.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnClearMarkers.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnClearMarkers.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnClearMarkers.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClearMarkers.Image = null;
            this.btnClearMarkers.Location = new System.Drawing.Point(30, 513);
            this.btnClearMarkers.Name = "btnClearMarkers";
            this.btnClearMarkers.Size = new System.Drawing.Size(111, 23);
            this.btnClearMarkers.TabIndex = 2;
            this.btnClearMarkers.Text = "Clear All Markers";
            this.btnClearMarkers.Click += new DecentForms.EventHandler(this.btnClearMarkers_Click);
            // 
            // comboMapProjectMode
            // 
            this.comboMapProjectMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMapProjectMode.FormattingEnabled = true;
            this.comboMapProjectMode.Location = new System.Drawing.Point(89, 49);
            this.comboMapProjectMode.Name = "comboMapProjectMode";
            this.comboMapProjectMode.Size = new System.Drawing.Size(246, 21);
            this.comboMapProjectMode.TabIndex = 1;
            this.comboMapProjectMode.SelectedIndexChanged += new System.EventHandler(this.comboMapProjectMode_SelectedIndexChanged);
            // 
            // groupMapExtraData
            // 
            this.groupMapExtraData.Controls.Add(this.editMapExtraData);
            this.groupMapExtraData.Controls.Add(this.label20);
            this.groupMapExtraData.Location = new System.Drawing.Point(30, 263);
            this.groupMapExtraData.Name = "groupMapExtraData";
            this.groupMapExtraData.Size = new System.Drawing.Size(266, 161);
            this.groupMapExtraData.TabIndex = 31;
            this.groupMapExtraData.TabStop = false;
            this.groupMapExtraData.Text = "Extra Data";
            // 
            // editMapExtraData
            // 
            this.editMapExtraData.Location = new System.Drawing.Point(6, 36);
            this.editMapExtraData.Multiline = true;
            this.editMapExtraData.Name = "editMapExtraData";
            this.editMapExtraData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.editMapExtraData.Size = new System.Drawing.Size(258, 119);
            this.editMapExtraData.TabIndex = 0;
            this.editMapExtraData.TextChanged += new System.EventHandler(this.editMapExtraData_TextChanged);
            this.editMapExtraData.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editMapExtraData_KeyPress);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(6, 20);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(139, 13);
            this.label20.TabIndex = 0;
            this.label20.Text = "Additional Binary Data (Hex)";
            // 
            // comboDesignerBackground
            // 
            this.comboDesignerBackground.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboDesignerBackground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboDesignerBackground.FormattingEnabled = true;
            this.comboDesignerBackground.Location = new System.Drawing.Point(30, 486);
            this.comboDesignerBackground.Name = "comboDesignerBackground";
            this.comboDesignerBackground.Size = new System.Drawing.Size(266, 21);
            this.comboDesignerBackground.TabIndex = 35;
            this.comboDesignerBackground.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            this.comboDesignerBackground.SelectedIndexChanged += new System.EventHandler(this.comboDesignerBackground_SelectedIndexChanged);
            // 
            // labelRightClickBehavior
            // 
            this.labelRightClickBehavior.AutoSize = true;
            this.labelRightClickBehavior.Location = new System.Drawing.Point(30, 430);
            this.labelRightClickBehavior.Name = "labelRightClickBehavior";
            this.labelRightClickBehavior.Size = new System.Drawing.Size(104, 13);
            this.labelRightClickBehavior.TabIndex = 32;
            this.labelRightClickBehavior.Text = "Right-click behavior:";
            // 
            // labelDesignerBackground
            // 
            this.labelDesignerBackground.AutoSize = true;
            this.labelDesignerBackground.Location = new System.Drawing.Point(30, 470);
            this.labelDesignerBackground.Name = "labelDesignerBackground";
            this.labelDesignerBackground.Size = new System.Drawing.Size(188, 13);
            this.labelDesignerBackground.TabIndex = 34;
            this.labelDesignerBackground.Text = "Map back render color (designer only):";
            // 
            // comboRightClickBehavior
            // 
            this.comboRightClickBehavior.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboRightClickBehavior.FormattingEnabled = true;
            this.comboRightClickBehavior.Location = new System.Drawing.Point(30, 446);
            this.comboRightClickBehavior.Name = "comboRightClickBehavior";
            this.comboRightClickBehavior.Size = new System.Drawing.Size(266, 21);
            this.comboRightClickBehavior.TabIndex = 33;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnToolEdit);
            this.flowLayoutPanel1.Controls.Add(this.btnToolRect);
            this.flowLayoutPanel1.Controls.Add(this.btnToolQuad);
            this.flowLayoutPanel1.Controls.Add(this.btnToolFill);
            this.flowLayoutPanel1.Controls.Add(this.btnToolSelect);
            this.flowLayoutPanel1.Controls.Add(this.btnToolMarker);
            this.flowLayoutPanel1.Controls.Add(this.comboMarkerTypes);
            this.flowLayoutPanel1.Controls.Add(this.labelMarkerValue1);
            this.flowLayoutPanel1.Controls.Add(this.editMarkerValue1);
            this.flowLayoutPanel1.Controls.Add(this.labelMarkerValue2);
            this.flowLayoutPanel1.Controls.Add(this.editMarkerValue2);
            this.flowLayoutPanel1.Controls.Add(this.comboMarkerColorOverride);
            this.flowLayoutPanel1.Controls.Add(this.btnToolEntity);
            this.flowLayoutPanel1.Controls.Add(this.comboEntityTypes);
            this.flowLayoutPanel1.Controls.Add(this.labelEntityValue1);
            this.flowLayoutPanel1.Controls.Add(this.editEntityValue1Default);
            this.flowLayoutPanel1.Controls.Add(this.labelEntityValue2);
            this.flowLayoutPanel1.Controls.Add(this.editEntityValue2Default);
            this.flowLayoutPanel1.Controls.Add(this.checkEntityDefaultEnabled);
            this.flowLayoutPanel1.Controls.Add(this.checkShowEntities);
            this.flowLayoutPanel1.Controls.Add(this.btnZoomOut);
            this.flowLayoutPanel1.Controls.Add(this.btnZoomIn);
            this.flowLayoutPanel1.Controls.Add(this.labelZoom);
            this.flowLayoutPanel1.Controls.Add(this.btnCopyMapImage);
            this.flowLayoutPanel1.Controls.Add(this.btnShiftLeft);
            this.flowLayoutPanel1.Controls.Add(this.btnShiftUp);
            this.flowLayoutPanel1.Controls.Add(this.btnShiftDown);
            this.flowLayoutPanel1.Controls.Add(this.btnShiftRight);
            this.flowLayoutPanel1.Controls.Add(this.btnRemoveOverlappingTiles);
            this.flowLayoutPanel1.Controls.Add(this.checkAutoTiling);
            this.flowLayoutPanel1.Controls.Add(this.labelEditInfo);
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                                                                                | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Location = new System.Drawing.Point(174, 6);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1150, 30);
            this.flowLayoutPanel1.TabIndex = 36;
            // 
            // btnToolEdit
            // 
            this.btnToolEdit.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolEdit.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolEdit.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolEdit.Checked = true;
            this.btnToolEdit.Image = ((System.Drawing.Image)(resources.GetObject("btnToolEdit.Image")));
            this.btnToolEdit.Location = new System.Drawing.Point(3, 3);
            this.btnToolEdit.Name = "btnToolEdit";
            this.btnToolEdit.Size = new System.Drawing.Size(24, 24);
            this.btnToolEdit.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnToolEdit, "Place/Pick Single Tile");
            this.btnToolEdit.CheckedChanged += new DecentForms.EventHandler(this.btnToolEdit_CheckedChanged);
            // 
            // btnToolRect
            // 
            this.btnToolRect.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolRect.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolRect.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolRect.Checked = false;
            this.btnToolRect.Image = ((System.Drawing.Image)(resources.GetObject("btnToolRect.Image")));
            this.btnToolRect.Location = new System.Drawing.Point(33, 3);
            this.btnToolRect.Name = "btnToolRect";
            this.btnToolRect.Size = new System.Drawing.Size(24, 24);
            this.btnToolRect.TabIndex = 4;
            this.toolTip1.SetToolTip(this.btnToolRect, "Rectangle");
            this.btnToolRect.CheckedChanged += new DecentForms.EventHandler(this.btnToolRect_CheckedChanged);
            // 
            // btnToolQuad
            // 
            this.btnToolQuad.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolQuad.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolQuad.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolQuad.Checked = false;
            this.btnToolQuad.Image = ((System.Drawing.Image)(resources.GetObject("btnToolQuad.Image")));
            this.btnToolQuad.Location = new System.Drawing.Point(63, 3);
            this.btnToolQuad.Name = "btnToolQuad";
            this.btnToolQuad.Size = new System.Drawing.Size(24, 24);
            this.btnToolQuad.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btnToolQuad, "Filled Rectangle");
            this.btnToolQuad.CheckedChanged += new DecentForms.EventHandler(this.btnToolQuad_CheckedChanged);
            // 
            // btnToolFill
            // 
            this.btnToolFill.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolFill.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolFill.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolFill.Checked = false;
            this.btnToolFill.Image = ((System.Drawing.Image)(resources.GetObject("btnToolFill.Image")));
            this.btnToolFill.Location = new System.Drawing.Point(93, 3);
            this.btnToolFill.Name = "btnToolFill";
            this.btnToolFill.Size = new System.Drawing.Size(24, 24);
            this.btnToolFill.TabIndex = 6;
            this.toolTip1.SetToolTip(this.btnToolFill, "Flood Fill");
            this.btnToolFill.CheckedChanged += new DecentForms.EventHandler(this.btnToolFill_CheckedChanged);
            // 
            // btnToolSelect
            // 
            this.btnToolSelect.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolSelect.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolSelect.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolSelect.Checked = false;
            this.btnToolSelect.Image = ((System.Drawing.Image)(resources.GetObject("btnToolSelect.Image")));
            this.btnToolSelect.Location = new System.Drawing.Point(123, 3);
            this.btnToolSelect.Name = "btnToolSelect";
            this.btnToolSelect.Size = new System.Drawing.Size(24, 24);
            this.btnToolSelect.TabIndex = 7;
            this.toolTip1.SetToolTip(this.btnToolSelect, "Selection");
            this.btnToolSelect.CheckedChanged += new DecentForms.EventHandler(this.btnToolSelect_CheckedChanged);
            // 
            // btnToolMarker
            // 
            this.btnToolMarker.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolMarker.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolMarker.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolMarker.Checked = false;
            this.btnToolMarker.Image = null;
            this.btnToolMarker.Location = new System.Drawing.Point(153, 3);
            this.btnToolMarker.Name = "btnToolMarker";
            this.btnToolMarker.Size = new System.Drawing.Size(24, 24);
            this.btnToolMarker.TabIndex = 8;
            this.btnToolMarker.Text = "M";
            this.toolTip1.SetToolTip(this.btnToolMarker, "Markers");
            this.btnToolMarker.CheckedChanged += new DecentForms.EventHandler(this.btnToolMarker_CheckedChanged);
            //
            // comboMarkerTypes
            //
            this.comboMarkerTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMarkerTypes.Location = new System.Drawing.Point(183, 3);
            this.comboMarkerTypes.Name = "comboMarkerTypes";
            this.comboMarkerTypes.Size = new System.Drawing.Size(150, 21);
            this.comboMarkerTypes.TabIndex = 3;
            this.comboMarkerTypes.SelectedIndexChanged += new System.EventHandler(this.comboMarkerTypes_SelectedIndexChanged);
            //
            // labelMarkerValue1
            //
            this.labelMarkerValue1.AutoSize = true;
            this.labelMarkerValue1.Location = new System.Drawing.Point(339, 7);
            this.labelMarkerValue1.Name = "labelMarkerValue1";
            this.labelMarkerValue1.Size = new System.Drawing.Size(43, 13);
            this.labelMarkerValue1.TabIndex = 30;
            this.labelMarkerValue1.Text = "Value 1:";
            this.labelMarkerValue1.Margin = new System.Windows.Forms.Padding(3, 7, 0, 0);
            //
            // editMarkerValue1
            //
            this.editMarkerValue1.Location = new System.Drawing.Point(382, 3);
            this.editMarkerValue1.Maximum = new decimal(new int[] { 255, 0, 0, 0});
            this.editMarkerValue1.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this.editMarkerValue1.Name = "editMarkerValue1";
            this.editMarkerValue1.Size = new System.Drawing.Size(55, 20);
            this.editMarkerValue1.TabIndex = 31;
            this.editMarkerValue1.Value = new decimal(new int[] { 0, 0, 0, 0});
            this.editMarkerValue1.ValueChanged += new System.EventHandler(this.editMarkerValue_ValueChanged);
            //
            // labelMarkerValue2
            //
            this.labelMarkerValue2.AutoSize = true;
            this.labelMarkerValue2.Name = "labelMarkerValue2";
            this.labelMarkerValue2.Size = new System.Drawing.Size(43, 13);
            this.labelMarkerValue2.TabIndex = 32;
            this.labelMarkerValue2.Text = "Value 2:";
            this.labelMarkerValue2.Margin = new System.Windows.Forms.Padding(3, 7, 0, 0);
            //
            // editMarkerValue2
            //
            this.editMarkerValue2.Maximum = new decimal(new int[] { 255, 0, 0, 0});
            this.editMarkerValue2.Minimum = new decimal(new int[] { 0, 0, 0, 0});
            this.editMarkerValue2.Name = "editMarkerValue2";
            this.editMarkerValue2.Size = new System.Drawing.Size(55, 20);
            this.editMarkerValue2.TabIndex = 33;
            this.editMarkerValue2.Value = new decimal(new int[] { 0, 0, 0, 0});
            this.editMarkerValue2.ValueChanged += new System.EventHandler(this.editMarkerValue_ValueChanged);
            //
            // comboMarkerColorOverride
            // 
            this.comboMarkerColorOverride.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboMarkerColorOverride.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMarkerColorOverride.Location = new System.Drawing.Point(339, 3);
            this.comboMarkerColorOverride.Name = "comboMarkerColorOverride";
            this.comboMarkerColorOverride.Size = new System.Drawing.Size(48, 21);
            this.comboMarkerColorOverride.TabIndex = 4;
            this.comboMarkerColorOverride.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboMarkerColorOverride_DrawItem);
            this.comboMarkerColorOverride.SelectedIndexChanged += new System.EventHandler(this.comboMarkerColorOverride_SelectedIndexChanged);
            //
            // btnToolEntity
            //
            this.btnToolEntity.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnToolEntity.BorderStyle = DecentForms.BorderStyle.NONE;
            this.btnToolEntity.CheckAlign = DecentForms.ContentAlignment.MiddleLeft;
            this.btnToolEntity.Checked = false;
            this.btnToolEntity.Image = null;
            this.btnToolEntity.Name = "btnToolEntity";
            this.btnToolEntity.Size = new System.Drawing.Size(24, 24);
            this.btnToolEntity.TabIndex = 40;
            this.btnToolEntity.Text = "E";
            this.toolTip1.SetToolTip(this.btnToolEntity, "Entities");
            this.btnToolEntity.CheckedChanged += new DecentForms.EventHandler(this.btnToolEntity_CheckedChanged);
            //
            // comboEntityTypes
            //
            this.comboEntityTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboEntityTypes.Name = "comboEntityTypes";
            this.comboEntityTypes.Size = new System.Drawing.Size(150, 21);
            this.comboEntityTypes.TabIndex = 41;
            this.comboEntityTypes.SelectedIndexChanged += new System.EventHandler(this.comboEntityTypes_SelectedIndexChanged);
            //
            // labelEntityValue1
            //
            this.labelEntityValue1.AutoSize = true;
            this.labelEntityValue1.Margin = new System.Windows.Forms.Padding(3, 7, 0, 0);
            this.labelEntityValue1.Name = "labelEntityValue1";
            this.labelEntityValue1.Size = new System.Drawing.Size(43, 13);
            this.labelEntityValue1.TabIndex = 42;
            this.labelEntityValue1.Text = "Value 1:";
            //
            // editEntityValue1Default
            //
            this.editEntityValue1Default.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            this.editEntityValue1Default.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.editEntityValue1Default.Name = "editEntityValue1Default";
            this.editEntityValue1Default.Size = new System.Drawing.Size(55, 20);
            this.editEntityValue1Default.TabIndex = 43;
            //
            // labelEntityValue2
            //
            this.labelEntityValue2.AutoSize = true;
            this.labelEntityValue2.Margin = new System.Windows.Forms.Padding(3, 7, 0, 0);
            this.labelEntityValue2.Name = "labelEntityValue2";
            this.labelEntityValue2.Size = new System.Drawing.Size(43, 13);
            this.labelEntityValue2.TabIndex = 44;
            this.labelEntityValue2.Text = "Value 2:";
            //
            // editEntityValue2Default
            //
            this.editEntityValue2Default.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            this.editEntityValue2Default.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.editEntityValue2Default.Name = "editEntityValue2Default";
            this.editEntityValue2Default.Size = new System.Drawing.Size(55, 20);
            this.editEntityValue2Default.TabIndex = 45;
            //
            // checkEntityDefaultEnabled
            //
            this.checkEntityDefaultEnabled.AutoSize = true;
            this.checkEntityDefaultEnabled.Checked = true;
            this.checkEntityDefaultEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkEntityDefaultEnabled.Margin = new System.Windows.Forms.Padding(6, 5, 3, 0);
            this.checkEntityDefaultEnabled.Name = "checkEntityDefaultEnabled";
            this.checkEntityDefaultEnabled.Size = new System.Drawing.Size(120, 17);
            this.checkEntityDefaultEnabled.TabIndex = 46;
            this.checkEntityDefaultEnabled.Text = "Ent. Enabled";
            this.checkEntityDefaultEnabled.UseVisualStyleBackColor = true;
            //
            // checkShowEntities
            //
            this.checkShowEntities.AutoSize = true;
            this.checkShowEntities.Checked = true;
            this.checkShowEntities.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkShowEntities.Margin = new System.Windows.Forms.Padding(6, 5, 3, 0);
            this.checkShowEntities.Name = "checkShowEntities";
            this.checkShowEntities.Size = new System.Drawing.Size(110, 17);
            this.checkShowEntities.TabIndex = 47;
            this.checkShowEntities.Text = "Show Entities";
            this.checkShowEntities.UseVisualStyleBackColor = true;
            this.checkShowEntities.CheckedChanged += new System.EventHandler(this.checkShowEntities_CheckedChanged);
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnZoomOut.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnZoomOut.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnZoomOut.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnZoomOut.Image = null;
            this.btnZoomOut.Location = new System.Drawing.Point(393, 3);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(24, 24);
            this.btnZoomOut.TabIndex = 10;
            this.btnZoomOut.Text = "-";
            this.toolTip1.SetToolTip(this.btnZoomOut, "Zoom out");
            this.btnZoomOut.Click += new DecentForms.EventHandler(this.btnZoomOut_Click);
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnZoomIn.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnZoomIn.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnZoomIn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnZoomIn.Image = null;
            this.btnZoomIn.Location = new System.Drawing.Point(423, 3);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(24, 24);
            this.btnZoomIn.TabIndex = 11;
            this.btnZoomIn.Text = "+";
            this.toolTip1.SetToolTip(this.btnZoomIn, "Zoom in");
            this.btnZoomIn.Click += new DecentForms.EventHandler(this.btnZoomIn_Click);
            // 
            // labelZoom
            // 
            this.labelZoom.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelZoom.AutoSize = true;
            this.labelZoom.Location = new System.Drawing.Point(453, 10);
            this.labelZoom.Name = "labelZoom";
            this.labelZoom.Size = new System.Drawing.Size(33, 13);
            this.labelZoom.TabIndex = 9;
            this.labelZoom.Text = "100%";
            // 
            // btnCopyMapImage
            // 
            this.btnCopyMapImage.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCopyMapImage.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCopyMapImage.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCopyMapImage.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCopyMapImage.Image = ((System.Drawing.Image)(resources.GetObject("btnCopyMapImage.Image")));
            this.btnCopyMapImage.Location = new System.Drawing.Point(492, 3);
            this.btnCopyMapImage.Name = "btnCopyMapImage";
            this.btnCopyMapImage.Size = new System.Drawing.Size(39, 24);
            this.btnCopyMapImage.TabIndex = 8;
            this.toolTip1.SetToolTip(this.btnCopyMapImage, "Copy map to clipboard as image");
            this.btnCopyMapImage.Click += new DecentForms.EventHandler(this.btnCopyImage_Click);
            // 
            // btnShiftLeft
            // 
            this.btnShiftLeft.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftLeft.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftLeft.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftLeft.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftLeft.Image = null;
            this.btnShiftLeft.Location = new System.Drawing.Point(537, 3);
            this.btnShiftLeft.Name = "btnShiftLeft";
            this.btnShiftLeft.Size = new System.Drawing.Size(24, 24);
            this.btnShiftLeft.TabIndex = 12;
            this.btnShiftLeft.Text = "◄";
            this.toolTip1.SetToolTip(this.btnShiftLeft, "Shift Map Left");
            this.btnShiftLeft.Click += new DecentForms.EventHandler(this.btnShiftLeft_Click);
            // 
            // btnShiftUp
            // 
            this.btnShiftUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftUp.Image = null;
            this.btnShiftUp.Location = new System.Drawing.Point(567, 3);
            this.btnShiftUp.Name = "btnShiftUp";
            this.btnShiftUp.Size = new System.Drawing.Size(24, 24);
            this.btnShiftUp.TabIndex = 13;
            this.btnShiftUp.Text = "▲";
            this.toolTip1.SetToolTip(this.btnShiftUp, "Shift Map Up");
            this.btnShiftUp.Click += new DecentForms.EventHandler(this.btnShiftUp_Click);
            // 
            // btnShiftDown
            // 
            this.btnShiftDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftDown.Image = null;
            this.btnShiftDown.Location = new System.Drawing.Point(597, 3);
            this.btnShiftDown.Name = "btnShiftDown";
            this.btnShiftDown.Size = new System.Drawing.Size(24, 24);
            this.btnShiftDown.TabIndex = 14;
            this.btnShiftDown.Text = "▼";
            this.toolTip1.SetToolTip(this.btnShiftDown, "Shift Map Down");
            this.btnShiftDown.Click += new DecentForms.EventHandler(this.btnShiftDown_Click);
            // 
            // btnShiftRight
            // 
            this.btnShiftRight.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnShiftRight.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnShiftRight.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnShiftRight.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnShiftRight.Image = null;
            this.btnShiftRight.Location = new System.Drawing.Point(627, 3);
            this.btnShiftRight.Name = "btnShiftRight";
            this.btnShiftRight.Size = new System.Drawing.Size(24, 24);
            this.btnShiftRight.TabIndex = 15;
            this.btnShiftRight.Text = "►";
            this.toolTip1.SetToolTip(this.btnShiftRight, "Shift Map Right");
            this.btnShiftRight.Click += new DecentForms.EventHandler(this.btnShiftRight_Click);
            //
            // btnRemoveOverlappingTiles
            //
            this.btnRemoveOverlappingTiles.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnRemoveOverlappingTiles.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnRemoveOverlappingTiles.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnRemoveOverlappingTiles.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnRemoveOverlappingTiles.Image = null;
            this.btnRemoveOverlappingTiles.Location = new System.Drawing.Point(657, 3);
            this.btnRemoveOverlappingTiles.Name = "btnRemoveOverlappingTiles";
            this.btnRemoveOverlappingTiles.Size = new System.Drawing.Size(24, 24);
            this.btnRemoveOverlappingTiles.TabIndex = 16;
            this.btnRemoveOverlappingTiles.Text = "⊘";
            this.toolTip1.SetToolTip(this.btnRemoveOverlappingTiles, "Remove tiles that are overlapped by larger tiles placed earlier in reading order");
            this.btnRemoveOverlappingTiles.Click += new DecentForms.EventHandler(this.btnRemoveOverlappingTiles_Click);
            // 
            // checkAutoTiling
            // 
            this.checkAutoTiling.Location = new System.Drawing.Point(657, 3);
            this.checkAutoTiling.Name = "checkAutoTiling";
            this.checkAutoTiling.Size = new System.Drawing.Size(73, 27);
            this.checkAutoTiling.TabIndex = 16;
            this.checkAutoTiling.Text = "Auto-tiling";
            this.checkAutoTiling.UseVisualStyleBackColor = true;
            // 
            // labelEditInfo
            // 
            this.labelEditInfo.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelEditInfo.Location = new System.Drawing.Point(3, 33);
            this.labelEditInfo.Name = "labelEditInfo";
            this.labelEditInfo.Size = new System.Drawing.Size(211, 23);
            this.labelEditInfo.TabIndex = 9;
            this.labelEditInfo.Text = "Tile Info";
            // 
            // comboTiles
            // 
            this.comboTiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.comboTiles.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboTiles.FormattingEnabled = true;
            this.comboTiles.IntegralHeight = false;
            this.comboTiles.ItemHeight = 24;
            this.comboTiles.Location = new System.Drawing.Point(8, 6);
            this.comboTiles.Name = "comboTiles";
            this.comboTiles.Size = new System.Drawing.Size(160, 614);
            this.comboTiles.TabIndex = 2;
            this.comboTiles.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboTiles_DrawItem);
            this.comboTiles.SelectedIndexChanged += new System.EventHandler(this.comboTiles_SelectedIndexChanged);
            // 
            // mapHScroll
            // 
            this.mapHScroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mapHScroll.BorderStyle = DecentForms.BorderStyle.NONE;
            this.mapHScroll.DisplayType = DecentForms.ScrollBar.SBDisplayType.RAISED;
            this.mapHScroll.Enabled = false;
            this.mapHScroll.LargeChange = 10;
            this.mapHScroll.Location = new System.Drawing.Point(174, 599);
            this.mapHScroll.Maximum = 100;
            this.mapHScroll.Minimum = 0;
            this.mapHScroll.Name = "mapHScroll";
            this.mapHScroll.Size = new System.Drawing.Size(819, 20);
            this.mapHScroll.SmallChange = 1;
            this.mapHScroll.TabIndex = 24;
            this.mapHScroll.Value = 0;
            this.mapHScroll.Scroll += new DecentForms.EventHandler(this.mapHScroll_Scroll);
            // 
            // mapVScroll
            // 
            this.mapVScroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mapVScroll.BorderStyle = DecentForms.BorderStyle.NONE;
            this.mapVScroll.DisplayType = DecentForms.ScrollBar.SBDisplayType.RAISED;
            this.mapVScroll.Enabled = false;
            this.mapVScroll.LargeChange = 10;
            this.mapVScroll.Location = new System.Drawing.Point(989, 39);
            this.mapVScroll.Maximum = 100;
            this.mapVScroll.Minimum = 0;
            this.mapVScroll.Name = "mapVScroll";
            this.mapVScroll.Size = new System.Drawing.Size(21, 554);
            this.mapVScroll.SmallChange = 1;
            this.mapVScroll.TabIndex = 23;
            this.mapVScroll.Value = 0;
            this.mapVScroll.Scroll += new DecentForms.EventHandler(this.mapVScroll_Scroll);
            // 
            // tabMapEditor
            // 
            this.tabMapEditor.Pages.AddRange(new Krypton.Navigator.KryptonPage[] {
                this.tabEditor,
                this.tabTiles,
                this.tabCharset,
                this.tabExport,
                this.tabImport,
                this.tabMarkers,
                this.tabEntities});
            this.tabMapEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMapEditor.Location = new System.Drawing.Point(0, 24);
            this.tabMapEditor.Name = "tabMapEditor";
            this.tabMapEditor.SelectedIndex = 0;
            this.tabMapEditor.Size = new System.Drawing.Size(1393, 654);
            this.tabMapEditor.TabIndex = 0;
            this.tabMapEditor.SelectedPageChanged += new System.EventHandler(this.tabMapEditor_SelectedIndexChanged);
            // 
            // tabTiles
            // 
            this.tabTiles.Controls.Add(this.labelSwatchSize);
            this.tabTiles.Controls.Add(this.editSwatchSize);
            this.tabTiles.Controls.Add(this.btnTileApply);
            this.tabTiles.Controls.Add(this.btnGetTileCount);
            this.tabTiles.Controls.Add(this.btnCopyTileCharToNextIncreased);
            this.tabTiles.Controls.Add(this.btnSetNextTileChar);
            this.tabTiles.Controls.Add(this.btnMoveTileDown);
            this.tabTiles.Controls.Add(this.btnMoveTileUp);
            this.tabTiles.Controls.Add(this.btnTileDelete);
            this.tabTiles.Controls.Add(this.btnTileClone);
            this.tabTiles.Controls.Add(this.btnTileAdd);
            this.tabTiles.Controls.Add(this.listTileChars);
            this.tabTiles.Controls.Add(this.listTileInfo);
            this.tabTiles.Controls.Add(this.editTileName);
            this.tabTiles.Controls.Add(this.editTileGroupId);
            this.tabTiles.Controls.Add(this.labelTileGroupId);
            this.tabTiles.Controls.Add(this.editTileHeight);
            this.tabTiles.Controls.Add(this.editTileWidth);
            this.tabTiles.Controls.Add(this.checkNotExportedOnMap);
            this.tabTiles.Controls.Add(this.checkTilePassable);
            this.tabTiles.Controls.Add(this.label17);
            this.tabTiles.Controls.Add(this.labelTilesBGColor4);
            this.tabTiles.Controls.Add(this.labelTilesMulticolor2);
            this.tabTiles.Controls.Add(this.label16);
            this.tabTiles.Controls.Add(this.labelTilesMulticolor1);
            this.tabTiles.Controls.Add(this.label4);
            this.tabTiles.Controls.Add(this.label15);
            this.tabTiles.Controls.Add(this.panelCharacters);
            this.tabTiles.Controls.Add(this.comboTileBGColor4);
            this.tabTiles.Controls.Add(this.comboTileMulticolor2);
            this.tabTiles.Controls.Add(this.comboTileMulticolor1);
            this.tabTiles.Controls.Add(this.comboTileBackground);
            this.tabTiles.Controls.Add(this.panelCharColors);
            this.tabTiles.Controls.Add(this.pictureTileDisplay);
            this.tabTiles.Location = new System.Drawing.Point(4, 22);
            this.tabTiles.Name = "tabTiles";
            this.tabTiles.Padding = new System.Windows.Forms.Padding(3);
            this.tabTiles.Size = new System.Drawing.Size(192, 74);
            this.tabTiles.TabIndex = 2;
            this.tabTiles.Text = "Tiles";
            // 
            // labelSwatchSize
            // 
            this.labelSwatchSize.AutoSize = true;
            this.labelSwatchSize.Location = new System.Drawing.Point(993, 295);
            this.labelSwatchSize.Name = "labelSwatchSize";
            this.labelSwatchSize.Size = new System.Drawing.Size(69, 13);
            this.labelSwatchSize.TabIndex = 22;
            this.labelSwatchSize.Text = "Swatch Size:";
            // 
            // editSwatchSize
            // 
            this.editSwatchSize.Location = new System.Drawing.Point(1068, 292);
            this.editSwatchSize.Name = "editSwatchSize";
            this.editSwatchSize.Size = new System.Drawing.Size(40, 20);
            this.editSwatchSize.TabIndex = 23;
            this.editSwatchSize.Text = "16";
            this.editSwatchSize.KeyDown += new System.Windows.Forms.KeyEventHandler(this.editSwatchSize_KeyDown);
            this.editSwatchSize.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editSwatchSize_KeyPress);
            // 
            // btnTileApply
            // 
            this.btnTileApply.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnTileApply.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnTileApply.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnTileApply.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnTileApply.Enabled = false;
            this.btnTileApply.Image = null;
            this.btnTileApply.Location = new System.Drawing.Point(522, 85);
            this.btnTileApply.Name = "btnTileApply";
            this.btnTileApply.Size = new System.Drawing.Size(58, 23);
            this.btnTileApply.TabIndex = 25;
            this.btnTileApply.Text = "Apply";
            this.btnTileApply.Click += new DecentForms.EventHandler(this.btnTileApply_Click);
            // 
            // btnGetTileCount
            // 
            this.btnGetTileCount.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnGetTileCount.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnGetTileCount.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnGetTileCount.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnGetTileCount.Image = null;
            this.btnGetTileCount.Location = new System.Drawing.Point(419, 434);
            this.btnGetTileCount.Name = "btnGetTileCount";
            this.btnGetTileCount.Size = new System.Drawing.Size(87, 23);
            this.btnGetTileCount.TabIndex = 26;
            this.btnGetTileCount.Text = "Get tile count";
            this.btnGetTileCount.Click += new DecentForms.EventHandler(this.btnGetTileCount_Click);
            // 
            // btnCopyTileCharToNextIncreased
            // 
            this.btnCopyTileCharToNextIncreased.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnCopyTileCharToNextIncreased.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnCopyTileCharToNextIncreased.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnCopyTileCharToNextIncreased.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCopyTileCharToNextIncreased.Enabled = false;
            this.btnCopyTileCharToNextIncreased.Image = null;
            this.btnCopyTileCharToNextIncreased.Location = new System.Drawing.Point(639, 245);
            this.btnCopyTileCharToNextIncreased.Name = "btnCopyTileCharToNextIncreased";
            this.btnCopyTileCharToNextIncreased.Size = new System.Drawing.Size(75, 23);
            this.btnCopyTileCharToNextIncreased.TabIndex = 28;
            this.btnCopyTileCharToNextIncreased.Text = "Copy inc\'ed";
            this.toolTip1.SetToolTip(this.btnCopyTileCharToNextIncreased, "Copy char+1/color to next slot");
            this.btnCopyTileCharToNextIncreased.Click += new DecentForms.EventHandler(this.btnCopyTileCharToNextIncreased_Click);
            // 
            // btnSetNextTileChar
            // 
            this.btnSetNextTileChar.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnSetNextTileChar.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnSetNextTileChar.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnSetNextTileChar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSetNextTileChar.Enabled = false;
            this.btnSetNextTileChar.Image = null;
            this.btnSetNextTileChar.Location = new System.Drawing.Point(639, 214);
            this.btnSetNextTileChar.Name = "btnSetNextTileChar";
            this.btnSetNextTileChar.Size = new System.Drawing.Size(75, 23);
            this.btnSetNextTileChar.TabIndex = 28;
            this.btnSetNextTileChar.Text = "Copy to next";
            this.toolTip1.SetToolTip(this.btnSetNextTileChar, "Copy char/color to next slot");
            this.btnSetNextTileChar.Click += new DecentForms.EventHandler(this.btnSetNextTileChar_Click);
            // 
            // btnMoveTileDown
            // 
            this.btnMoveTileDown.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveTileDown.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveTileDown.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveTileDown.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveTileDown.Enabled = false;
            this.btnMoveTileDown.Image = null;
            this.btnMoveTileDown.Location = new System.Drawing.Point(419, 405);
            this.btnMoveTileDown.Name = "btnMoveTileDown";
            this.btnMoveTileDown.Size = new System.Drawing.Size(44, 23);
            this.btnMoveTileDown.TabIndex = 26;
            this.btnMoveTileDown.Text = "Down";
            this.btnMoveTileDown.Click += new DecentForms.EventHandler(this.btnMoveTileDown_Click);
            // 
            // btnMoveTileUp
            // 
            this.btnMoveTileUp.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnMoveTileUp.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnMoveTileUp.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnMoveTileUp.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnMoveTileUp.Enabled = false;
            this.btnMoveTileUp.Image = null;
            this.btnMoveTileUp.Location = new System.Drawing.Point(419, 376);
            this.btnMoveTileUp.Name = "btnMoveTileUp";
            this.btnMoveTileUp.Size = new System.Drawing.Size(44, 23);
            this.btnMoveTileUp.TabIndex = 26;
            this.btnMoveTileUp.Text = "Up";
            this.btnMoveTileUp.Click += new DecentForms.EventHandler(this.btnMoveTileUp_Click);
            // 
            // btnTileDelete
            // 
            this.btnTileDelete.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnTileDelete.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnTileDelete.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnTileDelete.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnTileDelete.Enabled = false;
            this.btnTileDelete.Image = null;
            this.btnTileDelete.Location = new System.Drawing.Point(522, 114);
            this.btnTileDelete.Name = "btnTileDelete";
            this.btnTileDelete.Size = new System.Drawing.Size(58, 23);
            this.btnTileDelete.TabIndex = 25;
            this.btnTileDelete.Text = "Delete";
            this.btnTileDelete.Click += new DecentForms.EventHandler(this.btnTileDelete_Click);
            // 
            // btnTileClone
            // 
            this.btnTileClone.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnTileClone.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnTileClone.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnTileClone.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnTileClone.Enabled = false;
            this.btnTileClone.Image = null;
            this.btnTileClone.Location = new System.Drawing.Point(452, 114);
            this.btnTileClone.Name = "btnTileClone";
            this.btnTileClone.Size = new System.Drawing.Size(58, 23);
            this.btnTileClone.TabIndex = 25;
            this.btnTileClone.Text = "Clone";
            this.btnTileClone.Click += new DecentForms.EventHandler(this.btnCloneTile_Click);
            // 
            // btnTileAdd
            // 
            this.btnTileAdd.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnTileAdd.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnTileAdd.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnTileAdd.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnTileAdd.Image = null;
            this.btnTileAdd.Location = new System.Drawing.Point(452, 85);
            this.btnTileAdd.Name = "btnTileAdd";
            this.btnTileAdd.Size = new System.Drawing.Size(58, 23);
            this.btnTileAdd.TabIndex = 25;
            this.btnTileAdd.Text = "Add";
            this.btnTileAdd.Click += new DecentForms.EventHandler(this.btnAddTile_Click);
            // 
            // listTileChars
            // 
            this.listTileChars.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7});
            this.listTileChars.FullRowSelect = true;
            this.listTileChars.HideSelection = false;
            this.listTileChars.Location = new System.Drawing.Point(589, 5);
            this.listTileChars.Name = "listTileChars";
            this.listTileChars.OwnerDraw = true;
            this.listTileChars.SelectedTextBGColor = ((uint)(4294901760u));
            this.listTileChars.SelectedTextColor = ((uint)(4294967295u));
            this.listTileChars.Size = new System.Drawing.Size(125, 203);
            this.listTileChars.TabIndex = 24;
            this.listTileChars.UseCompatibleStateImageBehavior = false;
            this.listTileChars.View = System.Windows.Forms.View.Details;
            this.listTileChars.SelectedIndexChanged += new System.EventHandler(this.listTileChars_SelectedIndexChanged);
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Nr.";
            this.columnHeader5.Width = 0;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Char";
            this.columnHeader6.Width = 56;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Color";
            this.columnHeader7.Width = 55;
            // 
            // listTileInfo
            // 
            this.listTileInfo.AllowDrop = true;
            this.listTileInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listTileInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listTileInfo.FullRowSelect = true;
            this.listTileInfo.HideSelection = false;
            this.listTileInfo.Location = new System.Drawing.Point(3, 8);
            this.listTileInfo.Name = "listTileInfo";
            this.listTileInfo.OwnerDraw = true;
            this.listTileInfo.SelectedTextBGColor = ((uint)(4294901760u));
            this.listTileInfo.SelectedTextColor = ((uint)(4294967295u));
            this.listTileInfo.Size = new System.Drawing.Size(407, 58);
            this.listTileInfo.TabIndex = 24;
            this.listTileInfo.UseCompatibleStateImageBehavior = false;
            this.listTileInfo.View = System.Windows.Forms.View.Details;
            this.listTileInfo.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.listTileInfo_ItemDrag);
            this.listTileInfo.SelectedIndexChanged += new System.EventHandler(this.listTileInfo_SelectedIndexChanged);
            this.listTileInfo.DragDrop += new System.Windows.Forms.DragEventHandler(this.listTileInfo_DragDrop);
            this.listTileInfo.DragEnter += new System.Windows.Forms.DragEventHandler(this.listTileInfo_DragEnter);
            this.listTileInfo.DragOver += new System.Windows.Forms.DragEventHandler(this.listTileInfo_DragOver);
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Nr.";
            this.columnHeader4.Width = 35;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 200;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Info";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Used No.";
            // 
            // editTileName
            // 
            this.editTileName.Location = new System.Drawing.Point(480, 59);
            this.editTileName.Name = "editTileName";
            this.editTileName.Size = new System.Drawing.Size(100, 20);
            this.editTileName.TabIndex = 23;
            // 
            // editTileGroupId
            // 
            this.editTileGroupId.Location = new System.Drawing.Point(498, 264);
            this.editTileGroupId.MaxLength = 5;
            this.editTileGroupId.Name = "editTileGroupId";
            this.editTileGroupId.Size = new System.Drawing.Size(59, 20);
            this.editTileGroupId.TabIndex = 23;
            this.editTileGroupId.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editTileGroupId_KeyPress);
            // 
            // labelTileGroupId
            // 
            this.labelTileGroupId.AutoSize = true;
            this.labelTileGroupId.Location = new System.Drawing.Point(416, 267);
            this.labelTileGroupId.Name = "labelTileGroupId";
            this.labelTileGroupId.Size = new System.Drawing.Size(51, 13);
            this.labelTileGroupId.TabIndex = 22;
            this.labelTileGroupId.Text = "Group Id:";
            // 
            // editTileHeight
            // 
            this.editTileHeight.Location = new System.Drawing.Point(480, 32);
            this.editTileHeight.Name = "editTileHeight";
            this.editTileHeight.Size = new System.Drawing.Size(100, 20);
            this.editTileHeight.TabIndex = 23;
            // 
            // editTileWidth
            // 
            this.editTileWidth.Location = new System.Drawing.Point(480, 5);
            this.editTileWidth.Name = "editTileWidth";
            this.editTileWidth.Size = new System.Drawing.Size(100, 20);
            this.editTileWidth.TabIndex = 23;
            // 
            // checkNotExportedOnMap
            // 
            this.checkNotExportedOnMap.AutoSize = true;
            this.checkNotExportedOnMap.Location = new System.Drawing.Point(419, 315);
            this.checkNotExportedOnMap.Name = "checkNotExportedOnMap";
            this.checkNotExportedOnMap.Size = new System.Drawing.Size(125, 17);
            this.checkNotExportedOnMap.TabIndex = 23;
            this.checkNotExportedOnMap.Text = "Not exported on map";
            this.checkNotExportedOnMap.UseVisualStyleBackColor = true;
            this.checkNotExportedOnMap.CheckedChanged += new System.EventHandler(this.checkNotExportedOnMap_CheckedChanged);
            // 
            // checkTilePassable
            // 
            this.checkTilePassable.AutoSize = true;
            this.checkTilePassable.Checked = true;
            this.checkTilePassable.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkTilePassable.Location = new System.Drawing.Point(419, 293);
            this.checkTilePassable.Name = "checkTilePassable";
            this.checkTilePassable.Size = new System.Drawing.Size(69, 17);
            this.checkTilePassable.TabIndex = 23;
            this.checkTilePassable.Text = "Passable";
            this.checkTilePassable.UseVisualStyleBackColor = true;
            this.checkTilePassable.CheckedChanged += new System.EventHandler(this.checkTilePassable_CheckedChanged);
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(416, 62);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(38, 13);
            this.label17.TabIndex = 22;
            this.label17.Text = "Name:";
            // 
            // labelTilesBGColor4
            // 
            this.labelTilesBGColor4.AutoSize = true;
            this.labelTilesBGColor4.Location = new System.Drawing.Point(416, 239);
            this.labelTilesBGColor4.Name = "labelTilesBGColor4";
            this.labelTilesBGColor4.Size = new System.Drawing.Size(61, 13);
            this.labelTilesBGColor4.TabIndex = 22;
            this.labelTilesBGColor4.Text = "BG Color 4:";
            // 
            // labelTilesMulticolor2
            // 
            this.labelTilesMulticolor2.AutoSize = true;
            this.labelTilesMulticolor2.Location = new System.Drawing.Point(416, 213);
            this.labelTilesMulticolor2.Name = "labelTilesMulticolor2";
            this.labelTilesMulticolor2.Size = new System.Drawing.Size(64, 13);
            this.labelTilesMulticolor2.TabIndex = 22;
            this.labelTilesMulticolor2.Text = "Multicolor 2:";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(416, 35);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(61, 13);
            this.label16.TabIndex = 22;
            this.label16.Text = "Tile Height:";
            // 
            // labelTilesMulticolor1
            // 
            this.labelTilesMulticolor1.AutoSize = true;
            this.labelTilesMulticolor1.Location = new System.Drawing.Point(416, 187);
            this.labelTilesMulticolor1.Name = "labelTilesMulticolor1";
            this.labelTilesMulticolor1.Size = new System.Drawing.Size(64, 13);
            this.labelTilesMulticolor1.TabIndex = 22;
            this.labelTilesMulticolor1.Text = "Multicolor 1:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(416, 8);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "Tile Width:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(416, 160);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(68, 13);
            this.label15.TabIndex = 22;
            this.label15.Text = "Background:";
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
            this.panelCharacters.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelCharacters.EnableAutoScrollHorizontal = true;
            this.panelCharacters.EnableAutoScrollVertical = true;
            this.panelCharacters.HottrackColor = ((uint)(2151694591u));
            this.panelCharacters.ItemHeight = 8;
            this.panelCharacters.ItemWidth = 8;
            this.panelCharacters.Location = new System.Drawing.Point(725, 5);
            this.panelCharacters.Name = "panelCharacters";
            this.panelCharacters.PixelFormat = GR.Drawing.PixelFormat.DontCare;
            this.panelCharacters.SelectedIndex = -1;
            this.panelCharacters.Size = new System.Drawing.Size(260, 260);
            this.panelCharacters.TabIndex = 21;
            this.panelCharacters.TabStop = true;
            this.panelCharacters.VisibleAutoScrollHorizontal = false;
            this.panelCharacters.VisibleAutoScrollVertical = false;
            this.panelCharacters.SelectedIndexChanged += new System.EventHandler(this.panelCharacters_SelectedIndexChanged);
            // 
            // comboTileBGColor4
            // 
            this.comboTileBGColor4.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboTileBGColor4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTileBGColor4.FormattingEnabled = true;
            this.comboTileBGColor4.Location = new System.Drawing.Point(498, 236);
            this.comboTileBGColor4.Name = "comboTileBGColor4";
            this.comboTileBGColor4.Size = new System.Drawing.Size(59, 21);
            this.comboTileBGColor4.TabIndex = 1;
            this.comboTileBGColor4.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            this.comboTileBGColor4.SelectedIndexChanged += new System.EventHandler(this.comboBGColor4_SelectedIndexChanged);
            // 
            // comboTileMulticolor2
            // 
            this.comboTileMulticolor2.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboTileMulticolor2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTileMulticolor2.FormattingEnabled = true;
            this.comboTileMulticolor2.Location = new System.Drawing.Point(498, 210);
            this.comboTileMulticolor2.Name = "comboTileMulticolor2";
            this.comboTileMulticolor2.Size = new System.Drawing.Size(59, 21);
            this.comboTileMulticolor2.TabIndex = 1;
            this.comboTileMulticolor2.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            this.comboTileMulticolor2.SelectedIndexChanged += new System.EventHandler(this.comboMulticolor2_SelectedIndexChanged);
            // 
            // comboTileMulticolor1
            // 
            this.comboTileMulticolor1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboTileMulticolor1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTileMulticolor1.FormattingEnabled = true;
            this.comboTileMulticolor1.Location = new System.Drawing.Point(498, 183);
            this.comboTileMulticolor1.Name = "comboTileMulticolor1";
            this.comboTileMulticolor1.Size = new System.Drawing.Size(59, 21);
            this.comboTileMulticolor1.TabIndex = 1;
            this.comboTileMulticolor1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            this.comboTileMulticolor1.SelectedIndexChanged += new System.EventHandler(this.comboMulticolor1_SelectedIndexChanged);
            // 
            // comboTileBackground
            // 
            this.comboTileBackground.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboTileBackground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboTileBackground.FormattingEnabled = true;
            this.comboTileBackground.Location = new System.Drawing.Point(498, 157);
            this.comboTileBackground.Name = "comboTileBackground";
            this.comboTileBackground.Size = new System.Drawing.Size(59, 21);
            this.comboTileBackground.TabIndex = 1;
            this.comboTileBackground.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboColor_DrawItem);
            this.comboTileBackground.SelectedIndexChanged += new System.EventHandler(this.comboBackground_SelectedIndexChanged_1);
            // 
            // panelCharColors
            // 
            this.panelCharColors.AutoResize = false;
            this.panelCharColors.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelCharColors.DisplayPage = fastImage6;
            this.panelCharColors.Image = null;
            this.panelCharColors.Location = new System.Drawing.Point(725, 271);
            this.panelCharColors.Name = "panelCharColors";
            this.panelCharColors.Size = new System.Drawing.Size(260, 20);
            this.panelCharColors.TabIndex = 0;
            this.panelCharColors.TabStop = false;
            this.panelCharColors.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelCharColors_MouseDown);
            this.panelCharColors.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelCharColors_MouseMove);
            // 
            // pictureTileDisplay
            // 
            this.pictureTileDisplay.AutoResize = false;
            this.pictureTileDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureTileDisplay.DisplayPage = fastImage7;
            this.pictureTileDisplay.Image = null;
            this.pictureTileDisplay.Location = new System.Drawing.Point(996, 5);
            this.pictureTileDisplay.Name = "pictureTileDisplay";
            this.pictureTileDisplay.Size = new System.Drawing.Size(260, 260);
            this.pictureTileDisplay.TabIndex = 0;
            this.pictureTileDisplay.TabStop = false;
            this.pictureTileDisplay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureTileDisplay_MouseDown);
            this.pictureTileDisplay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureTileDisplay_MouseMove);
            // 
            // tabCharset
            // 
            this.tabCharset.Controls.Add(this.characterEditor);
            this.tabCharset.Location = new System.Drawing.Point(4, 22);
            this.tabCharset.Name = "tabCharset";
            this.tabCharset.Size = new System.Drawing.Size(192, 74);
            this.tabCharset.TabIndex = 3;
            this.tabCharset.Text = "Character Set";
            // 
            // characterEditor
            // 
            this.characterEditor.AllowModeChange = false;
            this.characterEditor.CharacterMapUsageText = "[map use]";
            this.characterEditor.CharactersPerRow = 64;
            this.characterEditor.CharacterUsageText = "[tile use]";
            this.characterEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.characterEditor.EditorMode = 1;
            this.characterEditor.Location = new System.Drawing.Point(0, 0);
            this.characterEditor.Name = "characterEditor";
            this.characterEditor.ShowCreateTileButton = false;
            this.characterEditor.Size = new System.Drawing.Size(192, 74);
            this.characterEditor.SwatchSize = 16;
            this.characterEditor.TabIndex = 0;
            this.characterEditor.Modified += new RetroDevStudio.Controls.CharacterEditor.ModifiedHandler(this.characterEditor_Modified);
            this.characterEditor.CharactersShifted += new RetroDevStudio.Controls.CharacterEditor.CharsetShiftedHandler(this.characterEditor_CharactersShifted);
            this.characterEditor.Load += new System.EventHandler(this.characterEditor_Load);
            // 
            // tabExport
            // 
            this.tabExport.Controls.Add(this.label5);
            this.tabExport.Controls.Add(this.comboExportOrientation);
            this.tabExport.Controls.Add(this.panelExport);
            this.tabExport.Controls.Add(this.editDataExport);
            this.tabExport.Controls.Add(this.btnExport);
            this.tabExport.Controls.Add(this.comboExportMethod);
            this.tabExport.Controls.Add(this.label24);
            this.tabExport.Controls.Add(this.label6);
            this.tabExport.Controls.Add(this.comboExportData);
            this.tabExport.Location = new System.Drawing.Point(4, 22);
            this.tabExport.Name = "tabExport";
            this.tabExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabExport.Size = new System.Drawing.Size(192, 74);
            this.tabExport.TabIndex = 4;
            this.tabExport.Text = "Export";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 34);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 43;
            this.label5.Text = "Orientation:";
            // 
            // comboExportOrientation
            // 
            this.comboExportOrientation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboExportOrientation.FormattingEnabled = true;
            this.comboExportOrientation.Items.AddRange(new object[] {
            "row by row",
            "column by column"});
            this.comboExportOrientation.Location = new System.Drawing.Point(121, 31);
            this.comboExportOrientation.Name = "comboExportOrientation";
            this.comboExportOrientation.Size = new System.Drawing.Size(317, 21);
            this.comboExportOrientation.TabIndex = 42;
            // 
            // panelExport
            // 
            this.panelExport.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panelExport.Location = new System.Drawing.Point(9, 83);
            this.panelExport.Name = "panelExport";
            this.panelExport.Size = new System.Drawing.Size(842, 0);
            this.panelExport.TabIndex = 41;
            // 
            // editDataExport
            // 
            this.editDataExport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editDataExport.Location = new System.Drawing.Point(857, 6);
            this.editDataExport.Multiline = true;
            this.editDataExport.Name = "editDataExport";
            this.editDataExport.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.editDataExport.Size = new System.Drawing.Size(0, 60);
            this.editDataExport.TabIndex = 40;
            this.editDataExport.WordWrap = false;
            // 
            // btnExport
            // 
            this.btnExport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnExport.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnExport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnExport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExport.Image = null;
            this.btnExport.Location = new System.Drawing.Point(363, 56);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 21);
            this.btnExport.TabIndex = 36;
            this.btnExport.Text = "Export";
            this.btnExport.Click += new DecentForms.EventHandler(this.btnExport_Click);
            // 
            // comboExportMethod
            // 
            this.comboExportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboExportMethod.FormattingEnabled = true;
            this.comboExportMethod.Location = new System.Drawing.Point(121, 56);
            this.comboExportMethod.Name = "comboExportMethod";
            this.comboExportMethod.Size = new System.Drawing.Size(236, 21);
            this.comboExportMethod.TabIndex = 34;
            this.comboExportMethod.SelectedIndexChanged += new System.EventHandler(this.comboExportMethod_SelectedIndexChanged);
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(6, 59);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(79, 13);
            this.label24.TabIndex = 35;
            this.label24.Text = "Export Method:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Export Data:";
            // 
            // comboExportData
            // 
            this.comboExportData.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboExportData.FormattingEnabled = true;
            this.comboExportData.Location = new System.Drawing.Point(121, 6);
            this.comboExportData.Name = "comboExportData";
            this.comboExportData.Size = new System.Drawing.Size(317, 21);
            this.comboExportData.TabIndex = 12;
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
            this.tabImport.Size = new System.Drawing.Size(192, 74);
            this.tabImport.TabIndex = 5;
            this.tabImport.Text = "Import";
            // 
            // panelImport
            // 
            this.panelImport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelImport.Location = new System.Drawing.Point(-4, 32);
            this.panelImport.Name = "panelImport";
            this.panelImport.Size = new System.Drawing.Size(196, 42);
            this.panelImport.TabIndex = 37;
            // 
            // btnImport
            // 
            this.btnImport.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.btnImport.BorderStyle = DecentForms.BorderStyle.FLAT;
            this.btnImport.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
            this.btnImport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnImport.Image = null;
            this.btnImport.Location = new System.Drawing.Point(355, 5);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(88, 21);
            this.btnImport.TabIndex = 36;
            this.btnImport.Text = "Import";
            this.btnImport.Click += new DecentForms.EventHandler(this.btnImport_Click);
            // 
            // comboImportMethod
            // 
            this.comboImportMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboImportMethod.FormattingEnabled = true;
            this.comboImportMethod.Location = new System.Drawing.Point(93, 5);
            this.comboImportMethod.Name = "comboImportMethod";
            this.comboImportMethod.Size = new System.Drawing.Size(256, 21);
            this.comboImportMethod.TabIndex = 34;
            this.comboImportMethod.SelectedIndexChanged += new System.EventHandler(this.comboImportMethod_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 35;
            this.label2.Text = "Import Method:";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.imageListbox1);
            this.tabPage1.Controls.Add(this.checkBox1);
            this.tabPage1.Controls.Add(this.comboBox1);
            this.tabPage1.Controls.Add(this.comboBox2);
            this.tabPage1.Controls.Add(this.comboBox3);
            this.tabPage1.Controls.Add(this.fastPictureBox1);
            this.tabPage1.Controls.Add(this.fastPictureBox2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(956, 475);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Screen";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(658, 65);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 13);
            this.label7.TabIndex = 22;
            this.label7.Text = "Multicolor 2:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(658, 11);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(68, 13);
            this.label9.TabIndex = 22;
            this.label9.Text = "Background:";
            // 
            // imageListbox1
            // 
            this.imageListbox1.AllowPopup = false;
            this.imageListbox1.AutoScroll = true;
            this.imageListbox1.AutoScrollHorizontalMaximum = 100;
            this.imageListbox1.AutoScrollHorizontalMinimum = 0;
            this.imageListbox1.AutoScrollHPos = 0;
            this.imageListbox1.AutoScrollVerticalMaximum = -23;
            this.imageListbox1.AutoScrollVerticalMinimum = 0;
            this.imageListbox1.AutoScrollVPos = 0;
            this.imageListbox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.imageListbox1.EnableAutoScrollHorizontal = true;
            this.imageListbox1.EnableAutoScrollVertical = true;
            this.imageListbox1.HottrackColor = ((uint)(2151694591u));
            this.imageListbox1.ItemHeight = 8;
            this.imageListbox1.ItemWidth = 8;
            this.imageListbox1.Location = new System.Drawing.Point(658, 124);
            this.imageListbox1.Name = "imageListbox1";
            this.imageListbox1.PixelFormat = GR.Drawing.PixelFormat.DontCare;
            this.imageListbox1.SelectedIndex = -1;
            this.imageListbox1.Size = new System.Drawing.Size(260, 260);
            this.imageListbox1.TabIndex = 21;
            this.imageListbox1.TabStop = true;
            this.imageListbox1.VisibleAutoScrollHorizontal = false;
            this.imageListbox1.VisibleAutoScrollVertical = false;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(658, 94);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(71, 17);
            this.checkBox1.TabIndex = 3;
            this.checkBox1.Text = "Multicolor";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(765, 62);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 21);
            this.comboBox1.TabIndex = 1;
            // 
            // comboBox2
            // 
            this.comboBox2.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(765, 35);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(121, 21);
            this.comboBox2.TabIndex = 1;
            // 
            // comboBox3
            // 
            this.comboBox3.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(765, 8);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(121, 21);
            this.comboBox3.TabIndex = 1;
            // 
            // fastPictureBox1
            // 
            this.fastPictureBox1.AutoResize = false;
            this.fastPictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.fastPictureBox1.DisplayPage = fastImage8;
            this.fastPictureBox1.Image = null;
            this.fastPictureBox1.Location = new System.Drawing.Point(658, 390);
            this.fastPictureBox1.Name = "fastPictureBox1";
            this.fastPictureBox1.Size = new System.Drawing.Size(260, 20);
            this.fastPictureBox1.TabIndex = 0;
            this.fastPictureBox1.TabStop = false;
            // 
            // fastPictureBox2
            // 
            this.fastPictureBox2.AutoResize = false;
            this.fastPictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.fastPictureBox2.DisplayPage = fastImage5;
            this.fastPictureBox2.Image = null;
            this.fastPictureBox2.Location = new System.Drawing.Point(8, 6);
            this.fastPictureBox2.Name = "fastPictureBox2";
            this.fastPictureBox2.Size = new System.Drawing.Size(644, 404);
            this.fastPictureBox2.TabIndex = 0;
            this.fastPictureBox2.TabStop = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(658, 38);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 13);
            this.label8.TabIndex = 22;
            this.label8.Text = "Multicolor 1:";
            // 
            // MapEditor
            // 
            this.ClientSize = new System.Drawing.Size(1393, 678);
            this.Controls.Add(this.tabMapEditor);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MapEditor";
            this.Text = "Map Editor";
            ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).EndInit();
            this.panelMapContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerTagID)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerValue1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editMarkerValue2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityTagID)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityTileIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityValue1Default)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.editEntityValue2Default)).EndInit();
            this.tabMarkers.ResumeLayout(false);
            this.tabMarkers.PerformLayout();
            this.tabEntities.ResumeLayout(false);
            this.tabEntities.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabEditor.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dimSlider)).EndInit();
            this.groupSize.ResumeLayout(false);
            this.groupSize.PerformLayout();
            this.groupMapExtraData.ResumeLayout(false);
            this.groupMapExtraData.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.tabMapEditor.ResumeLayout(false);
            this.tabTiles.ResumeLayout(false);
            this.tabTiles.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelCharColors)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureTileDisplay)).EndInit();
            this.tabCharset.ResumeLayout(false);
            this.tabExport.ResumeLayout(false);
            this.tabExport.PerformLayout();
            this.tabImport.ResumeLayout(false);
            this.tabImport.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fastPictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fastPictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem importCharsetToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem closeCharsetProjectToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem saveCharsetProjectToolStripMenuItem;
    private Krypton.Navigator.KryptonPage tabEditor;
    private System.Windows.Forms.Panel panelMapContainer;
    private GR.Forms.FastPictureBox pictureEditor;
    private Krypton.Navigator.KryptonNavigator tabMapEditor;
    private Krypton.Navigator.KryptonPage tabTiles;
    private System.Windows.Forms.Label labelTilesMulticolor2;
    private System.Windows.Forms.Label labelTilesMulticolor1;
    private System.Windows.Forms.Label label15;
    private GR.Forms.ImageListbox panelCharacters;
    private System.Windows.Forms.ComboBox comboTileMulticolor2;
    private System.Windows.Forms.ComboBox comboTileMulticolor1;
    private System.Windows.Forms.ComboBox comboTileBackground;
    private GR.Forms.FastPictureBox panelCharColors;
    private GR.Forms.FastPictureBox pictureTileDisplay;
    private System.Windows.Forms.TabPage tabPage1;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.Label label9;
    private GR.Forms.ImageListbox imageListbox1;
    private System.Windows.Forms.CheckBox checkBox1;
    private System.Windows.Forms.ComboBox comboBox1;
    private System.Windows.Forms.ComboBox comboBox2;
    private System.Windows.Forms.ComboBox comboBox3;
    private GR.Forms.FastPictureBox fastPictureBox1;
    private GR.Forms.FastPictureBox fastPictureBox2;
    private DecentForms.VScrollBar mapVScroll;
    private DecentForms.HScrollBar mapHScroll;
    private RetroDevStudio.Controls.CSListView listTileInfo;
    private System.Windows.Forms.TextBox editTileName;
    private System.Windows.Forms.TextBox editTileHeight;
    private System.Windows.Forms.TextBox editTileWidth;
    private System.Windows.Forms.CheckBox checkTilePassable;
    private System.Windows.Forms.CheckBox checkNotExportedOnMap;
    private System.Windows.Forms.Label label17;
    private System.Windows.Forms.Label label16;
    private System.Windows.Forms.Label label4;
    private DecentForms.Button btnTileDelete;
    private DecentForms.Button btnTileApply;
    private DecentForms.Button btnTileAdd;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.ColumnHeader columnHeader3;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private DecentForms.Button btnGetTileCount;
    private RetroDevStudio.Controls.CSListView listTileChars;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.ColumnHeader columnHeader6;
    private System.Windows.Forms.ColumnHeader columnHeader7;
    private System.Windows.Forms.ListBox comboTiles;
    private DecentForms.Button btnMoveTileDown;
    private DecentForms.Button btnMoveTileUp;
    private System.Windows.Forms.Label labelEditInfo;
    private DecentForms.RadioButton btnToolSelect;
    private DecentForms.RadioButton btnToolFill;
    private DecentForms.RadioButton btnToolQuad;
    private DecentForms.RadioButton btnToolRect;
    private DecentForms.RadioButton btnToolEdit;
    private System.Windows.Forms.Label labelTilesBGColor4;
    private System.Windows.Forms.ComboBox comboTileBGColor4;
    private DecentForms.Button btnCopyTileCharToNextIncreased;
        private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.CheckBox checkAutoTiling;
        private DecentForms.RadioButton btnToolMarker;
        private System.Windows.Forms.ComboBox comboMarkerTypes;
        private System.Windows.Forms.Label labelMarkerExportSymbol;
        private System.Windows.Forms.TextBox editMarkerExportSymbol;
        private System.Windows.Forms.Label labelMarkerValue1;
        private System.Windows.Forms.NumericUpDown editMarkerValue1;
        private System.Windows.Forms.Label labelMarkerValue2;
        private System.Windows.Forms.NumericUpDown editMarkerValue2;
        private System.Windows.Forms.CheckBox checkMarkerDefaultEnabled;
        private System.Windows.Forms.CheckBox checkMarkerDefaultTriggered;
        private System.Windows.Forms.ComboBox comboMarkerColorOverride;
        private DecentForms.RadioButton btnToolEntity;
        private System.Windows.Forms.ComboBox comboEntityTypes;
        private System.Windows.Forms.Label labelEntityValue1;
        private System.Windows.Forms.NumericUpDown editEntityValue1Default;
        private System.Windows.Forms.Label labelEntityValue2;
        private System.Windows.Forms.NumericUpDown editEntityValue2Default;
        private System.Windows.Forms.CheckBox checkEntityDefaultEnabled;
        private System.Windows.Forms.CheckBox checkShowEntities;
    private DecentForms.Button btnSetNextTileChar;
    private DecentForms.Button btnTileClone;
        private Krypton.Navigator.KryptonPage tabCharset;
    private Controls.CharacterEditor characterEditor;
    private DecentForms.Button btnCopyMapImage;
    private DecentForms.Button btnShiftLeft;
    private DecentForms.Button btnShiftUp;
    private DecentForms.Button btnShiftDown;
    private DecentForms.Button btnShiftRight;
    private DecentForms.Button btnRemoveOverlappingTiles;
    private DecentForms.Button btnZoomOut;
    private DecentForms.Button btnZoomIn;
    private System.Windows.Forms.Label labelZoom;
    private Krypton.Navigator.KryptonPage tabExport;
    private Krypton.Navigator.KryptonPage tabImport;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.ComboBox comboExportData;
    private DecentForms.Button btnExport;
    private System.Windows.Forms.ComboBox comboExportMethod;
    private System.Windows.Forms.Label label24;
    private System.Windows.Forms.TextBox editDataExport;
    private System.Windows.Forms.Panel panelExport;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.TextBox editSwatchSize;
    private System.Windows.Forms.Label labelSwatchSize;
    private System.Windows.Forms.ComboBox comboExportOrientation;
    private System.Windows.Forms.Panel panelImport;
    private DecentForms.Button btnImport;
    private System.Windows.Forms.ComboBox comboImportMethod;
    private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.TrackBar dimSlider;
        private System.Windows.Forms.GroupBox groupSize;
        private System.Windows.Forms.CheckBox checkShowGrid;
        private System.Windows.Forms.ComboBox comboMapAlternativeMode;
        private System.Windows.Forms.ComboBox comboMapAlternativeBGColor4;
        private System.Windows.Forms.ComboBox comboMapMultiColor2;
        private System.Windows.Forms.ComboBox comboMapBGColor;
        private System.Windows.Forms.ComboBox comboMapMultiColor1;
        private DecentForms.Button btnCopy;
        private DecentForms.Button btnMoveMapDown;
        private DecentForms.Button btnMoveMapUp;
        private DecentForms.Button btnMapAdd;
        private DecentForms.Button btnMapDelete;
        private DecentForms.Button btnMapApply;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox editMapName;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox editTileSpacingH;
        private System.Windows.Forms.TextBox editMapHeight;
        private System.Windows.Forms.TextBox editTileSpacingW;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox editMapWidth;
        private DecentForms.Button btnClearMarkerType;
        private System.Windows.Forms.ComboBox comboMaps;
        private DecentForms.Button btnClearMarkers;
        private System.Windows.Forms.ComboBox comboMapProjectMode;
        private System.Windows.Forms.GroupBox groupMapExtraData;
        private System.Windows.Forms.TextBox editMapExtraData;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ComboBox comboDesignerBackground;
        private System.Windows.Forms.Label labelRightClickBehavior;
        private System.Windows.Forms.Label labelDesignerBackground;
        private System.Windows.Forms.ComboBox comboRightClickBehavior;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem keepMapCharacterAspectRatioToolStripMenuItem;
        private Krypton.Navigator.KryptonPage tabMarkers;
        private Krypton.Navigator.KryptonPage tabEntities;
        private DecentForms.Button btnDeleteEntityType;
        private DecentForms.Button btnUpdateEntityType;
        private DecentForms.Button btnAddEntityType;
        private System.Windows.Forms.TextBox editEntityExportSymbol;
        private System.Windows.Forms.Label labelEntityExportSymbol;
        private System.Windows.Forms.NumericUpDown editEntityTagID;
        private System.Windows.Forms.NumericUpDown editEntityTileIndex;
        private System.Windows.Forms.Label labelEntityTileIndex;
        private System.Windows.Forms.TextBox editEntityName;
        private System.Windows.Forms.Label labelEntityName;
        private System.Windows.Forms.Label labelEntityTagID;
        private System.Windows.Forms.ListBox listEntityTypes;
        private System.Windows.Forms.ListBox listMarkerTypes;
        private System.Windows.Forms.Label labelMarkerName;
        private System.Windows.Forms.TextBox editMarkerName;
        private System.Windows.Forms.Label labelMarkerColor;
        private System.Windows.Forms.ComboBox comboMarkerColor;
        private DecentForms.Button btnAddMarkerType;
        private DecentForms.Button btnUpdateMarkerType;
        private DecentForms.Button btnDeleteMarkerType;
        private System.Windows.Forms.NumericUpDown editMarkerTagID;
        private System.Windows.Forms.Label labelMarkerTagID;
        private System.Windows.Forms.TextBox editTileGroupId;
        private System.Windows.Forms.Label labelTileGroupId;

    }
}
