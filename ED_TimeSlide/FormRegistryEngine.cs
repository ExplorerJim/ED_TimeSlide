using SQLitePCL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class FormRegistryEngine : Form
    {
        #region Variables
        // Operational Metric Counters
        private int skippedSystemsCount = 0;
        private int processedLinesCount = 0;
        private string currentArchiveName = "";

        private BackgroundWorker preprocessingWorker;

        private readonly StagingParserEngine stagingParser = new StagingParserEngine();
        #endregion

        #region Instances
        private OrbitRegistry inst_OrbitRegistry;
        #endregion

        public FormRegistryEngine(OrbitRegistry orbitRegistry)
        {
            InitializeComponent();
            InitializePreprocessingWorker();

            inst_OrbitRegistry = orbitRegistry;

            //testing setup of ease
            //txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Raw Data\Scan\Test";
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
                .Select(f => new { Path = f, Date = EddnLogProcessor.ExtractDateFromFilename(Path.GetFileName(f)) ?? "9999-12-31" })
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

                #region Route File Based on Engine Processor
                EddnLogProcessor.ProcessDataFile(file.Path, line => EvaluateAndRouteLine(line));
                #endregion
            }
            #endregion
            #region Update Log Display with Summary for run
            UpdateLogDisplay("## Final Master Registry Summary #############################");
            UpdateLogDisplay($"Master registry has {inst_OrbitRegistry.GetMasterCount()} records");
            UpdateLogDisplay($"{inst_OrbitRegistry.GetStagingCount()} remain in staging record");
            UpdateLogDisplay($"Skipped {skippedSystemsCount} systems due to validation failures");
            #endregion
            #region Finalization
            e.Result = $"Initialization Complete. Swept {processedLinesCount} log entries.";
            #endregion
            #region Save registries to disk
            inst_OrbitRegistry.SaveRegistriesToDisk();
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

            #region Ingest and Route via Data Factory, Send the raw text to our factory. Errors will automatically pipe back to our log textbox.
            var record = stagingParser.ParseScanLine(textLine, (error, details) => UpdateLogDisplay($"[INGESTION ERROR] {error}: {details}"));

            if (record?.Message != null && record.Message.EventName == "Scan")
            {
                #region Section 3.3 Hierarchical Parent Validation Gate, Check if the body orbits the primary star directly before routing to staging memory
                if (stagingParser.IsPrimaryStarOrbiter(record.Message, (error, details) => UpdateLogDisplay($"[INGESTION ERROR] {error}: {details}")))
                {
                    EvaluateRecordAgainstStagingRegistry(record);
                }
                #endregion
            }
            #endregion
        }
        private void EvaluateRecordAgainstStagingRegistry(EddnRecords record)
        {
            #region Function Variables
            var msg = record.Message;
            #endregion

            #region Preliminary Filters
            if (msg.DistanceFromArrivalLS == 0) return; // Ignore primary body             
            #endregion

            #region Collect Key Variables
            string basePlanetKey = $"{msg.SystemAddress}_{msg.BodyId}";
            long currentTimestampUnixSeconds = msg.Timestamp.ToUnixSeconds();
            double currentDistance = msg.DistanceFromArrivalLS;
            string bodyName = msg.BodyName;
            string systemName = msg.StarSystem;
            #endregion

            #region Check for Existing Master Record
            if (inst_OrbitRegistry.ContainsMasterKey(basePlanetKey)) return;
            #endregion

            #region Generate Staging Registry Key
            string uploaderId = record.Header?.UploaderId ?? "Anonymous";
            string stagingLookupKey = $"{basePlanetKey}_{uploaderId}";
            #endregion       

            #region If no staging entry for this body add it to stageing
            if (!inst_OrbitRegistry.TryGetStagingBlock(stagingLookupKey, out StagingOrbitBlock stagingBlock))
            {
                 stagingBlock = new StagingOrbitBlock
                {
                    SemiMajorAxis = msg.SemiMajorAxis,
                    Eccentricity = msg.Eccentricity,
                    OrbitalPeriod = msg.OrbitalPeriod,
                    IsRetrograde = msg.RotationPeriod < 0 
                };
                inst_OrbitRegistry.CommitStagingBlock(stagingLookupKey, stagingBlock);
            }
            #endregion

            #region Source-Isolated 1-Second Unique Timestamp Gate
            // Verifies uniqueness by checking BOTH the timestamp and the uploader ID together
            if (stagingBlock.CollectedPoints != null && stagingBlock.CollectedPoints.Count > 0)
            {
                bool isNetworkDuplicateRow = false;
                for (int ptIdx = 0; ptIdx < stagingBlock.CollectedPoints.Count; ptIdx++)
                {
                    if (stagingBlock.CollectedPoints[ptIdx].TimestampUnixSec == currentTimestampUnixSeconds && string.Equals(stagingBlock.CollectedPoints[ptIdx].UploaderId, uploaderId, StringComparison.Ordinal))
                    {
                        isNetworkDuplicateRow = true;
                        break;
                    }
                }

                if (isNetworkDuplicateRow)
                {
                    // True network double-pump from the same client: execute early return bypass
                    return;
                }
            }
            #endregion

            #region Add Current Point to Staging Block
            stagingBlock.CollectedPoints.Add(new StagedPoint
            {
                TimestampUnixSec = currentTimestampUnixSeconds,
                Distance = currentDistance,
                SourceFile = currentArchiveName,
                BodyName = bodyName,
                SystemName = systemName,
                UploaderId = uploaderId
            });
            #endregion

            #region Check for Staging Block Completion and add to Master Registry if Validated
            if (stagingBlock.CollectedPoints.Count == 5)
            {
                if (stagingBlock.CollectedPoints[0].SystemName == "Ki")
                {

                }

                if (TryValidateStagingCluster(stagingBlock, out MasterOrbitAnchor verifiedAnchor, out _))
                {
                    verifiedAnchor.LastCheckedTimestampUnixSec = verifiedAnchor.AnchorTimestampUnixSec;
                    inst_OrbitRegistry.CommitMasterAnchor(basePlanetKey, verifiedAnchor);
                }
                else
                {
                    IncrementSkippedSystemsDiagnostics(record.Message.BodyName, "5 point cluster validation failed, more than 1 outlier");
                }
                inst_OrbitRegistry.RemoveStagingBlock(stagingLookupKey);
            }
            #endregion
        }
        private bool TryValidateStagingCluster(StagingOrbitBlock stagingBlock, out MasterOrbitAnchor verifiedAnchor, out List<StagedPoint> structuralOutliers)
        {
            if (stagingBlock.CollectedPoints != null && stagingBlock.CollectedPoints.Count > 1)
            {
                stagingBlock.CollectedPoints = stagingBlock.CollectedPoints
                    .OrderBy(pt => pt.TimestampUnixSec)
                    .ToList();
            }

            #region Function Variables
            verifiedAnchor = null;
            structuralOutliers = new List<StagedPoint>();
            bool isStellarBody = stagingBlock.OrbitalPeriod <= 0;
            #endregion

            #region RANSAC Consensus Pass Iteration Loop Matrix
            for (int anchorIndex = 0; anchorIndex < stagingBlock.CollectedPoints.Count; anchorIndex++)
            {
                var candidateAnchor = stagingBlock.CollectedPoints[anchorIndex];

                #region Version 1.32: Pre-Calculate Both Absolute Directional Phase Paths
                double m0Climbing = 0.0;
                double m0Falling = 0.0;

                if (!isStellarBody)
                {
                    double distM = candidateAnchor.Distance * Settings.SpeedOfLightMetersPerSecond;
                    double cosE = (1.0 - (distM / stagingBlock.SemiMajorAxis)) / stagingBlock.Eccentricity;
                    cosE = Math.Max(-1.0, Math.Min(1.0, cosE));

                    double eClimbing = Math.Acos(cosE);
                    double eFalling = (2.0 * Math.PI) - eClimbing;

                    double mClimbing = eClimbing - (stagingBlock.Eccentricity * Math.Sin(eClimbing));
                    double mFalling = eFalling - (stagingBlock.Eccentricity * Math.Sin(eFalling));

                    double meanMotion = (2.0 * Math.PI) / stagingBlock.OrbitalPeriod;
                    double elapsedSec = candidateAnchor.TimestampUnixSec;// - Settings.RealLifeSimulationLaunchEpochSeconds;

                    m0Climbing = mClimbing - (meanMotion * elapsedSec);
                    m0Climbing = m0Climbing % (2.0 * Math.PI);
                    if (m0Climbing < 0) m0Climbing += (2.0 * Math.PI);

                    m0Falling = mFalling - (meanMotion * elapsedSec);
                    m0Falling = m0Falling % (2.0 * Math.PI);
                    if (m0Falling < 0) m0Falling += (2.0 * Math.PI);
                }
                else
                {

                }
                #endregion

                #region Evaluate Both Orientations Separately to Isolate True Vector
                bool[] directionOptions = isStellarBody ? new bool[] { true } : new bool[] { true, false };

                foreach (bool evaluateAsClimbing in directionOptions)
                {
                    int agreementCount = 0;
                    var localOutliers = new List<StagedPoint>();
                    double targetedM0 = evaluateAsClimbing ? m0Climbing : m0Falling;

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
                            if (stellarVariance <= Settings.MaxStellarVarianceLs) pointMatchesBaseline = true;
                        }
                        else
                        {
                            #region Version 1.32: Direct Absolute Prediction Extraction Loop
                            KeplerOrbitSolver.OrbitalElements testElements = new KeplerOrbitSolver.OrbitalElements
                            {
                                SemiMajorAxisMetres = stagingBlock.SemiMajorAxis,
                                Eccentricity = stagingBlock.Eccentricity,
                                OrbitalPeriodSeconds = stagingBlock.OrbitalPeriod,
                                AnchorTimestampUnixSec = candidateAnchor.TimestampUnixSec,//Settings.RealLifeSimulationLaunchEpochSeconds,
                                AnchorDistanceLs = candidateAnchor.Distance,
                                IsClimbingOutward = evaluateAsClimbing,
                                IsRetrograde = stagingBlock.IsRetrograde
                            };

                            double predictedDistance = KeplerOrbitSolver.PredictDistanceAtTimestamp(testElements, pointToCheck.TimestampUnixSec);
                            double variance = Math.Abs(pointToCheck.Distance - predictedDistance);
                            double errorPercentage = predictedDistance > 0 ? (variance / predictedDistance) * 100.0 : 0;

                            if (errorPercentage <= Settings.MaxAllowedErrorPercent) pointMatchesBaseline = true;
                            #endregion
                        }

                        if (pointMatchesBaseline)
                            agreementCount++;
                        else
                            localOutliers.Add(pointToCheck);
                    }

                    #region Consensus Graduation Trigger Matrix
                    if (agreementCount >= 4)
                    {
                        verifiedAnchor = new MasterOrbitAnchor
                        {
                            SemiMajorAxis = stagingBlock.SemiMajorAxis,
                            Eccentricity = stagingBlock.Eccentricity,
                            OrbitalPeriod = stagingBlock.OrbitalPeriod,
                            AnchorTimestampUnixSec = candidateAnchor.TimestampUnixSec,
                            AnchorDistance = candidateAnchor.Distance,
                            SystemName = candidateAnchor.SystemName,
                            VerifiedSourceFile = candidateAnchor.SourceFile,
                            BodyName = candidateAnchor.BodyName,
                            IsClimbingOutward = !isStellarBody && evaluateAsClimbing,
                            MeanAnomalyAtUniversalEpoch = targetedM0
                        };

                        structuralOutliers = localOutliers;
                        return true;
                    }
                    #endregion
                }
                #endregion
            }
            return false;
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
            lock (Settings.ErrorLogPath)
            {
                File.AppendAllText(Settings.ErrorLogPath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Anchor: {identifier} | Failure: {failureCode}{Environment.NewLine}");
            }
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
