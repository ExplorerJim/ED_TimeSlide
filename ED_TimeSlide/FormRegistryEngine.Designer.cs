namespace ED_TimeSlide
{
    partial class FormRegistryEngine
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.prg_OptimizationProgress = new System.Windows.Forms.ProgressBar();
            this.txbSkippedSystemsCounter = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnStartAnalysis = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.prg_OptimizationProgress);
            this.groupBox2.Controls.Add(this.txbSkippedSystemsCounter);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.rtbLog);
            this.groupBox2.Location = new System.Drawing.Point(13, 41);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(759, 507);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output";
            // 
            // prg_OptimizationProgress
            // 
            this.prg_OptimizationProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.prg_OptimizationProgress.Location = new System.Drawing.Point(6, 19);
            this.prg_OptimizationProgress.Name = "prg_OptimizationProgress";
            this.prg_OptimizationProgress.Size = new System.Drawing.Size(747, 23);
            this.prg_OptimizationProgress.TabIndex = 7;
            // 
            // txbSkippedSystemsCounter
            // 
            this.txbSkippedSystemsCounter.Location = new System.Drawing.Point(137, 48);
            this.txbSkippedSystemsCounter.Name = "txbSkippedSystemsCounter";
            this.txbSkippedSystemsCounter.Size = new System.Drawing.Size(100, 20);
            this.txbSkippedSystemsCounter.TabIndex = 6;
            this.txbSkippedSystemsCounter.Text = "0";
            this.txbSkippedSystemsCounter.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Skipped Systems Counter";
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.Location = new System.Drawing.Point(6, 74);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(747, 427);
            this.rtbLog.TabIndex = 4;
            this.rtbLog.Text = "";
            // 
            // btnStartAnalysis
            // 
            this.btnStartAnalysis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartAnalysis.Enabled = false;
            this.btnStartAnalysis.Location = new System.Drawing.Point(13, 12);
            this.btnStartAnalysis.Name = "btnStartAnalysis";
            this.btnStartAnalysis.Size = new System.Drawing.Size(759, 23);
            this.btnStartAnalysis.TabIndex = 7;
            this.btnStartAnalysis.Text = "Calculate Anchors and Barycenters";
            this.btnStartAnalysis.UseVisualStyleBackColor = true;
            this.btnStartAnalysis.Click += new System.EventHandler(this.BtnStartAnalysis_Click);
            // 
            // FormRegistryEngine
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 560);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnStartAnalysis);
            this.Name = "FormRegistryEngine";
            this.Text = "FormRegistryEngine";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnStartAnalysis;
        private System.Windows.Forms.TextBox txbSkippedSystemsCounter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar prg_OptimizationProgress;
    }
}