using ED_TimeSlide;
using Newtonsoft.Json;
using System;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public class StagingParserEngine
    {
        public StagingParserEngine()
        {
            // Structural instantiation initialization path
        }

        #region Public Factory Entry Points
        /// <summary>
        /// Parses a raw EDDN log string line. If it contains valid planetary or 
        /// stellar scan data, it extracts a fully mapped and sanitized EddnRecords object.
        /// </summary>
        public EddnRecords ParseScanLine(string textLine, Action<string, string> logErrorCallback)
        {
            #region Section 3.3 Text Containment Pre-Filter Gate
            if (string.IsNullOrWhiteSpace(textLine)) return null;

            bool isScanEvent = textLine.Contains("\"event\":\"Scan\"") ||
                              textLine.Contains("\"event\": \"Scan\"");

            if (!isScanEvent) return null;
            #endregion

            #region Core Object Deserialization Loop
            try
            {
                var record = JsonConvert.DeserializeObject<EddnRecords>(textLine);
                if (record?.Message == null || record.Message.EventName != "Scan") return null;

                #region Section 3.1 Dynamic Axis Key Fallback Handshake
                if (record.Message.SemiMajorAxis == 0 && record.Message.Axis > 0)
                {
                    record.Message.SemiMajorAxis = record.Message.Axis;
                }
                #endregion

                #region Execute Telemetry Sanitization Sweep
                SanitizeScanMessageStrings(record.Message);
                #endregion

                return record;
            }
            catch (JsonException ex)
            {
                #region Exploded Protocol Error Reporting Intercept
                if (logErrorCallback != null)
                {
                    logErrorCallback("Scan Serialization Fault", ex.Message);
                }
                #endregion
                return null;
            }
            #endregion
        }
        #endregion

        #region Private Internal Sanitization Processors
        private void SanitizeScanMessageStrings(ScanMessage msg)
        {
            #region Section 3.3 Clean and Validate Core String Primitives
            string targetSystem = (!string.IsNullOrEmpty(msg.StarSystem) ? msg.StarSystem : Settings.UnknownData).Trim();
            string targetBody = (!string.IsNullOrEmpty(msg.BodyName) ? msg.BodyName : Settings.UnknownData).Trim();

            if (!string.IsNullOrEmpty(msg.ScanType)) msg.ScanType = msg.ScanType.Trim();
            if (!string.IsNullOrEmpty(msg.StarType)) msg.StarType = msg.StarType.Trim();
            if (!string.IsNullOrEmpty(msg.PlanetClass)) msg.PlanetClass = msg.PlanetClass.Trim();
            #endregion

            #region Section 3.3 System Name Prefix Pruning Matrix
            if (targetBody.StartsWith(targetSystem, StringComparison.OrdinalIgnoreCase))
            {
                targetBody = targetBody.Substring(targetSystem.Length).Trim();
            }
            #endregion

            #region Assign Optimized String Tokens Back To Schema
            msg.StarSystem = targetSystem;
            msg.BodyName = targetBody;
            #endregion
        }
        #endregion

        #region Public Hierarchy Filtering Verification
        /// <summary>
        /// Audits the parsed record parents hierarchy tree against the spec guidelines.
        /// Returns true if the body orbits the primary star directly.
        /// </summary>
        #region Public Hierarchy Filtering Verification Suite
        public bool IsPrimaryStarOrbiter(ScanMessage msg, Action<string, string> logErrorCallback)
        {
            #region Verify Entity Has Valid Property Allocations
            if (msg == null) return false;
            #endregion

            #region If it's a primary star it has no parents
            if (msg.Parents == null || msg.Parents.Length == 0)
            {
                return false;
            }
            #endregion

            #region Evaluate Master Root System Anchors
            if (msg.Parents == null || msg.Parents.Length == 0)
            {
                return true;
            }
            #endregion

            #region Evaluate Master Root System Anchors
            if (msg.Parents.Length != 1)
            {
                return false;
            }
            #endregion

            try
            {
                #region Isolate Absolute First Indexed Node Matrix Layer
                var primaryParentNode = msg.Parents[0];
                #endregion

                #region Future_Barycenter_Triangulation
                if (primaryParentNode.ContainsKey("Null"))
                {
                    // TODO: Insert Phase 2 barycenter 3D vector triangulation logic here
                    return false;
                }
                #endregion

                #region Filter Nested Planetary Moons & Satellites
                if (primaryParentNode.ContainsKey("Planet"))
                {
                    return false;
                }
                #endregion

                #region Confirm Direct Primary Star Trajectory
                if (primaryParentNode.TryGetValue("Star", out int starIdentifierValue))
                {
                    try
                    {
                        // Approach A: Enforce absolute zero value validation boundaries
                        if (starIdentifierValue == 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logErrorCallback != null)
                        {
                            logErrorCallback("Parent Structure Verification Crash", ex.Message);
                        }
                        return false;
                    }
                }
                #endregion
            }
            catch
            {
                #region Fallback Structural Error Recovery Exception Block
                return false;
                #endregion
            }

            return false;
        }
        #endregion

        #endregion
    }
}