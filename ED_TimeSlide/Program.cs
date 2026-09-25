using ED_TimeSlide.Engine;
using ED_TimeSlide.Properties;
using MyWinFormsApp;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace ED_TimeSlide
{
    static class Program
    {
        private static SplashForm splashForm;
        private static Thread splashThread;

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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Start the splash screen form on a separate thread
            StartSplashScreen();

            // 2. Perform initialization tasks
            PerformStartupTasks();

            // 3. Close the splash screen
            CloseSplashScreen();

            // 4. Run the main application form
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

        private static void StartSplashScreen()
        {
            splashThread = new Thread(() =>
            {
                splashForm = new SplashForm();
                Application.Run(splashForm);
            });

            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.IsBackground = true;
            splashThread.Start();

            // Wait brief moment to guarantee the window handle has fully generated
            while (splashForm == null || !splashForm.IsHandleCreated)
            {
                Thread.Sleep(50);
            }
        }

        private static void PerformStartupTasks()
        {
            // Simulation of sequential configuration and loading logic

            #region Start SQLite
            UpdateSplashStatus("Loading ScanDB...", 20);
            SQLitePCL.Batteries.Init();
            #endregion
            Thread.Sleep(1000);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            UpdateSplashStatus("Validating structural file streams...", 90);
            Thread.Sleep(1000);
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

            UpdateSplashStatus("Finalizing app context launch...", 100);
            Thread.Sleep(400);
        }

        private static void UpdateSplashStatus(string text, int percent)
        {
            if (splashForm != null && !splashForm.IsDisposed)
            {
                splashForm.UpdateStatus(text, percent);
            }
        }

        private static void CloseSplashScreen()
        {
            if (splashForm != null)
            {
                splashForm.Invoke(new Action(() => splashForm.Close()));
            }

            if (splashThread != null && splashThread.IsAlive)
            {
                splashThread.Join();
            }
        }
    }
}
