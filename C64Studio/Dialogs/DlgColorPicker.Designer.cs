namespace RetroDevStudio.Dialogs
{
  partial class DlgColorPicker
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
      if ( disposing )
      {
        DisposeWheelBitmap();
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
      panelColorWheel = new System.Windows.Forms.Panel();
      panelPreviewOld = new System.Windows.Forms.Panel();
      panelPreviewNew = new System.Windows.Forms.Panel();
      labelR = new System.Windows.Forms.Label();
      trackR = new Krypton.Toolkit.KryptonTrackBar();
      editR = new System.Windows.Forms.NumericUpDown();
      labelG = new System.Windows.Forms.Label();
      trackG = new Krypton.Toolkit.KryptonTrackBar();
      editG = new System.Windows.Forms.NumericUpDown();
      labelB = new System.Windows.Forms.Label();
      trackB = new Krypton.Toolkit.KryptonTrackBar();
      editB = new System.Windows.Forms.NumericUpDown();
      labelH = new System.Windows.Forms.Label();
      trackH = new Krypton.Toolkit.KryptonTrackBar();
      editH = new System.Windows.Forms.NumericUpDown();
      labelS = new System.Windows.Forms.Label();
      trackS = new Krypton.Toolkit.KryptonTrackBar();
      editS = new System.Windows.Forms.NumericUpDown();
      labelV = new System.Windows.Forms.Label();
      trackV = new Krypton.Toolkit.KryptonTrackBar();
      editV = new System.Windows.Forms.NumericUpDown();
      labelA = new System.Windows.Forms.Label();
      trackA = new Krypton.Toolkit.KryptonTrackBar();
      editA = new System.Windows.Forms.NumericUpDown();
      labelHex = new System.Windows.Forms.Label();
      editHexColor = new System.Windows.Forms.TextBox();
      flowPickerSwatches = new System.Windows.Forms.FlowLayoutPanel();
      btnOK = new DecentForms.Button();
      btnCancel = new DecentForms.Button();
      ((System.ComponentModel.ISupportInitialize)editR).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editG).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editB).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editH).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editS).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editV).BeginInit();
      ((System.ComponentModel.ISupportInitialize)editA).BeginInit();
      SuspendLayout();
      //
      // panelColorWheel
      //
      panelColorWheel.Location = new System.Drawing.Point(12, 12);
      panelColorWheel.Name = "panelColorWheel";
      panelColorWheel.Size = new System.Drawing.Size(216, 216);
      panelColorWheel.TabIndex = 0;
      panelColorWheel.Paint += panelColorWheel_Paint;
      panelColorWheel.MouseDown += panelColorWheel_MouseDown;
      panelColorWheel.MouseMove += panelColorWheel_MouseMove;
      //
      // panelPreviewOld
      //
      panelPreviewOld.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      panelPreviewOld.Location = new System.Drawing.Point(12, 240);
      panelPreviewOld.Name = "panelPreviewOld";
      panelPreviewOld.Size = new System.Drawing.Size(105, 28);
      panelPreviewOld.TabIndex = 1;
      panelPreviewOld.Paint += panelPreviewOld_Paint;
      panelPreviewOld.Click += panelPreviewOld_Click;
      //
      // panelPreviewNew
      //
      panelPreviewNew.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      panelPreviewNew.Location = new System.Drawing.Point(117, 240);
      panelPreviewNew.Name = "panelPreviewNew";
      panelPreviewNew.Size = new System.Drawing.Size(105, 28);
      panelPreviewNew.TabIndex = 2;
      panelPreviewNew.Paint += panelPreviewNew_Paint;
      //
      // labelR
      //
      labelR.Location = new System.Drawing.Point(244, 16);
      labelR.Name = "labelR";
      labelR.Size = new System.Drawing.Size(18, 15);
      labelR.TabIndex = 3;
      labelR.Text = "R";
      //
      // trackR
      //
      trackR.Location = new System.Drawing.Point(264, 12);
      trackR.Maximum = 255;
      trackR.Name = "trackR";
      trackR.Size = new System.Drawing.Size(150, 27);
      trackR.TabIndex = 4;
      trackR.ValueChanged += channel_ValueChanged;
      //
      // editR
      //
      editR.Location = new System.Drawing.Point(422, 14);
      editR.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      editR.Name = "editR";
      editR.Size = new System.Drawing.Size(56, 20);
      editR.TabIndex = 5;
      editR.ValueChanged += channel_ValueChanged;
      //
      // labelG
      //
      labelG.Location = new System.Drawing.Point(244, 48);
      labelG.Name = "labelG";
      labelG.Size = new System.Drawing.Size(18, 15);
      labelG.TabIndex = 6;
      labelG.Text = "G";
      //
      // trackG
      //
      trackG.Location = new System.Drawing.Point(264, 44);
      trackG.Maximum = 255;
      trackG.Name = "trackG";
      trackG.Size = new System.Drawing.Size(150, 27);
      trackG.TabIndex = 7;
      trackG.ValueChanged += channel_ValueChanged;
      //
      // editG
      //
      editG.Location = new System.Drawing.Point(422, 46);
      editG.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      editG.Name = "editG";
      editG.Size = new System.Drawing.Size(56, 20);
      editG.TabIndex = 8;
      editG.ValueChanged += channel_ValueChanged;
      //
      // labelB
      //
      labelB.Location = new System.Drawing.Point(244, 80);
      labelB.Name = "labelB";
      labelB.Size = new System.Drawing.Size(18, 15);
      labelB.TabIndex = 9;
      labelB.Text = "B";
      //
      // trackB
      //
      trackB.Location = new System.Drawing.Point(264, 76);
      trackB.Maximum = 255;
      trackB.Name = "trackB";
      trackB.Size = new System.Drawing.Size(150, 27);
      trackB.TabIndex = 10;
      trackB.ValueChanged += channel_ValueChanged;
      //
      // editB
      //
      editB.Location = new System.Drawing.Point(422, 78);
      editB.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      editB.Name = "editB";
      editB.Size = new System.Drawing.Size(56, 20);
      editB.TabIndex = 11;
      editB.ValueChanged += channel_ValueChanged;
      //
      // labelH
      //
      labelH.Location = new System.Drawing.Point(244, 120);
      labelH.Name = "labelH";
      labelH.Size = new System.Drawing.Size(18, 15);
      labelH.TabIndex = 12;
      labelH.Text = "H";
      //
      // trackH
      //
      trackH.Location = new System.Drawing.Point(264, 116);
      trackH.Maximum = 359;
      trackH.Name = "trackH";
      trackH.Size = new System.Drawing.Size(150, 27);
      trackH.TabIndex = 13;
      trackH.ValueChanged += hsv_ValueChanged;
      //
      // editH
      //
      editH.Location = new System.Drawing.Point(422, 118);
      editH.Maximum = new decimal(new int[] { 359, 0, 0, 0 });
      editH.Name = "editH";
      editH.Size = new System.Drawing.Size(56, 20);
      editH.TabIndex = 14;
      editH.ValueChanged += hsv_ValueChanged;
      //
      // labelS
      //
      labelS.Location = new System.Drawing.Point(244, 152);
      labelS.Name = "labelS";
      labelS.Size = new System.Drawing.Size(18, 15);
      labelS.TabIndex = 15;
      labelS.Text = "S";
      //
      // trackS
      //
      trackS.Location = new System.Drawing.Point(264, 148);
      trackS.Maximum = 100;
      trackS.Name = "trackS";
      trackS.Size = new System.Drawing.Size(150, 27);
      trackS.TabIndex = 16;
      trackS.ValueChanged += hsv_ValueChanged;
      //
      // editS
      //
      editS.Location = new System.Drawing.Point(422, 150);
      editS.Name = "editS";
      editS.Size = new System.Drawing.Size(56, 20);
      editS.TabIndex = 17;
      editS.ValueChanged += hsv_ValueChanged;
      //
      // labelV
      //
      labelV.Location = new System.Drawing.Point(244, 184);
      labelV.Name = "labelV";
      labelV.Size = new System.Drawing.Size(18, 15);
      labelV.TabIndex = 18;
      labelV.Text = "V";
      //
      // trackV
      //
      trackV.Location = new System.Drawing.Point(264, 180);
      trackV.Maximum = 100;
      trackV.Name = "trackV";
      trackV.Size = new System.Drawing.Size(150, 27);
      trackV.TabIndex = 19;
      trackV.ValueChanged += hsv_ValueChanged;
      //
      // editV
      //
      editV.Location = new System.Drawing.Point(422, 182);
      editV.Name = "editV";
      editV.Size = new System.Drawing.Size(56, 20);
      editV.TabIndex = 20;
      editV.ValueChanged += hsv_ValueChanged;
      //
      // labelA
      //
      labelA.Location = new System.Drawing.Point(244, 224);
      labelA.Name = "labelA";
      labelA.Size = new System.Drawing.Size(56, 15);
      labelA.TabIndex = 21;
      labelA.Text = "Opacity";
      //
      // trackA
      //
      trackA.Location = new System.Drawing.Point(302, 220);
      trackA.Maximum = 255;
      trackA.Name = "trackA";
      trackA.Size = new System.Drawing.Size(112, 27);
      trackA.TabIndex = 22;
      trackA.Value = 255;
      trackA.ValueChanged += alpha_ValueChanged;
      //
      // editA
      //
      editA.Location = new System.Drawing.Point(422, 222);
      editA.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      editA.Name = "editA";
      editA.Size = new System.Drawing.Size(56, 20);
      editA.TabIndex = 23;
      editA.Value = new decimal(new int[] { 255, 0, 0, 0 });
      editA.ValueChanged += alpha_ValueChanged;
      //
      // labelHex
      //
      labelHex.Location = new System.Drawing.Point(244, 258);
      labelHex.Name = "labelHex";
      labelHex.Size = new System.Drawing.Size(34, 15);
      labelHex.TabIndex = 24;
      labelHex.Text = "Hex:";
      //
      // editHexColor
      //
      editHexColor.Location = new System.Drawing.Point(284, 254);
      editHexColor.Name = "editHexColor";
      editHexColor.Size = new System.Drawing.Size(110, 20);
      editHexColor.TabIndex = 25;
      editHexColor.TextChanged += editHexColor_TextChanged;
      editHexColor.Leave += editHexColor_Leave;
      //
      // flowPickerSwatches
      //
      flowPickerSwatches.Location = new System.Drawing.Point(12, 284);
      flowPickerSwatches.Name = "flowPickerSwatches";
      flowPickerSwatches.Size = new System.Drawing.Size(476, 80);
      flowPickerSwatches.TabIndex = 26;
      //
      // btnOK
      //
      btnOK.Location = new System.Drawing.Point(322, 372);
      btnOK.Name = "btnOK";
      btnOK.Size = new System.Drawing.Size(80, 28);
      btnOK.TabIndex = 27;
      btnOK.Text = "OK";
      btnOK.Click += new DecentForms.EventHandler(btnOK_Click);
      //
      // btnCancel
      //
      btnCancel.Location = new System.Drawing.Point(408, 372);
      btnCancel.Name = "btnCancel";
      btnCancel.Size = new System.Drawing.Size(80, 28);
      btnCancel.TabIndex = 28;
      btnCancel.Text = "Cancel";
      btnCancel.Click += new DecentForms.EventHandler(btnCancel_Click);
      //
      // DlgColorPicker
      //
      AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      ClientSize = new System.Drawing.Size(500, 412);
      Controls.Add(panelColorWheel);
      Controls.Add(panelPreviewOld);
      Controls.Add(panelPreviewNew);
      Controls.Add(labelR);
      Controls.Add(trackR);
      Controls.Add(editR);
      Controls.Add(labelG);
      Controls.Add(trackG);
      Controls.Add(editG);
      Controls.Add(labelB);
      Controls.Add(trackB);
      Controls.Add(editB);
      Controls.Add(labelH);
      Controls.Add(trackH);
      Controls.Add(editH);
      Controls.Add(labelS);
      Controls.Add(trackS);
      Controls.Add(editS);
      Controls.Add(labelV);
      Controls.Add(trackV);
      Controls.Add(editV);
      Controls.Add(labelA);
      Controls.Add(trackA);
      Controls.Add(editA);
      Controls.Add(labelHex);
      Controls.Add(editHexColor);
      Controls.Add(flowPickerSwatches);
      Controls.Add(btnOK);
      Controls.Add(btnCancel);
      FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      Name = "DlgColorPicker";
      ShowInTaskbar = false;
      StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      Text = "Colors";
      ((System.ComponentModel.ISupportInitialize)editR).EndInit();
      ((System.ComponentModel.ISupportInitialize)editG).EndInit();
      ((System.ComponentModel.ISupportInitialize)editB).EndInit();
      ((System.ComponentModel.ISupportInitialize)editH).EndInit();
      ((System.ComponentModel.ISupportInitialize)editS).EndInit();
      ((System.ComponentModel.ISupportInitialize)editV).EndInit();
      ((System.ComponentModel.ISupportInitialize)editA).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Panel panelColorWheel;
    private System.Windows.Forms.Panel panelPreviewOld;
    private System.Windows.Forms.Panel panelPreviewNew;
    private System.Windows.Forms.Label labelR;
    private Krypton.Toolkit.KryptonTrackBar trackR;
    private System.Windows.Forms.NumericUpDown editR;
    private System.Windows.Forms.Label labelG;
    private Krypton.Toolkit.KryptonTrackBar trackG;
    private System.Windows.Forms.NumericUpDown editG;
    private System.Windows.Forms.Label labelB;
    private Krypton.Toolkit.KryptonTrackBar trackB;
    private System.Windows.Forms.NumericUpDown editB;
    private System.Windows.Forms.Label labelH;
    private Krypton.Toolkit.KryptonTrackBar trackH;
    private System.Windows.Forms.NumericUpDown editH;
    private System.Windows.Forms.Label labelS;
    private Krypton.Toolkit.KryptonTrackBar trackS;
    private System.Windows.Forms.NumericUpDown editS;
    private System.Windows.Forms.Label labelV;
    private Krypton.Toolkit.KryptonTrackBar trackV;
    private System.Windows.Forms.NumericUpDown editV;
    private System.Windows.Forms.Label labelA;
    private Krypton.Toolkit.KryptonTrackBar trackA;
    private System.Windows.Forms.NumericUpDown editA;
    private System.Windows.Forms.Label labelHex;
    private System.Windows.Forms.TextBox editHexColor;
    private System.Windows.Forms.FlowLayoutPanel flowPickerSwatches;
    private DecentForms.Button btnOK;
    private DecentForms.Button btnCancel;
  }
}
