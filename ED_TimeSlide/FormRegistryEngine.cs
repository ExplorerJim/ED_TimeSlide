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
        #region Variables
        // Core Data Models
        private Dictionary<string, MasterOrbitAnchor> masterRegistry =
            new Dictionary<string, MasterOrbitAnchor>();
        private Dictionary<string, StagingOrbitBlock> stagingRegistry =
            new Dictionary<string, StagingOrbitBlock>();

        // System Path Identifiers
        private readonly string masterRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.MasterOrbitRegistryFileName);
        private readonly string stagingRegistryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.StagingOrbitRegistryFileName);
        private readonly string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.ErrorLogFileName);

        // Operational Metric Counters
        private int skippedSystemsCount = 0;
        private int processedLinesCount = 0;
        private string currentArchiveName = "";

        private BackgroundWorker preprocessingWorker;
        #endregion

        public FormRegistryEngine()
        {
            InitializeComponent();
            InitializePreprocessingWorker();
            LoadExistingRegistries();

            //testing setup of ease
            txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test";
            btnStartAnalysis.Enabled = true;
        }

        #region Form Events
        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            #region Open Folder Browser Dialog
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                #region Set Directory to be used by later processes
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                    btnStartAnalysis.Enabled = true;
                    UpdateLogDisplay($"Selected Target: {fbd.SelectedPath}");
                }
                #endregion
            }
            #endregion
        }
        private void BtnStartAnalysis_Click(object sender, EventArgs e)
        {
            #region Disable Buttons to Prevent Re-Entry
            btnStartAnalysis.Enabled = false;
            btnSelectFolder.Enabled = false;
            #endregion
            #region Reset Counters and Progress Bar
            skippedSystemsCount = 0;
            processedLinesCount = 0;
            txbSkippedSystemsCounter.Text = "0";
            progressBarFiles.Value = 0;
            #endregion
            #region Delete Existing Error Log
            if (File.Exists(errorLogPath)) File.Delete(errorLogPath);
            #endregion
            #region Get Target Folder Path
            string targetFolder = txtFolderPath.Text;
            #endregion
            #region Update Log Display
            UpdateLogDisplay("Initializing Chronological Preprocessing...");
            #endregion
            #region Start Background Worker
            preprocessingWorker.RunWorkerAsync(targetFolder);
            #endregion
        }
        #endregion

        #region Background Worker Functions
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
        private void PreprocessingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            #region Function Variables
            string folderPath = (string)e.Argument;
            BackgroundWorker worker = sender as BackgroundWorker;
            #endregion

            #region Get all files
            var allFiles = Directory.GetFiles(folderPath, "*.*")
                .Where(f => f.EndsWith(".rar") || f.EndsWith(".bz2") || f.EndsWith(".jsonl"))
                .Select(f => new { Path = f, Date = ExtractDateFromFilename(Path.GetFileName(f)) })
                .OrderBy(f => f.Date)
                .ToList();
            #endregion

            #region Check for empty directory
            if (allFiles.Count == 0)
            {
                worker.ReportProgress(0, "Error: No valid log archives discovered.");
                return;
            }
            #endregion

            #region Process each file
            int totalFiles = allFiles.Count;
            for (int i = 0; i < totalFiles; i++)
            {
                #region Update Current Archive Name and get file path
                var file = allFiles[i];
                currentArchiveName = Path.GetFileName(file.Path);
                #endregion
                #region Update Progress
                int percentage = (int)(((double)(i + 1) / totalFiles) * 100);
                worker.ReportProgress(percentage, $"Ingesting [{i + 1}/{totalFiles}]: {currentArchiveName}");
                #endregion

                #region Route File Based on Extension
                if (file.Path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                {
                    ReadJsonlFileNative(file.Path);
                }
                else
                {
                    StreamCompressedArchive(file.Path);
                }
                #endregion

                #region Update Log Display with Summary for file
                UpdateLogDisplay($"Master registry has {masterRegistry.Count} records");
                UpdateLogDisplay($"{stagingRegistry.Count} remain in staging record");
                UpdateLogDisplay($"Skipped {skippedSystemsCount} systems due to validation failures");
                #endregion
            }
            #endregion
            #region Update Log Display with Summary for run
            UpdateLogDisplay("## Final Master Registry Summary #############################");
            UpdateLogDisplay($"Final Master registry has {masterRegistry.Count} records");
            UpdateLogDisplay($"{stagingRegistry.Count} remain in Final staging record");
            UpdateLogDisplay($"Skipped {skippedSystemsCount} systems due to validation failures in total");
            #endregion
            #region Finalization
            e.Result = $"Initialization Complete. Swept {processedLinesCount} log entries.";
            #endregion
        }
        private void PreprocessingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            #region Update UI
            progressBarFiles.Value = e.ProgressPercentage;
            if (e.UserState != null) UpdateLogDisplay(e.UserState.ToString());
            #endregion
        }
        private void PreprocessingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            #region Save registries to disk
            SaveRegistriesToDisk();
            #endregion

            #region Update UI
            if (e.Result != null) 
                UpdateLogDisplay(e.Result.ToString());
            UpdateLogDisplay("Pass 1 Calibration Complete. Tables baked securely to disk.");
            btnSelectFolder.Enabled = true;
            btnStartAnalysis.Enabled = true;
            #endregion
        }
        #endregion

        #region Private Functions
        private void EvaluateAndRouteLine(string textLine)
        {
            #region Preliminary Filters
            if (string.IsNullOrWhiteSpace(textLine)) return;
            if (!textLine.Contains("\"event\":\"Scan\"") && !textLine.Contains("\"event\": \"Scan\"")) return;
            #endregion

            #region Deserialize and Evaluate line
            try
            {
                #region Deserialize JSON line
                var record = JsonConvert.DeserializeObject<EddnRecords>(textLine);
                #endregion

                #region Evaluate Scan Records
                if (record?.Message != null && record.Message.EventName == "Scan")
                {
                    EvaluateRecordAgainstStagingRegistry(record);
                }
                #endregion
                #region Ignore Non-Scan Records
                else { }
                #endregion
            }
            catch (JsonException) { }
            #endregion
        }
        private void EvaluateRecordAgainstStagingRegistry(EddnRecords record)
        {
            #region Variables
            var msg = record.Message;
            #endregion

            #region Preliminary Filters
            if (msg.BodyId == 0) return; // Ignore primary suns            
            if (msg.BodyName.Contains("Belt Cluster")) return; // Ignore belt clusters, as they are not valid orbital bodies
            if (msg.BodyName.Contains("Ring Cluster")) return; // Ignore ring clusters, as they are not valid orbital bodies
            // DISTANCE GATE FILTER: Turn off secondary star noise and distant binary if the body sits close to the arrivalpoint 
            if (msg.StarType != null && msg.DistanceFromArrivalLS <= Settings.MinStellarDistanceForStars) return;
            #endregion

            #region Collect Key Variables
            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";
            long currentTimestampSeconds = msg.Timestamp.ToUnixSeconds();
            double currentDistance = msg.DistanceFromArrivalLS;
            #endregion

            #region Check for Existing Master Record
            if (masterRegistry.ContainsKey(basePlanetKey)) return;
            #endregion

            #region Generate Staging Registry Key
            string uploaderId = record.Header?.UploaderId ?? "Anonymous";
            string stagingLookupKey = $"{basePlanetKey}_{uploaderId}";
            #endregion

            #region Directly Promote Stellar Bodies to Master Registry
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
            #endregion

            #region Get Staging Block or Create New
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
            #endregion

            #region Add Current Point to Staging Block
            stagingBlock.CollectedPoints.Add(new StagedPoint
            {
                Timestamp = currentTimestampSeconds,
                Distance = currentDistance,
                SourceFile = currentArchiveName,
                SoftwareName = record.Header?.SoftwareName ?? "UnknownTool",
                UploaderId = uploaderId
            });
            #endregion

            #region Check for Staging Block Completion and add to Master Registry if Validated
            if (stagingBlock.CollectedPoints.Count == 5)
            {
                if (TryValidateStagingCluster(stagingBlock, out MasterOrbitAnchor verifiedAnchor, out _))
                {
                    verifiedAnchor.LastCheckedTimestamp = verifiedAnchor.AnchorTimestamp;
                    masterRegistry[basePlanetKey] = verifiedAnchor;
                }
                else
                {
                    IncrementSkippedSystemsDiagnostics(record.Message.BodyName, "5 point cluster validation failed, more than 1 outlier");
                }
                stagingRegistry.Remove(stagingLookupKey);
            }
            #endregion
        }
        private bool TryValidateStagingCluster(StagingOrbitBlock stagingBlock, out MasterOrbitAnchor verifiedAnchor, out List<StagedPoint> structuralOutliers)
        {
            #region Function Variables
            verifiedAnchor = null;
            structuralOutliers = new List<StagedPoint>();

            var firstPt = stagingBlock.CollectedPoints[0];
            var lastPt = stagingBlock.CollectedPoints[stagingBlock.CollectedPoints.Count - 1];

            bool dynamicIsClimbing = lastPt.Distance >= firstPt.Distance;
            bool isStellarBody = stagingBlock.OrbitalPeriod <= 0;
            #endregion

            #region Iterate Through Each Point as Candidate Anchor
            for (int anchorIndex = 0; anchorIndex < stagingBlock.CollectedPoints.Count; anchorIndex++)
            {
                #region Initialize Candidate Anchor and Outliers
                var candidateAnchor = stagingBlock.CollectedPoints[anchorIndex];
                int agreementCount = 0;
                var localOutliers = new List<StagedPoint>();
                #endregion

                #region Check Each Point 
                for (int checkIndex = 0; checkIndex < stagingBlock.CollectedPoints.Count; checkIndex++)
                {
                    #region Skip Self-Comparison
                    if (anchorIndex == checkIndex)
                    {
                        agreementCount++;
                        continue;
                    }
                    #endregion

                    #region Calculate Variance
                    var pointToCheck = stagingBlock.CollectedPoints[checkIndex];
                    bool pointMatchesBaseline = false;
                    #endregion

                    #region If Stellar Body, Check Variance Directly
                    if (isStellarBody)
                    {
                        double stellarVariance = Math.Abs(pointToCheck.Distance - candidateAnchor.Distance);
                        if (stellarVariance <= Settings.MaxStellarVarianceLs) pointMatchesBaseline = true;
                    }
                    #endregion

                    #region If Planetary Body, Use Kepler Solver to Predict Distance
                    else
                    {
                        KeplerOrbitSolver.OrbitalElements orbitalElements = new KeplerOrbitSolver.OrbitalElements
                        {
                            SemiMajorAxisMetres = stagingBlock.SemiMajorAxis,
                            Eccentricity = stagingBlock.Eccentricity,
                            OrbitalPeriodSeconds = stagingBlock.OrbitalPeriod,
                            AnchorTimestamp = candidateAnchor.Timestamp,
                            AnchorDistanceLs = candidateAnchor.Distance,
                            IsClimbingOutward = dynamicIsClimbing
                        };
                        double predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(orbitalElements, pointToCheck.Timestamp);

                        double variance = Math.Abs(pointToCheck.Distance - predictedDistance);
                        double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;

                        if (errorPercentage <= Settings.MaxAllowedErrorPercent) pointMatchesBaseline = true;
                    }
                    #endregion

                    #region If Point Matches Baseline, Increment Agreement Count; Otherwise, Add to Local Outliers
                    if (pointMatchesBaseline) 
                        agreementCount++;
                    else
                        localOutliers.Add(pointToCheck);
                    #endregion
                }
                #endregion

                #region If Enough Points Agree, Promote to Master Registry
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
                #endregion
            }
            return false;
            #endregion
        }
        private void SaveRegistriesToDisk()
        {
            #region Try to Serialize Registries
            try
            {
                #region Serialize Master Registry to Disk
                using (StreamWriter sw = new StreamWriter(masterRegistryPath, false))
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                    serializer.Serialize(jw, masterRegistry);
                }
                #endregion
                #region Serialize Staging Registry to Disk
                using (StreamWriter sw = new StreamWriter(stagingRegistryPath, false))
                using (JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.None };
                    serializer.Serialize(jw, stagingRegistry);
                }
                #endregion
            }
            #endregion
            #region Catch Serialization Exceptions
            catch (Exception ex)
            {
                UpdateLogDisplay($"[ERROR] Serialization crash: {ex.Message}");
            }
            #endregion
        }
        private void LoadExistingRegistries()
        {
            #region Load Master Registry from Disk
            if (File.Exists(masterRegistryPath))
            {
                string json = File.ReadAllText(masterRegistryPath);
                masterRegistry = JsonConvert.DeserializeObject<Dictionary<string, MasterOrbitAnchor>>(json)
                                 ?? new Dictionary<string, MasterOrbitAnchor>();
            }
            #endregion
            #region Load Staging Registry from Disk
            if (File.Exists(stagingRegistryPath))
            {
                string json = File.ReadAllText(stagingRegistryPath);
                stagingRegistry = JsonConvert.DeserializeObject<Dictionary<string, StagingOrbitBlock>>(json)
                                  ?? new Dictionary<string, StagingOrbitBlock>();
            }
            #endregion
        }
        private void IncrementSkippedSystemsDiagnostics(string identifier, string failureCode)
        {
            #region Increment Counter
            skippedSystemsCount++;
            #endregion

            #region Update UI
            if (txbSkippedSystemsCounter.InvokeRequired)
            {
                txbSkippedSystemsCounter.Invoke(new Action(() => txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString()));
            }
            else
            {
                txbSkippedSystemsCounter.Text = skippedSystemsCount.ToString();
            }
            #endregion

            #region Log Error to Disk
            lock (masterRegistryPath)
            {
                File.AppendAllText(errorLogPath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
            #endregion
        }
        private void StreamCompressedArchive(string path)
        {
            #region Function Variables
            string tempDir = "";
            string[] filesToRead = new string[] { };
            #endregion

            #region Extract Compressed Archive Using WinRAR
            try
            {
                #region Create Temporary Directory
                tempDir = Path.Combine(Path.GetTempPath(), $"ED_Extract_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);
                #endregion
                #region Configure Process Start Info for WinRAR
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Settings.winRarExePath,
                    Arguments = $"e -y -ibck \"{path}\" \"{tempDir}\\\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                #endregion
                #region Update Log Display
                UpdateLogDisplay("Extracting archive: " + Path.GetFileName(path));
                #endregion
                #region Start WinRAR Process and Wait for Completion
                using (Process p = Process.Start(startInfo))
                {
                    p.WaitForExit();
                }
                #endregion
                #region Check for Extracted Files
                if (Directory.Exists(tempDir))
                {
                    filesToRead= Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                    UpdateLogDisplay("Extracted files: " + filesToRead.Length);
                }
                else
                {
                    UpdateLogDisplay("Failed to extract archive: " + Path.GetFileName(path));
                    return;
                }
                #endregion
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing {path}: {ex.Message}");
            }
            #endregion

            #region Update Log Display
            UpdateLogDisplay("Starting to process extracted files...");
            #endregion

            #region Read Each Line from Extracted Files and Evaluate
            try
            {
                foreach (string file in filesToRead)
                {
                    if (new FileInfo(file).Length == 0) continue;

                    foreach (string line in File.ReadLines(file))
                    {
                        processedLinesCount++;
                        EvaluateAndRouteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateLogDisplay($"Error processing {path}: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
                {
                    try 
                    { 
                        Directory.Delete(tempDir, true);
                        UpdateLogDisplay("Temporary directory deleted.");
                    } 
                    catch 
                    {
                        UpdateLogDisplay("Failed to delete temporary directory: " + tempDir);
                    }
                }
            }
            #endregion
        }
        private void ReadJsonlFileNative(string path)
        {
            #region Read Each Line from the JSONL File
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    processedLinesCount++;
                    EvaluateAndRouteLine(line);
                }
            }
            #endregion
        }
        private string ExtractDateFromFilename(string filename)
        {
            #region Use Regex to Extract Date
            var match = Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
            return match.Success ? match.Value : "9999-12-31";
            #endregion
        }
        #endregion

        #region UI Update Functions
        private void UpdateLogDisplay(string message)
        {
            #region Check for Invoke Required
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(UpdateLogDisplay), message);
            }
            #endregion 
            #region Directly Update Log Display
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
