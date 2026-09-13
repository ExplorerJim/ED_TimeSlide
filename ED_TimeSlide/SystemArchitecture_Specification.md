# ELITE DANGEROUS TIME SLIP FORENSICS ENGINE
## Master System Architecture Specification

---

## 1. Core Mission Statement & Architectural Paradigm
The Time Slip Forensics Engine is designed to process massive multi-month Elite Dangerous flight journal data logs (`Journal.Scan` strings) to detect chronological anomalies ("Time Slips"). 
The files come from the EDDN website https://edgalaxydata.space/EDDN/ These are thinned down versions of the files generated on the players PC, many lines of data are ommitted. There are many types of logs avalible from the EDDN website grouped together, i.e. scan, FSDJump, CarrierJump etc

The underlying universe simulation engine (Frontier Developments' *Stellar Forge*) operates on completely deterministic, nested, two-body "on-rails" Keplerian orbits. Celestial bodies do not deviate from their pre-calculated analytical pathing tracks. Therefore, any true statistical tracking variance between historical telemetry snapshots signifies a genuine timeline desynchronization event rather than random background noise or simulation drift.

---

## 2. Multi-Pass Execution Pipeline

To achieve flawless anomaly tracking without hardcoded metric gates, the engine executes sequentially across three isolated operational phases.

[Phase 1: Ingestion & System Mapping]
↓
[Phase 2: Barycentric Calibration Engine] -> Saves Master Coordinate Database
↓
[Phase 3: Keplerian Forensics Scan] --------> Generates Final Time Slip Dossiers

### Phase 1: Ingestion & System Mapping Tree
* **Objective:** Map the physical hierarchy tree of all encountered systems.
* **Logic Framework:** The engine abandons hardcoded distance filters (such as the legacy 50,000 LS gate). Instead, it natively parses the `Parents` JSON array block included within each telemetry line:
  * `{"Parents": [{"Ring": 1}, {"Star": 0}]}` -> Inner body circling the primary star.
  * `{"Parents": [{"Null": 1}, {"Null": 0}]}` -> Co-orbiting stellar pairs swinging around an invisible barycenter anchor.
* **Output:** A structural map flagging which unique system addresses contain multi-star binary structures requiring barycentric noise calibration.

### Phase 2: Barycentric Calibration Engine (Independent Pipeline)
* **Objective:** Calculate the exact fixed coordinate vectors of hidden barycenter pivot points.
* **Problem Statement:** Player journal files track `DistanceFromArrivalLS` relative to Star A, but Star A is orbiting an unprinted, invisible barycenter mass center. This creates a massive geometric swaying vector ("wobble noise") that ruins standard linear tracking.
* **The 3-Point Triangulation Math:** For systems flagged in Phase 1, the engine pulls three highly separated, valid data points across a multi-month period. Assuming a rigid elliptical rail plane, the engine executes a triangulation routine to solve for:
  1. The static 3D vector offset `(X, Y, Z)` from Star A `(0,0,0)` to the hidden Barycenter.
  2. The exact orbital parameters (`SemiMajorAxis`, `Eccentricity`) of Star B around that hidden anchor point.
* **Output:** This data is compiled directly into a static registry: `BarycenterCoordinateRegistry.json`. This acts as a global lookup table, reducing background structural noise to exactly 0.00%.

### Phase 3: Keplerian Forensics Pass (The Time Slip Scanner)
* **Objective:** Scan the telemetry logs to isolate true timeline jumps.
* **Staging Calibration & The Apex Turnaround Fix:** 
  * The engine holds telemetry lines in a 5-point calibration buffer (`TryValidateStagingCluster`) before confirming an active track.
  * To prevent fast-orbiting bodies (e.g., 0.5-day periods) from breaking the validation check during an orbital apex crossover (apoapsis/periapsis), the engine performs a **Consecutive-Pair Slope Check** across all five internal points (`Pt2 vs Pt1`, `Pt3 vs Pt2`).
  * If a direction reversal is detected (e.g., distance vectors transition from positive slope to negative slope), the engine calculates the apex curve natively, validating the track and assigning the `IsClimbingOutward` trajectory direction flag accurately.
* **The Physics Kepler Solver Execution:**
  * When evaluating any body orbiting a secondary star, the engine queries `BarycenterCoordinateRegistry.json`.
  * It computes the precise coordinate location of Star B at target timestamp `(t)` using the physics Kepler solver.
  * It dynamically shifts the mathematical center frame of reference to Star B's active position, translating the 1D scalar `DistanceFromArrivalLS` vector into an anomaly phase selection.
  * Any tracking measurement that deviates beyond the zero-noise threshold is permanently flagged as a true time slip.

---

## 3. Post-Analysis Reporting Specification

When a valid time slip is confirmed, the engine bypasses standard text logs and outputs a comprehensive file formatted exactly to the following forensic dossier structure:

```markdown
# TIME SLIP FORENSICS DOSSIER: [System Name] [Body ID]
## SYSTEM ID: [SystemAddress] | STATUS: CRITICAL TIME DESYNCHRONIZATION

### 1. TARGET SUMMARY
* **Body Name:** [e.g., Col 285 Sector JP-K b23-7 - B]
* **Body Type:** [Star / PlanetClass / Belt]
* **Parent Anchor:** [Static Reference / Resolved Barycenter Vector]
* **Orbital Period:** [Value in Seconds / Days]

### 2. BASELINE RAIL MODEL
* **Verified Calibration Timestamp:** [AnchorTimestamp Epoch]
* **Calculated Anchor Distance:** [AnchorDistance Scalar in LS]
* **Trajectory Vector:** [IsClimbingOutward True/False Vector Path]
* **Source Validation Document:** [VerifiedSourceFile Identifier]

### 3. THE DISRUPTION EVENT
* **Symptom:** [Instantaneous Spatial Tear / Periodic Timeline Drift]
* **Detection Timestamp:** [Event ISO Timestamp]
* **Expected Scalar Distance:** [Kepler Predicted Value in LS]
* **Observed Telemetry Distance:** [Logged Value in LS]
* **Absolute Variance Magnitude:** [Net Delta in Light Seconds]

### 4. TELEMETRY FORENSICS AUDIT TRAIL

| Timestamp (ISO) | Uploader ID | Software Client | Reported Distance (LS) | Calculated Drift (LS) |
|---|---|---|---|---|
| [t-1 Entry] | [Hash] | [Client Name] | [Value] | [Value] |
| [Event t] | [Hash] | [Client Name] | [Value] | [Value] (CRITICAL SPIKE) |
| [t+1 Entry] | [Hash] | [Client Name] | [Value] | [Value] |

### 5. SYSTEM DIAGNOSTIC SIGNATURE
* **Barycentric Wobble Factor:** 0.000000% (Fully Corrected via Phase 2 Matrix)
* **Confidence Rating:** [High / Moderate / Low based on client source verification checks]
* **Forensic Conclusion:** [Detailed technical summary explaining the physical scale of the temporal rift]
```