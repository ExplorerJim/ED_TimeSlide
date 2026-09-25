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
        }

        /// <summary>
        /// Thread-safely updates the status text and progress bar level.
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(message)));
                return;
            }

            lblStatus.Text = message;
        }
    }
}