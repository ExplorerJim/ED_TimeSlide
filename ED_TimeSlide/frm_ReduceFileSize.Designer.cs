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
            this.txb_InputFileNames = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.but_AddFile = new System.Windows.Forms.Button();
            this.txb_Status = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.but_Run = new System.Windows.Forms.Button();
            this.but_ClearFileNames = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.rad_MultipleFiles = new System.Windows.Forms.RadioButton();
            this.rad_OneFile = new System.Windows.Forms.RadioButton();
            this.but_SaveFileName = new System.Windows.Forms.Button();
            this.txb_OutputFileName = new System.Windows.Forms.TextBox();
            this.lab_OutputFileName = new System.Windows.Forms.Label();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.chk_MultiThreaded = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // txb_InputFileNames
            // 
            this.txb_InputFileNames.Location = new System.Drawing.Point(75, 19);
            this.txb_InputFileNames.Multiline = true;
            this.txb_InputFileNames.Name = "txb_InputFileNames";
            this.txb_InputFileNames.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txb_InputFileNames.Size = new System.Drawing.Size(700, 110);
            this.txb_InputFileNames.TabIndex = 0;
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
            // but_AddFile
            // 
            this.but_AddFile.Location = new System.Drawing.Point(781, 17);
            this.but_AddFile.Name = "but_AddFile";
            this.but_AddFile.Size = new System.Drawing.Size(75, 23);
            this.but_AddFile.TabIndex = 2;
            this.but_AddFile.Text = "Add File";
            this.but_AddFile.UseVisualStyleBackColor = true;
            this.but_AddFile.Click += new System.EventHandler(this.but_FindFile_Click);
            // 
            // txb_Status
            // 
            this.txb_Status.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_Status.Location = new System.Drawing.Point(6, 19);
            this.txb_Status.Multiline = true;
            this.txb_Status.Name = "txb_Status";
            this.txb_Status.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txb_Status.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txb_Status.Size = new System.Drawing.Size(1512, 111);
            this.txb_Status.TabIndex = 4;
            this.txb_Status.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.txb_Status);
            this.groupBox1.Location = new System.Drawing.Point(15, 187);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1524, 136);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Status";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // but_Run
            // 
            this.but_Run.Location = new System.Drawing.Point(13, 158);
            this.but_Run.Name = "but_Run";
            this.but_Run.Size = new System.Drawing.Size(1526, 23);
            this.but_Run.TabIndex = 2;
            this.but_Run.Text = "Run";
            this.but_Run.UseVisualStyleBackColor = true;
            this.but_Run.Click += new System.EventHandler(this.but_Run_Click);
            // 
            // but_ClearFileNames
            // 
            this.but_ClearFileNames.Location = new System.Drawing.Point(781, 46);
            this.but_ClearFileNames.Name = "but_ClearFileNames";
            this.but_ClearFileNames.Size = new System.Drawing.Size(75, 23);
            this.but_ClearFileNames.TabIndex = 2;
            this.but_ClearFileNames.Text = "Clear";
            this.but_ClearFileNames.UseVisualStyleBackColor = true;
            this.but_ClearFileNames.Click += new System.EventHandler(this.but_ClearFileNames_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txb_InputFileNames);
            this.groupBox2.Controls.Add(this.but_AddFile);
            this.groupBox2.Controls.Add(this.but_ClearFileNames);
            this.groupBox2.Location = new System.Drawing.Point(13, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(864, 139);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Input";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.but_SaveFileName);
            this.groupBox3.Controls.Add(this.txb_OutputFileName);
            this.groupBox3.Controls.Add(this.lab_OutputFileName);
            this.groupBox3.Location = new System.Drawing.Point(884, 13);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(655, 98);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Output";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.rad_MultipleFiles);
            this.groupBox4.Controls.Add(this.rad_OneFile);
            this.groupBox4.Location = new System.Drawing.Point(10, 17);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(231, 42);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Output Type";
            // 
            // rad_MultipleFiles
            // 
            this.rad_MultipleFiles.AutoSize = true;
            this.rad_MultipleFiles.Location = new System.Drawing.Point(76, 19);
            this.rad_MultipleFiles.Name = "rad_MultipleFiles";
            this.rad_MultipleFiles.Size = new System.Drawing.Size(152, 17);
            this.rad_MultipleFiles.TabIndex = 6;
            this.rad_MultipleFiles.Text = "Multiple Files (auto naming)";
            this.rad_MultipleFiles.UseVisualStyleBackColor = true;
            this.rad_MultipleFiles.CheckedChanged += new System.EventHandler(this.rad_MultipleFiles_CheckedChanged);
            // 
            // rad_OneFile
            // 
            this.rad_OneFile.AutoSize = true;
            this.rad_OneFile.Checked = true;
            this.rad_OneFile.Location = new System.Drawing.Point(6, 19);
            this.rad_OneFile.Name = "rad_OneFile";
            this.rad_OneFile.Size = new System.Drawing.Size(64, 17);
            this.rad_OneFile.TabIndex = 5;
            this.rad_OneFile.TabStop = true;
            this.rad_OneFile.Text = "One File";
            this.rad_OneFile.UseVisualStyleBackColor = true;
            this.rad_OneFile.CheckedChanged += new System.EventHandler(this.rad_OneFile_CheckedChanged);
            // 
            // but_SaveFileName
            // 
            this.but_SaveFileName.Location = new System.Drawing.Point(573, 63);
            this.but_SaveFileName.Name = "but_SaveFileName";
            this.but_SaveFileName.Size = new System.Drawing.Size(75, 23);
            this.but_SaveFileName.TabIndex = 4;
            this.but_SaveFileName.Text = "File Location";
            this.but_SaveFileName.UseVisualStyleBackColor = true;
            this.but_SaveFileName.Click += new System.EventHandler(this.but_SaveFileName_Click);
            // 
            // txb_OutputFileName
            // 
            this.txb_OutputFileName.Location = new System.Drawing.Point(67, 65);
            this.txb_OutputFileName.Name = "txb_OutputFileName";
            this.txb_OutputFileName.Size = new System.Drawing.Size(500, 20);
            this.txb_OutputFileName.TabIndex = 3;
            this.txb_OutputFileName.Text = "D:\\Documents\\Gaming\\ED\\EDD Journals\\Temp.EDD";
            // 
            // lab_OutputFileName
            // 
            this.lab_OutputFileName.AutoSize = true;
            this.lab_OutputFileName.Location = new System.Drawing.Point(6, 68);
            this.lab_OutputFileName.Name = "lab_OutputFileName";
            this.lab_OutputFileName.Size = new System.Drawing.Size(54, 13);
            this.lab_OutputFileName.TabIndex = 2;
            this.lab_OutputFileName.Text = "File Name";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog1_FileOk);
            // 
            // chk_MultiThreaded
            // 
            this.chk_MultiThreaded.AutoSize = true;
            this.chk_MultiThreaded.Location = new System.Drawing.Point(884, 118);
            this.chk_MultiThreaded.Name = "chk_MultiThreaded";
            this.chk_MultiThreaded.Size = new System.Drawing.Size(97, 17);
            this.chk_MultiThreaded.TabIndex = 8;
            this.chk_MultiThreaded.Text = "Multi Threaded";
            this.chk_MultiThreaded.UseVisualStyleBackColor = true;
            // 
            // frm_ReduceFileSize
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1551, 335);
            this.Controls.Add(this.chk_MultiThreaded);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.but_Run);
            this.Name = "frm_ReduceFileSize";
            this.Text = "Reduce File Size";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txb_InputFileNames;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button but_AddFile;
        private System.Windows.Forms.TextBox txb_Status;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button but_Run;
        private System.Windows.Forms.Button but_ClearFileNames;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txb_OutputFileName;
        private System.Windows.Forms.Label lab_OutputFileName;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Button but_SaveFileName;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RadioButton rad_MultipleFiles;
        private System.Windows.Forms.RadioButton rad_OneFile;
        private System.Windows.Forms.CheckBox chk_MultiThreaded;
    }
}