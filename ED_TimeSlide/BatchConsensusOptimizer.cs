using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

#region Block 1: Relational Data Models and Thread Coordinator
namespace ED_TimeSlide.Engine
{
    public class BatchConsensusOptimizer
    {
        #region Variables & Lock Primitives
        private readonly string _connectionString;
        private readonly string _errorLogPath;
        private readonly object _logFileLock = new object();

        private int _totalTargetsChecked = 0;
        private int _successfullyGraduatedCount = 0;
        private int _skippedLowDensityCount = 0;
        private int _skippedDivergentCount = 0;
        #endregion

        public BatchConsensusOptimizer(string dbPath)
        {
            // FIX: Removed 'Busy Timeout' from the connection string to prevent syntax exceptions
            _connectionString = $"Data Source={dbPath};Default Timeout={Settings.ConnectionDefaultTimeoutSec};";
            _errorLogPath = Settings.ErrorLogPath;
        }

        /// <summary>
        /// Instantiates an open connection and manually sets the SQLite busy timeout limit via pragma syntax.
        /// </summary>
        private SqliteConnection OpenManagedConnection()
        {
            var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Apply busy timeout manually via command pragma to prevent runtime exceptions
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA busy_timeout = {Settings.ConnectionBusyTimeoutMs};";
                cmd.ExecuteNonQuery();
            }
            return conn;
        }

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

        /// <summary>
        /// Compiles a fully frame-transformed dataset inside the moving Primary Star reference frame in a high-speed array sweep.
        /// </summary>
        public double[] GetStarCentricTrajectorySpace(int bodyId, KeplerOrbitSolver.OrbitalElements inputElements, long[] targetTimestamps)
        {
            int totalStepsCount = targetTimestamps.Length;
            double[] resolvedDistanceArrayLS = new double[totalStepsCount];

            using (var analyticalConn = OpenManagedConnection())
            {
                int primaryStarId = IsolatePrimarySystemStar(analyticalConn, bodyId);
                double targetPeriapsisRad = GetBodyPeriapsisRadians(analyticalConn, bodyId);

                // RULE 1: If evaluating the primary star itself, its distance relative to itself is flat at 0.0 Ls
                if (bodyId == primaryStarId)
                {
                    Array.Clear(resolvedDistanceArrayLS, 0, totalStepsCount);
                    return resolvedDistanceArrayLS;
                }

                // Fetch the primary star's structural orbit elements once to save performance loops
                bool primaryStarValid = TryLoadBodyElements(analyticalConn, primaryStarId, out KeplerOrbitSolver.OrbitalElements starElements);
                double starPeriapsisRad = GetBodyPeriapsisRadians(analyticalConn, primaryStarId);

                for (int step = 0; step < totalStepsCount; step++)
                {
                    long currentUnix = targetTimestamps[step];

                    // 1. Calculate Target Body position relative to its orbital center
                    double bodyOrbitRadiusLs = KeplerOrbitSolver.PredictDistanceAtTimestamp(inputElements, currentUnix);
                    double bodyAnomalyRad = CalculateOrbitalPhaseAngle(inputElements, currentUnix);
                    double bodyTrueAngleRad = bodyAnomalyRad + targetPeriapsisRad;

                    double bodyX = bodyOrbitRadiusLs * Math.Cos(bodyTrueAngleRad);
                    double bodyY = bodyOrbitRadiusLs * Math.Sin(bodyTrueAngleRad);

                    // 2. Calculate Primary Star position relative to its orbital center
                    double starX = 0.0, starY = 0.0;
                    if (primaryStarValid)
                    {
                        double starOrbitRadiusLs = KeplerOrbitSolver.PredictDistanceAtTimestamp(starElements, currentUnix);
                        double starAnomalyRad = CalculateOrbitalPhaseAngle(starElements, currentUnix);
                        double starTrueAngleRad = starAnomalyRad + starPeriapsisRad;

                        // Co-orbiting primary structures dance 180 degrees out of phase around the shared barycenter
                        starX = starOrbitRadiusLs * Math.Cos(starTrueAngleRad + Math.PI);
                        starY = starOrbitRadiusLs * Math.Sin(starTrueAngleRad + Math.PI);
                    }

                    // 3. Compute true relative spatial distance between the targets
                    double dx = bodyX - starX;
                    double dy = bodyY - starY;
                    resolvedDistanceArrayLS[step] = Math.Sqrt(dx * dx + dy * dy);
                }
            }

            return resolvedDistanceArrayLS;
        }


