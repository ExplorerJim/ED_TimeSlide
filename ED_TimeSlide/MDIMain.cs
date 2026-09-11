using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ED_TimeSlide
{
    public partial class MDIMain : Form
    {
        #region Structures
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
        #endregion

        #region Instances
        Form1 inst_Form1;
        frm_ReduceFileSize inst_ReduceFileSize;
        frm_Process1 inst_Process1;
        frm_Journey_Tracker inst_Journey_Tracker;
        FormTimeSlipAnalysis inst_FormTimeSlipAnalysis;
        #endregion

        public MDIMain()
        {
            InitializeComponent();

            Load_Variables();

            //Load_Form_Instance(inst_Process1);
            //Load_Form_Instance(inst_Journey_Tracker);
            
            Load_Form_Instance(inst_FormTimeSlipAnalysis);
        }
        #region Tool Strip
        private void form1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Load_Form_Instance(inst_Form1);
        }
        private void form2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Load_Form_Instance(inst_ReduceFileSize);
        }
        private void process1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Load_Form_Instance(inst_Process1);
        }
        private void journeyTrackerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Load_Form_Instance(inst_Journey_Tracker);
        }
        private void fullAutomatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(inst_FormTimeSlipAnalysis == null || inst_FormTimeSlipAnalysis.IsDisposed)
            {
                inst_FormTimeSlipAnalysis = new FormTimeSlipAnalysis();
                inst_FormTimeSlipAnalysis.MdiParent = this;
            }

            Load_Form_Instance(inst_FormTimeSlipAnalysis);
        }
        #endregion
        #region Private Functions
        private void Load_Variables()
        {
            inst_ReduceFileSize = new frm_ReduceFileSize();
            inst_ReduceFileSize.MdiParent = this;

            inst_Process1 = new frm_Process1();
            inst_Process1.MdiParent = this;

            inst_Journey_Tracker = new frm_Journey_Tracker();
            inst_Journey_Tracker.MdiParent = this;

            inst_FormTimeSlipAnalysis = new FormTimeSlipAnalysis();
            inst_FormTimeSlipAnalysis.MdiParent = this;
        }
        #region Private Functions - Load Forms
        private void Load_Form_Instance(Form form)
        {
            if (!form.Visible)
            {
                form.Show();
            }
            else
            {
                form.Activate();
            }
        }
        #endregion
        #endregion
    }
}
