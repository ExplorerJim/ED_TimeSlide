using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class FormRegistryEngine : Form
    {
        // V1_06 Core Data Models
        private Dictionary<string, MasterOrbitAnchor> masterRegistry =
            new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry =
            new Dictionary<string, StagingOrbitBlock>();

        // System Path Identifiers
        private readonly string masterRegistryPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MasterOrbitRegistry.json");
        private readonly string stagingRegistryPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StagingOrbitRegistry.json");
        private readonly string errorLogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SystemInitialization_Errors.txt");

        // Operational Metric Counters
        private int skippedSystemsCount = 0;
        private int processedLinesCount = 0;
        private string currentArchiveName = "";

        // Fixed Engine Safety Gates
        private const double MaxAllowedErrorPercent = 5.0;
        private const double MaxStellarVarianceLs = 25.0;
        private const double MinStellarDistanceForStars = 50000.0;
        private readonly string winRarExePath = @"C:\Program Files\WinRAR\Rar.exe";

        private BackgroundWorker preprocessingWorker;

        public FormRegistryEngine()
        {
            InitializeComponent();
            InitializePreprocessingWorker();
            LoadExistingRegistries();

            //testing setup of ease
            txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test";
            btnStartAnalysis.Enabled = true;
        }

        private void InitializePreprocessingWorker()
        {
            preprocessingWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };

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
                    UpdateLogDisplay($"Selected Target: {fbd.SelectedPath}");
                }
            }
        }

        private void BtnStartAnalysis_Click(object sender, EventArgs e)
        {
            btnStartAnalysis.Enabled = false;
            btnSelectFolder.Enabled = false;
            skippedSystemsCount = 0;
            processedLinesCount = 0;
            lblSkippedSystemsCounter.Text = "0";
            progressBarFiles.Value = 0;

            if (File.Exists(errorLogPath)) File.Delete(errorLogPath);

            string targetFolder = txtFolderPath.Text;
            UpdateLogDisplay("Initializing Chronological Preprocessing...");
            preprocessingWorker.RunWorkerAsync(targetFolder);
        }

        private void PreprocessingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;

            var allFiles = Directory.GetFiles(folderPath, "*.*")
                .Where(f => f.EndsWith(".rar") || f.EndsWith(".bz2") || f.EndsWith(".jsonl"))
                .Select(f => new { Path = f, Date = ExtractDateFromFilename(Path.GetFileName(f)) })
                .OrderBy(f => f.Date)
                .ToList();

            if (allFiles.Count == 0)
            {
                worker.ReportProgress(0, "Error: No valid log archives discovered.");
                return;
            }

            int totalFiles = allFiles.Count;
            for (int i = 0; i < totalFiles; i++)
            {
                var file = allFiles[i];
                currentArchiveName = Path.GetFileName(file.Path);
                int percentage = (int)(((double)(i + 1) / totalFiles) * 100);

                worker.ReportProgress(percentage, $"Ingesting [{i + 1}/{totalFiles}]: {currentArchiveName}");

                if (file.Path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                {
                    ReadJsonlFileNative(file.Path);
                }
                else
                {
                    StreamCompressedArchive(file.Path, file.Path.EndsWith(".bz2", StringComparison.OrdinalIgnoreCase));
                }
            }

            e.Result = $"Initialization Complete. Swept {processedLinesCount} log entries.";
        }

        private void EvaluateAndRouteLine(string textLine)
        {
            if (string.IsNullOrWhiteSpace(textLine)) return;
            if (!textLine.Contains("\"event\":\"Scan\"") && !textLine.Contains("\"event\": \"Scan\"")) return;

            try
            {
                var record = JsonConvert.DeserializeObject<EddnScanRecord>(textLine);
                if (record?.Message != null && record.Message.EventName == "Scan")
                {
                    EvaluateRecordAgainstStagingRegistry(record);
                }
            }
            catch (JsonException) { }
        }

        private void EvaluateRecordAgainstStagingRegistry(EddnScanRecord record)
        {
            var msg = record.Message;

            // Ignore primary suns
            if (msg.BodyId == 0) return;

            // Ignore belt and ring clusters, as they are not valid orbital bodies
            if (msg.BodyName.Contains("Belt Cluster") || msg.BodyName.Contains("Ring Cluster")) return;

            // DISTANCE GATE FILTER: Turn off secondary star noise and distant binary sun sets instantly.
            // If the body sits further than 10,000 LS out, it's a deep system outlier. We drop it.
            if (msg.StarType != null && msg.DistanceFromArrivalLS <= MinStellarDistanceForStars) return;

            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";
            long currentTimestampSeconds = msg.Timestamp.ToUnixSeconds();
            double currentDistance = msg.DistanceFromArrivalLS;

            if (masterRegistry.ContainsKey(basePlanetKey)) return;

            string uploaderId = record.Header?.UploaderId ?? "Anonymous";
            string stagingLookupKey = $"{basePlanetKey}_{uploaderId}";

            if (!string.IsNullOrEmpty(msg.StarType) || msg.OrbitalPeriod <= 0)
            {
                var starAnchor = new MasterOrbitAnchor
                {
                    SemiMajorAxis = msg.SemiMajorAxis,
                    Eccentricity = msg.Eccentricity,
                    OrbitalPeriod = msg.OrbitalPeriod,
                    AnchorTimestamp = currentTimestampSeconds,
                    AnchorDistance = currentDistance,
                    VerifiedSourceFile = currentArchiveName,
                    SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                    IsClimbingOutward = false,
                    LastCheckedTimestamp = currentTimestampSeconds
                };

                masterRegistry[basePlanetKey] = starAnchor;
                return;
            }

            if (!stagingRegistry.TryGetValue(stagingLookupKey, out StagingOrbitBlock stagingBlock))
            {
                stagingBlock = new StagingOrbitBlock
                {
                    SemiMajorAxis = msg.SemiMajorAxis,
                    Eccentricity = msg.Eccentricity,
                    OrbitalPeriod = msg.OrbitalPeriod
                };
                stagingRegistry[stagingLookupKey] = stagingBlock;
            }

            stagingBlock.CollectedPoints.Add(new StagedPoint
            {
                Timestamp = currentTimestampSeconds,
                Distance = currentDistance,
                SourceFile = currentArchiveName,
                SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                UploaderId = uploaderId
            });

            if (stagingBlock.CollectedPoints.Count == 5)
            {
                if (TryValidateStagingCluster(stagingBlock, out MasterOrbitAnchor verifiedAnchor, out _))
                {
                    verifiedAnchor.LastCheckedTimestamp = verifiedAnchor.AnchorTimestamp;
                    masterRegistry[basePlanetKey] = verifiedAnchor;
                }
                else
                {
                    IncrementSkippedSystemsDiagnostics(basePlanetKey, "PLANETARY_CONSENSUS_FAILED");
                }
                stagingRegistry.Remove(stagingLookupKey);
            }
        }

        private bool TryValidateStagingCluster(StagingOrbitBlock stagingBlock, out MasterOrbitAnchor verifiedAnchor, out List<StagedPoint> structuralOutliers)
        {
            verifiedAnchor = null;
            structuralOutliers = new List<StagedPoint>();

            var firstPt = stagingBlock.CollectedPoints[0];
            var lastPt = stagingBlock.CollectedPoints[stagingBlock.CollectedPoints.Count - 1];

            bool dynamicIsClimbing = lastPt.Distance >= firstPt.Distance;
            bool isStellarBody = stagingBlock.OrbitalPeriod <= 0;

            for (int anchorIndex = 0; anchorIndex < stagingBlock.CollectedPoints.Count; anchorIndex++)
            {
                var candidateAnchor = stagingBlock.CollectedPoints[anchorIndex];
                int agreementCount = 0;
                var localOutliers = new List<StagedPoint>();

                for (int checkIndex = 0; checkIndex < stagingBlock.CollectedPoints.Count; checkIndex++)
                {
                    if (anchorIndex == checkIndex)
                    {
                        agreementCount++;
                        continue;
                    }

                    var pointToCheck = stagingBlock.CollectedPoints[checkIndex];
                    bool pointMatchesBaseline = false;

                    if (isStellarBody)
                    {
                        double stellarVariance = Math.Abs(pointToCheck.Distance - candidateAnchor.Distance);
                        if (stellarVariance <= MaxStellarVarianceLs) pointMatchesBaseline = true;
                    }
                    else
                    {
                        double predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                            stagingBlock.SemiMajorAxis, stagingBlock.Eccentricity, stagingBlock.OrbitalPeriod,
                            candidateAnchor.Timestamp, candidateAnchor.Distance, pointToCheck.Timestamp, dynamicIsClimbing
                        );

                        double variance = Math.Abs(pointToCheck.Distance - predictedDistance);
                        double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;

                        if (errorPercentage <= MaxAllowedErrorPercent) pointMatchesBaseline = true;
                    }

                    if (pointMatchesBaseline) agreementCount++;
                    else localOutliers.Add(pointToCheck);
                }

                if (agreementCount >= 4)
                {
                    verifiedAnchor = new MasterOrbitAnchor
                    {
                        SemiMajorAxis = stagingBlock.SemiMajorAxis,
                        Eccentricity = stagingBlock.Eccentricity,
                        OrbitalPeriod = stagingBlock.OrbitalPeriod,
                        AnchorTimestamp = candidateAnchor.Timestamp,
                        AnchorDistance = candidateAnchor.Distance,
                        VerifiedSourceFile = candidateAnchor.SourceFile,
                        SoftwareName = candidateAnchor.SoftwareName,
                        IsClimbingOutward = !isStellarBody && dynamicIsClimbing
                    };
                    structuralOutliers = localOutliers;
                    return true;
                }
            }
            return false;
        }

        private void SaveRegistriesToDisk()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(masterRegistryPath, false))
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                    serializer.Serialize(jw, masterRegistry);
                }

                using (StreamWriter sw = new StreamWriter(stagingRegistryPath, false))
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                    serializer.Serialize(jw, stagingRegistry);
                }
            }
            catch (Exception ex)
            {
                UpdateLogDisplay($"[ERROR] Serialization crash: {ex.Message}");
            }
        }

        private void LoadExistingRegistries()
        {
            if (File.Exists(masterRegistryPath))
            {
                string json = File.ReadAllText(masterRegistryPath);
                masterRegistry = JsonConvert.DeserializeObject<Dictionary<string, MasterOrbitAnchor>>(json)
                                 ?? new Dictionary<string, MasterOrbitAnchor>();
            }
            if (File.Exists(stagingRegistryPath))
            {
                string json = File.ReadAllText(stagingRegistryPath);
                stagingRegistry = JsonConvert.DeserializeObject<Dictionary<string, StagingOrbitBlock>>(json)
                                  ?? new Dictionary<string, StagingOrbitBlock>();
            }
        }

        private void IncrementSkippedSystemsDiagnostics(string identifier, string failureCode)
        {
            skippedSystemsCount++;

            if (lblSkippedSystemsCounter.InvokeRequired)
            {
                lblSkippedSystemsCounter.Invoke(new Action(() => lblSkippedSystemsCounter.Text = skippedSystemsCount.ToString()));
            }
            else
            {
                lblSkippedSystemsCounter.Text = skippedSystemsCount.ToString();
            }

            lock (masterRegistryPath)
            {
                File.AppendAllText(errorLogPath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
        }

        private void StreamCompressedArchive(string path, bool isBz2)
        {
            string args = isBz2 ? $"e -so -inul \"{path}\"" : $"p -inul \"{path}\"";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = winRarExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            processedLinesCount++;
                            EvaluateAndRouteLine(line);
                        }
                    }
                    process.WaitForExit();
                }
            }
            catch { }
        }

        private void ReadJsonlFileNative(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    processedLinesCount++;
                    EvaluateAndRouteLine(line);
                }
            }
        }

        private void PreprocessingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null) UpdateLogDisplay(e.UserState.ToString());
        }

        private void PreprocessingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SaveRegistriesToDisk();
            if (e.Result != null) UpdateLogDisplay(e.Result.ToString());
            UpdateLogDisplay("Pass 1 Calibration Complete. Tables baked securely to disk.");

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
            var match = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
            return match.Success ? match.Value : "9999-12-31";
        }
    }
}
