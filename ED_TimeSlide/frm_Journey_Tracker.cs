using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ZedGraph;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace ED_TimeSlide
{
    public partial class frm_Journey_Tracker : Form
    {
        public frm_Journey_Tracker()
        {
            InitializeComponent();
        }

        #region Form Events
        private void but_FindFile_Click(object sender, EventArgs e)
        {
            //openFileDialog1.Filter = "Jsonl | *.jsonl"; Didn't work, to excited to find a journey so didn't fix it
            openFileDialog1.ShowDialog();
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            txb_FileLocation.Text = openFileDialog1.FileName;
        }
        private void but_FindJourney_Click(object sender, EventArgs e)
        {
            /// Function Variables
            string[] files = new string[0];
            StreamReader sw;
            string fileline = "";
            string[] lineitems = new string[0];
            string[] singlesection = new string[0];
            string temp_string = "";

            string starsystem = "";
            string body = "";
            double distance = 0;
            bool datapointfound = false;
            string commanderID = "";
            DateTime timestamp = new DateTime();
            bool timestamp_created = false;
            string dateprocessing = "";
            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;
            string scantype = "";

            int linenumber = 0;
            string commanderID2 = "";

            #region Find commander ID
            try
            {
                /// create the streamreader to read the file
                sw = new StreamReader(txb_FileLocation.Text);

                #region Read file line by line looking for the first datapoint that matches the criteria
                while (!sw.EndOfStream)
                {
                    fileline = sw.ReadLine();
                    #region Debug code
                    linenumber++;
                    if (linenumber == 245892)
                    {

                    }
                    #endregion

                    #region If string is not blank
                    if (fileline != "")
                    {
                        /// Split the line up by ,
                        lineitems = fileline.Split(',');

                        /// Reset all the data
                        starsystem = "";
                        body = "";
                        distance = 0;

                        #region Loop throught the section of the line
                        for (int i = 0; i < lineitems.Length; i++)
                        {
                            singlesection = lineitems[i].Split(':');

                            #region Check for needed data
                            #region System
                            if (singlesection[0].Contains("StarSystem"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                starsystem = temp_string.Remove(temp_string.Length - 1, 1);
                                #region Check if this is a system wanted
                                if (starsystem != txb_StarSystem.Text)
                                {
                                    starsystem = "";
                                    break; /// Break to end for loop
                                }
                                #endregion
                            }
                            else if (singlesection[0].Contains("BodyName"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                body = temp_string.Remove(temp_string.Length - 1, 1);

                                #region Check if this is a body type wanted
                                if (body != txb_Body.Text)
                                {
                                    body = "";
                                    break; /// Break to end for loop
                                }
                                #endregion
                            }
                            #endregion
                            #region BodyName
                            else if (singlesection[0].Contains("BodyName"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                body = temp_string.Remove(temp_string.Length - 1, 1);

                                #region Check if this is a body type wanted
                                if (body != txb_Body.Text)
                                {
                                    body = "";
                                    break; /// Break to end for loop
                                }
                                #endregion
                            }
                            #endregion
                            #region Distance
                            else if (singlesection[0].Contains("DistanceFromArrivalLS"))
                            {
                                temp_string = singlesection[1];
                                distance = Convert.ToDouble(temp_string.Remove(0, 1));

                                #region Is it the primary star, if so next line
                                if (temp_string.Remove(0, 1) != txb_DistanceToArrival.Text)
                                {
                                    distance = 0;
                                    break; /// Break to end for loop
                                }
                                #endregion
                            }
                            #endregion
                            #endregion
                        }
                        #endregion
                        #region Check if datapoint found
                        if ((starsystem != "") && (distance > 0) && (body != ""))
                        {
                            datapointfound = true;
                        }
                        else
                        {
                            continue; /// To next line
                        }
                        #endregion
                        #region Find Commander ID
                        for (int i = 0; i < lineitems.Length; i++)
                        {
                            singlesection = lineitems[i].Split(':');

                            #region Uploader ID
                            if (singlesection[0].Contains("uploaderID"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                txb_Commander_ID.Text = temp_string.Remove(temp_string.Length - 2, 2);
                                commanderID = txb_Commander_ID.Text;
                                break; // Out of for loop
                            }
                            #endregion
                        }
                        if(commanderID != "")
                        {
                            break; // Out of while loop
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion

                /// reset the streamreader to read the file again
                sw = new StreamReader(txb_FileLocation.Text);
                txb_Status.Text = "";

                #region Read file line by line looking for commander data
                while (!sw.EndOfStream)
                {
                    fileline = sw.ReadLine();

                    #region If string is not blank
                    if (fileline != "")
                    {
                        /// Split the line up by ,
                        lineitems = fileline.Split(',');

                        /// Reset all the data
                        body = "";
                        distance = -1;
                        starsystem = "";
                        scantype = "";
                        timestamp_created = false;
                        commanderID2 = "";

                        #region Loop throught the section of the line
                        for (int i = 0; i < lineitems.Length; i++)
                        {
                            singlesection = lineitems[i].Split(':');

                            #region Check for needed data
                            #region Uploader ID
                            if (singlesection[0].Contains("uploaderID"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                temp_string = temp_string.Remove(temp_string.Length - 2, 2);
                                if (temp_string != commanderID)
                                {
                                    break; // Out of for loop
                                }
                                else
                                {
                                    commanderID2 = temp_string;
                                }
                            }
                            else if (singlesection[0].Contains("StarSystem"))
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
                            }
                            #endregion
                            #region Scantype
                            else if (singlesection[0].Contains("ScanType"))
                            {
                                temp_string = singlesection[1];
                                temp_string = temp_string.Remove(0, 2);
                                scantype = temp_string.Remove(temp_string.Length - 1, 1);
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
                            }
                            #endregion

                            #endregion
                        }
                        if (commanderID2 != "")
                        {
                            txb_Status.AppendText(timestamp.ToString() + " - " + body + " - " + distance.ToString() + " - " + scantype + Environment.NewLine);
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion
            }
            catch
            {
                MessageBox.Show("Error reading file, please check the file is correct and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion
        }
        #endregion
    }
}
