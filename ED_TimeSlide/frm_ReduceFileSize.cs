using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ZedGraph;

namespace ED_TimeSlide
{
    public partial class frm_ReduceFileSize : Form
    {
        #region Varibles
        public List<MDIMain.ED_Data> Database;
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
        private void but_Run_Click(object sender, EventArgs e)
        {
            /// Function Variables
            StreamReader sw;
            string[] files = new string[0];
            string fileline = "";
            string temp_string = "";
            string[] lineitems = new string[0];
            string[] singlesection = new string[0];
            MDIMain.ED_Data temp_data = new MDIMain.ED_Data();
            string starsystem = "";
            string body = "";
            DateTime timestamp = new DateTime();
            double distance = 0;
            bool timestamp_created = false;
            string dateprocessing = "";
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;
            bool starsystemfound = false;
            bool bodyfound = false;
            MDIMain.ED_Data_Point temp_datapoint = new MDIMain.ED_Data_Point();
            MDIMain.ED_Data_Body temp_body = new MDIMain.ED_Data_Body();
            MDIMain.ED_Data temp_starsystem = new MDIMain.ED_Data();
            int added = 0;

            /// Get the filenames from the textbox
            files = txb_InputFileNames.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                        
            txb_Status.Text = "Starting process" + Environment.NewLine;
            #region loop through each file
            for (int f = 0; f < files.Length; f++)
            {
                /// Update the status textbox 
                txb_Status.AppendText("Process: " + files[f] + Environment.NewLine);
                try
                {
                    /// create the streamreader to read the file
                    sw = new StreamReader(files[f]);
                    
                    #region Read file line by line
                    while (!sw.EndOfStream)
                    {
                        fileline = sw.ReadLine();
                        temp_data = new MDIMain.ED_Data();

                        #region If string is not blank
                        if (fileline != "")
                        {
                            /// Split the line up by ,
                            lineitems = fileline.Split(',');

                            /// Reset all the data
                            starsystem = "";
                            body = "";
                            timestamp = new DateTime();
                            distance = 0;
                            timestamp_created = false;

                            #region Loop throught the section of the line
                            for (int i = 0; i < lineitems.Length; i++)
                            {
                                singlesection = lineitems[i].Split(':');

                                #region Check for needed data
                                #region StarSystem
                                if (singlesection[0].Contains("StarSystem"))
                                {
                                    temp_string = singlesection[1];
                                    temp_string = temp_string.Remove(0, 2);
                                    starsystem = temp_string.Remove(temp_string.Length - 1, 1);
                                }
                                #endregion
                                #region BodyName
                                else if (singlesection[0].Contains("BodyName"))
                                {
                                    temp_string = singlesection[1];
                                    temp_string = temp_string.Remove(0, 2);
                                    body = temp_string.Remove(temp_string.Length - 1, 1);

                                    #region Check if this is a body type wanted
                                    if (body.Contains("Belt Cluster") || body.Contains("Ring"))
                                    {
                                        /// Break to next line
                                        break; 
                                    }
                                    #endregion
                                }
                                #endregion
                                #region Timestamp
                                else if (singlesection[0].Contains("timestamp" + '"'))
                                {
                                    temp_string = singlesection[1] + ":" + singlesection[2] + ":" + singlesection[3];
                                    temp_string = temp_string.Remove(0, 2);
                                    dateprocessing = temp_string.Remove(4, temp_string.Length - 4);
                                    year = int.Parse(dateprocessing);
                                    dateprocessing = temp_string.Substring(5, 2);
                                    month = int.Parse(dateprocessing);
                                    dateprocessing = temp_string.Substring(8, 2);
                                    day = int.Parse(dateprocessing);
                                    dateprocessing = temp_string.Substring(11, 2);
                                    hour = int.Parse(dateprocessing);
                                    dateprocessing = temp_string.Substring(14, 2);
                                    minute = int.Parse(dateprocessing);
                                    dateprocessing = temp_string.Substring(17, 2);
                                    second = int.Parse(dateprocessing);
                                    timestamp = new DateTime(year, month, day, hour, minute, second);
                                    timestamp_created = true;
                                }
                                #endregion
                                #region Distance
                                else if (singlesection[0].Contains("DistanceFromArrivalLS"))
                                {
                                    temp_string = singlesection[1];
                                    distance = Convert.ToDouble(temp_string.Remove(0, 1));

                                    #region Is it the primary star, if so next line
                                    if(distance == 0)
                                    {
                                        break;
                                    }
                                    #endregion
                                }
                                #endregion
                                #region Remove some moons
                                if (singlesection[0].Contains("Parents"))
                                {
                                    if(singlesection[1].Contains("Planet"))
                                    {
                                        break;
                                    }
                                }
                                #endregion
                                #endregion

                                #region Was all required data found?
                                if (starsystem != "" && body != "" && distance > 0 && timestamp_created)
                                {
                                    starsystemfound = false;
                                    bodyfound = false;

                                    #region Remove system name from body, both can be the same 
                                    if (body != starsystem)
                                    {
                                        if (body.Contains(starsystem))
                                        {
                                            body = body.Replace(starsystem, "");
                                            body = body.Remove(0, 1);

                                            if (body.Length > 5)
                                            {

                                            }
                                        }
                                        else
                                        {

                                        }
                                    }
                                    #endregion
                                    #region Remove moons
                                    if (body.Length > 2)
                                    {
                                        if (char.IsLetter(body[body.Length - 1]) && char.IsSeparator(body[body.Length - 2]))
                                        {
                                            break;
                                        }
                                    }
                                    #endregion
                                    #region check if star system is already known
                                    #region loop throught the database
                                    for (int d = 0; d < Database.Count; d++)
                                    {
                                        #region Does the starsystem match
                                        if (Database[d].System_Name == starsystem)
                                        {
                                            starsystemfound = true;
                                            #region check if the body is known
                                            for (int b = 0; b < Database[d].Bodies.Count; b++)
                                            {
                                                #region Does the name match
                                                if (Database[d].Bodies[b].Body_Name == body)
                                                {
                                                    bodyfound = true;

                                                    #region Add data here
                                                    temp_datapoint = new MDIMain.ED_Data_Point();
                                                    temp_datapoint.TimeData = timestamp;
                                                    temp_datapoint.DistanceFromArrivalLS = distance;
                                                    Database[d].Bodies[b].Data_Points.Add(temp_datapoint);
                                                    added++;
                                                    #endregion
                                                    break;
                                                }
                                                #endregion
                                            }
                                            #endregion

                                            #region If not found add a new body to the list
                                            if (!bodyfound)
                                            {
                                                temp_body = new MDIMain.ED_Data_Body();
                                                temp_body.Body_Name = body;
                                                temp_body.Data_Points = new List<MDIMain.ED_Data_Point>();
                                                temp_datapoint = new MDIMain.ED_Data_Point();
                                                temp_datapoint.TimeData = timestamp;
                                                temp_datapoint.DistanceFromArrivalLS = distance;
                                                temp_body.Data_Points.Add(temp_datapoint);
                                                Database[d].Bodies.Add(temp_body);
                                                added++;
                                                break;
                                            }

                                            #endregion
                                        }
                                        #endregion
                                    }
                                    #endregion
                                    #region If the star system wasn't found add a new enty
                                    if (!starsystemfound)
                                    {
                                        temp_starsystem = new MDIMain.ED_Data();
                                        temp_starsystem.System_Name = starsystem;
                                        temp_starsystem.Bodies = new List<MDIMain.ED_Data_Body>();
                                        temp_body = new MDIMain.ED_Data_Body();
                                        temp_body.Body_Name = body;
                                        temp_body.Data_Points = new List<MDIMain.ED_Data_Point>();
                                        temp_datapoint = new MDIMain.ED_Data_Point();
                                        temp_datapoint.TimeData = timestamp;
                                        temp_datapoint.DistanceFromArrivalLS = distance;
                                        temp_body.Data_Points.Add(temp_datapoint);
                                        temp_starsystem.Bodies.Add(temp_body);
                                        Database.Add(temp_starsystem);
                                        added++;
                                        break;
                                    }
                                    #endregion
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                    
                    sw.Dispose();
                }
                catch
                {
                    txb_Status.AppendText("Catch during file read" + Environment.NewLine);
                    return;
                }


                #region Save File One - Multiple
                if (rad_MultipleFiles.Checked)
                {
                    #region Sort the data
                    /// Sort systems by name
                    Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));

                    #region For each system
                    for (int s = 0; s < Database.Count; s++)
                    {
                        #region for each body
                        for (int b = 0; b < Database[s].Bodies.Count; b++)
                        {
                            /// Sort the body data points by date
                            Database[s].Bodies[b].Data_Points.Sort((s1, s2) => s1.TimeData.CompareTo(s2.TimeData));
                        }
                        #endregion

                        /// Sort the bodies in sistance order of the first data point
                        Database[s].Bodies.Sort((s1, s2) => s1.Data_Points[0].DistanceFromArrivalLS.CompareTo(s2.Data_Points[0].DistanceFromArrivalLS));
                    }
                    #endregion

                    txb_Status.AppendText("Database sorted" + Environment.NewLine);
                    #endregion

                    #region Write file
                    temp_string = files[f].Remove(files[f].Length - 5, 5);
                    temp_string += "EDD";

                    using (StreamWriter sw2 = new StreamWriter(temp_string))
                    {
                        #region for each system
                        for (int s = 0; s < Database.Count; s++)
                        {
                            #region for each body
                            for (int b = 0; b < Database[s].Bodies.Count; b++)
                            {
                                temp_string = Database[s].System_Name + "," + Database[s].Bodies[b].Body_Name;

                                #region for each data point
                                for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++)
                                {
                                    temp_string += "," + Database[s].Bodies[b].Data_Points[d].TimeData.ToOADate() + "," + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString();
                                }
                                #endregion

                                /// Write the body data to file
                                sw2.WriteLine(temp_string);
                            }
                            #endregion
                        }
                        #endregion

                    }
                    #endregion

                    /// Clear Database
                    Database = new List<MDIMain.ED_Data>();

                    txb_Status.AppendText("File saved" + Environment.NewLine);
                }
                #endregion
            }
            #endregion

            #region One file export
            if (rad_OneFile.Checked)
            {
                txb_Status.AppendText("Load process complete. " + (Database.Count).ToString() + " Systems found, " + added.ToString() + " data points." + Environment.NewLine);

                #region Sort the data
                /// Sort systems by name
                Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));

                #region For each system
                for (int s = 0; s < Database.Count; s++)
                {
                    #region for each body
                    for (int b = 0; b < Database[s].Bodies.Count; b++)
                    {
                        /// Sort the body data points by date
                        Database[s].Bodies[b].Data_Points.Sort((s1, s2) => s1.TimeData.CompareTo(s2.TimeData));
                    }
                    #endregion

                    /// Sort the bodies in sistance order of the first data point
                    Database[s].Bodies.Sort((s1, s2) => s1.Data_Points[0].DistanceFromArrivalLS.CompareTo(s2.Data_Points[0].DistanceFromArrivalLS));
                }
                #endregion

                txb_Status.AppendText("Database sorted" + Environment.NewLine);
                #endregion

                #region Save File One - File
                using (StreamWriter sw2 = new StreamWriter(txb_OutputFileName.Text))
                {
                    #region for each system
                    for (int s = 0; s < Database.Count; s++)
                    {
                        #region for each body
                        for (int b = 0; b < Database[s].Bodies.Count; b++)
                        {
                            temp_string = Database[s].System_Name + "," + Database[s].Bodies[b].Body_Name;

                            #region for each data point
                            for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++)
                            {
                                temp_string += "," + Database[s].Bodies[b].Data_Points[d].TimeData.ToOADate() + "," + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString();
                            }
                            #endregion

                            /// Write the body data to file
                            sw2.WriteLine(temp_string);
                        }
                        #endregion
                    }
                    #endregion

                }
                txb_Status.AppendText("File saved" + Environment.NewLine);
                #endregion
            }
            #endregion
        }
        #endregion
        #endregion
    }
}
