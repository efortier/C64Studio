namespace RetroDevStudio.Dialogs
{
  partial class FormMapExtraData
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.editExtraData = new System.Windows.Forms.TextBox();
      this.btnCancel = new DecentForms.Button();
      this.btnOK = new DecentForms.Button();
      this.SuspendLayout();
      //
      // editExtraData
      //
      this.editExtraData.AcceptsReturn = true;
      this.editExtraData.AcceptsTab = true;
      this.editExtraData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.editExtraData.Font = new System.Drawing.Font( "Consolas", 10F );
      this.editExtraData.Location = new System.Drawing.Point( 12, 12 );
      this.editExtraData.Multiline = true;
      this.editExtraData.Name = "editExtraData";
      this.editExtraData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.editExtraData.Size = new System.Drawing.Size( 560, 320 );
      this.editExtraData.TabIndex = 0;
      this.editExtraData.WordWrap = true;
      //
      // btnCancel
      //
      this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnCancel.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnCancel.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnCancel.Image = null;
      this.btnCancel.Location = new System.Drawing.Point( 497, 348 );
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size( 75, 23 );
      this.btnCancel.TabIndex = 2;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.Click += new DecentForms.EventHandler( this.btnCancel_Click );
      //
      // btnOK
      //
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnOK.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOK.Image = null;
      this.btnOK.Location = new System.Drawing.Point( 416, 348 );
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size( 75, 23 );
      this.btnOK.TabIndex = 1;
      this.btnOK.Text = "OK";
      this.btnOK.Click += new DecentForms.EventHandler( this.btnOK_Click );
      //
      // FormMapExtraData
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.btnCancel;
      this.ClientSize = new System.Drawing.Size( 584, 383 );
      this.Controls.Add( this.btnOK );
      this.Controls.Add( this.btnCancel );
      this.Controls.Add( this.editExtraData );
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
      this.MinimumSize = new System.Drawing.Size( 320, 200 );
      this.Name = "FormMapExtraData";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Map extra data";
      this.ResumeLayout( false );
      this.PerformLayout();
    }

    #endregion
    private DecentForms.Button btnCancel;
    private DecentForms.Button btnOK;
    private System.Windows.Forms.TextBox editExtraData;
  }
}
