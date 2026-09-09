namespace ED_TimeSlide
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.but_Find_File = new System.Windows.Forms.Button();
            this.txb_File_Location = new System.Windows.Forms.TextBox();
            this.but_Process = new System.Windows.Forms.Button();
            this.txb_Status = new System.Windows.Forms.TextBox();
            this.txb_Odds = new System.Windows.Forms.TextBox();
            this.txb_BodyData = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "File location";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.InitialDirectory = "@d:\\Documents\\Gaming\\ED\\EDD Journals";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // but_Find_File
            // 
            this.but_Find_File.Location = new System.Drawing.Point(518, 10);
            this.but_Find_File.Name = "but_Find_File";
            this.but_Find_File.Size = new System.Drawing.Size(75, 23);
            this.but_Find_File.TabIndex = 1;
            this.but_Find_File.Text = "Find";
            this.but_Find_File.UseVisualStyleBackColor = true;
            this.but_Find_File.Click += new System.EventHandler(this.but_Find_File_Click);
            // 
            // txb_File_Location
            // 
            this.txb_File_Location.Location = new System.Drawing.Point(82, 10);
            this.txb_File_Location.Name = "txb_File_Location";
            this.txb_File_Location.Size = new System.Drawing.Size(430, 20);
            this.txb_File_Location.TabIndex = 2;
            this.txb_File_Location.Text = "D:\\Documents\\Gaming\\ED\\EDD Journals\\Test_Journal.Scan-2025-11-01_RealShort.jsonl";
            // 
            // but_Process
            // 
            this.but_Process.Location = new System.Drawing.Point(599, 10);
            this.but_Process.Name = "but_Process";
            this.but_Process.Size = new System.Drawing.Size(75, 23);
            this.but_Process.TabIndex = 1;
            this.but_Process.Text = "Process";
            this.but_Process.UseVisualStyleBackColor = true;
            this.but_Process.Click += new System.EventHandler(this.but_Process_Click);
            // 
            // txb_Status
            // 
            this.txb_Status.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_Status.Location = new System.Drawing.Point(13, 41);
            this.txb_Status.Multiline = true;
            this.txb_Status.Name = "txb_Status";
            this.txb_Status.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txb_Status.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txb_Status.Size = new System.Drawing.Size(1272, 504);
            this.txb_Status.TabIndex = 3;
            this.txb_Status.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txb_Odds
            // 
            this.txb_Odds.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_Odds.Location = new System.Drawing.Point(16, 551);
            this.txb_Odds.Multiline = true;
            this.txb_Odds.Name = "txb_Odds";
            this.txb_Odds.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txb_Odds.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txb_Odds.Size = new System.Drawing.Size(1269, 105);
            this.txb_Odds.TabIndex = 3;
            this.txb_Odds.WordWrap = false;
            // 
            // txb_BodyData
            // 
            this.txb_BodyData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_BodyData.Location = new System.Drawing.Point(16, 662);
            this.txb_BodyData.Multiline = true;
            this.txb_BodyData.Name = "txb_BodyData";
            this.txb_BodyData.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txb_BodyData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txb_BodyData.Size = new System.Drawing.Size(1269, 105);
            this.txb_BodyData.TabIndex = 3;
            this.txb_BodyData.WordWrap = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1297, 779);
            this.Controls.Add(this.txb_BodyData);
            this.Controls.Add(this.txb_Odds);
            this.Controls.Add(this.txb_Status);
            this.Controls.Add(this.txb_File_Location);
            this.Controls.Add(this.but_Process);
            this.Controls.Add(this.but_Find_File);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button but_Find_File;
        private System.Windows.Forms.TextBox txb_File_Location;
        private System.Windows.Forms.Button but_Process;
        private System.Windows.Forms.TextBox txb_Status;
        private System.Windows.Forms.TextBox txb_Odds;
        private System.Windows.Forms.TextBox txb_BodyData;
    }
}

