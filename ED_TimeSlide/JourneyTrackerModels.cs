using System;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    #region EDDN Polymorphic Travel & Spatial Models
    public class JourneyTimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string StarSystem { get; set; }
        public string BodyName { get; set; }
        public string DetailInfo { get; set; }
    }

    public class EddnGenericMessage
    {
        [JsonProperty("event")]
        public string EventName { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("StarSystem")]
        public string StarSystem { get; set; }

        [JsonProperty("System")]
        public string System { get; set; }

        [JsonProperty("Body")]
        public string Body { get; set; }

        [JsonProperty("BodyName")]
        public string BodyName { get; set; }

        [JsonProperty("StationName")]
        public string StationName { get; set; }

        [JsonProperty("StationType")]
        public string StationType { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("Longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("SystemAddress")]
        public long? SystemAddress { get; set; }

        [JsonProperty("StarPos")]
        public double[] StarPos { get; set; }
    }

    public class EddnGenericRecord
    {
        [JsonProperty("header")]
        public EddnHeader Header { get; set; }

        [JsonProperty("message")]
        public EddnGenericMessage Message { get; set; }
    }
    #endregion
}
