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
            // 1. Ensure all missing abstract parent frames have a safe placeholder slot initialized
            InitializeMissingBarycenterRows();

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };

            // 2. Query the database to find the deepest tier present in the system structure
            int maxDepth = 0;
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqliteCommand("SELECT MAX(HierarchyDepth) FROM BaryCentreNodes;", conn);
                var res = cmd.ExecuteScalar();
                if (res != DBNull.Value && res != null)
                {
                    maxDepth = Convert.ToInt32(res);
                }
            }

            // 3. CORE LOOP: Execute the optimization sequence sequentially from Depth 0 to maxDepth
            for (int currentDepth = 0; currentDepth <= maxDepth; currentDepth++)
            {
                // Isolate nodes belonging strictly to the active tier level
                List<OptimizerTarget> depthTargets = FetchTargetsForDepth(currentDepth);
                int targetCount = depthTargets.Count;

                if (targetCount > 0)
                {
                    loggerCallback?.Invoke($"[PASS - HIERARCHY TIER {currentDepth}] Loaded {targetCount} nodes. Calibrating...", 0, 0);

                    Parallel.ForEach(depthTargets, parallelOptions, target =>
                    {
                        Interlocked.Increment(ref _totalTargetsChecked);
                        ProcessSingleBodyConsensus(target);
                    });
                }
            }

            // 4. POST-PROCESS SWEEP: Safely bind synchronized baseline epochs to zero-radius nodes
            CalibrateAbstractBarycenters();

            #region Pipeline Terminal Summary Printout
            loggerCallback?.Invoke("----------------------------------------------------------------", 0, 0);
            loggerCallback?.Invoke("Top-down global optimization pass completed successfully.", 0, 0);
            loggerCallback?.Invoke($"Total Bodies Evaluated: {_totalTargetsChecked}", 0, 0);
            loggerCallback?.Invoke($"Successfully Calibrated & Anchored: {_successfullyGraduatedCount}", 0, 0);
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

                // FIX: The entire "2. Cascade anchor variables down..." transaction loop block is completely DELETED.
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

                // 1. Isolate unique abstract parent barycenters that do not have a mapped row in ObitInfo
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

                // 2. Insert rows leaving anchor tracking flags as explicit, uncalibrated database NULLs
                string insertPlaceholderQuery = @"
                        INSERT INTO ObitInfo (
                            BodyDBID, SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, 
                            OrbitalInclination, Periapsis, MeanAnomaly, AscendingNode, 
                            IsRetrograde, AnchorTimestamp_UnixSec, AnchorDistance_Ls, IsClimbingOutward
                        ) VALUES (
                            @BodyID, 0.0, 0.0, 0.0, 
                            0.0, 0.0, 0.0, 0.0, 
                            0, NULL, NULL, NULL
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
        private void CalibrateAbstractBarycenters()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // 1. Identify all abstract barycenters (SMA == 0) that are still uncalibrated (NULL anchors)
                string findNullBarycentersQuery = @"
            SELECT BodyDBID FROM ObitInfo 
            WHERE SemiMajorAxis = 0.0 AND AnchorTimestamp_UnixSec IS NULL;";

                var barycenterIds = new List<int>();
                using (var cmd = new SqliteCommand(findNullBarycentersQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        barycenterIds.Add(Convert.ToInt32(reader["BodyDBID"]));
                    }
                }

                if (barycenterIds.Count == 0) return;

                // 2. Query builder to locate the nearest physical child body orbiting this barycenter
                // to inherit its calibrated timeline epoch framework safely.
                string findChildAnchorQuery = @"
            SELECT o.AnchorTimestamp_UnixSec, o.IsClimbingOutward
            FROM BaryCentreNodes bcn
            JOIN ObitInfo o ON bcn.ChildBodyDBID = o.BodyDBID
            WHERE bcn.ParentBodyDBID = @ParentID 
              AND o.AnchorTimestamp_UnixSec IS NOT NULL
            LIMIT 1;";

                // 3. Update query that stamps the timeline anchor window onto the barycenter,
                // but explicitly enforces a 0.0 Ls coordinate distance offset vector.
                string updateBarycenterSql = @"
            UPDATE ObitInfo 
            SET AnchorTimestamp_UnixSec = @TS,
                AnchorDistance_Ls = 0.0,
                IsClimbingOutward = @CLIMB
            WHERE BodyDBID = @ParentID;";

                using (var transaction = conn.BeginTransaction())
                {
                    foreach (int baryId in barycenterIds)
                    {
                        long childTimestamp = 0;
                        bool childClimbing = true;
                        bool foundValidChild = false;

                        using (var cmdChild = new SqliteCommand(findChildAnchorQuery, conn, transaction))
                        {
                            cmdChild.Parameters.AddWithValue("@ParentID", baryId);
                            using (var reader = cmdChild.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    childTimestamp = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]);
                                    childClimbing = Convert.ToBoolean(reader["IsClimbingOutward"]);
                                    foundValidChild = true;
                                }
                            }
                        }

                        // If found, synchronize the time epoch but keep the barycenter origin stable at 0.0 Ls
                        if (foundValidChild)
                        {
                            using (var cmdUpdate = new SqliteCommand(updateBarycenterSql, conn, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@ParentID", baryId);
                                cmdUpdate.Parameters.AddWithValue("@TS", childTimestamp);
                                cmdUpdate.Parameters.AddWithValue("@CLIMB", childClimbing);
                                cmdUpdate.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }
        private List<OptimizerTarget> FetchTargetsForDepth(int depth)
        {
            var targets = new List<OptimizerTarget>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // This query isolates rows that haven't been calibrated yet, filtering strictly by target depth
                string query = @"
            SELECT o.BodyDBID, s.SystemName, b.BodyName, o.SemiMajorAxis, o.Eccentricity, o.OrbitalPeriod_Sec, o.IsRetrograde
            FROM ObitInfo o
            JOIN Bodies b ON o.BodyDBID = b.BodyDBID
            JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
            WHERE o.AnchorTimestamp_UnixSec IS NULL 
              AND o.SemiMajorAxis > 0.0
              AND o.BodyDBID IN (SELECT ChildBodyDBID FROM BaryCentreNodes WHERE HierarchyDepth = @Depth);";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Depth", depth);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool retrogradeVal = false;
                            if (reader["IsRetrograde"] != DBNull.Value)
                            {
                                retrogradeVal = Convert.ToBoolean(reader["IsRetrograde"]);
                            }

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
            }
            return targets;
        }

    }
}
