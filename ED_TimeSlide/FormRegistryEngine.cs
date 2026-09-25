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
            btnStartAnalysis.Enabled = false;
            UpdateLogDisplay("Initializing global O(N²) optimization sweep...");

            string dbPath = Settings.ScanDataDbPath;
            prg_OptimizationProgress.Value = 0;

            var optimizationProgress = new Progress<double>(percentage =>
            {
                prg_OptimizationProgress.Value = (int)Math.Min(100, Math.Max(0, percentage));
            });

            Task.Run(() =>
            {
                try
                {
                    var optimizer = new ScanDBPhase2(dbPath);

                    optimizer.ExecuteGlobalOptimizationPass((message, count, total) =>
                    {
                        if (total > 0)
                        {
                            double percentage = ((double)count / total) * 100;
                            ((IProgress<double>)optimizationProgress).Report(percentage);
                        }
                        else
                        {
                            this.Invoke(new Action(() => UpdateLogDisplay(message)));
                        }
                    });

                    this.Invoke(new Action(() =>
                    {
                        ((IProgress<double>)optimizationProgress).Report(100);
                        UpdateLogDisplay("Global optimization pass completed successfully.");
                        btnStartAnalysis.Enabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        UpdateLogDisplay($"CRITICAL ERROR: {ex.Message}");
                        IncrementSkippedSystemsDiagnostics("BATCH_OPTIMIZER", "MATH_PASS_FAIL");
                        btnStartAnalysis.Enabled = true;
                    }));
                }
            });
        }
        #endregion

        #region UI Update Functions
        private void UpdateLogDisplay(string message)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(UpdateLogDisplay), message);
            }
            else
            {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.ScrollToCaret();
            }
        }

        private void IncrementSkippedSystemsDiagnostics(string identifier, string failureCode)
        {
            skippedSystemsCount++;

            if (txbSkippedSystemsCounter.InvokeRequired)
            {
                txbSkippedSystemsCounter.Invoke(new Action(() =>
                    txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString()));
            }
            else
            {
                txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString();
            }

            lock (Settings.ErrorLogPath)
            {
                File.AppendAllText(Settings.ErrorLogPath,
                    $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
        }
        #endregion
    }
}
