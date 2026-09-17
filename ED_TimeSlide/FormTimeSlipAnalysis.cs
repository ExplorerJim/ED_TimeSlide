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
        #region Variables
        private int processedLinesCount = 0;
        private int foundOutliersCount = 0;

        #region ─── REGISTRY DATA STORES ───, Key format string: "StarSystemName_BodyID" (e.g., "Gondul_2")
        private Dictionary<string, MasterOrbitAnchor> masterRegistry = new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry = new Dictionary<string, StagingOrbitBlock>();
        #endregion

        #region Storage paths for the local registry state
        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.MasterOrbitRegistryFileName);
        #endregion

        #region Tracking variable to log which file is actively being scraped
        private string currentArchiveName = "";
        #endregion

        private readonly object fileLock = new object();
        private readonly string anomaliesReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.AnomaliesReportFileName);

        private BackgroundWorker analysisWorker;

        private readonly ForensicParserEngine forensicParser = new ForensicParserEngine();

        List<JourneyTimelineEvent> temporaryJourneyBuffer;
        #endregion

        #region Instances
        private OrbitRegistry inst_OrbitRegistry;
        #endregion

        public FormTimeSlipAnalysis(OrbitRegistry orbitRegistry)
        {
            InitializeComponent();
            InitializeBackgroundWorker();

            inst_OrbitRegistry = orbitRegistry;

            //testing setup of ease
            txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test";
            btnStartAnalysis.Enabled = true;
            txtWebCachePath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Other";
        }

        #region Form Events
        private void FormTimeSlipAnalysis_Load(object sender, EventArgs e)
        {
            LoadRegistriesFromDisk();
        }
        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            #region Folder Browser Dialog setup
            if (folderBrowserDialogScan.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text = folderBrowserDialogScan.SelectedPath;
                btnStartAnalysis.Enabled = true;
                UpdateLogDisplay($"Selected folder: {folderBrowserDialogScan.SelectedPath}");
            }
            #endregion
        }
        private void btnStartAnalysis_Click(object sender, EventArgs e)
        {
            #region UI State Updates
            btnStartAnalysis.Enabled = false;
            btnSelectFolder.Enabled = false;
            progressBarFiles.Value = 0;
            #endregion
            #region Capture the target folder path for processing
            string targetFolder = txtFolderPath.Text;
            UpdateLogDisplay("Starting Single-Pass Ingestion Loop...");
            #endregion
            #region Runs Phase 1 on a background thread so the MDI UI doesn't lock up
            analysisWorker.RunWorkerAsync(targetFolder);
            #endregion
        }
        private void btnSelectWebCache_Click(object sender, EventArgs e)
        {
            #region Folder Browser Dialog setup
            if (folderBrowserDialogCache.ShowDialog() == DialogResult.OK)
            {
                txtWebCachePath.Text = folderBrowserDialogCache.SelectedPath;

                // Optional: Save this path to Properties.Settings.Default so it remembers next time
                UpdateLogDisplay($"Web Cache set to: {txtWebCachePath.Text}");
            }
            #endregion
        }
        #endregion

        #region Background Worker Functions
        private void InitializeBackgroundWorker()
        {
            #region Background Worker Setup
            analysisWorker = new BackgroundWorker();
            analysisWorker.WorkerReportsProgress = true;
            analysisWorker.DoWork += AnalysisWorker_DoWork;
            analysisWorker.ProgressChanged += AnalysisWorker_ProgressChanged;
            analysisWorker.RunWorkerCompleted += AnalysisWorker_RunWorkerCompleted;
            #endregion
        }
        private void AnalysisWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            #region Initialize Local Ingestion Property Counters
            string targetFolderPath = (string)e.Argument;
            BackgroundWorker activeWorker = sender as BackgroundWorker;

            processedLinesCount = 0;
            foundOutliersCount = 0;

            // Instantiate persistent memory array field before streaming begins
            temporaryJourneyBuffer = new List<JourneyTimelineEvent>();
            #endregion

            #region Execute High-Speed Background Extraction Sequence
            // Section 6.1: Delegates directory handling entirely to the helper pipeline
            ExecuteRetrospectiveIngestionSequence(targetFolderPath, activeWorker);
            #endregion

            #region Finalization Thread Hand-off Reporting Matrix
            string logSummaryMessage = $"Phase 1 Complete. " +
                                       $"Swept {processedLinesCount} entries. " +
                                       $"Isolated {foundOutliersCount} anomalies.";

            e.Result = logSummaryMessage;
            #endregion
        }
        private void AnalysisWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            #region Update the UI Progress Bar and Log Messages
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                UpdateLogDisplay(e.UserState.ToString());
            }
            #endregion
        }
        private void AnalysisWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            #region Action Post-Analysis Cleanup Lifecycle on UI Thread Context
            string anomaliesInPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DetectedAnomalies.json");
            string dossierOutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FinalTimeSlipDossier.md");

            ExecuteOutputEngineeringReportFormatting(anomaliesInPath, dossierOutPath);
            #endregion
        }
        private void ExecuteRetrospectiveIngestionSequence(string folderPath, BackgroundWorker worker)
        {
            #region Guard Clauses Against Invalid Directory Paths
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                worker.ReportProgress(0, "Error: Selected target folder path is invalid.");
                return;
            }
            #endregion

            #region Gather Target Log Archives from Filesystem
            string[] rarFiles = Directory.GetFiles(folderPath, "*.rar");
            string[] bz2Files = Directory.GetFiles(folderPath, "*.bz2");
            string[] jsonlFiles = Directory.GetFiles(folderPath, "*.jsonl");

            int totalFiles = rarFiles.Length + bz2Files.Length + jsonlFiles.Length;
            #endregion

            #region Verify Target Files Footprint Matrix Presence
            if (totalFiles == 0)
            {
                worker.ReportProgress(0, "Error: No .rar, .bz2, or .jsonl files found.");
                return;
            }
            #endregion

            #region Streaming Operational Metrics to UI Counter Terminal
            worker.ReportProgress(0, $"Found {rarFiles.Length} .rar archive(s).");
            worker.ReportProgress(0, $"Found {bz2Files.Length} .bz2 archive(s).");
            worker.ReportProgress(0, $"Found {jsonlFiles.Length} extracted .jsonl file(s).");
            worker.ReportProgress(0, $"--------------------------------------------------");
            #endregion

            #region Processing Loop: Standard .rar Archives
            int currentFileIndex = 0;
            foreach (string file in rarFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming RAR: {currentArchiveName}");

                // Stream straight through to line execution block
                EddnLogProcessor.ProcessDataFile(file, line => IdentifyDiscontinuityPointsAndAnomalies(line));
            }
            #endregion

            #region Processing Loop: Compressed .bz2 Data Dumps
            foreach (string file in bz2Files)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming BZ2: {currentArchiveName}");

                EddnLogProcessor.ProcessDataFile(file, line => IdentifyDiscontinuityPointsAndAnomalies(line));
            }
            #endregion

            #region Processing Loop: Extracted Native .jsonl Logs
            foreach (string file in jsonlFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Reading JSONL: {currentArchiveName}");

                EddnLogProcessor.ProcessDataFile(file, line =>
                    IdentifyDiscontinuityPointsAndAnomalies(line));
            }
            #endregion
        }
        private void IdentifyDiscontinuityPointsAndAnomalies(string textLine)
        {
            #region Preliminary Ingestion Verification Guard
            if (string.IsNullOrWhiteSpace(textLine)) return;
            #endregion

            #region Telemetry Ingestion Loop: Core Scan Subsystem Forward Lookup Check
            if (textLine.Contains("\"event\":\"Scan\"") || textLine.Contains("\"event\": \"Scan\""))
            {
                try
                {
                    var record = JsonConvert.DeserializeObject<EddnRecords>(textLine);
                    if (record?.Message != null && record.Message.EventName == "Scan")
                    {
                        #region Dynamic Axis Key Verification Fallback
                        if (record.Message.SemiMajorAxis == 0 && record.Message.Axis > 0)
                        {
                            record.Message.SemiMajorAxis = record.Message.Axis;
                        }
                        #endregion

                        #region Execute Trajectory Validation Check Against Pre-Baked Table
                        VerifyIncomingScanAgainstMasterModel(record);
                        #endregion
                    }
                }
                catch (JsonException ex)
                {
                    #region Diagnostic Catch Interface Terminal Report Handler
                    UpdateLogDisplay($"[INGESTION ERROR] Scan Deserialization Fault: " +
                                     $"{ex.Message} | Raw Data: {textLine}");
                    #endregion
                }
                return;
            }
            #endregion

            #region Telemetry Ingestion Loop: Extensible Factory Travel & Spatial Anchors Ingestion
            // Call our forensic factory component to transform text into clean timeline models
            var timelineEvent = forensicParser.ParseTimelineLine(textLine, "Anonymous", (error, details) =>
                UpdateLogDisplay($"[INGESTION ERROR] {error}: {details}"));

            if (timelineEvent != null)
            {
                #region Increment Ingestion Statistics Counters
                processedLinesCount++;
                #endregion

                #region Cache Valid Object Framework Records Directly into Persistence Field List
                temporaryJourneyBuffer.Add(timelineEvent);
                #endregion
            }
            #endregion
        }
        private void ExecuteOutputEngineeringReportFormatting(string anomaliesReportPath, string masterReportOutPath)
        {
            #region Guard Clauses Against Missing Anomalies Footprint Table
            if (!File.Exists(anomaliesReportPath))
            {
                UpdateLogDisplay("Phase 2 Complete: No anomalous signatures logged to track.");
                ResetUiState();
                return;
            }
            #endregion

            #region Initialize Local Dossier Buffer Stream Tables
            UpdateLogDisplay("Starting Phase 2: Assembling Commander Flight Paths...");

            if (File.Exists(masterReportOutPath)) File.Delete(masterReportOutPath);

            string[] anomalyLines = File.ReadAllLines(anomaliesReportPath);
            var processedLookups = new HashSet<string>();
            int dossiersWritten = 0;
            #endregion

            #region Section 7.1 Write Rigid Document Header Standard Primitives
            File.WriteAllText(masterReportOutPath,
                $"# 🚀 ELITE DANGEROUS TIME-SLIP INVESTIGATION DOSSIER{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath,
                $"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath,
                $"Scan Source: Local In-Memory Buffer Pass{Environment.NewLine}" +
                $"---{Environment.NewLine}{Environment.NewLine}");
            #endregion

            #region Core Sequential Dossier Compiler Loop
            foreach (string line in anomalyLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    #region Deserialize Raw Historical Track Line
                    dynamic anomaly = JsonConvert.DeserializeObject(line);
                    string uploaderId = anomaly.UploaderID;
                    DateTime eventTime = anomaly.EventTimestamp;
                    string systemName = anomaly.StarSystem;
                    string bodyName = anomaly.BodyName;
                    string ghostDate = anomaly.GhostDateZulu;
                    double reportedDist = anomaly.ReportedDistance;
                    double expectedDist = anomaly.ExpectedDistance;
                    #endregion

                    #region Deduplication Index Token Mask Evaluation
                    string targetSearchDate = eventTime.ToString("yyyy-MM-dd");
                    string lookupToken = $"{uploaderId}_{targetSearchDate}";

                    if (processedLookups.Contains(lookupToken)) continue;
                    processedLookups.Add(lookupToken);
                    #endregion

                    #region Extract Active Chronological Flight Log Arrays from Memory
                    // Section 3.1: Queries factory collection directly without touching disk
                    List<JourneyTimelineEvent> journey = FetchCommanderJourneyLocal(
                        uploaderId,
                        temporaryJourneyBuffer);

                    dossiersWritten++;
                    #endregion

                    #region Stream Formatted Markdown Blocks to Disk File
                    using (StreamWriter sw = File.AppendText(masterReportOutPath))
                    {
                        sw.WriteLine($"## 🛰️ Anomaly Target: {bodyName}");
                        sw.WriteLine($"- **Detection Timestamp:** `{eventTime:yyyy-MM-dd HH:mm:ss} UTC`");
                        sw.WriteLine($"- **Ghost Target Date:** `{ghostDate}`");
                        sw.WriteLine($"- **Commander Token:** `{uploaderId}`");
                        sw.WriteLine();
                        sw.WriteLine("### 📅 Chronological Flight Timeline Log");

                        if (journey.Count == 0)
                        {
                            sw.WriteLine("> *No event history found in memory for this commander on this day.*");
                        }
                        else
                        {
                            foreach (var ev in journey)
                            {
                                bool isTheAnomaly = (ev.EventType == "Scan") &&
                                                    (ev.BodyName == bodyName) &&
                                                    Math.Abs((ev.Timestamp - eventTime).TotalSeconds) < 5;

                                string timestamp = $"`{ev.Timestamp:HH:mm:ss}`";
                                string evtType = $"**{ev.EventType}**";
                                string info = $"System Body: *{ev.BodyName}* {ev.DetailInfo}";

                                if (isTheAnomaly)
                                {
                                    sw.WriteLine($"- {timestamp} 🔴 {evtType} {info} **<-- [ANOMALY DETECTED]**");
                                    sw.WriteLine($"    - *Reported:* `{reportedDist:F4} LS`");
                                    sw.WriteLine($"    - *Expected:* `{expectedDist:F4} LS`");
                                    sw.WriteLine($"    - *Variance:* `{Math.Abs(reportedDist - expectedDist):F4} LS`");
                                }
                                else
                                {
                                    sw.WriteLine($"- {timestamp} {evtType} {info}");
                                }
                            }
                        }
                        sw.WriteLine($"{Environment.NewLine}---{Environment.NewLine}");
                    }
                    #endregion
                }
                catch { }
            }
            #endregion

            #region Final Interface Log Console Summary Report
            UpdateLogDisplay("==================================================");
            UpdateLogDisplay($"PHASE 2 SUCCESS: Compiled {dossiersWritten} Investigation Records!");
            UpdateLogDisplay($"Markdown File Saved to: {masterReportOutPath}");
            UpdateLogDisplay("==================================================");
            ResetUiState();
            #endregion
        }

        #endregion

        #region Private Functions
        private void EvaluateAndRouteLine(string textLine, List<JourneyTimelineEvent> journeyBuffer)
        {
            #region Preliminary Ingestion Verification Guard
            if (string.IsNullOrWhiteSpace(textLine)) return;
            #endregion

            #region Telemetry Branch: Core Scan Subsystem Forward Lookup Check
            if (textLine.Contains("\"event\":\"Scan\"") || textLine.Contains("\"event\": \"Scan\""))
            {
                try
                {
                    var record = JsonConvert.DeserializeObject<EddnRecords>(textLine);
                    if (record?.Message != null && record.Message.EventName == "Scan")
                    {
                        #region Dynamic Axis Key Verification Fallback
                        if (record.Message.SemiMajorAxis == 0 && record.Message.Axis > 0)
                        {
                            record.Message.SemiMajorAxis = record.Message.Axis;
                        }
                        #endregion

                        #region Execute Trajectory Validation Check Against Pre-Baked Table
                        VerifyIncomingScanAgainstMasterModel(record);
                        #endregion
                    }
                }
                catch (JsonException ex)
                {
                    UpdateLogDisplay($"[INGESTION ERROR] Scan Serialization Fault: {ex.Message} | Raw: {textLine}");
                }
                return;
            }
            #endregion

            #region Telemetry Branch: Extensible Factory Travel & Spatial Anchors Ingestion. Call the factory. If line is not a valid timeline match, it safely outputs null.
            var timelineEvent = forensicParser.ParseTimelineLine(textLine, "Anonymous", (error, details) =>
                UpdateLogDisplay($"[INGESTION ERROR] {error}: {details}"));

            if (timelineEvent != null)
            {
                processedLinesCount++;

                if (timelineEvent != null)
                {
                    processedLinesCount++;

                    journeyBuffer.Add(timelineEvent);
                }
            }
            #endregion

        }
        private void VerifyIncomingScanAgainstMasterModel(EddnRecords record)
        {
            #region Property Extraction & Sanitization
            var msg = record.Message;
            string systemName = (!string.IsNullOrEmpty(msg.StarSystem) ? msg.StarSystem : "Unknown").Trim();
            string cleanedBodyName = (!string.IsNullOrEmpty(msg.BodyName) ? msg.BodyName : "Unknown").Trim();

            if (cleanedBodyName.StartsWith(systemName, StringComparison.OrdinalIgnoreCase))
            {
                cleanedBodyName = cleanedBodyName.Substring(systemName.Length).Trim();
            }

            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";
            #endregion

            #region Master Registry Presence Verification
            if (!masterRegistry.TryGetValue(basePlanetKey, out MasterOrbitAnchor anchor)) return;
            #endregion

            #region Parent Hierarchy Validation Gate (V1_09 Compliance)
            // Section 2.2: Evaluate parent nodes. If entity nests under a Barycenter/Moon, bypass calculation for now
            if (msg.Parents != null && msg.Parents.Length > 0)
            {
                // Check the first parent entry in the array matrix
                var primaryParent = msg.Parents[0];

                #region Future_Barycenter_Triangulation Placeholder Gate
                if (primaryParent.ContainsKey("Null"))
                {
                    // Future development hook: Secondary star / barycentric planet processing will go here
                    return; // Safe loop escape
                }
                #endregion

                #region Moon & Satellite Hierarchy Bypass
                if (primaryParent.ContainsKey("Planet"))
                {
                    // Section 2.2: Moon orbits are skipped to prevent localized spatial tracking noise
                    return;
                }
                #endregion
            }
            #endregion

            #region Evaluate Standard Planetary Trajectories
            KeplerOrbitSolver.OrbitalElements elements = new KeplerOrbitSolver.OrbitalElements
            {
                SemiMajorAxisMetres = anchor.SemiMajorAxis,
                Eccentricity = anchor.Eccentricity,
                OrbitalPeriodSeconds = anchor.OrbitalPeriod,
                AnchorTimestamp = anchor.AnchorTimestamp,
                AnchorDistanceLs = anchor.AnchorDistance,
                IsClimbingOutward = anchor.IsClimbingOutward
            };

            long currentTimestampSeconds = msg.Timestamp.ToUnixSeconds();
            double reportedDistance = msg.DistanceFromArrivalLS;
            double expectedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(elements, currentTimestampSeconds);

            double absoluteVarianceLs = Math.Abs(reportedDistance - expectedDistance);
            double calculatedErrorPercent = expectedDistance > 0 ? (absoluteVarianceLs / expectedDistance) * 100.0 : 0;
            #endregion

            #region Dual-Mode Threshold Evaluation
            bool breaksDistanceLimit = absoluteVarianceLs > 5.0;
            bool breaksPercentageLimit = calculatedErrorPercent > Settings.MaxAllowedErrorPercent;

            if (breaksDistanceLimit || breaksPercentageLimit)
            {
                foundOutliersCount++;
                UpdateLogDisplay($"🚨 [TIME-SLIP DETECTED] Body: {msg.BodyName} | Dev: {absoluteVarianceLs:F2} LS ({calculatedErrorPercent:F2}%)");

                #region Reverse Engineer Historical Ghost Timestamp
                long ghostTimestamp = 0;
                long? solvedGhostTime = KeplerOrbitSolver.SolveGhostTimestamp(elements, reportedDistance);
                if (solvedGhostTime.HasValue)
                {
                    ghostTimestamp = solvedGhostTime.Value;
                }
                #endregion

                LogAnomalyToFile(basePlanetKey, record, expectedDistance, anchor.AnchorTimestamp, ghostTimestamp);
            }
            #endregion
        }
        private void LoadRegistriesFromDisk()
        {
            #region Load registries from disk
            try
            {
                if (File.Exists(masterRegistryPath))
                {
                    string json = File.ReadAllText(masterRegistryPath);
                    masterRegistry = JsonConvert.DeserializeObject<Dictionary<string, MasterOrbitAnchor>>(json)
                                     ?? new Dictionary<string, MasterOrbitAnchor>();
                    UpdateLogDisplay($"Loaded {masterRegistry.Count} verified anchors from Master Registry.");
                }
                else
                {
                    UpdateLogDisplay("No existing Master Registry found. Starting fresh not sure what will happen.");
                }

            }
            #endregion
            #region Handle any exceptions that may occur during the registry loading process
            catch (Exception ex)
            {
                UpdateLogDisplay($"[WARNING] Failed initializing registry configuration files: {ex.Message}");
            }
            #endregion
        }
        private void LogAnomalyToFile(string registryKey, EddnRecords record, double expectedDistance, long previousValidTimestamp, long ghostTimestamp)
        {
            #region Lock the file access to ensure thread safety when writing anomalies to the report file
            lock (fileLock)
            {
                #region Prepare the anomaly payload for JSON serialization, including all relevant details about the detected anomaly
                try
                {
                    #region Calculate the variance and error percentage for the anomaly report
                    double reportedDistance = record.Message.DistanceFromArrivalLS;
                    double variance = Math.Abs(reportedDistance - expectedDistance);
                    double errorPercentage = (variance / expectedDistance) * 100.0;
                    DateTime previousValidDate = DateTimeOffset.FromUnixTimeSeconds(previousValidTimestamp).UtcDateTime;
                    #endregion

                    #region Format our mathematically resolved Ghost Date strings
                    string ghostDateString = "IMPOSSIBLE_ORBIT_GLITCH";
                    if (ghostTimestamp > 0)
                    {
                        ghostDateString = DateTimeOffset.FromUnixTimeSeconds(ghostTimestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ssZ");
                    }
                    #endregion

                    #region Prepare the anomaly payload for JSON serialization, including all relevant details about the detected anomaly
                    var anomalyPayload = new
                    {
                        DetectionTime = DateTime.UtcNow,
                        SystemKey = registryKey,
                        StarSystem = record.Message.StarSystem,
                        BodyName = record.Message.BodyName,
                        EventTimestamp = record.Message.Timestamp,

                        PreviousValidDateZulu = previousValidDate.ToString("yyyy-MM-dd HH:mm:ssZ"),

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
                    #endregion

                    #region Append the serialized anomaly payload to the anomalies report file, ensuring each entry is on a new line for easy parsing later
                    string jsonLine = JsonConvert.SerializeObject(anomalyPayload, Formatting.None) + Environment.NewLine;
                    File.AppendAllText(anomaliesReportPath, jsonLine);
                    #endregion
                }
                #endregion
                #region Handle any exceptions that may occur during the anomaly logging process, writing the error to the debug console for troubleshooting
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed recording anomaly row entry: {ex.Message}");
                }
                #endregion
            }
            #endregion
        }
        /// <summary>
        /// Connects to the online database archive, downloads the target date block log,
        /// and extracts every tracking movement marker belonging to a specific anomalous Commander.
        /// </summary>
        private List<JourneyTimelineEvent> FetchCommanderJourneyLocal(string targetUploaderId, List<JourneyTimelineEvent> journeyBuffer)
        {
            #region Guard Clauses Against Empty Memory Buffers
            if (journeyBuffer == null || journeyBuffer.Count == 0)
            {
                return new List<JourneyTimelineEvent>();
            }
            if (string.IsNullOrWhiteSpace(targetUploaderId))
            {
                return new List<JourneyTimelineEvent>();
            }
            #endregion

            #region In-Memory Sequence Filter and Ordering
            return journeyBuffer
                .Where(ev => ev.DetailInfo != null)
                .GroupBy(ev => ev.Timestamp)
                .Select(group => group.First())
                .OrderBy(ev => ev.Timestamp)
                .ToList();
            #endregion
        }
        #endregion

        #region UI Update Functions
        private void ResetUiState()
        {
            #region Re-enable the UI controls after the analysis is complete
            btnSelectFolder.Enabled = true;
            btnStartAnalysis.Enabled = true;
            #endregion
        }
        private void UpdateLogDisplay(string message)
        {
            #region Thread-safe invocation for updating the RichTextBox log
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
            #endregion
        }
        #endregion
    }
}