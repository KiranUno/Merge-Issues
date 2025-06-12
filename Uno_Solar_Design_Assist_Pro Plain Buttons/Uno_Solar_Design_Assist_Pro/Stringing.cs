using System;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Stringing : Form
    {
        public Stringing()
        {
            InitializeComponent();
        }

        private void Stringing_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Add("SINGLE ROW");
            comboBox1.Items.Add("U STRING");
            comboBox1.Items.Add("LEAP FROG");
            comboBox1.Items.Add("CUSTOM");

            comboBox2.Items.Add("Left_To_Right");
            comboBox2.Items.Add("Right_To_Left");

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Global_Module.Stringing_Category = comboBox1.SelectedItem as string;
            Global_Module.Stringing_Type = comboBox2.SelectedItem as string;

            //Global_Module.Stringing_Submitted = true;
            this.DialogResult = DialogResult.OK;            
            this.Close();
        }
    }
}
