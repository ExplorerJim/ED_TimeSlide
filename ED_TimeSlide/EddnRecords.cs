using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ED_TimeSlide
{
    #region EDDN Telemetry Scan Models
    public class EddnRecords
    {
        [JsonProperty("header")]
        public EddnHeader Header { get; set; }

        [JsonProperty("message")]
        public ScanMessage Message { get; set; }
    }

    public class EddnHeader
    {
        [JsonProperty("softwareName")]
        public string SoftwareName { get; set; }

        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }
    }

    public class ScanMessage
    {
        [JsonProperty("event")]
        public string EventName { get; set; }

        // Real world time the uploaders PC collected the data
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("StarSystem")]
        public string StarSystem { get; set; }

        [JsonProperty("BodyName")]
        public string BodyName { get; set; }

        [JsonProperty("BodyID")]
        public int BodyId { get; set; }

        [JsonProperty("ScanType")]
        public string ScanType { get; set; }

        [JsonProperty("DistanceFromArrivalLS")]
        public double DistanceFromArrivalLS { get; set; }

        [JsonProperty("Eccentricity")]
        public double Eccentricity { get; set; }

        [JsonProperty("OrbitalPeriod")]
        public double OrbitalPeriod { get; set; }

        [JsonProperty("StarType")]
        public string StarType { get; set; }

        [JsonProperty("PlanetClass")]
        public string PlanetClass { get; set; }

        [JsonProperty("SystemAddress")]
        public long SystemAddress { get; set; }

        [JsonProperty("Parents")]
        public Dictionary<string, int>[] Parents { get; set; }

        // Mapped exclusively for standard planetary/stellar frames
        [JsonProperty("SemiMajorAxis")]
        public double SemiMajorAxis { get; set; }

        // BARYCENTER FIX: Extracts the gravitational node orbital radius fallback parameter 
        [JsonProperty("Axis")]
        public double Axis { get; set; }

        // Negative rotation signifys a retrograde body.
        [JsonProperty("RotationPeriod")]
        public double RotationPeriod { get; set; }
    }
    #endregion
}
