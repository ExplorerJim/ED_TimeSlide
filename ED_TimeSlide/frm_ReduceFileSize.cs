using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace ED_TimeSlide
{
    public partial class frm_ReduceFileSize : Form
    {
        #region Varibles
        public List<MDIMain.ED_Data> Database;
        private CancellationTokenSource _cts;
        private bool _isProcessing = false;
        #endregion

        #region Public Events
        public frm_ReduceFileSize()
        {
            InitializeComponent();
            saveFileDialog1.Filter = "EDData | *.EDD";

            #region Load Varibles
            Load_Varibles();
            #endregion
        }
        #endregion

        #region Private Functions
        private void Load_Varibles()
        {
            Database = new List<MDIMain.ED_Data>();
        }

        #region GUI private functions
        private void but_FindFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if (openFileDialog1.FileNames.Length > 0)
            {
                for (int f = 0; f < openFileDialog1.FileNames.Length; f++)
                {
                    if (txb_InputFileNames.Text == "")
                    {
                        txb_InputFileNames.Text = openFileDialog1.FileName;
                    }
                    else
                    {
                        txb_InputFileNames.AppendText(Environment.NewLine + openFileDialog1.FileNames[f]);
                    }
                }
            }
            else if (openFileDialog1.FileName != "")
            {
                if (txb_InputFileNames.Text == "")
                {
                    txb_InputFileNames.Text = openFileDialog1.FileName;
                }
                else
                {
                    txb_InputFileNames.AppendText(Environment.NewLine + openFileDialog1.FileName);
                }
            }
        }

        private void but_ClearFileNames_Click(object sender, EventArgs e)
        {
            txb_InputFileNames.Text = "";
        }

        private void but_SaveFileName_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            txb_OutputFileName.Text = saveFileDialog1.FileName;
        }

        private void rad_MultipleFiles_CheckedChanged(object sender, EventArgs e)
        {
            lab_OutputFileName.Enabled = false;
            txb_OutputFileName.Enabled = false;
            but_SaveFileName.Enabled = false;
        }

        private void rad_OneFile_CheckedChanged(object sender, EventArgs e)
        {
            lab_OutputFileName.Enabled = true;
            txb_OutputFileName.Enabled = true;
            but_SaveFileName.Enabled = true;
        }
        #endregion
        #endregion

        private async void but_Run_Click(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            string[] files = txb_InputFileNames.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            bool isMultipleFilesChecked = rad_MultipleFiles.Checked;
            bool isOneFileChecked = rad_OneFile.Checked;
            string outputFileName = txb_OutputFileName.Text;
            bool useMultiThreading = chk_MultiThreaded.Checked;

            if (files == null || files.Length == 0)
            {
                MessageBox.Show("Please select input files first.", "No Files Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isProcessing = true;
                but_Run_Click_DisableUI(false);
                txb_Status.Text = (useMultiThreading ? "Starting Multi-Threaded engine..." : "Starting Single-Threaded engine...") + Environment.NewLine;

                await Task.Run(() =>
                {
                    if (useMultiThreading)
                        ProcessFilesMultiThreaded(files, isMultipleFilesChecked, isOneFileChecked, outputFileName);
                    else
                        ProcessFilesSingleThreaded(files, isMultipleFilesChecked, isOneFileChecked, outputFileName);
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText($"Critical Error: {ex.Message}" + Environment.NewLine); });
            }
            finally
            {
                _isProcessing = false;
                but_Run_Click_DisableUI(true);
            }
        }

        private void but_Run_Click_DisableUI(bool enable)
        {
            but_Run.Enabled = enable;
        }

        private void ProcessFilesSingleThreaded(string[] files, bool multipleFilesChecked, bool oneFileChecked, string outputFileName)
        {
            #region Variables
            StreamReader sw; string fileline = ""; string temp_string = ""; string[] lineitems; string[] singlesection;
            string starsystem = ""; string body = ""; DateTime timestamp = new DateTime(); double distance = 0; bool timestamp_created = false;
            string dateprocessing = ""; int year, month, day, hour, minute, second; bool starsystemfound = false; bool bodyfound = false;
            int added = 0;
            #endregion

            for (int f = 0; f < files.Length; f++)
            {
                if (string.IsNullOrWhiteSpace(files[f])) continue;
                string currentFile = files[f];
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("Process: " + currentFile + Environment.NewLine); });

                try
                {
                    sw = new StreamReader(files[f]);
                    while (!sw.EndOfStream)
                    {
                        fileline = sw.ReadLine();
                        if (fileline == "") continue;
                        lineitems = fileline.Split(',');
                        starsystem = ""; body = ""; distance = 0; timestamp_created = false;

                        for (int i = 0; i < lineitems.Length; i++)
                        {
                            singlesection = lineitems[i].Split(':');

                            // FIX: Added line items array element targets [1] to pull values out of json tokens
                            if (lineitems[i].Contains("StarSystem"))
                            {
                                temp_string = singlesection[1].Remove(0, 2); starsystem = temp_string.Remove(temp_string.Length - 1, 1);
                            }
                            else if (lineitems[i].Contains("BodyName"))
                            {
                                temp_string = singlesection[1].Remove(0, 2); body = temp_string.Remove(temp_string.Length - 1, 1);
                                if (body.Contains("Belt Cluster") || body.Contains("Ring")) break;
                            }
                            else if (lineitems[i].Contains("timestamp" + '"'))
                            {
                                temp_string = singlesection[1] + ":" + singlesection[2] + ":" + singlesection[3];
                                temp_string = temp_string.Remove(0, 2);
                                year = int.Parse(temp_string.Remove(4, temp_string.Length - 4)); month = int.Parse(temp_string.Substring(5, 2)); day = int.Parse(temp_string.Substring(8, 2));
                                hour = int.Parse(temp_string.Substring(11, 2)); minute = int.Parse(temp_string.Substring(14, 2)); second = int.Parse(temp_string.Substring(17, 2));
                                timestamp = new DateTime(year, month, day, hour, minute, second); timestamp_created = true;
                            }
                            else if (lineitems[i].Contains("DistanceFromArrivalLS"))
                            {
                                distance = Convert.ToDouble(singlesection[1].Remove(0, 1)); if (distance == 0) break;
                            }
                            else if (lineitems[i].Contains("Parents") && lineitems[i].Contains("Planet")) break;

                            if (starsystem != "" && body != "" && distance > 0 && timestamp_created)
                            {
                                if (body != starsystem && body.Contains(starsystem)) body = body.Replace(starsystem, "").Remove(0, 1);
                                if (body.Length > 2 && char.IsLetter(body[body.Length - 1]) && char.IsSeparator(body[body.Length - 2])) break;

                                starsystemfound = false; bodyfound = false;
                                for (int d = 0; d < Database.Count; d++)
                                {
                                    if (Database[d].System_Name == starsystem)
                                    {
                                        starsystemfound = true;
                                        var currentSys = Database[d];
                                        for (int b = 0; b < currentSys.Bodies.Count; b++)
                                        {
                                            if (currentSys.Bodies[b].Body_Name == body)
                                            {
                                                bodyfound = true;
                                                currentSys.Bodies[b].Data_Points.Add(new MDIMain.ED_Data_Point { TimeData = timestamp, DistanceFromArrivalLS = distance });
                                                added++; break;
                                            }
                                        }
                                        if (!bodyfound)
                                        {
                                            var newB = new MDIMain.ED_Data_Body { Body_Name = body, Data_Points = new List<MDIMain.ED_Data_Point>() };
                                            newB.Data_Points.Add(new MDIMain.ED_Data_Point { TimeData = timestamp, DistanceFromArrivalLS = distance });
                                            currentSys.Bodies.Add(newB); added++;
                                        }
                                        Database[d] = currentSys;
                                        break;
                                    }
                                }
                                if (!starsystemfound)
                                {
                                    var newSys = new MDIMain.ED_Data { System_Name = starsystem, Bodies = new List<MDIMain.ED_Data_Body>() };
                                    var newB = new MDIMain.ED_Data_Body { Body_Name = body, Data_Points = new List<MDIMain.ED_Data_Point>() };
                                    newB.Data_Points.Add(new MDIMain.ED_Data_Point { TimeData = timestamp, DistanceFromArrivalLS = distance });
                                    newSys.Bodies.Add(newB); Database.Add(newSys); added++; break;
                                }
                            }
                        }
                    }
                    sw.Dispose();
                }
                catch
                {
                    this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("Catch during file read" + Environment.NewLine); }); return;
                }

                if (multipleFilesChecked)
                {
                    Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));
                    for (int s = 0; s < Database.Count; s++)
                    {
                        for (int b = 0; b < Database[s].Bodies.Count; b++) Database[s].Bodies[b].Data_Points.Sort((s1, s2) => s1.TimeData.CompareTo(s2.TimeData));
                        Database[s].Bodies.Sort((b1, b2) => b1.Data_Points[0].DistanceFromArrivalLS.CompareTo(b2.Data_Points[0].DistanceFromArrivalLS));
                    }
                    temp_string = files[f].Remove(files[f].Length - 5, 5) + "EDD";
                    using (StreamWriter sw2 = new StreamWriter(temp_string))
                    {
                        for (int s = 0; s < Database.Count; s++)
                        {
                            for (int b = 0; b < Database[s].Bodies.Count; b++)
                            {
                                temp_string = Database[s].System_Name + "," + Database[s].Bodies[b].Body_Name;
                                for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++) temp_string += "," + Database[s].Bodies[b].Data_Points[d].TimeData.ToOADate() + "," + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString();
                                sw2.WriteLine(temp_string);
                            }
                        }
                    }
                    Database = new List<MDIMain.ED_Data>();
                }
            }

            if (oneFileChecked)
            {
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("Load complete. " + Database.Count.ToString() + " Systems." + Environment.NewLine); });
                Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));
                for (int s = 0; s < Database.Count; s++)
                {
                    for (int b = 0; b < Database[s].Bodies.Count; b++) Database[s].Bodies[b].Data_Points.Sort((s1, s2) => s1.TimeData.CompareTo(s2.TimeData));
                    Database[s].Bodies.Sort((b1, b2) => b1.Data_Points[0].DistanceFromArrivalLS.CompareTo(b2.Data_Points[0].DistanceFromArrivalLS));
                }
                using (StreamWriter sw2 = new StreamWriter(outputFileName))
                {
                    for (int s = 0; s < Database.Count; s++)
                    {
                        for (int b = 0; b < Database[s].Bodies.Count; b++)
                        {
                            temp_string = Database[s].System_Name + "," + Database[s].Bodies[b].Body_Name;
                            for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++) temp_string += "," + Database[s].Bodies[b].Data_Points[d].TimeData.ToOADate() + "," + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString();
                            sw2.WriteLine(temp_string);
                        }
                    }
                }
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("File saved successfully." + Environment.NewLine); });
            }
        }


        private void ProcessFilesMultiThreaded(string[] files, bool multipleFilesChecked, bool oneFileChecked, string outputFileName)
        {
            var localThreadCaches = new System.Collections.Concurrent.ConcurrentBag<Dictionary<string, MDIMain.ED_Data>>();
            int globalAddedCounter = 0;

            this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("Parallel.ForEach running with fixed sequential struct persistence..." + Environment.NewLine); });

            Parallel.ForEach(files, currentFile =>
            {
                if (string.IsNullOrWhiteSpace(currentFile)) return;
                var localDatabase = new Dictionary<string, MDIMain.ED_Data>();

                try
                {
                    using (StreamReader sw = new StreamReader(currentFile))
                    {
                        while (!sw.EndOfStream)
                        {
                            string fileline = sw.ReadLine();
                            if (string.IsNullOrEmpty(fileline)) continue;
                            string[] lineitems = fileline.Split(',');
                            string starsystem = ""; string body = ""; double distance = 0; bool timestamp_created = false;
                            DateTime timestamp = new DateTime();

                            for (int i = 0; i < lineitems.Length; i++)
                            {
                                string[] singlesection = lineitems[i].Split(':');
                                if (singlesection.Length < 2) continue;

                                if (lineitems[i].Contains("StarSystem"))
                                {
                                    string ts = singlesection[1]; ts = ts.Remove(0, 2); starsystem = ts.Remove(ts.Length - 1, 1);
                                }
                                else if (lineitems[i].Contains("BodyName"))
                                {
                                    string ts = singlesection[1]; ts = ts.Remove(0, 2); body = ts.Remove(ts.Length - 1, 1);
                                    if (body.Contains("Belt Cluster") || body.Contains("Ring")) break;
                                }
                                else if (lineitems[i].Contains("timestamp" + '"'))
                                {
                                    string ts = singlesection[1] + ":" + singlesection[2] + ":" + singlesection[3];
                                    ts = ts.Remove(0, 2);
                                    int y = int.Parse(ts.Remove(4, ts.Length - 4)); int m = int.Parse(ts.Substring(5, 2)); int d = int.Parse(ts.Substring(8, 2));
                                    int h = int.Parse(ts.Substring(11, 2)); int min = int.Parse(ts.Substring(14, 2)); int sec = int.Parse(ts.Substring(17, 2));
                                    timestamp = new DateTime(y, m, d, h, min, sec); timestamp_created = true;
                                }
                                else if (lineitems[i].Contains("DistanceFromArrivalLS"))
                                {
                                    distance = Convert.ToDouble(singlesection[1].Remove(0, 1)); if (distance == 0) break;
                                }
                                else if (lineitems[i].Contains("Parents") && lineitems[i].Contains("Planet")) break;

                                if (starsystem != "" && body != "" && distance > 0 && timestamp_created)
                                {
                                    if (body != starsystem && body.Contains(starsystem)) body = body.Replace(starsystem, "").Remove(0, 1);
                                    if (body.Length > 2 && char.IsLetter(body[body.Length - 1]) && char.IsSeparator(body[body.Length - 2])) break;

                                    // FIX: Look up existing system entry within local batch context instead of overwriting it blank
                                    if (!localDatabase.TryGetValue(starsystem, out var systemRecord))
                                    {
                                        systemRecord = new MDIMain.ED_Data { System_Name = starsystem, Bodies = new List<MDIMain.ED_Data_Body>() };
                                    }

                                    int bodyIdx = systemRecord.Bodies.FindIndex(b => b.Body_Name == body);
                                    if (bodyIdx < 0)
                                    {
                                        var newBody = new MDIMain.ED_Data_Body { Body_Name = body, Data_Points = new List<MDIMain.ED_Data_Point>() };
                                        newBody.Data_Points.Add(new MDIMain.ED_Data_Point { TimeData = timestamp, DistanceFromArrivalLS = distance });
                                        systemRecord.Bodies.Add(newBody);
                                    }
                                    else
                                    {
                                        systemRecord.Bodies[bodyIdx].Data_Points.Add(new MDIMain.ED_Data_Point { TimeData = timestamp, DistanceFromArrivalLS = distance });
                                    }

                                    localDatabase[starsystem] = systemRecord; // Struct persistence writeback re-injection
                                    Interlocked.Increment(ref globalAddedCounter); break;
                                }
                            }
                        }
                    }
                }
                catch { }

                if (multipleFilesChecked)
                {
                    var singleFileList = localDatabase.Values.ToList();
                    singleFileList.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));
                    string singleOutPath = currentFile.Remove(currentFile.Length - 5, 5) + "EDD";
                    using (StreamWriter sw2 = new StreamWriter(singleOutPath))
                    {
                        foreach (var sys in singleFileList)
                        {
                            if (sys.Bodies.Count > 0)
                                sys.Bodies.Sort((b1, b2) => b1.Data_Points[0].DistanceFromArrivalLS.CompareTo(b2.Data_Points[0].DistanceFromArrivalLS));
                            foreach (var b in sys.Bodies)
                            {
                                b.Data_Points.Sort((dp1, dp2) => dp1.TimeData.CompareTo(dp2.TimeData));
                                string temp_string = sys.System_Name + "," + b.Body_Name;
                                foreach (var d in b.Data_Points) temp_string += "," + d.TimeData.ToOADate() + "," + d.DistanceFromArrivalLS.ToString();
                                sw2.WriteLine(temp_string);
                            }
                        }
                    }
                }
                else { localThreadCaches.Add(localDatabase); }
            });

            if (oneFileChecked)
            {
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText("Performing Deferred Master Merge..." + Environment.NewLine); });
                Database.Clear();

                foreach (var localCache in localThreadCaches)
                {
                    foreach (var kvp in localCache)
                    {
                        int masterSysIdx = Database.FindIndex(s => s.System_Name == kvp.Key);
                        if (masterSysIdx < 0)
                        {
                            Database.Add(kvp.Value);
                        }
                        else
                        {
                            var masterSystem = Database[masterSysIdx];
                            foreach (var incomingBody in kvp.Value.Bodies)
                            {
                                int masterBodyIdx = masterSystem.Bodies.FindIndex(b => b.Body_Name == incomingBody.Body_Name);
                                if (masterBodyIdx < 0)
                                {
                                    masterSystem.Bodies.Add(incomingBody);
                                }
                                else
                                {
                                    // FIX: Cumulative merge constraint - accurately appends every matching data point
                                    masterSystem.Bodies[masterBodyIdx].Data_Points.AddRange(incomingBody.Data_Points);
                                }
                            }
                            Database[masterSysIdx] = masterSystem; // Global struct writeback
                        }
                    }
                }

                Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));
                foreach (var system in Database)
                {
                    foreach (var body in system.Bodies) body.Data_Points.Sort((dp1, dp2) => dp1.TimeData.CompareTo(dp2.TimeData));
                    if (system.Bodies.Count > 0)
                        system.Bodies.Sort((b1, b2) => b1.Data_Points[0].DistanceFromArrivalLS.CompareTo(b2.Data_Points[0].DistanceFromArrivalLS));
                }

                using (StreamWriter sw2 = new StreamWriter(outputFileName))
                {
                    foreach (var s in Database)
                    {
                        foreach (var b in s.Bodies)
                        {
                            string temp_string = s.System_Name + "," + b.Body_Name;
                            foreach (var d in b.Data_Points)
                            {
                                temp_string += "," + d.TimeData.ToOADate() + "," + d.DistanceFromArrivalLS.ToString();
                            }
                            sw2.WriteLine(temp_string);
                        }
                    }
                }
                this.Invoke((MethodInvoker)delegate { txb_Status.AppendText($"Process Complete! {Database.Count} Systems, {globalAddedCounter} points saved." + Environment.NewLine); });
            }
        }


    }
}
