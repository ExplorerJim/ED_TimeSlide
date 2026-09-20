using OpenTK.Graphics.OpenGL;
using ScottPlot;
using ScottPlot.Statistics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ED_TimeSlide.ScanDataStorageDriver;

namespace ED_TimeSlide
{
    public partial class FormOrbitDiagnosticPlotter : Form
    {
        #region Dedicated UI Field Variables
        private readonly OrbitRegistry inst_OrbitRegistry;

        private readonly StagingParserEngine stagingParser = new StagingParserEngine();
        private readonly List<string> parallelRegistryKeys = new List<string>();

        private enum AxisZoomMode { Standard2D, XTimeOnly, YDistanceOnly }
        #endregion

        public FormOrbitDiagnosticPlotter(OrbitRegistry passedRegistry)
        {
            InitializeComponent();

            #region Assign Central Decoupled Database Reference Hand-off
            inst_OrbitRegistry = passedRegistry ??
                throw new ArgumentNullException(nameof(passedRegistry));
            #endregion

            #region 2. Clear out any design-time dummy options safely
            cmbPlanetSelector.Items.Clear();
            #endregion

            #region Default Control Interface Values
            lblPlotterStatus.Text = "[IDLE] Awaiting target file selection...";
            //txtFolderPath.Text = @"E:\Elite Dangerous\EDDN data\Processed\Test\Journal.Scan-2025-10-29.EDD";
            #endregion
        }
        #region User Interface Navigation Click Event Registries
        private void CmbPlanetSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region Guard Matrix Checks for Selection State Verification
            if (cmbPlanetSelector.SelectedItem == null) return;
            #endregion

            #region Extract Selected Index Coordinate Context
            int targetedSelectionIndex = cmbPlanetSelector.SelectedIndex;
            #endregion

            #region Guard Array Boundaries Against Out Of Bounds Sync Faults
            if (targetedSelectionIndex < 0 || targetedSelectionIndex >= parallelRegistryKeys.Count)
            {
                lblPlotterStatus.Text = "[ERROR] Selection index mapping disconnect occurred";
                return;
            }
            #endregion

            #region Clear persistent ScottPlot constraint rule filters inherited from prior closeups
            formsPlotCanvas.Plot.Axes.Rules.Clear();
            #endregion

            #region Synchronizes interface controls deck back onto standard unconstrained free 2D rules
            if (rdoZoomStandard != null && !rdoZoomStandard.Checked)
            {
                rdoZoomStandard.Checked = true;
            }
            #endregion

            #region  Perform Constant Time Key Retrieval Pass
            string targetRegistryKey = parallelRegistryKeys[targetedSelectionIndex];

            if (!inst_OrbitRegistry.TryGetMasterAnchor(targetRegistryKey, out MasterOrbitAnchor anchor))
            {
                lblPlotterStatus.Text = "[ERROR] Selected key not found inside Master Orbit Registry";
                return;
            }
            #endregion

            #region Update High-Speed Byte-Seeking Status Text Terminal
            lblPlotterStatus.Text = $"[LOADING] Byte seeking flat log records for: {cmbPlanetSelector.SelectedItem}...";
            #endregion

            #region Collect Data points from scanDB
            long systemAddress = -1;
            long bodyID = -1;
            if(inst_OrbitRegistry.SplitKeyintoIDs(targetRegistryKey, out systemAddress, out bodyID))
            { 
                CelestialDataset dataset = ScanDataStorageDriver.GetCelestialDataset(systemAddress, bodyID);
                if ((dataset.Metadata.SystemName != Settings.UnknownData) && (dataset.Metadata.BodyName != Settings.UnknownData)
                    && (dataset.DistancesToArrival.Length > 0) && (dataset.TimestampsExcelOA.Length > 0))
                {
                    RenderOrbitDiagnosticCanvas(anchor, dataset.TimestampsExcelOA, dataset.DistancesToArrival, targetRegistryKey);
                }
                else
                {
                    lblPlotterStatus.Text = $"[ERROR] Interface Chart Render Fault: No data avalible in ScanDB for this body";
                }
                
            }
            else
            {
                lblPlotterStatus.Text = $"[ERROR] Interface Chart Render Fault: Could not get systemAddress and BodyID from orbitRegistry";
            }
            #endregion
        }

