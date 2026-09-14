using System;
using System.Collections.Generic;

namespace ED_TimeSlide
{
    #region Classes
    /// <summary>
    /// Holds a single, 100% mathematically verified orbital anchor.
    /// This is what gets saved permanently to your Master Registry JSON file on disk.
    /// </summary>
    public class MasterOrbitAnchor
    {
        public double SemiMajorAxis { get; set; }
        public double Eccentricity { get; set; }
        public double OrbitalPeriod { get; set; }

        // Our 1 definitive trusted anchor point
        public long AnchorTimestamp { get; set; } // Unix epoch seconds for easier math
        public double AnchorDistance { get; set; }

        public string VerifiedSourceFile { get; set; }
        public string SoftwareName { get; set; }
        public bool IsClimbingOutward { get; set; }

        public long LastCheckedTimestamp { get; set; }
    }

    /// <summary>
    /// Holds a staging block of raw incoming data points.
    /// Once CollectedPoints hits a count of 5, it triggers the validation math.
    /// </summary>
    public class StagingOrbitBlock
    {
        public double SemiMajorAxis { get; set; }
        public double Eccentricity { get; set; }
        public double OrbitalPeriod { get; set; }

        public List<StagedPoint> CollectedPoints { get; set; } = new List<StagedPoint>();
    }

    public class StagedPoint
    {
        public long Timestamp { get; set; }
        public double Distance { get; set; }
        public string SourceFile { get; set; }
        public string SoftwareName { get; set; }
        public string UploaderId { get; set; }
    }
    #endregion
}
