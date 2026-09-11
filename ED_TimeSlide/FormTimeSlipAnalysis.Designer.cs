namespace ED_TimeSlide
{
    partial class FormTimeSlipAnalysis
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
            this.folderBrowserDialogScan = new System.Windows.Forms.FolderBrowserDialog();
            this.btnSelectFolder = new System.Windows.Forms.Button();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.btnStartAnalysis = new System.Windows.Forms.Button();
            this.progressBarFiles = new System.Windows.Forms.ProgressBar();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSelectFolder
            // 
            this.btnSelectFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectFolder.Location = new System.Drawing.Point(678, 16);
            this.btnSelectFolder.Name = "btnSelectFolder";
            this.btnSelectFolder.Size = new System.Drawing.Size(75, 23);
            this.btnSelectFolder.TabIndex = 0;
            this.btnSelectFolder.Text = "Select Scan Folder...";
            this.btnSelectFolder.UseVisualStyleBackColor = true;
            this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFolderPath.Location = new System.Drawing.Point(6, 19);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.ReadOnly = true;
            this.txtFolderPath.Size = new System.Drawing.Size(666, 20);
            this.txtFolderPath.TabIndex = 1;
            // 
            // btnStartAnalysis
            // 
            this.btnStartAnalysis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartAnalysis.Enabled = false;
            this.btnStartAnalysis.Location = new System.Drawing.Point(13, 72);
            this.btnStartAnalysis.Name = "btnStartAnalysis";
            this.btnStartAnalysis.Size = new System.Drawing.Size(759, 23);
            this.btnStartAnalysis.TabIndex = 2;
            this.btnStartAnalysis.Text = "Run Phase 1 Sweep";
            this.btnStartAnalysis.UseVisualStyleBackColor = true;
            this.btnStartAnalysis.Click += new System.EventHandler(this.btnStartAnalysis_Click);
            // 
            // progressBarFiles
            // 
            this.progressBarFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarFiles.Location = new System.Drawing.Point(6, 19);
            this.progressBarFiles.Name = "progressBarFiles";
            this.progressBarFiles.Size = new System.Drawing.Size(747, 23);
            this.progressBarFiles.TabIndex = 3;
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.Location = new System.Drawing.Point(6, 48);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(747, 393);
            this.rtbLog.TabIndex = 4;
            this.rtbLog.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.txtFolderPath);
            this.groupBox1.Controls.Add(this.btnSelectFolder);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(759, 53);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Inputs";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.progressBarFiles);
            this.groupBox2.Controls.Add(this.rtbLog);
            this.groupBox2.Location = new System.Drawing.Point(13, 101);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(759, 447);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output";
            // 
            // FormTimeSlipAnalysis
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 560);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnStartAnalysis);
            this.Name = "FormTimeSlipAnalysis";
            this.Text = "FormTimeSlipAnalysis";
            this.Load += new System.EventHandler(this.FormTimeSlipAnalysis_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogScan;
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnStartAnalysis;
        private System.Windows.Forms.ProgressBar progressBarFiles;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}