        #region User Interface Navigation Click Event Registries
        private void BtnPreviousPlanet_Click(object sender, EventArgs e)
        {
            #region Decrement Dropdown Index or Wrap Back to Tail Limit Safely
            if (cmbPlanetSelector.Items.Count > 0)
            {
                if (cmbPlanetSelector.SelectedIndex <= 0)
                {
                    cmbPlanetSelector.SelectedIndex = cmbPlanetSelector.Items.Count - 1;
                }
                else
                {
                    cmbPlanetSelector.SelectedIndex--;
                }

                CmbPlanetSelector_SelectedIndexChanged(sender, e);
            }
            #endregion
        }
        private void BtnNextPlanet_Click(object sender, EventArgs e)
        {
            #region Increment Dropdown Index or Loop Back to Initial Zero Position
            if (cmbPlanetSelector.Items.Count > 0)
            {
                if (cmbPlanetSelector.SelectedIndex >= cmbPlanetSelector.Items.Count - 1)
                {
                    cmbPlanetSelector.SelectedIndex = 0;
                }
                else
                {
                    cmbPlanetSelector.SelectedIndex++;
                }

                CmbPlanetSelector_SelectedIndexChanged(sender, e);
            }
            #endregion
        }
        #endregion
        #endregion
        private void RenderOrbitDiagnosticCanvas(MasterOrbitAnchor anchor, double[] dbTimestampExcelOA, double[] dbDistanceToArrival, string activeRegistryKey, ScottPlot.AxisLimits? preservedZoomLimits = null)
        {
            #region Guard Matrix Checks for Missing Components or Models
            if (anchor == null || dbTimestampExcelOA == null || dbTimestampExcelOA.Length == 0 || dbDistanceToArrival == null || dbDistanceToArrival.Length == 0 
                || dbDistanceToArrival.Length != dbTimestampExcelOA.Length) return;
            #endregion

            #region Setup In-Memory Observation Datasets Arrays
            int totalPointsCount = dbTimestampExcelOA.Length;
            double minimumTimestampXExcelOA = double.MaxValue;
            double maximumTimestampXExcelOA = double.MinValue;
            #endregion

            #region Pass 1: Parse Flat Telemetry Matrix Primitives from Disk
            for (int i = 0; i < totalPointsCount; i++)
            {
                if (dbTimestampExcelOA[i] < minimumTimestampXExcelOA) minimumTimestampXExcelOA = dbTimestampExcelOA[i];
                if (dbTimestampExcelOA[i] > maximumTimestampXExcelOA) maximumTimestampXExcelOA = dbTimestampExcelOA[i];
            }
            #endregion

            #region Pass 2: Calculate Continuous Kepler Trajectory Step Vectors
            double plotStartXExcelOASec = minimumTimestampXExcelOA - Settings.PaddingTimeCushion;
            double plotEndXExcelOASec = maximumTimestampXExcelOA + Settings.PaddingTimeCushion;

            #region Version 1.18: Dynamic Shift of the Kepler Interpolation Frame Range
            if (preservedZoomLimits.HasValue)
            {
                #region Extracts the exact current active zoom focus coordinates from screen limits
                double visibleWindowXMinExcelOA = preservedZoomLimits.Value.HorizontalRange.Min;
                double visibleWindowXMaxExcelOA = preservedZoomLimits.Value.HorizontalRange.Max;
                #endregion
                #region Restricts interpolation boundaries strictly inside the zoom focus window box
                if (!double.IsNaN(visibleWindowXMinExcelOA) && !double.IsNaN(visibleWindowXMaxExcelOA))
                {
                    plotStartXExcelOASec = visibleWindowXMinExcelOA;
                    plotEndXExcelOASec = visibleWindowXMaxExcelOA;
                }
                #endregion
            }
            #endregion

            #region Version 1.15: Compute Dynamic Adaptive Step Resolution Bounds
            double totalSpanTimeUnixSeconds = KeplerOrbitSolver.ToUnixSeconds(maximumTimestampXExcelOA - minimumTimestampXExcelOA);
            double completedOrbitCycles = totalSpanTimeUnixSeconds / anchor.OrbitalPeriod;
            double idealCalculatedSteps = completedOrbitCycles * Settings.TargetStepsPerOrbitCycle;

            int graphResolutionIntervalsSec = Math.Max(
                Settings.MinAdaptiveGraphResolution,
                Math.Min(Settings.MaxAdaptiveGraphResolution, (int)idealCalculatedSteps));

            double calculatedStepWidthSec = (plotEndXExcelOASec - plotStartXExcelOASec) / graphResolutionIntervalsSec;
            #endregion

            double[] curveTimestampsXSecExcelOA = new double[graphResolutionIntervalsSec + 1];
            double[] curveExpectedDistancesY = new double[graphResolutionIntervalsSec + 1];
            double[] upperVarianceBoundsY1 = new double[graphResolutionIntervalsSec + 1];
            double[] lowerVarianceBoundsY2 = new double[graphResolutionIntervalsSec + 1];

            KeplerOrbitSolver.OrbitalElements physicalElements =
                new KeplerOrbitSolver.OrbitalElements
                {
                    SemiMajorAxisMetres = anchor.SemiMajorAxis,
                    Eccentricity = anchor.Eccentricity,
                    OrbitalPeriodSeconds = anchor.OrbitalPeriod,
                    AnchorTimestampUnixSec = KeplerOrbitSolver.ToUnixSeconds(anchor.AnchorTimestampUnixSec),
                    AnchorDistanceLs = anchor.AnchorDistance,
                    IsClimbingOutward = anchor.IsClimbingOutward
                };
            #endregion

            #region Pass 3: Audit Live Telemetry Gaps and Track Peak Variances
            double absoluteMaximumDeviationLS = 0.0;
            double absoluteMaximumDeviationPercent = 0.0;

            var cleanPointsX = new List<double>();
            var cleanPointsY = new List<double>();
            var anomalyPointsX = new List<double>();
            var anomalyPointsY = new List<double>();

            for (int i = 0; i < totalPointsCount; i++)
            {
                #region Compute Real Versus Predicted Geometric Drift Offset
                long TargetUnixSeconds = KeplerOrbitSolver.ToUnixSeconds(dbTimestampExcelOA[i]);

                double predictedDistanceLS = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                    physicalElements,
                    TargetUnixSeconds);

                double calculationVarianceLS = Math.Abs(dbDistanceToArrival[i] - predictedDistanceLS);

                double calculationVariancePercent = (predictedDistanceLS > 0.0)
                    ? (calculationVarianceLS / predictedDistanceLS) * 100.0
                    : 0.0;

                #region Tracks peak percentage drift across the entire log data array sweep
                if (calculationVariancePercent > absoluteMaximumDeviationPercent)
                {
                    absoluteMaximumDeviationPercent = calculationVariancePercent;
                }
                #endregion
                #endregion

                #region Pass 3.5: Threshold Sorting and Clean Deviation Ingestion
                if (calculationVariancePercent <= Settings.MaxAllowedErrorPercent)
                {
                    cleanPointsX.Add(dbTimestampExcelOA[i]);
                    cleanPointsY.Add(dbDistanceToArrival[i]);

                    #region Version 1.16: Outlier filter gate prevents anomalies from poisoning bands
                    if (calculationVarianceLS > absoluteMaximumDeviationLS)
                    {
                        absoluteMaximumDeviationLS = calculationVarianceLS;
                    }
                    #endregion
                }
                else
                {
                    anomalyPointsX.Add(dbTimestampExcelOA[i]);
                    anomalyPointsY.Add(dbDistanceToArrival[i]);
                }
                #endregion
            }
            #endregion
            #region Pass 3.5: Interpolate Smooth Curve Rail Nodes & Map Uniform Raw LS Variances
            for (int step = 0; step <= graphResolutionIntervalsSec; step++)
            {
                #region Extrapolate Symmetrical Error Band Overlays
                double currentStepTimeXExcelOA = plotStartXExcelOASec + (step * calculatedStepWidthSec);
                long currentStepUnixSeconds = KeplerOrbitSolver.ToUnixSeconds(currentStepTimeXExcelOA);

                double expectedRailDistanceLS = KeplerOrbitSolver.PredictDistanceAtTimestamp(
                    physicalElements,
                    currentStepUnixSeconds);

                curveTimestampsXSecExcelOA[step] = currentStepTimeXExcelOA;
                curveExpectedDistancesY[step] = expectedRailDistanceLS;

                upperVarianceBoundsY1[step] = expectedRailDistanceLS + absoluteMaximumDeviationLS;
                lowerVarianceBoundsY2[step] = expectedRailDistanceLS - absoluteMaximumDeviationLS;
                #endregion
            }
            #endregion
            #region Pass 3.6: Calculate Cumulative Viewport Data Enveloping Extents (Fixed Sequence)
            double totalEnvelopeMinY = double.MaxValue;
            double totalEnvelopeMaxY = double.MinValue;

