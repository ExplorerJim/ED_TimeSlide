using System;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    // The top-level schema wrapper
    public class EddnScanRecord
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

        // Core continuous orbital parameters for the physics tracker
        [JsonProperty("DistanceFromArrivalLS")]
        public double DistanceFromArrivalLS { get; set; }

        [JsonProperty("SemiMajorAxis")]
        public double SemiMajorAxis { get; set; }

        [JsonProperty("Eccentricity")]
        public double Eccentricity { get; set; }

        [JsonProperty("OrbitalPeriod")]
        public double OrbitalPeriod { get; set; }

        [JsonProperty("StarType")]
        public string StarType { get; set; }

        [JsonProperty("SystemAddress")]
        public long SystemAddress { get; set; }
    }
}
