using Microsoft.Data.Sqlite;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static ED_TimeSlide.KeplerOrbitSolver;


namespace ED_TimeSlide
{
    public partial class FormOrbitDiagnosticPlotter : Form
    {
        #region Structures
        public struct RelationalAnchor
        {
            public double SemiMajorAxis;
            public double Eccentricity;
            public double OrbitalPeriod;
            public long AnchorTimestampUnixSec;
            public double AnchorDistance;
            public bool IsClimbingOutward;
            public bool IsRetrograde;
        }
        #endregion

        #region Variable
        private readonly string _connectionString;
        private readonly List<int> _parallelBodyDbIds = new List<int>();
        #endregion

        public FormOrbitDiagnosticPlotter()
        {
            InitializeComponent();
            _connectionString = $"Data Source={Settings.ScanDataDbPath};";

            cmbPlanetSelector.Items.Clear();
            lblPlotterStatus.Text = "[IDLE] Awaiting telemetry selection pass...";
        }

        #region Form Events
        private void FormOrbitDiagnosticPlotter_Load(object sender, EventArgs e)
        {
            PopulatePlanetSelectionComboBox();
        }
        private void BtnNextPlanet_Click(object sender, EventArgs e)
        {
            if (cmbPlanetSelector.Items.Count == 0) return;
            cmbPlanetSelector.SelectedIndex = (cmbPlanetSelector.SelectedIndex >= cmbPlanetSelector.Items.Count - 1) ? 0 : cmbPlanetSelector.SelectedIndex + 1;
        }
        private void BtnPreviousPlanet_Click(object sender, EventArgs e)
        {
            if (cmbPlanetSelector.Items.Count == 0) return;
            cmbPlanetSelector.SelectedIndex = (cmbPlanetSelector.SelectedIndex <= 0) ? cmbPlanetSelector.Items.Count - 1 : cmbPlanetSelector.SelectedIndex - 1;
        }
        private void BtnUpdateRenderCanvas_Click(object sender, EventArgs e)
        {
            // Capture live axis scale vectors prior to database calculation pass resets
            ScottPlot.AxisLimits currentLimits = formsPlotCanvas.Plot.Axes.GetLimits();

            if (cmbPlanetSelector.SelectedItem != null)
            {
                int index = cmbPlanetSelector.SelectedIndex;
                if (index >= 0 && index < _parallelBodyDbIds.Count)
                {
                    int targetedBodyDbId = _parallelBodyDbIds[index];
                    RelationalAnchor anchor;
                    if (TryLoadBodyAnchor(targetedBodyDbId, out anchor))
                    {
                        List<double> excelDates = new List<double>();
                        List<double> distances = new List<double>();
                        LoadBodyTelemetryArrays(targetedBodyDbId, excelDates, distances);

                        if (excelDates.Count > 0)
                        {
                            RenderOrbitDiagnosticCanvas(anchor, excelDates.ToArray(), distances.ToArray(), targetedBodyDbId, currentLimits);
                            return;
                        }
                    }
                }
            }

            CmbPlanetSelector_SelectedIndexChanged(sender, e);
        }
        private void but_UpdateComboBox_Click(object sender, EventArgs e)
        {
            PopulatePlanetSelectionComboBox();
        }
        private void OnZoomModeChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rdo && rdo.Checked) ApplyZoomConstraints();
        }
        private void CmbPlanetSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPlanetSelector.SelectedItem == null) return;

            int index = cmbPlanetSelector.SelectedIndex;
            if (index < 0 || index >= _parallelBodyDbIds.Count) return;

            int targetedBodyDbId = _parallelBodyDbIds[index];
            formsPlotCanvas.Plot.Axes.Rules.Clear();

            if (rdoZoomStandard != null && !rdoZoomStandard.Checked)
            {
                rdoZoomStandard.Checked = true;
            }

            lblPlotterStatus.Text = $"[LOADING] Querying telemetry parameters for BodyDBID: {targetedBodyDbId}...";

            RelationalAnchor anchor;
            if (!TryLoadBodyAnchor(targetedBodyDbId, out anchor))
            {
                lblPlotterStatus.Text = "[ERROR] Selected calibration metrics could not be pulled from ObitInfo.";
                return;
            }

            // Format relational attributes cleanly into single line layout string within txb_BodyInfo control hub
            if (txb_BodyInfo != null)
            {
                txb_BodyInfo.Text = $"BodyDBID: {targetedBodyDbId} | SemiMajorAxis: {anchor.SemiMajorAxis:F2} | Eccentricity: {anchor.Eccentricity:F4} | OrbitalPeriod: {anchor.OrbitalPeriod:F2} | AnchorTimestamp: {anchor.AnchorTimestampUnixSec} | AnchorDistance: {anchor.AnchorDistance:F4} | IsClimbingOutward: {anchor.IsClimbingOutward} | IsRetrograde: {anchor.IsRetrograde}";
            }

            List<double> excelDates = new List<double>();
            List<double> distances = new List<double>();
            LoadBodyTelemetryArrays(targetedBodyDbId, excelDates, distances);

            if (excelDates.Count > 0)
            {
                RenderOrbitDiagnosticCanvas(anchor, excelDates.ToArray(), distances.ToArray(), targetedBodyDbId);
            }
            else
            {
                lblPlotterStatus.Text = "[ERROR] Telemetry extraction fault: No data points logged for this ID.";
            }
        }
        #endregion

        #region Private Functions
        private void PopulatePlanetSelectionComboBox()
        {
            cmbPlanetSelector.SelectedIndexChanged -= CmbPlanetSelector_SelectedIndexChanged;
            cmbPlanetSelector.Items.Clear();
            _parallelBodyDbIds.Clear();

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                // Construct base cross-referenced query linking bodies back to parent system string names
                string query = @"
            SELECT s.SystemName, b.BodyDBID, b.BodyName, COUNT(d.DataPointDBID) as PointCount
            FROM Bodies b
            JOIN ObitInfo o ON b.BodyDBID = o.BodyDBID
            JOIN DataPoints d ON b.BodyDBID = d.BodyDBID
            JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
            WHERE o.AnchorTimestamp_UnixSec IS NOT NULL";

                // Updated check: Swapped from LIKE to an exact case-insensitive match check
                if (chb_SystemNameFilter != null && chb_SystemNameFilter.Checked && !string.IsNullOrWhiteSpace(txb_SystemNameFilter.Text))
                {
                    query += " AND s.SystemName = @SystemPattern COLLATE NOCASE";
                }

                query += @"
            GROUP BY b.BodyDBID
            ORDER BY s.SystemName ASC, b.BodyName ASC;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    if (chb_SystemNameFilter != null && chb_SystemNameFilter.Checked && !string.IsNullOrWhiteSpace(txb_SystemNameFilter.Text))
                    {
                        // Passing the exact string without wrapping it in wildcard '%' bounds
                        cmd.Parameters.AddWithValue("@SystemPattern", txb_SystemNameFilter.Text.Trim());
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int count = Convert.ToInt32(reader["PointCount"]);

                            if (count >= nud_Min_DataFilter.Value && count <= nud_Max_DataFilter.Value)
                            {
                                string systemName = reader["SystemName"].ToString();
                                string bodyName = reader["BodyName"].ToString();
                                int id = Convert.ToInt32(reader["BodyDBID"]);

                                cmbPlanetSelector.Items.Add($"{systemName} | {bodyName} (Points: {count})");
                                _parallelBodyDbIds.Add(id);
                            }
                        }
                    }
                }
            }

            cmbPlanetSelector.SelectedIndexChanged += CmbPlanetSelector_SelectedIndexChanged;
            if (cmbPlanetSelector.Items.Count > 0)
            {
                cmbPlanetSelector.SelectedIndex = 0;
            }
            else
            {
                lblPlotterStatus.Text = "[WARNING] No cross-referenced bodies match within active filter windows.";
            }
        }
        
        private void LoadBodyTelemetryArrays(int bodyId, List<double> excelDates, List<double> distances)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT Timestamp_UnixSec, DistanceToArrival 
                    FROM DataPoints WHERE BodyDBID = @BodyID ORDER BY Timestamp_UnixSec ASC;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long unixSec = Convert.ToInt64(reader["Timestamp_UnixSec"]);
                            double dist = Convert.ToDouble(reader["DistanceToArrival"]);

                            // Converting raw unix epoch timelines to Excel OA Date Doubles for ScottPlot canvas formatting
                            DateTime dt = DateTimeOffset.FromUnixTimeSeconds(unixSec).DateTime;
                            double oaDate = dt.ToOADate();

                            excelDates.Add(oaDate);
                            distances.Add(dist);
                        }
                    }
                }
            }
        }
        private void RenderOrbitDiagnosticCanvas(RelationalAnchor anchor, double[] dbTimestampExcelOA, double[] dbDistanceToArrival, int bodyId, ScottPlot.AxisLimits? preservedZoomLimits = null)
        {
            int totalPointsCount = dbTimestampExcelOA.Length;
            double minimumTimestampXExcelOA = dbTimestampExcelOA.Min();
            double maximumTimestampXExcelOA = dbTimestampExcelOA.Max();

            double plotStartXExcelOA = minimumTimestampXExcelOA - Settings.PaddingTimeCushion;
            double plotEndXExcelOA = maximumTimestampXExcelOA + Settings.PaddingTimeCushion;

            // Separate checking routine handles dynamic manual zoom persistence limits cleanly
            if (preservedZoomLimits.HasValue)
            {
                double focusXMin = preservedZoomLimits.Value.HorizontalRange.Min;
                double focusXMax = preservedZoomLimits.Value.HorizontalRange.Max;
                if (!double.IsNaN(focusXMin) && !double.IsNaN(focusXMax))
                {
                    plotStartXExcelOA = focusXMin;
                    plotEndXExcelOA = focusXMax;
                }
            }

            long startUnixSec = (long)((DateTime.FromOADate(plotStartXExcelOA) - new DateTime(1970, 1, 1)).TotalSeconds);
            long endUnixSec = (long)((DateTime.FromOADate(plotEndXExcelOA) - new DateTime(1970, 1, 1)).TotalSeconds);
            long minTelemetryUnix = (long)((DateTime.FromOADate(minimumTimestampXExcelOA) - new DateTime(1970, 1, 1)).TotalSeconds);
            long maxTelemetryUnix = (long)((DateTime.FromOADate(maximumTimestampXExcelOA) - new DateTime(1970, 1, 1)).TotalSeconds);

            double totalSpanTimeUnixSeconds = maxTelemetryUnix - minTelemetryUnix;
            double completedOrbitCycles = totalSpanTimeUnixSeconds / anchor.OrbitalPeriod;
            double idealCalculatedSteps = completedOrbitCycles * Settings.TargetStepsPerOrbitCycle;

            int graphResolutionIntervals = Math.Max(Settings.MinAdaptiveGraphResolution, Math.Min(Settings.MaxAdaptiveGraphResolution, (int)idealCalculatedSteps));
            double calculatedStepWidthExcelOA = (plotEndXExcelOA - plotStartXExcelOA) / graphResolutionIntervals;

            double[] curveTimestampsExcelOA = new double[graphResolutionIntervals + 1];
            double[] curveExpectedDistancesY = new double[graphResolutionIntervals + 1];
            double[] upperVarianceBoundsY1 = new double[graphResolutionIntervals + 1];
            double[] lowerVarianceBoundsY2 = new double[graphResolutionIntervals + 1];

            var physicalElements = new OrbitalElements
            {
                SemiMajorAxisMetres = anchor.SemiMajorAxis,
                Eccentricity = anchor.Eccentricity,
                OrbitalPeriodSeconds = anchor.OrbitalPeriod,
                AnchorTimestampUnixSec = anchor.AnchorTimestampUnixSec,
                AnchorDistanceLs = anchor.AnchorDistance,
                IsClimbingOutward = anchor.IsClimbingOutward,
                IsRetrograde = anchor.IsRetrograde
            };

            double absoluteMaximumDeviationLS = 0.0;
            double absoluteMaximumDeviationPercent = 0.0;

            var cleanPointsX = new List<double>();
            var cleanPointsY = new List<double>();
            var anomalyPointsX = new List<double>();
            var anomalyPointsY = new List<double>();

            for (int i = 0; i < totalPointsCount; i++)
            {
                long targetUnixSeconds = (long)((DateTime.FromOADate(dbTimestampExcelOA[i]) - new DateTime(1970, 1, 1)).TotalSeconds);
                double predDist = KeplerOrbitSolver.PredictDistanceAtTimestamp(physicalElements, targetUnixSeconds);

                double variance = Math.Abs(dbDistanceToArrival[i] - predDist);
                double variancePct = (predDist > 0.0) ? (variance / predDist) * 100.0 : 0.0;

                if (variancePct > absoluteMaximumDeviationPercent) absoluteMaximumDeviationPercent = variancePct;

                if (variancePct <= Settings.MaxAllowedErrorPercent)
                {
                    cleanPointsX.Add(dbTimestampExcelOA[i]);
                    cleanPointsY.Add(dbDistanceToArrival[i]);
                    if (variance > absoluteMaximumDeviationLS) absoluteMaximumDeviationLS = variance;
                }
                else
                {
                    anomalyPointsX.Add(dbTimestampExcelOA[i]);
                    anomalyPointsY.Add(dbDistanceToArrival[i]);
                }
            }

            for (int step = 0; step <= graphResolutionIntervals; step++)
            {
                double currentStepTimeExcelOA = plotStartXExcelOA + (step * calculatedStepWidthExcelOA);
                long currentUnix = (long)((DateTime.FromOADate(currentStepTimeExcelOA) - new DateTime(1970, 1, 1)).TotalSeconds);

                double expectedRailDistanceLS = KeplerOrbitSolver.PredictDistanceAtTimestamp(physicalElements, currentUnix);

                curveTimestampsExcelOA[step] = currentStepTimeExcelOA;
                curveExpectedDistancesY[step] = expectedRailDistanceLS;
                upperVarianceBoundsY1[step] = expectedRailDistanceLS + absoluteMaximumDeviationLS;
                lowerVarianceBoundsY2[step] = expectedRailDistanceLS - absoluteMaximumDeviationLS;
            }

            formsPlotCanvas.Plot.Clear();

            var shadedVarianceBand = formsPlotCanvas.Plot.Add.FillY(curveTimestampsExcelOA, upperVarianceBoundsY1, lowerVarianceBoundsY2);
            shadedVarianceBand.FillColor = ScottPlot.Color.FromHex("#FFE4B5").WithAlpha(0.4);
            shadedVarianceBand.LineColor = ScottPlot.Color.FromHex("#FFD700").WithAlpha(0.2);

            var smoothBlueCurveRail = formsPlotCanvas.Plot.Add.ScatterLine(curveTimestampsExcelOA, curveExpectedDistancesY);
            smoothBlueCurveRail.LineColor = ScottPlot.Color.FromHex("#1E90FF");
            smoothBlueCurveRail.LineWidth = 1;

            if (cleanPointsX.Count > 0)
            {
                var cleanScatter = formsPlotCanvas.Plot.Add.ScatterPoints(cleanPointsX.ToArray(), cleanPointsY.ToArray());
                cleanScatter.MarkerShape = MarkerShape.Eks;
                cleanScatter.MarkerSize = 7;
                cleanScatter.MarkerColor = ScottPlot.Color.FromHex("#0000FF");
            }

            if (anomalyPointsX.Count > 0)
            {
                var anomalyScatter = formsPlotCanvas.Plot.Add.ScatterPoints(anomalyPointsX.ToArray(), anomalyPointsY.ToArray());
                anomalyScatter.MarkerShape = MarkerShape.Eks;
                anomalyScatter.MarkerSize = 9;
                anomalyScatter.MarkerColor = ScottPlot.Color.FromHex("#FF0000");
            }

            formsPlotCanvas.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlotCanvas.Plot.XLabel("Time (Date Time)");
            formsPlotCanvas.Plot.YLabel("Distance to Arrival (Ls)");

            try
            {
                formsPlotCanvas.Plot.Axes.Margins(Settings.PlotXAxisMargin, Settings.PlotXAxisMargin);

                if (preservedZoomLimits.HasValue && !double.IsNaN(preservedZoomLimits.Value.HorizontalRange.Min))
                {
                    formsPlotCanvas.Plot.Axes.SetLimits(preservedZoomLimits.Value);
                }
                else
                {
                    double minY = Math.Min(curveExpectedDistancesY.Min(), dbDistanceToArrival.Min());
                    double maxY = Math.Max(curveExpectedDistancesY.Max(), dbDistanceToArrival.Max());
                    double height = maxY - minY;
                    double padY = (height > 0.0) ? (height * Settings.PlotYAxisMarginPercent) : 0.05;

                    formsPlotCanvas.Plot.Axes.SetLimitsY(minY - padY, maxY + padY);
                    formsPlotCanvas.Plot.Axes.SetLimitsX(plotStartXExcelOA, plotEndXExcelOA);
                }

                formsPlotCanvas.Refresh();

                lblPlotterStatus.Text = $"[IDLE] Visual plot synchronized successfully. Max Dev: {absoluteMaximumDeviationLS:F4} Ls | Max Error: {absoluteMaximumDeviationPercent:F2}%, Total Points: {totalPointsCount}";
            }
            catch (Exception ex)
            {
                lblPlotterStatus.Text = $"[ERROR] Viewport Scaling Failure: {ex.Message}";
            }
        }
        private void ApplyZoomConstraints()
        {
            formsPlotCanvas.Plot.Axes.Rules.Clear();

            if (rdoZoomYOnly.Checked)
            {
                var lockX = new ScottPlot.AxisRules.LockedHorizontal(formsPlotCanvas.Plot.Axes.Bottom, formsPlotCanvas.Plot.Axes.Bottom.Min, formsPlotCanvas.Plot.Axes.Bottom.Max);
                formsPlotCanvas.Plot.Axes.Rules.Add(lockX);
            }
            else if (rdoZoomXOnly.Checked)
            {
                var lockY = new ScottPlot.AxisRules.LockedVertical(formsPlotCanvas.Plot.Axes.Left, formsPlotCanvas.Plot.Axes.Left.Min, formsPlotCanvas.Plot.Axes.Left.Max);
                formsPlotCanvas.Plot.Axes.Rules.Add(lockY);
            }
            formsPlotCanvas.Refresh();
        }
        #endregion

        // TODO move out, maybe
        private bool TryLoadBodyAnchor(int bodyId, out RelationalAnchor anchor)
        {
            anchor = new RelationalAnchor();

            // Explicitly initialize your local tracking structure to standard parameter safety defaults
            anchor.SemiMajorAxis = 0.0;
            anchor.Eccentricity = 0.0;
            anchor.OrbitalPeriod = 0.0;
            anchor.IsRetrograde = false;
            anchor.AnchorTimestampUnixSec = 0;
            anchor.AnchorDistance = 0.0;
            anchor.IsClimbingOutward = false;

            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, IsRetrograde,
                           AnchorTimestamp_UnixSec, AnchorDistance_Ls, IsClimbingOutward
                    FROM ObitInfo WHERE BodyDBID = @BodyID;";

                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BodyID", bodyId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            anchor.SemiMajorAxis = Convert.ToDouble(reader["SemiMajorAxis"]);
                            anchor.Eccentricity = Convert.ToDouble(reader["Eccentricity"]);
                            anchor.OrbitalPeriod = Convert.ToDouble(reader["OrbitalPeriod_Sec"]);

                            #region Protect Optional Properties from Database DBNull Errors
                            if (reader["IsRetrograde"] != DBNull.Value)
                            {
                                anchor.IsRetrograde = Convert.ToBoolean(reader["IsRetrograde"]);
                            }

                            if (reader["AnchorTimestamp_UnixSec"] != DBNull.Value)
                            {
                                anchor.AnchorTimestampUnixSec = Convert.ToInt64(reader["AnchorTimestamp_UnixSec"]);
                            }

                            if (reader["AnchorDistance_Ls"] != DBNull.Value)
                            {
                                anchor.AnchorDistance = Convert.ToDouble(reader["AnchorDistance_Ls"]);
                            }

                            if (reader["IsClimbingOutward"] != DBNull.Value)
                            {
                                anchor.IsClimbingOutward = Convert.ToBoolean(reader["IsClimbingOutward"]);
                            }
                            #endregion

                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
