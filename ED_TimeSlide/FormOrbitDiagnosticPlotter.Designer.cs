namespace ED_TimeSlide
{
    partial class FormOrbitDiagnosticPlotter
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer Generated Code
        private void InitializeComponent()
        {
            this.cmbPlanetSelector = new System.Windows.Forms.ComboBox();
            this.btnPreviousPlanet = new System.Windows.Forms.Button();
            this.btnNextPlanet = new System.Windows.Forms.Button();
            this.lblPlotterStatus = new System.Windows.Forms.Label();
            this.formsPlotCanvas = new ScottPlot.WinForms.FormsPlot();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdoZoomYOnly = new System.Windows.Forms.RadioButton();
            this.rdoZoomXOnly = new System.Windows.Forms.RadioButton();
            this.rdoZoomStandard = new System.Windows.Forms.RadioButton();
            this.BtnUpdateRender = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.nud_Max_DataFilter = new System.Windows.Forms.NumericUpDown();
            this.nud_Min_DataFilter = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chb_SystemNameFilter = new System.Windows.Forms.CheckBox();
            this.txb_SystemNameFilter = new System.Windows.Forms.TextBox();
            this.but_UpdateComboBox = new System.Windows.Forms.Button();
            this.txb_BodyInfo = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Max_DataFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Min_DataFilter)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbPlanetSelector
            // 
            this.cmbPlanetSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlanetSelector.FormattingEnabled = true;
            this.cmbPlanetSelector.Location = new System.Drawing.Point(6, 15);
            this.cmbPlanetSelector.Name = "cmbPlanetSelector";
            this.cmbPlanetSelector.Size = new System.Drawing.Size(300, 21);
            this.cmbPlanetSelector.TabIndex = 1;
            this.cmbPlanetSelector.SelectedIndexChanged += new System.EventHandler(this.CmbPlanetSelector_SelectedIndexChanged);
            // 
            // btnPreviousPlanet
            // 
            this.btnPreviousPlanet.Location = new System.Drawing.Point(970, 12);
            this.btnPreviousPlanet.Name = "btnPreviousPlanet";
            this.btnPreviousPlanet.Size = new System.Drawing.Size(105, 40);
            this.btnPreviousPlanet.TabIndex = 7;
            this.btnPreviousPlanet.Text = "◀ Previous Planet";
            this.btnPreviousPlanet.UseVisualStyleBackColor = true;
            this.btnPreviousPlanet.Click += new System.EventHandler(this.BtnPreviousPlanet_Click);
            // 
            // btnNextPlanet
            // 
            this.btnNextPlanet.Location = new System.Drawing.Point(1081, 12);
            this.btnNextPlanet.Name = "btnNextPlanet";
            this.btnNextPlanet.Size = new System.Drawing.Size(105, 40);
            this.btnNextPlanet.TabIndex = 8;
            this.btnNextPlanet.Text = "Next Planet ▶";
            this.btnNextPlanet.UseVisualStyleBackColor = true;
            this.btnNextPlanet.Click += new System.EventHandler(this.BtnNextPlanet_Click);
            // 
            // lblPlotterStatus
            // 
            this.lblPlotterStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblPlotterStatus.AutoSize = true;
            this.lblPlotterStatus.Location = new System.Drawing.Point(12, 532);
            this.lblPlotterStatus.Name = "lblPlotterStatus";
            this.lblPlotterStatus.Size = new System.Drawing.Size(56, 13);
            this.lblPlotterStatus.TabIndex = 0;
            this.lblPlotterStatus.Text = "[STATUS]";
            // 
            // formsPlotCanvas
            // 
            this.formsPlotCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.formsPlotCanvas.Location = new System.Drawing.Point(12, 58);
            this.formsPlotCanvas.Name = "formsPlotCanvas";
            this.formsPlotCanvas.Size = new System.Drawing.Size(1492, 435);
            this.formsPlotCanvas.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdoZoomYOnly);
            this.groupBox1.Controls.Add(this.rdoZoomXOnly);
            this.groupBox1.Controls.Add(this.rdoZoomStandard);
            this.groupBox1.Location = new System.Drawing.Point(1192, 7);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(203, 45);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Zoom Controls";
            // 
            // rdoZoomYOnly
            // 
            this.rdoZoomYOnly.AutoSize = true;
            this.rdoZoomYOnly.Location = new System.Drawing.Point(140, 20);
            this.rdoZoomYOnly.Name = "rdoZoomYOnly";
            this.rdoZoomYOnly.Size = new System.Drawing.Size(53, 17);
            this.rdoZoomYOnly.TabIndex = 11;
            this.rdoZoomYOnly.TabStop = true;
            this.rdoZoomYOnly.Text = "Y axis";
            this.rdoZoomYOnly.UseVisualStyleBackColor = true;
            this.rdoZoomYOnly.CheckedChanged += new System.EventHandler(this.OnZoomModeChanged);
            // 
            // rdoZoomXOnly
            // 
            this.rdoZoomXOnly.AutoSize = true;
            this.rdoZoomXOnly.Location = new System.Drawing.Point(81, 20);
            this.rdoZoomXOnly.Name = "rdoZoomXOnly";
            this.rdoZoomXOnly.Size = new System.Drawing.Size(53, 17);
            this.rdoZoomXOnly.TabIndex = 10;
            this.rdoZoomXOnly.TabStop = true;
            this.rdoZoomXOnly.Text = "X axis";
            this.rdoZoomXOnly.UseVisualStyleBackColor = true;
            this.rdoZoomXOnly.CheckedChanged += new System.EventHandler(this.OnZoomModeChanged);
            // 
            // rdoZoomStandard
            // 
            this.rdoZoomStandard.AutoSize = true;
            this.rdoZoomStandard.Location = new System.Drawing.Point(7, 20);
            this.rdoZoomStandard.Name = "rdoZoomStandard";
            this.rdoZoomStandard.Size = new System.Drawing.Size(68, 17);
            this.rdoZoomStandard.TabIndex = 9;
            this.rdoZoomStandard.TabStop = true;
            this.rdoZoomStandard.Text = "Standard";
            this.rdoZoomStandard.UseVisualStyleBackColor = true;
            this.rdoZoomStandard.CheckedChanged += new System.EventHandler(this.OnZoomModeChanged);
            // 
            // BtnUpdateRender
            // 
            this.BtnUpdateRender.Location = new System.Drawing.Point(1401, 12);
            this.BtnUpdateRender.Name = "BtnUpdateRender";
            this.BtnUpdateRender.Size = new System.Drawing.Size(104, 40);
            this.BtnUpdateRender.TabIndex = 12;
            this.BtnUpdateRender.Text = "Update Render";
            this.BtnUpdateRender.UseVisualStyleBackColor = true;
            this.BtnUpdateRender.Click += new System.EventHandler(this.BtnUpdateRenderCanvas_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txb_SystemNameFilter);
            this.groupBox2.Controls.Add(this.chb_SystemNameFilter);
            this.groupBox2.Controls.Add(this.but_UpdateComboBox);
            this.groupBox2.Controls.Add(this.nud_Max_DataFilter);
            this.groupBox2.Controls.Add(this.nud_Min_DataFilter);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(330, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(634, 46);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Data Filter";
            // 
            // nud_Max_DataFilter
            // 
            this.nud_Max_DataFilter.Location = new System.Drawing.Point(132, 19);
            this.nud_Max_DataFilter.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nud_Max_DataFilter.Name = "nud_Max_DataFilter";
            this.nud_Max_DataFilter.Size = new System.Drawing.Size(57, 20);
            this.nud_Max_DataFilter.TabIndex = 3;
            this.nud_Max_DataFilter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_Max_DataFilter.Value = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            // 
            // nud_Min_DataFilter
            // 
            this.nud_Min_DataFilter.Location = new System.Drawing.Point(36, 19);
            this.nud_Min_DataFilter.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nud_Min_DataFilter.Name = "nud_Min_DataFilter";
            this.nud_Min_DataFilter.Size = new System.Drawing.Size(57, 20);
            this.nud_Min_DataFilter.TabIndex = 2;
            this.nud_Min_DataFilter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(99, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(27, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Max";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Min";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cmbPlanetSelector);
            this.groupBox3.Location = new System.Drawing.Point(12, 9);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(312, 43);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "System - Body to plot";
            // 
            // chb_SystemNameFilter
            // 
            this.chb_SystemNameFilter.AutoSize = true;
            this.chb_SystemNameFilter.Location = new System.Drawing.Point(195, 20);
            this.chb_SystemNameFilter.Name = "chb_SystemNameFilter";
            this.chb_SystemNameFilter.Size = new System.Drawing.Size(116, 17);
            this.chb_SystemNameFilter.TabIndex = 4;
            this.chb_SystemNameFilter.Text = "System Name Filter";
            this.chb_SystemNameFilter.UseVisualStyleBackColor = true;
            // 
            // txb_SystemNameFIlter
            // 
            this.txb_SystemNameFilter.Location = new System.Drawing.Point(317, 18);
            this.txb_SystemNameFilter.Name = "txb_SystemNameFIlter";
            this.txb_SystemNameFilter.Size = new System.Drawing.Size(200, 20);
            this.txb_SystemNameFilter.TabIndex = 5;
            // 
            // but_UpdateComboBox
            // 
            this.but_UpdateComboBox.Location = new System.Drawing.Point(523, 13);
            this.but_UpdateComboBox.Name = "but_UpdateComboBox";
            this.but_UpdateComboBox.Size = new System.Drawing.Size(104, 25);
            this.but_UpdateComboBox.TabIndex = 6;
            this.but_UpdateComboBox.Text = "Update Combobox";
            this.but_UpdateComboBox.UseVisualStyleBackColor = true;
            this.but_UpdateComboBox.Click += new System.EventHandler(this.but_UpdateComboBox_Click);
            // 
            // txb_BodyInfo
            // 
            this.txb_BodyInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_BodyInfo.Location = new System.Drawing.Point(12, 499);
            this.txb_BodyInfo.Name = "txb_BodyInfo";
            this.txb_BodyInfo.ReadOnly = true;
            this.txb_BodyInfo.Size = new System.Drawing.Size(1492, 20);
            this.txb_BodyInfo.TabIndex = 0;
            this.txb_BodyInfo.TabStop = false;
            // 
            // FormOrbitDiagnosticPlotter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1516, 561);
            this.Controls.Add(this.txb_BodyInfo);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.BtnUpdateRender);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblPlotterStatus);
            this.Controls.Add(this.formsPlotCanvas);
            this.Controls.Add(this.btnNextPlanet);
            this.Controls.Add(this.btnPreviousPlanet);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "FormOrbitDiagnosticPlotter";
            this.Text = "Keplerian Rail Diagnostic Plotter Deck";
            this.Load += new System.EventHandler(this.FormOrbitDiagnosticPlotter_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Max_DataFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Min_DataFilter)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region Control Variable Handles Definitions
        private System.Windows.Forms.ComboBox cmbPlanetSelector;
        private System.Windows.Forms.Button btnPreviousPlanet;
        private System.Windows.Forms.Button btnNextPlanet;
        private System.Windows.Forms.Label lblPlotterStatus;
        private ScottPlot.WinForms.FormsPlot formsPlotCanvas;
        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdoZoomYOnly;
        private System.Windows.Forms.RadioButton rdoZoomXOnly;
        private System.Windows.Forms.RadioButton rdoZoomStandard;
        private System.Windows.Forms.Button BtnUpdateRender;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown nud_Max_DataFilter;
        private System.Windows.Forms.NumericUpDown nud_Min_DataFilter;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txb_SystemNameFilter;
        private System.Windows.Forms.CheckBox chb_SystemNameFilter;
        private System.Windows.Forms.Button but_UpdateComboBox;
        private System.Windows.Forms.TextBox txb_BodyInfo;
    }
}
