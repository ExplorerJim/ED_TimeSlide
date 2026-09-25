using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static ED_TimeSlide.KeplerOrbitSolver;


namespace ED_TimeSlide.Engine
{
    public class ScanDBPhase2
    {
        #region Structures
        public struct OptimizerTarget
        {
            public int BodyDBID;
            public string SystemName;
            public string BodyName;
            public double SemiMajorAxis;
            public double Eccentricity;
            public double OrbitalPeriodSec;
            public bool IsRetrograde;
        }

        public struct TelemetryPoint
        {
            public long TimestampUnixSec;
            public double DistanceToArrivalLs;
        }
        #endregion

        #region Variables
        private readonly string _connectionString;
        private readonly object _logFileLock = new object();    
        private int _totalTargetsChecked = 0;
        private int _successfullyGraduatedCount = 0;
        private int _skippedLowDensityCount = 0;
        private int _skippedDivergentCount = 0;
        #endregion

        public ScanDBPhase2(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};";
        }
        public void ExecuteGlobalOptimizationPass(Action<string, int, int> loggerCallback)
        {
            // Dynamically generate default rows for missing abstract barycenters before checking targets
            InitializeMissingBarycenterRows();

            // Setup single thread debugging configuration safely
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };

            // =================================================================================
            // PASS 1: CORE SYSTEM CALIBRATION (Targets: Star A & Star B)
            // =================================================================================
            List<OptimizerTarget> pass1Targets = FetchTargetsForPass(1);
            int pass1Count = pass1Targets.Count;

            if (pass1Count > 0)
            {
                loggerCallback?.Invoke($"[PASS 1] Loaded {pass1Count} core nodes. Initiating loop passes...", 0, 0);

                Parallel.ForEach(pass1Targets, parallelOptions, target =>
                {
                    Interlocked.Increment(ref _totalTargetsChecked);
                    ProcessSingleBodyConsensus(target);

                    int processed = _totalTargetsChecked;
                    if (processed % 5000 == 0 || processed == pass1Count)
                    {
                        loggerCallback?.Invoke(string.Empty, processed, pass1Count);
                    }
                });
            }

            // =================================================================================
            // PASS 2: OUTER CLUSTER CALIBRATION (Targets: Outer Stars C & D)
            // =================================================================================
            List<OptimizerTarget> pass2Targets = FetchTargetsForPass(2);
            int pass2Count = pass2Targets.Count;

            if (pass2Count > 0)
            {
                loggerCallback?.Invoke($"[PASS 2] Loaded {pass2Count} outer nodes. Initiating loop passes...", 0, 0);

                Parallel.ForEach(pass2Targets, parallelOptions, target =>
                {
                    Interlocked.Increment(ref _totalTargetsChecked);
                    ProcessSingleBodyConsensus(target);

                    int processed = _totalTargetsChecked;
                    if (processed % 5000 == 0 || processed == (pass1Count + pass2Count))
                    {
                        loggerCallback?.Invoke(string.Empty, processed, (pass1Count + pass2Count));
                    }
                });
            }

