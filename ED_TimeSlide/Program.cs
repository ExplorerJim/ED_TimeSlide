using ED_TimeSlide.Properties;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// To get the packages run these commands in the Package Manager Console:
        /// Install-Package ZedGraph
        /// Install-Package Newtonsoft.Json
        /// 
        /// Make sure you have Winrar installed if using rar files
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Start SQLite
            SQLitePCL.Batteries.Init();
            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            #region Rigid Architecture Workspace Verification
            try
            {
                // Verify and enforce absolute folder hygiene using our clean Settings paths
                VerifyAndCreateFolder(Settings.DatabasesDir);
                VerifyAndCreateFolder(Settings.LogsDir);
                VerifyAndCreateFolder(Settings.ErrorsDir);
                VerifyAndCreateFolder(Settings.ReportsDir);
            }
            catch (Exception ex)
            {
                string alertMsg = "CRITICAL WORKSPACE ERROR: Environment initialization failed." +
                                  Environment.NewLine +
                                  "The toolkit cannot verify structural data folders." +
                                  Environment.NewLine + Environment.NewLine +
                                  $"Details: {ex.Message}";

                MessageBox.Show(alertMsg, "Setup Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            #endregion

            Application.Run(new MDIMain());
        }

        #region Private Static Helper Engines
        private static void VerifyAndCreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        #endregion
    }
}
