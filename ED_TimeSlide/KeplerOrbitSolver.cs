using System;
using System.Runtime.CompilerServices;

namespace ED_TimeSlide
{
    public static class KeplerOrbitSolver
    {
        #region Structs
        public struct OrbitalElements
        {
            public double SemiMajorAxisMetres;
            public double Eccentricity;
            public double OrbitalPeriodSeconds;
            public long AnchorTimestampUnixSec;
            public double AnchorDistanceLs;
            public bool IsClimbingOutward;
            public bool IsRetrograde;
        }
        #endregion        

        #region Public Functions
        /// <summary>
        /// Predicts the exact physical distance (in Light Seconds) a body should be from its 
        /// parent star at a specific target timestamp, grounded by a known valid reference anchor.
        /// </summary>
        public static double PredictDistanceAtTimestamp(OrbitalElements input, long targetTimestampUnixSec)
        {
            // 1. Convert anchor distance to metres for unified math scaling
            double anchorDistanceMetres = input.AnchorDistanceLs * Settings.SpeedOfLightMetersPerSecond;

            // 2. Reverse engineer the starting Mean Anomaly (M0) from our trusted anchor point
            double cosE = (1.0 - (anchorDistanceMetres / input.SemiMajorAxisMetres)) / input.Eccentricity;

            // Clamp value to handle floating-point rounding precision safety bounds
            cosE = Math.Max(-1.0, Math.Min(1.0, cosE));
            double eccentricAnomalyAnchor = Math.Acos(cosE);

            if (!input.IsClimbingOutward)
            {
                eccentricAnomalyAnchor = (2.0 * Math.PI) - eccentricAnomalyAnchor;
            }

            double meanAnomalyAnchor = eccentricAnomalyAnchor - input.Eccentricity * Math.Sin(eccentricAnomalyAnchor);

            // 3. Calculate elapsed time delta and find the new Mean Anomaly for our target time step
            double deltaTimeSeconds = targetTimestampUnixSec - input.AnchorTimestampUnixSec;
            double meanMotion = (2.0 * Math.PI) / input.OrbitalPeriodSeconds;

            if (input.IsRetrograde)
            {
                meanMotion = -meanMotion;
            }

            double targetMeanAnomaly = meanAnomalyAnchor + (meanMotion * deltaTimeSeconds);

            // Normalize angle boundaries within 0 to 2*PI radians
            targetMeanAnomaly = targetMeanAnomaly % (2.0 * Math.PI);
            if (targetMeanAnomaly < 0) targetMeanAnomaly += 2.0 * Math.PI;

            // 4. Solve Kepler's Transcendental Equation (M = E - e*sinE) using the Newton-Raphson method
            double targetEccentricAnomaly = targetMeanAnomaly; // Initial approximation guess
            for (int i = 0; i < 6; i++) // 5-6 iterations achieve extreme high-precision thresholds
            {
                double deltaE = (targetEccentricAnomaly - input.Eccentricity * Math.Sin(targetEccentricAnomaly) - targetMeanAnomaly)
                                / (1.0 - input.Eccentricity * Math.Cos(targetEccentricAnomaly));
                targetEccentricAnomaly -= deltaE;
                if (Math.Abs(deltaE) < 1e-7) break;
            }

            // 5. Calculate precise expected distance in metres, then scale back down to Light Seconds
            double expectedDistanceMetres = input.SemiMajorAxisMetres * (1.0 - input.Eccentricity * Math.Cos(targetEccentricAnomaly));
            return expectedDistanceMetres / Settings.SpeedOfLightMetersPerSecond;
        }

        /// <summary>
        /// Reverse-engineers a reported distance to find the exact Unix timestamp in history
        /// when the planet naturally occupied that specific orbital coordinate position.
        /// </summary>
        public static long? SolveGhostTimestamp(OrbitalElements input, double reportedDistanceLs)
        {
            double anchorDistanceMetres = input.AnchorDistanceLs * Settings.SpeedOfLightMetersPerSecond;
            double reportedDistanceMetres = reportedDistanceLs * Settings.SpeedOfLightMetersPerSecond;

            // 1. Back-calculate the candidate anchor angle (E_anchor)
            double cosE_anchor = (1.0 - (anchorDistanceMetres / input.SemiMajorAxisMetres)) / input.Eccentricity;
            cosE_anchor = Math.Max(-1.0, Math.Min(1.0, cosE_anchor));
            double E_anchor = Math.Acos(cosE_anchor);
            if (!input.IsClimbingOutward) E_anchor = (2.0 * Math.PI) - E_anchor;
            double M_anchor = E_anchor - input.Eccentricity * Math.Sin(E_anchor);

            // 2. Back-calculate the reported anomaly angle (E_reported)
            double cosE_reported = (1.0 - (reportedDistanceMetres / input.SemiMajorAxisMetres)) / input.Eccentricity;

            // If the reported distance is physically impossible for this ellipse, it's a glitch, not time travel
            if (cosE_reported < -1.0 || cosE_reported > 1.0) return null;

            double E_reported = Math.Acos(cosE_reported);
            if (!input.IsClimbingOutward) E_reported = (2.0 * Math.PI) - E_reported;
            double M_reported = E_reported - input.Eccentricity * Math.Sin(E_reported);

            // 3. Find the Mean Anomaly difference
            double deltaM = M_reported - M_anchor;

            // Normalize deltaM between -PI and +PI to find the closest historical time step
            while (deltaM > Math.PI) deltaM -= (2.0 * Math.PI);
            while (deltaM < -Math.PI) deltaM += (2.0 * Math.PI);

            // 4. Convert the anomaly shift back into real-world seconds passed
            double meanMotion = (2.0 * Math.PI) / input.OrbitalPeriodSeconds;
            double timeOffsetSeconds = deltaM / meanMotion;

            // Return the calculated Ghost Timestamp relative to your anchor time line
            return input.AnchorTimestampUnixSec + (long)timeOffsetSeconds;
        }

        /// <summary>
        /// Extension helper to convert standard ISO DateTime structures straight into clean Unix Epoch Seconds.
        /// </summary>
        public static long ToUnixSeconds(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
        public static long ToUnixSeconds(this double dateTimeExcelOA)
        {
            return (long)((dateTimeExcelOA-25569) * 86400.0);
        }
        public static long ToExcelOA(this double dateTimeUnixSec)
        {
            return (long)(dateTimeUnixSec / 86400.0) + 25569;
        }
        #endregion
    }
}
