using System;
using System.Diagnostics;
using System.IO;

namespace ED_TimeSlide
{
    public static class EddnLogProcessor
    {
        #region Public Static Extraction Entry Points
        /// <summary>
        /// Reads any valid data file path (natively or via WinRAR archive extraction)
        /// and streams every single raw text line directly to the provided callback action.
        /// </summary>
        public static void ProcessDataFile(string filePath, Action<string> lineCallback)
        {
            #region Path Verification Guard
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;
            if (lineCallback == null) return;
            #endregion

            #region Native JSONL File Processing Path
            if (filePath.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            {
                ReadJsonlFileNative(filePath, lineCallback);
                return;
            }
            #endregion

            #region Compressed Archive Extraction Path (.rar, .bz2, .zip)
            StreamCompressedArchiveViaWinRar(filePath, lineCallback);
            #endregion
        }
        #endregion

        #region Private Internal Worker Engines
        private static void ReadJsonlFileNative(string jsonlPath, Action<string> lineCallback)
        {
            #region Line-by-Line Native Stream Reader
            using (StreamReader reader = new StreamReader(jsonlPath))
            {
                string textLine;
                while ((textLine = reader.ReadLine()) != null)
                {
                    lineCallback(textLine);
                }
            }
            #endregion
        }

        private static void StreamCompressedArchiveViaWinRar(string archivePath, Action<string> lineCallback)
        {
            #region Setup Temporary Workspace Directory
            string tempDir = "";
            string[] extractedFiles = new string[] { };
            bool extractionSuccessful = false;
            #endregion

            #region Invoke WinRAR Extraction Subprocess
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), $"ED_Extract_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Settings.winRarExePath,
                    Arguments = $"e -y -ibck \"{archivePath}\" \"{tempDir}\\\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

                if (Directory.Exists(tempDir))
                {
                    extractedFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                    extractionSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROCESSOR CRASH] Extraction error on {archivePath}: {ex.Message}");
                extractionSuccessful = false;
            }
            #endregion

            #region Read Textual Streams from Extracted Temp Workspace
            if (extractionSuccessful)
            {
                try
                {
                    foreach (string file in extractedFiles)
                    {
                        if (new FileInfo(file).Length == 0) continue;

                        foreach (string line in File.ReadLines(file))
                        {
                            lineCallback(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PROCESSOR CRASH] Stream read error on {archivePath}: {ex.Message}");
                }
            }
            #endregion

            #region Rigid Temporary Workspace Directory Cleanup
            if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    Debug.WriteLine($"[PROCESSOR WARNING] Failed cleaning up temporary workspace: {tempDir}");
                }
            }
            #endregion
        }
        #endregion

        #region Public Static Filename Parsing Utilities
        /// <summary>
        /// Scans an archive or file name string via regex constraints 
        /// to isolate and extract the standardized upload date string (yyyy-MM-dd).
        /// </summary>
        public static string ExtractDateFromFilename(string filename)
        {
            #region Regex Date Component Extraction
            if (string.IsNullOrWhiteSpace(filename)) return null;

            var match = System.Text.RegularExpressions.Regex.Match(filename, @"\d{4}-\d{2}-\d{2}");
            return match.Success ? match.Value : null;
            #endregion
        }
        #endregion

        public static void EvaluateAndRouteLine(string textLine, Action<EddnRecords> successCallback)
        {
            // Basic structural validation gate matching specification section 3.02.02
            if (string.IsNullOrWhiteSpace(textLine)) return;
            if (!textLine.Contains("\"event\":\"Scan\"") && !textLine.Contains("\"event\": \"Scan\"")) return;

            try
            {
                var record = Newtonsoft.Json.JsonConvert.DeserializeObject<EddnRecords>(textLine);

                if (record?.Message != null && record.Message.EventName == "Scan")
                {
                    // Execute validation filter parameters check
                    if (record.Message.BodyId == 0) return;
                    if (record.Message.BodyName.Contains("Belt Cluster")) return;
                    if (record.Message.BodyName.Contains("Ring Cluster")) return;

                    if (record.Message.StarType != null &&
                        record.Message.DistanceFromArrivalLS <= Settings.MinStellarDistanceForStars)
                    {
                        return;
                    }

                    successCallback(record);
                }
            }
            catch
            {
                // TODO: Handle or route corrupt line string logging profiles to \Data\Errors\
            }
        }

    }
}
