using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ED_TimeSlide
{
    public static class Settings
    {
        // Fixed Engine Safety Gates
        public static readonly double MaxAllowedErrorPercent = 5.0;
        public static readonly double MaxStellarVarianceLs = 25.0;
        public static readonly double MinStellarDistanceForStars = 50000.0;
        public static readonly string winRarExePath = @"C:\Program Files\WinRAR\WinRAR.exe";

        // Speed of Light constant used to convert Light Seconds into Metres
        public static readonly double SpeedOfLightMetersPerSecond = 299792458.0;

        //Filenames
        public static readonly string MasterOrbitRegistryFileName = "MasterOrbitRegistry.json";
        public static readonly string StagingOrbitRegistryFileName = "StagingOrbitRegistry.json";
        public static readonly string AnomaliesReportFileName = "DetectedAnomalies.json";
        public static readonly string ErrorLogFileName = "SystemInitialization_Errors.txt";
    }
}
