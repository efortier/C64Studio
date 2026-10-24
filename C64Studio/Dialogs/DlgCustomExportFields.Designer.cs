namespace RetroDevStudio.Dialogs
{
  partial class DlgCustomExportFields
  {
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
      labelSyntax = new System.Windows.Forms.Label();
      editSyntax = new System.Windows.Forms.TextBox();
      labelFields = new System.Windows.Forms.Label();
      treeFields = new System.Windows.Forms.TreeView();
      labelDescription = new System.Windows.Forms.Label();
      btnInsert = new DecentForms.Button();
      btnClose = new DecentForms.Button();
      SuspendLayout();
      //
      // labelSyntax
      //
      labelSyntax.AutoSize = true;
      labelSyntax.Location = new System.Drawing.Point( 12, 9 );
      labelSyntax.Name = "labelSyntax";
      labelSyntax.Size = new System.Drawing.Size( 44, 13 );
      labelSyntax.TabIndex = 0;
      labelSyntax.Text = "Syntax:";
      //
      // editSyntax
      //
      editSyntax.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      editSyntax.Font = new System.Drawing.Font( "Consolas", 9F );
      editSyntax.Location = new System.Drawing.Point( 12, 25 );
      editSyntax.Multiline = true;
      editSyntax.Name = "editSyntax";
      editSyntax.ReadOnly = true;
      editSyntax.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      editSyntax.Size = new System.Drawing.Size( 760, 190 );
      editSyntax.TabIndex = 1;
      editSyntax.WordWrap = false;
      //
      // labelFields
      //
      labelFields.AutoSize = true;
      labelFields.Location = new System.Drawing.Point( 12, 224 );
      labelFields.Name = "labelFields";
      labelFields.Size = new System.Drawing.Size( 240, 13 );
      labelFields.TabIndex = 2;
      labelFields.Text = "Fields (double-click or Insert puts one into the template):";
      //
      // treeFields
      //
      treeFields.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      treeFields.HideSelection = false;
      treeFields.Location = new System.Drawing.Point( 12, 240 );
      treeFields.Name = "treeFields";
      treeFields.Size = new System.Drawing.Size( 760, 360 );
      treeFields.TabIndex = 3;
      treeFields.AfterSelect += treeFields_AfterSelect;
      treeFields.NodeMouseDoubleClick += treeFields_NodeMouseDoubleClick;
      //
      // labelDescription
      //
      labelDescription.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      labelDescription.Location = new System.Drawing.Point( 12, 606 );
      labelDescription.Name = "labelDescription";
      labelDescription.Size = new System.Drawing.Size( 760, 44 );
      labelDescription.TabIndex = 4;
      labelDescription.Text = "";
      //
      // btnInsert
      //
      btnInsert.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnInsert.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnInsert.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnInsert.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnInsert.Image = null;
      btnInsert.Location = new System.Drawing.Point( 604, 656 );
      btnInsert.Name = "btnInsert";
      btnInsert.Size = new System.Drawing.Size( 80, 23 );
      btnInsert.TabIndex = 5;
      btnInsert.Text = "Insert";
      btnInsert.Click += btnInsert_Click;
      //
      // btnClose
      //
      btnClose.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnClose.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnClose.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnClose.Image = null;
      btnClose.Location = new System.Drawing.Point( 692, 656 );
      btnClose.Name = "btnClose";
      btnClose.Size = new System.Drawing.Size( 80, 23 );
      btnClose.TabIndex = 6;
      btnClose.Text = "Close";
      btnClose.Click += btnClose_Click;
      //
      // DlgCustomExportFields
      //
      AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      ClientSize = new System.Drawing.Size( 784, 691 );
      Controls.Add( btnClose );
      Controls.Add( btnInsert );
      Controls.Add( labelDescription );
      Controls.Add( treeFields );
      Controls.Add( labelFields );
      Controls.Add( editSyntax );
      Controls.Add( labelSyntax );
      MinimizeBox = false;
      MinimumSize = new System.Drawing.Size( 500, 400 );
      Name = "DlgCustomExportFields";
      ShowIcon = false;
      ShowInTaskbar = false;
      StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      Text = "Custom export fields";
      ResumeLayout( false );
      PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label labelSyntax;
    private System.Windows.Forms.TextBox editSyntax;
    private System.Windows.Forms.Label labelFields;
    private System.Windows.Forms.TreeView treeFields;
    private System.Windows.Forms.Label labelDescription;
    private DecentForms.Button btnInsert;
    private DecentForms.Button btnClose;
  }
}
