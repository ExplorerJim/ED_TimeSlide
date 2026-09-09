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
    public partial class frm_Process1 : Form
    {
        #region Varibles
        public List<MDIMain.ED_Data> Database;
        int int_SystemShown;
        int int_BodyShown;
        DateTime dt_Min;
        DateTime dt_Max;
        #endregion

        #region Public Events
        public frm_Process1()
        {
            InitializeComponent();

            Load_Varibles();

            openFileDialog1.Filter = "EDData | *.EDD";
        }
        #endregion

        #region Private Functions
        private void Load_Varibles()
        {
            Database = new List<MDIMain.ED_Data>();
            int_SystemShown = 0;
            int_BodyShown = 0;

            //Hardcode the min and max dates for the graph, this will be changed to a user input in the future
            dt_Min = new DateTime(2025, 6, 30);
            dt_Max = new DateTime(2025, 11, 14);

            Setup_Graph();
        }
        #region GUI private functions
        private void but_AddFile_Click(object sender, EventArgs e)
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
        private void but_LoadFiles_Click(object sender, EventArgs e)
        {
            /// Function Variables
            string[] files = new string[0];
            StreamReader sw;
            string fileline = "";
            string[] lineitems = new string[0];
            MDIMain.ED_Data temp_data;
            MDIMain.ED_Data_Body temp_body;
            MDIMain.ED_Data_Point temp_datapoint;
            bool starsystemfound = false;
            bool bodyfound = false;
            int starsystems = 0;
            int bodies = 0;

            /// Reset the database
            Database = new List<MDIMain.ED_Data>();

            /// Get the filenames from the textbox
            files = txb_InputFileNames.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            #region loop through each file
            for (int f = 0; f < files.Length; f++)
            {
                /// Update the status textbox 
                txb_Status.AppendText("Load: " + files[f] + Environment.NewLine);

                try
                {
                    /// create the streamreader to read the file
                    sw = new StreamReader(files[f]);

                    #region Read file line by line
                    while (!sw.EndOfStream)
                    {
                        fileline = sw.ReadLine();
                        

                        #region If string is not blank
                        if (fileline != "")
                        {
                            temp_body = new MDIMain.ED_Data_Body();
                            starsystemfound = false;
                            bodyfound = false;

                            /// Split the line up by ,
                            lineitems = fileline.Split(',');

                            #region Is there enought data
                            if(chb_MinimumDataDuringLoad.Checked)
                            {
                                if(lineitems.Length < ((int)(nud_Options_MinDataDuringLoad.Value*2) + 2))
                                {
                                    continue;
                                }
                            }
                            #endregion

                            #region System Name filter
                            if (chb_System_Name.Checked == true)
                            {
                                if(lineitems[0] != txb_SystemName.Text)
                                {
                                    continue;
                                }
                                else
                                {

                                }
                            }
                            #endregion

                            #region Check if the system exists
                            for (int s = 0; s < Database.Count; s++)
                            {
                                if (Database[s].System_Name == lineitems[0])
                                {
                                    starsystemfound = true;
                                    starsystems++;
                                    #region Check if the body exists
                                    for(int b = 0; b < Database[s].Bodies.Count; b++)
                                    {
                                        if(Database[s].Bodies[b].Body_Name == lineitems[1])
                                        {
                                            bodyfound = true;
                                            bodies++;
                                            #region Loop throught the section of the line
                                            for (int i = 2; i < lineitems.Length; i = i + 2)
                                            {
                                                temp_datapoint = new MDIMain.ED_Data_Point();
                                                temp_datapoint.TimeData = DateTime.FromOADate(Convert.ToDouble(lineitems[i]));
                                                if ((temp_datapoint.TimeData > dt_Min) || (temp_datapoint.TimeData < dt_Max))
                                                {
                                                    temp_datapoint.DistanceFromArrivalLS = Convert.ToDouble(lineitems[i + 1]);
                                                    Database[s].Bodies[b].Data_Points.Add(temp_datapoint);
                                                }
                                            }
                                            #endregion
                                            break;
                                        }
                                    }
                                    #endregion
                                    #region If it doesn't exist
                                    if(!bodyfound)
                                    {
                                        temp_body.Body_Name = lineitems[1];
                                        temp_body.Data_Points = new List<MDIMain.ED_Data_Point>();
                                        bodies++;
                                        bodyfound = true;

                                        #region Loop throught the section of the line
                                        for (int i = 2; i < lineitems.Length; i = i + 2)
                                        {
                                            temp_datapoint = new MDIMain.ED_Data_Point();
                                            temp_datapoint.TimeData = DateTime.FromOADate(Convert.ToDouble(lineitems[i]));
                                            if ((temp_datapoint.TimeData > dt_Min) || (temp_datapoint.TimeData < dt_Max))
                                            {
                                                temp_datapoint.DistanceFromArrivalLS = Convert.ToDouble(lineitems[i + 1]);
                                                temp_body.Data_Points.Add(temp_datapoint);
                                            }
                                        }
                                        #endregion
                                        Database[s].Bodies.Add(temp_body);
                                        break;
                                    }
                                    #endregion  
                                }
                            }
                            #endregion
                            #region Add new system
                            if (!starsystemfound)
                            {
                                temp_data = new MDIMain.ED_Data();
                                temp_data.System_Name = lineitems[0];
                                temp_data.Bodies = new List<MDIMain.ED_Data_Body>();

                                temp_body.Body_Name = lineitems[1];
                                temp_body.Data_Points = new List<MDIMain.ED_Data_Point>();

                                #region Loop throught the section of the line
                                for (int i = 2; i < lineitems.Length; i = i + 2)
                                {
                                    temp_datapoint = new MDIMain.ED_Data_Point();
                                    temp_datapoint.TimeData = DateTime.FromOADate(Convert.ToDouble(lineitems[i]));
                                    if ((temp_datapoint.TimeData > dt_Min) || (temp_datapoint.TimeData < dt_Max))
                                    {
                                        temp_datapoint.DistanceFromArrivalLS = Convert.ToDouble(lineitems[i + 1]);
                                        temp_body.Data_Points.Add(temp_datapoint);
                                    }
                                }
                                #endregion
                                temp_data.Bodies.Add(temp_body);
                                Database.Add(temp_data);
                                bodies++;
                                starsystems++;
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
            }
            #endregion

            txb_Status.AppendText("Load complete" + Environment.NewLine);

            #region If required remove bodies with not enought data, then systems with no bodies
            if (chb_Options_MinimumDataPostLoad.Checked)
            {
                txb_Status.AppendText("Removing bodies with less than " + Convert.ToString(nud_Options_MinDataPostLoad.Value) + Environment.NewLine);

                for (int s = Database.Count - 1; s >= 0; s--)
                {
                    for (int b = Database[s].Bodies.Count - 1; b >= 0; b--)
                    {
                        if (Database[s].Bodies[b].Data_Points.Count < nud_Options_MinDataPostLoad.Value)
                        {
                            Database[s].Bodies.RemoveAt(b);
                            bodies--;
                        }
                    }
                }

                txb_Status.AppendText("Removing systems with no bodies" + Environment.NewLine);

                for (int s = Database.Count - 1; s >= 0; s--)
                {
                    if(Database[s].Bodies.Count == 0)
                    {
                        Database.RemoveAt(s);
                        starsystems--;
                    }
                }
            }
            #endregion

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

            Update_ComboBox();

            txb_Stats_Systems.Text = Convert.ToString(starsystems) + " - " + Database.Count.ToString();
            txb_Stats_Bodys.Text = Convert.ToString(bodies);
            if (Database.Count > 0)
            {
                Update_Graph();
            }
        }
        private void but_Previous_Click(object sender, EventArgs e)
        {
            //Still needs code, work around is to use the combobox to select the system which will give you the first body
        }
        private void but_Next_Click(object sender, EventArgs e)
        {
            if ((Database[int_SystemShown].Bodies.Count - 1) > int_BodyShown)
            {
                int_BodyShown++;
            }
            else
            {
                if ((Database.Count - 1) > int_SystemShown)
                {
                    int_SystemShown++;
                    cmb_Graph_System.SelectedIndex = int_SystemShown;
                    int_BodyShown = 0;
                }
                else
                {
                    int_SystemShown = 0;
                    cmb_Graph_System.SelectedIndex = int_SystemShown;
                    int_BodyShown = 0;
                }
            }

            Update_Graph();
        }
        private void cmb_Graph_System_SelectedIndexChanged(object sender, EventArgs e)
        {
            int_SystemShown = cmb_Graph_System.SelectedIndex;
            int_BodyShown = 0;
            Update_Graph();
        }
        #endregion
        private void Setup_Graph()
        {
            /// Function Variable
            GraphPane myPane;

            myPane = zedGraphControl1.GraphPane;

            myPane.Title.FontSpec.Size = 12;

            myPane.XAxis.Type = AxisType.Date;
            myPane.XAxis.Scale.Format = "dd/MM/yyyy";
            myPane.XAxis.Scale.FontSpec.Angle = 90;
            myPane.XAxis.Scale.FontSpec.Size = 6;
            myPane.XAxis.Scale.MajorUnit = DateUnit.Month;
            //myPane.XAxis.Scale.MajorStep = 1;
            //myPane.XAxis.Scale.MinorUnit = DateUnit.Day;
            // myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Title.FontSpec.Size = 6;
            myPane.XAxis.MajorGrid.IsVisible = true;
            myPane.XAxis.Title.Text = "Time";
            //myPane.XAxis.Scale.Min = new XDate(dt_Min);
            //myPane.XAxis.Scale.Max = new XDate(dt_Max);

            //myPane.YAxis.Scale.Min = -1.2;
            //myPane.YAxis.Scale.Max = 1.2;
            //myPane.YAxis.Scale.MajorStep = 10;
            //myPane.YAxis.Scale.MinorStep = 1;
            myPane.YAxis.MajorGrid.IsVisible = true;
            myPane.YAxis.Scale.FontSpec.Size = 6;
            myPane.YAxis.Title.FontSpec.Size = 6;
            myPane.YAxis.Title.Text = "Distance from Arrival LS";

            myPane.Legend.IsVisible = false;
        }
        private void Update_Graph()
        {
            /// Function Variable
            GraphPane myPane;
            PointPairList points;
            LineItem myCurve;

            myPane = zedGraphControl1.GraphPane;
            myPane.Title.Text = Database[int_SystemShown].System_Name + " - " + Database[int_SystemShown].Bodies[int_BodyShown].Body_Name;

            if (myPane.CurveList.Count > 0)
            {
                myPane.CurveList.RemoveAt(0);
            }

            points = new PointPairList();
            myCurve = myPane.AddCurve("Test Curve1", points, Color.Blue, SymbolType.Circle);

            for (int d = 0; d < Database[int_SystemShown].Bodies[int_BodyShown].Data_Points.Count; d++)
            {
                points.Add(new XDate(Database[int_SystemShown].Bodies[int_BodyShown].Data_Points[d].TimeData), Database[int_SystemShown].Bodies[int_BodyShown].Data_Points[d].DistanceFromArrivalLS);
            }

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
        }
        private void Update_ComboBox()
        {
            cmb_Graph_System.Items.Clear();

            for (int s = 0; s < Database.Count; s++)
            {
                cmb_Graph_System.Items.Add(Database[s].System_Name);
            }

            if (cmb_Graph_System.Items.Count > 0)
            {
                cmb_Graph_System.SelectedIndex = 0;
            }
        }
        #endregion
    }
}
