namespace RetroDevStudio.Dialogs
{
  partial class FormInputText
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
      this.labelPrompt = new System.Windows.Forms.Label();
      this.editInput = new System.Windows.Forms.TextBox();
      this.btnCancel = new DecentForms.Button();
      this.btnOK = new DecentForms.Button();
      this.SuspendLayout();
      //
      // labelPrompt
      //
      this.labelPrompt.AutoSize = true;
      this.labelPrompt.Location = new System.Drawing.Point( 12, 15 );
      this.labelPrompt.Name = "labelPrompt";
      this.labelPrompt.Size = new System.Drawing.Size( 43, 13 );
      this.labelPrompt.TabIndex = 1;
      this.labelPrompt.Text = "Prompt:";
      //
      // editInput
      //
      this.editInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
      this.editInput.Location = new System.Drawing.Point( 12, 35 );
      this.editInput.Name = "editInput";
      this.editInput.Size = new System.Drawing.Size( 260, 20 );
      this.editInput.TabIndex = 0;
      this.editInput.TextChanged += new System.EventHandler( this.editInput_TextChanged );
      //
      // btnCancel
      //
      this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.btnCancel.BorderStyle = DecentForms.BorderStyle.FLAT;
      this.btnCancel.ButtonBorder = DecentForms.Button.ButtonStyle.RAISED;
      this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnCancel.Image = null;
      this.btnCancel.Location = new System.Drawing.Point( 197, 72 );
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size( 75, 23 );
      this.btnCancel.TabIndex = 3;
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
      this.btnOK.Location = new System.Drawing.Point( 116, 72 );
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size( 75, 23 );
      this.btnOK.TabIndex = 2;
      this.btnOK.Text = "OK";
      this.btnOK.Click += new DecentForms.EventHandler( this.btnOK_Click );
      //
      // FormInputText
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = this.btnCancel;
      this.ClientSize = new System.Drawing.Size( 284, 107 );
      this.Controls.Add( this.btnOK );
      this.Controls.Add( this.btnCancel );
      this.Controls.Add( this.editInput );
      this.Controls.Add( this.labelPrompt );
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "FormInputText";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Input";
      this.ResumeLayout( false );
      this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.Label labelPrompt;
    private DecentForms.Button btnCancel;
    private DecentForms.Button btnOK;
    private System.Windows.Forms.TextBox editInput;
  }
}
