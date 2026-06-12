namespace RetroDevStudio.Documents
{
  partial class StartPage
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
      this.panelRecent = new System.Windows.Forms.Panel();
      this.listRecentFiles = new System.Windows.Forms.ListBox();
      this.panelRecentButtons = new System.Windows.Forms.Panel();
      this.btnOpenFile = new DecentForms.Button();
      this.btnRemoveEntry = new DecentForms.Button();
      this.btnClearHistory = new DecentForms.Button();
      this.labelRecentHeader = new System.Windows.Forms.Label();
      this.contextRecent = new System.Windows.Forms.ContextMenuStrip(this.components);
      this.menuItemPinRecent = new System.Windows.Forms.ToolStripMenuItem();
      this.menuItemRemoveRecent = new System.Windows.Forms.ToolStripMenuItem();
      ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).BeginInit();
      this.panelRecent.SuspendLayout();
      this.panelRecentButtons.SuspendLayout();
      this.contextRecent.SuspendLayout();
      this.SuspendLayout();
      //
      // panelRecent
      //
      this.panelRecent.Controls.Add(this.listRecentFiles);
      this.panelRecent.Controls.Add(this.panelRecentButtons);
      this.panelRecent.Controls.Add(this.labelRecentHeader);
      this.panelRecent.Dock = System.Windows.Forms.DockStyle.Left;
      this.panelRecent.Location = new System.Drawing.Point(0, 0);
      this.panelRecent.Name = "panelRecent";
      this.panelRecent.Padding = new System.Windows.Forms.Padding(6, 6, 0, 6);
      this.panelRecent.Size = new System.Drawing.Size(430, 664);
      this.panelRecent.TabIndex = 0;
      //
      // listRecentFiles
      //
      this.listRecentFiles.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.listRecentFiles.ContextMenuStrip = this.contextRecent;
      this.listRecentFiles.Dock = System.Windows.Forms.DockStyle.Fill;
      this.listRecentFiles.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.listRecentFiles.IntegralHeight = false;
      this.listRecentFiles.ItemHeight = 40;
      this.listRecentFiles.Location = new System.Drawing.Point(6, 36);
      this.listRecentFiles.Name = "listRecentFiles";
      this.listRecentFiles.Size = new System.Drawing.Size(424, 588);
      this.listRecentFiles.TabIndex = 1;
      this.listRecentFiles.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listRecentFiles_DrawItem);
      this.listRecentFiles.SelectedIndexChanged += new System.EventHandler(this.listRecentFiles_SelectedIndexChanged);
      this.listRecentFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listRecentFiles_MouseDoubleClick);
      this.listRecentFiles.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listRecentFiles_MouseDown);
      //
      // panelRecentButtons
      //
      this.panelRecentButtons.Controls.Add(this.btnOpenFile);
      this.panelRecentButtons.Controls.Add(this.btnRemoveEntry);
      this.panelRecentButtons.Controls.Add(this.btnClearHistory);
      this.panelRecentButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.panelRecentButtons.Location = new System.Drawing.Point(6, 624);
      this.panelRecentButtons.Name = "panelRecentButtons";
      this.panelRecentButtons.Size = new System.Drawing.Size(424, 34);
      this.panelRecentButtons.TabIndex = 2;
      //
      // btnOpenFile
      //
      this.btnOpenFile.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      this.btnOpenFile.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnOpenFile.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnOpenFile.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOpenFile.Image = null;
      this.btnOpenFile.Location = new System.Drawing.Point(0, 6);
      this.btnOpenFile.Name = "btnOpenFile";
      this.btnOpenFile.Size = new System.Drawing.Size(110, 24);
      this.btnOpenFile.TabIndex = 0;
      this.btnOpenFile.Text = "Open File";
      this.btnOpenFile.Click += new DecentForms.EventHandler(this.btnOpenFile_Click);
      //
      // btnRemoveEntry
      //
      this.btnRemoveEntry.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      this.btnRemoveEntry.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnRemoveEntry.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnRemoveEntry.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnRemoveEntry.Image = null;
      this.btnRemoveEntry.Location = new System.Drawing.Point(116, 6);
      this.btnRemoveEntry.Name = "btnRemoveEntry";
      this.btnRemoveEntry.Size = new System.Drawing.Size(90, 24);
      this.btnRemoveEntry.TabIndex = 1;
      this.btnRemoveEntry.Text = "Remove Entry";
      this.btnRemoveEntry.Click += new DecentForms.EventHandler(this.btnRemoveEntry_Click);
      //
      // btnClearHistory
      //
      this.btnClearHistory.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      this.btnClearHistory.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnClearHistory.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnClearHistory.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnClearHistory.Image = null;
      this.btnClearHistory.Location = new System.Drawing.Point(212, 6);
      this.btnClearHistory.Name = "btnClearHistory";
      this.btnClearHistory.Size = new System.Drawing.Size(110, 24);
      this.btnClearHistory.TabIndex = 2;
      this.btnClearHistory.Text = "Clear History";
      this.btnClearHistory.Click += new DecentForms.EventHandler(this.btnClearHistory_Click);
      //
      // labelRecentHeader
      //
      this.labelRecentHeader.Dock = System.Windows.Forms.DockStyle.Top;
      this.labelRecentHeader.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
      this.labelRecentHeader.Location = new System.Drawing.Point(6, 6);
      this.labelRecentHeader.Name = "labelRecentHeader";
      this.labelRecentHeader.Size = new System.Drawing.Size(424, 30);
      this.labelRecentHeader.TabIndex = 0;
      this.labelRecentHeader.Text = "Recent Files";
      this.labelRecentHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      //
      // contextRecent
      //
      this.contextRecent.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemPinRecent,
            this.menuItemRemoveRecent});
      this.contextRecent.Name = "contextRecent";
      this.contextRecent.Size = new System.Drawing.Size(181, 48);
      this.contextRecent.Opening += new System.ComponentModel.CancelEventHandler(this.contextRecent_Opening);
      //
      // menuItemPinRecent
      //
      this.menuItemPinRecent.Name = "menuItemPinRecent";
      this.menuItemPinRecent.Size = new System.Drawing.Size(180, 22);
      this.menuItemPinRecent.Text = "Pin to top";
      this.menuItemPinRecent.Click += new System.EventHandler(this.menuItemPinRecent_Click);
      //
      // menuItemRemoveRecent
      //
      this.menuItemRemoveRecent.Name = "menuItemRemoveRecent";
      this.menuItemRemoveRecent.Size = new System.Drawing.Size(180, 22);
      this.menuItemRemoveRecent.Text = "Remove from list";
      this.menuItemRemoveRecent.Click += new System.EventHandler(this.menuItemRemoveRecent_Click);
      //
      // StartPage
      //
      this.ClientSize = new System.Drawing.Size(1004, 664);
      this.Controls.Add(this.panelRecent);
      this.Name = "StartPage";
      this.Text = "Start Page";
      ((System.ComponentModel.ISupportInitialize)(this.m_FileWatcher)).EndInit();
      this.panelRecent.ResumeLayout(false);
      this.panelRecentButtons.ResumeLayout(false);
      this.contextRecent.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel panelRecent;
    private System.Windows.Forms.Label labelRecentHeader;
    private System.Windows.Forms.ListBox listRecentFiles;
    private System.Windows.Forms.Panel panelRecentButtons;
    private DecentForms.Button btnOpenFile;
    private DecentForms.Button btnRemoveEntry;
    private DecentForms.Button btnClearHistory;
    private System.Windows.Forms.ContextMenuStrip contextRecent;
    private System.Windows.Forms.ToolStripMenuItem menuItemPinRecent;
    private System.Windows.Forms.ToolStripMenuItem menuItemRemoveRecent;
  }
}