            #region Aggregate Scatter Telemetry Observation Data Channels
            if (cleanPointsY.Count > 0)
            {
                totalEnvelopeMaxY = Math.Max(totalEnvelopeMaxY, cleanPointsY.Max());
                totalEnvelopeMinY = Math.Min(totalEnvelopeMinY, cleanPointsY.Min());
            }
            if (anomalyPointsY.Count > 0)
            {
                totalEnvelopeMaxY = Math.Max(totalEnvelopeMaxY, anomalyPointsY.Max());
                totalEnvelopeMinY = Math.Min(totalEnvelopeMinY, anomalyPointsY.Min());
            }
            #endregion

            #region Version 1.22: Primary rail backup check guarantees non-zero bounds constraints
            if (totalEnvelopeMinY == double.MaxValue || totalEnvelopeMaxY == double.MinValue)
            {
                totalEnvelopeMinY = curveExpectedDistancesY.Min();
                totalEnvelopeMaxY = curveExpectedDistancesY.Max();
            }
            #endregion
            #endregion
            #region Aggregate Active Continuous Path Trajectory Elements (Safely Populated)
            if (upperVarianceBoundsY1.Length > 0)
            {
                totalEnvelopeMaxY = Math.Max(totalEnvelopeMaxY, upperVarianceBoundsY1.Max());
            }
            if (lowerVarianceBoundsY2.Length > 0)
            {
                totalEnvelopeMinY = Math.Min(totalEnvelopeMinY, lowerVarianceBoundsY2.Min());
            }
            #endregion
            #region Pass 5: Bind Data Channels Directly to ScottPlot Controls Viewport
            formsPlotCanvas.Plot.Clear();

