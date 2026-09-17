using System;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    public class ForensicParserEngine
    {
        public ForensicParserEngine()
        {
            // Extensible object factory initialization path
        }

        #region Public Timeline Extraction Entry Points
        public JourneyTimelineEvent ParseTimelineLine(string textLine,string targetUploaderId,Action<string, string> logErrorCallback)
        {
            #region Section 3.3 Target Commander Identity Guard Check
            if (string.IsNullOrWhiteSpace(textLine)) return null;
            if (string.IsNullOrWhiteSpace(targetUploaderId)) return null;

            if (!textLine.Contains(targetUploaderId)) return null;
            #endregion

            #region Process Macro Travel Transition Vectors
            bool isTravelEvent = textLine.Contains("\"event\":\"FSDJump\"") ||
                                 textLine.Contains("\"event\": \"FSDJump\"") ||
                                 textLine.Contains("\"event\":\"CarrierJump\"") ||
                                 textLine.Contains("\"event\": \"CarrierJump\"") ||
                                 textLine.Contains("\"event\":\"Docked\"") ||
                                 textLine.Contains("\"event\": \"Docked\"");

            if (isTravelEvent)
            {
                try
                {
                    var record = JsonConvert.DeserializeObject<EddnGenericRecord>(textLine);
                    if (record?.Message == null) return null;

                    var msg = record.Message;

                    #region Resolve and Trim Invariant Star System Tokens
                    string system = !string.IsNullOrEmpty(msg.StarSystem) ? msg.StarSystem
                                  : !string.IsNullOrEmpty(msg.System) ? msg.System
                                  : "Unknown System";

                    system = system.Trim();
                    #endregion

                    #region Compile Lean Sequencer Metadata Properties
                    string details = "Hyperspace Transit";
                    if (!string.IsNullOrEmpty(msg.StationName))
                    {
                        details = $"Docked at: {msg.StationName.Trim()}";
                    }
                    if (!string.IsNullOrEmpty(msg.StationType))
                    {
                        details += $" [{msg.StationType.Trim()}]";
                    }
                    #endregion

                    return new JourneyTimelineEvent
                    {
                        Timestamp = msg.Timestamp,
                        EventType = msg.EventName ?? "TravelSequence",
                        StarSystem = system,
                        BodyName = !string.IsNullOrEmpty(msg.BodyName) ? msg.BodyName.Trim() : "",
                        DetailInfo = details
                    };
                }
                catch (JsonException ex)
                {
                    #region Exploded Protocol Error Reporting Intercept
                    if (logErrorCallback != null)
                    {
                        logErrorCallback("Travel Serialization Fault", ex.Message);
                    }
                    #endregion
                    return null;
                }
            }
            #endregion

            #region Route Extensible Control Handoff down to Block 2
            return ParseSpatialAnchorLine(textLine, logErrorCallback);
            #endregion
        }
        #endregion

        #region Private Internal Geographic Anchor Processing Suit
        private JourneyTimelineEvent ParseSpatialAnchorLine(string textLine, Action<string, string> logErrorCallback)
        {
            #region Section 3.3 Spatial Anchor Position Filter Gates
            bool isAnchorEvent = textLine.Contains("\"event\":\"Location\"") ||
                                 textLine.Contains("\"event\": \"Location\"") ||
                                 textLine.Contains("\"event\":\"ApproachSettlement\"") ||
                                 textLine.Contains("\"event\": \"ApproachSettlement\"");

            if (!isAnchorEvent) return null;
            #endregion

            #region Core Geographic Object Extraction Loop
            try
            {
                var record = JsonConvert.DeserializeObject<EddnGenericRecord>(textLine);
                if (record?.Message == null) return null;

                var msg = record.Message;

                #region Extract Base System Matrix Identity Keys
                string system = !string.IsNullOrEmpty(msg.StarSystem) ? msg.StarSystem
                              : !string.IsNullOrEmpty(msg.System) ? msg.System
                              : "Unknown System";

                system = system.Trim();
                #endregion

                #region Section 2.2 System Name Prefix Pruning Sweep
                string rawBody = !string.IsNullOrEmpty(msg.BodyName) ? msg.BodyName
                               : !string.IsNullOrEmpty(msg.Body) ? msg.Body
                               : "Primary System Anchor";

                rawBody = rawBody.Trim();

                if (rawBody.StartsWith(system, StringComparison.OrdinalIgnoreCase))
                {
                    rawBody = rawBody.Substring(system.Length).Trim();
                }
                #endregion

                #region Compile Coordinate Tracking Metadata Properties
                string spatialSummary = "Galactic Frame Lock";

                if (msg.StarPos != null && msg.StarPos.Length == 3)
                {
                    spatialSummary = $"StarPos:[{msg.StarPos[0]:F2}, {msg.StarPos[1]:F2}, {msg.StarPos[2]:F2}]";
                }

                if (msg.Latitude.HasValue && msg.Longitude.HasValue)
                {
                    spatialSummary += $" | SurfaceCoords:({msg.Latitude.Value:F4}, {msg.Longitude.Value:F4})";
                }

                if (!string.IsNullOrEmpty(msg.Name))
                {
                    spatialSummary += $" | Site: {msg.Name.Trim()}";
                }
                #endregion

                return new JourneyTimelineEvent
                {
                    Timestamp = msg.Timestamp,
                    EventType = msg.EventName ?? "SpatialAnchor",
                    StarSystem = system,
                    BodyName = rawBody,
                    DetailInfo = spatialSummary
                };
            }
            catch (JsonException ex)
            {
                #region Diagnostic Exception Catch Logging
                if (logErrorCallback != null)
                {
                    logErrorCallback("Spatial Serialization Fault", ex.Message);
                }
                #endregion
                return null;
            }
            #endregion
        }
        #endregion

    }
}
