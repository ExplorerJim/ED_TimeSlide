using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZedGraph; // Requires the ZedGraph package you installed earlier
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    public static class TrendVisualizer
    {
/*
        // ─── CONFIGURATION CONSTANTS ───
        private const int TargetPointsPerSide = 15; // 15 points before, 15 after
        private const int MaxFileSearchDepth = 5;   // Look max 5 days/files in each direction

        public class TrendDataPoint
        {
            public DateTime Timestamp { get; set; }
            public double Distance { get; set; }
            public bool IsAnomaly { get; set; }
        }

        // Main entry point to generate the graph for a specific detection
        public static async Task<string> GenerateTrendGraphAsync(
            string systemName,
            string bodyName,
            DateTime anomalyTime,
            double anomalyDistance,
            MasterOrbitAnchor anchor,
            string outputFolder)
        {
            // 1. COLLECT DATA (Blue Points)
            List<TrendDataPoint> collectedPoints = await GatherSurroundingPointsAsync(systemName, bodyName, anomalyTime);

            // Add our specific anomaly point (Red Point) if not already in the set
            if (!collectedPoints.Any(p => Math.Abs((p.Timestamp - anomalyTime).TotalSeconds) < 1))
            {
                collectedPoints.Add(new TrendDataPoint { Timestamp = anomalyTime, Distance = anomalyDistance, IsAnomaly = true });
            }

            // Sort chronologically
            collectedPoints = collectedPoints.OrderBy(p => p.Timestamp).ToList();

            if (collectedPoints.Count < 2) return ""; // Not enough data to plot

            // 2. GENERATE CURVES & RENDER (ZedGraph)
            string imagePath = Path.Combine(outputFolder, $"Trend_{systemName}_{bodyName}_{anomalyTime:yyyyMMdd-HHmmss}.png");
            RenderGraph(collectedPoints, anchor, systemName, bodyName, imagePath);

            return imagePath;
        }
        private static async Task<List<TrendDataPoint>> GatherSurroundingPointsAsync(string targetSystem, string targetBody, DateTime centerDate)
        {
            List<TrendDataPoint> history = new List<TrendDataPoint>();
            int pointsBefore = 0;
            int pointsAfter = 0;

            // Define the search range: 0 = current day, -1 = yesterday, +1 = tomorrow, etc.
            // We search in an alternating pattern: 0, -1, +1, -2, +2...
            var searchOffsets = new List<int> { 0 };
            for (int i = 1; i <= MaxFileSearchDepth; i++)
            {
                searchOffsets.Add(-i);
                searchOffsets.Add(i);
            }

            foreach (int offset in searchOffsets)
            {
                // Optimization: Stop searching a direction if we already hit the target count
                if (offset < 0 && pointsBefore >= TargetPointsPerSide) continue;
                if (offset > 0 && pointsAfter >= TargetPointsPerSide) continue;

                DateTime targetDate = centerDate.AddDays(offset);
                string dateString = targetDate.ToString("yyyy-MM-dd");

                // Reuse your existing Phase 2 logic here to fetch the file lines!
                // (Assuming we refactor Phase 2's downloader into a reusable helper method 'FetchRawLinesForDate')
                List<string> fileLines = await WebDataHelper.FetchRawJsonLinesForDateAsync(dateString);

                foreach (string line in fileLines)
                {
                    if (!line.Contains(targetSystem) || !line.Contains(targetBody)) continue;

                    try
                    {
                        var record = JsonConvert.DeserializeObject<EddnScanRecord>(line);
                        // Validate it's the correct body and a Scan event
                        if (record?.Message != null &&
                            record.Message.EventName == "Scan" &&
                            record.Message.StarSystem == targetSystem &&
                            record.Message.BodyName == targetBody)
                        {
                            var pt = new TrendDataPoint
                            {
                                Timestamp = record.Message.Timestamp,
                                Distance = record.Message.DistanceFromArrivalLS,
                                IsAnomaly = false
                            };

                            history.Add(pt);

                            if (pt.Timestamp < centerDate) pointsBefore++;
                            else pointsAfter++;
                        }
                    }
                    catch { }
                }
            }

            return history;
        }
        private static void RenderGraph(List<TrendDataPoint> points, MasterOrbitAnchor anchor, string system, string body, string savePath)
        {
            // Setup Canvas
            GraphPane myPane = new GraphPane(new RectangleF(0, 0, 1200, 600), $"{system} - {body} : Orbital Verification", "Time (UTC)", "Distance (LS)");

            // Data Containers
            PointPairList listCollected = new PointPairList(); // Blue
            PointPairList listCalculated = new PointPairList(); // Orange
            PointPairList listAnomalies = new PointPairList(); // Red

            // Determine Time Range for the Calculation Curve
            double minTime = points.First().Timestamp.ToOADate();
            double maxTime = points.Last().Timestamp.ToOADate();

            // 1. Plot Collected Data (Blue) & Anomalies (Red)
            foreach (var p in points)
            {
                double xDate = p.Timestamp.ToOADate();

                if (p.IsAnomaly)
                {
                    listAnomalies.Add(xDate, p.Distance);
                }
                else
                {
                    listCollected.Add(xDate, p.Distance);
                }
            }

            // 2. Generate Calculated Curve (Orange) - "The Perfect Orbit"
            // We generate 100 resolution points across the visible time range for a smooth curve
            long startUnix = points.First().Timestamp.ToUnixSeconds();
            long endUnix = points.Last().Timestamp.ToUnixSeconds();
            long step = (endUnix - startUnix) / 100;
            if (step < 1) step = 1;

            for (long t = startUnix; t <= endUnix; t += step)
            {
                double predictedDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                    anchor.SemiMajorAxis, anchor.Eccentricity, anchor.OrbitalPeriod,
                    anchor.AnchorTimestamp, anchor.AnchorDistance, t, anchor.IsClimbingOutward
                );

                listCalculated.Add(DateTimeOffset.FromUnixTimeSeconds(t).UtcDateTime.ToOADate(), predictedDist);
            }

            // 3. Style the Lines
            LineItem curveCalc = myPane.AddCurve("Theoretical Orbit", listCalculated, Color.Orange, SymbolType.None);
            curveCalc.Line.Width = 2.0f;
            curveCalc.Line.IsAntiAlias = true;

            LineItem curveReal = myPane.AddCurve("EDDN History", listCollected, Color.Blue, SymbolType.Circle);
            curveReal.Line.IsVisible = false; // Don't connect dots, just show points
            curveReal.Symbol.Fill = new Fill(Color.Blue);
            curveReal.Symbol.Size = 4;

            LineItem curveBad = myPane.AddCurve("Anomaly Detection", listAnomalies, Color.Red, SymbolType.XCross);
            curveBad.Line.IsVisible = false;
            curveBad.Symbol.Size = 8;
            curveBad.Symbol.Border.Width = 2.0f;

            // Format Axis as Date
            myPane.XAxis.Type = AxisType.Date;
            myPane.XAxis.Scale.Format = "MM-dd HH:mm";

            // Save
            using (Bitmap bm = myPane.GetImage())
            {
                bm.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    */
    }
}
