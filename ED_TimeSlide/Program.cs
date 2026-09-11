using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ED_TimeSlide
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// To get the packages run these commands in the Package Manager Console:
        /// Install-Package ZedGraph
        /// Install-Package Newtonsoft.Json
        /// 
        /// Make sure you have Winrar installed if using rar files
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MDIMain());
        }
    }
}
