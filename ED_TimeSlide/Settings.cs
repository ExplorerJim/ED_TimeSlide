using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED_TimeSlide
{
    public static class Settings
    {
        #region Fixed Engine Safety Gates
        public static readonly double MaxAllowedErrorPercent = 5;
        public static readonly double MaxStellarVarianceLs = 25.0;
        public static readonly double MinStellarDistanceForStars = 50000.0;
        public static readonly string winRarExePath = @"C:\Program Files\WinRAR\WinRAR.exe";
        #endregion
        #region Speed of Light constant used to convert Light Seconds into Metres
        public static readonly double SpeedOfLightMetersPerSecond = 299792458.0;
        #endregion
        #region Filenames
        public static readonly string MasterOrbitRegistryFileName = "MasterOrbitRegistry.json";
        public static readonly string StagingOrbitRegistryFileName = "StagingOrbitRegistry.json";
        public static readonly string AnomaliesReportFileName = "DetectedAnomalies.json";
        public static readonly string ErrorLogFileName = "SystemInitialization_Errors.txt";
        #endregion
        #region Adaptive Orbital Graph Resolution Constraints
        public static readonly double TargetStepsPerOrbitCycle = 200.0;
        public static readonly int MinAdaptiveGraphResolution = 500;
        public static readonly int MaxAdaptiveGraphResolution = 4000;
        public static readonly double PlotXAxisMargin = 0.01;
        public static readonly double PaddingTimeCushion = 0.05;
        public static readonly double PlotYAxisMarginPercent = 0.10;
        #endregion
    }
}
