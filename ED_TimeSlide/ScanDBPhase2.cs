using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;


namespace ED_TimeSlide.Engine
{
    public class ScanDBPhase2
    {
        #region Structures
        public struct OptimizerTarget
        {
            public int BodyDBID;
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
        private readonly string _errorLogPath;
        private readonly object _logFileLock = new object();    
        private int _totalTargetsChecked = 0;
        private int _successfullyGraduatedCount = 0;
        private int _skippedLowDensityCount = 0;
        private int _skippedDivergentCount = 0;
        #endregion

        public ScanDBPhase2(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};";
            _errorLogPath = Settings.ErrorLogPath;
        }
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

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            #region Distribute isolated calculation matrix rows across all available hardware threads
            Parallel.ForEach(targets, parallelOptions, target =>
            {
                Interlocked.Increment(ref _totalTargetsChecked);
                ProcessSingleBodyConsensus(target);

                #region Throttling UI Updates: Only marshal one string text row every 50000 items processed
                int processed = _totalTargetsChecked;
                if (processed % 5000 == 0 || processed == totalCount)
                {
                    loggerCallback?.Invoke(string.Empty, processed, totalCount);
                }
                #endregion
            });
            #endregion

            #region Pipeline Terminal Summary Marshalling
            loggerCallback?.Invoke("----------------------------------------------------------------",0,0);
            loggerCallback?.Invoke("Global optimization processing pass completed successfully.", 0, 0);
            loggerCallback?.Invoke($"Total Bodies Evaluated: {totalCount}", 0, 0);
            loggerCallback?.Invoke($"Successfully Calibrated & Anchored: {_successfullyGraduatedCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Low Telemetry Density (< 3 pts): {_skippedLowDensityCount}", 0, 0);
            loggerCallback?.Invoke($"Skipped due to Trajectory Variance Divergence: {_skippedDivergentCount}", 0, 0);
            loggerCallback?.Invoke($"Detailed failure rows are securely baked to disk: {Path.GetFileName(_errorLogPath)}",0,0);
            loggerCallback?.Invoke("----------------------------------------------------------------",0,0);
            #endregion
        }
        private List<OptimizerTarget> FetchUncalibratedTargets()
        {
            var targets = new List<OptimizerTarget>();
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                #region Querying records from ObitInfo where structural timeline anchors are still unpopulated
                string query = @"
                    SELECT BodyDBID, SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, IsRetrograde
                    FROM ObitInfo
                    WHERE AnchorTimestamp_UnixSec IS NULL AND SemiMajorAxis > 0.0;";
                #endregion

                using (var cmd = new SqliteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        # region Safely parse nullable Boolean properties to shield reader from database runtime casting errors
                        bool retrogradeVal = false;
                        if (reader["IsRetrograde"] != DBNull.Value)
                        {
                            retrogradeVal = Convert.ToBoolean(reader["IsRetrograde"]);
                        }
                        #endregion
                        targets.Add(new OptimizerTarget
                        {
                            BodyDBID = Convert.ToInt32(reader["BodyDBID"]),
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
                WriteThreadSafeErrorLog(target.BodyDBID, "LOW_TELEMETRY_DENSITY", $"Point count is {points.Count}");
                return;
            }
            #endregion

            double lowestGlobalError = double.MaxValue;
            TelemetryPoint optimalAnchor = points[0];
            bool optimalClimbingFlag = true;

            #region Outer Minimization Pass (i): Sequentially evaluate every historical point as t0 reference
            for (int i = 0; i < points.Count; i++)
            {
                TelemetryPoint candidate = points[i];
                bool[] directionPermutations = new bool[] { true, false };

                #region Inner Loop Pass: Test both half-plane trajectory rails to resolve phase direction natively
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
                        double predDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                            testElements, 
                            points[j].TimestampUnixSec);

                        double variance = points[j].DistanceToArrivalLs - predDist;
                        accumulatedError += (variance * variance); 
                    }

                    if (accumulatedError < lowestGlobalError)
                    {
                        lowestGlobalError = accumulatedError;
                        optimalAnchor = candidate;
                        optimalClimbingFlag = evaluateAsClimbing;
                    }
                }
                #endregion
            }
            #endregion
            #region Trajectory Divergence Gate: Evaluate if the average variance stretches past maximum safety limits
            double averageVarianceLs = Math.Sqrt(lowestGlobalError / points.Count);
            if (averageVarianceLs > Settings.MaxAllowedVarianceLs)
            {
                Interlocked.Increment(ref _skippedDivergentCount);
                WriteThreadSafeErrorLog(target.BodyDBID, "MATH_PASS_DIVERGENT", $"MSE variance delta hit {averageVarianceLs:F4} Ls");
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
            }
        }
        private void WriteThreadSafeErrorLog(int bodyId, string failureCode, string technicalDetails)
        {
            #region Protect file streaming from parallel resource access crashes (IOException)
            lock (_logFileLock)
            {
                try
                {
                    string logLine = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | BodyDBID: {bodyId} | Failure: {failureCode} | Info: {technicalDetails}{Environment.NewLine}";
                    File.AppendAllText(_errorLogPath, logLine);
                }
                catch
                {
                    // Silent fail strictly protected during asynchronous text streaming exceptions
                }
            }
            #endregion
        }
    }
}
