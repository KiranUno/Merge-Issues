using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Trenches : Form
    {
        public static bool radio1;
        public static bool radio2;
        public static bool radio3;
        public static bool radio4;
        public static bool radio5;

        public string Img_Path = @"D:\Desktop\Uno_Solar_Design_Assist_Pro\Uno_Solar_Design_Assist_Pro\Support Documents\Trenches\Images\";
        public string Selected_Trench = "";
        public Trenches()
        {
            InitializeComponent();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(@"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Resources\download (1).png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(@"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Resources\download (2).png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(@"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Resources\download (3).png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(@"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Resources\download.png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = System.Drawing.Image.FromFile(@"C:\ProgramData\Autodesk\ApplicationPlugins\UnoTeams\Resources\download (4).png");
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Image Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            if (radioButton1.Checked == true)
            {
                radio1 = true;
            }
            else if (radioButton2.Checked == true)
            {
                radio2 = true;
            }
            else if (radioButton3.Checked == true)
            {
                radio3 = true;
            }
            else if (radioButton4.Checked == true)
            {
                radio4 = true;
            }
            else if (radioButton5.Checked == true)
            {
                radio5 = true;
            }
        }

        private void Trenches_Load(object sender, EventArgs e)
        {

        }
    }
}