            #region Version 1.15: Corrected Local Epoch Calendar Conversions (Excel OA Native)
            double[] curveOADatesXExcaleOA = curveTimestampsXSecExcelOA.ToArray();
            double[] cleanOADatesXExcelOA = cleanPointsX.ToArray();
            double[] anomalyOADatesXExcelOA = anomalyPointsX.ToArray();
            #endregion

            #region Render Shaded Background Tolerance Band Area Series Layer
            var shadedVarianceBand = formsPlotCanvas.Plot.Add.FillY(
                curveOADatesXExcaleOA,
                upperVarianceBoundsY1,
                lowerVarianceBoundsY2);

            shadedVarianceBand.FillColor = ScottPlot.Color.FromHex("#FFE4B5").WithAlpha(0.4);
            shadedVarianceBand.LineColor = ScottPlot.Color.FromHex("#FFD700").WithAlpha(0.2);
            #endregion

            #region Render Continuous Keplerian Smooth Path Line Rail Layer
            var smoothBlueCurveRail = formsPlotCanvas.Plot.Add.ScatterLine(
                curveOADatesXExcaleOA,
                curveExpectedDistancesY);

            smoothBlueCurveRail.LineColor = ScottPlot.Color.FromHex("#1E90FF");
            smoothBlueCurveRail.LineWidth = 1;
            #endregion

