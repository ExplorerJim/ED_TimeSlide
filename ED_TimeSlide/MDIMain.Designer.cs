namespace ED_TimeSlide
{
    partial class MDIMain
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.form2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.processingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.process1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.journeyTrackerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullAutomatedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.registryEngineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(17, 17);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.form2ToolStripMenuItem,
            this.processingToolStripMenuItem,
            this.journeyTrackerToolStripMenuItem,
            this.registryEngineToolStripMenuItem,
            this.fullAutomatedToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(2092, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // form2ToolStripMenuItem
            // 
            this.form2ToolStripMenuItem.Name = "form2ToolStripMenuItem";
            this.form2ToolStripMenuItem.Size = new System.Drawing.Size(102, 20);
            this.form2ToolStripMenuItem.Text = "Reduce File Size";
            this.form2ToolStripMenuItem.Click += new System.EventHandler(this.form2ToolStripMenuItem_Click);
            // 
            // processingToolStripMenuItem
            // 
            this.processingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.process1ToolStripMenuItem});
            this.processingToolStripMenuItem.Name = "processingToolStripMenuItem";
            this.processingToolStripMenuItem.Size = new System.Drawing.Size(76, 20);
            this.processingToolStripMenuItem.Text = "Processing";
            // 
            // process1ToolStripMenuItem
            // 
            this.process1ToolStripMenuItem.Name = "process1ToolStripMenuItem";
            this.process1ToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.process1ToolStripMenuItem.Text = "Process 1";
            this.process1ToolStripMenuItem.Click += new System.EventHandler(this.process1ToolStripMenuItem_Click);
            // 
            // journeyTrackerToolStripMenuItem
            // 
            this.journeyTrackerToolStripMenuItem.Name = "journeyTrackerToolStripMenuItem";
            this.journeyTrackerToolStripMenuItem.Size = new System.Drawing.Size(101, 20);
            this.journeyTrackerToolStripMenuItem.Text = "Journey Tracker";
            this.journeyTrackerToolStripMenuItem.Click += new System.EventHandler(this.journeyTrackerToolStripMenuItem_Click);
            // 
            // fullAutomatedToolStripMenuItem
            // 
            this.fullAutomatedToolStripMenuItem.Name = "fullAutomatedToolStripMenuItem";
            this.fullAutomatedToolStripMenuItem.Size = new System.Drawing.Size(101, 20);
            this.fullAutomatedToolStripMenuItem.Text = "Full Automated";
            this.fullAutomatedToolStripMenuItem.Click += new System.EventHandler(this.fullAutomatedToolStripMenuItem_Click);
            // 
            // registryEngineToolStripMenuItem
            // 
            this.registryEngineToolStripMenuItem.Name = "registryEngineToolStripMenuItem";
            this.registryEngineToolStripMenuItem.Size = new System.Drawing.Size(100, 20);
            this.registryEngineToolStripMenuItem.Text = "Registry Engine";
            this.registryEngineToolStripMenuItem.Click += new System.EventHandler(this.registryEngineToolStripMenuItem_Click);
            // 
            // MDIMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2092, 1059);
            this.Controls.Add(this.menuStrip1);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MDIMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ED Time Slide V1.06";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem form2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem processingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem process1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem journeyTrackerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fullAutomatedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem registryEngineToolStripMenuItem;
    }
}