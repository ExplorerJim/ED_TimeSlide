using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public class ScanDBPhase1 : IDisposable
    {
        #region Structures - Data Transfer Models
        public struct CelestialMetadata
        {
            public string SystemName;
            public string BodyName;
        }

        public struct CelestialDataset
        {
            public CelestialMetadata Metadata;
            public double[] TimestampsExcelOA;
            public double[] DistancesToArrival;
        }
        #endregion

        #region Variables
        private readonly BlockingCollection<EddnRecords> _ingestionQueue;
        private readonly CancellationToken _cancellationToken;
        private Task _consumerTask;
        private bool _isDisposed;
        private readonly ConcurrentDictionary<long, long> _systemCache;
        private readonly ConcurrentDictionary<string, long> _bodyCache;
        private const int QueueBoundedCapacity = 100000;
        private const int BatchSizeThreshold = 50000;
        private const int BatchTimeoutMilliseconds = 1000;
        #endregion

        public ScanDBPhase1(CancellationToken token)
        {
            #region Update instances
            _cancellationToken = token;
            _ingestionQueue = new BlockingCollection<EddnRecords>(QueueBoundedCapacity);         
            _systemCache = new ConcurrentDictionary<long, long>();
            _bodyCache = new ConcurrentDictionary<string, long>();
            #endregion

            InitializeDatabaseStructure();
            PreloadCacheFromDatabase();
            StartConsumerPipeline();
        }

        #region Private functions
        private void InitializeDatabaseStructure()
        {
            string dbPath = Settings.ScanDataDbPath;
            string dirPath = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string connectionString = $"Data Source={dbPath};";

            using (var connection = new SqliteConnection(connectionString))
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

                            #region Table 4: ObitInfo Constants Repository (IsRetrograde Constraint Added)
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

                            #region Table 5: BaryCentreNodes Hierarchy Matrix Index
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

                            #region Ingestion Performance Indexes Setup
                            command.CommandText = @"
                                CREATE UNIQUE INDEX IF NOT EXISTS idx_event_baseline 
                                ON DataPoints (BodyDBID, Timestamp_UnixSec, DistanceToArrival);";
                            command.ExecuteNonQuery();
                            #endregion
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        LogErrorToDisk("InitializeDatabaseStructure Terminal Failure", ex);
                        throw;
                    }
                }
            }
        }
        /// <summary> Deploys the background task worker loop to monitor incoming telemetry frames. </summary>
        private void StartConsumerPipeline()
        {
            _consumerTask = Task.Run(
                () => ProcessQueueItemsInBatches(),
                _cancellationToken
            );
        }
        /// <summary> Populates concurrent lookup dictionaries from relational rows to eliminate duplicate index queries during active transaction bulk blocks. </summary>
        private void PreloadCacheFromDatabase()
        {
            string connectionString = $"Data Source={Settings.ScanDataDbPath};";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                #region Warm Up Star Systems Lookup Cache
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT SystemEDID, SystemDBID FROM StarSystems;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                            {
                                _systemCache.TryAdd(reader.GetInt64(0), reader.GetInt64(1));
                            }
                        }
                    }
                }
                #endregion

                #region Warm Up Celestial Bodies Lookup Cache
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT SystemDBID, BodyEDID, BodyDBID FROM Bodies;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                            {
                                long sysId = reader.GetInt64(0);
                                long bodyEdid = reader.GetInt64(1);
                                long bodyDbid = reader.GetInt64(2);

                                string cacheKey = $"{sysId}_{bodyEdid}";
                                _bodyCache.TryAdd(cacheKey, bodyDbid);
                            }
                        }
                    }
                }
                #endregion
            }
        }
        /// <summary> Consumes telemetry items from the collection channel, executing performance flushes whenever threshold counts or timeout milestones are intercepted. </summary>
        private void ProcessQueueItemsInBatches()
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};";
            var localBatch = new List<EddnRecords>(BatchSizeThreshold);
            var lastFlushTime = DateTime.UtcNow;

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                #region Inject Transactional Performance Pragma Flags
                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = OFF;
                        PRAGMA cache_size = -50000;
                    ";
                    pragmaCmd.ExecuteNonQuery();
                }
                #endregion

                while (!_ingestionQueue.IsCompleted)
                {
                    if (_cancellationToken.IsCancellationRequested) break;

                    bool hasItem = false;
                    EddnRecords record = null;

                    try
                    {
                        int elapsed = (int)(DateTime.UtcNow - lastFlushTime).TotalMilliseconds;

                        int remainingTimeout = Math.Max(1, BatchTimeoutMilliseconds - elapsed);

                        hasItem = _ingestionQueue.TryTake(out record, remainingTimeout, _cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogErrorToDisk("BatchQueue Ingestion Extract Fault", ex);
                    }

                    if (hasItem && record != null)
                    {
                        localBatch.Add(record);
                    }

                    bool sizeReached = localBatch.Count >= BatchSizeThreshold;

                    double deltaMs = (DateTime.UtcNow - lastFlushTime).TotalMilliseconds;
                    bool timeReached = deltaMs >= BatchTimeoutMilliseconds;

                    if ((sizeReached || timeReached || _ingestionQueue.IsCompleted) &&
                        localBatch.Count > 0)
                    {
                        ExecuteBulkTransaction(connection, localBatch);
                        localBatch.Clear();
                        lastFlushTime = DateTime.UtcNow;
                    }
                }

                if (localBatch.Count > 0)
                {
                    ExecuteBulkTransaction(connection, localBatch);
                }
            }
        }
        /// <summary> Executes a synchronous database bulk transaction, deduplicating systems, generating body rows, mapping structural parent trees, and writing telemetry points. </summary>
        private void ExecuteBulkTransaction(SqliteConnection connection, List<EddnRecords> batch)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var record in batch)
                    {
                        if (record?.Message == null) continue;

                        long systemDbId = GetOrCreateCachedSystem(connection,transaction,record.Message);

                        long bodyDbId = GetOrCreateCachedBody(connection,transaction,systemDbId,record);

                        InsertDataPoint(connection,transaction,bodyDbId,record);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LogErrorToDisk("ExecuteBulkTransaction Block Core Aborted", ex);
                }
            }
        }
        private long GetOrCreateCachedSystem(SqliteConnection conn,SqliteTransaction trans,ScanMessage msg)
        {
            #region Cache Layer Hit: Skips the database engine read entirely
            if (_systemCache.TryGetValue(msg.SystemAddress, out long cachedId))
            {
                return cachedId;
            }
            #endregion
            #region Cache Layer Miss: Write out to relational storage once
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT INTO StarSystems (SystemEDID, SystemName) 
                    VALUES (@sysEdId, @sysName);
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@sysEdId", msg.SystemAddress);

                object nameVal = msg.StarSystem ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@sysName", nameVal);

                long dbId = (long)cmd.ExecuteScalar();

                _systemCache.TryAdd(msg.SystemAddress, dbId);
                return dbId;
            }
            #endregion
        }
        private long GetOrCreateCachedBody(SqliteConnection conn,SqliteTransaction trans,long systemDbId,EddnRecords rec)
        {
            #region Function variables
            var msg = rec.Message;
            string cacheKey = $"{systemDbId}_{msg.BodyId}";
            #endregion
            #region Get the cacheKey
            if (_bodyCache.TryGetValue(cacheKey, out long cachedId))
            {
                return cachedId;
            }
            #endregion
            #region Write Body Record
            long bodyDbId = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT INTO Bodies (SystemDBID, BodyEDID, BodyName, BodyType) 
                    VALUES (@sysDbId, @bodyEdId, @bodyName, @bodyType);
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@sysDbId", systemDbId);
                cmd.Parameters.AddWithValue("@bodyEdId", msg.BodyId);

                object nameVal = msg.BodyName ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@bodyName", nameVal);

                object typeVal = msg.PlanetClass ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@bodyType", typeVal);

                bodyDbId = (long)cmd.ExecuteScalar();
            }
            #endregion
            #region Write Orbit Record
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO ObitInfo (
                        BodyDBID, SemiMajorAxis, Eccentricity, OrbitalPeriod_Sec, 
                        OrbitalInclination, Periapsis, MeanAnomaly, AscendingNode, AnchorTimestamp_UnixSec, 
                        AnchorDistance_Ls, IsRetrograde, IsClimbingOutward
                    ) VALUES (
                        @bodyDbId, @sma, @ecc, @period, @incl, @peri, @mean , @ascend,
                        NULL, NULL, @isRetro, NULL
                    );";
                #region Pass and calcualte database entries
                #region An orbit is mathematically retrograde if its absolute inclination angle exceeds 90 degrees
                bool calculatedIsRetrograde = Math.Abs(msg.OrbitalInclination) > 90.0;
                #endregion
                cmd.Parameters.AddWithValue("@bodyDbId", bodyDbId);
                cmd.Parameters.AddWithValue("@sma", msg.SemiMajorAxis);
                cmd.Parameters.AddWithValue("@ecc", msg.Eccentricity);
                cmd.Parameters.AddWithValue("@period", msg.OrbitalPeriod);
                cmd.Parameters.AddWithValue("@incl", msg.OrbitalInclination);
                cmd.Parameters.AddWithValue("@peri", msg.Periapsis);
                cmd.Parameters.AddWithValue("@mean", msg.MeanAnomaly);
                cmd.Parameters.AddWithValue("@ascend", msg.AscendingNode);
                cmd.Parameters.AddWithValue("@isRetro", calculatedIsRetrograde ? 1 : 0);
                #endregion
                cmd.ExecuteNonQuery();
            }
            #endregion
            #region Write BaryCentreNodes
            if (msg.Parents != null && msg.Parents.Length > 0)
            {
                for (int depth = 0; depth < msg.Parents.Length; depth++)
                {
                    var parentNode = msg.Parents[(msg.Parents.Length - 1) - depth];
                    foreach (var pair in parentNode)
                    {
                        long parentDbId = 0;
                        string parentKey = $"{systemDbId}_{pair.Value}";

                        #region If this is a Barycentre and not a body, create a Body entry for it.=
                        if (!_bodyCache.TryGetValue(parentKey, out parentDbId))
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = trans;
                                cmd.CommandText = @"
                                    INSERT INTO Bodies (SystemDBID, BodyEDID, BodyName, BodyType) 
                                    VALUES (@sysDbId, @bodyEdId, @bodyName, @bodyType);
                                    SELECT last_insert_rowid();";

                                cmd.Parameters.AddWithValue("@sysDbId", systemDbId);
                                cmd.Parameters.AddWithValue("@bodyEdId", pair.Value);
                                cmd.Parameters.AddWithValue("@bodyName", $"{msg.StarSystem} ParentalNode_{pair.Value}");
                                cmd.Parameters.AddWithValue("@bodyType", pair.Key);

                                parentDbId = (long)cmd.ExecuteScalar();
                                _bodyCache.TryAdd(parentKey, parentDbId);
                            }
                        }
                        #endregion
                        #region Create BayCentreNode
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = @"
                                INSERT INTO BaryCentreNodes (ChildBodyDBID, ParentBodyDBID, HierarchyDepth) 
                                VALUES (@childId, @parentId, @depth);";

                            cmd.Parameters.AddWithValue("@childId", bodyDbId);
                            cmd.Parameters.AddWithValue("@parentId", parentDbId);
                            cmd.Parameters.AddWithValue("@depth", depth);

                            cmd.ExecuteNonQuery();
                        }
                        #endregion
                    }
                }
            }
            #endregion
            #region Add to cache
            _bodyCache.TryAdd(cacheKey, bodyDbId);
            #endregion
            return bodyDbId;
        }
        private void InsertDataPoint(SqliteConnection conn,SqliteTransaction trans,long bodyDbId,EddnRecords rec)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO DataPoints (
                        BodyDBID, Timestamp_UnixSec, DistanceToArrival, UploaderID, SoftwareName, ScanType
                    ) VALUES (
                        @bodyDbId, @timestamp, @distance, @uploader, @software, @scanType
                    );";

                long unixSeconds = new DateTimeOffset(rec.Message.Timestamp)
                    .ToUnixTimeSeconds();

                cmd.Parameters.AddWithValue("@bodyDbId", bodyDbId);
                cmd.Parameters.AddWithValue("@timestamp", unixSeconds);
                cmd.Parameters.AddWithValue("@distance", rec.Message.DistanceFromArrivalLS);

                object uploaderVal = rec.Header?.UploaderId ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@uploader", uploaderVal);

                object softwareVal = rec.Header?.SoftwareName ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@software", softwareVal);

                object scanVal = rec.Message.ScanType ?? (object)DBNull.Value;
                cmd.Parameters.AddWithValue("@scanType", scanVal);

                cmd.ExecuteNonQuery();
            }
        }
        /// <summary> Logs comprehensive exception data structures out to disk error payload logs. </summary>
        private void LogErrorToDisk(string context, Exception ex)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string errorDir = Path.Combine(baseDir, "Data", "Errors");

                if (!Directory.Exists(errorDir))
                {
                    Directory.CreateDirectory(errorDir);
                }

                string errorPayload = $"[{DateTime.UtcNow:s}] CONTEXT: {context}" + Environment.NewLine +
                                      $"ERROR: {ex.Message}" + Environment.NewLine +
                                      $"STACK: {ex.StackTrace}" + Environment.NewLine +
                                      new string('-', 60) + Environment.NewLine;

                File.AppendAllText(Settings.DBErrorLogPath, errorPayload);
            }
            catch
            {
                // Protects queue execution loop throughput if IO access blocks
            }
        }
        /// <summary> Logs descriptive inline text alerts to the file-system error repository. </summary>
        private void LogErrorToDisk(string context, string error)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string errorDir = Path.Combine(baseDir, "Data", "Errors");

                if (!Directory.Exists(errorDir))
                {
                    Directory.CreateDirectory(errorDir);
                }

                string errorPayload = $"[{DateTime.UtcNow:s}] CONTEXT: {context}" + Environment.NewLine +
                                      $"ERROR: {error}" + Environment.NewLine +
                                      new string('-', 60) + Environment.NewLine;

                File.AppendAllText(Settings.DBErrorLogPath, errorPayload);
            }
            catch
            {

                // Fallback catch boundary logic safety gate
            }
        }
        #endregion

        #region Public Functions
        /// <summary> Enqueues a parsed record into the bounded collection block. Logs descriptive alerts to disk if invalid string containment anomalies cross the interface line. </summary> 
        public void EnqueueRecord(EddnRecords record)
        {
            if (record == null || record.Message == null) return;

            #region Validate String Mutations Separately
            if (record.Message.BodyName.Contains(record.Message.StarSystem))
            {
                string trackingAlert = $"BodyName contains the SystemName: {record.Message.BodyName}";
                LogErrorToDisk("EnqueueRecord Incorrect String Allocation", trackingAlert);
            }
            #endregion
            #region Pipeline Channel Push Handle
            else
            {
                try
                {
                    #region Compute absolute retrograde vector orientation shifts before pushing onto task pools
                    if (record.Message != null)
                    {
                        #region Orbit maps to retrograde if its physical absolute inclination exceeds 90 degrees
                        bool isRetro = Math.Abs(record.Message.OrbitalInclination) > 90.0;
                        #endregion

                        #region Pass calculated data flag down into transient structure models for processing runs
                        record.Message.RotationPeriod = isRetro ? 1.0 : 0.0;
                        #endregion
                    }
                    #endregion
                    _ingestionQueue.Add(record);
                }
                catch (InvalidOperationException ex)
                {
                    LogErrorToDisk("EnqueueRecord Bounded Queue Channel Closed", ex);
                }
            }
            #endregion
        }
        /// </summary> Signals to the queue that file processing is complete. Waits synchronously for the worker tasks to flush remaining telemetry writes safely to storage. </summary>
        public void CompleteIngestion()
        {
            _ingestionQueue.CompleteAdding();
            _consumerTask?.Wait();
        }
        /// <summary> Retrieves only System and Body string names for lightweight UI validation checks. Bypasses bulky telemetry records for near-instant execution. </summary>
        public static CelestialMetadata GetCelestialMetadata(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";
            var meta = new CelestialMetadata
            {
                SystemName = Settings.UnknownData,
                BodyName = Settings.UnknownData
            };

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT s.SystemName, b.BodyName
                        FROM Bodies b
                        JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                        WHERE s.SystemEDID = @sysEdId AND b.BodyEDID = @bodyEdId 
                        LIMIT 1;";

                    cmd.Parameters.AddWithValue("@sysEdId", systemEdId);
                    cmd.Parameters.AddWithValue("@bodyEdId", bodyEdId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            meta.SystemName = reader.IsDBNull(0) ? Settings.UnknownData : reader.GetString(0);
                            meta.BodyName = reader.IsDBNull(1) ? Settings.UnknownData : reader.GetString(1);
                        }
                    }
                }
            }
            return meta;
        }
        /// Extracts chronological telemetry lines into flat double arrays, performing on-the-fly conversions from Unix seconds to native Excel OADate markers for plotter alignment loops. </summary>
        public static CelestialDataset GetCelestialDataset(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";

            var dataset = new CelestialDataset();
            dataset.Metadata = new CelestialMetadata
            {
                SystemName = Settings.UnknownData,
                BodyName = Settings.UnknownData
            };

            var tempTimestamps = new List<double>();
            var tempDistances = new List<double>();

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            s.SystemName, 
                            b.BodyName, 
                            d.Timestamp_UnixSec, 
                            d.DistanceToArrival
                        FROM Bodies b
                        JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                        LEFT JOIN DataPoints d ON b.BodyDBID = d.BodyDBID
                        WHERE s.SystemEDID = @sysEdId AND b.BodyEDID = @bodyEdId
                        ORDER BY d.Timestamp_UnixSec ASC;";

                    cmd.Parameters.AddWithValue("@sysEdId", systemEdId);
                    cmd.Parameters.AddWithValue("@bodyEdId", bodyEdId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        bool namesAssigned = false;

                        while (reader.Read())
                        {
                            if (!namesAssigned)
                            {
                                dataset.Metadata.SystemName = reader.IsDBNull(0) ? Settings.UnknownData : reader.GetString(0);
                                dataset.Metadata.BodyName = reader.IsDBNull(1) ? Settings.UnknownData : reader.GetString(1);
                                namesAssigned = true;
                            }

                            if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                            {
                                long unixSeconds = reader.GetInt64(2);
                                double distance = reader.GetDouble(3);

                                double excelOADate = DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                                    .DateTime
                                    .ToOADate();

                                tempTimestamps.Add(excelOADate);
                                tempDistances.Add(distance);
                            }
                        }
                    }
                }
            }

            dataset.TimestampsExcelOA = tempTimestamps.ToArray();
            dataset.DistancesToArrival = tempDistances.ToArray();

            return dataset;
        }
        #region Public Static Relational Extraction Data Getters (Realigned Framework)
        /// <summary> Reads total data counts from database table indexes for tracking performance metrics. </summary>
        public static int GetCelestialDataPointCount(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";
            int recordCount = 0;

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*) 
                        FROM DataPoints 
                        WHERE BodyDBID = (
                            SELECT b.BodyDBID 
                            FROM Bodies b
                            JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                            WHERE s.SystemEDID = @sysEdId AND b.BodyEDID = @bodyEdId
                            LIMIT 1
                        );";

                    cmd.Parameters.AddWithValue("@sysEdId", systemEdId);
                    cmd.Parameters.AddWithValue("@bodyEdId", bodyEdId);

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        recordCount = Convert.ToInt32(result);
                    }
                }
            }
            return recordCount;
        }
        #endregion
        #region Driver Lifecycle Interface Disposal Methods
        /// <summary> Releases all bounded queue pipelines and shared relational resources safely. </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _ingestionQueue?.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion
    }
}
