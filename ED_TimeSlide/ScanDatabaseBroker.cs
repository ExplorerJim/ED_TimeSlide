using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace ED_TimeSlide
{
    public partial class ScanDatabaseBroker
    {
        #region Variables & Connection Properties
        private readonly string _connectionString;
        private SqliteConnection _activeSessionConnection = null;
        private SqliteTransaction _activeSessionTransaction = null;
        #endregion

        #region Initialization & Lifecycle Construction
        /// <summary>
        /// Initializes a new instance of the database broker engine.
        /// </summary>
        public ScanDatabaseBroker()
        {
            _connectionString = $"Data Source={Settings.ScanDataDbPath};Default Timeout={Settings.ConnectionDefaultTimeoutSec};";
            EnsureStorageDirectoriesExist();
        }

        private void EnsureStorageDirectoriesExist()
        {
            string dbDir = Path.GetDirectoryName(Settings.ScanDataDbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
        }
        #endregion

        #region Core Connection Management Engine
        /// <summary>
        /// Opens a brand new isolated database connection channel for schema initialization or custom commands.
        /// </summary>
        public SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using (var pragmaCmd = new SqliteCommand($"PRAGMA busy_timeout = {Settings.ConnectionBusyTimeoutMs};", connection))
            {
                pragmaCmd.ExecuteNonQuery();
            }
            return connection;
        }

        /// <summary>
        /// Opens a persistent transactional bulk ingestion session to capture high-speed multithreaded queue drops.
        /// </summary>
        public void OpenIngestionSession()
        {
            if (_activeSessionConnection == null)
            {
                _activeSessionConnection = new SqliteConnection(_connectionString);
                _activeSessionConnection.Open();
                _activeSessionTransaction = _activeSessionConnection.BeginTransaction();
            }
        }

        public SqliteConnection GetActiveSessionConnection() => _activeSessionConnection;
        public SqliteTransaction GetActiveSessionTransaction() => _activeSessionTransaction;

        /// <summary>
        /// Commits the running session transaction frame and releases exclusive file locks securely.
        /// </summary>
        public void CloseIngestionSession()
        {
            try
            {
                if (_activeSessionTransaction != null)
                {
                    _activeSessionTransaction.Commit();
                }
            }
            catch (Exception)
            {
                _activeSessionTransaction?.Rollback();
                throw;
            }
            finally
            {
                _activeSessionTransaction?.Dispose();
                _activeSessionConnection?.Dispose();
                _activeSessionTransaction = null;
                _activeSessionConnection = null;
            }
        }

        /// <summary>
        /// Applies high-speed Write-Ahead Logging (WAL) and memory optimization flags to a given connection handle.
        /// </summary>
        public void ApplyPerformancePragmas(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = OFF;
                        PRAGMA cache_size = -50000;
                        PRAGMA temp_store = MEMORY;";
                command.ExecuteNonQuery();
            }
        }
        #endregion

        #region Database Structural Schema Orchestration
        /// <summary>
        /// Verifies and initializes all core storage structural layout frames on local disk storage.
        /// </summary>
        public void InitializeDatabaseStructure()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;

                            #region Table 1: StarSystems Schema Definition
                            command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS StarSystems (
                                SystemDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                SystemEDID BIGINT UNIQUE,
                                SystemName TEXT
                            );";
                            command.ExecuteNonQuery();
                            #endregion

                            #region Table 2: Bodies Schema Definition
                            command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Bodies (
                                BodyDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                SystemDBID INTEGER,
                                BodyEDID INTEGER,
                                BodyName TEXT,
                                BodyType TEXT,
                                FOREIGN KEY(SystemDBID) REFERENCES StarSystems(SystemDBID)
                            );";
                            command.ExecuteNonQuery();
                            #endregion

                            #region Table 3: DataPoints Telemetry Schema Definition
                            command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS DataPoints (
                                DataPointDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                BodyDBID INTEGER,
                                Timestamp_UnixSec BIGINT,
                                DistanceToArrival REAL,
                                UploaderID TEXT,
                                SoftwareName TEXT,
                                ScanType TEXT,
                                FOREIGN KEY(BodyDBID) REFERENCES Bodies(BodyDBID)
                            );";
                            command.ExecuteNonQuery();
                            #endregion

                            #region Table 4: ObitInfo Physics Invariants
                            command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS ObitInfo (
                                OrbitDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                BodyDBID INTEGER UNIQUE,
                                SemiMajorAxis REAL,
                                Eccentricity REAL,
                                OrbitalPeriod_Sec REAL,
                                OrbitalInclination REAL,
                                Periapsis REAL,
                                MeanAnomaly REAL,
                                AscendingNode REAL,
                                AnchorTimestamp_UnixSec BIGINT NULL,
                                AnchorDistance_Ls REAL NULL,
                                IsRetrograde INTEGER NOT NULL DEFAULT 0,
                                IsClimbingOutward BOOLEAN NULL,
                                FOREIGN KEY(BodyDBID) REFERENCES Bodies(BodyDBID)
                            );";
                            command.ExecuteNonQuery();
                            #endregion

                            #region Table 5: BaryCentreNodes Hierarchy Index
                            command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS BaryCentreNodes (
                                BaryCentreDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                ChildBodyDBID INTEGER,
                                ParentBodyDBID INTEGER,
                                HierarchyDepth INTEGER,
                                FOREIGN KEY(ChildBodyDBID) REFERENCES Bodies(BodyDBID),
                                FOREIGN KEY(ParentBodyDBID) REFERENCES Bodies(BodyDBID)
                            );";
                            command.ExecuteNonQuery();
                            #endregion

                            #region Performance Index Setup
                            command.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS idx_event_baseline ON DataPoints (BodyDBID, Timestamp_UnixSec, DistanceToArrival);";
                            command.ExecuteNonQuery();

                            command.CommandText = @"CREATE UNIQUE INDEX IF NOT EXISTS idx_bodies_lookup_composite ON Bodies (SystemDBID, BodyEDID);";
                            command.ExecuteNonQuery();
                            #endregion
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        #endregion
        #region High-Speed Raw Ingest Methods
        /// <summary>
        /// Inserts or resolves a system address key map via high-speed non-blocking transactions.
        /// </summary>
        public long GetOrCreateSystem(SqliteConnection conn, SqliteTransaction trans, ScanMessage msg)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO StarSystems (SystemEDID, SystemName) VALUES (@sysEdId, @sysName);
                    SELECT SystemDBID FROM StarSystems WHERE SystemEDID = @sysEdId LIMIT 1;";

                cmd.Parameters.AddWithValue("@sysEdId", msg.SystemAddress);
                cmd.Parameters.AddWithValue("@sysName", msg.StarSystem ?? (object)DBNull.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Inserts or resolves a body master identity index record. Bypasses parent nested checks to restore speed.
        /// </summary>
        public long GetOrCreateBodyRecord(SqliteConnection conn, SqliteTransaction trans, long systemDbId, EddnRecords rec)
        {
            var msg = rec.Message;
            long bodyDbId = 0;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO Bodies (SystemDBID, BodyEDID, BodyName, BodyType) VALUES (@sysDbId, @bodyEdId, @bodyName, @bodyType);
                    SELECT BodyDBID FROM Bodies WHERE SystemDBID = @sysDbId AND BodyEDID = @bodyEdId LIMIT 1;";

                cmd.Parameters.AddWithValue("@sysDbId", systemDbId);
                cmd.Parameters.AddWithValue("@bodyEdId", msg.BodyId);
                cmd.Parameters.AddWithValue("@bodyName", msg.BodyName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@bodyType", msg.PlanetClass ?? (object)DBNull.Value);
                bodyDbId = Convert.ToInt64(cmd.ExecuteScalar());
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO ObitInfo (BodyDBID, SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, OrbitalInclination, Periapsis, IsRetrograde, MeanAnomaly, AscendingNode) 
                    VALUES (@bodyDbId, @sma, @ecc, @period, @incl, @peri, @isRetro, @Mean, @Ascend);";

                // FIXED PARAMETER ASSIGNMENT TYPO
                cmd.Parameters.AddWithValue("@bodyDbId", bodyDbId);
                cmd.Parameters.AddWithValue("@sma", msg.SemiMajorAxis);
                cmd.Parameters.AddWithValue("@ecc", msg.Eccentricity);
                cmd.Parameters.AddWithValue("@period", msg.OrbitalPeriod);
                cmd.Parameters.AddWithValue("@incl", msg.OrbitalInclination);
                cmd.Parameters.AddWithValue("@peri", msg.Periapsis);
                cmd.Parameters.AddWithValue("@isRetro", Math.Abs(msg.OrbitalInclination) > 90.0 ? 1 : 0);
                cmd.Parameters.AddWithValue("@Mean", msg.MeanAnomaly);
                cmd.Parameters.AddWithValue("@Ascend", msg.AscendingNode);
                cmd.ExecuteNonQuery();
            }
            return bodyDbId;
        }

        /// <summary>
        /// Direct append injection handler for incoming raw telemetry logs.
        /// </summary>
        public void InsertTelemetryDataPoint(SqliteConnection conn, SqliteTransaction trans, long bodyDbId, EddnRecords rec)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO DataPoints (BodyDBID, Timestamp_UnixSec, DistanceToArrival, UploaderID, SoftwareName, ScanType)
                    VALUES (@bodyDbId, @timestamp, @distance, @uploader, @software, @scanType);";

                cmd.Parameters.AddWithValue("@bodyDbId", bodyDbId);
                cmd.Parameters.AddWithValue("@timestamp", new DateTimeOffset(rec.Message.Timestamp).ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("@distance", rec.Message.DistanceFromArrivalLS);
                cmd.Parameters.AddWithValue("@uploader", rec.Header?.UploaderId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@software", rec.Header?.SoftwareName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@scanType", rec.Message.ScanType ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion
        #region Deep Structural Selection Lookups (Used by Plotters and Engines)
        /// <summary>
        /// Compiles a coordinate structural dataset completely out of the database matching the given identifiers.
        /// </summary>
        public void LoadTelemetryArrays(int bodyId, List<double> excelDates, List<double> distances)
        {
            using (var conn = OpenConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Timestamp_UnixSec, DistanceToArrival 
                    FROM DataPoints WHERE BodyDBID = @BodyID ORDER BY Timestamp_UnixSec ASC;";
                cmd.Parameters.AddWithValue("@BodyID", bodyId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long unixSec = Convert.ToInt64(reader["Timestamp_UnixSec"]);
                        double dist = Convert.ToDouble(reader["DistanceToArrival"]);

                        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(unixSec).DateTime;
                        excelDates.Add(dt.ToOADate());
                        distances.Add(dist);
                    }
                }
            }
        }

        /// <summary>
        /// Builds specific lookup tables instantly on-disk right when a data lifecycle processing pipeline finishes.
        /// </summary>
        public void BuildPerformanceIndexes()
        {
            using (var conn = OpenConnection())
            {
                string indexBatchSql = @"
                    CREATE INDEX IF NOT EXISTS idx_barycentrenodes_child ON BaryCentreNodes(ChildBodyDBID);
                    CREATE INDEX IF NOT EXISTS idx_barycentrenodes_parent ON BaryCentreNodes(ParentBodyDBID);
                    CREATE INDEX IF NOT EXISTS idx_datapoints_body ON DataPoints(BodyDBID);
                    CREATE INDEX IF NOT EXISTS idx_bodies_system ON Bodies(SystemDBID);
                    CREATE INDEX IF NOT EXISTS idx_obitinfo_body ON ObitInfo(BodyDBID);
                ";

                using (var cmd = new SqliteCommand(indexBatchSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion
        #region Deferred Single-Pass Hierarchy Tree Compiler
        /// <summary>
        /// Deferred Post-Ingestion Pass: Scans unique master body strings once, extracts 
        /// hierarchical barycentric parents, and populates BaryCentreNodes structure matrices.
        /// </summary>
        public void GenerateSystemHierarchyTree(string[] inputFiles)
        {
            using (var conn = OpenConnection())
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // 1. Fetch only unique master bodies dropped into the relational table
                    var registeredBodies = new List<(long BodyDBID, long SystemDBID, long BodyEDID)>();
                    string fetchSql = "SELECT BodyDBID, SystemDBID, BodyEDID FROM Bodies;";

                    using (var cmd = new SqliteCommand(fetchSql, conn, trans))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            registeredBodies.Add((
                                reader.GetInt64(0),
                                reader.GetInt64(1),
                                reader.GetInt64(2)
                            ));
                        }
                    }

                    // Create a quick local dictionary tracking the raw files we are processing
                    var trackingBodyMap = new Dictionary<string, Newtonsoft.Json.Linq.JArray>();

                    // 2. Scan the file inputs once to extract the unique path trees mapped out on disk
                    foreach (var filePath in inputFiles)
                    {
                        if (!System.IO.File.Exists(filePath)) continue;

                        // Safely extract lines looking for unique mapping keys
                        foreach (var line in System.IO.File.ReadLines(filePath))
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            try
                            {
                                var json = Newtonsoft.Json.Linq.JObject.Parse(line);
                                var msg = json["message"];
                                if (msg == null) continue;

                                long sysAddr = msg["SystemAddress"]?.Value<long>() ?? 0;
                                int bodyId = msg["BodyID"]?.Value<int>() ?? 0;
                                var parents = msg["Parents"] as Newtonsoft.Json.Linq.JArray;

                                if (parents != null && parents.Count > 0)
                                {
                                    string key = $"{sysAddr}_{bodyId}";
                                    if (!trackingBodyMap.ContainsKey(key))
                                    {
                                        trackingBodyMap.Add(key, parents);
                                    }
                                }
                            }
                            catch { /* Skip corrupted text rows cleanly */ }
                        }
                    }
                    // 3. Bake structural mappings using high-speed transaction rows
                    string insertNodeSql = @"
                        INSERT INTO BaryCentreNodes (ChildBodyDBID, ParentBodyDBID, HierarchyDepth) 
                        VALUES (@childId, @parentId, @depth);";

                    string lookupParentSql = "SELECT BodyDBID FROM Bodies WHERE SystemDBID = @sysId AND BodyEDID = @parentEdid LIMIT 1;";
                    string createParentNodeSql = @"
                        INSERT OR IGNORE INTO Bodies (SystemDBID, BodyEDID, BodyName, BodyType) 
                        VALUES (@sysId, @parentEdid, @name, @type);";

                    foreach (var body in registeredBodies)
                    {
                        long systemEdid = 0;
                        string starSystemName = "Unknown";

                        using (var cmd = new SqliteCommand("SELECT SystemEDID, SystemName FROM StarSystems WHERE SystemDBID = @sysId LIMIT 1;", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@sysId", body.SystemDBID);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    systemEdid = reader.GetInt64(0);
                                    starSystemName = reader.GetString(1);
                                }
                            }
                        }

                        string key = $"{systemEdid}_{body.BodyEDID}";
                        if (trackingBodyMap.TryGetValue(key, out var parentsArray))
                        {
                            for (int depth = 0; depth < parentsArray.Count; depth++)
                            {
                                var parentObj = parentsArray[depth] as Newtonsoft.Json.Linq.JObject;
                                if (parentObj == null) continue;

                                foreach (var prop in parentObj.Properties())
                                {
                                    string parentType = prop.Name;
                                    long parentEdid = prop.Value.Value<long>();
                                    long parentDbId = 0;

                                    using (var cmd = new SqliteCommand(createParentNodeSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@sysId", body.SystemDBID);
                                        cmd.Parameters.AddWithValue("@parentEdid", parentEdid);
                                        cmd.Parameters.AddWithValue("@name", $"{starSystemName} ParentalNode_{parentEdid}");
                                        cmd.Parameters.AddWithValue("@type", parentType);
                                        cmd.ExecuteNonQuery();
                                    }

                                    using (var cmd = new SqliteCommand(lookupParentSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@sysId", body.SystemDBID);
                                        cmd.Parameters.AddWithValue("@parentEdid", parentEdid);
                                        var res = cmd.ExecuteScalar();
                                        if (res != null) parentDbId = Convert.ToInt64(res);
                                    }

                                    using (var cmd = new SqliteCommand(insertNodeSql, conn, trans))
                                    {
                                        cmd.Parameters.AddWithValue("@childId", body.BodyDBID);
                                        cmd.Parameters.AddWithValue("@parentId", parentDbId);
                                        cmd.Parameters.AddWithValue("@depth", depth);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    System.IO.File.AppendAllText(Settings.DBErrorLogPath, $"Hierarchy Compiler Exception: {ex.Message}{Environment.NewLine}");
                    throw;
                }
            }
        }
        #endregion
        #region Deferred Database Maintenance Sweeps
        /// <summary>
        /// Maintenance Pass: Purges uncalibrated exploration systems, orphan parent-child nodes, 
        /// and redundant telemetry blocks, then vacuums the database back to its minimal size footprint.
        /// </summary>
        public void PurgeLowDensityExplorationData()
        {
            return;

            using (var conn = OpenConnection())
            using (var trans = conn.BeginTransaction())
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;

                // Step 1: Drop physics logs only for bodies that explicitly have empty parameters AND are not parent anchors
                cmd.CommandText = @"
            DELETE FROM ObitInfo 
            WHERE AnchorTimestamp_UnixSec IS NULL
              AND BodyDBID NOT IN (SELECT DISTINCT ParentBodyDBID FROM BaryCentreNodes);";
                cmd.ExecuteNonQuery();

                // Step 2: Clear telemetry rows while explicitly protecting calibrated rows and barycentric hierarchy chains
                cmd.CommandText = @"
            DELETE FROM DataPoints 
            WHERE BodyDBID NOT IN (SELECT BodyDBID FROM ObitInfo)
              AND BodyDBID NOT IN (SELECT DISTINCT ChildBodyDBID FROM BaryCentreNodes)
              AND BodyDBID NOT IN (SELECT DISTINCT ParentBodyDBID FROM BaryCentreNodes);";
                cmd.ExecuteNonQuery();

                // Step 3: Remove structural parent strings ONLY if both ends of the relationship no longer exist
                cmd.CommandText = @"
            DELETE FROM BaryCentreNodes 
            WHERE ChildBodyDBID NOT IN (SELECT BodyDBID FROM Bodies)
               OR ParentBodyDBID NOT IN (SELECT BodyDBID FROM Bodies);";
                cmd.ExecuteNonQuery();

                // Step 4: Safely remove empty descriptor slots
                cmd.CommandText = @"
            DELETE FROM Bodies 
            WHERE BodyDBID NOT IN (SELECT BodyDBID FROM ObitInfo)
              AND BodyDBID NOT IN (SELECT DISTINCT BodyDBID FROM DataPoints)
              AND BodyDBID NOT IN (SELECT DISTINCT ChildBodyDBID FROM BaryCentreNodes)
              AND BodyDBID NOT IN (SELECT DISTINCT ParentBodyDBID FROM BaryCentreNodes);";
                cmd.ExecuteNonQuery();

                // Step 5: Purge star system wrappers only if they are completely empty
                cmd.CommandText = "DELETE FROM StarSystems WHERE SystemDBID NOT IN (SELECT DISTINCT SystemDBID FROM Bodies);";
                cmd.ExecuteNonQuery();

                trans.Commit();
            }

            // Step 6: Compress file and optimize sectors
            using (var conn = OpenConnection())
            using (var vacuumCmd = conn.CreateCommand())
            {
                vacuumCmd.CommandText = "VACUUM;";
                vacuumCmd.ExecuteNonQuery();
            }
        }
        #endregion
    }
}