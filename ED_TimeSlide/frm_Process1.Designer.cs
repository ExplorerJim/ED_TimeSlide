namespace ED_TimeSlide
{
    partial class frm_Process1
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
            this.components = new System.ComponentModel.Container();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txb_InputFileNames = new System.Windows.Forms.TextBox();
            this.but_AddFile = new System.Windows.Forms.Button();
            this.but_ClearFileNames = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txb_Stats_Bodys = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txb_Stats_Systems = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txb_Status = new System.Windows.Forms.TextBox();
            this.but_LoadFiles = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.nud_Options_MinDataPostLoad = new System.Windows.Forms.NumericUpDown();
            this.nud_Options_MinDataDuringLoad = new System.Windows.Forms.NumericUpDown();
            this.txb_SystemName = new System.Windows.Forms.TextBox();
            this.chb_System_Name = new System.Windows.Forms.CheckBox();
            this.chb_Options_MinimumDataPostLoad = new System.Windows.Forms.CheckBox();
            this.chb_MinimumDataDuringLoad = new System.Windows.Forms.CheckBox();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.but_Previous = new System.Windows.Forms.Button();
            this.but_Next = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.cmb_Graph_System = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Options_MinDataPostLoad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Options_MinDataDuringLoad)).BeginInit();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txb_InputFileNames);
            this.groupBox2.Controls.Add(this.but_AddFile);
            this.groupBox2.Controls.Add(this.but_ClearFileNames);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(864, 139);
            this.groupBox2.TabIndex = 7;
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
            this.txb_InputFileNames.Location = new System.Drawing.Point(75, 19);
            this.txb_InputFileNames.Multiline = true;
            this.txb_InputFileNames.Name = "txb_InputFileNames";
            this.txb_InputFileNames.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txb_InputFileNames.Size = new System.Drawing.Size(700, 110);
            this.txb_InputFileNames.TabIndex = 0;
            // 
            // but_AddFile
            // 
            this.but_AddFile.Location = new System.Drawing.Point(781, 17);
            this.but_AddFile.Name = "but_AddFile";
            this.but_AddFile.Size = new System.Drawing.Size(75, 23);
            this.but_AddFile.TabIndex = 2;
            this.but_AddFile.Text = "Add File";
            this.but_AddFile.UseVisualStyleBackColor = true;
            this.but_AddFile.Click += new System.EventHandler(this.but_AddFile_Click);
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
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txb_Stats_Bodys);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txb_Stats_Systems);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(1252, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(128, 78);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Stats";
            // 
            // txb_Stats_Bodys
            // 
            this.txb_Stats_Bodys.Location = new System.Drawing.Point(58, 44);
            this.txb_Stats_Bodys.Name = "txb_Stats_Bodys";
            this.txb_Stats_Bodys.Size = new System.Drawing.Size(60, 20);
            this.txb_Stats_Bodys.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Bodies";
            // 
            // txb_Stats_Systems
            // 
            this.txb_Stats_Systems.Location = new System.Drawing.Point(58, 18);
            this.txb_Stats_Systems.Name = "txb_Stats_Systems";
            this.txb_Stats_Systems.Size = new System.Drawing.Size(60, 20);
            this.txb_Stats_Systems.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Systems";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txb_Status);
            this.groupBox3.Location = new System.Drawing.Point(12, 157);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1234, 136);
            this.groupBox3.TabIndex = 9;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Status";
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
            this.txb_Status.Size = new System.Drawing.Size(1222, 111);
            this.txb_Status.TabIndex = 4;
            // 
            // but_LoadFiles
            // 
            this.but_LoadFiles.Location = new System.Drawing.Point(883, 112);
            this.but_LoadFiles.Name = "but_LoadFiles";
            this.but_LoadFiles.Size = new System.Drawing.Size(128, 39);
            this.but_LoadFiles.TabIndex = 10;
            this.but_LoadFiles.Text = "Load Files";
            this.but_LoadFiles.UseVisualStyleBackColor = true;
            this.but_LoadFiles.Click += new System.EventHandler(this.but_LoadFiles_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.nud_Options_MinDataPostLoad);
            this.groupBox4.Controls.Add(this.nud_Options_MinDataDuringLoad);
            this.groupBox4.Controls.Add(this.txb_SystemName);
            this.groupBox4.Controls.Add(this.chb_System_Name);
            this.groupBox4.Controls.Add(this.chb_Options_MinimumDataPostLoad);
            this.groupBox4.Controls.Add(this.chb_MinimumDataDuringLoad);
            this.groupBox4.Location = new System.Drawing.Point(883, 13);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(363, 93);
            this.groupBox4.TabIndex = 11;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Options";
            // 
            // nud_Options_MinDataPostLoad
            // 
            this.nud_Options_MinDataPostLoad.Location = new System.Drawing.Point(156, 39);
            this.nud_Options_MinDataPostLoad.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_Options_MinDataPostLoad.Name = "nud_Options_MinDataPostLoad";
            this.nud_Options_MinDataPostLoad.Size = new System.Drawing.Size(60, 20);
            this.nud_Options_MinDataPostLoad.TabIndex = 1;
            this.nud_Options_MinDataPostLoad.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_Options_MinDataPostLoad.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // nud_Options_MinDataDuringLoad
            // 
            this.nud_Options_MinDataDuringLoad.Location = new System.Drawing.Point(156, 17);
            this.nud_Options_MinDataDuringLoad.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_Options_MinDataDuringLoad.Name = "nud_Options_MinDataDuringLoad";
            this.nud_Options_MinDataDuringLoad.Size = new System.Drawing.Size(60, 20);
            this.nud_Options_MinDataDuringLoad.TabIndex = 1;
            this.nud_Options_MinDataDuringLoad.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_Options_MinDataDuringLoad.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // txb_SystemName
            // 
            this.txb_SystemName.Location = new System.Drawing.Point(156, 63);
            this.txb_SystemName.Name = "txb_SystemName";
            this.txb_SystemName.Size = new System.Drawing.Size(200, 20);
            this.txb_SystemName.TabIndex = 3;
            // 
            // chb_System_Name
            // 
            this.chb_System_Name.AutoSize = true;
            this.chb_System_Name.Location = new System.Drawing.Point(6, 65);
            this.chb_System_Name.Name = "chb_System_Name";
            this.chb_System_Name.Size = new System.Drawing.Size(91, 17);
            this.chb_System_Name.TabIndex = 0;
            this.chb_System_Name.Text = "System Name";
            this.chb_System_Name.UseVisualStyleBackColor = true;
            // 
            // chb_Options_MinimumDataPostLoad
            // 
            this.chb_Options_MinimumDataPostLoad.AutoSize = true;
            this.chb_Options_MinimumDataPostLoad.Location = new System.Drawing.Point(6, 42);
            this.chb_Options_MinimumDataPostLoad.Name = "chb_Options_MinimumDataPostLoad";
            this.chb_Options_MinimumDataPostLoad.Size = new System.Drawing.Size(137, 17);
            this.chb_Options_MinimumDataPostLoad.TabIndex = 0;
            this.chb_Options_MinimumDataPostLoad.Text = "Minimum data post load";
            this.chb_Options_MinimumDataPostLoad.UseVisualStyleBackColor = true;
            // 
            // chb_MinimumDataDuringLoad
            // 
            this.chb_MinimumDataDuringLoad.AutoSize = true;
            this.chb_MinimumDataDuringLoad.Location = new System.Drawing.Point(6, 20);
            this.chb_MinimumDataDuringLoad.Name = "chb_MinimumDataDuringLoad";
            this.chb_MinimumDataDuringLoad.Size = new System.Drawing.Size(146, 17);
            this.chb_MinimumDataDuringLoad.TabIndex = 0;
            this.chb_MinimumDataDuringLoad.Text = "Minimum data during load";
            this.chb_MinimumDataDuringLoad.UseVisualStyleBackColor = true;
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.zedGraphControl1.Location = new System.Drawing.Point(12, 299);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(2029, 766);
            this.zedGraphControl1.TabIndex = 12;
            this.zedGraphControl1.UseExtendedPrintDialog = true;
            // 
            // but_Previous
            // 
            this.but_Previous.Enabled = false;
            this.but_Previous.Location = new System.Drawing.Point(6, 19);
            this.but_Previous.Name = "but_Previous";
            this.but_Previous.Size = new System.Drawing.Size(75, 23);
            this.but_Previous.TabIndex = 13;
            this.but_Previous.Text = "Previous";
            this.but_Previous.UseVisualStyleBackColor = true;
            this.but_Previous.Click += new System.EventHandler(this.but_Previous_Click);
            // 
            // but_Next
            // 
            this.but_Next.Location = new System.Drawing.Point(87, 19);
            this.but_Next.Name = "but_Next";
            this.but_Next.Size = new System.Drawing.Size(75, 23);
            this.but_Next.TabIndex = 13;
            this.but_Next.Text = "Next";
            this.but_Next.UseVisualStyleBackColor = true;
            this.but_Next.Click += new System.EventHandler(this.but_Next_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.but_Next);
            this.groupBox5.Controls.Add(this.cmb_Graph_System);
            this.groupBox5.Controls.Add(this.but_Previous);
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Location = new System.Drawing.Point(1252, 243);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(429, 50);
            this.groupBox5.TabIndex = 14;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Graph Options";
            // 
            // cmb_Graph_System
            // 
            this.cmb_Graph_System.FormattingEnabled = true;
            this.cmb_Graph_System.Location = new System.Drawing.Point(220, 21);
            this.cmb_Graph_System.Name = "cmb_Graph_System";
            this.cmb_Graph_System.Size = new System.Drawing.Size(200, 21);
            this.cmb_Graph_System.TabIndex = 4;
            this.cmb_Graph_System.SelectedIndexChanged += new System.EventHandler(this.cmb_Graph_System_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(168, 24);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(46, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Systems";
            // 
            // frm_Process1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2053, 1077);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.zedGraphControl1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.but_LoadFiles);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Name = "frm_Process1";
            this.Text = "frm_Process1";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Options_MinDataPostLoad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Options_MinDataDuringLoad)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txb_InputFileNames;
        private System.Windows.Forms.Button but_AddFile;
        private System.Windows.Forms.Button but_ClearFileNames;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txb_Stats_Systems;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txb_Status;
        private System.Windows.Forms.Button but_LoadFiles;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.NumericUpDown nud_Options_MinDataDuringLoad;
        private System.Windows.Forms.CheckBox chb_MinimumDataDuringLoad;
        private System.Windows.Forms.NumericUpDown nud_Options_MinDataPostLoad;
        private System.Windows.Forms.CheckBox chb_Options_MinimumDataPostLoad;
        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Button but_Previous;
        private System.Windows.Forms.Button but_Next;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ComboBox cmb_Graph_System;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txb_SystemName;
        private System.Windows.Forms.CheckBox chb_System_Name;
        private System.Windows.Forms.TextBox txb_Stats_Bodys;
        private System.Windows.Forms.Label label3;
    }
}