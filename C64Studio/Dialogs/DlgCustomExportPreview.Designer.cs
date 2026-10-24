namespace RetroDevStudio.Dialogs
{
  partial class DlgCustomExportPreview
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
      labelInfo = new System.Windows.Forms.Label();
      editOutput = new System.Windows.Forms.TextBox();
      btnSave = new DecentForms.Button();
      btnCopy = new DecentForms.Button();
      btnClose = new DecentForms.Button();
      SuspendLayout();
      //
      // labelInfo
      //
      labelInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      labelInfo.Location = new System.Drawing.Point( 12, 9 );
      labelInfo.Name = "labelInfo";
      labelInfo.Size = new System.Drawing.Size( 860, 44 );
      labelInfo.TabIndex = 0;
      labelInfo.Text = "Preview";
      //
      // editOutput
      //
      editOutput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
      editOutput.Font = new System.Drawing.Font( "Consolas", 10F );
      editOutput.Location = new System.Drawing.Point( 12, 56 );
      editOutput.MaxLength = 4000000;
      editOutput.Multiline = true;
      editOutput.Name = "editOutput";
      editOutput.ReadOnly = true;
      editOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      editOutput.Size = new System.Drawing.Size( 860, 460 );
      editOutput.TabIndex = 1;
      editOutput.WordWrap = false;
      editOutput.DoubleClick += editOutput_DoubleClick;
      //
      // btnSave
      //
      btnSave.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnSave.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnSave.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnSave.Image = null;
      btnSave.Location = new System.Drawing.Point( 616, 528 );
      btnSave.Name = "btnSave";
      btnSave.Size = new System.Drawing.Size( 80, 23 );
      btnSave.TabIndex = 2;
      btnSave.Text = "Save";
      btnSave.Click += btnSave_Click;
      //
      // btnCopy
      //
      btnCopy.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnCopy.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnCopy.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnCopy.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnCopy.Image = null;
      btnCopy.Location = new System.Drawing.Point( 704, 528 );
      btnCopy.Name = "btnCopy";
      btnCopy.Size = new System.Drawing.Size( 80, 23 );
      btnCopy.TabIndex = 3;
      btnCopy.Text = "Copy";
      btnCopy.Click += btnCopy_Click;
      //
      // btnClose
      //
      btnClose.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
      btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
      btnClose.BorderStyle = DecentForms.BorderStyle.FLAT;
      btnClose.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      btnClose.Image = null;
      btnClose.Location = new System.Drawing.Point( 792, 528 );
      btnClose.Name = "btnClose";
      btnClose.Size = new System.Drawing.Size( 80, 23 );
      btnClose.TabIndex = 4;
      btnClose.Text = "Close";
      btnClose.Click += btnClose_Click;
      //
      // DlgCustomExportPreview
      //
      AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      CancelButton = btnClose;
      ClientSize = new System.Drawing.Size( 884, 563 );
      Controls.Add( btnClose );
      Controls.Add( btnCopy );
      Controls.Add( btnSave );
      Controls.Add( editOutput );
      Controls.Add( labelInfo );
      MinimizeBox = false;
      MinimumSize = new System.Drawing.Size( 500, 300 );
      Name = "DlgCustomExportPreview";
      ShowIcon = false;
      ShowInTaskbar = false;
      StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      Text = "Custom export preview";
      ResumeLayout( false );
      PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label labelInfo;
    private System.Windows.Forms.TextBox editOutput;
    private DecentForms.Button btnSave;
    private DecentForms.Button btnCopy;
    private DecentForms.Button btnClose;
  }
}
