namespace RetroDevStudio.Dialogs
{
  partial class DlgDeleteTileEntities
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
      labelHeadline = new System.Windows.Forms.Label();
      editDetails = new System.Windows.Forms.TextBox();
      btnDeleteTileAndEntities = new DecentForms.Button();
      btnDeleteTileOnly = new DecentForms.Button();
      btnCancel = new DecentForms.Button();
      SuspendLayout();
      //
      // labelHeadline
      //
      labelHeadline.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      labelHeadline.Location = new System.Drawing.Point( 12, 9 );
      labelHeadline.Name = "labelHeadline";
      labelHeadline.Size = new System.Drawing.Size( 536, 30 );
      labelHeadline.TabIndex = 4;
      labelHeadline.Text = "There are entities on the map using this tile.";
      //
      // editDetails
      //
      editDetails.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      editDetails.Location = new System.Drawing.Point( 12, 42 );
      editDetails.Multiline = true;
      editDetails.Name = "editDetails";
      editDetails.ReadOnly = true;
      editDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      editDetails.Size = new System.Drawing.Size( 536, 205 );
      editDetails.TabIndex = 3;
      editDetails.WordWrap = false;
      //
      // btnDeleteTileAndEntities
      //
      btnDeleteTileAndEntities.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnDeleteTileAndEntities.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnDeleteTileAndEntities.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnDeleteTileAndEntities.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnDeleteTileAndEntities.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnDeleteTileAndEntities.Image = null;
      btnDeleteTileAndEntities.Location = new System.Drawing.Point( 172, 262 );
      btnDeleteTileAndEntities.Name = "btnDeleteTileAndEntities";
      btnDeleteTileAndEntities.Size = new System.Drawing.Size( 160, 23 );
      btnDeleteTileAndEntities.TabIndex = 0;
      btnDeleteTileAndEntities.Text = "Delete tile and entities";
      btnDeleteTileAndEntities.Click += btnDeleteTileAndEntities_Click;
      //
      // btnDeleteTileOnly
      //
      btnDeleteTileOnly.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnDeleteTileOnly.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnDeleteTileOnly.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnDeleteTileOnly.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnDeleteTileOnly.DialogResult = System.Windows.Forms.DialogResult.OK;
      btnDeleteTileOnly.Image = null;
      btnDeleteTileOnly.Location = new System.Drawing.Point( 340, 262 );
      btnDeleteTileOnly.Name = "btnDeleteTileOnly";
      btnDeleteTileOnly.Size = new System.Drawing.Size( 120, 23 );
      btnDeleteTileOnly.TabIndex = 1;
      btnDeleteTileOnly.Text = "Delete tile anyway";
      btnDeleteTileOnly.Click += btnDeleteTileOnly_Click;
      //
      // btnCancel
      //
      btnCancel.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnCancel.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnCancel.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnCancel.Image = null;
      btnCancel.Location = new System.Drawing.Point( 468, 262 );
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size( 80, 23 );
      btnCancel.TabIndex = 2;
      btnCancel.Text = "Cancel";
      btnCancel.Click += btnCancel_Click;
      //
      // DlgDeleteTileEntities
      //
      AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      CancelButton = btnCancel;
      ClientSize = new System.Drawing.Size( 560, 297 );
      Controls.Add( btnDeleteTileAndEntities );
      Controls.Add( btnDeleteTileOnly );
      Controls.Add( btnCancel );
      Controls.Add( editDetails );
      Controls.Add( labelHeadline );
      FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "DlgDeleteTileEntities";
      Padding = new System.Windows.Forms.Padding( 9 );
      ShowIcon = false;
      ShowInTaskbar = false;
      StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      Text = "Delete tile";
      ResumeLayout( false );
      PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label labelHeadline;
    private System.Windows.Forms.TextBox editDetails;
    private DecentForms.Button btnDeleteTileAndEntities;
    private DecentForms.Button btnDeleteTileOnly;
    private DecentForms.Button btnCancel;
  }
}
