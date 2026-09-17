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
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.btnSelectEddFolder = new System.Windows.Forms.Button();
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
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(12, 12);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.Size = new System.Drawing.Size(560, 20);
            this.txtFolderPath.TabIndex = 0;
            // 
            // btnSelectEddFolder
            // 
            this.btnSelectEddFolder.Location = new System.Drawing.Point(578, 10);
            this.btnSelectEddFolder.Name = "btnSelectEddFolder";
            this.btnSelectEddFolder.Size = new System.Drawing.Size(140, 23);
            this.btnSelectEddFolder.TabIndex = 1;
            this.btnSelectEddFolder.Text = "Select Master Database";
            this.btnSelectEddFolder.UseVisualStyleBackColor = true;
            this.btnSelectEddFolder.Click += new System.EventHandler(this.BtnSelectEddFolder_Click);
            // 
            // cmbPlanetSelector
            // 
            this.cmbPlanetSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlanetSelector.FormattingEnabled = true;
            this.cmbPlanetSelector.Location = new System.Drawing.Point(12, 45);
            this.cmbPlanetSelector.Name = "cmbPlanetSelector";
            this.cmbPlanetSelector.Size = new System.Drawing.Size(414, 21);
            this.cmbPlanetSelector.TabIndex = 2;
            this.cmbPlanetSelector.SelectedIndexChanged += new System.EventHandler(this.CmbPlanetSelector_SelectedIndexChanged);
            // 
            // btnPreviousPlanet
            // 
            this.btnPreviousPlanet.Location = new System.Drawing.Point(432, 43);
            this.btnPreviousPlanet.Name = "btnPreviousPlanet";
            this.btnPreviousPlanet.Size = new System.Drawing.Size(140, 23);
            this.btnPreviousPlanet.TabIndex = 3;
            this.btnPreviousPlanet.Text = "◀ Previous Planet";
            this.btnPreviousPlanet.UseVisualStyleBackColor = true;
            this.btnPreviousPlanet.Click += new System.EventHandler(this.BtnPreviousPlanet_Click);
            // 
            // btnNextPlanet
            // 
            this.btnNextPlanet.Location = new System.Drawing.Point(578, 43);
            this.btnNextPlanet.Name = "btnNextPlanet";
            this.btnNextPlanet.Size = new System.Drawing.Size(140, 23);
            this.btnNextPlanet.TabIndex = 4;
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
            this.lblPlotterStatus.TabIndex = 6;
            this.lblPlotterStatus.Text = "[STATUS]";
            // 
            // formsPlotCanvas
            // 
            this.formsPlotCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.formsPlotCanvas.Location = new System.Drawing.Point(12, 72);
            this.formsPlotCanvas.Name = "formsPlotCanvas";
            this.formsPlotCanvas.Size = new System.Drawing.Size(1008, 448);
            this.formsPlotCanvas.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdoZoomYOnly);
            this.groupBox1.Controls.Add(this.rdoZoomXOnly);
            this.groupBox1.Controls.Add(this.rdoZoomStandard);
            this.groupBox1.Location = new System.Drawing.Point(724, 10);
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
            this.rdoZoomYOnly.TabIndex = 0;
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
            this.rdoZoomXOnly.TabIndex = 0;
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
            this.rdoZoomStandard.TabIndex = 0;
            this.rdoZoomStandard.TabStop = true;
            this.rdoZoomStandard.Text = "Standard";
            this.rdoZoomStandard.UseVisualStyleBackColor = true;
            this.rdoZoomStandard.CheckedChanged += new System.EventHandler(this.OnZoomModeChanged);
            // 
            // BtnUpdateRender
            // 
            this.BtnUpdateRender.Location = new System.Drawing.Point(933, 12);
            this.BtnUpdateRender.Name = "BtnUpdateRender";
            this.BtnUpdateRender.Size = new System.Drawing.Size(94, 45);
            this.BtnUpdateRender.TabIndex = 8;
            this.BtnUpdateRender.Text = "Update Render";
            this.BtnUpdateRender.UseVisualStyleBackColor = true;
            this.BtnUpdateRender.Click += new System.EventHandler(this.BtnUpdateRenderCanvas_Click);
            // 
            // FormOrbitDiagnosticPlotter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1032, 561);
            this.Controls.Add(this.BtnUpdateRender);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblPlotterStatus);
            this.Controls.Add(this.formsPlotCanvas);
            this.Controls.Add(this.btnNextPlanet);
            this.Controls.Add(this.btnPreviousPlanet);
            this.Controls.Add(this.cmbPlanetSelector);
            this.Controls.Add(this.btnSelectEddFolder);
            this.Controls.Add(this.txtFolderPath);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "FormOrbitDiagnosticPlotter";
            this.Text = "Keplerian Rail Diagnostic Plotter Deck";
            this.Load += new System.EventHandler(this.FormOrbitDiagnosticPlotter_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        #region Control Variable Handles Definitions
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnSelectEddFolder;
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
    }
}