            #region Render Clean Telemetry Observation As Blue Circles
            if (cleanPointsX.Count > 0)
            {
                var cleanScatter = formsPlotCanvas.Plot.Add.ScatterPoints(cleanOADatesXExcelOA, cleanPointsY.ToArray());
                cleanScatter.MarkerShape = MarkerShape.Eks;
                cleanScatter.MarkerSize = 7;
                cleanScatter.MarkerColor = ScottPlot.Color.FromHex("#0000FF");
            }
            #endregion

            #region Render Anomaly Telemetry Observation As Red X Marks
            if (anomalyPointsX.Count > 0)
            {
                var anomalyScatter = formsPlotCanvas.Plot.Add.ScatterPoints(
                    anomalyOADatesXExcelOA,
                    anomalyPointsY.ToArray());

                anomalyScatter.MarkerShape = MarkerShape.Eks;
                anomalyScatter.MarkerSize = 9;
                anomalyScatter.MarkerColor = ScottPlot.Color.FromHex("#FF0000");
            }
            #endregion
            #endregion
            #region Format Visual Chart Canvas Typography Elements & DateTime Axes
            formsPlotCanvas.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            formsPlotCanvas.Plot.XLabel("Time (Date Time)");
            formsPlotCanvas.Plot.YLabel("Distance to Arrival (Ls)");
            #endregion
            #region Version 1.21: Conditional Viewport Preservation and Canvas Repaint
            try
            {
                formsPlotCanvas.Plot.Axes.Margins(Settings.PlotXAxisMargin, Settings.PlotXAxisMargin);

                #region Evaluate Viewport Preservation Boundaries Constraints
                if (preservedZoomLimits.HasValue &&
                    !double.IsNaN(preservedZoomLimits.Value.HorizontalRange.Min) &&
                    !double.IsNaN(preservedZoomLimits.Value.HorizontalRange.Max))
                {
                    // View limits remain locked straight onto your manual zoom coordinates window
                    formsPlotCanvas.Plot.Axes.SetLimits(preservedZoomLimits.Value);
                }
                else
                {
                    #region Compute Proportional Percentage Vertical Padding Cushions
                    double overallEnvelopeHeight = totalEnvelopeMaxY - totalEnvelopeMinY;

                    // Dynamically maps a symmetric 10% whitespace buffer above and below components
                    double proportionalPaddingY = (overallEnvelopeHeight > 0.0)
                        ? (overallEnvelopeHeight * Settings.PlotYAxisMarginPercent)
                        : 0.05;

                    double customLockedYMin = totalEnvelopeMinY - proportionalPaddingY;
                    double customLockedYMax = totalEnvelopeMaxY + proportionalPaddingY;

                    // Enforces uniform whitespace ratios across flatlines and deep deviations cleanly
                    formsPlotCanvas.Plot.Axes.SetLimitsY(customLockedYMin, customLockedYMax);
                    formsPlotCanvas.Plot.Axes.SetLimitsX(plotStartXExcelOASec, plotEndXExcelOASec);
                    #endregion
                }
                #endregion

                formsPlotCanvas.Refresh();

                #region Assemble Form Status Strip Output Metrics
                string calculatedErrorSuffixString = (absoluteMaximumDeviationPercent > Settings.MaxAllowedErrorPercent)
                    ? $">{Settings.MaxAllowedErrorPercent:F3}% Threshold Breach (Peak: {absoluteMaximumDeviationPercent:F2}%)"
                    : $"{absoluteMaximumDeviationPercent:F2}% (Pass), ScanDB datapoints: {dbTimestampExcelOA.Length}";

                lblPlotterStatus.Text = $"[IDLE] Visual plot synchronized successfully. Max Dev: {absoluteMaximumDeviationLS:F4} Ls | Max Error: {calculatedErrorSuffixString}, ScanDB datapoints: {dbTimestampExcelOA.Length}";
                #endregion
            }
            catch (Exception ex)
            {
                lblPlotterStatus.Text = $"[ERROR] Viewport Auto-Scaling Pipeline Fault: {ex.Message}";
            }
            #endregion
        }
        #region User Interface Viewport Axis Constraints Controls
        private void OnZoomModeChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rdo && rdo.Checked)
            {
                ApplyZoomConstraints();
            }
        }
        #endregion


        #region User Interface Manual Render Invalidation Controls
        private void BtnUpdateRenderCanvas_Click(object sender, EventArgs e)
        {
            #region Query Current Selected Index and Pass Down Current Zoom Focus Limits
            if (cmbPlanetSelector.SelectedItem == null) return;

            int currentSelectionPointer = cmbPlanetSelector.SelectedIndex;
            if (currentSelectionPointer >= 0 && currentSelectionPointer < parallelRegistryKeys.Count)
            {
                string activeKey = parallelRegistryKeys[currentSelectionPointer];

                if (inst_OrbitRegistry.TryGetMasterAnchor(activeKey, out MasterOrbitAnchor anchor))
                {
                    #region Extracts the exact current active zoom focus rectangle from the screen
                    ScottPlot.AxisLimits activeZoomLimits = formsPlotCanvas.Plot.Axes.GetLimits();
                    #endregion

                    #region Collect Data points from scanDB
                    long systemAddress = -1;
                    long bodyID = -1;
                    if (inst_OrbitRegistry.SplitKeyintoIDs(activeKey, out systemAddress, out bodyID))
                    {
                        CelestialDataset dataset = ScanDataStorageDriver.GetCelestialDataset(systemAddress, bodyID);
                        if ((dataset.Metadata.SystemName != Settings.UnknownData) && (dataset.Metadata.BodyName != Settings.UnknownData)
                            && (dataset.DistancesToArrival.Length > 0) && (dataset.TimestampsExcelOA.Length > 0))
                        {
                            RenderOrbitDiagnosticCanvas(anchor, dataset.TimestampsExcelOA, dataset.DistancesToArrival, activeKey, activeZoomLimits);
                        }
                        else
                        {
                            lblPlotterStatus.Text = $"[ERROR] Interface Chart Render Fault: No data avalible in DB for this body, cause 2";
                        }
                    }
                    else
                    {
                        lblPlotterStatus.Text = $"[ERROR] Interface Chart Render Fault: Could not get systemAddress and BodyID from orbitRegistry, casue 2";
                    }
                    #endregion
                }
            }
            #endregion
        }
        #endregion

        private void ApplyZoomConstraints()
        {
            try
            {
                // Clear out all previous layout scale rules to ensure a clean state baseline
                formsPlotCanvas.Plot.Axes.Rules.Clear();

                if (rdoZoomYOnly.Checked)
                {
                    // Extract current viewport dimensions to establish absolute static limits
                    var currentXMin = formsPlotCanvas.Plot.Axes.Bottom.Min;
                    var currentXMax = formsPlotCanvas.Plot.Axes.Bottom.Max;

                    // Instantiating with target axis and double bounds constraints
                    var lockX = new ScottPlot.AxisRules.LockedHorizontal(
                        formsPlotCanvas.Plot.Axes.Bottom,
                        currentXMin,
                        currentXMax
                    );
                    formsPlotCanvas.Plot.Axes.Rules.Add(lockX);
                }
                else if (rdoZoomXOnly.Checked)
                {
                    // Extract current viewport dimensions to establish absolute static limits
                    var currentYMin = formsPlotCanvas.Plot.Axes.Left.Min;
                    var currentYMax = formsPlotCanvas.Plot.Axes.Left.Max;

                    // Instantiating with target axis and double bounds constraints
                    var lockY = new ScottPlot.AxisRules.LockedVertical(
                        formsPlotCanvas.Plot.Axes.Left,
                        currentYMin,
                        currentYMax
                    );
                    formsPlotCanvas.Plot.Axes.Rules.Add(lockY);
                }
                // "Standard" mode configuration skips adding rule objects, leaving full panning/zooming active

                // Refresh the geometric viewport canvas thread-safely
                formsPlotCanvas.Refresh();
            }
            catch (Exception ex)
            {
                // Route runtime visualization faults safely to the lower diagnostic text block strip
                if (lblPlotterStatus != null)
                {
                    lblPlotterStatus.Text = $"[ERROR] Failed to apply axis zoom constraints: {ex.Message}";
                }
            }
        }
        private async void FormOrbitDiagnosticPlotter_Load(object sender, EventArgs e)
        {
            /*
            #region Guard Matrix Checks for Active Default Text Strings
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text)) return;

            string verifiedStartupPath = txtFolderPath.Text.Trim();
            if (!File.Exists(verifiedStartupPath)) return;
            #endregion
            */
            #region Populate combo box
            lblPlotterStatus.Text = "[IDLE] ScanDB loaded. Ready";
            PopulatePlanetSelectionComboBox();
            #endregion
        }
        private void PopulatePlanetSelectionComboBox()
        {
            #region Guard Clauses for Missing Memory Database Anchors
            if (inst_OrbitRegistry == null || cmbPlanetSelector == null) return;
            #endregion

            #region Clear Dropdown State and Reset Parallel Index Trackers
            cmbPlanetSelector.SelectedIndexChanged -= CmbPlanetSelector_SelectedIndexChanged;
            cmbPlanetSelector.Items.Clear();
            parallelRegistryKeys.Clear();
            #endregion

            #region Extract Composite Keys and Build Human Readable Twin Pipelines
            int totalMasterRecords = inst_OrbitRegistry.GetMasterCount();
            if (totalMasterRecords > 0)
            {
                string[] masterKeysArray = inst_OrbitRegistry.GetMasterKeysSnapshot();
                Array.Sort(masterKeysArray);

                #region Synchronized Mapping Allocation Pass
                foreach (string compositeKey in masterKeysArray)
                {
                    if (string.IsNullOrWhiteSpace(compositeKey)) continue;
                    #region Check if the amount of datapoints avalible in the ScanDB is with the filter range
                    long systemID = -1;
                    long bodyID = -1;
                    if (!inst_OrbitRegistry.SplitKeyintoIDs(compositeKey, out systemID, out bodyID)) continue;
                    int scanDBDataPoints = ScanDataStorageDriver.GetCelestialDataPointCount(systemID, bodyID);
                    if (scanDBDataPoints < nud_Min_DataFilter.Value || scanDBDataPoints > nud_Max_DataFilter.Value)
                    {
                        continue;
                    }
                    #endregion

                    string systemBodyName = null;
                    if (inst_OrbitRegistry.TryGetSystemBodyName(compositeKey, out systemBodyName))
                    {
                        #region Commit Data Streams Directly to Parallel Memory Tracks
                        cmbPlanetSelector.Items.Add(systemBodyName);
                        parallelRegistryKeys.Add(compositeKey);
                        #endregion
                    }
                    else
                    {
                        cmbPlanetSelector.Items.Clear();
                        parallelRegistryKeys.Clear();
                        lblPlotterStatus.Text = "[ERROR] - Failed to load BodyNames configuration arrays";
                        break;
                    }
                }
                #endregion
            }
            #endregion

            #region Re-Bind Change Events Back to Dropdown Framework
            cmbPlanetSelector.SelectedIndexChanged += CmbPlanetSelector_SelectedIndexChanged;
            if (cmbPlanetSelector.Items.Count > 0)
            {
                cmbPlanetSelector.SelectedIndex = 0;
            }
            else
            {
                lblPlotterStatus.Text = $"[WARNING] No bodies avalible within filter window";
            }
            #endregion
        }

        private void nud_Min_DataFilter_ValueChanged(object sender, EventArgs e)
        {
            PopulatePlanetSelectionComboBox();
        }
    }
}