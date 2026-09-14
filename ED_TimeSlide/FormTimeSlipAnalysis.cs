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

        // ─── REGISTRY DATA STORES ───, Key format string: "StarSystemName_BodyID" (e.g., "Gondul_2")
        private Dictionary<string, MasterOrbitAnchor> masterRegistry = new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry = new Dictionary<string, StagingOrbitBlock>();

        // Storage paths for the local registry state
        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.MasterOrbitRegistryFileName);

        // Tracking variable to log which file is actively being scraped
        private string currentArchiveName = "";

        private readonly object fileLock = new object();
        private readonly string anomaliesReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.AnomaliesReportFileName);

        private BackgroundWorker analysisWorker;
        #endregion

        public FormTimeSlipAnalysis()
        {
            InitializeComponent();
            InitializeBackgroundWorker();

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
                LogMessage($"Selected folder: {folderBrowserDialogScan.SelectedPath}");
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
            LogMessage("Starting Single-Pass Ingestion Loop...");
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
                LogMessage($"Web Cache set to: {txtWebCachePath.Text}");
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
            #region Function Variables
            string folderPath = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;
            #endregion

            #region Reset counters for this run
            processedLinesCount = 0;
            foundOutliersCount = 0;
            #endregion
            
            #region Fetch file targets for all required extensions
            string[] rarFiles = Directory.GetFiles(folderPath, "*.rar");
            string[] bz2Files = Directory.GetFiles(folderPath, "*.bz2");
            string[] jsonlFiles = Directory.GetFiles(folderPath, "*.jsonl");
            int totalFiles = rarFiles.Length + bz2Files.Length + jsonlFiles.Length;
            #endregion

            #region Check if any files were found, return if not
            if (totalFiles == 0)
            {
                worker.ReportProgress(0, "Error: No .rar, .bz2, or .jsonl files found in the target directory.");
                return;
            }
            #endregion

            #region Report dynamic file metrics to the UI Log Console
            worker.ReportProgress(0, $"Found {rarFiles.Length} .rar archive(s).");
            worker.ReportProgress(0, $"Found {bz2Files.Length} .bz2 archive(s).");
            worker.ReportProgress(0, $"Found {jsonlFiles.Length} extracted .jsonl file(s).");
            worker.ReportProgress(0, $"--------------------------------------------------");
            #endregion

            #region Check for WinRAR engine presence if needed and avalible, return if not
            if ((rarFiles.Length > 0 || bz2Files.Length > 0) && !File.Exists(Settings.winRarExePath))
            {
                worker.ReportProgress(0, "Error: WinRAR engine not found at default location.");
                return;
            }
            #endregion

            #region Reset counters for this run
            processedLinesCount = 0;
            foundOutliersCount = 0;
            int currentFileIndex = 0;
            #endregion
            
            #region 1. Process standard .rar files
            foreach (string file in rarFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming RAR: {Path.GetFileName(file)}");
                StreamRarArchive(file, worker, percentage);
            }
            #endregion
           
            #region 2. Process .bz2 web data dumps using the same engine
            foreach (string file in bz2Files)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Streaming BZ2: {Path.GetFileName(file)}");
                StreamBz2Archive(file, worker, percentage);
            }
            #endregion
           
            #region 3. Process extracted uncompressed .jsonl files natively
            foreach (string file in jsonlFiles)
            {
                int percentage = (int)(((double)++currentFileIndex / totalFiles) * 100);
                currentArchiveName = Path.GetFileName(file);
                worker.ReportProgress(percentage, $"Reading JSONL: {Path.GetFileName(file)}");
                ReadJsonlLitFile(file);
            }
            #endregion
           
            #region Final Reporting
            e.Result = $"Phase 1 Complete. Swept {processedLinesCount} entries. Isolated {foundOutliersCount} timeline anomalies.";
            #endregion
        }
        private void AnalysisWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            #region Update the UI Progress Bar and Log Messages
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null)
            {
                LogMessage(e.UserState.ToString());
            }
            #endregion
        }
        private void AnalysisWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            #region Midway reporting to UI Log Console
            if (e.Result != null) LogMessage(e.Result.ToString());
            #endregion

            #region Check the anomalies report file and prepare for Phase 2
            string anomaliesReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DetectedAnomalies.json");
            string masterReportOutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FinalTimeSlipDossier.md");
            #endregion

            #region If no anomalies were found, log a message and reset the UI state
            if (!File.Exists(anomaliesReportPath))
            {
                LogMessage("Phase 2 Complete: No anomalies logged to track.");
                ResetUiState();
                return;
            }
            #endregion

            #region Update UI Log Console for Phase 2
            LogMessage("Starting Phase 2: Assembling Commander Flight Paths (Local Scan)...");
            #endregion

            #region Prepare the dossier output file, deleting any existing one to avoid appending to old data
            if (File.Exists(masterReportOutPath)) File.Delete(masterReportOutPath);

            string[] anomalyLines = File.ReadAllLines(anomaliesReportPath);
            HashSet<string> processedLookups = new HashSet<string>();
            int dossiersWritten = 0;
            #endregion

            #region Write Header to dossier Markdown file
            File.WriteAllText(masterReportOutPath, $"# 🚀 ELITE DANGEROUS TIME-SLIP INVESTIGATION DOSSIER{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath, $"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC{Environment.NewLine}");
            File.AppendAllText(masterReportOutPath, $"Scan Source: Local Files{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}");
            #endregion

            #region Iterate through each anomaly line
            foreach (string line in anomalyLines)
            {
                #region Skip empty lines to avoid processing errors
                if (string.IsNullOrWhiteSpace(line)) continue;
                #endregion

                try
                {
                    #region Deserialize the anomaly JSON line into a dynamic object to extract relevant fields
                    dynamic anomaly = JsonConvert.DeserializeObject(line);
                    string uploaderId = anomaly.UploaderID;
                    DateTime eventTime = anomaly.EventTimestamp;
                    string systemName = anomaly.StarSystem;
                    string bodyName = anomaly.BodyName;
                    string ghostDate = anomaly.GhostDateZulu;
                    double reportedDist = anomaly.ReportedDistance;
                    double expectedDist = anomaly.ExpectedDistance;
                    string sourceFile = anomaly.SourceArchive ?? "";
                    #endregion

                    #region Determine the target search date for local file lookup, defaulting to the event time if no file date is extracted
                    string targetSearchDate = eventTime.ToString("yyyy-MM-dd");
                    #endregion

                    #region Try to override with the File Date (Upload Date) if available
                    string fileDate = ExtractDateFromFilename(sourceFile);
                    if (!string.IsNullOrEmpty(fileDate))
                    {
                        targetSearchDate = fileDate;
                    }
                    #endregion

                    #region Check if this uploader/date combination has already been processed to avoid duplicate work
                    string lookupToken = $"{uploaderId}_{targetSearchDate}";
                    if (processedLookups.Contains(lookupToken)) continue;
                    processedLookups.Add(lookupToken);
                    #endregion

                    #region Log the assembly of the flight path anomaly for this specific body and date
                    LogMessage($"[LOCAL HUNT] Assembling flight path anomaly found in {bodyName} in files dated {targetSearchDate}...");
                    #endregion

                    #region Fetch the commander's journey timeline from local files based on the target date, uploader ID, and specified folders
                    List<JourneyTimelineEvent> journey = FetchCommanderJourneyLocal(
                        targetSearchDate,
                        uploaderId,
                        txtFolderPath.Text,
                        txtWebCachePath.Text,
                        sourceFile
                    );
                    #endregion

                    #region Increment the dossier counter for each processed anomaly
                    dossiersWritten++;
                    #endregion

                    #region Write the detailed anomaly report to the Markdown dossier file, including a chronological log of events and highlighting any detected anomalies
                    using (StreamWriter sw = File.AppendText(masterReportOutPath))
                    {
                        #region Write the header and summary information for this anomaly
                        sw.WriteLine($"## 🛰️ Anomaly Target: {bodyName}");
                        sw.WriteLine($"- **Detection Timestamp:** `{eventTime:yyyy-MM-dd HH:mm:ss} UTC`");
                        sw.WriteLine($"- **Ghost Target Date:** `{ghostDate}`");
                        sw.WriteLine($"- **Commander Token:** `{uploaderId}`");
                        sw.WriteLine();
                        sw.WriteLine("### 📅 Chronological Flight Timeline Log");
                        #endregion

                        #region Handle case where no journey events were found for this commander on the specified date
                        if (journey.Count == 0)
                        {
                            sw.WriteLine("> *No event history found in local files for this commander on this day.*");
                        }
                        #endregion
                        #region Iterate through the journey events and write them to the Markdown file, highlighting any anomalies detected based on the defined criteria
                        else
                        {
                            foreach (var ev in journey)
                            {
                                #region ANOMALY MATCHING LOGIC: We flag it if it's a SCAN event, for the right BODY, within 5 seconds of the log time
                                bool isTheAnomaly = (ev.EventType == "Scan") && (ev.BodyName == bodyName) && Math.Abs((ev.Timestamp - eventTime).TotalSeconds) < 5;

                                string timestamp = $"`{ev.Timestamp:HH:mm:ss}`";
                                string evtType = $"**{ev.EventType}**";
                                string info = $"System Body: *{ev.BodyName}* {ev.DetailInfo}";

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
                                #endregion
                            }
                        }
                        #endregion

                        #region Write a separator line to clearly delineate between different anomaly reports in the Markdown file
                        sw.WriteLine();
                        sw.WriteLine("---");
                        sw.WriteLine();
                        #endregion
                    }
                    #endregion
                }
                #region Fail silently on a single bad line so the report finishes, logging the error to the debug console
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Report Gen Error: {ex.Message}");
                }
                #endregion
            }
            #endregion

            #region Final Reporting to the UI Log Console
            LogMessage("==================================================");
            LogMessage($"PHASE 2 SUCCESS: Compiled {dossiersWritten} Investigation Records!");
            LogMessage($"Markdown File Saved to: {masterReportOutPath}");
            LogMessage("==================================================");
            #endregion

            #region Reset the UI state to allow for another analysis run 
            ResetUiState();
            #endregion
        }
        #endregion

        #region Private Functions
        private void StreamRarArchive(string rarPath, BackgroundWorker worker, int currentProgress)
        {
            #region Setup the ProcessStartInfo to invoke WinRAR for streaming the contents of the RAR archive
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Settings.winRarExePath,
                Arguments = $"p -inul \"{rarPath}\"", // Print file contents directly to stdout stream
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            ExecuteStreamReaderProcess(startInfo, rarPath, worker, currentProgress);
            #endregion
        }
        private void StreamBz2Archive(string bz2Path, BackgroundWorker worker, int currentProgress)
        {
            #region Setup the ProcessStartInfo to invoke WinRAR for streaming the contents of the BZ2 archive
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Settings.winRarExePath,
                Arguments = $"e -so -inul \"{bz2Path}\"", // 'e -so' extracts any compressed archive to stdout
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            ExecuteStreamReaderProcess(startInfo, bz2Path, worker, currentProgress);
            #endregion
        }
        private void ExecuteStreamReaderProcess(ProcessStartInfo startInfo, string filePath, BackgroundWorker worker, int progress)
        {
            #region Use a try-catch block to handle any exceptions that may occur during the process execution
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
            #endregion
        }
        private void ReadJsonlLitFile(string jsonlPath)
        {
            #region Read the JSONL file line by line
            using (StreamReader reader = new StreamReader(jsonlPath))
            {
                string textLine;
                while ((textLine = reader.ReadLine()) != null)
                {
                    processedLinesCount++;
                    EvaluateAndRouteLine(textLine);
                }
            }
            #endregion
        }
        private void EvaluateAndRouteLine(string textLine)
        {
            #region Skip empty or whitespace lines to avoid unnecessary processing
            if (string.IsNullOrWhiteSpace(textLine)) return;
            #endregion
            #region Filter for Scan events only, ignoring other event types to focus on relevant data
            if (!textLine.Contains("\"event\":\"Scan\"") && !textLine.Contains("\"event\": \"Scan\"")) return;
            #endregion
            #region Deserialize the JSON line and evaluate it against the registry
            try
            {
                var record = JsonConvert.DeserializeObject<EddnRecords>(textLine);
                if (record?.Message != null && record.Message.EventName == "Scan")
                {
                    // REMOVED: The StarType filter is gone! Stars pass through cleanly now.
                    EvaluateRecordAgainstRegistry(record);
                }
            }
            catch (JsonException) { }
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
                    LogMessage($"Loaded {masterRegistry.Count} verified anchors from Master Registry.");
                }
                else
                {
                    LogMessage("No existing Master Registry found. Starting fresh not sure what will happen.");
                }

            }
            #endregion
            #region Handle any exceptions that may occur during the registry loading process
            catch (Exception ex)
            {
                LogMessage($"[WARNING] Failed initializing registry configuration files: {ex.Message}");
            }
            #endregion
        }
        private void EvaluateRecordAgainstRegistry(EddnRecords record)
        {
            #region Function Variables
            var msg = record.Message;
            #endregion

            #region Preliminary Filters            
            if (msg.BodyId == 0) return; // Ignore primary suns            
            if (msg.BodyName.Contains("Belt Cluster") || msg.BodyName.Contains("Ring Cluster")) return; // Ignore belt and ring clusters, as they are not valid orbital bodies
            // DISTANCE GATE FILTER: Turn off secondary star noise and distant binary if the body sits close to the arrivalpoint
            if (msg.StarType != null && msg.DistanceFromArrivalLS <= Settings.MinStellarDistanceForStars) return;
            #endregion

            #region Create a unique composite lookup key for the specific planet or star body
            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";
            #endregion

            #region Extract the current timestamp and distance from the message for further calculations
            long currentTimestampSeconds = msg.Timestamp.ToUnixSeconds();
            double currentDistance = msg.DistanceFromArrivalLS;
            #endregion

            #region SCENARIO A: The orbital truth model has already been established globally
            if (masterRegistry.TryGetValue(basePlanetKey, out MasterOrbitAnchor anchor))
            {
                #region Initialize the predicted distance variable for the orbital calculation
                double predictedDistance = 0;
                #endregion

                #region Check if this body is a star or lacks an orbital loop cadence
                if (!string.IsNullOrEmpty(msg.StarType) || anchor.OrbitalPeriod <= 0)
                {
                    // STELLAR BODY RULE: Stars are physically fixed anchors relative to the system frame.
                    // Their expected coordinate is simply their baseline verified distance!
                    predictedDistance = anchor.AnchorDistance;
                }
                #endregion
                #region Handle the case for standard planets with defined orbital parameters
                else
                {
                    // STANDARD PLANET RULE: Run the full continuous Kepler orbit vector calculation
                    KeplerOrbitSolver.OrbitalElements orbitalInput = new KeplerOrbitSolver.OrbitalElements
                    {
                        SemiMajorAxisMetres = anchor.SemiMajorAxis,
                        Eccentricity = anchor.Eccentricity,
                        OrbitalPeriodSeconds = anchor.OrbitalPeriod,
                        AnchorTimestamp = anchor.AnchorTimestamp,
                        AnchorDistanceLs = anchor.AnchorDistance,
                        IsClimbingOutward = anchor.IsClimbingOutward
                    };
                    predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(orbitalInput, currentTimestampSeconds);
                }
                #endregion
                #region Calculate the variance and error percentage between the current reported distance and the predicted distance
                double variance = Math.Abs(currentDistance - predictedDistance);
                double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;
                #endregion
                #region THE HYBRID ANOMALY GATEWAY:
                // Flag if a planet drifts (> 2.0%) OR if ANY body (star/planet) shifts by more than 5.0 Light Seconds!
                if (errorPercentage > Settings.MaxAllowedErrorPercent || variance > Settings.MaxStellarVarianceLs)
                {
                    #region Increment the outlier counter and initialize the ghost timestamp variable for potential reverse-Kepler solving
                    foundOutliersCount++;
                    long ghostTimeOut = 0;
                    #endregion
                    #region Only attempt reverse-Kepler solving if it's a planet with moving orbit properties
                    if (string.IsNullOrEmpty(msg.StarType) && anchor.OrbitalPeriod > 0)
                    {
                        KeplerOrbitSolver.OrbitalElements orbitalInput = new KeplerOrbitSolver.OrbitalElements
                        {
                            SemiMajorAxisMetres = anchor.SemiMajorAxis,
                            Eccentricity = anchor.Eccentricity,
                            OrbitalPeriodSeconds = anchor.OrbitalPeriod,
                            AnchorTimestamp = anchor.AnchorTimestamp,
                            AnchorDistanceLs = anchor.AnchorDistance,
                            IsClimbingOutward = anchor.IsClimbingOutward
                        };
                        long? calculatedGhostTimestamp = KeplerOrbitSolver.SolveGhostTimestamp(orbitalInput, currentDistance);
                        ghostTimeOut = calculatedGhostTimestamp ?? 0;
                    }
                    #endregion

                    #region Write the anomaly out to your file report
                    LogAnomalyToFile(basePlanetKey, record, predictedDistance, anchor.LastCheckedTimestamp, ghostTimeOut);
                    #endregion
                }
                #endregion
                #region Update the last checked timestamp for the anchor if no anomaly was detected
                else
                {
                    anchor.LastCheckedTimestamp = currentTimestampSeconds;
                }
                #endregion
            }
            #endregion
            #region SCENARIO B: The body is not in the master registry
            else
            {
                //Ignore for now
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
        private List<JourneyTimelineEvent> FetchCommanderJourneyLocal(string targetDateYyyyMmDd, string targetUploaderId, string scanFolder, string cacheFolder, string specificSourceFile = "")
        {
            #region Initialize the timeline list and a HashSet to store unique file paths for processing
            var timeline = new List<JourneyTimelineEvent>();
            var filesToProcess = new HashSet<string>(); // Use HashSet to avoid duplicates automatically
            #endregion

            #region 1. Identify which folders to search
            var searchPaths = new List<string>();

            if (Directory.Exists(scanFolder)) searchPaths.Add(scanFolder);

            #region Only add cache folder if it exists and is different from scan folder
            if (!string.IsNullOrWhiteSpace(cacheFolder) && Directory.Exists(cacheFolder)
                && !searchPaths.Contains(cacheFolder))
            {
                searchPaths.Add(cacheFolder);
            }
            #endregion
            #endregion

            #region 2. Gather ALL matching files from ALL locations
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
            #endregion

            #region 3. FORCE INCLUDE THE SOURCE FILE (if valid path provided)
            if (!string.IsNullOrEmpty(specificSourceFile) && File.Exists(specificSourceFile))
            {
                filesToProcess.Add(specificSourceFile);
            }
            #region Handle case where specificSourceFile is just a filename (e.g. "Journal.Scan...") inside scanFolder
            else if (!string.IsNullOrEmpty(specificSourceFile) && Directory.Exists(scanFolder))
            {
                string potentialPath = Path.Combine(scanFolder, Path.GetFileName(specificSourceFile));
                if (File.Exists(potentialPath)) filesToProcess.Add(potentialPath);
            }
            #endregion
            System.Diagnostics.Debug.WriteLine($"[LOCAL HUNT] Found {filesToProcess.Count} total files for {targetDateYyyyMmDd} across {searchPaths.Count} folders.");
            #endregion

            #region 4. Process the Aggregated List
            foreach (string filePath in filesToProcess)
            {
                #region Initialize variables for file processing
                string[] filesToRead = new string[] { };
                string tempDir = "";
                bool isArchive = false;
                #endregion

                #region Try-Catch block to handle any exceptions during file processing
                try
                {
                    #region CASE A: Raw Text File
                    if (filePath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                    {
                        filesToRead = new string[] { filePath };
                    }
                    #endregion
                    #region CASE B: Archive -> Extract to Temp
                    else
                    {
                        isArchive = true;
                        tempDir = Path.Combine(Path.GetTempPath(), $"ED_Extract_{Guid.NewGuid()}");
                        Directory.CreateDirectory(tempDir);

                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = Settings.winRarExePath,
                            Arguments = $"e -y -ibck \"{filePath}\" \"{tempDir}\\\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (Process p = Process.Start(startInfo))
                        {
                            p.WaitForExit();
                        }

                        if (Directory.Exists(tempDir))
                        {
                            filesToRead = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                        }
                    }
                    #endregion

                    #region 5. READ DATA
                    foreach (string file in filesToRead)
                    {
                        #region Initialize file processing variables
                        if (new FileInfo(file).Length == 0) continue;
                        #endregion
                        #region Read each line of the file and filter for the target uploader ID, deserializing relevant events into the timeline
                        foreach (string line in File.ReadLines(file))
                        {
                            #region Skip lines that do not contain the target uploader ID to reduce unnecessary processing
                            if (!line.Contains(targetUploaderId)) continue;
                            #endregion
                            #region try-catch block to handle potential JSON deserialization errors for each line
                            try
                            {
                                #region Deserialize the line into an EddnGenericRecord object to access its message and other properties
                                var record = JsonConvert.DeserializeObject<EddnGenericRecord>(line);
                                #endregion
                                #region If the record has a valid message, filter for relevant events and add them to the timeline
                                if (record?.Message != null)
                                {
                                    #region Filter for relevant events and add them to the timeline
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
                                    else
                                    {
                                        string resolvedSystem = record.Message.StarSystem ?? record.Message.System ?? "Unknown";
                                        string resolvedBody = record.Message.BodyName ?? record.Message.Body ?? "";
                                        string details = record.Message.StationName ?? record.Message.Name ?? "";

                                        timeline.Add(new JourneyTimelineEvent
                                        {
                                            Timestamp = record.Message.Timestamp,
                                            EventType = "Unknown",
                                            StarSystem = resolvedSystem,
                                            BodyName = resolvedBody,
                                            DetailInfo = details
                                        });
                                    }
                                    #endregion
                                }
                                else
                                {

                                }
                                #endregion
                            }
                            catch { }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion
                #region Handle any exceptions that may occur during the file processing, logging the error to the debug console for troubleshooting
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing {filePath}: {ex.Message}");
                }
                #endregion
                #region Cleanup: Delete the temporary directory if it was created for archive extraction
                finally
                {
                    if (isArchive && !string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                    {
                        try { Directory.Delete(tempDir, true); } catch { }
                    }
                }
                #endregion
            }
            #endregion

            #region 6. Sort & Unique
            return timeline.GroupBy(x => x.Timestamp)
                           .Select(g => g.First())
                           .OrderBy(x => x.Timestamp)
                           .ToList();
            #endregion
        }
        private string ExtractDateFromFilename(string filename)
        {
            #region Looks for a pattern like "2025-10-29" inside any string
            var match = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
            if (match.Success)
            {
                return match.Value;
            }
            return null;
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
        private void LogMessage(string message)
        {
            #region Thread-safe invocation for updating the RichTextBox log
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
            #endregion
        }
        #endregion
    }
}