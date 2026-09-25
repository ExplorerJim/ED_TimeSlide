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

        // Phase 2.0: Unified Central Gatekeeper Engine Reference Mapping
        private readonly ScanDatabaseBroker _databaseBroker;
        #endregion

        public ScanDataStorageDriver(CancellationToken token)
        {
            _cancellationToken = token;
            _databaseBroker = new ScanDatabaseBroker();

            _ingestionQueue = new BlockingCollection<EddnRecords>(
                Settings.QueueBoundedCapacity
            );

            _systemCache = new ConcurrentDictionary<long, long>();
            _bodyCache = new ConcurrentDictionary<string, long>();

            // STEP 1: Run the structural schema check BEFORE locking down the connection channel
            // It safely opens, checks tables, and completely disposes of its temporary file handle
            _databaseBroker.InitializeDatabaseStructure();

            // STEP 2: Now that initialization is clear, safely throw open the high-speed transaction gate
            _databaseBroker.OpenIngestionSession();

            PreloadCacheFromDatabase();
            StartConsumerPipeline();
        }


        #region Private functions
        /// <summary>
        /// Populates concurrent lookup dictionaries from relational rows to eliminate
        /// duplicate index queries during active transaction bulk blocks.
        /// </summary>
        private void PreloadCacheFromDatabase()
        {
            using (var connection = _databaseBroker.OpenConnection())
            {
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
                                _systemCache.TryAdd(
                                    reader.GetInt64(0),
                                    reader.GetInt64(1)
                                );
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
                            if (!reader.IsDBNull(0) &&
                                !reader.IsDBNull(1) &&
                                !reader.IsDBNull(2))
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

        /// <summary>
        /// Deploys the background task worker loop to monitor incoming telemetry frames.
        /// </summary>
        private void StartConsumerPipeline()
        {
            _consumerTask = Task.Run(
                () => ProcessQueueItemsInBatches(),
                _cancellationToken
            );
        }
        /// <summary>
        /// Consumes telemetry items from the collection channel, executing performance flushes
        /// whenever threshold counts or timeout milestones are intercepted.
        /// </summary>
        private void ProcessQueueItemsInBatches()
        {
            var localBatch = new List<EddnRecords>(Settings.BatchSizeThreshold);
            var lastFlushTime = DateTime.UtcNow;

            while (!_ingestionQueue.IsCompleted)
            {
                if (_cancellationToken.IsCancellationRequested) break;

                bool hasItem = false;
                EddnRecords record = null;

                try
                {
                    int elapsed = (int)(DateTime.UtcNow - lastFlushTime).TotalMilliseconds;
                    int remainingTimeout = Math.Max(0, Settings.BatchTimeoutMilliseconds - elapsed);

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

                bool sizeReached = localBatch.Count >= Settings.BatchSizeThreshold;
                double deltaMs = (DateTime.UtcNow - lastFlushTime).TotalMilliseconds;
                bool timeReached = deltaMs >= Settings.BatchTimeoutMilliseconds;

                if ((sizeReached || timeReached || _ingestionQueue.IsCompleted) && localBatch.Count > 0)
                {
                    // 1. Extract the current active session connection handle
                    var connection = _databaseBroker.GetActiveSessionConnection();

                    // 2. Stream this batch pass down into our database rows
                    ExecuteBulkTransaction(connection, localBatch);

                    // 3. FLUSH GATEWAY FIX: Force the broker to bake records to disk and refresh its file context locks
                    _databaseBroker.CloseIngestionSession();
                    _databaseBroker.OpenIngestionSession();

                    localBatch.Clear();
                    lastFlushTime = DateTime.UtcNow;
                }
            }

            // Final trailing sweep flush loop pass
            if (localBatch.Count > 0)
            {
                var connection = _databaseBroker.GetActiveSessionConnection();
                ExecuteBulkTransaction(connection, localBatch);
                _databaseBroker.CloseIngestionSession();
            }
        }
        /// <summary>
        /// Executes a synchronous database bulk transaction, deduplicating systems,
        /// generating body rows, mapping structural parent trees, and writing telemetry points.
        /// </summary>
        private void ExecuteBulkTransaction(SqliteConnection connection, List<EddnRecords> batch)
        {
            var activeTransaction = _databaseBroker.GetActiveSessionTransaction();

            try
            {
                foreach (var record in batch)
                {
                    if (record?.Message == null) continue;

                    #region Get Mapped System ID
                    long systemDbid;
                    if (!_systemCache.TryGetValue(record.Message.SystemAddress, out systemDbid))
                    {
                        systemDbid = _databaseBroker.GetOrCreateSystem(connection, activeTransaction, record.Message);
                        _systemCache.TryAdd(record.Message.SystemAddress, systemDbid);
                    }
                    #endregion

                    #region Get Mapped Body ID (Bypassing JSON Parent Parsing Iterations)
                    string bodyCacheKey = $"{systemDbid}_{record.Message.BodyId}";
                    long bodyDbid;
                    if (!_bodyCache.TryGetValue(bodyCacheKey, out bodyDbid))
                    {
                        bodyDbid = _databaseBroker.GetOrCreateBodyRecord(connection, activeTransaction, systemDbid, record);
                        _bodyCache.TryAdd(bodyCacheKey, bodyDbid);
                    }
                    #endregion

                    // Write flat telemetry point instantly to storage
                    _databaseBroker.InsertTelemetryDataPoint(connection, activeTransaction, bodyDbid, record);
                }
            }
            catch (Exception ex)
            {
                LogErrorToDisk("ExecuteBulkTransaction Block Core Aborted", ex);
                throw;
            }
        }
        /// <summary>
        /// Logs comprehensive exception data structures out to disk error payload logs.
        /// </summary>
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

        /// <summary>
        /// Logs descriptive inline text alerts to the file-system error repository.
        /// </summary>
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
        /// <summary> 
        /// Enqueues a parsed record into the bounded collection block. Logs descriptive 
        /// alerts to disk if invalid string containment anomalies cross the interface line. 
        /// </summary> 
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
                    if (record.Message != null)
                    {
                        bool isRetro = Math.Abs(record.Message.OrbitalInclination) > 90.0;
                        record.Message.RotationPeriod = isRetro ? 1.0 : 0.0;
                    }

                    _ingestionQueue.Add(record);
                }
                catch (InvalidOperationException ex)
                {
                    LogErrorToDisk("EnqueueRecord Bounded Queue Channel Closed", ex);
                }
            }
            #endregion
        }

        /// <summary>
        /// Signals to the queue that file processing is complete. Waits synchronously 
        /// for the worker tasks to flush remaining telemetry writes safely to storage. 
        /// </summary>
        public void CompleteIngestion()
        {
            _ingestionQueue.CompleteAdding();
            _consumerTask?.Wait();

            // Cleanly close out your active engine transaction frame locks
            if (_databaseBroker != null)
            {
                _databaseBroker.CloseIngestionSession();
            }
        }

        #region Driver Lifecycle Interface Disposal Methods
        /// <summary>
        /// Releases all bounded queue pipelines and shared relational resources safely.
        /// </summary>
        public void Dispose()
        {
            if (_databaseBroker != null)
            {
                _databaseBroker.CloseIngestionSession();
            }

            if (_isDisposed) return;

            _ingestionQueue?.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion
    }
}
