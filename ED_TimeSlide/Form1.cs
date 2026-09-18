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

namespace ED_TimeSlide
{
    public partial class Form1 : Form
    {
        public struct ED_Data
        {
            public string System_Name;
            public List<ED_Data_Body> Bodies;
        }
        public struct ED_Data_Point
        {
            public DateTime TimeData;
            public double DistanceFromArrivalLS;
        }
        public struct ED_Data_Body
        {
            public string Body_Name;
            public List<ED_Data_Point> Data_Points;
        }

        public List<ED_Data> Database;

        public Form1()
        {
            InitializeComponent();

            Database = new List<ED_Data>();

            //Process_File();
        }

        private void but_Find_File_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void Process_File()
        {
            List<string> input = new List<string>();
            List<string> output;
            string temp_string = "";
            string temp_string2 = "";
            StreamReader sw;
            string[] temp_string_array = new string[0];
            string[] temp_string_array2 = new string[0];
            ED_Data_Point temp_datapoint = new ED_Data_Point();
            ED_Data_Body temp_body = new ED_Data_Body();
            ED_Data temp_starsystem = new ED_Data();
            string temp3 = "";
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;

            string starsystem = "";
            string body = "";
            DateTime timestamp = new DateTime();
            double distance = 0;
            bool timestamp_created = false;

            bool starsystemfound = false;
            bool bodyfound = false;
            int maxdatapoints = 0;
            string max_datapoints_System = "";
            string max_datapoints_body = "";
            int max_System_Position = 0;

            double distmin = 0;
            double distmax = 0;

            #region Clear the output Data
            output = new List<string>();
            #endregion
            #region Read file
            txb_Status.Text = "Starting process" + Environment.NewLine;
            txb_Odds.Text = "Odds" + Environment.NewLine;
            //txb_Status.Update();

            try
            {
                sw = new StreamReader(txb_File_Location.Text);

                #region Read file line by line
                while (!sw.EndOfStream)
                {
                    temp_string = sw.ReadLine();
                    
                    #region If string is not blank
                    if (temp_string != "")
                    {
                        /// Split the line up by ,
                        temp_string_array = temp_string.Split(',');

                        /// Reset all the data
                        starsystem = "";
                        body = "";
                        timestamp = new DateTime();
                        distance = 0;
                        timestamp_created = false;

                        #region Loop throught the section of the line
                        for (int i = 0; i < temp_string_array.Length; i++)
                        {
                            temp_string_array2 = temp_string_array[i].Split(':');

                            #region Check for needed data
                            #region StarSystem
                            if (temp_string_array2[0].Contains("StarSystem"))
                            {
                                temp_string2 = temp_string_array2[1];
                                temp_string2 = temp_string2.Remove(0, 2);
                                starsystem = temp_string2.Remove(temp_string2.Length - 1, 1);

                            }
                            #endregion
                            #region BodyName
                            else if (temp_string_array2[0].Contains("BodyName"))
                            {
                                temp_string2 = temp_string_array2[1];
                                temp_string2 = temp_string2.Remove(0, 2);
                                body = temp_string2.Remove(temp_string2.Length - 1, 1);
                            }
                            #endregion
                            #region Timestamp
                            else if (temp_string_array2[0].Contains("timestamp" + '"'))
                            {
                                temp_string2 = temp_string_array2[1] + ":" + temp_string_array2[2] + ":" + temp_string_array2[3];
                                temp_string2 = temp_string2.Remove(0, 2);
                                temp3 = temp_string2.Remove(4, temp_string2.Length - 4);
                                year = int.Parse(temp3);
                                temp3 = temp_string2.Substring(5, 2);
                                month = int.Parse(temp3);
                                temp3 = temp_string2.Substring(8, 2);
                                day = int.Parse(temp3);
                                temp3 = temp_string2.Substring(11, 2);
                                hour = int.Parse(temp3);
                                temp3 = temp_string2.Substring(14, 2);
                                minute = int.Parse(temp3);
                                temp3 = temp_string2.Substring(17, 2);
                                second = int.Parse(temp3);
                                timestamp = new DateTime(year, month, day, hour, minute, second);
                                timestamp_created = true;
                            }
                            #endregion
                            #region Distance
                            else if (temp_string_array2[0].Contains("DistanceFromArrivalLS"))
                            {
                                temp_string2 = temp_string_array2[1];
                                distance = Convert.ToDouble(temp_string2.Remove(0, 1));
                            }
                            #endregion
                            #endregion

                            #region Was all required data found?
                            if (starsystem != "" && body != "" && distance > 0 && timestamp_created)
                            {
                                starsystemfound = false;
                                bodyfound = false;
                                #region Remove system name from body
                                if (body != starsystem)
                                {
                                    body = body.Replace(starsystem, "");
                                    body = body.Remove(0, 1);
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
                                                temp_datapoint = new ED_Data_Point();
                                                temp_datapoint.TimeData = timestamp;
                                                temp_datapoint.DistanceFromArrivalLS = distance;
                                                Database[d].Bodies[b].Data_Points.Add(temp_datapoint);
                                                #endregion
                                                break;
                                            }
                                            #endregion
                                        }
                                        #endregion

                                        #region If not found add a new body to the list
                                        if (!bodyfound)
                                        {
                                            temp_body = new ED_Data_Body();
                                            temp_body.Body_Name = body;
                                            temp_body.Data_Points = new List<ED_Data_Point>();
                                            temp_datapoint = new ED_Data_Point();
                                            temp_datapoint.TimeData = timestamp;
                                            temp_datapoint.DistanceFromArrivalLS = distance;
                                            temp_body.Data_Points.Add(temp_datapoint);
                                            Database[d].Bodies.Add(temp_body);
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
                                    temp_starsystem = new ED_Data();
                                    temp_starsystem.System_Name = starsystem;
                                    temp_starsystem.Bodies = new List<ED_Data_Body>();
                                    temp_body = new ED_Data_Body();
                                    temp_body.Body_Name = body;
                                    temp_body.Data_Points = new List<ED_Data_Point>();
                                    temp_datapoint = new ED_Data_Point();
                                    temp_datapoint.TimeData = timestamp;
                                    temp_datapoint.DistanceFromArrivalLS = distance;
                                    temp_body.Data_Points.Add(temp_datapoint);
                                    temp_starsystem.Bodies.Add(temp_body);
                                    Database.Add(temp_starsystem);
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

            txb_Status.AppendText("Load process complete. " + (Database.Count).ToString() + " Systems found" + Environment.NewLine);
            //txb_Status.Update();
            #endregion
            #region Remove bodies with less than 4 datapoints, sort, find system with max data
            /// Sort systems first
            Database.Sort((s1, s2) => s1.System_Name.CompareTo(s2.System_Name));
            #region for each system
            for (int s = Database.Count - 1; s >= 0; s--)
            {
                #region for each body
                for (int b = Database[s].Bodies.Count - 1; b >= 0; b--)
                {
                    #region If only one datapoint, delete body else sort the datapoints by time
                    if (Database[s].Bodies[b].Data_Points.Count < 4)
                    {
                        Database[s].Bodies.RemoveAt(b);
                    }
                    else
                    {
                        Database[s].Bodies[b].Data_Points.Sort((s1, s2) => s1.TimeData.CompareTo(s2.TimeData));
                        #region Check if this is the most points and store data
                        if (Database[s].Bodies[b].Data_Points.Count > maxdatapoints)
                        {
                            maxdatapoints = Database[s].Bodies[b].Data_Points.Count;
                            max_datapoints_System = Database[s].System_Name;
                            max_datapoints_body = Database[s].Bodies[b].Body_Name;
                            max_System_Position = s;
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion
                #region If system now has no bodies remove it or sort the bodies
                if (Database[s].Bodies.Count == 0)
                {
                    Database.RemoveAt(s);
                }
                else
                {
                    Database[s].Bodies.Sort((s1, s2) => s1.Data_Points[0].DistanceFromArrivalLS.CompareTo(s2.Data_Points[0].DistanceFromArrivalLS));
                }
                #endregion
            }
            #endregion

            txb_Status.AppendText("Removed bodies with 1 data point, systems reduced to: " + (Database.Count).ToString() + Environment.NewLine);
            txb_Status.AppendText("Body with the most datapoints: " + max_datapoints_System + ": " + max_datapoints_body + " (" + maxdatapoints.ToString() + ")." + " Database entry " + max_System_Position.ToString() + Environment.NewLine);
            //txb_Status.Update();
            #endregion

            #region Only keep 3 bodies closest to star and remove astorid belts
            for (int s = 0; s < Database.Count; s++)
            {
                #region for each body backward remove astorid belts
                for (int b = Database[s].Bodies.Count - 1; b >= 0; b--)
                {
                    if(Database[s].Bodies[b].Body_Name.Contains("Belt Cluster") || Database[s].Bodies[b].Body_Name.Contains("Ring"))
                    {
                        Database[s].Bodies.RemoveAt(b);
                    }
                }
                #endregion
                #region for each body remove all but first 3 bodies
                for (int b = Database[s].Bodies.Count - 1; b >= 0; b--)
                {
                    if (b>2)
                    {
                        Database[s].Bodies.RemoveAt(b);
                    }
                }
                #endregion
                /*
                #region for each body 
                for (int b = 0; b < Database[s].Bodies.Count; b++)
                {
                    temp_string = Database[s].System_Name + " - " + Database[s].Bodies[b].Body_Name;

                    for(int d = 0; d< Database[s].Bodies[b].Data_Points.Count; d++)
                    {
                        temp_string = temp_string + ", " + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString("F2");
                    }
                    txb_BodyData.AppendText(temp_string + Environment.NewLine);
                }
                #endregion
                */
            }
            #endregion
            #region find min and max values, add text to odd textbox
            for (int s = 0; s < Database.Count; s++)
            {
                temp_string = "";
                for (int b = 0; b < Database[s].Bodies.Count; b++)
                {
                    distmin = 0;
                    distmax = 0;

                    for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++)
                    {
                        if (distmin == 0)
                        {
                            distmin = Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS;
                        }
                        else if (Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS < distmin)
                        {
                            distmin = Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS;
                        }
                        if (Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS > distmax)
                        {
                            distmax = Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS;
                        }
                    }

                    txb_Status.AppendText(Database[s].System_Name + " - " + Database[s].Bodies[b].Body_Name + ", data points: " + Database[s].Bodies[b].Data_Points.Count.ToString() + ", start:" + Database[s].Bodies[b].Data_Points[0].DistanceFromArrivalLS.ToString() + ", end: " + Database[s].Bodies[b].Data_Points[Database[s].Bodies[b].Data_Points.Count - 1].DistanceFromArrivalLS.ToString() + ", min:" + distmin.ToString() + ", max: " + distmax.ToString() + ", Variation: " + (distmax - distmin).ToString("F2") + Environment.NewLine);

                    if (distmin == Database[s].Bodies[b].Data_Points[0].DistanceFromArrivalLS && distmax == Database[s].Bodies[b].Data_Points[Database[s].Bodies[b].Data_Points.Count - 1].DistanceFromArrivalLS)
                    {

                    }
                    else if (distmax == Database[s].Bodies[b].Data_Points[0].DistanceFromArrivalLS && distmin == Database[s].Bodies[b].Data_Points[Database[s].Bodies[b].Data_Points.Count - 1].DistanceFromArrivalLS)
                    {

                    }
                    else
                    {
                        if (Database[s].Bodies[b].Data_Points.Count >= 10)
                        {
                            txb_Odds.AppendText(Database[s].System_Name + " - " + Database[s].Bodies[b].Body_Name + ", data points: " + Database[s].Bodies[b].Data_Points.Count.ToString() + ", start:" + Database[s].Bodies[b].Data_Points[0].DistanceFromArrivalLS.ToString() + ", end: " + Database[s].Bodies[b].Data_Points[Database[s].Bodies[b].Data_Points.Count - 1].DistanceFromArrivalLS.ToString() + ", min:" + distmin.ToString() + ", max: " + distmax.ToString() + ", Variation: " + (distmax - distmin).ToString("F2") + Environment.NewLine);

                            temp_string = Database[s].System_Name + " - " + Database[s].Bodies[b].Body_Name;
                            temp_string2 = Database[s].System_Name + " - " + Database[s].Bodies[b].Body_Name;

                            for (int d = 0; d < Database[s].Bodies[b].Data_Points.Count; d++)
                            {
                                temp_string2 = temp_string2 + ", " + Database[s].Bodies[b].Data_Points[d].TimeData.ToOADate();
                                temp_string = temp_string + ", " + Database[s].Bodies[b].Data_Points[d].DistanceFromArrivalLS.ToString("F5");
                            }
                            txb_BodyData.AppendText(temp_string2 + Environment.NewLine);
                            txb_BodyData.AppendText(temp_string + Environment.NewLine);
                        }
                    }
                }
            }
            #endregion
            //look at each system remove all bodies except for one with most data? This may loose a time skip
            //Create post run lookup for system 
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            txb_File_Location.Text = openFileDialog1.FileName;
        }

        private void but_Process_Click(object sender, EventArgs e)
        {
            Process_File();
        }
    }
}
