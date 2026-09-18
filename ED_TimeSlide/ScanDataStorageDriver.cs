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
    public class ScanDataStorageDriver : IDisposable
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
            public double[] ExcelOATimestamps;
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
        private const int BatchSizeThreshold = 5000;
        private const int BatchTimeoutMilliseconds = 1000;
        #endregion

        public ScanDataStorageDriver(CancellationToken token)
        {
            _cancellationToken = token;
            _ingestionQueue = new BlockingCollection<EddnRecords>(
                QueueBoundedCapacity
            );

            _systemCache = new ConcurrentDictionary<long, long>();
            _bodyCache = new ConcurrentDictionary<string, long>();


            InitializeDatabaseStructure();
            PreloadCacheFromDatabase();
            StartConsumerPipeline();
        }

        #region Public Functions
        /// <summary> Places a parsed record into the bounded queue. Blocks automatically if the queue reaches its maximum RAM threshold. </summary> 
        public void EnqueueRecord(EddnRecords record)
        {
            if (record == null || record.Message == null) return;

            #region Throw and error if the systemName is in bodyName
            if (record.Message.BodyName.Contains(record.Message.StarSystem))
            {
                LogErrorToDisk("EnqueueRecord Incorect Body Name", $"BodyName contains the SystemName: {record.Message.BodyName}");
            }
            #endregion
            #region Add the record to the scanDB
            else
            {
                try
                {
                    _ingestionQueue.Add(record);
                }
                catch (InvalidOperationException ex)
                {
                    LogErrorToDisk("EnqueueRecord_QueueClosed", ex);
                }
            }
            #endregion
        }
        /// <summary> Signal to the queue that no more records will be added. Allows the worker thread to finish remaining writes safely. </summary>
        public void CompleteIngestion()
        {
            _ingestionQueue.CompleteAdding();
            _consumerTask?.Wait();
        }
        /// <summary> Retrieves only the System and Body string names for verification. Bypasses the massive DataPoints table for near-instant execution. <summary>
        public static CelestialMetadata GetCelestialMetadata(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";
            var meta = new CelestialMetadata { SystemName = Settings.UnknownData, BodyName = Settings.UnknownData };

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
        /// <summary> Extracts names and all data tracking timelines into high-performance flat double arrays. Converts internal Unix timestamps directly into native Excel OADate values. </summary>
        public static CelestialDataset GetCelestialDataset(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";

            var dataset = new CelestialDataset();
            dataset.Metadata = new CelestialMetadata { SystemName = Settings.UnknownData, BodyName = Settings.UnknownData };

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
                    d.Timestamp, 
                    d.DistanceToArrival
                FROM Bodies b
                JOIN StarSystems s ON b.SystemDBID = s.SystemDBID
                LEFT JOIN DataPoints d ON b.BodyDBID = d.BodyDBID
                WHERE s.SystemEDID = @sysEdId AND b.BodyEDID = @bodyEdId
                ORDER BY d.Timestamp ASC;";

                    cmd.Parameters.AddWithValue("@sysEdId", systemEdId);
                    cmd.Parameters.AddWithValue("@bodyEdId", bodyEdId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        bool namesAssigned = false;

                        while (reader.Read())
                        {
                            // 1. Assign metadata names once from the first available record row
                            if (!namesAssigned)
                            {
                                dataset.Metadata.SystemName = reader.IsDBNull(0) ? Settings.UnknownData : reader.GetString(0);
                                dataset.Metadata.BodyName = reader.IsDBNull(1) ? Settings.UnknownData : reader.GetString(1);
                                namesAssigned = true;
                            }

                            // 2. Safely capture data point arrays if records exist
                            if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                            {
                                long unixSeconds = reader.GetInt64(2);
                                double distance = reader.GetDouble(3);

                                // 3. Mathematical conversion from Unix timestamp to Excel Native OADate
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

            // 4. Hydrate high-performance primitives directly to avoid object overheads
            dataset.ExcelOATimestamps = tempTimestamps.ToArray();
            dataset.DistancesToArrival = tempDistances.ToArray();

            return dataset;
        }
        /// <summary> Retrieves the total number of telemetry data points recorded for a specific body. Bypasses heavy data rows and string joins, reading straight from the database index. </summary>
        public static int GetCelestialDataPointCount(long systemEdId, long bodyEdId)
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};Mode=ReadOnly;";
            int recordCount = 0;

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    // Two-step optimization combined into an efficient nested subquery look-up
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
        #region Private Functions
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

                            // System Table Initialization
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS StarSystems (
                                    SystemDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    SystemEDID BIGINT UNIQUE,
                                    SystemName TEXT
                                );";
                            command.ExecuteNonQuery();

                            // Bodies Table Initialization
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS Bodies (
                                    BodyDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    SystemDBID INTEGER,
                                    BodyEDID INTEGER,
                                    BodyName TEXT,
                                    FOREIGN KEY(SystemDBID) REFERENCES StarSystems(SystemDBID)
                                );";
                            command.ExecuteNonQuery();

                            // DataPoints Table Initialization
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS DataPoints (
                                    DataPointDBID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    BodyDBID INTEGER,
                                    Timestamp BIGINT,
                                    DistanceToArrival REAL,
                                    UploaderID TEXT,
                                    SoftwareName TEXT,
                                    ScanType TEXT,
                                    FOREIGN KEY(BodyDBID) REFERENCES Bodies(BodyDBID)
                                );";
                            command.ExecuteNonQuery();

                            // Composite Uniqueness Index Definition
                            command.CommandText = @"
                                CREATE UNIQUE INDEX IF NOT EXISTS idx_event_baseline 
                                ON DataPoints (BodyDBID, Timestamp, DistanceToArrival);";
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        LogErrorToDisk("InitializeDatabaseStructure", ex);
                        throw;
                    }
                }
            }
        }
        private void StartConsumerPipeline()
        {
            _consumerTask = Task.Run(
                () => ProcessQueueItemsInBatches(),
                _cancellationToken
            );
        }
        private void PreloadCacheFromDatabase()
        {
            string connectionString = $"Data Source={Settings.ScanDataDbPath};";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 1. Warm up Star Systems Lookup Cache
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

                // 2. Warm up Celestial Bodies Lookup Cache
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
            }
        }
        private void ProcessQueueItemsInBatches()
        {
            string connString = $"Data Source={Settings.ScanDataDbPath};";
            var localBatch = new List<EddnRecords>(BatchSizeThreshold);
            var lastFlushTime = DateTime.UtcNow;

            using (var connection = new SqliteConnection(connString))
            {
                connection.Open();

                // Inject the PRAGMA optimization flags directly into the session pipeline
                using (var pragmaCmd = connection.CreateCommand())
                {
                    pragmaCmd.CommandText = @"
                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = OFF;
                        PRAGMA cache_size = -50000;
                    ";
                    pragmaCmd.ExecuteNonQuery();
                }

                while (!_ingestionQueue.IsCompleted)
                {
                    if (_cancellationToken.IsCancellationRequested) break;

                    bool hasItem = false;
                    EddnRecords record = null;

                    try
                    {
                        int elapsed = (int)(DateTime.UtcNow - lastFlushTime).TotalMilliseconds;
                        int remainingTimeout = Math.Max(0, BatchTimeoutMilliseconds - elapsed);

                        hasItem = _ingestionQueue.TryTake(
                            out record,
                            remainingTimeout,
                            _cancellationToken
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogErrorToDisk("BatchConsumer_QueueTake", ex);
                    }

                    if (hasItem && record != null)
                    {
                        localBatch.Add(record);
                    }

                    bool sizeReached = localBatch.Count >= BatchSizeThreshold;
                    bool timeReached = (DateTime.UtcNow - lastFlushTime).TotalMilliseconds >= BatchTimeoutMilliseconds;

                    if ((sizeReached || timeReached || _ingestionQueue.IsCompleted) && localBatch.Count > 0)
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
        private void ExecuteBulkTransaction(SqliteConnection connection, List<EddnRecords> batch)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var record in batch)
                    {
                        if (record?.Message == null) continue;

                        long systemDbId = GetOrCreateCachedSystem(
                            connection, transaction, record.Message
                        );

                        long bodyDbId = GetOrCreateCachedBody(
                            connection, transaction, systemDbId, record.Message
                        );

                        InsertDataPoint(
                            connection, transaction, bodyDbId, record
                        );
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LogErrorToDisk("ExecuteBulkTransaction_Bailed", ex);
                }
            }
        }
        private long GetOrCreateCachedSystem(SqliteConnection conn, SqliteTransaction trans, ScanMessage msg)
        {
            // Cache Layer Hit: Skips the database query entirely
            if (_systemCache.TryGetValue(msg.SystemAddress, out long cachedId))
            {
                return cachedId;
            }

            // Cache Layer Miss: Write the record out to the database once
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT INTO StarSystems (SystemEDID, SystemName) 
                    VALUES (@sysEdId, @sysName);
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@sysEdId", msg.SystemAddress);
                cmd.Parameters.AddWithValue("@sysName", msg.StarSystem ?? (object)DBNull.Value);

                long dbId = (long)cmd.ExecuteScalar();

                // Track this ID for future lookup rounds
                _systemCache.TryAdd(msg.SystemAddress, dbId);
                return dbId;
            }
        }
        private long GetOrCreateCachedBody(SqliteConnection conn, SqliteTransaction trans, long systemDbId, ScanMessage msg)
        {
            string cacheKey = $"{systemDbId}_{msg.BodyId}";

            if (_bodyCache.TryGetValue(cacheKey, out long cachedId))
            {
                return cachedId;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    INSERT INTO Bodies (SystemDBID, BodyEDID, BodyName) 
                    VALUES (@sysDbId, @bodyEdId, @bodyName);
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@sysDbId", systemDbId);
                cmd.Parameters.AddWithValue("@bodyEdId", msg.BodyId);
                cmd.Parameters.AddWithValue("@bodyName", msg.BodyName ?? (object)DBNull.Value);

                long dbId = (long)cmd.ExecuteScalar();

                _bodyCache.TryAdd(cacheKey, dbId);
                return dbId;
            }
        }
        private void InsertDataPoint(SqliteConnection conn,SqliteTransaction trans,long bodyDbId,EddnRecords rec)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;

                // Native unique constraint matching rules via INSERT OR IGNORE
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO DataPoints (
                        BodyDBID, Timestamp, DistanceToArrival, UploaderID, SoftwareName, ScanType
                    ) VALUES (
                        @bodyDbId, @timestamp, @distance, @uploader, @software, @scanType
                    );";

                long unixSeconds = new DateTimeOffset(rec.Message.Timestamp).ToUnixTimeSeconds();

                cmd.Parameters.AddWithValue("@bodyDbId", bodyDbId);
                cmd.Parameters.AddWithValue("@timestamp", unixSeconds);
                cmd.Parameters.AddWithValue("@distance", rec.Message.DistanceFromArrivalLS);
                cmd.Parameters.AddWithValue("@uploader", rec.Header?.UploaderId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@software", rec.Header?.SoftwareName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@scanType", rec.Message.ScanType ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }
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
                // TODO: Fail-safe path if logging directory permissions collapse
            }
        }
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
                                      $"ERROR: {error}" + Environment.NewLine;

                File.AppendAllText(Settings.DBErrorLogPath, errorPayload);
            }
            catch
            {
                // TODO: Fail-safe path if logging directory permissions collapse
            }
        }
        public void Dispose()
        {
            if (_isDisposed) return;

            _ingestionQueue?.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
