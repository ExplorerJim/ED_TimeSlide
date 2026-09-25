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
                prg_IngestionProgress.Value = 0;
                bool useParallelProcessing = chk_MultiThreaded.Checked;

                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;

                var progressHandler = new Progress<double>(percentage =>
                {
                    prg_IngestionProgress.Value = (int)Math.Min(100, Math.Max(0, percentage));
                });

                await Task.Run(() =>
                {
                    ExecuteExtractionWorkflow(targetFiles, useParallelProcessing, token, progressHandler);
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

        private void but_Abort_Click(object sender, EventArgs e)
        {
            if (!_isProcessing || _cts == null) return;
            UpdateLogDisplay("[SIGNAL SENT] Requesting pipeline abort sequence...");
            _cts.Cancel();
        }
        #endregion

        #region Private Functions
        private void ExecuteExtractionWorkflow(string[] files, bool runMultiThreaded, CancellationToken token, IProgress<double> progress)
        {
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

            int totalFiles = sortedFiles.Length;
            int completedFiles = 0;

            using (var storageDriver = new ScanDataStorageDriver(token))
            {

                if (runMultiThreaded)
                {
                    var parallelOptions = new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = -1 }; // -1 as many as it wants was 2 just 2 threads
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

                            int currentDone = Interlocked.Increment(ref completedFiles);
                            progress?.Report(((double)currentDone / totalFiles) * 100);
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
                    for (int i = 0; i < totalFiles; i++)
                    {
                        if (token.IsCancellationRequested) return;
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
                        progress?.Report(((double)completedFiles / totalFiles) * 100);
                    }
                }
                #region Teardown & Drain Queue Processing Syncs
                UpdateLogDisplay("All archives parsed. Draining storage writer queue buffer safely...");
                storageDriver.CompleteIngestion();

                UpdateLogDisplay("Compiling post-ingestion system hierarchy tree layouts...");
                // Call our highly optimized single-pass deferred compiler instance
                var broker = new ScanDatabaseBroker();
                broker.GenerateSystemHierarchyTree(sortedFiles);

                UpdateLogDisplay("Baking physical performance structural tables lookup indices...");
                broker.BuildPerformanceIndexes();
                #endregion
            }
            progress?.Report(100);
            UpdateLogDisplay("Pipeline processing complete. Relational tables baked securely to storage.");
        }

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

        #region Visual UI Contexts
        public void UpdateLogDisplay(string message)
        {
            if (txb_Status.InvokeRequired)
            {
                try { txb_Status.Invoke((MethodInvoker)delegate { UpdateLogDisplay(message); }); }
                catch { }
                return;
            }
            txb_Status.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txb_Status.SelectionStart = txb_Status.Text.Length;
            txb_Status.ScrollToCaret();
        }
        #endregion

        #region Environment Maintenance Engine
        private void PurgeOrphanedDebugResources()
        {
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
            catch { }

            try
            {
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath)) return;
                var directories = Directory.GetDirectories(tempPath, "ED_Extract_*", SearchOption.TopDirectoryOnly);
                foreach (string dir in directories)
                {
                    try { Directory.Delete(dir, true); }
                    catch { }
                }
            }
            catch { }
        }
        #endregion
    }
}
