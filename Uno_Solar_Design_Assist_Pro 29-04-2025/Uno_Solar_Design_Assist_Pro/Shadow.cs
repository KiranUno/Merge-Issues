using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Shadow : Form
    {
        public static ObjectId selectedObj = ObjectId.Null;
        public static bool Applyclicked;
        public static DateTime datetime1;
        public static DateTime datetime2;
        public static double latitude = 0;
        public static double longitude = 0;
        public Shadow()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            datetime1 = dateTimePicker1.Value.Date;
            datetime2 = dateTimePicker2.Value.Date;
            latitude = double.Parse(textBox1.Text);
            longitude = double.Parse(textBox2.Text);
            Applyclicked = true;
            if (textBox1.Text == null && textBox2.Text == null)
            {
                MessageBox.Show("Some values are null");
                return;
            }
            this.Close();
            Shadow_Analysis c = new Shadow_Analysis();
            c.start();
        }

        private void Shadow_Load(object sender, EventArgs e)
        {
            button1.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            doc.SendStringToExecute("GEOGRAPHICLOCATION\n", true, false, true);
            doc.SendStringToExecute("B\n", true, false, true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect a 3D solid:");
            peo.SetRejectMessage("\nOnly 3D solids are allowed.");
            peo.AddAllowedClass(typeof(Solid3d), false);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status == PromptStatus.OK)
            {
                button1.Enabled = true;
                selectedObj = per.ObjectId;

            }
            else
            {
                MessageBox.Show("Please select a valid 3D Object");
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
