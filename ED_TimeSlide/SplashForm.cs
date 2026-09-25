using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace MyWinFormsApp
{
    public partial class SplashForm : Form
    {
        // Import native function to modify control theme properties
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public SplashForm()
        {
            InitializeComponent();
            lblStatus.Parent = pictureBox1;
            lblStatus.BackColor = Color.Transparent;

            // Remove native OS styling so ForeColor and BackColor work perfectly
            SetWindowTheme(progressBar.Handle, "", "");

            // Define your custom appearance colors
            //progressBar.ForeColor = Color.Orange; // Progress fill color
            progressBar.BackColor = Color.DarkGray; // Empty track color
        }

        /// <summary>
        /// Thread-safely updates the status text and progress bar level.
        /// </summary>
        public void UpdateStatus(string message, int progressPercentage)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(message, progressPercentage)));
                return;
            }

            lblStatus.Text = message;

            if (progressPercentage >= progressBar.Minimum && progressPercentage <= progressBar.Maximum)
            {
                progressBar.Value = progressPercentage;
            }
        }
    }
}