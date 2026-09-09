namespace ED_TimeSlide
{
    partial class frm_Journey_Tracker
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
            this.txb_FileLocation = new System.Windows.Forms.TextBox();
            this.but_FindFile = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txb_DistanceToArrival = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txb_Body = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txb_Status = new System.Windows.Forms.TextBox();
            this.txb_Commander_ID = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.but_FindJourney = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.txb_StarSystem = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "File Location (Jsonl)";
            // 
            // txb_FileLocation
            // 
            this.txb_FileLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txb_FileLocation.Location = new System.Drawing.Point(125, 19);
            this.txb_FileLocation.Name = "txb_FileLocation";
            this.txb_FileLocation.Size = new System.Drawing.Size(489, 20);
            this.txb_FileLocation.TabIndex = 1;
            this.txb_FileLocation.Text = "D:\\Documents\\Gaming\\ED\\EDD Journals\\Raw Data\\Journal.Scan-2025-07-03.jsonl";
            // 
            // but_FindFile
            // 
            this.but_FindFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.but_FindFile.Location = new System.Drawing.Point(620, 19);
            this.but_FindFile.Name = "but_FindFile";
            this.but_FindFile.Size = new System.Drawing.Size(75, 23);
            this.but_FindFile.TabIndex = 2;
            this.but_FindFile.Text = "Find File";
            this.but_FindFile.UseVisualStyleBackColor = true;
            this.but_FindFile.Click += new System.EventHandler(this.but_FindFile_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.but_FindFile);
            this.groupBox1.Controls.Add(this.txb_FileLocation);
            this.groupBox1.Controls.Add(this.txb_DistanceToArrival);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txb_StarSystem);
            this.groupBox1.Controls.Add(this.txb_Body);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(708, 129);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Input";
            // 
            // txb_DistanceToArrival
            // 
            this.txb_DistanceToArrival.Location = new System.Drawing.Point(125, 102);
            this.txb_DistanceToArrival.Name = "txb_DistanceToArrival";
            this.txb_DistanceToArrival.Size = new System.Drawing.Size(100, 20);
            this.txb_DistanceToArrival.TabIndex = 1;
            this.txb_DistanceToArrival.Text = "210.968437";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Body as in file";
            // 
            // txb_Body
            // 
            this.txb_Body.Location = new System.Drawing.Point(125, 76);
            this.txb_Body.Name = "txb_Body";
            this.txb_Body.Size = new System.Drawing.Size(200, 20);
            this.txb_Body.TabIndex = 1;
            this.txb_Body.Text = "Gondul A 2";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(113, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Distance to Arrvial (Ls)";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.groupBox3);
            this.groupBox2.Controls.Add(this.txb_Commander_ID);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(13, 177);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(708, 550);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.txb_Status);
            this.groupBox3.Location = new System.Drawing.Point(7, 45);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(695, 499);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Journey";
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
            this.txb_Status.Size = new System.Drawing.Size(682, 471);
            this.txb_Status.TabIndex = 5;
            // 
            // txb_Commander_ID
            // 
            this.txb_Commander_ID.Location = new System.Drawing.Point(125, 19);
            this.txb_Commander_ID.Name = "txb_Commander_ID";
            this.txb_Commander_ID.Size = new System.Drawing.Size(388, 20);
            this.txb_Commander_ID.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Commander ID";
            // 
            // but_FindJourney
            // 
            this.but_FindJourney.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.but_FindJourney.Location = new System.Drawing.Point(13, 148);
            this.but_FindJourney.Name = "but_FindJourney";
            this.but_FindJourney.Size = new System.Drawing.Size(708, 23);
            this.but_FindJourney.TabIndex = 4;
            this.but_FindJourney.Text = "Find the Commander and their Journey";
            this.but_FindJourney.UseVisualStyleBackColor = true;
            this.but_FindJourney.Click += new System.EventHandler(this.but_FindJourney_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk);
            // 
            // txt_StarSystem
            // 
            this.txb_StarSystem.Location = new System.Drawing.Point(125, 50);
            this.txb_StarSystem.Name = "txt_StarSystem";
            this.txb_StarSystem.Size = new System.Drawing.Size(200, 20);
            this.txb_StarSystem.TabIndex = 1;
            this.txb_StarSystem.Text = "Gondul";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "System";
            // 
            // frm_Journey_Tracker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 741);
            this.Controls.Add(this.but_FindJourney);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "frm_Journey_Tracker";
            this.Text = "Journey_Tracker";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txb_FileLocation;
        private System.Windows.Forms.Button but_FindFile;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txb_DistanceToArrival;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button but_FindJourney;
        private System.Windows.Forms.TextBox txb_Commander_ID;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txb_Status;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txb_Body;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txb_StarSystem;
    }
}