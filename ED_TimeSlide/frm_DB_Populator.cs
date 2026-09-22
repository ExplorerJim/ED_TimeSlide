using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class frm_DB_Populator : Form
    {
        #region Variables
        private bool _isProcessing;
        private CancellationTokenSource _cts;
        #endregion

        #region Initialization and Constructors
        public frm_DB_Populator()
        {
            InitializeComponent();
            InitializeDefaultParameters();
            PurgeOrphanedDebugResources();
        }

        private void InitializeDefaultParameters()
        {
            _isProcessing = false;
        }
        #endregion

        #region Form Events
        /// <summary>
        /// Fires when the user clicks the Find File button. Automatically opens the file-system
        /// dialog, allowing multi-selection of raw JSONL streams or WinRAR archives.
        /// </summary>
        private void but_FindFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;

            string fileFilter = "EDDN Logs|*.jsonl;*.rar;*.bz2;*.zip|All Files|*.*";
            openFileDialog1.Filter = fileFilter;

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
                        string lineBreak = Environment.NewLine;
                        txb_InputFileNames.AppendText(lineBreak + file);
                    }
                }
            }
        }
        /// <summary>
        /// Fires when the user clicks the Clear File Names button. Wipes out the file tracking
        /// textbox primitives if an active ingestion run is not running.
        /// </summary>
        private void but_ClearFileNames_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            txb_InputFileNames.Text = "";
        }
        /// <summary>
        /// Fires when the user clicks the Run Ingestion button. Initializes the async task,
        /// captures the primary UI thread synchronization context via Progress, and starts 
        /// the pipeline with a hard-coded MaxDegreeOfParallelism safety gate.
        /// </summary>
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

                string initTimestamp = $"[{DateTime.Now:HH:mm:ss}]";
                txb_Status.Text = $"{initTimestamp} Initializing Data Pipelines..." + Environment.NewLine;

                // Safely reset progress control primitives on the GUI thread
                prg_IngestionProgress.Value = 0;
                bool useParallelProcessing = chk_MultiThreaded.Checked;

                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                // PROGRESS HANDSHAKE: Automatically captures the current UI SynchronizationContext
                var progressHandler = new Progress<double>(percentage =>
                {
                    prg_IngestionProgress.Value = (int)Math.Min(100, Math.Max(0, percentage));
                });

                // Dispatch extraction workflow out onto a thread-pool task background lane
                await Task.Run(() =>
                {
                    ExecuteExtractionWorkflow(targetFiles,useParallelProcessing,token,progressHandler);
                }, token);
            }
            catch (OperationCanceledException ex)
            {
                UpdateLogDisplay($"[ABORTED] Extraction run cancelled by user: {ex.Message}");
                prg_IngestionProgress.Value = 0;
            }
            catch (Exception ex)
            {
                UpdateLogDisplay($"[CRITICAL TERMINAL CRASH] Ingestion form thread failed: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                ToggleInterfaceControls(true);
            }
            #endregion
        }
        /// Fires when the user clicks the Abort button. Automatically triggers the cancellation
        /// token source to request an early exit from active background processing lanes.
        /// </summary>
        private void but_Abort_Click(object sender, EventArgs e)
        {
            if (!_isProcessing || _cts == null) return;

            UpdateLogDisplay("[SIGNAL SENT] Requesting pipeline abort sequence...");
            _cts.Cancel();
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Orchestrates the chronologically sorted archive processing routines. Throttles 
        /// the parallel worker pool to a MaxDegreeOfParallelism of 2 to guarantee system stability
        /// and prevent memory thrashing when extracting dense monthly WinRAR files.
        /// </summary>
        private void ExecuteExtractionWorkflow(string[] files, bool runMultiThreaded, CancellationToken token, IProgress<double> progress)
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
                    UpdateLogDisplay($"Spawning throttled Parallel.ForEach pipeline across {totalFiles} targets...");

                    #region Configure Resilient Concurrency Safety Limits
                    // RAM SHIELD: Limit to 2 concurrent workers to cut external process memory footprint in half
                    var parallelOptions = new ParallelOptions
                    {
                        CancellationToken = token,
                        MaxDegreeOfParallelism = 2
                    };
                    #endregion

                    try
                    {
                        Parallel.ForEach(sortedFiles, parallelOptions, currentFile =>
                        {
                            token.ThrowIfCancellationRequested();
                            UpdateLogDisplay($"[PRODUCER THREAD] Extracting: {Path.GetFileName(currentFile)}");

                            EddnLogProcessor.ProcessDataFile(currentFile, rawLine =>
                            {
                                token.ThrowIfCancellationRequested();
                                EddnLogProcessor.ProcessEddnScanLine(rawLine, record =>
                                {
                                    storageDriver.EnqueueRecord(record);
                                });
                            });

                            // Atomically increment file progress and push safely via captured UI handler
                            int currentDone = Interlocked.Increment(ref completedFiles);
                            double currentPct = ((double)currentDone / totalFiles) * 100;
                            progress?.Report(currentPct);
                        });
                    }
                    catch (OperationCanceledException ex)
                    {
                        UpdateLogDisplay($"[ABORTED] Parallel file ingestion gracefully canceled: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    UpdateLogDisplay($"Spawning sequential single-threaded loader loops across {totalFiles} targets...");

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
                            EddnLogProcessor.ProcessEddnScanLine(rawLine, record =>
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
                UpdateLogDisplay("All archives parsed. Draining storage writer queue buffer safely...");
                storageDriver.CompleteIngestion();
                #endregion
            }

            // Enforce explicit terminal fill confirmation on UI thread
            progress?.Report(100);
            UpdateLogDisplay("Pipeline processing complete. Relational tables baked securely to storage.");
            #endregion
        }
        /// <summary>
        /// Marshals control state updates safely back onto the primary form thread layout
        /// to lock or release structural input parameters during active bulk extraction.
        /// </summary>
        private void ToggleInterfaceControls(bool state)
        {
            _isProcessing = !state;

            this.Invoke((MethodInvoker)delegate
            {
                but_Run.Enabled = state;
                but_ClearFileNames.Enabled = state;
                chk_MultiThreaded.Enabled = state;
                txb_InputFileNames.ReadOnly = !state;
            });
        }
        #endregion
        #region Visual UI Contexts (Thread-Safe Form Loggers)
        /// <summary>
        /// Pipes operational metric traces directly onto the dashboard's text terminal box.
        /// Performs safe context invoking if dispatched from parallel background worker threads.
        /// </summary>
        public void UpdateLogDisplay(string message)
        {
            if (txb_Status.InvokeRequired)
            {
                try
                {
                    txb_Status.Invoke((MethodInvoker)delegate
                    {
                        UpdateLogDisplay(message);
                    });
                }
                catch (Exception ex)
                {
                    // Fail-safe diagnostic sink to protect tracking threads if form handle drops
                    System.Diagnostics.Debug.WriteLine(
                        $"[THREAD OUT CROSS-TALK FAULT] {ex.Message}"
                    );
                }
                return;
            }

            string timestamp = $"[{DateTime.Now:HH:mm:ss}]";
            string endBreak = Environment.NewLine;

            txb_Status.AppendText($"{timestamp} {message}{endBreak}");
            txb_Status.SelectionStart = txb_Status.Text.Length;
            txb_Status.ScrollToCaret();
        }
        #endregion

        #region Environment Maintenance Engine
        /// <summary>
        /// Safely terminates stuck background subprocesses and wipes
        /// abandoned temporary extraction workspaces from prior debug runs.
        /// </summary>
        private void PurgeOrphanedDebugResources()
        {
            // 1. Force close lingering WinRAR processes from aborted runs
            try
            {
                var localProcesses = System.Diagnostics.Process.GetProcesses();
                foreach (var proc in localProcesses)
                {
                    if (proc.ProcessName.Equals("WinRAR", StringComparison.OrdinalIgnoreCase))
                    {
                        proc.Kill();
                        proc.Dispose();
                    }
                }
            }
            catch
            {
                // Suppress permissions alerts on unmanaged system handles
            }

            // 2. Scan and shred orphaned temporary disk workspaces
            try
            {
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath)) return;

                var directories = Directory.GetDirectories(
                    tempPath,
                    "ED_Extract_*",
                    SearchOption.TopDirectoryOnly
                );

                foreach (string dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // Skip files currently locked by active OS processes
                    }
                }
            }
            catch
            {
                // Fail-safe path if local temp folder permissions shift
            }
        }
        #endregion

    }
}