        /// <summary>
        /// Public static gateway method allowing UI Plotters to score single telemetry entries inside the identical moving reference frame.
        /// </summary>
        public double CalculateRelativeStarCentricDistance(int bodyId, KeplerOrbitSolver.OrbitalElements testElements, long currentUnix)
        {
            using (var conn = OpenManagedConnection())
            {
                int primaryStarId = IsolatePrimarySystemStar(conn, bodyId);
                if (bodyId == primaryStarId) return 0.0;

                double targetDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(testElements, currentUnix);
                double targetAnomaly = CalculateOrbitalPhaseAngle(testElements, currentUnix);
                double targetPeriapsisRad = GetBodyPeriapsisRadians(conn, bodyId);
                double targetAngle = targetAnomaly + targetPeriapsisRad;

                double bodyX = targetDist * Math.Cos(targetAngle);
                double bodyY = targetDist * Math.Sin(targetAngle);

                double starX = 0.0, starY = 0.0;
                if (TryLoadBodyElements(conn, primaryStarId, out KeplerOrbitSolver.OrbitalElements starElements))
                {
                    double starOrbitRadiusLs = KeplerOrbitSolver.PredictDistanceAtTimestamp(starElements, currentUnix);
                    double starAnomalyRad = CalculateOrbitalPhaseAngle(starElements, currentUnix);
                    double starPeriapsisRad = GetBodyPeriapsisRadians(conn, primaryStarId);
                    double starTrueAngleRad = starAnomalyRad + starPeriapsisRad;

                    starX = starOrbitRadiusLs * Math.Cos(starTrueAngleRad + Math.PI);
                    starY = starOrbitRadiusLs * Math.Sin(starTrueAngleRad + Math.PI);
                }

                double dx = bodyX - starX;
                double dy = bodyY - starY;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// Internal helper to pull baseline orbital elements for a parent node.
        /// </summary>
        private bool TryLoadBodyElements(SqliteConnection conn, int bodyId, out KeplerOrbitSolver.OrbitalElements elements)
        {
            elements = new KeplerOrbitSolver.OrbitalElements();
            string query = "SELECT SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, IsRetrograde, AnchorTimestamp_UnixSec, AnchorDistance_Ls, IsClimbingOutward FROM ObitInfo WHERE BodyDBID = @BodyID;";

            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@BodyID", bodyId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && reader["AnchorTimestamp_UnixSec"] != DBNull.Value)
                    {
                        elements.SemiMajorAxisMetres = Convert.ToDouble(reader["SemiMajorAxis"]);
                        elements.Eccentricity = Convert.ToDouble(reader["Eccentricity"]);
                        elements.OrbitalPeriodSeconds = Convert.ToDouble(reader["OrbitalPeriod_Sec"]);
                        elements.IsRetrograde = Convert.ToInt32(reader["IsRetrograde"]) == 1;
                        elements.AnchorTimestampUnixSec = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]);
                        elements.AnchorDistanceLs = Convert.ToDouble(reader["AnchorDistance_Ls"]);
                        elements.IsClimbingOutward = Convert.ToBoolean(reader["IsClimbingOutward"]);
                        return true;
                    }
                }
            }
            return false;
        }


        #region Block 2: Database Data Loading Methods
        private List<OptimizerTarget> FetchUncalibratedTargets()
        {
            var targets = new List<OptimizerTarget>();
            // ROUTING HANDLES VIA OPENMANAGEDCONNECTION: Avoids unhandled argument keywords
            using (var conn = OpenManagedConnection())
            {
                string query = @"
                    SELECT o.BodyDBID, s.SystemName, b.BodyName, o.SemiMajorAxis, o.Eccentricity, o.OrbitalPeriod_Sec, o.IsRetrograde
                    FROM ObitInfo o
                    INNER JOIN Bodies b ON o.BodyDBID = b.BodyDBID
                    INNER JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                    WHERE o.AnchorTimestamp_UnixSec IS NULL AND o.SemiMajorAxis > 0.0;";

                using (var cmd = new SqliteCommand(query, conn))
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
                            SystemName = reader["SystemName"].ToString(),
                            BodyName = reader["BodyName"].ToString(),
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
        private int IsolatePrimarySystemStar(SqliteConnection conn, int bodyId)
        {
            int currentId = bodyId;
            for (int i = 0; i < 10; i++)
            {
                string query = "SELECT ParentBodyDBID FROM BaryCentreNodes WHERE ChildBodyDBID = @ChildID LIMIT 1;";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ChildID", currentId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value) currentId = Convert.ToInt32(result);
                    else break;
                }
            }
            return currentId;
        }
        private void GetBodyBarycentricCoordinates(SqliteConnection conn, int bodyId, long currentUnixSec, ref double x, ref double y)
        {
            string query = "SELECT SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, IsRetrograde, AnchorTimestamp_UnixSec, AnchorDistance_Ls, IsClimbingOutward FROM ObitInfo WHERE BodyDBID = @BodyID;";
            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@BodyID", bodyId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && reader["AnchorTimestamp_UnixSec"] != DBNull.Value)
                    {
                        var elements = new KeplerOrbitSolver.OrbitalElements
                        {
                            SemiMajorAxisMetres = Convert.ToDouble(reader["SemiMajorAxis"]),
                            Eccentricity = Convert.ToDouble(reader["Eccentricity"]),
                            OrbitalPeriodSeconds = Convert.ToDouble(reader["OrbitalPeriod_Sec"]),
                            IsRetrograde = Convert.ToInt32(reader["IsRetrograde"]) == 1,
                            AnchorTimestampUnixSec = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]),
                            AnchorDistanceLs = Convert.ToDouble(reader["AnchorDistance_Ls"]),
                            IsClimbingOutward = Convert.ToBoolean(reader["IsClimbingOutward"])
                        };

                        double dist = KeplerOrbitSolver.PredictDistanceAtTimestamp(elements, currentUnixSec);
                        double anomaly = CalculateOrbitalPhaseAngle(elements, currentUnixSec);
                        double periapsisRad = GetBodyPeriapsisRadians(conn, bodyId);
                        double trueAngle = anomaly + periapsisRad;

                        x = dist * Math.Cos(trueAngle);
                        y = dist * Math.Sin(trueAngle);

                        double parentDriftLs = CalculateRecursiveBarycentricDrift(conn, bodyId, currentUnixSec);
                        x += parentDriftLs;
                    }
                }
            }
        }
        /// <summary>
        /// Phase 2.1 Integer Fix: Traverses multi-row hierarchical systems cleanly using fast ID indexes and explicit depth checks.
        /// </summary>
        private double CalculateRecursiveBarycentricDrift(SqliteConnection conn, int bodyId, long currentUnixSec)
        {
            double accumulatedDriftLs = 0.0;
            int currentTargetId = bodyId;

            // Loop boundary constraints preventing circular uncalibrated text loops from trapping system threads
            int maxDepthClimbCounter = 0;

            while (maxDepthClimbCounter < 10)
            {
                maxDepthClimbCounter++;
                int discoveredParentId = -1;

                // PERFORMANCE UPGRADE: Enforce direct numeric ID comparison keys over character strings
                string parentQuery = @"
                    SELECT ParentBodyDBID 
                    FROM BaryCentreNodes 
                    WHERE ChildBodyDBID = @ChildID AND ParentBodyDBID IS NOT NULL
                    ORDER BY HierarchyDepth ASC LIMIT 1;";

                using (var cmd = new SqliteCommand(parentQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ChildID", currentTargetId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        discoveredParentId = Convert.ToInt32(result);
                    }
                }

                // SECURE EXIT: If no parent node exists, or it maps right back to itself, we have hit the absolute root star
                if (discoveredParentId == -1 || discoveredParentId == currentTargetId)
                {
                    break;
                }

                // Pull parent metrics exclusively via integer DBID indexing keys
                string orbitQuery = @"
                    SELECT SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, IsRetrograde,
                           AnchorTimestamp_UnixSec, AnchorDistance_Ls, IsClimbingOutward
                    FROM ObitInfo WHERE BodyDBID = @BodyID;";

                bool structuralDriftApplied = false;

                using (var cmd = new SqliteCommand(orbitQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", discoveredParentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && reader["AnchorTimestamp_UnixSec"] != DBNull.Value)
                        {
                            var parentElements = new KeplerOrbitSolver.OrbitalElements
                            {
                                SemiMajorAxisMetres = Convert.ToDouble(reader["SemiMajorAxis"]),
                                Eccentricity = Convert.ToDouble(reader["Eccentricity"]),
                                OrbitalPeriodSeconds = Convert.ToDouble(reader["OrbitalPeriod_Sec"]),
                                IsRetrograde = Convert.ToInt32(reader["IsRetrograde"]) == 1,
                                AnchorTimestampUnixSec = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]),
                                AnchorDistanceLs = Convert.ToDouble(reader["AnchorDistance_Ls"]),
                                IsClimbingOutward = Convert.ToBoolean(reader["IsClimbingOutward"])
                            };

                            accumulatedDriftLs += KeplerOrbitSolver.PredictDistanceAtTimestamp(parentElements, currentUnixSec);
                            structuralDriftApplied = true;
                        }
                    }
                }

                // TERMINATION GATE: If parent row exists but lacks calibrated elements, break to protect thread runtime
                if (!structuralDriftApplied)
                {
                    break;
                }

                // Step up to the next parent layer in the hierarchy tree
                currentTargetId = discoveredParentId;
            }

            return accumulatedDriftLs;
        }

        private List<TelemetryPoint> LoadBodyTelemetry(int bodyId)
        {
            var points = new List<TelemetryPoint>();
            using (var conn = OpenManagedConnection())
            {
                string query = "SELECT Timestamp_UnixSec, DistanceToArrival FROM DataPoints WHERE BodyDBID = @BodyID ORDER BY Timestamp_UnixSec ASC;";
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
        private void UpdateOptimalAnchorInDatabase(int bodyId, TelemetryPoint anchor, bool climbing)
        {
            using (var conn = OpenManagedConnection())
            {
                string query = "UPDATE ObitInfo SET AnchorTimestamp_UnixSec=@TS, AnchorDistance_Ls=@DIST, IsClimbingOutward=@CLIMB WHERE BodyDBID=@BodyID;";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyId);
                    cmd.Parameters.AddWithValue("@TS", anchor.TimestampUnixSec);
                    cmd.Parameters.AddWithValue("@DIST", anchor.DistanceToArrivalLs);
                    cmd.Parameters.AddWithValue("@CLIMB", climbing);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion
        public void ExecuteGlobalOptimizationPass(Action<string, int, int> loggerCallback)
        {
            List<OptimizerTarget> targets = FetchUncalibratedTargets();
            int totalCount = targets.Count;

            if (totalCount == 0)
            {
                loggerCallback?.Invoke("No uncalibrated targets found in ObitInfo table.", 0, 0);
                return;
            }

            loggerCallback?.Invoke($"Loaded {totalCount} nodes. Initiating hardware parallel loops...", 0, 0);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(targets, parallelOptions, target =>
            {
                Interlocked.Increment(ref _totalTargetsChecked);
                ProcessSingleBodyConsensus(target);

                int processed = _totalTargetsChecked;
                if (processed % 5000 == 0 || processed == totalCount)
                {
                    loggerCallback?.Invoke(string.Empty, processed, totalCount);
                }
            });

            #region Terminal Marshalling Updates
            loggerCallback?.Invoke("----------------------------------------------------------------", 0, 0);
            loggerCallback?.Invoke("Global optimization processing pass completed successfully.", 0, 0);
            loggerCallback?.Invoke($"Total Bodies Evaluated: {totalCount}", 0, 0);
            loggerCallback?.Invoke($"Successfully Calibrated & Anchored: {_successfullyGraduatedCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Low Telemetry Density: {_skippedLowDensityCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Trajectory Variance Divergence: {_skippedDivergentCount}", 0, 0);
            loggerCallback?.Invoke("----------------------------------------------------------------", 0, 0);
            #endregion

            // Trigger our decoupled database deep cleaner pass
            loggerCallback?.Invoke("Initiating low-density exploration data purge pass...", 0, 0);
            var broker = new ScanDatabaseBroker();
            broker.PurgeLowDensityExplorationData();

            loggerCallback?.Invoke("Reclaiming disk sectors. Vacuuming database storage blocks...", 0, 0);
            loggerCallback?.Invoke("Database optimization pass fully completed. Redundant exploration rows dropped.", 0, 0);

            // EXECUTE FINAL LOOKUP TREE BAKE: Re-indexes database matrices after barycenter modifications are finalized
            loggerCallback?.Invoke("Builing scanDB performance indexes.", 0, 0);
            broker.BuildPerformanceIndexes();
            loggerCallback?.Invoke("Finished.", 0, 0);
        }
        #region Block 3: O(N²) Optimization Loops and Table Persistence
        /// <summary>
        /// Solves the optimal reference t0 anchor plane using high-performance shared connection instances.
        /// </summary>
        private void ProcessSingleBodyConsensus(OptimizerTarget target)
        {
            List<TelemetryPoint> points = LoadBodyTelemetry(target.BodyDBID);
            if (points.Count < Settings.MinDataPoints)
            {
                Interlocked.Increment(ref _skippedLowDensityCount);
                WriteIsolatedDiagnosticLog(target, "LOW_TELEMETRY_DENSITY", points.Count, 0.0);
                return;
            }

            double lowestGlobalError = double.MaxValue;
            TelemetryPoint optimalAnchor = new TelemetryPoint();
            bool optimalClimbingFlag = true;

            // PERFORMANCE FIX: Share a single connection and pre-fetch the immutable primary star ID once
            using (var iterationConn = OpenManagedConnection())
            {
                int primaryStarId = IsolatePrimarySystemStar(iterationConn, target.BodyDBID);
                double targetPeriapsisRad = GetBodyPeriapsisRadians(iterationConn, target.BodyDBID);

                for (int i = 0; i < points.Count; i++)
                {
                    TelemetryPoint candidate = points[i];
                    bool[] directionPermutations = new bool[] { true, false };

                    foreach (bool evaluateAsClimbing in directionPermutations)
                    {
                        double accumulatedError = 0.0;
                        KeplerOrbitSolver.OrbitalElements testElements = new KeplerOrbitSolver.OrbitalElements
                        {
                            SemiMajorAxisMetres = target.SemiMajorAxis,
                            Eccentricity = target.Eccentricity,
                            OrbitalPeriodSeconds = target.OrbitalPeriodSec,
                            IsRetrograde = target.IsRetrograde,
                            AnchorTimestampUnixSec = candidate.TimestampUnixSec,
                            AnchorDistanceLs = candidate.DistanceToArrivalLs,
                            IsClimbingOutward = evaluateAsClimbing
                        };

                        for (int j = 0; j < points.Count; j++)
                        {
                            long currentUnix = points[j].TimestampUnixSec;
                            double finalTrajectoryLs = 0.0;

                            if (target.BodyDBID != primaryStarId)
                            {
                                double bodyDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(testElements, currentUnix);
                                double bodyAnomaly = CalculateOrbitalPhaseAngle(testElements, currentUnix);
                                double bodyAngle = bodyAnomaly + targetPeriapsisRad;

                                double bodyX = bodyDist * Math.Cos(bodyAngle);
                                double bodyY = bodyDist * Math.Sin(bodyAngle);

                                double parentDriftLs = CalculateRecursiveBarycentricDrift(iterationConn, target.BodyDBID, currentUnix);
                                bodyX += parentDriftLs;

                                double starX = 0.0, starY = 0.0;
                                GetBodyBarycentricCoordinates(iterationConn, primaryStarId, currentUnix, ref starX, ref starY);

                                double dx = bodyX - starX;
                                double dy = bodyY - starY;
                                finalTrajectoryLs = Math.Sqrt(dx * dx + dy * dy);
                            }

                            double variance = points[j].DistanceToArrivalLs - finalTrajectoryLs;
                            accumulatedError += (variance * variance);
                        }

                        if (accumulatedError < lowestGlobalError)
                        {
                            lowestGlobalError = accumulatedError;
                            optimalAnchor = candidate;
                            optimalClimbingFlag = evaluateAsClimbing;
                        }
                    }
                }
            }

            double averageVarianceLs = Math.Sqrt(lowestGlobalError / points.Count);
            if (averageVarianceLs > Settings.MaxAllowedVarianceLs)
            {
                Interlocked.Increment(ref _skippedDivergentCount);
                WriteIsolatedDiagnosticLog(target, "MATH_PASS_DIVERGENT", points.Count, averageVarianceLs);
                return;
            }

            UpdateOptimalAnchorInDatabase(target.BodyDBID, optimalAnchor, optimalClimbingFlag);
            Interlocked.Increment(ref _successfullyGraduatedCount);
        }
        private double GetBodyPeriapsisRadians(SqliteConnection conn, int bodyId)
        {
            string query = "SELECT Periapsis FROM ObitInfo WHERE BodyDBID = @BodyID LIMIT 1;";
            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@BodyID", bodyId);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDouble(result) * (Math.PI / 180.0) : 0.0;
            }
        }
        private double CalculateOrbitalPhaseAngle(KeplerOrbitSolver.OrbitalElements input, long targetTimestampUnixSec)
        {
            double anchorMetres = input.AnchorDistanceLs * Settings.SpeedOfLightMetersPerSecond;
            double cosE = (1.0 - (anchorMetres / input.SemiMajorAxisMetres)) / input.Eccentricity;
            cosE = Math.Max(-1.0, Math.Min(1.0, cosE));
            double eccentricAnomalyAnchor = Math.Acos(cosE);

            if (!input.IsClimbingOutward) eccentricAnomalyAnchor = (2.0 * Math.PI) - eccentricAnomalyAnchor;

            double meanAnomalyAnchor = eccentricAnomalyAnchor - input.Eccentricity * Math.Sin(eccentricAnomalyAnchor);
            double deltaTimeSeconds = targetTimestampUnixSec - input.AnchorTimestampUnixSec;
            double meanMotion = (2.0 * Math.PI) / input.OrbitalPeriodSeconds;

            if (input.IsRetrograde) meanMotion = -meanMotion;

            double targetMeanAnomaly = (meanAnomalyAnchor + (meanMotion * deltaTimeSeconds)) % (2.0 * Math.PI);
            if (targetMeanAnomaly < 0) targetMeanAnomaly += 2.0 * Math.PI;

            double targetEccentricAnomaly = targetMeanAnomaly;
            for (int i = 0; i < 6; i++)
            {
                double deltaE = (targetEccentricAnomaly - input.Eccentricity * Math.Sin(targetEccentricAnomaly) - targetMeanAnomaly)
                                / (1.0 - input.Eccentricity * Math.Cos(targetEccentricAnomaly));
                targetEccentricAnomaly -= deltaE;
                if (Math.Abs(deltaE) < 1e-7) break;
            }

            double tanHalfV = Math.Sqrt((1.0 + input.Eccentricity) / (1.0 - input.Eccentricity)) * Math.Tan(targetEccentricAnomaly / 2.0);
            return 2.0 * Math.Atan(tanHalfV);
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
                }
                catch { }
            }
        }


    }
        #endregion
}
#endregion
