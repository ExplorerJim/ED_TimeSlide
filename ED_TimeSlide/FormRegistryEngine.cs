using ED_TimeSlide.Engine;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class FormRegistryEngine : Form
    {
        #region Variables
        private int skippedSystemsCount = 0;
        #endregion

        public FormRegistryEngine()
        {
            InitializeComponent();
            btnStartAnalysis.Enabled = true;
        }

        #region Form Events
        private void BtnStartAnalysis_Click(object sender, EventArgs e)
        {
            // Lock UI button control to prevent multi-click thread starvation loops
            btnStartAnalysis.Enabled = false;
            UpdateLogDisplay("Initializing global O(N²) optimization sweep...");

            string dbPath = Settings.ScanDataDbPath;
            prg_OptimizationProgress.Value = 0;

            // Capture the primary UI thread context safely via the Progress handler wrapper
            var optimizationProgress = new Progress<double>(percentage =>
            {
                prg_OptimizationProgress.Value = (int)Math.Min(100, Math.Max(0, percentage));
            });

            // Offload the heavy computational physics loop to a background task channel
            Task.Run(() =>
            {
                try
                {
                    var optimizer = new BatchConsensusOptimizer(dbPath);

                    // Execute optimization sweep with progress routing hook
                    optimizer.ExecuteGlobalOptimizationPass((message, count, total) =>
                    {
                        // Check if total data count is declared to drive the UI tracking components
                        if (total > 0)
                        {
                            double percentage = ((double)count / total) * 100;
                            ((IProgress<double>)optimizationProgress).Report(percentage);
                        }
                        else
                        {
                            // Route clean milestone text out to display if it isn't an iterative log step
                            this.Invoke(new Action(() => UpdateLogDisplay(message)));
                        }
                    });

                    #region Final Success Marshalling Execution
                    this.Invoke(new Action(() =>
                    {
                        ((IProgress<double>)optimizationProgress).Report(100);
                        UpdateLogDisplay("Global optimization pass completed successfully.");
                        btnStartAnalysis.Enabled = true;
                    }));
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exploded Exception Diagnostic Routing (CS1061 Resolution)
                    this.Invoke(new Action(() =>
                    {
                        UpdateLogDisplay($"CRITICAL ERROR: {ex.Message}");
                        IncrementSkippedSystemsDiagnostics("BATCH_OPTIMIZER", "MATH_PASS_FAIL");
                        btnStartAnalysis.Enabled = true;
                    }));
                    #endregion
                }
            });
        }
        #endregion

        #region Background Worker Functions
        // Explicitly deferred out to background compute tasks
        #endregion

        #region Private Functions
        #endregion

        #region UI Update Functions
        private void UpdateLogDisplay(string message)
        {
            #region Check for Invoke Required
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(UpdateLogDisplay), message);
            }
            #endregion 
            #region Directly Update Log Display
            else
            {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.ScrollToCaret();
            }
            #endregion
        }

        private void IncrementSkippedSystemsDiagnostics(string identifier, string failureCode)
        {
            #region Increment Counter
            skippedSystemsCount++;
            #endregion

            #region Update UI
            if (txbSkippedSystemsCounter.InvokeRequired)
            {
                txbSkippedSystemsCounter.Invoke(new Action(() =>
                    txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString()));
            }
            else
            {
                txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString();
            }
            #endregion

            #region Log Error to Disk
            lock (Settings.ErrorLogPath)
            {
                File.AppendAllText(Settings.ErrorLogPath,
                    $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
            #endregion
        }
        #endregion
    }
}
