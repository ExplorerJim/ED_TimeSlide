using System;

namespace ED_TimeSlide
{
    public static class KeplerOrbitSolver
    {
        // Speed of Light constant used to convert Light Seconds into Metres
        private const double SpeedOfLightMetersPerSecond = 299792458.0;

        /// <summary>
        /// Predicts the exact physical distance (in Light Seconds) a body should be from its 
        /// parent star at a specific target timestamp, grounded by a known valid reference anchor.
        /// </summary>
        public static double PredictDistanceAtTimestamp(
            double semiMajorAxisMetres,
            double eccentricity,
            double orbitalPeriodSeconds,
            long anchorTimestamp,
            double anchorDistanceLs,
            long targetTimestamp,
            bool isClimbingOutward)
        {
            // 1. Convert anchor distance to metres for unified math scaling
            double anchorDistanceMetres = anchorDistanceLs * SpeedOfLightMetersPerSecond;

            // 2. Reverse engineer the starting Mean Anomaly (M0) from our trusted anchor point
            double cosE = (1.0 - (anchorDistanceMetres / semiMajorAxisMetres)) / eccentricity;

            // Clamp value to handle floating-point rounding precision safety bounds
            cosE = Math.Max(-1.0, Math.Min(1.0, cosE));
            double eccentricAnomalyAnchor = Math.Acos(cosE);

            if (!isClimbingOutward)
            {
                eccentricAnomalyAnchor = (2.0 * Math.PI) - eccentricAnomalyAnchor;
            }

            double meanAnomalyAnchor = eccentricAnomalyAnchor - eccentricity * Math.Sin(eccentricAnomalyAnchor);

            // 3. Calculate elapsed time delta and find the new Mean Anomaly for our target time step
            double deltaTimeSeconds = targetTimestamp - anchorTimestamp;
            double meanMotion = (2.0 * Math.PI) / orbitalPeriodSeconds;
            double targetMeanAnomaly = meanAnomalyAnchor + (meanMotion * deltaTimeSeconds);

            // Normalize angle boundaries within 0 to 2*PI radians
            targetMeanAnomaly = targetMeanAnomaly % (2.0 * Math.PI);
            if (targetMeanAnomaly < 0) targetMeanAnomaly += 2.0 * Math.PI;

            // 4. Solve Kepler's Transcendental Equation (M = E - e*sinE) using the Newton-Raphson method
            double targetEccentricAnomaly = targetMeanAnomaly; // Initial approximation guess
            for (int i = 0; i < 6; i++) // 5-6 iterations achieve extreme high-precision thresholds
            {
                double deltaE = (targetEccentricAnomaly - eccentricity * Math.Sin(targetEccentricAnomaly) - targetMeanAnomaly)
                                / (1.0 - eccentricity * Math.Cos(targetEccentricAnomaly));
                targetEccentricAnomaly -= deltaE;
                if (Math.Abs(deltaE) < 1e-7) break;
            }

            // 5. Calculate precise expected distance in metres, then scale back down to Light Seconds
            double expectedDistanceMetres = semiMajorAxisMetres * (1.0 - eccentricity * Math.Cos(targetEccentricAnomaly));
            return expectedDistanceMetres / SpeedOfLightMetersPerSecond;
        }

        /// <summary>
        /// Extension helper to convert standard ISO DateTime structures straight into clean Unix Epoch Seconds.
        /// </summary>
        public static long ToUnixSeconds(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// Reverse-engineers a reported distance to find the exact Unix timestamp in history
        /// when the planet naturally occupied that specific orbital coordinate position.
        /// </summary>
        public static long? SolveGhostTimestamp(
            double semiMajorAxisMetres,
            double eccentricity,
            double orbitalPeriodSeconds,
            long anchorTimestamp,
            double anchorDistanceLs,
            double reportedDistanceLs,
            bool isClimbingOutward)
        {
            double anchorDistanceMetres = anchorDistanceLs * SpeedOfLightMetersPerSecond;
            double reportedDistanceMetres = reportedDistanceLs * SpeedOfLightMetersPerSecond;

            // 1. Back-calculate the candidate anchor angle (E_anchor)
            double cosE_anchor = (1.0 - (anchorDistanceMetres / semiMajorAxisMetres)) / eccentricity;
            cosE_anchor = Math.Max(-1.0, Math.Min(1.0, cosE_anchor));
            double E_anchor = Math.Acos(cosE_anchor);
            if (!isClimbingOutward) E_anchor = (2.0 * Math.PI) - E_anchor;
            double M_anchor = E_anchor - eccentricity * Math.Sin(E_anchor);

            // 2. Back-calculate the reported anomaly angle (E_reported)
            double cosE_reported = (1.0 - (reportedDistanceMetres / semiMajorAxisMetres)) / eccentricity;

            // If the reported distance is physically impossible for this ellipse, it's a glitch, not time travel
            if (cosE_reported < -1.0 || cosE_reported > 1.0) return null;

            double E_reported = Math.Acos(cosE_reported);
            if (!isClimbingOutward) E_reported = (2.0 * Math.PI) - E_reported;
            double M_reported = E_reported - eccentricity * Math.Sin(E_reported);

            // 3. Find the Mean Anomaly difference
            double deltaM = M_reported - M_anchor;

            // Normalize deltaM between -PI and +PI to find the closest historical time step
            while (deltaM > Math.PI) deltaM -= (2.0 * Math.PI);
            while (deltaM < -Math.PI) deltaM += (2.0 * Math.PI);

            // 4. Convert the anomaly shift back into real-world seconds passed
            double meanMotion = (2.0 * Math.PI) / orbitalPeriodSeconds;
            double timeOffsetSeconds = deltaM / meanMotion;

            // Return the calculated Ghost Timestamp relative to your anchor time line
            return anchorTimestamp + (long)timeOffsetSeconds;
        }

    }
}
