using System;
using System.Windows.Forms;

namespace Uno_Solar_Design_Assist_Pro
{
    public partial class Trenches : Form
    {
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
                Selected_Trench = "2AC Cable";
                pictureBox1.Image = System.Drawing.Image.FromFile(Img_Path + Selected_Trench + ".png");
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
                Selected_Trench = "3AC Cable";
                pictureBox1.Image = System.Drawing.Image.FromFile(Img_Path + Selected_Trench + ".png");
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
                Selected_Trench = "4AC Cable";
                pictureBox1.Image = System.Drawing.Image.FromFile(Img_Path + Selected_Trench + ".png");
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
                Selected_Trench = "5AC Cable";
                pictureBox1.Image = System.Drawing.Image.FromFile(Img_Path + Selected_Trench + ".png");
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
                Selected_Trench = "DC Cable";
                pictureBox1.Image = System.Drawing.Image.FromFile(Img_Path + Selected_Trench + ".png");
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
        }
    }
}
