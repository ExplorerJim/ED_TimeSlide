using ED_TimeSlide;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ED_TimeSlide.KeplerOrbitSolver;

namespace ED_TimeSlide
{
    public static class Settings
    {
        #region Fixed Engine Safety Gates
        public static readonly double MaxAllowedErrorPercent = 5;
        public static readonly double MaxStellarVarianceLs = 25.0;
        public static readonly double MinStellarDistanceForStars = 50000.0;
        public static readonly string winRarExePath = @"C:\Program Files\WinRAR\WinRAR.exe";
        public static readonly string FilterBeltCluster = "Belt Cluster";
        public static readonly string FilterRingCluster = "Ring Cluster";
        #endregion
        #region Constants used
        public static readonly double SpeedOfLightMetersPerSecond = 299792458.0;
        //public static readonly long RealLifeSimulationLaunchEpochSeconds = 1388534400;
        #endregion
        #region Filenames
        private static readonly string BaseDataDir = @"D:\Elite Dangerous\Data"; //Office D, Lounge E
        public static readonly string DatabasesDir = Path.Combine(BaseDataDir, "Databases");
        public static readonly string LogsDir = Path.Combine(BaseDataDir, "Logs");
        public static readonly string ErrorsDir = Path.Combine(BaseDataDir, "Errors");
        public static readonly string ReportsDir = Path.Combine(BaseDataDir, "Reports");
        public static readonly string MasterOrbitRegistryPath = Path.Combine(DatabasesDir, "MasterOrbitRegistry.json");
        public static readonly string StagingOrbitRegistryPath = Path.Combine(DatabasesDir, "StagingOrbitRegistry.json");
        public static readonly string AnomaliesReportPath = Path.Combine(ReportsDir, "DetectedAnomalies.json");
        public static readonly string ErrorLogPath = Path.Combine(ErrorsDir, "SystemInitialization_Errors.txt");
        #endregion
        #region Adaptive Orbital Graph Resolution Constraints
        public static readonly double TargetStepsPerOrbitCycle = 200.0;
        public static readonly int MinAdaptiveGraphResolution = 500;
        public static readonly int MaxAdaptiveGraphResolution = 4000;
        public static readonly double PlotXAxisMargin = 0.01;
        public static readonly double PaddingTimeCushion = 0.05;
        public static readonly double PlotYAxisMarginPercent = 0.10;
        #endregion
        #region Scan Database
        public static readonly string ScanDataDbPath = Path.Combine(DatabasesDir, "ScanData.db");
        public static readonly string DBErrorLogPath = Path.Combine(ErrorsDir, "db_errors.txt");
        public static readonly string UnknownData = "Unknown";
        #endregion
        #region Phase 2 Optimization Specs & Bounds
        public static readonly double MaxAllowedVarianceLs = 8;
        public static readonly int MinDataPoints = 4;
        #endregion
        #region Central Database Broker Connection Strings & Stability Config
        public static readonly int ConnectionDefaultTimeoutSec = 5;
        public static readonly int ConnectionBusyTimeoutMs = 5000;
        #endregion
        #region Phase 2.2 Split Log Paths
        public static readonly string LogLowDensityPath = Path.Combine(LogsDir, "SystemInitialization_LowDensity.txt");
        public static readonly string LogDivergencePath = Path.Combine(LogsDir, "SystemInitialization_Divergence.txt");
        #endregion
        #region Bulk Telemetry Pipeline Thresholds
        public static readonly int QueueBoundedCapacity = 100000;
        public static readonly int BatchSizeThreshold = 5000;
        public static readonly int BatchTimeoutMilliseconds = 1000;
        #endregion
    }
}
