namespace ED_TimeSlide
{
    partial class frm_ReduceFileSize
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
            this.chk_MultiThreaded = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txb_InputFileNames = new System.Windows.Forms.TextBox();
            this.but_AddFile = new System.Windows.Forms.Button();
            this.but_ClearFileNames = new System.Windows.Forms.Button();
            this.but_Run = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.prg_IngestionProgress = new System.Windows.Forms.ProgressBar();
            this.txb_Status = new System.Windows.Forms.TextBox();
            this.but_Abort = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chk_MultiThreaded
            // 
            this.chk_MultiThreaded.AutoSize = true;
            this.chk_MultiThreaded.Location = new System.Drawing.Point(752, 161);
            this.chk_MultiThreaded.Name = "chk_MultiThreaded";
            this.chk_MultiThreaded.Size = new System.Drawing.Size(97, 17);
            this.chk_MultiThreaded.TabIndex = 13;
            this.chk_MultiThreaded.Text = "Multi Threaded";
            this.chk_MultiThreaded.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txb_InputFileNames);
            this.groupBox2.Controls.Add(this.but_AddFile);
            this.groupBox2.Controls.Add(this.but_ClearFileNames);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(864, 139);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Input";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "File Names";
            // 
            // txb_InputFileNames
            // 
            this.txb_InputFileNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_InputFileNames.Location = new System.Drawing.Point(75, 19);
            this.txb_InputFileNames.Multiline = true;
            this.txb_InputFileNames.Name = "txb_InputFileNames";
            this.txb_InputFileNames.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txb_InputFileNames.Size = new System.Drawing.Size(700, 110);
            this.txb_InputFileNames.TabIndex = 0;
            // 
            // but_AddFile
            // 
            this.but_AddFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.but_AddFile.Location = new System.Drawing.Point(781, 17);
            this.but_AddFile.Name = "but_AddFile";
            this.but_AddFile.Size = new System.Drawing.Size(75, 23);
            this.but_AddFile.TabIndex = 2;
            this.but_AddFile.Text = "Add File";
            this.but_AddFile.UseVisualStyleBackColor = true;
            this.but_AddFile.Click += new System.EventHandler(this.but_FindFile_Click);
            // 
            // but_ClearFileNames
            // 
            this.but_ClearFileNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.but_ClearFileNames.Location = new System.Drawing.Point(781, 46);
            this.but_ClearFileNames.Name = "but_ClearFileNames";
            this.but_ClearFileNames.Size = new System.Drawing.Size(75, 23);
            this.but_ClearFileNames.TabIndex = 2;
            this.but_ClearFileNames.Text = "Clear";
            this.but_ClearFileNames.UseVisualStyleBackColor = true;
            this.but_ClearFileNames.Click += new System.EventHandler(this.but_ClearFileNames_Click);
            // 
            // but_Run
            // 
            this.but_Run.Location = new System.Drawing.Point(12, 157);
            this.but_Run.Name = "but_Run";
            this.but_Run.Size = new System.Drawing.Size(364, 23);
            this.but_Run.TabIndex = 9;
            this.but_Run.Text = "Run";
            this.but_Run.UseVisualStyleBackColor = true;
            this.but_Run.Click += new System.EventHandler(this.but_Run_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Multiselect = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.prg_IngestionProgress);
            this.groupBox1.Controls.Add(this.txb_Status);
            this.groupBox1.Location = new System.Drawing.Point(14, 186);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(862, 417);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Status";
            // 
            // prg_IngestionProgress
            // 
            this.prg_IngestionProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.prg_IngestionProgress.Location = new System.Drawing.Point(11, 20);
            this.prg_IngestionProgress.Name = "prg_IngestionProgress";
            this.prg_IngestionProgress.Size = new System.Drawing.Size(845, 23);
            this.prg_IngestionProgress.TabIndex = 5;
            // 
            // txb_Status
            // 
            this.txb_Status.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_Status.Location = new System.Drawing.Point(6, 49);
            this.txb_Status.Multiline = true;
            this.txb_Status.Name = "txb_Status";
            this.txb_Status.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txb_Status.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txb_Status.Size = new System.Drawing.Size(850, 362);
            this.txb_Status.TabIndex = 4;
            // 
            // but_Abort
            // 
            this.but_Abort.Location = new System.Drawing.Point(382, 157);
            this.but_Abort.Name = "but_Abort";
            this.but_Abort.Size = new System.Drawing.Size(364, 23);
            this.but_Abort.TabIndex = 9;
            this.but_Abort.Text = "Abort";
            this.but_Abort.UseVisualStyleBackColor = true;
            this.but_Abort.Click += new System.EventHandler(this.but_Abort_Click);
            // 
            // frm_ReduceFileSize
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(886, 616);
            this.Controls.Add(this.chk_MultiThreaded);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.but_Abort);
            this.Controls.Add(this.but_Run);
            this.Controls.Add(this.groupBox1);
            this.Name = "frm_ReduceFileSize";
            this.Text = "Populate Scan Database with EDDN data";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chk_MultiThreaded;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txb_InputFileNames;
        private System.Windows.Forms.Button but_AddFile;
        private System.Windows.Forms.Button but_ClearFileNames;
        private System.Windows.Forms.Button but_Run;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txb_Status;
        private System.Windows.Forms.Button but_Abort;
        private System.Windows.Forms.ProgressBar prg_IngestionProgress;
    }
}