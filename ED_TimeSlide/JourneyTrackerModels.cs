using System;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    public class JourneyTimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string StarSystem { get; set; }
        public string BodyName { get; set; }
        public string DetailInfo { get; set; } // Stores things like StationName, CarrierID, or Lat/Long
    }

    public class EddnGenericMessage
    {
        // FIX: Solves the reserved keyword error by using an alias property map
        [JsonProperty("event")]
        public string EventName { get; set; }

        // FIX: Restores the missing timestamp property definition mapping target
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

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("Longitude")]
        public double? Longitude { get; set; }
    }

    public class EddnGenericRecord
    {
        [JsonProperty("header")]
        public EddnHeader Header { get; set; }

        [JsonProperty("message")]
        public EddnGenericMessage Message { get; set; }
    }
}
