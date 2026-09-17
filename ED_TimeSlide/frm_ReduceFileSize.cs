using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class frm_ReduceFileSize : Form
    {
        #region 1. Variables, Properties, and Class Fields
        private bool _isProcessing;
        private CancellationTokenSource _cts;
        #endregion

        #region 2. Initialization and Constructors
        public frm_ReduceFileSize()
        {
            InitializeComponent();
            InitializeDefaultParameters();
        }

        private void InitializeDefaultParameters()
        {
            // Testing setup defaults
            txb_InputFileNames.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test\Journal.Scan - 2025 - 10 - 29.jsonl";
            chk_MultiThreaded.Checked = true;
            _isProcessing = false;
        }
        #endregion

        #region 3. Public Accessor Interfaces / Events
        private void but_FindFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "EDDN Logs|*.jsonl;*.rar;*.bz2;*.zip|All Files|*.*";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openFileDialog1.FileNames)
                {
                    if (string.IsNullOrWhiteSpace(txb_InputFileNames.Text))
                    {
                        txb_InputFileNames.Text = file;
                    }
                    else
                    {
                        txb_InputFileNames.AppendText(Environment.NewLine + file);
                    }
                }
            }
        }

        private void but_ClearFileNames_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            txb_InputFileNames.Text = "";
        }

        #region 3. Public Accessor Interfaces / Events
        private async void but_Run_Click(object sender, EventArgs e)
        {
            #region UI Parameter Gate Validation
            if (_isProcessing) return;

            string[] targetFiles = txb_InputFileNames.Text.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (targetFiles == null || targetFiles.Length == 0)
            {
                MessageBox.Show(
                    "Execution rejected: The input files array tracking parameters are empty.",
                    "Parameter Gate Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }
            #endregion

            #region Asynchronous Execution Engine Dispatch
            try
            {
                ToggleInterfaceControls(false);
                txb_Status.Text = $"[{DateTime.Now:HH:mm:ss}] Initializing Data Pipelines..." + Environment.NewLine;

                // Reset progress controls on the GUI thread
                prg_IngestionProgress.Value = 0;
                bool useParallelProcessing = chk_MultiThreaded.Checked;

                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                // Setup the cross-thread context progress handler
                var progressHandler = new Progress<double>(percentage =>
                {
                    prg_IngestionProgress.Value = (int)Math.Min(100, Math.Max(0, percentage));
                });

                // Pass progress reporter down to the extraction engine task
                await Task.Run(() =>
                {
                    ExecuteExtractionWorkflow(targetFiles, useParallelProcessing, token, progressHandler);
                }, token);
            }
            catch (OperationCanceledException)
            {
                UpdateLogDisplay("[ABORTED] Extraction run cancelled by the user.");
                prg_IngestionProgress.Value = 0;
            }
            catch (Exception ex)
            {
                UpdateLogDisplay($"[CRITICAL TERMINAL CRASH] Ingestion thread failed: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                ToggleInterfaceControls(true);
            }
            #endregion
        }
        #endregion

        private void but_Abort_Click(object sender, EventArgs e)
        {
            if (!_isProcessing || _cts == null) return;

            UpdateLogDisplay("[SIGNAL SENT] Requesting pipeline abort sequence...");
            _cts.Cancel();
        }
        #endregion

        #region 4. Private Processing Functions / Internal Logic
        private void ExecuteExtractionWorkflow(string[] files,bool runMultiThreaded,CancellationToken token,IProgress<double> progress)
        {
            #region Sort Target Datasets Chronologically
            UpdateLogDisplay("Sorting target archives chronologically using regex filename keys...");

            var sortedFiles = files
                .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                .Select(f => new
                {
                    Path = f,
                    DateKey = EddnLogProcessor.ExtractDateFromFilename(Path.GetFileName(f)) ?? "9999-12-31"
                })
                .OrderBy(f => f.DateKey)
                .Select(f => f.Path)
                .ToArray();

            if (sortedFiles.Length == 0)
            {
                UpdateLogDisplay("[ABORTED] No valid target files found existing on local storage.");
                return;
            }
            #endregion

            #region Instantiate Shared Relational Pipeline Drivers
            UpdateLogDisplay($"Target path mapped to: {Settings.ScanDataDbPath}");
            UpdateLogDisplay("Spawning bounded database storage driver instance...");

            int totalFiles = sortedFiles.Length;
            int completedFiles = 0;

            using (var storageDriver = new ScanDataStorageDriver(token))
            {
                if (runMultiThreaded)
                {
                    UpdateLogDisplay($"Spawning Parallel.ForEach pipeline across {totalFiles} file targets...");

                    var parallelOptions = new ParallelOptions { CancellationToken = token };

                    try
                    {
                        Parallel.ForEach(sortedFiles, parallelOptions, currentFile =>
                        {
                            token.ThrowIfCancellationRequested();
                            UpdateLogDisplay($"[PRODUCER THREAD] Extracting: {Path.GetFileName(currentFile)}");

                            EddnLogProcessor.ProcessDataFile(currentFile, rawLine =>
                            {
                                token.ThrowIfCancellationRequested();
                                EddnLogProcessor.EvaluateAndRouteLine(rawLine, record =>
                                {
                                    storageDriver.EnqueueRecord(record);
                                });
                            });

                            // Atomically increment completed count and report progress safely
                            int currentDone = Interlocked.Increment(ref completedFiles);
                            double currentPct = ((double)currentDone / totalFiles) * 100;
                            progress?.Report(currentPct);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        UpdateLogDisplay("[ABORTED] Background worker threads stopped processing files.");
                        return;
                    }
                }
                else
                {
                    UpdateLogDisplay($"Spawning sequential single-threaded loader loops across {totalFiles} file targets...");

                    for (int i = 0; i < totalFiles; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            UpdateLogDisplay("[ABORTED] Sequential loop exited early via cancellation request.");
                            return;
                        }

                        string currentFile = sortedFiles[i];
                        UpdateLogDisplay($"[SERIAL WORKER] Extracting: {Path.GetFileName(currentFile)}");

                        EddnLogProcessor.ProcessDataFile(currentFile, rawLine =>
                        {
                            if (token.IsCancellationRequested) return;
                            EddnLogProcessor.EvaluateAndRouteLine(rawLine, record =>
                            {
                                storageDriver.EnqueueRecord(record);
                            });
                        });

                        completedFiles++;
                        double currentPct = ((double)completedFiles / totalFiles) * 100;
                        progress?.Report(currentPct);
                    }
                }

                #region Teardown & Drain Queue Processing Syncs
                UpdateLogDisplay("All records processed. Draining storage writer queue buffer safely...");
                storageDriver.CompleteIngestion();
                #endregion
            }

            // Force progress bar to fill completely once ingestion is baked safely
            progress?.Report(100);
            UpdateLogDisplay("Pipeline processing complete. Relational tables baked securely to storage.");
            #endregion
        }

        private void ToggleInterfaceControls(bool state)
        {
            _isProcessing = !state;

            // Marshalling UI thread components layout state safely
            this.Invoke((MethodInvoker)delegate
            {
                but_Run.Enabled = state;
                // TODO:but_FindFile.Enabled = state;
                but_ClearFileNames.Enabled = state;
                chk_MultiThreaded.Enabled = state;
                txb_InputFileNames.ReadOnly = !state;
            });
        }
        #endregion
        #region 5. Visual UI Contexts (Thread-Safe Form Loggers)
        /// <summary>
        /// Pipes operational metric traces directly onto the dashboard's text terminal box.
        /// Performs safe context invoking if dispatched from parallel background worker threads.
        /// </summary>
        public void UpdateLogDisplay(string message)
        {
            if (txb_Status.InvokeRequired)
            {
                txb_Status.Invoke((MethodInvoker)delegate
                {
                    UpdateLogDisplay(message);
                });
                return;
            }

            txb_Status.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txb_Status.SelectionStart = txb_Status.Text.Length;
            txb_Status.ScrollToCaret();
        }
        #endregion
    }
}