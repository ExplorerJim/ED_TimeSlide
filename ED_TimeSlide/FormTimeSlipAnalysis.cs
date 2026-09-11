using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    public partial class FormTimeSlipAnalysis : Form
    {
        private readonly string winRarExePath = @"C:\Program Files\WinRAR\Rar.exe";
        private int processedLinesCount = 0;
        private int foundOutliersCount = 0;
        private const double MaxAllowedErrorPercent = 5.0; // Maximum allowed error percentage for validation
        private const double MaxStellarVarianceLs = 25.0; // Maximum allowed variance in Light Seconds for stellar bodies

        // ─── REGISTRY DATA STORES ───
        // Key format string: "StarSystemName_BodyID" (e.g., "Gondul_2")
        private Dictionary<string, MasterOrbitAnchor> masterRegistry = new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry = new Dictionary<string, StagingOrbitBlock>();

        // Storage paths for the local registry state
        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MasterOrbitRegistry.json");
        private readonly string stagingRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StagingOrbitRegistry.json");

        // Tracking variable to log which file is actively being scraped
        private string currentArchiveName = "";

        private readonly object fileLock = new object();
        private readonly string anomaliesReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DetectedAnomalies.json");

        private BackgroundWorker analysisWorker;

        public FormTimeSlipAnalysis()
        {
            InitializeComponent();
            InitializeBackgroundWorker();

            //testing setup of ease
            txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test";
            btnStartAnalysis.Enabled = true;
        }

        private void InitializeBackgroundWorker()
        {
            analysisWorker = new BackgroundWorker();
            analysisWorker.WorkerReportsProgress = true;
            analysisWorker.DoWork += AnalysisWorker_DoWork;
            analysisWorker.ProgressChanged += AnalysisWorker_ProgressChanged;
            analysisWorker.RunWorkerCompleted += AnalysisWorker_RunWorkerCompleted;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialogScan.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text = folderBrowserDialogScan.SelectedPath;
                btnStartAnalysis.Enabled = true;
                LogMessage($"Selected folder: {folderBrowserDialogScan.SelectedPath}");
            }
        }

        private void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            btnStartAnalysis.Enabled = false;
            btnSelectFolder.Enabled = false;
            progressBarFiles.Value = 0;

            string targetFolder = txtFolderPath.Text;
            LogMessage("Starting Single-Pass Ingestion Loop...");

            // Runs Phase 1 on a background thread so the MDI UI doesn't lock up
            analysisWorker.RunWorkerAsync(targetFolder);
        }

        /// <summary>
        /// Entry point for the background thread execution loop.
        /// </summary>
        private void AnalysisWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;

            processedLinesCount = 0;
            foundOutliersCount = 0;

            // Fetch file targets for all required extensions
            string[] rarFiles = Directory.GetFiles(folderPath, "*.rar");
            string[] bz2Files = Directory.GetFiles(folderPath, "*.bz2");
            string[] jsonlFiles = Directory.GetFiles(folderPath, "*.jsonl");

            int totalFiles = rarFiles.Length + bz2Files.Length + jsonlFiles.Length;

            if (totalFiles == 0)
            {
                worker.ReportProgress(0, "Error: No .rar, .bz2, or .jsonl files found in the target directory.");
                return;
            }

            // Report dynamic file metrics to the UI Log Console
            worker.ReportProgress(0, $"Found {rarFiles.Length} .rar archive(s).");
            worker.ReportProgress(0, $"Found {bz2Files.Length} .bz2 archive(s).");
            worker.ReportProgress(0, $"Found {jsonlFiles.Length} extracted .jsonl file(s).");
            worker.ReportProgress(0, $"--------------------------------------------------");

            if ((rarFiles.Length > 0 || bz2Files.Length > 0) && !File.Exists(winRarExePath))
            {
                worker.ReportProgress(0, "Error: WinRAR engine not found at default location.");
                return;
            }

            processedLinesCount = 0;
            foundOutliersCount = 0;
            int currentFileIndex = 0;

            // 1. Process standard .rar files
            foreach (string file in rarFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming RAR: {Path.GetFileName(file)}");
                StreamRarArchive(file, worker, percentage);
            }

            // 2. Process .bz2 web data dumps using the same engine
            foreach (string file in bz2Files)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming BZ2: {Path.GetFileName(file)}");
                StreamBz2Archive(file, worker, percentage);
            }

            // 3. Process extracted uncompressed .jsonl files natively
            foreach (string file in jsonlFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Reading JSONL: {Path.GetFileName(file)}");
                ReadJsonlLitFile(file);
            }

            e.Result = $"Phase 1 Complete. Swept {processedLinesCount} entries. Isolated {foundOutliersCount} timeline anomalies.";
        }
        
        private void AnalysisWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                LogMessage(e.UserState.ToString());
            }
        }

        private void AnalysisWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SaveRegistriesToDisk();

            btnSelectFolder.Enabled = true;
            btnStartAnalysis.Enabled = true;

            if (e.Result != null)
            {
                LogMessage(e.Result.ToString());
            }

            LogMessage("==================================================");
            LogMessage("PROCESS STATUS: Full Analysis Sweep Completed Successfully.");
            LogMessage("==================================================");
        }

        private void LogMessage(string message)
        {
            // Thread-safe invocation for updating the RichTextBox log
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(LogMessage), message);
            }
            else
            {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.ScrollToCaret();
            }
        }

        /// <summary>
        /// Spawns a background process to read text output directly out of a targeted .rar archive.
        /// </summary>
        private void StreamRarArchiveContents(string rarPath, BackgroundWorker worker, int currentProgress)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = winRarExePath,
                Arguments = $"p -inul \"{rarPath}\"", // Tells WinRAR to extract files strictly to stdout stream
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader streamReader = process.StandardOutput)
                    {
                        string textLine;
                        while ((textLine = streamReader.ReadLine()) != null)
                        {
                            processedLinesCount++;

                            if (string.IsNullOrWhiteSpace(textLine)) continue;
                            if (!textLine.Contains("\"event\":\"Scan\"")) continue;

                            // Route the line to our deserialization and validation filter
                            ProcessScanJsonLine(textLine);
                        }
                    }
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                worker.ReportProgress(currentProgress, $"[ERROR] Failed streaming {Path.GetFileName(rarPath)}: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserializes raw text lines and hands them off to the validation layer.
        /// </summary>
        private void ProcessScanJsonLine(string jsonLine)
        {
            try
            {
                var record = JsonConvert.DeserializeObject<EddnScanRecord>(jsonLine);
                if (record?.Message != null && record.Message.EventName == "Scan")
                {
                    // ─── STAGING AND MASTER REGISTRY ATTACHMENT HUB ───
                    // Now this is an incredibly clean location to add our physics checks!
                    // EvaluateRecordAgainstRegistry(record);
                }
            }
            catch (JsonException)
            {
                // Silently swallow broken text pieces or packet fractures
            }
        }

        private void StreamRarArchive(string rarPath, BackgroundWorker worker, int currentProgress)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = winRarExePath,
                Arguments = $"p -inul \"{rarPath}\"", // Print file contents directly to stdout stream
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            ExecuteStreamReaderProcess(startInfo, rarPath, worker, currentProgress);
        }

        private void StreamBz2Archive(string bz2Path, BackgroundWorker worker, int currentProgress)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = winRarExePath,
                Arguments = $"e -so -inul \"{bz2Path}\"", // 'e -so' extracts any compressed archive to stdout
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            ExecuteStreamReaderProcess(startInfo, bz2Path, worker, currentProgress);
        }

        private void ExecuteStreamReaderProcess(ProcessStartInfo startInfo, string filePath, BackgroundWorker worker, int progress)
        {
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string textLine;
                        while ((textLine = reader.ReadLine()) != null)
                        {
                            processedLinesCount++;
                            EvaluateAndRouteLine(textLine);
                        }
                    }
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                worker.ReportProgress(progress, $"[ERROR] Stream failure on {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private void ReadJsonlLitFile(string jsonlPath)
        {
            using (StreamReader reader = new StreamReader(jsonlPath))
            {
                string textLine;
                while ((textLine = reader.ReadLine()) != null)
                {
                    processedLinesCount++;
                    EvaluateAndRouteLine(textLine);
                }
            }
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
                    // REMOVED: The StarType filter is gone! Stars pass through cleanly now.
                    EvaluateRecordAgainstRegistry(record);
                }
            }
            catch (JsonException) { }
        }

        private void LoadRegistriesFromDisk()
        {
            try
            {
                if (File.Exists(masterRegistryPath))
                {
                    string json = File.ReadAllText(masterRegistryPath);
                    masterRegistry = JsonConvert.DeserializeObject<Dictionary<string, MasterOrbitAnchor>>(json)
                                     ?? new Dictionary<string, MasterOrbitAnchor>();
                    LogMessage($"Loaded {masterRegistry.Count} verified anchors from Master Registry.");
                }

                if (File.Exists(stagingRegistryPath))
                {
                    string json = File.ReadAllText(stagingRegistryPath);
                    stagingRegistry = JsonConvert.DeserializeObject<Dictionary<string, StagingOrbitBlock>>(json)
                                      ?? new Dictionary<string, StagingOrbitBlock>();
                    LogMessage($"Loaded {stagingRegistry.Count} pending bodies from Staging Registry.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[WARNING] Failed initializing registry configuration files: {ex.Message}");
            }
        }

        private void SaveRegistriesToDisk()
        {
            try
            {
                // STREAM SAVING: Writes directly to disk byte-by-byte, using 0MB of excess RAM string buffers!
                using (StreamWriter sw = new StreamWriter(masterRegistryPath, false))
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None }; // None saves space!
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
                LogMessage($"[ERROR] Failed syncing registry cache disk write: {ex.Message}");
            }
        }

        private void FormTimeSlipAnalysis_Load(object sender, EventArgs e)
        {
            LoadRegistriesFromDisk();
        }

        private bool TryValidateStagingCluster(StagingOrbitBlock stagingBlock, out MasterOrbitAnchor verifiedAnchor, out List<StagedPoint> structuralOutliers)
        {
            verifiedAnchor = null;
            structuralOutliers = new List<StagedPoint>();

            // Detect trajectory direction trend (Only meaningful for moving planets)
            var firstPt = stagingBlock.CollectedPoints[0];
            var lastPt = stagingBlock.CollectedPoints[stagingBlock.CollectedPoints.Count - 1];
            bool dynamicIsClimbing = lastPt.Distance >= firstPt.Distance;

            // Is this a stellar body or a body with no orbital loop cadence?
            bool isStellarBody = stagingBlock.OrbitalPeriod <= 0;

            // Cycle through each collected point, temporarily treating it as our trusted model truth anchor
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
                        // STELLAR BODY EVALUATION: Compare raw fixed positions
                        double stellarVariance = Math.Abs(pointToCheck.Distance - candidateAnchor.Distance);
                        if (stellarVariance <= MaxStellarVarianceLs)
                        {
                            pointMatchesBaseline = true;
                        }
                    }
                    else
                    {
                        // PLANET EVALUATION: Run continuous Kepler prediction vector calculation
                        double predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                            stagingBlock.SemiMajorAxis,
                            stagingBlock.Eccentricity,
                            stagingBlock.OrbitalPeriod,
                            candidateAnchor.Timestamp,
                            candidateAnchor.Distance,
                            pointToCheck.Timestamp,
                            dynamicIsClimbing
                        );

                        double variance = Math.Abs(pointToCheck.Distance - predictedDistance);
                        double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;

                        if (errorPercentage <= MaxAllowedErrorPercent)
                        {
                            pointMatchesBaseline = true;
                        }
                    }

                    if (pointMatchesBaseline)
                    {
                        agreementCount++;
                    }
                    else
                    {
                        localOutliers.Add(pointToCheck);
                    }
                }

                // MAJORITY RULES: If 4 or more points out of our 5-point cluster completely agree, lock it in!
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
                        IsClimbingOutward = isStellarBody ? false : dynamicIsClimbing
                    };

                    structuralOutliers = localOutliers;
                    return true;
                }
            }

            return false;
        }
        private void EvaluateRecordAgainstRegistry(EddnScanRecord record)
        {
            var msg = record.Message;
            // Create a unique composite lookup key for the specific planet or star body
            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";

            long currentTimestampSeconds = msg.Timestamp.ToUnixSeconds();
            double currentDistance = msg.DistanceFromArrivalLS;

            // SCENARIO A: The orbital truth model has already been established globally
            if (masterRegistry.TryGetValue(basePlanetKey, out MasterOrbitAnchor anchor))
            {
                double predictedDistance = 0;

                // Check if this body is a star or lacks an orbital loop cadence
                if (!string.IsNullOrEmpty(msg.StarType) || anchor.OrbitalPeriod <= 0)
                {
                    // STELLAR BODY RULE: Stars are physically fixed anchors relative to the system frame.
                    // Their expected coordinate is simply their baseline verified distance!
                    predictedDistance = anchor.AnchorDistance;
                }
                else
                {
                    // STANDARD PLANET RULE: Run the full continuous Kepler orbit vector calculation
                    predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                        anchor.SemiMajorAxis,
                        anchor.Eccentricity,
                        anchor.OrbitalPeriod,
                        anchor.AnchorTimestamp,
                        anchor.AnchorDistance,
                        currentTimestampSeconds,
                        anchor.IsClimbingOutward
                    );
                }

                double variance = Math.Abs(currentDistance - predictedDistance);
                double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;

                // THE HYBRID ANOMALY GATEWAY:
                // Flag if a planet drifts (> 2.0%) OR if ANY body (star/planet) shifts by more than 5.0 Light Seconds!
                if (errorPercentage > MaxAllowedErrorPercent || variance > MaxStellarVarianceLs)
                {
                    foundOutliersCount++;

                    long ghostTimeOut = 0;
                    // Only attempt reverse-Kepler solving if it's a planet with moving orbit properties
                    if (string.IsNullOrEmpty(msg.StarType) && anchor.OrbitalPeriod > 0)
                    {
                        long? calculatedGhostTimestamp = KeplerOrbitSolver.SolveGhostTimestamp(
                            anchor.SemiMajorAxis,
                            anchor.Eccentricity,
                            anchor.OrbitalPeriod,
                            anchor.AnchorTimestamp,
                            anchor.AnchorDistance,
                            currentDistance,
                            anchor.IsClimbingOutward
                        );
                        ghostTimeOut = calculatedGhostTimestamp ?? 0;
                    }

                    // Write the anomaly out to your file report
                    LogAnomalyToFile(basePlanetKey, record, predictedDistance, anchor.LastCheckedTimestamp, ghostTimeOut);
                }
                else
                {
                    // The coordinate is perfectly valid! Update the baseline tracking anchor 
                    anchor.LastCheckedTimestamp = currentTimestampSeconds;
                }
            }
            // SCENARIO B: The body is still gathering evidence in staging
            else
            {
                string uploaderId = record.Header?.UploaderId ?? "Anonymous";
                string stagingLookupKey = $"{basePlanetKey}_{uploaderId}";

                // COLD-START STAR FIX: If this is a star, bypass the 5-point planet wait entirely!
                if (!string.IsNullOrEmpty(msg.StarType) || msg.OrbitalPeriod <= 0)
                {
                    var starAnchor = new MasterOrbitAnchor
                    {
                        SemiMajorAxis = msg.SemiMajorAxis,
                        Eccentricity = msg.Eccentricity,
                        OrbitalPeriod = msg.OrbitalPeriod,
                        AnchorTimestamp = currentTimestampSeconds,
                        AnchorDistance = currentDistance, // Locks the first point as the truth anchor
                        VerifiedSourceFile = currentArchiveName,
                        SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                        IsClimbingOutward = false,
                        LastCheckedTimestamp = currentTimestampSeconds
                    };

                    // Save immediately to master and skip staging entirely
                    masterRegistry[basePlanetKey] = starAnchor;
                    return; // Exit out so the next line can evaluate against this new master anchor!
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

                var newPoint = new StagedPoint
                {
                    Timestamp = currentTimestampSeconds,
                    Distance = currentDistance,
                    SourceFile = currentArchiveName,
                    SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                    UploaderId = uploaderId
                };

                stagingBlock.CollectedPoints.Add(newPoint);

                if (stagingBlock.CollectedPoints.Count == 5)
                {
                    if (TryValidateStagingCluster(stagingBlock, out MasterOrbitAnchor verifiedAnchor, out List<StagedPoint> structuralOutliers))
                    {
                        verifiedAnchor.LastCheckedTimestamp = verifiedAnchor.AnchorTimestamp;
                        masterRegistry[basePlanetKey] = verifiedAnchor;
                        stagingRegistry.Remove(stagingLookupKey);

                        foreach (var outlier in structuralOutliers)
                        {
                            foundOutliersCount++;
                            double predDist = 0;

                            // Apply the same star/fixed-body logic check during the staging back-check routine
                            if (!string.IsNullOrEmpty(msg.StarType) || verifiedAnchor.OrbitalPeriod <= 0)
                            {
                                predDist = verifiedAnchor.AnchorDistance;
                            }
                            else
                            {
                                predDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                                    verifiedAnchor.SemiMajorAxis, verifiedAnchor.Eccentricity, verifiedAnchor.OrbitalPeriod,
                                    verifiedAnchor.AnchorTimestamp, verifiedAnchor.AnchorDistance, outlier.Timestamp, verifiedAnchor.IsClimbingOutward
                                );
                            }

                            long ghostTimeOut = 0;
                            if (string.IsNullOrEmpty(msg.StarType) && verifiedAnchor.OrbitalPeriod > 0)
                            {
                                long? calculatedGhostTimestamp = KeplerOrbitSolver.SolveGhostTimestamp(
                                    verifiedAnchor.SemiMajorAxis, verifiedAnchor.Eccentricity, verifiedAnchor.OrbitalPeriod,
                                    verifiedAnchor.AnchorTimestamp, verifiedAnchor.AnchorDistance, outlier.Distance, verifiedAnchor.IsClimbingOutward
                                );
                                ghostTimeOut = calculatedGhostTimestamp ?? 0;
                            }

                            var reconstructedRecord = new EddnScanRecord
                            {
                                Header = new EddnHeader { SoftwareName = outlier.SoftwareName, UploaderId = outlier.UploaderId },
                                Message = new ScanMessage
                                {
                                    StarSystem = msg.StarSystem,
                                    BodyName = msg.BodyName,
                                    BodyId = msg.BodyId,
                                    Timestamp = DateTimeOffset.FromUnixTimeSeconds(outlier.Timestamp).DateTime,
                                    DistanceFromArrivalLS = outlier.Distance
                                }
                            };

                            LogAnomalyToFile(basePlanetKey, reconstructedRecord, predDist, verifiedAnchor.AnchorTimestamp, ghostTimeOut);
                        }
                    }
                    else
                    {
                        stagingRegistry.Remove(stagingLookupKey);
                    }
                }
            }
        }


        private void LogAnomalyToFile(string registryKey, EddnScanRecord record, double expectedDistance, long previousValidTimestamp, long ghostTimestamp)
        {
            lock (fileLock)
            {
                try
                {
                    double reportedDistance = record.Message.DistanceFromArrivalLS;
                    double variance = Math.Abs(reportedDistance - expectedDistance);
                    double errorPercentage = (variance / expectedDistance) * 100.0;

                    DateTime previousValidDate = DateTimeOffset.FromUnixTimeSeconds(previousValidTimestamp).UtcDateTime;

                    // Format our mathematically resolved Ghost Date strings
                    string ghostDateString = "IMPOSSIBLE_ORBIT_GLITCH";
                    if (ghostTimestamp > 0)
                    {
                        ghostDateString = DateTimeOffset.FromUnixTimeSeconds(ghostTimestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ssZ");
                    }

                    var anomalyPayload = new
                    {
                        DetectionTime = DateTime.UtcNow,
                        SystemKey = registryKey,
                        StarSystem = record.Message.StarSystem,
                        BodyName = record.Message.BodyName,
                        EventTimestamp = record.Message.Timestamp,

                        PreviousValidDateZulu = previousValidDate.ToString("yyyy-MM-dd HH:mm:ssZ"),

                        // NEW CALCULATION OUTPUTS:
                        GhostTimestamp = ghostTimestamp,
                        GhostDateZulu = ghostDateString,

                        UploaderID = record.Header?.UploaderId ?? "Anonymous",
                        SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                        ReportedDistance = reportedDistance,
                        ExpectedDistance = expectedDistance,
                        Variance = variance,
                        ErrorPercentage = errorPercentage,
                        SourceArchive = currentArchiveName
                    };

                    string jsonLine = JsonConvert.SerializeObject(anomalyPayload, Formatting.None) + Environment.NewLine;
                    File.AppendAllText(anomaliesReportPath, jsonLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed recording anomaly row entry: {ex.Message}");
                }
            }
        }


    }
}