            #region Pipeline Terminal Summary Marshalling
            int absoluteTotalCount = pass1Count + pass2Count;
            loggerCallback?.Invoke("----------------------------------------------------------------", 0, 0);
            loggerCallback?.Invoke("Global optimization processing pass completed successfully.", 0, 0);
            loggerCallback?.Invoke($"Total Bodies Evaluated: {absoluteTotalCount}", 0, 0);
            loggerCallback?.Invoke($"Successfully Calibrated & Anchored: {_successfullyGraduatedCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Low Telemetry Density (< 3 pts): {_skippedLowDensityCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Trajectory Variance Divergence: {_skippedDivergentCount}", 0, 0);
            loggerCallback?.Invoke("----------------------------------------------------------------", 0, 0);
            #endregion
        }
        private List<OptimizerTarget> FetchTargetsForPass(int passNumber)
        {
            var targets = new List<OptimizerTarget>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                string query = string.Empty;

                if (passNumber == 1)
                {
                    #region Pass 1 Script Query: Select bodies attached directly to primary roots (0 or 2)
                    query = @"
                        SELECT o.BodyDBID, s.SystemName, b.BodyName, o.SemiMajorAxis, o.Eccentricity, o.OrbitalPeriod_Sec, o.IsRetrograde
                        FROM ObitInfo o
                        JOIN Bodies b ON o.BodyDBID = b.BodyDBID
                        JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                        WHERE o.AnchorTimestamp_UnixSec IS NULL 
                          AND o.SemiMajorAxis > 0.0
                          AND o.BodyDBID IN (SELECT ChildBodyDBID FROM BaryCentreNodes WHERE ParentBodyDBID = 0 OR ParentBodyDBID = 2);";
                    #endregion
                }
                else if (passNumber == 2)
                {
                    #region Pass 2 Script Query: Select outer cluster bodies attached to secondary branches (6)
                    query = @"
                        SELECT o.BodyDBID, s.SystemName, b.BodyName, o.SemiMajorAxis, o.Eccentricity, o.OrbitalPeriod_Sec, o.IsRetrograde
                        FROM ObitInfo o
                        JOIN Bodies b ON o.BodyDBID = b.BodyDBID
                        JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                        WHERE o.AnchorTimestamp_UnixSec IS NULL 
                          AND o.SemiMajorAxis > 0.0
                          AND o.BodyDBID IN (SELECT ChildBodyDBID FROM BaryCentreNodes WHERE ParentBodyDBID = 6);";
                    #endregion
                }

                if (string.IsNullOrEmpty(query)) return targets;

                using (var cmd = new SqliteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        #region Safely parse nullable Boolean properties to shield reader from database runtime casting errors
                        bool retrogradeVal = false;
                        if (reader["IsRetrograde"] != DBNull.Value)
                        {
                            retrogradeVal = Convert.ToBoolean(reader["IsRetrograde"]);
                        }
                        #endregion
                        targets.Add(new OptimizerTarget
                        {
                            BodyDBID = Convert.ToInt32(reader["BodyDBID"]),
                            SystemName = Convert.ToString(reader["SystemName"]),
                            BodyName = Convert.ToString(reader["BodyName"]),
                            SemiMajorAxis = Convert.ToDouble(reader["SemiMajorAxis"]),
                            Eccentricity = Convert.ToDouble(reader["Eccentricity"]),
                            OrbitalPeriodSec = Convert.ToDouble(reader["OrbitalPeriod_Sec"]),
                            IsRetrograde = retrogradeVal
                        });
                    }
                }
            }
            return targets;
        }

        private List<TelemetryPoint> LoadBodyTelemetry(int bodyId)
        {
            var points = new List<TelemetryPoint>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT Timestamp_UnixSec, DistanceToArrival 
                    FROM DataPoints 
                    WHERE BodyDBID = @BodyID 
                    ORDER BY Timestamp_UnixSec ASC;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            points.Add(new TelemetryPoint
                            {
                                TimestampUnixSec = Convert.ToInt64(reader["Timestamp_UnixSec"]),
                                DistanceToArrivalLs = Convert.ToDouble(reader["DistanceToArrival"])
                            });
                        }
                    }
                }
            }
            return points;
        }

        private void ProcessSingleBodyConsensus(OptimizerTarget target)
        {
            List<TelemetryPoint> points = LoadBodyTelemetry(target.BodyDBID);

            #region Dynamic Data Density Safeguard: Reject trajectories that contain fewer than 3 observed points
            if (points.Count < Settings.MinDataPoints)
            {
                Interlocked.Increment(ref _skippedLowDensityCount);
                WriteIsolatedDiagnosticLog(target, "LOW_TELEMETRY_DENSITY", points.Count, 0.0);
                return;
            }
            #endregion

            int arrivalBodyId = ResolveArrivalBodyId(target.BodyDBID);
            if (arrivalBodyId == 0)
            {
                WriteIsolatedDiagnosticLog(target, "MISSING_ARRIVAL_DATUM", 0, 0.0);
                return;
            }

            double lowestGlobalError = double.MaxValue;
            TelemetryPoint optimalAnchor = points[0];
            bool optimalClimbingFlag = true;

            #region Outer Minimization Pass (i): Sequentially evaluate every historical point as t0 reference
            for (int i = 0; i < points.Count; i++)
            {
                TelemetryPoint candidate = points[i];
                bool[] retrogradePermutations = new bool[] { true, false };

                foreach (bool trialRetrograde in retrogradePermutations)
                {
                    double accumulatedError = 0.0;

                    for (int j = 0; j < points.Count; j++)
                    {
                        long evalTime = points[j].TimestampUnixSec;

                        // Temporarily assign the trial retrograde flag to the active target body elements
                        target.IsRetrograde = trialRetrograde;

                        Vector3D pTarget = ComputeGlobalVector(target.BodyDBID, evalTime, candidate);
                        Vector3D pArrival = ComputeGlobalVector(arrivalBodyId, evalTime, candidate);
                        double predDist = Vector3D.Distance(pTarget, pArrival);

                        double variance = points[j].DistanceToArrivalLs - predDist;
                        accumulatedError += (variance * variance);
                    }

                    if (accumulatedError < lowestGlobalError)
                    {
                        lowestGlobalError = accumulatedError;
                        optimalAnchor = candidate;
                        optimalClimbingFlag = trialRetrograde; // Saves out the solved retrograde state flag
                    }
                }
            }
            #endregion

            #region Trajectory Divergence Gate: Evaluate if the average variance stretches past maximum safety limits
            double averageVarianceLs = Math.Sqrt(lowestGlobalError / points.Count);
            if (averageVarianceLs > Settings.MaxAllowedVarianceLs)
            {
                Interlocked.Increment(ref _skippedDivergentCount);
                WriteIsolatedDiagnosticLog(target, "MATH_PASS_DIVERGENT", points.Count, averageVarianceLs);
                return;
            }
            #endregion

            UpdateOptimalAnchorInDatabase(target.BodyDBID, optimalAnchor, optimalClimbingFlag);
            Interlocked.Increment(ref _successfullyGraduatedCount);
        }

        private void UpdateOptimalAnchorInDatabase(int bodyId, TelemetryPoint anchor, bool climbing)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // 1. Update the active child body row cleanly
                string updateSql = @"
                    UPDATE ObitInfo 
                    SET AnchorTimestamp_UnixSec = @TS,
                        AnchorDistance_Ls = @DIST,
                        IsClimbingOutward = @CLIMB
                    WHERE BodyDBID = @BodyID;";

                using (var cmd = new SqliteCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyId);
                    cmd.Parameters.AddWithValue("@TS", anchor.TimestampUnixSec);
                    cmd.Parameters.AddWithValue("@DIST", anchor.DistanceToArrivalLs);
                    cmd.Parameters.AddWithValue("@CLIMB", climbing);
                    cmd.ExecuteNonQuery();
                }

                // 2. Cascade anchor variables down through the ENTIRE parent matrix stack
                List<int> parents = LookupAncestryChain(bodyId);
                if (parents.Count > 0)
                {
                    string updateParentSql = @"
                        UPDATE ObitInfo
                        SET AnchorTimestamp_UnixSec = @TS,
                            AnchorDistance_Ls = @DIST,
                            IsClimbingOutward = @CLIMB
                        WHERE BodyDBID = @ParentID 
                          AND AnchorTimestamp_UnixSec IS NULL;";

                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var cmdParent = new SqliteCommand(updateParentSql, conn, transaction))
                        {
                            cmdParent.Parameters.AddWithValue("@TS", anchor.TimestampUnixSec);
                            cmdParent.Parameters.AddWithValue("@DIST", anchor.DistanceToArrivalLs);
                            cmdParent.Parameters.AddWithValue("@CLIMB", climbing);
                            cmdParent.Parameters.Add("@ParentID", SqliteType.Integer);

                            // Walk the entire chain, initializing each parent node layer natively
                            foreach (int parentId in parents)
                            {
                                cmdParent.Parameters["@ParentID"].Value = parentId;
                                cmdParent.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
        }
        private void WriteIsolatedDiagnosticLog(OptimizerTarget target, string failureType, int pointsCount, double maxDeviationLs)
        {
            lock (_logFileLock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (failureType == "LOW_TELEMETRY_DENSITY")
                    {
                        File.AppendAllText(Settings.LogLowDensityPath, $"[{timestamp}] LOW_TELEMETRY_DENSITY | ID: {target.BodyDBID} | System: {target.SystemName} | Body: {target.BodyName} | Points: {pointsCount}{Environment.NewLine}");
                    }
                    else if (failureType == "MATH_PASS_DIVERGENT")
                    {
                        File.AppendAllText(Settings.LogDivergencePath, $"[{timestamp}] MATH_PASS_DIVERGENT | ID: {target.BodyDBID} | System: {target.SystemName} | Body: {target.BodyName} | Max Dev: {maxDeviationLs:F2} Ls{Environment.NewLine}");
                    }
                    else if(failureType == "MISSING_ARRIVAL_DATUM")
                    {
                        File.AppendAllText(Settings.LogMissingArrivalDatumPath, $"[{timestamp}] MISSING_ARRIVAL_DATUM | ID: {target.BodyDBID} | System: {target.SystemName} | Body: {target.BodyName} | No system star has AnchorDistance_Ls = 0.0 mapped{Environment.NewLine}");
                    }
                }
                catch { }
            }
        }


        //New
        private List<int> LookupAncestryChain(int bodyDbId)
        {
            var chainIds = new List<int>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT ParentBodyDBID FROM BaryCentreNodes 
                    WHERE ChildBodyDBID = @BodyID 
                    ORDER BY HierarchyDepth ASC;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyDbId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chainIds.Add(Convert.ToInt32(reader["ParentBodyDBID"]));
                        }
                    }
                }
            }
            return chainIds;
        }
        private int ResolveArrivalBodyId(int currentBodyDbId)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // Direct database subquery script: filters, scans, and isolates the entry body ID natively
                string query = @"
                    SELECT d.BodyDBID 
                    FROM DataPoints d
                    JOIN Bodies b ON d.BodyDBID = b.BodyDBID
                    WHERE d.DistanceToArrival = 0.0
                      AND b.SystemDBID = (SELECT SystemDBID FROM Bodies WHERE BodyDBID = @BodyID LIMIT 1)
                    LIMIT 1;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", currentBodyDbId);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        private bool TryLoadElements(int bodyId, out OrbitalElements elements)
        {
            elements = new OrbitalElements();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, 
                           AnchorTimestamp_UnixSec, AnchorDistance_Ls,
                           IsClimbingOutward, IsRetrograde,
                           OrbitalInclination, Periapsis, MeanAnomaly, AscendingNode
                    FROM ObitInfo 
                    WHERE BodyDBID = @ID;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", bodyId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            elements.SemiMajorAxisMetres = Convert.ToDouble(reader["SemiMajorAxis"]);
                            elements.Eccentricity = Convert.ToDouble(reader["Eccentricity"]);
                            elements.OrbitalPeriodSeconds = Convert.ToDouble(reader["OrbitalPeriod_Sec"]);

                            if (reader["AnchorTimestamp_UnixSec"] != DBNull.Value)
                                elements.AnchorTimestampUnixSec = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]);

                            if (reader["AnchorDistance_Ls"] != DBNull.Value)
                                elements.AnchorDistanceLs = Convert.ToDouble(reader["AnchorDistance_Ls"]);

                            if (reader["IsClimbingOutward"] != DBNull.Value)
                                elements.IsClimbingOutward = Convert.ToBoolean(reader["IsClimbingOutward"]);

                            if (reader["IsRetrograde"] != DBNull.Value)
                                elements.IsRetrograde = Convert.ToBoolean(reader["IsRetrograde"]);

                            elements.OrbitalInclination = Convert.ToDouble(reader["OrbitalInclination"]);
                            elements.Periapsis = Convert.ToDouble(reader["Periapsis"]);
                            elements.MeanAnomaly = Convert.ToDouble(reader["MeanAnomaly"]);
                            elements.AscendingNode = Convert.ToDouble(reader["AscendingNode"]);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private Vector3D ComputeGlobalVector(int bodyDbId, long targetUnixSec, TelemetryPoint candidateAnchor)
        {
            Vector3D sumVector = new Vector3D(0, 0, 0);

            // Fetch structural math parameters matching target base body ID row
            OrbitalElements targetEl;
            if (TryLoadElements(bodyDbId, out targetEl))
            {
                // Explicitly inject active optimization candidate anchor properties into target elements frame
                targetEl.AnchorTimestampUnixSec = candidateAnchor.TimestampUnixSec;
                targetEl.AnchorDistanceLs = candidateAnchor.DistanceToArrivalLs;

                sumVector += KeplerOrbitSolver.Compute3DLocalPosition(targetEl, targetUnixSec, targetEl.AnchorTimestampUnixSec);
            }

            // Climb horizontally outwards summing vectors from vertically stacked ancestor rows
            List<int> parentsChain = LookupAncestryChain(bodyDbId);
            foreach (int parentId in parentsChain)
            {
                OrbitalElements parentEl;
                if (TryLoadElements(parentId, out parentEl))
                {
                    // Note: Parent objects keep their standard baseline database settings unchanged 
                    // because their orbits are already calibrated relative to their own parents!
                    sumVector += KeplerOrbitSolver.Compute3DLocalPosition(parentEl, targetUnixSec, parentEl.AnchorTimestampUnixSec);
                }
            }

            return sumVector;
        }

        private void InitializeMissingBarycenterRows()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // Direct database script: Isolates parent nodes that exist in structure but lack a row in ObitInfo
                string findMissingQuery = @"
                    SELECT DISTINCT bcn.ParentBodyDBID 
                    FROM BaryCentreNodes bcn
                    LEFT JOIN ObitInfo o ON bcn.ParentBodyDBID = o.BodyDBID
                    WHERE o.BodyDBID IS NULL AND bcn.ParentBodyDBID != 0;";

                var missingParentIds = new List<int>();
                using (var cmdFind = new SqliteCommand(findMissingQuery, conn))
                using (var reader = cmdFind.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        missingParentIds.Add(Convert.ToInt32(reader["ParentBodyDBID"]));
                    }
                }

                if (missingParentIds.Count == 0) return;

                // Insert a placeholder row for each missing abstract barycenter node natively
                string insertPlaceholderQuery = @"
                    INSERT INTO ObitInfo (
                        BodyDBID, SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, 
                        OrbitalInclination, Periapsis, MeanAnomaly, AscendingNode, IsRetrograde
                    ) VALUES (
                        @BodyID, 0.0, 0.0, 0.0, 
                        0.0, 0.0, 0.0, 0.0, 0
                    );";

                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmdInsert = new SqliteCommand(insertPlaceholderQuery, conn, transaction))
                    {
                        cmdInsert.Parameters.Add("@BodyID", SqliteType.Integer);

                        foreach (int missingId in missingParentIds)
                        {
                            cmdInsert.Parameters["@BodyID"].Value = missingId;
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

    }
}
