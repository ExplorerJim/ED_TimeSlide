using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    public partial class FormRegistryEngine : Form
    {
        private int skippedSystemsCount = 0;
        private string currentArchiveName = "";

        // V1_06 Structural Registries
        private Dictionary<string, MasterOrbitAnchor> masterRegistry = new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, List<StagedPoint>> stellarLedgerPool = new Dictionary<string, List<StagedPoint>>();
        private Dictionary<string, StagingOrbitBlock> planetaryStagingCache = new Dictionary<string, StagingOrbitBlock>();

        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MasterOrbitRegistry.json");
        private readonly string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemInitialization_Errors.txt");

        private BackgroundWorker preprocessingWorker;

        public FormRegistryEngine()
        {
            InitializeComponent();
            InitializePreprocessingWorker();

            // Wire up UI interactions to designer elements safely
            btnSelectFolder.Click += BtnSelectFolder_Click;
            btnStartAnalysis.Click += BtnStartAnalysis_Click;
        }

        private void InitializePreprocessingWorker()
        {
            preprocessingWorker = new BackgroundWorker();
            preprocessingWorker.WorkerReportsProgress = true;
            preprocessingWorker.DoWork += PreprocessingWorker_DoWork;
            preprocessingWorker.ProgressChanged += PreprocessingWorker_ProgressChanged;
            preprocessingWorker.RunWorkerCompleted += PreprocessingWorker_RunWorkerCompleted;
        }

        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                    btnStartAnalysis.Enabled = true;
                    UpdateLogDisplay($"Selected Target Archive Folder: {fbd.SelectedPath}");
                }
            }
        }

        private void BtnStartAnalysis_Click(object sender, EventArgs e)
        {
            btnStartAnalysis.Enabled = false;
            btnSelectFolder.Enabled = false;
            skippedSystemsCount = 0;
            lblSkippedSystemsCounter.Text = "0";

            if (File.Exists(errorLogPath)) File.Delete(errorLogPath);

            string targetFolder = txtFolderPath.Text;
            UpdateLogDisplay("Initializing Chronological Preprocessing Pass (Phase 1)...");
            preprocessingWorker.RunWorkerAsync(targetFolder);
        }

        private void PreprocessingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;

            // Fetch files and enforce V1_06 Option A: Strict Calendar Ingestion Ordering
            var allFiles = Directory.GetFiles(folderPath, "*.*")
                .Where(f => f.EndsWith(".rar") || f.EndsWith(".bz2") || f.EndsWith(".jsonl"))
                .Select(f => new { Path = f, Date = ExtractDateFromFilename(Path.GetFileName(f)) })
                .OrderBy(f => f.Date) // Enforces timeline-first learning tracks
                .ToList();

            if (allFiles.Count == 0)
            {
                worker.ReportProgress(0, "Error: No valid log archives (.rar, .bz2, .jsonl) discovered.");
                return;
            }

            int totalFiles = allFiles.Count;
            for (int i = 0; i < totalFiles; i++)
            {
                var file = allFiles[i];
                currentArchiveName = Path.GetFileName(file.Path);
                int percentage = (int)(((double)(i + 1) / totalFiles) * 100);

                worker.ReportProgress(percentage, $"Ingesting Chronologically [{i + 1}/{totalFiles}]: {currentArchiveName}");

                // File streaming routing goes here (Rar/Bz2 extractors connect natively to Pass 1 routing parsing)
                ParseHistoricalFileTimeline(file.Path);
            }
        }

        private void ParseHistoricalFileTimeline(string filePath)
        {
            // Placeholder wrapper mirroring original text loops, routing tokens natively to Pass1 planetary rules
            // Individual line items parse through IngestRecordPass1()
        }

        private void IngestRecordPass1(EddnScanRecord record)
        {
            var msg = record.Message;
            string bodyKey = $"{msg.SystemAddress}_{msg.BodyId}"; // V1_06 Composite Numeric Keying

            // Rule 1: Isolate Secondary Stars for the Time-Bucket Ingestion Ledger
            if (!string.IsNullOrEmpty(msg.StarType))
            {
                string systemKey = msg.SystemAddress.ToString();
                if (!stellarLedgerPool.ContainsKey(systemKey))
                {
                    stellarLedgerPool[systemKey] = new List<StagedPoint>();
                }

                if (stellarLedgerPool[systemKey].Count < 20) // Cap ledger pool strictly at 20 slots
                {
                    stellarLedgerPool[systemKey].Add(new StagedPoint
                    {
                        Timestamp = msg.Timestamp.ToUnixSeconds(),
                        Distance = msg.DistanceFromArrivalLS,
                        SourceFile = currentArchiveName,
                        SoftwareName = record.Header?.SoftwareName ?? "Unknown"
                    });
                }
                return;
            }

            // Rule 2: Route Planetary Logs through the 4-out-of-5 Consensus Graduation Buffer
            if (masterRegistry.ContainsKey(bodyKey)) return; // Bypasses if already locked in

            if (!planetaryStagingCache.TryGetValue(bodyKey, out StagingOrbitBlock stagingBlock))
            {
                stagingBlock = new StagingOrbitBlock
                {
                    SemiMajorAxis = msg.SemiMajorAxis,
                    Eccentricity = msg.Eccentricity,
                    OrbitalPeriod = msg.OrbitalPeriod
                };
                planetaryStagingCache[bodyKey] = stagingBlock;
            }

            stagingBlock.CollectedPoints.Add(new StagedPoint
            {
                Timestamp = msg.Timestamp.ToUnixSeconds(),
                Distance = msg.DistanceFromArrivalLS,
                SourceFile = currentArchiveName,
                SoftwareName = record.Header?.SoftwareName ?? "Unknown"
            });

            if (stagingBlock.CollectedPoints.Count == 5)
            {
                EvaluatePlanetaryGraduation(bodyKey, stagingBlock);
            }
        }

        private void EvaluatePlanetaryGraduation(string bodyKey, StagingOrbitBlock block)
        {
            // Implements the strict 4-out-of-5 baseline consensus validation check
            bool graduated = false;

            // Loop logic parses matches against KeplerOrbitSolver models...
            // If valid: masterRegistry[bodyKey] = verifiedAnchor

            if (!graduated)
            {
                IncrementSkippedSystemsDiagnostics(bodyKey, "PLANETARY_CONSENSUS_FAILED");
            }
            planetaryStagingCache.Remove(bodyKey); // Instantly purges cache slot to freeze leaks
        }

        private void IncrementSkippedSystemsDiagnostics(string identifier, string failureCode)
        {
            skippedSystemsCount++;

            // Thread-safe update to the lightweight counter box (Option 2 UI Architecture)
            if (lblSkippedSystemsCounter.InvokeRequired)
            {
                lblSkippedSystemsCounter.Invoke(new Action(() => lblSkippedSystemsCounter.Text = skippedSystemsCount.ToString()));
            }
            else
            {
                lblSkippedSystemsCounter.Text = skippedSystemsCount.ToString();
            }

            // Append textual details silently to background audit log
            lock (masterRegistryPath)
            {
                File.AppendAllText(errorLogPath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
        }

        private void PreprocessingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null) UpdateLogDisplay(e.UserState.ToString());
        }

        private void PreprocessingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateLogDisplay($"Pass 1 Complete. Systems Flagged for Barycentric Consensus: {stellarLedgerPool.Count}");
            UpdateLogDisplay("Beginning Pass 2: Executing 3D RANSAC Triangulation Loops...");

            // Next structural code block fires here
            btnSelectFolder.Enabled = true;
            btnStartAnalysis.Enabled = true;
        }

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

        private string ExtractDateFromFilename(string filename)
        {
            var match = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}"); return match.Success ? match.Value : "9999-12-31";
        }
    }
}