using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Cabling : Form
    {
        public Cabling()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
                Global_Module.Trench_Line_Type = "Nearest";

            else if (radioButton2.Checked == true)
                Global_Module.Trench_Line_Type = "Manual_Selection";

            //Global_Module.Cabling_Submitted = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
