using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class FormTimeSlipAnalysis : Form
    {
        private readonly string winRarExePath = @"C:\Program Files\WinRAR\Rar.exe";
        private int processedLinesCount = 0;
        private int foundOutliersCount = 0;
        private const double MaxAllowedErrorPercent = 5.0; // Maximum allowed error percentage for validation
        private const double MaxStellarVarianceLs = 25.0; // Maximum allowed variance in Light Seconds for stellar bodies
        private const double MinStellarDistanceForStars = 50000.0; // Minimum allowed distance in Light Seconds for stars

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
            txtWebCachePath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Other";
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
            // 1. SAVE PHASE 1 RESULTS
            SaveRegistriesToDisk();

            // 2. UI & LOGGING UPDATES
            if (e.Result != null) LogMessage(e.Result.ToString());

            // 3. CHECK FOR ANOMALIES TO PROCESS
            string anomaliesReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DetectedAnomalies.json");
            string masterReportOutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FinalTimeSlipDossier.md");

            if (!File.Exists(anomaliesReportPath))
            {
                LogMessage("Phase 2 Complete: No anomalies logged to track.");
                ResetUiState();
                return;
            }

            LogMessage("Starting Phase 2: Assembling Commander Flight Paths (Local Scan)...");

            // 4. PREPARE THE DOSSIER
            if (File.Exists(masterReportOutPath)) File.Delete(masterReportOutPath);

            string[] anomalyLines = File.ReadAllLines(anomaliesReportPath);
            HashSet<string> processedLookups = new HashSet<string>();
            int dossiersWritten = 0;

            // Write Header
            File.WriteAllText(masterReportOutPath, $"# 🚀 ELITE DANGEROUS TIME-SLIP INVESTIGATION DOSSIER{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath, $"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath, $"Scan Source: Local Files{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}");

            // 5. LOOP THROUGH ANOMALIES
            foreach (string line in anomalyLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    dynamic anomaly = JsonConvert.DeserializeObject(line);
                    string uploaderId = anomaly.UploaderID;
                    DateTime eventTime = anomaly.EventTimestamp;
                    string systemName = anomaly.StarSystem;
                    string bodyName = anomaly.BodyName;
                    string ghostDate = anomaly.GhostDateZulu;
                    double reportedDist = anomaly.ReportedDistance;
                    double expectedDist = anomaly.ExpectedDistance;

                    string sourceFile = anomaly.SourceArchive ?? "";

                    // 1. DETERMINE TARGET DATE
                    // Default to Event Date
                    string targetSearchDate = eventTime.ToString("yyyy-MM-dd");

                    // Try to override with the File Date (Upload Date) if available
                    string fileDate = ExtractDateFromFilename(sourceFile);
                    if (!string.IsNullOrEmpty(fileDate))
                    {
                        targetSearchDate = fileDate;
                    }

                    // Deduplication
                    string lookupToken = $"{uploaderId}_{targetSearchDate}";
                    if (processedLookups.Contains(lookupToken)) continue;
                    processedLookups.Add(lookupToken);

                    LogMessage($"[LOCAL HUNT] Assembling flight path for {uploaderId} in files dated {targetSearchDate}...");

                    // 6. CALL THE LOCAL FETCH METHOD
                    // We pass 'txtFolderPath.Text' to force it to look in your existing scan folder
                    List<JourneyTimelineEvent> journey = FetchCommanderJourneyLocal(
                        targetSearchDate,
                        uploaderId,
                        txtFolderPath.Text,
                        txtWebCachePath.Text,
                        sourceFile
                    );

                    dossiersWritten++;

                    // 7. WRITE TO MARKDOWN
                    using (StreamWriter sw = File.AppendText(masterReportOutPath))
                    {
                        sw.WriteLine($"## 🛰️ Anomaly Target: {bodyName} ({systemName})");
                        sw.WriteLine($"- **Detection Timestamp:** `{eventTime:yyyy-MM-dd HH:mm:ss} UTC`");
                        sw.WriteLine($"- **Ghost Target Date:** `{ghostDate}`");
                        sw.WriteLine($"- **Commander Token:** `{uploaderId}`");
                        sw.WriteLine();
                        sw.WriteLine("### 📅 Chronological Flight Timeline Log");

                        if (journey.Count == 0)
                        {
                            sw.WriteLine("> *No event history found in local files for this commander on this day.*");
                        }
                        else
                        {
                            foreach (var ev in journey)
                            {
                                // ANOMALY MATCHING LOGIC
                                // We flag it if it's a SCAN event, for the right BODY, within 2 seconds of the log time
                                bool isTheAnomaly = (ev.EventType == "Scan")
                                                    && (ev.BodyName == bodyName)
                                                    && Math.Abs((ev.Timestamp - eventTime).TotalSeconds) < 5;

                                string timestamp = $"`{ev.Timestamp:HH:mm:ss}`";
                                string evtType = $"**{ev.EventType}**";
                                string info = $"System: *{ev.StarSystem}* | Body: *{ev.BodyName}* {ev.DetailInfo}";

                                if (isTheAnomaly)
                                {
                                    // 🔴 RED HIGHLIGHT
                                    sw.WriteLine($"- {timestamp} 🔴 {evtType} {info} **<-- [ANOMALY DETECTED]**");
                                    sw.WriteLine($"    - *Reported:* `{reportedDist:F2} LS`");
                                    sw.WriteLine($"    - *Expected:* `{expectedDist:F2} LS`");
                                    sw.WriteLine($"    - *Variance:* `{Math.Abs(reportedDist - expectedDist):F2} LS`");
                                }
                                else
                                {
                                    // Standard Line
                                    sw.WriteLine($"- {timestamp} {evtType} {info}");
                                }
                            }
                        }
                        sw.WriteLine();
                        sw.WriteLine("---");
                        sw.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    // Fail silently on a single bad line so the report finishes
                    System.Diagnostics.Debug.WriteLine($"Report Gen Error: {ex.Message}");
                }
            }

            LogMessage("==================================================");
            LogMessage($"PHASE 2 SUCCESS: Compiled {dossiersWritten} Investigation Records!");
            LogMessage($"Markdown File Saved to: {masterReportOutPath}");
            LogMessage("==================================================");

            ResetUiState();
        }

        private void ResetUiState()
        {
            btnSelectFolder.Enabled = true;
            btnStartAnalysis.Enabled = true;
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

            // DISTANCE GATE FILTER: Turn off secondary star noise and distant binary sun sets instantly.
            // If the body sits further than 10,000 LS out, it's a deep system outlier. We drop it.
            if (msg.DistanceFromArrivalLS <= MinStellarDistanceForStars) return;

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
        //if (targetUploaderId != "7abccbb30f4bfef60d3218aa7d1bd9afdf78a319") { return timeline; }

        /// <summary>
        /// Connects to the online database archive, downloads the target date block log,
        /// and extracts every tracking movement marker belonging to a specific anomalous Commander.
        /// </summary>
        private List<JourneyTimelineEvent> FetchCommanderJourneyLocal(string targetDateYyyyMmDd, string targetUploaderId, string scanFolder, string cacheFolder, string specificSourceFile = "")
        {
            var timeline = new List<JourneyTimelineEvent>();
            var filesToProcess = new HashSet<string>(); // Use HashSet to avoid duplicates automatically

            // 1. Identify which folders to search
            var searchPaths = new List<string>();

            if (Directory.Exists(scanFolder)) searchPaths.Add(scanFolder);

            // Only add cache folder if it exists and is different from scan folder
            if (!string.IsNullOrWhiteSpace(cacheFolder) && Directory.Exists(cacheFolder)
                && !searchPaths.Contains(cacheFolder))
            {
                searchPaths.Add(cacheFolder);
            }

            // 2. Gather ALL matching files from ALL locations
            foreach (string folder in searchPaths)
            {
                try
                {
                    var found = Directory.GetFiles(folder, $"*{targetDateYyyyMmDd}*", SearchOption.AllDirectories)
                                                     .Where(f => f.EndsWith(".rar") || f.EndsWith(".bz2") || f.EndsWith(".jsonl") || f.EndsWith(".zip"));
                    foreach (var f in found) filesToProcess.Add(f);
                }
                catch { }
            }

            // 2. FORCE INCLUDE THE SOURCE FILE (if valid path provided)
            if (!string.IsNullOrEmpty(specificSourceFile) && File.Exists(specificSourceFile))
            {
                filesToProcess.Add(specificSourceFile);
            }
            // Handle case where specificSourceFile is just a filename (e.g. "Journal.Scan...") inside scanFolder
            else if (!string.IsNullOrEmpty(specificSourceFile) && Directory.Exists(scanFolder))
            {
                string potentialPath = Path.Combine(scanFolder, Path.GetFileName(specificSourceFile));
                if (File.Exists(potentialPath)) filesToProcess.Add(potentialPath);
            }

            System.Diagnostics.Debug.WriteLine($"[LOCAL HUNT] Found {filesToProcess.Count} total files for {targetDateYyyyMmDd} across {searchPaths.Count} folders.");

            string winRarPath = @"C:\Program Files\WinRAR\WinRAR.exe";
            if (!File.Exists(winRarPath)) winRarPath = @"C:\Program Files (x86)\WinRAR\WinRAR.exe";

            // 3. Process the Aggregated List
            foreach (string filePath in filesToProcess)
            {
                string[] filesToRead = new string[] { };
                string tempDir = "";
                bool isArchive = false;

                try
                {
                    // CASE A: Raw Text File
                    if (filePath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                    {
                        filesToRead = new string[] { filePath };
                    }
                    // CASE B: Archive -> Extract to Temp
                    else
                    {
                        isArchive = true;
                        tempDir = Path.Combine(Path.GetTempPath(), $"ED_Extract_{Guid.NewGuid()}");
                        Directory.CreateDirectory(tempDir);

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = winRarPath,
                            Arguments = $"e -y -ibck \"{filePath}\" \"{tempDir}\\\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (Process p = Process.Start(startInfo))
                        {
                            p.WaitForExit();
                        }

                        // FIX: Recursive search to handle WinRAR subfolders
                        if (Directory.Exists(tempDir))
                        {
                            filesToRead = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                        }
                    }

                    // 4. READ DATA
                    foreach (string file in filesToRead)
                    {
                        if (new FileInfo(file).Length == 0) continue;

                        foreach (string line in File.ReadLines(file))
                        {
                            if (!line.Contains(targetUploaderId)) continue;

                            try
                            {
                                var record = JsonConvert.DeserializeObject<EddnGenericRecord>(line);
                                if (record?.Message != null)
                                {
                                    string evt = record.Message.EventName;
                                    if (evt == "FSDJump" || evt == "Location" || evt == "CarrierJump"
                                        || evt == "ApproachSettlement" || evt == "Scan" || evt == "Docked" || evt == "Undocked")
                                    {
                                        string resolvedSystem = record.Message.StarSystem ?? record.Message.System ?? "Unknown";
                                        string resolvedBody = record.Message.BodyName ?? record.Message.Body ?? "";
                                        string details = record.Message.StationName ?? record.Message.Name ?? "";

                                        timeline.Add(new JourneyTimelineEvent
                                        {
                                            Timestamp = record.Message.Timestamp,
                                            EventType = evt,
                                            StarSystem = resolvedSystem,
                                            BodyName = resolvedBody,
                                            DetailInfo = details
                                        });
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing {filePath}: {ex.Message}");
                }
                finally
                {
                    if (isArchive && !string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
            }

            // 5. Sort & Unique
            return timeline.GroupBy(x => x.Timestamp)
                           .Select(g => g.First())
                           .OrderBy(x => x.Timestamp)
                           .ToList();
        }




        private void btnSelectWebCache_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialogCache.ShowDialog() == DialogResult.OK)
            {
                txtWebCachePath.Text = folderBrowserDialogCache.SelectedPath;

                // Optional: Save this path to Properties.Settings.Default so it remembers next time
                LogMessage($"Web Cache set to: {txtWebCachePath.Text}");
            }
        }

        private string ExtractDateFromFilename(string filename)
        {
            // Looks for a pattern like "2025-10-29" inside any string
            var match = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
            if (match.Success)
            {
                return match.Value;
            }
            return null;
        }

    }
}